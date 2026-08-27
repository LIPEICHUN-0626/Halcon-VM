using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private sealed class VmFlowSnippetListItemS70
        {
            public VmFlowSnippetData Data { get; set; }
            public string Name { get; set; }
            public string ToolSummary { get; set; }
            public string ContractSummary { get; set; }
            public string UpdatedText { get; set; }
        }

        private readonly ObservableCollection<VmFlowSnippetListItemS70> flowSnippetItemsS70 = new ObservableCollection<VmFlowSnippetListItemS70>();
        private VmFlowSnippetLibraryData flowSnippetLibraryS70 = new VmFlowSnippetLibraryData();
        private string pendingDeleteSnippetIdS70;
        private Action<IList<VmToolInstance>, IDictionary<string, string>> flowPastePostProcessS70;
        private string flowPasteHistoryDescriptionS70;
        private int lastSnippetAutoBoundCountS70;
        private int lastSnippetUnresolvedCountS70;

        private void OpenFlowSnippetLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlowMoreToggleButton != null) FlowMoreToggleButton.IsChecked = false;
            if (FlowConnectionInspectorBorder != null) FlowConnectionInspectorBorder.Visibility = Visibility.Collapsed;
            if (FlowPortInspectorBorder != null) FlowPortInspectorBorder.Visibility = Visibility.Collapsed;
            if (FlowConnectionIssuePanel != null) FlowConnectionIssuePanel.Visibility = Visibility.Collapsed;
            LoadFlowSnippetLibraryS70(true);
            FlowSnippetLibraryPanel.Visibility = Visibility.Visible;
            RefreshFlowSnippetAnchorS70();
            HeaderStatusText.Text = "已打开本机流程片段库；运行态对象、结果、错误、耗时和调试指针不会进入片段。";
        }

        private void CloseFlowSnippetLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            FlowSnippetLibraryPanel.Visibility = Visibility.Collapsed;
            pendingDeleteSnippetIdS70 = null;
            HeaderStatusText.Text = "已关闭流程片段库；当前流程未被修改。";
        }

        private void LoadFlowSnippetLibraryS70(bool preserveSelection)
        {
            string selectedId = preserveSelection && GetSelectedFlowSnippetS70() != null ? GetSelectedFlowSnippetS70().SnippetId : null;
            string error;
            flowSnippetLibraryS70 = recipeService.LoadFlowSnippets(out error) ?? new VmFlowSnippetLibraryData();
            flowSnippetLibraryS70.Normalize();
            RefreshFlowSnippetItemsS70(selectedId);
            if (!string.IsNullOrWhiteSpace(error))
            {
                FlowSnippetStatusText.Text = "片段库读取失败，原文件未覆盖：" + error;
                HeaderStatusText.Text = FlowSnippetStatusText.Text;
                LogInfo(FlowSnippetStatusText.Text);
            }
        }

        private void RefreshFlowSnippetItemsS70(string selectedId)
        {
            flowSnippetItemsS70.Clear();
            foreach (VmFlowSnippetData snippet in flowSnippetLibraryS70.Snippets.OrderByDescending(item => item.UpdatedAtUtc, StringComparer.Ordinal))
            {
                int internalCount = CountSnippetInternalConnectionsS70(snippet);
                int required = snippet.ExternalInputs.Count(item => item.IsRequired);
                int optional = snippet.ExternalInputs.Count - required;
                DateTime updated;
                string updatedText = DateTime.TryParse(snippet.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out updated)
                    ? updated.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
                    : snippet.UpdatedAtUtc;
                flowSnippetItemsS70.Add(new VmFlowSnippetListItemS70
                {
                    Data = snippet,
                    Name = snippet.Name,
                    ToolSummary = snippet.ToolFlow.Count.ToString(CultureInfo.InvariantCulture) + " 个模块 · " + internalCount.ToString(CultureInfo.InvariantCulture) + " 条内部连接",
                    ContractSummary = required.ToString(CultureInfo.InvariantCulture) + " 必需外部输入" + (optional == 0 ? string.Empty : " · " + optional.ToString(CultureInfo.InvariantCulture) + " 可选"),
                    UpdatedText = updatedText
                });
            }
            FlowSnippetLibraryListBox.ItemsSource = flowSnippetItemsS70;
            FlowSnippetLibrarySummaryText.Text = "本机片段 " + flowSnippetItemsS70.Count.ToString(CultureInfo.InvariantCulture) + " 个 · 仅确定性线性流程";
            FlowSnippetLibraryEmptyText.Visibility = flowSnippetItemsS70.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FlowSnippetLibraryListBox.Visibility = flowSnippetItemsS70.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            VmFlowSnippetListItemS70 selected = flowSnippetItemsS70.FirstOrDefault(item => string.Equals(item.Data.SnippetId, selectedId, StringComparison.OrdinalIgnoreCase)) ?? flowSnippetItemsS70.FirstOrDefault();
            FlowSnippetLibraryListBox.SelectedItem = selected;
            RefreshSelectedFlowSnippetS70();
            RefreshToolboxSnippetItemsS71();
        }

        private static int CountSnippetInternalConnectionsS70(VmFlowSnippetData snippet)
        {
            HashSet<string> ids = new HashSet<string>(snippet.ToolFlow.Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
            int count = snippet.ToolFlow.Sum(item => (item.InputBindings ?? new List<VmToolInputBindingData>()).Count(binding => ids.Contains(binding.SourceToolId ?? string.Empty)));
            count += snippet.ToolFlow.Count(item => item.NumericJudge != null && ids.Contains(item.NumericJudge.InputToolId ?? string.Empty) &&
                                                  !(item.InputBindings ?? new List<VmToolInputBindingData>()).Any(binding => string.Equals(binding.TargetPortName, "Value", StringComparison.OrdinalIgnoreCase)));
            return count;
        }

        private VmFlowSnippetData GetSelectedFlowSnippetS70()
        {
            VmFlowSnippetListItemS70 item = FlowSnippetLibraryListBox == null ? null : FlowSnippetLibraryListBox.SelectedItem as VmFlowSnippetListItemS70;
            return item == null ? null : item.Data;
        }

        private void FlowSnippetLibraryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pendingDeleteSnippetIdS70 = null;
            RefreshSelectedFlowSnippetS70();
        }

        private void RefreshSelectedFlowSnippetS70()
        {
            VmFlowSnippetData snippet = GetSelectedFlowSnippetS70();
            bool selected = snippet != null;
            FlowSnippetSelectedNameText.Text = selected ? snippet.Name : "--";
            FlowSnippetToolOrderText.Text = selected ? string.Join(" → ", snippet.ToolFlow.Select(item => item.InstanceName)) : "--";
            FlowSnippetInternalConnectionsText.Text = selected ? CountSnippetInternalConnectionsS70(snippet).ToString(CultureInfo.InvariantCulture) + " 条（插入时按新 ToolId 重映射）" : "--";
            FlowSnippetExternalInputsText.Text = selected ? BuildSnippetExternalInputsTextS70(snippet) : "--";
            InsertFlowSnippetBeforeButton.IsEnabled = selected;
            InsertFlowSnippetAfterButton.IsEnabled = selected;
            RenameFlowSnippetButton.IsEnabled = selected;
            DeleteFlowSnippetButton.IsEnabled = selected;
            DeleteFlowSnippetButton.Content = "删除";
            if (selected) FlowSnippetNameTextBox.Text = snippet.Name;
            RefreshFlowSnippetAnchorS70();
        }

        private static string BuildSnippetExternalInputsTextS70(VmFlowSnippetData snippet)
        {
            if (snippet.ExternalInputs.Count == 0) return "无 · 片段内部自洽";
            return string.Join("；", snippet.ExternalInputs.Select(item =>
                (item.IsRequired ? "必需 " : "可选 ") + item.TargetToolName + "." + item.TargetPortName + ":" + item.DataType +
                (string.IsNullOrWhiteSpace(item.PreferredSourceText) ? string.Empty : "（原来源 " + item.PreferredSourceText + "）")));
        }

        private void RefreshFlowSnippetAnchorS70()
        {
            if (FlowSnippetAnchorText == null) return;
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            int index = anchor == null ? -1 : flowTools.IndexOf(anchor);
            FlowSnippetAnchorText.Text = index < 0 ? "插入锚点：未选择流程模块" : "插入锚点：#" + anchor.SequenceText + " " + anchor.InstanceName;
        }

        private void SaveSelectedFlowSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            string name = (FlowSnippetNameTextBox.Text ?? string.Empty).Trim();
            if (selected.Count < 2)
            {
                SetFlowSnippetStatusS70("保存失败：请连续选择至少 2 个真实流程模块。", true);
                return;
            }
            int first = flowTools.IndexOf(selected[0]);
            if (selected.Select((item, offset) => flowTools.IndexOf(item) == first + offset).Any(value => !value))
            {
                SetFlowSnippetStatusS70("保存失败：所选模块不连续；片段必须保留清晰的确定性执行顺序。", true);
                return;
            }
            string nameError = ValidateFlowSnippetNameS70(name, null);
            if (!string.IsNullOrWhiteSpace(nameError))
            {
                SetFlowSnippetStatusS70(nameError, true);
                return;
            }

            HashSet<string> selectedIds = new HashSet<string>(selected.Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
            List<ToolFlowRecipeItem> recipeItems = CaptureFlowRecipe().Where(item => selectedIds.Contains(item.ToolId)).ToList();
            foreach (ToolFlowRecipeItem item in recipeItems)
            {
                item.GraphX = null;
                item.GraphY = null;
                item.IsBreakpoint = false;
                if (!string.IsNullOrWhiteSpace(item.GroupId) && flowTools.Count(tool => string.Equals(tool.GroupId, item.GroupId, StringComparison.OrdinalIgnoreCase)) != recipeItems.Count(other => string.Equals(other.GroupId, item.GroupId, StringComparison.OrdinalIgnoreCase)))
                {
                    item.GroupId = null;
                    item.GroupName = null;
                    item.IsGroupCollapsed = false;
                }
            }
            VmFlowSnippetData snippet = new VmFlowSnippetData
            {
                Name = name,
                UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
                ToolFlow = recipeItems,
                ExternalInputs = BuildSnippetExternalInputsS70(selected, selectedIds)
            };
            flowSnippetLibraryS70.Snippets.Add(snippet);
            SaveFlowSnippetLibraryS70(snippet.SnippetId, "已保存片段“" + name + "”：" + recipeItems.Count.ToString(CultureInfo.InvariantCulture) + " 个模块，" + CountSnippetInternalConnectionsS70(snippet).ToString(CultureInfo.InvariantCulture) + " 条内部连接，" + snippet.ExternalInputs.Count.ToString(CultureInfo.InvariantCulture) + " 个外部输入契约。", true);
        }

        private List<VmFlowSnippetExternalInputData> BuildSnippetExternalInputsS70(IList<VmToolInstance> selected, ISet<string> selectedIds)
        {
            List<VmFlowSnippetExternalInputData> result = new List<VmFlowSnippetExternalInputData>();
            foreach (VmToolInstance tool in selected)
            {
                IList<VmPortDefinition> ports = ToolMetadata.GetInstanceInputPorts(tool);
                if (tool.Kind == VmToolKind.PositionCorrection2D)
                {
                    VmPortDefinition posePort = ports.First(item => item.PortName == "Pose2D");
                    VmToolInputBindingData poseBinding = tool.GetInputBinding("Pose2D");
                    VmPortDefinition[] legacyPorts = ports.Where(item => item.PortName == "CurrentRow" || item.PortName == "CurrentColumn" || item.PortName == "CurrentAngle").ToArray();
                    bool hasLegacyBinding = legacyPorts.Any(item => tool.GetInputBinding(item.PortName) != null);
                    if (poseBinding != null)
                    {
                        if (!selectedIds.Contains(poseBinding.SourceToolId ?? string.Empty)) AddSnippetExternalInputS70(result, tool, posePort, poseBinding, true);
                    }
                    else if (hasLegacyBinding)
                    {
                        foreach (VmPortDefinition legacyPort in legacyPorts)
                        {
                            VmToolInputBindingData legacyBinding = tool.GetInputBinding(legacyPort.PortName);
                            if (legacyBinding == null || !selectedIds.Contains(legacyBinding.SourceToolId ?? string.Empty)) AddSnippetExternalInputS70(result, tool, legacyPort, legacyBinding, true);
                        }
                    }
                    else
                    {
                        AddSnippetExternalInputS70(result, tool, posePort, null, true);
                    }
                    continue;
                }
                foreach (VmPortDefinition port in ports)
                {
                    VmToolInputBindingData binding = GetEffectiveInputBindingS69(tool, port);
                    if (binding != null && selectedIds.Contains(binding.SourceToolId ?? string.Empty)) continue;
                    bool required = IsRequiredConnectionPortS69(tool, port);
                    if (binding == null && (!required || HasImplicitInputS69(tool, port))) continue;
                    if (tool.Kind == VmToolKind.PositionCorrection2D && port.PortName != "Pose2D" && binding == null) continue;
                    AddSnippetExternalInputS70(result, tool, port, binding, required);
                }
            }
            return result;
        }

        private void AddSnippetExternalInputS70(ICollection<VmFlowSnippetExternalInputData> result, VmToolInstance tool, VmPortDefinition port, VmToolInputBindingData binding, bool required)
        {
            VmToolInstance source = GetInputSourceTool(binding);
            result.Add(new VmFlowSnippetExternalInputData
            {
                TargetTemplateToolId = tool.ToolId,
                TargetToolName = tool.InstanceName,
                TargetPortName = port.PortName,
                DataType = port.DataType,
                IsRequired = required,
                PreferredSourceToolId = binding == null ? null : binding.SourceToolId,
                PreferredSourcePortName = binding == null ? null : binding.SourcePortName,
                PreferredSourceText = binding == null ? null : (source == null ? binding.SourceToolId : source.InstanceName) + "." + binding.SourcePortName
            });
        }

        private string ValidateFlowSnippetNameS70(string name, string currentId)
        {
            if (string.IsNullOrWhiteSpace(name)) return "片段名称不能为空。";
            if (name.Length > 60) return "片段名称不能超过 60 个字符。";
            if (flowSnippetLibraryS70.Snippets.Any(item => !string.Equals(item.SnippetId, currentId, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
                return "片段名称已存在，请使用唯一名称。";
            return string.Empty;
        }

        private void RenameFlowSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowSnippetData snippet = GetSelectedFlowSnippetS70();
            if (snippet == null) return;
            string name = (FlowSnippetNameTextBox.Text ?? string.Empty).Trim();
            string error = ValidateFlowSnippetNameS70(name, snippet.SnippetId);
            if (!string.IsNullOrWhiteSpace(error)) { SetFlowSnippetStatusS70(error, true); return; }
            snippet.Name = name;
            snippet.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            SaveFlowSnippetLibraryS70(snippet.SnippetId, "已将本机片段改名为“" + name + "”。", true);
        }

        private void DeleteFlowSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowSnippetData snippet = GetSelectedFlowSnippetS70();
            if (snippet == null) return;
            if (!string.Equals(pendingDeleteSnippetIdS70, snippet.SnippetId, StringComparison.OrdinalIgnoreCase))
            {
                pendingDeleteSnippetIdS70 = snippet.SnippetId;
                DeleteFlowSnippetButton.Content = "确认删除";
                SetFlowSnippetStatusS70("再次点击“确认删除”将移除本机片段“" + snippet.Name + "”；当前流程不受影响。", true);
                return;
            }
            flowSnippetLibraryS70.Snippets.Remove(snippet);
            pendingDeleteSnippetIdS70 = null;
            SaveFlowSnippetLibraryS70(null, "已删除本机片段“" + snippet.Name + "”；当前流程未被修改。", true);
        }

        private void SaveFlowSnippetLibraryS70(string selectedId, string message, bool log)
        {
            try
            {
                flowSnippetLibraryS70.Normalize();
                recipeService.SaveFlowSnippets(flowSnippetLibraryS70);
                RefreshFlowSnippetItemsS70(selectedId);
                SetFlowSnippetStatusS70(message, false);
                if (log) LogInfo(message);
            }
            catch (Exception ex)
            {
                SetFlowSnippetStatusS70("片段库保存失败：" + ex.Message, true);
            }
        }

        private void SetFlowSnippetStatusS70(string message, bool error)
        {
            FlowSnippetStatusText.Text = message;
            FlowSnippetStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(error ? (byte)185 : (byte)3, error ? (byte)28 : (byte)105, error ? (byte)28 : (byte)161));
            HeaderStatusText.Text = message;
        }

        private void InsertFlowSnippetBeforeButton_Click(object sender, RoutedEventArgs e) { InsertSelectedFlowSnippetS70(false); }
        private void InsertFlowSnippetAfterButton_Click(object sender, RoutedEventArgs e) { InsertSelectedFlowSnippetS70(true); }

        private void InsertSelectedFlowSnippetS70(bool after)
        {
            VmFlowSnippetData snippet = GetSelectedFlowSnippetS70();
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            int anchorIndex = anchor == null ? -1 : flowTools.IndexOf(anchor);
            if (snippet == null) { SetFlowSnippetStatusS70("请先选择一个流程片段。", true); return; }
            if (anchorIndex < 0) { SetFlowSnippetStatusS70("请先在流程中选择插入锚点。", true); return; }
            InsertFlowSnippetAtIndexS70(snippet, anchorIndex + (after ? 1 : 0), anchor, after ? "在锚点后" : "在锚点前");
        }

        private void InsertFlowSnippetAtIndexS70(VmFlowSnippetData snippet, int insertionIndex, VmToolInstance anchor, string placementText)
        {
            if (snippet == null) { SetFlowSnippetStatusS70("请先选择一个流程片段。", true); return; }
            if (anchor == null || !flowTools.Contains(anchor)) { SetFlowSnippetStatusS70("请先在流程中选择插入锚点。", true); return; }
            List<VmToolClipboardItem> oldClipboard = flowToolClipboard.ToList();
            int? oldInsertionIndex = flowPasteInsertionIndex;
            try
            {
                flowToolClipboard.Clear();
                for (int index = 0; index < snippet.ToolFlow.Count; index++) flowToolClipboard.Add(CreateSnippetClipboardItemS70(snippet.ToolFlow[index], index));
                lastSnippetAutoBoundCountS70 = 0;
                lastSnippetUnresolvedCountS70 = 0;
                flowPasteHistoryDescriptionS70 = "插入流程片段“" + snippet.Name + "”";
                flowPastePostProcessS70 = (pasted, idMap) =>
                {
                    BindSnippetExternalInputsS70(snippet, pasted, idMap, anchor);
                    if (lastSnippetUnresolvedCountS70 > 0) OpenInsertedSnippetUnresolvedTasksS71(pasted);
                };
                int count = PasteFlowToolClipboard(Math.Max(0, Math.Min(insertionIndex, flowTools.Count)));
                if (count > 0)
                {
                    string message = "已" + placementText + "插入片段“" + snippet.Name + "”：" + count.ToString(CultureInfo.InvariantCulture) + " 个新模块，内部连接已重映射，" + lastSnippetAutoBoundCountS70.ToString(CultureInfo.InvariantCulture) + " 个外部输入已安全绑定，" + lastSnippetUnresolvedCountS70.ToString(CultureInfo.InvariantCulture) + " 个待补源；一次撤销可整体回退。";
                    SetFlowSnippetStatusS70(message, lastSnippetUnresolvedCountS70 > 0);
                    LogInfo(message);
                }
            }
            catch (Exception ex)
            {
                string message = "片段“" + snippet.Name + "”无法插入：" + ex.Message + "；请检查片段类型、版本和输入契约。";
                SetFlowSnippetStatusS70(message, true);
                LogInfo(message);
            }
            finally
            {
                flowPastePostProcessS70 = null;
                flowPasteHistoryDescriptionS70 = null;
                flowToolClipboard.Clear();
                flowToolClipboard.AddRange(oldClipboard);
                flowPasteInsertionIndex = oldInsertionIndex;
                RefreshFlowClipboardStatus();
            }
        }

        private static VmToolClipboardItem CreateSnippetClipboardItemS70(ToolFlowRecipeItem item, int sequence)
        {
            VmToolKind kind;
            if (!Enum.TryParse(item.ToolType, true, out kind)) throw new InvalidOperationException("片段包含当前版本不支持的工具类型：" + item.ToolType);
            NumericJudgeRecipeData judge = item.NumericJudge;
            return new VmToolClipboardItem
            {
                SourceToolId = item.ToolId,
                SourceSequence = sequence,
                Kind = kind,
                InstanceName = item.InstanceName,
                IsEnabled = item.IsEnabled,
                Parameters = item.Parameters == null ? new VmToolParameterData() : item.Parameters.Clone(),
                RoiIds = (item.RoiIds ?? new List<string>()).ToList(),
                InputBindings = (item.InputBindings ?? new List<VmToolInputBindingData>()).Select(binding => binding.Clone()).ToList(),
                InputToolId = judge == null ? null : judge.InputToolId,
                InputPortName = judge == null ? null : judge.InputPortName,
                NumericOperator = judge == null ? NumericJudgeOperatorOption.BetweenInclusive : judge.Operator,
                NumericLowerLimit = judge == null ? 0 : judge.LowerLimit,
                NumericUpperLimit = judge == null ? 0 : judge.UpperLimit,
                NumericTolerance = judge == null ? 0 : judge.Tolerance,
                HasManualGraphPosition = false,
                GroupId = item.GroupId,
                GroupName = item.GroupName,
                IsGroupCollapsed = item.IsGroupCollapsed
            };
        }

        private void BindSnippetExternalInputsS70(VmFlowSnippetData snippet, IList<VmToolInstance> pasted, IDictionary<string, string> idMap, VmToolInstance anchor)
        {
            foreach (VmFlowSnippetExternalInputData external in snippet.ExternalInputs)
            {
                string targetId;
                if (!idMap.TryGetValue(external.TargetTemplateToolId ?? string.Empty, out targetId)) { if (external.IsRequired) lastSnippetUnresolvedCountS70++; continue; }
                VmToolInstance target = pasted.FirstOrDefault(item => string.Equals(item.ToolId, targetId, StringComparison.OrdinalIgnoreCase));
                VmPortDefinition port = target == null ? null : ToolMetadata.GetInstanceInputPorts(target).FirstOrDefault(item => string.Equals(item.PortName, external.TargetPortName, StringComparison.OrdinalIgnoreCase));
                if (target == null || port == null) { if (external.IsRequired) lastSnippetUnresolvedCountS70++; continue; }
                VmToolInputBindingData existing = target.GetInputBinding(port.PortName);
                if (existing != null && string.IsNullOrWhiteSpace(GetInputBindingConfigurationError(target, existing))) { lastSnippetAutoBoundCountS70++; continue; }
                bool bound = false;
                if (!string.IsNullOrWhiteSpace(external.PreferredSourceToolId) && flowTools.Any(item => string.Equals(item.ToolId, external.PreferredSourceToolId, StringComparison.OrdinalIgnoreCase)))
                    bound = TrySetAutoBindingS66(target, port, external.PreferredSourceToolId, external.PreferredSourcePortName);
                if (!bound)
                {
                    HashSet<string> used = new HashSet<string>(target.InputBindings.Where(item => !string.Equals(item.TargetPortName, port.PortName, StringComparison.OrdinalIgnoreCase)).Select(item => item.SourceToolId + "|" + item.SourcePortName), StringComparer.OrdinalIgnoreCase);
                    Tuple<VmToolInstance, VmPortDefinition> nearest = FindNearestCompatibleSourceS66(flowTools.IndexOf(target), external.DataType, used, anchor);
                    bound = nearest != null && TrySetAutoBindingS66(target, port, nearest.Item1.ToolId, nearest.Item2.PortName);
                }
                if (bound) lastSnippetAutoBoundCountS70++;
                else if (external.IsRequired) lastSnippetUnresolvedCountS70++;
            }
        }

        private void RefreshPointPairLineConnectionStatusS70(VmToolInstance tool)
        {
            VmToolInputBindingData bindingA = tool == null ? null : tool.GetInputBinding("PointA");
            VmToolInputBindingData bindingB = tool == null ? null : tool.GetInputBinding("PointB");
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            object rawA;
            object rawB;
            bool readyA = sourceA != null && bindingA != null && sourceA.ResultCode == "OK" && sourceA.TryGetOutputValue(bindingA.SourcePortName, out rawA) && rawA is VmPoint2D;
            bool readyB = sourceB != null && bindingB != null && sourceB.ResultCode == "OK" && sourceB.TryGetOutputValue(bindingB.SourcePortName, out rawB) && rawB is VmPoint2D;
            tool.ConfigurationStatus = bindingA == null || bindingB == null ? "待连接几何" : (readyA && readyB ? "就绪" : "等待上游");
            tool.ConnectionStatus = bindingA == null || bindingB == null ? "PointA 与 PointB 必须连接" : (readyA && readyB ? "两路 Point2D 已连接 · 有值" : "点输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildPointPairLineInputSummaryS70(tool) + " → Line2D / MidPoint / Distance / XLD";
        }

        private string BuildPointPairLineInputSummaryS70(VmToolInstance tool)
        {
            VmToolInputBindingData a = tool == null ? null : tool.GetInputBinding("PointA");
            VmToolInputBindingData b = tool == null ? null : tool.GetInputBinding("PointB");
            VmToolInstance sourceA = GetInputSourceTool(a);
            VmToolInstance sourceB = GetInputSourceTool(b);
            return (sourceA == null || a == null ? "PointA←未连接" : sourceA.InstanceName + "." + a.SourcePortName) + " + " +
                   (sourceB == null || b == null ? "PointB←未连接" : sourceB.InstanceName + "." + b.SourcePortName);
        }

        private InspectionRecord RunPointPairLineConstructionToolS70(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.PointPairLineConstructionMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData bindingA = tool.GetInputBinding("PointA");
            VmToolInputBindingData bindingB = tool.GetInputBinding("PointB");
            if (bindingA == null || bindingB == null) throw new InvalidOperationException("两点构线/中点测量必须连接 PointA 与 PointB 两路 Point2D。");
            if (string.Equals(bindingA.SourceToolId, bindingB.SourceToolId, StringComparison.OrdinalIgnoreCase) && string.Equals(bindingA.SourcePortName, bindingB.SourcePortName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PointA 与 PointB 不能订阅同一个上游端点。");
            string errorA = GetInputBindingConfigurationError(tool, bindingA);
            string errorB = GetInputBindingConfigurationError(tool, bindingB);
            if (!string.IsNullOrWhiteSpace(errorA)) throw new InvalidOperationException("PointA：" + errorA);
            if (!string.IsNullOrWhiteSpace(errorB)) throw new InvalidOperationException("PointB：" + errorB);
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            if (sourceA == null || sourceB == null || sourceA.ResultCode != "OK" || sourceB.ResultCode != "OK")
                throw new InvalidOperationException("PointA 或 PointB 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object rawA;
            object rawB;
            if (!sourceA.TryGetOutputValue(bindingA.SourcePortName, out rawA) || !(rawA is VmPoint2D)) throw new InvalidOperationException("PointA 没有类型化 Point2D 运行值。");
            if (!sourceB.TryGetOutputValue(bindingB.SourcePortName, out rawB) || !(rawB is VmPoint2D)) throw new InvalidOperationException("PointB 没有类型化 Point2D 运行值。");
            VmPoint2D pointA = ((VmPoint2D)rawA).Clone();
            VmPoint2D pointB = ((VmPoint2D)rawB).Clone();
            string pointErrorA = pointA.GetValidationError();
            string pointErrorB = pointB.GetValidationError();
            if (!string.IsNullOrWhiteSpace(pointErrorA)) throw new InvalidOperationException("PointA：" + pointErrorA);
            if (!string.IsNullOrWhiteSpace(pointErrorB)) throw new InvalidOperationException("PointB：" + pointErrorB);
            if (!string.Equals(pointA.CoordinateContext, pointB.CoordinateContext, StringComparison.Ordinal) || !string.Equals(pointA.CoordinateContextId, pointB.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("PointA 与 PointB 坐标上下文不一致，禁止跨图像构线。");

            HTuple distanceTuple = null;
            HObject overlays = null;
            HObject segment = null;
            HObject endpointCrosses = null;
            HObject midpointCross = null;
            try
            {
                HOperatorSet.DistancePp(pointA.Row, pointA.Column, pointB.Row, pointB.Column, out distanceTuple);
                double distance = distanceTuple == null || distanceTuple.Length == 0 ? double.NaN : distanceTuple[0].D;
                if (double.IsNaN(distance) || double.IsInfinity(distance)) throw new InvalidOperationException("HALCON distance_pp 返回了非有限距离。");
                if (distance <= parameters.PointPairMinimumDistance) throw new InvalidOperationException("两点距离 " + distance.ToString("0.############", CultureInfo.InvariantCulture) + "px 不大于最小有效距离 " + parameters.PointPairMinimumDistance.ToString("0.############", CultureInfo.InvariantCulture) + "px，不能构造稳定 Line2D。");
                double deltaRow = pointB.Row - pointA.Row;
                double deltaColumn = pointB.Column - pointA.Column;
                VmPoint2D midpoint = VmPoint2D.CreateImagePixel((pointA.Row + pointB.Row) * 0.5, (pointA.Column + pointB.Column) * 0.5, pointA.CoordinateContextId);
                VmLine2D line = VmLine2D.CreateImagePixel(pointA.Row, pointA.Column, pointB.Row, pointB.Column, pointA.CoordinateContextId);
                string lineError = line.GetValidationError();
                if (!string.IsNullOrWhiteSpace(lineError)) throw new InvalidOperationException(lineError);
                double angle = line.DirectionAngleDegrees;

                HOperatorSet.GenContourPolygonXld(out segment, new HTuple(new[] { pointA.Row, pointB.Row }), new HTuple(new[] { pointA.Column, pointB.Column }));
                AppendOverlayObject(ref overlays, segment);
                HOperatorSet.GenCrossContourXld(out endpointCrosses, new HTuple(new[] { pointA.Row, pointB.Row }), new HTuple(new[] { pointA.Column, pointB.Column }), parameters.PointPairOverlayCrossSize, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, endpointCrosses);
                HOperatorSet.GenCrossContourXld(out midpointCross, midpoint.Row, midpoint.Column, parameters.PointPairOverlayCrossSize * 0.75, 0.0);
                AppendOverlayObject(ref overlays, midpointCross);

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("MainLine", line);
                tool.SetOutputValue("MidPoint", midpoint);
                tool.SetOutputValue("Distance", distance);
                tool.SetOutputValue("Angle", angle);
                tool.SetOutputValue("DeltaRow", deltaRow);
                tool.SetOutputValue("DeltaColumn", deltaColumn);
                tool.SetOutputValue("CoordinateContext", pointA.CoordinateContext + ":" + pointA.CoordinateContextId);
                string parameterText = "HALCON=distance_pp+gen_contour_polygon_xld+gen_cross_contour_xld, MinimumDistance=" + parameters.PointPairMinimumDistance.ToString("0.############", CultureInfo.InvariantCulture) + ", CrossSize=" + parameters.PointPairOverlayCrossSize.ToString("0.###", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(overlays, tool, "XLD", 4, "OK", distance);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture, "HALCON 两点构线/中点：Distance={0:0.###}px，Angle={1:0.###}°，DeltaRow={2:0.###}，DeltaColumn={3:0.###}，Mid=({4:0.###},{5:0.###}) · {6}", distance, angle, deltaRow, deltaColumn, midpoint.Row, midpoint.Column, pointA.CoordinateContextId);
                InspectionRecord record = CreateRecord("PointPairLineConstructionMeasurement", "OK", distance, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (distanceTuple != null) distanceTuple.Dispose();
                DisposeObject(midpointCross);
                DisposeObject(endpointCrosses);
                DisposeObject(segment);
                DisposeObject(overlays);
            }
        }
    }
}
