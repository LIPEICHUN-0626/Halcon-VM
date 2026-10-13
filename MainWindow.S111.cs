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
        private void LoadSubflowLoopS111(VmToolParameterData p)
        {
            if (DockSubflowLoopModeS111 == null) return;
            DockSubflowLoopModeS111.ItemsSource = new[]
            {
                new VmToolValueOption { Key = VmSubflowLoopS111.Once, DisplayText = "单次调用" },
                new VmToolValueOption { Key = VmSubflowLoopS111.Repeat, DisplayText = "固定次数" },
                new VmToolValueOption { Key = VmSubflowLoopS111.Each, DisplayText = "数组逐项" }
            };
            var definition = p == null ? null : p.Subflow;
            DockSubflowLoopModeS111.SelectedValue = definition == null ? VmSubflowLoopS111.Once : VmSubflowLoopS111.Mode(definition);
            DockSubflowLoopCountS111.Text = (definition == null ? 1 : definition.RepeatCount).ToString(CultureInfo.InvariantCulture);
            DockSubflowLoopInputS111.ItemsSource = definition == null ? new List<VmSubflowPortData>()
                : definition.Inputs.Where(port => VmArrayContractS105.IsType(port.DataType)).ToList();
            DockSubflowLoopInputS111.SelectedValue = definition == null ? null : definition.LoopInputPort;
            RefreshSubflowLoopVisibilityS111();
        }
        private void RefreshSubflowLoopVisibilityS111()
        {
            if (DockSubflowLoopModeS111 == null) return;
            string mode = DockSubflowLoopModeS111.SelectedValue as string;
            DockSubflowLoopCountRowS111.Visibility = mode == VmSubflowLoopS111.Repeat ? Visibility.Visible : Visibility.Collapsed;
            DockSubflowLoopInputRowS111.Visibility = mode == VmSubflowLoopS111.Each ? Visibility.Visible : Visibility.Collapsed;
            DockSubflowLoopHintS111.Text = mode == VmSubflowLoopS111.Repeat
                ? "按从 0 开始的索引依次执行指定次数，允许 0 次。单次调用最多 1000 项。"
                : mode == VmSubflowLoopS111.Each
                    ? "按数组原顺序逐项执行；在内部编辑中使用当前项、索引和总数。空数组不执行内部模块。"
                    : "按原子流程调用一次。若返回开启逐项收集，会生成 1 项数组。";
        }
        private void SubflowLoopModeChangedS111(object sender, SelectionChangedEventArgs e)
        {
            RefreshSubflowLoopVisibilityS111();
            if (!dockDraftUpdating) DockDraftValue_Changed(sender, e);
        }
        private bool ReadSubflowLoopS111(VmToolParameterData p)
        {
            if (p.Subflow == null) return FailDockDraft("请先创建子流程内容。");
            p.Subflow.LoopMode = DockSubflowLoopModeS111.SelectedValue as string;
            if (p.Subflow.LoopMode == VmSubflowLoopS111.Repeat)
            {
                int count;
                if (!int.TryParse(DockSubflowLoopCountS111.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                    return FailDockDraft("固定执行次数必须是 0–1000 的整数。");
                p.Subflow.RepeatCount = count;
            }
            p.Subflow.LoopInputPort = DockSubflowLoopInputS111.SelectedValue as string;
            string error = VmSubflowLoopS111.Validate(p.Subflow);
            if (error != null) return FailDockDraft(error);
            var oldDefinition = dockEditingTool.Parameters == null ? null : dockEditingTool.Parameters.Subflow;
            if (oldDefinition != null)
            {
                var oldProxy = VmSubflowLoopS111.ProxyPorts(oldDefinition).ToDictionary(port => port.PortName, port => port.DataType);
                var newProxy = VmSubflowLoopS111.ProxyPorts(p.Subflow).ToDictionary(port => port.PortName, port => port.DataType);
                foreach (var used in oldDefinition.Tools.SelectMany(item => item.InputBindings ?? new List<VmToolInputBindingData>())
                    .Where(binding => binding.SourceToolId == VmSubflowData.InputProxyId && (binding.SourcePortName ?? string.Empty).StartsWith("$Loop", StringComparison.Ordinal)))
                {
                    string nextType, previousType;
                    if (!oldProxy.TryGetValue(used.SourcePortName, out previousType) ||
                        !newProxy.TryGetValue(used.SourcePortName, out nextType) || previousType != nextType)
                        return FailDockDraft("内部模块仍引用类型已改变的循环端口 " + used.SourcePortName + "。请先进入内部编辑，解除连接后再切换模式。");
                }
            }
            return true;
        }
        private void RefreshLoopEditorPreviewS111(SubflowEditorFrameS102 frame, VmToolInstance proxy)
        {
            if (VmSubflowLoopS111.Mode(frame.Definition) == VmSubflowLoopS111.Once) return;
            try
            {
                int count = VmSubflowLoopS111.Iterations(frame.Definition, proxy);
                VmSubflowLoopS111.SetContext(frame.Definition, proxy, 0, count);
                proxy.InputSummary = count == 0 ? "本次数组为空；没有当前项可预览" : "仅预览第 1 项 / 共 " + count + " 项；真实调用按索引依次运行";
            }
            catch (InvalidOperationException ex)
            {
                proxy.InputSummary = "逐项预览等待数组来源：" + ex.Message;
            }
        }
        private void LoadSubflowLoopExampleS111_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("bounded-subflow-loop"); }
        private bool BuildSubflowLoopExampleS111()
        {
            var values = CreateFlowTool(VmToolKind.NumberArrayCreate, "01 三件尺寸", true, null);
            values.Parameters.ArrayText = "9.8,10.1,10.4"; flowTools.Add(values);
            var compensate = CreateFlowTool(VmToolKind.NumericExpression, "逐项尺寸补偿", true, null);
            compensate.Parameters.NumericExpressionText = "round((A-10)*2,3)";
            compensate.SetInputBinding("A", VmSubflowData.InputProxyId, VmSubflowLoopS111.Item);
            var call = CreateFlowTool(VmToolKind.SubflowCall, "02 每件补偿并收集", true, null);
            try
            {
                call.Parameters.Subflow = new VmSubflowData
                {
                    LoopMode = VmSubflowLoopS111.Each, LoopInputPort = "Values",
                    Tools = CaptureFlowRecipeS102(new[] { compensate }),
                    Inputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "Values", DisplayName = "本批尺寸数组", DataType = "Number[]" } },
                    Outputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "Compensated", DisplayName = "补偿结果", DataType = "Number", SourceToolId = compensate.ToolId, SourcePortName = "Number", CollectIterations = true } }
                };
                call.SetInputBinding("Values", values.ToolId, "Array"); flowTools.Add(call);
            }
            catch { call.Dispose(); throw; }
            finally { compensate.Dispose(); }
            var summary = CreateFlowTool(VmToolKind.NumberArrayStatistics, "03 补偿数组统计", true, null);
            summary.SetInputBinding("Array", call.ToolId, "Compensated$Items"); flowTools.Add(summary);
            var repeatBody = CreateFlowTool(VmToolKind.NumericExpression, "顺序编号加一", true, null);
            repeatBody.Parameters.NumericExpressionText = "A+1";
            repeatBody.SetInputBinding("A", VmSubflowData.InputProxyId, VmSubflowLoopS111.Index);
            var repeat = CreateFlowTool(VmToolKind.SubflowCall, "04 固定三次编号", true, null);
            try
            {
                repeat.Parameters.Subflow = new VmSubflowData
                {
                    LoopMode = VmSubflowLoopS111.Repeat, RepeatCount = 3,
                    Tools = CaptureFlowRecipeS102(new[] { repeatBody }),
                    Outputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "Sequence", DisplayName = "顺序编号", DataType = "Number", SourceToolId = repeatBody.ToolId, SourcePortName = "Number", CollectIterations = true } }
                };
                flowTools.Add(repeat);
            }
            catch { repeat.Dispose(); throw; }
            finally { repeatBody.Dispose(); }
            var sequence = CreateFlowTool(VmToolKind.NumberArrayStatistics, "05 编号数组统计", true, null);
            sequence.SetInputBinding("Array", repeat.ToolId, "Sequence$Items"); flowTools.Add(sequence);
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false;
            return true;
        }
    }
}
