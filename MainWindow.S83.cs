using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private string lastFlowNodeClickToolIdS83;
        private DateTime lastFlowNodeClickUtcS83 = DateTime.MinValue;

        private static readonly HashSet<string> CompactScalarFieldsS83 = NamesS83(
            "DockImageThresholdMinTextBox", "DockImageThresholdMaxTextBox",
            "DockNumericLowerTextBox", "DockNumericUpperTextBox", "DockNumericToleranceTextBox",
            "DockNccMinScoreS131", "DockNccMaxMatchesS131",
            "DockImageChannelIndexTextBox", "DockImageFilterWidthTextBox", "DockImageFilterHeightTextBox", "DockImageFilterRadiusTextBox",
            "DockLocalAdaptiveMaskWidthTextBox", "DockLocalAdaptiveMaskHeightTextBox", "DockLocalAdaptiveOffsetTextBox",
            "DockImageEnhancementScaleTextBox", "DockImageEnhancementOffsetTextBox", "DockImageResizeWidthTextBox", "DockImageResizeHeightTextBox",
            "DockImageRotateAngleTextBox", "DockImageAffineTranslateRowTextBox", "DockImageAffineTranslateColumnTextBox", "DockImageAffineRotationTextBox",
            "DockSubpixelEdgeAlphaTextBox", "DockSubpixelEdgeLowTextBox", "DockSubpixelEdgeHighTextBox",
            "DockPolarCenterRowTextBox", "DockPolarCenterColumnTextBox", "DockPolarInnerRadiusTextBox", "DockPolarOuterRadiusTextBox",
            "DockColorHueS129", "DockColorToleranceS129", "DockColorSatMinS129", "DockColorValueMinS129",
            "DockVariationAbsoluteS130", "DockVariationFactorS130", "DockVariationMinAreaS130");

        private void CompactTaskParameterFieldsS83(FrameworkElement panel, HashSet<string> preferred)
        {
            foreach (FrameworkElement input in EnumerateLogicalTree(panel, true).OfType<FrameworkElement>())
            {
                TextBox scalar = input as TextBox;
                if (scalar != null && CompactScalarFieldsS83.Contains(scalar.Name))
                {
                    scalar.MinWidth = 96;
                    scalar.MaxWidth = 140;
                    scalar.HorizontalAlignment = HorizontalAlignment.Left;
                }
                ComboBox choice = input as ComboBox;
                if (choice != null && preferred != null && preferred.Contains(choice.Name))
                {
                    choice.MinWidth = 160;
                    choice.MaxWidth = 220;
                    choice.HorizontalAlignment = HorizontalAlignment.Left;
                }
            }
        }

        private static readonly Dictionary<VmToolKind, HashSet<string>> TaskCommonParameterProfilesS83 =
            new Dictionary<VmToolKind, HashSet<string>>
            {
                { VmToolKind.ImageSource, NamesS83("DockLocalImagePathTextBox", "DockLocalImageSerialTextBox") },
                { VmToolKind.ImageChannel, NamesS83("DockImageChannelModeComboBox", "DockImageChannelIndexTextBox") },
                { VmToolKind.ImageFilter, NamesS83("DockImageFilterModeComboBox", "DockImageFilterWidthTextBox", "DockImageFilterHeightTextBox", "DockImageFilterRadiusTextBox") },
                { VmToolKind.ImageThreshold, NamesS83("DockImageThresholdMinTextBox", "DockImageThresholdMaxTextBox") },
                { VmToolKind.AutomaticThreshold, NamesS83("DockAutomaticThresholdPolarityComboBox") },
                { VmToolKind.LocalAdaptiveThreshold, NamesS83("DockLocalAdaptiveMaskWidthTextBox", "DockLocalAdaptiveMaskHeightTextBox", "DockLocalAdaptiveOffsetTextBox", "DockLocalAdaptivePolarityComboBox") },
                { VmToolKind.ColorInspectionHsv, NamesS83("DockColorHueS129", "DockColorToleranceS129", "DockColorSatMinS129", "DockColorValueMinS129") },
                { VmToolKind.VariationSurfaceInspection, NamesS83("DockVariationAbsoluteS130", "DockVariationFactorS130", "DockVariationMinAreaS130") },
                { VmToolKind.ImageEnhancement, NamesS83("DockImageEnhancementModeComboBox", "DockImageEnhancementScaleTextBox", "DockImageEnhancementOffsetTextBox") },
                { VmToolKind.ImageResize, NamesS83("DockImageResizeWidthTextBox", "DockImageResizeHeightTextBox", "DockImageResizeInterpolationComboBox") },
                { VmToolKind.ImageMirror, NamesS83("DockImageMirrorModeComboBox") },
                { VmToolKind.ImageRotate, NamesS83("DockImageRotateAngleTextBox", "DockImageRotateInterpolationComboBox") },
                { VmToolKind.ImageAffineTransform, NamesS83("DockImageAffineTranslateRowTextBox", "DockImageAffineTranslateColumnTextBox", "DockImageAffineRotationTextBox") },
                { VmToolKind.ImageProjectiveTransform, NamesS83("DockProjectiveOutputModeComboBox", "DockProjectiveInterpolationComboBox") },
                { VmToolKind.CameraDistortionCorrection, NamesS83("DockCameraModeS126") },
                { VmToolKind.SubpixelEdgeExtraction, NamesS83("DockSubpixelEdgeFilterComboBox", "DockSubpixelEdgeAlphaTextBox", "DockSubpixelEdgeLowTextBox", "DockSubpixelEdgeHighTextBox") },
                { VmToolKind.PolarImageUnwrap, NamesS83("DockPolarCenterRowTextBox", "DockPolarCenterColumnTextBox", "DockPolarInnerRadiusTextBox", "DockPolarOuterRadiusTextBox") },
                { VmToolKind.RoiImageExtraction, NamesS83("DockRoiImageExtractionModeComboBox") },
                { VmToolKind.ImageDifference, NamesS83("DockImageDifferenceModeComboBox", "DockImageDifferenceMultiplierTextBox", "DockImageDifferenceAddTextBox") },
                { VmToolKind.DynamicThreshold, NamesS83("DockDynamicThresholdPolarityComboBox", "DockDynamicThresholdOffsetTextBox") },
                { VmToolKind.RegionMorphology, NamesS83("DockRegionMorphologyModeComboBox", "DockRegionMorphologyRadiusTextBox") },
                { VmToolKind.RegionFeatureFilter, NamesS83("DockRegionFeatureComboBox", "DockRegionFeatureMinTextBox", "DockRegionFeatureMaxTextBox") },
                { VmToolKind.RegionSetOperation, NamesS83("DockRegionSetOperationComboBox") },
                { VmToolKind.RegionContourMeasurement, NamesS83("DockRegionContourModeComboBox") },
                { VmToolKind.XldContourFilter, NamesS83("DockXldContourFeatureComboBox", "DockXldContourMinTextBox", "DockXldContourMaxTextBox") },
                { VmToolKind.XldAdjacentContourUnion, NamesS83("DockXldAdjacentMaxDistAbsTextBox", "DockXldAdjacentMaxDistRelTextBox", "DockXldAdjacentAttributeModeComboBox") },
                { VmToolKind.XldContourSmoothing, NamesS83("DockXldSmoothNumRegrPointsTextBox") },
                { VmToolKind.XldContourSegmentation, NamesS83("DockXldSegmentModeComboBox", "DockXldSegmentSmoothTextBox") },
                { VmToolKind.XldLineFitting, NamesS83("DockXldLineFitAlgorithmComboBox", "DockXldLineFitMaxPointsTextBox", "DockXldLineFitIterationsTextBox") },
                { VmToolKind.XldCircleFitting, NamesS83("DockXldCircleFitAlgorithmComboBox", "DockXldCircleFitMaxPointsTextBox", "DockXldCircleFitMaxClosureTextBox") },
                { VmToolKind.XldEllipseFitting, NamesS83("DockXldEllipseFitAlgorithmComboBox", "DockXldEllipseFitMaxPointsTextBox", "DockXldEllipseFitMaxClosureTextBox") },
                { VmToolKind.XldRectangleFitting, NamesS83("DockXldRectangleFitAlgorithmComboBox", "DockXldRectangleFitMaxPointsTextBox", "DockXldRectangleFitMaxClosureTextBox") },
                { VmToolKind.XldLineRelationMeasurement, NamesS83("DockLineRelationFitAlgorithmComboBox", "DockLineRelationParallelToleranceTextBox", "DockLineRelationCoincidentToleranceTextBox", "DockLineRelationPerpendicularToleranceTextBox") },
                { VmToolKind.XldAffineTransform, NamesS83("DockXldAffineTranslateRowTextBox", "DockXldAffineTranslateColumnTextBox", "DockXldAffineScaleRowTextBox", "DockXldAffineScaleColumnTextBox") },
                { VmToolKind.XldProjectiveTransform, NamesS83("DockXldProjectiveCoordinatePolicyComboBox") },
                { VmToolKind.XldContourCrop, NamesS83("DockXldCropRow1TextBox", "DockXldCropColumn1TextBox", "DockXldCropRow2TextBox", "DockXldCropColumn2TextBox") },
                { VmToolKind.XldLengthClassification, NamesS83("DockXldLengthShortMaximumTextBox", "DockXldLengthMediumMaximumTextBox") },
                { VmToolKind.XldLengthSortSelection, NamesS83("DockXldLengthSortDirectionComboBox", "DockXldLengthSelectionModeComboBox", "DockXldLengthStartRankTextBox", "DockXldLengthEndRankTextBox") },
                { VmToolKind.XldPointClosureFilter, NamesS83("DockXldPointMinimumTextBox", "DockXldPointMaximumTextBox", "DockXldClosureModeComboBox", "DockXldClosureToleranceTextBox") },
                { VmToolKind.XldAreaDirectionFilter, NamesS83("DockXldAreaMinimumTextBox", "DockXldAreaMaximumTextBox", "DockXldAreaDirectionModeComboBox", "DockXldAreaClosureToleranceTextBox") },
                { VmToolKind.XldBoundingCompactnessFilter, NamesS83("DockXldBoundingWidthMinimumTextBox", "DockXldBoundingWidthMaximumTextBox", "DockXldBoundingHeightMinimumTextBox", "DockXldBoundingHeightMaximumTextBox") },
                { VmToolKind.XldCircularityConvexityFilter, NamesS83("DockXldCircularityMinimumTextBox", "DockXldCircularityMaximumTextBox", "DockXldConvexityMinimumTextBox", "DockXldConvexityMaximumTextBox") },
                { VmToolKind.XldEnclosingCircleDiameterMeasurement, NamesS83("DockXldEnclosingContourModeComboBox", "DockXldEnclosingMinimumPointCountTextBox", "DockXldEnclosingDiameterMinimumTextBox", "DockXldEnclosingDiameterMaximumTextBox") },
                { VmToolKind.XldMinimumRectangleDirectionMeasurement, NamesS83("DockXldMinimumRectangleContourModeComboBox", "DockXldMinimumRectangleMinimumPointCountTextBox", "DockXldMinimumRectangleWidthMinimumTextBox", "DockXldMinimumRectangleWidthMaximumTextBox") },
                { VmToolKind.XldDirectionConsistencyMeasurement, NamesS83("DockXldDirectionTargetAngleTextBox", "DockXldDirectionDeviationMinimumTextBox", "DockXldDirectionDeviationMaximumTextBox", "DockXldDirectionMinimumAspectRatioTextBox") },
                { VmToolKind.Blob, NamesS83("DockBlobMinTextBox", "DockBlobMaxTextBox", "DockBlobAreaTextBox") },
                { VmToolKind.GrayStat, NamesS83("DockGrayMinTextBox", "DockGrayMaxTextBox") },
                { VmToolKind.GrayHistogram, NamesS83("DockHistogramPercentileTextBoxS91") },
                { VmToolKind.GrayLineProfile, NamesS83("DockProfileStartRowS92", "DockProfileStartColumnS92", "DockProfileEndRowS92", "DockProfileEndColumnS92") },
                { VmToolKind.BranchValueSelection, NamesS83("DockBranchTypeS101") },
                { VmToolKind.SubflowCall, NamesS83("DockSubflowContentsS102Button") },
                { VmToolKind.EdgeMeasure, NamesS83("DockEdgeThresholdTextBox") },
                { VmToolKind.ShapeMatch, NamesS83("DockShapeMinScoreTextBox", "DockShapeMaxMatchesTextBox") },
                { VmToolKind.NccTemplateMatch, NamesS83("DockNccMinScoreS131", "DockNccMaxMatchesS131") },
                { VmToolKind.PoseImageAlignment, NamesS83("DockAlignmentSizeModeS132") },
                { VmToolKind.DataCode2DRecognition, NamesS83("DockDataCodeTypeComboBox", "DockDataCodeProfileComboBox", "DockDataCodePolarityComboBox", "DockDataCodeMaxResultsTextBox") },
                { VmToolKind.BarCode1DRecognition, NamesS83("DockBarCodeTypeComboBox", "DockBarCodeMaxResultsTextBox", "DockBarCodeNumScanlinesTextBox", "DockBarCodeMinIdenticalTextBox") },
                { VmToolKind.OcrRecognition, NamesS83("DockOcrModelTextBox", "DockOcrMinimumCharactersTextBox", "DockOcrConfidenceTextBox", "DockOcrThresholdTextBox") },
                { VmToolKind.HDevelop, NamesS83() },
                { VmToolKind.TcpClientExchange, NamesS83("DockTcpClientHostTextBox", "DockTcpClientPortTextBox", "DockTcpClientSendTemplateTextBox", "DockTcpClientWaitForResponseCheckBox") },
                { VmToolKind.ModbusTcpExchange, NamesS83("DockModbusHostS118", "DockModbusFunctionS118", "DockModbusAddressS118", "DockModbusQuantityS118", "DockModbusWriteValueS118") },
                { VmToolKind.ModbusRtuExchange, NamesS83("DockRtuPortS119", "DockRtuFunctionS119", "DockRtuAddressS119", "DockRtuQuantityS119", "DockRtuWriteValueS119") },
                { VmToolKind.TextAssembly, NamesS83("DockTextAssemblyTemplateTextBox", "DockTextAssemblySeparatorTextBox", "DockTextAssemblyEmptyPolicyComboBox", "DockTextAssemblyNumberFormatTextBox") },
                { VmToolKind.TextParser, NamesS83("DockTextParserModeComboBox", "DockTextParserPatternTextBox", "DockTextParserFieldIndexTextBox", "DockTextParserConvertNumberCheckBox") },
                { VmToolKind.NumericCalculation, NamesS83("DockNumericCalculationOperationComboBox", "DockNumericCalculationRoundDigitsTextBox") },
                { VmToolKind.NumberArrayCreate, NamesS83("DockArrayTextS105", "DockArrayDelimiterS105") },
                { VmToolKind.TextArraySplit, NamesS83("DockArrayTextS105", "DockArrayDelimiterS105") },
                { VmToolKind.NumberArrayStatistics, NamesS83("DockArrayIndexS105") },
                { VmToolKind.TextArraySelect, NamesS83("DockArrayIndexS105") },
                { VmToolKind.NumericExpression, NamesS83("DockExpressionTextS106") },
                { VmToolKind.JsonRead, NamesS83("DockJsonTextS110") },
                { VmToolKind.JsonWrite, NamesS83() },
                { VmToolKind.NumberArrayReorder, NamesS83("DockArrayReorderModeS108", "DockArraySliceStartS108", "DockArraySliceLengthS108") },
                { VmToolKind.TextArrayReorder, NamesS83("DockArrayReorderModeS108", "DockArraySliceStartS108", "DockArraySliceLengthS108") },
                { VmToolKind.ResultLogicAggregation, NamesS83("DockResultLogicOperationComboBox", "DockResultLogicCaseSensitiveCheckBox", "DockResultLogicTrimInputCheckBox") },
                { VmToolKind.CSharpDllScript, NamesS83("DockScriptExecutionModeComboBox", "DockScriptTimeoutTextBox", "DockScriptParameterNumberTextBox", "DockScriptParameterTextTextBox") },
                { VmToolKind.GlobalVariableRead, NamesS83("DockGlobalVariableComboBox") },
                { VmToolKind.GlobalVariableWrite, NamesS83("DockGlobalVariableComboBox") },
                { VmToolKind.PointToLineDistanceMeasurement, NamesS83("DockPointLineSideConventionComboBox", "DockPointLineCrossSizeTextBox") },
                { VmToolKind.PointToCircleRadialDeviationMeasurement, NamesS83("DockPointCircleSignConventionComboBox", "DockPointCircleCrossSizeTextBox") },
                { VmToolKind.PointToEllipseDistanceMeasurement, NamesS83("DockPointEllipseBoundaryToleranceTextBox", "DockPointEllipseCrossSizeTextBox", "DockPointEllipseContourResolutionTextBox") },
                { VmToolKind.LineEllipseIntersectionMeasurement, NamesS83("DockLineEllipseToleranceTextBox", "DockLineEllipseCrossSizeTextBox", "DockLineEllipseResolutionTextBox") },
                { VmToolKind.LineCircleIntersectionMeasurement, NamesS83("DockLineCircleIntersectionOrderComboBox", "DockLineCircleCrossSizeTextBox") },
                { VmToolKind.CircleCircleIntersectionMeasurement, NamesS83("DockCircleCircleIntersectionOrderComboBox", "DockCircleCircleCrossSizeTextBox") },
                { VmToolKind.PointPairLineConstructionMeasurement, NamesS83("DockPointPairMinimumDistanceTextBox", "DockPointPairCrossSizeTextBox") },
                { VmToolKind.TypedLineLineRelationMeasurement, NamesS83("DockLineLineParallelToleranceTextBox", "DockLineLineCoincidenceToleranceTextBox", "DockLineLineCrossSizeTextBox") },
                { VmToolKind.CaliperEdgePairMeasurement, NamesS83("DockCaliperLength2TextBox", "DockCaliperSigmaTextBox", "DockCaliperThresholdTextBox", "DockCaliperTransitionComboBox") },
                { VmToolKind.LinearCaliperArrayMeasurement, NamesS83("DockLinearCaliperCountTextBox", "DockLinearCaliperLength2TextBox", "DockLinearCaliperSigmaTextBox", "DockLinearCaliperThresholdTextBox") },
                { VmToolKind.ImageEdgeLineFindingS143, NamesS83("DockEdgeLineTransitionS143", "DockEdgeLineThresholdS143", "DockEdgeLineCountS143", "DockEdgeLineHalfWidthS143") },
                { VmToolKind.ConnectedObjectAnalysisS144, NamesS83("DockObjectsMinimumS144", "DockObjectsMaximumS144", "DockObjectsSortS144", "DockObjectsIndexS144") },
                { VmToolKind.CircularCaliperCircleFinder, NamesS83("DockCircularCaliperRadiusTextBox", "DockCircularCaliperCountTextBox", "DockCircularCaliperThresholdTextBox", "DockCircularCaliperTransitionComboBox") },
                { VmToolKind.RingWallThicknessMeasurement, NamesS83("DockRingRadiusTextBox", "DockRingCountTextBox", "DockRingThresholdTextBox", "DockRingTransitionComboBox") },
                { VmToolKind.PositionCorrection2D, NamesS83("DockPositionCorrectionReferenceRowTextBox", "DockPositionCorrectionReferenceColumnTextBox", "DockPositionCorrectionReferenceAngleTextBox") },
                { VmToolKind.PointSetGeometryStatistics, NamesS83("DockPointSetMinimumCountTextBox", "DockPointSetAbnormalPolicyComboBox") },
                { VmToolKind.TypedPointSetLineFittingMeasurement, NamesS83("DockTypedPointSetLineFitMinimumCountTextBox", "DockTypedPointSetLineFitAlgorithmComboBox", "DockTypedPointSetLineFitIterationsTextBox") },
                { VmToolKind.TypedPointSetCircleFittingMeasurement, NamesS83("DockTypedPointSetCircleFitMinimumCountTextBox", "DockTypedPointSetCircleFitAlgorithmComboBox", "DockTypedPointSetCircleFitMaxClosureTextBox") },
                { VmToolKind.TypedPointSetEllipseFittingMeasurement, NamesS83("DockTypedPointSetEllipseFitMinimumCountTextBox", "DockTypedPointSetEllipseFitAlgorithmComboBox", "DockTypedPointSetEllipseFitMaxClosureTextBox") },
                { VmToolKind.NumericJudge, NamesS83("DockNumericOperatorComboBox", "DockNumericLowerTextBox", "DockNumericUpperTextBox", "DockNumericToleranceTextBox") }
            };

        private static HashSet<string> NamesS83(params string[] names)
        {
            return new HashSet<string>(names ?? new string[0], StringComparer.Ordinal);
        }

        private static bool TryGetPreferredCommonParameterNamesS83(VmToolKind kind, out HashSet<string> names)
        {
            return TaskCommonParameterProfilesS83.TryGetValue(kind, out names);
        }

        private void DockImageThresholdEstimateButton_ClickS83(object sender, RoutedEventArgs e)
        {
            FrameworkElement button = sender as FrameworkElement;
            string polarity = Convert.ToString(button == null ? null : button.Tag, CultureInfo.InvariantCulture);
            EstimateImageThresholdFromCurrentInputS83(polarity);
        }

        private bool EstimateImageThresholdFromCurrentInputS83(string polarity)
        {
            VmToolInstance tool = dockEditingTool;
            string normalizedPolarity = string.Equals(polarity, "light", StringComparison.OrdinalIgnoreCase) ? "light" : "dark";
            if (tool == null || tool.Kind != VmToolKind.ImageThreshold)
            {
                return false;
            }
            if (isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning)
            {
                SetThresholdEstimateStatusS83("运行中不能估算，请先停止流程。", true);
                return false;
            }

            HImage inputImage = null;
            HImage grayImage = null;
            HRegion effectiveRoi = null;
            HObject reducedImage = null;
            HObject estimatedRegion = null;
            HTuple threshold = null;
            try
            {
                inputImage = CreateToolInputImage(tool);
                grayImage = CreateGrayImage(inputImage);
                effectiveRoi = CreateEffectiveRoiRegion(tool, false);
                HObject estimateImage = grayImage;
                if (effectiveRoi != null)
                {
                    HOperatorSet.ReduceDomain(grayImage, effectiveRoi, out reducedImage);
                    estimateImage = reducedImage;
                }

                HOperatorSet.BinaryThreshold(
                    estimateImage,
                    out estimatedRegion,
                    "max_separability",
                    normalizedPolarity,
                    out threshold);
                if (threshold == null || threshold.Length == 0)
                {
                    throw new InvalidOperationException("HALCON 未返回有效分割阈值。");
                }

                double value = Math.Max(0, Math.Min(255, threshold[0].D));
                double minimum = normalizedPolarity == "dark" ? 0 : value;
                double maximum = normalizedPolarity == "dark" ? value : 255;
                DockImageThresholdMinTextBox.Text = minimum.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageThresholdMaxTextBox.Text = maximum.ToString("0.###", CultureInfo.InvariantCulture);
                string targetText = normalizedPolarity == "dark" ? "暗目标" : "亮目标";
                string scopeText = effectiveRoi == null ? "全图" : "已应用 ROI/屏蔽";
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}建议 [{1:0.###}, {2:0.###}] · {3} · 尚未应用",
                    targetText,
                    minimum,
                    maximum,
                    scopeText);
                SetThresholdEstimateStatusS83(message, false);
                HeaderStatusText.Text = "已根据当前真实图像估算" + targetText + "阈值；可按 F5 试运行，确认后再应用。";
                LogInfo(tool.InstanceName + " 阈值估算：" + message);
                return true;
            }
            catch (Exception ex)
            {
                SetThresholdEstimateStatusS83("估算失败：" + ex.Message, true);
                HeaderStatusText.Text = "阈值估算失败：" + ex.Message;
                return false;
            }
            finally
            {
                if (threshold != null) threshold.Dispose();
                DisposeObject(estimatedRegion);
                DisposeObject(reducedImage);
                if (effectiveRoi != null) effectiveRoi.Dispose();
                if (grayImage != null) grayImage.Dispose();
                if (inputImage != null) inputImage.Dispose();
            }
        }

        private void SetThresholdEstimateStatusS83(string message, bool error)
        {
            if (DockImageThresholdEstimateStatusS83 == null) return;
            DockImageThresholdEstimateStatusS83.Text = message ?? string.Empty;
            DockImageThresholdEstimateStatusS83.Foreground = new SolidColorBrush(
                error ? Color.FromRgb(185, 28, 28) : Color.FromRgb(15, 118, 110));
            DockImageThresholdEstimateStatusS83.ToolTip = message;
        }
    }
}
