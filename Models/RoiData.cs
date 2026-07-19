using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    public enum RoiShapeType
    {
        Rectangle,
        Circle,
        Polygon,
        RotatedRectangle
    }

    public sealed class RoiData : IDisposable
    {
        private RoiData(RoiShapeType shapeType, double row1, double column1, double row2, double column2, double row, double column, double radius, double[] polygonRows, double[] polygonColumns, double phi, double length1, double length2)
        {
            ShapeType = shapeType;
            Row1 = row1;
            Column1 = column1;
            Row2 = row2;
            Column2 = column2;
            Row = row;
            Column = column;
            Radius = radius;
            PolygonRows = polygonRows;
            PolygonColumns = polygonColumns;
            Phi = phi;
            Length1 = length1;
            Length2 = length2;
            Region = CreateRegion();
        }

        public RoiShapeType ShapeType { get; private set; }

        public double Row1 { get; private set; }

        public double Column1 { get; private set; }

        public double Row2 { get; private set; }

        public double Column2 { get; private set; }

        public double Row { get; private set; }

        public double Column { get; private set; }

        public double Radius { get; private set; }

        public double[] PolygonRows { get; private set; }

        public double[] PolygonColumns { get; private set; }

        public double Phi { get; private set; }

        public double Length1 { get; private set; }

        public double Length2 { get; private set; }

        public HRegion Region { get; private set; }

        public string ShapeDisplayText
        {
            get
            {
                if (ShapeType == RoiShapeType.RotatedRectangle) return "旋转矩形";
                if (ShapeType == RoiShapeType.Polygon) return "多边形";
                if (ShapeType == RoiShapeType.Circle) return "圆形";
                return "矩形";
            }
        }

        public string DisplayText
        {
            get
            {
                if (ShapeType == RoiShapeType.Polygon)
                {
                    return string.Format("多边形：顶点={0}", PolygonRows == null ? 0 : PolygonRows.Length);
                }

                if (ShapeType == RoiShapeType.RotatedRectangle)
                {
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "旋转矩形：中心 R={0:F1}, C={1:F1}, 宽={2:F1}, 高={3:F1}, 角度={4:F1}°",
                        Row,
                        Column,
                        Length1 * 2.0,
                        Length2 * 2.0,
                        Phi * 180.0 / Math.PI);
                }

                if (ShapeType == RoiShapeType.Circle)
                {
                    return string.Format("圆形：R={0:F1}, C={1:F1}, 半径={2:F1}", Row, Column, Radius);
                }

                return string.Format("矩形：R1={0:F1}, C1={1:F1}, R2={2:F1}, C2={3:F1}", Row1, Column1, Row2, Column2);
            }
        }

        public static RoiData CreateRectangle(double row1, double column1, double row2, double column2)
        {
            double top = Math.Min(row1, row2);
            double left = Math.Min(column1, column2);
            double bottom = Math.Max(row1, row2);
            double right = Math.Max(column1, column2);
            return new RoiData(RoiShapeType.Rectangle, top, left, bottom, right, 0, 0, 0, null, null, 0, 0, 0);
        }

        public static RoiData CreateCircle(double row, double column, double radius)
        {
            return new RoiData(RoiShapeType.Circle, 0, 0, 0, 0, row, column, Math.Max(1, radius), null, null, 0, 0, 0);
        }

        public static RoiData CreateRotatedRectangle(double row, double column, double phi, double length1, double length2)
        {
            return new RoiData(
                RoiShapeType.RotatedRectangle,
                0,
                0,
                0,
                0,
                row,
                column,
                0,
                null,
                null,
                NormalizeAngle(phi),
                Math.Max(1, length1),
                Math.Max(1, length2));
        }

        public static RoiData CreatePolygon(IEnumerable<PointF> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            PointF[] array = points.ToArray();
            if (array.Length < 3)
            {
                throw new ArgumentException("Polygon ROI needs at least 3 points.", "points");
            }

            double[] rows = array.Select(item => (double)item.Y).ToArray();
            double[] columns = array.Select(item => (double)item.X).ToArray();
            return CreatePolygon(rows, columns);
        }

        public static RoiData CreatePolygon(double[] rows, double[] columns)
        {
            if (rows == null || columns == null || rows.Length != columns.Length || rows.Length < 3)
            {
                throw new ArgumentException("Polygon ROI needs matching row/column arrays with at least 3 points.");
            }

            double row1 = rows.Min();
            double row2 = rows.Max();
            double column1 = columns.Min();
            double column2 = columns.Max();
            return new RoiData(RoiShapeType.Polygon, row1, column1, row2, column2, 0, 0, 0, (double[])rows.Clone(), (double[])columns.Clone(), 0, 0, 0);
        }

        public RoiData Clone()
        {
            if (ShapeType == RoiShapeType.Polygon)
            {
                return CreatePolygon(PolygonRows, PolygonColumns);
            }

            if (ShapeType == RoiShapeType.Circle)
            {
                return CreateCircle(Row, Column, Radius);
            }

            if (ShapeType == RoiShapeType.RotatedRectangle)
            {
                return CreateRotatedRectangle(Row, Column, Phi, Length1, Length2);
            }

            return CreateRectangle(Row1, Column1, Row2, Column2);
        }

        public PointF[] GetRotatedRectangleCorners()
        {
            if (ShapeType != RoiShapeType.RotatedRectangle)
            {
                return new PointF[0];
            }

            double cos = Math.Cos(Phi);
            double sin = Math.Sin(Phi);
            double axis1Column = cos * Length1;
            double axis1Row = -sin * Length1;
            double axis2Column = sin * Length2;
            double axis2Row = cos * Length2;
            return new[]
            {
                new PointF((float)(Column - axis1Column - axis2Column), (float)(Row - axis1Row - axis2Row)),
                new PointF((float)(Column + axis1Column - axis2Column), (float)(Row + axis1Row - axis2Row)),
                new PointF((float)(Column + axis1Column + axis2Column), (float)(Row + axis1Row + axis2Row)),
                new PointF((float)(Column - axis1Column + axis2Column), (float)(Row - axis1Row + axis2Row))
            };
        }

        public void Dispose()
        {
            if (Region != null)
            {
                Region.Dispose();
                Region = null;
            }
        }

        private HRegion CreateRegion()
        {
            if (ShapeType == RoiShapeType.Polygon)
            {
                HObject region;
                HOperatorSet.GenRegionPolygon(out region, new HTuple(PolygonRows), new HTuple(PolygonColumns));
                return new HRegion(region);
            }

            if (ShapeType == RoiShapeType.Circle)
            {
                return new HRegion(Row, Column, Radius);
            }

            if (ShapeType == RoiShapeType.RotatedRectangle)
            {
                HObject region;
                HOperatorSet.GenRectangle2(out region, Row, Column, Phi, Length1, Length2);
                return new HRegion(region);
            }

            return new HRegion(Row1, Column1, Row2, Column2);
        }

        private static double NormalizeAngle(double value)
        {
            while (value > Math.PI) value -= Math.PI * 2.0;
            while (value <= -Math.PI) value += Math.PI * 2.0;
            return value;
        }
    }
}
