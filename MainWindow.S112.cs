using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private string imageSourceOptionsSignatureS112;
        private string roiDrawingImageContextS112;
        private int roiDrawingWidthS112;
        private int roiDrawingHeightS112;
        private WeakReference roiDrawingToolS112;

        private string GetImageContextSelectionKeyS112()
        {
            // SelectionChanged can precede SelectedValue's property commit. The selected item is authoritative.
            VmImageContextOption selected = ImageContextComboBox?.SelectedItem as VmImageContextOption;
            return selected?.Key ?? ImageContextComboBox?.SelectedValue as string ?? VmImageContextOption.GlobalInput;
        }

        private void ImageRoiMenuButton_ClickS112(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button?.ContextMenu == null || !button.IsEnabled) return;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }

        private void CaptureRoiDrawingContextS112(ResolvedImageContext context)
        {
            roiDrawingImageContextS112 = GetImageCoordinateContextS112(context);
            roiDrawingWidthS112 = context.Width;
            roiDrawingHeightS112 = context.Height;
            VmToolInstance tool = GetImageTaskToolS89();
            roiDrawingToolS112 = tool == null ? null : new WeakReference(tool);
        }

        private bool IsRoiDrawingContextCurrentS112(ResolvedImageContext context)
        {
            VmToolInstance capturedTool = roiDrawingToolS112?.Target as VmToolInstance;
            return !historicalImageMode && context != null && context.HasImage &&
                !string.IsNullOrWhiteSpace(roiDrawingImageContextS112) &&
                context.Width == roiDrawingWidthS112 && context.Height == roiDrawingHeightS112 &&
                string.Equals(roiDrawingImageContextS112, GetImageCoordinateContextS112(context), StringComparison.OrdinalIgnoreCase) &&
                (roiDrawingToolS112 == null || capturedTool != null && flowTools.Contains(capturedTool)) &&
                ReferenceEquals(capturedTool, GetImageTaskToolS89());
        }

        private void DiscardChangedRoiDrawingS112(ResolvedImageContext context)
        {
            if (roiDrawingImageContextS112 == null || IsRoiDrawingContextCurrentS112(context)) return;
            bool hadDrawing = pendingRoi != null || roiEditor.PreviewRoi != null || roiEditor.IsPolygonDrawing;
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Select;
            roiDrawingImageContextS112 = null;
            roiDrawingToolS112 = null;
            if (hadDrawing) HeaderStatusText.Text = "未确认ROI已取消：画面来源、尺寸或编辑模块已改变。已有ROI未移动或删除。";
        }

        private void IncludeConfirmedRoiInDockDraftS112(VmRoiLayer layer, VmToolInstance tool)
        {
            if (tool == null || !centralModuleConfigurationOpenS77 || !ReferenceEquals(tool, dockEditingTool) || !ToolMetadata.SupportsRoi(tool.Kind)) return;
            // Keep existing draft choices and fields; never reload the complete editor here.
            if (!dockRoiBindingRows.Any(row => row.RoiId == layer.RoiId))
                dockRoiBindingRows.Add(new VmRoiBindingItem { Layer = layer, IsBound = tool.IsRoiBound(layer.RoiId), IsCompatible = IsRoiCompatibleWithToolS112(layer, tool) });
            UpdateDockRoiSummary();
        }

        private void RefreshImageSourceOptionsS112()
        {
            if (ImageContextComboBox == null) return;
            var sources = flowTools.Where(item => item.Kind == VmToolKind.ImageSource).ToList();
            var entries = new List<VmImageContextOption>(VmImageContextOption.CreateAll());
            var inputTool = GetImageTaskToolS89();
            if (inputTool != null)
                foreach (var port in ToolMetadata.GetInstanceInputPorts(inputTool).Where(item => item.DataType == "Image"))
                    entries.Add(new VmImageContextOption { Key = VmImageContextOption.InputPortPrefix + port.PortName,
                        DisplayText = inputTool.InstanceName + " · " + port.DisplayName + "（已应用）" });
            foreach (VmToolInstance source in sources)
            {
                VmImageSnapshot snapshot = GetImageSnapshot(source, "Image");
                bool ready = source.IsEnabled && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) &&
                    snapshot != null && !snapshot.IsDisposed;
                string status = !source.IsEnabled ? "停用" : ready
                    ? snapshot.Width.ToString(CultureInfo.InvariantCulture) + "×" + snapshot.Height.ToString(CultureInfo.InvariantCulture)
                    : !string.IsNullOrWhiteSpace(source.ErrorMessage) ? "异常" : "待运行";
                entries.Add(new VmImageContextOption
                {
                    Key = VmImageContextOption.SourcePrefix + source.ToolId,
                    DisplayText = source.InstanceName + " · " + status
                });
            }
            string previous = GetImageContextSelectionKeyS112();
            if (!entries.Any(item => string.Equals(item.Key, previous, StringComparison.OrdinalIgnoreCase)))
            {
                if (previous.StartsWith(VmImageContextOption.SourcePrefix, StringComparison.Ordinal) || previous.StartsWith(VmImageContextOption.InputPortPrefix, StringComparison.Ordinal))
                    entries.Add(new VmImageContextOption { Key = previous, DisplayText = "已移除来源（请重新选择）" });
                else { previous = VmImageContextOption.GlobalInput; imageContextManuallySelected = false; }
            }
            string signature = string.Join("|", entries.Select(item => item.Key + "=" + item.DisplayText));
            if (signature == imageSourceOptionsSignatureS112) return;
            imageContextUpdating = true;
            try
            {
                ImageContextComboBox.ItemsSource = entries;
                ImageContextComboBox.SelectedValue = previous;
                imageSourceOptionsSignatureS112 = signature;
            }
            finally { imageContextUpdating = false; }
        }

        private string GetImageCoordinateContextS112(ResolvedImageContext image)
        {
            if (image == null || !image.HasImage) return null;
            if (image.Snapshot == null) return "Global.Image";
            VmToolInstance producer = flowTools.FirstOrDefault(item =>
                string.Equals(item.ToolId, image.Snapshot.ProducerToolId, StringComparison.OrdinalIgnoreCase));
            return producer == null ? null : ResolveOutputCoordinateContextS102(producer, image.Snapshot.ProducerPortName);
        }

        private void DockViewInputImageUI05_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as FrameworkElement)?.DataContext as VmInputPortEditorRow;
            var tool = dockEditingTool;
            if (!centralModuleConfigurationOpenS77 || tool == null || !flowTools.Contains(tool) || row == null ||
                !dockInputPortRows.Contains(row) || row.DataType != "Image") return;
            string error = null;
            var port = ToolMetadata.GetInstanceInputPorts(tool).FirstOrDefault(item => item.PortName == row.PortName && item.DataType == "Image");
            if (historicalImageMode || isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
                error = "请先停止运行或退出历史查看，再查看输入图像。";
            else if (pendingRoi != null || roiEditor.IsDrawing || roiEditor.IsPolygonDrawing || roiGeometryDraftDirty || roiEditingLayer != null)
                error = "请先确认/应用或取消正在编辑的 ROI，再切换输入画面。";
            else if (port == null || row.SelectedSourceKey != GetDockSavedInputSourceKey(tool, port))
                error = "这路来源尚未应用；请先应用或取消来源修改。查看图像不会自动应用或运行。";
            if (error != null) { DockInputSummaryText.Text = error; return; }
            RefreshImageSourceOptionsS112();
            SetImageContextSelection(VmImageContextOption.InputPortPrefix + row.PortName);
            imageContextManuallySelected = true;
            UpdateImageContextViewport(true);
            RefreshUiState(); ScheduleRefreshDisplay();
            DockInputSummaryText.Text = "正在查看：" + row.DisplayName + " · 已应用来源；缺图时保持空白，不会自动读图。";
        }

        private ResolvedImageContext ResolveImageInputPortUI05(VmToolInstance tool, VmPortDefinition port, string requested)
        {
            if (tool == null || port == null || !flowTools.Contains(tool))
                return CreateEmptyImageContext(requested, "当前模块已无此 Image 输入，请重新选择。", "输入不可用");
            var binding = tool.GetInputBinding(port.PortName);
            string label = tool.InstanceName + "." + port.DisplayName;
            if (binding == null)
                return port.PortName == "Image" ? CreateGlobalImageContext(requested, currentImage == null ? "系统.Image 暂无图像，请明确加载。" : "已应用输入 · 系统.Image", label + " ← 系统.Image") :
                    CreateEmptyImageContext(requested, "此输入尚未连接；不会借用另一输入或全局图像。", label + " ← 未连接");
            var source = GetInputSourceTool(binding);
            var snapshot = GetImageSnapshot(source, binding.SourcePortName);
            bool validPort = source != null && ToolMetadata.GetInstanceOutputPorts(source).Any(item => item.PortName == binding.SourcePortName && item.DataType == "Image");
            string sourceLabel = label + " ← " + GetBindingSourceText(binding);
            if (!validPort || flowTools.IndexOf(source) >= flowTools.IndexOf(tool) || !source.IsEnabled || !source.LastOutputUtc.HasValue ||
                (source.ResultCode != "OK" && source.ResultCode != "NG") || !string.IsNullOrWhiteSpace(source.ErrorMessage) || snapshot == null || snapshot.IsDisposed)
                return CreateEmptyImageContext(requested, "已应用来源暂无有效图像；请检查并明确运行上游。", sourceLabel);
            return CreateSnapshotContext(requested, snapshot, sourceLabel,
                source.RuntimeResultOrigin == VmRuntimeResultOrigin.ModuleExecution ? "已应用输入 · 最近执行图像" : "已应用输入 · 来源草稿/未标注预览，不代表正式结果");
        }

        private void DockLocateRoiUI03_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as FrameworkElement)?.DataContext as VmRoiBindingItem;
            var tool = dockEditingTool;
            string error = null;
            if (!centralModuleConfigurationOpenS77 || tool == null || !flowTools.Contains(tool) ||
                row == null || !dockRoiBindingRows.Contains(row) || row.Layer == null || !roiLayers.Contains(row.Layer))
                error = "ROI 或编辑模块已变化，请重新选择当前列表中的图层。";
            else if (historicalImageMode || isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87 || System.Threading.Volatile.Read(ref tcpTriggerPendingS104) != 0)
                error = "请先停止运行或退出历史查看，再选择 ROI。";
            else if (roiGeometryDraftDirty || roiEditingLayer != null || pendingRoi != null || roiEditor.IsDrawing || roiEditor.IsPolygonDrawing)
                error = "请先应用或取消正在修改、绘制的 ROI，再切换图层。";
            else if (!row.CanLocate || !row.Layer.IsVisible || !IsRoiCompatibleWithToolS112(row.Layer, tool))
                error = "该 ROI 缺少几何、已隐藏或属于其他图像；请先在图层菜单检查。";
            else if (ToolMetadata.GetInstanceInputPorts(tool).Where(port => port.DataType == "Image").Any(port =>
                dockInputPortRows.Any(input => input.PortName == port.PortName && input.SelectedSourceKey != GetDockSavedInputSourceKey(tool, port))))
                error = "输入图像来源尚未应用；请先应用或取消输入修改。";
            else
            {
                var context = ResolveModuleInputContext(tool, VmImageContextOption.ModuleInput, null);
                if (!context.HasImage || !LayerMatchesImageS112(row.Layer, context) ||
                    GetImageCoordinateContextS112(context) != ResolveTargetImageContextIdS86(tool))
                    error = "当前模块输入图像不可用或坐标不匹配；请先明确读取对应图像。";
            }
            if (error != null)
            {
                DockRoiSourceStatusText.Text = error;
                DockRoiSourceStatusText.Foreground = System.Windows.Media.Brushes.Firebrick;
                return;
            }
            // Reuse the original selection and handles; no binding, geometry or draft commit.
            bool changedImage = GetImageContextSelectionKeyS112() != VmImageContextOption.ModuleInput;
            SetImageContextSelection(VmImageContextOption.ModuleInput);
            if (changedImage) UpdateImageContextViewport(true);
            roiEditor.Tool = VisionTool.Select;
            RoiLayerList.SelectedItem = row.Layer;
            SelectRoiLayer(row.Layer);
            ShowRoiOverlayCheckBox.IsChecked = true;
            ScheduleRefreshDisplay();
            DockRoiSourceStatusText.Text = row.Layer.Name + " · " + row.Layer.RoleText +
                (row.Layer.IsLocked ? " · 已锁定，只读查看" : " · 已选中，可在图像中拖动控制点");
            DockRoiSourceStatusText.Foreground = System.Windows.Media.Brushes.Teal;
        }

        private bool LayerMatchesImageS112(VmRoiLayer layer, ResolvedImageContext image)
        {
            if (layer == null || string.IsNullOrWhiteSpace(layer.ImageContextId)) return true; // Legacy ROI.
            string current = GetImageCoordinateContextS112(image);
            return !string.IsNullOrWhiteSpace(current) && string.Equals(layer.ImageContextId, current, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRoiCompatibleWithToolS112(VmRoiLayer layer, VmToolInstance tool)
        {
            return layer == null || tool == null || string.IsNullOrWhiteSpace(layer.ImageContextId) ||
                string.Equals(layer.ImageContextId, ResolveTargetImageContextIdS86(tool), StringComparison.OrdinalIgnoreCase);
        }

        private string ValidateRoiImageSourceDraftS112(VmToolInstance tool)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind)) return string.Empty;
            string targetContext = ResolveTargetImageContextIdS86(tool);
            VmInputPortEditorRow imageRow = dockInputPortRows.FirstOrDefault(row =>
                ToolMetadata.GetInstanceInputPorts(tool).Any(port => port.PortName == row.PortName && port.DataType == "Image"));
            if (imageRow != null)
            {
                VmInputSourceOption selected = imageRow.SourceOptions.FirstOrDefault(option =>
                    string.Equals(option.Key, imageRow.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                {
                    VmToolInstance source = selected.IsDefault ? null : flowTools.FirstOrDefault(item =>
                        string.Equals(item.ToolId, selected.SourceToolId, StringComparison.OrdinalIgnoreCase));
                    targetContext = source == null ? "Global.Image" : ResolveOutputCoordinateContextS102(source, selected.SourcePortName);
                }
            }
            VmRoiBindingItem mismatch = dockRoiBindingRows.FirstOrDefault(row => row.IsBound && row.Layer != null &&
                !string.IsNullOrWhiteSpace(row.Layer.ImageContextId) &&
                !string.Equals(row.Layer.ImageContextId, targetContext, StringComparison.OrdinalIgnoreCase));
            return mismatch == null ? string.Empty :
                "ROI“" + mismatch.Name + "”属于另一图像来源或旧校正版本；请取消该 ROI 或重选对应 Image，内参变化后需重新预览并迁移 ROI。";
        }

        private bool LayerMatchesImageS112(VmResultOverlayLayer layer, ResolvedImageContext image)
        {
            if (layer == null) return false;
            VmToolInstance producer = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, layer.ToolId, StringComparison.OrdinalIgnoreCase));
            string layerContext = producer == null ? null : ResolveOutputCoordinateContextS102(producer, layer.PortName);
            string imageContext = GetImageCoordinateContextS112(image);
            return !string.IsNullOrWhiteSpace(imageContext) && string.Equals(layerContext, imageContext, StringComparison.OrdinalIgnoreCase);
        }

        private void LoadMultiImageExampleS112_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("multi-image-source");
        }

        private bool BuildMultiImageExampleS112()
        {
            VmToolInstance sourceA = CreateFlowTool(VmToolKind.ImageSource, "01 图像源 A · 颗粒", true, null);
            VmToolInstance threshold = CreateFlowTool(VmToolKind.ImageThreshold, "02 A · 阈值分割", true, null);
            VmToolInstance contour = CreateFlowTool(VmToolKind.RegionContourMeasurement, "03 A · 轮廓测量", true, null);
            VmToolInstance sourceB = CreateFlowTool(VmToolKind.ImageSource, "04 图像源 B · 工件", true, null);
            VmToolInstance channel = CreateFlowTool(VmToolKind.ImageChannel, "05 B · 灰度转换", true, null);
            VmToolInstance histogram = CreateFlowTool(VmToolKind.GrayHistogram, "06 B · 灰度分布", true, null);
            foreach (VmToolInstance tool in new[] { sourceA, threshold, contour, sourceB, channel, histogram }) flowTools.Add(tool);
            threshold.SetInputBinding("Image", sourceA.ToolId, "Image");
            contour.SetInputBinding("Region", threshold.ToolId, "Region");
            channel.SetInputBinding("Image", sourceB.ToolId, "Image");
            histogram.SetInputBinding("Image", channel.ToolId, "Image");
            threshold.Parameters.ImageThresholdMinGray = 80;
            threshold.Parameters.ImageThresholdMaxGray = 255;
            string pathA = GetHalconExampleImageS78("particle.png");
            string pathB = GetHalconExampleImageS78("mreut.png");
            sourceA.Parameters.LocalImagePath = pathA;
            sourceA.Parameters.LocalImageSerialNumber = "EXAMPLE-SOURCE-A";
            sourceB.Parameters.LocalImagePath = pathB;
            sourceB.Parameters.LocalImageSerialNumber = "EXAMPLE-SOURCE-B";
            return File.Exists(pathA) && File.Exists(pathB);
        }
    }
}
