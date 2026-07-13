namespace HalconWinFormsDemo.Models
{
    public sealed class TemplateMatchOptions
    {
        public TemplateMatchOptions()
        {
            MinScore = 0.60;
            MaxMatches = 1;
            AngleStartDeg = -180;
            AngleExtentDeg = 360;
            MaxOverlap = 0.5;
            Greediness = 0.9;
            NumLevels = "auto";
            Metric = "use_polarity";
            SubPixel = "least_squares";
            LimitToSearchRoi = false;
        }

        public double MinScore { get; set; }

        public int MaxMatches { get; set; }

        public double AngleStartDeg { get; set; }

        public double AngleExtentDeg { get; set; }

        public double MaxOverlap { get; set; }

        public double Greediness { get; set; }

        public string NumLevels { get; set; }

        public string Metric { get; set; }

        public string SubPixel { get; set; }

        public bool LimitToSearchRoi { get; set; }
    }
}
