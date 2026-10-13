namespace HalconWinFormsDemo.Models
{
    public sealed class ShapeMatchResult
    {
        public int Index { get; set; }

        // Zero-based position in this HALCON search's original tuple order, not a tracking ID.
        public int SourceIndex { get; set; } = -1;

        public double Row { get; set; }

        public double Column { get; set; }

        public double AngleDeg { get; set; }

        public double Scale { get; set; }

        public double Score { get; set; }

        public RoiShapeType? RoiShapeType { get; set; }

        public double RoiWidth { get; set; }

        public double RoiHeight { get; set; }

        public double RoiRadius { get; set; }
    }
}
