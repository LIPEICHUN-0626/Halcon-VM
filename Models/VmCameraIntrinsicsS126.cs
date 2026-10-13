using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmCameraIntrinsicsS126
    {
        public const string Model = "area_scan_division";
        [DataMember(Order = 1)] public double Focus { get; set; }
        [DataMember(Order = 2)] public double Kappa { get; set; }
        [DataMember(Order = 3)] public double Sx { get; set; }
        [DataMember(Order = 4)] public double Sy { get; set; }
        [DataMember(Order = 5)] public double Cx { get; set; }
        [DataMember(Order = 6)] public double Cy { get; set; }
        [DataMember(Order = 7)] public int Width { get; set; }
        [DataMember(Order = 8)] public int Height { get; set; }
        [DataMember(Order = 9, EmitDefaultValue = false)] public string ImportedFileName { get; set; }
        [DataMember(Order = 10, EmitDefaultValue = false)] public int CalibrationSampleCount { get; set; }
        [DataMember(Order = 11, EmitDefaultValue = false)] public double CalibrationRmsPx { get; set; }
        [DataMember(Order = 12, EmitDefaultValue = false)] public string CalibrationBoardName { get; set; }

        public VmCameraIntrinsicsS126 Clone() { return (VmCameraIntrinsicsS126)MemberwiseClone(); }
        public string Validate()
        {
            double[] values = { Focus, Kappa, Sx, Sy, Cx, Cy };
            if (values.Any(value => double.IsNaN(value) || double.IsInfinity(value))) return "相机内参不能含 NaN 或无穷值。";
            if (Focus <= 0 || Sx <= 0 || Sy <= 0) return "焦距与像元宽/高必须为正数；单位为米和米/像素。";
            if (Width < 2 || Height < 2 || Width > 16384 || Height > 16384 || (long)Width * Height > 67108864L)
                return "内参图像宽高必须为 2–16384，且总像素数不超过 67108864。";
            if (Cx < -Width || Cx > 2.0 * Width || Cy < -Height || Cy > 2.0 * Height)
                return "主点 Cx/Cy 远离标定图像范围，请核对内参与图像尺寸。";
            if (ImportedFileName != null && ImportedFileName.Length > 256) return "相机参数来源名称过长。";
            if (CalibrationSampleCount < 0 || CalibrationSampleCount > 64 ||
                double.IsNaN(CalibrationRmsPx) || double.IsInfinity(CalibrationRmsPx) || CalibrationRmsPx < 0)
                return "标定样本数或重投影误差无效。";
            return string.Empty;
        }

        public string ToEditText()
        {
            return string.Join(", ", new[] { Focus, Kappa, Sx, Sy, Cx, Cy }
                .Select(value => value.ToString("R", CultureInfo.InvariantCulture))) +
                ", " + Width.ToString(CultureInfo.InvariantCulture) + ", " + Height.ToString(CultureInfo.InvariantCulture);
        }

        public string ToSummary()
        {
            return Model + " · " + Width.ToString(CultureInfo.InvariantCulture) + "×" + Height.ToString(CultureInfo.InvariantCulture) +
                " · Kappa " + Kappa.ToString("G4", CultureInfo.InvariantCulture) +
                (string.IsNullOrWhiteSpace(ImportedFileName) ? " · 手动参数" : " · " + ImportedFileName) +
                (CalibrationSampleCount > 0 ? " · " + CalibrationSampleCount.ToString(CultureInfo.InvariantCulture) +
                    " 图/RMS " + CalibrationRmsPx.ToString("0.###", CultureInfo.InvariantCulture) + " px" : string.Empty);
        }

        public static VmCameraIntrinsicsS126 Parse(string text, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("请导入相机参数 .dat，或输入 8 个内参数值。");
            string[] parts = text.Replace("\r", " ").Replace("\n", " ").Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 9 && string.Equals(parts[0], Model, StringComparison.OrdinalIgnoreCase)) parts = parts.Skip(1).ToArray();
            if (parts.Length != 8) throw new InvalidOperationException("面阵除法模型需要 Focus, Kappa, Sx, Sy, Cx, Cy, Width, Height 共 8 个值。" );
            double[] values = new double[8];
            for (int i = 0; i < values.Length; i++)
                if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                    throw new InvalidOperationException("相机内参第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 项不是有效数值。" );
            if (values[6] != Math.Truncate(values[6]) || values[7] != Math.Truncate(values[7]) ||
                values[6] < int.MinValue || values[6] > int.MaxValue || values[7] < int.MinValue || values[7] > int.MaxValue)
                throw new InvalidOperationException("相机图像 Width/Height 必须是整数。" );
            var result = new VmCameraIntrinsicsS126 { Focus = values[0], Kappa = values[1], Sx = values[2], Sy = values[3],
                Cx = values[4], Cy = values[5], Width = (int)values[6], Height = (int)values[7], ImportedFileName = sourceName };
            string error = result.Validate();
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            return result;
        }

        public static VmCameraIntrinsicsS126 FromHalconTuple(HTuple tuple, string sourceName)
        {
            if (tuple == null || tuple.Length != 9 || !string.Equals(tuple[0].S, Model, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("当前只支持 HALCON area_scan_division 面阵除法模型的 .dat 内参。" );
            return Parse(string.Join(",", Enumerable.Range(1, 8).Select(index => tuple[index].D.ToString("R", CultureInfo.InvariantCulture))), sourceName);
        }

        public HTuple CreateHalconTuple()
        {
            string error = Validate();
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            return new HTuple(new object[] { Model, Focus, Kappa, Sx, Sy, Cx, Cy, Width, Height });
        }
    }
}
