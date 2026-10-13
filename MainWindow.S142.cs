using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class ProductionImageFrameS142 : IDisposable
        {
            public VmProductionImageEvidenceS142 Metadata;
            public HImage Image;
            public bool Attempted;
            public void Dispose() { if (Image != null) { Image.Dispose(); Image = null; } }
        }

        private sealed class ProductionImageCycleS142 : IDisposable
        {
            public VmProductionRunRecordS135 Record;
            public VmProductionImagePolicyS142 Policy;
            public long CopiedBytes;
            public List<ProductionImageFrameS142> Frames = new List<ProductionImageFrameS142>();
            public void Dispose() { foreach (var frame in Frames) frame.Dispose(); }
        }

        private ProductionImageCycleS142 productionImageCycleS142;
        private bool productionEvidenceReadBusyS142;
        private ProductionImageEvidenceStoreS142 CreateProductionEvidenceStoreS142()
        { return new ProductionImageEvidenceStoreS142(Path.GetDirectoryName(productionRunStoreS135.DirectoryPath)); }

        private void BeginProductionImageCycleS142(VmProductionRunRecordS135 run, string runId,
            VmFlowRunRequestMode mode, VmRunOriginS135 origin, bool childEditor, bool simulated)
        {
            if (productionImageCycleS142 != null) productionImageCycleS142.Dispose();
            productionImageCycleS142 = null;
            if (run == null) return;
            run.EvidenceKnownS142 = true;
            run.EvidenceImagesS142 = new List<VmProductionImageEvidenceS142>();
            run.EvidenceStatusS142 = "本次不是明确真实生产 Full 周期，不留图。";
            if (mode != VmFlowRunRequestMode.Full || childEditor || simulated || isImageQueueBatchRunning ||
                (origin != VmRunOriginS135.ManualFull && origin != VmRunOriginS135.Continuous && origin != VmRunOriginS135.TcpTrigger) ||
                run.RunId != runId || run.PageMode != "Production" || !run.SimulationKnownS138 || run.IsSimulationS138 ||
                run.FlowId != activeProjectFlowIdS104 || run.WorkspaceSessionId != projectSessionIdS104 ||
                run.AlgorithmDefinitionKeyVersionS138 != ProductionDefinitionIdentityS138.Version ||
                string.IsNullOrWhiteSpace(run.AlgorithmDefinitionKeyS138)) return;
            try
            {
                var policies = (productionRunLayout.ImagePoliciesS142 ?? new List<VmProductionImagePolicyS142>())
                    .Where(item => item != null && string.Equals(item.FlowId, run.FlowId, StringComparison.Ordinal)).Take(2).ToList();
                if (policies.Count == 0 || policies.Count == 1 && policies[0].Mode == "Off")
                { run.EvidenceStatusS142 = "周期留图未启用。"; return; }
                if (policies.Count != 1) throw new InvalidOperationException("本流程留图策略重复。");
                var policy = policies[0].Clone();
                string policyError = policy.Validate();
                if (policyError.Length > 0) throw new InvalidOperationException(policyError);
                if ((policy.Mode != "Failures" && policy.Mode != "All") || policy.SourceToolIds == null ||
                    policy.SourceToolIds.Count < 1 || policy.SourceToolIds.Count > 4 ||
                    policy.SourceToolIds.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 256) ||
                    policy.SourceToolIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != policy.SourceToolIds.Count)
                    throw new InvalidOperationException("留图范围/根图像源身份缺失、重复或超过四项。");
                var cycle = new ProductionImageCycleS142 { Record = run, Policy = policy };
                foreach (string sourceId in policy.SourceToolIds)
                {
                    var sources = flowTools.Where(tool => string.Equals(tool.ToolId, sourceId, StringComparison.Ordinal)).Take(2).ToList();
                    string reason = sources.Count != 1 || sources[0].Kind != VmToolKind.ImageSource
                        ? "显式根图像源缺失、重复或类型已改变；不按同名绑定。"
                        : !sources[0].IsEnabled ? "根图像源已停用。" : "本周期尚未到达该源采集步骤。";
                    cycle.Frames.Add(new ProductionImageFrameS142 { Metadata = new VmProductionImageEvidenceS142
                    {
                        RunId = run.RunId, FlowId = run.FlowId, ToolId = sourceId, PortName = "Image",
                        State = "Missing", Detail = reason
                    } });
                }
                productionImageCycleS142 = cycle;
                run.EvidenceStatusS142 = "本次留图配置已冻结，待源采集及终态范围决定。";
            }
            catch (Exception error)
            { run.EvidenceStatusS142 = "本次留图配置无效：" + LimitEvidenceTextS142(error.Message, 200); }
        }

        private void CaptureProductionImageStepS142(VmProductionRunRecordS135 run, VmToolInstance tool,
            InspectionRecord inspection, string terminal, DateTime startedUtc)
        {
            var cycle = productionImageCycleS142;
            if (cycle == null || run == null || tool == null || !ReferenceEquals(cycle.Record, run)) return;
            var frame = cycle.Frames.FirstOrDefault(item => item.Metadata.ToolId == tool.ToolId);
            if (frame == null) return;
            try
            {
                if (frame.Attempted) throw new InvalidOperationException("同一源步骤在根周期中重复，不猜测哪一帧为证据。");
                frame.Attempted = true;
                if (run.FlowId != activeProjectFlowIdS104 || run.WorkspaceSessionId != projectSessionIdS104)
                    throw new InvalidOperationException("源采集的流程/会话已变化。");
                object raw;
                var ports = ToolMetadata.GetInstanceOutputPorts(tool).Where(port => port.PortName == "Image").Take(2).ToList();
                if (tool.Kind != VmToolKind.ImageSource || !tool.IsEnabled || inspection == null || inspection.ResultCode != "OK" ||
                    !string.IsNullOrWhiteSpace(tool.ErrorMessage) || !tool.LastOutputUtc.HasValue || tool.LastOutputUtc.Value < startedUtc ||
                    ports.Count != 1 || ports[0].DataType != "Image" || !tool.TryGetOutputValue("Image", out raw))
                    throw new InvalidOperationException("本次源采集没有成功的正式 Image 输出（" + (inspection == null ? terminal : inspection.ResultCode) + "）。");
                var image = raw as VmImageSnapshot;
                if (image == null || image.IsDisposed || image.ProducerToolId != tool.ToolId || image.ProducerPortName != "Image")
                    throw new InvalidOperationException("Image 已释放、类型或真实生产者身份不符。");
                string context = ResolveOutputCoordinateContextS102(tool, "Image");
                if (context != GetCoordinateIdentityBaseS102(tool) + ".Image" || IsPendingCoordinateContextS102(context))
                    throw new InvalidOperationException("源图不是本次明确的 ImagePixel 图像链。");
                string path = ReadSourceTextS142(tool, "Path", 4096), serial = ReadSourceTextS142(tool, "SN", 256);
                string sourceMode = ReadSourceTextS142(tool, "SourceMode", 64);
                string timestamp = ReadSourceTextS142(tool, "Timestamp", 128);
                DateTimeOffset capturedAt;
                if (path != image.SourcePath || serial != image.SerialNumber || string.IsNullOrWhiteSpace(serial) ||
                    (sourceMode != "File" && sourceMode != "Replay" && sourceMode != "Camera") ||
                    !DateTimeOffset.TryParseExact(timestamp, "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out capturedAt) ||
                    capturedAt.UtcDateTime < startedUtc || capturedAt.UtcDateTime > DateTime.UtcNow)
                    throw new InvalidOperationException("本次源图的路径文字/SN/采集时间/模式事实不一致。");
                int width, height, channels;
                frame.Image = image.CreateEvidenceCopyS142(64L * 1024 * 1024 - cycle.CopiedBytes, out width, out height, out channels);
                cycle.CopiedBytes += (long)width * height * channels;
                var metadata = frame.Metadata;
                metadata.CoordinateContext = context; metadata.SerialNumber = serial; metadata.SourcePath = path;
                metadata.CapturedAtUtc = capturedAt.UtcDateTime.ToString("o", CultureInfo.InvariantCulture); metadata.SourceMode = sourceMode;
                metadata.Width = width; metadata.Height = height; metadata.Channels = channels; metadata.PixelType = "byte";
                metadata.Domain = "Full"; metadata.State = "Pending";
                metadata.Detail = "本周期源采集原图；不是故障步骤输入/结果或缺陷叠加。";
            }
            catch (Exception error)
            {
                frame.Dispose(); frame.Metadata.State = "Missing";
                frame.Metadata.Detail = LimitEvidenceTextS142(error.Message, 1024);
            }
        }

        private static string ReadSourceTextS142(VmToolInstance tool, string port, int maximum)
        {
            object value;
            if (!tool.TryGetOutputValue(port, out value) || !(value is string) || ((string)value).Length > maximum)
                throw new InvalidOperationException("本次源采集正式 " + port + " 输出缺失或超出预算。");
            return (string)value;
        }

        private async Task PublishProductionImagesS142(VmProductionRunRecordS135 run)
        {
            var cycle = productionImageCycleS142;
            if (cycle == null || !ReferenceEquals(cycle.Record, run)) return;
            var published = new List<VmProductionImageEvidenceS142>();
            try
            {
                bool include = run.TerminalState == "STOP" ? cycle.Policy.IncludeStopped :
                    cycle.Policy.Mode == "All" || run.TerminalState == "NG" || run.TerminalState == "ERROR" || run.TerminalState == "TIMEOUT";
                if (!include) { run.EvidenceStatusS142 = "本周期终态不在显式留图范围。"; return; }
                if (run.FlowId != activeProjectFlowIdS104 || run.WorkspaceSessionId != projectSessionIdS104 ||
                    run.AlgorithmDefinitionKeyVersionS138 != ProductionDefinitionIdentityS138.Version ||
                    run.AlgorithmDefinitionKeyS138 != ProductionDefinitionIdentityS138.Compute(CaptureRecipe()))
                    throw new InvalidOperationException("运行中正式定义/流程/会话变化，本次图像不作可靠周期关联。");
                // Only this accepted execution slot writes these independent frames; no background queue.
                var frozen = await Task.Run(() =>
                {
                    var store = cycle.Frames.Any(frame => frame.Image != null) ? CreateProductionEvidenceStoreS142() : null;
                    long remainingEncoded = 64L * 1024 * 1024;
                    foreach (var frame in cycle.Frames)
                    {
                        var image = frame.Image == null ? frame.Metadata.Clone() :
                            store.Publish(frame.Metadata, path => HOperatorSet.WriteImage(frame.Image, "png", 0, path), remainingEncoded);
                        if (image.State == "Saved") remainingEncoded -= image.ByteLength;
                        published.Add(image);
                    }
                    return published;
                });
                run.EvidenceImagesS142 = frozen;
                int saved = frozen.Count(image => image.State == "Saved");
                run.EvidenceStatusS142 = "源图已保存 " + saved + "/" + frozen.Count +
                    "；等待周期 JSON 关联。缺图不会用旧帧或当前显示图补充。";
            }
            catch (Exception error)
            {
                run.EvidenceStatusS142 = "留图未完整保存：" + LimitEvidenceTextS142(error.Message, 200);
                run.EvidenceImagesS142 = cycle.Frames.Select(frame =>
                {
                    var saved = published.FirstOrDefault(item => item.ToolId == frame.Metadata.ToolId && item.State == "Saved");
                    if (saved != null) return saved;
                    var metadata = frame.Metadata.Clone(); metadata.State = "Missing";
                    metadata.Detail = LimitEvidenceTextS142(error.Message, 1024); return metadata;
                }).ToList();
            }
            finally
            {
                cycle.Dispose();
                if (ReferenceEquals(productionImageCycleS142, cycle)) productionImageCycleS142 = null;
            }
        }

        private async Task<string> FinishProductionImageAssociationS142(VmProductionRunRecordS135 run, bool saved)
        {
            if (!run.EvidenceKnownS142 || run.EvidenceImagesS142 == null || !run.EvidenceImagesS142.Any(image => image.State == "Saved")) return string.Empty;
            if (saved) return " · 原图证据 " + run.EvidenceImagesS142.Count(image => image.State == "Saved") + "/" + run.EvidenceImagesS142.Count + " 已关联";
            try
            {
                var warnings = await Task.Run(() => CreateProductionEvidenceStoreS142().CleanupOwned(run.EvidenceImagesS142));
                return " · 图像未关联周期，已请求仅清理本次已确认文件" + (warnings.Count == 0 ? string.Empty : "；残留警告：" + LimitEvidenceTextS142(string.Join("；", warnings), 1800));
            }
            catch (Exception error) { return " · 图像未关联，残留清理未完成：" + LimitEvidenceTextS142(error.Message, 1500); }
        }

        private void ValidateProductionImageConfigurationS142(VmProductionRunLayoutData layout)
        {
            var policies = layout.ImagePoliciesS142 ?? new List<VmProductionImagePolicyS142>();
            if (policies.Count > 32 || policies.Any(item => item == null) ||
                policies.Select(item => item.FlowId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != policies.Count)
                throw new InvalidOperationException("每个稳定流程仅有一份留图策略，最多 32 份。");
            var flows = CaptureRecipe().Flows ?? new List<VmProjectFlowData>();
            foreach (var policy in policies)
            {
                if (policy.Mode != "Off" && policy.Mode != "Failures" && policy.Mode != "All") throw new InvalidOperationException("留图范围无效。");
                if (policy.Mode == "Off") continue; // An explicitly disabled dangling historical policy is safe to keep.
                var matches = flows.Where(flow => flow != null && flow.FlowId == policy.FlowId).Take(2).ToList();
                if (matches.Count != 1 || policy.SourceToolIds == null || policy.SourceToolIds.Count < 1 || policy.SourceToolIds.Count > 4 ||
                    policy.SourceToolIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != policy.SourceToolIds.Count)
                    throw new InvalidOperationException("留图需明确选择现有根流程和 1–4 个独立图像源。");
                foreach (string toolId in policy.SourceToolIds)
                {
                    var tools = (matches[0].Tools ?? new List<ToolFlowRecipeItem>()).Where(tool => tool != null && tool.ToolId == toolId).Take(2).ToList();
                    if (tools.Count != 1 || tools[0].ToolType != VmToolKind.ImageSource.ToString() || !tools[0].IsEnabled)
                        throw new InvalidOperationException("留图来源须是已启用的正式根图像源；复制/删除后请明确重选，不按同名猜测。");
                }
            }
        }

        private async void ReadProductionEvidenceS142(ProductionRunHistoryWindowS135 window, VmProductionEvidenceRequestEventArgsS142 request)
        {
            if (!ReferenceEquals(window, productionRunHistoryWindowS135)) return;
            if (productionEvidenceReadBusyS142) { window.SetEvidenceErrorS142("上一项证据正在读取，请稍后再查看。", request.Generation); return; }
            productionEvidenceReadBusyS142 = true;
            try
            {
                byte[] bytes = await Task.Run(() => CreateProductionEvidenceStoreS142().ReadVerified(request.Record, request.Image));
                if (ReferenceEquals(window, productionRunHistoryWindowS135))
                    window.SetEvidenceBytesS142(bytes, "只读源采集原图；已核对记录身份、长度与字节 SHA。不是故障步骤叠加或历史重放。", request.Generation);
            }
            catch (Exception error)
            {
                if (ReferenceEquals(window, productionRunHistoryWindowS135))
                    window.SetEvidenceErrorS142("原图证据不可用：" + LimitEvidenceTextS142(error.Message, 1500), request.Generation);
            }
            finally { productionEvidenceReadBusyS142 = false; }
        }

        private static string LimitEvidenceTextS142(string value, int maximum)
        { return value == null ? string.Empty : value.Length <= maximum ? value : value.Substring(0, maximum - 1) + "…"; }
    }
}
