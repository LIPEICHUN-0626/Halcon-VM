using System;
using System.IO;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    public static class RecipeSchema
    {
        public const int CurrentVersion = 2;
    }

    [DataContract]
    public sealed class RecipeRecoveryData
    {
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string FormalRecipePath { get; set; }

        [DataMember(Order = 2)]
        public string CapturedAtUtc { get; set; }

        [DataMember(Order = 3)]
        public VisionRecipe Recipe { get; set; }
    }

    public sealed class VmRecentRecipeItem
    {
        public string Path { get; set; }

        public string Name
        {
            get { return string.IsNullOrWhiteSpace(Path) ? "--" : System.IO.Path.GetFileNameWithoutExtension(Path); }
        }

        public string DirectoryText
        {
            get { return string.IsNullOrWhiteSpace(Path) ? "--" : (System.IO.Path.GetDirectoryName(Path) ?? "--"); }
        }

        public string StateText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
                {
                    return "文件不存在";
                }

                return "更新 " + File.GetLastWriteTime(Path).ToString("yyyy-MM-dd HH:mm");
            }
        }
    }
}
