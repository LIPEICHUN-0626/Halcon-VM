using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
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
        DynamicThreshold,
        ImageDifference,
        RegionContourMeasurement,
        XldContourFilter,
        XldContourSegmentation,
        XldLineFitting,
        XldCircleFitting,
        XldEllipseFitting,
        XldRectangleFitting
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
                case VmToolKind.ImageDifference:
                case VmToolKind.DynamicThreshold:
                case VmToolKind.ImageThreshold:
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionSetOperation:
                    return "图像处理";
                case VmToolKind.RegionContourMeasurement:
                case VmToolKind.XldContourFilter:
                case VmToolKind.XldContourSegmentation:
                case VmToolKind.XldLineFitting:
                case VmToolKind.XldCircleFitting:
                case VmToolKind.XldEllipseFitting:
                case VmToolKind.XldRectangleFitting:
                    return "测量";
                case VmToolKind.EdgeMeasure:
                    return "测量";
                case VmToolKind.HDevelop:
                    return "脚本";
                case VmToolKind.NumericJudge:
                    return "逻辑/计算";
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
                case VmToolKind.ImageThreshold:
                    return new[] { Port("Image", "输入图像", "Image", false) };
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
                case VmToolKind.XldContourSegmentation:
                case VmToolKind.XldLineFitting:
                case VmToolKind.XldCircleFitting:
                case VmToolKind.XldEllipseFitting:
                case VmToolKind.XldRectangleFitting:
                    return new[] { Port("XLD", "输入轮廓", "XLD", false) };
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
                   kind == VmToolKind.HDevelop;
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
