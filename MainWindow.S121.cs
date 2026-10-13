using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool simulationRunActiveS121;
        private bool lastResultIsSimulationS121;

        private static bool IsCommunicationSimulationS121(VmToolInstance tool)
        {
            return tool != null && IsManagedCommunicationS120(tool) && tool.Parameters != null &&
                tool.Parameters.CommunicationSimulationS121;
        }

        private static bool ContainsCommunicationSimulationS121(VmToolInstance tool)
        {
            if (IsCommunicationSimulationS121(tool)) return true;
            return tool != null && tool.Parameters != null && tool.Parameters.Subflow != null &&
                ContainsCommunicationSimulationS121(tool.Parameters.Subflow.Tools, 0);
        }

        private static bool ContainsCommunicationSimulationS121(IEnumerable<ToolFlowRecipeItem> tools, int depth)
        {
            if (tools == null || depth >= 8) return false;
            foreach (ToolFlowRecipeItem item in tools)
            {
                if (item == null || item.Parameters == null) continue;
                VmToolKind kind;
                if (Enum.TryParse(item.ToolType, out kind) &&
                    (kind == VmToolKind.TcpClientExchange || kind == VmToolKind.ModbusTcpExchange || kind == VmToolKind.ModbusRtuExchange) &&
                    item.Parameters.CommunicationSimulationS121) return true;
                if (item.Parameters.Subflow != null && ContainsCommunicationSimulationS121(item.Parameters.Subflow.Tools, depth + 1)) return true;
            }
            return false;
        }

        private static bool IsUnsafeDuringSimulationS121(VmToolInstance tool)
        {
            if (tool == null) return false;
            if (IsManagedCommunicationS120(tool)) return !IsCommunicationSimulationS121(tool);
            if (tool.Kind == VmToolKind.CSharpDllScript || tool.Kind == VmToolKind.HDevelop ||
                tool.Kind == VmToolKind.GlobalVariableWrite) return true;
            return tool.Kind == VmToolKind.ImageSource && tool.Parameters != null &&
                VmImageSourceModeS114.Normalize(tool.Parameters.ImageSourceModeS114) == VmImageSourceModeS114.Camera;
        }

        private void LoadCommunicationSimulationS121(VmToolParameterData p, VmToolKind kind)
        {
            bool wasLoading = communicationQueueLoadingS149;
            communicationQueueLoadingS149 = true;
            try
            {
                if (kind == VmToolKind.TcpClientExchange)
                {
                    DockTcpSimulationS121.IsChecked = p.CommunicationSimulationS121;
                    DockTcpSampleResponseS117.Text = p.TcpSimulationResponseS121 ?? string.Empty;
                }
                else if (kind == VmToolKind.ModbusTcpExchange)
                {
                    DockModbusSimulationS121.IsChecked = p.CommunicationSimulationS121;
                    DockModbusSampleFrameS118.Text = p.ModbusSimulationFrameS121 ?? string.Empty;
                }
                else if (kind == VmToolKind.ModbusRtuExchange)
                {
                    DockRtuSimulationS121.IsChecked = p.CommunicationSimulationS121;
                    DockRtuSampleFrameS119.Text = p.RtuSimulationFrameS121 ?? string.Empty;
                }
                LoadCommunicationQueueS149(p, kind);
            }
            finally { communicationQueueLoadingS149 = wasLoading; }
        }

        private void ReadCommunicationSimulationS121(VmToolParameterData p, VmToolKind kind)
        {
            if (kind == VmToolKind.TcpClientExchange)
            {
                p.CommunicationSimulationS121 = DockTcpSimulationS121.IsChecked == true;
                p.TcpSimulationResponseS121 = DockTcpSampleResponseS117.Text ?? string.Empty;
            }
            else if (kind == VmToolKind.ModbusTcpExchange)
            {
                p.CommunicationSimulationS121 = DockModbusSimulationS121.IsChecked == true;
                p.ModbusSimulationFrameS121 = DockModbusSampleFrameS118.Text ?? string.Empty;
            }
            else if (kind == VmToolKind.ModbusRtuExchange)
            {
                p.CommunicationSimulationS121 = DockRtuSimulationS121.IsChecked == true;
                p.RtuSimulationFrameS121 = DockRtuSampleFrameS119.Text ?? string.Empty;
            }
            ReadCommunicationQueueS149(p, kind);
        }

        private object BuildCommunicationSimulationS121(VmToolInstance tool, VmToolParameterData p)
        {
            if (tool.Kind == VmToolKind.TcpClientExchange)
            {
                var prepared = new PreparedTcpExchangeS120();
                try
                {
                    TcpFlowClientRequest request = BuildTcpRequestS120(tool, p);
                    string sample = ResolveCommunicationSimulationPayloadS149(p, tool.Kind);
                    if (string.IsNullOrWhiteSpace(sample)) throw new InvalidOperationException("TCP 离线模拟缺少样本响应。");
                    prepared.Result = new TcpFlowClientResult
                    {
                        Connected = false, SentText = request.Message, ReceivedText = sample,
                        BytesSent = 0, BytesReceived = Encoding.UTF8.GetByteCount(sample), RoundTripMilliseconds = 0
                    };
                }
                catch (Exception ex) { prepared.Error = ex; }
                return prepared;
            }
            if (tool.Kind == VmToolKind.ModbusTcpExchange)
            {
                var result = new CommunicationRecoveryResultS120<ModbusTcpResponseS118>
                { Attempts = new List<CommunicationAttemptS120>() };
                try
                {
                    ModbusTcpRequestS118 request = BuildResolvedModbusRequestS120(tool);
                    string sample = ResolveCommunicationSimulationPayloadS149(p, tool.Kind);
                    byte[] frame = ModbusTcpServiceS118.ParseHexFrame(sample);
                    if (frame.Length < 2) throw new InvalidOperationException("Modbus TCP 样本帧缺少事务号。");
                    int transaction = (frame[0] << 8) | frame[1];
                    result.Value = ModbusTcpServiceS118.ParseResponse(frame, request, transaction);
                }
                catch (Exception ex) { result.Error = ex; }
                result.Attempts.Add(new CommunicationAttemptS120 { Index = 1, Status = result.Error == null ? "离线解析成功" : "离线解析失败", Detail = result.Error == null ? "未访问设备" : result.Error.Message });
                return result;
            }
            var rtu = new CommunicationRecoveryResultS120<ModbusRtuResponseS119>
            { Attempts = new List<CommunicationAttemptS120>() };
            try
            {
                ModbusRtuRequestS119 request = BuildResolvedRtuRequestS120(tool);
                string sample = ResolveCommunicationSimulationPayloadS149(p, tool.Kind);
                byte[] frame = ModbusRtuServiceS119.ParseHexFrame(sample);
                rtu.Value = ModbusRtuServiceS119.ParseResponse(frame, request);
            }
            catch (Exception ex) { rtu.Error = ex; }
            rtu.Attempts.Add(new CommunicationAttemptS120 { Index = 1, Status = rtu.Error == null ? "离线解析成功" : "离线解析失败", Detail = rtu.Error == null ? "未打开串口" : rtu.Error.Message });
            return rtu;
        }
    }
}
