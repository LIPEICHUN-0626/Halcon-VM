using System;
using System.Collections.Generic;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    public static class VmArrayReorderS108
    {
        public const string Ascending = "Ascending", Descending = "Descending", Slice = "Slice", Concat = "Concat", Reindex = "Reindex";
        public static bool IsTool(VmToolKind kind) { return kind == VmToolKind.NumberArrayReorder || kind == VmToolKind.TextArrayReorder; }
        public static string DataType(VmToolKind kind) { return kind == VmToolKind.NumberArrayReorder ? "Number[]" : "String[]"; }
        public static string Normalize(string mode) { return string.IsNullOrEmpty(mode) ? Ascending : mode; }
        public static string DisplayName(string mode)
        {
            switch (Normalize(mode))
            {
                case Ascending: return "升序排列";
                case Descending: return "降序排列";
                case Slice: return "保序切片";
                case Concat: return "前后合并";
                case Reindex: return "按索引重排";
                default: return "未知任务";
            }
        }
        public static IEnumerable<VmToolValueOption> Options()
        {
            return new[] { Ascending, Descending, Slice, Concat, Reindex }.Select(mode => new VmToolValueOption { Key = mode, DisplayText = DisplayName(mode) });
        }
        public static string Validate(VmToolParameterData p)
        {
            string mode = Normalize(p.ArrayReorderMode);
            if (!new[] { Ascending, Descending, Slice, Concat, Reindex }.Contains(mode)) return "请选择数组排序、切片、合并或按索引重排任务。";
            if (mode == Slice && (p.ArraySliceStart < 0 || p.ArraySliceStart > VmArrayContractS105.MaxCount || p.ArraySliceLength < 0 || p.ArraySliceLength > VmArrayContractS105.MaxCount))
                return "切片起始索引和数量须为 0–10000 的整数，运行时不得超过输入范围。";
            return null;
        }
        public static int Count(object array)
        {
            if (array is VmNumberArrayS105) return ((VmNumberArrayS105)array).Count;
            if (array is VmStringArrayS105) return ((VmStringArrayS105)array).Count;
            throw new InvalidOperationException("需要正式 Number[] 或 String[]。");
        }
        public static object CopyByIndices(object array, IEnumerable<int> indices)
        {
            var numbers = array as VmNumberArrayS105;
            if (numbers != null) return new VmNumberArrayS105(indices.Select(index => numbers[index]));
            var texts = array as VmStringArrayS105;
            if (texts != null) return new VmStringArrayS105(indices.Select(index => texts[index]));
            throw new InvalidOperationException("需要正式 Number[] 或 String[]。");
        }
    }
}
