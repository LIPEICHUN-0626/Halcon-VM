using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly Dictionary<string, object> communicationPreparedResultsS120 = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private bool communicationTrialActiveS120;

        private static bool IsManagedCommunicationS120(VmToolInstance tool)
        {
            return tool != null && (tool.Kind == VmToolKind.TcpClientExchange ||
                tool.Kind == VmToolKind.ModbusTcpExchange || tool.Kind == VmToolKind.ModbusRtuExchange);
        }

        private sealed class PreparedTcpExchangeS120
        {
            public TcpFlowClientResult Result { get; set; }
            public Exception Error { get; set; }
        }

        private TcpFlowClientRequest BuildTcpRequestS120(VmToolInstance tool, VmToolParameterData p)
        {
            string payload = (p.TcpClientSendTemplate ?? string.Empty)
                .Replace("{String}", ResolveOptionalToolInputText(tool, "String"))
                .Replace("{Number}", ResolveOptionalToolInputText(tool, "Number"))
                .Replace("{ResultCode}", ResolveOptionalToolInputText(tool, "ResultCode"));
            return new TcpFlowClientRequest
            {
                Host = p.TcpClientHost, Port = p.TcpClientPort,
                ConnectTimeoutMilliseconds = p.TcpClientConnectTimeoutMilliseconds,
                SendTimeoutMilliseconds = p.TcpClientSendTimeoutMilliseconds,
                ReceiveTimeoutMilliseconds = p.TcpClientReceiveTimeoutMilliseconds,
                EncodingName = p.TcpClientEncoding, LineEnding = p.TcpClientLineEnding,
                Message = payload, WaitForResponse = p.TcpClientWaitForResponse
            };
        }

        private ModbusTcpRequestS118 BuildResolvedModbusRequestS120(VmToolInstance tool)
        {
            ModbusTcpRequestS118 request = BuildModbusRequestS118(tool.Parameters ?? new VmToolParameterData());
            if (!VmModbusFunctionS118.IsRead(request.FunctionCode))
            {
                object raw;
                if (TryResolveOptionalToolInputValue(tool, "WriteValue", out raw)) request.WriteValue = RequireCommunicationWriteValueS120(raw);
            }
            request.Validate();
            return request;
        }

        private ModbusRtuRequestS119 BuildResolvedRtuRequestS120(VmToolInstance tool)
        {
            ModbusRtuRequestS119 request = BuildRtuRequestS119(tool.Parameters ?? new VmToolParameterData());
            if (!VmModbusFunctionS118.IsRead(request.FunctionCode))
            {
                object raw;
                if (TryResolveOptionalToolInputValue(tool, "WriteValue", out raw)) request.WriteValue = RequireCommunicationWriteValueS120(raw);
            }
            request.Validate();
            return request;
        }

        private static int RequireCommunicationWriteValueS120(object raw)
        {
            double value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            if (double.IsNaN(value) || double.IsInfinity(value) || value != Math.Truncate(value) || value < 0 || value > 65535)
                throw new InvalidOperationException("上游写入值必须是 0–65535 的整数；写线圈仅接受 0/1。");
            return (int)value;
        }

        private CommunicationRecoveryResultS120<ModbusTcpResponseS118> ExecuteModbusTcpRecoveryS120(ModbusTcpRequestS118 request, VmToolParameterData p)
        {
            return CommunicationRecoveryS120.Execute(
                delegate(int remaining)
                {
                    var attempt = new ModbusTcpRequestS118
                    {
                        Host = request.Host, Port = request.Port, TimeoutMs = Math.Min(request.TimeoutMs, remaining),
                        UnitId = request.UnitId, FunctionCode = request.FunctionCode, Address = request.Address,
                        Quantity = request.Quantity, WriteValue = request.WriteValue
                    };
                    return ModbusTcpServiceS118.Execute(attempt, delegate { return isStopRequested; });
                }, VmModbusFunctionS118.IsRead(request.FunctionCode), BuildCommunicationRecoveryPolicyS120(p),
                delegate { return isStopRequested; });
        }

        private CommunicationRecoveryResultS120<ModbusRtuResponseS119> ExecuteModbusRtuRecoveryS120(ModbusRtuRequestS119 request, VmToolParameterData p)
        {
            return CommunicationRecoveryS120.Execute(
                delegate(int remaining)
                {
                    var attempt = new ModbusRtuRequestS119
                    {
                        PortName = request.PortName, BaudRate = request.BaudRate, ParityName = request.ParityName,
                        StopBitCount = request.StopBitCount, TimeoutMs = Math.Min(request.TimeoutMs, remaining),
                        Station = request.Station, FunctionCode = request.FunctionCode, Address = request.Address,
                        Quantity = request.Quantity, WriteValue = request.WriteValue
                    };
                    return ModbusRtuServiceS119.Execute(attempt, delegate { return isStopRequested; });
                }, VmModbusFunctionS118.IsRead(request.FunctionCode), BuildCommunicationRecoveryPolicyS120(p),
                delegate { return isStopRequested; });
        }

        private async Task<InspectionRecord> ExecuteCommunicationLeafWithTraceS120(VmToolInstance tool, string source,
            VmFlowRunPolicy policy, Stopwatch flowClock)
        {
            int remaining = policy != null && policy.FlowTimeoutMilliseconds > 0 && flowClock != null
                ? policy.FlowTimeoutMilliseconds - (int)flowClock.ElapsedMilliseconds : int.MaxValue;
            if (subflowTraceFramesS102.Count == 0) return await ExecuteCommunicationToolAsyncS120(tool, source, remaining);
            var row = AddSubflowTraceRowS102(tool);
            try { return await ExecuteCommunicationToolAsyncS120(tool, source, remaining); }
            finally { CompleteSubflowTraceRowS102(row, tool); }
        }

        private async Task<InspectionRecord> ExecuteCommunicationToolAsyncS120(VmToolInstance tool, string source, int flowRemainingBudgetMs = int.MaxValue)
        {
            if (!IsManagedCommunicationS120(tool)) return ExecuteFlowTool(tool, source);
            if (isStopRequested) throw new OperationCanceledException("通讯任务已停止。");
            if (GetExecutionSkipReasonS100(tool) != null) return ExecuteFlowTool(tool, source);
            if (simulationRunActiveS121 && !IsCommunicationSimulationS121(tool))
                throw new InvalidOperationException("离线模拟流程禁止执行真实 TCP/Modbus 模块；请将该模块也切换为模拟，或分开运行。");
            VmToolParameterData p = (tool.Parameters ?? new VmToolParameterData()).Clone();
            p.Normalize();
            InvalidateDownstreamResults(tool, "通讯任务正在重新执行");
            tool.ClearRuntimeOutputs();
            tool.ResultCode = "--"; tool.RunStatus = "运行中"; tool.ErrorMessage = string.Empty;
            tool.ElapsedMilliseconds = 0;
            if (flowRemainingBudgetMs < 100)
            {
                tool.ResultCode = "NG"; tool.RunStatus = "异常"; tool.ErrorMessage = "流程剩余时间不足以启动通讯任务。";
                throw new TimeoutException(tool.ErrorMessage);
            }
            if (tool.Kind != VmToolKind.TcpClientExchange)
                p.CommunicationTotalBudgetMsS120 = Math.Min(p.CommunicationTotalBudgetMsS120, flowRemainingBudgetMs);
            string validation = p.Validate(tool.Kind);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                tool.ResultCode = "NG"; tool.RunStatus = "异常"; tool.ErrorMessage = validation;
                RefreshCommunicationTasksS120();
                throw new InvalidOperationException(validation);
            }
            bool rootTool = flowTools.Contains(tool);
            string startingFlowId = activeProjectFlowIdS104;
            object prepared;
            Stopwatch operationClock = Stopwatch.StartNew();
            RefreshCommunicationTasksS120();
            try
            {
                if (p.CommunicationSimulationS121)
                {
                    prepared = BuildCommunicationSimulationS121(tool, p);
                }
                else if (tool.Kind == VmToolKind.TcpClientExchange)
                {
                    TcpFlowClientRequest request = BuildTcpRequestS120(tool, p);
                    prepared = await Task.Run(() =>
                    {
                        var result = new PreparedTcpExchangeS120();
                        try { result.Result = TcpFlowClientService.Execute(request, delegate { return isStopRequested; }); }
                        catch (Exception ex) { result.Error = ex; }
                        return result;
                    });
                }
                else if (tool.Kind == VmToolKind.ModbusTcpExchange)
                {
                    ModbusTcpRequestS118 request = BuildResolvedModbusRequestS120(tool);
                    prepared = await Task.Run(() => ExecuteModbusTcpRecoveryS120(request, p));
                }
                else
                {
                    ModbusRtuRequestS119 request = BuildResolvedRtuRequestS120(tool);
                    prepared = await Task.Run(() => ExecuteModbusRtuRecoveryS120(request, p));
                }
            }
            catch (OperationCanceledException ex)
            {
                tool.ResultCode = "STOP"; tool.RunStatus = "已停止"; tool.ErrorMessage = ex.Message;
                tool.ElapsedMilliseconds = operationClock.Elapsed.TotalMilliseconds;
                RefreshCommunicationTasksS120();
                throw;
            }
            catch (Exception ex)
            {
                tool.ResultCode = "NG"; tool.RunStatus = "异常"; tool.ErrorMessage = ex.Message;
                tool.ElapsedMilliseconds = operationClock.Elapsed.TotalMilliseconds;
                RefreshCommunicationTasksS120();
                throw;
            }
            if (isStopRequested || !string.Equals(startingFlowId, activeProjectFlowIdS104, StringComparison.OrdinalIgnoreCase) ||
                rootTool && !flowTools.Contains(tool))
            {
                tool.ClearRuntimeOutputs();
                tool.ResultCode = "STOP"; tool.RunStatus = "已停止"; tool.ErrorMessage = "通讯完成前流程已停止或切换，结果未发布。";
                tool.ElapsedMilliseconds = operationClock.Elapsed.TotalMilliseconds;
                RefreshCommunicationTasksS120();
                throw new OperationCanceledException(tool.ErrorMessage);
            }
            communicationPreparedResultsS120[tool.ToolId] = prepared;
            try { return ExecuteFlowTool(tool, source); }
            finally
            {
                tool.ElapsedMilliseconds = operationClock.Elapsed.TotalMilliseconds;
                communicationPreparedResultsS120.Remove(tool.ToolId);
                RefreshCommunicationTasksS120();
            }
        }

        private async Task RunStandaloneCommunicationAsyncS120(VmToolInstance tool, string source)
        {
            if (!RequireEngineeringEditS137("通讯模块手动调试")) return;
            if (tool == null || !IsManagedCommunicationS120(tool)) return;
            if (isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
            {
                HeaderStatusText.Text = "流程正在运行，请先停止或等待完成。";
                return;
            }
            isFlowExecutionActive = true; isStopRequested = false; isPauseRequested = false;
            if (IsCommunicationSimulationS121(tool))
            {
                lastResultPayload = string.Empty; lastResultIsSimulationS121 = true;
            }
            RefreshUiState();
            try
            {
                InspectionRecord record = await ExecuteCommunicationToolAsyncS120(tool, source);
                lastResultPayload = BuildJsonResultPayload(record.ResultCode, tool.ElapsedMilliseconds, new[] { record });
                lastResultIsSimulationS121 = IsCommunicationSimulationS121(tool);
                LastMessageText.Text = (lastResultIsSimulationS121 ? "离线模拟结果（禁止外发）：" : "当前模块调试结果（未自动外发）：") + lastResultPayload;
                HeaderStatusText.Text = tool.InstanceName + "：" + record.ResultCode + "，耗时 " + tool.ElapsedMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms";
            }
            catch (OperationCanceledException)
            {
                HeaderStatusText.Text = tool.InstanceName + " 已停止，本次结果未发布。";
            }
            catch (Exception ex)
            {
                HeaderStatusText.Text = tool.InstanceName + " 运行失败：" + ex.Message;
                AppendAlarm(HeaderStatusText.Text);
            }
            finally
            {
                isFlowExecutionActive = false; isStopRequested = false; isPauseRequested = false;
                RefreshUiState(); ScheduleRefreshDisplay();
            }
        }

        private sealed class CommunicationTaskRowS120
        {
            public string FlowId { get; set; }
            public string ToolId { get; set; }
            public VmToolKind Kind { get; set; }
            public List<string> ParentCalls { get; set; }
            public string Title { get; set; }
            public string Target { get; set; }
            public string State { get; set; }
            public string Error { get; set; }
        }

        private void CommunicationTasksExpanderS120_Expanded(object sender, RoutedEventArgs e)
        {
            RefreshCommunicationTasksS120();
        }

        private void RefreshCommunicationTasksS120_Click(object sender, RoutedEventArgs e)
        {
            RefreshCommunicationTasksS120();
        }

        private void RefreshCommunicationTasksS120()
        {
            if (CommunicationTasksExpanderS120 == null || CommunicationTasksExpanderS120.IsExpanded != true ||
                CommunicationTasksRowsS120 == null || !uiReady) return;
            var rows = new List<CommunicationTaskRowS120>();
            foreach (VmProjectFlowData flow in CaptureProjectFlowsS104())
                CollectCommunicationTasksS120(flow.FlowId, flow.Name, flow.Tools, new List<string>(), rows);
            foreach (CommunicationTaskRowS120 row in rows)
            {
                VmToolInstance runtime = string.Equals(row.FlowId, activeProjectFlowIdS104, StringComparison.OrdinalIgnoreCase)
                    ? flowTools.FirstOrDefault(tool => tool.ToolId == row.ToolId && tool.Kind == row.Kind) : null;
                if (runtime == null) continue;
                row.State = runtime.RunStatus + " · " + runtime.ResultCode + " · " + runtime.ElapsedText +
                    (runtime.Kind == VmToolKind.TcpClientExchange ? "" : " · 尝试 " + runtime.GetFormattedOutput("AttemptCount"));
                row.Error = runtime.ErrorMessage ?? string.Empty;
            }
            CommunicationTasksRowsS120.ItemsSource = rows;
            CommunicationTasksSummaryS120.Text = rows.Count == 0
                ? "当前方案没有 TCP/Modbus 流程任务。"
                : rows.Count.ToString(CultureInfo.InvariantCulture) + " 个通讯任务；这里只汇总配置和最近运行状态。人工重试请打开原模块确认后执行，写入不自动重放。";
        }

        private static void CollectCommunicationTasksS120(string flowId, string flowName,
            IEnumerable<ToolFlowRecipeItem> tools, List<string> parents, List<CommunicationTaskRowS120> rows)
        {
            foreach (ToolFlowRecipeItem item in tools ?? Enumerable.Empty<ToolFlowRecipeItem>())
            {
                if (item == null) continue;
                VmToolKind kind;
                if (Enum.TryParse(item.ToolType, out kind) &&
                    (kind == VmToolKind.TcpClientExchange || kind == VmToolKind.ModbusTcpExchange || kind == VmToolKind.ModbusRtuExchange))
                {
                    VmToolParameterData p = item.Parameters ?? new VmToolParameterData();
                    string target = kind == VmToolKind.TcpClientExchange
                        ? "TCP " + p.TcpClientHost + ":" + p.TcpClientPort.ToString(CultureInfo.InvariantCulture)
                        : kind == VmToolKind.ModbusTcpExchange
                            ? "Modbus TCP " + p.ModbusHostS118 + ":" + p.ModbusPortS118.ToString(CultureInfo.InvariantCulture) + " · " + VmModbusFunctionS118.Display(p.ModbusFunctionCodeS118)
                            : "Modbus RTU " + p.RtuPortNameS119 + " · " + VmModbusFunctionS118.Display(p.RtuFunctionCodeS119);
                    rows.Add(new CommunicationTaskRowS120
                    {
                        FlowId = flowId, ToolId = item.ToolId, Kind = kind, ParentCalls = parents.ToList(),
                        Title = flowName + " / " + item.InstanceName + (parents.Count > 0 ? " · 子流程" : string.Empty),
                        Target = (p.CommunicationSimulationS121 ? "离线模拟 · " : "真实设备 · ") + target,
                        State = "未在当前流程运行或暂无结果", Error = string.Empty
                    });
                }
                if (item.ToolType == VmToolKind.SubflowCall.ToString() && item.Parameters != null && item.Parameters.Subflow != null)
                {
                    var next = parents.ToList();
                    next.Add(item.ToolId);
                    if (next.Count <= 8)
                        CollectCommunicationTasksS120(flowId, flowName, item.Parameters.Subflow.Tools, next, rows);
                }
            }
        }

        private void OpenCommunicationTaskS120_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            CommunicationTaskRowS120 row = button == null ? null : button.Tag as CommunicationTaskRowS120;
            if (row == null) return;
            RunUiAction("定位通讯任务", delegate
            {
                if (!CanNavigateInputRepairS96()) return;
                while (subflowEditorsS102.Count > 0)
                {
                    int before = subflowEditorsS102.Count;
                    ReturnSubflowEditorS102_Click(null, null);
                    if (subflowEditorsS102.Count >= before) return;
                }
                if (row.FlowId != activeProjectFlowIdS104)
                {
                    SwitchProjectFlowS104(row.FlowId);
                    if (row.FlowId != activeProjectFlowIdS104) return;
                }
                foreach (string callId in row.ParentCalls)
                {
                    VmToolInstance call = flowTools.FirstOrDefault(item => item.ToolId == callId && item.Kind == VmToolKind.SubflowCall);
                    if (call == null) throw new InvalidOperationException("子流程已修改，请刷新通讯任务列表。");
                    int depth = subflowEditorsS102.Count;
                    EnterSubflowEditorS102(call);
                    if (subflowEditorsS102.Count <= depth) return;
                }
                VmToolInstance target = flowTools.FirstOrDefault(item => item.ToolId == row.ToolId && item.Kind == row.Kind);
                if (target == null) throw new InvalidOperationException("通讯任务已修改，请刷新列表。");
                OpenCentralModuleConfigurationS77(target, true);
                if (ReferenceEquals(dockEditingTool, target)) HeaderStatusText.Text = "已定位“" + target.InstanceName + "”；此操作不会自动连接设备或重放写入。";
            });
        }

        private static CommunicationRecoveryPolicyS120 BuildCommunicationRecoveryPolicyS120(VmToolParameterData p)
        {
            return new CommunicationRecoveryPolicyS120
            {
                AdditionalReadAttempts = p.CommunicationRetryCountS120,
                BackoffMs = p.CommunicationRetryDelayMsS120,
                TotalBudgetMs = p.CommunicationTotalBudgetMsS120
            };
        }

        private void LoadCommunicationRecoveryS120(VmToolParameterData p, bool rtu)
        {
            string count = p.CommunicationRetryCountS120.ToString(CultureInfo.InvariantCulture);
            string delay = p.CommunicationRetryDelayMsS120.ToString(CultureInfo.InvariantCulture);
            string budget = p.CommunicationTotalBudgetMsS120.ToString(CultureInfo.InvariantCulture);
            if (rtu)
            {
                DockRtuRetryCountS120.Text = count;
                DockRtuRetryDelayS120.Text = delay;
                DockRtuTotalBudgetS120.Text = budget;
                UpdateCommunicationRecoveryModeS120(true, VmModbusFunctionS118.IsRead(p.RtuFunctionCodeS119));
            }
            else
            {
                DockModbusRetryCountS120.Text = count;
                DockModbusRetryDelayS120.Text = delay;
                DockModbusTotalBudgetS120.Text = budget;
                UpdateCommunicationRecoveryModeS120(false, VmModbusFunctionS118.IsRead(p.ModbusFunctionCodeS118));
            }
        }

        private bool ReadCommunicationRecoveryS120(VmToolParameterData p, bool rtu)
        {
            int number;
            bool read = VmModbusFunctionS118.IsRead(rtu ? p.RtuFunctionCodeS119 : p.ModbusFunctionCodeS118);
            string count = rtu ? DockRtuRetryCountS120.Text : DockModbusRetryCountS120.Text;
            string delay = rtu ? DockRtuRetryDelayS120.Text : DockModbusRetryDelayS120.Text;
            string budget = rtu ? DockRtuTotalBudgetS120.Text : DockModbusTotalBudgetS120.Text;
            if (read)
            {
                if (!int.TryParse(count, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("额外读取尝试次数必须是整数。");
                p.CommunicationRetryCountS120 = number;
                if (!int.TryParse(delay, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("读取重试间隔必须是整数毫秒。");
                p.CommunicationRetryDelayMsS120 = number;
            }
            if (!int.TryParse(budget, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("通讯任务总预算必须是整数毫秒。");
            p.CommunicationTotalBudgetMsS120 = number;
            return true;
        }

        private void UpdateCommunicationRecoveryModeS120(bool rtu, bool read)
        {
            if (rtu)
            {
                if (DockRtuRecoveryFieldsS120 == null || DockRtuRecoveryHintS120 == null) return;
                DockRtuRecoveryFieldsS120.Visibility = read ? Visibility.Visible : Visibility.Collapsed;
                DockRtuRecoveryHintS120.Text = read ? "仅超时、断线或 CRC 错等可恢复读失败自动重试；设备异常码不重试。" : "写入可能已经到达设备：绝不自动重放。失败后须人工确认设备状态。";
            }
            else
            {
                if (DockModbusRecoveryFieldsS120 == null || DockModbusRecoveryHintS120 == null) return;
                DockModbusRecoveryFieldsS120.Visibility = read ? Visibility.Visible : Visibility.Collapsed;
                DockModbusRecoveryHintS120.Text = read ? "仅超时或断线等可恢复读失败自动重试；设备异常码不重试。" : "写入可能已经到达设备：绝不自动重放。失败后须人工确认设备状态。";
            }
        }
    }
}
