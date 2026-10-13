using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using Forms = System.Windows.Forms;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class SubflowTraceRowS102
        {
            public string RuntimeToolId { get; set; }
            public List<string> DefinitionPath { get; set; }
            public List<SubflowTracePortS102> Ports { get; set; } = new List<SubflowTracePortS102>();
            public string PortNotice { get; set; }
            public string Path { get; set; }
            public string State { get; set; }
            public string Elapsed { get; set; }
            public string Details { get; set; }
            public string ImageLabel { get; set; }
            public VmImageSnapshot Image { get; set; }
            public long ImageBytes { get; set; }
            public string ImageCoordinate { get; set; }
            public List<SubflowTraceOverlayS102> Overlays { get; set; } = new List<SubflowTraceOverlayS102>();
            public string OverlayNotice { get; set; }
        }

        private sealed class SubflowTraceOverlayS102
        {
            public HObject Shape;
            public string Label;
            public string Color;
            public long EstimatedBytes;
        }

        private sealed class SubflowTracePortS102
        {
            public string Direction { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string State { get; set; }
            public string Value { get; set; }
            public string Detail { get; set; }
        }

        private sealed class SubflowTraceFrameS102
        {
            public VmToolInstance Call;
            public SubflowTraceRowS102 Row;
            public string Next;
            public string Current;
            public string Iteration;
        }

        private readonly ObservableCollection<SubflowTraceRowS102> subflowTraceRowsS102 = new ObservableCollection<SubflowTraceRowS102>();
        private readonly Stack<SubflowTraceFrameS102> subflowTraceFramesS102 = new Stack<SubflowTraceFrameS102>();
        private Window subflowTraceImageWindowS102;
        private readonly Dictionary<string, List<string>> subflowDefinitionPathsS102 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private void ClearSubflowTraceS102()
        {
            if (subflowTraceFramesS102.Count > 0) return;
            if (subflowTraceImageWindowS102 != null) subflowTraceImageWindowS102.Close();
            foreach (var row in subflowTraceRowsS102) DisposeSubflowTraceVisualsS102(row);
            subflowTraceRowsS102.Clear(); RefreshSubflowTraceUiS102();
        }

        private List<string> GetSubflowDefinitionPathS102(VmToolInstance tool)
        {
            List<string> path;
            return subflowDefinitionPathsS102.TryGetValue(tool.ToolId, out path) ? path.ToList() :
                subflowEditorsS102.Reverse().Select(frame => frame.Call.ToolId).Concat(new[] { tool.ToolId }).ToList();
        }

        private SubflowTraceRowS102 AddSubflowTraceRowS102(VmToolInstance tool, bool pending = false)
        {
            var waiting = subflowTraceRowsS102.LastOrDefault(item => item.RuntimeToolId == tool.ToolId && (item.State == "待执行" || item.State == "已暂停"));
            if (waiting != null)
            {
                if (!pending) { waiting.State = "运行中"; RefreshSubflowTraceUiS102(); }
                return waiting;
            }
            string prefix = string.Join(" / ", subflowTraceFramesS102.Reverse().Select(frame => frame.Call.InstanceName + (string.IsNullOrEmpty(frame.Iteration) ? "" : " [" + frame.Iteration + "]")));
            var row = new SubflowTraceRowS102 { RuntimeToolId = tool.ToolId, DefinitionPath = GetSubflowDefinitionPathS102(tool),
                Path = string.IsNullOrEmpty(prefix) ? tool.InstanceName : prefix + " / " + tool.InstanceName,
                State = pending ? "待执行" : "运行中", Elapsed = "--", Details = "等待本次执行结果。" };
            CaptureSubflowTracePortsS102(row, tool, true);
            subflowTraceRowsS102.Add(row);
            while (subflowTraceRowsS102.Count > 256)
            {
                var oldest = subflowTraceRowsS102[0]; DisposeSubflowTraceVisualsS102(oldest);
                subflowTraceRowsS102.RemoveAt(0);
            }
            RefreshSubflowTraceUiS102(); return row;
        }

        private SubflowTraceFrameS102 BeginSubflowCallTraceS102(VmToolInstance tool)
        {
            var frame = new SubflowTraceFrameS102 { Call = tool, Row = AddSubflowTraceRowS102(tool), Current = "准备调用", Next = "准备输入" };
            subflowTraceFramesS102.Push(frame); RefreshSubflowTraceUiS102(); return frame;
        }

        private InspectionRecord ExecuteSubflowLeafWithTraceS102(VmToolInstance tool, string source)
        {
            if (subflowTraceFramesS102.Count == 0) return ExecuteFlowTool(tool, source);
            var row = AddSubflowTraceRowS102(tool);
            try { return ExecuteFlowTool(tool, source); }
            finally { CompleteSubflowTraceRowS102(row, tool); }
        }

        private void CompleteSubflowTraceRowS102(SubflowTraceRowS102 row, VmToolInstance tool)
        {
            if (!subflowTraceRowsS102.Contains(row)) return;
            row.State = tool.ResultCode;
            row.Elapsed = tool.ElapsedMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms";
            Func<string, string> shortText = value => string.IsNullOrEmpty(value) ? "--" : value.Length <= 1500 ? value : value.Substring(0, 1500) + "…";
            row.Details = "输入：" + shortText(tool.InputSummary) + "\n结果：" + shortText(tool.OutputSummary) +
                (string.IsNullOrWhiteSpace(tool.ErrorMessage) ? string.Empty : "\n错误：" + shortText(tool.ErrorMessage));
            CaptureSubflowTracePortsS102(row, tool, false);
            try
            {
                if (tool.ResultCode != "SKIP")
                {
                    CaptureSubflowTraceImageS102(row, tool);
                    CaptureSubflowTraceOverlaysS102(row, tool);
                }
            }
            catch (Exception ex)
            {
                // Debug capture failure must never change the algorithm outcome or fail the call.
                row.OverlayNotice = "部分图像/叠加未保留：" + shortText(ex.Message);
            }
            RefreshSubflowTraceUiS102();
        }

        private static string LimitSubflowTraceTextS102(string text)
        {
            return string.IsNullOrEmpty(text) ? "--" : text.Length <= 512 ? text : text.Substring(0, 512) + "…（摘要截断）";
        }

        private void CaptureSubflowTracePortsS102(SubflowTraceRowS102 row, VmToolInstance tool, bool inputs)
        {
            try
            {
                var ports = (inputs ? ToolMetadata.GetInstanceInputPorts(tool) : ToolMetadata.GetInstanceOutputPorts(tool)).ToList();
                if (ports.Count > 64) row.PortNotice = "每个方向最多保留 64 个端口，每项文本最多 512 字；其余未保留。";
                foreach (var port in ports.Take(64))
                {
                    var binding = inputs ? tool.GetInputBinding(port.PortName) : null;
                    var producer = inputs ? binding == null ? null : GetInputSourceTool(binding) : tool;
                    string sourcePort = inputs ? binding == null ? null : binding.SourcePortName : port.PortName;
                    object raw = null;
                    bool available = producer != null && sourcePort != null &&
                        (producer.ResultCode == "OK" || producer.ResultCode == "NG") && string.IsNullOrWhiteSpace(producer.ErrorMessage) &&
                        producer.TryGetOutputValue(sourcePort, out raw) && raw != null && !IsDisposedRuntimeValueS69(raw);
                    string state = available ? inputs ? "开始时可用" : "本次输出" : inputs ? binding == null ? "默认/未绑定" : "来源无有效值" : "未发布";
                    string value = available ? LimitSubflowTraceTextS102(producer.GetFormattedOutput(sourcePort)) : "--";
                    string context = available && (port.DataType == "Image" || port.DataType == "Region" || port.DataType == "XLD" || VmFlowValueContractS103.IsGeometryType(port.DataType))
                        ? ResolveOutputCoordinateContextS102(producer, sourcePort) : null;
                    row.Ports.Add(new SubflowTracePortS102
                    {
                        Direction = inputs ? "输入" : "输出", Name = port.DisplayName, Type = port.DataType, State = state, Value = value,
                        Detail = LimitSubflowTraceTextS102("端口：" + port.PortName + "\n来源：" +
                            (producer == null ? binding == null ? "工具默认/未显式订阅，不推测实际值" : binding.SourceToolId + "." + binding.SourcePortName : producer.InstanceName + "." + sourcePort) +
                            (raw == null ? string.Empty : "\n实际类型：" + raw.GetType().Name) +
                            (context == null ? string.Empty : "\n坐标来源：" + context))
                    });
                }
            }
            catch (Exception ex) { row.PortNotice = "部分端口未能记录：" + LimitSubflowTraceTextS102(ex.Message); }
        }

        private void CaptureSubflowTraceImageS102(SubflowTraceRowS102 row, VmToolInstance tool)
        {
            VmImageSnapshot source = null;
            foreach (var port in ToolMetadata.GetInstanceOutputPorts(tool).Where(port => port.DataType == "Image"))
            {
                source = GetImageSnapshot(tool, port.PortName);
                if (source != null && !source.IsDisposed) { row.ImageLabel = "本次输出 · " + port.DisplayName; break; }
            }
            if (source == null || source.IsDisposed)
            {
                string sourceName;
                source = FindUpstreamImageSnapshot(tool, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out sourceName);
                if (source == null || source.IsDisposed) source = FindGeometryReferenceImageS103(tool, out sourceName);
                row.ImageLabel = "输入/关联参考底图 · " + (sourceName ?? "无图像来源") + "（本次图形单独列出）";
            }
            if (source == null || source.IsDisposed) { row.ImageLabel = "本次没有可保留的图像快照。"; return; }
            long estimate = (long)source.Width * source.Height * Math.Max(1, source.ChannelCount) * 8;
            const long budget = 64L * 1024 * 1024;
            if (estimate > budget) { row.ImageLabel += " · 超出调试图像预算，未复制"; return; }
            foreach (var old in subflowTraceRowsS102.Where(item => item.Image != null).ToList())
            {
                if (subflowTraceRowsS102.Count(item => item.Image != null) < 8 && subflowTraceRowsS102.Sum(item => item.ImageBytes) + estimate <= budget) break;
                DisposeSubflowTraceVisualsS102(old); old.ImageLabel = "图像已从有界调试缓存释放，步骤结果保留。";
            }
            var imageProducer = ResolveRuntimeSourceS102(source.ProducerToolId);
            row.ImageCoordinate = imageProducer == null ? null : ResolveOutputCoordinateContextS102(imageProducer, source.ProducerPortName);
            row.Image = (VmImageSnapshot)CopySubflowValueS102(source, tool, "TraceImage"); row.ImageBytes = estimate;
        }

        private static void DisposeSubflowTraceVisualsS102(SubflowTraceRowS102 row)
        {
            if (row.Image != null) row.Image.Dispose();
            row.Image = null; row.ImageBytes = 0;
            foreach (var overlay in row.Overlays) overlay.Shape.Dispose();
            row.Overlays.Clear();
        }

        private void CaptureSubflowTraceOverlaysS102(SubflowTraceRowS102 row, VmToolInstance tool)
        {
            if (row.Image == null || (tool.ResultCode != "OK" && tool.ResultCode != "NG") || !string.IsNullOrWhiteSpace(tool.ErrorMessage)) return;
            int omitted = 0;
            foreach (var port in ToolMetadata.GetInstanceOutputPorts(tool).Where(port => port.DataType == "Region" || port.DataType == "XLD"))
            {
                object raw;
                if (!tool.TryGetOutputValue(port.PortName, out raw) || raw == null || IsDisposedRuntimeValueS69(raw)) continue;
                string context = ResolveOutputCoordinateContextS102(tool, port.PortName);
                if (string.IsNullOrWhiteSpace(row.ImageCoordinate) || IsPendingCoordinateContextS102(context) ||
                    IsPendingCoordinateContextS102(row.ImageCoordinate) || context != row.ImageCoordinate)
                { omitted++; continue; }
                if (row.Overlays.Count >= 8) { omitted++; continue; }
                HObject shape = null;
                try
                {
                    long estimate = 0;
                    var region = raw as VmRegionSnapshot; var xld = raw as VmXldSnapshot;
                    if (region != null)
                    {
                        // Area bounds the maximum number of region runs; use a conservative estimate.
                        if (region.ObjectCount > 256 || region.TotalArea > 8L * 1024 * 1024 / 24) { omitted++; continue; }
                        estimate = (long)Math.Ceiling(Math.Max(0, region.TotalArea)) * 24 + region.ObjectCount * 256L;
                        shape = region.CreateRegionCopy();
                    }
                    else if (xld != null)
                    {
                        if (xld.ObjectCount > 256) { omitted++; continue; }
                        shape = xld.CreateXldCopy();
                        estimate = xld.ObjectCount * 256L;
                        for (int index = 1; index <= xld.ObjectCount; index++)
                        {
                            HObject contour = null; HTuple rows = null, columns = null;
                            try
                            {
                                HOperatorSet.SelectObj(shape, out contour, index);
                                HOperatorSet.GetContourXld(contour, out rows, out columns);
                                estimate += (long)rows.Length * 32;
                            }
                            finally { if (contour != null) contour.Dispose(); if (rows != null) rows.Dispose(); if (columns != null) columns.Dispose(); }
                            if (estimate > 8L * 1024 * 1024) break;
                        }
                    }
                    if (shape == null) continue;
                    if (row.Overlays.Sum(item => item.EstimatedBytes) + estimate > 8L * 1024 * 1024) { omitted++; continue; }
                    foreach (var old in subflowTraceRowsS102.Where(item => !ReferenceEquals(item, row) && item.Overlays.Count > 0).ToList())
                    {
                        if (subflowTraceRowsS102.Sum(item => item.Overlays.Sum(overlay => overlay.EstimatedBytes)) + estimate <= 32L * 1024 * 1024) break;
                        foreach (var overlay in old.Overlays) overlay.Shape.Dispose();
                        old.Overlays.Clear(); old.OverlayNotice = "叠加已从有界调试缓存释放，图像与端口记录保留。";
                    }
                    row.Overlays.Add(new SubflowTraceOverlayS102 { Shape = shape, Label = port.DisplayName + " · " + port.DataType,
                        Color = region != null ? "green" : "cyan", EstimatedBytes = estimate });
                    shape = null;
                }
                catch (Exception ex) { omitted++; row.OverlayNotice = "部分叠加未保留：" + LimitSubflowTraceTextS102(ex.Message); }
                finally { if (shape != null) shape.Dispose(); }
            }
            if (omitted > 0) row.OverlayNotice = (row.OverlayNotice ?? string.Empty) + " · " + omitted + " 项因坐标未确认/不一致或调试预算限制未叠加。";
        }

        private void RefreshSubflowTraceUiS102()
        {
            if (SubflowTraceExpanderS102 == null) return;
            SubflowTraceExpanderS102.Visibility = subflowTraceRowsS102.Count > 0 || subflowTraceFramesS102.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            SubflowTraceExpanderS102.Header = "子流程调用 · " + subflowTraceRowsS102.Count + " 步记录";
            if (!ReferenceEquals(SubflowTraceListS102.ItemsSource, subflowTraceRowsS102)) SubflowTraceListS102.ItemsSource = subflowTraceRowsS102;
            SubflowTraceListS102.Items.Refresh();
            var frame = subflowTraceFramesS102.Count == 0 ? null : subflowTraceFramesS102.Peek();
            SubflowCallStackTextS102.Text = frame == null ? "本次调用已返回 / 中断；记录只属于本次调试。" :
                string.Join(" → ", subflowTraceFramesS102.Reverse().Select(item => item.Call.InstanceName)) + "\n当前 / 最近：" + frame.Current + "\n下一步：" + frame.Next + (isPauseRequested ? "（已暂停）" : string.Empty);
            RefreshSubflowTraceSelectionS102();
        }

        private void SubflowTraceSelectionS102_Changed(object sender, SelectionChangedEventArgs e) { RefreshSubflowTraceSelectionS102(); }

        private void RefreshSubflowTraceSelectionS102()
        {
            if (SubflowTraceListS102 == null) return;
            var row = SubflowTraceListS102.SelectedItem as SubflowTraceRowS102;
            SubflowTraceDetailsS102.Text = row == null ? "选择内部步骤查看输入、结果或异常。" : row.Details + "\n" + row.ImageLabel +
                (row.Overlays.Count == 0 ? string.Empty : "\n本次图形：" + string.Join("、", row.Overlays.Select(item => item.Label))) + "\n" + row.OverlayNotice;
            SubflowTraceImageButtonS102.IsEnabled = row != null && row.Image != null && !row.Image.IsDisposed;
            SubflowTracePortsS102.ItemsSource = row == null ? null : row.Ports;
            SubflowTracePortsNoticeS102.Text = row == null ? "输入记录开始时可用值，不代表所有分支均读取；输出仅记录本次发布值。" :
                "输入为开始时可用值，不代表实际读取；输出为本次发布值。" + row.PortNotice;
            SubflowTraceLocateButtonS102.IsEnabled = row != null && row.DefinitionPath != null && row.DefinitionPath.Count > 0 &&
                !isFlowExecutionActive && !isContinuousRunning && !isImageQueueBatchRunning && !IsCurrentToolContinuousTrialRunningS87 && !historicalImageMode;
        }

        private void LocateSubflowTraceToolS102_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("定位内部模块", delegate
            {
                var row = SubflowTraceListS102.SelectedItem as SubflowTraceRowS102;
                if (row == null || row.DefinitionPath == null || !CanNavigateInputRepairS96()) return;
                var path = row.DefinitionPath.ToList();
                if (path.Count == 0 || !CloseCentralModuleConfigurationS77()) return;
                // Resolve against the current complete recipe before switching any workspace.
                var tools = CaptureRootFlowRecipeS102();
                for (int i = 0; i < path.Count; i++)
                {
                    var item = tools.FirstOrDefault(candidate => candidate.ToolId == path[i]);
                    if (item == null) throw new InvalidOperationException("该记录对应的模块已删除或替换，不能按名称猜测定位。");
                    if (i + 1 < path.Count)
                    {
                        if (item.Parameters == null || item.Parameters.Subflow == null) throw new InvalidOperationException("该记录的内部流程结构已变化。");
                        tools = item.Parameters.Subflow.Tools;
                    }
                }
                var current = subflowEditorsS102.Reverse().Select(frame => frame.Call.ToolId).ToList();
                int common = 0;
                while (common < current.Count && common < path.Count - 1 && current[common] == path[common]) common++;
                while (subflowEditorsS102.Count > common)
                {
                    int count = subflowEditorsS102.Count; ReturnSubflowEditorS102_Click(null, null);
                    if (subflowEditorsS102.Count >= count) return;
                }
                for (int i = common; i < path.Count - 1; i++)
                {
                    var call = flowTools.FirstOrDefault(tool => tool.ToolId == path[i]);
                    if (call == null || call.Kind != VmToolKind.SubflowCall) throw new InvalidOperationException("调用路径已变化，请重新运行生成记录。");
                    int count = subflowEditorsS102.Count; EnterSubflowEditorS102(call);
                    if (subflowEditorsS102.Count != count + 1) return;
                }
                FocusFlowGraphTool(path.Last(), "已定位记录对应的模块定义；双击可编辑。历史输入/输出不是当前运行结果。");
            });
        }

        private void ShowSubflowTraceImageS102_Click(object sender, RoutedEventArgs e)
        {
            var row = SubflowTraceListS102.SelectedItem as SubflowTraceRowS102;
            if (row == null || row.Image == null || row.Image.IsDisposed) return;
            if (subflowTraceImageWindowS102 != null) subflowTraceImageWindowS102.Close();
            HImage copy = row.Image.CreateImageCopy();
            var overlayCopies = new List<SubflowTraceOverlayS102>();
            var controller = new ImageViewportController(); controller.SetImageSize(row.Image.Width, row.Image.Height);
            var control = new HWindowControl { Dock = Forms.DockStyle.Fill, BackColor = System.Drawing.Color.FromArgb(11,18,32) };
            var host = new System.Windows.Forms.Integration.WindowsFormsHost { Child = control };
            var panel = new DockPanel();
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8) };
            var fit = new Button { Content = "适应", Padding = new Thickness(8,3,8,3), Margin = new Thickness(0,0,6,0) };
            var original = new Button { Content = "1:1", Padding = new Thickness(8,3,8,3) };
            toolbar.Children.Add(fit); toolbar.Children.Add(original);
            var showOverlays = new CheckBox { Content = "本次结果叠加", IsChecked = row.Overlays.Count > 0, IsEnabled = row.Overlays.Count > 0,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12,0,0,0), ToolTip = "仅显示与此图像坐标来源一致的 Region/XLD 输出；不显示可编辑 ROI。" };
            toolbar.Children.Add(showOverlays);
            DockPanel.SetDock(toolbar, Dock.Top); panel.Children.Add(toolbar);
            var status = new TextBlock { Text = row.ImageLabel + "\n" + string.Join("、", row.Overlays.Select(item => item.Label)) +
                " · 只读快照，不可编辑 ROI\n" + row.OverlayNotice, Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
            DockPanel.SetDock(status, Dock.Bottom); panel.Children.Add(status); panel.Children.Add(host);
            var window = new Window { Owner = this, Title = "调用图像 · " + row.Path, Width = 800, Height = 580, MinWidth = 450, MinHeight = 300,
                ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel };
            bool closed = false; bool panning = false; System.Drawing.Point last = new System.Drawing.Point();
            Action draw = delegate
            {
                if (closed || !control.IsHandleCreated) return;
                try
                {
                    control.HalconWindow.ClearWindow(); controller.Apply(control.HalconWindow); copy.DispImage(control.HalconWindow);
                    if (showOverlays.IsChecked == true)
                    {
                        control.HalconWindow.SetDraw("margin"); control.HalconWindow.SetLineWidth(2);
                        foreach (var overlay in overlayCopies) { control.HalconWindow.SetColor(overlay.Color); control.HalconWindow.DispObj(overlay.Shape); }
                    }
                }
                catch (Exception ex) { status.Text = "图像显示失败：" + ex.Message; }
            };
            fit.Click += delegate { controller.Fit(); draw(); }; original.Click += delegate { controller.OriginalSize(control); draw(); };
            showOverlays.Checked += delegate { draw(); }; showOverlays.Unchecked += delegate { draw(); };
            control.Resize += delegate { draw(); }; control.MouseWheel += (s, args) => { controller.ZoomAt(args.Location, control, args.Delta); draw(); };
            control.MouseDown += (s, args) => { if (args.Button == Forms.MouseButtons.Left) { panning = true; last = args.Location; control.Capture = true; control.Focus(); } };
            control.MouseMove += (s, args) => { if (panning) { controller.PanByWindowDelta(args.X-last.X, args.Y-last.Y, control); last = args.Location; draw(); } };
            control.MouseUp += delegate { panning = false; control.Capture = false; };
            window.Loaded += delegate { draw(); };
            Action dispose = delegate
            {
                if (closed) return;
                closed = true; host.Child = null; control.Dispose(); copy.Dispose();
                foreach (var overlay in overlayCopies) overlay.Shape.Dispose(); overlayCopies.Clear();
                if (ReferenceEquals(subflowTraceImageWindowS102, window)) subflowTraceImageWindowS102 = null;
            };
            window.Closed += delegate { dispose(); };
            subflowTraceImageWindowS102 = window;
            try
            {
                foreach (var overlay in row.Overlays)
                {
                    HObject shape; HOperatorSet.CopyObj(overlay.Shape, out shape, 1, -1);
                    overlayCopies.Add(new SubflowTraceOverlayS102 { Shape = shape, Color = overlay.Color, Label = overlay.Label });
                }
                window.Show();
            }
            catch (Exception ex) { dispose(); HeaderStatusText.Text = "调用图像预览无法打开：" + ex.Message; }
        }
    }
}
