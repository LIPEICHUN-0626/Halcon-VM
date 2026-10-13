using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public static class ProductionDefinitionIdentityS138
    {
        public const int Version = 1;
        public static string ComputeFromSnapshotJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("没有已保存定义快照。 ");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return Compute((VisionRecipe)new DataContractJsonSerializer(typeof(VisionRecipe)).ReadObject(stream));
        }
        public static string Compute(VisionRecipe recipe)
        {
            if (recipe == null || recipe.SchemaVersion != RecipeSchema.CurrentVersion)
                throw new InvalidOperationException("未知方案版本不能使用 v1 稳定定义归一化。 ");
            var serializer = new DataContractJsonSerializer(typeof(VisionRecipe));
            VisionRecipe copy;
            using (var stream = new MemoryStream())
            { serializer.WriteObject(stream, recipe); stream.Position = 0; copy = (VisionRecipe)serializer.ReadObject(stream); }
            // CaptureRecipe derives LastImageDirectory from currentImagePath, not from the configured source.
            // Source LocalImagePath / ImageReplayFolder, device settings, model SHA and scripts stay untouched.
            copy.LastImageDirectory = null; copy.SavedAtUtc = null; copy.ActiveFlowId = null;
            copy.RightImageCrosshairs = null; copy.LeftImageCrosshairs = null;
            copy.ResultLayerStyles = null; copy.ProductionRunLayout = null;
            foreach (var flow in copy.Flows ?? new System.Collections.Generic.List<VmProjectFlowData>())
                if (flow != null) flow.ResultLayerStyles = null;
            foreach (var tool in VmProjectFlowData.EnumerateRecipeTools(copy))
            {
                // Only diagram position/group presentation is excluded; parameters and breakpoints remain conservative.
                tool.GraphX = null; tool.GraphY = null; tool.GroupId = null; tool.GroupName = null; tool.IsGroupCollapsed = false;
            }
            foreach (var variable in copy.GlobalVariables ?? new System.Collections.Generic.List<VmGlobalVariableData>())
            {
                // SetCurrentValue is the runtime writer. Keep identity, type and every default field.
                variable.NumberValue = 0; variable.TextValue = null; variable.BooleanValue = false;
                variable.ResultCodeValue = null; variable.CurrentArrayJson = null;
            }
            using (var stream = new MemoryStream())
            using (var sha = SHA256.Create())
            {
                serializer.WriteObject(stream, copy);
                byte[] definitionBytes = Encoding.UTF8.GetBytes("AlgorithmDefinition/v1\n" + Encoding.UTF8.GetString(stream.ToArray()));
                return BitConverter.ToString(sha.ComputeHash(definitionBytes)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
