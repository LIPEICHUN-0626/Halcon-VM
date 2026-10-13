using System;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmImageEdgeLineParametersS143
    {
        [DataMember(Order = 1)] public double StartRow { get; set; } = 100;
        [DataMember(Order = 2)] public double StartColumn { get; set; } = 50;
        [DataMember(Order = 3)] public double EndRow { get; set; } = 100;
        [DataMember(Order = 4)] public double EndColumn { get; set; } = 350;
        [DataMember(Order = 5)] public int SampleCount { get; set; } = 24;
        [DataMember(Order = 6)] public double SearchHalfWidth { get; set; } = 30;
        [DataMember(Order = 7)] public double AveragingHalfWidth { get; set; } = 3;
        [DataMember(Order = 8)] public double Sigma { get; set; } = 1;
        [DataMember(Order = 9)] public double Threshold { get; set; } = 20;
        [DataMember(Order = 10)] public string Transition { get; set; } = "all";
        [DataMember(Order = 11)] public string Selection { get; set; } = "first";
        [DataMember(Order = 12)] public int MinimumValidCount { get; set; } = 6;
        [DataMember(Order = 13)] public double MinimumCoverage { get; set; } = 0.5;
        [DataMember(Order = 14)] public double MaximumResidual { get; set; } = 2;
        [DataMember(Order = 15)] public string FitAlgorithm { get; set; } = "tukey";
        [DataMember(Order = 16)] public int Iterations { get; set; } = 5;
        public VmImageEdgeLineParametersS143 Clone() { return (VmImageEdgeLineParametersS143)MemberwiseClone(); }
        public string Validate()
        {
            if (new[] { StartRow, StartColumn, EndRow, EndColumn }.Any(v => !Finite(v) || v < 0 || v > 16383)) return "找线起终点须为 0–16383 内有限图像坐标。";
            if (Math.Sqrt(Math.Pow(EndRow - StartRow, 2) + Math.Pow(EndColumn - StartColumn, 2)) < 3) return "找线基线跨度至少 3 px，不能用重合端点。";
            if (SampleCount < 3 || SampleCount > 200 || MinimumValidCount < 3 || MinimumValidCount > SampleCount) return "采样数须为 3–200，最少有效点须为 3–采样数。";
            if (!Finite(SearchHalfWidth) || SearchHalfWidth < 2 || SearchHalfWidth > 2048 || !Finite(AveragingHalfWidth) || AveragingHalfWidth < 1 || AveragingHalfWidth > 128) return "搜索半宽须为 2–2048 px，均值半宽须为 1–128 px。";
            if (!Finite(Sigma) || Sigma < 0.4 || Sigma > Math.Min(100, SearchHalfWidth / 2) || !Finite(Threshold) || Threshold <= 0 || Threshold > 1000000) return "Sigma 须为 0.4–搜索半宽/2（最多100），边缘阈值须为正有限原灰度幅值。";
            if (!new[] { "all", "positive", "negative" }.Contains(Transition) || !new[] { "first", "last", "strongest" }.Contains(Selection)) return "边缘极性或选择规则无效；不默默替换方向语义。";
            if (!Finite(MinimumCoverage) || MinimumCoverage < 0.1 || MinimumCoverage > 1 || !Finite(MaximumResidual) || MaximumResidual < 0 || MaximumResidual > 2048) return "有效跨度覆盖须为 0.1–1，残差门槛须为 0–2048 px。";
            if (!new[] { "tukey", "huber" }.Contains(FitAlgorithm) || Iterations < 1 || Iterations > 10) return "拟合仅支持 tukey/huber，迭代须为 1–10。";
            return string.Empty;
        }
        internal static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }
}
