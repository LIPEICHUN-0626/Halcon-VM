using System;
using System.Collections.Generic;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class OverlayRenderer
    {
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
