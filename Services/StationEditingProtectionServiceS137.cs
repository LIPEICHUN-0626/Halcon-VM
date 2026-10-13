using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    // Application editing guard, not an OS/file security boundary or an extension sandbox.
    public sealed class StationEditingProtectionServiceS137
    {
        public const int Iterations = 600000, MaximumPolicyBytes = 65536, MaximumApprovedRecipes = 32;
        private const int VerificationBytes = 32, SaltBytes = 32, CooldownSeconds = 30;
        private static readonly ConcurrentDictionary<string, object> Gates = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate;
        private readonly string directory, policyPath;
        private Policy policy;
        private string loadedFileSha256;
        private bool loaded, faulted = true, credentialOperation;
        private long generation = 1, authorizedUntilTick, cooldownUntilTick;
        private DateTime? unlockedUntilUtc;
        private int failedAttempts;
        private string status = "编辑保护策略尚未读取，暂禁止工程编辑。";

        [DataContract]
        private sealed class Policy
        {
            [DataMember(Order = 1, IsRequired = true)] public int SchemaVersion { get; set; } = 1;
            [DataMember(Order = 2, IsRequired = true)] public int Version { get; set; } = 1;
            [DataMember(Order = 3, IsRequired = true)] public bool Enabled { get; set; }
            [DataMember(Order = 4, IsRequired = true)] public int UnlockMinutes { get; set; } = 30;
            [DataMember(Order = 5, IsRequired = true)] public int Pbkdf2Iterations { get; set; } = Iterations;
            [DataMember(Order = 6, IsRequired = true)] public string Salt { get; set; } = string.Empty;
            [DataMember(Order = 7, IsRequired = true)] public string Verification { get; set; } = string.Empty;
            [DataMember(Order = 8, IsRequired = true)] public List<VmApprovedStationRecipeS137> ApprovedRecipes { get; set; } = new List<VmApprovedStationRecipeS137>();
            public Policy Clone()
            {
                var copy = (Policy)MemberwiseClone();
                copy.ApprovedRecipes = ApprovedRecipes == null ? null : ApprovedRecipes.Select(item => item == null ? null : item.Clone()).ToList();
                return copy;
            }
        }
        private sealed class LeaseChangedException : Exception { }

        public StationEditingProtectionServiceS137(string stateDirectory)
        {
            if (string.IsNullOrWhiteSpace(stateDirectory)) throw new ArgumentException("工位状态目录不能为空。", "stateDirectory");
            directory = Path.GetFullPath(stateDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (directory.Length == 2 && directory[1] == ':') directory += Path.DirectorySeparatorChar;
            policyPath = Path.GetFullPath(Path.Combine(directory, "station-editing-protection.json"));
            gate = Gates.GetOrAdd(policyPath, _ => new object());
            // Construction resolves paths only. The caller must explicitly Reload before compatibility is granted.
        }

        public VmStationProtectionSnapshotS137 Reload()
        {
            lock (gate)
            {
                InvalidateSession("重新加载策略，旧工程授权已失效。");
                try
                {
                    byte[] payload = ReadPolicyBytes();
                    if (payload == null)
                    {
                        policy = new Policy(); loadedFileSha256 = null; loaded = true; faulted = false;
                        status = "未启用编辑保护；本机未配置凭据。";
                    }
                    else
                    {
                        Policy candidate;
                        using (var stream = new MemoryStream(payload, false)) candidate = (Policy)Serializer().ReadObject(stream);
                        ValidatePolicy(candidate);
                        policy = candidate; loadedFileSha256 = Hash(payload); loaded = true; faulted = false;
                        status = policy.Enabled ? "操作员·工程锁定；解锁不会解除设备或运行故障保护。" : "未启用编辑保护。";
                    }
                }
                catch { SetFault("保护策略损坏、不可读取或路径不可信，禁止工程编辑；请人工恢复可信站点配置后重新读取。" ); }
                return SnapshotUnsafe();
            }
        }

        public VmStationProtectionSnapshotS137 Snapshot
        { get { lock (gate) { RefreshExpiry(); return SnapshotUnsafe(); } } }
        public bool CanEdit
        { get { lock (gate) { RefreshExpiry(); return CanEditUnsafe(); } } }
        public long CaptureLease()
        { lock (gate) { RefreshExpiry(); return CanEditUnsafe() ? generation : -1; } }
        public bool IsLeaseCurrent(long lease)
        { lock (gate) { RefreshExpiry(); return lease >= 0 && lease == generation && CanEditUnsafe(); } }

        public VmStationProtectionOperationResultS137 Unlock(string password)
        {
            long lease; Policy captured;
            lock (gate)
            {
                RefreshExpiry();
                if (!loaded || faulted) return Result(false, "策略尚未就绪或存在故障，不能解锁。" );
                if (!policy.Enabled) return Result(false, "未启用编辑保护，无需工程解锁。" );
                if (credentialOperation) return Result(false, "已有一次凭据操作进行中，请等待该操作结束。" );
                if (RetrySeconds() > 0) return Result(false, "口令尝试已暂时冷却，请稍后明确重试。" );
                if (!ValidPassword(password)) { RecordFailure(); return Result(false, "口令须为 8–128 字符；未授予工程授权。" ); }
                lease = generation; captured = policy.Clone(); credentialOperation = true;
            }
            byte[] salt = null, expected = null, actual = null;
            try
            {
                salt = Convert.FromBase64String(captured.Salt); expected = Convert.FromBase64String(captured.Verification);
                actual = Derive(password, salt);
                bool verified = FixedTimeEqual(actual, expected);
                lock (gate)
                {
                    credentialOperation = false; RefreshExpiry();
                    if (!verified) RecordFailure(); // Lock/reload during a failed check cannot reset or bypass monotonic throttling.
                    if (lease != generation || !loaded || faulted || !policy.Enabled) return Result(false, "校验期间工程会话或策略已变化，未授予旧会话授权。" );
                    if (!DiskStillMatches()) { SetFault("站点策略已在本次会话外变化，工程授权已失效；请重新读取可信配置。" ); return Result(false, status); }
                    if (!verified) return Result(false, "口令不正确；未授予工程授权。" );
                    GrantSession();
                    return Result(true, "工程已限时解锁；设备故障、忙碌与写入保护仍然有效。" );
                }
            }
            catch
            {
                lock (gate) { credentialOperation = false; return Result(false, "无法完成口令校验，未新增工程授权。" ); }
            }
            finally { Clear(salt); Clear(expected); Clear(actual); }
        }

        public VmStationProtectionOperationResultS137 Enable(string newPassword, int unlockMinutes, IEnumerable<VmApprovedStationRecipeS137> approved)
        { return ChangePolicy(newPassword, unlockMinutes, approved, true); }
        public VmStationProtectionOperationResultS137 Configure(string newPassword, int unlockMinutes, IEnumerable<VmApprovedStationRecipeS137> approved)
        { return ChangePolicy(newPassword, unlockMinutes, approved, false); }

        private VmStationProtectionOperationResultS137 ChangePolicy(string newPassword, int unlockMinutes, IEnumerable<VmApprovedStationRecipeS137> approved, bool enabling)
        {
            long lease; Policy captured; List<VmApprovedStationRecipeS137> recipes;
            lock (gate)
            {
                RefreshExpiry();
                if (!loaded || faulted) return Result(false, "策略尚未就绪或存在故障，不能修改保护设置。" );
                if (credentialOperation) return Result(false, "已有一次凭据操作进行中，请等待结束。" );
                if (enabling ? policy.Enabled : !policy.Enabled || !AuthorizedUnsafe()) return Result(false, enabling ? "保护已启用，请沿工程授权修改设置。" : "修改已启用策略需要当前有效工程授权。" );
                if (unlockMinutes < 5 || unlockMinutes > 120) return Result(false, "授权时长须为 5–120 分钟。" );
                if (enabling ? !ValidPassword(newPassword) : !string.IsNullOrEmpty(newPassword) && !ValidPassword(newPassword)) return Result(false, "新口令须为 8–128 字符；已启用时空值表示保留原校验。" );
                try { recipes = CloneApproved(approved); }
                catch { return Result(false, "批准方案列表不合法：最多 32 个规范绝对 JSON 路径，须带真实 SHA256，不能重复路径。" ); }
                if (policy.Version >= 1000000) return Result(false, "站点策略版本已达到有界上限，请人工维护可信配置。" );
                lease = generation; captured = policy.Clone(); credentialOperation = true;
            }
            byte[] salt = null, verification = null;
            try
            {
                var changed = captured.Clone(); changed.Enabled = true; changed.Version++;
                changed.UnlockMinutes = unlockMinutes; changed.ApprovedRecipes = recipes;
                if (enabling || !string.IsNullOrEmpty(newPassword))
                {
                    salt = new byte[SaltBytes]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
                    verification = Derive(newPassword, salt);
                    changed.Salt = Convert.ToBase64String(salt); changed.Verification = Convert.ToBase64String(verification);
                }
                lock (gate)
                {
                    credentialOperation = false; RefreshExpiry();
                    if (lease != generation || !loaded || faulted || (enabling ? policy.Enabled : !policy.Enabled || !AuthorizedUnsafe()))
                        return Result(false, "工程会话已变化或到期，旧授权设置未发布。" );
                    Persist(changed, lease, !enabling);
                    policy = changed; loaded = true; faulted = false;
                    GrantSession();
                    return Result(true, enabling ? "本机编辑保护已明确启用，并开始限时工程会话。" : "站点保护设置已保存，并开始新的限时工程会话。" );
                }
            }
            catch (LeaseChangedException)
            { lock (gate) { credentialOperation = false; return Result(false, "写入前授权已到期或换代，旧授权设置未发布。" ); } }
            catch
            {
                lock (gate) { credentialOperation = false; SetFault("站点保护保存失败，未保留成功授权；禁止继续编辑，请重新读取/人工恢复可信配置。" ); return Result(false, status); }
            }
            finally { Clear(salt); Clear(verification); }
        }

        public VmStationProtectionOperationResultS137 Disable()
        {
            lock (gate)
            {
                RefreshExpiry();
                if (!loaded || faulted || !policy.Enabled || !AuthorizedUnsafe()) return Result(false, "关闭已启用保护需要当前有效工程授权。" );
                if (credentialOperation) return Result(false, "已有一次凭据操作进行中，请等待结束。" );
                if (policy.Version >= 1000000) return Result(false, "站点策略版本已达到有界上限。" );
                long lease = generation;
                try
                {
                    var changed = policy.Clone(); changed.Version++; changed.Enabled = false; changed.Salt = string.Empty; changed.Verification = string.Empty;
                    Persist(changed, lease, true); policy = changed; loaded = true; faulted = false;
                    InvalidateSession("未启用编辑保护；本次工程授权已经结束。" );
                    failedAttempts = 0; cooldownUntilTick = 0;
                    return Result(true, "已明确关闭本机编辑保护；旧编辑许可已失效。" );
                }
                catch (LeaseChangedException) { return Result(false, "关闭前工程授权已到期或换代，保护未被关闭。" ); }
                catch { SetFault("关闭保护时保存失败，工程授权已失效；请重新读取可信站点配置。" ); return Result(false, status); }
            }
        }

        public VmStationProtectionOperationResultS137 Lock()
        {
            lock (gate)
            {
                RefreshExpiry();
                InvalidateSession(faulted ? status : policy.Enabled ? "操作员·工程锁定；已取消本次工程授权。" : "未启用编辑保护；旧编辑许可已换代。" );
                return Result(true, faulted ? "工程授权已取消；策略故障仍须人工处理。" : "旧工程授权与编辑许可已失效；不会停止算子、重连或重新写入。" );
            }
        }

        private void Persist(Policy changed, long lease, bool requireAuthorization)
        {
            ValidatePolicy(changed); byte[] payload;
            using (var buffer = new MemoryStream()) { Serializer().WriteObject(buffer, changed); if (buffer.Length > MaximumPolicyBytes) throw new InvalidDataException("站点策略超过有界容量。"); payload = buffer.ToArray(); }
            RejectReparseAncestors(directory); if (File.Exists(directory)) throw new InvalidDataException("工位目录被文件占用。");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            RejectReparseAncestors(directory);
            string lockPath = Path.Combine(directory, ".station-editing-protection.lock"); RejectReparseAncestors(lockPath);
            using (var writeLock = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
                string temporary = Path.Combine(directory, ".station-protection-" + Guid.NewGuid().ToString("N") + ".tmp"); bool created = false;
                try
                {
                    if (!DiskStillMatches()) throw new InvalidDataException("站点策略外部变化。");
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { created = true; stream.Write(payload, 0, payload.Length); stream.Flush(true); }
                    RefreshExpiry();
                    if (lease != generation || !loaded || faulted || (requireAuthorization && !AuthorizedUnsafe())) throw new LeaseChangedException();
                    if (!DiskStillMatches()) throw new InvalidDataException("发布前站点策略外部变化。");
                    RejectReparseAncestors(policyPath); RejectReparseAncestors(temporary);
                    if (loadedFileSha256 == null) File.Move(temporary, policyPath);
                    else File.Replace(temporary, policyPath, null, true);
                    created = false; loadedFileSha256 = Hash(payload);
                }
                finally { if (created) { RejectReparseAncestors(temporary); if (File.Exists(temporary)) File.Delete(temporary); } }
            }
        }

        private byte[] ReadPolicyBytes()
        {
            RejectReparseAncestors(policyPath);
            try
            {
                using (var stream = new FileStream(policyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length < 1 || stream.Length > MaximumPolicyBytes) throw new InvalidDataException("策略文件容量非法。");
                    using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return buffer.ToArray(); }
                }
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }
        private bool DiskStillMatches()
        {
            try
            {
                byte[] bytes = ReadPolicyBytes();
                return bytes == null ? loadedFileSha256 == null : loadedFileSha256 != null && string.Equals(Hash(bytes), loadedFileSha256, StringComparison.Ordinal);
            }
            catch { return false; }
        }
        private static void ValidatePolicy(Policy value)
        {
            if (value == null || value.SchemaVersion != 1 || value.Version < 1 || value.Version > 1000000 || value.UnlockMinutes < 5 || value.UnlockMinutes > 120 || value.Pbkdf2Iterations != Iterations)
                throw new InvalidDataException("站点策略版本或参数非法。");
            CloneApproved(value.ApprovedRecipes);
            if (value.Salt == null || value.Verification == null || value.Salt.Length > 64 || value.Verification.Length > 64) throw new InvalidDataException("凭据结构非法。");
            if (value.Enabled)
            {
                byte[] salt = null, verification = null;
                try { salt = Convert.FromBase64String(value.Salt); verification = Convert.FromBase64String(value.Verification); if (salt.Length != SaltBytes || verification.Length != VerificationBytes) throw new InvalidDataException("凭据长度非法。"); }
                finally { Clear(salt); Clear(verification); }
            }
            else if (value.Salt.Length != 0 || value.Verification.Length != 0) throw new InvalidDataException("未启用策略不能保留活动凭据。");
        }
        private static List<VmApprovedStationRecipeS137> CloneApproved(IEnumerable<VmApprovedStationRecipeS137> values)
        {
            if (values == null) throw new InvalidDataException("批准列表缺失。");
            var copy = new List<VmApprovedStationRecipeS137>(); var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in values)
            {
                if (copy.Count >= MaximumApprovedRecipes || item == null) throw new InvalidDataException("批准列表超限或含空项目。");
                var frozen = item.Clone(); if (frozen.Validate().Length != 0 || !paths.Add(frozen.Path)) throw new InvalidDataException("批准方案不合法或重复。"); copy.Add(frozen);
            }
            return copy;
        }
        private static DataContractJsonSerializer Serializer()
        { return new DataContractJsonSerializer(typeof(Policy), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 1024 }); }
        private static byte[] Derive(string password, byte[] salt)
        { using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256)) return kdf.GetBytes(VerificationBytes); }
        private static bool FixedTimeEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != VerificationBytes || right.Length != VerificationBytes) return false;
            int difference = 0; for (int index = 0; index < VerificationBytes; index++) difference |= left[index] ^ right[index]; return difference == 0;
        }
        private static bool ValidPassword(string password) { return password != null && password.Length >= 8 && password.Length <= 128; }
        private static void Clear(byte[] bytes) { if (bytes != null) Array.Clear(bytes, 0, bytes.Length); }
        private static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
        private bool CanEditUnsafe() { return loaded && !faulted && (!policy.Enabled || AuthorizedUnsafe()); }
        private bool AuthorizedUnsafe() { return loaded && !faulted && policy.Enabled && authorizedUntilTick > Stopwatch.GetTimestamp(); }
        private void GrantSession()
        {
            InvalidateSession("工程已限时解锁；这是本应用内编辑授权，不是操作系统安全边界。" );
            authorizedUntilTick = Stopwatch.GetTimestamp() + (long)policy.UnlockMinutes * 60 * Stopwatch.Frequency;
            unlockedUntilUtc = DateTime.UtcNow.AddMinutes(policy.UnlockMinutes); failedAttempts = 0; cooldownUntilTick = 0;
        }
        private void InvalidateSession(string message)
        { generation++; authorizedUntilTick = 0; unlockedUntilUtc = null; status = message; }
        private void RefreshExpiry()
        { if (authorizedUntilTick != 0 && Stopwatch.GetTimestamp() >= authorizedUntilTick) InvalidateSession("工程授权已到期，进一步编辑已锁定；请明确处理未应用草稿。" ); }
        private void SetFault(string message)
        { policy = null; loaded = true; faulted = true; InvalidateSession(message); }
        private void RecordFailure()
        {
            failedAttempts++;
            if (failedAttempts >= 5) { failedAttempts = 0; cooldownUntilTick = Stopwatch.GetTimestamp() + CooldownSeconds * Stopwatch.Frequency; }
        }
        private int RetrySeconds()
        { long remaining = cooldownUntilTick - Stopwatch.GetTimestamp(); return remaining <= 0 ? 0 : (int)Math.Ceiling((double)remaining / Stopwatch.Frequency); }
        private VmStationProtectionSnapshotS137 SnapshotUnsafe()
        {
            RefreshExpiry();
            return new VmStationProtectionSnapshotS137 { Enabled = faulted || !loaded || policy.Enabled, IsEngineeringAuthorized = AuthorizedUnsafe(), IsFaulted = faulted || !loaded,
                Status = status, Generation = generation, UnlockedUntilUtc = unlockedUntilUtc, RetryAfterSeconds = RetrySeconds(), UnlockMinutes = policy == null ? 30 : policy.UnlockMinutes,
                ApprovedRecipes = policy == null ? new List<VmApprovedStationRecipeS137>() : policy.ApprovedRecipes.Select(item => item.Clone()).ToList() };
        }
        private VmStationProtectionOperationResultS137 Result(bool success, string message)
        { return new VmStationProtectionOperationResultS137 { Success = success, Message = message, Snapshot = SnapshotUnsafe() }; }
        private static void RejectReparseAncestors(string path)
        {
            string target = Path.GetFullPath(path), current = target;
            while (!string.IsNullOrWhiteSpace(current))
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(current);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("拒绝工位配置链接/重解析路径。");
                    if (!string.Equals(current, target, StringComparison.OrdinalIgnoreCase) && (attributes & FileAttributes.Directory) == 0)
                        throw new InvalidDataException("工位配置目录路径被普通文件占用。");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                string parent = Path.GetDirectoryName(current); if (string.Equals(current, parent, StringComparison.OrdinalIgnoreCase)) break; current = parent;
            }
        }
    }
}
