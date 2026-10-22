using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class CameraSessionS114 : IDisposable
        {
            public HFramegrabber Grabber;
            public string Configuration;
            public string ResourceKey;
            public int Frames;
            public string TriggerMode;
            public string OriginalTriggerMode;
            public string OriginalTriggerSource;

            public void Dispose()
            {
                if (Grabber != null)
                {
                    if (TriggerMode == VmCameraTriggerModeS115.Software)
                    {
                        try { Grabber.SetFramegrabberParam("do_abort_grab", 1); } catch { }
                        try { Grabber.SetFramegrabberParam("TriggerMode", "Off"); } catch { }
                        try { Grabber.SetFramegrabberParam("TriggerSource", OriginalTriggerSource); } catch { }
                        try { Grabber.SetFramegrabberParam("TriggerMode", OriginalTriggerMode); } catch { }
                    }
                    Grabber.Dispose();
                }
                Grabber = null;
            }
        }

        private sealed class ReplayCursorS114
        {
            public string Folder;
            public string[] Files;
            public int Next;
            public int Requests;
            public DateTimeOffset? LastRequestUtc;
        }

        private readonly Dictionary<string, CameraSessionS114> cameraSessionsS114 = new Dictionary<string, CameraSessionS114>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ReplayCursorS114> replayCursorsS114 = new Dictionary<string, ReplayCursorS114>(StringComparer.OrdinalIgnoreCase);
        private bool updatingReplayChoiceUI03;
        private sealed class ReplayChoiceUI03
        {
            public WeakReference Owner { get; set; }
            public int Index { get; set; }
            public string Path { get; set; }
            public string DisplayText { get { return (Index + 1).ToString(CultureInfo.InvariantCulture) + " · " + System.IO.Path.GetFileName(Path); } }
        }

        private void LoadImageSourceDraftS114(VmToolParameterData parameters, VmToolInstance tool)
        {
            DockReplayListExpanderUI03.IsExpanded = false;
            DockImageSourceModeS114.SelectedValue = VmImageSourceModeS114.Normalize(parameters.ImageSourceModeS114);
            DockReplayFolderS114.Text = parameters.ImageReplayFolderS114 ?? string.Empty;
            DockCameraInterfaceS114.Text = parameters.CameraInterfaceS114 ?? string.Empty;
            DockCameraDeviceS114.Text = parameters.CameraDeviceS114 ?? string.Empty;
            DockCameraTimeoutS114.Text = (parameters.CameraTimeoutMsS114 == 0 ? 5000 : parameters.CameraTimeoutMsS114).ToString(CultureInfo.InvariantCulture);
            DockCameraTriggerS115.SelectedValue = VmCameraTriggerModeS115.Normalize(parameters.CameraTriggerModeS115);
            DockReplayMissingEveryS115.Text = parameters.ReplayMissingEveryS115.ToString(CultureInfo.InvariantCulture);
            DockReplayIntervalS115.Text = parameters.ReplayMinimumIntervalMsS115.ToString(CultureInfo.InvariantCulture);
            UpdateImageSourceModeVisibilityS114();
            UpdateCameraStatusS114(tool);
            RefreshAcquisitionDiagnosticS115();
            RefreshImageSourceTaskUI03();
        }

        private bool ReadImageSourceDraftS114(VmToolParameterData parameters)
        {
            parameters.ImageSourceModeS114 = VmImageSourceModeS114.Normalize(DockImageSourceModeS114.SelectedValue as string);
            parameters.ImageReplayFolderS114 = (DockReplayFolderS114.Text ?? string.Empty).Trim();
            parameters.CameraInterfaceS114 = (DockCameraInterfaceS114.Text ?? string.Empty).Trim();
            parameters.CameraDeviceS114 = (DockCameraDeviceS114.Text ?? string.Empty).Trim();
            parameters.CameraTriggerModeS115 = VmCameraTriggerModeS115.Normalize(DockCameraTriggerS115.SelectedValue as string);
            if (parameters.ImageSourceModeS114 == VmImageSourceModeS114.Camera)
            {
                int timeout;
                if (!int.TryParse(DockCameraTimeoutS114.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeout))
                    return FailDockDraft("相机超时须为整数毫秒。");
                parameters.CameraTimeoutMsS114 = timeout;
            }
            if (parameters.ImageSourceModeS114 == VmImageSourceModeS114.Replay)
            {
                int missingEvery;
                int minimumInterval;
                if (!int.TryParse(DockReplayMissingEveryS115.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out missingEvery) ||
                    !int.TryParse(DockReplayIntervalS115.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out minimumInterval))
                    return FailDockDraft("回放故障周期和间隔须为整数；0 表示关闭。");
                parameters.ReplayMissingEveryS115 = missingEvery;
                parameters.ReplayMinimumIntervalMsS115 = minimumInterval;
            }
            return true;
        }

        private void DockImageSourceModeS114_Changed(object sender, RoutedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改图像采集模式")) return;
            UpdateImageSourceModeVisibilityS114();
            DockDraftValue_Changed(sender, e);
            RefreshImageSourceTaskUI03();
        }

        private void UpdateImageSourceModeVisibilityS114()
        {
            if (DockImageSourceFileS114 == null) return;
            // SelectionChanged precedes SelectedValue commit on this control.
            var selected = DockImageSourceModeS114.SelectedItem as VmToolValueOption;
            string mode = VmImageSourceModeS114.Normalize(selected == null ? DockImageSourceModeS114.SelectedValue as string : selected.Key);
            DockImageSourceFileS114.Visibility = mode == VmImageSourceModeS114.File ? Visibility.Visible : Visibility.Collapsed;
            DockImageSourceReplayS114.Visibility = mode == VmImageSourceModeS114.Replay ? Visibility.Visible : Visibility.Collapsed;
            DockImageSourceCameraS114.Visibility = mode == VmImageSourceModeS114.Camera ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DockSourcePathChangedUI03(object sender, TextChangedEventArgs e)
        {
            DockDraftValue_Changed(sender, e);
            RefreshImageSourceTaskUI03();
        }

        private void SelectSourceFileDraftUI03(string path, long lease)
        {
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.ImageSource || !IsEngineeringEditLeaseCurrentS137(lease)) return;
            DockLocalImagePathTextBox.Text = path;
            // Blank SN stays automatic; do not turn the first filename into a
            // permanent identifier for all later chosen files.
            DockFooterText.Text = "已选择文件，尚未读取；F5读取草稿，应用后保存配置。";
        }

        private static string SourcePathBriefUI03(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "尚未选择";
            try
            {
                string name = Path.GetFileName(value.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return string.IsNullOrWhiteSpace(name) ? value : name;
            }
            catch { return "路径格式需要修正"; }
        }

        private void RefreshImageSourceTaskUI03()
        {
            if (DockSourceCurrentFrameUI03 == null) return;
            UpdateImageSourceModeVisibilityS114();
            DockSourceFileBriefUI03.Text = SourcePathBriefUI03(DockLocalImagePathTextBox.Text);
            DockSourceFileBriefUI03.ToolTip = DockLocalImagePathTextBox.Text;
            DockReplayFolderBriefUI03.Text = SourcePathBriefUI03(DockReplayFolderS114.Text);
            DockReplayFolderBriefUI03.ToolTip = DockReplayFolderS114.Text;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.ImageSource) return;
            if (!subflowTrialActiveS102)
            {
                DockTrialRunButton.Content = "▶ 读取一帧";
                DockTrialRunButton.ToolTip = "F5：按当前来源草稿读取一帧，不应用配置；回放使用下一次位置，相机须已明确连接。";
            }
            object raw;
            VmImageSnapshot snapshot = tool.TryGetOutputValue("Image", out raw) ? raw as VmImageSnapshot : null;
            if (snapshot == null || snapshot.IsDisposed || snapshot.ProducerToolId != tool.ToolId || tool.ResultCode != "OK")
            {
                DockSourceCurrentFrameUI03.Text = "当前输出：无有效帧";
                DockSourceCurrentFrameUI03.ToolTip = string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "请明确读取一帧；选择路径、模式或打开方案不会自动取图。" : tool.ErrorMessage;
            }
            else
            {
                double index, count;
                bool indexed = tool.TryGetNumericOutput("FrameIndex", out index) && index >= 1 && Math.Truncate(index) == index;
                bool counted = tool.TryGetNumericOutput("FrameCount", out count) && count >= 0 && Math.Truncate(count) == count;
                string origin = tool.RuntimeResultOrigin == VmRuntimeResultOrigin.DraftTrial ? "草稿帧" : tool.RuntimeResultOrigin == VmRuntimeResultOrigin.ModuleExecution ? "已运行帧" : "未标记来源帧";
                object sourceMode;
                string mode = tool.TryGetOutputValue("SourceMode", out sourceMode) ? sourceMode as string : null;
                string modeName = mode == VmImageSourceModeS114.File ? "文件" : mode == VmImageSourceModeS114.Replay ? "回放" : mode == VmImageSourceModeS114.Camera ? "相机" : "方式未发布";
                DockSourceCurrentFrameUI03.Text = origin + "[" + modeName + "]：" + SourcePathBriefUI03(snapshot.SourcePath) +
                    (indexed ? " · " + index.ToString("0", CultureInfo.InvariantCulture) + (counted && count > 0 ? "/" + count.ToString("0", CultureInfo.InvariantCulture) : string.Empty) : " · 帧号未发布") +
                    " · " + snapshot.Width + "×" + snapshot.Height +
                    (dockDraftDirty && tool.RuntimeResultOrigin != VmRuntimeResultOrigin.DraftTrial ? " · 草稿未重新读取" : string.Empty);
                object timestamp;
                string time = tool.TryGetOutputValue("Timestamp", out timestamp) && timestamp is string ? (string)timestamp : "未发布";
                DockSourceCurrentFrameUI03.ToolTip = "实际输出：" + snapshot.SourcePath + "\nSN：" + snapshot.SerialNumber + "\n帧时间：" + time + "\n" + origin + "；只显示最后真实输出，不把所选路径当成已经读取。";
            }
            if (DockReplayListExpanderUI03.IsExpanded) RefreshReplayChoicesUI03(false);
        }

        private ReplayCursorS114 GetReplayCursorS114(VmToolInstance tool, VmToolParameterData parameters)
        {
            string folder = Path.GetFullPath(parameters.ImageReplayFolderS114);
            string[] files = Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                .Where(IsReplayImageS114).Take(10001).ToArray();
            if (files.Length == 0 || files.Length > 10000)
                throw new InvalidOperationException("回放目录需要 1–10000 张受支持的图片。当前：" + files.Length.ToString(CultureInfo.InvariantCulture));
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            ReplayCursorS114 cursor;
            if (!replayCursorsS114.TryGetValue(tool.ToolId, out cursor) || !PathsEqual(cursor.Folder, folder) || !cursor.Files.SequenceEqual(files, StringComparer.OrdinalIgnoreCase))
            {
                cursor = new ReplayCursorS114 { Folder = folder, Files = files, Next = 0 };
                replayCursorsS114[tool.ToolId] = cursor;
            }
            return cursor;
        }

        private bool ReplayChoiceMatchesAppliedUI03(VmToolInstance tool)
        {
            var selected = DockImageSourceModeS114.SelectedItem as VmToolValueOption;
            try { return tool != null && tool.Kind == VmToolKind.ImageSource && tool.Parameters != null &&
                VmImageSourceModeS114.Normalize(tool.Parameters.ImageSourceModeS114) == VmImageSourceModeS114.Replay &&
                VmImageSourceModeS114.Normalize(selected == null ? DockImageSourceModeS114.SelectedValue as string : selected.Key) == VmImageSourceModeS114.Replay &&
                PathsEqual(tool.Parameters.ImageReplayFolderS114, DockReplayFolderS114.Text); }
            catch { return false; }
        }

        private void DockReplayListExpandedUI03(object sender, RoutedEventArgs e) { RefreshReplayChoicesUI03(true); }

        private void RefreshReplayChoicesUI03(bool readDirectory)
        {
            if (updatingReplayChoiceUI03 || DockReplayNextChoiceUI03 == null) return;
            updatingReplayChoiceUI03 = true;
            try
            {
                VmToolInstance tool = dockEditingTool;
                DockReplayNextChoiceUI03.IsEnabled = false;
                if (!ReplayChoiceMatchesAppliedUI03(tool))
                {
                    DockReplayNextChoiceUI03.ItemsSource = null;
                    DockReplayListStatusUI03.Text = "目录/模式草稿请先应用；F5仍可按当前草稿取图。";
                    return;
                }
                ReplayCursorS114 cursor;
                if (readDirectory)
                {
                    long lease = CaptureEngineeringEditLeaseS137("查看回放列表");
                    if (lease < 0 || !CanChangeRecipeS134()) return;
                    cursor = GetReplayCursorS114(tool, tool.Parameters);
                    if (!IsEngineeringEditLeaseCurrentS137(lease) || dockEditingTool != tool || !ReplayChoiceMatchesAppliedUI03(tool)) return;
                }
                else if (!replayCursorsS114.TryGetValue(tool.ToolId, out cursor))
                { DockReplayListStatusUI03.Text = "尚无回放列表；重新展开查看，不会读取图像。"; return; }
                if (!PathsEqual(cursor.Folder, tool.Parameters.ImageReplayFolderS114))
                { DockReplayNextChoiceUI03.ItemsSource = null; DockReplayListStatusUI03.Text = "列表属于旧目录，请重新展开。"; return; }
                DockReplayNextChoiceUI03.ItemsSource = cursor.Files.Select((path, index) => new ReplayChoiceUI03 { Owner = new WeakReference(tool), Index = index, Path = path }).ToList();
                DockReplayNextChoiceUI03.SelectedIndex = cursor.Next;
                DockReplayNextChoiceUI03.IsEnabled = CanChangeRecipeS134(false);
                DockReplayListStatusUI03.Text = cursor.Files.Length + " 张 · 下一次读取 " + (cursor.Next + 1) + "；选择不取图，到末尾回首张。";
            }
            catch (Exception ex) { DockReplayNextChoiceUI03.ItemsSource = null; DockReplayListStatusUI03.Text = "列表不可用：" + ex.Message; }
            finally { updatingReplayChoiceUI03 = false; }
        }

        private void DockReplayNextChoiceChangedUI03(object sender, SelectionChangedEventArgs e)
        {
            if (updatingReplayChoiceUI03 || dockDraftUpdating) return;
            VmToolInstance tool = dockEditingTool;
            var choice = DockReplayNextChoiceUI03.SelectedItem as ReplayChoiceUI03;
            long lease = CaptureEngineeringEditLeaseS137("选择下一回放帧");
            ReplayCursorS114 cursor;
            if (lease < 0 || choice == null || choice.Owner == null || !ReferenceEquals(choice.Owner.Target, tool) || !CanChangeRecipeS134() || !ReplayChoiceMatchesAppliedUI03(tool) ||
                !replayCursorsS114.TryGetValue(tool.ToolId, out cursor) || !PathsEqual(cursor.Folder, tool.Parameters.ImageReplayFolderS114) ||
                choice.Index < 0 || choice.Index >= cursor.Files.Length || cursor.Files[choice.Index] != choice.Path || !IsEngineeringEditLeaseCurrentS137(lease))
            { RefreshReplayChoicesUI03(false); return; }
            cursor.Next = choice.Index;
            RefreshReplayChoicesUI03(false);
        }

        private void DockBrowseReplayFolderS114_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("选择图片回放目录");
            if (editLeaseS137 < 0) return;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择按文件名逐帧回放的图片目录";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                    DockReplayFolderS114.Text = dialog.SelectedPath;
            }
        }

        private static string CameraConfigurationS114(VmToolParameterData p)
        {
            return (p.CameraInterfaceS114 ?? string.Empty).Trim() + "|" + (p.CameraDeviceS114 ?? string.Empty).Trim() + "|" +
                p.CameraTimeoutMsS114.ToString(CultureInfo.InvariantCulture) + "|" + VmCameraTriggerModeS115.Normalize(p.CameraTriggerModeS115);
        }

        private void DockConnectCameraS114_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("相机手动连接测试");
            if (editLeaseS137 < 0) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.ImageSource) return;
            if (isFlowExecutionActive || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
            {
                DockCameraStatusS114.Text = "运行中，暂不能连接";
                return;
            }
            if (dockDraftDirty && !ApplyDockConfigurationDraft(true)) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            VmToolParameterData p = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string error = p.Validate(VmToolKind.ImageSource);
            if (VmImageSourceModeS114.Normalize(p.ImageSourceModeS114) != VmImageSourceModeS114.Camera || !string.IsNullOrWhiteSpace(error))
            {
                DockCameraStatusS114.Text = string.IsNullOrWhiteSpace(error) ? "请先选择相机模式" : error;
                return;
            }
            ReleaseImageSourceRuntimeS114(tool.ToolId);
            HFramegrabber grabber = null;
            CameraSessionS114 session = null;
            try
            {
                string resourceKey = CameraResourceKeyS116(p);
                var existingOwner = cameraSessionsS114.FirstOrDefault(item => item.Key != tool.ToolId &&
                    string.Equals(item.Value.ResourceKey, resourceKey, StringComparison.OrdinalIgnoreCase));
                if (existingOwner.Value != null)
                {
                    VmToolInstance owner = flowTools.FirstOrDefault(item => item.ToolId == existingOwner.Key);
                    throw new InvalidOperationException("此设备已由“" + (owner == null ? existingOwner.Key : owner.InstanceName) +
                        "”连接；先在所属模块断开，不能重复占用。");
                }
                grabber = new HFramegrabber(p.CameraInterfaceS114.Trim(), 0, 0, 0, 0, 0, 0,
                    "default", -1, "default", -1, "default",
                    string.IsNullOrWhiteSpace(p.CameraDeviceS114) ? "default" : p.CameraDeviceS114.Trim(), "default", -1, -1);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                { grabber.Dispose(); grabber = null; DockCameraStatusS114.Text = "工程授权已改变，手动连接未保留"; return; }
                grabber.SetFramegrabberParam("grab_timeout", p.CameraTimeoutMsS114);
                session = new CameraSessionS114
                {
                    Grabber = grabber,
                    Configuration = CameraConfigurationS114(p),
                    ResourceKey = resourceKey,
                    TriggerMode = VmCameraTriggerModeS115.Normalize(p.CameraTriggerModeS115)
                };
                if (session.TriggerMode == VmCameraTriggerModeS115.Software) ConfigureSoftwareTriggerS115(session);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                { session.Dispose(); session = null; grabber = null; DockCameraStatusS114.Text = "工程授权已改变，手动连接已释放"; return; }
                cameraSessionsS114[tool.ToolId] = session;
                acquisitionDiagnosticsS115.Remove(tool.ToolId);
                RefreshAcquisitionDiagnosticS115();
                grabber = null;
                DockCameraStatusS114.Text = session.TriggerMode == VmCameraTriggerModeS115.Software ? "已连接 · 软件触发就绪" : "已连接 · 自由采集就绪";
                LogInfo(tool.InstanceName + " HALCON 相机已显式连接。");
                RefreshUiState();
            }
            catch (Exception ex)
            {
                cameraSessionsS114.Remove(tool.ToolId);
                if (session != null) session.Dispose();
                else if (grabber != null) grabber.Dispose();
                DockCameraStatusS114.Text = "连接失败：" + ex.Message;
                RecordCameraConnectionFailureS115(tool, ex);
                PublishProductionAlarmS136(VmProductionAlarmSourceS136.CameraConnectionFailure, VmProductionAlarmSeverityS136.Error,
                    tool.InstanceName + " · 相机连接失败", ex.Message + "；不会自动重新连接或采集。", tool);
                AppendAlarm(tool.InstanceName + " 相机连接失败：" + ex.Message);
            }
        }

        private void DockDisconnectCameraS114_Click(object sender, RoutedEventArgs e)
        {
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.ImageSource) return;
            ReleaseImageSourceRuntimeS114(dockEditingTool.ToolId);
            DockCameraStatusS114.Text = "已断开";
            LogInfo(dockEditingTool.InstanceName + " HALCON 相机已断开。");
            RefreshUiState();
        }

        private void DockGrabCameraS114_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("相机手动单帧采集")) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.ImageSource) return;
            if (dockDraftDirty)
            {
                DockCameraStatusS114.Text = "先应用配置，再连接并采一帧";
                return;
            }
            RunStandaloneTool(tool, "相机单帧采集");
            UpdateCameraStatusS114(tool);
        }

        private void UpdateCameraStatusS114(VmToolInstance tool)
        {
            if (DockCameraStatusS114 == null) return;
            CameraSessionS114 session;
            DockCameraStatusS114.Text = tool != null && cameraSessionsS114.TryGetValue(tool.ToolId, out session) &&
                session.Configuration == CameraConfigurationS114(tool.Parameters ?? new VmToolParameterData())
                ? "已连接 · 已采 " + session.Frames.ToString(CultureInfo.InvariantCulture) + " 帧"
                : "未连接；方案打开不会自动连接";
        }

        private void ReleaseImageSourceRuntimeS114(string toolId)
        {
            CameraSessionS114 session;
            if (cameraSessionsS114.TryGetValue(toolId, out session))
            {
                cameraSessionsS114.Remove(toolId);
                session.Dispose();
            }
            replayCursorsS114.Remove(toolId);
        }

        private void DisposeImageSourceRuntimeS114()
        {
            foreach (CameraSessionS114 session in cameraSessionsS114.Values) session.Dispose();
            cameraSessionsS114.Clear();
            replayCursorsS114.Clear();
            acquisitionDiagnosticsS115.Clear();
        }

        private void PruneImageSourceRuntimeS114()
        {
            foreach (string id in cameraSessionsS114.Keys.Concat(replayCursorsS114.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
            {
                VmToolInstance tool = flowTools.FirstOrDefault(item => item.ToolId == id && item.Kind == VmToolKind.ImageSource);
                CameraSessionS114 session;
                if (tool == null || cameraSessionsS114.TryGetValue(id, out session) &&
                    (VmImageSourceModeS114.Normalize(tool.Parameters == null ? null : tool.Parameters.ImageSourceModeS114) != VmImageSourceModeS114.Camera ||
                     session.Configuration != CameraConfigurationS114(tool.Parameters ?? new VmToolParameterData())))
                    ReleaseImageSourceRuntimeS114(id);
            }
            foreach (string id in acquisitionDiagnosticsS115.Keys.ToArray())
                if (!flowTools.Any(item => item.Kind == VmToolKind.ImageSource && item.ToolId == id))
                    acquisitionDiagnosticsS115.Remove(id);
        }

        private string BuildImageSourceConnectionStatusS114(VmToolInstance tool, VmToolParameterData parameters)
        {
            string mode = VmImageSourceModeS114.Normalize(parameters.ImageSourceModeS114);
            if (mode == VmImageSourceModeS114.Camera)
            {
                CameraSessionS114 session;
                return cameraSessionsS114.TryGetValue(tool.ToolId, out session) &&
                    session.Configuration == CameraConfigurationS114(parameters)
                    ? "相机已连接 · " + session.Frames.ToString(CultureInfo.InvariantCulture) + " 帧"
                    : "相机未连接 · 打开参数显式连接";
            }
            return tool.TryGetOutputValue("Image", out _)
                ? mode == VmImageSourceModeS114.Replay ? "回放 · 有值" : "本地文件 · 有值"
                : mode == VmImageSourceModeS114.Replay ? "回放 · 等待运行" : "本地文件 · 等待运行";
        }

        private static string BuildImageSourceConnectionSummaryS114(VmToolParameterData parameters)
        {
            string mode = VmImageSourceModeS114.Normalize(parameters.ImageSourceModeS114);
            string label = mode == VmImageSourceModeS114.Camera ? parameters.CameraInterfaceS114 + "/" +
                (string.IsNullOrWhiteSpace(parameters.CameraDeviceS114) ? "default" : parameters.CameraDeviceS114)
                : mode == VmImageSourceModeS114.Replay ? parameters.ImageReplayFolderS114 : parameters.LocalImagePath;
            return (string.IsNullOrWhiteSpace(label) ? "待配置" : label) + " → Image / SN / 时间";
        }

        private static string BuildImageSourceInspectorTextS114(VmToolInstance tool, VmToolParameterData parameters)
        {
            object path;
            if (tool != null && tool.TryGetOutputValue("Path", out path) && path != null)
                return "本帧来源：" + path;
            return "配置来源：" + BuildImageSourceConnectionSummaryS114(parameters);
        }

        private static string BuildImageSourceInspectorSerialS114(VmToolInstance tool, VmToolParameterData parameters)
        {
            object serial;
            object timestamp;
            if (tool != null && tool.TryGetOutputValue("SN", out serial))
                return "SN：" + serial + (tool.TryGetOutputValue("Timestamp", out timestamp) ? " · UTC " + timestamp : string.Empty);
            return "SN：" + (string.IsNullOrWhiteSpace(parameters.LocalImageSerialNumber) ? "运行后显示" : parameters.LocalImageSerialNumber);
        }

        private string BuildImageSourceInputSummaryS114(VmToolInstance tool)
        {
            object path;
            if (tool.TryGetOutputValue("Path", out path) && path != null)
                return "来源：" + path;
            return "图像来源等待采集";
        }

        private InspectionRecord RunImageSourceS114(VmToolInstance tool)
        {
            Stopwatch acquisitionClock = Stopwatch.StartNew();
            VmToolParameterData p = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string pairedPath;
            if (pairedRuntimePathsS113.TryGetValue(tool.ToolId, out pairedPath))
            {
                p.ImageSourceModeS114 = VmImageSourceModeS114.File;
                p.LocalImagePath = pairedPath;
                p.LocalImageSerialNumber = Path.GetFileNameWithoutExtension(pairedPath);
            }
            string error = p.Validate(VmToolKind.ImageSource);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            string mode = VmImageSourceModeS114.Normalize(p.ImageSourceModeS114);
            string path;
            string serial;
            int frameIndex = 1;
            int frameCount = 1;
            HImage image;
            if (mode == VmImageSourceModeS114.Camera)
            {
                CameraSessionS114 session;
                if (!cameraSessionsS114.TryGetValue(tool.ToolId, out session) ||
                    session.Configuration != CameraConfigurationS114(p))
                    throw new InvalidOperationException("相机未连接或配置已改变；请在图像源参数中显式连接。不会回退到旧图片。");
                try
                {
                    if (session.TriggerMode == VmCameraTriggerModeS115.Software)
                    {
                        session.Grabber.SetFramegrabberParam("TriggerSoftware", 1);
                        image = session.Grabber.GrabImageAsync(-1);
                    }
                    else image = session.Grabber.GrabImage();
                    session.Frames++;
                }
                catch (Exception ex)
                {
                    ReleaseImageSourceRuntimeS114(tool.ToolId);
                    throw new InvalidOperationException("HALCON 相机取图失败，连接已释放：" + ex.Message, ex);
                }
                path = "camera://" + p.CameraInterfaceS114.Trim() + "/" +
                    (string.IsNullOrWhiteSpace(p.CameraDeviceS114) ? "default" : p.CameraDeviceS114.Trim());
                serial = tool.ToolId + "-" + session.Frames.ToString(CultureInfo.InvariantCulture);
                frameIndex = session.Frames;
                frameCount = 0;
            }
            else
            {
                if (mode == VmImageSourceModeS114.Replay)
                {
                    ReplayCursorS114 cursor = GetReplayCursorS114(tool, p);
                    string[] files = cursor.Files;
                    DateTimeOffset requestTime = DateTimeOffset.UtcNow;
                    cursor.Requests++;
                    if (p.ReplayMinimumIntervalMsS115 > 0 && cursor.LastRequestUtc.HasValue &&
                        (requestTime - cursor.LastRequestUtc.Value).TotalMilliseconds < p.ReplayMinimumIntervalMsS115)
                    {
                        double interval = (requestTime - cursor.LastRequestUtc.Value).TotalMilliseconds;
                        cursor.LastRequestUtc = requestTime;
                        throw new InvalidOperationException("回放节拍模拟：请求间隔 " + interval.ToString("0", CultureInfo.InvariantCulture) +
                            " ms，小于配置的 " + p.ReplayMinimumIntervalMsS115.ToString(CultureInfo.InvariantCulture) + " ms；本次不读图。");
                    }
                    cursor.LastRequestUtc = requestTime;
                    if (p.ReplayMissingEveryS115 >= 2 && cursor.Requests % p.ReplayMissingEveryS115 == 0)
                        throw new InvalidOperationException("回放缺图模拟：第 " + cursor.Requests.ToString(CultureInfo.InvariantCulture) +
                            " 次请求按配置产生缺图；本次不发布旧图。");
                    frameIndex = cursor.Next + 1;
                    frameCount = files.Length;
                    path = files[cursor.Next];
                }
                else path = Path.GetFullPath(p.LocalImagePath);
                image = imageService.ReadImage(path);
                serial = mode == VmImageSourceModeS114.Replay || string.IsNullOrWhiteSpace(p.LocalImageSerialNumber)
                    ? Path.GetFileNameWithoutExtension(path) : p.LocalImageSerialNumber.Trim();
            }
            acquisitionClock.Stop();

            using (image)
            {
                int width = GetImageWidth(image);
                int height = GetImageHeight(image);
                string timestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Image", VmImageSnapshot.Create(image, tool.ToolId, tool.InstanceName, "Image", path, serial));
                tool.SetOutputValue("SN", serial);
                tool.SetOutputValue("Path", path);
                tool.SetOutputValue("Width", (double)width);
                tool.SetOutputValue("Height", (double)height);
                tool.SetOutputValue("Timestamp", timestamp);
                tool.SetOutputValue("SourceMode", mode);
                tool.SetOutputValue("FrameIndex", (double)frameIndex);
                tool.SetOutputValue("FrameCount", (double)frameCount);
                tool.SetOutputValue("AcquisitionMs", acquisitionClock.Elapsed.TotalMilliseconds);
                tool.SetOutputValue("TriggerMode", mode == VmImageSourceModeS114.Camera
                    ? VmCameraTriggerModeS115.Normalize(p.CameraTriggerModeS115) : "OfflineRequest");
                string message = width.ToString(CultureInfo.InvariantCulture) + "×" + height.ToString(CultureInfo.InvariantCulture) +
                    " Image · " + (mode == VmImageSourceModeS114.Camera ? "相机" : mode == VmImageSourceModeS114.Replay ? "回放" : "文件") +
                    " " + frameIndex.ToString(CultureInfo.InvariantCulture) + (frameCount > 0 ? "/" + frameCount.ToString(CultureInfo.InvariantCulture) : string.Empty) +
                    " · SN=" + serial;
                InspectionRecord record = CreateRecord("ImageSource", "OK", 1, message, tool);
                ApplyRecordImage(record, tool, image);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message + "；" + path);
                if (mode == VmImageSourceModeS114.Replay)
                    replayCursorsS114[tool.ToolId].Next = frameIndex % frameCount;
                return record;
            }
        }

        private static bool IsReplayImageS114(string path)
        {
            string extension = Path.GetExtension(path);
            return new[] { ".bmp", ".png", ".jpg", ".jpeg", ".tif", ".tiff" }
                .Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private void LoadImageReplayExampleS114_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("载入图片回放示例")) return;
            LoadExampleProgramS78("image-replay-acquisition");
        }

        private bool BuildImageReplayExampleS114()
        {
            VmToolInstance source = CreateFlowTool(VmToolKind.ImageSource, "01 目录逐帧采集", true, null);
            VmToolInstance channel = CreateFlowTool(VmToolKind.ImageChannel, "02 转换灰度", true, null);
            VmToolInstance histogram = CreateFlowTool(VmToolKind.GrayHistogram, "03 亮度分布", true, null);
            foreach (VmToolInstance tool in new[] { source, channel, histogram }) flowTools.Add(tool);
            channel.SetInputBinding("Image", source.ToolId, "Image");
            histogram.SetInputBinding("Image", channel.ToolId, "Image");
            source.Parameters.ImageSourceModeS114 = VmImageSourceModeS114.Replay;
            source.Parameters.ImageReplayFolderS114 = Path.GetDirectoryName(GetHalconExampleImageS78("particle.png"));
            return File.Exists(GetHalconExampleImageS78("particle.png"));
        }
    }
}
