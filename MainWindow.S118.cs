using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadModbusDraftS118(VmToolParameterData p)
        {
            DockModbusHostS118.Text = p.ModbusHostS118 ?? string.Empty;
            DockModbusPortS118.Text = p.ModbusPortS118.ToString(CultureInfo.InvariantCulture);
            DockModbusTimeoutS118.Text = p.ModbusTimeoutMsS118.ToString(CultureInfo.InvariantCulture);
            DockModbusUnitS118.Text = p.ModbusUnitIdS118.ToString(CultureInfo.InvariantCulture);
            DockModbusFunctionS118.SelectedValue = p.ModbusFunctionCodeS118.ToString("D2", CultureInfo.InvariantCulture);
            DockModbusAddressS118.Text = p.ModbusAddressS118.ToString(CultureInfo.InvariantCulture);
            DockModbusQuantityS118.Text = p.ModbusQuantityS118.ToString(CultureInfo.InvariantCulture);
            DockModbusWriteValueS118.Text = p.ModbusWriteValueS118.ToString(CultureInfo.InvariantCulture);
            LoadCommunicationSimulationS121(p, VmToolKind.ModbusTcpExchange);
            DockModbusSampleResultS118.Text = "尚未解析；不会访问网络";
            DockModbusSampleResultS118.Foreground = Brushes.SlateGray;
            UpdateModbusModeUiS118();
            LoadModbusDecodeDraftS122(p, false);
            LoadCommunicationRecoveryS120(p, false);
        }

        private bool ReadModbusDraftS118(VmToolParameterData p)
        {
            int number;
            p.ModbusHostS118 = (DockModbusHostS118.Text ?? string.Empty).Trim();
            if (!int.TryParse(DockModbusPortS118.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("Modbus 端口必须是整数。");
            p.ModbusPortS118 = number;
            if (!int.TryParse(DockModbusTimeoutS118.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("Modbus 超时必须是整数毫秒。");
            p.ModbusTimeoutMsS118 = number;
            if (!int.TryParse(DockModbusUnitS118.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("Unit ID 必须是整数。");
            p.ModbusUnitIdS118 = number;
            if (!int.TryParse(DockModbusFunctionS118.SelectedValue as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("请选择 Modbus 功能。");
            p.ModbusFunctionCodeS118 = number;
            if (!int.TryParse(DockModbusAddressS118.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("PDU 地址必须是整数。");
            p.ModbusAddressS118 = number;
            if (VmModbusFunctionS118.IsRead(p.ModbusFunctionCodeS118))
            {
                if (!int.TryParse(DockModbusQuantityS118.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("读取数量必须是整数。");
                p.ModbusQuantityS118 = number;
            }
            else
            {
                if (!int.TryParse(DockModbusWriteValueS118.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("本地写入值必须是整数。");
                p.ModbusWriteValueS118 = number;
            }
            if (!ReadCommunicationRecoveryS120(p, false)) return false;
            ReadCommunicationSimulationS121(p, VmToolKind.ModbusTcpExchange);
            ReadModbusDecodeDraftS122(p, false);
            return true;
        }

        private void DockModbusFunctionS118_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 Modbus TCP 功能")) return;
            UpdateModbusModeUiS118();
            DockDraftValue_Changed(sender, e);
        }

        private void UpdateModbusModeUiS118()
        {
            if (DockModbusQuantityRowS118 == null || DockModbusWriteRowS118 == null) return;
            int function;
            bool isRead = int.TryParse(DockModbusFunctionS118.SelectedValue as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out function) && VmModbusFunctionS118.IsRead(function);
            DockModbusQuantityRowS118.Visibility = isRead ? Visibility.Visible : Visibility.Collapsed;
            DockModbusWriteRowS118.Visibility = isRead ? Visibility.Collapsed : Visibility.Visible;
            UpdateCommunicationRecoveryModeS120(false, isRead);
            UpdateModbusDecodeModeS122(false);
        }

        private ModbusTcpRequestS118 BuildModbusRequestS118(VmToolParameterData p)
        {
            return new ModbusTcpRequestS118
            {
                Host = p.ModbusHostS118,
                Port = p.ModbusPortS118,
                TimeoutMs = p.ModbusTimeoutMsS118,
                UnitId = p.ModbusUnitIdS118,
                FunctionCode = p.ModbusFunctionCodeS118,
                Address = p.ModbusAddressS118,
                Quantity = p.ModbusQuantityS118,
                WriteValue = p.ModbusWriteValueS118
            };
        }

        private void LoadModbusSampleS118_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("生成 Modbus TCP 模拟样本");
            if (editLeaseS137 < 0) return;
            VmToolParameterData draft = new VmToolParameterData();
            if (!ReadModbusDraftS118(draft)) return;
            try
            {
                ModbusTcpRequestS118 request = BuildModbusRequestS118(draft);
                byte[] frame = ModbusTcpServiceS118.BuildSampleResponse(request, 1);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                DockModbusSampleFrameS118.Text = BitConverter.ToString(frame).Replace("-", " ");
                DockModbusSampleResultS118.Text = "已生成本地示例响应；可修改字节后点击“解析帧”。";
                DockModbusSampleResultS118.Foreground = Brushes.SlateGray;
            }
            catch (Exception ex)
            {
                DockModbusSampleResultS118.Text = ex.Message;
                DockModbusSampleResultS118.Foreground = Brushes.IndianRed;
            }
        }

        private void ParseModbusSampleS118_Click(object sender, RoutedEventArgs e)
        {
            VmToolParameterData draft = new VmToolParameterData();
            if (!ReadModbusDraftS118(draft)) return;
            try
            {
                ModbusTcpRequestS118 request = BuildModbusRequestS118(draft);
                byte[] frame = ModbusTcpServiceS118.ParseHexFrame(DockModbusSampleFrameS118.Text);
                if (frame.Length < 2) throw new InvalidOperationException("Modbus TCP 样本帧缺少事务号。");
                ModbusTcpResponseS118 response = ModbusTcpServiceS118.ParseResponse(frame, request, (frame[0] << 8) | frame[1]);
                DockModbusSampleResultS118.Text = "解析成功 · " + FormatModbusDecodePreviewS122(response.Values, request.FunctionCode, draft) +
                    "；点击运行当前模块才发布流程输出。";
                DockModbusSampleResultS118.Foreground = Brushes.ForestGreen;
            }
            catch (Exception ex)
            {
                DockModbusSampleResultS118.Text = "解析失败：" + ex.Message;
                DockModbusSampleResultS118.Foreground = Brushes.IndianRed;
            }
        }

        private string BuildModbusInputSummaryS118(VmToolInstance tool)
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            if (VmModbusFunctionS118.IsRead(p.ModbusFunctionCodeS118)) return "读操作 · 无上游输入";
            VmToolInputBindingData binding = tool.GetInputBinding("WriteValue");
            VmToolInstance source = GetInputSourceTool(binding);
            return binding == null ? "本地写入值 " + p.ModbusWriteValueS118.ToString(CultureInfo.InvariantCulture) :
                "WriteValue ← " + (source == null ? "来源已失效" : source.InstanceName + "." + binding.SourcePortName);
        }

        private void RefreshModbusConnectionStatusS118(VmToolInstance tool, VmToolParameterData p)
        {
            VmToolInputBindingData binding = VmModbusFunctionS118.IsRead(p.ModbusFunctionCodeS118) ? null : tool.GetInputBinding("WriteValue");
            VmToolInstance source = GetInputSourceTool(binding);
            object value;
            bool waiting = binding != null && (source == null || !source.TryGetOutputValue(binding.SourcePortName, out value) || value == null);
            tool.ConfigurationStatus = waiting ? "等待上游" : "就绪";
            tool.ConnectionStatus = waiting ? "写入数值尚无值" : p.CommunicationSimulationS121 ? "离线样本 · 不连接网络" : (binding == null ? "本地参数 · 可运行" : "上游写入数值 · 有值");
            tool.ConnectionSummary = (p.CommunicationSimulationS121 ? "离线模拟 · " : "真实设备 · ") + VmModbusFunctionS118.Display(p.ModbusFunctionCodeS118) + " → " + p.ModbusHostS118 + ":" + p.ModbusPortS118.ToString(CultureInfo.InvariantCulture) + " · 地址 " + p.ModbusAddressS118.ToString(CultureInfo.InvariantCulture);
        }

        private InspectionRecord RunModbusTcpToolS118(VmToolInstance tool)
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            p.Normalize();
            ModbusTcpRequestS118 request = BuildResolvedModbusRequestS120(tool);
            ModbusTcpResponseS118 response = null;
            string error = string.Empty;
            int exceptionCode = 0;
            int attemptCount = 0;
            string attemptTrace = "未尝试";
            try
            {
                object prepared;
                var recovery = communicationPreparedResultsS120.TryGetValue(tool.ToolId, out prepared)
                    ? (CommunicationRecoveryResultS120<ModbusTcpResponseS118>)prepared
                    : p.CommunicationSimulationS121
                        ? (CommunicationRecoveryResultS120<ModbusTcpResponseS118>)BuildCommunicationSimulationS121(tool, p)
                        : ExecuteModbusTcpRecoveryS120(request, p);
                response = recovery.Value;
                attemptCount = recovery.AttemptCount;
                attemptTrace = recovery.Trace;
                if (recovery.Error != null)
                {
                    var protocolError = recovery.Error as ModbusTcpExceptionS118;
                    if (protocolError != null) exceptionCode = protocolError.ExceptionCode;
                    error = recovery.Error.Message;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (ModbusTcpExceptionS118 ex) { exceptionCode = ex.ExceptionCode; error = ex.Message; }
            catch (Exception ex) { error = ex.Message; }

            VmModbusDecodedValueS122 decoded = null;
            if (response != null)
            {
                try { decoded = DecodeModbusPrimaryS122(response.Values, request.FunctionCode, p); }
                catch (Exception ex) { error = "寄存器主值解码失败：" + ex.Message; }
            }
            bool ok = response != null && decoded != null;
            tool.SetOutputValue("IsSimulation", p.CommunicationSimulationS121);
            tool.SetOutputValue("Connected", response != null && !p.CommunicationSimulationS121);
            tool.SetOutputValue("FunctionCode", (double)request.FunctionCode);
            tool.SetOutputValue("Address", (double)request.Address);
            tool.SetOutputValue("ExceptionCode", (double)exceptionCode);
            tool.SetOutputValue("AttemptCount", (double)attemptCount);
            tool.SetOutputValue("AttemptTrace", attemptTrace);
            tool.SetOutputValue("Diagnostics", error);
            PublishModbusPrimaryS122(tool, decoded, error);
            tool.SetOutputValue("RequestHex", response == null && !p.CommunicationSimulationS121 ? string.Empty :
                BitConverter.ToString(ModbusTcpServiceS118.BuildRequest(request, response == null ? 0 : response.TransactionId)).Replace("-", " "));
            tool.SetOutputValue("ResponseHex", response == null
                ? (p.CommunicationSimulationS121 ? ResolveCommunicationSimulationPayloadS149(p, tool.Kind) : string.Empty)
                : response.FrameHex ?? string.Empty);
            if (response != null)
            {
                tool.SetOutputValue("Values", new VmNumberArrayS105(response.Values));
                tool.SetOutputValue("Count", (double)response.Values.Length);
                tool.SetOutputValue("TransactionId", (double)response.TransactionId);
                tool.SetOutputValue("RoundTripMs", response.RoundTripMs);
                if (VmModbusFunctionS118.IsBit(request.FunctionCode)) tool.SetOutputValue("IsOn", response.FirstIsOn);
            }
            tool.ErrorMessage = error;
            string message = ok
                ? string.Format(CultureInfo.InvariantCulture, "{0} · {1}:{2} · Unit {3} · 地址 {4} · {5} 项 · 主值 {6} ({8}) · {7:0} ms",
                    VmModbusFunctionS118.Display(request.FunctionCode), request.Host, request.Port, request.UnitId,
                    request.Address, response.Values.Length, decoded.ExactText, response.RoundTripMs, decoded.Type)
                : VmModbusFunctionS118.Display(request.FunctionCode) + " · 地址 " + request.Address.ToString(CultureInfo.InvariantCulture) + " · 尝试 " + attemptCount.ToString(CultureInfo.InvariantCulture) + " 次 · " + error;
            if (ok && !string.IsNullOrWhiteSpace(decoded.Warning)) message += " · " + decoded.Warning;
            if (p.CommunicationSimulationS121) message = "离线模拟" + CommunicationSampleLabelS149(p) + " · " + message + " · 未访问设备";
            InspectionRecord record = CreateRecord("ModbusTcpExchange", ok ? "OK" : "NG", decoded != null && decoded.Number.HasValue ? decoded.Number.Value : 0, message, tool);
            resultStore.Add(record);
            if (ok) LogInfo(tool.InstanceName + " 完成：" + message);
            else AppendAlarm(tool.InstanceName + " 异常：" + message);
            return record;
        }
    }
}
