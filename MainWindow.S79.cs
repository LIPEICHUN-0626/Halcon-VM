using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool updatingConnectionBindingS79;

        private bool IsFlowNodeCommandHitS79(DependencyObject hit)
        {
            while (hit != null)
            {
                if (hit is System.Windows.Controls.Primitives.ButtonBase) return true;
                FrameworkContentElement content = hit as FrameworkContentElement;
                DependencyObject parent = content?.Parent;
                if (parent == null && hit is System.Windows.Media.Visual) parent = System.Windows.Media.VisualTreeHelper.GetParent(hit);
                hit = parent ?? LogicalTreeHelper.GetParent(hit);
            }
            return false;
        }

        private void FlowBindingSummary_ClickS79(object sender, RoutedEventArgs e)
        {
            FrameworkElement button = sender as FrameworkElement;
            OpenFlowBindingSummaryS79(button?.Tag as VmToolInstance);
            e.Handled = true;
        }

        private void OpenFlowBindingSummaryS79(VmToolInstance tool)
        {
            if (tool == null || !flowTools.Contains(tool)) return;
            if (flowPortDragSession != null && flowPortDragSession.IsClickArmed)
            {
                VmFlowPortChip hoverPort = ResolveConnectionBodyHoverS79(tool);
                if (hoverPort != null) TryCompleteArmedFlowConnectionS79(hoverPort);
                else HeaderStatusText.Text = "不能连接：" + aggregatePortHoverErrorS75 + "；可换一个模块或按 Esc 取消。";
                return;
            }
            List<VmFlowConnectionVisual> inputs = flowConnections.Where(item =>
                string.Equals(item.TargetToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (inputs.Count == 1) SelectFlowConnection(inputs[0], false);
            else if (tool.FlowInputPorts.Count > 0)
            {
                OpenFlowAggregatePortFlyoutS75(tool, "IN");
                HeaderStatusText.Text = "请选择“" + tool.InstanceName + "”的精确输入端口，再重选来源。";
            }
            else HeaderStatusText.Text = "“" + tool.InstanceName + "”没有输入订阅；双击可编辑自身参数。";
        }

        // The capture target is measured in screen DIPs, so zooming out does not
        // turn the small visual anchor into an unusably small mouse target.
        private VmToolInstance FindNearbyConnectionAnchorS79(Point point, out string direction)
        {
            direction = null;
            double zoom = FlowGraphScaleTransform == null ? 1 : Math.Max(0.1, FlowGraphScaleTransform.ScaleX);
            VmToolInstance nearest = null;
            double nearestDistance = double.MaxValue;
            foreach (VmToolInstance tool in flowTools.Reverse())
            {
                if (Math.Abs(point.X - tool.GraphX - 140) * zoom > 24) continue;
                double inputDistance = Math.Abs(point.Y - tool.GraphY);
                double outputDistance = Math.Abs(point.Y - tool.GraphY - tool.GraphHeight);
                bool input = inputDistance <= outputDistance;
                if (Math.Min(inputDistance, outputDistance) * zoom > Math.Min(14, tool.GraphHeight * zoom / 3)) continue;
                if ((input ? tool.FlowInputPorts.Count : tool.FlowOutputPorts.Count) == 0) continue;
                double distance = Math.Pow(point.X - tool.GraphX - 140, 2) + Math.Pow(Math.Min(inputDistance, outputDistance), 2);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                direction = input ? "IN" : "OUT";
                nearest = tool;
            }
            return nearest;
        }

        private bool HandleConnectionMouseDownS79(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || isContinuousRunning || isFlowExecutionActive || flowNodeDragSession != null) return false;
            Point point = e.GetPosition(FlowGraphCanvas);
            if (flowPortDragSession != null)
            {
                if (!flowPortDragSession.IsClickArmed) return false;
                VmFlowPortChip port = FindFlowPortChipAt(point);
                if (port != null) return CompleteArmedFlowConnectionS79(port);
                if (aggregatePortHoverDetectedS75)
                {
                    HeaderStatusText.Text = "不能连接：" + aggregatePortHoverErrorS75 + " 可换一个模块，或按 Esc 取消。";
                    return true;
                }
                return false;
            }
            FrameworkElement hit = e.OriginalSource as FrameworkElement;
            if (hit != null && hit.Tag is VmFlowConnectionVisual) return false;
            // Forgiving invisible anchor regions must not steal a deliberate command click.
            if (IsFlowNodeCommandHitS79(e.OriginalSource as DependencyObject)) return false;
            string direction;
            VmToolInstance tool = FindNearbyConnectionAnchorS79(point, out direction);
            if (tool == null) return false;
            BeginAggregateFlowPortDragS79(tool, direction, FlowGraphCanvas,
                new Point(tool.GraphX + 140, tool.GraphY + (direction == "OUT" ? tool.GraphHeight : 0)));
            // Keep the real pointer origin for distinguishing a click from a drag.
            if (flowPortDragSession != null) flowPortDragSession.StartPoint = point;
            return true;
        }

        private VmFlowPortChip ResolveConnectionBodyHoverS79(VmToolInstance tool)
        {
            if (flowPortDragSession == null || tool == null) return null;
            bool startsAtOutput = !string.IsNullOrWhiteSpace(flowPortDragSession.AggregateOriginDirection)
                ? flowPortDragSession.AggregateOriginDirection == "OUT" : flowPortDragSession.FixedTargetPort == null;
            aggregatePortHoverDetectedS75 = true;
            return ResolveAggregateHoverPortS75(tool, startsAtOutput ? "IN" : "OUT", out aggregatePortHoverErrorS75);
        }

        private void UpdateConnectionBindingSelectorS79(VmFlowConnectionVisual connection)
        {
            if (FlowConnectionBindingComboS79 == null) return;
            updatingConnectionBindingS79 = true;
            try
            {
                List<VmInputSourceOption> options = new List<VmInputSourceOption>();
                VmToolInstance target = flowTools.FirstOrDefault(tool => tool.ToolId == connection.TargetToolId);
                VmFlowPortChip targetPort = target == null ? null : target.FlowInputPorts.FirstOrDefault(port => port.PortName == connection.TargetPortName);
                foreach (VmToolInstance source in flowTools)
                foreach (VmFlowPortChip port in source.FlowOutputPorts)
                {
                    string error;
                    if (!EvaluateFlowPortConnection(port, targetPort, out error)) continue;
                    options.Add(new VmInputSourceOption { Key = BuildDockSourceKey(source.ToolId, port.PortName),
                        SourceToolId = source.ToolId, SourcePortName = port.PortName, DataType = port.DataType,
                        DisplayText = source.InstanceName + "." + port.PortName, IsValid = true });
                }
                string key = BuildDockSourceKey(connection.SourceToolId, connection.SourcePortName);
                if (!options.Any(option => option.Key == key))
                    options.Add(new VmInputSourceOption { Key = key, DisplayText = connection.SourceEndpointText + "（当前来源不可用）", IsValid = false });
                FlowConnectionBindingComboS79.ItemsSource = options;
                FlowConnectionBindingComboS79.SelectedValue = key;
                FlowConnectionBindingComboS79.IsEnabled = !isContinuousRunning && !isFlowExecutionActive;
            }
            finally { updatingConnectionBindingS79 = false; }
        }

        private void FlowConnectionBindingCombo_SelectionChangedS79(object sender, SelectionChangedEventArgs e)
        {
            if (updatingConnectionBindingS79) return;
            VmInputSourceOption option = FlowConnectionBindingComboS79.SelectedItem as VmInputSourceOption;
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            if (option == null || connection == null) return;
            if (option.Key == BuildDockSourceKey(connection.SourceToolId, connection.SourcePortName)) return;
            VmToolInstance source = flowTools.FirstOrDefault(tool => tool.ToolId == option.SourceToolId);
            VmToolInstance target = flowTools.FirstOrDefault(tool => tool.ToolId == connection.TargetToolId);
            VmFlowPortChip sourcePort = source == null ? null : source.FlowOutputPorts.FirstOrDefault(port => port.PortName == option.SourcePortName);
            VmFlowPortChip targetPort = target == null ? null : target.FlowInputPorts.FirstOrDefault(port => port.PortName == connection.TargetPortName);
            string error;
            if (!TryApplyFlowPortBinding(sourcePort, targetPort, out error))
            {
                UpdateConnectionBindingSelectorS79(connection);
                HeaderStatusText.Text = "绑定未修改：" + error;
            }
        }

        private void BeginAggregateFlowPortDragS79(VmToolInstance tool, string direction, FrameworkElement origin, Point startPoint)
        {
            if (tool == null || origin == null || FlowGraphCanvas == null) return;
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("开始模块自动连接");
            if (editLeaseS137 < 0) return;
            CloseFlowAggregatePortFlyoutS75();
            flowPortDragSession = new FlowPortDragSession
            {
                EngineeringLeaseS137 = editLeaseS137,
                AggregateOriginToolId = tool.ToolId,
                AggregateOriginDirection = direction,
                StartPoint = startPoint
            };
            FlowPortDragPreviewPath.Data = CreateFlowConnectionGeometry(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y);
            FlowPortDragPreviewPath.Stroke = GetFlowConnectionBrush(string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase) ? "Image" : "Number");
            FlowPortDragPreviewPath.Visibility = Visibility.Visible;
            FlowPortDragEndpointText.Text = tool.InstanceName + " · " + direction + "（自动匹配端口）";
            FlowPortDragValidationText.Text = string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase)
                ? "拖到下游顶部连接点，或松开后再点击目标"
                : "拖到上游底部连接点，或松开后再点击来源";
            FlowPortDragStatusBorder.Visibility = Visibility.Visible;
            PositionFlowPortDragStatus(startPoint);
            FlowGraphCanvas.CaptureMouse();
            HeaderStatusText.Text = string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase)
                ? "连接已开始：拖到下游顶部圆点；也可松开后再点击目标。"
                : "重选来源：拖到上游底部圆点；也可松开后再点击来源。";
        }

        private void ArmFlowPortClickConnectionS79()
        {
            if (flowPortDragSession == null) return;
            if (!IsEngineeringEditLeaseCurrentS137(flowPortDragSession.EngineeringLeaseS137))
            {
                CancelFlowPortDrag("工程授权已改变，请重新开始连接；原绑定未修改。", true);
                return;
            }
            flowPortDragSession.IsClickArmed = true;
            if (FlowGraphCanvas != null && FlowGraphCanvas.IsMouseCaptured) Mouse.Capture(null);
            if (FlowPortDragPreviewPath != null) FlowPortDragPreviewPath.Visibility = Visibility.Collapsed;
            if (FlowPortDragStatusBorder != null) FlowPortDragStatusBorder.Visibility = Visibility.Collapsed;
            string direction = !string.IsNullOrWhiteSpace(flowPortDragSession.AggregateOriginDirection)
                ? flowPortDragSession.AggregateOriginDirection
                : (flowPortDragSession.FixedTargetPort == null ? "OUT" : "IN");
            HeaderStatusText.Text = string.Equals(direction, "OUT", StringComparison.OrdinalIgnoreCase)
                ? "已选来源：现在点击下游模块顶部连接点；系统会自动绑定最合适的同类型端口。"
                : "已选目标输入：现在点击上游模块底部连接点；系统会自动替换真实来源。";
        }

        private bool TryCompleteArmedAggregateFlowConnectionS79(VmToolInstance hoverTool, string hoverDirection)
        {
            if (flowPortDragSession == null || !flowPortDragSession.IsClickArmed || hoverTool == null) return false;
            string resolveError;
            VmFlowPortChip hoverPort = ResolveAggregateHoverPortS75(hoverTool, hoverDirection, out resolveError);
            if (hoverPort == null)
            {
                HeaderStatusText.Text = "不能自动连接：" + resolveError + "；可在端口列表精确选择。";
                OpenFlowAggregatePortFlyoutS75(hoverTool, hoverDirection);
                return true;
            }
            return CompleteArmedFlowConnectionS79(hoverPort);
        }

        private bool TryCompleteArmedFlowConnectionS79(VmFlowPortChip hoverPort)
        {
            return flowPortDragSession != null && flowPortDragSession.IsClickArmed && hoverPort != null && CompleteArmedFlowConnectionS79(hoverPort);
        }

        private bool CompleteArmedFlowConnectionS79(VmFlowPortChip hoverPort)
        {
            if (flowPortDragSession == null) return false;
            if (!IsEngineeringEditLeaseCurrentS137(flowPortDragSession.EngineeringLeaseS137))
            {
                CancelFlowPortDrag("工程授权已改变，请重新开始连接；原绑定未修改。", true);
                return true;
            }
            VmFlowPortChip source;
            VmFlowPortChip target;
            string error;
            if (!TryResolveFlowPortDragEndpoints(flowPortDragSession, hoverPort, out source, out target, out error))
            {
                HeaderStatusText.Text = "不能连接：" + error + "；待连接状态仍保留，可换一个端点或按 Esc 取消。";
                return true;
            }
            string applyError;
            bool applied = TryApplyFlowPortBinding(source, target, out applyError);
            CancelFlowPortDrag(applied ? null : "连接失败，原订阅已保留：" + applyError, !applied);
            return true;
        }

        private VmFlowPortChip ResolveAggregateHoverPortS79(VmToolInstance hoverTool, string hoverDirection, out string error)
        {
            error = string.Empty;
            if (flowPortDragSession == null || hoverTool == null)
            {
                error = "连接状态已失效。";
                return null;
            }
            bool originIsOutput = string.Equals(flowPortDragSession.AggregateOriginDirection, "OUT", StringComparison.OrdinalIgnoreCase);
            if (originIsOutput == string.Equals(hoverDirection, "OUT", StringComparison.OrdinalIgnoreCase))
            {
                error = originIsOutput ? "请选择下游顶部输入点。" : "请选择上游底部输出点。";
                return null;
            }
            VmToolInstance origin = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, flowPortDragSession.AggregateOriginToolId, StringComparison.OrdinalIgnoreCase));
            if (origin == null)
            {
                error = "起始模块已不存在。";
                return null;
            }
            IEnumerable<VmFlowPortChip> sources = originIsOutput ? origin.FlowOutputPorts : hoverTool.FlowOutputPorts;
            IEnumerable<VmFlowPortChip> targets = originIsOutput ? hoverTool.FlowInputPorts : origin.FlowInputPorts;
            List<Tuple<VmFlowPortChip, VmFlowPortChip, int>> pairs = BuildCompatiblePortPairsS79(sources, targets);
            if (pairs.Count == 0)
            {
                error = origin.InstanceName + " 与 " + hoverTool.InstanceName + " 没有可用的同类型前向端口。";
                return null;
            }
            return originIsOutput ? pairs[0].Item2 : pairs[0].Item1;
        }

        private bool TryResolveAggregateFlowConnectionS79(FlowPortDragSession session, VmFlowPortChip hoverPort, out VmFlowPortChip sourcePort, out VmFlowPortChip targetPort, out string error)
        {
            sourcePort = null;
            targetPort = null;
            error = string.Empty;
            VmToolInstance origin = session == null ? null : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, session.AggregateOriginToolId, StringComparison.OrdinalIgnoreCase));
            if (origin == null || hoverPort == null)
            {
                error = "连接端点已失效。";
                return false;
            }
            bool originIsOutput = string.Equals(session.AggregateOriginDirection, "OUT", StringComparison.OrdinalIgnoreCase);
            IEnumerable<VmFlowPortChip> sources = originIsOutput ? origin.FlowOutputPorts.AsEnumerable() : new[] { hoverPort }.AsEnumerable();
            IEnumerable<VmFlowPortChip> targets = originIsOutput ? new[] { hoverPort }.AsEnumerable() : origin.FlowInputPorts.AsEnumerable();
            List<Tuple<VmFlowPortChip, VmFlowPortChip, int>> pairs = BuildCompatiblePortPairsS79(sources, targets);
            if (pairs.Count == 0)
            {
                error = originIsOutput ? "目标没有兼容的可绑定输入端口。" : "来源没有兼容的可绑定输出端口。";
                return false;
            }
            sourcePort = pairs[0].Item1;
            targetPort = pairs[0].Item2;
            return true;
        }

        private List<Tuple<VmFlowPortChip, VmFlowPortChip, int>> BuildCompatiblePortPairsS79(IEnumerable<VmFlowPortChip> sources, IEnumerable<VmFlowPortChip> targets)
        {
            List<Tuple<VmFlowPortChip, VmFlowPortChip, int>> pairs = new List<Tuple<VmFlowPortChip, VmFlowPortChip, int>>();
            foreach (VmFlowPortChip source in sources ?? Enumerable.Empty<VmFlowPortChip>())
            foreach (VmFlowPortChip target in targets ?? Enumerable.Empty<VmFlowPortChip>())
            {
                string reason;
                if (EvaluateFlowPortConnection(source, target, out reason)) pairs.Add(Tuple.Create(source, target, GetAutoBindingScoreS79(source, target)));
            }
            return pairs.OrderByDescending(item => item.Item3)
                .ThenBy(item => item.Item2.PortName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Item1.PortName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private int GetAutoBindingScoreS79(VmFlowPortChip source, VmFlowPortChip target)
        {
            int score = 0;
            if (string.Equals(source.PortName, target.PortName, StringComparison.OrdinalIgnoreCase)) score += 200;
            if (string.Equals(source.PortName, source.DataType, StringComparison.OrdinalIgnoreCase)) score += 80;
            VmToolInstance targetTool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, target.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition definition = targetTool == null ? null : ToolMetadata.GetInstanceInputPorts(targetTool).FirstOrDefault(item => string.Equals(item.PortName, target.PortName, StringComparison.OrdinalIgnoreCase));
            if (targetTool != null && targetTool.GetInputBinding(target.PortName) == null) score += definition != null && !definition.IsOptional ? 100 : 40;
            if (string.Equals(target.PortName, "Image", StringComparison.OrdinalIgnoreCase) || string.Equals(target.PortName, "Value", StringComparison.OrdinalIgnoreCase)) score += 30;
            return score;
        }

        private string GetFlowPortDragOriginTextS79(FlowPortDragSession session)
        {
            if (session == null) return "连接端点";
            if (session.OriginPort != null) return session.OriginPort.OwnerToolName + "." + session.OriginPort.PortName + " · " + session.OriginPort.DataType;
            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, session.AggregateOriginToolId, StringComparison.OrdinalIgnoreCase));
            return (tool == null ? "模块" : tool.InstanceName) + " · " + (session.AggregateOriginDirection ?? "端口") + "（自动匹配）";
        }

        private string GetFlowPortDragOriginDataTypeS79(FlowPortDragSession session)
        {
            return session != null && session.OriginPort != null ? session.OriginPort.DataType : "Image";
        }
    }
}
