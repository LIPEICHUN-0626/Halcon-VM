using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadTcpResponseDraftS117(VmToolParameterData parameters)
        {
            DockTcpResponseModeS117.SelectedValue = VmTcpResponseModeS117.Normalize(parameters.TcpResponseModeS117);
            DockTcpResponseDelimiterS117.Text = parameters.TcpResponseDelimiterS117 ?? ",";
            DockTcpResponseStringS117.Text = parameters.TcpResponseStringSelectorS117 ?? string.Empty;
            DockTcpResponseNumberS117.Text = parameters.TcpResponseNumberSelectorS117 ?? string.Empty;
            DockTcpResponseBooleanS117.Text = parameters.TcpResponseBooleanSelectorS117 ?? string.Empty;
            LoadCommunicationSimulationS121(parameters, VmToolKind.TcpClientExchange);
            DockTcpSampleResultS117.Text = "尚未试解析";
            DockTcpSampleResultS117.Foreground = Brushes.LightSlateGray;
            UpdateTcpResponseModeVisibilityS117();
            RefreshTcpResponsePreviewS117();
        }

        private void ReadTcpResponseDraftS117(VmToolParameterData parameters)
        {
            parameters.TcpResponseModeS117 = VmTcpResponseModeS117.Normalize(DockTcpResponseModeS117.SelectedValue as string);
            parameters.TcpResponseDelimiterS117 = DockTcpResponseDelimiterS117.Text ?? string.Empty;
            parameters.TcpResponseStringSelectorS117 = (DockTcpResponseStringS117.Text ?? string.Empty).Trim();
            parameters.TcpResponseNumberSelectorS117 = (DockTcpResponseNumberS117.Text ?? string.Empty).Trim();
            parameters.TcpResponseBooleanSelectorS117 = (DockTcpResponseBooleanS117.Text ?? string.Empty).Trim();
            ReadCommunicationSimulationS121(parameters, VmToolKind.TcpClientExchange);
        }

        private void DockTcpResponseModeS117_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTcpResponseModeVisibilityS117();
            DockDraftValue_Changed(sender, e);
        }

        private void UpdateTcpResponseModeVisibilityS117()
        {
            if (DockTcpResponseFieldsS117 == null) return;
            string mode = VmTcpResponseModeS117.Normalize(DockTcpResponseModeS117.SelectedValue as string);
            DockTcpResponseFieldsS117.Visibility = mode == VmTcpResponseModeS117.None ? Visibility.Collapsed : Visibility.Visible;
            DockTcpResponseDelimiterRowS117.Visibility = mode == VmTcpResponseModeS117.Delimited ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string CompactTcpPreviewS117(object value)
        {
            string text = value == null ? string.Empty : value.ToString();
            text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            return text.Length > 64 ? text.Substring(0, 64) + "…" : text;
        }

        private void RefreshTcpResponsePreviewS117()
        {
            if (DockTcpResponsePreviewS117 == null) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.TcpClientExchange)
            {
                DockTcpResponsePreviewS117.Text = "运行后显示本次收发摘要。";
                return;
            }
            object sent;
            object received;
            bool hasSent = tool.TryGetOutputValue("SentText", out sent);
            bool hasReceived = tool.TryGetOutputValue("ReceivedText", out received);
            DockTcpResponsePreviewS117.Text = !hasSent && !hasReceived
                ? "模板变量来自 I/O 订阅；运行后显示本次收发摘要。"
                : (IsCommunicationSimulationS121(tool) ? "离线模拟 · 拟发送 “" : "发送 “") + CompactTcpPreviewS117(sent) + "” · 接收 “" + CompactTcpPreviewS117(received) + "”" +
                  (string.IsNullOrWhiteSpace(tool.ErrorMessage) ? string.Empty : " · " + tool.ErrorMessage);
        }

        private static string BuildTcpMappedSummaryS117(IDictionary<string, object> mapped)
        {
            if (mapped == null || mapped.Count == 0) return string.Empty;
            return " · 映射 " + string.Join("、", mapped.Select(item => item.Key.Replace("Response", string.Empty) +
                "=" + CompactTcpPreviewS117(item.Value)).ToArray());
        }

        private void TryParseTcpSampleS117_Click(object sender, RoutedEventArgs e)
        {
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.TcpClientExchange) return;
            VmToolParameterData parameters = (dockEditingTool.Parameters ?? new VmToolParameterData()).Clone();
            ReadTcpResponseDraftS117(parameters);
            parameters.TcpClientWaitForResponse = DockTcpClientWaitForResponseCheckBox.IsChecked == true;
            try
            {
                Dictionary<string, object> mapped = VmTcpResponseMappingS117.Parse(DockTcpSampleResponseS117.Text ?? string.Empty, parameters);
                DockTcpSampleResultS117.Text = mapped.Count == 0 ? "未启用字段映射；请先选择响应格式。" :
                    "试解析成功" + BuildTcpMappedSummaryS117(mapped) + "。此结果不会写入流程输出。";
                DockTcpSampleResultS117.Foreground = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                DockTcpSampleResultS117.Text = "试解析失败：" + ex.Message;
                DockTcpSampleResultS117.Foreground = Brushes.LightCoral;
            }
        }
    }
}
