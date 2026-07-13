using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class TemplateCreateWindow : Window
    {
        private readonly HImage image;
        private readonly RoiData referenceRoi;
        private readonly ImageViewportController viewport = new ImageViewportController();
        private readonly RoiEditor roiEditor = new RoiEditor();
        private readonly OverlayRenderer overlayRenderer = new OverlayRenderer();
        private HWindowControl imageWindow;
        private RoiData pendingRoi;
        private RoiData confirmedRoi;
        private bool refreshQueued;

        public TemplateCreateWindow(HImage image, RoiData referenceRoi, TemplateMatchOptions options)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            InitializeComponent();
            this.image = image;
            this.referenceRoi = referenceRoi == null ? null : referenceRoi.Clone();
            ApplyOptions(options ?? new TemplateMatchOptions());
        }

        public TemplateDefinition ResultDefinition { get; private set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            imageWindow = new HWindowControl
            {
                Dock = Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(17, 24, 39)
            };
            imageWindow.MouseDown += ImageWindow_MouseDown;
            imageWindow.MouseMove += ImageWindow_MouseMove;
            imageWindow.MouseUp += ImageWindow_MouseUp;
            imageWindow.MouseDoubleClick += ImageWindow_MouseDoubleClick;
            imageWindow.MouseWheel += ImageWindow_MouseWheel;
            imageWindow.Resize += delegate { ScheduleRefreshDisplay(); };
            HalconHost.Child = imageWindow;

            HTuple width;
            HTuple height;
            HOperatorSet.GetImageSize(image, out width, out height);
            viewport.SetImageSize(width.I, height.I);
            ImageStatusText.Text = string.Format("{0}x{1}", width.I, height.I);
            RefreshState();
            ScheduleRefreshDisplay();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (referenceRoi != null)
            {
                referenceRoi.Dispose();
            }

            ClearPending();
            if (confirmedRoi != null)
            {
                confirmedRoi.Dispose();
                confirmedRoi = null;
            }

            roiEditor.Dispose();
        }

        private void UseCurrentRoiButton_Click(object sender, RoutedEventArgs e)
        {
            if (referenceRoi == null)
            {
                ShowHint("主界面没有可用的已确认 ROI。");
                return;
            }

            SetPending(referenceRoi.Clone());
            ShowHint("已使用当前 ROI，请点击“确认区域”。");
            RefreshState();
            ScheduleRefreshDisplay();
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool(VisionTool.RectangleRoi, "正在绘制矩形模板区域。");
        }

        private void CircleButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool(VisionTool.CircleRoi, "正在绘制圆形模板区域。");
        }

        private void PolygonButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool(VisionTool.PolygonRoi, "正在绘制多边形模板区域：左键加点，双击结束。");
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearPending();
            if (confirmedRoi != null)
            {
                confirmedRoi.Dispose();
                confirmedRoi = null;
            }

            roiEditor.Cancel();
            ShowHint("模板区域已清除。");
            RefreshState();
            ScheduleRefreshDisplay();
        }

        private void ConfirmRegionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RoiData completed = null;
                if (roiEditor.Tool == VisionTool.PolygonRoi && roiEditor.IsPolygonDrawing)
                {
                    completed = roiEditor.CompletePolygon();
                }

                if (completed != null)
                {
                    SetPending(completed);
                }

                if (pendingRoi == null)
                {
                    throw new InvalidOperationException("请先选择模板区域。");
                }

                ValidateRoi(pendingRoi);
                if (confirmedRoi != null)
                {
                    confirmedRoi.Dispose();
                }

                confirmedRoi = pendingRoi.Clone();
                ClearPending();
                ShowHint("模板区域已确认，可以保存并训练。");
                RefreshState();
                ScheduleRefreshDisplay();
            }
            catch (Exception ex)
            {
                ShowHint(ex.Message);
                MessageBox.Show(this, ex.Message, "确认区域", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveTrainButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (confirmedRoi == null)
                {
                    throw new InvalidOperationException("请先确认模板区域。");
                }

                ValidateRoi(confirmedRoi);
                TemplateMatchOptions options = ReadOptions();
                ResultDefinition = new TemplateDefinition
                {
                    TemplateName = string.IsNullOrWhiteSpace(TemplateNameTextBox.Text) ? "模板_001" : TemplateNameTextBox.Text.Trim(),
                    TemplateRoi = confirmedRoi.Clone(),
                    TrainingMask = TemplateDefinition.CloneRegion(confirmedRoi.Region),
                    DisplayFrame = confirmedRoi.Clone(),
                    Options = options
                };
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowHint(ex.Message);
                MessageBox.Show(this, ex.Message, "保存并训练", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ResetOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyOptions(new TemplateMatchOptions());
        }

        private void ImageWindow_MouseDown(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button != Forms.MouseButtons.Left)
            {
                return;
            }

            PointF imagePoint = viewport.WindowToImage(e.Location, imageWindow);
            if (roiEditor.Tool == VisionTool.PolygonRoi)
            {
                roiEditor.AddPolygonPoint(imagePoint);
                ShowHint("多边形绘制中：双击结束，或点击确认区域。");
                ScheduleRefreshDisplay();
                return;
            }

            if (roiEditor.Begin(imagePoint))
            {
                ShowHint("模板区域绘制中。");
                ScheduleRefreshDisplay();
            }
        }

        private void ImageWindow_MouseMove(object sender, Forms.MouseEventArgs e)
        {
            PointF imagePoint = viewport.WindowToImage(e.Location, imageWindow);
            if (roiEditor.Tool == VisionTool.PolygonRoi)
            {
                roiEditor.UpdatePolygon(imagePoint);
                ScheduleRefreshDisplay();
                return;
            }

            if (roiEditor.IsDrawing)
            {
                roiEditor.Update(imagePoint);
                ScheduleRefreshDisplay();
            }
        }

        private void ImageWindow_MouseUp(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button != Forms.MouseButtons.Left || roiEditor.Tool == VisionTool.PolygonRoi)
            {
                return;
            }

            RoiData roi = roiEditor.Complete(viewport.WindowToImage(e.Location, imageWindow));
            if (roi != null)
            {
                SetPending(roi);
                ShowHint("模板区域待确认，请点击“确认区域”。");
                RefreshState();
                ScheduleRefreshDisplay();
            }
        }

        private void ImageWindow_MouseDoubleClick(object sender, Forms.MouseEventArgs e)
        {
            if (roiEditor.Tool != VisionTool.PolygonRoi)
            {
                return;
            }

            RoiData roi = roiEditor.CompletePolygon();
            if (roi != null)
            {
                SetPending(roi);
                ShowHint("多边形区域待确认，请点击“确认区域”。");
                RefreshState();
                ScheduleRefreshDisplay();
            }
        }

        private void ImageWindow_MouseWheel(object sender, Forms.MouseEventArgs e)
        {
            viewport.ZoomAt(e.Location, imageWindow, e.Delta);
            ScheduleRefreshDisplay();
        }

        private void SetTool(VisionTool tool, string hint)
        {
            roiEditor.Tool = tool;
            roiEditor.Cancel();
            ClearPending();
            ShowHint(hint);
            RefreshState();
            ScheduleRefreshDisplay();
        }

        private void SetPending(RoiData roi)
        {
            ClearPending();
            pendingRoi = roi;
        }

        private void ClearPending()
        {
            if (pendingRoi != null)
            {
                pendingRoi.Dispose();
                pendingRoi = null;
            }
        }

        private void RefreshDisplay()
        {
            if (imageWindow == null || imageWindow.HalconWindow == null)
            {
                return;
            }

            imageWindow.HalconWindow.ClearWindow();
            viewport.Apply(imageWindow.HalconWindow);
            image.DispImage(imageWindow.HalconWindow);
            overlayRenderer.Draw(
                imageWindow.HalconWindow,
                referenceRoi,
                confirmedRoi,
                pendingRoi ?? roiEditor.PreviewRoi,
                null,
                null,
                null,
                null,
                false,
                confirmedRoi != null,
                false);
        }

        private void ScheduleRefreshDisplay()
        {
            if (refreshQueued)
            {
                return;
            }

            refreshQueued = true;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                refreshQueued = false;
                RefreshDisplay();
            }), DispatcherPriority.Background);
        }

        private void RefreshState()
        {
            bool hasPending = pendingRoi != null || roiEditor.IsPolygonDrawing || roiEditor.PreviewRoi != null;
            ConfirmRegionButton.IsEnabled = hasPending;
            SaveTrainButton.IsEnabled = confirmedRoi != null;
            RegionText.Text = confirmedRoi != null
                ? "区域：已确认，" + confirmedRoi.ShapeType
                : (hasPending ? "区域：待确认" : "区域：未选择");
            BottomHintText.Text = confirmedRoi != null ? "区域已确认，点击“保存并训练”返回主界面。" : "请先选择并确认模板区域。";
        }

        private void ShowHint(string text)
        {
            StatusText.Text = text;
            BottomHintText.Text = text;
        }

        private void ApplyOptions(TemplateMatchOptions options)
        {
            MinScoreTextBox.Text = options.MinScore.ToString(CultureInfo.InvariantCulture);
            MaxMatchesTextBox.Text = options.MaxMatches.ToString(CultureInfo.InvariantCulture);
            AngleStartTextBox.Text = options.AngleStartDeg.ToString(CultureInfo.InvariantCulture);
            AngleExtentTextBox.Text = options.AngleExtentDeg.ToString(CultureInfo.InvariantCulture);
            MaxOverlapTextBox.Text = options.MaxOverlap.ToString(CultureInfo.InvariantCulture);
            GreedinessTextBox.Text = options.Greediness.ToString(CultureInfo.InvariantCulture);
            NumLevelsTextBox.Text = string.IsNullOrWhiteSpace(options.NumLevels) ? "auto" : options.NumLevels;
            LimitToRoiCheckBox.IsChecked = options.LimitToSearchRoi;
        }

        private TemplateMatchOptions ReadOptions()
        {
            return new TemplateMatchOptions
            {
                MinScore = ReadDouble(MinScoreTextBox.Text, "最小分数"),
                MaxMatches = Math.Max(1, ReadInt(MaxMatchesTextBox.Text, "匹配数量")),
                AngleStartDeg = ReadDouble(AngleStartTextBox.Text, "角度起点"),
                AngleExtentDeg = ReadDouble(AngleExtentTextBox.Text, "角度范围"),
                MaxOverlap = Clamp(ReadDouble(MaxOverlapTextBox.Text, "最大重叠"), 0, 1),
                Greediness = Clamp(ReadDouble(GreedinessTextBox.Text, "贪婪度"), 0, 1),
                NumLevels = string.IsNullOrWhiteSpace(NumLevelsTextBox.Text) ? "auto" : NumLevelsTextBox.Text.Trim(),
                Metric = "use_polarity",
                SubPixel = "least_squares",
                LimitToSearchRoi = LimitToRoiCheckBox.IsChecked == true
            };
        }

        private static void ValidateRoi(RoiData roi)
        {
            if (roi == null)
            {
                throw new InvalidOperationException("请先选择模板区域。");
            }

            if (roi.ShapeType == RoiShapeType.Circle)
            {
                if (roi.Radius < 4)
                {
                    throw new InvalidOperationException("圆形模板区域太小，请重新框选。");
                }

                return;
            }

            double width = Math.Abs(roi.Column2 - roi.Column1);
            double height = Math.Abs(roi.Row2 - roi.Row1);
            if (width < 4 || height < 4)
            {
                throw new InvalidOperationException("模板区域太小，请重新框选。");
            }
        }

        private static double ReadDouble(string text, string label)
        {
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new InvalidOperationException(label + "格式不正确。");
            }

            return value;
        }

        private static int ReadInt(string text, string label)
        {
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                throw new InvalidOperationException(label + "格式不正确。");
            }

            return value;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
