using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class RecipeService
    {
        private readonly string recipeDirectory;
        private readonly string stateDirectory;
        private readonly string resourceDirectory;

        public RecipeService()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public RecipeService(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException("基础目录不能为空。", "baseDirectory");
            }

            string root = Path.GetFullPath(baseDirectory);
            recipeDirectory = Path.Combine(root, "recipes");
            stateDirectory = Path.Combine(root, "config");
            resourceDirectory = Path.Combine(root, "resources");
            Directory.CreateDirectory(recipeDirectory);
            Directory.CreateDirectory(stateDirectory);
            Directory.CreateDirectory(resourceDirectory);
        }

        public string RecipeDirectory
        {
            get { return recipeDirectory; }
        }

        public string StateDirectory
        {
            get { return stateDirectory; }
        }

        public string ResourceDirectory
        {
            get { return resourceDirectory; }
        }

        public string StatePath
        {
            get { return Path.Combine(stateDirectory, "ui-state.json"); }
        }

        public string RecoveryPath
        {
            get { return Path.Combine(stateDirectory, "active-recipe-recovery.json"); }
        }

        public string FlowSnippetPath
        {
            get { return Path.Combine(stateDirectory, "flow-snippets.json"); }
        }

        public void SaveRecipe(string path, VisionRecipe recipe)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("配方路径不能为空。", "path");
            }

            if (recipe == null)
            {
                throw new ArgumentNullException("recipe");
            }

            recipe.SchemaVersion = RecipeSchema.CurrentVersion;
            recipe.SavedAtUtc = DateTime.UtcNow.ToString("O");
            string fullPath = Path.GetFullPath(path);
            WriteJsonAtomic(fullPath, recipe, typeof(VisionRecipe), fullPath + ".bak");

            VisionRecipe verified = (VisionRecipe)ReadJson(fullPath, typeof(VisionRecipe));
            if (verified == null || verified.SchemaVersion != RecipeSchema.CurrentVersion)
            {
                throw new InvalidDataException("配方保存后回读验证失败。");
            }
        }

        public VisionRecipe LoadRecipe(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("配方文件不存在。", path);
            }

            VisionRecipe recipe = (VisionRecipe)ReadJson(path, typeof(VisionRecipe));
            if (recipe == null)
            {
                throw new InvalidDataException("配方内容为空。");
            }

            if (recipe.SchemaVersion <= 0)
            {
                recipe.SchemaVersion = 1;
            }

            if (recipe.SchemaVersion > RecipeSchema.CurrentVersion)
            {
                throw new InvalidDataException("配方版本高于当前软件支持版本：v" + recipe.SchemaVersion + "。");
            }

            recipe.SchemaVersion = RecipeSchema.CurrentVersion;
            string recipeRoot = Path.GetDirectoryName(Path.GetFullPath(path));
            VmProjectFlowData.ValidateProject(recipe);
            foreach (var variable in recipe.GlobalVariables ?? new System.Collections.Generic.List<VmGlobalVariableData>())
                if (variable != null && VmArrayContractS105.IsType(variable.ValueType)) variable.CloneNormalized();
            EngineeringPackageServiceS139.ValidateAndResolve(recipe, path);
            Func<string, string> resolve = value => EngineeringPackageServiceS139.ResolveLooseReference(recipeRoot, value);
            recipe.TemplatePath = resolve(recipe.TemplatePath);
            recipe.HDevelopPath = resolve(recipe.HDevelopPath);
            foreach (var resource in recipe.ExternalResources ?? new List<VmExternalResourceData>())
                if (resource != null && !string.IsNullOrWhiteSpace(resource.PortablePathS139))
                { resource.PortablePathS139 = resolve(resource.PortablePathS139); resource.SourcePath = resolve(resource.SourcePath); }
                foreach (var tool in VmProjectFlowData.EnumerateRecipeTools(recipe))
                {
                    if (tool.Parameters != null) tool.Parameters.ShapeModelPath = resolve(tool.Parameters.ShapeModelPath);
                    if (tool.Parameters != null)
                    {
                        tool.Parameters.ScriptAssemblyPath = resolve(tool.Parameters.ScriptAssemblyPath);
                        tool.Parameters.CameraBoardPathS127 = resolve(tool.Parameters.CameraBoardPathS127);
                        for (int index = 0; index < (tool.Parameters.CameraSamplePathsS127?.Count ?? 0); index++)
                            tool.Parameters.CameraSamplePathsS127[index] = resolve(tool.Parameters.CameraSamplePathsS127[index]);
                        tool.Parameters.OcrModelPath = resolve(tool.Parameters.OcrModelPath);
                        if (tool.Parameters.OcrValidationS133 != null)
                        {
                            var ocr = tool.Parameters.OcrValidationS133;
                            ocr.ModelPath = resolve(ocr.ModelPath);
                            if (!string.IsNullOrWhiteSpace(ocr.ModelPath))
                            {
                                if (string.IsNullOrWhiteSpace(tool.Parameters.OcrModelPath))
                                    tool.Parameters.OcrModelPath = ocr.ModelPath;
                                else if (!string.Equals(Path.GetFullPath(tool.Parameters.OcrModelPath), Path.GetFullPath(ocr.ModelPath), StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidDataException("OCR 字库路径与保存的 SHA 版本不是同一文件：" + tool.InstanceName);
                            }
                        }
                    }
                    if (tool.Parameters != null && tool.Parameters.NccMatchingS131 != null)
                        tool.Parameters.NccMatchingS131.ModelPath = resolve(tool.Parameters.NccMatchingS131.ModelPath);
                    if (tool.Parameters != null && tool.Parameters.VariationInspectionS130 != null)
                    {
                        var variation = tool.Parameters.VariationInspectionS130;
                        variation.ModelPath = resolve(variation.ModelPath); variation.CoveragePath = resolve(variation.CoveragePath);
                        foreach (var sample in variation.Samples ?? new List<VmVariationSampleS130>())
                            if (sample != null) { sample.ImagePath = resolve(sample.ImagePath); sample.DomainPath = resolve(sample.DomainPath); }
                    }
                    if (tool.Parameters != null && !string.IsNullOrWhiteSpace(tool.Parameters.LocalImagePath) && !Path.IsPathRooted(tool.Parameters.LocalImagePath))
                        tool.Parameters.LocalImagePath = resolve(tool.Parameters.LocalImagePath);
                }
            return recipe;
        }

        public void ExportRecipeWithImages(string path, VisionRecipe recipe, bool includeShapeModels = false)
        {
            if (recipe == null) throw new ArgumentNullException("recipe");
            string fullPath = Path.GetFullPath(path);
            // Export operates on a detached DTO; the open recipe and original files remain untouched.
            VisionRecipe copy;
            using (var buffer = new MemoryStream(Encoding.UTF8.GetBytes(SerializeJson(recipe, typeof(VisionRecipe)))))
                copy = (VisionRecipe)new DataContractJsonSerializer(typeof(VisionRecipe)).ReadObject(buffer);
            string folderName = Path.GetFileNameWithoutExtension(fullPath) + "-images-" + Guid.NewGuid().ToString("N");
            string folder = Path.Combine(Path.GetDirectoryName(fullPath), folderName);
            var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var copiedOcr = new Dictionary<string, VmOcrModelResourceInfoS133>(StringComparer.OrdinalIgnoreCase);
            var createdFiles = new List<string>();
            var manifest = new StringBuilder("方案资源清单\r\n请同时迁移方案 JSON 与本目录。包含本地图像" +
                (includeShapeModels ? "及 Shape 模板文件" : "") + "；NCC 灰度模板、OCR MLP 字库、良品变化模型、共同有效域和良品样本始终包含。DLL、脚本依赖与设备环境不包含在内。OCR 字库按原字节受管复制；部署仍须配置兼容 HALCON 运行库及许可证。\r\n\r\n");
            bool published = false;
            try
            {
                Directory.CreateDirectory(folder);
                Func<string, string, string> copyResource = (sourcePath, label) =>
                {
                    if (string.IsNullOrWhiteSpace(sourcePath)) return sourcePath;
                    string source = Path.GetFullPath(sourcePath);
                    if (!File.Exists(source)) throw new FileNotFoundException("资源缺失，未完成导出：" + label, source);
                    string name;
                    if (!copied.TryGetValue(source, out name))
                    {
                        name = Guid.NewGuid().ToString("N") + Path.GetExtension(source);
                        string destination = Path.Combine(folder, name);
                        createdFiles.Add(destination);
                        File.Copy(source, destination, false);
                        copied.Add(source, name);
                    }
                    manifest.AppendLine(label + " → " + name);
                    return Path.Combine(folderName, name);
                };
                if (includeShapeModels)
                {
                    copy.TemplatePath = copyResource(copy.TemplatePath, "兼容模板");
                        foreach (var tool in VmProjectFlowData.EnumerateRecipeTools(copy))
                            if (tool.Parameters != null)
                                tool.Parameters.ShapeModelPath = copyResource(tool.Parameters.ShapeModelPath, tool.InstanceName + " · Shape 模板");
                }
                    foreach (var tool in VmProjectFlowData.EnumerateRecipeTools(copy))
                    {
                        if (tool.Parameters == null) continue;
                        if (string.Equals(tool.ToolType, VmToolKind.OcrRecognition.ToString(), StringComparison.OrdinalIgnoreCase))
                            PackageOcrResourceS133(tool.Parameters, tool.InstanceName, folderName, folder, copiedOcr, createdFiles, manifest);
                        var ncc = tool.Parameters.NccMatchingS131;
                        if (ncc != null && !string.IsNullOrWhiteSpace(ncc.ModelPath))
                        {
                            string expectedHash = ncc.ModelSha256;
                            if (string.IsNullOrWhiteSpace(expectedHash) || expectedHash.Length != 64 ||
                                !string.Equals(HashNccResourceS131(ncc.ModelPath), expectedHash, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidDataException("NCC 模板版本校验失败，未完成导出：" + tool.InstanceName);
                            string packagedPath = copyResource(ncc.ModelPath, tool.InstanceName + " · NCC 灰度模板");
                            string packagedAbsolutePath = Path.Combine(Path.GetDirectoryName(fullPath), packagedPath);
                            if (!string.Equals(HashNccResourceS131(packagedAbsolutePath), expectedHash, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidDataException("NCC 模板复制期间发生变化，未完成导出：" + tool.InstanceName);
                            // Immutable model bytes and version identity stay unchanged; only the detached recipe path moves.
                            ncc.ModelPath = packagedPath;
                        }
                        var variation = tool.Parameters.VariationInspectionS130;
                        if (variation != null)
                        {
                            variation.ModelPath = copyResource(variation.ModelPath, tool.InstanceName + " · 良品变化模型");
                            variation.CoveragePath = copyResource(variation.CoveragePath, tool.InstanceName + " · 训练共同有效域");
                            foreach (var sample in variation.Samples ?? new List<VmVariationSampleS130>())
                                if (sample != null)
                                {
                                    sample.ImagePath = copyResource(sample.ImagePath, tool.InstanceName + " · 良品 " + sample.Name);
                                    sample.DomainPath = copyResource(sample.DomainPath, tool.InstanceName + " · 良品有效域");
                                }
                        }
                        if (string.IsNullOrWhiteSpace(tool.Parameters.LocalImagePath)) continue;
                        string source = Path.GetFullPath(tool.Parameters.LocalImagePath);
                        if (!File.Exists(source)) throw new FileNotFoundException("图像资源缺失，未完成导出：" + tool.InstanceName, source);
                        string name;
                        if (!copied.TryGetValue(source, out name))
                        {
                            name = Guid.NewGuid().ToString("N") + Path.GetExtension(source);
                            string destination = Path.Combine(folder, name);
                            createdFiles.Add(destination);
                            File.Copy(source, destination, false);
                            copied.Add(source, name);
                        }
                        tool.Parameters.LocalImagePath = Path.Combine(folderName, name);
                        manifest.AppendLine((tool.InstanceName ?? tool.ToolId) + " → " + name);
                    }
                string manifestPath = Path.Combine(folder, "资源清单.txt");
                createdFiles.Add(manifestPath);
                File.WriteAllText(manifestPath, manifest.ToString(), new UTF8Encoding(true));
                string stagedRecipe = Path.Combine(folder, "recipe.pending.json");
                createdFiles.Add(stagedRecipe);
                SaveRecipe(stagedRecipe, copy);
                // Publish the recipe only once all copied resources and serialized data are ready.
                if (File.Exists(fullPath)) File.Replace(stagedRecipe, fullPath, fullPath + ".bak");
                else File.Move(stagedRecipe, fullPath);
                published = true;
            }
            finally
            {
                if (!published)
                {
                    // Only remove paths created by this export, never source files or prior exports.
                    foreach (string created in createdFiles)
                    {
                        try { if (File.Exists(created)) File.Delete(created); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    }
                    try { if (Directory.Exists(folder)) Directory.Delete(folder, false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
        }

        public string GetRecipeFingerprint(VisionRecipe recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            return SerializeJson(recipe, typeof(VisionRecipe));
        }

        public void SaveRecovery(RecipeRecoveryData recovery)
        {
            if (recovery == null || recovery.Recipe == null)
            {
                throw new ArgumentNullException("recovery");
            }

            recovery.CapturedAtUtc = DateTime.UtcNow.ToString("O");
            recovery.Recipe.SchemaVersion = RecipeSchema.CurrentVersion;
            recovery.Recipe.SavedAtUtc = null;
            WriteJsonAtomic(RecoveryPath, recovery, typeof(RecipeRecoveryData), null);
        }

        public bool TryLoadRecovery(out RecipeRecoveryData recovery, out string error)
        {
            recovery = null;
            error = string.Empty;
            if (!File.Exists(RecoveryPath))
            {
                return false;
            }

            try
            {
                recovery = (RecipeRecoveryData)ReadJson(RecoveryPath, typeof(RecipeRecoveryData));
                if (recovery == null || recovery.Recipe == null)
                {
                    throw new InvalidDataException("恢复文件不包含配方数据。");
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public string QuarantineRecovery()
        {
            if (!File.Exists(RecoveryPath))
            {
                return string.Empty;
            }

            string quarantined = RecoveryPath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            File.Move(RecoveryPath, quarantined);
            return quarantined;
        }

        public void DeleteRecovery()
        {
            if (File.Exists(RecoveryPath))
            {
                File.Delete(RecoveryPath);
            }
        }

        public void SaveLayout(UiLayoutState state)
        {
            if (state == null)
            {
                return;
            }

            WriteJsonAtomic(StatePath, state, typeof(UiLayoutState), null);
        }

        public UiLayoutState LoadLayout()
        {
            if (!File.Exists(StatePath))
            {
                return new UiLayoutState();
            }

            try
            {
                return (UiLayoutState)ReadJson(StatePath, typeof(UiLayoutState));
            }
            catch
            {
                return new UiLayoutState();
            }
        }

        public void SaveFlowSnippets(VmFlowSnippetLibraryData library)
        {
            if (library == null) throw new ArgumentNullException("library");
            library.Normalize();
            WriteJsonAtomic(FlowSnippetPath, library, typeof(VmFlowSnippetLibraryData), FlowSnippetPath + ".bak");
        }

        public VmFlowSnippetLibraryData LoadFlowSnippets(out string error)
        {
            error = string.Empty;
            if (!File.Exists(FlowSnippetPath)) return new VmFlowSnippetLibraryData();
            try
            {
                VmFlowSnippetLibraryData library = (VmFlowSnippetLibraryData)ReadJson(FlowSnippetPath, typeof(VmFlowSnippetLibraryData));
                if (library == null) throw new InvalidDataException("流程片段文件内容为空。");
                library.Normalize();
                return library;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return new VmFlowSnippetLibraryData();
            }
        }

        private static string HashNccResourceS131(string path)
        {
            using (FileStream stream = File.OpenRead(Path.GetFullPath(path)))
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void PackageOcrResourceS133(VmToolParameterData parameters, string toolName, string folderName,
            string folder, IDictionary<string, VmOcrModelResourceInfoS133> copied, IList<string> createdFiles, StringBuilder manifest)
        {
            var validation = parameters.OcrValidationS133 ?? new VmOcrValidationParametersS133();
            string validationError = validation.Validate();
            if (!string.IsNullOrEmpty(validationError)) throw new InvalidDataException(validationError);
            string legacyPath = parameters.OcrModelPath;
            if (string.IsNullOrWhiteSpace(legacyPath) && string.IsNullOrWhiteSpace(validation.ModelPath))
            {
                // Preserve the old built-in default while turning its bytes into a portable explicit version.
                string halconRoot = Environment.GetEnvironmentVariable("HALCONROOT");
                if (string.IsNullOrWhiteSpace(halconRoot)) halconRoot = @"C:\Program Files\MVTec\HALCON-20.11-Progress";
                legacyPath = Path.Combine(halconRoot, "ocr", "Industrial_NoRej.omc");
            }
            if (string.IsNullOrWhiteSpace(legacyPath))
                throw new InvalidDataException("OCR 旧字库路径缺失，不能猜测受管版本：" + toolName);
            string source = Path.GetFullPath(legacyPath);
            if (!string.IsNullOrWhiteSpace(validation.ModelPath) &&
                !string.Equals(source, Path.GetFullPath(validation.ModelPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("OCR 字库路径与 SHA 版本不是同一文件，未完成导出：" + toolName);

            VmOcrModelResourceInfoS133 packaged;
            // Hold the expected source bytes read-locked throughout import. A changed managed model is never
            // silently accepted. Legacy files have no saved SHA, so import establishes their real identity.
            using (var sourceLease = VmOcrModelResourceS133.OpenResource(source, validation.ModelSha256))
            {
                if (!copied.TryGetValue(source, out packaged))
                {
                    packaged = VmOcrModelResourceS133.Import(sourceLease.ModelPath, folder);
                    createdFiles.Add(packaged.ModelPath);
                    if (!string.Equals(packaged.ModelSha256, sourceLease.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("OCR 字库复制期间发生变化，未完成导出：" + toolName);
                    copied.Add(source, packaged);
                }
                if (!string.Equals(packaged.ModelSha256, sourceLease.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    (validation.ClassCount > 0 && validation.ClassCount != packaged.ClassCount))
                    throw new InvalidDataException("OCR 字库 SHA 或类别数与保存的身份不一致，未完成导出：" + toolName);
                using (var packagedLease = VmOcrModelResourceS133.OpenResource(packaged.ModelPath, packaged.ModelSha256)) { }
            }
            string relative = Path.Combine(folderName, Path.GetFileName(packaged.ModelPath));
            parameters.OcrModelPath = relative;
            validation.ModelPath = relative;
            validation.ModelSha256 = packaged.ModelSha256;
            validation.ClassCount = packaged.ClassCount;
            parameters.OcrValidationS133 = validation;
            manifest.AppendLine((toolName ?? "OCR") + " · OCR MLP 字库 → " + Path.GetFileName(packaged.ModelPath) +
                " · SHA256=" + packaged.ModelSha256 + " · 类别=" + packaged.ClassCount);
        }

        private static void WriteJsonAtomic(string path, object value, Type type, string backupPath)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("无法确定文件目录：" + fullPath);
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(directory, "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                WriteJson(temporaryPath, value, type);
                object verified = ReadJson(temporaryPath, type);
                if (verified == null)
                {
                    throw new InvalidDataException("临时文件回读验证失败。");
                }

                if (File.Exists(fullPath))
                {
                    if (!string.IsNullOrWhiteSpace(backupPath) && File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    File.Replace(temporaryPath, fullPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static string SerializeJson(object value, Type type)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static void WriteJson(string path, object value, Type type)
        {
            File.WriteAllText(path, SerializeJson(value, type), new UTF8Encoding(false));
        }

        private static object ReadJson(string path, Type type)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            using (FileStream stream = File.OpenRead(path))
            {
                return serializer.ReadObject(stream);
            }
        }
    }
}
