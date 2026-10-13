using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        [DataContract]
        private sealed class TemplateTargetDefinitionS145
        {
            [DataMember(Order = 1)] public VmToolParameterData Parameters;
            [DataMember(Order = 2)] public List<VmToolInputBindingData> Bindings;
            [DataMember(Order = 3)] public List<string> RoiIds;
            [DataMember(Order = 4)] public VmTemplateTargetParametersS145 Selection;
        }

        private sealed class TemplateTargetKeyStreamS145 : MemoryStream
        {
            public override void Write(byte[] buffer, int offset, int count)
            { if (count > 65536 - Position) throw new InvalidOperationException("模板参数身份超过64KiB预算，不生成不完整键。"); base.Write(buffer, offset, count); }
            public override void WriteByte(byte value)
            { if (Position >= 65536) throw new InvalidOperationException("模板参数身份超过64KiB预算。"); base.WriteByte(value); }
        }

        private string TemplateTargetDefinitionKeyS145(VmToolInstance tool)
        {
            if (tool.InputBindings.Count > 64 || tool.BoundRoiIds.Count > 256) throw new InvalidOperationException("模板订阅/ROI身份超过预算。");
            var data = new TemplateTargetDefinitionS145
            {
                Parameters = tool.Parameters.Clone(),
                Bindings = tool.InputBindings.OrderBy(item => item.TargetPortName, StringComparer.Ordinal).Select(item => item.Clone()).ToList(),
                RoiIds = tool.BoundRoiIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
                Selection = (tool.Parameters.TemplateTargetsS145 ?? new VmTemplateTargetParametersS145()).Clone()
            };
            // The optional null and an explicitly applied default have identical
            // selection semantics; otherwise keep the real parameter snapshot.
            data.Parameters.TemplateTargetsS145 = null;
            using (var stream = new TemplateTargetKeyStreamS145())
            using (var sha = SHA256.Create())
            {
                new DataContractJsonSerializer(typeof(TemplateTargetDefinitionS145)).WriteObject(stream, data);
                stream.Position = 0;
                return "template-target-v1:" + BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private VmImageSnapshot TemplateTargetInputS145(VmToolInstance tool, out string context, out string identity)
        {
            context = null; identity = null;
            var binding = tool.GetInputBinding("Image");
            if (binding == null) throw new InvalidOperationException("模板多目标输出须明确连接Image，不使用旧全局图。");
            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            var source = GetInputSourceTool(binding); object raw;
            if (source == null || !source.IsEnabled || source.ResultCode != "OK" || !source.LastOutputUtc.HasValue || !string.IsNullOrWhiteSpace(source.ErrorMessage) ||
                !source.TryGetOutputValue(binding.SourcePortName, out raw)) throw new InvalidOperationException("本次正式Image来源尚未运行或失效。");
            var image = raw as VmImageSnapshot;
            int count;
            using (var schema = new VmToolInstance { ToolId = source.ToolId, Kind = source.Kind, Parameters = source.Parameters.Clone() })
                count = ToolMetadata.GetInstanceOutputPorts(schema).Count(port => port.PortName == binding.SourcePortName && port.DataType == "Image");
            if (count != 1 || image == null || image.IsDisposed || string.IsNullOrWhiteSpace(image.ProducerToolId) || string.IsNullOrWhiteSpace(image.ProducerPortName))
                throw new InvalidOperationException("来源不是唯一正式受管Image或已释放。");
            context = ResolveOutputCoordinateContextS102(source, binding.SourcePortName);
            if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context))
                throw new InvalidOperationException("模板多目标缺少确定的ImagePixel图像链。");
            identity = binding.SourceToolId + "." + binding.SourcePortName + "|" + source.LastOutputUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture) +
                "|" + source.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture) + "|" + context;
            if (identity.Length > 2048) throw new InvalidOperationException("模板输入身份超过预算。");
            return image;
        }

        private string PrepareTemplateTargetContextS145(VmToolInstance tool, out string inputIdentity)
        {
            inputIdentity = null;
            if (tool.GetInputBinding("Image") == null && tool.Parameters.TemplateTargetsS145 == null) return null;
            string context; TemplateTargetInputS145(tool, out context, out inputIdentity); return context;
        }

        private void ClearUnqualifiedTemplateTargetsS145(VmToolInstance tool, string reason)
        {
            foreach (string port in new[] { "Centers", "Scores", "Angles", "Scales", "SourceIndices", "TargetCount", "SelectedIndex",
                "SelectedPoint", "SelectedPose", "SelectedScore", "SelectedScale", "TargetDefinitionKey", "TargetModelVersion", "TargetInputVersion", "TargetSortMode" })
                tool.SetOutputValue(port, null);
            tool.SetOutputValue("HasSelection", false); tool.SetOutputValue("TargetStatus", reason);
        }

        private void PublishTemplateTargetsS145(VmToolInstance tool, IEnumerable<VmTemplateTargetS145> targets,
            string context, string inputIdentity, string modelVersion)
        {
            if (string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(inputIdentity) || string.IsNullOrWhiteSpace(modelVersion))
            { ClearUnqualifiedTemplateTargetsS145(tool, "兼容最佳结果仍保留，但无正式Image/模块模型身份，多目标数组与选取未发布；请连接图像源并在本模块制作/加载模型。"); return; }
            var p = (tool.Parameters.TemplateTargetsS145 ?? new VmTemplateTargetParametersS145()).Clone();
            var result = VmTemplateTargetContractS145.Build(targets, p, context);
            string definition = TemplateTargetDefinitionKeyS145(tool);
            tool.SetOutputValue("Centers", result.Centers); tool.SetOutputValue("Scores", result.Scores);
            tool.SetOutputValue("Angles", result.Angles); tool.SetOutputValue("Scales", result.Scales); tool.SetOutputValue("SourceIndices", result.SourceIndices);
            tool.SetOutputValue("TargetCount", (double)result.TargetCount); tool.SetOutputValue("HasSelection", result.HasSelection);
            tool.SetOutputValue("SelectedIndex", result.SelectedIndex.HasValue ? (object)(double)result.SelectedIndex.Value : null);
            tool.SetOutputValue("SelectedPoint", result.SelectedPoint); tool.SetOutputValue("SelectedPose", result.SelectedPose);
            tool.SetOutputValue("SelectedScore", result.SelectedScore.HasValue ? (object)result.SelectedScore.Value : null);
            tool.SetOutputValue("SelectedScale", result.SelectedScale.HasValue ? (object)result.SelectedScale.Value : null);
            tool.SetOutputValue("TargetSortMode", p.SortMode); tool.SetOutputValue("TargetDefinitionKey", definition);
            tool.SetOutputValue("TargetModelVersion", modelVersion); tool.SetOutputValue("TargetInputVersion", inputIdentity);
            foreach (string port in new[] { "Centers", "SelectedPoint", "SelectedPose" }) tool.SetOutputCoordinateContext(port, context);
            tool.SetOutputValue("TargetStatus", result.TargetCount == 0 ? "本次无目标，数组为空，选中几何缺值。" :
                result.HasSelection ? "同序目标 " + result.TargetCount + "，正式选中 #" + result.SelectedIndex.Value + "；Pose仅位置/角度，真实尺度另列；不是物理跟踪或产品判定。" :
                "本次目标 " + result.TargetCount + "，选取编号 #" + p.SelectionIndex + " 越界，选中几何缺值、不钳位。");
        }

        private List<VmTemplateTargetS145> ConvertShapeTargetsS145(IList<ShapeMatchResult> matches, HImage input)
        {
            if (matches.Count > 1000) throw new InvalidOperationException("Shape候选超过1000，不截断。");
            HTuple width = null, height = null, objects = null;
            try
            {
                HOperatorSet.CountObj(input, out objects); HOperatorSet.GetImageSize(input, out width, out height);
                if (objects.I != 1 || width.I < 1 || height.I < 1 || width.I > 16384 || height.I > 16384 || (long)width.I * height.I > 16000000)
                    throw new InvalidOperationException("模板多目标只支持单幅1600万像素以内、边长16384以内图像。");
                var values = new List<VmTemplateTargetS145>(matches.Count);
                foreach (var match in matches)
                {
                    HTuple rigid = null, scaled = null, row = null, column = null;
                    try
                    {
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, match.Row, match.Column, match.AngleDeg * Math.PI / 180, out rigid);
                        HOperatorSet.HomMat2dScale(rigid, match.Scale, match.Scale, match.Row, match.Column, out scaled);
                        HOperatorSet.AffineTransPixel(scaled, 0, 0, out row, out column);
                        if (row.Length != 1 || column.Length != 1 || !VmImageEdgeLineParametersS143.Finite(row.D) || !VmImageEdgeLineParametersS143.Finite(column.D) ||
                            row.D < -0.5 || column.D < -0.5 || row.D > height.I - 0.5 || column.D > width.I - 0.5)
                            throw new InvalidOperationException("Shape本次模型参考点像素转换无效或越界，不直接使用HALCON变换Row/Column冒像素中心。");
                        values.Add(new VmTemplateTargetS145 { PixelRow = row.D, PixelColumn = column.D, AngleDegrees = match.AngleDeg,
                            Score = match.Score, Scale = match.Scale, SourceIndex = match.SourceIndex });
                    }
                    finally { foreach (var tuple in new[] { rigid, scaled, row, column }) if (tuple != null) tuple.Dispose(); }
                }
                return values;
            }
            finally { foreach (var tuple in new[] { width, height, objects }) if (tuple != null) tuple.Dispose(); }
        }

        private void RequireAppliedTemplateTargetS145(VmToolInstance source)
        {
            object key, version, inputVersion;
            string context, identity; TemplateTargetInputS145(source, out context, out identity);
            if (!source.TryGetOutputValue("TargetDefinitionKey", out key) || key as string != TemplateTargetDefinitionKeyS145(source) ||
                !source.TryGetOutputValue("TargetInputVersion", out inputVersion) || inputVersion as string != identity ||
                !source.TryGetOutputValue("TargetModelVersion", out version))
                throw new InvalidOperationException("选定位姿来自未应用的模板/选取草稿或旧输入，请先应用并重新运行。");
            if (source.Kind == VmToolKind.ShapeMatch)
            {
                TemplateItem item;
                if (!shapeToolResources.TryGetValue(source.ToolId, out item) || !item.HasModel || item.Service.RuntimeModelVersionS145 != version as string ||
                    !string.Equals(item.TemplatePath, source.Parameters.ShapeModelPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Shape模块模型已换代/未应用，不消费旧选定位姿。");
            }
            else if (source.Kind == VmToolKind.NccTemplateMatch)
            {
                var p = source.Parameters.NccMatchingS131;
                if (p == null || !p.HasModel || !string.Equals(p.ModelSha256, version as string, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("NCC模型版本已改变，不消费草稿或旧选定位姿。");
            }
        }

        private void RequireRigidAppliedTemplateTargetS145(VmToolInstance source)
        {
            RequireAppliedTemplateTargetS145(source);
            double scale;
            if (!source.TryGetNumericOutput("SelectedScale", out scale) || double.IsNaN(scale) || double.IsInfinity(scale) || Math.Abs(scale - 1) > 1e-9)
                throw new InvalidOperationException("当前位置修正/对齐只做刚体平移旋转，模板目标具有非单位真实尺度；不能忽略Scale冒完整变换，请使用显式缩放路径。");
        }

        private bool BuildTemplateTargetsExampleS145()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 选择多目标图像", true, null);
            var match = CreateFlowTool(VmToolKind.NccTemplateMatch, "02 制作模板并找多个目标", true, null);
            var points = CreateFlowTool(VmToolKind.PointSetGeometryStatistics, "03 本次目标参考点分析", true, null);
            var scores = CreateFlowTool(VmToolKind.NumberArrayStatistics, "04 同序分数数组统计", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "05 选中目标分数学习判定", true, null);
            foreach (var tool in new[] { source, match, points, scores, judge }) flowTools.Add(tool);
            match.SetInputBinding("Image", source.ToolId, "Image"); match.Parameters.NccMatchingS131.MaxMatches = 10;
            match.Parameters.TemplateTargetsS145 = new VmTemplateTargetParametersS145 { SortMode = "column", SelectionIndex = 0 };
            points.SetInputBinding("Points", match.ToolId, "Centers"); points.Parameters.PointSetMinimumCount = 1;
            scores.SetInputBinding("Array", match.ToolId, "Scores");
            judge.SetInputBinding("Value", match.ToolId, "SelectedScore"); judge.InputToolId = match.ToolId; judge.InputPortName = "SelectedScore";
            judge.NumericLowerLimit = 0.7; judge.NumericUpperLimit = 1;
            string path = GetHalconExampleImageS78("fabrik.png"); source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "EXAMPLE-TEMPLATE-TARGETS";
            return File.Exists(path);
        }

        private void LoadTemplateTargetsExampleS145_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadExampleProgramS78("template-multi-targets");
            if (lastExampleProgramKeyS78 == "template-multi-targets") HeaderStatusText.Text = "多目标学习方案已装载，不自动运行或预造模板。先运行图像源，在第02步读取输入/框选/制作并应用模板；再F5看多个真实目标，更多参数中排序/明细/明确选取。下游空数组/缺选按各模块契约拒绝；示例分数不是产品标准。";
        }
    }
}
