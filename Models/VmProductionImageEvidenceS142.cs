using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmProductionImagePolicyS142
    {
        [DataMember(Order = 1)] public string FlowId { get; set; }
        [DataMember(Order = 2)] public string Mode { get; set; } = "Off";
        [DataMember(Order = 3)] public bool IncludeStopped { get; set; }
        [DataMember(Order = 4)] public List<string> SourceToolIds { get; set; } = new List<string>();
        [OnDeserializing] private void Defaults(StreamingContext context) { Mode = "Off"; SourceToolIds = new List<string>(); }
        public VmProductionImagePolicyS142 Clone()
        {
            var copy = (VmProductionImagePolicyS142)MemberwiseClone();
            copy.SourceToolIds = SourceToolIds == null ? null : SourceToolIds.ToList();
            return copy;
        }
        public string Validate()
        {
            if (!VmProductionImageEvidenceS142.Identity(FlowId, 256)) return "留图策略流程身份缺失/超限。";
            if (Mode != "Off" && Mode != "Failures" && Mode != "All") return "留图范围必须明确为关闭、失败或全部。";
            if (SourceToolIds == null || SourceToolIds.Count > 4 || SourceToolIds.Any(id => !VmProductionImageEvidenceS142.Identity(id, 256)) ||
                SourceToolIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != SourceToolIds.Count) return "留图来源须为最多四个唯一真实模块身份。";
            if (Mode != "Off" && SourceToolIds.Count == 0) return "启用留图必须显式选择源采集模块。";
            return string.Empty;
        }
    }

    [DataContract]
    public sealed class VmProductionImageEvidenceS142
    {
        public const long MaximumEncodedBytes = 64L * 1024 * 1024;
        [DataMember(Order = 1)] public string RunId { get; set; }
        [DataMember(Order = 2)] public string FlowId { get; set; }
        [DataMember(Order = 3)] public string ToolId { get; set; }
        [DataMember(Order = 4)] public string PortName { get; set; } = "Image";
        [DataMember(Order = 5)] public string CoordinateContext { get; set; }
        [DataMember(Order = 6)] public string SerialNumber { get; set; }
        [DataMember(Order = 7)] public string SourcePath { get; set; }
        [DataMember(Order = 8)] public string CapturedAtUtc { get; set; }
        [DataMember(Order = 9)] public string SourceMode { get; set; }
        [DataMember(Order = 10)] public int Width { get; set; }
        [DataMember(Order = 11)] public int Height { get; set; }
        [DataMember(Order = 12)] public int Channels { get; set; }
        [DataMember(Order = 13)] public string PixelType { get; set; }
        [DataMember(Order = 14)] public string Domain { get; set; } = "Full";
        [DataMember(Order = 15)] public string State { get; set; } = "Pending";
        [DataMember(Order = 16)] public string Detail { get; set; }
        [DataMember(Order = 17)] public string RelativePath { get; set; }
        [DataMember(Order = 18)] public long ByteLength { get; set; }
        [DataMember(Order = 19)] public string Sha256 { get; set; }

        public VmProductionImageEvidenceS142 Clone() { return (VmProductionImageEvidenceS142)MemberwiseClone(); }

        public string Validate()
        {
            Guid run;
            if (!Guid.TryParseExact(RunId, "N", out run) || run == Guid.Empty || !Identity(FlowId, 256) || !Identity(ToolId, 256) || PortName != "Image")
                return "图像证据 RunId/Flow/源模块/Image 端口身份缺失或无效。";
            if (!Text(CoordinateContext, 512) || !Text(SerialNumber, 256) || !Text(SourcePath, 4096) || !Text(CapturedAtUtc, 64) ||
                !Text(SourceMode, 64) || !Text(PixelType, 32) || !Text(Domain, 32) || !Text(Detail, 1024)) return "图像证据元数据超过文字预算。";
            if (State != "Pending" && State != "Missing" && State != "Saved") return "图像证据状态无效，不能归一化掩盖错误。";
            if (State != "Missing")
            {
                string imageError = ValidateImage(); if (imageError.Length > 0) return imageError;
                DateTime captured;
                if (!DateTime.TryParseExact(CapturedAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out captured) || captured.Kind != DateTimeKind.Utc || captured == DateTime.MinValue)
                    return "图像采集时间须为明确 UTC 的 ISO round-trip 时间。";
                if (!Identity(CoordinateContext, 512) || (SourceMode != "File" && SourceMode != "Replay" && SourceMode != "Camera"))
                    return "图像坐标身份或采集模式没有明确冻结。";
            }
            else if (Width < 0 || Height < 0 || Channels < 0) return "缺失图像不能带负尺寸/通道数。";
            if (State == "Saved")
            {
                if (!OwnedRelativePath(RelativePath, RunId) || ByteLength < 45 || ByteLength > MaximumEncodedBytes || !Sha(Sha256))
                    return "已保存图像的受管文件名/长度/SHA 无效；不能改为缺失掩盖已保存错误。";
            }
            else if (!string.IsNullOrEmpty(RelativePath) || ByteLength != 0 || !string.IsNullOrEmpty(Sha256)) return "待处理/缺失图像不能携带已保存文件身份。";
            return string.Empty;
        }

        public string ValidateImage()
        {
            if (PixelType != "byte" || Domain != "Full" || (Channels != 1 && Channels != 3)) return "首版原图证据仅支持 byte、完整有效域与 1/3 通道，不自动缩放或转换。";
            if (Width < 1 || Height < 1 || Width > 16384 || Height > 16384 || (long)Width * Height > 16000000 || (long)Width * Height * Channels > 48L * 1024 * 1024)
                return "图像超出 16384 宽高、1600 万像素或 48 MiB 原始图像预算。";
            return string.Empty;
        }

        internal int TextLength
        {
            get { return Length(RunId) + Length(FlowId) + Length(ToolId) + Length(PortName) + Length(CoordinateContext) + Length(SerialNumber) + Length(SourcePath) +
                Length(CapturedAtUtc) + Length(SourceMode) + Length(PixelType) + Length(Domain) + Length(State) + Length(Detail) + Length(RelativePath) + Length(Sha256); }
        }
        internal static bool OwnedRelativePath(string value, string runId)
        {
            if (value == null || value.Length != 69 || value[32] != '-' || !value.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(value.Substring(0, 32), runId, StringComparison.OrdinalIgnoreCase)) return false;
            Guid file; return Guid.TryParseExact(value.Substring(33, 32), "N", out file) && file != Guid.Empty;
        }
        internal static bool Identity(string value, int maximum) { return !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Any(char.IsControl); }
        internal static bool Sha(string value) { return value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'); }
        private static bool Text(string value, int maximum) { return value == null || value.Length <= maximum; }
        private static int Length(string value) { return value == null ? 0 : value.Length; }
    }
}
