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

                double zoom = imageWidth / Math.Max(1.0, column2 - column1 + 1.0);
                return string.Format("Zoom: {0:F0}%", zoom * 100.0);
            }
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

            double visibleColumns = Math.Min(imageWidth, Math.Max(2, control.Width));
            double visibleRows = Math.Min(imageHeight, Math.Max(2, control.Height));
            double centerColumn = imageWidth / 2.0;
            double centerRow = imageHeight / 2.0;
            column1 = centerColumn - visibleColumns / 2.0;
            column2 = column1 + visibleColumns - 1.0;
            row1 = centerRow - visibleRows / 2.0;
            row2 = row1 + visibleRows - 1.0;
            ClampPart();
        }

        public void Apply(HWindow window)
        {
            if (!HasImage)
            {
                return;
            }

            window.SetPart((int)Math.Round(row1), (int)Math.Round(column1), (int)Math.Round(row2), (int)Math.Round(column2));
        }

        public PointF WindowToImage(Point point, Control control)
        {
            if (!HasImage || control.Width <= 0 || control.Height <= 0)
            {
                return PointF.Empty;
            }

            double visibleRows = row2 - row1 + 1.0;
            double visibleColumns = column2 - column1 + 1.0;
            float row = (float)(row1 + point.Y / (double)control.Height * visibleRows);
            float column = (float)(column1 + point.X / (double)control.Width * visibleColumns);
            return new PointF(column, row);
        }

        public void ZoomAt(Point point, Control control, int mouseDelta)
        {
            if (!HasImage || control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            double factor = mouseDelta > 0 ? 0.80 : 1.25;
            PointF imagePoint = WindowToImage(point, control);
            double currentRows = Math.Max(2, row2 - row1 + 1.0);
            double currentColumns = Math.Max(2, column2 - column1 + 1.0);
            double newRows = Clamp(currentRows * factor, 16, imageHeight);
            double newColumns = Clamp(currentColumns * factor, 16, imageWidth);

            row1 = imagePoint.Y - newRows / 2.0;
            row2 = row1 + newRows - 1.0;
            column1 = imagePoint.X - newColumns / 2.0;
            column2 = column1 + newColumns - 1.0;
            ClampPart();
        }

        public void PanByWindowDelta(int deltaX, int deltaY, Control control)
        {
            if (!HasImage || control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            double visibleRows = row2 - row1 + 1.0;
            double visibleColumns = column2 - column1 + 1.0;
            double imageDeltaColumn = -deltaX / (double)control.Width * visibleColumns;
            double imageDeltaRow = -deltaY / (double)control.Height * visibleRows;

            row1 += imageDeltaRow;
            row2 += imageDeltaRow;
            column1 += imageDeltaColumn;
            column2 += imageDeltaColumn;
            ClampPart();
        }

        private void ClampPart()
        {
            double rows = Math.Min(imageHeight, Math.Max(2, row2 - row1 + 1.0));
            double columns = Math.Min(imageWidth, Math.Max(2, column2 - column1 + 1.0));

            if (row1 < 0)
            {
                row1 = 0;
                row2 = rows - 1;
            }

            if (column1 < 0)
            {
                column1 = 0;
                column2 = columns - 1;
            }

            if (row2 >= imageHeight)
            {
                row2 = imageHeight - 1;
                row1 = Math.Max(0, row2 - rows + 1);
            }

            if (column2 >= imageWidth)
            {
                column2 = imageWidth - 1;
                column1 = Math.Max(0, column2 - columns + 1);
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
