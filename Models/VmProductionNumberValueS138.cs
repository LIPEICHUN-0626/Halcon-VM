using System;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmProductionNumberValueS138
    {
        [DataMember(Order = 1)] public string PortName { get; set; }
        [DataMember(Order = 2)] public string DisplayName { get; set; }
        [DataMember(Order = 3)] public string Unit { get; set; }
        [DataMember(Order = 4)] public string Semantic { get; set; }
        [DataMember(Order = 5)] public string State { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public double? Value { get; set; }
        public VmProductionNumberValueS138 Clone() { return (VmProductionNumberValueS138)MemberwiseClone(); }
        internal int TextLength { get { return Length(PortName) + Length(DisplayName) + Length(Unit) + Length(Semantic) + Length(State); } }
        internal void Normalize(ref bool truncated)
        {
            // Port identity is never shortened into another port. Oversized identity remains invalid.
            DisplayName = VmProductionRunRecordS135.Bound(DisplayName, 128, ref truncated);
            // Unit and semantic are grouping identities too; do not shorten them into a different group.
        }
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(PortName) || PortName.Length > 128 || !VmProductionRunRecordS135.Within(DisplayName, 128) ||
                !VmProductionRunRecordS135.Within(Unit, 64) || !VmProductionRunRecordS135.Within(Semantic, 256)) return "数值端口身份/单位/语义超出预算或缺失。";
            if (State != "Valid" && State != "Missing") return "数值有效状态不明确。";
            if (State == "Valid" ? !Value.HasValue || !VmProductionRunRecordS135.Finite(Value.Value) : Value.HasValue) return "缺失数值不能变为零，有效数值必须有限。";
            return string.Empty;
        }
        private static int Length(string value) { return value == null ? 0 : value.Length; }
    }
}
