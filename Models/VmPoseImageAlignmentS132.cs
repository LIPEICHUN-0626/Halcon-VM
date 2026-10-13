using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmPoseImageAlignmentParametersS132
    {
        [DataMember(Order = 1)] public bool HasReference { get; set; }
        [DataMember(Order = 2)] public double ReferenceRow { get; set; }
        [DataMember(Order = 3)] public double ReferenceColumn { get; set; }
        [DataMember(Order = 4)] public double ReferenceAngleDegrees { get; set; }
        [DataMember(Order = 5)] public int ReferenceWidth { get; set; }
        [DataMember(Order = 6)] public int ReferenceHeight { get; set; }
        [DataMember(Order = 7)] public string ReferenceSourceSignature { get; set; }
        [DataMember(Order = 8)] public string ReferenceContextId { get; set; }
        [DataMember(Order = 9)] public string ReferenceVersion { get; set; }
        [DataMember(Order = 10)] public string Interpolation { get; set; } = "bilinear";
        [DataMember(Order = 11)] public string OutputSizeMode { get; set; } = "Reference";
        [DataMember(Order = 12)] public int OutputWidth { get; set; }
        [DataMember(Order = 13)] public int OutputHeight { get; set; }

        [OnDeserializing]
        private void Defaults(StreamingContext context) { Interpolation = "bilinear"; OutputSizeMode = "Reference"; }

        public VmPoseImageAlignmentParametersS132 Clone() { return (VmPoseImageAlignmentParametersS132)MemberwiseClone(); }

        public string Validate()
        {
            if (Interpolation != "bilinear" && Interpolation != "nearest_neighbor") return "图像对齐插值只支持双线性或最近邻。";
            if (OutputSizeMode != "Reference" && OutputSizeMode != "Input") return "对齐输出范围只能选择参考图尺寸或当前输入尺寸。";
            if (OutputWidth < 0 || OutputHeight < 0 ||
                (OutputWidth > 0 && OutputHeight > 0 && (long)OutputWidth * OutputHeight > VmPoseImageAlignmentS132.MaximumImagePixels))
                return "对齐输出尺寸无效或超过 16777216 像素。";
            if (!HasReference) return string.Empty;
            Guid version;
            if (!Guid.TryParse(ReferenceVersion, out version) || version == Guid.Empty ||
                string.IsNullOrWhiteSpace(ReferenceSourceSignature) || ReferenceSourceSignature.Length > 16384 ||
                string.IsNullOrWhiteSpace(ReferenceContextId) || ReferenceContextId.Length > 4096)
                return "对齐参考版本、稳定图像来源签名或图像坐标身份缺失，请重新获取参考。";
            if (ReferenceWidth < 1 || ReferenceHeight < 1 || (long)ReferenceWidth * ReferenceHeight > VmPoseImageAlignmentS132.MaximumImagePixels ||
                !Finite(ReferenceRow) || !Finite(ReferenceColumn) || !Finite(ReferenceAngleDegrees) || Math.Abs(ReferenceAngleDegrees) > 360000 ||
                ReferenceRow < 0 || ReferenceRow > ReferenceHeight - 1 || ReferenceColumn < 0 || ReferenceColumn > ReferenceWidth - 1)
                return "参考位姿必须为参考图内的有限像素位置和有效角度，参考图不能超过 16777216 像素。";
            if (OutputSizeMode == "Reference" && ((OutputWidth != 0 && OutputWidth != ReferenceWidth) ||
                (OutputHeight != 0 && OutputHeight != ReferenceHeight)))
                return "参考图范围必须采用已保存参考尺寸；不通过隐含尺寸参数缩放图像。";
            return string.Empty;
        }

        internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class VmPoseImageAlignmentResultS132 : IDisposable
    {
        public HImage Image { get; internal set; }
        public HRegion ValidDomain { get; internal set; }
        // Pixel-corner matrix actually passed to affine_trans_image_size and affine_trans_region.
        public double[] Matrix { get; internal set; }
        // Standard HALCON true-pixel-center matrix for arithmetic/affine_trans_point_2d.
        public double[] PixelMatrix { get; internal set; }
        public int InputWidth { get; internal set; }
        public int InputHeight { get; internal set; }
        public int OutputWidth { get; internal set; }
        public int OutputHeight { get; internal set; }
        public int Channels { get; internal set; }
        public string ImageType { get; internal set; }
        public double EffectivePixels { get; internal set; }
        public double InputEffectivePixels { get; internal set; }
        public double SupportedInputPixels { get; internal set; }
        public VmPose2D AlignedPose { get; internal set; }
        public string OutputContextId { get; internal set; }
        public string DomainPolicy { get; internal set; }
        public void Dispose()
        {
            var image = Image; Image = null; var domain = ValidDomain; ValidDomain = null;
            try { if (image != null) image.Dispose(); } finally { if (domain != null) domain.Dispose(); }
        }
    }

    public static class VmPoseImageAlignmentS132
    {
        public const long MaximumImagePixels = 16777216;

        // Stable logical coordinate identity: deliberately excludes current pose, frame version and current input dimensions.
        // The caller namespaces this hash with its own tool identity when publishing outputs.
        public static string GetOutputContextId(VmPoseImageAlignmentParametersS132 parameters)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");
            var p = parameters.Clone(); string error = p.Validate();
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (!p.HasReference) throw new InvalidOperationException("尚未保存图像对齐参考位姿。" );
            Guid version = Guid.Parse(p.ReferenceVersion);
            var key = new StringBuilder();
            AppendField(key, version.ToString("N")); AppendField(key, p.ReferenceSourceSignature); AppendField(key, p.ReferenceContextId);
            AppendField(key, p.ReferenceRow.ToString("R", CultureInfo.InvariantCulture));
            AppendField(key, p.ReferenceColumn.ToString("R", CultureInfo.InvariantCulture));
            AppendField(key, p.ReferenceAngleDegrees.ToString("R", CultureInfo.InvariantCulture));
            AppendField(key, p.ReferenceWidth.ToString(CultureInfo.InvariantCulture));
            AppendField(key, p.ReferenceHeight.ToString(CultureInfo.InvariantCulture));
            AppendField(key, p.OutputSizeMode); AppendField(key, p.Interpolation);
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key.ToString()))).Replace("-", "").ToLowerInvariant();
        }

        public static void DescribeImage(HImage image, out int width, out int height, out int channels, out string type)
        {
            if (image == null) throw new InvalidOperationException("图像对齐需要明确的当前 Image 输入。" );
            HTuple objects = null, count = null, w = null, h = null;
            try
            {
                HOperatorSet.CountObj(image, out objects); HOperatorSet.CountChannels(image, out count); HOperatorSet.GetImageSize(image, out w, out h);
                if (objects.Length != 1 || objects.I != 1 || count.Length != 1 || (count.I != 1 && count.I != 3) || w.Length != 1 || h.Length != 1)
                    throw new InvalidOperationException("图像对齐仅接受单张、1 或 3 通道图像，不自动选择多图或转换通道。" );
                width = w.I; height = h.I; channels = count.I;
                if (width < 1 || height < 1 || (long)width * height > MaximumImagePixels)
                    throw new InvalidOperationException("图像对齐输入尺寸无效或超过 16777216 像素。" );
                type = null;
                for (int i = 1; i <= channels; i++)
                {
                    using (var channel = image.AccessChannel(i))
                    using (var channelType = channel.GetImageType())
                    {
                        string currentType = channelType.Length == 1 ? channelType.S : null;
                        if ((currentType != "byte" && currentType != "int2" && currentType != "uint2" && currentType != "real") ||
                            (type != null && currentType != type))
                            throw new InvalidOperationException("图像对齐支持同类型 byte/int2/uint2/real 的 1 或 3 通道，不自动缩放灰度、截断或转换类型。" );
                        type = currentType;
                    }
                }
            }
            finally { Dispose(objects); Dispose(count); Dispose(w); Dispose(h); }
        }

        public static VmPoseImageAlignmentResultS132 Align(HImage image, VmPose2D current, VmPoseImageAlignmentParametersS132 parameters)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");
            var p = parameters.Clone(); var pose = current == null ? null : current.Clone(); string error = p.Validate();
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (!p.HasReference) throw new InvalidOperationException("尚未保存参考位姿，请先从明确的有效输入中获取参考到草稿。" );
            if (pose == null || !string.IsNullOrEmpty(pose.GetValidationError()) ||
                pose.CoordinateContext != VmPose2D.ImagePixelContext || pose.CoordinateContextId != p.ReferenceContextId ||
                Math.Abs(pose.AngleDegrees) > 360000)
                throw new InvalidOperationException("当前 Pose2D 无效、属于世界坐标或不在已保存参考的图像链中。" );
            int inputWidth, inputHeight, channels; string type; DescribeImage(image, out inputWidth, out inputHeight, out channels, out type);
            if (pose.Row < 0 || pose.Row > inputHeight - 1 || pose.Column < 0 || pose.Column > inputWidth - 1)
                throw new InvalidOperationException("当前位姿参考点不在当前输入图像内，禁止发布对齐图。" );
            int outputWidth = p.OutputSizeMode == "Reference" ? p.ReferenceWidth : inputWidth;
            int outputHeight = p.OutputSizeMode == "Reference" ? p.ReferenceHeight : inputHeight;
            if (p.ReferenceRow > outputHeight - 1 || p.ReferenceColumn > outputWidth - 1)
                throw new InvalidOperationException("选择当前输入范围后，参考位置落在输出画布之外；请选择参考图范围。" );
            var result = new VmPoseImageAlignmentResultS132();
            HTuple pixelMatrix = null, translated = null, imageMatrix = null, transformedRow = null, transformedColumn = null;
            HImage transformedImage = null, reducedImage = null;
            HRegion clippedInput = null, supportedInput = null, transformedDomain = null, clippedDomain = null, validDomain = null;
            try
            {
                HOperatorSet.VectorAngleToRigid(pose.Row, pose.Column, ToRadians(pose.AngleDegrees),
                    p.ReferenceRow, p.ReferenceColumn, ToRadians(p.ReferenceAngleDegrees), out pixelMatrix);
                // Official correction: standard pixel-center rigid M -> T(+.5,+.5) * M * T(-.5,-.5).
                HOperatorSet.HomMat2dTranslate(pixelMatrix, 0.5, 0.5, out translated);
                HOperatorSet.HomMat2dTranslateLocal(translated, -0.5, -0.5, out imageMatrix);
                ValidateRigid(pixelMatrix); ValidateRigid(imageMatrix);
                HOperatorSet.AffineTransPixel(imageMatrix, pose.Row, pose.Column, out transformedRow, out transformedColumn);
                if (!Near(transformedRow.D, p.ReferenceRow, 1e-6) || !Near(transformedColumn.D, p.ReferenceColumn, 1e-6))
                    throw new InvalidOperationException("刚体矩阵未把当前真实像素参考点变换到保存参考，禁止继续执行。" );
                using (var domain = image.GetDomain())
                using (var inputCanvas = Rectangle(inputWidth, inputHeight))
                {
                    clippedInput = domain.Intersection(inputCanvas);
                    double inputPixels = Area(clippedInput);
                    if (inputPixels <= 0) throw new InvalidOperationException("输入真实有效域为空，不能用补零像素对齐。" );
                    if (type == "real") ValidateRealPixels(image, clippedInput, channels, inputPixels);
                    // Bilinear interpolation reads up to four neighboring pixels. A symmetric 3x3 erosion keeps a
                    // conservative one-pixel support margin around every input domain boundary/hole, even for identity.
                    supportedInput = p.Interpolation == "bilinear" ? clippedInput.ErosionRectangle1(3, 3) : clippedInput.CopyObj(1, -1);
                    double supportPixels = Area(supportedInput);
                    if (supportPixels <= 0) throw new InvalidOperationException("输入域不足以支持选定插值；双线性需要完整邻域，请扩大输入域或明确选择最近邻。" );
                    var matrixObject = new HHomMat2D(imageMatrix);
                    transformedDomain = supportedInput.AffineTransRegion(matrixObject, "nearest_neighbor");
                    transformedImage = image.AffineTransImageSize(matrixObject, p.Interpolation, outputWidth, outputHeight);
                    int actualWidth, actualHeight, actualChannels; string actualType;
                    DescribeImage(transformedImage, out actualWidth, out actualHeight, out actualChannels, out actualType);
                    if (actualWidth != outputWidth || actualHeight != outputHeight || actualChannels != channels || actualType != type)
                        throw new InvalidOperationException("HALCON 返回的对齐图尺寸、通道或类型与真实请求不一致。" );
                    using (var canvas = Rectangle(outputWidth, outputHeight))
                    using (var halconDomain = transformedImage.GetDomain())
                    {
                        clippedDomain = transformedDomain.Intersection(canvas);
                        validDomain = clippedDomain.Intersection(halconDomain);
                    }
                    double pixels = Area(validDomain);
                    if (pixels <= 0) throw new InvalidOperationException("对齐后的真实有效域为空，不能发布全画布黑图或保留旧输出。" );
                    reducedImage = transformedImage.ReduceDomain(validDomain);
                    string outputContext = GetOutputContextId(p);
                    result.Image = reducedImage; reducedImage = null; result.ValidDomain = validDomain; validDomain = null;
                    result.Matrix = imageMatrix.ToDArr(); result.PixelMatrix = pixelMatrix.ToDArr();
                    result.InputWidth = inputWidth; result.InputHeight = inputHeight; result.OutputWidth = outputWidth; result.OutputHeight = outputHeight;
                    result.Channels = channels; result.ImageType = type; result.EffectivePixels = pixels; result.InputEffectivePixels = inputPixels;
                    result.SupportedInputPixels = supportPixels; result.OutputContextId = outputContext;
                    result.AlignedPose = VmPose2D.CreateImagePixel(transformedRow.D, transformedColumn.D, NormalizeDegrees(p.ReferenceAngleDegrees), outputContext);
                    result.DomainPolicy = p.Interpolation == "bilinear" ? "真实输入域 3×3 侵蚀（一像素插值支持边界）→同矩阵最近邻变换→输出画布及 HALCON 实际域交集" :
                        "真实输入域→同矩阵最近邻变换→输出画布及 HALCON 实际域交集";
                    return result;
                }
            }
            catch { result.Dispose(); throw; }
            finally
            {
                Dispose(pixelMatrix); Dispose(translated); Dispose(imageMatrix); Dispose(transformedRow); Dispose(transformedColumn);
                if (transformedImage != null) transformedImage.Dispose(); if (reducedImage != null) reducedImage.Dispose();
                if (clippedInput != null) clippedInput.Dispose(); if (supportedInput != null) supportedInput.Dispose();
                if (transformedDomain != null) transformedDomain.Dispose(); if (clippedDomain != null) clippedDomain.Dispose(); if (validDomain != null) validDomain.Dispose();
            }
        }

        private static HRegion Rectangle(int width, int height)
        {
            var region = new HRegion();
            try { region.GenRectangle1(0.0, 0.0, height - 1.0, width - 1.0); return region; }
            catch { region.Dispose(); throw; }
        }
        private static double Area(HRegion region)
        {
            HTuple rows = null, columns = null, areas = null;
            try
            {
                areas = region.AreaCenter(out rows, out columns); double total = 0;
                for (int i = 0; i < areas.Length; i++)
                {
                    double area = areas[i].D;
                    if (!VmPoseImageAlignmentParametersS132.Finite(area) || area < 0) throw new InvalidOperationException("对齐有效域面积无效。" );
                    total += area;
                }
                if (!VmPoseImageAlignmentParametersS132.Finite(total) || total > MaximumImagePixels)
                    throw new InvalidOperationException("对齐有效域面积超出安全限制。" );
                return total;
            }
            finally { Dispose(areas); Dispose(rows); Dispose(columns); }
        }
        private static void ValidateRealPixels(HImage image, HRegion domain, int channels, double expectedPixels)
        {
            for (int i = 1; i <= channels; i++)
            using (var channel = image.AccessChannel(i))
            using (var reduced = channel.ReduceDomain(domain))
            {
                HObject finite = null; HTuple areas = null, rows = null, columns = null;
                try
                {
                    // HALCON real pixels are IEEE-754 single precision. A finite bound excludes +/-Infinity and NaN.
                    HOperatorSet.Threshold(reduced, out finite, -3.4028234663852886E38, 3.4028234663852886E38);
                    HOperatorSet.AreaCenter(finite, out areas, out rows, out columns); double count = 0;
                    for (int j = 0; j < areas.Length; j++) count += areas[j].D;
                    if (count != expectedPixels) throw new InvalidOperationException("real 输入有效域含 NaN/Infinity，不能把无效像素插值到对齐输出。" );
                }
                finally { if (finite != null) finite.Dispose(); Dispose(areas); Dispose(rows); Dispose(columns); }
            }
        }
        private static void ValidateRigid(HTuple matrix)
        {
            if (matrix == null || matrix.Length != 6) throw new InvalidOperationException("对齐矩阵不是合法 2D 刚体矩阵。" );
            double[] m = matrix.ToDArr();
            for (int i = 0; i < m.Length; i++) if (!VmPoseImageAlignmentParametersS132.Finite(m[i]) || Math.Abs(m[i]) > 100000000)
                throw new InvalidOperationException("对齐矩阵含非有限或超界数值。" );
            if (!Near(m[0] * m[0] + m[1] * m[1], 1, 1e-9) || !Near(m[3] * m[3] + m[4] * m[4], 1, 1e-9) ||
                !Near(m[0] * m[3] + m[1] * m[4], 0, 1e-9) || !Near(m[0] * m[4] - m[1] * m[3], 1, 1e-9))
                throw new InvalidOperationException("图像对齐只允许平移/旋转，不接受缩放、斜切或反射。" );
        }
        private static void AppendField(StringBuilder builder, string value)
        {
            value = value ?? string.Empty; builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('|');
        }
        private static bool Near(double a, double b, double tolerance)
        { return VmPoseImageAlignmentParametersS132.Finite(a) && VmPoseImageAlignmentParametersS132.Finite(b) && Math.Abs(a - b) <= tolerance; }
        private static double NormalizeDegrees(double value)
        { double angle = value % 360; return angle > 180 ? angle - 360 : (angle <= -180 ? angle + 360 : angle); }
        private static double ToRadians(double value) { return NormalizeDegrees(value) * Math.PI / 180.0; }
        private static void Dispose(HTuple tuple) { if (tuple != null) tuple.Dispose(); }
    }
}
