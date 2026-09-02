using System;
using System.Drawing;
using System.Windows.Forms;
using HalconDotNet;

namespace HalconWinFormsDemo.Services
{
    public sealed class ImageViewportController
    {
        private int imageWidth;
        private int imageHeight;
        private double row1;
        private double column1;
        private double row2;
        private double column2;
        private int viewportWidth;
        private int viewportHeight;

        public bool HasImage
        {
            get { return imageWidth > 0 && imageHeight > 0; }
        }

        public int ImageWidth
        {
            get { return imageWidth; }
        }

        public int ImageHeight
        {
            get { return imageHeight; }
        }

        public string ZoomText
        {
            get
            {
                if (!HasImage)
                {
                    return "Zoom: --";
                }

                double zoom = viewportWidth > 0 && viewportHeight > 0
                    ? 1.0 / Math.Max((row2 - row1 + 1) / viewportHeight, (column2 - column1 + 1) / viewportWidth)
                    : imageWidth / Math.Max(1.0, column2 - column1 + 1.0);
                return string.Format("Zoom: {0:F0}%", zoom * 100.0);
            }
        }

        public void SynchronizeNormalizedViewFrom(ImageViewportController source)
        {
            if (!HasImage || source == null || !source.HasImage)
            {
                return;
            }

            double sourceRows = Math.Max(2.0, source.row2 - source.row1 + 1.0);
            double sourceColumns = Math.Max(2.0, source.column2 - source.column1 + 1.0);
            double normalizedCenterRow = (source.row1 + source.row2 + 1.0) / (2.0 * Math.Max(1, source.imageHeight));
            double normalizedCenterColumn = (source.column1 + source.column2 + 1.0) / (2.0 * Math.Max(1, source.imageWidth));
            double normalizedRows = sourceRows / Math.Max(1.0, source.imageHeight);
            double normalizedColumns = sourceColumns / Math.Max(1.0, source.imageWidth);

            double targetRows = Math.Max(1.0, normalizedRows * imageHeight);
            double targetColumns = Math.Max(1.0, normalizedColumns * imageWidth);
            double targetCenterRow = normalizedCenterRow * imageHeight;
            double targetCenterColumn = normalizedCenterColumn * imageWidth;

            row1 = targetCenterRow - targetRows / 2.0;
            row2 = row1 + targetRows - 1.0;
            column1 = targetCenterColumn - targetColumns / 2.0;
            column2 = column1 + targetColumns - 1.0;
            ClampPart();
        }

        public void SetImageSize(int width, int height)
        {
            imageWidth = Math.Max(0, width);
            imageHeight = Math.Max(0, height);
            Fit();
        }

        public void Clear()
        {
            imageWidth = 0;
            imageHeight = 0;
            row1 = 0;
            column1 = 0;
            row2 = 0;
            column2 = 0;
        }

        public void CopyFrom(ImageViewportController source)
        {
            if (source == null)
            {
                Clear();
                return;
            }

            imageWidth = source.imageWidth;
            imageHeight = source.imageHeight;
            row1 = source.row1;
            column1 = source.column1;
            row2 = source.row2;
            column2 = source.column2;
            viewportWidth = source.viewportWidth;
            viewportHeight = source.viewportHeight;
        }

        public void Fit()
        {
            if (!HasImage)
            {
                return;
            }

            row1 = 0;
            column1 = 0;
            row2 = imageHeight - 1;
            column2 = imageWidth - 1;
        }

