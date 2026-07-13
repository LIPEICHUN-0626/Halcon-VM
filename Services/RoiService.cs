using System;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class RoiService : IDisposable
    {
        private RoiData currentRoi;

        public RoiData CurrentRoi
        {
            get { return currentRoi; }
        }

        public RoiData DrawRectangle(HWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            double row1;
            double column1;
            double row2;
            double column2;
            window.SetColor("yellow");
            window.DrawRectangle1(out row1, out column1, out row2, out column2);

            ReplaceCurrent(RoiData.CreateRectangle(row1, column1, row2, column2));
            return currentRoi;
        }

        public RoiData DrawCircle(HWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            double row;
            double column;
            double radius;
            window.SetColor("cyan");
            window.DrawCircle(out row, out column, out radius);

            ReplaceCurrent(RoiData.CreateCircle(row, column, radius));
            return currentRoi;
        }

        public RoiData SaveCurrent()
        {
            if (currentRoi == null)
            {
                throw new InvalidOperationException("当前没有 ROI 可保存。");
            }

            return currentRoi.Clone();
        }

        public void SetCurrent(RoiData roi)
        {
            ReplaceCurrent(roi);
        }

        public void Clear()
        {
            ReplaceCurrent(null);
        }

        public void Dispose()
        {
            Clear();
        }

        private void ReplaceCurrent(RoiData roi)
        {
            if (currentRoi != null)
            {
                currentRoi.Dispose();
            }

            currentRoi = roi;
        }
    }
}
