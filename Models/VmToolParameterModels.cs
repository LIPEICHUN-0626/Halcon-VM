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
            LineRelationParallelToleranceDegrees = 1.0;
            LineRelationCoincidentDistance = 1.0;
            LineRelationPerpendicularToleranceDegrees = 1.0;
            CaliperRow = 120;
            CaliperColumn = 160;
            CaliperPhiDegrees = 0;
            CaliperLength1 = 60;
            CaliperLength2 = 12;
            CaliperSigma = 1.0;
            CaliperThreshold = 20;
            CaliperTransition = VmCaliperTransition.All;
            CaliperSelect = VmCaliperSelect.All;
            LinearCaliperStartRow = 120;
            LinearCaliperStartColumn = 80;
            LinearCaliperEndRow = 120;
            LinearCaliperEndColumn = 240;
            LinearCaliperCount = 5;
            LinearCaliperLength1 = 60;
            LinearCaliperLength2 = 8;
            LinearCaliperSigma = 1.0;
            LinearCaliperThreshold = 20;
            LinearCaliperTransition = VmCaliperTransition.All;
            LinearCaliperSelect = VmCaliperSelect.All;
            CircularCaliperCenterRow = 120;
            CircularCaliperCenterColumn = 160;
            CircularCaliperSearchRadius = 80;
            CircularCaliperStartAngleDegrees = 0;
            CircularCaliperEndAngleDegrees = 360;
            CircularCaliperCount = 16;
            CircularCaliperLength1 = 30;
            CircularCaliperLength2 = 4;
            CircularCaliperSigma = 1.0;
            CircularCaliperThreshold = 20;
            CircularCaliperTransition = VmCaliperTransition.All;
            CircularCaliperSelect = VmCaliperSelect.All;
            RingCenterRow = 120;
            RingCenterColumn = 160;
            RingSearchRadius = 80;
            RingStartAngleDegrees = 0;
            RingEndAngleDegrees = 360;
            RingCaliperCount = 24;
            RingCaliperLength1 = 30;
            RingCaliperLength2 = 4;
            RingCaliperSigma = 1.0;
            RingCaliperThreshold = 20;
            RingCaliperTransition = VmCaliperTransition.Positive;
            RingCaliperSelect = VmCaliperSelect.All;
            PolarCenterRow = 120;
            PolarCenterColumn = 160;
            PolarStartAngleDegrees = 0;
            PolarEndAngleDegrees = 360;
            PolarInnerRadius = 50;
            PolarOuterRadius = 100;
            PolarOutputWidth = 512;
            PolarOutputHeight = 128;
            PolarInterpolation = VmPolarInterpolation.Bilinear;
            RoiImageExtractionMode = VmRoiImageExtractionMode.ReduceDomain;
            ImageResizeWidth = 640;
            ImageResizeHeight = 480;
            ImageResizeInterpolation = VmImageResizeInterpolation.Bilinear;
            ImageMirrorMode = VmImageMirrorMode.Row;
            ImageRotateAngleDegrees = 90;
            ImageRotateInterpolation = VmImageResizeInterpolation.Constant;
            ImageAffineScaleRow = 1;
            ImageAffineScaleColumn = 1;
            ImageAffineInterpolation = VmImageResizeInterpolation.Bilinear;
            ImageProjectiveSourceRow1 = 0;
            ImageProjectiveSourceColumn1 = 0;
            ImageProjectiveSourceRow2 = 0;
            ImageProjectiveSourceColumn2 = 639;
            ImageProjectiveSourceRow3 = 479;
            ImageProjectiveSourceColumn3 = 639;
            ImageProjectiveSourceRow4 = 479;
            ImageProjectiveSourceColumn4 = 0;
            ImageProjectiveTargetRow1 = 0;
            ImageProjectiveTargetColumn1 = 0;
            ImageProjectiveTargetRow2 = 0;
            ImageProjectiveTargetColumn2 = 639;
            ImageProjectiveTargetRow3 = 479;
            ImageProjectiveTargetColumn3 = 639;
            ImageProjectiveTargetRow4 = 479;
            ImageProjectiveTargetColumn4 = 0;
            ImageProjectiveInterpolation = VmProjectiveInterpolation.Bilinear;
            ImageProjectiveOutputMode = VmProjectiveOutputMode.KeepInputSize;
            ImageProjectiveOutputWidth = 640;
            ImageProjectiveOutputHeight = 480;
            SubpixelEdgeFilter = VmSubpixelEdgeFilter.Canny;
            SubpixelEdgeAlpha = 1.0;
            SubpixelEdgeLow = 20;
            SubpixelEdgeHigh = 40;
            SubpixelEdgeColorPolicy = VmSubpixelEdgeColorPolicy.AutoGray;
            XldAdjacentMaxDistAbs = 10.0;
            XldAdjacentMaxDistRel = 1.0;
            XldAdjacentAttributeMode = VmXldAdjacentAttributeMode.AttrKeep;
            XldSmoothNumRegrPoints = 5;
            XldAffineScaleRow = 1.0;
            XldAffineScaleColumn = 1.0;
            XldAffineCenterMode = VmXldAffineCenterMode.InputBounds;
            XldAffineCoordinatePolicy = VmXldAffineCoordinatePolicy.StandardPixelCenter;
            XldAffineTransformOrder = VmXldAffineTransformOrder.ScaleSlantRotateTranslate;
            SetDefaultXldProjectivePoints();
            XldProjectiveCoordinatePolicy = VmXldAffineCoordinatePolicy.StandardPixelCenter;
            XldCropRow1 = 40;
            XldCropColumn1 = 60;
            XldCropRow2 = 440;
            XldCropColumn2 = 580;
            XldCropCloseContours = true;
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
        [DataMember(Order = 67, EmitDefaultValue = false)] public double LineRelationParallelToleranceDegrees { get; set; }
        [DataMember(Order = 68, EmitDefaultValue = false)] public double LineRelationCoincidentDistance { get; set; }
        [DataMember(Order = 69, EmitDefaultValue = false)] public double LineRelationPerpendicularToleranceDegrees { get; set; }
        [DataMember(Order = 70, EmitDefaultValue = false)] public double CaliperRow { get; set; }
        [DataMember(Order = 71, EmitDefaultValue = false)] public double CaliperColumn { get; set; }
        [DataMember(Order = 72, EmitDefaultValue = false)] public double CaliperPhiDegrees { get; set; }
        [DataMember(Order = 73, EmitDefaultValue = false)] public double CaliperLength1 { get; set; }
        [DataMember(Order = 74, EmitDefaultValue = false)] public double CaliperLength2 { get; set; }
        [DataMember(Order = 75, EmitDefaultValue = false)] public double CaliperSigma { get; set; }
        [DataMember(Order = 76, EmitDefaultValue = false)] public double CaliperThreshold { get; set; }
        [DataMember(Order = 77, EmitDefaultValue = false)] public string CaliperTransition { get; set; }
        [DataMember(Order = 78, EmitDefaultValue = false)] public string CaliperSelect { get; set; }
        [DataMember(Order = 79, EmitDefaultValue = false)] public double LinearCaliperStartRow { get; set; }
        [DataMember(Order = 80, EmitDefaultValue = false)] public double LinearCaliperStartColumn { get; set; }
        [DataMember(Order = 81, EmitDefaultValue = false)] public double LinearCaliperEndRow { get; set; }
        [DataMember(Order = 82, EmitDefaultValue = false)] public double LinearCaliperEndColumn { get; set; }
        [DataMember(Order = 83, EmitDefaultValue = false)] public int LinearCaliperCount { get; set; }
        [DataMember(Order = 84, EmitDefaultValue = false)] public double LinearCaliperLength1 { get; set; }
        [DataMember(Order = 85, EmitDefaultValue = false)] public double LinearCaliperLength2 { get; set; }
        [DataMember(Order = 86, EmitDefaultValue = false)] public double LinearCaliperSigma { get; set; }
        [DataMember(Order = 87, EmitDefaultValue = false)] public double LinearCaliperThreshold { get; set; }
        [DataMember(Order = 88, EmitDefaultValue = false)] public string LinearCaliperTransition { get; set; }
        [DataMember(Order = 89, EmitDefaultValue = false)] public string LinearCaliperSelect { get; set; }
        [DataMember(Order = 90, EmitDefaultValue = false)] public double CircularCaliperCenterRow { get; set; }
        [DataMember(Order = 91, EmitDefaultValue = false)] public double CircularCaliperCenterColumn { get; set; }
        [DataMember(Order = 92, EmitDefaultValue = false)] public double CircularCaliperSearchRadius { get; set; }
        [DataMember(Order = 93, EmitDefaultValue = false)] public double CircularCaliperStartAngleDegrees { get; set; }
        [DataMember(Order = 94, EmitDefaultValue = false)] public double CircularCaliperEndAngleDegrees { get; set; }
        [DataMember(Order = 95, EmitDefaultValue = false)] public int CircularCaliperCount { get; set; }
        [DataMember(Order = 96, EmitDefaultValue = false)] public double CircularCaliperLength1 { get; set; }
        [DataMember(Order = 97, EmitDefaultValue = false)] public double CircularCaliperLength2 { get; set; }
        [DataMember(Order = 98, EmitDefaultValue = false)] public double CircularCaliperSigma { get; set; }
        [DataMember(Order = 99, EmitDefaultValue = false)] public double CircularCaliperThreshold { get; set; }
        [DataMember(Order = 100, EmitDefaultValue = false)] public string CircularCaliperTransition { get; set; }
        [DataMember(Order = 101, EmitDefaultValue = false)] public string CircularCaliperSelect { get; set; }
        [DataMember(Order = 102, EmitDefaultValue = false)] public double RingCenterRow { get; set; }
        [DataMember(Order = 103, EmitDefaultValue = false)] public double RingCenterColumn { get; set; }
        [DataMember(Order = 104, EmitDefaultValue = false)] public double RingSearchRadius { get; set; }
        [DataMember(Order = 105, EmitDefaultValue = false)] public double RingStartAngleDegrees { get; set; }
        [DataMember(Order = 106, EmitDefaultValue = false)] public double RingEndAngleDegrees { get; set; }
        [DataMember(Order = 107, EmitDefaultValue = false)] public int RingCaliperCount { get; set; }
        [DataMember(Order = 108, EmitDefaultValue = false)] public double RingCaliperLength1 { get; set; }
        [DataMember(Order = 109, EmitDefaultValue = false)] public double RingCaliperLength2 { get; set; }
        [DataMember(Order = 110, EmitDefaultValue = false)] public double RingCaliperSigma { get; set; }
        [DataMember(Order = 111, EmitDefaultValue = false)] public double RingCaliperThreshold { get; set; }
        [DataMember(Order = 112, EmitDefaultValue = false)] public string RingCaliperTransition { get; set; }
        [DataMember(Order = 113, EmitDefaultValue = false)] public string RingCaliperSelect { get; set; }
        [DataMember(Order = 114, EmitDefaultValue = false)] public double PolarCenterRow { get; set; }
        [DataMember(Order = 115, EmitDefaultValue = false)] public double PolarCenterColumn { get; set; }
        [DataMember(Order = 116, EmitDefaultValue = false)] public double PolarStartAngleDegrees { get; set; }
        [DataMember(Order = 117, EmitDefaultValue = false)] public double PolarEndAngleDegrees { get; set; }
        [DataMember(Order = 118, EmitDefaultValue = false)] public double PolarInnerRadius { get; set; }
        [DataMember(Order = 119, EmitDefaultValue = false)] public double PolarOuterRadius { get; set; }
        [DataMember(Order = 120, EmitDefaultValue = false)] public int PolarOutputWidth { get; set; }
        [DataMember(Order = 121, EmitDefaultValue = false)] public int PolarOutputHeight { get; set; }
        [DataMember(Order = 122, EmitDefaultValue = false)] public string PolarInterpolation { get; set; }
        [DataMember(Order = 123, EmitDefaultValue = false)] public string RoiImageExtractionMode { get; set; }
        [DataMember(Order = 124, EmitDefaultValue = false)] public int ImageResizeWidth { get; set; }
        [DataMember(Order = 125, EmitDefaultValue = false)] public int ImageResizeHeight { get; set; }
        [DataMember(Order = 126, EmitDefaultValue = false)] public string ImageResizeInterpolation { get; set; }
        [DataMember(Order = 127, EmitDefaultValue = false)] public string ImageMirrorMode { get; set; }
        [DataMember(Order = 128, EmitDefaultValue = false)] public double ImageRotateAngleDegrees { get; set; }
        [DataMember(Order = 129, EmitDefaultValue = false)] public string ImageRotateInterpolation { get; set; }
        [DataMember(Order = 130, EmitDefaultValue = false)] public double ImageAffineTranslateRow { get; set; }
        [DataMember(Order = 131, EmitDefaultValue = false)] public double ImageAffineTranslateColumn { get; set; }
        [DataMember(Order = 132, EmitDefaultValue = false)] public double ImageAffineScaleRow { get; set; }
        [DataMember(Order = 133, EmitDefaultValue = false)] public double ImageAffineScaleColumn { get; set; }
        [DataMember(Order = 134, EmitDefaultValue = false)] public double ImageAffineRotationDegrees { get; set; }
        [DataMember(Order = 135, EmitDefaultValue = false)] public double ImageAffineSlantDegrees { get; set; }
        [DataMember(Order = 136, EmitDefaultValue = false)] public string ImageAffineInterpolation { get; set; }
        [DataMember(Order = 137, EmitDefaultValue = false)] public bool ImageAffineAdaptImageSize { get; set; }
        [DataMember(Order = 138, EmitDefaultValue = false)] public double ImageProjectiveSourceRow1 { get; set; }
        [DataMember(Order = 139, EmitDefaultValue = false)] public double ImageProjectiveSourceColumn1 { get; set; }
        [DataMember(Order = 140, EmitDefaultValue = false)] public double ImageProjectiveSourceRow2 { get; set; }
        [DataMember(Order = 141, EmitDefaultValue = false)] public double ImageProjectiveSourceColumn2 { get; set; }
        [DataMember(Order = 142, EmitDefaultValue = false)] public double ImageProjectiveSourceRow3 { get; set; }
        [DataMember(Order = 143, EmitDefaultValue = false)] public double ImageProjectiveSourceColumn3 { get; set; }
        [DataMember(Order = 144, EmitDefaultValue = false)] public double ImageProjectiveSourceRow4 { get; set; }
        [DataMember(Order = 145, EmitDefaultValue = false)] public double ImageProjectiveSourceColumn4 { get; set; }
        [DataMember(Order = 146, EmitDefaultValue = false)] public double ImageProjectiveTargetRow1 { get; set; }
        [DataMember(Order = 147, EmitDefaultValue = false)] public double ImageProjectiveTargetColumn1 { get; set; }
        [DataMember(Order = 148, EmitDefaultValue = false)] public double ImageProjectiveTargetRow2 { get; set; }
        [DataMember(Order = 149, EmitDefaultValue = false)] public double ImageProjectiveTargetColumn2 { get; set; }
        [DataMember(Order = 150, EmitDefaultValue = false)] public double ImageProjectiveTargetRow3 { get; set; }
        [DataMember(Order = 151, EmitDefaultValue = false)] public double ImageProjectiveTargetColumn3 { get; set; }
        [DataMember(Order = 152, EmitDefaultValue = false)] public double ImageProjectiveTargetRow4 { get; set; }
        [DataMember(Order = 153, EmitDefaultValue = false)] public double ImageProjectiveTargetColumn4 { get; set; }
        [DataMember(Order = 154, EmitDefaultValue = false)] public string ImageProjectiveInterpolation { get; set; }
        [DataMember(Order = 155, EmitDefaultValue = false)] public string ImageProjectiveOutputMode { get; set; }
        [DataMember(Order = 156, EmitDefaultValue = false)] public int ImageProjectiveOutputWidth { get; set; }
        [DataMember(Order = 157, EmitDefaultValue = false)] public int ImageProjectiveOutputHeight { get; set; }
        [DataMember(Order = 158, EmitDefaultValue = false)] public bool ImageProjectiveTransformDomain { get; set; }
        [DataMember(Order = 159, EmitDefaultValue = false)] public string SubpixelEdgeFilter { get; set; }
        [DataMember(Order = 160, EmitDefaultValue = false)] public double SubpixelEdgeAlpha { get; set; }
        [DataMember(Order = 161, EmitDefaultValue = false)] public int SubpixelEdgeLow { get; set; }
        [DataMember(Order = 162, EmitDefaultValue = false)] public int SubpixelEdgeHigh { get; set; }
        [DataMember(Order = 163, EmitDefaultValue = false)] public string SubpixelEdgeColorPolicy { get; set; }
        [DataMember(Order = 164, EmitDefaultValue = false)] public double XldAdjacentMaxDistAbs { get; set; }
        [DataMember(Order = 165, EmitDefaultValue = false)] public double XldAdjacentMaxDistRel { get; set; }
        [DataMember(Order = 166, EmitDefaultValue = false)] public string XldAdjacentAttributeMode { get; set; }
        [DataMember(Order = 167, EmitDefaultValue = false)] public int XldSmoothNumRegrPoints { get; set; }
        [DataMember(Order = 168, EmitDefaultValue = false)] public double XldAffineTranslateRow { get; set; }
        [DataMember(Order = 169, EmitDefaultValue = false)] public double XldAffineTranslateColumn { get; set; }
        [DataMember(Order = 170, EmitDefaultValue = false)] public double XldAffineScaleRow { get; set; }
        [DataMember(Order = 171, EmitDefaultValue = false)] public double XldAffineScaleColumn { get; set; }
        [DataMember(Order = 172, EmitDefaultValue = false)] public double XldAffineRotationDegrees { get; set; }
        [DataMember(Order = 173, EmitDefaultValue = false)] public double XldAffineSlantDegrees { get; set; }
        [DataMember(Order = 174, EmitDefaultValue = false)] public string XldAffineCenterMode { get; set; }
        [DataMember(Order = 175, EmitDefaultValue = false)] public double XldAffineCenterRow { get; set; }
        [DataMember(Order = 176, EmitDefaultValue = false)] public double XldAffineCenterColumn { get; set; }
        [DataMember(Order = 177, EmitDefaultValue = false)] public string XldAffineCoordinatePolicy { get; set; }
        [DataMember(Order = 178, EmitDefaultValue = false)] public string XldAffineTransformOrder { get; set; }
        [DataMember(Order = 179, EmitDefaultValue = false)] public double XldProjectiveSourceRow1 { get; set; }
        [DataMember(Order = 180, EmitDefaultValue = false)] public double XldProjectiveSourceColumn1 { get; set; }
        [DataMember(Order = 181, EmitDefaultValue = false)] public double XldProjectiveSourceRow2 { get; set; }
        [DataMember(Order = 182, EmitDefaultValue = false)] public double XldProjectiveSourceColumn2 { get; set; }
        [DataMember(Order = 183, EmitDefaultValue = false)] public double XldProjectiveSourceRow3 { get; set; }
        [DataMember(Order = 184, EmitDefaultValue = false)] public double XldProjectiveSourceColumn3 { get; set; }
        [DataMember(Order = 185, EmitDefaultValue = false)] public double XldProjectiveSourceRow4 { get; set; }
        [DataMember(Order = 186, EmitDefaultValue = false)] public double XldProjectiveSourceColumn4 { get; set; }
        [DataMember(Order = 187, EmitDefaultValue = false)] public double XldProjectiveTargetRow1 { get; set; }
        [DataMember(Order = 188, EmitDefaultValue = false)] public double XldProjectiveTargetColumn1 { get; set; }
        [DataMember(Order = 189, EmitDefaultValue = false)] public double XldProjectiveTargetRow2 { get; set; }
        [DataMember(Order = 190, EmitDefaultValue = false)] public double XldProjectiveTargetColumn2 { get; set; }
        [DataMember(Order = 191, EmitDefaultValue = false)] public double XldProjectiveTargetRow3 { get; set; }
        [DataMember(Order = 192, EmitDefaultValue = false)] public double XldProjectiveTargetColumn3 { get; set; }
        [DataMember(Order = 193, EmitDefaultValue = false)] public double XldProjectiveTargetRow4 { get; set; }
        [DataMember(Order = 194, EmitDefaultValue = false)] public double XldProjectiveTargetColumn4 { get; set; }
        [DataMember(Order = 195, EmitDefaultValue = false)] public string XldProjectiveCoordinatePolicy { get; set; }
        [DataMember(Order = 196, EmitDefaultValue = false)] public double XldCropRow1 { get; set; }
        [DataMember(Order = 197, EmitDefaultValue = false)] public double XldCropColumn1 { get; set; }
        [DataMember(Order = 198, EmitDefaultValue = false)] public double XldCropRow2 { get; set; }
        [DataMember(Order = 199, EmitDefaultValue = false)] public double XldCropColumn2 { get; set; }
        [DataMember(Order = 200, EmitDefaultValue = false)] public bool XldCropCloseContours { get; set; }
        [DataMember(Order = 201, EmitDefaultValue = false)] public double XldLengthShortMaximum { get; set; }
        [DataMember(Order = 202, EmitDefaultValue = false)] public double XldLengthMediumMaximum { get; set; }
        [DataMember(Order = 203, EmitDefaultValue = false)] public string XldLengthSortDirection { get; set; }
        [DataMember(Order = 204, EmitDefaultValue = false)] public string XldLengthSelectionMode { get; set; }
        [DataMember(Order = 205, EmitDefaultValue = false)] public int XldLengthStartRank { get; set; }
        [DataMember(Order = 206, EmitDefaultValue = false)] public int XldLengthEndRank { get; set; }

        private void SetDefaultXldProjectivePoints()
        {
            XldProjectiveSourceRow1 = 0;
            XldProjectiveSourceColumn1 = 0;
            XldProjectiveSourceRow2 = 0;
            XldProjectiveSourceColumn2 = 639;
            XldProjectiveSourceRow3 = 479;
            XldProjectiveSourceColumn3 = 639;
            XldProjectiveSourceRow4 = 479;
            XldProjectiveSourceColumn4 = 0;
            XldProjectiveTargetRow1 = 0;
            XldProjectiveTargetColumn1 = 0;
            XldProjectiveTargetRow2 = 0;
            XldProjectiveTargetColumn2 = 639;
            XldProjectiveTargetRow3 = 479;
            XldProjectiveTargetColumn3 = 639;
            XldProjectiveTargetRow4 = 479;
            XldProjectiveTargetColumn4 = 0;
        }

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
            if (LineRelationParallelToleranceDegrees == 0 &&
                LineRelationCoincidentDistance == 0 &&
                LineRelationPerpendicularToleranceDegrees == 0)
            {
                LineRelationParallelToleranceDegrees = 1.0;
                LineRelationCoincidentDistance = 1.0;
                LineRelationPerpendicularToleranceDegrees = 1.0;
            }
            if (CaliperLength1 == 0 && CaliperLength2 == 0 && CaliperSigma == 0 && CaliperThreshold == 0 &&
                string.IsNullOrWhiteSpace(CaliperTransition) && string.IsNullOrWhiteSpace(CaliperSelect))
            {
                CaliperRow = 120;
                CaliperColumn = 160;
                CaliperLength1 = 60;
                CaliperLength2 = 12;
                CaliperSigma = 1.0;
                CaliperThreshold = 20;
            }
            CaliperTransition = VmCaliperTransition.Normalize(CaliperTransition);
            CaliperSelect = VmCaliperSelect.Normalize(CaliperSelect);
            if (LinearCaliperCount == 0 && LinearCaliperLength1 == 0 && LinearCaliperLength2 == 0 &&
                LinearCaliperSigma == 0 && LinearCaliperThreshold == 0 &&
                string.IsNullOrWhiteSpace(LinearCaliperTransition) && string.IsNullOrWhiteSpace(LinearCaliperSelect))
            {
                LinearCaliperStartRow = 120;
                LinearCaliperStartColumn = 80;
                LinearCaliperEndRow = 120;
                LinearCaliperEndColumn = 240;
                LinearCaliperCount = 5;
                LinearCaliperLength1 = 60;
                LinearCaliperLength2 = 8;
                LinearCaliperSigma = 1.0;
                LinearCaliperThreshold = 20;
            }
            LinearCaliperTransition = VmCaliperTransition.Normalize(LinearCaliperTransition);
            LinearCaliperSelect = VmCaliperSelect.Normalize(LinearCaliperSelect);
            if (CircularCaliperCount == 0 && CircularCaliperSearchRadius == 0 &&
                CircularCaliperLength1 == 0 && CircularCaliperLength2 == 0 &&
                CircularCaliperSigma == 0 && CircularCaliperThreshold == 0 &&
                string.IsNullOrWhiteSpace(CircularCaliperTransition) && string.IsNullOrWhiteSpace(CircularCaliperSelect))
            {
                CircularCaliperCenterRow = 120;
                CircularCaliperCenterColumn = 160;
                CircularCaliperSearchRadius = 80;
                CircularCaliperStartAngleDegrees = 0;
                CircularCaliperEndAngleDegrees = 360;
                CircularCaliperCount = 16;
                CircularCaliperLength1 = 30;
                CircularCaliperLength2 = 4;
                CircularCaliperSigma = 1.0;
                CircularCaliperThreshold = 20;
            }
            CircularCaliperTransition = VmCaliperTransition.Normalize(CircularCaliperTransition);
            CircularCaliperSelect = VmCaliperSelect.Normalize(CircularCaliperSelect);
            if (RingCaliperCount == 0 && RingSearchRadius == 0 &&
                RingCaliperLength1 == 0 && RingCaliperLength2 == 0 &&
                RingCaliperSigma == 0 && RingCaliperThreshold == 0 &&
                string.IsNullOrWhiteSpace(RingCaliperTransition) && string.IsNullOrWhiteSpace(RingCaliperSelect))
            {
                RingCenterRow = 120;
                RingCenterColumn = 160;
                RingSearchRadius = 80;
                RingStartAngleDegrees = 0;
                RingEndAngleDegrees = 360;
                RingCaliperCount = 24;
                RingCaliperLength1 = 30;
                RingCaliperLength2 = 4;
                RingCaliperSigma = 1.0;
                RingCaliperThreshold = 20;
                RingCaliperTransition = VmCaliperTransition.Positive;
                RingCaliperSelect = VmCaliperSelect.All;
            }
            RingCaliperTransition = VmCaliperTransition.Normalize(RingCaliperTransition);
            RingCaliperSelect = VmCaliperSelect.Normalize(RingCaliperSelect);
            if (PolarOutputWidth == 0 && PolarOutputHeight == 0 && PolarInnerRadius == 0 && PolarOuterRadius == 0 &&
                string.IsNullOrWhiteSpace(PolarInterpolation))
            {
                PolarCenterRow = 120;
                PolarCenterColumn = 160;
                PolarStartAngleDegrees = 0;
                PolarEndAngleDegrees = 360;
                PolarInnerRadius = 50;
                PolarOuterRadius = 100;
                PolarOutputWidth = 512;
                PolarOutputHeight = 128;
            }
            PolarInterpolation = VmPolarInterpolation.Normalize(PolarInterpolation);
            RoiImageExtractionMode = VmRoiImageExtractionMode.Normalize(RoiImageExtractionMode);
            if (ImageResizeWidth == 0 && ImageResizeHeight == 0 && string.IsNullOrWhiteSpace(ImageResizeInterpolation))
            {
                ImageResizeWidth = 640;
                ImageResizeHeight = 480;
            }
            ImageResizeInterpolation = VmImageResizeInterpolation.Normalize(ImageResizeInterpolation);
            ImageMirrorMode = VmImageMirrorMode.Normalize(ImageMirrorMode);
            if (string.IsNullOrWhiteSpace(ImageRotateInterpolation))
            {
                ImageRotateAngleDegrees = 90;
                ImageRotateInterpolation = VmImageResizeInterpolation.Constant;
            }
            ImageRotateInterpolation = VmImageResizeInterpolation.Normalize(ImageRotateInterpolation);
            if (string.IsNullOrWhiteSpace(ImageAffineInterpolation) && ImageAffineScaleRow == 0 && ImageAffineScaleColumn == 0)
            {
                ImageAffineScaleRow = 1;
                ImageAffineScaleColumn = 1;
                ImageAffineInterpolation = VmImageResizeInterpolation.Bilinear;
            }
            ImageAffineInterpolation = VmImageResizeInterpolation.Normalize(ImageAffineInterpolation);
            if (string.IsNullOrWhiteSpace(ImageProjectiveInterpolation) && string.IsNullOrWhiteSpace(ImageProjectiveOutputMode) &&
                ImageProjectiveSourceRow1 == 0 && ImageProjectiveSourceColumn1 == 0 &&
                ImageProjectiveSourceRow2 == 0 && ImageProjectiveSourceColumn2 == 0 &&
                ImageProjectiveSourceRow3 == 0 && ImageProjectiveSourceColumn3 == 0 &&
                ImageProjectiveSourceRow4 == 0 && ImageProjectiveSourceColumn4 == 0)
            {
                ImageProjectiveSourceColumn2 = 639;
                ImageProjectiveSourceRow3 = 479;
                ImageProjectiveSourceColumn3 = 639;
                ImageProjectiveSourceRow4 = 479;
                ImageProjectiveTargetColumn2 = 639;
                ImageProjectiveTargetRow3 = 479;
                ImageProjectiveTargetColumn3 = 639;
                ImageProjectiveTargetRow4 = 479;
                ImageProjectiveOutputWidth = 640;
                ImageProjectiveOutputHeight = 480;
            }
            ImageProjectiveInterpolation = VmProjectiveInterpolation.Normalize(ImageProjectiveInterpolation);
            ImageProjectiveOutputMode = VmProjectiveOutputMode.Normalize(ImageProjectiveOutputMode);
            if (ImageProjectiveOutputWidth <= 0) ImageProjectiveOutputWidth = 640;
            if (ImageProjectiveOutputHeight <= 0) ImageProjectiveOutputHeight = 480;
            SubpixelEdgeFilter = VmSubpixelEdgeFilter.Normalize(SubpixelEdgeFilter);
            SubpixelEdgeColorPolicy = VmSubpixelEdgeColorPolicy.Normalize(SubpixelEdgeColorPolicy);
            if (string.IsNullOrWhiteSpace(XldAdjacentAttributeMode) && XldAdjacentMaxDistAbs == 0 && XldAdjacentMaxDistRel == 0)
            {
                XldAdjacentMaxDistAbs = 10.0;
                XldAdjacentMaxDistRel = 1.0;
            }
            XldAdjacentAttributeMode = VmXldAdjacentAttributeMode.Normalize(XldAdjacentAttributeMode);
            if (XldSmoothNumRegrPoints == 0)
            {
                XldSmoothNumRegrPoints = 5;
            }
            if (XldAffineScaleRow == 0 && XldAffineScaleColumn == 0 &&
                string.IsNullOrWhiteSpace(XldAffineCenterMode) &&
                string.IsNullOrWhiteSpace(XldAffineCoordinatePolicy) &&
                string.IsNullOrWhiteSpace(XldAffineTransformOrder))
            {
                XldAffineScaleRow = 1.0;
                XldAffineScaleColumn = 1.0;
            }
            XldAffineCenterMode = VmXldAffineCenterMode.Normalize(XldAffineCenterMode);
            XldAffineCoordinatePolicy = VmXldAffineCoordinatePolicy.Normalize(XldAffineCoordinatePolicy);
            XldAffineTransformOrder = VmXldAffineTransformOrder.Normalize(XldAffineTransformOrder);
            if (string.IsNullOrWhiteSpace(XldProjectiveCoordinatePolicy) &&
                XldProjectiveSourceRow1 == 0 && XldProjectiveSourceColumn1 == 0 &&
                XldProjectiveSourceRow2 == 0 && XldProjectiveSourceColumn2 == 0 &&
                XldProjectiveSourceRow3 == 0 && XldProjectiveSourceColumn3 == 0 &&
                XldProjectiveSourceRow4 == 0 && XldProjectiveSourceColumn4 == 0 &&
                XldProjectiveTargetRow1 == 0 && XldProjectiveTargetColumn1 == 0 &&
                XldProjectiveTargetRow2 == 0 && XldProjectiveTargetColumn2 == 0 &&
                XldProjectiveTargetRow3 == 0 && XldProjectiveTargetColumn3 == 0 &&
                XldProjectiveTargetRow4 == 0 && XldProjectiveTargetColumn4 == 0)
            {
                SetDefaultXldProjectivePoints();
            }
            XldProjectiveCoordinatePolicy = VmXldAffineCoordinatePolicy.Normalize(XldProjectiveCoordinatePolicy);
            if (XldCropRow1 == 0 && XldCropColumn1 == 0 && XldCropRow2 == 0 && XldCropColumn2 == 0)
            {
                XldCropRow1 = 40;
                XldCropColumn1 = 60;
                XldCropRow2 = 440;
                XldCropColumn2 = 580;
                XldCropCloseContours = true;
            }
            if (XldLengthShortMaximum == 0 && XldLengthMediumMaximum == 0)
            {
                XldLengthShortMaximum = 50;
                XldLengthMediumMaximum = 150;
            }
            XldLengthSortDirection = VmXldLengthSortDirection.Normalize(XldLengthSortDirection);
            XldLengthSelectionMode = VmXldLengthSelectionMode.Normalize(XldLengthSelectionMode);
            if (XldLengthStartRank <= 0) XldLengthStartRank = 1;
            if (XldLengthEndRank <= 0) XldLengthEndRank = 1;
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

            if (kind == VmToolKind.PolarImageUnwrap)
            {
                if (!IsFinite(PolarCenterRow) || !IsFinite(PolarCenterColumn) || PolarCenterRow < 0 || PolarCenterColumn < 0)
                {
                    return "极坐标展开中心 Row/Column 必须是大于等于 0 的有限像素坐标。";
                }

                if (!IsFinite(PolarStartAngleDegrees) || !IsFinite(PolarEndAngleDegrees) ||
                    Math.Abs(PolarStartAngleDegrees) > 36000 || Math.Abs(PolarEndAngleDegrees) > 36000)
                {
                    return "极坐标展开起止角必须是 -36000° 到 36000° 之间的有限数值。";
                }

                double span = Math.Abs(PolarEndAngleDegrees - PolarStartAngleDegrees);
                if (span < 1 || span > 360.000001)
                {
                    return "极坐标展开角度覆盖必须在 1° 到 360° 之间；终止角小于起始角表示反向展开。";
                }

                if (!IsFinite(PolarInnerRadius) || !IsFinite(PolarOuterRadius) ||
                    PolarInnerRadius < 0 || PolarOuterRadius <= PolarInnerRadius || PolarOuterRadius > 1000000000)
                {
                    return "极坐标展开半径必须满足 0 ≤ 内半径 < 外半径 ≤ 1000000000。";
                }

                if (PolarOutputWidth < 2 || PolarOutputWidth > 8192 || PolarOutputHeight < 2 || PolarOutputHeight > 8192 ||
                    (long)PolarOutputWidth * PolarOutputHeight > 16777216L)
                {
                    return "极坐标展开输出宽高必须在 2 到 8192 之间，且总像素数不得超过 16777216。";
                }

                if (!VmPolarInterpolation.IsSupported(PolarInterpolation))
                {
                    return "极坐标展开插值方式无效。";
                }
            }

            if (kind == VmToolKind.RoiImageExtraction && !VmRoiImageExtractionMode.IsSupported(RoiImageExtractionMode))
            {
                return "ROI 图像提取模式无效。";
            }

            if (kind == VmToolKind.ImageResize)
            {
                if (ImageResizeWidth < 2 || ImageResizeWidth > 16384 || ImageResizeHeight < 2 || ImageResizeHeight > 16384 ||
                    (long)ImageResizeWidth * ImageResizeHeight > 67108864L)
                {
                    return "图像尺寸缩放目标宽高必须在 2 到 16384 之间，且总像素数不得超过 67108864。";
                }

                if (!VmImageResizeInterpolation.IsSupported(ImageResizeInterpolation))
                {
                    return "图像尺寸缩放插值方式无效。";
                }
            }

            if (kind == VmToolKind.ImageMirror && !VmImageMirrorMode.IsSupported(ImageMirrorMode))
            {
                return "图像镜像方向无效。";
            }

            if (kind == VmToolKind.ImageRotate)
            {
                if (!IsFinite(ImageRotateAngleDegrees) || ImageRotateAngleDegrees < 0 || ImageRotateAngleDegrees > 360)
                {
                    return "图像旋转角度必须是 0 到 360 度之间的有限数值。";
                }

                if (!VmImageResizeInterpolation.IsSupported(ImageRotateInterpolation))
                {
                    return "图像旋转插值方式无效。";
                }
            }

            if (kind == VmToolKind.ImageAffineTransform)
            {
                if (!IsFinite(ImageAffineTranslateRow) || !IsFinite(ImageAffineTranslateColumn) ||
                    ImageAffineTranslateRow < -100000 || ImageAffineTranslateRow > 100000 ||
                    ImageAffineTranslateColumn < -100000 || ImageAffineTranslateColumn > 100000)
                {
                    return "仿射平移 Row/Column 必须是 -100000 到 100000 之间的有限数值。";
                }
                if (!IsFinite(ImageAffineScaleRow) || !IsFinite(ImageAffineScaleColumn) ||
                    ImageAffineScaleRow < 0.001 || ImageAffineScaleRow > 100 ||
                    ImageAffineScaleColumn < 0.001 || ImageAffineScaleColumn > 100)
                {
                    return "仿射行列缩放比例必须在 0.001 到 100 之间。";
                }
                if (!IsFinite(ImageAffineRotationDegrees) || ImageAffineRotationDegrees < -360 || ImageAffineRotationDegrees > 360)
                {
                    return "仿射旋转角度必须在 -360 到 360 度之间。";
                }
                if (!IsFinite(ImageAffineSlantDegrees) || ImageAffineSlantDegrees <= -80 || ImageAffineSlantDegrees >= 80)
                {
                    return "仿射行方向斜切角必须严格位于 -80 到 80 度之间。";
                }
                if (!VmImageResizeInterpolation.IsSupported(ImageAffineInterpolation))
                {
                    return "仿射图像变换插值方式无效。";
                }
            }

            if (kind == VmToolKind.ImageProjectiveTransform)
            {
                double[] sourceRows = { ImageProjectiveSourceRow1, ImageProjectiveSourceRow2, ImageProjectiveSourceRow3, ImageProjectiveSourceRow4 };
                double[] sourceColumns = { ImageProjectiveSourceColumn1, ImageProjectiveSourceColumn2, ImageProjectiveSourceColumn3, ImageProjectiveSourceColumn4 };
                double[] targetRows = { ImageProjectiveTargetRow1, ImageProjectiveTargetRow2, ImageProjectiveTargetRow3, ImageProjectiveTargetRow4 };
                double[] targetColumns = { ImageProjectiveTargetColumn1, ImageProjectiveTargetColumn2, ImageProjectiveTargetColumn3, ImageProjectiveTargetColumn4 };
                if (sourceRows.Concat(sourceColumns).Concat(targetRows).Concat(targetColumns).Any(value => !IsFinite(value) || Math.Abs(value) > 1000000000))
                {
                    return "透视校正的源点和目标点必须是绝对值不超过 1000000000 的有限像素坐标。";
                }
                if (sourceRows.Any(value => value < 0) || sourceColumns.Any(value => value < 0))
                {
                    return "透视校正源点必须位于非负图像坐标；执行时还会检查是否落在输入图像内。";
                }
                if (!IsOrderedProjectiveQuadrilateral(sourceRows, sourceColumns, out string sourceReason))
                {
                    return "透视校正源四边形无效：" + sourceReason;
                }
                if (!IsOrderedProjectiveQuadrilateral(targetRows, targetColumns, out string targetReason))
                {
                    return "透视校正目标四边形无效：" + targetReason;
                }
                if (!VmProjectiveInterpolation.IsSupported(ImageProjectiveInterpolation))
                {
                    return "透视校正仅支持 nearest_neighbor 或 bilinear 插值。";
                }
                if (!VmProjectiveOutputMode.IsSupported(ImageProjectiveOutputMode))
                {
                    return "透视校正输出尺寸模式无效。";
                }
                if (VmProjectiveOutputMode.Normalize(ImageProjectiveOutputMode) == VmProjectiveOutputMode.ExplicitSize &&
                    (ImageProjectiveOutputWidth < 2 || ImageProjectiveOutputWidth > 16384 || ImageProjectiveOutputHeight < 2 || ImageProjectiveOutputHeight > 16384 ||
                     (long)ImageProjectiveOutputWidth * ImageProjectiveOutputHeight > 67108864L))
                {
                    return "透视校正显式输出宽高必须在 2 到 16384 之间，且总像素数不得超过 67108864。";
                }
            }

            if (kind == VmToolKind.SubpixelEdgeExtraction)
            {
                if (!VmSubpixelEdgeFilter.IsSupported(SubpixelEdgeFilter))
                {
                    return "亚像素边缘滤波器无效；请选择 HALCON 20.11 edges_sub_pix 支持的滤波器。";
                }
                if (!IsFinite(SubpixelEdgeAlpha) || SubpixelEdgeAlpha <= 0 || SubpixelEdgeAlpha > 100)
                {
                    return "亚像素边缘 Alpha 必须满足 0 < Alpha ≤ 100；Canny 的 Alpha 越大平滑越强，其余递归滤波器相反。";
                }
                if (SubpixelEdgeLow <= 0 || SubpixelEdgeHigh < SubpixelEdgeLow || SubpixelEdgeHigh > 65535)
                {
                    return "亚像素边缘滞后阈值必须满足 0 < Low ≤ High ≤ 65535。";
                }
                if (!VmSubpixelEdgeColorPolicy.IsSupported(SubpixelEdgeColorPolicy))
                {
                    return "亚像素边缘彩色处理策略无效。";
                }
            }

            if (kind == VmToolKind.XldAdjacentContourUnion)
            {
                if (!IsFinite(XldAdjacentMaxDistAbs) || XldAdjacentMaxDistAbs < 0 || XldAdjacentMaxDistAbs > 100000)
                {
                    return "相邻轮廓连接的最大绝对距离必须是 0 到 100000 之间的有限数值。";
                }
                if (!IsFinite(XldAdjacentMaxDistRel) || XldAdjacentMaxDistRel < 0 || XldAdjacentMaxDistRel > 1000)
                {
                    return "相邻轮廓连接的最大相对距离必须是 0 到 1000 之间的有限数值。";
                }
                if (!VmXldAdjacentAttributeMode.IsSupported(XldAdjacentAttributeMode))
                {
                    return "相邻轮廓连接属性策略无效；请选择 attr_keep 或 attr_forget。";
                }
            }

            if (kind == VmToolKind.XldContourSmoothing)
            {
                if (XldSmoothNumRegrPoints < 3 || XldSmoothNumRegrPoints > 999 || XldSmoothNumRegrPoints % 2 == 0)
                {
                    return "XLD 轮廓平滑回归点数必须是 3 到 999 之间的奇数；建议使用 3、5、7 或 9。";
                }
            }

            if (kind == VmToolKind.XldAffineTransform)
            {
                if (!IsFinite(XldAffineTranslateRow) || !IsFinite(XldAffineTranslateColumn) ||
                    Math.Abs(XldAffineTranslateRow) > 1000000000 || Math.Abs(XldAffineTranslateColumn) > 1000000000)
                {
                    return "XLD 仿射平移 Row/Column 必须是绝对值不超过 1000000000 的有限数值。";
                }
                if (!IsFinite(XldAffineScaleRow) || !IsFinite(XldAffineScaleColumn) ||
                    XldAffineScaleRow < 0.001 || XldAffineScaleRow > 1000 ||
                    XldAffineScaleColumn < 0.001 || XldAffineScaleColumn > 1000)
                {
                    return "XLD 仿射 Row/Column 缩放必须是 0.001 到 1000 之间的有限正数。";
                }
                if (!IsFinite(XldAffineRotationDegrees) || Math.Abs(XldAffineRotationDegrees) > 3600)
                {
                    return "XLD 仿射旋转角必须是 -3600° 到 3600° 之间的有限数值。";
                }
                if (!IsFinite(XldAffineSlantDegrees) || Math.Abs(XldAffineSlantDegrees) >= 80)
                {
                    return "XLD 仿射行斜切角必须是 -80° 到 80° 之间的有限数值（不含端点）。";
                }
                if (!VmXldAffineCenterMode.IsSupported(XldAffineCenterMode)) return "XLD 仿射中心模式无效。";
                if (VmXldAffineCenterMode.Normalize(XldAffineCenterMode) == VmXldAffineCenterMode.Manual &&
                    (!IsFinite(XldAffineCenterRow) || !IsFinite(XldAffineCenterColumn) ||
                     Math.Abs(XldAffineCenterRow) > 1000000000 || Math.Abs(XldAffineCenterColumn) > 1000000000))
                {
                    return "XLD 仿射手动中心 Row/Column 必须是绝对值不超过 1000000000 的有限数值。";
                }
                if (!VmXldAffineCoordinatePolicy.IsSupported(XldAffineCoordinatePolicy)) return "XLD 仿射坐标策略无效。";
                if (!VmXldAffineTransformOrder.IsSupported(XldAffineTransformOrder)) return "XLD 仿射组合顺序无效。";
            }

            if (kind == VmToolKind.XldProjectiveTransform)
            {
                double[] sourceRows = { XldProjectiveSourceRow1, XldProjectiveSourceRow2, XldProjectiveSourceRow3, XldProjectiveSourceRow4 };
                double[] sourceColumns = { XldProjectiveSourceColumn1, XldProjectiveSourceColumn2, XldProjectiveSourceColumn3, XldProjectiveSourceColumn4 };
                double[] targetRows = { XldProjectiveTargetRow1, XldProjectiveTargetRow2, XldProjectiveTargetRow3, XldProjectiveTargetRow4 };
                double[] targetColumns = { XldProjectiveTargetColumn1, XldProjectiveTargetColumn2, XldProjectiveTargetColumn3, XldProjectiveTargetColumn4 };
                if (sourceRows.Concat(sourceColumns).Concat(targetRows).Concat(targetColumns).Any(value => !IsFinite(value) || Math.Abs(value) > 1000000000))
                {
                    return "XLD 透视变换的源点和目标点必须是绝对值不超过 1000000000 的有限 Row/Column 坐标。";
                }
                string sourceReason;
                if (!IsOrderedProjectiveQuadrilateral(sourceRows, sourceColumns, out sourceReason))
                {
                    return "XLD 透视变换源四边形无效：" + sourceReason;
                }
                string targetReason;
                if (!IsOrderedProjectiveQuadrilateral(targetRows, targetColumns, out targetReason))
                {
                    return "XLD 透视变换目标四边形无效：" + targetReason;
                }
                if (!VmXldAffineCoordinatePolicy.IsSupported(XldProjectiveCoordinatePolicy)) return "XLD 透视变换坐标策略无效。";
            }

            if (kind == VmToolKind.XldContourCrop)
            {
                if (!IsFinite(XldCropRow1) || !IsFinite(XldCropColumn1) || !IsFinite(XldCropRow2) || !IsFinite(XldCropColumn2) ||
                    Math.Abs(XldCropRow1) > 1000000000 || Math.Abs(XldCropColumn1) > 1000000000 ||
                    Math.Abs(XldCropRow2) > 1000000000 || Math.Abs(XldCropColumn2) > 1000000000)
                {
                    return "XLD 裁剪矩形必须使用绝对值不超过 1000000000 的有限 Row/Column 坐标。";
                }
                if (XldCropRow2 - XldCropRow1 < 1 || XldCropColumn2 - XldCropColumn1 < 1)
                {
                    return "XLD 裁剪矩形要求 Row2 至少比 Row1 大 1，Column2 至少比 Column1 大 1。";
                }
            }

            if (kind == VmToolKind.XldLengthClassification)
            {
                if (!IsFinite(XldLengthShortMaximum) || !IsFinite(XldLengthMediumMaximum) ||
                    XldLengthShortMaximum < 0 || XldLengthMediumMaximum > 1000000000 ||
                    XldLengthMediumMaximum <= XldLengthShortMaximum)
                {
                    return "XLD 长度分级阈值必须是有限数值，并满足 0 ≤ 短轮廓上限 < 中轮廓上限 ≤ 1000000000。";
                }
            }

            if (kind == VmToolKind.XldLengthSortSelection)
            {
                if (XldLengthStartRank < 1 || XldLengthStartRank > 1000000 || XldLengthEndRank < 1 || XldLengthEndRank > 1000000)
                {
                    return "XLD 长度排序排名必须是 1 到 1000000 的整数。";
                }
                if (VmXldLengthSelectionMode.Normalize(XldLengthSelectionMode) == VmXldLengthSelectionMode.RankRange && XldLengthEndRank < XldLengthStartRank)
                {
                    return "XLD 长度排序排名区间必须满足结束排名 ≥ 起始排名。";
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

            if (kind == VmToolKind.XldLineFitting || kind == VmToolKind.XldLineRelationMeasurement)
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

            if (kind == VmToolKind.XldLineRelationMeasurement)
            {
                if (!IsFinite(LineRelationParallelToleranceDegrees) ||
                    LineRelationParallelToleranceDegrees < 0.01 ||
                    LineRelationParallelToleranceDegrees > 15)
                {
                    return "双直线平行角容差必须在 0.01° 到 15° 之间。";
                }

                if (!IsFinite(LineRelationPerpendicularToleranceDegrees) ||
                    LineRelationPerpendicularToleranceDegrees < 0.01 ||
                    LineRelationPerpendicularToleranceDegrees > 15)
                {
                    return "双直线垂直角容差必须在 0.01° 到 15° 之间。";
                }

                if (!IsFinite(LineRelationCoincidentDistance) ||
                    LineRelationCoincidentDistance < 0 ||
                    LineRelationCoincidentDistance > 100000)
                {
                    return "双直线重合距离容差必须在 0 到 100000 像素之间。";
                }
            }

            if (kind == VmToolKind.CaliperEdgePairMeasurement)
            {
                if (!IsFinite(CaliperRow) || !IsFinite(CaliperColumn) ||
                    CaliperRow < 0 || CaliperColumn < 0 ||
                    CaliperRow > 1000000000 || CaliperColumn > 1000000000)
                {
                    return "卡尺中心 Row/Column 必须是大于等于 0 的有限像素坐标。";
                }

                if (!IsFinite(CaliperPhiDegrees) || CaliperPhiDegrees < -180 || CaliperPhiDegrees > 180)
                {
                    return "卡尺方向角必须在 -180° 到 180° 之间。";
                }

                if (!IsFinite(CaliperLength1) || !IsFinite(CaliperLength2) ||
                    CaliperLength1 < 1 || CaliperLength1 > 100000 ||
                    CaliperLength2 < 1 || CaliperLength2 > 100000)
                {
                    return "卡尺 Length1/Length2 半长必须在 1 到 100000 像素之间。";
                }

                if (!IsFinite(CaliperSigma) || CaliperSigma < 0.4 || CaliperSigma > 100 ||
                    CaliperSigma > CaliperLength1 * 0.5)
                {
                    return "卡尺 Sigma 必须在 0.4 到 100 之间，且不得大于 Length1 的一半。";
                }

                if (!IsFinite(CaliperThreshold) || CaliperThreshold < 1 || CaliperThreshold > 1000000)
                {
                    return "卡尺边缘阈值必须是 1 到 1000000 之间的有限数值。";
                }

                if (!VmCaliperTransition.IsSupported(CaliperTransition))
                {
                    return "卡尺边缘极性无效。";
                }

                if (!VmCaliperSelect.IsSupported(CaliperSelect))
                {
                    return "卡尺边缘对选择方式无效。";
                }
            }

            if (kind == VmToolKind.LinearCaliperArrayMeasurement)
            {
                if (!IsFinite(LinearCaliperStartRow) || !IsFinite(LinearCaliperStartColumn) ||
                    !IsFinite(LinearCaliperEndRow) || !IsFinite(LinearCaliperEndColumn) ||
                    LinearCaliperStartRow < 0 || LinearCaliperStartColumn < 0 ||
                    LinearCaliperEndRow < 0 || LinearCaliperEndColumn < 0 ||
                    LinearCaliperStartRow > 1000000000 || LinearCaliperStartColumn > 1000000000 ||
                    LinearCaliperEndRow > 1000000000 || LinearCaliperEndColumn > 1000000000)
                {
                    return "线性多卡尺阵列起点/终点必须是大于等于 0 的有限像素坐标。";
                }

                double arrayDeltaRow = LinearCaliperEndRow - LinearCaliperStartRow;
                double arrayDeltaColumn = LinearCaliperEndColumn - LinearCaliperStartColumn;
                if (arrayDeltaRow * arrayDeltaRow + arrayDeltaColumn * arrayDeltaColumn < 1.0)
                {
                    return "线性多卡尺阵列起点与终点距离必须至少为 1 像素。";
                }

                if (LinearCaliperCount < 2 || LinearCaliperCount > 200)
                {
                    return "线性多卡尺阵列数量必须在 2 到 200 之间。";
                }

                if (!IsFinite(LinearCaliperLength1) || !IsFinite(LinearCaliperLength2) ||
                    LinearCaliperLength1 < 1 || LinearCaliperLength1 > 100000 ||
                    LinearCaliperLength2 < 1 || LinearCaliperLength2 > 100000)
                {
                    return "线性多卡尺 Length1/Length2 半长必须在 1 到 100000 像素之间。";
                }

                if (!IsFinite(LinearCaliperSigma) || LinearCaliperSigma < 0.4 || LinearCaliperSigma > 100 ||
                    LinearCaliperSigma > LinearCaliperLength1 * 0.5)
                {
                    return "线性多卡尺 Sigma 必须在 0.4 到 100 之间，且不得大于 Length1 的一半。";
                }

                if (!IsFinite(LinearCaliperThreshold) || LinearCaliperThreshold < 1 || LinearCaliperThreshold > 1000000)
                {
                    return "线性多卡尺边缘阈值必须是 1 到 1000000 之间的有限数值。";
                }

                if (!VmCaliperTransition.IsSupported(LinearCaliperTransition))
                {
                    return "线性多卡尺边缘极性无效。";
                }

                if (!VmCaliperSelect.IsSupported(LinearCaliperSelect))
                {
                    return "线性多卡尺边缘对选择方式无效。";
                }
            }

            if (kind == VmToolKind.CircularCaliperCircleFinder)
            {
                if (!IsFinite(CircularCaliperCenterRow) || !IsFinite(CircularCaliperCenterColumn) ||
                    CircularCaliperCenterRow < 0 || CircularCaliperCenterColumn < 0 ||
                    CircularCaliperCenterRow > 1000000000 || CircularCaliperCenterColumn > 1000000000)
                {
                    return "圆周卡尺圆心 Row/Column 必须是大于等于 0 的有限像素坐标。";
                }

                if (!IsFinite(CircularCaliperSearchRadius) ||
                    CircularCaliperSearchRadius < 1 || CircularCaliperSearchRadius > 1000000000)
                {
                    return "圆周卡尺搜索半径必须在 1 到 1000000000 像素之间。";
                }

                if (!IsFinite(CircularCaliperStartAngleDegrees) || !IsFinite(CircularCaliperEndAngleDegrees) ||
                    Math.Abs(CircularCaliperStartAngleDegrees) > 3600 || Math.Abs(CircularCaliperEndAngleDegrees) > 3600)
                {
                    return "圆周卡尺起止角必须是 -3600° 到 3600° 之间的有限数值。";
                }

                double circularSpan = Math.Abs(CircularCaliperEndAngleDegrees - CircularCaliperStartAngleDegrees);
                if (circularSpan < 30 || circularSpan > 360.000001)
                {
                    return "圆周卡尺角度覆盖必须在 30° 到 360° 之间；终止角小于起始角表示逆向采样。";
                }

                if (CircularCaliperCount < 3 || CircularCaliperCount > 360)
                {
                    return "圆周卡尺数量必须在 3 到 360 之间。";
                }

                if (!IsFinite(CircularCaliperLength1) || !IsFinite(CircularCaliperLength2) ||
                    CircularCaliperLength1 < 1 || CircularCaliperLength1 > 100000 ||
                    CircularCaliperLength2 < 1 || CircularCaliperLength2 > 100000)
                {
                    return "圆周卡尺 Length1/Length2 半长必须在 1 到 100000 像素之间。";
                }

                if (!IsFinite(CircularCaliperSigma) || CircularCaliperSigma < 0.4 || CircularCaliperSigma > 100 ||
                    CircularCaliperSigma > CircularCaliperLength1 * 0.5)
                {
                    return "圆周卡尺 Sigma 必须在 0.4 到 100 之间，且不得大于 Length1 的一半。";
                }

                if (!IsFinite(CircularCaliperThreshold) || CircularCaliperThreshold < 1 || CircularCaliperThreshold > 1000000)
                {
                    return "圆周卡尺边缘阈值必须是 1 到 1000000 之间的有限数值。";
                }

                if (!VmCaliperTransition.IsSupported(CircularCaliperTransition))
                {
                    return "圆周卡尺边缘极性无效。";
                }

                if (!VmCaliperSelect.IsSupported(CircularCaliperSelect))
                {
                    return "圆周卡尺边缘选择方式无效。";
                }
            }

            if (kind == VmToolKind.RingWallThicknessMeasurement)
            {
                if (!IsFinite(RingCenterRow) || !IsFinite(RingCenterColumn) ||
                    RingCenterRow < 0 || RingCenterColumn < 0 ||
                    RingCenterRow > 1000000000 || RingCenterColumn > 1000000000)
                {
                    return "圆环测量中心 Row/Column 必须是大于等于 0 的有限像素坐标。";
                }

                if (!IsFinite(RingSearchRadius) || RingSearchRadius < 1 || RingSearchRadius > 1000000000)
                {
                    return "圆环测量搜索半径必须在 1 到 1000000000 像素之间。";
                }

                if (!IsFinite(RingStartAngleDegrees) || !IsFinite(RingEndAngleDegrees) ||
                    Math.Abs(RingStartAngleDegrees) > 3600 || Math.Abs(RingEndAngleDegrees) > 3600)
                {
                    return "圆环测量起止角必须是 -3600° 到 3600° 之间的有限数值。";
                }

                double ringSpan = Math.Abs(RingEndAngleDegrees - RingStartAngleDegrees);
                if (ringSpan < 30 || ringSpan > 360.000001)
                {
                    return "圆环测量角度覆盖必须在 30° 到 360° 之间；终止角小于起始角表示逆向采样。";
                }

                if (RingCaliperCount < 3 || RingCaliperCount > 360)
                {
                    return "圆环径向卡尺数量必须在 3 到 360 之间。";
                }

                if (!IsFinite(RingCaliperLength1) || !IsFinite(RingCaliperLength2) ||
                    RingCaliperLength1 < 1 || RingCaliperLength1 > 100000 ||
                    RingCaliperLength2 < 1 || RingCaliperLength2 > 100000)
                {
                    return "圆环径向卡尺 Length1/Length2 半长必须在 1 到 100000 像素之间。";
                }

                if (!IsFinite(RingCaliperSigma) || RingCaliperSigma < 0.4 || RingCaliperSigma > 100 ||
                    RingCaliperSigma > RingCaliperLength1 * 0.5)
                {
                    return "圆环径向卡尺 Sigma 必须在 0.4 到 100 之间，且不得大于 Length1 的一半。";
                }

                if (!IsFinite(RingCaliperThreshold) || RingCaliperThreshold < 1 || RingCaliperThreshold > 1000000)
                {
                    return "圆环径向卡尺边缘阈值必须是 1 到 1000000 之间的有限数值。";
                }

                if (!VmCaliperTransition.IsSupported(RingCaliperTransition))
                {
                    return "圆环径向卡尺边缘极性无效。";
                }

                if (!VmCaliperSelect.IsSupported(RingCaliperSelect))
                {
                    return "圆环径向卡尺边缘对选择方式无效。";
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

        private static bool IsOrderedProjectiveQuadrilateral(double[] rows, double[] columns, out string reason)
        {
            const double epsilon = 0.000001;
            for (int first = 0; first < 4; first++)
            {
                for (int second = first + 1; second < 4; second++)
                {
                    double dr = rows[first] - rows[second];
                    double dc = columns[first] - columns[second];
                    if (dr * dr + dc * dc <= epsilon * epsilon)
                    {
                        reason = "存在重复点。";
                        return false;
                    }
                }
            }

            double rowSpan = rows.Max() - rows.Min();
            double columnSpan = columns.Max() - columns.Min();
            double scaleSquared = Math.Max(1.0, rowSpan * rowSpan + columnSpan * columnSpan);
            double minimumCross = Math.Max(epsilon, scaleSquared * 0.000001);
            double sign = 0;
            double twiceArea = 0;
            for (int index = 0; index < 4; index++)
            {
                int next = (index + 1) % 4;
                int after = (index + 2) % 4;
                double cross = (columns[next] - columns[index]) * (rows[after] - rows[next]) -
                               (rows[next] - rows[index]) * (columns[after] - columns[next]);
                if (Math.Abs(cross) <= minimumCross)
                {
                    reason = "相邻三点共线或近共线。";
                    return false;
                }
                if (sign == 0) sign = Math.Sign(cross);
                else if (Math.Sign(cross) != Math.Sign(sign))
                {
                    reason = "点序必须沿凸四边形轮廓连续排列，不能交叉。";
                    return false;
                }
                twiceArea += columns[index] * rows[next] - columns[next] * rows[index];
            }
            double minimumTwiceArea = Math.Max(1.0, rowSpan * columnSpan * 0.000002);
            if (Math.Abs(twiceArea) <= minimumTwiceArea)
            {
                reason = "四边形面积过小，矩阵将接近奇异。";
                return false;
            }
            reason = null;
            return true;
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

    public static class VmPolarInterpolation
    {
        public const string NearestNeighbor = "nearest_neighbor";
        public const string Bilinear = "bilinear";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, NearestNeighbor, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Bilinear, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, Bilinear, StringComparison.OrdinalIgnoreCase))
            {
                return Bilinear;
            }

            return string.Equals(value, NearestNeighbor, StringComparison.OrdinalIgnoreCase) ? NearestNeighbor : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            return normalized == NearestNeighbor ? "最近邻" : (normalized == Bilinear ? "双线性" : "未知插值");
        }

        public static VmPolarInterpolationOption[] CreateAll()
        {
            return new[]
            {
                new VmPolarInterpolationOption { Key = Bilinear, DisplayText = "双线性（质量优先）" },
                new VmPolarInterpolationOption { Key = NearestNeighbor, DisplayText = "最近邻（速度优先）" }
            };
        }
    }

    public sealed class VmPolarInterpolationOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmRoiImageExtractionMode
    {
        public const string ReduceDomain = "ReduceDomain";
        public const string CropDomain = "CropDomain";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, ReduceDomain, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, CropDomain, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, ReduceDomain, StringComparison.OrdinalIgnoreCase))
            {
                return ReduceDomain;
            }

            return string.Equals(value, CropDomain, StringComparison.OrdinalIgnoreCase) ? CropDomain : value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == CropDomain) return "裁剪外接框（局部坐标）";
            if (normalized == ReduceDomain) return "保留原图尺寸与坐标";
            return "未知模式";
        }

        public static VmRoiImageExtractionModeOption[] CreateAll()
        {
            return new[]
            {
                new VmRoiImageExtractionModeOption { Key = ReduceDomain, DisplayText = "保留原图尺寸与坐标（reduce_domain）" },
                new VmRoiImageExtractionModeOption { Key = CropDomain, DisplayText = "裁剪有效域外接框（crop_domain）" }
            };
        }
    }

    public sealed class VmRoiImageExtractionModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmImageResizeInterpolation
    {
        public const string NearestNeighbor = "nearest_neighbor";
        public const string Constant = "constant";
        public const string Bilinear = "bilinear";
        public const string Bicubic = "bicubic";
        public const string Weighted = "weighted";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, NearestNeighbor, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Constant, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Bilinear, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Bicubic, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Weighted, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Bilinear;
            if (string.Equals(value, NearestNeighbor, StringComparison.OrdinalIgnoreCase)) return NearestNeighbor;
            if (string.Equals(value, Constant, StringComparison.OrdinalIgnoreCase)) return Constant;
            if (string.Equals(value, Bilinear, StringComparison.OrdinalIgnoreCase)) return Bilinear;
            if (string.Equals(value, Bicubic, StringComparison.OrdinalIgnoreCase)) return Bicubic;
            if (string.Equals(value, Weighted, StringComparison.OrdinalIgnoreCase)) return Weighted;
            return value;
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == NearestNeighbor) return "最近邻";
            if (normalized == Constant) return "常量插值";
            if (normalized == Bilinear) return "双线性";
            if (normalized == Bicubic) return "双三次";
            if (normalized == Weighted) return "加权插值";
            return "未知插值";
        }

        public static VmImageResizeInterpolationOption[] CreateAll()
        {
            return new[]
            {
                new VmImageResizeInterpolationOption { Key = Bilinear, DisplayText = "双线性（通用）" },
                new VmImageResizeInterpolationOption { Key = NearestNeighbor, DisplayText = "最近邻（标签/速度）" },
                new VmImageResizeInterpolationOption { Key = Constant, DisplayText = "常量插值" },
                new VmImageResizeInterpolationOption { Key = Bicubic, DisplayText = "双三次（质量优先）" },
                new VmImageResizeInterpolationOption { Key = Weighted, DisplayText = "加权插值" }
            };
        }
    }

    public sealed class VmImageResizeInterpolationOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmProjectiveInterpolation
    {
        public const string NearestNeighbor = "nearest_neighbor";
        public const string Bilinear = "bilinear";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, NearestNeighbor, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Bilinear, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Bilinear;
            if (string.Equals(value, NearestNeighbor, StringComparison.OrdinalIgnoreCase)) return NearestNeighbor;
            if (string.Equals(value, Bilinear, StringComparison.OrdinalIgnoreCase)) return Bilinear;
            return value;
        }

        public static string GetDisplayText(string value)
        {
            return Normalize(value) == NearestNeighbor ? "最近邻" : "双线性";
        }

        public static VmImageResizeInterpolationOption[] CreateAll()
        {
            return new[]
            {
                new VmImageResizeInterpolationOption { Key = Bilinear, DisplayText = "双线性（质量优先）" },
                new VmImageResizeInterpolationOption { Key = NearestNeighbor, DisplayText = "最近邻（速度/标签）" }
            };
        }
    }

    public static class VmProjectiveOutputMode
    {
        public const string KeepInputSize = "KeepInputSize";
        public const string AdaptImageSize = "AdaptImageSize";
        public const string ExplicitSize = "ExplicitSize";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, KeepInputSize, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, AdaptImageSize, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ExplicitSize, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return KeepInputSize;
            if (string.Equals(value, KeepInputSize, StringComparison.OrdinalIgnoreCase)) return KeepInputSize;
            if (string.Equals(value, AdaptImageSize, StringComparison.OrdinalIgnoreCase)) return AdaptImageSize;
            if (string.Equals(value, ExplicitSize, StringComparison.OrdinalIgnoreCase)) return ExplicitSize;
            return value;
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == AdaptImageSize) return "自适应非负边界";
            if (normalized == ExplicitSize) return "显式输出尺寸";
            return "保持输入尺寸";
        }

        public static VmLayerOption[] CreateAll()
        {
            return new[]
            {
                new VmLayerOption { Key = KeepInputSize, DisplayText = "保持输入尺寸" },
                new VmLayerOption { Key = AdaptImageSize, DisplayText = "自适应非负边界" },
                new VmLayerOption { Key = ExplicitSize, DisplayText = "显式输出尺寸" }
            };
        }
    }

    public static class VmSubpixelEdgeFilter
    {
        public const string Deriche1 = "deriche1";
        public const string Lanser1 = "lanser1";
        public const string Deriche2 = "deriche2";
        public const string Lanser2 = "lanser2";
        public const string Shen = "shen";
        public const string ModifiedShen = "mshen";
        public const string Canny = "canny";
        public const string Sobel = "sobel";
        public const string SobelFast = "sobel_fast";

        private static readonly string[] Supported =
        {
            Deriche1, Lanser1, Deriche2, Lanser2, Shen, ModifiedShen, Canny, Sobel, SobelFast
        };

        public static bool IsSupported(string value)
        {
            return Supported.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Canny;
            string match = Supported.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
            return match ?? value;
        }

        public static VmLayerOption[] CreateAll()
        {
            return new[]
            {
                new VmLayerOption { Key = Canny, DisplayText = "Canny（高质量通用）" },
                new VmLayerOption { Key = Deriche1, DisplayText = "Deriche 1（递归）" },
                new VmLayerOption { Key = Lanser1, DisplayText = "Lanser 1（递归）" },
                new VmLayerOption { Key = Deriche2, DisplayText = "Deriche 2（递归）" },
                new VmLayerOption { Key = Lanser2, DisplayText = "Lanser 2（递归）" },
                new VmLayerOption { Key = Shen, DisplayText = "Shen（递归）" },
                new VmLayerOption { Key = ModifiedShen, DisplayText = "Modified Shen（递归）" },
                new VmLayerOption { Key = Sobel, DisplayText = "Sobel（固定宽度）" },
                new VmLayerOption { Key = SobelFast, DisplayText = "Sobel Fast（速度优先）" }
            };
        }
    }

    public static class VmSubpixelEdgeColorPolicy
    {
        public const string AutoGray = "AutoGray";
        public const string RequireSingleChannel = "RequireSingleChannel";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, AutoGray, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, RequireSingleChannel, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return AutoGray;
            if (string.Equals(value, AutoGray, StringComparison.OrdinalIgnoreCase)) return AutoGray;
            if (string.Equals(value, RequireSingleChannel, StringComparison.OrdinalIgnoreCase)) return RequireSingleChannel;
            return value;
        }

        public static string GetDisplayText(string value)
        {
            return Normalize(value) == RequireSingleChannel ? "仅允许单通道" : "彩色自动转灰";
        }

        public static VmLayerOption[] CreateAll()
        {
            return new[]
            {
                new VmLayerOption { Key = AutoGray, DisplayText = "彩色自动转灰（rgb1_to_gray）" },
                new VmLayerOption { Key = RequireSingleChannel, DisplayText = "仅允许单通道输入" }
            };
        }
    }

    public static class VmImageMirrorMode
    {
        public const string Row = "row";
        public const string Column = "column";
        public const string Diagonal = "diagonal";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Row, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Column, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Diagonal, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Row;
            if (string.Equals(value, Row, StringComparison.OrdinalIgnoreCase)) return Row;
            if (string.Equals(value, Column, StringComparison.OrdinalIgnoreCase)) return Column;
            if (string.Equals(value, Diagonal, StringComparison.OrdinalIgnoreCase)) return Diagonal;
            return value;
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Row) return "上下镜像（row）";
            if (normalized == Column) return "左右镜像（column）";
            if (normalized == Diagonal) return "主对角线镜像（diagonal）";
            return "未知方向";
        }

        public static VmImageMirrorModeOption[] CreateAll()
        {
            return new[]
            {
                new VmImageMirrorModeOption { Key = Row, DisplayText = "上下镜像（row / 水平轴）" },
                new VmImageMirrorModeOption { Key = Column, DisplayText = "左右镜像（column / 垂直轴）" },
                new VmImageMirrorModeOption { Key = Diagonal, DisplayText = "主对角线镜像（diagonal / 交换宽高）" }
            };
        }
    }

    public sealed class VmImageMirrorModeOption
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

    public static class VmXldAdjacentAttributeMode
    {
        public const string AttrKeep = "AttrKeep";
        public const string AttrForget = "AttrForget";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, AttrKeep, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, AttrForget, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "attr_keep", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "attr_forget", StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, AttrKeep, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "attr_keep", StringComparison.OrdinalIgnoreCase)) return AttrKeep;
            if (string.Equals(value, AttrForget, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "attr_forget", StringComparison.OrdinalIgnoreCase)) return AttrForget;
            return value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            return Normalize(value) == AttrForget ? "attr_forget" : "attr_keep";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == AttrForget) return "丢弃轮廓属性（attr_forget）";
            return normalized == AttrKeep ? "保留并适配属性（attr_keep）" : "未知属性策略";
        }

        public static VmXldAdjacentAttributeModeOption[] CreateAll()
        {
            return new[]
            {
                new VmXldAdjacentAttributeModeOption { Key = AttrKeep, DisplayText = "保留并适配属性（attr_keep）" },
                new VmXldAdjacentAttributeModeOption { Key = AttrForget, DisplayText = "丢弃属性（attr_forget）" }
            };
        }
    }

    public sealed class VmXldAdjacentAttributeModeOption
    {
        public string Key { get; set; }

        public string DisplayText { get; set; }
    }

    public static class VmXldAffineCenterMode
    {
        public const string InputBounds = "InputBounds";
        public const string Manual = "Manual";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, InputBounds, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, Manual, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, InputBounds, StringComparison.OrdinalIgnoreCase)) return InputBounds;
            if (string.Equals(value, Manual, StringComparison.OrdinalIgnoreCase)) return Manual;
            return value.Trim();
        }

        public static VmXldAffineOption[] CreateAll()
        {
            return new[]
            {
                new VmXldAffineOption { Key = InputBounds, DisplayText = "输入轮廓包围中心" },
                new VmXldAffineOption { Key = Manual, DisplayText = "手动 Row/Column 中心" }
            };
        }
    }

    public static class VmXldAffineCoordinatePolicy
    {
        public const string StandardPixelCenter = "StandardPixelCenter";
        public const string AffineTransPixel = "AffineTransPixel";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, StandardPixelCenter, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, AffineTransPixel, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, StandardPixelCenter, StringComparison.OrdinalIgnoreCase)) return StandardPixelCenter;
            if (string.Equals(value, AffineTransPixel, StringComparison.OrdinalIgnoreCase)) return AffineTransPixel;
            return value.Trim();
        }

        public static VmXldAffineOption[] CreateAll()
        {
            return new[]
            {
                new VmXldAffineOption { Key = StandardPixelCenter, DisplayText = "标准像素中心（±0.5 补偿）" },
                new VmXldAffineOption { Key = AffineTransPixel, DisplayText = "affine_trans_pixel 原点（不补偿）" }
            };
        }
    }

    public static class VmXldAffineTransformOrder
    {
        public const string ScaleSlantRotateTranslate = "ScaleSlantRotateTranslate";
        public const string ScaleRotateSlantTranslate = "ScaleRotateSlantTranslate";
        public const string RotateScaleSlantTranslate = "RotateScaleSlantTranslate";
        public const string RotateSlantScaleTranslate = "RotateSlantScaleTranslate";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, ScaleSlantRotateTranslate, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, ScaleRotateSlantTranslate, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, RotateScaleSlantTranslate, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, RotateSlantScaleTranslate, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, ScaleSlantRotateTranslate, StringComparison.OrdinalIgnoreCase)) return ScaleSlantRotateTranslate;
            if (string.Equals(value, ScaleRotateSlantTranslate, StringComparison.OrdinalIgnoreCase)) return ScaleRotateSlantTranslate;
            if (string.Equals(value, RotateScaleSlantTranslate, StringComparison.OrdinalIgnoreCase)) return RotateScaleSlantTranslate;
            if (string.Equals(value, RotateSlantScaleTranslate, StringComparison.OrdinalIgnoreCase)) return RotateSlantScaleTranslate;
            return value.Trim();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == ScaleRotateSlantTranslate) return "缩放 → 旋转 → 斜切 → 平移";
            if (normalized == RotateScaleSlantTranslate) return "旋转 → 缩放 → 斜切 → 平移";
            if (normalized == RotateSlantScaleTranslate) return "旋转 → 斜切 → 缩放 → 平移";
            return "缩放 → 斜切 → 旋转 → 平移";
        }

        public static VmXldAffineOption[] CreateAll()
        {
            return new[]
            {
                new VmXldAffineOption { Key = ScaleSlantRotateTranslate, DisplayText = "缩放 → 斜切 → 旋转 → 平移" },
                new VmXldAffineOption { Key = ScaleRotateSlantTranslate, DisplayText = "缩放 → 旋转 → 斜切 → 平移" },
                new VmXldAffineOption { Key = RotateScaleSlantTranslate, DisplayText = "旋转 → 缩放 → 斜切 → 平移" },
                new VmXldAffineOption { Key = RotateSlantScaleTranslate, DisplayText = "旋转 → 斜切 → 缩放 → 平移" }
            };
        }
    }

    public sealed class VmXldAffineOption
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

    public static class VmCaliperTransition
    {
        public const string All = "All";
        public const string Positive = "Positive";
        public const string Negative = "Negative";
        public const string AllStrongest = "AllStrongest";
        public const string PositiveStrongest = "PositiveStrongest";
        public const string NegativeStrongest = "NegativeStrongest";

        private static readonly string[] Values = { All, Positive, Negative, AllStrongest, PositiveStrongest, NegativeStrongest };

        public static bool IsSupported(string value)
        {
            return Values.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return All;
            }
            return Values.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Positive) return "positive";
            if (normalized == Negative) return "negative";
            if (normalized == AllStrongest) return "all_strongest";
            if (normalized == PositiveStrongest) return "positive_strongest";
            if (normalized == NegativeStrongest) return "negative_strongest";
            return "all";
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == Positive) return "正边缘（暗→亮）";
            if (normalized == Negative) return "负边缘（亮→暗）";
            if (normalized == AllStrongest) return "任意极性 · 最强边缘对";
            if (normalized == PositiveStrongest) return "正边缘 · 最强边缘对";
            if (normalized == NegativeStrongest) return "负边缘 · 最强边缘对";
            return "任意极性";
        }

        public static VmCaliperOption[] CreateAll()
        {
            return Values.Select(item => new VmCaliperOption { Key = item, DisplayText = GetDisplayText(item) }).ToArray();
        }
    }

    public static class VmCaliperSelect
    {
        public const string All = "All";
        public const string First = "First";
        public const string Last = "Last";

        private static readonly string[] Values = { All, First, Last };

        public static bool IsSupported(string value)
        {
            return Values.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        }

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return All;
            }
            return Values.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? value.Trim();
        }

        public static string GetHalconValue(string value)
        {
            return Normalize(value).ToLowerInvariant();
        }

        public static string GetDisplayText(string value)
        {
            string normalized = Normalize(value);
            if (normalized == First) return "第一对";
            if (normalized == Last) return "最后一对";
            return "全部边缘对";
        }

        public static VmCaliperOption[] CreateAll()
        {
            return Values.Select(item => new VmCaliperOption { Key = item, DisplayText = GetDisplayText(item) }).ToArray();
        }
    }

    public sealed class VmCaliperOption
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
