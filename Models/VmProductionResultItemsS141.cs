using System;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmProductionResultItemS141
    {
        [DataMember(Order = 1)] public string ItemId { get; set; }
        [DataMember(Order = 2)] public string Title { get; set; }
        [DataMember(Order = 3)] public string FlowId { get; set; }
        [DataMember(Order = 4)] public string ToolId { get; set; }
        [DataMember(Order = 5)] public string PortName { get; set; }
        [DataMember(Order = 6)] public string ValueType { get; set; }
        // Zero is a real display choice and must be serialized, not omitted as a default.
        [DataMember(Order = 7)] public int DecimalPlaces { get; set; } = 3;

        [OnDeserializing]
        private void Defaults(StreamingContext context) { DecimalPlaces = 3; }

        public VmProductionResultItemS141 Clone()
        {
            string title = (Title ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            if (title.Length > 40) title = title.Substring(0, 40);
            return new VmProductionResultItemS141
            {
                // Missing identity stays missing; truncating an ID could bind another source.
                ItemId = Identity(ItemId), Title = title, FlowId = Identity(FlowId), ToolId = Identity(ToolId),
                PortName = Identity(PortName), ValueType = Identity(ValueType), DecimalPlaces = Math.Max(0, Math.Min(8, DecimalPlaces))
            };
        }

        private static string Identity(string value)
        {
            if (value != null && value.Length > 256) throw new InvalidOperationException("生产结果项来源/身份超过 256 字符，不能截断后重新绑定。");
            return value;
        }
    }

    public sealed class VmProductionResultRowS141
    {
        public string Title { get; set; }
        public string ValueText { get; set; }
        public string UnitText { get; set; }
        public string StateText { get; set; }
        public string DetailText { get; set; }
    }

    // A value-only, detached current-cycle display snapshot. No HObject, image,
    // tool reference, device handle, runtime cache, or inferred judgement belongs here.
    public sealed class VmProductionResultValueS141
    {
        private object numberRaw;
        public string ItemId { get; set; }
        public string ValueType { get; set; }
        public string TextValue { get; set; }
        public string Unit { get; set; }
        public string State { get; set; }
        public string Reason { get; set; }
        public object NumberRaw
        {
            get { return numberRaw; }
            set
            {
                if (value != null && !IsNumberRaw(value)) throw new ArgumentException("当前周期显示快照只能保存有限原生数值，不能保存对象或句柄。");
                numberRaw = value;
            }
        }
        public bool? BooleanValue { get; set; }

        internal static bool IsNumberRaw(object value)
        {
            if (value is double) return !double.IsNaN((double)value) && !double.IsInfinity((double)value);
            if (value is float) return !float.IsNaN((float)value) && !float.IsInfinity((float)value);
            return value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is decimal;
        }
    }
}
