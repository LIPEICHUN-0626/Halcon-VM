using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private const string FlowSnippetDragFormatS71 = "HalconVm.FlowSnippet";
        private readonly ObservableCollection<VmFlowSnippetListItemS70> toolboxSnippetItemsS71 = new ObservableCollection<VmFlowSnippetListItemS70>();
        private bool toolboxSnippetModeS71;
        private Point toolboxSnippetDragStartS71;
        private VmFlowSnippetListItemS70 toolboxSnippetDragItemS71;
        private string flowStepActivityToolIdS71;

        private bool IsToolboxSnippetModeActiveS71 { get { return toolboxSnippetModeS71; } }

        private void ToolCatalogSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (toolboxSnippetModeS71)
            {
                ExitToolboxSnippetModeS71(true);
                return;
            }
            EnterToolboxSnippetModeS71();
        }

        private void ToolCatalogCategoryMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (ToolCatalogAllButton == null || ToolCatalogAllButton.ContextMenu == null) return;
            ToolCatalogAllButton.ContextMenu.PlacementTarget = ToolCatalogAllButton;
            ToolCatalogAllButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            ToolCatalogAllButton.ContextMenu.IsOpen = true;
        }

        private void ToolCatalogShowAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExitToolboxSnippetModeS71(false);
            SelectFirstToolboxCategoryS80();
            RefreshToolCatalogView();
            HeaderStatusText.Text = "模块箱已返回全部分类；可直接展开分类、搜索或切换常用/收藏/最近。";
        }

        private void ToolCatalogShowSnippetsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!toolboxSnippetModeS71) EnterToolboxSnippetModeS71();
        }

        private void EnterToolboxSnippetModeS71()
        {
            if (IsToolRecommendationContextActiveS67) ExitToolRecommendationContextS67(false);
            if (IsInputSourceContextActiveS68) ExitInputSourceContextS68(false);
            toolboxSnippetModeS71 = true;
            LoadFlowSnippetLibraryS70(false);
            if (ToolCatalogList != null) ToolCatalogList.Visibility = Visibility.Collapsed;
            if (ToolSnippetCatalogList != null)
            {
                ToolSnippetCatalogList.Visibility = Visibility.Visible;
                ToolSnippetCatalogList.ItemsSource = toolboxSnippetItemsS71;
            }
            RefreshToolboxSnippetItemsS71();
            UpdateToolboxSnippetModeButtonS71();
            HeaderStatusText.Text = "模块箱已切换为流程片段；双击、Enter、拖拽或右键可围绕当前流程锚点插入。";
        }

        private void ExitToolboxSnippetModeS71(bool feedback)
        {
            if (!toolboxSnippetModeS71) return;
            toolboxSnippetModeS71 = false;
            toolboxSnippetDragItemS71 = null;
            if (ToolSnippetCatalogList != null) ToolSnippetCatalogList.Visibility = Visibility.Collapsed;
            if (ToolCatalogList != null) ToolCatalogList.Visibility = Visibility.Visible;
            if (ToolSearchTextBox != null && !string.IsNullOrWhiteSpace(ToolSearchTextBox.Text)) ToolSearchTextBox.Text = string.Empty;
            UpdateToolboxSnippetModeButtonS71();
            RefreshToolCatalogView();
            if (feedback) HeaderStatusText.Text = "已返回完整工具目录；65+ 个真实工具与 9 个分类保持默认可见。";
        }

        private void UpdateToolboxSnippetModeButtonS71()
        {
            if (ToolCatalogSnippetButton == null) return;
            ToolCatalogSnippetButton.Content = toolboxSnippetModeS71 ? "返回" : "片段";
            ToolCatalogSnippetButton.Background = toolboxSnippetModeS71
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(124, 58, 237))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            ToolCatalogSnippetButton.Foreground = toolboxSnippetModeS71 ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray;
            if (!toolboxSnippetModeS71) UpdateToolboxCategoryButtonS80();
        }

        private void RefreshToolboxSnippetItemsS71()
        {
            if (!toolboxSnippetModeS71 || ToolSnippetCatalogList == null) return;
            string selectedId = GetSelectedToolboxSnippetS71() == null ? null : GetSelectedToolboxSnippetS71().SnippetId;
            string query = ToolSearchTextBox == null || ToolSearchTextBox.Text == null ? string.Empty : ToolSearchTextBox.Text.Trim();
            toolboxSnippetItemsS71.Clear();
            foreach (VmFlowSnippetData snippet in flowSnippetLibraryS70.Snippets.OrderByDescending(item => item.UpdatedAtUtc, StringComparer.Ordinal))
            {
                string searchable = string.Join(" ", new[]
                {
                    snippet.Name,
                    string.Join(" ", snippet.ToolFlow.Select(item => item.InstanceName + " " + item.ToolType)),
                    string.Join(" ", snippet.ExternalInputs.Select(item => item.TargetToolName + " " + item.TargetPortName + " " + item.DataType))
                });
                if (!string.IsNullOrWhiteSpace(query) && searchable.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                int internalCount = CountSnippetInternalConnectionsS70(snippet);
                int required = snippet.ExternalInputs.Count(item => item.IsRequired);
                int optional = snippet.ExternalInputs.Count - required;
                DateTime updated;
                string updatedText = DateTime.TryParse(snippet.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out updated)
                    ? updated.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.CurrentCulture)
                    : snippet.UpdatedAtUtc;
                toolboxSnippetItemsS71.Add(new VmFlowSnippetListItemS70
                {
                    Data = snippet,
                    Name = snippet.Name,
                    ToolSummary = snippet.ToolFlow.Count.ToString(CultureInfo.InvariantCulture) + " 个工具 · " + internalCount.ToString(CultureInfo.InvariantCulture) + " 条内部连接",
                    ContractSummary = required.ToString(CultureInfo.InvariantCulture) + " 个必需外部输入" + (optional == 0 ? string.Empty : " · " + optional.ToString(CultureInfo.InvariantCulture) + " 可选"),
                    UpdatedText = updatedText
                });
            }
            ToolSnippetCatalogList.ItemsSource = toolboxSnippetItemsS71;
            VmFlowSnippetListItemS70 selected = toolboxSnippetItemsS71.FirstOrDefault(item => string.Equals(item.Data.SnippetId, selectedId, StringComparison.OrdinalIgnoreCase)) ?? toolboxSnippetItemsS71.FirstOrDefault();
            ToolSnippetCatalogList.SelectedItem = selected;
            ToolCatalogEmptyText.Visibility = toolboxSnippetItemsS71.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ToolCatalogEmptyText.Text = flowSnippetLibraryS70.Snippets.Count == 0
                ? "本机还没有流程片段。请在流程中连续选择至少 2 个工具，再从“更多 → 流程片段”保存。"
                : "当前搜索没有匹配片段。清空搜索可恢复本机片段列表。";
            ToolCatalogCapabilityText.Text = flowSnippetLibraryS70.Snippets.Count.ToString(CultureInfo.InvariantCulture) + " 个片段 · 本机";
            ToolCatalogStatusText.Text = "片段 · " + toolboxSnippetItemsS71.Count.ToString(CultureInfo.InvariantCulture) + " 个可见 · 双击/Enter 插在锚点后，拖拽或右键选择位置";
            RefreshToolboxSnippetPreviewS71();
        }

        private VmFlowSnippetData GetSelectedToolboxSnippetS71()
        {
            VmFlowSnippetListItemS70 item = ToolSnippetCatalogList == null ? null : ToolSnippetCatalogList.SelectedItem as VmFlowSnippetListItemS70;
            return item == null ? null : item.Data;
        }

        private void ToolSnippetCatalogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshToolboxSnippetPreviewS71();
        }

        private void RefreshToolboxSnippetPreviewS71()
        {
            if (!toolboxSnippetModeS71 || ToolCapabilityNameText == null) return;
            VmFlowSnippetData snippet = GetSelectedToolboxSnippetS71();
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            if (snippet == null)
            {
                ToolCapabilityNameText.Text = "选择一个本机流程片段";
                ToolCapabilityMaturityText.Text = "本机模板 · 按需插入";
                ToolCapabilityPurposeText.Text = "片段是确定性线性工具模板，不是并行子流程。";
                ToolCapabilityInputText.Text = "IN  --";
                ToolCapabilityOutputText.Text = "顺序 --";
                ToolCapabilityGraphicsText.Text = "内部连接 --";
                ToolCapabilityInsertAnchorText.Text = anchor == null ? "插入锚点：未选择流程模块" : "插入锚点：" + anchor.SequenceText + " " + anchor.InstanceName;
                ToolCapabilityCompatibilityText.Text = "外部契约：--";
                ToolCapabilityInsertStatusText.Text = "先保存或选择片段，再围绕锚点插入。";
                return;
            }
            ToolCapabilityNameText.Text = snippet.Name + " · 流程片段";
            ToolCapabilityMaturityText.Text = "本机模板 · " + snippet.ToolFlow.Count.ToString(CultureInfo.InvariantCulture) + " 工具";
            ToolCapabilityPurposeText.Text = "工具顺序：" + string.Join(" → ", snippet.ToolFlow.Select(item => item.InstanceName));
            ToolCapabilityInputText.Text = "IN  " + BuildSnippetExternalInputsTextS70(snippet);
            ToolCapabilityOutputText.Text = "顺序 " + string.Join(" → ", snippet.ToolFlow.Select(item => ToolMetadata.GetDisplayName(ParseSnippetKindS71(item.ToolType))));
            ToolCapabilityGraphicsText.Text = "内部连接 " + CountSnippetInternalConnectionsS70(snippet).ToString(CultureInfo.InvariantCulture) + " 条 · 插入时按新 ToolId 重映射";
            ToolCapabilityInsertAnchorText.Text = anchor == null ? "插入锚点：未选择流程模块" : "插入锚点：" + anchor.SequenceText + " " + anchor.InstanceName;
            int required = snippet.ExternalInputs.Count(item => item.IsRequired);
            ToolCapabilityCompatibilityText.Text = "外部契约：" + required.ToString(CultureInfo.InvariantCulture) + " 个必需输入 · 安全复用兼容前序来源";
            ToolCapabilityInsertStatusText.Text = anchor == null
                ? "请选择流程锚点；不会无锚点静默追加复杂片段。"
                : "双击/Enter 插在锚点后；右键或拖拽可指定前后。未补齐输入会进入连接问题任务并可交给 S68 补源。";
            ToolCapabilityExpander.IsExpanded = true;
        }

        private static VmToolKind ParseSnippetKindS71(string value)
        {
            VmToolKind kind;
            return Enum.TryParse(value, true, out kind) ? kind : VmToolKind.HDevelop;
        }

        private void InsertSelectedToolboxSnippetS71(bool after)
        {
            VmFlowSnippetData snippet = GetSelectedToolboxSnippetS71();
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            if (snippet == null) { HeaderStatusText.Text = "请先在左侧选择流程片段。"; return; }
            if (anchor == null || !flowTools.Contains(anchor)) { HeaderStatusText.Text = "请先在流程中选择插入锚点。"; return; }
            int index = flowTools.IndexOf(anchor) + (after ? 1 : 0);
            InsertFlowSnippetAtIndexS70(snippet, index, anchor, after ? "在锚点后" : "在锚点前");
            RefreshToolboxSnippetPreviewS71();
        }

        private void ToolSnippetCatalogList_MouseDoubleClick(object sender, MouseButtonEventArgs e) { InsertSelectedToolboxSnippetS71(true); }
        private void ToolSnippetInsertBeforeMenuItem_Click(object sender, RoutedEventArgs e) { InsertSelectedToolboxSnippetS71(false); }
        private void ToolSnippetInsertAfterMenuItem_Click(object sender, RoutedEventArgs e) { InsertSelectedToolboxSnippetS71(true); }

        private void ToolSnippetCatalogList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            InsertSelectedToolboxSnippetS71((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift);
            e.Handled = true;
        }

        private void ToolSnippetCatalogList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem container = ItemsControl.ContainerFromElement(ToolSnippetCatalogList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container == null) return;
            container.IsSelected = true;
            container.Focus();
        }

        private void ToolSnippetCatalogList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            toolboxSnippetDragStartS71 = e.GetPosition(null);
            ListBoxItem container = ItemsControl.ContainerFromElement(ToolSnippetCatalogList, e.OriginalSource as DependencyObject) as ListBoxItem;
            toolboxSnippetDragItemS71 = container == null ? null : container.DataContext as VmFlowSnippetListItemS70;
        }

        private void ToolSnippetCatalogList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || toolboxSnippetDragItemS71 == null) return;
            Point current = e.GetPosition(null);
            if (Math.Abs(current.X - toolboxSnippetDragStartS71.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - toolboxSnippetDragStartS71.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            VmFlowSnippetData snippet = toolboxSnippetDragItemS71.Data;
            toolboxSnippetDragItemS71 = null;
            DragDrop.DoDragDrop(ToolSnippetCatalogList, new DataObject(FlowSnippetDragFormatS71, snippet), DragDropEffects.Copy);
            HideFlowDropHint();
        }

        private bool IsFlowSnippetDragS71(DragEventArgs e)
        {
            return e != null && e.Data != null && e.Data.GetDataPresent(FlowSnippetDragFormatS71);
        }

        private bool TryInsertDraggedFlowSnippetToListS71(DragEventArgs e)
        {
            if (!IsFlowSnippetDragS71(e)) return false;
            VmFlowSnippetData snippet = e.Data.GetData(FlowSnippetDragFormatS71) as VmFlowSnippetData;
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            if (snippet == null || anchor == null || !flowTools.Contains(anchor))
            {
                HeaderStatusText.Text = "片段拖拽被拒绝：请先选择有效流程锚点。";
                return true;
            }
            InsertFlowSnippetAtIndexS70(snippet, GetFlowDropInsertionIndex(e), anchor, "按顺序列表拖放位置");
            return true;
        }

        private bool TryInsertDraggedFlowSnippetToGraphS71(DragEventArgs e)
        {
            if (!IsFlowSnippetDragS71(e)) return false;
            VmFlowSnippetData snippet = e.Data.GetData(FlowSnippetDragFormatS71) as VmFlowSnippetData;
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            if (snippet == null || anchor == null || !flowTools.Contains(anchor))
            {
                HeaderStatusText.Text = "片段拖拽被拒绝：连接画布需要先选择插入锚点。";
                return true;
            }
            InsertFlowSnippetAtIndexS70(snippet, flowTools.IndexOf(anchor) + 1, anchor, "在画布锚点后");
            return true;
        }

        private void ToolSnippetOpenManagerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string selectedId = GetSelectedToolboxSnippetS71() == null ? null : GetSelectedToolboxSnippetS71().SnippetId;
            OpenFlowSnippetLibraryButton_Click(sender, e);
            if (!string.IsNullOrWhiteSpace(selectedId) && FlowSnippetLibraryListBox != null)
                FlowSnippetLibraryListBox.SelectedItem = flowSnippetItemsS70.FirstOrDefault(item => string.Equals(item.Data.SnippetId, selectedId, StringComparison.OrdinalIgnoreCase));
        }

        private void OpenInsertedSnippetUnresolvedTasksS71(IList<VmToolInstance> pasted)
        {
            HashSet<string> ids = new HashSet<string>((pasted ?? new List<VmToolInstance>()).Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
            RebuildConnectionIssuesS69(false);
            VmConnectionIssueS69 issue = connectionIssuesS69.FirstOrDefault(item => ids.Contains(item.TargetToolId) && item.IsBlocking);
            if (issue != null && FlowConnectionIssueListBox != null) FlowConnectionIssueListBox.SelectedItem = issue;
            if (FlowConnectionIssuePanel != null) FlowConnectionIssuePanel.Visibility = Visibility.Visible;
            if (FlowConnectionIssueSummaryText != null) FlowConnectionIssueSummaryText.Text += " · 片段待补源可交给 S68";
        }

        private void OpenFlowStepActivityButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            VmToolInstance tool = element == null ? null : element.Tag as VmToolInstance;
            OpenFlowStepActivityS71(tool);
            e.Handled = true;
        }

        private void OpenFlowStepActivityMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFlowStepActivityS71(FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance);
        }

        private void OpenFlowStepActivityS71(VmToolInstance tool)
        {
            if (tool == null)
            {
                HeaderStatusText.Text = "请先选择要查看记录的流程步骤。";
                return;
            }
            flowStepActivityToolIdS71 = tool.ToolId;
            if (FlowConnectionInspectorBorder != null) FlowConnectionInspectorBorder.Visibility = Visibility.Collapsed;
            if (FlowPortInspectorBorder != null) FlowPortInspectorBorder.Visibility = Visibility.Collapsed;
            if (FlowConnectionIssuePanel != null) FlowConnectionIssuePanel.Visibility = Visibility.Collapsed;
            RefreshFlowStepActivityS71();
            FlowStepActivityPanel.Visibility = Visibility.Visible;
            HeaderStatusText.Text = "已打开 " + tool.SequenceText + " " + tool.InstanceName + " 的步骤日志 / 通讯子页面。";
        }

        private void RefreshFlowStepActivityButton_Click(object sender, RoutedEventArgs e) { RefreshFlowStepActivityS71(); }

        private void CloseFlowStepActivityButton_Click(object sender, RoutedEventArgs e)
        {
            FlowStepActivityPanel.Visibility = Visibility.Collapsed;
            flowStepActivityToolIdS71 = null;
            HeaderStatusText.Text = "已关闭步骤记录子页面；流程画布保持纵向调试视图。";
        }

        private void RefreshFlowStepActivityS71()
        {
            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, flowStepActivityToolIdS71, StringComparison.OrdinalIgnoreCase));
            if (tool == null)
            {
                FlowStepActivityTitleText.Text = "步骤已删除或失效";
                FlowStepActivitySummaryText.Text = "请关闭本页并重新选择流程步骤。";
                FlowStepLogTextBox.Text = "--";
                FlowStepCommunicationTextBox.Text = "--";
                return;
            }
            FlowStepActivityTitleText.Text = tool.SequenceText + "  " + tool.InstanceName + " · " + tool.DisplayType;
            FlowStepActivitySummaryText.Text = "状态 " + tool.RunStatus + " · 结果 " + tool.ResultCode + " · 耗时 " + tool.ElapsedText +
                (string.IsNullOrWhiteSpace(tool.ErrorMessage) ? string.Empty : " · 异常 " + tool.ErrorMessage);
            List<string> logs = new List<string>();
            if (!string.IsNullOrWhiteSpace(tool.OutputSummary)) logs.Add("[本步骤结果] " + tool.OutputSummary);
            if (!string.IsNullOrWhiteSpace(tool.InputSummary)) logs.Add("[输入] " + tool.InputSummary);
            string allLog = BottomLogTextBox == null ? string.Empty : BottomLogTextBox.Text ?? string.Empty;
            logs.AddRange(allLog.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.IndexOf(tool.InstanceName, StringComparison.CurrentCultureIgnoreCase) >= 0 || line.IndexOf(tool.ToolId, StringComparison.OrdinalIgnoreCase) >= 0)
                .Reverse().Take(200).Reverse());
            FlowStepLogTextBox.Text = logs.Count == 0 ? "该步骤尚无运行记录。运行当前工具或流程后再刷新。" : string.Join(Environment.NewLine, logs.Distinct());

            if (tool.Kind == VmToolKind.TcpClientExchange)
            {
                VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
                FlowStepCommunicationTextBox.Text = "TCP 目标：" + p.TcpClientHost + ":" + p.TcpClientPort.ToString(CultureInfo.InvariantCulture) + Environment.NewLine +
                    "连接：" + tool.GetFormattedOutput("Connected") + Environment.NewLine +
                    "发送：" + tool.GetFormattedOutput("SentText") + Environment.NewLine +
                    "接收：" + tool.GetFormattedOutput("ReceivedText") + Environment.NewLine +
                    "字节：" + tool.GetFormattedOutput("BytesSent") + " / " + tool.GetFormattedOutput("BytesReceived") + Environment.NewLine +
                    "往返：" + tool.GetFormattedOutput("RoundTripMs") + " ms";
            }
            else
            {
                FlowStepCommunicationTextBox.Text = "该步骤不是通讯工具，没有独立收发交互。\r\n需要查看全局设备收发时，可从右侧“更多 → 通讯工作区”进入。";
            }
        }

        private void RefreshTypedLineLineConnectionStatusS71(VmToolInstance tool)
        {
            VmToolInputBindingData bindingA = tool == null ? null : tool.GetInputBinding("LineA");
            VmToolInputBindingData bindingB = tool == null ? null : tool.GetInputBinding("LineB");
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            object rawA;
            object rawB;
            bool readyA = sourceA != null && bindingA != null && string.Equals(sourceA.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && sourceA.TryGetOutputValue(bindingA.SourcePortName, out rawA) && rawA is VmLine2D;
            bool readyB = sourceB != null && bindingB != null && string.Equals(sourceB.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && sourceB.TryGetOutputValue(bindingB.SourcePortName, out rawB) && rawB is VmLine2D;
            tool.ConfigurationStatus = bindingA == null || bindingB == null ? "待连接几何" : (readyA && readyB ? "就绪" : "等待上游");
            tool.ConnectionStatus = bindingA == null || bindingB == null ? "LineA 与 LineB 必须连接" : (readyA && readyB ? "两路 Line2D 已连接 · 有值" : "直线输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildTypedLineLineInputSummaryS71(tool) + " → Point2D / Angle / ParallelDistance / XLD";
        }

        private string BuildTypedLineLineInputSummaryS71(VmToolInstance tool)
        {
            VmToolInputBindingData a = tool == null ? null : tool.GetInputBinding("LineA");
            VmToolInputBindingData b = tool == null ? null : tool.GetInputBinding("LineB");
            VmToolInstance sourceA = GetInputSourceTool(a);
            VmToolInstance sourceB = GetInputSourceTool(b);
            return (sourceA == null || a == null ? "LineA←未连接" : sourceA.InstanceName + "." + a.SourcePortName) + " + " +
                   (sourceB == null || b == null ? "LineB←未连接" : sourceB.InstanceName + "." + b.SourcePortName);
        }

        private InspectionRecord RunTypedLineLineRelationToolS71(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.TypedLineLineRelationMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData bindingA = tool.GetInputBinding("LineA");
            VmToolInputBindingData bindingB = tool.GetInputBinding("LineB");
            if (bindingA == null || bindingB == null) throw new InvalidOperationException("类型化双直线关系必须连接 LineA 与 LineB 两路 Line2D。");
            if (string.Equals(bindingA.SourceToolId, bindingB.SourceToolId, StringComparison.OrdinalIgnoreCase) && string.Equals(bindingA.SourcePortName, bindingB.SourcePortName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("LineA 与 LineB 不能订阅同一个上游端点。");
            string errorA = GetInputBindingConfigurationError(tool, bindingA);
            string errorB = GetInputBindingConfigurationError(tool, bindingB);
            if (!string.IsNullOrWhiteSpace(errorA)) throw new InvalidOperationException("LineA：" + errorA);
            if (!string.IsNullOrWhiteSpace(errorB)) throw new InvalidOperationException("LineB：" + errorB);
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            if (sourceA == null || sourceB == null || !string.Equals(sourceA.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(sourceB.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("LineA 或 LineB 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object rawA;
            object rawB;
            if (!sourceA.TryGetOutputValue(bindingA.SourcePortName, out rawA) || !(rawA is VmLine2D)) throw new InvalidOperationException("LineA 没有类型化 Line2D 运行值。");
            if (!sourceB.TryGetOutputValue(bindingB.SourcePortName, out rawB) || !(rawB is VmLine2D)) throw new InvalidOperationException("LineB 没有类型化 Line2D 运行值。");
            VmLine2D lineA = ((VmLine2D)rawA).Clone();
            VmLine2D lineB = ((VmLine2D)rawB).Clone();
            string lineErrorA = lineA.GetValidationError();
            string lineErrorB = lineB.GetValidationError();
            if (!string.IsNullOrWhiteSpace(lineErrorA)) throw new InvalidOperationException("LineA：" + lineErrorA);
            if (!string.IsNullOrWhiteSpace(lineErrorB)) throw new InvalidOperationException("LineB：" + lineErrorB);
            if (!string.Equals(lineA.CoordinateContext, lineB.CoordinateContext, StringComparison.Ordinal) || !string.Equals(lineA.CoordinateContextId, lineB.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("LineA 与 LineB 坐标上下文不一致，禁止跨图像或跨坐标系计算。");

            HTuple intersectionRow = null;
            HTuple intersectionColumn = null;
            HTuple overlapTuple = null;
            HTuple angleTuple = null;
            HObject overlays = null;
            HObject contourA = null;
            HObject contourB = null;
            HObject cross = null;
            try
            {
                HOperatorSet.IntersectionLines(lineA.StartRow, lineA.StartColumn, lineA.EndRow, lineA.EndColumn,
                    lineB.StartRow, lineB.StartColumn, lineB.EndRow, lineB.EndColumn,
                    out intersectionRow, out intersectionColumn, out overlapTuple);
                HOperatorSet.AngleLl(lineA.StartRow, lineA.StartColumn, lineA.EndRow, lineA.EndColumn,
                    lineB.StartRow, lineB.StartColumn, lineB.EndRow, lineB.EndColumn, out angleTuple);
                double signedAngle = angleTuple == null || angleTuple.Length == 0 ? double.NaN : NormalizeSignedDegreesS71(angleTuple[0].D * 180.0 / Math.PI);
                if (!IsFiniteS71(signedAngle)) throw new InvalidOperationException("HALCON angle_ll 返回了非有限角度。");
                double absolute = Math.Abs(signedAngle);
                double acuteAngle = Math.Min(absolute, 180.0 - absolute);
                double distanceA1 = HalconPointLineDistanceS71(lineA.StartRow, lineA.StartColumn, lineB);
                double distanceA2 = HalconPointLineDistanceS71(lineA.EndRow, lineA.EndColumn, lineB);
                double distanceB1 = HalconPointLineDistanceS71(lineB.StartRow, lineB.StartColumn, lineA);
                double distanceB2 = HalconPointLineDistanceS71(lineB.EndRow, lineB.EndColumn, lineA);
                double maximumSymmetricDistance = Math.Max(Math.Max(distanceA1, distanceA2), Math.Max(distanceB1, distanceB2));
                bool halconOverlap = overlapTuple != null && overlapTuple.Length > 0 && overlapTuple[0].I != 0;
                bool isParallel = acuteAngle <= parameters.LineLineParallelToleranceDegrees;
                bool isCoincident = isParallel && (halconOverlap || maximumSymmetricDistance <= parameters.LineLineCoincidenceDistanceTolerance);
                double parallelDistance = isParallel ? (distanceA1 + distanceA2 + distanceB1 + distanceB2) * 0.25 : 0.0;
                VmPoint2D intersectionPoint = null;
                if (!isParallel && intersectionRow != null && intersectionRow.Length > 0 && intersectionColumn != null && intersectionColumn.Length > 0)
                {
                    double row = intersectionRow[0].D;
                    double column = intersectionColumn[0].D;
                    if (!IsFiniteS71(row) || !IsFiniteS71(column)) throw new InvalidOperationException("HALCON intersection_lines 返回了非有限交点。");
                    intersectionPoint = VmPoint2D.CreateImagePixel(row, column, lineA.CoordinateContextId);
                }
                if (!isParallel && intersectionPoint == null) throw new InvalidOperationException("非平行直线未获得有限交点，HALCON 返回语义异常。");
                string relation = isCoincident ? "重合" : (isParallel ? "平行" : "相交");

                HOperatorSet.GenContourPolygonXld(out contourA, new HTuple(new[] { lineA.StartRow, lineA.EndRow }), new HTuple(new[] { lineA.StartColumn, lineA.EndColumn }));
                AppendOverlayObject(ref overlays, contourA);
                HOperatorSet.GenContourPolygonXld(out contourB, new HTuple(new[] { lineB.StartRow, lineB.EndRow }), new HTuple(new[] { lineB.StartColumn, lineB.EndColumn }));
                AppendOverlayObject(ref overlays, contourB);
                if (intersectionPoint != null)
                {
                    HOperatorSet.GenCrossContourXld(out cross, intersectionPoint.Row, intersectionPoint.Column, parameters.LineLineOverlayCrossSize, Math.PI / 4.0);
                    AppendOverlayObject(ref overlays, cross);
                }

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                if (intersectionPoint != null) tool.SetOutputValue("IntersectionPoint", intersectionPoint);
                tool.SetOutputValue("SignedAngle", signedAngle);
                tool.SetOutputValue("AcuteAngle", acuteAngle);
                tool.SetOutputValue("ParallelDistance", parallelDistance);
                tool.SetOutputValue("IsParallel", isParallel);
                tool.SetOutputValue("IsCoincident", isCoincident);
                tool.SetOutputValue("Relation", relation);
                tool.SetOutputValue("CoordinateContext", lineA.CoordinateContext + ":" + lineA.CoordinateContextId);
                string parameterText = "HALCON=intersection_lines+angle_ll+distance_pl+gen_contour_polygon_xld+gen_cross_contour_xld, ParallelTolerance=" + parameters.LineLineParallelToleranceDegrees.ToString("0.######", CultureInfo.InvariantCulture) + "deg, CoincidenceTolerance=" + parameters.LineLineCoincidenceDistanceTolerance.ToString("0.######", CultureInfo.InvariantCulture) + "px, CrossSize=" + parameters.LineLineOverlayCrossSize.ToString("0.###", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Parameters", parameterText);
                tool.SetOutputValue("ResultCode", "OK");
                ReplaceToolOverlayContours(overlays, tool, "XLD", intersectionPoint == null ? 2 : 3, "OK", lineA.Length + lineB.Length);
                overlays = null;
                string pointText = intersectionPoint == null ? "无有限交点" : "交点=(" + intersectionPoint.Row.ToString("0.###", CultureInfo.InvariantCulture) + "," + intersectionPoint.Column.ToString("0.###", CultureInfo.InvariantCulture) + ")";
                string message = string.Format(CultureInfo.InvariantCulture, "HALCON 类型化双直线：{0}，SignedAngle={1:0.###}°，AcuteAngle={2:0.###}°，ParallelDistance={3:0.###}px，{4} · {5}", relation, signedAngle, acuteAngle, parallelDistance, pointText, lineA.CoordinateContextId);
                InspectionRecord record = CreateRecord("TypedLineLineRelationMeasurement", "OK", acuteAngle, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (intersectionRow != null) intersectionRow.Dispose();
                if (intersectionColumn != null) intersectionColumn.Dispose();
                if (overlapTuple != null) overlapTuple.Dispose();
                if (angleTuple != null) angleTuple.Dispose();
                DisposeObject(cross);
                DisposeObject(contourB);
                DisposeObject(contourA);
                DisposeObject(overlays);
            }
        }

        private static double HalconPointLineDistanceS71(double row, double column, VmLine2D line)
        {
            HTuple distance = null;
            try
            {
                HOperatorSet.DistancePl(row, column, line.StartRow, line.StartColumn, line.EndRow, line.EndColumn, out distance);
                double value = distance == null || distance.Length == 0 ? double.NaN : distance[0].D;
                if (!IsFiniteS71(value)) throw new InvalidOperationException("HALCON distance_pl 返回了非有限距离。");
                return Math.Abs(value);
            }
            finally
            {
                if (distance != null) distance.Dispose();
            }
        }

        private static double NormalizeSignedDegreesS71(double value)
        {
            while (value <= -180.0) value += 360.0;
            while (value > 180.0) value -= 360.0;
            return value;
        }

        private static bool IsFiniteS71(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
