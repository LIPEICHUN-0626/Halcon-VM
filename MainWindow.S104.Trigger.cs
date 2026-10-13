using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmTcpFlowTriggerData tcpTriggerConfigS104 = new VmTcpFlowTriggerData();
        private bool tcpTriggerUiUpdatingS104;
        private sealed class TcpTriggerSessionS104
        {
            public string FlowId, FlowName, Command, WorkspaceId, Fingerprint;
            public VmTcpFlowTriggerData ReturnConfig;
            public readonly StringBuilder Buffer = new StringBuilder();
            public readonly object Gate = new object();
            public bool DiscardLine;
        }
        private volatile TcpTriggerSessionS104 tcpTriggerSessionS104;
        private int tcpTriggerPendingS104;

        private void LoadTcpTriggerConfigS104(VisionRecipe recipe)
        {
            DisarmTcpTriggerS104("打开方案后需重新启用");
            tcpTriggerConfigS104 = (recipe.TcpFlowTrigger ?? new VmTcpFlowTriggerData()).Clone().NormalizeS123();
            RefreshTcpTriggerPickerS104(true);
        }

        private void RefreshTcpTriggerPickerS104(bool loadConfig = false)
        {
            if (TcpTriggerFlowPickerS104 == null) return;
            string selected = loadConfig ? tcpTriggerConfigS104.FlowId : TcpTriggerFlowPickerS104.SelectedValue as string;
            tcpTriggerUiUpdatingS104 = true;
            try
            {
                TcpTriggerFlowPickerS104.ItemsSource = projectFlowsS104.ToList();
                TcpTriggerFlowPickerS104.SelectedValue = selected;
                if (loadConfig && TcpTriggerCommandS104 != null) TcpTriggerCommandS104.Text = tcpTriggerConfigS104.Command ?? "RUN";
                if (loadConfig && TcpTriggerReturnModeS123 != null) LoadTriggerReturnS123(tcpTriggerConfigS104);
            }
            finally { tcpTriggerUiUpdatingS104 = false; }
        }

        private void TcpTriggerDraftChangedS104(object sender, RoutedEventArgs e)
        {
            if (!uiReady || tcpTriggerUiUpdatingS104) return;
            if (!RequireEngineeringEditS137("编辑 TCP 触发配置"))
            { RefreshTcpTriggerPickerS104(true); return; }
            if (tcpTriggerSessionS104 != null) DisarmTcpTriggerS104("触发配置已编辑，请应用并重新启用");
        }

        private bool ApplyTcpTriggerConfigS104()
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("应用 TCP 触发及回传配置");
            if (editLeaseS137 < 0) return false;
            string id = TcpTriggerFlowPickerS104.SelectedValue as string;
            string command = TcpTriggerCommandS104.Text ?? string.Empty;
            if (!projectFlowsS104.Any(flow => flow.FlowId == id))
            { TcpTriggerStatusS104.Text = "请选择方案内的目标流程；原目标已删除时不会自动改跑其他流程。"; return false; }
            if (command.Length == 0 || command.Length > 64 || command.Any(ch => ch < 32 || ch > 126))
            { TcpTriggerStatusS104.Text = "指令须为 1–64 个可打印 ASCII 字符；不要填入换行符。"; return false; }
            var updated = tcpTriggerConfigS104.Clone();
            updated.FlowId = id; updated.Command = command;
            if (!TryReadTriggerReturnS123(updated)) return false;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return false;
            DisarmTcpTriggerS104("配置已应用，尚未启用");
            tcpTriggerConfigS104 = updated;
            TcpTriggerReturnStatusS123.Text = TriggerReturnModeS123(updated) == "None" ? "未启用结果回传" : "回传配置已应用；启用触发后生效";
            ScheduleRecipeStateCheck(); return true;
        }

        private void ApplyTcpTriggerConfigS104_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("应用触发配置", delegate { ApplyTcpTriggerConfigS104(); });
        }

        private string CaptureTriggerFingerprintS104()
        {
            var flows = CaptureProjectFlowsS104();
            foreach (var flow in flows) flow.ResultLayerStyles = null;
            return recipeService.GetRecipeFingerprint(new VisionRecipe { Flows = flows,
                RoiLayers = CaptureRoiLayers(), ExternalResources = CaptureExternalResources() });
        }

        private void ArmTcpTriggerS104_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("启用 TCP 流程触发", delegate
            {
                if (recipeChangeoverFaultedS134 || recipeChangeoverInProgressS134) { TcpTriggerStatusS104.Text = "方案切换尚未完成或状态未恢复，禁止启用触发。"; return; }
                if (!RequireConfiguredProductionRunS137(VmFlowRunRequestMode.Full) || !CanNavigateInputRepairS96()) return;
                bool engineeringS137 = !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && stationProtectionS137.CanEdit;
                long stationGenerationS137 = stationProtectionS137.Snapshot.Generation;
                long editLeaseS137 = engineeringS137 ? CaptureEngineeringEditLeaseS137("启用编辑后的 TCP 触发配置") : -1;
                if (engineeringS137)
                {
                    if (editLeaseS137 < 0 || !ApplyTcpTriggerConfigS104()) return;
                }
                else
                {
                    // Operator arming consumes only the applied definition; a leftover UI draft is never committed.
                    RefreshTcpTriggerPickerS104(true);
                }
                var appliedDefinitionS137 = tcpTriggerConfigS104;
                var configuredS137 = appliedDefinitionS137.Clone();
                if (!CanSelectConfiguredFlowS137(configuredS137.FlowId)) return;
                string commandS137 = configuredS137.Command ?? string.Empty;
                if (!projectFlowsS104.Any(flow => flow.FlowId == configuredS137.FlowId) || commandS137.Length == 0 ||
                    commandS137.Length > 64 || commandS137.Any(ch => ch < 32 || ch > 126))
                { TcpTriggerStatusS104.Text = "正式触发目标/指令未配置有效；须工程解锁后应用配置，不读取操作员草稿。"; return; }
                if (!tcpService.IsConnected) { TcpTriggerStatusS104.Text = "请先连接受信任设备；仅监听但没有客户端时不能启用。"; return; }
                if (!PrepareProjectFlowChangeS104()) return;
                if (stationGenerationS137 != stationProtectionS137.Snapshot.Generation ||
                    (engineeringS137 && !IsEngineeringEditLeaseCurrentS137(editLeaseS137)))
                { TcpTriggerStatusS104.Text = "确认期间工位授权已变化，未启用触发；请按当前状态重新确认。"; return; }
                SwitchProjectFlowS104(configuredS137.FlowId);
                if (activeProjectFlowIdS104 != configuredS137.FlowId || subflowEditorsS102.Count > 0) return;
                if (!RequireConfiguredProductionRunS137(VmFlowRunRequestMode.Full) || !CanSelectConfiguredFlowS137(configuredS137.FlowId)) return;
                if (!flowTools.Any(tool => tool.IsEnabled)) { TcpTriggerStatusS104.Text = "目标流程没有启用模块，请先搭建并启用流程。"; return; }
                if (flowTools.Any(ContainsCommunicationSimulationS121))
                { TcpTriggerStatusS104.Text = "目标流程包含离线模拟通讯，不允许设备触发或回传。"; return; }
                if (ShowBlockingRunReadinessS97(VmFlowRunRequestMode.Full)) return;
                string fingerprintS137 = CaptureTriggerFingerprintS104();
                if (!RequireConfiguredProductionRunS137(VmFlowRunRequestMode.Full)) return;
                if (stationProtectionClosingS137 || stationProtectionLoadingS137 || stationProtectionOperationBusyS137 ||
                    stationGenerationS137 != stationProtectionS137.Snapshot.Generation || !ReferenceEquals(appliedDefinitionS137, tcpTriggerConfigS104) ||
                    (engineeringS137 && !IsEngineeringEditLeaseCurrentS137(editLeaseS137)))
                { TcpTriggerStatusS104.Text = "工位授权或正式触发定义已变化，未启用；请重新确认已配置目标。"; return; }
                StopPlayback();
                tcpTriggerSessionS104 = new TcpTriggerSessionS104 { FlowId = activeProjectFlowIdS104,
                    FlowName = GetProjectFlowNameS104(), Command = configuredS137.Command,
                    WorkspaceId = projectSessionIdS104, Fingerprint = fingerprintS137,
                    ReturnConfig = configuredS137.Clone() };
                TcpTriggerStatusS104.Text = "已启用：" + GetProjectFlowNameS104() + " ← “" + configuredS137.Command + "” + 换行；忙碌时丢弃，不排队。";
                TcpTriggerReturnStatusS123.Text = TriggerReturnModeS123(configuredS137) == "None" ? "未启用结果回传" : "待机；只回传本次触发结果";
                LogInfo(TcpTriggerStatusS104.Text); RefreshUiState();
            });
        }

        private void StopTcpTriggerS104_Click(object sender, RoutedEventArgs e)
        {
            DisarmTcpTriggerS104("用户停止");
            if (Volatile.Read(ref tcpTriggerPendingS104) != 0) StopContinuousRun();
        }

        // May be called on the transport thread: invalidate the session before queueing any UI work.
        private void DisarmTcpTriggerS104(string reason)
        {
            tcpTriggerSessionS104 = null;
            Action update = delegate
            {
                if (TcpTriggerStatusS104 != null && tcpTriggerSessionS104 == null) TcpTriggerStatusS104.Text = "已停止 · " + reason;
                if (uiReady) RefreshUiState();
            };
            if (Dispatcher.CheckAccess()) update();
            else if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(update);
        }

        private void ReceiveTcpTriggerChunkS104(string text)
        {
            var session = tcpTriggerSessionS104;
            if (session == null || string.IsNullOrEmpty(text)) return;
            int rejected = 0;
            lock (session.Gate)
            {
                foreach (char ch in text)
                {
                    if (ch != '\n')
                    {
                        if (!session.DiscardLine)
                        {
                            if (session.Buffer.Length >= 128) { session.Buffer.Clear(); session.DiscardLine = true; }
                            else session.Buffer.Append(ch);
                        }
                        continue;
                    }
                    string line = session.Buffer.ToString();
                    session.Buffer.Clear();
                    if (session.DiscardLine) { session.DiscardLine = false; rejected++; continue; }
                    if (line.EndsWith("\r", StringComparison.Ordinal)) line = line.Substring(0, line.Length - 1);
                    if (!string.Equals(line, session.Command, StringComparison.Ordinal)) continue;
                    if (!ReferenceEquals(session, tcpTriggerSessionS104)) return;
                    // Reserve before Dispatcher enqueueing, so queued TCP messages cannot become deferred runs.
                    if (Volatile.Read(ref engineeringPackageBusyS139) || Volatile.Read(ref isFlowExecutionActive) || Volatile.Read(ref isContinuousRunning) ||
                        Volatile.Read(ref isImageQueueBatchRunning) || Volatile.Read(ref historicalImageMode) || IsCurrentToolContinuousTrialRunningS87 ||
                        Interlocked.CompareExchange(ref tcpTriggerPendingS104, 1, 0) != 0) { rejected++; continue; }
                    if (Dispatcher.HasShutdownStarted) { Interlocked.Exchange(ref tcpTriggerPendingS104, 0); return; }
                    Dispatcher.BeginInvoke(new Action(async delegate { await ExecuteTcpTriggerS104(session); }));
                }
            }
            if (rejected > 0 && !Dispatcher.HasShutdownStarted)
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    AppendTcpHistory("流程触发：丢弃 " + rejected + " 条忙碌/超长指令，不排队。");
                    if (ReferenceEquals(session, tcpTriggerSessionS104) && TcpTriggerReturnStatusS123 != null)
                        TcpTriggerReturnStatusS123.Text = "忙碌/超长指令已丢弃 " + rejected + " 条；当前任务不排队。";
                }));
        }

        private async Task ExecuteTcpTriggerS104(TcpTriggerSessionS104 session)
        {
            try
            {
                if (!ReferenceEquals(session, tcpTriggerSessionS104) || session.WorkspaceId != projectSessionIdS104) return;
                if (!tcpService.IsConnected || session.FlowId != activeProjectFlowIdS104 || subflowEditorsS102.Count > 0)
                { DisarmTcpTriggerS104("连接或目标上下文已改变"); return; }
                if (engineeringPackageBusyS139 || isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87 || historicalImageMode)
                { AppendTcpHistory("流程触发：当前忙碌，指令已丢弃。"); return; }
                if (dockDraftDirty || session.Fingerprint != CaptureTriggerFingerprintS104())
                { DisarmTcpTriggerS104("流程配置已改变，请重新确认后启用"); return; }
                if (!RequireConfiguredProductionRunS137(VmFlowRunRequestMode.Full))
                { DisarmTcpTriggerS104("当前工位不允许该已配置完整流程运行"); return; }
                if (ShowBlockingRunReadinessS97(VmFlowRunRequestMode.Full))
                { DisarmTcpTriggerS104("运行准备存在阻断，请修复后重新启用"); return; }
                AppendTcpHistory("流程触发：" + session.FlowName + "，已接受整行指令。");
                if (TriggerReturnModeS123(session.ReturnConfig) != "None")
                    TcpTriggerReturnStatusS123.Text = "已触发，等待本次流程判定…";
                var outcome = await RunInspectionRangeAsync(VmFlowRunRequestMode.Full, "TCP 指令触发", false,
                    TriggerReturnModeS123(session.ReturnConfig) != "None", VmRunOriginS135.TcpTrigger);
                AppendTcpHistory("流程触发完成：" + session.FlowName + " · " + outcome.ResultCode + " · " + outcome.StopReason);
                if (TriggerReturnModeS123(session.ReturnConfig) != "None" && !await ReturnTriggerOutcomeS123(session, outcome)) return;
                if (outcome.ExecutionError || outcome.ResultCode == "TIMEOUT" || outcome.ResultCode == "STOP" ||
                    (outcome.ResultCode == "NG" && ReadFlowRunPolicyFromUi().StopOnNg))
                    DisarmTcpTriggerS104("本次运行停止或异常，请处理后重新启用");
            }
            catch (Exception ex)
            {
                DisarmTcpTriggerS104("触发执行异常"); AppendAlarm("TCP 流程触发失败：" + ex.Message);
            }
            finally { Interlocked.Exchange(ref tcpTriggerPendingS104, 0); RefreshUiState(); }
        }
    }
}
