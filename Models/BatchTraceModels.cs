using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmImageBatchTraceRecord
    {
        public const int CurrentSchemaVersion = 3;

        [DataMember(Order = 1)] public int SchemaVersion { get; set; }
        [DataMember(Order = 2)] public string BatchId { get; set; }
        [DataMember(Order = 3)] public string RecipeName { get; set; }
        [DataMember(Order = 4)] public string RecipeFingerprint { get; set; }
        [DataMember(Order = 5)] public string StartedAtUtc { get; set; }
        [DataMember(Order = 6)] public string CompletedAtUtc { get; set; }
        [DataMember(Order = 7)] public string Scope { get; set; }
        [DataMember(Order = 8)] public int TotalCount { get; set; }
        [DataMember(Order = 9)] public int OkCount { get; set; }
        [DataMember(Order = 10)] public int NgCount { get; set; }
        [DataMember(Order = 11)] public int ErrorCount { get; set; }
        [DataMember(Order = 12)] public int StoppedCount { get; set; }
        [DataMember(Order = 13)] public int NotRunCount { get; set; }
        [DataMember(Order = 14)] public double TotalElapsedMilliseconds { get; set; }
        [DataMember(Order = 15)] public string Outcome { get; set; }
        [DataMember(Order = 16)] public List<VmImageBatchTraceItem> Items { get; set; }
        [DataMember(Order = 17, EmitDefaultValue = false)] public bool AssetsEnabled { get; set; }

        public string StartedAtText { get { return ToLocalText(StartedAtUtc); } }
        public string CompletedAtText { get { return ToLocalText(CompletedAtUtc); } }
        public string RecipeDisplay { get { return string.IsNullOrWhiteSpace(RecipeName) ? "未命名方案" : RecipeName; } }
        public string ScopeText { get { return NormalizeScopeText(Scope); } }
        public string CountsText { get { return string.Format(CultureInfo.InvariantCulture, "OK {0} / NG {1} / 异常 {2} / 停止 {3} / 未运行 {4}", OkCount, NgCount, ErrorCount, StoppedCount, NotRunCount); } }
        public string ElapsedText { get { return TotalElapsedMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms"; } }
        public string FingerprintShort { get { return string.IsNullOrWhiteSpace(RecipeFingerprint) ? "--" : RecipeFingerprint.Substring(0, Math.Min(12, RecipeFingerprint.Length)); } }

        public void Normalize()
        {
            SchemaVersion = CurrentSchemaVersion;
            if (string.IsNullOrWhiteSpace(BatchId)) BatchId = "LEGACY-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            RecipeName = string.IsNullOrWhiteSpace(RecipeName) ? "未命名方案" : RecipeName.Trim();
            RecipeFingerprint = RecipeFingerprint == null ? string.Empty : RecipeFingerprint.Trim();
            Scope = string.IsNullOrWhiteSpace(Scope) ? "Unknown" : Scope.Trim();
            Items = Items == null ? new List<VmImageBatchTraceItem>() : Items.Where(item => item != null).ToList();
            for (int index = 0; index < Items.Count; index++) Items[index].Normalize(index);
            TotalCount = TotalCount > 0 ? TotalCount : Items.Count;
            if (Items.Count > 0)
            {
                OkCount = Items.Count(item => string.Equals(item.State, "OK", StringComparison.OrdinalIgnoreCase));
                NgCount = Items.Count(item => string.Equals(item.State, "NG", StringComparison.OrdinalIgnoreCase));
                ErrorCount = Items.Count(item => string.Equals(item.State, "Error", StringComparison.OrdinalIgnoreCase));
                StoppedCount = Items.Count(item => string.Equals(item.State, "Stopped", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                OkCount = Math.Max(0, OkCount);
                NgCount = Math.Max(0, NgCount);
                ErrorCount = Math.Max(0, ErrorCount);
                StoppedCount = Math.Max(0, StoppedCount);
            }
            NotRunCount = Math.Max(0, TotalCount - OkCount - NgCount - ErrorCount - StoppedCount);
            if (string.IsNullOrWhiteSpace(Outcome))
            {
                Outcome = StoppedCount > 0 || NotRunCount > 0 ? "Stopped" : (ErrorCount > 0 ? "Error" : (NgCount > 0 ? "NG" : "OK"));
            }
        }

        private static string ToLocalText(string utc)
        {
            DateTime value;
            return DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value)
                ? value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : "--";
        }

        private static string NormalizeScopeText(string scope)
        {
            if (string.Equals(scope, "Current", StringComparison.OrdinalIgnoreCase)) return "当前图片";
            if (string.Equals(scope, "Selected", StringComparison.OrdinalIgnoreCase)) return "选中图片";
            if (string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase)) return "全部图片";
            return string.IsNullOrWhiteSpace(scope) ? "未知" : scope;
        }
    }

    [DataContract]
    public sealed class VmImageBatchTraceItem
    {
        [DataMember(Order = 1)] public int Sequence { get; set; }
        [DataMember(Order = 2)] public string FilePath { get; set; }
        [DataMember(Order = 3)] public string FileName { get; set; }
        [DataMember(Order = 4)] public string State { get; set; }
        [DataMember(Order = 5)] public string ResultCode { get; set; }
        [DataMember(Order = 6)] public double ElapsedMilliseconds { get; set; }
        [DataMember(Order = 7)] public string Message { get; set; }
        [DataMember(Order = 8, EmitDefaultValue = false)] public string SourceImageRelativePath { get; set; }
        [DataMember(Order = 9, EmitDefaultValue = false)] public string ResultImageRelativePath { get; set; }
        [DataMember(Order = 10, EmitDefaultValue = false)] public string ResultImageSource { get; set; }
        [DataMember(Order = 11, EmitDefaultValue = false)] public List<VmBatchTraceLayerSnapshotData> Layers { get; set; }
        [DataMember(Order = 12, EmitDefaultValue = false)] public List<VmBatchTraceToolResultData> ToolResults { get; set; }

        [IgnoreDataMember] public string AssetState { get; set; }
        [IgnoreDataMember] public string AssetMessage { get; set; }
        [IgnoreDataMember] public bool OriginalSourceExists { get; set; }

        public string SequenceText { get { return (Sequence + 1).ToString("00", CultureInfo.InvariantCulture); } }
        public string ElapsedText { get { return ElapsedMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms"; } }
        public string AssetStateText
        {
            get
            {
                if (string.Equals(AssetState, "Ready", StringComparison.OrdinalIgnoreCase)) return OriginalSourceExists ? "可回看" : "源已删·快照可回看";
                if (string.Equals(AssetState, "Corrupt", StringComparison.OrdinalIgnoreCase)) return "快照损坏";
                if (string.Equals(AssetState, "Missing", StringComparison.OrdinalIgnoreCase)) return "快照缺失";
                return "未保存";
            }
        }
        public string LayerCountText { get { return (Layers == null ? 0 : Layers.Count).ToString(CultureInfo.InvariantCulture) + " 层"; } }
        public string ToolResultCountText { get { return (ToolResults == null ? 0 : ToolResults.Count).ToString(CultureInfo.InvariantCulture) + " 个工具"; } }
        public string StateText
        {
            get
            {
                if (string.Equals(State, "Error", StringComparison.OrdinalIgnoreCase)) return "异常";
                if (string.Equals(State, "Stopped", StringComparison.OrdinalIgnoreCase)) return "停止";
                if (string.Equals(State, "Skipped", StringComparison.OrdinalIgnoreCase) || string.Equals(State, "NotRun", StringComparison.OrdinalIgnoreCase)) return "未运行";
                return string.IsNullOrWhiteSpace(State) ? "未运行" : State;
            }
        }

        public void Normalize(int index)
        {
            if (Sequence < 0) Sequence = index;
            FilePath = FilePath ?? string.Empty;
            FileName = string.IsNullOrWhiteSpace(FileName) ? System.IO.Path.GetFileName(FilePath) : FileName;
            State = string.IsNullOrWhiteSpace(State) ? "NotRun" : State;
            ResultCode = string.IsNullOrWhiteSpace(ResultCode) ? "--" : ResultCode;
            Message = Message ?? string.Empty;
            SourceImageRelativePath = SourceImageRelativePath ?? string.Empty;
            ResultImageRelativePath = ResultImageRelativePath ?? string.Empty;
            ResultImageSource = ResultImageSource ?? string.Empty;
            Layers = Layers == null ? new List<VmBatchTraceLayerSnapshotData>() : Layers.Where(layer => layer != null).ToList();
            for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++) Layers[layerIndex].Normalize(layerIndex);
            ToolResults = ToolResults == null ? new List<VmBatchTraceToolResultData>() : ToolResults.Where(result => result != null).ToList();
            for (int resultIndex = 0; resultIndex < ToolResults.Count; resultIndex++) ToolResults[resultIndex].Normalize(resultIndex);
            AssetState = string.IsNullOrWhiteSpace(AssetState) ? (SourceImageRelativePath.Length > 0 || ResultImageRelativePath.Length > 0 ? "Unknown" : "None") : AssetState;
            AssetMessage = AssetMessage ?? string.Empty;
            if (double.IsNaN(ElapsedMilliseconds) || double.IsInfinity(ElapsedMilliseconds) || ElapsedMilliseconds < 0) ElapsedMilliseconds = 0;
        }
    }

    [DataContract]
    public sealed class VmBatchTraceToolResultData
    {
        [DataMember(Order = 1)] public int Sequence { get; set; }
        [DataMember(Order = 2)] public string ToolId { get; set; }
        [DataMember(Order = 3)] public string ToolName { get; set; }
        [DataMember(Order = 4)] public string ToolType { get; set; }
        [DataMember(Order = 5)] public string ResultCode { get; set; }
        [DataMember(Order = 6)] public double ElapsedMilliseconds { get; set; }
        [DataMember(Order = 7, EmitDefaultValue = false)] public string ErrorMessage { get; set; }
        [DataMember(Order = 8, EmitDefaultValue = false)] public List<VmBatchTraceToolOutputData> Outputs { get; set; }

        public string SequenceText { get { return (Sequence + 1).ToString("00", CultureInfo.InvariantCulture); } }
        public string ElapsedText { get { return ElapsedMilliseconds <= 0 ? "--" : ElapsedMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms"; } }
        public string OutputText
        {
            get
            {
                return Outputs == null || Outputs.Count == 0
                    ? "无可持久化 Number/String 输出"
                    : string.Join(" · ", Outputs.Select(output => output.DisplayText));
            }
        }
        public string DetailText
        {
            get
            {
                string detail = string.Format(CultureInfo.InvariantCulture, "#{0} {1} · {2} · {3} · {4}", SequenceText, ToolName, ToolType, ResultCode, ElapsedText);
                return string.IsNullOrWhiteSpace(ErrorMessage) ? detail : detail + " · " + ErrorMessage;
            }
        }
        public string SearchText
        {
            get { return string.Join(" ", new[] { ToolId, ToolName, ToolType, ResultCode, ErrorMessage, OutputText }.Where(value => !string.IsNullOrWhiteSpace(value))); }
        }

        public void Normalize(int index)
        {
            if (Sequence < 0) Sequence = index;
            ToolId = ToolId == null ? string.Empty : ToolId.Trim();
            ToolName = string.IsNullOrWhiteSpace(ToolName) ? "未命名工具" : ToolName.Trim();
            ToolType = string.IsNullOrWhiteSpace(ToolType) ? "未知类型" : ToolType.Trim();
            ResultCode = string.IsNullOrWhiteSpace(ResultCode) ? "--" : ResultCode.Trim();
            ErrorMessage = ErrorMessage == null ? string.Empty : ErrorMessage.Trim();
            Outputs = Outputs == null ? new List<VmBatchTraceToolOutputData>() : Outputs.Where(output => output != null).ToList();
            for (int outputIndex = 0; outputIndex < Outputs.Count; outputIndex++) Outputs[outputIndex].Normalize(outputIndex);
            if (double.IsNaN(ElapsedMilliseconds) || double.IsInfinity(ElapsedMilliseconds) || ElapsedMilliseconds < 0) ElapsedMilliseconds = 0;
        }
    }

    [DataContract]
    public sealed class VmBatchTraceToolOutputData
    {
        [DataMember(Order = 1)] public int Sequence { get; set; }
        [DataMember(Order = 2)] public string PortName { get; set; }
        [DataMember(Order = 3)] public string DisplayName { get; set; }
        [DataMember(Order = 4)] public string DataType { get; set; }
        [DataMember(Order = 5)] public string Value { get; set; }

        public string DisplayText { get { return (string.IsNullOrWhiteSpace(DisplayName) ? PortName : DisplayName) + "=" + Value; } }

        public void Normalize(int index)
        {
            if (Sequence < 0) Sequence = index;
            PortName = PortName == null ? string.Empty : PortName.Trim();
            DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? PortName : DisplayName.Trim();
            DataType = string.Equals(DataType, "Number", StringComparison.OrdinalIgnoreCase) ? "Number" : "String";
            Value = Value == null ? string.Empty : Value;
        }
    }

    [DataContract]
    public sealed class VmBatchTraceLayerSnapshotData
    {
        [DataMember(Order = 1)] public int Sequence { get; set; }
        [DataMember(Order = 2)] public string ToolId { get; set; }
        [DataMember(Order = 3)] public string ToolName { get; set; }
        [DataMember(Order = 4)] public string PortName { get; set; }
        [DataMember(Order = 5)] public string DataType { get; set; }
        [DataMember(Order = 6)] public string RelativePath { get; set; }
        [DataMember(Order = 7)] public string Color { get; set; }
        [DataMember(Order = 8)] public int LineWidth { get; set; }
        [DataMember(Order = 9)] public int Opacity { get; set; }
        [DataMember(Order = 10)] public bool IsVisible { get; set; }
        [DataMember(Order = 11)] public int ObjectCount { get; set; }
        [DataMember(Order = 12)] public string MetricText { get; set; }

        [IgnoreDataMember] public string AssetState { get; set; }
        public string SourceText { get { return (string.IsNullOrWhiteSpace(ToolName) ? "--" : ToolName) + "." + (string.IsNullOrWhiteSpace(PortName) ? "--" : PortName); } }
        public string DisplayText { get { return string.Format(CultureInfo.InvariantCulture, "{0} · {1} · 对象 {2}", SourceText, DataType, ObjectCount); } }

        public void Normalize(int index)
        {
            if (Sequence < 0) Sequence = index;
            ToolId = ToolId ?? string.Empty;
            ToolName = ToolName ?? string.Empty;
            PortName = PortName ?? string.Empty;
            DataType = string.Equals(DataType, "Region", StringComparison.OrdinalIgnoreCase) ? "Region" : "XLD";
            RelativePath = RelativePath ?? string.Empty;
            Color = string.IsNullOrWhiteSpace(Color) ? (DataType == "Region" ? "green" : "yellow") : Color;
            LineWidth = Math.Max(1, Math.Min(8, LineWidth <= 0 ? 2 : LineWidth));
            Opacity = Math.Max(10, Math.Min(100, Opacity <= 0 ? 100 : Opacity));
            ObjectCount = Math.Max(0, ObjectCount);
            MetricText = MetricText ?? string.Empty;
            AssetState = string.IsNullOrWhiteSpace(AssetState) ? "Unknown" : AssetState;
        }
    }

    [DataContract]
    public sealed class VmBatchTraceStorageSettings
    {
        [DataMember(Order = 1)] public bool SaveImages { get; set; }
        [DataMember(Order = 2)] public bool SaveLayers { get; set; }
        [DataMember(Order = 3)] public int MaximumBatchCount { get; set; }
        [DataMember(Order = 4)] public int MaximumMegabytes { get; set; }

        public static VmBatchTraceStorageSettings CreateDefault()
        {
            return new VmBatchTraceStorageSettings { SaveImages = true, SaveLayers = true, MaximumBatchCount = 50, MaximumMegabytes = 2048 };
        }

        public VmBatchTraceStorageSettings Normalize()
        {
            MaximumBatchCount = Math.Max(1, Math.Min(5000, MaximumBatchCount <= 0 ? 50 : MaximumBatchCount));
            MaximumMegabytes = Math.Max(64, Math.Min(102400, MaximumMegabytes <= 0 ? 2048 : MaximumMegabytes));
            return this;
        }
    }

    public sealed class VmBatchTraceLoadResult
    {
        public VmBatchTraceLoadResult()
        {
            Records = new List<VmImageBatchTraceRecord>();
            Warnings = new List<string>();
        }

        public List<VmImageBatchTraceRecord> Records { get; private set; }
        public List<string> Warnings { get; private set; }
    }
}
