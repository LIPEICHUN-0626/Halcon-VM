using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class ImageEdgeLineSampleS143
    {
        public int Index;
        public double CenterRow, CenterColumn;
        public double? Row, Column, Amplitude, Residual;
        public bool Kept, QualityKnown;
        public string Reason;
    }

    public sealed class ImageEdgeLineResultS143 : IDisposable
    {
        public List<ImageEdgeLineSampleS143> Samples = new List<ImageEdgeLineSampleS143>();
        public VmLine2D Line;
        public HObject Graphics;
        public double? Coverage;
        public double? MeanResidual, MaxResidual;
        public string Reason;
        public void Dispose() { if (Graphics != null) { Graphics.Dispose(); Graphics = null; } }
    }

    public static class ImageEdgeLineServiceS143
    {
        // MeasurePos ignores reduced domains. Every rectangle PLUS its bilinear
        // margin is explicitly inside the source domain before invoking the operator.
        public static ImageEdgeLineResultS143 Run(HImage source, VmImageEdgeLineParametersS143 p, string context,
            HalconImageService fitting)
        {
            string error = p.Validate(); if (error.Length > 0) throw new InvalidOperationException(error);
            if (source == null || string.IsNullOrWhiteSpace(context) || fitting == null) throw new InvalidOperationException("明确源图/坐标/拟合服务缺失。");
            var result = new ImageEdgeLineResultS143();
            HImage gray = null; HObject domain = null;
            HTuple count = null, width = null, height = null, channels = null, type = null;
            try
            {
                HOperatorSet.CountObj(source, out count); HOperatorSet.GetImageSize(source, out width, out height);
                HOperatorSet.CountChannels(source, out channels); HOperatorSet.GetImageType(source, out type);
                if (count.I != 1 || width.I > 16384 || height.I > 16384 || (long)width.I * height.I > 16000000 || width.I < 1 || height.I < 1)
                    throw new InvalidOperationException("找线仅支持单幅图像，最多 1600 万像素 / 边长 16384。");
                if (channels.I != 1 && channels.I != 3 || type.Length != 1 || !new[] { "byte", "uint2", "real" }.Contains(type.S) || channels.I == 3 && type.S != "byte")
                    throw new InvalidOperationException("找线支持 byte/uint2/real 单通道或 byte RGB；不缩放高位灰度。RGB 明确使用 rgb1_to_gray。");
                HOperatorSet.GetDomain(source, out domain);
                if (channels.I == 1) gray = source.CopyImage();
                else { HObject converted; HOperatorSet.Rgb1ToGray(source, out converted); using (converted) gray = new HImage(converted); }
                double dr = p.EndRow - p.StartRow, dc = p.EndColumn - p.StartColumn, length = Math.Sqrt(dr * dr + dc * dc);
                double phi = Math.Atan2(-dr, dc) - Math.PI / 2;
                phi = Math.Atan2(Math.Sin(phi), Math.Cos(phi));
                var centers = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < p.SampleCount; index++)
                {
                    double ratio = (double)index / (p.SampleCount - 1);
                    var sample = new ImageEdgeLineSampleS143 { Index = index + 1,
                        CenterRow = Math.Round(p.StartRow + dr * ratio, MidpointRounding.AwayFromZero),
                        CenterColumn = Math.Round(p.StartColumn + dc * ratio, MidpointRounding.AwayFromZero) };
                    result.Samples.Add(sample);
                    string centerKey = sample.CenterRow.ToString("R", CultureInfo.InvariantCulture) + ":" + sample.CenterColumn.ToString("R", CultureInfo.InvariantCulture);
                    if (!centers.Add(centerKey)) { sample.Reason = "采样中心因像素舍入重复；不重复测量凑有效点数"; continue; }
                    if (!InsideDomain(domain, sample.CenterRow, sample.CenterColumn, phi, p.SearchHalfWidth + 2, p.AveragingHalfWidth + 2))
                    { sample.Reason = "搜索矩形或双线性边界余量超出真实有效域"; continue; }
                    HTuple handle = null, rows = null, columns = null, amplitudes = null, distances = null;
                    try
                    {
                        HOperatorSet.GenMeasureRectangle2(sample.CenterRow, sample.CenterColumn, phi, p.SearchHalfWidth, p.AveragingHalfWidth, width.I, height.I, "bilinear", out handle);
                        HOperatorSet.MeasurePos(gray, handle, p.Sigma, p.Threshold, p.Transition, p.Selection == "strongest" ? "all" : p.Selection,
                            out rows, out columns, out amplitudes, out distances);
                        if (rows.Length == 0) { sample.Reason = "未采到满足阈值/极性的边缘"; continue; }
                        if (rows.Length > 4096 || columns.Length != rows.Length || amplitudes.Length != rows.Length) throw new InvalidOperationException("边缘候选数组超限或不同序。");
                        int selected = 0;
                        if (p.Selection == "strongest")
                            for (int candidate = 1; candidate < rows.Length; candidate++)
                                if (Math.Abs(amplitudes[candidate].D) > Math.Abs(amplitudes[selected].D)) selected = candidate; // Stable first tie.
                        double row = rows[selected].D, column = columns[selected].D, amplitude = amplitudes[selected].D;
                        if (!VmImageEdgeLineParametersS143.Finite(row) || !VmImageEdgeLineParametersS143.Finite(column) || !VmImageEdgeLineParametersS143.Finite(amplitude))
                            throw new InvalidOperationException("真实边缘输出含非有限值。");
                        sample.Row = row; sample.Column = column; sample.Amplitude = amplitude; sample.Reason = "已采到，待质量拟合";
                    }
                    finally
                    {
                        try { if (handle != null && handle.Length > 0) HOperatorSet.CloseMeasure(handle); }
                        finally { DisposeTuples(handle, rows, columns, amplitudes, distances); }
                    }
                }
                var acquired = result.Samples.Where(sample => sample.Row.HasValue).ToList();
                if (acquired.Count < p.MinimumValidCount) result.Reason = "采到点不足最少有效点要求。";
                else
                {
                    VmLine2D preliminary = Fit(acquired, p, context, fitting);
                    foreach (var sample in acquired)
                    {
                        sample.Residual = Residual(sample, preliminary);
                        sample.QualityKnown = true;
                        sample.Kept = sample.Residual.Value <= p.MaximumResidual;
                        sample.Reason = sample.Kept ? "质量保留" : "超过残差门槛，质量拒绝";
                    }
                    var kept = acquired.Where(sample => sample.Kept).ToList();
                    if (kept.Count < p.MinimumValidCount) result.Reason = "残差筛选后有效点不足。";
                    else
                    {
                        VmLine2D final = Fit(kept, p, context, fitting);
                        // The second fitted line is checked against every point advertised as kept;
                        // no endless re-fit loop or invented robust-weight membership.
                        foreach (var sample in acquired) sample.Residual = Residual(sample, final);
                        if (kept.Any(sample => sample.Residual.Value > p.MaximumResidual))
                        {
                            foreach (var sample in kept.Where(item => item.Residual.Value > p.MaximumResidual))
                            { sample.Kept = false; sample.Reason = "最终残差超门槛，质量拒绝"; }
                            result.Reason = "最终拟合仍有保留点超出残差门槛。";
                        }
                        else
                        {
                            double[] projections = kept.Select(sample => ((sample.Row.Value - p.StartRow) * dr + (sample.Column.Value - p.StartColumn) * dc) / (length * length)).ToArray();
                            result.Coverage = Math.Min(1, Math.Max(0, projections.Max() - projections.Min()));
                            if (result.Coverage.Value < p.MinimumCoverage) result.Reason = "有效点集中短段，基线覆盖不足。";
                            else
                            {
                                if ((final.EndRow - final.StartRow) * dr + (final.EndColumn - final.StartColumn) * dc < 0)
                                { double r = final.StartRow, c = final.StartColumn; final.StartRow = final.EndRow; final.StartColumn = final.EndColumn; final.EndRow = r; final.EndColumn = c; }
                                if (final.Length < 3) result.Reason = "真实拟合线跨度不足 3 px。";
                                else { result.Line = final; result.MeanResidual = kept.Average(sample => sample.Residual.Value); result.MaxResidual = kept.Max(sample => sample.Residual.Value); result.Reason = "找到有效像素直线；产品容差需另接判定。"; }
                            }
                        }
                    }
                }
                BuildGraphics(result);
                return result;
            }
            catch { result.Dispose(); throw; }
            finally { if (gray != null) gray.Dispose(); if (domain != null) domain.Dispose(); DisposeTuples(count, width, height, channels, type); }
        }

        private static bool InsideDomain(HObject domain, double row, double column, double phi, double length1, double length2)
        {
            HObject rectangle = null, missing = null; HTuple area = null, r = null, c = null;
            try
            {
                HOperatorSet.GenRectangle2(out rectangle, row, column, phi, length1, length2);
                HOperatorSet.Difference(rectangle, domain, out missing);
                HOperatorSet.AreaCenter(missing, out area, out r, out c);
                return area.Length == 1 && area.D == 0;
            }
            finally { if (rectangle != null) rectangle.Dispose(); if (missing != null) missing.Dispose(); DisposeTuples(area, r, c); }
        }

        private static VmLine2D Fit(List<ImageEdgeLineSampleS143> samples, VmImageEdgeLineParametersS143 p, string context, HalconImageService fitting)
        {
            HObject contour = null, line = null; HTuple rb = null, cb = null, re = null, ce = null, nr = null, nc = null, distance = null;
            using (var rows = new HTuple(samples.Select(sample => sample.Row.Value).ToArray()))
            using (var columns = new HTuple(samples.Select(sample => sample.Column.Value).ToArray()))
            try
            {
                HOperatorSet.GenContourPolygonXld(out contour, rows, columns);
                line = fitting.FitXldLines(contour, p.FitAlgorithm, -1, 0, p.Iterations, 2, out rb, out cb, out re, out ce, out nr, out nc, out distance);
                if (rb.Length != 1 || cb.Length != 1 || re.Length != 1 || ce.Length != 1) throw new InvalidOperationException("HALCON 未输出唯一真实拟合线。");
                var value = VmLine2D.CreateImagePixel(rb.D, cb.D, re.D, ce.D, context);
                string error = value.GetValidationError(); if (error.Length > 0) throw new InvalidOperationException(error);
                return value;
            }
            finally { if (line != null) line.Dispose(); if (contour != null) contour.Dispose(); DisposeTuples(rb, cb, re, ce, nr, nc, distance); }
        }

        private static double Residual(ImageEdgeLineSampleS143 sample, VmLine2D line)
        {
            double value = Math.Abs((line.EndColumn - line.StartColumn) * (sample.Row.Value - line.StartRow) -
                (line.EndRow - line.StartRow) * (sample.Column.Value - line.StartColumn)) / line.Length;
            if (!VmImageEdgeLineParametersS143.Finite(value)) throw new InvalidOperationException("真实点线残差非有限。");
            return value;
        }

        private static void BuildGraphics(ImageEdgeLineResultS143 result)
        {
            HOperatorSet.GenEmptyObj(out result.Graphics);
            var points = result.Samples.Where(sample => sample.Row.HasValue).ToList();
            if (points.Count > 0)
                using (var rows = new HTuple(points.Select(sample => sample.Row.Value).ToArray()))
                using (var columns = new HTuple(points.Select(sample => sample.Column.Value).ToArray()))
                { HObject crosses; HOperatorSet.GenCrossContourXld(out crosses, rows, columns, 4, 0); using (crosses) Append(result, crosses); }
            if (result.Line != null)
                using (var rows = new HTuple(new[] { result.Line.StartRow, result.Line.EndRow }))
                using (var columns = new HTuple(new[] { result.Line.StartColumn, result.Line.EndColumn }))
                { HObject line; HOperatorSet.GenContourPolygonXld(out line, rows, columns); using (line) Append(result, line); }
        }
        private static void Append(ImageEdgeLineResultS143 result, HObject item)
        { HObject combined; HOperatorSet.ConcatObj(result.Graphics, item, out combined); result.Graphics.Dispose(); result.Graphics = combined; }
        private static void DisposeTuples(params HTuple[] tuples) { foreach (var tuple in tuples) if (tuple != null) tuple.Dispose(); }
    }
}
