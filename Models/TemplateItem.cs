using System;
using System.IO;
using HalconDotNet;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo.Models
{
    public sealed class TemplateItem : IDisposable
    {
        public TemplateItem()
        {
            Service = new ShapeTemplateService();
            Options = new TemplateMatchOptions();
        }

        public string Name { get; set; }

        public RoiData TemplateRoi { get; set; }

        public HRegion TrainingMask { get; set; }

        public bool TrainingMaskApplied { get; set; }

        public RoiData DisplayFrame { get; set; }

        public TemplateMatchOptions Options { get; set; }

        public ShapeTemplateService Service { get; private set; }

        public bool HasModel
        {
            get { return Service != null && Service.HasModel; }
        }

        public string TemplatePath
        {
            get { return Service == null ? string.Empty : Service.TemplatePath; }
        }

        public string Status
        {
            get
            {
                if (HasModel)
                {
                    return "已训练";
                }

                return TemplateRoi == null ? "待框选" : "已框选";
            }
        }

        public string SourceFile
        {
            get
            {
                return string.IsNullOrWhiteSpace(TemplatePath) ? string.Empty : Path.GetFileName(TemplatePath);
            }
        }

        public void Dispose()
        {
            if (TemplateRoi != null)
            {
                TemplateRoi.Dispose();
                TemplateRoi = null;
            }

            if (TrainingMask != null)
            {
                TrainingMask.Dispose();
                TrainingMask = null;
            }

            if (DisplayFrame != null)
            {
                DisplayFrame.Dispose();
                DisplayFrame = null;
            }

            if (Service != null)
            {
                Service.Dispose();
                Service = null;
            }
        }
    }
}
