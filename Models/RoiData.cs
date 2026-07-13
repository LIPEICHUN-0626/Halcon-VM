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
        Polygon
    }

    public sealed class RoiData : IDisposable
    {
        private RoiData(RoiShapeType shapeType, double row1, double column1, double row2, double column2, double row, double column, double radius, double[] polygonRows, double[] polygonColumns)
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

        public HRegion Region { get; private set; }

        public string DisplayText
        {
            get
            {
                if (ShapeType == RoiShapeType.Polygon)
                {
                    return string.Format("Polygon: Points={0}", PolygonRows == null ? 0 : PolygonRows.Length);
                }

                if (ShapeType == RoiShapeType.Circle)
                {
                    return string.Format("Circle: Row={0:F1}, Column={1:F1}, Radius={2:F1}", Row, Column, Radius);
                }

                return string.Format("Rectangle: Row1={0:F1}, Column1={1:F1}, Row2={2:F1}, Column2={3:F1}", Row1, Column1, Row2, Column2);
            }
        }

        public static RoiData CreateRectangle(double row1, double column1, double row2, double column2)
        {
            double top = Math.Min(row1, row2);
            double left = Math.Min(column1, column2);
            double bottom = Math.Max(row1, row2);
            double right = Math.Max(column1, column2);
            return new RoiData(RoiShapeType.Rectangle, top, left, bottom, right, 0, 0, 0, null, null);
        }

        public static RoiData CreateCircle(double row, double column, double radius)
        {
            return new RoiData(RoiShapeType.Circle, 0, 0, 0, 0, row, column, Math.Max(1, radius), null, null);
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
            return new RoiData(RoiShapeType.Polygon, row1, column1, row2, column2, 0, 0, 0, (double[])rows.Clone(), (double[])columns.Clone());
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

            return CreateRectangle(Row1, Column1, Row2, Column2);
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

            return new HRegion(Row1, Column1, Row2, Column2);
        }
    }
}
