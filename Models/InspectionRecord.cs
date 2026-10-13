using System;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    public sealed class InspectionRecord : IDisposable
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string RecipeName { get; set; }
        public string FlowId { get; set; }
        public string FlowName { get; set; }
        public string WorkspaceSessionId { get; set; }
        public string ExecutionScope { get; set; }
        public string ToolId { get; set; }
        public string ToolName { get; set; }
        public bool? IsRootTool { get; set; }
        public string FlowDisplay { get { return string.IsNullOrWhiteSpace(FlowId) ? "旧记录 · 流程未知" : FlowName; } }
        public string OriginDisplay { get { return (string.IsNullOrWhiteSpace(RecipeName) ? "未命名方案" : RecipeName) + " / " + FlowDisplay; } }

        public string ImageSource { get; set; }

        public string InspectionType { get; set; }

        public RoiData Roi { get; set; }

        public string RoiType
        {
            get { return Roi == null ? string.Empty : Roi.ShapeType.ToString(); }
        }

        public string TemplatePath { get; set; }

        public double? MatchRow { get; set; }

        public double? MatchColumn { get; set; }

        public double? MatchAngle { get; set; }

        public string ResultCode { get; set; }

        public double Score { get; set; }

        public string Message { get; set; }

        public HImage ImageSnapshot { get; set; }

        public void Dispose()
        {
            if (Roi != null)
            {
                Roi.Dispose();
                Roi = null;
            }

            if (ImageSnapshot != null)
            {
                ImageSnapshot.Dispose();
                ImageSnapshot = null;
            }
        }
    }
}
