using System;
using System.Globalization;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public static class ProductionResultValueServiceS141
    {
        public const string Valid = "Valid";
        public const string Missing = "Missing";
        public const int MaximumTextLength = 1024;

        public static bool IsSupportedType(string type)
        { return type == "Number" || type == "String" || type == "Boolean" || type == "ResultCode"; }

        public static VmProductionResultValueS141 Freeze(VmProductionResultItemS141 item, object raw, string unit, bool fresh, string unavailableReason)
        {
            var value = new VmProductionResultValueS141
            {
                ItemId = item == null ? null : item.ItemId,
                ValueType = item == null ? null : item.ValueType,
                Unit = unit ?? string.Empty, State = Missing
            };
            if (item == null) return Unavailable(value, "没有本周期结果项定义。");
            if (!SourceIdentity(item.ItemId) || !SourceIdentity(item.FlowId) || !SourceIdentity(item.ToolId) || !SourceIdentity(item.PortName))
                return Unavailable(value, "结果项或来源稳定身份缺失/超出预算，不能按名称或索引补绑定。");
            if (!IsSupportedType(item.ValueType)) return Unavailable(value, "输出不是正式 Number/String/Boolean/ResultCode 类型。");
            if (value.Unit.Length > 64 || ContainsControls(value.Unit))
            { value.Unit = string.Empty; return Unavailable(value, "声明单位超过 64 字符或含控制字符，不能截断成另一单位。"); }
            if (!fresh) return Unavailable(value, string.IsNullOrWhiteSpace(unavailableReason) ? "来源没有本周期有效输出。" : unavailableReason);
            if (raw == null) return Unavailable(value, "本周期输出缺失；不会补零或沿用旧值。");
            switch (item.ValueType)
            {
                case "Number":
                    if (!Number(raw)) return Unavailable(value, "Number 输出不是有限原生数值；不解析字符串或读取 HALCON/句柄。");
                    // Keep the original integer/decimal precision; display formatting
                    // never changes the numerical value consumed by the flow.
                    value.NumberRaw = raw;
                    break;
                case "Boolean":
                    if (!(raw is bool)) return Unavailable(value, "Boolean 输出必须是原生布尔值，不从数值或文本转换。");
                    value.BooleanValue = (bool)raw;
                    break;
                case "String":
                case "ResultCode":
                    var text = raw as string;
                    if (text == null) return Unavailable(value, "文本/结果码输出必须是原生字符串，不读取摘要或调用对象 ToString。");
                    if (text.Length > MaximumTextLength) return Unavailable(value, "文本超过 1024 字符，不能将截断内容冒充完整输出。");
                    value.TextValue = text;
                    if (ContainsControls(text)) value.Reason = "原始字符串包含控制字符；仅显示时清理，不改下游文本或判定。";
                    // ResultCode is a displayed typed value, not a new product decision.
                    break;
            }
            value.State = Valid;
            return value;
        }

        public static string Format(VmProductionResultValueS141 value, int decimals)
        {
            if (value == null || value.State != Valid) return "--";
            switch (value.ValueType)
            {
                case "Number":
                    if (!Number(value.NumberRaw)) return "--";
                    return ((IFormattable)value.NumberRaw).ToString("F" + Math.Max(0, Math.Min(8, decimals)).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                case "Boolean": return value.BooleanValue.HasValue ? value.BooleanValue.Value ? "真" : "假" : "--";
                case "String":
                case "ResultCode":
                    if (value.TextValue == null || value.TextValue.Length > MaximumTextLength) return "--";
                    return DisplayText(value.TextValue);
                default: return "--";
            }
        }

        private static bool SourceIdentity(string value)
        { return !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !ContainsControls(value); }

        private static bool Number(object raw)
        { return VmProductionResultValueS141.IsNumberRaw(raw); }

        private static VmProductionResultValueS141 Unavailable(VmProductionResultValueS141 value, string reason)
        {
            value.State = Missing; value.NumberRaw = null; value.TextValue = null; value.BooleanValue = null;
            reason = DisplayText(reason ?? "本周期结果不可用。");
            value.Reason = reason.Length <= 256 ? reason : reason.Substring(0, 239) + "…（原因过长）";
            return value;
        }

        private static bool ContainsControls(string value)
        {
            foreach (char character in value ?? string.Empty) if (char.IsControl(character)) return true;
            return false;
        }

        private static string DisplayText(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (char character in text)
            {
                if (!char.IsControl(character)) builder.Append(character);
                else if (character == '\r' || character == '\n' || character == '\t') builder.Append(' ');
            }
            return builder.ToString();
        }
    }
}
