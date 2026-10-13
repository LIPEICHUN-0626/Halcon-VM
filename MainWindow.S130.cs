using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmVariationParametersS130 variationDraftS130 = new VmVariationParametersS130();
        private readonly ObservableCollection<VmVariationSampleS130> variationSamplesS130 = new ObservableCollection<VmVariationSampleS130>();
        private CancellationTokenSource variationTrainingCancellationS130;
        private int variationRevisionS130;

        private void LoadVariationExampleS130_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("载入良品学习示例")) return;
            LoadExampleProgramS78("variation-surface");
            if (lastExampleProgramKeyS78 == "variation-surface" && flowTools.Any(t => t.Kind == VmToolKind.VariationSurfaceInspection))
                HeaderStatusText.Text = "良品学习示例已装载：先运行图像源，再双击第 02 步，加入至少 3 张对齐良品→学习→应用。示例未预训练；检测 Area=0 的范围判定仅用于学习。";
        }

        private bool BuildVariationExampleS130()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 测试图像（与良品同姿态）", true, null);
            var inspection = CreateFlowTool(VmToolKind.VariationSurfaceInspection, "02 学习良品与检测缺陷", true, null);
            var contour = CreateFlowTool(VmToolKind.RegionContourMeasurement, "03 缺陷轮廓", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "04 缺陷面积判定", true, null);
            foreach (var tool in new[] { source, inspection, contour, judge }) flowTools.Add(tool);
            inspection.SetInputBinding("Image", source.ToolId, "Image"); contour.SetInputBinding("Region", inspection.ToolId, "Region");
            judge.SetInputBinding("Value", inspection.ToolId, "Area"); judge.InputToolId = inspection.ToolId; judge.InputPortName = "Area";
            judge.NumericLowerLimit = 0; judge.NumericUpperLimit = 0;
            string path = GetHalconExampleImageS78(Path.Combine("cap_illumination", "cap_illumination_01.png"));
            source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "EXAMPLE-VARIATION";
            return File.Exists(path);
        }

        private void CancelVariationTrainingS130()
        { variationRevisionS130++; if (variationTrainingCancellationS130 != null) variationTrainingCancellationS130.Cancel(); }

        private void LoadVariationDraftS130(VmToolParameterData parameters)
        {
            CancelVariationTrainingS130();
            variationDraftS130 = (parameters.VariationInspectionS130 ?? new VmVariationParametersS130()).Clone();
            variationSamplesS130.Clear();
            foreach (var sample in variationDraftS130.Samples.Where(s => s != null))
            { sample.Status = "已加入"; variationSamplesS130.Add(sample); }
            DockVariationSampleListS130.ItemsSource = variationSamplesS130;
            DockVariationAbsoluteS130.Text = variationDraftS130.AbsoluteThreshold.ToString("R", CultureInfo.InvariantCulture);
            DockVariationFactorS130.Text = variationDraftS130.VariationFactor.ToString("R", CultureInfo.InvariantCulture);
            DockVariationMinAreaS130.Text = variationDraftS130.MinimumDefectArea.ToString("R", CultureInfo.InvariantCulture);
            DockVariationAlignedS130.IsChecked = false;
            DockVariationSamplePreviewS130.Source = null;
            DockVariationSamplePreviewS130.Visibility = Visibility.Collapsed;
            DockVariationTrainingStatusS130.Text = "良品样本→学习→应用→F5 检测；来源改变须重建样本集";
            RefreshVariationTaskS130();
        }

        private bool ReadVariationDraftS130(VmToolParameterData parameters)
        {
            var p = variationDraftS130.Clone(); double value;
            if (!TryParseUiDouble(DockVariationAbsoluteS130.Text, out value)) return FailDockDraft("最小灰差必须是数值。");
            p.AbsoluteThreshold = value;
            if (!TryParseUiDouble(DockVariationFactorS130.Text, out value)) return FailDockDraft("波动倍数必须是数值。");
            p.VariationFactor = value;
            if (!TryParseUiDouble(DockVariationMinAreaS130.Text, out value)) return FailDockDraft("最小缺陷面积必须是数值。");
            p.MinimumDefectArea = value;
            p.Samples = variationSamplesS130.Select(s => s.Clone()).ToList();
            string error = p.Validate();
            if (!string.IsNullOrWhiteSpace(error)) return FailDockDraft(error);
            parameters.VariationInspectionS130 = p;
            return true;
        }

        private bool IsVariationTaskStoppedS130()
        {
            return !isFlowExecutionActive && !isContinuousRunning && !isImageQueueBatchRunning && !historicalImageMode &&
                !IsCurrentToolContinuousTrialRunningS87 && System.Threading.Volatile.Read(ref tcpTriggerPendingS104) == 0;
        }

        private bool VariationInputMatchesDraftS130(VmToolInstance tool)
        {
            var row = dockInputPortRows.FirstOrDefault(r => r.PortName == "Image");
            var port = ToolMetadata.GetInstanceInputPorts(tool).FirstOrDefault(p => p.PortName == "Image");
            return row != null && port != null && row.SelectedSourceKey == GetDockSavedInputSourceKey(tool, port);
        }

        private string VariationSourceSignatureS130(VmToolInstance tool)
        {
            var binding = tool.GetInputBinding("Image");
            return VariationSourceSignatureForBindingS130(binding);
        }

        private string VariationSourceSignatureForBindingS130(VmToolInputBindingData binding)
        {
            if (binding == null) throw new InvalidOperationException("先连接明确的单通道 Image 来源。" );
            var text = new StringBuilder();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AppendVariationSourceS130(GetInputSourceTool(binding), text, visited, 0);
            return VmVariationModelS130.HashText(binding.SourcePortName + "\n" + text);
        }

        private void AppendVariationSourceS130(VmToolInstance source, StringBuilder text, ISet<string> visited, int depth)
        {
            if (source == null || depth > 64) throw new InvalidOperationException("良品来源缺失或依赖过深。" );
            string id = GetCoordinateIdentityBaseS102(source);
            text.Append(id).Append(':').Append(source.Kind).Append(';');
            if (!visited.Add(id)) return;
            // Different frames/files from one source are expected. Processing/calibration changes are not.
            if (source.Kind == VmToolKind.ImageSource)
            {
                var p = source.Parameters ?? new VmToolParameterData();
                text.Append(p.ImageSourceModeS114).Append('|').Append(p.CameraInterfaceS114).Append('|').Append(p.CameraDeviceS114);
            }
            else
            {
                text.Append(VmVariationModelS130.HashDto(source.Parameters ?? new VmToolParameterData(), typeof(VmToolParameterData)));
                var bound = CaptureRoiLayers().Where(r => source.BoundRoiIds.Contains(r.RoiId)).Select(r => new RoiLayerRecipeData
                { RoiId = r.RoiId, IsEnabled = r.IsEnabled, IsMask = r.IsMask, ImageContextId = r.ImageContextId, Geometry = r.Geometry }).ToList();
                text.Append(VmVariationModelS130.HashDto(bound, bound.GetType()));
                foreach (var input in ToolMetadata.GetInstanceInputPorts(source).OrderBy(p => p.PortName, StringComparer.Ordinal))
                {
                    var binding = source.GetInputBinding(input.PortName);
                    if (binding == null) continue;
                    text.Append(input.PortName).Append('=').Append(binding.SourcePortName);
                    AppendVariationSourceS130(GetInputSourceTool(binding), text, visited, depth + 1);
                }
            }
        }

        private VmImageSnapshot GetVariationInputSnapshotS130(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.VariationSurfaceInspection || !IsVariationTaskStoppedS130())
                throw new InvalidOperationException("请在当前良品检测模块、停止运行后管理样本。" );
            if (!VariationInputMatchesDraftS130(tool)) throw new InvalidOperationException("图像来源尚未应用，请先应用来源设置。" );
            var binding = tool.GetInputBinding("Image");
            var source = GetInputSourceTool(binding);
            var snapshot = binding == null ? null : GetImageSnapshot(source, binding.SourcePortName);
            if (source == null || source.ResultCode != "OK" || snapshot == null || snapshot.IsDisposed)
                throw new InvalidOperationException("先运行上游，取得当前来源的单通道图像。" );
            string context = ResolveTargetImageContextIdS86(tool), signature = VariationSourceSignatureS130(tool);
            if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context))
                throw new InvalidOperationException("图像坐标来源未明确，不能建立良品样本集。" );
            if (variationSamplesS130.Count > 0 && (variationDraftS130.DatasetContext != context || variationDraftS130.DatasetSourceSignature != signature))
                throw new InvalidOperationException("来源或上游处理已改变；先清空当前草稿样本集，再建立新模型。原已应用模型不会被覆盖。" );
            using (var image = snapshot.CreateImageCopy()) { int w, h; string type; VmVariationModelS130.DescribeImage(image, out w, out h, out type); }
            variationDraftS130.DatasetContext = context; variationDraftS130.DatasetSourceSignature = signature;
            return snapshot;
        }

        private string NewVariationResourceS130(string suffix)
        {
            string directory = Path.Combine(recipeService.RecipeDirectory, "VariationModels");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, Guid.NewGuid().ToString("N") + suffix);
        }

        private void VariationDatasetChangedS130(object sender, RoutedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改良品样本集")) return;
            CancelVariationTrainingS130();
            variationDraftS130.ClearModel();
            variationDraftS130.Samples = variationSamplesS130.Select(s => s.Clone()).ToList();
            DockVariationModelStateS130.Text = "样本已改变 · 请重新训练；原已应用模型保持不变";
            DockDraftValue_Changed(sender, e);
            RefreshVariationTaskS130();
        }

        private void DockVariationCaptureS130_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("采集当前良品样本");
            if (editLeaseS137 < 0) return;
            RunUiAction("加入当前良品", delegate
            {
                var snapshot = GetVariationInputSnapshotS130(dockEditingTool);
                if (variationSamplesS130.Count >= 64) throw new InvalidOperationException("最多 64 张良品。" );
                string imagePath = NewVariationResourceS130(".tif"), domainPath = NewVariationResourceS130(".reg");
                bool retained = false;
                try
                {
                    using (var image = snapshot.CreateImageCopy())
                    using (var domain = image.GetDomain())
                    {
                        if (VmVariationModelS130.Area(domain) <= 0) throw new InvalidOperationException("良品图像有效域为空。" );
                        HOperatorSet.WriteImage(image, "tiff", 0, imagePath); HOperatorSet.WriteRegion(domain, domainPath);
                    }
                    string digest = VmVariationModelS130.HashFile(imagePath);
                    string domainDigest = VmVariationModelS130.HashFile(domainPath);
                    if (variationSamplesS130.Any(s => s.ImageSha256 == digest)) throw new InvalidOperationException("同一图像已加入，请先采集下一张不同良品。" );
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    variationSamplesS130.Add(new VmVariationSampleS130 { SampleId = Guid.NewGuid().ToString("N"), ImagePath = imagePath,
                        ImageSha256 = digest, DomainPath = domainPath, DomainSha256 = domainDigest,
                        DisplayName = "输入良品 " + (variationSamplesS130.Count + 1).ToString("00") + " · " + DateTime.Now.ToString("HH:mm:ss"), Status = "当前输入良品 · 待训练" });
                    retained = true;
                    VariationDatasetChangedS130(sender, e);
                }
                finally { if (!retained) { VmVariationModelS130.DeleteCreated(imagePath); VmVariationModelS130.DeleteCreated(domainPath); } }
            });
        }

        private void DockVariationAddFilesS130_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("导入对齐良品样本");
            if (editLeaseS137 < 0) return;
            RunUiAction("加入对齐良品", delegate
            {
                GetVariationInputSnapshotS130(dockEditingTool);
                if (DockVariationAlignedS130.IsChecked != true) throw new InvalidOperationException("请先确认外部样本为良品且已按当前输入坐标对齐。" );
                var dialog = new OpenFileDialog { Title = "加入已对齐的良品图像", Multiselect = true, CheckFileExists = true,
                    Filter = "图像 (*.bmp;*.png;*.jpg;*.tif;*.tiff)|*.bmp;*.png;*.jpg;*.tif;*.tiff|所有文件 (*.*)|*.*" };
                if (dialog.ShowDialog(moduleParameterWindowS89 ?? this) != true) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                var pending = new List<VmVariationSampleS130>();
                bool retained = false;
                try
                {
                    foreach (string path in dialog.FileNames)
                    {
                        if (variationSamplesS130.Count + pending.Count >= 64) break;
                        if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                        string digest = VmVariationModelS130.HashFile(path);
                        if (variationSamplesS130.Any(s => s.ImageSha256 == digest) || pending.Any(s => s.ImageSha256 == digest)) continue;
                        string managed = NewVariationResourceS130(Path.GetExtension(path));
                        try
                        {
                            File.Copy(path, managed, false);
                            if (VmVariationModelS130.HashFile(managed) != digest) throw new InvalidOperationException("样本复制时来源变化，请重新加入。" );
                            pending.Add(new VmVariationSampleS130 { SampleId = Guid.NewGuid().ToString("N"), ImagePath = managed,
                                ImageSha256 = digest, DisplayName = Path.GetFileName(path), Status = "外部对齐良品 · 待训练" });
                        }
                        catch { VmVariationModelS130.DeleteCreated(managed); throw; }
                    }
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    foreach (var sample in pending) variationSamplesS130.Add(sample);
                    retained = true;
                    if (pending.Count > 0) VariationDatasetChangedS130(sender, e);
                    DockVariationTrainingStatusS130.Text = pending.Count > 0 ? "已加入 " + pending.Count + " 张受管样本；训练时逐张检查尺寸/类型" : "没有新样本；最多 64 张，不重复加入相同文件内容";
                }
                finally { if (!retained) foreach (var sample in pending) VmVariationModelS130.DeleteCreated(sample.ImagePath); }
            });
        }

        private void DockVariationRemoveS130_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("移除良品样本")) return;
            if (!IsVariationTaskStoppedS130()) return;
            var selected = DockVariationSampleListS130.SelectedItem as VmVariationSampleS130;
            if (selected == null) return;
            variationSamplesS130.Remove(selected); VariationDatasetChangedS130(sender, e);
        }
        private void DockVariationClearS130_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("清空良品样本草稿")) return;
            if (!IsVariationTaskStoppedS130()) return;
            variationSamplesS130.Clear(); variationDraftS130.DatasetContext = variationDraftS130.DatasetSourceSignature = null;
            VariationDatasetChangedS130(sender, e);
        }
        private void DockVariationCancelS130_Click(object sender, RoutedEventArgs e)
        { CancelVariationTrainingS130(); DockVariationTrainingStatusS130.Text = "已请求取消；当前 HALCON 算子结束后退出，不替换模型"; }

        private void DockVariationSampleSelectionS130_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DockVariationSamplePreviewS130.Source = null; DockVariationSamplePreviewS130.Visibility = Visibility.Collapsed;
            var sample = DockVariationSampleListS130.SelectedItem as VmVariationSampleS130;
            if (sample == null || !File.Exists(sample.ImagePath)) return;
            try
            {
                var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 320; bitmap.UriSource = new Uri(sample.ImagePath, UriKind.Absolute); bitmap.EndInit(); bitmap.Freeze();
                DockVariationSamplePreviewS130.Source = bitmap; DockVariationSamplePreviewS130.Visibility = Visibility.Visible;
            }
            catch { DockVariationTrainingStatusS130.Text = "该格式无法缩略预览；训练仍使用 HALCON 原始图像"; }
        }

        private async void DockVariationTrainS130_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("学习良品变化模型");
            if (editLeaseS137 < 0) return;
            VmToolInstance tool = dockEditingTool;
            VmVariationParametersS130 draft;
            try
            {
                if (variationTrainingCancellationS130 != null) throw new InvalidOperationException("上一训练尚在退出，请稍候。" );
                var snapshot = GetVariationInputSnapshotS130(tool);
                var p = tool.Parameters.Clone(); if (!ReadVariationDraftS130(p)) return; draft = p.VariationInspectionS130;
                if (draft.Samples.Count < 3) throw new InvalidOperationException("至少加入 3 张已对齐良品。" );
                using (var image = snapshot.CreateImageCopy())
                {
                    int width, height; string type;
                    VmVariationModelS130.DescribeImage(image, out width, out height, out type);
                    draft.Width = width; draft.Height = height; draft.ImageType = type;
                }
            }
            catch (Exception ex) { DockVariationTrainingStatusS130.Text = ex.Message; return; }
            string modelPath, coveragePath;
            try { modelPath = NewVariationResourceS130(".vam"); coveragePath = NewVariationResourceS130(".reg"); }
            catch (Exception ex) { DockVariationTrainingStatusS130.Text = "无法建立受管模型目录：" + ex.Message; return; }
            int revision = variationRevisionS130;
            var cancellation = new CancellationTokenSource(); variationTrainingCancellationS130 = cancellation;
            DockVariationTrainingStatusS130.Text = "后台学习中 · 可取消；本次完成后仍须应用";
            RefreshVariationTaskS130();
            bool retained = false;
            try
            {
                var report = await Task.Run(() => VmVariationModelS130.Train(draft, modelPath, coveragePath, cancellation.Token,
                    (index, status) => Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (dockEditingTool != tool || revision != variationRevisionS130 || cancellation.IsCancellationRequested ||
                            !ReferenceEquals(variationTrainingCancellationS130, cancellation) || !IsEngineeringEditLeaseCurrentS137(editLeaseS137, false)) return;
                        variationSamplesS130[index].Status = status; DockVariationSampleListS130.Items.Refresh();
                        DockVariationTrainingStatusS130.Text = "学习 " + (index + 1) + "/" + draft.Samples.Count + " · " + status;
                    }))));
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                { DockVariationTrainingStatusS130.Text = "工程授权已改变，本次模型未进入草稿；原已应用模型保留"; return; }
                if (dockEditingTool != tool || !centralModuleConfigurationOpenS77 || revision != variationRevisionS130 || cancellation.IsCancellationRequested ||
                    !VariationInputMatchesDraftS130(tool) || ResolveTargetImageContextIdS86(tool) != draft.DatasetContext ||
                    VariationSourceSignatureS130(tool) != draft.DatasetSourceSignature || !IsVariationTaskStoppedS130())
                { DockVariationTrainingStatusS130.Text = "来源/模块/运行状态已改变，已丢弃本次模型；请重新训练"; return; }
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                variationDraftS130 = report.Parameters; retained = true;
                DockVariationTrainingStatusS130.Text = report.Summary;
                DockDraftValue_Changed(sender, e);
                LogInfo(tool.InstanceName + "：" + report.Summary + "，未改变已应用模型");
            }
            catch (OperationCanceledException) { if (dockEditingTool == tool && revision == variationRevisionS130) DockVariationTrainingStatusS130.Text = "训练已取消，旧模型保留"; }
            catch (Exception ex) { if (dockEditingTool == tool && revision == variationRevisionS130) { DockVariationTrainingStatusS130.Text = "训练失败：" + ex.Message; LogInfo(tool.InstanceName + " 良品学习失败：" + ex.Message); } }
            finally
            {
                if (!retained) { VmVariationModelS130.DeleteCreated(modelPath); VmVariationModelS130.DeleteCreated(coveragePath); }
                if (ReferenceEquals(variationTrainingCancellationS130, cancellation)) variationTrainingCancellationS130 = null;
                cancellation.Dispose(); RefreshVariationTaskS130();
            }
        }

        private void RefreshVariationTaskS130()
        {
            if (DockVariationTrainS130 == null) return;
            bool busy = variationTrainingCancellationS130 != null;
            bool stopped = IsVariationTaskStoppedS130();
            if (busy && !stopped && !variationTrainingCancellationS130.IsCancellationRequested) CancelVariationTrainingS130();
            DockVariationTrainS130.IsEnabled = !busy && stopped && variationSamplesS130.Count >= 3;
            DockVariationCaptureS130.IsEnabled = !busy && stopped && variationSamplesS130.Count < 64;
            DockVariationAddFilesS130.IsEnabled = !busy && stopped && variationSamplesS130.Count < 64;
            DockVariationCancelS130.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            DockVariationSampleCountS130.Text = variationSamplesS130.Count + "/64 张良品";
            bool model = variationDraftS130.HasModelVersion();
            DockVariationModelStateS130.Text = model ? "模型版本：" + variationDraftS130.Samples.Count + " 张 · " + variationDraftS130.Width + "×" + variationDraftS130.Height + " " + variationDraftS130.ImageType + " · 同一草稿/应用路径" : "模型未训练或样本已变化 · 至少 3 张对齐良品";
        }

        private ISet<string> CaptureCurrentVariationDatasetIdsS130()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in flowTools.Where(t => t.Kind == VmToolKind.VariationSurfaceInspection))
            {
                var p = tool.Parameters.VariationInspectionS130;
                try { if (p != null && p.DatasetContext == ResolveTargetImageContextIdS86(tool) && p.DatasetSourceSignature == VariationSourceSignatureS130(tool)) result.Add(tool.ToolId); }
                catch { }
            }
            return result;
        }

        private void RebindCopiedVariationModelsS130(IEnumerable<VmToolInstance> tools, IDictionary<string, string> idMap, ISet<string> authorizedOldIds = null)
        {
            foreach (var tool in tools.Where(t => t.Kind == VmToolKind.VariationSurfaceInspection))
            {
                var p = tool.Parameters.VariationInspectionS130;
                var binding = tool.GetInputBinding("Image");
                if (p == null || binding == null || !idMap.Values.Contains(binding.SourceToolId, StringComparer.OrdinalIgnoreCase)) continue;
                // This path is called only for an explicit configuration clone, never for an arbitrary connection edit.
                bool modelReady = p.HasModelVersion();
                try
                {
                    string oldToolId = idMap.First(pair => pair.Value == tool.ToolId).Key;
                    if (authorizedOldIds != null)
                    {
                        if (!authorizedOldIds.Contains(oldToolId)) continue;
                    }
                    else
                    {
                        string oldSourceId = idMap.First(pair => pair.Value == binding.SourceToolId).Key;
                        var originalBinding = new VmToolInputBindingData { TargetPortName = "Image", SourceToolId = oldSourceId, SourcePortName = binding.SourcePortName };
                        var oldSource = GetInputSourceTool(originalBinding);
                        if (oldSource == null || ResolveOutputCoordinateContextS102(oldSource, binding.SourcePortName) != p.DatasetContext ||
                            VariationSourceSignatureForBindingS130(originalBinding) != p.DatasetSourceSignature) continue;
                    }
                    string context = ResolveTargetImageContextIdS86(tool);
                    if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context)) continue;
                    p.DatasetContext = context; p.DatasetSourceSignature = VariationSourceSignatureS130(tool);
                    if (modelReady) p.ModelDatasetKey = p.DatasetKey();
                }
                catch { /* Unresolved scoped sources keep the old identity and fail explicitly until resampled. */ }
            }
        }

        private InspectionRecord RunVariationInspectionS130(VmToolInstance tool, HImage image)
        {
            var p = (tool.Parameters.VariationInspectionS130 ?? new VmVariationParametersS130()).Clone();
            string error = p.Validate(); if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            string context = ResolveTargetImageContextIdS86(tool);
            if (!p.HasModelVersion()) throw new InvalidOperationException("良品模型未训练或样本版本已改变，请训练并应用。" );
            if (IsPendingCoordinateContextS102(context) || context != p.DatasetContext || VariationSourceSignatureS130(tool) != p.DatasetSourceSignature)
                throw new InvalidOperationException("当前图像来源/上游处理与训练版本不一致，禁止沿用旧良品模型。" );
            int width, height; string type; VmVariationModelS130.DescribeImage(image, out width, out height, out type);
            if (width != p.Width || height != p.Height || type != p.ImageType) throw new InvalidOperationException("检测图像尺寸/类型与良品模型不一致。" );
            HTuple model = null, count = null;
            HObject compared = null, difference = null, connected = null, selected = null;
            try
            {
                using (var modelLock = VmVariationModelS130.OpenResource(p.ModelPath, p.ModelSha256))
                using (var coverageLock = VmVariationModelS130.OpenResource(p.CoveragePath, p.CoverageSha256))
                using (var coverage = VmVariationModelS130.ReadCoverage(p.CoveragePath))
                using (var imageDomain = image.GetDomain())
                using (var roi = CreateEffectiveRoiRegion(tool, false))
                using (var effective = roi == null ? imageDomain.CopyObj(1, -1) : imageDomain.Intersection(roi))
                using (var outside = effective.Difference(coverage))
                {
                    double pixelCount = VmVariationModelS130.Area(effective);
                    if (pixelCount <= 0) throw new InvalidOperationException("缺陷检测有效域为空。" );
                    if (VmVariationModelS130.Area(outside) > 0) throw new InvalidOperationException("检测 ROI 包含未被全部良品覆盖的像素，请限制 ROI 或重新采样；不静默缩小检测范围。" );
                    HOperatorSet.ReadVariationModel(p.ModelPath, out model);
                    HOperatorSet.PrepareVariationModel(model, p.AbsoluteThreshold, p.VariationFactor);
                    using (var reduced = image.ReduceDomain(effective)) HOperatorSet.CompareVariationModel(reduced, out compared, model);
                    HOperatorSet.Intersection(compared, effective, out difference);
                    double rawArea = VmVariationModelS130.Area(difference);
                    if (rawArea > 0)
                    {
                        HOperatorSet.Connection(difference, out connected);
                        HOperatorSet.SelectShape(connected, out selected, "area", "and", p.MinimumDefectArea, 16777216);
                    }
                    else HOperatorSet.GenEmptyObj(out selected);
                    HOperatorSet.CountObj(selected, out count);
                    double area = VmVariationModelS130.Area(selected), ratio = 100 * area / pixelCount;
                    if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio < 0 || ratio > 100.000001)
                        throw new InvalidOperationException("缺陷面积/比例无效，已阻止发布。" );
                    tool.SetOutputValue("Region", VmRegionSnapshot.Create(selected, tool.ToolId, tool.InstanceName, "Region"));
                    tool.SetOutputCoordinateContext("Region", context);
                    tool.SetOutputValue("Area", area); tool.SetOutputValue("AreaRatio", ratio); tool.SetOutputValue("RawArea", rawArea);
                    tool.SetOutputValue("RegionCount", (double)count.I); tool.SetOutputValue("SampleCount", (double)p.Samples.Count);
                    tool.SetOutputValue("EffectivePixels", pixelCount); tool.SetOutputValue("HasDefect", area > 0);
                    tool.SetOutputValue("ModelVersion", p.ModelSha256); tool.SetOutputValue("Parameters", "standard; Abs=" + p.AbsoluteThreshold.ToString("R", CultureInfo.InvariantCulture) + "; Var=" + p.VariationFactor.ToString("R", CultureInfo.InvariantCulture) + "; MinArea=" + p.MinimumDefectArea.ToString("R", CultureInfo.InvariantCulture));
                    ReplaceToolOverlayRegion(selected, tool, "Region", count.I, area, "OK"); selected = null;
                    string message = string.Format(CultureInfo.InvariantCulture, "良品变化检测：缺陷 {0} 个 · {1:0.###} px / {2:0.###}% · 原始差异 {3:0.###} px · {4} 张学习样本", count.I, area, ratio, rawArea, p.Samples.Count);
                    var record = CreateRecord("VariationSurfaceInspection", "OK", area, message, tool);
                    resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message); return record;
                }
            }
            catch { tool.ClearRuntimeOutputs(); throw; }
            finally { DisposeObject(compared); DisposeObject(difference); DisposeObject(connected); DisposeObject(selected); if (count != null) count.Dispose(); VmVariationModelS130.ClearModel(model); }
        }
    }
}
