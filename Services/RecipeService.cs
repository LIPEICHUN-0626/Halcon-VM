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
        {
            recipeDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recipes");
            stateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            Directory.CreateDirectory(recipeDirectory);
            Directory.CreateDirectory(stateDirectory);
        }

        public string RecipeDirectory
        {
            get { return recipeDirectory; }
        }

        public string StatePath
        {
            get { return Path.Combine(stateDirectory, "ui-state.json"); }
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

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            WriteJson(path, recipe, typeof(VisionRecipe));
        }

        public VisionRecipe LoadRecipe(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("配方文件不存在。", path);
            }

            return (VisionRecipe)ReadJson(path, typeof(VisionRecipe));
        }

        public void SaveLayout(UiLayoutState state)
        {
            if (state == null)
            {
                return;
            }

            WriteJson(StatePath, state, typeof(UiLayoutState));
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

        private static void WriteJson(string path, object value, Type type)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                string json = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(path, json, new UTF8Encoding(false));
            }
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
