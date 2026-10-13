using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private string histogramDisplayToolIdS91;

        private InspectionRecord RunGrayHistogramToolS91(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            string error = p.Validate(VmToolKind.GrayHistogram);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            int channels = imageService.GetChannelCount(inputImage);
            if (channels != 1 && channels != 3) throw new InvalidOperationException("直方图支持单通道或 RGB 三通道图像。");
            HObject domain = null, intersection = null, united = null, overlay = null;
            HTuple imageType = null, absolute = null, mean = null, deviation = null;
            using (HImage gray = CreateGrayImage(inputImage))
            using (HRegion roi = CreateEffectiveRoiRegion(tool, false))
            {
                try
                {
                    HOperatorSet.GetImageType(gray, out imageType);
                    string type = imageType.S;
                    int origin;
                    switch (type)
                    {
                        case "byte": case "uint2": origin = 0; break;
                        case "int1": origin = -128; break;
                        case "int2": origin = -32768; break;
                        default: throw new InvalidOperationException("直方图支持 byte/int1/int2/uint2 原始整数灰度；不自动缩放或截断 " + type + " 图像。");
                    }
                    // Both gray_histo_abs and intensity ignore image domain. Pass
                    // the explicit intersection, unioned once to avoid double counts.
                    HOperatorSet.GetDomain(gray, out domain);
                    HOperatorSet.Intersection(domain, roi == null ? domain : roi, out intersection);
                    HOperatorSet.Union1(intersection, out united);
                    HOperatorSet.GrayHistoAbs(united, gray, 1, out absolute);
                    int expectedBins = type == "byte" || type == "int1" ? 256 : 65536;
                    if (absolute.Length != expectedBins) throw new InvalidOperationException("HALCON 直方图长度与原始灰度类型不一致。");
                    long[] frequencies = new long[absolute.Length];
                    for (int i = 0; i < frequencies.Length; ++i) frequencies[i] = (long)absolute[i].D;
                    VmGrayHistogramResult histogram = new VmGrayHistogramResult(frequencies, origin, type);
                    HOperatorSet.Intensity(united, gray, out mean, out deviation);
                    string parameters = string.Format(CultureInfo.InvariantCulture,
                        "gray_histo_abs(q=1)+intensity; Type={0}; RGB={1}; Scope=InputDomain∩(ROI-Masks); Quantile=nearest-rank; CustomP={2:0.###}; PeakTie=lowest-gray",
                        type, channels == 3, p.HistogramPercentile);
                    tool.SetOutputValue("Mean", mean.D);
                    tool.SetOutputValue("Deviation", deviation.D);
                    tool.SetOutputValue("Minimum", (double)histogram.Minimum);
                    tool.SetOutputValue("Maximum", (double)histogram.Maximum);
                    tool.SetOutputValue("DynamicRange", (double)(histogram.Maximum - histogram.Minimum));
                    tool.SetOutputValue("PeakGray", (double)histogram.PeakGray);
                    tool.SetOutputValue("PeakCount", (double)histogram.PeakCount);
                    tool.SetOutputValue("P10", histogram.Quantile(10));
                    tool.SetOutputValue("P50", histogram.Quantile(50));
                    tool.SetOutputValue("P90", histogram.Quantile(90));
                    tool.SetOutputValue("Percentile", histogram.Quantile(p.HistogramPercentile));
                    tool.SetOutputValue("SampleCount", (double)histogram.SampleCount);
                    tool.SetOutputValue("Parameters", parameters);
                    tool.SetOutputValue("Region", VmRegionSnapshot.Create(united, tool.ToolId, tool.InstanceName, "Region"));
                    tool.SetOutputValue("ResultCode", "OK");
                    HOperatorSet.CopyObj(united, out overlay, 1, -1);
                    ReplaceToolOverlayRegion(overlay, tool, "Region", 1, histogram.SampleCount, "OK");
                    overlay = null;
                    tool.GrayHistogramResult = histogram;
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "有效像素={0}，均值={1:0.###}，标准差={2:0.###}，P10/P50/P90={3}/{4}/{5}，原始灰度={6}",
                        histogram.SampleCount, mean.D, deviation.D, histogram.Quantile(10), histogram.Quantile(50), histogram.Quantile(90), type);
                    InspectionRecord record = CreateRecord("GrayHistogram", "OK", mean.D, message, tool);
                    resultStore.Add(record);
                    LogInfo(tool.InstanceName + " 完成：" + message);
                    return record;
                }
                catch
                {
                    tool.ClearRuntimeOutputs();
                    throw;
                }
                finally
                {
                    DisposeObject(overlay); DisposeObject(united); DisposeObject(intersection); DisposeObject(domain);
                    if (imageType != null) imageType.Dispose();
                    if (absolute != null) absolute.Dispose();
                    if (mean != null) mean.Dispose();
                    if (deviation != null) deviation.Dispose();
                }
            }
        }

        private void RefreshHistogramResultS91(VmToolInstance tool)
        {
            if (DockHistogramExpanderS91 == null) return;
            string id = tool == null ? null : tool.ToolId;
            if (histogramDisplayToolIdS91 != id) DockHistogramExpanderS91.IsExpanded = false;
            histogramDisplayToolIdS91 = id;
            DockHistogramChartS91.Children.Clear();
            DockHistogramQuantileTextS91.Text = string.Empty;
            DockHistogramExpanderS91.Visibility = tool != null && tool.Kind == VmToolKind.GrayHistogram ? Visibility.Visible : Visibility.Collapsed;
            VmGrayHistogramResult h = tool == null ? null : tool.GrayHistogramResult;
            if (h == null)
            {
                DockHistogramScopeTextS91.Text = "尚无本次有效分布，请连接图像并试运行。";
                return;
            }
            DockHistogramScopeTextS91.Text = string.Format(CultureInfo.InvariantCulture,
                "{0} 个有效像素 · {1} 原始灰度 · 检测域见图像结果图层 Region", h.SampleCount, h.PixelType);
            DockHistogramQuantileTextS91.Text = string.Format(CultureInfo.InvariantCulture,
                "P10 {0} / P50 {1} / P90 {2} · 峰值 {3}（{4} 像素）", h.Quantile(10), h.Quantile(50), h.Quantile(90), h.PeakGray, h.PeakCount);
            int range = h.Maximum - h.Minimum + 1;
            int bins = Math.Min(256, range);
            long[] display = new long[bins];
            for (int gray = h.Minimum; gray <= h.Maximum; ++gray)
                display[(int)((long)(gray - h.Minimum) * bins / range)] += h.GetCount(gray - h.Origin);
            double peak = display.Max();
            for (int i = 0; i < bins; ++i)
            {
                double height = 106.0 * display[i] / peak;
                Rectangle bar = new Rectangle { Width = Math.Max(1, 360.0 / bins), Height = height, Fill = Brushes.Teal };
                int start = h.Minimum + (int)Math.Ceiling((double)i * range / bins);
                int end = h.Minimum + (int)Math.Ceiling((double)(i + 1) * range / bins) - 1;
                bar.ToolTip = string.Format(CultureInfo.InvariantCulture, "灰度 {0}–{1}：{2} 像素（{3:P2}）", start, end, display[i], (double)display[i] / h.SampleCount);
                Canvas.SetLeft(bar, 20 + i * 360.0 / bins); Canvas.SetTop(bar, 118 - height);
                DockHistogramChartS91.Children.Add(bar);
            }
            AddHistogramLabelS91(h.Minimum.ToString(CultureInfo.InvariantCulture), 20, 123);
            AddHistogramLabelS91(h.Maximum.ToString(CultureInfo.InvariantCulture), 350, 123);
            AddHistogramLabelS91("像素频数", 20, 0);
        }

        private void AddHistogramLabelS91(string text, double x, double y)
        {
            TextBlock label = new TextBlock { Text = text, FontSize = 10, Foreground = Brushes.SlateGray };
            Canvas.SetLeft(label, x); Canvas.SetTop(label, y); DockHistogramChartS91.Children.Add(label);
        }
    }
}
