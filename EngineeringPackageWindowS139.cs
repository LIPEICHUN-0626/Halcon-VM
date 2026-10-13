using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using HalconWinFormsDemo.Models;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public sealed class VmEngineeringPackageRelocateEventS139 : EventArgs
    {
        public string EntryKey { get; private set; }
        public string Path { get; private set; }

        public VmEngineeringPackageRelocateEventS139(string entryKey, string path)
        { EntryKey = entryKey ?? string.Empty; Path = path ?? string.Empty; }
    }

    // This task window edits no recipe. The owner re-plans its independent export DTO.
    public sealed class EngineeringPackageWindowS139 : Window
    {
        private readonly TextBlock summary;
        private readonly TextBlock selectionDetail;
        private readonly TextBlock status;
        private readonly DataGrid entries;
        private readonly TextBox report;
        private readonly CheckBox unresolvedAcknowledgement;
        private readonly Button relocate;
        private readonly Button continueButton;
        private readonly Button copyReport;
        private readonly Button saveReport;
        private readonly Button cancel;
        private bool busy;

        private sealed class ResourceRowS139
        {
            public VmEngineeringPackageEntryS139 Entry { get; set; }
            public string Label { get { return Entry.Label ?? string.Empty; } }
            public string Kind { get { return Entry.Kind ?? string.Empty; } }
            public string State { get { return StateText(Entry.State); } }
            public string Size
            {
                get
                {
                    if (!string.Equals(Entry.State, "Ready", StringComparison.Ordinal) || Entry.Bytes < 0) return "—";
                    if (Entry.Bytes < 1024) return Entry.Bytes.ToString(CultureInfo.InvariantCulture) + " B";
                    if (Entry.Bytes < 1048576) return (Entry.Bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
                    return (Entry.Bytes / 1048576.0).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
                }
            }
            public string Detail { get { return Entry.Detail ?? string.Empty; } }
            public string TechnicalDetail
            {
                get
                {
                    return "来源：" + (Entry.SourcePath ?? "未声明") + "\nSHA-256：" +
                        (string.IsNullOrWhiteSpace(Entry.Sha256) ? "未取得" : Entry.Sha256) + "\n" +
                        (Entry.Detail ?? string.Empty) + "\n哈希只用于核对本次文件，不是签名、代码安全或算法可用证明。";
                }
            }
        }

        public VmEngineeringPackagePlanS139 Plan { get; private set; }
        public event EventHandler<VmEngineeringPackageRelocateEventS139> RelocateRequested;

        public EngineeringPackageWindowS139(VmEngineeringPackagePlanS139 plan)
        {
            Title = "工程迁移 · 资源与机器准备";
            Width = 880; Height = 610; MinWidth = 750; MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false; Background = Brush("#F8FAFC");

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            header.Children.Add(Text("工程随行资源", 18, "#0F172A", true));
            summary = Text(string.Empty, 12, "#475569"); summary.Margin = new Thickness(0, 6, 0, 0);
            header.Children.Add(summary); root.Children.Add(header);

            var tabs = new TabControl { Background = Brushes.White, BorderBrush = Brush("#CBD5E1") };
            Grid.SetRow(tabs, 1); root.Children.Add(tabs);

            var resourcePanel = new Grid { Margin = new Thickness(12) };
            resourcePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            resourcePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            resourcePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            resourcePanel.Children.Add(Text("仅列本次工程明确引用的文件。缺失项可重新定位；不会修改当前工程。", 11, "#64748B"));

            entries = new DataGrid
            {
                AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false,
                CanUserDeleteRows = false, CanUserReorderColumns = false,
                SelectionMode = DataGridSelectionMode.Single, SelectionUnit = DataGridSelectionUnit.FullRow,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = Brush("#E2E8F0"), RowHeight = 32,
                Background = Brushes.White, BorderBrush = Brush("#E2E8F0"),
                Margin = new Thickness(0, 9, 0, 8), Foreground = Brush("#334155")
            };
            entries.Columns.Add(Column("资源", "Label", new DataGridLength(2, DataGridLengthUnitType.Star)));
            entries.Columns.Add(Column("类型", "Kind", new DataGridLength(135)));
            entries.Columns.Add(Column("状态", "State", new DataGridLength(86)));
            entries.Columns.Add(Column("大小", "Size", new DataGridLength(88)));
            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new Binding("TechnicalDetail")));
            entries.RowStyle = rowStyle;
            entries.SelectionChanged += delegate { UpdateSelection(); };
            Grid.SetRow(entries, 1); resourcePanel.Children.Add(entries);

            var resourceActions = new StackPanel();
            relocate = Button("重新定位所选文件…", "只向本次导出计划提交路径，由主窗口重新核对资源身份与预算；不改活动方案。");
            relocate.HorizontalAlignment = HorizontalAlignment.Left;
            relocate.Click += Relocate_Click; resourceActions.Children.Add(relocate);
            selectionDetail = Text("选择资源后可查看路径、哈希与处理说明。", 11, "#64748B");
            selectionDetail.Margin = new Thickness(6);
            var more = new Expander { Header = "更多 · 所选资源详情", IsExpanded = false,
                Foreground = Brush("#475569"), Margin = new Thickness(0, 5, 0, 0),
                Content = new ScrollViewer { Content = selectionDetail, MaxHeight = 115,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
            resourceActions.Children.Add(more); Grid.SetRow(resourceActions, 2); resourcePanel.Children.Add(resourceActions);
            tabs.Items.Add(new TabItem { Header = "随行资源", Content = resourcePanel });

            var reportPanel = new Grid { Margin = new Thickness(12) };
            reportPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            reportPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            reportPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            reportPanel.Children.Add(Text("报告只包含本次配置与当前进程可确认事实。目标机器的运行库、许可与设备环境仍需人工核对。", 11, "#64748B"));
            report = new TextBox
            {
                IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                FontFamily = new FontFamily("Microsoft YaHei UI"), FontSize = 12,
                Foreground = Brush("#334155"), Background = Brushes.White,
                BorderBrush = Brush("#E2E8F0"), Padding = new Thickness(9), Margin = new Thickness(0, 9, 0, 8)
            };
            Grid.SetRow(report, 1); reportPanel.Children.Add(report);
            var reportActions = new WrapPanel();
            copyReport = Button("复制报告", "复制当前显示的准备报告，不连接设备、不安装运行库、不执行 DLL。");
            saveReport = Button("另存报告…", "显式选择文本文件，保存 UTF-8（含 BOM）报告；报告不代表目标机器验收通过。");
            copyReport.Click += CopyReport_Click; saveReport.Click += SaveReport_Click;
            reportActions.Children.Add(copyReport); reportActions.Children.Add(saveReport);
            Grid.SetRow(reportActions, 2); reportPanel.Children.Add(reportActions);
            tabs.Items.Add(new TabItem { Header = "目标机器准备", Content = reportPanel });

            unresolvedAcknowledgement = new CheckBox
            {
                Content = Text("已知尚有未解析依赖；仅导出资源副本，不等于可部署。", 11, "#9A3412"),
                Margin = new Thickness(1, 11, 0, 0), VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = "例如 DLL 的间接依赖、原生库或 HDevelop 的未解析引用。确认不会把未知项变成就绪或绕过缺失/阻断项。"
            };
            unresolvedAcknowledgement.Checked += delegate { UpdateActions(); };
            unresolvedAcknowledgement.Unchecked += delegate { UpdateActions(); };
            Grid.SetRow(unresolvedAcknowledgement, 2); root.Children.Add(unresolvedAcknowledgement);

            var footer = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            status = Text(string.Empty, 11, "#64748B"); status.VerticalAlignment = VerticalAlignment.Center;
            status.Margin = new Thickness(0, 0, 10, 0); footer.Children.Add(status);
            var finalActions = new StackPanel { Orientation = Orientation.Horizontal };
            cancel = Button("取消", "退出本次导出，不更改活动工程与已有工程包。");
            cancel.IsCancel = true;
            continueButton = Button("选择保存位置…", "由主窗口再次核对本次计划和工程状态，再选择工程 JSON 的保存位置。");
            continueButton.Click += delegate
            {
                if (!CanContinue()) return;
                DialogResult = true;
            };
            finalActions.Children.Add(cancel); finalActions.Children.Add(continueButton);
            Grid.SetColumn(finalActions, 1); footer.Children.Add(finalActions);
            Grid.SetRow(footer, 3); root.Children.Add(footer); Content = root;
            // Explicit cancel remains available; the owner cancels its value-only background planning.
            SetPlan(plan);
        }

        public void SetPlan(VmEngineeringPackagePlanS139 plan)
        {
            string selectedKey = (entries.SelectedItem as ResourceRowS139)?.Entry.Key;
            Plan = plan;
            // A refreshed plan has new facts; a previous unknown-dependency acknowledgement does not carry over.
            unresolvedAcknowledgement.IsChecked = false;
            unresolvedAcknowledgement.Visibility = plan != null && plan.UnresolvedDependencies ? Visibility.Visible : Visibility.Collapsed;
            var rows = (plan?.Entries ?? new List<VmEngineeringPackageEntryS139>()).Where(value => value != null)
                .Select(value => new ResourceRowS139 { Entry = value }).ToList();
            entries.ItemsSource = rows;
            entries.SelectedItem = rows.FirstOrDefault(value => string.Equals(value.Entry.Key, selectedKey, StringComparison.Ordinal));
            summary.Text = plan == null ? "无法生成迁移计划；本次导出不可用。" : plan.Summary ?? string.Empty;
            report.Text = plan?.ReportText ?? string.Empty;
            status.Text = plan == null ? "请返回主窗口重新生成计划。" : plan.BlockingCount > 0 ?
                "存在 " + plan.BlockingCount.ToString(CultureInfo.InvariantCulture) + " 个阻断项；处理后才能继续。" :
                plan.UnresolvedDependencies ? "仍有未解析依赖，请查看目标机器准备报告后明确确认。" :
                "资源清单不等于部署完成；保存后仍需目标机器核对。";
            UpdateSelection();
        }

        public void SetBusy(bool value) { busy = value; UpdateActions(); }

        private bool CanContinue()
        {
            return !busy && Plan != null && Plan.BlockingCount == 0 &&
                (!Plan.UnresolvedDependencies || unresolvedAcknowledgement.IsChecked == true);
        }

        private void UpdateActions()
        {
            continueButton.IsEnabled = CanContinue();
            relocate.IsEnabled = !busy && Plan != null && entries.SelectedItem is ResourceRowS139;
            unresolvedAcknowledgement.IsEnabled = !busy;
            entries.IsEnabled = !busy; cancel.IsEnabled = true;
            copyReport.IsEnabled = saveReport.IsEnabled = !busy && !string.IsNullOrWhiteSpace(report.Text);
        }

        private void UpdateSelection()
        {
            var row = entries.SelectedItem as ResourceRowS139;
            selectionDetail.Text = row == null ? "选择资源后可查看路径、哈希与处理说明。" : row.Label + "\n" + row.TechnicalDetail;
            UpdateActions();
        }

        private void Relocate_Click(object sender, RoutedEventArgs e)
        {
            var selected = entries.SelectedItem as ResourceRowS139;
            if (busy || selected == null || Plan == null) return;
            var frozenPlan = Plan;
            string key = selected.Entry.Key;
            var dialog = new OpenFileDialog { Title = "重新定位 · " + selected.Label, CheckFileExists = true,
                Multiselect = false, Filter = "原类型文件|*" + SafeExtension(selected.Entry.SourcePath) + "|所有文件|*.*" };
            try
            {
                if (dialog.ShowDialog(this) != true) return;
                if (busy || !ReferenceEquals(frozenPlan, Plan)) { status.Text = "计划已变化，请重新选择资源。"; return; }
                var handler = RelocateRequested;
                if (handler == null) { status.Text = "当前窗口尚未接入资源重新定位，未修改任何工程。"; return; }
                status.Text = "正在核对重新定位的文件…";
                handler(this, new VmEngineeringPackageRelocateEventS139(key, dialog.FileName));
            }
            catch (Exception error) { status.Text = "重新定位失败：" + error.Message; }
        }

        private void CopyReport_Click(object sender, RoutedEventArgs e)
        {
            if (busy || string.IsNullOrWhiteSpace(report.Text)) return;
            try { Clipboard.SetText(report.Text); status.Text = "已复制本次报告。目标机器仍需人工核对。"; }
            catch (Exception error) { status.Text = "复制失败：" + error.Message; }
        }

        private async void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            if (busy || string.IsNullOrWhiteSpace(report.Text)) return;
            string frozenReport = report.Text;
            var dialog = new SaveFileDialog { Title = "另存目标机器准备报告", Filter = "文本报告 (*.txt)|*.txt",
                DefaultExt = ".txt", AddExtension = true, OverwritePrompt = true, FileName = "工程迁移_目标准备报告.txt" };
            SetBusy(true);
            try
            {
                if (dialog.ShowDialog(this) != true) return;
                string output = System.IO.Path.GetFullPath(dialog.FileName);
                if (!string.Equals(System.IO.Path.GetExtension(output), ".txt", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("报告只允许保存为 .txt，不会写入方案或算法资源文件。");
                foreach (var entry in Plan.Entries)
                {
                    string source;
                    try { source = System.IO.Path.GetFullPath(entry.SourcePath ?? string.Empty); }
                    catch { continue; }
                    if (string.Equals(source, output, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("报告不能覆盖清单中的源文件，请选择独立报告名称。");
                }
                await Task.Run(() => File.WriteAllText(output, frozenReport, new UTF8Encoding(true, true)));
                status.Text = "已保存所选报告文本；没有安装、连接设备或执行扩展代码。";
            }
            catch (Exception error) { status.Text = "报告保存失败：" + error.Message; }
            finally { SetBusy(false); }
        }

        private static string SafeExtension(string path)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(path ?? string.Empty);
                return extension.Length > 0 && extension.Length <= 16 && extension.Skip(1).All(char.IsLetterOrDigit) ? extension : ".*";
            }
            catch { return ".*"; }
        }
        private static string StateText(string state)
        {
            if (string.Equals(state, "Ready", StringComparison.Ordinal)) return "已核对";
            if (string.Equals(state, "Missing", StringComparison.Ordinal)) return "缺失";
            if (string.Equals(state, "Blocked", StringComparison.Ordinal)) return "阻断";
            return "未知";
        }
        private static DataGridTextColumn Column(string header, string property, DataGridLength width)
        {
            var textStyle = new Style(typeof(TextBlock));
            textStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            textStyle.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
            textStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(6, 0, 6, 0)));
            return new DataGridTextColumn { Header = header, Binding = new Binding(property), Width = width, ElementStyle = textStyle };
        }
        private static TextBlock Text(string value, double size, string color, bool semibold = false)
        {
            return new TextBlock { Text = value, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap,
                FontWeight = semibold ? FontWeights.SemiBold : FontWeights.Normal };
        }
        private static Button Button(string text, string tooltip)
        {
            return new Button { Content = text, ToolTip = tooltip, Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 0), MinHeight = 30, Background = Brushes.White,
                BorderBrush = Brush("#CBD5E1"), Foreground = Brush("#334155") };
        }
        private static SolidColorBrush Brush(string color)
        { return (SolidColorBrush)new BrushConverter().ConvertFromString(color); }
    }
}
