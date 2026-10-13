using System;
using System.Globalization;
using System.IO.Ports;
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
        private string[] availableRtuPortsS119 = new string[0];

        private void LoadRtuDraftS119(VmToolParameterData p)
        {
            RefreshRtuPortsS119(p.RtuPortNameS119);
            DockRtuBaudS119.Text = p.RtuBaudRateS119.ToString(CultureInfo.InvariantCulture);
            DockRtuParityS119.SelectedValue = p.RtuParityS119 ?? "Even";
            DockRtuStopBitsS119.SelectedValue = p.RtuStopBitsS119.ToString(CultureInfo.InvariantCulture);
            DockRtuTimeoutS119.Text = p.RtuTimeoutMsS119.ToString(CultureInfo.InvariantCulture);
            DockRtuStationS119.Text = p.RtuStationS119.ToString(CultureInfo.InvariantCulture);
            DockRtuFunctionS119.SelectedValue = p.RtuFunctionCodeS119.ToString("D2", CultureInfo.InvariantCulture);
            DockRtuAddressS119.Text = p.RtuAddressS119.ToString(CultureInfo.InvariantCulture);
            DockRtuQuantityS119.Text = p.RtuQuantityS119.ToString(CultureInfo.InvariantCulture);
            DockRtuWriteValueS119.Text = p.RtuWriteValueS119.ToString(CultureInfo.InvariantCulture);
            LoadCommunicationSimulationS121(p, VmToolKind.ModbusRtuExchange);
            DockRtuSampleResultS119.Text = "尚未解析；不会打开串口";
            DockRtuSampleResultS119.Foreground = Brushes.SlateGray;
            UpdateRtuModeUiS119();
            LoadModbusDecodeDraftS122(p, true);
            RefreshRtuBriefS119(p);
            LoadCommunicationRecoveryS120(p, true);
        }

        private void RefreshRtuPortsS119(string preserve)
        {
            string current = preserve ?? DockRtuPortS119.Text ?? string.Empty;
            try
            {
                string[] ports = SerialPort.GetPortNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
                availableRtuPortsS119 = ports;
                DockRtuPortS119.ItemsSource = ports;
                DockRtuPortS119.Text = current;
                UpdateRtuPortStatusS119(current);
            }
            catch (Exception ex)
            {
                availableRtuPortsS119 = new string[0];
                DockRtuPortS119.Text = current;
                DockRtuPortStatusS119.Text = "串口列表读取失败：" + ex.Message + "；仍可手工填写。";
            }
        }

        private void RefreshRtuPortsS119_Click(object sender, RoutedEventArgs e)
        {
            RefreshRtuPortsS119(DockRtuPortS119.Text);
        }

        private void DockRtuPortS119_Changed(object sender, RoutedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 RTU 串口参数")) return;
            if (DockRtuPortS119 != null) UpdateRtuPortStatusS119(DockRtuPortS119.Text);
            DockDraftValue_Changed(sender, e);
        }

        private void UpdateRtuPortStatusS119(string selected)
        {
            if (DockRtuPortStatusS119 == null) return;
            string current = (selected ?? string.Empty).Trim();
            if (availableRtuPortsS119.Length == 0)
                DockRtuPortStatusS119.Text = "本机未发现 COM 口；可手工填写，但需设备接入后才能运行。";
            else
                DockRtuPortStatusS119.Text = (availableRtuPortsS119.Any(name => string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
                    ? current + " 已发现" : current + " 未在当前串口列表中") +
                    (ModbusRtuServiceS119.IsReserved(current) ? " · 本程序正在占用" : " · 仅运行时打开并释放");
        }

        private bool ReadRtuDraftS119(VmToolParameterData p)
        {
            int number;
            p.RtuPortNameS119 = (DockRtuPortS119.Text ?? string.Empty).Trim();
            if (!int.TryParse(DockRtuBaudS119.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("RTU 波特率必须是整数。");
            p.RtuBaudRateS119 = number;
            p.RtuParityS119 = DockRtuParityS119.SelectedValue as string;
            if (!int.TryParse(DockRtuStopBitsS119.SelectedValue as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("请选择 RTU 停止位。");
            p.RtuStopBitsS119 = number;
            if (!int.TryParse(DockRtuTimeoutS119.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("RTU 超时必须是整数毫秒。");
            p.RtuTimeoutMsS119 = number;
            if (!int.TryParse(DockRtuStationS119.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("RTU 站号必须是整数。");
            p.RtuStationS119 = number;
            if (!int.TryParse(DockRtuFunctionS119.SelectedValue as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("请选择 RTU 功能。");
            p.RtuFunctionCodeS119 = number;
            if (!int.TryParse(DockRtuAddressS119.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("RTU PDU 地址必须是整数。");
            p.RtuAddressS119 = number;
            if (VmModbusFunctionS118.IsRead(p.RtuFunctionCodeS119))
            {
                if (!int.TryParse(DockRtuQuantityS119.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("RTU 读取数量必须是整数。");
                p.RtuQuantityS119 = number;
            }
            else
            {
                if (!int.TryParse(DockRtuWriteValueS119.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return FailDockDraft("RTU 写入值必须是整数。");
                p.RtuWriteValueS119 = number;
            }
            if (!ReadCommunicationRecoveryS120(p, true)) return false;
            ReadCommunicationSimulationS121(p, VmToolKind.ModbusRtuExchange);
            ReadModbusDecodeDraftS122(p, true);
            return true;
        }

        private void DockRtuFunctionS119_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 RTU 功能")) return;
            UpdateRtuModeUiS119();
            DockDraftValue_Changed(sender, e);
        }

        private void UpdateRtuModeUiS119()
        {
            if (DockRtuQuantityRowS119 == null || DockRtuWriteRowS119 == null) return;
            int function;
            bool read = int.TryParse(DockRtuFunctionS119.SelectedValue as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out function) && VmModbusFunctionS118.IsRead(function);
            DockRtuQuantityRowS119.Visibility = read ? Visibility.Visible : Visibility.Collapsed;
            DockRtuWriteRowS119.Visibility = read ? Visibility.Collapsed : Visibility.Visible;
            UpdateCommunicationRecoveryModeS120(true, read);
            UpdateModbusDecodeModeS122(true);
        }

        private void RefreshRtuBriefS119(VmToolParameterData p)
        {
            DockRtuBriefS119.Text = string.Format(CultureInfo.InvariantCulture, "{0} · 8{1}{2} · 站号 {3}；细节按需展开",
                p.RtuBaudRateS119, p.RtuParityS119 == "Even" ? "E" : p.RtuParityS119 == "Odd" ? "O" : "N",
                p.RtuStopBitsS119, p.RtuStationS119);
        }

        private void DockRtuTransportS119_Changed(object sender, RoutedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 RTU 传输设置")) return;
            if (DockRtuBriefS119 != null && DockRtuBaudS119 != null && DockRtuStationS119 != null && DockRtuParityS119 != null && DockRtuStopBitsS119 != null)
            {
                string parity = DockRtuParityS119.SelectedValue as string;
                DockRtuBriefS119.Text = (DockRtuBaudS119.Text ?? "?") + " · 8" +
                    (parity == "Even" ? "E" : parity == "Odd" ? "O" : "N") +
                    (DockRtuStopBitsS119.SelectedValue as string ?? "?") + " · 站号 " +
                    (DockRtuStationS119.Text ?? "?") + "；细节按需展开";
            }
            DockDraftValue_Changed(sender, e);
        }

        private ModbusRtuRequestS119 BuildRtuRequestS119(VmToolParameterData p)
        {
            return new ModbusRtuRequestS119
            {
                PortName = p.RtuPortNameS119, BaudRate = p.RtuBaudRateS119, ParityName = p.RtuParityS119,
                StopBitCount = p.RtuStopBitsS119, TimeoutMs = p.RtuTimeoutMsS119, Station = p.RtuStationS119,
                FunctionCode = p.RtuFunctionCodeS119, Address = p.RtuAddressS119,
                Quantity = p.RtuQuantityS119, WriteValue = p.RtuWriteValueS119
            };
        }

        private void LoadRtuSampleS119_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("生成 RTU 模拟样本");
            if (editLeaseS137 < 0) return;
            VmToolParameterData draft = new VmToolParameterData();
            if (!ReadRtuDraftS119(draft)) return;
            try
            {
                byte[] frame = ModbusRtuServiceS119.BuildSampleResponse(BuildRtuRequestS119(draft));
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                DockRtuSampleFrameS119.Text = BitConverter.ToString(frame).Replace("-", " ");
                DockRtuSampleResultS119.Text = "已生成本地示例帧；末两字节为 CRC 低/高，可修改后试解析。";
                DockRtuSampleResultS119.Foreground = Brushes.SlateGray;
            }
            catch (Exception ex) { DockRtuSampleResultS119.Text = ex.Message; DockRtuSampleResultS119.Foreground = Brushes.IndianRed; }
        }

        private void ParseRtuSampleS119_Click(object sender, RoutedEventArgs e)
        {
            VmToolParameterData draft = new VmToolParameterData();
            if (!ReadRtuDraftS119(draft)) return;
            try
            {
                byte[] frame = ModbusRtuServiceS119.ParseHexFrame(DockRtuSampleFrameS119.Text);
                ModbusRtuResponseS119 result = ModbusRtuServiceS119.ParseResponse(frame, BuildRtuRequestS119(draft));
                DockRtuSampleResultS119.Text = "解析成功 · " + FormatModbusDecodePreviewS122(result.Values, draft.RtuFunctionCodeS119, draft) +
                    "；点击运行当前模块才发布流程输出。";
                DockRtuSampleResultS119.Foreground = Brushes.ForestGreen;
            }
            catch (Exception ex) { DockRtuSampleResultS119.Text = "解析失败：" + ex.Message; DockRtuSampleResultS119.Foreground = Brushes.IndianRed; }
        }

        private string BuildRtuInputSummaryS119(VmToolInstance tool)
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            if (VmModbusFunctionS118.IsRead(p.RtuFunctionCodeS119)) return "RTU 读操作 · 无上游输入";
            VmToolInputBindingData binding = tool.GetInputBinding("WriteValue");
            VmToolInstance source = GetInputSourceTool(binding);
            return binding == null ? "本地写入值 " + p.RtuWriteValueS119.ToString(CultureInfo.InvariantCulture) :
                "WriteValue ← " + (source == null ? "来源已失效" : source.InstanceName + "." + binding.SourcePortName);
        }

        private void RefreshRtuConnectionStatusS119(VmToolInstance tool, VmToolParameterData p)
        {
            VmToolInputBindingData binding = VmModbusFunctionS118.IsRead(p.RtuFunctionCodeS119) ? null : tool.GetInputBinding("WriteValue");
            VmToolInstance source = GetInputSourceTool(binding);
            object value;
            bool waiting = binding != null && (source == null || !source.TryGetOutputValue(binding.SourcePortName, out value) || value == null);
            bool busy = !p.CommunicationSimulationS121 && ModbusRtuServiceS119.IsReserved(p.RtuPortNameS119);
            tool.ConfigurationStatus = waiting ? "等待上游" : busy ? "设备占用" : "就绪";
            tool.ConnectionStatus = waiting ? "写入值尚无值" : busy ? "本程序当前占用串口" : p.CommunicationSimulationS121 ? "离线样本 · 不打开串口" : "单次打开 · 待运行";
            tool.ConnectionSummary = (p.CommunicationSimulationS121 ? "离线模拟 · " : "真实设备 · ") + VmModbusFunctionS118.Display(p.RtuFunctionCodeS119) + " → " + p.RtuPortNameS119 + " · 站号 " + p.RtuStationS119.ToString(CultureInfo.InvariantCulture);
        }

        private InspectionRecord RunModbusRtuToolS119(VmToolInstance tool)
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            p.Normalize();
            ModbusRtuRequestS119 request = BuildResolvedRtuRequestS120(tool);
            ModbusRtuResponseS119 response = null;
            string error = string.Empty;
            int exceptionCode = 0;
            int attemptCount = 0;
            string attemptTrace = "未尝试";
            try
            {
                object prepared;
                var recovery = communicationPreparedResultsS120.TryGetValue(tool.ToolId, out prepared)
                    ? (CommunicationRecoveryResultS120<ModbusRtuResponseS119>)prepared
                    : p.CommunicationSimulationS121
                        ? (CommunicationRecoveryResultS120<ModbusRtuResponseS119>)BuildCommunicationSimulationS121(tool, p)
                        : ExecuteModbusRtuRecoveryS120(request, p);
                response = recovery.Value;
                attemptCount = recovery.AttemptCount;
                attemptTrace = recovery.Trace;
                if (recovery.Error != null)
                {
                    var protocolError = recovery.Error as ModbusRtuExceptionS119;
                    if (protocolError != null) exceptionCode = protocolError.ExceptionCode;
                    error = recovery.Error.Message;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (ModbusRtuExceptionS119 ex) { exceptionCode = ex.ExceptionCode; error = ex.Message; }
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
            tool.SetOutputValue("Station", (double)request.Station);
            tool.SetOutputValue("ExceptionCode", (double)exceptionCode);
            tool.SetOutputValue("AttemptCount", (double)attemptCount);
            tool.SetOutputValue("AttemptTrace", attemptTrace);
            tool.SetOutputValue("Diagnostics", error);
            PublishModbusPrimaryS122(tool, decoded, error);
            tool.SetOutputValue("RequestHex", BitConverter.ToString(ModbusRtuServiceS119.BuildRequest(request)).Replace("-", " "));
            tool.SetOutputValue("ResponseHex", response == null
                ? (p.CommunicationSimulationS121 ? ResolveCommunicationSimulationPayloadS149(p, tool.Kind) : string.Empty)
                : response.FrameHex ?? string.Empty);
            if (response != null)
            {
                tool.SetOutputValue("Values", new VmNumberArrayS105(response.Values));
                tool.SetOutputValue("Count", (double)response.Values.Length);
                tool.SetOutputValue("RoundTripMs", response.RoundTripMs);
                if (VmModbusFunctionS118.IsBit(request.FunctionCode)) tool.SetOutputValue("IsOn", response.FirstIsOn);
            }
            tool.ErrorMessage = error;
            string message = ok
                ? string.Format(CultureInfo.InvariantCulture, "{0} · {1} · 站号 {2} · 地址 {3} · {4} 项 · 主值 {5} ({7}) · {6:0} ms",
                    VmModbusFunctionS118.Display(request.FunctionCode), request.PortName, request.Station, request.Address,
                    response.Values.Length, decoded.ExactText, response.RoundTripMs, decoded.Type)
                : VmModbusFunctionS118.Display(request.FunctionCode) + " · " + request.PortName + " · 尝试 " + attemptCount.ToString(CultureInfo.InvariantCulture) + " 次 · " + error;
            if (ok && !string.IsNullOrWhiteSpace(decoded.Warning)) message += " · " + decoded.Warning;
            if (p.CommunicationSimulationS121) message = "离线模拟" + CommunicationSampleLabelS149(p) + " · " + message + " · 未打开串口";
            InspectionRecord record = CreateRecord("ModbusRtuExchange", ok ? "OK" : "NG", decoded != null && decoded.Number.HasValue ? decoded.Number.Value : 0, message, tool);
            resultStore.Add(record);
            if (ok) LogInfo(tool.InstanceName + " 完成：" + message);
            else AppendAlarm(tool.InstanceName + " 异常：" + message);
            return record;
        }
    }
}
