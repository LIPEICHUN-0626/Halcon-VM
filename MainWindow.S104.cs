using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly List<VmProjectFlowData> projectFlowsS104 = new List<VmProjectFlowData>();
        private string activeProjectFlowIdS104;
        private bool changingProjectFlowS104;
        private string projectSessionIdS104 = Guid.NewGuid().ToString("N");
        private readonly Dictionary<string, RuntimeStatistics> projectFlowStatisticsS104 = new Dictionary<string, RuntimeStatistics>();

        private RuntimeStatistics GetActiveFlowStatisticsS104()
        {
            string key = activeProjectFlowIdS104 ?? string.Empty;
            RuntimeStatistics statistics;
            if (!projectFlowStatisticsS104.TryGetValue(key, out statistics))
            {
                statistics = new RuntimeStatistics(); projectFlowStatisticsS104.Add(key, statistics);
            }
            return statistics;
        }

        private IEnumerable<VmImageBatchTraceRecord> GetComparableBatchRecordsS104()
        {
            if (historicalBatchRecord == null) return Enumerable.Empty<VmImageBatchTraceRecord>();
            string flowId = historicalBatchRecord.FlowId;
            if (string.IsNullOrWhiteSpace(flowId))
                return batchTraceRecords.Where(record => record.BatchId == historicalBatchRecord.BatchId);
            return batchTraceRecords.Where(record => string.Equals(record.FlowId, flowId, StringComparison.OrdinalIgnoreCase));
        }

        private sealed class DeletedProjectFlowS104
        {
            public VmProjectFlowData Flow;
            public int Index;
            public DateTime DeletedAt;
            public string DisplayText { get { return Flow.Name + " · " + Flow.Tools.Count + " 个模块 · " + DeletedAt.ToString("HH:mm:ss"); } }
        }
        private readonly List<DeletedProjectFlowS104> deletedProjectFlowsS104 = new List<DeletedProjectFlowS104>();

        private sealed class ProjectFlowViewS104
        {
            public double Zoom, X, Y;
            public string SelectedId, SharedFingerprint;
            public List<FlowEditHistoryEntry> Undo, Redo;
        }
        private readonly Dictionary<string, ProjectFlowViewS104> projectFlowViewsS104 = new Dictionary<string, ProjectFlowViewS104>();

        private string GetProjectFlowNameS104()
        {
            var flow = projectFlowsS104.FirstOrDefault(item => item.FlowId == activeProjectFlowIdS104);
            return flow == null ? "流程 01" : flow.Name;
        }

        private void InitializeProjectFlowsS104(VisionRecipe recipe)
        {
            changingProjectFlowS104 = true;
            try
            {
                projectFlowsS104.Clear(); projectFlowViewsS104.Clear(); deletedProjectFlowsS104.Clear();
                projectSessionIdS104 = Guid.NewGuid().ToString("N"); projectFlowStatisticsS104.Clear();
                if (recipe.Flows != null) projectFlowsS104.AddRange(recipe.Flows.Select(flow => flow.Clone()));
                else projectFlowsS104.Add(new VmProjectFlowData { Name = "流程 01",
                    Tools = (recipe.ToolFlow ?? new List<ToolFlowRecipeItem>()).Select(VmSubflowData.CloneTool).ToList(),
                    ResultLayerStyles = recipe.ResultLayerStyles == null ? null : recipe.ResultLayerStyles.Select(item => item.Clone()).ToList(),
                    RunPolicy = (recipe.FlowRunPolicy ?? new VmFlowRunPolicy()).Normalize() });
                activeProjectFlowIdS104 = projectFlowsS104.Any(item => item.FlowId == recipe.ActiveFlowId)
                    ? recipe.ActiveFlowId : projectFlowsS104[0].FlowId;
            }
            finally { changingProjectFlowS104 = false; }
            RefreshProjectFlowPickerS104();
            LoadTcpTriggerConfigS104(recipe);
            ResetActiveFlowPresentationS104();
        }

        private List<VmProjectFlowData> CaptureProjectFlowsS104()
        {
            var result = projectFlowsS104.Select(flow => flow.Clone()).ToList();
            if (result.Count == 0) return result;
            var current = result.First(item => item.FlowId == activeProjectFlowIdS104);
            current.Tools = CaptureRootFlowRecipeS102();
            current.RunPolicy = ReadFlowRunPolicyFromUi();
            current.ResultLayerStyles = CaptureProjectFlowStylesS104();
            return result;
        }

        private void RefreshProjectFlowPickerS104()
        {
            if (ProjectFlowPickerS104 == null) return;
            changingProjectFlowS104 = true;
            try
            {
                ProjectFlowPickerS104.ItemsSource = projectFlowsS104.ToList();
                ProjectFlowPickerS104.SelectedValue = activeProjectFlowIdS104;
                RefreshSubflowEditorChromeS102();
                RefreshTcpTriggerPickerS104();
                if (RunOnceButton != null) RunOnceButton.ToolTip = "运行当前流程“" + GetProjectFlowNameS104() + "”；不运行方案中的其他流程。";
            }
            finally { changingProjectFlowS104 = false; }
        }

        private bool PrepareProjectFlowChangeS104()
        {
            if (!CanNavigateInputRepairS96()) return false;
            if (!CloseCentralModuleConfigurationS77()) return false;
            while (subflowEditorsS102.Count > 0)
            {
                int before = subflowEditorsS102.Count;
                ReturnSubflowEditorS102_Click(null, null);
                if (subflowEditorsS102.Count >= before) return false;
            }
            return true;
        }

        private string CaptureSharedFlowFingerprintS104()
        {
            return recipeService.GetRecipeFingerprint(new VisionRecipe { ToolFlow = new List<ToolFlowRecipeItem>(),
                RoiLayers = CaptureRoiLayers(), GlobalVariables = CaptureGlobalVariables(), ExternalResources = CaptureExternalResources(),
                ProductionRunLayout = productionRunLayout.CloneNormalized() });
        }

        private void SaveCurrentProjectFlowS104()
        {
            var flow = projectFlowsS104.First(item => item.FlowId == activeProjectFlowIdS104);
            flow.Tools = CaptureRootFlowRecipeS102(); flow.RunPolicy = ReadFlowRunPolicyFromUi();
            flow.ResultLayerStyles = CaptureProjectFlowStylesS104();
            var selected = FlowToolList.SelectedItem as VmToolInstance;
            projectFlowViewsS104[flow.FlowId] = new ProjectFlowViewS104 { Zoom = FlowZoomSlider.Value,
                X = FlowGraphViewport.HorizontalOffset, Y = FlowGraphViewport.VerticalOffset,
                SelectedId = selected == null ? null : selected.ToolId,
                Undo = flowUndoHistory.ToList(), Redo = flowRedoHistory.ToList(), SharedFingerprint = CaptureSharedFlowFingerprintS104() };
        }

        private void SwitchProjectFlowS104(string id)
        {
            if (id == activeProjectFlowIdS104) return;
            if (!CanChangeRecipeS134()) return;
            var target = projectFlowsS104.FirstOrDefault(item => item.FlowId == id);
            if (target == null) return;
            string requestSession = projectSessionIdS104;
            string previousId = activeProjectFlowIdS104;
            var previous = projectFlowsS104.FirstOrDefault(item => item.FlowId == previousId);
            if (previous == null) return;
            if (!CanSelectConfiguredFlowS137(id)) { RefreshProjectFlowPickerS104(); return; }
            DisarmTcpTriggerS104("切换流程后需重新启用");
            if (!PrepareProjectFlowChangeS104()) return;
            // Draft confirmation can pump the dispatcher. Never apply an old target
            // to a replaced project/session, even if the replacement reuses its ID.
            if (requestSession != projectSessionIdS104 || previousId != activeProjectFlowIdS104 ||
                !projectFlowsS104.Contains(previous) || !projectFlowsS104.Contains(target) ||
                !CanNavigateInputRepairS96() || !CanChangeRecipeS134())
            {
                HeaderStatusText.Text = "流程上下文已变化，本次未切换；请重新选择。";
                RefreshProjectFlowPickerS104();
                return;
            }
            if (!CanSelectConfiguredFlowS137(id)) { RefreshProjectFlowPickerS104(); return; }
            SaveCurrentProjectFlowS104();
            bool tracking = recipeTrackingSuspended;
            recipeTrackingSuspended = true;
            try
            {
                ClearInputRepairNavigationS96(); ClearSubflowTraceS102(); ClearShapeTrainingPreviewS89();
                ClearImageQueue(true); CancelRoiGeometryDraft(false); ClearPendingRoi();
                activeProjectFlowIdS104 = id;
                ApplyProjectFlowDataS104(target);
            }
            catch
            {
                // Restore the definition, never partially constructed runtime outputs.
                activeProjectFlowIdS104 = previous.FlowId;
                ApplyProjectFlowDataS104(previous);
                throw;
            }
            finally { recipeTrackingSuspended = tracking; RefreshProjectFlowPickerS104(); }
            ScheduleRecipeStateCheck();
            HeaderStatusText.Text = "当前流程：" + target.Name + "；其他流程已保留。图像与运行结果已清空，请载入本流程图像或运行其图像源。";
            LogInfo(HeaderStatusText.Text);
        }

        private void ApplyProjectFlowDataS104(VmProjectFlowData flow)
        {
            LoadResultLayerStyles(new VisionRecipe { ResultLayerStyles = flow.ResultLayerStyles });
            var policy = (flow.RunPolicy ?? new VmFlowRunPolicy()).Normalize();
            ContinuousIntervalTextBox.Text = policy.ContinuousIntervalMilliseconds.ToString(CultureInfo.InvariantCulture);
            FlowTimeoutTextBox.Text = policy.FlowTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture);
            StopOnNgCheckBox.IsChecked = policy.StopOnNg;
            runTimer.Interval = TimeSpan.FromMilliseconds(policy.ContinuousIntervalMilliseconds);
            suppressResultLayerStyleCapture = true;
            try { ApplyFlowFromRecipe(new VisionRecipe { ToolFlow = flow.Tools.Select(VmSubflowData.CloneTool).ToList(), EnableShapeMatch = false }); }
            finally { suppressResultLayerStyleCapture = false; }
            ResetFlowEditHistory(); ResetFlowDebugRuntimeState(true);
            ResetActiveFlowPresentationS104();
            ProjectFlowViewS104 view;
            if (projectFlowViewsS104.TryGetValue(flow.FlowId, out view))
            {
                // Never restore old shared resources through another flow's history.
                if (view.SharedFingerprint == CaptureSharedFlowFingerprintS104())
                {
                    flowUndoHistory.AddRange(view.Undo); flowRedoHistory.AddRange(view.Redo);
                }
                FlowZoomSlider.Value = view.Zoom;
                FlowToolList.SelectedItem = flowTools.FirstOrDefault(tool => tool.ToolId == view.SelectedId) ?? flowTools.FirstOrDefault();
                RefreshFlowGraphLayout();
                FlowGraphViewport.ScrollToHorizontalOffset(view.X); FlowGraphViewport.ScrollToVerticalOffset(view.Y);
            }
            else { RefreshFlowGraphLayout(); FitFlowButton_Click(null, null); }
            RefreshFlowEditHistoryState(); RefreshResultOverlayLayers(); RefreshGlobalVariableSubscriptions(); RefreshExternalResourceReferences();
            RefreshUiState(); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
        }

        private void ResetActiveFlowPresentationS104()
        {
            InvalidateProductionResultS141("当前流程已切换或初始化；等待该流程的新生产周期。");
            SetProductionRunSaveStatusS135("当前流程尚未运行；已有周期记录仍可查询");
            if (FlowRunStatusText == null) return;
            FlowRunStatusText.Text = "当前流程等待运行"; FlowRunResultText.Text = "--";
            FlowRunElapsedText.Text = "--"; FlowStopReasonText.Text = "--"; FlowCurrentToolText.Text = "--";
            FlowRunRangeText.Text = "范围：当前流程"; FlowRuntimeStatusBarText.Text = "当前流程：" + GetProjectFlowNameS104();
            lastResultPayload = null; LastMessageText.Text = "当前流程尚无可外发结果";
        }

        private void ProjectFlowPickerS104_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changingProjectFlowS104) return;
            string id = ProjectFlowPickerS104.SelectedValue as string;
            if (id == null || id == activeProjectFlowIdS104) return;
            RunUiAction("切换流程", delegate { SwitchProjectFlowS104(id); });
            RefreshProjectFlowPickerS104();
        }

        private List<ResultLayerStyleRecipeData> CaptureProjectFlowStylesS104()
        {
            foreach (var layer in resultOverlayLayers) StoreResultLayerStyle(layer);
            var tools = new HashSet<string>(VmSubflowData.EnumerateTools(CaptureRootFlowRecipeS102()).Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
            return resultLayerStyles.Values.Where(style => tools.Contains(style.ToolId ?? string.Empty)).Select(style => style.Clone()).ToList();
        }

        private void OpenProjectFlowMenuS104_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.ContextMenu == null) return;
            button.ContextMenu.PlacementTarget = button; button.ContextMenu.Placement = PlacementMode.Bottom; button.ContextMenu.IsOpen = true;
        }

        private void LoadMultiFlowExampleS104_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("multi-flow-project");
        }

        private bool BuildMultiFlowExampleS104()
        {
            BuildIlluminationExampleS94();
            var illumination = new VmProjectFlowData { Name = "工位 1 · 亮度检测", Tools = CaptureRootFlowRecipeS102(), RunPolicy = new VmFlowRunPolicy() };
            DisposeAllShapeToolResources(); DisposeFlowTools(); flowTools.Clear();
            BuildGeometryReuseExampleS103();
            var circle = new VmProjectFlowData { Name = "工位 2 · 圆弧测量", Tools = CaptureRootFlowRecipeS102(), RunPolicy = new VmFlowRunPolicy() };
            ClearImageQueue(true);
            projectFlowsS104.Clear(); projectFlowViewsS104.Clear(); deletedProjectFlowsS104.Clear();
            projectFlowsS104.Add(illumination); projectFlowsS104.Add(circle);
            activeProjectFlowIdS104 = illumination.FlowId;
            ApplyProjectFlowDataS104(illumination);
            // Teaching fixtures must neither connect devices nor enable output/trigger side effects.
            AutoSendMatchResultCheckBox.IsChecked = false;
            RefreshProjectFlowPickerS104();
            LoadTcpTriggerConfigS104(new VisionRecipe { TcpFlowTrigger = new VmTcpFlowTriggerData { FlowId = circle.FlowId, Command = "RUN_CIRCLE" } });
            return true;
        }

        private void RestoreDeletedProjectFlowS104_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("恢复已删除流程");
            if (editLeaseS137 < 0) return;
            RunUiAction("恢复已删除流程", delegate
            {
                if (!PrepareProjectFlowChangeS104()) return;
                if (deletedProjectFlowsS104.Count == 0) { HeaderStatusText.Text = "本次打开方案没有可恢复的流程。"; return; }
                StackPanel content; Button restore;
                var window = CreateBatchEditWindowS99("已删除流程 · 本次方案会话", out content, out restore);
                window.Height = 350; window.MinHeight = 280; restore.Content = "恢复到方案";
                content.Children.Add(new TextBlock { Text = "保留最近 10 条；恢复配置和连接，不恢复运行结果、不自动运行。关闭或重新打开方案会清空此列表。", TextWrapping = TextWrapping.Wrap });
                var list = new ListBox { ItemsSource = deletedProjectFlowsS104.AsEnumerable().Reverse().ToList(), DisplayMemberPath = "DisplayText", Height = 165, Margin = new Thickness(0,8,0,8), SelectedIndex = 0 };
                content.Children.Add(list);
                var discard = new Button { Content = "永久移除此项…", HorizontalAlignment = HorizontalAlignment.Left };
                content.Children.Add(discard);
                discard.Click += delegate
                {
                    var item = list.SelectedItem as DeletedProjectFlowS104;
                    if (item == null || MessageBox.Show(window, "永久移除此恢复副本？其共享资源引用保护也会解除。", "移除恢复副本", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    deletedProjectFlowsS104.Remove(item);
                    list.ItemsSource = deletedProjectFlowsS104.AsEnumerable().Reverse().ToList(); list.SelectedIndex = 0;
                    restore.IsEnabled = deletedProjectFlowsS104.Count > 0;
                    RefreshRoiLayerBindingSummaries(); RefreshExternalResourceReferences(); RefreshGlobalVariableSubscriptions();
                };
                restore.Click += delegate { if (list.SelectedItem != null) window.DialogResult = true; };
                if (window.ShowDialog() != true) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                var selected = list.SelectedItem as DeletedProjectFlowS104;
                if (selected == null) return;
                var flow = selected.Flow.Clone();
                if (projectFlowsS104.Any(item => item.FlowId == flow.FlowId)) throw new InvalidOperationException("此流程标识已存在，未覆盖现有流程。");
                string seed = flow.Name; int suffix = 2;
                while (projectFlowsS104.Any(item => string.Equals(item.Name, flow.Name, StringComparison.OrdinalIgnoreCase))) flow.Name = seed + " 恢复 " + suffix++;
                projectFlowsS104.Insert(Math.Min(selected.Index, projectFlowsS104.Count), flow);
                deletedProjectFlowsS104.Remove(selected);
                DisarmTcpTriggerS104("方案流程已恢复，请确认后重新启用");
                RefreshProjectFlowPickerS104(); RefreshRoiLayerBindingSummaries(); RefreshExternalResourceReferences(); RefreshGlobalVariableSubscriptions();
                ScheduleRecipeStateCheck();
                HeaderStatusText.Text = "已恢复“" + flow.Name + "”；从流程下拉框选择它，保存方案后保留。未切换当前工作区或自动运行。";
            });
        }

        private void EditProjectFlowS104_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("修改方案流程");
            if (editLeaseS137 < 0) return;
            RunUiAction("管理流程", delegate
            {
                if (!PrepareProjectFlowChangeS104()) return;
                var item = sender as MenuItem; string action = item == null ? null : item.Tag as string;
                var current = projectFlowsS104.First(flow => flow.FlowId == activeProjectFlowIdS104);
                if (action == "Delete")
                {
                    if (projectFlowsS104.Count == 1) { HeaderStatusText.Text = "方案至少保留一条流程；可以删除其模块以保持空白。"; return; }
                    if (MessageBox.Show(this, "删除流程“" + current.Name + "”及其全部模块？其他流程和共享资源保留。此操作不能通过流程编辑撤销恢复。",
                        "删除流程", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    SwitchProjectFlowS104(projectFlowsS104.First(flow => flow.FlowId != current.FlowId).FlowId);
                    if (activeProjectFlowIdS104 == current.FlowId) return;
                    deletedProjectFlowsS104.Add(new DeletedProjectFlowS104 { Flow = current.Clone(), Index = projectFlowsS104.IndexOf(current), DeletedAt = DateTime.Now });
                    while (deletedProjectFlowsS104.Count > 10) deletedProjectFlowsS104.RemoveAt(0);
                    projectFlowsS104.Remove(current); projectFlowViewsS104.Remove(current.FlowId);
                }
                else
                {
                    StackPanel content; Button apply;
                    var window = CreateBatchEditWindowS99(action == "Rename" ? "重命名流程" : action == "Copy" ? "复制流程" : "新建空白流程", out content, out apply);
                    window.Width = 420; window.MinWidth = 360; window.Height = 225; window.MinHeight = 210;
                    apply.Content = action == "Rename" ? "保存名称" : action == "Copy" ? "创建副本" : "创建流程";
                    string proposed = action == "Rename" ? current.Name : action == "Copy" ? current.Name + " 副本" : "流程 " + (projectFlowsS104.Count + 1).ToString("00");
                    string seed = proposed; int suffix = 2;
                    while (action != "Rename" && projectFlowsS104.Any(flow => string.Equals(flow.Name, proposed, StringComparison.OrdinalIgnoreCase))) proposed = seed + " " + suffix++;
                    content.Children.Add(new TextBlock { Text = "流程名称", Margin = new Thickness(0,8,0,4) });
                    var name = new TextBox { Text = proposed, MaxLength = 80, MinWidth = 220 }; content.Children.Add(name);
                    var error = new TextBlock { Foreground = System.Windows.Media.Brushes.Firebrick, TextWrapping = TextWrapping.Wrap }; content.Children.Add(error);
                    apply.Click += delegate
                    {
                        string value = name.Text.Trim();
                        if (value.Length == 0 || projectFlowsS104.Any(flow => (action != "Rename" || flow != current) && string.Equals(flow.Name, value, StringComparison.OrdinalIgnoreCase)))
                        { error.Text = "请输入不重复的流程名称。"; return; }
                        window.DialogResult = true;
                    };
                    if (window.ShowDialog() != true) return;
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    if (action == "Rename") current.Name = name.Text.Trim();
                    else
                    {
                        SaveCurrentProjectFlowS104();
                        var authorizedAlignmentCopies = action == "Copy" ? CaptureCurrentAlignmentReferenceIdsS132() : null;
                        var authorizedVariationCopies = action == "Copy" ? CaptureCurrentVariationDatasetIdsS130() : null;
                        var created = action == "Copy" ? current.CreateCopy(name.Text.Trim()) : new VmProjectFlowData { Name = name.Text.Trim() };
                        var variationCopyMap = action == "Copy" ? current.Tools.Select((tool, index) => new { Old = tool.ToolId, New = created.Tools[index].ToolId })
                            .ToDictionary(pair => pair.Old, pair => pair.New, StringComparer.OrdinalIgnoreCase) : null;
                        projectFlowsS104.Add(created); SwitchProjectFlowS104(created.FlowId);
                        if (variationCopyMap != null && activeProjectFlowIdS104 == created.FlowId)
                        {
                            RebindCopiedAlignmentReferencesS132(flowTools, variationCopyMap, authorizedAlignmentCopies);
                            RebindCopiedVariationModelsS130(flowTools, variationCopyMap, authorizedVariationCopies);
                        }
                    }
                }
                RefreshProjectFlowPickerS104(); RefreshRoiLayerBindingSummaries(); RefreshExternalResourceReferences(); RefreshGlobalVariableSubscriptions();
                ScheduleRecipeStateCheck();
            });
        }
    }
}
