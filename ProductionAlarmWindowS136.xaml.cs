using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public sealed class VmProductionAlarmActionEventArgsS136 : EventArgs
    {
        public string OccurrenceId { get; private set; }
        public int ExpectedVersion { get; private set; }
        public string HandlingNote { get; private set; }
        public VmProductionAlarmActionEventArgsS136(string occurrenceId, int expectedVersion, string handlingNote)
        { OccurrenceId = occurrenceId; ExpectedVersion = expectedVersion; HandlingNote = handlingNote ?? string.Empty; }
    }

    // Value-only viewer: it has no algorithm, image, recipe, communication, trigger, or recovery service.
    public partial class ProductionAlarmWindowS136 : Window
    {
        private List<VmProductionAlarmRecordS136> records = new List<VmProductionAlarmRecordS136>();
        private List<string> warnings = new List<string>();
        private VmProductionAlarmRecordS136 selected;
        private bool updating = true, busy, stale;
        private string status = "尚未载入事件。";
        private sealed class Option { public string Key { get; set; } public string Label { get; set; } }
        private sealed class EventRow
        {
            public VmProductionAlarmRecordS136 Record { get; set; }
            public DateTime UtcTime { get { return AsUtc(Record.OccurredAtUtc); } }
            public string LocalTimeText { get { return UtcTime.ToLocalTime().ToString("MM-dd HH:mm:ss", CultureInfo.InvariantCulture); } }
            public string Title { get { return Record.Title; } }
            public string SeverityText { get { return Record.Severity.ToString() == "Error" ? "异常" : "提示"; } }
            public string StateText { get { return StateLabel(Record.State.ToString()); } }
            public string SourceText { get { return SourceLabel(Record.Source.ToString()); } }
        }

        public event EventHandler RefreshRequested;
        public event EventHandler<VmProductionAlarmActionEventArgsS136> AcknowledgeRequested;
        public event EventHandler<VmProductionAlarmActionEventArgsS136> HandleRequested;
        public event EventHandler<VmProductionAlarmActionEventArgsS136> LocateRequested;
        public event EventHandler<VmProductionAlarmActionEventArgsS136> OpenTaskRequestedS150;

        public bool IsSelectedEventCurrentS150(string occurrenceId, int version)
        { return selected != null && !busy && !stale && selected.OccurrenceId == occurrenceId && selected.Version == version; }

        public ProductionAlarmWindowS136()
        {
            InitializeComponent();
            SetOptions(StatePickerS136, new[] { Item("Pending", "未确认 + 已确认待处置"), Item("Open", "仅未确认"), Item("Acknowledged", "仅已确认待处置"), Item("Handled", "已处置（人工说明）"), Item("", "全部载入状态") }, "Pending");
            SetOptions(SeverityPickerS136, new[] { Item("", "全部级别"), Item("Warning", "提示"), Item("Error", "异常") }, "");
            RebuildRecipeOptions(""); RebuildFlowOptions("");
            updating = false; ApplyFilters();
        }

        public void SetRecords(IEnumerable<VmProductionAlarmRecordS136> source, IEnumerable<string> readWarnings)
        {
            // This method is intentionally an atomic UI publication; the caller owns asynchronous read revision checks.
            var next = (source ?? Enumerable.Empty<VmProductionAlarmRecordS136>()).Where(record => record != null).Take(10001).Select(record => record.Clone()).ToList();
            var nextWarnings = (readWarnings ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (next.Count > 10000) { next.RemoveAt(10000); nextWarnings.Add("窗口载入预算最多 10000 事件，超出部分未显示；当前计数不是全部存储事件。"); }
            string recipeKey = Key(RecipePickerS136), flowKey = Key(FlowPickerS136);
            VmProductionAlarmRecordS136 previous = selected;
            records = next; warnings = nextWarnings;
            updating = true;
            try { RebuildRecipeOptions(recipeKey); RebuildFlowOptions(flowKey); }
            finally { updating = false; }
            ApplyFilters();
            if (previous != null)
            {
                var latest = records.FirstOrDefault(record => record.OccurrenceId == previous.OccurrenceId);
                if (latest == null || latest.Version != previous.Version)
                {
                    updating = true; EventsGridS136.SelectedItem = null; updating = false;
                    selected = previous.Clone(); stale = true; ShowSelection();
                    SetStatus("所选事件已变化或已不在载入范围，请重新选择；旧 ID/版本不会偷偷替换成最新事件。");
                }
            }
            WarningsTextS136.Text = warnings.Count == 0 ? "载入未报告读取警告。" : string.Join(Environment.NewLine, warnings.Select((value, index) => (index + 1).ToString(CultureInfo.InvariantCulture) + ". " + value));
            UpdateStatus(); UpdateActions();
        }

        public void SetStatus(string message) { status = message ?? string.Empty; UpdateStatus(); }
        public void SetBusy(bool value)
        {
            busy = value; RefreshButtonS136.IsEnabled = !busy; FiltersGridS136.IsEnabled = !busy;
            EventsGridS136.IsEnabled = !busy; UpdateActions();
        }

        private void UpdateStatus()
        {
            StatusTextS136.Text = status + (warnings.Count == 0 ? string.Empty : " · 读取有 " + warnings.Count + " 项警告，列表可能不完整，见“更多”。");
        }

        private static Option Item(string key, string label) { return new Option { Key = key, Label = label }; }
        private static string Key(ComboBox picker) { return picker.SelectedValue as string ?? string.Empty; }
        private static void SetOptions(ComboBox picker, IEnumerable<Option> options, string key)
        {
            var list = options.ToList(); picker.ItemsSource = list;
            picker.SelectedValue = list.Any(item => item.Key == key) ? key : string.Empty;
        }
        private static string RecipeKey(VmProductionAlarmRecordS136 record)
        {
            if (!string.IsNullOrWhiteSpace(record.RecipeDefinitionSha256)) return "sha:" + record.RecipeDefinitionSha256.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(record.RecipePath)) return "path:" + record.RecipePath.ToUpperInvariant();
            return "session:" + (string.IsNullOrWhiteSpace(record.WorkspaceSessionId) ? record.OccurrenceId : record.WorkspaceSessionId);
        }
        private static string FlowKey(VmProductionAlarmRecordS136 record) { return RecipeKey(record) + "|flow:" + (record.FlowId ?? string.Empty); }
        private static string Short(string text) { return string.IsNullOrEmpty(text) ? "未记录" : text.Length > 10 ? text.Substring(0, 10) : text; }
        private static string PathLabel(string path) { return path.Length > 28 ? "…" + path.Substring(path.Length - 28) : path; }
        private void RebuildRecipeOptions(string selectedKey)
        {
            SetOptions(RecipePickerS136, new[] { Item("", "全部方案身份") }.Concat(records.GroupBy(RecipeKey).Select(group =>
            {
                var r = group.First(); return Item(group.Key, (string.IsNullOrWhiteSpace(r.RecipeName) ? "未命名/未记录" : r.RecipeName) + " · " +
                    (!string.IsNullOrWhiteSpace(r.RecipeDefinitionSha256) ? Short(r.RecipeDefinitionSha256) : !string.IsNullOrWhiteSpace(r.RecipePath)
                        ? "路径 " + PathLabel(r.RecipePath) : "会话 " + Short(string.IsNullOrWhiteSpace(r.WorkspaceSessionId) ? r.OccurrenceId : r.WorkspaceSessionId)));
            }).OrderBy(item => item.Label)), selectedKey);
        }
        private void RebuildFlowOptions(string selectedKey)
        {
            string recipeKey = Key(RecipePickerS136);
            var matching = records.Where(record => recipeKey == "" || RecipeKey(record) == recipeKey);
            SetOptions(FlowPickerS136, new[] { Item("", "全部流程身份") }.Concat(matching.GroupBy(FlowKey).Select(group =>
            {
                var r = group.First(); return Item(group.Key, (string.IsNullOrWhiteSpace(r.FlowName) ? "未记录流程" : r.FlowName) + " · " + Short(r.FlowId) + " / " + Short(r.RecipeDefinitionSha256));
            }).OrderBy(item => item.Label)), selectedKey);
        }
        private void Recipe_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (updating || busy) return;
            updating = true; try { RebuildFlowOptions(""); } finally { updating = false; }
            ApplyFilters();
        }
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (!updating && !busy) ApplyFilters(); }

        private void ApplyFilters()
        {
            string state = Key(StatePickerS136), severity = Key(SeverityPickerS136), recipe = Key(RecipePickerS136), flow = Key(FlowPickerS136);
            string selectedId = selected == null || stale ? null : selected.OccurrenceId;
            var matching = records.Where(record => (state == "" || state == "Pending" ? state != "Pending" || record.State.ToString() != "Handled" : record.State.ToString() == state) &&
                (severity == "" || record.Severity.ToString() == severity) && (recipe == "" || RecipeKey(record) == recipe) && (flow == "" || FlowKey(record) == flow)).OrderByDescending(record => AsUtc(record.OccurredAtUtc)).ToList();
            updating = true;
            try
            {
                var rows = matching.Select(record => new EventRow { Record = record }).ToList(); EventsGridS136.ItemsSource = rows;
                EventsGridS136.SelectedItem = rows.FirstOrDefault(row => row.Record.OccurrenceId == selectedId);
            }
            finally { updating = false; }
            var rowSelected = EventsGridS136.SelectedItem as EventRow;
            selected = rowSelected == null ? null : rowSelected.Record.Clone(); stale = false;
            CountsTextS136.Text = "当前筛选 " + matching.Count + " / 已载入 " + records.Count + " · 未确认 " + matching.Count(record => record.State.ToString() == "Open") +
                " · 已确认待处置 " + matching.Count(record => record.State.ToString() == "Acknowledged") + " · 已处置 " + matching.Count(record => record.State.ToString() == "Handled");
            RefreshAdvancedFilterSummaryS150();
            ShowSelection();
        }

        private void RefreshAdvancedFilterSummaryS150()
        {
            var labels = new List<string>();
            foreach (var entry in new[] { new { Name = "级别", Picker = SeverityPickerS136 }, new { Name = "方案", Picker = RecipePickerS136 }, new { Name = "流程", Picker = FlowPickerS136 } })
                if (Key(entry.Picker).Length > 0)
                {
                    var option = entry.Picker.SelectedItem as Option;
                    labels.Add(entry.Name + "：" + (option == null ? "未知筛选身份" : option.Label));
                }
            AdvancedFilterSummaryPanelS150.Visibility = labels.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            AdvancedFilterSummaryTextS150.Text = "已启用 · " + string.Join(" · ", labels);
            AdvancedFilterSummaryTextS150.ToolTip = string.Join(Environment.NewLine, labels) + "\n高级筛选收起后仍生效；清除只影响查看，不修改事件。";
        }
        private void ClearAdvancedFilters_ClickS150(object sender, RoutedEventArgs e)
        {
            if (busy) return;
            updating = true;
            try { SeverityPickerS136.SelectedValue = ""; RecipePickerS136.SelectedValue = ""; RebuildFlowOptions(""); }
            finally { updating = false; }
            ApplyFilters();
        }

        private void Event_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (updating || busy) return;
            var row = EventsGridS136.SelectedItem as EventRow; selected = row == null ? null : row.Record.Clone(); stale = false;
            HandlingNoteS136.Text = string.Empty; ShowSelection();
        }
        private void ShowSelection()
        {
            SelectionPanelS136.Visibility = selected == null ? Visibility.Collapsed : Visibility.Visible;
            if (selected == null) { TechnicalIdentityS136.Text = "选中事件后可查看稳定身份；查看不会加载方案或重放设备操作。"; UpdateActions(); return; }
            SelectedTitleS136.Text = selected.Title + " · " + StateLabel(selected.State.ToString()) +
                (selected.TextTruncated ? " · 发生文字已截长" : string.Empty);
            SelectedContextS136.Text = SourceLabel(selected.Source.ToString()) + " · " + (selected.RecipeName ?? "未记录方案") + " / " + (selected.FlowName ?? "未记录流程") +
                (string.IsNullOrWhiteSpace(selected.ToolName) ? "" : " / " + selected.ToolName);
            bool shortened;
            SelectedDetailS136.Text = CompactFactPreview(selected.Detail, out shortened) +
                (shortened ? "\n已保存详情较长，展开“更多”查看。" : string.Empty);
            ActionHintS136.Text = stale ? "所显示版本已变化，旧动作已禁用；请重新选择该事件。" : CompactSourceHintS150(selected.Source.ToString());
            ActionHintS136.ToolTip = SourceHint(selected.Source.ToString());
            if (selected.TextTruncated)
                ActionHintS136.Text += " · 发生事实已截长，完整性未知，见更多。";
            HandlingPanelS136.Visibility = selected.State.ToString() == "Acknowledged" ? Visibility.Visible : Visibility.Collapsed;
            TechnicalIdentityS136.Text = "发生 UTC：" + AsUtc(selected.OccurredAtUtc).ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture) +
                "\nOccurrenceId：" + selected.OccurrenceId + " · Version：" + selected.Version + "\n定义 SHA：" + (selected.RecipeDefinitionSha256 ?? "") +
                "\n方案路径：" + (selected.RecipePath ?? "") + "\nFlowId：" + (selected.FlowId ?? "") + " · ToolId：" + (selected.ToolId ?? "") +
                "\nSession：" + (selected.WorkspaceSessionId ?? "") + " · RunId：" + (selected.RunId ?? "未记录") +
                "\n身份提示：" + (selected.IdentityWarning ?? "") + "\n确认 UTC：" + UtcText(selected.AcknowledgedAtUtc) + " · 处置 UTC：" + UtcText(selected.HandledAtUtc) +
                "\n发生文字完整性：" + (selected.TextTruncated ? "TextTruncated=true · 已保存事实已截长，不是完整原文。" : "TextTruncated=false · 记录未标记文字截长。") +
                "\n已保存事实详情：\n" + (selected.Detail ?? "") +
                "\n人工处置说明（完整保存内容）：" + (selected.HandlingNote ?? "") + "\n没有自动恢复、设备成功或现场认证含义。";
            UpdateActions();
        }
        private void UpdateActions()
        {
            bool valid = selected != null && !stale && !busy;
            AcknowledgeButtonS136.IsEnabled = valid && selected.State.ToString() == "Open";
            LocateButtonS136.IsEnabled = valid && !string.IsNullOrWhiteSpace(selected.ToolId);
            OpenTaskButtonS150.IsEnabled = valid && !string.IsNullOrWhiteSpace(selected.ToolId) &&
                !string.IsNullOrWhiteSpace(selected.WorkspaceSessionId) && !string.IsNullOrWhiteSpace(selected.FlowId) &&
                !string.IsNullOrWhiteSpace(selected.RecipeDefinitionSha256);
            HandlingNoteS136.IsEnabled = valid && selected.State.ToString() == "Acknowledged";
            HandleButtonS136.IsEnabled = valid && selected.State.ToString() == "Acknowledged" && !string.IsNullOrWhiteSpace(HandlingNoteS136.Text) &&
                HandlingNoteS136.Text.Length <= VmProductionAlarmRecordS136.MaximumHandlingNoteCharacters;
        }
        private void Note_Changed(object sender, TextChangedEventArgs e) { if (!updating) UpdateActions(); }
        private VmProductionAlarmActionEventArgsS136 FreezeAction(string note)
        {
            if (busy || stale || selected == null) return null;
            VmProductionAlarmRecordS136 snapshot = selected.Clone();
            return new VmProductionAlarmActionEventArgsS136(snapshot.OccurrenceId, snapshot.Version, note);
        }
        private void Acknowledge_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null || selected.State.ToString() != "Open") return;
            var args = FreezeAction(string.Empty); if (args != null && AcknowledgeRequested != null) AcknowledgeRequested(this, args);
        }
        private void Handle_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null || selected.State.ToString() != "Acknowledged" || string.IsNullOrWhiteSpace(HandlingNoteS136.Text)) return;
            if (HandlingNoteS136.Text.Length > VmProductionAlarmRecordS136.MaximumHandlingNoteCharacters)
            { SetStatus("处置说明超过 2048 字符，未提交；请显式缩短，不能静默截断说明。"); return; }
            var args = FreezeAction(HandlingNoteS136.Text.Trim()); if (args != null && HandleRequested != null) HandleRequested(this, args);
        }
        private void Locate_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null || string.IsNullOrWhiteSpace(selected.ToolId)) return;
            var args = FreezeAction(string.Empty); if (args != null && LocateRequested != null) LocateRequested(this, args);
        }
        private void OpenTask_ClickS150(object sender, RoutedEventArgs e)
        {
            if (selected == null || string.IsNullOrWhiteSpace(selected.ToolId)) return;
            var args = FreezeAction(string.Empty); if (args != null && OpenTaskRequestedS150 != null) OpenTaskRequestedS150(this, args);
        }
        private static string CompactSourceHintS150(string source)
        {
            switch (source)
            {
                case "ResultReturnUncertain": return "回传可能已送达，先人工核对设备；不自动重写。";
                case "ChangeoverDoubleFailure": return "双失败保护仍生效，确认或处置不会解除。";
                case "CameraConnectionFailure": return "先核对相机配置；不自动重连。";
                case "TcpCommunicationError": return "先核对连接与设备消息；不重发或启触发。";
                case "RunRecordSaveFailure": return "先核对存储目录；不重跑检测。";
                default: return "确认是已读，处置是人工说明；不代表恢复。";
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) { if (!busy && RefreshRequested != null) RefreshRequested(this, EventArgs.Empty); }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }
        private static DateTime AsUtc(DateTime date) { return date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime(); }
        private static string CompactFactPreview(string text, out bool shortened)
        {
            text = text ?? string.Empty;
            int length = Math.Min(220, text.Length), newlines = 0;
            for (int index = 0; index < length; index++)
                if (text[index] == '\n' && ++newlines == 2) { length = index; break; }
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
            shortened = length < text.Length;
            return shortened ? text.Substring(0, length) + "…" : text;
        }
        private static string UtcText(DateTime? value) { return value.HasValue ? AsUtc(value.Value).ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture) : "未记录"; }
        private static string StateLabel(string state) { return state == "Open" ? "未确认" : state == "Acknowledged" ? "已确认 · 待处置" : state == "Handled" ? "已处置（人工）" : "未识别状态"; }
        private static string SourceLabel(string source)
        {
            switch (source)
            {
                case "RunError": return "运行异常"; case "RunTimeout": return "流程超时"; case "CameraConnectionFailure": return "相机连接失败";
                case "TcpCommunicationError": return "TCP 通讯异常"; case "ResultReturnUncertain": return "结果回传不确定";
                case "ChangeoverDoubleFailure": return "换型与回退双失败"; case "RunRecordSaveFailure": return "周期记录保存失败"; default: return "未识别来源";
            }
        }
        private static string SourceHint(string source)
        {
            switch (source)
            {
                case "ResultReturnUncertain": return "结果可能已经送达；先人工核对设备。确认/处置不会重写，也不会改成已成功。";
                case "ChangeoverDoubleFailure": return "确认不会解除当前运行保护。请显式重新打开可用方案并检查；本窗口不恢复配置或句柄。";
                case "CameraConnectionFailure": return "先核对相机和连接配置；本窗口不自动重连。";
                case "TcpCommunicationError": return "先核对连接与设备消息；本窗口不重发、不重新启用触发。";
                case "RunRecordSaveFailure": return "检测结果不因此改变。请核对存储空间/目录；本窗口不重跑检测。";
                default: return "确认只表示已读；处置须填写人工说明。修复或核对后，由你在原入口显式运行，不能视为自动恢复。";
            }
        }
    }
}
