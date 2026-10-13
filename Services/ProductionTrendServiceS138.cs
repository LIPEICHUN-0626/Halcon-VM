using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    // Value-only bounded analytics. No algorithm, alarm, recipe or device dependency.
    public static class ProductionTrendServiceS138
    {
        public sealed class Metric
        {
            public string Key { get; set; }
            public string Label { get; set; }
            public string ToolId { get; set; }
            public string PortName { get; set; }
            public string Unit { get; set; }
            public string Semantic { get; set; }
        }
        public sealed class Point
        {
            public VmProductionRunRecordS135 Record { get; set; }
            public double? Value { get; set; }
            public string Validity { get; set; }
            public string RunId { get { return Record.RunId; } }
            public string LocalTime { get { return Record.StartedAtUtc.ToLocalTime().ToString("MM-dd HH:mm:ss"); } }
            public string State { get { return Record.TerminalState; } }
            public string ValueText { get { return Value.HasValue ? Value.Value.ToString("G8", CultureInfo.CurrentCulture) : "缺失"; } }
        }
        public sealed class Slice
        {
            public string Group { get; set; }
            public string Scope { get; set; }
            public string Range { get; set; }
            public DateTime FrozenAtUtc { get; set; }
            public DateTime? FromUtc { get; set; }
            public bool IncludeIncomplete { get; set; }
            public Metric Metric { get; set; }
            public List<Point> Points { get; set; } = new List<Point>();
            public string Statistics { get; set; }
            public string Warning { get; set; }
        }

        public static string GroupKey(string flowId, string key, int version)
        { return Encode("algorithm", version.ToString(CultureInfo.InvariantCulture), key, flowId); }
        public static string GroupKey(VmProductionRunRecordS135 record)
        {
            return record.AlgorithmDefinitionKeyVersionS138 > 0 && !string.IsNullOrEmpty(record.AlgorithmDefinitionKeyS138)
                ? GroupKey(record.FlowId, record.AlgorithmDefinitionKeyS138, record.AlgorithmDefinitionKeyVersionS138)
                : Encode("legacy-raw-snapshot", record.RecipeDefinitionSha256, record.FlowId);
        }
        public static string Qualification(VmProductionRunRecordS135 r)
        {
            if (!r.SimulationKnownS138) return "Unknown";
            if (r.IsSimulationS138) return "Simulation";
            bool root = r.Origin == VmRunOriginS135.ManualFull || r.Origin == VmRunOriginS135.Continuous || r.Origin == VmRunOriginS135.TcpTrigger;
            if (root && r.PageMode == "Production") return "Production";
            if (r.PageMode == "Developer" && (root || r.Origin == VmRunOriginS135.Debug)) return "Engineering";
            return "Unknown";
        }
        private static bool Incomplete(VmProductionRunRecordS135 r)
        { return (r.AlgorithmDefinitionKeyVersionS138 > 0 ? !r.DefinitionSaveKnownS138 || r.AlgorithmDefinitionWasUnsavedS138 : r.RecipeWasUnsaved) ||
                r.HasUnappliedDraft || r.StepsTruncated || r.TextTruncated || r.NumbersTruncatedS138 || r.CapturedSteps.Any(s => s.TextTruncated || s.NumbersTruncatedS138); }
        private static IEnumerable<VmProductionRunRecordS135> Select(IEnumerable<VmProductionRunRecordS135> records, string group, string scope, int limit, int days, bool include, DateTime now)
        {
            return records.Where(r => GroupKey(r) == group && Qualification(r) == scope &&
                (days == 0 || r.StartedAtUtc >= now.AddDays(-days)) && (include || !Incomplete(r)))
                .OrderByDescending(r => r.StartedAtUtc).ThenBy(r => r.RunId, StringComparer.Ordinal).Take(Math.Min(2000, Math.Max(1, limit)));
        }
        public static List<Metric> Metrics(IEnumerable<VmProductionRunRecordS135> records, string group)
        {
            var metrics = new List<Metric> { new Metric { Key = "elapsed", Label = "周期耗时 · ms", Unit = "ms", PortName = string.Empty, ToolId = string.Empty, Semantic = "CycleElapsed" } };
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in records.Where(r => GroupKey(r) == group).Take(2000))
            foreach (var step in r.CapturedSteps)
            foreach (var value in step.NumberValuesS138 ?? new List<VmProductionNumberValueS138>())
            {
                string key = Encode(step.ToolId, value.PortName, value.Unit, value.Semantic);
                if (!known.Add(key)) continue;
                if (metrics.Count >= 513) return metrics; // 512 selectors, with an explicit caption in the viewer.
                metrics.Add(new Metric { Key = key, ToolId = step.ToolId, PortName = value.PortName, Unit = value.Unit, Semantic = value.Semantic,
                    Label = step.ToolName + " / " + value.DisplayName + " · " + (string.IsNullOrEmpty(value.Unit) ? "单位未声明" : value.Unit) + " · " + Short(step.ToolId) });
            }
            return metrics;
        }
        public static Slice Build(IEnumerable<VmProductionRunRecordS135> records, string group, string scope, Metric metric, int limit, int days, bool include, DateTime now)
        {
            var slice = new Slice { Group = group, Scope = scope, Metric = metric, IncludeIncomplete = include,
                FrozenAtUtc = now, FromUtc = days == 0 ? (DateTime?)null : now.AddDays(-days),
                Range = "最近 " + limit + " 周期" + (days == 0 ? " · 已载入时间范围" : " · 最近 " + days + " 日") };
            var selected = Select(records, group, scope, limit, days, include, now).Select(r => r.Clone()).Reverse().ToList();
            foreach (var r in selected)
            {
                double? number = null; string validity = "本周期未保存此端口 / 未执行 / 截断";
                if (metric.Key == "elapsed") { number = r.ElapsedMilliseconds; validity = "周期耗时"; }
                else
                {
                    var matches = r.CapturedSteps.Where(s => s.ToolId == metric.ToolId).SelectMany(s => s.NumberValuesS138 ?? new List<VmProductionNumberValueS138>())
                        .Where(v => v.PortName == metric.PortName && v.Unit == metric.Unit && v.Semantic == metric.Semantic).Take(2).ToList();
                    if (matches.Count == 1) { number = matches[0].State == "Valid" ? matches[0].Value : null; validity = matches[0].State; }
                    else if (matches.Count > 1) validity = "重复身份，拒绝选择一个值冒充本周期";
                }
                if (number.HasValue && !VmProductionRunRecordS135.Finite(number.Value)) { number = null; validity = "非有限值"; }
                slice.Points.Add(new Point { Record = r, Value = number, Validity = validity });
            }
            int completed = selected.Count(r => r.TerminalState == "OK" || r.TerminalState == "NG");
            int ng = selected.Count(r => r.TerminalState == "NG");
            string states = string.Join(" · ", new[] { "OK", "NG", "ERROR", "TIMEOUT", "STOP" }.Select(s => s + " " + selected.Count(r => r.TerminalState == s)));
            // Descriptive measurements only use completed OK / business NG. Failed-run values remain visible as facts, not product statistics.
            var finite = slice.Points.Where(p => (p.State == "OK" || p.State == "NG") && p.Value.HasValue).Select(p => p.Value.Value).ToList();
            string stats = "完成周期有效值 " + finite.Count + " / " + completed;
            if (finite.Count > 0)
            {
                double min = finite.Min(), max = finite.Max(), mean = 0;
                foreach (double value in finite) mean += value / finite.Count;
                double spread = max - min;
                stats += " · Min " + Format(min) + " · Max " + Format(max) + " · Mean " + Format(mean) + " · 波动 " + Format(spread);
                if (!VmProductionRunRecordS135.Finite(mean) || !VmProductionRunRecordS135.Finite(spread)) slice.Warning = "统计溢出，相关统计不可用；未改变原始值。";
            }
            slice.Statistics = selected.Count + " 周期 · " + states + "\n" + stats +
                (scope == "Production" ? " · 完成周期业务 NG 比例 " + (completed == 0 ? "无分母" : ((double)ng / completed).ToString("P1", CultureInfo.CurrentCulture)) : " · 非生产范围，不计算产品不良率");
            slice.Warning = (slice.Warning ?? string.Empty) + (include ? " 已显式包含未保存/草稿/截断记录，不能视为批准生产能力。" : " 未保存、未应用草稿和截断记录已排除。") +
                " 仅本次已载入窗口；缺失不补零。ERROR/TIMEOUT/STOP 不进业务 NG 分母。非完整 SPC。";
            return slice;
        }
        public static void ExportCsv(string path, Slice slice)
        {
            // Slice was frozen before the Save dialog. Never query live viewer state here.
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                Write(writer, "RunId", "StartedUtc", "RawSnapshotSha256", "AlgorithmKey", "KeyVersion", "FlowId", "Scope", "Origin", "Page", "SimulationKnown", "Simulation", "Terminal", "ToolId", "Port", "Unit", "Semantic", "Validity", "Value", "RawSnapshotUnsaved", "Draft", "Incomplete", "Range", "IncludedIncomplete", "InputFacts", "RangeWarnings", "DefinitionSaveKnown", "AlgorithmDefinitionUnsaved", "FrozenAtUtc", "FromUtc");
                foreach (var point in slice.Points)
                {
                    var r = point.Record;
                    Write(writer, r.RunId, r.StartedAtUtc.ToString("O"), r.RecipeDefinitionSha256, r.AlgorithmDefinitionKeyS138,
                        r.AlgorithmDefinitionKeyVersionS138.ToString(CultureInfo.InvariantCulture), r.FlowId, slice.Scope, r.Origin.ToString(), r.PageMode,
                        r.SimulationKnownS138.ToString(), r.IsSimulationS138.ToString(), r.TerminalState, slice.Metric.ToolId, slice.Metric.PortName,
                        slice.Metric.Unit, slice.Metric.Semantic, point.Validity, point.Value.HasValue ? point.Value.Value.ToString("R", CultureInfo.InvariantCulture) : "",
                        r.RecipeWasUnsaved.ToString(), r.HasUnappliedDraft.ToString(), Incomplete(r).ToString(), slice.Range, slice.IncludeIncomplete.ToString(), r.RuntimeInputSummaryS138, slice.Warning,
                        r.DefinitionSaveKnownS138.ToString(), r.AlgorithmDefinitionWasUnsavedS138.ToString(), slice.FrozenAtUtc.ToString("O"), slice.FromUtc.HasValue ? slice.FromUtc.Value.ToString("O") : "");
                }
            }
        }
        private static void Write(TextWriter writer, params string[] fields)
        { writer.WriteLine(string.Join(",", fields.Select(Csv))); }
        private static string Csv(string text)
        {
            text = text ?? string.Empty;
            if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsControl(text[0]) || text[0] == '\ufeff' || "=+-@".IndexOf(text[0]) >= 0)) text = "'" + text;
            var safe = new StringBuilder();
            foreach (char c in text)
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t') safe.Append("\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else safe.Append(c);
            return "\"" + safe.ToString().Replace("\"", "\"\"") + "\"";
        }
        private static string Encode(params string[] fields)
        { return string.Concat(fields.Select(s => (s ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture) + ":" + (s ?? string.Empty))); }
        private static string Short(string value) { return string.IsNullOrEmpty(value) ? "?" : value.Substring(0, Math.Min(8, value.Length)); }
        private static string Format(double value) { return VmProductionRunRecordS135.Finite(value) ? value.ToString("G6", CultureInfo.CurrentCulture) : "不可用（溢出）"; }
    }
}
