using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class RecipeService
    {
        private readonly string recipeDirectory;
        private readonly string stateDirectory;

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
            Directory.CreateDirectory(recipeDirectory);
            Directory.CreateDirectory(stateDirectory);
        }

        public string RecipeDirectory
        {
            get { return recipeDirectory; }
        }

        public string StateDirectory
        {
            get { return stateDirectory; }
        }

        public string StatePath
        {
            get { return Path.Combine(stateDirectory, "ui-state.json"); }
        }

        public string RecoveryPath
        {
            get { return Path.Combine(stateDirectory, "active-recipe-recovery.json"); }
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
            return recipe;
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
