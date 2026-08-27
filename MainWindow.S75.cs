using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<VmFlowPortChip> aggregatePortFlyoutItemsS75 = new ObservableCollection<VmFlowPortChip>();
        private VmToolInstance aggregatePortFlyoutToolS75;
        private string aggregatePortFlyoutDirectionS75;
        private bool aggregatePortHoverDetectedS75;
        private string aggregatePortHoverErrorS75 = string.Empty;

        private void FlowAggregateAnchor_MouseLeftButtonDownS75(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement anchor = sender as FrameworkElement;
            VmToolInstance tool = anchor == null ? null : anchor.Tag as VmToolInstance;
            if (tool == null || isContinuousRunning || isFlowExecutionActive) return;
            string direction = anchor.Uid != null && anchor.Uid.EndsWith("IN", StringComparison.OrdinalIgnoreCase) ? "IN" : "OUT";
            OpenFlowAggregatePortFlyoutS75(tool, direction);
            List<VmFlowPortChip> ports = (string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase) ? tool.FlowInputPorts : tool.FlowOutputPorts).ToList();
            if (ports.Count == 1)
            {
                SelectAggregatePortS75(ports[0], anchor, e.GetPosition(FlowGraphCanvas), true);
            }
            else
            {
                HeaderStatusText.Text = ports.Count == 0
                    ? tool.InstanceName + " 没有可用的 " + direction + " 端口。"
                    : tool.InstanceName + " 有 " + ports.Count.ToString(CultureInfo.InvariantCulture) + " 个 " + direction + " 端口；请在连接点旁选择精确端口。";
            }
            e.Handled = true;
        }

        private void OpenFlowAggregatePortFlyoutS75(VmToolInstance tool, string direction)
        {
            if (tool == null || FlowAggregatePortFlyoutS75 == null) return;
            aggregatePortFlyoutToolS75 = tool;
            aggregatePortFlyoutDirectionS75 = direction;
            aggregatePortFlyoutItemsS75.Clear();
            IEnumerable<VmFlowPortChip> ports = string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase)
                ? tool.FlowInputPorts
                : tool.FlowOutputPorts;
            foreach (VmFlowPortChip port in ports) aggregatePortFlyoutItemsS75.Add(port);
            FlowAggregatePortTitleTextS75.Text = tool.InstanceName + " · " + direction;
            FlowAggregatePortSummaryTextS75.Text = string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase)
                ? "输入聚合点：显示当前来源；点击精确端口可改来源或从已连接端口重连。"
                : "输出聚合点：显示真实输出及消费者；点击精确端口后拖到下游输入点。";
            FlowAggregatePortHintTextS75.Text = aggregatePortFlyoutItemsS75.Count == 0
                ? "当前模块没有该方向端口。"
                : (aggregatePortFlyoutItemsS75.Count == 1 ? "单端口可从圆点直接拖接；点击端口可打开精确订阅信息。" : "多端口必须先选择精确端口，避免静默绑定错误数据。 ");

            double left = tool.GraphX + 292;
            if (FlowGraphCanvas != null && left + 292 > FlowGraphCanvas.Width - 10) left = Math.Max(8, tool.GraphX - 304);
            double top = string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(8, tool.GraphY - Math.Min(220, 62 + aggregatePortFlyoutItemsS75.Count * 48))
                : tool.GraphY + tool.GraphHeight + 8;
            if (FlowGraphCanvas != null) top = Math.Max(8, Math.Min(Math.Max(8, FlowGraphCanvas.Height - 190), top));
            Canvas.SetLeft(FlowAggregatePortFlyoutS75, left);
            Canvas.SetTop(FlowAggregatePortFlyoutS75, top);
            FlowAggregatePortFlyoutS75.Visibility = Visibility.Visible;
        }

        private void CloseFlowAggregatePortFlyoutButton_ClickS75(object sender, RoutedEventArgs e)
        {
            CloseFlowAggregatePortFlyoutS75();
        }

        private void CloseFlowAggregatePortFlyoutS75()
        {
            if (FlowAggregatePortFlyoutS75 != null) FlowAggregatePortFlyoutS75.Visibility = Visibility.Collapsed;
            aggregatePortFlyoutToolS75 = null;
            aggregatePortFlyoutDirectionS75 = null;
            aggregatePortFlyoutItemsS75.Clear();
        }

        private void FlowAggregatePortChoice_MouseLeftButtonDownS75(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement row = sender as FrameworkElement;
            VmFlowPortChip chip = row == null ? null : row.Tag as VmFlowPortChip;
            if (chip == null) return;
            SelectAggregatePortS75(chip, row, e.GetPosition(FlowGraphCanvas), true);
            e.Handled = true;
        }

        private void SelectAggregatePortS75(VmFlowPortChip chip, FrameworkElement dragOrigin, System.Windows.Point startPoint, bool allowDrag)
        {
            VmToolInstance owner = chip == null ? null : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, chip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            if (chip == null || owner == null) return;
            selectedFlowConnectionKey = null;
            selectedFlowPortToolId = chip.OwnerToolId;
            selectedFlowPortName = chip.PortName;
            selectedFlowPortDirection = chip.Direction;
            FlowToolList.SelectedItem = owner;
            RefreshFlowPortVisualization();
            bool started = allowDrag && BeginFlowPortDrag(chip, dragOrigin, startPoint);
            if (string.Equals(chip.Direction, "IN", StringComparison.OrdinalIgnoreCase)) EnterInputSourceContextS68(chip.OwnerToolId, chip.PortName);
            else EnterToolRecommendationContextS67(chip.OwnerToolId, chip.PortName);
            HeaderStatusText.Text = started
                ? (string.Equals(chip.Direction, "IN", StringComparison.OrdinalIgnoreCase)
                    ? "正在从聚合输入点重连 " + owner.InstanceName + "." + chip.PortName + "；拖到兼容 OUT。"
                    : "正在从聚合输出点连接 " + owner.InstanceName + "." + chip.PortName + "；拖到兼容 IN。")
                : (string.Equals(chip.Direction, "IN", StringComparison.OrdinalIgnoreCase)
                    ? "已选择精确输入端口；可在端口订阅面替换来源，或用行内清除取消订阅。"
                    : "已选择精确输出端口；可查看消费者或按住拖向下游输入连接点。 ");
        }

        private void FlowAggregatePortClearButton_MouseLeftButtonDownS75(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement button = sender as FrameworkElement;
            VmFlowPortChip chip = button == null ? null : button.Tag as VmFlowPortChip;
            VmToolInstance target = chip == null ? null : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, chip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition port = target == null ? null : ToolMetadata.GetInstanceInputPorts(target).FirstOrDefault(item => string.Equals(item.PortName, chip.PortName, StringComparison.OrdinalIgnoreCase));
            if (target != null && port != null)
            {
                selectedFlowPortToolId = target.ToolId;
                selectedFlowPortName = port.PortName;
                selectedFlowPortDirection = "IN";
                FlowToolList.SelectedItem = target;
                if (target.GetInputBinding(port.PortName) == null) HeaderStatusText.Text = target.InstanceName + "." + port.PortName + " 当前没有订阅。";
                else ClearSelectedFlowPortBinding(target, port, "聚合输入点已清除订阅");
                CloseFlowAggregatePortFlyoutS75();
            }
            e.Handled = true;
        }

        private VmFlowPortChip ResolveAggregateHoverPortS75(VmToolInstance owner, string direction, out string error)
        {
            error = string.Empty;
            if (owner == null || flowPortDragSession == null)
            {
                error = "连接点已失效。";
                return null;
            }
            if (flowPortDragSession.FixedTargetPort == null && !string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase))
            {
                error = "从 OUT 开始连线时，请拖到模块顶部的输入连接点。";
                return null;
            }
            if (flowPortDragSession.FixedTargetPort != null && !string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase))
            {
                error = "重连输入来源时，请拖到模块底部的输出连接点。";
                return null;
            }

            List<VmFlowPortChip> candidates = new List<VmFlowPortChip>();
            List<string> reasons = new List<string>();
            IEnumerable<VmFlowPortChip> ports = string.Equals(direction, "IN", StringComparison.OrdinalIgnoreCase) ? owner.FlowInputPorts : owner.FlowOutputPorts;
            foreach (VmFlowPortChip port in ports)
            {
                VmFlowPortChip source = flowPortDragSession.FixedTargetPort == null ? flowPortDragSession.SourcePort : port;
                VmFlowPortChip target = flowPortDragSession.FixedTargetPort == null ? port : flowPortDragSession.FixedTargetPort;
                string reason;
                if (EvaluateFlowPortConnection(source, target, out reason)) candidates.Add(port);
                else if (!string.IsNullOrWhiteSpace(reason)) reasons.Add(reason);
            }
            if (candidates.Count == 1) return candidates[0];
            if (candidates.Count > 1)
            {
                error = owner.InstanceName + " 有 " + candidates.Count.ToString(CultureInfo.InvariantCulture) + " 个兼容 " + direction + " 端口；请点击圆点并选择精确端口。";
                return null;
            }
            error = reasons.FirstOrDefault() ?? (owner.InstanceName + " 没有兼容的 " + direction + " 端口。");
            return null;
        }

        private void RefreshPointToEllipseConnectionStatusS75(VmToolInstance tool)
        {
            VmToolInputBindingData pointBinding = tool == null ? null : tool.GetInputBinding("Point");
            VmToolInputBindingData ellipseBinding = tool == null ? null : tool.GetInputBinding("Ellipse");
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance ellipseSource = GetInputSourceTool(ellipseBinding);
            object pointRaw;
            object ellipseRaw;
            bool pointReady = pointSource != null && pointBinding != null && string.Equals(pointSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && pointSource.TryGetOutputValue(pointBinding.SourcePortName, out pointRaw) && pointRaw is VmPoint2D;
            bool ellipseReady = ellipseSource != null && ellipseBinding != null && string.Equals(ellipseSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && ellipseSource.TryGetOutputValue(ellipseBinding.SourcePortName, out ellipseRaw) && ellipseRaw is VmEllipse2D;
            tool.ConfigurationStatus = pointBinding == null || ellipseBinding == null ? "待连接几何" : (pointReady && ellipseReady ? "就绪" : "等待上游");
            tool.ConnectionStatus = pointBinding == null || ellipseBinding == null ? "Point 与 Ellipse 必须连接" : (pointReady && ellipseReady ? "Point2D + Ellipse2D 已连接 · 有值" : "几何输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildPointToEllipseInputSummaryS75(tool) + " → ClosestPoint / Distance / Relation / XLD";
        }

        private string BuildPointToEllipseInputSummaryS75(VmToolInstance tool)
        {
            VmToolInputBindingData pointBinding = tool == null ? null : tool.GetInputBinding("Point");
            VmToolInputBindingData ellipseBinding = tool == null ? null : tool.GetInputBinding("Ellipse");
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance ellipseSource = GetInputSourceTool(ellipseBinding);
            string point = pointSource == null || pointBinding == null ? "Point2D←未连接" : pointSource.InstanceName + "." + pointBinding.SourcePortName;
            string ellipse = ellipseSource == null || ellipseBinding == null ? "Ellipse2D←未连接" : ellipseSource.InstanceName + "." + ellipseBinding.SourcePortName;
            return point + " + " + ellipse;
        }

        private InspectionRecord RunPointToEllipseDistanceToolS75(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.PointToEllipseDistanceMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData pointBinding = tool.GetInputBinding("Point");
            VmToolInputBindingData ellipseBinding = tool.GetInputBinding("Ellipse");
            if (pointBinding == null || ellipseBinding == null) throw new InvalidOperationException("点到椭圆测量必须连接 Point (Point2D) 与 Ellipse (Ellipse2D)。");
            string pointBindingError = GetInputBindingConfigurationError(tool, pointBinding);
            string ellipseBindingError = GetInputBindingConfigurationError(tool, ellipseBinding);
            if (!string.IsNullOrWhiteSpace(pointBindingError)) throw new InvalidOperationException(pointBindingError);
            if (!string.IsNullOrWhiteSpace(ellipseBindingError)) throw new InvalidOperationException(ellipseBindingError);
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance ellipseSource = GetInputSourceTool(ellipseBinding);
            if (pointSource == null || ellipseSource == null || !string.Equals(pointSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(ellipseSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Point2D 或 Ellipse2D 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object pointRaw;
            object ellipseRaw;
            if (!pointSource.TryGetOutputValue(pointBinding.SourcePortName, out pointRaw) || !(pointRaw is VmPoint2D)) throw new InvalidOperationException("Point 输入没有类型化 Point2D 运行值。");
            if (!ellipseSource.TryGetOutputValue(ellipseBinding.SourcePortName, out ellipseRaw) || !(ellipseRaw is VmEllipse2D)) throw new InvalidOperationException("Ellipse 输入没有类型化 Ellipse2D 运行值；不会拼接中心、角度和半轴标量。");
            VmPoint2D point = ((VmPoint2D)pointRaw).Clone();
            VmEllipse2D ellipse = ((VmEllipse2D)ellipseRaw).Clone();
            string pointError = point.GetValidationError();
            string ellipseError = ellipse.GetValidationError();
            if (!string.IsNullOrWhiteSpace(pointError)) throw new InvalidOperationException(pointError);
            if (!string.IsNullOrWhiteSpace(ellipseError)) throw new InvalidOperationException(ellipseError);
            if (!string.Equals(point.CoordinateContext, ellipse.CoordinateContext, StringComparison.Ordinal) || !string.Equals(point.CoordinateContextId, ellipse.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("Point2D 与 Ellipse2D 坐标上下文不一致，禁止跨图像测量。");

            HObject overlays = null;
            HObject ellipseContour = null;
            HObject measurementLine = null;
            HObject crosses = null;
            HTuple distanceMin = null;
            HTuple distanceMax = null;
            HTuple contourRows = null;
            HTuple contourColumns = null;
            try
            {
                HOperatorSet.GenEllipseContourXld(out ellipseContour, ellipse.CenterRow, ellipse.CenterColumn, ellipse.PhiRadians,
                    ellipse.Radius1, ellipse.Radius2, 0.0, Math.PI * 2.0, "positive", parameters.PointEllipseContourResolution);
                HOperatorSet.DistancePc(ellipseContour, point.Row, point.Column, out distanceMin, out distanceMax);
                if (distanceMin == null || distanceMin.Length < 1 || double.IsNaN(distanceMin[0].D) || double.IsInfinity(distanceMin[0].D) || distanceMin[0].D < 0)
                    throw new InvalidOperationException("HALCON distance_pc 未返回有效的点到椭圆距离。");
                HOperatorSet.GetContourXld(ellipseContour, out contourRows, out contourColumns);
                if (contourRows == null || contourRows.Length < 4) throw new InvalidOperationException("HALCON 椭圆轮廓点不足，无法确定最近位置。");
                System.Windows.Point nearest = FindNearestContourPointS74(point.Row, point.Column, contourRows, contourColumns);
                double closestRow = nearest.Y;
                double closestColumn = nearest.X;
                double distance = distanceMin[0].D;

                double cos = Math.Cos(ellipse.PhiRadians);
                double sin = Math.Sin(ellipse.PhiRadians);
                double deltaRow = point.Row - ellipse.CenterRow;
                double deltaColumn = point.Column - ellipse.CenterColumn;
                // HALCON image coordinates use Radius1 along (-sin(Phi), cos(Phi))
                // and Radius2 along (cos(Phi), sin(Phi)) in (row, column).
                double localU = -deltaRow * sin + deltaColumn * cos;
                double localV = deltaRow * cos + deltaColumn * sin;
                double normalizedRadius = Math.Sqrt((localU * localU) / (ellipse.Radius1 * ellipse.Radius1) + (localV * localV) / (ellipse.Radius2 * ellipse.Radius2));
                if (double.IsNaN(normalizedRadius) || double.IsInfinity(normalizedRadius)) throw new InvalidOperationException("椭圆归一化半径不是有限数值。");
                string relation = distance <= parameters.PointEllipseBoundaryTolerance ? "OnBoundary" : (normalizedRadius > 1.0 ? "Outside" : "Inside");
                double signedDistance = string.Equals(relation, "OnBoundary", StringComparison.Ordinal) ? 0 : (string.Equals(relation, "Outside", StringComparison.Ordinal) ? distance : -distance);

                double closestDeltaRow = closestRow - ellipse.CenterRow;
                double closestDeltaColumn = closestColumn - ellipse.CenterColumn;
                double closestU = -closestDeltaRow * sin + closestDeltaColumn * cos;
                double closestV = closestDeltaRow * cos + closestDeltaColumn * sin;
                double normalU = closestU / (ellipse.Radius1 * ellipse.Radius1);
                double normalV = closestV / (ellipse.Radius2 * ellipse.Radius2);
                double normalRow = -normalU * sin + normalV * cos;
                double normalColumn = normalU * cos + normalV * sin;
                double normalLength = Math.Sqrt(normalRow * normalRow + normalColumn * normalColumn);
                if (normalLength <= 1e-12) throw new InvalidOperationException("最近点外法线方向不稳定。");
                double normalAngle = Math.Atan2(normalRow, normalColumn) * 180.0 / Math.PI;

                AppendOverlayObject(ref overlays, ellipseContour);
                HOperatorSet.GenContourPolygonXld(out measurementLine,
                    new HTuple(new[] { point.Row, closestRow }), new HTuple(new[] { point.Column, closestColumn }));
                AppendOverlayObject(ref overlays, measurementLine);
                HOperatorSet.GenCrossContourXld(out crosses,
                    new HTuple(new[] { point.Row, closestRow }), new HTuple(new[] { point.Column, closestColumn }),
                    parameters.PointEllipseOverlayCrossSize, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, crosses);

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("ClosestPoint", VmPoint2D.CreateImagePixel(closestRow, closestColumn, point.CoordinateContextId));
                tool.SetOutputValue("Distance", distance);
                tool.SetOutputValue("SignedDistance", signedDistance);
                tool.SetOutputValue("Relation", relation);
                tool.SetOutputValue("NormalizedRadius", normalizedRadius);
                tool.SetOutputValue("NormalAngle", normalAngle);
                tool.SetOutputValue("CoordinateContext", point.CoordinateContext + ":" + point.CoordinateContextId);
                string parameterText = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=gen_ellipse_contour_xld+distance_pc, BoundaryTolerance={0:0.###}px, CrossSize={1:0.###}px, ContourResolution={2:0.###}",
                    parameters.PointEllipseBoundaryTolerance, parameters.PointEllipseOverlayCrossSize, parameters.PointEllipseContourResolution);
                tool.SetOutputValue("Parameters", parameterText);
                tool.SetOutputValue("ResultCode", "OK");
                ReplaceToolOverlayContours(overlays, tool, "XLD", 4, "OK", distance);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 点到椭圆：Point=({0:0.###},{1:0.###})，Closest=({2:0.###},{3:0.###})，Distance={4:0.###}px，Signed={5:0.###}px，Relation={6}，NormalizedRadius={7:0.######}，NormalAngle={8:0.###}° · {9}",
                    point.Row, point.Column, closestRow, closestColumn, distance, signedDistance, relation, normalizedRadius, normalAngle, point.CoordinateContextId);
                InspectionRecord record = CreateRecord("PointToEllipseDistanceMeasurement", "OK", distance, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (distanceMin != null) distanceMin.Dispose();
                if (distanceMax != null) distanceMax.Dispose();
                if (contourRows != null) contourRows.Dispose();
                if (contourColumns != null) contourColumns.Dispose();
                DisposeObject(crosses);
                DisposeObject(measurementLine);
                DisposeObject(ellipseContour);
                DisposeObject(overlays);
            }
        }
    }
}
