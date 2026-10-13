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
        private sealed class AcquisitionDiagnosticS115
        {
            public string Outcome;
            public string Source;
            public string Timestamp;
            public double DurationMs;
            public double FrameIndex;
            public string Error;
        }

        private readonly Dictionary<string, AcquisitionDiagnosticS115> acquisitionDiagnosticsS115 =
            new Dictionary<string, AcquisitionDiagnosticS115>(StringComparer.OrdinalIgnoreCase);

        private static void ConfigureSoftwareTriggerS115(CameraSessionS114 session)
        {
            try
            {
                string sources = session.Grabber.GetFramegrabberParam("TriggerSource_values").ToString();
                string names = session.Grabber.GetFramegrabberParam("available_param_names").ToString();
                if (sources.IndexOf("Software", StringComparison.OrdinalIgnoreCase) < 0 ||
                    names.IndexOf("TriggerSoftware", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("设备未公开 Software 触发源或 TriggerSoftware 命令。");
                session.OriginalTriggerMode = session.Grabber.GetFramegrabberParam("TriggerMode").S;
                session.OriginalTriggerSource = session.Grabber.GetFramegrabberParam("TriggerSource").S;
                session.Grabber.SetFramegrabberParam("TriggerMode", "On");
                session.Grabber.SetFramegrabberParam("TriggerSource", "Software");
                session.Grabber.GrabImageStart(-1);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("此 HALCON 接口/设备不能建立软件触发采集：" + ex.Message, ex);
            }
        }

        private void RecordAcquisitionAttemptS115(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.ImageSource ||
                tool.RunStatus != "完成" && tool.RunStatus != "失败") return;
            object path;
            object time;
            object frame;
            object acquisitionMs;
            bool ok = tool.RunStatus == "完成" && tool.ResultCode == "OK";
            acquisitionDiagnosticsS115[tool.ToolId] = new AcquisitionDiagnosticS115
            {
                Outcome = ok ? "OK" : "失败",
                Source = tool.TryGetOutputValue("Path", out path) && path != null
                    ? path.ToString() : BuildImageSourceConnectionSummaryS114(tool.Parameters ?? new VmToolParameterData()),
                Timestamp = tool.TryGetOutputValue("Timestamp", out time) && time != null
                    ? time.ToString() : DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                DurationMs = tool.TryGetOutputValue("AcquisitionMs", out acquisitionMs) && acquisitionMs is double
                    ? (double)acquisitionMs : tool.ElapsedMilliseconds,
                FrameIndex = tool.TryGetOutputValue("FrameIndex", out frame) && frame is double ? (double)frame : 0,
                Error = ok ? string.Empty : tool.ErrorMessage
            };
            RefreshAcquisitionDiagnosticS115();
            RefreshDeviceResourcesS116();
        }

        private void RecordCameraConnectionFailureS115(VmToolInstance tool, Exception error)
        {
            if (tool == null) return;
            acquisitionDiagnosticsS115[tool.ToolId] = new AcquisitionDiagnosticS115
            {
                Outcome = "连接失败",
                Source = BuildImageSourceConnectionSummaryS114(tool.Parameters ?? new VmToolParameterData()),
                Timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Error = error.Message
            };
            RefreshAcquisitionDiagnosticS115();
            RefreshDeviceResourcesS116();
        }

        private void RefreshAcquisitionDiagnosticS115()
        {
            if (DockAcquisitionLastS115 == null) return;
            VmToolInstance source = dockEditingTool;
            AcquisitionDiagnosticS115 diagnostic;
            if (source == null || source.Kind != VmToolKind.ImageSource ||
                !acquisitionDiagnosticsS115.TryGetValue(source.ToolId, out diagnostic))
            {
                DockAcquisitionLastS115.Text = "尚未采集；连接态不会随方案保存。";
                DockAcquisitionErrorS115.Text = string.Empty;
                DockAcquisitionErrorS115.Visibility = Visibility.Collapsed;
                return;
            }
            DockAcquisitionLastS115.Text = diagnostic.Outcome + " · " + diagnostic.Timestamp +
                " · " + diagnostic.DurationMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms" +
                (diagnostic.FrameIndex > 0 ? " · 第 " + diagnostic.FrameIndex.ToString("0", CultureInfo.InvariantCulture) + " 帧" : string.Empty) +
                Environment.NewLine + diagnostic.Source;
            DockAcquisitionErrorS115.Text = diagnostic.Error;
            DockAcquisitionErrorS115.Visibility = string.IsNullOrWhiteSpace(diagnostic.Error) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RefreshImageAcquisitionBarS115(bool historyReadOnly)
        {
            if (ImageAcquisitionBarS115 == null) return;
            string key = ImageContextComboBox == null ? null : ImageContextComboBox.SelectedValue as string;
            VmToolInstance source = !historyReadOnly && key != null && key.StartsWith(VmImageContextOption.SourcePrefix, StringComparison.Ordinal)
                ? flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ImageSource &&
                    string.Equals(item.ToolId, key.Substring(VmImageContextOption.SourcePrefix.Length), StringComparison.OrdinalIgnoreCase))
                : null;
            ImageAcquisitionBarS115.Visibility = source == null ? Visibility.Collapsed : Visibility.Visible;
            if (source == null) return;
            AcquisitionDiagnosticS115 diagnostic;
            acquisitionDiagnosticsS115.TryGetValue(source.ToolId, out diagnostic);
            VmToolParameterData parameters = source.Parameters ?? new VmToolParameterData();
            string status = BuildImageSourceConnectionStatusS114(source, parameters);
            ImageAcquisitionSummaryS115.Text = diagnostic == null ? status :
                status + " · " + diagnostic.Outcome + " · " + diagnostic.DurationMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms" +
                (diagnostic.FrameIndex > 0 ? " · 帧 " + diagnostic.FrameIndex.ToString("0", CultureInfo.InvariantCulture) : string.Empty) +
                " · " + diagnostic.Timestamp;
            string error = diagnostic == null ? source.ErrorMessage : diagnostic.Error;
            ImageAcquisitionSummaryS115.Foreground = string.IsNullOrWhiteSpace(error) ? Brushes.LightSlateGray : Brushes.LightCoral;
            ImageAcquisitionSummaryS115.ToolTip = string.IsNullOrWhiteSpace(error) ? ImageAcquisitionSummaryS115.Text : error;
            bool needsRepair = !string.IsNullOrWhiteSpace(error) ||
                !string.IsNullOrWhiteSpace(parameters.Validate(VmToolKind.ImageSource)) ||
                VmImageSourceModeS114.Normalize(parameters.ImageSourceModeS114) == VmImageSourceModeS114.Camera &&
                !cameraSessionsS114.ContainsKey(source.ToolId);
            ImageAcquisitionRepairS115.Visibility = needsRepair ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenImageAcquisitionSourceS115_Click(object sender, RoutedEventArgs e)
        {
            if (!CanNavigateInputRepairS96()) return;
            string key = ImageContextComboBox.SelectedValue as string;
            if (key == null || !key.StartsWith(VmImageContextOption.SourcePrefix, StringComparison.Ordinal)) return;
            VmToolInstance source = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ImageSource &&
                string.Equals(item.ToolId, key.Substring(VmImageContextOption.SourcePrefix.Length), StringComparison.OrdinalIgnoreCase));
            if (source == null) return;
            OpenCentralModuleConfigurationS77(source, true);
            if (ReferenceEquals(dockEditingTool, source))
                HeaderStatusText.Text = "已打开“" + source.InstanceName + "”的来源配置；检查采集诊断后可重新连接或修改图片路径。";
        }
    }
}
