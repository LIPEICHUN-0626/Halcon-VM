using System;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmConnectedObjectParametersS144
    {
        [DataMember(Order = 1)] public double MinimumArea { get; set; } = 1;
        [DataMember(Order = 2)] public double MaximumArea { get; set; } = 100000;
        [DataMember(Order = 3)] public string SortMode { get; set; } = "area-desc";
        [DataMember(Order = 4)] public int SelectionIndex { get; set; } = 0;
        public VmConnectedObjectParametersS144 Clone() { return (VmConnectedObjectParametersS144)MemberwiseClone(); }
        public string Validate()
        {
            if (!VmImageEdgeLineParametersS143.Finite(MinimumArea) || !VmImageEdgeLineParametersS143.Finite(MaximumArea) ||
                MinimumArea < 1 || MaximumArea < MinimumArea || MaximumArea > 100000)
                return "面积范围须为 1–100000 px² 内的有限值，上限不能小于下限。";
            if (!new[] { "area-desc", "area-asc", "row", "column" }.Contains(SortMode)) return "对象排序规则无效，不自动替换。";
            if (SelectionIndex < 0 || SelectionIndex >= 1000) return "选取编号从0开始，须为0–999；超出本次对象数时明确缺选，不钳到最后一个。";
            return string.Empty;
        }
    }

    public sealed class VmConnectedObjectRowS144
    {
        public int Index { get; set; }
        public int SourceIndex { get; set; }
        public double Area { get; set; }
        public double Row { get; set; }
        public double Column { get; set; }
        public bool Selected { get; set; }
    }
}
