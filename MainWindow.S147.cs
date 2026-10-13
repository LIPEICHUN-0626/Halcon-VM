using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class ModulePrerequisiteFactS147
        {
            public string Key { get; set; }
            public string Kind { get; set; }
            public string PortName { get; set; }
            public string Reason { get; set; }
            public string ActionText { get; set; }
            public string SessionId { get; set; }
            public string FlowId { get; set; }
            public string ToolId { get; set; }
            public string DefinitionKey { get; set; }
            public string InstanceStamp { get; set; }
            public bool IsBlocking { get; set; }
        }

        private sealed class ModulePrerequisiteStampS147
        {
            public readonly string Value = Guid.NewGuid().ToString("N");
        }

        private readonly ConditionalWeakTable<VmToolInstance, ModulePrerequisiteStampS147> modulePrerequisiteStampsS147 =
            new ConditionalWeakTable<VmToolInstance, ModulePrerequisiteStampS147>();
        private ISet<string> modulePrerequisiteScopeS147;
        private ISet<string> modulePrerequisiteConditionalIdsS147;
        private int modulePrerequisiteFactCountS147;
        private bool modulePrerequisiteScanIncompleteS147;

        [DataContract]
        private sealed class ModulePrerequisiteDefinitionS147
        {
            [DataMember(Order = 1)] public string Kind;
            [DataMember(Order = 2)] public VmToolParameterData Parameters;
            [DataMember(Order = 3)] public List<VmToolInputBindingData> Bindings;
            [DataMember(Order = 4)] public List<string> RoiIds;
            [DataMember(Order = 5)] public List<RoiLayerRecipeData> Rois;
        }

        private sealed class ModulePrerequisiteStreamS147 : MemoryStream
        {
            private readonly int limit;
            public ModulePrerequisiteStreamS147(int limit) { this.limit = limit; }
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count > limit - Position) throw new InvalidOperationException("模块前提定义超过字节预算。");
                base.Write(buffer, offset, count);
            }
            public override void WriteByte(byte value)
            {
                if (Position >= limit) throw new InvalidOperationException("模块前提定义超过字节预算。");
                base.WriteByte(value);
            }
        }

        private void ResetModulePrerequisiteScanS147(ISet<string> scopeS147 = null)
        {
            modulePrerequisiteScopeS147 = scopeS147;
            modulePrerequisiteConditionalIdsS147 = scopeS147 == null ? null : GetConditionalBranchToolIdsS100();
            modulePrerequisiteFactCountS147 = 0;
            modulePrerequisiteScanIncompleteS147 = false;
        }

        private static bool IsModulePrerequisiteKindS147(VmToolKind kind)
        {
            return kind == VmToolKind.ShapeMatch || kind == VmToolKind.NccTemplateMatch ||
                kind == VmToolKind.PoseImageAlignment || kind == VmToolKind.PlanarCalibration2D;
        }

        private List<ModulePrerequisiteFactS147> GetModulePrerequisiteFactsS147(VmToolInstance tool)
        {
            var facts = new List<ModulePrerequisiteFactS147>();
            if (tool == null || !tool.IsEnabled || !IsModulePrerequisiteKindS147(tool.Kind)) return facts;
            string definition;
            try { definition = ModulePrerequisiteIdentityS147(tool); }
            catch (Exception)
            {
                facts.Add(CreateModulePrerequisiteFactS147(tool, "definition-unknown", "Unknown", string.Empty,
                    "已应用定义身份无法在64KiB预算内完整冻结，前提提示不完整；不生成部分定义键或宣称没有问题。", string.Empty,
                    "unknown:definition-unavailable", true));
                return facts;
            }
            var parameters = tool.Parameters;
            if (parameters == null)
            {
                facts.Add(CreateModulePrerequisiteFactS147(tool, "parameters-missing", "Unknown", string.Empty,
                    "当前模块没有已应用参数，无法确定运行前提。", string.Empty, definition, true));
                return facts;
            }

            // Only this module's declared inputs are inspected. A source with no
            // current result is waiting, not a missing applied definition.
            foreach (var port in ToolMetadata.GetInputPorts(tool.Kind))
            {
                if (tool.Kind == VmToolKind.ShapeMatch && parameters.TemplateTargetsS145 == null &&
                    port.PortName == "Image" && tool.GetInputBinding(port.PortName) == null) continue;
                var binding = tool.GetInputBinding(port.PortName);
                if (binding == null && port.IsOptional) continue;
                string error = ModulePrerequisiteInputErrorS147(tool, port, binding);
                if (!string.IsNullOrWhiteSpace(error)) facts.Add(CreateModulePrerequisiteFactS147(tool,
                    "input-" + port.PortName, "Input", port.PortName, error, "打开输入", definition, true));
            }

            if (tool.Kind == VmToolKind.NccTemplateMatch)
            {
                var model = parameters.NccMatchingS131;
                bool declared = model != null && (!string.IsNullOrWhiteSpace(model.ModelPath) ||
                    !string.IsNullOrWhiteSpace(model.ModelSha256) || model.ModelFormatVersion != 0);
                string error = model == null ? string.Empty : model.Validate();
                if (!declared && (model == null || !model.HasModel))
                    facts.Add(CreateModulePrerequisiteFactS147(tool, "ncc-missing", "Template", string.Empty,
                        "尚无已应用NCC模板；请在本模块读取真实输入、框选制作并应用。有效编辑参数不等于已有模型。", "配置模板", definition, true));
                else if (!string.IsNullOrWhiteSpace(error) || !model.HasModel)
                    facts.Add(CreateModulePrerequisiteFactS147(tool, "ncc-invalid", "Template", string.Empty,
                        "已有NCC模型声明无效：" + (string.IsNullOrWhiteSpace(error) ? "模型版本、路径或SHA声明不完整。" : error), "配置模板", definition, true));
            }
            else if (tool.Kind == VmToolKind.ShapeMatch)
            {
                if (parameters.TemplateTargetsS145 != null && string.IsNullOrWhiteSpace(parameters.ShapeModelPath))
                    facts.Add(CreateModulePrerequisiteFactS147(tool, "shape-missing", "Template", string.Empty,
                        "正式多目标任务尚未声明本模块Shape模型；旧全局模板不代表本模块模型资格，请制作/选择并应用。", "配置模板", definition, true));
                else if (parameters.TemplateTargetsS145 == null && string.IsNullOrWhiteSpace(parameters.ShapeModelPath))
                    facts.Add(CreateModulePrerequisiteFactS147(tool, "shape-legacy", "Unknown", string.Empty,
                        "本模块未声明Shape模型，仍可能使用旧全局兼容路径；此提示不加载模型，不判定兼容模型可用，也不新增阻断。", string.Empty, definition, false));
                else
                {
                    string error = parameters.Validate(VmToolKind.ShapeMatch);
                    if (!string.IsNullOrWhiteSpace(error)) facts.Add(CreateModulePrerequisiteFactS147(tool, "shape-invalid", "Template", string.Empty,
                        "本模块Shape配置无效：" + error, "配置模板", definition, true));
                }
            }
            else if (tool.Kind == VmToolKind.PoseImageAlignment)
            {
                var reference = parameters.PoseImageAlignmentS132;
                string error = reference == null ? string.Empty : reference.Validate();
                if (reference == null || !reference.HasReference)
                    facts.Add(CreateModulePrerequisiteFactS147(tool, "reference-missing", "Reference", string.Empty,
                        "尚无已应用固定参考；请真实运行已应用定位，显式采参考并应用，不使用上一帧或零位姿补齐。", "配置固定参考", definition, true));
                else if (!string.IsNullOrWhiteSpace(error))
                    facts.Add(CreateModulePrerequisiteFactS147(tool, "reference-invalid", "Reference", string.Empty,
                        "已应用固定参考定义无效：" + error, "配置固定参考", definition, true));
                else if (!facts.Any(fact => fact.Kind == "Input"))
                {
                    try
                    {
                        string signature = ModulePrerequisiteAlignmentSignatureS147(tool);
                        if (!string.Equals(signature, reference.ReferenceSourceSignature, StringComparison.Ordinal))
                            facts.Add(CreateModulePrerequisiteFactS147(tool, "reference-source", "Reference", string.Empty,
                                "固定参考的正式输入来源/处理定义已改变；请在当前已应用链重新运行并显式采参考，不沿用旧参考。", "配置固定参考", definition, true));
                    }
                    catch (Exception)
                    {
                        facts.Add(CreateModulePrerequisiteFactS147(tool, "reference-signature-unknown", "Unknown", string.Empty,
                            "当前来源签名无法在纯配置预算内完整比较；不判断参考匹配，仍由原运行路径复核。", string.Empty, definition, false));
                    }
                }
            }
            else if (tool.Kind == VmToolKind.PlanarCalibration2D)
            {
                string error = ModulePrerequisitePlanarErrorS147(parameters);
                if (!string.IsNullOrWhiteSpace(error)) facts.Add(CreateModulePrerequisiteFactS147(tool, "calibration-invalid", "Calibration", string.Empty,
                    error + " 此处只检查必要静态条件，不拟合、不证明单位实测或现场精度。", "配置标定", definition, true));
            }
            if (facts.Count > 6)
            {
                facts.RemoveRange(5, facts.Count - 5);
                facts.Add(CreateModulePrerequisiteFactS147(tool, "module-budget", "Unknown", string.Empty,
                    "模块前提超过六条预算，提示不完整，不能将未列出的项视为已准备。", string.Empty, definition, true));
            }
            return facts;
        }

        private ModulePrerequisiteFactS147 CreateModulePrerequisiteFactS147(VmToolInstance tool, string key, string kind,
            string portName, string reason, string actionText, string definition, bool blocking)
        {
            return new ModulePrerequisiteFactS147
            {
                Key = ModulePrerequisiteTextS147(tool.ToolId, 256) + "|prerequisite|" + key, Kind = kind, PortName = portName,
                Reason = reason.Length <= 1024 ? reason : reason.Substring(0, 1000) + "（说明超限，已省略）",
                ActionText = actionText, SessionId = ModulePrerequisiteTextS147(projectSessionIdS104, 256),
                FlowId = ModulePrerequisiteTextS147(activeProjectFlowIdS104, 256),
                ToolId = ModulePrerequisiteTextS147(tool.ToolId, 256), DefinitionKey = definition,
                InstanceStamp = modulePrerequisiteStampsS147.GetValue(tool, ignored => new ModulePrerequisiteStampS147()).Value,
                IsBlocking = blocking
            };
        }

        private static string ModulePrerequisiteTextS147(string text, int maximum)
        {
            return text == null ? string.Empty : text.Length <= maximum ? text : text.Substring(0, maximum);
        }

        private bool IsModulePrerequisiteFactCurrentS147(ModulePrerequisiteFactS147 fact,
            out VmToolInstance tool, out ModulePrerequisiteFactS147 currentFact)
        {
            tool = null; currentFact = null;
            if (fact == null || fact.SessionId != projectSessionIdS104 || fact.FlowId != activeProjectFlowIdS104) return false;
            var matches = flowTools.Where(item => item.ToolId == fact.ToolId).Take(2).ToArray();
            if (matches.Length != 1 || !matches[0].IsEnabled ||
                modulePrerequisiteStampsS147.GetValue(matches[0], ignored => new ModulePrerequisiteStampS147()).Value != fact.InstanceStamp) return false;
            try
            {
                currentFact = GetModulePrerequisiteFactsS147(matches[0]).FirstOrDefault(item => item.Key == fact.Key &&
                    item.Kind == fact.Kind && item.PortName == fact.PortName && item.DefinitionKey == fact.DefinitionKey && item.Reason == fact.Reason &&
                    item.IsBlocking == fact.IsBlocking && item.ActionText == fact.ActionText);
                if (currentFact == null) return false;
                tool = matches[0]; return true;
            }
            catch (Exception) { currentFact = null; return false; }
        }

        private string ModulePrerequisiteInputErrorS147(VmToolInstance tool, VmPortDefinition port, VmToolInputBindingData binding)
        {
            if (binding == null) return "尚未明确连接正式 " + port.PortName + " 输入；上游尚未运行与缺少绑定不同。";
            if (tool.InputBindings.Count(item => item != null && string.Equals(item.TargetPortName, port.PortName, StringComparison.OrdinalIgnoreCase)) != 1)
                return "该正式输入的订阅不唯一，请重新选择来源。";
            var sources = flowTools.Where(source => source.ToolId == binding.SourceToolId).Take(2).ToArray();
            if (sources.Length != 1) return "正式输入来源不存在或身份不唯一，请重新选择。";
            var sourceTool = sources[0];
            if (!sourceTool.IsEnabled || flowTools.IndexOf(sourceTool) >= flowTools.IndexOf(tool))
                return "正式输入来源须为当前模块之前的启用工具。";
            // Dynamic port schemas remain with the existing S69 diagnostic.
            // Never Normalize the actual source to obtain a hint.
            if (sourceTool.Kind == VmToolKind.CSharpDllScript || sourceTool.Kind == VmToolKind.SubflowCall ||
                sourceTool.Kind == VmToolKind.SubflowInput || sourceTool.Kind == VmToolKind.JsonRead ||
                sourceTool.Kind == VmToolKind.BranchValueSelection || sourceTool.Kind == VmToolKind.GlobalVariableRead) return null;
            var outputs = ToolMetadata.GetOutputPorts(sourceTool.Kind).Where(output =>
                string.Equals(output.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if (outputs.Length != 1) return "正式来源输出端口不存在或不唯一，请重新选择。";
            return string.Equals(outputs[0].DataType, port.DataType, StringComparison.OrdinalIgnoreCase) ? null :
                "已配置端口类型不匹配：" + outputs[0].DataType + " → " + port.DataType + "。";
        }

        private static string ModulePrerequisitePlanarErrorS147(VmToolParameterData parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.PlanarCalibrationUnitS124) || parameters.PlanarCalibrationUnitS124.Length > 16)
                return "尚未填写1–16字符的真实世界单位。";
            var samples = parameters.PlanarCalibrationSamplesS124;
            if (samples == null || samples.Count < 3) return "尚未应用至少三个真实像素/世界对应点。";
            if (samples.Count > 64) return "平面标定对应点超过64个，定义超出原标定预算。";
            string context = samples[0] == null ? null : samples[0].ImageContextId;
            if (string.IsNullOrWhiteSpace(context) || context.StartsWith("Pending:", StringComparison.Ordinal))
                return "标定样本缺少已确定的正式图像来源身份。";
            foreach (var sample in samples)
                if (sample == null || !VmNccParametersS131.Finite(sample.PixelRow) || !VmNccParametersS131.Finite(sample.PixelColumn) ||
                    !VmNccParametersS131.Finite(sample.WorldX) || !VmNccParametersS131.Finite(sample.WorldY) ||
                    !string.Equals(sample.ImageContextId, context, StringComparison.OrdinalIgnoreCase))
                    return "已应用标定样本有空值、非有限值或跨图像链身份。";
            for (int i = 0; i < samples.Count; i++)
                for (int j = i + 1; j < samples.Count; j++)
                    if (Math.Abs(samples[i].PixelRow - samples[j].PixelRow) + Math.Abs(samples[i].PixelColumn - samples[j].PixelColumn) < 1e-3 ||
                        Math.Abs(samples[i].WorldX - samples[j].WorldX) + Math.Abs(samples[i].WorldY - samples[j].WorldY) < 1e-9)
                        return "已应用标定对应点存在重复像素点或世界点，请在原标定任务修正。";
            return null;
        }

        private string ModulePrerequisiteIdentityS147(VmToolInstance tool)
        {
            if (string.IsNullOrWhiteSpace(tool.ToolId) || tool.ToolId.Length > 256 ||
                string.IsNullOrWhiteSpace(projectSessionIdS104) || projectSessionIdS104.Length > 256 ||
                string.IsNullOrWhiteSpace(activeProjectFlowIdS104) || activeProjectFlowIdS104.Length > 256 ||
                tool.InputBindings.Count > 64 || tool.BoundRoiIds.Count > 256)
                throw new InvalidOperationException("模块订阅/ROI超出身份预算。");
            var definition = new ModulePrerequisiteDefinitionS147
            {
                Kind = tool.Kind.ToString(), Parameters = tool.Parameters,
                Bindings = tool.InputBindings.ToList(), RoiIds = tool.BoundRoiIds.ToList(),
                Rois = ModulePrerequisiteRoisS147(tool)
            };
            int budget = 65536;
            return "module-prerequisite-v1:" + ModulePrerequisiteHashS147(definition, typeof(ModulePrerequisiteDefinitionS147), ref budget).ToLowerInvariant();
        }

        private List<RoiLayerRecipeData> ModulePrerequisiteRoisS147(VmToolInstance tool)
        {
            if (roiLayers.Count > 2048 || tool.BoundRoiIds.Count > 256) throw new InvalidOperationException("ROI身份集合超过预算。");
            var rois = new List<RoiLayerRecipeData>();
            foreach (var layer in roiLayers)
            {
                if (layer == null || layer.Geometry == null || !tool.BoundRoiIds.Contains(layer.RoiId)) continue;
                if (rois.Count >= 256 || (layer.Geometry.PolygonRows != null && layer.Geometry.PolygonRows.Length > 4096) ||
                    (layer.Geometry.PolygonColumns != null && layer.Geometry.PolygonColumns.Length > 4096))
                    throw new InvalidOperationException("ROI定义超过身份预算。");
                rois.Add(new RoiLayerRecipeData { RoiId = layer.RoiId, IsEnabled = layer.IsEnabled, IsMask = layer.IsMask,
                    ImageContextId = layer.ImageContextId, Geometry = ToRecipeRoi(layer.Geometry) });
            }
            return rois;
        }

        private static string ModulePrerequisiteHashS147(object definition, Type type, ref int byteBudget)
        {
            using (var stream = new ModulePrerequisiteStreamS147(byteBudget))
            using (var sha = SHA256.Create())
            {
                // Serialize the actual value fields, without Clone/Normalize,
                // SDK objects, file access, or a partial/truncated digest.
                new DataContractJsonSerializer(type, new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 8192 }).WriteObject(stream, definition);
                byteBudget -= checked((int)stream.Length);
                stream.Position = 0;
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private string ModulePrerequisiteAlignmentSignatureS147(VmToolInstance tool)
        {
            int budget = 65536;
            string image = ModulePrerequisiteSourceSignatureS147(tool.GetInputBinding("Image"), ref budget);
            string pose = ModulePrerequisiteSourceSignatureS147(tool.GetInputBinding("Pose"), ref budget);
            return VmVariationModelS130.HashText(image + "\n" + pose);
        }

        private string ModulePrerequisiteSourceSignatureS147(VmToolInputBindingData binding, ref int byteBudget)
        {
            if (binding == null) throw new InvalidOperationException("正式来源缺失。");
            var text = new StringBuilder();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AppendModulePrerequisiteSourceS147(GetInputSourceTool(binding), text, visited, 0, ref byteBudget);
            if (text.Length > 65536 || binding.SourcePortName == null || binding.SourcePortName.Length > 256)
                throw new InvalidOperationException("来源签名超过预算。");
            return VmVariationModelS130.HashText(binding.SourcePortName + "\n" + text);
        }

        private void AppendModulePrerequisiteSourceS147(VmToolInstance source, StringBuilder text,
            ISet<string> visited, int depth, ref int byteBudget)
        {
            if (source == null || depth > 64 || visited.Count > 64 || source.InputBindings.Count > 64 || source.BoundRoiIds.Count > 256)
                throw new InvalidOperationException("来源依赖缺失或超过纯配置预算。");
            string id = GetCoordinateIdentityBaseS102(source);
            if (id == null || id.Length > 1024 || text.Length > 65536) throw new InvalidOperationException("来源身份超过预算。");
            text.Append(id).Append(':').Append(source.Kind).Append(';');
            if (!visited.Add(id)) return;
            if (source.Kind == VmToolKind.ImageSource)
            {
                var p = source.Parameters;
                if (p == null || (p.ImageSourceModeS114 != null && p.ImageSourceModeS114.Length > 128) ||
                    (p.CameraInterfaceS114 != null && p.CameraInterfaceS114.Length > 1024) ||
                    (p.CameraDeviceS114 != null && p.CameraDeviceS114.Length > 1024)) throw new InvalidOperationException("采集定义未知或超限。");
                text.Append(p.ImageSourceModeS114).Append('|').Append(p.CameraInterfaceS114).Append('|').Append(p.CameraDeviceS114);
                return;
            }
            // These dynamic definitions may normalize mutable parameters when
            // requesting metadata. Do not guess their complete source signature.
            if (source.Kind == VmToolKind.CSharpDllScript || source.Kind == VmToolKind.JsonRead || source.Kind == VmToolKind.JsonWrite ||
                source.Kind == VmToolKind.SubflowCall || source.Kind == VmToolKind.SubflowInput)
                throw new InvalidOperationException("动态来源签名留原运行路径复核。");
            if (source.Parameters == null) throw new InvalidOperationException("来源已应用参数缺失。");
            text.Append(ModulePrerequisiteHashS147(source.Parameters, typeof(VmToolParameterData), ref byteBudget));
            var rois = ModulePrerequisiteRoisS147(source);
            text.Append(ModulePrerequisiteHashS147(rois, typeof(List<RoiLayerRecipeData>), ref byteBudget));
            foreach (var input in ToolMetadata.GetInstanceInputPorts(source).OrderBy(port => port.PortName, StringComparer.Ordinal))
            {
                var binding = source.GetInputBinding(input.PortName);
                if (binding == null) continue;
                if (binding.SourcePortName == null || binding.SourcePortName.Length > 256) throw new InvalidOperationException("来源端口超限。");
                text.Append(input.PortName).Append('=').Append(binding.SourcePortName);
                AppendModulePrerequisiteSourceS147(GetInputSourceTool(binding), text, visited, depth + 1, ref byteBudget);
            }
        }
    }
}
