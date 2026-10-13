using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadArrayReorderParametersS108(VmToolParameterData p)
        {
            DockArrayReorderModeS108.ItemsSource = VmArrayReorderS108.Options().ToList();
            DockArrayReorderModeS108.SelectedValue = VmArrayReorderS108.Normalize(p.ArrayReorderMode);
            DockArraySliceStartS108.Text = p.ArraySliceStart.ToString(CultureInfo.InvariantCulture);
            DockArraySliceLengthS108.Text = p.ArraySliceLength.ToString(CultureInfo.InvariantCulture);
            RefreshArrayReorderFieldsS108();
        }
        private void RefreshArrayReorderFieldsS108()
        {
            if (DockArrayReorderModeS108 == null) return;
            string mode = VmArrayReorderS108.Normalize(DockArrayReorderModeS108.SelectedValue as string);
            bool slice = mode == VmArrayReorderS108.Slice;
            SetDockParameterRowVisibilityS85(DockArraySliceStartS108, slice);
            SetDockParameterRowVisibilityS85(DockArraySliceLengthS108, slice);
            if (DockArrayReorderHintS108 != null)
                DockArrayReorderHintS108.Text = mode == VmArrayReorderS108.Reindex ? "连接原数组、来源索引和原始数量" : mode == VmArrayReorderS108.Concat ? "第一数组在前，追加数组在后" : slice ? "从 0 开始，保留原有顺序" : "相同值保持原有顺序";
        }
        private void ArrayReorderModeChangedS108(object sender, SelectionChangedEventArgs e)
        {
            RefreshArrayReorderFieldsS108();
            if (dockDraftUpdating || dockEditingTool == null || !VmArrayReorderS108.IsTool(dockEditingTool.Kind)) return;
            var saved = dockInputPortRows.ToDictionary(row => row.PortName, row => row.SelectedSourceKey);
            var original = dockEditingTool.Parameters;
            bool updating = dockDraftUpdating;
            try
            {
                dockDraftUpdating = true;
                var preview = (original ?? new VmToolParameterData()).Clone();
                preview.ArrayReorderMode = VmArrayReorderS108.Normalize(DockArrayReorderModeS108.SelectedValue as string);
                dockEditingTool.Parameters = preview;
                BuildDockInputPortRows(dockEditingTool);
                foreach (var row in dockInputPortRows)
                {
                    string key;
                    if (saved.TryGetValue(row.PortName, out key) && row.SourceOptions.Any(option => option.Key == key && option.IsValid)) row.SelectedSourceKey = key;
                    RefreshDockInputRowState(row);
                }
            }
            finally { dockEditingTool.Parameters = original; dockDraftUpdating = updating; }
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
            DockDraftValue_Changed(sender, e);
        }
        private string GetDraftInputBindingErrorS108(VmToolInstance tool, VmToolInputBindingData binding)
        {
            if (tool != null && VmStructuredDataS110.IsTool(tool.Kind) && ReferenceEquals(tool, dockEditingTool)) return GetJsonDraftBindingErrorS110(tool, binding);
            if (tool == null || !VmArrayReorderS108.IsTool(tool.Kind) || !ReferenceEquals(tool, dockEditingTool) || DockArrayReorderModeS108 == null)
                return GetInputBindingConfigurationError(tool, binding);
            var original = tool.Parameters;
            try
            {
                var preview = (original ?? new VmToolParameterData()).Clone();
                preview.ArrayReorderMode = VmArrayReorderS108.Normalize(DockArrayReorderModeS108.SelectedValue as string);
                tool.Parameters = preview;
                return GetInputBindingConfigurationError(tool, binding);
            }
            finally { tool.Parameters = original; }
        }

        private bool ReadArrayReorderParametersS108(VmToolParameterData p)
        {
            p.ArrayReorderMode = VmArrayReorderS108.Normalize(DockArrayReorderModeS108.SelectedValue as string);
            if (p.ArrayReorderMode != VmArrayReorderS108.Slice) return true;
            int start, length;
            if (!int.TryParse(DockArraySliceStartS108.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out start) ||
                !int.TryParse(DockArraySliceLengthS108.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out length))
                return FailDockDraft("切片起始索引与数量必须是整数。");
            p.ArraySliceStart = start; p.ArraySliceLength = length;
            return true;
        }
        private void RefreshArrayReorderConnectionS108(VmToolInstance tool)
        {
            var ports = ToolMetadata.GetInstanceInputPorts(tool).Where(port => !port.IsOptional).ToList();
            var missing = ports.Where(port => tool.GetInputBinding(port.PortName) == null).Select(port => port.DisplayName).ToList();
            bool ready = missing.Count == 0 && ports.All(port =>
            {
                var binding = tool.GetInputBinding(port.PortName); var source = GetInputSourceTool(binding); object value;
                return source != null && (source.ResultCode == "OK" || source.ResultCode == "NG") && string.IsNullOrWhiteSpace(source.ErrorMessage) && source.TryGetOutputValue(binding.SourcePortName, out value) && value != null;
            });
            tool.ConfigurationStatus = missing.Count > 0 ? "待连接数组" : ready ? "就绪" : "等待上游";
            tool.ConnectionStatus = missing.Count > 0 ? "需要：" + string.Join(" / ", missing) : ready ? "已连接 · 有值" : "已连接 · 等待本周期值";
            tool.ConnectionSummary = VmArrayReorderS108.DisplayName((tool.Parameters ?? new VmToolParameterData()).ArrayReorderMode) + " → 数组 / 来源索引";
        }

        private InspectionRecord RunArrayReorderS108(VmToolInstance tool)
        {
            var p = tool.Parameters ?? new VmToolParameterData();
            string error = VmArrayReorderS108.Validate(p);
            if (error != null) throw new InvalidOperationException(error);
            string mode = VmArrayReorderS108.Normalize(p.ArrayReorderMode), type = VmArrayReorderS108.DataType(tool.Kind);
            VmToolInstance producer;
            object input = ReadBranchInputS101(tool, "Array", out producer);
            VmArrayContractS105.RequireType(type, input);
            int firstCount = VmArrayReorderS108.Count(input), secondCount = 0, sourceCount = firstCount;
            int[] indices;
            object output;
            if (mode == VmArrayReorderS108.Concat)
            {
                object second = ReadBranchInputS101(tool, "OtherArray", out producer);
                VmArrayContractS105.RequireType(type, second); secondCount = VmArrayReorderS108.Count(second);
                sourceCount = firstCount + secondCount;
                if (sourceCount > VmArrayContractS105.MaxCount) throw new InvalidOperationException("合并后为 " + sourceCount + " 项，超过 10000 项；不会截断。");
                indices = Enumerable.Range(0, sourceCount).ToArray();
                output = type == "Number[]"
                    ? (object)new VmNumberArrayS105(indices.Select(index => index < firstCount ? ((VmNumberArrayS105)input)[index] : ((VmNumberArrayS105)second)[index - firstCount]))
                    : new VmStringArrayS105(indices.Select(index => index < firstCount ? ((VmStringArrayS105)input)[index] : ((VmStringArrayS105)second)[index - firstCount]));
            }
            else
            {
                if (mode == VmArrayReorderS108.Slice)
                {
                    if (p.ArraySliceStart > firstCount || p.ArraySliceLength > firstCount - p.ArraySliceStart)
                        throw new InvalidOperationException("切片超出 " + firstCount + " 项输入范围：起点 " + p.ArraySliceStart + "，数量 " + p.ArraySliceLength + "。末尾只允许数量为 0 的空切片。");
                    indices = Enumerable.Range(p.ArraySliceStart, p.ArraySliceLength).ToArray();
                }
                else if (mode == VmArrayReorderS108.Reindex)
                {
                    object rawIndices = ReadBranchInputS101(tool, "Indices", out producer);
                    VmArrayContractS105.RequireType("Number[]", rawIndices);
                    object rawCount = ReadBranchInputS101(tool, "SourceCount", out producer);
                    RequireSubflowValueTypeS102("Number", rawCount);
                    double expectedCount = Convert.ToDouble(rawCount, CultureInfo.InvariantCulture);
                    if (expectedCount != firstCount) throw new InvalidOperationException("关联数量不一致：索引对应 " + expectedCount.ToString("G17", CultureInfo.InvariantCulture) + " 项，但待重排数组为 " + firstCount + " 项；请连接生成索引模块的 SourceCount，并检查标签来源。");
                    var requested = (VmNumberArrayS105)rawIndices;
                    indices = new int[requested.Count];
                    for (int i = 0; i < requested.Count; i++)
                    {
                        double value = requested[i];
                        if (value < 0 || value >= firstCount || value != Math.Truncate(value))
                            throw new InvalidOperationException("索引数组第 " + i + " 项为 " + value.ToString("G17", CultureInfo.InvariantCulture) + "，必须是输入范围内从 0 开始的整数。");
                        indices[i] = (int)value;
                    }
                }
                else
                {
                    IEnumerable<int> order = Enumerable.Range(0, firstCount);
                    bool descending = mode == VmArrayReorderS108.Descending;
                    if (type == "Number[]")
                    {
                        var numbers = (VmNumberArrayS105)input;
                        order = descending ? order.OrderByDescending(index => numbers[index]).ThenBy(index => index) : order.OrderBy(index => numbers[index]).ThenBy(index => index);
                    }
                    else
                    {
                        var texts = (VmStringArrayS105)input;
                        order = descending ? order.OrderByDescending(index => texts[index], StringComparer.Ordinal).ThenBy(index => index) : order.OrderBy(index => texts[index], StringComparer.Ordinal).ThenBy(index => index);
                    }
                    indices = order.ToArray();
                }
                output = VmArrayReorderS108.CopyByIndices(input, indices);
            }
            var indexOutput = new VmNumberArrayS105(indices.Select(index => (double)index));
            string operation = VmArrayReorderS108.DisplayName(mode);
            tool.SetOutputValue("Array", output); tool.SetOutputValue("Indices", indexOutput);
            tool.SetOutputValue("Count", (double)indices.Length); tool.SetOutputValue("SourceCount", (double)sourceCount);
            tool.SetOutputValue("FirstCount", (double)firstCount); tool.SetOutputValue("SecondCount", (double)secondCount);
            tool.SetOutputValue("Operation", operation); tool.SetOutputValue("ResultCode", "OK");
            tool.InputSummary = firstCount + (mode == VmArrayReorderS108.Concat ? " + " + secondCount : "") + " 项";
            string message = operation + " · " + sourceCount + " → " + indices.Length + " 项 · 不改变上游";
            tool.OutputSummary = message;
            var record = CreateRecord(tool.Kind.ToString(), "OK", indices.Length, message, tool);
            resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
            return record;
        }

        private void LoadArrayReorderExampleS108_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("array-reorder"); }
        private bool BuildArrayReorderExampleS108()
        {
            var values = CreateFlowTool(VmToolKind.NumberArrayCreate, "01 第一批尺寸", true, null);
            values.Parameters.ArrayText = "3,1,1,2"; flowTools.Add(values);
            var labels = CreateFlowTool(VmToolKind.TextArraySplit, "02 第一批标签", true, null);
            labels.Parameters.ArrayText = "P0,P1,P2,P3"; flowTools.Add(labels);
            var sort = CreateFlowTool(VmToolKind.NumberArrayReorder, "03 尺寸稳定升序", true, null);
            sort.SetInputBinding("Array", values.ToolId, "Array"); flowTools.Add(sort);
            var sortedLabels = CreateFlowTool(VmToolKind.TextArrayReorder, "04 标签跟随排序", true, null);
            sortedLabels.Parameters.ArrayReorderMode = VmArrayReorderS108.Reindex;
            sortedLabels.SetInputBinding("Array", labels.ToolId, "Array"); sortedLabels.SetInputBinding("Indices", sort.ToolId, "Indices"); sortedLabels.SetInputBinding("SourceCount", sort.ToolId, "SourceCount"); flowTools.Add(sortedLabels);
            var slice = CreateFlowTool(VmToolKind.NumberArrayReorder, "05 保留中间两项", true, null);
            slice.Parameters.ArrayReorderMode = VmArrayReorderS108.Slice; slice.Parameters.ArraySliceStart = 1; slice.Parameters.ArraySliceLength = 2;
            slice.SetInputBinding("Array", sort.ToolId, "Array"); flowTools.Add(slice);
            var selectedLabels = CreateFlowTool(VmToolKind.TextArrayReorder, "06 标签跟随切片", true, null);
            selectedLabels.Parameters.ArrayReorderMode = VmArrayReorderS108.Reindex;
            selectedLabels.SetInputBinding("Array", sortedLabels.ToolId, "Array"); selectedLabels.SetInputBinding("Indices", slice.ToolId, "Indices"); selectedLabels.SetInputBinding("SourceCount", slice.ToolId, "SourceCount"); flowTools.Add(selectedLabels);
            var extra = CreateFlowTool(VmToolKind.NumberArrayCreate, "07 第二批尺寸", true, null);
            extra.Parameters.ArrayText = "4,5"; flowTools.Add(extra);
            var extraLabels = CreateFlowTool(VmToolKind.TextArraySplit, "08 第二批标签", true, null);
            extraLabels.Parameters.ArrayText = "P4,P5"; flowTools.Add(extraLabels);
            var merged = CreateFlowTool(VmToolKind.NumberArrayReorder, "09 合并两批尺寸", true, null);
            merged.Parameters.ArrayReorderMode = VmArrayReorderS108.Concat; merged.SetInputBinding("Array", slice.ToolId, "Array"); merged.SetInputBinding("OtherArray", extra.ToolId, "Array"); flowTools.Add(merged);
            var mergedLabels = CreateFlowTool(VmToolKind.TextArrayReorder, "10 同顺序合并标签", true, null);
            mergedLabels.Parameters.ArrayReorderMode = VmArrayReorderS108.Concat; mergedLabels.SetInputBinding("Array", selectedLabels.ToolId, "Array"); mergedLabels.SetInputBinding("OtherArray", extraLabels.ToolId, "Array"); flowTools.Add(mergedLabels);
            var statistics = CreateFlowTool(VmToolKind.NumberArrayStatistics, "11 合并结果统计", true, null);
            statistics.SetInputBinding("Array", merged.ToolId, "Array"); flowTools.Add(statistics);
            var label = CreateFlowTool(VmToolKind.TextArraySelect, "12 对照首项标签", true, null);
            label.SetInputBinding("Array", mergedLabels.ToolId, "Array"); flowTools.Add(label);
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false;
            return true;
        }
    }
}
