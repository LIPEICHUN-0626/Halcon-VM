using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    // Local, bounded occurrence state only. No device recovery, execution, retries or outbound transport.
    public sealed class ProductionAlarmStoreS136
    {
        public const int MaximumRecords = 2000, MaximumRecordBytes = 32768, RetentionDays = 30;
        public const long MaximumDirectoryBytes = 67108864;
        private const int MaximumScanEntries = 4000, MaximumWarnings = 500;
        private static readonly ConcurrentDictionary<string, object> Gates = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly string directory;
        private readonly object gate;
        private readonly Dictionary<string, Descriptor> cache = new Dictionary<string, Descriptor>(StringComparer.OrdinalIgnoreCase);
        private sealed class Descriptor
        {
            internal string Path, Sha256;
            internal long Bytes;
            internal DateTime LastWriteUtc;
            internal VmProductionAlarmRecordS136 Record;
        }
        private sealed class Inventory
        {
            internal int Count;
            internal long Bytes;
            internal readonly List<Descriptor> Valid = new List<Descriptor>();
            internal readonly List<string> Warnings = new List<string>();
        }

        public ProductionAlarmStoreS136(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory)) throw new ArgumentException("报警基础目录不能为空。", "baseDirectory");
            directory = Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), "production-alarms"));
            gate = Gates.GetOrAdd(directory, _ => new object());
        }
        public string DirectoryPath { get { return directory; } }

        public VmProductionAlarmMutationResultS136 Add(VmProductionAlarmRecordS136 record)
        {
            return Mutate(delegate
            {
                if (record == null) throw new ArgumentNullException("record");
                var frozen = record.Clone().NormalizeBounded();
                RequireValid(frozen);
                if (frozen.State != VmProductionAlarmStateS136.Open || frozen.Version != 1)
                    throw new InvalidDataException("新增报警必须是本次新发生的未确认事实；不能导入确认/处置状态。");
                string path = RecordPath(frozen.OccurrenceId);
                if (File.Exists(path))
                {
                    Descriptor existing = ReadDescriptor(path);
                    if (!SameOccurrence(existing.Record, frozen))
                        return Failure("发生 ID 已存在但冻结事实不同，拒绝覆盖或合并不同 RunId。", existing.Record);
                    return new VmProductionAlarmMutationResultS136 { Success = true, Record = existing.Record.Clone(), Warning = "同一发生 ID 的冻结事实已存在，保留原确认/处置状态，未重复创建。" };
                }
                byte[] payload = SerializeBounded(frozen);
                Inventory inventory = ScanInventory();
                EnsureCapacity(inventory, 1, payload.LongLength, null);
                Publish(path, payload, false);
                Remember(path, frozen, payload);
                return Success(frozen, inventory.Warnings);
            });
        }

        public VmProductionAlarmMutationResultS136 Acknowledge(string id, int expectedVersion)
        {
            return Transition(id, expectedVersion, null, false);
        }
        public VmProductionAlarmMutationResultS136 Handle(string id, int expectedVersion, string note)
        {
            return Transition(id, expectedVersion, note, true);
        }

        private VmProductionAlarmMutationResultS136 Transition(string id, int expectedVersion, string note, bool handling)
        {
            return Mutate(delegate
            {
                if (expectedVersion < 1 || expectedVersion > 3) throw new InvalidDataException("打开时事件版本非法。");
                string path = RecordPath(id);
                Descriptor original = ReadDescriptor(path);
                var current = original.Record;
                if (current.Version != expectedVersion)
                    return Failure("报警版本已变化，请重新查看选定事件；未修改任何状态。", current);
                if (handling && (string.IsNullOrWhiteSpace(note) || note.Length > VmProductionAlarmRecordS136.MaximumHandlingNoteCharacters))
                    return Failure("已处置必须填写 1–2048 字符的明确人工备注，不能把确认当作设备恢复。", current);
                if (!handling && current.State != VmProductionAlarmStateS136.Open)
                    return new VmProductionAlarmMutationResultS136 { Success = true, Record = current.Clone(), Warning = "此事件已经确认；未新增确认时间，也未改变设备或运行保护。" };
                if (handling && current.State == VmProductionAlarmStateS136.Open)
                    return Failure("请先明确确认选定事件，再记录人工处置。", current);
                if (handling && current.State == VmProductionAlarmStateS136.Handled)
                {
                    if (!string.Equals(current.HandlingNote, note, StringComparison.Ordinal)) return Failure("此事件已经处置，不能覆盖既有处置说明。", current);
                    return new VmProductionAlarmMutationResultS136 { Success = true, Record = current.Clone(), Warning = "此事件已保留相同处置说明，未重复修改。" };
                }
                var changed = current.Clone();
                changed.Version++;
                if (handling)
                {
                    changed.State = VmProductionAlarmStateS136.Handled;
                    changed.HandledAtUtc = DateTime.UtcNow;
                    changed.HandlingNote = note; // Never truncate or normalize original occurrence facts on transition.
                }
                else
                {
                    changed.State = VmProductionAlarmStateS136.Acknowledged;
                    changed.AcknowledgedAtUtc = DateTime.UtcNow;
                }
                RequireValid(changed);
                byte[] payload = SerializeBounded(changed);
                Inventory inventory = ScanInventory();
                EnsureCapacity(inventory, 0, payload.LongLength - original.Bytes, path);
                Descriptor fresh = ReadDescriptor(path);
                if (fresh.Record.Version != expectedVersion || fresh.Sha256 != original.Sha256)
                    return Failure("写入前事件事实或版本发生变化，拒绝覆盖，请重新查看。", fresh.Record);
                Publish(path, payload, true);
                Remember(path, changed, payload);
                return Success(changed, inventory.Warnings);
            });
        }

        // An owned CreateNew lock also prevents two application processes from winning the same CAS.
        // Busy means one explicit failure, not a hidden spin/wait/retry loop.
        private VmProductionAlarmMutationResultS136 Mutate(Func<VmProductionAlarmMutationResultS136> action)
        {
            lock (gate)
            {
                try
                {
                    EnsureDirectory(true);
                    string lockPath = Child(".mutation.lock");
                    RejectReparseAncestors(lockPath);
                    using (var mutationLock = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose))
                    { return action(); }
                }
                catch (Exception ex) { return Failure("报警状态未保存：" + ex.Message, null); }
            }
        }

        public VmProductionAlarmReadResultS136 ReadRecent()
        {
            lock (gate)
            {
                var result = new VmProductionAlarmReadResultS136 { DirectoryPath = directory };
                try
                {
                    EnsureDirectory(false);
                    if (!Directory.Exists(directory)) return result;
                    int entries = 0, candidates = 0; long readBytes = 0;
                    foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                    {
                        if (++entries > MaximumScanEntries) { result.Truncated = true; Warn(result, "报警目录超过 4000 条目扫描上限，未读数量并非全量。"); break; }
                        try
                        {
                            RejectReparseAncestors(path);
                            if (Directory.Exists(path)) { result.Truncated = true; Warn(result, Path.GetFileName(path) + "：非受管子目录，未访问。"); continue; }
                            if (string.Equals(Path.GetFileName(path), ".mutation.lock", StringComparison.OrdinalIgnoreCase))
                            { result.Truncated = true; Warn(result, "另一进程正在修改报警，当前读取不保证完整状态快照。"); continue; }
                            string id;
                            if (!TryRecordId(path, out id)) { Warn(result, Path.GetFileName(path) + "：未知/临时文件，未当作报警，也未删除。"); continue; }
                            if (++candidates > MaximumRecords) { result.Truncated = true; Warn(result, "受管候选超过 2000 条读取上限，未读数量并非全量。"); break; }
                            var info = new FileInfo(path);
                            if (info.Length > MaximumRecordBytes) throw new InvalidDataException("超过 32 KiB 单事件上限。");
                            if (readBytes + info.Length > MaximumDirectoryBytes) { result.Truncated = true; Warn(result, "本次读取达到 64 MiB 总预算，其余报警未确认。"); break; }
                            readBytes += info.Length;
                            Descriptor descriptor = ReadDescriptor(path);
                            CacheDescriptor(path, descriptor);
                            result.Records.Add(descriptor.Record.Clone());
                        }
                        catch (Exception ex) { result.Truncated = true; Warn(result, Path.GetFileName(path) + "：" + ex.Message + "；损坏/不可读文件未删除或修补。"); }
                    }
                    result.Records.Sort((left, right) => { int order = right.OccurredAtUtc.CompareTo(left.OccurredAtUtc); return order != 0 ? order : string.Compare(left.OccurrenceId, right.OccurrenceId, StringComparison.OrdinalIgnoreCase); });
                }
                catch (Exception ex) { result.Truncated = true; Warn(result, "报警记录未完整读取：" + ex.Message); }
                return result;
            }
        }

        private Inventory ScanInventory()
        {
            var result = new Inventory(); int entries = 0; long readBytes = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                if (++entries > MaximumScanEntries) throw new InvalidDataException("报警目录超过 4000 条目扫描上限，拒绝继续写入。");
                RejectReparseAncestors(path);
                if (Directory.Exists(path)) throw new InvalidDataException("报警目录只支持受管平铺文件，发现子目录，不递归访问或清理。");
                if (string.Equals(Path.GetFileName(path), ".mutation.lock", StringComparison.OrdinalIgnoreCase)) continue;
                var info = new FileInfo(path); seen.Add(path);
                result.Count++; result.Bytes = checked(result.Bytes + info.Length);
                string id;
                if (!TryRecordId(path, out id)) { result.Warnings.Add(info.Name + "：未知/临时文件，保留不删除。"); continue; }
                try
                {
                    Descriptor descriptor;
                    if (!cache.TryGetValue(path, out descriptor) || descriptor.Bytes != info.Length || descriptor.LastWriteUtc != info.LastWriteTimeUtc)
                    {
                        if (info.Length > MaximumRecordBytes) throw new InvalidDataException("超过 32 KiB 单事件上限");
                        if (readBytes + info.Length > MaximumDirectoryBytes) throw new InvalidDataException("本次目录验证达到 64 MiB 读取预算");
                        readBytes += info.Length;
                        descriptor = ReadDescriptor(path); CacheDescriptor(path, descriptor);
                    }
                    result.Valid.Add(descriptor);
                }
                catch (Exception ex) { result.Warnings.Add(info.Name + "：" + ex.Message + "；未确认文件保留不删除。"); }
            }
            foreach (string stale in cache.Keys.Where(path => !seen.Contains(path)).ToArray()) cache.Remove(stale);
            return result;
        }

        private void EnsureCapacity(Inventory inventory, int incomingCount, long deltaBytes, string excludedPath)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-RetentionDays); int removed = 0;
            foreach (Descriptor descriptor in inventory.Valid.Where(item => item.Record.State == VmProductionAlarmStateS136.Handled)
                .OrderBy(item => item.Record.HandledAtUtc).ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(descriptor.Path, excludedPath, StringComparison.OrdinalIgnoreCase)) continue;
                bool expired = descriptor.Record.HandledAtUtc.Value < cutoff;
                if (!expired && inventory.Count + incomingCount <= MaximumRecords && inventory.Bytes + deltaBytes <= MaximumDirectoryBytes) continue;
                try
                {
                    Descriptor fresh = ReadDescriptor(descriptor.Path);
                    if (fresh.Sha256 != descriptor.Sha256 || fresh.Record.State != VmProductionAlarmStateS136.Handled)
                    { inventory.Warnings.Add(Path.GetFileName(descriptor.Path) + "：清理前事实变化，保留不删除。"); continue; }
                    EnsureDirectory(false); RejectReparseAncestors(descriptor.Path);
                    File.Delete(descriptor.Path); cache.Remove(descriptor.Path);
                    inventory.Count--; inventory.Bytes -= fresh.Bytes; removed++;
                }
                catch (Exception ex) { inventory.Warnings.Add(Path.GetFileName(descriptor.Path) + "：旧已处置记录未清理：" + ex.Message); }
            }
            if (removed > 0) inventory.Warnings.Insert(0, "按保留策略清理了 " + removed + " 条已验证已处置旧事件；未确认/待处置事件未删除。");
            if (inventory.Count + incomingCount > MaximumRecords || inventory.Bytes + deltaBytes > MaximumDirectoryBytes)
                throw new InvalidDataException("无法在保留全部未处置/损坏/未知文件的前提下满足 2000 文件或 64 MiB 预算；状态未发布，请显式处理容量。");
        }

        private void Publish(string destination, byte[] payload, bool replacing)
        {
            string temporary = Child("." + Guid.NewGuid().ToString("N") + ".pending.tmp"); bool created = false;
            try
            {
                EnsureDirectory(false); RejectReparseAncestors(destination);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { created = true; stream.Write(payload, 0, payload.Length); stream.Flush(true); }
                EnsureDirectory(false); RejectReparseAncestors(destination); RejectReparseAncestors(temporary);
                if (replacing) File.Replace(temporary, destination, null, true);
                else File.Move(temporary, destination);
                created = false;
            }
            finally
            {
                if (created) { EnsureDirectory(false); RejectReparseAncestors(temporary); if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }

        private Descriptor ReadDescriptor(string path)
        {
            EnsureDirectory(false); RejectReparseAncestors(path);
            string id;
            if (!TryRecordId(path, out id)) throw new InvalidDataException("文件名必须为非空 GUID N.json。");
            var info = new FileInfo(path); byte[] payload;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 1 || stream.Length > MaximumRecordBytes) throw new InvalidDataException("事件记录为空或超过 32 KiB。");
                using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); payload = buffer.ToArray(); }
                using (var buffer = new MemoryStream(payload, false))
                {
                    var record = (VmProductionAlarmRecordS136)Serializer().ReadObject(buffer);
                    RequireValid(record); // Never normalize corrupt persisted facts into apparently valid events.
                    if (!string.Equals(id, record.OccurrenceId, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("发生 ID 与文件名不一致。");
                    return new Descriptor { Path = path, Bytes = payload.LongLength, LastWriteUtc = info.LastWriteTimeUtc, Sha256 = Hash(payload), Record = record };
                }
            }
        }
        private void Remember(string path, VmProductionAlarmRecordS136 record, byte[] payload)
        {
            // Publication already succeeded. Cache failures must not turn that fact into a false save failure.
            try { var info = new FileInfo(path); CacheDescriptor(path, new Descriptor { Path = path, Bytes = payload.LongLength, LastWriteUtc = info.LastWriteTimeUtc, Sha256 = Hash(payload), Record = record.Clone() }); }
            catch { cache.Remove(path); }
        }
        private void CacheDescriptor(string path, Descriptor descriptor)
        {
            if (cache.Count >= MaximumScanEntries && !cache.ContainsKey(path)) cache.Clear();
            cache[path] = descriptor;
        }
        private static bool SameOccurrence(VmProductionAlarmRecordS136 left, VmProductionAlarmRecordS136 right)
        {
            var original = left.Clone(); original.State = VmProductionAlarmStateS136.Open; original.Version = 1;
            original.AcknowledgedAtUtc = null; original.HandledAtUtc = null; original.HandlingNote = string.Empty;
            return SerializeBounded(original).SequenceEqual(SerializeBounded(right));
        }
        private static byte[] SerializeBounded(VmProductionAlarmRecordS136 record)
        {
            using (var stream = new MemoryStream())
            {
                Serializer().WriteObject(stream, record);
                if (stream.Length > MaximumRecordBytes) throw new InvalidDataException("事件记录超过 32 KiB，未发布不完整 JSON。");
                return stream.ToArray();
            }
        }
        private static DataContractJsonSerializer Serializer()
        { return new DataContractJsonSerializer(typeof(VmProductionAlarmRecordS136), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 4096 }); }
        private static void RequireValid(VmProductionAlarmRecordS136 record)
        { if (record == null) throw new InvalidDataException("报警记录为空。"); string error = record.Validate(); if (!string.IsNullOrEmpty(error)) throw new InvalidDataException(error); }
        private static string Hash(byte[] payload)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", string.Empty).ToLowerInvariant(); }
        private string RecordPath(string id)
        { if (!VmProductionAlarmRecordS136.IsGuidN(id)) throw new InvalidDataException("发生 ID 必须为非空 GUID N。"); return Child(Guid.ParseExact(id, "N").ToString("N") + ".json"); }
        private string Child(string filename)
        {
            if (filename != Path.GetFileName(filename)) throw new InvalidDataException("报警文件名含目录片段。");
            string path = Path.GetFullPath(Path.Combine(directory, filename));
            if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("报警路径越出受管目录。");
            return path;
        }
        private bool TryRecordId(string path, out string id)
        {
            id = null;
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), directory, StringComparison.OrdinalIgnoreCase) || !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)) return false;
            string value = Path.GetFileNameWithoutExtension(path);
            if (!VmProductionAlarmRecordS136.IsGuidN(value)) return false;
            id = Guid.ParseExact(value, "N").ToString("N"); return true;
        }
        private void EnsureDirectory(bool create)
        {
            RejectReparseAncestors(directory);
            if (File.Exists(directory)) throw new InvalidDataException("报警目录位置被普通文件占用。");
            if (create && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            RejectReparseAncestors(directory);
        }
        private static void RejectReparseAncestors(string path)
        {
            string current = Path.GetFullPath(path);
            while (!string.IsNullOrWhiteSpace(current))
            {
                try { if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("拒绝报警重解析点/链接路径：" + current); }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                string parent = Path.GetDirectoryName(current); if (string.Equals(current, parent, StringComparison.OrdinalIgnoreCase)) break; current = parent;
            }
        }
        private static VmProductionAlarmMutationResultS136 Failure(string message, VmProductionAlarmRecordS136 record)
        { return new VmProductionAlarmMutationResultS136 { ErrorMessage = message, Record = record == null ? null : record.Clone() }; }
        private static VmProductionAlarmMutationResultS136 Success(VmProductionAlarmRecordS136 record, List<string> warnings)
        {
            var selected = warnings.Take(5).ToList();
            if (warnings.Count > 5) selected.Add("另有 " + (warnings.Count - 5) + " 项目录警告，请打开报警记录查看。");
            return new VmProductionAlarmMutationResultS136 { Success = true, Record = record.Clone(), Warning = string.Join(Environment.NewLine, selected) };
        }
        private static void Warn(VmProductionAlarmReadResultS136 result, string message)
        {
            if (result.Warnings.Count < MaximumWarnings) result.Warnings.Add(message);
            else if (result.Warnings.Count == MaximumWarnings) { result.Warnings.Add("其余报警目录警告超过 500 项，已省略。"); result.Truncated = true; }
        }
    }
}
