using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool flowExecutionFollowPausedS72;

        private void ResetFlowExecutionFollowS72()
        {
            flowExecutionFollowPausedS72 = false;
        }

        private void PauseFlowExecutionFollowS72(string status)
        {
            if (!isFlowExecutionActive) return;
            flowExecutionFollowPausedS72 = true;
            if (!string.IsNullOrWhiteSpace(status) && HeaderStatusText != null) HeaderStatusText.Text = status;
        }

        private void FollowFlowExecutionToolS72(VmToolInstance tool)
        {
            if (tool == null || flowExecutionFollowPausedS72 || FlowGraphViewport == null ||
                FlowGraphViewport.Visibility != Visibility.Visible || FlowZoomSlider == null) return;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                if (flowExecutionFollowPausedS72 || tool == null || !flowTools.Contains(tool)) return;
                double zoom = FlowZoomSlider.Value;
                FlowGraphViewport.ScrollToHorizontalOffset(Math.Max(0, (tool.GraphX + 140) * zoom - FlowGraphViewport.ViewportWidth * 0.5));
                FlowGraphViewport.ScrollToVerticalOffset(Math.Max(0, (tool.GraphY + tool.GraphHeight * 0.5) * zoom - FlowGraphViewport.ViewportHeight * 0.45));
            }));
        }

        private void FlowGraphViewport_PreviewMouseWheelS72(object sender, MouseWheelEventArgs e)
        {
            PauseFlowExecutionFollowS72("用户已滚动流程画布，执行跟随暂停；Alt+Enter 可恢复并定位当前步骤。");
        }

        private void PreviousFlowToolMenuItem_ClickS72(object sender, RoutedEventArgs e)
        {
            SelectAdjacentFlowToolS72(-1, "上下文菜单");
        }

        private void NextFlowToolMenuItem_ClickS72(object sender, RoutedEventArgs e)
        {
            SelectAdjacentFlowToolS72(1, "上下文菜单");
        }

        private void LocateExecutionPointerMenuItem_ClickS72(object sender, RoutedEventArgs e)
        {
            LocateExecutionPointerS72();
        }

        private void SelectAdjacentFlowToolS72(int direction, string source)
        {
            if (flowTools.Count == 0)
            {
                HeaderStatusText.Text = "当前流程没有模块。";
                return;
            }

            VmToolInstance selected = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            int index = selected == null ? (direction >= 0 ? 0 : flowTools.Count - 1) : flowTools.IndexOf(selected) + Math.Sign(direction);
            index = Math.Max(0, Math.Min(flowTools.Count - 1, index));
            VmToolInstance target = flowTools[index];
            flowExecutionFollowPausedS72 = isFlowExecutionActive && !target.IsExecutionPointer;
            FocusFlowGraphTool(target.ToolId, string.Format(CultureInfo.InvariantCulture,
                "{0}已定位 {1}/{2}：{3}；Alt+↑/↓ 可继续纵向检查。",
                source, index + 1, flowTools.Count, target.InstanceName));
        }

        private void LocateExecutionPointerS72()
        {
            VmToolInstance target = flowTools.FirstOrDefault(item => item.IsExecutionPointer);
            if (target == null && !string.IsNullOrWhiteSpace(debugPausedToolId))
                target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, debugPausedToolId, StringComparison.OrdinalIgnoreCase));
            if (target == null && !string.IsNullOrWhiteSpace(debugLastExecutedToolId))
                target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, debugLastExecutedToolId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                HeaderStatusText.Text = "当前没有执行指针；运行、单步或命中断点后可用 Alt+Enter 定位。";
                return;
            }

            flowExecutionFollowPausedS72 = false;
            FocusFlowGraphTool(target.ToolId, "已恢复执行跟随并定位：#" + target.SequenceText + " " + target.InstanceName + "。");
            FollowFlowExecutionToolS72(target);
        }

        private void RefreshTypedPointSetLineFitConnectionStatusS72(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            bool ready = source != null && binding != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) &&
                         source.TryGetOutputValue(binding.SourcePortName, out raw) && raw is VmPoint2DArray;
            tool.ConfigurationStatus = binding == null ? "待连接点集" : (ready ? "就绪" : "等待上游");
            tool.ConnectionStatus = binding == null ? "Points 未连接" : (ready ? "Point2D[] 已连接 · 有值" : "Point2D[] 已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildTypedPointSetLineFitInputSummaryS72(tool) + " → Line2D / Residual / XLD";
        }

        private string BuildTypedPointSetLineFitInputSummaryS72(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            return source == null || binding == null ? "Point2D[] ← 未连接" : source.InstanceName + "." + binding.SourcePortName + " (Point2D[])";
        }

        private InspectionRecord RunTypedPointSetLineFittingToolS72(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.TypedPointSetLineFittingMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            VmToolInputBindingData binding = tool.GetInputBinding("Points");
            if (binding == null) throw new InvalidOperationException("类型化点集直线拟合必须连接 Points (Point2D[])。");
            string bindingError = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(bindingError)) throw new InvalidOperationException(bindingError);
            VmToolInstance source = GetInputSourceTool(binding);
            if (source == null || !string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("点集上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object raw;
            if (!source.TryGetOutputValue(binding.SourcePortName, out raw) || !(raw is VmPoint2DArray))
                throw new InvalidOperationException("Points 输入没有类型化 Point2D[] 运行值；不会解析字符串坐标。");

            VmPoint2DArray input = ((VmPoint2DArray)raw).Clone();
            if (!input.IsValid) throw new InvalidOperationException("Point2D[] 已标记为无效。");
            if (!string.Equals(input.CoordinateContext, VmPoint2D.ImagePixelContext, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(input.CoordinateContextId))
                throw new InvalidOperationException("点集直线拟合只接受具备稳定 ContextId 的 ImagePixel 图像像素坐标。");

            bool skipInvalid = string.Equals(parameters.PointSetAbnormalPolicy, VmPointSetAbnormalPolicy.SkipInvalid, StringComparison.OrdinalIgnoreCase);
            int inputCount = input.Points == null ? 0 : input.Points.Count;
            int rejectedCount = 0;
            List<VmPoint2D> valid = new List<VmPoint2D>();
            foreach (VmPoint2D point in input.Points ?? new List<VmPoint2D>())
            {
                if (point != null && (!string.Equals(point.CoordinateContext, input.CoordinateContext, StringComparison.Ordinal) ||
                                      !string.Equals(point.CoordinateContextId, input.CoordinateContextId, StringComparison.Ordinal)))
                    throw new InvalidOperationException("Point2D[] 内部坐标上下文不一致，不能跨图像拟合。");
                string pointError = point == null ? "点为空" : point.GetValidationError();
                if (!string.IsNullOrWhiteSpace(pointError))
                {
                    if (!skipInvalid) throw new InvalidOperationException("Point2D[] 包含异常点：" + pointError);
                    rejectedCount++;
                    continue;
                }
                valid.Add(point.Clone());
            }
            if (valid.Count < parameters.PointSetMinimumCount)
                throw new InvalidOperationException("有效点数量 " + valid.Count.ToString(CultureInfo.InvariantCulture) + " 小于配置的最小点数 " + parameters.PointSetMinimumCount.ToString(CultureInfo.InvariantCulture) + "。");

            string algorithm = VmXldLineFitAlgorithm.Normalize(parameters.XldLineFitAlgorithm);
            string algorithmText = VmXldLineFitAlgorithm.GetDisplayText(algorithm);
            double[] rows = valid.Select(point => point.Row).ToArray();
            double[] columns = valid.Select(point => point.Column).ToArray();
            HObject sourceContour = null;
            HObject fittedLine = null;
            HObject pointCrosses = null;
            HObject inputPath = null;
            HObject residualContour = null;
            HObject overlays = null;
            HTuple rowBegin = null;
            HTuple columnBegin = null;
            HTuple rowEnd = null;
            HTuple columnEnd = null;
            HTuple normalRow = null;
            HTuple normalColumn = null;
            HTuple distance = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out sourceContour, new HTuple(rows), new HTuple(columns));
                fittedLine = imageService.FitXldLines(sourceContour, algorithm,
                    parameters.XldLineFitMaxNumPoints, parameters.XldLineFitClippingEndPoints,
                    parameters.XldLineFitIterations, parameters.XldLineFitClippingFactor,
                    out rowBegin, out columnBegin, out rowEnd, out columnEnd, out normalRow, out normalColumn, out distance);
                if (rowBegin == null || rowBegin.Length != 1 || rowEnd == null || rowEnd.Length != 1)
                    throw new InvalidOperationException("HALCON fit_line_contour_xld 未返回唯一有效直线。");

                VmLine2D mainLine = VmLine2D.CreateImagePixel(rowBegin[0].D, columnBegin[0].D, rowEnd[0].D, columnEnd[0].D, input.CoordinateContextId);
                string lineError = mainLine.GetValidationError();
                if (!string.IsNullOrWhiteSpace(lineError)) throw new InvalidOperationException("HALCON 拟合直线无效：" + lineError);

                double sumResidual = 0.0;
                double maxResidual = 0.0;
                foreach (VmPoint2D point in valid)
                {
                    double residual = HalconPointLineDistanceS71(point.Row, point.Column, mainLine);
                    sumResidual += residual;
                    maxResidual = Math.Max(maxResidual, residual);
                }
                double meanResidual = sumResidual / valid.Count;

                HOperatorSet.GenCrossContourXld(out pointCrosses, new HTuple(rows), new HTuple(columns), 7.0, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, pointCrosses);
                HOperatorSet.GenContourPolygonXld(out inputPath, new HTuple(rows), new HTuple(columns));
                AppendOverlayObject(ref overlays, inputPath);
                AppendOverlayObject(ref overlays, fittedLine);

                double vectorRow = mainLine.EndRow - mainLine.StartRow;
                double vectorColumn = mainLine.EndColumn - mainLine.StartColumn;
                double lengthSquared = vectorRow * vectorRow + vectorColumn * vectorColumn;
                foreach (VmPoint2D point in valid)
                {
                    double scale = ((point.Row - mainLine.StartRow) * vectorRow + (point.Column - mainLine.StartColumn) * vectorColumn) / lengthSquared;
                    double footRow = mainLine.StartRow + scale * vectorRow;
                    double footColumn = mainLine.StartColumn + scale * vectorColumn;
                    HOperatorSet.GenContourPolygonXld(out residualContour,
                        new HTuple(new[] { point.Row, footRow }), new HTuple(new[] { point.Column, footColumn }));
                    AppendOverlayObject(ref overlays, residualContour);
                    DisposeObject(residualContour);
                    residualContour = null;
                }

                string resultCode = rejectedCount == 0 ? "OK" : "NG";
                VmPoint2D startPoint = VmPoint2D.CreateImagePixel(mainLine.StartRow, mainLine.StartColumn, input.CoordinateContextId);
                VmPoint2D endPoint = VmPoint2D.CreateImagePixel(mainLine.EndRow, mainLine.EndColumn, input.CoordinateContextId);
                string parameterText = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=gen_contour_polygon_xld+fit_line_contour_xld+distance_pl, MinPoints={0}, InvalidPolicy={1}, MaxPoints={2}, ClipEnds={3}, Iterations={4}, ClipFactor={5:0.###}",
                    parameters.PointSetMinimumCount, parameters.PointSetAbnormalPolicy, parameters.XldLineFitMaxNumPoints,
                    parameters.XldLineFitClippingEndPoints, parameters.XldLineFitIterations, parameters.XldLineFitClippingFactor);
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("MainLine", mainLine);
                tool.SetOutputValue("StartPoint", startPoint);
                tool.SetOutputValue("EndPoint", endPoint);
                tool.SetOutputValue("Angle", mainLine.DirectionAngleDegrees);
                tool.SetOutputValue("InputCount", (double)inputCount);
                tool.SetOutputValue("ValidCount", (double)valid.Count);
                tool.SetOutputValue("RejectedCount", (double)rejectedCount);
                tool.SetOutputValue("MeanResidual", meanResidual);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("CoordinateContext", input.CoordinateContext + ":" + input.CoordinateContextId);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayContours(overlays, tool, "XLD", valid.Count * 2 + 2, resultCode, mainLine.Length);
                overlays = null;

                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 类型化点集直线拟合：{0}，输入={1}，有效={2}，拒绝={3}，角度={4:0.###}°，平均/最大残差={5:0.###}/{6:0.###}px，主线 ({7:0.###},{8:0.###})→({9:0.###},{10:0.###}) · {11}",
                    algorithmText, inputCount, valid.Count, rejectedCount, mainLine.DirectionAngleDegrees, meanResidual, maxResidual,
                    mainLine.StartRow, mainLine.StartColumn, mainLine.EndRow, mainLine.EndColumn, input.CoordinateContextId);
                InspectionRecord record = CreateRecord("TypedPointSetLineFittingMeasurement", resultCode, maxResidual, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (rowBegin != null) rowBegin.Dispose();
                if (columnBegin != null) columnBegin.Dispose();
                if (rowEnd != null) rowEnd.Dispose();
                if (columnEnd != null) columnEnd.Dispose();
                if (normalRow != null) normalRow.Dispose();
                if (normalColumn != null) normalColumn.Dispose();
                if (distance != null) distance.Dispose();
                DisposeObject(residualContour);
                DisposeObject(inputPath);
                DisposeObject(pointCrosses);
                DisposeObject(fittedLine);
                DisposeObject(sourceContour);
                DisposeObject(overlays);
            }
        }
    }
}
