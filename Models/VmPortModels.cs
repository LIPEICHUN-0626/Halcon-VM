using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmResultOverlayLayer : INotifyPropertyChanged
    {
        private bool isVisible;
        private string displayName;
        private int lineWidth;
        private int opacity;
        private bool isLocked;
        private string viewportScope;
        private int layerOrder;

        public event PropertyChangedEventHandler PropertyChanged;

        public string LayerKey { get; set; }

        public string ToolId { get; set; }

        public string ToolName { get; set; }

        public string PortName { get; set; }

        public string DataType { get; set; }

        public int Sequence { get; set; }

        public string ColorName { get; set; }

        public string HalconColor { get; set; }

        public string ColorHex { get; set; }

        public string StateKey { get; set; }

        public string StateText { get; set; }

        public string ResultCode { get; set; }

        public int ObjectCount { get; set; }

        public string MetricText { get; set; }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(displayName) ? SourceText : displayName; }
            set
            {
                string resolved = string.IsNullOrWhiteSpace(value) ? SourceText : value.Trim();
                if (displayName == resolved) return;
                displayName = resolved;
                OnPropertyChanged("DisplayName");
            }
        }

        public int LineWidth
        {
            get { return lineWidth <= 0 ? 2 : lineWidth; }
            set
            {
                int resolved = Math.Max(1, Math.Min(8, value));
                if (lineWidth == resolved) return;
                lineWidth = resolved;
                OnPropertyChanged("LineWidth");
            }
        }

        public int Opacity
        {
            get { return opacity <= 0 ? 100 : opacity; }
            set
            {
                int resolved = Math.Max(10, Math.Min(100, value));
                if (opacity == resolved) return;
                opacity = resolved;
                OnPropertyChanged("Opacity");
                OnPropertyChanged("OpacityText");
            }
        }

        public string OpacityText { get { return Opacity + "%"; } }

        public bool IsLocked
        {
            get { return isLocked; }
            set
            {
                if (isLocked == value) return;
                isLocked = value;
                OnPropertyChanged("IsLocked");
                OnPropertyChanged("LockText");
            }
        }

        public string LockText { get { return IsLocked ? "已锁定" : "可编辑"; } }

        public string ViewportScope
        {
            get { return VmLayerViewportScope.Normalize(viewportScope, VmLayerViewportScope.Right); }
            set
            {
                string resolved = VmLayerViewportScope.Normalize(value, VmLayerViewportScope.Right);
                if (string.Equals(viewportScope, resolved, StringComparison.OrdinalIgnoreCase)) return;
                viewportScope = resolved;
                OnPropertyChanged("ViewportScope");
                OnPropertyChanged("ViewportScopeText");
            }
        }

        public string ViewportScopeText { get { return VmLayerViewportScope.GetDisplayText(ViewportScope); } }

        public int LayerOrder
        {
            get { return layerOrder; }
            set
            {
                if (layerOrder == value) return;
                layerOrder = value;
                OnPropertyChanged("LayerOrder");
            }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value)
                {
                    return;
                }

                isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }

        public string SequenceText
        {
            get { return Sequence.ToString("00", System.Globalization.CultureInfo.InvariantCulture); }
        }

        public string SourceText
        {
            get { return ToolName + "." + PortName; }
        }

        public string TypeText
        {
            get { return DataType + " · " + ColorName; }
        }

        public string ObjectText
        {
            get { return "对象 " + ObjectCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(MetricText) ? string.Empty : " · " + MetricText); }
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

    public sealed class VmUnifiedLayerItem
    {
        public string LayerKey { get; set; }
        public string LayerKind { get; set; }
        public string DisplayName { get; set; }
        public string SourceText { get; set; }
        public string TypeText { get; set; }
        public string StateText { get; set; }
        public string CoordinateContextText { get; set; }
        public string ColorHex { get; set; }
        public int LayerOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }
        public bool IsCompatibilityLayer { get; set; }
        public VmRoiLayer RoiLayer { get; set; }
        public VmResultOverlayLayer ResultLayer { get; set; }
    }

    public sealed class VmLayerOption
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
    }

    public static class VmLayerViewportScope
    {
        public const string Both = "Both";
        public const string Left = "Left";
        public const string Right = "Right";

        public static string Normalize(string value, string fallback)
        {
            if (string.Equals(value, Both, StringComparison.OrdinalIgnoreCase)) return Both;
            if (string.Equals(value, Left, StringComparison.OrdinalIgnoreCase)) return Left;
            if (string.Equals(value, Right, StringComparison.OrdinalIgnoreCase)) return Right;
            return string.IsNullOrWhiteSpace(fallback) ? Both : fallback;
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value, Both);
            if (normalized == Left) return "左视口";
            if (normalized == Right) return "右视口";
            return "左右视口";
        }

        public static VmLayerOption[] CreateAll()
        {
            return new[]
            {
                new VmLayerOption { Key = Both, DisplayText = "左右视口" },
                new VmLayerOption { Key = Left, DisplayText = "仅左视口" },
                new VmLayerOption { Key = Right, DisplayText = "仅右视口" }
            };
        }

        public static bool Includes(string value, bool isLeftViewport)
        {
            string normalized = Normalize(value, Both);
            return normalized == Both || (isLeftViewport ? normalized == Left : normalized == Right);
        }
    }

    public static class VmOverlayPalette
    {
        private static readonly Dictionary<string, string> Hex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cyan", "#22D3EE" }, { "green", "#34D399" }, { "yellow", "#FACC15" },
            { "orange", "#FB923C" }, { "red", "#F87171" }, { "magenta", "#F472B6" },
            { "blue", "#60A5FA" }, { "white", "#F8FAFC" }, { "gray", "#94A3B8" }
        };

        public static string Normalize(string value, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
            return Hex.ContainsKey(normalized) ? normalized : fallback;
        }

        public static string GetHex(string value)
        {
            string normalized = Normalize(value, "cyan");
            return Hex[normalized];
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value, "cyan");
            if (normalized == "cyan") return "青色";
            if (normalized == "green") return "绿色";
            if (normalized == "yellow") return "黄色";
            if (normalized == "orange") return "橙色";
            if (normalized == "red") return "红色";
            if (normalized == "magenta") return "品红";
            if (normalized == "blue") return "蓝色";
            if (normalized == "white") return "白色";
            return "灰色";
        }

        public static VmLayerOption[] CreateAll()
        {
            return new[]
            {
                new VmLayerOption { Key = "cyan", DisplayText = "青色" },
                new VmLayerOption { Key = "green", DisplayText = "绿色" },
                new VmLayerOption { Key = "yellow", DisplayText = "黄色" },
                new VmLayerOption { Key = "orange", DisplayText = "橙色" },
                new VmLayerOption { Key = "red", DisplayText = "红色" },
                new VmLayerOption { Key = "magenta", DisplayText = "品红" },
                new VmLayerOption { Key = "blue", DisplayText = "蓝色" },
                new VmLayerOption { Key = "white", DisplayText = "白色" },
                new VmLayerOption { Key = "gray", DisplayText = "灰色" }
            };
        }
    }

    public sealed class VmFlowGroupVisual
    {
        public string GroupId { get; set; }

        public string GroupName { get; set; }

        public bool IsCollapsed { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string StateText { get; set; }

        public string MemberSummary { get; set; }

        public string BoundarySummary { get; set; }

        public string ExecutionSummary { get; set; }

        public string ErrorSummary { get; set; }

        public string DebugSummary { get; set; }

        public string ToggleText
        {
            get { return IsCollapsed ? "展开" : "折叠"; }
        }
    }

    public sealed class VmFlowPortChip
    {
        public string OwnerToolId { get; set; }

        public string OwnerToolName { get; set; }

        public string Direction { get; set; }

        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public string EndpointText { get; set; }

        public string ValueText { get; set; }

        public string StatusText { get; set; }

        public string StateKey { get; set; }

        public bool IsConnected { get; set; }

        public bool IsConnectionEndpointSelected { get; set; }

        public bool IsPortSelected { get; set; }

        public string PortText
        {
            get { return PortName + " : " + DataType; }
        }
    }

    public sealed class VmFlowPortConsumerOption
    {
        public string ConnectionKey { get; set; }

        public string TargetToolId { get; set; }

        public string TargetPortName { get; set; }

        public string DisplayText { get; set; }

        public string StatusText { get; set; }
    }

    public sealed class VmFlowConnectionVisual
    {
        public string ConnectionKey { get; set; }

        public string SourceToolId { get; set; }

        public string TargetToolId { get; set; }

        public string SourcePortName { get; set; }

        public string TargetPortName { get; set; }

        public string DataType { get; set; }

        public string SourceToolName { get; set; }

        public string TargetToolName { get; set; }

        public string SourceEndpointText { get; set; }

        public string TargetEndpointText { get; set; }

        public string CurrentValueText { get; set; }

        public string ErrorReason { get; set; }

        public string StateKey { get; set; }

        public string StateText { get; set; }

        public bool IsSelected { get; set; }

        public bool IsFocusPath { get; set; }

        public bool IsVisible { get; set; }

        public string Label { get; set; }

        public string FullLabel { get; set; }

        public bool IsLabelVisible { get; set; }

        public string StatusText { get; set; }

        public Geometry Geometry { get; set; }

        public Brush Stroke { get; set; }

        public Brush LabelBrush { get; set; }

        public double StrokeThickness { get; set; }

        public double StrokeOpacity { get; set; }

        public DoubleCollection StrokeDashArray { get; set; }

        public double LabelX { get; set; }

        public double LabelY { get; set; }
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

        public string StateKey { get; set; }

        public string SourceText { get; set; }

        public string CoordinateContextText { get; set; }

        public string ConsumerText { get; set; }

        public string DiagnosticText { get; set; }

        public bool IsReady { get; set; }

        public bool IsConfigured { get; set; }

        public string OptionalText
        {
            get { return IsOptional ? "可选" : "必需"; }
        }

        public string RequirementText
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

    public sealed class VmTaskResultSummaryRow
    {
        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public string ValueText { get; set; }

        public string StateKey { get; set; }

        public string DetailText { get; set; }
    }

    public sealed class VmTrialStabilityMetricOption
    {
        public string PortName { get; set; }

        public string DisplayText { get; set; }
    }

    public sealed class VmTrialStabilitySample
    {
        public int Cycle { get; set; }

        public DateTime Timestamp { get; set; }

        public string ResultCode { get; set; }

        public double ElapsedMilliseconds { get; set; }

        public string MetricPortName { get; set; }

        public double? MetricValue { get; set; }

        public string ErrorMessage { get; set; }

        public string CycleText
        {
            get { return "#" + Cycle.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        }

        public string ElapsedText
        {
            get { return ElapsedMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms"; }
        }

        public string MetricText
        {
            get { return MetricValue.HasValue ? MetricValue.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "--"; }
        }

        public string TimeText
        {
            get { return Timestamp.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture); }
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

        internal HRegion CreateObjectAnalysisCopyS144()
        {
            if (region == null) throw new ObjectDisposedException("VmRegionSnapshot", "上游Region已释放。");
            Services.ConnectedObjectServiceS144.ValidateBudget(region);
            return CreateRegionCopy();
        }

        internal HObject CreateObjectAnalysisItemS144(int index)
        {
            if (region == null) throw new ObjectDisposedException("VmRegionSnapshot", "对象分析Region已释放。");
            Services.ConnectedObjectServiceS144.ValidateBudget(region, Services.ConnectedObjectServiceS144.MaxConnectedObjects);
            if (index < 0 || index >= ObjectCount) throw new InvalidOperationException("查看编号已失效。");
            HObject selected; HOperatorSet.SelectObj(region, out selected, index + 1); return selected;
        }

        internal static VmRegionSnapshot CreateObjectAnalysisSnapshotS144(HObject source, string toolId, string toolName, string portName)
        {
            Services.ConnectedObjectServiceS144.ValidateBudget(source, Services.ConnectedObjectServiceS144.MaxConnectedObjects);
            HObject copy = null;
            HTuple count = null, areas = null, rows = null, columns = null;
            try
            {
                HOperatorSet.CountObj(source, out count);
                double total = 0;
                if (count.I > 0)
                {
                    HOperatorSet.AreaCenter(source, out areas, out rows, out columns);
                    for (int i = 0; i < areas.Length; i++) total += areas[i].D;
                }
                HOperatorSet.CopyObj(source, out copy, 1, -1);
                var result = new VmRegionSnapshot { region = copy, ProducerToolId = toolId, ProducerToolName = toolName,
                    ProducerPortName = portName, ObjectCount = count.I, TotalArea = total };
                copy = null; return result;
            }
            finally
            {
                if (copy != null) copy.Dispose();
                foreach (var tuple in new[] { count, areas, rows, columns }) if (tuple != null) tuple.Dispose();
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

        public int ChannelCount { get; private set; }

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

            HTuple width = null, height = null, channels = null;
            try
            {
                HOperatorSet.GetImageSize(source, out width, out height);
                HOperatorSet.CountChannels(source, out channels);
                return new VmImageSnapshot
                {
                    image = source.CopyImage(),
                    ProducerToolId = producerToolId,
                    ProducerToolName = producerToolName,
                    ProducerPortName = producerPortName,
                    SourcePath = sourcePath,
                    SerialNumber = serialNumber,
                    Width = width.I,
                    Height = height.I,
                    ChannelCount = channels.I
                };
            }
            finally
            {
                if (width != null) width.Dispose();
                if (height != null) height.Dispose();
                if (channels != null) channels.Dispose();
            }
        }

        public HImage CreateImageCopy()
        {
            if (image == null)
            {
                throw new ObjectDisposedException("VmImageSnapshot", "上游 Image 快照已释放，请重新运行上游工具。");
            }

            return image.CopyImage();
        }

        // Production evidence owns a separate raw frame. Inspect the original before allocating
        // that copy; unsupported pixels/domains are missing evidence, never display-scaled PNG.
        internal HImage CreateEvidenceCopyS142(long remainingBytes, out int width, out int height, out int channels)
        {
            if (image == null) throw new ObjectDisposedException("VmImageSnapshot", "本次源图已释放。");
            HTuple count = null, w = null, h = null, channelCount = null, type = null;
            HTuple area = null, centerRow = null, centerColumn = null;
            HTuple row1 = null, column1 = null, row2 = null, column2 = null;
            HObject domain = null;
            try
            {
                HOperatorSet.CountObj(image, out count);
                if (count.Length != 1 || count.I != 1) throw new InvalidOperationException("留图只支持单幅源图，不猜测图像数组。");
                HOperatorSet.GetImageSize(image, out w, out h);
                width = w.I; height = h.I;
                long pixels = (long)width * height;
                if (width < 1 || height < 1 || width > 16384 || height > 16384 || pixels > 16000000)
                    throw new InvalidOperationException("源图超过单幅 1600 万像素 / 边长 16384 的留图预算。");
                HOperatorSet.CountChannels(image, out channelCount);
                channels = channelCount.I;
                if (channels != 1 && channels != 3) throw new InvalidOperationException("留图首版仅支持 byte 单通道或三通道原图。");
                HOperatorSet.GetImageType(image, out type);
                if (type.Length != 1 || type.S != "byte") throw new InvalidOperationException("源图不是 byte；不转成显示灰度冒充原值证据。");
                if (pixels * channels > remainingBytes)
                    throw new InvalidOperationException("本周期源图副本超过 64 MiB 内存预算。");
                HOperatorSet.GetDomain(image, out domain);
                HOperatorSet.AreaCenter(domain, out area, out centerRow, out centerColumn);
                HOperatorSet.SmallestRectangle1(domain, out row1, out column1, out row2, out column2);
                if (area.Length != 1 || area.D != pixels || row1.D != 0 || column1.D != 0 ||
                    row2.D != height - 1 || column2.D != width - 1)
                    throw new InvalidOperationException("源图不是完整矩形有效域；不丢失 domain 语义保存成普通 PNG。");
                return image.CopyImage();
            }
            finally
            {
                if (domain != null) domain.Dispose();
                foreach (HTuple tuple in new[] { count, w, h, channelCount, type, area, centerRow, centerColumn, row1, column1, row2, column2 })
                    if (tuple != null) tuple.Dispose();
            }
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

    public sealed class VmXldSnapshot : IDisposable
    {
        private HObject xld;

        private VmXldSnapshot()
        {
        }

        public string ProducerToolId { get; private set; }

        public string ProducerToolName { get; private set; }

        public string ProducerPortName { get; private set; }

        public int ObjectCount { get; private set; }

        public double TotalLength { get; private set; }

        public bool IsDisposed
        {
            get { return xld == null; }
        }

        public string DisplayText
        {
            get
            {
                return IsDisposed
                    ? "XLD 已释放"
                    : "XLD ×" + ObjectCount + "，Length=" + TotalLength.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static VmXldSnapshot Create(HObject source, string producerToolId, string producerToolName, string producerPortName)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            HObject copy = null;
            try
            {
                HOperatorSet.CopyObj(source, out copy, 1, -1);
                HTuple count;
                HOperatorSet.CountObj(copy, out count);
                int objectCount = count == null || count.Length == 0 ? 0 : count.I;
                double totalLength = 0;
                if (objectCount > 0)
                {
                    HTuple lengths;
                    HOperatorSet.LengthXld(copy, out lengths);
                    if (lengths != null)
                    {
                        for (int index = 0; index < lengths.Length; index++)
                        {
                            totalLength += lengths[index].D;
                        }
                    }
                }

                VmXldSnapshot snapshot = new VmXldSnapshot
                {
                    xld = copy,
                    ProducerToolId = producerToolId,
                    ProducerToolName = producerToolName,
                    ProducerPortName = producerPortName,
                    ObjectCount = objectCount,
                    TotalLength = totalLength
                };
                copy = null;
                return snapshot;
            }
            finally
            {
                if (copy != null)
                {
                    copy.Dispose();
                }
            }
        }

        public HObject CreateXldCopy()
        {
            if (xld == null)
            {
                throw new ObjectDisposedException("VmXldSnapshot", "上游 XLD 快照已释放，请重新运行上游工具。");
            }

            HObject copy;
            HOperatorSet.CopyObj(xld, out copy, 1, -1);
            return copy;
        }

        public void Dispose()
        {
            if (xld != null)
            {
                xld.Dispose();
                xld = null;
            }
        }
    }

    public sealed class VmImageContextOption
    {
        public const string GlobalInput = "GlobalInput";
        public const string ModuleInput = "ModuleInput";
        public const string ModuleOutput = "ModuleOutput";
        public const string SourcePrefix = "Source:";

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
