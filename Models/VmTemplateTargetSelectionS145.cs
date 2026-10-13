using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmTemplateTargetParametersS145
    {
        [DataMember(Order = 1)] public string SortMode { get; set; } = "score";
        [DataMember(Order = 2)] public int SelectionIndex { get; set; }
        [OnDeserializing] private void Defaults(StreamingContext context) { SortMode = "score"; }
        public VmTemplateTargetParametersS145 Clone() { return (VmTemplateTargetParametersS145)MemberwiseClone(); }
        public string Validate()
        {
            if (SortMode != "score" && SortMode != "row" && SortMode != "column") return "模板目标排序须明确为分数、行或列，不自动替换未知规则。";
            if (SelectionIndex < 0 || SelectionIndex >= VmTemplateTargetContractS145.MaximumCandidates)
                return "正式选取编号从 0 开始，须为 0–999；超出本次候选数时明确缺选，不钳到末项。";
            return string.Empty;
        }
    }

    // Pixel-center values supplied at the real matching output boundary. HALCON
    // iconic coordinates must be converted by the existing algorithm contract,
    // not by this organizer. SourceIndex is the zero-based original HALCON tuple
    // position, explicitly converted from legacy one-based NCC display IDs.
    public sealed class VmTemplateTargetS145
    {
        public double PixelRow { get; set; }
        public double PixelColumn { get; set; }
        public double AngleDegrees { get; set; }
        public double Score { get; set; }
        // Shape callers must provide the actual match scale. NCC callers explicitly
        // provide 1; a missing scale is not silently defaulted for a Shape match.
        public double Scale { get; set; }
        public int SourceIndex { get; set; }
        public VmTemplateTargetS145 Clone() { return (VmTemplateTargetS145)MemberwiseClone(); }
        public string Validate()
        {
            if (!Finite(PixelRow) || !Finite(PixelColumn) || !Finite(AngleDegrees) || !Finite(Score) || Score < 0 || Score > 1)
                return "模板候选中心/角度须有限，真实分数须在 0–1；不能补造或钳制数值。";
            if (!Finite(Scale) || Scale < 0.000001 || Scale > 1000000)
                return "模板候选真实尺度须为 1e-6–1e6 的有限正值；NCC 显式提供 1，Shape 不回填单位尺度。";
            if (SourceIndex < 0 || SourceIndex >= VmTemplateTargetContractS145.MaximumCandidates)
                return "模板候选原编号须为本次原始顺序的 0–999 编号。";
            return string.Empty;
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class VmTemplateTargetResultS145
    {
        public IReadOnlyList<VmTemplateTargetS145> Targets { get; internal set; }
        public VmPoint2DArray Centers { get; internal set; }
        public VmNumberArrayS105 Scores { get; internal set; }
        public VmNumberArrayS105 Angles { get; internal set; }
        public VmNumberArrayS105 Scales { get; internal set; }
        public VmNumberArrayS105 SourceIndices { get; internal set; }
        public int TargetCount { get { return Targets == null ? 0 : Targets.Count; } }
        public bool HasSelection { get { return SelectedIndex.HasValue; } }
        public int? SelectedIndex { get; internal set; }
        public VmPoint2D SelectedPoint { get; internal set; }
        public VmPose2D SelectedPose { get; internal set; }
        public double? SelectedScore { get; internal set; }
        public double? SelectedScale { get; internal set; }
    }

    public static class VmTemplateTargetContractS145
    {
        public const int MaximumCandidates = 1000;
        public static VmTemplateTargetResultS145 Build(IEnumerable<VmTemplateTargetS145> source, VmTemplateTargetParametersS145 parameters, string coordinateContextId)
        {
            if (source == null) throw new ArgumentNullException("source");
            var options = parameters == null ? new VmTemplateTargetParametersS145() : parameters.Clone();
            string error = options.Validate(); if (error.Length > 0) throw new InvalidOperationException(error);
            if (string.IsNullOrWhiteSpace(coordinateContextId) || coordinateContextId.Length > 1024 || coordinateContextId.Any(char.IsControl))
                throw new InvalidOperationException("模板多目标结果缺少明确的唯一图像像素坐标身份，不回退 Global.Image。");
            // Bound enumeration before copying/sorting. Excess is a failure, not a
            // truncated result claimed complete. Never hold a matcher/model handle.
            var values = source.Take(MaximumCandidates + 1).Select(target => target == null ? null : target.Clone()).ToArray();
            if (values.Length > MaximumCandidates) throw new InvalidOperationException("本次模板候选超过 1000 项，拒绝截断冒完整结果。");
            var identities = new HashSet<int>();
            foreach (var value in values)
            {
                if (value == null) throw new InvalidOperationException("本次模板候选包含空值，不补造目标。");
                string invalid = value.Validate(); if (invalid.Length > 0) throw new InvalidOperationException(invalid);
                if (!identities.Add(value.SourceIndex)) throw new InvalidOperationException("本次模板候选原编号重复，不能用显示行号替换真实来源。");
            }
            // Exact ties: selected axis -> other axis -> score -> angle -> original
            // candidate ID. Score mode instead starts with descending actual score.
            // IDs belong to this match only; none of these orders is object tracking.
            IOrderedEnumerable<VmTemplateTargetS145> ordered;
            if (options.SortMode == "row") ordered = values.OrderBy(value => value.PixelRow).ThenBy(value => value.PixelColumn).ThenByDescending(value => value.Score);
            else if (options.SortMode == "column") ordered = values.OrderBy(value => value.PixelColumn).ThenBy(value => value.PixelRow).ThenByDescending(value => value.Score);
            else ordered = values.OrderByDescending(value => value.Score).ThenBy(value => value.PixelRow).ThenBy(value => value.PixelColumn);
            var targets = ordered.ThenBy(value => value.AngleDegrees).ThenBy(value => value.SourceIndex).ToArray();
            var centers = new VmPoint2DArray { CoordinateContext = VmPoint2D.ImagePixelContext, CoordinateContextId = coordinateContextId, IsValid = true };
            foreach (var target in targets)
                centers.Points.Add(new VmPoint2D { Row = target.PixelRow, Column = target.PixelColumn, CoordinateContext = VmPoint2D.ImagePixelContext, CoordinateContextId = coordinateContextId, IsValid = true });
            var result = new VmTemplateTargetResultS145
            {
                Targets = Array.AsReadOnly(targets), Centers = centers,
                Scores = new VmNumberArrayS105(targets.Select(target => target.Score)),
                Angles = new VmNumberArrayS105(targets.Select(target => target.AngleDegrees)),
                Scales = new VmNumberArrayS105(targets.Select(target => target.Scale)),
                SourceIndices = new VmNumberArrayS105(targets.Select(target => (double)target.SourceIndex))
            };
            if (options.SelectionIndex < targets.Length)
            {
                var target = targets[options.SelectionIndex];
                result.SelectedIndex = options.SelectionIndex;
                result.SelectedPoint = centers.Points[options.SelectionIndex].Clone();
                result.SelectedPose = new VmPose2D
                {
                    Row = target.PixelRow, Column = target.PixelColumn, AngleDegrees = target.AngleDegrees,
                    CoordinateContext = VmPose2D.ImagePixelContext, CoordinateContextId = coordinateContextId, IsValid = true
                };
                result.SelectedScore = target.Score;
                result.SelectedScale = target.Scale;
                // Pose2D remains the existing rigid position/angle contract. Actual
                // scale is independent; this pose does not promise a scale transform.
            }
            // Empty/out-of-range selection has true empty arrays or null Selected*.
            // No change is made to the matcher's old best Point/Pose/Score outputs.
            return result;
        }
    }
}
