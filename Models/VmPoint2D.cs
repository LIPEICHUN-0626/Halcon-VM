using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmPoint2D
    {
        public const string ImagePixelContext = "ImagePixel";

        public VmPoint2D()
        {
            CoordinateContext = ImagePixelContext;
            CoordinateContextId = "Global.Image";
            IsValid = true;
        }

        [DataMember(Order = 1)] public double Row { get; set; }
        [DataMember(Order = 2)] public double Column { get; set; }
        [DataMember(Order = 3)] public string CoordinateContext { get; set; }
        [DataMember(Order = 4)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 5)] public bool IsValid { get; set; }

        public static VmPoint2D CreateImagePixel(double row, double column, string contextId)
        {
            return new VmPoint2D
            {
                Row = row,
                Column = column,
                CoordinateContext = ImagePixelContext,
                CoordinateContextId = string.IsNullOrWhiteSpace(contextId) ? "Global.Image" : contextId.Trim(),
                IsValid = true
            };
        }

        public VmPoint2D Clone()
        {
            return new VmPoint2D
            {
                Row = Row,
                Column = Column,
                CoordinateContext = CoordinateContext,
                CoordinateContextId = CoordinateContextId,
                IsValid = IsValid
            };
        }

        public string GetValidationError()
        {
            if (!IsValid) return "Point2D 已标记为无效。";
            if (!IsFinite(Row) || !IsFinite(Column)) return "Point2D 的 Row 和 Column 必须都是有限数值。";
            if (!string.Equals(CoordinateContext, ImagePixelContext, StringComparison.Ordinal)) return "Point2D 当前只支持 ImagePixel 图像像素坐标。";
            if (string.IsNullOrWhiteSpace(CoordinateContextId)) return "Point2D 缺少坐标上下文标识。";
            return string.Empty;
        }

        public string ToDisplayString()
        {
            return string.Format(CultureInfo.InvariantCulture, "R={0:0.###}, C={1:0.###} · {2}", Row, Column,
                string.IsNullOrWhiteSpace(CoordinateContextId) ? CoordinateContext : CoordinateContextId);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Row={0:0.############};Column={1:0.############};CoordinateContext={2};CoordinateContextId={3};IsValid={4}",
                Row, Column, CoordinateContext ?? string.Empty, CoordinateContextId ?? string.Empty, IsValid ? "true" : "false");
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    [DataContract]
    public sealed class VmPoint2DArray
    {
        public VmPoint2DArray()
        {
            Points = new List<VmPoint2D>();
            CoordinateContext = VmPoint2D.ImagePixelContext;
            CoordinateContextId = "Global.Image";
            IsValid = true;
        }

        [DataMember(Order = 1)] public List<VmPoint2D> Points { get; set; }
        [DataMember(Order = 2)] public string CoordinateContext { get; set; }
        [DataMember(Order = 3)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 4)] public bool IsValid { get; set; }

        public int Count { get { return Points == null ? 0 : Points.Count; } }

        public static VmPoint2DArray CreateImagePixel(IEnumerable<double> rows, IEnumerable<double> columns, string contextId)
        {
            double[] rowValues = (rows ?? Enumerable.Empty<double>()).ToArray();
            double[] columnValues = (columns ?? Enumerable.Empty<double>()).ToArray();
            if (rowValues.Length != columnValues.Length) throw new ArgumentException("Point2D[] 的 Row/Column 数量必须一致。");
            string id = string.IsNullOrWhiteSpace(contextId) ? "Global.Image" : contextId.Trim();
            VmPoint2DArray result = new VmPoint2DArray { CoordinateContextId = id };
            for (int index = 0; index < rowValues.Length; index++) result.Points.Add(VmPoint2D.CreateImagePixel(rowValues[index], columnValues[index], id));
            return result;
        }

        public VmPoint2DArray Clone()
        {
            return new VmPoint2DArray
            {
                CoordinateContext = CoordinateContext,
                CoordinateContextId = CoordinateContextId,
                IsValid = IsValid,
                Points = (Points ?? new List<VmPoint2D>()).Select(point => point == null ? null : point.Clone()).ToList()
            };
        }

        public string GetValidationError(bool allowEmpty)
        {
            if (!IsValid) return "Point2D[] 已标记为无效。";
            if (!string.Equals(CoordinateContext, VmPoint2D.ImagePixelContext, StringComparison.Ordinal)) return "Point2D[] 当前只支持 ImagePixel 图像像素坐标。";
            if (string.IsNullOrWhiteSpace(CoordinateContextId)) return "Point2D[] 缺少坐标上下文标识。";
            if (Points == null || Points.Count == 0) return allowEmpty ? string.Empty : "Point2D[] 为空。";
            for (int index = 0; index < Points.Count; index++)
            {
                VmPoint2D point = Points[index];
                if (point == null) return "Point2D[] 第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 项为空。";
                string error = point.GetValidationError();
                if (!string.IsNullOrWhiteSpace(error)) return "Point2D[] 第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 项无效：" + error;
                if (!string.Equals(point.CoordinateContext, CoordinateContext, StringComparison.Ordinal) ||
                    !string.Equals(point.CoordinateContextId, CoordinateContextId, StringComparison.Ordinal))
                    return "Point2D[] 第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 项坐标上下文不一致。";
            }
            return string.Empty;
        }

        public string ToDisplayString()
        {
            int valid = (Points ?? new List<VmPoint2D>()).Count(point => point != null && string.IsNullOrWhiteSpace(point.GetValidationError()) &&
                point.CoordinateContext == CoordinateContext && point.CoordinateContextId == CoordinateContextId);
            return string.Format(CultureInfo.InvariantCulture, "点集 {0} · 有效 {1} / 异常 {2} · {3}{4}", Count, valid, Count - valid,
                CoordinateContextId, !IsValid || Points == null ? " · 容器无效" : string.Empty);
        }

        public override string ToString()
        {
            return "CoordinateContext=" + (CoordinateContext ?? string.Empty) + ";CoordinateContextId=" + (CoordinateContextId ?? string.Empty) +
                   ";IsValid=" + (IsValid ? "true" : "false") + ";Points=[" + string.Join("|", (Points ?? new List<VmPoint2D>()).Select(point => point == null ? "null" : point.ToString())) + "]";
        }
    }
}
