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
        private string recommendationSourceToolIdS67;
        private string recommendationSourcePortNameS67;
        private string recommendationSourceDataTypeS67;
        private string recommendationPreviousModeS67 = "All";

        private bool IsToolRecommendationContextActiveS67
        {
            get { return !string.IsNullOrWhiteSpace(recommendationSourceToolIdS67) && !string.IsNullOrWhiteSpace(recommendationSourcePortNameS67); }
        }

        private void EnterToolRecommendationContextS67(string sourceToolId, string sourcePortName)
        {
            if (IsInputSourceContextActiveS68) ExitInputSourceContextS68(false);
            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, sourceToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition sourcePort = source == null ? null : ToolMetadata.GetInstanceOutputPorts(source)
                .FirstOrDefault(port => string.Equals(port.PortName, sourcePortName, StringComparison.OrdinalIgnoreCase));
            if (source == null || sourcePort == null) return;

            if (!IsToolRecommendationContextActiveS67) recommendationPreviousModeS67 = toolCatalogMode;
            recommendationSourceToolIdS67 = source.ToolId;
            recommendationSourcePortNameS67 = sourcePort.PortName;
            recommendationSourceDataTypeS67 = sourcePort.DataType;
            RefreshToolCatalogView();
            VmToolCatalogItem first = toolCatalogView == null ? null : toolCatalogView.Cast<VmToolCatalogItem>().FirstOrDefault();
            if (first != null) ToolCatalogList.SelectedItem = first;
            RefreshToolRecommendationBannerS67();
        }

        private void ExitToolRecommendationContextS67(bool showFeedback)
        {
            recommendationSourceToolIdS67 = null;
            recommendationSourcePortNameS67 = null;
            recommendationSourceDataTypeS67 = null;
            toolCatalogMode = string.IsNullOrWhiteSpace(recommendationPreviousModeS67) ? "All" : recommendationPreviousModeS67;
            if (ToolSearchTextBox != null) ToolSearchTextBox.Text = string.Empty;
            RefreshToolCatalogView();
            RefreshToolRecommendationBannerS67();
            if (showFeedback) HeaderStatusText.Text = "已返回完整模块目录；搜索、常用、收藏和最近使用均可继续使用。";
        }

        private void ToolRecommendationBackButton_Click(object sender, RoutedEventArgs e)
        {
            ExitToolRecommendationContextS67(true);
        }

        private void ToolCatalogInsertRecommendationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InsertSelectedRecommendedToolS67();
        }

        private bool IsRecommendedToolS67(VmToolCatalogItem item)
        {
            VmPortDefinition targetPort;
            string rejection;
            return TryGetRecommendationTargetS67(item, out targetPort, out rejection);
        }

        private bool TryGetRecommendationTargetS67(VmToolCatalogItem item, out VmPortDefinition targetPort, out string rejection)
        {
            targetPort = null;
            rejection = string.Empty;
            if (!IsToolRecommendationContextActiveS67)
            {
                rejection = "未选择真实 OUT 端口。";
                return false;
            }
            VmToolInstance source = flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, recommendationSourceToolIdS67, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                rejection = "来源模块已删除。";
                return false;
            }
            if (!source.IsEnabled)
            {
                rejection = "来源模块已停用。";
                return false;
            }
            VmPortDefinition sourcePort = ToolMetadata.GetInstanceOutputPorts(source)
                .FirstOrDefault(port => string.Equals(port.PortName, recommendationSourcePortNameS67, StringComparison.OrdinalIgnoreCase));
            if (sourcePort == null)
            {
                rejection = "来源 OUT 端口已不存在。";
                return false;
            }
            if (item == null)
            {
                rejection = "未选择候选工具。";
                return false;
            }
            targetPort = ToolMetadata.GetInputPorts(item.Kind)
                .FirstOrDefault(port => !port.IsOptional && string.Equals(port.DataType, sourcePort.DataType, StringComparison.OrdinalIgnoreCase));
            if (targetPort == null)
            {
                rejection = "没有与 " + sourcePort.DataType + " 兼容的必需 IN 端口。";
                return false;
            }
            return true;
        }

        private void RefreshToolRecommendationBannerS67()
        {
            if (ToolRecommendationContextBorder == null) return;
            bool active = IsToolRecommendationContextActiveS67;
            ToolRecommendationContextBorder.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            if (ToolCatalogModePanel != null) ToolCatalogModePanel.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
            if (!active) return;

            VmToolInstance source = flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, recommendationSourceToolIdS67, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition sourcePort = source == null ? null : ToolMetadata.GetInstanceOutputPorts(source)
                .FirstOrDefault(port => string.Equals(port.PortName, recommendationSourcePortNameS67, StringComparison.OrdinalIgnoreCase));
            int total = toolCatalog.Count;
            int recommended = toolCatalog.Count(IsRecommendedToolS67);
            ToolRecommendationSourceText.Text = source == null || sourcePort == null
                ? "来源已失效"
                : source.InstanceName + "." + sourcePort.PortName + " : " + sourcePort.DataType + " · " + GetRecommendationSourceStateS67(source, sourcePort);
            ToolRecommendationRuleText.Text = recommended.ToString(CultureInfo.InvariantCulture) + " 个真实工具可接入 · 计划插在来源之后";
            ToolRecommendationRejectText.Text = "已排除 " + (total - recommended).ToString(CultureInfo.InvariantCulture) + " 个：无兼容必需 IN、来源停用或端口失效；不会伪造连接。";
        }

        private static string GetRecommendationSourceStateS67(VmToolInstance source, VmPortDefinition port)
        {
            object raw;
            if (source.TryGetOutputValue(port.PortName, out raw) && raw != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)) return "本周期有值";
            if (source.TryGetOutputValue(port.PortName, out raw) && raw != null) return "旧值已失效";
            return "等待运行";
        }

        private void RefreshRecommendationSelectionPreviewS67(VmToolCatalogItem item)
        {
            if (!IsToolRecommendationContextActiveS67 || ToolCapabilityInsertStatusText == null) return;
            VmPortDefinition targetPort;
            string rejection;
            if (!TryGetRecommendationTargetS67(item, out targetPort, out rejection))
            {
                ToolCapabilityInsertStatusText.Text = "拒绝原因：" + rejection;
                return;
            }
            VmToolInstance source = flowTools.First(tool => string.Equals(tool.ToolId, recommendationSourceToolIdS67, StringComparison.OrdinalIgnoreCase));
            List<string> remaining = ToolMetadata.GetInputPorts(item.Kind)
                .Where(port => !port.IsOptional && !string.Equals(port.PortName, targetPort.PortName, StringComparison.OrdinalIgnoreCase))
                .Select(port => port.PortName + ":" + port.DataType)
                .ToList();
            ToolCapabilityInsertAnchorText.Text = "推荐来源：" + source.InstanceName + "." + recommendationSourcePortNameS67 + " : " + recommendationSourceDataTypeS67;
            ToolCapabilityCompatibilityText.Text = "目标输入：" + targetPort.PortName + ":" + targetPort.DataType + " · 插入位置：来源之后";
            ToolCapabilityInsertStatusText.Text = remaining.Count == 0
                ? "双击或 Enter：插入、写回真实订阅并高亮新连接。"
                : "双击或 Enter：先连接所选 OUT；其余必需输入 " + string.Join("、", remaining) + " 将就近补接或明确保持待连接。";
        }

        private void InsertSelectedRecommendedToolS67()
        {
            VmToolCatalogItem catalogItem = ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem;
            VmPortDefinition targetPort;
            string rejection;
            if (!TryGetRecommendationTargetS67(catalogItem, out targetPort, out rejection))
            {
                SetInsertionFeedbackS66("推荐插入被拒绝：" + rejection, true);
                return;
            }
            if (productionRunMode || historicalImageMode)
            {
                SetInsertionFeedbackS66(productionRunMode ? "运行模式禁止编辑流程。" : "历史结果浏览为只读。", true);
                return;
            }
            VmToolInstance source = flowTools.First(tool => string.Equals(tool.ToolId, recommendationSourceToolIdS67, StringComparison.OrdinalIgnoreCase));
            int sourceIndex = flowTools.IndexOf(source);
            if (sourceIndex < 0)
            {
                SetInsertionFeedbackS66("推荐插入被拒绝：来源模块已离开当前流程。", true);
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            VmToolInstance inserted;
            suppressFlowInsertHistory = true;
            try { inserted = InsertCatalogTool(catalogItem, sourceIndex + 1, "按 OUT 端口推荐插入"); }
            finally { suppressFlowInsertHistory = false; }
            if (inserted == null) return;

            List<string> changes = new List<string>();
            if (!TrySetAutoBindingS66(inserted, targetPort, source.ToolId, recommendationSourcePortNameS67))
            {
                flowTools.Remove(inserted);
                SetInsertionFeedbackS66("推荐插入被拒绝：真实订阅校验未通过，流程保持不变。", true);
                return;
            }
            changes.Add(source.InstanceName + "." + recommendationSourcePortNameS67 + " → " + inserted.InstanceName + "." + targetPort.PortName);
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { source.ToolId + "|" + recommendationSourcePortNameS67 };
            foreach (VmPortDefinition input in ToolMetadata.GetInstanceInputPorts(inserted).Where(port => inserted.GetInputBinding(port.PortName) == null))
            {
                Tuple<VmToolInstance, VmPortDefinition> nearest = FindNearestCompatibleSourceS66(flowTools.IndexOf(inserted), input.DataType, used, source);
                if (nearest == null || !TrySetAutoBindingS66(inserted, input, nearest.Item1.ToolId, nearest.Item2.PortName)) continue;
                used.Add(nearest.Item1.ToolId + "|" + nearest.Item2.PortName);
                changes.Add(nearest.Item1.InstanceName + "." + nearest.Item2.PortName + " → " + inserted.InstanceName + "." + input.PortName);
            }

            VmToolInstance downstream = flowTools.ElementAtOrDefault(flowTools.IndexOf(inserted) + 1);
            if (downstream != null)
            {
                foreach (VmPortDefinition input in ToolMetadata.GetInstanceInputPorts(downstream).Where(port => !port.IsOptional && downstream.GetInputBinding(port.PortName) == null))
                {
                    VmPortDefinition output = ToolMetadata.GetInstanceOutputPorts(inserted).FirstOrDefault(port => string.Equals(port.DataType, input.DataType, StringComparison.OrdinalIgnoreCase));
                    if (output == null || !TrySetAutoBindingS66(downstream, input, inserted.ToolId, output.PortName)) continue;
                    changes.Add(inserted.InstanceName + "." + output.PortName + " → " + downstream.InstanceName + "." + input.PortName);
                }
            }

            InvalidateToolRunResult(inserted, "推荐模块已插入，等待运行");
            InvalidateDownstreamResults(inserted, "推荐插入位置和订阅已变化");
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("端口推荐插入模块：" + inserted.InstanceName, before);
            FlowToolList.SelectedItems.Clear();
            FlowToolList.SelectedItem = inserted;
            FlowToolList.ScrollIntoView(inserted);
            selectedFlowConnectionKey = BuildFlowConnectionKey(source.ToolId, recommendationSourcePortNameS67, inserted.ToolId, targetPort.PortName);
            RefreshUiState();
            SetInsertionFeedbackS66(inserted.InstanceName + " 已插在 " + source.InstanceName + " 之后；已写回 " + changes.Count.ToString(CultureInfo.InvariantCulture) + " 条真实订阅：" + string.Join("；", changes), false);
        }

        private void RefreshPointToCircleConnectionStatusS67(VmToolInstance tool)
        {
            VmToolInputBindingData pointBinding = tool == null ? null : tool.GetInputBinding("Point");
            VmToolInputBindingData circleBinding = tool == null ? null : tool.GetInputBinding("Circle");
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance circleSource = GetInputSourceTool(circleBinding);
            object pointRaw;
            object circleRaw;
            bool pointReady = pointSource != null && string.Equals(pointSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && pointSource.TryGetOutputValue(pointBinding.SourcePortName, out pointRaw) && pointRaw is VmPoint2D;
            bool circleReady = circleSource != null && string.Equals(circleSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && circleSource.TryGetOutputValue(circleBinding.SourcePortName, out circleRaw) && circleRaw is VmCircle2D;
            tool.ConfigurationStatus = pointBinding == null || circleBinding == null ? "待连接几何" : (pointReady && circleReady ? "就绪" : "等待上游");
            tool.ConnectionStatus = pointBinding == null || circleBinding == null ? "Point 与 Circle 必须连接" : (pointReady && circleReady ? "Point2D + Circle2D 已连接 · 有值" : "几何输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildPointToCircleInputSummaryS67(tool) + " → ClosestPoint / RadialDeviation / XLD";
        }

        private string BuildPointToCircleInputSummaryS67(VmToolInstance tool)
        {
            VmToolInputBindingData pointBinding = tool == null ? null : tool.GetInputBinding("Point");
            VmToolInputBindingData circleBinding = tool == null ? null : tool.GetInputBinding("Circle");
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance circleSource = GetInputSourceTool(circleBinding);
            string point = pointSource == null || pointBinding == null ? "Point2D←未连接" : pointSource.InstanceName + "." + pointBinding.SourcePortName;
            string circle = circleSource == null || circleBinding == null ? "Circle2D←未连接" : circleSource.InstanceName + "." + circleBinding.SourcePortName;
            return point + " + " + circle;
        }

        private InspectionRecord RunPointToCircleRadialDeviationToolS67(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.PointToCircleRadialDeviationMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData pointBinding = tool.GetInputBinding("Point");
            VmToolInputBindingData circleBinding = tool.GetInputBinding("Circle");
            if (pointBinding == null || circleBinding == null) throw new InvalidOperationException("点到圆径向偏差必须连接 Point (Point2D) 与 Circle (Circle2D)。");
            string pointBindingError = GetInputBindingConfigurationError(tool, pointBinding);
            string circleBindingError = GetInputBindingConfigurationError(tool, circleBinding);
            if (!string.IsNullOrWhiteSpace(pointBindingError)) throw new InvalidOperationException(pointBindingError);
            if (!string.IsNullOrWhiteSpace(circleBindingError)) throw new InvalidOperationException(circleBindingError);
            VmToolInstance pointSource = GetInputSourceTool(pointBinding);
            VmToolInstance circleSource = GetInputSourceTool(circleBinding);
            if (pointSource == null || circleSource == null || !string.Equals(pointSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(circleSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Point2D 或 Circle2D 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object pointRaw;
            object circleRaw;
            if (!pointSource.TryGetOutputValue(pointBinding.SourcePortName, out pointRaw) || !(pointRaw is VmPoint2D)) throw new InvalidOperationException("Point 输入没有类型化 Point2D 运行值。");
            if (!circleSource.TryGetOutputValue(circleBinding.SourcePortName, out circleRaw) || !(circleRaw is VmCircle2D)) throw new InvalidOperationException("Circle 输入没有类型化 Circle2D 运行值；不会拼接圆心和半径标量。");
            VmPoint2D point = ((VmPoint2D)pointRaw).Clone();
            VmCircle2D circle = ((VmCircle2D)circleRaw).Clone();
            string pointError = point.GetValidationError();
            string circleError = circle.GetValidationError();
            if (!string.IsNullOrWhiteSpace(pointError)) throw new InvalidOperationException(pointError);
            if (!string.IsNullOrWhiteSpace(circleError)) throw new InvalidOperationException(circleError);
            if (!string.Equals(point.CoordinateContext, circle.CoordinateContext, StringComparison.Ordinal) || !string.Equals(point.CoordinateContextId, circle.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("Point2D 与 Circle2D 坐标上下文不一致，禁止跨图像测量。");

            double deltaRow = point.Row - circle.CenterRow;
            double deltaColumn = point.Column - circle.CenterColumn;
            double centerDistance = Math.Sqrt(deltaRow * deltaRow + deltaColumn * deltaColumn);
            if (double.IsNaN(centerDistance) || double.IsInfinity(centerDistance)) throw new InvalidOperationException("点到圆心距离不是有限数值。");
            if (centerDistance <= 1e-9) throw new InvalidOperationException("点与圆心重合，最近圆周点方向不唯一，测量被拒绝。");
            double closestRow = circle.CenterRow + circle.Radius * deltaRow / centerDistance;
            double closestColumn = circle.CenterColumn + circle.Radius * deltaColumn / centerDistance;
            double outsidePositive = centerDistance - circle.Radius;
            double signedDeviation = string.Equals(parameters.PointCircleSignConvention, VmPointCircleSignConvention.InsidePositive, StringComparison.OrdinalIgnoreCase) ? -outsidePositive : outsidePositive;
            double absoluteDeviation = Math.Abs(outsidePositive);
            string radialState = Math.Abs(outsidePositive) <= 1e-9 ? "OnCircle" : (outsidePositive > 0 ? "Outside" : "Inside");

            HObject overlays = null;
            HObject circleContour = null;
            HObject radialLine = null;
            HObject crosses = null;
            try
            {
                HOperatorSet.GenCircleContourXld(out circleContour, circle.CenterRow, circle.CenterColumn, circle.Radius, 0.0, Math.PI * 2.0, "positive", 1.0);
                AppendOverlayObject(ref overlays, circleContour);
                HOperatorSet.GenContourPolygonXld(out radialLine, new HTuple(new[] { circle.CenterRow, closestRow, point.Row }), new HTuple(new[] { circle.CenterColumn, closestColumn, point.Column }));
                AppendOverlayObject(ref overlays, radialLine);
                HOperatorSet.GenCrossContourXld(out crosses, new HTuple(new[] { circle.CenterRow, closestRow, point.Row }), new HTuple(new[] { circle.CenterColumn, closestColumn, point.Column }), parameters.PointCircleOverlayCrossSize, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, crosses);
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("ClosestPoint", VmPoint2D.CreateImagePixel(closestRow, closestColumn, point.CoordinateContextId));
                tool.SetOutputValue("CenterDistance", centerDistance);
                tool.SetOutputValue("AbsoluteDeviation", absoluteDeviation);
                tool.SetOutputValue("SignedDeviation", signedDeviation);
                tool.SetOutputValue("RadialState", radialState);
                tool.SetOutputValue("CoordinateContext", point.CoordinateContext + ":" + point.CoordinateContextId);
                string parameterText = "SignConvention=" + parameters.PointCircleSignConvention + ", CrossSize=" + parameters.PointCircleOverlayCrossSize.ToString("0.###", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(overlays, tool, "XLD", 5, "OK", absoluteDeviation);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 点到圆径向偏差：Point=({0:0.###},{1:0.###})，Circle=({2:0.###},{3:0.###},R={4:0.###})，Closest=({5:0.###},{6:0.###})，CenterDistance={7:0.###}px，AbsDeviation={8:0.###}px，Signed={9:0.###}px（{10}）· {11}",
                    point.Row, point.Column, circle.CenterRow, circle.CenterColumn, circle.Radius, closestRow, closestColumn, centerDistance, absoluteDeviation, signedDeviation, radialState, point.CoordinateContextId);
                InspectionRecord record = CreateRecord("PointToCircleRadialDeviationMeasurement", "OK", absoluteDeviation, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(crosses);
                DisposeObject(radialLine);
                DisposeObject(circleContour);
                DisposeObject(overlays);
            }
        }
    }
}
