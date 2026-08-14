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
        TextParser
    }

    public sealed class VmToolCatalogItem : INotifyPropertyChanged
    {
        private bool isFavorite;
        private int recentRank = int.MaxValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public VmToolKind Kind { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

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
            get { return (Category + " " + Name + " " + Description).ToLowerInvariant(); }
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
        private readonly List<string> boundRoiIds = new List<string>();
        private readonly List<VmToolInputBindingData> inputBindings = new List<VmToolInputBindingData>();
        private readonly Dictionary<string, object> runtimeOutputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly ObservableCollection<VmRoiRunResult> roiResults = new ObservableCollection<VmRoiRunResult>();
        private readonly ObservableCollection<VmFlowPortChip> flowInputPorts = new ObservableCollection<VmFlowPortChip>();
        private readonly ObservableCollection<VmFlowPortChip> flowOutputPorts = new ObservableCollection<VmFlowPortChip>();

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
                if (isEnabled == value)
                {
                    return;
                }

                isEnabled = value;
                OnPropertyChanged("IsEnabled");
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
            }
        }

        public string ResultCode
        {
            get { return resultCode; }
            set
            {
                resultCode = value;
                OnPropertyChanged("ResultCode");
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
            foreach (IDisposable disposable in runtimeOutputs.Values.OfType<IDisposable>().Distinct().ToList())
            {
                disposable.Dispose();
            }
            runtimeOutputs.Clear();
            roiResults.Clear();
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
        }

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
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return "本地图像源";
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
                case VmToolKind.TextAssembly:
                    return "文本组装";
                case VmToolKind.TextParser:
                    return "文本解析";
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
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return "采集/输入";
                case VmToolKind.ShapeMatch:
                    return "定位";
                case VmToolKind.Blob:
                case VmToolKind.GrayStat:
                case VmToolKind.ImageChannel:
                case VmToolKind.ImageFilter:
                case VmToolKind.ImageEnhancement:
                case VmToolKind.ImageResize:
                case VmToolKind.ImageMirror:
                case VmToolKind.ImageRotate:
                case VmToolKind.ImageAffineTransform:
                case VmToolKind.ImageProjectiveTransform:
                case VmToolKind.SubpixelEdgeExtraction:
                case VmToolKind.PolarImageUnwrap:
                case VmToolKind.RoiImageExtraction:
                case VmToolKind.ImageDifference:
                case VmToolKind.DynamicThreshold:
                case VmToolKind.ImageThreshold:
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionSetOperation:
                    return "图像处理";
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
                case VmToolKind.XldEnclosingCircleDiameterMeasurement:
                case VmToolKind.XldMinimumRectangleDirectionMeasurement:
                case VmToolKind.XldDirectionConsistencyMeasurement:
                case VmToolKind.XldContourSegmentation:
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
                    return "脚本";
                case VmToolKind.NumericJudge:
                case VmToolKind.TextAssembly:
                case VmToolKind.TextParser:
                    return "逻辑/计算";
                case VmToolKind.TcpClientExchange:
                    return "通讯";
                default:
                    return "其他";
            }
        }

        public static string GetDescription(VmToolKind kind)
        {
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return "读取本地图像并输出带来源、SN 和尺寸的安全 Image 快照。";
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
                case VmToolKind.TextAssembly:
                    return "订阅可选 String、Number、ResultCode，按模板、分隔符、数值格式和空值策略生成可继续订阅的工业文本。";
                case VmToolKind.TextParser:
                    return "订阅 String，按字面分隔或正则匹配提取指定字段，可选执行 InvariantCulture 数值转换，并输出可继续订阅的文本、数值和匹配状态。";
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
            switch (kind)
            {
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
                case VmToolKind.CaliperEdgePairMeasurement:
                case VmToolKind.LinearCaliperArrayMeasurement:
                case VmToolKind.CircularCaliperCircleFinder:
                case VmToolKind.RingWallThicknessMeasurement:
                    return new[] { Port("Image", "输入图像", "Image", false) };
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
                        Port("SearchROI", "搜索 ROI", "Region", false),
                        Port("ShapeModel", "形状模板", "ShapeModel", false)
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
                case VmToolKind.TextParser:
                    return new[] { Port("String", "待解析文本", "String", false) };
                default:
                    return new VmPortDefinition[0];
            }
        }

        public static IList<VmPortDefinition> GetOutputPorts(VmToolKind kind)
        {
            switch (kind)
            {
                case VmToolKind.ImageSource:
                    return new[]
                    {
                        Port("Image", "本地图像", "Image", false),
                        Port("SN", "图像序列号", "String", false),
                        Port("Path", "图像路径", "String", false),
                        Port("Width", "图像宽度", "Number", false),
                        Port("Height", "图像高度", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.TcpClientExchange:
                    return new[]
                    {
                        Port("Connected", "已连接", "Boolean", false),
                        Port("SentText", "发送文本", "String", false),
                        Port("ReceivedText", "接收文本", "String", false),
                        Port("RoundTripMs", "往返耗时", "Number", false),
                        Port("BytesSent", "发送字节数", "Number", false),
                        Port("BytesReceived", "接收字节数", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
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
                        Port("Score", "最高匹配分数", "Number", false),
                        Port("MatchCount", "匹配数量", "Number", false),
                        Port("Row", "中心 Row", "Number", false),
                        Port("Column", "中心 Column", "Number", false),
                        Port("Angle", "角度", "Number", false),
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

        public static IList<VmPortDefinition> GetNumericOutputPorts(VmToolKind kind)
        {
            return GetOutputPorts(kind).Where(item => item.DataType == "Number").ToList();
        }

        public static bool SupportsRoi(VmToolKind kind)
        {
            return kind == VmToolKind.ShapeMatch ||
                   kind == VmToolKind.Blob ||
                   kind == VmToolKind.GrayStat ||
                   kind == VmToolKind.EdgeMeasure ||
                   kind == VmToolKind.SubpixelEdgeExtraction ||
                   kind == VmToolKind.HDevelop ||
                   kind == VmToolKind.RoiImageExtraction;
        }

        public static bool RequiresRoi(VmToolKind kind)
        {
            return kind == VmToolKind.ShapeMatch || kind == VmToolKind.HDevelop;
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
