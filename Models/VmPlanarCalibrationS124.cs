using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmPlanarSampleS124
    {
        [DataMember(Order = 1)] public double PixelRow { get; set; }
        [DataMember(Order = 2)] public double PixelColumn { get; set; }
        [DataMember(Order = 3)] public double WorldX { get; set; }
        [DataMember(Order = 4)] public double WorldY { get; set; }
        [DataMember(Order = 5)] public string ImageContextId { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public string SourceRoiId { get; set; }

        public VmPlanarSampleS124 Clone() { return (VmPlanarSampleS124)MemberwiseClone(); }
        public string ToDisplayString(int index, double? residual)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}. px ({1:0.##}, {2:0.##}) → 世界 ({3:0.###}, {4:0.###}){5}",
                index + 1, PixelRow, PixelColumn, WorldX, WorldY,
                residual.HasValue ? " · 误差 " + residual.Value.ToString("0.####", CultureInfo.InvariantCulture) : string.Empty);
        }
    }

    [DataContract]
    public sealed class VmWorldPoint2DS124
    {
        public const string WorldContext = "World2D";
        [DataMember(Order = 1)] public double X { get; set; }
        [DataMember(Order = 2)] public double Y { get; set; }
        [DataMember(Order = 3)] public string Unit { get; set; }
        [DataMember(Order = 4)] public string CoordinateContext { get; set; } = WorldContext;
        [DataMember(Order = 5)] public string CoordinateContextId { get; set; }
        [DataMember(Order = 6)] public string SourceImageContextId { get; set; }
        public VmWorldPoint2DS124 Clone() { return (VmWorldPoint2DS124)MemberwiseClone(); }
        public string GetValidationError()
        {
            if (double.IsNaN(X) || double.IsInfinity(X) || double.IsNaN(Y) || double.IsInfinity(Y)) return "世界坐标必须有限。";
            if (CoordinateContext != WorldContext || string.IsNullOrWhiteSpace(CoordinateContextId) || string.IsNullOrWhiteSpace(SourceImageContextId))
                return "世界坐标缺少独立标定与图像来源身份。";
            if (string.IsNullOrWhiteSpace(Unit)) return "世界坐标缺少单位。";
            return string.Empty;
        }
        public string ToDisplayString() { return string.Format(CultureInfo.InvariantCulture, "X={0:0.###}, Y={1:0.###} {2} · {3}", X, Y, Unit, CoordinateContextId); }
        public override string ToString() { return ToDisplayString(); }
    }

    public sealed class VmPlanarFitS124
    {
        public double[] Matrix { get; set; }
        public double[] Residuals { get; set; }
        public double RmsResidual { get; set; }
        public double MaxResidual { get; set; }

        public VmWorldPoint2DS124 Transform(VmPoint2D point, string imageContextId, string toolId, string unit)
        {
            if (point == null || !string.IsNullOrWhiteSpace(point.GetValidationError()) ||
                !string.Equals(point.CoordinateContextId, imageContextId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("待转换点不是当前标定图像链的有效 ImagePixel 点。");
            HTuple x = null, y = null, matrix = null;
            try
            {
                matrix = new HTuple(Matrix);
                HOperatorSet.AffineTransPoint2d(matrix, point.Column, point.Row, out x, out y);
                byte[] matrixBytes = new byte[Matrix.Length * sizeof(double)];
                for (int i = 0; i < Matrix.Length; i++)
                    Buffer.BlockCopy(BitConverter.GetBytes(Matrix[i]), 0, matrixBytes, i * sizeof(double), sizeof(double));
                string revision;
                using (SHA256 sha = SHA256.Create()) revision = BitConverter.ToString(sha.ComputeHash(matrixBytes), 0, 6).Replace("-", string.Empty);
                var result = new VmWorldPoint2DS124 { X = x[0].D, Y = y[0].D, Unit = unit,
                    CoordinateContextId = toolId + ":" + revision + "@" + imageContextId, SourceImageContextId = imageContextId };
                string error = result.GetValidationError();
                if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
                return result;
            }
            finally { if (x != null) x.Dispose(); if (y != null) y.Dispose(); if (matrix != null) matrix.Dispose(); }
        }
    }

    public static class VmPlanarCalibrationS124
    {
        public static VmPlanarFitS124 Fit(IList<VmPlanarSampleS124> samples, string imageContextId)
        {
            if (samples == null || samples.Count < 3 || samples.Count > 64)
                throw new InvalidOperationException("平面标定需要 3–64 个像素/世界对应点。");
            if (string.IsNullOrWhiteSpace(imageContextId) || imageContextId.StartsWith("Pending:", StringComparison.Ordinal))
                throw new InvalidOperationException("标定图像来源未确定；请先连接真实 Image 输入。");
            foreach (var sample in samples)
            {
                if (sample == null || !Finite(sample.PixelRow) || !Finite(sample.PixelColumn) || !Finite(sample.WorldX) || !Finite(sample.WorldY) ||
                    !string.Equals(sample.ImageContextId, imageContextId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("标定点包含非有限值或跨图像链来源；请重新从当前图像选点。");
            }
            for (int i = 0; i < samples.Count; i++)
                for (int j = i + 1; j < samples.Count; j++)
                    if (Math.Abs(samples[i].PixelRow - samples[j].PixelRow) + Math.Abs(samples[i].PixelColumn - samples[j].PixelColumn) < 1e-3 ||
                        Math.Abs(samples[i].WorldX - samples[j].WorldX) + Math.Abs(samples[i].WorldY - samples[j].WorldY) < 1e-9)
                        throw new InvalidOperationException("标定存在重复的像素点或世界点，请移除重复对应关系。");
            if (!NonCollinear(samples, false) || !NonCollinear(samples, true))
                throw new InvalidOperationException("像素点或世界点重复/近共线，不能拟合稳定平面变换。请分散选取至少三点。");

            HTuple pixelX = null, pixelY = null, worldX = null, worldY = null, matrix = null, inverse = null, fittedX = null, fittedY = null;
            try
            {
                pixelX = new HTuple(samples.Select(p => p.PixelColumn).ToArray());
                pixelY = new HTuple(samples.Select(p => p.PixelRow).ToArray());
                worldX = new HTuple(samples.Select(p => p.WorldX).ToArray());
                worldY = new HTuple(samples.Select(p => p.WorldY).ToArray());
                HOperatorSet.VectorToHomMat2d(pixelX, pixelY, worldX, worldY, out matrix);
                if (matrix == null || matrix.Length != 6 || Enumerable.Range(0, matrix.Length).Any(i => !Finite(matrix[i].D)))
                    throw new InvalidOperationException("HALCON 返回无效平面矩阵。");
                HOperatorSet.HomMat2dInvert(matrix, out inverse);
                if (inverse == null || inverse.Length != 6 || Enumerable.Range(0, inverse.Length).Any(i => !Finite(inverse[i].D)))
                    throw new InvalidOperationException("平面矩阵不可逆，世界坐标定义退化。");
                double directScale = Math.Sqrt(matrix[0].D * matrix[0].D + matrix[1].D * matrix[1].D + matrix[3].D * matrix[3].D + matrix[4].D * matrix[4].D);
                double inverseScale = Math.Sqrt(inverse[0].D * inverse[0].D + inverse[1].D * inverse[1].D + inverse[3].D * inverse[3].D + inverse[4].D * inverse[4].D);
                if (!Finite(directScale * inverseScale) || directScale * inverseScale > 1e8)
                    throw new InvalidOperationException("平面变换接近奇异，像素/世界点分布需更均匀。");
                HOperatorSet.AffineTransPoint2d(matrix, pixelX, pixelY, out fittedX, out fittedY);
                double[] residuals = new double[samples.Count];
                for (int i = 0; i < samples.Count; i++)
                {
                    residuals[i] = Math.Sqrt(Math.Pow(fittedX[i].D - samples[i].WorldX, 2) + Math.Pow(fittedY[i].D - samples[i].WorldY, 2));
                    if (!Finite(residuals[i])) throw new InvalidOperationException("标定残差非有限，坐标尺度可能无效。");
                }
                return new VmPlanarFitS124 { Matrix = Enumerable.Range(0, matrix.Length).Select(i => matrix[i].D).ToArray(),
                    Residuals = residuals, MaxResidual = residuals.Max(), RmsResidual = Math.Sqrt(residuals.Sum(r => r * r) / residuals.Length) };
            }
            catch (HalconException ex) { throw new InvalidOperationException("HALCON 平面标定失败：" + ex.Message, ex); }
            finally
            {
                if (fittedY != null) fittedY.Dispose(); if (fittedX != null) fittedX.Dispose();
                if (inverse != null) inverse.Dispose(); if (matrix != null) matrix.Dispose();
                if (worldY != null) worldY.Dispose(); if (worldX != null) worldX.Dispose();
                if (pixelY != null) pixelY.Dispose(); if (pixelX != null) pixelX.Dispose();
            }
        }

        private static bool NonCollinear(IList<VmPlanarSampleS124> points, bool world)
        {
            Func<VmPlanarSampleS124, double> x = p => world ? p.WorldX : p.PixelColumn;
            Func<VmPlanarSampleS124, double> y = p => world ? p.WorldY : p.PixelRow;
            double span = 0;
            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    span = Math.Max(span, Math.Pow(x(points[j]) - x(points[i]), 2) + Math.Pow(y(points[j]) - y(points[i]), 2));
            if (!Finite(span) || span <= (world ? 1e-18 : 1e-4)) return false;
            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    for (int k = j + 1; k < points.Count; k++)
                    {
                        double cross = (x(points[j]) - x(points[i])) * (y(points[k]) - y(points[i])) -
                            (y(points[j]) - y(points[i])) * (x(points[k]) - x(points[i]));
                        if (Math.Abs(cross) > span * 1e-6) return true;
                    }
            return false;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
