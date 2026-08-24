using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmPose2D
    {
        public const string ImagePixelContext = "ImagePixel";

        public VmPose2D()
        {
            CoordinateContext = ImagePixelContext;
            CoordinateContextId = "Global.Image";
            IsValid = true;
        }

        [DataMember(Order = 1)] public double Row { get; set; }
        [DataMember(Order = 2)] public double Column { get; set; }
        [DataMember(Order = 3)] public double AngleDegrees { get; set; }
        [DataMember(Order = 4)] public string CoordinateContext { get; set; }
        [DataMember(Order = 5)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 6)] public bool IsValid { get; set; }

        public static VmPose2D CreateImagePixel(double row, double column, double angleDegrees, string contextId)
        {
            return new VmPose2D
            {
                Row = row,
                Column = column,
                AngleDegrees = angleDegrees,
                CoordinateContext = ImagePixelContext,
                CoordinateContextId = string.IsNullOrWhiteSpace(contextId) ? "Global.Image" : contextId.Trim(),
                IsValid = true
            };
        }

        public VmPose2D Clone()
        {
            return new VmPose2D
            {
                Row = Row,
                Column = Column,
                AngleDegrees = AngleDegrees,
                CoordinateContext = CoordinateContext,
                CoordinateContextId = CoordinateContextId,
                IsValid = IsValid
            };
        }

        public string GetValidationError()
        {
            if (!IsValid) return "Pose2D 已标记为无效。";
            if (!IsFinite(Row) || !IsFinite(Column) || !IsFinite(AngleDegrees)) return "Pose2D 的 Row、Column 和 Angle 必须都是有限数值。";
            if (string.IsNullOrWhiteSpace(CoordinateContext)) return "Pose2D 缺少坐标上下文。";
            if (string.IsNullOrWhiteSpace(CoordinateContextId)) return "Pose2D 缺少坐标上下文标识。";
            return string.Empty;
        }

        public string ToDisplayString()
        {
            return string.Format(CultureInfo.InvariantCulture, "R={0:0.###}, C={1:0.###}, A={2:0.###}° · {3}", Row, Column, AngleDegrees,
                string.IsNullOrWhiteSpace(CoordinateContextId) ? CoordinateContext : CoordinateContextId);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Row={0:0.############};Column={1:0.############};AngleDegrees={2:0.############};CoordinateContext={3};CoordinateContextId={4};IsValid={5}",
                Row, Column, AngleDegrees, CoordinateContext ?? string.Empty, CoordinateContextId ?? string.Empty, IsValid ? "true" : "false");
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class VmPortMonitorRow
    {
        public string MonitorKey { get; set; }
        public string OwnerToolId { get; set; }
        public string OwnerToolName { get; set; }
        public string Direction { get; set; }
        public string PortName { get; set; }
        public string DisplayName { get; set; }
        public string SourceToolId { get; set; }
        public string SourceToolName { get; set; }
        public string SourcePortName { get; set; }
        public string DataType { get; set; }
        public string ValueText { get; set; }
        public string ContextText { get; set; }
        public string CycleText { get; set; }
        public string StateKey { get; set; }
        public string StateText { get; set; }
        public string ConnectionKey { get; set; }
        public bool IsPinned { get; set; }
        public string PinGlyph { get { return IsPinned ? "★" : "☆"; } }
        public string EndpointText { get { return OwnerToolName + "." + PortName; } }
        public string SourceText { get { return string.IsNullOrWhiteSpace(SourceToolName) ? "未连接" : SourceToolName + "." + SourcePortName; } }
        public string DirectionText { get { return string.Equals(Direction, "IN", StringComparison.OrdinalIgnoreCase) ? "IN" : "OUT"; } }
    }
}
