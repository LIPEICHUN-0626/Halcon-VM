using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmFlowSnippetLibraryData
    {
        public VmFlowSnippetLibraryData()
        {
            SchemaVersion = 1;
            Snippets = new List<VmFlowSnippetData>();
        }

        [DataMember(Order = 1)] public int SchemaVersion { get; set; }
        [DataMember(Order = 2)] public List<VmFlowSnippetData> Snippets { get; set; }

        public void Normalize()
        {
            SchemaVersion = 1;
            Snippets = (Snippets ?? new List<VmFlowSnippetData>()).Where(item => item != null).ToList();
            foreach (VmFlowSnippetData snippet in Snippets) snippet.Normalize();
        }
    }

    [DataContract]
    public sealed class VmFlowSnippetData
    {
        public VmFlowSnippetData()
        {
            SnippetId = Guid.NewGuid().ToString("N");
            Name = "流程片段";
            UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            ToolFlow = new List<ToolFlowRecipeItem>();
            ExternalInputs = new List<VmFlowSnippetExternalInputData>();
        }

        [DataMember(Order = 1)] public string SnippetId { get; set; }
        [DataMember(Order = 2)] public string Name { get; set; }
        [DataMember(Order = 3)] public string UpdatedAtUtc { get; set; }
        [DataMember(Order = 4)] public List<ToolFlowRecipeItem> ToolFlow { get; set; }
        [DataMember(Order = 5)] public List<VmFlowSnippetExternalInputData> ExternalInputs { get; set; }

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(SnippetId)) SnippetId = Guid.NewGuid().ToString("N");
            Name = string.IsNullOrWhiteSpace(Name) ? "流程片段" : Name.Trim();
            if (string.IsNullOrWhiteSpace(UpdatedAtUtc)) UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            ToolFlow = (ToolFlow ?? new List<ToolFlowRecipeItem>()).Where(item => item != null).ToList();
            ExternalInputs = (ExternalInputs ?? new List<VmFlowSnippetExternalInputData>()).Where(item => item != null).ToList();
        }
    }

    [DataContract]
    public sealed class VmFlowSnippetExternalInputData
    {
        [DataMember(Order = 1)] public string TargetTemplateToolId { get; set; }
        [DataMember(Order = 2)] public string TargetToolName { get; set; }
        [DataMember(Order = 3)] public string TargetPortName { get; set; }
        [DataMember(Order = 4)] public string DataType { get; set; }
        [DataMember(Order = 5)] public bool IsRequired { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public string PreferredSourceToolId { get; set; }
        [DataMember(Order = 7, EmitDefaultValue = false)] public string PreferredSourcePortName { get; set; }
        [DataMember(Order = 8, EmitDefaultValue = false)] public string PreferredSourceText { get; set; }
    }
}
