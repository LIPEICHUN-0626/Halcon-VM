using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<FlowNavigatorItemS73> flowNavigatorItemsS73 = new ObservableCollection<FlowNavigatorItemS73>();
        private bool flowViewportResizePendingS73;

        private double GetAutomaticFlowCanvasWidthS73()
        {
            double zoom = FlowZoomSlider == null || FlowZoomSlider.Value <= 0 ? 1.0 : FlowZoomSlider.Value;
            double viewportWidth = FlowGraphViewport == null ? 0 : FlowGraphViewport.ViewportWidth;
            if (viewportWidth <= 1 && FlowGraphViewport != null) viewportWidth = FlowGraphViewport.ActualWidth;
            if (viewportWidth <= 1) viewportWidth = 720;
            return Math.Max(390, Math.Floor((viewportWidth - 6) / zoom));
        }

        private void FlowGraphViewport_SizeChangedS73(object sender, SizeChangedEventArgs e)
        {
            ScheduleResponsiveFlowRefreshS73();
        }

        private void FlowNavigatorTrack_SizeChangedS73(object sender, SizeChangedEventArgs e)
        {
            RefreshFlowNavigatorS73();
        }

        private void ScheduleResponsiveFlowRefreshS73()
        {
            if (flowViewportResizePendingS73 || !uiReady) return;
            flowViewportResizePendingS73 = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                flowViewportResizePendingS73 = false;
                if (FlowGraphViewport != null && FlowGraphViewport.Visibility == Visibility.Visible)
                    RefreshFlowGraphLayout();
            }));
        }

        private void RefreshFlowNavigatorS73()
        {
            if (FlowNavigatorRailS73 == null || FlowNavigatorTrackS73 == null || FlowNavigatorItemsS73 == null) return;
            bool visible = FlowGraphViewport != null && FlowGraphViewport.Visibility == Visibility.Visible && flowTools.Count > 1;
            FlowNavigatorRailS73.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            flowNavigatorItemsS73.Clear();
            if (!visible)
            {
                RefreshFlowNavigatorViewportS74();
                return;
            }

            double height = Math.Max(30, FlowNavigatorTrackS73.ActualHeight);
            double usable = Math.Max(1, height - 18);
            VmToolInstance selected = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            for (int index = 0; index < flowTools.Count; index++)
            {
                VmToolInstance tool = flowTools[index];
                bool connectionBlocked = flowConnections.Any(item => string.Equals(item.TargetToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase) && string.Equals(item.StateKey, "Error", StringComparison.OrdinalIgnoreCase));
                bool failed = !string.IsNullOrWhiteSpace(tool.ErrorMessage) || string.Equals(tool.RunStatus, "失败", StringComparison.OrdinalIgnoreCase);
                bool ng = string.Equals(tool.ResultCode, "NG", StringComparison.OrdinalIgnoreCase);
                bool pointer = tool.IsExecutionPointer;
                bool lastExecuted = !string.IsNullOrWhiteSpace(debugLastExecutedToolId) && string.Equals(debugLastExecutedToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase);
                bool isSelected = ReferenceEquals(selected, tool);
                string state;
                string fill;
                string stroke;
                if (connectionBlocked || failed) { state = connectionBlocked ? "连接阻断" : "异常"; fill = "#DC2626"; stroke = "#7F1D1D"; }
                else if (pointer) { state = "下一执行"; fill = "#F59E0B"; stroke = "#B45309"; }
                else if (ng) { state = "NG"; fill = "#EF4444"; stroke = "#991B1B"; }
                else if (tool.IsBreakpoint) { state = "断点"; fill = "#8B5CF6"; stroke = "#5B21B6"; }
                else if (isSelected) { state = "已选中"; fill = "#2563EB"; stroke = "#1E3A8A"; }
                else if (lastExecuted) { state = "刚执行"; fill = "#FDBA74"; stroke = "#C2410C"; }
                else if (string.Equals(tool.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)) { state = "OK"; fill = "#86EFAC"; stroke = "#15803D"; }
                else { state = "未运行"; fill = "#E2E8F0"; stroke = "#94A3B8"; }

                double markerHeight = pointer || connectionBlocked || failed || isSelected ? 9 : 6;
                double ratio = flowTools.Count <= 1 ? 0.5 : (double)index / (flowTools.Count - 1);
                flowNavigatorItemsS73.Add(new FlowNavigatorItemS73
                {
                    ToolId = tool.ToolId,
                    Top = Math.Max(4, Math.Min(height - markerHeight - 4, 9 + ratio * Math.Max(1, usable - markerHeight))),
                    MarkerHeight = markerHeight,
                    Fill = CreateFrozenBrushS73(fill),
                    Stroke = CreateFrozenBrushS73(stroke),
                    ToolTip = string.Format(CultureInfo.InvariantCulture, "#{0} {1} · {2}\n点击定位真实流程节点", tool.SequenceText, tool.InstanceName, state)
                });
            }
            RefreshFlowNavigatorViewportS74();
        }

        private static SolidColorBrush CreateFrozenBrushS73(string value)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
            brush.Freeze();
            return brush;
        }

        private void FlowNavigatorMarker_ClickS73(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string toolId = button == null ? null : button.Tag as string;
            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
            if (tool == null) return;
            flowExecutionFollowPausedS72 = !tool.IsExecutionPointer && isFlowExecutionActive;
            FocusFlowGraphTool(tool.ToolId, "长流程导航已定位：#" + tool.SequenceText + " " + tool.InstanceName + "。");
            RefreshFlowNavigatorS73();
        }

        private void RefreshTypedPointSetCircleFitConnectionStatusS73(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            bool ready = source != null && binding != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) &&
                         source.TryGetOutputValue(binding.SourcePortName, out raw) && raw is VmPoint2DArray;
            tool.ConfigurationStatus = binding == null ? "待连接点集" : (ready ? "就绪" : "等待上游");
            tool.ConnectionStatus = binding == null ? "Points 未连接" : (ready ? "Point2D[] 已连接 · 有值" : "Point2D[] 已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildTypedPointSetCircleFitInputSummaryS73(tool) + " → Circle2D / Residual / XLD";
        }

        private string BuildTypedPointSetCircleFitInputSummaryS73(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            return source == null || binding == null ? "Point2D[] ← 未连接" : source.InstanceName + "." + binding.SourcePortName + " (Point2D[])";
        }

        private static double HalconPointPointDistanceS73(double row1, double column1, double row2, double column2)
        {
            HTuple distance = null;
            try
            {
                HOperatorSet.DistancePp(row1, column1, row2, column2, out distance);
                if (distance == null || distance.Length == 0) throw new InvalidOperationException("HALCON distance_pp 未返回点距。");
                return distance[0].D;
            }
            finally
            {
                if (distance != null) distance.Dispose();
            }
        }

        private InspectionRecord RunTypedPointSetCircleFittingToolS73(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.TypedPointSetCircleFittingMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            VmToolInputBindingData binding = tool.GetInputBinding("Points");
            if (binding == null) throw new InvalidOperationException("类型化点集圆拟合必须连接 Points (Point2D[])。");
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
                throw new InvalidOperationException("点集圆拟合只接受具备稳定 ContextId 的 ImagePixel 图像像素坐标。");

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
            if (diagonal <= 1e-6) throw new InvalidOperationException("点集全部重合或跨度过小，不能拟合圆。");

            string algorithm = VmXldCircleFitAlgorithm.Normalize(parameters.XldCircleFitAlgorithm);
            string algorithmText = VmXldCircleFitAlgorithm.GetDisplayText(algorithm);
            double[] rows = valid.Select(point => point.Row).ToArray();
            double[] columns = valid.Select(point => point.Column).ToArray();
            HObject sourceContour = null;
            HObject fittedCircle = null;
            HObject pointCrosses = null;
            HObject inputPath = null;
            HObject residualContour = null;
            HObject overlays = null;
            HTuple centerRows = null;
            HTuple centerColumns = null;
            HTuple radii = null;
            HTuple startPhis = null;
            HTuple endPhis = null;
            HTuple pointOrders = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out sourceContour, new HTuple(rows), new HTuple(columns));
                fittedCircle = imageService.FitXldCircles(sourceContour, algorithm,
                    parameters.XldCircleFitMaxNumPoints, parameters.XldCircleFitMaxClosureDistance,
                    parameters.XldCircleFitClippingEndPoints, parameters.XldCircleFitIterations,
                    parameters.XldCircleFitClippingFactor, out centerRows, out centerColumns, out radii,
                    out startPhis, out endPhis, out pointOrders);
                if (centerRows == null || centerRows.Length != 1 || radii == null || radii.Length != 1)
                    throw new InvalidOperationException("HALCON fit_circle_contour_xld 未返回唯一有效圆。");

                VmCircle2D mainCircle = VmCircle2D.CreateImagePixel(centerRows[0].D, centerColumns[0].D, radii[0].D, input.CoordinateContextId);
                string circleError = mainCircle.GetValidationError();
                if (!string.IsNullOrWhiteSpace(circleError)) throw new InvalidOperationException("HALCON 拟合圆无效：" + circleError);
                if (mainCircle.Radius > Math.Max(10, diagonal * 20))
                    throw new InvalidOperationException("点集接近共线或圆弧跨度不足，拟合半径相对点集跨度过大。");

                double sumResidual = 0;
                double maxResidual = 0;
                HOperatorSet.GenCrossContourXld(out pointCrosses, new HTuple(rows), new HTuple(columns), 7.0, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, pointCrosses);
                HOperatorSet.GenContourPolygonXld(out inputPath, new HTuple(rows), new HTuple(columns));
                AppendOverlayObject(ref overlays, inputPath);
                AppendOverlayObject(ref overlays, fittedCircle);
                foreach (VmPoint2D point in valid)
                {
                    double centerDistance = HalconPointPointDistanceS73(mainCircle.CenterRow, mainCircle.CenterColumn, point.Row, point.Column);
                    double residual = Math.Abs(centerDistance - mainCircle.Radius);
                    sumResidual += residual;
                    maxResidual = Math.Max(maxResidual, residual);
                    double nearestRow;
                    double nearestColumn;
                    if (centerDistance <= 1e-12)
                    {
                        nearestRow = mainCircle.CenterRow;
                        nearestColumn = mainCircle.CenterColumn + mainCircle.Radius;
                    }
                    else
                    {
                        double scale = mainCircle.Radius / centerDistance;
                        nearestRow = mainCircle.CenterRow + (point.Row - mainCircle.CenterRow) * scale;
                        nearestColumn = mainCircle.CenterColumn + (point.Column - mainCircle.CenterColumn) * scale;
                    }
                    HOperatorSet.GenContourPolygonXld(out residualContour,
                        new HTuple(new[] { point.Row, nearestRow }), new HTuple(new[] { point.Column, nearestColumn }));
                    AppendOverlayObject(ref overlays, residualContour);
                    DisposeObject(residualContour);
                    residualContour = null;
                }
                double meanResidual = sumResidual / valid.Count;
                string resultCode = rejectedCount == 0 ? "OK" : "NG";
                VmPoint2D centerPoint = VmPoint2D.CreateImagePixel(mainCircle.CenterRow, mainCircle.CenterColumn, input.CoordinateContextId);
                string parameterText = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=gen_contour_polygon_xld+fit_circle_contour_xld+distance_pp, MinPoints={0}, InvalidPolicy={1}, MaxPoints={2}, MaxClosure={3:0.###}, ClipEnds={4}, Iterations={5}, ClipFactor={6:0.###}",
                    parameters.PointSetMinimumCount, parameters.PointSetAbnormalPolicy, parameters.XldCircleFitMaxNumPoints,
                    parameters.XldCircleFitMaxClosureDistance, parameters.XldCircleFitClippingEndPoints,
                    parameters.XldCircleFitIterations, parameters.XldCircleFitClippingFactor);
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("MainCircle", mainCircle);
                tool.SetOutputValue("CenterPoint", centerPoint);
                tool.SetOutputValue("Radius", mainCircle.Radius);
                tool.SetOutputValue("InputCount", (double)inputCount);
                tool.SetOutputValue("ValidCount", (double)valid.Count);
                tool.SetOutputValue("RejectedCount", (double)rejectedCount);
                tool.SetOutputValue("MeanResidual", meanResidual);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("CoordinateContext", input.CoordinateContext + ":" + input.CoordinateContextId);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayContours(overlays, tool, "XLD", valid.Count * 2 + 2, resultCode, mainCircle.Radius);
                overlays = null;

                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 类型化点集圆拟合：{0}，输入={1}，有效={2}，拒绝={3}，圆心=({4:0.###},{5:0.###})，半径={6:0.###}px，平均/最大径向残差={7:0.###}/{8:0.###}px · {9}",
                    algorithmText, inputCount, valid.Count, rejectedCount, mainCircle.CenterRow, mainCircle.CenterColumn,
                    mainCircle.Radius, meanResidual, maxResidual, input.CoordinateContextId);
                InspectionRecord record = CreateRecord("TypedPointSetCircleFittingMeasurement", resultCode, maxResidual, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (centerRows != null) centerRows.Dispose();
                if (centerColumns != null) centerColumns.Dispose();
                if (radii != null) radii.Dispose();
                if (startPhis != null) startPhis.Dispose();
                if (endPhis != null) endPhis.Dispose();
                if (pointOrders != null) pointOrders.Dispose();
                DisposeObject(residualContour);
                DisposeObject(inputPath);
                DisposeObject(pointCrosses);
                DisposeObject(fittedCircle);
                DisposeObject(sourceContour);
                DisposeObject(overlays);
            }
        }

        private sealed class FlowNavigatorItemS73
        {
            public string ToolId { get; set; }
            public double Top { get; set; }
            public double MarkerHeight { get; set; }
            public Brush Fill { get; set; }
            public Brush Stroke { get; set; }
            public string ToolTip { get; set; }
        }
    }
}
