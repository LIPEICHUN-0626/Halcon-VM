using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmEngineeringPackageManifestS139
    {
        [DataMember(Order = 1, IsRequired = true)] public int SchemaVersion { get; set; } = 1;
        [DataMember(Order = 2, IsRequired = true)] public string ResourceDirectory { get; set; }
        [DataMember(Order = 3, IsRequired = true)] public List<VmEngineeringPackageFileS139> Files { get; set; } = new List<VmEngineeringPackageFileS139>();
        [DataMember(Order = 4)] public bool IndirectDependenciesUnresolved { get; set; }
        [DataMember(Order = 5)] public string PreparedAtUtc { get; set; }
    }
    [DataContract]
    public sealed class VmEngineeringPackageFileS139
    {
        [DataMember(Order = 1, IsRequired = true)] public string RelativePath { get; set; }
        [DataMember(Order = 2, IsRequired = true)] public string Sha256 { get; set; }
        [DataMember(Order = 3, IsRequired = true)] public long Bytes { get; set; }
        [DataMember(Order = 4)] public string Kind { get; set; }
    }
}
