using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public sealed class TemplateCreateForm : Form
    {
        private readonly HImage image;
        private RoiData initialRoi;
        private readonly string templateName;
        private readonly ImageViewportController viewport = new ImageViewportController();
        private readonly RoiEditor roiEditor = new RoiEditor();
        private readonly OverlayRenderer overlayRenderer = new OverlayRenderer();
        private readonly List<HRegion> maskUndoStack = new List<HRegion>();

        private HWindowControl imageWindow;
        private Label statusLabel;
        private Button rectButton;
        private Button circleButton;
        private Button polygonButton;
        private Button confirmButton;
        private Button useCurrentRoiButton;
        private Button clearButton;
        private Button saveTrainButton;
        private Button maskRectButton;
        private Button maskCircleButton;
        private Button maskBrushButton;
        private Button maskEraserButton;
        private Button maskUndoButton;
        private Button resetMaskButton;
        private CheckBox showMaskCheckBox;
        private CheckBox showFrameCheckBox;
        private NumericUpDown minScoreInput;
        private NumericUpDown maxMatchesInput;
        private NumericUpDown angleStartInput;
        private NumericUpDown angleExtentInput;
        private NumericUpDown maxOverlapInput;
        private NumericUpDown greedinessInput;
        private NumericUpDown brushSizeInput;
        private ComboBox numLevelsCombo;
        private ComboBox metricCombo;
        private ComboBox subPixelCombo;

        private RoiData confirmedRoi;
        private RoiData pendingRoi;
        private HRegion trainingMask;
        private bool trainingMaskApplied;
        private bool panning;
        private bool brushEditing;
        private Point lastPanPoint;

        public TemplateCreateForm(HImage sourceImage, RoiData currentRoi, string name, TemplateMatchOptions options)
        {
            if (sourceImage == null)
            {
                throw new ArgumentNullException("sourceImage");
            }

            image = sourceImage.CopyImage();
            initialRoi = currentRoi == null ? null : currentRoi.Clone();
            confirmedRoi = null;
            templateName = string.IsNullOrWhiteSpace(name) ? "Template" : name;

            Text = "创建模板 - " + templateName;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1180, 760);
            Width = 1320;
            Height = 840;
            Font = new Font("Microsoft YaHei UI", 9F);

            BuildLayout();
            WireEvents();
            ApplyOptions(options ?? new TemplateMatchOptions());
            InitViewport();
            UpdateStatus();
        }

        public TemplateDefinition ResultDefinition { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                image.Dispose();
                roiEditor.Dispose();
                DisposeRoi(ref confirmedRoi);
                DisposeRoi(ref pendingRoi);
                DisposeRoi(ref initialRoi);
                DisposeTrainingMask();
                foreach (HRegion region in maskUndoStack)
                {
                    if (region != null)
                    {
                        region.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel imagePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            imageWindow = new HWindowControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderColor = Color.FromArgb(39, 48, 64),
                ImagePart = new Rectangle(0, 0, 640, 480),
                TabStop = true
            };
            imagePanel.Controls.Add(imageWindow);
            content.Controls.Add(imagePanel, 0, 0);

            Panel side = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0, 0, 8, 0) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(42, 54, 71)
            };
            AddSection(layout, "流程提示", statusLabel, 96);

            FlowLayoutPanel roiTools = ToolPanel();
            useCurrentRoiButton = CreateToolButton("使用当前ROI");
            rectButton = CreateToolButton("矩形");
            circleButton = CreateToolButton("圆形");
            polygonButton = CreateToolButton("多边形");
            clearButton = CreateToolButton("清除");
            roiTools.Controls.AddRange(new Control[] { useCurrentRoiButton, rectButton, circleButton, polygonButton, clearButton });
            AddSection(layout, "1 模板区域", roiTools, 124);

            FlowLayoutPanel maskTools = ToolPanel();
            resetMaskButton = CreateToolButton("ROI转Mask");
            maskRectButton = CreateToolButton("屏蔽矩形");
            maskCircleButton = CreateToolButton("屏蔽圆形");
            maskBrushButton = CreateToolButton("画笔屏蔽");
            maskEraserButton = CreateToolButton("橡皮擦恢复");
            maskUndoButton = CreateToolButton("撤销");
            showMaskCheckBox = new CheckBox { Text = "显示Mask", AutoSize = true, Margin = new Padding(6, 10, 8, 3) };
            brushSizeInput = Number(2, 200, 20, 2, 0);
            maskTools.Controls.AddRange(new Control[] { resetMaskButton, maskRectButton, maskCircleButton, maskBrushButton, maskEraserButton, maskUndoButton, showMaskCheckBox, new Label { Text = "画笔", AutoSize = true, Margin = new Padding(6, 10, 0, 3) }, brushSizeInput });
            AddSection(layout, "2 屏蔽区域", maskTools, 146);

            TableLayoutPanel paramLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            paramLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            paramLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            minScoreInput = Number(0, 1, 0.6M, 0.05M, 2);
            maxMatchesInput = Number(1, 50, 1, 1, 0);
            angleStartInput = Number(-360, 360, -180, 5, 0);
            angleExtentInput = Number(1, 720, 360, 5, 0);
            maxOverlapInput = Number(0, 1, 0.5M, 0.05M, 2);
            greedinessInput = Number(0, 1, 0.9M, 0.05M, 2);
            numLevelsCombo = Combo("auto", "1", "2", "3", "4", "5", "6");
            metricCombo = Combo("use_polarity", "ignore_global_polarity", "ignore_local_polarity", "ignore_color_polarity");
            subPixelCombo = Combo("least_squares", "interpolation", "none");
            AddRow(paramLayout, "最小分数", minScoreInput);
            AddRow(paramLayout, "匹配数量", maxMatchesInput);
            AddRow(paramLayout, "起始角度", angleStartInput);
            AddRow(paramLayout, "角度范围", angleExtentInput);
            AddRow(paramLayout, "最大重叠", maxOverlapInput);
            AddRow(paramLayout, "贪婪度", greedinessInput);
            AddRow(paramLayout, "金字塔", numLevelsCombo);
            AddRow(paramLayout, "极性", metricCombo);
            AddRow(paramLayout, "亚像素", subPixelCombo);
            AddSection(layout, "3 参数设置", paramLayout, 336);

            FlowLayoutPanel previewTools = ToolPanel();
            showFrameCheckBox = new CheckBox { Text = "显示外接矩形框", AutoSize = true, Margin = new Padding(6, 10, 8, 3) };
            previewTools.Controls.Add(showFrameCheckBox);
            AddSection(layout, "4 训练预览", previewTools, 72);

            side.Controls.Add(layout);
            content.Controls.Add(side, 1, 0);
            root.Controls.Add(content, 0, 0);
            root.Controls.Add(CreateActionBar(), 0, 1);
            Controls.Add(root);
        }

        private Panel CreateActionBar()
        {
            Panel panel = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(8), BackColor = Color.FromArgb(246, 248, 251) };
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Right, WrapContents = false, AutoSize = true };
            confirmButton = CreatePrimaryButton("确认区域");
            saveTrainButton = CreatePrimaryButton("保存并训练");
            Button cancelButton = CreatePrimaryButton("取消");
            cancelButton.DialogResult = DialogResult.Cancel;
            buttons.Controls.AddRange(new Control[] { confirmButton, saveTrainButton, cancelButton });
            panel.Controls.Add(buttons);
            return panel;
        }

        private void WireEvents()
        {
            rectButton.Click += delegate { SelectTool(VisionTool.RectangleRoi); };
            circleButton.Click += delegate { SelectTool(VisionTool.CircleRoi); };
            polygonButton.Click += delegate { SelectTool(VisionTool.PolygonRoi); };
            useCurrentRoiButton.Click += delegate { UseInitialRoi(); };
            confirmButton.Click += delegate { ConfirmRegion(); };
            clearButton.Click += delegate { ClearRegion(); };
            resetMaskButton.Click += delegate { ResetMaskFromRoi(); };
            maskRectButton.Click += delegate { SelectTool(VisionTool.MaskRectangleAdd); };
            maskCircleButton.Click += delegate { SelectTool(VisionTool.MaskCircleAdd); };
            maskBrushButton.Click += delegate { SelectTool(VisionTool.MaskBrushAdd); };
            maskEraserButton.Click += delegate { SelectTool(VisionTool.MaskEraser); };
            maskUndoButton.Click += delegate { UndoMaskEdit(); };
            showMaskCheckBox.CheckedChanged += delegate { RefreshDisplay(); };
            showFrameCheckBox.CheckedChanged += delegate { RefreshDisplay(); };
            saveTrainButton.Click += delegate { SaveAndClose(); };

            imageWindow.MouseEnter += delegate { imageWindow.Focus(); };
            imageWindow.MouseDown += ImageWindowMouseDown;
            imageWindow.MouseMove += ImageWindowMouseMove;
            imageWindow.MouseUp += ImageWindowMouseUp;
            imageWindow.MouseWheel += ImageWindowMouseWheel;
            imageWindow.MouseDoubleClick += ImageWindowMouseDoubleClick;
        }

        private void InitViewport()
        {
            int width;
            int height;
            image.GetImageSize(out width, out height);
            viewport.SetImageSize(width, height);
            RefreshDisplay();
        }

        private void SelectTool(VisionTool tool)
        {
            roiEditor.Tool = tool;
            Highlight(rectButton, tool == VisionTool.RectangleRoi);
            Highlight(circleButton, tool == VisionTool.CircleRoi);
            Highlight(polygonButton, tool == VisionTool.PolygonRoi);
            Highlight(maskRectButton, tool == VisionTool.MaskRectangleAdd);
            Highlight(maskCircleButton, tool == VisionTool.MaskCircleAdd);
            Highlight(maskBrushButton, tool == VisionTool.MaskBrushAdd);
            Highlight(maskEraserButton, tool == VisionTool.MaskEraser);
            UpdateStatus();
        }

        private void ImageWindowMouseDown(object sender, MouseEventArgs e)
        {
            imageWindow.Focus();
            if (e.Button == MouseButtons.Right)
            {
                panning = true;
                lastPanPoint = e.Location;
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            PointF imagePoint = ClampImagePoint(viewport.WindowToImage(e.Location, imageWindow));
            if (roiEditor.Tool == VisionTool.PolygonRoi)
            {
                roiEditor.AddPolygonPoint(imagePoint);
                RefreshDisplay();
                UpdateStatus();
                return;
            }

            if (RoiEditor.IsBrushTool(roiEditor.Tool))
            {
                PushMaskUndo();
                brushEditing = true;
                ApplyBrushEdit(imagePoint);
                return;
            }

            roiEditor.Begin(imagePoint);
        }

        private void ImageWindowMouseMove(object sender, MouseEventArgs e)
        {
            PointF imagePoint = ClampImagePoint(viewport.WindowToImage(e.Location, imageWindow));
            if (panning)
            {
                viewport.PanByWindowDelta(e.X - lastPanPoint.X, e.Y - lastPanPoint.Y, imageWindow);
                lastPanPoint = e.Location;
                RefreshDisplay();
                return;
            }

            if (roiEditor.IsDrawing)
            {
                roiEditor.Update(imagePoint);
                RefreshDisplay();
                return;
            }

            if (roiEditor.IsPolygonDrawing)
            {
                roiEditor.UpdatePolygon(imagePoint);
                RefreshDisplay();
                return;
            }

            if (brushEditing)
            {
                ApplyBrushEdit(imagePoint);
            }
        }

        private void ImageWindowMouseUp(object sender, MouseEventArgs e)
        {
            if (brushEditing)
            {
                brushEditing = false;
                UpdateStatus();
                return;
            }

            if (panning)
            {
                panning = false;
                return;
            }

            if (!roiEditor.IsDrawing)
            {
                return;
            }

            RoiData roi = roiEditor.Complete(ClampImagePoint(viewport.WindowToImage(e.Location, imageWindow)));
            if (RoiEditor.IsMaskShapeTool(roiEditor.Tool))
            {
                AddMaskRegion(roi, true, true);
            }
            else
            {
                SetPendingRoi(roi);
            }

            RefreshDisplay();
            UpdateStatus();
        }

        private void ImageWindowMouseWheel(object sender, MouseEventArgs e)
        {
            viewport.ZoomAt(e.Location, imageWindow, e.Delta);
            RefreshDisplay();
        }

        private void ImageWindowMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (roiEditor.IsPolygonDrawing)
            {
                RoiData roi = roiEditor.CompletePolygon();
                if (roi != null)
                {
                    SetPendingRoi(roi);
                }

                RefreshDisplay();
                UpdateStatus();
                return;
            }

            viewport.Fit();
            RefreshDisplay();
        }

        private void UseInitialRoi()
        {
            if (initialRoi == null)
            {
                MessageBox.Show(this, "当前没有已确认 ROI。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetPendingRoi(initialRoi.Clone());
            RefreshDisplay();
            UpdateStatus();
        }

        private void ConfirmRegion()
        {
            if (pendingRoi == null && roiEditor.IsPolygonDrawing)
            {
                SetPendingRoi(roiEditor.CompletePolygon());
            }

            if (pendingRoi == null)
            {
                MessageBox.Show(this, "请先绘制模板区域。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string validationMessage;
            if (!TryValidateTemplateRoi(pendingRoi, out validationMessage))
            {
                MessageBox.Show(this, validationMessage, "模板区域无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetConfirmedRoi(pendingRoi.Clone());
            DisposeRoi(ref pendingRoi);
            ResetMaskFromRoi();
            RefreshDisplay();
            UpdateStatus();
        }

        private void ClearRegion()
        {
            DisposeRoi(ref pendingRoi);
            DisposeRoi(ref confirmedRoi);
            roiEditor.Cancel();
            DisposeTrainingMask();
            trainingMaskApplied = false;
            RefreshDisplay();
            UpdateStatus();
        }

        private void SaveAndClose()
        {
            if (confirmedRoi == null)
            {
                MessageBox.Show(this, "请先确认模板区域。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string validationMessage;
            if (!TryValidateTemplateRoi(confirmedRoi, out validationMessage))
            {
                MessageBox.Show(this, validationMessage, "模板区域无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HRegion mask = trainingMaskApplied && trainingMask != null
                ? TemplateDefinition.CloneRegion(trainingMask)
                : TemplateDefinition.CloneRegion(confirmedRoi.Region);

            ResultDefinition = new TemplateDefinition
            {
                TemplateName = templateName,
                TemplateRoi = confirmedRoi.Clone(),
                TrainingMask = mask,
                DisplayFrame = confirmedRoi.Clone(),
                Options = ReadOptions()
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ResetMaskFromRoi()
        {
            if (confirmedRoi == null)
            {
                return;
            }

            DisposeTrainingMask();
            trainingMask = TemplateDefinition.CloneRegion(confirmedRoi.Region);
            trainingMaskApplied = false;
            RefreshDisplay();
            UpdateStatus();
        }

        private bool TryValidateTemplateRoi(RoiData roi, out string message)
        {
            message = null;
            if (roi == null)
            {
                message = "请先绘制模板区域。";
                return false;
            }

            double minRow;
            double minColumn;
            double maxRow;
            double maxColumn;
            GetRoiBounds(roi, out minRow, out minColumn, out maxRow, out maxColumn);

            if (minRow < 0 || minColumn < 0 || maxRow >= viewport.ImageHeight || maxColumn >= viewport.ImageWidth)
            {
                message = "模板区域必须完整位于图像范围内，请重新框选。";
                return false;
            }

            double width = maxColumn - minColumn;
            double height = maxRow - minRow;
            if (width < 8 || height < 8)
            {
                message = "模板区域太小，请至少框选 8x8 像素以上的目标。";
                return false;
            }

            if (roi.ShapeType == RoiShapeType.Circle && roi.Radius < 4)
            {
                message = "圆形模板半径太小，请重新框选更大的目标。";
                return false;
            }

            if (roi.ShapeType == RoiShapeType.Polygon)
            {
                if (roi.PolygonRows == null || roi.PolygonRows.Length < 3)
                {
                    message = "多边形模板至少需要 3 个点。";
                    return false;
                }

                double area = PolygonArea(roi);
                if (area < 64)
                {
                    message = "多边形模板面积太小，请重新框选更大的目标。";
                    return false;
                }
            }

            return true;
        }

        private static void GetRoiBounds(RoiData roi, out double minRow, out double minColumn, out double maxRow, out double maxColumn)
        {
            if (roi.ShapeType == RoiShapeType.Circle)
            {
                minRow = roi.Row - roi.Radius;
                maxRow = roi.Row + roi.Radius;
                minColumn = roi.Column - roi.Radius;
                maxColumn = roi.Column + roi.Radius;
                return;
            }

            minRow = Math.Min(roi.Row1, roi.Row2);
            maxRow = Math.Max(roi.Row1, roi.Row2);
            minColumn = Math.Min(roi.Column1, roi.Column2);
            maxColumn = Math.Max(roi.Column1, roi.Column2);
        }

        private static double PolygonArea(RoiData roi)
        {
            double area = 0;
            for (int i = 0; i < roi.PolygonRows.Length; i++)
            {
                int next = (i + 1) % roi.PolygonRows.Length;
                area += roi.PolygonColumns[i] * roi.PolygonRows[next] - roi.PolygonColumns[next] * roi.PolygonRows[i];
            }

            return Math.Abs(area) * 0.5;
        }

        private void AddMaskRegion(RoiData roi, bool shield, bool pushUndo)
        {
            if (roi == null)
            {
                return;
            }

            try
            {
                if (pushUndo)
                {
                    PushMaskUndo();
                }

                if (trainingMask == null && confirmedRoi != null)
                {
                    trainingMask = TemplateDefinition.CloneRegion(confirmedRoi.Region);
                }

                if (trainingMask == null)
                {
                    return;
                }

                HObject updated = null;
                HObject restoreArea = null;
                if (shield)
                {
                    HOperatorSet.Difference(trainingMask, roi.Region, out updated);
                }
                else
                {
                    HOperatorSet.Intersection(roi.Region, confirmedRoi.Region, out restoreArea);
                    HOperatorSet.Union2(trainingMask, restoreArea, out updated);
                }

                DisposeTrainingMask();
                trainingMask = new HRegion(updated);
                trainingMaskApplied = true;
                if (restoreArea != null)
                {
                    restoreArea.Dispose();
                }
            }
            finally
            {
                roi.Dispose();
            }
        }

        private void ApplyBrushEdit(PointF imagePoint)
        {
            imagePoint = ClampImagePoint(imagePoint);
            RoiData brush = RoiData.CreateCircle(imagePoint.Y, imagePoint.X, (double)brushSizeInput.Value);
            AddMaskRegion(brush, roiEditor.Tool != VisionTool.MaskEraser, false);
            RefreshDisplay();
            UpdateStatus();
        }

        private PointF ClampImagePoint(PointF point)
        {
            float x = Math.Max(0, Math.Min(Math.Max(0, viewport.ImageWidth - 1), point.X));
            float y = Math.Max(0, Math.Min(Math.Max(0, viewport.ImageHeight - 1), point.Y));
            return new PointF(x, y);
        }

        private void PushMaskUndo()
        {
            maskUndoStack.Add(TemplateDefinition.CloneRegion(trainingMask));
            if (maskUndoStack.Count > 20)
            {
                HRegion oldest = maskUndoStack[0];
                maskUndoStack.RemoveAt(0);
                if (oldest != null)
                {
                    oldest.Dispose();
                }
            }
        }

        private void UndoMaskEdit()
        {
            if (maskUndoStack.Count == 0)
            {
                return;
            }

            HRegion previous = maskUndoStack[maskUndoStack.Count - 1];
            maskUndoStack.RemoveAt(maskUndoStack.Count - 1);
            DisposeTrainingMask();
            trainingMask = previous;
            trainingMaskApplied = trainingMask != null;
            RefreshDisplay();
            UpdateStatus();
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
            HRegion visibleMask = showMaskCheckBox != null && showMaskCheckBox.Checked ? trainingMask : null;
            RoiData preview = pendingRoi ?? roiEditor.PreviewRoi;
            RoiData visibleTemplate = confirmedRoi ?? initialRoi;
            overlayRenderer.Draw(imageWindow.HalconWindow, null, visibleTemplate, preview, visibleMask, showFrameCheckBox != null && showFrameCheckBox.Checked ? confirmedRoi : null, null, null, false, visibleTemplate != null, showFrameCheckBox != null && showFrameCheckBox.Checked);
        }

        private void UpdateStatus()
        {
            string roiText = confirmedRoi == null ? "1 选择模板区域：点击“使用当前ROI”或选择矩形/圆形/多边形后在图像上框选。" : "模板区域已确认: " + confirmedRoi.DisplayText;
            if (pendingRoi != null || roiEditor.IsPolygonDrawing)
            {
                roiText = "2 确认区域：当前区域待确认，请点击底部“确认区域”。";
            }

            string maskText = trainingMaskApplied ? "屏蔽区已应用" : "未应用屏蔽区";
            string trainText = confirmedRoi == null ? "3 保存并训练：请先确认模板区域，按钮暂不可用。" : "3 保存并训练：可点击底部按钮完成模板训练。";
            statusLabel.Text = roiText + Environment.NewLine + trainText + Environment.NewLine + maskText + "；右键拖动平移，滚轮缩放。";
            confirmButton.Enabled = pendingRoi != null || roiEditor.IsPolygonDrawing;
            saveTrainButton.Enabled = confirmedRoi != null;
            maskRectButton.Enabled = confirmedRoi != null;
            maskCircleButton.Enabled = confirmedRoi != null;
            maskBrushButton.Enabled = confirmedRoi != null;
            maskEraserButton.Enabled = trainingMask != null;
            maskUndoButton.Enabled = maskUndoStack.Count > 0;
        }

        private TemplateMatchOptions ReadOptions()
        {
            return new TemplateMatchOptions
            {
                MinScore = (double)minScoreInput.Value,
                MaxMatches = (int)maxMatchesInput.Value,
                AngleStartDeg = (double)angleStartInput.Value,
                AngleExtentDeg = (double)angleExtentInput.Value,
                MaxOverlap = (double)maxOverlapInput.Value,
                Greediness = (double)greedinessInput.Value,
                NumLevels = Convert.ToString(numLevelsCombo.SelectedItem),
                Metric = Convert.ToString(metricCombo.SelectedItem),
                SubPixel = Convert.ToString(subPixelCombo.SelectedItem),
                LimitToSearchRoi = false
            };
        }

        private void ApplyOptions(TemplateMatchOptions options)
        {
            minScoreInput.Value = ClampDecimal((decimal)options.MinScore, minScoreInput.Minimum, minScoreInput.Maximum);
            maxMatchesInput.Value = ClampDecimal(options.MaxMatches, maxMatchesInput.Minimum, maxMatchesInput.Maximum);
            angleStartInput.Value = ClampDecimal((decimal)options.AngleStartDeg, angleStartInput.Minimum, angleStartInput.Maximum);
            angleExtentInput.Value = ClampDecimal((decimal)options.AngleExtentDeg, angleExtentInput.Minimum, angleExtentInput.Maximum);
            maxOverlapInput.Value = ClampDecimal((decimal)options.MaxOverlap, maxOverlapInput.Minimum, maxOverlapInput.Maximum);
            greedinessInput.Value = ClampDecimal((decimal)options.Greediness, greedinessInput.Minimum, greedinessInput.Maximum);
            SelectComboValue(numLevelsCombo, options.NumLevels);
            SelectComboValue(metricCombo, options.Metric);
            SelectComboValue(subPixelCombo, options.SubPixel);
        }

        private void SetPendingRoi(RoiData roi)
        {
            DisposeRoi(ref pendingRoi);
            pendingRoi = roi;
        }

        private void SetConfirmedRoi(RoiData roi)
        {
            DisposeRoi(ref confirmedRoi);
            confirmedRoi = roi;
        }

        private void DisposeTrainingMask()
        {
            if (trainingMask != null)
            {
                trainingMask.Dispose();
                trainingMask = null;
            }
        }

        private static void DisposeRoi(ref RoiData roi)
        {
            if (roi != null)
            {
                roi.Dispose();
                roi = null;
            }
        }

        private static FlowLayoutPanel ToolPanel()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(2), AutoScroll = true };
        }

        private static Button CreateToolButton(string text)
        {
            return new Button { Text = text, Width = 108, MinimumSize = new Size(96, 34), Height = 34, Margin = new Padding(3), FlatStyle = FlatStyle.System };
        }

        private static Button CreatePrimaryButton(string text)
        {
            return new Button { Text = text, Width = 128, MinimumSize = new Size(120, 38), Height = 38, Margin = new Padding(6, 3, 0, 3), FlatStyle = FlatStyle.System };
        }

        private static void Highlight(Button button, bool active)
        {
            button.BackColor = active ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
        }

        private static NumericUpDown Number(decimal min, decimal max, decimal value, decimal increment, int decimals)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Increment = increment, DecimalPlaces = decimals, Dock = DockStyle.Left, Width = 120 };
        }

        private static ComboBox Combo(params string[] values)
        {
            ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 180 };
            combo.Items.AddRange(values);
            combo.SelectedIndex = 0;
            return combo;
        }

        private static void AddRow(TableLayoutPanel layout, string labelText, Control control)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private static void AddSection(TableLayoutPanel layout, string title, Control content, int height)
        {
            GroupBox group = new GroupBox { Text = title, Dock = DockStyle.Top, Height = height, Padding = new Padding(8), Margin = new Padding(3, 3, 3, 8) };
            content.Dock = DockStyle.Fill;
            group.Controls.Add(content);
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height + 10));
            layout.Controls.Add(group, 0, row);
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static void SelectComboValue(ComboBox combo, string value)
        {
            if (combo == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            int index = combo.Items.IndexOf(value);
            if (index >= 0)
            {
                combo.SelectedIndex = index;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TemplateCreateForm
            // 
            this.ClientSize = new System.Drawing.Size(968, 482);
            this.Name = "TemplateCreateForm";
            this.ResumeLayout(false);

        }
    }
}
