using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmSubflowPortData
    {
        [DataMember(Order = 1)] public string PortName { get; set; }
        [DataMember(Order = 2)] public string DisplayName { get; set; }
        [DataMember(Order = 3)] public string DataType { get; set; }
        [DataMember(Order = 4)] public string SourceToolId { get; set; }
        [DataMember(Order = 5)] public string SourcePortName { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public bool CollectIterations { get; set; }
        public VmSubflowPortData Clone() { return (VmSubflowPortData)MemberwiseClone(); }
    }

    [DataContract]
    public sealed class VmSubflowData
    {
        public const string InputProxyId = "$SubflowInput";
        [DataMember(Order = 1)] public List<ToolFlowRecipeItem> Tools { get; set; } = new List<ToolFlowRecipeItem>();
        [DataMember(Order = 2)] public List<VmSubflowPortData> Inputs { get; set; } = new List<VmSubflowPortData>();
        [DataMember(Order = 3)] public List<VmSubflowPortData> Outputs { get; set; } = new List<VmSubflowPortData>();
        [DataMember(Order = 4, EmitDefaultValue = false)] public string LoopMode { get; set; }
        [DataMember(Order = 5)] public int RepeatCount { get; set; } = 1;
        [DataMember(Order = 6, EmitDefaultValue = false)] public string LoopInputPort { get; set; }

        public VmSubflowData Clone()
        {
            return new VmSubflowData
            {
                LoopMode = LoopMode, RepeatCount = RepeatCount, LoopInputPort = LoopInputPort,
                Tools = (Tools ?? new List<ToolFlowRecipeItem>()).Select(CloneTool).ToList(),
                Inputs = (Inputs ?? new List<VmSubflowPortData>()).Select(port => port.Clone()).ToList(),
                Outputs = (Outputs ?? new List<VmSubflowPortData>()).Select(port => port.Clone()).ToList()
            };
        }

        public static ToolFlowRecipeItem CloneTool(ToolFlowRecipeItem item)
        {
            return new ToolFlowRecipeItem
            {
                ToolId = item.ToolId, ToolType = item.ToolType, InstanceName = item.InstanceName, IsEnabled = item.IsEnabled,
                Parameters = item.Parameters == null ? null : item.Parameters.Clone(),
                InputBindings = (item.InputBindings ?? new List<VmToolInputBindingData>()).Select(binding => binding.Clone()).ToList(),
                RoiIds = item.RoiIds == null ? null : item.RoiIds.ToList(), GraphX = item.GraphX, GraphY = item.GraphY,
                GroupId = item.GroupId, GroupName = item.GroupName, IsGroupCollapsed = item.IsGroupCollapsed, IsBreakpoint = item.IsBreakpoint,
                NumericJudge = item.NumericJudge == null ? null : new NumericJudgeRecipeData
                {
                    InputToolId = item.NumericJudge.InputToolId, InputPortName = item.NumericJudge.InputPortName,
                    Operator = item.NumericJudge.Operator, LowerLimit = item.NumericJudge.LowerLimit,
                    UpperLimit = item.NumericJudge.UpperLimit, Tolerance = item.NumericJudge.Tolerance
                }
            };
        }

        public string Validate(int depth = 0)
        {
            if (depth >= 8) return "子流程嵌套最多 8 层。";
            if (Tools == null || Tools.Count == 0 || Tools.Count > 256) return "子流程必须包含 1–256 个模块。";
            if (Inputs == null || Outputs == null || Outputs.Count == 0) return "子流程输入/输出定义缺失。";
            string loopError = VmSubflowLoopS111.Validate(this);
            if (loopError != null) return loopError;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in Tools)
            {
                VmToolKind kind;
                if (tool == null || string.IsNullOrWhiteSpace(tool.ToolId) || tool.ToolId == InputProxyId || !ids.Add(tool.ToolId) || !Enum.TryParse(tool.ToolType, out kind) ||
                    !Enum.IsDefined(typeof(VmToolKind), kind) || kind == VmToolKind.SubflowInput)
                    return "子流程包含无效类型、重复或空模块标识。";
                if (kind == VmToolKind.SubflowCall)
                {
                    var child = tool.Parameters == null ? null : tool.Parameters.Subflow;
                    string error = child == null ? "嵌套子流程内容缺失。" : child.Validate(depth + 1);
                    if (error != null) return error;
                }
            }
            foreach (var ports in new[] { Inputs, Outputs })
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var port in ports)
                    if (port == null || string.IsNullOrWhiteSpace(port.PortName) || port.PortName == "ResultCode" || port.PortName == "$RunCondition" ||
                        !names.Add(port.PortName) || !IsBoundaryTypeSupported(port.DataType)) return "子流程端口名称或类型无效。";
            }
            foreach (var port in Outputs)
                if (!ids.Contains(port.SourceToolId ?? string.Empty) || string.IsNullOrWhiteSpace(port.SourcePortName)) return "子流程返回来源不存在。";
            return null;
        }

        public static bool IsBoundaryTypeSupported(string type)
        {
            return VmFlowValueContractS103.Normalize(type, true) != null;
        }

        public static IEnumerable<ToolFlowRecipeItem> EnumerateTools(IEnumerable<ToolFlowRecipeItem> tools, int depth = 0)
        {
            if (depth > 8) throw new InvalidOperationException("子流程资源嵌套超过 8 层。");
            foreach (var tool in tools ?? Enumerable.Empty<ToolFlowRecipeItem>())
            {
                if (tool == null) continue;
                yield return tool;
                var child = tool.Parameters == null ? null : tool.Parameters.Subflow;
                if (child != null)
                    foreach (var nested in EnumerateTools(child.Tools, depth + 1)) yield return nested;
            }
        }
    }
}
