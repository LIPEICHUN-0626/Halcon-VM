using System;
using System.Globalization;
using System.Runtime.Serialization;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmColorInspectionParametersS129
    {
        [DataMember(Order = 1)] public double HueDegrees { get; set; } = 0;
        [DataMember(Order = 2)] public double HueToleranceDegrees { get; set; } = 15;
        [DataMember(Order = 3)] public double SaturationMinimum { get; set; } = 20;
        [DataMember(Order = 4)] public double SaturationMaximum { get; set; } = 100;
        [DataMember(Order = 5)] public double ValueMinimum { get; set; } = 10;
        [DataMember(Order = 6)] public double ValueMaximum { get; set; } = 100;

        [OnDeserializing]
        private void InitializeDefaults(StreamingContext context)
        {
            HueDegrees = 0; HueToleranceDegrees = 15;
            SaturationMinimum = 20; SaturationMaximum = 100;
            ValueMinimum = 10; ValueMaximum = 100;
        }

        public VmColorInspectionParametersS129 Clone() { return (VmColorInspectionParametersS129)MemberwiseClone(); }
        public string Validate()
        {
            if (!InRange(HueDegrees, 0, 360) || HueDegrees == 360 || !InRange(HueToleranceDegrees, 0, 180))
                return "目标色相为 0–小于 360°，色相容差为 0–180°。";
            if (!InRange(SaturationMinimum, 0, 100) || !InRange(SaturationMaximum, 0, 100) || SaturationMinimum > SaturationMaximum ||
                !InRange(ValueMinimum, 0, 100) || !InRange(ValueMaximum, 0, 100) || ValueMinimum > ValueMaximum)
                return "饱和度/亮度范围必须为 0–100%，且下限不超过上限。";
            return string.Empty;
        }
        public string Summary()
        {
            return string.Format(CultureInfo.InvariantCulture, "H={0:0.###}°±{1:0.###}°; S={2:0.###}–{3:0.###}%; V={4:0.###}–{5:0.###}%",
                HueDegrees, HueToleranceDegrees, SaturationMinimum, SaturationMaximum, ValueMinimum, ValueMaximum);
        }
        private static bool InRange(double value, double minimum, double maximum)
        { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= minimum && value <= maximum; }
    }

    public sealed class VmHsvImagesS129 : IDisposable
    {
        public HObject Hue { get; private set; }
        public HObject Saturation { get; private set; }
        public HObject Value { get; private set; }
        public HObject Domain { get; private set; }
        public double PixelCount { get; private set; }

        public static double GetRgbScale(HImage input)
        {
            HTuple channels = null, types = null;
            try
            {
                HOperatorSet.CountChannels(input, out channels);
                if (channels.I != 3) throw new InvalidOperationException("颜色检测需要 3 通道 RGB 图像，请重选彩色图像来源。");
                HOperatorSet.GetImageType(input, out types);
                if (types.Length < 1) throw new InvalidOperationException("RGB 图像类型为空。");
                string type = types[0].S;
                for (int i = 0; i < types.Length; i++)
                    if (types[i].S != type) throw new InvalidOperationException("RGB 三通道必须使用相同图像类型。");
                if (type == "byte") return 1.0 / 255;
                if (type == "uint2") return 1.0 / 65535;
                if (type == "real") return 1;
                throw new InvalidOperationException("颜色检测支持 byte、uint2 或 0–1 的 real RGB；不会自动推测其他类型幅值。");
            }
            finally { if (channels != null) channels.Dispose(); if (types != null) types.Dispose(); }
        }

        public static VmHsvImagesS129 Create(HImage input, HRegion roi)
        {
            var result = new VmHsvImagesS129();
            HObject domain = null, effective = null, reduced = null, converted = null, normalized = null;
            HObject red = null, green = null, blue = null, hue = null, saturation = null, value = null;
            HTuple area = null, row = null, column = null;
            try
            {
                double scale = GetRgbScale(input);
                HOperatorSet.GetDomain(input, out domain);
                if (roi != null) HOperatorSet.Intersection(domain, roi, out effective);
                else HOperatorSet.CopyObj(domain, out effective, 1, -1);
                HOperatorSet.AreaCenter(effective, out area, out row, out column);
                double count = 0;
                for (int i = 0; i < area.Length; i++) count += area[i].D;
                if (count <= 0 || double.IsNaN(count) || double.IsInfinity(count)) throw new InvalidOperationException("颜色检测的图像有效域/ROI/屏蔽交集为空。");
                HOperatorSet.ReduceDomain(input, effective, out reduced);
                HOperatorSet.ConvertImageType(reduced, out converted, "real");
                HOperatorSet.ScaleImage(converted, out normalized, scale, 0);
                HOperatorSet.Decompose3(normalized, out red, out green, out blue);
                ValidateNormalizedChannel(red, count); ValidateNormalizedChannel(green, count); ValidateNormalizedChannel(blue, count);
                HOperatorSet.TransFromRgb(red, green, blue, out hue, out saturation, out value, "hsv");
                result.Hue = hue; result.Saturation = saturation; result.Value = value; result.Domain = effective; result.PixelCount = count;
                hue = saturation = value = effective = null;
                return result;
            }
            catch { result.Dispose(); throw; }
            finally
            {
                Dispose(domain); Dispose(effective); Dispose(reduced); Dispose(converted); Dispose(normalized);
                Dispose(red); Dispose(green); Dispose(blue); Dispose(hue); Dispose(saturation); Dispose(value);
                if (area != null) area.Dispose(); if (row != null) row.Dispose(); if (column != null) column.Dispose();
            }
        }

        private static void ValidateNormalizedChannel(HObject image, double expectedCount)
        {
            HObject valid = null;
            HTuple area = null, row = null, column = null;
            try
            {
                HOperatorSet.Threshold(image, out valid, 0, 1);
                HOperatorSet.AreaCenter(valid, out area, out row, out column);
                double validCount = 0;
                for (int i = 0; i < area.Length; i++) validCount += area[i].D;
                if (validCount != expectedCount)
                    throw new InvalidOperationException("RGB 有效域含无效值，或 real 幅值不在 0–1；请先显式转换幅值。");
            }
            finally { Dispose(valid); if (area != null) area.Dispose(); if (row != null) row.Dispose(); if (column != null) column.Dispose(); }
        }

        public double[] ReadSample(HImage input, int row, int column)
        {
            HTuple valid = null, rgb = null, h = null, s = null, v = null;
            try
            {
                HOperatorSet.TestRegionPoint(Domain, row, column, out valid);
                if (valid.Length != 1 || valid.I != 1) throw new InvalidOperationException("取样点不在图像/ROI/屏蔽的有效域内。");
                HOperatorSet.GetGrayval(input, row, column, out rgb);
                HOperatorSet.GetGrayval(Hue, row, column, out h);
                HOperatorSet.GetGrayval(Saturation, row, column, out s);
                HOperatorSet.GetGrayval(Value, row, column, out v);
                double scale = GetRgbScale(input);
                return new[] { h.D * 180 / Math.PI, s.D * 100, v.D * 100, rgb[0].D * scale, rgb[1].D * scale, rgb[2].D * scale };
            }
            finally
            { if (valid != null) valid.Dispose(); if (rgb != null) rgb.Dispose(); if (h != null) h.Dispose(); if (s != null) s.Dispose(); if (v != null) v.Dispose(); }
        }

        public void Dispose() { Dispose(Hue); Dispose(Saturation); Dispose(Value); Dispose(Domain); Hue = Saturation = Value = Domain = null; }
        private static void Dispose(HObject item) { if (item != null) item.Dispose(); }
    }
}
