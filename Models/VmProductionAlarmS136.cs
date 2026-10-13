using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public enum VmProductionAlarmSourceS136
    {
        [EnumMember] RunError,
        [EnumMember] RunTimeout,
        [EnumMember] CameraConnectionFailure,
        [EnumMember] TcpCommunicationError,
        [EnumMember] ResultReturnUncertain,
        [EnumMember] ChangeoverDoubleFailure,
        [EnumMember] RunRecordSaveFailure
    }
    [DataContract]
    public enum VmProductionAlarmSeverityS136 { [EnumMember] Warning, [EnumMember] Error }
    [DataContract]
    public enum VmProductionAlarmStateS136 { [EnumMember] Open, [EnumMember] Acknowledged, [EnumMember] Handled }

    // Value-only occurrence facts. Acknowledgement is reading; handling is a human note, not recovery.
    [DataContract]
    public sealed class VmProductionAlarmRecordS136
    {
        public const int MaximumOccurrenceTextCharacters = 3072, MaximumHandlingNoteCharacters = 2048;
        [DataMember(Order = 1, IsRequired = true)] public int SchemaVersion { get; set; } = 1;
        [DataMember(Order = 2, IsRequired = true)] public string OccurrenceId { get; set; } = string.Empty;
        [DataMember(Order = 3, IsRequired = true)] public int Version { get; set; } = 1;
        [DataMember(Order = 4, IsRequired = true)] public DateTime OccurredAtUtc { get; set; }
        [DataMember(Order = 5, IsRequired = true)] public VmProductionAlarmSourceS136 Source { get; set; }
        [DataMember(Order = 6, IsRequired = true)] public VmProductionAlarmSeverityS136 Severity { get; set; }
        [DataMember(Order = 7, IsRequired = true)] public VmProductionAlarmStateS136 State { get; set; }
        [DataMember(Order = 8)] public string Title { get; set; } = string.Empty;
        [DataMember(Order = 9)] public string Detail { get; set; } = string.Empty;
        [DataMember(Order = 10)] public string RecipeName { get; set; } = string.Empty;
        [DataMember(Order = 11)] public string RecipePath { get; set; } = string.Empty;
        [DataMember(Order = 12)] public string RecipeDefinitionSha256 { get; set; } = string.Empty;
        [DataMember(Order = 13)] public string WorkspaceSessionId { get; set; } = string.Empty;
        [DataMember(Order = 14)] public string FlowId { get; set; } = string.Empty;
        [DataMember(Order = 15)] public string FlowName { get; set; } = string.Empty;
        [DataMember(Order = 16)] public string ToolId { get; set; } = string.Empty;
        [DataMember(Order = 17)] public string ToolName { get; set; } = string.Empty;
        [DataMember(Order = 18)] public string RunId { get; set; } = string.Empty;
        [DataMember(Order = 19)] public string IdentityWarning { get; set; } = string.Empty;
        [DataMember(Order = 20)] public DateTime? AcknowledgedAtUtc { get; set; }
        [DataMember(Order = 21)] public DateTime? HandledAtUtc { get; set; }
        [DataMember(Order = 22)] public string HandlingNote { get; set; } = string.Empty;
        [DataMember(Order = 23)] public bool TextTruncated { get; set; }

        public VmProductionAlarmRecordS136 Clone() { return (VmProductionAlarmRecordS136)MemberwiseClone(); }

        public VmProductionAlarmRecordS136 NormalizeBounded()
        {
            bool truncated = TextTruncated;
            Title = Bound(Title, 256, ref truncated);
            Detail = Bound(Detail, 8192, ref truncated);
            RecipeName = Bound(RecipeName, 256, ref truncated);
            RecipePath = Bound(RecipePath, 4096, ref truncated);
            FlowName = Bound(FlowName, 256, ref truncated);
            ToolName = Bound(ToolName, 256, ref truncated);
            IdentityWarning = Bound(IdentityWarning, 2048, ref truncated);
            HandlingNote = Bound(HandlingNote, MaximumHandlingNoteCharacters, ref truncated);
            // Reserve space for a later full handling note without rewriting original occurrence facts.
            int remaining = Math.Max(0, MaximumOccurrenceTextCharacters - IdentityText().Sum(value => value == null ? 0 : value.Length));
            Title = Take(Title, ref remaining, ref truncated);
            IdentityWarning = Take(IdentityWarning, ref remaining, ref truncated);
            RecipeName = Take(RecipeName, ref remaining, ref truncated);
            FlowName = Take(FlowName, ref remaining, ref truncated);
            ToolName = Take(ToolName, ref remaining, ref truncated);
            RecipePath = Take(RecipePath, ref remaining, ref truncated);
            Detail = Take(Detail, ref remaining, ref truncated);
            TextTruncated = truncated;
            return this;
        }

        public string Validate()
        {
            if (SchemaVersion != 1) return "不支持的报警记录版本。";
            if (!IsGuidN(OccurrenceId)) return "报警发生 ID 必须为非空 GUID N。";
            if (OccurredAtUtc.Kind != DateTimeKind.Utc || OccurredAtUtc == DateTime.MinValue) return "报警发生时间必须为真实 UTC。";
            if (!Enum.IsDefined(typeof(VmProductionAlarmSourceS136), Source) || !Enum.IsDefined(typeof(VmProductionAlarmSeverityS136), Severity) || !Enum.IsDefined(typeof(VmProductionAlarmStateS136), State)) return "报警来源、级别或状态非法。";
            if (Version != (int)State + 1) return "报警状态与转换版本不一致。";
            if (string.IsNullOrWhiteSpace(Title) || !ValidText(Title, 256) || !ValidText(Detail, 8192)) return "报警标题/事实详情缺失或超长。";
            if (!ValidText(RecipeName, 256) || !ValidText(RecipePath, 4096) || !ValidText(FlowName, 256) || !ValidText(ToolName, 256) || !ValidText(IdentityWarning, 2048)) return "报警显示身份超长或缺失。";
            if (!ValidText(RecipeDefinitionSha256, 64) || (RecipeDefinitionSha256.Length != 0 && (RecipeDefinitionSha256.Length != 64 || RecipeDefinitionSha256.Any(c => !Uri.IsHexDigit(c))))) return "方案定义 SHA256 不合法。";
            if (RecipeDefinitionSha256.Length == 0 && string.IsNullOrWhiteSpace(IdentityWarning)) return "方案定义未知时须明确保留身份说明。";
            if (string.IsNullOrWhiteSpace(WorkspaceSessionId) || !ValidText(WorkspaceSessionId, 128) || !ValidText(FlowId, 256) || !ValidText(ToolId, 512)) return "会话/流程/模块身份缺失或超长。";
            if (!ValidText(RunId, 32) || (RunId.Length != 0 && !IsGuidN(RunId))) return "已知 RunId 必须为 GUID N，未知请留空。";
            if (!ValidText(HandlingNote, MaximumHandlingNoteCharacters)) return "处置备注超过 2048 字符或缺失。";
            if (OccurrenceText().Sum(value => value.Length) > MaximumOccurrenceTextCharacters) return "报警发生事实超过有界文字预算。";
            if (State == VmProductionAlarmStateS136.Open)
            {
                if (AcknowledgedAtUtc.HasValue || HandledAtUtc.HasValue || HandlingNote.Length != 0) return "未确认报警不能带确认/处置事实。";
            }
            else
            {
                if (!AcknowledgedAtUtc.HasValue || AcknowledgedAtUtc.Value.Kind != DateTimeKind.Utc || AcknowledgedAtUtc.Value < OccurredAtUtc) return "确认时间缺失、非 UTC 或早于发生。";
                if (State == VmProductionAlarmStateS136.Acknowledged)
                {
                    if (HandledAtUtc.HasValue || HandlingNote.Length != 0) return "待处置报警不能预填已处置事实。";
                }
                else if (!HandledAtUtc.HasValue || HandledAtUtc.Value.Kind != DateTimeKind.Utc || HandledAtUtc.Value < AcknowledgedAtUtc.Value || string.IsNullOrWhiteSpace(HandlingNote)) return "已处置须保留有效 UTC 时间和明确人工备注。";
            }
            return string.Empty;
        }

        internal IEnumerable<string> OccurrenceText()
        { return IdentityText().Concat(new[] { Title, Detail, RecipeName, RecipePath, FlowName, ToolName, IdentityWarning }); }
        private IEnumerable<string> IdentityText()
        { return new[] { OccurrenceId, RecipeDefinitionSha256, WorkspaceSessionId, FlowId, ToolId, RunId }; }
        internal static bool IsGuidN(string value) { Guid id; return Guid.TryParseExact(value, "N", out id) && id != Guid.Empty; }
        private static bool ValidText(string value, int maximum) { return value != null && value.Length <= maximum; }
        private static string Take(string value, ref int remaining, ref bool truncated)
        { string bounded = Bound(value, remaining, ref truncated); remaining -= bounded.Length; return bounded; }
        private static string Bound(string value, int maximum, ref bool truncated)
        {
            if (value == null) return string.Empty;
            if (value.Length <= maximum) return value;
            truncated = true;
            int length = Math.Max(0, maximum);
            if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
            return value.Substring(0, length);
        }
    }

    public sealed class VmProductionAlarmMutationResultS136
    {
        public bool Success { get; internal set; }
        public VmProductionAlarmRecordS136 Record { get; internal set; }
        public string Warning { get; internal set; } = string.Empty;
        public string ErrorMessage { get; internal set; } = string.Empty;
    }
    public sealed class VmProductionAlarmReadResultS136
    {
        public List<VmProductionAlarmRecordS136> Records { get; private set; } = new List<VmProductionAlarmRecordS136>();
        public List<string> Warnings { get; private set; } = new List<string>();
        public bool Truncated { get; internal set; }
        public string DirectoryPath { get; internal set; } = string.Empty;
    }
}
