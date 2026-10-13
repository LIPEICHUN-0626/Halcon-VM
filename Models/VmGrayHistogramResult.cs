using System;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    // Immutable, managed current-run display data. Never persisted in a recipe or
    // reused after runtime outputs are invalidated. Numerical ports stay Number.
    public sealed class VmGrayHistogramResult
    {
        private readonly long[] counts;
        public int Origin { get; private set; }
        public string PixelType { get; private set; }
        public long SampleCount { get; private set; }
        public int Minimum { get; private set; }
        public int Maximum { get; private set; }
        public int PeakGray { get; private set; }
        public long PeakCount { get; private set; }
        public int BinCount { get { return counts.Length; } }

        public VmGrayHistogramResult(long[] frequencies, int origin, string pixelType)
        {
            if (frequencies == null || frequencies.Length == 0 || frequencies.Any(n => n < 0))
                throw new ArgumentException("直方图频数无效。");
            counts = (long[])frequencies.Clone();
            Origin = origin;
            PixelType = pixelType;
            SampleCount = counts.Sum();
            if (SampleCount <= 0) throw new InvalidOperationException("统计有效域为空，没有可用像素。");
            Minimum = Array.FindIndex(counts, n => n > 0) + origin;
            Maximum = Array.FindLastIndex(counts, n => n > 0) + origin;
            PeakCount = counts.Max();
            // Ties deliberately choose the lowest gray value, not an unstable mode.
            PeakGray = Array.IndexOf(counts, PeakCount) + origin;
        }

        public long GetCount(int index) { return counts[index]; }

        public double Quantile(double percent)
        {
            if (double.IsNaN(percent) || double.IsInfinity(percent) || percent < 0 || percent > 100)
                throw new ArgumentOutOfRangeException("percent");
            long rank = Math.Max(1, (long)Math.Ceiling(SampleCount * (percent / 100.0)));
            long cumulative = 0;
            for (int i = 0; i < counts.Length; ++i)
            {
                cumulative += counts[i];
                if (cumulative >= rank) return Origin + i;
            }
            return Maximum;
        }
    }
}
