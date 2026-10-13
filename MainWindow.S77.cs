using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private const string FlowReorderDragFormatS77 = "HalconWinFormsDemo.FlowReorder.S77";
        private VmToolInstance flowReorderPressToolS77;
        private Point flowReorderPressPointS77;
        private int flowReorderInsertionIndexS77 = -1;
        private string flowReorderValidationReasonS77 = string.Empty;
        private int flowReorderGripDownCountS77;
        private int flowReorderGripMoveCountS77;
        private int flowReorderDragStartCountS77;
        private int flowReorderDragOverCountS77;
        private int dataCodeModelsCreatedS77;
        private int dataCodeModelsClearedS77;
        private object dockConfigurationHomeContentS77;
        private bool centralModuleConfigurationOpenS77;
        private readonly HashSet<TextBlock> technicalHelpMovedToHoverS77 = new HashSet<TextBlock>();

        private void FlowReorderGrip_PreviewMouseLeftButtonDownS77(object sender, MouseButtonEventArgs e)
        {
            if (!RequireEngineeringEditS137("拖动流程顺序")) return;
            FrameworkElement element = sender as FrameworkElement;
            VmToolInstance tool = element == null ? null : element.Tag as VmToolInstance;
            if (tool == null || isContinuousRunning || isFlowExecutionActive) return;
            flowReorderGripDownCountS77++;
            flowReorderPressToolS77 = tool;
            flowReorderPressPointS77 = e.GetPosition(FlowGraphViewport);
            element.CaptureMouse();
            e.Handled = true;
        }

        private void FlowReorderGrip_PreviewMouseMoveS77(object sender, MouseEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null || flowReorderPressToolS77 == null || e.LeftButton != MouseButtonState.Pressed) return;
            flowReorderGripMoveCountS77++;
            Point current = e.GetPosition(FlowGraphViewport);
            if (Math.Abs(current.X - flowReorderPressPointS77.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - flowReorderPressPointS77.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            VmToolInstance tool = flowReorderPressToolS77;
            flowReorderPressToolS77 = null;
            element.ReleaseMouseCapture();
            DataObject data = new DataObject(FlowReorderDragFormatS77, tool.ToolId);
            flowReorderDragStartCountS77++;
            DragDrop.DoDragDrop(element, data, DragDropEffects.Move);
            ClearFlowReorderVisualsS77();
            e.Handled = true;
        }

        private void FlowReorderGrip_PreviewMouseLeftButtonUpS77(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            flowReorderPressToolS77 = null;
            if (element != null && element.IsMouseCaptured) element.ReleaseMouseCapture();
            e.Handled = true;
        }

        private bool IsFlowReorderDragS77(DragEventArgs e)
        {
            return e != null && e.Data != null && e.Data.GetDataPresent(FlowReorderDragFormatS77);
        }

        private bool TryHandleFlowReorderDragOverS77(DragEventArgs e)
        {
            if (!IsFlowReorderDragS77(e)) return false;
            flowReorderDragOverCountS77++;
            string toolId = e.Data.GetData(FlowReorderDragFormatS77) as string;
            VmToolInstance movingTool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
            Point canvasPoint = e.GetPosition(FlowGraphCanvas);
            int insertionIndex = GetFlowReorderInsertionIndexS77(canvasPoint.Y);
            List<VmToolInstance> proposed;
            string reason;
            bool valid = TryBuildFlowReorderS77(movingTool, insertionIndex, out proposed, out reason);
            flowReorderInsertionIndexS77 = insertionIndex;
            flowReorderValidationReasonS77 = reason ?? string.Empty;
            ShowFlowReorderVisualS77(insertionIndex, valid, reason);
            AutoScrollFlowReorderS77(e.GetPosition(FlowGraphViewport));
            e.Effects = valid ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
            return true;
        }

        private bool TryHandleFlowReorderDropS77(DragEventArgs e)
        {
            if (!IsFlowReorderDragS77(e)) return false;
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("重排流程步骤");
            if (editLeaseS137 < 0) { ClearFlowReorderVisualsS77(); e.Effects = DragDropEffects.None; e.Handled = true; return true; }
            string toolId = e.Data.GetData(FlowReorderDragFormatS77) as string;
            VmToolInstance movingTool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
            int insertionIndex = GetFlowReorderInsertionIndexS77(e.GetPosition(FlowGraphCanvas).Y);
            List<VmToolInstance> proposed;
            string reason;
            if (!TryBuildFlowReorderS77(movingTool, insertionIndex, out proposed, out reason))
            {
                ClearFlowReorderVisualsS77();
                HeaderStatusText.Text = "不能调整顺序：" + reason;
                LogInfo("纵向拖动重排已拒绝：" + reason);
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return true;
            }
            if (ReferenceEquals(movingTool, dockEditingTool) && !ConfirmDockDraftChanges("拖动调整流程顺序"))
            {
                ClearFlowReorderVisualsS77();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return true;
            }

            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) { ClearFlowReorderVisualsS77(); e.Effects = DragDropEffects.None; e.Handled = true; return true; }
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            ApplyFlowReorderS77(proposed);
            foreach (VmToolInstance tool in flowTools) InvalidateToolRunResult(tool, "流程顺序已调整");
            ApplyFlowToolSelection(new[] { movingTool }, movingTool, true);
            CommitFlowEditHistory("纵向拖动重排：" + movingTool.InstanceName, before);
            ClearFlowReorderVisualsS77();
            RefreshUiState();
            int finalIndex = flowTools.IndexOf(movingTool) + 1;
            string message = string.Format(CultureInfo.InvariantCulture, "顺序已调整：{0} → 第 {1} 步；真实订阅保持有效，可一次撤销。", movingTool.InstanceName, finalIndex);
            HeaderStatusText.Text = message;
            LogInfo(message);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            return true;
        }

        private int GetFlowReorderInsertionIndexS77(double canvasY)
        {
            if (flowTools.Count == 0) return 0;
            for (int index = 0; index < flowTools.Count; index++)
            {
                VmToolInstance tool = flowTools[index];
                double center = tool.GraphY + Math.Max(32.0, tool.GraphHeight) / 2.0;
                if (canvasY < center) return index;
            }
            return flowTools.Count;
        }

        private List<VmToolInstance> GetFlowReorderBlockS77(VmToolInstance movingTool)
        {
            if (movingTool == null) return new List<VmToolInstance>();
            if (string.IsNullOrWhiteSpace(movingTool.GroupId)) return new List<VmToolInstance> { movingTool };
            return flowTools.Where(item => string.Equals(item.GroupId, movingTool.GroupId, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private bool TryBuildFlowReorderS77(VmToolInstance movingTool, int insertionIndex, out List<VmToolInstance> proposed, out string reason)
        {
            proposed = flowTools.ToList();
            reason = string.Empty;
            if (movingTool == null || !flowTools.Contains(movingTool)) { reason = "找不到被拖动的模块。"; return false; }
            if (movingTool.Kind == VmToolKind.SubflowInput) { reason = "调用输入是固定边界，不参与执行顺序调整。"; return false; }
            if (isContinuousRunning || isFlowExecutionActive) { reason = "流程运行期间不能调整顺序。"; return false; }
            List<VmToolInstance> moving = GetFlowReorderBlockS77(movingTool);
            if (moving.Count == 0) { reason = "没有可移动的步骤。"; return false; }
            insertionIndex = Math.Max(0, Math.Min(flowTools.Count, insertionIndex));
            if (subflowEditorsS102.Count > 0) insertionIndex = Math.Max(1, insertionIndex);
            int removedBefore = moving.Count(item => flowTools.IndexOf(item) < insertionIndex);
            int adjustedIndex = insertionIndex - removedBefore;
            proposed = flowTools.Where(item => !moving.Contains(item)).ToList();
            adjustedIndex = Math.Max(0, Math.Min(proposed.Count, adjustedIndex));
            proposed.InsertRange(adjustedIndex, moving);
            if (proposed.SequenceEqual(flowTools)) { reason = "放置位置与当前顺序相同。"; return false; }
            if (!ValidateFlowOrderDependenciesS77(proposed, out reason)) return false;
            string blockText = moving.Count == 1 ? moving[0].InstanceName : (moving[0].GroupName + "（" + moving.Count.ToString(CultureInfo.InvariantCulture) + " 步）");
            reason = string.Format(CultureInfo.InvariantCulture, "{0} 将插入为第 {1} 步；全部订阅顺序有效。", blockText, adjustedIndex + 1);
            return true;
        }

        private bool ValidateFlowOrderDependenciesS77(IList<VmToolInstance> proposed, out string reason)
        {
            reason = string.Empty;
            Dictionary<string, int> indexById = proposed.Select((tool, index) => new { tool.ToolId, Index = index })
                .Where(item => !string.IsNullOrWhiteSpace(item.ToolId))
                .ToDictionary(item => item.ToolId, item => item.Index, StringComparer.OrdinalIgnoreCase);
            foreach (VmToolInstance target in proposed)
            {
                int targetIndex = indexById[target.ToolId];
                foreach (VmToolInputBindingData binding in target.InputBindings.Where(item => item != null && !string.IsNullOrWhiteSpace(item.SourceToolId)))
                {
                    int sourceIndex;
                    if (!indexById.TryGetValue(binding.SourceToolId, out sourceIndex))
                    {
                        reason = target.InstanceName + "." + binding.TargetPortName + " 的来源模块不存在；请先修复连接。";
                        return false;
                    }
                    if (sourceIndex >= targetIndex)
                    {
                        VmToolInstance source = proposed[sourceIndex];
                        reason = source.InstanceName + "." + binding.SourcePortName + " 必须位于 " + target.InstanceName + "." + binding.TargetPortName + " 之前；此次移动会破坏真实执行依赖。";
                        return false;
                    }
                }
                string correctionId = target.Parameters == null ? string.Empty : target.Parameters.PositionCorrectionToolId;
                if (!string.IsNullOrWhiteSpace(correctionId))
                {
                    int correctionIndex;
                    if (!indexById.TryGetValue(correctionId, out correctionIndex))
                    {
                        reason = target.InstanceName + " 引用的位置修正模块不存在；请先修复位置修正来源。";
                        return false;
                    }
                    if (correctionIndex >= targetIndex)
                    {
                        reason = proposed[correctionIndex].InstanceName + " 必须位于使用它的位置修正消费者 " + target.InstanceName + " 之前。";
                        return false;
                    }
                }
            }
            return true;
        }

        private void ApplyFlowReorderS77(IList<VmToolInstance> proposed)
        {
            if (!RequireEngineeringEditS137("应用流程顺序")) return;
            for (int index = 0; index < proposed.Count; index++)
            {
                VmToolInstance expected = proposed[index];
                int current = flowTools.IndexOf(expected);
                if (current != index) flowTools.Move(current, index);
            }
        }

        private void ShowFlowReorderVisualS77(int insertionIndex, bool valid, string reason)
        {
            if (FlowReorderInsertionLineS77 == null || FlowReorderStatusBorderS77 == null) return;
            double y;
            double x;
            if (flowTools.Count == 0)
            {
                x = 24; y = 48;
            }
            else if (insertionIndex <= 0)
            {
                x = flowTools[0].GraphX; y = Math.Max(8, flowTools[0].GraphY - 10);
            }
            else if (insertionIndex >= flowTools.Count)
            {
                VmToolInstance last = flowTools[flowTools.Count - 1];
                x = last.GraphX; y = last.GraphY + Math.Max(32, last.GraphHeight) + 10;
            }
            else
            {
                VmToolInstance previous = flowTools[insertionIndex - 1];
                VmToolInstance next = flowTools[insertionIndex];
                x = Math.Min(previous.GraphX, next.GraphX);
                y = (previous.GraphY + Math.Max(32, previous.GraphHeight) + next.GraphY) / 2.0;
            }
            Brush brush = valid ? new SolidColorBrush(Color.FromRgb(15, 118, 110)) : new SolidColorBrush(Color.FromRgb(220, 38, 38));
            FlowReorderInsertionLineS77.X1 = x;
            FlowReorderInsertionLineS77.X2 = x + 280;
            FlowReorderInsertionLineS77.Y1 = y;
            FlowReorderInsertionLineS77.Y2 = y;
            FlowReorderInsertionLineS77.Stroke = brush;
            FlowReorderInsertionLineS77.Visibility = Visibility.Visible;
            FlowReorderStatusTextS77.Text = reason ?? string.Empty;
            FlowReorderStatusTextS77.Foreground = brush;
            FlowReorderStatusBorderS77.BorderBrush = brush;
            Canvas.SetLeft(FlowReorderStatusBorderS77, x + 292);
            Canvas.SetTop(FlowReorderStatusBorderS77, Math.Max(8, y - 17));
            FlowReorderStatusBorderS77.Visibility = Visibility.Visible;
        }

        private void AutoScrollFlowReorderS77(Point viewportPoint)
        {
            if (FlowGraphViewport == null) return;
            const double edge = 48.0;
            const double step = 24.0;
            if (viewportPoint.Y < edge) FlowGraphViewport.ScrollToVerticalOffset(Math.Max(0, FlowGraphViewport.VerticalOffset - step));
            else if (viewportPoint.Y > FlowGraphViewport.ViewportHeight - edge) FlowGraphViewport.ScrollToVerticalOffset(Math.Min(FlowGraphViewport.ScrollableHeight, FlowGraphViewport.VerticalOffset + step));
        }

        private void ClearFlowReorderVisualsS77()
        {
            flowReorderInsertionIndexS77 = -1;
            flowReorderValidationReasonS77 = string.Empty;
            if (FlowReorderInsertionLineS77 != null) FlowReorderInsertionLineS77.Visibility = Visibility.Collapsed;
            if (FlowReorderStatusBorderS77 != null) FlowReorderStatusBorderS77.Visibility = Visibility.Collapsed;
        }

        private void OpenCentralModuleConfigurationS77(VmToolInstance tool, bool preserveInputNavigation = false)
        {
            if (tool != null && tool.Kind == VmToolKind.SubflowInput)
            {
                HeaderStatusText.Text = "这是调用输入边界，不是执行模块；其来源在上一级调用节点的输入页修改。";
                return;
            }
            if (tool == null || CentralModuleConfigurationOverlayS77 == null || CentralModuleConfigurationHostS77 == null) return;
            if (centralModuleConfigurationOpenS77 && ReferenceEquals(dockEditingTool, tool))
            {
                ActivateModuleParameterWindowS89();
                return;
            }
            if (!ReferenceEquals(dockEditingTool, tool) && !ConfirmDockDraftChanges("切换模块配置")) return;
            if (!preserveInputNavigation) ClearInputRepairNavigationS96();
            if (imageWorkspaceExpanded) ImageWorkspaceExpandButton_Click(null, null);
            if (!ReferenceEquals(dockEditingTool, tool)) LoadDockConfigurationDraft(tool);
            if (!centralModuleConfigurationOpenS77)
            {
                dockConfigurationHomeContentS77 = DockConfigurationTab.Content;
                DockConfigurationTab.Content = null;
                CentralModuleConfigurationHostS77.Content = dockConfigurationHomeContentS77;
                CentralModuleConfigurationOverlayS77.Visibility = Visibility.Visible;
                centralModuleConfigurationOpenS77 = true;
                ShowModuleParameterWindowS89();
                SetCentralConfigurationChromeS82(true);
            }

            RightTabs.SelectedItem = ModuleResultTab;
            // The central input task replaces the compact connection editor while
            // editing a module. Clicking another connection can reopen it normally.
            selectedFlowConnectionKey = null;
            RefreshFlowPortVisualization();
            SelectCentralConfigurationTaskS77(DockParametersTaskTab, "Parameters");
            UpdateCentralModuleConfigurationTitleS77(tool);
            SynchronizeMeasurementDomainContext(tool);
            SynchronizeProjectiveGeometryContext(tool);
            RefreshDualImageInputOptions(tool);
            UpdateImageContextViewport(true);
            RefreshShapeTrainingPreviewS89(false);
            ActivateModuleParameterWindowS89();
            HeaderStatusText.Text = "已打开“" + tool.InstanceName + "”参数窗；流程位置和缩放保持不变。";
            RefreshRunShortcutHintS89();
        }

        private bool CloseCentralModuleConfigurationS77()
        {
            if (!centralModuleConfigurationOpenS77) return true;
            if (IsCurrentToolContinuousTrialRunningS87)
                StopCurrentToolContinuousTrialS87("关闭模块配置", false);
            if (!ConfirmDockDraftChanges("关闭模块配置")) return false;
            ClearCurrentToolTrialStabilityS88("关闭模块配置", false);
            CentralModuleConfigurationHostS77.Content = null;
            DockConfigurationTab.Content = dockConfigurationHomeContentS77;
            dockConfigurationHomeContentS77 = null;
            CentralModuleConfigurationOverlayS77.Visibility = Visibility.Collapsed;
            centralModuleConfigurationOpenS77 = false;
            ClearInputRepairNavigationS96();
            SetCentralConfigurationChromeS82(false);
            HideModuleParameterWindowS89();
            SynchronizeMeasurementDomainContext(GetImageTaskToolS89());
            SynchronizeProjectiveGeometryContext(GetImageTaskToolS89());
            RefreshDualImageInputOptions(GetImageTaskToolS89());
            UpdateImageContextViewport(true);
            RightTabs.SelectedItem = ModuleResultTab;
            HeaderStatusText.Text = "参数窗已关闭；流程布局保持不变。";
            RefreshRunShortcutHintS89();
            return true;
        }

        private void CloseCentralModuleConfigurationButton_ClickS77(object sender, RoutedEventArgs e)
        {
            CloseCentralModuleConfigurationS77();
        }

        private void CentralParametersButton_ClickS77(object sender, RoutedEventArgs e)
        {
            DockInstanceParameterGroup.Visibility = Visibility.Collapsed;
            SelectCentralConfigurationTaskS77(DockParametersTaskTab, "Parameters");
        }

        private void CentralInputsButton_ClickS77(object sender, RoutedEventArgs e)
        {
            SelectCentralConfigurationTaskS77(DockIoTaskTab, "Io");
        }

        private void CentralRoiButton_ClickS77(object sender, RoutedEventArgs e)
        {
            SelectCentralConfigurationTaskS77(DockRoiTaskTab, "Roi");
        }

        private void CentralResultsButton_ClickS77(object sender, RoutedEventArgs e)
        {
            SelectCentralConfigurationTaskS77(DockResultTaskTab, "Result");
        }

        private void SelectCentralConfigurationTaskS77(TabItem taskTab, string activeTask)
        {
            if (DockConfigurationTabs == null || taskTab == null) return;
            DockConfigurationTabs.SelectedItem = taskTab;
            StyleCentralConfigurationTaskButtonS77(CentralParametersButtonS77, string.Equals(activeTask, "Parameters", StringComparison.Ordinal));
            StyleCentralConfigurationTaskButtonS77(CentralInputsButtonS77, string.Equals(activeTask, "Io", StringComparison.Ordinal));
            StyleCentralConfigurationTaskButtonS77(CentralRoiButtonS77, string.Equals(activeTask, "Roi", StringComparison.Ordinal));
            StyleCentralConfigurationTaskButtonS77(CentralResultsButtonS77, string.Equals(activeTask, "Result", StringComparison.Ordinal));
        }

        private static void StyleCentralConfigurationTaskButtonS77(Button button, bool active)
        {
            if (button == null) return;
            button.Background = new SolidColorBrush(active ? Color.FromRgb(15, 118, 110) : Color.FromRgb(248, 250, 252));
            button.Foreground = new SolidColorBrush(active ? Colors.White : Color.FromRgb(51, 65, 85));
            button.BorderBrush = new SolidColorBrush(active ? Color.FromRgb(13, 148, 136) : Color.FromRgb(203, 213, 225));
            button.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void UpdateCentralModuleConfigurationTitleS77(VmToolInstance tool)
        {
            RefreshInputRepairNavigationS96();
            if (CentralModuleConfigurationTitleS77 == null) return;
            CentralModuleConfigurationTitleS77.Text = tool == null
                ? "选择模块"
                : "#" + tool.SequenceText + "  " + tool.InstanceName;
            if (CentralModuleConfigurationContextS82 != null)
            {
                CentralModuleConfigurationContextS82.Text = tool == null
                    ? "双击流程模块打开参数"
                    : tool.DisplayType + "  ·  " + tool.ConfigurationStatus + "  ·  " + tool.ResultCode + "  ·  " + tool.ElapsedText;
            }
        }

        private void MoveInlineTechnicalHelpToHoverS77(FrameworkElement activePanel)
        {
            if (activePanel == null) return;
            foreach (TextBlock textBlock in EnumerateLogicalTree(activePanel, true).OfType<TextBlock>().ToList())
            {
                string help = textBlock.Text == null ? string.Empty : textBlock.Text.Trim();
                if (technicalHelpMovedToHoverS77.Contains(textBlock) ||
                    !string.IsNullOrWhiteSpace(textBlock.Name) ||
                    help.Length < 28 ||
                    !IsMutedTechnicalHelpBrushS77(textBlock.Foreground)) continue;

                DependencyObject current = textBlock;
                GroupBox group = null;
                while (current != null && !ReferenceEquals(current, activePanel))
                {
                    group = current as GroupBox;
                    if (group != null) break;
                    current = LogicalTreeHelper.GetParent(current);
                }
                if (group == null) continue;

                string existing = group.ToolTip as string;
                if (string.IsNullOrWhiteSpace(existing)) group.ToolTip = help;
                else if (existing.IndexOf(help, StringComparison.Ordinal) < 0) group.ToolTip = existing + Environment.NewLine + Environment.NewLine + help;
                textBlock.Visibility = Visibility.Collapsed;
                technicalHelpMovedToHoverS77.Add(textBlock);
            }
        }

        private static bool IsMutedTechnicalHelpBrushS77(Brush brush)
        {
            SolidColorBrush solid = brush as SolidColorBrush;
            if (solid == null) return false;
            Color color = solid.Color;
            return color == Color.FromRgb(100, 116, 139) || color == Color.FromRgb(71, 85, 105);
        }

        private void RefreshDataCodeConnectionStatusS77(VmToolInstance tool)
        {
            if (tool == null) return;
            bool ready = HasToolInputImage(tool, "Image", false);
            tool.ConfigurationStatus = ready ? "就绪" : "待连接图像";
            tool.ConnectionStatus = ready ? "Image 已连接 · 有值" : "Image 必须连接前序图像输出";
            tool.ConnectionSummary = GetImageInputSummary(tool) + " → 解码文本 / 数量 / Pose2D / XLD";
        }

        private InspectionRecord RunDataCode2DRecognitionToolS77(VmToolInstance tool, HImage inputImage, HRegion roiRegion)
        {
            if (inputImage == null) throw new InvalidOperationException("二维代码识别需要有效的 Image 输入。");
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.DataCode2DRecognition);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            HImage gray = null;
            HObject reduced = null;
            HObject symbols = null;
            HTuple modelHandle = null;
            HTuple resultHandles = null;
            HTuple decodedStrings = null;
            HTuple rows = null;
            HTuple columns = null;
            HTuple phis = null;
            HTuple length1 = null;
            HTuple length2 = null;
            bool modelCreated = false;
            try
            {
                int channels = imageService.GetChannelCount(inputImage);
                if (channels <= 0) throw new InvalidOperationException("二维代码输入图像为空或通道无效。");
                gray = channels == 1 ? inputImage.CopyImage() : CreateGrayImage(inputImage);
                HObject searchImage = gray;
                if (roiRegion != null)
                {
                    HOperatorSet.ReduceDomain(gray, roiRegion, out reduced);
                    searchImage = reduced;
                }

                HOperatorSet.CreateDataCode2dModel(parameters.DataCodeType, "default_parameters", parameters.DataCodeRecognitionProfile, out modelHandle);
                modelCreated = true;
                dataCodeModelsCreatedS77++;
                HOperatorSet.SetDataCode2dParam(modelHandle, "polarity", parameters.DataCodePolarity);
                HOperatorSet.SetDataCode2dParam(modelHandle, "timeout", parameters.DataCodeTimeoutMilliseconds);
                HOperatorSet.FindDataCode2d(searchImage, out symbols, modelHandle,
                    "stop_after_result_num", parameters.DataCodeMaxResults, out resultHandles, out decodedStrings);

                HTuple objectCountTuple;
                HOperatorSet.CountObj(symbols, out objectCountTuple);
                int contourCount = objectCountTuple == null || objectCountTuple.Length == 0 ? 0 : objectCountTuple.I;
                if (objectCountTuple != null) objectCountTuple.Dispose();
                int codeCount = decodedStrings == null ? 0 : decodedStrings.Length;
                List<string> texts = new List<string>();
                for (int index = 0; index < codeCount; index++) texts.Add(decodedStrings[index].S ?? string.Empty);

                double centerRow = 0;
                double centerColumn = 0;
                double angleDegrees = 0;
                string contextId = GetImageCoordinateContextIdS65(tool, "Image");
                if (contourCount > 0)
                {
                    HOperatorSet.SmallestRectangle2Xld(symbols, out rows, out columns, out phis, out length1, out length2);
                    if (rows != null && rows.Length > 0 && columns != null && columns.Length > 0 && phis != null && phis.Length > 0)
                    {
                        centerRow = rows[0].D;
                        centerColumn = columns[0].D;
                        angleDegrees = phis[0].D * 180.0 / Math.PI;
                        if (!IsFiniteS77(centerRow) || !IsFiniteS77(centerColumn) || !IsFiniteS77(angleDegrees))
                            throw new InvalidOperationException("HALCON 返回了非有限的二维代码中心或方向。");
                        tool.SetOutputValue("CenterPoint", VmPoint2D.CreateImagePixel(centerRow, centerColumn, contextId));
                        tool.SetOutputValue("Pose", VmPose2D.CreateImagePixel(centerRow, centerColumn, angleDegrees, contextId));
                    }
                }

                string resultCode = codeCount > 0 ? "OK" : "NG";
                string decodedText = texts.FirstOrDefault() ?? string.Empty;
                string decodedTextSet = string.Join(Environment.NewLine, texts);
                string parameterSummary = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=create/find/clear_data_code_2d_model, Type={0}, Profile={1}, Polarity={2}, MaxResults={3}, Timeout={4}ms, Channels={5}->1, Domain={6}",
                    parameters.DataCodeType, parameters.DataCodeRecognitionProfile, parameters.DataCodePolarity,
                    parameters.DataCodeMaxResults, parameters.DataCodeTimeoutMilliseconds, channels,
                    roiRegion == null ? "FullImage" : "ReducedDomain(ROI-Mask)");
                string diagnostics = codeCount > 0
                    ? string.Format(CultureInfo.InvariantCulture, "Decoded={0}, Contours={1}, ResultHandles={2}", codeCount, contourCount, resultHandles == null ? 0 : resultHandles.Length)
                    : "未识别到代码；请检查代码类型、极性、清晰度、运行域和超时。";

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(symbols, tool.ToolId, tool.InstanceName, "XLD"));
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
                ReplaceToolOverlayContours(symbols, tool, "XLD", contourCount, resultCode, codeCount);

                string message = codeCount > 0
                    ? string.Format(CultureInfo.InvariantCulture, "HALCON 二维代码识别完成：Type={0}，Count={1}，Text={2}，Center=({3:0.###},{4:0.###})，Angle={5:0.###}°。", parameters.DataCodeType, codeCount, decodedText, centerRow, centerColumn, angleDegrees)
                    : "HALCON 二维代码识别未检出结果：" + parameterSummary;
                InspectionRecord record = CreateRecord("DataCode2DRecognition", resultCode, codeCount, message, tool);
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
                        HOperatorSet.ClearDataCode2dModel(modelHandle);
                        dataCodeModelsClearedS77++;
                    }
                    catch (Exception clearError)
                    {
                        LogInfo("二维代码模型释放失败：" + clearError.Message);
                    }
                }
                if (rows != null) rows.Dispose();
                if (columns != null) columns.Dispose();
                if (phis != null) phis.Dispose();
                if (length1 != null) length1.Dispose();
                if (length2 != null) length2.Dispose();
                if (decodedStrings != null) decodedStrings.Dispose();
                if (resultHandles != null) resultHandles.Dispose();
                if (modelHandle != null) modelHandle.Dispose();
                DisposeObject(symbols);
                DisposeObject(reduced);
                if (gray != null) gray.Dispose();
            }
        }

        private static bool IsFiniteS77(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
