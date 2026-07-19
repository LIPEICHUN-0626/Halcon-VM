using System;
using System.IO;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmToolParameterData
    {
        public VmToolParameterData()
        {
            BlobMinGray = 80;
            BlobMaxGray = 255;
            BlobMinArea = 50;
            GrayMin = 0;
            GrayMax = 255;
            EdgeThreshold = 30;
            RoiExecutionMode = VmRoiExecutionMode.Union;
            ImageChannelMode = VmImageChannelMode.Keep;
            ImageChannelIndex = 1;
            ImageFilterMode = VmImageFilterMode.Mean;
            ImageFilterMaskWidth = 3;
            ImageFilterMaskHeight = 3;
            ImageFilterRadius = 1;
            ImageThresholdMinGray = 128;
            ImageThresholdMaxGray = 255;
            RegionMorphologyMode = VmRegionMorphologyMode.OpeningCircle;
            RegionMorphologyRadius = 3.5;
            RegionFeature = VmRegionFeature.Area;
            RegionFeatureMin = 0;
            RegionFeatureMax = 999999999;
            RegionSetOperationMode = VmRegionSetOperationMode.Union;
        }

        [DataMember(Order = 1)] public double BlobMinGray { get; set; }
        [DataMember(Order = 2)] public double BlobMaxGray { get; set; }
        [DataMember(Order = 3)] public double BlobMinArea { get; set; }
        [DataMember(Order = 4)] public double GrayMin { get; set; }
        [DataMember(Order = 5)] public double GrayMax { get; set; }
        [DataMember(Order = 6)] public double EdgeThreshold { get; set; }
        [DataMember(Order = 7)] public string RoiExecutionMode { get; set; }
        [DataMember(Order = 8, EmitDefaultValue = false)] public string LocalImagePath { get; set; }
        [DataMember(Order = 9, EmitDefaultValue = false)] public string LocalImageSerialNumber { get; set; }
        [DataMember(Order = 10, EmitDefaultValue = false)] public string ImageChannelMode { get; set; }
        [DataMember(Order = 11, EmitDefaultValue = false)] public int ImageChannelIndex { get; set; }
        [DataMember(Order = 12, EmitDefaultValue = false)] public string ImageFilterMode { get; set; }
        [DataMember(Order = 13, EmitDefaultValue = false)] public int ImageFilterMaskWidth { get; set; }
        [DataMember(Order = 14, EmitDefaultValue = false)] public int ImageFilterMaskHeight { get; set; }
        [DataMember(Order = 15, EmitDefaultValue = false)] public int ImageFilterRadius { get; set; }
        [DataMember(Order = 16, EmitDefaultValue = false)] public double ImageThresholdMinGray { get; set; }
        [DataMember(Order = 17, EmitDefaultValue = false)] public double ImageThresholdMaxGray { get; set; }
        [DataMember(Order = 18, EmitDefaultValue = false)] public string RegionMorphologyMode { get; set; }
        [DataMember(Order = 19, EmitDefaultValue = false)] public double RegionMorphologyRadius { get; set; }
        [DataMember(Order = 20, EmitDefaultValue = false)] public string RegionFeature { get; set; }
        [DataMember(Order = 21, EmitDefaultValue = false)] public double RegionFeatureMin { get; set; }
        [DataMember(Order = 22, EmitDefaultValue = false)] public double RegionFeatureMax { get; set; }
        [DataMember(Order = 23, EmitDefaultValue = false)] public string RegionSetOperationMode { get; set; }

        public VmToolParameterData Clone()
        {
            VmToolParameterData clone = (VmToolParameterData)MemberwiseClone();
            clone.Normalize();
            return clone;
        }

        public void Normalize()
        {
            RoiExecutionMode = VmRoiExecutionMode.Normalize(RoiExecutionMode);
            ImageChannelMode = VmImageChannelMode.Normalize(ImageChannelMode);
            ImageFilterMode = VmImageFilterMode.Normalize(ImageFilterMode);
            RegionMorphologyMode = VmRegionMorphologyMode.Normalize(RegionMorphologyMode);
            RegionSetOperationMode = VmRegionSetOperationMode.Normalize(RegionSetOperationMode);
            if (string.IsNullOrWhiteSpace(RegionFeature))
            {
                RegionFeature = VmRegionFeature.Area;
                RegionFeatureMin = 0;
                RegionFeatureMax = 999999999;
            }
            else
            {
                RegionFeature = VmRegionFeature.Normalize(RegionFeature);
            }
            if (ImageChannelIndex <= 0)
            {
                ImageChannelIndex = 1;
            }
            if (ImageFilterMaskWidth == 0) ImageFilterMaskWidth = 3;
            if (ImageFilterMaskHeight == 0) ImageFilterMaskHeight = 3;
            if (ImageFilterRadius == 0) ImageFilterRadius = 1;
            if (RegionMorphologyRadius == 0) RegionMorphologyRadius = 3.5;
        }

        public string Validate(VmToolKind kind)
        {
            if (kind == VmToolKind.ImageSource)
            {
                if (string.IsNullOrWhiteSpace(LocalImagePath))
                {
                    return "请选择本地图像文件。";
                }

                if (!File.Exists(LocalImagePath))
                {
                    return "本地图像文件不存在：" + LocalImagePath;
                }
            }

            if (kind == VmToolKind.ImageChannel)
            {
                if (!VmImageChannelMode.IsSupported(ImageChannelMode))
                {
                    return "图像通道模式无效。";
                }

                if (VmImageChannelMode.Normalize(ImageChannelMode) == VmImageChannelMode.Extract && ImageChannelIndex < 1)
                {
                    return "提取通道序号必须从 1 开始。";
                }
            }

            if (kind == VmToolKind.ImageFilter)
            {
                if (!VmImageFilterMode.IsSupported(ImageFilterMode))
                {
                    return "图像滤波模式无效。";
                }

                string filterMode = VmImageFilterMode.Normalize(ImageFilterMode);
                if (filterMode == VmImageFilterMode.Mean &&
                    (!IsOddInRange(ImageFilterMaskWidth, 3, 255) || !IsOddInRange(ImageFilterMaskHeight, 3, 255)))
                {
                    return "均值滤波模板宽高必须是 3 到 255 之间的奇数。";
                }

                if (filterMode == VmImageFilterMode.Median && (ImageFilterRadius < 1 || ImageFilterRadius > 100))
                {
                    return "中值滤波半径必须在 1 到 100 之间。";
                }
            }

            if (kind == VmToolKind.ImageThreshold &&
                (!IsFinite(ImageThresholdMinGray) || !IsFinite(ImageThresholdMaxGray) ||
                 ImageThresholdMinGray < 0 || ImageThresholdMaxGray > 255 || ImageThresholdMinGray > ImageThresholdMaxGray))
            {
                return "阈值分割灰度范围必须满足 0 ≤ 下限 ≤ 上限 ≤ 255。";
            }

            if (kind == VmToolKind.RegionMorphology)
            {
                if (!VmRegionMorphologyMode.IsSupported(RegionMorphologyMode))
                {
                    return "Region 形态学操作模式无效。";
                }

                if (!IsFinite(RegionMorphologyRadius) || RegionMorphologyRadius < 0.5 || RegionMorphologyRadius > 100)
                {
                    return "Region 形态学圆形半径必须在 0.5 到 100 像素之间。";
                }
            }

            if (kind == VmToolKind.RegionFeatureFilter)
            {
                if (!VmRegionFeature.IsSupported(RegionFeature))
                {
                    return "Region 特征筛选类型无效。";
                }

                if (!IsFinite(RegionFeatureMin) || !IsFinite(RegionFeatureMax) || RegionFeatureMin < 0 || RegionFeatureMin > RegionFeatureMax)
                {
                    return "Region 特征筛选范围必须满足 0 ≤ 下限 ≤ 上限。";
                }

                if (VmRegionFeature.Normalize(RegionFeature) == VmRegionFeature.Circularity && RegionFeatureMax > 1)
                {
                    return "圆度筛选范围必须位于 0 到 1。";
                }

                if (RegionFeatureMax > 1000000000000.0)
                {
                    return "Region 特征筛选上限不能超过 1e12。";
                }
            }

            if (kind == VmToolKind.RegionSetOperation && !VmRegionSetOperationMode.IsSupported(RegionSetOperationMode))
            {
                return "Region 集合运算模式无效。";
            }

            if ((kind == VmToolKind.Blob || kind == VmToolKind.GrayStat || kind == VmToolKind.EdgeMeasure) &&
                !VmRoiExecutionMode.IsSupported(RoiExecutionMode))
            {
                return "ROI 执行模式无效。";
            }

            if (kind == VmToolKind.Blob)
            {
                if (!IsFinite(BlobMinGray) || !IsFinite(BlobMaxGray) || BlobMinGray < 0 || BlobMaxGray > 255 || BlobMinGray > BlobMaxGray)
                {
                    return "Blob 灰度范围必须满足 0 ≤ 下限 ≤ 上限 ≤ 255。";
                }

                if (!IsFinite(BlobMinArea) || BlobMinArea < 0)
                {
                    return "Blob 最小面积必须是非负数。";
                }
            }
            else if (kind == VmToolKind.GrayStat)
            {
                if (!IsFinite(GrayMin) || !IsFinite(GrayMax) || GrayMin < 0 || GrayMax > 255 || GrayMin > GrayMax)
                {
                    return "灰度判定范围必须满足 0 ≤ 下限 ≤ 上限 ≤ 255。";
                }
            }
            else if (kind == VmToolKind.EdgeMeasure && (!IsFinite(EdgeThreshold) || EdgeThreshold <= 0))
            {
                return "边缘阈值必须大于 0。";
            }

            return string.Empty;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsOddInRange(int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum && value % 2 == 1;
        }
    }

    public static class VmImageFilterMode
    {
        public const string Mean = "Mean";
        public const string Median = "Median";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Mean, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Median, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Mean, StringComparison.OrdinalIgnoreCase))
            {
                return Mean;
            }

            return string.Equals(value, Median, StringComparison.OrdinalIgnoreCase) ? Median : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            return normalized == Median ? "中值滤波" : (normalized == Mean ? "均值滤波" : "未知模式");
        }

        public static VmImageFilterModeOption[] CreateAll()
        {
            return new[]
            {
                new VmImageFilterModeOption { Key = Mean, DisplayText = "均值滤波（模板宽×高）" },
                new VmImageFilterModeOption { Key = Median, DisplayText = "中值滤波（圆形半径）" }
            };
        }
    }

    public sealed class VmImageFilterModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmRegionMorphologyMode
    {
        public const string OpeningCircle = "OpeningCircle";
        public const string ClosingCircle = "ClosingCircle";
        public const string DilationCircle = "DilationCircle";
        public const string ErosionCircle = "ErosionCircle";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, OpeningCircle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ClosingCircle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, DilationCircle, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ErosionCircle, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, OpeningCircle, StringComparison.OrdinalIgnoreCase)) return OpeningCircle;
            if (string.Equals(value, ClosingCircle, StringComparison.OrdinalIgnoreCase)) return ClosingCircle;
            if (string.Equals(value, DilationCircle, StringComparison.OrdinalIgnoreCase)) return DilationCircle;
            return string.Equals(value, ErosionCircle, StringComparison.OrdinalIgnoreCase) ? ErosionCircle : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == ClosingCircle) return "圆形闭运算";
            if (normalized == DilationCircle) return "圆形膨胀";
            if (normalized == ErosionCircle) return "圆形腐蚀";
            return normalized == OpeningCircle ? "圆形开运算" : "未知模式";
        }

        public static VmRegionMorphologyModeOption[] CreateAll()
        {
            return new[]
            {
                new VmRegionMorphologyModeOption { Key = OpeningCircle, DisplayText = "圆形开运算（去除小区域/毛刺）" },
                new VmRegionMorphologyModeOption { Key = ClosingCircle, DisplayText = "圆形闭运算（填补间隙/小孔）" },
                new VmRegionMorphologyModeOption { Key = DilationCircle, DisplayText = "圆形膨胀（扩大区域）" },
                new VmRegionMorphologyModeOption { Key = ErosionCircle, DisplayText = "圆形腐蚀（收缩区域）" }
            };
        }
    }

    public sealed class VmRegionMorphologyModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmRegionSetOperationMode
    {
        public const string Union = "Union";
        public const string Intersection = "Intersection";
        public const string Difference = "Difference";
        public const string SymmetricDifference = "SymmetricDifference";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Union, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Intersection, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Difference, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, SymmetricDifference, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Union, StringComparison.OrdinalIgnoreCase)) return Union;
            if (string.Equals(value, Intersection, StringComparison.OrdinalIgnoreCase)) return Intersection;
            if (string.Equals(value, Difference, StringComparison.OrdinalIgnoreCase)) return Difference;
            return string.Equals(value, SymmetricDifference, StringComparison.OrdinalIgnoreCase) ? SymmetricDifference : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Intersection) return "交集 A ∩ B";
            if (normalized == Difference) return "差集 A - B";
            if (normalized == SymmetricDifference) return "对称差 A △ B";
            return normalized == Union ? "并集 A ∪ B" : "未知操作";
        }

        public static VmRegionSetOperationModeOption[] CreateAll()
        {
            return new[]
            {
                new VmRegionSetOperationModeOption { Key = Union, DisplayText = "并集 A ∪ B" },
                new VmRegionSetOperationModeOption { Key = Intersection, DisplayText = "交集 A ∩ B" },
                new VmRegionSetOperationModeOption { Key = Difference, DisplayText = "差集 A - B" },
                new VmRegionSetOperationModeOption { Key = SymmetricDifference, DisplayText = "对称差 A △ B" }
            };
        }
    }

    public sealed class VmRegionSetOperationModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmRegionFeature
    {
        public const string Area = "Area";
        public const string Width = "Width";
        public const string Height = "Height";
        public const string Circularity = "Circularity";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Area, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Width, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Height, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Circularity, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Area, StringComparison.OrdinalIgnoreCase)) return Area;
            if (string.Equals(value, Width, StringComparison.OrdinalIgnoreCase)) return Width;
            if (string.Equals(value, Height, StringComparison.OrdinalIgnoreCase)) return Height;
            return string.Equals(value, Circularity, StringComparison.OrdinalIgnoreCase) ? Circularity : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Width) return "外接框宽度";
            if (normalized == Height) return "外接框高度";
            if (normalized == Circularity) return "圆度";
            return normalized == Area ? "区域面积" : "未知特征";
        }

        public static string GetHalconFeatureName(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Width) return "width";
            if (normalized == Height) return "height";
            if (normalized == Circularity) return "circularity";
            return "area";
        }

        public static string GetUnit(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Area) return "px²";
            if (normalized == Circularity) return "无量纲";
            return "px";
        }

        public static VmRegionFeatureOption[] CreateAll()
        {
            return new[]
            {
                new VmRegionFeatureOption { Key = Area, DisplayText = "区域面积（px²）" },
                new VmRegionFeatureOption { Key = Width, DisplayText = "外接框宽度（px）" },
                new VmRegionFeatureOption { Key = Height, DisplayText = "外接框高度（px）" },
                new VmRegionFeatureOption { Key = Circularity, DisplayText = "圆度（0..1）" }
            };
        }
    }

    public sealed class VmRegionFeatureOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmImageChannelMode
    {
        public const string Keep = "Keep";
        public const string ToGray = "ToGray";
        public const string Extract = "Extract";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Keep, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ToGray, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Extract, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Keep, StringComparison.OrdinalIgnoreCase))
            {
                return Keep;
            }

            if (string.Equals(value, ToGray, StringComparison.OrdinalIgnoreCase))
            {
                return ToGray;
            }

            return string.Equals(value, Extract, StringComparison.OrdinalIgnoreCase) ? Extract : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == ToGray) return "转换为灰度图";
            if (normalized == Extract) return "提取指定通道";
            return normalized == Keep ? "保持原图" : "未知模式";
        }

        public static VmImageChannelModeOption[] CreateAll()
        {
            return new[]
            {
                new VmImageChannelModeOption { Key = Keep, DisplayText = "保持原图（复制）" },
                new VmImageChannelModeOption { Key = ToGray, DisplayText = "转换为灰度图" },
                new VmImageChannelModeOption { Key = Extract, DisplayText = "提取指定通道" }
            };
        }
    }

    public sealed class VmImageChannelModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmRoiExecutionMode
    {
        public const string Union = "Union";
        public const string PerRoi = "PerRoi";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Union, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, PerRoi, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Union, StringComparison.OrdinalIgnoreCase))
            {
                return Union;
            }

            return string.Equals(value, PerRoi, StringComparison.OrdinalIgnoreCase) ? PerRoi : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            return normalized == PerRoi ? "逐 ROI 独立执行" : (normalized == Union ? "合并 ROI 执行" : "未知 ROI 执行模式");
        }

        public static VmRoiExecutionModeOption[] CreateAll()
        {
            return new[]
            {
                new VmRoiExecutionModeOption { Key = Union, DisplayText = "合并 ROI（兼容旧配方）" },
                new VmRoiExecutionModeOption { Key = PerRoi, DisplayText = "逐 ROI（独立判定与结果）" }
            };
        }
    }

    public sealed class VmRoiExecutionModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmNumericJudgeParameterValidator
    {
        public static string Validate(string comparisonOperator, double lower, double upper, double tolerance)
        {
            if (!NumericJudgeOperatorOption.IsSupported(comparisonOperator))
            {
                return "比较方式无效。";
            }

            if (!IsFinite(lower) || !IsFinite(upper))
            {
                return "阈值必须是有限数值。";
            }

            if ((comparisonOperator == NumericJudgeOperatorOption.BetweenInclusive ||
                 comparisonOperator == NumericJudgeOperatorOption.OutsideInclusive) && lower > upper)
            {
                return "区间下限不能大于上限。";
            }

            if (!IsFinite(tolerance) || tolerance < 0)
            {
                return "相等容差必须大于或等于 0。";
            }

            return string.Empty;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
