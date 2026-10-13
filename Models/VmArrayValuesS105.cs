using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    // Owned, bounded values: never expose the mutable backing array across tools.
    public sealed class VmNumberArrayS105
    {
        private readonly double[] values;
        public int Count { get { return values.Length; } }
        public double this[int index] { get { return values[index]; } }
        public VmNumberArrayS105(IEnumerable<double> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            values = source.Take(VmArrayContractS105.MaxCount + 1).ToArray();
            if (Count > VmArrayContractS105.MaxCount) throw new InvalidOperationException("数组最多 10000 项，不会截断数据。");
            if (values.Any(value => double.IsNaN(value) || double.IsInfinity(value))) throw new InvalidOperationException("数值数组不能包含 NaN 或无穷大。");
        }
        public VmNumberArrayS105 Clone() { return new VmNumberArrayS105(values); }
        public double[] ToArray() { return (double[])values.Clone(); }
        public override string ToString() { return "Number[] · " + Count + " 项 · " + string.Join(", ", values.Take(3).Select(value => value.ToString("G6", CultureInfo.InvariantCulture))) + (Count > 3 ? " …" : ""); }
    }

    public sealed class VmStringArrayS105
    {
        private readonly string[] values;
        public int Count { get { return values.Length; } }
        public string this[int index] { get { return values[index]; } }
        public VmStringArrayS105(IEnumerable<string> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            values = source.Take(VmArrayContractS105.MaxCount + 1).ToArray();
            if (Count > VmArrayContractS105.MaxCount) throw new InvalidOperationException("数组最多 10000 项，不会截断数据。");
            if (values.Any(value => value == null)) throw new InvalidOperationException("文本数组不能包含 null；空文本可保留。");
            if (values.Sum(value => (long)value.Length) > VmArrayContractS105.MaxTextLength) throw new InvalidOperationException("文本数组内容最多 262144 字符。");
        }
        public VmStringArrayS105 Clone() { return new VmStringArrayS105(values); }
        public override string ToString() { return "String[] · " + Count + " 项 · " + string.Join(" | ", values.Take(3).Select(VmArrayContractS105.ShortText)) + (Count > 3 ? " …" : ""); }
    }

    public static class VmArrayContractS105
    {
        public const int MaxCount = 10000;
        public const int MaxTextLength = 262144;
        public static bool IsType(string type) { return type == "Number[]" || type == "String[]"; }
        public static bool IsTool(VmToolKind kind) { return IsSource(kind) || kind == VmToolKind.NumberArrayStatistics || kind == VmToolKind.TextArraySelect; }
        public static bool IsSource(VmToolKind kind) { return kind == VmToolKind.NumberArrayCreate || kind == VmToolKind.TextArraySplit; }
        public static string ShortText(string text)
        {
            text = text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            return text.Length > 32 ? text.Substring(0, 32) + "…" : (text.Length == 0 ? "〈空〉" : text);
        }
        public static void RequireType(string type, object value)
        {
            if (!(type == "Number[]" && value is VmNumberArrayS105) && !(type == "String[]" && value is VmStringArrayS105))
                throw new InvalidOperationException("数组结果与声明类型 " + type + " 不一致，不隐式转换文本或点集。");
        }
        public static object Clone(object value)
        {
            if (value is VmNumberArrayS105) return ((VmNumberArrayS105)value).Clone();
            if (value is VmStringArrayS105) return ((VmStringArrayS105)value).Clone();
            throw new InvalidOperationException("不是正式数值或文本数组。");
        }
        public static string Validate(VmToolKind kind, VmToolParameterData p)
        {
            if (IsSource(kind))
            {
                if ((p.ArrayText ?? "").Length > MaxTextLength) return "数组输入文本最多 262144 字符。";
                if (string.IsNullOrEmpty(p.ArrayDelimiter) || p.ArrayDelimiter.Length > 16) return "字面分隔符必须为 1–16 个字符；不使用正则或隐式转义。";
            }
            else if (p.ArrayIndex < 0 || p.ArrayIndex >= MaxCount) return "元素索引从 0 开始，范围 0–9999；运行时还须小于实际数量。";
            return null;
        }
        public static string[] Split(string text, string delimiter)
        {
            if (text == null || text.Length > MaxTextLength) throw new InvalidOperationException("输入文本为空值或超过 262144 字符。");
            if (string.IsNullOrEmpty(delimiter) || delimiter.Length > 16) throw new InvalidOperationException("字面分隔符必须为 1–16 个字符。");
            if (text.Length == 0) return new string[0];
            var items = text.Split(new[] { delimiter }, MaxCount + 1, StringSplitOptions.None);
            if (items.Length > MaxCount) throw new InvalidOperationException("分割结果超过 10000 项，不会截断或合并剩余项。");
            return items;
        }
    }
}
