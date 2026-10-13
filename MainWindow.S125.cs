using System;
using System.Globalization;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmWorldPoint2DS124 GetWorldDraftPointS125(VmInputPortEditorRow row)
        {
            if (row == null || row.SourceOptions == null) return null;
            var selected = row.SourceOptions.FirstOrDefault(item => item.Key == row.SelectedSourceKey);
            if (selected == null || selected.IsDefault || !selected.IsValid) return null;
            VmToolInstance source = flowTools.FirstOrDefault(item => item.ToolId == selected.SourceToolId);
            object raw;
            if (source != null && source.Kind == VmToolKind.PlanarCalibration2D && !IsAppliedPlanarOutputCurrentS148(source)) return null;
            return source != null && source.IsEnabled && source.ResultCode == "OK" &&
                source.TryGetOutputValue(selected.SourcePortName, out raw) ? raw as VmWorldPoint2DS124 : null;
        }

        private string GetWorldCalibrationLabelS125(VmWorldPoint2DS124 point)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.CoordinateContextId)) return "未确定";
            string identity = point.CoordinateContextId.Split(':')[0];
            VmToolInstance calibration = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, identity, StringComparison.OrdinalIgnoreCase));
            return calibration == null ? (identity.Length > 8 ? identity.Substring(0, 8) : identity) : calibration.InstanceName;
        }

        private string BuildWorldPairInputSummaryS125(VmToolInstance tool)
        {
            var a = tool.GetInputBinding("PointA");
            var b = tool.GetInputBinding("PointB");
            return "世界点 A←" + (a == null ? "未连接" : a.SourcePortName) +
                " · B←" + (b == null ? "未连接" : b.SourcePortName);
        }

        private VmWorldPoint2DS124 ReadWorldPairPointS125(VmToolInstance tool, string portName)
        {
            var binding = tool.GetInputBinding(portName);
            if (binding == null) throw new InvalidOperationException(portName + " 未连接；请在输入页选择 World2D 点。");
            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(portName + "：" + error);
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            if (source == null || !source.IsEnabled || source.ResultCode != "OK" ||
                !source.TryGetOutputValue(binding.SourcePortName, out raw) || !(raw is VmWorldPoint2DS124))
                throw new InvalidOperationException(portName + " 上游未在本周期输出有效 World2D 点；请先运行来源模块。");
            var point = ((VmWorldPoint2DS124)raw).Clone();
            error = point.GetValidationError();
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(portName + "：" + error);
            if (source.Kind == VmToolKind.PlanarCalibration2D) RequireAppliedPlanarOutputS148(source);
            return point;
        }

        private static string GetWorldPairMismatchS125(VmWorldPoint2DS124 a, VmWorldPoint2DS124 b)
        {
            if (a == null || b == null) return string.Empty;
            if (!string.Equals(a.CoordinateContextId, b.CoordinateContextId, StringComparison.OrdinalIgnoreCase))
                return "两点来自不同平面标定；请在输入页重选同一标定来源，或打开上游标定配置。";
            if (!string.Equals(a.SourceImageContextId, b.SourceImageContextId, StringComparison.OrdinalIgnoreCase))
                return "两点不属于同一图像链；请重选同源标定点。";
            if (!string.Equals(a.Unit, b.Unit, StringComparison.Ordinal))
                return "两点单位不同；请在上游标定配置中统一单位，不能隐式换算。";
            return string.Empty;
        }

        private InspectionRecord RunWorldPointPairToolS125(VmToolInstance tool)
        {
            var bindingA = tool.GetInputBinding("PointA");
            var bindingB = tool.GetInputBinding("PointB");
            if (bindingA != null && bindingB != null &&
                string.Equals(bindingA.SourceToolId, bindingB.SourceToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(bindingA.SourcePortName, bindingB.SourcePortName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A/B 不能选择同一上游端点；请在输入页选择两个不同世界点。");
            VmWorldPoint2DS124 a = ReadWorldPairPointS125(tool, "PointA");
            VmWorldPoint2DS124 b = ReadWorldPairPointS125(tool, "PointB");
            string mismatch = GetWorldPairMismatchS125(a, b);
            if (!string.IsNullOrWhiteSpace(mismatch)) throw new InvalidOperationException(mismatch);
            double dx = b.X - a.X, dy = b.Y - a.Y;
            HTuple halconDistance = null;
            try
            {
                HOperatorSet.DistancePp(a.Y, a.X, b.Y, b.X, out halconDistance);
                double distance = halconDistance[0].D;
                double direction = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                if (double.IsNaN(distance) || double.IsInfinity(distance) || double.IsNaN(direction) || double.IsInfinity(direction))
                    throw new InvalidOperationException("世界测量结果非有限；请检查坐标和标定。 ");
                var mid = a.Clone(); mid.X = a.X + dx / 2.0; mid.Y = a.Y + dy / 2.0;
                tool.SetOutputValue("WorldDistance", distance);
                tool.SetOutputValue("DirectionAngle", direction);
                tool.SetOutputValue("DeltaX", dx); tool.SetOutputValue("DeltaY", dy);
                tool.SetOutputValue("MidPoint", mid);
                tool.SetOutputValue("Unit", a.Unit);
                tool.SetOutputValue("CalibrationSource", a.CoordinateContextId);
                tool.SetOutputValue("ResultCode", "OK");
                string message = string.Format(CultureInfo.InvariantCulture,
                    "世界距离 {0:0.####} {1} · 方向 {2:0.###}° · 标定 {3}；可将 WorldDistance 接数值判定",
                    distance, a.Unit, direction, GetWorldCalibrationLabelS125(a));
                var record = CreateRecord("WorldPointPairMeasurement", "OK", distance, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
            finally { if (halconDistance != null) halconDistance.Dispose(); }
        }
    }
}
