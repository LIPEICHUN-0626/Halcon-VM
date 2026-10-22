using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private VmNccParametersS131 nccDraftS131 = new VmNccParametersS131();
        private HImage nccPreviewImageS131;
        private VmImageSnapshot nccPreviewSnapshotS131;
        private string nccPreviewIdentityS131;
        private int nccPreviewWidthS131, nccPreviewHeightS131, nccTrainingRevisionS131;
        private Point? nccTrainingStartS131;
        private long nccBoxEditLeaseS137 = -1;
        private Rect? nccTrainingBoxS131;
        private CancellationTokenSource nccTrainingCancellationS131;
        private VmToolInstance nccCandidatesToolS131;
        private IList<VmNccMatchS131> nccCandidateViewsS131 = new List<VmNccMatchS131>();
        private bool nccCandidateGridUpdatingS131;

        private sealed class NccCandidateStateS131
        {
            public VmToolInstance Tool;
            public IList<VmNccMatchS131> Matches;
            public string Summary, ModelVersion, Context;
        }
        private readonly Dictionary<string, NccCandidateStateS131> nccResultsS131 = new Dictionary<string, NccCandidateStateS131>(StringComparer.OrdinalIgnoreCase);

        private void CancelNccTrainingS131()
        {
            nccTrainingRevisionS131++;
            if (nccTrainingCancellationS131 != null) nccTrainingCancellationS131.Cancel();
        }

        private void ClearNccPreviewS131()
        {
            CancelNccTrainingS131();
            if (nccPreviewImageS131 != null) { nccPreviewImageS131.Dispose(); nccPreviewImageS131 = null; }
            nccPreviewSnapshotS131 = null; nccPreviewIdentityS131 = null;
            nccTrainingBoxS131 = null; nccTrainingStartS131 = null;
            nccBoxEditLeaseS137 = -1;
            nccPreviewWidthS131 = nccPreviewHeightS131 = 0;
            if (DockNccPreviewS131 == null) return;
            DockNccPreviewS131.ReleaseMouseCapture();
            DockNccPreviewImageS131.Source = null;
            DockNccTrainingRectS131.Visibility = Visibility.Collapsed;
        }

        private void LoadNccDraftS131(VmToolParameterData parameters)
        {
            ClearNccPreviewS131();
            nccDraftS131 = (parameters.NccMatchingS131 ?? new VmNccParametersS131()).Clone();
            DockNccMinScoreS131.Text = nccDraftS131.MinScore.ToString("R", CultureInfo.InvariantCulture);
            DockNccMaxMatchesS131.Text = nccDraftS131.MaxMatches.ToString(CultureInfo.InvariantCulture);
            DockNccAngleStartS131.Text = nccDraftS131.AngleStartDegrees.ToString("R", CultureInfo.InvariantCulture);
            DockNccAngleExtentS131.Text = nccDraftS131.AngleExtentDegrees.ToString("R", CultureInfo.InvariantCulture);
            DockNccMaxOverlapS131.Text = nccDraftS131.MaxOverlap.ToString("R", CultureInfo.InvariantCulture);
            DockNccLevelsS131.Text = nccDraftS131.NumLevels.ToString(CultureInfo.InvariantCulture);
            DockNccPolarityS131.IsChecked = nccDraftS131.UsePolarity;
            DockNccSubpixelS131.IsChecked = nccDraftS131.Subpixel;
            DockNccTrainingS131.IsExpanded = !nccDraftS131.HasModel;
            DockNccTrainingStatusS131.Text = "读取输入→框选→制作模板→应用；训练框不等于搜索 ROI";
            RefreshNccTaskS131();
        }

        private bool ReadNccDraftS131(VmToolParameterData parameters)
        {
            var p = nccDraftS131.Clone(); double number; int integer;
            if (!TryParseUiDouble(DockNccMinScoreS131.Text, out number)) return FailDockDraft("NCC 最低分数须为数值。" ); p.MinScore = number;
            if (!int.TryParse(DockNccMaxMatchesS131.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("NCC 最多候选须为整数。" ); p.MaxMatches = integer;
            if (!TryParseUiDouble(DockNccAngleStartS131.Text, out number)) return FailDockDraft("NCC 起始角须为数值。" ); p.AngleStartDegrees = number;
            if (!TryParseUiDouble(DockNccAngleExtentS131.Text, out number)) return FailDockDraft("NCC 角度范围须为数值。" ); p.AngleExtentDegrees = number;
            if (!TryParseUiDouble(DockNccMaxOverlapS131.Text, out number)) return FailDockDraft("NCC 最大重叠须为数值。" ); p.MaxOverlap = number;
            if (!int.TryParse(DockNccLevelsS131.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("NCC 金字塔层数须为整数。" ); p.NumLevels = integer;
            p.UsePolarity = DockNccPolarityS131.IsChecked == true; p.Subpixel = DockNccSubpixelS131.IsChecked == true;
            string error = p.Validate(); if (!string.IsNullOrWhiteSpace(error)) return FailDockDraft(error);
            parameters.NccMatchingS131 = p; return true;
        }

        private bool NccInputMatchesDraftS131(VmToolInstance tool)
        {
            var row = dockInputPortRows.FirstOrDefault(r => r.PortName == "Image");
            var port = ToolMetadata.GetInstanceInputPorts(tool).FirstOrDefault(p => p.PortName == "Image");
            return row != null && port != null && row.SelectedSourceKey == GetDockSavedInputSourceKey(tool, port);
        }

        private string NccInputIdentityS131(VmToolInstance tool)
        {
            var binding = tool.GetInputBinding("Image"); var source = GetInputSourceTool(binding);
            if (binding == null || source == null || !string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)) return null;
            return tool.ToolId + ":" + source.ToolId + ":" + binding.SourcePortName + ":" + source.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture);
        }

        private VmImageSnapshot GetNccInputSnapshotS131(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.NccTemplateMatch || !IsVariationTaskStoppedS130())
                throw new InvalidOperationException("请停止运行并打开 NCC 模块。" );
            if (!NccInputMatchesDraftS131(tool)) throw new InvalidOperationException("Image 来源草稿有改动，请先应用，再读取输入；不会自动提交参数。" );
            var binding = tool.GetInputBinding("Image"); var source = GetInputSourceTool(binding);
            string bindingError = binding == null ? "NCC 必须显式连接 Image。" : GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(bindingError)) throw new InvalidOperationException(bindingError);
            var snapshot = GetImageSnapshot(source, binding.SourcePortName);
            if (source == null || source.ResultCode != "OK" || snapshot == null || snapshot.IsDisposed)
                throw new InvalidOperationException("当前图像输入未就绪，请先运行上游；不回退全局图或上次结果。" );
            return snapshot;
        }

        private void DockNccReadS131_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("读取 NCC 模板制作输入");
            if (editLeaseS137 < 0) return;
            RunUiAction("读取 NCC 输入", delegate
            {
                if (nccTrainingCancellationS131 != null || !IsVariationTaskStoppedS130()) throw new InvalidOperationException("训练或流程运行中不能重新取图。" );
                var tool = dockEditingTool;
                if (tool == null || tool.Kind != VmToolKind.NccTemplateMatch) return;
                if (!NccInputMatchesDraftS131(tool)) throw new InvalidOperationException("请先应用 Image 订阅改动。" );
                var binding = tool.GetInputBinding("Image"); var source = GetInputSourceTool(binding);
                if (source != null && source.Kind == VmToolKind.ImageSource)
                {
                    if (!source.IsEnabled) throw new InvalidOperationException("图像源已停用。" );
                    ExecuteFlowTool(source, "NCC 制作模板读取输入");
                }
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                ClearNccPreviewS131();
                var snapshot = GetNccInputSnapshotS131(tool);
                HImage copy = snapshot.CreateImageCopy();
                try
                {
                    int width, height; string type;
                    VmNccModelS131.DescribeImage(copy, out width, out height, out type);
                    using (var display = copy.ConvertImageType("byte"))
                    {
                        IntPtr pointer = display.GetImagePointer1(out type, out width, out height);
                        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pointer, checked(width * height), width);
                        bitmap.Freeze();
                        if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                        DockNccPreviewImageS131.Source = bitmap;
                    }
                    nccPreviewImageS131 = copy; copy = null;
                    nccPreviewWidthS131 = width; nccPreviewHeightS131 = height;
                    nccPreviewSnapshotS131 = snapshot; nccPreviewIdentityS131 = NccInputIdentityS131(tool);
                    DockNccTrainingS131.IsExpanded = true;
                    DockNccTrainingStatusS131.Text = "拖动框选目标；显示为 byte，训练保留原始 byte/uint2";
                }
                finally { if (copy != null) copy.Dispose(); RefreshNccTaskS131(); }
            });
        }

        private Rect NccPreviewBoundsS131()
        {
            if (nccPreviewWidthS131 <= 0 || nccPreviewHeightS131 <= 0) return Rect.Empty;
            double scale = Math.Min(DockNccPreviewS131.ActualWidth / nccPreviewWidthS131, DockNccPreviewS131.ActualHeight / nccPreviewHeightS131);
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) return Rect.Empty;
            return new Rect((DockNccPreviewS131.ActualWidth - nccPreviewWidthS131 * scale) / 2,
                (DockNccPreviewS131.ActualHeight - nccPreviewHeightS131 * scale) / 2, nccPreviewWidthS131 * scale, nccPreviewHeightS131 * scale);
        }
        private Point NccPreviewPixelS131(Point point, Rect bounds)
        {
            return new Point(Math.Floor(Math.Max(0, Math.Min(nccPreviewWidthS131 - 1, (point.X - bounds.X) * nccPreviewWidthS131 / bounds.Width))),
                Math.Floor(Math.Max(0, Math.Min(nccPreviewHeightS131 - 1, (point.Y - bounds.Y) * nccPreviewHeightS131 / bounds.Height))));
        }
        private void DockNccPreviewS131_MouseDown(object sender, MouseButtonEventArgs e)
        {
            nccBoxEditLeaseS137 = CaptureEngineeringEditLeaseS137("框选 NCC 训练目标");
            if (nccBoxEditLeaseS137 < 0) return;
            if (!IsVariationTaskStoppedS130() || nccTrainingCancellationS131 != null) return;
            var bounds = NccPreviewBoundsS131(); var position = e.GetPosition(DockNccPreviewS131);
            if (bounds.IsEmpty || !bounds.Contains(position)) return;
            nccTrainingStartS131 = NccPreviewPixelS131(position, bounds); DockNccPreviewS131.CaptureMouse(); e.Handled = true;
        }
        private void DockNccPreviewS131_MouseMove(object sender, MouseEventArgs e)
        {
            if (!nccTrainingStartS131.HasValue || e.LeftButton != MouseButtonState.Pressed) return;
            if (!IsEngineeringEditLeaseCurrentS137(nccBoxEditLeaseS137, false))
            { nccTrainingStartS131 = null; nccBoxEditLeaseS137 = -1; DockNccPreviewS131.ReleaseMouseCapture(); e.Handled = true; return; }
            var bounds = NccPreviewBoundsS131(); if (bounds.IsEmpty) return;
            nccTrainingBoxS131 = new Rect(nccTrainingStartS131.Value, NccPreviewPixelS131(e.GetPosition(DockNccPreviewS131), bounds));
            DrawNccTrainingBoxS131(); e.Handled = true;
        }
        private void DockNccPreviewS131_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!nccTrainingStartS131.HasValue) return;
            if (!IsEngineeringEditLeaseCurrentS137(nccBoxEditLeaseS137))
            { nccTrainingStartS131 = null; nccBoxEditLeaseS137 = -1; DockNccPreviewS131.ReleaseMouseCapture(); e.Handled = true; return; }
            var bounds = NccPreviewBoundsS131();
            if (!bounds.IsEmpty) nccTrainingBoxS131 = new Rect(nccTrainingStartS131.Value, NccPreviewPixelS131(e.GetPosition(DockNccPreviewS131), bounds));
            nccTrainingStartS131 = null; DockNccPreviewS131.ReleaseMouseCapture();
            nccBoxEditLeaseS137 = -1;
            DrawNccTrainingBoxS131();
            if (nccTrainingBoxS131.HasValue)
            {
                var r = nccTrainingBoxS131.Value;
                DockNccTrainingStatusS131.Text = string.Format(CultureInfo.InvariantCulture, "训练框 R[{0},{1}] C[{2},{3}] · {4}×{5} 像素", r.Top, r.Bottom, r.Left, r.Right, r.Width + 1, r.Height + 1);
            }
            RefreshNccTaskS131(); e.Handled = true;
        }
        private void DockNccPreviewS131_SizeChanged(object sender, SizeChangedEventArgs e) { DrawNccTrainingBoxS131(); }
        private void DrawNccTrainingBoxS131()
        {
            if (DockNccTrainingRectS131 == null) return;
            var bounds = NccPreviewBoundsS131();
            if (!nccTrainingBoxS131.HasValue || bounds.IsEmpty) { DockNccTrainingRectS131.Visibility = Visibility.Collapsed; return; }
            var r = nccTrainingBoxS131.Value;
            Canvas.SetLeft(DockNccTrainingRectS131, bounds.X + r.Left * bounds.Width / nccPreviewWidthS131);
            Canvas.SetTop(DockNccTrainingRectS131, bounds.Y + r.Top * bounds.Height / nccPreviewHeightS131);
            DockNccTrainingRectS131.Width = (r.Width + 1) * bounds.Width / nccPreviewWidthS131;
            DockNccTrainingRectS131.Height = (r.Height + 1) * bounds.Height / nccPreviewHeightS131;
            DockNccTrainingRectS131.Visibility = Visibility.Visible;
        }

        private async void DockNccTrainS131_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("制作 NCC 模板");
            if (editLeaseS137 < 0) return;
            var tool = dockEditingTool; HImage trainingImage = null; string path = null; bool retained = false;
            VmNccParametersS131 draft; string identity, context; Rect box;
            try
            {
                if (nccTrainingCancellationS131 != null) throw new InvalidOperationException("上一训练尚在退出。" );
                var snapshot = GetNccInputSnapshotS131(tool);
                if (nccPreviewImageS131 == null || !nccTrainingBoxS131.HasValue || !ReferenceEquals(snapshot, nccPreviewSnapshotS131) || nccPreviewIdentityS131 != NccInputIdentityS131(tool))
                    throw new InvalidOperationException("请重新读取当前输入并框选目标；旧预览不会用于制作新模板。" );
                var parameters = tool.Parameters.Clone(); if (!ReadNccDraftS131(parameters)) return;
                draft = parameters.NccMatchingS131; box = nccTrainingBoxS131.Value;
                if (box.Width < 2 || box.Height < 2) throw new InvalidOperationException("训练框至少为 3×3 像素，并应包含明显纹理。" );
                identity = nccPreviewIdentityS131; context = ResolveTargetImageContextIdS86(tool);
                if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context)) throw new InvalidOperationException("图像坐标身份尚未确定。" );
                string directory = Path.Combine(recipeService.RecipeDirectory, "NccModels"); Directory.CreateDirectory(directory);
                path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".ncm");
                trainingImage = nccPreviewImageS131.CopyImage();
            }
            catch (Exception ex) { if (trainingImage != null) trainingImage.Dispose(); DockNccTrainingStatusS131.Text = ex.Message; LogInfo("NCC 制作模板：" + ex.Message); return; }
            int revision = nccTrainingRevisionS131;
            var cancellation = new CancellationTokenSource(); nccTrainingCancellationS131 = cancellation;
            DockNccTrainingStatusS131.Text = "后台制作中 · 当前算子结束后可取消；旧模型保留";
            RefreshNccTaskS131();
            try
            {
                var trained = await Task.Run(delegate
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    var result = VmNccModelS131.Train(trainingImage, draft, box.Top, box.Left, box.Bottom, box.Right, context, path);
                    cancellation.Token.ThrowIfCancellationRequested(); return result;
                });
                cancellation.Token.ThrowIfCancellationRequested();
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                { DockNccTrainingStatusS131.Text = "工程授权已改变，本次模板未进入草稿；原已应用模板保留"; return; }
                if (dockEditingTool != tool || revision != nccTrainingRevisionS131 || !IsVariationTaskStoppedS130() || !NccInputMatchesDraftS131(tool) || identity != NccInputIdentityS131(tool) || context != ResolveTargetImageContextIdS86(tool))
                    throw new OperationCanceledException("模块或输入已改变。" );
                var current = tool.Parameters.Clone(); if (!ReadNccDraftS131(current)) return;
                var latest = current.NccMatchingS131;
                if (latest.AngleStartDegrees != draft.AngleStartDegrees || latest.AngleExtentDegrees != draft.AngleExtentDegrees || latest.NumLevels != draft.NumLevels || latest.UsePolarity != draft.UsePolarity)
                    throw new OperationCanceledException("训练属性已改变。" );
                trained.MinScore = latest.MinScore; trained.MaxMatches = latest.MaxMatches; trained.MaxOverlap = latest.MaxOverlap; trained.Subpixel = latest.Subpixel;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                nccDraftS131 = trained; retained = true;
                DockNccTrainingS131.IsExpanded = false;
                DockNccTrainingStatusS131.Text = "新模板进入草稿 · F5 试运行，确认后应用";
                DockDraftValue_Changed(sender, e);
                LogInfo(tool.InstanceName + " NCC 模板已制作：" + trained.ModelSha256 + "；尚未替换已应用版本");
            }
            catch (OperationCanceledException) { if (dockEditingTool == tool && revision == nccTrainingRevisionS131) DockNccTrainingStatusS131.Text = "训练未应用 · 输入/属性改变或已取消，旧模型保留"; }
            catch (Exception ex) { if (dockEditingTool == tool) { DockNccTrainingStatusS131.Text = "制作失败：" + ex.Message; LogInfo(tool.InstanceName + " NCC 制作失败：" + ex.Message); } }
            finally
            {
                trainingImage.Dispose();
                if (!retained && path != null) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
                if (ReferenceEquals(nccTrainingCancellationS131, cancellation)) nccTrainingCancellationS131 = null;
                cancellation.Dispose(); RefreshNccTaskS131();
            }
        }
        private void DockNccCancelS131_Click(object sender, RoutedEventArgs e)
        { CancelNccTrainingS131(); DockNccTrainingStatusS131.Text = "已请求取消 · 当前 HALCON 算子结束后退出，不替换模型"; }

        private void RefreshNccTaskS131()
        {
            if (DockNccTrainS131 == null) return;
            bool busy = nccTrainingCancellationS131 != null, stopped = IsVariationTaskStoppedS130();
            if (busy && !stopped && !nccTrainingCancellationS131.IsCancellationRequested) CancelNccTrainingS131();
            DockNccReadS131.IsEnabled = stopped && !busy;
            DockNccTrainS131.IsEnabled = stopped && !busy && nccPreviewImageS131 != null && nccTrainingBoxS131.HasValue;
            DockNccPreviewS131.IsEnabled = stopped && !busy;
            DockNccCancelS131.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            var applied = dockEditingTool == null || dockEditingTool.Parameters == null ? null : dockEditingTool.Parameters.NccMatchingS131;
            bool pending = nccDraftS131.HasModel && (applied == null || !string.Equals(nccDraftS131.ModelPath, applied.ModelPath, StringComparison.OrdinalIgnoreCase));
            DockNccModelStateS131.Text = nccDraftS131.HasModel ? (pending ? "新模板草稿 · 待应用" : "已应用模板 · 重跑看结果") : "未配置模板 · 先读取图像";
            DockNccModelStateS131.ToolTip = nccDraftS131.HasModel ? nccDraftS131.ModelPath + "\nSHA256: " + nccDraftS131.ModelSha256 : "输入图像 → 框选目标 → 制作草稿模板 → F5 → 应用";
        }

        private InspectionRecord RunNccMatchingS131(VmToolInstance tool, HImage image)
        {
            nccResultsS131.Remove(tool.ToolId);
            var p = (tool.Parameters.NccMatchingS131 ?? new VmNccParametersS131()).Clone();
            string targetInput;
            string targetContext = PrepareTemplateTargetContextS145(tool, out targetInput);
            string context = targetContext ?? ResolveTargetImageContextIdS86(tool);
            if (string.IsNullOrWhiteSpace(context) || context == "无效来源" || IsPendingCoordinateContextS102(context)) throw new InvalidOperationException("NCC 输入缺少明确图像坐标身份。" );
            HObject contours = null;
            try
            {
                IList<VmNccMatchS131> matches;
                using (var roi = CreateEffectiveRoiRegion(tool, false)) matches = VmNccModelS131.Match(image, roi, p);
                PublishTemplateTargetsS145(tool, matches.Select(match => new VmTemplateTargetS145
                    { PixelRow = match.PixelRow, PixelColumn = match.PixelColumn, AngleDegrees = match.AngleDegrees,
                        Score = match.Score, Scale = 1, SourceIndex = match.Index - 1 }), targetContext, targetInput, p.ModelSha256);
                var best = matches.OrderByDescending(m => m.Score).ThenBy(m => m.Index).FirstOrDefault();
                string code = best == null ? "NG" : "OK";
                string summary = "[" + string.Join(" | ", matches.Select(m => string.Format(CultureInfo.InvariantCulture, "#{0}:Score={1:R},PixelRow={2:R},PixelColumn={3:R},AngleDegrees={4:R},HalconRow={5:R},HalconColumn={6:R}", m.Index, m.Score, m.PixelRow, m.PixelColumn, m.AngleDegrees, m.Row, m.Column))) + "]";
                tool.SetOutputValue("MatchCount", (double)matches.Count); tool.SetOutputValue("HasMatch", best != null);
                tool.SetOutputValue("Score", best == null ? 0d : best.Score);
                tool.SetOutputValue("CenterRow", best == null ? null : (object)best.PixelRow);
                tool.SetOutputValue("CenterColumn", best == null ? null : (object)best.PixelColumn);
                tool.SetOutputValue("AngleDegrees", best == null ? null : (object)best.AngleDegrees);
                tool.SetOutputValue("Point", best == null ? null : (object)VmPoint2D.CreateImagePixel(best.PixelRow, best.PixelColumn, context));
                tool.SetOutputValue("Pose", best == null ? null : (object)VmPose2D.CreateImagePixel(best.PixelRow, best.PixelColumn, best.AngleDegrees, context));
                tool.SetOutputValue("ReferenceRow", p.TemplateReferenceRow); tool.SetOutputValue("ReferenceColumn", p.TemplateReferenceColumn);
                tool.SetOutputValue("ReferenceAngleDegrees", 0d);
                tool.SetOutputValue("TemplateWidth", p.TrainingColumn2 - p.TrainingColumn1 + 1); tool.SetOutputValue("TemplateHeight", p.TrainingRow2 - p.TrainingRow1 + 1);
                tool.SetOutputValue("Candidates", summary); tool.SetOutputValue("ModelVersion", p.ModelSha256);
                tool.SetOutputValue("Parameters", string.Format(CultureInfo.InvariantCulture, "MinScore={0:R};MaxMatches={1};Overlap={2:R};AngleStart={3:R};AngleExtent={4:R};Levels={5};Polarity={6};Subpixel={7};Timeout=10000ms", p.MinScore, p.MaxMatches, p.MaxOverlap, p.AngleStartDegrees, p.AngleExtentDegrees, p.NumLevels, p.UsePolarity, p.Subpixel));
                foreach (var match in matches)
                {
                    HObject frame = CreateNccCandidateFrameS131(match, p);
                    try { AppendOverlayObject(ref contours, frame); } finally { DisposeObject(frame); }
                }
                if (contours != null)
                {
                    tool.SetOutputValue("XLD", VmXldSnapshot.Create(contours, tool.ToolId, tool.InstanceName, "XLD"));
                    tool.SetOutputCoordinateContext("XLD", context);
                    ReplaceToolOverlayContours(contours, tool, "XLD", matches.Count, code); contours = null;
                }
                else tool.SetOutputValue("XLD", null);
                nccResultsS131[tool.ToolId] = new NccCandidateStateS131 { Tool = tool, Matches = matches, Summary = summary, ModelVersion = p.ModelSha256, Context = context };
                string message = best == null ? "NCC 未找到目标 · Score=0 为无匹配哨兵；不发布点/位姿" : string.Format(CultureInfo.InvariantCulture, "NCC {0} 个目标 · 最优分数 {1:0.000} · R={2:0.##} C={3:0.##} A={4:0.##}°", matches.Count, best.Score, best.PixelRow, best.PixelColumn, best.AngleDegrees);
                var record = CreateRecord("NccTemplateMatch", code, best == null ? 0 : best.Score, message, tool);
                resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message); return record;
            }
            catch { tool.ClearRuntimeOutputs(); nccResultsS131.Remove(tool.ToolId); throw; }
            finally { DisposeObject(contours); }
        }

        private HObject CreateNccCandidateFrameS131(VmNccMatchS131 match, VmNccParametersS131 p)
        {
            HTuple matrix = null, rows = null, columns = null; HObject frame = null;
            try
            {
                double r1 = p.TrainingRow1 - p.TemplateReferenceRow, r2 = p.TrainingRow2 - p.TemplateReferenceRow;
                double c1 = p.TrainingColumn1 - p.TemplateReferenceColumn, c2 = p.TrainingColumn2 - p.TemplateReferenceColumn;
                HOperatorSet.VectorAngleToRigid(0, 0, 0, match.Row, match.Column, match.AngleRadians, out matrix);
                HOperatorSet.AffineTransPixel(matrix, new HTuple(new[] { r1, r1, r2, r2, r1 }), new HTuple(new[] { c1, c2, c2, c1, c1 }), out rows, out columns);
                HOperatorSet.GenContourPolygonXld(out frame, rows, columns); return frame;
            }
            finally { if (matrix != null) matrix.Dispose(); if (rows != null) rows.Dispose(); if (columns != null) columns.Dispose(); }
        }

        private NccCandidateStateS131 GetNccCandidateStateS131(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.NccTemplateMatch) return null;
            NccCandidateStateS131 state; object summary, version;
            if (!nccResultsS131.TryGetValue(tool.ToolId, out state)) return null;
            if (!ReferenceEquals(state.Tool, tool) || !tool.TryGetOutputValue("Candidates", out summary) || !tool.TryGetOutputValue("ModelVersion", out version) ||
                !string.Equals(summary as string, state.Summary, StringComparison.Ordinal) || !string.Equals(version as string, state.ModelVersion, StringComparison.Ordinal))
            { nccResultsS131.Remove(tool.ToolId); return null; }
            return state;
        }

        private void RefreshNccCandidateGridS131(VmToolInstance tool)
        {
            if (DockNccCandidatesS131 == null) return;
            var state = GetNccCandidateStateS131(tool);
            bool show = tool != null && tool.Kind == VmToolKind.NccTemplateMatch;
            DockNccCandidatesS131.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show) DockRoiResultsDataGrid.Visibility = Visibility.Collapsed;
            var values = state == null ? new List<VmNccMatchS131>() : state.Matches;
            if (ReferenceEquals(nccCandidatesToolS131, tool) && ReferenceEquals(nccCandidateViewsS131, values)) return;
            nccCandidateGridUpdatingS131 = true;
            try { nccCandidatesToolS131 = tool; nccCandidateViewsS131 = values; DockNccCandidatesS131.ItemsSource = values; DockNccCandidatesS131.SelectedIndex = -1; }
            finally { nccCandidateGridUpdatingS131 = false; }
            foreach (string key in nccResultsS131.Keys.Where(key => !flowTools.Any(t => t.ToolId == key)).ToList()) nccResultsS131.Remove(key);
        }

        private void DockNccCandidateS131_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (nccCandidateGridUpdatingS131 || dockDraftUpdating) return;
            var match = DockNccCandidatesS131.SelectedItem as VmNccMatchS131;
            if (match == null || nccCandidatesToolS131 != dockEditingTool || GetNccCandidateStateS131(dockEditingTool) == null) return;
            SetImageContextSelection(VmImageContextOption.ModuleInput); UpdateImageContextViewport(true);
            HeaderStatusText.Text = string.Format(CultureInfo.InvariantCulture, "NCC 候选 #{0} · 分数 {1:0.000} · 仅查看；最优输出未改变", match.Index, match.Score);
            ScheduleRefreshDisplay();
        }

        private void DrawNccCandidateSelectionS131(HWindow window, VmToolInstance tool, bool left)
        {
            if (window == null || historicalImageMode || tool != nccCandidatesToolS131 || tool != GetImageTaskToolS89()) return;
            var state = GetNccCandidateStateS131(tool); var match = DockNccCandidatesS131.SelectedItem as VmNccMatchS131;
            var context = left ? ResolveDualInputImageContext() : ResolveImageContext();
            if (state == null || match == null || !state.Matches.Contains(match) || GetImageCoordinateContextS112(context) != state.Context) return;
            HObject cross = null;
            try
            {
                HOperatorSet.GenCrossContourXld(out cross, match.PixelRow, match.PixelColumn, 16, 0);
                window.SetColor("magenta"); window.SetLineWidth(3); window.DispObj(cross);
                window.DispText("#" + match.Index, "image", match.PixelRow + 10, match.PixelColumn + 10, "magenta", "box", "false");
            }
            catch (HalconException) { }
            finally { DisposeObject(cross); }
        }
    }
}
