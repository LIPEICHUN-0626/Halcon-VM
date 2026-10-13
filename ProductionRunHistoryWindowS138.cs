using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class ProductionRunHistoryWindowS135
    {
        private bool trendReadyS138, changingTrendS138;
        private string currentTrendKeyS138 = string.Empty;
        private ProductionTrendServiceS138.Slice trendSliceS138;
        private List<ProductionTrendServiceS138.Metric> trendMetricsS138 = new List<ProductionTrendServiceS138.Metric>();

        private void InitializeTrendS138()
        {
            SetOptions(TrendScopeS138, new[] { Option("Production", "明确生产"), Option("Engineering", "工程运行"), Option("Simulation", "明确模拟"), Option("Unknown", "资格未知 / 旧记录") }, "Production");
            SetOptions(TrendCountS138, new[] { Option("200", "最近 200 周期"), Option("1000", "最近 1000 周期"), Option("2000", "最近 2000 周期") }, "200");
            SetOptions(TrendTimeS138, new[] { Option("1", "最近 24 小时"), Option("7", "最近 7 日"), Option("30", "最近 30 日"), Option("0", "全部已载入时间") }, "1");
            trendReadyS138 = true; RebuildTrendIdentitiesS138();
        }
        public void SetCurrentTrendIdentityS138(string flowId, string key, int version)
        {
            string next = version > 0 && !string.IsNullOrEmpty(key) ? ProductionTrendServiceS138.GroupKey(flowId, key, version) : string.Empty;
            bool changed = next != currentTrendKeyS138;
            currentTrendKeyS138 = next;
            RebuildTrendIdentitiesS138(changed);
        }
        private void RebuildTrendIdentitiesS138(bool useCurrent = false)
        {
            if (!trendReadyS138) return;
            string preferred = useCurrent ? currentTrendKeyS138 : SelectedKey(TrendIdentityS138);
            if (string.IsNullOrEmpty(preferred)) preferred = currentTrendKeyS138;
            var options = new List<FilterOptionS135>();
            if (!string.IsNullOrEmpty(currentTrendKeyS138)) options.Add(Option(currentTrendKeyS138, "打开 / 刷新时已应用定义与流程"));
            options.AddRange(recordsS135.GroupBy(ProductionTrendServiceS138.GroupKey, StringComparer.Ordinal).Select(g => g.First())
                .Where(r => ProductionTrendServiceS138.GroupKey(r) != currentTrendKeyS138)
                .Select(r => Option(ProductionTrendServiceS138.GroupKey(r), r.RecipeName + " / " + r.FlowName + " · " +
                    (r.AlgorithmDefinitionKeyVersionS138 > 0 ? "v" + r.AlgorithmDefinitionKeyVersionS138 + " " + Short(r.AlgorithmDefinitionKeyS138, 10) : "旧快照 " + Short(r.RecipeDefinitionSha256, 10)) + " · " + Short(r.FlowId, 8))));
            if (options.Count == 0) options.Add(Option("", "尚无可选择的历史身份"));
            changingTrendS138 = true;
            try { SetOptions(TrendIdentityS138, options, preferred); if (TrendIdentityS138.SelectedIndex < 0) TrendIdentityS138.SelectedIndex = 0; RebuildTrendMetricsS138(); }
            finally { changingTrendS138 = false; }
            RefreshTrendS138();
        }
        private void RebuildTrendMetricsS138()
        {
            string selected = SelectedKey(TrendMetricS138);
            trendMetricsS138 = ProductionTrendServiceS138.Metrics(recordsS135, SelectedKey(TrendIdentityS138));
            TrendMetricS138.ItemsSource = trendMetricsS138;
            TrendMetricS138.SelectedValue = trendMetricsS138.Any(m => m.Key == selected) ? selected : "elapsed";
        }
        private void RefreshTrendS138()
        {
            if (!trendReadyS138 || changingTrendS138) return;
            var metric = TrendMetricS138.SelectedItem as ProductionTrendServiceS138.Metric;
            if (metric == null) return;
            int count, days;
            if (!int.TryParse(SelectedKey(TrendCountS138), out count) || !int.TryParse(SelectedKey(TrendTimeS138), out days)) return;
            trendSliceS138 = ProductionTrendServiceS138.Build(recordsS135, SelectedKey(TrendIdentityS138), SelectedKey(TrendScopeS138), metric, count, days, TrendIncludeIncompleteS138.IsChecked == true, DateTime.UtcNow);
            TrendStatsS138.Text = trendSliceS138.Statistics;
            string rangeWarning = trendSliceS138.Warning + (warningsS135.Count > 0 ? " 有读取警告，详见周期记录的更多。" : "") +
                (trendMetricsS138.Count >= 513 ? " 端口选择达到 512 项上限。" : "") +
                " 两页筛选独立：本页用稳定定义；记录页保留原始快照 SHA。模拟周期当前由原 S135 隔离，不重复入库。";
            TrendNoteS138.Text = "仅已载入窗口 · 缺失不补零 · 非完整 SPC" +
                (TrendIncludeIncompleteS138.IsChecked == true ? " · 已包含未保存/草稿/截断" : " · 已排除未保存/草稿/截断") +
                (warningsS135.Count > 0 ? " · 有读取警告" : "") + (trendMetricsS138.Count >= 513 ? " · 端口清单有界" : "") + "（范围说明见悬停）";
            TrendNoteS138.ToolTip = rangeWarning;
            trendSliceS138.Warning = rangeWarning; // Freeze the range/read-budget warnings for export too.
            TrendPointsS138.ItemsSource = trendSliceS138.Points;
            TrendExportS138.IsEnabled = !busyS135 && trendSliceS138.Points.Count > 0;
            TrendSelectedS138.Text = "点选曲线或一行；查看所选周期只读，不定位名称相同的当前模块。";
            DrawTrendS138();
        }
        private void SetTrendBusyS138(bool busy)
        {
            if (TrendFiltersS138 == null) return;
            TrendFiltersS138.IsEnabled = !busy;
            TrendExportS138.IsEnabled = !busy && trendSliceS138 != null && trendSliceS138.Points.Count > 0;
        }
        private void TrendIdentity_ChangedS138(object sender, SelectionChangedEventArgs e)
        {
            if (!trendReadyS138 || changingTrendS138) return;
            changingTrendS138 = true; try { RebuildTrendMetricsS138(); } finally { changingTrendS138 = false; }
            RefreshTrendS138();
        }
        private void TrendFilter_ChangedS138(object sender, SelectionChangedEventArgs e) { RefreshTrendS138(); }
        private void TrendInclude_ChangedS138(object sender, RoutedEventArgs e) { RefreshTrendS138(); }
        private void TrendCanvas_SizeChangedS138(object sender, SizeChangedEventArgs e) { DrawTrendS138(); }
        private void DrawTrendS138()
        {
            if (!trendReadyS138 || TrendCanvasS138 == null) return;
            var canvas = TrendCanvasS138; canvas.Children.Clear();
            double width = canvas.ActualWidth, height = canvas.ActualHeight;
            if (width < 100 || height < 55 || trendSliceS138 == null) return;
            const double left = 58, top = 22, bottom = 24;
            double span = Math.Max(1, width - left - 12), plotHeight = Math.Max(1, height - top - bottom);
            canvas.Children.Add(new Line { X1 = left, Y1 = top, X2 = left, Y2 = height - bottom, Stroke = Brushes.LightGray });
            canvas.Children.Add(new Line { X1 = left, Y1 = height - bottom, X2 = width - 12, Y2 = height - bottom, Stroke = Brushes.LightGray });
            var values = trendSliceS138.Points.Where(p => p.Value.HasValue).Select(p => p.Value.Value).ToList();
            if (values.Count == 0) { AddTrendLabelS138("此范围没有有效数值；缺失不会绘制为零。", left + 8, top + 12); return; }
            // Scale before subtracting to keep +/-Double.MaxValue from overflowing chart geometry.
            double scale = Math.Max(1, Math.Max(Math.Abs(values.Min()), Math.Abs(values.Max())));
            double min = values.Min() / scale, max = values.Max() / scale;
            if (min == max) { min -= 0.05; max += 0.05; }
            AddTrendLabelS138(values.Max().ToString("G5"), 0, top - 10);
            AddTrendLabelS138(values.Min().ToString("G5"), 0, height - bottom - 12);
            AddTrendLabelS138(trendSliceS138.Metric.Label, left + 4, 0);
            double? previousX = null, previousY = null;
            for (int i = 0; i < trendSliceS138.Points.Count; i++)
            {
                var point = trendSliceS138.Points[i];
                double x = left + span * i / Math.Max(1, trendSliceS138.Points.Count - 1);
                if (!point.Value.HasValue) { previousX = previousY = null; continue; }
                double y = top + (max - point.Value.Value / scale) / (max - min) * plotHeight;
                if (previousX.HasValue) canvas.Children.Add(new Line { X1 = previousX.Value, Y1 = previousY.Value, X2 = x, Y2 = y, Stroke = Brushes.SlateGray, StrokeThickness = 1 });
                Brush brush = point.State == "NG" ? Brushes.DarkOrange : point.State == "ERROR" ? Brushes.Firebrick : point.State == "TIMEOUT" ? Brushes.Goldenrod : point.State == "STOP" ? Brushes.Gray : Brushes.Teal;
                var dot = new Ellipse { Width = 5, Height = 5, Fill = brush, IsHitTestVisible = false };
                Canvas.SetLeft(dot, x - 2.5); Canvas.SetTop(dot, y - 2.5); canvas.Children.Add(dot);
                previousX = x; previousY = y;
            }
            if (trendSliceS138.Points.Count > 0)
            { AddTrendLabelS138(trendSliceS138.Points.First().LocalTime, left, height - 17); AddTrendLabelS138(trendSliceS138.Points.Last().LocalTime, Math.Max(left, width - 130), height - 17); }
        }
        private void AddTrendLabelS138(string text, double x, double y)
        {
            var label = new TextBlock { Text = text, FontSize = 10, Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.NoWrap, IsHitTestVisible = false };
            Canvas.SetLeft(label, x); Canvas.SetTop(label, y); TrendCanvasS138.Children.Add(label);
        }
        private void TrendCanvas_ClickS138(object sender, MouseButtonEventArgs e)
        {
            if (trendSliceS138 == null || trendSliceS138.Points.Count == 0) return;
            double fraction = (e.GetPosition(TrendCanvasS138).X - 58) / Math.Max(1, TrendCanvasS138.ActualWidth - 70);
            int index = (int)Math.Round(Math.Max(0, Math.Min(1, fraction)) * (trendSliceS138.Points.Count - 1));
            TrendPointsS138.SelectedItem = trendSliceS138.Points[index]; TrendPointsS138.ScrollIntoView(TrendPointsS138.SelectedItem);
        }
        private void TrendPoint_ChangedS138(object sender, SelectionChangedEventArgs e)
        {
            var point = TrendPointsS138.SelectedItem as ProductionTrendServiceS138.Point;
            if (point != null) TrendSelectedS138.Text = "RunId " + Short(point.RunId, 12) + " · " + point.State + " · " + point.ValueText;
        }
        private void TrendOpenRecord_ClickS138(object sender, RoutedEventArgs e)
        {
            var point = TrendPointsS138.SelectedItem as ProductionTrendServiceS138.Point;
            if (point == null) { TrendSelectedS138.Text = "先选择一个周期点。"; return; }
            updatingS135 = true;
            try
            {
                SetOptions(TimePickerS135, (IEnumerable<FilterOptionS135>)TimePickerS135.ItemsSource, "");
                StatePickerS135.SelectedValue = ""; OriginPickerS135.SelectedValue = ""; PagePickerS135.SelectedValue = "";
                OnlyProductionS135.IsChecked = false;
                RecipePickerS135.SelectedValue = RecipeIdentity(point.Record); RebuildFlowOptions(FlowIdentity(point.Record));
            }
            finally { updatingS135 = false; }
            ApplyFilters(point.RunId); HistoryTabsS138.SelectedIndex = 0; MoreExpanderS135.IsExpanded = true;
            if (RecordsGridS135.SelectedItem != null) RecordsGridS135.ScrollIntoView(RecordsGridS135.SelectedItem);
            SetStatus("已只读定位同 RunId 周期；记录页筛选已切至此身份，不运行或恢复历史。" );
        }
        private async void TrendExport_ClickS138(object sender, RoutedEventArgs e)
        {
            if (busyS135 || trendSliceS138 == null || trendSliceS138.Points.Count == 0) return;
            var frozen = trendSliceS138; // Build produced independent deep-cloned records; later filtering replaces the slice.
            var dialog = new Microsoft.Win32.SaveFileDialog { Title = "导出点击时趋势范围", Filter = "CSV (*.csv)|*.csv", DefaultExt = ".csv", FileName = "周期趋势-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv" };
            if (dialog.ShowDialog(this) != true || !IsLoaded) return;
            if (!string.Equals(System.IO.Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase)) { TrendNoteS138.Text = "请选择 CSV 文件，不覆盖其他格式文件。"; return; }
            SetBusy(true);
            try { await Task.Run(() => ProductionTrendServiceS138.ExportCsv(dialog.FileName, frozen)); if (IsLoaded) TrendNoteS138.Text = "已导出点击时冻结范围 " + frozen.Points.Count + " 周期；不包含未加载历史、不重放。"; }
            catch (Exception ex) { if (IsLoaded) TrendNoteS138.Text = "趋势导出失败：" + ex.Message; }
            finally { if (IsLoaded) SetBusy(false); }
        }
    }
}
