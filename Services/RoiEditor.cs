using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public enum VisionTool
    {
        Select,
        Pan,
        RectangleRoi,
        CircleRoi,
        EllipseRoi,
        AnnulusRoi,
        PolygonRoi,
        RotatedRectangleRoi,
        LineRoi,
        PointRoi,
        TemplateRectangleRoi,
        TemplateCircleRoi,
        MaskRectangleAdd,
        MaskCircleAdd,
        MaskBrushAdd,
        MaskEraser
    }

    public enum RoiEditHandle
    {
        None,
        Move,
        TopLeft,
        Top,
        TopRight,
        Left,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
        Radius,
        InnerRadius,
        OuterRadius,
        Rotate,
        Length1Start,
        Length1End,
        Length2Start,
        Length2End,
        PolygonVertex,
        LineStart,
        LineEnd,
        Point
    }

    public struct RoiEditHit
    {
        public RoiEditHandle Handle { get; set; }

        public int VertexIndex { get; set; }

        public bool IsHit
        {
            get { return Handle != RoiEditHandle.None; }
        }
    }

    public static class RoiGeometryEditor
    {
        public static RoiEditHandle HitTest(RoiData roi, PointF point, double tolerance)
        {
            return HitTestDetailed(roi, point, tolerance).Handle;
        }

        public static RoiEditHit HitTestDetailed(RoiData roi, PointF point, double tolerance)
        {
            if (roi == null || tolerance <= 0)
            {
                return NoHit();
            }

            if (roi.ShapeType == RoiShapeType.Point)
            {
                return Distance(point, new PointF((float)roi.Column, (float)roi.Row)) <= tolerance
                    ? Hit(RoiEditHandle.Point)
                    : NoHit();
            }

            if (roi.ShapeType == RoiShapeType.Line)
            {
                PointF start = new PointF((float)roi.Column1, (float)roi.Row1);
                PointF end = new PointF((float)roi.Column2, (float)roi.Row2);
                if (Distance(point, start) <= tolerance) return Hit(RoiEditHandle.LineStart);
                if (Distance(point, end) <= tolerance) return Hit(RoiEditHandle.LineEnd);
                return DistanceToSegment(point, start, end) <= tolerance ? Hit(RoiEditHandle.Move) : NoHit();
            }

            if (roi.ShapeType == RoiShapeType.Circle)
            {
                if (Distance(point, new PointF((float)(roi.Column + roi.Radius), (float)roi.Row)) <= tolerance)
                {
                    return Hit(RoiEditHandle.Radius);
                }

                double dx = point.X - roi.Column;
                double dy = point.Y - roi.Row;
                return dx * dx + dy * dy <= (roi.Radius + tolerance) * (roi.Radius + tolerance)
                    ? Hit(RoiEditHandle.Move)
                    : NoHit();
            }

            if (roi.ShapeType == RoiShapeType.Annulus)
            {
                PointF center = new PointF((float)roi.Column, (float)roi.Row);
                if (Distance(point, new PointF((float)(roi.Column + roi.InnerRadius), (float)roi.Row)) <= tolerance)
                {
                    return Hit(RoiEditHandle.InnerRadius);
                }
                if (Distance(point, new PointF((float)(roi.Column + roi.OuterRadius), (float)roi.Row)) <= tolerance)
                {
                    return Hit(RoiEditHandle.OuterRadius);
                }
                if (Distance(point, center) <= tolerance)
                {
                    return Hit(RoiEditHandle.Move);
                }

                double dx = point.X - roi.Column;
                double dy = point.Y - roi.Row;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                return distance >= roi.InnerRadius - tolerance && distance <= roi.OuterRadius + tolerance
                    ? Hit(RoiEditHandle.Move)
                    : NoHit();
            }

            if (roi.ShapeType == RoiShapeType.Polygon)
            {
                for (int index = 0; index < roi.PolygonRows.Length; index++)
                {
                    PointF vertex = new PointF((float)roi.PolygonColumns[index], (float)roi.PolygonRows[index]);
                    if (Distance(point, vertex) <= tolerance)
                    {
                        return Hit(RoiEditHandle.PolygonVertex, index);
                    }
                }

                if (PointInPolygon(roi.PolygonRows, roi.PolygonColumns, point) || IsNearPolygonEdge(roi.PolygonRows, roi.PolygonColumns, point, tolerance))
                {
                    return Hit(RoiEditHandle.Move);
                }

                return NoHit();
            }

            if (roi.ShapeType == RoiShapeType.RotatedRectangle || roi.ShapeType == RoiShapeType.Ellipse)
            {
                PointF center = new PointF((float)roi.Column, (float)roi.Row);
                PointF length1Start = LocalPoint(roi, -roi.Length1, 0);
                PointF length1End = LocalPoint(roi, roi.Length1, 0);
                PointF length2Start = LocalPoint(roi, 0, -roi.Length2);
                PointF length2End = LocalPoint(roi, 0, roi.Length2);
                PointF rotate = LocalPoint(roi, roi.Length1 + 16, 0);
                RoiEditHandle closestHandle = RoiEditHandle.None;
                double closestDistance = tolerance;
                PointF[] handlePoints = { rotate, length1Start, length1End, length2Start, length2End, center };
                RoiEditHandle[] handleKinds =
                {
                    RoiEditHandle.Rotate,
                    RoiEditHandle.Length1Start,
                    RoiEditHandle.Length1End,
                    RoiEditHandle.Length2Start,
                    RoiEditHandle.Length2End,
                    RoiEditHandle.Move
                };
                for (int index = 0; index < handlePoints.Length; index++)
                {
                    double distance = Distance(point, handlePoints[index]);
                    if (distance <= closestDistance)
                    {
                        closestDistance = distance;
                        closestHandle = handleKinds[index];
                    }
                }
                if (closestHandle != RoiEditHandle.None)
                {
                    return Hit(closestHandle);
                }

                double local1;
                double local2;
                ToLocal(roi, point, out local1, out local2);
                if (roi.ShapeType == RoiShapeType.RotatedRectangle)
                {
                    return Math.Abs(local1) <= roi.Length1 + tolerance && Math.Abs(local2) <= roi.Length2 + tolerance
                        ? Hit(RoiEditHandle.Move)
                        : NoHit();
                }

                double normalized1 = local1 / Math.Max(1, roi.Length1 + tolerance);
                double normalized2 = local2 / Math.Max(1, roi.Length2 + tolerance);
                return normalized1 * normalized1 + normalized2 * normalized2 <= 1.0
                    ? Hit(RoiEditHandle.Move)
                    : NoHit();
            }

            if (roi.ShapeType != RoiShapeType.Rectangle)
            {
                return NoHit();
            }

            double centerRow = (roi.Row1 + roi.Row2) / 2.0;
            double centerColumn = (roi.Column1 + roi.Column2) / 2.0;
            RoiEditHandle[] handles =
            {
                RoiEditHandle.TopLeft, RoiEditHandle.Top, RoiEditHandle.TopRight,
                RoiEditHandle.Left, RoiEditHandle.Right,
                RoiEditHandle.BottomLeft, RoiEditHandle.Bottom, RoiEditHandle.BottomRight
            };
            PointF[] points =
            {
                new PointF((float)roi.Column1, (float)roi.Row1),
                new PointF((float)centerColumn, (float)roi.Row1),
                new PointF((float)roi.Column2, (float)roi.Row1),
                new PointF((float)roi.Column1, (float)centerRow),
                new PointF((float)roi.Column2, (float)centerRow),
                new PointF((float)roi.Column1, (float)roi.Row2),
                new PointF((float)centerColumn, (float)roi.Row2),
                new PointF((float)roi.Column2, (float)roi.Row2)
            };
            for (int index = 0; index < handles.Length; index++)
            {
                if (Distance(point, points[index]) <= tolerance)
                {
                    return Hit(handles[index]);
                }
            }

            RoiEditHandle rectangleHandle = point.Y >= roi.Row1 - tolerance && point.Y <= roi.Row2 + tolerance &&
                   point.X >= roi.Column1 - tolerance && point.X <= roi.Column2 + tolerance
                ? RoiEditHandle.Move
                : RoiEditHandle.None;
            return rectangleHandle == RoiEditHandle.None ? NoHit() : Hit(rectangleHandle);
        }

        public static RoiData Transform(
            RoiData source,
            RoiEditHandle handle,
            PointF start,
            PointF current,
            int imageWidth,
            int imageHeight)
        {
            return TransformDetailed(source, handle, -1, start, current, imageWidth, imageHeight);
        }

        public static RoiData TransformDetailed(
            RoiData source,
            RoiEditHandle handle,
            int vertexIndex,
            PointF start,
            PointF current,
            int imageWidth,
            int imageHeight)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            double deltaColumn = current.X - start.X;
            double deltaRow = current.Y - start.Y;
            if (source.ShapeType == RoiShapeType.Point)
            {
                if (handle != RoiEditHandle.Point && handle != RoiEditHandle.Move) return source.Clone();
                double row = source.Row + deltaRow;
                double column = source.Column + deltaColumn;
                if (imageHeight > 0) row = Clamp(row, 0, imageHeight - 1);
                if (imageWidth > 0) column = Clamp(column, 0, imageWidth - 1);
                return RoiData.CreatePoint(row, column);
            }

            if (source.ShapeType == RoiShapeType.Line)
            {
                double lineRow1 = source.Row1;
                double lineColumn1 = source.Column1;
                double lineRow2 = source.Row2;
                double lineColumn2 = source.Column2;
                if (handle == RoiEditHandle.Move)
                {
                    deltaRow = ClampDelta(deltaRow, Math.Min(lineRow1, lineRow2), Math.Max(lineRow1, lineRow2), imageHeight);
                    deltaColumn = ClampDelta(deltaColumn, Math.Min(lineColumn1, lineColumn2), Math.Max(lineColumn1, lineColumn2), imageWidth);
                    return RoiData.CreateLine(lineRow1 + deltaRow, lineColumn1 + deltaColumn, lineRow2 + deltaRow, lineColumn2 + deltaColumn);
                }

                if (handle == RoiEditHandle.LineStart)
                {
                    lineRow1 = imageHeight > 0 ? Clamp(current.Y, 0, imageHeight - 1) : current.Y;
                    lineColumn1 = imageWidth > 0 ? Clamp(current.X, 0, imageWidth - 1) : current.X;
                }
                else if (handle == RoiEditHandle.LineEnd)
                {
                    lineRow2 = imageHeight > 0 ? Clamp(current.Y, 0, imageHeight - 1) : current.Y;
                    lineColumn2 = imageWidth > 0 ? Clamp(current.X, 0, imageWidth - 1) : current.X;
                }
                else
                {
                    return source.Clone();
                }

                double lineDeltaRow = lineRow2 - lineRow1;
                double lineDeltaColumn = lineColumn2 - lineColumn1;
                if (lineDeltaRow * lineDeltaRow + lineDeltaColumn * lineDeltaColumn < 4.0)
                {
                    throw new InvalidOperationException("直线长度必须至少为 2 像素。");
                }
                return RoiData.CreateLine(lineRow1, lineColumn1, lineRow2, lineColumn2);
            }

            if (source.ShapeType == RoiShapeType.Circle)
            {
                if (handle == RoiEditHandle.Radius)
                {
                    double dx = current.X - source.Column;
                    double dy = current.Y - source.Row;
                    double radius = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                    if (imageWidth > 0 && imageHeight > 0)
                    {
                        radius = Math.Min(radius, Math.Max(1, Math.Min(Math.Min(source.Column, imageWidth - 1 - source.Column), Math.Min(source.Row, imageHeight - 1 - source.Row))));
                    }
                    return RoiData.CreateCircle(source.Row, source.Column, radius);
                }

                if (handle == RoiEditHandle.Move)
                {
                    double row = source.Row + deltaRow;
                    double column = source.Column + deltaColumn;
                    if (imageWidth > 0 && imageHeight > 0)
                    {
                        row = Clamp(row, source.Radius, Math.Max(source.Radius, imageHeight - 1 - source.Radius));
                        column = Clamp(column, source.Radius, Math.Max(source.Radius, imageWidth - 1 - source.Radius));
                    }
                    return RoiData.CreateCircle(row, column, source.Radius);
                }

                return source.Clone();
            }

            if (source.ShapeType == RoiShapeType.Annulus)
            {
                if (handle == RoiEditHandle.InnerRadius || handle == RoiEditHandle.OuterRadius)
                {
                    double dx = current.X - source.Column;
                    double dy = current.Y - source.Row;
                    double radius = Math.Sqrt(dx * dx + dy * dy);
                    double innerRadius = source.InnerRadius;
                    double outerRadius = source.OuterRadius;
                    if (handle == RoiEditHandle.InnerRadius)
                    {
                        innerRadius = Clamp(radius, 1, Math.Max(1, outerRadius - 1));
                    }
                    else
                    {
                        double maximum = MaxCenteredRadius(source.Row, source.Column, imageWidth, imageHeight);
                        if (maximum > 0) radius = Math.Min(radius, maximum);
                        outerRadius = Math.Max(innerRadius + 1, radius);
                    }
                    return RoiData.CreateAnnulus(source.Row, source.Column, innerRadius, outerRadius);
                }

                if (handle == RoiEditHandle.Move)
                {
                    double row = source.Row + deltaRow;
                    double column = source.Column + deltaColumn;
                    if (imageWidth > 0 && imageHeight > 0)
                    {
                        row = Clamp(row, source.OuterRadius, Math.Max(source.OuterRadius, imageHeight - 1 - source.OuterRadius));
                        column = Clamp(column, source.OuterRadius, Math.Max(source.OuterRadius, imageWidth - 1 - source.OuterRadius));
                    }
                    return RoiData.CreateAnnulus(row, column, source.InnerRadius, source.OuterRadius);
                }

                return source.Clone();
            }

            if (source.ShapeType == RoiShapeType.Polygon)
            {
                double[] rows = (double[])source.PolygonRows.Clone();
                double[] columns = (double[])source.PolygonColumns.Clone();
                if (handle == RoiEditHandle.PolygonVertex)
                {
                    if (vertexIndex < 0 || vertexIndex >= rows.Length)
                    {
                        throw new InvalidOperationException("多边形顶点索引无效。");
                    }

                    rows[vertexIndex] = imageHeight > 0 ? Clamp(current.Y, 0, imageHeight - 1) : current.Y;
                    columns[vertexIndex] = imageWidth > 0 ? Clamp(current.X, 0, imageWidth - 1) : current.X;
                    string polygonError = ValidatePolygon(rows, columns);
                    if (!string.IsNullOrWhiteSpace(polygonError))
                    {
                        throw new InvalidOperationException(polygonError);
                    }
                    return RoiData.CreatePolygon(rows, columns);
                }

                if (handle != RoiEditHandle.Move)
                {
                    return source.Clone();
                }

                double minRow = rows.Min();
                double maxRow = rows.Max();
                double minColumn = columns.Min();
                double maxColumn = columns.Max();
                deltaRow = ClampDelta(deltaRow, minRow, maxRow, imageHeight);
                deltaColumn = ClampDelta(deltaColumn, minColumn, maxColumn, imageWidth);
                for (int index = 0; index < rows.Length; index++)
                {
                    rows[index] += deltaRow;
                    columns[index] += deltaColumn;
                }
                return RoiData.CreatePolygon(rows, columns);
            }

            if (source.ShapeType == RoiShapeType.RotatedRectangle || source.ShapeType == RoiShapeType.Ellipse)
            {
                if (handle == RoiEditHandle.Move)
                {
                    if (source.ShapeType == RoiShapeType.Ellipse)
                    {
                        double rowExtent;
                        double columnExtent;
                        GetEllipseExtents(source.Phi, source.Length1, source.Length2, out rowExtent, out columnExtent);
                        deltaRow = ClampDelta(deltaRow, source.Row - rowExtent, source.Row + rowExtent, imageHeight);
                        deltaColumn = ClampDelta(deltaColumn, source.Column - columnExtent, source.Column + columnExtent, imageWidth);
                        return RoiData.CreateEllipse(source.Row + deltaRow, source.Column + deltaColumn, source.Phi, source.Length1, source.Length2);
                    }

                    PointF[] corners = source.GetRotatedRectangleCorners();
                    deltaRow = ClampDelta(deltaRow, corners.Min(item => item.Y), corners.Max(item => item.Y), imageHeight);
                    deltaColumn = ClampDelta(deltaColumn, corners.Min(item => item.X), corners.Max(item => item.X), imageWidth);
                    return RoiData.CreateRotatedRectangle(source.Row + deltaRow, source.Column + deltaColumn, source.Phi, source.Length1, source.Length2);
                }

                double phi = source.Phi;
                double length1 = source.Length1;
                double length2 = source.Length2;
                if (handle == RoiEditHandle.Rotate)
                {
                    phi = Math.Atan2(-(current.Y - source.Row), current.X - source.Column);
                }
                else
                {
                    double local1;
                    double local2;
                    ToLocal(source, current, out local1, out local2);
                    if (handle == RoiEditHandle.Length1End) length1 = Math.Max(1, local1);
                    else if (handle == RoiEditHandle.Length1Start) length1 = Math.Max(1, -local1);
                    else if (handle == RoiEditHandle.Length2End) length2 = Math.Max(1, local2);
                    else if (handle == RoiEditHandle.Length2Start) length2 = Math.Max(1, -local2);
                    else return source.Clone();
                }

                return source.ShapeType == RoiShapeType.Ellipse
                    ? FitEllipse(source.Row, source.Column, phi, length1, length2, imageWidth, imageHeight)
                    : FitRotatedRectangle(source.Row, source.Column, phi, length1, length2, imageWidth, imageHeight);
            }

            double row1 = source.Row1;
            double row2 = source.Row2;
            double column1 = source.Column1;
            double column2 = source.Column2;
            if (handle == RoiEditHandle.Move)
            {
                deltaRow = ClampDelta(deltaRow, row1, row2, imageHeight);
                deltaColumn = ClampDelta(deltaColumn, column1, column2, imageWidth);
                return RoiData.CreateRectangle(row1 + deltaRow, column1 + deltaColumn, row2 + deltaRow, column2 + deltaColumn);
            }

            if (handle == RoiEditHandle.TopLeft || handle == RoiEditHandle.Top || handle == RoiEditHandle.TopRight) row1 += deltaRow;
            if (handle == RoiEditHandle.BottomLeft || handle == RoiEditHandle.Bottom || handle == RoiEditHandle.BottomRight) row2 += deltaRow;
            if (handle == RoiEditHandle.TopLeft || handle == RoiEditHandle.Left || handle == RoiEditHandle.BottomLeft) column1 += deltaColumn;
            if (handle == RoiEditHandle.TopRight || handle == RoiEditHandle.Right || handle == RoiEditHandle.BottomRight) column2 += deltaColumn;
            if (imageWidth > 0)
            {
                column1 = Clamp(column1, 0, imageWidth - 1);
                column2 = Clamp(column2, 0, imageWidth - 1);
            }
            if (imageHeight > 0)
            {
                row1 = Clamp(row1, 0, imageHeight - 1);
                row2 = Clamp(row2, 0, imageHeight - 1);
            }
            EnsureMinimumSpan(ref row1, ref row2, imageHeight);
            EnsureMinimumSpan(ref column1, ref column2, imageWidth);
            return RoiData.CreateRectangle(row1, column1, row2, column2);
        }

        public static RoiData Offset(RoiData source, double deltaRow, double deltaColumn, int imageWidth, int imageHeight)
        {
            return Transform(source, RoiEditHandle.Move, new PointF(0, 0), new PointF((float)deltaColumn, (float)deltaRow), imageWidth, imageHeight);
        }

        public static string ValidatePolygon(double[] rows, double[] columns)
        {
            if (rows == null || columns == null || rows.Length != columns.Length || rows.Length < 3)
            {
                return "多边形至少需要 3 个有效顶点。";
            }

            for (int index = 0; index < rows.Length; index++)
            {
                int next = (index + 1) % rows.Length;
                double dx = columns[index] - columns[next];
                double dy = rows[index] - rows[next];
                if (dx * dx + dy * dy < 1.0)
                {
                    return "相邻多边形顶点距离过小。";
                }
            }

            double signedArea = 0;
            for (int index = 0; index < rows.Length; index++)
            {
                int next = (index + 1) % rows.Length;
                signedArea += columns[index] * rows[next] - columns[next] * rows[index];
            }
            if (Math.Abs(signedArea) * 0.5 < 4.0)
            {
                return "多边形面积过小，无法形成有效运行区域。";
            }

            for (int first = 0; first < rows.Length; first++)
            {
                int firstNext = (first + 1) % rows.Length;
                for (int second = first + 1; second < rows.Length; second++)
                {
                    int secondNext = (second + 1) % rows.Length;
                    if (first == second || firstNext == second || secondNext == first)
                    {
                        continue;
                    }
                    if (SegmentsIntersect(
                        columns[first], rows[first], columns[firstNext], rows[firstNext],
                        columns[second], rows[second], columns[secondNext], rows[secondNext]))
                    {
                        return "多边形边线不能自相交。";
                    }
                }
            }

            return string.Empty;
        }

        private static RoiData FitRotatedRectangle(double row, double column, double phi, double length1, double length2, int imageWidth, int imageHeight)
        {
            if (imageWidth > 2) column = Clamp(column, 1, imageWidth - 2);
            if (imageHeight > 2) row = Clamp(row, 1, imageHeight - 2);
            using (RoiData candidate = RoiData.CreateRotatedRectangle(row, column, phi, Math.Max(1, length1), Math.Max(1, length2)))
            {
                if (imageWidth <= 0 || imageHeight <= 0)
                {
                    return candidate.Clone();
                }

                double scale = 1.0;
                foreach (PointF corner in candidate.GetRotatedRectangleCorners())
                {
                    double dx = corner.X - column;
                    double dy = corner.Y - row;
                    if (dx > 0) scale = Math.Min(scale, (imageWidth - 1 - column) / dx);
                    else if (dx < 0) scale = Math.Min(scale, column / -dx);
                    if (dy > 0) scale = Math.Min(scale, (imageHeight - 1 - row) / dy);
                    else if (dy < 0) scale = Math.Min(scale, row / -dy);
                }

                scale = Math.Max(0, Math.Min(1, scale));
                return RoiData.CreateRotatedRectangle(row, column, phi, Math.Max(1, length1 * scale), Math.Max(1, length2 * scale));
            }
        }

        private static RoiData FitEllipse(double row, double column, double phi, double radius1, double radius2, int imageWidth, int imageHeight)
        {
            radius1 = Math.Max(2, radius1);
            radius2 = Math.Max(2, radius2);
            if (imageWidth <= 0 || imageHeight <= 0)
            {
                return RoiData.CreateEllipse(row, column, phi, radius1, radius2);
            }

            column = Clamp(column, 2, Math.Max(2, imageWidth - 3));
            row = Clamp(row, 2, Math.Max(2, imageHeight - 3));
            double rowExtent;
            double columnExtent;
            GetEllipseExtents(phi, radius1, radius2, out rowExtent, out columnExtent);
            double scale = 1.0;
            if (columnExtent > 0)
            {
                scale = Math.Min(scale, Math.Min(column, imageWidth - 1 - column) / columnExtent);
            }
            if (rowExtent > 0)
            {
                scale = Math.Min(scale, Math.Min(row, imageHeight - 1 - row) / rowExtent);
            }
            scale = Math.Max(0, Math.Min(1, scale));
            return RoiData.CreateEllipse(row, column, phi, Math.Max(2, radius1 * scale), Math.Max(2, radius2 * scale));
        }

        private static void GetEllipseExtents(double phi, double radius1, double radius2, out double rowExtent, out double columnExtent)
        {
            double cos = Math.Cos(phi);
            double sin = Math.Sin(phi);
            columnExtent = Math.Sqrt(radius1 * radius1 * cos * cos + radius2 * radius2 * sin * sin);
            rowExtent = Math.Sqrt(radius1 * radius1 * sin * sin + radius2 * radius2 * cos * cos);
        }

        private static double MaxCenteredRadius(double row, double column, int imageWidth, int imageHeight)
        {
            if (imageWidth <= 0 || imageHeight <= 0)
            {
                return 0;
            }

            return Math.Max(1, Math.Min(Math.Min(column, imageWidth - 1 - column), Math.Min(row, imageHeight - 1 - row)));
        }

        private static PointF LocalPoint(RoiData roi, double local1, double local2)
        {
            double cos = Math.Cos(roi.Phi);
            double sin = Math.Sin(roi.Phi);
            return new PointF(
                (float)(roi.Column + local1 * cos + local2 * sin),
                (float)(roi.Row - local1 * sin + local2 * cos));
        }

        private static void ToLocal(RoiData roi, PointF point, out double local1, out double local2)
        {
            double dx = point.X - roi.Column;
            double dy = point.Y - roi.Row;
            double cos = Math.Cos(roi.Phi);
            double sin = Math.Sin(roi.Phi);
            local1 = dx * cos - dy * sin;
            local2 = dx * sin + dy * cos;
        }

        private static bool PointInPolygon(double[] rows, double[] columns, PointF point)
        {
            bool inside = false;
            int previous = rows.Length - 1;
            for (int current = 0; current < rows.Length; current++)
            {
                bool crosses = (rows[current] > point.Y) != (rows[previous] > point.Y) &&
                               point.X < (columns[previous] - columns[current]) * (point.Y - rows[current]) /
                               (rows[previous] - rows[current]) + columns[current];
                if (crosses) inside = !inside;
                previous = current;
            }
            return inside;
        }

        private static bool IsNearPolygonEdge(double[] rows, double[] columns, PointF point, double tolerance)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                int next = (index + 1) % rows.Length;
                if (DistanceToSegment(point.X, point.Y, columns[index], rows[index], columns[next], rows[next]) <= tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        private static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            double t = Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSquared));
            double closestX = x1 + t * dx;
            double closestY = y1 + t * dy;
            return Math.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));
        }

        private static bool SegmentsIntersect(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            double o1 = Orientation(ax, ay, bx, by, cx, cy);
            double o2 = Orientation(ax, ay, bx, by, dx, dy);
            double o3 = Orientation(cx, cy, dx, dy, ax, ay);
            double o4 = Orientation(cx, cy, dx, dy, bx, by);
            const double epsilon = 0.000001;
            if (o1 * o2 < -epsilon && o3 * o4 < -epsilon)
            {
                return true;
            }

            return (Math.Abs(o1) <= epsilon && IsOnSegment(ax, ay, bx, by, cx, cy, epsilon)) ||
                   (Math.Abs(o2) <= epsilon && IsOnSegment(ax, ay, bx, by, dx, dy, epsilon)) ||
                   (Math.Abs(o3) <= epsilon && IsOnSegment(cx, cy, dx, dy, ax, ay, epsilon)) ||
                   (Math.Abs(o4) <= epsilon && IsOnSegment(cx, cy, dx, dy, bx, by, epsilon));
        }

        private static bool IsOnSegment(double ax, double ay, double bx, double by, double px, double py, double epsilon)
        {
            return px >= Math.Min(ax, bx) - epsilon && px <= Math.Max(ax, bx) + epsilon &&
                   py >= Math.Min(ay, by) - epsilon && py <= Math.Max(ay, by) + epsilon;
        }

        private static double Orientation(double ax, double ay, double bx, double by, double cx, double cy)
        {
            return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        }

        private static RoiEditHit Hit(RoiEditHandle handle, int vertexIndex = -1)
        {
            return new RoiEditHit { Handle = handle, VertexIndex = vertexIndex };
        }

        private static RoiEditHit NoHit()
        {
            return Hit(RoiEditHandle.None);
        }

        private static double ClampDelta(double delta, double minimum, double maximum, int imageSize)
        {
            if (imageSize <= 0)
            {
                return delta;
            }

            return Clamp(delta, -minimum, imageSize - 1 - maximum);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (maximum < minimum)
            {
                return (minimum + maximum) / 2.0;
            }
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static void EnsureMinimumSpan(ref double first, ref double second, int imageSize)
        {
            double low = Math.Min(first, second);
            double high = Math.Max(first, second);
            if (high - low >= 2)
            {
                return;
            }

            if (imageSize > 0 && imageSize < 3)
            {
                low = 0;
                high = Math.Max(0, imageSize - 1);
            }
            else if (imageSize >= 3)
            {
                double maximum = imageSize - 1;
                low = Clamp((low + high) / 2.0 - 1, 0, maximum - 2);
                high = low + 2;
            }
            else
            {
                low = (low + high) / 2.0 - 1;
                high = low + 2;
            }

            if (first <= second)
            {
                first = low;
                second = high;
            }
            else
            {
                first = high;
                second = low;
            }
        }

        private static double Distance(PointF left, PointF right)
        {
            double dx = left.X - right.X;
            double dy = left.Y - right.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double DistanceToSegment(PointF point, PointF start, PointF end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double denominator = dx * dx + dy * dy;
            if (denominator <= 0.000001) return Distance(point, start);
            double t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / denominator;
            t = Math.Max(0, Math.Min(1, t));
            return Distance(point, new PointF((float)(start.X + t * dx), (float)(start.Y + t * dy)));
        }
    }

    public sealed class RoiEditor : IDisposable
    {
        private PointF startPoint;
        private readonly List<PointF> polygonPoints = new List<PointF>();
        private RoiData previewRoi;

        public bool IsDrawing { get; private set; }

        public bool IsPolygonDrawing
        {
            get { return polygonPoints.Count > 0; }
        }

        public RoiData PreviewRoi
        {
            get { return previewRoi; }
        }

        public VisionTool Tool { get; set; }

        public bool Begin(PointF imagePoint)
        {
            if (!IsDragShapeTool(Tool))
            {
                return false;
            }

            startPoint = imagePoint;
            IsDrawing = true;
            SetPreview(CreateRoi(imagePoint));
            return true;
        }

        public bool AddPolygonPoint(PointF imagePoint)
        {
            if (Tool != VisionTool.PolygonRoi)
            {
                return false;
            }

            polygonPoints.Add(imagePoint);
            UpdatePolygonPreview(null);
            return true;
        }

        public void UpdatePolygon(PointF imagePoint)
        {
            if (Tool != VisionTool.PolygonRoi || polygonPoints.Count == 0)
            {
                return;
            }

            UpdatePolygonPreview(imagePoint);
        }

        public RoiData CompletePolygon()
        {
            if (polygonPoints.Count < 3)
            {
                return null;
            }

            RoiData roi = RoiData.CreatePolygon(polygonPoints);
            polygonPoints.Clear();
            ClearPreview();
            return roi;
        }

        public void Update(PointF imagePoint)
        {
            if (!IsDrawing)
            {
                return;
            }

            SetPreview(CreateRoi(imagePoint));
        }

        public RoiData Complete(PointF imagePoint)
        {
            if (!IsDrawing)
            {
                return null;
            }

            IsDrawing = false;
            RoiData roi = CreateRoi(imagePoint);
            ClearPreview();
            return roi;
        }

        public void Cancel()
        {
            IsDrawing = false;
            polygonPoints.Clear();
            ClearPreview();
        }

        public void Dispose()
        {
            ClearPreview();
        }

        private RoiData CreateRoi(PointF imagePoint)
        {
            if (Tool == VisionTool.PointRoi)
            {
                return RoiData.CreatePoint(startPoint.Y, startPoint.X);
            }

            if (Tool == VisionTool.LineRoi)
            {
                double endX = imagePoint.X;
                double endY = imagePoint.Y;
                double dx = endX - startPoint.X;
                double dy = endY - startPoint.Y;
                if (dx * dx + dy * dy < 4.0)
                {
                    endX = startPoint.X + 2.0;
                    endY = startPoint.Y;
                }
                return RoiData.CreateLine(startPoint.Y, startPoint.X, endY, endX);
            }

            if (Tool == VisionTool.EllipseRoi)
            {
                double dx = imagePoint.X - startPoint.X;
                double dy = imagePoint.Y - startPoint.Y;
                double radius1 = Math.Max(2, Math.Sqrt(dx * dx + dy * dy));
                return RoiData.CreateEllipse(startPoint.Y, startPoint.X, Math.Atan2(-dy, dx), radius1, Math.Max(2, radius1 * 0.6));
            }

            if (Tool == VisionTool.AnnulusRoi)
            {
                double dx = imagePoint.X - startPoint.X;
                double dy = imagePoint.Y - startPoint.Y;
                double outerRadius = Math.Max(3, Math.Sqrt(dx * dx + dy * dy));
                return RoiData.CreateAnnulus(startPoint.Y, startPoint.X, Math.Max(1, outerRadius * 0.55), outerRadius);
            }

            if (Tool == VisionTool.RotatedRectangleRoi)
            {
                double dx = imagePoint.X - startPoint.X;
                double dy = imagePoint.Y - startPoint.Y;
                double length1 = Math.Max(2, Math.Sqrt(dx * dx + dy * dy));
                double length2 = Math.Max(2, length1 * 0.5);
                double phi = Math.Atan2(-dy, dx);
                return RoiData.CreateRotatedRectangle(startPoint.Y, startPoint.X, phi, length1, length2);
            }

            if (Tool == VisionTool.CircleRoi || Tool == VisionTool.TemplateCircleRoi || Tool == VisionTool.MaskCircleAdd)
            {
                double dx = imagePoint.X - startPoint.X;
                double dy = imagePoint.Y - startPoint.Y;
                double radius = Math.Sqrt(dx * dx + dy * dy);
                return RoiData.CreateCircle(startPoint.Y, startPoint.X, Math.Max(1, radius));
            }

            return RoiData.CreateRectangle(startPoint.Y, startPoint.X, imagePoint.Y, imagePoint.X);
        }

        public static bool IsSearchRoiTool(VisionTool tool)
        {
            return tool == VisionTool.RectangleRoi || tool == VisionTool.CircleRoi || tool == VisionTool.EllipseRoi || tool == VisionTool.AnnulusRoi || tool == VisionTool.PolygonRoi || tool == VisionTool.RotatedRectangleRoi || tool == VisionTool.LineRoi || tool == VisionTool.PointRoi;
        }

        public static bool IsTemplateRoiTool(VisionTool tool)
        {
            return tool == VisionTool.TemplateRectangleRoi || tool == VisionTool.TemplateCircleRoi;
        }

        public static bool IsMaskShapeTool(VisionTool tool)
        {
            return tool == VisionTool.MaskRectangleAdd || tool == VisionTool.MaskCircleAdd;
        }

        public static bool IsBrushTool(VisionTool tool)
        {
            return tool == VisionTool.MaskBrushAdd || tool == VisionTool.MaskEraser;
        }

        private static bool IsDragShapeTool(VisionTool tool)
        {
            return (IsSearchRoiTool(tool) && tool != VisionTool.PolygonRoi) || IsTemplateRoiTool(tool) || IsMaskShapeTool(tool);
        }

        private void UpdatePolygonPreview(PointF? floatingPoint)
        {
            if (polygonPoints.Count < 2)
            {
                ClearPreview();
                return;
            }

            List<PointF> points = polygonPoints.ToList();
            if (floatingPoint.HasValue)
            {
                points.Add(floatingPoint.Value);
            }

            if (points.Count < 3)
            {
                ClearPreview();
                return;
            }

            SetPreview(RoiData.CreatePolygon(points));
        }

        private void SetPreview(RoiData roi)
        {
            ClearPreview();
            previewRoi = roi;
        }

        private void ClearPreview()
        {
            if (previewRoi != null)
            {
                previewRoi.Dispose();
                previewRoi = null;
            }
        }
    }
}
