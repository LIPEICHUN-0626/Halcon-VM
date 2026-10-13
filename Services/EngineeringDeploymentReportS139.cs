using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    // A frozen, descriptive preparation report. No DLL loading, device probe, or installation.
    public static class EngineeringDeploymentReportS139
    {
        public const int MaximumReportCharacters = 65536;
        public const int MaximumEntries = 2048;
        private const int MaximumDepth = 8;

        public static string BuildReport(VisionRecipe recipe, IReadOnlyList<VmEngineeringPackageEntryS139> entries)
        {
            var writer = new BoundedReport();
            writer.Line("工程迁移 · 目标机器准备报告");
            writer.Line("冻结时间 UTC：" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            writer.Line("状态：部分实现（待集中验证）。本报告不等于部署完成、算法可用或现场认证。");
            writer.Line("本次只读工程声明及当前进程信息；未运行脚本、加载外部 DLL、探测许可证、连接设备或修改系统。");
            writer.Line("不随行工位编辑保护策略、密码、旧运行句柄、生产记录或设备连接状态。");
            writer.Line();
            writer.Line("一、当前进程事实（不是目标机器验证）");
            writer.Line("进程位数：" + (Environment.Is64BitProcess ? "64 位" : "32 位") + "；操作系统位数：" + (Environment.Is64BitOperatingSystem ? "64 位" : "32 位"));
            writer.Line("当前 CLR：" + Environment.Version + "；应用构建目标：.NET Framework 4.8（目标机器安装/兼容性未验证）。");
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var halcon = assemblies.Where(item => string.Equals(item.GetName().Name, "halcondotnet", StringComparison.OrdinalIgnoreCase)).Take(16).ToList();
            if (halcon.Count == 0) writer.Line("HALCON 托管程序集：当前进程未观察到已加载 halcondotnet；不据此判断是否安装。");
            foreach (Assembly assembly in halcon) writer.Line("已加载 HALCON 托管 identity：" + Safe(assembly.FullName, 2048));
            writer.Line("HALCON 原生运行库、位数/版本配套、HDevEngine 与目标许可：未探测；目标机器须合法安装并核实。托管程序集已加载不证明原生算子或许可可用。");
            writer.Line("OpenCV：当前内置工具代码未声明 OpenCV 调用/依赖；本报告不猜测安装。自定义代码/DLL 的未声明 OpenCV 或其他依赖仍未知。");
            writer.Line();
            writer.Line("二、全工程声明与设备准备");
            if (recipe == null)
            {
                writer.Line("阻断：工程定义缺失，不能据此生成完整依赖清单。");
            }
            else
            {
                writer.Line("工程：" + Safe(recipe.Name, 256) + "；Schema：" + recipe.SchemaVersion.ToString(CultureInfo.InvariantCulture));
                writer.Line("方案 TCP 声明：" + Safe(recipe.TcpMode, 64) + " " + Safe(recipe.TcpIp, 256) + ":" + recipe.TcpPort.ToString(CultureInfo.InvariantCulture) + "，编码 " + Safe(recipe.TcpEncoding, 64) + "；自动回传声明 " + (recipe.AutoSendResult ? "开启" : "关闭") + "。未连接/外发。");
                if (recipe.TcpFlowTrigger != null)
                {
                    var trigger = recipe.TcpFlowTrigger;
                    writer.Line("触发目标 FlowId：" + Safe(trigger.FlowId, 256) + "；回传模式：" + Safe(trigger.ReturnModeS123, 128) + "。触发启用态不随行，不会自动恢复连接/写入。");
                    writer.Line("回传目标声明：网络 " + Safe(trigger.ReturnHostS123, 256) + ":" + trigger.ReturnPortS123.ToString(CultureInfo.InvariantCulture) + "；串口 " + Safe(trigger.ReturnComPortS123, 64) + "/" + trigger.ReturnBaudS123.ToString(CultureInfo.InvariantCulture) + "/" + Safe(trigger.ReturnParityS123, 32) + "/" + trigger.ReturnStopBitsS123.ToString(CultureInfo.InvariantCulture) + "。只有配置，不证明链路可用。");
                }
                var traversal = new ToolTraversal(writer, recipe);
                if (recipe.Flows == null) traversal.Scope(recipe.ToolFlow, "旧单流程", 0);
                else
                {
                    int rootCount = 0;
                    foreach (var flow in recipe.Flows)
                    {
                        if (++rootCount > MaximumEntries) { writer.MarkTruncated("根流程超过 2048 项，后续未枚举"); break; }
                        if (flow == null) { writer.Line("阻断：根流程定义为 null。"); continue; }
                        traversal.Scope(flow.Tools, "流程 " + Safe(flow.Name, 128) + " [" + Safe(flow.FlowId, 128) + "]", 0);
                    }
                }
                writer.Line("已枚举模块：" + traversal.Count.ToString(CultureInfo.InvariantCulture) + "（包含禁用模块和嵌套子流程；配置不是设备验证）。");
                if (recipe.EnableHDevelop && !traversal.HasHDevelop)
                    WriteHDevelop(writer, "旧配方 HDevelop", recipe);
                int resourceCount = 0;
                foreach (var resource in recipe.ExternalResources ?? new List<VmExternalResourceData>())
                {
                    if (++resourceCount > MaximumEntries) { writer.MarkTruncated("外部资源声明超过 2048 项，后续未枚举"); break; }
                    if (resource == null) { writer.Line("阻断：外部资源声明为 null。"); continue; }
                    writer.Line("资源库 DLL：" + Safe(resource.Name, 128) + "，ResourceId=" + Safe(resource.ResourceId, 128) + "，声明版本=" + Safe(resource.AssemblyVersion, 128) + "，SHA=" + Safe(resource.Sha256, 128) + "。声明元数据未证明程序集可加载或入口有效。");
                }
            }
            writer.Line();
            writer.Line("三、当时随行清单（与本次冻结导出计划对应）");
            if (entries == null) writer.Line("阻断：随行清单缺失。");
            else
            {
                int count = Math.Min(entries.Count, MaximumEntries);
                for (int i = 0; i < count && !writer.Full; i++)
                {
                    var entry = entries[i];
                    if (entry == null) { writer.Line("阻断：清单包含 null 条目。"); continue; }
                    writer.Line("[" + Safe(entry.State, 32) + "] " + Safe(entry.Kind, 128) + " · " + Safe(entry.Label, 256));
                    writer.Line("  来源=" + Safe(entry.SourcePath, 1024) + "；字节=" + entry.Bytes.ToString(CultureInfo.InvariantCulture) + "；当时SHA=" + Safe(entry.Sha256, 128) + "；预期SHA=" + Safe(entry.ExpectedSha256, 128));
                    writer.Line("  " + Safe(entry.Detail, 1024));
                }
                if (entries.Count > MaximumEntries) writer.MarkTruncated("资源清单超过 2048 项，后续未列出");
            }
            writer.Line();
            writer.Line("四、目标机器人工准备与明确未知项");
            writer.Line("1. 安装并匹配合法 HALCON/.NET 运行环境、位数、原生库和许可；本功能不下载/安装、不再分发授权或供应商 SDK。");
            writer.Line("2. DLL 只随行明确声明的本体；托管引用、原生 DLL、动态加载、配置/数据文件等间接依赖未解析。来源必须受信任；SHA 是内容身份，不是签名、恶意代码检测或安全认证。");
            writer.Line("3. 内联 C# 源定义已在工程 JSON 中，不另造源文件；编译环境/引用、代码访问的外部文件和本机 API 未验证。AppDomain 不是安全沙箱。");
            writer.Line("4. HDevelop 本体可以随行，include、过程搜索目录、外部模型和原生扩展依赖未解析，须人工补齐；复制本体不等于完整脚本部署。");
            writer.Line("5. HALCON 相机接口插件、厂商 SDK/驱动、相机设备/触发/网络带宽与权限须目标现场准备；接口/设备配置存在不证明可采集。");
            writer.Line("6. 回放目录是运行环境声明，不递归打包目录图片集合。校正运行使用方案内数值内参，求参板文件/采样是否随行以冻结清单为准；精度仍须目标现场验证。");
            writer.Line("7. 配置目标网卡/IP/防火墙/协议端点与串口驱动/COM映射/线路；通讯参数和离线模拟不证明设备在线。本功能不连接、不外发、不重放写入。");
            writer.Line("8. 移动整个工程 JSON 及随行资源目录，不单移 JSON。按照报告补齐未知依赖，再执行后续集中运行/异常/部署验证；当前机器事实不能证明目标机器就绪。");
            return writer.Finish();
        }

        private static void WriteHDevelop(BoundedReport writer, string scope, VisionRecipe recipe)
        {
            writer.Line(scope + "：HDevelop 本体=" + Safe(recipe.HDevelopPath, 1024) + "，过程=" + Safe(recipe.ProcedureName, 256) + "；include/外部过程/原生依赖未知，未执行。");
        }

        private sealed class ToolTraversal
        {
            private readonly BoundedReport writer;
            private readonly VisionRecipe recipe;
            private readonly HashSet<List<ToolFlowRecipeItem>> visited = new HashSet<List<ToolFlowRecipeItem>>();
            public int Count { get; private set; }
            public bool HasHDevelop { get; private set; }
            public ToolTraversal(BoundedReport writer, VisionRecipe recipe) { this.writer = writer; this.recipe = recipe; }

            public void Scope(List<ToolFlowRecipeItem> tools, string scope, int depth)
            {
                if (tools == null) { writer.Line("阻断：" + scope + " 的模块集合缺失。"); return; }
                if (depth > MaximumDepth) { writer.MarkTruncated("子流程超过 8 层，后续未枚举"); return; }
                if (!visited.Add(tools)) { writer.Line("阻断：重复/循环引用模块集合，未重复枚举。"); return; }
                foreach (var tool in tools)
                {
                    if (writer.Full) return;
                    if (Count >= MaximumEntries) { writer.MarkTruncated("全工程模块超过 2048 项，后续未枚举"); return; }
                    Count++;
                    if (tool == null) { writer.Line("阻断：" + scope + " 包含 null 模块。"); continue; }
                    VmToolKind kind;
                    if (!Enum.TryParse(tool.ToolType, out kind) || !Enum.IsDefined(typeof(VmToolKind), kind))
                    { writer.Line("未知模块类型：" + Safe(tool.ToolType, 128) + "；其依赖未解析。"); continue; }
                    string label = scope + " / " + Safe(tool.InstanceName, 128) + " [" + Safe(tool.ToolId, 128) + "]" + (tool.IsEnabled ? string.Empty : "（禁用）");
                    var p = tool.Parameters;
                    if (kind == VmToolKind.HDevelop)
                    {
                        HasHDevelop = true;
                        WriteHDevelop(writer, label, recipe);
                    }
                    if (p == null)
                    { writer.Line(label + "：参数定义缺失，依赖未能确认。"); continue; }
                    switch (kind)
                    {
                        case VmToolKind.CSharpDllScript:
                            if (string.Equals(p.ScriptExecutionMode, VmScriptExecutionMode.Dll, StringComparison.OrdinalIgnoreCase))
                                writer.Line(label + "：DLL声明路径=" + Safe(p.ScriptAssemblyPath, 1024) + "，ResourceId=" + Safe(p.ScriptResourceId, 128) + "，入口=" + Safe(p.ScriptTypeName, 256) + "." + Safe(p.ScriptMethodName, 128) + "；仅声明本体，托管/原生间接依赖未知，未加载/执行。");
                            else
                                writer.Line(label + "：内联 C# 源已在 JSON；编译与额外程序集/原生/文件依赖未知，未输出代码/参数值、未执行。");
                            break;
                        case VmToolKind.ImageSource:
                            if (p.ImageSourceModeS114 == VmImageSourceModeS114.Camera)
                                writer.Line(label + "：HALCON 相机接口=" + Safe(p.CameraInterfaceS114, 128) + "，设备=" + Safe(p.CameraDeviceS114, 256) + "，触发=" + Safe(p.CameraTriggerModeS115, 64) + "；目标插件/SDK/驱动/物理设备与授权未验证。");
                            else if (p.ImageSourceModeS114 == VmImageSourceModeS114.Replay)
                                writer.Line(label + "：回放目录=" + Safe(p.ImageReplayFolderS114, 1024) + "；目录集合不递归随行，目标需人工准备/重新定位。");
                            else
                                writer.Line(label + "：本地图像声明=" + Safe(p.LocalImagePath, 1024) + "；随行状态以本次冻结清单为准。");
                            break;
                        case VmToolKind.TcpClientExchange:
                            writer.Line(label + "：TCP " + Safe(p.TcpClientHost, 256) + ":" + p.TcpClientPort.ToString(CultureInfo.InvariantCulture) + "，编码=" + Safe(p.TcpClientEncoding, 64) + Simulation(p.CommunicationSimulationS121) + "；未连接/外发，未输出发送模板/内容。");
                            break;
                        case VmToolKind.ModbusTcpExchange:
                            writer.Line(label + "：Modbus TCP " + Safe(p.ModbusHostS118, 256) + ":" + p.ModbusPortS118.ToString(CultureInfo.InvariantCulture) + "，站号=" + p.ModbusUnitIdS118.ToString(CultureInfo.InvariantCulture) + Simulation(p.CommunicationSimulationS121) + "；设备/寄存器与网络未验证，未读写。");
                            break;
                        case VmToolKind.ModbusRtuExchange:
                            writer.Line(label + "：Modbus RTU " + Safe(p.RtuPortNameS119, 64) + " / " + p.RtuBaudRateS119.ToString(CultureInfo.InvariantCulture) + " / " + Safe(p.RtuParityS119, 32) + " / stop=" + p.RtuStopBitsS119.ToString(CultureInfo.InvariantCulture) + ", station=" + p.RtuStationS119.ToString(CultureInfo.InvariantCulture) + Simulation(p.CommunicationSimulationS121) + "；串口驱动/映射/线路未验证，未打开串口。");
                            break;
                        case VmToolKind.CameraDistortionCorrection:
                            writer.Line(label + "：运行内参=" + (p.CameraIntrinsicsS126 == null ? "缺失" : "方案内数值（未运行验证）") + "；标定板=" + Safe(p.CameraBoardPathS127, 1024) + "；样本声明=" + (p.CameraSamplePathsS127 == null ? 0 : p.CameraSamplePathsS127.Count).ToString(CultureInfo.InvariantCulture) + "；求参文件是否随行见冻结清单。");
                            break;
                    }
                    if (p.Subflow != null) Scope(p.Subflow.Tools, label + " / 子流程", depth + 1);
                }
            }
        }

        private static string Simulation(bool value) { return value ? "（声明为离线模拟，非真实设备证据）" : "（声明为真实通讯，尚未现场验证）"; }

        private static string Safe(string value, int maximum)
        {
            if (string.IsNullOrWhiteSpace(value)) return "未声明";
            int length = Math.Min(value.Length, maximum);
            var text = new StringBuilder(length + 32);
            for (int i = 0; i < length; i++) text.Append(char.IsControl(value[i]) ? ' ' : value[i]);
            if (length < value.Length) text.Append("…（字段截断）");
            return text.ToString();
        }

        private sealed class BoundedReport
        {
            private const string TruncationSuffix = "\r\n【报告截断】超过 65536 字符预算，后续内容未列出；清单不完整，不能据此认定部署就绪。\r\n";
            private readonly StringBuilder text = new StringBuilder();
            private bool truncated;
            public bool Full { get { return text.Length >= MaximumReportCharacters - TruncationSuffix.Length; } }
            public void Line(string value = "")
            {
                string line = (value ?? string.Empty) + "\r\n";
                int available = MaximumReportCharacters - TruncationSuffix.Length - text.Length;
                if (available <= 0) { truncated = true; return; }
                if (line.Length > available) { text.Append(line, 0, available); truncated = true; }
                else text.Append(line);
            }
            public void MarkTruncated(string reason) { Line("【枚举截断】" + reason + "；报告不完整，不能认定部署就绪。"); }
            public string Finish() { if (truncated) text.Append(TruncationSuffix); return text.ToString(); }
        }
    }
}
