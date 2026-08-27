using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class VmInputSourceSuggestionS68
        {
            public string Key { get; set; }
            public string SourceToolId { get; set; }
            public string SourcePortName { get; set; }
            public string DisplayText { get; set; }
            public string StatusText { get; set; }
            public string RejectionText { get; set; }
            public string DataType { get; set; }
            public bool IsValid { get; set; }
            public int SourceIndex { get; set; }
        }

        private readonly ObservableCollection<VmInputSourceSuggestionS68> inputSourceSuggestionsS68 = new ObservableCollection<VmInputSourceSuggestionS68>();
        private string inputSourceTargetToolIdS68;
        private string inputSourceTargetPortNameS68;
        private string inputSourceTargetDataTypeS68;
        private string inputSourcePreviousModeS68 = "All";

        private bool IsInputSourceContextActiveS68
        {
            get { return !string.IsNullOrWhiteSpace(inputSourceTargetToolIdS68) && !string.IsNullOrWhiteSpace(inputSourceTargetPortNameS68); }
        }

        private bool HasValidInputSourceSuggestionsS68
        {
            get { return inputSourceSuggestionsS68.Any(item => item.IsValid); }
        }

        private bool EnterInputSourceContextS68(string targetToolId, string targetPortName)
        {
            VmToolInstance target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, targetToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition targetPort = target == null ? null : ToolMetadata.GetInstanceInputPorts(target)
                .FirstOrDefault(port => string.Equals(port.PortName, targetPortName, StringComparison.OrdinalIgnoreCase));
            if (target == null || targetPort == null) return false;

            VmToolInputBindingData binding = target.GetInputBinding(targetPort.PortName);
            string error = binding == null
                ? (targetPort.IsOptional ? "可选输入尚未连接。" : "必需输入尚未连接。")
                : (target.Kind == VmToolKind.NumericJudge
                    ? GetNumericJudgeConfigurationError(target)
                    : GetInputBindingConfigurationError(target, binding));
            if (binding != null && string.IsNullOrWhiteSpace(error))
            {
                if (IsInputSourceContextActiveS68) ExitInputSourceContextS68(false);
                return false;
            }

            if (IsToolRecommendationContextActiveS67) ExitToolRecommendationContextS67(false);
            if (!IsInputSourceContextActiveS68) inputSourcePreviousModeS68 = toolCatalogMode;
            inputSourceTargetToolIdS68 = target.ToolId;
            inputSourceTargetPortNameS68 = targetPort.PortName;
            inputSourceTargetDataTypeS68 = targetPort.DataType;
            RebuildInputSourceSuggestionsS68();
            RefreshToolCatalogView();
            VmInputSourceSuggestionS68 first = inputSourceSuggestionsS68.FirstOrDefault(item => item.IsValid) ?? inputSourceSuggestionsS68.FirstOrDefault();
            if (ToolInputSourceSuggestionList != null) ToolInputSourceSuggestionList.SelectedItem = first;
            if (!HasValidInputSourceSuggestionsS68 && ToolCatalogList != null)
                ToolCatalogList.SelectedItem = toolCatalogView.Cast<VmToolCatalogItem>().FirstOrDefault();
            RefreshInputSourceContextBannerS68();
            return true;
        }

        private void ExitInputSourceContextS68(bool showFeedback)
        {
            inputSourceTargetToolIdS68 = null;
            inputSourceTargetPortNameS68 = null;
            inputSourceTargetDataTypeS68 = null;
            inputSourceSuggestionsS68.Clear();
            toolCatalogMode = string.IsNullOrWhiteSpace(inputSourcePreviousModeS68) ? "All" : inputSourcePreviousModeS68;
            if (ToolSearchTextBox != null) ToolSearchTextBox.Text = string.Empty;
            RefreshToolCatalogView();
            RefreshInputSourceContextBannerS68();
            if (showFeedback) HeaderStatusText.Text = "已返回完整模块目录；现有来源、创建来源和完整工具浏览状态均已退出。";
        }

        private void ToolInputSourceBackButton_Click(object sender, RoutedEventArgs e)
        {
            ExitInputSourceContextS68(true);
        }

        private void RebuildInputSourceSuggestionsS68()
        {
            inputSourceSuggestionsS68.Clear();
            if (!IsInputSourceContextActiveS68) return;
            VmToolInstance target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, inputSourceTargetToolIdS68, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition targetPort = target == null ? null : ToolMetadata.GetInstanceInputPorts(target)
                .FirstOrDefault(port => string.Equals(port.PortName, inputSourceTargetPortNameS68, StringComparison.OrdinalIgnoreCase));
            if (target == null || targetPort == null) return;
            int targetIndex = flowTools.IndexOf(target);
            List<VmInputSourceSuggestionS68> candidates = new List<VmInputSourceSuggestionS68>();
            for (int sourceIndex = 0; sourceIndex < flowTools.Count; sourceIndex++)
            {
                VmToolInstance source = flowTools[sourceIndex];
                if (ReferenceEquals(source, target)) continue;
                foreach (VmPortDefinition sourcePort in ToolMetadata.GetInstanceOutputPorts(source)
                    .Where(port => string.Equals(port.DataType, targetPort.DataType, StringComparison.OrdinalIgnoreCase)))
                {
                    List<string> rejections = new List<string>();
                    if (sourceIndex >= targetIndex) rejections.Add("位于目标之后");
                    if (!source.IsEnabled) rejections.Add("来源已停用");
                    if (IsDuplicateMultiInputEndpoint(target, targetPort.PortName, source.ToolId, sourcePort.PortName)) rejections.Add("与另一输入重复");
                    bool valid = rejections.Count == 0;
                    int consumers = GetFlowPortConsumerCount(source, sourcePort.PortName);
                    string runtime = GetRecommendationSourceStateS67(source, sourcePort);
                    candidates.Add(new VmInputSourceSuggestionS68
                    {
                        Key = BuildDockSourceKey(source.ToolId, sourcePort.PortName),
                        SourceToolId = source.ToolId,
                        SourcePortName = sourcePort.PortName,
                        DataType = sourcePort.DataType,
                        SourceIndex = sourceIndex,
                        IsValid = valid,
                        RejectionText = valid ? string.Empty : string.Join("、", rejections),
                        DisplayText = source.SequenceText + "  " + source.InstanceName + "." + sourcePort.PortName,
                        StatusText = valid
                            ? sourcePort.DataType + " · " + runtime + " · 消费者 " + consumers.ToString(CultureInfo.InvariantCulture) + " · 可绑定"
                            : sourcePort.DataType + " · 拒绝：" + string.Join("、", rejections)
                    });
                }
            }
            foreach (VmInputSourceSuggestionS68 item in candidates.OrderByDescending(item => item.IsValid).ThenByDescending(item => item.SourceIndex).ThenBy(item => item.DisplayText, StringComparer.Ordinal))
                inputSourceSuggestionsS68.Add(item);
        }

        private void RefreshInputSourceContextBannerS68()
        {
            if (ToolInputSourceContextBorder == null) return;
            bool active = IsInputSourceContextActiveS68;
            ToolInputSourceContextBorder.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            if (active && ToolCatalogModePanel != null) ToolCatalogModePanel.Visibility = Visibility.Collapsed;
            if (!active) return;

            VmToolInstance target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, inputSourceTargetToolIdS68, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition port = target == null ? null : ToolMetadata.GetInstanceInputPorts(target)
                .FirstOrDefault(item => string.Equals(item.PortName, inputSourceTargetPortNameS68, StringComparison.OrdinalIgnoreCase));
            int valid = inputSourceSuggestionsS68.Count(item => item.IsValid);
            int rejected = inputSourceSuggestionsS68.Count - valid;
            ToolInputSourceTargetText.Text = target == null || port == null
                ? "目标已失效"
                : target.SequenceText + "  " + target.InstanceName + "." + port.PortName + " : " + port.DataType;
            ToolInputSourceRuleText.Text = valid > 0
                ? valid.ToString(CultureInfo.InvariantCulture) + " 个现有前序来源可直接绑定 · 优先复用现有结果"
                : "没有可用现有来源 · 下方仅显示可在目标前创建的真实工具";
            ToolInputSourceRejectText.Text = rejected == 0
                ? "顺序、启用状态和多输入来源约束均满足。"
                : "另有 " + rejected.ToString(CultureInfo.InvariantCulture) + " 个同类型端点被拒绝；逐项显示原因。";
            ToolInputSourceSuggestionList.Visibility = inputSourceSuggestionsS68.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            BindSelectedInputSourceButton.Visibility = valid > 0 ? Visibility.Visible : Visibility.Collapsed;
            ToolInputSourceCreateHintText.Visibility = valid == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsInputSourceProducerToolS68(VmToolCatalogItem item)
        {
            VmPortDefinition output;
            string rejection;
            return TryGetInputSourceProducerOutputS68(item, out output, out rejection);
        }

        private bool TryGetInputSourceProducerOutputS68(VmToolCatalogItem item, out VmPortDefinition output, out string rejection)
        {
            output = null;
            rejection = string.Empty;
            if (!IsInputSourceContextActiveS68)
            {
                rejection = "未选择缺失或异常 IN 端口。";
                return false;
            }
            if (item == null)
            {
                rejection = "未选择候选工具。";
                return false;
            }
            output = ToolMetadata.GetOutputPorts(item.Kind)
                .FirstOrDefault(port => string.Equals(port.DataType, inputSourceTargetDataTypeS68, StringComparison.OrdinalIgnoreCase));
            if (output == null)
            {
                rejection = "该工具不产生 " + inputSourceTargetDataTypeS68 + " 输出。";
                return false;
            }
            return true;
        }

        private void RefreshInputSourceCatalogSelectionPreviewS68(VmToolCatalogItem item)
        {
            if (!IsInputSourceContextActiveS68 || HasValidInputSourceSuggestionsS68 || ToolCapabilityInsertStatusText == null) return;
            VmPortDefinition output;
            string rejection;
            if (!TryGetInputSourceProducerOutputS68(item, out output, out rejection))
            {
                ToolCapabilityInsertStatusText.Text = "创建来源被拒绝：" + rejection;
                return;
            }
            VmToolInstance target = flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, inputSourceTargetToolIdS68, StringComparison.OrdinalIgnoreCase));
            ToolCapabilityInsertAnchorText.Text = "待补输入：" + (target == null ? "目标已失效" : target.InstanceName + "." + inputSourceTargetPortNameS68 + " : " + inputSourceTargetDataTypeS68);
            ToolCapabilityCompatibilityText.Text = "创建输出：" + item.Name + "." + output.PortName + ":" + output.DataType + " · 插在目标之前";
            ToolCapabilityInsertStatusText.Text = "双击或 Enter：创建来源、补齐可推断输入并写回目标真实订阅。";
        }

        private void ToolInputSourceSuggestionList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BindSelectedInputSourceS68();
        }

        private void BindSelectedInputSourceButton_Click(object sender, RoutedEventArgs e)
        {
            BindSelectedInputSourceS68();
        }

        private void BindSelectedInputSourceS68()
        {
            if (productionRunMode || historicalImageMode)
            {
                HeaderStatusText.Text = productionRunMode ? "运行模式禁止编辑流程。" : "历史结果浏览为只读。";
                return;
            }
            VmInputSourceSuggestionS68 selected = ToolInputSourceSuggestionList == null ? null : ToolInputSourceSuggestionList.SelectedItem as VmInputSourceSuggestionS68;
            if (selected == null)
            {
                HeaderStatusText.Text = "请选择一个现有上游来源。";
                return;
            }
            if (!selected.IsValid)
            {
                ToolInputSourceRejectText.Text = "所选端点不可绑定：" + selected.RejectionText;
                HeaderStatusText.Text = "补源被拒绝：" + selected.RejectionText;
                return;
            }
            VmToolInstance target;
            VmPortDefinition port;
            if (!TryGetInputSourceTargetS68(out target, out port))
            {
                HeaderStatusText.Text = "目标 IN 端口已失效。";
                return;
            }
            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selected.SourceToolId, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                HeaderStatusText.Text = "所选来源模块已删除。";
                return;
            }

            VmToolInputBindingData previous = target.GetInputBinding(port.PortName);
            previous = previous == null ? null : previous.Clone();
            string previousTool = target.InputToolId;
            string previousPort = target.InputPortName;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            SetSelectedFlowPortBinding(target, port, source.ToolId, selected.SourcePortName);
            VmToolInputBindingData candidate = target.GetInputBinding(port.PortName);
            string error = target.Kind == VmToolKind.NumericJudge ? GetNumericJudgeConfigurationError(target) : GetInputBindingConfigurationError(target, candidate);
            if (!string.IsNullOrWhiteSpace(error))
            {
                RestoreSelectedFlowPortBinding(target, port, previous, previousTool, previousPort);
                HeaderStatusText.Text = "补源失败，原订阅已保留：" + error;
                return;
            }

            InvalidateToolRunResult(target, "输入端口已从补源导航绑定");
            InvalidateDownstreamResults(target, "上游输入来源已变化");
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("补充上游来源：" + target.InstanceName + "." + port.PortName, before);
            selectedFlowConnectionKey = BuildFlowConnectionKey(source.ToolId, selected.SourcePortName, target.ToolId, port.PortName);
            LogInfo("IN 补源已绑定：" + source.InstanceName + "." + selected.SourcePortName + " → " + target.InstanceName + "." + port.PortName);
            string message = "已复用现有来源并写回真实 InputBindings：" + source.InstanceName + "." + selected.SourcePortName + " → " + target.InstanceName + "." + port.PortName;
            ExitInputSourceContextS68(false);
            RefreshUiState();
            HeaderStatusText.Text = message;
        }

        private bool TryGetInputSourceTargetS68(out VmToolInstance target, out VmPortDefinition port)
        {
            target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, inputSourceTargetToolIdS68, StringComparison.OrdinalIgnoreCase));
            port = target == null ? null : ToolMetadata.GetInstanceInputPorts(target)
                .FirstOrDefault(item => string.Equals(item.PortName, inputSourceTargetPortNameS68, StringComparison.OrdinalIgnoreCase));
            return target != null && port != null;
        }

        private void ToolCatalogInsertInputSourceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InsertSelectedInputSourceToolS68();
        }

        private void InsertSelectedInputSourceToolS68()
        {
            if (productionRunMode || historicalImageMode)
            {
                HeaderStatusText.Text = productionRunMode ? "运行模式禁止编辑流程。" : "历史结果浏览为只读。";
                return;
            }
            if (HasValidInputSourceSuggestionsS68)
            {
                HeaderStatusText.Text = "已有可直接绑定的前序来源；请先复用现有结果，避免无必要地增加模块。";
                return;
            }
            VmToolInstance target;
            VmPortDefinition targetPort;
            if (!TryGetInputSourceTargetS68(out target, out targetPort))
            {
                HeaderStatusText.Text = "目标 IN 端口已失效。";
                return;
            }
            VmToolCatalogItem catalogItem = ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem;
            VmPortDefinition producerOutput;
            string rejection;
            if (!TryGetInputSourceProducerOutputS68(catalogItem, out producerOutput, out rejection))
            {
                HeaderStatusText.Text = "创建来源被拒绝：" + rejection;
                return;
            }

            int targetIndex = flowTools.IndexOf(target);
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            VmToolInstance inserted;
            suppressFlowInsertHistory = true;
            try { inserted = InsertCatalogTool(catalogItem, targetIndex, "按 IN 端口补源插入"); }
            finally { suppressFlowInsertHistory = false; }
            if (inserted == null) return;

            List<string> changes = new List<string>();
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (VmPortDefinition input in ToolMetadata.GetInstanceInputPorts(inserted).Where(port => inserted.GetInputBinding(port.PortName) == null))
            {
                Tuple<VmToolInstance, VmPortDefinition> nearest = FindNearestCompatibleSourceS66(flowTools.IndexOf(inserted), input.DataType, used, target);
                if (nearest == null || !TrySetAutoBindingS66(inserted, input, nearest.Item1.ToolId, nearest.Item2.PortName)) continue;
                used.Add(nearest.Item1.ToolId + "|" + nearest.Item2.PortName);
                changes.Add(nearest.Item1.InstanceName + "." + nearest.Item2.PortName + " → " + inserted.InstanceName + "." + input.PortName);
            }
            VmPortDefinition actualOutput = ToolMetadata.GetInstanceOutputPorts(inserted)
                .FirstOrDefault(port => string.Equals(port.PortName, producerOutput.PortName, StringComparison.OrdinalIgnoreCase) && string.Equals(port.DataType, targetPort.DataType, StringComparison.OrdinalIgnoreCase));
            if (actualOutput == null || !TrySetAutoBindingS66(target, targetPort, inserted.ToolId, actualOutput.PortName))
            {
                flowTools.Remove(inserted);
                HeaderStatusText.Text = "创建来源失败：新工具输出未通过目标真实订阅校验，流程保持不变。";
                return;
            }
            changes.Add(inserted.InstanceName + "." + actualOutput.PortName + " → " + target.InstanceName + "." + targetPort.PortName);
            InvalidateToolRunResult(inserted, "补源模块已插入，等待运行");
            InvalidateToolRunResult(target, "输入来源工具已插入");
            InvalidateDownstreamResults(target, "上游输入来源已变化");
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("创建上游来源：" + inserted.InstanceName, before);
            FlowToolList.SelectedItems.Clear();
            FlowToolList.SelectedItem = inserted;
            FlowToolList.ScrollIntoView(inserted);
            selectedFlowConnectionKey = BuildFlowConnectionKey(inserted.ToolId, actualOutput.PortName, target.ToolId, targetPort.PortName);
            LogInfo("IN 补源已创建：" + string.Join("；", changes));
            string message = inserted.InstanceName + " 已插在 " + target.InstanceName + " 之前并连接 " + targetPort.PortName + "；共写回 " + changes.Count.ToString(CultureInfo.InvariantCulture) + " 条真实订阅。";
            ExitInputSourceContextS68(false);
            RefreshUiState();
            HeaderStatusText.Text = message;
        }

        private void RefreshLineCircleConnectionStatusS68(VmToolInstance tool)
        {
            VmToolInputBindingData lineBinding = tool == null ? null : tool.GetInputBinding("Line");
            VmToolInputBindingData circleBinding = tool == null ? null : tool.GetInputBinding("Circle");
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            VmToolInstance circleSource = GetInputSourceTool(circleBinding);
            object lineRaw;
            object circleRaw;
            bool lineReady = lineSource != null && string.Equals(lineSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && lineSource.TryGetOutputValue(lineBinding.SourcePortName, out lineRaw) && lineRaw is VmLine2D;
            bool circleReady = circleSource != null && string.Equals(circleSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && circleSource.TryGetOutputValue(circleBinding.SourcePortName, out circleRaw) && circleRaw is VmCircle2D;
            tool.ConfigurationStatus = lineBinding == null || circleBinding == null ? "待连接几何" : (lineReady && circleReady ? "就绪" : "等待上游");
            tool.ConnectionStatus = lineBinding == null || circleBinding == null ? "Line 与 Circle 必须连接" : (lineReady && circleReady ? "Line2D + Circle2D 已连接 · 有值" : "几何输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildLineCircleInputSummaryS68(tool) + " → Point2D[] / Relation / ChordLength / XLD";
        }

        private string BuildLineCircleInputSummaryS68(VmToolInstance tool)
        {
            VmToolInputBindingData lineBinding = tool == null ? null : tool.GetInputBinding("Line");
            VmToolInputBindingData circleBinding = tool == null ? null : tool.GetInputBinding("Circle");
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            VmToolInstance circleSource = GetInputSourceTool(circleBinding);
            string line = lineSource == null || lineBinding == null ? "Line2D←未连接" : lineSource.InstanceName + "." + lineBinding.SourcePortName;
            string circle = circleSource == null || circleBinding == null ? "Circle2D←未连接" : circleSource.InstanceName + "." + circleBinding.SourcePortName;
            return line + " + " + circle;
        }

        private InspectionRecord RunLineCircleIntersectionToolS68(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.LineCircleIntersectionMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData lineBinding = tool.GetInputBinding("Line");
            VmToolInputBindingData circleBinding = tool.GetInputBinding("Circle");
            if (lineBinding == null || circleBinding == null) throw new InvalidOperationException("直线圆交点测量必须连接 Line (Line2D) 与 Circle (Circle2D)。");
            string lineBindingError = GetInputBindingConfigurationError(tool, lineBinding);
            string circleBindingError = GetInputBindingConfigurationError(tool, circleBinding);
            if (!string.IsNullOrWhiteSpace(lineBindingError)) throw new InvalidOperationException(lineBindingError);
            if (!string.IsNullOrWhiteSpace(circleBindingError)) throw new InvalidOperationException(circleBindingError);
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            VmToolInstance circleSource = GetInputSourceTool(circleBinding);
            if (lineSource == null || circleSource == null || !string.Equals(lineSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(circleSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Line2D 或 Circle2D 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object lineRaw;
            object circleRaw;
            if (!lineSource.TryGetOutputValue(lineBinding.SourcePortName, out lineRaw) || !(lineRaw is VmLine2D)) throw new InvalidOperationException("Line 输入没有类型化 Line2D 运行值。");
            if (!circleSource.TryGetOutputValue(circleBinding.SourcePortName, out circleRaw) || !(circleRaw is VmCircle2D)) throw new InvalidOperationException("Circle 输入没有类型化 Circle2D 运行值。");
            VmLine2D line = ((VmLine2D)lineRaw).Clone();
            VmCircle2D circle = ((VmCircle2D)circleRaw).Clone();
            string lineError = line.GetValidationError();
            string circleError = circle.GetValidationError();
            if (!string.IsNullOrWhiteSpace(lineError)) throw new InvalidOperationException(lineError);
            if (!string.IsNullOrWhiteSpace(circleError)) throw new InvalidOperationException(circleError);
            if (!string.Equals(line.CoordinateContext, circle.CoordinateContext, StringComparison.Ordinal) || !string.Equals(line.CoordinateContextId, circle.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("Line2D 与 Circle2D 坐标上下文不一致，禁止跨图像求交。");

            HTuple intersectionRows = null;
            HTuple intersectionColumns = null;
            HObject overlays = null;
            HObject lineContour = null;
            HObject circleContour = null;
            HObject crosses = null;
            try
            {
                HOperatorSet.IntersectionLineCircle(
                    line.StartRow, line.StartColumn, line.EndRow, line.EndColumn,
                    circle.CenterRow, circle.CenterColumn, circle.Radius,
                    0.0, Math.PI * 2.0, "positive",
                    out intersectionRows, out intersectionColumns);
                if (intersectionRows == null || intersectionColumns == null || intersectionRows.Length != intersectionColumns.Length)
                    throw new InvalidOperationException("HALCON intersection_line_circle 返回了不一致的 Row/Column 交点集合。");

                double directionRow = line.EndRow - line.StartRow;
                double directionColumn = line.EndColumn - line.StartColumn;
                double lengthSquared = directionRow * directionRow + directionColumn * directionColumn;
                List<Tuple<double, double, double>> ordered = new List<Tuple<double, double, double>>();
                for (int index = 0; index < intersectionRows.Length; index++)
                {
                    double row = intersectionRows[index].D;
                    double column = intersectionColumns[index].D;
                    if (double.IsNaN(row) || double.IsInfinity(row) || double.IsNaN(column) || double.IsInfinity(column))
                        throw new InvalidOperationException("HALCON 返回了非有限交点坐标。");
                    double projection = ((row - line.StartRow) * directionRow + (column - line.StartColumn) * directionColumn) / lengthSquared;
                    ordered.Add(Tuple.Create(row, column, projection));
                }
                ordered = string.Equals(parameters.LineCircleIntersectionOrder, VmLineCircleIntersectionOrder.ReverseAlongLine, StringComparison.OrdinalIgnoreCase)
                    ? ordered.OrderByDescending(item => item.Item3).ToList()
                    : ordered.OrderBy(item => item.Item3).ToList();

                double[] rows = ordered.Select(item => item.Item1).ToArray();
                double[] columns = ordered.Select(item => item.Item2).ToArray();
                VmPoint2DArray points = VmPoint2DArray.CreateImagePixel(rows, columns, line.CoordinateContextId);
                int count = points.Count;
                string relation = count == 0 ? "Separated" : (count == 1 ? "Tangent" : "Secant");
                double chordLength = count < 2 ? 0.0 : Math.Sqrt(Math.Pow(rows[1] - rows[0], 2) + Math.Pow(columns[1] - columns[0], 2));
                string resultCode = count == 0 ? "NG" : "OK";

                HOperatorSet.GenContourPolygonXld(out lineContour, new HTuple(new[] { line.StartRow, line.EndRow }), new HTuple(new[] { line.StartColumn, line.EndColumn }));
                AppendOverlayObject(ref overlays, lineContour);
                HOperatorSet.GenCircleContourXld(out circleContour, circle.CenterRow, circle.CenterColumn, circle.Radius, 0.0, Math.PI * 2.0, "positive", 1.0);
                AppendOverlayObject(ref overlays, circleContour);
                if (count > 0)
                {
                    HOperatorSet.GenCrossContourXld(out crosses, new HTuple(rows), new HTuple(columns), parameters.LineCircleOverlayCrossSize, Math.PI / 4.0);
                    AppendOverlayObject(ref overlays, crosses);
                }

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("IntersectionPoints", points);
                if (count > 0) tool.SetOutputValue("PointA", points.Points[0].Clone());
                if (count > 1) tool.SetOutputValue("PointB", points.Points[1].Clone());
                tool.SetOutputValue("IntersectionCount", (double)count);
                tool.SetOutputValue("ChordLength", chordLength);
                tool.SetOutputValue("Relation", relation);
                tool.SetOutputValue("CoordinateContext", line.CoordinateContext + ":" + line.CoordinateContextId);
                string parameterText = "HALCON=intersection_line_circle, LineMode=Infinite, Order=" + parameters.LineCircleIntersectionOrder + ", CrossSize=" + parameters.LineCircleOverlayCrossSize.ToString("0.###", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(overlays, tool, "XLD", 2 + count, resultCode, chordLength);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 直线圆交点：Relation={0}，Count={1}，ChordLength={2:0.###}px，Line=({3:0.###},{4:0.###})→({5:0.###},{6:0.###})，Circle=({7:0.###},{8:0.###},R={9:0.###}) · {10}",
                    relation, count, chordLength, line.StartRow, line.StartColumn, line.EndRow, line.EndColumn, circle.CenterRow, circle.CenterColumn, circle.Radius, line.CoordinateContextId);
                InspectionRecord record = CreateRecord("LineCircleIntersectionMeasurement", resultCode, chordLength, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (intersectionRows != null) intersectionRows.Dispose();
                if (intersectionColumns != null) intersectionColumns.Dispose();
                DisposeObject(crosses);
                DisposeObject(circleContour);
                DisposeObject(lineContour);
                DisposeObject(overlays);
            }
        }
    }
}
