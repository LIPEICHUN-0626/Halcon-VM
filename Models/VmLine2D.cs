using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmLine2D
    {
        public const string ImagePixelContext = "ImagePixel";

        public VmLine2D()
        {
            CoordinateContext = ImagePixelContext;
            CoordinateContextId = "Global.Image";
            IsValid = true;
        }

        [DataMember(Order = 1)] public double StartRow { get; set; }
        [DataMember(Order = 2)] public double StartColumn { get; set; }
        [DataMember(Order = 3)] public double EndRow { get; set; }
        [DataMember(Order = 4)] public double EndColumn { get; set; }
        [DataMember(Order = 5)] public string CoordinateContext { get; set; }
        [DataMember(Order = 6)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 7)] public bool IsValid { get; set; }

        public double Length
        {
            get
            {
                double row = EndRow - StartRow;
                double column = EndColumn - StartColumn;
                return Math.Sqrt(row * row + column * column);
            }
        }

        public double DirectionAngleDegrees
        {
            get { return Math.Atan2(EndRow - StartRow, EndColumn - StartColumn) * 180.0 / Math.PI; }
        }

        public static VmLine2D CreateImagePixel(double startRow, double startColumn, double endRow, double endColumn, string contextId)
        {
            return new VmLine2D
            {
                StartRow = startRow,
                StartColumn = startColumn,
                EndRow = endRow,
                EndColumn = endColumn,
                CoordinateContext = ImagePixelContext,
                CoordinateContextId = string.IsNullOrWhiteSpace(contextId) ? "Global.Image" : contextId.Trim(),
                IsValid = true
            };
        }

        public VmLine2D Clone()
        {
            return new VmLine2D
            {
                StartRow = StartRow,
                StartColumn = StartColumn,
                EndRow = EndRow,
                EndColumn = EndColumn,
                CoordinateContext = CoordinateContext,
                CoordinateContextId = CoordinateContextId,
                IsValid = IsValid
            };
        }

        public string GetValidationError()
        {
            if (!IsValid) return "Line2D 已标记为无效。";
            if (!IsFinite(StartRow) || !IsFinite(StartColumn) || !IsFinite(EndRow) || !IsFinite(EndColumn))
                return "Line2D 的起点和终点必须都是有限数值。";
            if (!string.Equals(CoordinateContext, ImagePixelContext, StringComparison.Ordinal))
                return "Line2D 当前只支持 ImagePixel 图像像素坐标。";
            if (string.IsNullOrWhiteSpace(CoordinateContextId)) return "Line2D 缺少坐标上下文标识。";
            if (!IsFinite(Length) || Length <= 1e-9) return "Line2D 起点与终点重合，不能表示有效直线。";
            return string.Empty;
        }

        public string ToDisplayString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "({0:0.###},{1:0.###})→({2:0.###},{3:0.###}) · L={4:0.###}px · {5}",
                StartRow, StartColumn, EndRow, EndColumn, Length,
                string.IsNullOrWhiteSpace(CoordinateContextId) ? CoordinateContext : CoordinateContextId);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "StartRow={0:0.############};StartColumn={1:0.############};EndRow={2:0.############};EndColumn={3:0.############};CoordinateContext={4};CoordinateContextId={5};IsValid={6}",
                StartRow, StartColumn, EndRow, EndColumn, CoordinateContext ?? string.Empty, CoordinateContextId ?? string.Empty, IsValid ? "true" : "false");
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
