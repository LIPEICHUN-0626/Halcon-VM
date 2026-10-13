using System.Collections.Generic;

namespace HalconWinFormsDemo.Models
{
    // A detached, read-only review result; never an instruction to apply a definition.
    public sealed class VmRecipeDiffReportS140
    {
        public List<VmRecipeDiffItemS140> Items { get; internal set; } = new List<VmRecipeDiffItemS140>();
        public bool IsComplete { get; internal set; }
        public string SummaryText { get; internal set; }
        public string WarningText { get; internal set; }
        public string CurrentIdentity { get; internal set; }
        public string TargetIdentity { get; internal set; }
    }

    public sealed class VmRecipeDiffItemS140
    {
        public string ChangeText { get; internal set; }
        public string AreaText { get; internal set; }
        public string ObjectText { get; internal set; }
        public string FieldText { get; internal set; }
        public string CurrentText { get; internal set; }
        public string TargetText { get; internal set; }
        public string DetailText { get; internal set; }
        public bool IsDisplay { get; internal set; }
        public bool IsUnknown { get; internal set; }
    }
}
