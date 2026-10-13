using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class RecipeChangeoverWindowS134 : Window
    {
        public event EventHandler PreviewCurrentRequested;
        public event EventHandler BrowseRequested;
        public event EventHandler ApplyRequested;
        public event EventHandler LocateRequested;
        public event EventHandler TargetFlowChanged;
        public event EventHandler DiffRequested;

        private List<DiffRowS140> diffRowsS140 = new List<DiffRowS140>();
        private bool diffBusyS140;
        private bool hasDiffReportS140;

        private sealed class DiffRowS140
        {
            public string ChangeText { get; set; }
            public string AreaText { get; set; }
            public string ObjectText { get; set; }
            public string FieldText { get; set; }
            public string CurrentText { get; set; }
            public string TargetText { get; set; }
            public string DetailText { get; set; }
            public bool IsDisplay { get; set; }
            public bool IsUnknown { get; set; }
            public string HoverText { get { return ObjectText + "\n字段：" + FieldText + "\n当前：" + Short(CurrentText, 320) + "\n目标：" + Short(TargetText, 320) + "\n" + Short(DetailText, 500); } }
        }

        public RecipeChangeoverWindowS134() { InitializeComponent(); }
        private void PreviewCurrent_Click(object sender, RoutedEventArgs e)
        { if (PreviewCurrentRequested != null) PreviewCurrentRequested(this, EventArgs.Empty); }
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            InvalidateDiffReviewS140("重新选择目标，旧审阅已清空；选定后可重新生成。");
            if (BrowseRequested != null) BrowseRequested(this, EventArgs.Empty);
        }
        private void Apply_Click(object sender, RoutedEventArgs e)
        { if (ApplyRequested != null) ApplyRequested(this, EventArgs.Empty); }
        private void Locate_Click(object sender, RoutedEventArgs e)
        { if (LocateRequested != null) LocateRequested(this, EventArgs.Empty); }
        private void TargetFlow_Changed(object sender, SelectionChangedEventArgs e)
        {
            InvalidateDiffReviewS140("所选目标流程已变化，旧审阅不可用于当前目标。");
            if (TargetFlowChanged != null) TargetFlowChanged(this, EventArgs.Empty);
        }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        public bool IsDiffTabSelectedS140 { get { return ReviewTabsS140 != null && ReviewTabsS140.SelectedIndex == 1; } }

        public void SetDiffReviewS140(VmRecipeDiffReportS140 report, string context)
        {
            if (report == null) { InvalidateDiffReviewS140("没有有效审阅；请先选择目标方案与流程。"); return; }
            hasDiffReportS140 = true;
            diffRowsS140 = (report.Items ?? new List<VmRecipeDiffItemS140>()).Where(value => value != null)
                .Select(value => new DiffRowS140
                {
                    ChangeText = value.ChangeText ?? string.Empty, AreaText = value.AreaText ?? string.Empty,
                    ObjectText = value.ObjectText ?? string.Empty, FieldText = value.FieldText ?? string.Empty,
                    CurrentText = value.CurrentText ?? string.Empty, TargetText = value.TargetText ?? string.Empty,
                    DetailText = value.DetailText ?? string.Empty, IsDisplay = value.IsDisplay, IsUnknown = value.IsUnknown
                }).ToList();
            int unknown = diffRowsS140.Count(value => value.IsUnknown);
            int display = diffRowsS140.Count(value => value.IsDisplay && !value.IsUnknown);
            int algorithm = diffRowsS140.Count - unknown - display;
            string counts = "算法/执行 " + algorithm.ToString(CultureInfo.InvariantCulture) + " · 显示 " + display.ToString(CultureInfo.InvariantCulture) + " · 未知 " + unknown.ToString(CultureInfo.InvariantCulture);
            DiffSummaryTextS140.Text = counts + (report.IsComplete ? string.Empty : " · 审阅不完整");
            DiffSummaryTextS140.ToolTip = report.SummaryText ?? counts;
            DiffSummaryTextS140.Foreground = new System.Windows.Media.SolidColorBrush(!report.IsComplete || unknown > 0 ? System.Windows.Media.Color.FromRgb(154, 52, 18) : System.Windows.Media.Color.FromRgb(51, 65, 85));
            string contextText = string.IsNullOrWhiteSpace(context) ? "当前已应用 → 所选目标；差异不能替代准备或换型授权。" : context;
            DiffContextTextS140.Text = string.Join("\n", contextText.Split('\n').Take(2));
            DiffContextTextS140.ToolTip = contextText;
            DiffWarningTextS140.Text = (!report.IsComplete ? "比较预算或身份受限，不能据此认定无差异。 " : string.Empty) + (report.WarningText ?? string.Empty);
            DiffWarningTextS140.Visibility = string.IsNullOrWhiteSpace(DiffWarningTextS140.Text) ? Visibility.Collapsed : Visibility.Visible;
            DiffCurrentIdentityS140.Text = report.CurrentIdentity ?? string.Empty;
            DiffTargetIdentityS140.Text = report.TargetIdentity ?? string.Empty;
            DiffDetailExpanderS140.IsExpanded = false;
            ApplyDiffFilterS140();
        }

        public void InvalidateDiffReviewS140(string reason)
        {
            // Some original flow-picker events can fire while InitializeComponent is still creating the new tab.
            diffRowsS140.Clear();
            hasDiffReportS140 = false;
            if (DiffItemsS140 == null) return;
            DiffItemsS140.ItemsSource = null;
            DiffSummaryTextS140.Text = "审阅未生成 / 已失效";
            DiffSummaryTextS140.ToolTip = null;
            DiffContextTextS140.Text = "当前已应用 → 所选目标；准备与确认换型仍沿原流程。";
            DiffContextTextS140.ToolTip = null;
            DiffWarningTextS140.Text = reason ?? "当前或目标身份已变化，请重新生成审阅。";
            DiffWarningTextS140.Visibility = Visibility.Visible;
            DiffCurrentIdentityS140.Text = DiffTargetIdentityS140.Text = string.Empty;
            DiffDetailExpanderS140.IsExpanded = false;
            ClearDiffSelectionS140();
        }

        public void SetDiffBusyS140(bool busy)
        {
            diffBusyS140 = busy;
            // This is a read-only job: closing, stopping and the original owner safeguards remain independent.
            RefreshDiffButtonS140.IsEnabled = !busy;
            RefreshDiffButtonS140.Content = busy ? "正在生成…" : "生成 / 刷新审阅";
        }

        private void DiffRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (diffBusyS140) return;
            var handler = DiffRequested;
            if (handler != null) handler(this, EventArgs.Empty);
            else InvalidateDiffReviewS140("审阅入口尚未接入，未修改任何配置。");
        }

        private void DiffFilter_Changed(object sender, SelectionChangedEventArgs e)
        { if (DiffItemsS140 != null) ApplyDiffFilterS140(); }

        private void ApplyDiffFilterS140()
        {
            int filter = DiffFilterS140.SelectedIndex;
            IEnumerable<DiffRowS140> rows = diffRowsS140;
            if (filter == 0) rows = rows.Where(value => !value.IsDisplay && !value.IsUnknown);
            else if (filter == 1) rows = rows.Where(value => value.IsDisplay && !value.IsUnknown);
            else if (filter == 2) rows = rows.Where(value => value.IsUnknown);
            var visible = rows.ToList();
            DiffItemsS140.ItemsSource = visible;
            DiffItemsS140.SelectedItem = visible.FirstOrDefault();
            if (visible.Count == 0) ClearDiffSelectionS140();
        }

        private void DiffItem_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DiffCurrentValueS140 == null) return;
            var item = DiffItemsS140.SelectedItem as DiffRowS140;
            if (item == null) { ClearDiffSelectionS140(); return; }
            DiffCurrentValueS140.Text = item.CurrentText;
            DiffTargetValueS140.Text = item.TargetText;
            DiffSelectedDetailS140.Text = item.ObjectText + " · " + item.FieldText + (string.IsNullOrWhiteSpace(item.DetailText) ? string.Empty : "\n" + item.DetailText);
        }

        private void ClearDiffSelectionS140()
        {
            if (DiffCurrentValueS140 == null) return;
            DiffCurrentValueS140.Text = DiffTargetValueS140.Text = string.Empty;
            DiffSelectedDetailS140.Text = hasDiffReportS140 ? "当前分类没有条目；完整性与未知状态请以上方摘要为准，可切换其他分类。" : "尚无可查看的字段；不代表已经完成比较。";
        }

        private static string Short(string text, int limit)
        {
            if (string.IsNullOrEmpty(text)) return "（空）";
            return text.Length <= limit ? text : text.Substring(0, limit) + "…（完整值见更多）";
        }
    }
}
