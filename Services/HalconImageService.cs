using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HalconDotNet;

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
