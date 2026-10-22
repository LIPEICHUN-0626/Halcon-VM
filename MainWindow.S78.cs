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
        private void ExportRecipeImagesS95_Click(object sender, RoutedEventArgs e)
        {
            OpenEngineeringPackageS139();
        }
        private string lastExampleProgramKeyS78 = string.Empty;

        private void OpenChineseHelpMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            OpenHelpCenterS78(null);
        }

        private void OpenCurrentModuleHelpMenuItem_ClickS78(object sender, RoutedEventArgs e)
        {
            VmToolInstance tool = centralModuleConfigurationOpenS77
                ? dockEditingTool
                : (FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance);
            OpenHelpCenterS78(tool == null ? (VmToolKind?)null : tool.Kind);
        }

        private HelpCenterWindow helpCenterWindowS78;

        private void OpenHelpCenterS78(VmToolKind? kind)
        {
            if (helpCenterWindowS78 == null)
            {
                var window = new HelpCenterWindow(kind) { Owner = this, ShowActivated = ShowActivated };
                if (!ShowActivated) { window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = Left; window.Top = Top; }
                helpCenterWindowS78 = window;
                window.Closed += delegate { if (ReferenceEquals(helpCenterWindowS78, window)) helpCenterWindowS78 = null; };
                window.Show();
            }
            else
            {
                helpCenterWindowS78.SelectToolTopicUI10(kind);
                if (ShowActivated) helpCenterWindowS78.Activate();
            }
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
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("装载学习示例");
            if (editLeaseS137 < 0) return;
            RunUiAction("打开示例程序", delegate
            {
                if (!CanChangeRecipeS134()) return;
                if (!ConfirmDockDraftChanges("打开示例程序") || !ConfirmUnsavedRecipeChanges("打开示例程序")) return;
                if (centralModuleConfigurationOpenS77 && !CloseCentralModuleConfigurationS77()) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137) || !CanChangeRecipeS134()) return;

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
                    ? IsLearningProjectKeyS146(key)
                        ? "示例“" + displayName + "”已装载；请按学习步骤准备图像、参数及所需模型/标定，再主动运行。"
                        : "示例“" + displayName + "”已装载；不会自动运行，请从上到下查看模块后点击“运行流程”。"
                    : "示例“" + displayName + "”已装载，但未找到本机 HALCON 示例图像；请在首个图像源中选择自己的图片。";
                LogInfo(HeaderStatusText.Text);
                CompleteLearningProjectLoadS146(key, displayName);
            });
        }

        private bool BuildExampleFlowS78(string key, out string displayName)
        {
            if (key == "project-object-production")
            { displayName = "对象检测、条件判定与生产查看"; return BuildObjectProductionLearningProjectS146(); }
            if (key == "project-alignment-line")
            { displayName = "明确定位、固定参考对齐与找边判定"; return BuildAlignmentLineLearningProjectS146(); }
            if (key == "project-world-distance")
            { displayName = "真实边缘点、平面标定与世界测量"; return BuildWorldDistanceLearningProjectS146(); }
            if (string.Equals(key, "template-multi-targets", StringComparison.OrdinalIgnoreCase))
            { displayName = "模板多目标、明确选取与类型化复用"; return BuildTemplateTargetsExampleS145(); }
            if (string.Equals(key, "connected-objects", StringComparison.OrdinalIgnoreCase))
            { displayName = "连通对象分析、面积数组和明确选取"; return BuildObjectExampleS144(); }
            if (string.Equals(key, "image-edge-line", StringComparison.OrdinalIgnoreCase))
            { displayName = "图像直边采样与像素直线"; return BuildEdgeLineExampleS143(); }
            if (string.Equals(key, "ocr-character-validation", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "OCR 逐字结果与文本校验";
                return BuildOcrValidationExampleS133();
            }
            if (string.Equals(key, "pose-image-alignment", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "模板定位与图像对齐";
                return BuildAlignmentExampleS132();
            }
            if (string.Equals(key, "ncc-template", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "NCC 灰度模板制作与定位";
                return BuildNccExampleS131();
            }
            if (string.Equals(key, "variation-surface", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "良品学习与表面缺陷";
                return BuildVariationExampleS130();
            }
            if (string.Equals(key, "color-coverage", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "颜色取样与覆盖比例";
                return BuildColorExampleS129();
            }
            if (string.Equals(key, "image-replay-acquisition", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "离线采集与逐帧亮度分析";
                return BuildImageReplayExampleS114();
            }
            if (string.Equals(key, "paired-image-difference", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "双源配对与绝对差分";
                return BuildPairedDifferenceExampleS113();
            }
            if (string.Equals(key, "multi-image-source", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "双图像源独立检测";
                return BuildMultiImageExampleS112();
            }
            if (string.Equals(key, "bounded-subflow-loop", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "子流程逐项补偿与固定编号（无需图像）";
                return BuildSubflowLoopExampleS111();
            }
            if (string.Equals(key, "structured-data", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "结构化报文与测量结果（无需图像）";
                return BuildJsonExampleS110();
            }
            if (string.Equals(key, "variable-arrays", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "方案数组缓存与读写（无需图像）";
                return BuildVariableArraysExampleS109();
            }
            if (string.Equals(key, "array-reorder", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "数组整理与标签关联（无需图像）";
                return BuildArrayReorderExampleS108();
            }
            if (string.Equals(key, "script-arrays", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "脚本数组逐项补偿（无需图像）";
                return BuildScriptArrayExampleS107();
            }
            if (string.Equals(key, "numeric-expression", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "测量偏差与数值公式（无需图像）";
                return BuildExpressionExampleS106();
            }
            if (string.Equals(key, "array-data", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "数组统计、子流程与报文组装（无需图像）";
                return BuildArrayExampleS105();
            }
            if (string.Equals(key, "multi-flow-project", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "双流程检测与指令触发";
                return BuildMultiFlowExampleS104();
            }
            if (string.Equals(key, "geometry-branch", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "判定选择几何分支";
                return BuildGeometryBranchExampleS103();
            }
            if (string.Equals(key, "geometry-reuse", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "几何点集跨子流程复用";
                return BuildGeometryReuseExampleS103();
            }
            if (string.Equals(key, "subflow-inspection", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "子流程检测与结果返回";
                return BuildSubflowInspectionExampleS102();
            }
            if (string.Equals(key, "conditional-paths", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "判定驱动真 / 假条件分支";
                bool available = BuildIlluminationExampleS94();
                var source = flowTools[0];
                var judge = flowTools[3];
                var whenTrue = CreateFlowTool(VmToolKind.ImageFilter, "05 通过时滤波", true, null);
                var whenFalse = CreateFlowTool(VmToolKind.ImageEnhancement, "06 未通过时增强", true, null);
                flowTools.Add(whenTrue); flowTools.Add(whenFalse);
                foreach (var tool in new[] { whenTrue, whenFalse })
                {
                    tool.SetInputBinding("Image", source.ToolId, "Image");
                    tool.SetInputBinding("$RunCondition", judge.ToolId, "Passed");
                }
                whenFalse.Parameters.ExecutionConditionWhenFalse = true;
                var merge = CreateFlowTool(VmToolKind.BranchValueSelection, "07 选择实际分支图像", true, null);
                merge.Parameters.BranchValueType = "Image";
                merge.SetInputBinding("Selector", judge.ToolId, "Passed");
                merge.SetInputBinding("WhenTrue", whenTrue.ToolId, "Image");
                merge.SetInputBinding("WhenFalse", whenFalse.ToolId, "Image");
                var final = CreateFlowTool(VmToolKind.GrayHistogram, "08 汇合后亮度分析", true, null);
                final.SetInputBinding("Image", merge.ToolId, "Value");
                flowTools.Add(merge); flowTools.Add(final);
                StopOnNgCheckBox.IsChecked = false;
                return available;
            }
            if (string.Equals(key, "illumination-profile", StringComparison.OrdinalIgnoreCase))
            {
                displayName = "亮度分布与沿线分析";
                return BuildIlluminationExampleS94();
            }
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

        private void LoadIlluminationExampleS94_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("illumination-profile");
        }

        private void LoadConditionalExampleS100_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("conditional-paths");
        }

        private void LoadSubflowExampleS102_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("subflow-inspection");
        }

        private bool BuildSubflowInspectionExampleS102()
        {
            // Reuse the self-authored image fixture; no device or vendor example file is required.
            bool available = BuildIlluminationExampleS94();
            var source = flowTools[0];
            foreach (var previous in flowTools.Skip(1).ToList()) { flowTools.Remove(previous); previous.Dispose(); }
            source.InstanceName = "01 自带亮区图像";
            source.Parameters.LocalImageSerialNumber = "LEARN-SUBFLOW";
            var threshold = CreateFlowTool(VmToolKind.ImageThreshold, "01 提取亮区", true, null);
            var contour = CreateFlowTool(VmToolKind.RegionContourMeasurement, "02 计算轮廓", true, null);
            var call = CreateFlowTool(VmToolKind.SubflowCall, "02 亮区检测子流程", true, null);
            try
            {
                threshold.Parameters.ImageThresholdMinGray = 235;
                threshold.Parameters.ImageThresholdMaxGray = 255;
                threshold.SetInputBinding("Image", VmSubflowData.InputProxyId, "ImageIn");
                contour.SetInputBinding("Region", threshold.ToolId, "Region");
                call.Parameters.Subflow = new VmSubflowData
                {
                    Tools = CaptureFlowRecipeS102(new[] { threshold, contour }),
                    Inputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "ImageIn", DisplayName = "检测图像", DataType = "Image" } },
                    Outputs = new List<VmSubflowPortData>
                    {
                        new VmSubflowPortData { PortName = "DetectedRegion", DisplayName = "亮区", DataType = "Region", SourceToolId = threshold.ToolId, SourcePortName = "Region" },
                        new VmSubflowPortData { PortName = "DetectedArea", DisplayName = "亮区面积", DataType = "Number", SourceToolId = threshold.ToolId, SourcePortName = "Area" },
                        new VmSubflowPortData { PortName = "DetectedContour", DisplayName = "亮区轮廓", DataType = "XLD", SourceToolId = contour.ToolId, SourcePortName = "XLD" },
                        new VmSubflowPortData { PortName = "ContourLength", DisplayName = "轮廓总长", DataType = "Number", SourceToolId = contour.ToolId, SourcePortName = "TotalLength" }
                    }
                };
                call.SetInputBinding("ImageIn", source.ToolId, "Image");
                flowTools.Add(call);
            }
            catch { call.Dispose(); throw; }
            finally { threshold.Dispose(); contour.Dispose(); }
            var histogram = CreateFlowTool(VmToolKind.GrayHistogram, "03 返回区域内亮度", true, null);
            histogram.SetInputBinding("Image", source.ToolId, "Image");
            histogram.SetInputBinding("ROI", call.ToolId, "DetectedRegion");
            histogram.Parameters.RoiSourceMode = VmRoiSourceMode.Inherited;
            flowTools.Add(histogram);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "04 返回面积判定", true, null);
            judge.SetInputBinding("Value", call.ToolId, "DetectedArea");
            judge.InputToolId = call.ToolId; judge.InputPortName = "DetectedArea";
            judge.NumericLowerLimit = 3900; judge.NumericUpperLimit = 4100;
            flowTools.Add(judge);
            StopOnNgCheckBox.IsChecked = false;
            return available;
        }

        private bool BuildIlluminationExampleS94()
        {
            // Self-authored fixture: no HALCON installation example assets are required.
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HalconVision", "LearningImages");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "illumination-" + Guid.NewGuid().ToString("N") + ".png");
            using (var bitmap = new System.Drawing.Bitmap(320, 240))
            {
                for (int y = 0; y < bitmap.Height; y++)
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int gray = 30 + (int)Math.Round(190.0 * x / (bitmap.Width - 1));
                        if (x >= 140 && x < 180 && y >= 70 && y < 170) gray = 245;
                        bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(gray, gray, gray));
                    }
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            VmToolInstance source = CreateFlowTool(VmToolKind.ImageSource, "01 渐变与亮区图像", true, null);
            VmToolInstance histogram = CreateFlowTool(VmToolKind.GrayHistogram, "02 全图亮度分布", true, null);
            VmToolInstance profile = CreateFlowTool(VmToolKind.GrayLineProfile, "03 水平线亮度变化", true, null);
            VmToolInstance judge = CreateFlowTool(VmToolKind.NumericJudge, "04 亮度变化范围判定", true, null);
            foreach (var tool in new[] { source, histogram, profile, judge }) flowTools.Add(tool);
            source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "LEARN-ILLUMINATION";
            histogram.SetInputBinding("Image", source.ToolId, "Image");
            profile.SetInputBinding("Image", source.ToolId, "Image");
            histogram.Parameters.HistogramPercentile = 95;
            profile.Parameters.ProfileStartRow = 120;
            profile.Parameters.ProfileStartColumn = 0;
            profile.Parameters.ProfileEndRow = 120;
            profile.Parameters.ProfileEndColumn = 319;
            profile.Parameters.ProfileSampleCount = 320;
            judge.SetInputBinding("Value", profile.ToolId, "Range");
            judge.InputToolId = profile.ToolId;
            judge.InputPortName = "Range";
            judge.NumericLowerLimit = 200;
            judge.NumericUpperLimit = 230;
            return true;
        }

        private static string GetHalconExampleImageS78(string relativePath)
        {
            string publicDocuments = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            return Path.Combine(publicDocuments, "MVTec", "HALCON-20.11-Progress", "examples", "images", relativePath);
        }
    }
}
