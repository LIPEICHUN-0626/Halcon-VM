using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmEllipse2D
    {
        public const string ImagePixelContext = "ImagePixel";

        public VmEllipse2D()
        {
            CoordinateContext = ImagePixelContext;
            CoordinateContextId = "Global.Image";
            IsValid = true;
        }

        [DataMember(Order = 1)] public double CenterRow { get; set; }
        [DataMember(Order = 2)] public double CenterColumn { get; set; }
        [DataMember(Order = 3)] public double PhiRadians { get; set; }
        [DataMember(Order = 4)] public double Radius1 { get; set; }
        [DataMember(Order = 5)] public double Radius2 { get; set; }
        [DataMember(Order = 6)] public string CoordinateContext { get; set; }
        [DataMember(Order = 7)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 8)] public bool IsValid { get; set; }

        public double PhiDegrees { get { return PhiRadians * 180.0 / Math.PI; } }
        public double AxisRatio { get { return Radius2 <= 1e-12 ? double.PositiveInfinity : Radius1 / Radius2; } }

        public static VmEllipse2D CreateImagePixel(double centerRow, double centerColumn, double phiRadians, double radius1, double radius2, string contextId)
        {
            if (radius2 > radius1)
            {
                double swap = radius1;
                radius1 = radius2;
                radius2 = swap;
                phiRadians += Math.PI / 2.0;
            }
            return new VmEllipse2D
            {
                CenterRow = centerRow,
                CenterColumn = centerColumn,
                PhiRadians = NormalizeHalfTurn(phiRadians),
                Radius1 = radius1,
                Radius2 = radius2,
                CoordinateContext = ImagePixelContext,
                CoordinateContextId = string.IsNullOrWhiteSpace(contextId) ? "Global.Image" : contextId.Trim(),
                IsValid = true
            };
        }

        public VmEllipse2D Clone()
        {
            return new VmEllipse2D
            {
                CenterRow = CenterRow,
                CenterColumn = CenterColumn,
                PhiRadians = PhiRadians,
                Radius1 = Radius1,
                Radius2 = Radius2,
                CoordinateContext = CoordinateContext,
                CoordinateContextId = CoordinateContextId,
                IsValid = IsValid
            };
        }

        public string GetValidationError()
        {
            if (!IsValid) return "Ellipse2D 已标记为无效。";
            if (!IsFinite(CenterRow) || !IsFinite(CenterColumn) || !IsFinite(PhiRadians) || !IsFinite(Radius1) || !IsFinite(Radius2))
                return "Ellipse2D 的中心、方向和半轴必须都是有限数值。";
            if (Radius1 <= 1e-9 || Radius2 <= 1e-9) return "Ellipse2D 的长短半轴都必须大于 0。";
            if (Radius1 + 1e-9 < Radius2) return "Ellipse2D 约定 Radius1 为长半轴且不得小于 Radius2。";
            if (!string.Equals(CoordinateContext, ImagePixelContext, StringComparison.Ordinal))
                return "Ellipse2D 当前只支持 ImagePixel 图像像素坐标。";
            if (string.IsNullOrWhiteSpace(CoordinateContextId)) return "Ellipse2D 缺少坐标上下文标识。";
            return string.Empty;
        }

        public string ToDisplayString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Center=({0:0.###},{1:0.###}) · R1/R2={2:0.###}/{3:0.###}px · Phi={4:0.###}° · {5}",
                CenterRow, CenterColumn, Radius1, Radius2, PhiDegrees,
                string.IsNullOrWhiteSpace(CoordinateContextId) ? CoordinateContext : CoordinateContextId);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "CenterRow={0:0.############};CenterColumn={1:0.############};PhiRadians={2:0.############};Radius1={3:0.############};Radius2={4:0.############};CoordinateContext={5};CoordinateContextId={6};IsValid={7}",
                CenterRow, CenterColumn, PhiRadians, Radius1, Radius2, CoordinateContext ?? string.Empty, CoordinateContextId ?? string.Empty, IsValid ? "true" : "false");
        }

        private static double NormalizeHalfTurn(double angle)
        {
            if (!IsFinite(angle)) return angle;
            while (angle < -Math.PI / 2.0) angle += Math.PI;
            while (angle >= Math.PI / 2.0) angle -= Math.PI;
            return angle;
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
