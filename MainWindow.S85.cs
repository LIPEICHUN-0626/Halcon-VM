using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private TextBox invalidParameterFieldUI07;
        private object invalidParameterBrushUI07, invalidParameterThicknessUI07, invalidParameterTipUI07;

        private string FilterModeUI07()
        {
            var selected = DockImageFilterModeComboBox.SelectedItem as VmImageFilterModeOption;
            return VmImageFilterMode.Normalize(selected == null ? null : selected.Key);
        }

        private string EnhancementModeUI07()
        {
            var selected = DockImageEnhancementModeComboBox.SelectedItem as VmImageEnhancementModeOption;
            return VmImageEnhancementMode.Normalize(selected == null ? null : selected.Key);
        }

        private string ChannelModeUI07()
        {
            var selected = DockImageChannelModeComboBox.SelectedItem as VmImageChannelModeOption;
            return VmImageChannelMode.Normalize(selected == null ? null : selected.Key);
        }

        private string DifferenceModeUI07()
        {
            var selected = DockImageDifferenceModeComboBox.SelectedItem as VmImageDifferenceModeOption;
            return VmImageDifferenceMode.Normalize(selected == null ? null : selected.Key);
        }

        private string ProjectiveOutputModeUI07()
        {
            var selected = DockProjectiveOutputModeComboBox.SelectedItem as VmLayerOption;
            return VmProjectiveOutputMode.Normalize(selected == null ? null : selected.Key);
        }

        private string NumericOperationUI07()
        {
            var selected = DockNumericCalculationOperationComboBox.SelectedItem as VmTcpOption;
            return VmNumericCalculationOperation.Normalize(selected == null ? null : selected.Key);
        }

        // Shared by the read-only four-point preview and explicit draft validation.
        // Hidden size fields never replace the applied values in another mode.
        private bool TryReadProjectiveSizeUI07(out int width, out int height, out TextBox field, out string error)
        {
            width = height = 0;
            field = DockProjectiveOutputWidthTextBox;
            error = "输出宽：请输入 2–16384 的整数（像素）。";
            if (!int.TryParse(field.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out width) || width < 2 || width > 16384) return false;
            field = DockProjectiveOutputHeightTextBox;
            error = "输出高：请输入 2–16384 的整数（像素）。";
            if (!int.TryParse(field.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out height) || height < 2 || height > 16384) return false;
            error = "输出宽×高不能超过 67108864 像素，请减小尺寸。";
            if ((long)width * height > 67108864L) return false;
            error = null;
            field = null;
            return true;
        }

        private void ClearParameterFieldErrorUI07()
        {
            if (invalidParameterFieldUI07 == null) return;
            RestoreParameterPropertyUI07(invalidParameterFieldUI07, Control.BorderBrushProperty, invalidParameterBrushUI07);
            RestoreParameterPropertyUI07(invalidParameterFieldUI07, Control.BorderThicknessProperty, invalidParameterThicknessUI07);
            RestoreParameterPropertyUI07(invalidParameterFieldUI07, FrameworkElement.ToolTipProperty, invalidParameterTipUI07);
            invalidParameterFieldUI07 = null;
            DockValidationText.Visibility = Visibility.Visible;
            DockAdvancedParameterValidationText.Text = string.Empty;
            DockAdvancedParameterValidationText.Visibility = Visibility.Collapsed;
        }

        private static void RestoreParameterPropertyUI07(DependencyObject field, DependencyProperty property, object value)
        {
            if (value == DependencyProperty.UnsetValue) field.ClearValue(property);
            else field.SetValue(property, value);
        }

        private bool FailParameterFieldUI07(TextBox field, string message)
        {
            ClearParameterFieldErrorUI07();
            invalidParameterFieldUI07 = field;
            invalidParameterBrushUI07 = field.ReadLocalValue(Control.BorderBrushProperty);
            invalidParameterThicknessUI07 = field.ReadLocalValue(Control.BorderThicknessProperty);
            invalidParameterTipUI07 = field.ReadLocalValue(FrameworkElement.ToolTipProperty);
            field.BorderBrush = Brushes.Firebrick; field.BorderThickness = new Thickness(2);
            field.ToolTip = message;
            DockValidationText.Text = message;
            DockValidationText.Visibility = Visibility.Collapsed;
            DockAdvancedParameterValidationText.Text = message;
            DockAdvancedParameterValidationText.Visibility = Visibility.Visible;
            SetDockDraftUiState("请修正参数", true);
            DockFooterText.Text = "请修改标红字段";
            // Reveal only when this particular field is in a folded advanced row.
            Grid grid = field.Parent as Grid;
            if (grid != null && Grid.GetRow(field) < grid.RowDefinitions.Count)
            {
                RowDefinition row = grid.RowDefinitions[Grid.GetRow(field)];
                foreach (DockAdvancedParameterRowState state in dockAdvancedParameterRows)
                {
                    if (!ReferenceEquals(state.Row, row)) continue;
                    dockAdvancedParameterExpandedByKind[dockEditingTool.Kind] = true;
                    UpdateDockAdvancedParameterLayout(dockEditingTool);
                    break;
                }
            }
            SelectCentralConfigurationTaskS77(DockParametersTaskTab, "Parameters");
            DockConfigurationTabs.UpdateLayout(); field.BringIntoView();
            if (moduleParameterWindowS89 != null && moduleParameterWindowS89.ShowActivated && moduleParameterWindowS89.IsActive)
                field.Focus();
            return false;
        }

        private bool ReadIntegerParameterUI07(TextBox field, string name, int minimum, int maximum, bool odd, out int value)
        {
            if (int.TryParse(field.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) &&
                value >= minimum && value <= maximum && (!odd || value % 2 == 1)) return true;
            return FailParameterFieldUI07(field, name + "：请输入 " + minimum + "–" + maximum + (odd ? " 的奇数（像素）。" : " 的整数（像素）。"));
        }

        private bool ReadNumberParameterUI07(TextBox field, string name, double minimum, double maximum, bool excludeMinimum, out double value)
        {
            if (TryParseUiDouble(field.Text, out value) && !double.IsNaN(value) && !double.IsInfinity(value) &&
                (excludeMinimum ? value > minimum : value >= minimum) && value <= maximum) return true;
            return FailParameterFieldUI07(field, name + "：请输入" + (excludeMinimum ? "大于 " : "不小于 ") + minimum.ToString(CultureInfo.InvariantCulture) +
                " 且不超过 " + maximum.ToString(CultureInfo.InvariantCulture) + " 的有限数值。");
        }

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
                bool mean = FilterModeUI07() == VmImageFilterMode.Mean;
                SetDockParameterRowVisibilityS85(DockImageFilterWidthTextBox, mean);
                SetDockParameterRowVisibilityS85(DockImageFilterRadiusTextBox, !mean);
            }

            if (DockImageEnhancementModeComboBox != null)
            {
                bool linear = EnhancementModeUI07() == VmImageEnhancementMode.LinearScale;
                SetDockParameterRowVisibilityS85(DockImageEnhancementScaleTextBox, linear);
                SetDockParameterRowVisibilityS85(DockImageEnhancementMaskWidthTextBox, !linear);
                SetDockParameterRowVisibilityS85(DockImageEnhancementFactorTextBox, !linear);
            }

            if (DockImageChannelModeComboBox != null)
            {
                bool extract = ChannelModeUI07() == VmImageChannelMode.Extract;
                SetDockParameterRowVisibilityS85(DockImageChannelIndexTextBox, extract);
            }

            if (DockImageDifferenceModeComboBox != null)
            {
                bool directional = DifferenceModeUI07() != VmImageDifferenceMode.AbsoluteDifference;
                DockImageDifferenceAddLabel.Visibility = directional ? Visibility.Visible : Visibility.Collapsed;
                DockImageDifferenceAddTextBox.Visibility = directional ? Visibility.Visible : Visibility.Collapsed;
            }

            if (DockProjectiveOutputModeComboBox != null)
            {
                bool explicitSize = ProjectiveOutputModeUI07() == VmProjectiveOutputMode.ExplicitSize;
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
                bool round = NumericOperationUI07() == VmNumericCalculationOperation.Round;
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
