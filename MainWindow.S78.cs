using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private int exampleProgramLoadCountS78;
        private string lastExampleProgramKeyS78 = string.Empty;

        private void OpenChineseHelpMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            OpenHelpCenterS78(null);
        }

        private void OpenCurrentModuleHelpMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            VmToolInstance tool = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            OpenHelpCenterS78(tool == null ? (VmToolKind?)null : tool.Kind);
        }

        private void OpenHelpCenterS78(VmToolKind? kind)
        {
            HelpCenterWindow window = new HelpCenterWindow(kind) { Owner = this };
            window.Show();
            HeaderStatusText.Text = kind.HasValue
                ? "已打开当前模块帮助：" + ToolMetadata.GetDisplayName(kind.Value)
                : "已打开中文版帮助中心。";
        }

        private void LoadBasicSegmentationExampleMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("basic-segmentation");
        }

        private void LoadDataCodeExampleMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("data-code");
        }

        private void LoadContourMeasurementExampleMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("contour-measurement");
        }

        private void LoadExampleProgramS78(string key)
        {
            RunUiAction("打开示例程序", delegate
            {
                if (!ConfirmDockDraftChanges("打开示例程序") || !ConfirmUnsavedRecipeChanges("打开示例程序")) return;
                if (centralModuleConfigurationOpenS77 && !CloseCentralModuleConfigurationS77()) return;

                StopPlayback();
                StopContinuousRun();
                recipeTrackingSuspended = true;
                bool sampleAvailable;
                string displayName;
                try
                {
                    currentRecipePath = null;
                    ApplyRecipe(new VisionRecipe
                    {
                        Name = "示例程序",
                        EnableShapeMatch = false,
                        EnableBlob = false,
                        EnableGrayStat = false,
                        EnableEdgeMeasure = false,
                        EnableHDevelop = false,
                        ToolFlow = new List<ToolFlowRecipeItem>()
                    });
                    sampleAvailable = BuildExampleFlowS78(key, out displayName);
                    RecipeNameEditTextBox.Text = "示例 · " + displayName;
                    lastExampleProgramKeyS78 = key;
                    exampleProgramLoadCountS78++;
                    ResetFlowEditHistory();
                }
                finally
                {
                    recipeTrackingSuspended = false;
                }

                savedRecipeFingerprint = "__EXAMPLE_REQUIRES_SAVE__";
                lastRecipeSavedAt = null;
                SetRecipeDirtyState(true, "示例程序尚未另存为正式方案");
                ScheduleRecipeStateCheck();
                if (flowTools.Count > 0)
                {
                    FlowToolList.SelectedItem = flowTools[0];
                    RefreshFlowGraphLayout();
                    FitFlowButton_Click(null, null);
                }
                RefreshUiState();
                HeaderStatusText.Text = sampleAvailable
                    ? "示例“" + displayName + "”已装载；不会自动运行，请从上到下查看模块后点击“运行流程”。"
                    : "示例“" + displayName + "”已装载，但未找到本机 HALCON 示例图像；请在首个图像源中选择自己的图片。";
                LogInfo(HeaderStatusText.Text);
            });
        }

        private bool BuildExampleFlowS78(string key, out string displayName)
        {
            if (string.Equals(key, "data-code", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "二维码识别与结果输出";
                return BuildDataCodeExampleS78();
            }
            if (string.Equals(key, "contour-measurement", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "亚像素轮廓与直线拟合";
                return BuildContourMeasurementExampleS78();
            }
            displayName = "基础分割与区域测量";
            return BuildBasicSegmentationExampleS78();
        }

        private bool BuildBasicSegmentationExampleS78()
        {
            VmToolInstance source = CreateFlowTool(VmToolKind.ImageSource, "01 示例图像", true, null);
            VmToolInstance channel = CreateFlowTool(VmToolKind.ImageChannel, "02 转换灰度", true, null);
            VmToolInstance filter = CreateFlowTool(VmToolKind.ImageFilter, "03 图像滤波", true, null);
            VmToolInstance threshold = CreateFlowTool(VmToolKind.ImageThreshold, "04 阈值分割", true, null);
            VmToolInstance morphology = CreateFlowTool(VmToolKind.RegionMorphology, "05 Region 形态学", true, null);
            VmToolInstance feature = CreateFlowTool(VmToolKind.RegionFeatureFilter, "06 面积筛选", true, null);
            VmToolInstance contour = CreateFlowTool(VmToolKind.RegionContourMeasurement, "07 轮廓测量", true, null);
            VmToolInstance judge = CreateFlowTool(VmToolKind.NumericJudge, "08 轮廓长度判定", true, null);
            foreach (VmToolInstance tool in new[] { source, channel, filter, threshold, morphology, feature, contour, judge }) flowTools.Add(tool);
            channel.SetInputBinding("Image", source.ToolId, "Image");
            filter.SetInputBinding("Image", channel.ToolId, "Image");
            threshold.SetInputBinding("Image", filter.ToolId, "Image");
            morphology.SetInputBinding("Region", threshold.ToolId, "Region");
            feature.SetInputBinding("Region", morphology.ToolId, "Region");
            contour.SetInputBinding("Region", feature.ToolId, "Region");
            judge.SetInputBinding("Value", contour.ToolId, "TotalLength");
            judge.InputToolId = contour.ToolId; judge.InputPortName = "TotalLength"; judge.NumericLowerLimit = 1; judge.NumericUpperLimit = 1000000000;
            threshold.Parameters.ImageThresholdMinGray = 80; threshold.Parameters.ImageThresholdMaxGray = 255;
            feature.Parameters.RegionFeature = VmRegionFeature.Area; feature.Parameters.RegionFeatureMin = 20; feature.Parameters.RegionFeatureMax = 1000000;
            string path = GetHalconExampleImageS78("particle.png"); source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "EXAMPLE-SEGMENT";
            return File.Exists(path);
        }

        private bool BuildDataCodeExampleS78()
        {
            VmToolInstance source = CreateFlowTool(VmToolKind.ImageSource, "01 二维码图像", true, null);
            VmToolInstance reader = CreateFlowTool(VmToolKind.DataCode2DRecognition, "02 二维代码识别", true, null);
            VmToolInstance text = CreateFlowTool(VmToolKind.TextAssembly, "03 解码文本", true, null);
            VmToolInstance judge = CreateFlowTool(VmToolKind.NumericJudge, "04 代码数量判定", true, null);
            foreach (VmToolInstance tool in new[] { source, reader, text, judge }) flowTools.Add(tool);
            reader.SetInputBinding("Image", source.ToolId, "Image");
            text.SetInputBinding("String", reader.ToolId, "DecodedText");
            judge.SetInputBinding("Value", reader.ToolId, "CodeCount");
            judge.InputToolId = reader.ToolId; judge.InputPortName = "CodeCount"; judge.NumericLowerLimit = 1; judge.NumericUpperLimit = 8;
            reader.Parameters.DataCodeType = VmDataCode2DType.QrCode;
            reader.Parameters.DataCodeRecognitionProfile = VmDataCodeRecognitionProfile.Standard;
            reader.Parameters.DataCodePolarity = VmDataCodePolarity.DarkOnLight;
            reader.Parameters.DataCodeMaxResults = 8; reader.Parameters.DataCodeTimeoutMilliseconds = 3000;
            string path = GetHalconExampleImageS78(Path.Combine("datacode", "qrcode", "qr_generated.png")); source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "EXAMPLE-QR";
            return File.Exists(path);
        }

        private bool BuildContourMeasurementExampleS78()
        {
            VmToolInstance source = CreateFlowTool(VmToolKind.ImageSource, "01 轮廓图像", true, null);
            VmToolInstance channel = CreateFlowTool(VmToolKind.ImageChannel, "02 转换灰度", true, null);
            VmToolInstance edge = CreateFlowTool(VmToolKind.SubpixelEdgeExtraction, "03 亚像素边缘", true, null);
            VmToolInstance smooth = CreateFlowTool(VmToolKind.XldContourSmoothing, "04 轮廓平滑", true, null);
            VmToolInstance fit = CreateFlowTool(VmToolKind.XldLineFitting, "05 直线拟合", true, null);
            VmToolInstance judge = CreateFlowTool(VmToolKind.NumericJudge, "06 残差判定", true, null);
            foreach (VmToolInstance tool in new[] { source, channel, edge, smooth, fit, judge }) flowTools.Add(tool);
            channel.SetInputBinding("Image", source.ToolId, "Image");
            edge.SetInputBinding("Image", channel.ToolId, "Image");
            smooth.SetInputBinding("XLD", edge.ToolId, "XLD");
            fit.SetInputBinding("XLD", smooth.ToolId, "XLD");
            judge.SetInputBinding("Value", fit.ToolId, "MaxResidual");
            judge.InputToolId = fit.ToolId; judge.InputPortName = "MaxResidual"; judge.NumericLowerLimit = 0; judge.NumericUpperLimit = 20;
            edge.Parameters.SubpixelEdgeFilter = VmSubpixelEdgeFilter.Canny; edge.Parameters.SubpixelEdgeAlpha = 1; edge.Parameters.SubpixelEdgeLow = 15; edge.Parameters.SubpixelEdgeHigh = 35;
            fit.Parameters.XldLineFitAlgorithm = VmXldLineFitAlgorithm.Tukey;
            string path = GetHalconExampleImageS78("mreut.png"); source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "EXAMPLE-XLD";
            return File.Exists(path);
        }

        private static string GetHalconExampleImageS78(string relativePath)
        {
            string publicDocuments = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            return Path.Combine(publicDocuments, "MVTec", "HALCON-20.11-Progress", "examples", "images", relativePath);
        }
    }
}
