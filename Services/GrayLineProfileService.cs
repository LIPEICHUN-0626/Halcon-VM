using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HalconDotNet;

namespace HalconWinFormsDemo.Services
{
    public sealed class GrayLineProfileResult
    {
        public ReadOnlyCollection<double> Values { get; private set; }
        public ReadOnlyCollection<double> Rows { get; private set; }
        public ReadOnlyCollection<double> Columns { get; private set; }
        public double Length { get; private set; }
        public double Mean { get; private set; }
        public double Deviation { get; private set; }
        public int MinimumIndex { get; private set; }
        public int MaximumIndex { get; private set; }

        internal GrayLineProfileResult(double[] values, double[] rows, double[] columns, double length)
        {
            Values = Array.AsReadOnly((double[])values.Clone());
            Rows = Array.AsReadOnly((double[])rows.Clone());
            Columns = Array.AsReadOnly((double[])columns.Clone());
            Length = length;
            Mean = values.Average();
            Deviation = Math.Sqrt(values.Select(value => (value - Mean) * (value - Mean)).Average());
            MinimumIndex = Array.IndexOf(values, values.Min());
            MaximumIndex = Array.IndexOf(values, values.Max());
        }
    }

    public static class GrayLineProfileService
    {
        public static GrayLineProfileResult Analyze(HImage image, double row1, double column1, double row2, double column2, int count)
        {
            if (image == null || !image.IsInitialized()) throw new InvalidOperationException("没有可用的输入图像。");
            if (count < 2 || count > 8192) throw new ArgumentOutOfRangeException("count", "剖面采样数必须为 2–8192。");
            if (new[] { row1, column1, row2, column2 }.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
                throw new ArgumentException("剖面端点必须是有限坐标。");
            double dr = row2 - row1, dc = column2 - column1;
            double length = Math.Sqrt(dr * dr + dc * dc);
            if (length <= 0 || double.IsInfinity(length)) throw new ArgumentException("剖面线段必须具有有效的非零长度。");
            HTuple channels = null, width = null, height = null, type = null;
            HImage gray = null;
            HObject converted = null;
            try
            {
                HOperatorSet.CountChannels(image, out channels);
                if (channels.I == 1) gray = image.CopyImage();
                else if (channels.I == 3)
                {
                    HOperatorSet.Rgb1ToGray(image, out converted);
                    gray = new HImage(converted);
                    converted = null;
                }
                else throw new InvalidOperationException("剖面支持单通道或 RGB 三通道图像。");
                HOperatorSet.GetImageSize(gray, out width, out height);
                HOperatorSet.GetImageType(gray, out type);
                if (!new[] { "byte", "int1", "int2", "uint2", "int4", "real" }.Contains(type.S))
                    throw new InvalidOperationException("剖面不支持该灰度类型：" + type.S);
                if (Math.Min(row1, row2) < 0 || Math.Max(row1, row2) > height.I - 1 ||
                    Math.Min(column1, column2) < 0 || Math.Max(column1, column2) > width.I - 1)
                    throw new InvalidOperationException("剖面端点越过输入图像像素边界。");
                double[] rows = new double[count], columns = new double[count];
                using (HRegion domain = gray.GetDomain())
                {
                    for (int i = 0; i < count; ++i)
                    {
                        double t = (double)i / (count - 1);
                        rows[i] = i == count - 1 ? row2 : row1 + t * dr;
                        columns[i] = i == count - 1 ? column2 : column1 + t * dc;
                        int top = (int)Math.Floor(rows[i]), bottom = (int)Math.Ceiling(rows[i]);
                        int left = (int)Math.Floor(columns[i]), right = (int)Math.Ceiling(columns[i]);
                        if (domain.TestRegionPoint(top, left) == 0 || domain.TestRegionPoint(top, right) == 0 ||
                            domain.TestRegionPoint(bottom, left) == 0 || domain.TestRegionPoint(bottom, right) == 0)
                            throw new InvalidOperationException("剖面第 " + (i + 1) + " 个采样点或双线性邻域超出图像有效域。");
                    }
                }
                using (HTuple rowTuple = new HTuple(rows))
                using (HTuple columnTuple = new HTuple(columns))
                using (HTuple sampled = gray.GetGrayvalInterpolated(rowTuple, columnTuple, "bilinear"))
                {
                    if (sampled.Length != count) throw new InvalidOperationException("HALCON 返回的采样数不一致。");
                    double[] values = new double[count];
                    for (int i = 0; i < count; ++i)
                    {
                        values[i] = sampled[i].D;
                        if (double.IsNaN(values[i]) || double.IsInfinity(values[i])) throw new InvalidOperationException("剖面包含非有限灰度值。");
                    }
                    return new GrayLineProfileResult(values, rows, columns, length);
                }
            }
            finally
            {
                if (converted != null) converted.Dispose();
                if (gray != null) gray.Dispose();
                foreach (HTuple tuple in new[] { channels, width, height, type }) if (tuple != null) tuple.Dispose();
            }
        }
    }
}
