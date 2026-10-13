using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private static string TriggerReturnModeS123(VmTcpFlowTriggerData data)
        {
            return string.IsNullOrWhiteSpace(data.ReturnModeS123) ? "None" : data.ReturnModeS123;
        }

        private void LoadTriggerReturnS123(VmTcpFlowTriggerData data)
        {
            data = data.Clone().NormalizeS123();
            TcpTriggerReturnModeS123.SelectedValue = data.ReturnModeS123;
            TcpTriggerReturnOkS123.Text = data.ReturnOkValueS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnNgS123.Text = data.ReturnNgValueS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnErrorS123.Text = data.ReturnErrorValueS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnFunctionS123.SelectedValue = data.ReturnFunctionS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnAddressS123.Text = data.ReturnAddressS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnTimeoutS123.Text = data.ReturnTimeoutS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnHostS123.Text = data.ReturnHostS123;
            TcpTriggerReturnPortS123.Text = data.ReturnPortS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnUnitS123.Text = data.ReturnUnitS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnComS123.Text = data.ReturnComPortS123;
            TcpTriggerReturnBaudS123.Text = data.ReturnBaudS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnStationS123.Text = data.ReturnStationS123.ToString(CultureInfo.InvariantCulture);
            TcpTriggerReturnParityS123.SelectedValue = data.ReturnParityS123;
            TcpTriggerReturnStopBitsS123.SelectedValue = data.ReturnStopBitsS123.ToString(CultureInfo.InvariantCulture);
            UpdateTriggerReturnModeS123();
            TcpTriggerReturnStatusS123.Text = data.ReturnModeS123 == "None" ? "未启用结果回传" : "配置已加载；触发仍须手动启用";
        }

        private void TcpTriggerReturnDraftChangedS123(object sender, RoutedEventArgs e)
        {
            if (TcpTriggerReturnFieldsS123 == null) return;
            UpdateTriggerReturnModeS123();
            if (!uiReady || tcpTriggerUiUpdatingS104) return;
            TcpTriggerReturnStatusS123.Text = "回传草稿已变化；请应用配置并重新启用触发";
            TcpTriggerDraftChangedS104(sender, e);
        }

        private void UpdateTriggerReturnModeS123()
        {
            string mode = TcpTriggerReturnModeS123.SelectedValue as string ?? "None";
            bool modbus = mode == "ModbusTcp" || mode == "ModbusRtu";
            TcpTriggerReturnFieldsS123.Visibility = mode == "None" ? Visibility.Collapsed : Visibility.Visible;
            TcpTriggerReturnModbusFieldsS123.Visibility = modbus ? Visibility.Visible : Visibility.Collapsed;
            TcpTriggerReturnTcpFieldsS123.Visibility = mode == "ModbusTcp" ? Visibility.Visible : Visibility.Collapsed;
            TcpTriggerReturnRtuFieldsS123.Visibility = mode == "ModbusRtu" ? Visibility.Visible : Visibility.Collapsed;
            string target = mode == "TcpText" ? "原 TCP 连接文本" : mode == "ModbusTcp" ? "Modbus TCP 单点" : mode == "ModbusRtu" ? "Modbus RTU 单点" : "回传关闭";
            TcpTriggerPathS123.Text = "TCP 指令 → 固定目标流程 → 本次判定 → " + target;
        }

        private static int ParseTriggerReturnIntS123(string text, string label)
        {
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException(label + "须为整数。");
            return value;
        }

        private bool TryReadTriggerReturnS123(VmTcpFlowTriggerData data)
        {
            try
            {
                data.ReturnModeS123 = TcpTriggerReturnModeS123.SelectedValue as string ?? "None";
                if (data.ReturnModeS123 == "None") return true;
                data.ReturnOkValueS123 = ParseTriggerReturnIntS123(TcpTriggerReturnOkS123.Text, "OK 映射");
                data.ReturnNgValueS123 = ParseTriggerReturnIntS123(TcpTriggerReturnNgS123.Text, "NG 映射");
                data.ReturnErrorValueS123 = ParseTriggerReturnIntS123(TcpTriggerReturnErrorS123.Text, "异常映射");
                if (data.ReturnModeS123 == "TcpText") return true;
                data.ReturnFunctionS123 = ParseTriggerReturnIntS123(TcpTriggerReturnFunctionS123.SelectedValue as string, "功能码");
                data.ReturnAddressS123 = ParseTriggerReturnIntS123(TcpTriggerReturnAddressS123.Text, "PDU 地址");
                data.ReturnTimeoutS123 = ParseTriggerReturnIntS123(TcpTriggerReturnTimeoutS123.Text, "超时");
                if (data.ReturnFunctionS123 != 5 && data.ReturnFunctionS123 != 6)
                    throw new InvalidOperationException("回传只支持 05/06 单点写入。");
                if (data.ReturnFunctionS123 == 5 && data.ReturnOkValueS123 == data.ReturnNgValueS123)
                    throw new InvalidOperationException("05 写线圈的 OK/NG 必须不同；执行异常将不写入。");
                if (data.ReturnFunctionS123 == 6 && (data.ReturnOkValueS123 == data.ReturnNgValueS123 ||
                    data.ReturnOkValueS123 == data.ReturnErrorValueS123 || data.ReturnNgValueS123 == data.ReturnErrorValueS123))
                    throw new InvalidOperationException("06 写寄存器的 OK/NG/异常映射必须不同。");
                int[] values = data.ReturnFunctionS123 == 5
                    ? new[] { data.ReturnOkValueS123, data.ReturnNgValueS123 }
                    : new[] { data.ReturnOkValueS123, data.ReturnNgValueS123, data.ReturnErrorValueS123 };
                foreach (int value in values)
                {
                    if (data.ReturnFunctionS123 == 5 && value != 0 && value != 1)
                        throw new InvalidOperationException("05 写线圈的 OK/NG 只接受 0/1；执行异常不写入。");
                    if (data.ReturnFunctionS123 == 6 && (value < 0 || value > 65535))
                        throw new InvalidOperationException("06 回传映射须为 0–65535。");
                }
                if (data.ReturnModeS123 == "ModbusTcp")
                {
                    data.ReturnHostS123 = (TcpTriggerReturnHostS123.Text ?? "").Trim();
                    data.ReturnPortS123 = ParseTriggerReturnIntS123(TcpTriggerReturnPortS123.Text, "端口");
                    data.ReturnUnitS123 = ParseTriggerReturnIntS123(TcpTriggerReturnUnitS123.Text, "Unit ID");
                    BuildTriggerReturnTcpRequestS123(data, data.ReturnOkValueS123).Validate();
                }
                else if (data.ReturnModeS123 == "ModbusRtu")
                {
                    data.ReturnComPortS123 = (TcpTriggerReturnComS123.Text ?? "").Trim();
                    data.ReturnBaudS123 = ParseTriggerReturnIntS123(TcpTriggerReturnBaudS123.Text, "波特率");
                    data.ReturnStationS123 = ParseTriggerReturnIntS123(TcpTriggerReturnStationS123.Text, "站号");
                    data.ReturnParityS123 = TcpTriggerReturnParityS123.SelectedValue as string;
                    data.ReturnStopBitsS123 = ParseTriggerReturnIntS123(TcpTriggerReturnStopBitsS123.SelectedValue as string, "停止位");
                    BuildTriggerReturnRtuRequestS123(data, data.ReturnOkValueS123).Validate();
                }
                else throw new InvalidOperationException("未知回传目标。");
                return true;
            }
            catch (Exception ex)
            {
                TcpTriggerReturnStatusS123.Text = "配置未应用：" + ex.Message;
                TcpTriggerStatusS104.Text = TcpTriggerReturnStatusS123.Text;
                return false;
            }
        }

        private static ModbusTcpRequestS118 BuildTriggerReturnTcpRequestS123(VmTcpFlowTriggerData data, int value)
        {
            return new ModbusTcpRequestS118 { Host = data.ReturnHostS123, Port = data.ReturnPortS123,
                TimeoutMs = data.ReturnTimeoutS123, UnitId = data.ReturnUnitS123,
                FunctionCode = data.ReturnFunctionS123, Address = data.ReturnAddressS123,
                Quantity = 1, WriteValue = value };
        }

        private static ModbusRtuRequestS119 BuildTriggerReturnRtuRequestS123(VmTcpFlowTriggerData data, int value)
        {
            return new ModbusRtuRequestS119 { PortName = data.ReturnComPortS123, BaudRate = data.ReturnBaudS123,
                ParityName = data.ReturnParityS123, StopBitCount = data.ReturnStopBitsS123,
                TimeoutMs = data.ReturnTimeoutS123, Station = data.ReturnStationS123,
                FunctionCode = data.ReturnFunctionS123, Address = data.ReturnAddressS123,
                Quantity = 1, WriteValue = value };
        }

        private bool CanReturnTriggerResultS123(TcpTriggerSessionS104 session)
        {
            return ReferenceEquals(session, tcpTriggerSessionS104) && !isStopRequested && tcpService.IsConnected &&
                session.WorkspaceId == projectSessionIdS104 && session.FlowId == activeProjectFlowIdS104;
        }

        private async Task<bool> ReturnTriggerOutcomeS123(TcpTriggerSessionS104 session, FlowRunOutcome outcome)
        {
            if (session.ReturnConfig.ReturnModeS123 == "None") return true;
            if (string.IsNullOrWhiteSpace(outcome.RunId) || outcome.ResultCode == "STOP" || outcome.ResultCode == "TIMEOUT" || !CanReturnTriggerResultS123(session) ||
                lastResultIsSimulationS121 || flowTools.Count == 0 || flowTools.Any(ContainsCommunicationSimulationS121))
            {
                TcpTriggerReturnStatusS123.Text = "未回传：本次停止、超时、上下文变化或离线模拟。";
                DisarmTcpTriggerS104("结果未回传；请检查后重新启用");
                return false;
            }
            bool ok = !outcome.ExecutionError && outcome.ResultCode == "OK";
            bool ng = !outcome.ExecutionError && outcome.ResultCode == "NG";
            if (!ok && !ng && session.ReturnConfig.ReturnFunctionS123 == 5 &&
                session.ReturnConfig.ReturnModeS123 != "TcpText")
            {
                TcpTriggerReturnStatusS123.Text = "执行异常未写入：05 线圈只有两态；请用 06 或 TCP 文本表达三态。";
                DisarmTcpTriggerS104("异常未回传，请检查后重新启用");
                return false;
            }
            int mapped = ok ? session.ReturnConfig.ReturnOkValueS123 : ng ? session.ReturnConfig.ReturnNgValueS123 : session.ReturnConfig.ReturnErrorValueS123;
            string verdict = ok ? "OK" : ng ? "NG" : "ERROR";
            var config = session.ReturnConfig;
            // Freeze the original run/recipe ownership before I/O, not a newer project after a context-change failure.
            var returnAlarmContextS136 = FreezeProductionAlarmS136(VmProductionAlarmSourceS136.ResultReturnUncertain,
                VmProductionAlarmSeverityS136.Error, "本次设备结果回传待核对", "", null, outcome.RunId, outcome.TraceContextS136);
            isFlowExecutionActive = true;
            TcpTriggerReturnStatusS123.Text = "本次 " + verdict + " → " + mapped.ToString(CultureInfo.InvariantCulture) + "，回传中…";
            RefreshUiState();
            bool returnAttemptStartedS136 = false;
            try
            {
                if (!CanReturnTriggerResultS123(session)) throw new OperationCanceledException("目标已切换或用户已停止。");
                if (config.ReturnModeS123 == "ModbusTcp")
                {
                    var request = BuildTriggerReturnTcpRequestS123(config, mapped);
                    returnAttemptStartedS136 = true;
                    await Task.Run(() => ModbusTcpServiceS118.Execute(request, () => !CanReturnTriggerResultS123(session)));
                }
                else if (config.ReturnModeS123 == "ModbusRtu")
                {
                    var request = BuildTriggerReturnRtuRequestS123(config, mapped);
                    returnAttemptStartedS136 = true;
                    await Task.Run(() => ModbusRtuServiceS119.Execute(request, () => !CanReturnTriggerResultS123(session)));
                }
                else
                {
                    string payload = "RESULT=" + verdict + ",VALUE=" + mapped.ToString(CultureInfo.InvariantCulture) +
                        ",FLOW=" + session.FlowId + ",RUN=" + outcome.RunId;
                    returnAttemptStartedS136 = true;
                    await Task.Run(() => tcpService.Send(payload, System.Text.Encoding.ASCII, true, 1500));
                }
                if (!CanReturnTriggerResultS123(session)) throw new OperationCanceledException("回传后目标已切换；交付状态待设备核对。");
                TcpTriggerReturnStatusS123.Text = "已回传本次 " + verdict + " → " + mapped.ToString(CultureInfo.InvariantCulture) + " · " + outcome.RunId;
                AppendTcpHistory("流程回传：" + TcpTriggerReturnStatusS123.Text);
                return true;
            }
            catch (Exception ex)
            {
                TcpTriggerReturnStatusS123.Text = "回传失败/状态不确定：" + ex.Message + "；可能已送达，不自动重放。";
                AppendAlarm("流程回传失败：" + TcpTriggerReturnStatusS123.Text);
                if (returnAttemptStartedS136)
                {
                    returnAlarmContextS136.OccurredAtUtc = DateTime.UtcNow;
                    returnAlarmContextS136.Detail = TcpTriggerReturnStatusS123.Text;
                    PublishFrozenProductionAlarmS136(returnAlarmContextS136);
                } // A user stop before starting I/O is not a formal device-delivery fault.
                DisarmTcpTriggerS104("回传失败，请核对设备状态后手动重新启用");
                return false;
            }
            finally
            {
                isFlowExecutionActive = false;
                isStopRequested = false;
                RefreshUiState();
            }
        }
    }
}
