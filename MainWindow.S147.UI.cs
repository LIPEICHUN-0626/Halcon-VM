using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private long modulePrerequisiteNavigationGenerationS147;

        private static bool HasModulePrerequisiteRouteS147(ModulePrerequisiteFactS147 fact)
        {
            return fact != null && !string.IsNullOrWhiteSpace(fact.ActionText) &&
                (fact.Kind == "Input" || fact.Kind == "Template" || fact.Kind == "Reference" || fact.Kind == "Calibration");
        }

        private bool CanNavigateModulePrerequisiteS147()
        {
            if (!CanNavigateInputRepairS96()) return false;
            if (!CanChangeRecipeS134(false))
            {
                HeaderStatusText.Text = "运行、训练、触发或工程切换尚未结束；缺口说明仍可阅读，停止后再打开修复任务。";
                return false;
            }
            return true;
        }

        private bool RejectModulePrerequisiteNavigationS147(string reason)
        {
            modulePrerequisiteNavigationGenerationS147++;
            // Refresh the existing read-only guidance, not the whole legacy file/
            // algorithm readiness scan. Its explicit refresh command remains intact.
            RefreshSelectedConnectionIssueS69();
            RefreshLearningGuideS146();
            HeaderStatusText.Text = reason;
            return false;
        }

        private bool OpenModulePrerequisiteTaskS147(ModulePrerequisiteFactS147 fact)
        {
            VmToolInstance tool; ModulePrerequisiteFactS147 currentFact;
            if (!IsModulePrerequisiteFactCurrentS147(fact, out tool, out currentFact))
                return RejectModulePrerequisiteNavigationS147("此动作的方案、流程、模块实例或定义缺口已改变；请刷新清单，不按名称猜测替代模块。");
            if (!HasModulePrerequisiteRouteS147(currentFact))
                return RejectModulePrerequisiteNavigationS147("只读说明：" + currentFact.Reason + "；没有可推定的修复动作，也不代表准备通过。");
            if (!CanNavigateModulePrerequisiteS147()) return false;

            if (productionRunMode)
            {
                // Keep the original station authorization and explicit unlock path.
                // Do not directly toggle productionRunMode or create permissions.
                ToggleProductionModeButton_Click(new Button { Tag = "Developer" }, null);
                if (productionRunMode) return false;
                if (!CanNavigateModulePrerequisiteS147()) return false;
                if (!IsModulePrerequisiteFactCurrentS147(fact, out tool, out currentFact))
                    return RejectModulePrerequisiteNavigationS147("返回工程页面时身份或缺口已改变，请重新选择任务；尚未打开修复位置。");
            }

            long lease = CaptureEngineeringEditLeaseS137(currentFact.ActionText);
            if (lease < 0) return false; // Locked facts remain readable in the existing list/guide.
            VmToolInstance expectedTool = tool;
            if (!IsEngineeringEditLeaseCurrentS137(lease) || !CanNavigateModulePrerequisiteS147()) return false;
            if (!IsModulePrerequisiteFactCurrentS147(fact, out tool, out currentFact) || !ReferenceEquals(tool, expectedTool))
                return RejectModulePrerequisiteNavigationS147("工程授权确认后模块或已应用缺口已改变，请刷新；未执行任何修复。");

            // The original small window owns the only draft. It asks about previous
            // unapplied changes. Preserve S96 producer/consumer return navigation.
            OpenCentralModuleConfigurationS77(tool, true);
            if (!centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, expectedTool))
                return RejectModulePrerequisiteNavigationS147("指定模块任务未打开；取消原草稿确认时不会继续导航，也不会报告修复成功。");
            if (!IsEngineeringEditLeaseCurrentS137(lease) || !CanNavigateModulePrerequisiteS147()) return false;
            if (!IsModulePrerequisiteFactCurrentS147(fact, out tool, out currentFact) || !ReferenceEquals(tool, expectedTool))
                return RejectModulePrerequisiteNavigationS147("开窗期间模块或已应用缺口已改变；原窗保留，未定位或自动修复，请刷新。");

            FrameworkElement target;
            if (!SelectModulePrerequisiteTaskS147(currentFact, expectedTool, out target))
                return RejectModulePrerequisiteNavigationS147("该事实没有对应的当前原任务/端口，不能打开近似位置；请刷新或查看原模块帮助。");
            if (!IsEngineeringEditLeaseCurrentS137(lease) || !CanNavigateModulePrerequisiteS147()) return false;
            if (!IsModulePrerequisiteFactCurrentS147(fact, out tool, out currentFact) || !ReferenceEquals(tool, expectedTool) ||
                !centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, expectedTool))
                return RejectModulePrerequisiteNavigationS147("任务展开期间身份、授权或缺口已改变，未继续聚焦；这不是修复完成。");

            string actionText = currentFact.ActionText;
            long generation = ++modulePrerequisiteNavigationGenerationS147;
            // WPF must finish attaching/expanding the original content before scroll.
            // Revalidate the same facts/lease again; an old callback must never focus
            // a new project, a replacement instance or another user's task.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                if (generation != modulePrerequisiteNavigationGenerationS147) return;
                if (!centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, expectedTool)) return;
                if (!IsEngineeringEditLeaseCurrentS137(lease, false) || !CanNavigateModulePrerequisiteS147())
                { HeaderStatusText.Text = "任务聚焦已停止：授权或忙碌状态已改变；未自动修复或执行。"; return; }
                VmToolInstance resolved; ModulePrerequisiteFactS147 refreshed;
                if (!IsModulePrerequisiteFactCurrentS147(fact, out resolved, out refreshed) || !ReferenceEquals(resolved, expectedTool))
                { RejectModulePrerequisiteNavigationS147("当前任务身份或缺口已失效，请刷新；没有使用旧动作聚焦新模块。"); return; }
                if (target == null || !target.IsVisible) return;
                if (refreshed.Kind == "Input" && refreshed.PortName != "ROI" && refreshed.PortName != "SearchROI")
                {
                    var row = DockInputPortItemsControl.Items.OfType<VmInputPortEditorRow>().FirstOrDefault(item => item.PortName == refreshed.PortName);
                    if (row == null) { RejectModulePrerequisiteNavigationS147("当前输入端口已不在原编辑表，请刷新；未按显示顺序猜端口。"); return; }
                    DockInputPortItemsControl.SelectedItem = row;
                    DockInputPortItemsControl.ScrollIntoView(row);
                    var container = DockInputPortItemsControl.ItemContainerGenerator.ContainerFromItem(row) as FrameworkElement;
                    if (container != null) { container.BringIntoView(); container.Focus(); }
                }
                else { target.BringIntoView(); target.Focus(); }
            }));
            HeaderStatusText.Text = "已打开“" + expectedTool.InstanceName + "”的“" + actionText + "”原任务；只导航，不自动读图、训练、采参考、采点、运行或应用。";
            return true;
        }

        private bool SelectModulePrerequisiteTaskS147(ModulePrerequisiteFactS147 fact, VmToolInstance tool, out FrameworkElement target)
        {
            target = null;
            if (fact.Kind == "Input")
            {
                if (fact.PortName == "ROI" || fact.PortName == "SearchROI")
                {
                    SelectCentralConfigurationTaskS77(DockRoiTaskTab, "Roi");
                    target = DockRoiSourceModeComboBox;
                    return target != null;
                }
                var row = DockInputPortItemsControl.Items.OfType<VmInputPortEditorRow>().FirstOrDefault(item => item.PortName == fact.PortName);
                if (row == null) return false;
                SelectCentralConfigurationTaskS77(DockIoTaskTab, "Io");
                DockInputPortItemsControl.SelectedItem = row;
                DockInputPortItemsControl.ScrollIntoView(row);
                target = DockInputPortItemsControl;
                return true;
            }

            bool template = fact.Kind == "Template" && (tool.Kind == VmToolKind.NccTemplateMatch || tool.Kind == VmToolKind.ShapeMatch);
            bool reference = fact.Kind == "Reference" && tool.Kind == VmToolKind.PoseImageAlignment;
            bool calibration = fact.Kind == "Calibration" && tool.Kind == VmToolKind.PlanarCalibration2D;
            if (!template && !reference && !calibration) return false;
            DockInstanceParameterGroup.Visibility = Visibility.Collapsed;
            SelectCentralConfigurationTaskS77(DockParametersTaskTab, "Parameters");
            // Reveal the existing single More; never make a second parameter state
            // or alter the common-field profile, canvas scale or node positions.
            dockAdvancedParameterExpandedByKind[tool.Kind] = true;
            UpdateDockAdvancedParameterLayout(tool);
            if (template && tool.Kind == VmToolKind.NccTemplateMatch)
            {
                DockNccTrainingS131.IsExpanded = true;
                target = DockNccReadS131;
            }
            else if (template)
            {
                ShapeTrainingExpanderS89.IsExpanded = true;
                target = ShapeTrainingPreviewS89;
            }
            else if (reference) target = DockAlignmentCaptureS132;
            else target = tool.Parameters == null || string.IsNullOrWhiteSpace(tool.Parameters.PlanarCalibrationUnitS124)
                ? (FrameworkElement)DockPlanarUnitS124 : DockPlanarWorldXS124;
            return target != null;
        }
    }
}
