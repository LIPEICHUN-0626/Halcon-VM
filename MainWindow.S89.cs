using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private Window moduleParameterWindowS89;
        private bool closingModuleParameterWindowS89;
        private string shapeDraftModelPathUI02;
        private string shapePreviewIdentityS89;
        private HImage shapePreviewGlobalImageS89;
        private int shapePreviewWidthS89;
        private int shapePreviewHeightS89;
        private Rect? shapeTrainingDraftS89;
        private Point? shapeTrainingStartS89;

        // Reparent the existing editor, not a second copy of its draft or state.
        // In particular, do not call S81's column/zoom/layout transformation.
        private void ShowModuleParameterWindowS89()
        {
            if (moduleParameterWindowS89 != null) { ActivateModuleParameterWindowS89(); return; }
            MainWorkspaceGrid.Children.Remove(CentralModuleConfigurationOverlayS77);
            Rect area = SystemParameters.WorkArea;
            moduleParameterWindowS89 = new Window
            {
                Owner = this, ShowActivated = this.ShowActivated, Title = "模块参数", Width = Math.Min(520, area.Width),
                Height = Math.Min(480, area.Height), MinWidth = 460, MinHeight = 420,
                ShowInTaskbar = false, WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.CanResizeWithGrip, WindowStartupLocation = WindowStartupLocation.Manual,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };
            moduleParameterWindowS89.Resources.MergedDictionaries.Add(Resources);
            moduleParameterWindowS89.Content = CentralModuleConfigurationOverlayS77;
            moduleParameterWindowS89.Left = Math.Max(area.Left, Math.Min(area.Right - moduleParameterWindowS89.Width, Left + ActualWidth - moduleParameterWindowS89.Width - 28));
            moduleParameterWindowS89.Top = Math.Max(area.Top, Math.Min(area.Bottom - moduleParameterWindowS89.Height, Top + 64));
            if (!ShowActivated)
            {
                // Non-activating hosts (including isolated render diagnostics) keep their child off-screen too.
                moduleParameterWindowS89.Left = Left;
                moduleParameterWindowS89.Top = Top;
            }
            moduleParameterWindowS89.PreviewKeyDown += ModuleParameterWindow_PreviewKeyDownS89;
            moduleParameterWindowS89.Closing += delegate(object sender, System.ComponentModel.CancelEventArgs args)
            {
                if (closingModuleParameterWindowS89) return;
                args.Cancel = true;
                // Close outside the native Closing callback: draft confirmation can cancel.
                Dispatcher.BeginInvoke(new Action(delegate { CloseCentralModuleConfigurationS77(); }));
            };
            moduleParameterWindowS89.Show();
        }

        private void ActivateModuleParameterWindowS89()
        {
            if (moduleParameterWindowS89 != null && moduleParameterWindowS89.ShowActivated)
                moduleParameterWindowS89.Activate();
        }

        private void ModuleParameterWindow_PreviewKeyDownS89(object sender, KeyEventArgs e)
        {
            if (TryHandleModuleParameterShortcutS89(e.Key, Keyboard.Modifiers))
            { e.Handled = true; return; }
            if (TryHandleRunShortcutS89(e.Key, Keyboard.Modifiers))
            { e.Handled = true; return; }
            // The parameter window is a separate task. Never forward flow-wide
            // F9/F10, delete or clipboard commands to a different selected node.
            // F10 is scoped to an already paused subflow trial, never a new parent run.
            if (e.Key == Key.F10 && subflowTrialActiveS102 && isPauseRequested && !isStopRequested)
            {
                DebugStepButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (colorPickingToolS129 != null)
                { DockColorCancelPickS129_Click(sender, e); e.Handled = true; return; }
                if (profileEditingToolS93 != null)
                {
                    EndProfileEditS93(true);
                    e.Handled = true;
                    return;
                }
                CloseCentralModuleConfigurationS77();
                e.Handled = true;
            }
        }

        private bool TryHandleRunShortcutS89(Key key, ModifierKeys modifiers)
        {
            if (key != Key.F5) return false;
            if (modifiers == ModifierKeys.Shift)
            { StopRunButton_Click(null, null); return true; }
            if (modifiers != ModifierKeys.None) return false;
            if (historicalImageMode)
            { HeaderStatusText.Text = "历史回看为只读，请先返回实时画面再运行。"; return true; }
            if (centralModuleConfigurationOpenS77)
            {
                bool pausedCurrentSubflow = subflowTrialActiveS102 && isPauseRequested && !isStopRequested;
                if (isContinuousRunning || isImageQueueBatchRunning ||
                    System.Threading.Volatile.Read(ref tcpTriggerPendingS104) != 0 || isFlowExecutionActive && !pausedCurrentSubflow)
                    HeaderStatusText.Text = "任务正在执行；参数窗F5只控制当前草稿，请停止或等待完成。";
                else DockTrialRunButton_Click(null, null);
            }
            else if (isFlowExecutionActive && isPauseRequested)
                DebugContinueButton_Click(null, null);
            else if (isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87 || System.Threading.Volatile.Read(ref tcpTriggerPendingS104) != 0)
                HeaderStatusText.Text = "任务正在执行，请停止或等待完成；本次快捷键不会切换运行范围。";
            else if (flowTools.Any(tool => tool.IsEnabled))
                RunOnceButton_Click(null, null);
            else HeaderStatusText.Text = "当前流程没有启用模块，请先添加并配置工具。";
            return true;
        }

        private bool CanBeginManualRunCommandS89()
        {
            if (historicalImageMode)
            { HeaderStatusText.Text = "历史回看为只读，请先返回实时画面再运行。"; return false; }
            if (isContinuousRunning || isFlowExecutionActive || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87 || System.Threading.Volatile.Read(ref tcpTriggerPendingS104) != 0)
            { HeaderStatusText.Text = "已有任务正在执行；请明确停止并等待退出后再选择运行范围，不自动切换。"; return false; }
            return true;
        }

        private void RefreshRunShortcutHintS89()
        {
            if (FlowRunShortcutHintS89 == null) return;
            FlowRunShortcutHintS89.Text = centralModuleConfigurationOpenS77 && dockEditingTool != null
                ? "F5 试运行：" + dockEditingTool.InstanceName
                : isFlowExecutionActive && isPauseRequested ? "F5 继续当前流程" : "F5 运行当前流程";
        }

        private bool TryHandleModuleParameterShortcutS89(Key key, ModifierKeys modifiers)
        {
            if (modifiers != ModifierKeys.Control || !centralModuleConfigurationOpenS77 || dockEditingTool == null) return false;
            if (key == Key.D1 || key == Key.NumPad1) SelectCentralConfigurationTaskS77(DockParametersTaskTab, "Parameters");
            else if (key == Key.D2 || key == Key.NumPad2) SelectCentralConfigurationTaskS77(DockIoTaskTab, "Io");
            else if (key == Key.D3 || key == Key.NumPad3) SelectCentralConfigurationTaskS77(DockRoiTaskTab, "Roi");
            else if (key == Key.D4 || key == Key.NumPad4) SelectCentralConfigurationTaskS77(DockResultTaskTab, "Result");
            else if (key == Key.Enter) DockApplyButton_Click(null, null);
            else return false;
            return true;
        }

        private void HideModuleParameterWindowS89()
        {
            CancelVariationTrainingS130();
            ClearNccPreviewS131();
            ClearCameraRoiPreviewS128();
            EndColorPickS129();
            EndProfileEditS93(false);
            ClearShapeTrainingPreviewS89();
            if (moduleParameterWindowS89 == null) return;
            closingModuleParameterWindowS89 = true;
            try
            {
                moduleParameterWindowS89.Content = null;
                moduleParameterWindowS89.Close();
                moduleParameterWindowS89 = null;
                MainWorkspaceGrid.Children.Add(CentralModuleConfigurationOverlayS77);
            }
            finally { closingModuleParameterWindowS89 = false; }
        }

        private void ClearShapeTrainingPreviewS89()
        {
            shapePreviewIdentityS89 = null;
            shapePreviewGlobalImageS89 = null;
            shapePreviewWidthS89 = shapePreviewHeightS89 = 0;
            shapeTrainingDraftS89 = null;
            shapeTrainingStartS89 = null;
            ShapeTrainingPreviewS89.ReleaseMouseCapture();
            ShapeTrainingImageS89.Source = null;
            ShapeTrainingRectangleS89.Visibility = Visibility.Collapsed;
        }

        private string GetShapePreviewIdentityS89(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool.InputBindings.FirstOrDefault(item => item.TargetPortName == "Image");
            VmToolInstance source = binding == null ? null : GetInputSourceTool(binding);
            return tool.ToolId + ":" + (source == null ? "global:" + currentImagePath : source.ToolId + ":" + source.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture));
        }

        private void ShapeReadInputButton_ClickS89(object sender, RoutedEventArgs e)
        {
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.ShapeMatch) return;
            if (!CanChangeRecipeS134()) return;
            if (!ShapeInputMatchesDraftUI02(dockEditingTool))
            { ShapeTrainingStatusS89.Text = "输入来源有草稿改动，请明确应用后再读取；其他参数不会自动应用。"; return; }
            RefreshShapeTrainingPreviewS89(true);
        }

        private bool ShapeInputMatchesDraftUI02(VmToolInstance tool)
        {
            var row = dockInputPortRows.FirstOrDefault(candidate => candidate.PortName == "Image");
            var port = ToolMetadata.GetInstanceInputPorts(tool).FirstOrDefault(candidate => candidate.PortName == "Image");
            return row != null && port != null && row.SelectedSourceKey == GetDockSavedInputSourceKey(tool, port);
        }

        private void RefreshShapeTrainingPreviewS89(bool readSource)
        {
            VmToolInstance tool = dockEditingTool;
            ClearShapeTrainingPreviewS89();
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch) return;
            long readLeaseUI02 = readSource ? CaptureEngineeringEditLeaseS137("读取Shape模板制作输入") : -1;
            if (readSource && readLeaseUI02 < 0) return;
            ShapeTrainingExpanderS89.IsExpanded = !HasShapeModelResource(tool) || readSource;
            try
            {
                VmToolInputBindingData binding = tool.InputBindings.FirstOrDefault(item => item.TargetPortName == "Image");
                VmToolInstance source = binding == null ? null : GetInputSourceTool(binding);
                if (readSource && source != null && source.Kind == VmToolKind.ImageSource)
                {
                    if (!source.IsEnabled) throw new InvalidOperationException("图像源已停用，请先启用。");
                    ExecuteFlowTool(source, "模板制作读取输入");
                }
                if (readSource && !IsEngineeringEditLeaseCurrentS137(readLeaseUI02))
                { ShapeTrainingStatusS89.Text = "工程授权已改变，未更新训练预览。"; return; }
                using (HImage input = CreateToolInputImage(tool))
                {
                    HObject mono = null, display = null;
                    try
                    {
                        if (imageService.GetChannelCount(input) == 3) HOperatorSet.Rgb1ToGray(input, out mono);
                        else if (imageService.GetChannelCount(input) == 1) HOperatorSet.CopyImage(input, out mono);
                        else throw new InvalidOperationException("模板预览支持单通道或 RGB 图像。");
                        HOperatorSet.ConvertImageType(mono, out display, "byte");
                        using (HImage gray = new HImage(display))
                        {
                            string type;
                            IntPtr pointer = gray.GetImagePointer1(out type, out shapePreviewWidthS89, out shapePreviewHeightS89);
                            BitmapSource bitmap = BitmapSource.Create(shapePreviewWidthS89, shapePreviewHeightS89, 96, 96,
                                PixelFormats.Gray8, null, pointer, checked(shapePreviewWidthS89 * shapePreviewHeightS89), shapePreviewWidthS89);
                            bitmap.Freeze();
                            ShapeTrainingImageS89.Source = bitmap;
                        }
                    }
                    finally { if (display != null) display.Dispose(); if (mono != null) mono.Dispose(); }
                }
                shapePreviewIdentityS89 = GetShapePreviewIdentityS89(tool);
                shapePreviewGlobalImageS89 = currentImage;
                ShapeTrainingStatusS89.Text = "拖动框选目标，再点“制作模板”。预览为灰度。";
            }
            catch (Exception ex)
            {
                ClearShapeTrainingPreviewS89();
                ShapeTrainingStatusS89.Text = "输入未就绪：" + ex.Message;
                if (readSource) LogInfo("模板制作读取输入失败：" + ex.Message);
            }
        }

        private Rect ShapePreviewImageBoundsS89()
        {
            if (shapePreviewWidthS89 < 1 || shapePreviewHeightS89 < 1) return Rect.Empty;
            double scale = Math.Min(ShapeTrainingPreviewS89.ActualWidth / shapePreviewWidthS89, ShapeTrainingPreviewS89.ActualHeight / shapePreviewHeightS89);
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) return Rect.Empty;
            double width = shapePreviewWidthS89 * scale, height = shapePreviewHeightS89 * scale;
            return new Rect((ShapeTrainingPreviewS89.ActualWidth - width) / 2, (ShapeTrainingPreviewS89.ActualHeight - height) / 2, width, height);
        }

        private Point ShapePreviewPixelS89(Point position, Rect bounds)
        {
            return new Point(Math.Max(0, Math.Min(shapePreviewWidthS89 - 1, (position.X - bounds.X) * shapePreviewWidthS89 / bounds.Width)),
                Math.Max(0, Math.Min(shapePreviewHeightS89 - 1, (position.Y - bounds.Y) * shapePreviewHeightS89 / bounds.Height)));
        }

        private void ShapeTrainingPreview_MouseDownS89(object sender, MouseButtonEventArgs e)
        {
            if (!RequireEngineeringEditS137("框选Shape模板")) return;
            Rect bounds = ShapePreviewImageBoundsS89();
            Point position = e.GetPosition(ShapeTrainingPreviewS89);
            if (bounds.IsEmpty || !bounds.Contains(position) || IsCurrentToolContinuousTrialRunningS87) return;
            shapeTrainingStartS89 = ShapePreviewPixelS89(position, bounds);
            ShapeTrainingPreviewS89.CaptureMouse();
            e.Handled = true;
        }

        private void ShapeTrainingPreview_MouseMoveS89(object sender, MouseEventArgs e)
        {
            if (!shapeTrainingStartS89.HasValue || e.LeftButton != MouseButtonState.Pressed) return;
            Rect bounds = ShapePreviewImageBoundsS89();
            if (bounds.IsEmpty) return;
            shapeTrainingDraftS89 = new Rect(shapeTrainingStartS89.Value, ShapePreviewPixelS89(e.GetPosition(ShapeTrainingPreviewS89), bounds));
            DrawShapeTrainingRectangleS89();
            e.Handled = true;
        }

        private void ShapeTrainingPreview_MouseUpS89(object sender, MouseButtonEventArgs e)
        {
            if (!shapeTrainingStartS89.HasValue) return;
            Rect bounds = ShapePreviewImageBoundsS89();
            if (!bounds.IsEmpty)
                shapeTrainingDraftS89 = new Rect(shapeTrainingStartS89.Value, ShapePreviewPixelS89(e.GetPosition(ShapeTrainingPreviewS89), bounds));
            shapeTrainingStartS89 = null;
            ShapeTrainingPreviewS89.ReleaseMouseCapture();
            DrawShapeTrainingRectangleS89();
            ShapeTrainingStatusS89.Text = "框选尚未保存；点击“制作模板”后生效。";
            e.Handled = true;
        }

        private void ShapeTrainingPreview_SizeChangedS89(object sender, SizeChangedEventArgs e) { DrawShapeTrainingRectangleS89(); }

        private void DrawShapeTrainingRectangleS89()
        {
            if (ShapeTrainingRectangleS89 == null) return;
            Rect bounds = ShapePreviewImageBoundsS89();
            if (!shapeTrainingDraftS89.HasValue || bounds.IsEmpty) { ShapeTrainingRectangleS89.Visibility = Visibility.Collapsed; return; }
            Rect roi = shapeTrainingDraftS89.Value;
            Canvas.SetLeft(ShapeTrainingRectangleS89, bounds.X + roi.X * bounds.Width / shapePreviewWidthS89);
            Canvas.SetTop(ShapeTrainingRectangleS89, bounds.Y + roi.Y * bounds.Height / shapePreviewHeightS89);
            ShapeTrainingRectangleS89.Width = roi.Width * bounds.Width / shapePreviewWidthS89;
            ShapeTrainingRectangleS89.Height = roi.Height * bounds.Height / shapePreviewHeightS89;
            ShapeTrainingRectangleS89.Visibility = Visibility.Visible;
        }

        private void ShapeMakeTemplateButton_ClickS89(object sender, RoutedEventArgs e)
        {
            if (!shapeTrainingDraftS89.HasValue)
            {
                if (string.IsNullOrWhiteSpace(DockShapeTrainingRoiComboBox.SelectedValue as string))
                {
                    ShapeTrainingExpanderS89.IsExpanded = true;
                    ShapeTrainingStatusS89.Text = "先读取输入图像，再拖动框选要制作的模板。";
                    return;
                }
                DockShapeTrainButton_Click(sender, e);
                return;
            }
            RunUiAction("制作模板", delegate { TrainShapeFromPreviewS89(); });
        }

        private void TrainShapeFromPreviewS89()
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("训练框选Shape模板");
            if (editLeaseS137 < 0) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch || !shapeTrainingDraftS89.HasValue) throw new InvalidOperationException("请先框选模板。");
            if (isFlowExecutionActive || isContinuousRunning || IsCurrentToolContinuousTrialRunningS87) throw new InvalidOperationException("请先停止运行再制作模板。");
            Rect rectangle = shapeTrainingDraftS89.Value;
            if (rectangle.Width < 8 || rectangle.Height < 8) throw new InvalidOperationException("模板框选范围太小，宽高至少为 8 个图像像素。");
            if (!CanChangeRecipeS134() || !ShapeInputMatchesDraftUI02(tool)) throw new InvalidOperationException("请等待当前任务退出，并先明确应用 Image 来源改动；制作模板不自动提交参数。");
            if (shapePreviewIdentityS89 != GetShapePreviewIdentityS89(tool) ||
                (!tool.InputBindings.Any(item => item.TargetPortName == "Image") && !ReferenceEquals(shapePreviewGlobalImageS89, currentImage)))
                throw new InvalidOperationException("输入图像已更换，请重新读取并框选，避免用旧图位置制作模板。");
            VmToolParameterData p = tool.Parameters.Clone();
            string draftError;
            if (!TryReadShapeDockDraftFields(p, out draftError)) throw new InvalidOperationException(draftError);
            draftError = p.Validate(VmToolKind.ShapeMatch);
            if (!string.IsNullOrWhiteSpace(draftError)) throw new InvalidOperationException(draftError);
            using (RoiData geometry = RoiData.CreateRectangle(rectangle.Top, rectangle.Left, rectangle.Bottom, rectangle.Right))
                TrainShapeGeometryDraftUI02(tool, geometry, p, editLeaseS137);
            shapeTrainingDraftS89 = null;
            DockShapeTrainingRoiComboBox.SelectedValue = null;
            ShapeTrainingExpanderS89.IsExpanded = false;
            DrawShapeTrainingRectangleS89();
        }

        private void TrainShapeGeometryDraftUI02(VmToolInstance tool, RoiData geometry, VmToolParameterData p, long editLeaseS137)
        {
            using (HImage input = CreateToolInputImage(tool))
            using (TemplateDefinition definition = new TemplateDefinition
            {
                TemplateName = p.ShapeModelName, TemplateRoi = geometry.Clone(), TrainingMask = TemplateDefinition.CloneRegion(geometry.Region),
                DisplayFrame = geometry.Clone(), Options = CreateShapeOptions(p)
            })
            {
                TemplateItem item = new TemplateItem { Name = p.ShapeModelName, TemplateRoi = geometry.Clone(), TrainingMask = TemplateDefinition.CloneRegion(geometry.Region), DisplayFrame = geometry.Clone(), Options = CreateShapeOptions(p) };
                try
                {
                    item.Service.Train(input, definition);
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    // A new resource version preserves older recipes and undo references.
                    string path = CreateShapeModelVersionPathS89(tool);
                    item.Service.Save(path);
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    shapeDraftModelPathUI02 = path;
                }
                finally { if (item != null) item.Dispose(); }
            }
            DockDraftValue_Changed(null, null);
            UpdateShapeModelStateText(tool);
            ShapeTrainingStatusS89.Text = "新模板进入草稿：F5 试运行，确认后应用。";
            HeaderStatusText.Text = ShapeTrainingStatusS89.Text;
            LogInfo(tool.InstanceName + " 的新 Shape 模板已保存为独立草稿版本；未替换已应用参数或模型。");
        }

        private void RestoreShapeResourceAfterDraftTrialUI02(VmToolInstance tool, VmToolParameterData applied)
        {
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch) return;
            TemplateItem resource;
            if (shapeToolResources.TryGetValue(tool.ToolId, out resource) && resource != null &&
                !string.Equals(resource.TemplatePath, applied.ShapeModelPath, StringComparison.OrdinalIgnoreCase))
                DisposeShapeToolResource(tool.ToolId);
        }

        private string CreateShapeModelVersionPathS89(VmToolInstance tool)
        {
            return Path.Combine(Path.GetDirectoryName(GetManagedShapeModelPath(tool)), tool.ToolId + "_" + Guid.NewGuid().ToString("N") + ".shm");
        }

        private VmToolInstance GetImageTaskToolS89()
        {
            if (centralModuleConfigurationOpenS77 && dockEditingTool != null && flowTools.Contains(dockEditingTool)) return dockEditingTool;
            return FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
        }
    }
}
