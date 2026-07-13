using System;
using System.ComponentModel;

namespace HalconWinFormsDemo.Models
{
    public enum VmToolKind
    {
        ShapeMatch,
        Blob,
        GrayStat,
        EdgeMeasure,
        HDevelop
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
                default:
                    return string.Empty;
            }
        }
    }
}
