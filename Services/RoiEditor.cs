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
        PolygonRoi,
        TemplateRectangleRoi,
        TemplateCircleRoi,
        MaskRectangleAdd,
        MaskCircleAdd,
        MaskBrushAdd,
        MaskEraser
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
            return tool == VisionTool.RectangleRoi || tool == VisionTool.CircleRoi || tool == VisionTool.PolygonRoi;
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
