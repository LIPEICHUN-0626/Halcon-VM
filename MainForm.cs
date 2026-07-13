using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public sealed class MainForm : Form
    {
#if DEBUG
        private const string BuildConfigName = "Debug";
#else
        private const string BuildConfigName = "Release";
#endif
        private const string VmBuildStamp = "VM优化版 " + BuildConfigName + " 2026-06-22";

        private readonly HalconImageService imageService = new HalconImageService();
        private readonly HDevInspectionService inspectionService = new HDevInspectionService();
        private readonly ShapeTemplateService templateService = new ShapeTemplateService();
        private readonly InspectionResultStore resultStore = new InspectionResultStore();
        private readonly CsvExportService csvExportService = new CsvExportService();
        private readonly XlsxExportService xlsxExportService = new XlsxExportService();
        private readonly TcpCommunicationService tcpService = new TcpCommunicationService();
        private readonly AppLogger logger = new AppLogger();
        private readonly ImageViewportController viewport = new ImageViewportController();
        private readonly RoiEditor roiEditor = new RoiEditor();
        private readonly OverlayRenderer overlayRenderer = new OverlayRenderer();
        private readonly BindingList<ResultGridRow> gridRows = new BindingList<ResultGridRow>();
        private readonly List<string> imageQueue = new List<string>();
        private readonly Timer imagePlayTimer = new Timer();

        private SplitContainer rootSplit;
        private SplitContainer mainSplit;
        private HWindowControl imageWindow;
        private TextBox logTextBox;
        private TabControl rightTabs;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel modeStatus;
        private ToolStripStatusLabel imageStatus;
        private ToolStripStatusLabel zoomStatus;
        private ToolStripStatusLabel mouseStatus;
        private ToolStripStatusLabel roiStatus;
        private ToolStripStatusLabel templateStatus;

        private RadioButton cameraModeRadio;
        private RadioButton fileModeRadio;
        private ComboBox cameraInterfaceCombo;
        private TextBox cameraDeviceTextBox;
        private Button openCameraButton;
        private Button grabButton;
        private Button continuousButton;
        private Button stopGrabButton;
        private Button closeCameraButton;
        private Button loadImagesButton;
        private Button loadFolderButton;
        private Button prevImageButton;
        private Button nextImageButton;
        private Button playButton;
        private Button stopPlayButton;
        private Button toGrayButton;
        private Button toColorButton;
        private Button restoreOriginalButton;
        private Label queueLabel;
        private Label imageNavLabel;

        private Button selectToolButton;
        private Button panToolButton;
        private Button rectToolButton;
        private Button circleToolButton;
        private Button polygonToolButton;
        private Button clearRoiButton;
        private Button fitButton;
        private Button clearOverlayButton;
        private Button saveViewButton;

        private NumericUpDown minScoreInput;
        private NumericUpDown maxMatchesInput;
        private NumericUpDown angleStartInput;
        private NumericUpDown angleExtentInput;
        private Button trainTemplateButton;
        private Button saveTemplateButton;
        private Button loadTemplateButton;
        private Button runTemplateButton;
        private Button addTemplateButton;
        private Button renameTemplateButton;
        private Button deleteTemplateButton;
        private Button templateRectToolButton;
        private Button templateCircleToolButton;
        private Button confirmTemplateRoiButton;
        private Button discardPendingTemplateRoiButton;
        private Button confirmRoiButton;
        private Button discardPendingRoiButton;
        private Button confirmTrainingMaskButton;
        private Button resetParamsButton;
        private Button toggleAdvancedParamsButton;
        private Button toggleMaskToolsButton;
        private Button resetMaskButton;
        private Button undoMaskButton;
        private Button maskRectButton;
        private Button maskCircleButton;
        private Button maskBrushButton;
        private Button maskEraserButton;
        private Button previewMaskButton;
        private Button useRoiAsFrameButton;
        private Button moveFrameLeftButton;
        private Button moveFrameRightButton;
        private Button moveFrameUpButton;
        private Button moveFrameDownButton;
        private Button growFrameButton;
        private Button shrinkFrameButton;
        private Label templateInfoLabel;
        private NumericUpDown maxOverlapInput;
        private NumericUpDown greedinessInput;
        private NumericUpDown brushSizeInput;
        private ComboBox numLevelsCombo;
        private ComboBox metricCombo;
        private ComboBox subPixelCombo;
        private CheckBox limitToSearchRoiCheckBox;
        private CheckBox showTrainingMaskCheckBox;
        private CheckBox showDisplayFrameCheckBox;
        private CheckBox showSearchRoiCheckBox;
        private Panel advancedParameterPanel;
        private GroupBox advancedMaskGroup;
        private Label roiWorkflowLabel;
        private Label maskWorkflowLabel;
        private Label matchSummaryLabel;
        private int advancedParamsRow = -1;
        private DataGridView templateGrid;
        private TextBox hdevPathTextBox;
        private TextBox procedureNameTextBox;
        private Button browseHdevButton;
        private Button runHdevButton;
        private Button openHdevExampleButton;

        private DateTimePicker startTimePicker;
        private DateTimePicker endTimePicker;
        private CheckBox startTimeCheckBox;
        private CheckBox endTimeCheckBox;
        private TextBox resultFilterTextBox;
        private TextBox sourceFilterTextBox;
        private DataGridView resultGrid;
        private Button queryButton;
        private Button exportCsvButton;
        private Button exportXlsxButton;
        private Button clearResultsButton;
        private Button replayResultButton;

        private RadioButton tcpClientModeRadio;
        private RadioButton tcpServerModeRadio;
        private Label tcpIpLabel;
        private TextBox tcpIpTextBox;
        private NumericUpDown tcpPortInput;
        private ComboBox tcpEncodingCombo;
        private CheckBox tcpAppendNewLineCheckBox;
        private CheckBox autoSendMatchResultCheckBox;
        private TextBox tcpSendTextBox;
        private TextBox tcpHistoryTextBox;
        private Button tcpConnectButton;
        private Button tcpDisconnectButton;
        private Button tcpStartServerButton;
        private Button tcpStopServerButton;
        private Button tcpSendButton;
        private Button sendLastMatchButton;
        private Label tcpStatusLabel;
        private GroupBox tcpClientGroup;
        private GroupBox tcpServerGroup;

        private HImage currentImage;
        private HImage originalImage;
        private string currentImageSource = "Unknown";
        private RoiData currentRoi;
        private RoiData pendingSearchRoi;
        private RoiData pendingTemplateRoi;
        private HRegion trainingMask;
        private RoiData displayFrame;
        private readonly List<HRegion> maskUndoStack = new List<HRegion>();
        private readonly BindingList<TemplateItem> templateItems = new BindingList<TemplateItem>();
        private TemplateItem currentTemplateItem;
        private int nextTemplateIndex = 1;
        private bool trainingMaskConfirmed;
        private List<ShapeMatchResult> currentMatches = new List<ShapeMatchResult>();
        private List<InspectionRecord> filteredRecords = new List<InspectionRecord>();
        private string lastMatchMessage;
        private int currentImageIndex = -1;
        private bool playbackActive;
        private bool panning;
        private bool brushEditing;
        private FrameEditMode frameEditMode;
        private PointF frameEditStart;
        private RoiData frameEditOriginal;
        private Point lastPanPoint;
        private bool displayRefreshPending;

        public MainForm()
        {
            Text = "HALCON 视觉检测工作站 - " + VmBuildStamp;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1360, 860);
            Width = 1500;
            Height = 940;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(239, 242, 247);
            KeyPreview = true;
            AllowDrop = true;

            BuildLayout();
            WireEvents();
            SelectTool(VisionTool.Select);
            RefreshResultGrid();
            RefreshUiState();
            logger.Info("应用启动：" + VmBuildStamp);
            logger.Info("当前程序路径：" + Application.ExecutablePath);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 等 WinForms 完成第一次布局后，再设置 SplitContainer 的最小尺寸和分割位置。
            // 这样可以避免控件还没有实际宽高时，提前设置 PanelMinSize / SplitterDistance 导致异常。
            BeginInvoke(new Action(delegate
            {
                ApplySplitLayout();
            }));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            imagePlayTimer.Stop();
            imageService.StopContinuousGrab();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCurrentImage();
                DisposeOriginalImage();
                DisposeCurrentRoi();
                DisposePendingSearchRoi();
                DisposePendingTemplateRoi();
                DisposeTrainingMask();
                DisposeDisplayFrame();
                DisposeMaskUndoStack();
                DisposeTemplates();
                EndDisplayFrameEdit();
                roiEditor.Dispose();
                imageService.Dispose();
                inspectionService.Dispose();
                tcpService.Dispose();
                templateService.Dispose();
                resultStore.Dispose();
                logger.Dispose();
                imagePlayTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void BuildLayout()
        {
            // 注意：这里不要直接设置 Panel1MinSize / Panel2MinSize / SplitterDistance。
            // SplitContainer 在刚 new 出来时还没有完成布局，Width / Height 可能还是默认值。
            // 如果这时候设置最小尺寸，容易触发：
            // SplitterDistance 必须在 Panel1MinSize 和 Width - Panel2MinSize 之间。
            rootSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 7
            };

            mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 7,
                FixedPanel = FixedPanel.Panel2
            };

            mainSplit.Panel1.Controls.Add(BuildVisionPanel());
            mainSplit.Panel2.Controls.Add(BuildRightPanel());
            rootSplit.Panel1.Controls.Add(mainSplit);
            rootSplit.Panel2.Controls.Add(BuildLogPanel());
            Controls.Add(rootSplit);
        }

        private void ScheduleRefreshDisplay()
        {
            if (displayRefreshPending || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            displayRefreshPending = true;
            BeginInvoke(new Action(delegate
            {
                displayRefreshPending = false;
                ClampMainLayout();
                RefreshDisplay();
            }));
        }

        private void ApplySplitLayout()
        {
            if (rootSplit == null || mainSplit == null)
            {
                return;
            }

            ClampMainLayout();

            // 再安全设置分割位置。
            // rootSplit：日志区大约保留 220 像素。
            SetSafeSplitterDistance(rootSplit, rootSplit.Height - 220);

            // mainSplit：右侧参数区大约保留 420 像素。
            SetSafeSplitterDistance(mainSplit, mainSplit.Width - 420);
        }

        private void ClampMainLayout()
        {
            if (rootSplit != null)
            {
                int total = rootSplit.Height - rootSplit.SplitterWidth;
                if (total > 0)
                {
                    int logMin = Math.Min(120, Math.Max(60, total / 5));
                    int mainMin = Math.Min(520, Math.Max(180, total - logMin));
                    if (mainMin + logMin > total)
                    {
                        logMin = Math.Max(40, Math.Min(logMin, total / 4));
                        mainMin = Math.Max(80, total - logMin);
                    }

                    rootSplit.Panel1MinSize = Math.Max(0, mainMin);
                    rootSplit.Panel2MinSize = Math.Max(0, Math.Min(logMin, total - rootSplit.Panel1MinSize));

                    int maxDistance = total - rootSplit.Panel2MinSize;
                    if (rootSplit.SplitterDistance < rootSplit.Panel1MinSize || rootSplit.SplitterDistance > maxDistance)
                    {
                        SetSafeSplitterDistance(rootSplit, Math.Min(Math.Max(rootSplit.SplitterDistance, rootSplit.Panel1MinSize), maxDistance));
                    }
                }
            }

            if (mainSplit != null)
            {
                int total = mainSplit.Width - mainSplit.SplitterWidth;
                if (total > 0)
                {
                    int rightMin = Math.Min(360, Math.Max(220, total / 4));
                    int leftMin = Math.Min(360, Math.Max(160, total - rightMin));
                    if (leftMin + rightMin > total)
                    {
                        rightMin = Math.Max(100, Math.Min(rightMin, total / 3));
                        leftMin = Math.Max(80, total - rightMin);
                    }

                    mainSplit.Panel1MinSize = Math.Max(0, leftMin);
                    mainSplit.Panel2MinSize = Math.Max(0, Math.Min(rightMin, total - mainSplit.Panel1MinSize));
                }
            }
        }

        private static void SetSafeSplitterDistance(SplitContainer split, int targetDistance)
        {
            if (split == null)
            {
                return;
            }

            int totalLength;
            if (split.Orientation == Orientation.Vertical)
            {
                totalLength = split.Width;
            }
            else
            {
                totalLength = split.Height;
            }

            int min = split.Panel1MinSize;
            int max = totalLength - split.Panel2MinSize - split.SplitterWidth;

            // 当前控件尺寸还不够容纳两个 Panel 的最小尺寸时，先不设置。
            // 等窗体显示或用户调整窗口后，布局会自动恢复。
            if (max < min)
            {
                return;
            }

            if (targetDistance < min)
            {
                targetDistance = min;
            }

            if (targetDistance > max)
            {
                targetDistance = max;
            }

            split.SplitterDistance = targetDistance;
        }

        private Control BuildVisionPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            FlowLayoutPanel toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                BackColor = Color.FromArgb(250, 251, 253),
                Padding = new Padding(2, 4, 2, 2)
            };
            loadImagesButton = CreateToolbarButton("打开图片");
            loadFolderButton = CreateToolbarButton("打开文件夹");
            prevImageButton = CreateToolbarButton("上一张");
            nextImageButton = CreateToolbarButton("下一张");
            playButton = CreateToolbarButton("自动播放");
            stopPlayButton = CreateToolbarButton("停止播放");
            toGrayButton = CreateToolbarButton("转灰度");
            toColorButton = CreateToolbarButton("转彩色");
            restoreOriginalButton = CreateToolbarButton("恢复原图");
            imageNavLabel = new Label { Text = "0/0", Width = 70, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(2, 2, 8, 2), ForeColor = Color.FromArgb(78, 89, 108) };
            selectToolButton = CreateToolbarButton("选择");
            panToolButton = CreateToolbarButton("平移");
            fitButton = CreateToolbarButton("适应窗口");
            saveViewButton = CreateToolbarButton("保存截图");
            toolbar.Controls.AddRange(new Control[]
            {
                loadImagesButton, loadFolderButton, prevImageButton, nextImageButton, imageNavLabel, playButton, stopPlayButton,
                toGrayButton, toColorButton, restoreOriginalButton,
                selectToolButton, panToolButton, fitButton, saveViewButton
            });

            Panel imagePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Padding = new Padding(0) };
            imageWindow = new HWindowControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderColor = Color.FromArgb(39, 48, 64),
                ImagePart = new Rectangle(0, 0, 640, 480),
                TabStop = true,
                AllowDrop = true
            };
            imagePanel.Controls.Add(imageWindow);

            statusStrip = new StatusStrip { Dock = DockStyle.Fill, SizingGrip = false };
            modeStatus = new ToolStripStatusLabel("模式: 本地图");
            imageStatus = new ToolStripStatusLabel("图像: --");
            zoomStatus = new ToolStripStatusLabel("Zoom: --");
            mouseStatus = new ToolStripStatusLabel("坐标: --");
            roiStatus = new ToolStripStatusLabel("ROI: 未设置");
            templateStatus = new ToolStripStatusLabel("模板: 未加载");
            statusStrip.Items.AddRange(new ToolStripItem[] { modeStatus, imageStatus, zoomStatus, mouseStatus, roiStatus, templateStatus });

            panel.Controls.Add(toolbar, 0, 0);
            panel.Controls.Add(imagePanel, 0, 1);
            panel.Controls.Add(statusStrip, 0, 2);
            return panel;
        }

        private Control BuildRightPanel()
        {
            rightTabs = new TabControl { Dock = DockStyle.Fill };
            rightTabs.TabPages.Add(BuildAcquisitionTab());
            rightTabs.TabPages.Add(BuildTemplateTab());
            rightTabs.TabPages.Add(BuildCommunicationTab());
            rightTabs.TabPages.Add(BuildResultsTab());
            return rightTabs;
        }

        private TabPage BuildAcquisitionTab()
        {
            TabPage tab = CreateTab("取图");
            TableLayoutPanel layout = CreateTabLayout(9);

            cameraModeRadio = new RadioButton { Text = "相机在线", Checked = false, Width = 100 };
            fileModeRadio = new RadioButton { Text = "本地图读取", Checked = true, Width = 120 };
            FlowLayoutPanel modePanel = CreateLinePanel();
            modePanel.Controls.Add(cameraModeRadio);
            modePanel.Controls.Add(fileModeRadio);
            AddFullRow(layout, modePanel, 34);

            cameraInterfaceCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cameraInterfaceCombo.Items.AddRange(new object[] { "GigEVision2", "USB3Vision", "GenICamTL", "DirectShow" });
            cameraInterfaceCombo.SelectedIndex = 0;
            cameraDeviceTextBox = new TextBox { Dock = DockStyle.Fill, Text = "default" };
            AddRow(layout, "相机接口", cameraInterfaceCombo);
            AddRow(layout, "设备名", cameraDeviceTextBox);

            FlowLayoutPanel cameraButtons = CreateLinePanel();
            openCameraButton = CreateButton("打开相机");
            grabButton = CreateButton("单次取图");
            continuousButton = CreateButton("连续采集");
            stopGrabButton = CreateButton("停止采集");
            closeCameraButton = CreateButton("关闭相机");
            cameraButtons.Controls.AddRange(new Control[] { openCameraButton, grabButton, continuousButton, stopGrabButton, closeCameraButton });
            AddFullRow(layout, cameraButtons, 76);

            queueLabel = CreateInfoLabel("本地图像的打开、上一张、下一张已放到图像窗口上方工具条。也可拖拽图片或文件夹到图像窗口。");
            AddFullRow(layout, queueLabel, 70);
            AddHelp(layout, "操作提示", "滚轮缩放，右键拖动平移；选择矩形/圆形 ROI 后在图像上左键拖拽绘制。");

            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage BuildTemplateTab()
        {
            TabPage tab = CreateTab("ROI/模板");
            Panel scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            GroupBox imageGroup = CreateWorkflowGroup("1 选择图片");
            TableLayoutPanel imageLayout = CreateWorkflowLayout(2);
            matchSummaryLabel = CreateInfoLabel("请使用图像区上方工具条打开或切换图片。");
            AddFullRow(imageLayout, matchSummaryLabel, 44);
            imageGroup.Controls.Add(imageLayout);
            AddWorkflowGroup(layout, imageGroup);

            GroupBox roiGroup = CreateWorkflowGroup("2 框选并确认 ROI");
            TableLayoutPanel roiLayout = CreateWorkflowLayout(4);
            roiWorkflowLabel = CreateInfoLabel("ROI: 未框选。选择形状后在图像上框选，确认后进入模板匹配设置。");
            AddFullRow(roiLayout, roiWorkflowLabel, 44);
            FlowLayoutPanel roiButtons = CreateLinePanel();
            rectToolButton = CreateButton("框选矩形ROI");
            circleToolButton = CreateButton("框选圆形ROI");
            polygonToolButton = CreateButton("多边形ROI");
            confirmRoiButton = CreateButton("确认ROI");
            discardPendingRoiButton = CreateButton("放弃框选");
            clearRoiButton = CreateButton("清除ROI");
            roiButtons.Controls.AddRange(new Control[] { rectToolButton, circleToolButton, polygonToolButton, confirmRoiButton, discardPendingRoiButton, clearRoiButton });
            AddFullRow(roiLayout, roiButtons, 76);
            roiGroup.Controls.Add(roiLayout);
            AddWorkflowGroup(layout, roiGroup);

            // 旧模板库控件保留为内部兼容入口，不再加入主流程界面，避免普通用户被多模板列表打断。
            templateGrid = new DataGridView
            {
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                Height = 120,
                DataSource = templateItems
            };
            templateGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "模板名称", Width = 110 });
            templateGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "状态", Width = 70 });
            templateGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SourceFile", HeaderText = "来源文件", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            addTemplateButton = CreateButton("新增模板");
            renameTemplateButton = CreateButton("重命名");
            deleteTemplateButton = CreateButton("删除模板");
            loadTemplateButton = CreateButton("加载模板");
            saveTemplateButton = CreateButton("保存模板");

            advancedMaskGroup = CreateWorkflowGroup("高级：图像屏蔽（可选）");
            advancedMaskGroup.Visible = false;
            TableLayoutPanel maskLayout = CreateWorkflowLayout(6);
            maskWorkflowLabel = CreateInfoLabel("默认直接使用 ROI 创建模板。这里仅用于屏蔽背景、污点、反光等干扰区域。");
            AddFullRow(maskLayout, maskWorkflowLabel, 44);
            FlowLayoutPanel maskButtons = CreateLinePanel();
            resetMaskButton = CreateButton("ROI转Mask");
            maskRectButton = CreateButton("添加矩形");
            maskCircleButton = CreateButton("添加圆形");
            maskBrushButton = CreateButton("画笔添加");
            maskEraserButton = CreateButton("橡皮擦");
            undoMaskButton = CreateButton("撤销");
            confirmTrainingMaskButton = CreateButton("应用屏蔽区域");
            maskButtons.Controls.AddRange(new Control[] { resetMaskButton, maskRectButton, maskCircleButton, maskBrushButton, maskEraserButton, undoMaskButton, confirmTrainingMaskButton });
            AddFullRow(maskLayout, maskButtons, 76);
            brushSizeInput = CreateNumber(2, 200, 20, 2, 0);
            AddRow(maskLayout, "画笔半径", brushSizeInput);
            showTrainingMaskCheckBox = new CheckBox { Text = "显示训练 Mask 边界", Checked = false, Dock = DockStyle.Left, Width = 180 };
            AddRow(maskLayout, "Mask显示", showTrainingMaskCheckBox);
            previewMaskButton = CreateButton("显示/隐藏Mask");
            AddRow(maskLayout, "预览", previewMaskButton);
            advancedMaskGroup.Controls.Add(maskLayout);

            GroupBox runGroup = CreateWorkflowGroup("3 模板匹配");
            TableLayoutPanel runLayout = CreateWorkflowLayout(10);
            templateInfoLabel = CreateInfoLabel("确认 ROI 后，点击“模板匹配设置”，在弹窗里框选模板区域并保存训练。");
            AddFullRow(runLayout, templateInfoLabel, 44);

            templateRectToolButton = CreateButton("模板矩形ROI");
            templateCircleToolButton = CreateButton("模板圆形ROI");
            confirmTemplateRoiButton = CreateButton("确认模板ROI");
            discardPendingTemplateRoiButton = CreateButton("放弃模板框");

            minScoreInput = CreateNumber(0, 1, 0.6M, 0.05M, 2);
            maxMatchesInput = CreateNumber(1, 50, 1, 1, 0);
            angleStartInput = CreateNumber(-360, 360, -180, 5, 0);
            angleExtentInput = CreateNumber(1, 720, 360, 5, 0);
            limitToSearchRoiCheckBox = new CheckBox { Text = "限定在 ROI 内搜索", Checked = false, Dock = DockStyle.Left, Width = 170 };
            showSearchRoiCheckBox = new CheckBox { Text = "显示搜索ROI", Checked = false, Dock = DockStyle.Left, Width = 130 };

            toggleAdvancedParamsButton = CreateButton("高级参数");
            toggleMaskToolsButton = CreateButton("图像屏蔽");
            resetParamsButton = CreateButton("恢复默认参数");

            advancedParameterPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Visible = false };
            TableLayoutPanel advancedLayout = CreateWorkflowLayout(5);
            maxOverlapInput = CreateNumber(0, 1, 0.5M, 0.05M, 2);
            greedinessInput = CreateNumber(0, 1, 0.9M, 0.05M, 2);
            numLevelsCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 120 };
            numLevelsCombo.Items.AddRange(new object[] { "auto", "1", "2", "3", "4", "5", "6" });
            numLevelsCombo.SelectedIndex = 0;
            metricCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 140 };
            metricCombo.Items.AddRange(new object[] { "use_polarity", "ignore_global_polarity", "ignore_local_polarity", "ignore_color_polarity" });
            metricCombo.SelectedIndex = 0;
            subPixelCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 140 };
            subPixelCombo.Items.AddRange(new object[] { "least_squares", "interpolation", "none" });
            subPixelCombo.SelectedIndex = 0;
            AddRow(advancedLayout, "最大重叠", maxOverlapInput);
            AddRow(advancedLayout, "贪婪度", greedinessInput);
            AddRow(advancedLayout, "金字塔层", numLevelsCombo);
            AddRow(advancedLayout, "极性", metricCombo);
            AddRow(advancedLayout, "亚像素", subPixelCombo);
            advancedParameterPanel.Controls.Add(advancedLayout);

            useRoiAsFrameButton = CreateButton("ROI作显示框");
            showDisplayFrameCheckBox = new CheckBox { Text = "显示外接矩形框", Checked = false, AutoSize = true, Margin = new Padding(3, 8, 8, 3) };
            moveFrameLeftButton = CreateSmallButton("左移");
            moveFrameRightButton = CreateSmallButton("右移");
            moveFrameUpButton = CreateSmallButton("上移");
            moveFrameDownButton = CreateSmallButton("下移");
            growFrameButton = CreateSmallButton("放大");
            shrinkFrameButton = CreateSmallButton("缩小");

            FlowLayoutPanel templateButtons = CreateLinePanel();
            trainTemplateButton = CreatePrimaryButton("模板匹配设置");
            runTemplateButton = CreatePrimaryButton("执行匹配");
            clearOverlayButton = CreateButton("清除叠加");
            templateButtons.Controls.AddRange(new Control[] { trainTemplateButton, runTemplateButton, clearOverlayButton });
            AddFullRow(runLayout, templateButtons, 76);

            FlowLayoutPanel displayOptions = CreateLinePanel();
            displayOptions.Controls.Add(showDisplayFrameCheckBox);
            displayOptions.Controls.Add(showSearchRoiCheckBox);
            AddRow(runLayout, "显示选项", displayOptions);

            FlowLayoutPanel packageButtons = CreateLinePanel();
            packageButtons.Controls.AddRange(new Control[] { saveTemplateButton, loadTemplateButton });
            AddRow(runLayout, "模板包", packageButtons);

            FlowLayoutPanel paramButtons = CreateLinePanel();
            paramButtons.Controls.AddRange(new Control[] { toggleAdvancedParamsButton, resetParamsButton });
            AddRow(runLayout, "参数", paramButtons);

            advancedParamsRow = NextRow(runLayout);
            runLayout.RowStyles[advancedParamsRow] = new RowStyle(SizeType.Absolute, 1);
            advancedParameterPanel.Margin = new Padding(3, 4, 3, 4);
            runLayout.Controls.Add(advancedParameterPanel, 0, advancedParamsRow);
            runLayout.SetColumnSpan(advancedParameterPanel, 2);

            runGroup.Controls.Add(runLayout);
            AddWorkflowGroup(layout, runGroup);

            GroupBox hdevGroup = CreateWorkflowGroup("高级：扩展模块与 HDevelop 示例");
            TableLayoutPanel hdevLayout = CreateWorkflowLayout(4);
            hdevPathTextBox = new TextBox { Dock = DockStyle.Fill };
            browseHdevButton = CreateButton("浏览");
            TableLayoutPanel pathPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            pathPanel.Controls.Add(hdevPathTextBox, 0, 0);
            pathPanel.Controls.Add(browseHdevButton, 1, 0);
            procedureNameTextBox = new TextBox { Dock = DockStyle.Fill, Text = "RunInspection" };
            runHdevButton = CreateButton("执行 HDev");
            openHdevExampleButton = CreateButton("打开示例说明");
            AddRow(hdevLayout, "程序", pathPanel);
            AddRow(hdevLayout, "过程名", procedureNameTextBox);
            AddFullRow(hdevLayout, CreateInfoLabel("可选增强：批量测试、结果导出、图像预处理、HDevelop 脚本都放在这里，不影响 ROI→模板匹配主流程。"), 48);
            AddFullRow(hdevLayout, openHdevExampleButton, 38);
            AddFullRow(hdevLayout, runHdevButton, 38);
            hdevGroup.Controls.Add(hdevLayout);
            AddWorkflowGroup(layout, hdevGroup);

            scrollPanel.Controls.Add(layout);
            tab.Controls.Add(scrollPanel);
            return tab;
        }

        private TabPage BuildCommunicationTab()
        {
            TabPage tab = CreateTab("通讯");
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            GroupBox modeGroup = CreateFixedGroup("1 通讯模式");
            TableLayoutPanel modePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
            modePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            modePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tcpClientModeRadio = new RadioButton { Text = "TCP客户端", Checked = true, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Width = 160, Height = 46, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold) };
            tcpServerModeRadio = new RadioButton { Text = "TCP服务端", Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Width = 160, Height = 46, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold) };
            tcpClientModeRadio.Dock = DockStyle.Fill;
            tcpServerModeRadio.Dock = DockStyle.Fill;
            modePanel.Controls.Add(tcpClientModeRadio, 0, 0);
            modePanel.Controls.Add(tcpServerModeRadio, 1, 0);
            modeGroup.Controls.Add(modePanel);
            layout.Controls.Add(modeGroup, 0, 0);

            GroupBox configGroup = CreateFixedGroup("2 连接/监听");
            TableLayoutPanel configLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, Padding = new Padding(8) };
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            tcpIpTextBox = new TextBox { Dock = DockStyle.Fill, Text = "127.0.0.1" };
            tcpPortInput = CreateNumber(1, 65535, 9000, 1, 0);
            tcpPortInput.Dock = DockStyle.Fill;
            tcpEncodingCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 130 };
            tcpEncodingCombo.Items.AddRange(new object[] { "UTF-8", "ASCII", "GBK" });
            tcpEncodingCombo.SelectedIndex = 0;
            tcpAppendNewLineCheckBox = new CheckBox { Text = "发送时追加换行", Checked = true, AutoSize = true, Dock = DockStyle.Left };
            tcpIpLabel = new Label { Text = "远端IP", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            configLayout.Controls.Add(tcpIpLabel, 0, 0);
            configLayout.Controls.Add(tcpIpTextBox, 1, 0);
            configLayout.Controls.Add(new Label { Text = "端口", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            configLayout.Controls.Add(tcpPortInput, 3, 0);

            tcpClientGroup = CreateCompactGroup();
            FlowLayoutPanel clientPanel = CreateFixedLinePanel();
            tcpConnectButton = CreatePrimaryButton("连接");
            tcpDisconnectButton = CreatePrimaryButton("断开");
            clientPanel.Controls.AddRange(new Control[] { tcpConnectButton, tcpDisconnectButton });
            tcpClientGroup.Controls.Add(clientPanel);
            configLayout.Controls.Add(tcpClientGroup, 0, 1);
            configLayout.SetColumnSpan(tcpClientGroup, 4);

            tcpServerGroup = CreateCompactGroup();
            FlowLayoutPanel serverPanel = CreateFixedLinePanel();
            tcpStartServerButton = CreatePrimaryButton("启动监听");
            tcpStopServerButton = CreatePrimaryButton("停止监听");
            serverPanel.Controls.AddRange(new Control[] { tcpStartServerButton, tcpStopServerButton });
            tcpServerGroup.Controls.Add(serverPanel);
            configLayout.Controls.Add(tcpServerGroup, 0, 2);
            configLayout.SetColumnSpan(tcpServerGroup, 4);
            configGroup.Controls.Add(configLayout);
            layout.Controls.Add(configGroup, 0, 1);

            GroupBox sendGroup = CreateFixedGroup("3 发送字符");
            TableLayoutPanel sendLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8) };
            sendLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            sendLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            sendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sendLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            tcpStatusLabel = CreateInfoLabel("TCP 未连接，发送不可用。");
            sendLayout.Controls.Add(tcpStatusLabel, 0, 0);
            FlowLayoutPanel sendOptions = CreateFixedLinePanel();
            sendOptions.Controls.Add(new Label { Text = "编码", Width = 42, Height = 24, TextAlign = ContentAlignment.MiddleLeft });
            sendOptions.Controls.Add(tcpEncodingCombo);
            sendOptions.Controls.Add(tcpAppendNewLineCheckBox);
            sendLayout.Controls.Add(sendOptions, 0, 1);
            TableLayoutPanel sendLine = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            sendLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sendLine.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
            tcpSendTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Text = "Hello" };
            tcpSendButton = CreatePrimaryButton("发送手动文本");
            tcpSendButton.Dock = DockStyle.Fill;
            sendLine.Controls.Add(tcpSendTextBox, 0, 0);
            sendLine.Controls.Add(tcpSendButton, 1, 0);
            sendLayout.Controls.Add(sendLine, 0, 2);

            FlowLayoutPanel resultSendPanel = CreateFixedLinePanel();
            autoSendMatchResultCheckBox = new CheckBox { Text = "自动发送匹配结果", Checked = true, AutoSize = true, Margin = new Padding(3, 8, 10, 3) };
            sendLastMatchButton = CreatePrimaryButton("发送最新匹配结果");
            sendLastMatchButton.Width = 166;
            sendLastMatchButton.MinimumSize = new Size(150, 38);
            resultSendPanel.Controls.Add(autoSendMatchResultCheckBox);
            resultSendPanel.Controls.Add(sendLastMatchButton);
            sendLayout.Controls.Add(resultSendPanel, 0, 3);
            sendGroup.Controls.Add(sendLayout);
            layout.Controls.Add(sendGroup, 0, 2);

            GroupBox historyGroup = CreateFixedGroup("4 收发记录");
            tcpHistoryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };
            historyGroup.Controls.Add(tcpHistoryTextBox);
            layout.Controls.Add(historyGroup, 0, 3);

            tab.Controls.Add(layout);
            return tab;
        }

        private TabPage BuildResultsTab()
        {
            TabPage tab = CreateTab("结果查询");
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            FlowLayoutPanel filters = CreateLinePanel();
            startTimeCheckBox = new CheckBox { Text = "开始", Width = 54 };
            startTimePicker = new DateTimePicker { Width = 146, Format = DateTimePickerFormat.Custom, CustomFormat = "MM-dd HH:mm:ss" };
            endTimeCheckBox = new CheckBox { Text = "结束", Width = 54 };
            endTimePicker = new DateTimePicker { Width = 146, Format = DateTimePickerFormat.Custom, CustomFormat = "MM-dd HH:mm:ss" };
            resultFilterTextBox = new TextBox { Width = 90 };
            sourceFilterTextBox = new TextBox { Width = 110 };
            queryButton = CreateButton("查询");
            filters.Controls.AddRange(new Control[]
            {
                startTimeCheckBox, startTimePicker, endTimeCheckBox, endTimePicker,
                new Label { Text = "结果", Width = 38, TextAlign = ContentAlignment.MiddleLeft },
                resultFilterTextBox,
                new Label { Text = "来源", Width = 38, TextAlign = ContentAlignment.MiddleLeft },
                sourceFilterTextBox,
                queryButton
            });

            resultGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                DataSource = gridRows
            };
            AddGridColumn("Id", "ID", 42);
            AddGridColumn("Timestamp", "时间", 132);
            AddGridColumn("InspectionType", "类型", 78);
            AddGridColumn("ResultCode", "结果", 62);
            AddGridColumn("Score", "分数", 62);
            AddGridColumn("Match", "位置", 120);
            AddGridColumn("Message", "消息", 160);

            FlowLayoutPanel resultButtons = CreateLinePanel();
            replayResultButton = CreateButton("回看结果");
            exportCsvButton = CreateButton("导出CSV");
            exportXlsxButton = CreateButton("导出XLSX");
            clearResultsButton = CreateButton("清空结果");
            resultButtons.Controls.AddRange(new Control[] { replayResultButton, exportCsvButton, exportXlsxButton, clearResultsButton });

            Label hint = CreateInfoLabel("当前查询结果用于导出；双击表格行可回看图像和叠加。");
            layout.Controls.Add(filters, 0, 0);
            layout.Controls.Add(resultGrid, 0, 1);
            layout.Controls.Add(resultButtons, 0, 2);
            layout.Controls.Add(hint, 0, 3);
            tab.Controls.Add(layout);
            return tab;
        }

        private Control BuildLogPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10, 0, 10, 10) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel header = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            header.Controls.Add(new Label { Text = "日志", Width = 48, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold) });
            Button clearLogButton = CreateSmallButton("清空日志");
            Button openLogButton = CreateSmallButton("打开目录");
            clearLogButton.Click += delegate { logTextBox.Clear(); };
            openLogButton.Click += delegate { Process.Start("explorer.exe", logger.LogDirectory); };
            header.Controls.Add(clearLogButton);
            header.Controls.Add(openLogButton);

            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(28, 32, 40),
                ForeColor = Color.FromArgb(230, 235, 241),
                BorderStyle = BorderStyle.FixedSingle
            };

            panel.Controls.Add(header, 0, 0);
            panel.Controls.Add(logTextBox, 0, 1);
            return panel;
        }

        private void WireEvents()
        {
            cameraModeRadio.CheckedChanged += delegate { RefreshUiState(); };
            fileModeRadio.CheckedChanged += delegate { RefreshUiState(); };
            openCameraButton.Click += delegate { OpenCamera(); };
            grabButton.Click += delegate { GrabSingle(); };
            continuousButton.Click += delegate { StartContinuousGrab(); };
            stopGrabButton.Click += delegate { StopContinuousGrab(); };
            closeCameraButton.Click += delegate { CloseCamera(); };
            loadImagesButton.Click += delegate { LoadImageFiles(); };
            loadFolderButton.Click += delegate { LoadImageFolder(); };
            prevImageButton.Click += delegate { ShowPreviousImage(); };
            nextImageButton.Click += delegate { ShowNextImage(); };
            playButton.Click += delegate { StartPlayback(); };
            stopPlayButton.Click += delegate { StopPlayback(); };
            toGrayButton.Click += delegate { ConvertCurrentImageToGray(); };
            toColorButton.Click += delegate { ConvertCurrentImageToColor(); };
            restoreOriginalButton.Click += delegate { RestoreOriginalImage(); };

            selectToolButton.Click += delegate { SelectTool(VisionTool.Select); };
            panToolButton.Click += delegate { SelectTool(VisionTool.Pan); };
            rectToolButton.Click += delegate { SelectTool(VisionTool.RectangleRoi); };
            circleToolButton.Click += delegate { SelectTool(VisionTool.CircleRoi); };
            polygonToolButton.Click += delegate { SelectTool(VisionTool.PolygonRoi); };
            confirmRoiButton.Click += delegate { ConfirmSearchRoi(); };
            discardPendingRoiButton.Click += delegate { DiscardPendingSearchRoi(); };
            clearRoiButton.Click += delegate { ClearRoi(); };
            fitButton.Click += delegate { FitImage(); };
            clearOverlayButton.Click += delegate { ClearOverlay(); };
            saveViewButton.Click += delegate { SaveViewSnapshot(); };

            trainTemplateButton.Click += delegate { TrainTemplate(); };
            addTemplateButton.Click += delegate { AddTemplate(); };
            renameTemplateButton.Click += delegate { RenameTemplate(); };
            deleteTemplateButton.Click += delegate { DeleteTemplate(); };
            saveTemplateButton.Click += delegate { SaveTemplate(); };
            loadTemplateButton.Click += delegate { LoadTemplate(); };
            runTemplateButton.Click += delegate { RunTemplateMatch(); };
            templateRectToolButton.Click += delegate { SelectTool(VisionTool.TemplateRectangleRoi); };
            templateCircleToolButton.Click += delegate { SelectTool(VisionTool.TemplateCircleRoi); };
            confirmTemplateRoiButton.Click += delegate { ConfirmTemplateRoi(); };
            discardPendingTemplateRoiButton.Click += delegate { DiscardPendingTemplateRoi(); };
            maskRectButton.Click += delegate { SelectTool(VisionTool.MaskRectangleAdd); };
            maskCircleButton.Click += delegate { SelectTool(VisionTool.MaskCircleAdd); };
            maskBrushButton.Click += delegate { SelectTool(VisionTool.MaskBrushAdd); };
            maskEraserButton.Click += delegate { SelectTool(VisionTool.MaskEraser); };
            resetMaskButton.Click += delegate { ResetMaskFromSearchRoi(); };
            undoMaskButton.Click += delegate { UndoMaskEdit(); };
            confirmTrainingMaskButton.Click += delegate { ConfirmTrainingMask(); };
            previewMaskButton.Click += delegate { PreviewTrainingMask(); };
            useRoiAsFrameButton.Click += delegate { UseSearchRoiAsDisplayFrame(); };
            moveFrameLeftButton.Click += delegate { NudgeDisplayFrame(0, -1); };
            moveFrameRightButton.Click += delegate { NudgeDisplayFrame(0, 1); };
            moveFrameUpButton.Click += delegate { NudgeDisplayFrame(-1, 0); };
            moveFrameDownButton.Click += delegate { NudgeDisplayFrame(1, 0); };
            growFrameButton.Click += delegate { ScaleDisplayFrame(1.08); };
            shrinkFrameButton.Click += delegate { ScaleDisplayFrame(0.92); };
            showTrainingMaskCheckBox.CheckedChanged += delegate { RefreshDisplay(); };
            showDisplayFrameCheckBox.CheckedChanged += delegate { RefreshDisplay(); };
            showSearchRoiCheckBox.CheckedChanged += delegate { RefreshDisplay(); };
            toggleAdvancedParamsButton.Click += delegate { ToggleAdvancedParams(); };
            toggleMaskToolsButton.Click += delegate { ToggleMaskTools(); };
            resetParamsButton.Click += delegate { ResetTemplateParameters(); };
            browseHdevButton.Click += delegate { BrowseHdev(); };
            openHdevExampleButton.Click += delegate { OpenHdevExample(); };
            runHdevButton.Click += delegate { RunHdevInspection(); };

            tcpClientModeRadio.CheckedChanged += delegate { RefreshUiState(); };
            tcpServerModeRadio.CheckedChanged += delegate { RefreshUiState(); };
            tcpConnectButton.Click += delegate { ConnectTcpClient(); };
            tcpDisconnectButton.Click += delegate { StopTcpCommunication(); };
            tcpStartServerButton.Click += delegate { StartTcpServer(); };
            tcpStopServerButton.Click += delegate { StopTcpCommunication(); };
            tcpSendButton.Click += delegate { SendTcpText(); };
            sendLastMatchButton.Click += delegate { SendLastMatchResult(); };
            rootSplit.SplitterMoved += delegate { ClampMainLayout(); ScheduleRefreshDisplay(); };
            mainSplit.SplitterMoved += delegate { ClampMainLayout(); ScheduleRefreshDisplay(); };
            rightTabs.SelectedIndexChanged += delegate { ScheduleRefreshDisplay(); };
            imageWindow.Resize += delegate { ScheduleRefreshDisplay(); };
            Resize += delegate { ClampMainLayout(); ScheduleRefreshDisplay(); };

            queryButton.Click += delegate { RefreshResultGrid(); };
            exportCsvButton.Click += delegate { ExportCsv(); };
            exportXlsxButton.Click += delegate { ExportXlsx(); };
            clearResultsButton.Click += delegate { ClearResults(); };
            replayResultButton.Click += delegate { ReplaySelectedResult(); };
            resultGrid.CellDoubleClick += delegate { ReplaySelectedResult(); };
            resultStore.Changed += delegate { RefreshResultGrid(); };
            templateGrid.SelectionChanged += delegate { SelectTemplateFromGrid(); };

            imageWindow.MouseEnter += delegate { imageWindow.Focus(); };
            imageWindow.MouseWheel += ImageWindowMouseWheel;
            imageWindow.MouseDown += ImageWindowMouseDown;
            imageWindow.MouseMove += ImageWindowMouseMove;
            imageWindow.MouseUp += ImageWindowMouseUp;
            imageWindow.MouseDoubleClick += ImageWindowMouseDoubleClick;
            imageWindow.DragEnter += FileDragEnter;
            imageWindow.DragDrop += FileDragDrop;
            DragEnter += FileDragEnter;
            DragDrop += FileDragDrop;

            imageService.ImageGrabbed += ImageServiceImageGrabbed;
            tcpService.MessageReceived += TcpServiceMessageReceived;
            tcpService.StatusChanged += TcpServiceStatusChanged;
            tcpService.ErrorOccurred += TcpServiceErrorOccurred;
            logger.MessageLogged += LoggerMessageLogged;
            imagePlayTimer.Interval = 750;
            imagePlayTimer.Tick += delegate { if (playbackActive) { ShowNextImage(); } };
        }

        private void LoadImageFiles()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.gif|All files|*.*";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadImageQueue(dialog.FileNames);
                }
            }
        }

        private void LoadImageFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadImageQueue(Directory.GetFiles(dialog.SelectedPath));
                }
            }
        }

        private void LoadImageQueue(IEnumerable<string> paths)
        {
            RunAction("加载图片", delegate
            {
                StopPlayback();
                List<string> files = ExpandImagePaths(paths).ToList();
                if (files.Count == 0)
                {
                    throw new InvalidOperationException("没有找到可读取的图片文件。");
                }

                imageQueue.Clear();
                imageQueue.AddRange(files);
                currentImageIndex = 0;
                ShowImageAtCurrentIndex();
                logger.Info("本地图队列已加载：" + files.Count + " 张。");
            });
        }

        private IEnumerable<string> ExpandImagePaths(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path).Where(IsImageFile).OrderBy(item => item))
                    {
                        yield return file;
                    }
                }
                else if (File.Exists(path) && IsImageFile(path))
                {
                    yield return path;
                }
            }
        }

        private static bool IsImageFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".bmp" || extension == ".jpg" || extension == ".jpeg" || extension == ".png" ||
                   extension == ".tif" || extension == ".tiff" || extension == ".gif";
        }

        private void ShowPreviousImage()
        {
            if (imageQueue.Count == 0)
            {
                return;
            }

            currentImageIndex = (currentImageIndex - 1 + imageQueue.Count) % imageQueue.Count;
            ShowImageAtCurrentIndex();
        }

        private void ShowNextImage()
        {
            if (imageQueue.Count == 0)
            {
                return;
            }

            currentImageIndex = (currentImageIndex + 1) % imageQueue.Count;
            ShowImageAtCurrentIndex();
        }

        private void ShowImageAtCurrentIndex()
        {
            string path = imageQueue[currentImageIndex];
            HImage image = imageService.ReadImage(path);
            SetCurrentImage(image, path);
            currentMatches.Clear();
            if (imageNavLabel != null)
            {
                imageNavLabel.Text = string.Format("{0}/{1}", currentImageIndex + 1, imageQueue.Count);
            }
            RefreshDisplay();
            RefreshUiState();
        }

        private void StartPlayback()
        {
            if (imageQueue.Count <= 1)
            {
                ShowMessage("请先加载至少 2 张本地图。");
                return;
            }

            SetPlayback(true);
            logger.Info("自动播放开始。");
        }

        private void StopPlayback()
        {
            SetPlayback(false);
        }

        private void SetPlayback(bool active)
        {
            playbackActive = active;
            if (active)
            {
                imagePlayTimer.Start();
            }
            else
            {
                imagePlayTimer.Stop();
            }

            RefreshUiState();
        }

        private void OpenCamera()
        {
            RunAction("打开相机", delegate
            {
                StopPlayback();
                imageService.OpenCamera(Convert.ToString(cameraInterfaceCombo.SelectedItem), cameraDeviceTextBox.Text);
                logger.Info("相机已打开。");
                RefreshUiState();
            });
        }

        private void GrabSingle()
        {
            RunAction("单次取图", delegate
            {
                StopPlayback();
                SetCurrentImage(imageService.GrabSingle(), "Camera");
                currentMatches.Clear();
                logger.Info("单次取图完成。");
                RefreshDisplay();
            });
        }

        private void StartContinuousGrab()
        {
            RunAction("连续采集", delegate
            {
                StopPlayback();
                imageService.StartContinuousGrab(delegate { return !IsDisposed; });
                logger.Info("连续采集开始。");
                RefreshUiState();
            });
        }

        private void StopContinuousGrab()
        {
            imageService.StopContinuousGrab();
            logger.Info("连续采集停止。");
            RefreshUiState();
        }

        private void CloseCamera()
        {
            RunAction("关闭相机", delegate
            {
                imageService.CloseCamera();
                logger.Info("相机已关闭。");
                RefreshUiState();
            });
        }

        private void ImageServiceImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            if (IsDisposed)
            {
                if (e.Image != null)
                {
                    e.Image.Dispose();
                }
                return;
            }

            BeginInvoke(new Action(delegate
            {
                if (e.Error != null)
                {
                    logger.Error("连续采集失败", e.Error);
                    ShowMessage(e.Error.Message);
                    return;
                }

                SetCurrentImage(e.Image, e.Source);
                currentMatches.Clear();
                RefreshDisplay();
            }));
        }

        private void SelectTool(VisionTool tool)
        {
            roiEditor.Tool = tool;
            selectToolButton.BackColor = tool == VisionTool.Select ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            panToolButton.BackColor = tool == VisionTool.Pan ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            rectToolButton.BackColor = tool == VisionTool.RectangleRoi ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            circleToolButton.BackColor = tool == VisionTool.CircleRoi ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            if (polygonToolButton != null)
            {
                polygonToolButton.BackColor = tool == VisionTool.PolygonRoi ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            }
            if (maskRectButton != null)
            {
                maskRectButton.BackColor = tool == VisionTool.MaskRectangleAdd ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
                maskCircleButton.BackColor = tool == VisionTool.MaskCircleAdd ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
                maskBrushButton.BackColor = tool == VisionTool.MaskBrushAdd ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
                maskEraserButton.BackColor = tool == VisionTool.MaskEraser ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            }
            if (templateRectToolButton != null)
            {
                templateRectToolButton.BackColor = tool == VisionTool.TemplateRectangleRoi ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
                templateCircleToolButton.BackColor = tool == VisionTool.TemplateCircleRoi ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
            }
            modeStatus.Text = "工具: " + ToolText(tool);
        }

        private static string ToolText(VisionTool tool)
        {
            switch (tool)
            {
                case VisionTool.Pan:
                    return "平移";
                case VisionTool.RectangleRoi:
                    return "矩形ROI";
                case VisionTool.CircleRoi:
                    return "圆形ROI";
                case VisionTool.PolygonRoi:
                    return "多边形ROI";
                case VisionTool.TemplateRectangleRoi:
                    return "模板矩形ROI";
                case VisionTool.TemplateCircleRoi:
                    return "模板圆形ROI";
                case VisionTool.MaskRectangleAdd:
                    return "Mask矩形";
                case VisionTool.MaskCircleAdd:
                    return "Mask圆形";
                case VisionTool.MaskBrushAdd:
                    return "画笔+";
                case VisionTool.MaskEraser:
                    return "橡皮擦";
                default:
                    return "选择";
            }
        }

        private void ImageWindowMouseDown(object sender, MouseEventArgs e)
        {
            if (!viewport.HasImage)
            {
                return;
            }

            imageWindow.Focus();
            if (e.Button == MouseButtons.Right || roiEditor.Tool == VisionTool.Pan)
            {
                panning = true;
                lastPanPoint = e.Location;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                PointF imagePoint = ClampImagePoint(viewport.WindowToImage(e.Location, imageWindow));
                if (roiEditor.Tool == VisionTool.PolygonRoi)
                {
                    roiEditor.AddPolygonPoint(imagePoint);
                    RefreshDisplay();
                    RefreshUiState();
                    return;
                }

                if (RoiEditor.IsBrushTool(roiEditor.Tool))
                {
                    PushMaskUndo();
                    brushEditing = true;
                    ApplyBrushEdit(imagePoint);
                    return;
                }

                if (BeginDisplayFrameEdit(e.Location, imagePoint))
                {
                    return;
                }

                roiEditor.Begin(imagePoint);
            }
        }

        private void ImageWindowMouseMove(object sender, MouseEventArgs e)
        {
            PointF imagePoint = ClampImagePoint(viewport.WindowToImage(e.Location, imageWindow));
            mouseStatus.Text = viewport.HasImage ? string.Format("坐标: R{0:F1}, C{1:F1}", imagePoint.Y, imagePoint.X) : "坐标: --";

            if (panning)
            {
                viewport.PanByWindowDelta(e.X - lastPanPoint.X, e.Y - lastPanPoint.Y, imageWindow);
                lastPanPoint = e.Location;
                RefreshDisplay();
                return;
            }

            if (frameEditMode != FrameEditMode.None)
            {
                UpdateDisplayFrameEdit(imagePoint);
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
                RefreshUiState();
                return;
            }

            if (frameEditMode != FrameEditMode.None)
            {
                EndDisplayFrameEdit();
                RefreshUiState();
                return;
            }

            if (panning)
            {
                panning = false;
                return;
            }

            if (roiEditor.IsDrawing)
            {
                RoiData roi = roiEditor.Complete(ClampImagePoint(viewport.WindowToImage(e.Location, imageWindow)));
                if (RoiEditor.IsMaskShapeTool(roiEditor.Tool))
                {
                    AddMaskRegion(roi, false);
                }
                else if (RoiEditor.IsTemplateRoiTool(roiEditor.Tool))
                {
                    SetPendingTemplateRoi(roi);
                    logger.Info("模板 ROI 待确认：" + pendingTemplateRoi.DisplayText);
                }
                else
                {
                    SetPendingSearchRoi(roi);
                    logger.Info("ROI 待确认：" + pendingSearchRoi.DisplayText);
                }
                RefreshDisplay();
                RefreshUiState();
            }
        }

        private void ImageWindowMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (roiEditor.IsPolygonDrawing)
            {
                RoiData roi = roiEditor.CompletePolygon();
                if (roi != null)
                {
                    SetPendingSearchRoi(roi);
                    logger.Info("多边形 ROI 待确认：" + pendingSearchRoi.DisplayText);
                }

                RefreshDisplay();
                RefreshUiState();
                return;
            }

            FitImage();
        }

        private void ImageWindowMouseWheel(object sender, MouseEventArgs e)
        {
            viewport.ZoomAt(e.Location, imageWindow, e.Delta);
            RefreshDisplay();
            RefreshUiState();
        }

        private void FitImage()
        {
            viewport.Fit();
            RefreshDisplay();
            ScheduleRefreshDisplay();
            RefreshUiState();
        }

        private void ClearRoi()
        {
            DisposeCurrentRoi();
            DisposePendingSearchRoi();
            currentMatches.Clear();
            roiEditor.Cancel();
            logger.Info("ROI 已清除。");
            RefreshDisplay();
            RefreshUiState();
        }

        private void ClearOverlay()
        {
            currentMatches.Clear();
            RefreshDisplay();
            logger.Info("叠加结果已清除。");
        }

        private void AddTemplate()
        {
            TemplateItem item = new TemplateItem
            {
                Name = string.Format("模板_{0:000}", nextTemplateIndex++)
            };
            item.Options = ReadTemplateOptions();
            templateItems.Add(item);
            SelectTemplateItem(item);
            logger.Info("已新增模板：" + item.Name);
            RefreshUiState();
        }

        private void RenameTemplate()
        {
            if (currentTemplateItem == null)
            {
                return;
            }

            string name = PromptText("重命名模板", "模板名称", currentTemplateItem.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            currentTemplateItem.Name = name.Trim();
            RefreshTemplateGrid();
            RefreshUiState();
        }

        private void DeleteTemplate()
        {
            if (currentTemplateItem == null)
            {
                return;
            }

            if (MessageBox.Show(this, "确认删除模板 “" + currentTemplateItem.Name + "”？", "删除模板", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            int index = templateItems.IndexOf(currentTemplateItem);
            TemplateItem deleting = currentTemplateItem;
            templateItems.Remove(deleting);
            deleting.Dispose();
            currentTemplateItem = null;
            currentMatches.Clear();

            if (templateItems.Count > 0)
            {
                SelectTemplateItem(templateItems[Math.Min(index, templateItems.Count - 1)]);
            }
            else
            {
                DisposePendingTemplateRoi();
                DisposeTrainingMask();
                DisposeDisplayFrame();
                DisposeMaskUndoStack();
                trainingMaskConfirmed = false;
            }

            RefreshDisplay();
            RefreshUiState();
        }

        private void SelectTemplateFromGrid()
        {
            if (templateGrid == null || templateGrid.CurrentRow == null)
            {
                return;
            }

            TemplateItem item = templateGrid.CurrentRow.DataBoundItem as TemplateItem;
            if (item != null && !object.ReferenceEquals(item, currentTemplateItem))
            {
                SelectTemplateItem(item);
                RefreshDisplay();
            }
        }

        private void SelectTemplateItem(TemplateItem item)
        {
            currentTemplateItem = item;
            DisposePendingTemplateRoi();
            DisposeTrainingMask();
            DisposeDisplayFrame();
            DisposeMaskUndoStack();
            trainingMaskConfirmed = false;
            currentMatches.Clear();

            if (item != null)
            {
                SetTrainingMask(TemplateDefinition.CloneRegion(item.TrainingMask));
                trainingMaskConfirmed = item.TrainingMaskApplied && trainingMask != null;
                SetDisplayFrame(item.DisplayFrame == null ? null : item.DisplayFrame.Clone());
                ApplyTemplateOptions(item.Options);
            }

            if (templateGrid != null && item != null)
            {
                int index = templateItems.IndexOf(item);
                if (index >= 0 && index < templateGrid.Rows.Count)
                {
                    templateGrid.ClearSelection();
                    templateGrid.Rows[index].Selected = true;
                    templateGrid.CurrentCell = templateGrid.Rows[index].Cells[0];
                }
            }

            RefreshUiState();
        }

        private void ConfirmTemplateRoi()
        {
            if (currentTemplateItem == null || pendingTemplateRoi == null)
            {
                return;
            }

            ReplaceTemplateRoi(currentTemplateItem, pendingTemplateRoi.Clone());
            DisposePendingTemplateRoi();
            currentMatches.Clear();
            RefreshTemplateGrid();
            RefreshDisplay();
            RefreshUiState();
        }

        private void DiscardPendingTemplateRoi()
        {
            DisposePendingTemplateRoi();
            roiEditor.Cancel();
            RefreshDisplay();
            RefreshUiState();
        }

        private void ReplaceTemplateRoi(TemplateItem item, RoiData roi)
        {
            if (item.Service != null && item.Service.HasModel)
            {
                item.Service.Clear();
            }

            if (item.TemplateRoi != null)
            {
                item.TemplateRoi.Dispose();
            }

            item.TemplateRoi = roi;
            if (item.DisplayFrame != null)
            {
                item.DisplayFrame.Dispose();
            }

            item.DisplayFrame = roi == null ? null : roi.Clone();
            if (item.TrainingMask != null)
            {
                item.TrainingMask.Dispose();
            }

            item.TrainingMask = roi == null ? null : TemplateDefinition.CloneRegion(roi.Region);
            item.TrainingMaskApplied = false;

            if (object.ReferenceEquals(item, currentTemplateItem))
            {
                SetDisplayFrame(item.DisplayFrame == null ? null : item.DisplayFrame.Clone());
                SetTrainingMask(TemplateDefinition.CloneRegion(item.TrainingMask));
                trainingMaskConfirmed = false;
            }
        }

        private void ConfirmSearchRoi()
        {
            if (pendingSearchRoi == null && roiEditor.IsPolygonDrawing)
            {
                SetPendingSearchRoi(roiEditor.CompletePolygon());
            }

            if (pendingSearchRoi == null)
            {
                return;
            }

            SetCurrentRoi(pendingSearchRoi.Clone());
            DisposePendingSearchRoi();
            currentMatches.Clear();
            logger.Info("ROI 已确认：" + currentRoi.DisplayText);
            RefreshDisplay();
            RefreshUiState();
        }

        private void DiscardPendingSearchRoi()
        {
            DisposePendingSearchRoi();
            roiEditor.Cancel();
            logger.Info("待确认 ROI 已放弃。");
            RefreshDisplay();
            RefreshUiState();
        }

        private void ResetMaskFromSearchRoi()
        {
            RunAction("ROI转Mask", delegate
            {
                if (currentTemplateItem == null || currentTemplateItem.TemplateRoi == null)
                {
                    throw new InvalidOperationException("请先框选模板 ROI。");
                }

                PushMaskUndo();
                SetTrainingMask(TemplateDefinition.CloneRegion(currentTemplateItem.TemplateRoi.Region));
                trainingMaskConfirmed = false;
                if (displayFrame == null)
                {
                    SetDisplayFrame(currentTemplateItem.TemplateRoi.Clone());
                }

                logger.Info("训练 Mask 已由模板 ROI 生成。");
                RefreshDisplay();
                RefreshUiState();
            });
        }

        private void UndoMaskEdit()
        {
            if (maskUndoStack.Count == 0)
            {
                return;
            }

            HRegion previous = maskUndoStack[maskUndoStack.Count - 1];
            maskUndoStack.RemoveAt(maskUndoStack.Count - 1);
            SetTrainingMask(previous);
            trainingMaskConfirmed = false;
            RefreshDisplay();
            RefreshUiState();
        }

        private void ConfirmTrainingMask()
        {
            if (currentTemplateItem == null)
            {
                return;
            }

            if (trainingMask == null && currentTemplateItem.TemplateRoi != null)
            {
                SetTrainingMask(TemplateDefinition.CloneRegion(currentTemplateItem.TemplateRoi.Region));
            }

            if (trainingMask == null)
            {
                return;
            }

            trainingMaskConfirmed = true;
            currentTemplateItem.TrainingMaskApplied = true;
            if (currentTemplateItem.TrainingMask != null)
            {
                currentTemplateItem.TrainingMask.Dispose();
            }

            currentTemplateItem.TrainingMask = TemplateDefinition.CloneRegion(trainingMask);
            if (displayFrame == null && currentTemplateItem.TemplateRoi != null)
            {
                SetDisplayFrame(currentTemplateItem.TemplateRoi.Clone());
            }

            logger.Info("训练区域修正已应用。");
            RefreshDisplay();
            RefreshUiState();
        }

        private void ToggleMaskTools()
        {
            if (advancedMaskGroup == null)
            {
                return;
            }

            advancedMaskGroup.Visible = !advancedMaskGroup.Visible;
            toggleMaskToolsButton.Text = advancedMaskGroup.Visible ? "收起屏蔽" : "图像屏蔽";
            RefreshUiState();
        }

        private void ToggleAdvancedParams()
        {
            if (advancedParameterPanel == null)
            {
                return;
            }

            advancedParameterPanel.Visible = !advancedParameterPanel.Visible;
            TableLayoutPanel parent = advancedParameterPanel.Parent as TableLayoutPanel;
            if (parent != null && advancedParamsRow >= 0 && advancedParamsRow < parent.RowStyles.Count)
            {
                parent.RowStyles[advancedParamsRow] = new RowStyle(SizeType.Absolute, advancedParameterPanel.Visible ? 160 : 1);
            }

            toggleAdvancedParamsButton.Text = advancedParameterPanel.Visible ? "收起高级" : "高级参数";
        }

        private void ResetTemplateParameters()
        {
            ApplyTemplateOptions(new TemplateMatchOptions());
            logger.Info("模板匹配参数已恢复默认。");
            RefreshUiState();
        }

        private void PreviewTrainingMask()
        {
            if (showTrainingMaskCheckBox != null)
            {
                showTrainingMaskCheckBox.Checked = !showTrainingMaskCheckBox.Checked;
            }

            RefreshDisplay();
        }

        private void UseSearchRoiAsDisplayFrame()
        {
            RunAction("模板ROI作显示框", delegate
            {
                if (currentTemplateItem == null || currentTemplateItem.TemplateRoi == null)
                {
                    throw new InvalidOperationException("请先框选模板 ROI。");
                }

                if (currentTemplateItem.DisplayFrame != null)
                {
                    currentTemplateItem.DisplayFrame.Dispose();
                }

                currentTemplateItem.DisplayFrame = currentTemplateItem.TemplateRoi.Clone();
                SetDisplayFrame(currentTemplateItem.DisplayFrame.Clone());
                RefreshDisplay();
                RefreshUiState();
            });
        }

        private void NudgeDisplayFrame(double rowDirection, double columnDirection)
        {
            if (displayFrame == null)
            {
                return;
            }

            double step = Math.Max(1, Math.Min(viewport.ImageWidth, viewport.ImageHeight) * 0.005);
            RoiData updated = OffsetRoi(displayFrame, rowDirection * step, columnDirection * step);
            SetCurrentTemplateDisplayFrame(updated);
            RefreshDisplay();
        }

        private void ScaleDisplayFrame(double factor)
        {
            if (displayFrame == null)
            {
                return;
            }

            SetCurrentTemplateDisplayFrame(ScaleRoi(displayFrame, factor));
            RefreshDisplay();
        }

        private void SetCurrentTemplateDisplayFrame(RoiData frame)
        {
            if (currentTemplateItem != null)
            {
                if (currentTemplateItem.DisplayFrame != null)
                {
                    currentTemplateItem.DisplayFrame.Dispose();
                }

                currentTemplateItem.DisplayFrame = frame == null ? null : frame.Clone();
            }

            SetDisplayFrame(frame);
        }

        private bool BeginDisplayFrameEdit(Point windowPoint, PointF imagePoint)
        {
            if (roiEditor.Tool != VisionTool.Select || displayFrame == null)
            {
                return false;
            }

            FrameEditMode mode = HitTestDisplayFrame(windowPoint, imagePoint);
            if (mode == FrameEditMode.None)
            {
                return false;
            }

            frameEditMode = mode;
            frameEditStart = imagePoint;
            frameEditOriginal = displayFrame.Clone();
            return true;
        }

        private void UpdateDisplayFrameEdit(PointF imagePoint)
        {
            if (frameEditOriginal == null)
            {
                return;
            }

            double rowDelta = imagePoint.Y - frameEditStart.Y;
            double columnDelta = imagePoint.X - frameEditStart.X;
            SetDisplayFrame(TransformFrame(frameEditOriginal, frameEditMode, rowDelta, columnDelta));
        }

        private void EndDisplayFrameEdit()
        {
            if (currentTemplateItem != null)
            {
                if (currentTemplateItem.DisplayFrame != null)
                {
                    currentTemplateItem.DisplayFrame.Dispose();
                }

                currentTemplateItem.DisplayFrame = displayFrame == null ? null : displayFrame.Clone();
            }

            frameEditMode = FrameEditMode.None;
            if (frameEditOriginal != null)
            {
                frameEditOriginal.Dispose();
                frameEditOriginal = null;
            }
        }

        private FrameEditMode HitTestDisplayFrame(Point windowPoint, PointF imagePoint)
        {
            double tolerance = ImageTolerance(windowPoint, 10);
            if (displayFrame.ShapeType == RoiShapeType.Circle)
            {
                double dr = imagePoint.Y - displayFrame.Row;
                double dc = imagePoint.X - displayFrame.Column;
                double distance = Math.Sqrt(dr * dr + dc * dc);
                if (Math.Abs(distance - displayFrame.Radius) <= tolerance)
                {
                    return FrameEditMode.Radius;
                }

                return distance < displayFrame.Radius ? FrameEditMode.Move : FrameEditMode.None;
            }

            bool nearLeft = Math.Abs(imagePoint.X - displayFrame.Column1) <= tolerance;
            bool nearRight = Math.Abs(imagePoint.X - displayFrame.Column2) <= tolerance;
            bool nearTop = Math.Abs(imagePoint.Y - displayFrame.Row1) <= tolerance;
            bool nearBottom = Math.Abs(imagePoint.Y - displayFrame.Row2) <= tolerance;
            bool insideColumns = imagePoint.X >= displayFrame.Column1 - tolerance && imagePoint.X <= displayFrame.Column2 + tolerance;
            bool insideRows = imagePoint.Y >= displayFrame.Row1 - tolerance && imagePoint.Y <= displayFrame.Row2 + tolerance;

            if (nearLeft && nearTop)
            {
                return FrameEditMode.TopLeft;
            }

            if (nearRight && nearTop)
            {
                return FrameEditMode.TopRight;
            }

            if (nearLeft && nearBottom)
            {
                return FrameEditMode.BottomLeft;
            }

            if (nearRight && nearBottom)
            {
                return FrameEditMode.BottomRight;
            }

            if (nearLeft && insideRows)
            {
                return FrameEditMode.Left;
            }

            if (nearRight && insideRows)
            {
                return FrameEditMode.Right;
            }

            if (nearTop && insideColumns)
            {
                return FrameEditMode.Top;
            }

            if (nearBottom && insideColumns)
            {
                return FrameEditMode.Bottom;
            }

            return insideColumns && insideRows ? FrameEditMode.Move : FrameEditMode.None;
        }

        private double ImageTolerance(Point windowPoint, int pixels)
        {
            PointF point1 = viewport.WindowToImage(windowPoint, imageWindow);
            PointF point2 = viewport.WindowToImage(new Point(windowPoint.X + pixels, windowPoint.Y + pixels), imageWindow);
            return Math.Max(2, Math.Max(Math.Abs(point2.X - point1.X), Math.Abs(point2.Y - point1.Y)));
        }

        private PointF ClampImagePoint(PointF point)
        {
            float x = Math.Max(0, Math.Min(Math.Max(0, viewport.ImageWidth - 1), point.X));
            float y = Math.Max(0, Math.Min(Math.Max(0, viewport.ImageHeight - 1), point.Y));
            return new PointF(x, y);
        }

        private void ApplyBrushEdit(PointF imagePoint)
        {
            imagePoint = ClampImagePoint(imagePoint);
            RoiData brush = RoiData.CreateCircle(imagePoint.Y, imagePoint.X, (double)brushSizeInput.Value);
            AddMaskRegion(brush, roiEditor.Tool == VisionTool.MaskEraser, false);
            RefreshDisplay();
        }

        private void AddMaskRegion(RoiData roi, bool subtract)
        {
            AddMaskRegion(roi, subtract, true);
        }

        private void AddMaskRegion(RoiData roi, bool subtract, bool pushUndo)
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

                if (subtract)
                {
                    if (trainingMask == null)
                    {
                        return;
                    }

                    HObject diff;
                    HOperatorSet.Difference(trainingMask, roi.Region, out diff);
                    SetTrainingMask(new HRegion(diff));
                }
                else
                {
                    if (trainingMask == null)
                    {
                        SetTrainingMask(TemplateDefinition.CloneRegion(roi.Region));
                    }
                    else
                    {
                        HObject union;
                        HOperatorSet.Union2(trainingMask, roi.Region, out union);
                        SetTrainingMask(new HRegion(union));
                    }
                }

                trainingMaskConfirmed = false;
            }
            finally
            {
                roi.Dispose();
            }
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

        private TemplateDefinition BuildTemplateDefinition()
        {
            if (currentTemplateItem == null)
            {
                throw new InvalidOperationException("请先新增或选择模板。");
            }

            if (currentTemplateItem.TemplateRoi == null)
            {
                throw new InvalidOperationException("请先框选模板 ROI。");
            }

            HRegion mask = trainingMask != null && trainingMaskConfirmed
                ? TemplateDefinition.CloneRegion(trainingMask)
                : TemplateDefinition.CloneRegion(currentTemplateItem.TemplateRoi.Region);
            RoiData frame = currentTemplateItem.DisplayFrame != null
                ? currentTemplateItem.DisplayFrame.Clone()
                : currentTemplateItem.TemplateRoi.Clone();

            if (mask == null)
            {
                throw new InvalidOperationException("请先框选模板 ROI。");
            }

            return new TemplateDefinition
            {
                TemplateName = currentTemplateItem.Name,
                TemplateRoi = currentTemplateItem.TemplateRoi.Clone(),
                TrainingMask = mask,
                DisplayFrame = frame,
                Options = ReadTemplateOptions()
            };
        }

        private TemplateMatchOptions ReadTemplateOptions()
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
                LimitToSearchRoi = limitToSearchRoiCheckBox.Checked
            };
        }

        private void ApplyLoadedTemplateDefinition(TemplateItem item)
        {
            if (item == null || item.Service == null)
            {
                return;
            }

            TemplateDefinition loaded = item.Service.Definition;
            if (loaded == null)
            {
                return;
            }

            try
            {
                item.Name = string.IsNullOrWhiteSpace(loaded.TemplateName) ? item.Name : loaded.TemplateName;
                ReplaceTemplateRoi(item, loaded.TemplateRoi == null ? (loaded.DisplayFrame == null ? null : loaded.DisplayFrame.Clone()) : loaded.TemplateRoi.Clone());
                if (item.TrainingMask != null)
                {
                    item.TrainingMask.Dispose();
                }

                item.TrainingMask = TemplateDefinition.CloneRegion(loaded.TrainingMask);
                item.TrainingMaskApplied = item.TrainingMask != null;
                if (item.DisplayFrame != null)
                {
                    item.DisplayFrame.Dispose();
                }

                item.DisplayFrame = loaded.DisplayFrame == null ? (item.TemplateRoi == null ? null : item.TemplateRoi.Clone()) : loaded.DisplayFrame.Clone();
                SetTrainingMask(TemplateDefinition.CloneRegion(item.TrainingMask));
                SetDisplayFrame(item.DisplayFrame == null ? null : item.DisplayFrame.Clone());
                ApplyTemplateOptions(loaded.Options);
                trainingMaskConfirmed = trainingMask != null;
            }
            finally
            {
                loaded.Dispose();
            }
        }

        private void ApplyTemplateOptions(TemplateMatchOptions options)
        {
            if (options == null)
            {
                return;
            }

            minScoreInput.Value = ClampDecimal((decimal)options.MinScore, minScoreInput.Minimum, minScoreInput.Maximum);
            maxMatchesInput.Value = ClampDecimal(options.MaxMatches, maxMatchesInput.Minimum, maxMatchesInput.Maximum);
            angleStartInput.Value = ClampDecimal((decimal)options.AngleStartDeg, angleStartInput.Minimum, angleStartInput.Maximum);
            angleExtentInput.Value = ClampDecimal((decimal)options.AngleExtentDeg, angleExtentInput.Minimum, angleExtentInput.Maximum);
            maxOverlapInput.Value = ClampDecimal((decimal)options.MaxOverlap, maxOverlapInput.Minimum, maxOverlapInput.Maximum);
            greedinessInput.Value = ClampDecimal((decimal)options.Greediness, greedinessInput.Minimum, greedinessInput.Maximum);
            SelectComboValue(numLevelsCombo, options.NumLevels);
            SelectComboValue(metricCombo, options.Metric);
            SelectComboValue(subPixelCombo, options.SubPixel);
            limitToSearchRoiCheckBox.Checked = options.LimitToSearchRoi;
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

        private void RefreshTemplateGrid()
        {
            if (templateGrid != null)
            {
                templateGrid.Refresh();
            }
        }

        private static string SafeFileName(string text)
        {
            string name = string.IsNullOrWhiteSpace(text) ? "shape_model" : text.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(name) ? "shape_model" : name;
        }

        private string PromptText(string title, string labelText, string defaultValue)
        {
            using (Form dialog = new Form())
            using (Label label = new Label())
            using (TextBox textBox = new TextBox())
            using (Button okButton = new Button())
            using (Button cancelButton = new Button())
            {
                dialog.Text = title;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(360, 118);

                label.Text = labelText;
                label.SetBounds(12, 12, 330, 22);
                textBox.Text = defaultValue ?? string.Empty;
                textBox.SetBounds(12, 38, 330, 24);
                okButton.Text = "确定";
                okButton.DialogResult = DialogResult.OK;
                okButton.SetBounds(174, 78, 78, 28);
                cancelButton.Text = "取消";
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.SetBounds(264, 78, 78, 28);

                dialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;
                return dialog.ShowDialog(this) == DialogResult.OK ? textBox.Text : null;
            }
        }

        private void TrainTemplate()
        {
            RunAction("模板匹配设置", delegate
            {
                if (currentImage == null)
                {
                    throw new InvalidOperationException("请先打开图片。");
                }

                if (currentRoi == null)
                {
                    throw new InvalidOperationException("请先框选并确认 ROI。");
                }

                if (currentTemplateItem == null)
                {
                    currentTemplateItem = new TemplateItem
                    {
                        Name = string.Format("模板_{0:000}", nextTemplateIndex++),
                        Options = ReadTemplateOptions()
                    };
                    templateItems.Add(currentTemplateItem);
                    RefreshTemplateGrid();
                }

                using (TemplateCreateForm dialog = new TemplateCreateForm(currentImage, currentRoi, currentTemplateItem.Name, ReadTemplateOptions()))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.ResultDefinition == null)
                    {
                        return;
                    }

                    TemplateDefinition definition = dialog.ResultDefinition;
                    try
                    {
                        currentTemplateItem.Options = TemplateDefinition.CloneOptions(definition.Options);
                        ReplaceTemplateRoi(currentTemplateItem, definition.TemplateRoi == null ? null : definition.TemplateRoi.Clone());
                        if (currentTemplateItem.TrainingMask != null)
                        {
                            currentTemplateItem.TrainingMask.Dispose();
                        }

                        currentTemplateItem.TrainingMask = TemplateDefinition.CloneRegion(definition.TrainingMask);
                        currentTemplateItem.TrainingMaskApplied = definition.TrainingMask != null;
                        SetTrainingMask(TemplateDefinition.CloneRegion(currentTemplateItem.TrainingMask));
                        trainingMaskConfirmed = currentTemplateItem.TrainingMaskApplied;
                        SetDisplayFrame(definition.DisplayFrame == null ? null : definition.DisplayFrame.Clone());
                        if (currentTemplateItem.DisplayFrame != null)
                        {
                            currentTemplateItem.DisplayFrame.Dispose();
                        }

                        currentTemplateItem.DisplayFrame = displayFrame == null ? null : displayFrame.Clone();
                        ApplyTemplateOptions(definition.Options);
                        currentTemplateItem.Service.Train(currentImage, definition);
                    }
                    finally
                    {
                        definition.Dispose();
                    }
                }

                currentMatches.Clear();
                templateInfoLabel.Text = "模板 “" + currentTemplateItem.Name + "” 已保存训练，可执行匹配。";
                logger.Info("模板匹配设置已保存并训练：" + currentTemplateItem.Name);
                RefreshTemplateGrid();
                RefreshDisplay();
                RefreshUiState();
            });
        }

        private void SaveTemplate()
        {
            RunAction("保存模板", delegate
            {
                if (currentTemplateItem == null)
                {
                    throw new InvalidOperationException("请先选择模板。");
                }

                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "HALCON shape model|*.shm";
                    dialog.FileName = SafeFileName(currentTemplateItem.Name) + ".shm";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        currentTemplateItem.Service.Save(dialog.FileName);
                        templateInfoLabel.Text = "模板包已保存：" + dialog.FileName;
                        logger.Info("模板包已保存：" + dialog.FileName);
                        RefreshTemplateGrid();
                    }
                }
            });
        }

        private void LoadTemplate()
        {
            RunAction("加载模板", delegate
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Filter = "HALCON shape model|*.shm|All files|*.*";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        TemplateItem item = new TemplateItem { Name = Path.GetFileNameWithoutExtension(dialog.FileName) };
                        item.Service.Load(dialog.FileName);
                        templateItems.Add(item);
                        SelectTemplateItem(item);
                        ApplyLoadedTemplateDefinition(item);
                        templateInfoLabel.Text = "模板包已加载：" + dialog.FileName;
                        logger.Info("模板包已加载：" + dialog.FileName);
                        RefreshTemplateGrid();
                        RefreshUiState();
                    }
                }
            });
        }

        private void RunTemplateMatch()
        {
            RunAction("执行匹配", delegate
            {
                if (currentImage == null)
                {
                    SetLastMatchMessage(BuildErrorMatchMessage("请先打开图片"));
                    throw new InvalidOperationException("请先打开图片。");
                }

                if (currentRoi == null)
                {
                    SetLastMatchMessage(BuildErrorMatchMessage("请先框选并确认ROI"));
                    throw new InvalidOperationException("请先框选并确认 ROI。");
                }

                if (currentTemplateItem == null)
                {
                    SetLastMatchMessage(BuildErrorMatchMessage("请先设置模板"));
                    throw new InvalidOperationException("请先点击“模板匹配设置”，确认模板区域并保存训练。");
                }

                if (!currentTemplateItem.HasModel)
                {
                    SetLastMatchMessage(BuildErrorMatchMessage("未训练模板"));
                    throw new InvalidOperationException("当前模板尚未训练，请进入“模板匹配设置”并点击“保存并训练”。");
                }

                List<ShapeMatchResult> matches = currentTemplateItem.Service.Match(
                    currentImage,
                    currentRoi,
                    ReadTemplateOptions());

                currentMatches = matches;
                if (matches.Count == 0)
                {
                    AddRecord("ShapeModel", "NG", 0, "未匹配到目标。", null);
                }
                else
                {
                    foreach (ShapeMatchResult match in matches)
                    {
                        AddRecord("ShapeModel", "OK", match.Score, "模板匹配成功：" + currentTemplateItem.Name, match);
                    }
                }

                SetLastMatchMessage(BuildMatchResultMessage(currentTemplateItem.Name, matches));
                AutoSendMatchResultIfNeeded();
                logger.Info("模板匹配完成：" + currentTemplateItem.Name + "，结果数：" + matches.Count);
                RefreshDisplay();
                RefreshUiState();
            });
        }

        private void BrowseHdev()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "HDevelop files|*.hdev;*.hdvp|All files|*.*";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    hdevPathTextBox.Text = dialog.FileName;
                }
            }
        }

        private void OpenHdevExample()
        {
            RunAction("打开 HDevelop 示例", delegate
            {
                string path = FindExampleFile("HDevelopTemplateMatchingExample.md");
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    throw new FileNotFoundException("未找到 HDevelop 示例说明文件。", path);
                }

                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            });
        }

        private static string FindExampleFile(string fileName)
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Examples", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Examples", fileName);
        }

        private void RunHdevInspection()
        {
            RunAction("HDevelop 检测", delegate
            {
                HDevInspectionResult result = inspectionService.RunInspection(hdevPathTextBox.Text, procedureNameTextBox.Text, currentImage, currentRoi);
                AddRecord("HDevelop", result.ResultCode, result.Score, result.Message, null);
                logger.Info("HDevelop 检测完成：" + result.ResultCode);
            });
        }

        private void ConnectTcpClient()
        {
            RunAction("TCP客户端连接", delegate
            {
                tcpService.ConnectClient(tcpIpTextBox.Text.Trim(), (int)tcpPortInput.Value, ResolveTcpEncoding());
                string message = "客户端连接中：" + tcpIpTextBox.Text.Trim() + ":" + tcpPortInput.Value;
                AppendTcpHistory("状态：" + message);
                SetTcpStatus(message);
                RefreshUiState();
            });
        }

        private void StartTcpServer()
        {
            RunAction("TCP服务端监听", delegate
            {
                tcpService.StartServer(tcpIpTextBox.Text.Trim(), (int)tcpPortInput.Value, ResolveTcpEncoding());
                string message = "服务端监听中：" + tcpIpTextBox.Text.Trim() + ":" + tcpPortInput.Value;
                AppendTcpHistory("状态：" + message);
                SetTcpStatus(message);
                RefreshUiState();
            });
        }

        private void StopTcpCommunication()
        {
            tcpService.Stop();
            AppendTcpHistory("状态：TCP 已停止。");
            SetTcpStatus("TCP 已停止。");
            RefreshUiState();
        }

        private void SendTcpText()
        {
            RunAction("TCP发送", delegate
            {
                SendTcpPayload(tcpSendTextBox.Text ?? string.Empty, "发送手动");
            });
        }

        private void SendLastMatchResult()
        {
            RunAction("发送最新匹配结果", delegate
            {
                if (string.IsNullOrWhiteSpace(lastMatchMessage))
                {
                    throw new InvalidOperationException("当前没有可发送的匹配结果。");
                }

                SendTcpPayload(lastMatchMessage, "发送结果");
            });
        }

        private void SendTcpPayload(string payload, string sourceLabel)
        {
            if (string.IsNullOrEmpty(payload))
            {
                throw new InvalidOperationException("发送内容为空。");
            }

            tcpService.Send(payload, ResolveTcpEncoding(), tcpAppendNewLineCheckBox.Checked);
            string text = payload.TrimEnd('\r', '\n');
            AppendTcpHistory(sourceLabel + "：" + text);
            logger.Info(sourceLabel + "：" + text);
            RefreshUiState();
        }

        private void AutoSendMatchResultIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(lastMatchMessage))
            {
                return;
            }

            if (autoSendMatchResultCheckBox == null || !autoSendMatchResultCheckBox.Checked)
            {
                AppendTcpHistory("状态：已生成匹配结果，自动发送已关闭。");
                return;
            }

            if (!tcpService.IsConnected)
            {
                AppendTcpHistory("状态：TCP 未连接，匹配结果未发送：" + lastMatchMessage);
                return;
            }

            try
            {
                SendTcpPayload(lastMatchMessage, "发送结果");
            }
            catch (Exception ex)
            {
                logger.Error("自动发送匹配结果失败", ex);
                AppendTcpHistory("异常：自动发送匹配结果失败：" + ex.Message);
            }
        }

        private void SetLastMatchMessage(string message)
        {
            lastMatchMessage = message;
            if (!string.IsNullOrWhiteSpace(message))
            {
                AppendTcpHistory("最新匹配结果：" + message);
            }

            RefreshUiState();
        }

        private string BuildMatchResultMessage(string templateName, IList<ShapeMatchResult> matches)
        {
            string safeTemplate = EscapeMessageValue(string.IsNullOrWhiteSpace(templateName) ? "Template" : templateName);
            int count = matches == null ? 0 : matches.Count;
            if (count <= 0)
            {
                return "RESULT=NG,TEMPLATE=" + safeTemplate + ",COUNT=0,SCORE=0";
            }

            ShapeMatchResult best = matches.OrderByDescending(item => item.Score).First();
            return string.Format(
                CultureInfo.InvariantCulture,
                "RESULT=OK,TEMPLATE={0},COUNT={1},SCORE={2:F3},ROW={3:F2},COL={4:F2},ANGLE={5:F2}",
                safeTemplate,
                count,
                best.Score,
                best.Row,
                best.Column,
                best.AngleDeg);
        }

        private static string BuildErrorMatchMessage(string message)
        {
            return "RESULT=ERROR,MESSAGE=" + EscapeMessageValue(message);
        }

        private static string EscapeMessageValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace(",", "，").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private Encoding ResolveTcpEncoding()
        {
            string value = tcpEncodingCombo == null ? "UTF-8" : Convert.ToString(tcpEncodingCombo.SelectedItem);
            if (string.Equals(value, "ASCII", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.ASCII;
            }

            if (string.Equals(value, "GBK", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.GetEncoding("GBK");
            }

            return Encoding.UTF8;
        }

        private void TcpServiceMessageReceived(object sender, TcpCommunicationMessageEventArgs e)
        {
            RunOnUiThread(delegate
            {
                string text = e.Text.TrimEnd('\r', '\n');
                logger.Info("TCP接收：" + text);
                AppendTcpHistory("接收：" + text);
                SetTcpStatus("收到：" + text);
                RefreshUiState();
            });
        }

        private void TcpServiceStatusChanged(object sender, TcpCommunicationStatusEventArgs e)
        {
            RunOnUiThread(delegate
            {
                logger.Info("TCP状态：" + e.Message);
                AppendTcpHistory("状态：" + e.Message);
                SetTcpStatus(e.Message);
                RefreshUiState();
            });
        }

        private void TcpServiceErrorOccurred(object sender, TcpCommunicationErrorEventArgs e)
        {
            RunOnUiThread(delegate
            {
                logger.Error(e.Message, e.Exception);
                string text = e.Message + "：" + (e.Exception == null ? string.Empty : e.Exception.Message);
                AppendTcpHistory("异常：" + text);
                SetTcpStatus(text);
                RefreshUiState();
            });
        }

        private void SetTcpStatus(string text)
        {
            if (tcpStatusLabel != null)
            {
                tcpStatusLabel.Text = text;
            }
        }

        private void AppendTcpHistory(string text)
        {
            if (tcpHistoryTextBox == null)
            {
                return;
            }

            string line = string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, text ?? string.Empty);
            if (tcpHistoryTextBox.TextLength > 0)
            {
                tcpHistoryTextBox.AppendText(Environment.NewLine);
            }

            tcpHistoryTextBox.AppendText(line);
            tcpHistoryTextBox.SelectionStart = tcpHistoryTextBox.TextLength;
            tcpHistoryTextBox.ScrollToCaret();
        }

        private void RunOnUiThread(Action action)
        {
            if (IsDisposed || action == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(action);
                return;
            }

            action();
        }

        private void AddRecord(string type, string resultCode, double score, string message, ShapeMatchResult match)
        {
            InspectionRecord record = new InspectionRecord
            {
                Timestamp = DateTime.Now,
                ImageSource = currentImageSource,
                InspectionType = type,
                Roi = currentRoi == null ? null : currentRoi.Clone(),
                TemplatePath = currentTemplateItem == null ? string.Empty : currentTemplateItem.TemplatePath,
                MatchRow = match == null ? (double?)null : match.Row,
                MatchColumn = match == null ? (double?)null : match.Column,
                MatchAngle = match == null ? (double?)null : match.AngleDeg,
                ResultCode = resultCode,
                Score = score,
                Message = message,
                ImageSnapshot = currentImage == null ? null : currentImage.CopyImage()
            };
            resultStore.Add(record);
        }

        private void RefreshResultGrid()
        {
            DateTime? start = startTimeCheckBox != null && startTimeCheckBox.Checked ? startTimePicker.Value : (DateTime?)null;
            DateTime? end = endTimeCheckBox != null && endTimeCheckBox.Checked ? endTimePicker.Value : (DateTime?)null;
            string result = resultFilterTextBox == null ? null : resultFilterTextBox.Text;
            string source = sourceFilterTextBox == null ? null : sourceFilterTextBox.Text;
            filteredRecords = resultStore.Query(start, end, result, source).ToList();

            gridRows.RaiseListChangedEvents = false;
            gridRows.Clear();
            foreach (InspectionRecord record in filteredRecords)
            {
                gridRows.Add(new ResultGridRow(record));
            }
            gridRows.RaiseListChangedEvents = true;
            gridRows.ResetBindings();
        }

        private void ReplaySelectedResult()
        {
            if (resultGrid.CurrentRow == null || resultGrid.CurrentRow.DataBoundItem == null)
            {
                return;
            }

            ResultGridRow row = (ResultGridRow)resultGrid.CurrentRow.DataBoundItem;
            InspectionRecord record = resultStore.Records.FirstOrDefault(item => item.Id == row.Id);
            if (record == null || record.ImageSnapshot == null)
            {
                return;
            }

            SetCurrentImage(record.ImageSnapshot.CopyImage(), record.ImageSource);
            SetCurrentRoi(record.Roi == null ? null : record.Roi.Clone());
            currentMatches.Clear();
            if (record.MatchRow.HasValue && record.MatchColumn.HasValue)
            {
                currentMatches.Add(new ShapeMatchResult
                {
                    Row = record.MatchRow.Value,
                    Column = record.MatchColumn.Value,
                    AngleDeg = record.MatchAngle.GetValueOrDefault(),
                    Score = record.Score,
                    RoiShapeType = record.Roi == null ? (RoiShapeType?)null : record.Roi.ShapeType,
                    RoiWidth = record.Roi == null ? 0 : Math.Abs(record.Roi.Column2 - record.Roi.Column1),
                    RoiHeight = record.Roi == null ? 0 : Math.Abs(record.Roi.Row2 - record.Roi.Row1),
                    RoiRadius = record.Roi == null ? 0 : record.Roi.Radius
                });
            }
            RefreshDisplay();
            rightTabs.SelectedIndex = 1;
        }

        private void ExportCsv()
        {
            RunAction("导出 CSV", delegate
            {
                EnsureExportRecords();
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "CSV file|*.csv";
                    dialog.FileName = "inspection_results.csv";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        csvExportService.Export(dialog.FileName, filteredRecords);
                        logger.Info("CSV 已导出：" + dialog.FileName);
                    }
                }
            });
        }

        private void ExportXlsx()
        {
            RunAction("导出 XLSX", delegate
            {
                EnsureExportRecords();
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Excel workbook|*.xlsx";
                    dialog.FileName = "inspection_results.xlsx";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        xlsxExportService.Export(dialog.FileName, filteredRecords);
                        logger.Info("XLSX 已导出：" + dialog.FileName);
                    }
                }
            });
        }

        private void ClearResults()
        {
            if (MessageBox.Show(this, "确认清空所有结果？", "清空结果", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                resultStore.Clear();
                currentMatches.Clear();
                RefreshDisplay();
            }
        }

        private void SaveViewSnapshot()
        {
            RunAction("保存截图", delegate
            {
                if (currentImage == null)
                {
                    throw new InvalidOperationException("当前没有图像。");
                }

                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "PNG image|*.png|Bitmap image|*.bmp";
                    dialog.FileName = "vision_view.png";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        HImage snapshot = new HImage();
                        try
                        {
                            snapshot.DumpWindowImage(imageWindow.HalconWindow);
                            string ext = Path.GetExtension(dialog.FileName).TrimStart('.').ToLowerInvariant();
                            snapshot.WriteImage(ext == "bmp" ? "bmp" : "png", 0, dialog.FileName);
                            logger.Info("截图已保存：" + dialog.FileName);
                        }
                        finally
                        {
                            snapshot.Dispose();
                        }
                    }
                }
            });
        }

        private void FileDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void FileDragDrop(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null && paths.Length > 0)
            {
                fileModeRadio.Checked = true;
                LoadImageQueue(paths);
            }
        }

        private void LoggerMessageLogged(object sender, LogMessageEventArgs e)
        {
            if (IsDisposed || logTextBox == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(delegate { AppendLog(e.Message); }));
            }
            else
            {
                AppendLog(e.Message);
            }
        }

        private void AppendLog(string message)
        {
            logTextBox.AppendText(message + Environment.NewLine);
        }

        private void SetCurrentImage(HImage image, string source)
        {
            SetCurrentImage(image, source, true);
        }

        private void SetCurrentImage(HImage image, string source, bool replaceOriginal)
        {
            DisposeCurrentImage();
            DisposePendingSearchRoi();
            currentImage = image;
            if (replaceOriginal)
            {
                DisposeOriginalImage();
                originalImage = image == null ? null : image.CopyImage();
            }

            currentImageSource = string.IsNullOrWhiteSpace(source) ? "Unknown" : source;
            int width;
            int height;
            currentImage.GetImageSize(out width, out height);
            viewport.SetImageSize(width, height);
            RefreshUiState();
        }

        private void ConvertCurrentImageToGray()
        {
            RunAction("转灰度", delegate
            {
                if (currentImage == null)
                {
                    throw new InvalidOperationException("请先打开图片。");
                }

                HImage image = imageService.ToGray(currentImage);
                SetCurrentImage(image, currentImageSource, false);
                currentMatches.Clear();
                logger.Info("当前图像已转为灰度。");
                RefreshDisplay();
            });
        }

        private void ConvertCurrentImageToColor()
        {
            RunAction("转彩色", delegate
            {
                if (currentImage == null)
                {
                    throw new InvalidOperationException("请先打开图片。");
                }

                HImage source = originalImage != null && imageService.GetChannelCount(originalImage) >= 3
                    ? originalImage
                    : currentImage;
                HImage image = imageService.ToColor(source);
                SetCurrentImage(image, currentImageSource, false);
                currentMatches.Clear();
                logger.Info("当前图像已转为彩色。");
                RefreshDisplay();
            });
        }

        private void RestoreOriginalImage()
        {
            RunAction("恢复原图", delegate
            {
                if (originalImage == null)
                {
                    throw new InvalidOperationException("没有可恢复的原图。");
                }

                SetCurrentImage(originalImage.CopyImage(), currentImageSource, false);
                currentMatches.Clear();
                logger.Info("已恢复原始图像。");
                RefreshDisplay();
            });
        }

        private void SetCurrentRoi(RoiData roi)
        {
            DisposeCurrentRoi();
            currentRoi = roi;
        }

        private void SetPendingSearchRoi(RoiData roi)
        {
            DisposePendingSearchRoi();
            pendingSearchRoi = roi;
        }

        private void SetPendingTemplateRoi(RoiData roi)
        {
            DisposePendingTemplateRoi();
            pendingTemplateRoi = roi;
        }

        private void SetTrainingMask(HRegion mask)
        {
            DisposeTrainingMask();
            trainingMask = mask;
        }

        private void SetDisplayFrame(RoiData frame)
        {
            DisposeDisplayFrame();
            displayFrame = frame;
        }

        private static RoiData OffsetRoi(RoiData roi, double rowOffset, double columnOffset)
        {
            if (roi.ShapeType == RoiShapeType.Circle)
            {
                return RoiData.CreateCircle(roi.Row + rowOffset, roi.Column + columnOffset, roi.Radius);
            }

            return RoiData.CreateRectangle(
                roi.Row1 + rowOffset,
                roi.Column1 + columnOffset,
                roi.Row2 + rowOffset,
                roi.Column2 + columnOffset);
        }

        private static RoiData ScaleRoi(RoiData roi, double factor)
        {
            if (roi.ShapeType == RoiShapeType.Circle)
            {
                return RoiData.CreateCircle(roi.Row, roi.Column, Math.Max(1, roi.Radius * factor));
            }

            double centerRow = (roi.Row1 + roi.Row2) / 2.0;
            double centerColumn = (roi.Column1 + roi.Column2) / 2.0;
            double halfHeight = Math.Max(1, (roi.Row2 - roi.Row1) * factor / 2.0);
            double halfWidth = Math.Max(1, (roi.Column2 - roi.Column1) * factor / 2.0);
            return RoiData.CreateRectangle(centerRow - halfHeight, centerColumn - halfWidth, centerRow + halfHeight, centerColumn + halfWidth);
        }

        private static RoiData TransformFrame(RoiData roi, FrameEditMode mode, double rowDelta, double columnDelta)
        {
            if (roi.ShapeType == RoiShapeType.Circle)
            {
                if (mode == FrameEditMode.Radius)
                {
                    return RoiData.CreateCircle(roi.Row, roi.Column, Math.Max(1, roi.Radius + columnDelta));
                }

                return RoiData.CreateCircle(roi.Row + rowDelta, roi.Column + columnDelta, roi.Radius);
            }

            double row1 = roi.Row1;
            double row2 = roi.Row2;
            double column1 = roi.Column1;
            double column2 = roi.Column2;

            switch (mode)
            {
                case FrameEditMode.Move:
                    return OffsetRoi(roi, rowDelta, columnDelta);
                case FrameEditMode.Left:
                    column1 += columnDelta;
                    break;
                case FrameEditMode.Right:
                    column2 += columnDelta;
                    break;
                case FrameEditMode.Top:
                    row1 += rowDelta;
                    break;
                case FrameEditMode.Bottom:
                    row2 += rowDelta;
                    break;
                case FrameEditMode.TopLeft:
                    row1 += rowDelta;
                    column1 += columnDelta;
                    break;
                case FrameEditMode.TopRight:
                    row1 += rowDelta;
                    column2 += columnDelta;
                    break;
                case FrameEditMode.BottomLeft:
                    row2 += rowDelta;
                    column1 += columnDelta;
                    break;
                case FrameEditMode.BottomRight:
                    row2 += rowDelta;
                    column2 += columnDelta;
                    break;
            }

            if (Math.Abs(row2 - row1) < 2)
            {
                row2 = row1 + Math.Sign(row2 - row1 == 0 ? 1 : row2 - row1) * 2;
            }

            if (Math.Abs(column2 - column1) < 2)
            {
                column2 = column1 + Math.Sign(column2 - column1 == 0 ? 1 : column2 - column1) * 2;
            }

            return RoiData.CreateRectangle(row1, column1, row2, column2);
        }

        private void DisposeCurrentImage()
        {
            if (currentImage != null)
            {
                currentImage.Dispose();
                currentImage = null;
                viewport.Clear();
            }
        }

        private void DisposeOriginalImage()
        {
            if (originalImage != null)
            {
                originalImage.Dispose();
                originalImage = null;
            }
        }

        private void DisposeCurrentRoi()
        {
            if (currentRoi != null)
            {
                currentRoi.Dispose();
                currentRoi = null;
            }
        }

        private void DisposePendingSearchRoi()
        {
            if (pendingSearchRoi != null)
            {
                pendingSearchRoi.Dispose();
                pendingSearchRoi = null;
            }
        }

        private void DisposePendingTemplateRoi()
        {
            if (pendingTemplateRoi != null)
            {
                pendingTemplateRoi.Dispose();
                pendingTemplateRoi = null;
            }
        }

        private void DisposeTrainingMask()
        {
            if (trainingMask != null)
            {
                trainingMask.Dispose();
                trainingMask = null;
            }
        }

        private void DisposeDisplayFrame()
        {
            if (displayFrame != null)
            {
                displayFrame.Dispose();
                displayFrame = null;
            }
        }

        private void DisposeMaskUndoStack()
        {
            foreach (HRegion region in maskUndoStack)
            {
                if (region != null)
                {
                    region.Dispose();
                }
            }

            maskUndoStack.Clear();
        }

        private void DisposeTemplates()
        {
            foreach (TemplateItem item in templateItems)
            {
                if (item != null)
                {
                    item.Dispose();
                }
            }

            templateItems.Clear();
            currentTemplateItem = null;
        }

        private void RefreshDisplay()
        {
            if (imageWindow == null || imageWindow.HalconWindow == null)
            {
                return;
            }

            imageWindow.HalconWindow.ClearWindow();
            if (currentImage != null)
            {
                viewport.Apply(imageWindow.HalconWindow);
                currentImage.DispImage(imageWindow.HalconWindow);
                HRegion visibleMask = showTrainingMaskCheckBox == null || showTrainingMaskCheckBox.Checked ? trainingMask : null;
                RoiData previewRoi = pendingSearchRoi ?? pendingTemplateRoi ?? roiEditor.PreviewRoi;
                RoiData templateRoi = currentTemplateItem == null ? null : currentTemplateItem.TemplateRoi;
                RoiData confirmedBoundary = currentMatches.Count == 0 ? (currentRoi ?? templateRoi) : templateRoi;
                ShapeTemplateService activeTemplateService = currentTemplateItem == null ? null : currentTemplateItem.Service;
                bool showSearchRoi = showSearchRoiCheckBox != null && showSearchRoiCheckBox.Checked;
                bool showTemplateRoi = confirmedBoundary != null && currentMatches.Count == 0;
                bool showDisplayFrame = showDisplayFrameCheckBox != null && showDisplayFrameCheckBox.Checked;
                overlayRenderer.Draw(imageWindow.HalconWindow, currentRoi, confirmedBoundary, previewRoi, visibleMask, displayFrame, currentMatches, activeTemplateService, showSearchRoi, showTemplateRoi, showDisplayFrame);
            }
            RefreshUiState();
        }

        private void RefreshUiState()
        {
            bool hasImage = currentImage != null;
            bool fileMode = fileModeRadio != null && fileModeRadio.Checked;
            bool cameraMode = cameraModeRadio != null && cameraModeRadio.Checked;
            bool hasRoi = currentRoi != null;
            bool hasPendingRoi = pendingSearchRoi != null || roiEditor.IsPolygonDrawing;
            bool hasTemplateItem = currentTemplateItem != null;
            bool hasTemplateRoi = currentTemplateItem != null && currentTemplateItem.TemplateRoi != null;
            bool hasPendingTemplateRoi = pendingTemplateRoi != null;
            bool hasTemplate = currentTemplateItem != null && currentTemplateItem.HasModel;
            bool hasMask = trainingMask != null;
            bool hasFrame = displayFrame != null;
            bool maskReady = hasMask && trainingMaskConfirmed;

            if (modeStatus != null)
            {
                modeStatus.Text = string.Format("模式: {0} / 工具: {1}", cameraMode ? "相机" : "本地图", ToolText(roiEditor.Tool));
                imageStatus.Text = hasImage ? string.Format("图像: {0}x{1}", viewport.ImageWidth, viewport.ImageHeight) : "图像: --";
                zoomStatus.Text = viewport.ZoomText;
                roiStatus.Text = hasPendingRoi ? "ROI: 待确认" : (hasRoi ? "ROI: 已确认 " + currentRoi.ShapeType : "ROI: 未设置");
                templateStatus.Text = hasTemplateItem ? "模板: " + currentTemplateItem.Name + (hasTemplate ? " 已训练" : " 未训练") : "模板: 未选择";
            }

            if (imageNavLabel != null)
            {
                imageNavLabel.Text = imageQueue.Count > 0 ? string.Format("{0}/{1}", currentImageIndex + 1, imageQueue.Count) : "0/0";
            }

            if (matchSummaryLabel != null)
            {
                string imageName = string.IsNullOrWhiteSpace(currentImageSource) ? "Unknown" : Path.GetFileName(currentImageSource);
                if (!hasImage)
                {
                    matchSummaryLabel.Text = "请使用图像区上方工具条打开图片。";
                }
                else if (currentMatches.Count > 0)
                {
                    string templateName = hasTemplateItem ? currentTemplateItem.Name : "未选择模板";
                    string messageText = string.IsNullOrWhiteSpace(lastMatchMessage) ? string.Empty : "；最新外发: " + lastMatchMessage;
                    matchSummaryLabel.Text = string.Format("当前图片: {0}；模板: {1}；匹配 {2} 个，最高分 {3:F3}{4}。", imageName, templateName, currentMatches.Count, currentMatches.Max(item => item.Score), messageText);
                }
                else
                {
                    matchSummaryLabel.Text = string.Format("当前图片: {0}。可在图像区上方直接切换上一张/下一张。", imageName);
                }
            }

            if (roiWorkflowLabel != null)
            {
                roiWorkflowLabel.Text = hasPendingRoi ? "ROI: 待确认。确认后即可进入模板匹配设置。" : (hasRoi ? "ROI: 已确认，橙色边界为当前参考区域。下一步点击“模板匹配设置”。" : "ROI: 未框选。支持矩形、圆形、多边形。");
            }

            if (maskWorkflowLabel != null)
            {
                maskWorkflowLabel.Text = maskReady ? "图像屏蔽: 已应用，创建模板时会排除屏蔽区域。" : (hasMask ? "图像屏蔽: 已编辑但未应用；创建模板仍直接使用 ROI。" : "图像屏蔽: 未启用。默认直接使用 ROI 创建模板。");
            }

            if (cameraInterfaceCombo == null)
            {
                return;
            }

            cameraInterfaceCombo.Enabled = cameraMode;
            cameraDeviceTextBox.Enabled = cameraMode;
            openCameraButton.Enabled = cameraMode;
            grabButton.Enabled = cameraMode;
            continuousButton.Enabled = cameraMode;
            stopGrabButton.Enabled = cameraMode;
            closeCameraButton.Enabled = cameraMode;

            loadImagesButton.Enabled = fileMode && !playbackActive;
            loadFolderButton.Enabled = fileMode && !playbackActive;
            prevImageButton.Enabled = fileMode && imageQueue.Count > 0 && !playbackActive;
            nextImageButton.Enabled = fileMode && imageQueue.Count > 0 && !playbackActive;
            playButton.Enabled = fileMode && imageQueue.Count > 1 && !playbackActive;
            stopPlayButton.Enabled = fileMode && playbackActive;
            toGrayButton.Enabled = hasImage;
            toColorButton.Enabled = hasImage;
            restoreOriginalButton.Enabled = hasImage && originalImage != null;

            rectToolButton.Enabled = hasImage;
            circleToolButton.Enabled = hasImage;
            polygonToolButton.Enabled = hasImage;
            confirmRoiButton.Enabled = hasPendingRoi;
            discardPendingRoiButton.Enabled = hasPendingRoi;
            addTemplateButton.Enabled = true;
            renameTemplateButton.Enabled = hasTemplateItem;
            deleteTemplateButton.Enabled = hasTemplateItem;
            loadTemplateButton.Enabled = true;

            templateRectToolButton.Enabled = hasImage && hasTemplateItem;
            templateCircleToolButton.Enabled = hasImage && hasTemplateItem;
            confirmTemplateRoiButton.Enabled = hasTemplateItem && hasPendingTemplateRoi;
            discardPendingTemplateRoiButton.Enabled = hasPendingTemplateRoi;

            trainTemplateButton.Enabled = hasImage && hasRoi;
            saveTemplateButton.Enabled = hasTemplate;
            runTemplateButton.Enabled = hasImage && hasTemplate;
            runHdevButton.Enabled = hasImage && hasRoi;
            clearRoiButton.Enabled = hasRoi || hasPendingRoi;
            clearOverlayButton.Enabled = currentMatches.Count > 0;
            fitButton.Enabled = hasImage;
            saveViewButton.Enabled = hasImage;
            resetMaskButton.Enabled = hasTemplateRoi;
            undoMaskButton.Enabled = maskUndoStack.Count > 0;
            maskRectButton.Enabled = hasImage && hasTemplateRoi;
            maskCircleButton.Enabled = hasImage && hasTemplateRoi;
            maskBrushButton.Enabled = hasImage && hasTemplateRoi;
            maskEraserButton.Enabled = hasImage && hasMask;
            confirmTrainingMaskButton.Enabled = hasTemplateRoi && hasMask;
            previewMaskButton.Enabled = hasMask;
            useRoiAsFrameButton.Enabled = hasTemplateRoi;
            showDisplayFrameCheckBox.Enabled = hasFrame;
            showSearchRoiCheckBox.Enabled = hasRoi;
            moveFrameLeftButton.Enabled = hasFrame;
            moveFrameRightButton.Enabled = hasFrame;
            moveFrameUpButton.Enabled = hasFrame;
            moveFrameDownButton.Enabled = hasFrame;
            growFrameButton.Enabled = hasFrame;
            shrinkFrameButton.Enabled = hasFrame;

            if (tcpClientModeRadio != null)
            {
                bool tcpClientMode = tcpClientModeRadio.Checked;
                bool tcpRunning = tcpService.IsRunning;
                bool tcpConnected = tcpService.IsConnected;
                tcpClientModeRadio.BackColor = tcpClientMode ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
                tcpServerModeRadio.BackColor = !tcpClientMode ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
                if (tcpIpLabel != null)
                {
                    tcpIpLabel.Text = tcpClientMode ? "远端IP" : "监听IP";
                }

                tcpConnectButton.Enabled = tcpClientMode && !tcpRunning;
                tcpDisconnectButton.Enabled = tcpClientMode && tcpRunning;
                tcpStartServerButton.Enabled = !tcpClientMode && !tcpRunning;
                tcpStopServerButton.Enabled = !tcpClientMode && tcpRunning;
                tcpSendButton.Enabled = tcpConnected;
                sendLastMatchButton.Enabled = tcpConnected && !string.IsNullOrWhiteSpace(lastMatchMessage);
                tcpIpTextBox.Enabled = !tcpRunning;
                tcpPortInput.Enabled = !tcpRunning;
                tcpEncodingCombo.Enabled = !tcpRunning;
                tcpClientGroup.Visible = tcpClientMode;
                tcpServerGroup.Visible = !tcpClientMode;
                if (tcpStatusLabel != null)
                {
                    if (tcpConnected)
                    {
                        tcpStatusLabel.Text = "TCP 已连接，可发送。";
                    }
                    else if (tcpRunning)
                    {
                        tcpStatusLabel.Text = tcpClientMode ? "客户端连接中，发送暂不可用。" : "服务端监听中，等待客户端接入。";
                    }
                    else
                    {
                        tcpStatusLabel.Text = "TCP 未连接，发送不可用。";
                    }
                }
            }

            if (templateInfoLabel != null)
            {
                if (!hasImage)
                {
                    templateInfoLabel.Text = "请先打开图片。";
                }
                else if (hasPendingRoi)
                {
                    templateInfoLabel.Text = "ROI 待确认。点击“确认ROI”后即可进入模板匹配设置。";
                }
                else if (hasTemplate)
                {
                    templateInfoLabel.Text = "当前模板 “" + currentTemplateItem.Name + "” 已训练，可执行匹配。";
                }
                else if (!hasRoi)
                {
                    templateInfoLabel.Text = "请先框选并确认 ROI。";
                }
                else
                {
                    templateInfoLabel.Text = "ROI 已确认。点击“模板匹配设置”，在弹窗中选择模板区域并保存训练。";
                }
            }
        }

        private void EnsureExportRecords()
        {
            if (filteredRecords == null || filteredRecords.Count == 0)
            {
                throw new InvalidOperationException("当前查询结果为空，无法导出。");
            }
        }

        private void RunAction(string title, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.Error(title + "失败", ex);
                ShowMessage(ex.Message);
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static TabPage CreateTab(string title)
        {
            return new TabPage(title) { Padding = new Padding(0), BackColor = Color.FromArgb(246, 248, 251) };
        }

        private static TableLayoutPanel CreateTabLayout(int rows)
        {
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = rows, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            return layout;
        }

        private static GroupBox CreateWorkflowGroup(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
        }

        private static GroupBox CreateFixedGroup(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 8),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
        }

        private static GroupBox CreateCompactGroup()
        {
            return new GroupBox
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                Margin = new Padding(0),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular)
            };
        }

        private static TableLayoutPanel CreateWorkflowLayout(int rows)
        {
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = rows, Padding = new Padding(8), Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            return layout;
        }

        private static void AddWorkflowGroup(TableLayoutPanel layout, GroupBox group)
        {
            int row = NextRow(layout);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(group, 0, row);
        }

        private static FlowLayoutPanel CreateLinePanel()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, Margin = new Padding(0), Padding = new Padding(0) };
        }

        private static FlowLayoutPanel CreateFixedLinePanel()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, Margin = new Padding(0), Padding = new Padding(0) };
        }

        private static Button CreateButton(string text)
        {
            return new Button { Text = text, Width = 96, MinimumSize = new Size(96, 34), Height = 34, Margin = new Padding(3, 3, 5, 3), FlatStyle = FlatStyle.System };
        }

        private static Button CreatePrimaryButton(string text)
        {
            return new Button { Text = text, Width = 136, MinimumSize = new Size(120, 38), Height = 38, Margin = new Padding(3, 3, 7, 3), FlatStyle = FlatStyle.System };
        }

        private static Button CreateToolbarButton(string text)
        {
            return new Button { Text = text, Width = 96, MinimumSize = new Size(96, 34), Height = 34, Margin = new Padding(2, 2, 4, 2), FlatStyle = FlatStyle.System };
        }

        private static Button CreateSmallButton(string text)
        {
            return new Button { Text = text, Width = 82, MinimumSize = new Size(82, 30), Height = 30, Margin = new Padding(3, 2, 4, 2), FlatStyle = FlatStyle.System };
        }

        private static Label CreateInfoLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(78, 89, 108) };
        }

        private static NumericUpDown CreateNumber(decimal min, decimal max, decimal value, decimal increment, int decimals)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Increment = increment, DecimalPlaces = decimals, Dock = DockStyle.Left, Width = 120 };
        }

        private static void AddSection(TableLayoutPanel layout, string text)
        {
            Label label = new Label { Text = text, Dock = DockStyle.Fill, Height = 28, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
            int row = NextRow(layout);
            layout.Controls.Add(label, 0, row);
            layout.SetColumnSpan(label, 2);
        }

        private static void AddHelp(TableLayoutPanel layout, string label, string text)
        {
            AddRow(layout, label, CreateInfoLabel(text));
        }

        private static void AddRow(TableLayoutPanel layout, string labelText, Control control)
        {
            Label label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            int row = NextRow(layout);

            EnsureRowExists(layout, row);

            control.Margin = new Padding(3, 4, 3, 4);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private static void AddFullRow(TableLayoutPanel layout, Control control, int height)
        {
            int row = NextRow(layout);

            EnsureRowExists(layout, row);

            layout.RowStyles[row] = new RowStyle(SizeType.Absolute, height);
            control.Margin = new Padding(3, 4, 3, 4);
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        private static void EnsureRowExists(TableLayoutPanel layout, int row)
        {
            while (layout.RowStyles.Count <= row)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowCount = layout.RowStyles.Count;
            }
        }

        private static int NextRow(TableLayoutPanel layout)
        {
            int row = 0;
            foreach (Control control in layout.Controls)
            {
                row = Math.Max(row, layout.GetRow(control) + 1);
            }
            return row;
        }

        private void AddGridColumn(string propertyName, string title, int width)
        {
            resultGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = title,
                Width = width,
                AutoSizeMode = title == "消息" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None
            });
        }

        private enum FrameEditMode
        {
            None,
            Move,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Radius
        }

        private sealed class ResultGridRow
        {
            public ResultGridRow(InspectionRecord record)
            {
                Id = record.Id;
                Timestamp = record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                InspectionType = record.InspectionType;
                ResultCode = record.ResultCode;
                Score = record.Score.ToString("F3");
                Match = record.MatchRow.HasValue ? string.Format("R{0:F1}, C{1:F1}", record.MatchRow.Value, record.MatchColumn.GetValueOrDefault()) : string.Empty;
                Message = record.Message;
            }

            public int Id { get; private set; }
            public string Timestamp { get; private set; }
            public string InspectionType { get; private set; }
            public string ResultCode { get; private set; }
            public string Score { get; private set; }
            public string Match { get; private set; }
            public string Message { get; private set; }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1141, 560);
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }
    }
}
