using System;
using System.Collections.Generic;
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
        private int barCodeModelsCreatedS80;
        private int barCodeModelsClearedS80;

        private void RefreshBarCodeConnectionStatusS80(VmToolInstance tool)
        {
            if (tool == null) return;
            bool ready = HasToolInputImage(tool, "Image", false);
            tool.ConfigurationStatus = ready ? "就绪" : "待连接图像";
            tool.ConnectionStatus = ready ? "Image 已连接 · 有值" : "Image 必须连接前序图像输出";
            tool.ConnectionSummary = GetImageInputSummary(tool) + " → 解码文本 / 数量 / Pose2D / XLD";
        }

        private InspectionRecord RunBarCode1DRecognitionToolS80(VmToolInstance tool, HImage inputImage, HRegion roiRegion)
        {
            if (inputImage == null) throw new InvalidOperationException("一维码识别需要有效的 Image 输入。");
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.BarCode1DRecognition);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            HImage gray = null;
            HObject reduced = null;
            HObject symbolRegions = null;
            HObject contours = null;
            HTuple modelHandle = null;
            HTuple decodedStrings = null;
            HTuple areas = null;
            HTuple rows = null;
            HTuple columns = null;
            HTuple orientations = null;
            bool modelCreated = false;
            try
            {
                int channels = imageService.GetChannelCount(inputImage);
                if (channels <= 0) throw new InvalidOperationException("一维码输入图像为空或通道无效。");
                gray = channels == 1 ? inputImage.CopyImage() : CreateGrayImage(inputImage);
                HObject searchImage = gray;
                if (roiRegion != null)
                {
                    HOperatorSet.ReduceDomain(gray, roiRegion, out reduced);
                    searchImage = reduced;
                }

                HOperatorSet.CreateBarCodeModel(new HTuple(), new HTuple(), out modelHandle);
                modelCreated = true;
                barCodeModelsCreatedS80++;
                HOperatorSet.SetBarCodeParam(modelHandle, "stop_after_result_num", parameters.BarCodeMaxResults);
                HOperatorSet.SetBarCodeParam(modelHandle, "num_scanlines", parameters.BarCodeNumScanlines);
                HOperatorSet.SetBarCodeParam(modelHandle, "min_identical_scanlines", parameters.BarCodeMinIdenticalScanlines);
                HOperatorSet.SetBarCodeParam(modelHandle, "orientation", parameters.BarCodeOrientationDegrees);
                HOperatorSet.SetBarCodeParam(modelHandle, "orientation_tol", parameters.BarCodeOrientationToleranceDegrees);
                HOperatorSet.FindBarCode(searchImage, out symbolRegions, modelHandle, parameters.BarCodeType, out decodedStrings);

                HTuple objectCountTuple;
                HOperatorSet.CountObj(symbolRegions, out objectCountTuple);
                int regionCount = objectCountTuple == null || objectCountTuple.Length == 0 ? 0 : objectCountTuple.I;
                if (objectCountTuple != null) objectCountTuple.Dispose();
                int codeCount = decodedStrings == null ? 0 : decodedStrings.Length;
                List<string> texts = new List<string>();
                for (int index = 0; index < codeCount; index++) texts.Add(decodedStrings[index].S ?? string.Empty);

                double centerRow = 0;
                double centerColumn = 0;
                double angleDegrees = 0;
                string contextId = GetImageCoordinateContextIdS65(tool, "Image");
                if (regionCount > 0)
                {
                    HOperatorSet.AreaCenter(symbolRegions, out areas, out rows, out columns);
                    HOperatorSet.OrientationRegion(symbolRegions, out orientations);
                    HOperatorSet.GenContourRegionXld(symbolRegions, out contours, "border");
                    if (rows != null && rows.Length > 0 && columns != null && columns.Length > 0 && orientations != null && orientations.Length > 0)
                    {
                        centerRow = rows[0].D;
                        centerColumn = columns[0].D;
                        angleDegrees = orientations[0].D * 180.0 / Math.PI;
                        if (!IsFiniteS80(centerRow) || !IsFiniteS80(centerColumn) || !IsFiniteS80(angleDegrees))
                            throw new InvalidOperationException("HALCON 返回了非有限的一维码中心或方向。");
                        tool.SetOutputValue("CenterPoint", VmPoint2D.CreateImagePixel(centerRow, centerColumn, contextId));
                        tool.SetOutputValue("Pose", VmPose2D.CreateImagePixel(centerRow, centerColumn, angleDegrees, contextId));
                    }
                }
                else
                {
                    HOperatorSet.GenEmptyObj(out contours);
                }

                string resultCode = codeCount > 0 ? "OK" : "NG";
                string decodedText = texts.FirstOrDefault() ?? string.Empty;
                string decodedTextSet = string.Join(Environment.NewLine, texts);
                string parameterSummary = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=create/find/clear_bar_code_model, Type={0}, MaxResults={1}, Scanlines={2}, MinIdentical={3}, Orientation={4:0.###}°, Tolerance={5:0.###}°, Channels={6}->1, Domain={7}",
                    parameters.BarCodeType, parameters.BarCodeMaxResults, parameters.BarCodeNumScanlines,
                    parameters.BarCodeMinIdenticalScanlines, parameters.BarCodeOrientationDegrees,
                    parameters.BarCodeOrientationToleranceDegrees, channels,
                    roiRegion == null ? "FullImage" : "ReducedDomain(ROI-Mask)");
                string diagnostics = codeCount > 0
                    ? string.Format(CultureInfo.InvariantCulture, "Decoded={0}, Regions={1}", codeCount, regionCount)
                    : "未识别到一维码；请检查制式、方向、扫描线、清晰度和运行域。";

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(contours, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("DecodedText", decodedText);
                tool.SetOutputValue("DecodedTexts", decodedTextSet);
                tool.SetOutputValue("CodeCount", (double)codeCount);
                tool.SetOutputValue("CenterRow", centerRow);
                tool.SetOutputValue("CenterColumn", centerColumn);
                tool.SetOutputValue("Angle", angleDegrees);
                tool.SetOutputValue("CoordinateContext", VmPoint2D.ImagePixelContext + ":" + contextId);
                tool.SetOutputValue("Parameters", parameterSummary);
                tool.SetOutputValue("Diagnostics", diagnostics);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayContours(contours, tool, "XLD", regionCount, resultCode, codeCount);

                string message = codeCount > 0
                    ? string.Format(CultureInfo.InvariantCulture, "HALCON 一维码识别完成：Type={0}，Count={1}，Text={2}，Center=({3:0.###},{4:0.###})，Angle={5:0.###}°。", parameters.BarCodeType, codeCount, decodedText, centerRow, centerColumn, angleDegrees)
                    : "HALCON 一维码识别未检出结果：" + parameterSummary;
                InspectionRecord record = CreateRecord("BarCode1DRecognition", resultCode, codeCount, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (modelCreated && modelHandle != null && modelHandle.Length > 0)
                {
                    try
                    {
                        HOperatorSet.ClearBarCodeModel(modelHandle);
                        barCodeModelsClearedS80++;
                    }
                    catch (Exception clearError)
                    {
                        LogInfo("一维码模型释放失败：" + clearError.Message);
                    }
                }
                if (areas != null) areas.Dispose();
                if (rows != null) rows.Dispose();
                if (columns != null) columns.Dispose();
                if (orientations != null) orientations.Dispose();
                if (decodedStrings != null) decodedStrings.Dispose();
                if (modelHandle != null) modelHandle.Dispose();
                DisposeObject(contours);
                DisposeObject(symbolRegions);
                DisposeObject(reduced);
                if (gray != null) gray.Dispose();
            }
        }

        private static bool IsFiniteS80(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
