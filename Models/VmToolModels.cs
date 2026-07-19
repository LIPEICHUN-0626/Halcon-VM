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
        RegionSetOperation
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
                case VmToolKind.ImageThreshold:
                    return "阈值分割";
                case VmToolKind.RegionMorphology:
                    return "Region 形态学";
                case VmToolKind.RegionFeatureFilter:
                    return "Region 特征筛选";
                case VmToolKind.RegionSetOperation:
                    return "Region 集合运算";
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
                case VmToolKind.ImageThreshold:
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                case VmToolKind.RegionSetOperation:
                    return "图像处理";
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
                case VmToolKind.ImageThreshold:
                    return "将输入图像按明确的灰度上下限执行 HALCON 阈值分割，输出可订阅的安全 Region 快照。";
                case VmToolKind.RegionMorphology:
                    return "订阅上游 Region，使用 HALCON 圆形开闭、膨胀或腐蚀处理区域并输出可继续订阅的安全 Region 快照。";
                case VmToolKind.RegionFeatureFilter:
                    return "订阅上游 Region，使用 HALCON 按面积、宽度、高度或圆度筛选连通区域并输出安全 Region 快照。";
                case VmToolKind.RegionSetOperation:
                    return "订阅两路不同的上游 Region，使用 HALCON 执行并集、交集、差集或对称差，并输出可继续订阅的安全 Region 快照。";
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
                case VmToolKind.ImageThreshold:
                    return new[] { Port("Image", "输入图像", "Image", false) };
                case VmToolKind.RegionMorphology:
                case VmToolKind.RegionFeatureFilter:
                    return new[] { Port("Region", "输入区域", "Region", false) };
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
