using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    // This is a detached export plan, never a second live engineering state.
    public sealed class VmEngineeringPackageEntryS139
    {
        public string Key { get; internal set; } = string.Empty;
        public string Label { get; internal set; } = string.Empty;
        public string Kind { get; internal set; } = string.Empty;
        public string SourcePath { get; internal set; } = string.Empty;
        public string Sha256 { get; internal set; } = string.Empty;
        public long Bytes { get; internal set; }
        public string State { get; internal set; } = "Missing";
        public string Detail { get; internal set; } = string.Empty;
        public string ExpectedSha256 { get; internal set; } = string.Empty;
        internal string StorageIdentityS139 { get; set; } = string.Empty;

        // Applies a relocation only to the detached recipe owned by this plan.
        [IgnoreDataMember]
        internal Action<string> WritePath { get; set; }
    }

    public sealed class VmEngineeringPackagePlanS139
    {
        public List<VmEngineeringPackageEntryS139> Entries { get; internal set; }
            = new List<VmEngineeringPackageEntryS139>();
        public string ReportText { get; internal set; } = string.Empty;
        public bool UnresolvedDependencies { get; internal set; }

        public int BlockingCount
        {
            get { return (Entries ?? new List<VmEngineeringPackageEntryS139>()).Count(entry => entry == null || entry.State != "Ready"); }
        }

        public string Summary
        {
            get
            {
                var entries = Entries ?? new List<VmEngineeringPackageEntryS139>();
                int ready = entries.Count(entry => entry != null && entry.State == "Ready");
                int missing = entries.Count(entry => entry != null && entry.State == "Missing");
                int blocked = entries.Count - ready - missing;
                long uniqueBytes = 0;
                bool overflow = false;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries.Where(item => item != null && item.State == "Ready" && item.Bytes >= 0))
                {
                    // Only equal content identity/size is content deduplication; names alone are not.
                    string identity = string.IsNullOrWhiteSpace(entry.Sha256)
                        ? "path:" + (entry.SourcePath ?? string.Empty)
                        : "sha:" + entry.Sha256 + ":" + entry.Bytes.ToString(CultureInfo.InvariantCulture) + ":" + entry.StorageIdentityS139;
                    if (!seen.Add(identity)) continue;
                    if (uniqueBytes > long.MaxValue - entry.Bytes) { overflow = true; break; }
                    uniqueBytes += entry.Bytes;
                }
                return string.Format(CultureInfo.InvariantCulture,
                    "随行 {0} 项 · 缺失 {1} 项 · 阻断 {2} 项 · 去重后 {3}{4}",
                    ready, missing, blocked,
                    overflow ? "字节总量超出可表示范围" : uniqueBytes.ToString("N0", CultureInfo.InvariantCulture) + " 字节",
                    UnresolvedDependencies ? " · 间接依赖未解析" : string.Empty);
            }
        }

        [IgnoreDataMember] internal VisionRecipe Recipe { get; set; }
        [IgnoreDataMember] internal string ManagedResourceRoot { get; set; }
        [IgnoreDataMember] internal Dictionary<string, string> Relocations { get; set; }
            = new Dictionary<string, string>(StringComparer.Ordinal);
    }

}
