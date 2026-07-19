using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmFlowPortChip
    {
        public string Direction { get; set; }

        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public string EndpointText { get; set; }

        public string ValueText { get; set; }

        public string StatusText { get; set; }

        public string StateKey { get; set; }

        public bool IsConnected { get; set; }

        public string PortText
        {
            get { return PortName + " : " + DataType; }
        }
    }

    public sealed class VmPortDefinition
    {
        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public bool IsOptional { get; set; }
    }

    public sealed class VmPortDisplayItem
    {
        public string Direction { get; set; }

        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public string Source { get; set; }

        public string CurrentValue { get; set; }

        public string Status { get; set; }

        public bool IsConnected { get; set; }
    }

    public sealed class VmSourceToolOption
    {
        public string ToolId { get; set; }

        public string DisplayText { get; set; }
    }

    public sealed class VmSourcePortOption
    {
        public string PortName { get; set; }

        public string DisplayText { get; set; }

        public string DataType { get; set; }
    }

    [DataContract]
    public sealed class VmToolInputBindingData
    {
        [DataMember(Order = 1)] public string TargetPortName { get; set; }

        [DataMember(Order = 2)] public string SourceToolId { get; set; }

        [DataMember(Order = 3)] public string SourcePortName { get; set; }

        public VmToolInputBindingData Clone()
        {
            return (VmToolInputBindingData)MemberwiseClone();
        }
    }

    public sealed class VmInputSourceOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }

        public string SourceToolId { get; set; }

        public string SourcePortName { get; set; }

        public string DataType { get; set; }

        public bool IsDefault { get; set; }

        public bool IsValid { get; set; }
    }

    public sealed class VmInputPortEditorRow : INotifyPropertyChanged
    {
        private string selectedSourceKey;

        public event PropertyChangedEventHandler PropertyChanged;

        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public bool IsOptional { get; set; }

        public IList<VmInputSourceOption> SourceOptions { get; set; }

        public string CurrentValue { get; set; }

        public string StatusText { get; set; }

        public string OptionalText
        {
            get { return IsOptional ? "可选" : "必需"; }
        }

        public string SelectedSourceKey
        {
            get { return selectedSourceKey; }
            set
            {
                if (selectedSourceKey == value)
                {
                    return;
                }

                selectedSourceKey = value;
                OnPropertyChanged("SelectedSourceKey");
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

    public sealed class VmRegionSnapshot : IDisposable
    {
        private HObject region;

        private VmRegionSnapshot()
        {
        }

        public string ProducerToolId { get; private set; }

        public string ProducerToolName { get; private set; }

        public string ProducerPortName { get; private set; }

        public int ObjectCount { get; private set; }

        public double TotalArea { get; private set; }

        public bool IsDisposed
        {
            get { return region == null; }
        }

        public string DisplayText
        {
            get
            {
                return IsDisposed
                    ? "Region 已释放"
                    : "Region ×" + ObjectCount + "，Area=" + TotalArea.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static VmRegionSnapshot Create(HObject source, string producerToolId, string producerToolName, string producerPortName)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            HObject copy;
            HOperatorSet.CopyObj(source, out copy, 1, -1);
            HTuple count;
            HTuple area;
            HTuple row;
            HTuple column;
            HOperatorSet.CountObj(copy, out count);
            HOperatorSet.AreaCenter(copy, out area, out row, out column);
            double totalArea = 0;
            if (area != null)
            {
                for (int index = 0; index < area.Length; index++)
                {
                    totalArea += area[index].D;
                }
            }

            return new VmRegionSnapshot
            {
                region = copy,
                ProducerToolId = producerToolId,
                ProducerToolName = producerToolName,
                ProducerPortName = producerPortName,
                ObjectCount = count == null || count.Length == 0 ? 0 : count.I,
                TotalArea = totalArea
            };
        }

        public HRegion CreateRegionCopy()
        {
            if (region == null)
            {
                throw new ObjectDisposedException("VmRegionSnapshot", "上游 Region 快照已释放，请重新运行上游工具。");
            }

            HObject copy = null;
            try
            {
                HOperatorSet.CopyObj(region, out copy, 1, -1);
                HRegion result = new HRegion(copy);
                copy = null;
                return result;
            }
            finally
            {
                if (copy != null)
                {
                    copy.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (region != null)
            {
                region.Dispose();
                region = null;
            }
        }
    }

    public sealed class VmImageSnapshot : IDisposable
    {
        private HImage image;

        private VmImageSnapshot()
        {
        }

        public string ProducerToolId { get; private set; }

        public string ProducerToolName { get; private set; }

        public string ProducerPortName { get; private set; }

        public string SourcePath { get; private set; }

        public string SerialNumber { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool IsDisposed
        {
            get { return image == null; }
        }

        public string SourceName
        {
            get { return string.IsNullOrWhiteSpace(SourcePath) ? "内存图像" : Path.GetFileName(SourcePath); }
        }

        public string DisplayText
        {
            get
            {
                return IsDisposed
                    ? "Image 已释放"
                    : string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}×{1} · {2} · SN={3}",
                        Width,
                        Height,
                        SourceName,
                        string.IsNullOrWhiteSpace(SerialNumber) ? "--" : SerialNumber);
            }
        }

        public static VmImageSnapshot Create(
            HImage source,
            string producerToolId,
            string producerToolName,
            string producerPortName,
            string sourcePath,
            string serialNumber)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            HTuple width;
            HTuple height;
            HOperatorSet.GetImageSize(source, out width, out height);
            return new VmImageSnapshot
            {
                image = source.CopyImage(),
                ProducerToolId = producerToolId,
                ProducerToolName = producerToolName,
                ProducerPortName = producerPortName,
                SourcePath = sourcePath,
                SerialNumber = serialNumber,
                Width = width.I,
                Height = height.I
            };
        }

        public HImage CreateImageCopy()
        {
            if (image == null)
            {
                throw new ObjectDisposedException("VmImageSnapshot", "上游 Image 快照已释放，请重新运行上游工具。");
            }

            return image.CopyImage();
        }

        public string GetPixelDisplay(int row, int column)
        {
            if (image == null)
            {
                return "已释放";
            }

            if (row < 0 || column < 0 || row >= Height || column >= Width)
            {
                return "越界";
            }

            HTuple value;
            HOperatorSet.GetGrayval(image, row, column, out value);
            return FormatPixelTuple(value);
        }

        public void Dispose()
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
        }

        internal static string FormatPixelTuple(HTuple value)
        {
            if (value == null || value.Length == 0)
            {
                return "--";
            }

            List<string> channels = new List<string>();
            for (int index = 0; index < value.Length; index++)
            {
                channels.Add(value[index].D.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            }

            return string.Join("/", channels);
        }
    }

    public sealed class VmImageContextOption
    {
        public const string GlobalInput = "GlobalInput";
        public const string ModuleInput = "ModuleInput";
        public const string ModuleOutput = "ModuleOutput";

        public string Key { get; set; }

        public string DisplayText { get; set; }

        public static IList<VmImageContextOption> CreateAll()
        {
            return new List<VmImageContextOption>
            {
                new VmImageContextOption { Key = GlobalInput, DisplayText = "全局输入" },
                new VmImageContextOption { Key = ModuleInput, DisplayText = "当前模块输入" },
                new VmImageContextOption { Key = ModuleOutput, DisplayText = "当前模块输出" }
            };
        }
    }

    public sealed class NumericJudgeOperatorOption
    {
        public const string BetweenInclusive = "BetweenInclusive";
        public const string OutsideInclusive = "OutsideInclusive";
        public const string GreaterOrEqual = "GreaterOrEqual";
        public const string LessOrEqual = "LessOrEqual";
        public const string Equal = "Equal";
        public const string NotEqual = "NotEqual";

        public string Key { get; set; }

        public string DisplayText { get; set; }

        public static IList<NumericJudgeOperatorOption> CreateAll()
        {
            return new List<NumericJudgeOperatorOption>
            {
                new NumericJudgeOperatorOption { Key = BetweenInclusive, DisplayText = "区间内（含边界）" },
                new NumericJudgeOperatorOption { Key = OutsideInclusive, DisplayText = "区间外（含边界）" },
                new NumericJudgeOperatorOption { Key = GreaterOrEqual, DisplayText = "大于等于下限" },
                new NumericJudgeOperatorOption { Key = LessOrEqual, DisplayText = "小于等于上限" },
                new NumericJudgeOperatorOption { Key = Equal, DisplayText = "等于目标值（下限）" },
                new NumericJudgeOperatorOption { Key = NotEqual, DisplayText = "不等于目标值（下限）" }
            };
        }

        public static bool IsSupported(string key)
        {
            return key == BetweenInclusive ||
                   key == OutsideInclusive ||
                   key == GreaterOrEqual ||
                   key == LessOrEqual ||
                   key == Equal ||
                   key == NotEqual;
        }

        public static string GetDisplayText(string key)
        {
            foreach (NumericJudgeOperatorOption item in CreateAll())
            {
                if (item.Key == key)
                {
                    return item.DisplayText;
                }
            }

            return key ?? string.Empty;
        }
    }
}
