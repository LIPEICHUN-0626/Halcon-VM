using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
        private const string VersionStamp = "HALCON VM S25 2026-07-23";
        private const string ToolCatalogDragFormat = "HalconVm.ToolCatalogItem";
        private const double FlowLayoutGridSize = 24;
        private const int FlowEditHistoryLimit = 50;

        private readonly HalconImageService imageService = new HalconImageService();
        private readonly AppLogger logger;
        private readonly ImageViewportController viewport = new ImageViewportController();
        private readonly OverlayRenderer overlayRenderer = new OverlayRenderer();
        private readonly RoiEditor roiEditor = new RoiEditor();
        private readonly TcpCommunicationService tcpService = new TcpCommunicationService();
        private readonly InspectionResultStore resultStore = new InspectionResultStore();
        private readonly RecipeService recipeService;
        private readonly StartupDiagnosticsService diagnosticsService = new StartupDiagnosticsService();
        private readonly CsvExportService csvExportService = new CsvExportService();
        private readonly XlsxExportService xlsxExportService = new XlsxExportService();
        private readonly RuntimeStatistics runtimeStatistics = new RuntimeStatistics();
        private readonly HDevInspectionService hdevService = new HDevInspectionService();
        private readonly DispatcherTimer playbackTimer = new DispatcherTimer();
        private readonly DispatcherTimer runTimer = new DispatcherTimer();
        private readonly DispatcherTimer recipeAutosaveTimer = new DispatcherTimer();
        private readonly ObservableCollection<VmToolInstance> flowTools = new ObservableCollection<VmToolInstance>();
        private readonly ObservableCollection<VmPortDisplayItem> inputPortRows = new ObservableCollection<VmPortDisplayItem>();
        private readonly ObservableCollection<VmPortDisplayItem> outputPortRows = new ObservableCollection<VmPortDisplayItem>();
        private readonly ObservableCollection<VmRoiLayer> roiLayers = new ObservableCollection<VmRoiLayer>();
        private readonly ObservableCollection<VmRoiBindingItem> roiBindingRows = new ObservableCollection<VmRoiBindingItem>();
        private readonly ObservableCollection<VmRoiBindingItem> dockRoiBindingRows = new ObservableCollection<VmRoiBindingItem>();
        private readonly ObservableCollection<VmInputPortEditorRow> dockInputPortRows = new ObservableCollection<VmInputPortEditorRow>();
        private readonly ObservableCollection<VmFlowConnectionVisual> flowConnections = new ObservableCollection<VmFlowConnectionVisual>();
        private readonly ObservableCollection<VmFlowGroupVisual> flowGroupVisuals = new ObservableCollection<VmFlowGroupVisual>();
        private readonly ObservableCollection<VmInputSourceOption> flowPortSourceOptions = new ObservableCollection<VmInputSourceOption>();
        private readonly ObservableCollection<VmFlowPortConsumerOption> flowPortConsumers = new ObservableCollection<VmFlowPortConsumerOption>();
        private readonly ObservableCollection<VmRecentRecipeItem> recentRecipes = new ObservableCollection<VmRecentRecipeItem>();
        private readonly List<VmToolCatalogItem> toolCatalog = new List<VmToolCatalogItem>();
        private readonly HashSet<VmToolKind> favoriteToolKinds = new HashSet<VmToolKind>();
        private readonly List<VmToolKind> recentToolKinds = new List<VmToolKind>();
        private ICollectionView toolCatalogView;
        private string toolCatalogMode = "All";
        private System.Windows.Point toolCatalogDragStartPoint;
        private VmToolCatalogItem toolCatalogDragItem;
        private bool isFlowGraphPanning;
        private bool isFlowGraphSelecting;
        private bool flowGraphSelectionMoved;
        private bool flowSelectionUpdating;
        private System.Windows.Point flowGraphPanStart;
        private System.Windows.Point flowGraphSelectionStart;
        private double flowGraphPanHorizontalOffset;
        private double flowGraphPanVerticalOffset;
        private readonly HashSet<string> flowGraphSelectionBaseline = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string selectedFlowConnectionKey;
        private string selectedFlowPortToolId;
        private string selectedFlowPortName;
        private string selectedFlowPortDirection;
        private FlowPortDragSession flowPortDragSession;
        private FlowNodeDragSession flowNodeDragSession;
        private bool isManualFlowLayout;
        private readonly List<VmToolClipboardItem> flowToolClipboard = new List<VmToolClipboardItem>();
        private readonly List<FlowEditHistoryEntry> flowUndoHistory = new List<FlowEditHistoryEntry>();
        private readonly List<FlowEditHistoryEntry> flowRedoHistory = new List<FlowEditHistoryEntry>();
        private int? flowPasteInsertionIndex;
        private bool flowEditHistorySuspended;
        private bool suppressFlowInsertHistory;

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
        private string toolOverlayProducerToolId;
        private string toolOverlaySourceText;
        private string toolOverlayColorText;
        private string toolOverlayResultCode;
        private int toolOverlayObjectCount;
        private double toolOverlayArea;
        private string lastResultPayload = string.Empty;
        private string currentRecipePath;
        private string savedRecipeFingerprint;
        private DateTime? lastRecipeSavedAt;
        private RecipeRecoveryData pendingRecovery;
        private bool refreshQueued;
        private bool uiReady;
        private bool isRecipeDirty;
        private bool recipeTrackingSuspended;
        private bool recipeCloseConfirmed;
        private bool isPanning;
        private System.Drawing.Point lastPanPoint;
        private bool isContinuousRunning;
        private bool isFlowExecutionActive;
        private bool isPauseRequested;
        private bool isStopRequested;
        private bool isDebugBreakpointPause;
        private bool debugSingleStepRequested;
        private bool debugPauseBeforeNextTool;
        private string debugPausedToolId;
        private bool inspectorUpdating;
        private bool roiBindingUpdating;
        private bool imageContextUpdating;
        private bool imageContextManuallySelected;
        private bool dockDraftUpdating;
        private bool dockDraftDirty;
        private bool dockSelectionReverting;
        private VmToolInstance dockEditingTool;
        private VmRoiLayer roiEditingLayer;
        private RoiEditHandle roiEditHandle;
        private int roiEditVertexIndex = -1;
        private RoiData roiEditOriginalGeometry;
        private PointF roiEditStartPoint;
        private bool roiGeometryChanged;
        private bool imageWorkspaceExpanded;
        private bool showRoiOverlay = true;
        private bool showResultOverlay = true;

        private sealed class ResolvedImageContext
        {
            public string RequestedKey { get; set; }
            public string SourceText { get; set; }
            public string DetailText { get; set; }
            public string StateText { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public HImage GlobalImage { get; set; }
            public VmImageSnapshot Snapshot { get; set; }

            public bool HasImage
            {
                get { return GlobalImage != null || Snapshot != null && !Snapshot.IsDisposed; }
            }

            public HImage CreateImageCopy()
            {
                if (Snapshot != null)
                {
                    return Snapshot.CreateImageCopy();
                }

                return GlobalImage == null ? null : GlobalImage.CopyImage();
            }

            public string GetPixelDisplay(int row, int column)
            {
                if (Snapshot != null)
                {
                    return Snapshot.GetPixelDisplay(row, column);
                }

                if (GlobalImage == null || row < 0 || column < 0 || row >= Height || column >= Width)
                {
                    return "越界";
                }

                HTuple value;
                HOperatorSet.GetGrayval(GlobalImage, row, column, out value);
                return VmImageSnapshot.FormatPixelTuple(value);
            }
        }

        private sealed class VmToolClipboardItem
        {
            public string SourceToolId { get; set; }
            public int SourceSequence { get; set; }
            public VmToolKind Kind { get; set; }
            public string InstanceName { get; set; }
            public bool IsEnabled { get; set; }
            public VmToolParameterData Parameters { get; set; }
            public List<string> RoiIds { get; set; }
            public List<VmToolInputBindingData> InputBindings { get; set; }
            public string InputToolId { get; set; }
            public string InputPortName { get; set; }
            public string NumericOperator { get; set; }
            public double NumericLowerLimit { get; set; }
            public double NumericUpperLimit { get; set; }
            public double NumericTolerance { get; set; }
            public bool HasManualGraphPosition { get; set; }
            public double GraphX { get; set; }
            public double GraphY { get; set; }
            public string GroupId { get; set; }
            public string GroupName { get; set; }
            public bool IsGroupCollapsed { get; set; }
        }

        private sealed class FlowPortDragSession
        {
            public VmFlowPortChip OriginPort { get; set; }
            public VmFlowPortChip SourcePort { get; set; }
            public VmFlowPortChip FixedTargetPort { get; set; }
            public System.Windows.Point StartPoint { get; set; }
            public bool HasMoved { get; set; }
        }

        private sealed class FlowNodeDragSession
        {
            public VmToolInstance Tool { get; set; }
            public System.Windows.Point StartPoint { get; set; }
            public Dictionary<VmToolInstance, System.Windows.Point> InitialPositions { get; set; }
            public Dictionary<VmToolInstance, bool> InitialManualFlags { get; set; }
            public bool InitialManualMode { get; set; }
            public bool HasMoved { get; set; }
            public FlowEditSnapshot BeforeSnapshot { get; set; }
        }

        private sealed class FlowEditSnapshot
        {
            public List<ToolFlowRecipeItem> ToolFlow { get; set; }
            public List<string> SelectedToolIds { get; set; }
            public string PrimaryToolId { get; set; }
            public string Fingerprint { get; set; }
        }

        private sealed class FlowEditHistoryEntry
        {
            public string Description { get; set; }
            public FlowEditSnapshot Before { get; set; }
            public FlowEditSnapshot After { get; set; }
        }

        public MainWindow()
            : this(new RecipeService(), new AppLogger())
        {
        }

        internal MainWindow(RecipeService recipeService, AppLogger logger)
        {
            if (recipeService == null) throw new ArgumentNullException("recipeService");
            if (logger == null) throw new ArgumentNullException("logger");
            this.recipeService = recipeService;
            this.logger = logger;
            InitializeComponent();

            playbackTimer.Interval = TimeSpan.FromMilliseconds(650);
            playbackTimer.Tick += PlaybackTimer_Tick;
            runTimer.Interval = TimeSpan.FromMilliseconds(500);
            runTimer.Tick += RunTimer_Tick;
            recipeAutosaveTimer.Interval = TimeSpan.FromMilliseconds(750);
            recipeAutosaveTimer.Tick += RecipeAutosaveTimer_Tick;

            logger.MessageLogged += Logger_MessageLogged;
            tcpService.MessageReceived += TcpService_MessageReceived;
            tcpService.StatusChanged += TcpService_StatusChanged;
            tcpService.ErrorOccurred += TcpService_ErrorOccurred;
            resultStore.Changed += ResultStore_Changed;

            AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(RecipeEditor_TextChanged));
            AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(RecipeEditor_SelectionChanged));
            AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(RecipeEditor_ToggleChanged));
            AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(RecipeEditor_ToggleChanged));

            InitializeVmWorkspace();
            uiReady = true;
        }

        private void InitializeVmWorkspace()
        {
            flowTools.CollectionChanged += delegate { RefreshFlowSequence(); ScheduleRecipeStateCheck(); };
            roiLayers.CollectionChanged += delegate { ScheduleRecipeStateCheck(); };
            toolCatalog.AddRange(new[]
            {
                CreateCatalogItem(VmToolKind.ImageSource),
                CreateCatalogItem(VmToolKind.ImageChannel),
                CreateCatalogItem(VmToolKind.ImageFilter),
                CreateCatalogItem(VmToolKind.ImageEnhancement),
                CreateCatalogItem(VmToolKind.ImageDifference),
                CreateCatalogItem(VmToolKind.DynamicThreshold),
                CreateCatalogItem(VmToolKind.ImageThreshold),
                CreateCatalogItem(VmToolKind.RegionMorphology),
                CreateCatalogItem(VmToolKind.RegionFeatureFilter),
                CreateCatalogItem(VmToolKind.RegionSetOperation),
                CreateCatalogItem(VmToolKind.RegionContourMeasurement),
                CreateCatalogItem(VmToolKind.XldContourFilter),
                CreateCatalogItem(VmToolKind.XldContourSegmentation),
                CreateCatalogItem(VmToolKind.XldLineFitting),
                CreateCatalogItem(VmToolKind.XldCircleFitting),
                CreateCatalogItem(VmToolKind.XldEllipseFitting),
                CreateCatalogItem(VmToolKind.XldRectangleFitting),
                CreateCatalogItem(VmToolKind.ShapeMatch),
                CreateCatalogItem(VmToolKind.Blob),
                CreateCatalogItem(VmToolKind.GrayStat),
                CreateCatalogItem(VmToolKind.EdgeMeasure),
                CreateCatalogItem(VmToolKind.HDevelop),
                CreateCatalogItem(VmToolKind.NumericJudge)
            });
            toolCatalogView = CollectionViewSource.GetDefaultView(toolCatalog);
            toolCatalogView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            toolCatalogView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            toolCatalogView.SortDescriptions.Add(new SortDescription("RecentRank", ListSortDirection.Ascending));
            toolCatalogView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            toolCatalogView.Filter = FilterToolCatalogItem;
            ToolCatalogList.ItemsSource = toolCatalogView;
            FlowToolList.ItemsSource = flowTools;
            FlowGraphNodeItemsControl.ItemsSource = flowTools;
            FlowGroupItemsControl.ItemsSource = flowGroupVisuals;
            FlowConnectionItemsControl.ItemsSource = flowConnections;
            FlowConnectionLabelItemsControl.ItemsSource = flowConnections;
            FlowPortSourceComboBox.ItemsSource = flowPortSourceOptions;
            FlowPortConsumerListBox.ItemsSource = flowPortConsumers;
            InputPortsItemsControl.ItemsSource = inputPortRows;
            OutputPortsItemsControl.ItemsSource = outputPortRows;
            RoiLayerList.ItemsSource = roiLayers;
            RoiBindingItemsControl.ItemsSource = roiBindingRows;
            DockInputPortItemsControl.ItemsSource = dockInputPortRows;
            DockRoiBindingItemsControl.ItemsSource = dockRoiBindingRows;
            RecentRecipeList.ItemsSource = recentRecipes;
            NumericOperatorComboBox.ItemsSource = NumericJudgeOperatorOption.CreateAll();
            DockNumericOperatorComboBox.ItemsSource = NumericJudgeOperatorOption.CreateAll();
            DockImageChannelModeComboBox.ItemsSource = VmImageChannelMode.CreateAll();
            DockImageFilterModeComboBox.ItemsSource = VmImageFilterMode.CreateAll();
            DockImageEnhancementModeComboBox.ItemsSource = VmImageEnhancementMode.CreateAll();
            DockImageDifferenceModeComboBox.ItemsSource = VmImageDifferenceMode.CreateAll();
            DockDynamicThresholdPolarityComboBox.ItemsSource = VmDynamicThresholdPolarity.CreateAll();
            DockRegionMorphologyModeComboBox.ItemsSource = VmRegionMorphologyMode.CreateAll();
            DockRegionFeatureComboBox.ItemsSource = VmRegionFeature.CreateAll();
            DockRegionSetOperationComboBox.ItemsSource = VmRegionSetOperationMode.CreateAll();
            DockRegionContourModeComboBox.ItemsSource = VmRegionContourMode.CreateAll();
            DockXldContourFeatureComboBox.ItemsSource = VmXldContourFeature.CreateAll();
            DockXldSegmentModeComboBox.ItemsSource = VmXldSegmentMode.CreateAll();
            DockXldLineFitAlgorithmComboBox.ItemsSource = VmXldLineFitAlgorithm.CreateAll();
            DockXldCircleFitAlgorithmComboBox.ItemsSource = VmXldCircleFitAlgorithm.CreateAll();
            DockXldEllipseFitAlgorithmComboBox.ItemsSource = VmXldEllipseFitAlgorithm.CreateAll();
            DockXldRectangleFitAlgorithmComboBox.ItemsSource = VmXldRectangleFitAlgorithm.CreateAll();
            DockRoiExecutionModeComboBox.ItemsSource = VmRoiExecutionMode.CreateAll();
            ImageContextComboBox.ItemsSource = VmImageContextOption.CreateAll();
            ImageContextComboBox.SelectedValue = VmImageContextOption.GlobalInput;
            RefreshToolCatalogView();
            ApplyFlowFromRecipe(new VisionRecipe());
        }

        private void RefreshFlowSequence()
        {
            for (int index = 0; index < flowTools.Count; index++)
            {
                flowTools[index].Sequence = index + 1;
            }
        }

        private void RefreshFlowPortVisualization()
        {
            RefreshFlowSelectionFlags();
            VmToolInstance selected = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            string selectedConnectionSourceToolId;
            string selectedConnectionSourcePortName;
            string selectedConnectionTargetToolId;
            string selectedConnectionTargetPortName;
            bool hasSelectedConnection = TryParseFlowConnectionKey(
                selectedFlowConnectionKey,
                out selectedConnectionSourceToolId,
                out selectedConnectionSourcePortName,
                out selectedConnectionTargetToolId,
                out selectedConnectionTargetPortName);
            HashSet<string> upstreamIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> downstreamIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selected != null)
            {
                foreach (VmToolInputBindingData binding in selected.InputBindings)
                {
                    if (!string.IsNullOrWhiteSpace(binding.SourceToolId)) upstreamIds.Add(binding.SourceToolId);
                }
                if (selected.Kind == VmToolKind.NumericJudge && !string.IsNullOrWhiteSpace(selected.InputToolId))
                {
                    upstreamIds.Add(selected.InputToolId);
                }

                foreach (VmToolInstance candidate in flowTools)
                {
                    if (candidate.InputBindings.Any(item => string.Equals(item.SourceToolId, selected.ToolId, StringComparison.OrdinalIgnoreCase)) ||
                        candidate.Kind == VmToolKind.NumericJudge && string.Equals(candidate.InputToolId, selected.ToolId, StringComparison.OrdinalIgnoreCase))
                    {
                        downstreamIds.Add(candidate.ToolId);
                    }
                }
            }

            foreach (VmToolInstance tool in flowTools)
            {
                if (ReferenceEquals(tool, selected))
                {
                    tool.DependencyState = "Selected";
                    tool.DependencySummary = string.Format(
                        CultureInfo.InvariantCulture,
                        "当前模块 · 直接上游 {0} · 直接下游 {1}",
                        upstreamIds.Count,
                        downstreamIds.Count);
                }
                else if (upstreamIds.Contains(tool.ToolId))
                {
                    tool.DependencyState = "Upstream";
                    tool.DependencySummary = "直接上游 → " + selected.InstanceName;
                }
                else if (downstreamIds.Contains(tool.ToolId))
                {
                    tool.DependencyState = "Downstream";
                    tool.DependencySummary = selected.InstanceName + " → 直接下游";
                }
                else
                {
                    tool.DependencyState = "Neutral";
                    tool.DependencySummary = selected == null ? "选择模块后显示直接依赖" : "非直接依赖";
                }

                if (hasSelectedConnection)
                {
                    if (string.Equals(tool.ToolId, selectedConnectionSourceToolId, StringComparison.OrdinalIgnoreCase))
                    {
                        tool.DependencyState = "ConnectionSource";
                        tool.DependencySummary = "所选连接来源端点 · " + selectedConnectionSourcePortName;
                    }
                    else if (string.Equals(tool.ToolId, selectedConnectionTargetToolId, StringComparison.OrdinalIgnoreCase))
                    {
                        tool.DependencyState = "ConnectionTarget";
                        tool.DependencySummary = "所选连接目标端点 · " + selectedConnectionTargetPortName;
                    }
                    else
                    {
                        tool.DependencyState = "Neutral";
                        tool.DependencySummary = "非所选连接端点";
                    }
                }

                tool.FlowInputPorts.Clear();
                foreach (VmPortDefinition port in ToolMetadata.GetInputPorts(tool.Kind))
                {
                    tool.FlowInputPorts.Add(CreateFlowInputPortChip(tool, port));
                }

                tool.FlowOutputPorts.Clear();
                foreach (VmPortDefinition port in ToolMetadata.GetOutputPorts(tool.Kind))
                {
                    int consumerCount = GetFlowPortConsumerCount(tool, port.PortName);
                    string value = tool.GetFormattedOutput(port.PortName);
                    tool.FlowOutputPorts.Add(new VmFlowPortChip
                    {
                        OwnerToolId = tool.ToolId,
                        OwnerToolName = tool.InstanceName,
                        Direction = "OUT",
                        PortName = port.PortName,
                        DisplayName = port.DisplayName,
                        DataType = port.DataType,
                        EndpointText = value == "--"
                            ? (consumerCount == 0 ? "等待运行 · 无消费者" : "等待运行 · " + consumerCount.ToString(CultureInfo.InvariantCulture) + " 个消费者")
                            : value + (consumerCount == 0 ? " · 无消费者" : " · → " + consumerCount.ToString(CultureInfo.InvariantCulture) + " 个消费者"),
                        ValueText = value,
                        StatusText = port.DisplayName + "；" + (value == "--" ? "尚无本周期输出" : "当前值 " + value),
                        StateKey = string.Equals(tool.ResultCode, "NG", StringComparison.OrdinalIgnoreCase) ? "NG" : (value == "--" ? "Waiting" : "Ready"),
                        IsConnected = consumerCount > 0,
                        IsPortSelected = IsSelectedFlowPort(tool.ToolId, port.PortName, "OUT"),
                        IsConnectionEndpointSelected = hasSelectedConnection &&
                            string.Equals(tool.ToolId, selectedConnectionSourceToolId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(port.PortName, selectedConnectionSourcePortName, StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
            RefreshFlowGraphLayout();
        }

        private VmFlowPortChip CreateFlowInputPortChip(VmToolInstance tool, VmPortDefinition port)
        {
            VmToolInputBindingData binding = tool.GetInputBinding(port.PortName);
            if (tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value" && !string.IsNullOrWhiteSpace(tool.InputToolId))
            {
                binding = new VmToolInputBindingData
                {
                    TargetPortName = port.PortName,
                    SourceToolId = tool.InputToolId,
                    SourcePortName = tool.InputPortName
                };
            }

            if (binding != null)
            {
                VmToolInstance source = GetInputSourceTool(binding);
                string error = tool.Kind == VmToolKind.NumericJudge
                    ? GetNumericJudgeConfigurationError(tool)
                    : GetInputBindingConfigurationError(tool, binding);
                string value = source == null ? "--" : source.GetFormattedOutput(binding.SourcePortName);
                bool ready = string.IsNullOrWhiteSpace(error) && source != null &&
                             string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && value != "--";
                return new VmFlowPortChip
                {
                    OwnerToolId = tool.ToolId,
                    OwnerToolName = tool.InstanceName,
                    Direction = "IN",
                    PortName = port.PortName,
                    DisplayName = port.DisplayName,
                    DataType = port.DataType,
                    EndpointText = source == null
                        ? "← 无效来源"
                        : "← " + source.InstanceName + "." + binding.SourcePortName + (value == "--" ? string.Empty : " = " + value),
                    ValueText = value,
                    StatusText = string.IsNullOrWhiteSpace(error)
                        ? (ready ? "连接有效 · 本周期有值" : "连接有效 · 等待上游 OK 本周期输出")
                        : error,
                    StateKey = string.IsNullOrWhiteSpace(error) ? (ready ? "Ready" : "Waiting") : "Error",
                    IsConnected = source != null && string.IsNullOrWhiteSpace(error),
                    IsPortSelected = IsSelectedFlowPort(tool.ToolId, port.PortName, "IN"),
                    IsConnectionEndpointSelected = IsSelectedFlowConnectionEndpoint(tool.ToolId, port.PortName, false)
                };
            }

            bool systemImage = port.PortName == "Image";
            bool localResource = port.PortName == "ROI" || port.PortName == "SearchROI" || port.PortName == "ShapeModel" || port.PortName == "Program";
            bool defaultReady = systemImage && currentImage != null || localResource || port.IsOptional;
            string endpoint = systemImage
                ? "← 系统.Image" + (currentImage == null ? "（暂无图像）" : "（当前图像）")
                : (localResource ? "← 本地资源 / 图层" : (port.IsOptional ? "← 未连接（可选）" : "← 未连接（必需）"));
            return new VmFlowPortChip
            {
                OwnerToolId = tool.ToolId,
                OwnerToolName = tool.InstanceName,
                Direction = "IN",
                PortName = port.PortName,
                DisplayName = port.DisplayName,
                DataType = port.DataType,
                EndpointText = endpoint,
                ValueText = "--",
                StatusText = defaultReady ? "使用默认输入策略" : "必需输入尚未连接",
                StateKey = defaultReady ? (systemImage && currentImage == null ? "Waiting" : "Ready") : "Missing",
                IsConnected = defaultReady,
                IsPortSelected = IsSelectedFlowPort(tool.ToolId, port.PortName, "IN"),
                IsConnectionEndpointSelected = IsSelectedFlowConnectionEndpoint(tool.ToolId, port.PortName, false)
            };
        }

        private bool IsSelectedFlowPort(string toolId, string portName, string direction)
        {
            return string.Equals(selectedFlowPortToolId, toolId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(selectedFlowPortName, portName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(selectedFlowPortDirection, direction, StringComparison.OrdinalIgnoreCase);
        }

        private int GetFlowPortConsumerCount(VmToolInstance source, string sourcePortName)
        {
            return flowTools.Count(tool =>
                tool.InputBindings.Any(binding =>
                    string.Equals(binding.SourceToolId, source.ToolId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(binding.SourcePortName, sourcePortName, StringComparison.OrdinalIgnoreCase)) ||
                tool.Kind == VmToolKind.NumericJudge &&
                string.Equals(tool.InputToolId, source.ToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(tool.InputPortName, sourcePortName, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<VmToolInputBindingData> GetFlowGraphBindings(VmToolInstance tool)
        {
            foreach (VmToolInputBindingData binding in tool.InputBindings)
            {
                yield return binding;
            }

            if (tool.Kind == VmToolKind.NumericJudge &&
                !string.IsNullOrWhiteSpace(tool.InputToolId) &&
                !tool.InputBindings.Any(item => string.Equals(item.TargetPortName, "Value", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new VmToolInputBindingData
                {
                    TargetPortName = "Value",
                    SourceToolId = tool.InputToolId,
                    SourcePortName = tool.InputPortName
                };
            }
        }

        private static string BuildFlowConnectionKey(string sourceToolId, string sourcePortName, string targetToolId, string targetPortName)
        {
            return string.Join("|", new[]
            {
                sourceToolId ?? string.Empty,
                sourcePortName ?? string.Empty,
                targetToolId ?? string.Empty,
                targetPortName ?? string.Empty
            });
        }

        private static bool TryParseFlowConnectionKey(
            string key,
            out string sourceToolId,
            out string sourcePortName,
            out string targetToolId,
            out string targetPortName)
        {
            sourceToolId = null;
            sourcePortName = null;
            targetToolId = null;
            targetPortName = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string[] parts = key.Split('|');
            if (parts.Length != 4)
            {
                return false;
            }

            sourceToolId = parts[0];
            sourcePortName = parts[1];
            targetToolId = parts[2];
            targetPortName = parts[3];
            return !string.IsNullOrWhiteSpace(targetToolId) && !string.IsNullOrWhiteSpace(targetPortName);
        }

        private bool IsSelectedFlowConnectionEndpoint(string toolId, string portName, bool sourceEndpoint)
        {
            string sourceToolId;
            string sourcePortName;
            string targetToolId;
            string targetPortName;
            if (!TryParseFlowConnectionKey(selectedFlowConnectionKey, out sourceToolId, out sourcePortName, out targetToolId, out targetPortName))
            {
                return false;
            }

            return sourceEndpoint
                ? string.Equals(toolId, sourceToolId, StringComparison.OrdinalIgnoreCase) && string.Equals(portName, sourcePortName, StringComparison.OrdinalIgnoreCase)
                : string.Equals(toolId, targetToolId, StringComparison.OrdinalIgnoreCase) && string.Equals(portName, targetPortName, StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshFlowGraphLayout()
        {
            if (FlowGraphCanvas == null)
            {
                return;
            }

            const double nodeWidth = 360;
            const double leftMargin = 42;
            const double topMargin = 38;
            const double levelPitch = 500;
            const double nodeGap = 28;

            Dictionary<string, VmToolInstance> byId = new Dictionary<string, VmToolInstance>(StringComparer.OrdinalIgnoreCase);
            foreach (VmToolInstance tool in flowTools)
            {
                if (!string.IsNullOrWhiteSpace(tool.ToolId) && !byId.ContainsKey(tool.ToolId)) byId.Add(tool.ToolId, tool);
            }
            Dictionary<string, int> levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<int, double> nextYByLevel = new Dictionary<int, double>();

            for (int index = 0; index < flowTools.Count; index++)
            {
                VmToolInstance tool = flowTools[index];
                List<VmToolInstance> parents = GetFlowGraphBindings(tool)
                    .Select(item => byId.ContainsKey(item.SourceToolId ?? string.Empty) ? byId[item.SourceToolId] : null)
                    .Where(item => item != null && flowTools.IndexOf(item) < index)
                    .Distinct()
                    .ToList();
                int level = parents.Count == 0 ? 0 : parents.Max(item => levels.ContainsKey(item.ToolId) ? levels[item.ToolId] + 1 : 1);
                levels[tool.ToolId] = level;

                int portRows = Math.Max(ToolMetadata.GetInputPorts(tool.Kind).Count, ToolMetadata.GetOutputPorts(tool.Kind).Count);
                double portPitch = tool.IsGroupCollapsed ? 24 : 40;
                tool.GraphHeight = Math.Max(tool.IsGroupCollapsed ? 110 : 150, 78 + portRows * portPitch);
                double automaticX = leftMargin + level * levelPitch;

                double desiredY = topMargin;
                if (parents.Count > 0)
                {
                    desiredY = parents.Average(item => item.GraphY + item.GraphHeight / 2.0) - tool.GraphHeight / 2.0;
                }
                double nextY;
                if (!nextYByLevel.TryGetValue(level, out nextY)) nextY = topMargin;
                double automaticY = Math.Max(topMargin, Math.Max(desiredY, nextY));
                if (!isManualFlowLayout || !tool.HasManualGraphPosition ||
                    double.IsNaN(tool.GraphX) || double.IsInfinity(tool.GraphX) ||
                    double.IsNaN(tool.GraphY) || double.IsInfinity(tool.GraphY))
                {
                    tool.GraphX = automaticX;
                    tool.GraphY = automaticY;
                    tool.HasManualGraphPosition = isManualFlowLayout;
                }
                nextYByLevel[level] = automaticY + tool.GraphHeight + nodeGap;
            }

            RefreshFlowGroupVisuals();

            flowConnections.Clear();
            VmToolInstance selected = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            foreach (VmToolInstance target in flowTools)
            {
                IList<VmPortDefinition> targetPorts = ToolMetadata.GetInputPorts(target.Kind);
                foreach (VmToolInputBindingData binding in GetFlowGraphBindings(target))
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.TargetPortName)) continue;
                    VmPortDefinition targetPort = targetPorts.FirstOrDefault(item => string.Equals(item.PortName, binding.TargetPortName, StringComparison.OrdinalIgnoreCase));
                    if (targetPort == null) continue;

                    int targetPortIndex = targetPorts.ToList().FindIndex(item => string.Equals(item.PortName, binding.TargetPortName, StringComparison.OrdinalIgnoreCase));
                    VmToolInstance source;
                    byId.TryGetValue(binding.SourceToolId ?? string.Empty, out source);
                    double targetX = target.GraphX;
                    double targetY = GetFlowPortCenterY(target, Math.Max(0, targetPortIndex));
                    double sourceX = targetX - 150;
                    double sourceY = targetY;
                    string sourceName = "缺失来源";
                    string error = source == null ? "上游模块不存在或已删除。" : GetInputBindingConfigurationError(target, binding);
                    if (target.Kind == VmToolKind.NumericJudge) error = GetNumericJudgeConfigurationError(target);
                    if (source != null)
                    {
                        IList<VmPortDefinition> sourcePorts = ToolMetadata.GetOutputPorts(source.Kind);
                        int sourcePortIndex = sourcePorts.ToList().FindIndex(item => string.Equals(item.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                        sourceX = source.GraphX + nodeWidth;
                        sourceY = GetFlowPortCenterY(source, Math.Max(0, sourcePortIndex));
                        sourceName = source.InstanceName;
                    }

                    string value = source == null ? "--" : source.GetFormattedOutput(binding.SourcePortName);
                    bool ready = string.IsNullOrWhiteSpace(error) && source != null &&
                                 string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && value != "--";
                    string connectionKey = BuildFlowConnectionKey(binding.SourceToolId, binding.SourcePortName, target.ToolId, binding.TargetPortName);
                    bool connectionSelected = string.Equals(selectedFlowConnectionKey, connectionKey, StringComparison.OrdinalIgnoreCase);
                    bool highlighted = selected != null && (ReferenceEquals(selected, source) || ReferenceEquals(selected, target));
                    SolidColorBrush typeBrush = GetFlowConnectionBrush(string.IsNullOrWhiteSpace(error) ? targetPort.DataType : "Error");
                    flowConnections.Add(new VmFlowConnectionVisual
                    {
                        ConnectionKey = connectionKey,
                        SourceToolId = binding.SourceToolId,
                        TargetToolId = target.ToolId,
                        SourcePortName = binding.SourcePortName,
                        TargetPortName = binding.TargetPortName,
                        DataType = targetPort.DataType,
                        SourceToolName = sourceName,
                        TargetToolName = target.InstanceName,
                        SourceEndpointText = sourceName + "." + (binding.SourcePortName ?? "?"),
                        TargetEndpointText = target.InstanceName + "." + binding.TargetPortName,
                        CurrentValueText = value,
                        ErrorReason = string.IsNullOrWhiteSpace(error) ? "无" : error,
                        StateKey = string.IsNullOrWhiteSpace(error) ? (ready ? "Ready" : "Waiting") : "Error",
                        StateText = string.IsNullOrWhiteSpace(error) ? (ready ? "连接有效 · 本周期有值" : "连接有效 · 等待上游 OK 本周期输出") : "连接异常",
                        IsSelected = connectionSelected,
                        Label = sourceName + "." + (binding.SourcePortName ?? "?") + " → " + binding.TargetPortName + " · " + targetPort.DataType,
                        StatusText = string.IsNullOrWhiteSpace(error) ? (ready ? "连接有效 · 本周期有值" : "连接有效 · 等待上游 OK 本周期输出") : error,
                        Geometry = CreateFlowConnectionGeometry(sourceX, sourceY, targetX, targetY),
                        Stroke = typeBrush,
                        LabelBrush = typeBrush,
                        StrokeThickness = connectionSelected ? 5.5 : (highlighted ? 4 : 2.4),
                        StrokeDashArray = string.IsNullOrWhiteSpace(error) && ready ? null : new DoubleCollection { 5, 3 },
                        LabelX = (sourceX + targetX) / 2.0 - 90,
                        LabelY = (sourceY + targetY) / 2.0 - 16
                    });
                }
            }

            double groupRight = flowGroupVisuals.Select(item => item.X + item.Width).DefaultIfEmpty(0).Max();
            double groupBottom = flowGroupVisuals.Select(item => item.Y + item.Height).DefaultIfEmpty(0).Max();
            double canvasWidth = flowTools.Count == 0 ? 1000 : Math.Max(1000, Math.Max(flowTools.Max(item => item.GraphX) + nodeWidth + 100, groupRight + 40));
            double canvasHeight = flowTools.Count == 0 ? 700 : Math.Max(700, Math.Max(flowTools.Max(item => item.GraphY + item.GraphHeight) + 100, groupBottom + 40));
            FlowGraphCanvas.Width = canvasWidth;
            FlowGraphCanvas.Height = canvasHeight;
            FlowGraphEmptyState.Visibility = flowTools.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (FlowLayoutModeText != null)
            {
                FlowLayoutModeText.Text = isManualFlowLayout ? "布局：手动 · 已入配方" : "布局：自动 · 按依赖";
                FlowLayoutModeText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(isManualFlowLayout ? "#B45309" : "#0F766E"));
            }
            if (!string.IsNullOrWhiteSpace(selectedFlowConnectionKey) && !flowConnections.Any(item => item.IsSelected))
            {
                selectedFlowConnectionKey = null;
            }
            UpdateFlowConnectionInspector();
            UpdateFlowPortInspector();
        }

        private static double GetFlowPortCenterY(VmToolInstance tool, int portIndex)
        {
            return tool.GraphY + (tool.IsGroupCollapsed ? 79 : 87) + Math.Max(0, portIndex) * (tool.IsGroupCollapsed ? 24 : 40);
        }

        private void RefreshFlowGroupVisuals()
        {
            flowGroupVisuals.Clear();
            foreach (IGrouping<string, VmToolInstance> group in flowTools
                .Where(item => !string.IsNullOrWhiteSpace(item.GroupId))
                .GroupBy(item => item.GroupId, StringComparer.OrdinalIgnoreCase))
            {
                List<VmToolInstance> members = group.OrderBy(item => flowTools.IndexOf(item)).ToList();
                if (members.Count < 2) continue;
                HashSet<string> memberIds = new HashSet<string>(members.Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
                List<string> boundaryInputs = new List<string>();
                List<string> boundaryOutputs = new List<string>();
                int internalConnections = 0;
                foreach (VmToolInstance target in members)
                {
                    foreach (VmToolInputBindingData binding in GetFlowGraphBindings(target))
                    {
                        if (binding == null || string.IsNullOrWhiteSpace(binding.SourceToolId)) continue;
                        if (memberIds.Contains(binding.SourceToolId)) internalConnections++;
                        else boundaryInputs.Add(target.InstanceName + "." + binding.TargetPortName);
                    }
                }
                foreach (VmToolInstance target in flowTools.Where(item => !memberIds.Contains(item.ToolId)))
                {
                    foreach (VmToolInputBindingData binding in GetFlowGraphBindings(target).Where(item => item != null && memberIds.Contains(item.SourceToolId ?? string.Empty)))
                    {
                        VmToolInstance source = members.FirstOrDefault(item => string.Equals(item.ToolId, binding.SourceToolId, StringComparison.OrdinalIgnoreCase));
                        boundaryOutputs.Add((source == null ? "?" : source.InstanceName) + "." + binding.SourcePortName);
                    }
                }

                int errorCount = members.Count(item => string.Equals(item.ResultCode, "NG", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.ConfigurationStatus, "异常", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(item.ErrorMessage));
                double left = Math.Max(6, members.Min(item => item.GraphX) - 22);
                double top = Math.Max(4, members.Min(item => item.GraphY) - 62);
                double right = members.Max(item => item.GraphX + 360) + 22;
                double bottom = members.Max(item => item.GraphY + item.GraphHeight) + 22;
                bool collapsed = members.All(item => item.IsGroupCollapsed);
                int breakpointCount = members.Count(item => item.IsBreakpoint);
                VmToolInstance pointer = members.FirstOrDefault(item => item.IsExecutionPointer);
                flowGroupVisuals.Add(new VmFlowGroupVisual
                {
                    GroupId = group.Key,
                    GroupName = string.IsNullOrWhiteSpace(members[0].GroupName) ? "流程组合" : members[0].GroupName,
                    IsCollapsed = collapsed,
                    X = left,
                    Y = top,
                    Width = Math.Max(404, right - left),
                    Height = Math.Max(150, bottom - top),
                    StateText = collapsed ? "安全折叠 · 全部真实端口仍可见" : "已展开 · 组织容器",
                    MemberSummary = "成员 " + members.Count.ToString(CultureInfo.InvariantCulture) + "：" + string.Join("、", members.Select(item => "#" + item.SequenceText + " " + item.InstanceName)),
                    BoundarySummary = "边界 IN " + boundaryInputs.Count.ToString(CultureInfo.InvariantCulture) +
                        (boundaryInputs.Count == 0 ? "" : " [" + string.Join("、", boundaryInputs.Distinct().Take(3)) + (boundaryInputs.Distinct().Count() > 3 ? "…" : "") + "]") +
                        " / OUT " + boundaryOutputs.Count.ToString(CultureInfo.InvariantCulture) +
                        (boundaryOutputs.Count == 0 ? "" : " [" + string.Join("、", boundaryOutputs.Distinct().Take(3)) + (boundaryOutputs.Distinct().Count() > 3 ? "…" : "") + "]") +
                        " · 内部连接 " + internalConnections.ToString(CultureInfo.InvariantCulture),
                    ExecutionSummary = "确定性执行 #" + members.First().SequenceText + " → #" + members.Last().SequenceText + " · 不改变 flowTools 顺序",
                    ErrorSummary = errorCount == 0 ? "异常 0" : "异常 " + errorCount.ToString(CultureInfo.InvariantCulture) + " · 请展开检查成员",
                    DebugSummary = "断点 " + breakpointCount.ToString(CultureInfo.InvariantCulture) +
                        (pointer == null ? " · 当前无执行指针" : " · 下一执行 #" + pointer.SequenceText + " " + pointer.InstanceName)
                });
            }
        }

        private static SolidColorBrush GetFlowConnectionBrush(string dataType)
        {
            System.Windows.Media.Color color;
            if (string.Equals(dataType, "Image", StringComparison.OrdinalIgnoreCase)) color = System.Windows.Media.Color.FromRgb(2, 132, 199);
            else if (string.Equals(dataType, "Region", StringComparison.OrdinalIgnoreCase)) color = System.Windows.Media.Color.FromRgb(124, 58, 237);
            else if (string.Equals(dataType, "XLD", StringComparison.OrdinalIgnoreCase)) color = System.Windows.Media.Color.FromRgb(217, 119, 6);
            else if (string.Equals(dataType, "Number", StringComparison.OrdinalIgnoreCase)) color = System.Windows.Media.Color.FromRgb(5, 150, 105);
            else if (string.Equals(dataType, "Error", StringComparison.OrdinalIgnoreCase)) color = System.Windows.Media.Color.FromRgb(220, 38, 38);
            else color = System.Windows.Media.Color.FromRgb(100, 116, 139);
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Geometry CreateFlowConnectionGeometry(double sourceX, double sourceY, double targetX, double targetY)
        {
            double control = Math.Max(65, Math.Min(190, Math.Abs(targetX - sourceX) * 0.45));
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(new System.Windows.Point(sourceX, sourceY), false, false);
                context.BezierTo(
                    new System.Windows.Point(sourceX + control, sourceY),
                    new System.Windows.Point(targetX - control, targetY),
                    new System.Windows.Point(targetX, targetY), true, false);
                context.BeginFigure(new System.Windows.Point(targetX, targetY), false, false);
                context.LineTo(new System.Windows.Point(targetX - 11, targetY - 6), true, false);
                context.BeginFigure(new System.Windows.Point(targetX, targetY), false, false);
                context.LineTo(new System.Windows.Point(targetX - 11, targetY + 6), true, false);
            }
            geometry.Freeze();
            return geometry;
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

            bool enabled = checkBox.IsChecked == true;
            if (tool.IsEnabled == enabled) return;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            tool.IsEnabled = enabled;
            InvalidateToolRunResult(tool, "工具启停已变化");
            InvalidateDownstreamResults(tool, "上游工具启停已变化");
            CommitFlowEditHistory((enabled ? "启用模块：" : "停用模块：") + tool.InstanceName, before);
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
            VmToolInstance instance = new VmToolInstance
            {
                ToolId = string.IsNullOrWhiteSpace(toolId) ? Guid.NewGuid().ToString("N") : toolId,
                Kind = kind,
                InstanceName = string.IsNullOrWhiteSpace(name) ? CreateUniqueToolName(kind) : name,
                IsEnabled = isEnabled,
                InputSummary = DefaultInputSummary(kind),
                OutputSummary = "尚未运行"
            };

            if (kind == VmToolKind.NumericJudge)
            {
                instance.ConnectionStatus = "未连接";
                instance.ConnectionSummary = "Value ← 请选择上游数值端口";
            }
            else if (kind == VmToolKind.ImageSource)
            {
                instance.ConnectionStatus = "待选择图片";
                instance.ConnectionSummary = "本地文件 → Image / SN / Path";
            }
            else if (kind == VmToolKind.RegionMorphology || kind == VmToolKind.RegionFeatureFilter || kind == VmToolKind.RegionContourMeasurement)
            {
                instance.ConnectionStatus = "待连接 Region";
                instance.ConnectionSummary = "Region ← 请选择上游区域";
            }
            else if (kind == VmToolKind.XldContourFilter || kind == VmToolKind.XldContourSegmentation || kind == VmToolKind.XldLineFitting || kind == VmToolKind.XldCircleFitting || kind == VmToolKind.XldEllipseFitting || kind == VmToolKind.XldRectangleFitting)
            {
                instance.ConnectionStatus = "待连接 XLD";
                instance.ConnectionSummary = "XLD ← 请选择上游轮廓";
            }
            else if (kind == VmToolKind.RegionSetOperation)
            {
                instance.ConnectionStatus = "待连接 2 路 Region";
                instance.ConnectionSummary = "RegionA + RegionB ← 请选择两路上游区域";
            }
            else if (kind == VmToolKind.DynamicThreshold)
            {
                instance.ConnectionStatus = "待连接 2 路单通道 Image";
                instance.ConnectionSummary = "OriginalImage + ReferenceImage ← 请选择两路上游图像";
            }
            else if (kind == VmToolKind.ImageDifference)
            {
                instance.ConnectionStatus = "待连接 2 路 Image";
                instance.ConnectionSummary = "ImageA + ImageB ← 请选择两路上游图像";
            }
            else
            {
                instance.ConnectionStatus = "系统输入";
                instance.ConnectionSummary = DefaultInputSummary(kind);
            }

            return instance;
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
                case VmToolKind.ImageSource:
                    return "无输入 · 输出 Image / SN / Path";
                case VmToolKind.ImageChannel:
                    return "Image → HALCON 通道处理 → Image";
                case VmToolKind.ImageFilter:
                    return "Image → HALCON 滤波 → Image";
                case VmToolKind.ImageEnhancement:
                    return "Image → HALCON 图像增强 → Image";
                case VmToolKind.ImageDifference:
                    return "ImageA + ImageB → HALCON 图像差分 → Image";
                case VmToolKind.DynamicThreshold:
                    return "OriginalImage + ReferenceImage → HALCON dyn_threshold → Region";
                case VmToolKind.ImageThreshold:
                    return "Image → HALCON 灰度阈值 → Region";
                case VmToolKind.RegionMorphology:
                    return "Region → HALCON 形态学 → Region";
                case VmToolKind.RegionFeatureFilter:
                    return "Region → HALCON 特征筛选 → Region";
                case VmToolKind.RegionSetOperation:
                    return "RegionA + RegionB → HALCON 集合运算 → Region";
                case VmToolKind.RegionContourMeasurement:
                    return "Region → HALCON 轮廓提取/长度测量 → XLD + Number";
                case VmToolKind.XldContourFilter:
                    return "XLD → HALCON 轮廓筛选 → XLD + Number";
                case VmToolKind.XldContourSegmentation:
                    return "XLD → HALCON 轮廓分段 → XLD + Number";
                case VmToolKind.XldLineFitting:
                    return "XLD → HALCON 鲁棒直线拟合 → XLD + 端点/角度/残差";
                case VmToolKind.XldCircleFitting:
                    return "XLD → HALCON 鲁棒圆拟合 → XLD + 圆心/半径/角度/残差";
                case VmToolKind.XldEllipseFitting:
                    return "XLD → HALCON 鲁棒椭圆拟合 → XLD + 中心/方向/长短轴/残差";
                case VmToolKind.XldRectangleFitting:
                    return "XLD → HALCON 鲁棒旋转矩形拟合 → XLD + 中心/方向/边长/角点/残差";
                case VmToolKind.ShapeMatch:
                    return "Image + SearchROI + ShapeModel";
                case VmToolKind.Blob:
                case VmToolKind.GrayStat:
                case VmToolKind.EdgeMeasure:
                    return "Image + 可选 ROI";
                case VmToolKind.HDevelop:
                    return "Image + ROI";
                case VmToolKind.NumericJudge:
                    return "Value ← 未连接";
                default:
                    return "--";
            }
        }

        private void ApplyFlowFromRecipe(VisionRecipe recipe)
        {
            dockDraftDirty = false;
            dockEditingTool = null;
            isFlowGraphSelecting = false;
            flowGraphSelectionMoved = false;
            flowGraphSelectionBaseline.Clear();
            selectedFlowConnectionKey = null;
            selectedFlowPortToolId = null;
            selectedFlowPortName = null;
            selectedFlowPortDirection = null;
            flowPortDragSession = null;
            flowNodeDragSession = null;
            isDebugBreakpointPause = false;
            debugSingleStepRequested = false;
            debugPauseBeforeNextTool = false;
            debugPausedToolId = null;
            isPauseRequested = false;
            isManualFlowLayout = recipe != null && recipe.ToolFlow != null && recipe.ToolFlow.Any(item =>
                item != null && item.GraphX.HasValue && item.GraphY.HasValue &&
                IsFiniteGraphCoordinate(item.GraphX.Value) && IsFiniteGraphCoordinate(item.GraphY.Value));
            dockInputPortRows.Clear();
            dockRoiBindingRows.Clear();
            if (FlowGraphSelectionRectangle != null)
            {
                FlowGraphSelectionRectangle.Visibility = Visibility.Collapsed;
            }
            if (FlowPortDragPreviewPath != null) FlowPortDragPreviewPath.Visibility = Visibility.Collapsed;
            if (FlowPortDragStatusBorder != null) FlowPortDragStatusBorder.Visibility = Visibility.Collapsed;
            DisposeFlowTools();
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

                    VmToolInstance instance = CreateFlowTool(kind, recipeItem.InstanceName, recipeItem.IsEnabled, recipeItem.ToolId);
                    ApplyToolRecipeData(instance, recipeItem);
                    if (recipeItem.Parameters == null)
                    {
                        ApplyLegacyToolParameters(instance, recipe);
                    }
                    flowTools.Add(instance);
                }

                if (!recipe.ToolFlow.Any(item => item != null && item.RoiIds != null))
                {
                    BindLegacyRoiToFlowTools();
                }
            }

            foreach (VmToolInstance numericTool in flowTools.Where(item => item.Kind == VmToolKind.NumericJudge && string.IsNullOrWhiteSpace(item.InputToolId)))
            {
                AutoBindNumericJudge(numericTool);
            }

            NormalizeFlowGroups();

            if (flowTools.Count == 0)
            {
                VisionRecipe legacy = recipe ?? new VisionRecipe();
                if (legacy.EnableShapeMatch)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.ShapeMatch, null, true, null));
                }
                if (legacy.EnableBlob)
                {
                    VmToolInstance tool = CreateFlowTool(VmToolKind.Blob, null, true, null);
                    ApplyLegacyToolParameters(tool, legacy);
                    flowTools.Add(tool);
                }
                if (legacy.EnableGrayStat)
                {
                    VmToolInstance tool = CreateFlowTool(VmToolKind.GrayStat, null, true, null);
                    ApplyLegacyToolParameters(tool, legacy);
                    flowTools.Add(tool);
                }
                if (legacy.EnableEdgeMeasure)
                {
                    VmToolInstance tool = CreateFlowTool(VmToolKind.EdgeMeasure, null, true, null);
                    ApplyLegacyToolParameters(tool, legacy);
                    flowTools.Add(tool);
                }
                if (legacy.EnableHDevelop)
                {
                    flowTools.Add(CreateFlowTool(VmToolKind.HDevelop, null, true, null));
                }


                BindLegacyRoiToFlowTools();
            }

            SyncLegacyToolChecksFromFlow();
            RefreshRoiLayerBindingSummaries();
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
                IsEnabled = item.IsEnabled,
                RoiIds = item.BoundRoiIds.ToList(),
                Parameters = item.Parameters == null ? null : item.Parameters.Clone(),
                InputBindings = item.InputBindings.Select(binding => binding.Clone()).ToList(),
                GraphX = isManualFlowLayout && item.HasManualGraphPosition ? (double?)item.GraphX : null,
                GraphY = isManualFlowLayout && item.HasManualGraphPosition ? (double?)item.GraphY : null,
                GroupId = item.GroupId,
                GroupName = item.GroupName,
                IsGroupCollapsed = item.IsGroupCollapsed,
                IsBreakpoint = item.IsBreakpoint,
                NumericJudge = item.Kind == VmToolKind.NumericJudge
                    ? new NumericJudgeRecipeData
                    {
                        InputToolId = item.InputToolId,
                        InputPortName = item.InputPortName,
                        Operator = item.NumericOperator,
                        LowerLimit = item.NumericLowerLimit,
                        UpperLimit = item.NumericUpperLimit,
                        Tolerance = item.NumericTolerance
                    }
                    : null
            }).ToList();
        }

        private FlowEditSnapshot CaptureFlowEditSnapshot()
        {
            List<ToolFlowRecipeItem> toolFlow = CaptureFlowRecipe();
            VisionRecipe fingerprintRecipe = new VisionRecipe { ToolFlow = toolFlow };
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            VmToolInstance primary = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            return new FlowEditSnapshot
            {
                ToolFlow = toolFlow,
                SelectedToolIds = selected.Select(item => item.ToolId).ToList(),
                PrimaryToolId = primary == null ? null : primary.ToolId,
                Fingerprint = recipeService.GetRecipeFingerprint(fingerprintRecipe)
            };
        }

        private void CommitFlowEditHistory(string description, FlowEditSnapshot before)
        {
            if (flowEditHistorySuspended || before == null) return;
            FlowEditSnapshot after = CaptureFlowEditSnapshot();
            if (string.Equals(before.Fingerprint, after.Fingerprint, StringComparison.Ordinal))
            {
                RefreshFlowEditHistoryState();
                return;
            }

            flowUndoHistory.Add(new FlowEditHistoryEntry
            {
                Description = string.IsNullOrWhiteSpace(description) ? "流程编辑" : description,
                Before = before,
                After = after
            });
            while (flowUndoHistory.Count > FlowEditHistoryLimit) flowUndoHistory.RemoveAt(0);
            flowRedoHistory.Clear();
            RefreshRecipeDirtyStateAfterFlowEdit(description);
            RefreshFlowEditHistoryState();
        }

        private void ResetFlowEditHistory()
        {
            flowUndoHistory.Clear();
            flowRedoHistory.Clear();
            RefreshFlowEditHistoryState();
        }

        private void RefreshFlowEditHistoryState()
        {
            bool flowBusy = isContinuousRunning || isFlowExecutionActive;
            if (UndoFlowEditButton != null) UndoFlowEditButton.IsEnabled = flowUndoHistory.Count > 0 && !flowBusy;
            if (RedoFlowEditButton != null) RedoFlowEditButton.IsEnabled = flowRedoHistory.Count > 0 && !flowBusy;
            if (UndoFlowEditMenuItem != null) UndoFlowEditMenuItem.IsEnabled = flowUndoHistory.Count > 0 && !flowBusy;
            if (RedoFlowEditMenuItem != null) RedoFlowEditMenuItem.IsEnabled = flowRedoHistory.Count > 0 && !flowBusy;
            if (FlowEditHistoryText != null)
            {
                string undo = flowUndoHistory.Count == 0 ? "无" : flowUndoHistory[flowUndoHistory.Count - 1].Description;
                string redo = flowRedoHistory.Count == 0 ? "无" : flowRedoHistory[flowRedoHistory.Count - 1].Description;
                FlowEditHistoryText.Text = "历史 " + flowUndoHistory.Count.ToString(CultureInfo.InvariantCulture) + "/" + flowRedoHistory.Count.ToString(CultureInfo.InvariantCulture);
                FlowEditHistoryText.ToolTip = "撤销：" + undo + "\n重做：" + redo + "\n最多保留 " + FlowEditHistoryLimit.ToString(CultureInfo.InvariantCulture) + " 次纯配方快照；不保存 HImage/HRegion/XLD。";
            }
        }

        private void RefreshRecipeDirtyStateAfterFlowEdit(string detail)
        {
            if (recipeTrackingSuspended || string.IsNullOrWhiteSpace(savedRecipeFingerprint))
            {
                ScheduleRecipeStateCheck();
                return;
            }

            string fingerprint = recipeService.GetRecipeFingerprint(CaptureRecipe());
            bool dirty = !string.Equals(fingerprint, savedRecipeFingerprint, StringComparison.Ordinal);
            SetRecipeDirtyState(dirty, dirty ? (detail + " · 尚未保存") : (string.IsNullOrWhiteSpace(currentRecipePath) ? "当前为未落盘的新方案" : "方案已保存"));
            ScheduleRecipeStateCheck();
        }

        private void ApplyFlowEditSnapshot(FlowEditSnapshot snapshot)
        {
            if (snapshot == null) return;
            bool previousRecipeTracking = recipeTrackingSuspended;
            flowEditHistorySuspended = true;
            recipeTrackingSuspended = true;
            try
            {
                ApplyFlowFromRecipe(new VisionRecipe { ToolFlow = snapshot.ToolFlow });
                List<VmToolInstance> selected = flowTools.Where(item => snapshot.SelectedToolIds != null && snapshot.SelectedToolIds.Contains(item.ToolId, StringComparer.OrdinalIgnoreCase)).ToList();
                VmToolInstance primary = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, snapshot.PrimaryToolId, StringComparison.OrdinalIgnoreCase));
                if (primary == null) primary = selected.LastOrDefault();
                ApplyFlowToolSelection(selected, primary, false);
            }
            finally
            {
                recipeTrackingSuspended = previousRecipeTracking;
                flowEditHistorySuspended = false;
            }
        }

        private void UndoFlowEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (flowUndoHistory.Count == 0 || isContinuousRunning || isFlowExecutionActive) return;
            if (!ConfirmDockDraftChanges("撤销流程编辑")) return;
            FlowEditHistoryEntry entry = flowUndoHistory[flowUndoHistory.Count - 1];
            flowUndoHistory.RemoveAt(flowUndoHistory.Count - 1);
            flowRedoHistory.Add(entry);
            ApplyFlowEditSnapshot(entry.Before);
            RefreshRecipeDirtyStateAfterFlowEdit("已撤销：" + entry.Description);
            RefreshUiState();
            HeaderStatusText.Text = "已撤销：" + entry.Description + "；运行对象已释放，恢复后的模块等待重新运行。";
            LogInfo(HeaderStatusText.Text);
        }

        private void RedoFlowEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (flowRedoHistory.Count == 0 || isContinuousRunning || isFlowExecutionActive) return;
            if (!ConfirmDockDraftChanges("重做流程编辑")) return;
            FlowEditHistoryEntry entry = flowRedoHistory[flowRedoHistory.Count - 1];
            flowRedoHistory.RemoveAt(flowRedoHistory.Count - 1);
            flowUndoHistory.Add(entry);
            ApplyFlowEditSnapshot(entry.After);
            RefreshRecipeDirtyStateAfterFlowEdit("已重做：" + entry.Description);
            RefreshUiState();
            HeaderStatusText.Text = "已重做：" + entry.Description + "；恢复后的模块等待重新运行。";
            LogInfo(HeaderStatusText.Text);
        }

        private static void ApplyToolRecipeData(VmToolInstance tool, ToolFlowRecipeItem recipeItem)
        {
            if (tool == null || recipeItem == null)
            {
                return;
            }

            tool.ReplaceRoiBindings(recipeItem.RoiIds);
            tool.ReplaceInputBindings(recipeItem.InputBindings);
            tool.Parameters = recipeItem.Parameters == null ? (tool.Parameters ?? new VmToolParameterData()) : recipeItem.Parameters.Clone();
            tool.Parameters.Normalize();
            tool.GroupId = string.IsNullOrWhiteSpace(recipeItem.GroupId) ? null : recipeItem.GroupId.Trim();
            tool.GroupName = string.IsNullOrWhiteSpace(tool.GroupId) ? null : (string.IsNullOrWhiteSpace(recipeItem.GroupName) ? "流程组合" : recipeItem.GroupName.Trim());
            tool.IsGroupCollapsed = !string.IsNullOrWhiteSpace(tool.GroupId) && recipeItem.IsGroupCollapsed;
            tool.IsBreakpoint = recipeItem.IsBreakpoint;
            if (recipeItem.GraphX.HasValue && recipeItem.GraphY.HasValue &&
                IsFiniteGraphCoordinate(recipeItem.GraphX.Value) && IsFiniteGraphCoordinate(recipeItem.GraphY.Value))
            {
                tool.GraphX = Math.Max(0, recipeItem.GraphX.Value);
                tool.GraphY = Math.Max(0, recipeItem.GraphY.Value);
                tool.HasManualGraphPosition = true;
            }

            if (tool.Kind != VmToolKind.NumericJudge || recipeItem.NumericJudge == null)
            {
                return;
            }

            NumericJudgeRecipeData data = recipeItem.NumericJudge;
            tool.InputToolId = data.InputToolId;
            tool.InputPortName = data.InputPortName;
            tool.NumericOperator = NumericJudgeOperatorOption.IsSupported(data.Operator)
                ? data.Operator
                : NumericJudgeOperatorOption.BetweenInclusive;
            tool.NumericLowerLimit = data.LowerLimit;
            tool.NumericUpperLimit = data.UpperLimit;
            tool.NumericTolerance = data.Tolerance < 0 ? 0.001 : data.Tolerance;
        }

        private static bool IsFiniteGraphCoordinate(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 1000000;
        }

        private static void ApplyLegacyToolParameters(VmToolInstance tool, VisionRecipe recipe)
        {
            if (tool == null || recipe == null)
            {
                return;
            }

            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.BlobMinGray = recipe.BlobMinGray;
            parameters.BlobMaxGray = recipe.BlobMaxGray;
            parameters.BlobMinArea = recipe.BlobMinArea;
            parameters.GrayMin = recipe.GrayMin;
            parameters.GrayMax = recipe.GrayMax;
            parameters.EdgeThreshold = recipe.EdgeThreshold;
            parameters.RoiExecutionMode = VmRoiExecutionMode.Union;
            tool.Parameters = parameters;
        }

        private void AutoBindNumericJudge(VmToolInstance judgeTool)
        {
            if (judgeTool == null || judgeTool.Kind != VmToolKind.NumericJudge)
            {
                return;
            }

            int judgeIndex = flowTools.IndexOf(judgeTool);
            IEnumerable<VmToolInstance> candidates = judgeIndex < 0
                ? flowTools.AsEnumerable().Reverse()
                : flowTools.Take(judgeIndex).Reverse();
            VmToolInstance source = candidates.FirstOrDefault(item => ToolMetadata.GetNumericOutputPorts(item.Kind).Count > 0);
            if (source == null)
            {
                return;
            }

            VmPortDefinition port = ToolMetadata.GetNumericOutputPorts(source.Kind).FirstOrDefault();
            if (port == null)
            {
                return;
            }

            judgeTool.InputToolId = source.ToolId;
            judgeTool.InputPortName = port.PortName;
        }

        private VmToolInstance GetInputSourceTool(VmToolInstance tool)
        {
            return tool == null || string.IsNullOrWhiteSpace(tool.InputToolId)
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, tool.InputToolId, StringComparison.OrdinalIgnoreCase));
        }

        private VmToolInstance GetInputSourceTool(VmToolInputBindingData binding)
        {
            return binding == null || string.IsNullOrWhiteSpace(binding.SourceToolId)
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, binding.SourceToolId, StringComparison.OrdinalIgnoreCase));
        }

        private string GetInputBindingConfigurationError(VmToolInstance target, VmToolInputBindingData binding)
        {
            if (target == null || binding == null)
            {
                return string.Empty;
            }

            VmPortDefinition targetPort = ToolMetadata.GetInputPorts(target.Kind)
                .FirstOrDefault(item => string.Equals(item.PortName, binding.TargetPortName, StringComparison.OrdinalIgnoreCase));
            if (targetPort == null)
            {
                return "目标输入端口不存在：" + binding.TargetPortName;
            }

            VmToolInstance source = GetInputSourceTool(binding);
            if (source == null)
            {
                return "上游工具不存在或已删除。";
            }

            int sourceIndex = flowTools.IndexOf(source);
            int targetIndex = flowTools.IndexOf(target);
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex >= targetIndex)
            {
                return "输入来源必须位于当前工具之前。";
            }

            if (!source.IsEnabled)
            {
                return "上游工具已停用。";
            }

            VmPortDefinition sourcePort = ToolMetadata.GetOutputPorts(source.Kind)
                .FirstOrDefault(item => string.Equals(item.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
            if (sourcePort == null)
            {
                return "上游输出端口不存在：" + binding.SourcePortName;
            }

            if (!string.Equals(sourcePort.DataType, targetPort.DataType, StringComparison.OrdinalIgnoreCase))
            {
                return "端口类型不匹配：" + sourcePort.DataType + " → " + targetPort.DataType;
            }

            if (string.Equals(targetPort.DataType, "Region", StringComparison.OrdinalIgnoreCase))
            {
                VmToolParameterData parameters = target.Parameters ?? new VmToolParameterData();
                if (VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi)
                {
                    return "逐 ROI 模式必须使用本地图层和稳定 RoiId；请切换为合并 ROI 或断开 Region 订阅。";
                }
            }

            return string.Empty;
        }

        private string GetRegionSetInputConfigurationError(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.RegionSetOperation)
            {
                return string.Empty;
            }

            VmToolInputBindingData regionA = tool.GetInputBinding("RegionA");
            VmToolInputBindingData regionB = tool.GetInputBinding("RegionB");
            if (regionA == null || regionB == null)
            {
                return "Region 集合运算必须同时连接 RegionA 与 RegionB。";
            }

            string errorA = GetInputBindingConfigurationError(tool, regionA);
            if (!string.IsNullOrWhiteSpace(errorA)) return "RegionA：" + errorA;
            string errorB = GetInputBindingConfigurationError(tool, regionB);
            if (!string.IsNullOrWhiteSpace(errorB)) return "RegionB：" + errorB;
            if (string.Equals(regionA.SourceToolId, regionB.SourceToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(regionA.SourcePortName, regionB.SourcePortName, StringComparison.OrdinalIgnoreCase))
            {
                return "RegionA 与 RegionB 不能订阅同一个上游端口；请选择两路不同的 Region 来源。";
            }

            return string.Empty;
        }

        private HRegion CreateEffectiveRoiRegion(VmToolInstance tool, bool required)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("ROI");
            if (binding == null)
            {
                return CreateBoundRoiRegion(tool, required);
            }

            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            VmToolInstance source = GetInputSourceTool(binding);
            if (!string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(source.InstanceName + " 尚无本周期 OK 结果，请先按流程顺序运行上游工具。");
            }

            object raw;
            VmRegionSnapshot snapshot = source.TryGetOutputValue(binding.SourcePortName, out raw) ? raw as VmRegionSnapshot : null;
            if (snapshot == null || snapshot.IsDisposed)
            {
                throw new InvalidOperationException(source.InstanceName + "." + binding.SourcePortName + " 没有可用 Region 快照，请重新运行上游工具。");
            }

            return snapshot.CreateRegionCopy();
        }

        private HRegion CreateToolInputRegion(VmToolInstance tool, string portName)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(portName);
            if (binding == null)
            {
                throw new InvalidOperationException((tool == null ? "当前工具" : tool.InstanceName) + "." + portName + " 必须订阅前序 Region 输出。");
            }

            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            VmToolInstance source = GetInputSourceTool(binding);
            if (!string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(source.InstanceName + " 尚无本周期 OK Region，请先按流程顺序运行上游工具。");
            }

            object raw;
            VmRegionSnapshot snapshot = source.TryGetOutputValue(binding.SourcePortName, out raw) ? raw as VmRegionSnapshot : null;
            if (snapshot == null || snapshot.IsDisposed)
            {
                throw new InvalidOperationException(source.InstanceName + "." + binding.SourcePortName + " 没有可用 Region 快照，请重新运行上游工具。");
            }

            return snapshot.CreateRegionCopy();
        }

        private HObject CreateToolInputXld(VmToolInstance tool, string portName)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(portName);
            if (binding == null)
            {
                throw new InvalidOperationException((tool == null ? "当前工具" : tool.InstanceName) + "." + portName + " 必须订阅前序 XLD 输出。");
            }

            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            VmToolInstance source = GetInputSourceTool(binding);
            if (!string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(source.InstanceName + " 尚无本周期 OK XLD，请先按流程顺序运行上游工具。");
            }

            object raw;
            VmXldSnapshot snapshot = source.TryGetOutputValue(binding.SourcePortName, out raw) ? raw as VmXldSnapshot : null;
            if (snapshot == null || snapshot.IsDisposed)
            {
                throw new InvalidOperationException(source.InstanceName + "." + binding.SourcePortName + " 没有可用 XLD 快照，请重新运行上游工具。");
            }

            return snapshot.CreateXldCopy();
        }

        private string GetRoiInputSummary(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("ROI");
            if (binding == null)
            {
                return GetRoiBindingSummary(tool);
            }

            VmToolInstance source = GetInputSourceTool(binding);
            return source == null
                ? "Region 来源无效"
                : "订阅 " + source.InstanceName + "." + binding.SourcePortName;
        }

        private VmImageSnapshot GetImageSnapshot(VmToolInstance source, string portName)
        {
            object raw;
            return source != null && source.TryGetOutputValue(portName, out raw)
                ? raw as VmImageSnapshot
                : null;
        }

        private string GetImageInputSummary(VmToolInstance tool)
        {
            return GetImageInputSummary(tool, "Image");
        }

        private string GetImageInputSummary(VmToolInstance tool, string portName)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(portName);
            if (binding == null)
            {
                return string.Equals(portName, "Image", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(currentImagePath)
                    ? "系统.Image"
                    : (string.Equals(portName, "Image", StringComparison.OrdinalIgnoreCase)
                        ? "系统.Image（" + Path.GetFileName(currentImagePath) + "）"
                        : portName + " 未连接");
            }

            VmToolInstance source = GetInputSourceTool(binding);
            return source == null
                ? "Image 来源无效"
                : "订阅 " + source.InstanceName + "." + binding.SourcePortName;
        }

        private HImage CreateToolInputImage(VmToolInstance tool)
        {
            return CreateToolInputImage(tool, "Image", true);
        }

        private HImage CreateToolInputImage(VmToolInstance tool, string portName, bool allowSystemImage)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(portName);
            if (binding == null)
            {
                if (!allowSystemImage)
                {
                    throw new InvalidOperationException((tool == null ? "当前工具" : tool.InstanceName) + "." + portName + " 必须订阅前序 Image 输出。");
                }
                EnsureImage();
                return currentImage.CopyImage();
            }

            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            VmToolInstance source = GetInputSourceTool(binding);
            if (!string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(source.InstanceName + " 尚无本周期 OK 图像，请先按流程顺序运行上游工具。");
            }

            VmImageSnapshot snapshot = GetImageSnapshot(source, binding.SourcePortName);
            if (snapshot == null || snapshot.IsDisposed)
            {
                throw new InvalidOperationException(source.InstanceName + "." + binding.SourcePortName + " 没有可用 Image 快照，请重新运行上游工具。");
            }

            return snapshot.CreateImageCopy();
        }

        private bool HasToolInputImage(VmToolInstance tool)
        {
            return HasToolInputImage(tool, "Image", true);
        }

        private bool HasToolInputImage(VmToolInstance tool, string portName, bool allowSystemImage)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(portName);
            if (binding == null)
            {
                return allowSystemImage && currentImage != null;
            }

            if (!string.IsNullOrWhiteSpace(GetInputBindingConfigurationError(tool, binding)))
            {
                return false;
            }

            VmToolInstance source = GetInputSourceTool(binding);
            VmImageSnapshot snapshot = GetImageSnapshot(source, binding.SourcePortName);
            return source != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && snapshot != null && !snapshot.IsDisposed;
        }

        private string GetDynamicThresholdInputConfigurationError(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.DynamicThreshold)
            {
                return string.Empty;
            }

            VmToolInputBindingData original = tool.GetInputBinding("OriginalImage");
            VmToolInputBindingData reference = tool.GetInputBinding("ReferenceImage");
            if (original == null || reference == null)
            {
                return "动态阈值分割必须同时连接 OriginalImage 与 ReferenceImage。";
            }

            string originalError = GetInputBindingConfigurationError(tool, original);
            if (!string.IsNullOrWhiteSpace(originalError)) return "OriginalImage：" + originalError;
            string referenceError = GetInputBindingConfigurationError(tool, reference);
            if (!string.IsNullOrWhiteSpace(referenceError)) return "ReferenceImage：" + referenceError;
            if (string.Equals(original.SourceToolId, reference.SourceToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(original.SourcePortName, reference.SourcePortName, StringComparison.OrdinalIgnoreCase))
            {
                return "OriginalImage 与 ReferenceImage 不能订阅同一个上游端口。";
            }

            return string.Empty;
        }

        private string GetImageDifferenceInputConfigurationError(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.ImageDifference)
            {
                return string.Empty;
            }

            VmToolInputBindingData imageA = tool.GetInputBinding("ImageA");
            VmToolInputBindingData imageB = tool.GetInputBinding("ImageB");
            if (imageA == null || imageB == null)
            {
                return "双图像差分必须同时连接 ImageA 与 ImageB。";
            }

            string imageAError = GetInputBindingConfigurationError(tool, imageA);
            if (!string.IsNullOrWhiteSpace(imageAError)) return "ImageA：" + imageAError;
            string imageBError = GetInputBindingConfigurationError(tool, imageB);
            if (!string.IsNullOrWhiteSpace(imageBError)) return "ImageB：" + imageBError;
            if (string.Equals(imageA.SourceToolId, imageB.SourceToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(imageA.SourcePortName, imageB.SourcePortName, StringComparison.OrdinalIgnoreCase))
            {
                return "ImageA 与 ImageB 不能订阅同一个上游端口。";
            }

            return string.Empty;
        }

        private static int GetImageWidth(HImage image)
        {
            HTuple width;
            HTuple height;
            HOperatorSet.GetImageSize(image, out width, out height);
            return width.I;
        }

        private static int GetImageHeight(HImage image)
        {
            HTuple width;
            HTuple height;
            HOperatorSet.GetImageSize(image, out width, out height);
            return height.I;
        }

        private string GetNumericJudgeConfigurationError(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.NumericJudge)
            {
                return "不是数值判定工具。";
            }

            VmToolInstance source = GetInputSourceTool(tool);
            if (source == null)
            {
                return "未选择上游工具。";
            }

            int sourceIndex = flowTools.IndexOf(source);
            int judgeIndex = flowTools.IndexOf(tool);
            if (sourceIndex < 0 || judgeIndex < 0 || sourceIndex >= judgeIndex)
            {
                return "上游工具必须位于数值判定之前。";
            }

            if (!source.IsEnabled)
            {
                return "上游工具已停用。";
            }

            VmPortDefinition port = ToolMetadata.GetNumericOutputPorts(source.Kind)
                .FirstOrDefault(item => string.Equals(item.PortName, tool.InputPortName, StringComparison.OrdinalIgnoreCase));
            if (port == null)
            {
                return "所选上游端口不存在或不是数值类型。";
            }

            return VmNumericJudgeParameterValidator.Validate(
                tool.NumericOperator,
                tool.NumericLowerLimit,
                tool.NumericUpperLimit,
                tool.NumericTolerance);
        }

        private void RefreshNumericJudgeConnectionStatus(VmToolInstance tool)
        {
            VmToolInstance source = GetInputSourceTool(tool);
            string sourceName = source == null ? "未选择" : source.InstanceName;
            string portName = string.IsNullOrWhiteSpace(tool.InputPortName) ? "未选择" : tool.InputPortName;
            string error = GetNumericJudgeConfigurationError(tool);
            tool.ConnectionSummary = sourceName + "." + portName + " → Value";
            tool.InputSummary = "Value ← " + sourceName + "." + portName;
            if (string.IsNullOrWhiteSpace(error))
            {
                tool.ConnectionStatus = source.TryGetNumericOutput(tool.InputPortName, out _) ? "已连接 · 有值" : "已连接 · 等待运行";
                tool.ConfigurationStatus = "就绪";
            }
            else
            {
                tool.ConnectionStatus = "连接异常";
                tool.ConfigurationStatus = error;
            }
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
                ImageSourceInspectorPanel.Visibility = Visibility.Collapsed;
                ShapeInspectorPanel.Visibility = Visibility.Collapsed;
                BlobInspectorPanel.Visibility = Visibility.Collapsed;
                GrayInspectorPanel.Visibility = Visibility.Collapsed;
                EdgeInspectorPanel.Visibility = Visibility.Collapsed;
                HDevInspectorPanel.Visibility = Visibility.Collapsed;
                NumericJudgeInspectorPanel.Visibility = Visibility.Collapsed;

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
                    SelectedResultToolText.Text = "--";
                    SelectedResultStatusText.Text = "--";
                    SelectedResultOutputText.Text = "--";
                    SelectedResultErrorText.Text = "--";
                    SelectedRoiResultsDataGrid.ItemsSource = null;
                    RefreshRoiLayerContextResults(null);
                    RefreshPortPanel(null);
                    RefreshRoiBindingEditor();
                    LoadDockConfigurationDraft(null);
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
                SelectedResultToolText.Text = selected.InstanceName + " · " + selected.DisplayType;
                SelectedResultStatusText.Text = selected.ResultCode + " · " + selected.RunStatus + " · " + selected.ElapsedText;
                SelectedResultOutputText.Text = string.IsNullOrWhiteSpace(selected.OutputSummary) ? "--" : selected.OutputSummary;
                SelectedResultErrorText.Text = string.IsNullOrWhiteSpace(selected.ErrorMessage) ? "--" : selected.ErrorMessage;
                SelectedRoiResultsDataGrid.ItemsSource = selected.RoiResults;
                VmToolParameterData selectedParameters = selected.Parameters ?? new VmToolParameterData();
                BlobMinGrayTextBox.Text = selectedParameters.BlobMinGray.ToString("0.###", CultureInfo.InvariantCulture);
                BlobMaxGrayTextBox.Text = selectedParameters.BlobMaxGray.ToString("0.###", CultureInfo.InvariantCulture);
                BlobMinAreaTextBox.Text = selectedParameters.BlobMinArea.ToString("0.###", CultureInfo.InvariantCulture);
                GrayMinTextBox.Text = selectedParameters.GrayMin.ToString("0.###", CultureInfo.InvariantCulture);
                GrayMaxTextBox.Text = selectedParameters.GrayMax.ToString("0.###", CultureInfo.InvariantCulture);
                EdgeThresholdTextBox.Text = selectedParameters.EdgeThreshold.ToString("0.###", CultureInfo.InvariantCulture);
                ImageSourcePathText.Text = "文件：" + (string.IsNullOrWhiteSpace(selectedParameters.LocalImagePath) ? "--" : selectedParameters.LocalImagePath);
                ImageSourceSerialText.Text = "SN：" + (string.IsNullOrWhiteSpace(selectedParameters.LocalImageSerialNumber) ? "--" : selectedParameters.LocalImageSerialNumber);

                switch (selected.Kind)
                {
                    case VmToolKind.ImageSource:
                        ImageSourceInspectorPanel.Visibility = Visibility.Visible;
                        break;
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
                    case VmToolKind.NumericJudge:
                        NumericJudgeInspectorPanel.Visibility = Visibility.Visible;
                        RefreshNumericJudgeEditor(selected);
                        break;
                }

                if (!ReferenceEquals(dockEditingTool, selected))
                {
                    LoadDockConfigurationDraft(selected);
                }
                else
                {
                    RefreshDockConfigurationRuntime(selected);
                }

                RefreshPortPanel(selected);
                RefreshRoiBindingEditor();
                RefreshRoiLayerContextResults(selected);
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
                tool.ConnectionStatus = "已停用";
                return;
            }

            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            string parameterError = parameters.Validate(tool.Kind);
            if (!string.IsNullOrWhiteSpace(parameterError))
            {
                tool.ConfigurationStatus = "参数异常";
                tool.ConnectionStatus = parameterError;
                if (tool.Kind == VmToolKind.ImageSource) tool.ConnectionSummary = "本地文件 → Image / SN / Path";
                else if (tool.Kind == VmToolKind.ImageDifference) tool.ConnectionSummary = "ImageA + ImageB → 双图像差分";
                else if (tool.Kind == VmToolKind.DynamicThreshold) tool.ConnectionSummary = "OriginalImage + ReferenceImage → 动态阈值分割";
                else if (tool.Kind == VmToolKind.ImageChannel || tool.Kind == VmToolKind.ImageFilter || tool.Kind == VmToolKind.ImageEnhancement || tool.Kind == VmToolKind.ImageThreshold) tool.ConnectionSummary = "Image → " + tool.DisplayType;
                else if (tool.Kind == VmToolKind.RegionSetOperation) tool.ConnectionSummary = "RegionA + RegionB → " + tool.DisplayType;
                else if (tool.Kind == VmToolKind.RegionMorphology || tool.Kind == VmToolKind.RegionFeatureFilter || tool.Kind == VmToolKind.RegionContourMeasurement) tool.ConnectionSummary = "Region → " + tool.DisplayType;
                else if (tool.Kind == VmToolKind.XldContourFilter || tool.Kind == VmToolKind.XldContourSegmentation || tool.Kind == VmToolKind.XldLineFitting || tool.Kind == VmToolKind.XldCircleFitting || tool.Kind == VmToolKind.XldEllipseFitting || tool.Kind == VmToolKind.XldRectangleFitting) tool.ConnectionSummary = "XLD → " + tool.DisplayType;
                else tool.ConnectionSummary = "系统.Image + " + GetRoiBindingSummary(tool);
                return;
            }

            string inputBindingError = tool.InputBindings
                .Select(binding => GetInputBindingConfigurationError(tool, binding))
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error));
            if (!string.IsNullOrWhiteSpace(inputBindingError))
            {
                tool.ConfigurationStatus = "输入异常";
                tool.ConnectionStatus = inputBindingError;
                tool.ConnectionSummary = tool.Kind == VmToolKind.RegionSetOperation
                    ? "RegionA + RegionB ← 输入连接异常"
                    : (tool.Kind == VmToolKind.XldContourFilter || tool.Kind == VmToolKind.XldContourSegmentation || tool.Kind == VmToolKind.XldLineFitting || tool.Kind == VmToolKind.XldCircleFitting || tool.Kind == VmToolKind.XldEllipseFitting || tool.Kind == VmToolKind.XldRectangleFitting ? "XLD ← 输入连接异常" : GetRoiInputSummary(tool));
                return;
            }

            string regionSetInputError = GetRegionSetInputConfigurationError(tool);
            if (!string.IsNullOrWhiteSpace(regionSetInputError))
            {
                tool.ConfigurationStatus = "输入异常";
                tool.ConnectionStatus = regionSetInputError;
                tool.ConnectionSummary = "RegionA + RegionB ← 需要两路不同的前序 Region";
                return;
            }

            string dynamicThresholdInputError = GetDynamicThresholdInputConfigurationError(tool);
            if (!string.IsNullOrWhiteSpace(dynamicThresholdInputError))
            {
                tool.ConfigurationStatus = "输入异常";
                tool.ConnectionStatus = dynamicThresholdInputError;
                tool.ConnectionSummary = "OriginalImage + ReferenceImage ← 需要两路不同的前序单通道 Image";
                return;
            }

            string imageDifferenceInputError = GetImageDifferenceInputConfigurationError(tool);
            if (!string.IsNullOrWhiteSpace(imageDifferenceInputError))
            {
                tool.ConfigurationStatus = "输入异常";
                tool.ConnectionStatus = imageDifferenceInputError;
                tool.ConnectionSummary = "ImageA + ImageB ← 需要两路不同的前序同规格 Image";
                return;
            }

            switch (tool.Kind)
            {
                case VmToolKind.ImageSource:
                    tool.ConfigurationStatus = "就绪";
                    tool.ConnectionStatus = tool.TryGetOutputValue("Image", out _) ? "本地文件 · 有值" : "本地文件 · 等待运行";
                    tool.ConnectionSummary = Path.GetFileName(parameters.LocalImagePath) + " → Image / SN / Path";
                    break;
                case VmToolKind.NumericJudge:
                    RefreshNumericJudgeConnectionStatus(tool);
                    break;
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionContourMeasurement:
                {
                    VmToolInputBindingData requiredRegionBinding = tool.GetInputBinding("Region");
                    VmToolInstance requiredRegionSource = GetInputSourceTool(requiredRegionBinding);
                    object requiredRegionRaw;
                    VmRegionSnapshot requiredRegionSnapshot = requiredRegionSource != null && requiredRegionBinding != null && requiredRegionSource.TryGetOutputValue(requiredRegionBinding.SourcePortName, out requiredRegionRaw)
                        ? requiredRegionRaw as VmRegionSnapshot
                        : null;
                    bool hasRegion = requiredRegionBinding != null && requiredRegionSource != null &&
                                     string.Equals(requiredRegionSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) &&
                                     requiredRegionSnapshot != null && !requiredRegionSnapshot.IsDisposed;
                    tool.ConfigurationStatus = requiredRegionBinding == null ? "待连接 Region" : (hasRegion ? "就绪" : "等待上游");
                    tool.ConnectionStatus = requiredRegionBinding == null
                        ? "Region 未连接"
                        : (hasRegion ? "Region 已连接 · 有值" : "Region 已连接 · 等待上游 OK");
                    tool.ConnectionSummary = requiredRegionSource == null || requiredRegionBinding == null
                        ? "Region ← 请选择上游区域"
                        : requiredRegionSource.InstanceName + "." + requiredRegionBinding.SourcePortName + " → Region";
                    break;
                }
                case VmToolKind.XldContourFilter:
                case VmToolKind.XldContourSegmentation:
                case VmToolKind.XldLineFitting:
                case VmToolKind.XldCircleFitting:
                case VmToolKind.XldEllipseFitting:
                case VmToolKind.XldRectangleFitting:
                {
                    VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                    VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                    object xldRaw;
                    VmXldSnapshot xldSnapshot = xldSource != null && xldBinding != null && xldSource.TryGetOutputValue(xldBinding.SourcePortName, out xldRaw)
                        ? xldRaw as VmXldSnapshot
                        : null;
                    bool hasXld = xldBinding != null && xldSource != null &&
                                  string.Equals(xldSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) &&
                                  xldSnapshot != null && !xldSnapshot.IsDisposed;
                    tool.ConfigurationStatus = xldBinding == null ? "待连接 XLD" : (hasXld ? "就绪" : "等待上游");
                    tool.ConnectionStatus = xldBinding == null
                        ? "XLD 未连接"
                        : (hasXld ? "XLD 已连接 · 有值" : "XLD 已连接 · 等待上游 OK");
                    tool.ConnectionSummary = xldSource == null || xldBinding == null
                        ? "XLD ← 请选择上游轮廓"
                        : xldSource.InstanceName + "." + xldBinding.SourcePortName + " → XLD";
                    break;
                }
                case VmToolKind.RegionSetOperation:
                {
                    VmToolInputBindingData regionA = tool.GetInputBinding("RegionA");
                    VmToolInputBindingData regionB = tool.GetInputBinding("RegionB");
                    VmToolInstance sourceA = GetInputSourceTool(regionA);
                    VmToolInstance sourceB = GetInputSourceTool(regionB);
                    object rawA;
                    object rawB;
                    VmRegionSnapshot snapshotA = sourceA != null && regionA != null && sourceA.TryGetOutputValue(regionA.SourcePortName, out rawA) ? rawA as VmRegionSnapshot : null;
                    VmRegionSnapshot snapshotB = sourceB != null && regionB != null && sourceB.TryGetOutputValue(regionB.SourcePortName, out rawB) ? rawB as VmRegionSnapshot : null;
                    bool readyA = sourceA != null && string.Equals(sourceA.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && snapshotA != null && !snapshotA.IsDisposed;
                    bool readyB = sourceB != null && string.Equals(sourceB.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && snapshotB != null && !snapshotB.IsDisposed;
                    bool ready = readyA && readyB;
                    tool.ConfigurationStatus = ready ? "就绪" : "等待上游";
                    tool.ConnectionStatus = ready
                        ? "2 路 Region 已连接 · 有值"
                        : "2 路 Region 已连接 · 等待上游 OK";
                    tool.ConnectionSummary = sourceA.InstanceName + "." + regionA.SourcePortName + " + " + sourceB.InstanceName + "." + regionB.SourcePortName + " → Region";
                    break;
                }
                case VmToolKind.DynamicThreshold:
                {
                    VmToolInputBindingData originalBinding = tool.GetInputBinding("OriginalImage");
                    VmToolInputBindingData referenceBinding = tool.GetInputBinding("ReferenceImage");
                    VmToolInstance originalSource = GetInputSourceTool(originalBinding);
                    VmToolInstance referenceSource = GetInputSourceTool(referenceBinding);
                    bool originalReady = HasToolInputImage(tool, "OriginalImage", false);
                    bool referenceReady = HasToolInputImage(tool, "ReferenceImage", false);
                    bool ready = originalReady && referenceReady;
                    tool.ConfigurationStatus = ready ? "就绪" : "等待上游";
                    tool.ConnectionStatus = ready ? "2 路单通道 Image 已连接 · 有值" : "2 路 Image 已连接 · 等待上游 OK";
                    tool.ConnectionSummary = originalSource.InstanceName + "." + originalBinding.SourcePortName + " + " + referenceSource.InstanceName + "." + referenceBinding.SourcePortName + " → Region";
                    break;
                }
                case VmToolKind.ImageDifference:
                {
                    VmToolInputBindingData imageABinding = tool.GetInputBinding("ImageA");
                    VmToolInputBindingData imageBBinding = tool.GetInputBinding("ImageB");
                    VmToolInstance imageASource = GetInputSourceTool(imageABinding);
                    VmToolInstance imageBSource = GetInputSourceTool(imageBBinding);
                    bool imageAReady = HasToolInputImage(tool, "ImageA", false);
                    bool imageBReady = HasToolInputImage(tool, "ImageB", false);
                    bool ready = imageAReady && imageBReady;
                    tool.ConfigurationStatus = ready ? "就绪" : "等待上游";
                    tool.ConnectionStatus = ready ? "2 路 Image 已连接 · 有值" : "2 路 Image 已连接 · 等待上游 OK";
                    tool.ConnectionSummary = imageASource.InstanceName + "." + imageABinding.SourcePortName + " + " + imageBSource.InstanceName + "." + imageBBinding.SourcePortName + " → Image";
                    break;
                }
                case VmToolKind.ShapeMatch:
                    tool.ConfigurationStatus = currentTemplateItem == null || !currentTemplateItem.HasModel
                        ? "待配置模板"
                        : (GetBoundRoiLayers(tool).Count == 0 ? "待绑定 ROI" : "就绪");
                    tool.ConnectionStatus = tool.ConfigurationStatus == "就绪" ? "系统输入 · 已就绪" : "系统输入 · 待配置";
                    tool.ConnectionSummary = "系统.Image + " + GetRoiBindingSummary(tool) + " + ShapeModel";
                    break;
                case VmToolKind.HDevelop:
                    tool.ConfigurationStatus = string.IsNullOrWhiteSpace(HDevPathTextBox.Text)
                        ? "待选择程序"
                        : (GetBoundRoiLayers(tool).Count == 0 ? "待绑定 ROI" : "就绪");
                    tool.ConnectionStatus = tool.ConfigurationStatus == "就绪" ? "系统输入 · 已就绪" : "系统输入 · 待配置";
                    tool.ConnectionSummary = "系统.Image + " + GetRoiBindingSummary(tool) + " + Program";
                    break;
                default:
                    bool perRoiRequiresBinding = (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure) &&
                                                 VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi;
                    bool hasPerRoiBinding = GetBoundRoiLayers(tool).Count > 0;
                    bool hasToolImage = HasToolInputImage(tool);
                    VmToolInputBindingData imageBinding = tool.GetInputBinding("Image");
                    tool.ConfigurationStatus = !hasToolImage
                        ? "等待图像"
                        : (perRoiRequiresBinding && !hasPerRoiBinding ? "待绑定 ROI" : "就绪");
                    VmToolInputBindingData regionBinding = tool.GetInputBinding("ROI");
                    VmToolInstance regionSource = GetInputSourceTool(regionBinding);
                    object regionRaw;
                    VmRegionSnapshot regionSnapshot = regionSource != null && regionSource.TryGetOutputValue(regionBinding == null ? null : regionBinding.SourcePortName, out regionRaw)
                        ? regionRaw as VmRegionSnapshot
                        : null;
                    tool.ConnectionStatus = !hasToolImage
                        ? (imageBinding == null ? "系统输入 · 等待图像" : "Image 已连接 · 等待上游")
                        : (perRoiRequiresBinding && !hasPerRoiBinding
                            ? (imageBinding == null ? "系统输入 · 等待 ROI" : "Image 已连接 · 等待 ROI")
                            : (regionBinding == null
                                ? (imageBinding == null ? "系统输入 · 已就绪" : "Image 已连接 · 有值")
                                : (regionSnapshot == null || regionSnapshot.IsDisposed ? "Region 已连接 · 等待上游" : "Region 已连接 · 有值")));
                    tool.ConnectionSummary = tool.Kind == VmToolKind.ImageChannel || tool.Kind == VmToolKind.ImageFilter || tool.Kind == VmToolKind.ImageEnhancement
                        ? GetImageInputSummary(tool) + " → Image"
                        : (tool.Kind == VmToolKind.ImageThreshold
                            ? GetImageInputSummary(tool) + " → Region"
                            : GetImageInputSummary(tool) + " + " + GetRoiInputSummary(tool));
                    break;
            }
        }

        private void RefreshNumericJudgeEditor(VmToolInstance tool)
        {
            int judgeIndex = flowTools.IndexOf(tool);
            List<VmSourceToolOption> sourceOptions = flowTools
                .Take(Math.Max(0, judgeIndex))
                .Where(item => ToolMetadata.GetNumericOutputPorts(item.Kind).Count > 0)
                .Select(item => new VmSourceToolOption
                {
                    ToolId = item.ToolId,
                    DisplayText = item.SequenceText + "  " + item.InstanceName
                })
                .ToList();
            NumericSourceToolComboBox.ItemsSource = sourceOptions;
            NumericSourceToolComboBox.SelectedValue = tool.InputToolId;

            VmToolInstance source = GetInputSourceTool(tool);
            List<VmSourcePortOption> portOptions = source == null
                ? new List<VmSourcePortOption>()
                : ToolMetadata.GetNumericOutputPorts(source.Kind)
                    .Select(item => new VmSourcePortOption
                    {
                        PortName = item.PortName,
                        DisplayText = item.DisplayName + "  (" + item.PortName + ")",
                        DataType = item.DataType
                    })
                    .ToList();
            NumericSourcePortComboBox.ItemsSource = portOptions;
            NumericSourcePortComboBox.SelectedValue = tool.InputPortName;
            NumericOperatorComboBox.SelectedValue = tool.NumericOperator;
            NumericLowerLimitTextBox.Text = tool.NumericLowerLimit.ToString("0.###", CultureInfo.InvariantCulture);
            NumericUpperLimitTextBox.Text = tool.NumericUpperLimit.ToString("0.###", CultureInfo.InvariantCulture);
            NumericToleranceTextBox.Text = tool.NumericTolerance.ToString("0.###", CultureInfo.InvariantCulture);

            string error = GetNumericJudgeConfigurationError(tool);
            NumericConfigValidationText.Text = string.IsNullOrWhiteSpace(error)
                ? "连接有效。运行全流程时将读取上游本周期数值；独立运行使用上游最近一次结果。"
                : "配置异常：" + error;
            NumericConfigValidationText.Foreground = string.IsNullOrWhiteSpace(error)
                ? System.Windows.Media.Brushes.SeaGreen
                : System.Windows.Media.Brushes.Firebrick;
        }

        private void RefreshPortPanel(VmToolInstance tool)
        {
            inputPortRows.Clear();
            outputPortRows.Clear();

            if (tool == null)
            {
                IoToolTitleText.Text = "未选择流程工具";
                IoConnectionStatusText.Text = "请在流程区选择工具实例。";
                IoRunCurrentButton.IsEnabled = false;
                IoOpenParametersButton.IsEnabled = false;
                return;
            }

            IoToolTitleText.Text = tool.SequenceText + "  " + tool.InstanceName;
            IoConnectionStatusText.Text = tool.ConnectionStatus + " · " + tool.ConnectionSummary;
            IoRunCurrentButton.IsEnabled = tool.IsEnabled;
            IoOpenParametersButton.IsEnabled = true;

            foreach (VmPortDefinition port in ToolMetadata.GetInputPorts(tool.Kind))
            {
                inputPortRows.Add(BuildInputPortRow(tool, port));
            }

            foreach (VmPortDefinition port in ToolMetadata.GetOutputPorts(tool.Kind))
            {
                object raw;
                bool hasValue = tool.TryGetOutputValue(port.PortName, out raw);
                outputPortRows.Add(new VmPortDisplayItem
                {
                    Direction = "OUT",
                    PortName = port.PortName,
                    DisplayName = port.DisplayName,
                    DataType = port.DataType,
                    Source = tool.InstanceName + "." + port.PortName,
                    CurrentValue = hasValue ? tool.GetFormattedOutput(port.PortName) : "--",
                    Status = hasValue ? "有值" : "尚未运行",
                    IsConnected = hasValue
                });
            }
        }

        private VmPortDisplayItem BuildInputPortRow(VmToolInstance tool, VmPortDefinition port)
        {
            string source = "系统." + port.PortName;
            string currentValue = "--";
            string status = "未连接";
            bool connected = false;

            if (tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value")
            {
                VmToolInstance sourceTool = GetInputSourceTool(tool);
                source = sourceTool == null
                    ? "未选择"
                    : sourceTool.InstanceName + "." + (tool.InputPortName ?? "未选择");
                double numericValue;
                connected = string.IsNullOrWhiteSpace(GetNumericJudgeConfigurationError(tool));
                if (sourceTool != null && sourceTool.TryGetNumericOutput(tool.InputPortName, out numericValue))
                {
                    currentValue = numericValue.ToString("0.###", CultureInfo.InvariantCulture);
                    status = connected ? "已连接 · 有值" : "连接异常 · 有历史值";
                }
                else
                {
                    status = connected ? "已连接 · 等待运行" : "连接异常";
                }
            }
            else if (port.PortName == "Image")
            {
                VmToolInputBindingData binding = tool.GetInputBinding("Image");
                if (binding == null)
                {
                    source = "系统.Image（当前图像）";
                    connected = currentImage != null;
                    currentValue = connected
                        ? string.Format(CultureInfo.InvariantCulture, "{0}×{1}", GetImageWidth(currentImage), GetImageHeight(currentImage))
                        : "--";
                    status = connected ? "默认输入 · 有值" : "默认输入 · 等待图像";
                }
                else
                {
                    VmToolInstance sourceTool = GetInputSourceTool(binding);
                    string bindingError = GetInputBindingConfigurationError(tool, binding);
                    VmImageSnapshot snapshot = GetImageSnapshot(sourceTool, binding.SourcePortName);
                    source = sourceTool == null ? "无效来源" : sourceTool.InstanceName + "." + binding.SourcePortName;
                    currentValue = snapshot == null ? "--" : snapshot.DisplayText;
                    connected = string.IsNullOrWhiteSpace(bindingError);
                    status = !connected
                        ? "连接异常 · " + bindingError
                        : (snapshot == null || snapshot.IsDisposed
                            ? "已连接 · 等待上游运行"
                            : (string.Equals(sourceTool.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) ? "已连接 · 有值" : "已连接 · 上游非 OK"));
                }
            }
            else if (port.PortName == "SearchROI" || port.PortName == "ROI")
            {
                VmToolInputBindingData binding = tool.GetInputBinding(port.PortName);
                if (binding != null)
                {
                    VmToolInstance sourceTool = GetInputSourceTool(binding);
                    string bindingError = GetInputBindingConfigurationError(tool, binding);
                    object raw;
                    VmRegionSnapshot snapshot = sourceTool != null && sourceTool.TryGetOutputValue(binding.SourcePortName, out raw)
                        ? raw as VmRegionSnapshot
                        : null;
                    source = sourceTool == null ? "无效来源" : sourceTool.InstanceName + "." + binding.SourcePortName;
                    currentValue = snapshot == null ? "--" : snapshot.DisplayText;
                    connected = string.IsNullOrWhiteSpace(bindingError);
                    status = !connected
                        ? "连接异常 · " + bindingError
                        : (snapshot == null || snapshot.IsDisposed
                            ? "已连接 · 等待上游运行"
                            : (string.Equals(sourceTool.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) ? "已连接 · 有值" : "已连接 · 上游非 OK"));
                }
                else
                {
                List<VmRoiLayer> boundLayers = GetBoundRoiLayers(tool);
                VmToolParameterData roiParameters = tool.Parameters ?? new VmToolParameterData();
                bool perRoiMode = (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure) &&
                                  VmRoiExecutionMode.Normalize(roiParameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi;
                connected = boundLayers.Count > 0 || (port.IsOptional && !perRoiMode);
                source = boundLayers.Count == 0 ? "未绑定" : string.Join(" + ", boundLayers.Select(item => item.Name));
                currentValue = boundLayers.Count == 0
                    ? "--"
                    : "Region ×" + boundLayers.Count.ToString(CultureInfo.InvariantCulture);
                status = boundLayers.Count > 0
                    ? "已连接 · " + (perRoiMode ? "逐区 " : "合并 ") + boundLayers.Count.ToString(CultureInfo.InvariantCulture) + " 个区域"
                    : (port.IsOptional && !perRoiMode ? "可选 · 全图运行" : "等待 ROI 绑定");
                }
            }
            else if (port.PortName == "ShapeModel")
            {
                connected = currentTemplateItem != null && currentTemplateItem.HasModel;
                currentValue = connected ? currentTemplateItem.Name : "--";
                status = connected ? "已连接" : "等待模板";
            }
            else if (port.PortName == "Program")
            {
                connected = !string.IsNullOrWhiteSpace(HDevPathTextBox.Text);
                currentValue = connected ? Path.GetFileName(HDevPathTextBox.Text) : "--";
                status = connected ? "已连接" : "等待程序";
            }

            return new VmPortDisplayItem
            {
                Direction = "IN",
                PortName = port.PortName,
                DisplayName = port.DisplayName,
                DataType = port.DataType,
                Source = source,
                CurrentValue = currentValue,
                Status = status,
                IsConnected = connected
            };
        }

        private void NumericSourceToolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool == null || tool.Kind != VmToolKind.NumericJudge)
            {
                return;
            }

            tool.InputToolId = NumericSourceToolComboBox.SelectedValue as string;
            VmToolInstance source = GetInputSourceTool(tool);
            VmPortDefinition firstPort = source == null ? null : ToolMetadata.GetNumericOutputPorts(source.Kind).FirstOrDefault();
            tool.InputPortName = firstPort == null ? null : firstPort.PortName;
            RefreshUiState();
        }

        private void NumericSourcePortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool != null && tool.Kind == VmToolKind.NumericJudge)
            {
                tool.InputPortName = NumericSourcePortComboBox.SelectedValue as string;
                RefreshUiState();
            }
        }

        private void NumericOperatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool != null && tool.Kind == VmToolKind.NumericJudge)
            {
                tool.NumericOperator = NumericOperatorComboBox.SelectedValue as string;
                RefreshUiState();
            }
        }

        private void NumericJudgeParameter_LostFocus(object sender, RoutedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool == null || tool.Kind != VmToolKind.NumericJudge)
            {
                return;
            }

            double lower;
            double upper;
            double tolerance;
            if (!TryParseUiDouble(NumericLowerLimitTextBox.Text, out lower) ||
                !TryParseUiDouble(NumericUpperLimitTextBox.Text, out upper) ||
                !TryParseUiDouble(NumericToleranceTextBox.Text, out tolerance))
            {
                NumericConfigValidationText.Text = "配置异常：阈值和容差必须是有效数值。";
                NumericConfigValidationText.Foreground = System.Windows.Media.Brushes.Firebrick;
                tool.ConfigurationStatus = "阈值格式错误";
                return;
            }

            tool.NumericLowerLimit = lower;
            tool.NumericUpperLimit = upper;
            tool.NumericTolerance = tolerance;
            RefreshUiState();
        }

        private static bool TryParseUiDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private void ToolParameter_LostFocus(object sender, RoutedEventArgs e)
        {
            if (inspectorUpdating)
            {
                return;
            }

            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool == null)
            {
                return;
            }

            VmToolParameterData parameters = tool.Parameters == null ? new VmToolParameterData() : tool.Parameters.Clone();
            double value;
            if (tool.Kind == VmToolKind.Blob)
            {
                if (!TryParseUiDouble(BlobMinGrayTextBox.Text, out value)) { HeaderStatusText.Text = "Blob 灰度下限必须是数值。"; return; }
                parameters.BlobMinGray = value;
                if (!TryParseUiDouble(BlobMaxGrayTextBox.Text, out value)) { HeaderStatusText.Text = "Blob 灰度上限必须是数值。"; return; }
                parameters.BlobMaxGray = value;
                if (!TryParseUiDouble(BlobMinAreaTextBox.Text, out value)) { HeaderStatusText.Text = "Blob 最小面积必须是数值。"; return; }
                parameters.BlobMinArea = value;
            }
            else if (tool.Kind == VmToolKind.GrayStat)
            {
                if (!TryParseUiDouble(GrayMinTextBox.Text, out value)) { HeaderStatusText.Text = "灰度下限必须是数值。"; return; }
                parameters.GrayMin = value;
                if (!TryParseUiDouble(GrayMaxTextBox.Text, out value)) { HeaderStatusText.Text = "灰度上限必须是数值。"; return; }
                parameters.GrayMax = value;
            }
            else if (tool.Kind == VmToolKind.EdgeMeasure)
            {
                if (!TryParseUiDouble(EdgeThresholdTextBox.Text, out value)) { HeaderStatusText.Text = "边缘阈值必须是数值。"; return; }
                parameters.EdgeThreshold = value;
            }

            string error = parameters.Validate(tool.Kind);
            if (!string.IsNullOrWhiteSpace(error))
            {
                HeaderStatusText.Text = error;
                tool.ConfigurationStatus = "参数异常";
                return;
            }

            tool.Parameters = parameters;
            HeaderStatusText.Text = tool.InstanceName + " 参数已应用。";
            RefreshUiState();
        }

        private void NumericJudgeRunButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool != null && tool.Kind == VmToolKind.NumericJudge)
            {
                RunStandaloneTool(tool, "数值判定独立运行");
            }
        }

        private void OpenIoPanelButton_Click(object sender, RoutedEventArgs e)
        {
            RightTabs.SelectedItem = IoTab;
            RefreshInspector();
        }

        private void IoOpenParametersButton_Click(object sender, RoutedEventArgs e)
        {
            RightTabs.SelectedItem = DockConfigurationTab;
            RefreshInspector();
        }

        private void IoRunCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (tool != null)
            {
                RunStandaloneTool(tool, "I/O 面板运行当前");
            }
        }

        private void IoRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshInspector();
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

            recipeTrackingSuspended = true;
            try
            {
                LoadLayoutState();
            }
            finally
            {
                recipeTrackingSuspended = false;
            }
            InitializeRecipeBaseline(false);
            LoadPendingRecovery();
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
            if (!recipeCloseConfirmed && (!ConfirmDockDraftChanges("退出软件") || !ConfirmUnsavedRecipeChanges("退出软件")))
            {
                e.Cancel = true;
                return;
            }

            recipeCloseConfirmed = true;
            recipeAutosaveTimer.Stop();
            if (pendingRecovery == null)
            {
                recipeService.DeleteRecovery();
            }
            StopPlayback();
            StopContinuousRun();
            SaveLayoutState();
            DisposeToolOverlays();
            DisposeCurrentImage();
            DisposeCurrentRoi();
            DisposeRoiLayers();
            ClearPendingRoi();
            DisposeFlowTools();

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
                if (!ConfirmDockDraftChanges("新建方案") || !ConfirmUnsavedRecipeChanges("新建方案"))
                {
                    return;
                }

                StopPlayback();
                StopContinuousRun();
                recipeTrackingSuspended = true;
                try
                {
                    currentRecipePath = null;
                    ApplyRecipe(new VisionRecipe());
                }
                finally
                {
                    recipeTrackingSuspended = false;
                }
                savedRecipeFingerprint = "__NEW_REQUIRES_SAVE__";
                lastRecipeSavedAt = null;
                SetRecipeDirtyState(true, "新方案尚未保存");
                ScheduleRecipeStateCheck();
                LogInfo("已新建默认方案，首次保存时请选择路径。");
            });
        }

        private void LoadRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("加载配方", delegate
            {
                if (!ConfirmDockDraftChanges("打开其他方案") || !ConfirmUnsavedRecipeChanges("打开其他方案"))
                {
                    return;
                }

                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "Vision recipe|*.json|All files|*.*",
                    InitialDirectory = GetRecipeDialogDirectory()
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                LoadRecipeFromPath(dialog.FileName);
            });
        }

        private void SaveRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentRecipe(false);
        }

        private void SaveRecipeAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentRecipe(true);
        }

        private void OpenRecentRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedRecentRecipe();
        }

        private void RecentRecipeList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenSelectedRecentRecipe();
        }

        private void RemoveRecentRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            VmRecentRecipeItem selected = RecentRecipeList.SelectedItem as VmRecentRecipeItem;
            if (selected == null)
            {
                return;
            }

            recentRecipes.Remove(selected);
            SaveLayoutState();
            HeaderStatusText.Text = "已从最近方案中移除记录。";
        }

        private void RecoverRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (pendingRecovery == null || pendingRecovery.Recipe == null)
            {
                return;
            }

            recipeTrackingSuspended = true;
            try
            {
                StopPlayback();
                StopContinuousRun();
                currentRecipePath = string.IsNullOrWhiteSpace(pendingRecovery.FormalRecipePath) ? null : pendingRecovery.FormalRecipePath;
                ApplyRecipe(pendingRecovery.Recipe);
            }
            finally
            {
                recipeTrackingSuspended = false;
            }

            pendingRecovery = null;
            recipeService.DeleteRecovery();
            savedRecipeFingerprint = "__RECOVERED_REQUIRES_SAVE__";
            SetRecipeDirtyState(true, "已恢复异常副本，请检查后保存");
            ScheduleRecipeStateCheck();
            LogInfo("已恢复异常退出前的方案副本，等待用户确认保存。");
            RefreshUiState();
        }

        private void DiscardRecoveryButton_Click(object sender, RoutedEventArgs e)
        {
            pendingRecovery = null;
            recipeService.DeleteRecovery();
            RefreshRecoveryWorkspace();
            LogInfo("已忽略并删除异常恢复副本。");
        }

        private bool SaveCurrentRecipe(bool forceSaveAs)
        {
            try
            {
                if (!ConfirmDockDraftChanges("保存方案"))
                {
                    return false;
                }

                string path = forceSaveAs ? null : currentRecipePath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    SaveFileDialog dialog = new SaveFileDialog
                    {
                        Filter = "Vision recipe|*.json",
                        InitialDirectory = GetRecipeDialogDirectory(),
                        FileName = SafeFileName(RecipeNameEditTextBox.Text, "DefaultRecipe") + ".json"
                    };
                    if (dialog.ShowDialog(this) != true)
                    {
                        return false;
                    }

                    path = dialog.FileName;
                }

                VisionRecipe recipe = CaptureRecipe();
                recipeService.SaveRecipe(path, recipe);
                currentRecipePath = Path.GetFullPath(path);
                AddRecentRecipe(currentRecipePath);
                InitializeRecipeBaseline(true);
                SaveLayoutState();
                LogInfo("已原子保存方案：" + currentRecipePath);
                HeaderStatusText.Text = "方案已保存并完成回读验证。";
                RefreshUiState();
                return true;
            }
            catch (Exception ex)
            {
                AppendAlarm("保存方案失败：" + ex.Message);
                MessageBox.Show(this, ex.Message, "保存方案失败", MessageBoxButton.OK, MessageBoxImage.Error);
                RefreshUiState();
                return false;
            }
        }

        private bool ConfirmDockDraftChanges(string nextAction)
        {
            if (!dockDraftDirty || dockEditingTool == null)
            {
                return true;
            }

            MessageBoxResult result = MessageBox.Show(
                this,
                "“" + dockEditingTool.InstanceName + "”存在尚未应用的模块配置草稿。是否先应用，再" + nextAction + "？\n\n是：应用后继续\n否：放弃草稿并继续\n取消：返回配置",
                "未应用的模块配置",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                return ApplyDockConfigurationDraft(true);
            }

            LoadDockConfigurationDraft(dockEditingTool);
            return true;
        }

        private bool ConfirmUnsavedRecipeChanges(string nextAction)
        {
            if (pendingRecovery != null)
            {
                MessageBoxResult recoveryResult = MessageBox.Show(
                    this,
                    "当前还有一个尚未处理的异常恢复副本。继续" + nextAction + "会忽略并删除该副本，是否继续？",
                    "待处理的恢复副本",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);
                if (recoveryResult != MessageBoxResult.OK)
                {
                    return false;
                }

                pendingRecovery = null;
                recipeService.DeleteRecovery();
                RefreshRecoveryWorkspace();
            }

            if (!isRecipeDirty)
            {
                return true;
            }

            MessageBoxResult result = MessageBox.Show(
                this,
                "当前方案存在未保存修改。是否先保存，再" + nextAction + "？\n\n是：保存后继续\n否：放弃修改并继续\n取消：返回当前方案",
                "未保存的方案",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                return SaveCurrentRecipe(false);
            }

            recipeAutosaveTimer.Stop();
            recipeService.DeleteRecovery();
            pendingRecovery = null;
            return true;
        }

        private void LoadRecipeFromPath(string path)
        {
            VisionRecipe recipe = recipeService.LoadRecipe(path);
            StopPlayback();
            StopContinuousRun();
            recipeTrackingSuspended = true;
            try
            {
                currentRecipePath = Path.GetFullPath(path);
                ApplyRecipe(recipe);
            }
            finally
            {
                recipeTrackingSuspended = false;
            }

            AddRecentRecipe(currentRecipePath);
            pendingRecovery = null;
            InitializeRecipeBaseline(true);
            SaveLayoutState();
            LogInfo("已加载方案：" + currentRecipePath);
            HeaderStatusText.Text = "方案已加载：" + Path.GetFileNameWithoutExtension(currentRecipePath);
            RefreshUiState();
        }

        private void OpenSelectedRecentRecipe()
        {
            VmRecentRecipeItem selected = RecentRecipeList.SelectedItem as VmRecentRecipeItem;
            if (selected == null)
            {
                HeaderStatusText.Text = "请先选择一个最近方案。";
                return;
            }

            if (!File.Exists(selected.Path))
            {
                HeaderStatusText.Text = "最近方案文件已不存在，可移除该记录。";
                return;
            }

            if (!ConfirmDockDraftChanges("打开最近方案") || !ConfirmUnsavedRecipeChanges("打开最近方案"))
            {
                return;
            }

            RunUiAction("打开最近方案", delegate { LoadRecipeFromPath(selected.Path); });
        }

        private string GetRecipeDialogDirectory()
        {
            if (!string.IsNullOrWhiteSpace(currentRecipePath))
            {
                string currentDirectory = Path.GetDirectoryName(currentRecipePath);
                if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
                {
                    return currentDirectory;
                }
            }

            VmRecentRecipeItem recent = recentRecipes.FirstOrDefault(item => File.Exists(item.Path));
            string recentDirectory = recent == null ? null : Path.GetDirectoryName(recent.Path);
            return !string.IsNullOrWhiteSpace(recentDirectory) && Directory.Exists(recentDirectory)
                ? recentDirectory
                : recipeService.RecipeDirectory;
        }

        private void AddRecentRecipe(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path);
            VmRecentRecipeItem existing = recentRecipes.FirstOrDefault(item => PathsEqual(item.Path, fullPath));
            if (existing != null)
            {
                recentRecipes.Remove(existing);
            }

            recentRecipes.Insert(0, new VmRecentRecipeItem { Path = fullPath });
            while (recentRecipes.Count > 8)
            {
                recentRecipes.RemoveAt(recentRecipes.Count - 1);
            }
            if (RecentRecipeList != null)
            {
                RecentRecipeList.Items.Refresh();
            }
        }

        private static bool PathsEqual(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second) &&
                   string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeRecipeBaseline(bool clearRecovery)
        {
            recipeAutosaveTimer.Stop();
            savedRecipeFingerprint = recipeService.GetRecipeFingerprint(CaptureRecipe());
            lastRecipeSavedAt = !string.IsNullOrWhiteSpace(currentRecipePath) && File.Exists(currentRecipePath)
                ? (DateTime?)File.GetLastWriteTime(currentRecipePath)
                : null;
            if (clearRecovery)
            {
                pendingRecovery = null;
                recipeService.DeleteRecovery();
            }

            SetRecipeDirtyState(false, string.IsNullOrWhiteSpace(currentRecipePath) ? "当前为未落盘的新方案" : "方案已保存");
            RefreshRecoveryWorkspace();
        }

        private void SetRecipeDirtyState(bool dirty, string detail)
        {
            isRecipeDirty = dirty;
            if (RecipeStateBadgeText == null)
            {
                return;
            }

            bool hasFormalPath = !string.IsNullOrWhiteSpace(currentRecipePath);
            string name = string.IsNullOrWhiteSpace(RecipeNameEditTextBox.Text) ? "未命名" : RecipeNameEditTextBox.Text.Trim();
            RecipeNameText.Text = "方案：" + name + (dirty ? " *" : string.Empty);
            RecipePathText.Text = hasFormalPath ? currentRecipePath : "尚未选择正式保存路径";
            RecipeSchemaText.Text = "v" + RecipeSchema.CurrentVersion + " · JSON · 兼容旧版无版本字段";
            RecipeSavedAtText.Text = lastRecipeSavedAt.HasValue ? lastRecipeSavedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "--";

            if (dirty)
            {
                RecipeStateBadgeText.Text = "未保存";
                RecipeStateBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226));
                RecipeStateBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113));
                RecipeStateBadgeText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 27, 27));
                RecipeWorkspaceStatusBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 247, 237));
                RecipeWorkspaceStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(253, 186, 116));
                RecipeWorkspaceStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(154, 52, 18));
            }
            else if (hasFormalPath)
            {
                RecipeStateBadgeText.Text = "已保存";
                RecipeStateBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 252, 231));
                RecipeStateBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 222, 128));
                RecipeStateBadgeText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 128, 61));
                RecipeWorkspaceStatusBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 253, 245));
                RecipeWorkspaceStatusBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 231, 183));
                RecipeWorkspaceStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(6, 95, 70));
            }
            else
            {
                RecipeStateBadgeText.Text = "新方案";
                RecipeStateBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 243, 199));
                RecipeStateBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
                RecipeStateBadgeText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(146, 64, 14));
            }

            RecipeWorkspaceStatusText.Text = detail ?? (dirty ? "方案存在未保存修改" : "方案已保存");
        }

        private void ScheduleRecipeStateCheck()
        {
            if (!uiReady || recipeTrackingSuspended || string.IsNullOrWhiteSpace(savedRecipeFingerprint))
            {
                return;
            }

            recipeAutosaveTimer.Stop();
            recipeAutosaveTimer.Start();
        }

        private void RecipeAutosaveTimer_Tick(object sender, EventArgs e)
        {
            recipeAutosaveTimer.Stop();
            if (recipeTrackingSuspended || string.IsNullOrWhiteSpace(savedRecipeFingerprint))
            {
                return;
            }

            try
            {
                VisionRecipe recipe = CaptureRecipe();
                string fingerprint = recipeService.GetRecipeFingerprint(recipe);
                bool dirty = !string.Equals(fingerprint, savedRecipeFingerprint, StringComparison.Ordinal);
                if (!dirty)
                {
                    SetRecipeDirtyState(false, string.IsNullOrWhiteSpace(currentRecipePath) ? "当前为未落盘的新方案" : "方案已保存");
                    if (pendingRecovery == null)
                    {
                        recipeService.DeleteRecovery();
                    }
                    return;
                }

                recipeService.SaveRecovery(new RecipeRecoveryData
                {
                    FormalRecipePath = currentRecipePath,
                    Recipe = recipe
                });
                SetRecipeDirtyState(true, "未保存修改已写入独立恢复副本 · " + DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                SetRecipeDirtyState(true, "当前配置暂不可保存：" + ex.Message);
                AppendAlarm("自动保存恢复副本失败：" + ex.Message);
            }
        }

        private void RecipeEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = e.OriginalSource as TextBox;
            if (textBox != null && textBox.IsReadOnly)
            {
                return;
            }

            ScheduleRecipeStateCheck();
        }

        private void RecipeEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScheduleRecipeStateCheck();
        }

        private void RecipeEditor_ToggleChanged(object sender, RoutedEventArgs e)
        {
            ScheduleRecipeStateCheck();
        }

        private void LoadPendingRecovery()
        {
            RecipeRecoveryData recovery;
            string error;
            if (!recipeService.TryLoadRecovery(out recovery, out error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    string quarantined = string.Empty;
                    try { quarantined = recipeService.QuarantineRecovery(); } catch { }
                    AppendAlarm("恢复文件损坏，已隔离：" + error + (string.IsNullOrWhiteSpace(quarantined) ? string.Empty : " · " + quarantined));
                    RecoveryStatusText.Text = "发现损坏的恢复文件，已隔离；正式方案未被修改。";
                }
                RefreshRecoveryWorkspace();
                return;
            }

            DateTime captured;
            bool hasCapturedTime = DateTime.TryParse(recovery.CapturedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out captured);
            bool formalIsNewer = !string.IsNullOrWhiteSpace(recovery.FormalRecipePath) && File.Exists(recovery.FormalRecipePath) &&
                                 hasCapturedTime && File.GetLastWriteTimeUtc(recovery.FormalRecipePath) >= captured.ToUniversalTime();
            if (formalIsNewer)
            {
                recipeService.DeleteRecovery();
                pendingRecovery = null;
                RefreshRecoveryWorkspace();
                return;
            }

            pendingRecovery = recovery;
            RefreshRecoveryWorkspace();
            RecipeWorkspaceTabs.SelectedIndex = 2;
            HeaderStatusText.Text = "检测到异常退出前的方案恢复副本，请在“方案”页选择恢复或忽略。";
            LogInfo("检测到待恢复方案副本：" + (recovery.CapturedAtUtc ?? "时间未知"));
        }

        private void RefreshRecoveryWorkspace()
        {
            if (RecoveryStatusText == null)
            {
                return;
            }

            bool available = pendingRecovery != null && pendingRecovery.Recipe != null;
            RecoverRecipeButton.IsEnabled = available;
            DiscardRecoveryButton.IsEnabled = available;
            if (available)
            {
                RecoveryStatusText.Text = "发现 " + (pendingRecovery.CapturedAtUtc ?? "未知时间") + " 的异常恢复副本。恢复后仍需人工检查并正式保存。";
                RecipeStateBadgeText.Text = "可恢复";
            }
            else if (string.IsNullOrWhiteSpace(RecoveryStatusText.Text) || RecoveryStatusText.Text.StartsWith("发现 ", StringComparison.Ordinal))
            {
                RecoveryStatusText.Text = "当前没有待恢复的方案。";
            }
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

        private async void RunOnceButton_Click(object sender, RoutedEventArgs e)
        {
            await StartManualFlowRunAsync(VmFlowRunRequestMode.Full, "手动单次");
        }

        private async void RunContinuousButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VmFlowRunPolicy policy = ReadFlowRunPolicyFromUi();
                VmFlowExecutionPlanner.BuildPlan(flowTools, FlowToolList.SelectedItem as VmToolInstance, VmFlowRunRequestMode.Full);
                StopPlayback();
                isStopRequested = false;
                ResetFlowDebugRuntimeState(true);
                isContinuousRunning = true;
                runTimer.Interval = TimeSpan.FromMilliseconds(policy.ContinuousIntervalMilliseconds);
                runTimer.Start();
                LogInfo("连续运行已启动，周期间隔 " + policy.ContinuousIntervalMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms。");
                RefreshUiState();
                await RunInspectionRangeAsync(VmFlowRunRequestMode.Full, "连续运行");
            }
            catch (Exception ex)
            {
                HandleFlowRunException("连续运行", ex);
            }
        }

        private void StopRunButton_Click(object sender, RoutedEventArgs e)
        {
            StopContinuousRun();
        }

        private void PauseRunButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isContinuousRunning && !isFlowExecutionActive)
            {
                HeaderStatusText.Text = "当前没有可暂停的流程任务。";
                return;
            }

            if (isPauseRequested)
            {
                isPauseRequested = false;
                isDebugBreakpointPause = false;
                debugPauseBeforeNextTool = false;
                HeaderStatusText.Text = "流程已继续。";
            }
            else
            {
                isPauseRequested = true;
                isDebugBreakpointPause = false;
                HeaderStatusText.Text = "流程将在当前工具完成后暂停。";
            }
            LogInfo(HeaderStatusText.Text);
            RefreshUiState();
        }

        private void ToggleBreakpointButton_Click(object sender, RoutedEventArgs e)
        {
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "运行期间不能修改断点；请先停止流程。";
                return;
            }

            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (selected.Count == 0)
            {
                HeaderStatusText.Text = "请先选择一个或多个流程模块。";
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            bool addBreakpoint = selected.Any(item => !item.IsBreakpoint);
            foreach (VmToolInstance tool in selected)
            {
                tool.IsBreakpoint = addBreakpoint;
            }
            CommitFlowEditHistory((addBreakpoint ? "设置" : "取消") + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个断点", before);
            RefreshFlowPortVisualization();
            RefreshUiState();
            HeaderStatusText.Text = (addBreakpoint ? "已设置 " : "已取消 ") + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块断点。";
            LogInfo(HeaderStatusText.Text);
        }

        private void ClearBreakpointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (isContinuousRunning || isFlowExecutionActive) return;
            List<VmToolInstance> breakpoints = flowTools.Where(item => item.IsBreakpoint).ToList();
            if (breakpoints.Count == 0)
            {
                HeaderStatusText.Text = "当前流程没有断点。";
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            foreach (VmToolInstance tool in breakpoints) tool.IsBreakpoint = false;
            CommitFlowEditHistory("清除全部断点", before);
            RefreshFlowPortVisualization();
            RefreshUiState();
            HeaderStatusText.Text = "已清除全部 " + breakpoints.Count.ToString(CultureInfo.InvariantCulture) + " 个断点。";
            LogInfo(HeaderStatusText.Text);
        }

        private async void DebugStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (isFlowExecutionActive)
            {
                if (!isPauseRequested)
                {
                    HeaderStatusText.Text = "流程正在执行当前模块；单步将在暂停后可用。";
                    return;
                }

                debugSingleStepRequested = true;
                debugPauseBeforeNextTool = false;
                isDebugBreakpointPause = false;
                isPauseRequested = false;
                HeaderStatusText.Text = "单步：执行下一模块后再次暂停。";
                LogInfo(HeaderStatusText.Text);
                RefreshUiState();
                return;
            }

            if (isContinuousRunning)
            {
                StopContinuousRun();
            }

            debugSingleStepRequested = true;
            debugPauseBeforeNextTool = false;
            isDebugBreakpointPause = false;
            VmFlowRunRequestMode mode = FlowToolList.SelectedItem is VmToolInstance
                ? VmFlowRunRequestMode.FromHere
                : VmFlowRunRequestMode.Full;
            await StartManualFlowRunAsync(mode, "调试单步");
        }

        private void DebugContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isFlowExecutionActive || !isPauseRequested)
            {
                HeaderStatusText.Text = "当前没有停在断点或单步位置的流程。";
                return;
            }

            debugSingleStepRequested = false;
            debugPauseBeforeNextTool = false;
            isDebugBreakpointPause = false;
            isPauseRequested = false;
            HeaderStatusText.Text = "调试继续：按确定性顺序执行。";
            LogInfo(HeaderStatusText.Text);
            RefreshUiState();
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
                EnsureDisplayImage();
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
            RefreshToolCatalogView();
        }

        private bool FilterToolCatalogItem(object value)
        {
            VmToolCatalogItem item = value as VmToolCatalogItem;
            if (item == null)
            {
                return false;
            }

            string query = ToolSearchTextBox == null || ToolSearchTextBox.Text == null
                ? string.Empty
                : ToolSearchTextBox.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(query) && !item.SearchText.Contains(query))
            {
                return false;
            }

            if (string.Equals(toolCatalogMode, "Favorite", StringComparison.OrdinalIgnoreCase))
            {
                return item.IsFavorite;
            }

            if (string.Equals(toolCatalogMode, "Recent", StringComparison.OrdinalIgnoreCase))
            {
                return item.RecentRank != int.MaxValue;
            }

            return true;
        }

        private void ToolCatalogModeButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            toolCatalogMode = button == null || button.Tag == null ? "All" : button.Tag.ToString();
            RefreshToolCatalogView();
        }

        private void RefreshToolCatalogView()
        {
            if (toolCatalogView == null || ToolCatalogList == null)
            {
                return;
            }

            for (int index = 0; index < toolCatalog.Count; index++)
            {
                VmToolCatalogItem item = toolCatalog[index];
                item.IsFavorite = favoriteToolKinds.Contains(item.Kind);
                int recentIndex = recentToolKinds.IndexOf(item.Kind);
                item.RecentRank = recentIndex < 0 ? int.MaxValue : recentIndex;
            }

            toolCatalogView.Refresh();
            int visibleCount = toolCatalogView.Cast<object>().Count();
            ToolCatalogEmptyText.Visibility = visibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            string modeText = string.Equals(toolCatalogMode, "Favorite", StringComparison.OrdinalIgnoreCase)
                ? "收藏"
                : (string.Equals(toolCatalogMode, "Recent", StringComparison.OrdinalIgnoreCase) ? "最近使用" : "全部分类");
            ToolCatalogStatusText.Text = modeText + " · " + visibleCount.ToString(CultureInfo.InvariantCulture) + " 个工具 · 双击追加或拖入指定位置";
            UpdateToolCatalogModeButtons();
        }

        private void UpdateToolCatalogModeButtons()
        {
            UpdateToolCatalogModeButton(ToolCatalogAllButton, "All");
            UpdateToolCatalogModeButton(ToolCatalogFavoriteButton, "Favorite");
            UpdateToolCatalogModeButton(ToolCatalogRecentButton, "Recent");
        }

        private void UpdateToolCatalogModeButton(Button button, string mode)
        {
            if (button == null)
            {
                return;
            }

            bool selected = string.Equals(toolCatalogMode, mode, StringComparison.OrdinalIgnoreCase);
            button.Background = selected
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 118, 110))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            button.Foreground = selected ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGray;
        }

        private void ToolCatalogFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            VmToolCatalogItem item = button == null ? null : button.Tag as VmToolCatalogItem;
            if (item == null)
            {
                return;
            }

            if (!favoriteToolKinds.Add(item.Kind))
            {
                favoriteToolKinds.Remove(item.Kind);
            }
            RefreshToolCatalogView();
            SaveLayoutState();
            HeaderStatusText.Text = item.Name + (favoriteToolKinds.Contains(item.Kind) ? " 已加入收藏。" : " 已取消收藏。");
            e.Handled = true;
        }

        private void ToolCatalogList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            toolCatalogDragStartPoint = e.GetPosition(null);
            ListBoxItem container = ItemsControl.ContainerFromElement(ToolCatalogList, e.OriginalSource as DependencyObject) as ListBoxItem;
            toolCatalogDragItem = container == null ? null : container.DataContext as VmToolCatalogItem;
        }

        private void ToolCatalogList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || toolCatalogDragItem == null)
            {
                return;
            }

            System.Windows.Point current = e.GetPosition(null);
            if (Math.Abs(current.X - toolCatalogDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - toolCatalogDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            VmToolCatalogItem dragged = toolCatalogDragItem;
            toolCatalogDragItem = null;
            DataObject data = new DataObject(ToolCatalogDragFormat, dragged);
            DragDrop.DoDragDrop(ToolCatalogList, data, DragDropEffects.Copy);
            HideFlowDropHint();
        }

        private void FlowToolList_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(ToolCatalogDragFormat))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            int insertionIndex = GetFlowDropInsertionIndex(e);
            FlowDropHintText.Text = insertionIndex >= flowTools.Count
                ? "插入位置：流程末尾（第 " + (flowTools.Count + 1).ToString(CultureInfo.InvariantCulture) + " 步）"
                : "插入位置：第 " + (insertionIndex + 1).ToString(CultureInfo.InvariantCulture) + " 步之前";
            FlowDropHintBorder.Visibility = Visibility.Visible;
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private int GetFlowDropInsertionIndex(DragEventArgs e)
        {
            for (int index = 0; index < flowTools.Count; index++)
            {
                ListBoxItem container = FlowToolList.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                if (container == null)
                {
                    continue;
                }

                System.Windows.Point point = e.GetPosition(container);
                if (point.Y >= 0 && point.Y <= container.ActualHeight)
                {
                    return point.Y > container.ActualHeight / 2.0 ? index + 1 : index;
                }
            }

            return flowTools.Count;
        }

        private void FlowToolList_DragLeave(object sender, DragEventArgs e)
        {
            HideFlowDropHint();
        }

        private void FlowToolList_Drop(object sender, DragEventArgs e)
        {
            VmToolCatalogItem catalogItem = e.Data.GetData(ToolCatalogDragFormat) as VmToolCatalogItem;
            if (catalogItem == null)
            {
                return;
            }

            int insertionIndex = GetFlowDropInsertionIndex(e);
            HideFlowDropHint();
            InsertCatalogTool(catalogItem, insertionIndex, "拖拽插入");
            e.Handled = true;
        }

        private void FlowGraphViewport_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(ToolCatalogDragFormat))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            FlowDropHintText.Text = "插入位置：流程末尾（二维画布按依赖自动布局；精确顺序可在顺序列表调整）";
            FlowDropHintBorder.Visibility = Visibility.Visible;
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void FlowGraphViewport_Drop(object sender, DragEventArgs e)
        {
            VmToolCatalogItem catalogItem = e.Data.GetData(ToolCatalogDragFormat) as VmToolCatalogItem;
            if (catalogItem == null) return;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            System.Windows.Point dropPoint = e.GetPosition(FlowGraphCanvas);
            HideFlowDropHint();
            VmToolInstance inserted;
            suppressFlowInsertHistory = true;
            try { inserted = InsertCatalogTool(catalogItem, flowTools.Count, "拖入连接画布并追加"); }
            finally { suppressFlowInsertHistory = false; }
            if (inserted != null)
            {
                EnableManualFlowLayout();
                inserted.GraphX = SnapFlowCoordinate(Math.Max(16, dropPoint.X - 180));
                inserted.GraphY = SnapFlowCoordinate(Math.Max(16, dropPoint.Y - 40));
                inserted.HasManualGraphPosition = true;
                CommitManualLayoutOperation("已将 " + inserted.InstanceName + " 放到网格位置", before);
            }
            e.Handled = true;
        }

        private void FlowGraphNode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            VmToolInstance tool = element == null ? null : element.Tag as VmToolInstance;
            if (tool == null) return;
            bool toggle = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            bool alreadySelected = selected.Contains(tool);
            bool selectionChanges = toggle || selected.Count != 1 || !alreadySelected;
            if (selectionChanges && !ConfirmDockDraftChanges("更改模块选择"))
            {
                e.Handled = true;
                return;
            }
            selectedFlowConnectionKey = null;
            selectedFlowPortToolId = null;
            selectedFlowPortName = null;
            selectedFlowPortDirection = null;
            if (toggle)
            {
                if (alreadySelected) selected.Remove(tool);
                else selected.Add(tool);
                ApplyFlowToolSelection(selected, alreadySelected ? selected.LastOrDefault() : tool, true);
            }
            else
            {
                ApplyFlowToolSelection(new[] { tool }, tool, true);
            }
            if (e.ClickCount >= 2)
            {
                ConfigureToolButton_Click(sender, e);
            }
            else if (e.ChangedButton == MouseButton.Left && !toggle && !isContinuousRunning && !isFlowExecutionActive)
            {
                BeginFlowNodeDrag(tool, e.GetPosition(FlowGraphCanvas));
            }
            e.Handled = true;
        }

        private void BeginFlowNodeDrag(VmToolInstance tool, System.Windows.Point startPoint)
        {
            if (tool == null || FlowGraphCanvas == null) return;
            List<VmToolInstance> moving = GetSelectedFlowToolsInSequence();
            if (!moving.Contains(tool)) moving = new List<VmToolInstance> { tool };
            flowNodeDragSession = new FlowNodeDragSession
            {
                Tool = tool,
                StartPoint = startPoint,
                InitialManualMode = isManualFlowLayout,
                InitialPositions = moving.ToDictionary(item => item, item => new System.Windows.Point(item.GraphX, item.GraphY)),
                InitialManualFlags = flowTools.ToDictionary(item => item, item => item.HasManualGraphPosition),
                BeforeSnapshot = CaptureFlowEditSnapshot()
            };
            FlowGraphCanvas.CaptureMouse();
        }

        private void FlowGraphNode_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            VmToolInstance tool = element == null ? null : element.Tag as VmToolInstance;
            if (tool == null)
            {
                return;
            }

            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (!selected.Contains(tool))
            {
                if (!ConfirmDockDraftChanges("更改模块选择"))
                {
                    e.Handled = true;
                    return;
                }
                selected.Clear();
                selected.Add(tool);
            }
            ApplyFlowToolSelection(selected, tool, true);
            flowPasteInsertionIndex = flowTools.IndexOf(tool) + 1;
            HeaderStatusText.Text = "右键目标：" + tool.InstanceName + "；粘贴将插入到该模块之后。";
            e.Handled = true;
        }

        private static bool IsFlowGraphInteractiveElement(DependencyObject source)
        {
            DependencyObject current = source;
            while (current != null)
            {
                FrameworkElement element = current as FrameworkElement;
                if (element != null && (element.Tag is VmToolInstance || element.Tag is VmFlowPortChip || element.Tag is VmFlowConnectionVisual || element.Tag is VmFlowGroupVisual))
                {
                    return true;
                }
                FrameworkContentElement content = current as FrameworkContentElement;
                if (content != null)
                {
                    current = content.Parent;
                }
                else if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }
            return false;
        }

        private void FlowGraphCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || IsFlowGraphInteractiveElement(e.OriginalSource as DependencyObject))
            {
                return;
            }
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能更改模块多选。";
                e.Handled = true;
                return;
            }
            if (!ConfirmDockDraftChanges("更改模块选择"))
            {
                e.Handled = true;
                return;
            }

            isFlowGraphSelecting = true;
            flowGraphSelectionMoved = false;
            flowGraphSelectionStart = e.GetPosition(FlowGraphCanvas);
            flowGraphSelectionBaseline.Clear();
            foreach (VmToolInstance tool in GetSelectedFlowToolsInSequence()) flowGraphSelectionBaseline.Add(tool.ToolId);
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                ApplyFlowToolSelection(new VmToolInstance[0], null, false);
            }
            Canvas.SetLeft(FlowGraphSelectionRectangle, flowGraphSelectionStart.X);
            Canvas.SetTop(FlowGraphSelectionRectangle, flowGraphSelectionStart.Y);
            FlowGraphSelectionRectangle.Width = 0;
            FlowGraphSelectionRectangle.Height = 0;
            FlowGraphSelectionRectangle.Visibility = Visibility.Visible;
            FlowGraphCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void FlowGraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (flowNodeDragSession != null)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    CompleteFlowNodeDrag(false);
                    return;
                }

                System.Windows.Point point = e.GetPosition(FlowGraphCanvas);
                double deltaX = point.X - flowNodeDragSession.StartPoint.X;
                double deltaY = point.Y - flowNodeDragSession.StartPoint.Y;
                if (!flowNodeDragSession.HasMoved &&
                    Math.Abs(deltaX) < SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(deltaY) < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                if (!flowNodeDragSession.HasMoved)
                {
                    EnableManualFlowLayout();
                    flowNodeDragSession.HasMoved = true;
                }
                foreach (KeyValuePair<VmToolInstance, System.Windows.Point> entry in flowNodeDragSession.InitialPositions)
                {
                    entry.Key.GraphX = SnapFlowCoordinate(Math.Max(16, entry.Value.X + deltaX));
                    entry.Key.GraphY = SnapFlowCoordinate(Math.Max(16, entry.Value.Y + deltaY));
                    entry.Key.HasManualGraphPosition = true;
                }
                RefreshFlowGraphLayout();
                HeaderStatusText.Text = "正在手动移动 " + flowNodeDragSession.InitialPositions.Count.ToString(CultureInfo.InvariantCulture) + " 个模块；执行顺序与连接不变。";
                e.Handled = true;
                return;
            }

            if (flowPortDragSession != null)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    CancelFlowPortDrag("端口拖拽已取消。", true);
                    return;
                }

                System.Windows.Point dragPoint = e.GetPosition(FlowGraphCanvas);
                if (Math.Abs(dragPoint.X - flowPortDragSession.StartPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(dragPoint.Y - flowPortDragSession.StartPoint.Y) >= SystemParameters.MinimumVerticalDragDistance)
                {
                    flowPortDragSession.HasMoved = true;
                }
                VmFlowPortChip hoverPort = FindFlowPortChipAt(dragPoint);
                VmFlowPortChip sourcePort = null;
                VmFlowPortChip targetPort = null;
                string validationError = string.Empty;
                bool valid = TryResolveFlowPortDragEndpoints(flowPortDragSession, hoverPort, out sourcePort, out targetPort, out validationError);
                UpdateFlowPortDragPreview(dragPoint, sourcePort, targetPort, hoverPort != null, valid, validationError);
                e.Handled = true;
                return;
            }

            if (!isFlowGraphSelecting || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            System.Windows.Point current = e.GetPosition(FlowGraphCanvas);
            double left = Math.Min(flowGraphSelectionStart.X, current.X);
            double top = Math.Min(flowGraphSelectionStart.Y, current.Y);
            double width = Math.Abs(current.X - flowGraphSelectionStart.X);
            double height = Math.Abs(current.Y - flowGraphSelectionStart.Y);
            if (width >= SystemParameters.MinimumHorizontalDragDistance || height >= SystemParameters.MinimumVerticalDragDistance)
            {
                flowGraphSelectionMoved = true;
            }
            Canvas.SetLeft(FlowGraphSelectionRectangle, left);
            Canvas.SetTop(FlowGraphSelectionRectangle, top);
            FlowGraphSelectionRectangle.Width = width;
            FlowGraphSelectionRectangle.Height = height;

            if (flowGraphSelectionMoved)
            {
                Rect selection = new Rect(left, top, width, height);
                List<VmToolInstance> hits = GetFlowToolsIntersectingSelection(selection);
                List<VmToolInstance> selected = ResolveFlowGraphSelection(selection,
                    (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                ApplyFlowToolSelection(selected, hits.LastOrDefault() ?? selected.LastOrDefault(), false);
            }
            e.Handled = true;
        }

        private List<VmToolInstance> GetFlowToolsIntersectingSelection(Rect selection)
        {
            return flowTools
                .Where(tool => selection.IntersectsWith(new Rect(tool.GraphX, tool.GraphY, 360, tool.GraphHeight)))
                .OrderBy(tool => flowTools.IndexOf(tool))
                .ToList();
        }

        private List<VmToolInstance> ResolveFlowGraphSelection(Rect selection, bool toggleBaseline)
        {
            List<VmToolInstance> hits = GetFlowToolsIntersectingSelection(selection);
            if (!toggleBaseline)
            {
                return hits;
            }

            HashSet<string> selectedIds = new HashSet<string>(flowGraphSelectionBaseline, StringComparer.OrdinalIgnoreCase);
            foreach (VmToolInstance hit in hits)
            {
                if (!selectedIds.Add(hit.ToolId)) selectedIds.Remove(hit.ToolId);
            }
            return flowTools.Where(tool => selectedIds.Contains(tool.ToolId)).ToList();
        }

        private void FlowGraphCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (flowNodeDragSession != null && e.ChangedButton == MouseButton.Left)
            {
                CompleteFlowNodeDrag(true);
                e.Handled = true;
                return;
            }

            if (flowPortDragSession != null && e.ChangedButton == MouseButton.Left)
            {
                System.Windows.Point point = e.GetPosition(FlowGraphCanvas);
                VmFlowPortChip hoverPort = FindFlowPortChipAt(point);
                VmFlowPortChip sourcePort = null;
                VmFlowPortChip targetPort = null;
                string validationError = string.Empty;
                bool valid = flowPortDragSession.HasMoved &&
                    TryResolveFlowPortDragEndpoints(flowPortDragSession, hoverPort, out sourcePort, out targetPort, out validationError);
                if (!flowPortDragSession.HasMoved)
                {
                    CancelFlowPortDrag(null, false);
                }
                else if (!valid)
                {
                    CancelFlowPortDrag("连线未修改：" + (string.IsNullOrWhiteSpace(validationError) ? "请拖到兼容端口。" : validationError), true);
                }
                else
                {
                    string applyError;
                    bool applied = TryApplyFlowPortBinding(sourcePort, targetPort, out applyError);
                    CancelFlowPortDrag(applied ? null : "连线失败，原订阅已保留：" + applyError, !applied);
                }
                e.Handled = true;
                return;
            }

            if (!isFlowGraphSelecting || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (!flowGraphSelectionMoved)
            {
                List<VmToolInstance> baseline = flowTools.Where(tool => flowGraphSelectionBaseline.Contains(tool.ToolId)).ToList();
                ApplyFlowToolSelection(
                    (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ? baseline : new List<VmToolInstance>(),
                    baseline.LastOrDefault(),
                    false);
            }
            isFlowGraphSelecting = false;
            FlowGraphSelectionRectangle.Visibility = Visibility.Collapsed;
            Mouse.Capture(null);
            FlowToolList_SelectionChanged(FlowToolList, null);
            RefreshUiState();
            int count = GetSelectedFlowToolsInSequence().Count;
            HeaderStatusText.Text = count == 0
                ? "已清除模块选择；拖动画布空白可框选。"
                : "画布框选完成：已选择 " + count.ToString(CultureInfo.InvariantCulture) + " 个真实流程模块。";
            e.Handled = true;
        }

        private void ApplyFlowToolSelection(IEnumerable<VmToolInstance> tools, VmToolInstance primary, bool refreshSelectionUi)
        {
            List<VmToolInstance> selected = (tools ?? Enumerable.Empty<VmToolInstance>())
                .Where(tool => tool != null && flowTools.Contains(tool))
                .Distinct()
                .OrderBy(tool => flowTools.IndexOf(tool))
                .ToList();
            if (primary != null && selected.Contains(primary))
            {
                selected.Remove(primary);
                selected.Insert(0, primary);
            }

            flowSelectionUpdating = true;
            try
            {
                FlowToolList.SelectedItems.Clear();
                foreach (VmToolInstance tool in selected) FlowToolList.SelectedItems.Add(tool);
            }
            finally
            {
                flowSelectionUpdating = false;
            }
            RefreshFlowSelectionFlags();
            if (refreshSelectionUi)
            {
                FlowToolList_SelectionChanged(FlowToolList, null);
                RefreshUiState();
            }
        }

        private void RefreshFlowSelectionFlags()
        {
            HashSet<VmToolInstance> selected = FlowToolList == null
                ? new HashSet<VmToolInstance>()
                : new HashSet<VmToolInstance>(FlowToolList.SelectedItems.Cast<VmToolInstance>());
            foreach (VmToolInstance tool in flowTools) tool.IsFlowSelected = selected.Contains(tool);
            if (FlowSelectionStatusText != null)
            {
                FlowSelectionStatusText.Text = "已选 " + selected.Count.ToString(CultureInfo.InvariantCulture) + "/" + flowTools.Count.ToString(CultureInfo.InvariantCulture) + " · 拖空白框选 / Ctrl 增减";
            }
            UpdateFlowGroupCommandState();
        }

        private List<VmToolInstance> GetSelectedFlowGroupMembers()
        {
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            VmToolInstance grouped = selected.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.GroupId));
            if (grouped == null) return new List<VmToolInstance>();
            return flowTools.Where(item => string.Equals(item.GroupId, grouped.GroupId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => flowTools.IndexOf(item)).ToList();
        }

        private void UpdateFlowGroupCommandState()
        {
            if (FlowGroupNameTextBox == null) return;
            bool busy = isContinuousRunning || isFlowExecutionActive;
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            List<VmToolInstance> group = GetSelectedFlowGroupMembers();
            bool hasGroup = group.Count >= 2;
            bool collapsed = hasGroup && group.All(item => item.IsGroupCollapsed);
            CreateFlowGroupButton.IsEnabled = selected.Count >= 2 && !busy;
            UngroupFlowGroupButton.IsEnabled = hasGroup && !busy;
            CollapseFlowGroupButton.IsEnabled = hasGroup && !collapsed && !busy;
            ExpandFlowGroupButton.IsEnabled = hasGroup && collapsed && !busy;
            RenameFlowGroupButton.IsEnabled = hasGroup && !busy;
            FlowGroupNameTextBox.IsEnabled = hasGroup && !busy;
            if (!FlowGroupNameTextBox.IsKeyboardFocusWithin)
            {
                FlowGroupNameTextBox.Text = hasGroup ? group[0].GroupName : string.Empty;
            }
            FlowGroupStateText.Text = !hasGroup
                ? "组合：选择至少 2 个模块创建"
                : "组合：" + group[0].GroupName + " · " + group.Count.ToString(CultureInfo.InvariantCulture) + " 成员 · " + (collapsed ? "安全折叠" : "展开");
        }

        private string CreateUniqueFlowGroupName(string preferred)
        {
            string baseName = string.IsNullOrWhiteSpace(preferred) ? "组合" : preferred.Trim();
            HashSet<string> names = new HashSet<string>(flowTools.Where(item => !string.IsNullOrWhiteSpace(item.GroupId) && !string.IsNullOrWhiteSpace(item.GroupName)).Select(item => item.GroupName), StringComparer.OrdinalIgnoreCase);
            if (!names.Contains(baseName)) return baseName;
            int index = 2;
            string candidate;
            do
            {
                candidate = baseName + "_" + index.ToString("00", CultureInfo.InvariantCulture);
                index++;
            }
            while (names.Contains(candidate));
            return candidate;
        }

        private void NormalizeFlowGroups()
        {
            foreach (IGrouping<string, VmToolInstance> group in flowTools.Where(item => !string.IsNullOrWhiteSpace(item.GroupId)).GroupBy(item => item.GroupId, StringComparer.OrdinalIgnoreCase).ToList())
            {
                List<VmToolInstance> members = group.OrderBy(item => flowTools.IndexOf(item)).ToList();
                if (members.Count < 2)
                {
                    members[0].GroupId = null;
                    members[0].GroupName = null;
                    members[0].IsGroupCollapsed = false;
                    continue;
                }
                string name = members.Select(item => item.GroupName).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? "流程组合";
                bool collapsed = members[0].IsGroupCollapsed;
                foreach (VmToolInstance member in members)
                {
                    member.GroupName = name;
                    member.IsGroupCollapsed = collapsed;
                }
            }
        }

        private void CreateFlowGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (isContinuousRunning || isFlowExecutionActive) return;
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (selected.Count < 2)
            {
                HeaderStatusText.Text = "请先框选或 Ctrl 选择至少 2 个真实流程模块。";
                return;
            }
            if (!ConfirmDockDraftChanges("创建流程组合")) return;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            string groupId = Guid.NewGuid().ToString("N");
            string groupName = CreateUniqueFlowGroupName("组合 " + (flowTools.Where(item => !string.IsNullOrWhiteSpace(item.GroupId)).Select(item => item.GroupId).Distinct(StringComparer.OrdinalIgnoreCase).Count() + 1).ToString("00", CultureInfo.InvariantCulture));
            foreach (VmToolInstance tool in selected)
            {
                tool.GroupId = groupId;
                tool.GroupName = groupName;
                tool.IsGroupCollapsed = false;
            }
            NormalizeFlowGroups();
            RefreshUiState();
            CommitFlowEditHistory("创建组合：" + groupName, before);
            HeaderStatusText.Text = "已创建 " + groupName + "；成员与真实依赖不变，仍按 #" + selected.First().SequenceText + " → #" + selected.Last().SequenceText + " 确定性执行。";
            LogInfo(HeaderStatusText.Text);
        }

        private void UngroupFlowGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSelectedFlowGroup("解组", delegate(List<VmToolInstance> members)
            {
                foreach (VmToolInstance member in members)
                {
                    member.GroupId = null;
                    member.GroupName = null;
                    member.IsGroupCollapsed = false;
                }
            });
        }

        private void CollapseFlowGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSelectedFlowGroup("折叠组合", members => members.ForEach(item => item.IsGroupCollapsed = true));
        }

        private void ExpandFlowGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSelectedFlowGroup("展开组合", members => members.ForEach(item => item.IsGroupCollapsed = false));
        }

        private void RenameFlowGroupButton_Click(object sender, RoutedEventArgs e)
        {
            List<VmToolInstance> members = GetSelectedFlowGroupMembers();
            if (members.Count < 2) return;
            string requested = FlowGroupNameTextBox.Text == null ? string.Empty : FlowGroupNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(requested))
            {
                HeaderStatusText.Text = "组合名称不能为空。";
                return;
            }
            bool duplicate = flowTools.Any(item => !string.IsNullOrWhiteSpace(item.GroupId) &&
                !string.Equals(item.GroupId, members[0].GroupId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.GroupName, requested, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                HeaderStatusText.Text = "组合名称已存在，请使用唯一名称。";
                return;
            }
            ChangeSelectedFlowGroup("重命名组合", items => items.ForEach(item => item.GroupName = requested));
        }

        private void ChangeSelectedFlowGroup(string action, Action<List<VmToolInstance>> change)
        {
            if (isContinuousRunning || isFlowExecutionActive) return;
            List<VmToolInstance> members = GetSelectedFlowGroupMembers();
            if (members.Count < 2)
            {
                HeaderStatusText.Text = "请先选择一个流程组合中的模块。";
                return;
            }
            if (!ConfirmDockDraftChanges(action)) return;
            string name = members[0].GroupName;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            change(members);
            NormalizeFlowGroups();
            RefreshUiState();
            CommitFlowEditHistory(action + "：" + name, before);
            HeaderStatusText.Text = action + "完成：" + name + "；真实连接和确定性执行顺序未改变。";
            LogInfo(HeaderStatusText.Text);
        }

        private void FlowGroupVisual_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            VmFlowGroupVisual visual = element == null ? null : element.Tag as VmFlowGroupVisual;
            if (visual == null) return;
            List<VmToolInstance> members = flowTools.Where(item => string.Equals(item.GroupId, visual.GroupId, StringComparison.OrdinalIgnoreCase)).ToList();
            ApplyFlowToolSelection(members, members.FirstOrDefault(), true);
            HeaderStatusText.Text = "已选择组合 " + visual.GroupName + " 的 " + members.Count.ToString(CultureInfo.InvariantCulture) + " 个成员。";
            e.Handled = true;
        }

        private void FlowGroupVisual_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            FlowGroupVisual_MouseLeftButtonDown(sender, e);
        }

        private void FlowGraphCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (flowTools.Count == 0)
            {
                flowPasteInsertionIndex = 0;
                return;
            }

            System.Windows.Point point = e.GetPosition(FlowGraphCanvas);
            VmToolInstance nearest = flowTools
                .OrderBy(item => Math.Pow(item.GraphX + 180 - point.X, 2) + Math.Pow(item.GraphY + item.GraphHeight * 0.5 - point.Y, 2))
                .FirstOrDefault();
            flowPasteInsertionIndex = nearest == null ? flowTools.Count : flowTools.IndexOf(nearest) + 1;
            HeaderStatusText.Text = nearest == null
                ? "画布粘贴位置：流程末尾。"
                : "画布粘贴位置：" + nearest.InstanceName + " 之后。";
        }

        private void FlowToolList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem container = ItemsControl.ContainerFromElement(FlowToolList, e.OriginalSource as DependencyObject) as ListBoxItem;
            VmToolInstance tool = container == null ? null : container.DataContext as VmToolInstance;
            if (tool == null)
            {
                flowPasteInsertionIndex = flowTools.Count;
                return;
            }

            if (!FlowToolList.SelectedItems.Contains(tool))
            {
                FlowToolList.SelectedItems.Clear();
                FlowToolList.SelectedItem = tool;
            }
            flowPasteInsertionIndex = flowTools.IndexOf(tool) + 1;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && flowNodeDragSession != null)
            {
                CancelFlowNodeDrag();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && flowPortDragSession != null)
            {
                CancelFlowPortDrag("已取消端口拖拽；原订阅保持不变。", true);
                e.Handled = true;
                return;
            }

            if (Keyboard.FocusedElement is TextBoxBase || Keyboard.FocusedElement is ComboBox)
            {
                return;
            }

            if (e.Key == Key.Delete && !string.IsNullOrWhiteSpace(selectedFlowConnectionKey) &&
                FlowGraphViewport != null && FlowGraphViewport.Visibility == Visibility.Visible)
            {
                ClearFlowConnectionButton_Click(sender, e);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F9)
            {
                ToggleBreakpointButton_Click(sender, e);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F10)
            {
                DebugStepButton_Click(sender, e);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F5)
            {
                if (isFlowExecutionActive && isPauseRequested) DebugContinueButton_Click(sender, e);
                else RunOnceButton_Click(sender, e);
                e.Handled = true;
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return;
            }

            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                RedoFlowEditButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Z)
            {
                UndoFlowEditButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Y)
            {
                RedoFlowEditButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.C)
            {
                CopyFlowToolButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.V)
            {
                PasteFlowToolButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void FlowConnectionPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            VmFlowConnectionVisual connection = element == null ? null : element.Tag as VmFlowConnectionVisual;
            if (connection == null)
            {
                return;
            }

            SelectFlowConnection(connection);
            e.Handled = true;
        }

        private void SelectFlowConnection(VmFlowConnectionVisual connection)
        {
            if (connection == null)
            {
                selectedFlowConnectionKey = null;
                RefreshFlowPortVisualization();
                return;
            }

            selectedFlowConnectionKey = connection.ConnectionKey;
            selectedFlowPortToolId = null;
            selectedFlowPortName = null;
            selectedFlowPortDirection = null;
            VmToolInstance target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, connection.TargetToolId, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                FlowToolList.SelectedItem = target;
            }
            RefreshFlowPortVisualization();
            HeaderStatusText.Text = "已选择真实连接：" + connection.SourceEndpointText + " → " + connection.TargetEndpointText;
        }

        private VmFlowConnectionVisual GetSelectedFlowConnection()
        {
            return string.IsNullOrWhiteSpace(selectedFlowConnectionKey)
                ? null
                : flowConnections.FirstOrDefault(item => string.Equals(item.ConnectionKey, selectedFlowConnectionKey, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateFlowConnectionInspector()
        {
            if (FlowConnectionInspectorBorder == null)
            {
                return;
            }

            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            bool graphVisible = FlowGraphViewport != null && FlowGraphViewport.Visibility == Visibility.Visible;
            FlowConnectionInspectorBorder.Visibility = connection != null && graphVisible ? Visibility.Visible : Visibility.Collapsed;
            if (connection == null)
            {
                return;
            }

            FlowConnectionInspectorStateText.Text = connection.StateText;
            FlowConnectionInspectorSourceText.Text = connection.SourceEndpointText;
            FlowConnectionInspectorTargetText.Text = connection.TargetEndpointText;
            FlowConnectionInspectorTypeText.Text = connection.DataType;
            FlowConnectionInspectorValueText.Text = string.IsNullOrWhiteSpace(connection.CurrentValueText) ? "--" : connection.CurrentValueText;
            FlowConnectionInspectorErrorText.Text = string.IsNullOrWhiteSpace(connection.ErrorReason) ? "无" : connection.ErrorReason;
            RepairFlowConnectionButton.IsEnabled = string.Equals(connection.StateKey, "Error", StringComparison.OrdinalIgnoreCase);
        }

        private void FlowPortChip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            VmFlowPortChip chip = element == null ? null : element.Tag as VmFlowPortChip;
            VmToolInstance owner = chip == null
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, chip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            if (chip == null || owner == null)
            {
                return;
            }

            selectedFlowConnectionKey = null;
            selectedFlowPortToolId = chip.OwnerToolId;
            selectedFlowPortName = chip.PortName;
            selectedFlowPortDirection = chip.Direction;
            FlowToolList.SelectedItem = owner;
            if (FlowGraphViewport.Visibility != Visibility.Visible)
            {
                SetFlowEditorView(true);
            }
            bool dragStarted = IsFlowGraphDescendant(element) && BeginFlowPortDrag(chip, element, e.GetPosition(FlowGraphCanvas));
            RefreshFlowPortVisualization();
            HeaderStatusText.Text = dragStarted
                ? (string.Equals(chip.Direction, "IN", StringComparison.OrdinalIgnoreCase)
                    ? "正在重连：拖到新的兼容 OUT 端口，Esc 或空白处放开可取消。"
                    : "正在连线：拖到兼容 IN 端口，绿色表示可连接。")
                : (string.Equals(chip.Direction, "IN", StringComparison.OrdinalIgnoreCase)
                    ? "已选择输入端口：可直接替换或清除真实订阅。"
                    : "已选择输出端口：可查看并定位全部真实消费者。");
            e.Handled = true;
        }

        private bool BeginFlowPortDrag(VmFlowPortChip chip, FrameworkElement element, System.Windows.Point startPoint)
        {
            if (chip == null || element == null || FlowGraphCanvas == null || isContinuousRunning || isFlowExecutionActive)
            {
                return false;
            }

            VmFlowPortChip sourcePort = chip;
            VmFlowPortChip fixedTargetPort = null;
            if (string.Equals(chip.Direction, "IN", StringComparison.OrdinalIgnoreCase))
            {
                VmToolInstance target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, chip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
                VmToolInputBindingData binding = target == null ? null : target.GetInputBinding(chip.PortName);
                if (target != null && target.Kind == VmToolKind.NumericJudge && string.Equals(chip.PortName, "Value", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(target.InputToolId))
                {
                    binding = new VmToolInputBindingData { TargetPortName = chip.PortName, SourceToolId = target.InputToolId, SourcePortName = target.InputPortName };
                }
                VmToolInstance currentSource = GetInputSourceTool(binding);
                sourcePort = currentSource == null || binding == null
                    ? null
                    : currentSource.FlowOutputPorts.FirstOrDefault(item => string.Equals(item.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                if (sourcePort == null)
                {
                    HeaderStatusText.Text = "该 IN 端口尚未连接；请从兼容 OUT 端口拖入。";
                    return false;
                }
                fixedTargetPort = chip;
            }

            flowPortDragSession = new FlowPortDragSession
            {
                OriginPort = chip,
                SourcePort = sourcePort,
                FixedTargetPort = fixedTargetPort,
                StartPoint = startPoint,
                HasMoved = false
            };
            FlowPortDragPreviewPath.Data = CreateFlowConnectionGeometry(startPoint.X, startPoint.Y, startPoint.X, startPoint.Y);
            FlowPortDragPreviewPath.Stroke = GetFlowConnectionBrush(chip.DataType);
            FlowPortDragPreviewPath.Visibility = Visibility.Visible;
            FlowPortDragEndpointText.Text = chip.OwnerToolName + "." + chip.PortName + " · " + chip.DataType;
            FlowPortDragValidationText.Text = fixedTargetPort == null ? "拖到兼容 IN 端口" : "拖到新的兼容 OUT 端口以替换来源";
            FlowPortDragStatusBorder.BorderBrush = GetFlowConnectionBrush(chip.DataType);
            FlowPortDragStatusBorder.Visibility = Visibility.Visible;
            PositionFlowPortDragStatus(startPoint);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                FlowGraphCanvas.CaptureMouse();
            }
            return true;
        }

        private VmFlowPortChip FindFlowPortChipAt(System.Windows.Point point)
        {
            DependencyObject current = FlowGraphCanvas == null ? null : FlowGraphCanvas.InputHitTest(point) as DependencyObject;
            while (current != null && !ReferenceEquals(current, FlowGraphCanvas))
            {
                FrameworkElement element = current as FrameworkElement;
                VmFlowPortChip chip = element == null ? null : element.Tag as VmFlowPortChip;
                if (chip != null) return chip;
                current = GetFlowVisualParent(current);
            }
            return null;
        }

        private bool IsFlowGraphDescendant(DependencyObject element)
        {
            DependencyObject current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, FlowGraphCanvas)) return true;
                current = GetFlowVisualParent(current);
            }
            return false;
        }

        private static DependencyObject GetFlowVisualParent(DependencyObject current)
        {
            if (current == null) return null;
            FrameworkContentElement content = current as FrameworkContentElement;
            if (content != null) return content.Parent;
            if (current is Visual || current is System.Windows.Media.Media3D.Visual3D) return VisualTreeHelper.GetParent(current);
            return LogicalTreeHelper.GetParent(current);
        }

        private bool TryResolveFlowPortDragEndpoints(
            FlowPortDragSession session,
            VmFlowPortChip hoverPort,
            out VmFlowPortChip sourcePort,
            out VmFlowPortChip targetPort,
            out string error)
        {
            sourcePort = null;
            targetPort = null;
            error = "请拖到兼容端口。";
            if (session == null || hoverPort == null) return false;
            if (session.FixedTargetPort == null)
            {
                sourcePort = session.SourcePort;
                targetPort = hoverPort;
            }
            else
            {
                sourcePort = hoverPort;
                targetPort = session.FixedTargetPort;
            }
            return EvaluateFlowPortConnection(sourcePort, targetPort, out error);
        }

        private bool EvaluateFlowPortConnection(VmFlowPortChip sourceChip, VmFlowPortChip targetChip, out string error)
        {
            error = string.Empty;
            if (sourceChip == null || targetChip == null ||
                !string.Equals(sourceChip.Direction, "OUT", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(targetChip.Direction, "IN", StringComparison.OrdinalIgnoreCase))
            {
                error = "只能从 OUT 连接到 IN。";
                return false;
            }

            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, sourceChip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            VmToolInstance target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, targetChip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition sourcePort = source == null ? null : ToolMetadata.GetOutputPorts(source.Kind).FirstOrDefault(item => string.Equals(item.PortName, sourceChip.PortName, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition targetPort = target == null ? null : ToolMetadata.GetInputPorts(target.Kind).FirstOrDefault(item => string.Equals(item.PortName, targetChip.PortName, StringComparison.OrdinalIgnoreCase));
            if (source == null || target == null || sourcePort == null || targetPort == null)
            {
                error = "端口所属模块或端口定义已失效。";
                return false;
            }
            if (!CanDockSubscribePort(target, targetPort))
            {
                error = target.InstanceName + "." + targetPort.PortName + " 暂不支持直接改订阅。";
                return false;
            }
            if (!source.IsEnabled)
            {
                error = source.InstanceName + " 已停用，不能作为新来源。";
                return false;
            }
            if (flowTools.IndexOf(source) >= flowTools.IndexOf(target))
            {
                error = "来源必须位于目标模块之前；当前执行仍按确定性顺序。";
                return false;
            }
            if (!string.Equals(sourcePort.DataType, targetPort.DataType, StringComparison.OrdinalIgnoreCase))
            {
                error = "类型不兼容：" + sourcePort.DataType + " 不能连接到 " + targetPort.DataType + "。";
                return false;
            }
            if (IsDuplicateMultiInputEndpoint(target, targetPort.PortName, source.ToolId, sourcePort.PortName))
            {
                error = "同一双输入模块的两个端口不能订阅同一个上游端点。";
                return false;
            }
            return true;
        }

        private bool TryApplyFlowPortBinding(VmFlowPortChip sourceChip, VmFlowPortChip targetChip, out string error)
        {
            if (!EvaluateFlowPortConnection(sourceChip, targetChip, out error)) return false;
            VmToolInstance source = flowTools.First(item => string.Equals(item.ToolId, sourceChip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            VmToolInstance target = flowTools.First(item => string.Equals(item.ToolId, targetChip.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition targetPort = ToolMetadata.GetInputPorts(target.Kind).First(item => string.Equals(item.PortName, targetChip.PortName, StringComparison.OrdinalIgnoreCase));
            VmToolInputBindingData previousBinding = target.GetInputBinding(targetPort.PortName);
            previousBinding = previousBinding == null ? null : previousBinding.Clone();
            string previousInputToolId = target.InputToolId;
            string previousInputPortName = target.InputPortName;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            SetSelectedFlowPortBinding(target, targetPort, source.ToolId, sourceChip.PortName);
            VmToolInputBindingData candidate = target.GetInputBinding(targetPort.PortName);
            error = target.Kind == VmToolKind.NumericJudge
                ? GetNumericJudgeConfigurationError(target)
                : GetInputBindingConfigurationError(target, candidate);
            if (!string.IsNullOrWhiteSpace(error))
            {
                RestoreSelectedFlowPortBinding(target, targetPort, previousBinding, previousInputToolId, previousInputPortName);
                return false;
            }

            selectedFlowConnectionKey = BuildFlowConnectionKey(source.ToolId, sourceChip.PortName, target.ToolId, targetPort.PortName);
            selectedFlowPortToolId = null;
            selectedFlowPortName = null;
            selectedFlowPortDirection = null;
            InvalidateToolRunResult(target, "输入端口拖拽连线已修改");
            InvalidateDownstreamResults(target, "上游输入订阅已修改");
            LogInfo("端口拖拽连线：" + source.InstanceName + "." + sourceChip.PortName + " → " + target.InstanceName + "." + targetPort.PortName);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("拖拽连接：" + target.InstanceName + "." + targetPort.PortName, before);
            RefreshUiState();
            HeaderStatusText.Text = "已写回真实 InputBindings：" + source.InstanceName + "." + sourceChip.PortName + " → " + target.InstanceName + "." + targetPort.PortName;
            return true;
        }

        private void UpdateFlowPortDragPreview(
            System.Windows.Point current,
            VmFlowPortChip sourcePort,
            VmFlowPortChip targetPort,
            bool hasHoverPort,
            bool valid,
            string error)
        {
            if (flowPortDragSession == null) return;
            FlowPortDragPreviewPath.Data = CreateFlowConnectionGeometry(
                flowPortDragSession.StartPoint.X,
                flowPortDragSession.StartPoint.Y,
                current.X,
                current.Y);
            SolidColorBrush brush = valid
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129))
                : (hasHoverPort ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)) : GetFlowConnectionBrush(flowPortDragSession.OriginPort.DataType));
            FlowPortDragPreviewPath.Stroke = brush;
            FlowPortDragStatusBorder.BorderBrush = brush;
            FlowPortDragValidationText.Foreground = brush;
            FlowPortDragEndpointText.Text = sourcePort == null || targetPort == null
                ? flowPortDragSession.OriginPort.OwnerToolName + "." + flowPortDragSession.OriginPort.PortName + " · " + flowPortDragSession.OriginPort.DataType
                : sourcePort.OwnerToolName + "." + sourcePort.PortName + " → " + targetPort.OwnerToolName + "." + targetPort.PortName + " · " + targetPort.DataType;
            FlowPortDragValidationText.Text = valid ? "可连接 · 放开写回真实 InputBindings" : (string.IsNullOrWhiteSpace(error) ? "拖到兼容端口" : error);
            PositionFlowPortDragStatus(current);
        }

        private void PositionFlowPortDragStatus(System.Windows.Point point)
        {
            if (FlowPortDragStatusBorder == null || FlowGraphCanvas == null) return;
            Canvas.SetLeft(FlowPortDragStatusBorder, Math.Max(8, Math.Min(FlowGraphCanvas.Width - 330, point.X + 14)));
            Canvas.SetTop(FlowPortDragStatusBorder, Math.Max(8, Math.Min(FlowGraphCanvas.Height - 70, point.Y + 14)));
        }

        private void CancelFlowPortDrag(string status, bool logStatus)
        {
            flowPortDragSession = null;
            if (FlowPortDragPreviewPath != null) FlowPortDragPreviewPath.Visibility = Visibility.Collapsed;
            if (FlowPortDragStatusBorder != null) FlowPortDragStatusBorder.Visibility = Visibility.Collapsed;
            if (FlowGraphCanvas != null && FlowGraphCanvas.IsMouseCaptured) Mouse.Capture(null);
            if (!string.IsNullOrWhiteSpace(status))
            {
                HeaderStatusText.Text = status;
                if (logStatus) LogInfo(status);
            }
        }

        private void UpdateFlowPortInspector()
        {
            if (FlowPortInspectorBorder == null)
            {
                return;
            }

            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selectedFlowPortToolId, StringComparison.OrdinalIgnoreCase));
            bool input = string.Equals(selectedFlowPortDirection, "IN", StringComparison.OrdinalIgnoreCase);
            VmPortDefinition port = tool == null
                ? null
                : (input ? ToolMetadata.GetInputPorts(tool.Kind) : ToolMetadata.GetOutputPorts(tool.Kind))
                    .FirstOrDefault(item => string.Equals(item.PortName, selectedFlowPortName, StringComparison.OrdinalIgnoreCase));
            bool graphVisible = FlowGraphViewport != null && FlowGraphViewport.Visibility == Visibility.Visible;
            if (tool == null || port == null || !graphVisible)
            {
                FlowPortInspectorBorder.Visibility = Visibility.Collapsed;
                if (tool == null || port == null)
                {
                    selectedFlowPortToolId = null;
                    selectedFlowPortName = null;
                    selectedFlowPortDirection = null;
                }
                return;
            }

            FlowPortInspectorBorder.Visibility = Visibility.Visible;
            FlowPortInspectorDirectionText.Text = input ? "IN · 输入订阅" : "OUT · 输出消费者";
            FlowPortInspectorEndpointText.Text = tool.InstanceName + "." + port.PortName + "（" + port.DisplayName + "）";
            FlowPortInspectorTypeText.Text = port.DataType;
            VmFlowPortChip chip = (input ? tool.FlowInputPorts : tool.FlowOutputPorts)
                .FirstOrDefault(item => string.Equals(item.PortName, port.PortName, StringComparison.OrdinalIgnoreCase));
            FlowPortInspectorStatusText.Text = chip == null ? "--" : chip.StatusText;
            FlowPortInspectorValueText.Text = chip == null || string.IsNullOrWhiteSpace(chip.ValueText) ? "--" : chip.ValueText;
            FlowPortInputEditorPanel.Visibility = input ? Visibility.Visible : Visibility.Collapsed;
            FlowPortOutputConsumerPanel.Visibility = input ? Visibility.Collapsed : Visibility.Visible;
            if (input)
            {
                BuildFlowPortSourceOptions(tool, port);
            }
            else
            {
                BuildFlowPortConsumers(tool, port);
            }
        }

        private void BuildFlowPortSourceOptions(VmToolInstance target, VmPortDefinition port)
        {
            flowPortSourceOptions.Clear();
            flowPortSourceOptions.Add(new VmInputSourceOption
            {
                Key = GetDockDefaultInputKey(port.PortName),
                DisplayText = GetDockDefaultInputText(port),
                DataType = port.DataType,
                IsDefault = true,
                IsValid = true
            });

            int targetIndex = flowTools.IndexOf(target);
            if (CanDockSubscribePort(target, port))
            {
                foreach (VmToolInstance source in flowTools.Take(Math.Max(0, targetIndex)))
                {
                    foreach (VmPortDefinition sourcePort in ToolMetadata.GetOutputPorts(source.Kind)
                        .Where(item => string.Equals(item.DataType, port.DataType, StringComparison.OrdinalIgnoreCase)))
                    {
                        flowPortSourceOptions.Add(new VmInputSourceOption
                        {
                            Key = BuildDockSourceKey(source.ToolId, sourcePort.PortName),
                            DisplayText = source.SequenceText + "  " + source.InstanceName + "." + sourcePort.PortName + " · " + sourcePort.DisplayName + (source.IsEnabled ? string.Empty : "（已停用）"),
                            SourceToolId = source.ToolId,
                            SourcePortName = sourcePort.PortName,
                            DataType = sourcePort.DataType,
                            IsValid = source.IsEnabled
                        });
                    }
                }
            }

            string savedKey = GetDockSavedInputSourceKey(target, port);
            if (!flowPortSourceOptions.Any(item => string.Equals(item.Key, savedKey, StringComparison.OrdinalIgnoreCase)))
            {
                VmToolInputBindingData savedBinding = target.GetInputBinding(port.PortName);
                if (target.Kind == VmToolKind.NumericJudge && port.PortName == "Value" && !string.IsNullOrWhiteSpace(target.InputToolId))
                {
                    savedBinding = new VmToolInputBindingData { TargetPortName = port.PortName, SourceToolId = target.InputToolId, SourcePortName = target.InputPortName };
                }
                flowPortSourceOptions.Add(new VmInputSourceOption
                {
                    Key = savedKey,
                    DisplayText = "⚠ 已保存来源无效、已停用或不在当前模块之前",
                    SourceToolId = savedBinding == null ? null : savedBinding.SourceToolId,
                    SourcePortName = savedBinding == null ? null : savedBinding.SourcePortName,
                    DataType = port.DataType,
                    IsValid = false
                });
            }
            FlowPortSourceComboBox.SelectedValue = savedKey;
            FlowPortInputValidationText.Text = CanDockSubscribePort(target, port)
                ? "仅可选择当前模块之前、类型兼容且已启用的输出。"
                : "该兼容端口暂不支持从画布直接改订阅；可打开输入页查看。";
        }

        private void BuildFlowPortConsumers(VmToolInstance source, VmPortDefinition sourcePort)
        {
            flowPortConsumers.Clear();
            foreach (VmToolInstance target in flowTools)
            {
                foreach (VmToolInputBindingData binding in GetFlowGraphBindings(target).Where(item =>
                    string.Equals(item.SourceToolId, source.ToolId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.SourcePortName, sourcePort.PortName, StringComparison.OrdinalIgnoreCase)))
                {
                    string error = target.Kind == VmToolKind.NumericJudge
                        ? GetNumericJudgeConfigurationError(target)
                        : GetInputBindingConfigurationError(target, binding);
                    flowPortConsumers.Add(new VmFlowPortConsumerOption
                    {
                        ConnectionKey = BuildFlowConnectionKey(source.ToolId, sourcePort.PortName, target.ToolId, binding.TargetPortName),
                        TargetToolId = target.ToolId,
                        TargetPortName = binding.TargetPortName,
                        DisplayText = target.SequenceText + "  " + target.InstanceName + "." + binding.TargetPortName,
                        StatusText = string.IsNullOrWhiteSpace(error)
                            ? "连接有效 · 当前值 " + source.GetFormattedOutput(sourcePort.PortName)
                            : "连接异常 · " + error
                    });
                }
            }
            FlowPortConsumerCountText.Text = flowPortConsumers.Count.ToString(CultureInfo.InvariantCulture) + " 个";
            FlowPortNoConsumerText.Visibility = flowPortConsumers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FlowPortConsumerListBox.Visibility = flowPortConsumers.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            FlowPortConsumerListBox.SelectedIndex = flowPortConsumers.Count == 0 ? -1 : 0;
        }

        private void CloseFlowPortInspectorButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFlowPortToolId = null;
            selectedFlowPortName = null;
            selectedFlowPortDirection = null;
            RefreshFlowPortVisualization();
            HeaderStatusText.Text = "已关闭端口订阅与消费者导航器。";
        }

        private void ApplyFlowPortSourceButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance target;
            VmPortDefinition port;
            if (!TryGetSelectedInputFlowPort(out target, out port))
            {
                HeaderStatusText.Text = "请选择一个输入端口。";
                return;
            }

            VmInputSourceOption selected = FlowPortSourceComboBox.SelectedItem as VmInputSourceOption;
            if (selected == null)
            {
                FlowPortInputValidationText.Text = "请选择输入来源。";
                return;
            }
            if (selected.IsDefault)
            {
                ClearSelectedFlowPortBinding(target, port, "输入端口已清除订阅");
                return;
            }
            if (!selected.IsValid)
            {
                FlowPortInputValidationText.Text = "所选来源已停用、无效或不满足前序顺序约束。";
                return;
            }
            if (IsDuplicateMultiInputEndpoint(target, port.PortName, selected.SourceToolId, selected.SourcePortName))
            {
                FlowPortInputValidationText.Text = "同一双输入模块的两个端口不能订阅同一个上游端点。";
                return;
            }

            VmToolInputBindingData previousBinding = target.GetInputBinding(port.PortName);
            previousBinding = previousBinding == null ? null : previousBinding.Clone();
            string previousInputToolId = target.InputToolId;
            string previousInputPortName = target.InputPortName;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            SetSelectedFlowPortBinding(target, port, selected.SourceToolId, selected.SourcePortName);
            VmToolInputBindingData candidate = target.GetInputBinding(port.PortName) ?? new VmToolInputBindingData
            {
                TargetPortName = port.PortName,
                SourceToolId = selected.SourceToolId,
                SourcePortName = selected.SourcePortName
            };
            string error = target.Kind == VmToolKind.NumericJudge
                ? GetNumericJudgeConfigurationError(target)
                : GetInputBindingConfigurationError(target, candidate);
            if (!string.IsNullOrWhiteSpace(error))
            {
                RestoreSelectedFlowPortBinding(target, port, previousBinding, previousInputToolId, previousInputPortName);
                FlowPortInputValidationText.Text = error;
                HeaderStatusText.Text = "订阅替换失败：" + error;
                return;
            }

            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selected.SourceToolId, StringComparison.OrdinalIgnoreCase));
            InvalidateToolRunResult(target, "输入端口订阅已替换");
            InvalidateDownstreamResults(target, "上游输入订阅已替换");
            LogInfo("端口订阅已替换：" + (source == null ? selected.SourceToolId : source.InstanceName) + "." + selected.SourcePortName + " → " + target.InstanceName + "." + port.PortName);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("替换订阅：" + target.InstanceName + "." + port.PortName, before);
            RefreshUiState();
            HeaderStatusText.Text = "已写回真实 InputBindings：" + target.InstanceName + "." + port.PortName;
        }

        private bool TryGetSelectedInputFlowPort(out VmToolInstance target, out VmPortDefinition port)
        {
            target = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selectedFlowPortToolId, StringComparison.OrdinalIgnoreCase));
            port = target == null || !string.Equals(selectedFlowPortDirection, "IN", StringComparison.OrdinalIgnoreCase)
                ? null
                : ToolMetadata.GetInputPorts(target.Kind).FirstOrDefault(item => string.Equals(item.PortName, selectedFlowPortName, StringComparison.OrdinalIgnoreCase));
            return target != null && port != null;
        }

        private static void SetSelectedFlowPortBinding(VmToolInstance target, VmPortDefinition port, string sourceToolId, string sourcePortName)
        {
            target.SetInputBinding(port.PortName, sourceToolId, sourcePortName);
            if (target.Kind == VmToolKind.NumericJudge && string.Equals(port.PortName, "Value", StringComparison.OrdinalIgnoreCase))
            {
                target.InputToolId = sourceToolId;
                target.InputPortName = sourcePortName;
            }
        }

        private static void RestoreSelectedFlowPortBinding(VmToolInstance target, VmPortDefinition port, VmToolInputBindingData previousBinding, string previousInputToolId, string previousInputPortName)
        {
            target.RemoveInputBinding(port.PortName);
            if (previousBinding != null)
            {
                target.SetInputBinding(previousBinding.TargetPortName, previousBinding.SourceToolId, previousBinding.SourcePortName);
            }
            if (target.Kind == VmToolKind.NumericJudge && string.Equals(port.PortName, "Value", StringComparison.OrdinalIgnoreCase))
            {
                target.InputToolId = previousInputToolId;
                target.InputPortName = previousInputPortName;
            }
        }

        private void ClearFlowPortSourceButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance target;
            VmPortDefinition port;
            if (!TryGetSelectedInputFlowPort(out target, out port))
            {
                HeaderStatusText.Text = "请选择一个输入端口。";
                return;
            }
            ClearSelectedFlowPortBinding(target, port, "输入端口已清除订阅");
        }

        private void ClearSelectedFlowPortBinding(VmToolInstance target, VmPortDefinition port, string reason)
        {
            if (target == null || port == null || target.GetInputBinding(port.PortName) == null) return;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            target.RemoveInputBinding(port.PortName);
            if (target.Kind == VmToolKind.NumericJudge && string.Equals(port.PortName, "Value", StringComparison.OrdinalIgnoreCase))
            {
                target.InputToolId = null;
                target.InputPortName = null;
            }
            InvalidateToolRunResult(target, reason);
            InvalidateDownstreamResults(target, "上游输入订阅已清除");
            LogInfo("端口订阅已清除：" + target.InstanceName + "." + port.PortName);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("清除订阅：" + target.InstanceName + "." + port.PortName, before);
            RefreshUiState();
            HeaderStatusText.Text = "已清除真实 InputBindings；必需端口会保持可见的待连接状态。";
        }

        private void OpenSelectedFlowPortInputButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance target;
            VmPortDefinition port;
            if (!TryGetSelectedInputFlowPort(out target, out port))
            {
                HeaderStatusText.Text = "请选择一个输入端口。";
                return;
            }
            OpenToolInputWorkspace(target, port.PortName);
        }

        private void OpenToolInputWorkspace(VmToolInstance target, string targetPortName)
        {
            FlowToolList.SelectedItem = target;
            if (SupportsDockConfiguration(target.Kind))
            {
                RightTabs.SelectedItem = DockConfigurationTab;
                if (!ReferenceEquals(dockEditingTool, target))
                {
                    LoadDockConfigurationDraft(target);
                }
                DockConfigurationTabs.SelectedIndex = 2;
                VmInputPortEditorRow row = dockInputPortRows.FirstOrDefault(item => string.Equals(item.PortName, targetPortName, StringComparison.OrdinalIgnoreCase));
                if (row != null)
                {
                    DockInputPortItemsControl.SelectedItem = row;
                    DockInputPortItemsControl.ScrollIntoView(row);
                }
                HeaderStatusText.Text = "已打开 " + target.InstanceName + "." + targetPortName + " 的输入订阅。";
            }
            else
            {
                RightTabs.SelectedItem = IoTab;
                HeaderStatusText.Text = target.InstanceName + " 尚在兼容配置边界，已打开其真实 I/O 端口页。";
            }
        }

        private void LocateFlowPortConsumerButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowPortConsumerOption consumer = FlowPortConsumerListBox.SelectedItem as VmFlowPortConsumerOption;
            VmFlowConnectionVisual connection = consumer == null
                ? null
                : flowConnections.FirstOrDefault(item => string.Equals(item.ConnectionKey, consumer.ConnectionKey, StringComparison.OrdinalIgnoreCase));
            if (connection == null)
            {
                HeaderStatusText.Text = "请选择一个仍然有效的消费者。";
                return;
            }
            SelectFlowConnection(connection);
            FocusFlowGraphTool(connection.TargetToolId, "已定位消费者并选中对应真实连接。" );
        }

        private void LocateSelectedFlowPortOwnerButton_Click(object sender, RoutedEventArgs e)
        {
            FocusFlowGraphTool(selectedFlowPortToolId, "已定位所选输出端口所属模块。" );
        }

        private void CloseFlowConnectionInspectorButton_Click(object sender, RoutedEventArgs e)
        {
            selectedFlowConnectionKey = null;
            RefreshFlowPortVisualization();
            HeaderStatusText.Text = "已关闭连接检查器。";
        }

        private void LocateFlowConnectionSourceButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            FocusFlowGraphTool(connection == null ? null : connection.SourceToolId, "已定位所选连接的来源模块。");
        }

        private void LocateFlowConnectionTargetButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            FocusFlowGraphTool(connection == null ? null : connection.TargetToolId, "已定位所选连接的目标模块。");
        }

        private void FocusFlowGraphTool(string toolId, string statusText)
        {
            VmToolInstance tool = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
            if (tool == null)
            {
                HeaderStatusText.Text = "连接端点模块不存在，无法定位。";
                return;
            }

            FlowToolList.SelectedItem = tool;
            SetFlowEditorView(true);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                double zoom = FlowZoomSlider == null ? 1 : FlowZoomSlider.Value;
                FlowGraphViewport.ScrollToHorizontalOffset(Math.Max(0, (tool.GraphX + 180) * zoom - FlowGraphViewport.ViewportWidth / 2.0));
                FlowGraphViewport.ScrollToVerticalOffset(Math.Max(0, (tool.GraphY + tool.GraphHeight / 2.0) * zoom - FlowGraphViewport.ViewportHeight / 2.0));
            }));
            HeaderStatusText.Text = statusText;
        }

        private void OpenFlowConnectionTargetInputButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            VmToolInstance target = connection == null
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, connection.TargetToolId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                HeaderStatusText.Text = "连接目标模块不存在，无法打开输入配置。";
                return;
            }

            FlowToolList.SelectedItem = target;
            if (SupportsDockConfiguration(target.Kind))
            {
                RightTabs.SelectedItem = DockConfigurationTab;
                if (!ReferenceEquals(dockEditingTool, target))
                {
                    LoadDockConfigurationDraft(target);
                }
                DockConfigurationTabs.SelectedIndex = 2;
                VmInputPortEditorRow row = dockInputPortRows.FirstOrDefault(item => string.Equals(item.PortName, connection.TargetPortName, StringComparison.OrdinalIgnoreCase));
                if (row != null)
                {
                    DockInputPortItemsControl.SelectedItem = row;
                    DockInputPortItemsControl.ScrollIntoView(row);
                }
                HeaderStatusText.Text = "已打开 " + target.InstanceName + "." + connection.TargetPortName + " 的输入订阅。";
            }
            else
            {
                RightTabs.SelectedItem = IoTab;
                HeaderStatusText.Text = target.InstanceName + " 尚在兼容配置边界，已打开其真实 I/O 端口页。";
            }
        }

        private void ClearFlowConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            VmToolInstance target = connection == null
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, connection.TargetToolId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                HeaderStatusText.Text = "连接目标模块不存在，无法清除。";
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            if (target.Kind == VmToolKind.NumericJudge && string.Equals(connection.TargetPortName, "Value", StringComparison.OrdinalIgnoreCase))
            {
                target.InputToolId = null;
                target.InputPortName = null;
                target.RemoveInputBinding(connection.TargetPortName);
            }
            else
            {
                target.RemoveInputBinding(connection.TargetPortName);
            }
            selectedFlowConnectionKey = null;
            InvalidateToolRunResult(target, "输入连接已清除");
            InvalidateDownstreamResults(target, "上游输入连接已清除");
            LogInfo("连接已清除：" + connection.SourceEndpointText + " → " + connection.TargetEndpointText);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("清除连接：" + target.InstanceName + "." + connection.TargetPortName, before);
            RefreshUiState();
            HeaderStatusText.Text = "已从真实 InputBindings 清除 " + target.InstanceName + "." + connection.TargetPortName + "。";
        }

        private void RepairFlowConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            VmToolInstance target = connection == null
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, connection.TargetToolId, StringComparison.OrdinalIgnoreCase));
            VmPortDefinition targetPort = target == null
                ? null
                : ToolMetadata.GetInputPorts(target.Kind).FirstOrDefault(item => string.Equals(item.PortName, connection.TargetPortName, StringComparison.OrdinalIgnoreCase));
            if (target == null || targetPort == null)
            {
                HeaderStatusText.Text = "目标端口不存在，无法自动修复。";
                return;
            }

            int targetIndex = flowTools.IndexOf(target);
            VmToolInstance repairedSource = null;
            VmPortDefinition repairedPort = null;
            foreach (VmToolInstance source in flowTools.Take(Math.Max(0, targetIndex)).Reverse().Where(item => item.IsEnabled))
            {
                foreach (VmPortDefinition sourcePort in ToolMetadata.GetOutputPorts(source.Kind).Where(item => string.Equals(item.DataType, targetPort.DataType, StringComparison.OrdinalIgnoreCase)))
                {
                    if (IsDuplicateMultiInputEndpoint(target, connection.TargetPortName, source.ToolId, sourcePort.PortName))
                    {
                        continue;
                    }
                    repairedSource = source;
                    repairedPort = sourcePort;
                    break;
                }
                if (repairedSource != null) break;
            }

            if (repairedSource == null || repairedPort == null)
            {
                HeaderStatusText.Text = "自动修复失败：当前模块之前没有可用的 " + targetPort.DataType + " 输出。";
                return;
            }

            VmToolInputBindingData previousBinding = target.GetInputBinding(connection.TargetPortName);
            previousBinding = previousBinding == null ? null : previousBinding.Clone();
            string previousInputToolId = target.InputToolId;
            string previousInputPortName = target.InputPortName;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            if (target.Kind == VmToolKind.NumericJudge && string.Equals(connection.TargetPortName, "Value", StringComparison.OrdinalIgnoreCase))
            {
                target.InputToolId = repairedSource.ToolId;
                target.InputPortName = repairedPort.PortName;
                target.SetInputBinding(connection.TargetPortName, repairedSource.ToolId, repairedPort.PortName);
            }
            else
            {
                target.SetInputBinding(connection.TargetPortName, repairedSource.ToolId, repairedPort.PortName);
            }

            VmToolInputBindingData repairedBinding = new VmToolInputBindingData
            {
                TargetPortName = connection.TargetPortName,
                SourceToolId = repairedSource.ToolId,
                SourcePortName = repairedPort.PortName
            };
            string repairError = target.Kind == VmToolKind.NumericJudge
                ? GetNumericJudgeConfigurationError(target)
                : GetInputBindingConfigurationError(target, repairedBinding);
            if (!string.IsNullOrWhiteSpace(repairError))
            {
                if (target.Kind == VmToolKind.NumericJudge && string.Equals(connection.TargetPortName, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    target.InputToolId = previousInputToolId;
                    target.InputPortName = previousInputPortName;
                    target.RemoveInputBinding(connection.TargetPortName);
                    if (previousBinding != null)
                    {
                        target.SetInputBinding(previousBinding.TargetPortName, previousBinding.SourceToolId, previousBinding.SourcePortName);
                    }
                }
                else if (previousBinding == null)
                {
                    target.RemoveInputBinding(connection.TargetPortName);
                }
                else
                {
                    target.SetInputBinding(previousBinding.TargetPortName, previousBinding.SourceToolId, previousBinding.SourcePortName);
                }
                HeaderStatusText.Text = "自动修复失败：" + repairError;
                return;
            }

            selectedFlowConnectionKey = BuildFlowConnectionKey(repairedSource.ToolId, repairedPort.PortName, target.ToolId, connection.TargetPortName);
            InvalidateToolRunResult(target, "输入连接已修复");
            InvalidateDownstreamResults(target, "上游输入连接已修复");
            LogInfo("连接已自动修复：" + repairedSource.InstanceName + "." + repairedPort.PortName + " → " + target.InstanceName + "." + connection.TargetPortName);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("自动修复连接：" + target.InstanceName + "." + connection.TargetPortName, before);
            RefreshUiState();
            HeaderStatusText.Text = "已用最近的兼容前序端口修复真实输入连接。";
        }

        private bool IsDuplicateMultiInputEndpoint(VmToolInstance target, string targetPortName, string sourceToolId, string sourcePortName)
        {
            if (target == null || target.Kind != VmToolKind.RegionSetOperation && target.Kind != VmToolKind.DynamicThreshold && target.Kind != VmToolKind.ImageDifference)
            {
                return false;
            }

            return target.InputBindings.Any(item =>
                !string.Equals(item.TargetPortName, targetPortName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.SourceToolId, sourceToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.SourcePortName, sourcePortName, StringComparison.OrdinalIgnoreCase));
        }

        private void FitSelectedFlowConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            VmFlowConnectionVisual connection = GetSelectedFlowConnection();
            VmToolInstance source = connection == null ? null : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, connection.SourceToolId, StringComparison.OrdinalIgnoreCase));
            VmToolInstance target = connection == null ? null : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, connection.TargetToolId, StringComparison.OrdinalIgnoreCase));
            if (source == null || target == null)
            {
                HeaderStatusText.Text = "所选连接缺少完整端点，无法适应依赖。";
                return;
            }

            SetFlowEditorView(true);
            double left = Math.Min(source.GraphX, target.GraphX);
            double top = Math.Min(source.GraphY, target.GraphY);
            double right = Math.Max(source.GraphX + 360, target.GraphX + 360);
            double bottom = Math.Max(source.GraphY + source.GraphHeight, target.GraphY + target.GraphHeight);
            double availableWidth = Math.Max(320, FlowGraphViewport.ActualWidth - 80);
            double availableHeight = Math.Max(240, FlowGraphViewport.ActualHeight - 80);
            double zoom = Math.Max(0.35, Math.Min(1.8, Math.Min(availableWidth / Math.Max(1, right - left), availableHeight / Math.Max(1, bottom - top))));
            FlowZoomSlider.Value = zoom;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                FlowGraphViewport.ScrollToHorizontalOffset(Math.Max(0, (left + right) * 0.5 * zoom - FlowGraphViewport.ViewportWidth * 0.5));
                FlowGraphViewport.ScrollToVerticalOffset(Math.Max(0, (top + bottom) * 0.5 * zoom - FlowGraphViewport.ViewportHeight * 0.5));
            }));
            HeaderStatusText.Text = "已适应所选连接的两个真实依赖端点。";
        }

        private void FlowGraphViewButton_Click(object sender, RoutedEventArgs e)
        {
            SetFlowEditorView(true);
        }

        private void FlowListViewButton_Click(object sender, RoutedEventArgs e)
        {
            SetFlowEditorView(false);
        }

        private void SetFlowEditorView(bool graphView)
        {
            if (FlowGraphViewport == null || FlowToolList == null) return;
            FlowGraphViewport.Visibility = graphView ? Visibility.Visible : Visibility.Collapsed;
            FlowToolList.Visibility = graphView ? Visibility.Collapsed : Visibility.Visible;
            FlowGraphViewButton.IsChecked = graphView;
            FlowListViewButton.IsChecked = !graphView;
            if (graphView) RefreshFlowGraphLayout();
            UpdateFlowConnectionInspector();
            UpdateFlowPortInspector();
            HeaderStatusText.Text = graphView
                ? "已切换连接画布：连线来自真实端口订阅，执行仍按确定性顺序。"
                : "已切换顺序列表：可精确拖拽调整确定性执行位置。";
        }

        private void AutoLayoutFlowButton_Click(object sender, RoutedEventArgs e)
        {
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能重置画布布局。";
                return;
            }
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            isManualFlowLayout = false;
            foreach (VmToolInstance tool in flowTools) tool.HasManualGraphPosition = false;
            RefreshFlowGraphLayout();
            SetFlowEditorView(true);
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("恢复自动布局", before);
            LogInfo("连接画布已恢复依赖自动布局；配方中的手动坐标已清除，执行顺序和连接保持不变。");
            HeaderStatusText.Text = "已按真实输入依赖自动分层布局；这不改变确定性执行顺序。";
        }

        private void EnableManualFlowLayout()
        {
            if (!isManualFlowLayout)
            {
                RefreshFlowGraphLayout();
                isManualFlowLayout = true;
            }
            foreach (VmToolInstance tool in flowTools) tool.HasManualGraphPosition = true;
        }

        private double SnapFlowCoordinate(double value)
        {
            bool snap = FlowSnapGridCheckBox == null || FlowSnapGridCheckBox.IsChecked == true;
            return snap ? Math.Round(value / FlowLayoutGridSize) * FlowLayoutGridSize : Math.Round(value, 1);
        }

        private void CompleteFlowNodeDrag(bool report)
        {
            FlowNodeDragSession session = flowNodeDragSession;
            flowNodeDragSession = null;
            Mouse.Capture(null);
            if (session == null || !session.HasMoved) return;
            RefreshFlowGraphLayout();
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("移动画布模块", session.BeforeSnapshot);
            if (report)
            {
                string message = "画布手动布局已更新：移动 " + session.InitialPositions.Count.ToString(CultureInfo.InvariantCulture) + " 个模块，坐标进入配方；流程顺序和端口订阅未改变。";
                HeaderStatusText.Text = message;
                LogInfo(message);
            }
        }

        private void CancelFlowNodeDrag()
        {
            FlowNodeDragSession session = flowNodeDragSession;
            flowNodeDragSession = null;
            Mouse.Capture(null);
            if (session == null) return;
            foreach (KeyValuePair<VmToolInstance, System.Windows.Point> entry in session.InitialPositions)
            {
                entry.Key.GraphX = entry.Value.X;
                entry.Key.GraphY = entry.Value.Y;
            }
            foreach (KeyValuePair<VmToolInstance, bool> entry in session.InitialManualFlags)
            {
                entry.Key.HasManualGraphPosition = entry.Value;
            }
            isManualFlowLayout = session.InitialManualMode;
            RefreshFlowGraphLayout();
            HeaderStatusText.Text = "已取消模块移动；原布局、执行顺序和连接保持不变。";
        }

        private bool PrepareManualLayoutSelection(int minimumCount, string action, out List<VmToolInstance> selected)
        {
            selected = GetSelectedFlowToolsInSequence();
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能" + action + "。";
                return false;
            }
            if (selected.Count < minimumCount)
            {
                HeaderStatusText.Text = action + "至少需要选择 " + minimumCount.ToString(CultureInfo.InvariantCulture) + " 个模块。";
                return false;
            }
            EnableManualFlowLayout();
            return true;
        }

        private void CommitManualLayoutOperation(string message, FlowEditSnapshot before)
        {
            foreach (VmToolInstance tool in flowTools) tool.HasManualGraphPosition = true;
            RefreshFlowGraphLayout();
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory(message, before);
            HeaderStatusText.Text = message + "；执行顺序和连接未改变。";
            LogInfo(HeaderStatusText.Text);
        }

        private void AlignFlowToolsHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            List<VmToolInstance> selected;
            if (!PrepareManualLayoutSelection(2, "水平对齐", out selected)) return;
            double y = SnapFlowCoordinate(selected[0].GraphY);
            foreach (VmToolInstance tool in selected) tool.GraphY = y;
            CommitManualLayoutOperation("已将 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块水平对齐", before);
        }

        private void AlignFlowToolsVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            List<VmToolInstance> selected;
            if (!PrepareManualLayoutSelection(2, "垂直对齐", out selected)) return;
            double x = SnapFlowCoordinate(selected[0].GraphX);
            foreach (VmToolInstance tool in selected) tool.GraphX = x;
            CommitManualLayoutOperation("已将 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块垂直对齐", before);
        }

        private void DistributeFlowToolsHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            List<VmToolInstance> selected;
            if (!PrepareManualLayoutSelection(3, "横向等距", out selected)) return;
            List<VmToolInstance> ordered = selected.OrderBy(item => item.GraphX).ToList();
            double step = (ordered[ordered.Count - 1].GraphX - ordered[0].GraphX) / (ordered.Count - 1);
            for (int index = 1; index < ordered.Count - 1; index++) ordered[index].GraphX = SnapFlowCoordinate(ordered[0].GraphX + step * index);
            CommitManualLayoutOperation("已将 " + ordered.Count.ToString(CultureInfo.InvariantCulture) + " 个模块横向等距分布", before);
        }

        private void DistributeFlowToolsVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            List<VmToolInstance> selected;
            if (!PrepareManualLayoutSelection(3, "纵向等距", out selected)) return;
            List<VmToolInstance> ordered = selected.OrderBy(item => item.GraphY).ToList();
            double step = (ordered[ordered.Count - 1].GraphY - ordered[0].GraphY) / (ordered.Count - 1);
            for (int index = 1; index < ordered.Count - 1; index++) ordered[index].GraphY = SnapFlowCoordinate(ordered[0].GraphY + step * index);
            CommitManualLayoutOperation("已将 " + ordered.Count.ToString(CultureInfo.InvariantCulture) + " 个模块纵向等距分布", before);
        }

        private void FlowGraphViewport_PreviewMouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            isFlowGraphPanning = true;
            flowGraphPanStart = e.GetPosition(FlowGraphViewport);
            flowGraphPanHorizontalOffset = FlowGraphViewport.HorizontalOffset;
            flowGraphPanVerticalOffset = FlowGraphViewport.VerticalOffset;
            FlowGraphViewport.CaptureMouse();
            e.Handled = true;
        }

        private void FlowGraphViewport_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isFlowGraphPanning || e.MiddleButton != MouseButtonState.Pressed) return;
            System.Windows.Point current = e.GetPosition(FlowGraphViewport);
            FlowGraphViewport.ScrollToHorizontalOffset(flowGraphPanHorizontalOffset - (current.X - flowGraphPanStart.X));
            FlowGraphViewport.ScrollToVerticalOffset(flowGraphPanVerticalOffset - (current.Y - flowGraphPanStart.Y));
            e.Handled = true;
        }

        private void FlowGraphViewport_PreviewMouseMiddleButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            isFlowGraphPanning = false;
            FlowGraphViewport.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void HideFlowDropHint()
        {
            if (FlowDropHintBorder != null)
            {
                FlowDropHintBorder.Visibility = Visibility.Collapsed;
            }
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

            InsertCatalogTool(catalogItem, flowTools.Count, "追加");
        }

        private VmToolInstance InsertCatalogTool(VmToolCatalogItem catalogItem, int insertionIndex, string action)
        {
            if (catalogItem == null)
            {
                return null;
            }

            FlowEditSnapshot before = suppressFlowInsertHistory ? null : CaptureFlowEditSnapshot();
            int targetIndex = Math.Max(0, Math.Min(insertionIndex, flowTools.Count));
            VmToolInstance instance = CreateFlowTool(catalogItem.Kind, null, true, null);
            flowTools.Insert(targetIndex, instance);
            if (isManualFlowLayout)
            {
                System.Windows.Point position = GetNextManualFlowPosition(targetIndex);
                instance.GraphX = position.X;
                instance.GraphY = position.Y;
                instance.HasManualGraphPosition = true;
            }
            VmRoiLayer selectedLayer = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
            if (selectedLayer != null && ToolMetadata.SupportsRoi(instance.Kind))
            {
                instance.BindRoi(selectedLayer.RoiId);
            }
            if (instance.Kind == VmToolKind.NumericJudge)
            {
                AutoBindNumericJudge(instance);
            }
            FlowToolList.SelectedItem = instance;
            FlowToolList.ScrollIntoView(instance);
            RecordToolCatalogUsage(catalogItem.Kind);
            LogInfo("已从工具箱" + action + "：" + instance.InstanceName + "，线性位置 " + (targetIndex + 1).ToString(CultureInfo.InvariantCulture));
            HeaderStatusText.Text = instance.InstanceName + " 已插入为第 " + (targetIndex + 1).ToString(CultureInfo.InvariantCulture) + " 步；流程仍按确定性线性顺序执行。";
            RefreshRoiLayerBindingSummaries();
            CommitFlowEditHistory("添加模块：" + instance.InstanceName, before);
            RefreshUiState();
            return instance;
        }

        private System.Windows.Point GetNextManualFlowPosition(int insertionIndex)
        {
            VmToolInstance anchor = insertionIndex > 0 && insertionIndex - 1 < flowTools.Count
                ? flowTools[insertionIndex - 1]
                : flowTools.Where(item => item.HasManualGraphPosition).LastOrDefault();
            if (anchor != null && !ReferenceEquals(anchor, flowTools.ElementAtOrDefault(insertionIndex)))
            {
                return new System.Windows.Point(SnapFlowCoordinate(anchor.GraphX + 48), SnapFlowCoordinate(anchor.GraphY + 48));
            }
            double maxY = flowTools.Where(item => item.HasManualGraphPosition).Select(item => item.GraphY + item.GraphHeight + FlowLayoutGridSize).DefaultIfEmpty(48).Max();
            return new System.Windows.Point(48, SnapFlowCoordinate(maxY));
        }

        private void RecordToolCatalogUsage(VmToolKind kind)
        {
            recentToolKinds.Remove(kind);
            recentToolKinds.Insert(0, kind);
            while (recentToolKinds.Count > 8)
            {
                recentToolKinds.RemoveAt(recentToolKinds.Count - 1);
            }
            RefreshToolCatalogView();
            SaveLayoutState();
        }

        private List<VmToolInstance> GetSelectedFlowToolsInSequence()
        {
            List<VmToolInstance> selected = FlowToolList == null
                ? new List<VmToolInstance>()
                : FlowToolList.SelectedItems.Cast<VmToolInstance>().Distinct().ToList();
            if (selected.Count == 0 && FlowToolList != null && FlowToolList.SelectedItem is VmToolInstance)
            {
                selected.Add((VmToolInstance)FlowToolList.SelectedItem);
            }

            return selected.OrderBy(item => flowTools.IndexOf(item)).ToList();
        }

        private static VmToolClipboardItem CaptureFlowToolClipboardItem(VmToolInstance tool, int sourceSequence)
        {
            return new VmToolClipboardItem
            {
                SourceToolId = tool.ToolId,
                SourceSequence = sourceSequence,
                Kind = tool.Kind,
                InstanceName = tool.InstanceName,
                IsEnabled = tool.IsEnabled,
                Parameters = (tool.Parameters ?? new VmToolParameterData()).Clone(),
                RoiIds = tool.BoundRoiIds.ToList(),
                InputBindings = tool.InputBindings.Select(item => item.Clone()).ToList(),
                InputToolId = tool.InputToolId,
                InputPortName = tool.InputPortName,
                NumericOperator = tool.NumericOperator,
                NumericLowerLimit = tool.NumericLowerLimit,
                NumericUpperLimit = tool.NumericUpperLimit,
                NumericTolerance = tool.NumericTolerance,
                HasManualGraphPosition = tool.HasManualGraphPosition,
                GraphX = tool.GraphX,
                GraphY = tool.GraphY,
                GroupId = tool.GroupId,
                GroupName = tool.GroupName,
                IsGroupCollapsed = tool.IsGroupCollapsed
            };
        }

        private int CopySelectedFlowToolsToClipboard()
        {
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (selected.Count == 0)
            {
                HeaderStatusText.Text = "请先选择要复制的流程模块。";
                return 0;
            }

            flowToolClipboard.Clear();
            for (int index = 0; index < selected.Count; index++)
            {
                flowToolClipboard.Add(CaptureFlowToolClipboardItem(selected[index], index));
            }
            HashSet<string> selectedIds = new HashSet<string>(selected.Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
            foreach (IGrouping<string, VmToolClipboardItem> clipboardGroup in flowToolClipboard.Where(item => !string.IsNullOrWhiteSpace(item.GroupId)).GroupBy(item => item.GroupId, StringComparer.OrdinalIgnoreCase).ToList())
            {
                int actualMemberCount = flowTools.Count(item => string.Equals(item.GroupId, clipboardGroup.Key, StringComparison.OrdinalIgnoreCase));
                if (clipboardGroup.Count() == actualMemberCount && clipboardGroup.All(item => selectedIds.Contains(item.SourceToolId))) continue;
                foreach (VmToolClipboardItem item in clipboardGroup)
                {
                    item.GroupId = null;
                    item.GroupName = null;
                    item.IsGroupCollapsed = false;
                }
            }

            flowPasteInsertionIndex = selected.Max(item => flowTools.IndexOf(item)) + 1;
            RefreshFlowClipboardStatus();
            HeaderStatusText.Text = "已复制 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块；运行结果、快照、耗时和错误不会进入剪贴板。";
            LogInfo("流程剪贴板已复制：" + string.Join("、", selected.Select(item => item.InstanceName)));
            return selected.Count;
        }

        private string CreateUniqueToolCopyName(string sourceName)
        {
            string baseName = string.IsNullOrWhiteSpace(sourceName) ? "模块副本" : sourceName.Trim() + "_副本";
            string candidate = baseName;
            int index = 1;
            while (flowTools.Any(item => string.Equals(item.InstanceName, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                index++;
                candidate = baseName + "_" + index.ToString("00", CultureInfo.InvariantCulture);
            }

            return candidate;
        }

        private int ResolveFlowPasteInsertionIndex()
        {
            if (flowPasteInsertionIndex.HasValue)
            {
                return Math.Max(0, Math.Min(flowPasteInsertionIndex.Value, flowTools.Count));
            }

            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            return selected.Count == 0 ? flowTools.Count : selected.Max(item => flowTools.IndexOf(item)) + 1;
        }

        private int PasteFlowToolClipboard(int insertionIndex)
        {
            if (flowToolClipboard.Count == 0)
            {
                HeaderStatusText.Text = "流程剪贴板为空，请先复制模块。";
                return 0;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            int targetIndex = Math.Max(0, Math.Min(insertionIndex, flowTools.Count));
            foreach (VmToolInstance existing in flowTools.Skip(targetIndex).ToList())
            {
                InvalidateToolRunResult(existing, "流程已插入模块副本");
            }

            List<VmToolClipboardItem> orderedEntries = flowToolClipboard.OrderBy(item => item.SourceSequence).ToList();
            Dictionary<string, string> idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> groupIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> groupNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<VmToolInstance> pasted = new List<VmToolInstance>();
            foreach (VmToolClipboardItem entry in orderedEntries)
            {
                VmToolInstance instance = CreateFlowTool(entry.Kind, CreateUniqueToolCopyName(entry.InstanceName), entry.IsEnabled, null);
                instance.Parameters = (entry.Parameters ?? new VmToolParameterData()).Clone();
                instance.Parameters.Normalize();
                instance.ReplaceRoiBindings((entry.RoiIds ?? new List<string>()).Where(id => roiLayers.Any(layer => string.Equals(layer.RoiId, id, StringComparison.OrdinalIgnoreCase))));
                instance.NumericOperator = NumericJudgeOperatorOption.IsSupported(entry.NumericOperator)
                    ? entry.NumericOperator
                    : NumericJudgeOperatorOption.BetweenInclusive;
                instance.NumericLowerLimit = entry.NumericLowerLimit;
                instance.NumericUpperLimit = entry.NumericUpperLimit;
                instance.NumericTolerance = Math.Max(0, entry.NumericTolerance);
                if (isManualFlowLayout && entry.HasManualGraphPosition)
                {
                    instance.GraphX = SnapFlowCoordinate(entry.GraphX + 48);
                    instance.GraphY = SnapFlowCoordinate(entry.GraphY + 48);
                    instance.HasManualGraphPosition = true;
                }
                if (!string.IsNullOrWhiteSpace(entry.GroupId))
                {
                    string newGroupId;
                    if (!groupIdMap.TryGetValue(entry.GroupId, out newGroupId))
                    {
                        newGroupId = Guid.NewGuid().ToString("N");
                        groupIdMap.Add(entry.GroupId, newGroupId);
                        groupNameMap.Add(entry.GroupId, CreateUniqueFlowGroupName((entry.GroupName ?? "流程组合") + "_副本"));
                    }
                    instance.GroupId = newGroupId;
                    instance.GroupName = groupNameMap[entry.GroupId];
                    instance.IsGroupCollapsed = entry.IsGroupCollapsed;
                }
                flowTools.Insert(targetIndex + pasted.Count, instance);
                idMap[entry.SourceToolId] = instance.ToolId;
                pasted.Add(instance);
            }

            int retainedBindings = 0;
            int clearedBindings = 0;
            for (int index = 0; index < pasted.Count; index++)
            {
                VmToolClipboardItem entry = orderedEntries[index];
                VmToolInstance target = pasted[index];
                foreach (VmToolInputBindingData sourceBinding in entry.InputBindings ?? new List<VmToolInputBindingData>())
                {
                    string mappedSourceId;
                    string sourceToolId = idMap.TryGetValue(sourceBinding.SourceToolId ?? string.Empty, out mappedSourceId)
                        ? mappedSourceId
                        : sourceBinding.SourceToolId;
                    VmToolInputBindingData candidate = new VmToolInputBindingData
                    {
                        TargetPortName = sourceBinding.TargetPortName,
                        SourceToolId = sourceToolId,
                        SourcePortName = sourceBinding.SourcePortName
                    };
                    if (string.IsNullOrWhiteSpace(GetInputBindingConfigurationError(target, candidate)))
                    {
                        target.SetInputBinding(candidate.TargetPortName, candidate.SourceToolId, candidate.SourcePortName);
                        retainedBindings++;
                    }
                    else
                    {
                        clearedBindings++;
                    }
                }

                if (target.Kind == VmToolKind.NumericJudge && !string.IsNullOrWhiteSpace(entry.InputToolId))
                {
                    string mappedNumericSourceId;
                    target.InputToolId = idMap.TryGetValue(entry.InputToolId, out mappedNumericSourceId) ? mappedNumericSourceId : entry.InputToolId;
                    target.InputPortName = entry.InputPortName;
                    if (!string.IsNullOrWhiteSpace(GetNumericJudgeConfigurationError(target)))
                    {
                        target.InputToolId = null;
                        target.InputPortName = null;
                        clearedBindings++;
                    }
                    else
                    {
                        retainedBindings++;
                    }
                }

                InvalidateToolRunResult(target, "由流程剪贴板创建，等待运行");
            }

            NormalizeFlowGroups();

            ApplyFlowToolSelection(pasted, pasted[pasted.Count - 1], false);
            FlowToolList.ScrollIntoView(pasted[pasted.Count - 1]);
            flowPasteInsertionIndex = targetIndex + pasted.Count;
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("粘贴 " + pasted.Count.ToString(CultureInfo.InvariantCulture) + " 个模块", before);
            string bindingText = retainedBindings.ToString(CultureInfo.InvariantCulture) + " 条连接保留" +
                                 (clearedBindings == 0 ? string.Empty : "，" + clearedBindings.ToString(CultureInfo.InvariantCulture) + " 条无效连接已清除");
            HeaderStatusText.Text = "已粘贴 " + pasted.Count.ToString(CultureInfo.InvariantCulture) + " 个模块（" + bindingText + "）；新模块等待运行。";
            LogInfo(HeaderStatusText.Text);
            return pasted.Count;
        }

        private void RefreshFlowClipboardStatus()
        {
            if (FlowClipboardStatusText == null)
            {
                return;
            }

            FlowClipboardStatusText.Text = flowToolClipboard.Count == 0
                ? "剪贴板：空 · 空白框选 / Ctrl 增减 / 中键平移"
                : "剪贴板：" + flowToolClipboard.Count.ToString(CultureInfo.InvariantCulture) + " 个模块 · 参数/ROI/合法连接 · 不含运行结果";
        }

        private void CopyFlowToolButton_Click(object sender, RoutedEventArgs e)
        {
            CopySelectedFlowToolsToClipboard();
            RefreshUiState();
        }

        private void PasteFlowToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能粘贴模块。";
                return;
            }
            PasteFlowToolClipboard(ResolveFlowPasteInsertionIndex());
        }

        private void DuplicateFlowToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能创建模块副本。";
                return;
            }
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (selected.Count == 0)
            {
                HeaderStatusText.Text = "请先选择要创建副本的流程模块。";
                return;
            }

            int insertionIndex = selected.Max(item => flowTools.IndexOf(item)) + 1;
            if (CopySelectedFlowToolsToClipboard() > 0)
            {
                PasteFlowToolClipboard(insertionIndex);
            }
        }

        private void BatchEnableFlowToolsButton_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedFlowToolsEnabled(true);
        }

        private void BatchDisableFlowToolsButton_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedFlowToolsEnabled(false);
        }

        private void SetSelectedFlowToolsEnabled(bool enabled)
        {
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能批量启停模块。";
                return;
            }
            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (selected.Count == 0)
            {
                HeaderStatusText.Text = "请先选择要批量启停的流程模块。";
                return;
            }
            if (selected.Contains(dockEditingTool) && !ConfirmDockDraftChanges(enabled ? "批量启用模块" : "批量停用模块"))
            {
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            int changed = 0;
            foreach (VmToolInstance tool in selected)
            {
                if (tool.IsEnabled == enabled) continue;
                tool.IsEnabled = enabled;
                InvalidateToolRunResult(tool, enabled ? "工具已批量启用" : "工具已批量停用");
                InvalidateDownstreamResults(tool, "上游工具批量启停已变化");
                changed++;
            }
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory((enabled ? "批量启用模块" : "批量停用模块"), before);
            RefreshUiState();
            HeaderStatusText.Text = (enabled ? "已批量启用 " : "已批量停用 ") + changed.ToString(CultureInfo.InvariantCulture) + " 个模块（选择共 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个）。";
            LogInfo(HeaderStatusText.Text);
        }

        private void FlowToolList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flowSelectionUpdating)
            {
                RefreshFlowSelectionFlags();
                return;
            }
            if (dockSelectionReverting)
            {
                RefreshInspector();
                return;
            }

            VmToolInstance selectedTool = FlowToolList.SelectedItem as VmToolInstance;
            if (!string.IsNullOrWhiteSpace(selectedFlowPortToolId) &&
                (selectedTool == null || !string.Equals(selectedTool.ToolId, selectedFlowPortToolId, StringComparison.OrdinalIgnoreCase)))
            {
                selectedFlowPortToolId = null;
                selectedFlowPortName = null;
                selectedFlowPortDirection = null;
            }
            if (dockDraftDirty && dockEditingTool != null && !ReferenceEquals(dockEditingTool, selectedTool))
            {
                MessageBoxResult choice = System.Windows.MessageBox.Show(
                    this,
                    "“" + dockEditingTool.InstanceName + "”存在尚未应用的模块配置草稿。\n\n是：应用后切换\n否：放弃草稿并切换\n取消：留在当前模块",
                    "切换模块",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                if (choice == MessageBoxResult.Yes)
                {
                    if (!ApplyDockConfigurationDraft(false))
                    {
                        RevertFlowSelectionToDockTool();
                        return;
                    }
                }
                else if (choice == MessageBoxResult.No)
                {
                    dockDraftDirty = false;
                }
                else
                {
                    RevertFlowSelectionToDockTool();
                    return;
                }
            }

            if (!ReferenceEquals(dockEditingTool, selectedTool))
            {
                dockEditingTool = null;
            }
            if (!imageContextManuallySelected && ImageContextComboBox != null)
            {
                VmToolInstance selected = selectedTool;
                string automaticContext = selected == null
                    ? VmImageContextOption.GlobalInput
                    : (selected.Kind == VmToolKind.ImageSource
                        ? VmImageContextOption.ModuleOutput
                        : (selected.Kind == VmToolKind.RegionMorphology || selected.Kind == VmToolKind.RegionFeatureFilter || selected.Kind == VmToolKind.RegionSetOperation || selected.Kind == VmToolKind.RegionContourMeasurement || selected.Kind == VmToolKind.XldContourFilter || selected.Kind == VmToolKind.XldContourSegmentation || selected.Kind == VmToolKind.XldLineFitting || selected.Kind == VmToolKind.XldCircleFitting || selected.Kind == VmToolKind.XldEllipseFitting || selected.Kind == VmToolKind.XldRectangleFitting || ToolMetadata.GetInputPorts(selected.Kind).Any(item => string.Equals(item.DataType, "Image", StringComparison.OrdinalIgnoreCase))
                            ? VmImageContextOption.ModuleInput
                            : VmImageContextOption.GlobalInput));
                SetImageContextSelection(automaticContext);
                UpdateImageContextViewport(true);
                ScheduleRefreshDisplay();
            }
            RefreshInspector();
            RefreshFlowPortVisualization();
            RefreshFlowSelectionFlags();
        }

        private void RevertFlowSelectionToDockTool()
        {
            dockSelectionReverting = true;
            try
            {
                FlowToolList.SelectedItem = dockEditingTool;
                if (dockEditingTool != null)
                {
                    FlowToolList.ScrollIntoView(dockEditingTool);
                }
            }
            finally
            {
                dockSelectionReverting = false;
            }
        }

        private void FlowToolList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenSelectedToolConfiguration();
        }

        private void ConfigureToolButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedToolConfiguration();
        }

        private void OpenSelectedToolConfiguration()
        {
            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected == null)
            {
                HeaderStatusText.Text = "请先选择流程工具。";
                return;
            }

            if (SupportsDockConfiguration(selected.Kind))
            {
                RightTabs.SelectedItem = DockConfigurationTab;
                if (!ReferenceEquals(dockEditingTool, selected))
                {
                    LoadDockConfigurationDraft(selected);
                }
                DockConfigurationTabs.SelectedIndex = 0;
                HeaderStatusText.Text = selected.InstanceName + " 已定位到右侧模块配置工作台。";
                return;
            }

            ToolConfigurationWindow window = new ToolConfigurationWindow(
                selected,
                delegate(VmToolInstance tool) { RunStandaloneTool(tool, "模块配置试运行"); },
                delegate(string name) { return GetToolInstanceNameError(selected, name); },
                roiLayers,
                flowTools)
            {
                Owner = this
            };
            window.ShowDialog();
            if (window.WasApplied)
            {
                LogInfo("模块配置已应用：" + selected.InstanceName);
                InvalidateDownstreamResults(selected, "上游配置已修改");
            }

            if (window.OpenRoiWorkspaceRequested)
            {
                RightTabs.SelectedItem = RoiTab;
                RoiLayerPanel.Visibility = Visibility.Visible;
                RoiLayerPanelColumn.Width = new GridLength(220);
                ToggleRoiLayerPanelButton.Content = "隐藏图层";
                if (window.RequestedRoiTool.HasValue)
                {
                    VisionTool requestedTool = window.RequestedRoiTool.Value;
                    string hint = requestedTool == VisionTool.RectangleRoi
                        ? "矩形 ROI：按住左键拖拽绘制，松开后点击确认。"
                        : (requestedTool == VisionTool.CircleRoi
                            ? "圆形 ROI：从圆心按住左键向外拖拽，松开后点击确认。"
                            : "多边形 ROI：左键加点，双击或点击确认 ROI 结束。");
                    SetRoiTool(requestedTool, hint);
                }
            }

            if (window.OpenIoWorkspaceRequested)
            {
                RightTabs.SelectedItem = IoTab;
            }

            RefreshUiState();
        }

        private static bool SupportsDockConfiguration(VmToolKind kind)
        {
            return kind == VmToolKind.ImageSource ||
                   kind == VmToolKind.ImageChannel ||
                   kind == VmToolKind.ImageFilter ||
                   kind == VmToolKind.ImageEnhancement ||
                   kind == VmToolKind.ImageDifference ||
                   kind == VmToolKind.DynamicThreshold ||
                   kind == VmToolKind.ImageThreshold ||
                   kind == VmToolKind.RegionMorphology ||
                   kind == VmToolKind.RegionFeatureFilter ||
                   kind == VmToolKind.RegionSetOperation ||
                   kind == VmToolKind.RegionContourMeasurement ||
                   kind == VmToolKind.XldContourFilter ||
                   kind == VmToolKind.XldContourSegmentation ||
                   kind == VmToolKind.XldLineFitting ||
                   kind == VmToolKind.XldCircleFitting ||
                   kind == VmToolKind.XldEllipseFitting ||
                   kind == VmToolKind.XldRectangleFitting ||
                   kind == VmToolKind.Blob ||
                   kind == VmToolKind.GrayStat ||
                   kind == VmToolKind.EdgeMeasure ||
                   kind == VmToolKind.NumericJudge;
        }

        private void LoadDockConfigurationDraft(VmToolInstance tool)
        {
            if (DockModuleTitleText == null)
            {
                return;
            }

            dockDraftUpdating = true;
            try
            {
                dockEditingTool = tool;
                dockInputPortRows.Clear();
                dockRoiBindingRows.Clear();
                bool hasTool = tool != null;
                DockModuleTitleText.Text = hasTool ? tool.SequenceText + "  " + tool.InstanceName : "请选择流程模块";
                DockModuleTypeText.Text = hasTool ? tool.Category + " / " + tool.DisplayType : "右侧常驻配置，不打开主要参数弹窗";
                DockEmptyText.Visibility = hasTool ? Visibility.Collapsed : Visibility.Visible;
                DockApplyButton.IsEnabled = hasTool && SupportsDockConfiguration(tool.Kind);
                DockRevertButton.IsEnabled = hasTool && SupportsDockConfiguration(tool.Kind);
                DockTrialRunButton.IsEnabled = hasTool && tool.IsEnabled;
                DockInstanceNameTextBox.IsEnabled = hasTool && SupportsDockConfiguration(tool.Kind);
                DockEnabledCheckBox.IsEnabled = hasTool && SupportsDockConfiguration(tool.Kind);
                DockValidationText.Text = string.Empty;
                SetDockPanelVisibility(tool);

                if (!hasTool)
                {
                    DockInstanceNameTextBox.Text = string.Empty;
                    DockEnabledCheckBox.IsChecked = false;
                    DockRunStateText.Text = "状态：--";
                    DockResultText.Text = "最近结果：--";
                    DockErrorText.Text = "错误：--";
                    DockRoiResultsDataGrid.ItemsSource = null;
                    DockHelpText.Text = "选择模块后显示帮助。";
                    DockFooterText.Text = "选择模块开始配置";
                    return;
                }

                VmToolParameterData p = (tool.Parameters ?? new VmToolParameterData()).Clone();
                DockInstanceNameTextBox.Text = tool.InstanceName;
                DockEnabledCheckBox.IsChecked = tool.IsEnabled;
                DockLocalImagePathTextBox.Text = p.LocalImagePath ?? string.Empty;
                DockLocalImageSerialTextBox.Text = p.LocalImageSerialNumber ?? string.Empty;
                DockImageChannelModeComboBox.SelectedValue = VmImageChannelMode.Normalize(p.ImageChannelMode);
                DockImageChannelIndexTextBox.Text = p.ImageChannelIndex.ToString(CultureInfo.InvariantCulture);
                DockImageFilterModeComboBox.SelectedValue = VmImageFilterMode.Normalize(p.ImageFilterMode);
                DockImageFilterWidthTextBox.Text = p.ImageFilterMaskWidth.ToString(CultureInfo.InvariantCulture);
                DockImageFilterHeightTextBox.Text = p.ImageFilterMaskHeight.ToString(CultureInfo.InvariantCulture);
                DockImageFilterRadiusTextBox.Text = p.ImageFilterRadius.ToString(CultureInfo.InvariantCulture);
                DockImageEnhancementModeComboBox.SelectedValue = VmImageEnhancementMode.Normalize(p.ImageEnhancementMode);
                DockImageEnhancementScaleTextBox.Text = p.ImageEnhancementScale.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageEnhancementOffsetTextBox.Text = p.ImageEnhancementOffset.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageEnhancementMaskWidthTextBox.Text = p.ImageEnhancementMaskWidth.ToString(CultureInfo.InvariantCulture);
                DockImageEnhancementMaskHeightTextBox.Text = p.ImageEnhancementMaskHeight.ToString(CultureInfo.InvariantCulture);
                DockImageEnhancementFactorTextBox.Text = p.ImageEnhancementFactor.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageDifferenceModeComboBox.SelectedValue = VmImageDifferenceMode.Normalize(p.ImageDifferenceMode);
                DockImageDifferenceMultiplierTextBox.Text = p.ImageDifferenceMultiplier.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageDifferenceAddTextBox.Text = p.ImageDifferenceAdd.ToString("0.###", CultureInfo.InvariantCulture);
                DockDynamicThresholdPolarityComboBox.SelectedValue = VmDynamicThresholdPolarity.Normalize(p.DynamicThresholdPolarity);
                DockDynamicThresholdOffsetTextBox.Text = p.DynamicThresholdOffset.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageThresholdMinTextBox.Text = p.ImageThresholdMinGray.ToString("0.###", CultureInfo.InvariantCulture);
                DockImageThresholdMaxTextBox.Text = p.ImageThresholdMaxGray.ToString("0.###", CultureInfo.InvariantCulture);
                DockRegionMorphologyModeComboBox.SelectedValue = VmRegionMorphologyMode.Normalize(p.RegionMorphologyMode);
                DockRegionMorphologyRadiusTextBox.Text = p.RegionMorphologyRadius.ToString("0.###", CultureInfo.InvariantCulture);
                DockRegionFeatureComboBox.SelectedValue = VmRegionFeature.Normalize(p.RegionFeature);
                DockRegionFeatureMinTextBox.Text = p.RegionFeatureMin.ToString("0.###", CultureInfo.InvariantCulture);
                DockRegionFeatureMaxTextBox.Text = p.RegionFeatureMax.ToString("0.###", CultureInfo.InvariantCulture);
                DockRegionSetOperationComboBox.SelectedValue = VmRegionSetOperationMode.Normalize(p.RegionSetOperationMode);
                DockRegionContourModeComboBox.SelectedValue = VmRegionContourMode.Normalize(p.RegionContourMode);
                DockXldContourFeatureComboBox.SelectedValue = VmXldContourFeature.Normalize(p.XldContourFeature);
                DockXldContourMinTextBox.Text = p.XldContourMin.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldContourMaxTextBox.Text = p.XldContourMax.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldSegmentModeComboBox.SelectedValue = VmXldSegmentMode.Normalize(p.XldSegmentMode);
                DockXldSegmentSmoothTextBox.Text = p.XldSegmentSmooth.ToString(CultureInfo.InvariantCulture);
                DockXldSegmentDistance1TextBox.Text = p.XldSegmentMaxLineDistance1.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldSegmentDistance2TextBox.Text = p.XldSegmentMaxLineDistance2.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldLineFitAlgorithmComboBox.SelectedValue = VmXldLineFitAlgorithm.Normalize(p.XldLineFitAlgorithm);
                DockXldLineFitMaxPointsTextBox.Text = p.XldLineFitMaxNumPoints.ToString(CultureInfo.InvariantCulture);
                DockXldLineFitClipEndpointsTextBox.Text = p.XldLineFitClippingEndPoints.ToString(CultureInfo.InvariantCulture);
                DockXldLineFitIterationsTextBox.Text = p.XldLineFitIterations.ToString(CultureInfo.InvariantCulture);
                DockXldLineFitClippingFactorTextBox.Text = p.XldLineFitClippingFactor.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldCircleFitAlgorithmComboBox.SelectedValue = VmXldCircleFitAlgorithm.Normalize(p.XldCircleFitAlgorithm);
                DockXldCircleFitMaxPointsTextBox.Text = p.XldCircleFitMaxNumPoints.ToString(CultureInfo.InvariantCulture);
                DockXldCircleFitMaxClosureTextBox.Text = p.XldCircleFitMaxClosureDistance.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldCircleFitClipEndpointsTextBox.Text = p.XldCircleFitClippingEndPoints.ToString(CultureInfo.InvariantCulture);
                DockXldCircleFitIterationsTextBox.Text = p.XldCircleFitIterations.ToString(CultureInfo.InvariantCulture);
                DockXldCircleFitClippingFactorTextBox.Text = p.XldCircleFitClippingFactor.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldEllipseFitAlgorithmComboBox.SelectedValue = VmXldEllipseFitAlgorithm.Normalize(p.XldEllipseFitAlgorithm);
                DockXldEllipseFitMaxPointsTextBox.Text = p.XldEllipseFitMaxNumPoints.ToString(CultureInfo.InvariantCulture);
                DockXldEllipseFitMaxClosureTextBox.Text = p.XldEllipseFitMaxClosureDistance.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldEllipseFitClipEndpointsTextBox.Text = p.XldEllipseFitClippingEndPoints.ToString(CultureInfo.InvariantCulture);
                DockXldEllipseFitVossTableTextBox.Text = p.XldEllipseFitVossTabSize.ToString(CultureInfo.InvariantCulture);
                DockXldEllipseFitIterationsTextBox.Text = p.XldEllipseFitIterations.ToString(CultureInfo.InvariantCulture);
                DockXldEllipseFitClippingFactorTextBox.Text = p.XldEllipseFitClippingFactor.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldRectangleFitAlgorithmComboBox.SelectedValue = VmXldRectangleFitAlgorithm.Normalize(p.XldRectangleFitAlgorithm);
                DockXldRectangleFitMaxPointsTextBox.Text = p.XldRectangleFitMaxNumPoints.ToString(CultureInfo.InvariantCulture);
                DockXldRectangleFitMaxClosureTextBox.Text = p.XldRectangleFitMaxClosureDistance.ToString("0.###", CultureInfo.InvariantCulture);
                DockXldRectangleFitClipEndpointsTextBox.Text = p.XldRectangleFitClippingEndPoints.ToString(CultureInfo.InvariantCulture);
                DockXldRectangleFitIterationsTextBox.Text = p.XldRectangleFitIterations.ToString(CultureInfo.InvariantCulture);
                DockXldRectangleFitClippingFactorTextBox.Text = p.XldRectangleFitClippingFactor.ToString("0.###", CultureInfo.InvariantCulture);
                UpdateDockImageFilterParameterState();
                UpdateDockImageEnhancementParameterState();
                UpdateDockImageDifferenceParameterState();
                DockBlobMinTextBox.Text = p.BlobMinGray.ToString("0.###", CultureInfo.InvariantCulture);
                DockBlobMaxTextBox.Text = p.BlobMaxGray.ToString("0.###", CultureInfo.InvariantCulture);
                DockBlobAreaTextBox.Text = p.BlobMinArea.ToString("0.###", CultureInfo.InvariantCulture);
                DockGrayMinTextBox.Text = p.GrayMin.ToString("0.###", CultureInfo.InvariantCulture);
                DockGrayMaxTextBox.Text = p.GrayMax.ToString("0.###", CultureInfo.InvariantCulture);
                DockEdgeThresholdTextBox.Text = p.EdgeThreshold.ToString("0.###", CultureInfo.InvariantCulture);
                DockNumericOperatorComboBox.SelectedValue = tool.NumericOperator;
                DockNumericLowerTextBox.Text = tool.NumericLowerLimit.ToString("0.###", CultureInfo.InvariantCulture);
                DockNumericUpperTextBox.Text = tool.NumericUpperLimit.ToString("0.###", CultureInfo.InvariantCulture);
                DockNumericToleranceTextBox.Text = tool.NumericTolerance.ToString("0.###", CultureInfo.InvariantCulture);
                BuildDockInputPortRows(tool);
                BuildDockRoiBindingRows(tool, p);
                DockHelpText.Text = ToolMetadata.GetDescription(tool.Kind) + (SupportsDockConfiguration(tool.Kind)
                    ? "\n\n本页使用实例草稿；应用后进入配方，试运行完成后恢复已应用配置。"
                    : "\n\n该模块当前保留兼容配置入口，尚未迁移完整模型或程序页面。");
                RefreshDockConfigurationRuntime(tool);
                DockFooterText.Text = SupportsDockConfiguration(tool.Kind) ? "实例参数草稿已与当前配置同步" : "兼容模块：使用兼容配置入口";
            }
            finally
            {
                dockDraftDirty = false;
                dockDraftUpdating = false;
                DockDraftStateText.Text = tool == null ? "无草稿" : (SupportsDockConfiguration(tool.Kind) ? "已应用" : "兼容");
            }
        }

        private void SetDockPanelVisibility(VmToolInstance tool)
        {
            VmToolKind? kind = tool == null ? (VmToolKind?)null : tool.Kind;
            DockImageSourcePanel.Visibility = kind == VmToolKind.ImageSource ? Visibility.Visible : Visibility.Collapsed;
            DockImageChannelPanel.Visibility = kind == VmToolKind.ImageChannel ? Visibility.Visible : Visibility.Collapsed;
            DockImageFilterPanel.Visibility = kind == VmToolKind.ImageFilter ? Visibility.Visible : Visibility.Collapsed;
            DockImageEnhancementPanel.Visibility = kind == VmToolKind.ImageEnhancement ? Visibility.Visible : Visibility.Collapsed;
            DockImageDifferencePanel.Visibility = kind == VmToolKind.ImageDifference ? Visibility.Visible : Visibility.Collapsed;
            DockDynamicThresholdPanel.Visibility = kind == VmToolKind.DynamicThreshold ? Visibility.Visible : Visibility.Collapsed;
            DockImageThresholdPanel.Visibility = kind == VmToolKind.ImageThreshold ? Visibility.Visible : Visibility.Collapsed;
            DockRegionMorphologyPanel.Visibility = kind == VmToolKind.RegionMorphology ? Visibility.Visible : Visibility.Collapsed;
            DockRegionFeaturePanel.Visibility = kind == VmToolKind.RegionFeatureFilter ? Visibility.Visible : Visibility.Collapsed;
            DockRegionSetOperationPanel.Visibility = kind == VmToolKind.RegionSetOperation ? Visibility.Visible : Visibility.Collapsed;
            DockRegionContourPanel.Visibility = kind == VmToolKind.RegionContourMeasurement ? Visibility.Visible : Visibility.Collapsed;
            DockXldContourFilterPanel.Visibility = kind == VmToolKind.XldContourFilter ? Visibility.Visible : Visibility.Collapsed;
            DockXldContourSegmentationPanel.Visibility = kind == VmToolKind.XldContourSegmentation ? Visibility.Visible : Visibility.Collapsed;
            DockXldLineFittingPanel.Visibility = kind == VmToolKind.XldLineFitting ? Visibility.Visible : Visibility.Collapsed;
            DockXldCircleFittingPanel.Visibility = kind == VmToolKind.XldCircleFitting ? Visibility.Visible : Visibility.Collapsed;
            DockXldEllipseFittingPanel.Visibility = kind == VmToolKind.XldEllipseFitting ? Visibility.Visible : Visibility.Collapsed;
            DockXldRectangleFittingPanel.Visibility = kind == VmToolKind.XldRectangleFitting ? Visibility.Visible : Visibility.Collapsed;
            DockBlobPanel.Visibility = kind == VmToolKind.Blob ? Visibility.Visible : Visibility.Collapsed;
            DockGrayPanel.Visibility = kind == VmToolKind.GrayStat ? Visibility.Visible : Visibility.Collapsed;
            DockEdgePanel.Visibility = kind == VmToolKind.EdgeMeasure ? Visibility.Visible : Visibility.Collapsed;
            DockNumericPanel.Visibility = kind == VmToolKind.NumericJudge ? Visibility.Visible : Visibility.Collapsed;
            DockCompatibilityPanel.Visibility = kind == VmToolKind.ShapeMatch || kind == VmToolKind.HDevelop ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshDockConfigurationRuntime(VmToolInstance tool)
        {
            if (tool == null || DockRunStateText == null)
            {
                return;
            }

            DockModuleTitleText.Text = tool.SequenceText + "  " + tool.InstanceName;
            DockRunStateText.Text = "状态：" + tool.ConfigurationStatus + " · " + tool.RunStatus + " · " + tool.ResultCode + " · " + tool.ElapsedText;
            DockResultText.Text = "最近结果：" + tool.ResultCode + " · " + (string.IsNullOrWhiteSpace(tool.OutputSummary) ? "--" : tool.OutputSummary);
            DockErrorText.Text = "错误：" + (string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "无" : tool.ErrorMessage);
            DockRoiResultsDataGrid.ItemsSource = tool.RoiResults;
            foreach (VmInputPortEditorRow row in dockInputPortRows)
            {
                RefreshDockInputRowState(row);
            }
            DockInputPortItemsControl.Items.Refresh();
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
        }

        private void DockDraftValue_Changed(object sender, RoutedEventArgs e)
        {
            if (dockDraftUpdating || dockEditingTool == null || !SupportsDockConfiguration(dockEditingTool.Kind))
            {
                return;
            }

            dockDraftDirty = true;
            DockDraftStateText.Text = "未应用";
            DockFooterText.Text = "草稿已修改；试运行不会自动应用";
            DockValidationText.Text = string.Empty;
            if (ReferenceEquals(sender, DockRoiExecutionModeComboBox))
            {
                UpdateDockRoiSummary();
            }
            if (ReferenceEquals(sender, DockImageFilterModeComboBox))
            {
                UpdateDockImageFilterParameterState();
            }
            if (ReferenceEquals(sender, DockImageEnhancementModeComboBox))
            {
                UpdateDockImageEnhancementParameterState();
            }
            if (ReferenceEquals(sender, DockImageDifferenceModeComboBox))
            {
                UpdateDockImageDifferenceParameterState();
            }
        }

        private void UpdateDockImageFilterParameterState()
        {
            if (DockImageFilterModeComboBox == null)
            {
                return;
            }

            bool mean = VmImageFilterMode.Normalize(DockImageFilterModeComboBox.SelectedValue as string) == VmImageFilterMode.Mean;
            DockImageFilterWidthTextBox.IsEnabled = mean;
            DockImageFilterHeightTextBox.IsEnabled = mean;
            DockImageFilterRadiusTextBox.IsEnabled = !mean;
        }

        private void UpdateDockImageEnhancementParameterState()
        {
            if (DockImageEnhancementModeComboBox == null) return;
            bool linear = VmImageEnhancementMode.Normalize(DockImageEnhancementModeComboBox.SelectedValue as string) == VmImageEnhancementMode.LinearScale;
            DockImageEnhancementScaleTextBox.IsEnabled = linear;
            DockImageEnhancementOffsetTextBox.IsEnabled = linear;
            DockImageEnhancementMaskWidthTextBox.IsEnabled = !linear;
            DockImageEnhancementMaskHeightTextBox.IsEnabled = !linear;
            DockImageEnhancementFactorTextBox.IsEnabled = !linear;
        }

        private void UpdateDockImageDifferenceParameterState()
        {
            if (DockImageDifferenceModeComboBox == null) return;
            bool directional = VmImageDifferenceMode.Normalize(DockImageDifferenceModeComboBox.SelectedValue as string) != VmImageDifferenceMode.AbsoluteDifference;
            DockImageDifferenceAddTextBox.IsEnabled = directional;
        }

        private bool TryReadDockConfigurationDraft(
            out VmToolParameterData parameters,
            out string instanceName,
            out bool enabled,
            out string numericOperator,
            out double numericLower,
            out double numericUpper,
            out double numericTolerance)
        {
            parameters = dockEditingTool == null ? new VmToolParameterData() : (dockEditingTool.Parameters ?? new VmToolParameterData()).Clone();
            instanceName = string.Empty;
            enabled = false;
            numericOperator = NumericJudgeOperatorOption.BetweenInclusive;
            numericLower = 0;
            numericUpper = 100;
            numericTolerance = 0.001;
            VmToolInstance tool = dockEditingTool;
            if (tool == null)
            {
                return FailDockDraft("请先选择流程模块。");
            }

            instanceName = DockInstanceNameTextBox.Text == null ? string.Empty : DockInstanceNameTextBox.Text.Trim();
            string nameError = GetToolInstanceNameError(tool, instanceName);
            if (!string.IsNullOrWhiteSpace(nameError))
            {
                return FailDockDraft(nameError);
            }

            enabled = DockEnabledCheckBox.IsChecked == true;
            double number;
            int integer;
            if (tool.Kind == VmToolKind.ImageSource)
            {
                parameters.LocalImagePath = DockLocalImagePathTextBox.Text == null ? string.Empty : DockLocalImagePathTextBox.Text.Trim();
                parameters.LocalImageSerialNumber = DockLocalImageSerialTextBox.Text == null ? string.Empty : DockLocalImageSerialTextBox.Text.Trim();
            }
            else if (tool.Kind == VmToolKind.ImageChannel)
            {
                parameters.ImageChannelMode = VmImageChannelMode.Normalize(DockImageChannelModeComboBox.SelectedValue as string);
                if (!int.TryParse(DockImageChannelIndexTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
                {
                    return FailDockDraft("图像通道序号必须是整数。");
                }
                parameters.ImageChannelIndex = integer;
            }
            else if (tool.Kind == VmToolKind.ImageFilter)
            {
                parameters.ImageFilterMode = VmImageFilterMode.Normalize(DockImageFilterModeComboBox.SelectedValue as string);
                if (!int.TryParse(DockImageFilterWidthTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
                {
                    return FailDockDraft("均值滤波模板宽必须是整数。");
                }
                parameters.ImageFilterMaskWidth = integer;
                if (!int.TryParse(DockImageFilterHeightTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
                {
                    return FailDockDraft("均值滤波模板高必须是整数。");
                }
                parameters.ImageFilterMaskHeight = integer;
                if (!int.TryParse(DockImageFilterRadiusTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
                {
                    return FailDockDraft("中值滤波半径必须是整数。");
                }
                parameters.ImageFilterRadius = integer;
            }
            else if (tool.Kind == VmToolKind.ImageEnhancement)
            {
                parameters.ImageEnhancementMode = VmImageEnhancementMode.Normalize(DockImageEnhancementModeComboBox.SelectedValue as string);
                if (!TryParseUiDouble(DockImageEnhancementScaleTextBox.Text, out number)) return FailDockDraft("图像增强灰度增益必须是数值。");
                parameters.ImageEnhancementScale = number;
                if (!TryParseUiDouble(DockImageEnhancementOffsetTextBox.Text, out number)) return FailDockDraft("图像增强灰度偏移必须是数值。");
                parameters.ImageEnhancementOffset = number;
                if (!int.TryParse(DockImageEnhancementMaskWidthTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("局部增强模板宽必须是整数。");
                parameters.ImageEnhancementMaskWidth = integer;
                if (!int.TryParse(DockImageEnhancementMaskHeightTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("局部增强模板高必须是整数。");
                parameters.ImageEnhancementMaskHeight = integer;
                if (!TryParseUiDouble(DockImageEnhancementFactorTextBox.Text, out number)) return FailDockDraft("局部增强因子必须是数值。");
                parameters.ImageEnhancementFactor = number;
            }
            else if (tool.Kind == VmToolKind.ImageDifference)
            {
                parameters.ImageDifferenceMode = VmImageDifferenceMode.Normalize(DockImageDifferenceModeComboBox.SelectedValue as string);
                if (!TryParseUiDouble(DockImageDifferenceMultiplierTextBox.Text, out number)) return FailDockDraft("双图像差分灰度增益必须是数值。");
                parameters.ImageDifferenceMultiplier = number;
                if (!TryParseUiDouble(DockImageDifferenceAddTextBox.Text, out number)) return FailDockDraft("双图像差分灰度偏移必须是数值。");
                parameters.ImageDifferenceAdd = number;
            }
            else if (tool.Kind == VmToolKind.DynamicThreshold)
            {
                parameters.DynamicThresholdPolarity = VmDynamicThresholdPolarity.Normalize(DockDynamicThresholdPolarityComboBox.SelectedValue as string);
                if (!TryParseUiDouble(DockDynamicThresholdOffsetTextBox.Text, out number)) return FailDockDraft("动态阈值灰度偏移必须是数值。");
                parameters.DynamicThresholdOffset = number;
            }
            else if (tool.Kind == VmToolKind.ImageThreshold)
            {
                if (!TryParseUiDouble(DockImageThresholdMinTextBox.Text, out number)) return FailDockDraft("阈值分割灰度下限必须是数值。");
                parameters.ImageThresholdMinGray = number;
                if (!TryParseUiDouble(DockImageThresholdMaxTextBox.Text, out number)) return FailDockDraft("阈值分割灰度上限必须是数值。");
                parameters.ImageThresholdMaxGray = number;
            }
            else if (tool.Kind == VmToolKind.RegionMorphology)
            {
                parameters.RegionMorphologyMode = VmRegionMorphologyMode.Normalize(DockRegionMorphologyModeComboBox.SelectedValue as string);
                if (!TryParseUiDouble(DockRegionMorphologyRadiusTextBox.Text, out number)) return FailDockDraft("Region 形态学圆形半径必须是数值。");
                parameters.RegionMorphologyRadius = number;
            }
            else if (tool.Kind == VmToolKind.RegionFeatureFilter)
            {
                parameters.RegionFeature = VmRegionFeature.Normalize(DockRegionFeatureComboBox.SelectedValue as string);
                if (!TryParseUiDouble(DockRegionFeatureMinTextBox.Text, out number)) return FailDockDraft("区域特征下限必须是数值。");
                parameters.RegionFeatureMin = number;
                if (!TryParseUiDouble(DockRegionFeatureMaxTextBox.Text, out number)) return FailDockDraft("区域特征上限必须是数值。");
                parameters.RegionFeatureMax = number;
            }
            else if (tool.Kind == VmToolKind.RegionSetOperation)
            {
                parameters.RegionSetOperationMode = VmRegionSetOperationMode.Normalize(DockRegionSetOperationComboBox.SelectedValue as string);
            }
            else if (tool.Kind == VmToolKind.RegionContourMeasurement)
            {
                parameters.RegionContourMode = VmRegionContourMode.Normalize(DockRegionContourModeComboBox.SelectedValue as string);
            }
            else if (tool.Kind == VmToolKind.XldContourFilter)
            {
                parameters.XldContourFeature = VmXldContourFeature.Normalize(DockXldContourFeatureComboBox.SelectedValue as string);
                if (!TryParseUiDouble(DockXldContourMinTextBox.Text, out number)) return FailDockDraft("XLD 轮廓筛选下限必须是数值。");
                parameters.XldContourMin = number;
                if (!TryParseUiDouble(DockXldContourMaxTextBox.Text, out number)) return FailDockDraft("XLD 轮廓筛选上限必须是数值。");
                parameters.XldContourMax = number;
            }
            else if (tool.Kind == VmToolKind.XldContourSegmentation)
            {
                parameters.XldSegmentMode = VmXldSegmentMode.Normalize(DockXldSegmentModeComboBox.SelectedValue as string);
                if (!int.TryParse(DockXldSegmentSmoothTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 分段平滑点数必须是整数。");
                parameters.XldSegmentSmooth = integer;
                if (!TryParseUiDouble(DockXldSegmentDistance1TextBox.Text, out number)) return FailDockDraft("XLD 分段最大线距 1 必须是数值。");
                parameters.XldSegmentMaxLineDistance1 = number;
                if (!TryParseUiDouble(DockXldSegmentDistance2TextBox.Text, out number)) return FailDockDraft("XLD 分段最大线距 2 必须是数值。");
                parameters.XldSegmentMaxLineDistance2 = number;
            }
            else if (tool.Kind == VmToolKind.XldLineFitting)
            {
                parameters.XldLineFitAlgorithm = VmXldLineFitAlgorithm.Normalize(DockXldLineFitAlgorithmComboBox.SelectedValue as string);
                if (!int.TryParse(DockXldLineFitMaxPointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 直线拟合最大采样点数必须是整数。");
                parameters.XldLineFitMaxNumPoints = integer;
                if (!int.TryParse(DockXldLineFitClipEndpointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 直线拟合端点裁剪必须是整数。");
                parameters.XldLineFitClippingEndPoints = integer;
                if (!int.TryParse(DockXldLineFitIterationsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 直线拟合迭代次数必须是整数。");
                parameters.XldLineFitIterations = integer;
                if (!TryParseUiDouble(DockXldLineFitClippingFactorTextBox.Text, out number)) return FailDockDraft("XLD 直线拟合裁剪因子必须是数值。");
                parameters.XldLineFitClippingFactor = number;
            }
            else if (tool.Kind == VmToolKind.XldCircleFitting)
            {
                parameters.XldCircleFitAlgorithm = VmXldCircleFitAlgorithm.Normalize(DockXldCircleFitAlgorithmComboBox.SelectedValue as string);
                if (!int.TryParse(DockXldCircleFitMaxPointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 圆拟合最大采样点数必须是整数。");
                parameters.XldCircleFitMaxNumPoints = integer;
                if (!TryParseUiDouble(DockXldCircleFitMaxClosureTextBox.Text, out number)) return FailDockDraft("XLD 圆拟合最大闭合距离必须是数值。");
                parameters.XldCircleFitMaxClosureDistance = number;
                if (!int.TryParse(DockXldCircleFitClipEndpointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 圆拟合端点裁剪必须是整数。");
                parameters.XldCircleFitClippingEndPoints = integer;
                if (!int.TryParse(DockXldCircleFitIterationsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 圆拟合迭代次数必须是整数。");
                parameters.XldCircleFitIterations = integer;
                if (!TryParseUiDouble(DockXldCircleFitClippingFactorTextBox.Text, out number)) return FailDockDraft("XLD 圆拟合裁剪因子必须是数值。");
                parameters.XldCircleFitClippingFactor = number;
            }
            else if (tool.Kind == VmToolKind.XldEllipseFitting)
            {
                parameters.XldEllipseFitAlgorithm = VmXldEllipseFitAlgorithm.Normalize(DockXldEllipseFitAlgorithmComboBox.SelectedValue as string);
                if (!int.TryParse(DockXldEllipseFitMaxPointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 椭圆拟合最大采样点数必须是整数。");
                parameters.XldEllipseFitMaxNumPoints = integer;
                if (!TryParseUiDouble(DockXldEllipseFitMaxClosureTextBox.Text, out number)) return FailDockDraft("XLD 椭圆拟合最大闭合距离必须是数值。");
                parameters.XldEllipseFitMaxClosureDistance = number;
                if (!int.TryParse(DockXldEllipseFitClipEndpointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 椭圆拟合端点裁剪必须是整数。");
                parameters.XldEllipseFitClippingEndPoints = integer;
                if (!int.TryParse(DockXldEllipseFitVossTableTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("Voss 查找表大小必须是整数。");
                parameters.XldEllipseFitVossTabSize = integer;
                if (!int.TryParse(DockXldEllipseFitIterationsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 椭圆拟合迭代次数必须是整数。");
                parameters.XldEllipseFitIterations = integer;
                if (!TryParseUiDouble(DockXldEllipseFitClippingFactorTextBox.Text, out number)) return FailDockDraft("XLD 椭圆拟合裁剪因子必须是数值。");
                parameters.XldEllipseFitClippingFactor = number;
            }
            else if (tool.Kind == VmToolKind.XldRectangleFitting)
            {
                parameters.XldRectangleFitAlgorithm = VmXldRectangleFitAlgorithm.Normalize(DockXldRectangleFitAlgorithmComboBox.SelectedValue as string);
                if (!int.TryParse(DockXldRectangleFitMaxPointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 旋转矩形拟合最大采样点数必须是整数。");
                parameters.XldRectangleFitMaxNumPoints = integer;
                if (!TryParseUiDouble(DockXldRectangleFitMaxClosureTextBox.Text, out number)) return FailDockDraft("XLD 旋转矩形拟合最大闭合距离必须是数值。");
                parameters.XldRectangleFitMaxClosureDistance = number;
                if (!int.TryParse(DockXldRectangleFitClipEndpointsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 旋转矩形拟合端点裁剪必须是整数。");
                parameters.XldRectangleFitClippingEndPoints = integer;
                if (!int.TryParse(DockXldRectangleFitIterationsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("XLD 旋转矩形拟合迭代次数必须是整数。");
                parameters.XldRectangleFitIterations = integer;
                if (!TryParseUiDouble(DockXldRectangleFitClippingFactorTextBox.Text, out number)) return FailDockDraft("XLD 旋转矩形拟合裁剪因子必须是数值。");
                parameters.XldRectangleFitClippingFactor = number;
            }
            else if (tool.Kind == VmToolKind.Blob)
            {
                if (!TryParseUiDouble(DockBlobMinTextBox.Text, out number)) return FailDockDraft("Blob 灰度下限必须是数值。");
                parameters.BlobMinGray = number;
                if (!TryParseUiDouble(DockBlobMaxTextBox.Text, out number)) return FailDockDraft("Blob 灰度上限必须是数值。");
                parameters.BlobMaxGray = number;
                if (!TryParseUiDouble(DockBlobAreaTextBox.Text, out number)) return FailDockDraft("Blob 最小面积必须是数值。");
                parameters.BlobMinArea = number;
            }
            else if (tool.Kind == VmToolKind.GrayStat)
            {
                if (!TryParseUiDouble(DockGrayMinTextBox.Text, out number)) return FailDockDraft("灰度下限必须是数值。");
                parameters.GrayMin = number;
                if (!TryParseUiDouble(DockGrayMaxTextBox.Text, out number)) return FailDockDraft("灰度上限必须是数值。");
                parameters.GrayMax = number;
            }
            else if (tool.Kind == VmToolKind.EdgeMeasure)
            {
                if (!TryParseUiDouble(DockEdgeThresholdTextBox.Text, out number)) return FailDockDraft("边缘阈值必须是数值。");
                parameters.EdgeThreshold = number;
            }
            else if (tool.Kind == VmToolKind.NumericJudge)
            {
                numericOperator = DockNumericOperatorComboBox.SelectedValue as string;
                if (!TryParseUiDouble(DockNumericLowerTextBox.Text, out numericLower) ||
                    !TryParseUiDouble(DockNumericUpperTextBox.Text, out numericUpper) ||
                    !TryParseUiDouble(DockNumericToleranceTextBox.Text, out numericTolerance))
                {
                    return FailDockDraft("数值判定阈值和容差必须是数值。");
                }
                string numericError = VmNumericJudgeParameterValidator.Validate(numericOperator, numericLower, numericUpper, numericTolerance);
                if (!string.IsNullOrWhiteSpace(numericError)) return FailDockDraft(numericError);
            }

            if (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure)
            {
                parameters.RoiExecutionMode = VmRoiExecutionMode.Normalize(DockRoiExecutionModeComboBox.SelectedValue as string);
            }
            parameters.Normalize();
            string parameterError = parameters.Validate(tool.Kind);
            if (!string.IsNullOrWhiteSpace(parameterError)) return FailDockDraft(parameterError);
            string inputError = ValidateDockInputDraft(parameters);
            if (!string.IsNullOrWhiteSpace(inputError)) return FailDockDraft(inputError);
            DockValidationText.Text = string.Empty;
            return true;
        }

        private bool FailDockDraft(string message)
        {
            DockValidationText.Text = message;
            DockDraftStateText.Text = "校验失败";
            DockFooterText.Text = message;
            return false;
        }

        private bool ApplyDockConfigurationDraft(bool refreshUi)
        {
            VmToolInstance tool = dockEditingTool;
            if (tool == null || !SupportsDockConfiguration(tool.Kind))
            {
                return false;
            }

            VmToolParameterData parameters;
            string instanceName;
            bool enabled;
            string numericOperator;
            double numericLower;
            double numericUpper;
            double numericTolerance;
            if (!TryReadDockConfigurationDraft(out parameters, out instanceName, out enabled, out numericOperator, out numericLower, out numericUpper, out numericTolerance))
            {
                return false;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            tool.InstanceName = instanceName;
            tool.IsEnabled = enabled;
            tool.Parameters = parameters;
            ApplyDockInputDraft(tool);
            if (ToolMetadata.SupportsRoi(tool.Kind))
            {
                tool.ReplaceRoiBindings(dockRoiBindingRows.Where(item => item.IsBound).Select(item => item.RoiId));
            }
            if (tool.Kind == VmToolKind.NumericJudge)
            {
                tool.NumericOperator = numericOperator;
                tool.NumericLowerLimit = numericLower;
                tool.NumericUpperLimit = numericUpper;
                tool.NumericTolerance = numericTolerance;
            }

            InvalidateToolRunResult(tool, "模块配置已修改");
            InvalidateDownstreamResults(tool, "上游模块配置已修改");
            dockDraftDirty = false;
            DockDraftStateText.Text = "已应用";
            DockFooterText.Text = "已应用到当前实例，等待运行";
            DockValidationText.Text = string.Empty;
            LogInfo("停靠式模块配置已应用：" + tool.InstanceName);
            RefreshRoiLayerBindingSummaries();
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("应用配置：" + tool.InstanceName, before);
            if (refreshUi)
            {
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
            return true;
        }

        private void DockApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyDockConfigurationDraft(true);
        }

        private void DockRevertButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDockConfigurationDraft(dockEditingTool);
            HeaderStatusText.Text = "已撤销未应用草稿。";
        }

        private void DockTrialRunButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance tool = dockEditingTool;
            VmToolParameterData draft;
            string instanceName;
            bool enabled;
            string numericOperator;
            double numericLower;
            double numericUpper;
            double numericTolerance;
            if (tool == null || !TryReadDockConfigurationDraft(out draft, out instanceName, out enabled, out numericOperator, out numericLower, out numericUpper, out numericTolerance))
            {
                return;
            }

            VmToolParameterData oldParameters = tool.Parameters;
            List<VmToolInputBindingData> oldInputBindings = tool.InputBindings.Select(item => item.Clone()).ToList();
            List<string> oldRoiBindings = tool.BoundRoiIds.ToList();
            string oldInputToolId = tool.InputToolId;
            string oldInputPortName = tool.InputPortName;
            string oldOperator = tool.NumericOperator;
            double oldLower = tool.NumericLowerLimit;
            double oldUpper = tool.NumericUpperLimit;
            double oldTolerance = tool.NumericTolerance;
            try
            {
                tool.Parameters = draft;
                ApplyDockInputDraft(tool);
                if (ToolMetadata.SupportsRoi(tool.Kind)) tool.ReplaceRoiBindings(dockRoiBindingRows.Where(item => item.IsBound).Select(item => item.RoiId));
                tool.NumericOperator = numericOperator;
                tool.NumericLowerLimit = numericLower;
                tool.NumericUpperLimit = numericUpper;
                tool.NumericTolerance = numericTolerance;
                RunStandaloneTool(tool, "停靠式模块配置试运行");
                DockFooterText.Text = "试运行完成；草稿尚未应用";
                DockDraftStateText.Text = dockDraftDirty ? "未应用" : "试运行";
            }
            finally
            {
                tool.Parameters = oldParameters;
                tool.ReplaceInputBindings(oldInputBindings);
                tool.ReplaceRoiBindings(oldRoiBindings);
                tool.InputToolId = oldInputToolId;
                tool.InputPortName = oldInputPortName;
                tool.NumericOperator = oldOperator;
                tool.NumericLowerLimit = oldLower;
                tool.NumericUpperLimit = oldUpper;
                tool.NumericTolerance = oldTolerance;
                RefreshToolConfigurationStatus(tool);
                RefreshDockConfigurationRuntime(tool);
            }
        }

        private void DockOpenCompatibilityButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            if (selected == null || SupportsDockConfiguration(selected.Kind))
            {
                return;
            }

            ToolConfigurationWindow window = new ToolConfigurationWindow(
                selected,
                delegate(VmToolInstance tool) { RunStandaloneTool(tool, "兼容模块配置试运行"); },
                delegate(string name) { return GetToolInstanceNameError(selected, name); },
                roiLayers,
                flowTools) { Owner = this };
            window.ShowDialog();
            if (window.WasApplied)
            {
                InvalidateDownstreamResults(selected, "上游兼容配置已修改");
                RefreshUiState();
            }
        }

        private void BuildDockInputPortRows(VmToolInstance tool)
        {
            dockInputPortRows.Clear();
            if (tool == null)
            {
                DockInputSummaryText.Text = "该模块没有输入端口。";
                return;
            }

            int toolIndex = flowTools.IndexOf(tool);
            IEnumerable<VmToolInstance> previousTools = toolIndex < 0 ? Enumerable.Empty<VmToolInstance>() : flowTools.Take(toolIndex);
            foreach (VmPortDefinition port in ToolMetadata.GetInputPorts(tool.Kind))
            {
                List<VmInputSourceOption> options = new List<VmInputSourceOption>
                {
                    new VmInputSourceOption
                    {
                        Key = GetDockDefaultInputKey(port.PortName),
                        DisplayText = GetDockDefaultInputText(port),
                        DataType = port.DataType,
                        IsDefault = true,
                        IsValid = true
                    }
                };
                if (CanDockSubscribePort(tool, port))
                {
                    foreach (VmToolInstance source in previousTools)
                    {
                        foreach (VmPortDefinition output in ToolMetadata.GetOutputPorts(source.Kind).Where(item => string.Equals(item.DataType, port.DataType, StringComparison.OrdinalIgnoreCase)))
                        {
                            options.Add(new VmInputSourceOption
                            {
                                Key = BuildDockSourceKey(source.ToolId, output.PortName),
                                DisplayText = source.SequenceText + "  " + source.InstanceName + "." + output.DisplayName,
                                SourceToolId = source.ToolId,
                                SourcePortName = output.PortName,
                                DataType = output.DataType,
                                IsValid = source.IsEnabled
                            });
                        }
                    }
                }

                string savedKey = GetDockSavedInputSourceKey(tool, port);
                if (!options.Any(item => string.Equals(item.Key, savedKey, StringComparison.OrdinalIgnoreCase)))
                {
                    options.Add(new VmInputSourceOption
                    {
                        Key = savedKey,
                        DisplayText = "⚠ 已保存来源无效或不在当前模块之前",
                        DataType = port.DataType,
                        IsValid = false
                    });
                }

                VmInputPortEditorRow row = new VmInputPortEditorRow
                {
                    PortName = port.PortName,
                    DisplayName = port.DisplayName,
                    DataType = port.DataType,
                    IsOptional = port.IsOptional,
                    SourceOptions = options,
                    SelectedSourceKey = savedKey
                };
                RefreshDockInputRowState(row);
                dockInputPortRows.Add(row);
            }
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
        }

        private static bool CanDockSubscribePort(VmToolInstance tool, VmPortDefinition port)
        {
            if (tool == null || port == null)
            {
                return false;
            }

            if (tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value")
            {
                return true;
            }

            if (port.PortName == "Image")
            {
                return tool.Kind == VmToolKind.ImageChannel || tool.Kind == VmToolKind.ImageFilter || tool.Kind == VmToolKind.ImageEnhancement || tool.Kind == VmToolKind.ImageThreshold || tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure;
            }

            if ((port.PortName == "OriginalImage" || port.PortName == "ReferenceImage") && tool.Kind == VmToolKind.DynamicThreshold)
            {
                return true;
            }

            if ((port.PortName == "ImageA" || port.PortName == "ImageB") && tool.Kind == VmToolKind.ImageDifference)
            {
                return true;
            }

            if (port.PortName == "Region" && (tool.Kind == VmToolKind.RegionMorphology || tool.Kind == VmToolKind.RegionFeatureFilter || tool.Kind == VmToolKind.RegionContourMeasurement))
            {
                return true;
            }

            if (port.PortName == "XLD" && (tool.Kind == VmToolKind.XldContourFilter || tool.Kind == VmToolKind.XldContourSegmentation || tool.Kind == VmToolKind.XldLineFitting || tool.Kind == VmToolKind.XldCircleFitting || tool.Kind == VmToolKind.XldEllipseFitting || tool.Kind == VmToolKind.XldRectangleFitting))
            {
                return true;
            }

            if ((port.PortName == "RegionA" || port.PortName == "RegionB") && tool.Kind == VmToolKind.RegionSetOperation)
            {
                return true;
            }

            return port.PortName == "ROI" && (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure);
        }

        private string GetDockSavedInputSourceKey(VmToolInstance tool, VmPortDefinition port)
        {
            if (tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value" && !string.IsNullOrWhiteSpace(tool.InputToolId) && !string.IsNullOrWhiteSpace(tool.InputPortName))
            {
                return BuildDockSourceKey(tool.InputToolId, tool.InputPortName);
            }

            VmToolInputBindingData binding = tool.GetInputBinding(port.PortName);
            return binding == null ? GetDockDefaultInputKey(port.PortName) : BuildDockSourceKey(binding.SourceToolId, binding.SourcePortName);
        }

        private void RefreshDockInputRowState(VmInputPortEditorRow row)
        {
            if (row == null)
            {
                return;
            }

            VmInputSourceOption selected = row.SourceOptions == null
                ? null
                : row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
            if (selected == null || !selected.IsValid)
            {
                row.CurrentValue = "--";
                row.StatusText = "输入来源无效";
                return;
            }

            if (selected.IsDefault)
            {
                if (row.PortName == "Image" && currentImage != null)
                {
                    row.CurrentValue = GetImageWidth(currentImage).ToString(CultureInfo.InvariantCulture) + "×" + GetImageHeight(currentImage).ToString(CultureInfo.InvariantCulture);
                    row.StatusText = "默认输入 · 有值";
                }
                else
                {
                    row.CurrentValue = "--";
                    row.StatusText = selected.DisplayText;
                }
                return;
            }

            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selected.SourceToolId, StringComparison.OrdinalIgnoreCase));
            row.CurrentValue = source == null ? "--" : source.GetFormattedOutput(selected.SourcePortName);
            row.StatusText = source == null
                ? "上游模块不存在"
                : (row.CurrentValue == "--" ? "连接有效 · 等待上游运行" : "连接有效 · 有值 · 上游 " + source.ResultCode);
        }

        private string ValidateDockInputDraft(VmToolParameterData parameters)
        {
            foreach (VmInputPortEditorRow row in dockInputPortRows)
            {
                VmInputSourceOption selected = row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                if (selected == null || !selected.IsValid)
                {
                    return row.DisplayName + " 的输入来源无效。";
                }

                if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.NumericJudge && row.PortName == "Value" && selected.IsDefault)
                {
                    return "数值判定必须选择一个位于当前模块之前的 Number 输出。";
                }

                if (dockEditingTool != null &&
                    (dockEditingTool.Kind == VmToolKind.RegionMorphology || dockEditingTool.Kind == VmToolKind.RegionFeatureFilter || dockEditingTool.Kind == VmToolKind.RegionContourMeasurement) &&
                    row.PortName == "Region" && selected.IsDefault)
                {
                    return dockEditingTool.DisplayType + "必须选择一个位于当前模块之前的 Region 输出。";
                }

                if (dockEditingTool != null && (dockEditingTool.Kind == VmToolKind.XldContourFilter || dockEditingTool.Kind == VmToolKind.XldContourSegmentation || dockEditingTool.Kind == VmToolKind.XldLineFitting || dockEditingTool.Kind == VmToolKind.XldCircleFitting || dockEditingTool.Kind == VmToolKind.XldEllipseFitting || dockEditingTool.Kind == VmToolKind.XldRectangleFitting) && row.PortName == "XLD" && selected.IsDefault)
                {
                    return dockEditingTool.DisplayType + "必须选择一个位于当前模块之前的 XLD 输出。";
                }

                if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.RegionSetOperation &&
                    (row.PortName == "RegionA" || row.PortName == "RegionB") && selected.IsDefault)
                {
                    return "Region 集合运算必须为 RegionA 与 RegionB 分别选择前序 Region 输出。";
                }


                if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.DynamicThreshold &&
                    (row.PortName == "OriginalImage" || row.PortName == "ReferenceImage") && selected.IsDefault)
                {
                    return "动态阈值分割必须为原图与参考图分别选择前序单通道 Image 输出。";
                }

                if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.ImageDifference &&
                    (row.PortName == "ImageA" || row.PortName == "ImageB") && selected.IsDefault)
                {
                    return "双图像差分必须为 ImageA 与 ImageB 分别选择前序 Image 输出。";
                }

                if (!selected.IsDefault && row.DataType == "Region" && VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi)
                {
                    return "逐 ROI 模式不能同时订阅上游 Region；请切换为合并 ROI 或恢复默认输入。";
                }
            }
            if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.RegionSetOperation)
            {
                VmInputPortEditorRow rowA = dockInputPortRows.FirstOrDefault(item => item.PortName == "RegionA");
                VmInputPortEditorRow rowB = dockInputPortRows.FirstOrDefault(item => item.PortName == "RegionB");
                if (rowA != null && rowB != null && string.Equals(rowA.SelectedSourceKey, rowB.SelectedSourceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return "RegionA 与 RegionB 不能选择同一个上游端口。";
                }
            }
            if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.DynamicThreshold)
            {
                VmInputPortEditorRow originalRow = dockInputPortRows.FirstOrDefault(item => item.PortName == "OriginalImage");
                VmInputPortEditorRow referenceRow = dockInputPortRows.FirstOrDefault(item => item.PortName == "ReferenceImage");
                if (originalRow != null && referenceRow != null && string.Equals(originalRow.SelectedSourceKey, referenceRow.SelectedSourceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return "原图与参考图不能订阅同一个上游端口；请提供真实的两路 Image。";
                }
            }
            if (dockEditingTool != null && dockEditingTool.Kind == VmToolKind.ImageDifference)
            {
                VmInputPortEditorRow rowA = dockInputPortRows.FirstOrDefault(item => item.PortName == "ImageA");
                VmInputPortEditorRow rowB = dockInputPortRows.FirstOrDefault(item => item.PortName == "ImageB");
                if (rowA != null && rowB != null && string.Equals(rowA.SelectedSourceKey, rowB.SelectedSourceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return "ImageA 与 ImageB 不能订阅同一个上游端口；请提供真实的两路 Image。";
                }
            }
            return string.Empty;
        }

        private void ApplyDockInputDraft(VmToolInstance tool)
        {
            foreach (VmInputPortEditorRow row in dockInputPortRows)
            {
                VmInputSourceOption selected = row.SourceOptions.First(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                if (tool.Kind == VmToolKind.NumericJudge && row.PortName == "Value")
                {
                    tool.InputToolId = selected.IsDefault ? null : selected.SourceToolId;
                    tool.InputPortName = selected.IsDefault ? null : selected.SourcePortName;
                    if (selected.IsDefault) tool.RemoveInputBinding(row.PortName);
                    else tool.SetInputBinding(row.PortName, selected.SourceToolId, selected.SourcePortName);
                }
                else if (selected.IsDefault)
                {
                    tool.RemoveInputBinding(row.PortName);
                }
                else
                {
                    tool.SetInputBinding(row.PortName, selected.SourceToolId, selected.SourcePortName);
                }
            }
        }

        private void DockInputSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            VmInputPortEditorRow row = comboBox == null ? null : comboBox.DataContext as VmInputPortEditorRow;
            if (row == null || dockDraftUpdating)
            {
                return;
            }
            RefreshDockInputRowState(row);
            DockInputPortItemsControl.Items.Refresh();
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
            DockDraftValue_Changed(sender, e);
        }

        private void DockResetInputSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VmInputPortEditorRow row in dockInputPortRows)
            {
                row.SelectedSourceKey = GetDockDefaultInputKey(row.PortName);
                RefreshDockInputRowState(row);
            }
            DockInputPortItemsControl.Items.Refresh();
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
            DockDraftValue_Changed(sender, e);
        }

        private string BuildDockInputDraftSummary()
        {
            if (dockInputPortRows.Count == 0)
            {
                return "该模块没有输入端口。";
            }

            return string.Join("；", dockInputPortRows.Select(row =>
            {
                VmInputSourceOption selected = row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                return row.DisplayName + " ← " + (selected == null ? "无效来源" : selected.DisplayText);
            }));
        }

        private static string GetDockDefaultInputKey(string portName)
        {
            return "default:" + portName;
        }

        private static string BuildDockSourceKey(string toolId, string portName)
        {
            return (toolId ?? string.Empty) + "|" + (portName ?? string.Empty);
        }

        private static string GetDockDefaultInputText(VmPortDefinition port)
        {
            if (port.PortName == "Image") return "系统.Image（当前图像）";
            if (port.PortName == "ROI" || port.PortName == "SearchROI") return "本地 ROI 图层 / 全图兼容";
            if (port.PortName == "ShapeModel") return "当前模板资源";
            if (port.PortName == "Program") return "当前 HDevelop 程序";
            return port.IsOptional ? "未连接（可选）" : "未连接";
        }

        private void BuildDockRoiBindingRows(VmToolInstance tool, VmToolParameterData parameters)
        {
            dockRoiBindingRows.Clear();
            bool supportsRoi = tool != null && ToolMetadata.SupportsRoi(tool.Kind);
            if (supportsRoi)
            {
                foreach (VmRoiLayer layer in roiLayers)
                {
                    dockRoiBindingRows.Add(new VmRoiBindingItem { Layer = layer, IsBound = tool.IsRoiBound(layer.RoiId) });
                }
            }

            bool supportsMode = tool != null && (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure);
            DockRoiExecutionModeComboBox.Visibility = supportsMode ? Visibility.Visible : Visibility.Collapsed;
            DockRoiExecutionModeComboBox.SelectedValue = supportsMode ? VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) : null;
            DockRoiBindingItemsControl.Visibility = supportsRoi ? Visibility.Visible : Visibility.Collapsed;
            DockRoiBindingItemsControl.IsEnabled = tool != null && SupportsDockConfiguration(tool.Kind);
            UpdateDockRoiSummary();
        }

        private void UpdateDockRoiSummary()
        {
            if (dockEditingTool == null || !ToolMetadata.SupportsRoi(dockEditingTool.Kind))
            {
                DockRoiSummaryText.Text = "该模块没有 ROI 输入；可在输入页配置其他类型化来源。";
                return;
            }

            List<string> names = dockRoiBindingRows.Where(item => item.IsBound).Select(item => item.Name).ToList();
            string summary = names.Count == 0
                ? "未绑定 ROI"
                : "已绑定 " + names.Count.ToString(CultureInfo.InvariantCulture) + " 个：" + string.Join("、", names);
            if (dockEditingTool.Kind == VmToolKind.Blob || dockEditingTool.Kind == VmToolKind.GrayStat || dockEditingTool.Kind == VmToolKind.EdgeMeasure)
            {
                summary += "；" + VmRoiExecutionMode.GetDisplayText(DockRoiExecutionModeComboBox.SelectedValue as string);
            }
            DockRoiSummaryText.Text = summary;
        }

        private void DockRoiBindingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateDockRoiSummary();
            DockDraftValue_Changed(sender, e);
        }

        private void DockSelectAllRoiButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VmRoiBindingItem item in dockRoiBindingRows) item.IsBound = true;
            DockRoiBindingItemsControl.Items.Refresh();
            UpdateDockRoiSummary();
            DockDraftValue_Changed(sender, e);
        }

        private void DockClearRoiButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VmRoiBindingItem item in dockRoiBindingRows) item.IsBound = false;
            DockRoiBindingItemsControl.Items.Refresh();
            UpdateDockRoiSummary();
            DockDraftValue_Changed(sender, e);
        }

        private void DockOpenRoiWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (dockEditingTool != null && SupportsDockConfiguration(dockEditingTool.Kind) && dockDraftDirty && !ApplyDockConfigurationDraft(true))
            {
                return;
            }
            RightTabs.SelectedItem = RoiTab;
            RoiLayerPanel.Visibility = Visibility.Visible;
            RoiLayerPanelColumn.Width = new GridLength(220);
            ToggleRoiLayerPanelButton.Content = "隐藏图层";
            HeaderStatusText.Text = dockEditingTool == null ? "已打开 ROI 工作区。" : "已打开 " + dockEditingTool.InstanceName + " 的 ROI 工作区。";
        }

        private void DockBrowseLocalImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择本地图像源",
                Filter = "图像文件|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|所有文件|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) == true)
            {
                DockLocalImagePathTextBox.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(DockLocalImageSerialTextBox.Text))
                {
                    DockLocalImageSerialTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
                DockFooterText.Text = "已选择图像；应用或试运行后生效";
            }
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
            if (ReferenceEquals(selected, dockEditingTool) && !ConfirmDockDraftChanges("调整流程顺序"))
            {
                return;
            }

            int oldIndex = flowTools.IndexOf(selected);
            int newIndex = oldIndex + direction;
            if (newIndex < 0 || newIndex >= flowTools.Count)
            {
                return;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            flowTools.Move(oldIndex, newIndex);
            foreach (VmToolInstance tool in flowTools)
            {
                InvalidateToolRunResult(tool, "流程顺序已调整");
            }
            FlowToolList.SelectedItem = selected;
            LogInfo("流程顺序已调整：" + selected.InstanceName + " -> " + (newIndex + 1));
            CommitFlowEditHistory("调整顺序：" + selected.InstanceName, before);
            RefreshUiState();
        }

        private void DeleteToolButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFlowTools();
        }

        private int DeleteSelectedFlowTools()
        {
            if (isContinuousRunning || isFlowExecutionActive)
            {
                HeaderStatusText.Text = "流程运行期间不能批量删除模块。";
                return 0;
            }

            List<VmToolInstance> selected = GetSelectedFlowToolsInSequence();
            if (selected.Count == 0)
            {
                HeaderStatusText.Text = "请先选择要删除的流程模块。";
                return 0;
            }
            if (selected.Contains(dockEditingTool) && !ConfirmDockDraftChanges("批量删除模块"))
            {
                return 0;
            }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            int index = selected.Min(item => flowTools.IndexOf(item));
            HashSet<string> selectedIds = new HashSet<string>(selected.Select(item => item.ToolId), StringComparer.OrdinalIgnoreCase);
            string names = string.Join("、", selected.Select(item => item.InstanceName));
            foreach (VmToolInstance source in selected)
            {
                InvalidateDownstreamResults(source, "上游工具将被批量删除");
            }

            foreach (VmToolInstance downstream in flowTools.Where(item => !selectedIds.Contains(item.ToolId)).ToList())
            {
                bool bindingRemoved = false;
                foreach (VmToolInputBindingData binding in downstream.InputBindings.Where(item => selectedIds.Contains(item.SourceToolId)).ToList())
                {
                    downstream.RemoveInputBinding(binding.TargetPortName);
                    bindingRemoved = true;
                }

                if (downstream.Kind == VmToolKind.NumericJudge && selectedIds.Contains(downstream.InputToolId ?? string.Empty))
                {
                    downstream.InputToolId = null;
                    downstream.InputPortName = null;
                    bindingRemoved = true;
                }
                if (bindingRemoved)
                {
                    InvalidateToolRunResult(downstream, "上游工具已批量删除");
                }
            }

            selectedFlowConnectionKey = null;
            selectedFlowPortToolId = null;
            selectedFlowPortName = null;
            selectedFlowPortDirection = null;
            ApplyFlowToolSelection(new VmToolInstance[0], null, false);
            foreach (VmToolInstance tool in selected)
            {
                flowTools.Remove(tool);
                tool.Dispose();
            }
            NormalizeFlowGroups();
            dockEditingTool = null;
            dockDraftDirty = false;
            if (flowTools.Count > 0)
            {
                VmToolInstance next = flowTools[Math.Min(index, flowTools.Count - 1)];
                ApplyFlowToolSelection(new[] { next }, next, false);
            }
            else
            {
                RefreshInspector();
            }

            LogInfo("已从真实流程批量删除 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块：" + names);
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
            ScheduleRecipeStateCheck();
            CommitFlowEditHistory("删除 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块", before);
            HeaderStatusText.Text = "已批量删除 " + selected.Count.ToString(CultureInfo.InvariantCulture) + " 个模块；消费者连接已清理，下游结果已失效。";
            return selected.Count;
        }

        private void RenameToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlowToolList.SelectedItem == null)
            {
                return;
            }

            RightTabs.SelectedItem = DockConfigurationTab;
            DockConfigurationTabs.SelectedIndex = 0;
            DockInstanceNameTextBox.Focus();
            DockInstanceNameTextBox.SelectAll();
            HeaderStatusText.Text = "在模块配置草稿中输入实例名称，点击“应用”后生效。";
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
            string nameError = GetToolInstanceNameError(selected, name);
            if (!string.IsNullOrWhiteSpace(nameError))
            {
                HeaderStatusText.Text = nameError;
                ToolInstanceNameTextBox.Text = selected.InstanceName;
                return;
            }

            selected.InstanceName = name;
            LogInfo("工具实例已重命名：" + name);
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
        }

        private string GetToolInstanceNameError(VmToolInstance current, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "实例名称不能为空。";
            }

            return flowTools.Any(item => item != current && string.Equals(item.InstanceName, name, StringComparison.OrdinalIgnoreCase))
                ? "工具实例名称不能重复。"
                : string.Empty;
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
                InvalidateToolRunResult(selected, "工具启停已变化");
                InvalidateDownstreamResults(selected, "上游工具启停已变化");
                RefreshUiState();
            }
        }

        private async void RunCurrentToolButton_Click(object sender, RoutedEventArgs e)
        {
            await StartManualFlowRunAsync(VmFlowRunRequestMode.Current, "运行当前");
        }

        private async void RunToHereButton_Click(object sender, RoutedEventArgs e)
        {
            await StartManualFlowRunAsync(VmFlowRunRequestMode.ToHere, "运行到此");
        }

        private async void RunFromHereButton_Click(object sender, RoutedEventArgs e)
        {
            await StartManualFlowRunAsync(VmFlowRunRequestMode.FromHere, "从此运行");
        }

        private void FlowZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FlowCanvasScaleTransform != null)
            {
                FlowCanvasScaleTransform.ScaleX = e.NewValue;
                FlowCanvasScaleTransform.ScaleY = e.NewValue;
            }
            if (FlowGraphScaleTransform != null)
            {
                FlowGraphScaleTransform.ScaleX = e.NewValue;
                FlowGraphScaleTransform.ScaleY = e.NewValue;
            }
        }

        private void FitFlowButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlowGraphViewport != null && FlowGraphViewport.Visibility == Visibility.Visible && FlowGraphCanvas != null && FlowZoomSlider != null)
            {
                double horizontal = FlowGraphViewport.ViewportWidth <= 0 ? 1 : (FlowGraphViewport.ViewportWidth - 24) / Math.Max(1, FlowGraphCanvas.Width);
                double vertical = FlowGraphViewport.ViewportHeight <= 0 ? 1 : (FlowGraphViewport.ViewportHeight - 24) / Math.Max(1, FlowGraphCanvas.Height);
                FlowZoomSlider.Value = Math.Max(FlowZoomSlider.Minimum, Math.Min(1.0, Math.Min(horizontal, vertical)));
                FlowGraphViewport.ScrollToHorizontalOffset(0);
                FlowGraphViewport.ScrollToVerticalOffset(0);
            }
            else if (FlowZoomSlider != null)
            {
                FlowZoomSlider.Value = 1.0;
            }

            if (FlowToolList != null && FlowToolList.Visibility == Visibility.Visible && FlowToolList.SelectedItem != null)
            {
                FlowToolList.ScrollIntoView(FlowToolList.SelectedItem);
            }

            HeaderStatusText.Text = "流程视图已适应窗口。";
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                this,
                VersionStamp + Environment.NewLine + "VisionMaster 风格工作流，HALCON 视觉内核。",
                "关于 HALCON VM",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
            RightTabs.SelectedItem = DockConfigurationTab;
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

        private void RotatedRectangleRoiButton_Click(object sender, RoutedEventArgs e)
        {
            SetRoiTool(VisionTool.RotatedRectangleRoi, "旋转矩形 ROI：从中心向主轴方向拖动，松开并确认；选择后可旋转及调整宽高。");
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

                VmToolInstance selectedTool = FlowToolList.SelectedItem as VmToolInstance;
                VmRoiLayer layer = AddRoiLayer(pendingRoi, null, selectedTool);
                ClearPendingRoi();
                roiEditor.Tool = VisionTool.Select;
                currentMatches.Clear();
                DisposeToolOverlays();
                HeaderStatusText.Text = selectedTool != null && ToolMetadata.SupportsRoi(selectedTool.Kind)
                    ? "ROI 已新增并绑定到 " + selectedTool.InstanceName
                    : "ROI 已新增；可在右侧选择要绑定的视觉工具。";
                LogInfo("ROI 图层已新增：" + layer.Name + "，" + layer.GeometryText + "，" + layer.BindingSummary);
                RefreshUiState();
                ScheduleRefreshDisplay();
            });
        }

        private void ClearRoiButton_Click(object sender, RoutedEventArgs e)
        {
            VmRoiLayer selected = RoiLayerList.SelectedItem as VmRoiLayer;
            if (selected == null)
            {
                HeaderStatusText.Text = "请先选择要删除的 ROI 图层。";
                return;
            }

            if (selected.IsLocked)
            {
                HeaderStatusText.Text = "ROI 已锁定，解锁后才能删除：" + selected.Name;
                LogInfo(HeaderStatusText.Text);
                return;
            }

            int index = roiLayers.IndexOf(selected);
            InvalidateRoiDependentResults(selected.RoiId, "ROI 图层已删除");
            foreach (VmToolInstance tool in flowTools)
            {
                tool.UnbindRoi(selected.RoiId);
            }
            roiLayers.Remove(selected);
            selected.Dispose();
            RefreshRoiLayerSequence();
            VmRoiLayer next = roiLayers.Count == 0 ? null : roiLayers[Math.Min(index, roiLayers.Count - 1)];
            RoiLayerList.SelectedItem = next;
            SelectRoiLayer(next);
            ClearPendingRoi();
            currentMatches.Clear();
            roiEditor.Cancel();
            HeaderStatusText.Text = "ROI 图层已删除：" + selected.Name;
            LogInfo(HeaderStatusText.Text);
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void ClearAllRoiLayersButton_Click(object sender, RoutedEventArgs e)
        {
            VmRoiLayer locked = roiLayers.FirstOrDefault(item => item.IsLocked);
            if (locked != null)
            {
                HeaderStatusText.Text = "存在已锁定 ROI，全部清空已取消：" + locked.Name;
                LogInfo(HeaderStatusText.Text);
                return;
            }

            foreach (VmToolInstance tool in flowTools)
            {
                InvalidateToolRunResult(tool, "ROI 图层已清空");
                tool.ReplaceRoiBindings(null);
            }

            DisposeCurrentRoi();
            DisposeRoiLayers();
            ClearPendingRoi();
            currentMatches.Clear();
            roiEditor.Cancel();
            RefreshRoiLayerSequence();
            RefreshRoiLayerBindingSummaries();
            HeaderStatusText.Text = "全部 ROI 图层已清空。";
            LogInfo(HeaderStatusText.Text);
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void CancelRoiDrawingButton_Click(object sender, RoutedEventArgs e)
        {
            FinishRoiGeometryEdit(false);
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Select;
            HeaderStatusText.Text = "ROI 绘制已取消。";
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void SelectImageToolButton_Click(object sender, RoutedEventArgs e)
        {
            FinishRoiGeometryEdit(false);
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Select;
            HeaderStatusText.Text = "选择模式：单击 ROI 选中，拖动内部移动，拖动控制点调整；右键平移。";
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void PanImageToolButton_Click(object sender, RoutedEventArgs e)
        {
            FinishRoiGeometryEdit(false);
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Pan;
            HeaderStatusText.Text = "平移模式：在图像上按住右键拖动。";
            RefreshUiState();
        }

        private void ImageOriginalSizeButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureDisplayImage();
            viewport.OriginalSize(imageWindow);
            HeaderStatusText.Text = "图像已切换到 1:1 显示。";
            ScheduleRefreshDisplay();
            RefreshUiState();
        }

        private void ToggleRoiLayerPanelButton_Click(object sender, RoutedEventArgs e)
        {
            bool show = RoiLayerPanel.Visibility != Visibility.Visible;
            RoiLayerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            RoiLayerPanelColumn.Width = show ? new GridLength(220) : new GridLength(0);
            ToggleRoiLayerPanelButton.Content = show ? "隐藏图层" : "显示图层";
            ScheduleRefreshDisplay();
        }

        private void ImageWorkspaceExpandButton_Click(object sender, RoutedEventArgs e)
        {
            imageWorkspaceExpanded = !imageWorkspaceExpanded;
            if (imageWorkspaceExpanded)
            {
                Grid.SetColumn(ImageWorkspacePanel, 0);
                Grid.SetColumnSpan(ImageWorkspacePanel, 5);
                Grid.SetRow(ImageWorkspacePanel, 0);
                Grid.SetRowSpan(ImageWorkspacePanel, 3);
                Panel.SetZIndex(ImageWorkspacePanel, 50);
                ImageWorkspaceExpandButton.Content = "▣ 恢复布局";
                HeaderStatusText.Text = "图像工作区已放大；流程和配置布局保留在原位置。";
            }
            else
            {
                Grid.SetColumn(ImageWorkspacePanel, 4);
                Grid.SetColumnSpan(ImageWorkspacePanel, 1);
                Grid.SetRow(ImageWorkspacePanel, 0);
                Grid.SetRowSpan(ImageWorkspacePanel, 1);
                Panel.SetZIndex(ImageWorkspacePanel, 0);
                ImageWorkspaceExpandButton.Content = "⛶ 放大图像";
                HeaderStatusText.Text = "图像工作区已恢复原布局。";
            }

            Dispatcher.BeginInvoke(new Action(delegate
            {
                UpdateImageContextViewport(true);
                ScheduleRefreshDisplay();
            }), DispatcherPriority.Loaded);
        }

        private void ImageOverlayVisibility_Changed(object sender, RoutedEventArgs e)
        {
            showRoiOverlay = ShowRoiOverlayCheckBox == null || ShowRoiOverlayCheckBox.IsChecked == true;
            showResultOverlay = ShowResultOverlayCheckBox == null || ShowResultOverlayCheckBox.IsChecked == true;
            if (HeaderStatusText != null)
            {
                HeaderStatusText.Text = "叠加显示：ROI " + (showRoiOverlay ? "开启" : "隐藏") + "，结果 " + (showResultOverlay ? "开启" : "隐藏") + "；数据与绑定未删除。";
            }
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void RoiLayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectRoiLayer(RoiLayerList.SelectedItem as VmRoiLayer);
            RefreshRoiBindingEditor();
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void RoiLayerVisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            VmRoiLayer layer = checkBox == null ? null : checkBox.DataContext as VmRoiLayer;
            if (layer != null)
            {
                layer.IsVisible = checkBox.IsChecked == true;
                ScheduleRefreshDisplay();
            }
        }

        private void RoiLayerEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            VmRoiLayer layer = checkBox == null ? null : checkBox.DataContext as VmRoiLayer;
            if (layer != null)
            {
                layer.IsEnabled = checkBox.IsChecked == true;
                InvalidateRoiDependentResults(layer.RoiId, "ROI 启停已变化");
                RefreshRoiLayerBindingSummaries();
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
        }

        private void RoiLayerLockCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            VmRoiLayer layer = checkBox == null ? null : checkBox.DataContext as VmRoiLayer;
            if (layer == null)
            {
                return;
            }

            SetRoiLayerLocked(layer, checkBox.IsChecked == true);
        }

        private void SelectedRoiLockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            VmRoiLayer layer = RoiLayerList.SelectedItem as VmRoiLayer;
            if (layer != null)
            {
                SetRoiLayerLocked(layer, SelectedRoiLockCheckBox.IsChecked == true);
            }
        }

        private void ToggleSelectedRoiLockButton_Click(object sender, RoutedEventArgs e)
        {
            VmRoiLayer layer = RoiLayerList.SelectedItem as VmRoiLayer;
            if (layer == null)
            {
                HeaderStatusText.Text = "请先选择要锁定或解锁的 ROI。";
                return;
            }

            SetRoiLayerLocked(layer, !layer.IsLocked);
        }

        private void SetRoiLayerLocked(VmRoiLayer layer, bool locked)
        {
            if (layer == null || layer.IsLocked == locked)
            {
                return;
            }

            if (roiEditingLayer == layer)
            {
                FinishRoiGeometryEdit(false);
            }
            layer.IsLocked = locked;
            HeaderStatusText.Text = layer.Name + (locked ? " 已锁定，几何和删除受保护。" : " 已解锁，可在选择模式中编辑。");
            LogInfo(HeaderStatusText.Text);
            SelectRoiLayer(layer);
            ScheduleRecipeStateCheck();
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void CopySelectedRoiButton_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("复制ROI", delegate
            {
                VmRoiLayer source = RoiLayerList.SelectedItem as VmRoiLayer;
                if (source == null || source.Geometry == null)
                {
                    throw new InvalidOperationException("请先选择要复制的 ROI 图层。");
                }

                ResolvedImageContext context = ResolveImageContext();
                using (RoiData offset = RoiGeometryEditor.Offset(source.Geometry, 12, 12, context.Width, context.Height))
                {
                    VmRoiLayer copy = AddRoiLayer(offset, CreateUniqueRoiCopyName(source.Name), null);
                    copy.IsEnabled = source.IsEnabled;
                    copy.IsVisible = source.IsVisible;
                    copy.IsLocked = false;
                    foreach (VmToolInstance tool in flowTools.Where(item => item.IsRoiBound(source.RoiId)))
                    {
                        tool.BindRoi(copy.RoiId);
                        InvalidateToolRunResult(tool, "ROI 图层已复制");
                    }
                    RefreshRoiLayerBindingSummaries();
                    ScheduleRecipeStateCheck();
                    HeaderStatusText.Text = "ROI 已复制：" + source.Name + " → " + copy.Name;
                    LogInfo(HeaderStatusText.Text);
                    RefreshUiState();
                    ScheduleRefreshDisplay();
                }
            });
        }

        private void RoiBindingCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (roiBindingUpdating)
            {
                return;
            }

            CheckBox checkBox = sender as CheckBox;
            VmRoiBindingItem row = checkBox == null ? null : checkBox.DataContext as VmRoiBindingItem;
            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            if (row == null || tool == null || !ToolMetadata.SupportsRoi(tool.Kind))
            {
                return;
            }

            if (checkBox.IsChecked == true)
            {
                tool.BindRoi(row.RoiId);
            }
            else
            {
                tool.UnbindRoi(row.RoiId);
            }

            InvalidateToolRunResult(tool, "ROI 绑定已变化");

            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void SelectedRoiLayerNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            VmRoiLayer layer = RoiLayerList.SelectedItem as VmRoiLayer;
            if (layer == null)
            {
                return;
            }

            string name = SelectedRoiLayerNameTextBox.Text == null ? string.Empty : SelectedRoiLayerNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name) || roiLayers.Any(item => item != layer && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedRoiLayerNameTextBox.Text = layer.Name;
                HeaderStatusText.Text = "ROI 图层名称不能为空或重复。";
                return;
            }

            layer.Name = name;
            InvalidateRoiDependentResults(layer.RoiId, "ROI 名称已变化");
            RoiSelectedLayerText.Text = layer.SequenceText + "  " + layer.Name + " · " + layer.ShapeText;
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
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
            string directory = string.IsNullOrWhiteSpace(currentRecipePath) ? null : Path.GetDirectoryName(currentRecipePath);
            OpenDirectory(!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory) ? directory : recipeService.RecipeDirectory);
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

        private void ImageContextComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (imageContextUpdating || ImageContextComboBox == null)
            {
                return;
            }

            if (uiReady)
            {
                imageContextManuallySelected = true;
            }
            UpdateImageContextViewport(true);
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void SetImageContextSelection(string key)
        {
            if (ImageContextComboBox == null)
            {
                return;
            }

            imageContextUpdating = true;
            try
            {
                ImageContextComboBox.SelectedValue = key;
            }
            finally
            {
                imageContextUpdating = false;
            }
        }

        private ResolvedImageContext ResolveImageContext()
        {
            string requested = ImageContextComboBox == null
                ? VmImageContextOption.GlobalInput
                : ImageContextComboBox.SelectedValue as string ?? VmImageContextOption.GlobalInput;
            VmToolInstance selected = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;

            if (requested == VmImageContextOption.ModuleOutput && selected != null)
            {
                VmPortDefinition imagePort = ToolMetadata.GetOutputPorts(selected.Kind)
                    .FirstOrDefault(item => string.Equals(item.DataType, "Image", StringComparison.OrdinalIgnoreCase));
                VmImageSnapshot snapshot = GetImageSnapshot(selected, imagePort == null ? null : imagePort.PortName);
                if (snapshot != null && !snapshot.IsDisposed)
                {
                    return CreateSnapshotContext(
                        requested,
                        snapshot,
                        selected.InstanceName + "." + imagePort.PortName,
                        "模块输出 · 有值");
                }

                return ResolveModuleInputContext(
                    selected,
                    requested,
                    "当前模块没有可用 Image 输出，已回退到模块输入/全局输入");
            }

            if (requested == VmImageContextOption.ModuleInput)
            {
                return ResolveModuleInputContext(
                    selected,
                    requested,
                    selected == null ? "未选择模块，已回退到全局输入" : null);
            }

            return CreateGlobalImageContext(requested, null, "全局输入");
        }

        private ResolvedImageContext ResolveModuleInputContext(VmToolInstance tool, string requested, string fallbackReason)
        {
            VmPortDefinition imageInputPort = tool == null
                ? null
                : ToolMetadata.GetInputPorts(tool.Kind).FirstOrDefault(item => string.Equals(item.DataType, "Image", StringComparison.OrdinalIgnoreCase));
            if (imageInputPort != null)
            {
                VmToolInputBindingData binding = tool.GetInputBinding(imageInputPort.PortName);
                if (binding != null)
                {
                    VmToolInstance source = GetInputSourceTool(binding);
                    VmImageSnapshot snapshot = GetImageSnapshot(source, binding.SourcePortName);
                    if (snapshot != null && !snapshot.IsDisposed)
                    {
                        return CreateSnapshotContext(
                            requested,
                            snapshot,
                            tool.InstanceName + "." + imageInputPort.PortName + " ← " + source.InstanceName + "." + binding.SourcePortName,
                            string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)
                                ? "模块输入 · 有值"
                                : "模块输入 · 上游非 OK 历史值");
                    }

                    fallbackReason = tool.InstanceName + "." + imageInputPort.PortName + " 已订阅但暂无有效快照，已回退到全局输入";
                }
                else if (string.Equals(imageInputPort.PortName, "Image", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateGlobalImageContext(
                        requested,
                        fallbackReason,
                        tool.InstanceName + "." + imageInputPort.PortName + " ← 系统.Image");
                }
            }

            if (tool != null)
            {
                string upstreamSource;
                VmImageSnapshot upstreamSnapshot = FindUpstreamImageSnapshot(tool, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out upstreamSource);
                if (upstreamSnapshot != null && !upstreamSnapshot.IsDisposed)
                {
                    return CreateSnapshotContext(
                        requested,
                        upstreamSnapshot,
                        tool.InstanceName + " 关联图像 ← " + upstreamSource,
                        "模块关联图像 · 有值");
                }
            }

            return CreateGlobalImageContext(
                requested,
                fallbackReason ?? (tool == null ? "未选择模块，已回退到全局输入" : "当前模块没有 Image 输入，已回退到全局输入"),
                "全局输入");
        }

        private VmImageSnapshot FindUpstreamImageSnapshot(VmToolInstance tool, ISet<string> visitedToolIds, out string sourceText)
        {
            sourceText = null;
            if (tool == null || visitedToolIds == null || !visitedToolIds.Add(tool.ToolId ?? string.Empty))
            {
                return null;
            }

            foreach (VmToolInputBindingData binding in tool.InputBindings)
            {
                VmToolInstance source = GetInputSourceTool(binding);
                if (source == null)
                {
                    continue;
                }

                VmPortDefinition sourcePort = ToolMetadata.GetOutputPorts(source.Kind)
                    .FirstOrDefault(item => string.Equals(item.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                if (sourcePort != null && string.Equals(sourcePort.DataType, "Image", StringComparison.OrdinalIgnoreCase))
                {
                    VmImageSnapshot snapshot = GetImageSnapshot(source, binding.SourcePortName);
                    if (snapshot != null && !snapshot.IsDisposed)
                    {
                        sourceText = source.InstanceName + "." + binding.SourcePortName;
                        return snapshot;
                    }
                }

                string nestedSource;
                VmImageSnapshot nested = FindUpstreamImageSnapshot(source, visitedToolIds, out nestedSource);
                if (nested != null && !nested.IsDisposed)
                {
                    sourceText = nestedSource;
                    return nested;
                }
            }

            return null;
        }

        private ResolvedImageContext CreateSnapshotContext(string requested, VmImageSnapshot snapshot, string sourceText, string stateText)
        {
            return new ResolvedImageContext
            {
                RequestedKey = requested,
                Snapshot = snapshot,
                SourceText = sourceText,
                DetailText = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}×{1} · SN={2} · {3}",
                    snapshot.Width,
                    snapshot.Height,
                    string.IsNullOrWhiteSpace(snapshot.SerialNumber) ? "--" : snapshot.SerialNumber,
                    snapshot.SourceName),
                StateText = stateText,
                Width = snapshot.Width,
                Height = snapshot.Height
            };
        }

        private ResolvedImageContext CreateGlobalImageContext(string requested, string fallbackReason, string sourceText)
        {
            int width = currentImage == null ? 0 : GetImageWidth(currentImage);
            int height = currentImage == null ? 0 : GetImageHeight(currentImage);
            string name = string.IsNullOrWhiteSpace(currentImagePath) ? "内存/相机图像" : Path.GetFileName(currentImagePath);
            return new ResolvedImageContext
            {
                RequestedKey = requested,
                GlobalImage = currentImage,
                SourceText = sourceText,
                DetailText = currentImage == null
                    ? "--"
                    : string.Format(CultureInfo.InvariantCulture, "{0}×{1} · SN=-- · {2}", width, height, name),
                StateText = currentImage == null
                    ? (fallbackReason ?? "全局输入暂无图像")
                    : (fallbackReason ?? "全局输入 · 有值"),
                Width = width,
                Height = height
            };
        }

        private void UpdateImageContextViewport(bool fit)
        {
            ResolvedImageContext context = ResolveImageContext();
            if (!context.HasImage)
            {
                return;
            }

            viewport.SetImageSize(context.Width, context.Height);
            if (fit)
            {
                viewport.Fit();
            }
        }

        private void EnsureDisplayImage()
        {
            if (!ResolveImageContext().HasImage)
            {
                throw new InvalidOperationException("当前图像上下文没有可用图像，请先运行本地图像源或打开图片。");
            }
        }

        private void ImageWindow_MouseDown(object sender, Forms.MouseEventArgs e)
        {
            if (!ResolveImageContext().HasImage)
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
            if (roiEditor.Tool == VisionTool.Select)
            {
                BeginRoiGeometryEdit(imagePoint, e.Location);
                return;
            }
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
            ResolvedImageContext context = ResolveImageContext();
            if (!context.HasImage)
            {
                return;
            }

            PointF imagePoint = viewport.WindowToImage(e.Location, imageWindow);
            MouseStatusText.Text = string.Format(CultureInfo.InvariantCulture, "坐标：R {0:F1}, C {1:F1}", imagePoint.Y, imagePoint.X);
            int pixelRow = (int)Math.Floor(imagePoint.Y);
            int pixelColumn = (int)Math.Floor(imagePoint.X);
            string pixelValue;
            try
            {
                pixelValue = context.GetPixelDisplay(pixelRow, pixelColumn);
            }
            catch
            {
                pixelValue = "不可读";
            }
            ImagePixelStatusText.Text = string.Format(
                CultureInfo.InvariantCulture,
                "像素：R {0}, C {1}, G {2}",
                pixelRow,
                pixelColumn,
                pixelValue);

            if (isPanning)
            {
                viewport.PanByWindowDelta(e.X - lastPanPoint.X, e.Y - lastPanPoint.Y, imageWindow);
                lastPanPoint = e.Location;
                ScheduleRefreshDisplay();
                RefreshUiState();
                return;
            }

            if (roiEditingLayer != null && roiEditOriginalGeometry != null)
            {
                try
                {
                    RoiData transformed = RoiGeometryEditor.TransformDetailed(
                        roiEditOriginalGeometry,
                        roiEditHandle,
                        roiEditVertexIndex,
                        roiEditStartPoint,
                        imagePoint,
                        context.Width,
                        context.Height);
                    roiEditingLayer.ReplaceGeometry(transformed);
                    roiGeometryChanged = true;
                    SelectRoiLayer(roiEditingLayer);
                    HeaderStatusText.Text = "ROI 编辑中：" + roiEditingLayer.Name + " · " + GetRoiEditHandleText(roiEditHandle, roiEditVertexIndex);
                }
                catch (InvalidOperationException ex)
                {
                    HeaderStatusText.Text = "ROI 编辑受限：" + ex.Message;
                }
                ScheduleRefreshDisplay();
                return;
            }

            if (roiEditor.Tool == VisionTool.Select)
            {
                VmRoiLayer selectedLayer = RoiLayerList.SelectedItem as VmRoiLayer;
                RoiEditHandle hoverHandle = selectedLayer == null || selectedLayer.IsLocked
                    ? RoiEditHandle.None
                    : RoiGeometryEditor.HitTest(selectedLayer.Geometry, imagePoint, GetRoiHitTolerance(e.Location));
                imageWindow.Cursor = hoverHandle == RoiEditHandle.None
                    ? Forms.Cursors.Default
                    : (hoverHandle == RoiEditHandle.Move ? Forms.Cursors.SizeAll : Forms.Cursors.Cross);
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

            if (e.Button == Forms.MouseButtons.Left && roiEditingLayer != null)
            {
                FinishRoiGeometryEdit(true);
                return;
            }

            if (!ResolveImageContext().HasImage || e.Button != Forms.MouseButtons.Left || roiEditor.Tool == VisionTool.PolygonRoi)
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
            if (!ResolveImageContext().HasImage)
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
            if (!ResolveImageContext().HasImage)
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

        private async void RunTimer_Tick(object sender, EventArgs e)
        {
            if (!isContinuousRunning || isPauseRequested || isFlowExecutionActive)
            {
                return;
            }

            await RunInspectionRangeAsync(VmFlowRunRequestMode.Full, "连续运行");
            if (isContinuousRunning && !isStopRequested && imageFiles.Count > 1)
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
            UpdateImageContextViewport(true);
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
            UpdateImageContextViewport(true);
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

        private async Task StartManualFlowRunAsync(VmFlowRunRequestMode mode, string source)
        {
            try
            {
                if (isContinuousRunning)
                {
                    StopContinuousRun();
                }

                if (isFlowExecutionActive)
                {
                    HeaderStatusText.Text = "已有流程正在运行，请先停止或等待完成。";
                    return;
                }

                isStopRequested = false;
                isPauseRequested = false;
                await RunInspectionRangeAsync(mode, source);
            }
            catch (Exception ex)
            {
                HandleFlowRunException(source, ex);
            }
        }

        private void SetFlowExecutionPointer(VmToolInstance tool)
        {
            foreach (VmToolInstance item in flowTools)
            {
                item.IsExecutionPointer = ReferenceEquals(item, tool);
            }
            debugPausedToolId = tool == null ? null : tool.ToolId;
            RefreshFlowPortVisualization();
        }

        private void ResetFlowDebugRuntimeState(bool clearPause)
        {
            isDebugBreakpointPause = false;
            debugSingleStepRequested = false;
            debugPauseBeforeNextTool = false;
            debugPausedToolId = null;
            if (clearPause) isPauseRequested = false;
            foreach (VmToolInstance item in flowTools) item.IsExecutionPointer = false;
            RefreshFlowPortVisualization();
        }

        private async Task RunInspectionRangeAsync(VmFlowRunRequestMode mode, string source)
        {
            if (isFlowExecutionActive)
            {
                return;
            }

            VmToolInstance selected = FlowToolList.SelectedItem as VmToolInstance;
            VmFlowExecutionPlan plan = VmFlowExecutionPlanner.BuildPlan(flowTools, selected, mode);
            VmFlowRunPolicy policy = ReadFlowRunPolicyFromUi();
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<InspectionRecord> records = new List<InspectionRecord>();
            string stopReason = string.Empty;
            string resultCode = "--";
            bool executionError = false;

            isFlowExecutionActive = true;
            FlowRunStatusText.Text = "流程运行中";
            FlowRunRangeText.Text = "范围：" + plan.RangeText;
            FlowRunResultText.Text = "RUN";
            FlowRunElapsedText.Text = "--";
            FlowStopReasonText.Text = "--";
            FlowCurrentToolText.Text = "准备执行";
            FlowRuntimeStatusBarText.Text = "流程：" + plan.RangeText;
            if (RightTabs != null && ModuleResultTab != null)
            {
                RightTabs.SelectedItem = ModuleResultTab;
            }
            RefreshUiState();

            currentMatches.Clear();
            DisposeToolOverlays();
            if (mode == VmFlowRunRequestMode.Full)
            {
                foreach (VmToolInstance tool in flowTools)
                {
                    tool.ClearRuntimeOutputs();
                }
            }

            try
            {
                foreach (VmToolInstance tool in plan.Tools)
                {
                    bool pauseForDebug = !debugSingleStepRequested && (debugPauseBeforeNextTool || tool.IsBreakpoint);
                    if (pauseForDebug)
                    {
                        bool causedByBreakpoint = tool.IsBreakpoint && !debugPauseBeforeNextTool;
                        debugPauseBeforeNextTool = false;
                        isDebugBreakpointPause = true;
                        isPauseRequested = true;
                        SetFlowExecutionPointer(tool);
                        FlowRunStatusText.Text = causedByBreakpoint ? "命中断点" : "单步暂停";
                        FlowCurrentToolText.Text = "下一执行 #" + tool.SequenceText + " · " + tool.InstanceName;
                        FlowRuntimeStatusBarText.Text = "调试：" + (causedByBreakpoint ? "断点" : "单步") + " · 下一执行 " + tool.InstanceName;
                        HeaderStatusText.Text = (causedByBreakpoint ? "命中断点：" : "单步暂停：") + tool.InstanceName + "；可单步、继续或停止。";
                        LogInfo(HeaderStatusText.Text);
                        RefreshUiState();
                    }
                    else
                    {
                        SetFlowExecutionPointer(tool);
                    }

                    VmFlowStepDecision beforeDecision = VmFlowRuntimeDecider.EvaluateBeforeStep(
                        policy,
                        stopwatch.ElapsedMilliseconds,
                        isPauseRequested,
                        isStopRequested);
                    while (beforeDecision == VmFlowStepDecision.Pause)
                    {
                        FlowRunStatusText.Text = "流程已暂停";
                        FlowCurrentToolText.Text = "等待继续 · 下一步 " + tool.InstanceName;
                        await Task.Delay(50);
                        beforeDecision = VmFlowRuntimeDecider.EvaluateBeforeStep(
                            policy,
                            stopwatch.ElapsedMilliseconds,
                            isPauseRequested,
                            isStopRequested);
                    }

                    if (beforeDecision == VmFlowStepDecision.UserStop)
                    {
                        stopReason = "用户停止";
                        resultCode = "STOP";
                        break;
                    }

                    if (beforeDecision == VmFlowStepDecision.Timeout)
                    {
                        stopReason = "流程超时（" + policy.FlowTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms）";
                        resultCode = "TIMEOUT";
                        break;
                    }

                    FlowRunStatusText.Text = "流程运行中";
                    FlowCurrentToolText.Text = tool.SequenceText + " · " + tool.InstanceName;
                    FlowRuntimeStatusBarText.Text = "流程：运行 " + tool.InstanceName;
                    await Dispatcher.Yield(DispatcherPriority.Background);

                    InspectionRecord record;
                    try
                    {
                        record = ExecuteFlowTool(tool, source);
                        records.Add(record);
                    }
                    catch (Exception ex)
                    {
                        stopReason = tool.InstanceName + " 执行错误：" + ex.Message;
                        resultCode = "NG";
                        executionError = true;
                        AppendAlarm(stopReason);
                        break;
                    }

                    if (debugSingleStepRequested)
                    {
                        debugSingleStepRequested = false;
                        debugPauseBeforeNextTool = true;
                    }

                    VmFlowStepDecision afterDecision = VmFlowRuntimeDecider.EvaluateAfterStep(
                        policy,
                        stopwatch.ElapsedMilliseconds,
                        record.ResultCode,
                        isStopRequested);
                    if (afterDecision == VmFlowStepDecision.UserStop)
                    {
                        stopReason = "用户停止";
                        resultCode = "STOP";
                        break;
                    }

                    if (afterDecision == VmFlowStepDecision.Timeout)
                    {
                        stopReason = "流程超时（" + policy.FlowTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms）";
                        resultCode = "TIMEOUT";
                        break;
                    }

                    if (afterDecision == VmFlowStepDecision.NgStop)
                    {
                        stopReason = "遇 NG 停止 · " + tool.InstanceName;
                        resultCode = "NG";
                        break;
                    }

                    await Dispatcher.Yield(DispatcherPriority.Background);
                    if (isStopRequested)
                    {
                        stopReason = "用户停止";
                        resultCode = "STOP";
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(resultCode))
                {
                    resultCode = "--";
                }

                if (resultCode == "--")
                {
                    bool allOk = records.Count == plan.Tools.Count && records.All(record => string.Equals(record.ResultCode, "OK", StringComparison.OrdinalIgnoreCase));
                    resultCode = allOk ? "OK" : "NG";
                    stopReason = allOk ? "范围执行完成" : "范围执行完成（包含 NG）";
                }

                if (records.Count > 0)
                {
                    string message = string.Join(" | ", records.Select(item => item.InspectionType + ":" + item.Message));
                    if (mode == VmFlowRunRequestMode.Full)
                    {
                        runtimeStatistics.Record(resultCode, stopwatch.Elapsed.TotalMilliseconds, message);
                    }

                    lastResultPayload = BuildJsonResultPayload(resultCode, stopwatch.Elapsed.TotalMilliseconds, records);
                    LastMessageText.Text = "最新外发结果：" + lastResultPayload;
                    if (mode == VmFlowRunRequestMode.Full && resultCode != "STOP" && resultCode != "TIMEOUT")
                    {
                        AutoSendResultIfNeeded();
                    }
                }

                if (isContinuousRunning && (executionError || (resultCode == "NG" && policy.StopOnNg) || resultCode == "TIMEOUT"))
                {
                    isContinuousRunning = false;
                    runTimer.Stop();
                }

                HeaderStatusText.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}：{1}，范围 {2}，耗时 {3:F1} ms",
                    source,
                    resultCode,
                    plan.RangeText,
                    stopwatch.Elapsed.TotalMilliseconds);
                LogInfo(HeaderStatusText.Text + "，停止原因：" + stopReason);
            }
            finally
            {
                stopwatch.Stop();
                isFlowExecutionActive = false;
                FlowRunStatusText.Text = resultCode == "OK" ? "流程完成" : (resultCode == "STOP" ? "流程已停止" : "流程结束");
                FlowRunResultText.Text = resultCode;
                FlowRunElapsedText.Text = stopwatch.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms";
                FlowStopReasonText.Text = string.IsNullOrWhiteSpace(stopReason) ? "--" : stopReason;
                FlowCurrentToolText.Text = "--";
                FlowRuntimeStatusBarText.Text = "流程：" + resultCode + " · " + stopwatch.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + " ms";
                ResetFlowDebugRuntimeState(true);
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
        }

        private VmFlowRunPolicy ReadFlowRunPolicyFromUi()
        {
            int interval;
            if (!int.TryParse(ContinuousIntervalTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out interval) || interval < 50 || interval > 60000)
            {
                throw new InvalidOperationException("连续运行间隔必须是 50-60000 ms 的整数。");
            }

            int timeout;
            if (!int.TryParse(FlowTimeoutTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeout) || timeout < 0 || timeout > 3600000 || timeout > 0 && timeout < 50)
            {
                throw new InvalidOperationException("流程超时必须为 0（关闭）或 50-3600000 ms 的整数。");
            }

            return new VmFlowRunPolicy
            {
                ContinuousIntervalMilliseconds = interval,
                FlowTimeoutMilliseconds = timeout,
                StopOnNg = StopOnNgCheckBox.IsChecked == true
            };
        }

        private void HandleFlowRunException(string actionName, Exception ex)
        {
            HeaderStatusText.Text = actionName + "失败：" + ex.Message;
            LogInfo(HeaderStatusText.Text);
            AppendAlarm(HeaderStatusText.Text);
            FlowRunStatusText.Text = "运行失败";
            FlowRunResultText.Text = "NG";
            FlowStopReasonText.Text = ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, actionName, MessageBoxButton.OK, MessageBoxImage.Warning);
            RefreshUiState();
        }

        private void RunStandaloneTool(VmToolInstance tool, string source)
        {
            RunUiAction(source, delegate
            {
                if (isContinuousRunning || isFlowExecutionActive)
                {
                    throw new InvalidOperationException("流程正在运行，请先停止或等待完成。");
                }

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
            InvalidateDownstreamResults(tool, "上游工具已重新运行");
            tool.ClearRuntimeOutputs();
            tool.RunStatus = "运行中";
            tool.ResultCode = "--";
            tool.ErrorMessage = string.Empty;
            try
            {
                InspectionRecord record;
                switch (tool.Kind)
                {
                    case VmToolKind.ImageSource:
                        record = RunLocalImageSourceTool(tool);
                        tool.InputSummary = "本地文件：" + Path.GetFileName((tool.Parameters ?? new VmToolParameterData()).LocalImagePath);
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.ImageChannel:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunImageChannelTool(tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.ImageFilter:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunImageFilterTool(tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.ImageEnhancement:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunImageEnhancementTool(tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.ImageDifference:
                        string imageDifferenceError = GetImageDifferenceInputConfigurationError(tool);
                        if (!string.IsNullOrWhiteSpace(imageDifferenceError))
                        {
                            throw new InvalidOperationException(imageDifferenceError);
                        }
                        using (HImage imageA = CreateToolInputImage(tool, "ImageA", false))
                        using (HImage imageB = CreateToolInputImage(tool, "ImageB", false))
                        {
                            record = RunImageDifferenceTool(tool, imageA, imageB);
                            tool.InputSummary = GetImageInputSummary(tool, "ImageA") + " + " + GetImageInputSummary(tool, "ImageB");
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.DynamicThreshold:
                        string dynamicThresholdError = GetDynamicThresholdInputConfigurationError(tool);
                        if (!string.IsNullOrWhiteSpace(dynamicThresholdError))
                        {
                            throw new InvalidOperationException(dynamicThresholdError);
                        }
                        using (HImage originalImage = CreateToolInputImage(tool, "OriginalImage", false))
                        using (HImage referenceImage = CreateToolInputImage(tool, "ReferenceImage", false))
                        {
                            record = RunDynamicThresholdTool(tool, originalImage, referenceImage);
                            ApplyRecordImage(record, tool, originalImage);
                            tool.InputSummary = GetImageInputSummary(tool, "OriginalImage") + " + " + GetImageInputSummary(tool, "ReferenceImage");
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.ImageThreshold:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunImageThresholdTool(tool, inputImage);
                            ApplyRecordImage(record, tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.RegionMorphology:
                        using (HRegion inputRegion = CreateToolInputRegion(tool, "Region"))
                        {
                            record = RunRegionMorphologyTool(tool, inputRegion);
                            VmToolInputBindingData regionBinding = tool.GetInputBinding("Region");
                            VmToolInstance regionSource = GetInputSourceTool(regionBinding);
                            tool.InputSummary = regionSource == null || regionBinding == null
                                ? "Region ← 无效来源"
                                : regionSource.InstanceName + "." + regionBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.RegionFeatureFilter:
                        using (HRegion inputRegion = CreateToolInputRegion(tool, "Region"))
                        {
                            record = RunRegionFeatureFilterTool(tool, inputRegion);
                            VmToolInputBindingData regionBinding = tool.GetInputBinding("Region");
                            VmToolInstance regionSource = GetInputSourceTool(regionBinding);
                            tool.InputSummary = regionSource == null || regionBinding == null
                                ? "Region ← 无效来源"
                                : regionSource.InstanceName + "." + regionBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.RegionContourMeasurement:
                        using (HRegion inputRegion = CreateToolInputRegion(tool, "Region"))
                        {
                            record = RunRegionContourMeasurementTool(tool, inputRegion);
                            VmToolInputBindingData regionBinding = tool.GetInputBinding("Region");
                            VmToolInstance regionSource = GetInputSourceTool(regionBinding);
                            tool.InputSummary = regionSource == null || regionBinding == null
                                ? "Region ← 无效来源"
                                : regionSource.InstanceName + "." + regionBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.XldContourFilter:
                        using (HObject inputXld = CreateToolInputXld(tool, "XLD"))
                        {
                            record = RunXldContourFilterTool(tool, inputXld);
                            VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                            VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                            tool.InputSummary = xldSource == null || xldBinding == null
                                ? "XLD ← 无效来源"
                                : xldSource.InstanceName + "." + xldBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.XldContourSegmentation:
                        using (HObject inputXld = CreateToolInputXld(tool, "XLD"))
                        {
                            record = RunXldContourSegmentationTool(tool, inputXld);
                            VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                            VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                            tool.InputSummary = xldSource == null || xldBinding == null
                                ? "XLD ← 无效来源"
                                : xldSource.InstanceName + "." + xldBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.XldLineFitting:
                        using (HObject inputXld = CreateToolInputXld(tool, "XLD"))
                        {
                            record = RunXldLineFittingTool(tool, inputXld);
                            VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                            VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                            tool.InputSummary = xldSource == null || xldBinding == null
                                ? "XLD ← 无效来源"
                                : xldSource.InstanceName + "." + xldBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.XldCircleFitting:
                        using (HObject inputXld = CreateToolInputXld(tool, "XLD"))
                        {
                            record = RunXldCircleFittingTool(tool, inputXld);
                            VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                            VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                            tool.InputSummary = xldSource == null || xldBinding == null
                                ? "XLD ← 无效来源"
                                : xldSource.InstanceName + "." + xldBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.XldEllipseFitting:
                        using (HObject inputXld = CreateToolInputXld(tool, "XLD"))
                        {
                            record = RunXldEllipseFittingTool(tool, inputXld);
                            VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                            VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                            tool.InputSummary = xldSource == null || xldBinding == null
                                ? "XLD ← 无效来源"
                                : xldSource.InstanceName + "." + xldBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.XldRectangleFitting:
                        using (HObject inputXld = CreateToolInputXld(tool, "XLD"))
                        {
                            record = RunXldRectangleFittingTool(tool, inputXld);
                            VmToolInputBindingData xldBinding = tool.GetInputBinding("XLD");
                            VmToolInstance xldSource = GetInputSourceTool(xldBinding);
                            tool.InputSummary = xldSource == null || xldBinding == null
                                ? "XLD ← 无效来源"
                                : xldSource.InstanceName + "." + xldBinding.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.RegionSetOperation:
                        string regionSetError = GetRegionSetInputConfigurationError(tool);
                        if (!string.IsNullOrWhiteSpace(regionSetError))
                        {
                            throw new InvalidOperationException(regionSetError);
                        }
                        using (HRegion inputRegionA = CreateToolInputRegion(tool, "RegionA"))
                        using (HRegion inputRegionB = CreateToolInputRegion(tool, "RegionB"))
                        {
                            record = RunRegionSetOperationTool(tool, inputRegionA, inputRegionB);
                            VmToolInputBindingData bindingA = tool.GetInputBinding("RegionA");
                            VmToolInputBindingData bindingB = tool.GetInputBinding("RegionB");
                            VmToolInstance sourceA = GetInputSourceTool(bindingA);
                            VmToolInstance sourceB = GetInputSourceTool(bindingB);
                            tool.InputSummary = sourceA.InstanceName + "." + bindingA.SourcePortName + " + " + sourceB.InstanceName + "." + bindingB.SourcePortName;
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.ShapeMatch:
                        EnsureImage();
                        record = RunShapeMatchTool(tool, source);
                        tool.InputSummary = "Image + " + GetRoiBindingSummary(tool) + " + ShapeModel";
                        tool.OutputSummary = currentMatches.Count == 0
                            ? "Matches=0"
                            : string.Format(CultureInfo.InvariantCulture, "Matches={0}, Best={1:F3}", currentMatches.Count, currentMatches.Max(item => item.Score));
                        break;
                    case VmToolKind.Blob:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunBlobTool(tool, inputImage);
                            ApplyRecordImage(record, tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool) + " + " + GetRoiInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.GrayStat:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunGrayStatTool(tool, inputImage);
                            ApplyRecordImage(record, tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool) + " + " + GetRoiInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.EdgeMeasure:
                        using (HImage inputImage = CreateToolInputImage(tool))
                        {
                            record = RunEdgeMeasureTool(tool, inputImage);
                            ApplyRecordImage(record, tool, inputImage);
                            tool.InputSummary = GetImageInputSummary(tool) + " + " + GetRoiInputSummary(tool);
                            tool.OutputSummary = record.Message;
                        }
                        break;
                    case VmToolKind.HDevelop:
                        EnsureImage();
                        record = RunHDevTool(tool);
                        tool.InputSummary = "Image + " + GetRoiBindingSummary(tool) + " + HDevelop";
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.NumericJudge:
                        record = RunNumericJudgeTool(tool);
                        tool.OutputSummary = string.Format(
                            CultureInfo.InvariantCulture,
                            "Value={0:0.###}, Result={1}",
                            record.Score,
                            record.ResultCode);
                        break;
                    default:
                        throw new NotSupportedException("不支持的工具类型：" + tool.Kind);
                }

                PublishToolOutputs(tool, record);
                tool.ResultCode = record.ResultCode;
                tool.RunStatus = string.Equals(record.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) ? "完成" : "异常";
                if (tool.Kind == VmToolKind.ImageSource || tool.Kind == VmToolKind.ImageChannel || tool.Kind == VmToolKind.ImageFilter || tool.Kind == VmToolKind.ImageEnhancement || tool.Kind == VmToolKind.ImageDifference || tool.Kind == VmToolKind.DynamicThreshold || tool.Kind == VmToolKind.ImageThreshold || tool.Kind == VmToolKind.RegionMorphology || tool.Kind == VmToolKind.RegionFeatureFilter || tool.Kind == VmToolKind.RegionSetOperation || tool.Kind == VmToolKind.RegionContourMeasurement || tool.Kind == VmToolKind.XldContourFilter || tool.Kind == VmToolKind.XldContourSegmentation || tool.Kind == VmToolKind.XldLineFitting || tool.Kind == VmToolKind.XldCircleFitting || tool.Kind == VmToolKind.XldEllipseFitting || tool.Kind == VmToolKind.XldRectangleFitting)
                {
                    UpdateImageContextViewport(true);
                    ScheduleRefreshDisplay();
                }
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

        private InspectionRecord RunNumericJudgeTool(VmToolInstance tool)
        {
            string configurationError = GetNumericJudgeConfigurationError(tool);
            if (!string.IsNullOrWhiteSpace(configurationError))
            {
                throw new InvalidOperationException(configurationError);
            }

            VmToolInstance sourceTool = GetInputSourceTool(tool);
            double value;
            if (sourceTool == null || !sourceTool.TryGetNumericOutput(tool.InputPortName, out value))
            {
                throw new InvalidOperationException("上游端口尚无有效数值。请先运行上游工具，或按顺序运行全流程。");
            }

            bool passed = EvaluateNumericJudge(tool, value);
            string operatorText = NumericJudgeOperatorOption.GetDisplayText(tool.NumericOperator);
            string limits = GetNumericJudgeLimitText(tool);
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}={2:0.###}，规则={3} {4}，判定={5}",
                sourceTool.InstanceName,
                tool.InputPortName,
                value,
                operatorText,
                limits,
                passed ? "OK" : "NG");

            tool.InputSummary = sourceTool.InstanceName + "." + tool.InputPortName + " = " + value.ToString("0.###", CultureInfo.InvariantCulture);
            InspectionRecord record = CreateRecord("NumericJudge", passed ? "OK" : "NG", value, message, tool);
            resultStore.Add(record);
            LogInfo("数值判定完成：" + message);
            return record;
        }

        private InspectionRecord RunLocalImageSourceTool(VmToolInstance tool)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.ImageSource);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string path = Path.GetFullPath(parameters.LocalImagePath);
            string serialNumber = string.IsNullOrWhiteSpace(parameters.LocalImageSerialNumber)
                ? Path.GetFileNameWithoutExtension(path)
                : parameters.LocalImageSerialNumber.Trim();
            using (HImage image = imageService.ReadImage(path))
            {
                int width = GetImageWidth(image);
                int height = GetImageHeight(image);
                tool.SetOutputValue("Image", VmImageSnapshot.Create(image, tool.ToolId, tool.InstanceName, "Image", path, serialNumber));
                tool.SetOutputValue("SN", serialNumber);
                tool.SetOutputValue("Path", path);
                tool.SetOutputValue("Width", (double)width);
                tool.SetOutputValue("Height", (double)height);
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "已发布 {0}×{1} Image，SN={2}，文件={3}",
                    width,
                    height,
                    serialNumber,
                    Path.GetFileName(path));
                InspectionRecord record = CreateRecord("LocalImageSource", "OK", 1, message, tool);
                ApplyRecordImage(record, tool, image);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
        }

        private InspectionRecord RunImageChannelTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.ImageChannel);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            int inputChannels = imageService.GetChannelCount(inputImage);
            if (inputChannels <= 0)
            {
                throw new InvalidOperationException("输入图像没有可用通道。");
            }

            string mode = VmImageChannelMode.Normalize(parameters.ImageChannelMode);
            using (HImage outputImage = mode == VmImageChannelMode.ToGray
                ? imageService.ToGray(inputImage)
                : (mode == VmImageChannelMode.Extract
                    ? imageService.ExtractChannel(inputImage, parameters.ImageChannelIndex)
                    : inputImage.CopyImage()))
            {
                int outputChannels = imageService.GetChannelCount(outputImage);
                int width = GetImageWidth(outputImage);
                int height = GetImageHeight(outputImage);
                string sourcePath;
                string serialNumber;
                GetToolImageMetadata(tool, out sourcePath, out serialNumber);
                tool.SetOutputValue("Image", VmImageSnapshot.Create(outputImage, tool.ToolId, tool.InstanceName, "Image", sourcePath, serialNumber));
                tool.SetOutputValue("InputChannels", (double)inputChannels);
                tool.SetOutputValue("OutputChannels", (double)outputChannels);
                tool.SetOutputValue("Width", (double)width);
                tool.SetOutputValue("Height", (double)height);
                tool.SetOutputValue("Mode", VmImageChannelMode.GetDisplayText(mode));

                string modeDetail = mode == VmImageChannelMode.Extract
                    ? VmImageChannelMode.GetDisplayText(mode) + " " + parameters.ImageChannelIndex.ToString(CultureInfo.InvariantCulture)
                    : VmImageChannelMode.GetDisplayText(mode);
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}，通道 {1}→{2}，尺寸 {3}×{4}",
                    modeDetail,
                    inputChannels,
                    outputChannels,
                    width,
                    height);
                InspectionRecord record = CreateRecord("ImageChannel", "OK", outputChannels, message, tool);
                ApplyRecordImage(record, tool, outputImage);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
        }

        private InspectionRecord RunImageFilterTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.ImageFilter);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string mode = VmImageFilterMode.Normalize(parameters.ImageFilterMode);
            using (HImage outputImage = mode == VmImageFilterMode.Median
                ? imageService.MedianFilter(inputImage, parameters.ImageFilterRadius)
                : imageService.MeanFilter(inputImage, parameters.ImageFilterMaskWidth, parameters.ImageFilterMaskHeight))
            {
                int channels = imageService.GetChannelCount(outputImage);
                int width = GetImageWidth(outputImage);
                int height = GetImageHeight(outputImage);
                string sourcePath;
                string serialNumber;
                GetToolImageMetadata(tool, out sourcePath, out serialNumber);
                string kernel = mode == VmImageFilterMode.Median
                    ? "R=" + parameters.ImageFilterRadius.ToString(CultureInfo.InvariantCulture)
                    : parameters.ImageFilterMaskWidth.ToString(CultureInfo.InvariantCulture) + "×" + parameters.ImageFilterMaskHeight.ToString(CultureInfo.InvariantCulture);
                tool.SetOutputValue("Image", VmImageSnapshot.Create(outputImage, tool.ToolId, tool.InstanceName, "Image", sourcePath, serialNumber));
                tool.SetOutputValue("Width", (double)width);
                tool.SetOutputValue("Height", (double)height);
                tool.SetOutputValue("Channels", (double)channels);
                tool.SetOutputValue("Mode", VmImageFilterMode.GetDisplayText(mode));
                tool.SetOutputValue("Kernel", kernel);

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}，模板 {1}，通道 {2}，尺寸 {3}×{4}",
                    VmImageFilterMode.GetDisplayText(mode),
                    kernel,
                    channels,
                    width,
                    height);
                InspectionRecord record = CreateRecord("ImageFilter", "OK", channels, message, tool);
                ApplyRecordImage(record, tool, outputImage);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
        }

        private InspectionRecord RunImageEnhancementTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.ImageEnhancement);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string mode = VmImageEnhancementMode.Normalize(parameters.ImageEnhancementMode);
            using (HImage outputImage = mode == VmImageEnhancementMode.LocalContrast
                ? imageService.Emphasize(inputImage, parameters.ImageEnhancementMaskWidth, parameters.ImageEnhancementMaskHeight, parameters.ImageEnhancementFactor)
                : imageService.ScaleImage(inputImage, parameters.ImageEnhancementScale, parameters.ImageEnhancementOffset))
            {
                int channels = imageService.GetChannelCount(outputImage);
                int width = GetImageWidth(outputImage);
                int height = GetImageHeight(outputImage);
                string sourcePath;
                string serialNumber;
                GetToolImageMetadata(tool, out sourcePath, out serialNumber);
                string parameterSummary = mode == VmImageEnhancementMode.LocalContrast
                    ? string.Format(CultureInfo.InvariantCulture, "Mask={0}×{1}, Factor={2:0.###}", parameters.ImageEnhancementMaskWidth, parameters.ImageEnhancementMaskHeight, parameters.ImageEnhancementFactor)
                    : string.Format(CultureInfo.InvariantCulture, "Scale={0:0.###}, Offset={1:0.###}", parameters.ImageEnhancementScale, parameters.ImageEnhancementOffset);
                tool.SetOutputValue("Image", VmImageSnapshot.Create(outputImage, tool.ToolId, tool.InstanceName, "Image", sourcePath, serialNumber));
                tool.SetOutputValue("Width", (double)width);
                tool.SetOutputValue("Height", (double)height);
                tool.SetOutputValue("Channels", (double)channels);
                tool.SetOutputValue("Mode", VmImageEnhancementMode.GetDisplayText(mode));
                tool.SetOutputValue("Parameters", parameterSummary);

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}，{1}，通道 {2}，尺寸 {3}×{4}",
                    VmImageEnhancementMode.GetDisplayText(mode),
                    parameterSummary,
                    channels,
                    width,
                    height);
                InspectionRecord record = CreateRecord("ImageEnhancement", "OK", channels, message, tool);
                ApplyRecordImage(record, tool, outputImage);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
        }

        private InspectionRecord RunImageDifferenceTool(VmToolInstance tool, HImage imageA, HImage imageB)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.ImageDifference);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            int channelsA = imageService.GetChannelCount(imageA);
            int channelsB = imageService.GetChannelCount(imageB);
            if (channelsA <= 0 || channelsB <= 0)
            {
                throw new InvalidOperationException("双图像差分输入没有可用通道。");
            }
            if (channelsA != channelsB)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "双图像差分两路通道数不匹配：ImageA={0}，ImageB={1}。请先用图像通道工具统一通道。",
                    channelsA,
                    channelsB));
            }

            int widthA = GetImageWidth(imageA);
            int heightA = GetImageHeight(imageA);
            int widthB = GetImageWidth(imageB);
            int heightB = GetImageHeight(imageB);
            if (widthA != widthB || heightA != heightB)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "双图像差分两路尺寸不匹配：ImageA={0}×{1}，ImageB={2}×{3}。",
                    widthA,
                    heightA,
                    widthB,
                    heightB));
            }

            string mode = VmImageDifferenceMode.Normalize(parameters.ImageDifferenceMode);
            using (HImage outputImage = mode == VmImageDifferenceMode.AbsoluteDifference
                ? imageService.AbsoluteDifference(imageA, imageB, parameters.ImageDifferenceMultiplier)
                : (mode == VmImageDifferenceMode.SubtractBA
                    ? imageService.SubtractImage(imageB, imageA, parameters.ImageDifferenceMultiplier, parameters.ImageDifferenceAdd)
                    : imageService.SubtractImage(imageA, imageB, parameters.ImageDifferenceMultiplier, parameters.ImageDifferenceAdd)))
            {
                int outputChannels = imageService.GetChannelCount(outputImage);
                int outputWidth = GetImageWidth(outputImage);
                int outputHeight = GetImageHeight(outputImage);
                string sourcePath;
                string serialNumber;
                GetToolImageMetadata(tool, out sourcePath, out serialNumber);
                string parameterSummary = mode == VmImageDifferenceMode.AbsoluteDifference
                    ? string.Format(CultureInfo.InvariantCulture, "Mult={0:0.###}", parameters.ImageDifferenceMultiplier)
                    : string.Format(CultureInfo.InvariantCulture, "Mult={0:0.###}, Add={1:0.###}", parameters.ImageDifferenceMultiplier, parameters.ImageDifferenceAdd);
                tool.SetOutputValue("Image", VmImageSnapshot.Create(outputImage, tool.ToolId, tool.InstanceName, "Image", sourcePath, serialNumber));
                tool.SetOutputValue("Width", (double)outputWidth);
                tool.SetOutputValue("Height", (double)outputHeight);
                tool.SetOutputValue("Channels", (double)outputChannels);
                tool.SetOutputValue("Mode", VmImageDifferenceMode.GetDisplayText(mode));
                tool.SetOutputValue("Parameters", parameterSummary);

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}，{1}，通道 {2}，尺寸 {3}×{4}",
                    VmImageDifferenceMode.GetDisplayText(mode),
                    parameterSummary,
                    outputChannels,
                    outputWidth,
                    outputHeight);
                InspectionRecord record = CreateRecord("ImageDifference", "OK", outputChannels, message, tool);
                ApplyRecordImage(record, tool, outputImage);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
        }

        private InspectionRecord RunDynamicThresholdTool(VmToolInstance tool, HImage originalImage, HImage referenceImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.DynamicThreshold);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            int originalChannels = imageService.GetChannelCount(originalImage);
            int referenceChannels = imageService.GetChannelCount(referenceImage);
            if (originalChannels != referenceChannels)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "动态阈值两路图像通道不匹配：原图 {0} 通道，参考图 {1} 通道。请先用图像通道工具转为单通道。",
                    originalChannels,
                    referenceChannels));
            }
            if (originalChannels != 1)
            {
                throw new InvalidOperationException("HALCON dyn_threshold 要求两路输入均为单通道图像；请先用图像通道工具转灰或提取通道。");
            }

            int originalWidth = GetImageWidth(originalImage);
            int originalHeight = GetImageHeight(originalImage);
            int referenceWidth = GetImageWidth(referenceImage);
            int referenceHeight = GetImageHeight(referenceImage);
            if (originalWidth != referenceWidth || originalHeight != referenceHeight)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "动态阈值两路图像尺寸不匹配：原图 {0}×{1}，参考图 {2}×{3}。",
                    originalWidth,
                    originalHeight,
                    referenceWidth,
                    referenceHeight));
            }

            string polarity = VmDynamicThresholdPolarity.Normalize(parameters.DynamicThresholdPolarity);
            HRegion thresholdRegion = null;
            HObject connectedRegion = null;
            try
            {
                thresholdRegion = imageService.DynamicThreshold(
                    originalImage,
                    referenceImage,
                    parameters.DynamicThresholdOffset,
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
                tool.SetOutputValue("Region", VmRegionSnapshot.Create(connectedRegion, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("Polarity", polarity);
                tool.SetOutputValue("Offset", parameters.DynamicThresholdOffset);
                ReplaceToolOverlayRegion(connectedRegion, tool, "Region", regionCount, totalArea, resultCode);
                connectedRegion = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON dyn_threshold {0}，Offset={1:0.###}，Region={2}，Area={3:0.###}",
                    VmDynamicThresholdPolarity.GetDisplayText(polarity),
                    parameters.DynamicThresholdOffset,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("DynamicThreshold", resultCode, totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(connectedRegion);
                if (thresholdRegion != null)
                {
                    thresholdRegion.Dispose();
                }
            }
        }

        private InspectionRecord RunImageThresholdTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.ImageThreshold);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            HImage gray = null;
            HObject thresholdRegion = null;
            HObject connectedRegion = null;
            try
            {
                int inputChannels = imageService.GetChannelCount(inputImage);
                if (inputChannels <= 0)
                {
                    throw new InvalidOperationException("输入图像没有可用通道。");
                }

                gray = CreateGrayImage(inputImage);
                HOperatorSet.Threshold(gray, out thresholdRegion, parameters.ImageThresholdMinGray, parameters.ImageThresholdMaxGray);
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
                string grayConvention = inputChannels > 1 ? "rgb1_to_gray" : "单通道保持";
                string thresholdText = string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0:0.###}, {1:0.###}] / {2}",
                    parameters.ImageThresholdMinGray,
                    parameters.ImageThresholdMaxGray,
                    grayConvention);

                tool.SetOutputValue("Region", VmRegionSnapshot.Create(connectedRegion, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("Threshold", thresholdText);
                ReplaceToolOverlayRegion(connectedRegion, tool, "Region", regionCount, totalArea, regionCount > 0 && totalArea > 0 ? "OK" : "NG");
                connectedRegion = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON threshold {0}，Region={1}，Area={2:0.###}",
                    thresholdText,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("ImageThreshold", regionCount > 0 && totalArea > 0 ? "OK" : "NG", totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(connectedRegion);
                DisposeObject(thresholdRegion);
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private InspectionRecord RunRegionMorphologyTool(VmToolInstance tool, HRegion inputRegion)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.RegionMorphology);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string mode = VmRegionMorphologyMode.Normalize(parameters.RegionMorphologyMode);
            HObject morphologyRegion = null;
            HObject unionRegion = null;
            HObject connectedRegion = null;
            try
            {
                if (mode == VmRegionMorphologyMode.ClosingCircle)
                {
                    HOperatorSet.ClosingCircle(inputRegion, out morphologyRegion, parameters.RegionMorphologyRadius);
                }
                else if (mode == VmRegionMorphologyMode.DilationCircle)
                {
                    HOperatorSet.DilationCircle(inputRegion, out morphologyRegion, parameters.RegionMorphologyRadius);
                }
                else if (mode == VmRegionMorphologyMode.ErosionCircle)
                {
                    HOperatorSet.ErosionCircle(inputRegion, out morphologyRegion, parameters.RegionMorphologyRadius);
                }
                else
                {
                    HOperatorSet.OpeningCircle(inputRegion, out morphologyRegion, parameters.RegionMorphologyRadius);
                }

                HOperatorSet.Union1(morphologyRegion, out unionRegion);
                HOperatorSet.Connection(unionRegion, out connectedRegion);

                HTuple count;
                HTuple area;
                HTuple row;
                HTuple column;
                HOperatorSet.CountObj(connectedRegion, out count);
                HOperatorSet.AreaCenter(connectedRegion, out area, out row, out column);
                double totalArea = SumTuple(area);
                int regionCount = totalArea <= 0 || count == null || count.Length == 0 ? 0 : count.I;
                if (totalArea <= 0)
                {
                    DisposeObject(connectedRegion);
                    HOperatorSet.GenEmptyObj(out connectedRegion);
                }

                string operation = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} / R={1:0.###}px",
                    VmRegionMorphologyMode.GetDisplayText(mode),
                    parameters.RegionMorphologyRadius);
                tool.SetOutputValue("Region", VmRegionSnapshot.Create(connectedRegion, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("Operation", operation);
                ReplaceToolOverlayRegion(connectedRegion, tool, "Region", regionCount, totalArea, regionCount > 0 && totalArea > 0 ? "OK" : "NG");
                connectedRegion = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}，Region={1}，Area={2:0.###}",
                    operation,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("RegionMorphology", regionCount > 0 && totalArea > 0 ? "OK" : "NG", totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(connectedRegion);
                DisposeObject(unionRegion);
                DisposeObject(morphologyRegion);
            }
        }

        private InspectionRecord RunRegionFeatureFilterTool(VmToolInstance tool, HRegion inputRegion)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.RegionFeatureFilter);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string feature = VmRegionFeature.Normalize(parameters.RegionFeature);
            HObject connectedRegion = null;
            HObject selectedRegion = null;
            try
            {
                HOperatorSet.Connection(inputRegion, out connectedRegion);
                HOperatorSet.SelectShape(
                    connectedRegion,
                    out selectedRegion,
                    VmRegionFeature.GetHalconFeatureName(feature),
                    "and",
                    parameters.RegionFeatureMin,
                    parameters.RegionFeatureMax);

                HTuple count;
                HOperatorSet.CountObj(selectedRegion, out count);
                int regionCount = count == null || count.Length == 0 ? 0 : count.I;
                double totalArea = 0;
                if (regionCount > 0)
                {
                    HTuple area;
                    HTuple row;
                    HTuple column;
                    HOperatorSet.AreaCenter(selectedRegion, out area, out row, out column);
                    totalArea = SumTuple(area);
                }

                string unit = VmRegionFeature.GetUnit(feature);
                string range = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.###}..{1:0.###} {2}",
                    parameters.RegionFeatureMin,
                    parameters.RegionFeatureMax,
                    unit);
                string featureText = VmRegionFeature.GetDisplayText(feature);
                string resultCode = regionCount > 0 && totalArea > 0 ? "OK" : "NG";
                tool.SetOutputValue("Region", VmRegionSnapshot.Create(selectedRegion, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("Feature", featureText);
                tool.SetOutputValue("Range", range);
                ReplaceToolOverlayRegion(selectedRegion, tool, "Region", regionCount, totalArea, resultCode);
                selectedRegion = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}筛选 {1}，Region={2}，Area={3:0.###}",
                    featureText,
                    range,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("RegionFeatureFilter", resultCode, totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(selectedRegion);
                DisposeObject(connectedRegion);
            }
        }

        private InspectionRecord RunRegionSetOperationTool(VmToolInstance tool, HRegion inputRegionA, HRegion inputRegionB)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.RegionSetOperation);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string mode = VmRegionSetOperationMode.Normalize(parameters.RegionSetOperationMode);
            HObject unionA = null;
            HObject unionB = null;
            HObject setResult = null;
            HObject connectedResult = null;
            try
            {
                HOperatorSet.Union1(inputRegionA, out unionA);
                HOperatorSet.Union1(inputRegionB, out unionB);
                if (mode == VmRegionSetOperationMode.Intersection)
                {
                    HOperatorSet.Intersection(unionA, unionB, out setResult);
                }
                else if (mode == VmRegionSetOperationMode.Difference)
                {
                    HOperatorSet.Difference(unionA, unionB, out setResult);
                }
                else if (mode == VmRegionSetOperationMode.SymmetricDifference)
                {
                    HOperatorSet.SymmDifference(unionA, unionB, out setResult);
                }
                else
                {
                    HOperatorSet.Union2(unionA, unionB, out setResult);
                }

                HOperatorSet.Connection(setResult, out connectedResult);
                HTuple count;
                HTuple area;
                HTuple row;
                HTuple column;
                HOperatorSet.CountObj(connectedResult, out count);
                HOperatorSet.AreaCenter(connectedResult, out area, out row, out column);
                double totalArea = SumTuple(area);
                int regionCount = totalArea <= 0 || count == null || count.Length == 0 ? 0 : count.I;
                if (totalArea <= 0)
                {
                    DisposeObject(connectedResult);
                    HOperatorSet.GenEmptyObj(out connectedResult);
                }

                string operation = VmRegionSetOperationMode.GetDisplayText(mode);
                string resultCode = regionCount > 0 && totalArea > 0 ? "OK" : "NG";
                tool.SetOutputValue("Region", VmRegionSnapshot.Create(connectedResult, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("Operation", operation);
                ReplaceToolOverlayRegion(connectedResult, tool, "Region", regionCount, totalArea, resultCode);
                connectedResult = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON {0}，Region={1}，Area={2:0.###}",
                    operation,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("RegionSetOperation", resultCode, totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(connectedResult);
                DisposeObject(setResult);
                DisposeObject(unionB);
                DisposeObject(unionA);
            }
        }

        private InspectionRecord RunRegionContourMeasurementTool(VmToolInstance tool, HRegion inputRegion)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.RegionContourMeasurement);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string mode = VmRegionContourMode.Normalize(parameters.RegionContourMode);
            HObject contours = null;
            try
            {
                HTuple regionArea;
                HTuple row;
                HTuple column;
                HOperatorSet.AreaCenter(inputRegion, out regionArea, out row, out column);
                double inputArea = SumTuple(regionArea);
                if (inputArea > 0)
                {
                    contours = imageService.GenerateRegionContours(inputRegion, mode);
                }
                else
                {
                    HOperatorSet.GenEmptyObj(out contours);
                }

                HTuple count;
                HOperatorSet.CountObj(contours, out count);
                int contourCount = count == null || count.Length == 0 ? 0 : count.I;
                double totalLength = 0;
                if (contourCount > 0)
                {
                    HTuple lengths;
                    HOperatorSet.LengthXld(contours, out lengths);
                    totalLength = SumTuple(lengths);
                }

                string modeText = VmRegionContourMode.GetDisplayText(mode);
                string resultCode = contourCount > 0 && totalLength > 0 ? "OK" : "NG";
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(contours, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("TotalLength", totalLength);
                tool.SetOutputValue("ContourCount", (double)contourCount);
                tool.SetOutputValue("Mode", modeText);
                ReplaceToolOverlayContours(contours, tool, "XLD", contourCount, resultCode, totalLength);
                contours = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON Region 轮廓测量：{0}，XLD={1}，总长度={2:0.###}",
                    modeText,
                    contourCount,
                    totalLength);
                InspectionRecord record = CreateRecord("RegionContourMeasurement", resultCode, totalLength, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(contours);
            }
        }

        private InspectionRecord RunXldContourFilterTool(VmToolInstance tool, HObject inputXld)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.XldContourFilter);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string feature = VmXldContourFeature.Normalize(parameters.XldContourFeature);
            HObject selectedContours = null;
            try
            {
                selectedContours = imageService.FilterXldContours(inputXld, feature, parameters.XldContourMin, parameters.XldContourMax);
                HTuple count;
                HOperatorSet.CountObj(selectedContours, out count);
                int contourCount = count == null || count.Length == 0 ? 0 : count.I;
                double totalLength = 0;
                if (contourCount > 0)
                {
                    HTuple lengths;
                    HOperatorSet.LengthXld(selectedContours, out lengths);
                    totalLength = SumTuple(lengths);
                }

                string featureText = VmXldContourFeature.GetDisplayText(feature);
                string range = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.###}..{1:0.###} px",
                    parameters.XldContourMin,
                    parameters.XldContourMax);
                string resultCode = contourCount > 0 && totalLength > 0 ? "OK" : "NG";
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(selectedContours, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("TotalLength", totalLength);
                tool.SetOutputValue("ContourCount", (double)contourCount);
                tool.SetOutputValue("Feature", featureText);
                tool.SetOutputValue("Range", range);
                ReplaceToolOverlayContours(selectedContours, tool, "XLD", contourCount, resultCode, totalLength);
                selectedContours = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON XLD {0}筛选 {1}，XLD={2}，总长度={3:0.###}",
                    featureText,
                    range,
                    contourCount,
                    totalLength);
                InspectionRecord record = CreateRecord("XldContourFilter", resultCode, totalLength, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(selectedContours);
            }
        }

        private InspectionRecord RunXldContourSegmentationTool(VmToolInstance tool, HObject inputXld)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.XldContourSegmentation);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            string mode = VmXldSegmentMode.Normalize(parameters.XldSegmentMode);
            HObject segmentedContours = null;
            try
            {
                segmentedContours = imageService.SegmentXldContours(
                    inputXld,
                    mode,
                    parameters.XldSegmentSmooth,
                    parameters.XldSegmentMaxLineDistance1,
                    parameters.XldSegmentMaxLineDistance2);
                HTuple count;
                HOperatorSet.CountObj(segmentedContours, out count);
                int segmentCount = count == null || count.Length == 0 ? 0 : count.I;
                double totalLength = 0;
                if (segmentCount > 0)
                {
                    HTuple lengths;
                    HOperatorSet.LengthXld(segmentedContours, out lengths);
                    totalLength = SumTuple(lengths);
                }

                int lineCount = 0;
                int arcCount = 0;
                for (int index = 1; index <= segmentCount; index++)
                {
                    HObject segment = null;
                    try
                    {
                        HOperatorSet.SelectObj(segmentedContours, out segment, index);
                        HTuple approximation;
                        HOperatorSet.GetContourGlobalAttribXld(segment, "cont_approx", out approximation);
                        if (approximation != null && approximation.Length > 0)
                        {
                            double value = approximation[0].D;
                            if (Math.Abs(value + 1) < 0.001) lineCount++;
                            else if (Math.Abs(value) < 0.001 || Math.Abs(value - 1) < 0.001) arcCount++;
                        }
                    }
                    catch (HOperatorException)
                    {
                        // HALCON may copy very short contours without a usable global attribute.
                    }
                    finally
                    {
                        DisposeObject(segment);
                    }
                }

                string modeText = VmXldSegmentMode.GetDisplayText(mode);
                string parameterText = string.Format(
                    CultureInfo.InvariantCulture,
                    "Smooth={0}, MaxLineDist1={1:0.###}, MaxLineDist2={2:0.###}",
                    parameters.XldSegmentSmooth,
                    parameters.XldSegmentMaxLineDistance1,
                    parameters.XldSegmentMaxLineDistance2);
                string resultCode = segmentCount > 0 && totalLength > 0 ? "OK" : "NG";
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(segmentedContours, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("TotalLength", totalLength);
                tool.SetOutputValue("SegmentCount", (double)segmentCount);
                tool.SetOutputValue("LineCount", (double)lineCount);
                tool.SetOutputValue("ArcCount", (double)arcCount);
                tool.SetOutputValue("Mode", modeText);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(segmentedContours, tool, "XLD", segmentCount, resultCode, totalLength);
                segmentedContours = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON XLD 轮廓分段：{0}，段={1}（直线={2}，弧={3}），总长度={4:0.###}；{5}",
                    modeText,
                    segmentCount,
                    lineCount,
                    arcCount,
                    totalLength,
                    parameterText);
                InspectionRecord record = CreateRecord("XldContourSegmentation", resultCode, totalLength, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(segmentedContours);
            }
        }

        private InspectionRecord RunXldLineFittingTool(VmToolInstance tool, HObject inputXld)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.XldLineFitting);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);

            HTuple inputCountTuple;
            HOperatorSet.CountObj(inputXld, out inputCountTuple);
            int inputCount = inputCountTuple == null || inputCountTuple.Length == 0 ? 0 : inputCountTuple.I;
            for (int index = 1; index <= inputCount; index++)
            {
                HObject contour = null;
                try
                {
                    HOperatorSet.SelectObj(inputXld, out contour, index);
                    HTuple rows;
                    HTuple columns;
                    HOperatorSet.GetContourXld(contour, out rows, out columns);
                    if (rows == null || rows.Length < 2)
                    {
                        throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 少于 2 个点，不能拟合直线。");
                    }

                    try
                    {
                        HTuple approximation;
                        HOperatorSet.GetContourGlobalAttribXld(contour, "cont_approx", out approximation);
                        if (approximation != null && approximation.Length > 0 && Math.Abs(approximation[0].D + 1) > 0.001)
                        {
                            throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是圆弧或椭圆弧；请先用“XLD 轮廓分段”筛出直线段。");
                        }
                    }
                    catch (HOperatorException)
                    {
                        // 原始多边形 XLD 可能没有 cont_approx；仍允许按实际点拟合。
                    }
                }
                finally
                {
                    DisposeObject(contour);
                }
            }

            string algorithm = VmXldLineFitAlgorithm.Normalize(parameters.XldLineFitAlgorithm);
            string algorithmText = VmXldLineFitAlgorithm.GetDisplayText(algorithm);
            string parameterText = string.Format(
                CultureInfo.InvariantCulture,
                "MaxPoints={0}, ClipEnds={1}, Iterations={2}, ClipFactor={3:0.###}",
                parameters.XldLineFitMaxNumPoints,
                parameters.XldLineFitClippingEndPoints,
                parameters.XldLineFitIterations,
                parameters.XldLineFitClippingFactor);

            HObject fittedLines = null;
            HTuple rowBegin = null;
            HTuple columnBegin = null;
            HTuple rowEnd = null;
            HTuple columnEnd = null;
            HTuple normalRow = null;
            HTuple normalColumn = null;
            HTuple distance = null;
            try
            {
                if (inputCount == 0)
                {
                    HOperatorSet.GenEmptyObj(out fittedLines);
                    rowBegin = columnBegin = rowEnd = columnEnd = normalRow = normalColumn = distance = new HTuple();
                }
                else
                {
                    fittedLines = imageService.FitXldLines(
                        inputXld,
                        algorithm,
                        parameters.XldLineFitMaxNumPoints,
                        parameters.XldLineFitClippingEndPoints,
                        parameters.XldLineFitIterations,
                        parameters.XldLineFitClippingFactor,
                        out rowBegin,
                        out columnBegin,
                        out rowEnd,
                        out columnEnd,
                        out normalRow,
                        out normalColumn,
                        out distance);
                }

                int lineCount = rowBegin == null ? 0 : rowBegin.Length;
                double totalLength = 0;
                double maxResidual = 0;
                double weightedCos = 0;
                double weightedSin = 0;
                double primaryLength = -1;
                double primaryAngle = 0;
                double startRow = 0;
                double startColumn = 0;
                double endRow = 0;
                double endColumn = 0;
                for (int index = 0; index < lineCount; index++)
                {
                    double rb = rowBegin[index].D;
                    double cb = columnBegin[index].D;
                    double re = rowEnd[index].D;
                    double ce = columnEnd[index].D;
                    double length = Math.Sqrt((re - rb) * (re - rb) + (ce - cb) * (ce - cb));
                    double angle = Math.Atan2(re - rb, ce - cb);
                    totalLength += length;
                    weightedCos += Math.Cos(2 * angle) * length;
                    weightedSin += Math.Sin(2 * angle) * length;
                    if (length > primaryLength)
                    {
                        primaryLength = length;
                        primaryAngle = angle;
                        startRow = rb;
                        startColumn = cb;
                        endRow = re;
                        endColumn = ce;
                    }

                    if (index < inputCount)
                    {
                        HObject sourceContour = null;
                        try
                        {
                            HOperatorSet.SelectObj(inputXld, out sourceContour, index + 1);
                            HTuple rows;
                            HTuple columns;
                            HOperatorSet.GetContourXld(sourceContour, out rows, out columns);
                            double nr = normalRow[index].D;
                            double nc = normalColumn[index].D;
                            double d = distance[index].D;
                            for (int pointIndex = 0; pointIndex < rows.Length; pointIndex++)
                            {
                                maxResidual = Math.Max(maxResidual, Math.Abs(rows[pointIndex].D * nr + columns[pointIndex].D * nc - d));
                            }
                        }
                        finally
                        {
                            DisposeObject(sourceContour);
                        }
                    }
                }

                double meanAngleRadians = Math.Abs(weightedCos) + Math.Abs(weightedSin) < 1e-9
                    ? primaryAngle
                    : 0.5 * Math.Atan2(weightedSin, weightedCos);
                double meanAngle = meanAngleRadians * 180.0 / Math.PI;
                while (meanAngle >= 90) meanAngle -= 180;
                while (meanAngle < -90) meanAngle += 180;
                string resultCode = lineCount > 0 && totalLength > 0 ? "OK" : "NG";

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(fittedLines, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("LineCount", (double)lineCount);
                tool.SetOutputValue("TotalLength", totalLength);
                tool.SetOutputValue("MeanAngle", meanAngle);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("StartRow", startRow);
                tool.SetOutputValue("StartColumn", startColumn);
                tool.SetOutputValue("EndRow", endRow);
                tool.SetOutputValue("EndColumn", endColumn);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(fittedLines, tool, "XLD", lineCount, resultCode, totalLength);
                fittedLines = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON XLD 直线拟合：{0}，直线={1}，总长度={2:0.###}px，平均角={3:0.###}°，最大残差={4:0.###}px，主线 ({5:0.###},{6:0.###})→({7:0.###},{8:0.###})；{9}",
                    algorithmText,
                    lineCount,
                    totalLength,
                    meanAngle,
                    maxResidual,
                    startRow,
                    startColumn,
                    endRow,
                    endColumn,
                    parameterText);
                InspectionRecord record = CreateRecord("XldLineFitting", resultCode, totalLength, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(fittedLines);
            }
        }

        private InspectionRecord RunXldCircleFittingTool(VmToolInstance tool, HObject inputXld)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.XldCircleFitting);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);

            HTuple inputCountTuple;
            HOperatorSet.CountObj(inputXld, out inputCountTuple);
            int inputCount = inputCountTuple == null || inputCountTuple.Length == 0 ? 0 : inputCountTuple.I;
            List<double> contourDiagonals = new List<double>();
            for (int index = 1; index <= inputCount; index++)
            {
                HObject contour = null;
                try
                {
                    HOperatorSet.SelectObj(inputXld, out contour, index);
                    HTuple rows;
                    HTuple columns;
                    HOperatorSet.GetContourXld(contour, out rows, out columns);
                    if (rows == null || rows.Length < 3)
                    {
                        throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 少于 3 个点，不能拟合圆。");
                    }

                    double minRow = rows[0].D;
                    double maxRow = minRow;
                    double minColumn = columns[0].D;
                    double maxColumn = minColumn;
                    for (int pointIndex = 1; pointIndex < rows.Length; pointIndex++)
                    {
                        minRow = Math.Min(minRow, rows[pointIndex].D);
                        maxRow = Math.Max(maxRow, rows[pointIndex].D);
                        minColumn = Math.Min(minColumn, columns[pointIndex].D);
                        maxColumn = Math.Max(maxColumn, columns[pointIndex].D);
                    }
                    contourDiagonals.Add(Math.Sqrt((maxRow - minRow) * (maxRow - minRow) + (maxColumn - minColumn) * (maxColumn - minColumn)));

                    try
                    {
                        HTuple approximation;
                        HOperatorSet.GetContourGlobalAttribXld(contour, "cont_approx", out approximation);
                        if (approximation != null && approximation.Length > 0 && Math.Abs(approximation[0].D + 1) < 0.001)
                        {
                            throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是直线段；请从“XLD 轮廓分段”选择圆弧候选。");
                        }
                    }
                    catch (HOperatorException)
                    {
                        // 原始整圆或圆弧 XLD 可能没有 cont_approx；继续按实际点拟合并执行退化检查。
                    }
                }
                finally
                {
                    DisposeObject(contour);
                }
            }

            string algorithm = VmXldCircleFitAlgorithm.Normalize(parameters.XldCircleFitAlgorithm);
            string algorithmText = VmXldCircleFitAlgorithm.GetDisplayText(algorithm);
            string parameterText = string.Format(
                CultureInfo.InvariantCulture,
                "MaxPoints={0}, MaxClosure={1:0.###}, ClipEnds={2}, Iterations={3}, ClipFactor={4:0.###}",
                parameters.XldCircleFitMaxNumPoints,
                parameters.XldCircleFitMaxClosureDistance,
                parameters.XldCircleFitClippingEndPoints,
                parameters.XldCircleFitIterations,
                parameters.XldCircleFitClippingFactor);

            HObject fittedCircles = null;
            HTuple rowsFit = null;
            HTuple columnsFit = null;
            HTuple radii = null;
            HTuple startPhis = null;
            HTuple endPhis = null;
            HTuple pointOrders = null;
            try
            {
                if (inputCount == 0)
                {
                    HOperatorSet.GenEmptyObj(out fittedCircles);
                    rowsFit = columnsFit = radii = startPhis = endPhis = pointOrders = new HTuple();
                }
                else
                {
                    fittedCircles = imageService.FitXldCircles(
                        inputXld,
                        algorithm,
                        parameters.XldCircleFitMaxNumPoints,
                        parameters.XldCircleFitMaxClosureDistance,
                        parameters.XldCircleFitClippingEndPoints,
                        parameters.XldCircleFitIterations,
                        parameters.XldCircleFitClippingFactor,
                        out rowsFit,
                        out columnsFit,
                        out radii,
                        out startPhis,
                        out endPhis,
                        out pointOrders);
                }

                int circleCount = rowsFit == null ? 0 : rowsFit.Length;
                double maxResidual = 0;
                double primaryArcLength = -1;
                double centerRow = 0;
                double centerColumn = 0;
                double radius = 0;
                double startPhi = 0;
                double endPhi = 0;
                string pointOrder = "positive";
                for (int index = 0; index < circleCount; index++)
                {
                    double currentRadius = radii[index].D;
                    if (double.IsNaN(currentRadius) || double.IsInfinity(currentRadius) || currentRadius <= 0)
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓得到无效半径，输入可能退化或不是圆弧。");
                    }
                    double diagonal = index < contourDiagonals.Count ? contourDiagonals[index] : 0;
                    if (diagonal <= 1e-6 || currentRadius > Math.Max(10, diagonal * 20))
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓接近直线或退化，拟合半径相对轮廓跨度过大。");
                    }

                    double span = Math.Abs(endPhis[index].D - startPhis[index].D);
                    if (span > Math.PI * 2) span = Math.PI * 2;
                    double arcLength = currentRadius * span;
                    if (arcLength > primaryArcLength)
                    {
                        primaryArcLength = arcLength;
                        centerRow = rowsFit[index].D;
                        centerColumn = columnsFit[index].D;
                        radius = currentRadius;
                        startPhi = startPhis[index].D;
                        endPhi = endPhis[index].D;
                        pointOrder = pointOrders[index].S;
                    }

                    if (index < inputCount)
                    {
                        HObject sourceContour = null;
                        try
                        {
                            HOperatorSet.SelectObj(inputXld, out sourceContour, index + 1);
                            HTuple rows;
                            HTuple columns;
                            HOperatorSet.GetContourXld(sourceContour, out rows, out columns);
                            for (int pointIndex = 0; pointIndex < rows.Length; pointIndex++)
                            {
                                double dr = rows[pointIndex].D - rowsFit[index].D;
                                double dc = columns[pointIndex].D - columnsFit[index].D;
                                maxResidual = Math.Max(maxResidual, Math.Abs(Math.Sqrt(dr * dr + dc * dc) - currentRadius));
                            }
                        }
                        finally
                        {
                            DisposeObject(sourceContour);
                        }
                    }
                }

                string resultCode = circleCount > 0 && radius > 0 ? "OK" : "NG";
                string pointOrderText = string.Equals(pointOrder, "negative", StringComparison.OrdinalIgnoreCase) ? "negative（反向）" : "positive（正向）";
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(fittedCircles, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("CircleCount", (double)circleCount);
                tool.SetOutputValue("CenterRow", centerRow);
                tool.SetOutputValue("CenterColumn", centerColumn);
                tool.SetOutputValue("Radius", radius);
                tool.SetOutputValue("StartPhi", startPhi);
                tool.SetOutputValue("EndPhi", endPhi);
                tool.SetOutputValue("PointOrder", pointOrderText);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(fittedCircles, tool, "XLD", circleCount, resultCode, radius);
                fittedCircles = null;

                string message = circleCount == 0
                    ? "HALCON XLD 圆拟合：输入为空，圆=0；" + parameterText
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "HALCON XLD 圆拟合：{0}，圆={1}，主圆心=({2:0.###},{3:0.###})，半径={4:0.###}px，角度={5:0.###}°→{6:0.###}°，点序={7}，最大径向残差={8:0.###}px；{9}",
                        algorithmText,
                        circleCount,
                        centerRow,
                        centerColumn,
                        radius,
                        startPhi * 180.0 / Math.PI,
                        endPhi * 180.0 / Math.PI,
                        pointOrderText,
                        maxResidual,
                        parameterText);
                InspectionRecord record = CreateRecord("XldCircleFitting", resultCode, radius, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(fittedCircles);
            }
        }

        private InspectionRecord RunXldEllipseFittingTool(VmToolInstance tool, HObject inputXld)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.XldEllipseFitting);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);

            HTuple inputCountTuple;
            HOperatorSet.CountObj(inputXld, out inputCountTuple);
            int inputCount = inputCountTuple == null || inputCountTuple.Length == 0 ? 0 : inputCountTuple.I;
            List<double> contourDiagonals = new List<double>();
            for (int index = 1; index <= inputCount; index++)
            {
                HObject contour = null;
                try
                {
                    HOperatorSet.SelectObj(inputXld, out contour, index);
                    HTuple rows;
                    HTuple columns;
                    HOperatorSet.GetContourXld(contour, out rows, out columns);
                    if (rows == null || rows.Length < 5)
                    {
                        throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 少于 5 个点，不能拟合椭圆。");
                    }

                    double minRow = rows[0].D;
                    double maxRow = minRow;
                    double minColumn = columns[0].D;
                    double maxColumn = minColumn;
                    for (int pointIndex = 1; pointIndex < rows.Length; pointIndex++)
                    {
                        minRow = Math.Min(minRow, rows[pointIndex].D);
                        maxRow = Math.Max(maxRow, rows[pointIndex].D);
                        minColumn = Math.Min(minColumn, columns[pointIndex].D);
                        maxColumn = Math.Max(maxColumn, columns[pointIndex].D);
                    }
                    contourDiagonals.Add(Math.Sqrt((maxRow - minRow) * (maxRow - minRow) + (maxColumn - minColumn) * (maxColumn - minColumn)));

                    try
                    {
                        HTuple approximation;
                        HOperatorSet.GetContourGlobalAttribXld(contour, "cont_approx", out approximation);
                        if (approximation != null && approximation.Length > 0)
                        {
                            double value = approximation[0].D;
                            if (Math.Abs(value + 1) < 0.001)
                            {
                                throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是直线段；请从“XLD 轮廓分段”选择椭圆弧候选。");
                            }
                            if (Math.Abs(value - 1) < 0.001)
                            {
                                throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是圆弧；请使用“XLD 圆拟合测量”。");
                            }
                        }
                    }
                    catch (HOperatorException)
                    {
                        // 原始椭圆 XLD 可能没有 cont_approx；继续用拟合轴比和跨度执行退化检查。
                    }
                }
                finally
                {
                    DisposeObject(contour);
                }
            }

            string algorithm = VmXldEllipseFitAlgorithm.Normalize(parameters.XldEllipseFitAlgorithm);
            string algorithmText = VmXldEllipseFitAlgorithm.GetDisplayText(algorithm);
            string parameterText = string.Format(
                CultureInfo.InvariantCulture,
                "MaxPoints={0}, MaxClosure={1:0.###}, ClipEnds={2}, VossTable={3}, Iterations={4}, ClipFactor={5:0.###}",
                parameters.XldEllipseFitMaxNumPoints,
                parameters.XldEllipseFitMaxClosureDistance,
                parameters.XldEllipseFitClippingEndPoints,
                parameters.XldEllipseFitVossTabSize,
                parameters.XldEllipseFitIterations,
                parameters.XldEllipseFitClippingFactor);

            HObject fittedEllipses = null;
            HTuple rowsFit = null;
            HTuple columnsFit = null;
            HTuple phis = null;
            HTuple radii1 = null;
            HTuple radii2 = null;
            HTuple startPhis = null;
            HTuple endPhis = null;
            HTuple pointOrders = null;
            try
            {
                if (inputCount == 0)
                {
                    HOperatorSet.GenEmptyObj(out fittedEllipses);
                    rowsFit = columnsFit = phis = radii1 = radii2 = startPhis = endPhis = pointOrders = new HTuple();
                }
                else
                {
                    fittedEllipses = imageService.FitXldEllipses(
                        inputXld,
                        algorithm,
                        parameters.XldEllipseFitMaxNumPoints,
                        parameters.XldEllipseFitMaxClosureDistance,
                        parameters.XldEllipseFitClippingEndPoints,
                        parameters.XldEllipseFitVossTabSize,
                        parameters.XldEllipseFitIterations,
                        parameters.XldEllipseFitClippingFactor,
                        out rowsFit,
                        out columnsFit,
                        out phis,
                        out radii1,
                        out radii2,
                        out startPhis,
                        out endPhis,
                        out pointOrders);
                }

                int ellipseCount = rowsFit == null ? 0 : rowsFit.Length;
                double maxResidual = 0;
                double primaryWeight = -1;
                double centerRow = 0;
                double centerColumn = 0;
                double phi = 0;
                double radius1 = 0;
                double radius2 = 0;
                double startPhi = 0;
                double endPhi = 0;
                string pointOrder = "positive";
                for (int index = 0; index < ellipseCount; index++)
                {
                    double currentRadius1 = radii1[index].D;
                    double currentRadius2 = radii2[index].D;
                    if (!IsFiniteGraphCoordinate(currentRadius1) || !IsFiniteGraphCoordinate(currentRadius2) || currentRadius1 <= 0 || currentRadius2 <= 0)
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓得到无效半轴，输入可能退化或不是椭圆弧。");
                    }
                    if (currentRadius2 > currentRadius1)
                    {
                        throw new InvalidOperationException("HALCON 返回的长短半轴次序异常；Radius1 应为长半轴，Radius2 应为短半轴。");
                    }
                    double axisRatio = currentRadius2 / currentRadius1;
                    if (axisRatio >= 0.98)
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓长短轴接近相等，属于圆退化；请使用“XLD 圆拟合测量”。");
                    }
                    if (axisRatio < 0.01)
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓短轴过小，输入接近直线或病态椭圆。");
                    }
                    double diagonal = index < contourDiagonals.Count ? contourDiagonals[index] : 0;
                    if (diagonal <= 1e-6 || currentRadius1 > Math.Max(10, diagonal * 20))
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓拟合半轴相对轮廓跨度过大，输入接近直线或退化。");
                    }

                    double span = Math.Abs(endPhis[index].D - startPhis[index].D);
                    if (span > Math.PI * 2) span = Math.PI * 2;
                    double weight = currentRadius1 * currentRadius2 * Math.Max(0.05, span / (Math.PI * 2));
                    if (weight > primaryWeight)
                    {
                        primaryWeight = weight;
                        centerRow = rowsFit[index].D;
                        centerColumn = columnsFit[index].D;
                        phi = phis[index].D;
                        radius1 = currentRadius1;
                        radius2 = currentRadius2;
                        startPhi = startPhis[index].D;
                        endPhi = endPhis[index].D;
                        pointOrder = pointOrders[index].S;
                    }

                    HObject sourceContour = null;
                    try
                    {
                        HOperatorSet.SelectObj(inputXld, out sourceContour, index + 1);
                        HTuple rows;
                        HTuple columns;
                        HOperatorSet.GetContourXld(sourceContour, out rows, out columns);
                        double cos = Math.Cos(phis[index].D);
                        double sin = Math.Sin(phis[index].D);
                        for (int pointIndex = 0; pointIndex < rows.Length; pointIndex++)
                        {
                            double x = columns[pointIndex].D - columnsFit[index].D;
                            double y = rows[pointIndex].D - rowsFit[index].D;
                            double major = x * cos + y * sin;
                            double minor = -x * sin + y * cos;
                            double normalizedRadius = Math.Sqrt(major * major / (currentRadius1 * currentRadius1) + minor * minor / (currentRadius2 * currentRadius2));
                            maxResidual = Math.Max(maxResidual, Math.Abs(normalizedRadius - 1) * currentRadius2);
                        }
                    }
                    finally
                    {
                        DisposeObject(sourceContour);
                    }
                }

                string resultCode = ellipseCount > 0 && radius1 > radius2 && radius2 > 0 ? "OK" : "NG";
                string pointOrderText = string.Equals(pointOrder, "negative", StringComparison.OrdinalIgnoreCase) ? "negative（反向）" : "positive（正向）";
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(fittedEllipses, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("EllipseCount", (double)ellipseCount);
                tool.SetOutputValue("CenterRow", centerRow);
                tool.SetOutputValue("CenterColumn", centerColumn);
                tool.SetOutputValue("Phi", phi);
                tool.SetOutputValue("Radius1", radius1);
                tool.SetOutputValue("Radius2", radius2);
                tool.SetOutputValue("StartPhi", startPhi);
                tool.SetOutputValue("EndPhi", endPhi);
                tool.SetOutputValue("PointOrder", pointOrderText);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(fittedEllipses, tool, "XLD", ellipseCount, resultCode, radius1);
                fittedEllipses = null;

                string message = ellipseCount == 0
                    ? "HALCON XLD 椭圆拟合：输入为空，椭圆=0；" + parameterText
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "HALCON XLD 椭圆拟合：{0}，椭圆={1}，主中心=({2:0.###},{3:0.###})，方向={4:0.###}°，长/短半轴={5:0.###}/{6:0.###}px，角度={7:0.###}°→{8:0.###}°，点序={9}，最大残差={10:0.###}px；{11}",
                        algorithmText,
                        ellipseCount,
                        centerRow,
                        centerColumn,
                        phi * 180.0 / Math.PI,
                        radius1,
                        radius2,
                        startPhi * 180.0 / Math.PI,
                        endPhi * 180.0 / Math.PI,
                        pointOrderText,
                        maxResidual,
                        parameterText);
                InspectionRecord record = CreateRecord("XldEllipseFitting", resultCode, radius1, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(fittedEllipses);
            }
        }

        private InspectionRecord RunXldRectangleFittingTool(VmToolInstance tool, HObject inputXld)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.XldRectangleFitting);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);

            HTuple inputCountTuple;
            HOperatorSet.CountObj(inputXld, out inputCountTuple);
            int inputCount = inputCountTuple == null || inputCountTuple.Length == 0 ? 0 : inputCountTuple.I;
            List<double> contourDiagonals = new List<double>();
            for (int index = 1; index <= inputCount; index++)
            {
                HObject contour = null;
                try
                {
                    HOperatorSet.SelectObj(inputXld, out contour, index);
                    HTuple rows;
                    HTuple columns;
                    HOperatorSet.GetContourXld(contour, out rows, out columns);
                    if (rows == null || rows.Length < 8)
                    {
                        throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 少于 8 个点，不能拟合旋转矩形。");
                    }

                    double minRow = rows[0].D;
                    double maxRow = minRow;
                    double minColumn = columns[0].D;
                    double maxColumn = minColumn;
                    for (int pointIndex = 1; pointIndex < rows.Length; pointIndex++)
                    {
                        minRow = Math.Min(minRow, rows[pointIndex].D);
                        maxRow = Math.Max(maxRow, rows[pointIndex].D);
                        minColumn = Math.Min(minColumn, columns[pointIndex].D);
                        maxColumn = Math.Max(maxColumn, columns[pointIndex].D);
                    }
                    double diagonal = Math.Sqrt((maxRow - minRow) * (maxRow - minRow) + (maxColumn - minColumn) * (maxColumn - minColumn));
                    if (diagonal <= 1e-6)
                    {
                        throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 已退化为点，不能拟合旋转矩形。");
                    }
                    contourDiagonals.Add(diagonal);

                    try
                    {
                        HTuple approximation;
                        HOperatorSet.GetContourGlobalAttribXld(contour, "cont_approx", out approximation);
                        if (approximation != null && approximation.Length > 0)
                        {
                            double value = approximation[0].D;
                            if (Math.Abs(value + 1) < 0.001)
                            {
                                throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是单一直线段；旋转矩形拟合需要同一轮廓覆盖四条边。");
                            }
                            if (Math.Abs(value - 1) < 0.001)
                            {
                                throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是圆弧；请使用“XLD 圆拟合测量”。");
                            }
                            if (Math.Abs(value - 2) < 0.001)
                            {
                                throw new InvalidOperationException("第 " + index.ToString(CultureInfo.InvariantCulture) + " 条 XLD 是椭圆弧；请使用“XLD 椭圆拟合测量”。");
                            }
                        }
                    }
                    catch (HOperatorException)
                    {
                        // 原始矩形轮廓通常没有 cont_approx；继续按真实点集执行 HALCON 拟合和退化检查。
                    }
                }
                finally
                {
                    DisposeObject(contour);
                }
            }

            string algorithm = VmXldRectangleFitAlgorithm.Normalize(parameters.XldRectangleFitAlgorithm);
            string algorithmText = VmXldRectangleFitAlgorithm.GetDisplayText(algorithm);
            string parameterText = string.Format(
                CultureInfo.InvariantCulture,
                "MaxPoints={0}, MaxClosure={1:0.###}, ClipEnds={2}, Iterations={3}, ClipFactor={4:0.###}",
                parameters.XldRectangleFitMaxNumPoints,
                parameters.XldRectangleFitMaxClosureDistance,
                parameters.XldRectangleFitClippingEndPoints,
                parameters.XldRectangleFitIterations,
                parameters.XldRectangleFitClippingFactor);

            HObject fittedRectangles = null;
            HTuple rowsFit = null;
            HTuple columnsFit = null;
            HTuple phis = null;
            HTuple lengths1 = null;
            HTuple lengths2 = null;
            HTuple pointOrders = null;
            try
            {
                if (inputCount == 0)
                {
                    HOperatorSet.GenEmptyObj(out fittedRectangles);
                    rowsFit = columnsFit = phis = lengths1 = lengths2 = pointOrders = new HTuple();
                }
                else
                {
                    try
                    {
                        fittedRectangles = imageService.FitXldRectangles(
                            inputXld,
                            algorithm,
                            parameters.XldRectangleFitMaxNumPoints,
                            parameters.XldRectangleFitMaxClosureDistance,
                            parameters.XldRectangleFitClippingEndPoints,
                            parameters.XldRectangleFitIterations,
                            parameters.XldRectangleFitClippingFactor,
                            out rowsFit,
                            out columnsFit,
                            out phis,
                            out lengths1,
                            out lengths2,
                            out pointOrders);
                    }
                    catch (HOperatorException ex)
                    {
                        throw new InvalidOperationException("HALCON 旋转矩形拟合失败；请确认每条 XLD 至少 8 点，并覆盖矩形四条边的内部点。原始错误：" + ex.Message, ex);
                    }
                }

                int rectangleCount = rowsFit == null ? 0 : rowsFit.Length;
                double maxResidual = 0;
                double primaryWeight = -1;
                double centerRow = 0;
                double centerColumn = 0;
                double phi = 0;
                double length1 = 0;
                double length2 = 0;
                string pointOrder = "positive";
                for (int index = 0; index < rectangleCount; index++)
                {
                    double currentRow = rowsFit[index].D;
                    double currentColumn = columnsFit[index].D;
                    double currentPhi = phis[index].D;
                    double currentLength1 = lengths1[index].D;
                    double currentLength2 = lengths2[index].D;
                    if (double.IsNaN(currentRow) || double.IsInfinity(currentRow) ||
                        double.IsNaN(currentColumn) || double.IsInfinity(currentColumn) ||
                        double.IsNaN(currentPhi) || double.IsInfinity(currentPhi) ||
                        double.IsNaN(currentLength1) || double.IsInfinity(currentLength1) ||
                        double.IsNaN(currentLength2) || double.IsInfinity(currentLength2) ||
                        currentLength1 <= 0.5 || currentLength2 <= 0.5)
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓得到无效矩形几何，输入可能退化或未覆盖四条边。");
                    }

                    double largerLength = Math.Max(currentLength1, currentLength2);
                    double smallerLength = Math.Min(currentLength1, currentLength2);
                    double diagonal = index < contourDiagonals.Count ? contourDiagonals[index] : 0;
                    if (smallerLength / largerLength < 0.005 || diagonal <= 1e-6 || largerLength > Math.Max(10, diagonal * 20))
                    {
                        throw new InvalidOperationException("第 " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 条轮廓接近直线或病态矩形，边长比例/轮廓跨度不可信。");
                    }

                    double weight = currentLength1 * currentLength2;
                    if (weight > primaryWeight)
                    {
                        primaryWeight = weight;
                        centerRow = currentRow;
                        centerColumn = currentColumn;
                        phi = currentPhi;
                        length1 = currentLength1;
                        length2 = currentLength2;
                        pointOrder = pointOrders[index].S;
                    }

                    HObject sourceContour = null;
                    try
                    {
                        HOperatorSet.SelectObj(inputXld, out sourceContour, index + 1);
                        HTuple rows;
                        HTuple columns;
                        HOperatorSet.GetContourXld(sourceContour, out rows, out columns);
                        double cos = Math.Cos(currentPhi);
                        double sin = Math.Sin(currentPhi);
                        for (int pointIndex = 0; pointIndex < rows.Length; pointIndex++)
                        {
                            double deltaColumn = columns[pointIndex].D - currentColumn;
                            double deltaRow = rows[pointIndex].D - currentRow;
                            double u = deltaColumn * cos - deltaRow * sin;
                            double v = deltaColumn * sin + deltaRow * cos;
                            double outsideU = Math.Abs(u) - currentLength1;
                            double outsideV = Math.Abs(v) - currentLength2;
                            double residual;
                            if (outsideU > 0 && outsideV > 0) residual = Math.Sqrt(outsideU * outsideU + outsideV * outsideV);
                            else if (outsideU > 0) residual = outsideU;
                            else if (outsideV > 0) residual = outsideV;
                            else residual = Math.Min(-outsideU, -outsideV);
                            maxResidual = Math.Max(maxResidual, residual);
                        }
                    }
                    finally
                    {
                        DisposeObject(sourceContour);
                    }
                }

                string resultCode = rectangleCount > 0 && length1 > 0.5 && length2 > 0.5 ? "OK" : "NG";
                string pointOrderText = string.Equals(pointOrder, "negative", StringComparison.OrdinalIgnoreCase) ? "negative（反向）" : "positive（正向）";
                string orientationState = rectangleCount == 0
                    ? "--"
                    : (Math.Abs(length1 - length2) / Math.Max(length1, length2) < 0.02
                        ? "近正方形 · 90°方向等价"
                        : "方向稳定");
                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);
                double corner1Row = centerRow + length1 * sinPhi - length2 * cosPhi;
                double corner1Column = centerColumn - length1 * cosPhi - length2 * sinPhi;
                double corner2Row = centerRow - length1 * sinPhi - length2 * cosPhi;
                double corner2Column = centerColumn + length1 * cosPhi - length2 * sinPhi;
                double corner3Row = centerRow - length1 * sinPhi + length2 * cosPhi;
                double corner3Column = centerColumn + length1 * cosPhi + length2 * sinPhi;
                double corner4Row = centerRow + length1 * sinPhi + length2 * cosPhi;
                double corner4Column = centerColumn - length1 * cosPhi + length2 * sinPhi;

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(fittedRectangles, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("RectangleCount", (double)rectangleCount);
                tool.SetOutputValue("CenterRow", centerRow);
                tool.SetOutputValue("CenterColumn", centerColumn);
                tool.SetOutputValue("Phi", phi);
                tool.SetOutputValue("Length1", length1);
                tool.SetOutputValue("Length2", length2);
                tool.SetOutputValue("Corner1Row", corner1Row);
                tool.SetOutputValue("Corner1Column", corner1Column);
                tool.SetOutputValue("Corner2Row", corner2Row);
                tool.SetOutputValue("Corner2Column", corner2Column);
                tool.SetOutputValue("Corner3Row", corner3Row);
                tool.SetOutputValue("Corner3Column", corner3Column);
                tool.SetOutputValue("Corner4Row", corner4Row);
                tool.SetOutputValue("Corner4Column", corner4Column);
                tool.SetOutputValue("PointOrder", pointOrderText);
                tool.SetOutputValue("OrientationState", orientationState);
                tool.SetOutputValue("MaxResidual", maxResidual);
                tool.SetOutputValue("Algorithm", algorithmText);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(fittedRectangles, tool, "XLD", rectangleCount, resultCode, 2 * (length1 + length2));
                fittedRectangles = null;

                string message = rectangleCount == 0
                    ? "HALCON XLD 旋转矩形拟合：输入为空，矩形=0；" + parameterText
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "HALCON XLD 旋转矩形拟合：{0}，矩形={1}，主中心=({2:0.###},{3:0.###})，方向={4:0.###}°，半边长={5:0.###}/{6:0.###}px，整边长={7:0.###}×{8:0.###}px，点序={9}，{10}，最大边界残差={11:0.###}px；{12}",
                        algorithmText,
                        rectangleCount,
                        centerRow,
                        centerColumn,
                        phi * 180.0 / Math.PI,
                        length1,
                        length2,
                        length1 * 2,
                        length2 * 2,
                        pointOrderText,
                        orientationState,
                        maxResidual,
                        parameterText);
                InspectionRecord record = CreateRecord("XldRectangleFitting", resultCode, Math.Max(length1, length2), message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(fittedRectangles);
            }
        }

        private void GetToolImageMetadata(VmToolInstance tool, out string sourcePath, out string serialNumber)
        {
            sourcePath = currentImagePath;
            serialNumber = string.IsNullOrWhiteSpace(currentImagePath) ? null : Path.GetFileNameWithoutExtension(currentImagePath);
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Image");
            VmToolInstance source = GetInputSourceTool(binding);
            VmImageSnapshot snapshot = GetImageSnapshot(source, binding == null ? null : binding.SourcePortName);
            if (snapshot == null || snapshot.IsDisposed)
            {
                return;
            }

            sourcePath = snapshot.SourcePath;
            serialNumber = snapshot.SerialNumber;
        }

        private void ApplyRecordImage(InspectionRecord record, VmToolInstance tool, HImage image)
        {
            if (record == null || image == null)
            {
                return;
            }

            if (record.ImageSnapshot != null)
            {
                record.ImageSnapshot.Dispose();
            }

            record.ImageSnapshot = image.CopyImage();
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Image");
            VmToolInstance source = GetInputSourceTool(binding);
            VmImageSnapshot snapshot = GetImageSnapshot(source, binding == null ? null : binding.SourcePortName);
            if (tool != null && tool.Kind == VmToolKind.ImageSource)
            {
                record.ImageSource = Path.GetFileName((tool.Parameters ?? new VmToolParameterData()).LocalImagePath);
            }
            else if (snapshot != null)
            {
                record.ImageSource = source.InstanceName + "." + binding.SourcePortName + " / " + snapshot.SourceName;
            }
            else
            {
                record.ImageSource = string.IsNullOrWhiteSpace(currentImagePath) ? "系统.Image" : Path.GetFileName(currentImagePath);
            }
        }

        private static bool EvaluateNumericJudge(VmToolInstance tool, double value)
        {
            switch (tool.NumericOperator)
            {
                case NumericJudgeOperatorOption.BetweenInclusive:
                    return value >= tool.NumericLowerLimit && value <= tool.NumericUpperLimit;
                case NumericJudgeOperatorOption.OutsideInclusive:
                    return value <= tool.NumericLowerLimit || value >= tool.NumericUpperLimit;
                case NumericJudgeOperatorOption.GreaterOrEqual:
                    return value >= tool.NumericLowerLimit;
                case NumericJudgeOperatorOption.LessOrEqual:
                    return value <= tool.NumericUpperLimit;
                case NumericJudgeOperatorOption.Equal:
                    return Math.Abs(value - tool.NumericLowerLimit) <= tool.NumericTolerance;
                case NumericJudgeOperatorOption.NotEqual:
                    return Math.Abs(value - tool.NumericLowerLimit) > tool.NumericTolerance;
                default:
                    throw new InvalidOperationException("不支持的数值比较方式：" + tool.NumericOperator);
            }
        }

        private static string GetNumericJudgeLimitText(VmToolInstance tool)
        {
            if (tool.NumericOperator == NumericJudgeOperatorOption.BetweenInclusive ||
                tool.NumericOperator == NumericJudgeOperatorOption.OutsideInclusive)
            {
                return string.Format(CultureInfo.InvariantCulture, "[{0:0.###}, {1:0.###}]", tool.NumericLowerLimit, tool.NumericUpperLimit);
            }

            if (tool.NumericOperator == NumericJudgeOperatorOption.LessOrEqual)
            {
                return tool.NumericUpperLimit.ToString("0.###", CultureInfo.InvariantCulture);
            }

            if (tool.NumericOperator == NumericJudgeOperatorOption.Equal ||
                tool.NumericOperator == NumericJudgeOperatorOption.NotEqual)
            {
                return string.Format(CultureInfo.InvariantCulture, "目标={0:0.###}, 容差={1:0.###}", tool.NumericLowerLimit, tool.NumericTolerance);
            }

            return tool.NumericLowerLimit.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void PublishToolOutputs(VmToolInstance tool, InspectionRecord record)
        {
            tool.SetOutputValue("ResultCode", record.ResultCode);
            switch (tool.Kind)
            {
                case VmToolKind.ImageSource:
                case VmToolKind.ImageChannel:
                case VmToolKind.ImageFilter:
                case VmToolKind.ImageEnhancement:
                case VmToolKind.ImageDifference:
                    break;
                case VmToolKind.ImageThreshold:
                case VmToolKind.DynamicThreshold:
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionSetOperation:
                    tool.SetOutputValue("Area", record.Score);
                    break;
                case VmToolKind.RegionContourMeasurement:
                case VmToolKind.XldContourFilter:
                case VmToolKind.XldContourSegmentation:
                case VmToolKind.XldLineFitting:
                    tool.SetOutputValue("TotalLength", record.Score);
                    break;
                case VmToolKind.ShapeMatch:
                    tool.SetOutputValue("Score", record.Score);
                    tool.SetOutputValue("MatchCount", currentMatches.Count);
                    if (record.MatchRow.HasValue)
                    {
                        tool.SetOutputValue("Row", record.MatchRow.Value);
                    }
                    if (record.MatchColumn.HasValue)
                    {
                        tool.SetOutputValue("Column", record.MatchColumn.Value);
                    }
                    if (record.MatchAngle.HasValue)
                    {
                        tool.SetOutputValue("Angle", record.MatchAngle.Value);
                    }
                    break;
                case VmToolKind.Blob:
                    tool.SetOutputValue("Area", record.Score);
                    tool.SetOutputValue("RoiResults", tool.RoiResults.ToList());
                    break;
                case VmToolKind.GrayStat:
                    tool.SetOutputValue("Mean", record.Score);
                    tool.SetOutputValue("RoiResults", tool.RoiResults.ToList());
                    break;
                case VmToolKind.EdgeMeasure:
                    tool.SetOutputValue("Length", record.Score);
                    tool.SetOutputValue("RoiResults", tool.RoiResults.ToList());
                    break;
                case VmToolKind.HDevelop:
                    tool.SetOutputValue("Score", record.Score);
                    break;
                case VmToolKind.NumericJudge:
                    tool.SetOutputValue("Value", record.Score);
                    tool.SetOutputValue("Passed", string.Equals(record.ResultCode, "OK", StringComparison.OrdinalIgnoreCase));
                    break;
            }
        }

        private InspectionRecord RunShapeMatchTool(VmToolInstance tool, string source)
        {
            return ExecuteShapeMatch(tool, source);
        }

        private InspectionRecord ExecuteShapeMatch(VmToolInstance tool, string source)
        {
            EnsureImage();
            if (currentTemplateItem == null || !currentTemplateItem.HasModel)
            {
                throw new InvalidOperationException("请先训练或加载模板。");
            }

            List<ShapeMatchResult> matches;
            using (HRegion roiRegion = CreateBoundRoiRegion(tool, true))
            {
                matches = currentTemplateItem.Service.Match(currentImage, roiRegion, currentTemplateItem.Options);
            }
            currentMatches.Clear();
            currentMatches.AddRange(matches);

            ShapeMatchResult best = matches.OrderByDescending(item => item.Score).FirstOrDefault();
            InspectionRecord record = CreateRecord("ShapeModel", best == null ? "NG" : "OK", best == null ? 0 : best.Score, matches.Count == 0 ? "未匹配到目标" : "匹配数量：" + matches.Count, tool);
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

        private InspectionRecord RunBlobTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.Blob);
            if (!string.IsNullOrWhiteSpace(parameterError))
            {
                throw new InvalidOperationException(parameterError);
            }
            if (parameters.RoiExecutionMode == VmRoiExecutionMode.PerRoi)
            {
                return RunBlobPerRoi(tool, parameters, inputImage);
            }
            double minGray = parameters.BlobMinGray;
            double maxGray = parameters.BlobMaxGray;
            double minArea = parameters.BlobMinArea;

            HObject thresholdRegion = null;
            HObject clippedRegion = null;
            HObject connectedRegion = null;
            HObject selectedRegion = null;
            HImage gray = null;
            HRegion roiRegion = null;
            try
            {
                gray = CreateGrayImage(inputImage);
                HOperatorSet.Threshold(gray, out thresholdRegion, minGray, maxGray);
                HObject sourceRegion = thresholdRegion;
                roiRegion = CreateEffectiveRoiRegion(tool, false);
                if (roiRegion != null)
                {
                    HOperatorSet.Intersection(thresholdRegion, roiRegion, out clippedRegion);
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
                tool.SetOutputValue("SelectedRegion", VmRegionSnapshot.Create(selectedRegion, tool.ToolId, tool.InstanceName, "SelectedRegion"));
                ReplaceToolOverlayRegion(selectedRegion, tool, "SelectedRegion", count, totalArea, count > 0 ? "OK" : "NG");
                selectedRegion = null;

                string message = count > 0
                    ? "Blob数量：" + count + "，运行域=" + GetRoiInputSummary(tool)
                    : "未找到满足面积的 Blob";
                InspectionRecord record = CreateRecord("Blob", count > 0 ? "OK" : "NG", totalArea, message, tool);
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
                if (roiRegion != null)
                {
                    roiRegion.Dispose();
                }
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private void BeginRoiGeometryEdit(PointF imagePoint, System.Drawing.Point windowPoint)
        {
            double tolerance = GetRoiHitTolerance(windowPoint);
            VmRoiLayer selected = RoiLayerList.SelectedItem as VmRoiLayer;
            RoiEditHit hit = selected == null || !selected.IsVisible
                ? new RoiEditHit { Handle = RoiEditHandle.None, VertexIndex = -1 }
                : RoiGeometryEditor.HitTestDetailed(selected.Geometry, imagePoint, tolerance);
            RoiEditHandle handle = hit.Handle;
            VmRoiLayer hitLayer = selected;
            if (handle == RoiEditHandle.None)
            {
                hitLayer = roiLayers.Reverse().FirstOrDefault(layer =>
                    layer.IsVisible && layer.Geometry != null &&
                    RoiGeometryEditor.HitTest(layer.Geometry, imagePoint, tolerance) != RoiEditHandle.None);
                hit = hitLayer == null
                    ? new RoiEditHit { Handle = RoiEditHandle.None, VertexIndex = -1 }
                    : RoiGeometryEditor.HitTestDetailed(hitLayer.Geometry, imagePoint, tolerance);
                handle = hit.Handle;
            }

            if (hitLayer == null || handle == RoiEditHandle.None)
            {
                RoiLayerList.SelectedItem = null;
                SelectRoiLayer(null);
                HeaderStatusText.Text = "选择模式：单击 ROI 选中，拖动内部移动，拖动控制点调整。";
                ScheduleRefreshDisplay();
                return;
            }

            RoiLayerList.SelectedItem = hitLayer;
            SelectRoiLayer(hitLayer);
            if (hitLayer.IsLocked)
            {
                HeaderStatusText.Text = "ROI 已锁定，当前仅选择不编辑：" + hitLayer.Name;
                ScheduleRefreshDisplay();
                return;
            }

            FinishRoiGeometryEdit(false);
            roiEditingLayer = hitLayer;
            roiEditHandle = handle;
            roiEditVertexIndex = hit.VertexIndex;
            roiEditStartPoint = imagePoint;
            roiEditOriginalGeometry = hitLayer.Geometry.Clone();
            roiGeometryChanged = false;
            imageWindow.Capture = true;
            HeaderStatusText.Text = "开始编辑 ROI：" + hitLayer.Name + " · " + GetRoiEditHandleText(handle, hit.VertexIndex);
        }

        private double GetRoiHitTolerance(System.Drawing.Point windowPoint)
        {
            PointF origin = viewport.WindowToImage(windowPoint, imageWindow);
            PointF horizontal = viewport.WindowToImage(new System.Drawing.Point(windowPoint.X + 9, windowPoint.Y), imageWindow);
            PointF vertical = viewport.WindowToImage(new System.Drawing.Point(windowPoint.X, windowPoint.Y + 9), imageWindow);
            return Math.Max(2, Math.Max(Math.Abs(horizontal.X - origin.X), Math.Abs(vertical.Y - origin.Y)));
        }

        private static string GetRoiEditHandleText(RoiEditHandle handle, int vertexIndex)
        {
            switch (handle)
            {
                case RoiEditHandle.Move:
                    return "整体移动";
                case RoiEditHandle.Rotate:
                    return "旋转";
                case RoiEditHandle.Length1Start:
                case RoiEditHandle.Length1End:
                    return "调整宽度";
                case RoiEditHandle.Length2Start:
                case RoiEditHandle.Length2End:
                    return "调整高度";
                case RoiEditHandle.PolygonVertex:
                    return "顶点 " + (vertexIndex + 1).ToString(CultureInfo.InvariantCulture);
                case RoiEditHandle.Radius:
                    return "调整半径";
                default:
                    return "调整尺寸";
            }
        }

        private void FinishRoiGeometryEdit(bool commit)
        {
            VmRoiLayer layer = roiEditingLayer;
            if (layer != null && !commit && roiEditOriginalGeometry != null)
            {
                layer.ReplaceGeometry(roiEditOriginalGeometry.Clone());
                SelectRoiLayer(layer);
            }

            if (layer != null && commit && roiGeometryChanged)
            {
                InvalidateRoiDependentResults(layer.RoiId, "ROI 几何已修改");
                currentMatches.Clear();
                DisposeToolOverlays();
                ScheduleRecipeStateCheck();
                HeaderStatusText.Text = "ROI 几何已更新：" + layer.Name + " · " + layer.GeometryText;
                LogInfo(HeaderStatusText.Text);
            }

            if (roiEditOriginalGeometry != null)
            {
                roiEditOriginalGeometry.Dispose();
                roiEditOriginalGeometry = null;
            }
            roiEditingLayer = null;
            roiEditHandle = RoiEditHandle.None;
            roiEditVertexIndex = -1;
            roiGeometryChanged = false;
            if (imageWindow != null)
            {
                imageWindow.Capture = false;
                imageWindow.Cursor = Forms.Cursors.Default;
            }
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private InspectionRecord RunBlobPerRoi(VmToolInstance tool, VmToolParameterData parameters, HImage inputImage)
        {
            List<VmRoiLayer> layers = GetRequiredPerRoiLayers(tool);
            List<VmRoiRunResult> results = new List<VmRoiRunResult>();
            HImage gray = null;
            HObject thresholdRegion = null;
            HObject aggregateSelection = null;
            int aggregateObjectCount = 0;
            try
            {
                gray = CreateGrayImage(inputImage);
                HOperatorSet.Threshold(gray, out thresholdRegion, parameters.BlobMinGray, parameters.BlobMaxGray);
                foreach (VmRoiLayer layer in layers)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    HObject clippedRegion = null;
                    HObject connectedRegion = null;
                    HObject selectedRegion = null;
                    try
                    {
                        HOperatorSet.Intersection(thresholdRegion, layer.Geometry.Region, out clippedRegion);
                        HOperatorSet.Connection(clippedRegion, out connectedRegion);
                        HOperatorSet.SelectShape(connectedRegion, out selectedRegion, "area", "and", parameters.BlobMinArea, 999999999.0);
                        HTuple area;
                        HTuple row;
                        HTuple column;
                        HOperatorSet.AreaCenter(selectedRegion, out area, out row, out column);
                        double totalArea = SumTuple(area);
                        int count = area == null ? 0 : area.Length;
                        aggregateObjectCount += count;
                        AppendOverlayObject(ref aggregateSelection, selectedRegion);
                        results.Add(CreateRoiRunResult(layer, count > 0 ? "OK" : "NG", "Area", totalArea, "Blob=" + count.ToString(CultureInfo.InvariantCulture), null, stopwatch.Elapsed.TotalMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        results.Add(CreateRoiRunResult(layer, "NG", "Area", 0, "执行失败", ex.Message, stopwatch.Elapsed.TotalMilliseconds));
                        logger.Error(tool.InstanceName + " / " + layer.Name + " 逐 ROI Blob 失败", ex);
                    }
                    finally
                    {
                        stopwatch.Stop();
                        DisposeObject(selectedRegion);
                        DisposeObject(connectedRegion);
                        DisposeObject(clippedRegion);
                    }
                }

                if (aggregateSelection != null)
                {
                    tool.SetOutputValue("SelectedRegion", VmRegionSnapshot.Create(aggregateSelection, tool.ToolId, tool.InstanceName, "SelectedRegion"));
                    double selectedArea = results.Sum(item => item.Value);
                    ReplaceToolOverlayRegion(aggregateSelection, tool, "SelectedRegion", aggregateObjectCount, selectedArea, aggregateObjectCount > 0 ? "OK" : "NG");
                    aggregateSelection = null;
                }

                return CompletePerRoiRun(tool, "Blob", "Area", results, results.Sum(item => item.Value));
            }
            finally
            {
                DisposeObject(aggregateSelection);
                DisposeObject(thresholdRegion);
                if (gray != null) gray.Dispose();
            }
        }

        private InspectionRecord RunGrayStatTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.GrayStat);
            if (!string.IsNullOrWhiteSpace(parameterError))
            {
                throw new InvalidOperationException(parameterError);
            }
            if (parameters.RoiExecutionMode == VmRoiExecutionMode.PerRoi)
            {
                return RunGrayStatPerRoi(tool, parameters, inputImage);
            }
            double min = parameters.GrayMin;
            double max = parameters.GrayMax;
            HImage gray = null;
            HRegion region = null;
            try
            {
                gray = CreateGrayImage(inputImage);
                HTuple mean;
                HTuple deviation;
                region = CreateEffectiveRoiRegion(tool, false) ?? new HRegion(0.0, 0.0, (double)GetImageHeight(inputImage) - 1.0, (double)GetImageWidth(inputImage) - 1.0);
                HOperatorSet.Intensity(region, gray, out mean, out deviation);
                double value = mean.D;
                bool ok = value >= min && value <= max;
                InspectionRecord record = CreateRecord("GrayStat", ok ? "OK" : "NG", value, string.Format(CultureInfo.InvariantCulture, "Mean={0:F2}, Dev={1:F2}, 运行域={2}", value, deviation.D, GetRoiInputSummary(tool)), tool);
                resultStore.Add(record);
                LogInfo("灰度统计完成：" + record.Message);
                return record;
            }
            finally
            {
                if (region != null)
                {
                    region.Dispose();
                }
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private InspectionRecord RunGrayStatPerRoi(VmToolInstance tool, VmToolParameterData parameters, HImage inputImage)
        {
            List<VmRoiLayer> layers = GetRequiredPerRoiLayers(tool);
            List<VmRoiRunResult> results = new List<VmRoiRunResult>();
            HImage gray = null;
            try
            {
                gray = CreateGrayImage(inputImage);
                foreach (VmRoiLayer layer in layers)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    try
                    {
                        HTuple mean;
                        HTuple deviation;
                        HOperatorSet.Intensity(layer.Geometry.Region, gray, out mean, out deviation);
                        double value = mean.D;
                        bool ok = value >= parameters.GrayMin && value <= parameters.GrayMax;
                        results.Add(CreateRoiRunResult(layer, ok ? "OK" : "NG", "Mean", value, "Dev=" + deviation.D.ToString("0.###", CultureInfo.InvariantCulture), null, stopwatch.Elapsed.TotalMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        results.Add(CreateRoiRunResult(layer, "NG", "Mean", 0, "执行失败", ex.Message, stopwatch.Elapsed.TotalMilliseconds));
                        logger.Error(tool.InstanceName + " / " + layer.Name + " 逐 ROI 灰度统计失败", ex);
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                }

                double average = results.Count == 0 ? 0 : results.Average(item => item.Value);
                return CompletePerRoiRun(tool, "GrayStat", "Mean", results, average);
            }
            finally
            {
                if (gray != null) gray.Dispose();
            }
        }

        private InspectionRecord RunEdgeMeasureTool(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.EdgeMeasure);
            if (!string.IsNullOrWhiteSpace(parameterError))
            {
                throw new InvalidOperationException(parameterError);
            }
            if (parameters.RoiExecutionMode == VmRoiExecutionMode.PerRoi)
            {
                return RunEdgeMeasurePerRoi(tool, parameters, inputImage);
            }
            double threshold = parameters.EdgeThreshold;
            HImage gray = null;
            HObject reduced = null;
            HObject edges = null;
            HRegion roiRegion = null;
            try
            {
                gray = CreateGrayImage(inputImage);
                HObject edgeInput = gray;
                roiRegion = CreateEffectiveRoiRegion(tool, false);
                if (roiRegion != null)
                {
                    HOperatorSet.ReduceDomain(gray, roiRegion, out reduced);
                    edgeInput = reduced;
                }

                HOperatorSet.EdgesSubPix(edgeInput, out edges, "canny", 1.0, threshold, threshold * 2.0);
                HTuple lengths;
                HOperatorSet.LengthXld(edges, out lengths);
                double totalLength = SumTuple(lengths);
                ReplaceToolOverlayContours(edges, tool, "Contours", lengths == null ? 0 : lengths.Length, totalLength > 0 ? "OK" : "NG", totalLength);
                edges = null;

                InspectionRecord record = CreateRecord("EdgeMeasure", totalLength > 0 ? "OK" : "NG", totalLength, string.Format(CultureInfo.InvariantCulture, "边缘总长度={0:F1}, 运行域={1}", totalLength, GetRoiInputSummary(tool)), tool);
                resultStore.Add(record);
                LogInfo("边缘测量完成：" + record.Message);
                return record;
            }
            finally
            {
                DisposeObject(edges);
                DisposeObject(reduced);
                if (roiRegion != null)
                {
                    roiRegion.Dispose();
                }
                if (gray != null)
                {
                    gray.Dispose();
                }
            }
        }

        private InspectionRecord RunEdgeMeasurePerRoi(VmToolInstance tool, VmToolParameterData parameters, HImage inputImage)
        {
            List<VmRoiLayer> layers = GetRequiredPerRoiLayers(tool);
            List<VmRoiRunResult> results = new List<VmRoiRunResult>();
            HImage gray = null;
            HObject aggregateContours = null;
            try
            {
                gray = CreateGrayImage(inputImage);
                foreach (VmRoiLayer layer in layers)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    HObject reduced = null;
                    HObject edges = null;
                    try
                    {
                        HOperatorSet.ReduceDomain(gray, layer.Geometry.Region, out reduced);
                        HOperatorSet.EdgesSubPix(reduced, out edges, "canny", 1.0, parameters.EdgeThreshold, parameters.EdgeThreshold * 2.0);
                        HTuple lengths;
                        HOperatorSet.LengthXld(edges, out lengths);
                        double totalLength = SumTuple(lengths);
                        AppendOverlayObject(ref aggregateContours, edges);
                        results.Add(CreateRoiRunResult(layer, totalLength > 0 ? "OK" : "NG", "Length", totalLength, "边缘总长度=" + totalLength.ToString("0.###", CultureInfo.InvariantCulture), null, stopwatch.Elapsed.TotalMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        results.Add(CreateRoiRunResult(layer, "NG", "Length", 0, "执行失败", ex.Message, stopwatch.Elapsed.TotalMilliseconds));
                        logger.Error(tool.InstanceName + " / " + layer.Name + " 逐 ROI 边缘测量失败", ex);
                    }
                    finally
                    {
                        stopwatch.Stop();
                        DisposeObject(edges);
                        DisposeObject(reduced);
                    }
                }

                if (aggregateContours != null)
                {
                    ReplaceToolOverlayContours(aggregateContours, tool, "Contours", results.Count, results.Any(item => item.ResultCode == "OK") ? "OK" : "NG", results.Sum(item => item.Value));
                    aggregateContours = null;
                }

                return CompletePerRoiRun(tool, "EdgeMeasure", "Length", results, results.Sum(item => item.Value));
            }
            finally
            {
                DisposeObject(aggregateContours);
                if (gray != null) gray.Dispose();
            }
        }

        private List<VmRoiLayer> GetRequiredPerRoiLayers(VmToolInstance tool)
        {
            VmToolInputBindingData subscribedRegion = tool == null ? null : tool.GetInputBinding("ROI");
            if (subscribedRegion != null)
            {
                throw new InvalidOperationException("逐 ROI 模式必须使用本地 ROI 图层；请在输入页断开 Region 订阅，或切换为合并 ROI。");
            }

            List<VmRoiLayer> layers = GetBoundRoiLayers(tool);
            if (layers.Count == 0)
            {
                throw new InvalidOperationException(tool.InstanceName + " 使用逐 ROI 模式，必须至少绑定一个已启用 ROI。请在统一模块配置的 ROI / 模板页完成绑定。");
            }

            return layers;
        }

        private static VmRoiRunResult CreateRoiRunResult(VmRoiLayer layer, string resultCode, string valueName, double value, string message, string error, double elapsedMilliseconds)
        {
            return new VmRoiRunResult
            {
                RoiId = layer.RoiId,
                RoiName = layer.Name,
                ShapeText = layer.ShapeText,
                ResultCode = resultCode,
                ValueName = valueName,
                Value = value,
                Message = message,
                ErrorMessage = error,
                ElapsedMilliseconds = elapsedMilliseconds
            };
        }

        private InspectionRecord CompletePerRoiRun(VmToolInstance tool, string inspectionType, string valueName, IList<VmRoiRunResult> results, double aggregateValue)
        {
            tool.ReplaceRoiResults(results);
            int okCount = results.Count(item => string.Equals(item.ResultCode, "OK", StringComparison.OrdinalIgnoreCase));
            int ngCount = results.Count - okCount;
            string resultCode = ngCount == 0 && results.Count > 0 ? "OK" : "NG";
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "逐 ROI：总数={0}，OK={1}，NG={2}，{3}={4:0.###}",
                results.Count,
                okCount,
                ngCount,
                valueName,
                aggregateValue);
            InspectionRecord record = CreateRecord(inspectionType, resultCode, aggregateValue, message, tool);
            resultStore.Add(record);
            foreach (VmRoiRunResult result in results)
            {
                LogInfo(tool.InstanceName + " / " + result.RoiName + "：" + result.ResultCode + "，" + result.ValueText + "，" + result.DisplayMessage);
            }
            LogInfo(tool.InstanceName + " 完成：" + message);
            return record;
        }

        private static void AppendOverlayObject(ref HObject aggregate, HObject value)
        {
            if (value == null)
            {
                return;
            }

            if (aggregate == null)
            {
                HOperatorSet.CopyObj(value, out aggregate, 1, -1);
                return;
            }

            HObject combined;
            HOperatorSet.ConcatObj(aggregate, value, out combined);
            aggregate.Dispose();
            aggregate = combined;
        }

        private InspectionRecord RunHDevTool(VmToolInstance tool)
        {
            string path = HDevPathTextBox.Text == null ? string.Empty : HDevPathTextBox.Text.Trim();
            string procedure = string.IsNullOrWhiteSpace(HDevProcedureTextBox.Text) ? "RunInspection" : HDevProcedureTextBox.Text.Trim();
            HDevInspectionResult result;
            using (HRegion roiRegion = CreateBoundRoiRegion(tool, true))
            {
                result = hdevService.RunInspection(path, procedure, currentImage, roiRegion);
            }
            InspectionRecord record = CreateRecord("HDevelop", string.IsNullOrWhiteSpace(result.ResultCode) ? "OK" : result.ResultCode, result.Score, result.Message, tool);
            resultStore.Add(record);
            LogInfo("HDevelop 执行完成：" + record.Message);
            return record;
        }

        private InspectionRecord CreateRecord(string type, string resultCode, double score, string message)
        {
            return CreateRecord(type, resultCode, score, message, null);
        }

        private InspectionRecord CreateRecord(string type, string resultCode, double score, string message, VmToolInstance tool)
        {
            RoiData recordRoi = tool == null ? currentRoi : GetPrimaryBoundRoi(tool);
            return new InspectionRecord
            {
                Timestamp = DateTime.Now,
                ImageSource = string.IsNullOrWhiteSpace(currentImagePath) ? "Camera/Memory" : Path.GetFileName(currentImagePath),
                InspectionType = type,
                Roi = recordRoi == null ? null : recordRoi.Clone(),
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
            HTuple count = null;
            HTuple area = null;
            HTuple row = null;
            HTuple column = null;
            try
            {
                HOperatorSet.CountObj(region, out count);
                HOperatorSet.AreaCenter(region, out area, out row, out column);
            }
            catch (HalconException)
            {
                // The renderer still owns and displays the object; legend metrics fall back to zero.
            }
            VmToolInstance producer = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            ReplaceToolOverlayRegion(region, producer, "Region", count == null || count.Length == 0 ? 0 : count.I, SumTuple(area), producer == null ? "--" : producer.ResultCode);
        }

        private void ReplaceToolOverlayRegion(HObject region, VmToolInstance producer, string portName, int objectCount, double area, string resultCode)
        {
            DisposeObject(toolOverlayRegion);
            toolOverlayRegion = region;
            toolOverlayProducerToolId = producer == null ? null : producer.ToolId;
            toolOverlaySourceText = producer == null ? "最近 Region 结果" : producer.InstanceName + "." + portName;
            toolOverlayColorText = "黄色";
            toolOverlayResultCode = string.IsNullOrWhiteSpace(resultCode) || resultCode == "--" ? (objectCount > 0 ? "OK" : "NG") : resultCode;
            toolOverlayObjectCount = Math.Max(0, objectCount);
            toolOverlayArea = Math.Max(0, area);
        }

        private void ReplaceToolOverlayContours(HObject contours)
        {
            HTuple count = null;
            try { HOperatorSet.CountObj(contours, out count); } catch (HalconException) { }
            VmToolInstance producer = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            ReplaceToolOverlayContours(contours, producer, "Contours", count == null || count.Length == 0 ? 0 : count.I, producer == null ? "--" : producer.ResultCode);
        }

        private void ReplaceToolOverlayContours(HObject contours, VmToolInstance producer, string portName, int objectCount, string resultCode, double totalLength = 0)
        {
            DisposeObject(toolOverlayContours);
            toolOverlayContours = contours;
            toolOverlayProducerToolId = producer == null ? null : producer.ToolId;
            toolOverlaySourceText = producer == null ? "最近轮廓结果" : producer.InstanceName + "." + portName;
            toolOverlayColorText = "青色";
            toolOverlayResultCode = string.IsNullOrWhiteSpace(resultCode) || resultCode == "--" ? (objectCount > 0 ? "OK" : "NG") : resultCode;
            toolOverlayObjectCount = Math.Max(0, objectCount);
            toolOverlayArea = Math.Max(0, totalLength);
        }

        private void RefreshDisplay()
        {
            if (imageWindow == null || imageWindow.HalconWindow == null)
            {
                return;
            }

            imageWindow.HalconWindow.ClearWindow();
            ResolvedImageContext context = ResolveImageContext();
            if (!context.HasImage)
            {
                return;
            }

            HImage displayImage = context.CreateImageCopy();
            try
            {
                viewport.Apply(imageWindow.HalconWindow);
                displayImage.DispImage(imageWindow.HalconWindow);

                VmRoiLayer selectedLayer = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
                if (showRoiOverlay)
                {
                    foreach (VmRoiLayer layer in roiLayers.Where(item => item.IsVisible && item.Geometry != null))
                    {
                        string color = layer == selectedLayer ? "green" : (layer.IsEnabled ? "cyan" : "gray");
                        overlayRenderer.DrawRoiLayer(imageWindow.HalconWindow, layer.Geometry, color, layer == selectedLayer ? 3 : 2, layer == selectedLayer, layer.IsLocked);
                    }
                }

                if (showResultOverlay && toolOverlayRegion != null)
                {
                    imageWindow.HalconWindow.SetColor("yellow");
                    imageWindow.HalconWindow.SetDraw("margin");
                    imageWindow.HalconWindow.SetLineWidth(2);
                    imageWindow.HalconWindow.DispObj(toolOverlayRegion);
                }

                if (showResultOverlay && toolOverlayContours != null)
                {
                    imageWindow.HalconWindow.SetColor("cyan");
                    imageWindow.HalconWindow.SetLineWidth(2);
                    imageWindow.HalconWindow.DispObj(toolOverlayContours);
                }

                RoiData templateRoi = currentTemplateItem == null ? null : currentTemplateItem.TemplateRoi;
                RoiData confirmedBoundary = templateRoi;
                ShapeTemplateService service = currentTemplateItem == null ? null : currentTemplateItem.Service;
                overlayRenderer.Draw(
                    imageWindow.HalconWindow,
                    null,
                    showRoiOverlay ? confirmedBoundary : null,
                    pendingRoi ?? roiEditor.PreviewRoi,
                    null,
                    showResultOverlay && currentTemplateItem != null ? currentTemplateItem.DisplayFrame : null,
                    showResultOverlay ? currentMatches : null,
                    service,
                    false,
                    showRoiOverlay && confirmedBoundary != null && currentMatches.Count == 0,
                    showResultOverlay && ShowResultFrameCheckBox.IsChecked == true);
            }
            finally
            {
                displayImage.Dispose();
            }
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

            ResolvedImageContext imageContext = ResolveImageContext();
            bool hasImage = imageContext.HasImage;
            bool hasGlobalImage = currentImage != null;
            bool hasRoi = currentRoi != null;
            bool hasRoiLayers = roiLayers.Count > 0;
            bool hasPendingRoi = pendingRoi != null || roiEditor.IsPolygonDrawing || roiEditor.PreviewRoi != null;
            bool hasTemplate = currentTemplateItem != null && currentTemplateItem.HasModel;
            bool tcpRunning = tcpService.IsRunning;
            bool canSend = tcpService.CanSend;
            bool clientMode = TcpClientModeRadio.IsChecked == true;
            bool hasEnabledTools = flowTools.Any(item => item.IsEnabled);
            bool flowBusy = isContinuousRunning || isFlowExecutionActive;

            RefreshFlowEditHistoryState();

            bool hasSelectedFlowTools = FlowToolList != null && FlowToolList.SelectedItems.Count > 0;
            CopyFlowToolButton.IsEnabled = hasSelectedFlowTools;
            PasteFlowToolButton.IsEnabled = flowToolClipboard.Count > 0 && !flowBusy;
            DuplicateFlowToolButton.IsEnabled = hasSelectedFlowTools && !flowBusy;
            BatchEnableFlowToolsButton.IsEnabled = hasSelectedFlowTools && !flowBusy;
            BatchDisableFlowToolsButton.IsEnabled = hasSelectedFlowTools && !flowBusy;
            BatchDeleteFlowToolsButton.IsEnabled = hasSelectedFlowTools && !flowBusy;
            RefreshFlowClipboardStatus();

            foreach (VmToolInstance tool in flowTools)
            {
                RefreshToolConfigurationStatus(tool);
            }
            RefreshFlowPortVisualization();
            SyncLegacyToolChecksFromFlow();

            PreviousImageButton.IsEnabled = imageFiles.Count > 1 && imageIndex > 0 && !playbackTimer.IsEnabled;
            NextImageButton.IsEnabled = imageFiles.Count > 1 && imageIndex < imageFiles.Count - 1 && !playbackTimer.IsEnabled;
            PlayButton.IsEnabled = imageFiles.Count > 1 && !playbackTimer.IsEnabled && !flowBusy;
            StopPlayButton.IsEnabled = playbackTimer.IsEnabled;
            RunOnceButton.IsEnabled = hasEnabledTools && !flowBusy;
            RunContinuousButton.IsEnabled = hasEnabledTools && !flowBusy;
            PauseRunButton.IsEnabled = flowBusy;
            PauseRunButton.Content = isPauseRequested ? "▶ 继续" : "Ⅱ 暂停";
            StopRunButton.IsEnabled = flowBusy;
            ToggleBreakpointButton.IsEnabled = hasSelectedFlowTools && !flowBusy;
            ClearBreakpointsButton.IsEnabled = flowTools.Any(item => item.IsBreakpoint) && !flowBusy;
            DebugStepButton.IsEnabled = hasEnabledTools && (!flowBusy || isFlowExecutionActive && isPauseRequested);
            DebugContinueButton.IsEnabled = isFlowExecutionActive && isPauseRequested;
            if (FlowDebugStateText != null)
            {
                int breakpointCount = flowTools.Count(item => item.IsBreakpoint);
                VmToolInstance pointer = flowTools.FirstOrDefault(item => item.IsExecutionPointer);
                FlowDebugStateText.Text = "断点 " + breakpointCount.ToString(CultureInfo.InvariantCulture) +
                    (pointer == null ? " · 指针 --" : " · 下一执行 #" + pointer.SequenceText + " " + pointer.InstanceName);
            }
            ContinuousIntervalTextBox.IsEnabled = !flowBusy;
            FlowTimeoutTextBox.IsEnabled = !flowBusy;
            StopOnNgCheckBox.IsEnabled = !flowBusy;
            ClearOverlayButton.IsEnabled = hasImage;
            SaveScreenshotButton.IsEnabled = hasImage;

            RectangleRoiButton.IsEnabled = hasImage && !flowBusy;
            CircleRoiButton.IsEnabled = hasImage && !flowBusy;
            PolygonRoiButton.IsEnabled = hasImage && !flowBusy;
            VmRoiLayer selectedEditableRoi = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
            CopySelectedRoiButton.IsEnabled = selectedEditableRoi != null && hasImage && !flowBusy;
            ToggleSelectedRoiLockButton.IsEnabled = selectedEditableRoi != null && !flowBusy;
            ClearRoiButton.IsEnabled = selectedEditableRoi != null && !selectedEditableRoi.IsLocked;
            ClearAllRoiLayersButton.IsEnabled = hasRoiLayers && !roiLayers.Any(item => item.IsLocked);
            ConfirmRoiButton.IsEnabled = hasPendingRoi;
            FitImageButton.IsEnabled = hasImage;
            TemplateSettingsButton.IsEnabled = hasGlobalImage && hasRoi && !flowBusy;
            SaveTemplateButton.IsEnabled = hasTemplate;
            LoadTemplateButton.IsEnabled = !flowBusy;
            VmToolInstance shapeTool = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ShapeMatch);
            RunMatchButton.IsEnabled = hasGlobalImage && hasTemplate && shapeTool != null && GetBoundRoiLayers(shapeTool).Count > 0 && !flowBusy;

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
            ImageContextSourceText.Text = "来源：" + imageContext.SourceText;
            ImageContextDetailText.Text = imageContext.DetailText;
            HalconHost.Visibility = hasImage ? Visibility.Visible : Visibility.Collapsed;
            ImageEmptyStateBorder.Visibility = hasImage ? Visibility.Collapsed : Visibility.Visible;
            ImageEmptyStateText.Text = imageContext.StateText + "\n请选择全局输入，或先运行并选择具有 Image 输出/关联图像的模块。";
            OverlayStatusText.Text = BuildResultOverlayLegendText();
            int visibleRoiCount = roiLayers.Count(item => item.IsVisible && item.Geometry != null);
            RoiOverlayLegendText.Text = showRoiOverlay
                ? "ROI：" + visibleRoiCount.ToString(CultureInfo.InvariantCulture) + " 个 · 青/绿"
                : "ROI：已隐藏 · 图层/绑定保留";
            if (!hasImage)
            {
                ImagePixelStatusText.Text = "像素：--";
            }

            VmRoiLayer selectedRoiLayer = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
            RoiStatusText.Text = hasPendingRoi
                ? "ROI：正在绘制，等待确认"
                : (selectedRoiLayer == null
                    ? "ROI：0 个图层"
                    : "ROI：共 " + roiLayers.Count.ToString(CultureInfo.InvariantCulture) + " 个，当前 " + selectedRoiLayer.Name + " · " + selectedRoiLayer.BindingSummary);
            TemplateStatusText.Text = hasTemplate ? "模板：已训练/加载，" + currentTemplateItem.Name : "模板：未训练";
            MatchResultText.Text = currentMatches.Count == 0 ? MatchResultText.Text : MatchResultText.Text;

            ModeStatusText.Text = isPauseRequested
                ? (isDebugBreakpointPause ? "模式：断点调试" : (debugPauseBeforeNextTool ? "模式：单步调试" : "模式：流程暂停"))
                : (isContinuousRunning
                    ? "模式：连续运行"
                    : (isFlowExecutionActive ? "模式：单次运行" : (playbackTimer.IsEnabled ? "模式：图片播放" : "模式：手动调试")));
            RunModeText.Text = ModeStatusText.Text;
            ImageStatusText.Text = hasImage ? string.Format("图像：{0}x{1}", imageContext.Width, imageContext.Height) : "图像：--";
            ZoomStatusText.Text = hasImage ? viewport.ZoomText.Replace("Zoom", "缩放") : "缩放：--";
            RoiStatusBarText.Text = hasRoiLayers ? "ROI：" + roiLayers.Count.ToString(CultureInfo.InvariantCulture) + " 图层" : "ROI：未设置";
            RoiLayerCountText.Text = roiLayers.Count.ToString(CultureInfo.InvariantCulture);
            TemplateStatusBarText.Text = hasTemplate ? "模板：" + currentTemplateItem.Name : "模板：未训练";
            TcpStatusBarText.Text = canSend ? "TCP：可发送" : "TCP：未连接";

            MetricOkText.Text = runtimeStatistics.OkCount.ToString(CultureInfo.InvariantCulture);
            MetricNgText.Text = runtimeStatistics.NgCount.ToString(CultureInfo.InvariantCulture);
            MetricYieldText.Text = runtimeStatistics.TotalCount == 0 ? "--" : runtimeStatistics.YieldRate.ToString("F1", CultureInfo.InvariantCulture) + "%";
            MetricCycleText.Text = runtimeStatistics.LastCycleMilliseconds <= 0 ? "--" : runtimeStatistics.LastCycleMilliseconds.ToString("F1", CultureInfo.InvariantCulture) + "ms";

            SetRecipeDirtyState(isRecipeDirty, RecipeWorkspaceStatusText == null ? null : RecipeWorkspaceStatusText.Text);
            RefreshRecoveryWorkspace();
            RefreshInspector();
            ScheduleRecipeStateCheck();
        }

        private void SetRoiTool(VisionTool tool, string hint)
        {
            EnsureDisplayImage();
            FinishRoiGeometryEdit(false);
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
            toolOverlayProducerToolId = null;
            toolOverlaySourceText = null;
            toolOverlayColorText = null;
            toolOverlayResultCode = null;
            toolOverlayObjectCount = 0;
            toolOverlayArea = 0;
        }

        private string BuildResultOverlayLegendText()
        {
            bool hasOverlay = toolOverlayRegion != null || toolOverlayContours != null || currentMatches.Count > 0;
            if (!showResultOverlay)
            {
                return hasOverlay ? "结果：已隐藏 · 输出快照保留" : "结果：已隐藏 · 当前无输出";
            }

            if (toolOverlayRegion == null && toolOverlayContours == null && currentMatches.Count > 0)
            {
                VmToolInstance selected = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
                string source = selected == null ? "Shape 匹配结果" : selected.InstanceName + ".Matches";
                return source + " · 绿色 · 对象 " + currentMatches.Count.ToString(CultureInfo.InvariantCulture) + " · OK";
            }

            if (toolOverlayRegion == null && toolOverlayContours == null)
            {
                return "结果：无可用叠加 · 等待模块运行";
            }

            VmToolInstance producer = string.IsNullOrWhiteSpace(toolOverlayProducerToolId)
                ? null
                : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, toolOverlayProducerToolId, StringComparison.OrdinalIgnoreCase));
            string state = producer != null && string.Equals(producer.ResultCode, "--", StringComparison.OrdinalIgnoreCase)
                ? "失效 · 请重新运行"
                : (producer == null && !string.IsNullOrWhiteSpace(toolOverlayProducerToolId)
                    ? "失效 · 来源已删除"
                    : (string.IsNullOrWhiteSpace(toolOverlayResultCode) ? "--" : toolOverlayResultCode));
            string areaText = string.Equals(toolOverlayColorText, "黄色", StringComparison.Ordinal)
                ? " · Area " + toolOverlayArea.ToString("0.###", CultureInfo.InvariantCulture)
                : (string.Equals(toolOverlayColorText, "青色", StringComparison.Ordinal) && toolOverlayArea > 0
                    ? " · Length " + toolOverlayArea.ToString("0.###", CultureInfo.InvariantCulture)
                    : string.Empty);
            return string.Format(
                CultureInfo.InvariantCulture,
                "结果：{0} · {1} · 对象 {2}{3} · {4}",
                string.IsNullOrWhiteSpace(toolOverlaySourceText) ? "最近结果" : toolOverlaySourceText,
                string.IsNullOrWhiteSpace(toolOverlayColorText) ? "默认色" : toolOverlayColorText,
                toolOverlayObjectCount,
                areaText,
                state);
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
            if (!isContinuousRunning && !runTimer.IsEnabled && !isFlowExecutionActive)
            {
                HeaderStatusText.Text = "当前没有运行中的流程。";
                return;
            }

            isStopRequested = true;
            isPauseRequested = false;
            isDebugBreakpointPause = false;
            debugSingleStepRequested = false;
            debugPauseBeforeNextTool = false;
            isContinuousRunning = false;
            runTimer.Stop();
            LogInfo(isFlowExecutionActive ? "已请求停止，将在当前工具完成后生效。" : "连续运行已停止。");
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
                RecipeWorkspaceTabs.SelectedIndex = 3;
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
            VmFlowRunPolicy flowPolicy = (recipe.FlowRunPolicy ?? new VmFlowRunPolicy()).Normalize();
            ContinuousIntervalTextBox.Text = flowPolicy.ContinuousIntervalMilliseconds.ToString(CultureInfo.InvariantCulture);
            FlowTimeoutTextBox.Text = flowPolicy.FlowTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture);
            StopOnNgCheckBox.IsChecked = flowPolicy.StopOnNg;
            runTimer.Interval = TimeSpan.FromMilliseconds(flowPolicy.ContinuousIntervalMilliseconds);

            LoadRoiLayersFromRecipe(recipe);

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
            ResetFlowEditHistory();

            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private VisionRecipe CaptureRecipe()
        {
            VmToolParameterData blobParameters = GetParametersForLegacyField(VmToolKind.Blob);
            VmToolParameterData grayParameters = GetParametersForLegacyField(VmToolKind.GrayStat);
            VmToolParameterData edgeParameters = GetParametersForLegacyField(VmToolKind.EdgeMeasure);
            return new VisionRecipe
            {
                Name = string.IsNullOrWhiteSpace(RecipeNameEditTextBox.Text) ? "DefaultRecipe" : RecipeNameEditTextBox.Text.Trim(),
                LastImageDirectory = string.IsNullOrWhiteSpace(currentImagePath) ? string.Empty : Path.GetDirectoryName(currentImagePath),
                SearchRoi = ToRecipeRoi(GetLegacySearchRoi()),
                TemplatePath = currentTemplateItem == null ? string.Empty : currentTemplateItem.TemplatePath,
                TemplateOptions = ToRecipeOptions(currentTemplateItem == null ? null : currentTemplateItem.Options),
                EnableShapeMatch = HasEnabledTool(VmToolKind.ShapeMatch),
                EnableBlob = HasEnabledTool(VmToolKind.Blob),
                EnableGrayStat = HasEnabledTool(VmToolKind.GrayStat),
                EnableEdgeMeasure = HasEnabledTool(VmToolKind.EdgeMeasure),
                EnableHDevelop = HasEnabledTool(VmToolKind.HDevelop),
                BlobMinGray = blobParameters.BlobMinGray,
                BlobMaxGray = blobParameters.BlobMaxGray,
                BlobMinArea = blobParameters.BlobMinArea,
                GrayMin = grayParameters.GrayMin,
                GrayMax = grayParameters.GrayMax,
                EdgeThreshold = edgeParameters.EdgeThreshold,
                HDevelopPath = HDevPathTextBox.Text,
                ProcedureName = HDevProcedureTextBox.Text,
                TcpMode = TcpServerModeRadio.IsChecked == true ? "Server" : "Client",
                TcpIp = TcpIpTextBox.Text,
                TcpPort = ReadTcpPortOrDefault(9000),
                TcpEncoding = GetTcpEncodingText(),
                AutoSendResult = AutoSendMatchResultCheckBox.IsChecked == true,
                ToolFlow = CaptureFlowRecipe(),
                RoiLayers = CaptureRoiLayers(),
                FlowRunPolicy = ReadFlowRunPolicyFromUi()
            };
        }

        private VmToolParameterData GetParametersForLegacyField(VmToolKind kind)
        {
            VmToolInstance tool = flowTools.FirstOrDefault(item => item.Kind == kind && item.Parameters != null);
            return tool == null ? new VmToolParameterData() : tool.Parameters;
        }

        private void LoadRoiLayersFromRecipe(VisionRecipe recipe)
        {
            DisposeCurrentRoi();
            DisposeRoiLayers();

            if (recipe != null && recipe.RoiLayers != null)
            {
                foreach (RoiLayerRecipeData item in recipe.RoiLayers)
                {
                    RoiData geometry = item == null ? null : FromRecipeRoi(item.Geometry);
                    if (geometry == null)
                    {
                        continue;
                    }

                    roiLayers.Add(new VmRoiLayer
                    {
                        RoiId = string.IsNullOrWhiteSpace(item.RoiId) ? Guid.NewGuid().ToString("N") : item.RoiId,
                        Name = string.IsNullOrWhiteSpace(item.Name) ? CreateUniqueRoiName() : item.Name,
                        IsEnabled = item.IsEnabled,
                        IsVisible = item.IsVisible,
                        IsLocked = item.IsLocked,
                        Geometry = geometry
                    });
                }
            }

            if (roiLayers.Count == 0 && recipe != null && recipe.SearchRoi != null)
            {
                RoiData legacyGeometry = FromRecipeRoi(recipe.SearchRoi);
                if (legacyGeometry != null)
                {
                    roiLayers.Add(new VmRoiLayer
                    {
                        RoiId = "legacy-search-roi",
                        Name = "搜索 ROI 01",
                        IsEnabled = true,
                        IsVisible = true,
                        IsLocked = false,
                        Geometry = legacyGeometry
                    });
                }
            }

            RefreshRoiLayerSequence();
            VmRoiLayer first = roiLayers.FirstOrDefault();
            if (RoiLayerList != null)
            {
                RoiLayerList.SelectedItem = first;
            }
            SelectRoiLayer(first);
            RefreshRoiLayerBindingSummaries();
        }

        private List<RoiLayerRecipeData> CaptureRoiLayers()
        {
            return roiLayers
                .Where(item => item != null && item.Geometry != null)
                .Select(item => new RoiLayerRecipeData
                {
                    RoiId = item.RoiId,
                    Name = item.Name,
                    IsEnabled = item.IsEnabled,
                    IsVisible = item.IsVisible,
                    IsLocked = item.IsLocked,
                    Geometry = ToRecipeRoi(item.Geometry)
                })
                .ToList();
        }

        private RoiData GetLegacySearchRoi()
        {
            VmRoiLayer layer = roiLayers.FirstOrDefault(item => item.IsEnabled && item.Geometry != null) ?? roiLayers.FirstOrDefault(item => item.Geometry != null);
            return layer == null ? currentRoi : layer.Geometry;
        }

        private void DisposeRoiLayers()
        {
            foreach (VmRoiLayer layer in roiLayers.ToList())
            {
                layer.Dispose();
            }

            roiLayers.Clear();
            roiBindingRows.Clear();
        }

        private void RefreshRoiLayerSequence()
        {
            for (int index = 0; index < roiLayers.Count; index++)
            {
                roiLayers[index].Sequence = index + 1;
            }

            if (RoiLayerCountText != null)
            {
                RoiLayerCountText.Text = roiLayers.Count.ToString(CultureInfo.InvariantCulture);
            }
        }

        private string CreateUniqueRoiName()
        {
            int index = 1;
            string candidate;
            do
            {
                candidate = "ROI " + index.ToString("00", CultureInfo.InvariantCulture);
                index++;
            }
            while (roiLayers.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)));

            return candidate;
        }

        private string CreateUniqueRoiCopyName(string sourceName)
        {
            string baseName = (string.IsNullOrWhiteSpace(sourceName) ? "ROI" : sourceName.Trim()) + " 副本";
            string candidate = baseName;
            int index = 2;
            while (roiLayers.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = baseName + " " + index.ToString("00", CultureInfo.InvariantCulture);
                index++;
            }
            return candidate;
        }

        private VmRoiLayer AddRoiLayer(RoiData geometry, string name, VmToolInstance bindTool)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException("geometry");
            }

            VmRoiLayer layer = new VmRoiLayer
            {
                RoiId = Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(name) ? CreateUniqueRoiName() : name,
                Geometry = geometry.Clone(),
                IsEnabled = true,
                IsVisible = true
            };
            roiLayers.Add(layer);
            RefreshRoiLayerSequence();

            if (bindTool != null && ToolMetadata.SupportsRoi(bindTool.Kind))
            {
                bindTool.BindRoi(layer.RoiId);
            }

            if (RoiLayerList != null)
            {
                RoiLayerList.SelectedItem = layer;
                RoiLayerList.ScrollIntoView(layer);
            }
            SelectRoiLayer(layer);
            RefreshRoiLayerBindingSummaries();
            return layer;
        }

        private void SelectRoiLayer(VmRoiLayer layer)
        {
            DisposeCurrentRoi();
            currentRoi = layer == null || layer.Geometry == null ? null : layer.Geometry.Clone();
            if (SelectedRoiLayerNameTextBox != null)
            {
                SelectedRoiLayerNameTextBox.IsEnabled = layer != null;
                SelectedRoiLayerNameTextBox.Text = layer == null ? string.Empty : layer.Name;
                SelectedRoiGeometryText.Text = layer == null ? "--" : layer.GeometryText;
                SelectedRoiLockCheckBox.IsEnabled = layer != null;
                SelectedRoiLockCheckBox.IsChecked = layer != null && layer.IsLocked;
                ToggleSelectedRoiLockButton.Content = layer != null && layer.IsLocked ? "解锁" : "锁定";
                RoiSelectedLayerText.Text = layer == null
                    ? "未选择 ROI 图层"
                    : layer.SequenceText + "  " + layer.Name + " · " + layer.ShapeText + " · " + layer.LockText;
                if (RoiEditGuidanceText != null)
                {
                    RoiEditGuidanceText.Text = GetRoiEditGuidance(layer);
                }
            }
        }

        private static string GetRoiEditGuidance(VmRoiLayer layer)
        {
            if (layer == null || layer.Geometry == null)
            {
                return "选择模式：单击 ROI 后拖动内部移动，拖动控制点编辑几何。";
            }
            if (layer.IsLocked)
            {
                return "该 ROI 已锁定：可查看和运行，但不能移动、调整或删除。";
            }
            if (layer.Geometry.ShapeType == RoiShapeType.RotatedRectangle)
            {
                return "绿色中心移动；绿色轴端调整宽高；橙色圆点旋转。";
            }
            if (layer.Geometry.ShapeType == RoiShapeType.Polygon)
            {
                return "拖动内部整体移动；拖动带编号的绿色顶点精细调整；自相交或过小几何会被拒绝。";
            }
            if (layer.Geometry.ShapeType == RoiShapeType.Circle)
            {
                return "拖动内部移动；拖动圆周绿色控制点调整半径。";
            }
            return "拖动内部移动；拖动边角绿色控制点调整矩形尺寸。";
        }

        private void BindLegacyRoiToFlowTools()
        {
            VmRoiLayer layer = roiLayers.FirstOrDefault(item => item.IsEnabled && item.Geometry != null) ?? roiLayers.FirstOrDefault(item => item.Geometry != null);
            if (layer == null)
            {
                return;
            }

            foreach (VmToolInstance tool in flowTools.Where(item => ToolMetadata.SupportsRoi(item.Kind) && item.BoundRoiIds.Count == 0))
            {
                tool.BindRoi(layer.RoiId);
            }

            RefreshRoiLayerBindingSummaries();
        }

        private List<VmRoiLayer> GetBoundRoiLayers(VmToolInstance tool)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind))
            {
                return new List<VmRoiLayer>();
            }

            return roiLayers
                .Where(layer => layer.IsEnabled && layer.Geometry != null && tool.IsRoiBound(layer.RoiId))
                .ToList();
        }

        private RoiData GetPrimaryBoundRoi(VmToolInstance tool)
        {
            VmRoiLayer layer = GetBoundRoiLayers(tool).FirstOrDefault();
            return layer == null ? null : layer.Geometry;
        }

        private HRegion CreateBoundRoiRegion(VmToolInstance tool, bool required)
        {
            List<VmRoiLayer> layers = GetBoundRoiLayers(tool);
            if (layers.Count == 0)
            {
                if (required)
                {
                    throw new InvalidOperationException(tool.InstanceName + " 尚未绑定已启用的 ROI。请在图像/ROI 页勾选至少一个图层。");
                }

                return null;
            }

            HObject combined = null;
            try
            {
                HOperatorSet.CopyObj(layers[0].Geometry.Region, out combined, 1, -1);
                for (int index = 1; index < layers.Count; index++)
                {
                    HObject merged;
                    HOperatorSet.Union2(combined, layers[index].Geometry.Region, out merged);
                    combined.Dispose();
                    combined = merged;
                }

                HRegion result = new HRegion(combined);
                combined = null;
                return result;
            }
            finally
            {
                DisposeObject(combined);
            }
        }

        private string GetRoiBindingSummary(VmToolInstance tool)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind))
            {
                return "不使用 ROI";
            }

            VmToolInputBindingData binding = tool.GetInputBinding("ROI");
            if (binding != null)
            {
                VmToolInstance source = GetInputSourceTool(binding);
                return source == null
                    ? "Region 订阅无效"
                    : "Region ← " + source.InstanceName + "." + binding.SourcePortName;
            }

            List<VmRoiLayer> bound = GetBoundRoiLayers(tool);
            return bound.Count == 0
                ? (ToolMetadata.RequiresRoi(tool.Kind) ? "ROI 未绑定" : "全图运行 · ROI 可选")
                : "ROI ×" + bound.Count.ToString(CultureInfo.InvariantCulture) + " · " + string.Join(", ", bound.Select(item => item.Name));
        }

        private void RefreshRoiLayerBindingSummaries()
        {
            foreach (VmRoiLayer layer in roiLayers)
            {
                List<string> names = flowTools
                    .Where(tool => tool.IsRoiBound(layer.RoiId))
                    .Select(tool => tool.InstanceName)
                    .ToList();
                layer.BindingSummary = names.Count == 0 ? "未绑定工具" : string.Join(", ", names);
            }

            RefreshRoiBindingEditor();
        }

        private void RefreshRoiBindingEditor()
        {
            if (RoiBindingItemsControl == null || FlowToolList == null)
            {
                return;
            }

            VmToolInstance tool = FlowToolList.SelectedItem as VmToolInstance;
            bool canBind = tool != null && ToolMetadata.SupportsRoi(tool.Kind);
            roiBindingUpdating = true;
            try
            {
                roiBindingRows.Clear();
                foreach (VmRoiLayer layer in roiLayers)
                {
                    roiBindingRows.Add(new VmRoiBindingItem
                    {
                        Layer = layer,
                        IsBound = canBind && tool.IsRoiBound(layer.RoiId)
                    });
                }
            }
            finally
            {
                roiBindingUpdating = false;
            }

            RoiBindingItemsControl.IsEnabled = canBind;
            RoiBindingToolText.Text = canBind
                ? tool.SequenceText + "  " + tool.InstanceName
                : (tool == null ? "请先选择流程中的视觉工具" : tool.InstanceName + " 不使用 ROI");
            RoiBindingsStatusText.Text = canBind
                ? GetRoiBindingSummary(tool) + "。" + GetRoiExecutionModeSummary(tool)
                : "数值与非视觉工具没有 ROI 输入。";
        }

        private static string GetRoiExecutionModeSummary(VmToolInstance tool)
        {
            if (tool == null || (tool.Kind != VmToolKind.Blob && tool.Kind != VmToolKind.GrayStat && tool.Kind != VmToolKind.EdgeMeasure))
            {
                return "运行时使用绑定区域。";
            }

            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            return VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi
                ? "逐 ROI 独立执行并输出集合结果。"
                : "运行时合并所有已启用且已绑定的区域。";
        }

        private void RefreshRoiLayerContextResults(VmToolInstance tool)
        {
            foreach (VmRoiLayer layer in roiLayers)
            {
                VmRoiRunResult result = tool == null
                    ? null
                    : tool.RoiResults.FirstOrDefault(item => string.Equals(item.RoiId, layer.RoiId, StringComparison.OrdinalIgnoreCase));
                layer.ContextResultCode = result == null ? "--" : result.ResultCode;
                layer.ContextResultText = result == null
                    ? (tool == null ? "当前工具未选择" : "当前工具无逐区结果")
                    : result.ResultCode + " · " + result.ValueText;
            }
        }

        private void InvalidateRoiDependentResults(string roiId, string reason)
        {
            foreach (VmToolInstance tool in flowTools.Where(item => item.IsRoiBound(roiId)))
            {
                InvalidateToolRunResult(tool, reason);
            }
        }

        private static void InvalidateToolRunResult(VmToolInstance tool, string reason)
        {
            if (tool == null)
            {
                return;
            }

            tool.ClearRuntimeOutputs();
            tool.ResultCode = "--";
            tool.RunStatus = "未运行";
            tool.OutputSummary = reason + "，等待重新运行";
            tool.ErrorMessage = string.Empty;
        }

        private void InvalidateDownstreamResults(VmToolInstance source, string reason)
        {
            if (source == null)
            {
                return;
            }

            HashSet<string> invalidSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { source.ToolId };
            bool changed;
            do
            {
                changed = false;
                foreach (VmToolInstance downstream in flowTools.Where(item => item != source && !invalidSourceIds.Contains(item.ToolId)))
                {
                    bool dependsOnSource = downstream.InputBindings.Any(binding => invalidSourceIds.Contains(binding.SourceToolId)) ||
                                           downstream.Kind == VmToolKind.NumericJudge && invalidSourceIds.Contains(downstream.InputToolId ?? string.Empty);
                    if (dependsOnSource)
                    {
                        InvalidateToolRunResult(downstream, reason);
                        invalidSourceIds.Add(downstream.ToolId);
                        changed = true;
                    }
                }
            }
            while (changed);
        }

        private void DisposeFlowTools()
        {
            foreach (VmToolInstance tool in flowTools.ToList())
            {
                tool.Dispose();
            }
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
                PolygonColumns = roi.PolygonColumns,
                Phi = roi.Phi,
                Length1 = roi.Length1,
                Length2 = roi.Length2
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

            if (string.Equals(data.ShapeType, RoiShapeType.RotatedRectangle.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return RoiData.CreateRotatedRectangle(data.Row, data.Column, data.Phi, data.Length1, data.Length2);
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
            favoriteToolKinds.Clear();
            foreach (string value in state.FavoriteToolKinds ?? new List<string>())
            {
                VmToolKind kind;
                if (Enum.TryParse(value, true, out kind) && toolCatalog.Any(item => item.Kind == kind))
                {
                    favoriteToolKinds.Add(kind);
                }
            }
            recentToolKinds.Clear();
            foreach (string value in state.RecentToolKinds ?? new List<string>())
            {
                VmToolKind kind;
                if (Enum.TryParse(value, true, out kind) && toolCatalog.Any(item => item.Kind == kind) && !recentToolKinds.Contains(kind))
                {
                    recentToolKinds.Add(kind);
                }
                if (recentToolKinds.Count >= 8)
                {
                    break;
                }
            }
            RefreshToolCatalogView();
            recentRecipes.Clear();
            foreach (string path in (state.RecentRecipePaths ?? new List<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8))
            {
                try
                {
                    recentRecipes.Add(new VmRecentRecipeItem { Path = Path.GetFullPath(path) });
                }
                catch
                {
                    // Ignore malformed local history entries; they are not part of the recipe itself.
                }
            }
            if (state.BottomPanelHeight >= 80)
            {
                BottomPanelRow.Height = new GridLength(state.BottomPanelHeight);
            }

            if (state.RightPanelWidth >= 390)
            {
                double maximumWidth = RootGrid.ActualWidth > 0
                    ? Math.Min(680, Math.Max(390, RootGrid.ActualWidth * 0.42))
                    : 520;
                RightPanelColumn.Width = new GridLength(Math.Min(state.RightPanelWidth, maximumWidth));
            }

            if (!string.IsNullOrWhiteSpace(state.LastRecipePath) && File.Exists(state.LastRecipePath))
            {
                try
                {
                    currentRecipePath = state.LastRecipePath;
                    ApplyRecipe(recipeService.LoadRecipe(state.LastRecipePath));
                    AddRecentRecipe(currentRecipePath);
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
                LastImageDirectory = string.IsNullOrWhiteSpace(currentImagePath) ? string.Empty : Path.GetDirectoryName(currentImagePath),
                RecentRecipePaths = recentRecipes.Select(item => item.Path).ToList(),
                FavoriteToolKinds = favoriteToolKinds.Select(item => item.ToString()).OrderBy(item => item).ToList(),
                RecentToolKinds = recentToolKinds.Select(item => item.ToString()).ToList()
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
