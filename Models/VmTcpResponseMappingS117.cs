using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    public static class VmTcpResponseModeS117
    {
        public const string None = "None";
        public const string Delimited = "Delimited";
        public const string Json = "Json";

        public static string Normalize(string value)
        {
            return value == Delimited || value == Json ? value : None;
        }

        public static VmToolValueOption[] CreateAll()
        {
            return new[]
            {
                new VmToolValueOption { Key = None, DisplayText = "仅保留原始响应" },
                new VmToolValueOption { Key = Delimited, DisplayText = "按分隔符取字段" },
                new VmToolValueOption { Key = Json, DisplayText = "按 JSON 路径取字段" }
            };
        }
    }

    public static class VmTcpResponseMappingS117
    {
        public static string Validate(VmToolParameterData parameters)
        {
            string mode = VmTcpResponseModeS117.Normalize(parameters.TcpResponseModeS117);
            if (mode == VmTcpResponseModeS117.None) return null;
            if (!parameters.TcpClientWaitForResponse) return "启用响应字段映射时必须等待 TCP 响应。";
            string[] selectors = Selectors(parameters);
            if (selectors.All(string.IsNullOrWhiteSpace)) return "请至少填写一个响应字段位置或 JSON 路径。";
            if (mode == VmTcpResponseModeS117.Delimited &&
                (string.IsNullOrEmpty(parameters.TcpResponseDelimiterS117) || parameters.TcpResponseDelimiterS117.Length > 16 ||
                 parameters.TcpResponseDelimiterS117.Any(char.IsControl)))
                return "响应分隔符须为 1–16 个非控制字符。";
            foreach (string selector in selectors.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                if (selector.Length > 1024) return "响应字段路径不能超过 1024 字符。";
                if (mode == VmTcpResponseModeS117.Delimited)
                {
                    int index;
                    if (!int.TryParse(selector, NumberStyles.None, CultureInfo.InvariantCulture, out index) ||
                        index < 0 || index > 1023 || selector.Length > 1 && selector[0] == '0')
                        return "分隔字段位置须为 0–1023 的非负整数，从 0 开始且不加前导零。";
                }
                else
                {
                    try { VmStructuredDataS110.DecodePath(selector); }
                    catch (InvalidOperationException ex) { return "JSON 响应路径无效：" + ex.Message; }
                }
            }
            return null;
        }

        public static Dictionary<string, object> Parse(string response, VmToolParameterData parameters)
        {
            string error = Validate(parameters);
            if (error != null) throw new InvalidOperationException(error);
            string mode = VmTcpResponseModeS117.Normalize(parameters.TcpResponseModeS117);
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            if (mode == VmTcpResponseModeS117.None) return values;
            if (string.IsNullOrEmpty(response)) throw new InvalidOperationException("响应为空，无法提取配置的字段。");
            string[] selectors = Selectors(parameters);
            string[] names = { "ResponseString", "ResponseNumber", "ResponseBoolean" };
            string[] types = { "String", "Number", "Boolean" };
            if (mode == VmTcpResponseModeS117.Json)
            {
                var fields = new List<VmJsonFieldS110>();
                for (int i = 0; i < selectors.Length; i++)
                    if (!string.IsNullOrWhiteSpace(selectors[i]))
                        fields.Add(new VmJsonFieldS110 { Id = names[i], Name = names[i], Path = selectors[i], DataType = types[i] });
                foreach (var field in VmStructuredDataS110.Read(response.TrimEnd('\r', '\n'), fields))
                    values[field.Key.Substring("Field_".Length)] = field.Value;
                return values;
            }
            string[] parts = response.TrimEnd('\r', '\n').Split(new[] { parameters.TcpResponseDelimiterS117 }, StringSplitOptions.None);
            for (int i = 0; i < selectors.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(selectors[i])) continue;
                int index = int.Parse(selectors[i], CultureInfo.InvariantCulture);
                if (index >= parts.Length) throw new InvalidOperationException(names[i] + "：响应只有 " + parts.Length.ToString(CultureInfo.InvariantCulture) + " 个字段，索引 " + index.ToString(CultureInfo.InvariantCulture) + " 越界。");
                string part = parts[index].Trim();
                if (i == 0) values[names[i]] = part;
                else if (i == 1)
                {
                    double number;
                    if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
                        double.IsNaN(number) || double.IsInfinity(number))
                        throw new InvalidOperationException("ResponseNumber：字段“" + part + "”不是有限数值。使用不变文化小数点。");
                    values[names[i]] = number;
                }
                else
                {
                    bool boolean;
                    if (part == "1") boolean = true;
                    else if (part == "0") boolean = false;
                    else if (!bool.TryParse(part, out boolean))
                        throw new InvalidOperationException("ResponseBoolean：字段“" + part + "”不是 true/false/1/0。");
                    values[names[i]] = boolean;
                }
            }
            return values;
        }

        private static string[] Selectors(VmToolParameterData parameters)
        {
            return new[]
            {
                (parameters.TcpResponseStringSelectorS117 ?? string.Empty).Trim(),
                (parameters.TcpResponseNumberSelectorS117 ?? string.Empty).Trim(),
                (parameters.TcpResponseBooleanSelectorS117 ?? string.Empty).Trim()
            };
        }
    }
}
