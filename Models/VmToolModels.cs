using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmToolValueOption
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
    }

    public static class VmXldLengthSortDirection
    {
        public const string Ascending = "Ascending";
        public const string Descending = "Descending";

        public static string Normalize(string value)
        {
            return string.Equals(value, Descending, StringComparison.OrdinalIgnoreCase) ? Descending : Ascending;
        }

        public static string GetDisplayText(string value)
        {
            return Normalize(value) == Descending ? "降序（长→短）" : "升序（短→长）";
        }

        public static IList<VmToolValueOption> CreateAll()
        {
            return new[]
            {
                new VmToolValueOption { Key = Ascending, DisplayText = "升序（短→长）" },
                new VmToolValueOption { Key = Descending, DisplayText = "降序（长→短）" }
            };
        }
    }

    public static class VmXldLengthSelectionMode
    {
        public const string TopN = "TopN";
        public const string BottomN = "BottomN";
        public const string RankRange = "RankRange";

        public static string Normalize(string value)
        {
            if (string.Equals(value, BottomN, StringComparison.OrdinalIgnoreCase)) return BottomN;
            if (string.Equals(value, RankRange, StringComparison.OrdinalIgnoreCase)) return RankRange;
            return TopN;
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == BottomN) return "末 N 条";
            if (normalized == RankRange) return "排名区间";
            return "前 N 条";
        }

        public static IList<VmToolValueOption> CreateAll()
        {
            return new[]
            {
                new VmToolValueOption { Key = TopN, DisplayText = "前 N 条" },
                new VmToolValueOption { Key = BottomN, DisplayText = "末 N 条" },
                new VmToolValueOption { Key = RankRange, DisplayText = "排名区间" }
            };
        }
    }

    public static class VmXldClosureSelectionMode
    {
        public const string All = "All";
        public const string OpenOnly = "OpenOnly";
        public const string ClosedOnly = "ClosedOnly";

        public static string Normalize(string value)
        {
            if (string.Equals(value, OpenOnly, StringComparison.OrdinalIgnoreCase)) return OpenOnly;
            if (string.Equals(value, ClosedOnly, StringComparison.OrdinalIgnoreCase)) return ClosedOnly;
            return All;
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == OpenOnly) return "仅开放轮廓";
            if (normalized == ClosedOnly) return "仅闭合轮廓";
            return "开放 + 闭合";
        }

        public static IList<VmToolValueOption> CreateAll()
        {
            return new[]
            {
                new VmToolValueOption { Key = All, DisplayText = "开放 + 闭合" },
                new VmToolValueOption { Key = OpenOnly, DisplayText = "仅开放轮廓" },
                new VmToolValueOption { Key = ClosedOnly, DisplayText = "仅闭合轮廓" }
            };
        }
    }

    public static class VmXldAreaDirectionMode
    {
        public const string All = "All";
        public const string Clockwise = "Clockwise";
        public const string Counterclockwise = "Counterclockwise";

        public static string Normalize(string value)
        {
            if (string.Equals(value, Clockwise, StringComparison.OrdinalIgnoreCase)) return Clockwise;
            if (string.Equals(value, Counterclockwise, StringComparison.OrdinalIgnoreCase)) return Counterclockwise;
            return All;
        }

        public static bool IsSupported(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   string.Equals(value, All, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Clockwise, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Counterclockwise, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Clockwise) return "顺时针（HALCON negative）";
            if (normalized == Counterclockwise) return "逆时针（HALCON positive）";
            return "全部方向";
        }

        public static IList<VmToolValueOption> CreateAll()
        {
            return new[]
            {
                new VmToolValueOption { Key = All, DisplayText = "全部方向" },
                new VmToolValueOption { Key = Clockwise, DisplayText = "顺时针" },
                new VmToolValueOption { Key = Counterclockwise, DisplayText = "逆时针" }
            };
        }
    }
    public enum VmToolKind
    {
        ShapeMatch,
        Blob,
        GrayStat,
        EdgeMeasure,
        HDevelop,
        NumericJudge,
        ImageSource,
        ImageChannel,
        ImageFilter,
        ImageThreshold,
        RegionMorphology,
        RegionFeatureFilter,
        RegionSetOperation,
        ImageEnhancement,
        ImageResize,
        ImageMirror,
        ImageRotate,
        PolarImageUnwrap,
        RoiImageExtraction,
        DynamicThreshold,
        ImageDifference,
        RegionContourMeasurement,
        XldContourFilter,
        XldContourSegmentation,
        XldLineFitting,
        XldCircleFitting,
        XldEllipseFitting,
        XldRectangleFitting,
        XldLineRelationMeasurement,
        CaliperEdgePairMeasurement,
        LinearCaliperArrayMeasurement,
        CircularCaliperCircleFinder,
        RingWallThicknessMeasurement,
        ImageAffineTransform,
        ImageProjectiveTransform,
        SubpixelEdgeExtraction,
        XldAdjacentContourUnion,
        XldContourSmoothing,
        XldAffineTransform,
        XldProjectiveTransform,
        XldContourCrop,
        XldLengthClassification,
        XldLengthSortSelection,
        XldPointClosureFilter,
        XldAreaDirectionFilter,
        XldBoundingCompactnessFilter,
        XldCircularityConvexityFilter,
        XldEnclosingCircleDiameterMeasurement,
        XldMinimumRectangleDirectionMeasurement,
        XldDirectionConsistencyMeasurement,
        TcpClientExchange,
        TextAssembly,
        TextParser,
        NumericCalculation,
        ResultLogicAggregation,
        CSharpDllScript,
        GlobalVariableRead,
        GlobalVariableWrite,
        PositionCorrection2D,
        PointSetGeometryStatistics,
        PointToLineDistanceMeasurement,
        PointToCircleRadialDeviationMeasurement,
        PointToEllipseDistanceMeasurement,
        LineEllipseIntersectionMeasurement,
        LineCircleIntersectionMeasurement,
        CircleCircleIntersectionMeasurement,
        PointPairLineConstructionMeasurement,
        TypedLineLineRelationMeasurement,
        TypedPointSetLineFittingMeasurement,
        TypedPointSetCircleFittingMeasurement,
        TypedPointSetEllipseFittingMeasurement,
        DataCode2DRecognition,
        BarCode1DRecognition,
        OcrRecognition,
        AutomaticThreshold,
        LocalAdaptiveThreshold,
        GrayHistogram,
        GrayLineProfile,
        BranchValueSelection,
        SubflowCall,
        SubflowInput,
        NumberArrayCreate,
        TextArraySplit,
        NumberArrayStatistics,
        TextArraySelect,
        NumericExpression,
        NumberArrayReorder,
        TextArrayReorder,
        JsonRead,
        JsonWrite,
        ModbusTcpExchange,
        ModbusRtuExchange,
        PlanarCalibration2D,
        WorldPointPairMeasurement,
        CameraDistortionCorrection,
        ColorInspectionHsv,
        VariationSurfaceInspection,
        NccTemplateMatch,
        PoseImageAlignment,
        ImageEdgeLineFindingS143,
        ConnectedObjectAnalysisS144
    }

    public sealed class VmToolCatalogItem : INotifyPropertyChanged
    {
        private bool isFavorite;
        private int recentRank = int.MaxValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public VmToolKind Kind { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string ImplementedCategory { get { return "已实现 · " + Category; } }

        public string Description { get; set; }

        public int CategoryOrder { get; set; }

        public bool IsFavorite
        {
            get { return isFavorite; }
            set
            {
                if (isFavorite == value)
                {
                    return;
                }

                isFavorite = value;
                OnPropertyChanged("IsFavorite");
                OnPropertyChanged("FavoriteGlyph");
                OnPropertyChanged("FavoriteHint");
            }
        }

        public string FavoriteGlyph
        {
            get { return IsFavorite ? "★" : "☆"; }
        }

        public string FavoriteHint
        {
            get { return IsFavorite ? "取消收藏" : "收藏工具"; }
        }

        public int RecentRank
        {
            get { return recentRank; }
            set
            {
                if (recentRank == value)
                {
                    return;
                }

                recentRank = value;
                OnPropertyChanged("RecentRank");
                OnPropertyChanged("RecentText");
            }
        }

        public string RecentText
        {
            get { return RecentRank == int.MaxValue ? string.Empty : "最近 " + (RecentRank + 1).ToString(CultureInfo.InvariantCulture); }
        }

        public string SearchText
        {
            get { return (Category + " " + Name + " " + Description + " " + TaskSearchAliases).ToLowerInvariant(); }
        }

        private string TaskSearchAliases
        {
            get
            {
                switch (Kind)
                {
                case VmToolKind.ImageSource: return "读图 打开图片 本地图片 目录回放 相机 采集 输入图像 acquisition";
                    case VmToolKind.ImageFilter: return "去噪 降噪 平滑 均值 中值 denoise blur";
                    case VmToolKind.ImageEnhancement: return "增强 对比度 清晰 灰度拉伸 contrast";
                    case VmToolKind.ImageThreshold: return "分割 二值化 提取前景 threshold";
                    case VmToolKind.AutomaticThreshold: return "自动分割 二值化 自动阈值 otsu";
                    case VmToolKind.LocalAdaptiveThreshold: return "光照不均 局部分割 自适应二值化";
                    case VmToolKind.ColorInspectionHsv: return "颜色 色彩 色相 HSV RGB 涂层 彩色标记 覆盖率 分选 错色";
                    case VmToolKind.VariationSurfaceInspection: return "良品 学习 变化模型 缺陷 表面 划痕 污渍 印刷 缺失 variation 统计";
                    case VmToolKind.NccTemplateMatch: return "NCC 灰度 模板匹配 相关匹配 图案 定位 制作模板 旋转 极性 correlation";
                    case VmToolKind.PoseImageAlignment: return "定位 图像对齐 姿态归一化 参考姿态 配准 刚体 平移 旋转 alignment registration";
                    case VmToolKind.GrayHistogram: return "亮度 曝光 灰度分布 分位数 histogram";
                    case VmToolKind.GrayLineProfile: return "亮度 沿线 灰度曲线 剖面 光照变化 profile";
                    case VmToolKind.GrayStat: return "亮度 曝光 灰度均匀性";
                    case VmToolKind.CircularCaliperCircleFinder: return "找圆 圆心 半径 圆边缘";
                    case VmToolKind.XldCircleFitting: return "找圆 拟合圆 圆心 半径";
                    case VmToolKind.XldLineFitting: return "找线 拟合直线 方向";
                    case VmToolKind.ImageEdgeLineFindingS143: return "找线 直线 边缘 单边 鲁棒 卡尺";
                    case VmToolKind.ConnectedObjectAnalysisS144: return "连通域 对象 颗粒 多孔 漏装 中心 面积 编号 选取";
                    case VmToolKind.CaliperEdgePairMeasurement: return "测宽 边缘间距 卡尺 尺寸";
                    case VmToolKind.RegionFeatureFilter: return "面积筛选 去小块 目标筛选";
                    case VmToolKind.DataCode2DRecognition: return "扫码 读码 二维码 qr datamatrix";
                    case VmToolKind.NumericJudge: return "合格判断 范围判断 上下限 ok ng";
                    default: return string.Empty;
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public enum VmRuntimeResultOrigin { Unspecified, ModuleExecution, DraftTrial }

    public sealed class VmToolInstance : INotifyPropertyChanged, IDisposable
    {
        private string instanceName;
        private bool isEnabled;
        private string configurationStatus;
        private string runStatus;
        private string resultCode;
        private double elapsedMilliseconds;
        private string inputSummary;
        private string outputSummary;
        private string errorMessage;
        private int sequence;
        private string inputToolId;
        private string inputPortName;
        private string numericOperator;
        private double numericLowerLimit;
        private double numericUpperLimit;
        private double numericTolerance;
        private string connectionStatus;
        private string connectionSummary;
        private string dependencyState;
        private string dependencySummary;
        private bool isFlowSelected;
        private bool isFlowDetailExpanded;
        private double graphX;
        private double graphY;
        private double graphHeight = 180;
        private bool hasManualGraphPosition;
        private string groupId;
        private string groupName;
        private bool isGroupCollapsed;
        private bool isBreakpoint;
        private bool isExecutionPointer;
        private bool isTaskLensCompactS76;
        private readonly List<string> boundRoiIds = new List<string>();
        private readonly List<VmToolInputBindingData> inputBindings = new List<VmToolInputBindingData>();
        private readonly Dictionary<string, object> runtimeOutputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> runtimeOutputCoordinates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string CoordinateIdentityBase { get; set; }
        private readonly ObservableCollection<VmRoiRunResult> roiResults = new ObservableCollection<VmRoiRunResult>();
        private readonly ObservableCollection<VmFlowPortChip> flowInputPorts = new ObservableCollection<VmFlowPortChip>();
        private readonly ObservableCollection<VmFlowPortChip> flowOutputPorts = new ObservableCollection<VmFlowPortChip>();
        private long runtimeOutputVersion;
        private DateTime? lastOutputUtc;
        public VmRuntimeResultOrigin RuntimeResultOrigin { get; set; }

        public VmToolInstance()
        {
            ToolId = Guid.NewGuid().ToString("N");
            IsEnabled = true;
            ConfigurationStatus = "待配置";
            RunStatus = "未运行";
            ResultCode = "--";
            InputSummary = "--";
            OutputSummary = "--";
            ErrorMessage = string.Empty;
            NumericOperator = NumericJudgeOperatorOption.BetweenInclusive;
            NumericLowerLimit = 0;
            NumericUpperLimit = 100;
            NumericTolerance = 0.001;
            ConnectionStatus = "未检查";
            ConnectionSummary = "--";
            DependencyState = "Neutral";
            DependencySummary = "选择模块后显示直接依赖";
            Parameters = new VmToolParameterData();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ToolId { get; set; }

        public VmToolParameterData Parameters { get; set; }

        public int Sequence
        {
            get { return sequence; }
            set
            {
                if (sequence == value)
                {
                    return;
                }

                sequence = value;
                OnPropertyChanged("Sequence");
                OnPropertyChanged("SequenceText");
            }
        }

        public string SequenceText
        {
            get { return Sequence.ToString("00"); }
        }

        public VmToolKind Kind { get; set; }

        public string InstanceName
        {
            get { return instanceName; }
            set
            {
                if (instanceName == value)
                {
                    return;
                }

                instanceName = value;
                OnPropertyChanged("InstanceName");
            }
        }

        public string DisplayType
        {
            get { return ToolMetadata.GetDisplayName(Kind); }
        }

        public string Category
        {
            get { return ToolMetadata.GetCategory(Kind); }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (Kind == VmToolKind.SubflowInput) value = true;
                if (isEnabled == value)
                {
                    return;
                }

                isEnabled = value;
                OnPropertyChanged("IsEnabled");
                OnPropertyChanged("TaskLensStatusTextS76");
            }
        }

        public bool IsFlowSelected
        {
            get { return isFlowSelected; }
            set
            {
                if (isFlowSelected == value)
                {
                    return;
                }

                isFlowSelected = value;
                OnPropertyChanged("IsFlowSelected");
                OnPropertyChanged("ShowFlowDetails");
            }
        }

        public bool IsFlowDetailExpanded
        {
            get { return isFlowDetailExpanded; }
            set
            {
                if (isFlowDetailExpanded == value) return;
                isFlowDetailExpanded = value;
                OnPropertyChanged("IsFlowDetailExpanded");
                OnPropertyChanged("ShowFlowDetails");
                OnPropertyChanged("FlowDetailToggleText");
            }
        }

        public bool ShowFlowDetails
        {
            get { return IsFlowDetailExpanded; }
        }

        public string FlowDetailToggleText
        {
            get { return IsFlowDetailExpanded ? "收起" : "端口"; }
        }

        public bool IsTaskLensCompactS76
        {
            get { return isTaskLensCompactS76; }
            set
            {
                if (isTaskLensCompactS76 == value) return;
                isTaskLensCompactS76 = value;
                OnPropertyChanged("IsTaskLensCompactS76");
            }
        }

        public string GroupId
        {
            get { return groupId; }
            set
            {
                if (string.Equals(groupId, value, StringComparison.Ordinal)) return;
                groupId = value;
                OnPropertyChanged("GroupId");
                OnPropertyChanged("HasFlowGroup");
                OnPropertyChanged("GroupBadgeText");
            }
        }

        public string GroupName
        {
            get { return groupName; }
            set
            {
                if (string.Equals(groupName, value, StringComparison.Ordinal)) return;
                groupName = value;
                OnPropertyChanged("GroupName");
                OnPropertyChanged("GroupBadgeText");
            }
        }

        public bool IsGroupCollapsed
        {
            get { return isGroupCollapsed; }
            set
            {
                if (isGroupCollapsed == value) return;
                isGroupCollapsed = value;
                OnPropertyChanged("IsGroupCollapsed");
                OnPropertyChanged("GroupBadgeText");
            }
        }

        public bool HasFlowGroup
        {
            get { return !string.IsNullOrWhiteSpace(GroupId); }
        }

        public string GroupBadgeText
        {
            get
            {
                return HasFlowGroup
                    ? (IsGroupCollapsed ? "▸ " : "▾ ") + (string.IsNullOrWhiteSpace(GroupName) ? "未命名组合" : GroupName)
                    : string.Empty;
            }
        }

        public bool IsBreakpoint
        {
            get { return isBreakpoint; }
            set
            {
                if (isBreakpoint == value) return;
                isBreakpoint = value;
                OnPropertyChanged("IsBreakpoint");
                OnPropertyChanged("BreakpointBadgeText");
                OnPropertyChanged("DebugBadgeText");
            }
        }

        public bool IsExecutionPointer
        {
            get { return isExecutionPointer; }
            set
            {
                if (isExecutionPointer == value) return;
                isExecutionPointer = value;
                OnPropertyChanged("IsExecutionPointer");
                OnPropertyChanged("ExecutionBadgeText");
                OnPropertyChanged("DebugBadgeText");
            }
        }

        public string BreakpointBadgeText
        {
            get { return IsBreakpoint ? "● 断点" : string.Empty; }
        }

        public string ExecutionBadgeText
        {
            get { return IsExecutionPointer ? "▶ 下一执行" : string.Empty; }
        }

        public string DebugBadgeText
        {
            get
            {
                if (IsBreakpoint && IsExecutionPointer) return "● 断点 · ▶ 下一执行";
                if (IsExecutionPointer) return ExecutionBadgeText;
                return BreakpointBadgeText;
            }
        }

        public string ConfigurationStatus
        {
            get { return configurationStatus; }
            set
            {
                configurationStatus = value;
                OnPropertyChanged("ConfigurationStatus");
            }
        }

        public string RunStatus
        {
            get { return runStatus; }
            set
            {
                runStatus = value;
                OnPropertyChanged("RunStatus");
                OnPropertyChanged("TaskLensStatusTextS76");
            }
        }

        public string ResultCode
        {
            get { return resultCode; }
            set
            {
                resultCode = value;
                OnPropertyChanged("ResultCode");
                OnPropertyChanged("TaskLensStatusTextS76");
            }
        }

        public string TaskLensStatusTextS76
        {
            get
            {
                if (!IsEnabled) return "停用";
                if (string.Equals(ResultCode, "OK", StringComparison.OrdinalIgnoreCase)) return "OK";
                if (string.Equals(ResultCode, "NG", StringComparison.OrdinalIgnoreCase)) return "NG";
                if (!string.IsNullOrWhiteSpace(RunStatus) && !string.Equals(RunStatus, "未运行", StringComparison.OrdinalIgnoreCase)) return RunStatus;
                return "等待";
            }
        }

        public double ElapsedMilliseconds
        {
            get { return elapsedMilliseconds; }
            set
            {
                elapsedMilliseconds = value;
                OnPropertyChanged("ElapsedMilliseconds");
                OnPropertyChanged("ElapsedText");
            }
        }

        public string ElapsedText
        {
            get { return ElapsedMilliseconds <= 0 ? "--" : ElapsedMilliseconds.ToString("F1") + " ms"; }
        }

        public string InputSummary
        {
            get { return inputSummary; }
            set
            {
                inputSummary = value;
                OnPropertyChanged("InputSummary");
            }
        }

        public string OutputSummary
        {
            get { return outputSummary; }
            set
            {
                outputSummary = value;
                OnPropertyChanged("OutputSummary");
            }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        public string InputToolId
        {
            get { return inputToolId; }
            set
            {
                inputToolId = value;
                OnPropertyChanged("InputToolId");
            }
        }

        public string InputPortName
        {
            get { return inputPortName; }
            set
            {
                inputPortName = value;
                OnPropertyChanged("InputPortName");
            }
        }

        public string NumericOperator
        {
            get { return numericOperator; }
            set
            {
                numericOperator = value;
                OnPropertyChanged("NumericOperator");
            }
        }

        public double NumericLowerLimit
        {
            get { return numericLowerLimit; }
            set
            {
                numericLowerLimit = value;
                OnPropertyChanged("NumericLowerLimit");
            }
        }

        public double NumericUpperLimit
        {
            get { return numericUpperLimit; }
            set
            {
                numericUpperLimit = value;
                OnPropertyChanged("NumericUpperLimit");
            }
        }

        public double NumericTolerance
        {
            get { return numericTolerance; }
            set
            {
                numericTolerance = value;
                OnPropertyChanged("NumericTolerance");
            }
        }

        public string ConnectionStatus
        {
            get { return connectionStatus; }
            set
            {
                connectionStatus = value;
                OnPropertyChanged("ConnectionStatus");
            }
        }

        public string ConnectionSummary
        {
            get { return connectionSummary; }
            set
            {
                connectionSummary = value;
                OnPropertyChanged("ConnectionSummary");
            }
        }

        public string DependencyState
        {
            get { return dependencyState; }
            set
            {
                if (dependencyState == value)
                {
                    return;
                }

                dependencyState = value;
                OnPropertyChanged("DependencyState");
            }
        }

        public string DependencySummary
        {
            get { return dependencySummary; }
            set
            {
                if (dependencySummary == value)
                {
                    return;
                }

                dependencySummary = value;
                OnPropertyChanged("DependencySummary");
            }
        }

        public double GraphX
        {
            get { return graphX; }
            set
            {
                if (Math.Abs(graphX - value) < 0.01) return;
                graphX = value;
                OnPropertyChanged("GraphX");
                OnPropertyChanged("GraphPositionText");
            }
        }

        public double GraphY
        {
            get { return graphY; }
            set
            {
                if (Math.Abs(graphY - value) < 0.01) return;
                graphY = value;
                OnPropertyChanged("GraphY");
                OnPropertyChanged("GraphPositionText");
            }
        }

        public bool HasManualGraphPosition
        {
            get { return hasManualGraphPosition; }
            set
            {
                if (hasManualGraphPosition == value) return;
                hasManualGraphPosition = value;
                OnPropertyChanged("HasManualGraphPosition");
            }
        }

        public string GraphPositionText
        {
            get { return string.Format(CultureInfo.InvariantCulture, "X {0:0}  Y {1:0}", GraphX, GraphY); }
        }

        public double GraphHeight
        {
            get { return graphHeight; }
            set
            {
                if (Math.Abs(graphHeight - value) < 0.01) return;
                graphHeight = value;
                OnPropertyChanged("GraphHeight");
            }
        }

        public ObservableCollection<VmFlowPortChip> FlowInputPorts
        {
            get { return flowInputPorts; }
        }

        public ObservableCollection<VmFlowPortChip> FlowOutputPorts
        {
            get { return flowOutputPorts; }
        }

        public IList<string> BoundRoiIds
        {
            get { return boundRoiIds; }
        }

        public ObservableCollection<VmRoiRunResult> RoiResults
        {
            get { return roiResults; }
        }

        public IList<VmToolInputBindingData> InputBindings
        {
            get { return inputBindings; }
        }

        public VmToolInputBindingData GetInputBinding(string targetPortName)
        {
            return inputBindings.FirstOrDefault(item => string.Equals(item.TargetPortName, targetPortName, StringComparison.OrdinalIgnoreCase));
        }

        public void SetInputBinding(string targetPortName, string sourceToolId, string sourcePortName)
        {
            RemoveInputBinding(targetPortName);
            if (string.IsNullOrWhiteSpace(targetPortName) || string.IsNullOrWhiteSpace(sourceToolId) || string.IsNullOrWhiteSpace(sourcePortName))
            {
                return;
            }

            inputBindings.Add(new VmToolInputBindingData
            {
                TargetPortName = targetPortName.Trim(),
                SourceToolId = sourceToolId.Trim(),
                SourcePortName = sourcePortName.Trim()
            });
            OnPropertyChanged("InputBindings");
        }

        public void RemoveInputBinding(string targetPortName)
        {
            VmToolInputBindingData existing = GetInputBinding(targetPortName);
            if (existing == null)
            {
                return;
            }

            inputBindings.Remove(existing);
            OnPropertyChanged("InputBindings");
        }

        public void ReplaceInputBindings(IEnumerable<VmToolInputBindingData> bindings)
        {
            List<VmToolInputBindingData> replacements = bindings == null
                ? new List<VmToolInputBindingData>()
                : bindings.Where(item => item != null && !string.IsNullOrWhiteSpace(item.TargetPortName)).Select(item => item.Clone()).ToList();
            inputBindings.Clear();
            foreach (VmToolInputBindingData binding in replacements)
            {
                SetInputBinding(binding.TargetPortName, binding.SourceToolId, binding.SourcePortName);
            }

            OnPropertyChanged("InputBindings");
        }

        public void ReplaceRoiResults(IEnumerable<VmRoiRunResult> results)
        {
            roiResults.Clear();
            if (results != null)
            {
                foreach (VmRoiRunResult result in results)
                {
                    roiResults.Add(result);
                }
            }

            OnPropertyChanged("RoiResults");
        }

        public bool IsRoiBound(string roiId)
        {
            return !string.IsNullOrWhiteSpace(roiId) && boundRoiIds.Any(item => string.Equals(item, roiId, StringComparison.OrdinalIgnoreCase));
        }

        public void BindRoi(string roiId)
        {
            if (string.IsNullOrWhiteSpace(roiId) || IsRoiBound(roiId))
            {
                return;
            }

            boundRoiIds.Add(roiId);
            OnPropertyChanged("BoundRoiIds");
        }

        public void UnbindRoi(string roiId)
        {
            string existing = boundRoiIds.FirstOrDefault(item => string.Equals(item, roiId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                return;
            }

            boundRoiIds.Remove(existing);
            OnPropertyChanged("BoundRoiIds");
        }

        public void ReplaceRoiBindings(IEnumerable<string> roiIds)
        {
            boundRoiIds.Clear();
            if (roiIds != null)
            {
                foreach (string roiId in roiIds.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    boundRoiIds.Add(roiId);
                }
            }

            OnPropertyChanged("BoundRoiIds");
        }

        public void ClearRuntimeOutputs()
        {
            RuntimeResultOrigin = VmRuntimeResultOrigin.Unspecified;
            ElapsedMilliseconds = 0;
            GrayHistogramResult = null;
            GrayLineProfileResult = null;
            foreach (IDisposable disposable in runtimeOutputs.Values.OfType<IDisposable>().Distinct().ToList())
            {
                disposable.Dispose();
            }
            runtimeOutputs.Clear();
            runtimeOutputCoordinates.Clear();
            roiResults.Clear();
            runtimeOutputVersion++;
            lastOutputUtc = null;
        }

        public void SetOutputValue(string portName, object value)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                return;
            }

            object existing;
            if (runtimeOutputs.TryGetValue(portName, out existing) && !ReferenceEquals(existing, value))
            {
                IDisposable disposable = existing as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            runtimeOutputs[portName] = value;
            runtimeOutputCoordinates.Remove(portName);
            lastOutputUtc = DateTime.UtcNow;
        }

        public void SetOutputCoordinateContext(string portName, string context)
        {
            if (!string.IsNullOrWhiteSpace(portName) && !string.IsNullOrWhiteSpace(context)) runtimeOutputCoordinates[portName] = context;
        }

        public bool TryGetOutputCoordinateContext(string portName, out string context)
        {
            return runtimeOutputCoordinates.TryGetValue(portName ?? string.Empty, out context);
        }

        public VmGrayHistogramResult GrayHistogramResult { get; set; }
        public HalconWinFormsDemo.Services.GrayLineProfileResult GrayLineProfileResult { get; set; }
        public long RuntimeOutputVersion { get { return runtimeOutputVersion; } }

        public DateTime? LastOutputUtc { get { return lastOutputUtc; } }

        public bool TryGetOutputValue(string portName, out object value)
        {
            return runtimeOutputs.TryGetValue(portName ?? string.Empty, out value);
        }

        public bool TryGetNumericOutput(string portName, out double value)
        {
            value = 0;
            object raw;
            if (!TryGetOutputValue(portName, out raw) || raw == null)
            {
                return false;
            }

            if (raw is double)
            {
                value = (double)raw;
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }

            try
            {
                value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
            catch
            {
                return false;
            }
        }

        public string GetFormattedOutput(string portName)
        {
            object value;
            if (!TryGetOutputValue(portName, out value) || value == null)
            {
                return "--";
            }

            if (value is double)
            {
                return ((double)value).ToString("0.###", CultureInfo.InvariantCulture);
            }

            if (value is bool)
            {
                return (bool)value ? "True" : "False";
            }

            VmRegionSnapshot regionSnapshot = value as VmRegionSnapshot;
            if (regionSnapshot != null)
            {
                return regionSnapshot.DisplayText;
            }

            VmImageSnapshot imageSnapshot = value as VmImageSnapshot;
            if (imageSnapshot != null)
            {
                return imageSnapshot.DisplayText;
            }

            VmXldSnapshot xldSnapshot = value as VmXldSnapshot;
            if (xldSnapshot != null)
            {
                return xldSnapshot.DisplayText;
            }

            VmPose2D pose = value as VmPose2D;
            if (pose != null)
            {
                return pose.ToDisplayString();
            }

            VmPoint2D point = value as VmPoint2D;
            if (point != null)
            {
                return point.ToDisplayString();
            }

            VmWorldPoint2DS124 worldPoint = value as VmWorldPoint2DS124;
            if (worldPoint != null)
            {
                return worldPoint.ToDisplayString();
            }

            VmPoint2DArray points = value as VmPoint2DArray;
            if (points != null)
            {
                return points.ToDisplayString();
            }

            VmLine2D line = value as VmLine2D;
            if (line != null)
            {
                return line.ToDisplayString();
            }

            VmCircle2D circle = value as VmCircle2D;
            if (circle != null)
            {
                return circle.ToDisplayString();
            }

            VmEllipse2D ellipse = value as VmEllipse2D;
            if (ellipse != null)
            {
                return ellipse.ToDisplayString();
            }

            IEnumerable<VmRoiRunResult> roiResultItems = value as IEnumerable<VmRoiRunResult>;
            if (roiResultItems != null)
            {
                return "逐 ROI 结果 ×" + roiResultItems.Count().ToString(CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public void Dispose()
        {
            ClearRuntimeOutputs();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public static class ToolMetadata
    {
        public static string GetDisplayName(VmToolKind kind)
        {
            if (kind == VmToolKind.ConnectedObjectAnalysisS144) return "连通域对象分析";
            if (kind == VmToolKind.ImageEdgeLineFindingS143) return "边缘找直线";
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return "图像源";
                case VmToolKind.ImageChannel:
                    return "图像通道";
                case VmToolKind.ImageFilter:
                    return "图像滤波";
                case VmToolKind.ImageEnhancement:
                    return "图像增强";
                case VmToolKind.ImageResize:
                    return "图像尺寸缩放";
                case VmToolKind.ImageMirror:
                    return "图像镜像";
                case VmToolKind.ImageRotate:
                    return "图像旋转";
                case VmToolKind.ImageAffineTransform:
                    return "仿射图像变换";
                case VmToolKind.ImageProjectiveTransform:
                    return "透视图像校正";
                case VmToolKind.CameraDistortionCorrection:
                    return "镜头畸变校正";
                case VmToolKind.PoseImageAlignment:
                    return "定位图像对齐";
                case VmToolKind.SubpixelEdgeExtraction:
                    return "亚像素边缘提取";
                case VmToolKind.XldAdjacentContourUnion:
                    return "相邻轮廓连接";
                case VmToolKind.XldContourSmoothing:
                    return "XLD 轮廓平滑";
                case VmToolKind.XldAffineTransform:
                    return "XLD 仿射变换";
                case VmToolKind.XldProjectiveTransform:
                    return "XLD 透视变换";
                case VmToolKind.XldContourCrop:
                    return "XLD 矩形裁剪";
                case VmToolKind.XldLengthClassification:
                    return "XLD 长度分级";
                case VmToolKind.XldLengthSortSelection:
                    return "XLD 长度排序/索引选择";
                case VmToolKind.XldPointClosureFilter:
                    return "XLD 点数/闭合筛选";
                case VmToolKind.XldAreaDirectionFilter:
                    return "XLD 面积/方向筛选";
                case VmToolKind.XldBoundingCompactnessFilter:
                    return "XLD 外接框/紧凑度筛选";
                case VmToolKind.XldCircularityConvexityFilter:
                    return "XLD 圆度/凸性筛选";
                case VmToolKind.XldEnclosingCircleDiameterMeasurement:
                    return "XLD 最小包围圆/直径测量";
                case VmToolKind.XldMinimumRectangleDirectionMeasurement:
                    return "XLD 最小旋转矩形/方向测量";
                case VmToolKind.XldDirectionConsistencyMeasurement:
                    return "XLD 方向一致性/角度偏差";
                case VmToolKind.TcpClientExchange:
                    return "TCP 客户端发送/接收";
                case VmToolKind.ModbusTcpExchange:
                    return "Modbus TCP 读写";
                case VmToolKind.ModbusRtuExchange:
                    return "Modbus RTU 串口读写";
                case VmToolKind.TextAssembly:
                    return "文本组装";
                case VmToolKind.TextParser:
                    return "文本解析";
                case VmToolKind.NumberArrayCreate: return "数值数组生成";
                case VmToolKind.TextArraySplit: return "文本分割为数组";
                case VmToolKind.NumberArrayStatistics: return "数值数组统计 / 取值";
                case VmToolKind.TextArraySelect: return "文本数组取值";
                case VmToolKind.NumericExpression: return "数值表达式";
                case VmToolKind.JsonRead: return "结构化数据读取";
                case VmToolKind.JsonWrite: return "结构化数据组装";
                case VmToolKind.NumberArrayReorder: return "数值数组整理";
                case VmToolKind.TextArrayReorder: return "文本数组整理";
                case VmToolKind.NumericCalculation:
                    return "数值运算";
                case VmToolKind.ResultLogicAggregation:
                    return "结果逻辑汇总";
                case VmToolKind.BranchValueSelection:
                    return "分支结果选择";
                case VmToolKind.SubflowCall:
                    return "子流程调用";
                case VmToolKind.SubflowInput:
                    return "子流程输入边界";
                case VmToolKind.CSharpDllScript:
                    return "C# / DLL 脚本";
                case VmToolKind.GlobalVariableRead:
                    return "读取全局变量";
                case VmToolKind.GlobalVariableWrite:
                    return "写入全局变量";
                case VmToolKind.PositionCorrection2D:
                    return "2D 位置修正";
                case VmToolKind.PlanarCalibration2D:
                    return "平面标定 / 世界测量";
                case VmToolKind.WorldPointPairMeasurement:
                    return "世界点距离 / 方向";
                case VmToolKind.PointSetGeometryStatistics:
                    return "点集几何统计";
                case VmToolKind.PointToLineDistanceMeasurement:
                    return "点到直线距离";
                case VmToolKind.PointToCircleRadialDeviationMeasurement:
                    return "点到圆径向偏差";
                case VmToolKind.PointToEllipseDistanceMeasurement:
                    return "点到椭圆距离/位置";
                case VmToolKind.LineEllipseIntersectionMeasurement:
                    return "直线段与椭圆交点/弦长";
                case VmToolKind.DataCode2DRecognition:
                    return "二维代码识别";
                case VmToolKind.BarCode1DRecognition:
                    return "一维码识别";
                case VmToolKind.OcrRecognition:
                    return "OCR 字符识别";
                case VmToolKind.LineCircleIntersectionMeasurement:
                    return "直线圆交点测量";
                case VmToolKind.CircleCircleIntersectionMeasurement:
                    return "圆圆关系/交点测量";
                case VmToolKind.PointPairLineConstructionMeasurement:
                    return "两点构线/中点测量";
                case VmToolKind.TypedLineLineRelationMeasurement:
                    return "类型化双直线关系/交点";
                case VmToolKind.TypedPointSetLineFittingMeasurement:
                    return "类型化点集直线拟合";
                case VmToolKind.TypedPointSetCircleFittingMeasurement:
                    return "类型化点集圆拟合";
                case VmToolKind.TypedPointSetEllipseFittingMeasurement:
                    return "类型化点集椭圆拟合";
                case VmToolKind.PolarImageUnwrap:
                    return "极坐标展开";
                case VmToolKind.RoiImageExtraction:
                    return "ROI 图像提取";
                case VmToolKind.ImageDifference:
                    return "双图像差分";
                case VmToolKind.DynamicThreshold:
                    return "动态阈值分割";
                case VmToolKind.ImageThreshold:
                    return "阈值分割";
                case VmToolKind.AutomaticThreshold:
                    return "自动阈值分割";
                case VmToolKind.LocalAdaptiveThreshold:
                    return "局部自适应阈值";
                case VmToolKind.ColorInspectionHsv:
                    return "HSV 颜色检测";
                case VmToolKind.VariationSurfaceInspection:
                    return "良品变化缺陷检测";
                case VmToolKind.GrayHistogram:
                    return "灰度直方图分析";
                case VmToolKind.GrayLineProfile:
                    return "灰度线剖面分析";
                case VmToolKind.RegionMorphology:
                    return "Region 形态学";
                case VmToolKind.RegionFeatureFilter:
                    return "Region 特征筛选";
                case VmToolKind.RegionSetOperation:
                    return "Region 集合运算";
                case VmToolKind.RegionContourMeasurement:
                    return "Region 轮廓测量";
                case VmToolKind.XldContourFilter:
                    return "XLD 轮廓筛选";
                case VmToolKind.XldContourSegmentation:
                    return "XLD 轮廓分段";
                case VmToolKind.XldLineFitting:
                    return "XLD 直线拟合测量";
                case VmToolKind.XldCircleFitting:
                    return "XLD 圆拟合测量";
                case VmToolKind.XldEllipseFitting:
                    return "XLD 椭圆拟合测量";
                case VmToolKind.XldRectangleFitting:
                    return "XLD 旋转矩形拟合测量";
                case VmToolKind.XldLineRelationMeasurement:
                    return "双直线关系测量";
                case VmToolKind.CaliperEdgePairMeasurement:
                    return "卡尺边缘对测量";
                case VmToolKind.LinearCaliperArrayMeasurement:
                    return "线性多卡尺阵列";
                case VmToolKind.CircularCaliperCircleFinder:
                    return "圆周卡尺找圆";
                case VmToolKind.RingWallThicknessMeasurement:
                    return "圆环壁厚/同心度测量";
                case VmToolKind.ShapeMatch:
                    return "Shape Model 模板匹配";
                case VmToolKind.NccTemplateMatch:
                    return "NCC 灰度模板匹配";
                case VmToolKind.Blob:
                    return "Blob 面积筛选";
                case VmToolKind.GrayStat:
                    return "灰度统计判定";
                case VmToolKind.EdgeMeasure:
                    return "边缘测量";
                case VmToolKind.HDevelop:
                    return "HDevelop 扩展";
                case VmToolKind.NumericJudge:
                    return "数值判定";
                default:
                    return kind.ToString();
            }
        }

        public static string GetCategory(VmToolKind kind)
        {
            if (kind == VmToolKind.ConnectedObjectAnalysisS144) return "检测/分割";
            if (kind == VmToolKind.ImageEdgeLineFindingS143) return "测量";
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return "采集/输入";
                case VmToolKind.ShapeMatch:
                case VmToolKind.NccTemplateMatch:
                case VmToolKind.PositionCorrection2D:
                case VmToolKind.PlanarCalibration2D:
                    return "定位";
                case VmToolKind.WorldPointPairMeasurement:
                    return "测量";
                case VmToolKind.ImageChannel:
                case VmToolKind.ImageFilter:
                case VmToolKind.ImageEnhancement:
                case VmToolKind.ImageResize:
                case VmToolKind.ImageMirror:
                case VmToolKind.ImageRotate:
                case VmToolKind.ImageAffineTransform:
                case VmToolKind.ImageProjectiveTransform:
                case VmToolKind.CameraDistortionCorrection:
                case VmToolKind.PoseImageAlignment:
                case VmToolKind.PolarImageUnwrap:
                case VmToolKind.ImageDifference:
                    return "图像处理";
                case VmToolKind.Blob:
                case VmToolKind.GrayStat:
                case VmToolKind.SubpixelEdgeExtraction:
                case VmToolKind.GrayHistogram:
                case VmToolKind.RoiImageExtraction:
                case VmToolKind.GrayLineProfile:
                case VmToolKind.DynamicThreshold:
                case VmToolKind.ImageThreshold:
                case VmToolKind.AutomaticThreshold:
                case VmToolKind.LocalAdaptiveThreshold:
                case VmToolKind.ColorInspectionHsv:
                case VmToolKind.RegionMorphology:
                case VmToolKind.VariationSurfaceInspection:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionSetOperation:
                    return "检测/分割";
                case VmToolKind.DataCode2DRecognition:
                case VmToolKind.BarCode1DRecognition:
                    return "识别";
                case VmToolKind.OcrRecognition:
                    return "识别";
                case VmToolKind.RegionContourMeasurement:
                case VmToolKind.XldContourFilter:
                case VmToolKind.XldAdjacentContourUnion:
                case VmToolKind.XldContourSmoothing:
                case VmToolKind.XldAffineTransform:
                case VmToolKind.XldProjectiveTransform:
                case VmToolKind.XldContourCrop:
                case VmToolKind.XldLengthClassification:
                case VmToolKind.XldLengthSortSelection:
                case VmToolKind.XldPointClosureFilter:
                case VmToolKind.XldAreaDirectionFilter:
                case VmToolKind.XldBoundingCompactnessFilter:
                case VmToolKind.XldCircularityConvexityFilter:
                case VmToolKind.XldContourSegmentation:
                    return "轮廓处理";
                case VmToolKind.XldEnclosingCircleDiameterMeasurement:
                case VmToolKind.XldMinimumRectangleDirectionMeasurement:
                case VmToolKind.XldDirectionConsistencyMeasurement:
                case VmToolKind.PointSetGeometryStatistics:
                case VmToolKind.PointToLineDistanceMeasurement:
                case VmToolKind.PointToCircleRadialDeviationMeasurement:
                case VmToolKind.PointToEllipseDistanceMeasurement:
                case VmToolKind.LineEllipseIntersectionMeasurement:
                case VmToolKind.LineCircleIntersectionMeasurement:
                case VmToolKind.CircleCircleIntersectionMeasurement:
                case VmToolKind.PointPairLineConstructionMeasurement:
                case VmToolKind.TypedLineLineRelationMeasurement:
                case VmToolKind.TypedPointSetLineFittingMeasurement:
                case VmToolKind.TypedPointSetCircleFittingMeasurement:
                case VmToolKind.TypedPointSetEllipseFittingMeasurement:
                case VmToolKind.XldLineFitting:
                case VmToolKind.XldCircleFitting:
                case VmToolKind.XldEllipseFitting:
                case VmToolKind.XldRectangleFitting:
                case VmToolKind.XldLineRelationMeasurement:
                case VmToolKind.CaliperEdgePairMeasurement:
                case VmToolKind.LinearCaliperArrayMeasurement:
                case VmToolKind.CircularCaliperCircleFinder:
                case VmToolKind.RingWallThicknessMeasurement:
                    return "测量";
                case VmToolKind.EdgeMeasure:
                    return "测量";
                case VmToolKind.HDevelop:
                case VmToolKind.CSharpDllScript:
                    return "脚本";
                case VmToolKind.NumericJudge:
                case VmToolKind.TextAssembly:
                case VmToolKind.TextParser:
                case VmToolKind.NumericCalculation:
                case VmToolKind.ResultLogicAggregation:
                case VmToolKind.NumberArrayCreate:
                case VmToolKind.TextArraySplit:
                case VmToolKind.NumberArrayStatistics:
                case VmToolKind.TextArraySelect:
                case VmToolKind.NumericExpression:
                case VmToolKind.NumberArrayReorder:
                case VmToolKind.JsonRead:
                case VmToolKind.JsonWrite:
                case VmToolKind.TextArrayReorder:
                case VmToolKind.GlobalVariableRead:
                case VmToolKind.BranchValueSelection:
                case VmToolKind.SubflowCall:
                case VmToolKind.SubflowInput:
                case VmToolKind.GlobalVariableWrite:
                    return "逻辑/计算";
                case VmToolKind.TcpClientExchange:
                case VmToolKind.ModbusTcpExchange:
                case VmToolKind.ModbusRtuExchange:
                    return "通讯";
                default:
                    return "其他";
            }
        }

        public static int GetCategoryOrder(VmToolKind kind)
        {
            switch (GetCategory(kind))
            {
                case "采集/输入": return 10;
                case "图像处理": return 20;
                case "检测/分割": return 30;
                case "识别": return 35;
                case "定位": return 40;
                case "轮廓处理": return 50;
                case "测量": return 60;
                case "逻辑/计算": return 70;
                case "通讯": return 80;
                case "脚本": return 90;
                default: return 100;
            }
        }

        public static string GetDescription(VmToolKind kind)
        {
            if (kind == VmToolKind.ConnectedObjectAnalysisS144) return "先合并真实Region再按8邻域求连通对象，按面积筛选/稳定排序，输出同序中心、面积、原连接编号及明确选中区域；编号从0开始，查看不改变正式选取。";
            if (kind == VmToolKind.ImageEdgeLineFindingS143) return "沿图上搜索带使用 HALCON 单边采样与鲁棒拟合，输出真实像素直线、采到点集和覆盖/残差；不使用旧图，不把测量几何当产品合格。";
            if (kind == VmToolKind.SubflowCall) return "将连续模块封装为有明确输入与返回值的子流程，按原执行器逐步调用。参数窗可进入原流程工作区编辑内部模块并返回，保存完整方案；完整调用栈监视与连续试运行仍在接入。";
            if (kind == VmToolKind.SubflowInput) return "只读调用输入边界，提供父流程传入的类型化结果，不参与执行。返回上一级调用节点修改外部来源。";
            if (kind == VmToolKind.BranchValueSelection) return "按 Boolean 选择真或假路径的类型化结果，只读取被选中的有效输出；支持 Number/String/Boolean/Image/Region/XLD，用于互斥分支汇合，不使用被跳过路径的旧值。";
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return "读取本地图片、逐帧回放目录或显式连接的 HALCON 相机，输出带来源、SN、时间和尺寸的安全 Image 快照。";
                case VmToolKind.ImageChannel:
                    return "使用 HALCON 保持原图、转换灰度或提取指定图像通道，并继续输出安全 Image 快照。";
                case VmToolKind.ImageFilter:
                    return "使用 HALCON 均值或中值滤波抑制随机噪声，并继续输出安全 Image 快照。";
                case VmToolKind.ImageEnhancement:
                    return "使用 HALCON 线性灰度缩放或局部对比度增强改善图像可分辨性，并继续输出安全 Image 快照。";
                case VmToolKind.ImageResize:
                    return "使用 HALCON zoom_image_size 将图像缩放到明确目标宽高，输出行列比例与新坐标语义，并继续输出安全 Image 快照。";
                case VmToolKind.ImageMirror:
                    return "使用 HALCON mirror_image 按行、列或主对角线镜像图像；对角线模式会交换非方形图像的宽高，并继续输出安全 Image 快照。";
                case VmToolKind.ImageRotate:
                    return "使用 HALCON rotate_image 围绕图像中心逆时针旋转；90°/270°交换宽高，其余角度保持图像矩阵尺寸，并明确输出新坐标语义。";
                case VmToolKind.ImageAffineTransform:
                    return "使用 HALCON affine_trans_image 围绕图像中心组合平移、旋转、行列缩放和行方向斜切；可选择是否自适应输出尺寸，并输出 HomMat2D 与明确的新坐标语义。";
                case VmToolKind.ImageProjectiveTransform:
                    return "使用 HALCON normalized_dlt 由四组源/目标像素点建立投影矩阵，再执行 projective_trans_image 或 projective_trans_image_size 完成梯形与平面透视校正。";
                case VmToolKind.CameraDistortionCorrection:
                    return "导入、输入或由标定板多图求出 HALCON 面阵除法内参，生成独立校正 Image；可显式迁移同原图链的点、轮廓和支持的 ROI，查看前后坐标并供下游复用。";
                case VmToolKind.SubpixelEdgeExtraction:
                    return "订阅真实 Image，按 HALCON edges_sub_pix 提取亚像素 XLD；支持滤波器、滞后阈值、彩色转灰和 ROI/屏蔽运行域。";
                case VmToolKind.PolarImageUnwrap:
                    return "使用 HALCON 将圆环或圆弧区域按角度与半径展开为矩形图像；整圆自动避开首尾重复列，并继续输出安全 Image 快照。";
                case VmToolKind.RoiImageExtraction:
                    return "使用 HALCON reduce_domain 按绑定 ROI 或前序 Region 限定有效域，可选择保留原图坐标或以 crop_domain 裁剪外接框，并输出明确坐标偏移的安全 Image 快照。";
                case VmToolKind.ImageDifference:
                    return "显式订阅两路同尺寸、同通道图像，使用 HALCON 绝对差分或带增益/偏移的方向减法提取变化，并继续输出安全 Image 快照。";
                case VmToolKind.DynamicThreshold:
                    return "显式订阅原图与参考图，使用 HALCON dyn_threshold 按局部灰度差提取明缺陷或暗缺陷，并输出安全 Region 快照。";
                case VmToolKind.ImageThreshold:
                    return "将输入图像按明确的灰度上下限执行 HALCON 阈值分割，输出可订阅的安全 Region 快照。";
                case VmToolKind.AutomaticThreshold:
                    return "订阅真实 Image，在 ROI 与屏蔽有效域内使用 HALCON max_separability 自动计算阈值，按目标明暗输出安全 Region 与可订阅阈值。";
                case VmToolKind.LocalAdaptiveThreshold:
                    return "订阅真实 Image，在 ROI 与屏蔽有效域内使用 HALCON var_threshold 按局部均值和灰度波动提取亮/暗目标，输出安全 Region 与可订阅参数。";
                case VmToolKind.ColorInspectionHsv:
                    return "图像内直接取色，按 HALCON HSV 环绕色相及饱和度/亮度范围检测 RGB 颜色，输出真实区域与有效 ROI 内的覆盖比例，可接区域筛选和数值判定。";
                case VmToolKind.VariationSurfaceInspection:
                    return "从多张同源、同尺寸且已对齐的单通道良品学习 HALCON 标准变化模型，检测表面灰度异常；输出缺陷区域、面积和比例，可接区域筛选和数值判定。";
                case VmToolKind.GrayHistogram:
                    return "使用 HALCON 统计真实 Image 与 ROI/屏蔽有效域的灰度分布，保留 byte/int1/int2/uint2 原始灰度，RGB 转亮度；输出分位数、均值、波动、峰值与样本数，可在结果页查看分布。";
                case VmToolKind.GrayLineProfile:
                    return "沿明确起终点使用 HALCON 双线性等距采样，检查每个采样邻域的输入有效域，输出灰度空间曲线、采样线 XLD、均值/波动/极值及位置，支持 Number 下游。";
                case VmToolKind.RegionMorphology:
                    return "订阅上游 Region，使用 HALCON 圆形开闭、膨胀或腐蚀处理区域并输出可继续订阅的安全 Region 快照。";
                case VmToolKind.RegionFeatureFilter:
                    return "订阅上游 Region，使用 HALCON 按面积、宽度、高度或圆度筛选连通区域并输出安全 Region 快照。";
                case VmToolKind.RegionSetOperation:
                    return "订阅两路不同的上游 Region，使用 HALCON 执行并集、交集、差集或对称差，并输出可继续订阅的安全 Region 快照。";
                case VmToolKind.RegionContourMeasurement:
                    return "订阅上游 Region，使用 HALCON 生成可显示的 XLD 轮廓并测量轮廓总长度与数量。";
                case VmToolKind.XldContourFilter:
                    return "订阅上游 XLD，使用 HALCON 按轮廓长度或最大跨度筛选，并输出可继续订阅和显示的 XLD。";
                case VmToolKind.XldAdjacentContourUnion:
                    return "订阅上游 XLD，使用 HALCON union_adjacent_contours_xld 按端点绝对/相对距离连接相邻轮廓，并可选择保留或丢弃轮廓属性。";
                case VmToolKind.XldContourSmoothing:
                    return "订阅上游 XLD，使用 HALCON smooth_contours_xld 以局部回归线投影抑制轮廓噪声，输出平滑前后长度变化并明确点属性不会保留。";
                case VmToolKind.XldAffineTransform:
                    return "订阅上游 XLD，使用 HALCON affine_trans_contour_xld 执行平移、中心旋转、行列缩放和斜切；可配置组合顺序及 affine_trans_pixel/标准像素中心坐标策略。";
                case VmToolKind.XldProjectiveTransform:
                    return "订阅上游 XLD，由四组源/目标 Row/Column 点以 normalized_dlt 建立投影矩阵，再使用 HALCON projective_trans_contour_xld 完成透视变换并明确像素中心坐标策略。";
                case VmToolKind.XldContourCrop:
                    return "订阅上游 XLD，使用 HALCON crop_contours_xld 在矩形边界精确求交并裁剪轮廓，可选择将原闭合轮廓沿裁剪边界重新闭合。";
                case VmToolKind.XldLengthClassification:
                    return "订阅上游 XLD，使用 HALCON length_xld 测量每条轮廓并以 select_obj 互斥分为短、中、长三组，输出三组托管 XLD 及数量、总长、最小、最大和平均长度。";
                case VmToolKind.XldLengthSortSelection:
                    return "订阅上游 XLD，以 HALCON length_xld 获取每条长度并保持同长度原始次序，按升/降序执行 Top-N、Bottom-N 或排名区间选择，再以一基 select_obj 输出托管 XLD 和可读原始索引。";
                case VmToolKind.XldPointClosureFilter:
                    return "订阅上游 XLD，以 HALCON get_contour_xld 读取逐轮廓点序列，按点数范围及首末点距离容差筛选开放/闭合轮廓，再以一基 select_obj 输出托管 XLD、原始索引和统计。";
                case VmToolKind.XldAreaDirectionFilter:
                    return "订阅上游 XLD，先明确跳过开放或退化轮廓，再使用 HALCON area_center_xld 测量闭合轮廓面积和点序方向，按绝对面积及顺/逆时针筛选并输出托管 XLD、原始索引和统计。";
                case VmToolKind.XldBoundingCompactnessFilter:
                    return "订阅上游 XLD，显式跳过开放、自交和退化轮廓，使用 HALCON smallest_rectangle1_xld 与 compactness_xld 测量轴对齐外接框宽高和紧凑度，按三个闭区间筛选并输出托管 XLD、原始索引和统计。";
                case VmToolKind.XldCircularityConvexityFilter:
                    return "订阅上游 XLD，显式跳过开放、自交和退化轮廓，使用 HALCON circularity_xld 与 convexity_xld 测量圆度和凸性，按两个闭区间筛选并输出托管 XLD、原始索引和统计。";
                case VmToolKind.XldEnclosingCircleDiameterMeasurement:
                    return "订阅上游 XLD，使用 HALCON smallest_circle_xld 与 diameter_xld 测量每条轮廓的最小包围圆和最大点间直径，按开闭策略及直径闭区间选择，并输出圆与直径线托管 XLD。";
                case VmToolKind.XldMinimumRectangleDirectionMeasurement:
                    return "订阅上游 XLD，使用 HALCON smallest_rectangle2_xld 计算包含全部轮廓点的最小面积旋转矩形，输出中心、主轴方向、长短边、面积和四角点，并生成托管矩形 XLD。";
                case VmToolKind.XldDirectionConsistencyMeasurement:
                    return "订阅上游 XLD，以 HALCON smallest_rectangle2_xld 的主轴方向测量相对目标方向的半周等价有符号偏差；跳过近正方形不稳定轮廓，输出矩形、方向轴和逐轮廓合格判定。";
                case VmToolKind.TcpClientExchange:
                    return "通用 TCP 客户端流程工具：将可选 String、Number、ResultCode 输入组装到发送模板，按编码、行尾和超时配置完成一次连接、发送与可选响应接收。";
                case VmToolKind.ModbusTcpExchange:
                    return "标准 Modbus TCP 任务：读线圈、离散输入、保持/输入寄存器或写单点；按 MBAP 校验事务号、Unit ID、功能码、长度、异常及写入回显。地址填写零起始 PDU 地址。";
                case VmToolKind.ModbusRtuExchange:
                    return "真实串口 Modbus RTU 任务：按站号/功能码读位、读 16 位寄存器或写单点；校验 CRC、响应长度、异常和写入回显。一次打开并释放 COM 口，不自动重连。";
                case VmToolKind.TextAssembly:
                    return "订阅可选 String、Number、ResultCode，按模板、分隔符、数值格式和空值策略生成可继续订阅的工业文本。";
                case VmToolKind.TextParser:
                    return "订阅 String，按字面分隔或正则匹配提取指定字段，可选执行 InvariantCulture 数值转换，并输出可继续订阅的文本、数值和匹配状态。";
                case VmToolKind.NumberArrayCreate: return "将输入文本或本地文本按字面分隔符生成 Number[]；采用小数点格式，拒绝空项、非数值、NaN 和无穷大，最多 10000 项。输入已连接时不回退到本地文本。";
                case VmToolKind.TextArraySplit: return "将输入文本或本地文本按字面分隔符生成 String[]；保留空字段与空白，整段空文本产生空数组，最多 10000 项。不解析 CSV 引号或转义。";
                case VmToolKind.NumberArrayStatistics: return "订阅 Number[]，计算数量、最小、最大、总和、均值，并按从 0 开始的索引提取数值。空数组、越界或求和溢出明确报错，不修改输入或默默忽略异常项。";
                case VmToolKind.TextArraySelect: return "订阅 String[]，按从 0 开始的索引提取原始文本，继续用于文本组装、解析或通讯；空数组和越界明确报错。";
                case VmToolKind.NumberArrayReorder:
                case VmToolKind.TextArrayReorder:
                    return "对正式数组执行稳定升降序、保序切片、同类型前后合并或按索引重排，发布新数组和从 0 开始的来源索引，不修改上游。文本按 Ordinal 排序；重排需匹配来源数量，允许显式重复索引。";
                case VmToolKind.NumericExpression: return "用一条有限数值公式完成多步计算，订阅实际使用的 A/B/C/D，支持括号、四则、余数、幂和常用函数，输出 Number 驱动判定。不是 C# 脚本，不访问文件或设备；公式最多 1024 字符、256 节点、32 层解析嵌套。";
                case VmToolKind.JsonRead: return "按明确字段路径读取 JSON，输出数值、文本、布尔或数组；严格区分缺失、null 与空值，最多 32 字段，全部成功后一起发布。可使用本地报文或订阅通讯/脚本输出。";
                case VmToolKind.JsonWrite: return "把类型化输入按字段路径组装为 JSON 对象，自动处理字符串转义和数组；不拼接伪 JSON，不自动外发，输出可连接通讯或脚本。";
                case VmToolKind.NumericCalculation:
                    return "订阅必需 NumberA 和按运算类型使用的可选 NumberB，执行加、减、乘、除、最小、最大、绝对值或四舍五入，并输出可继续订阅的有限数值。";
                case VmToolKind.ResultLogicAggregation:
                    return "订阅必需 ResultCodeA 和按逻辑模式使用的可选 ResultCodeB，对规范 OK/NG 结果执行全部、任一、取反或一元透传，并输出汇总结果与通过数量。";
                case VmToolKind.CSharpDllScript:
                    return "受信任扩展流程工具：固定标量端口保持兼容，命名接口支持数值、文本、布尔、结果码及 Number[] / String[] 输入、参数和输出；数组独立复制并检查上限。内联 C# 与约定 DLL 使用同一接口，可卸载 AppDomain 不是安全沙箱。";
                case VmToolKind.GlobalVariableRead:
                    return "按稳定 VariableId 读取当前方案中的强类型全局变量，并发布 Number、String、Boolean 或 ResultCode 值供后续工具订阅。";
                case VmToolKind.GlobalVariableWrite:
                    return "订阅与目标变量类型一致的上游端口，在确定性流程中写入当前方案变量，并发布写后值、Changed 与执行结果。";
                case VmToolKind.PositionCorrection2D:
                    return "优先订阅类型化 Pose2D，也兼容旧版 Row、Column、Angle 三路输入；以 HALCON vector_angle_to_rigid 计算从基准位姿到当前位姿的 2D 刚体修正，并为下游 ROI/屏蔽区跟随提供稳定来源。";
                case VmToolKind.PlanarCalibration2D:
                    return "在图像上创建点 ROI 并输入对应世界坐标；HALCON 拟合像素→世界平面变换，显示逐点残差，并把同图像链的点转换为独立 World2D 坐标和真实世界距离。";
                case VmToolKind.WorldPointPairMeasurement:
                    return "订阅两路同一标定与图像链、同单位的 World2D 点；HALCON 测量实际距离，输出方向、偏移、中点与可订阅的数值结果。";
                case VmToolKind.PointSetGeometryStatistics:
                    return "订阅同一图像像素坐标上下文的类型化 Point2D[]，校验点数和异常策略，计算质心、边界与跨度，并使用 HALCON 生成可显示的点集、质心和外接框 XLD。";
                case VmToolKind.PointToLineDistanceMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Point2D 与 Line2D，计算垂足、无符号距离及按直线方向定义的有符号侧值，并使用 HALCON 生成点、垂足和辅助线 XLD。";
                case VmToolKind.PointToCircleRadialDeviationMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Point2D 与 Circle2D，计算最近圆周点、圆心距、绝对径向偏差及内外有符号偏差，并使用 HALCON 生成圆与径向辅助 XLD。";
                case VmToolKind.PointToEllipseDistanceMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Point2D 与 Ellipse2D，使用 HALCON gen_ellipse_contour_xld 与 distance_pc 求点到椭圆距离，并输出最近点、内外有符号距离、归一化半径、法线方向及托管 XLD。";
                case VmToolKind.LineEllipseIntersectionMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化有限 Line2D 线段与 Ellipse2D，使用 HALCON gen_contour_polygon_xld、gen_ellipse_contour_xld 与 intersection_contours_xld 求 0/1/2 个有限交点，输出类型化点集、关系、弦长及托管 XLD。";
                case VmToolKind.DataCode2DRecognition:
                    return "订阅真实 Image，可选 ROI/屏蔽区，使用 HALCON create/find/clear_data_code_2d_model 识别 QR Code 或 Data Matrix ECC 200，输出解码文本、数量、首码中心/姿态及托管 XLD。";
                case VmToolKind.BarCode1DRecognition:
                    return "订阅真实 Image，可选 ROI/屏蔽区，使用 HALCON create/find/clear_bar_code_model 识别 Code 128、Code 39、EAN-13 或自动类型，输出解码文本、数量、首码中心/姿态及托管 XLD。";
                case VmToolKind.OcrRecognition:
                    return "使用 HALCON MLP 字库识别 Image 中的字符；可订阅前序字符 Region，或在 ROI/屏蔽区内按灰度分割。输出文本、字符数、置信度、中心点集及轮廓；不支持任意版面自动识别。";
                case VmToolKind.LineCircleIntersectionMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Line2D 与 Circle2D，调用 HALCON intersection_line_circle 求无限直线与整圆的 0/1/2 个交点，输出类型化点集、相离/相切/相交关系、弦长及托管 XLD。";
                case VmToolKind.CircleCircleIntersectionMeasurement:
                    return "订阅同一 ImagePixel 上下文的两路不同 Circle2D，调用 HALCON intersection_circles 求两整圆关系及 0/1/2 个有限交点，明确区分分离、内含、内外切、相交、同心与重合，并输出圆心距、公共弦长及托管 XLD。";
                case VmToolKind.PointPairLineConstructionMeasurement:
                    return "订阅同一 ImagePixel 上下文的两路不同 Point2D，使用 HALCON distance_pp 测量点距，以 A→B 构造类型化 Line2D 和中点 Point2D，并输出方向、行列差及托管线段/关键点 XLD。";
                case VmToolKind.TypedLineLineRelationMeasurement:
                    return "订阅同一 ImagePixel 上下文的两路不同 Line2D，使用 HALCON intersection_lines、angle_ll 与 distance_pl 区分相交、平行和重合，输出有限交点、夹角、平行距及托管 XLD。";
                case VmToolKind.TypedPointSetLineFittingMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Point2D[]，按异常点策略生成 HALCON XLD 点序列并调用 fit_line_contour_xld 鲁棒拟合，输出 MainLine、方向、残差、点数和托管点/线/残差图形。";
                case VmToolKind.TypedPointSetCircleFittingMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Point2D[]，按异常点策略生成 HALCON XLD 点序列并调用 fit_circle_contour_xld 鲁棒拟合，输出 MainCircle、圆心、半径、残差、点数和托管点/圆/径向残差图形。";
                case VmToolKind.TypedPointSetEllipseFittingMeasurement:
                    return "订阅同一 ImagePixel 上下文的类型化 Point2D[]，按异常点策略生成 HALCON XLD 点序列并调用 fit_ellipse_contour_xld 鲁棒拟合，以 distance_pc 计算点到拟合轮廓残差，输出 MainEllipse、中心、方向、长短半轴、轴比、点数和托管点/椭圆/残差图形。";
                case VmToolKind.XldContourSegmentation:
                    return "订阅上游 XLD，使用 HALCON 将轮廓分为直线段及圆弧或椭圆弧，并输出分类、长度与可继续订阅的 XLD。";
                case VmToolKind.XldLineFitting:
                    return "订阅直线段 XLD，使用 HALCON 鲁棒拟合直线，输出端点、长度、方向与残差，并提供可显示的拟合线 XLD。";
                case VmToolKind.XldCircleFitting:
                    return "订阅圆弧或整圆 XLD，使用 HALCON 鲁棒拟合圆，输出圆心、半径、弧角、点序与残差，并提供可显示的拟合圆弧 XLD。";
                case VmToolKind.XldEllipseFitting:
                    return "订阅椭圆弧或闭合 XLD，使用 HALCON 鲁棒拟合椭圆，输出中心、主轴方向、长短半轴、弧角、点序与残差，并提供可显示的拟合椭圆 XLD。";
                case VmToolKind.XldRectangleFitting:
                    return "订阅完整或部分矩形 XLD，使用 HALCON 鲁棒拟合旋转矩形，输出中心、方向、两边半长、四角点、点序与残差，并提供可显示的拟合矩形 XLD。";
                case VmToolKind.XldLineRelationMeasurement:
                    return "订阅两路不同的直线 XLD，使用 HALCON 拟合主线并测量交点、方向、夹角、平行/重合状态和线间距，输出可显示的两线与交点 XLD。";
                case VmToolKind.CaliperEdgePairMeasurement:
                    return "订阅真实 Image，在旋转矩形测量域内使用 HALCON measure_pairs 提取边缘对，输出边缘坐标、幅值、间距、数量及可显示的测量图形。";
                case VmToolKind.LinearCaliperArrayMeasurement:
                    return "订阅真实 Image，沿起点到终点均匀布置多个独立 HALCON 卡尺，输出逐卡尺距离、有效/失败索引、统计量与可显示的阵列 XLD。";
                case VmToolKind.CircularCaliperCircleFinder:
                    return "订阅真实 Image，沿圆周径向布置独立 HALCON 卡尺采集边缘点并鲁棒拟合圆，输出圆心、半径、残差、有效/失败索引及可显示 XLD。";
                case VmToolKind.RingWallThicknessMeasurement:
                    return "订阅真实 Image，沿圆周径向执行 HALCON 边缘对测量，分别鲁棒拟合内外圆并输出壁厚统计、偏心量、有效/失败索引及可显示 XLD。";
                case VmToolKind.ShapeMatch:
                    return "HALCON Shape Model 定位目标并输出中心、角度和分数。";
                case VmToolKind.NccTemplateMatch:
                    return "在本模块框选灰度模板，使用 HALCON NCC 搜索图案并输出真实候选、分数和位姿。搜索 ROI 仅限制模型参考点，训练框不是搜索范围；无匹配不输出虚假点或位姿。";
                case VmToolKind.PoseImageAlignment:
                    return "订阅同图像链的 Image 与 Pose2D，以 HALCON 刚体变换将当前姿态对齐到显式保存的参考姿态，输出独立坐标的真实图像和有效覆盖域。检测 ROI 必须在对齐图另配，不继承源图 ROI。";
                case VmToolKind.Blob:
                    return "按灰度和面积筛选连通区域。";
                case VmToolKind.GrayStat:
                    return "统计 ROI 灰度均值并按范围判定。";
                case VmToolKind.EdgeMeasure:
                    return "提取亚像素边缘并统计轮廓长度。";
                case VmToolKind.HDevelop:
                    return "调用约定输入输出的 HDevelop 过程。";
                case VmToolKind.NumericJudge:
                    return "订阅上游数值输出，按阈值和比较方式生成工业 OK/NG 判定。";
                default:
                    return string.Empty;
            }
        }

        public static IList<VmPortDefinition> GetInputPorts(VmToolKind kind)
        {
            if (kind == VmToolKind.ConnectedObjectAnalysisS144) return new[] { Port("Region", "明确输入区域", "Region", false) };
            if (kind == VmToolKind.ImageEdgeLineFindingS143) return new[] { Port("Image", "明确输入图像", "Image", false) };
            if (kind == VmToolKind.JsonRead) return new[] { Port("Json", "JSON 文本（未连接使用本地文本）", "String", true) };
            if (kind == VmToolKind.JsonWrite) return new VmPortDefinition[0];
            if (kind == VmToolKind.BranchValueSelection) return new[]
            {
                Port("Selector", "选择条件", "Boolean", false),
                Port("WhenTrue", "真路径值（选中时必需）", "Number", true),
                Port("WhenFalse", "假路径值（选中时必需）", "Number", true)
            };
            switch (kind)
            {
                case VmToolKind.NumberArrayReorder:
                case VmToolKind.TextArrayReorder:
                    return new[] { Port("Array", "待整理数组", VmArrayReorderS108.DataType(kind), false), Port("OtherArray", "追加数组", VmArrayReorderS108.DataType(kind), true),
                        Port("Indices", "来源索引数组", "Number[]", true), Port("SourceCount", "索引对应的原始数量", "Number", true) };
                case VmToolKind.NumericExpression:
                    return new[] { Port("A", "变量 A", "Number", true), Port("B", "变量 B", "Number", true), Port("C", "变量 C", "Number", true), Port("D", "变量 D", "Number", true) };
                case VmToolKind.PlanarCalibration2D:
                    return new[] { Port("Image", "标定图像来源", "Image", false),
                        Port("PointA", "待测像素点 A", "Point2D", true), Port("PointB", "待测像素点 B", "Point2D", true) };
                case VmToolKind.WorldPointPairMeasurement:
                    return new[] { Port("PointA", "世界点 A", "WorldPoint2D", false),
                        Port("PointB", "世界点 B", "WorldPoint2D", false) };
                case VmToolKind.CameraDistortionCorrection:
                    return new[] { Port("Image", "原始相机图像", "Image", false),
                        Port("Point", "原图点（迁移到校正图）", "Point2D", true),
                        Port("XLD", "原图轮廓（迁移到校正图）", "XLD", true) };
                case VmToolKind.PoseImageAlignment:
                    return new[] { Port("Image", "待对齐图像", "Image", false),
                        Port("Pose", "本图像链当前定位姿态", "Pose2D", false) };
                case VmToolKind.NumberArrayCreate:
                case VmToolKind.TextArraySplit:
                    return new[] { Port("String", "输入文本（未连接时使用本地文本）", "String", true) };
                case VmToolKind.NumberArrayStatistics:
                    return new[] { Port("Array", "数值数组", "Number[]", false) };
                case VmToolKind.TextArraySelect:
                    return new[] { Port("Array", "文本数组", "String[]", false) };
                case VmToolKind.ImageSource:
                    return new VmPortDefinition[0];
                case VmToolKind.ImageChannel:
                case VmToolKind.ImageFilter:
                case VmToolKind.ImageEnhancement:
                case VmToolKind.ImageResize:
                case VmToolKind.ImageMirror:
                case VmToolKind.ImageRotate:
                case VmToolKind.ImageAffineTransform:
                case VmToolKind.ImageProjectiveTransform:
                case VmToolKind.PolarImageUnwrap:
                case VmToolKind.ImageThreshold:
                case VmToolKind.AutomaticThreshold:
                case VmToolKind.LocalAdaptiveThreshold:
                case VmToolKind.CaliperEdgePairMeasurement:
                case VmToolKind.LinearCaliperArrayMeasurement:
                case VmToolKind.CircularCaliperCircleFinder:
                case VmToolKind.RingWallThicknessMeasurement:
                    return new[] { Port("Image", "输入图像", "Image", false) };
                case VmToolKind.DataCode2DRecognition:
                case VmToolKind.BarCode1DRecognition:
                    return new[]
                    {
                        Port("Image", "输入图像", "Image", false),
                        Port("ROI", "绑定 ROI / 前序 Region", "Region", true)
                    };
                case VmToolKind.GrayHistogram:
                    return new[] { Port("Image", "输入图像", "Image", false), Port("ROI", "检测域 / 前序区域", "Region", true) };
                case VmToolKind.ColorInspectionHsv:
                    return new[] { Port("Image", "RGB 彩色图像", "Image", false), Port("ROI", "检测域 / 前序区域", "Region", true) };
                case VmToolKind.VariationSurfaceInspection:
                    return new[] { Port("Image", "已对齐单通道图像", "Image", false), Port("ROI", "检测域 / 前序区域", "Region", true) };
                case VmToolKind.GrayLineProfile:
                    return new[] { Port("Image", "输入图像", "Image", false) };
                case VmToolKind.OcrRecognition:
                    return new[]
                    {
                        Port("Image", "输入图像", "Image", false),
                        Port("Characters", "已分割字符（可选）", "Region", true),
                        Port("ROI", "检测范围 / 前序 Region", "Region", true)
                    };
                case VmToolKind.SubpixelEdgeExtraction:
                    return new[]
                    {
                        Port("Image", "输入图像", "Image", false),
                        Port("ROI", "绑定 ROI / 前序 Region", "Region", true)
                    };
                case VmToolKind.RoiImageExtraction:
                    return new[]
                    {
                        Port("Image", "输入图像", "Image", false),
                        Port("ROI", "绑定 ROI / 前序 Region", "Region", true)
                    };
                case VmToolKind.ImageDifference:
                    return new[]
                    {
                        Port("ImageA", "图像 A（被减图）", "Image", false),
                        Port("ImageB", "图像 B（减图）", "Image", false)
                    };
                case VmToolKind.DynamicThreshold:
                    return new[]
                    {
                        Port("OriginalImage", "原始图像", "Image", false),
                        Port("ReferenceImage", "参考图像", "Image", false)
                    };
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionContourMeasurement:
                    return new[] { Port("Region", "输入区域", "Region", false) };
                case VmToolKind.XldContourFilter:
                case VmToolKind.XldAdjacentContourUnion:
                case VmToolKind.XldContourSmoothing:
                case VmToolKind.XldAffineTransform:
                case VmToolKind.XldProjectiveTransform:
                case VmToolKind.XldContourCrop:
                case VmToolKind.XldLengthClassification:
                case VmToolKind.XldLengthSortSelection:
                case VmToolKind.XldPointClosureFilter:
                case VmToolKind.XldAreaDirectionFilter:
                case VmToolKind.XldBoundingCompactnessFilter:
                case VmToolKind.XldCircularityConvexityFilter:
                case VmToolKind.XldEnclosingCircleDiameterMeasurement:
                case VmToolKind.XldMinimumRectangleDirectionMeasurement:
                case VmToolKind.XldDirectionConsistencyMeasurement:
                case VmToolKind.XldContourSegmentation:
                case VmToolKind.XldLineFitting:
                case VmToolKind.XldCircleFitting:
                case VmToolKind.XldEllipseFitting:
                case VmToolKind.XldRectangleFitting:
                    return new[] { Port("XLD", "输入轮廓", "XLD", false) };
                case VmToolKind.XldLineRelationMeasurement:
                    return new[]
                    {
                        Port("LineA", "直线 A", "XLD", false),
                        Port("LineB", "直线 B", "XLD", false)
                    };
                case VmToolKind.RegionSetOperation:
                    return new[]
                    {
                        Port("RegionA", "区域 A", "Region", false),
                        Port("RegionB", "区域 B", "Region", false)
                    };
                case VmToolKind.ShapeMatch:
                    return new[]
                    {
                        Port("Image", "图像", "Image", false),
                        Port("SearchROI", "搜索 ROI（默认全图）", "Region", true),
                        Port("ShapeModel", "形状模板（模块内制作）", "ShapeModel", true)
                    };
                case VmToolKind.NccTemplateMatch:
                    return new[]
                    {
                        Port("Image", "灰度模板搜索图像", "Image", false),
                        Port("ROI", "搜索参考点范围（默认全图）", "Region", true)
                    };
                case VmToolKind.Blob:
                case VmToolKind.GrayStat:
                case VmToolKind.EdgeMeasure:
                    return new[]
                    {
                        Port("Image", "图像", "Image", false),
                        Port("ROI", "ROI", "Region", true)
                    };
                case VmToolKind.HDevelop:
                    return new[]
                    {
                        Port("Image", "图像", "Image", false),
                        Port("ROI", "ROI", "Region", false),
                        Port("Program", "HDevelop 程序", "File", false)
                    };
                case VmToolKind.NumericJudge:
                    return new[] { Port("Value", "待判定数值", "Number", false) };
                case VmToolKind.TcpClientExchange:
                case VmToolKind.TextAssembly:
                    return new[]
                    {
                        Port("String", "字符串变量", "String", true),
                        Port("Number", "数值变量", "Number", true),
                        Port("ResultCode", "结果码变量", "String", true)
                    };
                case VmToolKind.ModbusTcpExchange:
                case VmToolKind.ModbusRtuExchange:
                    return new[] { Port("WriteValue", "写入数值（仅 05/06）", "Number", true) };
                case VmToolKind.TextParser:
                    return new[] { Port("String", "待解析文本", "String", false) };
                case VmToolKind.NumericCalculation:
                    return new[]
                    {
                        Port("NumberA", "数值 A", "Number", false),
                        Port("NumberB", "数值 B（双目运算）", "Number", true)
                    };
                case VmToolKind.ResultLogicAggregation:
                    return new[]
                    {
                        Port("ResultCodeA", "结果码 A", "String", false),
                        Port("ResultCodeB", "结果码 B（双目逻辑）", "String", true)
                    };
                case VmToolKind.CSharpDllScript:
                    return new[]
                    {
                        Port("NumberA", "可选数值 A", "Number", true),
                        Port("NumberB", "可选数值 B", "Number", true),
                        Port("String", "可选字符串", "String", true),
                        Port("ResultCode", "可选结果码", "String", true)
                    };
                case VmToolKind.GlobalVariableRead:
                    return new VmPortDefinition[0];
                case VmToolKind.GlobalVariableWrite:
                    return new[]
                    {
                        Port("NumberValue", "数值写入值", "Number", true),
                        Port("TextValue", "字符串写入值", "String", true),
                        Port("BooleanValue", "布尔写入值", "Boolean", true),
                        Port("ResultCodeValue", "结果码写入值", "String", true),
                        Port("NumberArrayValue", "数值数组写入值", "Number[]", true),
                        Port("StringArrayValue", "文本数组写入值", "String[]", true)
                    };
                case VmToolKind.PositionCorrection2D:
                    return new[]
                    {
                        Port("Pose2D", "当前位姿", "Pose2D", true),
                        Port("CurrentRow", "当前 Row（兼容）", "Number", true),
                        Port("CurrentColumn", "当前 Column（兼容）", "Number", true),
                        Port("CurrentAngle", "当前角度（兼容 °）", "Number", true)
                    };
                case VmToolKind.PointSetGeometryStatistics:
                    return new[] { Port("Points", "图像像素点集", "Point2D[]", false) };
                case VmToolKind.PointToLineDistanceMeasurement:
                    return new[]
                    {
                        Port("Point", "待测图像点", "Point2D", false),
                        Port("Line", "参考图像直线", "Line2D", false)
                    };
                case VmToolKind.PointToCircleRadialDeviationMeasurement:
                    return new[]
                    {
                        Port("Point", "待测图像点", "Point2D", false),
                        Port("Circle", "参考图像圆", "Circle2D", false)
                    };
                case VmToolKind.PointToEllipseDistanceMeasurement:
                    return new[]
                    {
                        Port("Point", "待测图像点", "Point2D", false),
                        Port("Ellipse", "参考图像椭圆", "Ellipse2D", false)
                    };
                case VmToolKind.LineEllipseIntersectionMeasurement:
                    return new[]
                    {
                        Port("Line", "有限图像线段", "Line2D", false),
                        Port("Ellipse", "参考图像椭圆", "Ellipse2D", false)
                    };
                case VmToolKind.LineCircleIntersectionMeasurement:
                    return new[]
                    {
                        Port("Line", "参考图像直线", "Line2D", false),
                        Port("Circle", "参考图像圆", "Circle2D", false)
                    };
                case VmToolKind.CircleCircleIntersectionMeasurement:
                    return new[]
                    {
                        Port("CircleA", "参考图像圆 A", "Circle2D", false),
                        Port("CircleB", "参考图像圆 B", "Circle2D", false)
                    };
                case VmToolKind.PointPairLineConstructionMeasurement:
                    return new[]
                    {
                        Port("PointA", "起点 A", "Point2D", false),
                        Port("PointB", "终点 B", "Point2D", false)
                    };
                case VmToolKind.TypedLineLineRelationMeasurement:
                    return new[]
                    {
                        Port("LineA", "参考图像直线 A", "Line2D", false),
                        Port("LineB", "参考图像直线 B", "Line2D", false)
                    };
                case VmToolKind.TypedPointSetLineFittingMeasurement:
                    return new[] { Port("Points", "待拟合图像像素点集", "Point2D[]", false) };
                case VmToolKind.TypedPointSetCircleFittingMeasurement:
                    return new[] { Port("Points", "待拟合图像像素点集", "Point2D[]", false) };
                case VmToolKind.TypedPointSetEllipseFittingMeasurement:
                    return new[] { Port("Points", "待拟合图像像素点集", "Point2D[]", false) };
                default:
                    return new VmPortDefinition[0];
            }
        }

        public static IList<VmPortDefinition> GetOutputPorts(VmToolKind kind)
        {
            if (kind == VmToolKind.ConnectedObjectAnalysisS144) return new[]
            {
                Port("Region", "筛选排序后的对象区域", "Region", false),
                Port("SelectedRegion", "明确选中区域（缺选时无值）", "Region", true),
                Port("Centers", "同序对象中心", "Point2D[]", false),
                Port("Areas", "同序面积 px²", "Number[]", false),
                Port("SourceIndices", "原连接编号（从0开始）", "Number[]", false),
                Port("Count", "筛选后的对象数", "Number", false),
                Port("ConnectedCount", "合并后连接对象数", "Number", false),
                Port("TotalArea", "筛选后的总面积 px²", "Number", false),
                Port("HasSelection", "本次有明确选中对象", "Boolean", false),
                Port("SelectedPoint", "选中对象中心", "Point2D", true),
                Port("SelectedArea", "选中对象面积 px²", "Number", true),
                Port("SelectedIndex", "选中编号（从0开始）", "Number", true),
                Port("Parameters", "真实分析与预算规则", "String", false),
                Port("ResultCode", "分析结果码（非产品判定）", "String", false)
            };
            if (kind == VmToolKind.ImageEdgeLineFindingS143) return new[]
            {
                Port("HasLine", "找到有效直线", "Boolean", false), Port("MainLine", "有效主直线", "Line2D", false),
                Port("EdgePoints", "采到边缘点（含质量拒绝点）", "Point2D[]", false), Port("XLD", "采点与有效拟合线", "XLD", false),
                Port("KeptPoints", "残差规则质量保留点", "Point2D[]", false), Port("RejectedPoints", "残差规则质量拒绝点", "Point2D[]", false),
                Port("MissingSamples", "未采到边缘的搜索中心", "Point2D[]", false),
                Port("ValidCount", "质量保留采点数", "Number", false), Port("SampledCount", "采到点数", "Number", false),
                Port("FailedCount", "未采到点数", "Number", false), Port("RejectedCount", "质量拒绝点数", "Number", false),
                Port("Coverage", "保留点沿基线跨度比例", "Number", false), Port("AngleDegrees", "主线方向（度）", "Number", false),
                Port("Length", "有效主线长度（px）", "Number", false), Port("MeanResidual", "保留点平均残差（px）", "Number", false),
                Port("MaxResidual", "保留点最大残差（px）", "Number", false), Port("Samples", "同序逐采样诊断", "String", false),
                Port("Parameters", "参数与搜索方向", "String", false), Port("ResultCode", "找线结果码", "ResultCode", false)
            };
            if (kind == VmToolKind.JsonRead) return new[] { Port("FieldCount", "读取字段数", "Number", false), Port("ResultCode", "执行结果码", "String", false) };
            if (kind == VmToolKind.JsonWrite) return new[] { Port("Json", "JSON 文本", "String", false), Port("Length", "文本长度", "Number", false), Port("FieldCount", "组装字段数", "Number", false), Port("ResultCode", "执行结果码", "String", false) };
            if (kind == VmToolKind.SubflowCall) return new[] { Port("ResultCode", "子流程判定", "ResultCode", false), Port("ExecutedCount", "执行模块数", "Number", false),
                Port(VmSubflowLoopS111.Completed, "完成项数", "Number", false), Port(VmSubflowLoopS111.Planned, "计划项数", "Number", false), Port(VmSubflowLoopS111.HasLast, "有最后一次结果", "Boolean", false) };
            if (kind == VmToolKind.BranchValueSelection) return new[]
            {
                Port("Value", "选中结果", "Number", false), Port("SelectedBranch", "选中路径", "String", false),
                Port("SelectedSource", "结果来源", "String", false), Port("SelectedResultCode", "来源判定", "String", false),
                Port("ResultCode", "结果码", "ResultCode", false)
            };
            switch (kind)
            {
                case VmToolKind.PlanarCalibration2D:
                    return new[] { Port("XLD", "标定点与残差图形", "XLD", false),
                        Port("WorldPointA", "世界点 A", "WorldPoint2D", false), Port("WorldPointB", "世界点 B", "WorldPoint2D", false),
                        Port("WorldDistance", "A-B 世界距离", "Number", false),
                        Port("RmsResidual", "均方根残差", "Number", false), Port("MaxResidual", "最大残差", "Number", false),
                        Port("Residuals", "逐点世界残差", "Number[]", false),
                        Port("SampleCount", "标定点数量", "Number", false), Port("Unit", "世界单位", "String", false),
                        Port("ImageContext", "图像来源", "String", false), Port("PixelToWorldMatrix", "像素列/行到世界 X/Y 矩阵（仅点）", "String", false),
                        Port("ResultCode", "结果码", "String", false) };
                case VmToolKind.WorldPointPairMeasurement:
                    return new[] { Port("WorldDistance", "世界距离", "Number", false),
                        Port("DirectionAngle", "A 到 B 方向（度）", "Number", false),
                        Port("DeltaX", "世界 X 偏移", "Number", false), Port("DeltaY", "世界 Y 偏移", "Number", false),
                        Port("MidPoint", "世界中点", "WorldPoint2D", false),
                        Port("Unit", "世界单位", "String", false), Port("CalibrationSource", "标定来源", "String", false),
                        Port("ResultCode", "结果码", "ResultCode", false) };
                case VmToolKind.ImageSource:
                    return new[]
                    {
                        Port("Image", "采集图像", "Image", false),
                        Port("SN", "图像序列号", "String", false),
                        Port("Path", "来源路径或设备", "String", false),
                        Port("Width", "图像宽度", "Number", false),
                        Port("Height", "图像高度", "Number", false),
                        Port("Timestamp", "采集时间 UTC", "String", false),
                        Port("SourceMode", "来源模式", "String", false),
                        Port("FrameIndex", "帧序号", "Number", false),
                        Port("FrameCount", "回放总帧数", "Number", false),
                        Port("AcquisitionMs", "采集耗时毫秒", "Number", false),
                        Port("TriggerMode", "触发模式", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PoseImageAlignment:
                    return new[]
                    {
                        Port("Image", "参考坐标对齐图像", "Image", false),
                        Port("Region", "对齐图有效覆盖域", "Region", false),
                        Port("AlignedPose", "输出参考坐标位姿", "Pose2D", false),
                        Port("Width", "输出宽度（px）", "Number", false),
                        Port("Height", "输出高度（px）", "Number", false),
                        Port("EffectivePixels", "有效覆盖像素数", "Number", false),
                        Port("InputRow", "输入定位 Row", "Number", false),
                        Port("InputColumn", "输入定位 Column", "Number", false),
                        Port("InputAngleDegrees", "输入定位角度（°）", "Number", false),
                        Port("ReferenceRow", "参考定位 Row", "Number", false),
                        Port("ReferenceColumn", "参考定位 Column", "Number", false),
                        Port("ReferenceAngleDegrees", "参考定位角度（°）", "Number", false),
                        Port("TranslationRow", "变换 Row 平移（px）", "Number", false),
                        Port("TranslationColumn", "变换 Column 平移（px）", "Number", false),
                        Port("RotationDegrees", "变换旋转角度（°）", "Number", false),
                        Port("Transform", "本次刚体变换", "String", false),
                        Port("ReferenceVersion", "显式参考版本", "String", false),
                        Port("Parameters", "对齐参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.DataCode2DRecognition:
                    return new[]
                    {
                        Port("XLD", "二维代码轮廓", "XLD", false),
                        Port("DecodedText", "首个解码文本", "String", false),
                        Port("DecodedTexts", "全部解码文本", "String", false),
                        Port("CodeCount", "识别数量", "Number", false),
                        Port("CenterPoint", "首码中心", "Point2D", false),
                        Port("Pose", "首码中心与方向", "Pose2D", false),
                        Port("CenterRow", "首码中心 Row", "Number", false),
                        Port("CenterColumn", "首码中心 Column", "Number", false),
                        Port("Angle", "首码方向（°）", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("Diagnostics", "识别诊断", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.BarCode1DRecognition:
                    return new[]
                    {
                        Port("XLD", "一维码轮廓", "XLD", false),
                        Port("DecodedText", "首个解码文本", "String", false),
                        Port("DecodedTexts", "全部解码文本", "String", false),
                        Port("CodeCount", "识别数量", "Number", false),
                        Port("CenterPoint", "首码中心", "Point2D", false),
                        Port("Pose", "首码中心与方向", "Pose2D", false),
                        Port("CenterRow", "首码中心 Row", "Number", false),
                        Port("CenterColumn", "首码中心 Column", "Number", false),
                        Port("Angle", "首码方向（°）", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("Diagnostics", "识别诊断", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.OcrRecognition:
                    return new[]
                    {
                        Port("Text", "整段文本", "String", false),
                        Port("FirstCharacter", "首个字符", "String", false),
                        Port("Characters", "逐字符文本", "String", false),
                        Port("CharacterTexts", "逐字符文本数组", "String[]", false),
                        Port("CharacterConfidences", "逐字符置信度数组", "Number[]", false),
                        Port("CharactersRegion", "同序字符区域", "Region", false),
                        Port("CharacterCount", "字符数", "Number", false),
                        Port("MeanConfidence", "平均置信度", "Number", false),
                        Port("MinimumConfidence", "最低置信度", "Number", false),
                        Port("LowConfidenceCount", "低置信度字符数", "Number", false),
                        Port("TextRuleMatch", "文本规则匹配", "Boolean", false),
                        Port("ValidationFailureReason", "文本规则失败原因", "String", false),
                        Port("ValidationMode", "文本校验模式", "String", false),
                        Port("ModelVersion", "字库 SHA 版本", "String", false),
                        Port("Centers", "字符中心点集", "Point2D[]", false),
                        Port("XLD", "字符轮廓", "XLD", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("Diagnostics", "识别诊断", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TcpClientExchange:
                    return new[]
                    {
                        Port("IsSimulation", "离线模拟", "Boolean", false),
                        Port("Connected", "已连接", "Boolean", false),
                        Port("SentText", "发送文本", "String", false),
                        Port("ReceivedText", "接收文本", "String", false),
                        Port("RoundTripMs", "往返耗时", "Number", false),
                        Port("BytesSent", "发送字节数", "Number", false),
                        Port("BytesReceived", "接收字节数", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ModbusTcpExchange:
                    return new[]
                    {
                        Port("IsSimulation", "离线模拟", "Boolean", false),
                        Port("Connected", "通讯成功", "Boolean", false),
                        Port("Value", "解码主值", "Number", false),
                        Port("DecodedText", "精确主值文本", "String", false),
                        Port("DecodeSummary", "主值解释", "String", false),
                        Port("DecodeType", "主值类型", "String", false),
                        Port("WordOrder", "寄存器字序", "String", false),
                        Port("RawWords", "主值原始字", "String", false),
                        Port("DecodeStatus", "解码状态", "String", false),
                        Port("Values", "全部数值", "Number[]", false),
                        Port("Count", "数值数量", "Number", false),
                        Port("FunctionCode", "功能码", "Number", false),
                        Port("Address", "PDU 地址", "Number", false),
                        Port("TransactionId", "事务号", "Number", false),
                        Port("RoundTripMs", "往返耗时", "Number", false),
                        Port("RequestHex", "请求帧", "String", false),
                        Port("ResponseHex", "响应帧", "String", false),
                        Port("AttemptCount", "尝试次数", "Number", false),
                        Port("AttemptTrace", "逐次诊断", "String", false),
                        Port("ExceptionCode", "设备异常码", "Number", false),
                        Port("Diagnostics", "通讯诊断", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ModbusRtuExchange:
                    return GetOutputPorts(VmToolKind.ModbusTcpExchange)
                        .Where(port => port.PortName != "TransactionId")
                        .Concat(new[] { Port("Station", "RTU 站号", "Number", false) }).ToArray();
                case VmToolKind.TextAssembly:
                    return new[]
                    {
                        Port("Text", "组装文本", "String", false),
                        Port("Length", "文本长度", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TextParser:
                    return new[]
                    {
                        Port("Text", "解析文本", "String", false),
                        Port("Number", "解析数值", "Number", false),
                        Port("FieldCount", "字段数量", "Number", false),
                        Port("Matched", "是否匹配", "Boolean", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.NumberArrayReorder:
                case VmToolKind.TextArrayReorder:
                    return new[] { Port("Array", "整理后的数组", VmArrayReorderS108.DataType(kind), false), Port("Indices", "来源索引（从 0 开始）", "Number[]", false),
                        Port("Count", "输出数量", "Number", false), Port("SourceCount", "索引对应的原始数量", "Number", false), Port("FirstCount", "第一数组数量", "Number", false),
                        Port("SecondCount", "追加数组数量", "Number", false), Port("Operation", "整理任务", "String", false), Port("ResultCode", "结果码", "String", false) };
                case VmToolKind.NumericExpression:
                    return new[] { Port("Number", "公式结果", "Number", false), Port("VariableCount", "参与变量数", "Number", false), Port("Formula", "本次公式", "String", false), Port("ResultCode", "结果码", "String", false) };
                case VmToolKind.NumberArrayCreate:
                case VmToolKind.TextArraySplit:
                    return new[] { Port("Array", "数组", kind == VmToolKind.NumberArrayCreate ? "Number[]" : "String[]", false), Port("Count", "元素数量", "Number", false), Port("ResultCode", "结果码", "String", false) };
                case VmToolKind.NumberArrayStatistics:
                    return new[] { Port("SelectedValue", "所选数值", "Number", false), Port("Count", "元素数量", "Number", false), Port("Min", "最小值", "Number", false), Port("Max", "最大值", "Number", false), Port("Mean", "均值", "Number", false), Port("Sum", "总和", "Number", false), Port("Index", "索引（从 0 开始）", "Number", false), Port("ResultCode", "结果码", "String", false) };
                case VmToolKind.TextArraySelect:
                    return new[] { Port("Text", "所选文本", "String", false), Port("Count", "元素数量", "Number", false), Port("Length", "文本长度", "Number", false), Port("Index", "索引（从 0 开始）", "Number", false), Port("ResultCode", "结果码", "String", false) };
                case VmToolKind.NumericCalculation:
                    return new[]
                    {
                        Port("Number", "运算结果", "Number", false),
                        Port("OperandCount", "操作数数量", "Number", false),
                        Port("Operation", "运算类型", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ResultLogicAggregation:
                    return new[]
                    {
                        Port("ResultCode", "汇总结果码", "String", false),
                        Port("IsOk", "汇总是否 OK", "Boolean", false),
                        Port("InputCount", "参与输入数量", "Number", false),
                        Port("PassedCount", "输入 OK 数量", "Number", false)
                    };
                case VmToolKind.CSharpDllScript:
                    return new[]
                    {
                        Port("Number", "脚本数值输出", "Number", false),
                        Port("Text", "脚本文本输出", "String", false),
                        Port("ResultCode", "脚本结果码", "String", false),
                        Port("IsOk", "脚本是否 OK", "Boolean", false),
                        Port("AssemblyVersion", "程序集版本", "String", false),
                        Port("AssemblySha256", "程序集 SHA-256", "String", false),
                        Port("Diagnostics", "编译/调用诊断", "String", false),
                        Port("ExecutionMs", "脚本执行耗时", "Number", false)
                    };
                case VmToolKind.GlobalVariableRead:
                    return new[]
                    {
                        Port("NumberValue", "数值变量值", "Number", false),
                        Port("TextValue", "字符串变量值", "String", false),
                        Port("BooleanValue", "布尔变量值", "Boolean", false),
                        Port("ResultCodeValue", "结果码变量值", "String", false),
                        Port("NumberArrayValue", "数值数组变量值", "Number[]", false),
                        Port("StringArrayValue", "文本数组变量值", "String[]", false),
                        Port("VariableName", "变量名称", "String", false),
                        Port("ValueType", "变量类型", "String", false),
                        Port("ResultCode", "执行结果码", "String", false)
                    };
                case VmToolKind.GlobalVariableWrite:
                    return new[]
                    {
                        Port("NumberValue", "写后数值", "Number", false),
                        Port("TextValue", "写后字符串", "String", false),
                        Port("BooleanValue", "写后布尔值", "Boolean", false),
                        Port("ResultCodeValue", "写后结果码", "String", false),
                        Port("NumberArrayValue", "写后数值数组", "Number[]", false),
                        Port("StringArrayValue", "写后文本数组", "String[]", false),
                        Port("Changed", "值是否改变", "Boolean", false),
                        Port("VariableName", "变量名称", "String", false),
                        Port("ValueType", "变量类型", "String", false),
                        Port("ResultCode", "执行结果码", "String", false)
                    };
                case VmToolKind.PositionCorrection2D:
                    return new[]
                    {
                        Port("ReferenceRow", "基准 Row", "Number", false),
                        Port("ReferenceColumn", "基准 Column", "Number", false),
                        Port("ReferenceAngle", "基准角度（°）", "Number", false),
                        Port("CurrentRow", "当前 Row", "Number", false),
                        Port("CurrentColumn", "当前 Column", "Number", false),
                        Port("CurrentAngle", "当前角度（°）", "Number", false),
                        Port("DeltaRow", "Row 偏移", "Number", false),
                        Port("DeltaColumn", "Column 偏移", "Number", false),
                        Port("DeltaAngle", "角度偏移（°）", "Number", false),
                        Port("HomMat2D", "2D 刚体矩阵", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PointSetGeometryStatistics:
                    return new[]
                    {
                        Port("XLD", "点集/质心/外接框", "XLD", false),
                        Port("Centroid", "点集质心", "Point2D", false),
                        Port("PointCount", "有效点数量", "Number", false),
                        Port("SkippedCount", "跳过异常点数量", "Number", false),
                        Port("CentroidRow", "质心 Row", "Number", false),
                        Port("CentroidColumn", "质心 Column", "Number", false),
                        Port("MinRow", "最小 Row", "Number", false),
                        Port("MinColumn", "最小 Column", "Number", false),
                        Port("MaxRow", "最大 Row", "Number", false),
                        Port("MaxColumn", "最大 Column", "Number", false),
                        Port("RowSpan", "Row 跨度", "Number", false),
                        Port("ColumnSpan", "Column 跨度", "Number", false),
                        Port("MaxSpan", "最大跨度", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PointToLineDistanceMeasurement:
                    return new[]
                    {
                        Port("XLD", "点/垂足/参考线", "XLD", false),
                        Port("FootPoint", "垂足", "Point2D", false),
                        Port("Distance", "点线距离", "Number", false),
                        Port("SignedSide", "有符号侧值", "Number", false),
                        Port("Side", "点所在侧", "String", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PointToCircleRadialDeviationMeasurement:
                    return new[]
                    {
                        Port("XLD", "圆/径向线/关键点", "XLD", false),
                        Port("ClosestPoint", "最近圆周点", "Point2D", false),
                        Port("CenterDistance", "点到圆心距离", "Number", false),
                        Port("AbsoluteDeviation", "绝对径向偏差", "Number", false),
                        Port("SignedDeviation", "有符号径向偏差", "Number", false),
                        Port("RadialState", "圆内/圆外状态", "String", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PointToEllipseDistanceMeasurement:
                    return new[]
                    {
                        Port("XLD", "椭圆/测量线/关键点", "XLD", false),
                        Port("ClosestPoint", "椭圆最近点", "Point2D", false),
                        Port("Distance", "点到椭圆距离", "Number", false),
                        Port("SignedDistance", "有符号距离（外正内负）", "Number", false),
                        Port("Relation", "Inside/Outside/OnBoundary", "String", false),
                        Port("NormalizedRadius", "椭圆归一化半径", "Number", false),
                        Port("NormalAngle", "最近点外法线角（°）", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.LineEllipseIntersectionMeasurement:
                    return new[]
                    {
                        Port("XLD", "线段/椭圆/交点", "XLD", false),
                        Port("IntersectionPoints", "交点集合", "Point2D[]", false),
                        Port("Point1", "按线段方向第一交点", "Point2D", false),
                        Port("Point2", "按线段方向第二交点", "Point2D", false),
                        Port("IntersectionCount", "有限交点数量", "Number", false),
                        Port("ChordLength", "两交点弦长", "Number", false),
                        Port("Relation", "Disjoint/Tangent/Secant/Overlapping", "String", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.LineCircleIntersectionMeasurement:
                    return new[]
                    {
                        Port("XLD", "直线/圆/交点", "XLD", false),
                        Port("IntersectionPoints", "交点集合", "Point2D[]", false),
                        Port("PointA", "主交点 A", "Point2D", false),
                        Port("PointB", "主交点 B", "Point2D", false),
                        Port("IntersectionCount", "交点数量", "Number", false),
                        Port("ChordLength", "两交点弦长", "Number", false),
                        Port("Relation", "相离/相切/相交", "String", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.CircleCircleIntersectionMeasurement:
                    return new[]
                    {
                        Port("XLD", "两圆/圆心线/交点", "XLD", false),
                        Port("IntersectionPoints", "交点集合", "Point2D[]", false),
                        Port("PointA", "主交点 A", "Point2D", false),
                        Port("PointB", "主交点 B", "Point2D", false),
                        Port("CenterDistance", "两圆心距离", "Number", false),
                        Port("IntersectionCount", "有限交点数量", "Number", false),
                        Port("CommonChordLength", "公共弦长度", "Number", false),
                        Port("Relation", "圆圆关系", "String", false),
                        Port("IsOverlapping", "是否重合", "Boolean", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PointPairLineConstructionMeasurement:
                    return new[]
                    {
                        Port("XLD", "两点/线段/中点", "XLD", false),
                        Port("MainLine", "A 到 B 类型化直线", "Line2D", false),
                        Port("MidPoint", "两点中点", "Point2D", false),
                        Port("Distance", "两点距离", "Number", false),
                        Port("Angle", "A 到 B 方向角（°）", "Number", false),
                        Port("DeltaRow", "B.Row - A.Row", "Number", false),
                        Port("DeltaColumn", "B.Column - A.Column", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TypedLineLineRelationMeasurement:
                    return new[]
                    {
                        Port("XLD", "两直线/有限交点", "XLD", false),
                        Port("IntersectionPoint", "有限真实交点", "Point2D", false),
                        Port("SignedAngle", "A 到 B 有符号角（°）", "Number", false),
                        Port("AcuteAngle", "无向锐角（°）", "Number", false),
                        Port("ParallelDistance", "平行线距（px）", "Number", false),
                        Port("IsParallel", "是否平行", "Boolean", false),
                        Port("IsCoincident", "是否重合", "Boolean", false),
                        Port("Relation", "相交/平行/重合", "String", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TypedPointSetLineFittingMeasurement:
                    return new[]
                    {
                        Port("XLD", "输入点/拟合线/残差", "XLD", false),
                        Port("MainLine", "类型化拟合主直线", "Line2D", false),
                        Port("StartPoint", "拟合线起点", "Point2D", false),
                        Port("EndPoint", "拟合线终点", "Point2D", false),
                        Port("Angle", "直线方向角（°）", "Number", false),
                        Port("InputCount", "输入点数量", "Number", false),
                        Port("ValidCount", "有效点数量", "Number", false),
                        Port("RejectedCount", "拒绝点数量", "Number", false),
                        Port("MeanResidual", "平均点线残差", "Number", false),
                        Port("MaxResidual", "最大点线残差", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TypedPointSetCircleFittingMeasurement:
                    return new[]
                    {
                        Port("XLD", "输入点/拟合圆/径向残差", "XLD", false),
                        Port("MainCircle", "类型化拟合主圆", "Circle2D", false),
                        Port("CenterPoint", "类型化圆心", "Point2D", false),
                        Port("Radius", "拟合半径（px）", "Number", false),
                        Port("InputCount", "输入点数量", "Number", false),
                        Port("ValidCount", "有效点数量", "Number", false),
                        Port("RejectedCount", "拒绝点数量", "Number", false),
                        Port("MeanResidual", "平均径向残差", "Number", false),
                        Port("MaxResidual", "最大径向残差", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TypedPointSetEllipseFittingMeasurement:
                    return new[]
                    {
                        Port("XLD", "输入点/拟合椭圆/逐点残差", "XLD", false),
                        Port("MainEllipse", "类型化拟合主椭圆", "Ellipse2D", false),
                        Port("CenterPoint", "类型化椭圆中心", "Point2D", false),
                        Port("Phi", "主轴方向角（°）", "Number", false),
                        Port("Radius1", "长半轴（px）", "Number", false),
                        Port("Radius2", "短半轴（px）", "Number", false),
                        Port("AxisRatio", "长短半轴比", "Number", false),
                        Port("InputCount", "输入点数量", "Number", false),
                        Port("ValidCount", "有效点数量", "Number", false),
                        Port("RejectedCount", "拒绝点数量", "Number", false),
                        Port("MeanResidual", "平均点椭圆残差", "Number", false),
                        Port("MaxResidual", "最大点椭圆残差", "Number", false),
                        Port("CoordinateContext", "坐标上下文", "String", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.CaliperEdgePairMeasurement:
                    return new[]
                    {
                        Port("XLD", "测量矩形/边缘点/配对线", "XLD", false),
                        Port("PrimaryDistance", "主边缘对间距", "Number", false),
                        Port("PairCount", "边缘对数量", "Number", false),
                        Port("FirstRows", "第一边缘 Row 数组", "String", false),
                        Port("FirstColumns", "第一边缘 Column 数组", "String", false),
                        Port("FirstAmplitudes", "第一边缘幅值数组", "String", false),
                        Port("SecondRows", "第二边缘 Row 数组", "String", false),
                        Port("SecondColumns", "第二边缘 Column 数组", "String", false),
                        Port("SecondAmplitudes", "第二边缘幅值数组", "String", false),
                        Port("IntraDistances", "边缘对内部距离数组", "String", false),
                        Port("InterDistances", "相邻边缘对距离数组", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.LinearCaliperArrayMeasurement:
                    return new[]
                    {
                        Port("XLD", "阵列基线/卡尺/边缘点/配对线", "XLD", false),
                        Port("MeanDistance", "有效卡尺平均主间距", "Number", false),
                        Port("ValidCount", "有效卡尺数量", "Number", false),
                        Port("PairCount", "边缘对总数量", "Number", false),
                        Port("MinDistance", "有效卡尺最小主间距", "Number", false),
                        Port("MaxDistance", "有效卡尺最大主间距", "Number", false),
                        Port("StdDev", "有效卡尺主间距标准差", "Number", false),
                        Port("Distances", "逐卡尺主间距", "String", false),
                        Port("ValidIndices", "有效卡尺索引", "String", false),
                        Port("FailedIndices", "失败卡尺索引", "String", false),
                        Port("FailureDetails", "失败原因", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.CircularCaliperCircleFinder:
                    return new[]
                    {
                        Port("XLD", "搜索圆/卡尺/边缘点/拟合圆", "XLD", false),
                        Port("CenterRow", "拟合圆心 Row", "Number", false),
                        Port("CenterColumn", "拟合圆心 Column", "Number", false),
                        Port("Radius", "拟合圆半径", "Number", false),
                        Port("ValidCount", "有效边缘点数量", "Number", false),
                        Port("FailedCount", "失败卡尺数量", "Number", false),
                        Port("MaxResidual", "最大径向残差", "Number", false),
                        Port("MeanAmplitude", "平均边缘幅值", "Number", false),
                        Port("EdgePoints", "类型化边缘点集", "Point2D[]", false),
                        Port("EdgeRows", "边缘点 Row 数组", "String", false),
                        Port("EdgeColumns", "边缘点 Column 数组", "String", false),
                        Port("EdgeAmplitudes", "边缘幅值数组", "String", false),
                        Port("ValidIndices", "有效卡尺索引", "String", false),
                        Port("FailedIndices", "失败卡尺索引", "String", false),
                        Port("FailureDetails", "失败原因", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.RingWallThicknessMeasurement:
                    return new[]
                    {
                        Port("XLD", "搜索圆/径向卡尺/内外边缘点/拟合圆", "XLD", false),
                        Port("InnerCenterRow", "内圆圆心 Row", "Number", false),
                        Port("InnerCenterColumn", "内圆圆心 Column", "Number", false),
                        Port("InnerRadius", "内圆半径", "Number", false),
                        Port("OuterCenterRow", "外圆圆心 Row", "Number", false),
                        Port("OuterCenterColumn", "外圆圆心 Column", "Number", false),
                        Port("OuterRadius", "外圆半径", "Number", false),
                        Port("MeanThickness", "平均壁厚", "Number", false),
                        Port("MinThickness", "最小壁厚", "Number", false),
                        Port("MaxThickness", "最大壁厚", "Number", false),
                        Port("StdDevThickness", "壁厚标准差", "Number", false),
                        Port("Eccentricity", "内外圆心偏心量", "Number", false),
                        Port("ValidCount", "有效径向卡尺数量", "Number", false),
                        Port("FailedCount", "失败径向卡尺数量", "Number", false),
                        Port("MaxInnerResidual", "内圆最大径向残差", "Number", false),
                        Port("MaxOuterResidual", "外圆最大径向残差", "Number", false),
                        Port("Thicknesses", "逐卡尺壁厚", "String", false),
                        Port("InnerRows", "内边缘 Row 数组", "String", false),
                        Port("InnerColumns", "内边缘 Column 数组", "String", false),
                        Port("OuterRows", "外边缘 Row 数组", "String", false),
                        Port("OuterColumns", "外边缘 Column 数组", "String", false),
                        Port("ValidIndices", "有效卡尺索引", "String", false),
                        Port("FailedIndices", "失败卡尺索引", "String", false),
                        Port("FailureDetails", "失败原因", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.RegionContourMeasurement:
                    return new[]
                    {
                        Port("XLD", "轮廓对象", "XLD", false),
                        Port("TotalLength", "轮廓总长度", "Number", false),
                        Port("ContourCount", "轮廓数量", "Number", false),
                        Port("Mode", "轮廓模式", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldContourFilter:
                    return new[]
                    {
                        Port("XLD", "筛选轮廓", "XLD", false),
                        Port("TotalLength", "轮廓总长度", "Number", false),
                        Port("ContourCount", "轮廓数量", "Number", false),
                        Port("Feature", "筛选特征", "String", false),
                        Port("Range", "筛选范围", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldAdjacentContourUnion:
                    return new[]
                    {
                        Port("XLD", "连接后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("JoinedCount", "已连接数量", "Number", false),
                        Port("TotalLength", "输出轮廓总长度", "Number", false),
                        Port("Mode", "属性策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldContourSmoothing:
                    return new[]
                    {
                        Port("XLD", "平滑后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("InputTotalLength", "平滑前总长度", "Number", false),
                        Port("TotalLength", "平滑后总长度", "Number", false),
                        Port("LengthDelta", "长度变化", "Number", false),
                        Port("NumRegrPoints", "回归点数", "Number", false),
                        Port("AttributePolicy", "属性语义", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldAffineTransform:
                    return new[]
                    {
                        Port("XLD", "仿射变换后轮廓", "XLD", false),
                        Port("ContourCount", "轮廓数量", "Number", false),
                        Port("InputTotalLength", "变换前总长度", "Number", false),
                        Port("TotalLength", "变换后总长度", "Number", false),
                        Port("MinRow", "输出最小 Row", "Number", false),
                        Port("MinColumn", "输出最小 Column", "Number", false),
                        Port("MaxRow", "输出最大 Row", "Number", false),
                        Port("MaxColumn", "输出最大 Column", "Number", false),
                        Port("CenterRow", "变换中心 Row", "Number", false),
                        Port("CenterColumn", "变换中心 Column", "Number", false),
                        Port("HomMat2D", "二维仿射矩阵", "String", false),
                        Port("TransformOrder", "组合顺序", "String", false),
                        Port("CoordinatePolicy", "坐标策略", "String", false),
                        Port("AttributePolicy", "属性语义", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldProjectiveTransform:
                    return new[]
                    {
                        Port("XLD", "透视变换后轮廓", "XLD", false),
                        Port("ContourCount", "轮廓数量", "Number", false),
                        Port("InputTotalLength", "变换前总长度", "Number", false),
                        Port("TotalLength", "变换后总长度", "Number", false),
                        Port("MinRow", "输出最小 Row", "Number", false),
                        Port("MinColumn", "输出最小 Column", "Number", false),
                        Port("MaxRow", "输出最大 Row", "Number", false),
                        Port("MaxColumn", "输出最大 Column", "Number", false),
                        Port("HomMat2D", "二维投影矩阵", "String", false),
                        Port("PointError", "四点最大映射误差", "Number", false),
                        Port("CoordinatePolicy", "坐标策略", "String", false),
                        Port("AttributePolicy", "属性语义", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldContourCrop:
                    return new[]
                    {
                        Port("XLD", "裁剪后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("InputTotalLength", "裁剪前总长度", "Number", false),
                        Port("TotalLength", "裁剪后总长度", "Number", false),
                        Port("MinRow", "输出最小 Row", "Number", false),
                        Port("MinColumn", "输出最小 Column", "Number", false),
                        Port("MaxRow", "输出最大 Row", "Number", false),
                        Port("MaxColumn", "输出最大 Column", "Number", false),
                        Port("CropRectangle", "裁剪矩形", "String", false),
                        Port("CloseContours", "闭合策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldLengthClassification:
                    return new[]
                    {
                        Port("ShortXLD", "短轮廓", "XLD", false),
                        Port("MediumXLD", "中轮廓", "XLD", false),
                        Port("LongXLD", "长轮廓", "XLD", false),
                        Port("ShortCount", "短轮廓数量", "Number", false),
                        Port("ShortTotalLength", "短轮廓总长度", "Number", false),
                        Port("ShortMinLength", "短轮廓最小长度", "Number", false),
                        Port("ShortMaxLength", "短轮廓最大长度", "Number", false),
                        Port("ShortMeanLength", "短轮廓平均长度", "Number", false),
                        Port("MediumCount", "中轮廓数量", "Number", false),
                        Port("MediumTotalLength", "中轮廓总长度", "Number", false),
                        Port("MediumMinLength", "中轮廓最小长度", "Number", false),
                        Port("MediumMaxLength", "中轮廓最大长度", "Number", false),
                        Port("MediumMeanLength", "中轮廓平均长度", "Number", false),
                        Port("LongCount", "长轮廓数量", "Number", false),
                        Port("LongTotalLength", "长轮廓总长度", "Number", false),
                        Port("LongMinLength", "长轮廓最小长度", "Number", false),
                        Port("LongMaxLength", "长轮廓最大长度", "Number", false),
                        Port("LongMeanLength", "长轮廓平均长度", "Number", false),
                        Port("ShortMaximum", "短轮廓上限", "Number", false),
                        Port("MediumMaximum", "中轮廓上限", "Number", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldLengthSortSelection:
                    return new[]
                    {
                        Port("XLD", "排序选择后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "选择轮廓数量", "Number", false),
                        Port("TotalLength", "选择总长度", "Number", false),
                        Port("MinLength", "选择最小长度", "Number", false),
                        Port("MaxLength", "选择最大长度", "Number", false),
                        Port("MeanLength", "选择平均长度", "Number", false),
                        Port("OriginalIndices", "原始索引顺序", "String", false),
                        Port("Lengths", "选择长度顺序", "String", false),
                        Port("SortDirection", "排序方向", "String", false),
                        Port("SelectionMode", "选择模式", "String", false),
                        Port("RankRange", "排名范围", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldPointClosureFilter:
                    return new[]
                    {
                        Port("XLD", "筛选后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("InputOpenCount", "输入开放轮廓数", "Number", false),
                        Port("InputClosedCount", "输入闭合轮廓数", "Number", false),
                        Port("SelectedOpenCount", "输出开放轮廓数", "Number", false),
                        Port("SelectedClosedCount", "输出闭合轮廓数", "Number", false),
                        Port("MinPointCount", "输出最小点数", "Number", false),
                        Port("MaxPointCount", "输出最大点数", "Number", false),
                        Port("MeanPointCount", "输出平均点数", "Number", false),
                        Port("OriginalIndices", "原始索引", "String", false),
                        Port("PointCounts", "逐轮廓点数", "String", false),
                        Port("ClosureMode", "开闭筛选", "String", false),
                        Port("ClosureTolerance", "闭合容差", "Number", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldAreaDirectionFilter:
                    return new[]
                    {
                        Port("XLD", "面积/方向筛选后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("InputOpenCount", "输入开放轮廓数", "Number", false),
                        Port("InputClosedCount", "输入闭合轮廓数", "Number", false),
                        Port("DegenerateCount", "退化轮廓数", "Number", false),
                        Port("ClockwiseCount", "有效顺时针数", "Number", false),
                        Port("CounterclockwiseCount", "有效逆时针数", "Number", false),
                        Port("SelectedClockwiseCount", "输出顺时针数", "Number", false),
                        Port("SelectedCounterclockwiseCount", "输出逆时针数", "Number", false),
                        Port("MinArea", "输出最小绝对面积", "Number", false),
                        Port("MaxArea", "输出最大绝对面积", "Number", false),
                        Port("MeanArea", "输出平均绝对面积", "Number", false),
                        Port("OriginalIndices", "原始索引", "String", false),
                        Port("Areas", "逐轮廓绝对面积", "String", false),
                        Port("DirectionMode", "方向筛选", "String", false),
                        Port("ClosureTolerance", "闭合容差", "Number", false),
                        Port("RejectPolicy", "开放/退化策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldBoundingCompactnessFilter:
                    return new[]
                    {
                        Port("XLD", "外接框/紧凑度筛选后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("InputOpenCount", "输入开放轮廓数", "Number", false),
                        Port("InputClosedCount", "输入闭合轮廓数", "Number", false),
                        Port("SelfIntersectingCount", "自交轮廓数", "Number", false),
                        Port("DegenerateCount", "退化轮廓数", "Number", false),
                        Port("MinWidth", "输出最小宽度", "Number", false),
                        Port("MaxWidth", "输出最大宽度", "Number", false),
                        Port("MeanWidth", "输出平均宽度", "Number", false),
                        Port("MinHeight", "输出最小高度", "Number", false),
                        Port("MaxHeight", "输出最大高度", "Number", false),
                        Port("MeanHeight", "输出平均高度", "Number", false),
                        Port("MinCompactness", "输出最小紧凑度", "Number", false),
                        Port("MaxCompactness", "输出最大紧凑度", "Number", false),
                        Port("MeanCompactness", "输出平均紧凑度", "Number", false),
                        Port("OriginalIndices", "原始索引", "String", false),
                        Port("Widths", "逐轮廓宽度", "String", false),
                        Port("Heights", "逐轮廓高度", "String", false),
                        Port("CompactnessValues", "逐轮廓紧凑度", "String", false),
                        Port("ClosureTolerance", "闭合容差", "Number", false),
                        Port("RejectPolicy", "开放/自交/退化策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldCircularityConvexityFilter:
                    return new[]
                    {
                        Port("XLD", "圆度/凸性筛选后轮廓", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("ContourCount", "输出轮廓数量", "Number", false),
                        Port("InputOpenCount", "输入开放轮廓数", "Number", false),
                        Port("InputClosedCount", "输入闭合轮廓数", "Number", false),
                        Port("SelfIntersectingCount", "自交轮廓数", "Number", false),
                        Port("DegenerateCount", "退化轮廓数", "Number", false),
                        Port("MinCircularity", "输出最小圆度", "Number", false),
                        Port("MaxCircularity", "输出最大圆度", "Number", false),
                        Port("MeanCircularity", "输出平均圆度", "Number", false),
                        Port("MinConvexity", "输出最小凸性", "Number", false),
                        Port("MaxConvexity", "输出最大凸性", "Number", false),
                        Port("MeanConvexity", "输出平均凸性", "Number", false),
                        Port("OriginalIndices", "原始索引", "String", false),
                        Port("CircularityValues", "逐轮廓圆度", "String", false),
                        Port("ConvexityValues", "逐轮廓凸性", "String", false),
                        Port("ClosureTolerance", "闭合容差", "Number", false),
                        Port("RejectPolicy", "开放/自交/退化策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldEnclosingCircleDiameterMeasurement:
                    return new[]
                    {
                        Port("XLD", "最小包围圆与最大直径线", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("MeasuredCount", "有效测量数量", "Number", false),
                        Port("ContourCount", "范围内输出数量", "Number", false),
                        Port("OpenCount", "输入开放轮廓数", "Number", false),
                        Port("ClosedCount", "输入闭合轮廓数", "Number", false),
                        Port("SelfIntersectingCount", "自交轮廓数", "Number", false),
                        Port("DegenerateCount", "退化轮廓数", "Number", false),
                        Port("CenterRow", "主结果圆心 Row", "Number", false),
                        Port("CenterColumn", "主结果圆心 Column", "Number", false),
                        Port("Radius", "主结果最小包围圆半径", "Number", false),
                        Port("Diameter", "主结果最大点间直径", "Number", false),
                        Port("StartRow", "主直径起点 Row", "Number", false),
                        Port("StartColumn", "主直径起点 Column", "Number", false),
                        Port("EndRow", "主直径终点 Row", "Number", false),
                        Port("EndColumn", "主直径终点 Column", "Number", false),
                        Port("MinRadius", "输出最小半径", "Number", false),
                        Port("MaxRadius", "输出最大半径", "Number", false),
                        Port("MeanRadius", "输出平均半径", "Number", false),
                        Port("MinDiameter", "输出最小直径", "Number", false),
                        Port("MaxDiameter", "输出最大直径", "Number", false),
                        Port("MeanDiameter", "输出平均直径", "Number", false),
                        Port("OriginalIndices", "原始索引", "String", false),
                        Port("Radii", "逐轮廓半径", "String", false),
                        Port("Diameters", "逐轮廓直径", "String", false),
                        Port("ContourPolicy", "开闭轮廓策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldMinimumRectangleDirectionMeasurement:
                    return new[]
                    {
                        Port("XLD", "最小旋转矩形", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("MeasuredCount", "有效测量数量", "Number", false),
                        Port("ContourCount", "范围内输出数量", "Number", false),
                        Port("OpenCount", "输入开放轮廓数", "Number", false),
                        Port("ClosedCount", "输入闭合轮廓数", "Number", false),
                        Port("SelfIntersectingCount", "自交轮廓数", "Number", false),
                        Port("DegenerateCount", "退化轮廓数", "Number", false),
                        Port("CenterRow", "主矩形中心 Row", "Number", false),
                        Port("CenterColumn", "主矩形中心 Column", "Number", false),
                        Port("Phi", "主轴方向角（弧度）", "Number", false),
                        Port("AngleDegrees", "主轴方向角（度）", "Number", false),
                        Port("Length1", "主边半长", "Number", false),
                        Port("Length2", "次边半长", "Number", false),
                        Port("Width", "主边全长", "Number", false),
                        Port("Height", "次边全长", "Number", false),
                        Port("Area", "矩形面积", "Number", false),
                        Port("Corner1Row", "角点1 Row", "Number", false),
                        Port("Corner1Column", "角点1 Column", "Number", false),
                        Port("Corner2Row", "角点2 Row", "Number", false),
                        Port("Corner2Column", "角点2 Column", "Number", false),
                        Port("Corner3Row", "角点3 Row", "Number", false),
                        Port("Corner3Column", "角点3 Column", "Number", false),
                        Port("Corner4Row", "角点4 Row", "Number", false),
                        Port("Corner4Column", "角点4 Column", "Number", false),
                        Port("MinWidth", "输出最小主边", "Number", false),
                        Port("MaxWidth", "输出最大主边", "Number", false),
                        Port("MeanWidth", "输出平均主边", "Number", false),
                        Port("MinHeight", "输出最小次边", "Number", false),
                        Port("MaxHeight", "输出最大次边", "Number", false),
                        Port("MeanHeight", "输出平均次边", "Number", false),
                        Port("MinArea", "输出最小面积", "Number", false),
                        Port("MaxArea", "输出最大面积", "Number", false),
                        Port("MeanArea", "输出平均面积", "Number", false),
                        Port("OriginalIndices", "原始索引", "String", false),
                        Port("Widths", "逐轮廓主边", "String", false),
                        Port("Heights", "逐轮廓次边", "String", false),
                        Port("AnglesDegrees", "逐轮廓方向角", "String", false),
                        Port("Areas", "逐轮廓面积", "String", false),
                        Port("ContourPolicy", "开闭轮廓策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldDirectionConsistencyMeasurement:
                    return new[]
                    {
                        Port("XLD", "方向矩形与主轴", "XLD", false),
                        Port("InputContourCount", "输入轮廓数量", "Number", false),
                        Port("MeasuredCount", "稳定测量数量", "Number", false),
                        Port("PassCount", "偏差合格数量", "Number", false),
                        Port("FailCount", "偏差不合格数量", "Number", false),
                        Port("UnstableCount", "近正方形不稳定数量", "Number", false),
                        Port("DegenerateCount", "退化轮廓数量", "Number", false),
                        Port("OpenCount", "开放轮廓数量", "Number", false),
                        Port("ClosedCount", "闭合轮廓数量", "Number", false),
                        Port("PrimaryAngle", "主轮廓方向角", "Number", false),
                        Port("TargetAngle", "目标方向角", "Number", false),
                        Port("SignedDeviation", "主轮廓有符号偏差", "Number", false),
                        Port("AbsoluteDeviation", "主轮廓绝对偏差", "Number", false),
                        Port("MinDeviation", "最小绝对偏差", "Number", false),
                        Port("MaxDeviation", "最大绝对偏差", "Number", false),
                        Port("MeanDeviation", "平均绝对偏差", "Number", false),
                        Port("PassIndices", "合格原始索引", "String", false),
                        Port("FailIndices", "不合格原始索引", "String", false),
                        Port("AnglesDegrees", "逐轮廓方向角", "String", false),
                        Port("SignedDeviations", "逐轮廓有符号偏差", "String", false),
                        Port("AbsoluteDeviations", "逐轮廓绝对偏差", "String", false),
                        Port("ContourPolicy", "轮廓与稳定性策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.SubpixelEdgeExtraction:
                    return new[]
                    {
                        Port("XLD", "亚像素边缘轮廓", "XLD", false),
                        Port("TotalLength", "轮廓总长度", "Number", false),
                        Port("ContourCount", "轮廓数量", "Number", false),
                        Port("Filter", "边缘滤波器", "String", false),
                        Port("ColorPolicy", "彩色处理策略", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldContourSegmentation:
                    return new[]
                    {
                        Port("XLD", "分段轮廓", "XLD", false),
                        Port("TotalLength", "分段总长度", "Number", false),
                        Port("SegmentCount", "分段数量", "Number", false),
                        Port("LineCount", "直线段数量", "Number", false),
                        Port("ArcCount", "圆弧/椭圆弧数量", "Number", false),
                        Port("Mode", "分段模式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldLineFitting:
                    return new[]
                    {
                        Port("XLD", "拟合直线", "XLD", false),
                        Port("MainLine", "类型化主直线", "Line2D", false),
                        Port("LineCount", "直线数量", "Number", false),
                        Port("TotalLength", "直线总长度", "Number", false),
                        Port("MeanAngle", "平均方向角", "Number", false),
                        Port("MaxResidual", "最大点线残差", "Number", false),
                        Port("StartRow", "主线起点行", "Number", false),
                        Port("StartColumn", "主线起点列", "Number", false),
                        Port("EndRow", "主线终点行", "Number", false),
                        Port("EndColumn", "主线终点列", "Number", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldCircleFitting:
                    return new[]
                    {
                        Port("XLD", "拟合圆弧", "XLD", false),
                        Port("MainCircle", "类型化主圆", "Circle2D", false),
                        Port("CircleCount", "圆数量", "Number", false),
                        Port("CenterRow", "主圆圆心行", "Number", false),
                        Port("CenterColumn", "主圆圆心列", "Number", false),
                        Port("Radius", "主圆半径", "Number", false),
                        Port("StartPhi", "主圆起始角", "Number", false),
                        Port("EndPhi", "主圆终止角", "Number", false),
                        Port("PointOrder", "主圆点序", "String", false),
                        Port("MaxResidual", "最大径向残差", "Number", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldEllipseFitting:
                    return new[]
                    {
                        Port("XLD", "拟合椭圆弧", "XLD", false),
                        Port("EllipseCount", "椭圆数量", "Number", false),
                        Port("CenterRow", "主椭圆中心行", "Number", false),
                        Port("CenterColumn", "主椭圆中心列", "Number", false),
                        Port("Phi", "主轴方向角", "Number", false),
                        Port("Radius1", "长半轴", "Number", false),
                        Port("Radius2", "短半轴", "Number", false),
                        Port("StartPhi", "主椭圆起始角", "Number", false),
                        Port("EndPhi", "主椭圆终止角", "Number", false),
                        Port("PointOrder", "主椭圆点序", "String", false),
                        Port("MaxResidual", "最大椭圆残差", "Number", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldRectangleFitting:
                    return new[]
                    {
                        Port("XLD", "拟合旋转矩形", "XLD", false),
                        Port("RectangleCount", "矩形数量", "Number", false),
                        Port("CenterRow", "主矩形中心行", "Number", false),
                        Port("CenterColumn", "主矩形中心列", "Number", false),
                        Port("Phi", "Length1 边方向角", "Number", false),
                        Port("Length1", "Length1 边半长", "Number", false),
                        Port("Length2", "Length2 边半长", "Number", false),
                        Port("Corner1Row", "角点1行", "Number", false),
                        Port("Corner1Column", "角点1列", "Number", false),
                        Port("Corner2Row", "角点2行", "Number", false),
                        Port("Corner2Column", "角点2列", "Number", false),
                        Port("Corner3Row", "角点3行", "Number", false),
                        Port("Corner3Column", "角点3列", "Number", false),
                        Port("Corner4Row", "角点4行", "Number", false),
                        Port("Corner4Column", "角点4列", "Number", false),
                        Port("PointOrder", "主矩形点序", "String", false),
                        Port("OrientationState", "方向稳定性", "String", false),
                        Port("MaxResidual", "最大边界残差", "Number", false),
                        Port("Algorithm", "拟合算法", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.XldLineRelationMeasurement:
                    return new[]
                    {
                        Port("XLD", "拟合直线与交点", "XLD", false),
                        Port("IntersectionRow", "交点行", "Number", false),
                        Port("IntersectionColumn", "交点列", "Number", false),
                        Port("IntersectionValid", "交点有效", "Boolean", false),
                        Port("DirectionA", "直线 A 方向角", "Number", false),
                        Port("DirectionB", "直线 B 方向角", "Number", false),
                        Port("SignedAngle", "有向夹角", "Number", false),
                        Port("AcuteAngle", "锐角夹角", "Number", false),
                        Port("ParallelDistance", "平行线间距", "Number", false),
                        Port("IsParallel", "是否平行", "Boolean", false),
                        Port("IsCoincident", "是否重合/近重合", "Boolean", false),
                        Port("Relation", "直线关系", "String", false),
                        Port("ResidualA", "直线 A 最大残差", "Number", false),
                        Port("ResidualB", "直线 B 最大残差", "Number", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageChannel:
                    return new[]
                    {
                        Port("Image", "输出图像", "Image", false),
                        Port("InputChannels", "输入通道数", "Number", false),
                        Port("OutputChannels", "输出通道数", "Number", false),
                        Port("Width", "图像宽度", "Number", false),
                        Port("Height", "图像高度", "Number", false),
                        Port("Mode", "处理模式", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageFilter:
                    return new[]
                    {
                        Port("Image", "滤波图像", "Image", false),
                        Port("Width", "图像宽度", "Number", false),
                        Port("Height", "图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("Mode", "滤波模式", "String", false),
                        Port("Kernel", "滤波模板", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageEnhancement:
                    return new[]
                    {
                        Port("Image", "增强图像", "Image", false),
                        Port("Width", "图像宽度", "Number", false),
                        Port("Height", "图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("Mode", "增强模式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageResize:
                    return new[]
                    {
                        Port("Image", "缩放图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("ScaleRow", "行方向缩放比例", "Number", false),
                        Port("ScaleColumn", "列方向缩放比例", "Number", false),
                        Port("Interpolation", "插值方式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageMirror:
                    return new[]
                    {
                        Port("Image", "镜像图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("Mode", "镜像方向", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageRotate:
                    return new[]
                    {
                        Port("Image", "旋转图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("Angle", "逆时针旋转角度", "Number", false),
                        Port("Interpolation", "插值方式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageAffineTransform:
                    return new[]
                    {
                        Port("Image", "仿射变换图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("HomMat2D", "仿射矩阵", "String", false),
                        Port("Interpolation", "插值方式", "String", false),
                        Port("AdaptImageSize", "自适应尺寸", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageProjectiveTransform:
                    return new[]
                    {
                        Port("Image", "透视校正图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("HomMat2D", "投影矩阵", "String", false),
                        Port("Interpolation", "插值方式", "String", false),
                        Port("OutputMode", "输出尺寸模式", "String", false),
                        Port("TransformDomain", "变换输入域", "String", false),
                        Port("PointError", "四点映射最大误差", "Number", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.CameraDistortionCorrection:
                    return new[]
                    {
                        Port("Image", "畸变校正图像", "Image", false),
                        Port("Width", "输出宽度", "Number", false), Port("Height", "输出高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("CameraModel", "相机模型", "String", false),
                        Port("CalibrationSource", "内参来源", "String", false),
                        Port("RectifyMode", "视野模式", "String", false),
                        Port("InputImageContext", "原图坐标身份", "String", false),
                        Port("OutputImageContext", "校正图坐标身份", "String", false),
                        Port("Point", "校正坐标点", "Point2D", true),
                        Port("XLD", "校正坐标轮廓", "XLD", true),
                        Port("PointMarker", "校正点标记", "XLD", true),
                        Port("MappedObjectCount", "迁移点/轮廓数量", "Number", false),
                        Port("CalibrationRmsPx", "标定重投影 RMS（像素）", "Number", false),
                        Port("CalibrationSampleCount", "成功标定图数", "Number", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.PolarImageUnwrap:
                    return new[]
                    {
                        Port("Image", "极坐标展开图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("AngleRange", "角度范围", "String", false),
                        Port("RadiusRange", "半径范围", "String", false),
                        Port("Interpolation", "插值方式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.RoiImageExtraction:
                    return new[]
                    {
                        Port("Image", "ROI 提取图像", "Image", false),
                        Port("Width", "输出图像宽度", "Number", false),
                        Port("Height", "输出图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("OffsetRow", "相对原图 Row 偏移", "Number", false),
                        Port("OffsetColumn", "相对原图 Column 偏移", "Number", false),
                        Port("Mode", "提取模式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageDifference:
                    return new[]
                    {
                        Port("Image", "差分图像", "Image", false),
                        Port("Width", "图像宽度", "Number", false),
                        Port("Height", "图像高度", "Number", false),
                        Port("Channels", "图像通道数", "Number", false),
                        Port("Mode", "差分模式", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.DynamicThreshold:
                    return new[]
                    {
                        Port("Region", "动态阈值区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "连通区域数", "Number", false),
                        Port("Polarity", "明暗极性", "String", false),
                        Port("Offset", "灰度偏移", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ImageThreshold:
                    return new[]
                    {
                        Port("Region", "分割区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "连通区域数", "Number", false),
                        Port("Threshold", "阈值摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.AutomaticThreshold:
                    return new[]
                    {
                        Port("Region", "自动分割区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "连通区域数", "Number", false),
                        Port("Threshold", "自动阈值", "Number", false),
                        Port("Polarity", "目标明暗", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.LocalAdaptiveThreshold:
                    return new[]
                    {
                        Port("Region", "局部自适应分割区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "连通区域数", "Number", false),
                        Port("WindowWidth", "局部窗口宽度", "Number", false),
                        Port("WindowHeight", "局部窗口高度", "Number", false),
                        Port("Offset", "最小灰差", "Number", false),
                        Port("StdDevScale", "标准差系数", "Number", false),
                        Port("Polarity", "目标明暗", "String", false),
                        Port("Parameters", "参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ColorInspectionHsv:
                    return new[]
                    {
                        Port("Region", "匹配颜色区域", "Region", false), Port("Area", "匹配面积（像素）", "Number", false),
                        Port("AreaRatio", "覆盖比例（%）", "Number", false), Port("RegionCount", "连通区域数", "Number", false),
                        Port("SampleCount", "检测有效像素数", "Number", false), Port("HasMatch", "存在匹配颜色", "Boolean", false),
                        Port("HueCenter", "目标色相（度）", "Number", false), Port("HueTolerance", "色相容差（度）", "Number", false),
                        Port("Parameters", "HSV 范围与幅值契约", "String", false), Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.VariationSurfaceInspection:
                    return new[]
                    {
                        Port("Region", "筛选后的缺陷区域", "Region", false), Port("Area", "缺陷面积（像素）", "Number", false),
                        Port("AreaRatio", "缺陷比例（%）", "Number", false), Port("RawArea", "未筛选差异面积", "Number", false),
                        Port("RegionCount", "缺陷数量", "Number", false), Port("SampleCount", "学习良品数", "Number", false),
                        Port("EffectivePixels", "检测有效像素", "Number", false), Port("HasDefect", "存在筛选后缺陷", "Boolean", false),
                        Port("ModelVersion", "模型版本 SHA-256", "String", false), Port("Parameters", "容差与筛选摘要", "String", false),
                        Port("ResultCode", "执行结果码", "String", false)
                    };
                case VmToolKind.GrayHistogram:
                    return new[]
                    {
                        Port("Region", "实际统计有效域", "Region", false),
                        Port("Mean", "平均灰度", "Number", false),
                        Port("Deviation", "灰度标准差", "Number", false),
                        Port("Minimum", "最小灰度", "Number", false),
                        Port("Maximum", "最大灰度", "Number", false),
                        Port("DynamicRange", "动态范围", "Number", false),
                        Port("PeakGray", "峰值灰度", "Number", false),
                        Port("PeakCount", "峰值像素数", "Number", false),
                        Port("P10", "P10 灰度", "Number", false),
                        Port("P50", "P50 中位灰度", "Number", false),
                        Port("P90", "P90 灰度", "Number", false),
                        Port("Percentile", "自选分位灰度", "Number", false),
                        Port("SampleCount", "有效像素数", "Number", false),
                        Port("Parameters", "统计契约", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.GrayLineProfile:
                    return new[]
                    {
                        Port("XLD", "实际采样线", "XLD", false),
                        Port("Samples", "沿线灰度数组", "Number[]", false),
                        Port("Mean", "平均灰度", "Number", false),
                        Port("Deviation", "灰度标准差", "Number", false),
                        Port("Minimum", "最小灰度", "Number", false),
                        Port("Maximum", "最大灰度", "Number", false),
                        Port("Range", "灰度跨度", "Number", false),
                        Port("SampleCount", "采样点数", "Number", false),
                        Port("Length", "采样线长 (px)", "Number", false),
                        Port("MinimumRow", "最小值 Y", "Number", false),
                        Port("MinimumColumn", "最小值 X", "Number", false),
                        Port("MaximumRow", "最大值 Y", "Number", false),
                        Port("MaximumColumn", "最大值 X", "Number", false),
                        Port("MinimumDistance", "最小值沿线距离", "Number", false),
                        Port("MaximumDistance", "最大值沿线距离", "Number", false),
                        Port("Parameters", "采样契约", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.RegionMorphology:
                    return new[]
                    {
                        Port("Region", "形态学区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "连通区域数", "Number", false),
                        Port("Operation", "操作摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.RegionFeatureFilter:
                    return new[]
                    {
                        Port("Region", "筛选区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "区域数量", "Number", false),
                        Port("Feature", "筛选特征", "String", false),
                        Port("Range", "筛选范围", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.RegionSetOperation:
                    return new[]
                    {
                        Port("Region", "集合运算区域", "Region", false),
                        Port("Area", "区域总面积", "Number", false),
                        Port("RegionCount", "区域数量", "Number", false),
                        Port("Operation", "集合操作", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.ShapeMatch:
                    return new[]
                    {
                        Port("XLD", "候选模型轮廓", "XLD", true),
                        Port("Pose2D", "最佳目标位姿", "Pose2D", true),
                        Port("Centers", "同序候选像素中心", "Point2D[]", false),
                        Port("Scores", "同序真实候选分数", "Number[]", false),
                        Port("Angles", "同序真实候选角度（°）", "Number[]", false),
                        Port("Scales", "同序真实候选尺度（位姿不含缩放）", "Number[]", false),
                        Port("SourceIndices", "本次算法候选原编号（非跟踪ID）", "Number[]", false),
                        Port("TargetSortMode", "本次正式候选排序规则", "String", false),
                        Port("TargetStatus", "本次多目标结果状态", "String", false),
                        Port("TargetDefinitionKey", "本次模板参数/订阅定义身份", "String", false),
                        Port("TargetModelVersion", "本次实际模板模型版本", "String", false),
                        Port("TargetInputVersion", "本次正式输入图像版本身份", "String", false),
                        Port("TargetCount", "正式候选数量", "Number", false),
                        Port("HasSelection", "明确编号有本次选中目标", "Boolean", false),
                        Port("SelectedIndex", "正式选中排序编号（0起）", "Number", true),
                        Port("SelectedPoint", "明确选中像素中心", "Point2D", true),
                        Port("SelectedPose", "选中刚体位置/角度（尺度另列）", "Pose2D", true),
                        Port("SelectedScore", "明确选中真实分数", "Number", true),
                        Port("SelectedScale", "明确选中真实尺度（非缩放变换）", "Number", true),
                        Port("Score", "最高匹配分数", "Number", false),
                        Port("MatchCount", "匹配数量", "Number", false),
                        Port("Row", "中心 Row", "Number", false),
                        Port("Column", "中心 Column", "Number", false),
                        Port("Angle", "角度", "Number", false),
                        Port("Scale", "尺度", "Number", false),
                        Port("Candidates", "候选摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.NccTemplateMatch:
                    return new[]
                    {
                        Port("XLD", "候选模板框", "XLD", true),
                        Port("Point", "最佳目标参考点", "Point2D", true),
                        Port("Pose", "最佳目标位姿", "Pose2D", true),
                        Port("MatchCount", "匹配数量", "Number", false),
                        Port("HasMatch", "本次找到目标", "Boolean", false),
                        Port("Centers", "同序候选像素参考点", "Point2D[]", false),
                        Port("Scores", "同序真实候选分数", "Number[]", false),
                        Port("Angles", "同序真实候选角度（°）", "Number[]", false),
                        Port("Scales", "同序候选尺度（NCC 为 1）", "Number[]", false),
                        Port("SourceIndices", "本次算法候选原编号（非跟踪ID）", "Number[]", false),
                        Port("TargetSortMode", "本次正式候选排序规则", "String", false),
                        Port("TargetStatus", "本次多目标结果状态", "String", false),
                        Port("TargetDefinitionKey", "本次模板参数/订阅定义身份", "String", false),
                        Port("TargetModelVersion", "本次实际模板模型版本", "String", false),
                        Port("TargetInputVersion", "本次正式输入图像版本身份", "String", false),
                        Port("TargetCount", "正式候选数量", "Number", false),
                        Port("HasSelection", "明确编号有本次选中目标", "Boolean", false),
                        Port("SelectedIndex", "正式选中排序编号（0起）", "Number", true),
                        Port("SelectedPoint", "明确选中像素参考点", "Point2D", true),
                        Port("SelectedPose", "选中刚体位置/角度（尺度另列）", "Pose2D", true),
                        Port("SelectedScore", "明确选中真实分数", "Number", true),
                        Port("SelectedScale", "明确选中尺度（NCC 为 1）", "Number", true),
                        Port("Score", "最高匹配分数", "Number", false),
                        Port("CenterRow", "匹配参考点 Row", "Number", true),
                        Port("CenterColumn", "匹配参考点 Column", "Number", true),
                        Port("AngleDegrees", "匹配旋转角度（°）", "Number", true),
                        Port("ReferenceRow", "训练参考点 Row", "Number", false),
                        Port("ReferenceColumn", "训练参考点 Column", "Number", false),
                        Port("ReferenceAngleDegrees", "训练参考角度（°）", "Number", false),
                        Port("TemplateWidth", "训练模板宽度（px）", "Number", false),
                        Port("TemplateHeight", "训练模板高度（px）", "Number", false),
                        Port("Candidates", "本轮候选详情", "String", false),
                        Port("ModelVersion", "独立模板版本", "String", false),
                        Port("Parameters", "搜索参数摘要", "String", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.Blob:
                    return new[]
                    {
                        Port("Area", "区域总面积", "Number", false),
                        Port("SelectedRegion", "Blob 选中区域", "Region", true),
                        Port("RoiResults", "逐 ROI 结果", "RoiResult[]", true),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.GrayStat:
                    return new[]
                    {
                        Port("Mean", "灰度均值", "Number", false),
                        Port("RoiResults", "逐 ROI 结果", "RoiResult[]", true),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.EdgeMeasure:
                    return new[]
                    {
                        Port("XLD", "边缘轮廓", "XLD", true),
                        Port("Length", "边缘总长度", "Number", false),
                        Port("RoiResults", "逐 ROI 结果", "RoiResult[]", true),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.HDevelop:
                    return new[]
                    {
                        Port("Score", "过程数值", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.NumericJudge:
                    return new[]
                    {
                        Port("Value", "输入数值", "Number", false),
                        Port("Passed", "判定通过", "Boolean", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                default:
                    return new VmPortDefinition[0];
            }
        }

        public static string NormalizeBranchValueType(string value)
        {
            return VmFlowValueContractS103.Normalize(value, false) ?? "Number";
        }

        public static IList<VmPortDefinition> GetInstanceInputPorts(VmToolInstance tool)
        {
            List<VmPortDefinition> ports = GetInputPorts(tool == null ? VmToolKind.ImageSource : tool.Kind).ToList();
            if (tool != null && tool.Kind == VmToolKind.ModbusTcpExchange &&
                (tool.Parameters == null || VmModbusFunctionS118.IsRead(tool.Parameters.ModbusFunctionCodeS118)))
                ports.RemoveAll(port => port.PortName == "WriteValue");
            if (tool != null && tool.Kind == VmToolKind.ModbusRtuExchange &&
                (tool.Parameters == null || VmModbusFunctionS118.IsRead(tool.Parameters.RtuFunctionCodeS119)))
                ports.RemoveAll(port => port.PortName == "WriteValue");
            if (tool != null && tool.Kind == VmToolKind.JsonWrite)
                ports.AddRange(VmStructuredDataS110.Fields(tool.Parameters ?? new VmToolParameterData()).Where(field => field != null)
                    .Select(field => Port(field.PortName, field.Name, field.DataType, false)));
            if (tool != null && VmArrayReorderS108.IsTool(tool.Kind))
            {
                string mode = VmArrayReorderS108.Normalize((tool.Parameters ?? new VmToolParameterData()).ArrayReorderMode);
                ports = ports.Where(port => port.PortName == "Array" || mode == VmArrayReorderS108.Concat && port.PortName == "OtherArray" ||
                    mode == VmArrayReorderS108.Reindex && (port.PortName == "Indices" || port.PortName == "SourceCount")).ToList();
                foreach (var port in ports) port.IsOptional = false;
            }
            if (tool != null && tool.Kind == VmToolKind.NumericExpression)
            {
                IList<string> variables = new List<string>();
                try { variables = VmNumericExpressionS106.Compile((tool.Parameters ?? new VmToolParameterData()).NumericExpressionText).Variables; }
                catch (InvalidOperationException) { /* Parameter diagnostics report incomplete formulas. */ }
                foreach (var port in ports)
                {
                    port.IsOptional = !variables.Contains(port.PortName);
                    port.DisplayName = "变量 " + port.PortName + (port.IsOptional ? "（公式未使用）" : "（公式需要）");
                }
            }
            if (tool != null && tool.Kind == VmToolKind.SubflowCall && tool.Parameters != null && tool.Parameters.Subflow != null)
                ports.AddRange((tool.Parameters.Subflow.Inputs ?? new List<VmSubflowPortData>()).Select(port => Port(port.PortName, port.DisplayName, port.DataType, false)));
            if (tool != null && tool.Kind == VmToolKind.BranchValueSelection)
                foreach (var port in ports.Where(item => item.PortName == "WhenTrue" || item.PortName == "WhenFalse"))
                    port.DataType = NormalizeBranchValueType(tool.Parameters == null ? null : tool.Parameters.BranchValueType);
            if (tool != null && tool.Kind != VmToolKind.SubflowInput)
                ports.Add(Port("$RunCondition", tool.Parameters != null && tool.Parameters.ExecutionConditionWhenFalse ? "执行条件 · 假时运行" : "执行条件 · 真时运行", "Boolean", true));
            if (tool != null && SupportsRoi(tool.Kind) &&
                !ports.Any(item => string.Equals(item.PortName, "ROI", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(item.PortName, "SearchROI", StringComparison.OrdinalIgnoreCase)))
            {
                ports.Add(Port("ROI", "前序检测区域（可选）", "Region", true));
            }
            if (tool == null || tool.Kind != VmToolKind.CSharpDllScript || tool.Parameters == null) return ports;
            tool.Parameters.Normalize();
            foreach (VmScriptInterfaceMemberData member in tool.Parameters.ScriptInterfaceMembers
                .Where(item => item != null && string.Equals(item.Direction, VmScriptMemberDirection.Input, StringComparison.OrdinalIgnoreCase)))
            {
                ports.Add(Port(member.PortName, member.Name, VmScriptScalarType.ToPortDataType(member.ValueType), !member.IsRequired));
            }
            return ports;
        }

        public static IList<VmPortDefinition> GetInstanceOutputPorts(VmToolInstance tool)
        {
            List<VmPortDefinition> ports = GetOutputPorts(tool == null ? VmToolKind.ImageSource : tool.Kind).ToList();
            if (tool != null && tool.Kind == VmToolKind.ModbusTcpExchange && tool.Parameters != null &&
                VmModbusFunctionS118.IsBit(tool.Parameters.ModbusFunctionCodeS118))
                ports.Add(Port("IsOn", "首个位状态", "Boolean", false));
            if (tool != null && tool.Kind == VmToolKind.ModbusRtuExchange && tool.Parameters != null &&
                VmModbusFunctionS118.IsBit(tool.Parameters.RtuFunctionCodeS119))
                ports.Add(Port("IsOn", "首个位状态", "Boolean", false));
            if (tool != null && tool.Kind == VmToolKind.TcpClientExchange && tool.Parameters != null &&
                VmTcpResponseModeS117.Normalize(tool.Parameters.TcpResponseModeS117) != VmTcpResponseModeS117.None)
            {
                if (!string.IsNullOrWhiteSpace(tool.Parameters.TcpResponseStringSelectorS117))
                    ports.Add(Port("ResponseString", "映射响应文本", "String", false));
                if (!string.IsNullOrWhiteSpace(tool.Parameters.TcpResponseNumberSelectorS117))
                    ports.Add(Port("ResponseNumber", "映射响应数值", "Number", false));
                if (!string.IsNullOrWhiteSpace(tool.Parameters.TcpResponseBooleanSelectorS117))
                    ports.Add(Port("ResponseBoolean", "映射响应布尔值", "Boolean", false));
            }
            if (tool != null && tool.Kind == VmToolKind.JsonRead)
                ports.AddRange(VmStructuredDataS110.Fields(tool.Parameters ?? new VmToolParameterData()).Where(field => field != null)
                    .Select(field => Port(field.PortName, field.Name, field.DataType, false)));
            if (tool != null && (tool.Kind == VmToolKind.SubflowCall || tool.Kind == VmToolKind.SubflowInput) && tool.Parameters != null && tool.Parameters.Subflow != null)
                ports.AddRange(VmSubflowLoopS111.ReturnPorts(tool.Parameters.Subflow).Select(port => Port(port.PortName, port.DisplayName, port.DataType, false)));
            if (tool != null && tool.Kind == VmToolKind.BranchValueSelection)
                ports.First(item => item.PortName == "Value").DataType = NormalizeBranchValueType(tool.Parameters == null ? null : tool.Parameters.BranchValueType);
            if (tool == null || tool.Kind != VmToolKind.CSharpDllScript || tool.Parameters == null) return ports;
            tool.Parameters.Normalize();
            foreach (VmScriptInterfaceMemberData member in tool.Parameters.ScriptInterfaceMembers
                .Where(item => item != null && string.Equals(item.Direction, VmScriptMemberDirection.Output, StringComparison.OrdinalIgnoreCase)))
            {
                ports.Add(Port(member.PortName, member.Name, VmScriptScalarType.ToPortDataType(member.ValueType), !member.IsRequired));
            }
            return ports;
        }

        public static IList<VmPortDefinition> GetNumericOutputPorts(VmToolKind kind)
        {
            return GetOutputPorts(kind).Where(item => item.DataType == "Number").ToList();
        }

        public static IList<VmPortDefinition> GetInstanceNumericOutputPorts(VmToolInstance tool)
        {
            return GetInstanceOutputPorts(tool).Where(item => string.Equals(item.DataType, "Number", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public static bool SupportsRoi(VmToolKind kind)
        {
            return kind == VmToolKind.ShapeMatch ||
                   kind == VmToolKind.NccTemplateMatch ||
                   kind == VmToolKind.DataCode2DRecognition ||
                   kind == VmToolKind.BarCode1DRecognition ||
                   kind == VmToolKind.OcrRecognition ||
                   kind == VmToolKind.Blob ||
                   kind == VmToolKind.GrayStat ||
                   kind == VmToolKind.GrayHistogram ||
                   kind == VmToolKind.EdgeMeasure ||
                   kind == VmToolKind.SubpixelEdgeExtraction ||
                   kind == VmToolKind.AutomaticThreshold ||
                   kind == VmToolKind.LocalAdaptiveThreshold ||
                   kind == VmToolKind.ColorInspectionHsv ||
                   kind == VmToolKind.VariationSurfaceInspection ||
                   kind == VmToolKind.HDevelop ||
                   kind == VmToolKind.RoiImageExtraction;
        }

        public static bool RequiresRoi(VmToolKind kind)
        {
            return kind == VmToolKind.HDevelop;
        }

        private static VmPortDefinition Port(string name, string displayName, string dataType, bool optional)
        {
            return new VmPortDefinition
            {
                PortName = name,
                DisplayName = displayName,
                DataType = dataType,
                IsOptional = optional
            };
        }
    }
}
