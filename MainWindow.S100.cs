using System;
using System.Linq;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private string GetExecutionSkipReasonS100(VmToolInstance tool)
        {
            var condition = tool.GetInputBinding("$RunCondition");
            if (condition != null)
            {
                string error = GetInputBindingConfigurationError(tool, condition);
                if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException("执行条件无效：" + error);
                var producer = GetInputSourceTool(condition);
                if (producer != null && producer.ResultCode == "SKIP") return "条件来源“" + producer.InstanceName + "”已跳过。";
                object value;
                if (producer == null || !producer.TryGetOutputValue(condition.SourcePortName, out value) || !(value is bool) ||
                    (producer.ResultCode != "OK" && producer.ResultCode != "NG") || !string.IsNullOrEmpty(producer.ErrorMessage))
                    throw new InvalidOperationException("执行条件尚无可用布尔值；请先运行来源模块，不能把缺失值当作假。");
                bool expected = !(tool.Parameters ?? new VmToolParameterData()).ExecutionConditionWhenFalse;
                if ((bool)value != expected) return producer.InstanceName + "." + condition.SourcePortName + "=" + ((bool)value ? "真" : "假") + "，本模块要求" + (expected ? "真" : "假") + "。";
            }
            foreach (var binding in tool.InputBindings.Where(item => item.TargetPortName != "$RunCondition"))
            {
                if (tool.Kind == VmToolKind.BranchValueSelection && (binding.TargetPortName == "WhenTrue" || binding.TargetPortName == "WhenFalse")) continue;
                var producer = GetInputSourceTool(binding);
                if (producer != null && producer.ResultCode == "SKIP") return "输入 " + binding.TargetPortName + " 的来源“" + producer.InstanceName + "”已跳过。";
            }
            if (tool.Kind == VmToolKind.NumericJudge && tool.GetInputBinding("Value") == null && !string.IsNullOrWhiteSpace(tool.InputToolId))
            {
                var producer = flowTools.FirstOrDefault(item => item.ToolId == tool.InputToolId);
                if (producer != null && producer.ResultCode == "SKIP") return "数值判定的来源“" + producer.InstanceName + "”已跳过。";
            }
            return null;
        }

        private InspectionRecord CreateConditionSkipS100(VmToolInstance tool, string reason)
        {
            if (string.Equals(toolOverlayProducerToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase)) DisposeToolOverlays();
            tool.ResultCode = "SKIP";
            tool.RunStatus = "条件跳过";
            tool.InputSummary = reason;
            tool.OutputSummary = "未执行；没有本次算法输出。";
            tool.ErrorMessage = string.Empty;
            var record = CreateRecord("ConditionSkip", "SKIP", 0, tool.InstanceName + "：" + reason, tool);
            resultStore.Add(record);
            LogInfo("条件跳过：" + tool.InstanceName + " · " + reason);
            RefreshResultOverlayLayers();
            ScheduleRefreshDisplay();
            return record;
        }

        private System.Collections.Generic.HashSet<string> GetConditionalBranchToolIdsS100()
        {
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tool in flowTools)
                if (tool.GetInputBinding("$RunCondition") != null || tool.InputBindings.Any(binding => ids.Contains(binding.SourceToolId)) ||
                    (tool.Kind == VmToolKind.NumericJudge && ids.Contains(tool.InputToolId ?? string.Empty))) ids.Add(tool.ToolId);
            return ids;
        }
    }
}
