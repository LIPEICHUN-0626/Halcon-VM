using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    /// <summary>Value-only, read-only trace viewer. It has no recipe or execution actions.</summary>
    public partial class ProductionRunHistoryWindowS135 : Window
    {
        private bool updatingS135 = true;
        private bool busyS135;
        private List<VmProductionRunRecordS135> recordsS135 = new List<VmProductionRunRecordS135>();
        private List<VmProductionRunRecordS135> filteredS135 = new List<VmProductionRunRecordS135>();
        private List<string> warningsS135 = new List<string>();

        public event EventHandler RefreshRequested;
        public event EventHandler ExportRequested;

        public IReadOnlyList<VmProductionRunRecordS135> FilteredRecords
        {
            get { return filteredS135.Select(record => record.Clone()).ToList().AsReadOnly(); }
        }

        public ProductionRunHistoryWindowS135()
        {
            InitializeComponent();
            SetOptions(TimePickerS135, new[] { Option("1", "最近 24 小时"), Option("7", "最近 7 日"), Option("30", "最近 30 日"), Option("", "全部已载入") }, "1");
            SetOptions(StatePickerS135, new[] { Option("", "全部终态"), Option("OK", "OK · 合格"), Option("NG", "NG · 不合格"), Option("STOP", "STOP · 停止"), Option("TIMEOUT", "TIMEOUT · 超时"), Option("ERROR", "ERROR · 异常") }, "");
            SetOptions(OriginPickerS135, new[] { Option("", "全部来源") }.Concat(Enum.GetValues(typeof(VmRunOriginS135)).Cast<VmRunOriginS135>().Select(origin => Option(origin.ToString(), OriginLabel(origin)))), "");
            SetOptions(PagePickerS135, new[] { Option("", "全部页面（非产品计数）"), Option("Production", "生产页面"), Option("Developer", "开发页面") }, "");
            RebuildRecipeOptions("");
            RebuildFlowOptions("");
            updatingS135 = false;
            InitializeTrendS138();
            InitializeEvidenceS142();
            ApplyFilters();
        }

        public void SetRecords(IEnumerable<VmProductionRunRecordS135> records, IEnumerable<string> warnings)
        {
            string recipeKey = SelectedKey(RecipePickerS135);
            string flowKey = SelectedKey(FlowPickerS135);
            string selectedRunId = (RecordsGridS135.SelectedItem as RunRowS135)?.Record.RunId;
            updatingS135 = true;
            try
            {
                recordsS135 = (records ?? Enumerable.Empty<VmProductionRunRecordS135>())
                    .Where(record => record != null).Select(record => record.Clone())
                    .OrderByDescending(record => record.StartedAtUtc).ThenBy(record => record.RunId, StringComparer.Ordinal).ToList();
                warningsS135 = (warnings ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
                RebuildRecipeOptions(recipeKey);
                RebuildFlowOptions(flowKey);
                ReadWarningsS135.Text = warningsS135.Count == 0 ? "读取警告：无。" :
                    "读取警告（" + warningsS135.Count.ToString(CultureInfo.InvariantCulture) + "）：\n" +
                    string.Join("\n", warningsS135.Take(100).Select(value => Short(value, 1000))) +
                    (warningsS135.Count > 100 ? "\n仅显示前 100 条警告；读取范围不应视为完整历史。" : "");
                MoreExpanderS135.Header = warningsS135.Count == 0 ? "更多 · 所选周期详情 / 读取警告" : "更多 · 所选周期详情 / " + warningsS135.Count + " 条读取警告";
            }
            finally { updatingS135 = false; }
            ApplyFilters(selectedRunId);
            RebuildTrendIdentitiesS138();
        }

        public void SetStatus(string message)
        {
            StatusTextS135.Text = message ?? "";
            StatusTextS135.ToolTip = message ?? "";
        }

        public void SetBusy(bool busy)
        {
            busyS135 = busy;
            RefreshButtonS135.IsEnabled = !busy;
            FiltersGridS135.IsEnabled = !busy;
            ExportButtonS135.IsEnabled = !busy && filteredS135.Count > 0;
            SetTrendBusyS138(busy);
        }

        private static FilterOptionS135 Option(string key, string label) { return new FilterOptionS135 { Key = key, Label = label }; }
        private static string SelectedKey(ComboBox picker) { return picker?.SelectedValue as string ?? ""; }

        private static void SetOptions(ComboBox picker, IEnumerable<FilterOptionS135> options, string preferredKey)
        {
            List<FilterOptionS135> values = options.ToList();
            picker.ItemsSource = values;
            picker.SelectedValue = values.Any(value => string.Equals(value.Key, preferredKey, StringComparison.Ordinal)) ? preferredKey : "";
        }

        private static string RecipeIdentity(VmProductionRunRecordS135 record)
        {
            if (!string.IsNullOrWhiteSpace(record.RecipeDefinitionSha256)) return "sha:" + record.RecipeDefinitionSha256.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(record.RecipePath)) return "path:" + record.RecipePath.ToUpperInvariant();
            return "session:" + (record.WorkspaceSessionId ?? record.RunId ?? "unknown");
        }

        private static string FlowIdentity(VmProductionRunRecordS135 record) { return RecipeIdentity(record) + "|" + record.FlowId; }

        private void RebuildRecipeOptions(string preferredKey)
        {
            var values = new[] { Option("", "全部定义版本") }.Concat(recordsS135.GroupBy(RecipeIdentity, StringComparer.Ordinal)
                .Select(group => group.First()).Select(record => Option(RecipeIdentity(record),
                    (string.IsNullOrWhiteSpace(record.RecipeName) ? "未命名方案" : record.RecipeName) + " · " +
                    (string.IsNullOrWhiteSpace(record.RecipeDefinitionSha256) ? "身份见详情" : Short(record.RecipeDefinitionSha256, 10)) +
                    (record.RecipeWasUnsaved ? "（未保存）" : ""))));
            SetOptions(RecipePickerS135, values, preferredKey);
        }

        private void RebuildFlowOptions(string preferredKey)
        {
            string recipeKey = SelectedKey(RecipePickerS135);
            IEnumerable<VmProductionRunRecordS135> candidates = recordsS135.Where(record => recipeKey.Length == 0 || RecipeIdentity(record) == recipeKey);
            var values = new[] { Option("", "全部流程身份") }.Concat(candidates.GroupBy(FlowIdentity, StringComparer.Ordinal).Select(group => group.First())
                .Select(record => Option(FlowIdentity(record), (string.IsNullOrWhiteSpace(record.FlowName) ? record.FlowId : record.FlowName) +
                    " · " + Short(record.FlowId, 8) + (recipeKey.Length == 0 ? " / " + Short(record.RecipeDefinitionSha256, 8) : ""))));
            SetOptions(FlowPickerS135, values, preferredKey);
        }

        private void ApplyFilters(string preferredRunId = null)
        {
            if (updatingS135 || RecordsGridS135 == null) return;
            string selectedRunId = preferredRunId ?? (RecordsGridS135.SelectedItem as RunRowS135)?.Record.RunId;
            string time = SelectedKey(TimePickerS135), recipe = SelectedKey(RecipePickerS135), flow = SelectedKey(FlowPickerS135);
            string terminal = SelectedKey(StatePickerS135), origin = SelectedKey(OriginPickerS135), page = SelectedKey(PagePickerS135);
            bool strictProduction = OnlyProductionS135.IsChecked == true;
            int days;
            DateTime? fromUtc = int.TryParse(time, NumberStyles.None, CultureInfo.InvariantCulture, out days) ? DateTime.UtcNow.AddDays(-days) : (DateTime?)null;
            filteredS135 = recordsS135.Where(record =>
                (!fromUtc.HasValue || record.StartedAtUtc >= fromUtc.Value) &&
                (recipe.Length == 0 || RecipeIdentity(record) == recipe) && (flow.Length == 0 || FlowIdentity(record) == flow) &&
                (terminal.Length == 0 || record.TerminalState == terminal) && (origin.Length == 0 || record.Origin.ToString() == origin) &&
                (strictProduction ? IsExplicitProduction(record) : page.Length == 0 || record.PageMode == page)).ToList();
            List<RunRowS135> rows = filteredS135.Select(record => new RunRowS135(record)).ToList();
            RecordsGridS135.ItemsSource = rows;
            RecordsGridS135.SelectedItem = rows.FirstOrDefault(row => row.Record.RunId == selectedRunId);
            ShowSelectedRecord();
            StatisticsTextS135.Text = (strictProduction ? "当前生产筛选：" : "当前筛选（非产品计数）：") + filteredS135.Count + " 周期 · " +
                string.Join(" · ", new[] { "OK", "NG", "STOP", "TIMEOUT", "ERROR" }.Select(code => code + " " + filteredS135.Count(record => record.TerminalState == code)));
            ModeCaptionS135.Text = strictProduction ? "明确生产页面的完整周期 · 本地持久记录" : "完整运行周期 · 含开发 / 调试，不能当作产品数量";
            PagePickerS135.Visibility = strictProduction ? Visibility.Collapsed : Visibility.Visible;
            RangeTextS135.Text = "时间按本机时区显示；一行是一次完整运行周期，不等于一个步骤或一件产品。" +
                (warningsS135.Count > 0 ? " 存在读取警告，范围可能不完整。" : " 仅统计本次载入且满足筛选条件的记录。");
            ExportButtonS135.IsEnabled = !busyS135 && filteredS135.Count > 0;
        }

        private static bool IsExplicitProduction(VmProductionRunRecordS135 record)
        {
            return HalconWinFormsDemo.Services.ProductionTrendServiceS138.Qualification(record) == "Production";
        }

        private void ShowSelectedRecord()
        {
            RunRowS135 row = RecordsGridS135.SelectedItem as RunRowS135;
            RefreshSelectedEvidenceS142(row?.Record);
            if (row == null)
            {
                SelectedIdentityS135.Text = "选择周期查看真实身份与步骤；已保存的原图证据可按需查看，不重放运行。";
                SelectedReasonS135.Text = "";
                StepsGridS135.ItemsSource = null;
                StepsGridS135.Visibility = Visibility.Collapsed;
                return;
            }
            VmProductionRunRecordS135 record = row.Record;
            SelectedIdentityS135.Text = "RunId: " + record.RunId + "\nUTC: " + record.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture) + " → " + record.EndedAtUtc.ToString("O", CultureInfo.InvariantCulture) +
                "\n定义 SHA256: " + record.RecipeDefinitionSha256 + "\n方案路径: " + (string.IsNullOrWhiteSpace(record.RecipePath) ? "未落盘" : record.RecipePath) +
                "\n稳定算法键 v" + record.AlgorithmDefinitionKeyVersionS138 + ": " + (record.AlgorithmDefinitionKeyS138 ?? "旧记录未保存") +
                "\n稳定算法定义保存状态: " + (!record.DefinitionSaveKnownS138 ? "未记录 / 无法确定" : record.AlgorithmDefinitionWasUnsavedS138 ? "未保存" : "与开始时已保存定义一致（不证明磁盘未被外部修改）") +
                "\nFlowId: " + record.FlowId + " · 会话: " + record.WorkspaceSessionId + "\n来源: " + OriginLabel(record.Origin) + " · 页面: " + record.PageMode + " · " + record.SourceLabel +
                "\n运行时定义" + (record.RecipeWasUnsaved ? "尚未保存" : "已保存") + (record.HasUnappliedDraft ? "；另有未应用草稿（不包含在定义中）" : "；无未应用草稿") +
                "\n计划 " + record.PlannedStepCount + " · 调度尝试 " + record.TotalStepCount + " · 保存步骤 " + record.CapturedSteps.Count + "（SKIP 为调度跳过，不是算法已执行）" +
                "\n趋势资格: " + HalconWinFormsDemo.Services.ProductionTrendServiceS138.Qualification(record) + " · 模拟事实: " + (!record.SimulationKnownS138 ? "旧记录未知" : record.IsSimulationS138 ? "模拟" : "明确非模拟（不是现场设备认证）") +
                "\n运行输入事实: " + (record.RuntimeInputSummaryS138 ?? "旧记录未保存");
            SelectedReasonS135.Text = (string.IsNullOrWhiteSpace(record.StopReason) ? "结束原因：无附加说明。" : "结束原因：" + record.StopReason) +
                (record.StepsTruncated || record.TextTruncated || record.NumbersTruncatedS138 || record.CapturedSteps.Any(step => step.TextTruncated) ? "\n记录存在有界截断，不能视为完整步骤 / 文本 / 数值。" : "");
            StepsGridS135.ItemsSource = record.CapturedSteps.Select(step => new StepRowS135(step)).ToList();
            StepsGridS135.Visibility = record.CapturedSteps.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { ApplyFilters(); }
        private void ProductionFilter_Changed(object sender, RoutedEventArgs e) { ApplyFilters(); }
        private void Record_Changed(object sender, SelectionChangedEventArgs e) { if (!updatingS135) ShowSelectedRecord(); }
        private void Recipe_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (updatingS135) return;
            string flow = SelectedKey(FlowPickerS135);
            updatingS135 = true;
            try { RebuildFlowOptions(flow); }
            finally { updatingS135 = false; }
            ApplyFilters();
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) { if (!busyS135) RefreshRequested?.Invoke(this, EventArgs.Empty); }
        private void Export_Click(object sender, RoutedEventArgs e) { if (!busyS135 && filteredS135.Count > 0) ExportRequested?.Invoke(this, EventArgs.Empty); }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        private static string OriginLabel(VmRunOriginS135 origin)
        {
            switch (origin)
            {
                case VmRunOriginS135.ManualFull: return "手动整流程";
                case VmRunOriginS135.Continuous: return "连续运行";
                case VmRunOriginS135.TcpTrigger: return "TCP 触发";
                case VmRunOriginS135.Debug: return "调试";
                case VmRunOriginS135.ImageBatch: return "图片批次";
                default: return "来源未知";
            }
        }

        private static string Short(string value, int length)
        {
            value = value ?? "";
            if (value.Length <= length) return value;
            int end = length;
            if (end > 0 && char.IsHighSurrogate(value[end - 1])) end--;
            return value.Substring(0, end) + "…";
        }

        private sealed class FilterOptionS135 { public string Key { get; set; } public string Label { get; set; } }
        private sealed class RunRowS135
        {
            public RunRowS135(VmProductionRunRecordS135 record) { Record = record; }
            public VmProductionRunRecordS135 Record { get; private set; }
            public DateTime UtcTime { get { return Record.StartedAtUtc; } }
            public string LocalTimeText { get { return UtcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture); } }
            public string RecipeText { get { return (string.IsNullOrWhiteSpace(Record.RecipeName) ? "未命名方案" : Record.RecipeName) + (Record.RecipeWasUnsaved ? "（未保存）" : ""); } }
            public string FlowText { get { return string.IsNullOrWhiteSpace(Record.FlowName) ? Record.FlowId : Record.FlowName; } }
            public string OriginText { get { return (IsExplicitProduction(Record) ? "" : Record.PageMode == "Developer" ? "开发 · " : "非生产 · ") + OriginLabel(Record.Origin); } }
            public string TerminalState { get { return Record.TerminalState; } }
            public double ElapsedMilliseconds { get { return Record.ElapsedMilliseconds; } }
            public string ElapsedText { get { return Record.ElapsedMilliseconds.ToString("F2", CultureInfo.CurrentCulture); } }
            public string IdentityTooltip { get { return "RunId: " + Record.RunId + "\n定义 SHA256: " + Record.RecipeDefinitionSha256 + "\nFlowId: " + Record.FlowId + "\n页面: " + Record.PageMode + " · 来源: " + OriginLabel(Record.Origin) + "\n" + Record.SourceLabel; } }
        }

        private sealed class StepRowS135
        {
            private readonly VmProductionRunStepS135 step;
            public StepRowS135(VmProductionRunStepS135 value) { step = value; }
            public int Sequence { get { return step.Sequence; } }
            public string ToolName { get { return step.ToolName; } }
            public string ResultCode { get { return step.ResultCode; } }
            public double ElapsedMilliseconds { get { return step.ElapsedMilliseconds; } }
            public string DetailText { get { return Short(!string.IsNullOrWhiteSpace(step.ErrorMessage) ? step.ErrorMessage : !string.IsNullOrWhiteSpace(step.OutputSummary) ? step.OutputSummary : step.Message, 180); } }
            public string FullText { get { return "ToolId: " + step.ToolId + " · 类型: " + step.ToolType + "\n输入: " + step.InputSummary + "\n输出: " + step.OutputSummary + "\n说明: " + step.Message + "\n异常: " + step.ErrorMessage + (step.TextTruncated ? "\n本步骤文本已截断。" : "") +
                "\nNumber: " + (step.NumberValuesS138 == null ? "旧记录未保存，不补造" : string.Join(" · ", step.NumberValuesS138.Select(n => n.PortName + "=" + (n.Value.HasValue ? n.Value.Value.ToString("R", CultureInfo.InvariantCulture) : "缺失") + " " + (string.IsNullOrEmpty(n.Unit) ? "单位未声明" : n.Unit)))) + (step.NumbersTruncatedS138 ? "（数值有截断）" : ""); } }
        }
    }
}
