using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmToolInstance colorPickingToolS129;
        private VmImageSnapshot colorPickingImageS129;
        private long colorPickEditLeaseS137 = -1;
        private WeakReference<VmImageSnapshot> colorSampleImageUI03;
        private string colorPickNoticeUI03;
        private string colorPickDetailsUI03;

        private void LoadColorExampleS129_Click(object sender, RoutedEventArgs e)
        { if (RequireEngineeringEditS137("载入颜色检测示例")) LoadExampleProgramS78("color-coverage"); }

        private bool BuildColorExampleS129()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 彩色零件图像", true, null);
            var color = CreateFlowTool(VmToolKind.ColorInspectionHsv, "02 红色区域检测", true, null);
            var feature = CreateFlowTool(VmToolKind.RegionFeatureFilter, "03 排除小色块", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "04 原始红色覆盖比例判定", true, null);
            foreach (var tool in new[] { source, color, feature, judge }) flowTools.Add(tool);
            color.SetInputBinding("Image", source.ToolId, "Image");
            feature.SetInputBinding("Region", color.ToolId, "Region");
            feature.Parameters.RegionFeature = VmRegionFeature.Area;
            feature.Parameters.RegionFeatureMin = 20; feature.Parameters.RegionFeatureMax = 1000000000;
            judge.SetInputBinding("Value", color.ToolId, "AreaRatio");
            judge.InputToolId = color.ToolId; judge.InputPortName = "AreaRatio";
            judge.NumericLowerLimit = 0.1; judge.NumericUpperLimit = 100;
            string path = GetHalconExampleImageS78(System.IO.Path.Combine("color", "color_fuses_00.png"));
            source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "EXAMPLE-COLOR";
            return System.IO.File.Exists(path);
        }

        private void LoadColorDraftS129(VmToolParameterData parameters)
        {
            EndColorPickS129();
            ClearColorSampleUI03();
            var color = parameters.ColorInspectionS129 ?? new VmColorInspectionParametersS129();
            DockColorHueS129.Text = color.HueDegrees.ToString("R", CultureInfo.InvariantCulture);
            DockColorToleranceS129.Text = color.HueToleranceDegrees.ToString("R", CultureInfo.InvariantCulture);
            DockColorSatMinS129.Text = color.SaturationMinimum.ToString("R", CultureInfo.InvariantCulture);
            DockColorSatMaxS129.Text = color.SaturationMaximum.ToString("R", CultureInfo.InvariantCulture);
            DockColorValueMinS129.Text = color.ValueMinimum.ToString("R", CultureInfo.InvariantCulture);
            DockColorValueMaxS129.Text = color.ValueMaximum.ToString("R", CultureInfo.InvariantCulture);
            DockColorChipS129.Background = Brushes.Transparent;
            DockColorSampleSummaryS129.Text = "尚未取样 · 可直接输入色相和范围";
        }

        private bool ReadColorDraftS129(VmToolParameterData parameters)
        {
            var color = new VmColorInspectionParametersS129();
            double number;
            if (!TryParseUiDouble(DockColorHueS129.Text, out number)) return FailDockDraft("目标色相必须是角度数值。");
            color.HueDegrees = number;
            if (!TryParseUiDouble(DockColorToleranceS129.Text, out number)) return FailDockDraft("色相容差必须是角度数值。");
            color.HueToleranceDegrees = number;
            if (!TryParseUiDouble(DockColorSatMinS129.Text, out number)) return FailDockDraft("饱和度下限必须是百分比数值。");
            color.SaturationMinimum = number;
            if (!TryParseUiDouble(DockColorSatMaxS129.Text, out number)) return FailDockDraft("饱和度上限必须是百分比数值。");
            color.SaturationMaximum = number;
            if (!TryParseUiDouble(DockColorValueMinS129.Text, out number)) return FailDockDraft("亮度下限必须是百分比数值。");
            color.ValueMinimum = number;
            if (!TryParseUiDouble(DockColorValueMaxS129.Text, out number)) return FailDockDraft("亮度上限必须是百分比数值。");
            color.ValueMaximum = number;
            string error = color.Validate();
            if (!string.IsNullOrWhiteSpace(error)) return FailDockDraft(error);
            parameters.ColorInspectionS129 = color;
            return true;
        }

        private void ClearColorSampleUI03()
        {
            colorSampleImageUI03 = null;
            colorPickNoticeUI03 = colorPickDetailsUI03 = null;
            if (DockColorChipS129 != null) DockColorChipS129.Background = Brushes.Transparent;
        }

        private void SetColorNoticeUI03(string message, string details = null)
        {
            colorPickNoticeUI03 = message; colorPickDetailsUI03 = details ?? message;
            DockColorSampleSummaryS129.Text = message;
            DockColorSampleSummaryS129.ToolTip = colorPickDetailsUI03;
        }

        // Existing metadata only: refreshing this panel must not acquire images,
        // calculate HSV or change the applied configuration.
        private string ColorPickUnavailableUI03(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.ColorInspectionHsv) return "请打开颜色检测模块";
            if (!IsVariationTaskStoppedS130() || historicalImageMode) return "请先停止运行并返回当前图像";
            if (!ColorInputDraftMatchesS129(tool)) return "先应用输入 / ROI 来源，再取色";
            var binding = tool.GetInputBinding("Image");
            if (binding == null) return "在“输入”页连接 RGB 图像";
            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) return error;
            var source = GetInputSourceTool(binding);
            var snapshot = GetImageSnapshot(source, binding.SourcePortName);
            if (source == null || !source.IsEnabled || source.ResultCode != "OK" || !source.LastOutputUtc.HasValue ||
                !string.IsNullOrWhiteSpace(source.ErrorMessage) || snapshot == null || snapshot.IsDisposed)
                return "先运行上游图像源，再取色";
            if (snapshot.ChannelCount != 3) return "当前为灰度图，请在“输入”页选择 RGB 图像";
            try { ValidateThresholdRoiBindingsUIBatch02(tool); }
            catch (InvalidOperationException ex) { return ex.Message; }
            return null;
        }

        private bool ColorInputDraftMatchesS129(VmToolInstance tool)
        {
            foreach (var port in ToolMetadata.GetInstanceInputPorts(tool))
            {
                var row = dockInputPortRows.FirstOrDefault(item => item.PortName == port.PortName);
                if (row != null && row.SelectedSourceKey != GetDockSavedInputSourceKey(tool, port)) return false;
            }
            if (dockRoiBindingRows.Any(row => row.IsBound != tool.IsRoiBound(row.RoiId))) return false;
            return VmRoiSourceMode.Normalize(DockRoiSourceModeComboBox.SelectedValue as string) ==
                GetAppliedRoiSourceModeS86(tool);
        }

        private bool CanPickColorS129()
        {
            return colorPickingToolS129 != null && dockEditingTool == colorPickingToolS129 && centralModuleConfigurationOpenS77 &&
                !historicalImageMode && !isFlowExecutionActive && !isContinuousRunning && !isImageQueueBatchRunning &&
                !IsCurrentToolContinuousTrialRunningS87 && System.Threading.Volatile.Read(ref tcpTriggerPendingS104) == 0 &&
                ColorInputDraftMatchesS129(colorPickingToolS129) && colorPickingImageS129 != null && !colorPickingImageS129.IsDisposed &&
                IsEngineeringEditLeaseCurrentS137(colorPickEditLeaseS137, false);
        }

        private void EndColorPickS129()
        {
            colorPickingToolS129 = null; colorPickingImageS129 = null;
            colorPickEditLeaseS137 = -1;
            if (DockColorCancelPickS129 != null) DockColorCancelPickS129.Visibility = Visibility.Collapsed;
        }

        private void RefreshColorPickStateS129()
        {
            if (dockDraftUpdating || DockColorPickButtonUI03 == null || dockEditingTool == null || dockEditingTool.Kind != VmToolKind.ColorInspectionHsv) return;
            var binding = dockEditingTool.GetInputBinding("Image");
            var snapshot = binding == null ? null : GetImageSnapshot(GetInputSourceTool(binding), binding.SourcePortName);
            string reason = ColorPickUnavailableUI03(dockEditingTool);
            if (colorPickingToolS129 != null && (!CanPickColorS129() || reason != null || !ReferenceEquals(colorPickingImageS129, snapshot)))
            { EndColorPickS129(); ClearColorSampleUI03(); SetColorNoticeUI03("图像或配置已改变，请重新取色"); }
            VmImageSnapshot sampled;
            if (colorSampleImageUI03 != null && (!colorSampleImageUI03.TryGetTarget(out sampled) || !ReferenceEquals(sampled, snapshot) || reason != null))
                ClearColorSampleUI03();
            DockColorPickButtonUI03.IsEnabled = reason == null && colorPickingToolS129 == null;
            DockColorPickButtonUI03.ToolTip = reason ?? "在当前模块的 RGB 输入图像上单击有色点；仅填写草稿色相，不改变容差、不自动应用或运行。";
            if (reason != null) { DockColorSampleSummaryS129.Text = reason; DockColorSampleSummaryS129.ToolTip = reason; }
            else if (colorPickingToolS129 == null)
            {
                DockColorSampleSummaryS129.Text = colorPickNoticeUI03 ?? string.Format(CultureInfo.InvariantCulture, "输入 {0}×{1} RGB · 可取色或手填色相", snapshot.Width, snapshot.Height);
                DockColorSampleSummaryS129.ToolTip = colorPickDetailsUI03 ?? (snapshot.ProducerToolName + "." + snapshot.ProducerPortName + " · " + snapshot.DisplayText);
            }
        }

        private void DockColorPickS129_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("图像取色写入草稿");
            if (editLeaseS137 < 0) return;
            try
            {
                EndColorPickS129();
                var tool = dockEditingTool;
                string unavailable = ColorPickUnavailableUI03(tool);
                if (unavailable != null) throw new InvalidOperationException(unavailable);
                if (tool == null || tool.Kind != VmToolKind.ColorInspectionHsv || historicalImageMode || isFlowExecutionActive ||
                    isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87 ||
                    System.Threading.Volatile.Read(ref tcpTriggerPendingS104) != 0)
                    throw new InvalidOperationException("请在当前模块、停止运行后取色。");
                if (!ColorInputDraftMatchesS129(tool)) throw new InvalidOperationException("输入或 ROI 来源尚未应用；先应用来源设置，再取色。");
                var binding = tool.GetInputBinding("Image");
                if (binding == null) throw new InvalidOperationException("先在输入页连接明确的 RGB 图像来源。");
                var source = GetInputSourceTool(binding);
                var snapshot = GetImageSnapshot(source, binding.SourcePortName);
                if (source == null || source.ResultCode != "OK" || snapshot == null || snapshot.IsDisposed)
                    throw new InvalidOperationException("RGB 来源尚未在本周期运行，请先运行上游图像源。");
                using (HImage image = snapshot.CreateImageCopy()) VmHsvImagesS129.GetRgbScale(image);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                EndProfileEditS93(false);
                roiEditor.Tool = Services.VisionTool.Select;
                ClearPendingRoi();
                if (xldContourSelectionMode) SetXldContourSelectionMode(false, false);
                SetImageContextSelection(VmImageContextOption.ModuleInput);
                UpdateImageContextViewport(true);
                colorPickingToolS129 = tool; colorPickingImageS129 = snapshot;
                colorPickEditLeaseS137 = editLeaseS137;
                DockColorCancelPickS129.Visibility = Visibility.Visible;
                ClearColorSampleUI03();
                SetColorNoticeUI03("点击输入图像中的目标颜色 · Esc 取消", "只更新当前草稿的目标色相；饱和度、亮度和容差保留。点击必须位于实际输入图像与有效 ROI / 屏蔽范围内。");
                DockColorPickButtonUI03.IsEnabled = false;
                HeaderStatusText.Text = DockColorSampleSummaryS129.Text;
                ScheduleRefreshDisplay();
            }
            catch (Exception ex) { SetColorNoticeUI03(ex.Message); HeaderStatusText.Text = ex.Message; }
        }

        private void DockColorCancelPickS129_Click(object sender, RoutedEventArgs e)
        { EndColorPickS129(); SetColorNoticeUI03("取色已取消 · 原参数草稿保留"); RefreshColorPickStateS129(); }

        private bool ColorMouseDownS129(System.Drawing.PointF point, bool comparison)
        {
            if (colorPickingToolS129 == null) return false;
            if (!IsEngineeringEditLeaseCurrentS137(colorPickEditLeaseS137))
            { EndColorPickS129(); SetColorNoticeUI03("工程授权已改变，取色未写入草稿"); return true; }
            string unavailable = ColorPickUnavailableUI03(colorPickingToolS129);
            if (!CanPickColorS129() || unavailable != null)
            { EndColorPickS129(); SetColorNoticeUI03(unavailable ?? "取色已结束，请在当前模块重新开始"); return true; }
            var context = comparison ? ResolveDualInputImageContext() : ResolveImageContext();
            var binding = colorPickingToolS129.GetInputBinding("Image");
            if (binding == null || !ReferenceEquals(GetImageSnapshot(GetInputSourceTool(binding), binding.SourcePortName), colorPickingImageS129))
            { EndColorPickS129(); SetColorNoticeUI03("图像已更新，请重新开始取色。"); return true; }
            if (!context.HasImage || !ReferenceEquals(context.Snapshot, colorPickingImageS129))
            { SetColorNoticeUI03("请在该模块订阅的输入画面上取色。"); return true; }
            int row = (int)Math.Floor(point.Y), column = (int)Math.Floor(point.X);
            if (row < 0 || column < 0 || row >= context.Height || column >= context.Width)
            { SetColorNoticeUI03("取样点越界，请点击图像内的颜色。"); return true; }
            try
            {
                double[] sample;
                using (HImage image = colorPickingImageS129.CreateImageCopy())
                using (HRegion roi = CreateEffectiveRoiRegion(colorPickingToolS129, false))
                using (HRegion domain = image.GetDomain())
                using (HRegion effective = roi == null ? domain.CopyObj(1, -1) : domain.Intersection(roi))
                {
                    HTuple inside = null;
                    try
                    {
                        HOperatorSet.TestRegionPoint(effective, row, column, out inside);
                        if (inside.Length != 1 || inside.I != 1) throw new InvalidOperationException("取样点位于 ROI/图像有效域之外或屏蔽区内。");
                    }
                    finally { if (inside != null) inside.Dispose(); }
                    // One real pixel uses the same HALCON conversion as detection, without converting the full image for a click.
                    using (HImage pixel = image.CropRectangle1(row, column, row, column))
                    using (VmHsvImagesS129 hsv = VmHsvImagesS129.Create(pixel, null)) sample = hsv.ReadSample(pixel, 0, 0);
                }
                if (sample[1] < 3 || sample[2] < 2)
                    throw new InvalidOperationException("该点饱和度过低或太暗，请改选有色点。检测黑/白/灰可在常用页将容差设为 180°，再限定饱和度和亮度。");
                double hue = ((sample[0] % 360) + 360) % 360;
                if (!IsEngineeringEditLeaseCurrentS137(colorPickEditLeaseS137))
                { EndColorPickS129(); SetColorNoticeUI03("工程授权已改变，取色未写入草稿"); return true; }
                var sampledImage = colorPickingImageS129;
                EndColorPickS129();
                // A picked pixel proposes a user-editable angle, not float noise
                // filling the short field. Explicit existing recipe values stay exact.
                double proposedHue = Math.Round(hue, 3) % 360;
                DockColorHueS129.Text = proposedHue.ToString("0.###", CultureInfo.InvariantCulture);
                DockDraftValue_Changed(DockColorHueS129, new RoutedEventArgs());
                colorSampleImageUI03 = new WeakReference<VmImageSnapshot>(sampledImage);
                DockColorChipS129.Background = new SolidColorBrush(Color.FromRgb(ToColorByteS129(sample[3]), ToColorByteS129(sample[4]), ToColorByteS129(sample[5])));
                SetColorNoticeUI03(string.Format(CultureInfo.InvariantCulture, "已取色 H {0:0.###}° · 仅填入草稿", proposedHue),
                    string.Format(CultureInfo.InvariantCulture, "样本 X={0}, Y={1} · H={2:G9}° S={3:0.##}% V={4:0.##}% · 容差和过滤范围保留；F5 试运行，应用后重跑下游。", column, row, hue, sample[1], sample[2]));
                RefreshColorPickStateS129();
            }
            catch (Exception ex) { SetColorNoticeUI03(ex.Message); HeaderStatusText.Text = ex.Message; }
            return true;
        }

        private static byte ToColorByteS129(double value) { return (byte)Math.Max(0, Math.Min(255, Math.Round(value * 255))); }

        private InspectionRecord RunColorInspectionS129(VmToolInstance tool, HImage image)
        {
            var parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string error = parameters.Validate(VmToolKind.ColorInspectionHsv);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            if (tool.GetInputBinding("Image") == null) throw new InvalidOperationException("HSV 颜色检测必须连接明确的 RGB Image 来源。");
            var color = parameters.ColorInspectionS129;
            HObject hueRegion = null, first = null, second = null, satRegion = null, valueRegion = null, matchHs = null, match = null, connected = null;
            HTuple area = null, row = null, column = null, count = null;
            try
            {
                using (HRegion roi = CreateEffectiveRoiRegion(tool, false))
                using (VmHsvImagesS129 hsv = VmHsvImagesS129.Create(image, roi))
                {
                    double center = color.HueDegrees * Math.PI / 180, tolerance = color.HueToleranceDegrees * Math.PI / 180;
                    double lower = center - tolerance, upper = center + tolerance;
                    if (color.HueToleranceDegrees >= 180) HOperatorSet.CopyObj(hsv.Domain, out hueRegion, 1, -1);
                    else if (lower < 0)
                    {
                        HOperatorSet.Threshold(hsv.Hue, out first, 0, upper);
                        HOperatorSet.Threshold(hsv.Hue, out second, lower + 2 * Math.PI, 2 * Math.PI);
                        HOperatorSet.Union2(first, second, out hueRegion);
                    }
                    else if (upper >= 2 * Math.PI)
                    {
                        HOperatorSet.Threshold(hsv.Hue, out first, lower, 2 * Math.PI);
                        HOperatorSet.Threshold(hsv.Hue, out second, 0, upper - 2 * Math.PI);
                        HOperatorSet.Union2(first, second, out hueRegion);
                    }
                    else HOperatorSet.Threshold(hsv.Hue, out hueRegion, lower, upper);
                    HOperatorSet.Threshold(hsv.Saturation, out satRegion, color.SaturationMinimum / 100, color.SaturationMaximum / 100);
                    HOperatorSet.Threshold(hsv.Value, out valueRegion, color.ValueMinimum / 100, color.ValueMaximum / 100);
                    HOperatorSet.Intersection(hueRegion, satRegion, out matchHs);
                    HOperatorSet.Intersection(matchHs, valueRegion, out match);
                    HOperatorSet.AreaCenter(match, out area, out row, out column);
                    double matchedArea = SumTuple(area), ratio = 100 * matchedArea / hsv.PixelCount;
                    if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio < 0 || ratio > 100.000001)
                        throw new InvalidOperationException("颜色覆盖比例无效，已阻止发布。");
                    int regionCount = 0;
                    if (matchedArea > 0)
                    { HOperatorSet.Connection(match, out connected); HOperatorSet.CountObj(connected, out count); regionCount = count.I; }
                    else HOperatorSet.GenEmptyObj(out connected);
                    string context = ResolveTargetImageContextIdS86(tool);
                    if (IsPendingCoordinateContextS102(context)) throw new InvalidOperationException("RGB 图像坐标来源尚未明确。");
                    tool.SetOutputValue("Region", VmRegionSnapshot.Create(connected, tool.ToolId, tool.InstanceName, "Region"));
                    tool.SetOutputCoordinateContext("Region", context);
                    tool.SetOutputValue("Area", matchedArea); tool.SetOutputValue("AreaRatio", ratio);
                    tool.SetOutputValue("RegionCount", (double)regionCount); tool.SetOutputValue("SampleCount", hsv.PixelCount);
                    tool.SetOutputValue("HasMatch", matchedArea > 0); tool.SetOutputValue("HueCenter", color.HueDegrees);
                    tool.SetOutputValue("HueTolerance", color.HueToleranceDegrees);
                    tool.SetOutputValue("Parameters", color.Summary() + "; RGB: byte/255, uint2/65535, real[0,1]; ROI valid pixels denominator");
                    ReplaceToolOverlayRegion(connected, tool, "Region", regionCount, matchedArea, "OK"); connected = null;
                    string message = string.Format(CultureInfo.InvariantCulture, "HSV 颜色检测：覆盖 {0:0.###}% · 匹配 {1:0} / 有效 {2:0} 像素 · {3} 个区域 · {4}",
                        ratio, matchedArea, hsv.PixelCount, regionCount, color.Summary());
                    var record = CreateRecord("ColorInspectionHsv", "OK", matchedArea, message, tool);
                    resultStore.Add(record); LogInfo(tool.InstanceName + " 完成：" + message); return record;
                }
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
            finally
            {
                DisposeObject(hueRegion); DisposeObject(first); DisposeObject(second); DisposeObject(satRegion); DisposeObject(valueRegion);
                DisposeObject(matchHs); DisposeObject(match); DisposeObject(connected);
                if (area != null) area.Dispose(); if (row != null) row.Dispose(); if (column != null) column.Dispose(); if (count != null) count.Dispose();
            }
        }
    }
}
