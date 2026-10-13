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
        private string GetEdgeLineInputContextS143(VmToolInstance tool)
        {
            var binding = tool.GetInputBinding("Image");
            if (binding == null) throw new InvalidOperationException("边缘找直线须在输入页明确连接 Image，不使用活动旧图。");
            var source = GetInputSourceTool(binding);
            object raw;
            if (source == null || !source.TryGetOutputValue(binding.SourcePortName, out raw)) throw new InvalidOperationException("上游图像本次没有有效输出，请先运行上游。");
            var image = raw as VmImageSnapshot;
            int portCount;
            using (var schema = new VmToolInstance { ToolId = source.ToolId, Kind = source.Kind,
                Parameters = source.Parameters == null ? new VmToolParameterData() : source.Parameters.Clone() })
                portCount = ToolMetadata.GetInstanceOutputPorts(schema).Count(port => port.PortName == binding.SourcePortName && port.DataType == "Image");
            // Formal call/selection outputs may forward a child/branch image snapshot.
            // Preserve its real producer rather than relabeling it as the forwarding node.
            if (image == null || image.IsDisposed || portCount != 1 || string.IsNullOrWhiteSpace(image.ProducerToolId) || string.IsNullOrWhiteSpace(image.ProducerPortName))
                throw new InvalidOperationException("上游 Image 类型、生命周期或生产者身份不符。");
            string context = ResolveOutputCoordinateContextS102(source, binding.SourcePortName);
            if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context))
                throw new InvalidOperationException("上游图像链尚未确定。");
            return context;
        }

        private InspectionRecord RunImageEdgeLineS143(VmToolInstance tool, HImage input)
        {
            var p = (tool.Parameters.ImageEdgeLineS143 ?? new VmImageEdgeLineParametersS143()).Clone();
            string context = GetEdgeLineInputContextS143(tool);
            if (tool.BoundRoiIds.Count > 0 || tool.GetInputBinding("ROI") != null ||
                !string.IsNullOrWhiteSpace(tool.Parameters.PositionCorrectionToolId))
                throw new InvalidOperationException("找线首版只使用明确搜索带及真实图像有效域，不支持本地/继承 ROI、屏蔽或位置修正，请先移除这些配置。");
            try
            {
                using (var result = ImageEdgeLineServiceS143.Run(input, p, context, imageService))
                {
                    var points = result.Samples.Where(sample => sample.Row.HasValue).ToList();
                    int kept = points.Count(sample => sample.QualityKnown && sample.Kept), rejected = points.Count(sample => sample.QualityKnown && !sample.Kept);
                    tool.SetOutputValue("HasLine", result.Line != null);
                    tool.SetOutputValue("MainLine", result.Line == null ? null : result.Line.Clone());
                    tool.SetOutputValue("EdgePoints", VmPoint2DArray.CreateImagePixel(points.Select(sample => sample.Row.Value), points.Select(sample => sample.Column.Value), context));
                    var keptPoints = points.Where(sample => sample.Kept).ToList();
                    var rejectedPoints = points.Where(sample => sample.QualityKnown && !sample.Kept).ToList();
                    var missing = result.Samples.Where(sample => !sample.Row.HasValue).ToList();
                    tool.SetOutputValue("KeptPoints", VmPoint2DArray.CreateImagePixel(keptPoints.Select(sample => sample.Row.Value), keptPoints.Select(sample => sample.Column.Value), context));
                    tool.SetOutputValue("RejectedPoints", VmPoint2DArray.CreateImagePixel(rejectedPoints.Select(sample => sample.Row.Value), rejectedPoints.Select(sample => sample.Column.Value), context));
                    tool.SetOutputValue("MissingSamples", VmPoint2DArray.CreateImagePixel(missing.Select(sample => sample.CenterRow), missing.Select(sample => sample.CenterColumn), context));
                    tool.SetOutputValue("XLD", VmXldSnapshot.Create(result.Graphics, tool.ToolId, tool.InstanceName, "XLD"));
                    foreach (string port in new[] { "MainLine", "EdgePoints", "KeptPoints", "RejectedPoints", "MissingSamples", "XLD" }) tool.SetOutputCoordinateContext(port, context);
                    tool.SetOutputValue("SampledCount", (double)points.Count); tool.SetOutputValue("ValidCount", (double)kept);
                    tool.SetOutputValue("FailedCount", (double)(p.SampleCount - points.Count)); tool.SetOutputValue("RejectedCount", (double)rejected);
                    tool.SetOutputValue("Coverage", result.Coverage.HasValue ? (object)result.Coverage.Value : null);
                    tool.SetOutputValue("AngleDegrees", result.Line == null ? null : (object)result.Line.DirectionAngleDegrees);
                    tool.SetOutputValue("Length", result.Line == null ? null : (object)result.Line.Length);
                    tool.SetOutputValue("MeanResidual", result.MeanResidual.HasValue ? (object)result.MeanResidual.Value : null);
                    tool.SetOutputValue("MaxResidual", result.MaxResidual.HasValue ? (object)result.MaxResidual.Value : null);
                    tool.SetOutputValue("Samples", string.Join("\n", result.Samples.Select(sample => string.Format(CultureInfo.InvariantCulture,
                        "#{0} center=({1:0.###},{2:0.###}); edge={3}; amplitude={4}; residual={5}; {6}", sample.Index, sample.CenterRow, sample.CenterColumn,
                        sample.Row.HasValue ? "(" + sample.Row.Value.ToString("0.###", CultureInfo.InvariantCulture) + "," + sample.Column.Value.ToString("0.###", CultureInfo.InvariantCulture) + ")" : "missing",
                        sample.Amplitude.HasValue ? sample.Amplitude.Value.ToString("0.###", CultureInfo.InvariantCulture) : "missing",
                        sample.Residual.HasValue ? sample.Residual.Value.ToString("0.###", CultureInfo.InvariantCulture) : "missing", sample.Reason))));
                    tool.SetOutputValue("Parameters", string.Format(CultureInfo.InvariantCulture,
                        "HALCON MeasurePos + FitXldLines; Baseline=({0},{1})->({2},{3}); N={4}; SearchHalf={5}px; AverageHalf={6}px; Sigma={7}; Threshold={8}; Polarity={9}; Select={10}; Fit={11}; MaxResidual={12}px; MinCoverage={13}; normal(row,col)=(dcol,-drow); reversing endpoints reverses polarity direction.",
                        p.StartRow, p.StartColumn, p.EndRow, p.EndColumn, p.SampleCount, p.SearchHalfWidth, p.AveragingHalfWidth, p.Sigma, p.Threshold, p.Transition, p.Selection, p.FitAlgorithm, p.MaximumResidual, p.MinimumCoverage));
                    string code = result.Line == null ? "NG" : "OK";
                    tool.SetOutputValue("ResultCode", code);
                    string message = "HALCON 边缘找直线：" + result.Reason + " 采到 " + points.Count + "/" + p.SampleCount +
                        "，质量保留 " + kept + "，拒绝 " + rejected + (result.Line == null ? string.Empty :
                        " · 方向 " + result.Line.DirectionAngleDegrees.ToString("0.###", CultureInfo.InvariantCulture) + "°，最大残差 " + result.MaxResidual.Value.ToString("0.###", CultureInfo.InvariantCulture) + " px");
                    HObject graphics = result.Graphics; result.Graphics = null;
                    try { ReplaceToolOverlayContours(graphics, tool, "XLD", points.Count + (result.Line == null ? 0 : 1), code, result.Line == null ? 0 : result.Line.Length); graphics = null; }
                    finally { if (graphics != null) graphics.Dispose(); }
                    var record = CreateRecord("ImageEdgeLineFindingS143", code, points.Count, message, tool);
                    ApplyRecordImage(record, tool, input); resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
                    return record;
                }
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
        }

        private void LoadEdgeLineExampleS143_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadExampleProgramS78("image-edge-line");
            if (lastExampleProgramKeyS78 == "image-edge-line")
                HeaderStatusText.Text = "找线学习方案已装载，不自动运行。先运行图像源，再双击第 02 步，点击编辑搜索带放到真实直边，F5 看采点/方向/残差；第 03 步只是学习用覆盖门槛，需按实际任务调整。";
        }

        private bool BuildEdgeLineExampleS143()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 选择含直边的图像", true, null);
            var line = CreateFlowTool(VmToolKind.ImageEdgeLineFindingS143, "02 编辑搜索带并找直线", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "03 有效基线覆盖判定", true, null);
            foreach (var tool in new[] { source, line, judge }) flowTools.Add(tool);
            line.SetInputBinding("Image", source.ToolId, "Image");
            line.Parameters.ImageEdgeLineS143 = new VmImageEdgeLineParametersS143();
            judge.SetInputBinding("Value", line.ToolId, "Coverage"); judge.InputToolId = line.ToolId; judge.InputPortName = "Coverage";
            judge.NumericLowerLimit = 0.5; judge.NumericUpperLimit = 1;
            string path = GetHalconExampleImageS78("fabrik.png");
            source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "EXAMPLE-EDGE-LINE";
            return System.IO.File.Exists(path);
        }
    }
}
