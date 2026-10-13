using System;
using System.Collections.Generic;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo.Models
{
    public static class VmCameraGeometryS128
    {
        public static void MapPoints(double[] rows, double[] columns, HTuple cameraIn, HTuple cameraOut,
            int inputWidth, int inputHeight, int outputWidth, int outputHeight, out double[] mappedRows, out double[] mappedColumns)
        {
            ValidatePoints(rows, columns, inputWidth, inputHeight);
            HTuple rr = null, cc = null, backRows = null, backColumns = null;
            using (var sourceRows = new HTuple(rows))
            using (var sourceColumns = new HTuple(columns))
            try
            {
                HOperatorSet.ChangeRadialDistortionPoints(sourceRows, sourceColumns, cameraIn, cameraOut, out rr, out cc);
                mappedRows = rr.ToDArr(); mappedColumns = cc.ToDArr();
                if (mappedRows.Length != rows.Length || mappedColumns.Length != rows.Length)
                    throw new InvalidOperationException("畸变映射返回的点数量不一致。");
                ValidatePoints(mappedRows, mappedColumns, outputWidth, outputHeight);
                HOperatorSet.ChangeRadialDistortionPoints(rr, cc, cameraOut, cameraIn, out backRows, out backColumns);
                if (backRows.Length != rows.Length || backColumns.Length != rows.Length)
                    throw new InvalidOperationException("畸变映射无法逆向复核。");
                for (int i = 0; i < rows.Length; i++)
                {
                    double dr = backRows[i].D - rows[i], dc = backColumns[i].D - columns[i];
                    if (!Finite(dr) || !Finite(dc) || dr * dr + dc * dc > 0.0025)
                        throw new InvalidOperationException("畸变映射逆向误差超过 0.05 px；请检查内参和可逆范围。");
                }
            }
            finally
            {
                if (rr != null) rr.Dispose(); if (cc != null) cc.Dispose();
                if (backRows != null) backRows.Dispose(); if (backColumns != null) backColumns.Dispose();
            }
        }

        public static HObject MapContours(HObject input, HTuple cameraIn, HTuple cameraOut,
            int inputWidth, int inputHeight, int outputWidth, int outputHeight)
        {
            HObject mapped = null;
            HTuple count = null;
            try
            {
                HOperatorSet.CountObj(input, out count);
                if (count.I < 1 || count.I > 4096) throw new InvalidOperationException("迁移轮廓须为 1–4096 个对象。");
                // Check every original vertex before publishing the HALCON contour mapping.
                int total = 0;
                for (int index = 1; index <= count.I; index++)
                {
                    HObject item = null; HTuple rows = null, columns = null;
                    try
                    {
                        HOperatorSet.SelectObj(input, out item, index);
                        HOperatorSet.GetContourXld(item, out rows, out columns);
                        total = checked(total + rows.Length);
                        if (total > 262144) throw new InvalidOperationException("迁移轮廓总点数超过 262144，请先简化轮廓。");
                        double[] rr, cc;
                        MapPoints(rows.ToDArr(), columns.ToDArr(), cameraIn, cameraOut,
                            inputWidth, inputHeight, outputWidth, outputHeight, out rr, out cc);
                    }
                    finally { if (item != null) item.Dispose(); if (rows != null) rows.Dispose(); if (columns != null) columns.Dispose(); }
                }
                HOperatorSet.ChangeRadialDistortionContoursXld(input, out mapped, cameraIn, cameraOut);
                HObject result = mapped; mapped = null; return result;
            }
            finally { if (count != null) count.Dispose(); if (mapped != null) mapped.Dispose(); }
        }

        public static RoiData MapRoi(RoiData source, HTuple cameraIn, HTuple cameraOut,
            int inputWidth, int inputHeight, int outputWidth, int outputHeight)
        {
            if (source == null) throw new InvalidOperationException("未选择 ROI。");
            if (source.ShapeType == RoiShapeType.Line || source.ShapeType == RoiShapeType.Annulus)
                throw new InvalidOperationException("直线经畸变后可能弯曲，圆环需要内外边界；当前 ROI 迁移支持点、矩形、旋转矩形、圆、椭圆和多边形。");
            double[] rows, columns;
            if (source.ShapeType == RoiShapeType.Point)
            {
                MapPoints(new[] { source.Row }, new[] { source.Column }, cameraIn, cameraOut,
                    inputWidth, inputHeight, outputWidth, outputHeight, out rows, out columns);
                return RoiData.CreatePoint(rows[0], columns[0]);
            }
            var boundaryRows = new List<double>(); var boundaryColumns = new List<double>();
            if (source.ShapeType == RoiShapeType.Circle || source.ShapeType == RoiShapeType.Ellipse)
            {
                double a = source.ShapeType == RoiShapeType.Circle ? source.Radius : source.Length1;
                double b = source.ShapeType == RoiShapeType.Circle ? source.Radius : source.Length2;
                double phi = source.ShapeType == RoiShapeType.Circle ? 0 : source.Phi;
                if (!Finite(a) || !Finite(b) || !Finite(phi) || a < 1 || b < 1 || Math.Max(a, b) > 2048 / Math.PI)
                    throw new InvalidOperationException("ROI 半径/半轴无效，或边界需要超过 2048 个迁移采样点。");
                int n = Math.Max(16, (int)Math.Ceiling(Math.PI * Math.Max(a, b)));
                if (n > 2048) throw new InvalidOperationException("ROI 曲线需要超过 2048 个迁移采样点，请缩小 ROI。");
                for (int i = 0; i < n; i++)
                {
                    double angle = 2 * Math.PI * i / n;
                    boundaryRows.Add(source.Row - a * Math.Cos(angle) * Math.Sin(phi) + b * Math.Sin(angle) * Math.Cos(phi));
                    boundaryColumns.Add(source.Column + a * Math.Cos(angle) * Math.Cos(phi) + b * Math.Sin(angle) * Math.Sin(phi));
                }
            }
            else
            {
                double[] vertexRows, vertexColumns;
                if (source.ShapeType == RoiShapeType.Polygon)
                { vertexRows = source.PolygonRows; vertexColumns = source.PolygonColumns; }
                else if (source.ShapeType == RoiShapeType.RotatedRectangle)
                {
                    var corners = source.GetRotatedRectangleCorners();
                    vertexRows = corners.Select(p => (double)p.Y).ToArray(); vertexColumns = corners.Select(p => (double)p.X).ToArray();
                }
                else if (source.ShapeType == RoiShapeType.Rectangle)
                {
                    vertexRows = new[] { source.Row1, source.Row1, source.Row2, source.Row2 };
                    vertexColumns = new[] { source.Column1, source.Column2, source.Column2, source.Column1 };
                }
                else throw new InvalidOperationException("该 ROI 形状不支持畸变迁移。");
                if (vertexRows == null || vertexColumns == null || vertexRows.Length != vertexColumns.Length || vertexRows.Length > 2048)
                    throw new InvalidOperationException("ROI 顶点无效或超过 2048 个。");
                ValidatePoints(vertexRows, vertexColumns, inputWidth, inputHeight);
                string error = RoiGeometryEditor.ValidatePolygon(vertexRows, vertexColumns);
                if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
                for (int i = 0; i < vertexRows.Length; i++)
                {
                    int next = (i + 1) % vertexRows.Length;
                    double dr = vertexRows[next] - vertexRows[i], dc = vertexColumns[next] - vertexColumns[i];
                    int n = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(dr * dr + dc * dc) / 2));
                    if (boundaryRows.Count + n > 2048) throw new InvalidOperationException("ROI 边界超过 2048 个迁移采样点，请缩小或简化 ROI。");
                    for (int j = 0; j < n; j++)
                    { boundaryRows.Add(vertexRows[i] + dr * j / n); boundaryColumns.Add(vertexColumns[i] + dc * j / n); }
                }
            }
            MapPoints(boundaryRows.ToArray(), boundaryColumns.ToArray(), cameraIn, cameraOut,
                inputWidth, inputHeight, outputWidth, outputHeight, out rows, out columns);
            // Keep editor-compatible spacing after contraction (less than 1 px trimming).
            var polygonRows = new List<double>(); var polygonColumns = new List<double>();
            for (int i = 0; i < rows.Length; i++)
                if (polygonRows.Count == 0 || DistanceSquared(rows[i], columns[i], polygonRows.Last(), polygonColumns.Last()) >= 1)
                { polygonRows.Add(rows[i]); polygonColumns.Add(columns[i]); }
            while (polygonRows.Count > 3 && DistanceSquared(polygonRows.Last(), polygonColumns.Last(), polygonRows[0], polygonColumns[0]) < 1)
            { polygonRows.RemoveAt(polygonRows.Count - 1); polygonColumns.RemoveAt(polygonColumns.Count - 1); }
            string polygonError = RoiGeometryEditor.ValidatePolygon(polygonRows.ToArray(), polygonColumns.ToArray());
            if (!string.IsNullOrWhiteSpace(polygonError)) throw new InvalidOperationException("映射 ROI 无效：" + polygonError);
            return RoiData.CreatePolygon(polygonRows.ToArray(), polygonColumns.ToArray());
        }

        private static void ValidatePoints(double[] rows, double[] columns, int width, int height)
        {
            if (rows == null || columns == null || rows.Length != columns.Length || rows.Length < 1 || rows.Length > 262144)
                throw new InvalidOperationException("畸变映射点集为空、数量不一致或超过安全上限。");
            for (int i = 0; i < rows.Length; i++)
                if (!Finite(rows[i]) || !Finite(columns[i]) || rows[i] < 0 || rows[i] > height - 1 || columns[i] < 0 || columns[i] > width - 1)
                    throw new InvalidOperationException("畸变映射点越出原图或校正图范围；不会自动裁剪坐标。");
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static double DistanceSquared(double row, double column, double otherRow, double otherColumn)
        { return (row - otherRow) * (row - otherRow) + (column - otherColumn) * (column - otherColumn); }
    }
}