        public void OriginalSize(Control control)
        {
            if (!HasImage || control == null || control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            double visibleColumns = Math.Max(2, control.ClientSize.Width);
            double visibleRows = Math.Max(2, control.ClientSize.Height);
            viewportWidth = control.ClientSize.Width;
            viewportHeight = control.ClientSize.Height;
            double centerColumn = imageWidth / 2.0;
            double centerRow = imageHeight / 2.0;
            column1 = centerColumn - visibleColumns / 2.0;
            column2 = column1 + visibleColumns - 1.0;
            row1 = centerRow - visibleRows / 2.0;
            row2 = row1 + visibleRows - 1.0;
        }

        public void Apply(HWindow window)
        {
            if (!HasImage)
            {
                return;
            }

            int windowRow, windowColumn, width, height;
            window.GetWindowExtents(out windowRow, out windowColumn, out width, out height);
            double top, left, bottom, right;
            GetDisplayPart(width, height, out top, out left, out bottom, out right);
            window.SetPart(top, left, bottom, right);
        }

        private void GetDisplayPart(int width, int height, out double top, out double left, out double bottom, out double right)
        {
            viewportWidth = Math.Max(1, width);
            viewportHeight = Math.Max(1, height);
            // HALCON stretches SetPart to fill the window. Expand the shorter
            // image-space dimension instead, keeping square pixels and padding.
            double rows = Math.Max(1, row2 - row1 + 1);
            double columns = Math.Max(1, column2 - column1 + 1);
            double scale = Math.Max(rows / Math.Max(1, height), columns / Math.Max(1, width));
            double displayRows = scale * Math.Max(1, height);
            double displayColumns = scale * Math.Max(1, width);
            top = (row1 + row2 + 1 - displayRows) / 2;
            left = (column1 + column2 + 1 - displayColumns) / 2;
            bottom = top + displayRows - 1;
            right = left + displayColumns - 1;
        }

        public PointF WindowToImage(Point point, Control control)
        {
            if (!HasImage || control == null || control.ClientSize.Width <= 0 || control.ClientSize.Height <= 0)
            {
                return PointF.Empty;
            }

            double top, left, bottom, right;
            GetDisplayPart(control.ClientSize.Width, control.ClientSize.Height, out top, out left, out bottom, out right);
            float row = (float)(top + point.Y / (double)control.ClientSize.Height * (bottom - top + 1));
            float column = (float)(left + point.X / (double)control.ClientSize.Width * (right - left + 1));
            return new PointF(column, row);
        }

        public void ZoomAt(Point point, Control control, int mouseDelta)
        {
            if (!HasImage || control == null || control.ClientSize.Width <= 0 || control.ClientSize.Height <= 0 || mouseDelta == 0)
            {
                return;
            }

            double factor = mouseDelta > 0 ? 0.80 : 1.25;
            PointF imagePoint = WindowToImage(point, control);
            double top, left, bottom, right;
            GetDisplayPart(control.ClientSize.Width, control.ClientSize.Height, out top, out left, out bottom, out right);
            double fitScale = Math.Max(imageHeight / (double)viewportHeight, imageWidth / (double)viewportWidth);
            double scale = Clamp((bottom - top + 1) / viewportHeight * factor, 1.0 / 64, Math.Max(1, fitScale) * 8);
            // Zoom around the pointer, not around the center of the selected point.
            row1 = imagePoint.Y - point.Y * scale;
            row2 = row1 + viewportHeight * scale - 1;
            column1 = imagePoint.X - point.X * scale;
            column2 = column1 + viewportWidth * scale - 1;
            ClampPart();
        }

        public void PanByWindowDelta(int deltaX, int deltaY, Control control)
        {
            if (!HasImage || control == null || control.ClientSize.Width <= 0 || control.ClientSize.Height <= 0)
            {
                return;
            }

            double top, left, bottom, right;
            GetDisplayPart(control.ClientSize.Width, control.ClientSize.Height, out top, out left, out bottom, out right);
            double imageDeltaColumn = -deltaX / (double)control.ClientSize.Width * (right - left + 1);
            double imageDeltaRow = -deltaY / (double)control.ClientSize.Height * (bottom - top + 1);

            row1 += imageDeltaRow;
            row2 += imageDeltaRow;
            column1 += imageDeltaColumn;
            column2 += imageDeltaColumn;
            ClampPart();
        }

        private void ClampPart()
        {
            // Limit the center without cropping the view rectangle: cropping it
            // changes magnification when panning a padded or small 1:1 image.
            double rows = Math.Max(1, row2 - row1 + 1);
            double columns = Math.Max(1, column2 - column1 + 1);
            double centerRow = Clamp((row1 + row2 + 1) / 2, 0, imageHeight);
            double centerColumn = Clamp((column1 + column2 + 1) / 2, 0, imageWidth);
            row1 = centerRow - rows / 2;
            row2 = row1 + rows - 1;
            column1 = centerColumn - columns / 2;
            column2 = column1 + columns - 1;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
