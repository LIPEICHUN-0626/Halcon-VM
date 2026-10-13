using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    public static class VmExternalResourceType
    {
        public const string ManagedAssembly = "ManagedAssembly";
        public static string Normalize(string value) { return ManagedAssembly; }
        public static string GetDisplayText(string value) { return "受管 .NET DLL"; }
    }

    public static class VmExternalResourceHealth
    {
        public const string Healthy = "Healthy";
        public const string UpdateAvailable = "UpdateAvailable";
        public const string Missing = "Missing";
        public const string HashMismatch = "HashMismatch";
        public const string Invalid = "Invalid";
        public const string Unchecked = "Unchecked";
    }

    [DataContract]
    public sealed class VmExternalResourceData : INotifyPropertyChanged
    {
        private string name;
        private string sourcePath;
        private string managedRelativePath;
        private string sha256;
        private string assemblyVersion;
        private string defaultTypeName;
        private string defaultMethodName;
        private string healthState;
        private string healthDetail;
        private string managedPath;
        private int referenceCount;

        public VmExternalResourceData()
        {
            ResourceId = Guid.NewGuid().ToString("N");
            ResourceType = VmExternalResourceType.ManagedAssembly;
            Name = "ManagedAssembly";
            DefaultTypeName = "UserScripts.ScriptEntry";
            DefaultMethodName = "Execute";
            HealthState = VmExternalResourceHealth.Unchecked;
            HealthDetail = "尚未检查资源。";
        }

        [DataMember(Order = 1)] public string ResourceId { get; set; }
        [DataMember(Order = 2)] public string ResourceType { get; set; }
        [DataMember(Order = 3)] public string Name { get { return name; } set { name = value; OnPropertyChanged(); OnPropertyChanged("DisplayText"); } }
        [DataMember(Order = 4)] public string SourcePath { get { return sourcePath; } set { sourcePath = value; OnPropertyChanged(); } }
        [DataMember(Order = 5)] public string ManagedRelativePath { get { return managedRelativePath; } set { managedRelativePath = value; OnPropertyChanged(); } }
        [DataMember(Order = 6)] public string Sha256 { get { return sha256; } set { sha256 = value; OnPropertyChanged(); OnPropertyChanged("ShortSha256"); } }
        [DataMember(Order = 7)] public string AssemblyVersion { get { return assemblyVersion; } set { assemblyVersion = value; OnPropertyChanged(); OnPropertyChanged("VersionDisplayText"); OnPropertyChanged("DisplayText"); } }
        [DataMember(Order = 8)] public string DefaultTypeName { get { return defaultTypeName; } set { defaultTypeName = value; OnPropertyChanged(); } }
        [DataMember(Order = 9)] public string DefaultMethodName { get { return defaultMethodName; } set { defaultMethodName = value; OnPropertyChanged(); } }
        [DataMember(Order = 10, EmitDefaultValue = false)] public string ImportedAtUtc { get; set; }
        // A portable resource still has the same ResourceId / byte SHA / signature. Loaded paths are resolved by RecipeService.
        [DataMember(Order = 11, EmitDefaultValue = false)] public string PortablePathS139 { get; set; }

        [IgnoreDataMember] public string HealthState { get { return healthState; } set { healthState = value; OnPropertyChanged(); OnPropertyChanged("HealthDisplayText"); OnPropertyChanged("IsRunnable"); } }
        [IgnoreDataMember] public string HealthDetail { get { return healthDetail; } set { healthDetail = value; OnPropertyChanged(); } }
        [IgnoreDataMember] public string ManagedPath { get { return managedPath; } set { managedPath = value; OnPropertyChanged(); } }
        [IgnoreDataMember] public int ReferenceCount { get { return referenceCount; } set { referenceCount = Math.Max(0, value); OnPropertyChanged(); OnPropertyChanged("ReferenceSummary"); } }

        public string DisplayText { get { return Name + " · " + VersionDisplayText; } }
        public string ResourceTypeDisplayText { get { return VmExternalResourceType.GetDisplayText(ResourceType); } }
        public string VersionDisplayText { get { return "v" + (string.IsNullOrWhiteSpace(AssemblyVersion) ? "--" : AssemblyVersion); } }
        public string ShortSha256 { get { return string.IsNullOrWhiteSpace(Sha256) ? "--" : Sha256.Substring(0, Math.Min(12, Sha256.Length)); } }
        public string ReferenceSummary { get { return "引用 " + ReferenceCount; } }
        public bool IsRunnable { get { return HealthState == VmExternalResourceHealth.Healthy || HealthState == VmExternalResourceHealth.UpdateAvailable; } }
        public string HealthDisplayText
        {
            get
            {
                switch (HealthState)
                {
                    case VmExternalResourceHealth.Healthy: return "可运行";
                    case VmExternalResourceHealth.UpdateAvailable: return "源文件有更新";
                    case VmExternalResourceHealth.Missing: return "副本缺失";
                    case VmExternalResourceHealth.HashMismatch: return "哈希不一致";
                    case VmExternalResourceHealth.Invalid: return "入口无效";
                    default: return "待检查";
                }
            }
        }

        public VmExternalResourceData CloneNormalized()
        {
            return new VmExternalResourceData
            {
                ResourceId = string.IsNullOrWhiteSpace(ResourceId) ? Guid.NewGuid().ToString("N") : ResourceId.Trim(),
                ResourceType = VmExternalResourceType.Normalize(ResourceType),
                Name = NormalizeName(Name, SourcePath),
                SourcePath = (SourcePath ?? string.Empty).Trim(),
                ManagedRelativePath = (ManagedRelativePath ?? string.Empty).Trim(),
                Sha256 = (Sha256 ?? string.Empty).Trim().ToLowerInvariant(),
                AssemblyVersion = string.IsNullOrWhiteSpace(AssemblyVersion) ? "0.0.0.0" : AssemblyVersion.Trim(),
                DefaultTypeName = string.IsNullOrWhiteSpace(DefaultTypeName) ? "UserScripts.ScriptEntry" : DefaultTypeName.Trim(),
                DefaultMethodName = string.IsNullOrWhiteSpace(DefaultMethodName) ? "Execute" : DefaultMethodName.Trim(),
                ImportedAtUtc = ImportedAtUtc,
                PortablePathS139 = PortablePathS139
            };
        }

        private static string NormalizeName(string value, string path)
        {
            string result = (value ?? string.Empty).Trim();
            if (result.Length == 0 && !string.IsNullOrWhiteSpace(path)) result = Path.GetFileNameWithoutExtension(path.Trim());
            if (result.Length == 0) result = "ManagedAssembly";
            return result.Length > 80 ? result.Substring(0, 80) : result;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
