using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void RefreshDockPositionCorrectionOptions(VmToolInstance target, string selectedToolId)
        {
            if (DockRoiPositionCorrectionComboBox == null) return;
            int targetIndex = target == null ? -1 : flowTools.IndexOf(target);
            List<VmToolValueOption> options = new List<VmToolValueOption>
            {
                new VmToolValueOption { Key = string.Empty, DisplayText = "不使用 · 基础 ROI" }
            };
            foreach (VmToolInstance source in flowTools.Where(item => item.Kind == VmToolKind.PositionCorrection2D))
            {
                int sourceIndex = flowTools.IndexOf(source);
                if (targetIndex >= 0 && sourceIndex >= targetIndex) continue;
                options.Add(new VmToolValueOption
                {
                    Key = source.ToolId,
                    DisplayText = source.SequenceText + "  " + source.InstanceName + " · " +
                                  (string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) ? "本周期有值" : "等待运行")
                });
            }
            if (!string.IsNullOrWhiteSpace(selectedToolId) && options.All(item => !string.Equals(item.Key, selectedToolId, StringComparison.OrdinalIgnoreCase)))
            {
                options.Add(new VmToolValueOption { Key = selectedToolId, DisplayText = "无效来源 · " + selectedToolId.Substring(0, Math.Min(8, selectedToolId.Length)) });
            }
            DockRoiPositionCorrectionComboBox.ItemsSource = options;
            DockRoiPositionCorrectionComboBox.SelectedValue = selectedToolId ?? string.Empty;
            UpdateDockPositionCorrectionStatus(target, selectedToolId);
        }

        private void UpdateDockPositionCorrectionStatus(VmToolInstance target, string correctionToolId)
        {
            if (DockRoiPositionCorrectionStatusText == null) return;
            if (string.IsNullOrWhiteSpace(correctionToolId))
            {
                DockRoiPositionCorrectionStatusText.Text = "不跟随位置修正，运行时使用配方中的基础 ROI / 屏蔽区。";
                DockRoiPositionCorrectionStatusText.Foreground = System.Windows.Media.Brushes.SlateGray;
                return;
            }
            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, correctionToolId, StringComparison.OrdinalIgnoreCase));
            string error = GetPositionCorrectionBindingConfigurationError(target, correctionToolId, false);
            DockRoiPositionCorrectionStatusText.Text = string.IsNullOrWhiteSpace(error)
                ? "跟随 " + source.SequenceText + "  " + source.InstanceName + "；基础几何不变，运行时派生修正 ROI。"
                : error;
            DockRoiPositionCorrectionStatusText.Foreground = string.IsNullOrWhiteSpace(error)
                ? System.Windows.Media.Brushes.SeaGreen
                : System.Windows.Media.Brushes.Firebrick;
        }

        private string GetPositionCorrectionBindingConfigurationError(VmToolInstance target, string correctionToolId, bool requireExecuted)
        {
            if (string.IsNullOrWhiteSpace(correctionToolId)) return string.Empty;
            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, correctionToolId, StringComparison.OrdinalIgnoreCase));
            if (source == null) return "位置修正来源不存在；请选择有效的前序 2D 位置修正工具。";
            if (source.Kind != VmToolKind.PositionCorrection2D) return "位置修正来源类型无效；只允许 2D 位置修正工具。";
            if (!source.IsEnabled) return source.InstanceName + " 已停用，不能驱动 ROI 跟随。";
            int sourceIndex = flowTools.IndexOf(source);
            int targetIndex = target == null ? -1 : flowTools.IndexOf(target);
            if (targetIndex >= 0 && sourceIndex >= targetIndex) return "位置修正来源必须位于当前 ROI 工具之前。";
            if (!requireExecuted) return string.Empty;
            if (!string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                return source.InstanceName + " 尚无本周期 OK 位姿，请先运行上游位置修正。";
            object raw;
            HHomMat2D matrix = source.TryGetOutputValue("HomMat2DHandle", out raw) ? raw as HHomMat2D : null;
            if (matrix == null) return source.InstanceName + " 的本周期 HomMat2D 已失效，请重新运行。";
            return string.Empty;
        }

        private double ResolveRequiredFiniteNumber(VmToolInstance tool, string portName, string displayName)
        {
            object raw;
            if (!TryResolveOptionalToolInputValue(tool, portName, out raw) || raw == null)
                throw new InvalidOperationException("2D 位置修正必须连接 " + displayName + " 输入。");
            double value;
            try { value = Convert.ToDouble(raw, CultureInfo.InvariantCulture); }
            catch (Exception ex) { throw new InvalidOperationException(displayName + " 无法转换为数值：" + ex.Message, ex); }
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(displayName + " 必须是有限数值，不能为 NaN 或 Infinity。");
            return value;
        }

        private InspectionRecord RunPositionCorrection2DTool(VmToolInstance tool)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.PositionCorrection2D);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            string modeError = GetPositionCorrectionInputModeError(tool, true);
            if (!string.IsNullOrWhiteSpace(modeError)) throw new InvalidOperationException(modeError);
            VmPose2D pose = ResolvePositionCorrectionPose(tool);
            double currentRow = pose.Row;
            double currentColumn = pose.Column;
            double currentAngle = pose.AngleDegrees;
            if (Math.Abs(currentAngle) > 360000) throw new InvalidOperationException("当前角度必须在 -360000° 到 360000° 之间。");

            double deltaRow = currentRow - parameters.PositionCorrectionReferenceRow;
            double deltaColumn = currentColumn - parameters.PositionCorrectionReferenceColumn;
            double deltaAngle = NormalizeSignedAngleDegrees(currentAngle - parameters.PositionCorrectionReferenceAngleDegrees);
            HHomMat2D matrix = new HHomMat2D();
            matrix.VectorAngleToRigid(
                    parameters.PositionCorrectionReferenceRow,
                    parameters.PositionCorrectionReferenceColumn,
                    DegreesToRadians(parameters.PositionCorrectionReferenceAngleDegrees),
                    currentRow,
                    currentColumn,
                    DegreesToRadians(currentAngle));
                string matrixText = string.Join(",", matrix.RawData.ToDArr().Select(value => value.ToString("0.############", CultureInfo.InvariantCulture)));
                tool.SetOutputValue("ReferenceRow", parameters.PositionCorrectionReferenceRow);
                tool.SetOutputValue("ReferenceColumn", parameters.PositionCorrectionReferenceColumn);
                tool.SetOutputValue("ReferenceAngle", parameters.PositionCorrectionReferenceAngleDegrees);
                tool.SetOutputValue("CurrentRow", currentRow);
                tool.SetOutputValue("CurrentColumn", currentColumn);
                tool.SetOutputValue("CurrentAngle", currentAngle);
                tool.SetOutputValue("DeltaRow", deltaRow);
                tool.SetOutputValue("DeltaColumn", deltaColumn);
                tool.SetOutputValue("DeltaAngle", deltaAngle);
                tool.SetOutputValue("HomMat2D", matrixText);
                tool.SetOutputValue("HomMat2DHandle", matrix);
                matrix = null;
                tool.SetOutputValue("ResultCode", "OK");
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "2D 位置修正完成 · 基准=({0:0.###},{1:0.###},{2:0.###}°) · 当前=({3:0.###},{4:0.###},{5:0.###}°) · Δ=({6:0.###},{7:0.###},{8:0.###}°)",
                    parameters.PositionCorrectionReferenceRow, parameters.PositionCorrectionReferenceColumn, parameters.PositionCorrectionReferenceAngleDegrees,
                    currentRow, currentColumn, currentAngle, deltaRow, deltaColumn, deltaAngle);
                InspectionRecord record = CreateRecord("PositionCorrection2D", "OK", Math.Sqrt(deltaRow * deltaRow + deltaColumn * deltaColumn), message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
            return record;
        }

        private string BuildPositionCorrectionInputSummary(VmToolInstance tool)
        {
            VmToolInputBindingData poseBinding = tool == null ? null : tool.GetInputBinding("Pose2D");
            if (poseBinding != null)
            {
                VmToolInstance poseSource = GetInputSourceTool(poseBinding);
                return poseSource == null ? "Pose2D←无效来源" : "Pose2D←" + poseSource.InstanceName + "." + poseBinding.SourcePortName;
            }
            string[] names = { "CurrentRow", "CurrentColumn", "CurrentAngle" };
            List<string> parts = new List<string>();
            foreach (string name in names)
            {
                VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(name);
                VmToolInstance source = GetInputSourceTool(binding);
                parts.Add(binding == null || source == null ? name + "←未连接" : name + "←" + source.InstanceName + "." + binding.SourcePortName);
            }
            return string.Join(" + ", parts);
        }

        private string GetPositionCorrectionInputModeError(VmToolInstance tool, bool requireComplete)
        {
            if (tool == null || tool.Kind != VmToolKind.PositionCorrection2D) return string.Empty;
            bool hasPose = tool.GetInputBinding("Pose2D") != null;
            string[] legacyNames = { "CurrentRow", "CurrentColumn", "CurrentAngle" };
            int legacyCount = legacyNames.Count(name => tool.GetInputBinding(name) != null);
            if (hasPose && legacyCount > 0) return "2D 位置修正不能同时连接 Pose2D 与旧版 Row/Column/Angle；请选择一种输入方式。";
            if (!requireComplete) return string.Empty;
            if (hasPose) return string.Empty;
            if (legacyCount == 0) return "2D 位置修正必须连接一个 Pose2D，或完整连接旧版 CurrentRow、CurrentColumn、CurrentAngle。";
            if (legacyCount != legacyNames.Length) return "旧版位姿输入必须同时连接 CurrentRow、CurrentColumn、CurrentAngle，不能缺少字段。";
            return string.Empty;
        }

        private VmPose2D ResolvePositionCorrectionPose(VmToolInstance tool)
        {
            VmToolInputBindingData poseBinding = tool.GetInputBinding("Pose2D");
            if (poseBinding == null)
            {
                return VmPose2D.CreateImagePixel(
                    ResolveRequiredFiniteNumber(tool, "CurrentRow", "当前 Row"),
                    ResolveRequiredFiniteNumber(tool, "CurrentColumn", "当前 Column"),
                    ResolveRequiredFiniteNumber(tool, "CurrentAngle", "当前角度"),
                    "Legacy.NumberTriplet");
            }

            string bindingError = GetInputBindingConfigurationError(tool, poseBinding);
            if (!string.IsNullOrWhiteSpace(bindingError)) throw new InvalidOperationException("Pose2D：" + bindingError);
            VmToolInstance source = GetInputSourceTool(poseBinding);
            if (source == null || !string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Pose2D 来源尚无本周期 OK 结果，请先运行上游工具。");
            object raw;
            VmPose2D pose = source.TryGetOutputValue(poseBinding.SourcePortName, out raw) ? raw as VmPose2D : null;
            if (pose == null) throw new InvalidOperationException("Pose2D 来源没有可用的本周期类型化位姿，旧值不会被复用。");
            string poseError = pose.GetValidationError();
            if (!string.IsNullOrWhiteSpace(poseError)) throw new InvalidOperationException(poseError);
            if (!string.Equals(pose.CoordinateContext, VmPose2D.ImagePixelContext, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Pose2D 坐标上下文不匹配：2D 位置修正当前只接受同一图像像素坐标 ImagePixel，不接受标定、机器人或其他坐标。");
            if (Math.Abs(pose.AngleDegrees) > 360000) throw new InvalidOperationException("Pose2D 角度必须在 -360000° 到 360000° 之间。");
            return pose.Clone();
        }

        private HHomMat2D GetPositionCorrectionMatrixForTool(VmToolInstance tool)
        {
            string correctionToolId = tool == null || tool.Parameters == null ? string.Empty : tool.Parameters.PositionCorrectionToolId;
            if (string.IsNullOrWhiteSpace(correctionToolId)) return null;
            string error = GetPositionCorrectionBindingConfigurationError(tool, correctionToolId, true);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            VmToolInstance source = flowTools.First(item => string.Equals(item.ToolId, correctionToolId, StringComparison.OrdinalIgnoreCase));
            object raw;
            return source.TryGetOutputValue("HomMat2DHandle", out raw) ? raw as HHomMat2D : null;
        }

        private string AppendPositionCorrectionSummary(VmToolInstance tool, string roiSummary)
        {
            string correctionToolId = tool == null || tool.Parameters == null ? string.Empty : tool.Parameters.PositionCorrectionToolId;
            if (string.IsNullOrWhiteSpace(correctionToolId)) return roiSummary;
            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, correctionToolId, StringComparison.OrdinalIgnoreCase));
            return roiSummary + " · 位置修正←" + (source == null ? "无效来源" : source.InstanceName);
        }

        private HRegion CreatePositionCorrectedUnionRoiRegion(VmToolInstance tool, IList<VmRoiLayer> layers)
        {
            using (HRegion raw = CreateUnionRoiRegion(layers))
            {
                HHomMat2D matrix = GetPositionCorrectionMatrixForTool(tool);
                return matrix == null ? new HRegion(raw) : raw.AffineTransRegion(matrix, "nearest_neighbor");
            }
        }

        private HRegion CreatePositionCorrectedRoiRegion(VmToolInstance tool, HRegion region)
        {
            HHomMat2D matrix = GetPositionCorrectionMatrixForTool(tool);
            return matrix == null ? new HRegion(region) : region.AffineTransRegion(matrix, "nearest_neighbor");
        }

        private void DrawPositionCorrectedRoiPreview(HWindow window, bool isLeftViewport, bool allowInputCoordinateRoi, VmToolInstance selectedTool)
        {
            if (!allowInputCoordinateRoi || selectedTool == null || !ToolMetadata.SupportsRoi(selectedTool.Kind) || selectedTool.Parameters == null ||
                string.IsNullOrWhiteSpace(selectedTool.Parameters.PositionCorrectionToolId)) return;
            try
            {
                HHomMat2D matrix = GetPositionCorrectionMatrixForTool(selectedTool);
                if (matrix == null) return;
                foreach (VmRoiLayer layer in GetBoundRoiLayers(selectedTool).Where(item => item.IsVisible && item.Geometry != null && VmLayerViewportScope.Includes(item.ViewportScope, isLeftViewport)))
                {
                    using (HRegion corrected = layer.Geometry.Region.AffineTransRegion(matrix, "nearest_neighbor"))
                    {
                        window.SetDraw("margin");
                        window.SetLineWidth(3);
                        window.SetColor(layer.IsMask ? "orange" : "green");
                        window.DispObj(corrected);
                    }
                }
            }
            catch (HalconException) { }
            catch (InvalidOperationException) { }
        }

        private static double NormalizeSignedAngleDegrees(double angle)
        {
            double normalized = angle % 360.0;
            if (normalized >= 180.0) normalized -= 360.0;
            if (normalized < -180.0) normalized += 360.0;
            return normalized;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
