using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private long productionAlarmNavigationGenerationS150;
        private sealed class ProductionAlarmTaskIdentityS150
        {
            public string OccurrenceId, SessionId, FlowId, ToolId, DefinitionSha, InstanceStamp;
            public int Version;
        }

        private bool TryResolveProductionAlarmTaskS150(ProductionAlarmWindowS136 window,
            ProductionAlarmTaskIdentityS150 identity, out VmToolInstance tool, out string reason)
        {
            tool = null; reason = string.Empty;
            VmProductionAlarmRecordS136 record;
            if (window == null || !ReferenceEquals(window, productionAlarmWindowS136) || identity == null ||
                !window.IsSelectedEventCurrentS150(identity.OccurrenceId, identity.Version) ||
                !productionAlarmsS136.TryGetValue(identity.OccurrenceId, out record) || record.Version != identity.Version)
            { reason = "所选事件、窗口或实际记录版本已改变；请重新选择，不使用旧动作。"; return false; }
            if (record.WorkspaceSessionId != identity.SessionId || record.FlowId != identity.FlowId || record.ToolId != identity.ToolId ||
                !string.Equals(record.RecipeDefinitionSha256, identity.DefinitionSha, StringComparison.OrdinalIgnoreCase) ||
                identity.SessionId != projectSessionIdS104 || identity.FlowId != activeProjectFlowIdS104 ||
                string.IsNullOrWhiteSpace(identity.ToolId) || string.IsNullOrWhiteSpace(identity.DefinitionSha))
            { reason = "发生身份与当前会话/流程不一致或定义未知；保持只读，不加载历史方案或猜同名模块。"; return false; }
            var matches = flowTools.Where(value => value.ToolId == identity.ToolId).Take(2).ToArray();
            if (matches.Length != 1 || modulePrerequisiteStampsS147.GetValue(matches[0], ignored => new ModulePrerequisiteStampS147()).Value != identity.InstanceStamp)
            { reason = "模块不存在、身份不唯一或原动作实例被替换；不把同ID新实例当旧动作目标。"; return false; }
            try
            {
                string fingerprint = recipeService.GetRecipeFingerprint(CaptureRecipe());
                string hash;
                using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(fingerprint))).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(hash, identity.DefinitionSha, StringComparison.OrdinalIgnoreCase))
                { reason = "当前完整方案定义已改变；发生事实仍可查看，不将历史模块错误推定到新定义。"; return false; }
            }
            catch (Exception ex) { reason = "当前定义无法完整复核，未定位或打开参数：" + ex.Message; return false; }
            tool = matches[0]; return true;
        }

        private bool CanNavigateProductionAlarmTaskS150(out string reason)
        {
            reason = string.Empty;
            if (CanChangeRecipeS134(false) && !recipeChangeoverFaultedS134 && !historicalImageMode && subflowEditorsS102.Count == 0)
                return true;
            reason = "运行、训练、触发、换型故障、历史查看或内部编辑尚未结束；事件事实仍可阅读，暂不切换模块上下文。";
            return false;
        }
        private void ReportProductionAlarmTaskS150(ProductionAlarmWindowS136 window, string message)
        {
            if (window != null && ReferenceEquals(window, productionAlarmWindowS136)) window.SetStatus(message);
            HeaderStatusText.Text = message;
        }

        private void NavigateProductionAlarmTaskS150(VmProductionAlarmActionEventArgsS136 args, bool openParameters)
        {
            try { NavigateProductionAlarmTaskCoreS150(args, openParameters); }
            catch (Exception ex) { ReportProductionAlarmTaskS150(productionAlarmWindowS136, "模块任务未完成：" + ex.Message + "；未报告打开或修复成功。"); }
        }

        private void NavigateProductionAlarmTaskCoreS150(VmProductionAlarmActionEventArgsS136 args, bool openParameters)
        {
            var window = productionAlarmWindowS136;
            long generation = ++productionAlarmNavigationGenerationS150;
            VmProductionAlarmRecordS136 record;
            if (window == null || args == null || !window.IsSelectedEventCurrentS150(args.OccurrenceId, args.ExpectedVersion) ||
                !productionAlarmsS136.TryGetValue(args.OccurrenceId, out record) || record.Version != args.ExpectedVersion)
            { ReportProductionAlarmTaskS150(window, "所选事件已改变，请刷新并重新选择；未打开任何参数。"); return; }
            var matches = flowTools.Where(value => value.ToolId == record.ToolId).Take(2).ToArray();
            if (matches.Length != 1)
            { ReportProductionAlarmTaskS150(window, "当前工程不存在唯一的发生模块；不按名称或顺序替代。"); return; }
            var identity = new ProductionAlarmTaskIdentityS150
            {
                OccurrenceId = record.OccurrenceId, Version = record.Version, SessionId = record.WorkspaceSessionId,
                FlowId = record.FlowId, ToolId = record.ToolId, DefinitionSha = record.RecipeDefinitionSha256,
                InstanceStamp = modulePrerequisiteStampsS147.GetValue(matches[0], ignored => new ModulePrerequisiteStampS147()).Value
            };
            string reason; VmToolInstance tool;
            if (!CanNavigateProductionAlarmTaskS150(out reason) || !TryResolveProductionAlarmTaskS150(window, identity, out tool, out reason))
            { ReportProductionAlarmTaskS150(window, reason); return; }

            if (!openParameters)
            {
                // Do not call FocusFlowGraphTool: that original helper changes the
                // editor view and schedules an unguarded scroll. Selection only
                // leaves the current page and the independent parameter draft intact.
                // Ordinary SelectionChanged can ask to apply/discard a hidden
                // draft. This read-only command must not enter that path.
                bool wasSelecting = flowSelectionUpdating;
                flowSelectionUpdating = true;
                try { FlowToolList.SelectedItem = tool; }
                finally { flowSelectionUpdating = wasSelecting; }
                if (!TryResolveProductionAlarmTaskS150(window, identity, out tool, out reason) || !ReferenceEquals(FlowToolList.SelectedItem, tool))
                { ReportProductionAlarmTaskS150(window, string.IsNullOrWhiteSpace(reason) ? "定位期间选择已改变，未继续滚动。" : reason); return; }
                if (!productionRunMode && FlowToolList.IsVisible) FlowToolList.ScrollIntoView(tool);
                ReportProductionAlarmTaskS150(window, "已选中当前工程模块“" + tool.InstanceName + "”；保持页面和参数窗，不运行或修改定义。");
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
                {
                    VmToolInstance current; string invalid;
                    if (generation != productionAlarmNavigationGenerationS150 || productionRunMode ||
                        FlowGraphViewport == null || !FlowGraphViewport.IsVisible ||
                        !CanNavigateProductionAlarmTaskS150(out invalid) ||
                        !TryResolveProductionAlarmTaskS150(window, identity, out current, out invalid) ||
                        !ReferenceEquals(FlowToolList.SelectedItem, current)) return;
                    double zoom = FlowZoomSlider == null ? 1 : FlowZoomSlider.Value;
                    double x = (current.GraphX + 140) * zoom - FlowGraphViewport.ViewportWidth / 2;
                    double y = (current.GraphY + current.GraphHeight / 2) * zoom - FlowGraphViewport.ViewportHeight / 2;
                    if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)) return;
                    FlowGraphViewport.ScrollToHorizontalOffset(Math.Max(0, x));
                    FlowGraphViewport.ScrollToVerticalOffset(Math.Max(0, y));
                }));
                return;
            }

            if (tool.Kind == VmToolKind.SubflowInput)
            { ReportProductionAlarmTaskS150(window, "当前目标是调用输入边界，不能作为执行模块打开参数；事实保持只读。"); return; }
            if (productionRunMode)
            {
                ToggleProductionModeButton_Click(new Button { Tag = "Developer" }, null);
                if (productionRunMode)
                { ReportProductionAlarmTaskS150(window, "工程页面尚未打开；请完成原显式解锁后重新点击打开参数，没有自动续开或修复。"); return; }
            }
            long lease = CaptureEngineeringEditLeaseS137("打开报警模块参数");
            if (lease < 0) { ReportProductionAlarmTaskS150(window, HeaderStatusText.Text); return; }
            if (!IsEngineeringEditLeaseCurrentS137(lease) || !CanNavigateProductionAlarmTaskS150(out reason) ||
                !TryResolveProductionAlarmTaskS150(window, identity, out tool, out reason))
            { ReportProductionAlarmTaskS150(window, string.IsNullOrWhiteSpace(reason) ? "授权已失效，未打开参数。" : reason); return; }
            if (!ReferenceEquals(dockEditingTool, tool) && !ConfirmDockDraftChanges("打开报警所属模块参数"))
            { ReportProductionAlarmTaskS150(window, "已取消原草稿确认，指定参数未打开；未报告修复成功。"); return; }
            // Confirm may apply another draft or pump messages. Reject a changed
            // event/definition/instance before the original window loads a target.
            if (!IsEngineeringEditLeaseCurrentS137(lease) || !CanNavigateProductionAlarmTaskS150(out reason) ||
                !TryResolveProductionAlarmTaskS150(window, identity, out tool, out reason))
            { ReportProductionAlarmTaskS150(window, string.IsNullOrWhiteSpace(reason) ? "确认后授权已失效，未打开参数。" : reason); return; }
            // The original small window owns the only draft and its confirmation.
            // Keep S96 return history; opening is not F5, apply or fault recovery.
            OpenCentralModuleConfigurationS77(tool, true);
            if (!centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, tool))
            { ReportProductionAlarmTaskS150(window, "指定模块参数未打开；取消原草稿确认不代表定位或修复成功。"); return; }
            if (!IsEngineeringEditLeaseCurrentS137(lease) || !CanNavigateProductionAlarmTaskS150(out reason) ||
                !TryResolveProductionAlarmTaskS150(window, identity, out tool, out reason) || !ReferenceEquals(dockEditingTool, tool))
            { ReportProductionAlarmTaskS150(window, string.IsNullOrWhiteSpace(reason) ? "开窗期间授权或上下文已改变；原窗保留，未继续操作。" : reason); return; }
            ReportProductionAlarmTaskS150(window, "已打开“" + tool.InstanceName + "”的原参数窗；不自动修复、运行、重连、回传或解除故障。");
        }
    }
}
