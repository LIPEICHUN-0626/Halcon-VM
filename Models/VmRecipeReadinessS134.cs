using System.Collections.Generic;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    // This is a detached static report, not an execution result or a production safety certificate.
    public sealed class VmRecipeReadinessIssueS134
    {
        public string Category { get; internal set; }
        public string Reason { get; internal set; }
        public string FlowId { get; internal set; }
        public string FlowName { get; internal set; }
        public string ToolId { get; internal set; }
        public string ToolName { get; internal set; }
        public bool IsBlocking { get; internal set; }
        public string SeverityText { get { return IsBlocking ? "静态阻断" : "待运行确认"; } }
        public string DisplayText
        {
            get { return Category + " · " + FlowName + (string.IsNullOrEmpty(ToolName) ? string.Empty : " / " + ToolName); }
        }
    }

    public sealed class VmRecipeReadinessReportS134
    {
        public string Name { get; internal set; }
        public int FlowCount { get; internal set; }
        public int ToolCount { get; internal set; }
        public int ResourceCount { get; internal set; }
        public List<VmRecipeReadinessIssueS134> Issues { get; internal set; } = new List<VmRecipeReadinessIssueS134>();
        public bool CanApply { get { return !Issues.Any(item => item.IsBlocking); } }
        public string SummaryText
        {
            get
            {
                int blocked = Issues.Count(item => item.IsBlocking);
                return (blocked == 0 ? "目标流程静态项目无阻断" : "目标换型有 " + blocked + " 项静态阻断") +
                    " · 全方案 " + FlowCount + " 流程 · 目标范围 " + ToolCount + " 模块 / " + ResourceCount +
                    " 文件资源。尚未连接设备、取图或执行算法，不代表可以开始生产。";
            }
        }
    }
}
