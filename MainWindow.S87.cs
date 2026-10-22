using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly DispatcherTimer currentToolTrialTimerS87 = new DispatcherTimer();
        private CurrentToolTrialSessionS87 currentToolTrialSessionS87;
        private bool currentToolTrialCycleActiveS87;
        private string currentToolTrialLastUiTextS87 = "未启动";

        private sealed class CurrentToolTrialSessionS87
        {
            public long EngineeringLeaseS137 { get; set; }
            public string ToolId { get; set; }
            public int IntervalMilliseconds { get; set; }
            public VmToolParameterData DraftParameters { get; set; }
            public List<VmToolInputBindingData> DraftInputBindings { get; set; }
            public List<string> DraftRoiBindings { get; set; }
            public string DraftInputToolId { get; set; }
            public string DraftInputPortName { get; set; }
            public string DraftNumericOperator { get; set; }
            public double DraftNumericLower { get; set; }
            public double DraftNumericUpper { get; set; }
            public double DraftNumericTolerance { get; set; }
            public VmToolParameterData AppliedParameters { get; set; }
            public List<VmToolInputBindingData> AppliedInputBindings { get; set; }
            public List<string> AppliedRoiBindings { get; set; }
            public string AppliedInputToolId { get; set; }
            public string AppliedInputPortName { get; set; }
            public string AppliedNumericOperator { get; set; }
            public double AppliedNumericLower { get; set; }
            public double AppliedNumericUpper { get; set; }
            public double AppliedNumericTolerance { get; set; }
            public int Iterations { get; set; }
            public int OkCount { get; set; }
            public int NgCount { get; set; }
            public int SkipCount { get; set; }
            public int ErrorCount { get; set; }
            public string LastResultCode { get; set; }
            public double LastElapsedMilliseconds { get; set; }
            public VmFlowRunPolicy SubflowPolicy { get; set; }
            public string StopReason { get; set; }
        }

        private bool IsCurrentToolContinuousTrialRunningS87
        {
            get { return currentToolTrialSessionS87 != null; }
        }

        private void InitializeCurrentToolTrialS87()
        {
            currentToolTrialTimerS87.Interval = TimeSpan.FromMilliseconds(500);
            currentToolTrialTimerS87.Tick += CurrentToolTrialTimer_TickS87;
            if (DockCurrentToolTrialIntervalComboBoxS87 != null)
            {
                DockCurrentToolTrialIntervalComboBoxS87.ItemsSource = new[] { 100, 250, 500, 1000, 2000 };
                DockCurrentToolTrialIntervalComboBoxS87.SelectedItem = 500;
            }
            RefreshCurrentToolTrialUiS87();
        }

        private void UpdateDockInputTaskRowS87(VmInputPortEditorRow row, VmInputSourceOption selected, VmToolInstance source)
        {
            if (row == null) return;
            row.SourceText = selected == null ? "无效来源" : selected.DisplayText;
            row.ConsumerText = dockEditingTool == null ? "--" : dockEditingTool.InstanceName + "." + row.PortName;

            string configurationError = string.Empty;
            if (selected == null || !selected.IsValid)
            {
                configurationError = "已保存来源不存在、已停用或不在当前模块之前。";
            }
            else if (!selected.IsDefault && dockEditingTool != null)
            {
                configurationError = GetDraftInputBindingErrorS108(
                    dockEditingTool,
                    new VmToolInputBindingData
                    {
                        TargetPortName = row.PortName,
                        SourceToolId = selected.SourceToolId,
                        SourcePortName = selected.SourcePortName
                    });
            }

            bool visualContext = IsImagePixelDataTypeS87(row.DataType);
            row.CoordinateContextText = visualContext
                ? (source == null
                    ? (dockEditingTool == null ? "Global.Image" : ResolveImagePixelContextIdS66(dockEditingTool))
                    : ResolveOutputCoordinateContextS102(source, selected.SourcePortName))
                : "不适用（标量 / 文本 / 状态）";
            if (string.Equals(row.DataType, "WorldPoint2D", StringComparison.OrdinalIgnoreCase))
            {
                var worldPoint = GetWorldDraftPointS125(row);
                row.CoordinateContextText = worldPoint == null
                    ? "World2D · 等待标定来源运行"
                    : "World2D · " + worldPoint.Unit + " · 标定 " + GetWorldCalibrationLabelS125(worldPoint);
                if (worldPoint != null)
                {
                    row.SourceText = source.InstanceName + "." + selected.SourcePortName + " · " + worldPoint.Unit;
                    row.CurrentValue = string.Format(CultureInfo.InvariantCulture, "X={0:0.###}, Y={1:0.###} {2}", worldPoint.X, worldPoint.Y, worldPoint.Unit);
                    if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.WorldPointPairMeasurement)
                    {
                        var other = dockInputPortRows.FirstOrDefault(item => item.PortName == (row.PortName == "PointA" ? "PointB" : "PointA"));
                        string mismatch = GetWorldPairMismatchS125(worldPoint, GetWorldDraftPointS125(other));
                        if (!string.IsNullOrWhiteSpace(mismatch)) configurationError = mismatch;
                    }
                }
            }

            bool defaultConfigured = selected != null && selected.IsDefault &&
                (row.IsOptional ||
                 row.PortName == "ROI" || row.PortName == "SearchROI" ||
                 row.PortName == "ShapeModel" || row.PortName == "Program" ||
                 row.PortName == "Image" && currentImage != null);
            row.IsConfigured = string.IsNullOrWhiteSpace(configurationError) &&
                (selected != null && !selected.IsDefault || defaultConfigured);
            object currentRaw = null;
            bool bound = selected != null && !selected.IsDefault;
            bool hasSnapshot = bound && source != null && source.IsEnabled && source.LastOutputUtc.HasValue &&
                (source.ResultCode == "OK" || source.ResultCode == "NG") && string.IsNullOrWhiteSpace(source.ErrorMessage) &&
                source.TryGetOutputValue(selected.SourcePortName, out currentRaw) && currentRaw != null;
            string valueError = hasSnapshot ? GetResultValueErrorUI06(row.DataType, currentRaw) : null;
            row.CanInspectValueUI06 = hasSnapshot && (currentRaw is VmPoint2DArray || valueError == null && VmArrayContractS105.IsType(row.DataType));
            if (bound) row.CurrentValue = !hasSnapshot ? "--" : valueError == null ? source.GetFormattedOutput(selected.SourcePortName) : "类型/数值无效";
            row.IsReady = row.IsConfigured &&
                (bound ? hasSnapshot && valueError == null && source.RuntimeResultOrigin == VmRuntimeResultOrigin.ModuleExecution :
                 row.IsOptional && selected != null && selected.IsDefault ||
                 row.PortName == "ROI" || row.PortName == "SearchROI" ||
                 !string.Equals(row.CurrentValue, "--", StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(configurationError))
            {
                row.StateKey = "Error";
                row.StatusText = "不可用 · " + configurationError;
                row.DiagnosticText = configurationError;
            }
            else if (bound && hasSnapshot)
            {
                bool preview = source.RuntimeResultOrigin != VmRuntimeResultOrigin.ModuleExecution;
                row.StateKey = valueError != null ? "Error" : preview ? "Preview" : "Ready";
                row.StatusText = valueError != null ? "输出类型 / 数值无效" :
                    (source.RuntimeResultOrigin == VmRuntimeResultOrigin.DraftTrial ? "草稿预览 · " : preview ? "来源未标注 · " : "最近执行 · ") + ShortResultValueS88(BriefTypedValueUI06(currentRaw, row.CurrentValue), 64);
                row.DiagnosticText = (valueError == null ? "只读当前来源快照；实际使用时仍检查应用参数、模型版本及坐标。" : valueError) +
                    "\n" + BuildResultOriginTextS88(source) + " · 输出版本 " + source.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture);
            }
            else if (bound && row.IsConfigured)
            {
                row.StateKey = "Waiting";
                row.StatusText = "已选择 · 暂无有效值";
                row.DiagnosticText = "保留真实端口连接；请运行上游。可选输入已连接时也需要有效值，不回退旧值。";
            }
            else if (row.IsReady)
            {
                row.StateKey = "Ready";
                row.StatusText = selected != null && selected.IsDefault && row.IsOptional
                    ? "未连接 · 可选，不阻塞运行"
                    : (string.Equals(row.CurrentValue, "--", StringComparison.Ordinal)
                        ? "来源可用 · 使用默认任务范围"
                        : "有值 · " + ShortResultValueS88(row.CurrentValue, 64));
                row.DiagnosticText = "来源、顺序和类型检查通过。";
            }
            else if (row.IsOptional)
            {
                row.StateKey = "Optional";
                row.StatusText = "未连接 · 可选";
                row.DiagnosticText = "可选输入未提供，不阻塞当前工具。";
            }
            else if (row.IsConfigured)
            {
                row.StateKey = "Waiting";
                row.StatusText = "已选择 · 等待前序本周期值";
                row.DiagnosticText = "结构连接有效；请先运行上游或运行到当前模块。";
            }
            else
            {
                row.StateKey = "Error";
                row.StatusText = "未连接 · 请选择必需来源";
                row.DiagnosticText = "必需输入没有可执行来源。";
            }
        }

        private static bool IsImagePixelDataTypeS87(string dataType)
        {
            return string.Equals(dataType, "Image", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Region", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "XLD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Pose2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Point2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Point2D[]", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Line2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Circle2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Ellipse2D", StringComparison.OrdinalIgnoreCase);
        }

        private void DockCurrentToolTrialIntervalComboBox_SelectionChangedS87(object sender, SelectionChangedEventArgs e)
        {
            if (IsCurrentToolContinuousTrialRunningS87)
            {
                DockCurrentToolTrialIntervalComboBoxS87.SelectedItem = currentToolTrialSessionS87.IntervalMilliseconds;
            }
        }

        private void DockContinuousTrialToggleButton_ClickS87(object sender, RoutedEventArgs e)
        {
            if (IsCurrentToolContinuousTrialRunningS87)
            {
                StopCurrentToolContinuousTrialS87("用户停止", true);
                return;
            }
            if (subflowTrialActiveS102) { StopContinuousRun(); return; }
            StartCurrentToolContinuousTrialS87();
        }

        private async void StartCurrentToolContinuousTrialS87()
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("启动当前模块草稿连续试运行");
            if (editLeaseS137 < 0) return;
            if (recipeChangeoverFaultedS134 || recipeChangeoverInProgressS134)
            { HeaderStatusText.Text = "方案切换尚未完成或状态未恢复，不能启动当前模块连续试运行。"; return; }
            VmToolInstance tool = dockEditingTool;
            try
            {
                if (tool == null || !SupportsDockConfiguration(tool.Kind))
                    throw new InvalidOperationException("请先双击一个可配置模块。");
                if (historicalImageMode)
                    throw new InvalidOperationException("历史结果回看期间不能连续试运行。");
                if (isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning)
                    throw new InvalidOperationException("已有流程或图片批次正在运行，请先停止。");

                int interval = DockCurrentToolTrialIntervalComboBoxS87 == null || DockCurrentToolTrialIntervalComboBoxS87.SelectedItem == null
                    ? 500
                    : Convert.ToInt32(DockCurrentToolTrialIntervalComboBoxS87.SelectedItem, CultureInfo.InvariantCulture);
                if (interval < 100 || interval > 2000)
                    throw new InvalidOperationException("当前模块连续试运行节拍必须为 100-2000 ms。");

                VmToolParameterData draft;
                string instanceName;
                bool enabled;
                string numericOperator;
                double numericLower;
                double numericUpper;
                double numericTolerance;
                if (!TryReadDockConfigurationDraft(out draft, out instanceName, out enabled, out numericOperator, out numericLower, out numericUpper, out numericTolerance))
                {
                    if (DockContinuousTrialToggleButtonS87 != null) DockContinuousTrialToggleButtonS87.IsChecked = false;
                    return;
                }
                if (!enabled || !tool.IsEnabled) throw new InvalidOperationException("当前模块已停用，不能连续试运行。");

                CurrentToolTrialSessionS87 session = CaptureCurrentToolTrialSessionS87(
                    tool, draft, numericOperator, numericLower, numericUpper, numericTolerance, interval);
                session.EngineeringLeaseS137 = editLeaseS137;
                if (tool.Kind == VmToolKind.SubflowCall) session.SubflowPolicy = ReadFlowRunPolicyFromUi();
                BeginCurrentToolTrialStabilityS88(tool);
                currentToolTrialSessionS87 = session;
                currentToolTrialTimerS87.Interval = TimeSpan.FromMilliseconds(interval);
                currentToolTrialLastUiTextS87 = "正在启动 · " + tool.InstanceName;
                LogInfo("当前模块连续试运行已启动：" + tool.InstanceName + "，节拍 " + interval.ToString(CultureInfo.InvariantCulture) + " ms；草稿与输入绑定已冻结。");
                RefreshCurrentToolTrialUiS87();
                RefreshUiState();
                await RunCurrentToolTrialCycleS87();
            }
            catch (Exception ex)
            {
                currentToolTrialTimerS87.Stop();
                if (!currentToolTrialCycleActiveS87) currentToolTrialSessionS87 = null;
                if (DockContinuousTrialToggleButtonS87 != null) DockContinuousTrialToggleButtonS87.IsChecked = false;
                currentToolTrialLastUiTextS87 = "启动失败 · " + ex.Message;
                HeaderStatusText.Text = currentToolTrialLastUiTextS87;
                AppendAlarm("当前模块连续试运行启动失败：" + ex.Message);
                RefreshCurrentToolTrialUiS87();
            }
        }

        private CurrentToolTrialSessionS87 CaptureCurrentToolTrialSessionS87(
            VmToolInstance tool,
            VmToolParameterData draft,
            string numericOperator,
            double numericLower,
            double numericUpper,
            double numericTolerance,
            int interval)
        {
            CurrentToolTrialSessionS87 session = new CurrentToolTrialSessionS87
            {
                ToolId = tool.ToolId,
                IntervalMilliseconds = interval,
                DraftParameters = draft.Clone(),
                DraftRoiBindings = dockRoiBindingRows.Where(item => item.IsBound).Select(item => item.RoiId).ToList(),
                DraftNumericOperator = numericOperator,
                DraftNumericLower = numericLower,
                DraftNumericUpper = numericUpper,
                DraftNumericTolerance = numericTolerance,
                AppliedParameters = tool.Parameters,
                AppliedInputBindings = tool.InputBindings.Select(item => item.Clone()).ToList(),
                AppliedRoiBindings = tool.BoundRoiIds.ToList(),
                AppliedInputToolId = tool.InputToolId,
                AppliedInputPortName = tool.InputPortName,
                AppliedNumericOperator = tool.NumericOperator,
                AppliedNumericLower = tool.NumericLowerLimit,
                AppliedNumericUpper = tool.NumericUpperLimit,
                AppliedNumericTolerance = tool.NumericTolerance,
                LastResultCode = "--"
            };

            try
            {
                tool.Parameters = session.DraftParameters.Clone();
                ApplyDockInputDraft(tool);
                if (ToolMetadata.SupportsRoi(tool.Kind)) tool.ReplaceRoiBindings(session.DraftRoiBindings);
                tool.NumericOperator = numericOperator;
                tool.NumericLowerLimit = numericLower;
                tool.NumericUpperLimit = numericUpper;
                tool.NumericTolerance = numericTolerance;
                session.DraftInputBindings = tool.InputBindings.Select(item => item.Clone()).ToList();
                session.DraftInputToolId = tool.InputToolId;
                session.DraftInputPortName = tool.InputPortName;
            }
            finally
            {
                RestoreAppliedCurrentToolConfigurationS87(tool, session);
            }
            return session;
        }

        private async void CurrentToolTrialTimer_TickS87(object sender, EventArgs e)
        {
            await RunCurrentToolTrialCycleS87();
        }

        private async Task RunCurrentToolTrialCycleS87()
        {
            CurrentToolTrialSessionS87 session = currentToolTrialSessionS87;
            if (session == null || currentToolTrialCycleActiveS87) return;
            if (!IsEngineeringEditLeaseCurrentS137(session.EngineeringLeaseS137)) { StopCurrentToolContinuousTrialS87("工程授权已改变", true); return; }
            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, session.ToolId, StringComparison.OrdinalIgnoreCase));
            if (tool == null || dockEditingTool == null || !string.Equals(dockEditingTool.ToolId, session.ToolId, StringComparison.OrdinalIgnoreCase))
            {
                StopCurrentToolContinuousTrialS87("模块已切换或删除", true);
                return;
            }

            currentToolTrialTimerS87.Stop();
            currentToolTrialCycleActiveS87 = true;
            InvalidateProductionResultS141("当前模块连续试运行属于工程调试；旧生产周期值已清空。");
            bool subflowTrial = tool.Kind == VmToolKind.SubflowCall;
            bool communicationTrialS120 = IsManagedCommunicationS120(tool);
            bool previousTracking = recipeTrackingSuspended;
            string previousCode = session.LastResultCode;
            try
            {
                if (subflowTrial)
                {
                    ClearSubflowTraceS102();
                    subflowTrialActiveS102 = true; isFlowExecutionActive = true; recipeTrackingSuspended = true;
                    isStopRequested = false; isPauseRequested = false; ResetFlowDebugRuntimeState(true);
                    RefreshUiState();
                }
                else if (communicationTrialS120)
                {
                    communicationTrialActiveS120 = true; isFlowExecutionActive = true;
                    isStopRequested = false; isPauseRequested = false; RefreshUiState();
                    if (IsCommunicationSimulationS121(tool))
                    {
                        lastResultPayload = string.Empty; lastResultIsSimulationS121 = true;
                    }
                }
                ApplyFrozenCurrentToolConfigurationS87(tool, session);
                InspectionRecord record = subflowTrial
                    ? await ExecuteScheduledToolS102(tool, "当前子流程连续试运行", session.SubflowPolicy, Stopwatch.StartNew())
                    : communicationTrialS120
                        ? await ExecuteCommunicationToolAsyncS120(tool, "当前通讯模块连续试运行")
                        : ExecuteFlowTool(tool, "当前模块连续试运行");
                session.Iterations++;
                if (!IsEngineeringEditLeaseCurrentS137(session.EngineeringLeaseS137)) session.StopReason = "工程授权已改变；本轮结束后不再启动草稿试运行";
                session.LastResultCode = record.ResultCode;
                session.LastElapsedMilliseconds = tool.ElapsedMilliseconds;
                if (string.Equals(record.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)) session.OkCount++;
                else if (string.Equals(record.ResultCode, "SKIP", StringComparison.OrdinalIgnoreCase)) session.SkipCount++;
                else session.NgCount++;
                RecordCurrentToolTrialStabilityS88(tool, session.Iterations, record.ResultCode, tool.ElapsedMilliseconds, string.Empty);
                lastResultPayload = BuildJsonResultPayload(record.ResultCode, tool.ElapsedMilliseconds, new[] { record });
                lastResultIsSimulationS121 = flowTools.Any(ContainsCommunicationSimulationS121);
                LastMessageText.Text = (lastResultIsSimulationS121 ? "离线模拟结果（禁止外发）：" : "当前模块调试结果（未自动外发）：") + lastResultPayload;
                currentToolTrialLastUiTextS87 = BuildCurrentToolTrialStatusTextS87(session, "运行中");
                HeaderStatusText.Text = tool.InstanceName + " 连续试运行 #" + session.Iterations.ToString(CultureInfo.InvariantCulture) +
                    "：" + record.ResultCode + "，" + tool.ElapsedMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms";
                if (session.Iterations == 1 || session.Iterations % 10 == 0 || !string.Equals(previousCode, record.ResultCode, StringComparison.OrdinalIgnoreCase))
                    LogInfo(HeaderStatusText.Text);
                if (subflowTrial && session.SubflowPolicy.StopOnNg && record.ResultCode == "NG")
                    session.StopReason = "遇 NG 停止";
            }
            catch (OperationCanceledException) when (subflowTrial || communicationTrialS120)
            {
                session.LastResultCode = "STOP";
                session.LastElapsedMilliseconds = tool.ElapsedMilliseconds;
                session.StopReason = session.StopReason ?? "用户停止";
                lastResultPayload = string.Empty; LastMessageText.Text = communicationTrialS120
                    ? "当前通讯模块已停止，本轮未发布结果。" : "当前子流程已停止，本轮未发布返回值。";
            }
            catch (SubflowNgStopS102 ex)
            {
                session.Iterations++; session.NgCount++;
                session.LastResultCode = "NG"; session.LastElapsedMilliseconds = tool.ElapsedMilliseconds;
                session.StopReason = ex.Message;
                lastResultPayload = string.Empty; LastMessageText.Text = "当前子流程遇 NG 停止，本轮未发布返回值。";
                RecordCurrentToolTrialStabilityS88(tool, session.Iterations, "NG", tool.ElapsedMilliseconds, string.Empty);
            }
            catch (Exception ex)
            {
                session.Iterations++;
                session.ErrorCount++;
                session.LastResultCode = ex is TimeoutException ? "TIMEOUT" : "ERROR";
                session.LastElapsedMilliseconds = tool.ElapsedMilliseconds;
                if (subflowTrial) { lastResultPayload = string.Empty; LastMessageText.Text = "当前子流程未完成，本轮未发布返回值：" + ex.Message; }
                RecordCurrentToolTrialStabilityS88(tool, session.Iterations, session.LastResultCode, tool.ElapsedMilliseconds, ex.Message);
                currentToolTrialLastUiTextS87 = BuildCurrentToolTrialStatusTextS87(session, "已停止") + " · " + ex.Message;
                AppendAlarm("当前模块连续试运行已停止：" + tool.InstanceName + " · " + ex.Message);
                StopCurrentToolContinuousTrialS87("输入或执行异常：" + ex.Message, true);
            }
            finally
            {
                RestoreAppliedCurrentToolConfigurationS87(tool, session);
                currentToolTrialCycleActiveS87 = false;
                if (subflowTrial)
                {
                    subflowTrialActiveS102 = false; isFlowExecutionActive = false; recipeTrackingSuspended = previousTracking;
                    isStopRequested = false; isPauseRequested = false; ResetFlowDebugRuntimeState(true);
                }
                else if (communicationTrialS120)
                {
                    communicationTrialActiveS120 = false; isFlowExecutionActive = false;
                    isStopRequested = false; isPauseRequested = false;
                }
                if (ReferenceEquals(currentToolTrialSessionS87, session))
                {
                    if (session.StopReason != null) StopCurrentToolContinuousTrialS87(session.StopReason, false);
                    else currentToolTrialTimerS87.Start();
                }
                RefreshToolConfigurationStatus(tool);
                RefreshDualImageInputOptions(tool);
                UpdateComparisonImageViewport(true);
                RefreshDockConfigurationRuntime(tool);
                RefreshCurrentToolTrialUiS87();
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
        }

        private static void ApplyFrozenCurrentToolConfigurationS87(VmToolInstance tool, CurrentToolTrialSessionS87 session)
        {
            tool.Parameters = session.DraftParameters.Clone();
            tool.ReplaceInputBindings(session.DraftInputBindings.Select(item => item.Clone()));
            tool.ReplaceRoiBindings(session.DraftRoiBindings);
            tool.InputToolId = session.DraftInputToolId;
            tool.InputPortName = session.DraftInputPortName;
            tool.NumericOperator = session.DraftNumericOperator;
            tool.NumericLowerLimit = session.DraftNumericLower;
            tool.NumericUpperLimit = session.DraftNumericUpper;
            tool.NumericTolerance = session.DraftNumericTolerance;
        }

        private static void RestoreAppliedCurrentToolConfigurationS87(VmToolInstance tool, CurrentToolTrialSessionS87 session)
        {
            if (tool == null || session == null) return;
            tool.Parameters = session.AppliedParameters;
            tool.ReplaceInputBindings(session.AppliedInputBindings.Select(item => item.Clone()));
            tool.ReplaceRoiBindings(session.AppliedRoiBindings);
            tool.InputToolId = session.AppliedInputToolId;
            tool.InputPortName = session.AppliedInputPortName;
            tool.NumericOperator = session.AppliedNumericOperator;
            tool.NumericLowerLimit = session.AppliedNumericLower;
            tool.NumericUpperLimit = session.AppliedNumericUpper;
            tool.NumericTolerance = session.AppliedNumericTolerance;
        }

        private string BuildCurrentToolTrialStatusTextS87(CurrentToolTrialSessionS87 session, string state)
        {
            if (session == null) return state;
            return state + " · 循环 " + session.Iterations.ToString(CultureInfo.InvariantCulture) +
                   " · OK " + session.OkCount.ToString(CultureInfo.InvariantCulture) +
                   " / NG " + session.NgCount.ToString(CultureInfo.InvariantCulture) +
                   (session.SkipCount == 0 ? string.Empty : " / 跳过 " + session.SkipCount.ToString(CultureInfo.InvariantCulture)) +
                   " / 异常 " + session.ErrorCount.ToString(CultureInfo.InvariantCulture) +
                   " · 最近 " + session.LastResultCode +
                   (session.LastElapsedMilliseconds <= 0 ? string.Empty : " " + session.LastElapsedMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms");
        }

        private void StopCurrentToolContinuousTrialS87(string reason, bool refreshUi)
        {
            CurrentToolTrialSessionS87 session = currentToolTrialSessionS87;
            if (session == null)
            {
                if (DockContinuousTrialToggleButtonS87 != null) DockContinuousTrialToggleButtonS87.IsChecked = false;
                return;
            }
            currentToolTrialTimerS87.Stop();
            if (currentToolTrialCycleActiveS87 && (subflowTrialActiveS102 || communicationTrialActiveS120))
            {
                session.StopReason = session.StopReason ?? reason;
                isStopRequested = true; isPauseRequested = false;
                currentToolTrialLastUiTextS87 = BuildCurrentToolTrialStatusTextS87(session, "正在停止");
                RefreshCurrentToolTrialUiS87();
                if (refreshUi) RefreshUiState();
                return;
            }
            currentToolTrialSessionS87 = null;
            currentToolTrialLastUiTextS87 = BuildCurrentToolTrialStatusTextS87(session, "已停止") + " · " + reason;
            if (DockContinuousTrialToggleButtonS87 != null) DockContinuousTrialToggleButtonS87.IsChecked = false;
            LogInfo("当前模块连续试运行已停止：" + currentToolTrialLastUiTextS87);
            RefreshCurrentToolTrialUiS87();
            if (refreshUi) RefreshUiState();
        }

        private void StopCurrentToolContinuousTrialForContextS87(VmToolInstance nextTool, string reason)
        {
            VmToolInstance runningTool = IsCurrentToolContinuousTrialRunningS87
                ? flowTools.FirstOrDefault(item => string.Equals(item.ToolId, currentToolTrialSessionS87.ToolId, StringComparison.OrdinalIgnoreCase))
                : null;
            bool sameRunningTool = runningTool != null && ReferenceEquals(nextTool, runningTool);
            if (!sameRunningTool && IsCurrentToolContinuousTrialRunningS87) StopCurrentToolContinuousTrialS87(reason, false);
            ClearCurrentToolTrialStabilityForContextS88(nextTool, reason);
        }

        private void RefreshCurrentToolTrialUiS87()
        {
            if (DockContinuousTrialToggleButtonS87 == null) return;
            bool running = IsCurrentToolContinuousTrialRunningS87;
            bool otherBusy = isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning || historicalImageMode;
            bool hasTool = dockEditingTool != null && dockEditingTool.IsEnabled && SupportsDockConfiguration(dockEditingTool.Kind);
            bool stopping = running && currentToolTrialSessionS87.StopReason != null || subflowTrialActiveS102 && isStopRequested;
            bool pausedCall = subflowTrialActiveS102 && isPauseRequested && !isStopRequested;
            DockContinuousTrialToggleButtonS87.IsChecked = running;
            DockContinuousTrialToggleButtonS87.Content = stopping ? "正在停止…" : running || subflowTrialActiveS102 ? "■ 停止" : "↻ 连续";
            DockContinuousTrialToggleButtonS87.IsEnabled = !stopping && (running || subflowTrialActiveS102 || hasTool && !otherBusy);
            DockContinuousTrialToggleButtonS87.ToolTip = hasTool && dockEditingTool.Kind == VmToolKind.SubflowCall
                ? "重复运行当前子流程草稿及其内部步骤；每轮结束后等待设定间隔，不运行父流程。停止在当前算子结束后的安全边界生效。"
                : "重复试运行当前模块草稿；每轮结束后等待设定间隔，不运行整条流程。";
            DockTrialRunButton.Content = pausedCall ? "▶ 继续调用" : hasTool && dockEditingTool.Kind == VmToolKind.ImageSource ? "▶ 读取一帧" : "▶ 试运行当前模块";
            DockTrialRunButton.ToolTip = pausedCall ? "F5：继续当前已暂停的子流程，不重新开始，不运行父流程。" : "F5：使用当前草稿只运行此模块一次。";
            DockTrialRunButton.IsEnabled = pausedCall || hasTool && !running && !otherBusy;
            DockSubflowStepButtonS102.Visibility = pausedCall ? Visibility.Visible : Visibility.Collapsed;
            DockConfigurationTabs.IsEnabled = !running && !subflowTrialActiveS102;
            DockApplyButton.IsEnabled = hasTool && !running && !subflowTrialActiveS102;
            DockRevertButton.IsEnabled = hasTool && !running && !subflowTrialActiveS102;
            DockCurrentToolTrialIntervalComboBoxS87.IsEnabled = !running && !otherBusy;
            DockCurrentToolTrialStatusTextS87.Text = running
                ? BuildCurrentToolTrialStatusTextS87(currentToolTrialSessionS87, stopping ? "正在停止" : isPauseRequested && subflowTrialActiveS102 ? "内部已暂停" : "运行中")
                : currentToolTrialLastUiTextS87;
            DockCurrentToolTrialStatusTextS87.Foreground = new SolidColorBrush(running ? Color.FromRgb(4, 120, 87) : Color.FromRgb(71, 85, 105));
            if (running) DockFooterText.Text = pausedCall ? debugPauseReason : BuildCurrentToolTrialStatusTextS87(currentToolTrialSessionS87, stopping ? "正在停止" : "连续试运行");
            RefreshCurrentToolTrialStabilityUiS88();
        }
    }
}
