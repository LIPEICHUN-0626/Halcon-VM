using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class ExternalResourceService
    {
        private readonly string resourceDirectory;
        private readonly ScriptToolService scriptToolService;

        public ExternalResourceService(string resourceDirectory, ScriptToolService scriptToolService)
        {
            if (string.IsNullOrWhiteSpace(resourceDirectory)) throw new ArgumentException("外部资源目录不能为空。", "resourceDirectory");
            if (scriptToolService == null) throw new ArgumentNullException("scriptToolService");
            this.resourceDirectory = Path.GetFullPath(resourceDirectory);
            this.scriptToolService = scriptToolService;
            Directory.CreateDirectory(this.resourceDirectory);
        }

        public string ResourceDirectory { get { return resourceDirectory; } }

        public VmExternalResourceData Import(string sourcePath, string name, string typeName, string methodName, string stableResourceId)
        {
            string source = NormalizeExistingDll(sourcePath);
            string entryType = (typeName ?? string.Empty).Trim();
            string entryMethod = (methodName ?? string.Empty).Trim();
            if (entryType.Length == 0) throw new InvalidOperationException("必须填写 DLL 类型全名。");
            if (entryMethod.Length == 0) throw new InvalidOperationException("必须填写 DLL 静态方法名。");

            ScriptToolExecutionReport sourceReport = scriptToolService.Validate(new ScriptToolExecutionRequest
            {
                Mode = VmScriptExecutionMode.Dll,
                AssemblyPath = source,
                TypeName = entryType,
                MethodName = entryMethod,
                TimeoutMilliseconds = 1000
            });
            string resourceId = string.IsNullOrWhiteSpace(stableResourceId) ? Guid.NewGuid().ToString("N") : stableResourceId.Trim();
            if (!IsSafeId(resourceId)) throw new InvalidOperationException("资源 ID 无效。");
            string hash = sourceReport.AssemblySha256;
            string relativePath = Path.Combine(resourceId, hash + ".dll");
            string target = ResolveManagedPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            CopyAtomicallyIfMissing(source, target, hash);

            ScriptToolExecutionReport managedReport = scriptToolService.Validate(new ScriptToolExecutionRequest
            {
                Mode = VmScriptExecutionMode.Dll,
                AssemblyPath = target,
                TypeName = entryType,
                MethodName = entryMethod,
                TimeoutMilliseconds = 1000
            });
            if (!string.Equals(hash, managedReport.AssemblySha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("受管副本复制后哈希不一致，资源导入已拒绝。");

            VmExternalResourceData resource = new VmExternalResourceData
            {
                ResourceId = resourceId,
                ResourceType = VmExternalResourceType.ManagedAssembly,
                Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(source) : name.Trim(),
                SourcePath = source,
                ManagedRelativePath = relativePath,
                Sha256 = hash,
                AssemblyVersion = managedReport.AssemblyVersion,
                DefaultTypeName = entryType,
                DefaultMethodName = entryMethod,
                ImportedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            }.CloneNormalized();
            Inspect(resource);
            return resource;
        }

        public void Inspect(VmExternalResourceData resource)
        {
            if (resource == null) return;
            resource.ManagedPath = string.Empty;
            try
            {
                string managedPath = ResolveManagedPath(resource.ManagedRelativePath);
                resource.ManagedPath = managedPath;
                if (!File.Exists(managedPath)) { SetHealth(resource, VmExternalResourceHealth.Missing, "受管副本不存在：" + managedPath); return; }
                string actualHash = ComputeSha256(managedPath);
                if (string.IsNullOrWhiteSpace(resource.Sha256) || !string.Equals(actualHash, resource.Sha256, StringComparison.OrdinalIgnoreCase))
                { SetHealth(resource, VmExternalResourceHealth.HashMismatch, "受管副本 SHA-256 与配方记录不一致，已禁止执行。"); return; }
                ScriptToolExecutionReport report = scriptToolService.Validate(new ScriptToolExecutionRequest
                {
                    Mode = VmScriptExecutionMode.Dll,
                    AssemblyPath = managedPath,
                    TypeName = resource.DefaultTypeName,
                    MethodName = resource.DefaultMethodName,
                    TimeoutMilliseconds = 1000
                });
                resource.AssemblyVersion = report.AssemblyVersion;
                if (!string.IsNullOrWhiteSpace(resource.SourcePath) && File.Exists(resource.SourcePath) && !string.Equals(ComputeSha256(resource.SourcePath), resource.Sha256, StringComparison.OrdinalIgnoreCase))
                    SetHealth(resource, VmExternalResourceHealth.UpdateAvailable, "原始来源已变化；当前仍执行已验证受管副本，可使用“刷新来源”创建新版本。");
                else
                    SetHealth(resource, VmExternalResourceHealth.Healthy, "受管副本、哈希和入口签名均有效。");
            }
            catch (Exception ex) { SetHealth(resource, VmExternalResourceHealth.Invalid, ex.Message); }
        }

        public string EnsureReadyForExecution(VmExternalResourceData resource)
        {
            if (resource == null) throw new InvalidOperationException("脚本选择的外部资源不存在。");
            Inspect(resource);
            if (!resource.IsRunnable) throw new InvalidOperationException("外部资源“" + resource.Name + "”不可运行：" + resource.HealthDetail);
            return resource.ManagedPath;
        }

        public string ResolveManagedPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) throw new InvalidOperationException("外部资源缺少受管副本路径。");
            string fullPath = Path.GetFullPath(Path.Combine(resourceDirectory, relativePath));
            string prefix = resourceDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("外部资源路径越出受管目录，已拒绝访问。");
            return fullPath;
        }

        public static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] value = sha.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(value.Length * 2);
                foreach (byte item in value) builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string NormalizeExistingDll(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("请选择要导入的 DLL 文件。");
            string fullPath = Path.GetFullPath(path.Trim());
            if (!File.Exists(fullPath)) throw new FileNotFoundException("外部资源 DLL 不存在。", fullPath);
            if (!string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("外部资源首版仅支持受信任托管 .NET DLL。");
            AssemblyName.GetAssemblyName(fullPath);
            return fullPath;
        }

        private static bool IsSafeId(string value)
        {
            foreach (char item in value) if (!char.IsLetterOrDigit(item) && item != '-' && item != '_') return false;
            return value.Length > 0 && value.Length <= 80;
        }

        private static void CopyAtomicallyIfMissing(string source, string target, string expectedHash)
        {
            if (File.Exists(target))
            {
                if (!string.Equals(ComputeSha256(target), expectedHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("同版本受管副本已存在但哈希异常，导入已拒绝。");
                return;
            }
            string temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.Copy(source, temporary, false);
                if (!string.Equals(ComputeSha256(temporary), expectedHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("临时受管副本哈希校验失败。");
                File.Move(temporary, target);
            }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
        }

        private static void SetHealth(VmExternalResourceData resource, string state, string detail)
        {
            resource.HealthState = state;
            resource.HealthDetail = detail ?? string.Empty;
        }
    }
}
