using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;
using HalconDotNet;

namespace HalconWinFormsDemo.Services
{
    public sealed class BatchTraceStore
    {
        private readonly string directory;
        private readonly string settingsPath;

        public BatchTraceStore(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory)) throw new ArgumentException("追溯目录不能为空。", "baseDirectory");
            directory = Path.Combine(Path.GetFullPath(baseDirectory), "batch-traces");
            settingsPath = Path.Combine(Path.GetFullPath(baseDirectory), "batch-trace-settings.json");
        }

        public string DirectoryPath { get { return directory; } }

        public VmBatchTraceStorageSettings LoadSettings()
        {
            if (!File.Exists(settingsPath)) return VmBatchTraceStorageSettings.CreateDefault();
            try
            {
                using (FileStream stream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    VmBatchTraceStorageSettings value = (VmBatchTraceStorageSettings)new DataContractJsonSerializer(typeof(VmBatchTraceStorageSettings)).ReadObject(stream);
                    return (value ?? VmBatchTraceStorageSettings.CreateDefault()).Normalize();
                }
            }
            catch
            {
                return VmBatchTraceStorageSettings.CreateDefault();
            }
        }

        public void SaveSettings(VmBatchTraceStorageSettings settings)
        {
            settings = (settings ?? VmBatchTraceStorageSettings.CreateDefault()).Normalize();
            string temp = settingsPath + ".tmp";
            using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(VmBatchTraceStorageSettings)).WriteObject(stream, settings);
                stream.Flush(true);
            }
            if (File.Exists(settingsPath)) File.Replace(temp, settingsPath, settingsPath + ".bak", true); else File.Move(temp, settingsPath);
        }

        public void SaveItemAssets(VmImageBatchTraceRecord record, VmImageBatchTraceItem item, HImage sourceImage, HImage resultImage, IEnumerable<VmBatchTraceLayerCapture> layers)
        {
            if (record == null) throw new ArgumentNullException("record");
            if (item == null) throw new ArgumentNullException("item");
            Directory.CreateDirectory(directory);
            string batchFolder = ResolveAssetPath(Path.Combine("assets", SafeSegment(record.BatchId)));
            string itemFolder = Path.Combine(batchFolder, item.Sequence.ToString("0000", CultureInfo.InvariantCulture));
            if (Directory.Exists(itemFolder)) Directory.Delete(itemFolder, true);
            Directory.CreateDirectory(itemFolder);
            item.SourceImageRelativePath = string.Empty;
            item.ResultImageRelativePath = string.Empty;
            item.Layers = new List<VmBatchTraceLayerSnapshotData>();
            if (sourceImage != null)
            {
                string path = Path.Combine(itemFolder, "source.png");
                HOperatorSet.WriteImage(sourceImage, "png", 0, path);
                item.SourceImageRelativePath = ToRelative(path);
            }
            if (resultImage != null)
            {
                string path = Path.Combine(itemFolder, "result.png");
                HOperatorSet.WriteImage(resultImage, "png", 0, path);
                item.ResultImageRelativePath = ToRelative(path);
            }
            int index = 0;
            foreach (VmBatchTraceLayerCapture capture in layers ?? Enumerable.Empty<VmBatchTraceLayerCapture>())
            {
                if (capture == null || capture.Object == null) continue;
                string path = Path.Combine(itemFolder, "layer-" + index.ToString("000", CultureInfo.InvariantCulture) + ".hobj");
                HOperatorSet.WriteObject(capture.Object, path);
                VmBatchTraceLayerSnapshotData data = capture.Metadata ?? new VmBatchTraceLayerSnapshotData();
                data.Sequence = index;
                data.RelativePath = ToRelative(path);
                data.Normalize(index);
                item.Layers.Add(data);
                index++;
            }
            item.AssetState = item.SourceImageRelativePath.Length > 0 || item.ResultImageRelativePath.Length > 0 ? "Ready" : "None";
            item.AssetMessage = item.AssetState == "Ready" ? "结果快照已保存" : "未启用结果图像保存";
        }

        public string Save(VmImageBatchTraceRecord record)
        {
            if (record == null) throw new ArgumentNullException("record");
            Directory.CreateDirectory(directory);
            record.Normalize();
            string safeId = new string(record.BatchId.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());
            if (string.IsNullOrWhiteSpace(safeId)) safeId = Guid.NewGuid().ToString("N");
            string path = Path.Combine(directory, safeId + ".json");
            string temp = path + ".tmp";
            WriteJson(temp, record);
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
            else File.Move(temp, path);
            return path;
        }

        public VmBatchTraceLoadResult LoadAll()
        {
            VmBatchTraceLoadResult result = new VmBatchTraceLoadResult();
            Directory.CreateDirectory(directory);
            foreach (string path in Directory.GetFiles(directory, "*.json").OrderByDescending(File.GetLastWriteTimeUtc))
            {
                try
                {
                    VmImageBatchTraceRecord record = ReadJson(path);
                    if (record == null) throw new InvalidDataException("记录内容为空。");
                    record.Normalize();
                    AuditAssets(record);
                    result.Records.Add(record);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add(Path.GetFileName(path) + "：" + ex.Message);
                }
            }
            result.Records.Sort((left, right) => string.Compare(right.StartedAtUtc, left.StartedAtUtc, StringComparison.Ordinal));
            return result;
        }

        public HImage ReadImageAsset(string relativePath)
        {
            HObject raw = null;
            HOperatorSet.ReadImage(out raw, ResolveAssetPath(relativePath));
            HImage image = new HImage(raw);
            raw = null;
            return image;
        }

        public HObject ReadObjectAsset(string relativePath)
        {
            HObject value;
            HOperatorSet.ReadObject(out value, ResolveAssetPath(relativePath));
            return value;
        }

        public void DeleteBatch(VmImageBatchTraceRecord record)
        {
            if (record == null) return;
            string safeId = SafeSegment(record.BatchId);
            foreach (string suffix in new[] { ".json", ".json.bak" })
            {
                string path = Path.Combine(directory, safeId + suffix);
                if (File.Exists(path)) File.Delete(path);
            }
            string assetFolder = ResolveAssetPath(Path.Combine("assets", safeId));
            if (Directory.Exists(assetFolder)) Directory.Delete(assetFolder, true);
        }

        public string EnforceRetention(VmBatchTraceStorageSettings settings)
        {
            settings = (settings ?? VmBatchTraceStorageSettings.CreateDefault()).Normalize();
            VmBatchTraceLoadResult loaded = LoadAll();
            List<VmImageBatchTraceRecord> ordered = loaded.Records.OrderByDescending(item => item.StartedAtUtc).ToList();
            int removed = 0;
            while (ordered.Count > settings.MaximumBatchCount || GetDirectoryBytes(directory) > (long)settings.MaximumMegabytes * 1024L * 1024L)
            {
                if (ordered.Count == 0) break;
                VmImageBatchTraceRecord oldest = ordered[ordered.Count - 1];
                DeleteBatch(oldest);
                ordered.RemoveAt(ordered.Count - 1);
                removed++;
            }
            return string.Format(CultureInfo.InvariantCulture, "保留 {0} 批，容量 {1:0.0} MB，清理 {2} 批", ordered.Count, GetDirectoryBytes(directory) / 1048576.0, removed);
        }

        public int CleanupOrphanAssets()
        {
            string assets = ResolveAssetPath("assets");
            if (!Directory.Exists(assets)) return 0;
            HashSet<string> valid = new HashSet<string>(LoadAll().Records.Select(item => SafeSegment(item.BatchId)), StringComparer.OrdinalIgnoreCase);
            int removed = 0;
            foreach (string path in Directory.GetDirectories(assets))
            {
                if (valid.Contains(Path.GetFileName(path))) continue;
                Directory.Delete(path, true);
                removed++;
            }
            return removed;
        }

        public void ExportCsv(string path, IEnumerable<VmImageBatchTraceRecord> records)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("导出路径不能为空。", "path");
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("BatchId,RecipeName,RecipeFingerprint,StartedAtUtc,CompletedAtUtc,Scope,Outcome,TotalCount,OK,NG,Error,Stopped,NotRun,TotalElapsedMs,ItemSequence,FileName,FilePath,ItemState,ResultCode,ItemElapsedMs,Message,FlowId,FlowName");
            foreach (VmImageBatchTraceRecord record in records ?? Enumerable.Empty<VmImageBatchTraceRecord>())
            {
                record.Normalize();
                IEnumerable<VmImageBatchTraceItem> items = record.Items.Count == 0
                    ? (IEnumerable<VmImageBatchTraceItem>)new[] { new VmImageBatchTraceItem() }
                    : record.Items;
                foreach (VmImageBatchTraceItem item in items)
                {
                    string[] values =
                    {
                        record.BatchId, record.RecipeName, record.RecipeFingerprint, record.StartedAtUtc, record.CompletedAtUtc,
                        record.Scope, record.Outcome, record.TotalCount.ToString(CultureInfo.InvariantCulture), record.OkCount.ToString(CultureInfo.InvariantCulture),
                        record.NgCount.ToString(CultureInfo.InvariantCulture), record.ErrorCount.ToString(CultureInfo.InvariantCulture), record.StoppedCount.ToString(CultureInfo.InvariantCulture),
                        record.NotRunCount.ToString(CultureInfo.InvariantCulture), record.TotalElapsedMilliseconds.ToString("0.###", CultureInfo.InvariantCulture),
                        (item.Sequence + 1).ToString(CultureInfo.InvariantCulture), item.FileName, item.FilePath, item.State, item.ResultCode,
                        item.ElapsedMilliseconds.ToString("0.###", CultureInfo.InvariantCulture), item.Message, record.FlowId, record.FlowName
                    };
                    csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
                }
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
        }

        public static string HashFingerprint(string serializedRecipe)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(serializedRecipe ?? string.Empty));
                return string.Concat(hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string EscapeCsv(string value)
        {
            string text = value ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private void AuditAssets(VmImageBatchTraceRecord record)
        {
            foreach (VmImageBatchTraceItem item in record.Items)
            {
                item.OriginalSourceExists = !string.IsNullOrWhiteSpace(item.FilePath) && File.Exists(item.FilePath);
                if (string.IsNullOrWhiteSpace(item.SourceImageRelativePath) && string.IsNullOrWhiteSpace(item.ResultImageRelativePath))
                {
                    item.AssetState = "None";
                    item.AssetMessage = "该历史记录未保存图像快照";
                    continue;
                }
                try
                {
                    using (HImage source = string.IsNullOrWhiteSpace(item.SourceImageRelativePath) ? null : ReadImageAsset(item.SourceImageRelativePath)) { }
                    using (HImage result = string.IsNullOrWhiteSpace(item.ResultImageRelativePath) ? null : ReadImageAsset(item.ResultImageRelativePath)) { }
                    foreach (VmBatchTraceLayerSnapshotData layer in item.Layers)
                    {
                        using (HObject value = ReadObjectAsset(layer.RelativePath)) { }
                        layer.AssetState = "Ready";
                    }
                    item.AssetState = "Ready";
                    item.AssetMessage = item.OriginalSourceExists ? "快照完整" : "原始源文件已删除，独立快照仍可回看";
                }
                catch (FileNotFoundException ex)
                {
                    item.AssetState = "Missing";
                    item.AssetMessage = ex.Message;
                }
                catch (DirectoryNotFoundException ex)
                {
                    item.AssetState = "Missing";
                    item.AssetMessage = ex.Message;
                }
                catch (Exception ex)
                {
                    item.AssetState = "Corrupt";
                    item.AssetMessage = ex.Message;
                }
            }
        }

        private string ResolveAssetPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) throw new FileNotFoundException("追溯资源路径为空。");
            string root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("追溯资源路径越出受管目录。");
            return full;
        }

        private string ToRelative(string fullPath)
        {
            Uri root = new Uri(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(root.MakeRelativeUri(new Uri(Path.GetFullPath(fullPath))).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string SafeSegment(string value)
        {
            string result = new string((value ?? string.Empty).Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());
            return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
        }

        private static long GetDirectoryBytes(string path)
        {
            if (!Directory.Exists(path)) return 0;
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(file =>
            {
                try { return new FileInfo(file).Length; }
                catch { return 0L; }
            });
        }

        private static void WriteJson(string path, VmImageBatchTraceRecord record)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(VmImageBatchTraceRecord)).WriteObject(stream, record);
                stream.Flush(true);
            }
        }

        private static VmImageBatchTraceRecord ReadJson(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (VmImageBatchTraceRecord)new DataContractJsonSerializer(typeof(VmImageBatchTraceRecord)).ReadObject(stream);
            }
        }
    }

    public sealed class VmBatchTraceLayerCapture
    {
        public HObject Object { get; set; }
        public VmBatchTraceLayerSnapshotData Metadata { get; set; }
    }
}
