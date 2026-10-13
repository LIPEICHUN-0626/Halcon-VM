using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class PairedImageSampleS113
        {
            public string Key { get; set; }
            public string PathA { get; set; }
            public string PathB { get; set; }
            public string DisplayText { get { return Key + " · A " + Path.GetFileName(PathA) + " / B " + Path.GetFileName(PathB); } }
        }

        private readonly List<PairedImageSampleS113> pairedSamplesS113 = new List<PairedImageSampleS113>();
        private readonly Dictionary<string, string> pairedRuntimePathsS113 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string pairedSourceOptionsSignatureS113;
        private string pairedScanFolderAS113;
        private string pairedScanFolderBS113;
        private string pairedScanFlowIdS113;
        private string pairedScanSourceIdAS113;
        private string pairedScanSourceIdBS113;

        private void RefreshPairedSourceOptionsS113()
        {
            if (PairedSourceAComboS113 == null || PairedSourceBComboS113 == null) return;
            var sources = flowTools.Where(item => item.Kind == VmToolKind.ImageSource).ToList();
            string signature = string.Join("|", sources.Select(item => item.ToolId + "=" + item.InstanceName));
            if (signature == pairedSourceOptionsSignatureS113) return;
            string a = PairedSourceAComboS113.SelectedValue as string;
            string b = PairedSourceBComboS113.SelectedValue as string;
            PairedSourceAComboS113.ItemsSource = sources;
            PairedSourceBComboS113.ItemsSource = sources;
            PairedSourceAComboS113.SelectedValue = sources.Any(item => item.ToolId == a) ? a : sources.Select(item => item.ToolId).FirstOrDefault();
            PairedSourceBComboS113.SelectedValue = sources.Any(item => item.ToolId == b && item.ToolId != (string)PairedSourceAComboS113.SelectedValue)
                ? b : sources.Skip(1).Select(item => item.ToolId).FirstOrDefault();
            pairedSourceOptionsSignatureS113 = signature;
        }

        private void BrowsePairFolderAS113_Click(object sender, RoutedEventArgs e) { BrowsePairFolderS113(PairedFolderATextS113); }
        private void BrowsePairFolderBS113_Click(object sender, RoutedEventArgs e) { BrowsePairFolderS113(PairedFolderBTextS113); }
        private void BrowsePairFolderS113(System.Windows.Controls.TextBox target)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("修改配对图像目录");
            if (editLeaseS137 < 0) return;
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择该图像源的图片目录；两个目录按同名文件配对。";
                if (Directory.Exists(target.Text)) dialog.SelectedPath = target.Text;
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                target.Text = dialog.SelectedPath;
                pairedSamplesS113.Clear();
                pairedScanFlowIdS113 = null;
                PairedSampleComboS113.ItemsSource = null;
                PairedImageStatusS113.Text = "目录已更新，请重新执行配对检查。";
            }
        }

        private void ScanImagePairsS113_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("双源配对检查", delegate
            {
                if (isImageQueueBatchRunning || isFlowExecutionActive || isContinuousRunning)
                    throw new InvalidOperationException("检测运行中不能更换配对列表。");
                pairedSamplesS113.Clear();
                pairedScanFlowIdS113 = null;
                PairedSampleComboS113.ItemsSource = null;
                var sources = ResolvePairSourceToolsS113();
                var a = IndexPairFolderS113(PairedFolderATextS113.Text, "A");
                var b = IndexPairFolderS113(PairedFolderBTextS113.Text, "B");
                var missingB = a.Keys.Except(b.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                var missingA = b.Keys.Except(a.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                if (missingB.Count > 0 || missingA.Count > 0)
                    throw new InvalidOperationException("配对不完整：B 缺 " + missingB.Count + " 张" +
                        (missingB.Count == 0 ? "" : "（如 " + missingB[0] + "）") + "，A 缺 " + missingA.Count + " 张" +
                        (missingA.Count == 0 ? "" : "（如 " + missingA[0] + "）") + "；补齐后再检查，不会错配运行。");
                pairedSamplesS113.Clear();
                foreach (string key in a.Keys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
                    pairedSamplesS113.Add(new PairedImageSampleS113 { Key = key, PathA = a[key], PathB = b[key] });
                PairedSampleComboS113.ItemsSource = null;
                PairedSampleComboS113.ItemsSource = pairedSamplesS113.ToList();
                PairedSampleComboS113.SelectedIndex = pairedSamplesS113.Count == 0 ? -1 : 0;
                pairedScanFolderAS113 = Path.GetFullPath(PairedFolderATextS113.Text);
                pairedScanFolderBS113 = Path.GetFullPath(PairedFolderBTextS113.Text);
                pairedScanFlowIdS113 = activeProjectFlowIdS104;
                pairedScanSourceIdAS113 = sources.Item1.ToolId;
                pairedScanSourceIdBS113 = sources.Item2.ToolId;
                PairedImageStatusS113.Text = "同名配对 " + pairedSamplesS113.Count.ToString(CultureInfo.InvariantCulture) + " 组；尚未运行";
                HeaderStatusText.Text = PairedImageStatusS113.Text;
            });
        }

        private Dictionary<string, string> IndexPairFolderS113(string folder, string side)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                throw new InvalidOperationException(side + " 图像目录不存在，请选择有效目录。");
            var paths = Directory.EnumerateFiles(folder).Where(HasSupportedImageExtension).Take(1001).ToList();
            if (paths.Count == 0) throw new InvalidOperationException(side + " 目录没有可识别图片。");
            if (paths.Count > 1000) throw new InvalidOperationException(side + " 目录超过 1000 张图片；请拆分批次。");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                string key = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(key) || result.ContainsKey(key))
                    throw new InvalidOperationException(side + " 目录存在空名或重复配对键：“" + key + "”。请保留每个键一张图片。");
                result.Add(key, path);
            }
            return result;
        }

        private Tuple<VmToolInstance, VmToolInstance> ResolvePairSourceToolsS113()
        {
            string a = PairedSourceAComboS113.SelectedValue as string;
            string b = PairedSourceBComboS113.SelectedValue as string;
            VmToolInstance toolA = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ImageSource && item.ToolId == a && item.IsEnabled);
            VmToolInstance toolB = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ImageSource && item.ToolId == b && item.IsEnabled);
            if (toolA == null || toolB == null || toolA.ToolId == toolB.ToolId)
                throw new InvalidOperationException("请选择当前流程中两个不同且已启用的本地图像源模块。");
            if (VmImageSourceModeS114.Normalize(toolA.Parameters == null ? null : toolA.Parameters.ImageSourceModeS114) == VmImageSourceModeS114.Camera ||
                VmImageSourceModeS114.Normalize(toolB.Parameters == null ? null : toolB.Parameters.ImageSourceModeS114) == VmImageSourceModeS114.Camera)
                throw new InvalidOperationException("目录配对不能覆盖正在使用的相机源；请使用本地图片或离线回放源模块。");
            return Tuple.Create(toolA, toolB);
        }

        private void PreflightImagePairS113(PairedImageSampleS113 pair)
        {
            if (pair == null || !File.Exists(pair.PathA) || !File.Exists(pair.PathB))
                throw new InvalidOperationException("配对文件已缺失；请重新检查目录。组=" + (pair == null ? "--" : pair.Key));
            using (HImage a = imageService.ReadImage(pair.PathA))
            using (HImage b = imageService.ReadImage(pair.PathB))
            {
                bool fused = flowTools.Any(item => item.IsEnabled &&
                    (item.Kind == VmToolKind.ImageDifference || item.Kind == VmToolKind.DynamicThreshold));
                if (!fused) return; // Parallel source chains may legitimately use different image sizes.
                HTuple typeA, typeB;
                HOperatorSet.GetImageType(a, out typeA);
                HOperatorSet.GetImageType(b, out typeB);
                if (GetImageWidth(a) != GetImageWidth(b) || GetImageHeight(a) != GetImageHeight(b) ||
                    imageService.GetChannelCount(a) != imageService.GetChannelCount(b) ||
                    !string.Equals(typeA.ToString(), typeB.ToString(), StringComparison.Ordinal))
                    throw new InvalidOperationException("组“" + pair.Key + "”的两路图片尺寸、通道或位深不一致；双图差分/动态阈值不能执行。请更换配对或移除融合模块。");
            }
        }

        private void PreviewImagePairS113_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("覆盖双源输入作工程预览")) return;
            RunUiAction("预览双源组", delegate
            {
                if (historicalImageMode || isImageQueueBatchRunning || isContinuousRunning || isFlowExecutionActive)
                    throw new InvalidOperationException("请先结束当前运行或历史回看。");
                PairedImageSampleS113 pair = PairedSampleComboS113.SelectedItem as PairedImageSampleS113;
                if (pair == null) throw new InvalidOperationException("请先检查配对并选择一个图片组。");
                var sources = ResolvePairSourceToolsS113();
                ValidatePairScanS113(sources);
                PreflightImagePairS113(pair);
                try
                {
                    pairedRuntimePathsS113[sources.Item1.ToolId] = pair.PathA;
                    pairedRuntimePathsS113[sources.Item2.ToolId] = pair.PathB;
                    InspectionRecord resultA = ExecuteFlowTool(sources.Item1, "双源组预览 A · " + pair.Key);
                    InspectionRecord resultB = ExecuteFlowTool(sources.Item2, "双源组预览 B · " + pair.Key);
                    if (resultA.ResultCode != "OK" || resultB.ResultCode != "OK")
                        throw new InvalidOperationException("图像源未成功加载；请查看步骤记录和模块异常。");
                    SelectPairViewS113(sources);
                    PairedImageStatusS113.Text = "预览 “" + pair.Key + "” · 右 A / 左 B；下游尚未运行";
                    HeaderStatusText.Text = PairedImageStatusS113.Text;
                    RefreshUiState(); ScheduleRefreshDisplay();
                }
                finally { pairedRuntimePathsS113.Clear(); }
            });
        }

        private void SelectPairViewS113(Tuple<VmToolInstance, VmToolInstance> sources)
        {
            if (!dualImageMode) SetDualImageMode(true);
            SetImageContextSelection(VmImageContextOption.SourcePrefix + sources.Item1.ToolId);
            imageContextManuallySelected = true;
            DualInputPortComboBox.SelectedValue = VmImageContextOption.SourcePrefix + sources.Item2.ToolId;
            UpdateImageContextViewport(true);
        }

        private void ValidatePairScanS113(Tuple<VmToolInstance, VmToolInstance> sourceTools)
        {
            if (pairedScanFlowIdS113 != activeProjectFlowIdS104 ||
                pairedScanSourceIdAS113 != sourceTools.Item1.ToolId || pairedScanSourceIdBS113 != sourceTools.Item2.ToolId ||
                !string.Equals(pairedScanFolderAS113, Path.GetFullPath(PairedFolderATextS113.Text), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(pairedScanFolderBS113, Path.GetFullPath(PairedFolderBTextS113.Text), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("流程、图像源或目录已更改；请重新执行配对检查，避免运行旧图片组。");
        }

        private async void RunCurrentImagePairS113_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = PairedSampleComboS113.SelectedItem as PairedImageSampleS113;
                await RunImagePairsS113Async(selected == null ? new List<PairedImageSampleS113>() : new List<PairedImageSampleS113> { selected });
            }
            catch (Exception ex) { HandleFlowRunException("双源当前组", ex); }
        }

        private async void RunAllImagePairsS113_Click(object sender, RoutedEventArgs e)
        {
            try { await RunImagePairsS113Async(pairedSamplesS113.ToList()); }
            catch (Exception ex) { HandleFlowRunException("双源全部组", ex); }
        }

        private async Task RunImagePairsS113Async(List<PairedImageSampleS113> targets)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("双源图片组工程调试");
            if (editLeaseS137 < 0) return;
            if (historicalImageMode || isImageQueueBatchRunning || isContinuousRunning || isFlowExecutionActive || IsCurrentToolContinuousTrialRunningS87)
                throw new InvalidOperationException("请先结束历史回看、连续调试或其他流程运行。");
            if (targets == null || targets.Count == 0) throw new InvalidOperationException("没有可运行的图片组；请先检查配对。");
            var sourceTools = ResolvePairSourceToolsS113();
            if (subflowEditorsS102.Count > 0) throw new InvalidOperationException("请先返回主流程再运行双源组。");
            ValidatePairScanS113(sourceTools);
            VmFlowExecutionPlanner.BuildPlan(flowTools, FlowToolList.SelectedItem as VmToolInstance, VmFlowRunRequestMode.Full);
            VisionRecipe recipe = CaptureRecipe();
            VmFlowRunPolicy policy = ReadFlowRunPolicyFromUi();
            VmImageBatchTraceRecord trace = new VmImageBatchTraceRecord
            {
                SchemaVersion = VmImageBatchTraceRecord.CurrentSchemaVersion,
                BatchId = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-P" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant(),
                RecipeName = string.IsNullOrWhiteSpace(recipe.Name) ? "未命名方案" : recipe.Name,
                FlowId = activeProjectFlowIdS104,
                FlowName = GetProjectFlowNameS104(),
                RecipeFingerprint = BatchTraceStore.HashFingerprint(recipeService.GetRecipeFingerprint(recipe)),
                StartedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                Scope = targets.Count == 1 ? "PairedCurrent" : "PairedAll",
                TotalCount = targets.Count,
                Outcome = "Running",
                Items = targets.Select((pair, index) => new VmImageBatchTraceItem
                {
                    Sequence = index, PairKey = pair.Key, FilePath = pair.PathA,
                    SecondaryFilePath = pair.PathB, FileName = pair.Key,
                    SourceToolIdA = sourceTools.Item1.ToolId, SourceToolIdB = sourceTools.Item2.ToolId,
                    State = "NotRun", ResultCode = "--", Message = "等待双源运行"
                }).ToList()
            };
            Stopwatch stopwatch = Stopwatch.StartNew();
            StopPlayback();
            SelectPairViewS113(sourceTools);
            isImageQueueBatchRunning = true;
            imageQueueBatchStopRequested = false;
            isStopRequested = false;
            isPauseRequested = false;
            RefreshUiState();
            try
            {
                for (int index = 0; index < targets.Count; index++)
                {
                    if (imageQueueBatchStopRequested || isStopRequested) break;
                    PairedImageSampleS113 pair = targets[index];
                    VmImageBatchTraceItem item = trace.Items[index];
                    PairedImageStatusS113.Text = "运行 " + (index + 1).ToString(CultureInfo.InvariantCulture) + "/" + targets.Count.ToString(CultureInfo.InvariantCulture) + " · " + pair.Key;
                    await Dispatcher.Yield(DispatcherPriority.Background);
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) { imageQueueBatchStopRequested = true; break; }
                    Stopwatch itemWatch = Stopwatch.StartNew();
                    try
                    {
                        PreflightImagePairS113(pair);
                        pairedRuntimePathsS113[sourceTools.Item1.ToolId] = pair.PathA;
                        pairedRuntimePathsS113[sourceTools.Item2.ToolId] = pair.PathB;
                        FlowRunOutcome outcome = await RunInspectionRangeAsync(VmFlowRunRequestMode.Full, "双源组 " + pair.Key, false, false, VmRunOriginS135.ImageBatch);
                        item.ResultCode = outcome.ResultCode;
                        item.State = outcome.ResultCode == "OK" ? "OK" :
                            outcome.ResultCode == "STOP" ? "Stopped" :
                            outcome.ExecutionError || outcome.ResultCode == "TIMEOUT" ? "Error" : "NG";
                        item.Message = "A " + Path.GetFileName(pair.PathA) + " / B " + Path.GetFileName(pair.PathB) + " · " + outcome.StopReason;
                        item.ToolResults = CaptureBatchTraceToolResults();
                    }
                    catch (Exception ex)
                    {
                        item.State = "Error"; item.ResultCode = "ERROR";
                        item.Message = "A " + Path.GetFileName(pair.PathA) + " / B " + Path.GetFileName(pair.PathB) + " · " + ex.Message;
                        AppendAlarm("双源组“" + pair.Key + "”未运行成功：" + ex.Message);
                    }
                    finally
                    {
                        pairedRuntimePathsS113.Clear();
                        itemWatch.Stop();
                        item.ElapsedMilliseconds = itemWatch.Elapsed.TotalMilliseconds;
                    }
                    if (item.State == "Stopped" || imageQueueBatchStopRequested || isStopRequested) break;
                    if ((item.State == "NG" || item.State == "Error") && policy.StopOnNg) break;
                    if (index < targets.Count - 1 && policy.ContinuousIntervalMilliseconds > 0)
                        await DelayImageQueueBatchIntervalAsync(policy.ContinuousIntervalMilliseconds);
                }
            }
            finally
            {
                pairedRuntimePathsS113.Clear();
                stopwatch.Stop();
                foreach (VmImageBatchTraceItem item in trace.Items.Where(item => item.State == "NotRun"))
                {
                    item.State = "Skipped";
                    item.Message = imageQueueBatchStopRequested || isStopRequested ? "停止后未运行" : "遇 NG/异常后未运行";
                }
                trace.CompletedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                trace.TotalElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                trace.Outcome = imageQueueBatchStopRequested || isStopRequested ? "Stopped" :
                    trace.Items.Any(item => item.State == "Error") ? "Error" :
                    trace.Items.Any(item => item.State == "NG") ? "NG" : "OK";
                trace.AssetsEnabled = false; // Pair paths/identity and per-tool records persist; no misleading single-image asset.
                trace.Normalize();
                TryPersistCompletedBatchTrace(trace);
                isImageQueueBatchRunning = false;
                imageQueueBatchStopRequested = false;
                isStopRequested = false;
                isPauseRequested = false;
                PairedImageStatusS113.Text = "双源组完成 · OK " + trace.Items.Count(item => item.State == "OK") +
                    " / " + trace.Items.Count + " · " + trace.Outcome;
                HeaderStatusText.Text = PairedImageStatusS113.Text;
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
        }

        private void LoadPairedDifferenceExampleS113_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("paired-image-difference");
        }

        private bool BuildPairedDifferenceExampleS113()
        {
            VmToolInstance sourceA = CreateFlowTool(VmToolKind.ImageSource, "01 源 A · 待测", true, null);
            VmToolInstance sourceB = CreateFlowTool(VmToolKind.ImageSource, "02 源 B · 参考", true, null);
            VmToolInstance filterB = CreateFlowTool(VmToolKind.ImageFilter, "03 参考图平滑", true, null);
            VmToolInstance difference = CreateFlowTool(VmToolKind.ImageDifference, "04 双图绝对差分", true, null);
            VmToolInstance threshold = CreateFlowTool(VmToolKind.ImageThreshold, "05 差异分割", true, null);
            VmToolInstance contour = CreateFlowTool(VmToolKind.RegionContourMeasurement, "06 差异轮廓", true, null);
            foreach (VmToolInstance tool in new[] { sourceA, sourceB, filterB, difference, threshold, contour }) flowTools.Add(tool);
            filterB.SetInputBinding("Image", sourceB.ToolId, "Image");
            difference.SetInputBinding("ImageA", sourceA.ToolId, "Image");
            difference.SetInputBinding("ImageB", filterB.ToolId, "Image");
            threshold.SetInputBinding("Image", difference.ToolId, "Image");
            contour.SetInputBinding("Region", threshold.ToolId, "Region");
            filterB.Parameters.ImageFilterMaskWidth = 5;
            filterB.Parameters.ImageFilterMaskHeight = 5;
            threshold.Parameters.ImageThresholdMinGray = 8;
            threshold.Parameters.ImageThresholdMaxGray = 255;
            string image = GetHalconExampleImageS78("particle.png");
            sourceA.Parameters.LocalImagePath = image;
            sourceB.Parameters.LocalImagePath = image;
            sourceA.Parameters.LocalImageSerialNumber = "EXAMPLE-A";
            sourceB.Parameters.LocalImageSerialNumber = "EXAMPLE-B";
            return File.Exists(image);
        }
    }
}
