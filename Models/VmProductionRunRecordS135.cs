using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public enum VmRunOriginS135
    {
        [EnumMember] Unknown,
        [EnumMember] ManualFull,
        [EnumMember] Continuous,
        [EnumMember] TcpTrigger,
        [EnumMember] Debug,
        [EnumMember] ImageBatch
    }

    [DataContract]
    public sealed class VmProductionRunStepS135
    {
        [DataMember(Order = 1)] public int Sequence { get; set; }
        [DataMember(Order = 2)] public string ToolId { get; set; }
        [DataMember(Order = 3)] public string ToolName { get; set; }
        [DataMember(Order = 4)] public string ToolType { get; set; }
        [DataMember(Order = 5)] public string ResultCode { get; set; }
        [DataMember(Order = 6)] public double ElapsedMilliseconds { get; set; }
        [DataMember(Order = 7)] public string InputSummary { get; set; }
        [DataMember(Order = 8)] public string OutputSummary { get; set; }
        [DataMember(Order = 9)] public string Message { get; set; }
        [DataMember(Order = 10)] public string ErrorMessage { get; set; }
        [DataMember(Order = 11)] public bool TextTruncated { get; set; }
        [DataMember(Order = 12, EmitDefaultValue = false)] public List<VmProductionNumberValueS138> NumberValuesS138 { get; set; }
        [DataMember(Order = 13, EmitDefaultValue = false)] public bool NumbersTruncatedS138 { get; set; }

        public VmProductionRunStepS135 Clone()
        {
            var copy = (VmProductionRunStepS135)MemberwiseClone();
            copy.NumberValuesS138 = NumberValuesS138 == null ? null : NumberValuesS138.Select(n => n == null ? null : n.Clone()).ToList();
            return copy;
        }

        public VmProductionRunStepS135 NormalizeBounded()
        {
            bool truncated = TextTruncated;
            ToolName = VmProductionRunRecordS135.Bound(ToolName, 256, ref truncated);
            InputSummary = VmProductionRunRecordS135.Bound(InputSummary, 2048, ref truncated);
            OutputSummary = VmProductionRunRecordS135.Bound(OutputSummary, 2048, ref truncated);
            Message = VmProductionRunRecordS135.Bound(Message, 4096, ref truncated);
            ErrorMessage = VmProductionRunRecordS135.Bound(ErrorMessage, 4096, ref truncated);
            if (NumberValuesS138 != null)
            {
                if (NumberValuesS138.Count > 32) { NumberValuesS138 = NumberValuesS138.Take(32).ToList(); NumbersTruncatedS138 = true; }
                foreach (var number in NumberValuesS138) if (number != null) number.Normalize(ref truncated);
            }
            TextTruncated = truncated;
            return this;
        }

        public string Validate()
        {
            if (Sequence < 0 || Sequence > 1000000) return "周期步骤序号超出 0–1000000。";
            if (string.IsNullOrWhiteSpace(ToolId) || ToolId.Length > 512 || string.IsNullOrWhiteSpace(ToolType) || ToolType.Length > 128)
                return "周期步骤的真实模块身份缺失或超过上限。";
            if (ResultCode == null || ResultCode.Length > 32) return "周期步骤结果码缺失或过长。";
            if (!VmProductionRunRecordS135.Finite(ElapsedMilliseconds) || ElapsedMilliseconds < 0) return "步骤耗时须为有限非负数。";
            if (!VmProductionRunRecordS135.Within(ToolName, 256) || !VmProductionRunRecordS135.Within(InputSummary, 2048) ||
                !VmProductionRunRecordS135.Within(OutputSummary, 2048) || !VmProductionRunRecordS135.Within(Message, 4096) || !VmProductionRunRecordS135.Within(ErrorMessage, 4096))
                return "周期步骤文本超过有界摘要上限。";
            if (NumberValuesS138 != null)
            {
                if (NumberValuesS138.Count > 32) return "步骤数值超过 32 项。";
                var ports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var number in NumberValuesS138)
                {
                    if (number == null || !ports.Add(number.PortName ?? string.Empty)) return "数值端口为空或重复。";
                    string error = number.Validate(); if (error.Length > 0) return error;
                }
            }
            return string.Empty;
        }
    }

    [DataContract]
    public sealed class VmProductionRunRecordS135
    {
        public const int MaximumSteps = 2048, MaximumTextCharacters = 131072;
        [DataMember(Order = 1, IsRequired = true)] public int SchemaVersion { get; set; } = 1;
        [DataMember(Order = 2, IsRequired = true)] public string RunId { get; set; }
        [DataMember(Order = 3, IsRequired = true)] public DateTime StartedAtUtc { get; set; }
        [DataMember(Order = 4, IsRequired = true)] public DateTime EndedAtUtc { get; set; }
        [DataMember(Order = 5)] public string RecipeName { get; set; }
        [DataMember(Order = 6)] public string RecipePath { get; set; }
        [DataMember(Order = 7)] public string RecipeDefinitionSha256 { get; set; }
        [DataMember(Order = 8)] public bool RecipeWasUnsaved { get; set; }
        [DataMember(Order = 9)] public bool HasUnappliedDraft { get; set; }
        [DataMember(Order = 10)] public string FlowId { get; set; }
        [DataMember(Order = 11)] public string FlowName { get; set; }
        [DataMember(Order = 12)] public string WorkspaceSessionId { get; set; }
        [DataMember(Order = 13, IsRequired = true)] public VmRunOriginS135 Origin { get; set; }
        [DataMember(Order = 14)] public string PageMode { get; set; }
        [DataMember(Order = 15)] public string SourceLabel { get; set; }
        [DataMember(Order = 16, IsRequired = true)] public string TerminalState { get; set; }
        [DataMember(Order = 17)] public string StopReason { get; set; }
        [DataMember(Order = 18)] public double ElapsedMilliseconds { get; set; }
        [DataMember(Order = 19)] public int PlannedStepCount { get; set; }
        [DataMember(Order = 20)] public int TotalStepCount { get; set; }
        [DataMember(Order = 21)] public List<VmProductionRunStepS135> CapturedSteps { get; set; } = new List<VmProductionRunStepS135>();
        [DataMember(Order = 22)] public bool StepsTruncated { get; set; }
        [DataMember(Order = 23)] public bool TextTruncated { get; set; }
        [DataMember(Order = 24, EmitDefaultValue = false)] public string AlgorithmDefinitionKeyS138 { get; set; }
        [DataMember(Order = 25, EmitDefaultValue = false)] public int AlgorithmDefinitionKeyVersionS138 { get; set; }
        [DataMember(Order = 26, EmitDefaultValue = false)] public bool SimulationKnownS138 { get; set; }
        [DataMember(Order = 27, EmitDefaultValue = false)] public bool IsSimulationS138 { get; set; }
        [DataMember(Order = 28, EmitDefaultValue = false)] public string RuntimeInputSummaryS138 { get; set; }
        [DataMember(Order = 29, EmitDefaultValue = false)] public bool NumbersTruncatedS138 { get; set; }
        [DataMember(Order = 30, EmitDefaultValue = false)] public bool DefinitionSaveKnownS138 { get; set; }
        [DataMember(Order = 31, EmitDefaultValue = false)] public bool AlgorithmDefinitionWasUnsavedS138 { get; set; }
        [DataMember(Order = 32, EmitDefaultValue = false)] public bool EvidenceKnownS142 { get; set; }
        [DataMember(Order = 33, EmitDefaultValue = false)] public string EvidenceStatusS142 { get; set; }
        [DataMember(Order = 34, EmitDefaultValue = false)] public List<VmProductionImageEvidenceS142> EvidenceImagesS142 { get; set; }

        public VmProductionRunRecordS135 Clone()
        {
            var copy = (VmProductionRunRecordS135)MemberwiseClone();
            copy.CapturedSteps = CapturedSteps == null ? null : CapturedSteps.Select(step => step == null ? null : step.Clone()).ToList();
            copy.EvidenceImagesS142 = EvidenceImagesS142 == null ? null : EvidenceImagesS142.Select(image => image == null ? null : image.Clone()).ToList();
            return copy;
        }

        public VmProductionRunRecordS135 NormalizeBounded()
        {
            // Never rewrite identity, UTC, counts, origin or an invalid terminal state to a plausible value.
            bool truncated = TextTruncated;
            RecipeName = Bound(RecipeName, 256, ref truncated);
            RecipePath = Bound(RecipePath, 4096, ref truncated);
            FlowName = Bound(FlowName, 256, ref truncated);
            SourceLabel = Bound(SourceLabel, 512, ref truncated);
            StopReason = Bound(StopReason, 8192, ref truncated);
            RuntimeInputSummaryS138 = Bound(RuntimeInputSummaryS138, 8192, ref truncated);
            int used = Length(RunId) + Length(RecipeName) + Length(RecipePath) + Length(RecipeDefinitionSha256) +
                Length(FlowId) + Length(FlowName) + Length(WorkspaceSessionId) + Length(PageMode) + Length(SourceLabel) + Length(TerminalState) + Length(StopReason) +
                Length(AlgorithmDefinitionKeyS138) + Length(RuntimeInputSummaryS138) + Length(EvidenceStatusS142) +
                (EvidenceImagesS142 == null ? 0 : EvidenceImagesS142.Sum(image => image == null ? 0 : image.TextLength));
            // Evidence fields are frozen facts/file identity: do not truncate or repair them.
            int capturedNumbers = 0;
            if (CapturedSteps != null)
            {
                if (CapturedSteps.Count > MaximumSteps) { CapturedSteps = CapturedSteps.Take(MaximumSteps).ToList(); StepsTruncated = true; }
                var bounded = new List<VmProductionRunStepS135>();
                foreach (var step in CapturedSteps)
                {
                    if (step == null) { bounded.Add(null); continue; } // A malformed value stays invalid; do not hide it.
                    step.NormalizeBounded();
                    if (step.NumberValuesS138 != null)
                    {
                        var numbers = new List<VmProductionNumberValueS138>();
                        foreach (var number in step.NumberValuesS138)
                        {
                            if (number == null) { numbers.Add(null); continue; }
                            if (capturedNumbers >= 512 || used + number.TextLength > MaximumTextCharacters)
                            { step.NumbersTruncatedS138 = true; NumbersTruncatedS138 = true; break; }
                            used += number.TextLength; capturedNumbers++; numbers.Add(number);
                        }
                        step.NumberValuesS138 = numbers;
                        NumbersTruncatedS138 |= step.NumbersTruncatedS138;
                    }
                    int identityLength = Length(step.ToolId) + Length(step.ToolType) + Length(step.ResultCode);
                    if (used + identityLength > MaximumTextCharacters) { StepsTruncated = true; break; }
                    used += identityLength;
                    bool stepTruncated = step.TextTruncated;
                    step.ToolName = Budget(step.ToolName, ref used, ref stepTruncated);
                    step.InputSummary = Budget(step.InputSummary, ref used, ref stepTruncated);
                    step.OutputSummary = Budget(step.OutputSummary, ref used, ref stepTruncated);
                    step.Message = Budget(step.Message, ref used, ref stepTruncated);
                    step.ErrorMessage = Budget(step.ErrorMessage, ref used, ref stepTruncated);
                    step.TextTruncated = stepTruncated;
                    truncated |= stepTruncated;
                    bounded.Add(step);
                }
                CapturedSteps = bounded;
                if (TotalStepCount > CapturedSteps.Count) StepsTruncated = true;
            }
            TextTruncated = truncated;
            return this;
        }

        public string Validate()
        {
            Guid id;
            if (SchemaVersion != 1) return "运行记录版本不受支持。";
            if (!Guid.TryParseExact(RunId, "N", out id) || id == Guid.Empty) return "RunId 须为非空 GUID N 格式。";
            if (StartedAtUtc.Kind != DateTimeKind.Utc || EndedAtUtc.Kind != DateTimeKind.Utc || StartedAtUtc == DateTime.MinValue || EndedAtUtc < StartedAtUtc)
                return "运行起止须为真实 UTC 时间，结束不能早于开始。";
            if (!Enum.IsDefined(typeof(VmRunOriginS135), Origin)) return "运行来源枚举无效，不能猜成生产来源。";
            if (PageMode != "Developer" && PageMode != "Production") return "运行页面模式必须显式为 Developer 或 Production。";
            if (!new[] { "OK", "NG", "STOP", "TIMEOUT", "ERROR" }.Contains(TerminalState)) return "运行终态无效；不把未知状态归一化为 OK。";
            if (!Finite(ElapsedMilliseconds) || ElapsedMilliseconds < 0) return "周期耗时须为有限非负数。";
            if (PlannedStepCount < 0 || PlannedStepCount > 1000000 || TotalStepCount < 0 || TotalStepCount > 1000000)
                return "运行计划/实际步骤数量超出上限。";
            if (CapturedSteps == null || CapturedSteps.Count > MaximumSteps || CapturedSteps.Count > TotalStepCount ||
                TotalStepCount > CapturedSteps.Count && !StepsTruncated) return "步骤快照与真实总数或截断标记不一致。";
            if (string.IsNullOrWhiteSpace(FlowId) || FlowId.Length > 256 || string.IsNullOrWhiteSpace(WorkspaceSessionId) || WorkspaceSessionId.Length > 128)
                return "流程或会话的稳定身份缺失或过长。";
            if (!IsSha256(RecipeDefinitionSha256)) return "方案定义 SHA-256 缺失或无效。";
            if (AlgorithmDefinitionKeyVersionS138 != 0 && (AlgorithmDefinitionKeyVersionS138 != 1 || !IsSha256(AlgorithmDefinitionKeyS138)) ||
                AlgorithmDefinitionKeyVersionS138 == 0 && !string.IsNullOrEmpty(AlgorithmDefinitionKeyS138)) return "稳定算法定义键版本/身份无效。";
            if (IsSimulationS138 && !SimulationKnownS138) return "模拟事实没有明确来源。";
            if (RuntimeInputSummaryS138 != null && RuntimeInputSummaryS138.Length > 8192) return "运行输入摘要超过预算。";
            if (EvidenceKnownS142)
            {
                if (string.IsNullOrWhiteSpace(EvidenceStatusS142) || EvidenceStatusS142.Length > 256 || EvidenceImagesS142 == null || EvidenceImagesS142.Count > 4)
                    return "周期图像证据状态缺失或超过四个来源预算。";
                var evidenceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var evidencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                long evidenceBytes = 0;
                foreach (var image in EvidenceImagesS142)
                {
                    if (image == null || !string.Equals(image.RunId, RunId, StringComparison.OrdinalIgnoreCase) || !string.Equals(image.FlowId, FlowId, StringComparison.OrdinalIgnoreCase) ||
                        !evidenceKeys.Add((image.ToolId ?? "") + "\n" + (image.PortName ?? ""))) return "图像证据源身份重复或不属于本周期/流程。";
                    string imageError = image.Validate(); if (imageError.Length > 0) return imageError;
                    if (image.State == "Pending") return "终态周期不能发布尚未完成的图像证据。";
                    if (image.State == "Saved")
                    {
                        if (PageMode != "Production" || !SimulationKnownS138 || IsSimulationS138 ||
                            (Origin != VmRunOriginS135.ManualFull && Origin != VmRunOriginS135.Continuous && Origin != VmRunOriginS135.TcpTrigger) ||
                            AlgorithmDefinitionKeyVersionS138 != 1 || !IsSha256(AlgorithmDefinitionKeyS138))
                            return "已保存图像必须属于明确非模拟的真实完整生产周期。";
                        DateTime captured;
                        if (!DateTime.TryParseExact(image.CapturedAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out captured) ||
                            captured.Kind != DateTimeKind.Utc || captured < StartedAtUtc || captured > EndedAtUtc) return "源原图采集时间不属于该真实周期起止范围。";
                        if (!evidencePaths.Add(image.RelativePath)) return "同一周期多个来源不能共享冒称同一证据文件。";
                    }
                    evidenceBytes += image.ByteLength;
                }
                if (evidenceBytes > VmProductionImageEvidenceS142.MaximumEncodedBytes) return "周期图像编码总字节超过 64 MiB。";
            }
            else if (!string.IsNullOrEmpty(EvidenceStatusS142) || EvidenceImagesS142 != null && EvidenceImagesS142.Count != 0)
                return "没有明确留图事实的旧记录不能补造证据状态或图片。";
            if (CapturedSteps.Sum(s => s == null || s.NumberValuesS138 == null ? 0 : s.NumberValuesS138.Count) > 512) return "周期数值超过 512 项。";
            if (!Within(RecipeName, 256) || !Within(RecipePath, 4096) || !Within(FlowName, 256) || !Within(SourceLabel, 512) || !Within(StopReason, 8192))
                return "运行记录文本超过有界摘要上限。";
            foreach (var step in CapturedSteps)
            {
                if (step == null) return "周期步骤快照为空。";
                string error = step.Validate(); if (!string.IsNullOrEmpty(error)) return error;
            }
            int characters = Length(RunId) + Length(RecipeName) + Length(RecipePath) + Length(RecipeDefinitionSha256) + Length(FlowId) + Length(FlowName) +
                Length(WorkspaceSessionId) + Length(PageMode) + Length(SourceLabel) + Length(TerminalState) + Length(StopReason) +
                Length(AlgorithmDefinitionKeyS138) + Length(RuntimeInputSummaryS138) + Length(EvidenceStatusS142) +
                (EvidenceImagesS142 == null ? 0 : EvidenceImagesS142.Sum(image => image.TextLength)) +
                CapturedSteps.Sum(step => Length(step.ToolId) + Length(step.ToolName) + Length(step.ToolType) + Length(step.ResultCode) +
                    Length(step.InputSummary) + Length(step.OutputSummary) + Length(step.Message) + Length(step.ErrorMessage) +
                    (step.NumberValuesS138 == null ? 0 : step.NumberValuesS138.Sum(n => n.TextLength)));
            if (characters > MaximumTextCharacters) return "周期文本超过 131072 字符总预算。";
            return string.Empty;
        }

        private static string Budget(string text, ref int used, ref bool truncated)
        {
            string result = Bound(text, Math.Max(0, MaximumTextCharacters - used), ref truncated);
            used += result.Length; return result;
        }
        internal static string Bound(string value, int maximum, ref bool truncated)
        {
            if (value == null) return string.Empty;
            if (value.Length <= maximum) return value;
            int length = Math.Max(0, maximum);
            if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
            truncated = true; return value.Substring(0, length);
        }
        private static int Length(string value) { return value == null ? 0 : value.Length; }
        internal static bool Within(string value, int maximum) { return value != null && value.Length <= maximum; }
        internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static bool IsSha256(string value) { return value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'); }
    }

    public sealed class VmProductionRunSaveResultS135
    {
        public bool Saved { get; internal set; }
        public bool Duplicate { get; internal set; }
        public string Warning { get; internal set; } = string.Empty;
        public string ErrorMessage { get; internal set; } = string.Empty;
    }

    public sealed class VmProductionRunReadResultS135
    {
        public List<VmProductionRunRecordS135> Records { get; private set; } = new List<VmProductionRunRecordS135>();
        public List<string> Warnings { get; private set; } = new List<string>();
        public bool Truncated { get; internal set; }
        public string DirectoryPath { get; internal set; }
    }
}
