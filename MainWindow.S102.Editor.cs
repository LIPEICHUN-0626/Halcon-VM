using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class SubflowEditorFrameS102
        {
            public VmToolInstance Call;
            public List<VmToolInstance> ParentTools;
            public VmSubflowData Definition;
            public string DefinitionFingerprintS137;
            public FlowEditSnapshot Before;
            public List<FlowEditHistoryEntry> Undo;
            public List<FlowEditHistoryEntry> Redo;
            public bool ManualLayout;
            public double Zoom, HorizontalOffset, VerticalOffset;
        }

        private readonly Stack<SubflowEditorFrameS102> subflowEditorsS102 = new Stack<SubflowEditorFrameS102>();

        private sealed class ScopedToolReferenceS102
        {
            public string FlowId;
            public ToolFlowRecipeItem Tool;
            public List<string> IdPath;
            public string DisplayPath;
        }

        private List<ScopedToolReferenceS102> CaptureScopedToolReferencesS102()
        {
            var result = new List<ScopedToolReferenceS102>();
            if (projectFlowsS104.Count == 0)
                CollectScopedToolReferencesS102(CaptureRootFlowRecipeS102(), new List<string>(), "流程 01", result);
            else foreach (var flow in CaptureProjectFlowsS104())
            {
                var references = new List<ScopedToolReferenceS102>();
                CollectScopedToolReferencesS102(flow.Tools, new List<string>(), flow.Name, references);
                foreach (var reference in references) reference.FlowId = flow.FlowId;
                result.AddRange(references);
            }
            foreach (var deleted in deletedProjectFlowsS104)
            {
                var references = new List<ScopedToolReferenceS102>();
                CollectScopedToolReferencesS102(deleted.Flow.Tools, new List<string>(), "已删除待恢复 / " + deleted.Flow.Name, references);
                foreach (var reference in references) reference.FlowId = deleted.Flow.FlowId;
                result.AddRange(references);
            }
            return result;
        }

        private static void CollectScopedToolReferencesS102(IEnumerable<ToolFlowRecipeItem> tools, List<string> ancestors, string label, List<ScopedToolReferenceS102> result)
        {
            if (ancestors.Count > 8) throw new InvalidOperationException("子流程引用超过允许层数。");
            foreach (var tool in tools ?? Enumerable.Empty<ToolFlowRecipeItem>())
            {
                if (tool == null) continue;
                var path = ancestors.Concat(new[] { tool.ToolId }).ToList();
                string display = label + " / " + tool.InstanceName;
                result.Add(new ScopedToolReferenceS102 { Tool = tool, IdPath = path, DisplayPath = display });
                if (tool.Parameters != null && tool.Parameters.Subflow != null)
                    CollectScopedToolReferencesS102(tool.Parameters.Subflow.Tools, path, display, result);
            }
        }

        private static bool UsesRoiReferenceS102(ToolFlowRecipeItem tool, string roiId)
        {
            return (tool.RoiIds != null && tool.RoiIds.Contains(roiId, StringComparer.OrdinalIgnoreCase)) ||
                (tool.Parameters != null && string.Equals(tool.Parameters.ShapeTrainingRoiId, roiId, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanDeleteSharedRoisS102(IEnumerable<string> roiIds)
        {
            if (!CanNavigateInputRepairS96() || !ConfirmDockDraftChanges("删除 ROI")) return false;
            var ids = roiIds.ToList();
            var scope = subflowEditorsS102.Reverse().Select(frame => frame.Call.ToolId).ToList();
            var conflicts = CaptureScopedToolReferencesS102().Where(item => ids.Any(id => UsesRoiReferenceS102(item.Tool, id)) &&
                (item.FlowId != activeProjectFlowIdS104 || item.IdPath.Count != scope.Count + 1 || !item.IdPath.Take(scope.Count).SequenceEqual(scope, StringComparer.OrdinalIgnoreCase) ||
                 (item.Tool.Parameters != null && ids.Contains(item.Tool.Parameters.ShapeTrainingRoiId, StringComparer.OrdinalIgnoreCase))))
                .Select(item => item.DisplayPath).ToList();
            if (conflicts.Count == 0) return true;
            HeaderStatusText.Text = "ROI 仍被其他层级或模板训练引用，删除已取消；请先解除对应引用。";
            MessageBox.Show(this, HeaderStatusText.Text + "\n\n" + string.Join("\n", conflicts.Take(12)) +
                (conflicts.Count > 12 ? "\n另有 " + (conflicts.Count - 12) + " 项引用。" : string.Empty), "共享 ROI 正在使用", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private void InvalidateScopedDependenciesS102(Func<ToolFlowRecipeItem, bool> uses, string reason)
        {
            var affected = CaptureScopedToolReferencesS102().Where(item => item.FlowId == activeProjectFlowIdS104 && uses(item.Tool)).ToList();
            if (affected.Count == 0) return;
            var frames = subflowEditorsS102.Reverse().ToList();
            for (int depth = 0; depth <= frames.Count; depth++)
            {
                var scope = frames.Take(depth).Select(frame => frame.Call.ToolId).ToList();
                var liveTools = depth == frames.Count ? flowTools.ToList() : frames[depth].ParentTools;
                int first = liveTools.FindIndex(tool => affected.Any(item => item.IdPath.Count > depth &&
                    item.IdPath.Take(depth).SequenceEqual(scope, StringComparer.OrdinalIgnoreCase) && item.IdPath[depth] == tool.ToolId));
                if (first < 0) continue;
                // Linear execution is authoritative: invalidate this scope from its affected call/tool onward.
                foreach (var tool in liveTools.Skip(first)) InvalidateToolRunResult(tool, reason);
            }
        }

        private List<ToolFlowRecipeItem> CaptureRootFlowRecipeS102()
        {
            var children = CaptureFlowRecipe().Where(item => item.ToolType != VmToolKind.SubflowInput.ToString()).ToList();
            foreach (var frame in subflowEditorsS102)
            {
                var parent = CaptureFlowRecipeS102(frame.ParentTools);
                var call = parent.First(item => item.ToolId == frame.Call.ToolId);
                call.Parameters.Subflow = frame.Definition.Clone();
                call.Parameters.Subflow.Tools = children;
                call.InputBindings = call.InputBindings.Where(binding => binding.TargetPortName == "$RunCondition" ||
                    frame.Definition.Inputs.Any(port => port.PortName == binding.TargetPortName)).ToList();
                children = parent.Where(item => item.ToolType != VmToolKind.SubflowInput.ToString()).ToList();
            }
            return children;
        }

        private void RefreshSubflowEditorChromeS102()
        {
            if (SubflowBackButtonS102 == null) return;
            bool inside = subflowEditorsS102.Count > 0;
            SubflowBackButtonS102.Visibility = inside ? Visibility.Visible : Visibility.Collapsed;
            SubflowBoundaryButtonS102.Visibility = inside ? Visibility.Visible : Visibility.Collapsed;
            string path = GetProjectFlowNameS104() + (inside ? " / " + string.Join(" / ", subflowEditorsS102.Reverse().Select(frame => frame.Call.InstanceName)) : string.Empty);
            FlowScopeNameS102.Text = path; FlowScopeNameS102.ToolTip = path;
            SubflowBackButtonS102.ToolTip = inside ? "保存当前内部修改并返回上一级流程；主流程中的连接保持不变。" : string.Empty;
        }

        private void SwitchEditorToolsS102(IEnumerable<VmToolInstance> tools, bool manualLayout)
        {
            bool previousTracking = recipeTrackingSuspended;
            bool previousHistory = flowEditHistorySuspended;
            recipeTrackingSuspended = true; flowEditHistorySuspended = true;
            try
            {
                ClearSubflowTraceS102();
                ClearMeasurementDomainContext(); ClearShapeTrainingPreviewS89(); DisposeToolOverlays();
                dockEditingTool = null; dockInputPortRows.Clear(); dockRoiBindingRows.Clear();
                selectedFlowConnectionKey = null; flowPortDragSession = null; flowNodeDragSession = null;
                flowTools.Clear();
                foreach (var tool in tools) flowTools.Add(tool);
                isManualFlowLayout = manualLayout;
                FlowToolList.SelectedItem = flowTools.FirstOrDefault(tool => tool.Kind != VmToolKind.SubflowInput);
                ResetFlowDebugRuntimeState(true); RefreshFlowGraphLayout(); RefreshUiState();
                RefreshSubflowEditorChromeS102(); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
            }
            finally { recipeTrackingSuspended = previousTracking; flowEditHistorySuspended = previousHistory; }
        }

        private void EnterSubflowEditorS102(VmToolInstance call)
        {
            if (!CanNavigateInputRepairS96() || call == null || call.Kind != VmToolKind.SubflowCall || !flowTools.Contains(call)) return;
            if (subflowEditorsS102.Count >= 8) throw new InvalidOperationException("内部编辑最多进入 8 层。");
            if (!CloseCentralModuleConfigurationS77()) return;
            var definition = call.Parameters.Subflow;
            string error = definition == null ? "子流程内容缺失。" : definition.Validate();
            if (error != null) throw new InvalidOperationException(error);
            StopPlayback();
            var frame = new SubflowEditorFrameS102
            {
                Call = call, ParentTools = flowTools.ToList(), Definition = definition.Clone(), Before = CaptureFlowEditSnapshot(),
                Undo = flowUndoHistory.ToList(), Redo = flowRedoHistory.ToList(), ManualLayout = isManualFlowLayout,
                Zoom = FlowZoomSlider.Value, HorizontalOffset = FlowGraphViewport.HorizontalOffset, VerticalOffset = FlowGraphViewport.VerticalOffset
            };
            var children = new List<VmToolInstance>();
            try
            {
                var proxy = new VmToolInstance { ToolId = VmSubflowData.InputProxyId, Kind = VmToolKind.SubflowInput, InstanceName = "调用输入 · 不参与执行", IsEnabled = true };
                proxy.Parameters.Subflow = new VmSubflowData { Outputs = VmSubflowLoopS111.ProxyPorts(definition) };
                children.Add(proxy);
                foreach (var item in definition.Tools)
                {
                    VmToolKind kind;
                    if (!Enum.TryParse(item.ToolType, out kind) || kind == VmToolKind.SubflowInput) throw new InvalidOperationException("内部模块类型无效。");
                    var child = CreateFlowTool(kind, item.InstanceName, item.IsEnabled, item.ToolId);
                    child.CoordinateIdentityBase = GetCoordinateIdentityBaseS102(call) + "/" + item.ToolId;
                    children.Add(child); ApplyToolRecipeData(child, item);
                }
            }
            catch { foreach (var child in children) child.Dispose(); throw; }
            subflowEditorsS102.Push(frame);
            SwitchEditorToolsS102(children, children.Any(tool => tool.HasManualGraphPosition));
            RefreshSubflowEditorInputsS102(); ResetFlowEditHistory();
            var initialDefinition = frame.Definition.Clone();
            initialDefinition.Tools = CaptureFlowRecipe().Where(item => item.ToolType != VmToolKind.SubflowInput.ToString()).ToList();
            frame.DefinitionFingerprintS137 = GetSubflowDefinitionFingerprintS137(initialDefinition);
            FitFlowButton_Click(null, null);
            HeaderStatusText.Text = "正在编辑 " + call.InstanceName + "；双击内部模块配置，完成后点击返回上级。保存始终保存完整方案。";
        }

        private void RefreshSubflowEditorInputsS102()
        {
            if (subflowEditorsS102.Count == 0) return;
            var frame = subflowEditorsS102.Peek();
            var proxy = flowTools.FirstOrDefault(tool => tool.Kind == VmToolKind.SubflowInput);
            if (proxy == null) return;
            foreach (var child in flowTools.Where(tool => tool.Kind != VmToolKind.SubflowInput))
                child.CoordinateIdentityBase = GetCoordinateIdentityBaseS102(frame.Call) + "/" + child.ToolId;
            proxy.ClearRuntimeOutputs(); proxy.IsEnabled = true; proxy.ResultCode = "OK"; proxy.ErrorMessage = string.Empty;
            int ready = 0;
            foreach (var port in frame.Definition.Inputs)
            {
                var binding = frame.Call.GetInputBinding(port.PortName);
                var producer = binding == null ? null : frame.ParentTools.FirstOrDefault(tool => tool.ToolId == binding.SourceToolId);
                object raw;
                if (producer == null || (producer.ResultCode != "OK" && producer.ResultCode != "NG") || !string.IsNullOrWhiteSpace(producer.ErrorMessage) ||
                    !producer.TryGetOutputValue(binding.SourcePortName, out raw) || raw == null || IsDisposedRuntimeValueS69(raw)) continue;
                RequireSubflowValueTypeS102(port.DataType, raw);
                proxy.SetOutputValue(port.PortName, CopySubflowValueS102(raw, proxy, port.PortName)); ready++;
                if (port.DataType == "Image" || port.DataType == "Region" || port.DataType == "XLD")
                    proxy.SetOutputCoordinateContext(port.PortName, ResolveOutputCoordinateContextS102(producer, binding.SourcePortName));
            }
            RefreshLoopEditorPreviewS111(frame, proxy);
            proxy.RunStatus = "边界来源"; proxy.OutputSummary = ready + "/" + frame.Definition.Inputs.Count + " 项已有父流程结果";
            proxy.ConfigurationStatus = "只读调用边界";
            RefreshFlowPortVisualization();
        }

        private string GetSubflowDefinitionFingerprintS137(VmSubflowData definition)
        {
            return recipeService.GetRecipeFingerprint(new VisionRecipe
            {
                ToolFlow = new List<ToolFlowRecipeItem>
                {
                    new ToolFlowRecipeItem { ToolId = "$EditorDefinition", Parameters = new VmToolParameterData { Subflow = definition } }
                }
            });
        }

        private void ReturnSubflowEditorS102_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("返回上级流程", delegate
            {
                if (subflowEditorsS102.Count == 0 || !CanNavigateInputRepairS96() || !CloseCentralModuleConfigurationS77()) return;
                var frame = subflowEditorsS102.Peek();
                var definition = frame.Definition.Clone();
                definition.Tools = CaptureFlowRecipe().Where(item => item.ToolType != VmToolKind.SubflowInput.ToString()).ToList();
                bool definitionChangedS137 = !string.Equals(frame.DefinitionFingerprintS137,
                    GetSubflowDefinitionFingerprintS137(definition), StringComparison.Ordinal);
                long editLeaseS137 = definitionChangedS137 ? CaptureEngineeringEditLeaseS137("保留子流程内部修改并返回") : -1;
                if (definitionChangedS137 && editLeaseS137 < 0)
                {
                    HeaderStatusText.Text = "子流程内部修改仍保留，未替换父调用定义。请从“工位工程保护”解锁后再次返回；未修改的只读导航无需解锁。";
                    return;
                }
                string error = definition.Validate();
                if (error != null) throw new InvalidOperationException(error + " 请先修正返回值来源或撤销对应修改，内部内容不会丢弃。");
                foreach (var port in definition.Outputs)
                {
                    var producer = flowTools.FirstOrDefault(tool => tool.ToolId == port.SourceToolId);
                    if (producer == null || !ToolMetadata.GetInstanceOutputPorts(producer).Any(item => item.PortName == port.SourcePortName && item.DataType == port.DataType))
                        throw new InvalidOperationException("返回值来源失效：" + port.DisplayName + "。请从返回值入口重选，当前编辑保持不变。");
                }
                if (definitionChangedS137)
                {
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                    {
                        HeaderStatusText.Text = "工程授权已改变，子流程内部修改仍保留且未替换父调用定义；解锁后再次返回。";
                        return;
                    }
                    frame.Call.Parameters.Subflow = definition;
                    frame.Call.ReplaceInputBindings(frame.Call.InputBindings.Where(binding => binding.TargetPortName == "$RunCondition" ||
                        definition.Inputs.Any(port => port.PortName == binding.TargetPortName)).Select(binding => binding.Clone()).ToList());
                }
                var children = flowTools.ToList();
                subflowEditorsS102.Pop();
                SwitchEditorToolsS102(frame.ParentTools, frame.ManualLayout);
                foreach (var child in children) { DisposeShapeToolResource(child.ToolId); child.Dispose(); }
                flowUndoHistory.Clear(); flowUndoHistory.AddRange(frame.Undo);
                flowRedoHistory.Clear(); flowRedoHistory.AddRange(frame.Redo);
                FlowToolList.SelectedItem = frame.Call;
                FlowZoomSlider.Value = frame.Zoom;
                FlowGraphViewport.ScrollToHorizontalOffset(frame.HorizontalOffset); FlowGraphViewport.ScrollToVerticalOffset(frame.VerticalOffset);
                RefreshSubflowEditorInputsS102();
                bool changed = !string.Equals(frame.Before.Fingerprint, CaptureFlowEditSnapshot().Fingerprint, StringComparison.Ordinal);
                if (changed)
                {
                    InvalidateToolRunResult(frame.Call, "子流程内部已修改"); InvalidateDownstreamResults(frame.Call, "子流程内部已修改");
                    CommitFlowEditHistory("编辑子流程：" + frame.Call.InstanceName, frame.Before);
                }
                RefreshFlowEditHistoryState(); ScheduleRecipeStateCheck();
                HeaderStatusText.Text = "已返回 " + (subflowEditorsS102.Count == 0 ? "主流程" : subflowEditorsS102.Peek().Call.InstanceName) +
                    (changed ? "；内部修改已保留，一次撤销可恢复进入前状态。" : "；内部配置未变化，保留父流程已有结果。" );
            });
        }

        private void EditSubflowReturnSourcesS102_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("编辑子流程接口");
            if (editLeaseS137 < 0) return;
            if (subflowEditorsS102.Count == 0 || !CanNavigateInputRepairS96()) return;
            if (!CloseCentralModuleConfigurationS77()) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            var frame = subflowEditorsS102.Peek();
            var draft = frame.Definition.Clone();
            StackPanel content; Button apply;
            var window = CreateBatchEditWindowS99("子流程接口 · 输入 / 返回", out content, out apply);
            var tabs = new TabControl(); content.Children.Add(tabs);
            var inputPanel = new StackPanel(); var outputPanel = new StackPanel();
            tabs.Items.Add(new TabItem { Header = "输入", Content = inputPanel });
            tabs.Items.Add(new TabItem { Header = "返回", Content = outputPanel });
            Action<bool> render = null;
            render = inputDirection =>
            {
                var panel = inputDirection ? inputPanel : outputPanel;
                var ports = inputDirection ? draft.Inputs : draft.Outputs;
                panel.Children.Clear();
                var addBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,8,0,8) };
                var newType = new ComboBox { Width = 125 };
                ConfigureFlowValueTypePickerS103(newType, true, "Image");
                var add = new Button { Content = inputDirection ? "添加输入" : "添加返回", Margin = new Thickness(8,0,0,0), MinWidth = 90,
                    ToolTip = inputDirection ? "添加后，内部模块可订阅该输入；返回上级后连接它的来源。" : "把内部模块的结果公开给上一级流程。" };
                addBar.Children.Add(newType); addBar.Children.Add(add); panel.Children.Add(addBar);
                add.Click += delegate
                {
                    string label = inputDirection ? "输入" : "返回"; int index = 1;
                    while (ports.Any(port => string.Equals(port.DisplayName, label + index, StringComparison.OrdinalIgnoreCase))) index++;
                    ports.Add(new VmSubflowPortData { PortName = (inputDirection ? "In_" : "Out_") + Guid.NewGuid().ToString("N"),
                        DisplayName = label + index, DataType = (string)newType.SelectedValue });
                    render(inputDirection);
                };
                foreach (var port in ports.ToList())
                {
                    var row = new StackPanel { Margin = new Thickness(0,0,0,10) };
                    var heading = new DockPanel();
                    var remove = new Button { Content = "移除", MinWidth = 48, Margin = new Thickness(8,0,0,0), ToolTip = "只允许移除没有消费者的接口；已连接接口请先改接来源。" };
                    DockPanel.SetDock(remove, Dock.Right); heading.Children.Add(remove);
                    var name = new TextBox { Text = port.DisplayName, MaxLength = 64, ToolTip = port.DataType + " · 改名不会改变连接或端口标识。" };
                    name.TextChanged += delegate { port.DisplayName = name.Text.Trim(); };
                    heading.Children.Add(name); row.Children.Add(heading);
                    remove.Click += delegate
                    {
                        string usage = GetSubflowBoundaryUsageS102(frame, port, inputDirection);
                        if (usage != null) { MessageBox.Show(window, "接口仍被使用：" + usage + "。请先在对应输入栏改接或清除连接。"); return; }
                        if (!inputDirection && ports.Count == 1) { MessageBox.Show(window, "子流程至少保留一个返回值。可先添加新的返回，再移除旧项。"); return; }
                        ports.Remove(port); render(inputDirection);
                    };
                    var context = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = System.Windows.Media.Brushes.SlateGray, Margin = new Thickness(0,3,0,2) };
                    row.Children.Add(context);
                    if (inputDirection)
                    {
                        var binding = frame.Call.GetInputBinding(port.PortName);
                        var source = binding == null ? null : frame.ParentTools.FirstOrDefault(tool => tool.ToolId == binding.SourceToolId);
                        context.Text = port.DataType + " · " + (source == null ? "待连接：返回上级，在调用模块的输入页选择来源。" : source.InstanceName + " · " + binding.SourcePortName);
                    }
                    else
                    {
                        if (port.DataType == "Number" || port.DataType == "String")
                        {
                            var collect = new CheckBox { Content = "同时收集每次返回为数组", IsChecked = port.CollectIterations,
                                ToolTip = "普通返回保留最后一次；额外发布同名 $Items 数组。空循环只发布空数组，不发布普通返回。" };
                            collect.Checked += delegate { port.CollectIterations = true; };
                            collect.Unchecked += delegate { port.CollectIterations = false; };
                            row.Children.Add(collect);
                        }
                        var options = flowTools.Where(tool => tool.Kind != VmToolKind.SubflowInput)
                            .SelectMany(tool => ToolMetadata.GetInstanceOutputPorts(tool).Where(output => output.DataType == port.DataType)
                            .Select(output => new VmInputSourceOption { Key = tool.ToolId + "|" + output.PortName, DisplayText = tool.InstanceName + " · " + output.DisplayName, SourceToolId = tool.ToolId, SourcePortName = output.PortName })).ToList();
                        var picker = new ComboBox { ItemsSource = options, DisplayMemberPath = "DisplayText", SelectedValuePath = "Key", SelectedValue = port.SourceToolId + "|" + port.SourcePortName };
                        Action refresh = delegate
                        {
                            var selected = picker.SelectedItem as VmInputSourceOption;
                            port.SourceToolId = selected == null ? null : selected.SourceToolId;
                            port.SourcePortName = selected == null ? null : selected.SourcePortName;
                            string identity = selected == null || (port.DataType != "Image" && port.DataType != "Region" && port.DataType != "XLD" && !VmFlowValueContractS103.IsGeometryType(port.DataType)) ? null :
                                ResolveOutputCoordinateContextS102(flowTools.First(tool => tool.ToolId == selected.SourceToolId), selected.SourcePortName);
                            context.Text = port.DataType + " · " + (selected == null ? "请选择内部来源" : identity == null ? "返回所选模块的本次结果" :
                                IsPendingCoordinateContextS102(identity) ? "图像链待确认：运行上游后显示真实来源。" : "坐标来源：" + identity);
                        };
                        picker.SelectionChanged += delegate { refresh(); }; refresh(); row.Children.Add(picker);
                    }
                    panel.Children.Add(row);
                }
            };
            render(true); render(false);
            apply.Content = "应用接口配置";
            apply.Click += delegate
            {
                if (draft.Outputs.Any(port => string.IsNullOrWhiteSpace(port.SourceToolId))) { MessageBox.Show(window, "请为每个返回值选择兼容的内部来源。"); return; }
                foreach (var old in frame.Definition.Outputs.Where(port => port.CollectIterations))
                {
                    var next = draft.Outputs.FirstOrDefault(port => port.PortName == old.PortName);
                    if (next != null && next.CollectIterations) continue;
                    string usage = GetSubflowBoundaryUsageS102(frame,
                        new VmSubflowPortData { PortName = VmSubflowLoopS111.CollectionPort(old) }, false);
                    if (usage != null) { MessageBox.Show(window, "收集数组仍被使用：" + usage + "。请先解除连接。"); return; }
                }
                var preview = frame.Definition.Clone();
                preview.Inputs = draft.Inputs.Select(port => port.Clone()).ToList();
                preview.Outputs = draft.Outputs.Select(port => port.Clone()).ToList();
                string loopError = VmSubflowLoopS111.Validate(preview);
                if (loopError != null) { MessageBox.Show(window, loopError); return; }
                var inputLabels = draft.Inputs.Select(port => port.DisplayName).ToList();
                var outputLabels = draft.Outputs.Select(port => port.DisplayName).ToList();
                if (inputLabels.Concat(outputLabels).Any(label => string.IsNullOrWhiteSpace(label) || label.Length > 64 || label.Any(char.IsControl)) ||
                    inputLabels.Distinct(StringComparer.OrdinalIgnoreCase).Count() != inputLabels.Count || outputLabels.Distinct(StringComparer.OrdinalIgnoreCase).Count() != outputLabels.Count)
                { MessageBox.Show(window, "名称不能为空、不能包含控制字符，最多 64 字；同方向接口名称不能重复。"); return; }
                var before = CaptureFlowEditSnapshot();
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                frame.Definition.Inputs = draft.Inputs.Select(port => port.Clone()).ToList();
                frame.Definition.Outputs = draft.Outputs.Select(port => port.Clone()).ToList();
                var proxy = flowTools.FirstOrDefault(tool => tool.Kind == VmToolKind.SubflowInput);
                if (proxy != null) proxy.Parameters.Subflow.Outputs = VmSubflowLoopS111.ProxyPorts(frame.Definition);
                RefreshSubflowEditorInputsS102();
                CommitFlowEditHistory("修改子流程输入与返回接口", before);
                ScheduleRecipeStateCheck(); window.DialogResult = true;
            };
            window.ShowDialog();
        }

        private string GetSubflowBoundaryUsageS102(SubflowEditorFrameS102 frame, VmSubflowPortData port, bool inputDirection)
        {
            string sourceId = inputDirection ? VmSubflowData.InputProxyId : frame.Call.ToolId;
            var consumers = (inputDirection ? flowTools.AsEnumerable() : frame.ParentTools.AsEnumerable())
                .Where(tool => tool.InputBindings.Any(binding => binding.SourceToolId == sourceId && binding.SourcePortName == port.PortName) ||
                    (tool.Kind == VmToolKind.NumericJudge && tool.InputToolId == sourceId && tool.InputPortName == port.PortName))
                .Select(tool => tool.InstanceName).ToList();
            if (!inputDirection)
            {
                if (port.CollectIterations)
                    consumers.AddRange(frame.ParentTools.Where(tool => tool.InputBindings.Any(binding =>
                            binding.SourceToolId == sourceId && binding.SourcePortName == VmSubflowLoopS111.CollectionPort(port)) ||
                            tool.Kind == VmToolKind.NumericJudge && tool.InputToolId == sourceId && tool.InputPortName == VmSubflowLoopS111.CollectionPort(port))
                        .Select(tool => tool.InstanceName));
                var parentFrame = subflowEditorsS102.Skip(1).FirstOrDefault();
                if (parentFrame != null) consumers.AddRange(parentFrame.Definition.Outputs.Where(output => output.SourceToolId == sourceId &&
                    (output.SourcePortName == port.PortName || port.CollectIterations && output.SourcePortName == VmSubflowLoopS111.CollectionPort(port)))
                    .Select(output => "上一级返回 · " + output.DisplayName));
            }
            return consumers.Count == 0 ? null : string.Join("、", consumers.Distinct());
        }

        private void ClearSubflowEditorsS102()
        {
            ClearSubflowTraceS102();
            foreach (var frame in subflowEditorsS102)
                foreach (var tool in frame.ParentTools) { DisposeShapeToolResource(tool.ToolId); tool.Dispose(); }
            subflowEditorsS102.Clear(); RefreshSubflowEditorChromeS102();
        }
    }
}
