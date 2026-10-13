using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;
using Microsoft.Win32.SafeHandles;

namespace HalconWinFormsDemo.Services
{
    // A flat, bounded file store. It never owns algorithm/image handles, opens
    // SourcePath, replays a capture, changes a decision, or deletes unknown files.
    public sealed class ProductionImageEvidenceStoreS142
    {
        public const long MaximumDirectoryBytes = 1024L * 1024 * 1024;
        public const int MaximumFiles = 10000, MaximumScanEntries = 20000;
        private static readonly ConcurrentDictionary<string, object> Gates = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly string directory;
        private readonly string recordDirectory;
        private readonly object gate;
        private List<string> warnings = new List<string>();
        public string DirectoryPath { get { return directory; } }
        public IReadOnlyList<string> LastWarnings { get { lock (gate) return warnings.ToArray(); } }

        public ProductionImageEvidenceStoreS142(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory)) throw new ArgumentException("图像证据基础目录不能为空。", "baseDirectory");
            directory = Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), "production-evidence"));
            recordDirectory = Path.GetFullPath(Path.Combine(Path.GetFullPath(baseDirectory), "production-runs"));
            gate = Gates.GetOrAdd(directory, _ => new object());
            RejectPath(directory);
            // Default Off must not create a new evidence directory or write anything.
            lock (gate)
            {
                if (Directory.Exists(directory))
                {
                    try { warnings = ScanInventory().Warnings; }
                    catch (Exception ex) { warnings.Add("现有证据目录未完整确认：" + ex.Message + "；不自动清理未知或孤立资源。"); }
                }
            }
        }

        public VmProductionImageEvidenceS142 Publish(VmProductionImageEvidenceS142 metadata, Action<string> encodePng, long maximumEncodedBytes = 64L * 1024 * 1024)
        {
            if (metadata == null) throw new ArgumentNullException("metadata");
            var result = metadata.Clone();
            // Never clear a caller's invalid Saved identity into an apparently new file.
            if (result.State != "Pending") throw new InvalidDataException("仅本次 Pending 原图可发布；不会覆盖或改写已保存证据。");
            string temporary = null, destination = null; bool createdTemporary = false, moved = false;
            lock (gate)
            {
                try
                {
                    string error = result.Validate(); if (error.Length > 0) throw new InvalidDataException(error);
                    maximumEncodedBytes = Math.Min(maximumEncodedBytes, VmProductionImageEvidenceS142.MaximumEncodedBytes);
                    if (maximumEncodedBytes < 45) throw new InvalidDataException("本周期剩余图像编码预算不足。");
                    if (encodePng == null) throw new ArgumentNullException("encodePng");
                    EnsureDirectory(true);
                    var inventory = ScanInventory(); warnings = inventory.Warnings;
                    if (inventory.Files + 1 > MaximumFiles || inventory.Bytes >= MaximumDirectoryBytes)
                        throw new InvalidDataException("图像证据达到 10000 文件或 1 GiB 预算；未知/孤立文件保留，不自动腾空。");
                    string nonce = Guid.NewGuid().ToString("N");
                    temporary = ResolveChild("." + result.RunId + "-" + nonce + ".pending.png");
                    destination = ResolveChild(result.RunId + "-" + nonce + ".png");
                    using (var owned = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { createdTemporary = true; owned.Flush(true); }
                    // HALCON may overwrite this call's empty file. It is deliberately a
                    // .png path, not an arbitrary caller path or a generated source path.
                    RejectPath(temporary); encodePng(temporary); RejectPath(temporary);
                    long bytes; string sha;
                    using (var stream = OpenRead(temporary))
                    {
                        bytes = stream.Length; RequireLength(bytes);
                        if (bytes > maximumEncodedBytes) throw new InvalidDataException("本图编码超过本周期剩余字节预算，未发布部分图像。");
                        ValidatePngStream(stream, result);
                        stream.Position = 0; sha = Hash(stream);
                    }
                    inventory = ScanInventory(temporary); warnings = inventory.Warnings;
                    if (inventory.Files > MaximumFiles || inventory.Bytes > MaximumDirectoryBytes)
                        throw new InvalidDataException("编码后证据目录超过文件/1 GiB 预算，未发布本图。");
                    EnsureDirectory(false); RejectPath(destination); RejectPath(temporary);
                    File.Move(temporary, destination); createdTemporary = false; moved = true; // No overwrite or replay.
                    result.RelativePath = Path.GetFileName(destination); result.ByteLength = bytes; result.Sha256 = sha; result.State = "Saved";
                    result.Detail = "本周期源采集原图已保存；SHA 是当时字节身份，不是现场真实性认证。";
                    // Publication must not rely only on the temporary-file check.
                    using (var stream = OpenRead(destination)) VerifyStream(stream, result);
                    string finalError = result.Validate(); if (finalError.Length > 0) throw new InvalidDataException(finalError);
                    return result;
                }
                catch (Exception ex)
                {
                    // Deletion is limited to this call's created file, and after Move
                    // requires the exact frozen length/SHA, never a guessed filename.
                    if (moved)
                    {
                        try { DeleteVerified(result); moved = false; }
                        catch (Exception cleanup) { AddWarning("本次未关联图像保留残留：" + cleanup.Message); }
                    }
                    result.State = "Missing"; result.RelativePath = null; result.ByteLength = 0; result.Sha256 = null;
                    result.Detail = Bound("图像未保存：" + ex.Message, 1024);
                    return result;
                }
                finally
                {
                    if (createdTemporary && temporary != null)
                    {
                        try
                        {
                            EnsureDirectory(false); RejectPath(temporary);
                            // This exact temporary name was created by this call only.
                            if (File.Exists(temporary)) File.Delete(temporary);
                        }
                        catch (Exception ex) { AddWarning("本次临时图像未能清理，保留不删除其他文件：" + ex.Message); }
                    }
                    AttachWarnings(result);
                }
            }
        }

        public byte[] ReadVerified(VmProductionRunRecordS135 record, VmProductionImageEvidenceS142 metadata)
        {
            if (record == null || metadata == null) throw new ArgumentNullException("record/metadata");
            string recordError = record.Validate(); if (recordError.Length > 0) throw new InvalidDataException(recordError);
            if (!record.EvidenceKnownS142 || record.EvidenceImagesS142 == null || metadata.State != "Saved") throw new InvalidDataException("该周期没有明确已保存原图；不会从旧 SourcePath 补造。");
            var exact = record.EvidenceImagesS142.Where(image => image != null && SameMetadata(image, metadata)).Take(2).ToArray();
            if (exact.Length != 1 || !string.Equals(metadata.RunId, record.RunId, StringComparison.OrdinalIgnoreCase) || !string.Equals(metadata.FlowId, record.FlowId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("所选原图元数据不唯一或不属于该真实周期/流程。");
            string error = metadata.Validate(); if (error.Length > 0) throw new InvalidDataException(error);
            lock (gate)
            {
                EnsureDirectory(false);
                string recordPath = Path.Combine(recordDirectory, record.RunId + ".json");
                // Keep the original journal locked throughout this read. A retained
                // window DTO alone never authorizes reading an orphaned image.
                using (var journal = OpenRecord(recordPath))
                using (var stream = OpenRead(ResolveChild(metadata.RelativePath)))
                {
                    var actualRecord = ReadJournal(journal);
                    if (!SerializeJournal(actualRecord).SequenceEqual(SerializeJournal(record)))
                        throw new InvalidDataException("原周期记录已改变；本次窗口快照失效，请重新读取。");
                    VerifyStream(stream, metadata); stream.Position = 0;
                    byte[] bytes = new byte[checked((int)stream.Length)];
                    int offset = 0; while (offset < bytes.Length) { int read = stream.Read(bytes, offset, bytes.Length - offset); if (read == 0) throw new EndOfStreamException("图像读取中断。"); offset += read; }
                    ValidatePng(bytes, metadata); return bytes;
                }
            }
        }

        public List<string> CleanupOwned(IEnumerable<VmProductionImageEvidenceS142> images)
        {
            var result = new List<string>();
            if (images == null) return result;
            lock (gate)
            {
                int count = 0;
                foreach (var image in images)
                {
                    if (++count > 4) { result.Add("证据清理超过单周期四来源预算，其余保留不删除。"); break; }
                    if (image == null || image.State != "Saved") continue;
                    try { DeleteVerified(image); }
                    catch (Exception ex) { result.Add(Bound("旧周期原图未清理：" + ex.Message, 1024)); }
                }
            }
            return result;
        }

        public static void ValidatePng(byte[] bytes, VmProductionImageEvidenceS142 metadata)
        {
            if (bytes == null || metadata == null) throw new ArgumentNullException("bytes/metadata");
            RequireLength(bytes.LongLength);
            using (var stream = new MemoryStream(bytes, false)) ValidatePngStream(stream, metadata);
        }

        private static void ValidatePngStream(Stream stream, VmProductionImageEvidenceS142 metadata)
        {
            RequireLength(stream.Length);
            string imageError = metadata.ValidateImage(); if (imageError.Length > 0) throw new InvalidDataException(imageError);
            stream.Position = 0;
            byte[] header = ReadExactly(stream, 33);
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (!header.Take(8).SequenceEqual(signature) || UInt(header, 8) != 13 || Encoding.ASCII.GetString(header, 12, 4) != "IHDR") throw new InvalidDataException("不是受支持 PNG/IHDR 原图。");
            long width = UInt(header, 16), height = UInt(header, 20);
            int channels = header[25] == 0 ? 1 : header[25] == 2 ? 3 : 0;
            if (width != metadata.Width || height != metadata.Height || channels != metadata.Channels || header[24] != 8 || header[26] != 0 || header[27] != 0 || header[28] > 1)
                throw new InvalidDataException("PNG 原始尺寸/byte 位深/灰度或 RGB 通道与冻结源图不符；不自动转换。");
            if (Crc(header, 12, 17) != UInt(header, 29)) throw new InvalidDataException("PNG IHDR 校验不符。");
            bool data = false, ended = false; int chunks = 0;
            while (stream.Position < stream.Length)
            {
                if (++chunks > 20000) throw new InvalidDataException("PNG 分块数量超过有界预算。");
                byte[] chunk = ReadExactly(stream, 8); long length = UInt(chunk, 0); string kind = Encoding.ASCII.GetString(chunk, 4, 4);
                if (length > VmProductionImageEvidenceS142.MaximumEncodedBytes || stream.Position + length + 4 > stream.Length) throw new InvalidDataException("PNG 分块长度越界。");
                if (kind == "IDAT") data = true;
                else if (kind == "IEND") { if (length != 0 || !data) throw new InvalidDataException("PNG 结束块或像素数据缺失。"); ended = true; }
                else if (kind != "pHYs" && kind != "gAMA" && kind != "cHRM" && kind != "sRGB" && kind != "tIME" && kind != "tEXt")
                    throw new InvalidDataException("PNG 包含未支持的转换/压缩附加块：" + kind + "；不交给解码器猜测。");
                if (kind != "IDAT" && length > 65536) throw new InvalidDataException("PNG 附加块超过 64 KiB 预算。");
                stream.Seek(length + 4, SeekOrigin.Current);
                if (ended) { if (stream.Position != stream.Length) throw new InvalidDataException("PNG 结束块后仍有数据。"); break; }
            }
            if (!ended) throw new InvalidDataException("PNG 原图未完整结束。");
            stream.Position = 0;
        }

        private void DeleteVerified(VmProductionImageEvidenceS142 metadata)
        {
            string error = metadata.Validate(); if (error.Length > 0 || metadata.State != "Saved") throw new InvalidDataException(error.Length > 0 ? error : "仅清理已保存且完整验证的文件。");
            EnsureDirectory(false); string path = ResolveChild(metadata.RelativePath); RejectPath(path);
            // DELETE right + exclusive sharing keep the verified physical file open.
            // Deletion is by that same handle, not a later possibly-replaced path.
            using (var handle = CreateFile(path, 0x80000000u | 0x00010000u, 0, IntPtr.Zero, 3, 0x00200000u, IntPtr.Zero))
            {
                if (handle.IsInvalid) throw new IOException("无法独占打开待清理原图。", Marshal.GetLastWin32Error());
                CheckHandlePath(handle, path); RejectPath(path);
                using (var stream = new FileStream(handle, FileAccess.Read))
                {
                    VerifyStream(stream, metadata); CheckHandlePath(handle, path); RejectPath(path);
                    var disposition = new FileDisposition { DeleteFile = true };
                    if (!SetFileInformationByHandle(handle, 4, ref disposition, 1)) throw new IOException("已验证图像未能按句柄清理。", Marshal.GetLastWin32Error());
                }
            }
        }

        internal VmProductionRunRecordS135 DeleteVerifiedRecordS142(string path, string expectedSha)
        {
            RequireRecordPath(path); RejectPath(path);
            using (var handle = CreateFile(path, 0x80000000u | 0x00010000u, 0, IntPtr.Zero, 3, 0x00200000u, IntPtr.Zero))
            {
                if (handle.IsInvalid) throw new IOException("无法独占打开待清理周期记录。", Marshal.GetLastWin32Error());
                CheckHandlePath(handle, path); RejectPath(path);
                using (var stream = new FileStream(handle, FileAccess.Read))
                {
                    RequireRecordLength(stream.Length);
                    if (!string.Equals(Hash(stream), expectedSha, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("旧周期清理前实际字节已变，保留不删除。");
                    stream.Position = 0; var record = ReadJournal(stream);
                    if (!string.Equals(Path.GetFileNameWithoutExtension(path), record.RunId, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("待清理周期文件名与记录身份不同。");
                    CheckHandlePath(handle, path); RejectPath(path);
                    var disposition = new FileDisposition { DeleteFile = true };
                    if (!SetFileInformationByHandle(handle, 4, ref disposition, 1)) throw new IOException("已验证周期记录未能按句柄清理。", Marshal.GetLastWin32Error());
                    return record;
                }
            }
        }

        private FileStream OpenRecord(string path)
        {
            RequireRecordPath(path); RejectPath(path);
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try { CheckHandlePath(stream.SafeFileHandle, path); RejectPath(path); RequireRecordLength(stream.Length); return stream; }
            catch { stream.Dispose(); throw; }
        }
        private void RequireRecordPath(string path)
        {
            Guid run;
            if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), recordDirectory, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path) != ".json" ||
                !Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out run) || run == Guid.Empty)
                throw new InvalidDataException("原周期记录路径不是本日志根内的稳定 RunId JSON。");
        }
        private static VmProductionRunRecordS135 ReadJournal(Stream stream)
        {
            RequireRecordLength(stream.Length); stream.Position = 0;
            var record = (VmProductionRunRecordS135)JournalSerializer().ReadObject(stream);
            if (record == null) throw new InvalidDataException("原周期记录为空。");
            string error = record.Validate(); if (error.Length > 0) throw new InvalidDataException("原周期记录未通过核对：" + error);
            return record;
        }
        private static byte[] SerializeJournal(VmProductionRunRecordS135 record)
        {
            using (var stream = new MemoryStream())
            { JournalSerializer().WriteObject(stream, record); RequireRecordLength(stream.Length); return stream.ToArray(); }
        }
        private static DataContractJsonSerializer JournalSerializer()
        { return new DataContractJsonSerializer(typeof(VmProductionRunRecordS135), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 65536 }); }
        private static void RequireRecordLength(long length)
        { if (length < 1 || length > ProductionRunStoreS135.MaximumRecordBytes) throw new InvalidDataException("原周期 JSON 超出 2 MiB 或为空。"); }

        private FileStream OpenRead(string path)
        {
            RejectPath(path);
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try { CheckHandlePath(stream.SafeFileHandle, path); RejectPath(path); RequireLength(stream.Length); return stream; }
            catch { stream.Dispose(); throw; }
        }
        private static void VerifyStream(Stream stream, VmProductionImageEvidenceS142 metadata)
        {
            RequireLength(stream.Length);
            if (stream.Length != metadata.ByteLength) throw new InvalidDataException("原图文件长度与保存身份不同。");
            ValidatePngStream(stream, metadata); stream.Position = 0;
            if (!string.Equals(Hash(stream), metadata.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("原图 SHA 与保存字节身份不同，拒绝查看/删除。");
            stream.Position = 0;
        }

        private Inventory ScanInventory(string ownedTemporary = null)
        {
            EnsureDirectory(false); var inventory = new Inventory();
            if (!Directory.Exists(directory)) return inventory;
            int entries = 0;
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                if (++entries > MaximumScanEntries) throw new InvalidDataException("证据目录超过 20000 条目扫描上限，停止新增。");
                RejectPath(path);
                if (Directory.Exists(path)) throw new InvalidDataException("证据目录发现子目录，不能递归扫描或清理。");
                var info = new FileInfo(path); inventory.Files++; inventory.Bytes = checked(inventory.Bytes + info.Length);
                string name = info.Name;
                if (!IsPublishedName(name) && !string.Equals(path, ownedTemporary, StringComparison.OrdinalIgnoreCase) && inventory.Warnings.Count < 100)
                    inventory.Warnings.Add(Bound(name, 160) + "：未知/残留临时资源，计入容量且保留不删除。");
            }
            if (inventory.Files > 0) inventory.Warnings.Add("现有文件只有经对应周期和字节身份核对后才能清理；不自动清理孤立文件。");
            if (inventory.Files >= MaximumFiles || inventory.Bytes >= MaximumDirectoryBytes) inventory.Warnings.Add("证据目录已达到文件数或容量限制，不能再留图。");
            return inventory;
        }
        private sealed class Inventory { public int Files; public long Bytes; public List<string> Warnings = new List<string>(); }

        private void EnsureDirectory(bool create)
        {
            RejectPath(directory); if (File.Exists(directory)) throw new InvalidDataException("证据目录位置被文件占用。");
            if (create && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            RejectPath(directory);
        }
        private string ResolveChild(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..")
                throw new InvalidDataException("证据文件名包含目录、绝对路径或无效片段。");
            string path = Path.GetFullPath(Path.Combine(directory, name));
            if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("证据路径越出受管根。");
            RejectPath(path); return path;
        }
        private static bool IsPublishedName(string name)
        {
            Guid run;
            return name != null && name.Length == 69 && Guid.TryParseExact(name.Substring(0, 32), "N", out run) && run != Guid.Empty &&
                VmProductionImageEvidenceS142.OwnedRelativePath(name, name.Substring(0, 32));
        }
        private static void RejectPath(string path)
        {
            string full = Path.GetFullPath(path);
            if (full.StartsWith("\\\\", StringComparison.Ordinal)) throw new InvalidDataException("证据根不接受 UNC/设备路径。");
            string root = Path.GetPathRoot(full);
            foreach (string part in full.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                string stem = part.Split('.')[0].ToUpperInvariant();
                if (part.Length == 0 || part.Length > 255 || part == "." || part == ".." || part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal) ||
                    part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                    stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '1' && stem[3] <= '9')
                    throw new InvalidDataException("证据路径含含糊或保留的 Windows 文件名。");
            }
            for (string current = full; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                try { if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("证据路径含链接/重解析点。"); }
                catch (FileNotFoundException) { } catch (DirectoryNotFoundException) { }
                if (current == Path.GetPathRoot(current)) break;
            }
        }
        private static void CheckHandlePath(SafeFileHandle handle, string expected)
        {
            var buffer = new StringBuilder(32768);
            uint length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 0);
            if (length == 0 || length >= buffer.Capacity) throw new IOException("无法确认已打开文件的实际路径。", Marshal.GetLastWin32Error());
            string actual = buffer.ToString(); if (actual.StartsWith("\\\\?\\", StringComparison.Ordinal)) actual = actual.Substring(4);
            if (!string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("已打开物理文件的最终路径不是冻结的受管文件。");
        }
        private static bool SameMetadata(VmProductionImageEvidenceS142 a, VmProductionImageEvidenceS142 b)
        {
            return a.RunId == b.RunId && a.FlowId == b.FlowId && a.ToolId == b.ToolId && a.PortName == b.PortName && a.CoordinateContext == b.CoordinateContext &&
                a.SerialNumber == b.SerialNumber && a.SourcePath == b.SourcePath && a.CapturedAtUtc == b.CapturedAtUtc && a.SourceMode == b.SourceMode &&
                a.Width == b.Width && a.Height == b.Height && a.Channels == b.Channels && a.PixelType == b.PixelType && a.Domain == b.Domain && a.State == b.State &&
                a.RelativePath == b.RelativePath && a.ByteLength == b.ByteLength && a.Sha256 == b.Sha256 && a.Detail == b.Detail;
        }
        private void AddWarning(string text) { if (warnings.Count < 102) warnings.Add(Bound(text, 1024)); }
        private void AttachWarnings(VmProductionImageEvidenceS142 result)
        {
            if (warnings.Count == 0) return;
            result.Detail = Bound(Bound(result.Detail ?? "", 512) + "\n目录警告 " + warnings.Count.ToString(CultureInfo.InvariantCulture) + " 项：" +
                string.Join("；", warnings.Take(2).Select(warning => Bound(warning, 180))) + "。未知/孤立资源不会自动删除。", 1024);
        }
        private static string Bound(string text, int length) { return text.Length <= length ? text : text.Substring(0, length - 1) + "…"; }
        private static void RequireLength(long length) { if (length < 45 || length > VmProductionImageEvidenceS142.MaximumEncodedBytes) throw new InvalidDataException("PNG 编码为空/不完整或超过单文件 64 MiB。"); }
        private static string Hash(Stream stream) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static byte[] ReadExactly(Stream stream, int count)
        {
            byte[] data = new byte[count]; int offset = 0;
            while (offset < count) { int read = stream.Read(data, offset, count - offset); if (read == 0) throw new EndOfStreamException("PNG 元数据读取不完整。"); offset += read; }
            return data;
        }
        private static uint UInt(byte[] value, int start) { return (uint)value[start] << 24 | (uint)value[start + 1] << 16 | (uint)value[start + 2] << 8 | value[start + 3]; }
        private static uint Crc(byte[] data, int start, int length)
        {
            uint crc = 0xffffffff;
            for (int i = start; i < start + length; i++) { crc ^= data[i]; for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1; }
            return crc ^ 0xffffffff;
        }
        [StructLayout(LayoutKind.Sequential)] private struct FileDisposition { [MarshalAs(UnmanagedType.U1)] public bool DeleteFile; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
        private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint attributes, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle file, int infoClass, ref FileDisposition info, uint length);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetFinalPathNameByHandleW")]
        private static extern uint GetFinalPathNameByHandle(SafeFileHandle file, StringBuilder path, uint length, uint flags);
    }
}
