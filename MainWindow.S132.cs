using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmPoseImageAlignmentParametersS132 alignmentDraftS132 = new VmPoseImageAlignmentParametersS132();

        private void LoadAlignmentDraftS132(VmToolParameterData parameters)
        {
            alignmentDraftS132 = (parameters.PoseImageAlignmentS132 ?? new VmPoseImageAlignmentParametersS132()).Clone();
            DockAlignmentSizeModeS132.SelectedValue = alignmentDraftS132.OutputSizeMode;
            DockAlignmentInterpolationS132.SelectedValue = alignmentDraftS132.Interpolation;
        }

        private bool ReadAlignmentDraftS132(VmToolParameterData parameters)
        {
            var p = ReadAlignmentChoicesUI03();
            string error = p.Validate();
            if (!string.IsNullOrWhiteSpace(error)) return FailDockDraft(error);
            parameters.PoseImageAlignmentS132 = p; return true;
        }

        private VmPoseImageAlignmentParametersS132 ReadAlignmentChoicesUI03()
        {
            // SelectedValue can lag behind SelectionChanged. Validate, preview
            // and apply the same visible choices, including invalid selections.
            var p = alignmentDraftS132.Clone();
            var size = DockAlignmentSizeModeS132.SelectedItem as ComboBoxItem;
            var interpolation = DockAlignmentInterpolationS132.SelectedItem as ComboBoxItem;
            p.OutputSizeMode = size == null ? null : size.Tag as string;
            p.Interpolation = interpolation == null ? null : interpolation.Tag as string;
            return p;
        }

        private string AlignmentCaptureHintUI03(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.PoseImageAlignment) return "请打开图像对齐模块";
            if (!IsVariationTaskStoppedS130() || nccTrainingCancellationS131 != null) return "请先停止运行或等待模板制作完成";
            if (!AlignmentBindingsMatchDraftS132(tool)) return "先应用输入来源，再运行上游定位";
            // Only inspect existing outputs. Painting this panel must not read
            // images, train, run or invoke the legacy HALCON pose adaptation.
            foreach (string port in new[] { "Image", "Pose" })
            {
                var binding = tool.GetInputBinding(port);
                if (binding == null) return port == "Image" ? "在“输入”中连接原始图像" : "在“输入”中连接定位结果";
                if (!string.IsNullOrWhiteSpace(GetInputBindingConfigurationError(tool, binding)))
                    return "请在“输入”中检查" + (port == "Image" ? "图像来源" : "定位来源");
                var source = GetInputSourceTool(binding); object raw;
                if (source == null || !source.IsEnabled || source.ResultCode != "OK" || !source.LastOutputUtc.HasValue ||
                    !string.IsNullOrWhiteSpace(source.ErrorMessage) || !source.TryGetOutputValue(binding.SourcePortName, out raw))
                    return port == "Image" ? "请先运行上游图像源" : "请先应用模板并运行上游定位";
                if (port == "Image" && (!(raw is VmImageSnapshot) || ((VmImageSnapshot)raw).IsDisposed)) return "请重新运行上游图像源";
                if (port != "Pose") continue;
                if (!(raw is VmPose2D)) return "上游尚无有效位姿，请重新定位";
                try
                {
                    if ((source.Kind == VmToolKind.ShapeMatch || source.Kind == VmToolKind.NccTemplateMatch) && binding.SourcePortName == "SelectedPose")
                        RequireRigidAppliedTemplateTargetS145(source);
                    if (source.Kind == VmToolKind.NccTemplateMatch)
                    {
                        var model = source.Parameters == null ? null : source.Parameters.NccMatchingS131; object version;
                        if (model == null || !model.HasModel || !source.TryGetOutputValue("ModelVersion", out version) ||
                            !string.Equals(version as string, model.ModelSha256, StringComparison.OrdinalIgnoreCase))
                            return "先应用模板并重跑定位，再采固定参考";
                    }
                }
                catch (InvalidOperationException) { return "请检查定位结果：应用模板后重跑，所选目标须为原始尺度"; }
            }
            // The explicit capture still revalidates coordinates, resources,
            // current image and bounds. This is not a production qualification.
            return null;
        }

        private string AlignmentInputContextS132(VmToolInstance tool)
        {
            var binding = tool == null ? null : tool.GetInputBinding("Image");
            return binding == null ? null : ResolveOutputCoordinateContextS102(GetInputSourceTool(binding), binding.SourcePortName);
        }

        private string AlignmentSourceSignatureForBindingsS132(VmToolInputBindingData image, VmToolInputBindingData pose)
        {
            if (image == null || pose == null) throw new InvalidOperationException("对齐必须明确连接 Image 与 Pose。" );
            return VmVariationModelS130.HashText(VariationSourceSignatureForBindingS130(image) + "\n" + VariationSourceSignatureForBindingS130(pose));
        }
        private string AlignmentSourceSignatureS132(VmToolInstance tool)
        { return AlignmentSourceSignatureForBindingsS132(tool.GetInputBinding("Image"), tool.GetInputBinding("Pose")); }

        private string AlignmentOutputContextS132(VmToolInstance tool, VmPoseImageAlignmentParametersS132 p)
        {
            if (tool == null || p == null || !p.HasReference || !string.IsNullOrWhiteSpace(p.Validate())) return null;
            return GetCoordinateIdentityBaseS102(tool) + ".Aligned:" + VmPoseImageAlignmentS132.GetOutputContextId(p);
        }

        private bool AlignmentBindingsMatchDraftS132(VmToolInstance tool)
        {
            return new[] { "Image", "Pose" }.All(name =>
            {
                var row = dockInputPortRows.FirstOrDefault(r => r.PortName == name);
                var port = ToolMetadata.GetInstanceInputPorts(tool).FirstOrDefault(p => p.PortName == name);
                return row != null && port != null && row.SelectedSourceKey == GetDockSavedInputSourceKey(tool, port);
            });
        }

        private VmPose2D GetAlignmentInputPoseS132(VmToolInstance tool)
        {
            var binding = tool.GetInputBinding("Pose");
            if (binding == null) throw new InvalidOperationException("请连接正式 Pose2D 位姿输出。" );
            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            var source = GetInputSourceTool(binding); object raw;
            if (source == null || source.ResultCode != "OK" || !source.TryGetOutputValue(binding.SourcePortName, out raw) || !(raw is VmPose2D))
                throw new InvalidOperationException("本次上游没有有效位姿；未找到模板或旧结果不得用于对齐。" );
            if ((source.Kind == VmToolKind.ShapeMatch || source.Kind == VmToolKind.NccTemplateMatch) && binding.SourcePortName == "SelectedPose")
                RequireRigidAppliedTemplateTargetS145(source);
            if (source.Kind == VmToolKind.NccTemplateMatch)
            {
                var model = source.Parameters == null ? null : source.Parameters.NccMatchingS131;
                object version;
                if (model == null || !model.HasModel || !source.TryGetOutputValue("ModelVersion", out version) || !string.Equals(version as string, model.ModelSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("NCC 位姿来自未应用的模板草稿，请先应用 NCC 参数并重新运行，再采参考或对齐。" );
            }
            var pose = ((VmPose2D)raw).Clone();
            // The legacy Shape pose contains HALCON iconic transformation coordinates. Adapt only in
            // this consumer; scalar Shape outputs, model matching and its stable drawing path stay untouched.
            if (source.Kind == VmToolKind.ShapeMatch && binding.SourcePortName == "Pose2D")
            {
                HTuple matrix = null, row = null, column = null;
                try
                {
                    HOperatorSet.VectorAngleToRigid(0, 0, 0, pose.Row, pose.Column, pose.AngleDegrees * Math.PI / 180, out matrix);
                    HOperatorSet.AffineTransPixel(matrix, 0, 0, out row, out column);
                    pose.Row = row.D; pose.Column = column.D;
                    pose.CoordinateContextId = GetImageCoordinateContextIdS65(source, "Image");
                }
                finally { if (matrix != null) matrix.Dispose(); if (row != null) row.Dispose(); if (column != null) column.Dispose(); }
            }
            error = pose.GetValidationError(); if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            string context = AlignmentInputContextS132(tool);
            if (pose.CoordinateContext != VmPose2D.ImagePixelContext || string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context) || pose.CoordinateContextId != context)
                throw new InvalidOperationException("Image 与 Pose 必须属于同一 ImagePixel 图像链；世界或其他图像坐标不能直接对齐。" );
            return pose;
        }

        private void DockAlignmentCaptureS132_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("保存图像对齐固定参考");
            if (editLeaseS137 < 0) return;
            RunUiAction("保存对齐参考姿态", delegate
            {
                var tool = dockEditingTool;
                if (tool == null || tool.Kind != VmToolKind.PoseImageAlignment || !IsVariationTaskStoppedS130() || nccTrainingCancellationS131 != null)
                    throw new InvalidOperationException("请停止运行后配置对齐参考。" );
                if (!AlignmentBindingsMatchDraftS132(tool)) throw new InvalidOperationException("Image / Pose 来源草稿已改变，请先应用，再取参考。" );
                var pose = GetAlignmentInputPoseS132(tool);
                using (var image = CreateToolInputImage(tool, "Image", false))
                {
                    int width, height, channels; string type;
                    VmPoseImageAlignmentS132.DescribeImage(image, out width, out height, out channels, out type);
                    if (pose.Row < 0 || pose.Row > height - 1 || pose.Column < 0 || pose.Column > width - 1)
                        throw new InvalidOperationException("参考位姿不在当前输入图像内。" );
                    var p = alignmentDraftS132.Clone();
                    p.HasReference = true; p.ReferenceRow = pose.Row; p.ReferenceColumn = pose.Column; p.ReferenceAngleDegrees = pose.AngleDegrees;
                    p.ReferenceWidth = width; p.ReferenceHeight = height; p.OutputWidth = width; p.OutputHeight = height;
                    p.ReferenceContextId = pose.CoordinateContextId; p.ReferenceSourceSignature = AlignmentSourceSignatureS132(tool);
                    p.ReferenceVersion = Guid.NewGuid().ToString("N");
                    string error = p.Validate(); if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    alignmentDraftS132 = p;
                }
                DockDraftValue_Changed(sender, e); RefreshAlignmentTaskS132();
                LogInfo(tool.InstanceName + "：保存固定参考到草稿 " + alignmentDraftS132.ReferenceVersion);
            });
        }

        private void RefreshAlignmentTaskS132()
        {
            if (DockAlignmentCaptureS132 == null) return;
            var p = alignmentDraftS132;
            VmToolInstance tool = dockEditingTool; object raw; string context;
            var applied = tool == null || tool.Parameters == null ? null : tool.Parameters.PoseImageAlignmentS132;
            bool referenceApplied = p.HasReference && applied != null && applied.HasReference && p.ReferenceVersion == applied.ReferenceVersion;
            DockAlignmentReferenceS132.Text = p.HasReference ? string.Format(CultureInfo.InvariantCulture, "{0} · {1}×{2}", referenceApplied ? "参考已应用" : "参考待应用", p.ReferenceWidth, p.ReferenceHeight) : "尚未采集参考";
            DockAlignmentReferenceS132.ToolTip = p.HasReference ? string.Format(CultureInfo.InvariantCulture, "行 {0:0.###} / 列 {1:0.###} / 角度 {2:0.###}°\n参考版本：{3}\n固定参考不会随下一帧移动；更新只进入草稿。", p.ReferenceRow, p.ReferenceColumn, p.ReferenceAngleDegrees, p.ReferenceVersion) : "先用标准位置的产品图像完成定位，再显式采集固定参考。";
            string hint = AlignmentCaptureHintUI03(tool);
            DockAlignmentCaptureS132.IsEnabled = hint == null;
            DockAlignmentCaptureS132.Content = p.HasReference ? "更新固定参考" : "采为固定参考";
            DockAlignmentCaptureS132.ToolTip = hint ?? "将当前图像及定位姿态保存到草稿；F5 试运行，确认后应用。不自动运行或改变已应用参考。";
            var view = ReadAlignmentChoicesUI03();
            bool ready = tool != null && tool.Kind == VmToolKind.PoseImageAlignment && tool.ResultCode == "OK" && tool.TryGetOutputValue("Image", out raw) && raw is VmImageSnapshot && !((VmImageSnapshot)raw).IsDisposed && tool.TryGetOutputCoordinateContext("Image", out context) && context == AlignmentOutputContextS132(tool, view);
            DockAlignmentCompareS132.IsEnabled = ready && !historicalImageMode && AlignmentBindingsMatchDraftS132(tool);
            DockAlignmentStateS132.Text = hint ?? (!p.HasReference ? "定位结果已就绪，可采为固定参考" :
                !referenceApplied ? "参考在草稿中 · F5 试运行，确认后应用" :
                dockDraftDirty ? "参数已修改 · F5 试运行，确认后应用" :
                ready ? "已生成对齐图，可查看前后对照" : "参考已就绪 · F5 运行对齐，再配置下游找线");
        }

        private void DockAlignmentCompareS132_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("查看原图与对齐图", delegate
            {
                var tool = dockEditingTool;
                string outputContext;
                var view = ReadAlignmentChoicesUI03();
                if (tool == null || tool.Kind != VmToolKind.PoseImageAlignment || tool.ResultCode != "OK" || GetImageSnapshot(tool, "Image") == null || historicalImageMode ||
                    !AlignmentBindingsMatchDraftS132(tool) ||
                    !tool.TryGetOutputCoordinateContext("Image", out outputContext) || outputContext != AlignmentOutputContextS132(tool, view))
                    throw new InvalidOperationException("请先试运行对齐模块。" );
                SetImageContextSelection(VmImageContextOption.ModuleOutput); SetDualImageMode(true);
                DualInputPortComboBox.SelectedValue = "Image";
                UpdateImageContextViewport(true); UpdateComparisonImageViewport(true); ScheduleRefreshDisplay();
                DockAlignmentStateS132.Text = "左：原始输入；右：对齐图 · ROI 坐标独立，黑边不作为有效检测域";
            });
        }

        private InspectionRecord RunPoseImageAlignmentS132(VmToolInstance tool, HImage input)
        {
            var p = (tool.Parameters.PoseImageAlignmentS132 ?? new VmPoseImageAlignmentParametersS132()).Clone();
            string error = p.Validate(); if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            if (!p.HasReference) throw new InvalidOperationException("请在当前模块显式保存参考位姿并应用。" );
            var pose = GetAlignmentInputPoseS132(tool);
            if (pose.CoordinateContextId != p.ReferenceContextId || AlignmentSourceSignatureS132(tool) != p.ReferenceSourceSignature)
                throw new InvalidOperationException("图像链或定位配置已改变，请显式重新取参考；旧对齐图不会继续使用。" );
            try
            {
                using (var result = VmPoseImageAlignmentS132.Align(input, pose, p))
                {
                    string context = AlignmentOutputContextS132(tool, p);
                    result.AlignedPose.CoordinateContextId = context;
                    string path, serial; GetToolImageMetadata(tool, out path, out serial);
                    tool.SetOutputValue("Image", VmImageSnapshot.Create(result.Image, tool.ToolId, tool.InstanceName, "Image", path, serial));
                    tool.SetOutputValue("Region", VmRegionSnapshot.Create(result.ValidDomain, tool.ToolId, tool.InstanceName, "Region"));
                    tool.SetOutputValue("AlignedPose", result.AlignedPose.Clone());
                    foreach (string port in new[] { "Image", "Region", "AlignedPose" }) tool.SetOutputCoordinateContext(port, context);
                    tool.SetOutputValue("Width", (double)result.OutputWidth); tool.SetOutputValue("Height", (double)result.OutputHeight);
                    tool.SetOutputValue("EffectivePixels", result.EffectivePixels);
                    tool.SetOutputValue("InputRow", pose.Row); tool.SetOutputValue("InputColumn", pose.Column); tool.SetOutputValue("InputAngleDegrees", pose.AngleDegrees);
                    tool.SetOutputValue("ReferenceRow", p.ReferenceRow); tool.SetOutputValue("ReferenceColumn", p.ReferenceColumn); tool.SetOutputValue("ReferenceAngleDegrees", p.ReferenceAngleDegrees);
                    tool.SetOutputValue("TranslationRow", result.PixelMatrix[2]); tool.SetOutputValue("TranslationColumn", result.PixelMatrix[5]);
                    double rotation = p.ReferenceAngleDegrees - pose.AngleDegrees;
                    rotation = (rotation % 360 + 540) % 360 - 180;
                    tool.SetOutputValue("RotationDegrees", rotation);
                    tool.SetOutputValue("ReferenceVersion", p.ReferenceVersion);
                    string transform = "PixelCenter=[" + string.Join(",", result.PixelMatrix.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "];HalconCorner=[" + string.Join(",", result.Matrix.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "]";
                    tool.SetOutputValue("Transform", transform);
                    tool.SetOutputValue("Parameters", p.OutputSizeMode + ";" + p.Interpolation + ";" + result.DomainPolicy + "; SourceContext=" + pose.CoordinateContextId + "; OutputContext=" + context);
                    string message = string.Format(CultureInfo.InvariantCulture, "HALCON 位姿对齐：旋转 {0:0.###}° · 平移矩阵 R={1:0.###} C={2:0.###} · 输出 {3}×{4} · 有效 {5:0} px · {6}", rotation, result.PixelMatrix[2], result.PixelMatrix[5], result.OutputWidth, result.OutputHeight, result.EffectivePixels, result.DomainPolicy);
                    var record = CreateRecord("PoseImageAlignment", "OK", result.EffectivePixels, message, tool);
                    ApplyRecordImage(record, tool, result.Image); resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
                    RefreshResultOverlayLayers(); return record;
                }
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
        }
    }
}
