using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void AddConfigurationReadinessIssuesS97(List<VmConnectionIssueS69> issues, VmToolInstance tool, int index)
        {
            var parameters = tool.Parameters;
            if (parameters == null)
            {
                AddModulePrerequisiteReadinessIssuesS147(issues, tool, index);
                return;
            }
            string error = parameters.Validate(tool.Kind);
            if (!string.IsNullOrWhiteSpace(error)) AddReadinessIssueS97(issues, tool, index, "参数", error);
            if (tool.Kind == VmToolKind.ImageSource && !string.IsNullOrWhiteSpace(parameters.LocalImagePath) && !File.Exists(parameters.LocalImagePath))
                AddReadinessIssueS97(issues, tool, index, "图像文件", "本地图像不存在或不可访问。打开参数重新选择图片。原路径：" + parameters.LocalImagePath);
            if (tool.Kind == VmToolKind.ShapeMatch && !string.IsNullOrWhiteSpace(parameters.ShapeModelPath) && !File.Exists(parameters.ShapeModelPath))
                AddReadinessIssueS97(issues, tool, index, "模板文件", "Shape 模板不存在或不可访问。打开模板匹配重新选择或制作模板。原路径：" + parameters.ShapeModelPath);
            AddModulePrerequisiteReadinessIssuesS147(issues, tool, index);
        }

        private void AddModulePrerequisiteReadinessIssuesS147(List<VmConnectionIssueS69> issues, VmToolInstance tool, int index)
        {
            if (!IsModulePrerequisiteKindS147(tool.Kind) || modulePrerequisiteScanIncompleteS147 ||
                (modulePrerequisiteScopeS147 != null && (!modulePrerequisiteScopeS147.Contains(tool.ToolId) ||
                    modulePrerequisiteConditionalIdsS147.Contains(tool.ToolId)))) return;
            foreach (var fact in GetModulePrerequisiteFactsS147(tool))
            {
                // Reserve one of the 200 rows for an explicit incomplete marker.
                if (modulePrerequisiteFactCountS147 >= 199)
                {
                    modulePrerequisiteScanIncompleteS147 = true;
                    issues.Add(new VmConnectionIssueS69
                    {
                        Key = tool.ToolId + "|prerequisite|scan-budget", TargetToolId = tool.ToolId,
                        TargetPortName = string.Empty, TargetText = tool.SequenceText + "  " + tool.InstanceName,
                        DataType = "未知", SourceText = "本次已应用根配置范围", SeverityText = "未知",
                        ReasonText = "模块前提超过本次200条预算，后续范围未完整检查；不能当作没有问题。请缩小真实执行范围或先处理已列出的缺口。",
                        SeverityBrush = CreateIssueBrushS69("#DC2626"), IsBlocking = true, CanSupplement = false,
                        ToolIndex = index, PortIndex = -1, IssueCategory = "模块前提预算", OpensParameters = false
                    });
                    return;
                }
                modulePrerequisiteFactCountS147++;
                var parameterIssue = fact.Kind != "Input" && fact.Key.EndsWith("invalid", StringComparison.Ordinal)
                    ? issues.FirstOrDefault(issue => issue.TargetToolId == tool.ToolId && issue.IssueCategory == "参数" && issue.PrerequisiteS147 == null)
                    : null;
                if (parameterIssue != null)
                {
                    parameterIssue.PrerequisiteS147 = fact;
                    continue;
                }
                issues.Add(new VmConnectionIssueS69
                {
                    Key = fact.Kind == "Input" ? tool.ToolId + "|" + fact.PortName : fact.Key,
                    TargetToolId = tool.ToolId, TargetPortName = fact.PortName,
                    TargetText = tool.SequenceText + "  " + tool.InstanceName,
                    DataType = "模块前提", SourceText = "当前已应用定义（非运行证明）", ReasonText = fact.Reason,
                    SeverityText = fact.IsBlocking ? "前提" : "未知", SeverityBrush = CreateIssueBrushS69(fact.IsBlocking ? "#DC2626" : "#D97706"),
                    IsBlocking = fact.IsBlocking, CanSupplement = false, ToolIndex = index, PortIndex = -1,
                    IssueCategory = "模块前提", OpensParameters = !string.IsNullOrWhiteSpace(fact.ActionText), PrerequisiteS147 = fact
                });
            }
        }

        private void AddReadinessIssueS97(List<VmConnectionIssueS69> issues, VmToolInstance tool, int index, string category, string reason)
        {
            issues.Add(new VmConnectionIssueS69
            {
                Key = tool.ToolId + "|readiness|" + category,
                TargetToolId = tool.ToolId, TargetPortName = string.Empty,
                TargetText = tool.SequenceText + "  " + tool.InstanceName,
                DataType = category, SourceText = "当前已应用参数", ReasonText = reason,
                SeverityText = category == "参数" ? "参数" : "文件",
                SeverityBrush = CreateIssueBrushS69("#DC2626"), IsBlocking = true,
                CanSupplement = false, ToolIndex = index, PortIndex = -1,
                IssueCategory = category, OpensParameters = true
            });
        }

        private bool ShowBlockingRunReadinessS97(VmFlowRunRequestMode mode)
        {
            // Use the real execution range. An unrelated disabled/out-of-range module must not block this request.
            var plan = VmFlowExecutionPlanner.BuildPlan(flowTools, FlowToolList.SelectedItem as VmToolInstance, mode);
            var selectedIds = new HashSet<string>(plan.Tools.Select(tool => tool.ToolId), StringComparer.OrdinalIgnoreCase);
            RebuildConnectionIssuesS69(false, selectedIds);
            var conditionalIds = GetConditionalBranchToolIdsS100();
            var blocking = connectionIssuesS69.FirstOrDefault(issue => issue.IsBlocking && selectedIds.Contains(issue.TargetToolId) &&
                (!conditionalIds.Contains(issue.TargetToolId) || issue.TargetPortName == "$RunCondition"));
            if (blocking == null) return false;
            if (FlowMoreToggleButton != null) FlowMoreToggleButton.IsChecked = false;
            FlowConnectionIssuePanel.Visibility = Visibility.Visible;
            FlowConnectionIssueListBox.SelectedItem = blocking;
            FlowConnectionIssueListBox.ScrollIntoView(blocking);
            RefreshSelectedConnectionIssueS69();
            HeaderStatusText.Text = "本次运行尚未开始：" + blocking.TargetText + " · " + blocking.IssueCategory + "。请从问题清单打开对应配置。";
            return true;
        }
    }
}
