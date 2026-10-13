using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    // A bounded local value journal. It owns no algorithm/ROI/image handles and never replays a run.
    public sealed class ProductionRunStoreS135
    {
        public const int MaximumRecords = 10000, RetentionDays = 30, MaximumRecordBytes = 2097152;
        public const long MaximumDirectoryBytes = 268435456;
        private const int MaximumScanEntries = 20000, MaximumWarnings = 1000;
        private static readonly ConcurrentDictionary<string, object> Gates = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly string directory;
        private readonly object gate;
        private readonly ProductionImageEvidenceStoreS142 evidenceStoreS142;
        private readonly Dictionary<string, FileDescriptor> descriptorCache = new Dictionary<string, FileDescriptor>(StringComparer.OrdinalIgnoreCase);

        private sealed class FileDescriptor
        {
            internal string Path, ContentSha256;
            internal long Bytes;
            internal DateTime LastWriteUtc, StartedAtUtc, EndedAtUtc;
        }
        private sealed class Inventory
        {
            internal int FileCount;
            internal long Bytes;
            internal readonly List<FileDescriptor> ValidRecords = new List<FileDescriptor>();
            internal readonly List<string> Warnings = new List<string>();
        }

        public ProductionRunStoreS135(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory)) throw new ArgumentException("周期日志基础目录不能为空。", "baseDirectory");
            directory = Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), "production-runs"));
            gate = Gates.GetOrAdd(directory, _ => new object());
            evidenceStoreS142 = new ProductionImageEvidenceStoreS142(baseDirectory);
        }
        public string DirectoryPath { get { return directory; } }

        public VmProductionRunSaveResultS135 Save(VmProductionRunRecordS135 record)
        {
            lock (gate)
            {
                var result = new VmProductionRunSaveResultS135();
                string temporary = null; bool created = false;
                try
                {
                    var frozen = Prepare(record);
                    byte[] payload = Serialize(frozen);
                    if (payload.Length > MaximumRecordBytes) throw new InvalidDataException("周期记录超过 2 MiB，不会发布不完整 JSON。");
                    EnsureDirectory(true);
                    string destination = ResolveRecordPath(frozen.RunId);
                    if (File.Exists(destination)) { result = CheckDuplicate(destination, frozen.RunId, payload); return result; }
                    if (frozen.EndedAtUtc < DateTime.UtcNow.AddDays(-RetentionDays))
                        throw new InvalidDataException("周期结束时间已超过 30 日保留窗口，本日志不新增过期记录。");
                    Inventory inventory = ScanInventory();
                    int removed = EnforceRetention(inventory, payload.LongLength);
                    if (inventory.FileCount + 1 > MaximumRecords || inventory.Bytes + payload.LongLength > MaximumDirectoryBytes)
                        throw new InvalidDataException("无法在不删除损坏/未知文件的前提下满足 10000 文件或 256 MiB 保留预算，请显式处理目录警告。");
                    temporary = ResolveChild("." + Guid.NewGuid().ToString("N") + ".pending.tmp");
                    EnsureDirectory(false);
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { created = true; stream.Write(payload, 0, payload.Length); stream.Flush(true); }
                    EnsureDirectory(false); RejectReparseAncestors(temporary);
                    try { File.Move(temporary, destination); created = false; }
                    catch (IOException)
                    {
                        if (!File.Exists(destination)) throw;
                        // Another process may have won publication. Verify once; never retry or overwrite.
                        result = CheckDuplicate(destination, frozen.RunId, payload); return result;
                    }
                    result.Saved = true;
                    var info = new FileInfo(destination);
                    descriptorCache[destination] = new FileDescriptor { Path = destination, Bytes = info.Length, LastWriteUtc = info.LastWriteTimeUtc,
                        StartedAtUtc = frozen.StartedAtUtc, EndedAtUtc = frozen.EndedAtUtc, ContentSha256 = Hash(payload) };
                    var warnings = inventory.Warnings.Take(5).ToList();
                    if (removed > 0) warnings.Insert(0, "按 30 日/数量/容量策略清理了 " + removed + " 条已验证旧周期记录。");
                    if (inventory.Warnings.Count > 5) warnings.Add("另有 " + (inventory.Warnings.Count - 5) + " 项目录警告，请打开周期记录查看。");
                    result.Warning = string.Join(Environment.NewLine, warnings);
                }
                catch (Exception ex)
                {
                    if (result.Saved) result.Warning = JoinWarning(result.Warning, "记录已原子发布，但发布后信息未完整获取：" + ex.Message);
                    else result.ErrorMessage = "周期记录未保存：" + ex.Message;
                }
                finally
                {
                    if (created && temporary != null)
                    {
                        try { EnsureDirectory(false); RejectReparseAncestors(temporary); if (File.Exists(temporary)) File.Delete(temporary); }
                        catch (Exception ex) { result.Warning = JoinWarning(result.Warning, "本次临时文件未能清理：" + ex.Message); }
                    }
                }
                return result;
            }
        }

        public VmProductionRunReadResultS135 ReadRecent(int maxRecords = 2000)
        {
            if (maxRecords < 1 || maxRecords > MaximumRecords) throw new ArgumentOutOfRangeException("maxRecords", "读取数量须为 1–10000。");
            lock (gate)
            {
                var result = new VmProductionRunReadResultS135 { DirectoryPath = directory };
                try
                {
                    EnsureDirectory(false);
                    if (!Directory.Exists(directory)) { Warn(result, "周期日志目录尚未建立；当前没有已保存周期，不代表检测从未运行。"); return result; }
                    var paths = new List<string>(); int entries = 0;
                    foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                    {
                        if (++entries > MaximumScanEntries) { result.Truncated = true; Warn(result, "目录超过 20000 条目扫描上限，其余未读取。"); break; }
                        try
                        {
                            RejectReparseAncestors(path);
                            if (Directory.Exists(path)) { Warn(result, Path.GetFileName(path) + "：非周期子目录，未访问。"); continue; }
                            string runId;
                            if (!TryFileRunId(path, out runId)) { Warn(result, Path.GetFileName(path) + "：未知/临时文件，不当作周期记录，未删除。"); continue; }
                            paths.Add(path);
                        }
                        catch (Exception ex) { Warn(result, Path.GetFileName(path) + "：" + ex.Message); }
                    }
                    long readBytes = 0;
                    foreach (string path in paths.OrderByDescending(File.GetLastWriteTimeUtc))
                    {
                        try
                        {
                            var info = new FileInfo(path);
                            if (info.Length > MaximumRecordBytes) throw new InvalidDataException("超过 2 MiB 单记录上限，未读取。");
                            if (readBytes + info.Length > MaximumDirectoryBytes) { result.Truncated = true; Warn(result, "本次读取超过 256 MiB 总预算，其余记录未确认。"); break; }
                            readBytes += info.Length;
                            FileDescriptor descriptor; byte[] payload;
                            var record = ReadRecord(path, out descriptor, out payload);
                            descriptorCache[path] = descriptor;
                            // Keep a bounded newest set by frozen run time, not by mutable filesystem timestamps.
                            int index = result.Records.BinarySearch(record, RecordOrder.Instance);
                            if (index < 0) index = ~index;
                            result.Records.Insert(index, record);
                            if (result.Records.Count > maxRecords) { result.Records.RemoveAt(result.Records.Count - 1); result.Truncated = true; }
                        }
                        catch (Exception ex) { Warn(result, Path.GetFileName(path) + "：" + ex.Message); }
                    }
                }
                catch (Exception ex) { Warn(result, "周期日志未能完整读取：" + ex.Message); result.Truncated = true; }
                return result;
            }
        }

        public void ExportCsv(string path, IEnumerable<VmProductionRunRecordS135> records)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("CSV 路径不能为空。", "path");
            if (records == null) throw new ArgumentNullException("records");
            lock (gate)
            {
                var snapshot = new List<VmProductionRunRecordS135>(); long snapshotBytes = 0;
                foreach (var record in records)
                {
                    if (snapshot.Count >= MaximumRecords) throw new InvalidDataException("CSV 最多导出明确筛选的 10000 周期，不截断为成功。");
                    var frozen = Prepare(record);
                    byte[] serialized = Serialize(frozen);
                    if (serialized.Length > MaximumRecordBytes || snapshotBytes + serialized.LongLength > MaximumDirectoryBytes)
                        throw new InvalidDataException("CSV 筛选快照超过单周期 2 MiB 或总计 256 MiB 预算，不截断为成功。");
                    snapshotBytes += serialized.LongLength;
                    snapshot.Add(frozen);
                }
                snapshot.Sort(RecordOrder.Instance);
                string destination = Path.GetFullPath(path);
                if (!string.Equals(Path.GetExtension(destination), ".csv", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("周期导出须使用 .csv 文件。" );
                if (destination.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("CSV 不能写进受管周期日志目录或覆盖 JSON 记录。" );
                RejectReparseAncestors(destination);
                string parent = Path.GetDirectoryName(destination);
                if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent)) throw new DirectoryNotFoundException("请选择存在的 CSV 目标目录。");
                string temporary = Path.Combine(parent, ".production-csv-" + Guid.NewGuid().ToString("N") + ".tmp");
                bool created = false;
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(true), 8192, true))
                    {
                        created = true;
                        WriteCsvRow(writer, new[] { "SchemaVersion", "RunId", "StartedAtUtc", "EndedAtUtc", "RecipeName", "RecipePath", "RecipeDefinitionSha256", "RecipeWasUnsaved", "HasUnappliedDraft",
                            "FlowId", "FlowName", "WorkspaceSessionId", "Origin", "PageMode", "SourceLabel", "TerminalState", "StopReason", "ElapsedMilliseconds", "PlannedStepCount", "TotalStepCount",
                            "CapturedStepCount", "StepsTruncated", "RecordTextTruncated", "Sequence", "ToolId", "ToolName", "ToolType", "StepResultCode", "StepElapsedMilliseconds", "InputSummary", "OutputSummary", "Message", "ErrorMessage", "StepTextTruncated" });
                        foreach (var record in snapshot)
                        {
                            var steps = record.CapturedSteps.Count == 0 ? new VmProductionRunStepS135[] { null } : record.CapturedSteps.ToArray();
                            foreach (var step in steps)
                            {
                                WriteCsvRow(writer, CsvValues(record, step));
                                if (stream.Length > MaximumDirectoryBytes) throw new InvalidDataException("CSV 超过 256 MiB 导出预算；未发布部分文件。");
                            }
                        }
                        writer.Flush();
                        if (stream.Length > MaximumDirectoryBytes) throw new InvalidDataException("CSV 超过 256 MiB 导出预算；未发布部分文件。");
                        stream.Flush(true);
                    }
                    RejectReparseAncestors(destination); RejectReparseAncestors(temporary);
                    if (File.Exists(destination)) File.Replace(temporary, destination, null, true);
                    else File.Move(temporary, destination);
                    created = false;
                }
                finally
                {
                    if (created)
                    {
                        RejectReparseAncestors(temporary);
                        if (File.Exists(temporary)) File.Delete(temporary);
                    }
                }
            }
        }

        private VmProductionRunSaveResultS135 CheckDuplicate(string path, string runId, byte[] expected)
        {
            FileDescriptor descriptor; byte[] payload;
            VmProductionRunRecordS135 existing = ReadRecord(path, out descriptor, out payload);
            if (!string.Equals(existing.RunId, runId, StringComparison.OrdinalIgnoreCase) || !payload.SequenceEqual(expected))
                return new VmProductionRunSaveResultS135 { ErrorMessage = "RunId 已存在但内容不同，拒绝覆盖已有周期记录。" };
            return new VmProductionRunSaveResultS135 { Saved = true, Duplicate = true, Warning = "同一 RunId 记录已存在且内容一致，未重复写入。" };
        }

        private Inventory ScanInventory()
        {
            EnsureDirectory(false);
            var inventory = new Inventory(); int entries = 0; long readBytes = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                if (++entries > MaximumScanEntries) throw new InvalidDataException("周期目录超过 20000 条目扫描上限，不再写入。");
                RejectReparseAncestors(path);
                if (Directory.Exists(path)) throw new InvalidDataException("受管周期目录必须平铺；发现子目录，不递归读写或清理。");
                var info = new FileInfo(path);
                seen.Add(path);
                inventory.FileCount++; inventory.Bytes = checked(inventory.Bytes + info.Length);
                string runId;
                if (!TryFileRunId(path, out runId)) { inventory.Warnings.Add(info.Name + "：未知/残留临时文件，保留不删除。"); continue; }
                try
                {
                    FileDescriptor descriptor;
                    if (!descriptorCache.TryGetValue(path, out descriptor) || descriptor.Bytes != info.Length || descriptor.LastWriteUtc != info.LastWriteTimeUtc)
                    {
                        if (info.Length > MaximumRecordBytes) throw new InvalidDataException("超过 2 MiB 单记录上限");
                        if (readBytes + info.Length > MaximumDirectoryBytes) throw new InvalidDataException("本次目录验证达到 256 MiB 读取预算，未确认此文件");
                        readBytes += info.Length;
                        byte[] payload; ReadRecord(path, out descriptor, out payload); descriptorCache[path] = descriptor;
                    }
                    inventory.ValidRecords.Add(descriptor);
                }
                catch (Exception ex) { inventory.Warnings.Add(info.Name + "：" + ex.Message + "；损坏记录保留不删除。"); }
            }
            foreach (string stale in descriptorCache.Keys.Where(path => !seen.Contains(path)).ToArray()) descriptorCache.Remove(stale);
            return inventory;
        }

        private int EnforceRetention(Inventory inventory, long incomingBytes)
        {
            int removed = 0;
            DateTime cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
            foreach (var descriptor in inventory.ValidRecords.OrderBy(value => value.StartedAtUtc).ThenBy(value => value.Path, StringComparer.OrdinalIgnoreCase))
            {
                if (descriptor.EndedAtUtc >= cutoff && inventory.FileCount + 1 <= MaximumRecords && inventory.Bytes + incomingBytes <= MaximumDirectoryBytes) continue;
                try
                {
                    FileDescriptor fresh; byte[] payload;
                    var freshRecord = ReadRecord(descriptor.Path, out fresh, out payload); // Revalidate before deletion; cached metadata never authorizes deleting bad files.
                    if (fresh.ContentSha256 != descriptor.ContentSha256) { inventory.Warnings.Add(Path.GetFileName(descriptor.Path) + "：清理前内容变化，保留不删除。"); descriptorCache[descriptor.Path] = fresh; continue; }
                    EnsureDirectory(false); RejectReparseAncestors(descriptor.Path);
                    freshRecord = evidenceStoreS142.DeleteVerifiedRecordS142(descriptor.Path, fresh.ContentSha256);
                    inventory.FileCount--; inventory.Bytes -= fresh.Bytes; descriptorCache.Remove(descriptor.Path); removed++;
                    // A successfully deleted, freshly validated JSON authorizes only
                    // its exact Saved image identities. Unknown/bad/unlinked images stay.
                    foreach (string warning in evidenceStoreS142.CleanupOwned(freshRecord.EvidenceImagesS142)) inventory.Warnings.Add(warning);
                }
                catch (Exception ex) { inventory.Warnings.Add(Path.GetFileName(descriptor.Path) + "：旧记录未清理：" + ex.Message); }
            }
            return removed;
        }

        private VmProductionRunRecordS135 ReadRecord(string path, out FileDescriptor descriptor, out byte[] payload)
        {
            EnsureDirectory(false); RejectReparseAncestors(path);
            string fileId;
            if (!TryFileRunId(path, out fileId)) throw new InvalidDataException("文件名不是严格 GUID N.json。" );
            path = ResolveRecordPath(fileId);
            FileInfo info = new FileInfo(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 1 || stream.Length > MaximumRecordBytes) throw new InvalidDataException("记录为空或超过 2 MiB。");
                using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); payload = buffer.ToArray(); }
                using (var buffer = new MemoryStream(payload, false))
                {
                    var value = (VmProductionRunRecordS135)Serializer().ReadObject(buffer);
                    if (value == null) throw new InvalidDataException("周期记录内容为空。" );
                    string error = value.Validate();
                    if (!string.IsNullOrEmpty(error)) throw new InvalidDataException(error);
                    if (!string.Equals(value.RunId, fileId, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("文件名 RunId 与记录内部身份不一致。" );
                    // Read is deliberately not NormalizeBounded: corrupt/unsupported values are never silently repaired.
                    descriptor = new FileDescriptor { Path = path, Bytes = payload.LongLength, LastWriteUtc = info.LastWriteTimeUtc,
                        StartedAtUtc = value.StartedAtUtc, EndedAtUtc = value.EndedAtUtc, ContentSha256 = Hash(payload) };
                    return value;
                }
            }
        }

        private static VmProductionRunRecordS135 Prepare(VmProductionRunRecordS135 record)
        {
            if (record == null) throw new ArgumentNullException("record");
            if (record.CapturedSteps != null && record.TotalStepCount < record.CapturedSteps.Count)
                throw new InvalidDataException("真实步骤总数小于已有快照数量，不能通过截断掩盖矛盾。");
            if (record.EvidenceImagesS142 != null && record.EvidenceImagesS142.Count > 4)
                throw new InvalidDataException("图像证据超过四个来源，不能截断掩盖丢图。");
            var frozen = record.Clone().NormalizeBounded();
            string error = frozen.Validate();
            if (!string.IsNullOrEmpty(error)) throw new InvalidDataException(error);
            return frozen;
        }
        private static DataContractJsonSerializer Serializer()
        { return new DataContractJsonSerializer(typeof(VmProductionRunRecordS135), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 65536 }); }
        private static byte[] Serialize(VmProductionRunRecordS135 value)
        { using (var stream = new MemoryStream()) { Serializer().WriteObject(stream, value); return stream.ToArray(); } }
        private static string Hash(byte[] value)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(value)).Replace("-", string.Empty).ToLowerInvariant(); }

        private void EnsureDirectory(bool create)
        {
            RejectReparseAncestors(directory);
            if (File.Exists(directory)) throw new InvalidDataException("周期日志目录位置被文件占用。" );
            if (create && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            RejectReparseAncestors(directory);
        }
        private string ResolveRecordPath(string runId)
        {
            Guid id;
            if (!Guid.TryParseExact(runId, "N", out id) || id == Guid.Empty) throw new InvalidDataException("RunId 不符合 GUID N 文件契约。" );
            return ResolveChild(id.ToString("N") + ".json");
        }
        private string ResolveChild(string filename)
        {
            if (filename != Path.GetFileName(filename)) throw new InvalidDataException("周期文件名含目录片段。" );
            string full = Path.GetFullPath(Path.Combine(directory, filename));
            if (!string.Equals(Path.GetDirectoryName(full), directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("周期路径越出受管目录。" );
            return full;
        }
        private bool TryFileRunId(string path, out string id)
        {
            id = null;
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), directory, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)) return false;
            Guid guid;
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out guid) || guid == Guid.Empty) return false;
            id = guid.ToString("N"); return true;
        }
        private static void RejectReparseAncestors(string path)
        {
            string current = Path.GetFullPath(path);
            while (!string.IsNullOrWhiteSpace(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("拒绝重解析点、链接或目录接合路径：" + current);
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                string parent = Path.GetDirectoryName(current);
                if (string.Equals(current, parent, StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }
        }

        private sealed class RecordOrder : IComparer<VmProductionRunRecordS135>
        {
            internal static readonly RecordOrder Instance = new RecordOrder();
            public int Compare(VmProductionRunRecordS135 left, VmProductionRunRecordS135 right)
            { int order = right.StartedAtUtc.CompareTo(left.StartedAtUtc); return order != 0 ? order : string.Compare(left.RunId, right.RunId, StringComparison.OrdinalIgnoreCase); }
        }
        private static void Warn(VmProductionRunReadResultS135 result, string message)
        {
            if (result.Warnings.Count < MaximumWarnings) result.Warnings.Add(message);
            else if (result.Warnings.Count == MaximumWarnings) { result.Warnings.Add("其余警告超过 1000 项，已省略；读取并非完整无错误。"); result.Truncated = true; }
        }
        private static string JoinWarning(string left, string right) { return string.IsNullOrEmpty(left) ? right : left + Environment.NewLine + right; }

        private static IEnumerable<string> CsvValues(VmProductionRunRecordS135 record, VmProductionRunStepS135 step)
        {
            return new[] { record.SchemaVersion.ToString(CultureInfo.InvariantCulture), record.RunId, record.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture), record.EndedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                record.RecipeName, record.RecipePath, record.RecipeDefinitionSha256, record.RecipeWasUnsaved.ToString(), record.HasUnappliedDraft.ToString(), record.FlowId, record.FlowName, record.WorkspaceSessionId,
                record.Origin.ToString(), record.PageMode, record.SourceLabel, record.TerminalState, record.StopReason, record.ElapsedMilliseconds.ToString("R", CultureInfo.InvariantCulture), record.PlannedStepCount.ToString(CultureInfo.InvariantCulture),
                record.TotalStepCount.ToString(CultureInfo.InvariantCulture), record.CapturedSteps.Count.ToString(CultureInfo.InvariantCulture), record.StepsTruncated.ToString(), record.TextTruncated.ToString(),
                step == null ? "" : step.Sequence.ToString(CultureInfo.InvariantCulture), step == null ? "" : step.ToolId, step == null ? "" : step.ToolName, step == null ? "" : step.ToolType, step == null ? "" : step.ResultCode,
                step == null ? "" : step.ElapsedMilliseconds.ToString("R", CultureInfo.InvariantCulture), step == null ? "" : step.InputSummary, step == null ? "" : step.OutputSummary, step == null ? "" : step.Message,
                step == null ? "" : step.ErrorMessage, step == null ? "" : step.TextTruncated.ToString() };
        }
        private static void WriteCsvRow(TextWriter writer, IEnumerable<string> fields)
        { writer.WriteLine(string.Join(",", fields.Select(CsvCell))); }
        private static string CsvCell(string value)
        {
            string text = value ?? string.Empty;
            if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsControl(text[0]) || text[0] == '\ufeff' || "=+-@".IndexOf(text[0]) >= 0)) text = "'" + text;
            var safe = new StringBuilder();
            foreach (char c in text)
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t') safe.Append("\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else safe.Append(c);
            return "\"" + safe.ToString().Replace("\"", "\"\"") + "\"";
        }
    }
}
