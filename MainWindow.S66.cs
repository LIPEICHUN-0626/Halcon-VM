using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void InsertSelectedCatalogToolBeforeButton_Click(object sender, RoutedEventArgs e)
        {
            InsertSelectedCatalogToolAtAnchorS66(false);
        }

        private void InsertSelectedCatalogToolAfterButton_Click(object sender, RoutedEventArgs e)
        {
            InsertSelectedCatalogToolAtAnchorS66(true);
        }

        private void InsertSelectedCatalogToolAtAnchorS66(bool after)
        {
            VmToolCatalogItem catalogItem = ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem;
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            if (productionRunMode)
            {
                SetInsertionFeedbackS66("运行模式禁止编辑流程；请先返回开发模式。", true);
                return;
            }
            if (historicalImageMode)
            {
                SetInsertionFeedbackS66("历史结果浏览为只读；退出历史模式后再插入。", true);
                return;
            }
            if (catalogItem == null)
            {
                SetInsertionFeedbackS66("请先在左侧模块箱选择要插入的工具。", true);
                return;
            }
            int anchorIndex = anchor == null ? -1 : flowTools.IndexOf(anchor);
            if (anchorIndex < 0)
            {
                SetInsertionFeedbackS66("请先选择流程中的插入锚点；Enter 仍可追加到末尾。", true);
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            int insertionIndex = anchorIndex + (after ? 1 : 0);
            VmToolInstance inserted;
            suppressFlowInsertHistory = true;
            try { inserted = InsertCatalogTool(catalogItem, insertionIndex, after ? "按锚点插入到后方" : "按锚点插入到前方"); }
            finally { suppressFlowInsertHistory = false; }
            if (inserted == null) return;

            List<string> bindings = AutoBindInsertedToolS66(inserted, anchor, after);
            InvalidateToolRunResult(inserted, "模块已插入，等待运行");
            InvalidateDownstreamResults(inserted, "流程插入位置和订阅已变化");
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("锚点插入模块：" + inserted.InstanceName, before);
            FlowToolList.SelectedItems.Clear();
            FlowToolList.SelectedItem = inserted;
            FlowToolList.ScrollIntoView(inserted);
            if (bindings.Count > 0)
            {
                VmToolInputBindingData first = inserted.InputBindings.FirstOrDefault();
                if (first != null)
                    selectedFlowConnectionKey = BuildFlowConnectionKey(first.SourceToolId, first.SourcePortName, inserted.ToolId, first.TargetPortName);
            }
            RefreshUiState();
            RefreshToolCapabilityPreviewS65(catalogItem);
            string position = after ? "后" : "前";
            SetInsertionFeedbackS66(inserted.InstanceName + " 已插入到 " + anchor.InstanceName + " 之" + position +
                                    "；" + (bindings.Count == 0 ? "未找到可安全自动连接的端口，必需输入保持待连接。" : "已写回 " + bindings.Count.ToString(CultureInfo.InvariantCulture) + " 条真实订阅：" + string.Join("；", bindings)), false);
        }

        private List<string> AutoBindInsertedToolS66(VmToolInstance inserted, VmToolInstance anchor, bool after)
        {
            List<string> changes = new List<string>();
            int insertedIndex = flowTools.IndexOf(inserted);
            VmToolInstance downstream = after
                ? flowTools.ElementAtOrDefault(insertedIndex + 1)
                : anchor;
            HashSet<string> usedSourceEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!after && downstream != null)
            {
                foreach (VmToolInputBindingData oldBinding in downstream.InputBindings.Select(item => item.Clone()).ToList())
                {
                    VmPortDefinition downstreamPort = ToolMetadata.GetInstanceInputPorts(downstream)
                        .FirstOrDefault(port => string.Equals(port.PortName, oldBinding.TargetPortName, StringComparison.OrdinalIgnoreCase));
                    VmPortDefinition insertedInput = ToolMetadata.GetInstanceInputPorts(inserted)
                        .FirstOrDefault(port => inserted.GetInputBinding(port.PortName) == null && downstreamPort != null && string.Equals(port.DataType, downstreamPort.DataType, StringComparison.OrdinalIgnoreCase));
                    VmPortDefinition insertedOutput = ToolMetadata.GetInstanceOutputPorts(inserted)
                        .FirstOrDefault(port => downstreamPort != null && string.Equals(port.DataType, downstreamPort.DataType, StringComparison.OrdinalIgnoreCase));
                    if (insertedInput == null || insertedOutput == null) continue;
                    if (!TrySetAutoBindingS66(inserted, insertedInput, oldBinding.SourceToolId, oldBinding.SourcePortName)) continue;
                    if (!TrySetAutoBindingS66(downstream, downstreamPort, inserted.ToolId, insertedOutput.PortName))
                    {
                        inserted.RemoveInputBinding(insertedInput.PortName);
                        continue;
                    }
                    usedSourceEndpoints.Add(oldBinding.SourceToolId + "|" + oldBinding.SourcePortName);
                    changes.Add(GetEndpointNameS66(oldBinding.SourceToolId, oldBinding.SourcePortName) + " → " + inserted.InstanceName + "." + insertedInput.PortName + " → " + downstream.InstanceName + "." + downstreamPort.PortName);
                }
            }

            foreach (VmPortDefinition input in ToolMetadata.GetInstanceInputPorts(inserted))
            {
                if (inserted.GetInputBinding(input.PortName) != null) continue;
                Tuple<VmToolInstance, VmPortDefinition> source = FindNearestCompatibleSourceS66(insertedIndex, input.DataType, usedSourceEndpoints, anchor);
                if (source == null || !TrySetAutoBindingS66(inserted, input, source.Item1.ToolId, source.Item2.PortName)) continue;
                usedSourceEndpoints.Add(source.Item1.ToolId + "|" + source.Item2.PortName);
                changes.Add(source.Item1.InstanceName + "." + source.Item2.PortName + " → " + inserted.InstanceName + "." + input.PortName);
            }

            if (after && downstream != null)
            {
                foreach (VmPortDefinition targetPort in ToolMetadata.GetInstanceInputPorts(downstream).Where(port => !port.IsOptional && downstream.GetInputBinding(port.PortName) == null))
                {
                    VmPortDefinition output = ToolMetadata.GetInstanceOutputPorts(inserted).FirstOrDefault(port => string.Equals(port.DataType, targetPort.DataType, StringComparison.OrdinalIgnoreCase));
                    if (output == null || !TrySetAutoBindingS66(downstream, targetPort, inserted.ToolId, output.PortName)) continue;
                    changes.Add(inserted.InstanceName + "." + output.PortName + " → " + downstream.InstanceName + "." + targetPort.PortName);
                }
            }
            return changes;
        }

        private Tuple<VmToolInstance, VmPortDefinition> FindNearestCompatibleSourceS66(int beforeIndex, string dataType, ISet<string> usedEndpoints, VmToolInstance preferred)
        {
            IEnumerable<VmToolInstance> candidates = flowTools.Take(Math.Max(0, beforeIndex)).Reverse();
            if (preferred != null && flowTools.IndexOf(preferred) < beforeIndex)
                candidates = new[] { preferred }.Concat(candidates.Where(tool => !ReferenceEquals(tool, preferred)));
            foreach (VmToolInstance source in candidates.Where(tool => tool.IsEnabled))
            {
                foreach (VmPortDefinition port in ToolMetadata.GetInstanceOutputPorts(source).Where(port => string.Equals(port.DataType, dataType, StringComparison.OrdinalIgnoreCase)))
                {
                    string key = source.ToolId + "|" + port.PortName;
                    if (!usedEndpoints.Contains(key)) return Tuple.Create(source, port);
                }
            }
            return null;
        }

        private bool TrySetAutoBindingS66(VmToolInstance target, VmPortDefinition targetPort, string sourceToolId, string sourcePortName)
        {
            VmToolInputBindingData previous = target.GetInputBinding(targetPort.PortName);
            previous = previous == null ? null : previous.Clone();
            string oldTool = target.InputToolId;
            string oldPort = target.InputPortName;
            SetSelectedFlowPortBinding(target, targetPort, sourceToolId, sourcePortName);
            VmToolInputBindingData candidate = target.GetInputBinding(targetPort.PortName);
            string error = candidate == null ? "订阅未写入。" : GetInputBindingConfigurationError(target, candidate);
            if (string.IsNullOrWhiteSpace(error) && target.Kind == VmToolKind.NumericJudge && targetPort.PortName == "Value") error = GetNumericJudgeConfigurationError(target);
            if (string.IsNullOrWhiteSpace(error)) return true;
            RestoreSelectedFlowPortBinding(target, targetPort, previous, oldTool, oldPort);
            return false;
        }

        private string GetEndpointNameS66(string toolId, string portName)
        {
            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
            return (tool == null ? toolId : tool.InstanceName) + "." + portName;
        }

        private void SetInsertionFeedbackS66(string textValue, bool error)
        {
            HeaderStatusText.Text = textValue;
            if (ToolCapabilityInsertStatusText != null)
            {
                ToolCapabilityInsertStatusText.Text = textValue;
                ToolCapabilityInsertStatusText.Foreground = error
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(167, 243, 208));
            }
            LogInfo(textValue);
        }

        private void RefreshToolInsertionPreviewS66(VmToolCatalogItem item)
        {
            if (ToolCapabilityInsertAnchorText == null) return;
            VmToolInstance anchor = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            int index = anchor == null ? -1 : flowTools.IndexOf(anchor);
            ToolCapabilityInsertAnchorText.Text = anchor == null || index < 0
                ? "插入锚点：未选择流程模块"
                : "插入锚点：#" + anchor.SequenceText + " " + anchor.InstanceName + " · 可插入此前/此后";
            if (item == null || index < 0)
            {
                ToolCapabilityCompatibilityText.Text = "就近兼容：选择工具和流程锚点后显示";
                ToolCapabilityInsertStatusText.Text = "拒绝原因：缺少工具或流程锚点；Enter 仍可追加到末尾。";
                return;
            }
            List<string> previews = new List<string>();
            List<string> downstreamPreviews = new List<string>();
            List<string> missing = new List<string>();
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (VmPortDefinition input in ToolMetadata.GetInputPorts(item.Kind))
            {
                Tuple<VmToolInstance, VmPortDefinition> source = FindNearestCompatibleSourceS66(index + 1, input.DataType, used, anchor);
                if (source == null)
                {
                    if (!input.IsOptional) missing.Add(input.PortName + ":" + input.DataType);
                }
                else
                {
                    used.Add(source.Item1.ToolId + "|" + source.Item2.PortName);
                    previews.Add(input.PortName + "←" + source.Item1.InstanceName + "." + source.Item2.PortName);
                }
            }
            foreach (VmPortDefinition output in ToolMetadata.GetOutputPorts(item.Kind))
            {
                VmPortDefinition targetPort = ToolMetadata.GetInstanceInputPorts(anchor).FirstOrDefault(port => string.Equals(port.DataType, output.DataType, StringComparison.OrdinalIgnoreCase));
                if (targetPort != null) downstreamPreviews.Add(output.PortName + "→" + anchor.InstanceName + "." + targetPort.PortName + "（插入此前可桥接）");
                VmToolInstance next = flowTools.ElementAtOrDefault(index + 1);
                if (next == null) continue;
                VmPortDefinition nextPort = ToolMetadata.GetInstanceInputPorts(next).FirstOrDefault(port => next.GetInputBinding(port.PortName) == null && string.Equals(port.DataType, output.DataType, StringComparison.OrdinalIgnoreCase));
                if (nextPort != null) downstreamPreviews.Add(output.PortName + "→" + next.InstanceName + "." + nextPort.PortName + "（插入此后可补接）");
            }
            List<string> compatibility = new List<string>();
            if (previews.Count > 0) compatibility.Add("上游 " + string.Join(" · ", previews));
            if (downstreamPreviews.Count > 0) compatibility.Add("下游 " + string.Join(" · ", downstreamPreviews.Distinct(StringComparer.OrdinalIgnoreCase)));
            ToolCapabilityCompatibilityText.Text = compatibility.Count == 0 ? "就近兼容：未发现可自动连接端口" : "就近兼容：" + string.Join("；", compatibility);
            ToolCapabilityInsertStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            ToolCapabilityInsertStatusText.Text = missing.Count == 0
                ? "可自动连接全部必需输入；Ctrl+Enter 插入到后方，Ctrl+Shift+Enter 插入到前方。"
                : "插入后待连接：" + string.Join("、", missing) + "；不会伪造连接或改变确定性执行语义。";
        }

        private string ResolveImagePixelContextIdS66(VmToolInstance tool)
        {
            return ResolveImagePixelContextIdS66(tool, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) ?? "Global.Image";
        }

        private string ResolveImagePixelContextIdS66(VmToolInstance tool, ISet<string> visited)
        {
            if (tool == null || !visited.Add(tool.ToolId)) return null;
            if (tool.Kind == VmToolKind.BranchValueSelection)
            {
                return ResolveOutputCoordinateContextS102(tool, "Value", visited);
            }
            foreach (VmToolInputBindingData binding in tool.InputBindings)
            {
                if (binding.TargetPortName == "$RunCondition") continue;
                VmToolInstance source = GetInputSourceTool(binding);
                if (source == null) continue;
                object raw;
                if (source.TryGetOutputValue(binding.SourcePortName, out raw))
                {
                    VmPose2D pose = raw as VmPose2D;
                    if (pose != null && !string.IsNullOrWhiteSpace(pose.CoordinateContextId)) return pose.CoordinateContextId;
                    VmPoint2D point = raw as VmPoint2D;
                    if (point != null && !string.IsNullOrWhiteSpace(point.CoordinateContextId)) return point.CoordinateContextId;
                    VmPoint2DArray points = raw as VmPoint2DArray;
                    if (points != null && !string.IsNullOrWhiteSpace(points.CoordinateContextId)) return points.CoordinateContextId;
                    VmLine2D line = raw as VmLine2D;
                    if (line != null && !string.IsNullOrWhiteSpace(line.CoordinateContextId)) return line.CoordinateContextId;
                    VmCircle2D circle = raw as VmCircle2D;
                    if (circle != null && !string.IsNullOrWhiteSpace(circle.CoordinateContextId)) return circle.CoordinateContextId;
                    VmEllipse2D ellipse = raw as VmEllipse2D;
                    if (ellipse != null && !string.IsNullOrWhiteSpace(ellipse.CoordinateContextId)) return ellipse.CoordinateContextId;
                }
                VmPortDefinition sourcePort = ToolMetadata.GetInstanceOutputPorts(source).FirstOrDefault(port => string.Equals(port.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                if (sourcePort != null && (sourcePort.DataType == "Image" || sourcePort.DataType == "Region" || sourcePort.DataType == "XLD"))
                    return ResolveOutputCoordinateContextS102(source, sourcePort.PortName, visited);
                string nested = ResolveImagePixelContextIdS66(source, visited);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
            return null;
        }

        private void RefreshPointToLineConnectionStatusS66(VmToolInstance tool)
        {
            VmToolInputBindingData pointBinding = tool == null ? null : tool.GetInputBinding("Point");
            VmToolInputBindingData lineBinding = tool == null ? null : tool.GetInputBinding("Line");
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            object pointRaw;
            object lineRaw;
            bool pointReady = pointSource != null && string.Equals(pointSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && pointSource.TryGetOutputValue(pointBinding.SourcePortName, out pointRaw) && pointRaw is VmPoint2D;
            bool lineReady = lineSource != null && string.Equals(lineSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && lineSource.TryGetOutputValue(lineBinding.SourcePortName, out lineRaw) && lineRaw is VmLine2D;
            tool.ConfigurationStatus = pointBinding == null || lineBinding == null ? "待连接几何" : (pointReady && lineReady ? "就绪" : "等待上游");
            tool.ConnectionStatus = pointBinding == null || lineBinding == null ? "Point 与 Line 必须连接" : (pointReady && lineReady ? "Point2D + Line2D 已连接 · 有值" : "几何输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildPointToLineInputSummaryS66(tool) + " → FootPoint / Distance / SignedSide / XLD";
        }

        private string BuildPointToLineInputSummaryS66(VmToolInstance tool)
        {
            VmToolInputBindingData pointBinding = tool == null ? null : tool.GetInputBinding("Point");
            VmToolInputBindingData lineBinding = tool == null ? null : tool.GetInputBinding("Line");
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            string point = pointSource == null || pointBinding == null ? "Point2D←未连接" : pointSource.InstanceName + "." + pointBinding.SourcePortName;
            string line = lineSource == null || lineBinding == null ? "Line2D←未连接" : lineSource.InstanceName + "." + lineBinding.SourcePortName;
            return point + " + " + line;
        }

        private InspectionRecord RunPointToLineDistanceToolS66(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.PointToLineDistanceMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData pointBinding = tool.GetInputBinding("Point");
            VmToolInputBindingData lineBinding = tool.GetInputBinding("Line");
            if (pointBinding == null || lineBinding == null) throw new InvalidOperationException("点到直线距离必须连接 Point (Point2D) 与 Line (Line2D)。");
            string pointBindingError = GetInputBindingConfigurationError(tool, pointBinding);
            string lineBindingError = GetInputBindingConfigurationError(tool, lineBinding);
            if (!string.IsNullOrWhiteSpace(pointBindingError)) throw new InvalidOperationException(pointBindingError);
            if (!string.IsNullOrWhiteSpace(lineBindingError)) throw new InvalidOperationException(lineBindingError);
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            if (pointSource == null || lineSource == null || !string.Equals(pointSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(lineSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Point2D 或 Line2D 上游尚未在本周期产生 OK 结果，禁止使用未运行或失效值。");
            object pointRaw;
            object lineRaw;
            if (!pointSource.TryGetOutputValue(pointBinding.SourcePortName, out pointRaw) || !(pointRaw is VmPoint2D)) throw new InvalidOperationException("Point 输入没有类型化 Point2D 运行值；不会解析字符串坐标。");
            if (!lineSource.TryGetOutputValue(lineBinding.SourcePortName, out lineRaw) || !(lineRaw is VmLine2D)) throw new InvalidOperationException("Line 输入没有类型化 Line2D 运行值；不会拼接多个 Number 端口。");
            VmPoint2D point = ((VmPoint2D)pointRaw).Clone();
            VmLine2D line = ((VmLine2D)lineRaw).Clone();
            string pointError = point.GetValidationError();
            string lineError = line.GetValidationError();
            if (!string.IsNullOrWhiteSpace(pointError)) throw new InvalidOperationException(pointError);
            if (!string.IsNullOrWhiteSpace(lineError)) throw new InvalidOperationException(lineError);
            if (!string.Equals(point.CoordinateContext, line.CoordinateContext, StringComparison.Ordinal) || !string.Equals(point.CoordinateContextId, line.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("Point2D 与 Line2D 坐标上下文不一致，禁止跨图像测量。");

            double rowDirection = line.EndRow - line.StartRow;
            double columnDirection = line.EndColumn - line.StartColumn;
            double lengthSquared = rowDirection * rowDirection + columnDirection * columnDirection;
            double projection = ((point.Row - line.StartRow) * rowDirection + (point.Column - line.StartColumn) * columnDirection) / lengthSquared;
            double footRow = line.StartRow + projection * rowDirection;
            double footColumn = line.StartColumn + projection * columnDirection;
            HTuple distanceTuple;
            HOperatorSet.DistancePl(point.Row, point.Column, line.StartRow, line.StartColumn, line.EndRow, line.EndColumn, out distanceTuple);
            double distance = distanceTuple == null || distanceTuple.Length == 0 ? 0 : distanceTuple.D;
            double leftSigned = (rowDirection * (point.Column - line.StartColumn) - columnDirection * (point.Row - line.StartRow)) / Math.Sqrt(lengthSquared);
            double signedSide = string.Equals(parameters.PointLineSideConvention, VmPointLineSideConvention.RightPositive, StringComparison.OrdinalIgnoreCase) ? -leftSigned : leftSigned;
            string side = Math.Abs(signedSide) <= 1e-9 ? "OnLine" : (signedSide > 0 ? "Positive" : "Negative");
            HObject overlays = null;
            HObject lineContour = null;
            HObject footLine = null;
            HObject crosses = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out lineContour, new HTuple(new[] { line.StartRow, line.EndRow }), new HTuple(new[] { line.StartColumn, line.EndColumn }));
                AppendOverlayObject(ref overlays, lineContour);
                HOperatorSet.GenContourPolygonXld(out footLine, new HTuple(new[] { point.Row, footRow }), new HTuple(new[] { point.Column, footColumn }));
                AppendOverlayObject(ref overlays, footLine);
                HOperatorSet.GenCrossContourXld(out crosses, new HTuple(new[] { point.Row, footRow }), new HTuple(new[] { point.Column, footColumn }), parameters.PointLineOverlayCrossSize, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, crosses);
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("FootPoint", VmPoint2D.CreateImagePixel(footRow, footColumn, point.CoordinateContextId));
                tool.SetOutputValue("Distance", distance);
                tool.SetOutputValue("SignedSide", signedSide);
                tool.SetOutputValue("Side", side);
                tool.SetOutputValue("CoordinateContext", point.CoordinateContext + ":" + point.CoordinateContextId);
                string parameterText = "SideConvention=" + parameters.PointLineSideConvention + ", CrossSize=" + parameters.PointLineOverlayCrossSize.ToString("0.###", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(overlays, tool, "XLD", 4, "OK", distance);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 点到直线距离：Point=({0:0.###},{1:0.###})，Foot=({2:0.###},{3:0.###})，Distance={4:0.###}px，SignedSide={5:0.###}px（{6}）· {7}",
                    point.Row, point.Column, footRow, footColumn, distance, signedSide, side, point.CoordinateContextId);
                InspectionRecord record = CreateRecord("PointToLineDistanceMeasurement", "OK", distance, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(crosses);
                DisposeObject(footLine);
                DisposeObject(lineContour);
                DisposeObject(overlays);
            }
        }
    }
}
