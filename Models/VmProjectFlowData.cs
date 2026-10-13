using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmTcpFlowTriggerData
    {
        [DataMember(Order = 1)] public string FlowId { get; set; }
        [DataMember(Order = 2)] public string Command { get; set; } = "RUN";
        [DataMember(Order = 3, EmitDefaultValue = false)] public string ReturnModeS123 { get; set; }
        [DataMember(Order = 4, EmitDefaultValue = false)] public string ReturnHostS123 { get; set; }
        [DataMember(Order = 5, EmitDefaultValue = false)] public int ReturnPortS123 { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public string ReturnComPortS123 { get; set; }
        [DataMember(Order = 7, EmitDefaultValue = false)] public int ReturnBaudS123 { get; set; }
        [DataMember(Order = 8, EmitDefaultValue = false)] public string ReturnParityS123 { get; set; }
        [DataMember(Order = 9, EmitDefaultValue = false)] public int ReturnStopBitsS123 { get; set; }
        [DataMember(Order = 10, EmitDefaultValue = false)] public int ReturnStationS123 { get; set; }
        [DataMember(Order = 11, EmitDefaultValue = false)] public int ReturnFunctionS123 { get; set; }
        [DataMember(Order = 12, EmitDefaultValue = false)] public int ReturnAddressS123 { get; set; }
        [DataMember(Order = 13, EmitDefaultValue = false)] public int ReturnTimeoutS123 { get; set; }
        [DataMember(Order = 14, EmitDefaultValue = false)] public int ReturnOkValueS123 { get; set; }
        [DataMember(Order = 15, EmitDefaultValue = false)] public int ReturnNgValueS123 { get; set; }
        [DataMember(Order = 16, EmitDefaultValue = false)] public int ReturnErrorValueS123 { get; set; }
        [DataMember(Order = 17, EmitDefaultValue = false)] public int ReturnUnitS123 { get; set; }
        public VmTcpFlowTriggerData NormalizeS123()
        {
            if (string.IsNullOrWhiteSpace(ReturnModeS123)) ReturnModeS123 = "None";
            if (ReturnHostS123 == null) ReturnHostS123 = "127.0.0.1";
            if (ReturnModeS123 == "None" && ReturnPortS123 == 0) ReturnPortS123 = 502;
            if (ReturnComPortS123 == null) ReturnComPortS123 = "COM1";
            if (ReturnModeS123 == "None" && ReturnBaudS123 == 0) ReturnBaudS123 = 9600;
            if (ReturnParityS123 == null) ReturnParityS123 = "Even";
            if (ReturnModeS123 == "None" && ReturnStopBitsS123 == 0) ReturnStopBitsS123 = 1;
            if (ReturnModeS123 == "None" && ReturnStationS123 == 0) ReturnStationS123 = 1;
            if (ReturnModeS123 == "None" && ReturnUnitS123 == 0) ReturnUnitS123 = 1;
            if (ReturnModeS123 == "None" && ReturnFunctionS123 == 0) ReturnFunctionS123 = 6;
            if (ReturnModeS123 == "None" && ReturnTimeoutS123 == 0) ReturnTimeoutS123 = 1500;
            if (ReturnModeS123 == "None" && ReturnOkValueS123 == 0 && ReturnNgValueS123 == 0 && ReturnErrorValueS123 == 0)
            { ReturnOkValueS123 = 1; ReturnNgValueS123 = 0; ReturnErrorValueS123 = 2; }
            return this;
        }
        public VmTcpFlowTriggerData Clone() { return (VmTcpFlowTriggerData)MemberwiseClone(); }
    }

    [DataContract]
    public sealed class VmProjectFlowData
    {
        [DataMember(Order = 1)] public string FlowId { get; set; } = Guid.NewGuid().ToString("N");
        [DataMember(Order = 2)] public string Name { get; set; } = "流程 01";
        [DataMember(Order = 3)] public List<ToolFlowRecipeItem> Tools { get; set; } = new List<ToolFlowRecipeItem>();
        [DataMember(Order = 4)] public VmFlowRunPolicy RunPolicy { get; set; } = new VmFlowRunPolicy();
        [DataMember(Order = 5, EmitDefaultValue = false)] public List<ResultLayerStyleRecipeData> ResultLayerStyles { get; set; }

        public VmProjectFlowData Clone()
        {
            return new VmProjectFlowData { FlowId = FlowId, Name = Name,
                Tools = Tools.Select(VmSubflowData.CloneTool).ToList(), RunPolicy = (RunPolicy ?? new VmFlowRunPolicy()).Normalize(),
                ResultLayerStyles = ResultLayerStyles == null ? null : ResultLayerStyles.Select(item => item.Clone()).ToList() };
        }

        // A v3 project has one authoritative collection. Legacy ToolFlow is used only when Flows is absent.
        public static IEnumerable<ToolFlowRecipeItem> EnumerateRecipeTools(VisionRecipe recipe)
        {
            if (recipe == null) yield break;
            var roots = recipe.Flows == null ? new[] { recipe.ToolFlow } : recipe.Flows.Select(flow => flow.Tools);
            foreach (var root in roots)
                foreach (var tool in VmSubflowData.EnumerateTools(root)) yield return tool;
        }

        public static void ValidateProject(VisionRecipe recipe)
        {
            if (recipe.Flows == null) return; // Preserve the original legacy migration path.
            if (recipe.Flows.Count == 0) throw new InvalidOperationException("方案至少需要一条流程；空白流程可以不含模块。");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var flow in recipe.Flows)
            {
                if (flow == null || string.IsNullOrWhiteSpace(flow.FlowId) || !ids.Add(flow.FlowId) ||
                    string.IsNullOrWhiteSpace(flow.Name) || !names.Add(flow.Name.Trim()) || flow.Tools == null)
                    throw new InvalidOperationException("流程标识或名称重复/缺失，或流程内容缺失；未载入方案。");
                ValidateScope(flow.Tools, false, 0);
            }
        }

        private static void ValidateScope(List<ToolFlowRecipeItem> tools, bool internalScope, int depth)
        {
            if (depth > 8) throw new InvalidOperationException("子流程嵌套超过 8 层。");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in tools)
            {
                VmToolKind kind;
                if (tool == null || string.IsNullOrWhiteSpace(tool.ToolId) || tool.ToolId == VmSubflowData.InputProxyId ||
                    !ids.Add(tool.ToolId) || !Enum.TryParse(tool.ToolType, out kind) || !Enum.IsDefined(typeof(VmToolKind), kind) || kind == VmToolKind.SubflowInput)
                    throw new InvalidOperationException("流程包含无效或重复模块标识/类型。");
                var child = tool.Parameters == null ? null : tool.Parameters.Subflow;
                if (kind == VmToolKind.SubflowCall)
                {
                    if (child == null || child.Tools == null) throw new InvalidOperationException("子流程内容缺失。");
                    ValidateScope(child.Tools, true, depth + 1);
                }
            }
            // Unresolved subscriptions remain editable and are reported by the original scoped input diagnostics.
            // Do not make an incomplete engineering draft impossible to save and reopen.
        }

        public VmProjectFlowData CreateCopy(string name)
        {
            var copy = Clone(); copy.FlowId = Guid.NewGuid().ToString("N"); copy.Name = name;
            copy.ResultLayerStyles = null; // New tool identities start with the normal layer style defaults.
            RemapScope(copy.Tools, null); return copy;
        }

        private static void RemapScope(List<ToolFlowRecipeItem> tools, List<VmSubflowPortData> returns)
        {
            var ids = tools.ToDictionary(tool => tool.ToolId, tool => Guid.NewGuid().ToString("N"), StringComparer.OrdinalIgnoreCase);
            var groups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in tools)
            {
                tool.ToolId = ids[tool.ToolId];
                foreach (var binding in tool.InputBindings ?? new List<VmToolInputBindingData>())
                    if (ids.ContainsKey(binding.SourceToolId ?? string.Empty)) binding.SourceToolId = ids[binding.SourceToolId];
                if (tool.NumericJudge != null && ids.ContainsKey(tool.NumericJudge.InputToolId ?? string.Empty))
                    tool.NumericJudge.InputToolId = ids[tool.NumericJudge.InputToolId];
                if (!string.IsNullOrWhiteSpace(tool.GroupId))
                {
                    if (!groups.ContainsKey(tool.GroupId)) groups.Add(tool.GroupId, Guid.NewGuid().ToString("N"));
                    tool.GroupId = groups[tool.GroupId];
                }
                if (tool.Parameters != null && tool.Parameters.Subflow != null)
                    RemapScope(tool.Parameters.Subflow.Tools, tool.Parameters.Subflow.Outputs);
            }
            foreach (var port in returns ?? new List<VmSubflowPortData>())
                if (ids.ContainsKey(port.SourceToolId ?? string.Empty)) port.SourceToolId = ids[port.SourceToolId];
        }
    }
}
