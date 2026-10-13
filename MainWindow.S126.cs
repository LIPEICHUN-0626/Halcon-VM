using System;
using System.Globalization;
using System.IO;
using System.Windows;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private string cameraImportedEditTextS126;
        private string cameraImportedFileNameS126;
        private VmCameraIntrinsicsS126 cameraSolvedDraftS127;

        private void LoadCameraDraftS126(VmToolParameterData parameters)
        {
            ClearCameraRoiPreviewS128();
            var camera = parameters.CameraIntrinsicsS126;
            cameraImportedEditTextS126 = camera == null ? string.Empty : camera.ToEditText();
            cameraImportedFileNameS126 = camera == null ? null : camera.ImportedFileName;
            cameraSolvedDraftS127 = camera != null && camera.CalibrationSampleCount > 0 ? camera.Clone() : null;
            DockCameraParamsTextS126.Text = cameraImportedEditTextS126;
            DockCameraModeS126.SelectedIndex = parameters.CameraRectifyModeS126 == "fullsize" ? 1 : 0;
            DockCameraSummaryS126.Text = camera == null ? "尚未提供内参 · 先连接 Image，再导入 HALCON .dat 或填入已知参数" : camera.ToSummary();
            LoadCameraBoardDraftS127(parameters);
        }

        private bool ReadCameraDraftS126(VmToolParameterData parameters)
        {
            try
            {
                string edited = DockCameraParamsTextS126.Text ?? string.Empty;
                string source = string.Equals(edited, cameraImportedEditTextS126, StringComparison.Ordinal)
                    ? cameraImportedFileNameS126 : null;
                parameters.CameraIntrinsicsS126 = cameraSolvedDraftS127 != null &&
                    string.Equals(edited, cameraSolvedDraftS127.ToEditText(), StringComparison.Ordinal)
                    ? cameraSolvedDraftS127.Clone() : VmCameraIntrinsicsS126.Parse(edited, source);
                parameters.CameraRectifyModeS126 = DockCameraModeS126.SelectedIndex == 1 ? "fullsize" : "fixed";
                ReadCameraBoardDraftS127(parameters);
                return true;
            }
            catch (InvalidOperationException ex) { return FailDockDraft(ex.Message); }
        }

        private void DockCameraImportS126_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("导入相机内参");
            if (editLeaseS137 < 0) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.CameraDistortionCorrection) return;
            var dialog = new OpenFileDialog { Title = "导入 HALCON 相机内参", Filter = "HALCON 相机参数 (*.dat)|*.dat|所有文件 (*.*)|*.*",
                CheckFileExists = true, Multiselect = false };
            if (dialog.ShowDialog(this) != true) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            HTuple tuple = null;
            try
            {
                HOperatorSet.ReadCamPar(dialog.FileName, out tuple);
                var camera = VmCameraIntrinsicsS126.FromHalconTuple(tuple, Path.GetFileName(dialog.FileName));
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                cameraImportedEditTextS126 = camera.ToEditText();
                cameraImportedFileNameS126 = camera.ImportedFileName;
                cameraSolvedDraftS127 = null;
                DockCameraParamsTextS126.Text = cameraImportedEditTextS126;
                DockCameraSummaryS126.Text = camera.ToSummary() + " · 点击参数窗“应用”后保存到方案";
                DockDraftValue_Changed(sender, e);
            }
            catch (Exception ex)
            {
                DockCameraSummaryS126.Text = "导入失败：" + ex.Message;
            }
            finally { if (tuple != null) tuple.Dispose(); }
        }

        private InspectionRecord RunCameraCorrectionS126(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string error = parameters.Validate(VmToolKind.CameraDistortionCorrection);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            if (tool.GetInputBinding("Image") == null)
                throw new InvalidOperationException("镜头畸变校正必须在输入页连接明确的 Image 来源，不能使用隐式全局图像。");
            if (inputImage == null || !inputImage.IsInitialized()) throw new InvalidOperationException("校正输入图像为空。" );
            VmCameraIntrinsicsS126 camera = parameters.CameraIntrinsicsS126;
            int inputWidth = GetImageWidth(inputImage), inputHeight = GetImageHeight(inputImage);
            int inputChannels = imageService.GetChannelCount(inputImage);
            if (inputWidth != camera.Width || inputHeight != camera.Height)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "内参适用 {0}×{1}，当前 Image 为 {2}×{3}；请重选正确来源或重新导入内参。", camera.Width, camera.Height, inputWidth, inputHeight));
            HTuple types = null, camIn = null, camOut = null;
            HObject region = null, corrected = null;
            try
            {
                HOperatorSet.GetImageType(inputImage, out types);
                for (int i = 0; i < types.Length; i++)
                    if (types[i].S != "byte" && types[i].S != "uint2" && types[i].S != "real")
                        throw new InvalidOperationException("HALCON 畸变校正当前仅接受 byte、uint2 或 real 图像；请先转换图像类型。" );
                camIn = camera.CreateHalconTuple();
                HOperatorSet.ChangeRadialDistortionCamPar(parameters.CameraRectifyModeS126, camIn, 0.0, out camOut);
                HOperatorSet.GenEmptyObj(out region);
                using (HImage full = inputImage.FullDomain())
                    HOperatorSet.ChangeRadialDistortionImage(full, region, out corrected, camIn, camOut);
                using (HImage outputImage = new HImage(corrected))
                {
                    int width = GetImageWidth(outputImage), height = GetImageHeight(outputImage);
                    int channels = imageService.GetChannelCount(outputImage);
                    if (width < 2 || height < 2 || width > 16384 || height > 16384 ||
                        (long)width * height > 67108864L || channels != inputChannels)
                        throw new InvalidOperationException("畸变校正输出尺寸或通道无效，已阻止发布。" );
                    string sourcePath, serialNumber;
                    GetToolImageMetadata(tool, out sourcePath, out serialNumber);
                    string inputContext = ResolveOutputCoordinateContextS102(GetInputSourceTool(tool.GetInputBinding("Image")),
                        tool.GetInputBinding("Image").SourcePortName);
                    if (string.IsNullOrWhiteSpace(inputContext) || IsPendingCoordinateContextS102(inputContext))
                        throw new InvalidOperationException("输入图像坐标来源尚未确定，不能生成可复用的校正图。" );
                    string outputContext = GetCameraOutputContextS128(tool, camera, parameters.CameraRectifyModeS126, inputContext);
                    int mappedCount = PublishCameraGeometryS128(tool, camIn, camOut, inputContext, outputContext,
                        inputWidth, inputHeight, width, height);
                    tool.SetOutputValue("Image", VmImageSnapshot.Create(outputImage, tool.ToolId, tool.InstanceName, "Image", sourcePath, serialNumber));
                    tool.SetOutputCoordinateContext("Image", outputContext);
                    tool.SetOutputValue("Width", (double)width); tool.SetOutputValue("Height", (double)height);
                    tool.SetOutputValue("Channels", (double)channels);
                    tool.SetOutputValue("CameraModel", VmCameraIntrinsicsS126.Model);
                    tool.SetOutputValue("CalibrationSource", camera.ImportedFileName ?? "方案内手动内参");
                    tool.SetOutputValue("RectifyMode", parameters.CameraRectifyModeS126);
                    tool.SetOutputValue("InputImageContext", inputContext);
                    tool.SetOutputValue("OutputImageContext", outputContext);
                    if (camera.CalibrationSampleCount > 0)
                    {
                        tool.SetOutputValue("CalibrationRmsPx", camera.CalibrationRmsPx);
                        tool.SetOutputValue("CalibrationSampleCount", (double)camera.CalibrationSampleCount);
                    }
                    tool.SetOutputValue("Parameters", camera.ToSummary());
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "HALCON 镜头畸变校正：{0}×{1} → {2}×{3}，{4}，{5}；输出是独立像素坐标链",
                        inputWidth, inputHeight, width, height, parameters.CameraRectifyModeS126, camera.ToSummary());
                    if (mappedCount > 0) message += "；已迁移 " + mappedCount.ToString(CultureInfo.InvariantCulture) + " 个点/轮廓";
                    var record = CreateRecord("CameraDistortionCorrection", "OK", width * (double)height, message, tool);
                    ApplyRecordImage(record, tool, outputImage);
                    resultStore.Add(record);
                    LogInfo(tool.InstanceName + " 完成：" + message);
                    return record;
                }
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
            finally
            {
                if (types != null) types.Dispose(); if (camIn != null) camIn.Dispose(); if (camOut != null) camOut.Dispose();
                DisposeObject(region); DisposeObject(corrected);
            }
        }
    }
}
