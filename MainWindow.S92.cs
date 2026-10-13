using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private string profileDisplayToolIdS92;
        private string profileEditingToolS93;
        private int profileDragEndS93;
        private string[] profileDragOriginalS93;
        private object profileInputIdentityS93;
        private bool profileDragComparisonS93;
        private System.Drawing.PointF profileDragAnchorS93;
        private double[] profileDragGeometryS93;

        private void ProfileReverseS93_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("反向剖面采样线")) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.GrayLineProfile ||
                isFlowExecutionActive || isContinuousRunning || IsCurrentToolContinuousTrialRunningS87) return;
            if (profileDragEndS93 != 0) EndProfileEditS93(true);
            string row = DockProfileStartRowS92.Text, column = DockProfileStartColumnS92.Text;
            DockProfileStartRowS92.Text = DockProfileEndRowS92.Text;
            DockProfileStartColumnS92.Text = DockProfileEndColumnS92.Text;
            DockProfileEndRowS92.Text = row;
            DockProfileEndColumnS92.Text = column;
            HeaderStatusText.Text = "采样方向已反转（草稿）；试运行后更新曲线及极值距离，应用后保存。";
            ScheduleRefreshDisplay();
        }

        private bool ProfileInputIsCurrentS93(bool comparison)
        {
            if (!ProfileEditIsCurrentS93()) return false;
            ResolvedImageContext context = comparison ? ResolveDualInputImageContext() : ResolveImageContext();
            VmToolInputBindingData binding = dockEditingTool.GetInputBinding("Image");
            if (binding != null)
            {
                VmImageSnapshot bound = GetImageSnapshot(GetInputSourceTool(binding), binding.SourcePortName);
                if (bound == null || bound.IsDisposed || !ReferenceEquals(bound, context.Snapshot)) return false;
            }
            return context.HasImage && (comparison || context.RequestedKey == VmImageContextOption.ModuleInput) &&
                ReferenceEquals(profileInputIdentityS93, (object)context.Snapshot ?? context.GlobalImage);
        }

        private bool ProfileEditIsCurrentS93()
        {
            return profileEditingToolS93 != null && centralModuleConfigurationOpenS77 &&
                dockEditingTool != null && dockEditingTool.Kind == VmToolKind.GrayLineProfile &&
                dockEditingTool.ToolId == profileEditingToolS93 &&
                ReferenceEquals(GetImageTaskToolS89(), dockEditingTool) && !historicalImageMode &&
                !isFlowExecutionActive && !isContinuousRunning && !IsCurrentToolContinuousTrialRunningS87;
        }

        private void ProfilePickLineS93_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("图像剖面取线");
            if (editLeaseS137 < 0) return;
            profileEngineeringLeaseS137 = editLeaseS137;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.GrayLineProfile ||
                isFlowExecutionActive || isContinuousRunning || IsCurrentToolContinuousTrialRunningS87) return;
            SetImageContextSelection(VmImageContextOption.ModuleInput);
            if (!ResolveImageContext().HasImage)
            {
                HeaderStatusText.Text = "请先运行当前模块的上游图像源，再取线。";
                return;
            }
            ClearMeasurementDomainContext();
            FinishRoiGeometryEdit(false);
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Select;
            ClearPendingRoi();
            if (xldContourSelectionMode) SetXldContourSelectionMode(false, false);
            profileEditingToolS93 = dockEditingTool.ToolId;
            ResolvedImageContext inputContext = ResolveImageContext();
            profileInputIdentityS93 = (object)inputContext.Snapshot ?? inputContext.GlobalImage;
            if (!ProfileInputIsCurrentS93(false))
            {
                EndProfileEditS93(false);
                HeaderStatusText.Text = "订阅的图像尚未就绪；请先运行上游。取线不会使用回退图片。";
                return;
            }
            profileDragEndS93 = 0;
            UpdateImageContextViewport(true);
            DockProfilePickButtonS93.Content = "重新取线";
            DockProfileFinishButtonS93.Visibility = Visibility.Visible;
            DockProfileEditStateS93.Text = "拖端点微调 · 拖线平移 · Esc 撤回本次拖动";
            HeaderStatusText.Text = "拖出采样线；绿色为起点，橙色为终点。拖端点微调，Esc 撤回本次拖动；只修改草稿。";
            ScheduleRefreshDisplay();
        }

        private void ProfileFinishLineS93_Click(object sender, RoutedEventArgs e)
        {
            EndProfileEditS93(false);
        }

        private void EndProfileEditS93(bool cancelDrag)
        {
            if (cancelDrag && profileDragOriginalS93 != null && ProfileEditIsCurrentS93())
            {
                DockProfileStartRowS92.Text = profileDragOriginalS93[0];
                DockProfileStartColumnS92.Text = profileDragOriginalS93[1];
                DockProfileEndRowS92.Text = profileDragOriginalS93[2];
                DockProfileEndColumnS92.Text = profileDragOriginalS93[3];
            }
            profileEditingToolS93 = null;
            profileInputIdentityS93 = null;
            profileDragEndS93 = 0;
            profileDragOriginalS93 = null;
            profileDragGeometryS93 = null;
            if (imageWindow != null) imageWindow.Capture = false;
            if (comparisonImageWindow != null) comparisonImageWindow.Capture = false;
            if (DockProfilePickButtonS93 != null) DockProfilePickButtonS93.Content = "图像取线";
            if (DockProfileFinishButtonS93 != null) DockProfileFinishButtonS93.Visibility = Visibility.Collapsed;
            if (DockProfileEditStateS93 != null) DockProfileEditStateS93.Text = "也可直接填写像素坐标";
            ScheduleRefreshDisplay();
        }

        private long profileEngineeringLeaseS137 = -1;

        private bool ProfileMouseDownS93(System.Drawing.PointF point, System.Drawing.Point location, bool comparison = false)
        {
            if (!ProfileEditIsCurrentS93()) return false;
            if (!IsEngineeringEditLeaseCurrentS137(profileEngineeringLeaseS137)) { EndProfileEditS93(true); return true; }
            if (!ProfileInputIsCurrentS93(comparison))
            {
                EndProfileEditS93(true);
                HeaderStatusText.Text = "图像来源已变化，取线已结束；请在新的模块输入上重新开启。";
                return true;
            }
            var context = comparison ? ResolveDualInputImageContext() : ResolveImageContext();
            if (!context.HasImage || point.X < 0 || point.Y < 0 || point.X > context.Width - 1 || point.Y > context.Height - 1) return true;
            profileDragOriginalS93 = new[] { DockProfileStartRowS92.Text, DockProfileStartColumnS92.Text, DockProfileEndRowS92.Text, DockProfileEndColumnS92.Text };
            double r1, c1, r2, c2;
            double tolerance = GetXldHitTolerance(comparison ? comparisonViewport : viewport, comparison ? comparisonImageWindow : imageWindow, location);
            bool valid = TryParseUiDouble(DockProfileStartRowS92.Text, out r1) & TryParseUiDouble(DockProfileStartColumnS92.Text, out c1) &
                TryParseUiDouble(DockProfileEndRowS92.Text, out r2) & TryParseUiDouble(DockProfileEndColumnS92.Text, out c2);
            valid = valid && new[] { r1, c1, r2, c2 }.All(v => !double.IsNaN(v) && !double.IsInfinity(v));
            bool hitStart = valid && Math.Sqrt(Math.Pow(point.X - c1, 2) + Math.Pow(point.Y - r1, 2)) <= tolerance;
            bool hitEnd = valid && Math.Sqrt(Math.Pow(point.X - c2, 2) + Math.Pow(point.Y - r2, 2)) <= tolerance;
            bool hitLine = valid && r1 >= 0 && r2 >= 0 && c1 >= 0 && c2 >= 0 &&
                r1 <= context.Height - 1 && r2 <= context.Height - 1 && c1 <= context.Width - 1 && c2 <= context.Width - 1 &&
                DistanceToSegment(point, new System.Drawing.PointF((float)c1, (float)r1), new System.Drawing.PointF((float)c2, (float)r2)) <= tolerance * 0.65;
            profileDragEndS93 = hitStart ? 1 : hitEnd ? 2 : hitLine ? 3 : 2;
            profileDragAnchorS93 = point;
            profileDragGeometryS93 = new[] { r1, c1, r2, c2 };
            if (!hitStart && !hitEnd && !hitLine)
            {
                DockProfileStartRowS92.Text = point.Y.ToString("0.###", CultureInfo.InvariantCulture);
                DockProfileStartColumnS92.Text = point.X.ToString("0.###", CultureInfo.InvariantCulture);
            }
            profileDragComparisonS93 = comparison;
            (comparison ? comparisonImageWindow : imageWindow).Capture = true;
            ProfileMouseMoveS93(point, comparison);
            return true;
        }

        private bool ProfileMouseMoveS93(System.Drawing.PointF point, bool comparison = false)
        {
            if (profileDragEndS93 == 0) return false;
            if (!IsEngineeringEditLeaseCurrentS137(profileEngineeringLeaseS137)) { EndProfileEditS93(true); return true; }
            if (profileDragComparisonS93 != comparison) return false;
            if (!ProfileEditIsCurrentS93()) { EndProfileEditS93(false); return false; }
            if (!ProfileInputIsCurrentS93(comparison)) { EndProfileEditS93(true); return true; }
            var context = comparison ? ResolveDualInputImageContext() : ResolveImageContext();
            if (!context.HasImage) { EndProfileEditS93(true); return true; }
            if (profileDragEndS93 == 3 && profileDragGeometryS93 != null)
            {
                double[] g = profileDragGeometryS93;
                double dr = Math.Max(-Math.Min(g[0], g[2]), Math.Min(context.Height - 1 - Math.Max(g[0], g[2]), point.Y - profileDragAnchorS93.Y));
                double dc = Math.Max(-Math.Min(g[1], g[3]), Math.Min(context.Width - 1 - Math.Max(g[1], g[3]), point.X - profileDragAnchorS93.X));
                DockProfileStartRowS92.Text = (g[0] + dr).ToString("0.###", CultureInfo.InvariantCulture);
                DockProfileStartColumnS92.Text = (g[1] + dc).ToString("0.###", CultureInfo.InvariantCulture);
                DockProfileEndRowS92.Text = (g[2] + dr).ToString("0.###", CultureInfo.InvariantCulture);
                DockProfileEndColumnS92.Text = (g[3] + dc).ToString("0.###", CultureInfo.InvariantCulture);
                ScheduleRefreshDisplay();
                return true;
            }
            string row = Math.Max(0, Math.Min(context.Height - 1, point.Y)).ToString("0.###", CultureInfo.InvariantCulture);
            string column = Math.Max(0, Math.Min(context.Width - 1, point.X)).ToString("0.###", CultureInfo.InvariantCulture);
            if (profileDragEndS93 == 1) { DockProfileStartRowS92.Text = row; DockProfileStartColumnS92.Text = column; }
            else { DockProfileEndRowS92.Text = row; DockProfileEndColumnS92.Text = column; }
            ScheduleRefreshDisplay();
            return true;
        }

        private void DrawProfileLineS93(HWindow window, bool comparison = false)
        {
            if (!ProfileInputIsCurrentS93(comparison)) return;
            double r1, c1, r2, c2;
            if (!TryParseUiDouble(DockProfileStartRowS92.Text, out r1) || !TryParseUiDouble(DockProfileStartColumnS92.Text, out c1) ||
                !TryParseUiDouble(DockProfileEndRowS92.Text, out r2) || !TryParseUiDouble(DockProfileEndColumnS92.Text, out c2) ||
                new[] { r1, c1, r2, c2 }.Any(v => double.IsNaN(v) || double.IsInfinity(v))) return;
            window.SetLineWidth(2);
            window.SetColor("cyan");
            window.DispLine(r1, c1, r2, c2);
            double size = GetXldHitTolerance(comparison ? comparisonViewport : viewport,
                comparison ? comparisonImageWindow : imageWindow, new System.Drawing.Point(20, 20)) * 0.7;
            double length = Math.Sqrt((r2 - r1) * (r2 - r1) + (c2 - c1) * (c2 - c1));
            if (length > 0.001)
            {
                double arrow = Math.Min(size * 1.5, length * 0.25);
                double dr = (r2 - r1) / length, dc = (c2 - c1) / length;
                window.DispLine(r2, c2, r2 - dr * arrow + dc * arrow * 0.5, c2 - dc * arrow - dr * arrow * 0.5);
                window.DispLine(r2, c2, r2 - dr * arrow - dc * arrow * 0.5, c2 - dc * arrow + dr * arrow * 0.5);
            }
            window.SetColor("green");
            window.DispCross(r1, c1, size, 0);
            window.SetColor("orange");
            window.DispCross(r2, c2, size, 0);
            window.SetLineWidth(1);
        }
        private void LoadProfileParametersS92(VmToolParameterData p)
        {
            EndProfileEditS93(false);
            DockProfileStartRowS92.Text = p.ProfileStartRow.ToString(CultureInfo.InvariantCulture);
            DockProfileStartColumnS92.Text = p.ProfileStartColumn.ToString(CultureInfo.InvariantCulture);
            DockProfileEndRowS92.Text = p.ProfileEndRow.ToString(CultureInfo.InvariantCulture);
            DockProfileEndColumnS92.Text = p.ProfileEndColumn.ToString(CultureInfo.InvariantCulture);
            DockProfileSampleCountS92.Text = p.ProfileSampleCount.ToString(CultureInfo.InvariantCulture);
        }
        private bool ReadProfileParametersS92(VmToolParameterData p)
        {
            double value; int count;
            if (!TryParseUiDouble(DockProfileStartRowS92.Text, out value)) return FailDockDraft("剖面起点 Y 必须是数值。"); p.ProfileStartRow = value;
            if (!TryParseUiDouble(DockProfileStartColumnS92.Text, out value)) return FailDockDraft("剖面起点 X 必须是数值。"); p.ProfileStartColumn = value;
            if (!TryParseUiDouble(DockProfileEndRowS92.Text, out value)) return FailDockDraft("剖面终点 Y 必须是数值。"); p.ProfileEndRow = value;
            if (!TryParseUiDouble(DockProfileEndColumnS92.Text, out value)) return FailDockDraft("剖面终点 X 必须是数值。"); p.ProfileEndColumn = value;
            if (!int.TryParse(DockProfileSampleCountS92.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) return FailDockDraft("剖面采样数必须是整数。"); p.ProfileSampleCount = count;
            return true;
        }
        private InspectionRecord RunGrayLineProfileS92(VmToolInstance tool, HImage input)
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            string error = p.Validate(VmToolKind.GrayLineProfile);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            HObject xld = null;
            try
            {
                GrayLineProfileResult result = GrayLineProfileService.Analyze(input, p.ProfileStartRow, p.ProfileStartColumn, p.ProfileEndRow, p.ProfileEndColumn, p.ProfileSampleCount);
                using (HTuple rows = new HTuple(result.Rows.ToArray()))
                using (HTuple columns = new HTuple(result.Columns.ToArray()))
                    HOperatorSet.GenContourPolygonXld(out xld, rows, columns);
                double minimum = result.Values[result.MinimumIndex], maximum = result.Values[result.MaximumIndex];
                tool.SetOutputValue("Mean", result.Mean); tool.SetOutputValue("Deviation", result.Deviation);
                tool.SetOutputValue("Minimum", minimum); tool.SetOutputValue("Maximum", maximum); tool.SetOutputValue("Range", maximum - minimum);
                tool.SetOutputValue("SampleCount", (double)result.Values.Count); tool.SetOutputValue("Length", result.Length);
                tool.SetOutputValue("Samples", new VmNumberArrayS105(result.Values));
                tool.SetOutputValue("MinimumRow", result.Rows[result.MinimumIndex]); tool.SetOutputValue("MinimumColumn", result.Columns[result.MinimumIndex]);
                tool.SetOutputValue("MaximumRow", result.Rows[result.MaximumIndex]); tool.SetOutputValue("MaximumColumn", result.Columns[result.MaximumIndex]);
                tool.SetOutputValue("MinimumDistance", result.Length * result.MinimumIndex / (result.Values.Count - 1));
                tool.SetOutputValue("MaximumDistance", result.Length * result.MaximumIndex / (result.Values.Count - 1));
                tool.SetOutputValue("Parameters", string.Format(CultureInfo.InvariantCulture,
                    "HALCON bilinear; Start(Y,X)=({0},{1}); End=({2},{3}); Samples={4}; IncludesEndpoints=True; Domain=all-support-pixels; Tie=first-along-line",
                    p.ProfileStartRow, p.ProfileStartColumn, p.ProfileEndRow, p.ProfileEndColumn, p.ProfileSampleCount));
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(xld, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("ResultCode", "OK");
                ReplaceToolOverlayContours(xld, tool, "XLD", 1, "OK", result.Length); xld = null;
                tool.GrayLineProfileResult = result;
                string message = string.Format(CultureInfo.InvariantCulture, "采样 {0} 点，线长 {1:0.###} px，均值 {2:0.###}，标准差 {3:0.###}，灰度 {4:0.###}–{5:0.###}", result.Values.Count, result.Length, result.Mean, result.Deviation, minimum, maximum);
                InspectionRecord record = CreateRecord("GrayLineProfile", "OK", result.Mean, message, tool);
                resultStore.Add(record); LogInfo(tool.InstanceName + " 完成：" + message); return record;
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
            finally { DisposeObject(xld); }
        }
        private void RefreshProfileResultS92(VmToolInstance tool)
        {
            if (DockProfileExpanderS92 == null) return;
            string id = tool == null ? null : tool.ToolId;
            if (profileDisplayToolIdS92 != id) DockProfileExpanderS92.IsExpanded = false;
            profileDisplayToolIdS92 = id;
            DockProfileExpanderS92.Visibility = tool != null && tool.Kind == VmToolKind.GrayLineProfile ? Visibility.Visible : Visibility.Collapsed;
            DockProfileChartS92.Children.Clear();
            GrayLineProfileResult r = tool == null ? null : tool.GrayLineProfileResult;
            if (r == null) { DockProfileSummaryS92.Text = "连接图像并设置起终点，试运行后查看实际曲线。"; return; }
            double min = r.Values[r.MinimumIndex], max = r.Values[r.MaximumIndex];
            DockProfileSummaryS92.Text = string.Format(CultureInfo.InvariantCulture, "起点 → 终点 · {0} 点 / {1:0.###} px · 采样线见图像 XLD 图层", r.Values.Count, r.Length);
            Polyline line = new Polyline { Stroke = Brushes.Teal, StrokeThickness = 1.5 };
            for (int i = 0; i < r.Values.Count; ++i)
                line.Points.Add(new System.Windows.Point(42 + 336.0 * i / (r.Values.Count - 1), max == min ? 72 : 125 - 106 * (r.Values[i] - min) / (max - min)));
            DockProfileChartS92.Children.Add(line);
            AddProfileLabelS92(max.ToString("0.###", CultureInfo.InvariantCulture), 1, 12);
            AddProfileLabelS92(min.ToString("0.###", CultureInfo.InvariantCulture), 1, 119);
            AddProfileLabelS92("0 px", 42, 140);
            AddProfileLabelS92(r.Length.ToString("0.###", CultureInfo.InvariantCulture) + " px", 325, 140);
        }
        private void AddProfileLabelS92(string text, double x, double y)
        {
            TextBlock label = new TextBlock { Text = text, FontSize = 10, Foreground = Brushes.SlateGray };
            Canvas.SetLeft(label, x); Canvas.SetTop(label, y); DockProfileChartS92.Children.Add(label);
        }
    }
}
