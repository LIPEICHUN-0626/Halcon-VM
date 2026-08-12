using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class HalconImageService : IDisposable
    {
        private HFramegrabber framegrabber;
        private CancellationTokenSource continuousGrabCancellation;

        public event EventHandler<ImageGrabbedEventArgs> ImageGrabbed;

        public bool IsCameraOpen
        {
            get { return framegrabber != null; }
        }

        public HImage ReadImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("请选择图片文件。", "imagePath");
            }

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("图片文件不存在。", imagePath);
            }

            return new HImage(imagePath);
        }

        public int GetChannelCount(HImage image)
        {
            if (image == null)
            {
                return 0;
            }

            HTuple channels;
            HOperatorSet.CountChannels(image, out channels);
            return channels.Length == 0 ? 0 : channels.I;
        }

        public HImage ToGray(HImage image)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            if (GetChannelCount(image) <= 1)
            {
                return image.CopyImage();
            }

            HObject gray;
            HOperatorSet.Rgb1ToGray(image, out gray);
            return new HImage(gray);
        }

        public HImage ToColor(HImage image)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            if (GetChannelCount(image) >= 3)
            {
                return image.CopyImage();
            }

            HObject color;
            HOperatorSet.Compose3(image, image, image, out color);
            return new HImage(color);
        }

        public HImage ExtractChannel(HImage image, int channelIndex)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            int channelCount = GetChannelCount(image);
            if (channelIndex < 1 || channelIndex > channelCount)
            {
                throw new ArgumentOutOfRangeException(
                    "channelIndex",
                    string.Format("图像只有 {0} 个通道，不能提取第 {1} 通道。", channelCount, channelIndex));
            }

            HObject channel;
            HOperatorSet.AccessChannel(image, out channel, channelIndex);
            return new HImage(channel);
        }

        public HImage MeanFilter(HImage image, int maskWidth, int maskHeight)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            return image.MeanImage(maskWidth, maskHeight);
        }

        public HImage MedianFilter(HImage image, int radius)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            return image.MedianImage("circle", radius, "mirrored");
        }

        public HImage ScaleImage(HImage image, double scale, double offset)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            HObject enhanced;
            HOperatorSet.ScaleImage(image, out enhanced, scale, offset);
            return new HImage(enhanced);
        }

        public HImage Emphasize(HImage image, int maskWidth, int maskHeight, double factor)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            HObject enhanced;
            HOperatorSet.Emphasize(image, out enhanced, maskWidth, maskHeight, factor);
            return new HImage(enhanced);
        }

        public HImage AbsoluteDifference(HImage imageA, HImage imageB, double multiplier)
        {
            if (imageA == null)
            {
                throw new ArgumentNullException("imageA");
            }

            if (imageB == null)
            {
                throw new ArgumentNullException("imageB");
            }

            HObject difference = null;
            try
            {
                HOperatorSet.AbsDiffImage(imageA, imageB, out difference, multiplier);
                HImage result = new HImage(difference);
                difference = null;
                return result;
            }
            finally
            {
                if (difference != null)
                {
                    difference.Dispose();
                }
            }
        }

        public HImage SubtractImage(HImage minuend, HImage subtrahend, double multiplier, double add)
        {
            if (minuend == null)
            {
                throw new ArgumentNullException("minuend");
            }

            if (subtrahend == null)
            {
                throw new ArgumentNullException("subtrahend");
            }

            HObject difference = null;
            try
            {
                HOperatorSet.SubImage(minuend, subtrahend, out difference, multiplier, add);
                HImage result = new HImage(difference);
                difference = null;
                return result;
            }
            finally
            {
                if (difference != null)
                {
                    difference.Dispose();
                }
            }
        }

        public HRegion DynamicThreshold(HImage originalImage, HImage referenceImage, double offset, string polarity)
        {
            if (originalImage == null)
            {
                throw new ArgumentNullException("originalImage");
            }

            if (referenceImage == null)
            {
                throw new ArgumentNullException("referenceImage");
            }

            HObject region = null;
            try
            {
                HOperatorSet.DynThreshold(originalImage, referenceImage, out region, offset, polarity);
                HRegion result = new HRegion(region);
                region = null;
                return result;
            }
            finally
            {
                if (region != null)
                {
                    region.Dispose();
                }
            }
        }

        public HObject GenerateRegionContours(HRegion region, string mode)
        {
            if (region == null)
            {
                throw new ArgumentNullException("region");
            }

            HObject contours = null;
            try
            {
                HOperatorSet.GenContourRegionXld(region, out contours, VmRegionContourMode.GetHalconValue(mode));
                HObject result = contours;
                contours = null;
                return result;
            }
            finally
            {
                if (contours != null)
                {
                    contours.Dispose();
                }
            }
        }

        public HObject FilterXldContours(HObject contours, string feature, double minimum, double maximum)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            HObject selectedContours = null;
            try
            {
                HOperatorSet.SelectContoursXld(
                    contours,
                    out selectedContours,
                    VmXldContourFeature.GetHalconValue(feature),
                    minimum,
                    maximum,
                    0,
                    0);
                HObject result = selectedContours;
                selectedContours = null;
                return result;
            }
            finally
            {
                if (selectedContours != null)
                {
                    selectedContours.Dispose();
                }
            }
        }

        public HObject UnionAdjacentXldContours(HObject contours, double maxDistAbs, double maxDistRel, string attributeMode)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            HObject unionContours = null;
            try
            {
                HOperatorSet.UnionAdjacentContoursXld(
                    contours,
                    out unionContours,
                    maxDistAbs,
                    maxDistRel,
                    VmXldAdjacentAttributeMode.GetHalconValue(attributeMode));
                HObject result = unionContours;
                unionContours = null;
                return result;
            }
            finally
            {
                if (unionContours != null)
                {
                    unionContours.Dispose();
                }
            }
        }

        public HObject SmoothXldContours(HObject contours, int numRegrPoints)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            HObject smoothedContours = null;
            try
            {
                HOperatorSet.SmoothContoursXld(contours, out smoothedContours, numRegrPoints);
                HObject result = smoothedContours;
                smoothedContours = null;
                return result;
            }
            finally
            {
                if (smoothedContours != null)
                {
                    smoothedContours.Dispose();
                }
            }
        }

        public HObject AffineTransformXldContours(HObject contours, HTuple homMat2D)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }
            if (homMat2D == null || homMat2D.Length != 6)
            {
                throw new ArgumentException("二维仿射矩阵必须包含 6 个有限数值。", "homMat2D");
            }

            HObject transformedContours = null;
            try
            {
                HOperatorSet.AffineTransContourXld(contours, out transformedContours, homMat2D);
                HObject result = transformedContours;
                transformedContours = null;
                return result;
            }
            finally
            {
                if (transformedContours != null)
                {
                    transformedContours.Dispose();
                }
            }
        }

        public HObject ProjectiveTransformXldContours(HObject contours, HTuple homMat2D)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }
            if (homMat2D == null || homMat2D.Length != 9 || homMat2D.ToDArr().Any(value => double.IsNaN(value) || double.IsInfinity(value)))
            {
                throw new ArgumentException("二维投影矩阵必须包含 9 个有限数值。", "homMat2D");
            }

            HObject transformedContours = null;
            try
            {
                HOperatorSet.ProjectiveTransContourXld(contours, out transformedContours, homMat2D);
                HObject result = transformedContours;
                transformedContours = null;
                return result;
            }
            finally
            {
                if (transformedContours != null)
                {
                    transformedContours.Dispose();
                }
            }
        }

        public HObject CropXldContours(HObject contours, double row1, double column1, double row2, double column2, bool closeContours)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }
            if (double.IsNaN(row1) || double.IsInfinity(row1) || double.IsNaN(column1) || double.IsInfinity(column1) ||
                double.IsNaN(row2) || double.IsInfinity(row2) || double.IsNaN(column2) || double.IsInfinity(column2) ||
                row2 - row1 < 1 || column2 - column1 < 1)
            {
                throw new ArgumentException("XLD 裁剪矩形必须是至少 1×1 像素的有限有序矩形。", "row1");
            }

            HObject croppedContours = null;
            try
            {
                HOperatorSet.CropContoursXld(contours, out croppedContours, row1, column1, row2, column2, closeContours ? "true" : "false");
                HObject result = croppedContours;
                croppedContours = null;
                return result;
            }
            finally
            {
                if (croppedContours != null)
                {
                    croppedContours.Dispose();
                }
            }
        }

        public HObject SegmentXldContours(HObject contours, string mode, int smoothContour, double maxLineDistance1, double maxLineDistance2)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            HObject segmentedContours = null;
            try
            {
                HOperatorSet.SegmentContoursXld(
                    contours,
                    out segmentedContours,
                    VmXldSegmentMode.GetHalconValue(mode),
                    smoothContour,
                    maxLineDistance1,
                    maxLineDistance2);
                HObject result = segmentedContours;
                segmentedContours = null;
                return result;
            }
            finally
            {
                if (segmentedContours != null)
                {
                    segmentedContours.Dispose();
                }
            }
        }

        public HObject FitXldLines(
            HObject contours,
            string algorithm,
            int maxNumPoints,
            int clippingEndPoints,
            int iterations,
            double clippingFactor,
            out HTuple rowBegin,
            out HTuple columnBegin,
            out HTuple rowEnd,
            out HTuple columnEnd,
            out HTuple normalRow,
            out HTuple normalColumn,
            out HTuple distance)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            rowBegin = columnBegin = rowEnd = columnEnd = normalRow = normalColumn = distance = null;
            HObject fittedLines = null;
            HOperatorSet.FitLineContourXld(
                contours,
                VmXldLineFitAlgorithm.GetHalconValue(algorithm),
                maxNumPoints,
                clippingEndPoints,
                iterations,
                clippingFactor,
                out rowBegin,
                out columnBegin,
                out rowEnd,
                out columnEnd,
                out normalRow,
                out normalColumn,
                out distance);

            try
            {
                HOperatorSet.GenEmptyObj(out fittedLines);
                for (int index = 0; index < rowBegin.Length; index++)
                {
                    HObject line = null;
                    HObject combined = null;
                    try
                    {
                        HOperatorSet.GenContourPolygonXld(
                            out line,
                            new HTuple(new[] { rowBegin[index].D, rowEnd[index].D }),
                            new HTuple(new[] { columnBegin[index].D, columnEnd[index].D }));
                        HOperatorSet.ConcatObj(fittedLines, line, out combined);
                        fittedLines.Dispose();
                        fittedLines = combined;
                        combined = null;
                    }
                    finally
                    {
                        if (line != null) line.Dispose();
                        if (combined != null) combined.Dispose();
                    }
                }

                HObject result = fittedLines;
                fittedLines = null;
                return result;
            }
            finally
            {
                if (fittedLines != null) fittedLines.Dispose();
            }
        }

        public HObject FitXldCircles(
            HObject contours,
            string algorithm,
            int maxNumPoints,
            double maxClosureDistance,
            int clippingEndPoints,
            int iterations,
            double clippingFactor,
            out HTuple row,
            out HTuple column,
            out HTuple radius,
            out HTuple startPhi,
            out HTuple endPhi,
            out HTuple pointOrder)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            row = column = radius = startPhi = endPhi = pointOrder = null;
            HOperatorSet.FitCircleContourXld(
                contours,
                VmXldCircleFitAlgorithm.GetHalconValue(algorithm),
                maxNumPoints,
                maxClosureDistance,
                clippingEndPoints,
                iterations,
                clippingFactor,
                out row,
                out column,
                out radius,
                out startPhi,
                out endPhi,
                out pointOrder);

            HObject fittedCircles = null;
            try
            {
                HOperatorSet.GenEmptyObj(out fittedCircles);
                for (int index = 0; index < row.Length; index++)
                {
                    HObject circle = null;
                    HObject combined = null;
                    try
                    {
                        string order = string.Equals(pointOrder[index].S, "negative", StringComparison.OrdinalIgnoreCase) ? "negative" : "positive";
                        HOperatorSet.GenCircleContourXld(
                            out circle,
                            row[index].D,
                            column[index].D,
                            radius[index].D,
                            startPhi[index].D,
                            endPhi[index].D,
                            order,
                            1.0);
                        HOperatorSet.ConcatObj(fittedCircles, circle, out combined);
                        fittedCircles.Dispose();
                        fittedCircles = combined;
                        combined = null;
                    }
                    finally
                    {
                        if (circle != null) circle.Dispose();
                        if (combined != null) combined.Dispose();
                    }
                }

                HObject result = fittedCircles;
                fittedCircles = null;
                return result;
            }
            finally
            {
                if (fittedCircles != null) fittedCircles.Dispose();
            }
        }

        public HObject FitXldEllipses(
            HObject contours,
            string algorithm,
            int maxNumPoints,
            double maxClosureDistance,
            int clippingEndPoints,
            int vossTabSize,
            int iterations,
            double clippingFactor,
            out HTuple row,
            out HTuple column,
            out HTuple phi,
            out HTuple radius1,
            out HTuple radius2,
            out HTuple startPhi,
            out HTuple endPhi,
            out HTuple pointOrder)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            row = column = phi = radius1 = radius2 = startPhi = endPhi = pointOrder = null;
            HOperatorSet.FitEllipseContourXld(
                contours,
                VmXldEllipseFitAlgorithm.GetHalconValue(algorithm),
                maxNumPoints,
                maxClosureDistance,
                clippingEndPoints,
                vossTabSize,
                iterations,
                clippingFactor,
                out row,
                out column,
                out phi,
                out radius1,
                out radius2,
                out startPhi,
                out endPhi,
                out pointOrder);

            HObject fittedEllipses = null;
            try
            {
                HOperatorSet.GenEmptyObj(out fittedEllipses);
                for (int index = 0; index < row.Length; index++)
                {
                    HObject ellipse = null;
                    HObject combined = null;
                    try
                    {
                        string order = string.Equals(pointOrder[index].S, "negative", StringComparison.OrdinalIgnoreCase) ? "negative" : "positive";
                        HOperatorSet.GenEllipseContourXld(
                            out ellipse,
                            row[index].D,
                            column[index].D,
                            phi[index].D,
                            radius1[index].D,
                            radius2[index].D,
                            startPhi[index].D,
                            endPhi[index].D,
                            order,
                            1.0);
                        HOperatorSet.ConcatObj(fittedEllipses, ellipse, out combined);
                        fittedEllipses.Dispose();
                        fittedEllipses = combined;
                        combined = null;
                    }
                    finally
                    {
                        if (ellipse != null) ellipse.Dispose();
                        if (combined != null) combined.Dispose();
                    }
                }

                HObject result = fittedEllipses;
                fittedEllipses = null;
                return result;
            }
            finally
            {
                if (fittedEllipses != null) fittedEllipses.Dispose();
            }
        }

        public HObject FitXldRectangles(
            HObject contours,
            string algorithm,
            int maxNumPoints,
            double maxClosureDistance,
            int clippingEndPoints,
            int iterations,
            double clippingFactor,
            out HTuple row,
            out HTuple column,
            out HTuple phi,
            out HTuple length1,
            out HTuple length2,
            out HTuple pointOrder)
        {
            if (contours == null)
            {
                throw new ArgumentNullException("contours");
            }

            row = column = phi = length1 = length2 = pointOrder = null;
            HOperatorSet.FitRectangle2ContourXld(
                contours,
                VmXldRectangleFitAlgorithm.GetHalconValue(algorithm),
                maxNumPoints,
                maxClosureDistance,
                clippingEndPoints,
                iterations,
                clippingFactor,
                out row,
                out column,
                out phi,
                out length1,
                out length2,
                out pointOrder);

            HObject fittedRectangles = null;
            try
            {
                HOperatorSet.GenEmptyObj(out fittedRectangles);
                for (int index = 0; index < row.Length; index++)
                {
                    HObject rectangle = null;
                    HObject combined = null;
                    try
                    {
                        HOperatorSet.GenRectangle2ContourXld(
                            out rectangle,
                            row[index].D,
                            column[index].D,
                            phi[index].D,
                            length1[index].D,
                            length2[index].D);
                        HOperatorSet.ConcatObj(fittedRectangles, rectangle, out combined);
                        fittedRectangles.Dispose();
                        fittedRectangles = combined;
                        combined = null;
                    }
                    finally
                    {
                        if (rectangle != null) rectangle.Dispose();
                        if (combined != null) combined.Dispose();
                    }
                }

                HObject result = fittedRectangles;
                fittedRectangles = null;
                return result;
            }
            finally
            {
                if (fittedRectangles != null) fittedRectangles.Dispose();
            }
        }

        public void OpenCamera(string interfaceName, string deviceName)
        {
            CloseCamera();

            string resolvedDevice = string.IsNullOrWhiteSpace(deviceName) ? "default" : deviceName.Trim();

            framegrabber = new HFramegrabber(
                interfaceName,
                0,
                0,
                0,
                0,
                0,
                0,
                "default",
                -1,
                "default",
                -1,
                "default",
                resolvedDevice,
                "default",
                -1,
                -1);
        }

        public HImage GrabSingle()
        {
            EnsureCameraOpen();
            return framegrabber.GrabImage();
        }

        public void StartContinuousGrab(Func<bool> canPublish)
        {
            EnsureCameraOpen();
            StopContinuousGrab();

            continuousGrabCancellation = new CancellationTokenSource();
            CancellationToken token = continuousGrabCancellation.Token;

            Task.Run(delegate
            {
                while (!token.IsCancellationRequested)
                {
                    HImage image = null;
                    try
                    {
                        image = GrabSingle();
                        if (canPublish == null || canPublish())
                        {
                            OnImageGrabbed(image, "Camera");
                            image = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnImageGrabbed(null, "Camera", ex);
                        break;
                    }
                    finally
                    {
                        if (image != null)
                        {
                            image.Dispose();
                        }
                    }

                    Thread.Sleep(30);
                }
            }, token);
        }

        public void StopContinuousGrab()
        {
            if (continuousGrabCancellation == null)
            {
                return;
            }

            continuousGrabCancellation.Cancel();
            continuousGrabCancellation.Dispose();
            continuousGrabCancellation = null;
        }

        public void CloseCamera()
        {
            StopContinuousGrab();

            if (framegrabber != null)
            {
                framegrabber.CloseFramegrabber();
                framegrabber.Dispose();
                framegrabber = null;
            }
        }

        public void Dispose()
        {
            CloseCamera();
        }

        private void EnsureCameraOpen()
        {
            if (!IsCameraOpen)
            {
                throw new InvalidOperationException("相机未打开。");
            }
        }

        private void OnImageGrabbed(HImage image, string source, Exception error = null)
        {
            EventHandler<ImageGrabbedEventArgs> handler = ImageGrabbed;
            if (handler != null)
            {
                handler(this, new ImageGrabbedEventArgs(image, source, error));
            }
        }
    }

    public sealed class ImageGrabbedEventArgs : EventArgs
    {
        public ImageGrabbedEventArgs(HImage image, string source, Exception error)
        {
            Image = image;
            Source = source;
            Error = error;
        }

        public HImage Image { get; private set; }

        public string Source { get; private set; }

        public Exception Error { get; private set; }
    }
}
