using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class CommunicationQueueEditorS149
        {
            public ComboBox Picker;
            public TextBox Name;
            public TextBlock Status;
        }
        private readonly Dictionary<VmToolKind, CommunicationQueueEditorS149> communicationQueueEditorsS149 =
            new Dictionary<VmToolKind, CommunicationQueueEditorS149>();
        private VmCommunicationSampleQueueS149 communicationQueueDraftS149;
        private VmToolInstance communicationQueueOwnerS149;
        private string communicationQueueSessionS149, communicationQueueFlowS149;
        private bool communicationQueueLoadingS149;

        private static string ResolveCommunicationSimulationPayloadS149(VmToolParameterData parameters, VmToolKind kind)
        {
            if (parameters.CommunicationSampleQueueS149 != null) return parameters.CommunicationSampleQueueS149.Resolve(kind);
            return (kind == VmToolKind.TcpClientExchange ? parameters.TcpSimulationResponseS121
                : kind == VmToolKind.ModbusTcpExchange ? parameters.ModbusSimulationFrameS121 : parameters.RtuSimulationFrameS121) ?? string.Empty;
        }
        private static string CommunicationSampleLabelS149(VmToolParameterData parameters)
        {
            var queue = parameters.CommunicationSampleQueueS149;
            if (queue == null) return string.Empty;
            if (queue.Samples == null || queue.SelectedIndex < 0 || queue.SelectedIndex >= queue.Samples.Count || queue.Samples[queue.SelectedIndex] == null)
                return " · 样本选取无效";
            string name = queue.Samples[queue.SelectedIndex].Name ?? string.Empty;
            return " · 样本[" + queue.SelectedIndex + "] " + (name.Length > 64 ? name.Substring(0, 64) : name).Replace("\r", " ").Replace("\n", " ");
        }

        private TextBox CommunicationSampleBoxS149(VmToolKind kind)
        {
            return kind == VmToolKind.TcpClientExchange ? DockTcpSampleResponseS117
                : kind == VmToolKind.ModbusTcpExchange ? DockModbusSampleFrameS118 : DockRtuSampleFrameS119;
        }
        private CheckBox CommunicationSimulationCheckS149(VmToolKind kind)
        {
            return kind == VmToolKind.TcpClientExchange ? DockTcpSimulationS121
                : kind == VmToolKind.ModbusTcpExchange ? DockModbusSimulationS121 : DockRtuSimulationS121;
        }
        private bool IsCommunicationQueueOwnerCurrentS149(VmToolKind kind)
        {
            return dockEditingTool != null && ReferenceEquals(communicationQueueOwnerS149, dockEditingTool) &&
                dockEditingTool.Kind == kind && communicationQueueSessionS149 == projectSessionIdS104 &&
                communicationQueueFlowS149 == activeProjectFlowIdS104;
        }
        private bool CanEditCommunicationQueueS149(VmToolKind kind)
        {
            return !communicationQueueLoadingS149 && !dockDraftUpdating && IsCommunicationQueueOwnerCurrentS149(kind) &&
                CanChangeRecipeS134(false) && RequireEngineeringEditS137("编辑离线通讯样本草稿");
        }

        private CommunicationQueueEditorS149 EnsureCommunicationQueueEditorS149(VmToolKind kind)
        {
            CommunicationQueueEditorS149 editor;
            if (communicationQueueEditorsS149.TryGetValue(kind, out editor)) return editor;
            var box = CommunicationSampleBoxS149(kind);
            var parent = box.Parent as StackPanel;
            if (parent == null) throw new InvalidOperationException("离线样本原任务入口不可用，不创建另一参数窗。");
            editor = new CommunicationQueueEditorS149
            {
                Picker = new ComboBox { MinWidth = 125, MaxWidth = 235, Margin = new Thickness(0, 0, 5, 0),
                    ToolTip = "明确选取一个草稿样本；不自动应用、运行、切换下一帧或启用离线模拟。" },
                Name = new TextBox { Width = 100, MaxLength = 64, Margin = new Thickness(0, 0, 5, 0),
                    ToolTip = "当前样本名称（1–64字符），不是设备结果。" },
                Status = new TextBlock { Foreground = Brushes.SlateGray, FontSize = 11, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 5) }
            };
            var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 3) };
            var menu = new Menu { Background = Brushes.Transparent, VerticalAlignment = VerticalAlignment.Center };
            var manage = new MenuItem { Header = "管理", ToolTip = "只编辑当前模块草稿，不读写文件或访问设备。" };
            var add = new MenuItem { Header = "将当前响应加入队列" };
            var remove = new MenuItem { Header = "删除所选样本" };
            var single = new MenuItem { Header = "返回单样本（保留当前响应）" };
            manage.Items.Add(add); manage.Items.Add(remove); manage.Items.Add(single); menu.Items.Add(manage);
            row.Children.Add(editor.Picker); row.Children.Add(editor.Name); row.Children.Add(menu);
            parent.Children.Insert(0, row); parent.Children.Insert(1, editor.Status);
            communicationQueueEditorsS149.Add(kind, editor);
            editor.Picker.SelectionChanged += delegate
            {
                if (communicationQueueLoadingS149 || !IsCommunicationQueueOwnerCurrentS149(kind)) return;
                if (!CanEditCommunicationQueueS149(kind)) { RefreshCommunicationQueueUiS149(kind, false); return; }
                if (communicationQueueDraftS149 == null || editor.Picker.SelectedIndex < 0) return;
                communicationQueueDraftS149.SelectedIndex = editor.Picker.SelectedIndex;
                RefreshCommunicationQueueUiS149(kind, true); MarkCommunicationQueueDraftS149(kind);
            };
            editor.Name.TextChanged += delegate
            {
                if (!CanEditCommunicationQueueS149(kind)) return;
                var sample = SelectedCommunicationSampleS149();
                if (sample == null) return;
                sample.Name = editor.Name.Text; MarkCommunicationQueueDraftS149(kind);
            };
            editor.Name.LostKeyboardFocus += delegate
            { if (!communicationQueueLoadingS149 && IsCommunicationQueueOwnerCurrentS149(kind)) RefreshCommunicationQueueUiS149(kind, false); };
            box.TextChanged += delegate
            {
                if (!CanEditCommunicationQueueS149(kind)) return;
                var sample = SelectedCommunicationSampleS149();
                if (sample != null) sample.Payload = box.Text;
                MarkCommunicationQueueDraftS149(kind);
            };
            CommunicationSimulationCheckS149(kind).Checked += delegate { RefreshCommunicationQueueStatusS149(kind); };
            CommunicationSimulationCheckS149(kind).Unchecked += delegate { RefreshCommunicationQueueStatusS149(kind); };
            add.Click += delegate
            {
                if (!CanEditCommunicationQueueS149(kind)) return;
                var candidate = communicationQueueDraftS149 == null
                    ? new VmCommunicationSampleQueueS149 { Samples = new List<VmCommunicationSampleS149>() }
                    : communicationQueueDraftS149.Clone();
                if (candidate.Samples == null || candidate.Samples.Count >= 16)
                { editor.Status.Text = "队列最多16项；请明确删除不用的样本，不截断。"; return; }
                candidate.Samples.Add(new VmCommunicationSampleS149 { Name = "样本" + (candidate.Samples.Count + 1), Payload = box.Text });
                candidate.SelectedIndex = candidate.Samples.Count - 1;
                string error = candidate.Validate(kind);
                if (error.Length > 0) { editor.Status.Text = error; return; }
                communicationQueueDraftS149 = candidate;
                RefreshCommunicationQueueUiS149(kind, true); MarkCommunicationQueueDraftS149(kind);
            };
            remove.Click += delegate
            {
                if (!CanEditCommunicationQueueS149(kind) || SelectedCommunicationSampleS149() == null) return;
                communicationQueueDraftS149.Samples.RemoveAt(communicationQueueDraftS149.SelectedIndex);
                if (communicationQueueDraftS149.Samples.Count == 0) communicationQueueDraftS149 = null;
                else communicationQueueDraftS149.SelectedIndex = -1; // Require another explicit choice, not a nearest index.
                RefreshCommunicationQueueUiS149(kind, communicationQueueDraftS149 != null); MarkCommunicationQueueDraftS149(kind);
            };
            single.Click += delegate
            {
                if (!CanEditCommunicationQueueS149(kind)) return;
                communicationQueueDraftS149 = null;
                RefreshCommunicationQueueUiS149(kind, false); MarkCommunicationQueueDraftS149(kind);
            };
            return editor;
        }

        private VmCommunicationSampleS149 SelectedCommunicationSampleS149()
        {
            var queue = communicationQueueDraftS149;
            return queue == null || queue.Samples == null || queue.SelectedIndex < 0 || queue.SelectedIndex >= queue.Samples.Count
                ? null : queue.Samples[queue.SelectedIndex];
        }
        private void LoadCommunicationQueueS149(VmToolParameterData parameters, VmToolKind kind)
        {
            // Legacy Load fills hidden TCP/Modbus editors too; only the actual module owns this draft.
            if (dockEditingTool == null || dockEditingTool.Kind != kind) return;
            communicationQueueOwnerS149 = dockEditingTool;
            communicationQueueSessionS149 = projectSessionIdS104; communicationQueueFlowS149 = activeProjectFlowIdS104;
            communicationQueueDraftS149 = parameters.CommunicationSampleQueueS149 == null ? null : parameters.CommunicationSampleQueueS149.Clone();
            EnsureCommunicationQueueEditorS149(kind); RefreshCommunicationQueueUiS149(kind, true);
        }
        private void ReadCommunicationQueueS149(VmToolParameterData parameters, VmToolKind kind)
        {
            if (dockEditingTool == null || dockEditingTool.Kind != kind) return;
            if (!IsCommunicationQueueOwnerCurrentS149(kind)) throw new InvalidOperationException("通讯样本草稿上下文已失效，请重新打开原模块。");
            parameters.CommunicationSampleQueueS149 = communicationQueueDraftS149 == null ? null : communicationQueueDraftS149.Clone();
        }
        private void RefreshCommunicationQueueUiS149(VmToolKind kind, bool updatePayload)
        {
            if (!IsCommunicationQueueOwnerCurrentS149(kind)) return;
            var editor = communicationQueueEditorsS149[kind];
            bool wasLoading = communicationQueueLoadingS149; communicationQueueLoadingS149 = true;
            try
            {
                var queue = communicationQueueDraftS149; var sample = SelectedCommunicationSampleS149();
                editor.Picker.ItemsSource = queue == null ? new[] { "单样本（兼容）" }
                    : (queue.Samples ?? new List<VmCommunicationSampleS149>()).Select((item, index) => index + " · " + (item == null ? "空样本" : item.Name)).ToArray();
                editor.Picker.IsEnabled = queue != null;
                editor.Picker.SelectedIndex = queue == null ? 0 : queue.SelectedIndex;
                editor.Name.Visibility = queue == null ? Visibility.Collapsed : Visibility.Visible;
                editor.Name.IsEnabled = sample != null; editor.Name.Text = sample == null ? string.Empty : sample.Name;
                var box = CommunicationSampleBoxS149(kind);
                box.IsReadOnly = queue != null && sample == null;
                if (updatePayload && queue != null) box.Text = sample == null ? string.Empty : sample.Payload;
                RefreshCommunicationQueueStatusS149(kind);
            }
            finally { communicationQueueLoadingS149 = wasLoading; }
        }
        private void RefreshCommunicationQueueStatusS149(VmToolKind kind)
        {
            if (!IsCommunicationQueueOwnerCurrentS149(kind) || !communicationQueueEditorsS149.ContainsKey(kind)) return;
            var editor = communicationQueueEditorsS149[kind]; var queue = communicationQueueDraftS149;
            string error = queue == null ? string.Empty : queue.Validate(kind);
            string mode = CommunicationSimulationCheckS149(kind).IsChecked == true ? "离线模拟 · 原F5执行所选响应" : "模拟未启用 · F5仍沿原真实通讯路径";
            editor.Status.Text = error.Length > 0 ? error : mode + (queue == null ? " · 单样本" : " · " + queue.Samples.Count + "项草稿");
            editor.Status.ToolTip = (error.Length > 0 ? error + "\n" : "") + mode +
                "\n选择/编辑不自动应用或运行，也不启用模拟。1–16项，名称≤64字符，合计UTF-8≤64KiB；坏帧走原异常，不回退旧值。";
            editor.Status.Foreground = error.Length > 0 || CommunicationSimulationCheckS149(kind).IsChecked != true ? Brushes.DarkGoldenrod : Brushes.SlateGray;
        }
        private void MarkCommunicationQueueDraftS149(VmToolKind kind)
        {
            DockDraftValue_Changed(CommunicationSampleBoxS149(kind), null);
            RefreshCommunicationQueueStatusS149(kind);
            string hint = "样本草稿已变；尚未试解析或F5，正式输出仍属于已应用定义。";
            if (kind == VmToolKind.TcpClientExchange) DockTcpSampleResultS117.Text = hint;
            else if (kind == VmToolKind.ModbusTcpExchange) DockModbusSampleResultS118.Text = hint;
            else DockRtuSampleResultS119.Text = hint;
        }
    }
}
