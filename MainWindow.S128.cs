using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private RoiData cameraRoiPreviewS128;
        private RoiData cameraRoiSourcePreviewS128;
        private RoiData cameraRoiSourceReferenceS128;
        private VmRoiLayer cameraRoiSourceLayerS128;
        private VmImageSnapshot cameraRoiImageSnapshotS128;
        private string cameraRoiToolIdS128, cameraRoiInputContextS128, cameraRoiOutputContextS128;

        private string GetCameraOutputContextS128(VmToolInstance tool, VmCameraIntrinsicsS126 camera, string mode, string inputContext)
        {
            using (SHA256 sha = SHA256.Create())
            {
                string revision = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(
                    camera.ToEditText() + "|" + mode + "|" + inputContext)), 0, 6).Replace("-", string.Empty);
                return GetCoordinateIdentityBaseS102(tool) + ".Image:" + revision;
            }
        }

        private int PublishCameraGeometryS128(VmToolInstance tool, HTuple cameraIn, HTuple cameraOut,
            string inputContext, string outputContext, int inputWidth, int inputHeight, int outputWidth, int outputHeight)
        {
            int mappedCount = 0;
            var pointBinding = tool.GetInputBinding("Point");
            if (pointBinding != null)
            {
                string error = GetInputBindingConfigurationError(tool, pointBinding);
                if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
                var source = GetInputSourceTool(pointBinding);
                object raw;
                var point = source != null && source.ResultCode == "OK" && source.TryGetOutputValue(pointBinding.SourcePortName, out raw) ? raw as VmPoint2D : null;
                if (point == null || !string.IsNullOrWhiteSpace(point.GetValidationError()) ||
                    !string.Equals(point.CoordinateContextId, inputContext, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("迁移点必须来自本周期有效的原图 Point2D，且与 Image 属于同一图像链。");
                double[] rows, columns;
                VmCameraGeometryS128.MapPoints(new[] { point.Row }, new[] { point.Column }, cameraIn, cameraOut,
                    inputWidth, inputHeight, outputWidth, outputHeight, out rows, out columns);
                tool.SetOutputValue("Point", VmPoint2D.CreateImagePixel(rows[0], columns[0], outputContext));
                tool.SetOutputCoordinateContext("Point", outputContext);
                HObject marker = null;
                try
                {
                    HOperatorSet.GenCrossContourXld(out marker, rows[0], columns[0], 10.0, Math.PI / 4);
                    tool.SetOutputValue("PointMarker", VmXldSnapshot.Create(marker, tool.ToolId, tool.InstanceName, "PointMarker"));
                    tool.SetOutputCoordinateContext("PointMarker", outputContext);
                }
                finally { DisposeObject(marker); }
                mappedCount++;
            }
            var contourBinding = tool.GetInputBinding("XLD");
            if (contourBinding != null)
            {
                var source = GetInputSourceTool(contourBinding);
                string context = ResolveOutputCoordinateContextS102(source, contourBinding.SourcePortName);
                if (IsPendingCoordinateContextS102(context) || !string.Equals(context, inputContext, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("迁移轮廓与 Image 不属于同一原图坐标链，不能用另一相机或旧校正版本的轮廓。");
                using (HObject input = CreateToolInputXld(tool, "XLD"))
                using (HObject output = VmCameraGeometryS128.MapContours(input, cameraIn, cameraOut,
                    inputWidth, inputHeight, outputWidth, outputHeight))
                {
                    var snapshot = VmXldSnapshot.Create(output, tool.ToolId, tool.InstanceName, "XLD");
                    tool.SetOutputValue("XLD", snapshot);
                    tool.SetOutputCoordinateContext("XLD", outputContext);
                    mappedCount += snapshot.ObjectCount;
                }
            }
            tool.SetOutputValue("MappedObjectCount", (double)mappedCount);
            return mappedCount;
        }

        private void ClearCameraRoiPreviewS128()
        {
            if (cameraRoiPreviewS128 != null) cameraRoiPreviewS128.Dispose();
            if (cameraRoiSourcePreviewS128 != null) cameraRoiSourcePreviewS128.Dispose();
            cameraRoiPreviewS128 = cameraRoiSourcePreviewS128 = cameraRoiSourceReferenceS128 = null;
            cameraRoiSourceLayerS128 = null; cameraRoiImageSnapshotS128 = null;
            cameraRoiToolIdS128 = cameraRoiInputContextS128 = cameraRoiOutputContextS128 = null;
            if (DockCameraKeepRoiS128 != null) DockCameraKeepRoiS128.IsEnabled = false;
        }

        private bool IsCameraRoiPreviewCurrentS128()
        {
            var tool = dockEditingTool;
            if (cameraRoiPreviewS128 == null || tool == null || tool.ToolId != cameraRoiToolIdS128 || dockDraftDirty ||
                tool.ResultCode != "OK" || tool.Parameters == null || tool.Parameters.CameraIntrinsicsS126 == null ||
                cameraRoiSourceLayerS128 == null || !roiLayers.Contains(cameraRoiSourceLayerS128) ||
                !ReferenceEquals(cameraRoiSourceReferenceS128, cameraRoiSourceLayerS128.Geometry) ||
                !string.Equals(cameraRoiSourceLayerS128.ImageContextId, cameraRoiInputContextS128, StringComparison.OrdinalIgnoreCase) ||
                cameraRoiImageSnapshotS128 == null || cameraRoiImageSnapshotS128.IsDisposed ||
                !ReferenceEquals(cameraRoiImageSnapshotS128, GetImageSnapshot(tool, "Image"))) return false;
            string inputContext = ResolveTargetImageContextIdS86(tool);
            return inputContext == cameraRoiInputContextS128 && cameraRoiOutputContextS128 ==
                GetCameraOutputContextS128(tool, tool.Parameters.CameraIntrinsicsS126, tool.Parameters.CameraRectifyModeS126, inputContext);
        }

        private void RefreshCameraMappingStateS128()
        {
            if (DockCameraMappingStateS128 == null || dockEditingTool == null || dockEditingTool.Kind != VmToolKind.CameraDistortionCorrection) return;
            if (cameraRoiPreviewS128 != null && !IsCameraRoiPreviewCurrentS128()) ClearCameraRoiPreviewS128();
            DockCameraKeepRoiS128.IsEnabled = IsCameraRoiPreviewCurrentS128() && !isFlowExecutionActive && !isContinuousRunning &&
                !isImageQueueBatchRunning && !IsCurrentToolContinuousTrialRunningS87;
            if (cameraRoiPreviewS128 == null)
                DockCameraMappingStateS128.Text = "点/轮廓：在“输入”页选择原图来源，运行后输出校正坐标。ROI：应用并运行后选择原图 ROI，再预览迁移。";
        }

        private void DockCameraPreviewRoiS128_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("预览 ROI 畸变迁移", delegate
            {
                ClearCameraRoiPreviewS128();
                var tool = dockEditingTool;
                if (tool == null || tool.Kind != VmToolKind.CameraDistortionCorrection || dockDraftDirty || tool.ResultCode != "OK")
                    throw new InvalidOperationException("请先应用参数并运行镜头畸变校正，再预览 ROI 迁移。");
                if (historicalImageMode || isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
                    throw new InvalidOperationException("请在停止运行后的当前方案中预览迁移。");
                var source = RoiLayerList.SelectedItem as VmRoiLayer;
                string inputContext = ResolveTargetImageContextIdS86(tool);
                if (source == null || source.Geometry == null || string.IsNullOrWhiteSpace(source.ImageContextId) ||
                    !string.Equals(source.ImageContextId, inputContext, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("请选择属于该模块原图坐标链的 ROI；没有明确图像身份的旧 ROI 需在原图重新绘制。");
                VmImageSnapshot output = GetImageSnapshot(tool, "Image");
                var parameters = tool.Parameters;
                string expected = GetCameraOutputContextS128(tool, parameters.CameraIntrinsicsS126, parameters.CameraRectifyModeS126, inputContext);
                if (output == null || output.IsDisposed || ResolveOutputCoordinateContextS102(tool, "Image") != expected)
                    throw new InvalidOperationException("校正图与当前内参版本不一致，请重新运行。");
                HTuple cameraOut = null;
                using (HTuple cameraIn = parameters.CameraIntrinsicsS126.CreateHalconTuple())
                using (HImage input = CreateToolInputImage(tool, "Image", false))
                try
                {
                    var camera = parameters.CameraIntrinsicsS126;
                    if (GetImageWidth(input) != camera.Width || GetImageHeight(input) != camera.Height)
                        throw new InvalidOperationException("当前原图尺寸与内参不一致，请重新运行。");
                    HOperatorSet.ChangeRadialDistortionCamPar(parameters.CameraRectifyModeS126, cameraIn, 0.0, out cameraOut);
                    cameraRoiPreviewS128 = VmCameraGeometryS128.MapRoi(source.Geometry, cameraIn, cameraOut,
                        camera.Width, camera.Height, output.Width, output.Height);
                    cameraRoiSourcePreviewS128 = source.Geometry.Clone();
                    cameraRoiSourceReferenceS128 = source.Geometry; cameraRoiSourceLayerS128 = source;
                    cameraRoiToolIdS128 = tool.ToolId; cameraRoiImageSnapshotS128 = output;
                    cameraRoiInputContextS128 = inputContext; cameraRoiOutputContextS128 = expected;
                }
                finally { if (cameraOut != null) cameraOut.Dispose(); }
                SetImageContextSelection(VmImageContextOption.ModuleOutput);
                SetDualImageMode(true);
                DualInputPortComboBox.SelectedValue = "Image";
                UpdateComparisonImageViewport(true);
                DockCameraMappingStateS128.Text = source.Name + " → " + cameraRoiPreviewS128.ShapeDisplayText +
                    " · 左原图 / 右校正图 · 黄色为迁移预览，点击“保留为新 ROI”写入方案";
                DockCameraKeepRoiS128.IsEnabled = true;
                ScheduleRefreshDisplay();
            });
        }

        private void DockCameraKeepRoiS128_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("保留畸变迁移 ROI");
            if (editLeaseS137 < 0) return;
            RunUiAction("保留迁移 ROI", delegate
            {
                if (!IsCameraRoiPreviewCurrentS128() || historicalImageMode || isFlowExecutionActive || isContinuousRunning ||
                    isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
                    throw new InvalidOperationException("预览已失效或正在运行，请停止并重新预览。");
                FlowEditSnapshot before = CaptureFlowEditSnapshot();
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                var source = cameraRoiSourceLayerS128;
                VmRoiLayer copy = AddRoiLayer(cameraRoiPreviewS128, CreateUniqueRoiCopyName(source.Name + " · 校正"), null);
                copy.ImageContextId = cameraRoiOutputContextS128;
                copy.IsMask = source.IsMask; copy.IsEnabled = source.IsEnabled; copy.IsVisible = true;
                copy.DisplayColor = source.DisplayColor; copy.DisplayLineWidth = source.DisplayLineWidth;
                copy.DisplayOpacity = source.DisplayOpacity; copy.ViewportScope = VmLayerViewportScope.Both;
                ClearCameraRoiPreviewS128();
                RefreshRoiLayerBindingSummaries(); RefreshUnifiedLayers();
                CommitFlowEditHistory("畸变迁移 ROI：" + copy.Name, before);
                ScheduleRecipeStateCheck(); RefreshUiState(); ScheduleRefreshDisplay();
                DockCameraMappingStateS128.Text = "已保留 " + copy.Name + "；可在下游工具的 ROI 页选择，原 ROI 保持独立。";
                LogInfo(DockCameraMappingStateS128.Text);
            });
        }

        private void DockCameraClearPreviewS128_Click(object sender, RoutedEventArgs e)
        { ClearCameraRoiPreviewS128(); RefreshCameraMappingStateS128(); ScheduleRefreshDisplay(); }

        private void DrawCameraRoiPreviewS128(HWindow window, ResolvedImageContext image)
        {
            if (!showRoiOverlay || !IsCameraRoiPreviewCurrentS128()) return;
            string context = GetImageCoordinateContextS112(image);
            RoiData geometry = context == cameraRoiInputContextS128 ? cameraRoiSourcePreviewS128 :
                context == cameraRoiOutputContextS128 ? cameraRoiPreviewS128 : null;
            if (geometry != null) overlayRenderer.DrawRoiLayer(window, geometry, "yellow", 2, false, false, cameraRoiSourceLayerS128.IsMask, 100);
        }
    }
}
