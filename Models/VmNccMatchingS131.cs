using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmNccParametersS131
    {
        [DataMember(Order = 1)] public string ModelPath { get; set; }
        [DataMember(Order = 2)] public string ModelSha256 { get; set; }
        [DataMember(Order = 3)] public string ModelName { get; set; }
        [DataMember(Order = 4)] public double MinScore { get; set; } = 0.7;
        [DataMember(Order = 5)] public int MaxMatches { get; set; } = 1;
        [DataMember(Order = 6)] public double MaxOverlap { get; set; } = 0.5;
        [DataMember(Order = 7)] public double AngleStartDegrees { get; set; } = -180;
        [DataMember(Order = 8)] public double AngleExtentDegrees { get; set; } = 360;
        [DataMember(Order = 9)] public int NumLevels { get; set; }
        [DataMember(Order = 10)] public bool UsePolarity { get; set; } = true;
        [DataMember(Order = 11)] public bool Subpixel { get; set; } = true;
        [DataMember(Order = 12)] public double TrainingRow1 { get; set; }
        [DataMember(Order = 13)] public double TrainingColumn1 { get; set; }
        [DataMember(Order = 14)] public double TrainingRow2 { get; set; }
        [DataMember(Order = 15)] public double TrainingColumn2 { get; set; }
        [DataMember(Order = 16)] public double TemplateReferenceRow { get; set; }
        [DataMember(Order = 17)] public double TemplateReferenceColumn { get; set; }
        [DataMember(Order = 18)] public string TrainingImageContext { get; set; }
        [DataMember(Order = 19)] public string TrainingImageType { get; set; }
        // HALCON can widen the generated angular grid slightly. These are the queried, actual model bounds.
        [DataMember(Order = 20)] public double TrainedAngleStartDegrees { get; set; }
        [DataMember(Order = 21)] public double TrainedAngleExtentDegrees { get; set; }
        [DataMember(Order = 22)] public bool TrainedUsePolarity { get; set; }
        [DataMember(Order = 23)] public int TrainedNumLevels { get; set; }
        [DataMember(Order = 24)] public int ActualNumLevels { get; set; }
        [DataMember(Order = 25)] public double ActualAngleStepDegrees { get; set; }
        [DataMember(Order = 26)] public int TrainingImageWidth { get; set; }
        [DataMember(Order = 27)] public int TrainingImageHeight { get; set; }
        [DataMember(Order = 28)] public double TrainingPixelCount { get; set; }
        [DataMember(Order = 29)] public string TrainedAtUtc { get; set; }
        [DataMember(Order = 30)] public int ModelFormatVersion { get; set; }

        [OnDeserializing]
        private void Defaults(StreamingContext context)
        {
            MinScore = 0.7; MaxMatches = 1; MaxOverlap = 0.5; AngleStartDegrees = -180;
            AngleExtentDegrees = 360; NumLevels = 0; UsePolarity = true; Subpixel = true;
        }

        public VmNccParametersS131 Clone() { return (VmNccParametersS131)MemberwiseClone(); }

        public bool HasModel
        {
            get { return ModelFormatVersion == 1 && !string.IsNullOrWhiteSpace(ModelPath) && IsSha256(ModelSha256); }
        }

        public string Validate()
        {
            if (!Finite(MinScore) || MinScore < 0 || MinScore > 1 || !Finite(MaxOverlap) || MaxOverlap < 0 || MaxOverlap > 1)
                return "NCC 最低分数和最大重叠须为 0–1 的有限数。";
            if (MaxMatches < 1 || MaxMatches > 256) return "NCC 最多候选须为 1–256，不使用无界候选模式。";
            if (!Finite(AngleStartDegrees) || AngleStartDegrees < -180 || AngleStartDegrees > 180 ||
                !Finite(AngleExtentDegrees) || AngleExtentDegrees < 0 || AngleExtentDegrees > 360)
                return "NCC 起始角须为 −180–180°，搜索角度范围须为 0–360°，均须为有限数。";
            if (NumLevels < 0 || NumLevels > 10) return "NCC 金字塔层数须为 0（自动）或 1–10。";
            if (!string.IsNullOrWhiteSpace(ModelName) && ModelName.Length > 128) return "NCC 模型名称最多 128 个字符。";
            if (!string.IsNullOrWhiteSpace(ModelPath) || !string.IsNullOrWhiteSpace(ModelSha256) || ModelFormatVersion != 0)
            {
                if (!HasModel || string.IsNullOrWhiteSpace(TrainingImageContext) ||
                    (TrainingImageType != "byte" && TrainingImageType != "uint2")) return "NCC 模型版本、哈希或训练图像身份不完整，请重新制作模板。";
                if (TrainingImageWidth < 1 || TrainingImageHeight < 1 || (long)TrainingImageWidth * TrainingImageHeight > VmNccModelS131.MaximumImagePixels ||
                    !ValidBox(TrainingRow1, TrainingColumn1, TrainingRow2, TrainingColumn2, TrainingImageWidth, TrainingImageHeight) ||
                    TrainingRow1 != Math.Floor(TrainingRow1) || TrainingColumn1 != Math.Floor(TrainingColumn1) ||
                    TrainingRow2 != Math.Floor(TrainingRow2) || TrainingColumn2 != Math.Floor(TrainingColumn2) ||
                    !Finite(TemplateReferenceRow) || !Finite(TemplateReferenceColumn) ||
                    Math.Abs(TemplateReferenceRow - (TrainingRow1 + TrainingRow2) * 0.5) > 1e-7 ||
                    Math.Abs(TemplateReferenceColumn - (TrainingColumn1 + TrainingColumn2) * 0.5) > 1e-7 ||
                    !Finite(TrainingPixelCount) || TrainingPixelCount < 8 || TrainingPixelCount > VmNccModelS131.MaximumTemplatePixels)
                    return "NCC 模板矩形、参考点或训练尺寸无效，请重新制作模板。";
                if (TrainedNumLevels < 0 || TrainedNumLevels > 10 || ActualNumLevels < 1 || ActualNumLevels > 10 ||
                    !Finite(TrainedAngleStartDegrees) || Math.Abs(TrainedAngleStartDegrees) > 720 ||
                    !Finite(TrainedAngleExtentDegrees) || TrainedAngleExtentDegrees < 0 || TrainedAngleExtentDegrees > 720 ||
                    !Finite(ActualAngleStepDegrees) || ActualAngleStepDegrees < 0 || ActualAngleStepDegrees > 11.25 + 1e-8)
                    return "NCC 训练角度网格或层数元数据无效，请重新制作模板。";
            }
            return string.Empty;
        }

        internal static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
        internal static bool ValidBox(double r1, double c1, double r2, double c2, int width, int height)
        {
            return Finite(r1) && Finite(c1) && Finite(r2) && Finite(c2) && r1 >= 0 && c1 >= 0 &&
                r2 >= r1 && c2 >= c1 && r2 <= height - 1 && c2 <= width - 1;
        }
        internal static bool IsSha256(string text)
        {
            if (text == null || text.Length != 64) return false;
            foreach (char c in text) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F')) return false;
            return true;
        }
    }

    public sealed class VmNccMatchS131
    {
        public int Index { get; set; } // One-based, in the original HALCON candidate order.
        public double Row { get; set; } // HALCON iconic-transformation coordinate, not the precise pixel-center coordinate.
        public double Column { get; set; }
        public double AngleRadians { get; set; }
        public double Score { get; set; }
        public double PixelRow { get; set; }
        public double PixelColumn { get; set; }
        public double AngleDegrees { get { return AngleRadians * 180.0 / Math.PI; } }
    }

    // One private handle per operation. Model assets are immutable versions; no handle lives in recipe/draft state.
    public static class VmNccModelS131
    {
        public const long MaximumImagePixels = 16777216;
        public const long MaximumTemplatePixels = 1048576;
        public const int SearchTimeoutMilliseconds = 10000;
        private const long MaximumModelBytes = 536870912;

        public static void DescribeImage(HImage image, out int width, out int height, out string type)
        {
            if (image == null) throw new InvalidOperationException("NCC 需要明确的当前模块输入图像。" );
            HTuple w = null, h = null, t = null, channels = null, objects = null;
            try
            {
                HOperatorSet.CountObj(image, out objects); HOperatorSet.CountChannels(image, out channels);
                HOperatorSet.GetImageSize(image, out w, out h); HOperatorSet.GetImageType(image, out t);
                if (objects.Length != 1 || objects.I != 1 || channels.Length != 1 || channels.I != 1 || t.Length != 1 ||
                    (t.S != "byte" && t.S != "uint2"))
                    throw new InvalidOperationException("NCC 仅支持单张、单通道 byte/uint2 图像；RGB 请在上游显式转灰度，不自动缩放或转型。" );
                width = w.I; height = h.I; type = t.S;
                if (width < 1 || height < 1 || (long)width * height > MaximumImagePixels)
                    throw new InvalidOperationException("NCC 图像尺寸无效或超过 16777216 像素，请在上游显式裁剪。" );
            }
            finally { Dispose(w); Dispose(h); Dispose(t); Dispose(channels); Dispose(objects); }
        }

        public static VmNccParametersS131 Train(HImage image, VmNccParametersS131 draft, double row1, double column1,
            double row2, double column2, string context, string newModelPath)
        {
            if (draft == null) throw new ArgumentNullException("draft");
            var p = draft.Clone();
            string error = p.Validate();
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            int width, height; string type; DescribeImage(image, out width, out height, out type);
            if (!VmNccParametersS131.ValidBox(row1, column1, row2, column2, width, height))
                throw new InvalidOperationException("训练矩形必须完整位于当前输入图像内，坐标须为有限数，不自动截断。" );
            if (row1 != Math.Floor(row1) || column1 != Math.Floor(column1) || row2 != Math.Floor(row2) || column2 != Math.Floor(column2))
                throw new InvalidOperationException("训练矩形须按整数像素选取，以保持真实域质心与显示参考点一致；不隐式四舍五入。" );
            if (string.IsNullOrWhiteSpace(context)) throw new InvalidOperationException("训练图像坐标身份缺失，不能建立 NCC 模板。" );
            if (string.IsNullOrWhiteSpace(newModelPath) || !string.Equals(Path.GetExtension(newModelPath), ".ncm", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("NCC 新模型版本须使用独立 .ncm 文件。" );
            string destination = Path.GetFullPath(newModelPath);
            if ((!string.IsNullOrWhiteSpace(draft.ModelPath) && string.Equals(destination, Path.GetFullPath(draft.ModelPath), StringComparison.OrdinalIgnoreCase)) || File.Exists(destination))
                throw new InvalidOperationException("新模型路径已被占用，禁止覆盖当前模型版本。" );
            string directory = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);
            string temporary = Path.Combine(directory, ".ncc-training-" + Guid.NewGuid().ToString("N") + ".ncm");
            bool ownedTemporary = false, moved = false, complete = false;
            HTuple model = null, area = null, referenceRow = null, referenceColumn = null, mean = null, deviation = null;
            HObject rectangle = null, missing = null, template = null;
            try
            {
                HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
                using (var domain = image.GetDomain())
                {
                    HOperatorSet.Difference(rectangle, domain, out missing);
                    if (Area(missing) != 0) throw new InvalidOperationException("训练矩形有像素位于图像有效域之外，请换用完整可见的模板，不自动补背景。" );
                }
                HOperatorSet.AreaCenter(rectangle, out area, out referenceRow, out referenceColumn);
                if (area.Length != 1 || !VmNccParametersS131.Finite(area.D) || area.D < 8 || area.D > MaximumTemplatePixels)
                    throw new InvalidOperationException("NCC 模板须包含 8–1048576 个真实像素。" );
                HOperatorSet.Intensity(rectangle, image, out mean, out deviation);
                if (mean.Length != 1 || deviation.Length != 1 || !VmNccParametersS131.Finite(mean.D) ||
                    !VmNccParametersS131.Finite(deviation.D) || deviation.D <= 0)
                    throw new InvalidOperationException("训练区域没有有效灰度变化，不能制作归一化相关模板。" );
                HOperatorSet.ReduceDomain(image, rectangle, out template);
                HOperatorSet.CreateNccModel(template, p.NumLevels, ToRadians(p.AngleStartDegrees), ToRadians(p.AngleExtentDegrees),
                    "auto", p.UsePolarity ? "use_polarity" : "ignore_global_polarity", out model);
                VerifyDefaultOrigin(model);
                int actualLevels; double actualStart, actualExtent, actualStep; string actualMetric;
                ReadModelMetadata(model, out actualLevels, out actualStart, out actualExtent, out actualStep, out actualMetric);
                if (actualMetric != (p.UsePolarity ? "use_polarity" : "ignore_global_polarity"))
                    throw new InvalidOperationException("HALCON NCC 实际模型极性与训练设置不符，不能保存该版本。" );
                p.ModelFormatVersion = 1; p.ModelPath = destination; p.ModelSha256 = new string('0', 64);
                p.TrainingRow1 = row1; p.TrainingColumn1 = column1; p.TrainingRow2 = row2; p.TrainingColumn2 = column2;
                p.TemplateReferenceRow = referenceRow.D; p.TemplateReferenceColumn = referenceColumn.D;
                p.TrainingImageContext = context; p.TrainingImageType = type; p.TrainingImageWidth = width; p.TrainingImageHeight = height;
                p.TrainingPixelCount = area.D; p.TrainedUsePolarity = p.UsePolarity; p.TrainedNumLevels = p.NumLevels;
                p.ActualNumLevels = actualLevels; p.TrainedAngleStartDegrees = ToDegrees(actualStart);
                p.TrainedAngleExtentDegrees = ToDegrees(actualExtent); p.ActualAngleStepDegrees = ToDegrees(actualStep);
                p.TrainedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(p.ModelName)) p.ModelName = "NCC 灰度模板";
                error = p.Validate(); if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                using (new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) ownedTemporary = true;
                HOperatorSet.WriteNccModel(model, temporary);
                p.ModelSha256 = HashFile(temporary);
                // File.Move refuses an existing destination; model creation never overwrites a previous draft/applied version.
                File.Move(temporary, destination); moved = true;
                complete = true; return p;
            }
            finally
            {
                ClearModel(model); Dispose(area); Dispose(referenceRow); Dispose(referenceColumn); Dispose(mean); Dispose(deviation);
                if (rectangle != null) rectangle.Dispose(); if (missing != null) missing.Dispose(); if (template != null) template.Dispose();
                if (!complete) { if (ownedTemporary) DeleteOwned(temporary); if (moved) DeleteOwned(destination); }
            }
        }

        public static IList<VmNccMatchS131> Match(HImage image, HRegion roi, VmNccParametersS131 parameters)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");
            var p = parameters.Clone(); string error = p.Validate();
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (!p.HasModel) throw new InvalidOperationException("尚未制作 NCC 模板，请先从当前模块输入框选并制作模板。" );
            if (p.UsePolarity != p.TrainedUsePolarity || p.NumLevels != p.TrainedNumLevels)
                throw new InvalidOperationException("极性或建模层数已经改变，请重新制作模板；不能把新草稿参数冒充原模型。" );
            int width, height; string type; DescribeImage(image, out width, out height, out type);
            if (type != p.TrainingImageType) throw new InvalidOperationException("当前图像灰度类型与模板不同，请在上游显式统一类型，不自动转型。" );
            using (var resource = OpenResource(p.ModelPath, p.ModelSha256))
            using (var domain = image.GetDomain())
            using (var searchRoi = roi == null ? null : roi.Union1())
            using (var effective = searchRoi == null ? domain.CopyObj(1, -1) : domain.Intersection(searchRoi))
            {
                if (Area(effective) <= 0) throw new InvalidOperationException("NCC 搜索有效域为空，不能把输入配置错误当作无匹配。" );
                HTuple model = null, rows = null, columns = null, angles = null, scores = null;
                HObject searchImage = null;
                try
                {
                    HOperatorSet.ReadNccModel(p.ModelPath, out model); VerifyDefaultOrigin(model);
                    int levels; double start, extent, step; string metric;
                    ReadModelMetadata(model, out levels, out start, out extent, out step, out metric);
                    if (levels != p.ActualNumLevels || !Near(ToDegrees(start), p.TrainedAngleStartDegrees) ||
                        !Near(ToDegrees(extent), p.TrainedAngleExtentDegrees) || !Near(ToDegrees(step), p.ActualAngleStepDegrees) ||
                        metric != (p.TrainedUsePolarity ? "use_polarity" : "ignore_global_polarity"))
                        throw new InvalidOperationException("NCC 文件与已保存训练元数据不一致，请重新制作模板。" );
                    double requestedStart = ToRadians(p.AngleStartDegrees), requestedExtent = ToRadians(p.AngleExtentDegrees);
                    double fullTurn = 2 * Math.PI;
                    if (extent >= fullTurn - 1e-9 && requestedExtent >= fullTurn - 1e-9)
                    {
                        // HALCON may shift the sampling start to include zero.
                        // A verified full-turn model remains a full turn, not an
                        // invalid linear interval at the -pi/pi representation seam.
                        requestedStart = start;
                        requestedExtent = fullTurn;
                    }
                    else
                    {
                        double shift = Math.Round((start - requestedStart) / fullTurn) * fullTurn;
                        if (requestedStart + shift >= start - 1e-9 && requestedStart + shift + requestedExtent <= start + extent + 1e-9)
                            requestedStart += shift;
                    }
                    if (requestedStart < start - 1e-9 || requestedStart + requestedExtent > start + extent + 1e-9)
                        throw new InvalidOperationException("搜索角度超出已制作模型的旋转范围，请缩小范围或重新制作模板；不静默截断。" );
                    HOperatorSet.SetNccModelParam(model, "timeout", SearchTimeoutMilliseconds);
                    // The reduced domain constrains the unchanged original model centroid, not its entire footprint.
                    HOperatorSet.ReduceDomain(image, effective, out searchImage);
                    HOperatorSet.FindNccModel(searchImage, model, requestedStart, requestedExtent, p.MinScore, p.MaxMatches,
                        p.MaxOverlap, p.Subpixel ? "true" : "false", 0, out rows, out columns, out angles, out scores);
                    if (rows == null || columns == null || angles == null || scores == null ||
                        rows.Length != columns.Length || rows.Length != angles.Length || rows.Length != scores.Length ||
                        rows.Length > p.MaxMatches || rows.Length > 256)
                        throw new InvalidOperationException("HALCON NCC 候选输出长度不一致或超出配置上限。" );
                    // Reject the entire raw return before any candidate list is allocated/copied.
                    for (int i = 0; i < rows.Length; i++)
                    {
                        double r = rows[i].D, c = columns[i].D, angle = angles[i].D, score = scores[i].D;
                        if (!VmNccParametersS131.Finite(r) || !VmNccParametersS131.Finite(c) ||
                            !VmNccParametersS131.Finite(angle) || !VmNccParametersS131.Finite(score) ||
                            score < p.MinScore - 1e-7 || score > 1 + 1e-7 || Math.Abs(angle) > 4 * Math.PI)
                            throw new InvalidOperationException("HALCON NCC 输出非有限或不合法候选，禁止发布旧结果或截断数值。" );
                    }
                    var matches = new List<VmNccMatchS131>(rows.Length);
                    for (int i = 0; i < rows.Length; i++)
                    {
                        double r = rows[i].D, c = columns[i].D, angle = angles[i].D, score = scores[i].D;
                        HTuple matrix = null, pixelRow = null, pixelColumn = null;
                        try
                        {
                            // find_ncc_model returns iconic-transformation coordinates. This is the documented conversion to true pixel centers.
                            HOperatorSet.VectorAngleToRigid(0, 0, 0, r, c, angle, out matrix);
                            HOperatorSet.AffineTransPixel(matrix, 0, 0, out pixelRow, out pixelColumn);
                            if (pixelRow == null || pixelColumn == null || pixelRow.Length != 1 || pixelColumn.Length != 1 ||
                                !VmNccParametersS131.Finite(pixelRow.D) || !VmNccParametersS131.Finite(pixelColumn.D) ||
                                pixelRow.D < -0.5 || pixelRow.D > height - 0.5 || pixelColumn.D < -0.5 || pixelColumn.D > width - 0.5)
                                throw new InvalidOperationException("NCC 参考点超出输入图像坐标范围，禁止发布无效位置。" );
                            matches.Add(new VmNccMatchS131 { Index = i + 1, Row = r, Column = c, AngleRadians = angle,
                                Score = score, PixelRow = pixelRow.D, PixelColumn = pixelColumn.D });
                        }
                        finally { Dispose(matrix); Dispose(pixelRow); Dispose(pixelColumn); }
                    }
                    // An empty list is a genuine successful search with no matches, not an execution error.
                    return new ReadOnlyCollection<VmNccMatchS131>(matches);
                }
                finally { ClearModel(model); Dispose(rows); Dispose(columns); Dispose(angles); Dispose(scores); if (searchImage != null) searchImage.Dispose(); }
            }
        }

        public static string HashFile(string path)
        {
            using (var stream = OpenResource(path, null)) return HashStream(stream);
        }
        public static FileStream OpenResource(string path, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("NCC 模型路径缺失。" );
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                if (stream.Length < 1 || stream.Length > MaximumModelBytes) throw new InvalidOperationException("NCC 模型为空或超过 512 MB。" );
                if (expectedHash != null && (!VmNccParametersS131.IsSha256(expectedHash) ||
                    !string.Equals(HashStream(stream), expectedHash, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("NCC 模型 SHA-256 不符，禁止读取已被改写的模型版本。" );
                stream.Position = 0; return stream;
            }
            catch { stream.Dispose(); throw; }
        }
        private static string HashStream(Stream stream)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        private static double Area(HObject region)
        {
            HTuple area = null, row = null, column = null;
            try
            {
                HOperatorSet.AreaCenter(region, out area, out row, out column); double total = 0;
                for (int i = 0; i < area.Length; i++)
                { double value = area[i].D; if (!VmNccParametersS131.Finite(value) || value < 0) throw new InvalidOperationException("NCC 区域面积无效。" ); total += value; }
                return total;
            }
            finally { Dispose(area); Dispose(row); Dispose(column); }
        }
        private static void ReadModelMetadata(HTuple model, out int levels, out double start, out double extent, out double step, out string metric)
        {
            HTuple l = null, a = null, e = null, s = null, m = null;
            try
            {
                HOperatorSet.GetNccModelParams(model, out l, out a, out e, out s, out m);
                if (l.Length != 1 || a.Length != 1 || e.Length != 1 || s.Length != 1 || m.Length != 1)
                    throw new InvalidOperationException("NCC 模型参数输出不完整。" );
                levels = l.I; start = a.D; extent = e.D; step = s.D; metric = m.S;
            }
            finally { Dispose(l); Dispose(a); Dispose(e); Dispose(s); Dispose(m); }
        }
        private static void VerifyDefaultOrigin(HTuple model)
        {
            HTuple r = null, c = null;
            try
            {
                HOperatorSet.GetNccModelOrigin(model, out r, out c);
                if (r.Length != 1 || c.Length != 1 || !Near(r.D, 0) || !Near(c.D, 0))
                    throw new InvalidOperationException("NCC 模型含非默认参考点；本模块只支持训练域质心，不能隐式混用搜索 ROI 和自定义原点。" );
            }
            finally { Dispose(r); Dispose(c); }
        }
        private static bool Near(double a, double b) { return VmNccParametersS131.Finite(a) && VmNccParametersS131.Finite(b) && Math.Abs(a - b) <= 1e-7; }
        private static double ToRadians(double degrees) { return degrees * Math.PI / 180.0; }
        private static double ToDegrees(double radians) { return radians * 180.0 / Math.PI; }
        private static void ClearModel(HTuple model)
        {
            if (model == null) return;
            // Cleanup must not hide an original training/search error or skip disposal of subsequent images/tuples.
            try { if (model.Length > 0) HOperatorSet.ClearNccModel(model); } catch { } finally { model.Dispose(); }
        }
        private static void Dispose(HTuple tuple) { if (tuple != null) tuple.Dispose(); }
        private static void DeleteOwned(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }
}
