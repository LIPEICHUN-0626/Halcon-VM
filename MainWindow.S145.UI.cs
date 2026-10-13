using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private Window templateTargetDetailsWindowS145;
        private TextBlock templateTargetDetailsStatusS145;
        private VmToolInstance templateTargetDetailsToolS145;
        private DateTime? templateTargetDetailsOutputUtcS145;
        private long templateTargetDetailsVersionS145;
        private string templateTargetDetailsContextS145, templateTargetDetailsInputS145, templateTargetDetailsSortS145;
        private List<TemplateTargetRowS145> templateTargetDetailsRowsS145;
        private int templateTargetViewIndexS145 = -1;
        private bool templateTargetShowNumbersS145, templateTargetDetailsValidS145;

        private sealed class TemplateTargetRowS145
        {
            public int Index { get; set; }
            public int SourceIndex { get; set; }
            public double Score { get; set; }
            public double Row { get; set; }
            public double Column { get; set; }
            public double AngleDegrees { get; set; }
            public double Scale { get; set; }
            public bool Selected { get; set; }
            public string SelectedText { get { return Selected ? "是" : "—"; } }
        }

        private static bool IsTemplateTargetKindS145(VmToolInstance tool)
        { return tool != null && (tool.Kind == VmToolKind.ShapeMatch || tool.Kind == VmToolKind.NccTemplateMatch); }

        private void LoadTemplateTargetsDraftS145(VmToolParameterData parameters)
        {
            var p = parameters.TemplateTargetsS145 ?? new VmTemplateTargetParametersS145();
            DockTemplateTargetSortS145.SelectedValue = p.SortMode;
            DockTemplateTargetIndexS145.Text = p.SelectionIndex.ToString(CultureInfo.InvariantCulture);
        }

        private bool ReadTemplateTargetsDraftS145(VmToolParameterData parameters)
        {
            int index;
            if (!int.TryParse(DockTemplateTargetIndexS145.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                return FailDockDraft("模板选取编号须是从0开始的整数，不从旧值补充。");
            var p = new VmTemplateTargetParametersS145 { SortMode = DockTemplateTargetSortS145.SelectedValue as string, SelectionIndex = index };
            string error = p.Validate();
            if (!string.IsNullOrEmpty(error)) return FailDockDraft(error);
            // Merely applying an old best-only module must not enable a new
            // explicit-input requirement when the new selection is unchanged.
            if (parameters.TemplateTargetsS145 == null && p.SortMode == "score" && p.SelectionIndex == 0) return true;
            parameters.TemplateTargetsS145 = p;
            return true;
        }

        // This task is outside either original parameter panel, and is deliberately
        // not named *Panel: the original common-parameter scanner keeps its one panel.
        // Call at the end of UpdateDockAdvancedParameterLayout, including collapse.
        private void SetTemplateTargetsPanelS145(VmToolInstance tool)
        {
            if (DockTemplateTargetsS145Task == null) return;
            bool expanded;
            DockTemplateTargetsS145Task.Visibility = IsTemplateTargetKindS145(tool) &&
                dockAdvancedParameterExpandedByKind.TryGetValue(tool.Kind, out expanded) && expanded
                ? Visibility.Visible : Visibility.Collapsed;
            DockTemplateTargetDetailsS145.IsEnabled = IsTemplateTargetKindS145(tool) && !TemplateTargetInteractionBusyS145() &&
                tool.LastOutputUtc.HasValue && string.IsNullOrWhiteSpace(tool.ErrorMessage) && (tool.ResultCode == "OK" || tool.ResultCode == "NG");
            if (templateTargetDetailsValidS145 && !IsTemplateTargetDetailCurrentS145(templateTargetDetailsToolS145))
                InvalidateTemplateTargetViewS145("模块、输入或本次输出已改变；旧明细只供阅读。");
        }

        private bool TemplateTargetInteractionBusyS145()
        {
            return isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || subflowTrialActiveS102 ||
                IsCurrentToolContinuousTrialRunningS87 || nccTrainingCancellationS131 != null ||
                System.Threading.Volatile.Read(ref tcpTriggerPendingS104) != 0 || !CanChangeRecipeS134(false);
        }

        private string ReadTemplateTargetInputIdentityS145(VmToolInstance tool)
        {
            var binding = tool == null ? null : tool.GetInputBinding("Image");
            if (binding == null || !string.IsNullOrWhiteSpace(GetInputBindingConfigurationError(tool, binding)))
                throw new InvalidOperationException("没有有效的正式 Image 订阅，不回退全局图像。");
            var source = GetInputSourceTool(binding);
            var snapshot = GetImageSnapshot(source, binding.SourcePortName);
            if (source == null || !source.IsEnabled || source.ResultCode != "OK" || !source.LastOutputUtc.HasValue ||
                !tool.LastOutputUtc.HasValue || source.LastOutputUtc.Value > tool.LastOutputUtc.Value || snapshot == null || snapshot.IsDisposed)
                throw new InvalidOperationException("输入未运行、已释放或比本次匹配更新，不能把旧目标画到新帧。");
            string context = ResolveOutputCoordinateContextS102(source, binding.SourcePortName);
            if (string.IsNullOrWhiteSpace(context) || context == "Global.Image" || context == "无效来源" || IsPendingCoordinateContextS102(context))
                throw new InvalidOperationException("输入图像缺少明确的当前像素坐标身份。");
            var values = new[] { binding.SourceToolId, binding.SourcePortName, context,
                source.LastOutputUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture), source.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture),
                snapshot.ProducerToolId, snapshot.ProducerPortName, snapshot.SerialNumber, snapshot.SourcePath,
                snapshot.Width.ToString(CultureInfo.InvariantCulture), snapshot.Height.ToString(CultureInfo.InvariantCulture) };
            // Only immutable strings/timestamps are held. No image/model SDK handle
            // or VmImageSnapshot is retained by the frozen detail window.
            return string.Concat(values.Select(value => (value ?? "").Length.ToString(CultureInfo.InvariantCulture) + ":" + (value ?? "")));
        }

        private List<TemplateTargetRowS145> ReadTemplateTargetRowsS145(VmToolInstance tool, out string context, out string sortMode)
        {
            context = null; sortMode = null;
            if (!IsTemplateTargetKindS145(tool) || !tool.LastOutputUtc.HasValue || !string.IsNullOrWhiteSpace(tool.ErrorMessage) ||
                (tool.ResultCode != "OK" && tool.ResultCode != "NG"))
                throw new InvalidOperationException("先F5当前模板模块，才有本次类型化目标；不读取历史摘要。");
            object rawCenters, rawScores, rawAngles, rawScales, rawIndices, rawSelection, rawSort;
            if (!tool.TryGetOutputValue("Centers", out rawCenters) || !tool.TryGetOutputValue("Scores", out rawScores) ||
                !tool.TryGetOutputValue("Angles", out rawAngles) || !tool.TryGetOutputValue("Scales", out rawScales) ||
                !tool.TryGetOutputValue("SourceIndices", out rawIndices) || !tool.TryGetOutputValue("HasSelection", out rawSelection) ||
                !(rawSelection is bool) || !tool.TryGetOutputValue("TargetSortMode", out rawSort) || !(rawSort is string))
            {
                object status;
                string detail = tool.TryGetOutputValue("TargetStatus", out status) ? status as string : null;
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)
                    ? "本次同序目标输出缺失，不解析 Candidates 文本补造数组。" : detail);
            }
            var centers = rawCenters as VmPoint2DArray;
            var scores = rawScores as VmNumberArrayS105; var angles = rawAngles as VmNumberArrayS105;
            var scales = rawScales as VmNumberArrayS105; var indices = rawIndices as VmNumberArrayS105;
            sortMode = (string)rawSort;
            double count;
            if ((sortMode != "score" && sortMode != "row" && sortMode != "column") || centers == null || centers.Points == null ||
                centers.Points.Count > (tool.Kind == VmToolKind.NccTemplateMatch ? 256 : 1000) || centers.GetValidationError(true).Length > 0 || centers.CoordinateContext != VmPoint2D.ImagePixelContext ||
                scores == null || angles == null || scales == null || indices == null || scores.Count != centers.Points.Count ||
                angles.Count != scores.Count || scales.Count != scores.Count || indices.Count != scores.Count ||
                !tool.TryGetNumericOutput("TargetCount", out count) || count != scores.Count ||
                !tool.TryGetOutputCoordinateContext("Centers", out context) || string.IsNullOrWhiteSpace(context) || context != centers.CoordinateContextId ||
                context == "Global.Image" || IsPendingCoordinateContextS102(context))
                throw new InvalidOperationException("目标数组的类型、完整数量、排序或坐标不一致，拒绝错序明细。");
            if (ResolveTargetImageContextIdS86(tool) != context)
                throw new InvalidOperationException("目标已不属于当前输入图像链，不能在当前图像定位。");
            ReadTemplateTargetInputIdentityS145(tool);
            double selectedIndex = -1;
            bool hasSelection = (bool)rawSelection;
            if (hasSelection && (!tool.TryGetNumericOutput("SelectedIndex", out selectedIndex) || !FiniteTemplateTargetNumberS145(selectedIndex) ||
                selectedIndex < 0 || selectedIndex >= scores.Count || selectedIndex != Math.Truncate(selectedIndex)))
                throw new InvalidOperationException("正式选取编号无效，不能用查看编号替代。");
            var rows = new List<TemplateTargetRowS145>(); var identities = new HashSet<int>();
            for (int i = 0; i < scores.Count; i++)
            {
                if (!FiniteTemplateTargetNumberS145(scores[i]) || scores[i] < 0 || scores[i] > 1 || !FiniteTemplateTargetNumberS145(angles[i]) ||
                    !FiniteTemplateTargetNumberS145(scales[i]) || scales[i] <= 0 || !FiniteTemplateTargetNumberS145(indices[i]) ||
                    indices[i] < 0 || indices[i] >= 1000 || indices[i] != Math.Truncate(indices[i]) || !identities.Add((int)indices[i]) ||
                    (tool.Kind == VmToolKind.NccTemplateMatch && scales[i] != 1))
                    throw new InvalidOperationException("真实分数、角度、尺度或原编号无效；不钳制或改造来源。");
                rows.Add(new TemplateTargetRowS145 { Index = i, SourceIndex = (int)indices[i], Score = scores[i],
                    Row = centers.Points[i].Row, Column = centers.Points[i].Column, AngleDegrees = angles[i], Scale = scales[i], Selected = i == selectedIndex });
            }
            return rows;
        }

        private static bool FiniteTemplateTargetNumberS145(double value)
        { return !double.IsNaN(value) && !double.IsInfinity(value); }

        private bool IsTemplateTargetDetailCurrentS145(VmToolInstance tool)
        {
            if (!templateTargetDetailsValidS145 || tool == null || !ReferenceEquals(tool, templateTargetDetailsToolS145) ||
                !ReferenceEquals(tool, GetImageTaskToolS89()) || tool.LastOutputUtc != templateTargetDetailsOutputUtcS145 ||
                tool.RuntimeOutputVersion != templateTargetDetailsVersionS145 || !string.IsNullOrWhiteSpace(tool.ErrorMessage) ||
                (tool.ResultCode != "OK" && tool.ResultCode != "NG")) return false;
            string context; object sort;
            if (!tool.TryGetOutputCoordinateContext("Centers", out context) || context != templateTargetDetailsContextS145 ||
                !tool.TryGetOutputValue("TargetSortMode", out sort) || !string.Equals(sort as string, templateTargetDetailsSortS145, StringComparison.Ordinal)) return false;
            try { return ReadTemplateTargetInputIdentityS145(tool) == templateTargetDetailsInputS145; }
            catch (InvalidOperationException) { return false; }
        }

        private void InvalidateTemplateTargetViewS145(string reason)
        {
            templateTargetDetailsValidS145 = false; templateTargetViewIndexS145 = -1;
            if (templateTargetDetailsStatusS145 != null) templateTargetDetailsStatusS145.Text = "只读旧快照 · " + (reason ?? "本次输出已失效，不能定位或填入草稿。");
        }

        private void ShowTemplateTargetDetailsS145_Click(object sender, RoutedEventArgs e)
        {
            var tool = dockEditingTool;
            if (TemplateTargetInteractionBusyS145()) { HeaderStatusText.Text = "运行、训练或切换尚未结束，暂不打开多目标明细。"; return; }
            try
            {
                string context, sortMode;
                var rows = ReadTemplateTargetRowsS145(tool, out context, out sortMode);
                string inputIdentity = ReadTemplateTargetInputIdentityS145(tool);
                DateTime? outputUtc = tool.LastOutputUtc; long version = tool.RuntimeOutputVersion;
                if (templateTargetDetailsWindowS145 != null) templateTargetDetailsWindowS145.Close();
                var root = new DockPanel { Margin = new Thickness(12) };
                var formal = rows.FirstOrDefault(row => row.Selected);
                var heading = new TextBlock { Text = tool.InstanceName + " · " + rows.Count + " 个目标 · " + TemplateTargetSortTextS145(sortMode) +
                    " · " + (formal == null ? "正式缺选" : "正式选取 #" + formal.Index), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8),
                    ToolTip = "只读冻结本次类型化值，编号从0开始；原编号是本次HALCON顺序，不是跟踪身份。中心px、角度°、真实尺度无单位。绿色仅标记正式选取，不表示产品合格；Shape刚体Pose不含尺度变换。\nToolId=" + tool.ToolId + "\n坐标=" + context + "\n输出UTC=" + outputUtc.Value.ToString("O", CultureInfo.InvariantCulture) };
                DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
                var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var numbers = new CheckBox { Content = "图上编号（最多100个）", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0),
                    ToolTip = "完整明细最多1000项；图上仅显示前100个标签，选中其余行仍可定位。不是截断正式数组。" };
                var draft = new Button { Content = "将查看目标填入草稿", ToolTip = "同时填写本次冻结的排序规则和0起选取编号；不自动应用、运行或修改最佳输出。锁定、运行或失效时拒绝。" };
                actions.Children.Add(numbers); actions.Children.Add(draft); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
                var status = new TextBlock { Text = rows.Count == 0 ? "本次无匹配 · 数组为空，没有可选目标。" : "选行只高亮查看；正式选取不随查看改变。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) };
                DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
                var grid = new DataGrid { ItemsSource = rows, IsReadOnly = true, AutoGenerateColumns = false, CanUserAddRows = false,
                    CanUserDeleteRows = false, CanUserSortColumns = false, SelectionMode = DataGridSelectionMode.Single, EnableRowVirtualization = true };
                foreach (var field in new[] { new[] { "编号(0起)", "Index", "{0}" }, new[] { "原编号(0起)", "SourceIndex", "{0}" },
                    new[] { "分数", "Score", "{0:0.0000}" }, new[] { "中心行 px", "Row", "{0:0.###}" }, new[] { "中心列 px", "Column", "{0:0.###}" },
                    new[] { "角度 °", "AngleDegrees", "{0:0.###}" }, new[] { "真实尺度", "Scale", "{0:0.####}" }, new[] { "正式选取", "SelectedText", "{0}" } })
                    grid.Columns.Add(new DataGridTextColumn { Header = field[0], Binding = new Binding(field[1]) { StringFormat = field[2] } });
                root.Children.Add(grid);
                var window = new Window { Owner = this, Title = "模板多目标明细 · 只读冻结", Content = root, Width = 790, Height = 430,
                    MinWidth = 650, MinHeight = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                templateTargetDetailsWindowS145 = window; templateTargetDetailsStatusS145 = status; templateTargetDetailsToolS145 = tool;
                templateTargetDetailsRowsS145 = rows; templateTargetDetailsContextS145 = context; templateTargetDetailsInputS145 = inputIdentity;
                templateTargetDetailsOutputUtcS145 = outputUtc; templateTargetDetailsVersionS145 = version; templateTargetDetailsSortS145 = sortMode;
                templateTargetViewIndexS145 = -1; templateTargetShowNumbersS145 = true; templateTargetDetailsValidS145 = true;
                grid.SelectionChanged += delegate
                {
                    var row = grid.SelectedItem as TemplateTargetRowS145;
                    if (row == null) { templateTargetViewIndexS145 = -1; ScheduleRefreshDisplay(); return; }
                    if (!IsTemplateTargetDetailCurrentS145(tool)) { InvalidateTemplateTargetViewS145("模块、输入或本次输出已改变，不在新图定位。"); ScheduleRefreshDisplay(); return; }
                    templateTargetViewIndexS145 = row.Index;
                    SetImageContextSelection(VmImageContextOption.ModuleInput); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
                    status.Text = "查看 #" + row.Index + " · 正式选取未改变";
                };
                numbers.Checked += delegate { templateTargetShowNumbersS145 = true; ScheduleRefreshDisplay(); };
                numbers.Unchecked += delegate { templateTargetShowNumbersS145 = false; ScheduleRefreshDisplay(); };
                draft.Click += delegate
                {
                    var row = grid.SelectedItem as TemplateTargetRowS145;
                    if (row == null || !ReferenceEquals(dockEditingTool, tool) || !IsTemplateTargetDetailCurrentS145(tool))
                    { status.Text = "没有当前可选目标；旧明细不能填入新草稿，请重新查看。"; return; }
                    long lease = CaptureEngineeringEditLeaseS137("将模板目标填入草稿");
                    if (lease < 0 || TemplateTargetInteractionBusyS145() || !IsEngineeringEditLeaseCurrentS137(lease) || !IsTemplateTargetDetailCurrentS145(tool))
                    { status.Text = "当前锁定、运行中或输出失效；正式结果未改变。"; return; }
                    bool updating = dockDraftUpdating;
                    try
                    {
                        dockDraftUpdating = true;
                        DockTemplateTargetSortS145.SelectedValue = sortMode;
                        DockTemplateTargetIndexS145.Text = row.Index.ToString(CultureInfo.InvariantCulture);
                    }
                    finally { dockDraftUpdating = updating; }
                    DockDraftValue_Changed(DockTemplateTargetIndexS145, new RoutedEventArgs());
                    status.Text = TemplateTargetSortTextS145(sortMode) + " / #" + row.Index + " 已填入草稿；回原小窗F5或应用，不自动运行。";
                };
                window.Closed += delegate
                {
                    if (!ReferenceEquals(templateTargetDetailsWindowS145, window)) return;
                    templateTargetDetailsWindowS145 = null; templateTargetDetailsStatusS145 = null; templateTargetDetailsToolS145 = null;
                    templateTargetDetailsRowsS145 = null; templateTargetDetailsContextS145 = null; templateTargetDetailsInputS145 = null;
                    templateTargetDetailsSortS145 = null; templateTargetDetailsOutputUtcS145 = null; templateTargetDetailsValidS145 = false;
                    templateTargetViewIndexS145 = -1; templateTargetShowNumbersS145 = false; ScheduleRefreshDisplay();
                };
                window.Show(); SetImageContextSelection(VmImageContextOption.ModuleInput); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
            }
            catch (InvalidOperationException error) { HeaderStatusText.Text = "多目标明细未打开：" + error.Message; }
        }

        private static string TemplateTargetSortTextS145(string mode)
        { return mode == "row" ? "按行升序" : mode == "column" ? "按列升序" : "按分数降序"; }

        private void DrawTemplateTargetsS145(HWindow window, VmToolInstance tool, bool left)
        {
            if (window == null || !IsTemplateTargetKindS145(tool) || historicalImageMode || !ReferenceEquals(tool, GetImageTaskToolS89())) return;
            var image = left ? ResolveDualInputImageContext() : ResolveImageContext();
            if (image == null || !image.HasImage || image.Snapshot == null || image.Snapshot.IsDisposed) return;
            string wanted = GetImageCoordinateContextS112(image);
            try
            {
                var binding = tool.GetInputBinding("Image");
                if (binding == null || !ReferenceEquals(image.Snapshot, GetImageSnapshot(GetInputSourceTool(binding), binding.SourcePortName))) return;
                string context, sortMode;
                var rows = ReadTemplateTargetRowsS145(tool, out context, out sortMode);
                if (context != wanted) return;
                var formal = rows.FirstOrDefault(row => row.Selected);
                if (formal != null) DrawTemplateTargetMarkerS145(window, formal, "green", "选取 #" + formal.Index, image.Snapshot.Width, image.Snapshot.Height);
                if (!IsTemplateTargetDetailCurrentS145(tool))
                {
                    if (templateTargetDetailsValidS145) InvalidateTemplateTargetViewS145("当前图像/输出已改变，旧明细不能定位或填入草稿。");
                    return;
                }
                if (templateTargetShowNumbersS145)
                    foreach (var row in templateTargetDetailsRowsS145.Take(100))
                        if (row.Row >= 0 && row.Column >= 0 && row.Row < image.Snapshot.Height && row.Column < image.Snapshot.Width)
                            window.DispText("#" + row.Index, "image", row.Row, row.Column, row.Selected ? "green" : "cyan", "box", "false");
                var viewed = templateTargetDetailsRowsS145.FirstOrDefault(row => row.Index == templateTargetViewIndexS145);
                if (viewed != null) DrawTemplateTargetMarkerS145(window, viewed, "magenta", "查看 #" + viewed.Index, image.Snapshot.Width, image.Snapshot.Height);
            }
            catch (InvalidOperationException) { }
            catch (HalconException) { }
        }

        private static void DrawTemplateTargetMarkerS145(HWindow window, TemplateTargetRowS145 row, string color, string text, int width, int height)
        {
            if (row.Row < 0 || row.Column < 0 || row.Row >= height || row.Column >= width) return; // Do not clamp a real point into a false geometry.
            HObject cross = null;
            try
            {
                HOperatorSet.GenCrossContourXld(out cross, row.Row, row.Column, 16, row.AngleDegrees * Math.PI / 180);
                window.SetColor(color); window.SetLineWidth(3); window.DispObj(cross);
                window.DispText(text, "image", row.Row + 10, row.Column + 10, color, "box", "false");
            }
            finally { if (cross != null) cross.Dispose(); }
        }
    }
}
