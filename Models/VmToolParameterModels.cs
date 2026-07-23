using System;
using System.IO;
using System.Linq;
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
            ImageEnhancementMode = VmImageEnhancementMode.LinearScale;
            ImageEnhancementScale = 1.0;
            ImageEnhancementOffset = 0.0;
            ImageEnhancementMaskWidth = 7;
            ImageEnhancementMaskHeight = 7;
            ImageEnhancementFactor = 1.0;
            DynamicThresholdPolarity = VmDynamicThresholdPolarity.Light;
            DynamicThresholdOffset = 5.0;
            ImageDifferenceMode = VmImageDifferenceMode.AbsoluteDifference;
            ImageDifferenceMultiplier = 1.0;
            ImageDifferenceAdd = 0.0;
            RegionContourMode = VmRegionContourMode.Border;
            XldContourFeature = VmXldContourFeature.ContourLength;
            XldContourMin = 0;
            XldContourMax = 999999999;
            XldSegmentMode = VmXldSegmentMode.LinesCircles;
            XldSegmentSmooth = 5;
            XldSegmentMaxLineDistance1 = 4;
            XldSegmentMaxLineDistance2 = 2;
            XldLineFitAlgorithm = VmXldLineFitAlgorithm.Tukey;
            XldLineFitMaxNumPoints = -1;
            XldLineFitClippingEndPoints = 0;
            XldLineFitIterations = 5;
            XldLineFitClippingFactor = 2;
            XldCircleFitAlgorithm = VmXldCircleFitAlgorithm.ATukey;
            XldCircleFitMaxNumPoints = -1;
            XldCircleFitMaxClosureDistance = 1.5;
            XldCircleFitClippingEndPoints = 0;
            XldCircleFitIterations = 5;
            XldCircleFitClippingFactor = 2;
            XldEllipseFitAlgorithm = VmXldEllipseFitAlgorithm.FTukey;
            XldEllipseFitMaxNumPoints = -1;
            XldEllipseFitMaxClosureDistance = 1.5;
            XldEllipseFitClippingEndPoints = 0;
            XldEllipseFitVossTabSize = 200;
            XldEllipseFitIterations = 5;
            XldEllipseFitClippingFactor = 2;
            XldRectangleFitAlgorithm = VmXldRectangleFitAlgorithm.Tukey;
            XldRectangleFitMaxNumPoints = -1;
            XldRectangleFitMaxClosureDistance = 1.5;
            XldRectangleFitClippingEndPoints = 0;
            XldRectangleFitIterations = 5;
            XldRectangleFitClippingFactor = 2;
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
        [DataMember(Order = 24, EmitDefaultValue = false)] public string ImageEnhancementMode { get; set; }
        [DataMember(Order = 25, EmitDefaultValue = false)] public double ImageEnhancementScale { get; set; }
        [DataMember(Order = 26, EmitDefaultValue = false)] public double ImageEnhancementOffset { get; set; }
        [DataMember(Order = 27, EmitDefaultValue = false)] public int ImageEnhancementMaskWidth { get; set; }
        [DataMember(Order = 28, EmitDefaultValue = false)] public int ImageEnhancementMaskHeight { get; set; }
        [DataMember(Order = 29, EmitDefaultValue = false)] public double ImageEnhancementFactor { get; set; }
        [DataMember(Order = 30, EmitDefaultValue = false)] public string DynamicThresholdPolarity { get; set; }
        [DataMember(Order = 31, EmitDefaultValue = false)] public double DynamicThresholdOffset { get; set; }
        [DataMember(Order = 32, EmitDefaultValue = false)] public string ImageDifferenceMode { get; set; }
        [DataMember(Order = 33, EmitDefaultValue = false)] public double ImageDifferenceMultiplier { get; set; }
        [DataMember(Order = 34, EmitDefaultValue = false)] public double ImageDifferenceAdd { get; set; }
        [DataMember(Order = 35, EmitDefaultValue = false)] public string RegionContourMode { get; set; }
        [DataMember(Order = 36, EmitDefaultValue = false)] public string XldContourFeature { get; set; }
        [DataMember(Order = 37, EmitDefaultValue = false)] public double XldContourMin { get; set; }
        [DataMember(Order = 38, EmitDefaultValue = false)] public double XldContourMax { get; set; }
        [DataMember(Order = 39, EmitDefaultValue = false)] public string XldSegmentMode { get; set; }
        [DataMember(Order = 40, EmitDefaultValue = false)] public int XldSegmentSmooth { get; set; }
        [DataMember(Order = 41, EmitDefaultValue = false)] public double XldSegmentMaxLineDistance1 { get; set; }
        [DataMember(Order = 42, EmitDefaultValue = false)] public double XldSegmentMaxLineDistance2 { get; set; }
        [DataMember(Order = 43, EmitDefaultValue = false)] public string XldLineFitAlgorithm { get; set; }
        [DataMember(Order = 44, EmitDefaultValue = false)] public int XldLineFitMaxNumPoints { get; set; }
        [DataMember(Order = 45, EmitDefaultValue = false)] public int XldLineFitClippingEndPoints { get; set; }
        [DataMember(Order = 46, EmitDefaultValue = false)] public int XldLineFitIterations { get; set; }
        [DataMember(Order = 47, EmitDefaultValue = false)] public double XldLineFitClippingFactor { get; set; }
        [DataMember(Order = 48, EmitDefaultValue = false)] public string XldCircleFitAlgorithm { get; set; }
        [DataMember(Order = 49, EmitDefaultValue = false)] public int XldCircleFitMaxNumPoints { get; set; }
        [DataMember(Order = 50, EmitDefaultValue = false)] public double XldCircleFitMaxClosureDistance { get; set; }
        [DataMember(Order = 51, EmitDefaultValue = false)] public int XldCircleFitClippingEndPoints { get; set; }
        [DataMember(Order = 52, EmitDefaultValue = false)] public int XldCircleFitIterations { get; set; }
        [DataMember(Order = 53, EmitDefaultValue = false)] public double XldCircleFitClippingFactor { get; set; }
        [DataMember(Order = 54, EmitDefaultValue = false)] public string XldEllipseFitAlgorithm { get; set; }
        [DataMember(Order = 55, EmitDefaultValue = false)] public int XldEllipseFitMaxNumPoints { get; set; }
        [DataMember(Order = 56, EmitDefaultValue = false)] public double XldEllipseFitMaxClosureDistance { get; set; }
        [DataMember(Order = 57, EmitDefaultValue = false)] public int XldEllipseFitClippingEndPoints { get; set; }
        [DataMember(Order = 58, EmitDefaultValue = false)] public int XldEllipseFitVossTabSize { get; set; }
        [DataMember(Order = 59, EmitDefaultValue = false)] public int XldEllipseFitIterations { get; set; }
        [DataMember(Order = 60, EmitDefaultValue = false)] public double XldEllipseFitClippingFactor { get; set; }
        [DataMember(Order = 61, EmitDefaultValue = false)] public string XldRectangleFitAlgorithm { get; set; }
        [DataMember(Order = 62, EmitDefaultValue = false)] public int XldRectangleFitMaxNumPoints { get; set; }
        [DataMember(Order = 63, EmitDefaultValue = false)] public double XldRectangleFitMaxClosureDistance { get; set; }
        [DataMember(Order = 64, EmitDefaultValue = false)] public int XldRectangleFitClippingEndPoints { get; set; }
        [DataMember(Order = 65, EmitDefaultValue = false)] public int XldRectangleFitIterations { get; set; }
        [DataMember(Order = 66, EmitDefaultValue = false)] public double XldRectangleFitClippingFactor { get; set; }

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
            ImageEnhancementMode = VmImageEnhancementMode.Normalize(ImageEnhancementMode);
            DynamicThresholdPolarity = VmDynamicThresholdPolarity.Normalize(DynamicThresholdPolarity);
            ImageDifferenceMode = VmImageDifferenceMode.Normalize(ImageDifferenceMode);
            RegionContourMode = VmRegionContourMode.Normalize(RegionContourMode);
            if (string.IsNullOrWhiteSpace(XldContourFeature))
            {
                XldContourFeature = VmXldContourFeature.ContourLength;
                if (XldContourMin == 0 && XldContourMax == 0) XldContourMax = 999999999;
            }
            else
            {
                XldContourFeature = VmXldContourFeature.Normalize(XldContourFeature);
            }
            if (string.IsNullOrWhiteSpace(XldSegmentMode))
            {
                XldSegmentMode = VmXldSegmentMode.LinesCircles;
                if (XldSegmentSmooth == 0 && XldSegmentMaxLineDistance1 == 0 && XldSegmentMaxLineDistance2 == 0)
                {
                    XldSegmentSmooth = 5;
                    XldSegmentMaxLineDistance1 = 4;
                    XldSegmentMaxLineDistance2 = 2;
                }
            }
            else
            {
                XldSegmentMode = VmXldSegmentMode.Normalize(XldSegmentMode);
            }
            if (string.IsNullOrWhiteSpace(XldLineFitAlgorithm))
            {
                XldLineFitAlgorithm = VmXldLineFitAlgorithm.Tukey;
                if (XldLineFitMaxNumPoints == 0 && XldLineFitClippingEndPoints == 0 &&
                    XldLineFitIterations == 0 && XldLineFitClippingFactor == 0)
                {
                    XldLineFitMaxNumPoints = -1;
                    XldLineFitIterations = 5;
                    XldLineFitClippingFactor = 2;
                }
            }
            else
            {
                XldLineFitAlgorithm = VmXldLineFitAlgorithm.Normalize(XldLineFitAlgorithm);
            }
            if (string.IsNullOrWhiteSpace(XldCircleFitAlgorithm))
            {
                XldCircleFitAlgorithm = VmXldCircleFitAlgorithm.ATukey;
                if (XldCircleFitMaxNumPoints == 0 && XldCircleFitMaxClosureDistance == 0 &&
                    XldCircleFitClippingEndPoints == 0 && XldCircleFitIterations == 0 && XldCircleFitClippingFactor == 0)
                {
                    XldCircleFitMaxNumPoints = -1;
                    XldCircleFitMaxClosureDistance = 1.5;
                    XldCircleFitIterations = 5;
                    XldCircleFitClippingFactor = 2;
                }
            }
            else
            {
                if (VmXldCircleFitAlgorithm.IsSupported(XldCircleFitAlgorithm))
                {
                    XldCircleFitAlgorithm = VmXldCircleFitAlgorithm.Normalize(XldCircleFitAlgorithm);
                }
            }
            if (string.IsNullOrWhiteSpace(XldEllipseFitAlgorithm))
            {
                XldEllipseFitAlgorithm = VmXldEllipseFitAlgorithm.FTukey;
                if (XldEllipseFitMaxNumPoints == 0 && XldEllipseFitMaxClosureDistance == 0 &&
                    XldEllipseFitClippingEndPoints == 0 && XldEllipseFitVossTabSize == 0 &&
                    XldEllipseFitIterations == 0 && XldEllipseFitClippingFactor == 0)
                {
                    XldEllipseFitMaxNumPoints = -1;
                    XldEllipseFitMaxClosureDistance = 1.5;
                    XldEllipseFitVossTabSize = 200;
                    XldEllipseFitIterations = 5;
                    XldEllipseFitClippingFactor = 2;
                }
            }
            else if (VmXldEllipseFitAlgorithm.IsSupported(XldEllipseFitAlgorithm))
            {
                XldEllipseFitAlgorithm = VmXldEllipseFitAlgorithm.Normalize(XldEllipseFitAlgorithm);
            }
            if (string.IsNullOrWhiteSpace(XldRectangleFitAlgorithm))
            {
                XldRectangleFitAlgorithm = VmXldRectangleFitAlgorithm.Tukey;
                if (XldRectangleFitMaxNumPoints == 0 && XldRectangleFitMaxClosureDistance == 0 &&
                    XldRectangleFitClippingEndPoints == 0 && XldRectangleFitIterations == 0 &&
                    XldRectangleFitClippingFactor == 0)
                {
                    XldRectangleFitMaxNumPoints = -1;
                    XldRectangleFitMaxClosureDistance = 1.5;
                    XldRectangleFitIterations = 5;
                    XldRectangleFitClippingFactor = 2;
                }
            }
            else if (VmXldRectangleFitAlgorithm.IsSupported(XldRectangleFitAlgorithm))
            {
                XldRectangleFitAlgorithm = VmXldRectangleFitAlgorithm.Normalize(XldRectangleFitAlgorithm);
            }
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

            if (kind == VmToolKind.ImageEnhancement)
            {
                if (!VmImageEnhancementMode.IsSupported(ImageEnhancementMode))
                {
                    return "图像增强模式无效。";
                }

                string enhancementMode = VmImageEnhancementMode.Normalize(ImageEnhancementMode);
                if (enhancementMode == VmImageEnhancementMode.LinearScale &&
                    (!IsFinite(ImageEnhancementScale) || !IsFinite(ImageEnhancementOffset) ||
                     ImageEnhancementScale < 0.01 || ImageEnhancementScale > 10 ||
                     ImageEnhancementOffset < -255 || ImageEnhancementOffset > 255))
                {
                    return "线性灰度缩放必须满足增益 0.01..10、偏移 -255..255。";
                }

                if (enhancementMode == VmImageEnhancementMode.LocalContrast &&
                    (!IsOddInRange(ImageEnhancementMaskWidth, 3, 255) ||
                     !IsOddInRange(ImageEnhancementMaskHeight, 3, 255) ||
                     !IsFinite(ImageEnhancementFactor) || ImageEnhancementFactor <= 0 || ImageEnhancementFactor > 20))
                {
                    return "局部对比度增强必须使用 3..255 的奇数模板，增强因子必须满足 0 < 因子 ≤ 20。";
                }
            }

            if (kind == VmToolKind.DynamicThreshold)
            {
                if (!VmDynamicThresholdPolarity.IsSupported(DynamicThresholdPolarity))
                {
                    return "动态阈值明暗极性无效。";
                }

                if (!IsFinite(DynamicThresholdOffset) || DynamicThresholdOffset < 1 || DynamicThresholdOffset >= 255)
                {
                    return "动态阈值偏移必须满足 1 ≤ 偏移 < 255；过小偏移会产生大量噪声。";
                }
            }

            if (kind == VmToolKind.ImageDifference)
            {
                if (!VmImageDifferenceMode.IsSupported(ImageDifferenceMode))
                {
                    return "双图像差分模式无效。";
                }

                if (!IsFinite(ImageDifferenceMultiplier) || ImageDifferenceMultiplier <= 0 || ImageDifferenceMultiplier > 100)
                {
                    return "双图像差分增益必须满足 0 < 增益 ≤ 100。";
                }

                if (VmImageDifferenceMode.Normalize(ImageDifferenceMode) != VmImageDifferenceMode.AbsoluteDifference &&
                    (!IsFinite(ImageDifferenceAdd) || ImageDifferenceAdd < -65535 || ImageDifferenceAdd > 65535))
                {
                    return "方向减法灰度偏移必须在 -65535 到 65535 之间。";
                }
            }

            if (kind == VmToolKind.RegionContourMeasurement && !VmRegionContourMode.IsSupported(RegionContourMode))
            {
                return "Region 轮廓模式无效。";
            }

            if (kind == VmToolKind.XldContourFilter)
            {
                if (!VmXldContourFeature.IsSupported(XldContourFeature))
                {
                    return "XLD 轮廓筛选特征无效。";
                }

                if (!IsFinite(XldContourMin) || !IsFinite(XldContourMax) || XldContourMin < 0 || XldContourMin > XldContourMax)
                {
                    return "XLD 轮廓筛选范围必须满足 0 ≤ 下限 ≤ 上限。";
                }

                if (XldContourMax > 1000000000000.0)
                {
                    return "XLD 轮廓筛选上限不能超过 1e12 像素。";
                }
            }

            if (kind == VmToolKind.XldContourSegmentation)
            {
                if (!VmXldSegmentMode.IsSupported(XldSegmentMode))
                {
                    return "XLD 轮廓分段模式无效。";
                }

                if (XldSegmentSmooth != 0 && (XldSegmentSmooth < 3 || XldSegmentSmooth > 101 || XldSegmentSmooth % 2 == 0))
                {
                    return "XLD 分段平滑点数必须为 0，或 3 到 101 之间的奇数。";
                }

                if (!IsFinite(XldSegmentMaxLineDistance1) || !IsFinite(XldSegmentMaxLineDistance2) ||
                    XldSegmentMaxLineDistance1 < 0 || XldSegmentMaxLineDistance1 > 10000 ||
                    XldSegmentMaxLineDistance2 < 0 || XldSegmentMaxLineDistance2 > 10000)
                {
                    return "XLD 分段最大线距 1/2 必须是 0 到 10000 之间的有限数值。";
                }
            }

            if (kind == VmToolKind.XldLineFitting)
            {
                if (!VmXldLineFitAlgorithm.IsSupported(XldLineFitAlgorithm))
                {
                    return "XLD 直线拟合算法无效。";
                }

                if (XldLineFitMaxNumPoints != -1 && (XldLineFitMaxNumPoints < 2 || XldLineFitMaxNumPoints > 1000000))
                {
                    return "最大采样点数必须为 -1（全部），或 2 到 1000000。";
                }

                if (XldLineFitClippingEndPoints < 0 || XldLineFitClippingEndPoints > 100000)
                {
                    return "端点裁剪数量必须在 0 到 100000 之间。";
                }

                if (XldLineFitIterations < 0 || XldLineFitIterations > 1000)
                {
                    return "鲁棒迭代次数必须在 0 到 1000 之间。";
                }

                if (!IsFinite(XldLineFitClippingFactor) || XldLineFitClippingFactor <= 0 || XldLineFitClippingFactor > 100)
                {
                    return "裁剪因子必须满足 0 < 因子 ≤ 100。";
                }
            }

            if (kind == VmToolKind.XldCircleFitting)
            {
                if (!VmXldCircleFitAlgorithm.IsSupported(XldCircleFitAlgorithm))
                {
                    return "XLD 圆拟合算法无效。";
                }

                if (XldCircleFitMaxNumPoints != -1 && (XldCircleFitMaxNumPoints < 3 || XldCircleFitMaxNumPoints > 1000000))
                {
                    return "圆拟合最大采样点数必须为 -1（全部），或 3 到 1000000。";
                }

                if (!IsFinite(XldCircleFitMaxClosureDistance) || XldCircleFitMaxClosureDistance < 0 || XldCircleFitMaxClosureDistance > 100000)
                {
                    return "最大闭合距离必须是 0 到 100000 之间的有限数值。";
                }

                if (XldCircleFitClippingEndPoints < 0 || XldCircleFitClippingEndPoints > 100000)
                {
                    return "圆拟合端点裁剪数量必须在 0 到 100000 之间。";
                }

                if (XldCircleFitIterations < 0 || XldCircleFitIterations > 1000)
                {
                    return "圆拟合鲁棒迭代次数必须在 0 到 1000 之间。";
                }

                if (!IsFinite(XldCircleFitClippingFactor) || XldCircleFitClippingFactor <= 0 || XldCircleFitClippingFactor > 100)
                {
                    return "圆拟合裁剪因子必须满足 0 < 因子 ≤ 100。";
                }
            }

            if (kind == VmToolKind.XldEllipseFitting)
            {
                if (!VmXldEllipseFitAlgorithm.IsSupported(XldEllipseFitAlgorithm))
                {
                    return "XLD 椭圆拟合算法无效。";
                }

                if (XldEllipseFitMaxNumPoints != -1 && (XldEllipseFitMaxNumPoints < 5 || XldEllipseFitMaxNumPoints > 1000000))
                {
                    return "椭圆拟合最大采样点数必须为 -1（全部），或 5 到 1000000。";
                }

                if (!IsFinite(XldEllipseFitMaxClosureDistance) || XldEllipseFitMaxClosureDistance < 0 || XldEllipseFitMaxClosureDistance > 100000)
                {
                    return "椭圆拟合最大闭合距离必须是 0 到 100000 之间的有限数值。";
                }

                if (XldEllipseFitClippingEndPoints < 0 || XldEllipseFitClippingEndPoints > 100000)
                {
                    return "椭圆拟合端点裁剪数量必须在 0 到 100000 之间。";
                }

                if (XldEllipseFitVossTabSize < 4 || XldEllipseFitVossTabSize > 1000000)
                {
                    return "Voss 查找表大小必须在 4 到 1000000 之间。";
                }

                if (XldEllipseFitIterations < 0 || XldEllipseFitIterations > 1000)
                {
                    return "椭圆拟合鲁棒迭代次数必须在 0 到 1000 之间。";
                }

                if (!IsFinite(XldEllipseFitClippingFactor) || XldEllipseFitClippingFactor <= 0 || XldEllipseFitClippingFactor > 100)
                {
                    return "椭圆拟合裁剪因子必须满足 0 < 因子 ≤ 100。";
                }
            }

            if (kind == VmToolKind.XldRectangleFitting)
            {
                if (!VmXldRectangleFitAlgorithm.IsSupported(XldRectangleFitAlgorithm))
                {
                    return "XLD 旋转矩形拟合算法无效。";
                }

                if (XldRectangleFitMaxNumPoints != -1 &&
                    (XldRectangleFitMaxNumPoints < 8 || XldRectangleFitMaxNumPoints > 1000000))
                {
                    return "旋转矩形拟合最大采样点数必须为 -1（全部），或 8 到 1000000。";
                }

                if (!IsFinite(XldRectangleFitMaxClosureDistance) ||
                    XldRectangleFitMaxClosureDistance < 0 || XldRectangleFitMaxClosureDistance > 100000)
                {
                    return "旋转矩形拟合最大闭合距离必须是 0 到 100000 之间的有限数值。";
                }

                if (XldRectangleFitClippingEndPoints < 0 || XldRectangleFitClippingEndPoints > 100000)
                {
                    return "旋转矩形拟合端点裁剪数量必须在 0 到 100000 之间。";
                }

                if (XldRectangleFitIterations < 0 || XldRectangleFitIterations > 1000)
                {
                    return "旋转矩形拟合鲁棒迭代次数必须在 0 到 1000 之间。";
                }

                if (!IsFinite(XldRectangleFitClippingFactor) ||
                    XldRectangleFitClippingFactor <= 0 || XldRectangleFitClippingFactor > 100)
                {
                    return "旋转矩形拟合裁剪因子必须满足 0 < 因子 ≤ 100。";
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

    public static class VmImageEnhancementMode
    {
        public const string LinearScale = "LinearScale";
        public const string LocalContrast = "LocalContrast";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, LinearScale, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, LocalContrast, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, LinearScale, StringComparison.OrdinalIgnoreCase))
            {
                return LinearScale;
            }

            return string.Equals(value, LocalContrast, StringComparison.OrdinalIgnoreCase) ? LocalContrast : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            return normalized == LocalContrast ? "局部对比度增强" : (normalized == LinearScale ? "线性灰度缩放" : "未知模式");
        }

        public static VmImageEnhancementModeOption[] CreateAll()
        {
            return new[]
            {
                new VmImageEnhancementModeOption { Key = LinearScale, DisplayText = "线性灰度缩放（增益 + 偏移）" },
                new VmImageEnhancementModeOption { Key = LocalContrast, DisplayText = "局部对比度增强（Emphasize）" }
            };
        }
    }

    public sealed class VmImageEnhancementModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmImageDifferenceMode
    {
        public const string AbsoluteDifference = "AbsoluteDifference";
        public const string SubtractAB = "SubtractAB";
        public const string SubtractBA = "SubtractBA";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, AbsoluteDifference, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, SubtractAB, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, SubtractBA, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, AbsoluteDifference, StringComparison.OrdinalIgnoreCase))
            {
                return AbsoluteDifference;
            }

            if (string.Equals(value, SubtractAB, StringComparison.OrdinalIgnoreCase)) return SubtractAB;
            if (string.Equals(value, SubtractBA, StringComparison.OrdinalIgnoreCase)) return SubtractBA;
            return value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == SubtractAB) return "方向减法 A - B";
            if (normalized == SubtractBA) return "方向减法 B - A";
            return normalized == AbsoluteDifference ? "绝对差分 |A - B|" : "未知模式";
        }

        public static VmImageDifferenceModeOption[] CreateAll()
        {
            return new[]
            {
                new VmImageDifferenceModeOption { Key = AbsoluteDifference, DisplayText = "绝对差分 |A - B|（AbsDiffImage）" },
                new VmImageDifferenceModeOption { Key = SubtractAB, DisplayText = "方向减法 A - B（SubImage）" },
                new VmImageDifferenceModeOption { Key = SubtractBA, DisplayText = "方向减法 B - A（SubImage）" }
            };
        }
    }

    public static class VmRegionContourMode
    {
        public const string Border = "Border";
        public const string Center = "Center";
        public const string BorderHoles = "BorderHoles";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Border, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Center, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, BorderHoles, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Border, StringComparison.OrdinalIgnoreCase)) return Border;
            if (string.Equals(value, Center, StringComparison.OrdinalIgnoreCase)) return Center;
            return string.Equals(value, BorderHoles, StringComparison.OrdinalIgnoreCase) ? BorderHoles : value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Center) return "center";
            if (normalized == BorderHoles) return "border_holes";
            return "border";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Center) return "边界像素中心";
            if (normalized == BorderHoles) return "外边界与孔洞";
            return normalized == Border ? "像素外边界" : "未知模式";
        }

        public static VmRegionContourModeOption[] CreateAll()
        {
            return new[]
            {
                new VmRegionContourModeOption { Key = Border, DisplayText = "像素外边界（border）" },
                new VmRegionContourModeOption { Key = Center, DisplayText = "边界像素中心（center）" },
                new VmRegionContourModeOption { Key = BorderHoles, DisplayText = "外边界与孔洞（border_holes）" }
            };
        }
    }

    public sealed class VmRegionContourModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmXldContourFeature
    {
        public const string ContourLength = "ContourLength";
        public const string MaximumExtent = "MaximumExtent";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, ContourLength, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, MaximumExtent, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, ContourLength, StringComparison.OrdinalIgnoreCase)) return ContourLength;
            return string.Equals(value, MaximumExtent, StringComparison.OrdinalIgnoreCase) ? MaximumExtent : value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            return Normalize(value) == MaximumExtent ? "maximum_extent" : "contour_length";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == MaximumExtent) return "最大跨度";
            return normalized == ContourLength ? "轮廓长度" : "未知特征";
        }

        public static VmXldContourFeatureOption[] CreateAll()
        {
            return new[]
            {
                new VmXldContourFeatureOption { Key = ContourLength, DisplayText = "轮廓长度（contour_length）" },
                new VmXldContourFeatureOption { Key = MaximumExtent, DisplayText = "最大跨度（maximum_extent）" }
            };
        }
    }

    public sealed class VmXldContourFeatureOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmXldSegmentMode
    {
        public const string Lines = "Lines";
        public const string LinesCircles = "LinesCircles";
        public const string LinesEllipses = "LinesEllipses";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Lines, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, LinesCircles, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, LinesEllipses, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, LinesCircles, StringComparison.OrdinalIgnoreCase)) return LinesCircles;
            if (string.Equals(value, Lines, StringComparison.OrdinalIgnoreCase)) return Lines;
            return string.Equals(value, LinesEllipses, StringComparison.OrdinalIgnoreCase) ? LinesEllipses : value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Lines) return "lines";
            if (normalized == LinesEllipses) return "lines_ellipses";
            return "lines_circles";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Lines) return "仅直线";
            if (normalized == LinesEllipses) return "直线 + 椭圆弧";
            return normalized == LinesCircles ? "直线 + 圆弧" : "未知模式";
        }

        public static VmXldSegmentModeOption[] CreateAll()
        {
            return new[]
            {
                new VmXldSegmentModeOption { Key = Lines, DisplayText = "仅直线（lines）" },
                new VmXldSegmentModeOption { Key = LinesCircles, DisplayText = "直线 + 圆弧（lines_circles）" },
                new VmXldSegmentModeOption { Key = LinesEllipses, DisplayText = "直线 + 椭圆弧（lines_ellipses）" }
            };
        }
    }

    public sealed class VmXldSegmentModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmXldLineFitAlgorithm
    {
        public const string Regression = "Regression";
        public const string Huber = "Huber";
        public const string Tukey = "Tukey";
        public const string Drop = "Drop";
        public const string Gauss = "Gauss";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Regression, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Huber, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Tukey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Drop, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Gauss, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Tukey, StringComparison.OrdinalIgnoreCase)) return Tukey;
            if (string.Equals(value, Regression, StringComparison.OrdinalIgnoreCase)) return Regression;
            if (string.Equals(value, Huber, StringComparison.OrdinalIgnoreCase)) return Huber;
            if (string.Equals(value, Drop, StringComparison.OrdinalIgnoreCase)) return Drop;
            return string.Equals(value, Gauss, StringComparison.OrdinalIgnoreCase) ? Gauss : value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            return Normalize(value).ToLowerInvariant();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Regression) return "最小二乘";
            if (normalized == Huber) return "Huber 鲁棒";
            if (normalized == Drop) return "剔除离群点";
            if (normalized == Gauss) return "高斯加权";
            return normalized == Tukey ? "Tukey 鲁棒" : "未知算法";
        }

        public static VmXldLineFitAlgorithmOption[] CreateAll()
        {
            return new[]
            {
                new VmXldLineFitAlgorithmOption { Key = Tukey, DisplayText = "Tukey 鲁棒（推荐）" },
                new VmXldLineFitAlgorithmOption { Key = Huber, DisplayText = "Huber 鲁棒" },
                new VmXldLineFitAlgorithmOption { Key = Regression, DisplayText = "最小二乘（regression）" },
                new VmXldLineFitAlgorithmOption { Key = Drop, DisplayText = "剔除离群点（drop）" },
                new VmXldLineFitAlgorithmOption { Key = Gauss, DisplayText = "高斯加权（gauss）" }
            };
        }
    }

    public sealed class VmXldLineFitAlgorithmOption
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
    }

    public static class VmXldCircleFitAlgorithm
    {
        public const string Algebraic = "Algebraic";
        public const string AHuber = "AHuber";
        public const string ATukey = "ATukey";
        public const string Geometric = "Geometric";
        public const string GeoHuber = "GeoHuber";
        public const string GeoTukey = "GeoTukey";

        public static bool IsSupported(string value)
        {
            return new[] { Algebraic, AHuber, ATukey, Geometric, GeoHuber, GeoTukey }
                .Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        }

        public static string Normalize(string value)
        {
            return new[] { Algebraic, AHuber, ATukey, Geometric, GeoHuber, GeoTukey }
                .FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? ATukey;
        }

        public static string GetHalconValue(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Algebraic) return "algebraic";
            if (normalized == AHuber) return "ahuber";
            if (normalized == Geometric) return "geometric";
            if (normalized == GeoHuber) return "geohuber";
            if (normalized == GeoTukey) return "geotukey";
            return "atukey";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Algebraic) return "代数拟合";
            if (normalized == AHuber) return "代数 Huber 鲁棒";
            if (normalized == Geometric) return "几何拟合";
            if (normalized == GeoHuber) return "几何 Huber 鲁棒";
            if (normalized == GeoTukey) return "几何 Tukey 鲁棒";
            return "代数 Tukey 鲁棒";
        }

        public static VmXldCircleFitAlgorithmOption[] CreateAll()
        {
            return new[]
            {
                new VmXldCircleFitAlgorithmOption { Key = ATukey, DisplayText = "代数 Tukey 鲁棒（推荐）" },
                new VmXldCircleFitAlgorithmOption { Key = AHuber, DisplayText = "代数 Huber 鲁棒" },
                new VmXldCircleFitAlgorithmOption { Key = Algebraic, DisplayText = "代数拟合" },
                new VmXldCircleFitAlgorithmOption { Key = GeoTukey, DisplayText = "几何 Tukey 鲁棒" },
                new VmXldCircleFitAlgorithmOption { Key = GeoHuber, DisplayText = "几何 Huber 鲁棒" },
                new VmXldCircleFitAlgorithmOption { Key = Geometric, DisplayText = "几何拟合" }
            };
        }
    }

    public sealed class VmXldCircleFitAlgorithmOption
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
    }

    public static class VmXldEllipseFitAlgorithm
    {
        public const string Fitzgibbon = "Fitzgibbon";
        public const string FHuber = "FHuber";
        public const string FTukey = "FTukey";
        public const string FocPoints = "FocPoints";
        public const string FPHuber = "FPHuber";
        public const string FPTukey = "FPTukey";
        public const string Geometric = "Geometric";
        public const string GeoHuber = "GeoHuber";
        public const string GeoTukey = "GeoTukey";
        public const string Voss = "Voss";

        private static readonly string[] Values =
        {
            Fitzgibbon, FHuber, FTukey, FocPoints, FPHuber, FPTukey,
            Geometric, GeoHuber, GeoTukey, Voss
        };

        public static bool IsSupported(string value)
        {
            return Values.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        }

        public static string Normalize(string value)
        {
            return Values.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? FTukey;
        }

        public static string GetHalconValue(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Fitzgibbon) return "fitzgibbon";
            if (normalized == FHuber) return "fhuber";
            if (normalized == FocPoints) return "focpoints";
            if (normalized == FPHuber) return "fphuber";
            if (normalized == FPTukey) return "fptukey";
            if (normalized == Geometric) return "geometric";
            if (normalized == GeoHuber) return "geohuber";
            if (normalized == GeoTukey) return "geotukey";
            if (normalized == Voss) return "voss";
            return "ftukey";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Fitzgibbon) return "Fitzgibbon 直接拟合";
            if (normalized == FHuber) return "Fitzgibbon Huber 鲁棒";
            if (normalized == FocPoints) return "焦点距离拟合";
            if (normalized == FPHuber) return "焦点距离 Huber 鲁棒";
            if (normalized == FPTukey) return "焦点距离 Tukey 鲁棒";
            if (normalized == Geometric) return "几何拟合";
            if (normalized == GeoHuber) return "几何 Huber 鲁棒";
            if (normalized == GeoTukey) return "几何 Tukey 鲁棒";
            if (normalized == Voss) return "Voss 查表拟合";
            return "Fitzgibbon Tukey 鲁棒";
        }

        public static VmXldEllipseFitAlgorithmOption[] CreateAll()
        {
            return Values.Select(value => new VmXldEllipseFitAlgorithmOption
            {
                Key = value,
                DisplayText = GetDisplayText(value) + (value == FTukey ? "（推荐）" : string.Empty)
            }).ToArray();
        }
    }

    public sealed class VmXldEllipseFitAlgorithmOption
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
    }

    public static class VmXldRectangleFitAlgorithm
    {
        public const string Regression = "Regression";
        public const string Huber = "Huber";
        public const string Tukey = "Tukey";

        private static readonly string[] Values = { Regression, Huber, Tukey };

        public static bool IsSupported(string value)
        {
            return Values.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        }

        public static string Normalize(string value)
        {
            return Values.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? Tukey;
        }

        public static string GetHalconValue(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Regression) return "regression";
            if (normalized == Huber) return "huber";
            return "tukey";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Regression) return "回归拟合";
            if (normalized == Huber) return "Huber 鲁棒拟合";
            return "Tukey 鲁棒拟合";
        }

        public static VmXldRectangleFitAlgorithmOption[] CreateAll()
        {
            return Values.Select(value => new VmXldRectangleFitAlgorithmOption
            {
                Key = value,
                DisplayText = GetDisplayText(value) + (value == Tukey ? "（推荐）" : string.Empty)
            }).ToArray();
        }
    }

    public sealed class VmXldRectangleFitAlgorithmOption
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
    }

    public sealed class VmImageDifferenceModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmDynamicThresholdPolarity
    {
        public const string Light = "Light";
        public const string Dark = "Dark";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Light, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Dark, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Light, StringComparison.OrdinalIgnoreCase))
            {
                return Light;
            }

            return string.Equals(value, Dark, StringComparison.OrdinalIgnoreCase) ? Dark : value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            return Normalize(value) == Dark ? "dark" : "light";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            return normalized == Dark ? "暗缺陷（原图 ≤ 参考图 - 偏移）" :
                (normalized == Light ? "明缺陷（原图 ≥ 参考图 + 偏移）" : "未知极性");
        }

        public static VmDynamicThresholdPolarityOption[] CreateAll()
        {
            return new[]
            {
                new VmDynamicThresholdPolarityOption { Key = Light, DisplayText = "明缺陷（Light）" },
                new VmDynamicThresholdPolarityOption { Key = Dark, DisplayText = "暗缺陷（Dark）" }
            };
        }
    }

    public sealed class VmDynamicThresholdPolarityOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
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
