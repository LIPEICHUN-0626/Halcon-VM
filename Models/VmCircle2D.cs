using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmCircle2D
    {
        public const string ImagePixelContext = "ImagePixel";

        public VmCircle2D()
        {
            CoordinateContext = ImagePixelContext;
            CoordinateContextId = "Global.Image";
            IsValid = true;
        }

        [DataMember(Order = 1)] public double CenterRow { get; set; }
        [DataMember(Order = 2)] public double CenterColumn { get; set; }
        [DataMember(Order = 3)] public double Radius { get; set; }
        [DataMember(Order = 4)] public string CoordinateContext { get; set; }
        [DataMember(Order = 5)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 6)] public bool IsValid { get; set; }

        public static VmCircle2D CreateImagePixel(double centerRow, double centerColumn, double radius, string contextId)
        {
            return new VmCircle2D
            {
                CenterRow = centerRow,
                CenterColumn = centerColumn,
                Radius = radius,
                CoordinateContext = ImagePixelContext,
                CoordinateContextId = string.IsNullOrWhiteSpace(contextId) ? "Global.Image" : contextId.Trim(),
                IsValid = true
            };
        }

        public VmCircle2D Clone()
        {
            return new VmCircle2D
            {
                CenterRow = CenterRow,
                CenterColumn = CenterColumn,
                Radius = Radius,
                CoordinateContext = CoordinateContext,
                CoordinateContextId = CoordinateContextId,
                IsValid = IsValid
            };
        }

        public string GetValidationError()
        {
            if (!IsValid) return "Circle2D 已标记为无效。";
            if (!IsFinite(CenterRow) || !IsFinite(CenterColumn) || !IsFinite(Radius))
                return "Circle2D 的圆心与半径必须都是有限数值。";
            if (Radius <= 1e-9) return "Circle2D 半径必须大于 0。";
            if (!string.Equals(CoordinateContext, ImagePixelContext, StringComparison.Ordinal))
                return "Circle2D 当前只支持 ImagePixel 图像像素坐标。";
            if (string.IsNullOrWhiteSpace(CoordinateContextId)) return "Circle2D 缺少坐标上下文标识。";
            return string.Empty;
        }

        public string ToDisplayString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Center=({0:0.###},{1:0.###}) · R={2:0.###}px · {3}",
                CenterRow, CenterColumn, Radius,
                string.IsNullOrWhiteSpace(CoordinateContextId) ? CoordinateContext : CoordinateContextId);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "CenterRow={0:0.############};CenterColumn={1:0.############};Radius={2:0.############};CoordinateContext={3};CoordinateContextId={4};IsValid={5}",
                CenterRow, CenterColumn, Radius, CoordinateContext ?? string.Empty, CoordinateContextId ?? string.Empty, IsValid ? "true" : "false");
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
