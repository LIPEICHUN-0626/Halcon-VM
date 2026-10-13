using System;
using System.Collections.Generic;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmFlowValueTypeOptionS103
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
        public string Group { get; set; }
    }

    public static class VmFlowValueContractS103
    {
        public static IEnumerable<VmFlowValueTypeOptionS103> GetTypes(bool includeResultCode)
        {
            string[] keys = { "Number", "String", "Boolean", "ResultCode", "Image", "Region", "XLD", "Point2D", "Point2D[]", "Pose2D", "Line2D", "Circle2D", "Ellipse2D", "WorldPoint2D", "Number[]", "String[]" };
            string[] labels = { "数值", "文本", "布尔", "结果码", "图像", "区域", "轮廓", "点", "点集", "位置姿态", "直线", "圆", "椭圆", "世界点", "数值数组", "文本数组" };
            for (int i = 0; i < keys.Length; i++)
            {
                if (!includeResultCode && keys[i] == "ResultCode") continue;
                yield return new VmFlowValueTypeOptionS103 { Key = keys[i], DisplayText = labels[i], Group = i < 4 ? "数值文本" : i < 7 ? "图像图形" : i < 14 ? "几何点集" : "数组" };
            }
        }

        public static string Normalize(string type, bool includeResultCode)
        {
            return GetTypes(includeResultCode).Select(item => item.Key).FirstOrDefault(key => string.Equals(key, type, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsGeometryType(string type)
        {
            return GetTypes(true).Any(item => item.Group == "几何点集" && item.Key == type);
        }

        public static string GetGeometryType(object value)
        {
            return value is VmWorldPoint2DS124 ? "WorldPoint2D" : value is VmPoint2D ? "Point2D" : value is VmPoint2DArray ? "Point2D[]" : value is VmPose2D ? "Pose2D" :
                value is VmLine2D ? "Line2D" : value is VmCircle2D ? "Circle2D" : value is VmEllipse2D ? "Ellipse2D" : null;
        }

        public static string GetContextId(object value)
        {
            if (value is VmPoint2D) return ((VmPoint2D)value).CoordinateContextId;
            if (value is VmWorldPoint2DS124) return ((VmWorldPoint2DS124)value).CoordinateContextId;
            if (value is VmPoint2DArray) return ((VmPoint2DArray)value).CoordinateContextId;
            if (value is VmPose2D) return ((VmPose2D)value).CoordinateContextId;
            if (value is VmLine2D) return ((VmLine2D)value).CoordinateContextId;
            if (value is VmCircle2D) return ((VmCircle2D)value).CoordinateContextId;
            if (value is VmEllipse2D) return ((VmEllipse2D)value).CoordinateContextId;
            return null;
        }

        public static void RequireGeometry(string type, object value)
        {
            if (!IsGeometryType(type) || GetGeometryType(value) != type) throw new InvalidOperationException("几何结果与声明类型 " + type + " 不一致。");
            string error = null;
            if (value is VmPoint2D) error = ((VmPoint2D)value).GetValidationError();
            else if (value is VmWorldPoint2DS124) error = ((VmWorldPoint2DS124)value).GetValidationError();
            else if (value is VmLine2D) error = ((VmLine2D)value).GetValidationError();
            else if (value is VmCircle2D) error = ((VmCircle2D)value).GetValidationError();
            else if (value is VmEllipse2D) error = ((VmEllipse2D)value).GetValidationError();
            else if (value is VmPose2D)
            {
                var pose = (VmPose2D)value;
                error = pose.GetValidationError();
                if (pose.CoordinateContext != VmPose2D.ImagePixelContext) error = "几何复用当前只接受 ImagePixel，不能隐式转换坐标体系。";
            }
            else if (value is VmPoint2DArray)
            {
                var points = (VmPoint2DArray)value;
                if (!points.IsValid || points.Points == null || points.CoordinateContext != VmPoint2D.ImagePixelContext || string.IsNullOrWhiteSpace(points.CoordinateContextId))
                    error = "点集容器无效或缺少 ImagePixel 坐标身份。";
                else if (points.Points.Any(point => point != null && (point.CoordinateContext != points.CoordinateContext || point.CoordinateContextId != points.CoordinateContextId)))
                    error = "点集包含不一致的坐标来源，不能跨调用或分支混用。";
                // Preserve null/invalid members and empty collections for the downstream tool's explicit policy.
            }
            string context = GetContextId(value);
            if (context != null && context.StartsWith("Pending:", StringComparison.Ordinal)) error = "几何坐标来源尚未确定，请先运行真实上游。";
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
        }

        public static object CloneGeometry(object value)
        {
            string type = GetGeometryType(value);
            RequireGeometry(type, value);
            if (value is VmPoint2D) return ((VmPoint2D)value).Clone();
            if (value is VmWorldPoint2DS124) return ((VmWorldPoint2DS124)value).Clone();
            if (value is VmPoint2DArray) return ((VmPoint2DArray)value).Clone();
            if (value is VmPose2D) return ((VmPose2D)value).Clone();
            if (value is VmLine2D) return ((VmLine2D)value).Clone();
            if (value is VmCircle2D) return ((VmCircle2D)value).Clone();
            if (value is VmEllipse2D) return ((VmEllipse2D)value).Clone();
            throw new InvalidOperationException("不支持的几何结果。");
        }
    }
}
