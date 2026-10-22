using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadBranchSelectionParametersS101(VmToolParameterData parameters)
        {
            ConfigureFlowValueTypePickerS103(DockBranchTypeS101, false, parameters.BranchValueType);
        }

        private void DockBranchTypeS101_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dockDraftUpdating || dockEditingTool == null || dockEditingTool.Kind != VmToolKind.BranchValueSelection) return;
            var saved = dockInputPortRows.ToDictionary(row => row.PortName, row => row.SelectedSourceKey);
            var original = dockEditingTool.Parameters;
            bool wasUpdating = dockDraftUpdating;
            try
            {
                dockDraftUpdating = true;
                var preview = (original ?? new VmToolParameterData()).Clone();
                preview.BranchValueType = ToolMetadata.NormalizeBranchValueType(DockBranchTypeS101.SelectedValue as string);
                dockEditingTool.Parameters = preview;
                BuildDockInputPortRows(dockEditingTool);
                foreach (var row in dockInputPortRows)
                {
                    string key;
                    if (saved.TryGetValue(row.PortName, out key) && row.SourceOptions.Any(option => option.Key == key && option.IsValid)) row.RestoreSourceKeyUI06(key);
                    else row.RestoreSourceKeyUI06(GetDockDefaultInputKey(row.PortName));
                    RefreshDockInputRowState(row);
                }
            }
            finally { dockEditingTool.Parameters = original; dockDraftUpdating = wasUpdating; }
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
            DockDraftValue_Changed(sender, e);
        }

        private object ReadBranchInputS101(VmToolInstance tool, string portName, out VmToolInstance producer)
        {
            var binding = tool.GetInputBinding(portName);
            if (binding == null) throw new InvalidOperationException(portName + " 尚未连接；选中的路径必须有实际来源。");
            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(portName + "：" + error);
            producer = GetInputSourceTool(binding);
            object value;
            if (producer == null || producer.ResultCode == "SKIP" || (producer.ResultCode != "OK" && producer.ResultCode != "NG") ||
                !string.IsNullOrWhiteSpace(producer.ErrorMessage) || !producer.TryGetOutputValue(binding.SourcePortName, out value) || value == null || IsDisposedRuntimeValueS69(value))
                throw new InvalidOperationException(portName + " 的来源未产生有效结果或已跳过；不会读取历史值或自动改用另一分支。");
            return value;
        }

        private InspectionRecord RunBranchValueSelectionS101(VmToolInstance tool)
        {
            try
            {
                VmToolInstance conditionProducer;
                object selector = ReadBranchInputS101(tool, "Selector", out conditionProducer);
                if (!(selector is bool)) throw new InvalidOperationException("选择条件必须是 Boolean，不隐式转换文本或数值。");
                bool chooseTrue = (bool)selector;
                string portName = chooseTrue ? "WhenTrue" : "WhenFalse";
                VmToolInstance producer;
                object raw = ReadBranchInputS101(tool, portName, out producer);
                string type = ToolMetadata.NormalizeBranchValueType((tool.Parameters ?? new VmToolParameterData()).BranchValueType);
                object result;
                if (VmFlowValueContractS103.IsGeometryType(type))
                {
                    VmFlowValueContractS103.RequireGeometry(type, raw);
                    result = VmFlowValueContractS103.CloneGeometry(raw);
                }
                else if (type == "Image" && raw is VmImageSnapshot)
                {
                    var snapshot = (VmImageSnapshot)raw;
                    using (HImage image = snapshot.CreateImageCopy())
                        result = VmImageSnapshot.Create(image, tool.ToolId, tool.InstanceName, "Value", snapshot.SourcePath, snapshot.SerialNumber);
                }
                else if (type == "Region" && raw is VmRegionSnapshot)
                {
                    using (HRegion region = ((VmRegionSnapshot)raw).CreateRegionCopy()) result = VmRegionSnapshot.Create(region, tool.ToolId, tool.InstanceName, "Value");
                }
                else if (type == "XLD" && raw is VmXldSnapshot)
                {
                    using (HObject contour = ((VmXldSnapshot)raw).CreateXldCopy()) result = VmXldSnapshot.Create(contour, tool.ToolId, tool.InstanceName, "Value");
                }
                else if (VmArrayContractS105.IsType(type))
                {
                    VmArrayContractS105.RequireType(type, raw);
                    result = VmArrayContractS105.Clone(raw);
                }
                else if (type == "String" && raw is string) result = raw;
                else if (type == "Boolean" && raw is bool) result = raw;
                else if (type == "Number" && (raw is double || raw is float || raw is decimal || raw is int || raw is long || raw is short || raw is byte || raw is uint || raw is ushort || raw is ulong || raw is sbyte))
                {
                    double number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                    if (double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidOperationException("选中数值不是有限值。");
                    result = number;
                }
                else throw new InvalidOperationException("选中结果与声明类型 " + type + " 不一致。");
                tool.SetOutputValue("Value", result);
                if (type == "Image" || type == "Region" || type == "XLD" || VmFlowValueContractS103.IsGeometryType(type))
                    tool.SetOutputCoordinateContext("Value", ResolveOutputCoordinateContextS102(producer, tool.GetInputBinding(portName).SourcePortName));
                tool.SetOutputValue("SelectedBranch", chooseTrue ? "True" : "False");
                tool.SetOutputValue("SelectedSource", producer.InstanceName + "." + tool.GetInputBinding(portName).SourcePortName);
                tool.SetOutputValue("SelectedResultCode", producer.ResultCode);
                string message = (chooseTrue ? "真" : "假") + "路径 → " + producer.InstanceName + "；输出 " + type + "，未读取另一分支。";
                tool.InputSummary = message; tool.OutputSummary = message;
                var record = CreateRecord("BranchValueSelection", "OK", result is double ? (double)result : 0, message, tool);
                resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
                RefreshResultOverlayLayers(); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
                return record;
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
        }

        private VmToolInstance GetSelectedBranchProducerS101(VmToolInstance tool)
        {
            return GetInputSourceTool(GetSelectedBranchBindingS101(tool));
        }

        private VmToolInputBindingData GetSelectedBranchBindingS101(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.BranchValueSelection) return null;
            var selector = tool.GetInputBinding("Selector");
            var source = GetInputSourceTool(selector);
            object value;
            if (source == null || selector == null || (source.ResultCode != "OK" && source.ResultCode != "NG") ||
                !string.IsNullOrWhiteSpace(source.ErrorMessage) || !source.TryGetOutputValue(selector.SourcePortName, out value) || !(value is bool)) return null;
            return tool.GetInputBinding((bool)value ? "WhenTrue" : "WhenFalse");
        }
    }
}
