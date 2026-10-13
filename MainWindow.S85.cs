using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void SetDockParameterRowVisibilityS85(FrameworkElement anchor, bool visible)
        {
            if (anchor == null) return;
            Grid grid = anchor.Parent as Grid;
            if (grid == null) return;
            int row = Grid.GetRow(anchor);
            Visibility visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            foreach (UIElement child in grid.Children)
            {
                if (Grid.GetRow(child) == row) child.Visibility = visibility;
            }
        }

        private void UpdateDockTaskConditionalVisibilityS85()
        {
            RefreshArrayReorderFieldsS108();
            if (DockImageFilterModeComboBox != null)
            {
                bool mean = VmImageFilterMode.Normalize(DockImageFilterModeComboBox.SelectedValue as string) == VmImageFilterMode.Mean;
                SetDockParameterRowVisibilityS85(DockImageFilterWidthTextBox, mean);
                SetDockParameterRowVisibilityS85(DockImageFilterRadiusTextBox, !mean);
            }

            if (DockImageEnhancementModeComboBox != null)
            {
                bool linear = VmImageEnhancementMode.Normalize(DockImageEnhancementModeComboBox.SelectedValue as string) == VmImageEnhancementMode.LinearScale;
                SetDockParameterRowVisibilityS85(DockImageEnhancementScaleTextBox, linear);
                SetDockParameterRowVisibilityS85(DockImageEnhancementMaskWidthTextBox, !linear);
                SetDockParameterRowVisibilityS85(DockImageEnhancementFactorTextBox, !linear);
            }

            if (DockImageChannelModeComboBox != null)
            {
                bool extract = VmImageChannelMode.Normalize(DockImageChannelModeComboBox.SelectedValue as string) == VmImageChannelMode.Extract;
                SetDockParameterRowVisibilityS85(DockImageChannelIndexTextBox, extract);
            }

            if (DockImageDifferenceModeComboBox != null)
            {
                bool directional = VmImageDifferenceMode.Normalize(DockImageDifferenceModeComboBox.SelectedValue as string) != VmImageDifferenceMode.AbsoluteDifference;
                DockImageDifferenceAddLabel.Visibility = directional ? Visibility.Visible : Visibility.Collapsed;
                DockImageDifferenceAddTextBox.Visibility = directional ? Visibility.Visible : Visibility.Collapsed;
            }

            if (DockProjectiveOutputModeComboBox != null)
            {
                bool explicitSize = VmProjectiveOutputMode.Normalize(DockProjectiveOutputModeComboBox.SelectedValue as string) == VmProjectiveOutputMode.ExplicitSize;
                SetDockParameterRowVisibilityS85(DockProjectiveOutputWidthTextBox, explicitSize);
            }

            if (DockXldLengthSelectionModeComboBox != null)
            {
                bool rankRange = VmXldLengthSelectionMode.Normalize(DockXldLengthSelectionModeComboBox.SelectedValue as string) == VmXldLengthSelectionMode.RankRange;
                DockXldLengthEndRankLabel.Visibility = rankRange ? Visibility.Visible : Visibility.Collapsed;
                DockXldLengthEndRankTextBox.Visibility = rankRange ? Visibility.Visible : Visibility.Collapsed;
            }

            if (DockNumericCalculationOperationComboBox != null)
            {
                bool round = VmNumericCalculationOperation.Normalize(DockNumericCalculationOperationComboBox.SelectedValue as string) == VmNumericCalculationOperation.Round;
                SetDockParameterRowVisibilityS85(DockNumericCalculationRoundDigitsTextBox, round);
            }
        }

        private InspectionRecord RunLocalAdaptiveThresholdToolS85(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.LocalAdaptiveThreshold);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);

            HImage grayImage = null;
            HRegion effectiveRoi = null;
            HObject reducedImage = null;
            HObject thresholdRegion = null;
            HObject connectedRegion = null;
            try
            {
                if (imageService.GetChannelCount(inputImage) <= 0)
                    throw new InvalidOperationException("输入图像没有可用通道。");

                grayImage = CreateGrayImage(inputImage);
                effectiveRoi = CreateEffectiveRoiRegion(tool, false);
                HObject operationImage = grayImage;
                if (effectiveRoi != null)
                {
                    HOperatorSet.ReduceDomain(grayImage, effectiveRoi, out reducedImage);
                    operationImage = reducedImage;
                }

                string polarity = VmDynamicThresholdPolarity.Normalize(parameters.LocalAdaptivePolarity);
                HOperatorSet.VarThreshold(
                    operationImage,
                    out thresholdRegion,
                    parameters.LocalAdaptiveMaskWidth,
                    parameters.LocalAdaptiveMaskHeight,
                    parameters.LocalAdaptiveStdDevScale,
                    parameters.LocalAdaptiveOffset,
                    VmDynamicThresholdPolarity.GetHalconValue(polarity));
                HOperatorSet.Connection(thresholdRegion, out connectedRegion);

                HTuple count;
                HTuple area;
                HTuple row;
                HTuple column;
                HOperatorSet.CountObj(connectedRegion, out count);
                HOperatorSet.AreaCenter(thresholdRegion, out area, out row, out column);
                double totalArea = SumTuple(area);
                int regionCount = totalArea <= 0 || count == null || count.Length == 0 ? 0 : count.I;
                if (totalArea <= 0)
                {
                    DisposeObject(connectedRegion);
                    HOperatorSet.GenEmptyObj(out connectedRegion);
                }

                string resultCode = regionCount > 0 && totalArea > 0 ? "OK" : "NG";
                string scope = effectiveRoi == null ? "全图" : "ROI-屏蔽有效域";
                string parameterSummary = string.Format(
                    CultureInfo.InvariantCulture,
                    "var_threshold; Window={0}x{1}; StdDevScale={2:0.###}; Offset={3:0.###}; Polarity={4}; Scope={5}",
                    parameters.LocalAdaptiveMaskWidth,
                    parameters.LocalAdaptiveMaskHeight,
                    parameters.LocalAdaptiveStdDevScale,
                    parameters.LocalAdaptiveOffset,
                    polarity,
                    scope);

                tool.SetOutputValue("Region", VmRegionSnapshot.Create(connectedRegion, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("WindowWidth", (double)parameters.LocalAdaptiveMaskWidth);
                tool.SetOutputValue("WindowHeight", (double)parameters.LocalAdaptiveMaskHeight);
                tool.SetOutputValue("Offset", parameters.LocalAdaptiveOffset);
                tool.SetOutputValue("StdDevScale", parameters.LocalAdaptiveStdDevScale);
                tool.SetOutputValue("Polarity", polarity);
                tool.SetOutputValue("Parameters", parameterSummary);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayRegion(connectedRegion, tool, "Region", regionCount, totalArea, resultCode);
                connectedRegion = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON var_threshold {0}，窗口={1}×{2}，最小灰差={3:0.###}，{4}，Region={5}，Area={6:0.###}",
                    VmDynamicThresholdPolarity.GetDisplayText(polarity),
                    parameters.LocalAdaptiveMaskWidth,
                    parameters.LocalAdaptiveMaskHeight,
                    parameters.LocalAdaptiveOffset,
                    scope,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("LocalAdaptiveThreshold", resultCode, totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(connectedRegion);
                DisposeObject(thresholdRegion);
                DisposeObject(reducedImage);
                if (effectiveRoi != null) effectiveRoi.Dispose();
                if (grayImage != null) grayImage.Dispose();
            }
        }
    }
}
