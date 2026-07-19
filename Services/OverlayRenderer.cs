using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class OverlayRenderer
    {
        public void DrawRoiLayer(HWindow window, RoiData roi, string color, double width, bool selected, bool locked)
        {
            DrawRoi(window, roi, color, width);
            if (selected)
            {
                DrawEditHandles(window, roi, locked);
            }
        }

        public void Draw(
            HWindow window,
            RoiData searchRoi,
            RoiData templateRoi,
            RoiData previewRoi,
            HRegion trainingMask,
            RoiData displayFrame,
            IList<ShapeMatchResult> matches,
            ShapeTemplateService templateService,
            bool showSearchRoi,
            bool showTemplateRoi,
            bool showDisplayFrame)
        {
            window.SetDraw("margin");
            if (showSearchRoi)
            {
                DrawRoi(window, searchRoi, "green", 2);
            }

            if (showTemplateRoi)
            {
                DrawRoi(window, templateRoi, "orange", 2);
            }

            DrawRegionBoundary(window, trainingMask, "cyan", 2);
            if (showDisplayFrame)
            {
                DrawRoi(window, displayFrame, "magenta", 1);
                DrawFrameHandles(window, displayFrame);
            }

            DrawRoi(window, previewRoi, "red", 2);
            DrawMatches(window, matches, templateService);
        }

        private static void DrawRoi(HWindow window, RoiData roi, string color, double width)
        {
            if (roi == null)
            {
                return;
            }

            window.SetDraw("margin");
            window.SetColor(color);
            window.SetLineWidth(width);
            if (roi.ShapeType == RoiShapeType.Polygon)
            {
                DrawRegionBoundary(window, roi.Region, color, width);
            }
            else if (roi.ShapeType == RoiShapeType.RotatedRectangle)
            {
                window.DispRectangle2(roi.Row, roi.Column, roi.Phi, roi.Length1, roi.Length2);
            }
            else if (roi.ShapeType == RoiShapeType.Circle)
            {
                window.DispCircle(roi.Row, roi.Column, roi.Radius);
            }
            else
            {
                window.DispRectangle1(roi.Row1, roi.Column1, roi.Row2, roi.Column2);
            }
        }

        private static void DrawRegionBoundary(HWindow window, HRegion region, string color, double width)
        {
            if (region == null)
            {
                return;
            }

            HObject contour = null;
            try
            {
                window.SetDraw("margin");
                window.SetColor(color);
                window.SetLineWidth(width);
                HOperatorSet.GenContourRegionXld(region, out contour, "border");
                window.DispObj(contour);
            }
            catch (HalconException)
            {
                window.DispObj(region);
            }
            finally
            {
                if (contour != null)
                {
                    contour.Dispose();
                }
            }
        }

        private static void DrawMatches(HWindow window, IList<ShapeMatchResult> matches, ShapeTemplateService templateService)
        {
            if (matches == null || matches.Count == 0)
            {
                return;
            }

            window.SetDraw("margin");
            window.SetColor("green");
            window.SetLineWidth(2);

            if (templateService != null && templateService.HasModel)
            {
                try
                {
                    templateService.DisplayMatchContours(window, matches);
                }
                catch (HalconException)
                {
                    // Fallback geometry below keeps the result visible.
                }
            }

            foreach (ShapeMatchResult match in matches)
            {
                DrawMatchFrame(window, match);
                window.DispCross(match.Row, match.Column, 48, DegToRad(match.AngleDeg));
                window.DispText(
                    string.Format("{0:F3}", match.Score),
                    "image",
                    (int)Math.Max(0, match.Row - 60),
                    (int)Math.Max(0, match.Column + 32),
                    "green",
                    "box",
                    "false");
            }
        }

        private static void DrawFrameHandles(HWindow window, RoiData roi)
        {
            if (roi == null)
            {
                return;
            }

            window.SetColor("magenta");
            window.SetLineWidth(1);
            if (roi.ShapeType == RoiShapeType.Polygon)
            {
                for (int index = 0; index < roi.PolygonRows.Length; index++)
                {
                    window.DispCross(roi.PolygonRows[index], roi.PolygonColumns[index], 14, 0);
                }
                return;
            }

            if (roi.ShapeType == RoiShapeType.RotatedRectangle)
            {
                DrawRotatedRectangleHandles(window, roi, false);
                return;
            }

            if (roi.ShapeType == RoiShapeType.Circle)
            {
                window.DispCross(roi.Row, roi.Column + roi.Radius, 16, 0);
                return;
            }

            double centerRow = (roi.Row1 + roi.Row2) / 2.0;
            double centerColumn = (roi.Column1 + roi.Column2) / 2.0;
            window.DispCross(roi.Row1, roi.Column1, 14, 0);
            window.DispCross(roi.Row1, centerColumn, 14, 0);
            window.DispCross(roi.Row1, roi.Column2, 14, 0);
            window.DispCross(centerRow, roi.Column1, 14, 0);
            window.DispCross(centerRow, roi.Column2, 14, 0);
            window.DispCross(roi.Row2, roi.Column1, 14, 0);
            window.DispCross(roi.Row2, centerColumn, 14, 0);
            window.DispCross(roi.Row2, roi.Column2, 14, 0);
        }

        private static void DrawEditHandles(HWindow window, RoiData roi, bool locked)
        {
            if (roi == null)
            {
                return;
            }

            window.SetColor(locked ? "yellow" : "green");
            window.SetLineWidth(2);
            if (roi.ShapeType == RoiShapeType.Polygon)
            {
                for (int index = 0; index < roi.PolygonRows.Length; index++)
                {
                    window.DispCross(roi.PolygonRows[index], roi.PolygonColumns[index], locked ? 12 : 18, 0);
                    if (!locked)
                    {
                        SafeDispText(
                            window,
                            (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                            (int)Math.Max(0, roi.PolygonRows[index] - 16),
                            (int)Math.Max(0, roi.PolygonColumns[index] + 8),
                            "green");
                    }
                }
                if (locked)
                {
                    SafeDispText(window, "LOCK", (int)Math.Max(0, roi.Row1 - 18), (int)Math.Max(0, roi.Column1), "yellow");
                }
                return;
            }

            if (roi.ShapeType == RoiShapeType.RotatedRectangle)
            {
                DrawRotatedRectangleHandles(window, roi, locked);
                return;
            }

            if (roi.ShapeType == RoiShapeType.Circle)
            {
                window.DispCross(roi.Row, roi.Column, 14, 0);
                window.DispCross(roi.Row, roi.Column + roi.Radius, 18, 0);
                if (locked)
                {
                    SafeDispText(window, "LOCK", (int)Math.Max(0, roi.Row - roi.Radius - 18), (int)Math.Max(0, roi.Column - 18), "yellow");
                }
                return;
            }

            double centerRow = (roi.Row1 + roi.Row2) / 2.0;
            double centerColumn = (roi.Column1 + roi.Column2) / 2.0;
            window.DispCross(centerRow, centerColumn, 14, 0);
            window.DispCross(roi.Row1, roi.Column1, 14, 0);
            window.DispCross(roi.Row1, centerColumn, 14, 0);
            window.DispCross(roi.Row1, roi.Column2, 14, 0);
            window.DispCross(centerRow, roi.Column1, 14, 0);
            window.DispCross(centerRow, roi.Column2, 14, 0);
            window.DispCross(roi.Row2, roi.Column1, 14, 0);
            window.DispCross(roi.Row2, centerColumn, 14, 0);
            window.DispCross(roi.Row2, roi.Column2, 14, 0);
            if (locked)
            {
                SafeDispText(window, "LOCK", (int)Math.Max(0, roi.Row1 - 18), (int)Math.Max(0, roi.Column1), "yellow");
            }
        }

        private static void DrawRotatedRectangleHandles(HWindow window, RoiData roi, bool locked)
        {
            PointD center = LocalPoint(roi, 0, 0);
            PointD length1Start = LocalPoint(roi, -roi.Length1, 0);
            PointD length1End = LocalPoint(roi, roi.Length1, 0);
            PointD length2Start = LocalPoint(roi, 0, -roi.Length2);
            PointD length2End = LocalPoint(roi, 0, roi.Length2);
            PointD rotate = LocalPoint(roi, roi.Length1 + 16, 0);

            window.SetColor(locked ? "yellow" : "green");
            window.DispCross(center.Row, center.Column, 14, roi.Phi);
            window.DispCross(length1Start.Row, length1Start.Column, locked ? 12 : 18, roi.Phi);
            window.DispCross(length1End.Row, length1End.Column, locked ? 12 : 18, roi.Phi);
            window.DispCross(length2Start.Row, length2Start.Column, locked ? 12 : 18, roi.Phi);
            window.DispCross(length2End.Row, length2End.Column, locked ? 12 : 18, roi.Phi);
            if (!locked)
            {
                window.SetColor("orange");
                window.DispLine(length1End.Row, length1End.Column, rotate.Row, rotate.Column);
                window.DispCircle(rotate.Row, rotate.Column, 4);
            }
            else
            {
                PointF[] corners = roi.GetRotatedRectangleCorners();
                double top = corners.Length == 0 ? roi.Row : corners.Min(item => (double)item.Y);
                double left = corners.Length == 0 ? roi.Column : corners.Min(item => (double)item.X);
                SafeDispText(window, "LOCK", (int)Math.Max(0, top - 18), (int)Math.Max(0, left), "yellow");
            }
        }

        private static void SafeDispText(HWindow window, string text, int row, int column, string color)
        {
            try
            {
                window.DispText(text, "image", row, column, color, "box", "false");
            }
            catch (HalconException)
            {
                try
                {
                    window.SetColor(color);
                    window.SetTposition(row, column);
                    window.WriteString(text);
                }
                catch (HalconException)
                {
                    // Some embedded window types cannot render text; geometry handles remain visible.
                }
            }
        }

        private static PointD LocalPoint(RoiData roi, double local1, double local2)
        {
            double cos = Math.Cos(roi.Phi);
            double sin = Math.Sin(roi.Phi);
            return new PointD
            {
                Column = roi.Column + local1 * cos + local2 * sin,
                Row = roi.Row - local1 * sin + local2 * cos
            };
        }

        private struct PointD
        {
            public double Row;
            public double Column;
        }

        private static void DrawMatchFrame(HWindow window, ShapeMatchResult match)
        {
            if (!match.RoiShapeType.HasValue)
            {
                return;
            }

            window.SetDraw("margin");
            if (match.RoiShapeType.Value == RoiShapeType.Circle && match.RoiRadius > 0)
            {
                window.DispCircle(match.Row, match.Column, match.RoiRadius);
                return;
            }

            if (match.RoiWidth > 0 && match.RoiHeight > 0)
            {
                window.DispRectangle2(
                    match.Row,
                    match.Column,
                    DegToRad(match.AngleDeg),
                    match.RoiWidth / 2.0,
                    match.RoiHeight / 2.0);
            }
        }

        private static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }
    }
}
