using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    public static class VmScriptMemberDirection
    {
        public const string Input = "Input";
        public const string Parameter = "Parameter";
        public const string Output = "Output";

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Input;
            if (string.Equals(value, Input, StringComparison.OrdinalIgnoreCase)) return Input;
            if (string.Equals(value, Parameter, StringComparison.OrdinalIgnoreCase)) return Parameter;
            if (string.Equals(value, Output, StringComparison.OrdinalIgnoreCase)) return Output;
            return value.Trim();
        }

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Input, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Parameter, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Output, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class VmScriptScalarType
    {
        public const string Number = "Number";
        public const string String = "String";
        public const string Boolean = "Boolean";
        public const string ResultCode = "ResultCode";

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Number;
            if (string.Equals(value, Number, StringComparison.OrdinalIgnoreCase)) return Number;
            if (string.Equals(value, String, StringComparison.OrdinalIgnoreCase)) return String;
            if (string.Equals(value, Boolean, StringComparison.OrdinalIgnoreCase)) return Boolean;
            if (string.Equals(value, ResultCode, StringComparison.OrdinalIgnoreCase)) return ResultCode;
            return value.Trim();
        }

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Number, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, String, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Boolean, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ResultCode, StringComparison.OrdinalIgnoreCase);
        }

        public static string ToPortDataType(string value)
        {
            string normalized = Normalize(value);
            return string.Equals(normalized, ResultCode, StringComparison.OrdinalIgnoreCase) ? String : normalized;
        }

        public static bool TryParseDefault(string valueType, string text, bool allowEmpty, out object value, out string error)
        {
            value = null;
            error = string.Empty;
            string normalizedType = Normalize(valueType);
            string source = text ?? string.Empty;
            if (source.Length == 0 && allowEmpty)
            {
                return true;
            }

            if (string.Equals(normalizedType, Number, StringComparison.OrdinalIgnoreCase))
            {
                double number;
                if (!double.TryParse(source, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsNaN(number) || double.IsInfinity(number))
                {
                    error = "Number 默认值必须是有限数值（使用英文小数点）。";
                    return false;
                }
                value = number;
                return true;
            }

            if (string.Equals(normalizedType, Boolean, StringComparison.OrdinalIgnoreCase))
            {
                bool boolean;
                if (!bool.TryParse(source, out boolean))
                {
                    error = "Boolean 默认值只能是 true 或 false。";
                    return false;
                }
                value = boolean;
                return true;
            }

            if (string.Equals(normalizedType, ResultCode, StringComparison.OrdinalIgnoreCase))
            {
                string code = source.Trim().ToUpperInvariant();
                if (!string.Equals(code, "OK", StringComparison.Ordinal) && !string.Equals(code, "NG", StringComparison.Ordinal))
                {
                    error = "ResultCode 默认值只能是 OK 或 NG。";
                    return false;
                }
                value = code;
                return true;
            }

            value = source;
            return true;
        }
    }

    [DataContract]
    public sealed class VmScriptInterfaceMemberData
    {
        [DataMember(Order = 1)] public string MemberId { get; set; }
        [DataMember(Order = 2)] public string Direction { get; set; }
        [DataMember(Order = 3)] public string Name { get; set; }
        [DataMember(Order = 4)] public string ValueType { get; set; }
        [DataMember(Order = 5, EmitDefaultValue = false)] public string DefaultValue { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public bool IsRequired { get; set; }
        [DataMember(Order = 7, EmitDefaultValue = false)] public string Description { get; set; }

        public string PortName
        {
            get
            {
                string safeId = new string((MemberId ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
                if (safeId.Length == 0) safeId = "missing";
                return (string.Equals(VmScriptMemberDirection.Normalize(Direction), VmScriptMemberDirection.Output, StringComparison.Ordinal)
                    ? "NamedOutput_"
                    : "NamedInput_") + safeId;
            }
        }

        public string DirectionText
        {
            get
            {
                string normalized = VmScriptMemberDirection.Normalize(Direction);
                if (normalized == VmScriptMemberDirection.Parameter) return "参数";
                if (normalized == VmScriptMemberDirection.Output) return "输出";
                return "输入";
            }
        }

        public string SummaryText
        {
            get { return DirectionText + " · " + VmScriptScalarType.Normalize(ValueType) + (IsRequired ? " · 必需" : string.Empty); }
        }

        public void Normalize()
        {
            MemberId = (MemberId ?? string.Empty).Trim();
            if (MemberId.Length == 0) MemberId = Guid.NewGuid().ToString("N");
            Direction = VmScriptMemberDirection.Normalize(Direction);
            Name = (Name ?? string.Empty).Trim();
            ValueType = VmScriptScalarType.Normalize(ValueType);
            if (DefaultValue == null) DefaultValue = string.Empty;
            Description = (Description ?? string.Empty).Trim();
            if (Direction == VmScriptMemberDirection.Output) DefaultValue = string.Empty;
        }

        public VmScriptInterfaceMemberData Clone()
        {
            VmScriptInterfaceMemberData clone = (VmScriptInterfaceMemberData)MemberwiseClone();
            clone.Normalize();
            return clone;
        }

        public static VmScriptInterfaceMemberData Create(string direction, string valueType, string name)
        {
            VmScriptInterfaceMemberData member = new VmScriptInterfaceMemberData
            {
                MemberId = Guid.NewGuid().ToString("N"),
                Direction = direction,
                ValueType = valueType,
                Name = name,
                DefaultValue = string.Empty,
                Description = string.Empty
            };
            member.Normalize();
            return member;
        }
    }

    [Serializable]
    public sealed class ScriptScalarValue
    {
        public string ValueType { get; private set; }
        public double NumberValue { get; private set; }
        public string TextValue { get; private set; }
        public bool BooleanValue { get; private set; }

        public static ScriptScalarValue FromNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException("value", "Number 必须是有限数值。");
            return new ScriptScalarValue { ValueType = VmScriptScalarType.Number, NumberValue = value, TextValue = string.Empty };
        }

        public static ScriptScalarValue FromString(string value)
        {
            return new ScriptScalarValue { ValueType = VmScriptScalarType.String, TextValue = value ?? string.Empty };
        }

        public static ScriptScalarValue FromBoolean(bool value)
        {
            return new ScriptScalarValue { ValueType = VmScriptScalarType.Boolean, BooleanValue = value, TextValue = value ? "true" : "false" };
        }

        public static ScriptScalarValue FromResultCode(string value)
        {
            string code = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (code != "OK" && code != "NG") throw new ArgumentException("ResultCode 只能是 OK 或 NG。", "value");
            return new ScriptScalarValue { ValueType = VmScriptScalarType.ResultCode, TextValue = code, BooleanValue = code == "OK" };
        }

        public object ToObject()
        {
            if (ValueType == VmScriptScalarType.Number) return NumberValue;
            if (ValueType == VmScriptScalarType.Boolean) return BooleanValue;
            return TextValue ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class ScriptToolContext
    {
        private readonly Dictionary<string, ScriptScalarValue> namedInputs = new Dictionary<string, ScriptScalarValue>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ScriptScalarValue> namedParameters = new Dictionary<string, ScriptScalarValue>(StringComparer.OrdinalIgnoreCase);

        public bool HasNumberA { get; set; }
        public double NumberA { get; set; }
        public bool HasNumberB { get; set; }
        public double NumberB { get; set; }
        public bool HasText { get; set; }
        public string Text { get; set; }
        public bool HasResultCode { get; set; }
        public string ResultCode { get; set; }
        public double ParameterNumber { get; set; }
        public string ParameterText { get; set; }

        public IReadOnlyDictionary<string, ScriptScalarValue> NamedInputs
        {
            get { return new ReadOnlyDictionary<string, ScriptScalarValue>(namedInputs); }
        }

        public IReadOnlyDictionary<string, ScriptScalarValue> NamedParameters
        {
            get { return new ReadOnlyDictionary<string, ScriptScalarValue>(namedParameters); }
        }

        internal void SetNamedInput(string name, ScriptScalarValue value)
        {
            SetNamedValue(namedInputs, name, value, "输入");
        }

        internal void SetNamedParameter(string name, ScriptScalarValue value)
        {
            SetNamedValue(namedParameters, name, value, "参数");
        }

        public bool HasInput(string name) { return namedInputs.ContainsKey(NormalizeName(name)); }
        public bool HasParameter(string name) { return namedParameters.ContainsKey(NormalizeName(name)); }
        public double GetInputNumber(string name) { return GetValue(namedInputs, name, VmScriptScalarType.Number, "输入").NumberValue; }
        public string GetInputString(string name) { return GetValue(namedInputs, name, VmScriptScalarType.String, "输入").TextValue ?? string.Empty; }
        public bool GetInputBoolean(string name) { return GetValue(namedInputs, name, VmScriptScalarType.Boolean, "输入").BooleanValue; }
        public string GetInputResultCode(string name) { return GetValue(namedInputs, name, VmScriptScalarType.ResultCode, "输入").TextValue ?? string.Empty; }
        public double GetParameterNumber(string name) { return GetValue(namedParameters, name, VmScriptScalarType.Number, "参数").NumberValue; }
        public string GetParameterString(string name) { return GetValue(namedParameters, name, VmScriptScalarType.String, "参数").TextValue ?? string.Empty; }
        public bool GetParameterBoolean(string name) { return GetValue(namedParameters, name, VmScriptScalarType.Boolean, "参数").BooleanValue; }
        public string GetParameterResultCode(string name) { return GetValue(namedParameters, name, VmScriptScalarType.ResultCode, "参数").TextValue ?? string.Empty; }

        private static void SetNamedValue(IDictionary<string, ScriptScalarValue> collection, string name, ScriptScalarValue value, string label)
        {
            string key = NormalizeName(name);
            if (value == null) throw new ArgumentNullException("value");
            if (collection.ContainsKey(key)) throw new InvalidOperationException(label + "名称重复：" + key);
            collection.Add(key, value);
        }

        private static ScriptScalarValue GetValue(IDictionary<string, ScriptScalarValue> collection, string name, string type, string label)
        {
            string key = NormalizeName(name);
            ScriptScalarValue value;
            if (!collection.TryGetValue(key, out value)) throw new KeyNotFoundException("未提供脚本" + label + "：" + key);
            if (!string.Equals(value.ValueType, type, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("脚本" + label + " “" + key + "” 的类型是 " + value.ValueType + "，不能按 " + type + " 读取。");
            return value;
        }

        private static string NormalizeName(string name)
        {
            string key = (name ?? string.Empty).Trim();
            if (key.Length == 0) throw new ArgumentException("成员名称不能为空。", "name");
            return key;
        }
    }

    [Serializable]
    public sealed class ScriptToolResult
    {
        private readonly Dictionary<string, ScriptScalarValue> namedOutputs = new Dictionary<string, ScriptScalarValue>(StringComparer.OrdinalIgnoreCase);

        public bool HasNumber { get; set; }
        public double Number { get; set; }
        public string Text { get; set; }
        public string ResultCode { get; set; }
        public bool IsOk { get; set; }

        public IReadOnlyDictionary<string, ScriptScalarValue> NamedOutputs
        {
            get { return new ReadOnlyDictionary<string, ScriptScalarValue>(namedOutputs); }
        }

        public void SetNumber(string name, double value) { SetNamedOutput(name, ScriptScalarValue.FromNumber(value)); }
        public void SetString(string name, string value) { SetNamedOutput(name, ScriptScalarValue.FromString(value)); }
        public void SetBoolean(string name, bool value) { SetNamedOutput(name, ScriptScalarValue.FromBoolean(value)); }
        public void SetResultCode(string name, string value) { SetNamedOutput(name, ScriptScalarValue.FromResultCode(value)); }

        private void SetNamedOutput(string name, ScriptScalarValue value)
        {
            string key = (name ?? string.Empty).Trim();
            if (key.Length == 0) throw new ArgumentException("输出名称不能为空。", "name");
            if (namedOutputs.ContainsKey(key)) throw new InvalidOperationException("脚本重复写入命名输出：" + key);
            namedOutputs.Add(key, value);
        }

        public static ScriptToolResult Ok(double number, string text)
        {
            return new ScriptToolResult
            {
                HasNumber = true,
                Number = number,
                Text = text ?? string.Empty,
                ResultCode = "OK",
                IsOk = true
            };
        }

        public static ScriptToolResult Ng(double number, string text)
        {
            return new ScriptToolResult
            {
                HasNumber = true,
                Number = number,
                Text = text ?? string.Empty,
                ResultCode = "NG",
                IsOk = false
            };
        }
    }

    public sealed class ScriptToolExecutionRequest
    {
        public string Mode { get; set; }
        public string InlineCode { get; set; }
        public string AssemblyPath { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public ScriptToolContext Context { get; set; }
    }

    public sealed class ScriptToolExecutionReport
    {
        public ScriptToolResult Result { get; set; }
        public string AssemblySha256 { get; set; }
        public string AssemblyVersion { get; set; }
        public string Diagnostics { get; set; }
        public double ElapsedMilliseconds { get; set; }
    }
}
