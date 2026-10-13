using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmApprovedStationRecipeS137
    {
        [DataMember(Order = 1)] public string Path { get; set; } = string.Empty;
        [DataMember(Order = 2)] public string FileSha256 { get; set; } = string.Empty;
        [DataMember(Order = 3)] public string Name { get; set; } = string.Empty;
        public VmApprovedStationRecipeS137 Clone() { return (VmApprovedStationRecipeS137)MemberwiseClone(); }
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Path) || Path.Length > 4096) return "批准方案路径缺失或超过 4096 字符。";
            try
            {
                if (!System.IO.Path.IsPathRooted(Path) || !string.Equals(System.IO.Path.GetFullPath(Path), Path, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(System.IO.Path.GetExtension(Path), ".json", StringComparison.OrdinalIgnoreCase)) return "批准方案必须使用规范绝对 JSON 路径。";
            }
            catch { return "批准方案路径不合法。"; }
            if (FileSha256 == null || FileSha256.Length != 64 || FileSha256.Any(c => !Uri.IsHexDigit(c))) return "批准方案须保留真实文件 SHA256。";
            if (Name == null || Name.Length > 256) return "批准方案名称超过 256 字符或缺失。";
            return string.Empty;
        }
    }

    // Display/authorization values only: credentials and derived verification values are never exposed.
    public sealed class VmStationProtectionSnapshotS137
    {
        public bool Enabled { get; internal set; }
        public bool IsEngineeringAuthorized { get; internal set; }
        public bool IsFaulted { get; internal set; }
        public string Status { get; internal set; } = string.Empty;
        public long Generation { get; internal set; }
        public DateTime? UnlockedUntilUtc { get; internal set; }
        public int RetryAfterSeconds { get; internal set; }
        public int UnlockMinutes { get; internal set; } = 30;
        public List<VmApprovedStationRecipeS137> ApprovedRecipes { get; internal set; } = new List<VmApprovedStationRecipeS137>();
        public VmStationProtectionSnapshotS137 Clone()
        {
            var clone = (VmStationProtectionSnapshotS137)MemberwiseClone();
            clone.ApprovedRecipes = ApprovedRecipes == null ? new List<VmApprovedStationRecipeS137>() : ApprovedRecipes.Select(item => item == null ? null : item.Clone()).ToList();
            return clone;
        }
    }
    public sealed class VmStationProtectionOperationResultS137
    {
        public bool Success { get; internal set; }
        public string Message { get; internal set; } = string.Empty;
        public VmStationProtectionSnapshotS137 Snapshot { get; internal set; }
    }
}
