using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmOcrValidationParametersS133
    {
        [DataMember(Order = 1)] public string Mode { get; set; } = "None";
        [DataMember(Order = 2)] public string ExpectedText { get; set; } = string.Empty;
        [DataMember(Order = 3)] public string Pattern { get; set; } = string.Empty;
        [DataMember(Order = 4)] public bool IgnoreCase { get; set; }
        [DataMember(Order = 5)] public string ModelPath { get; set; } = string.Empty;
        [DataMember(Order = 6)] public string ModelSha256 { get; set; } = string.Empty;
        [DataMember(Order = 7)] public int ClassCount { get; set; }

        [OnDeserializing]
        private void Defaults(StreamingContext context)
        {
            Mode = "None"; ExpectedText = Pattern = ModelPath = ModelSha256 = string.Empty;
        }

        public VmOcrValidationParametersS133 Clone() { return (VmOcrValidationParametersS133)MemberwiseClone(); }

        public string Validate()
        {
            if (Mode != "None" && Mode != "Exact" && Mode != "Pattern") return "OCR 文本校验模式无效。";
            if (ExpectedText == null || ExpectedText.Length > VmOcrTextValidationS133.MaximumTextLength)
                return "OCR 期望文本最多 10000 字符，不能为 null。";
            if (Pattern == null || Pattern.Length > VmOcrTextValidationS133.MaximumPatternLength)
                return "OCR 格式表达式最多 1024 字符，不能为 null。";
            if (Mode == "Pattern")
            {
                try { VmOcrTextValidationS133.CreateRegex(Pattern, IgnoreCase); }
                catch (ArgumentException ex) { return "OCR 格式表达式无效：" + ex.Message; }
            }
            bool hasPath = !string.IsNullOrWhiteSpace(ModelPath), hasHash = !string.IsNullOrWhiteSpace(ModelSha256);
            if (!hasPath && !hasHash && ClassCount == 0) return string.Empty; // Explicit legacy compatibility.
            if (!hasPath || ModelPath.Length > 4096 || !VmOcrModelResourceS133.IsSha256(ModelSha256) || ClassCount < 1 || ClassCount > 10000)
                return "OCR 受管字库路径、SHA 身份或字符类别数不完整，请从原字库入口重新导入。";
            return string.Empty;
        }
    }

    public sealed class VmOcrTextValidationResultS133
    {
        public bool Match { get; private set; }
        public string FailureReason { get; private set; }
        internal VmOcrTextValidationResultS133(bool match, string reason) { Match = match; FailureReason = reason ?? string.Empty; }
    }

    public static class VmOcrTextValidationS133
    {
        public const int MaximumTextLength = 10000;
        public const int MaximumPatternLength = 1024;
        public const int TimeoutMilliseconds = 100;

        internal static Regex CreateRegex(string pattern, bool ignoreCase)
        {
            RegexOptions options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            // Validate the expression in isolation first: otherwise unmatched user parentheses could escape the outer group.
            new Regex(pattern, options, TimeSpan.FromMilliseconds(TimeoutMilliseconds));
            // Outer absolute anchors remain effective even if the user's expression enables multiline mode.
            return new Regex("\\A(?:" + pattern + ")\\z", options, TimeSpan.FromMilliseconds(TimeoutMilliseconds));
        }

        public static VmOcrTextValidationResultS133 Evaluate(string text, VmOcrValidationParametersS133 p)
        {
            if (p == null) return new VmOcrTextValidationResultS133(false, "OCR 文本校验参数缺失。");
            string error = p.Validate();
            if (!string.IsNullOrEmpty(error)) return new VmOcrTextValidationResultS133(false, error);
            if (text == null) return new VmOcrTextValidationResultS133(false, "OCR 原始文本缺失。");
            if (text.Length > MaximumTextLength) return new VmOcrTextValidationResultS133(false, "OCR 原始文本超过 10000 字符，拒绝执行文本规则。");
            if (p.Mode == "None") return new VmOcrTextValidationResultS133(true, string.Empty);
            if (p.Mode == "Exact")
            {
                bool match = string.Equals(text, p.ExpectedText, p.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                return new VmOcrTextValidationResultS133(match, match ? string.Empty : "识别原文与期望文本不一致。");
            }
            try
            {
                bool match = CreateRegex(p.Pattern, p.IgnoreCase).IsMatch(text);
                return new VmOcrTextValidationResultS133(match, match ? string.Empty : "识别原文不满足完整文本格式。");
            }
            catch (RegexMatchTimeoutException) { return new VmOcrTextValidationResultS133(false, "OCR 格式校验超过 100 ms，按 NG 处理。"); }
            catch (ArgumentException ex) { return new VmOcrTextValidationResultS133(false, "OCR 格式表达式无效：" + ex.Message); }
        }
    }

    public sealed class VmOcrCharacterS133
    {
        public int Index { get; private set; }
        public string Text { get; private set; }
        public double Confidence { get; private set; }
        public double Row { get; private set; }
        public double Column { get; private set; }
        public double Row1 { get; private set; }
        public double Column1 { get; private set; }
        public double Row2 { get; private set; }
        public double Column2 { get; private set; }
        public bool IsLowConfidence { get; private set; }
        public bool IsRejected { get; private set; }
        public string StateText { get { return IsRejected ? (IsLowConfidence ? "拒识 · 低信" : "拒识") : IsLowConfidence ? "低信" : "正常"; } }
        public string ConfidenceText { get { return Confidence.ToString("0.######", CultureInfo.InvariantCulture); } }

        public VmOcrCharacterS133(int index, string text, double confidence, double minimumConfidence,
            double row, double column, double row1, double column1, double row2, double column2)
        {
            if (index < 1 || index > 10000 || text == null || !Finite(confidence) || confidence < 0 || confidence > 1 ||
                !Finite(row) || !Finite(column) || !Finite(row1) || !Finite(column1) || !Finite(row2) || !Finite(column2) ||
                row2 < row1 || column2 < column1)
                throw new InvalidOperationException("OCR 逐字识别结果、分数或区域几何无效，不会截断或伪造字符。");
            Index = index; Text = text; Confidence = confidence; Row = row; Column = column;
            Row1 = row1; Column1 = column1; Row2 = row2; Column2 = column2;
            IsLowConfidence = confidence < minimumConfidence;
            IsRejected = string.IsNullOrWhiteSpace(text) || text == "\u001a";
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class VmOcrModelResourceInfoS133
    {
        public string ModelPath { get; internal set; }
        public string ModelSha256 { get; internal set; }
        public int ClassCount { get; internal set; }
    }

    // The lease holds an OS read lock for the full read/classification operation; a hash check alone is insufficient.
    public sealed class VmOcrResourceLeaseS133 : IDisposable
    {
        internal FileStream Stream;
        public string ModelPath { get; internal set; }
        public string Sha256 { get; internal set; }
        public void Dispose() { if (Stream != null) { Stream.Dispose(); Stream = null; } }
    }

    public static class VmOcrModelResourceS133
    {
        public const long MaximumModelBytes = 536870912;

        public static VmOcrResourceLeaseS133 OpenResource(string path, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(path) || !string.Equals(Path.GetExtension(path), ".omc", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("OCR 字库必须为 HALCON MLP .omc 文件。");
            if (!string.IsNullOrWhiteSpace(expectedHash) && !IsSha256(expectedHash)) throw new InvalidOperationException("OCR 字库 SHA 格式无效。");
            string full = Path.GetFullPath(path);
            FileStream stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                if (stream.Length < 1 || stream.Length > MaximumModelBytes) throw new InvalidOperationException("OCR 字库为空或超过 512 MiB。");
                string hash;
                using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                stream.Position = 0;
                if (!string.IsNullOrWhiteSpace(expectedHash) && !string.Equals(expectedHash, hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("OCR 字库内容与保存的 SHA 身份不一致，请显式重新导入；不接受静默换库。");
                return new VmOcrResourceLeaseS133 { Stream = stream, ModelPath = full, Sha256 = hash };
            }
            catch { stream.Dispose(); throw; }
        }

        public static VmOcrModelResourceInfoS133 Import(string sourcePath, string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("OCR 模型版本目录缺失。", "directory");
            string destination = Path.Combine(Path.GetFullPath(directory), Guid.NewGuid().ToString("N") + ".omc");
            bool created = false;
            HTuple model = null;
            bool loaded = false;
            try
            {
                string hash;
                using (var source = OpenResource(sourcePath, string.Empty))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { created = true; source.Stream.CopyTo(target); target.Flush(true); }
                    hash = source.Sha256;
                }
                using (var resource = OpenResource(destination, hash))
                {
                    HOperatorSet.ReadOcrClassMlp(resource.ModelPath, out model); loaded = true;
                    int classes = GetClassCount(model);
                    return new VmOcrModelResourceInfoS133 { ModelPath = resource.ModelPath, ModelSha256 = resource.Sha256, ClassCount = classes };
                }
            }
            catch { if (created && File.Exists(destination)) File.Delete(destination); throw; }
            finally { try { if (loaded) HOperatorSet.ClearOcrClassMlp(model); } finally { if (model != null) model.Dispose(); } }
        }

        public static int GetClassCount(HTuple model)
        {
            HTuple width = null, height = null, interpolation = null, features = null, characters = null, hidden = null, preprocessing = null, components = null;
            try
            {
                HOperatorSet.GetParamsOcrClassMlp(model, out width, out height, out interpolation, out features, out characters, out hidden, out preprocessing, out components);
                if (characters == null || characters.Length < 1 || characters.Length > 10000 || width.I < 1 || height.I < 1)
                    throw new InvalidOperationException("OCR MLP 字库类别或字符归一化尺寸无效。" );
                return characters.Length;
            }
            finally { foreach (HTuple tuple in new[] { width, height, interpolation, features, characters, hidden, preprocessing, components }) if (tuple != null) tuple.Dispose(); }
        }

        internal static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (char c in value) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F')) return false;
            return true;
        }
    }
}
