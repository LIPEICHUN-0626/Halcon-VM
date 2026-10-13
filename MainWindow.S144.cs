using System;
using System.Globalization;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmRegionSnapshot GetObjectRegionSourceS144(VmToolInstance tool, out string context)
        {
            var binding = tool.GetInputBinding("Region");
            if (binding == null) throw new InvalidOperationException("对象分析须明确订阅Region，不使用全局ROI或旧区域。");
            string bindingError = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(bindingError)) throw new InvalidOperationException(bindingError);
            var source = GetInputSourceTool(binding); object raw;
            if (source == null || (source.ResultCode != "OK" && source.ResultCode != "NG") || !string.IsNullOrWhiteSpace(source.ErrorMessage) ||
                !source.LastOutputUtc.HasValue || !source.TryGetOutputValue(binding.SourcePortName, out raw))
                throw new InvalidOperationException("Region来源本次未运行或已失效，请先运行上游。");
            var region = raw as VmRegionSnapshot;
            int matches;
            using (var schema = new VmToolInstance { ToolId = source.ToolId, Kind = source.Kind,
                Parameters = source.Parameters == null ? new VmToolParameterData() : source.Parameters.Clone() })
                matches = ToolMetadata.GetInstanceOutputPorts(schema).Count(port => port.PortName == binding.SourcePortName && port.DataType == "Region");
            if (matches != 1 || region == null || region.IsDisposed || string.IsNullOrWhiteSpace(region.ProducerToolId) || string.IsNullOrWhiteSpace(region.ProducerPortName))
                throw new InvalidOperationException("来源不是唯一正式有效Region，或已释放；不从摘要构造对象。");
            context = ResolveOutputCoordinateContextS102(source, binding.SourcePortName);
            if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context))
                throw new InvalidOperationException("Region图像像素坐标链尚未确定。");
            // Require a real image in this coordinate chain. A user-supplied label
            // alone is not evidence that a Region is in ImagePixel coordinates.
            string name;
            if (FindObjectReferenceImageS144(context, out name) == null)
                throw new InvalidOperationException("Region没有唯一有效的同链ImagePixel图像来源，不接受世界坐标/任意标签或旧全局图。");
            return region;
        }

        private VmImageSnapshot FindObjectReferenceImageS144(string wanted, out string sourceName)
        {
            sourceName = null; VmImageSnapshot found = null;
            var tools = subflowRuntimeScopesS102.SelectMany(scope => scope).Concat(flowTools)
                .Concat(subflowEditorsS102.SelectMany(frame => frame.ParentTools)).Distinct();
            foreach (var source in tools)
            {
                if ((source.ResultCode != "OK" && source.ResultCode != "NG") || !source.LastOutputUtc.HasValue || !string.IsNullOrWhiteSpace(source.ErrorMessage)) continue;
                using (var schema = new VmToolInstance { ToolId = source.ToolId, Kind = source.Kind,
                    Parameters = source.Parameters == null ? new VmToolParameterData() : source.Parameters.Clone() })
                    foreach (var port in ToolMetadata.GetInstanceOutputPorts(schema).Where(port => port.DataType == "Image"))
                    {
                        var image = GetImageSnapshot(source, port.PortName);
                        if (image == null || image.IsDisposed || ResolveOutputCoordinateContextS102(source, port.PortName) != wanted) continue;
                        // An alias to the same managed snapshot is not a second image.
                        if (found != null && !ReferenceEquals(found, image)) return null;
                        found = image; sourceName = source.InstanceName + "." + port.PortName;
                    }
            }
            return found;
        }

        private bool TryResolveObjectImageContextS144(VmToolInstance tool, string requested, out ResolvedImageContext image)
        {
            image = null; if (tool == null || tool.Kind != VmToolKind.ConnectedObjectAnalysisS144) return false;
            try
            {
                string context, name; GetObjectRegionSourceS144(tool, out context);
                var snapshot = FindObjectReferenceImageS144(context, out name);
                image = snapshot == null ? CreateEmptyImageContext(requested, "对应图像已失效，不回退全局图。", tool.InstanceName)
                    : CreateSnapshotContext(requested, snapshot, tool.InstanceName + " ← " + name, "同链区域对象参考图 · 非Image输出");
            }
            catch (Exception error) { image = CreateEmptyImageContext(requested, error.Message, tool.InstanceName + " · 等待Region来源"); }
            return true;
        }

        private InspectionRecord RunConnectedObjectsS144(VmToolInstance tool)
        {
            var p = (tool.Parameters.ConnectedObjectsS144 ?? new VmConnectedObjectParametersS144()).Clone();
            if (tool.BoundRoiIds.Count > 0 || tool.GetInputBinding("ROI") != null || !string.IsNullOrWhiteSpace(tool.Parameters.PositionCorrectionToolId))
                throw new InvalidOperationException("对象分析首版只分析明确输入Region，不支持额外ROI、屏蔽或位置修正；请在上游生成检测域并移除本地配置。");
            try
            {
                string context; var source = GetObjectRegionSourceS144(tool, out context);
                using (var input = source.CreateObjectAnalysisCopyS144())
                using (var result = ConnectedObjectServiceS144.Run(input, p))
                {
                    var selected = result.Rows.SingleOrDefault(item => item.Selected);
                    tool.SetOutputValue("Region", VmRegionSnapshot.CreateObjectAnalysisSnapshotS144(result.Regions, tool.ToolId, tool.InstanceName, "Region"));
                    tool.SetOutputValue("SelectedRegion", result.SelectedRegion == null ? null :
                        VmRegionSnapshot.CreateObjectAnalysisSnapshotS144(result.SelectedRegion, tool.ToolId, tool.InstanceName, "SelectedRegion"));
                    tool.SetOutputValue("Centers", VmPoint2DArray.CreateImagePixel(result.Rows.Select(item => item.Row), result.Rows.Select(item => item.Column), context));
                    tool.SetOutputValue("Areas", new VmNumberArrayS105(result.Rows.Select(item => item.Area)));
                    tool.SetOutputValue("SourceIndices", new VmNumberArrayS105(result.Rows.Select(item => (double)item.SourceIndex)));
                    tool.SetOutputValue("Count", (double)result.Rows.Count); tool.SetOutputValue("ConnectedCount", (double)result.ConnectedCount);
                    tool.SetOutputValue("TotalArea", result.TotalArea); tool.SetOutputValue("HasSelection", selected != null);
                    tool.SetOutputValue("SelectedPoint", selected == null ? null : VmPoint2D.CreateImagePixel(selected.Row, selected.Column, context));
                    tool.SetOutputValue("SelectedArea", selected == null ? null : (object)selected.Area);
                    tool.SetOutputValue("SelectedIndex", selected == null ? null : (object)(double)selected.Index);
                    foreach (string port in new[] { "Region", "SelectedRegion", "Centers", "SelectedPoint" }) tool.SetOutputCoordinateContext(port, context);
                    tool.SetOutputValue("Parameters", string.Format(CultureInfo.InvariantCulture,
                        "union1 -> connection(8) -> area [{0},{1}] px² -> stable {2} (area/row/column/source-index tie); selection={3} (0-based); input <=256 objects/100000 sum pixels; connected <=1000; graph labels <=100; no extra ROI/mask/correction; analysis OK is not product acceptance.",
                        p.MinimumArea, p.MaximumArea, p.SortMode, p.SelectionIndex));
                    tool.SetOutputValue("ResultCode", "OK");
                    string message = "HALCON连通域对象：连接 " + result.ConnectedCount + "，面积筛选后 " + result.Rows.Count +
                        "，总面积 " + result.TotalArea.ToString("0.###", CultureInfo.InvariantCulture) + " px²；" +
                        (selected == null ? "编号 #" + p.SelectionIndex + " 本次缺选，选中区域/中心/面积无值" : "选中 #" + selected.Index + "，面积 " + selected.Area.ToString("0.###", CultureInfo.InvariantCulture) + " px²") +
                        "。分析成功不是产品合格，请接对象数/面积/HasSelection判定。";
                    HObject overlay = result.Regions; result.Regions = null;
                    try { ReplaceToolOverlayRegion(overlay, tool, "Region", result.Rows.Count, result.TotalArea, "OK"); overlay = null; }
                    finally { if (overlay != null) overlay.Dispose(); }
                    var record = CreateRecord("ConnectedObjectAnalysisS144", "OK", result.Rows.Count, message, tool);
                    resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message); return record;
                }
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
        }

        private bool BuildObjectExampleS144()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 选择含独立对象图像", true, null);
            var threshold = CreateFlowTool(VmToolKind.ImageThreshold, "02 提取真实区域", true, null);
            var objects = CreateFlowTool(VmToolKind.ConnectedObjectAnalysisS144, "03 对象中心/面积与选取", true, null);
            var stats = CreateFlowTool(VmToolKind.NumberArrayStatistics, "04 同序面积数组统计", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "05 对象数量学习判定", true, null);
            foreach (var tool in new[] { source, threshold, objects, stats, judge }) flowTools.Add(tool);
            threshold.SetInputBinding("Image", source.ToolId, "Image");
            threshold.Parameters.ImageThresholdMinGray = 0; threshold.Parameters.ImageThresholdMaxGray = 100;
            objects.SetInputBinding("Region", threshold.ToolId, "Region"); objects.Parameters.ConnectedObjectsS144 = new VmConnectedObjectParametersS144();
            stats.SetInputBinding("Array", objects.ToolId, "Areas");
            judge.SetInputBinding("Value", objects.ToolId, "Count"); judge.InputToolId = objects.ToolId; judge.InputPortName = "Count";
            judge.NumericLowerLimit = 1; judge.NumericUpperLimit = 1000;
            string path = GetHalconExampleImageS78("clip.png"); source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "EXAMPLE-CONNECTED-OBJECTS";
            return System.IO.File.Exists(path);
        }

        private void LoadObjectExampleS144_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadExampleProgramS78("connected-objects");
            if (lastExampleProgramKeyS78 == "connected-objects") HeaderStatusText.Text = "对象学习方案已装载，不自动运行。选择自己的图像，先调上游阈值/检测域，再F5对象分析；明细仅查看，选取编号须明确写入草稿/应用。示例门槛不是产品标准，空面积数组的统计模块会明确拒绝。";
        }
    }
}
