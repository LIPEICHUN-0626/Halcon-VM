using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public static class RecipeReadinessServiceS134
    {
        private const int MaximumTools = 10000, MaximumResources = 4096;
        private const long MaximumFileBytes = 536870912, MaximumHashBytes = 536870912;

        public static VmRecipeReadinessReportS134 Evaluate(VisionRecipe recipe, string flowId = null)
        {
            var report = new VmRecipeReadinessReportS134 { Name = recipe == null ? "未选择方案" : recipe.Name };
            var checker = new Checker(recipe, report);
            checker.Evaluate(flowId);
            return report;
        }

        private sealed class Checker
        {
            private readonly VisionRecipe recipe;
            private readonly VmRecipeReadinessReportS134 report;
            private readonly HashSet<string> resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private int guardedTools;
            private int enabledTools;
            private long hashedBytes;
            private string flowId, flowName;

            internal Checker(VisionRecipe value, VmRecipeReadinessReportS134 result) { recipe = value; report = result; }

            internal void Evaluate(string selectedFlowId)
            {
                if (recipe == null) { Add(null, "方案", "尚未选择目标方案。", true); return; }
                if (recipe.SchemaVersion > RecipeSchema.CurrentVersion)
                    Add(null, "方案版本", "目标方案版本高于本软件支持版本。", true);
                report.FlowCount = recipe.Flows == null ? 1 : recipe.Flows.Count;
                bool validStructure = GuardProject();
                if (validStructure)
                {
                    try { VmProjectFlowData.ValidateProject(recipe); }
                    catch (Exception ex) { Add(null, "方案结构", ex.Message, true); validStructure = false; }
                }
                if (!validStructure) return; // Never clone or inspect metadata from an unbounded/cyclic tree.

                if (recipe.Flows != null)
                {
                    string requested = string.IsNullOrWhiteSpace(selectedFlowId) ? recipe.ActiveFlowId : selectedFlowId;
                    var selected = recipe.Flows.FirstOrDefault(item => string.Equals(item.FlowId, requested, StringComparison.OrdinalIgnoreCase));
                    if (selected == null) { Add(null, "目标流程", "请选择目标方案中真实存在的流程；不静默改选第一条流程。", true); return; }
                    flowId = selected.FlowId; flowName = selected.Name;
                    CheckScope(selected.Tools, null, true, 0);
                }
                else
                {
                    flowId = "Legacy"; flowName = "旧单流程";
                    if (recipe.ToolFlow == null)
                    {
                        // The application's existing legacy loader owns migration; do not invent modern bindings here.
                        enabledTools = new[] { recipe.EnableShapeMatch, recipe.EnableBlob, recipe.EnableGrayStat, recipe.EnableEdgeMeasure, recipe.EnableHDevelop }.Count(value => value);
                        report.ToolCount = enabledTools;
                        if (recipe.EnableShapeMatch) FileResource(null, "旧 Shape 模板", recipe.TemplatePath, null, true, false);
                        if (recipe.EnableHDevelop) FileResource(null, "旧 HDevelop 程序", recipe.HDevelopPath, null, true, false);
                        Add(null, "旧方案迁移", "ToolFlow 字段缺失，加载时按原历史开关迁移；本预览不创建模块或猜测输入订阅，迁移后须明确检查并运行。", false);
                    }
                    else CheckScope(recipe.ToolFlow, null, true, 0);
                }
                report.ResourceCount = resources.Count;
                if (enabledTools == 0) Add(null, "生产流程", "目标没有已启用的可执行模块；普通方案打开仍可维修，显式生产换型不能把空白或全停用流程当可检测配方。", true);
                Add(null, "运行边界", "静态报告只检查定义、文件与身份。上游本次结果、动态条件分支、图像尺寸/坐标/有效域、算法句柄可加载性、设备连接和现场时序均待显式运行确认；不会自动采集、检测或外发。", false);
            }

            private bool GuardProject()
            {
                if (recipe.Flows != null && (recipe.Flows.Count == 0 || recipe.Flows.Count > 1024))
                { Add(null, "方案结构", "方案流程集合为空或超过本次有界静态检查的 1024 条流程上限。", true); return false; }
                bool valid = true;
                if (recipe.Flows == null)
                    return recipe.ToolFlow == null || GuardScope(recipe.ToolFlow, new HashSet<VmSubflowData>(), 0);
                foreach (var flow in recipe.Flows)
                {
                    if (flow == null || flow.Tools == null) { Add(null, "方案结构", "流程或模块集合缺失。", true); valid = false; continue; }
                    flowId = flow.FlowId; flowName = flow.Name;
                    valid = GuardScope(flow.Tools, new HashSet<VmSubflowData>(), 0) && valid;
                }
                return valid;
            }

            private bool GuardScope(IList<ToolFlowRecipeItem> tools, HashSet<VmSubflowData> active, int depth)
            {
                if (tools == null || depth > 8 || tools.Count > MaximumTools)
                { Add(null, "方案结构", "模块集合缺失、嵌套超过 8 层或单范围超过 10000 模块。", true); return false; }
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool valid = true;
                foreach (var item in tools)
                {
                    if (++guardedTools > MaximumTools) { Add(null, "方案结构", "方案超过本次有界静态检查的 10000 模块总上限。", true); return false; }
                    VmToolKind kind;
                    if (item == null || string.IsNullOrWhiteSpace(item.ToolId) || !ids.Add(item.ToolId) || item.ToolId == VmSubflowData.InputProxyId ||
                        !Enum.TryParse(item.ToolType, out kind) || !Enum.IsDefined(typeof(VmToolKind), kind) || kind == VmToolKind.SubflowInput)
                    { Add(item, "方案结构", "模块为空、类型无效或本范围标识重复/非法；停用模块也不能破坏方案结构。", true); valid = false; continue; }
                    if (kind != VmToolKind.SubflowCall) continue;
                    var child = item.Parameters == null ? null : item.Parameters.Subflow;
                    if (child == null || !active.Add(child)) { Add(item, "方案结构", "子流程定义缺失或存在循环引用。", true); valid = false; continue; }
                    if ((child.Inputs ?? new List<VmSubflowPortData>()).Any(port => port == null) ||
                        (child.Outputs ?? new List<VmSubflowPortData>()).Any(port => port == null))
                    { Add(item, "方案结构", "子流程端口集合包含 null，既有深复制无法安全加载该定义；停用也不能掩盖。", true); valid = false; }
                    valid = GuardScope(child.Tools, active, depth + 1) && valid;
                    active.Remove(child);
                }
                return valid;
            }

            private void CheckScope(IList<ToolFlowRecipeItem> tools, VmSubflowData boundary, bool active, int depth)
            {
                if (depth > 8 || tools == null) return;
                var instances = new Dictionary<string, VmToolInstance>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in tools)
                {
                    report.ToolCount++;
                    VmToolKind kind = (VmToolKind)Enum.Parse(typeof(VmToolKind), item.ToolType);
                    try
                    {
                        var parameters = item.Parameters == null ? new VmToolParameterData() : item.Parameters.Clone();
                        if (kind == VmToolKind.OcrRecognition && string.IsNullOrWhiteSpace(parameters.OcrModelPath) &&
                            (parameters.OcrValidationS133 == null || string.IsNullOrWhiteSpace(parameters.OcrValidationS133.ModelPath)))
                        {
                            string halconRoot = Environment.GetEnvironmentVariable("HALCONROOT");
                            if (string.IsNullOrWhiteSpace(halconRoot)) halconRoot = @"C:\Program Files\MVTec\HALCON-20.11-Progress";
                            parameters.OcrModelPath = Path.Combine(halconRoot, "ocr", "Industrial_NoRej.omc");
                        }
                        instances.Add(item.ToolId, new VmToolInstance { ToolId = item.ToolId, Kind = kind,
                            InstanceName = item.InstanceName, IsEnabled = item.IsEnabled,
                            Parameters = parameters });
                    }
                    catch (Exception ex) { Add(item, "参数定义", "无法生成独立只读参数副本：" + ex.Message, active && item.IsEnabled); }
                }
                foreach (var item in tools)
                {
                    VmToolInstance tool;
                    if (!instances.TryGetValue(item.ToolId, out tool)) continue;
                    bool enabled = active && item.IsEnabled;
                    if (enabled)
                    {
                        enabledTools++;
                        try
                        {
                            string error = tool.Kind == VmToolKind.PlanarCalibration2D ? ValidatePlanar(tool.Parameters) : tool.Parameters.Validate(tool.Kind);
                            if (!string.IsNullOrWhiteSpace(error)) Add(item, "参数", error, true);
                            CheckBindings(item, tool, tools, instances, boundary);
                            CheckReferences(item, tool, tools, instances);
                            CheckResources(item, tool);
                        }
                        catch (Exception ex) { Add(item, "静态准备", ex.Message, true); }
                    }
                    if (tool.Kind == VmToolKind.SubflowCall && tool.Parameters.Subflow != null)
                    {
                        var child = tool.Parameters.Subflow;
                        if (enabled)
                        {
                            string error = child.Validate(depth);
                            if (!string.IsNullOrWhiteSpace(error)) Add(item, "子流程契约", error, true);
                            CheckReturns(item, child, enabled);
                        }
                        CheckScope(child.Tools, child, enabled, depth + 1);
                    }
                }
                if (active && tools.Count == 0) Add(null, "空白流程", "目标流程为空，没有可开始检测的模块；可换入空方案，但不能把空白当生产准备完成。", false);
            }

            private void CheckBindings(ToolFlowRecipeItem item, VmToolInstance tool, IList<ToolFlowRecipeItem> scope,
                IDictionary<string, VmToolInstance> instances, VmSubflowData boundary)
            {
                var ports = ToolMetadata.GetInstanceInputPorts(tool);
                var bindings = (item.InputBindings ?? new List<VmToolInputBindingData>()).Select(b => b == null ? null : b.Clone()).ToList();
                if (tool.Kind == VmToolKind.NumericJudge && !bindings.Any(b => b != null && b.TargetPortName == "Value") && item.NumericJudge != null &&
                    !string.IsNullOrWhiteSpace(item.NumericJudge.InputToolId))
                    bindings.Add(new VmToolInputBindingData { TargetPortName = "Value", SourceToolId = item.NumericJudge.InputToolId, SourcePortName = item.NumericJudge.InputPortName });
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var binding in bindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.TargetPortName) || !names.Add(binding.TargetPortName))
                    { Add(item, "输入订阅", "输入定义为空或同一目标端口重复订阅。", true); continue; }
                    var port = ports.FirstOrDefault(p => string.Equals(p.PortName, binding.TargetPortName, StringComparison.OrdinalIgnoreCase));
                    if (port == null) { Add(item, "输入订阅", "目标端口不存在或当前模式不支持：" + binding.TargetPortName, true); continue; }
                    VmPortDefinition sourcePort = null;
                    if (binding.SourceToolId == VmSubflowData.InputProxyId && boundary != null)
                    {
                        var proxy = VmSubflowLoopS111.ProxyPorts(boundary).FirstOrDefault(p => string.Equals(p.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                        if (proxy != null) sourcePort = new VmPortDefinition { PortName = proxy.PortName, DataType = proxy.DataType };
                    }
                    else
                    {
                        VmToolInstance source;
                        if (!instances.TryGetValue(binding.SourceToolId ?? string.Empty, out source))
                        { Add(item, "输入订阅", "来源模块不在本流程范围：" + binding.SourceToolId, true); continue; }
                        int sourceIndex = scope.ToList().FindIndex(t => string.Equals(t.ToolId, source.ToolId, StringComparison.OrdinalIgnoreCase));
                        if (sourceIndex >= scope.IndexOf(item) || !source.IsEnabled)
                        { Add(item, "输入订阅", "来源必须为本范围已启用的前序模块：" + source.InstanceName, true); continue; }
                        sourcePort = ToolMetadata.GetInstanceOutputPorts(source).FirstOrDefault(p => string.Equals(p.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                        if (source.Kind == VmToolKind.GlobalVariableRead && !IsVariablePortActive(source, binding.SourcePortName))
                        { Add(item, "变量订阅", "来源变量的实际类型不提供该输出：" + binding.SourcePortName, true); continue; }
                    }
                    if (sourcePort == null) Add(item, "输入订阅", "来源输出端口不存在：" + binding.SourcePortName, true);
                    else if (!string.Equals(port.DataType, sourcePort.DataType, StringComparison.OrdinalIgnoreCase))
                        Add(item, "输入类型", sourcePort.DataType + " 不能订阅到 " + binding.TargetPortName + "：" + port.DataType, true);
                    else if (port.DataType == "Region" && ToolMetadata.SupportsRoi(tool.Kind) &&
                        string.Equals(port.PortName, tool.Kind == VmToolKind.ShapeMatch ? "SearchROI" : "ROI", StringComparison.OrdinalIgnoreCase) &&
                        tool.Parameters.RoiExecutionMode == VmRoiExecutionMode.PerRoi)
                        Add(item, "ROI 订阅", "逐 ROI 模式要求本地图层和稳定 RoiId，不能订阅前序 Region。", true);
                }
                foreach (var port in ports.Where(p => !p.IsOptional && !names.Contains(p.PortName)))
                {
                    bool localDefault = port.PortName == "ROI" || port.PortName == "SearchROI" || port.PortName == "ShapeModel" || port.PortName == "Program" ||
                        port.PortName == "Image" && IsLegacyImage(tool.Kind);
                    Add(item, "输入准备", port.DisplayName + (localDefault ? "使用既有兼容本地/系统来源，当前图像或句柄未运行确认。" : "缺少正式前序输入订阅。"), !localDefault);
                }
                if (bindings.Any(b => b != null && b.TargetPortName == "$RunCondition"))
                    Add(item, "动态条件", "运行条件为本次上游布尔值；静态预览不选择分支，也不保证本模块本次一定执行。", false);
                if (tool.Kind == VmToolKind.PositionCorrection2D)
                {
                    bool pose = names.Contains("Pose2D"); int legacy = new[] { "CurrentRow", "CurrentColumn", "CurrentAngle" }.Count(names.Contains);
                    if (pose && legacy > 0 || !pose && legacy != 3) Add(item, "位置修正", "须连接 Pose2D 或完整旧 Row/Column/Angle，不能混用或缺失。", true);
                }
                CheckDistinct(item, tool.Kind, bindings);
            }

            private void CheckDistinct(ToolFlowRecipeItem item, VmToolKind kind, IList<VmToolInputBindingData> bindings)
            {
                string[] pair = kind == VmToolKind.RegionSetOperation ? new[] { "RegionA", "RegionB" } :
                    kind == VmToolKind.ImageDifference ? new[] { "ImageA", "ImageB" } :
                    kind == VmToolKind.DynamicThreshold ? new[] { "OriginalImage", "ReferenceImage" } : null;
                if (pair == null) return;
                var a = bindings.FirstOrDefault(value => value != null && value.TargetPortName == pair[0]);
                var b = bindings.FirstOrDefault(v => v != null && v.TargetPortName == pair[1]);
                if (a != null && b != null && a.SourceToolId == b.SourceToolId && a.SourcePortName == b.SourcePortName)
                    Add(item, "双输入", pair[0] + " / " + pair[1] + " 必须订阅两路不同的前序来源。", true);
            }

            private void CheckReturns(ToolFlowRecipeItem item, VmSubflowData child, bool active)
            {
                foreach (var port in child.Outputs ?? new List<VmSubflowPortData>())
                {
                    if (port == null) continue;
                    var source = child.Tools.FirstOrDefault(t => t.ToolId == port.SourceToolId);
                    if (source == null || !source.IsEnabled) { Add(item, "子流程返回", "返回 " + port.PortName + " 的来源不存在或已停用。", active); continue; }
                    VmToolKind kind;
                    if (!Enum.TryParse(source.ToolType, out kind)) continue;
                    var metadata = ToolMetadata.GetInstanceOutputPorts(new VmToolInstance { Kind = kind,
                        Parameters = source.Parameters == null ? new VmToolParameterData() : source.Parameters.Clone() });
                    var result = metadata.FirstOrDefault(p => p.PortName == port.SourcePortName);
                    if (result == null || !string.Equals(result.DataType, port.DataType, StringComparison.OrdinalIgnoreCase))
                        Add(item, "子流程返回", "返回 " + port.PortName + " 的真实来源端口不存在或类型不一致。", active);
                }
            }

            private void CheckReferences(ToolFlowRecipeItem item, VmToolInstance tool, IList<ToolFlowRecipeItem> scope, IDictionary<string, VmToolInstance> instances)
            {
                var p = tool.Parameters;
                if (tool.Kind == VmToolKind.NumericJudge && item.NumericJudge != null)
                {
                    var n = item.NumericJudge;
                    string error = VmNumericJudgeParameterValidator.Validate(n.Operator, n.LowerLimit, n.UpperLimit, n.Tolerance);
                    if (!string.IsNullOrWhiteSpace(error)) Add(item, "数值判定", error, true);
                }
                if (ToolMetadata.SupportsRoi(tool.Kind))
                {
                    foreach (string id in item.RoiIds ?? new List<string>())
                        if (!(recipe.RoiLayers ?? new List<RoiLayerRecipeData>()).Any(r => r != null && r.RoiId == id))
                            Add(item, "ROI 引用", "绑定的 ROI 不存在：" + id, true);
                    if (!string.IsNullOrWhiteSpace(p.PositionCorrectionToolId))
                    {
                        VmToolInstance source;
                        if (!instances.TryGetValue(p.PositionCorrectionToolId, out source) || !source.IsEnabled || source.Kind != VmToolKind.PositionCorrection2D ||
                            scope.ToList().FindIndex(t => t.ToolId == source.ToolId) >= scope.IndexOf(item))
                            Add(item, "位置修正引用", "ROI 位置修正来源必须是本范围已启用的前序 2D 位置修正工具。", true);
                    }
                }
                if (tool.Kind == VmToolKind.GlobalVariableRead || tool.Kind == VmToolKind.GlobalVariableWrite)
                {
                    var matches = (recipe.GlobalVariables ?? new List<VmGlobalVariableData>()).Where(v => v != null && v.VariableId == p.GlobalVariableId).ToList();
                    if (matches.Count != 1 || !VmGlobalVariableType.IsSupported(matches[0].ValueType)) Add(item, "变量引用", "所选变量不存在、标识重复或类型无效。", true);
                    else if (VmArrayContractS105.IsType(matches[0].ValueType))
                    {
                        try { matches[0].CloneNormalized(); }
                        catch (Exception ex) { Add(item, "变量默认值", ex.Message, true); }
                    }
                }
            }

            private bool IsVariablePortActive(VmToolInstance source, string port)
            {
                var variable = (recipe.GlobalVariables ?? new List<VmGlobalVariableData>()).FirstOrDefault(v => v != null && v.VariableId == source.Parameters.GlobalVariableId);
                string[] valuePorts = { "NumberValue", "TextValue", "BooleanValue", "ResultCodeValue", "NumberArrayValue", "StringArrayValue" };
                return !valuePorts.Contains(port) || variable != null && VmGlobalVariableType.GetWritePortName(variable.ValueType) == port;
            }

            private void CheckResources(ToolFlowRecipeItem item, VmToolInstance tool)
            {
                var p = tool.Parameters;
                switch (tool.Kind)
                {
                    case VmToolKind.ImageSource:
                        string mode = VmImageSourceModeS114.Normalize(p.ImageSourceModeS114);
                        if (mode == VmImageSourceModeS114.File) FileResource(item, "本地图像", p.LocalImagePath, null, true, false);
                        else Add(item, "采集准备", mode == VmImageSourceModeS114.Camera ? "相机接口、设备连接及触发取图均未连接确认。" : "回放目录配置已按原规则检查；具体帧可读性和缺图时序待请求时确认。", false);
                        break;
                    case VmToolKind.ShapeMatch:
                        FileResource(item, "Shape 模板", string.IsNullOrWhiteSpace(p.ShapeModelPath) ? recipe.TemplatePath : p.ShapeModelPath, null, true, false);
                        break;
                    case VmToolKind.NccTemplateMatch:
                        var ncc = p.NccMatchingS131;
                        if (ncc == null || !ncc.HasModel) Add(item, "NCC 模板", "尚未应用具有真实训练元数据与 SHA 身份的 NCC 模板。", true);
                        else FileResource(item, "NCC 模板", ncc.ModelPath, ncc.ModelSha256, true, true);
                        break;
                    case VmToolKind.VariationSurfaceInspection:
                        var variation = p.VariationInspectionS130;
                        if (variation == null || !variation.HasModelVersion()) { Add(item, "良品模型", "模型/共同域缺失或样本集版本已变化，须重新学习并应用。", true); break; }
                        FileResource(item, "良品变化模型", variation.ModelPath, variation.ModelSha256, true, true);
                        FileResource(item, "训练共同有效域", variation.CoveragePath, variation.CoverageSha256, true, true);
                        foreach (var sample in variation.Samples ?? new List<VmVariationSampleS130>())
                            if (sample != null) { FileResource(item, "良品样本", sample.ImagePath, sample.ImageSha256, true, true); FileResource(item, "良品样本有效域", sample.DomainPath, sample.DomainSha256, true, true); }
                        break;
                    case VmToolKind.OcrRecognition:
                        var ocr = p.OcrValidationS133;
                        string model = p.OcrModelPath;
                        if (string.IsNullOrWhiteSpace(model))
                        { string root = Environment.GetEnvironmentVariable("HALCONROOT"); if (string.IsNullOrWhiteSpace(root)) root = @"C:\Program Files\MVTec\HALCON-20.11-Progress"; model = Path.Combine(root, "ocr", "Industrial_NoRej.omc"); }
                        FileResource(item, "OCR MLP 字库", model, ocr == null ? null : ocr.ModelSha256, true, ocr != null && !string.IsNullOrWhiteSpace(ocr.ModelPath));
                        if (ocr == null || string.IsNullOrWhiteSpace(ocr.ModelPath)) Add(item, "旧 OCR 字库", "旧字库没有保存的 SHA 身份；保留既有路径行为，建议从原入口导入受管版本。静态存在不证明 MLP 可加载。", false);
                        break;
                    case VmToolKind.CSharpDllScript:
                        if (VmScriptExecutionMode.Normalize(p.ScriptExecutionMode) == VmScriptExecutionMode.Dll)
                        {
                            if (string.IsNullOrWhiteSpace(p.ScriptResourceId)) FileResource(item, "兼容脚本 DLL", p.ScriptAssemblyPath, null, true, false);
                            else
                            {
                                var matches = (recipe.ExternalResources ?? new List<VmExternalResourceData>()).Where(r => r != null && r.ResourceId == p.ScriptResourceId).ToList();
                                if (matches.Count != 1) { Add(item, "脚本资源", "受管资源不存在或标识重复。", true); break; }
                                string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources"));
                                string file = Path.GetFullPath(Path.Combine(root, matches[0].ManagedRelativePath ?? ""));
                                if (!file.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                                    Add(item, "脚本资源", "受管 DLL 路径越出本机资源目录。", true);
                                else FileResource(item, "受管脚本 DLL", file, matches[0].Sha256, true, true);
                            }
                        }
                        Add(item, "脚本执行", "未编译代码、加载 DLL 或解析入口/依赖；AppDomain 不属于安全沙箱，仅运行受信任扩展。", false);
                        break;
                    case VmToolKind.HDevelop:
                        FileResource(item, "HDevelop 程序", recipe.HDevelopPath, null, true, false);
                        break;
                    case VmToolKind.CameraDistortionCorrection:
                        Add(item, "相机校正", "运行使用方案内数值内参，不要求导入时的 .dat 原文件仍存在；有效域、精度、RMS 与实际映射须运行确认。", false);
                        break;
                    case VmToolKind.PlanarCalibration2D:
                        Add(item, "平面标定", "只检查对应点与单位；未调用 HALCON 拟合，不把静态点表当现场精度认证。", false);
                        break;
                    case VmToolKind.PoseImageAlignment:
                        if (p.PoseImageAlignmentS132 == null || !p.PoseImageAlignmentS132.HasReference) Add(item, "对齐参考", "尚未显式获取并应用固定参考位姿。", true);
                        break;
                    case VmToolKind.TcpClientExchange:
                    case VmToolKind.ModbusTcpExchange:
                    case VmToolKind.ModbusRtuExchange:
                        if (p.CommunicationSimulationS121) Add(item, "生产通讯", "目标流程含离线模拟通讯；不能用模拟响应冒充现场设备，进入生产前须明确切为真实设备模式。", true);
                        else Add(item, "设备通讯", "主机/串口参数不证明设备在线，握手、超时和写入返回未实测；预览不连接或外发。", false);
                        break;
                }
            }

            private void FileResource(ToolFlowRecipeItem tool, string category, string path, string hash, bool blocking, bool requireHash)
            {
                if (string.IsNullOrWhiteSpace(path)) { Add(tool, category, "资源路径缺失。", blocking); return; }
                try
                {
                    string full = Path.GetFullPath(path);
                    resources.Add(full); report.ResourceCount = resources.Count;
                    if (resources.Count > MaximumResources) throw new InvalidOperationException("本次准备资源超过 4096 文件上限，未继续读取。");
                    using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (stream.Length < 1 || stream.Length > MaximumFileBytes) throw new InvalidDataException("资源为空或超过 512 MiB。");
                        if (requireHash && !IsHash(hash)) throw new InvalidDataException("受管资源 SHA 身份缺失或无效。");
                        if (!string.IsNullOrWhiteSpace(hash))
                        {
                            if (!IsHash(hash)) throw new InvalidDataException("SHA 身份格式无效。");
                            if (hashedBytes + stream.Length > MaximumHashBytes) throw new InvalidDataException("本次 SHA 读取超过 512 MiB 总预算，资源身份尚未确认；准备报告不能放行未确认资源。");
                            hashedBytes += stream.Length;
                            string actual;
                            // Each referenced version is checked under its own live read lease, not a stale path cache.
                            using (var sha = SHA256.Create()) actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                            if (!string.Equals(actual, hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("文件内容与保存的 SHA 不一致，拒绝静默换资源。");
                        }
                    }
                }
                catch (Exception ex) { Add(tool, category, ex.Message + " · " + path, blocking); }
            }

            private static string ValidatePlanar(VmToolParameterData p)
            {
                if (string.IsNullOrWhiteSpace(p.PlanarCalibrationUnitS124) || p.PlanarCalibrationUnitS124.Length > 16) return "标定单位必须为 1–16 个字符。";
                var samples = p.PlanarCalibrationSamplesS124;
                if (samples == null || samples.Count < 3 || samples.Count > 64) return "平面标定需要 3–64 个像素/世界对应点。";
                string context = samples[0] == null ? null : samples[0].ImageContextId;
                if (string.IsNullOrWhiteSpace(context) || context.StartsWith("Pending:", StringComparison.Ordinal)) return "标定对应点的图像链身份未确定。";
                if (samples.Any(s => s == null || !Finite(s.PixelRow) || !Finite(s.PixelColumn) || !Finite(s.WorldX) || !Finite(s.WorldY) || !string.Equals(s.ImageContextId, context, StringComparison.OrdinalIgnoreCase)))
                    return "标定点包含非法数值或跨图像链身份。";
                for (int i = 0; i < samples.Count; i++) for (int j = i + 1; j < samples.Count; j++)
                    if (Math.Abs(samples[i].PixelRow - samples[j].PixelRow) + Math.Abs(samples[i].PixelColumn - samples[j].PixelColumn) < 1e-3 ||
                        Math.Abs(samples[i].WorldX - samples[j].WorldX) + Math.Abs(samples[i].WorldY - samples[j].WorldY) < 1e-9) return "标定存在重复的像素或世界对应点。";
                return null; // Non-collinearity, fitting and residuals remain explicitly unverified runtime work.
            }

            private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
            private static bool IsHash(string hash) { return hash != null && hash.Length == 64 && hash.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'); }
            private static bool IsLegacyImage(VmToolKind kind) { return kind == VmToolKind.ShapeMatch || kind == VmToolKind.Blob || kind == VmToolKind.GrayStat || kind == VmToolKind.EdgeMeasure || kind == VmToolKind.HDevelop; }
            private void Add(ToolFlowRecipeItem tool, string category, string reason, bool blocking)
            {
                report.Issues.Add(new VmRecipeReadinessIssueS134 { Category = category, Reason = reason, FlowId = flowId,
                    FlowName = flowName ?? "方案", ToolId = tool == null ? null : tool.ToolId,
                    ToolName = tool == null ? null : tool.InstanceName, IsBlocking = blocking });
            }
        }
    }
}
