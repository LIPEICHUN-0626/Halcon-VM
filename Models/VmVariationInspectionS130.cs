using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmVariationSampleS130
    {
        [DataMember(Order = 1)] public string SampleId { get; set; }
        [DataMember(Order = 2)] public string ImagePath { get; set; }
        [DataMember(Order = 3)] public string ImageSha256 { get; set; }
        [DataMember(Order = 4)] public string DomainPath { get; set; }
        [DataMember(Order = 5)] public string DomainSha256 { get; set; }
        [DataMember(Order = 6)] public string DisplayName { get; set; }
        public string Name { get { return string.IsNullOrWhiteSpace(DisplayName) ? Path.GetFileName(ImagePath) : DisplayName; } }
        public string Status { get; set; }
        public VmVariationSampleS130 Clone() { return (VmVariationSampleS130)MemberwiseClone(); }
    }

    [DataContract]
    public sealed class VmVariationParametersS130
    {
        [DataMember(Order = 1)] public double AbsoluteThreshold { get; set; } = 15;
        [DataMember(Order = 2)] public double VariationFactor { get; set; } = 3;
        [DataMember(Order = 3)] public double MinimumDefectArea { get; set; } = 5;
        [DataMember(Order = 4)] public List<VmVariationSampleS130> Samples { get; set; } = new List<VmVariationSampleS130>();
        [DataMember(Order = 5)] public string DatasetContext { get; set; }
        [DataMember(Order = 6)] public string DatasetSourceSignature { get; set; }
        [DataMember(Order = 7)] public string ModelPath { get; set; }
        [DataMember(Order = 8)] public string CoveragePath { get; set; }
        [DataMember(Order = 9)] public string ModelSha256 { get; set; }
        [DataMember(Order = 10)] public string CoverageSha256 { get; set; }
        [DataMember(Order = 11)] public string ModelDatasetKey { get; set; }
        [DataMember(Order = 12)] public int Width { get; set; }
        [DataMember(Order = 13)] public int Height { get; set; }
        [DataMember(Order = 14)] public string ImageType { get; set; }
        [DataMember(Order = 15)] public string TrainedAtUtc { get; set; }

        [OnDeserializing]
        private void Defaults(StreamingContext context)
        { AbsoluteThreshold = 15; VariationFactor = 3; MinimumDefectArea = 5; Samples = new List<VmVariationSampleS130>(); }

        public VmVariationParametersS130 Clone()
        {
            var copy = (VmVariationParametersS130)MemberwiseClone();
            copy.Samples = (Samples ?? new List<VmVariationSampleS130>()).Select(s => s == null ? null : s.Clone()).ToList();
            return copy;
        }

        public string Validate()
        {
            if (!Finite(AbsoluteThreshold) || AbsoluteThreshold < 0 || AbsoluteThreshold > 65535 ||
                !Finite(VariationFactor) || VariationFactor < 0 || VariationFactor > 100 ||
                !Finite(MinimumDefectArea) || MinimumDefectArea < 0 || MinimumDefectArea > 16777216)
                return "灰差须为 0–65535、波动倍数为 0–100、最小缺陷面积为 0–16777216 像素，均须为有限数。";
            if (Samples == null || Samples.Count > 64 || Samples.Any(s => s == null || string.IsNullOrWhiteSpace(s.SampleId) ||
                string.IsNullOrWhiteSpace(s.ImagePath) || string.IsNullOrWhiteSpace(s.ImageSha256)) ||
                Samples.Select(s => s.SampleId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Samples.Count)
                return "良品样本队列无效：最多 64 张，每张须有唯一编号、受管图像和哈希。";
            return string.Empty;
        }

        public string DatasetKey()
        {
            return VmVariationModelS130.HashText((DatasetContext ?? "") + "\n" + (DatasetSourceSignature ?? "") + "\n" +
                string.Join("\n", (Samples ?? new List<VmVariationSampleS130>()).Select(s => s == null ? "null" :
                    s.SampleId + ":" + s.ImageSha256 + ":" + s.DomainSha256)));
        }
        public bool HasModelVersion()
        { return !string.IsNullOrWhiteSpace(ModelPath) && !string.IsNullOrWhiteSpace(CoveragePath) && ModelDatasetKey == DatasetKey(); }
        public void ClearModel()
        { ModelPath = CoveragePath = ModelSha256 = CoverageSha256 = ModelDatasetKey = TrainedAtUtc = null; Width = Height = 0; ImageType = null; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class VmVariationTrainingReportS130
    {
        public VmVariationParametersS130 Parameters { get; set; }
        public string[] Statuses { get; set; }
        public string Summary { get; set; }
    }

    // Handles are local to one training/detection call; no mutable model is shared with a draft or another module.
    public static class VmVariationModelS130
    {
        public static string HashText(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        public static string HashDto(object value, Type type)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(type).WriteObject(stream, value);
                using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "");
            }
        }
        public static string HashFile(string path)
        { using (var stream = OpenResource(path, null)) return HashStream(stream); }
        private static string HashStream(Stream stream)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        public static FileStream OpenResource(string path, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("良品模型/样本资源路径缺失。");
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                if (stream.Length == 0 || stream.Length > 536870912) throw new InvalidOperationException("良品资源为空或超过 512 MB。" );
                if (expectedHash != null && (string.IsNullOrWhiteSpace(expectedHash) ||
                    !string.Equals(HashStream(stream), expectedHash, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("良品资源 SHA-256 不符，禁止使用被修改的模型/样本。" );
                stream.Position = 0; return stream;
            }
            catch { stream.Dispose(); throw; }
        }

        public static void DescribeImage(HImage image, out int width, out int height, out string type)
        {
            HTuple w = null, h = null, t = null, channels = null;
            try
            {
                HOperatorSet.GetImageSize(image, out w, out h); HOperatorSet.GetImageType(image, out t); HOperatorSet.CountChannels(image, out channels);
                width = w.I; height = h.I; type = t.S;
                if (channels.I != 1 || (type != "byte" && type != "int2" && type != "uint2"))
                    throw new InvalidOperationException("良品变化检测仅支持单通道 byte/int2/uint2；RGB 请在上游显式转灰度，不自动缩放灰度。" );
                if (width <= 0 || height <= 0 || (long)width * height > 16777216)
                    throw new InvalidOperationException("良品图像须为有效尺寸且不超过 16777216 像素。" );
            }
            finally { DisposeTuple(w); DisposeTuple(h); DisposeTuple(t); DisposeTuple(channels); }
        }

        public static VmVariationTrainingReportS130 Train(VmVariationParametersS130 draft, string modelPath, string coveragePath,
            CancellationToken cancellation, Action<int, string> progress)
        {
            var p = draft.Clone();
            string error = p.Validate();
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            if (p.Samples.Count < 3 || string.IsNullOrWhiteSpace(p.DatasetContext) || string.IsNullOrWhiteSpace(p.DatasetSourceSignature))
                throw new InvalidOperationException("至少 3 张已对齐的良品，且须从明确图像来源建立样本集。" );
            var report = new VmVariationTrainingReportS130 { Statuses = new string[p.Samples.Count] };
            HTuple model = null;
            HRegion coverage = null;
            bool complete = false;
            try
            {
                for (int i = 0; i < p.Samples.Count; i++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var sample = p.Samples[i];
                    try
                    {
                        using (var locked = OpenResource(sample.ImagePath, sample.ImageSha256))
                        using (var image = new HImage(sample.ImagePath))
                        using (var imageDomain = image.GetDomain())
                        {
                            int width, height; string type;
                            DescribeImage(image, out width, out height, out type);
                            if (i == 0)
                            {
                                if (p.Width > 0 && (width != p.Width || height != p.Height || type != p.ImageType))
                                    throw new InvalidOperationException("首张样本与当前订阅图像的尺寸/类型不一致。" );
                                if ((long)width * height * p.Samples.Count > 268435456)
                                    throw new InvalidOperationException("本次样本总像素超过 268435456，请减少样本或裁剪上游图像。" );
                                p.Width = width; p.Height = height; p.ImageType = type;
                                HOperatorSet.CreateVariationModel(width, height, type, "standard", out model);
                            }
                            else if (width != p.Width || height != p.Height || type != p.ImageType)
                                throw new InvalidOperationException("图像尺寸/类型与首张良品不一致。" );
                            using (var domainLock = string.IsNullOrWhiteSpace(sample.DomainPath) ? null : OpenResource(sample.DomainPath, sample.DomainSha256))
                            using (var savedDomain = ReadCoverage(sample.DomainPath))
                            using (var valid = savedDomain == null ? imageDomain.CopyObj(1, -1) : imageDomain.Intersection(savedDomain))
                            {
                                var next = coverage == null ? valid.CopyObj(1, -1) : coverage.Intersection(valid);
                                if (coverage != null) coverage.Dispose(); coverage = next;
                                if (Area(coverage) <= 0) throw new InvalidOperationException("样本共同有效域为空。" );
                            }
                            // Train full frames but retain the intersection of genuine sample domains as the only allowed detection coverage.
                            using (var full = image.FullDomain()) HOperatorSet.TrainVariationModel(full, model);
                        }
                        report.Statuses[i] = "已学习";
                        if (progress != null) progress(i, report.Statuses[i]);
                    }
                    catch (Exception ex)
                    {
                        report.Statuses[i] = "失败：" + ex.Message;
                        if (progress != null) progress(i, report.Statuses[i]);
                        throw new InvalidOperationException("第 " + (i + 1) + " 张“" + sample.Name + "”：" + ex.Message, ex);
                    }
                }
                cancellation.ThrowIfCancellationRequested();
                HOperatorSet.PrepareVariationModel(model, p.AbsoluteThreshold, p.VariationFactor);
                HOperatorSet.ClearTrainDataVariationModel(model);
                HOperatorSet.WriteVariationModel(model, modelPath);
                HOperatorSet.WriteRegion(coverage, coveragePath);
                cancellation.ThrowIfCancellationRequested();
                p.ModelPath = modelPath; p.CoveragePath = coveragePath;
                p.ModelSha256 = HashFile(modelPath); p.CoverageSha256 = HashFile(coveragePath);
                p.ModelDatasetKey = p.DatasetKey(); p.TrainedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                report.Parameters = p;
                report.Summary = string.Format(CultureInfo.InvariantCulture, "已学习 {0} 张 · {1}×{2} {3} · 共同有效域 {4:0} px · 待应用", p.Samples.Count, p.Width, p.Height, p.ImageType, Area(coverage));
                complete = true; return report;
            }
            finally
            {
                if (coverage != null) coverage.Dispose(); ClearModel(model);
                if (!complete) { DeleteCreated(modelPath); DeleteCreated(coveragePath); }
            }
        }

        public static HRegion ReadCoverage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            HObject raw = null;
            try { HOperatorSet.ReadRegion(out raw, path); return new HRegion(raw); }
            finally { if (raw != null) raw.Dispose(); }
        }
        public static double Area(HObject region)
        {
            HTuple area = null, row = null, column = null;
            try { HOperatorSet.AreaCenter(region, out area, out row, out column); double total = 0; for (int i = 0; i < area.Length; i++) total += area[i].D; return total; }
            finally { DisposeTuple(area); DisposeTuple(row); DisposeTuple(column); }
        }
        public static void ClearModel(HTuple model)
        { if (model == null) return; try { HOperatorSet.ClearVariationModel(model); } catch { } finally { model.Dispose(); } }
        // Only fresh caller-created version files are passed here; never remove referenced applied/undo/recipe resources.
        public static void DeleteCreated(string path) { try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); } catch { } }
        private static void DisposeTuple(HTuple tuple) { if (tuple != null) tuple.Dispose(); }
    }
}
