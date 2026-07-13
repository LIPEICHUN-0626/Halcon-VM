using System;
using System.Collections.Generic;
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
        NumericJudge
    }

    public sealed class VmToolCatalogItem
    {
        public VmToolKind Kind { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public string SearchText
        {
            get { return (Category + " " + Name + " " + Description).ToLowerInvariant(); }
        }
    }

    public sealed class VmToolInstance : INotifyPropertyChanged
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
        private readonly List<string> boundRoiIds = new List<string>();
        private readonly Dictionary<string, object> runtimeOutputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ToolId { get; set; }

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

        public IList<string> BoundRoiIds
        {
            get { return boundRoiIds; }
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
            runtimeOutputs.Clear();
        }

        public void SetOutputValue(string portName, object value)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                return;
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

            return Convert.ToString(value, CultureInfo.InvariantCulture);
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
                case VmToolKind.ShapeMatch:
                    return "定位";
                case VmToolKind.Blob:
                case VmToolKind.GrayStat:
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
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.GrayStat:
                    return new[]
                    {
                        Port("Mean", "灰度均值", "Number", false),
                        Port("ResultCode", "结果码", "String", false)
                    };
                case VmToolKind.EdgeMeasure:
                    return new[]
                    {
                        Port("Length", "边缘总长度", "Number", false),
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
