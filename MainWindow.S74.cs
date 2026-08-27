using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool flowTaskFocusModeS74 = true;
        private bool flowNavigatorDraggingS74;
        private double flowNavigatorDragOffsetS74;

        private void FlowConnectionDisplayMode_CheckedS74(object sender, RoutedEventArgs e)
        {
            if (FlowTaskFocusModeRadioS74 == null) return;
            flowTaskFocusModeS74 = FlowTaskFocusModeRadioS74.IsChecked == true;
            if (!uiReady) return;
            RefreshFlowGraphLayout();
            int visible = flowConnections.Count(item => item.IsVisible);
            HeaderStatusText.Text = flowTaskFocusModeS74
                ? string.Format(CultureInfo.InvariantCulture, "任务聚焦：当前显示 {0}/{1} 条真实依赖；全部连接仍完整保留。", visible, flowConnections.Count)
                : string.Format(CultureInfo.InvariantCulture, "全部依赖：当前显示 {0} 条真实连接。", flowConnections.Count);
        }

        private void FlowGraphViewport_ScrollChangedS74(object sender, ScrollChangedEventArgs e)
        {
            RefreshFlowNavigatorViewportS74();
        }

        private void RefreshFlowNavigatorViewportS74()
        {
            if (FlowNavigatorViewportThumbS74 == null || FlowNavigatorTrackS73 == null || FlowGraphViewport == null ||
                FlowNavigatorRailS73 == null || FlowNavigatorRailS73.Visibility != Visibility.Visible)
            {
                if (FlowNavigatorViewportThumbS74 != null) FlowNavigatorViewportThumbS74.Visibility = Visibility.Collapsed;
                return;
            }

            double trackHeight = FlowNavigatorTrackS73.ActualHeight;
            double extent = FlowGraphViewport.ExtentHeight;
            double viewport = FlowGraphViewport.ViewportHeight;
            if (trackHeight <= 20 || extent <= 0 || viewport <= 0 || extent <= viewport + 0.5)
            {
                FlowNavigatorViewportThumbS74.Visibility = Visibility.Collapsed;
                return;
            }

            const double trackInset = 8.0;
            double usable = Math.Max(1, trackHeight - trackInset * 2.0);
            double thumbHeight = Math.Max(18, Math.Min(usable, usable * viewport / extent));
            double travel = Math.Max(0, usable - thumbHeight);
            double ratio = FlowGraphViewport.ScrollableHeight <= 0 ? 0 : FlowGraphViewport.VerticalOffset / FlowGraphViewport.ScrollableHeight;
            double top = trackInset + Math.Max(0, Math.Min(1, ratio)) * travel;
            FlowNavigatorViewportThumbS74.Height = thumbHeight;
            Canvas.SetTop(FlowNavigatorViewportThumbS74, top);
            FlowNavigatorViewportThumbS74.ToolTip = string.Format(CultureInfo.InvariantCulture,
                "当前可见流程 {0:0}%–{1:0}% · 拖动或点击导航空白快速定位",
                100.0 * FlowGraphViewport.VerticalOffset / extent,
                100.0 * Math.Min(extent, FlowGraphViewport.VerticalOffset + viewport) / extent);
            FlowNavigatorViewportThumbS74.Visibility = Visibility.Visible;
        }

        private void FlowNavigatorTrack_PreviewMouseLeftButtonDownS74(object sender, MouseButtonEventArgs e)
        {
            if (FlowGraphViewport == null || FlowNavigatorTrackS73 == null || FlowNavigatorViewportThumbS74 == null ||
                FlowNavigatorViewportThumbS74.Visibility != Visibility.Visible) return;

            DependencyObject current = e.OriginalSource as DependencyObject;
            while (current != null && !ReferenceEquals(current, FlowNavigatorTrackS73))
            {
                if (current is Button) return;
                current = VisualTreeHelper.GetParent(current);
            }

            double y = e.GetPosition(FlowNavigatorTrackS73).Y;
            double top = Canvas.GetTop(FlowNavigatorViewportThumbS74);
            if (double.IsNaN(top)) top = 8;
            if (y >= top && y <= top + FlowNavigatorViewportThumbS74.ActualHeight)
            {
                flowNavigatorDragOffsetS74 = y - top;
            }
            else
            {
                flowNavigatorDragOffsetS74 = FlowNavigatorViewportThumbS74.ActualHeight / 2.0;
                ScrollFlowFromNavigatorS74(y);
            }
            flowNavigatorDraggingS74 = true;
            flowExecutionFollowPausedS72 = isFlowExecutionActive;
            Mouse.Capture(FlowNavigatorTrackS73);
            e.Handled = true;
        }

        private void FlowNavigatorTrack_PreviewMouseMoveS74(object sender, MouseEventArgs e)
        {
            if (!flowNavigatorDraggingS74 || e.LeftButton != MouseButtonState.Pressed) return;
            ScrollFlowFromNavigatorS74(e.GetPosition(FlowNavigatorTrackS73).Y);
            e.Handled = true;
        }

        private void FlowNavigatorTrack_PreviewMouseLeftButtonUpS74(object sender, MouseButtonEventArgs e)
        {
            if (!flowNavigatorDraggingS74) return;
            flowNavigatorDraggingS74 = false;
            if (Mouse.Captured == FlowNavigatorTrackS73) Mouse.Capture(null);
            e.Handled = true;
        }

        private void ScrollFlowFromNavigatorS74(double pointerY)
        {
            double trackHeight = FlowNavigatorTrackS73.ActualHeight;
            double thumbHeight = FlowNavigatorViewportThumbS74.ActualHeight;
            const double trackInset = 8.0;
            double travel = Math.Max(1, trackHeight - trackInset * 2.0 - thumbHeight);
            double requestedTop = pointerY - flowNavigatorDragOffsetS74;
            double ratio = Math.Max(0, Math.Min(1, (requestedTop - trackInset) / travel));
            FlowGraphViewport.ScrollToVerticalOffset(ratio * FlowGraphViewport.ScrollableHeight);
            RefreshFlowNavigatorViewportS74();
        }

        private void RefreshTypedPointSetEllipseFitConnectionStatusS74(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            bool ready = source != null && binding != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) &&
                         source.TryGetOutputValue(binding.SourcePortName, out raw) && raw is VmPoint2DArray;
            tool.ConfigurationStatus = binding == null ? "待连接点集" : (ready ? "就绪" : "等待上游");
            tool.ConnectionStatus = binding == null ? "Points 未连接" : (ready ? "Point2D[] 已连接 · 有值" : "Point2D[] 已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildTypedPointSetEllipseFitInputSummaryS74(tool) + " → Ellipse2D / Axis / Residual / XLD";
        }

        private string BuildTypedPointSetEllipseFitInputSummaryS74(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            return source == null || binding == null ? "Point2D[] ← 未连接" : source.InstanceName + "." + binding.SourcePortName + " (Point2D[])";
        }

        private InspectionRecord RunTypedPointSetEllipseFittingToolS74(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.TypedPointSetEllipseFittingMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            VmToolInputBindingData binding = tool.GetInputBinding("Points");
            if (binding == null) throw new InvalidOperationException("类型化点集椭圆拟合必须连接 Points (Point2D[])。");
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
                throw new InvalidOperationException("点集椭圆拟合只接受具备稳定 ContextId 的 ImagePixel 图像像素坐标。");

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

            double minRow = valid.Min(point => point.Row);
            double maxRow = valid.Max(point => point.Row);
            double minColumn = valid.Min(point => point.Column);
            double maxColumn = valid.Max(point => point.Column);
            double diagonal = Math.Sqrt((maxRow - minRow) * (maxRow - minRow) + (maxColumn - minColumn) * (maxColumn - minColumn));
            if (diagonal <= 1e-6) throw new InvalidOperationException("点集全部重合或跨度过小，不能拟合椭圆。");
            EnsurePointSetHasEllipseSpanS74(valid);

            string algorithm = VmXldEllipseFitAlgorithm.Normalize(parameters.XldEllipseFitAlgorithm);
            string algorithmText = VmXldEllipseFitAlgorithm.GetDisplayText(algorithm);
            double[] rows = valid.Select(point => point.Row).ToArray();
            double[] columns = valid.Select(point => point.Column).ToArray();
            HObject sourceContour = null;
            HObject fittedEllipse = null;
            HObject pointCrosses = null;
            HObject inputPath = null;
            HObject residualContour = null;
            HObject overlays = null;
            HTuple centerRows = null;
            HTuple centerColumns = null;
            HTuple phis = null;
            HTuple radius1s = null;
            HTuple radius2s = null;
            HTuple startPhis = null;
            HTuple endPhis = null;
            HTuple pointOrders = null;
            HTuple distanceMin = null;
            HTuple distanceMax = null;
            HTuple fittedRows = null;
            HTuple fittedColumns = null;
            HTuple rowTuple = null;
            HTuple columnTuple = null;
            try
            {
                rowTuple = new HTuple(rows);
                columnTuple = new HTuple(columns);
                HOperatorSet.GenContourPolygonXld(out sourceContour, rowTuple, columnTuple);
                fittedEllipse = imageService.FitXldEllipses(sourceContour, algorithm,
                    parameters.XldEllipseFitMaxNumPoints, parameters.XldEllipseFitMaxClosureDistance,
                    parameters.XldEllipseFitClippingEndPoints, parameters.XldEllipseFitVossTabSize,
                    parameters.XldEllipseFitIterations, parameters.XldEllipseFitClippingFactor,
                    out centerRows, out centerColumns, out phis, out radius1s, out radius2s,
                    out startPhis, out endPhis, out pointOrders);
                if (centerRows == null || centerRows.Length != 1 || radius1s == null || radius1s.Length != 1 || radius2s == null || radius2s.Length != 1)
                    throw new InvalidOperationException("HALCON fit_ellipse_contour_xld 未返回唯一有效椭圆。");

                VmEllipse2D mainEllipse = VmEllipse2D.CreateImagePixel(centerRows[0].D, centerColumns[0].D, phis[0].D, radius1s[0].D, radius2s[0].D, input.CoordinateContextId);
                string ellipseError = mainEllipse.GetValidationError();
                if (!string.IsNullOrWhiteSpace(ellipseError)) throw new InvalidOperationException("HALCON 拟合椭圆无效：" + ellipseError);
                if (mainEllipse.Radius1 > Math.Max(10, diagonal * 20) || mainEllipse.Radius2 / mainEllipse.Radius1 < 0.005)
                    throw new InvalidOperationException("点集接近共线或椭圆弧跨度不足，拟合半轴相对点集跨度异常。");

                HOperatorSet.DistancePc(fittedEllipse, rowTuple, columnTuple, out distanceMin, out distanceMax);
                if (distanceMin == null || distanceMin.Length != valid.Count)
                    throw new InvalidOperationException("HALCON distance_pc 未返回每个输入点的轮廓残差。");
                HOperatorSet.GetContourXld(fittedEllipse, out fittedRows, out fittedColumns);
                if (fittedRows == null || fittedRows.Length < 2)
                    throw new InvalidOperationException("拟合椭圆轮廓点不足，无法生成残差图形。");

                double sumResidual = 0;
                double maxResidual = 0;
                HOperatorSet.GenCrossContourXld(out pointCrosses, rowTuple, columnTuple, 7.0, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, pointCrosses);
                HOperatorSet.GenContourPolygonXld(out inputPath, rowTuple, columnTuple);
                AppendOverlayObject(ref overlays, inputPath);
                AppendOverlayObject(ref overlays, fittedEllipse);
                for (int index = 0; index < valid.Count; index++)
                {
                    double residual = distanceMin[index].D;
                    if (double.IsNaN(residual) || double.IsInfinity(residual) || residual < 0)
                        throw new InvalidOperationException("HALCON distance_pc 返回了无效残差。");
                    sumResidual += residual;
                    maxResidual = Math.Max(maxResidual, residual);
                    System.Windows.Point nearest = FindNearestContourPointS74(valid[index].Row, valid[index].Column, fittedRows, fittedColumns);
                    HOperatorSet.GenContourPolygonXld(out residualContour,
                        new HTuple(new[] { valid[index].Row, nearest.Y }),
                        new HTuple(new[] { valid[index].Column, nearest.X }));
                    AppendOverlayObject(ref overlays, residualContour);
                    DisposeObject(residualContour);
                    residualContour = null;
                }

                double meanResidual = sumResidual / valid.Count;
                string resultCode = rejectedCount == 0 ? "OK" : "NG";
                VmPoint2D centerPoint = VmPoint2D.CreateImagePixel(mainEllipse.CenterRow, mainEllipse.CenterColumn, input.CoordinateContextId);
                string parameterText = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=gen_contour_polygon_xld+fit_ellipse_contour_xld+distance_pc, MinPoints={0}, InvalidPolicy={1}, MaxPoints={2}, MaxClosure={3:0.###}, ClipEnds={4}, VossTable={5}, Iterations={6}, ClipFactor={7:0.###}",
                    parameters.PointSetMinimumCount, parameters.PointSetAbnormalPolicy, parameters.XldEllipseFitMaxNumPoints,
                    parameters.XldEllipseFitMaxClosureDistance, parameters.XldEllipseFitClippingEndPoints,
                    parameters.XldEllipseFitVossTabSize, parameters.XldEllipseFitIterations, parameters.XldEllipseFitClippingFactor);
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("MainEllipse", mainEllipse);
                tool.SetOutputValue("CenterPoint", centerPoint);
                tool.SetOutputValue("Phi", mainEllipse.PhiDegrees);
                tool.SetOutputValue("Radius1", mainEllipse.Radius1);
                tool.SetOutputValue("Radius2", mainEllipse.Radius2);
                tool.SetOutputValue("AxisRatio", mainEllipse.AxisRatio);
                tool.SetOutputValue("InputCount", (double)inputCount);
                tool.SetOutputValue("ValidCount", (double)valid.Count);
                tool.SetOutputValue("RejectedCount", (double)rejectedCount);
                tool.SetOutputValue("MeanResidual", meanResidual);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("CoordinateContext", input.CoordinateContext + ":" + input.CoordinateContextId);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayContours(overlays, tool, "XLD", valid.Count * 2 + 2, resultCode, mainEllipse.Radius1 + mainEllipse.Radius2);
                overlays = null;

                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 类型化点集椭圆拟合：{0}，输入={1}，有效={2}，拒绝={3}，中心=({4:0.###},{5:0.###})，方向={6:0.###}°，R1/R2={7:0.###}/{8:0.###}px，轴比={9:0.###}，平均/最大残差={10:0.###}/{11:0.###}px · {12}",
                    algorithmText, inputCount, valid.Count, rejectedCount, mainEllipse.CenterRow, mainEllipse.CenterColumn,
                    mainEllipse.PhiDegrees, mainEllipse.Radius1, mainEllipse.Radius2, mainEllipse.AxisRatio,
                    meanResidual, maxResidual, input.CoordinateContextId);
                InspectionRecord record = CreateRecord("TypedPointSetEllipseFittingMeasurement", resultCode, maxResidual, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (rowTuple != null) rowTuple.Dispose();
                if (columnTuple != null) columnTuple.Dispose();
                if (centerRows != null) centerRows.Dispose();
                if (centerColumns != null) centerColumns.Dispose();
                if (phis != null) phis.Dispose();
                if (radius1s != null) radius1s.Dispose();
                if (radius2s != null) radius2s.Dispose();
                if (startPhis != null) startPhis.Dispose();
                if (endPhis != null) endPhis.Dispose();
                if (pointOrders != null) pointOrders.Dispose();
                if (distanceMin != null) distanceMin.Dispose();
                if (distanceMax != null) distanceMax.Dispose();
                if (fittedRows != null) fittedRows.Dispose();
                if (fittedColumns != null) fittedColumns.Dispose();
                DisposeObject(residualContour);
                DisposeObject(inputPath);
                DisposeObject(pointCrosses);
                DisposeObject(fittedEllipse);
                DisposeObject(sourceContour);
                DisposeObject(overlays);
            }
        }

        private static void EnsurePointSetHasEllipseSpanS74(IList<VmPoint2D> points)
        {
            double meanRow = points.Average(item => item.Row);
            double meanColumn = points.Average(item => item.Column);
            double covarianceRow = 0;
            double covarianceColumn = 0;
            double covarianceCross = 0;
            foreach (VmPoint2D point in points)
            {
                double row = point.Row - meanRow;
                double column = point.Column - meanColumn;
                covarianceRow += row * row;
                covarianceColumn += column * column;
                covarianceCross += row * column;
            }
            double trace = covarianceRow + covarianceColumn;
            double discriminant = Math.Sqrt(Math.Max(0, (covarianceRow - covarianceColumn) * (covarianceRow - covarianceColumn) + 4 * covarianceCross * covarianceCross));
            double largest = (trace + discriminant) / 2.0;
            double smallest = (trace - discriminant) / 2.0;
            if (largest <= 1e-12 || smallest <= Math.Max(1e-10, largest * 1e-8))
                throw new InvalidOperationException("点集接近共线，缺少可稳定拟合椭圆的二维跨度。");
        }

        private static System.Windows.Point FindNearestContourPointS74(double row, double column, HTuple contourRows, HTuple contourColumns)
        {
            double bestDistanceSquared = double.PositiveInfinity;
            double bestRow = contourRows[0].D;
            double bestColumn = contourColumns[0].D;
            for (int index = 0; index < contourRows.Length - 1; index++)
            {
                double row1 = contourRows[index].D;
                double column1 = contourColumns[index].D;
                double row2 = contourRows[index + 1].D;
                double column2 = contourColumns[index + 1].D;
                double deltaRow = row2 - row1;
                double deltaColumn = column2 - column1;
                double denominator = deltaRow * deltaRow + deltaColumn * deltaColumn;
                double t = denominator <= 1e-20 ? 0 : ((row - row1) * deltaRow + (column - column1) * deltaColumn) / denominator;
                t = Math.Max(0, Math.Min(1, t));
                double candidateRow = row1 + t * deltaRow;
                double candidateColumn = column1 + t * deltaColumn;
                double distanceSquared = (row - candidateRow) * (row - candidateRow) + (column - candidateColumn) * (column - candidateColumn);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestRow = candidateRow;
                    bestColumn = candidateColumn;
                }
            }
            return new System.Windows.Point(bestColumn, bestRow);
        }
    }
}
