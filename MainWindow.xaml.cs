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
        private const string VersionStamp = "HALCON VM S03 2026-07-13";

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
        private readonly ObservableCollection<VmPortDisplayItem> inputPortRows = new ObservableCollection<VmPortDisplayItem>();
        private readonly ObservableCollection<VmPortDisplayItem> outputPortRows = new ObservableCollection<VmPortDisplayItem>();
        private readonly ObservableCollection<VmRoiLayer> roiLayers = new ObservableCollection<VmRoiLayer>();
        private readonly ObservableCollection<VmRoiBindingItem> roiBindingRows = new ObservableCollection<VmRoiBindingItem>();
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
        private bool roiBindingUpdating;

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
                CreateCatalogItem(VmToolKind.HDevelop),
                CreateCatalogItem(VmToolKind.NumericJudge)
            });
            ToolCatalogList.ItemsSource = toolCatalog;
            FlowToolList.ItemsSource = flowTools;
            InputPortsItemsControl.ItemsSource = inputPortRows;
            OutputPortsItemsControl.ItemsSource = outputPortRows;
            RoiLayerList.ItemsSource = roiLayers;
            RoiBindingItemsControl.ItemsSource = roiBindingRows;
            NumericOperatorComboBox.ItemsSource = NumericJudgeOperatorOption.CreateAll();
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

                    VmToolInstance instance = CreateFlowTool(kind, recipeItem.InstanceName, recipeItem.IsEnabled, recipeItem.ToolId);
                    ApplyToolRecipeData(instance, recipeItem);
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

        private static void ApplyToolRecipeData(VmToolInstance tool, ToolFlowRecipeItem recipeItem)
        {
            if (tool == null || recipeItem == null)
            {
                return;
            }

            tool.ReplaceRoiBindings(recipeItem.RoiIds);

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

            if (!NumericJudgeOperatorOption.IsSupported(tool.NumericOperator))
            {
                return "比较方式无效。";
            }

            if (double.IsNaN(tool.NumericLowerLimit) || double.IsInfinity(tool.NumericLowerLimit) ||
                double.IsNaN(tool.NumericUpperLimit) || double.IsInfinity(tool.NumericUpperLimit))
            {
                return "阈值必须是有限数值。";
            }

            if ((tool.NumericOperator == NumericJudgeOperatorOption.BetweenInclusive ||
                 tool.NumericOperator == NumericJudgeOperatorOption.OutsideInclusive) &&
                tool.NumericLowerLimit > tool.NumericUpperLimit)
            {
                return "区间下限不能大于上限。";
            }

            if (double.IsNaN(tool.NumericTolerance) || double.IsInfinity(tool.NumericTolerance) || tool.NumericTolerance < 0)
            {
                return "相等容差必须大于或等于 0。";
            }

            return string.Empty;
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
                    RefreshPortPanel(null);
                    RefreshRoiBindingEditor();
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
                    case VmToolKind.NumericJudge:
                        NumericJudgeInspectorPanel.Visibility = Visibility.Visible;
                        RefreshNumericJudgeEditor(selected);
                        break;
                }

                RefreshPortPanel(selected);
                RefreshRoiBindingEditor();
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

            switch (tool.Kind)
            {
                case VmToolKind.NumericJudge:
                    RefreshNumericJudgeConnectionStatus(tool);
                    break;
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
                    tool.ConfigurationStatus = currentImage == null ? "等待图像" : "就绪";
                    tool.ConnectionStatus = currentImage == null ? "系统输入 · 等待图像" : "系统输入 · 已就绪";
                    tool.ConnectionSummary = "系统.Image + " + GetRoiBindingSummary(tool);
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
                connected = currentImage != null;
                currentValue = connected
                    ? string.Format(CultureInfo.InvariantCulture, "{0}×{1}", viewport.ImageWidth, viewport.ImageHeight)
                    : "--";
                status = connected ? "已连接" : "等待图像";
            }
            else if (port.PortName == "SearchROI" || port.PortName == "ROI")
            {
                List<VmRoiLayer> boundLayers = GetBoundRoiLayers(tool);
                connected = boundLayers.Count > 0 || port.IsOptional;
                source = boundLayers.Count == 0 ? "未绑定" : string.Join(" + ", boundLayers.Select(item => item.Name));
                currentValue = boundLayers.Count == 0
                    ? "--"
                    : "Region ×" + boundLayers.Count.ToString(CultureInfo.InvariantCulture);
                status = boundLayers.Count > 0
                    ? "已连接 · 合并 " + boundLayers.Count.ToString(CultureInfo.InvariantCulture) + " 个区域"
                    : (port.IsOptional ? "可选 · 全图运行" : "等待 ROI 绑定");
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
            RightTabs.SelectedIndex = 1;
            RefreshInspector();
        }

        private void IoOpenParametersButton_Click(object sender, RoutedEventArgs e)
        {
            RightTabs.SelectedIndex = 0;
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
            DisposeRoiLayers();
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
            LogInfo("已从工具箱添加：" + instance.InstanceName);
            RefreshRoiLayerBindingSummaries();
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
            RefreshUiState();
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
            RefreshRoiLayerBindingSummaries();
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
            RefreshRoiLayerBindingSummaries();
            RefreshUiState();
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

            int index = roiLayers.IndexOf(selected);
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
            foreach (VmToolInstance tool in flowTools)
            {
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
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Select;
            HeaderStatusText.Text = "ROI 绘制已取消。";
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void SelectImageToolButton_Click(object sender, RoutedEventArgs e)
        {
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Select;
            HeaderStatusText.Text = "选择模式：滚轮缩放，右键拖动平移。";
            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private void PanImageToolButton_Click(object sender, RoutedEventArgs e)
        {
            ClearPendingRoi();
            roiEditor.Cancel();
            roiEditor.Tool = VisionTool.Pan;
            HeaderStatusText.Text = "平移模式：在图像上按住右键拖动。";
            RefreshUiState();
        }

        private void ImageOriginalSizeButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureImage();
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
                RefreshRoiLayerBindingSummaries();
                RefreshUiState();
                ScheduleRefreshDisplay();
            }
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
                foreach (VmToolInstance tool in flowTools)
                {
                    tool.ClearRuntimeOutputs();
                }

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
            tool.ClearRuntimeOutputs();
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
                        record = RunShapeMatchTool(tool, source);
                        tool.InputSummary = "Image + " + GetRoiBindingSummary(tool) + " + ShapeModel";
                        tool.OutputSummary = currentMatches.Count == 0
                            ? "Matches=0"
                            : string.Format(CultureInfo.InvariantCulture, "Matches={0}, Best={1:F3}", currentMatches.Count, currentMatches.Max(item => item.Score));
                        break;
                    case VmToolKind.Blob:
                        EnsureImage();
                        record = RunBlobTool(tool);
                        tool.InputSummary = "Image + " + GetRoiBindingSummary(tool);
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.GrayStat:
                        EnsureImage();
                        record = RunGrayStatTool(tool);
                        tool.InputSummary = "Image + " + GetRoiBindingSummary(tool);
                        tool.OutputSummary = record.Message;
                        break;
                    case VmToolKind.EdgeMeasure:
                        EnsureImage();
                        record = RunEdgeMeasureTool(tool);
                        tool.InputSummary = "Image + " + GetRoiBindingSummary(tool);
                        tool.OutputSummary = record.Message;
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
                    break;
                case VmToolKind.GrayStat:
                    tool.SetOutputValue("Mean", record.Score);
                    break;
                case VmToolKind.EdgeMeasure:
                    tool.SetOutputValue("Length", record.Score);
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

        private InspectionRecord RunBlobTool(VmToolInstance tool)
        {
            double minGray = ReadDouble(BlobMinGrayTextBox, "Blob灰度下限");
            double maxGray = ReadDouble(BlobMaxGrayTextBox, "Blob灰度上限");
            double minArea = ReadDouble(BlobMinAreaTextBox, "Blob最小面积");

            HObject thresholdRegion = null;
            HObject clippedRegion = null;
            HObject connectedRegion = null;
            HObject selectedRegion = null;
            HImage gray = null;
            HRegion roiRegion = null;
            try
            {
                gray = CreateGrayImage(currentImage);
                HOperatorSet.Threshold(gray, out thresholdRegion, minGray, maxGray);
                HObject sourceRegion = thresholdRegion;
                roiRegion = CreateBoundRoiRegion(tool, false);
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
                ReplaceToolOverlayRegion(selectedRegion);
                selectedRegion = null;

                string message = count > 0
                    ? "Blob数量：" + count + "，ROI=" + GetBoundRoiLayers(tool).Count.ToString(CultureInfo.InvariantCulture)
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

        private InspectionRecord RunGrayStatTool(VmToolInstance tool)
        {
            double min = ReadDouble(GrayMinTextBox, "灰度下限");
            double max = ReadDouble(GrayMaxTextBox, "灰度上限");
            HImage gray = null;
            HRegion region = null;
            try
            {
                gray = CreateGrayImage(currentImage);
                HTuple mean;
                HTuple deviation;
                region = CreateBoundRoiRegion(tool, false) ?? new HRegion(0.0, 0.0, (double)viewport.ImageHeight - 1.0, (double)viewport.ImageWidth - 1.0);
                HOperatorSet.Intensity(region, gray, out mean, out deviation);
                double value = mean.D;
                bool ok = value >= min && value <= max;
                InspectionRecord record = CreateRecord("GrayStat", ok ? "OK" : "NG", value, string.Format(CultureInfo.InvariantCulture, "Mean={0:F2}, Dev={1:F2}, ROI={2}", value, deviation.D, GetBoundRoiLayers(tool).Count), tool);
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

        private InspectionRecord RunEdgeMeasureTool(VmToolInstance tool)
        {
            double threshold = ReadDouble(EdgeThresholdTextBox, "边缘阈值");
            HImage gray = null;
            HObject reduced = null;
            HObject edges = null;
            HRegion roiRegion = null;
            try
            {
                gray = CreateGrayImage(currentImage);
                HObject edgeInput = gray;
                roiRegion = CreateBoundRoiRegion(tool, false);
                if (roiRegion != null)
                {
                    HOperatorSet.ReduceDomain(gray, roiRegion, out reduced);
                    edgeInput = reduced;
                }

                HOperatorSet.EdgesSubPix(edgeInput, out edges, "canny", 1.0, threshold, threshold * 2.0);
                HTuple lengths;
                HOperatorSet.LengthXld(edges, out lengths);
                double totalLength = SumTuple(lengths);
                ReplaceToolOverlayContours(edges);
                edges = null;

                InspectionRecord record = CreateRecord("EdgeMeasure", totalLength > 0 ? "OK" : "NG", totalLength, string.Format(CultureInfo.InvariantCulture, "边缘总长度={0:F1}, ROI={1}", totalLength, GetBoundRoiLayers(tool).Count), tool);
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

            VmRoiLayer selectedLayer = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
            foreach (VmRoiLayer layer in roiLayers.Where(item => item.IsVisible && item.Geometry != null))
            {
                string color = layer == selectedLayer ? "green" : (layer.IsEnabled ? "cyan" : "gray");
                overlayRenderer.DrawRoiLayer(imageWindow.HalconWindow, layer.Geometry, color, layer == selectedLayer ? 3 : 2);
            }

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
            RoiData confirmedBoundary = templateRoi;
            ShapeTemplateService service = currentTemplateItem == null ? null : currentTemplateItem.Service;
            overlayRenderer.Draw(
                imageWindow.HalconWindow,
                null,
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
            bool hasRoiLayers = roiLayers.Count > 0;
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
            ClearRoiButton.IsEnabled = RoiLayerList != null && RoiLayerList.SelectedItem != null;
            ClearAllRoiLayersButton.IsEnabled = hasRoiLayers;
            ConfirmRoiButton.IsEnabled = hasPendingRoi;
            FitImageButton.IsEnabled = hasImage;
            TemplateSettingsButton.IsEnabled = hasImage && hasRoi && !isContinuousRunning;
            SaveTemplateButton.IsEnabled = hasTemplate;
            LoadTemplateButton.IsEnabled = !isContinuousRunning;
            VmToolInstance shapeTool = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ShapeMatch);
            RunMatchButton.IsEnabled = hasImage && hasTemplate && shapeTool != null && GetBoundRoiLayers(shapeTool).Count > 0 && !isContinuousRunning;

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

            VmRoiLayer selectedRoiLayer = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
            RoiStatusText.Text = hasPendingRoi
                ? "ROI：正在绘制，等待确认"
                : (selectedRoiLayer == null
                    ? "ROI：0 个图层"
                    : "ROI：共 " + roiLayers.Count.ToString(CultureInfo.InvariantCulture) + " 个，当前 " + selectedRoiLayer.Name + " · " + selectedRoiLayer.BindingSummary);
            TemplateStatusText.Text = hasTemplate ? "模板：已训练/加载，" + currentTemplateItem.Name : "模板：未训练";
            MatchResultText.Text = currentMatches.Count == 0 ? MatchResultText.Text : MatchResultText.Text;

            ModeStatusText.Text = isContinuousRunning ? "模式：连续运行" : (playbackTimer.IsEnabled ? "模式：图片播放" : "模式：手动调试");
            RunModeText.Text = ModeStatusText.Text;
            ImageStatusText.Text = hasImage ? string.Format("图像：{0}x{1}", viewport.ImageWidth, viewport.ImageHeight) : "图像：--";
            ZoomStatusText.Text = hasImage ? viewport.ZoomText.Replace("Zoom", "缩放") : "缩放：--";
            RoiStatusBarText.Text = hasRoiLayers ? "ROI：" + roiLayers.Count.ToString(CultureInfo.InvariantCulture) + " 图层" : "ROI：未设置";
            RoiLayerCountText.Text = roiLayers.Count.ToString(CultureInfo.InvariantCulture);
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

            RefreshUiState();
            ScheduleRefreshDisplay();
        }

        private VisionRecipe CaptureRecipe()
        {
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
                ToolFlow = CaptureFlowRecipe(),
                RoiLayers = CaptureRoiLayers()
            };
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
                RoiSelectedLayerText.Text = layer == null
                    ? "未选择 ROI 图层"
                    : layer.SequenceText + "  " + layer.Name + " · " + layer.ShapeText;
            }
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
                ? GetRoiBindingSummary(tool) + "。运行时合并所有已启用且已绑定的区域。"
                : "数值与非视觉工具没有 ROI 输入。";
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
