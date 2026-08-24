using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    public static class VmGlobalVariableType
    {
        public const string Number = "Number";
        public const string String = "String";
        public const string Boolean = "Boolean";
        public const string ResultCode = "ResultCode";

        public static string Normalize(string value)
        {
            if (string.Equals(value, String, StringComparison.OrdinalIgnoreCase)) return String;
            if (string.Equals(value, Boolean, StringComparison.OrdinalIgnoreCase)) return Boolean;
            if (string.Equals(value, ResultCode, StringComparison.OrdinalIgnoreCase)) return ResultCode;
            return Number;
        }

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Number, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, String, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Boolean, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ResultCode, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetDisplayText(string value)
        {
            switch (Normalize(value))
            {
                case String: return "字符串";
                case Boolean: return "布尔值";
                case ResultCode: return "结果码";
                default: return "数值";
            }
        }

        public static string GetWritePortName(string value)
        {
            switch (Normalize(value))
            {
                case String: return "TextValue";
                case Boolean: return "BooleanValue";
                case ResultCode: return "ResultCodeValue";
                default: return "NumberValue";
            }
        }
    }

    [DataContract]
    public sealed class VmGlobalVariableData : INotifyPropertyChanged
    {
        private string variableId;
        private string name;
        private string valueType;
        private double defaultNumberValue;
        private double numberValue;
        private string defaultTextValue;
        private string textValue;
        private bool defaultBooleanValue;
        private bool booleanValue;
        private string defaultResultCodeValue;
        private string resultCodeValue;
        private int readerCount;
        private int writerCount;

        public VmGlobalVariableData()
        {
            VariableId = Guid.NewGuid().ToString("N");
            Name = "Variable";
            ValueType = VmGlobalVariableType.Number;
            DefaultTextValue = string.Empty;
            TextValue = string.Empty;
            DefaultResultCodeValue = "OK";
            ResultCodeValue = "OK";
        }

        [DataMember(Order = 1)] public string VariableId { get { return variableId; } set { Set(ref variableId, value); } }
        [DataMember(Order = 2)] public string Name { get { return name; } set { Set(ref name, value); Notify(nameof(DisplayText)); } }
        [DataMember(Order = 3)] public string ValueType { get { return valueType; } set { Set(ref valueType, value); NotifyDisplay(); } }
        [DataMember(Order = 4, EmitDefaultValue = false)] public double DefaultNumberValue { get { return defaultNumberValue; } set { Set(ref defaultNumberValue, value); Notify(nameof(DefaultValueText)); } }
        [DataMember(Order = 5, EmitDefaultValue = false)] public double NumberValue { get { return numberValue; } set { Set(ref numberValue, value); NotifyCurrent(); } }
        [DataMember(Order = 6, EmitDefaultValue = false)] public string DefaultTextValue { get { return defaultTextValue; } set { Set(ref defaultTextValue, value); Notify(nameof(DefaultValueText)); } }
        [DataMember(Order = 7, EmitDefaultValue = false)] public string TextValue { get { return textValue; } set { Set(ref textValue, value); NotifyCurrent(); } }
        [DataMember(Order = 8, EmitDefaultValue = false)] public bool DefaultBooleanValue { get { return defaultBooleanValue; } set { Set(ref defaultBooleanValue, value); Notify(nameof(DefaultValueText)); } }
        [DataMember(Order = 9, EmitDefaultValue = false)] public bool BooleanValue { get { return booleanValue; } set { Set(ref booleanValue, value); NotifyCurrent(); } }
        [DataMember(Order = 10, EmitDefaultValue = false)] public string DefaultResultCodeValue { get { return defaultResultCodeValue; } set { Set(ref defaultResultCodeValue, value); Notify(nameof(DefaultValueText)); } }
        [DataMember(Order = 11, EmitDefaultValue = false)] public string ResultCodeValue { get { return resultCodeValue; } set { Set(ref resultCodeValue, value); NotifyCurrent(); } }

        [IgnoreDataMember] public int ReaderCount { get { return readerCount; } }
        [IgnoreDataMember] public int WriterCount { get { return writerCount; } }
        [IgnoreDataMember] public string TypeDisplayText { get { return VmGlobalVariableType.GetDisplayText(ValueType); } }
        [IgnoreDataMember] public string CurrentValueText { get { return GetValueText(false); } }
        [IgnoreDataMember] public string DefaultValueText { get { return GetValueText(true); } }
        [IgnoreDataMember] public string DisplayText { get { return (Name ?? string.Empty) + "  ·  " + CurrentValueText; } }
        [IgnoreDataMember] public string SubscriptionSummary { get { return "读取 " + ReaderCount.ToString(CultureInfo.InvariantCulture) + " · 写入 " + WriterCount.ToString(CultureInfo.InvariantCulture); } }

        public VmGlobalVariableData CloneNormalized()
        {
            return new VmGlobalVariableData
            {
                VariableId = string.IsNullOrWhiteSpace(VariableId) ? Guid.NewGuid().ToString("N") : VariableId.Trim(),
                Name = string.IsNullOrWhiteSpace(Name) ? "Variable" : Name.Trim(),
                ValueType = VmGlobalVariableType.Normalize(ValueType),
                DefaultNumberValue = DefaultNumberValue,
                NumberValue = NumberValue,
                DefaultTextValue = DefaultTextValue ?? string.Empty,
                TextValue = TextValue ?? string.Empty,
                DefaultBooleanValue = DefaultBooleanValue,
                BooleanValue = BooleanValue,
                DefaultResultCodeValue = NormalizeResultCode(DefaultResultCodeValue),
                ResultCodeValue = NormalizeResultCode(ResultCodeValue)
            };
        }

        public object GetCurrentValue()
        {
            switch (VmGlobalVariableType.Normalize(ValueType))
            {
                case VmGlobalVariableType.String: return TextValue ?? string.Empty;
                case VmGlobalVariableType.Boolean: return BooleanValue;
                case VmGlobalVariableType.ResultCode: return NormalizeResultCode(ResultCodeValue);
                default: return NumberValue;
            }
        }

        public void SetCurrentValue(object value)
        {
            switch (VmGlobalVariableType.Normalize(ValueType))
            {
                case VmGlobalVariableType.String:
                    TextValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    break;
                case VmGlobalVariableType.Boolean:
                    BooleanValue = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    break;
                case VmGlobalVariableType.ResultCode:
                    string code = NormalizeResultCode(Convert.ToString(value, CultureInfo.InvariantCulture));
                    if (code.Length == 0) throw new InvalidOperationException("结果码变量仅允许 OK 或 NG。");
                    ResultCodeValue = code;
                    break;
                default:
                    double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    if (double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidOperationException("数值变量必须是有限值。");
                    NumberValue = number;
                    break;
            }
        }

        public void ResetCurrentToDefault()
        {
            switch (VmGlobalVariableType.Normalize(ValueType))
            {
                case VmGlobalVariableType.String: TextValue = DefaultTextValue ?? string.Empty; break;
                case VmGlobalVariableType.Boolean: BooleanValue = DefaultBooleanValue; break;
                case VmGlobalVariableType.ResultCode: ResultCodeValue = NormalizeResultCode(DefaultResultCodeValue); break;
                default: NumberValue = DefaultNumberValue; break;
            }
        }

        public void SetSubscriptionCounts(int readers, int writers)
        {
            readerCount = Math.Max(0, readers);
            writerCount = Math.Max(0, writers);
            Notify(nameof(ReaderCount));
            Notify(nameof(WriterCount));
            Notify(nameof(SubscriptionSummary));
        }

        public static string NormalizeResultCode(string value)
        {
            string code = (value ?? string.Empty).Trim().ToUpperInvariant();
            return code == "OK" || code == "NG" ? code : string.Empty;
        }

        private string GetValueText(bool useDefault)
        {
            switch (VmGlobalVariableType.Normalize(ValueType))
            {
                case VmGlobalVariableType.String: return useDefault ? (DefaultTextValue ?? string.Empty) : (TextValue ?? string.Empty);
                case VmGlobalVariableType.Boolean: return (useDefault ? DefaultBooleanValue : BooleanValue) ? "True" : "False";
                case VmGlobalVariableType.ResultCode: return useDefault ? NormalizeResultCode(DefaultResultCodeValue) : NormalizeResultCode(ResultCodeValue);
                default: return (useDefault ? DefaultNumberValue : NumberValue).ToString("0.############", CultureInfo.InvariantCulture);
            }
        }

        private void NotifyCurrent() { Notify(nameof(CurrentValueText)); Notify(nameof(DisplayText)); }
        private void NotifyDisplay() { Notify(nameof(TypeDisplayText)); Notify(nameof(CurrentValueText)); Notify(nameof(DefaultValueText)); Notify(nameof(DisplayText)); }
        private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null) { if (Equals(field, value)) return; field = value; Notify(propertyName); }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string propertyName = null) { PropertyChangedEventHandler handler = PropertyChanged; if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName)); }
    }
}
