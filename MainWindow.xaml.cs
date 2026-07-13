using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow : Window
    {
        private const string VersionStamp = "WPF Product Station 2026-06-29";

        private readonly HalconImageService imageService = new HalconImageService();
        private readonly AppLogger logger = new AppLogger();
        private readonly ImageViewportController viewport = new ImageViewportController();
        private readonly OverlayRenderer overlayRenderer = new OverlayRenderer();
        private readonly RoiEditor roiEditor = new RoiEditor();
        private readonly TcpCommunicationService tcpService = new TcpCommunicationService();
        private readonly InspectionResultStore resultStore = new InspectionResultStore();
        private readonly RecipeService recipeService = new RecipeService();
        private readonly StartupDiagnosticsService diagnosticsService = new StartupDiagnosticsService();
        private readonly CsvExportService csvExportService = new CsvExportService();
        private readonly XlsxExportService xlsxExportService = new XlsxExportService();
        private readonly RuntimeStatistics runtimeStatistics = new RuntimeStatistics();
        private readonly HDevInspectionService hdevService = new HDevInspectionService();
        private readonly DispatcherTimer playbackTimer = new DispatcherTimer();
        private readonly DispatcherTimer runTimer = new DispatcherTimer();
        private readonly ObservableCollection<VmToolInstance> flowTools = new ObservableCollection<VmToolInstance>();
        private readonly List<VmToolCatalogItem> toolCatalog = new List<VmToolCatalogItem>();

        private Forms.Integration.WindowsFormsHost host;
        private HWindowControl imageWindow;
        private HImage currentImage;
        private HImage originalImage;
        private string currentImagePath;
        private readonly List<string> imageFiles = new List<string>();
        private int imageIndex = -1;
        private RoiData currentRoi;
        private RoiData pendingRoi;
        private TemplateItem currentTemplateItem;
        private readonly List<ShapeMatchResult> currentMatches = new List<ShapeMatchResult>();
        private HObject toolOverlayRegion;
        private HObject toolOverlayContours;
        private string lastResultPayload = string.Empty;
        private string currentRecipePath;
        private bool refreshQueued;
        private bool uiReady;
        private bool isPanning;
        private System.Drawing.Point lastPanPoint;
        private bool isContinuousRunning;
        private bool inspectorUpdating;

        public MainWindow()
        {
            InitializeComponent();

            playbackTimer.Interval = TimeSpan.FromMilliseconds(650);
            playbackTimer.Tick += PlaybackTimer_Tick;
            runTimer.Interval = TimeSpan.FromMilliseconds(250);
            runTimer.Tick += RunTimer_Tick;

            logger.MessageLogged += Logger_MessageLogged;
            tcpService.MessageReceived += TcpService_MessageReceived;
            tcpService.StatusChanged += TcpService_StatusChanged;
            tcpService.ErrorOccurred += TcpService_ErrorOccurred;
            resultStore.Changed += ResultStore_Changed;

            InitializeVmWorkspace();
            uiReady = true;
        }

        private void InitializeVmWorkspace()
        {
            flowTools.CollectionChanged += delegate { RefreshFlowSequence(); };
            toolCatalog.AddRange(new[]
            {
                CreateCatalogItem(VmToolKind.ShapeMatch),
                CreateCatalogItem(VmToolKind.Blob),
                CreateCatalogItem(VmToolKind.GrayStat),
                CreateCatalogItem(VmToolKind.EdgeMeasure),
                CreateCatalogItem(VmToolKind.HDevelop)
            });
            ToolCatalogList.ItemsSource = toolCatalog;
            FlowToolList.ItemsSource = flowTools;
            ApplyFlowFromRecipe(new VisionRecipe());
        }

        private void RefreshFlowSequence()
        {
            for (int index = 0; index < flowTools.Count; index++)
            {
                flowTools[index].Sequence = index + 1;
            }
        }

        private void FlowToolEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!uiReady)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            VmToolInstance tool = checkBox == null ? null : checkBox.DataContext as VmToolInstance;
            if (tool == null)
            {
                return;
            }

            tool.IsEnabled = checkBox.IsChecked == true;
            RefreshUiState();
        }

        private static VmToolCatalogItem CreateCatalogItem(VmToolKind kind)
        {
            return new VmToolCatalogItem
            {
                Kind = kind,
                Name = ToolMetadata.GetDisplayName(kind),
                Category = ToolMetadata.GetCategory(kind),
                Description = ToolMetadata.GetDescription(kind)
            };
        }

        private VmToolInstance CreateFlowTool(VmToolKind kind, string name, bool isEnabled, string toolId)
        {
            return new VmToolInstance
            {
                ToolId = string.IsNullOrWhiteSpace(toolId) ? Guid.NewGuid().ToString("N") : toolId,
                Kind = kind,
                InstanceName = string.IsNullOrWhiteSpace(name) ? CreateUniqueToolName(kind) : name,
                IsEnabled = isEnabled,
                InputSummary = DefaultInputSummary(kind),
                OutputSummary = "尚未运行"
            };
        }

        private string CreateUniqueToolName(VmToolKind kind)
        {
            string baseName = ToolMetadata.GetDisplayName(kind);
            int index = 1;
            string candidate = baseName;
            while (flowTools.Any(item => string.Equals(item.InstanceName, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
                candidate = baseName + "_" + index.ToString("00", CultureInfo.InvariantCulture);
            }

            return candidate;
        }

        private static string DefaultInputSummary(VmToolKind kind)
        {
            switch (kind)
            {
                case VmToolKind.ShapeMatch:
                    return "Image + SearchROI + ShapeModel";
                case VmToolKind.Blob:
                case VmToolKind.GrayStat:
                case VmToolKind.EdgeMeasure:
                    return "Image + 可选 ROI";
                case VmToolKind.HDevelop:
                    return "Image + ROI";
                default:
                    return "--";
            }
        }

        private void ApplyFlowFromRecipe(VisionRecipe recipe)
        {
            flowTools.Clear();

            if (recipe != null && recipe.ToolFlow != null && recipe.ToolFlow.Count > 0)
            {
                foreach (ToolFlowRecipeItem recipeItem in recipe.ToolFlow)
                {
                    VmToolKind kind;
                    if (recipeItem == null || !Enum.TryParse(recipeItem.ToolType, true, out kind))
                    {
                        continue;
                    }

                    if (flowTools.Any(item => item.Kind == kind))
                    {
                        continue;
                    }

                    flowTools.Add(CreateFlowTool(kind, recipeItem.InstanceName, recipeItem.IsEnabled, recipeItem.ToolId));
                }
            }

            if (flowTools.Count == 0)
            {
                VisionRecipe legacy = recipe ?? new VisionRecipe();
                if (legacy.EnableShapeMatch)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.ShapeMatch, null, true, null));
                }
                if (legacy.EnableBlob)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.Blob, null, true, null));
                }
                if (legacy.EnableGrayStat)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.GrayStat, null, true, null));
                }
                if (legacy.EnableEdgeMeasure)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.EdgeMeasure, null, true, null));
                }
                if (legacy.EnableHDevelop)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.HDevelop, null, true, null));
                }
            }

            SyncLegacyToolChecksFromFlow();
            if (FlowToolList != null && flowTools.Count > 0)
            {
                FlowToolList.SelectedIndex = 0;
            }

            RefreshInspector();
        }

        private List<ToolFlowRecipeItem> CaptureFlowRecipe()
        {
            return flowTools.Select(item => new ToolFlowRecipeItem
            {
                ToolId = item.ToolId,
                ToolType = item.Kind.ToString(),
                InstanceName = item.InstanceName,
                IsEnabled = item.IsEnabled
            }).ToList();
        }

        private bool HasEnabledTool(VmToolKind kind)
        {
            return flowTools.Any(item => item.Kind == kind && item.IsEnabled);
        }

        private void SyncLegacyToolChecksFromFlow()
        {
            if (EnableShapeToolCheckBox == null)
            {
                return;
            }

            EnableShapeToolCheckBox.IsChecked = HasEnabledTool(VmToolKind.ShapeMatch);
            EnableBlobToolCheckBox.IsChecked = HasEnabledTool(VmToolKind.Blob);
            EnableGrayToolCheckBox.IsChecked = HasEnabledTool(VmToolKind.GrayStat);
            EnableEdgeToolCheckBox.IsChecked = HasEnabledTool(VmToolKind.EdgeMeasure);
            EnableHDevToolCheckBox.IsChecked = HasEnabledTool(VmToolKind.HDevelop);
        }

        private void RefreshInspector()
        {
            if (FlowToolList == null || InspectorToolTitleText == null)
            {
                return;
            }

            inspectorUpdating = true;
            try
            {
                ShapeInspectorPanel.Visibility = Visibility.Collapsed;
                BlobInspectorPanel.Visibility = Visibility.Collapsed;
                GrayInspectorPanel.Visibility = Visibility.Collapsed;
                EdgeInspectorPanel.Visibility = Visibility.Collapsed;
                HDevInspectorPanel.Visibility = Visibility.Collapsed;

                VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
                bool hasSelection = selected != null;
                InspectorEmptyText.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
                ToolInstanceNameTextBox.IsEnabled = hasSelection;
                SelectedToolEnabledCheckBox.IsEnabled = hasSelection;

                if (!hasSelection)
                {
                    InspectorToolTitleText.Text = "请选择流程工具";
                    InspectorToolTypeText.Text = "从左侧工具箱添加，或在流程中选择实例";
                    ToolInstanceNameTextBox.Text = string.Empty;
                    SelectedToolEnabledCheckBox.IsChecked = false;
                    SelectedToolStatusText.Text = "--";
                    InspectorInputSummaryText.Text = "--";
                    InspectorOutputSummaryText.Text = "--";
                    InspectorErrorText.Text = "--";
                    return;
                }

                RefreshToolConfigurationStatus(selected);
                InspectorToolTitleText.Text = selected.InstanceName;
                InspectorToolTypeText.Text = selected.Category + " / " + selected.DisplayType;
                ToolInstanceNameTextBox.Text = selected.InstanceName;
                SelectedToolEnabledCheckBox.IsChecked = selected.IsEnabled;
                SelectedToolStatusText.Text = selected.ConfigurationStatus + " · " + selected.RunStatus + " · " + selected.ElapsedText;
                InspectorInputSummaryText.Text = selected.InputSummary;
                InspectorOutputSummaryText.Text = selected.OutputSummary;
                InspectorErrorText.Text = string.IsNullOrWhiteSpace(selected.ErrorMessage) ? "--" : selected.ErrorMessage;

                switch (selected.Kind)
                {
                    case VmToolKind.ShapeMatch:
                        ShapeInspectorPanel.Visibility = Visibility.Visible;
                        break;
                    case VmToolKind.Blob:
                        BlobInspectorPanel.Visibility = Visibility.Visible;
                        break;
                    case VmToolKind.GrayStat:
                        GrayInspectorPanel.Visibility = Visibility.Visible;
                        break;
                    case VmToolKind.EdgeMeasure:
                        EdgeInspectorPanel.Visibility = Visibility.Visible;
                        break;
                    case VmToolKind.HDevelop:
                        HDevInspectorPanel.Visibility = Visibility.Visible;
                        break;
                }
            }
            finally
            {
                inspectorUpdating = false;
            }
        }

        private void RefreshToolConfigurationStatus(VmToolInstance tool)
        {
            if (!tool.IsEnabled)
            {
                tool.ConfigurationStatus = "已停用";
                return;
            }

            switch (tool.Kind)
            {
                case VmToolKind.ShapeMatch:
                    tool.ConfigurationStatus = currentTemplateItem == null || !currentTemplateItem.HasModel
                        ? "待配置模板"
                        : (currentRoi == null ? "待配置 ROI" : "就绪");
                    break;
                case VmToolKind.HDevelop:
                    tool.ConfigurationStatus = string.IsNullOrWhiteSpace(HDevPathTextBox.Text) ? "待选择程序" : "就绪";
                    break;
                default:
                    tool.ConfigurationStatus = currentImage == null ? "等待图像" : "就绪";
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            host = HalconHost;
            imageWindow = new HWindowControl
            {
                Dock = Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(11, 18, 32)
            };

            imageWindow.MouseDown += ImageWindow_MouseDown;
            imageWindow.MouseMove += ImageWindow_MouseMove;
            imageWindow.MouseUp += ImageWindow_MouseUp;
            imageWindow.MouseDoubleClick += ImageWindow_MouseDoubleClick;
            imageWindow.MouseWheel += ImageWindow_MouseWheel;
            imageWindow.Resize += delegate { ScheduleRefreshDisplay(); };
            host.Child = imageWindow;

            LoadLayoutState();
            RunStartupDiagnostics(false);
            LogInfo("启动版本：" + VersionStamp);
            LogInfo("EXE路径：" + System.Reflection.Assembly.GetExecutingAssembly().Location);
            AppendTcpHistory("状态：TCP未连接，发送不可用。");
            RefreshResultGrid();
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopPlayback();
            StopContinuousRun();
            SaveLayoutState();
            DisposeToolOverlays();
            DisposeCurrentImage();
            DisposeCurrentRoi();
            ClearPendingRoi();

            if (currentTemplateItem != null)
            {
                currentTemplateItem.Dispose();
                currentTemplateItem = null;
            }

            resultStore.Dispose();
            tcpService.Dispose();
            roiEditor.Dispose();
            imageService.Dispose();
            hdevService.Dispose();
            logger.Dispose();
        }

        private void NewRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("新建配方", delegate
            {
                StopPlayback();
                StopContinuousRun();
                RecipeNameEditTextBox.Text = "DefaultRecipe";
                currentRecipePath = null;
                ApplyRecipe(new VisionRecipe());
                LogInfo("已新建默认配方。");
            });
        }

        private void LoadRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("加载配方", delegate
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "Vision recipe|*.json|All files|*.*",
                    InitialDirectory = recipeService.RecipeDirectory
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                VisionRecipe recipe = recipeService.LoadRecipe(dialog.FileName);
                currentRecipePath = dialog.FileName;
                ApplyRecipe(recipe);
                LogInfo("已加载配方：" + dialog.FileName);
            });
        }

        private void SaveRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("保存配方", delegate
            {
                string path = currentRecipePath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    SaveFileDialog dialog = new SaveFileDialog
                    {
                        Filter = "Vision recipe|*.json",
                        InitialDirectory = recipeService.RecipeDirectory,
                        FileName = SafeFileName(RecipeNameEditTextBox.Text, "DefaultRecipe") + ".json"
                    };
                    if (dialog.ShowDialog(this) != true)
                    {
                        return;
                    }

                    path = dialog.FileName;
                }

                VisionRecipe recipe = CaptureRecipe();
                recipeService.SaveRecipe(path, recipe);
                currentRecipePath = path;
                LogInfo("已保存配方：" + path);
                RefreshUiState();
            });
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("打开图片", delegate
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "Image files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|All files|*.*",
                    Multiselect = true
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                LoadImageQueue(dialog.FileNames);
            });
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("打开文件夹", delegate
            {
                using (Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog())
                {
                    dialog.Description = "选择图片文件夹";
                    if (dialog.ShowDialog() != Forms.DialogResult.OK)
                    {
                        return;
                    }

                    string[] files = EnumerateImages(dialog.SelectedPath).ToArray();
                    if (files.Length == 0)
                    {
                        throw new InvalidOperationException("当前文件夹没有可读取的图片。");
                    }

                    LoadImageQueue(files);
                }
            });
        }

        private void PreviousImageButton_Click(object sender, RoutedEventArgs e)
        {
            MoveImage(-1);
        }

        private void NextImageButton_Click(object sender, RoutedEventArgs e)
        {
            MoveImage(1);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("自动播放", delegate
            {
                if (imageFiles.Count <= 1)
                {
                    throw new InvalidOperationException("请先加载多张图片或文件夹。");
                }

                StopContinuousRun();
                playbackTimer.Start();
                LogInfo("自动播放已启动。");
                RefreshUiState();
            });
        }

        private void StopPlayButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
        }

        private void RunOnceButton_Click(object sender, RoutedEventArgs e)
        {
            RunInspectionCycle("手动单次");
        }

        private void RunContinuousButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("连续运行", delegate
            {
                EnsureImage();
                StopPlayback();
                isContinuousRunning = true;
                runTimer.Start();
                LogInfo("连续运行已启动。");
                RefreshUiState();
            });
        }

        private void StopRunButton_Click(object sender, RoutedEventArgs e)
        {
            StopContinuousRun();
        }

        private void ClearOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            currentMatches.Clear();
            DisposeToolOverlays();
            HeaderStatusText.Text = "叠加已清除";
            ScheduleRefreshDisplay();
            RefreshUiState();
        }

        private void SaveScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("保存截图", delegate
            {
                EnsureImage();
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|Bitmap Image|*.bmp",
                    FileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_view.png"
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                string extension = Path.GetExtension(dialog.FileName);
                string format = string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) ? "bmp" : "png";
                imageWindow.HalconWindow.DumpWindow(format, dialog.FileName);
                LogInfo("已保存当前视图截图：" + dialog.FileName);
            });
        }

        private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            RunStartupDiagnostics(true);
        }

        private void ToolSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ToolCatalogList == null)
            {
                return;
            }

            string query = ToolSearchTextBox.Text == null ? string.Empty : ToolSearchTextBox.Text.Trim().ToLowerInvariant();
            ToolCatalogList.ItemsSource = string.IsNullOrWhiteSpace(query)
                ? toolCatalog
                : toolCatalog.Where(item => item.SearchText.Contains(query)).ToList();
        }

        private void ToolCatalogList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddSelectedCatalogTool();
        }

        private void AddToolButton_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedCatalogTool();
        }

        private void AddSelectedCatalogTool()
        {
            VmToolCatalogItem catalogItem = ToolCatalogList.SelectedItem as VmToolCatalogItem;
            if (catalogItem == null)
            {
                HeaderStatusText.Text = "请先在工具箱选择工具。";
                return;
            }

            VmToolInstance existing = flowTools.FirstOrDefault(item => item.Kind == catalogItem.Kind);
            if (existing != null)
            {
                FlowToolList.SelectedItem = existing;
                FlowToolList.ScrollIntoView(existing);
                HeaderStatusText.Text = "当前版本每类工具保留一个实例，已定位到现有工具。";
                return;
            }

            VmToolInstance instance = CreateFlowTool(catalogItem.Kind, null, true, null);
            flowTools.Add(instance);
            FlowToolList.SelectedItem = instance;
            FlowToolList.ScrollIntoView(instance);
            LogInfo("已从工具箱添加：" + instance.InstanceName);
            RefreshUiState();
        }

        private void FlowToolList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshInspector();
        }

        private void MoveToolUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedTool(-1);
        }

        private void MoveToolDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedTool(1);
        }

        private void MoveSelectedTool(int direction)
        {
            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected == null)
            {
                return;
            }

            int oldIndex = flowTools.IndexOf(selected);
            int newIndex = oldIndex + direction;
            if (newIndex < 0 || newIndex >= flowTools.Count)
            {
                return;
            }

            flowTools.Move(oldIndex, newIndex);
            FlowToolList.SelectedItem = selected;
            LogInfo("流程顺序已调整：" + selected.InstanceName + " -> " + (newIndex + 1));
        }

        private void DeleteToolButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected == null)
            {
                return;
            }

            int index = flowTools.IndexOf(selected);
            flowTools.Remove(selected);
            if (flowTools.Count > 0)
            {
                FlowToolList.SelectedIndex = Math.Min(index, flowTools.Count - 1);
            }
            else
            {
                RefreshInspector();
            }

            LogInfo("已从流程删除：" + selected.InstanceName);
            RefreshUiState();
        }

        private void RenameToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlowToolList.SelectedItem == null)
            {
                return;
            }

            ToolInstanceNameTextBox.Focus();
            ToolInstanceNameTextBox.SelectAll();
            HeaderStatusText.Text = "在 Inspector 中输入实例名称，离开输入框后生效。";
        }

        private void ToolInstanceNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected == null)
            {
                return;
            }

            string name = ToolInstanceNameTextBox.Text == null ? string.Empty : ToolInstanceNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ToolInstanceNameTextBox.Text = selected.InstanceName;
                return;
            }

            if (flowTools.Any(item => item != selected && string.Equals(item.InstanceName, name, StringComparison.OrdinalIgnoreCase)))
            {
                HeaderStatusText.Text = "工具实例名称不能重复。";
                ToolInstanceNameTextBox.Text = selected.InstanceName;
                return;
            }

            selected.InstanceName = name;
            LogInfo("工具实例已重命名：" + name);
        }

        private void SelectedToolEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected != null)
            {
                selected.IsEnabled = SelectedToolEnabledCheckBox.IsChecked == true;
                RefreshUiState();
            }
        }

        private void RunCurrentToolButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected == null)
            {
                HeaderStatusText.Text = "请先选择流程工具。";
                return;
            }

            RunStandaloneTool(selected, "运行当前");
        }

        private void SelectShapeToolButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance shapeTool = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ShapeMatch);
            if (shapeTool == null)
            {
                shapeTool = CreateFlowTool(VmToolKind.ShapeMatch, null, true, null);
                flowTools.Insert(0, shapeTool);
            }

            FlowToolList.SelectedItem = shapeTool;
            RightTabs.SelectedIndex = 0;
        }

        private void RectangleRoiButton_Click(object sender, RoutedEventArgs e)
        {
            SetRoiTool(VisionTool.RectangleRoi, "矩形 ROI：按住左键拖拽绘制，松开后点击确认。");
        }

        private void CircleRoiButton_Click(object sender, RoutedEventArgs e)
        {
            SetRoiTool(VisionTool.CircleRoi, "圆形 ROI：从圆心按住左键向外拖拽，松开后点击确认。");
        }

        private void PolygonRoiButton_Click(object sender, RoutedEventArgs e)
        {
            SetRoiTool(VisionTool.PolygonRoi, "多边形 ROI：左键加点，双击或点击确认 ROI 结束。");
        }

        private void ConfirmRoiButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("确认ROI", delegate
            {
                RoiData completed = null;
                if (roiEditor.Tool == VisionTool.PolygonRoi && roiEditor.IsPolygonDrawing)
                {
                    completed = roiEditor.CompletePolygon();
                }

                if (completed != null)
                {
                    SetPendingRoi(completed);
                }

                if (pendingRoi == null)
                {
                    throw new InvalidOperationException("请先在图像上绘制 ROI。");
                }

                if (currentRoi != null)
                {
                    currentRoi.Dispose();
                }

                currentRoi = pendingRoi.Clone();
                ClearPendingRoi();
                currentMatches.Clear();
                DisposeToolOverlays();
                HeaderStatusText.Text = "ROI 已确认，可以训练模板或运行检测。";
                LogInfo("ROI 已确认：" + currentRoi.DisplayText);
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private void ClearRoiButton_Click(object sender, RoutedEventArgs e)
        {
            DisposeCurrentRoi();
            ClearPendingRoi();
            currentMatches.Clear();
            roiEditor.Cancel();
            HeaderStatusText.Text = "ROI 已清除";
            LogInfo("ROI 已清除。");
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void FitImageButton_Click(object sender, RoutedEventArgs e)
        {
            viewport.Fit();
            ScheduleRefreshDisplay();
            RefreshUiState();
        }

        private void TemplateSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("训练模板", delegate
            {
                EnsureImage();
                if (currentRoi == null)
                {
                    throw new InvalidOperationException("请先绘制并确认 ROI。");
                }

                TemplateMatchOptions options = currentTemplateItem == null
                    ? new TemplateMatchOptions()
                    : TemplateDefinition.CloneOptions(currentTemplateItem.Options);

                TemplateCreateWindow dialog = new TemplateCreateWindow(currentImage, currentRoi, options)
                {
                    Owner = this
                };
                if (dialog.ShowDialog() != true || dialog.ResultDefinition == null)
                {
                    HeaderStatusText.Text = "模板训练已取消。";
                    return;
                }

                TemplateDefinition definition = dialog.ResultDefinition;
                ReplaceTemplate(new TemplateItem
                {
                    Name = string.IsNullOrWhiteSpace(definition.TemplateName) ? "Template_001" : definition.TemplateName,
                    TemplateRoi = definition.TemplateRoi == null ? null : definition.TemplateRoi.Clone(),
                    TrainingMask = TemplateDefinition.CloneRegion(definition.TrainingMask),
                    DisplayFrame = definition.DisplayFrame == null ? null : definition.DisplayFrame.Clone(),
                    Options = TemplateDefinition.CloneOptions(definition.Options)
                });

                currentTemplateItem.Service.Train(currentImage, definition);
                currentMatches.Clear();
                HeaderStatusText.Text = "模板已训练，可以执行匹配。";
                LogInfo("模板已训练：" + currentTemplateItem.Name);
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("保存模板", delegate
            {
                if (currentTemplateItem == null || !currentTemplateItem.HasModel)
                {
                    throw new InvalidOperationException("当前没有可保存的模板。");
                }

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "HALCON shape model|*.shm",
                    FileName = SafeFileName(currentTemplateItem.Name, "Template_001") + ".shm"
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                currentTemplateItem.Service.Save(dialog.FileName);
                LogInfo("模板已保存：" + dialog.FileName);
                RefreshUiState();
            });
        }

        private void LoadTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("加载模板", delegate
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "HALCON shape model|*.shm|All files|*.*"
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                TemplateItem item = new TemplateItem { Name = Path.GetFileNameWithoutExtension(dialog.FileName) };
                item.Service.Load(dialog.FileName);
                TemplateDefinition definition = item.Service.Definition;
                if (definition != null)
                {
                    item.Options = TemplateDefinition.CloneOptions(definition.Options);
                    item.TemplateRoi = definition.TemplateRoi == null ? null : definition.TemplateRoi.Clone();
                    item.DisplayFrame = definition.DisplayFrame == null ? null : definition.DisplayFrame.Clone();
                    item.TrainingMask = TemplateDefinition.CloneRegion(definition.TrainingMask);
                }

                ReplaceTemplate(item);
                currentMatches.Clear();
                LogInfo("模板已加载：" + dialog.FileName);
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private void RunMatchButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance shapeTool = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ShapeMatch);
            if (shapeTool == null)
            {
                shapeTool = CreateFlowTool(VmToolKind.ShapeMatch, null, true, null);
                flowTools.Insert(0, shapeTool);
            }

            FlowToolList.SelectedItem = shapeTool;
            RunStandaloneTool(shapeTool, "模板匹配独立运行");
        }

        private void OverlayOptionChanged(object sender, RoutedEventArgs e)
        {
            ScheduleRefreshDisplay();
        }

        private void ToolConfigChanged(object sender, RoutedEventArgs e)
        {
            RefreshUiState();
        }

        private void BrowseHDevButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "HDevelop program|*.hdev;*.hdvp|All files|*.*"
            };
            if (dialog.ShowDialog(this) == true)
            {
                HDevPathTextBox.Text = dialog.FileName;
            }
        }

        private void TcpModeChanged(object sender, RoutedEventArgs e)
        {
            if (uiReady)
            {
                RefreshUiState();
            }
        }

        private void TcpConnectButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("TCP连接", delegate
            {
                int port = ReadTcpPort();
                AppendTcpHistory("状态：客户端连接中 " + TcpIpTextBox.Text.Trim() + ":" + port);
                tcpService.ConnectClient(TcpIpTextBox.Text.Trim(), port, ResolveTcpEncoding());
                RefreshUiState();
            });
        }

        private void TcpDisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            tcpService.Stop();
            AppendTcpHistory("状态：TCP已断开。");
            RefreshUiState();
        }

        private void TcpStartServerButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("启动监听", delegate
            {
                int port = ReadTcpPort();
                AppendTcpHistory("状态：服务端监听 " + TcpIpTextBox.Text.Trim() + ":" + port);
                tcpService.StartServer(TcpIpTextBox.Text.Trim(), port, ResolveTcpEncoding());
                RefreshUiState();
            });
        }

        private void TcpStopServerButton_Click(object sender, RoutedEventArgs e)
        {
            tcpService.Stop();
            AppendTcpHistory("状态：服务端监听已停止。");
            RefreshUiState();
        }

        private void TcpSendButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("发送文本", delegate
            {
                SendTcpPayload(TcpSendTextBox.Text ?? string.Empty, "手动发送");
            });
        }

        private void SendLastMatchButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("发送结果", delegate
            {
                if (string.IsNullOrWhiteSpace(lastResultPayload))
                {
                    throw new InvalidOperationException("当前没有可发送的检测结果。");
                }

                SendTcpPayload(lastResultPayload, "发送结果");
            });
        }

        private void ReviewResultButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("回看结果", delegate
            {
                InspectionRecord record = ResultsDataGrid.SelectedItem as InspectionRecord;
                if (record == null)
                {
                    throw new InvalidOperationException("请先选择一条结果记录。");
                }

                if (record.ImageSnapshot != null)
                {
                    ReplaceCurrentImage(record.ImageSnapshot.CopyImage(), true);
                }

                if (record.Roi != null)
                {
                    DisposeCurrentRoi();
                    currentRoi = record.Roi.Clone();
                }

                HeaderStatusText.Text = "已回看记录 #" + record.Id + "：" + record.ResultCode;
                LogInfo("回看结果记录 #" + record.Id);
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            ExportResults("CSV");
        }

        private void ExportXlsxButton_Click(object sender, RoutedEventArgs e)
        {
            ExportResults("XLSX");
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            resultStore.Clear();
            runtimeStatistics.Reset();
            LogInfo("结果记录已清空。");
            RefreshUiState();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            BottomLogTextBox.Clear();
            AlarmTextBox.Clear();
        }

        private void OpenLogDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDirectory(logger.LogDirectory);
        }

        private void OpenRecipeDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDirectory(recipeService.RecipeDirectory);
        }

        private void RightTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScheduleRefreshDisplay();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            RunUiAction("拖拽加载", delegate
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    return;
                }

                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> files = new List<string>();
                foreach (string path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        files.AddRange(EnumerateImages(path));
                    }
                    else if (IsImageFile(path))
                    {
                        files.Add(path);
                    }
                }

                if (files.Count == 0)
                {
                    throw new InvalidOperationException("拖拽内容中没有可读取图片。");
                }

                LoadImageQueue(files);
            });
        }

        private void ImageWindow_MouseDown(object sender, Forms.MouseEventArgs e)
        {
            if (currentImage == null)
            {
                return;
            }

            if (e.Button == Forms.MouseButtons.Right)
            {
                isPanning = true;
                lastPanPoint = e.Location;
                imageWindow.Capture = true;
                HeaderStatusText.Text = "右键平移中。";
                return;
            }

            if (e.Button != Forms.MouseButtons.Left)
            {
                return;
            }

            PointF imagePoint = viewport.WindowToImage(e.Location, imageWindow);
            if (roiEditor.Tool == VisionTool.PolygonRoi)
            {
                roiEditor.AddPolygonPoint(imagePoint);
                HeaderStatusText.Text = "多边形 ROI 绘制中：双击或点击确认 ROI 完成。";
                ScheduleRefreshDisplay();
                return;
            }

            if (roiEditor.Begin(imagePoint))
            {
                HeaderStatusText.Text = "ROI 绘制中，松开鼠标后可确认。";
                ScheduleRefreshDisplay();
            }
        }

        private void ImageWindow_MouseMove(object sender, Forms.MouseEventArgs e)
        {
            if (currentImage == null)
            {
                return;
            }

            PointF imagePoint = viewport.WindowToImage(e.Location, imageWindow);
            MouseStatusText.Text = string.Format(CultureInfo.InvariantCulture, "坐标：R {0:F1}, C {1:F1}", imagePoint.Y, imagePoint.X);

            if (isPanning)
            {
                viewport.PanByWindowDelta(e.X - lastPanPoint.X, e.Y - lastPanPoint.Y, imageWindow);
                lastPanPoint = e.Location;
                ScheduleRefreshDisplay();
                RefreshUiState();
                return;
            }

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
            if (e.Button == Forms.MouseButtons.Right)
            {
                isPanning = false;
                imageWindow.Capture = false;
                HeaderStatusText.Text = "平移完成。";
                return;
            }

            if (currentImage == null || e.Button != Forms.MouseButtons.Left || roiEditor.Tool == VisionTool.PolygonRoi)
            {
                return;
            }

            PointF imagePoint = viewport.WindowToImage(e.Location, imageWindow);
            RoiData roi = roiEditor.Complete(imagePoint);
            if (roi != null)
            {
                SetPendingRoi(roi);
                HeaderStatusText.Text = "ROI 待确认，请点击“确认ROI”。";
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
        }

        private void ImageWindow_MouseDoubleClick(object sender, Forms.MouseEventArgs e)
        {
            if (currentImage == null)
            {
                return;
            }

            if (roiEditor.Tool == VisionTool.PolygonRoi && e.Button == Forms.MouseButtons.Left)
            {
                RoiData roi = roiEditor.CompletePolygon();
                if (roi != null)
                {
                    SetPendingRoi(roi);
                    HeaderStatusText.Text = "多边形 ROI 待确认，请点击“确认ROI”。";
                    RefreshUiState();
                    ScheduleRefreshDisplay();
                }
                return;
            }

            viewport.Fit();
            HeaderStatusText.Text = "图像已适应窗口。";
            ScheduleRefreshDisplay();
            RefreshUiState();
        }

        private void ImageWindow_MouseWheel(object sender, Forms.MouseEventArgs e)
        {
            if (currentImage == null)
            {
                return;
            }

            viewport.ZoomAt(e.Location, imageWindow, e.Delta);
            ScheduleRefreshDisplay();
            RefreshUiState();
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (imageFiles.Count <= 1)
            {
                StopPlayback();
                return;
            }

            imageIndex = (imageIndex + 1) % imageFiles.Count;
            LoadImage(imageFiles[imageIndex], false);
        }

        private void RunTimer_Tick(object sender, EventArgs e)
        {
            if (!isContinuousRunning)
            {
                return;
            }

            RunInspectionCycle("连续运行");
            if (imageFiles.Count > 1)
            {
                imageIndex = (imageIndex + 1) % imageFiles.Count;
                LoadImage(imageFiles[imageIndex], false);
            }
        }

        private void LoadImageQueue(IEnumerable<string> files)
        {
            StopPlayback();
            StopContinuousRun();
            imageFiles.Clear();
            imageFiles.AddRange(files.Where(IsImageFile).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            if (imageFiles.Count == 0)
            {
                throw new InvalidOperationException("没有可读取的图片。");
            }

            imageIndex = 0;
            LoadImage(imageFiles[0], true);
            LogInfo("图片队列已加载：" + imageFiles.Count + " 张。");
        }

        private void MoveImage(int direction)
        {
            if (imageFiles.Count == 0)
            {
                return;
            }

            StopPlayback();
            imageIndex = Math.Max(0, Math.Min(imageFiles.Count - 1, imageIndex + direction));
            LoadImage(imageFiles[imageIndex], true);
        }

        private void LoadImage(string path, bool log)
        {
            HImage image = imageService.ReadImage(path);
            HImage original = image.CopyImage();
            DisposeCurrentImage();
            currentImage = image;
            originalImage = original;
            currentImagePath = path;
            currentMatches.Clear();
            DisposeToolOverlays();
            SetViewportFromImage(currentImage);
            HeaderStatusText.Text = "已打开：" + Path.GetFileName(path);
            if (log)
            {
                LogInfo("打开图片：" + path);
            }

            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void ReplaceCurrentImage(HImage image, bool replaceOriginal)
        {
            if (image == null)
            {
                return;
            }

            if (currentImage != null)
            {
                currentImage.Dispose();
            }

            currentImage = image;
            if (replaceOriginal)
            {
                if (originalImage != null)
                {
                    originalImage.Dispose();
                }

                originalImage = image.CopyImage();
            }

            currentMatches.Clear();
            DisposeToolOverlays();
            SetViewportFromImage(currentImage);
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void SetViewportFromImage(HImage image)
        {
            HTuple width;
            HTuple height;
            HOperatorSet.GetImageSize(image, out width, out height);
            viewport.SetImageSize(width.I, height.I);
        }

        private void RunInspectionCycle(string source)
        {
            RunUiAction(source, delegate
            {
                List<VmToolInstance> enabledTools = flowTools.Where(item => item.IsEnabled).ToList();
                if (enabledTools.Count == 0)
                {
                    throw new InvalidOperationException("流程中没有启用的工具。请从工具箱添加并启用工具。");
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                currentMatches.Clear();
                DisposeToolOverlays();

                List<InspectionRecord> records = new List<InspectionRecord>();
                foreach (VmToolInstance tool in enabledTools)
                {
                    records.Add(ExecuteFlowTool(tool, source));
                }

                bool ok = records.All(record => string.Equals(record.ResultCode, "OK", StringComparison.OrdinalIgnoreCase));
                string resultCode = ok ? "OK" : "NG";
                string message = string.Join(" | ", records.Select(item => item.InspectionType + ":" + item.Message));
                stopwatch.Stop();

                runtimeStatistics.Record(resultCode, stopwatch.Elapsed.TotalMilliseconds, message);
                lastResultPayload = BuildJsonResultPayload(resultCode, stopwatch.Elapsed.TotalMilliseconds, records);
                LastMessageText.Text = "最新外发结果：" + lastResultPayload;
                HeaderStatusText.Text = string.Format(CultureInfo.InvariantCulture, "检测完成：{0}，耗时 {1:F1} ms", resultCode, stopwatch.Elapsed.TotalMilliseconds);
                LogInfo(HeaderStatusText.Text);
                AutoSendResultIfNeeded();
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private void RunStandaloneTool(VmToolInstance tool, string source)
        {
            RunUiAction(source, delegate
            {
                if (tool == null)
                {
                    throw new InvalidOperationException("请先选择流程工具。");
                }

                InspectionRecord record = ExecuteFlowTool(tool, source);
                lastResultPayload = BuildJsonResultPayload(record.ResultCode, tool.ElapsedMilliseconds, new[] { record });
                LastMessageText.Text = "最新外发结果：" + lastResultPayload;
                HeaderStatusText.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}：{1}，耗时 {2:F1} ms",
                    tool.InstanceName,
                    record.ResultCode,
                    tool.ElapsedMilliseconds);
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private InspectionRecord ExecuteFlowTool(VmToolInstance tool, string source)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            tool.RunStatus = "运行中";
            tool.ResultCode = "--";
            tool.ErrorMessage = string.Empty;
            try
            {
                InspectionRecord record;
                switch (tool.Kind)
                {
                    case VmToolKind.ShapeMatch:
                        EnsureImage();
                        record = RunShapeMatchTool(source, false);
                        tool.InputSummary = "Image + SearchROI + ShapeModel";
                        tool.OutputSummary = currentMatches.Count == 0
                            ? "Matches=0"
                            : string.Format(CultureInfo.InvariantCulture, "Matches={0}, Best={1:F3}", currentMatches.Count, currentMatches.Max(item => item.Score));
                        break;
                    case VmToolKind.Blob:
                        EnsureImage();
                        record = RunBlobTool();
                        tool.InputSummary = currentRoi == null ? "Image" : "Image + ROI";
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.GrayStat:
                        EnsureImage();
                        record = RunGrayStatTool();
                        tool.InputSummary = currentRoi == null ? "Image" : "Image + ROI";
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.EdgeMeasure:
                        EnsureImage();
                        record = RunEdgeMeasureTool();
                        tool.InputSummary = currentRoi == null ? "Image" : "Image + ROI";
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.HDevelop:
                        EnsureImage();
                        record = RunHDevTool();
                        tool.InputSummary = "Image + ROI + HDevelop";
                        tool.OutputSummary = record.Message;
                        break;
                    default:
                        throw new NotSupportedException("不支持的工具类型：" + tool.Kind);
                }

                tool.ResultCode = record.ResultCode;
                tool.RunStatus = string.Equals(record.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) ? "完成" : "异常";
                return record;
            }
            catch (Exception ex)
            {
                tool.ResultCode = "NG";
                tool.RunStatus = "失败";
                tool.ErrorMessage = ex.Message;
                tool.OutputSummary = "失败：" + ex.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                tool.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                RefreshInspector();
            }
        }

        private InspectionRecord RunShapeMatchTool(string source, bool throwOnDisabled)
        {
            if (throwOnDisabled)
            {
                return RunUiActionWithResult("模板匹配", delegate { return ExecuteShapeMatch(source); });
            }

            return ExecuteShapeMatch(source);
        }

        private InspectionRecord ExecuteShapeMatch(string source)
        {
            EnsureImage();
            if (currentRoi == null)
            {
                throw new InvalidOperationException("请先确认搜索 ROI。");
            }

            if (currentTemplateItem == null || !currentTemplateItem.HasModel)
            {
                throw new InvalidOperationException("请先训练或加载模板。");
            }

            List<ShapeMatchResult> matches = currentTemplateItem.Service.Match(currentImage, currentRoi, currentTemplateItem.Options);
            currentMatches.Clear();
            currentMatches.AddRange(matches);

            ShapeMatchResult best = matches.OrderByDescending(item => item.Score).FirstOrDefault();
            InspectionRecord record = CreateRecord("ShapeModel", best == null ? "NG" : "OK", best == null ? 0 : best.Score, matches.Count == 0 ? "未匹配到目标" : "匹配数量：" + matches.Count);
            if (best != null)
            {
                record.MatchRow = best.Row;
                record.MatchColumn = best.Column;
                record.MatchAngle = best.AngleDeg;
            }

            record.TemplatePath = currentTemplateItem.TemplatePath;
            resultStore.Add(record);
            MatchResultText.Text = string.Format(CultureInfo.InvariantCulture, "结果：{0}，数量 {1}，最高分 {2:F3}", record.ResultCode, matches.Count, record.Score);
            lastResultPayload = BuildKeyValueMatchPayload(currentTemplateItem.Name, matches);
            LastMessageText.Text = "最新外发结果：" + lastResultPayload;
            LogInfo("模板匹配完成：" + currentTemplateItem.Name + "，结果数：" + matches.Count);
            RefreshUiState();
            ScheduleRefreshDisplay();
            return record;
        }

        private InspectionRecord RunBlobTool()
        {
            double minGray = ReadDouble(BlobMinGrayTextBox, "Blob灰度下限");
            double maxGray = ReadDouble(BlobMaxGrayTextBox, "Blob灰度上限");
            double minArea = ReadDouble(BlobMinAreaTextBox, "Blob最小面积");

            HObject thresholdRegion = null;
            HObject clippedRegion = null;
            HObject connectedRegion = null;
            HObject selectedRegion = null;
            HImage gray = null;
            try
            {
                gray = CreateGrayImage(currentImage);
                HOperatorSet.Threshold(gray, out thresholdRegion, minGray, maxGray);
                HObject sourceRegion = thresholdRegion;
                if (currentRoi != null)
                {
                    HOperatorSet.Intersection(thresholdRegion, currentRoi.Region, out clippedRegion);
                    sourceRegion = clippedRegion;
                }

                HOperatorSet.Connection(sourceRegion, out connectedRegion);
                HOperatorSet.SelectShape(connectedRegion, out selectedRegion, "area", "and", minArea, 999999999.0);

                HTuple area;
                HTuple row;
                HTuple column;
                HOperatorSet.AreaCenter(selectedRegion, out area, out row, out column);
                double totalArea = SumTuple(area);
                int count = area == null ? 0 : area.Length;
                ReplaceToolOverlayRegion(selectedRegion);
                selectedRegion = null;

                InspectionRecord record = CreateRecord("Blob", count > 0 ? "OK" : "NG", totalArea, count > 0 ? "Blob数量：" + count : "未找到满足面积的 Blob");
                resultStore.Add(record);
                LogInfo(string.Format(CultureInfo.InvariantCulture, "Blob完成：数量 {0}，面积 {1:F1}", count, totalArea));
                return record;
            }
            finally
            {
                DisposeObject(selectedRegion);
                DisposeObject(connectedRegion);
                DisposeObject(clippedRegion);
                DisposeObject(thresholdRegion);
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private InspectionRecord RunGrayStatTool()
        {
            double min = ReadDouble(GrayMinTextBox, "灰度下限");
            double max = ReadDouble(GrayMaxTextBox, "灰度上限");
            HImage gray = null;
            try
            {
                gray = CreateGrayImage(currentImage);
                HTuple mean;
                HTuple deviation;
                HRegion region = currentRoi == null ? new HRegion(0.0, 0.0, (double)viewport.ImageHeight - 1.0, (double)viewport.ImageWidth - 1.0) : currentRoi.Region;
                HOperatorSet.Intensity(region, gray, out mean, out deviation);
                double value = mean.D;
                bool ok = value >= min && value <= max;
                InspectionRecord record = CreateRecord("GrayStat", ok ? "OK" : "NG", value, string.Format(CultureInfo.InvariantCulture, "Mean={0:F2}, Dev={1:F2}", value, deviation.D));
                resultStore.Add(record);
                LogInfo("灰度统计完成：" + record.Message);
                if (currentRoi == null)
                {
                    region.Dispose();
                }

                return record;
            }
            finally
            {
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private InspectionRecord RunEdgeMeasureTool()
        {
            double threshold = ReadDouble(EdgeThresholdTextBox, "边缘阈值");
            HImage gray = null;
            HObject reduced = null;
            HObject edges = null;
            try
            {
                gray = CreateGrayImage(currentImage);
                HObject edgeInput = gray;
                if (currentRoi != null)
                {
                    HOperatorSet.ReduceDomain(gray, currentRoi.Region, out reduced);
                    edgeInput = reduced;
                }

                HOperatorSet.EdgesSubPix(edgeInput, out edges, "canny", 1.0, threshold, threshold * 2.0);
                HTuple lengths;
                HOperatorSet.LengthXld(edges, out lengths);
                double totalLength = SumTuple(lengths);
                ReplaceToolOverlayContours(edges);
                edges = null;

                InspectionRecord record = CreateRecord("EdgeMeasure", totalLength > 0 ? "OK" : "NG", totalLength, string.Format(CultureInfo.InvariantCulture, "边缘总长度={0:F1}", totalLength));
                resultStore.Add(record);
                LogInfo("边缘测量完成：" + record.Message);
                return record;
            }
            finally
            {
                DisposeObject(edges);
                DisposeObject(reduced);
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private InspectionRecord RunHDevTool()
        {
            string path = HDevPathTextBox.Text == null ? string.Empty : HDevPathTextBox.Text.Trim();
            string procedure = string.IsNullOrWhiteSpace(HDevProcedureTextBox.Text) ? "RunInspection" : HDevProcedureTextBox.Text.Trim();
            HDevInspectionResult result = hdevService.RunInspection(path, procedure, currentImage, currentRoi);
            InspectionRecord record = CreateRecord("HDevelop", string.IsNullOrWhiteSpace(result.ResultCode) ? "OK" : result.ResultCode, result.Score, result.Message);
            resultStore.Add(record);
            LogInfo("HDevelop 执行完成：" + record.Message);
            return record;
        }

        private InspectionRecord CreateRecord(string type, string resultCode, double score, string message)
        {
            return new InspectionRecord
            {
                Timestamp = DateTime.Now,
                ImageSource = string.IsNullOrWhiteSpace(currentImagePath) ? "Camera/Memory" : Path.GetFileName(currentImagePath),
                InspectionType = type,
                Roi = currentRoi == null ? null : currentRoi.Clone(),
                ResultCode = resultCode,
                Score = score,
                Message = message,
                ImageSnapshot = currentImage == null ? null : currentImage.CopyImage()
            };
        }

        private HImage CreateGrayImage(HImage source)
        {
            return imageService.ToGray(source);
        }

        private void ReplaceToolOverlayRegion(HObject region)
        {
            DisposeObject(toolOverlayRegion);
            toolOverlayRegion = region;
        }

        private void ReplaceToolOverlayContours(HObject contours)
        {
            DisposeObject(toolOverlayContours);
            toolOverlayContours = contours;
        }

        private void RefreshDisplay()
        {
            if (imageWindow == null || imageWindow.HalconWindow == null)
            {
                return;
            }

            imageWindow.HalconWindow.ClearWindow();
            if (currentImage == null)
            {
                return;
            }

            viewport.Apply(imageWindow.HalconWindow);
            currentImage.DispImage(imageWindow.HalconWindow);

            if (toolOverlayRegion != null)
            {
                imageWindow.HalconWindow.SetColor("yellow");
                imageWindow.HalconWindow.SetDraw("margin");
                imageWindow.HalconWindow.SetLineWidth(2);
                imageWindow.HalconWindow.DispObj(toolOverlayRegion);
            }

            if (toolOverlayContours != null)
            {
                imageWindow.HalconWindow.SetColor("cyan");
                imageWindow.HalconWindow.SetLineWidth(2);
                imageWindow.HalconWindow.DispObj(toolOverlayContours);
            }

            RoiData templateRoi = currentTemplateItem == null ? null : currentTemplateItem.TemplateRoi;
            RoiData confirmedBoundary = currentMatches.Count == 0 ? (currentRoi ?? templateRoi) : templateRoi;
            ShapeTemplateService service = currentTemplateItem == null ? null : currentTemplateItem.Service;
            overlayRenderer.Draw(
                imageWindow.HalconWindow,
                currentRoi,
                confirmedBoundary,
                pendingRoi ?? roiEditor.PreviewRoi,
                null,
                currentTemplateItem == null ? null : currentTemplateItem.DisplayFrame,
                currentMatches,
                service,
                false,
                confirmedBoundary != null && currentMatches.Count == 0,
                ShowResultFrameCheckBox.IsChecked == true);
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

        private void RefreshUiState()
        {
            if (!uiReady || PreviousImageButton == null)
            {
                return;
            }

            bool hasImage = currentImage != null;
            bool hasRoi = currentRoi != null;
            bool hasPendingRoi = pendingRoi != null || roiEditor.IsPolygonDrawing || roiEditor.PreviewRoi != null;
            bool hasTemplate = currentTemplateItem != null && currentTemplateItem.HasModel;
            bool tcpRunning = tcpService.IsRunning;
            bool canSend = tcpService.CanSend;
            bool clientMode = TcpClientModeRadio.IsChecked == true;
            bool hasEnabledTools = flowTools.Any(item => item.IsEnabled);

            foreach (VmToolInstance tool in flowTools)
            {
                RefreshToolConfigurationStatus(tool);
            }
            SyncLegacyToolChecksFromFlow();

            PreviousImageButton.IsEnabled = imageFiles.Count > 1 && imageIndex > 0 && !playbackTimer.IsEnabled;
            NextImageButton.IsEnabled = imageFiles.Count > 1 && imageIndex < imageFiles.Count - 1 && !playbackTimer.IsEnabled;
            PlayButton.IsEnabled = imageFiles.Count > 1 && !playbackTimer.IsEnabled && !isContinuousRunning;
            StopPlayButton.IsEnabled = playbackTimer.IsEnabled;
            RunOnceButton.IsEnabled = hasImage && hasEnabledTools && !isContinuousRunning;
            RunContinuousButton.IsEnabled = hasImage && hasEnabledTools && !isContinuousRunning;
            StopRunButton.IsEnabled = isContinuousRunning;
            ClearOverlayButton.IsEnabled = hasImage;
            SaveScreenshotButton.IsEnabled = hasImage;

            RectangleRoiButton.IsEnabled = hasImage && !isContinuousRunning;
            CircleRoiButton.IsEnabled = hasImage && !isContinuousRunning;
            PolygonRoiButton.IsEnabled = hasImage && !isContinuousRunning;
            ClearRoiButton.IsEnabled = hasRoi || hasPendingRoi;
            ConfirmRoiButton.IsEnabled = hasPendingRoi;
            FitImageButton.IsEnabled = hasImage;
            TemplateSettingsButton.IsEnabled = hasImage && hasRoi && !isContinuousRunning;
            SaveTemplateButton.IsEnabled = hasTemplate;
            LoadTemplateButton.IsEnabled = !isContinuousRunning;
            RunMatchButton.IsEnabled = hasImage && hasRoi && hasTemplate && !isContinuousRunning;

            TcpConnectButton.Visibility = clientMode ? Visibility.Visible : Visibility.Collapsed;
            TcpDisconnectButton.Visibility = clientMode ? Visibility.Visible : Visibility.Collapsed;
            TcpStartServerButton.Visibility = clientMode ? Visibility.Collapsed : Visibility.Visible;
            TcpStopServerButton.Visibility = clientMode ? Visibility.Collapsed : Visibility.Visible;
            TcpIpLabel.Text = clientMode ? "远端IP" : "监听IP";
            TcpConnectButton.IsEnabled = clientMode && !tcpRunning;
            TcpDisconnectButton.IsEnabled = clientMode && tcpRunning;
            TcpStartServerButton.IsEnabled = !clientMode && !tcpRunning;
            TcpStopServerButton.IsEnabled = !clientMode && tcpRunning;
            TcpIpTextBox.IsEnabled = !tcpRunning;
            TcpPortTextBox.IsEnabled = !tcpRunning;
            TcpEncodingCombo.IsEnabled = !tcpRunning;
            TcpSendButton.IsEnabled = canSend;
            SendLastMatchButton.IsEnabled = canSend && !string.IsNullOrWhiteSpace(lastResultPayload);

            TcpStatusText.Text = canSend
                ? "TCP已连接，可发送。"
                : (tcpRunning ? (clientMode ? "客户端连接中，发送暂不可用。" : "服务端监听中，等待客户端接入。") : "未连接，发送不可用。");

            ImageIndexText.Text = imageFiles.Count == 0 ? "队列：0/0" : string.Format("队列：{0}/{1}", imageIndex + 1, imageFiles.Count);
            CurrentFileText.Text = "当前文件：" + (string.IsNullOrWhiteSpace(currentImagePath) ? "--" : currentImagePath);
            OverlayStatusText.Text = hasImage
                ? string.Format("{0}  {1}x{2}", string.IsNullOrWhiteSpace(currentImagePath) ? "图像" : Path.GetFileName(currentImagePath), viewport.ImageWidth, viewport.ImageHeight)
                : "图像区：拖拽图片或打开文件夹开始";

            RoiStatusText.Text = hasPendingRoi ? "ROI：待确认" : (hasRoi ? "ROI：已确认，" + currentRoi.DisplayText : "ROI：未设置");
            TemplateStatusText.Text = hasTemplate ? "模板：已训练/加载，" + currentTemplateItem.Name : "模板：未训练";
            MatchResultText.Text = currentMatches.Count == 0 ? MatchResultText.Text : MatchResultText.Text;

            ModeStatusText.Text = isContinuousRunning ? "模式：连续运行" : (playbackTimer.IsEnabled ? "模式：图片播放" : "模式：手动调试");
            RunModeText.Text = ModeStatusText.Text;
            ImageStatusText.Text = hasImage ? string.Format("图像：{0}x{1}", viewport.ImageWidth, viewport.ImageHeight) : "图像：--";
            ZoomStatusText.Text = hasImage ? viewport.ZoomText.Replace("Zoom", "缩放") : "缩放：--";
            RoiStatusBarText.Text = hasRoi ? "ROI：" + currentRoi.ShapeType : "ROI：未设置";
            TemplateStatusBarText.Text = hasTemplate ? "模板：" + currentTemplateItem.Name : "模板：未训练";
            TcpStatusBarText.Text = canSend ? "TCP：可发送" : "TCP：未连接";

            MetricOkText.Text = runtimeStatistics.OkCount.ToString(CultureInfo.InvariantCulture);
            MetricNgText.Text = runtimeStatistics.NgCount.ToString(CultureInfo.InvariantCulture);
            MetricYieldText.Text = runtimeStatistics.TotalCount == 0 ? "--" : runtimeStatistics.YieldRate.ToString("F1", CultureInfo.InvariantCulture) + "%";
            MetricCycleText.Text = runtimeStatistics.LastCycleMilliseconds <= 0 ? "--" : runtimeStatistics.LastCycleMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + "ms";

            RecipeNameText.Text = "配方：" + (string.IsNullOrWhiteSpace(RecipeNameEditTextBox.Text) ? "未命名" : RecipeNameEditTextBox.Text);
            RecipePathText.Text = "路径：" + (string.IsNullOrWhiteSpace(currentRecipePath) ? "--" : currentRecipePath);
            RefreshInspector();
        }

        private void SetRoiTool(VisionTool tool, string hint)
        {
            EnsureImage();
            roiEditor.Tool = tool;
            ClearPendingRoi();
            roiEditor.Cancel();
            HeaderStatusText.Text = hint;
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void SetPendingRoi(RoiData roi)
        {
            ClearPendingRoi();
            pendingRoi = roi;
        }

        private void ClearPendingRoi()
        {
            if (pendingRoi != null)
            {
                pendingRoi.Dispose();
                pendingRoi = null;
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

        private void DisposeCurrentImage()
        {
            if (currentImage != null)
            {
                currentImage.Dispose();
                currentImage = null;
            }

            if (originalImage != null)
            {
                originalImage.Dispose();
                originalImage = null;
            }
        }

        private void DisposeToolOverlays()
        {
            DisposeObject(toolOverlayRegion);
            DisposeObject(toolOverlayContours);
            toolOverlayRegion = null;
            toolOverlayContours = null;
        }

        private static void DisposeObject(HObject obj)
        {
            if (obj != null)
            {
                obj.Dispose();
            }
        }

        private void ReplaceTemplate(TemplateItem item)
        {
            if (currentTemplateItem != null)
            {
                currentTemplateItem.Dispose();
            }

            currentTemplateItem = item;
        }

        private void EnsureImage()
        {
            if (currentImage == null)
            {
                throw new InvalidOperationException("请先打开图片。");
            }
        }

        private void StopPlayback()
        {
            if (!playbackTimer.IsEnabled)
            {
                return;
            }

            playbackTimer.Stop();
            LogInfo("自动播放已停止。");
            RefreshUiState();
        }

        private void StopContinuousRun()
        {
            if (!isContinuousRunning && !runTimer.IsEnabled)
            {
                return;
            }

            isContinuousRunning = false;
            runTimer.Stop();
            LogInfo("连续运行已停止。");
            RefreshUiState();
        }

        private void RunStartupDiagnostics(bool showTab)
        {
            IList<DiagnosticItem> items = diagnosticsService.Run(logger.LogDirectory, recipeService.RecipeDirectory);
            DiagnosticsDataGrid.ItemsSource = items;
            foreach (DiagnosticItem item in items)
            {
                if (string.Equals(item.Status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    LogInfo("自检OK：" + item.Name + " - " + item.Detail);
                }
                else
                {
                    AppendAlarm("自检NG：" + item.Name + " - " + item.Detail);
                }
            }

            if (showTab)
            {
                RightTabs.SelectedItem = ProjectDiagnosticsTab;
                HeaderStatusText.Text = items.Any(item => item.Status == "NG") ? "自检完成，存在异常项。" : "自检完成，全部通过。";
            }
        }

        private void ApplyRecipe(VisionRecipe recipe)
        {
            StopPlayback();
            StopContinuousRun();
            if (recipe == null)
            {
                recipe = new VisionRecipe();
            }

            RecipeNameEditTextBox.Text = string.IsNullOrWhiteSpace(recipe.Name) ? "DefaultRecipe" : recipe.Name;
            BlobMinGrayTextBox.Text = recipe.BlobMinGray.ToString(CultureInfo.InvariantCulture);
            BlobMaxGrayTextBox.Text = recipe.BlobMaxGray.ToString(CultureInfo.InvariantCulture);
            BlobMinAreaTextBox.Text = recipe.BlobMinArea.ToString(CultureInfo.InvariantCulture);
            GrayMinTextBox.Text = recipe.GrayMin.ToString(CultureInfo.InvariantCulture);
            GrayMaxTextBox.Text = recipe.GrayMax.ToString(CultureInfo.InvariantCulture);
            EdgeThresholdTextBox.Text = recipe.EdgeThreshold.ToString(CultureInfo.InvariantCulture);
            HDevPathTextBox.Text = recipe.HDevelopPath ?? string.Empty;
            HDevProcedureTextBox.Text = string.IsNullOrWhiteSpace(recipe.ProcedureName) ? "RunInspection" : recipe.ProcedureName;
            TcpClientModeRadio.IsChecked = !string.Equals(recipe.TcpMode, "Server", StringComparison.OrdinalIgnoreCase);
            TcpServerModeRadio.IsChecked = string.Equals(recipe.TcpMode, "Server", StringComparison.OrdinalIgnoreCase);
            TcpIpTextBox.Text = string.IsNullOrWhiteSpace(recipe.TcpIp) ? "127.0.0.1" : recipe.TcpIp;
            TcpPortTextBox.Text = recipe.TcpPort <= 0 ? "9000" : recipe.TcpPort.ToString(CultureInfo.InvariantCulture);
            SetTcpEncoding(recipe.TcpEncoding);
            AutoSendMatchResultCheckBox.IsChecked = recipe.AutoSendResult;

            DisposeCurrentRoi();
            currentRoi = FromRecipeRoi(recipe.SearchRoi);

            ReplaceTemplate(null);

            if (!string.IsNullOrWhiteSpace(recipe.TemplatePath) && File.Exists(recipe.TemplatePath))
            {
                TemplateItem item = new TemplateItem { Name = Path.GetFileNameWithoutExtension(recipe.TemplatePath) };
                item.Service.Load(recipe.TemplatePath);
                TemplateDefinition definition = item.Service.Definition;
                if (definition != null)
                {
                    item.Options = TemplateDefinition.CloneOptions(definition.Options);
                    item.TemplateRoi = definition.TemplateRoi == null ? null : definition.TemplateRoi.Clone();
                    item.DisplayFrame = definition.DisplayFrame == null ? null : definition.DisplayFrame.Clone();
                    item.TrainingMask = TemplateDefinition.CloneRegion(definition.TrainingMask);
                }

                ReplaceTemplate(item);
            }

            ApplyFlowFromRecipe(recipe);

            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private VisionRecipe CaptureRecipe()
        {
            return new VisionRecipe
            {
                Name = string.IsNullOrWhiteSpace(RecipeNameEditTextBox.Text) ? "DefaultRecipe" : RecipeNameEditTextBox.Text.Trim(),
                LastImageDirectory = string.IsNullOrWhiteSpace(currentImagePath) ? string.Empty : Path.GetDirectoryName(currentImagePath),
                SearchRoi = ToRecipeRoi(currentRoi),
                TemplatePath = currentTemplateItem == null ? string.Empty : currentTemplateItem.TemplatePath,
                TemplateOptions = ToRecipeOptions(currentTemplateItem == null ? null : currentTemplateItem.Options),
                EnableShapeMatch = HasEnabledTool(VmToolKind.ShapeMatch),
                EnableBlob = HasEnabledTool(VmToolKind.Blob),
                EnableGrayStat = HasEnabledTool(VmToolKind.GrayStat),
                EnableEdgeMeasure = HasEnabledTool(VmToolKind.EdgeMeasure),
                EnableHDevelop = HasEnabledTool(VmToolKind.HDevelop),
                BlobMinGray = ReadDoubleOrDefault(BlobMinGrayTextBox, 80),
                BlobMaxGray = ReadDoubleOrDefault(BlobMaxGrayTextBox, 255),
                BlobMinArea = ReadDoubleOrDefault(BlobMinAreaTextBox, 50),
                GrayMin = ReadDoubleOrDefault(GrayMinTextBox, 0),
                GrayMax = ReadDoubleOrDefault(GrayMaxTextBox, 255),
                EdgeThreshold = ReadDoubleOrDefault(EdgeThresholdTextBox, 30),
                HDevelopPath = HDevPathTextBox.Text,
                ProcedureName = HDevProcedureTextBox.Text,
                TcpMode = TcpServerModeRadio.IsChecked == true ? "Server" : "Client",
                TcpIp = TcpIpTextBox.Text,
                TcpPort = ReadTcpPortOrDefault(9000),
                TcpEncoding = GetTcpEncodingText(),
                AutoSendResult = AutoSendMatchResultCheckBox.IsChecked == true,
                ToolFlow = CaptureFlowRecipe()
            };
        }

        private static RoiRecipeData ToRecipeRoi(RoiData roi)
        {
            if (roi == null)
            {
                return null;
            }

            return new RoiRecipeData
            {
                ShapeType = roi.ShapeType.ToString(),
                Row1 = roi.Row1,
                Column1 = roi.Column1,
                Row2 = roi.Row2,
                Column2 = roi.Column2,
                Row = roi.Row,
                Column = roi.Column,
                Radius = roi.Radius,
                PolygonRows = roi.PolygonRows,
                PolygonColumns = roi.PolygonColumns
            };
        }

        private static RoiData FromRecipeRoi(RoiRecipeData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ShapeType))
            {
                return null;
            }

            if (string.Equals(data.ShapeType, RoiShapeType.Circle.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return RoiData.CreateCircle(data.Row, data.Column, data.Radius);
            }

            if (string.Equals(data.ShapeType, RoiShapeType.Polygon.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return RoiData.CreatePolygon(data.PolygonRows, data.PolygonColumns);
            }

            return RoiData.CreateRectangle(data.Row1, data.Column1, data.Row2, data.Column2);
        }

        private static TemplateMatchRecipeData ToRecipeOptions(TemplateMatchOptions options)
        {
            if (options == null)
            {
                return null;
            }

            return new TemplateMatchRecipeData
            {
                MinScore = options.MinScore,
                MaxMatches = options.MaxMatches,
                AngleStartDeg = options.AngleStartDeg,
                AngleExtentDeg = options.AngleExtentDeg,
                LimitToSearchRoi = options.LimitToSearchRoi
            };
        }

        private void LoadLayoutState()
        {
            UiLayoutState state = recipeService.LoadLayout();
            if (state.BottomPanelHeight >= 80)
            {
                BottomPanelRow.Height = new GridLength(state.BottomPanelHeight);
            }

            if (state.RightPanelWidth >= 240)
            {
                double maximumWidth = RootGrid.ActualWidth > 0
                    ? Math.Min(420, Math.Max(310, RootGrid.ActualWidth * 0.32))
                    : 420;
                RightPanelColumn.Width = new GridLength(Math.Min(state.RightPanelWidth, maximumWidth));
            }

            if (!string.IsNullOrWhiteSpace(state.LastRecipePath) && File.Exists(state.LastRecipePath))
            {
                try
                {
                    currentRecipePath = state.LastRecipePath;
                    ApplyRecipe(recipeService.LoadRecipe(state.LastRecipePath));
                }
                catch (Exception ex)
                {
                    AppendAlarm("自动加载最近配方失败：" + ex.Message);
                }
            }
        }

        private void SaveLayoutState()
        {
            recipeService.SaveLayout(new UiLayoutState
            {
                BottomPanelHeight = BottomPanelRow.ActualHeight > 0 ? BottomPanelRow.ActualHeight : BottomPanelRow.Height.Value,
                RightPanelWidth = RightPanelColumn.ActualWidth > 0 ? RightPanelColumn.ActualWidth : RightPanelColumn.Width.Value,
                LastRecipePath = currentRecipePath,
                LastImageDirectory = string.IsNullOrWhiteSpace(currentImagePath) ? string.Empty : Path.GetDirectoryName(currentImagePath)
            });
        }

        private void ResultStore_Changed(object sender, EventArgs e)
        {
            RefreshResultGrid();
        }

        private void RefreshResultGrid()
        {
            ResultsDataGrid.ItemsSource = null;
            ResultsDataGrid.ItemsSource = resultStore.Records.OrderByDescending(item => item.Timestamp).ToList();
        }

        private void ExportResults(string type)
        {
            RunUiAction("导出" + type, delegate
            {
                List<InspectionRecord> records = resultStore.Records.OrderBy(item => item.Id).ToList();
                if (records.Count == 0)
                {
                    throw new InvalidOperationException("当前没有可导出的结果。");
                }

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = type == "CSV" ? "CSV file|*.csv" : "Excel workbook|*.xlsx",
                    FileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_results." + (type == "CSV" ? "csv" : "xlsx")
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                if (type == "CSV")
                {
                    csvExportService.Export(dialog.FileName, records);
                }
                else
                {
                    xlsxExportService.Export(dialog.FileName, records);
                }

                LogInfo("结果已导出：" + dialog.FileName);
            });
        }

        private void AutoSendResultIfNeeded()
        {
            if (AutoSendMatchResultCheckBox.IsChecked != true || string.IsNullOrWhiteSpace(lastResultPayload))
            {
                return;
            }

            if (!tcpService.CanSend)
            {
                AppendTcpHistory("状态：TCP未连接，结果未自动发送。");
                return;
            }

            SendTcpPayload(lastResultPayload, "自动发送结果");
        }

        private void SendTcpPayload(string payload, string sourceLabel)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new InvalidOperationException("发送内容为空。");
            }

            if (!tcpService.CanSend)
            {
                AppendTcpHistory("状态：TCP未连接，无法发送：" + payload);
                throw new InvalidOperationException("TCP 未连接，发送不可用。");
            }

            tcpService.Send(payload, ResolveTcpEncoding(), TcpAppendNewLineCheckBox.IsChecked == true);
            string text = payload.TrimEnd('\r', '\n');
            AppendTcpHistory(sourceLabel + "：" + text);
            LogInfo(sourceLabel + "：" + text);
            RefreshUiState();
        }

        private string BuildKeyValueMatchPayload(string templateName, IList<ShapeMatchResult> matches)
        {
            string safeTemplate = EscapePayloadValue(string.IsNullOrWhiteSpace(templateName) ? "Template_001" : templateName);
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

        private string BuildJsonResultPayload(string resultCode, double elapsedMs, IList<InspectionRecord> records)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            builder.Append("\"result\":\"").Append(EscapeJson(resultCode)).Append("\",");
            builder.Append("\"elapsedMs\":").Append(elapsedMs.ToString("F1", CultureInfo.InvariantCulture)).Append(",");
            builder.Append("\"image\":\"").Append(EscapeJson(string.IsNullOrWhiteSpace(currentImagePath) ? string.Empty : Path.GetFileName(currentImagePath))).Append("\",");
            builder.Append("\"tools\":[");
            for (int i = 0; i < records.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                InspectionRecord record = records[i];
                builder.Append("{\"type\":\"").Append(EscapeJson(record.InspectionType)).Append("\",");
                builder.Append("\"result\":\"").Append(EscapeJson(record.ResultCode)).Append("\",");
                builder.Append("\"score\":").Append(record.Score.ToString("F3", CultureInfo.InvariantCulture)).Append(",");
                builder.Append("\"message\":\"").Append(EscapeJson(record.Message)).Append("\"}");
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static string EscapePayloadValue(string value)
        {
            return (value ?? string.Empty).Replace(",", "_").Replace("=", "_").Replace("\r", " ").Replace("\n", " ");
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }

        private int ReadTcpPort()
        {
            int port;
            if (!int.TryParse(TcpPortTextBox.Text, out port) || port < 1 || port > 65535)
            {
                throw new InvalidOperationException("端口必须是 1-65535 的整数。");
            }

            return port;
        }

        private int ReadTcpPortOrDefault(int defaultValue)
        {
            int port;
            return int.TryParse(TcpPortTextBox.Text, out port) ? port : defaultValue;
        }

        private Encoding ResolveTcpEncoding()
        {
            string text = GetTcpEncodingText();
            if (string.Equals(text, "ASCII", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.ASCII;
            }

            if (string.Equals(text, "GBK", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.GetEncoding("GBK");
            }

            return Encoding.UTF8;
        }

        private string GetTcpEncodingText()
        {
            ComboBoxItem item = TcpEncodingCombo.SelectedItem as ComboBoxItem;
            return item == null ? "UTF-8" : item.Content.ToString();
        }

        private void SetTcpEncoding(string encoding)
        {
            for (int i = 0; i < TcpEncodingCombo.Items.Count; i++)
            {
                ComboBoxItem item = TcpEncodingCombo.Items[i] as ComboBoxItem;
                if (item != null && string.Equals(item.Content.ToString(), encoding, StringComparison.OrdinalIgnoreCase))
                {
                    TcpEncodingCombo.SelectedIndex = i;
                    return;
                }
            }

            TcpEncodingCombo.SelectedIndex = 0;
        }

        private double ReadDouble(TextBox box, string name)
        {
            double value;
            if (!double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new InvalidOperationException(name + " 必须是数字。");
            }

            return value;
        }

        private double ReadDoubleOrDefault(TextBox box, double defaultValue)
        {
            double value;
            return double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }

        private static double SumTuple(HTuple tuple)
        {
            if (tuple == null || tuple.Length == 0)
            {
                return 0;
            }

            double sum = 0;
            for (int i = 0; i < tuple.Length; i++)
            {
                sum += tuple[i].D;
            }

            return sum;
        }

        private void TcpService_MessageReceived(object sender, TcpCommunicationMessageEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AppendTcpHistory("接收：" + e.Text);
                LogInfo("TCP接收：" + e.Text);
                RefreshUiState();
            }));
        }

        private void TcpService_StatusChanged(object sender, TcpCommunicationStatusEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AppendTcpHistory("状态：" + e.Message);
                LogInfo("TCP状态：" + e.Message);
                RefreshUiState();
            }));
        }

        private void TcpService_ErrorOccurred(object sender, TcpCommunicationErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                string message = e.Exception == null ? e.Message : e.Message + "：" + e.Exception.Message;
                AppendTcpHistory("异常：" + message);
                AppendAlarm("TCP异常：" + message);
                logger.Error(e.Message, e.Exception);
                RefreshUiState();
            }));
        }

        private void Logger_MessageLogged(object sender, LogMessageEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AppendText(BottomLogTextBox, e.Message);
                if (string.Equals(e.Level, "ERROR", StringComparison.OrdinalIgnoreCase))
                {
                    AppendText(AlarmTextBox, e.Message);
                }
            }));
        }

        private void AppendTcpHistory(string text)
        {
            AppendText(TcpHistoryTextBox, string.Format("{0:HH:mm:ss} {1}", DateTime.Now, text));
        }

        private void AppendAlarm(string text)
        {
            AppendText(AlarmTextBox, string.Format("{0:HH:mm:ss} {1}", DateTime.Now, text));
            logger.Info("报警：" + text);
        }

        private void LogInfo(string message)
        {
            logger.Info(message);
        }

        private static void AppendText(TextBox box, string text)
        {
            if (box == null)
            {
                return;
            }

            box.AppendText(text + Environment.NewLine);
            box.ScrollToEnd();
        }

        private void RunUiAction(string actionName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HeaderStatusText.Text = actionName + "失败：" + ex.Message;
                LogInfo(actionName + "失败：" + ex.Message);
                AppendAlarm(actionName + "失败：" + ex.Message);
                System.Windows.MessageBox.Show(this, ex.Message, actionName, MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
        }

        private T RunUiActionWithResult<T>(string actionName, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                HeaderStatusText.Text = actionName + "失败：" + ex.Message;
                LogInfo(actionName + "失败：" + ex.Message);
                AppendAlarm(actionName + "失败：" + ex.Message);
                System.Windows.MessageBox.Show(this, ex.Message, actionName, MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshUiState();
                ScheduleRefreshDisplay();
                return default(T);
            }
        }

        private static IEnumerable<string> EnumerateImages(string directory)
        {
            string[] extensions = { ".bmp", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
            return Directory.GetFiles(directory)
                .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsImageFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeFileName(string value, string fallback)
        {
            string name = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return name;
        }

        private static void OpenDirectory(string path)
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}
