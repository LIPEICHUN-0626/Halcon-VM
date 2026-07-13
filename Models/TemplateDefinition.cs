using System;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    public sealed class TemplateDefinition : IDisposable
    {
        public const int CurrentMetadataVersion = 2;

        public TemplateDefinition()
        {
            MetadataVersion = CurrentMetadataVersion;
            Options = new TemplateMatchOptions();
        }

        public int MetadataVersion { get; set; }

        public string TemplateName { get; set; }

        public RoiData TemplateRoi { get; set; }

        public HRegion TrainingMask { get; set; }

        public RoiData DisplayFrame { get; set; }

        public TemplateMatchOptions Options { get; set; }

        public string TemplatePath { get; set; }

        public TemplateDefinition Clone()
        {
            return new TemplateDefinition
            {
                MetadataVersion = MetadataVersion,
                TemplateName = TemplateName,
                TemplateRoi = TemplateRoi == null ? null : TemplateRoi.Clone(),
                TrainingMask = CloneRegion(TrainingMask),
                DisplayFrame = DisplayFrame == null ? null : DisplayFrame.Clone(),
                Options = CloneOptions(Options),
                TemplatePath = TemplatePath
            };
        }

        public void Dispose()
        {
            if (TrainingMask != null)
            {
                TrainingMask.Dispose();
                TrainingMask = null;
            }

            if (TemplateRoi != null)
            {
                TemplateRoi.Dispose();
                TemplateRoi = null;
            }

            if (DisplayFrame != null)
            {
                DisplayFrame.Dispose();
                DisplayFrame = null;
            }
        }

        public static HRegion CloneRegion(HRegion source)
        {
            if (source == null)
            {
                return null;
            }

            HObject copy;
            HOperatorSet.CopyObj(source, out copy, 1, -1);
            return new HRegion(copy);
        }

        public static TemplateMatchOptions CloneOptions(TemplateMatchOptions source)
        {
            if (source == null)
            {
                return new TemplateMatchOptions();
            }

            return new TemplateMatchOptions
            {
                MinScore = source.MinScore,
                MaxMatches = source.MaxMatches,
                AngleStartDeg = source.AngleStartDeg,
                AngleExtentDeg = source.AngleExtentDeg,
                MaxOverlap = source.MaxOverlap,
                Greediness = source.Greediness,
                NumLevels = source.NumLevels,
                Metric = source.Metric,
                SubPixel = source.SubPixel,
                LimitToSearchRoi = source.LimitToSearchRoi
            };
        }
    }
}
