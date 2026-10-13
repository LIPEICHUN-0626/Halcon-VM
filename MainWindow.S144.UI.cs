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
        private Window objectDetailsWindowS144;
        private VmToolInstance objectDetailsToolS144;
        private DateTime? objectDetailsOutputUtcS144;
        private string objectDetailsContextS144;
        private List<VmConnectedObjectRowS144> objectDetailsRowsS144;
        private int objectViewIndexS144 = -1;
        private bool objectShowNumbersS144;

        private void LoadConnectedObjectsDraftS144(VmToolParameterData parameters)
        {
            var p = parameters.ConnectedObjectsS144 ?? new VmConnectedObjectParametersS144();
            DockObjectsMinimumS144.Text = p.MinimumArea.ToString("R", CultureInfo.InvariantCulture);
            DockObjectsMaximumS144.Text = p.MaximumArea.ToString("R", CultureInfo.InvariantCulture);
            DockObjectsSortS144.SelectedValue = p.SortMode;
            DockObjectsIndexS144.Text = p.SelectionIndex.ToString(CultureInfo.InvariantCulture);
        }

        private bool ReadConnectedObjectsDraftS144(VmToolParameterData parameters)
        {
            double minimum, maximum; int index;
            if (!TryParseUiDouble(DockObjectsMinimumS144.Text, out minimum) || !TryParseUiDouble(DockObjectsMaximumS144.Text, out maximum) ||
                !int.TryParse(DockObjectsIndexS144.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                return FailDockDraft("对象面积/编号须为明确数值，不能用旧值补充。");
            var p = new VmConnectedObjectParametersS144 { MinimumArea = minimum, MaximumArea = maximum,
                SortMode = DockObjectsSortS144.SelectedValue as string, SelectionIndex = index };
            string error = p.Validate(); if (error.Length > 0) return FailDockDraft(error);
            parameters.ConnectedObjectsS144 = p; return true;
        }

        private List<VmConnectedObjectRowS144> ReadObjectDetailRowsS144(VmToolInstance tool, out string context)
        {
            context = null;
            if (tool == null || tool.Kind != VmToolKind.ConnectedObjectAnalysisS144 || tool.ResultCode != "OK" ||
                !string.IsNullOrWhiteSpace(tool.ErrorMessage) || !tool.LastOutputUtc.HasValue)
                throw new InvalidOperationException("先F5当前对象分析，才有本次可查看的对象。");
            object rawCenters, rawAreas, rawIndices, rawSelection;
            if (!tool.TryGetOutputValue("Centers", out rawCenters) || !tool.TryGetOutputValue("Areas", out rawAreas) ||
                !tool.TryGetOutputValue("SourceIndices", out rawIndices) || !tool.TryGetOutputValue("HasSelection", out rawSelection) || !(rawSelection is bool))
                throw new InvalidOperationException("本次同序对象输出缺失，不从摘要补齐。");
            var centers = rawCenters as VmPoint2DArray; var areas = rawAreas as VmNumberArrayS105; var indices = rawIndices as VmNumberArrayS105;
            if (centers == null || centers.Points == null || centers.Points.Count > 1000 || centers.GetValidationError(true).Length > 0 ||
                areas == null || indices == null || areas.Count != centers.Points.Count || indices.Count != centers.Points.Count ||
                !tool.TryGetOutputCoordinateContext("Centers", out context) || context != centers.CoordinateContextId)
                throw new InvalidOperationException("对象数组长度/类型/坐标不一致，不能显示错序明细。");
            double selectedIndex = -1;
            bool hasSelection = (bool)rawSelection;
            if (hasSelection && (!tool.TryGetNumericOutput("SelectedIndex", out selectedIndex) || selectedIndex < 0 ||
                selectedIndex >= areas.Count || selectedIndex != Math.Truncate(selectedIndex)))
                throw new InvalidOperationException("正式选取编号无效。");
            var rows = new List<VmConnectedObjectRowS144>(); var sourceIndices = new HashSet<int>();
            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i] < 1 || areas[i] > 100000 || indices[i] < 0 || indices[i] >= 1000 || indices[i] != Math.Truncate(indices[i]) || !sourceIndices.Add((int)indices[i]))
                    throw new InvalidOperationException("对象面积或原连接编号无效。");
                rows.Add(new VmConnectedObjectRowS144 { Index = i, SourceIndex = (int)indices[i], Area = areas[i],
                    Row = centers.Points[i].Row, Column = centers.Points[i].Column, Selected = i == selectedIndex });
            }
            return rows;
        }

        private bool IsObjectDetailCurrentS144(VmToolInstance tool)
        {
            string context;
            return tool != null && tool == objectDetailsToolS144 && tool == GetImageTaskToolS89() && tool.LastOutputUtc == objectDetailsOutputUtcS144 &&
                tool.ResultCode == "OK" && string.IsNullOrWhiteSpace(tool.ErrorMessage) &&
                tool.TryGetOutputCoordinateContext("Centers", out context) && context == objectDetailsContextS144;
        }

        private void ShowObjectDetailsS144_Click(object sender, RoutedEventArgs e)
        {
            var tool = dockEditingTool;
            try
            {
                string context; var rows = ReadObjectDetailRowsS144(tool, out context);
                if (objectDetailsWindowS144 != null) objectDetailsWindowS144.Close();
                var root = new DockPanel { Margin = new Thickness(12) };
                var heading = new TextBlock { Text = tool.InstanceName + " · " + rows.Count + " 个对象 · 只读冻结（编号从0开始）",
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8),
                    ToolTip = "明细只保留本次类型化值，不持有HALCON对象。重跑后旧明细仍可阅读，但不能在新图上定位或写入新草稿。面积px²，中心px；选中列是冻结时正式输出。" };
                DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
                var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                var numbers = new CheckBox { Content = "图上编号（最多100个）", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
                var draft = new Button { Content = "将查看编号填入草稿", ToolTip = "仅填写选取编号，不自动应用、运行或修改下游。锁定、运行中或本次输出失效时拒绝。" };
                actions.Children.Add(numbers); actions.Children.Add(draft); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
                var status = new TextBlock { Text = "选行只定位查看；正式选取在原参数小窗设置。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
                DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
                var grid = new DataGrid { ItemsSource = rows, IsReadOnly = true, AutoGenerateColumns = false,
                    CanUserAddRows = false, CanUserDeleteRows = false, CanUserSortColumns = false,
                    SelectionMode = DataGridSelectionMode.Single, EnableRowVirtualization = true };
                foreach (var pair in new[] { new[] { "编号(0起)", "Index" }, new[] { "原连接编号(0起)", "SourceIndex" },
                    new[] { "面积 px²", "Area" }, new[] { "中心行 px", "Row" }, new[] { "中心列 px", "Column" }, new[] { "正式选中", "Selected" } })
                    grid.Columns.Add(new DataGridTextColumn { Header = pair[0], Binding = new Binding(pair[1]) { StringFormat = "{0:0.###}" } });
                root.Children.Add(grid);
                var window = new Window { Owner = this, Title = "对象明细", Content = root, Width = 650, Height = 420,
                    MinWidth = 500, MinHeight = 280, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                objectDetailsWindowS144 = window; objectDetailsToolS144 = tool; objectDetailsRowsS144 = rows;
                objectDetailsOutputUtcS144 = tool.LastOutputUtc; objectDetailsContextS144 = context;
                objectViewIndexS144 = -1; objectShowNumbersS144 = true;
                grid.SelectionChanged += delegate
                {
                    var row = grid.SelectedItem as VmConnectedObjectRowS144;
                    if (row == null || !IsObjectDetailCurrentS144(tool)) { status.Text = "只读旧快照：当前模块/输出已改变，不在新图定位。"; objectViewIndexS144 = -1; ScheduleRefreshDisplay(); return; }
                    objectViewIndexS144 = row.Index;
                    SetImageContextSelection(VmImageContextOption.ModuleInput); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
                    status.Text = "查看 #" + row.Index + " · 正式选取未改变";
                };
                numbers.Checked += delegate { objectShowNumbersS144 = true; ScheduleRefreshDisplay(); };
                numbers.Unchecked += delegate { objectShowNumbersS144 = false; ScheduleRefreshDisplay(); };
                draft.Click += delegate
                {
                    var row = grid.SelectedItem as VmConnectedObjectRowS144;
                    if (row == null || dockEditingTool != tool || !IsObjectDetailCurrentS144(tool)) { status.Text = "当前明细/模块已改变，不能写入草稿；请重新查看。"; return; }
                    if (!RequireEngineeringEditS137("选取对象编号") || !CanChangeRecipeS134(false) || isFlowExecutionActive ||
                        isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
                    { status.Text = "当前不允许编辑；查看不会修改正式输出。"; return; }
                    DockObjectsIndexS144.Text = row.Index.ToString(CultureInfo.InvariantCulture);
                    status.Text = "#" + row.Index + " 已填入草稿，尚未应用；回原小窗F5/应用。";
                };
                window.Closed += delegate
                {
                    if (objectDetailsWindowS144 != window) return;
                    objectDetailsWindowS144 = null; objectDetailsToolS144 = null; objectDetailsRowsS144 = null;
                    objectDetailsContextS144 = null; objectDetailsOutputUtcS144 = null; objectViewIndexS144 = -1; objectShowNumbersS144 = false; ScheduleRefreshDisplay();
                };
                window.Show(); SetImageContextSelection(VmImageContextOption.ModuleInput); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
            }
            catch (Exception error) { HeaderStatusText.Text = "对象明细未打开：" + error.Message; }
        }

        private void DrawConnectedObjectsS144(HWindow window, VmToolInstance tool, bool left)
        {
            if (tool == null || tool.Kind != VmToolKind.ConnectedObjectAnalysisS144 || window == null || historicalImageMode) return;
            var image = left ? ResolveDualInputImageContext() : ResolveImageContext();
            string wanted = GetImageCoordinateContextS112(image), context;
            if (!image.HasImage || string.IsNullOrWhiteSpace(wanted)) return;
            try
            {
                var rows = ReadObjectDetailRowsS144(tool, out context); if (context != wanted) return;
                var formal = rows.SingleOrDefault(row => row.Selected);
                if (formal != null)
                {
                    object raw;
                    if (tool.TryGetOutputValue("SelectedRegion", out raw) && raw is VmRegionSnapshot && !((VmRegionSnapshot)raw).IsDisposed)
                        using (var region = ((VmRegionSnapshot)raw).CreateObjectAnalysisCopyS144())
                        { window.SetColor("green"); window.SetDraw("margin"); window.SetLineWidth(3); window.DispObj(region); }
                }
                if (!IsObjectDetailCurrentS144(tool)) return;
                if (objectShowNumbersS144)
                    foreach (var row in objectDetailsRowsS144.Take(100)) window.DispText("#" + row.Index, "image", row.Row, row.Column, row.Selected ? "green" : "cyan", "box", "false");
                var viewed = objectDetailsRowsS144.SingleOrDefault(row => row.Index == objectViewIndexS144);
                if (viewed != null)
                {
                    object raw;
                    if (tool.TryGetOutputValue("Region", out raw) && raw is VmRegionSnapshot && !((VmRegionSnapshot)raw).IsDisposed)
                        using (var selected = ((VmRegionSnapshot)raw).CreateObjectAnalysisItemS144(viewed.Index))
                        { window.SetColor("magenta"); window.SetDraw("margin"); window.SetLineWidth(3); window.DispObj(selected); }
                    window.DispText("查看 #" + viewed.Index, "image", viewed.Row + 8, viewed.Column + 8, "magenta", "box", "false");
                }
            }
            catch (InvalidOperationException) { }
            catch (HalconException) { }
        }
    }
}
