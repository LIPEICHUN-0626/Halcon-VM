using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public static class EngineeringPackageServiceS139
    {
        public const int MaximumReferences = 2048;
        public const long MaximumFileBytes = 536870912, MaximumTotalBytes = 2147483648, MaximumRecipeBytes = 67108864;
        private sealed class Reference
        {
            internal string Key, Label, Kind, Path, Expected;
            internal Action<string> Set;
        }
        private static DataContractJsonSerializer Serializer()
        { return new DataContractJsonSerializer(typeof(VisionRecipe), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 262144 }); }
        private static VisionRecipe Clone(VisionRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException("recipe");
            using (var memory = new MemoryStream())
            {
                Serializer().WriteObject(memory, recipe);
                if (memory.Length > MaximumRecipeBytes) throw new InvalidDataException("工程定义超过 64 MiB，未创建无界迁移计划。 ");
                memory.Position = 0; return (VisionRecipe)Serializer().ReadObject(memory);
            }
        }
        public static VmEngineeringPackagePlanS139 CreatePlan(VisionRecipe recipe, string managedResourceRoot, CancellationToken token = default(CancellationToken))
        {
            var copy = Clone(recipe);
            copy.EngineeringPackageS139 = null; // A new export always builds a new, current manifest.
            var plan = new VmEngineeringPackagePlanS139 { Recipe = copy, ManagedResourceRoot = Path.GetFullPath(managedResourceRoot) };
            var cache = new Dictionary<string, Tuple<string, long>>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            foreach (var reference in References(copy, plan.ManagedResourceRoot, true))
            {
                token.ThrowIfCancellationRequested();
                var entry = new VmEngineeringPackageEntryS139 { Key = reference.Key, Label = Short(reference.Label, 256), Kind = reference.Kind,
                    SourcePath = reference.Path ?? string.Empty, ExpectedSha256 = reference.Expected ?? string.Empty, WritePath = reference.Set };
                plan.Entries.Add(entry);
                try
                {
                    if (string.IsNullOrWhiteSpace(reference.Path) || !File.Exists(reference.Path))
                    { entry.State = "Missing"; entry.Detail = "声明文件缺失；重新定位只改本次副本，模型原 SHA 仍须一致。"; continue; }
                    entry.SourcePath = Path.GetFullPath(reference.Path);
                    Tuple<string, long> identity;
                    if (!cache.TryGetValue(entry.SourcePath, out identity))
                    {
                        using (var input = OpenReadGuarded(entry.SourcePath))
                        {
                            if (input.Length > MaximumFileBytes || total > MaximumTotalBytes - input.Length)
                                throw new InvalidDataException("迁移预算：单文件最多 512 MiB，源文件去重总量最多 2 GiB。 ");
                            total += input.Length;
                            identity = Tuple.Create(Hash(input, token), input.Length);
                        }
                        cache.Add(entry.SourcePath, identity);
                    }
                    entry.Sha256 = identity.Item1; entry.Bytes = identity.Item2;
                    if (!string.IsNullOrWhiteSpace(entry.ExpectedSha256) && (!IsHash(entry.ExpectedSha256) ||
                        !string.Equals(entry.ExpectedSha256, entry.Sha256, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException("资源 SHA 与已应用版本不一致；导出不能偷偷接受另一版模型/DLL，请回原配置发布正确版本。 ");
                    entry.State = "Ready";
                    entry.StorageIdentityS139 = StorageName(entry);
                    entry.Detail = "已冻结字节 SHA / 长度；发布前复核，不证明算法、DLL安全、间接依赖或目标环境可用。";
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { entry.State = "Blocked"; entry.Detail = Short(ex.Message, 2048); }
            }
            plan.UnresolvedDependencies = !string.IsNullOrWhiteSpace(copy.HDevelopPath) ||
                (copy.ExternalResources != null && copy.ExternalResources.Count > 0) ||
                VmProjectFlowData.EnumerateRecipeTools(copy).Any(t => t.Parameters != null &&
                    (t.ToolType == VmToolKind.CSharpDllScript.ToString() || !string.IsNullOrWhiteSpace(t.Parameters.ImageReplayFolderS114)));
            plan.ReportText = EngineeringDeploymentReportS139.BuildReport(copy, plan.Entries);
            return plan;
        }
        public static VmEngineeringPackagePlanS139 Relocate(VmEngineeringPackagePlanS139 plan, string key, string path, CancellationToken token)
        {
            // Work on another independent copy so cancellation never mutates even the old frozen plan.
            var copy = Clone(plan.Recipe);
            var reference = References(copy, plan.ManagedResourceRoot, true).FirstOrDefault(r => r.Key == key);
            if (reference == null) throw new InvalidOperationException("资源身份已变化，不能按名称改另一条引用。 ");
            reference.Set(Path.GetFullPath(path));
            var next = CreatePlan(copy, plan.ManagedResourceRoot, token);
            foreach (var item in plan.Relocations) next.Relocations[item.Key] = item.Value;
            next.Relocations[key] = path;
            return next;
        }

        public static string Publish(string path, VmEngineeringPackagePlanS139 plan, RecipeService recipeService, Func<bool> stillAuthorized, CancellationToken token)
        {
            if (plan == null || plan.BlockingCount > 0) throw new InvalidOperationException("迁移清单仍有缺失/阻断，未发布。 ");
            if (!stillAuthorized()) throw new InvalidOperationException("工程授权/会话已变化，未发布迁移副本。 ");
            string target = Path.GetFullPath(path), root = Path.GetDirectoryName(target);
            if (!string.Equals(Path.GetExtension(target), ".json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("工程副本必须为 JSON。 ");
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException("目标目录不存在。 ");
            AssertNoReparse(root); if (File.Exists(target)) AssertNoReparse(target);
            if (plan.Entries.Any(e => string.Equals(e.SourcePath, target, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("导出目标不能覆盖源资源。 ");
            string directoryName = "engineering-assets-" + Guid.NewGuid().ToString("N");
            string directory = Under(root, directoryName), staged = Under(root, ".engineering-" + Guid.NewGuid().ToString("N") + ".pending.json");
            var created = new List<string>(); bool published = false;
            var copy = Clone(plan.Recipe);
            var references = References(copy, plan.ManagedResourceRoot, true).ToDictionary(r => r.Key, StringComparer.Ordinal);
            var manifest = new VmEngineeringPackageManifestS139 { ResourceDirectory = directoryName,
                IndirectDependenciesUnresolved = plan.UnresolvedDependencies, PreparedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) };
            var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            try
            {
                if (Directory.Exists(directory)) throw new IOException("独立资源目录已存在，不覆盖。 ");
                Directory.CreateDirectory(directory); AssertNoReparse(directory);
                foreach (var entry in plan.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    if (!stillAuthorized()) throw new InvalidOperationException("工程授权已失效，副本未发布。 ");
                    Reference reference;
                    if (!references.TryGetValue(entry.Key, out reference)) throw new InvalidOperationException("冻结引用身份不一致。 ");
                    // DLL / HDevelop file names are preserved inside a per-content directory. Indirect lookup is still unknown.
                    string relativeFile = StorageName(entry);
                    string dedup = entry.Sha256 + ":" + entry.Bytes.ToString(CultureInfo.InvariantCulture) + ":" + relativeFile;
                    string relative;
                    using (var input = OpenReadGuarded(entry.SourcePath))
                    {
                        if (input.Length != entry.Bytes || !string.Equals(Hash(input, token), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("资源在计划之后已变化：" + entry.Label);
                        if (!copied.TryGetValue(dedup, out relative))
                        {
                            if (manifest.Files.Count >= MaximumReferences || total > MaximumTotalBytes - input.Length) throw new InvalidDataException("实际复制超过文件数/总量预算。 ");
                            relative = Path.Combine(directoryName, relativeFile);
                            string destination = Under(root, relative);
                            Directory.CreateDirectory(Path.GetDirectoryName(destination)); AssertNoReparse(Path.GetDirectoryName(destination));
                            input.Position = 0;
                            created.Add(destination);
                            using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            {
                                var buffer = new byte[65536]; int count;
                                while ((count = input.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); output.Write(buffer, 0, count); }
                                output.Flush(true);
                            }
                            using (var output = OpenReadGuarded(destination))
                                if (output.Length != entry.Bytes || Hash(output, token) != entry.Sha256) throw new InvalidDataException("复制后的资源字节不一致。 ");
                            total += entry.Bytes;
                            copied.Add(dedup, relative);
                            manifest.Files.Add(new VmEngineeringPackageFileS139 { RelativePath = relative, Sha256 = entry.Sha256, Bytes = entry.Bytes, Kind = entry.Kind });
                        }
                    }
                    reference.Set(relative);
                }
                copy.EngineeringPackageS139 = manifest;
                string reportFile = Under(directory, "目标机器准备.txt"); created.Add(reportFile);
                WriteNew(reportFile, plan.ReportText);
                string listFile = Under(directory, "资源清单.txt"); created.Add(listFile);
                WriteNew(listFile, "S139 随行工程资源清单（SHA不是签名或算法可用证明）\r\n" + plan.Summary + "\r\n" +
                    string.Join("\r\n", manifest.Files.Select(f => f.RelativePath + " · " + f.Bytes + " bytes · SHA256 " + f.Sha256)) +
                    "\r\n请同时迁移JSON与本目录；间接依赖、运行库、许可和设备环境按准备报告人工核对。 ");
                created.Add(staged);
                recipeService.SaveRecipe(staged, copy);
                if (new FileInfo(staged).Length > MaximumRecipeBytes) throw new InvalidDataException("已暂存定义超过64MiB，未发布。 ");
                // Validate a separate deserialized value; resolution must not alter the relative-path export DTO.
                VisionRecipe serialized;
                using (var read = OpenReadGuarded(staged)) serialized = (VisionRecipe)Serializer().ReadObject(read);
                ValidateAndResolve(serialized, staged, token);
                token.ThrowIfCancellationRequested();
                if (!stillAuthorized()) throw new InvalidOperationException("发布前工程授权/会话已变化，未发布。 ");
                AssertNoReparse(root); AssertNoReparse(directory);
                if (File.Exists(target))
                {
                    AssertNoReparse(target);
                    // Preserve the previous JSON in a fresh backup, never overwrite a pre-existing .bak.
                    File.Replace(staged, target, Under(root, Path.GetFileName(target) + ".backup-" + Guid.NewGuid().ToString("N")), true);
                }
                else File.Move(staged, target);
                published = true;
                return directory;
            }
            finally
            {
                if (!published)
                {
                    // Only verified in-root paths created by this attempt. Non-recursive directory cleanup.
                    foreach (string file in created.AsEnumerable().Reverse())
                        try { string checkedPath = Under(root, Relative(root, file)); AssertNoReparse(Path.GetDirectoryName(checkedPath)); if (File.Exists(checkedPath)) { AssertNoReparse(checkedPath); File.Delete(checkedPath); } } catch { }
                    try
                    {
                        AssertNoReparse(directory);
                        foreach (var child in copied.Values.Select(v => Path.GetDirectoryName(Under(root, v))).Distinct(StringComparer.OrdinalIgnoreCase))
                            if (child != directory && Directory.Exists(child)) { AssertNoReparse(child); Directory.Delete(child, false); }
                        if (Directory.Exists(directory)) Directory.Delete(directory, false);
                    }
                    catch { /* Retained residue is safer than deleting a changed/unverified directory. */ }
                }
            }
        }

        public static void ValidateAndResolve(VisionRecipe recipe, string recipePath, CancellationToken token = default(CancellationToken))
        {
            var manifest = recipe.EngineeringPackageS139;
            if (manifest == null) return;
            if (manifest.SchemaVersion != 1 || manifest.Files == null || manifest.Files.Count > MaximumReferences ||
                string.IsNullOrWhiteSpace(manifest.ResourceDirectory) || !manifest.ResourceDirectory.StartsWith("engineering-assets-", StringComparison.Ordinal) ||
                manifest.ResourceDirectory.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                throw new InvalidDataException("随行工程清单版本/目录/数量无效。 ");
            string root = Path.GetDirectoryName(Path.GetFullPath(recipePath)); AssertNoReparse(root);
            string resourceRoot = Under(root, manifest.ResourceDirectory);
            var files = new Dictionary<string, VmEngineeringPackageFileS139>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            foreach (var file in manifest.Files)
            {
                token.ThrowIfCancellationRequested();
                if (file == null || !IsHash(file.Sha256) || file.Bytes < 0 || file.Bytes > MaximumFileBytes || total > MaximumTotalBytes - file.Bytes)
                    throw new InvalidDataException("随行文件身份/长度/预算无效。 ");
                string full = Under(root, file.RelativePath);
                if (!IsUnder(resourceRoot, full) || files.ContainsKey(full)) throw new InvalidDataException("资源清单重复或越出独立资源目录。 ");
                using (var input = OpenReadGuarded(full))
                    if (input.Length != file.Bytes || !string.Equals(Hash(input, token), file.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("随行资源缺失或 SHA 不一致：" + file.RelativePath);
                total += file.Bytes; files.Add(full, file);
            }
            foreach (var reference in References(recipe, string.Empty, false))
            {
                if (string.IsNullOrWhiteSpace(reference.Path)) continue;
                string full = Under(root, reference.Path);
                VmEngineeringPackageFileS139 file;
                if (!files.TryGetValue(full, out file)) throw new InvalidDataException("工程资源引用未列入已验证清单：" + reference.Label);
                if (!string.IsNullOrWhiteSpace(reference.Expected) && !string.Equals(reference.Expected, file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("模型/DLL声明版本与随行清单不同：" + reference.Label);
                reference.Set(full);
            }
        }

        // Centralized actual field enumeration shared by plan, relocation, publisher and loader.
        private static List<Reference> References(VisionRecipe recipe, string managedRoot, bool useOcrDefault)
        {
            var result = new List<Reference>();
            Action<string, string, string, string, string, Action<string>> add = (key, label, kind, path, sha, write) =>
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                if (result.Count >= MaximumReferences) throw new InvalidDataException("全工程显式资源引用超过2048项，不能截断后当完整迁移。 ");
                result.Add(new Reference { Key = key, Label = label, Kind = kind, Path = path, Expected = sha, Set = write });
            };
            add("legacy-template", "兼容 Shape 模板", "Shape 模板", recipe.TemplatePath, null, p => recipe.TemplatePath = p);
            add("hdevelop", "HDevelop 程序本体", "HDevelop", recipe.HDevelopPath, null, p => recipe.HDevelopPath = p);
            int ordinal = 0;
            foreach (var tool in VmProjectFlowData.EnumerateRecipeTools(recipe))
            {
                var p = tool.Parameters; if (p == null) { ordinal++; continue; }
                string key = "tool-" + (ordinal++).ToString(CultureInfo.InvariantCulture) + "/";
                string label = tool.InstanceName ?? tool.ToolId;
                add(key + "image", label + " · 本地图像", "图像", p.LocalImagePath, null, value => p.LocalImagePath = value);
                add(key + "shape", label + " · Shape 模板", "Shape 模板", p.ShapeModelPath, null, value => p.ShapeModelPath = value);
                add(key + "dll", label + " · 脚本 DLL 本体", "脚本 DLL", p.ScriptAssemblyPath, null, value => p.ScriptAssemblyPath = value);
                if (tool.ToolType == VmToolKind.OcrRecognition.ToString() || !string.IsNullOrWhiteSpace(p.OcrModelPath) || p.OcrValidationS133 != null && !string.IsNullOrWhiteSpace(p.OcrValidationS133.ModelPath))
                {
                    string ocr = string.IsNullOrWhiteSpace(p.OcrModelPath) ? p.OcrValidationS133?.ModelPath : p.OcrModelPath;
                    if (string.IsNullOrWhiteSpace(ocr) && useOcrDefault)
                    {
                        string halconRoot = Environment.GetEnvironmentVariable("HALCONROOT");
                        ocr = Path.Combine(string.IsNullOrWhiteSpace(halconRoot) ? @"C:\Program Files\MVTec\HALCON-20.11-Progress" : halconRoot, "ocr", "Industrial_NoRej.omc");
                    }
                    var validation = p.OcrValidationS133;
                    add(key + "ocr", label + " · OCR MLP 字库", "OCR 字库", ocr, validation?.ModelSha256, value =>
                    { p.OcrModelPath = value; if (p.OcrValidationS133 != null) p.OcrValidationS133.ModelPath = value; });
                    if (validation != null && !string.IsNullOrWhiteSpace(validation.ModelPath) && !string.IsNullOrWhiteSpace(p.OcrModelPath) &&
                        !string.Equals(validation.ModelPath, p.OcrModelPath, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("OCR 字库路径与冻结版本不是同一引用：" + label);
                }
                var ncc = p.NccMatchingS131;
                if (ncc != null) add(key + "ncc", label + " · NCC 模板", "NCC 模板", ncc.ModelPath, ncc.ModelSha256, value => ncc.ModelPath = value);
                var variation = p.VariationInspectionS130;
                if (variation != null)
                {
                    add(key + "variation", label + " · 良品变化模型", "变化模型", variation.ModelPath, variation.ModelSha256, value => variation.ModelPath = value);
                    add(key + "coverage", label + " · 共同有效域", "有效域", variation.CoveragePath, variation.CoverageSha256, value => variation.CoveragePath = value);
                    int sampleIndex = 0;
                    foreach (var sample in variation.Samples ?? new List<VmVariationSampleS130>())
                    {
                        if (sample == null) continue;
                        string sk = key + "sample-" + (sampleIndex++).ToString(CultureInfo.InvariantCulture);
                        add(sk + "/image", label + " · 良品 " + sample.Name, "良品样本", sample.ImagePath, sample.ImageSha256, value => sample.ImagePath = value);
                        add(sk + "/domain", label + " · 良品有效域", "有效域", sample.DomainPath, sample.DomainSha256, value => sample.DomainPath = value);
                    }
                }
                add(key + "board", label + " · 标定板定义", "标定板", p.CameraBoardPathS127, null, value => p.CameraBoardPathS127 = value);
                for (int i = 0; i < (p.CameraSamplePathsS127?.Count ?? 0); i++)
                { int index = i; add(key + "calibration-" + i, label + " · 标定样本 " + (i + 1), "标定样本", p.CameraSamplePathsS127[i], null, value => p.CameraSamplePathsS127[index] = value); }
            }
            int resourceIndex = 0;
            foreach (var resource in recipe.ExternalResources ?? new List<VmExternalResourceData>())
            {
                if (resource == null) throw new InvalidDataException("资源库声明为空，不能猜测另一DLL。 ");
                string source = resource.PortablePathS139;
                if (string.IsNullOrWhiteSpace(source))
                {
                    if (string.IsNullOrWhiteSpace(managedRoot)) throw new InvalidDataException("随行工程资源库缺少便携引用：" + resource.Name);
                    source = Under(managedRoot, resource.ManagedRelativePath);
                }
                add("resource-" + resourceIndex++, resource.Name + " · 资源库受管 DLL", "资源库 DLL", source, resource.Sha256,
                    value => { resource.PortablePathS139 = value; resource.SourcePath = value; });
            }
            return result;
        }
        public static string ResolveLooseReference(string root, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            string full = Path.IsPathRooted(value) ? Path.GetFullPath(value) : Under(root, value);
            // Legacy explicit absolute references remain supported, relative references never escape their recipe root.
            if (Path.IsPathRooted(value)) return full; // Legacy explicit absolute configuration is not a portable-path claim.
            if (File.Exists(full) || Directory.Exists(full)) AssertNoReparse(full);
            else { string parent = Path.GetDirectoryName(full); if (Directory.Exists(parent)) AssertNoReparse(parent); }
            return full;
        }
        public static void AssertNoReparse(string path)
        {
            string full = Path.GetFullPath(path);
            if (full.StartsWith(@"\\", StringComparison.Ordinal)) throw new InvalidDataException("网络/UNC资源不直接随行，请显式定位到本地普通文件。 ");
            string local = full.Substring(Path.GetPathRoot(full).Length).TrimEnd('\\', '/');
            if (local.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Any(p => !SafeComponent(p)))
                throw new InvalidDataException("资源路径包含不明确的Windows文件名/ADS/保留设备名。 ");
            string cursor = full;
            while (!string.IsNullOrEmpty(cursor))
            {
                if ((File.Exists(cursor) || Directory.Exists(cursor)) && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("资源路径包含重解析点/链接，拒绝访问：" + cursor);
                string parent = Path.GetDirectoryName(cursor);
                if (parent == cursor) break; cursor = parent;
            }
        }
        private static FileStream OpenReadGuarded(string path)
        {
            AssertNoReparse(path);
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try { AssertNoReparse(path); if (stream.Length > MaximumFileBytes) throw new InvalidDataException("文件超过512MiB。 "); return stream; }
            catch { stream.Dispose(); throw; }
        }
        private static string Hash(Stream stream, CancellationToken token)
        {
            stream.Position = 0;
            using (var sha = SHA256.Create())
            {
                var buffer = new byte[65536]; int count;
                while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); sha.TransformBlock(buffer, 0, count, buffer, 0); }
                sha.TransformFinalBlock(new byte[0], 0, 0); stream.Position = 0;
                return BitConverter.ToString(sha.Hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
        private static bool IsHash(string value) { return value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'); }
        private static bool IsUnder(string root, string path) { return path.StartsWith(Path.GetFullPath(root).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase); }
        private static string Under(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Split('\\', '/').Any(p => !SafeComponent(p)))
                throw new InvalidDataException("资源引用须为目录内的明确相对路径，不能绝对/越界/含ADS。 ");
            string full = Path.GetFullPath(Path.Combine(root, relative));
            if (!IsUnder(root, full)) throw new InvalidDataException("资源路径越出目标目录。 ");
            return full;
        }
        private static string Relative(string root, string file)
        {
            if (!IsUnder(root, Path.GetFullPath(file))) throw new InvalidDataException("清理路径越出目标目录。 ");
            return Path.GetFullPath(file).Substring(Path.GetFullPath(root).TrimEnd('\\', '/').Length + 1);
        }
        private static string Short(string value, int limit) { return string.IsNullOrEmpty(value) ? string.Empty : value.Substring(0, Math.Min(limit, value.Length)); }
        private static string StorageName(VmEngineeringPackageEntryS139 entry)
        {
            string extension = Path.GetExtension(entry.SourcePath).ToLowerInvariant();
            if (extension.Length > 16 || extension.Any(c => !char.IsLetterOrDigit(c) && c != '.')) extension = ".bin";
            return entry.Kind == "脚本 DLL" || entry.Kind == "资源库 DLL" || entry.Kind == "HDevelop"
                ? Path.Combine(entry.Sha256, Path.GetFileName(entry.SourcePath)) : entry.Sha256 + extension;
        }
        private static bool SafeComponent(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "." || value == ".." || value.Length > 255 || value.EndsWith(".") || value.EndsWith(" ") ||
                value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            string stem = value.Split('.')[0].ToUpperInvariant();
            return stem != "CON" && stem != "PRN" && stem != "AUX" && stem != "NUL" &&
                !(stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] >= '1' && stem[3] <= '9');
        }
        private static void WriteNew(string path, string value)
        {
            using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(output, new UTF8Encoding(true), 4096, true))
            { writer.Write(value); writer.Flush(); output.Flush(true); }
        }
    }
}
