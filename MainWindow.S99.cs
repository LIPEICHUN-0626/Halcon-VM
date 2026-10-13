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
        private List<VmToolInstance> GetBatchEditTargetsS99(string action)
        {
            if (!CanNavigateInputRepairS96()) return null;
            var targets = GetSelectedFlowToolsInSequence();
            if (targets.Count < 2) { HeaderStatusText.Text = "请先多选至少两个流程模块，再" + action + "。"; return null; }
            if (targets.Contains(dockEditingTool) && !ConfirmDockDraftChanges(action)) return null;
            return targets;
        }

        private Window CreateBatchEditWindowS99(string title, out StackPanel content, out Button apply)
        {
            var window = new Window { Owner = this, Title = title, Width = 570, Height = 480,
                MinWidth = 460, MinHeight = 360, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var root = new DockPanel { Margin = new Thickness(14) };
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,10,0,0) };
            actions.Children.Add(new Button { Content = "取消", IsCancel = true, MinWidth = 70, Margin = new Thickness(0,0,8,0) });
            apply = new Button { Content = "应用到所选模块", MinWidth = 125, IsDefault = true };
            actions.Children.Add(apply); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            content = new StackPanel();
            root.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            window.Content = root;
            return window;
        }

        private void CompleteBatchEditS99(IList<VmToolInstance> targets, FlowEditSnapshot before, string description)
        {
            foreach (var tool in targets)
            {
                InvalidateToolRunResult(tool, description);
                InvalidateDownstreamResults(tool, "上游批量配置已变化");
            }
            if (targets.Contains(dockEditingTool)) LoadDockConfigurationDraft(dockEditingTool);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory(description, before);
            RefreshUiState();
            ScheduleRefreshDisplay();
            HeaderStatusText.Text = description + "；一次撤销可回退，需重新运行后查看结果。";
            LogInfo(HeaderStatusText.Text);
        }

        private void BatchRenameToolsS99_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("批量重命名模块");
            if (editLeaseS137 < 0) return;
            var targets = GetBatchEditTargetsS99("批量命名");
            if (targets == null) return;
            StackPanel content; Button apply;
            var window = CreateBatchEditWindowS99("批量命名 · 预览后应用", out content, out apply);
            content.Children.Add(new TextBlock { Text = "按当前执行顺序编号，不改变节点位置、连接或执行顺序。", TextWrapping = TextWrapping.Wrap });
            var fields = new WrapPanel { Margin = new Thickness(0,10,0,8) };
            fields.Children.Add(new TextBlock { Text = "前缀", VerticalAlignment = VerticalAlignment.Center });
            var prefix = new TextBox { Text = "工位_", Width = 130, Margin = new Thickness(7,0,12,0) };
            fields.Children.Add(prefix);
            fields.Children.Add(new TextBlock { Text = "起始编号", VerticalAlignment = VerticalAlignment.Center });
            var start = new TextBox { Text = "1", Width = 65, Margin = new Thickness(7,0,0,0) };
            fields.Children.Add(start); content.Children.Add(fields);
            var keepName = new CheckBox { Content = "编号后保留原模块名", IsChecked = true, Margin = new Thickness(0,0,0,8) };
            content.Children.Add(keepName);
            var notice = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.Firebrick, Margin = new Thickness(0,0,0,8) };
            content.Children.Add(notice);
            var preview = new ListBox { MinHeight = 150, MaxHeight = 240, BorderThickness = new Thickness(0) };
            content.Children.Add(preview);
            List<string> names = null;
            Action refresh = delegate
            {
                preview.Items.Clear(); names = null; apply.IsEnabled = false;
                int first;
                if (!int.TryParse(start.Text, out first) || first < 0 || first > 999999 - targets.Count)
                { notice.Text = "起始编号请输入 0–999999 范围内整数，并为全部所选模块留出编号。"; return; }
                var proposed = targets.Select((tool, index) => prefix.Text.Trim() + (first + index).ToString("D2", CultureInfo.InvariantCulture) +
                    (keepName.IsChecked == true ? " " + tool.InstanceName : string.Empty)).ToList();
                var used = new HashSet<string>(flowTools.Where(tool => !targets.Contains(tool)).Select(tool => tool.InstanceName), StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < proposed.Count; index++)
                {
                    preview.Items.Add(targets[index].InstanceName + "  →  " + proposed[index]);
                    if (proposed[index].Length > 64 || proposed[index].Any(char.IsControl))
                    { notice.Text = "名称最多 64 字，不能包含换行等控制字符。"; return; }
                    if (!used.Add(proposed[index])) { notice.Text = "名称与现有模块冲突，请修改前缀或起始编号。"; return; }
                }
                notice.Text = "将修改 " + targets.Count + " 个名称；未选模块保持不变。";
                names = proposed; apply.IsEnabled = true;
            };
            prefix.TextChanged += delegate { refresh(); }; start.TextChanged += delegate { refresh(); };
            keepName.Checked += delegate { refresh(); }; keepName.Unchecked += delegate { refresh(); };
            apply.Click += delegate
            {
                refresh();
                if (names == null || !CanNavigateInputRepairS96() || targets.Any(tool => !flowTools.Contains(tool))) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                var before = CaptureFlowEditSnapshot();
                for (int index = 0; index < targets.Count; index++) targets[index].InstanceName = names[index];
                CompleteBatchEditS99(targets, before, "批量命名 " + targets.Count + " 个模块");
                window.DialogResult = true;
            };
            refresh(); window.ShowDialog();
        }

        private void BatchCopyJudgeRulesS99_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("批量复用判定规则");
            if (editLeaseS137 < 0) return;
            var targets = GetBatchEditTargetsS99("复用判定规则");
            if (targets == null) return;
            if (targets.Any(tool => tool.Kind != VmToolKind.NumericJudge))
            { HeaderStatusText.Text = "此操作只适用于数值判定模块；请多选需要统一规则的数值判定。"; return; }
            StackPanel content; Button apply;
            var window = CreateBatchEditWindowS99("复用数值判定规则", out content, out apply);
            content.Children.Add(new TextBlock { Text = "选择规则来源。只同步比较方式、上下限和容差；每个模块原有输入连接、名称和启用状态保持不变。", TextWrapping = TextWrapping.Wrap });
            var sourcePicker = new ComboBox { ItemsSource = targets, DisplayMemberPath = "InstanceName", SelectedIndex = 0, Margin = new Thickness(0,10,0,10) };
            content.Children.Add(sourcePicker);
            var summary = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,10) };
            content.Children.Add(summary);
            var preview = new ListBox { BorderThickness = new Thickness(0), MaxHeight = 230 }; content.Children.Add(preview);
            Action refresh = delegate
            {
                var source = sourcePicker.SelectedItem as VmToolInstance;
                preview.Items.Clear(); apply.IsEnabled = source != null;
                if (source == null) return;
                string error = VmNumericJudgeParameterValidator.Validate(source.NumericOperator, source.NumericLowerLimit, source.NumericUpperLimit, source.NumericTolerance);
                var option = NumericJudgeOperatorOption.CreateAll().FirstOrDefault(item => item.Key == source.NumericOperator);
                summary.Text = string.IsNullOrWhiteSpace(error)
                    ? (option == null ? source.NumericOperator : option.DisplayText) + " · 下限 " + source.NumericLowerLimit.ToString("G", CultureInfo.CurrentCulture) +
                        " · 上限 " + source.NumericUpperLimit.ToString("G", CultureInfo.CurrentCulture) + " · 容差 " + source.NumericTolerance.ToString("G", CultureInfo.CurrentCulture)
                    : error;
                apply.IsEnabled = string.IsNullOrWhiteSpace(error);
                foreach (var target in targets.Where(tool => !ReferenceEquals(tool, source))) preview.Items.Add("应用到：" + target.InstanceName + "（输入不变）");
            };
            sourcePicker.SelectionChanged += delegate { refresh(); };
            apply.Click += delegate
            {
                refresh();
                if (!apply.IsEnabled || !CanNavigateInputRepairS96() || targets.Any(tool => !flowTools.Contains(tool))) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                var source = (VmToolInstance)sourcePicker.SelectedItem;
                var changed = targets.Where(tool => !ReferenceEquals(tool, source)).ToList();
                var before = CaptureFlowEditSnapshot();
                foreach (var target in changed)
                {
                    target.NumericOperator = source.NumericOperator; target.NumericLowerLimit = source.NumericLowerLimit;
                    target.NumericUpperLimit = source.NumericUpperLimit; target.NumericTolerance = source.NumericTolerance;
                }
                CompleteBatchEditS99(changed, before, "复用判定规则到 " + changed.Count + " 个模块");
                window.DialogResult = true;
            };
            refresh(); window.ShowDialog();
        }
    }
}
