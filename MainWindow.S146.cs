using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class LearningStepS146
        {
            public string ToolId, Title, Instruction, Action;
            public override string ToString() { return Title; }
        }

        // Guidance keeps only stable identities and prose, never algorithm state
        // or SDK objects. Its temporary step number is deliberately not a recipe.
        private sealed class LearningSessionS146
        {
            public string ProjectKey, SessionId, FlowId, Title;
            public List<LearningStepS146> Steps;
            public List<string> ToolIds;
            public Dictionary<string, string> ToolStampsS148;
            public int Index;
        }

        private LearningSessionS146 learningSessionS146;
        private Window learningGuideWindowS146;
        private TextBlock learningStepTitleS146, learningStepInstructionS146, learningStepStateS146;
        private TextBlock learningStepGapS148, learningStepDetailS148;
        private Expander learningStepDetailsS148;
        private string learningGuideStepKeyS148;
        private Button learningStepActionS146, learningPreviousS146, learningNextS146;
        private ComboBox learningStepsListS146;
        private bool learningGuideUpdatingS146;
        private ModulePrerequisiteFactS147 learningPrerequisiteS147;

        private static bool IsLearningProjectKeyS146(string key)
        {
            return key == "project-object-production" || key == "project-alignment-line" || key == "project-world-distance";
        }

        private void LoadObjectProductionLearningProjectS146_Click(object sender, RoutedEventArgs e)
        { LoadExampleProgramS78("project-object-production"); }
        private void LoadAlignmentLineLearningProjectS146_Click(object sender, RoutedEventArgs e)
        { LoadExampleProgramS78("project-alignment-line"); }
        private void LoadWorldDistanceLearningProjectS146_Click(object sender, RoutedEventArgs e)
        { LoadExampleProgramS78("project-world-distance"); }

        private void CompleteLearningProjectLoadS146(string key, string title)
        {
            if (learningGuideWindowS146 != null) learningGuideWindowS146.Close();
            learningSessionS146 = null;
            if (!IsLearningProjectKeyS146(key)) return;
            var tools = flowTools.ToList();
            if (tools.Count == 0 || tools.Count > 12 || tools.Any(tool => string.IsNullOrWhiteSpace(tool.ToolId)) ||
                tools.Select(tool => tool.ToolId).Distinct(StringComparer.Ordinal).Count() != tools.Count)
            { HeaderStatusText.Text = "学习流程身份/节点数量不符合边界，未创建猜测式导航。"; return; }
            var steps = tools.Select(tool => new LearningStepS146
            {
                ToolId = tool.ToolId, Title = LearningProseS146(tool.InstanceName, 120), Action = "Module",
                Instruction = LearningInstructionS146(key, tool)
            }).ToList();
            steps.Add(new LearningStepS146 { Title = "查看本次生产结果", Action = "Production",
                Instruction = "先应用所有草稿，再主动进入运行页面并执行完整流程。预配结果卡只读取本流程、本次真实生产周期；工程F5、跳过、失败或缺值不会变成旧生产值。学习门槛不是产品出厂标准。此按钮只切换页面，不运行或启用触发。" });
            steps.Add(new LearningStepS146 { Title = "另存为自己的工程", Action = "Save",
                Instruction = "选择自己的新文件路径，保存真实模块、订阅、已应用参数、模板/参考/标定定义与生产布局；未应用草稿不在正式定义中。另存窗口由你确认，不自动覆盖原工程。步骤号仅供本次学习，重开工程不会自动训练或执行。" });
            steps.Add(new LearningStepS146 { Title = "工程随行与目标机器准备", Action = "Export",
                Instruction = "打开原随行清单，核对实际图片、模型与依赖，再主动导出独立副本。资源缺失或环境未知必须先处理；复制JSON不是部署就绪，不安装运行库、连接设备或执行外部代码。" });
            learningSessionS146 = new LearningSessionS146
            {
                ProjectKey = key, Title = title, SessionId = projectSessionIdS104, FlowId = activeProjectFlowIdS104,
                ToolIds = tools.Select(tool => tool.ToolId).ToList(), Steps = steps,
                ToolStampsS148 = tools.ToDictionary(tool => tool.ToolId, LearningToolStampS148, StringComparer.Ordinal)
            };
            ShowLearningGuideS146();
        }

        private static string LearningInstructionS146(string key, VmToolInstance tool)
        {
            switch (tool.Kind)
            {
                case VmToolKind.ImageSource:
                    return "在常用页选择真实图片，主动F5预览、确认并应用，再读取一帧供下游使用。应用会清除此前草稿输出。自带图片缺失时换成自己的图；存在图片不表示算法已经准备或产品合格。";
                case VmToolKind.ImageThreshold:
                    return "调整灰度范围，使需要的对象成为真实区域；F5看图确认后应用。不要把背景、噪声或阈值生成成功当产品合格，区域预算仍按原对象工具执行。";
                case VmToolKind.ConnectedObjectAnalysisS144:
                    return "查看同序中心和面积，按需打开对象明细；查看高亮不改正式选取。明确排序/0起编号，F5确认后应用。空域或越界不会补零对象，不把对象编号当跟踪ID。";
                case VmToolKind.NumberArrayStatistics:
                    return "输入已连接真实对象面积数组。查看统计量并确认输入来源；空数组按原契约处理，不从日志或旧值补数。统计成功不是质量通过。";
                case VmToolKind.NccTemplateMatch:
                    return "先运行图像源，在本模块读取输入并框选/制作模板，应用后重新运行。更多参数里明确排序和选取；选中目标来自本次真实候选，查看不改旧最佳输出。缺模型、无匹配或编号越界时不造位姿。";
                case VmToolKind.PoseImageAlignment:
                    return "先应用NCC并重新运行，再显式采当前SelectedPose作固定参考，F5确认后应用。点击下一步不会采参考。对齐输出是独立坐标的真实Image；NCC尺度1，原图ROI不能偷偷沿用到对齐链。";
                case VmToolKind.ImageEdgeLineFindingS143:
                    return key == "project-alignment-line"
                        ? "先得到真实对齐图，再在该模块编辑搜索带到直边，设置法向/极性，F5查看有效点、缺点和覆盖。不要用原图坐标代替对齐图，也不要把画出的搜索基线当找到的结果。"
                        : "在真实直边上编辑搜索带，设置法向/极性后F5；KeptPoints是本次质量保留点。端点范围随搜索带而变，不自动等于工件物理尺寸；缺线不继续用旧点。";
                case VmToolKind.TypedPointSetLineFittingMeasurement:
                    return "输入连接找线的真实KeptPoints。F5查看鲁棒拟合和正式StartPoint/EndPoint，它们不是从摘要猜来的点；只有有效同链点才可交给标定，必要时调整采点与拟合参数。";
                case VmToolKind.PlanarCalibration2D:
                    return "在同一原图采至少三个非退化像素/世界对应点，填写真实单位并应用；界面缺省mm不是实测单位证据，不预造毫米比例。输入PointA/B已连拟合线两端。F5查看残差与WorldPointA/B；这是平面映射，不是相机内参、畸变修正或现场精度认证。";
                case VmToolKind.WorldPointPairMeasurement:
                    return "输入连接同一标定的WorldPointA/B，F5看真实世界距离及单位。同标定、同图像链、同单位检查继续生效；拟合边缘段长度不自动代表稳定物理尺寸。";
                case VmToolKind.NumericJudge:
                    return "在输入页核对当前实际测量来源，在常用页按自己的任务设置上下限；F5后应用。示例门槛仅供学习，不能代替实物、公差和量测研究。Passed才是当前判定，异常或缺值不视为通过。";
                case VmToolKind.ImageFilter:
                    return "输入页中的$RunCondition已连接数量判定的Passed；只在当前判定通过时运行，未通过显示跳过。它演示真实条件调度，不把跳过当成功，不沿用上次滤波输出；整条流程关闭遇NG停止以便看清条件路径。";
                case VmToolKind.GrayStat:
                    return tool.GetInputBinding("$RunCondition") == null
                        ? "核对正式Image/ROI来源，F5查看真实有效域内的灰度统计；应用草稿后再继续。统计成功不是产品合格。"
                        : "输入页的$RunCondition已连接数量判定Passed；通过才测量原图灰度，未通过显示跳过、不沿用旧值。整条流程关闭遇NG停止以便看清条件路径，数量合格也不代表灰度已合格。";
                default:
                    return "双击式打开原参数窗，依次查看常用、输入、ROI和结果；主动F5当前模块、确认并应用，再继续。学习步骤不自动改参、运行或替你确认结果。";
            }
        }

        private bool IsLearningSessionCurrentS146()
        {
            var session = learningSessionS146;
            if (session == null || session.SessionId != projectSessionIdS104 || session.FlowId != activeProjectFlowIdS104 ||
                session.Steps == null || session.Steps.Count == 0 || session.Steps.Count > 15 || session.Index < 0 || session.Index >= session.Steps.Count ||
                session.ToolIds == null || session.ToolIds.Count == 0 || session.ToolIds.Count > 12 || session.ToolStampsS148 == null ||
                session.ToolStampsS148.Count != session.ToolIds.Count) return false;
            foreach (string id in session.ToolIds)
            {
                var matches = flowTools.Where(tool => tool.ToolId == id).Take(2).ToArray();
                string expected;
                if (matches.Length != 1 || !session.ToolStampsS148.TryGetValue(id, out expected) || LearningToolStampS148(matches[0]) != expected) return false;
            }
            return true;
        }

        private string LearningToolStampS148(VmToolInstance tool)
        { return modulePrerequisiteStampsS147.GetValue(tool, ignored => new ModulePrerequisiteStampS147()).Value; }

        private static string LearningProseS146(string text, int limit)
        { return string.IsNullOrEmpty(text) ? "—" : text.Length <= limit ? text : text.Substring(0, limit) + "…"; }

        private void OpenLearningProjectGuideS146_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLearningSessionCurrentS146())
            { HeaderStatusText.Text = "当前没有本次装载的完整学习项目，或方案/流程/模块身份已改变；请主动选择完整项目。保存工程仍可在原模块帮助中学习。"; return; }
            ShowLearningGuideS146();
        }

        private void ShowLearningGuideS146()
        {
            if (learningGuideWindowS146 != null) { RefreshLearningGuideS146(); if (ShowActivated) learningGuideWindowS146.Activate(); return; }
            var root = new StackPanel { Margin = new Thickness(12) };
            learningStepTitleS146 = new TextBlock { FontSize = 15, FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 0, 6) };
            learningStepInstructionS146 = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 7), Foreground = Brushes.DimGray };
            learningStepGapS148 = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 5), Visibility = Visibility.Collapsed };
            learningStepStateS146 = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 11,
                Foreground = Brushes.SlateGray, Margin = new Thickness(0, 0, 0, 10) };
            var actions = new DockPanel { LastChildFill = true };
            learningStepActionS146 = new Button { Padding = new Thickness(9, 4, 9, 4), Margin = new Thickness(0, 0, 8, 0) };
            var help = new Button { Content = "模块帮助", Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(0, 0, 6, 0),
                ToolTip = LearningHoverS148("查看原当前模块帮助，不运行、不改变学习进度或工程定义。") };
            learningStepsListS146 = new ComboBox { MaxDropDownHeight = 260, Margin = new Thickness(0, 0, 0, 9),
                ToolTip = LearningHoverS148("直接选择学习步骤；只换提示，不切换正在编辑的模块。点击下方任务按钮才打开模块，不自动运行。") };
            learningStepDetailS148 = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, FontSize = 11 };
            var detailContent = new StackPanel { Margin = new Thickness(4, 5, 4, 2) };
            detailContent.Children.Add(learningStepDetailS148);
            learningStepDetailsS148 = new Expander { Header = "说明", IsExpanded = false, Content = detailContent, Margin = new Thickness(0, 9, 0, 0),
                ToolTip = LearningHoverS148("完整技术说明、实际状态和其他步骤按需展开；临时步号不是准备完成或验收。") };
            var navigation = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            learningPreviousS146 = new Button { Content = "上一步", Margin = new Thickness(0, 0, 5, 0), Padding = new Thickness(6, 3, 6, 3) };
            learningNextS146 = new Button { Content = "下一步", Padding = new Thickness(6, 3, 6, 3),
                ToolTip = LearningHoverS148("只切换当前任务，不读取、训练、采参考/标定、应用、运行或标记完成。") };
            navigation.Children.Add(help); navigation.Children.Add(learningPreviousS146); navigation.Children.Add(learningNextS146);
            DockPanel.SetDock(navigation, Dock.Right); actions.Children.Add(navigation);
            learningStepActionS146.HorizontalAlignment = HorizontalAlignment.Left; actions.Children.Add(learningStepActionS146);
            root.Children.Add(learningStepTitleS146); root.Children.Add(learningStepsListS146);
            root.Children.Add(learningStepInstructionS146); root.Children.Add(learningStepGapS148);
            root.Children.Add(learningStepStateS146); root.Children.Add(actions); root.Children.Add(learningStepDetailsS148);
            var window = new Window { Owner = this, Title = "学习 · 当前任务", Width = 430, MinWidth = 360, MinHeight = 210, MaxHeight = 530, SizeToContent = SizeToContent.Height,
                Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            window.ShowActivated = ShowActivated;
            if (!ShowActivated) { window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = Left; window.Top = Top; }
            learningGuideWindowS146 = window;
            learningGuideStepKeyS148 = null;
            learningPreviousS146.Click += delegate { MoveLearningStepS146(-1); };
            learningNextS146.Click += delegate { MoveLearningStepS146(1); };
            learningStepActionS146.Click += delegate { InvokeLearningStepS146(); };
            help.Click += delegate
            {
                if (!IsLearningSessionCurrentS146()) { RefreshLearningGuideS146(); return; }
                var step = learningSessionS146.Steps[learningSessionS146.Index];
                var tool = flowTools.FirstOrDefault(item => item.ToolId == step.ToolId);
                OpenHelpCenterS78(tool == null ? (VmToolKind?)null : tool.Kind);
            };
            learningStepsListS146.SelectionChanged += delegate
            {
                if (!learningGuideUpdatingS146 && IsLearningSessionCurrentS146() && learningStepsListS146.SelectedIndex >= 0)
                { learningSessionS146.Index = learningStepsListS146.SelectedIndex; RefreshLearningGuideS146(); }
            };
            window.Activated += delegate { RefreshLearningGuideS146(); };
            window.Closed += delegate
            {
                if (!ReferenceEquals(learningGuideWindowS146, window)) return;
                learningGuideWindowS146 = null; learningPrerequisiteS147 = null; learningGuideStepKeyS148 = null;
            };
            RefreshLearningGuideS146(); window.Show();
        }

        private void MoveLearningStepS146(int delta)
        {
            if (IsLearningSessionCurrentS146())
                learningSessionS146.Index = Math.Max(0, Math.Min(learningSessionS146.Steps.Count - 1, learningSessionS146.Index + delta));
            RefreshLearningGuideS146();
        }

        private void RefreshLearningGuideS146()
        {
            if (learningGuideWindowS146 == null) return;
            bool current = IsLearningSessionCurrentS146();
            learningStepActionS146.IsEnabled = current; learningPreviousS146.IsEnabled = current; learningNextS146.IsEnabled = current;
            learningStepsListS146.IsEnabled = current;
            if (!current)
            {
                learningPrerequisiteS147 = null;
                learningStepTitleS146.Text = "学习导航已失效"; learningStepInstructionS146.Text = "原工程或模块实例已改变，请重新选择学习项目。";
                learningStepGapS148.Visibility = Visibility.Visible; learningStepGapS148.Text = "不按同名、同编号或位置替代原模块。";
                learningStepGapS148.Foreground = Brushes.DarkGoldenrod;
                learningStepStateS146.Text = "旧步骤不代表当前工程状态。"; learningStepActionS146.Content = "项目已改变";
                learningStepDetailS148.Text = "方案/流程切换、删除重建或撤销后的同ID替换会使导航失效。原实例字符串身份不匹配时不打开替代模块；重新装载完整项目沿原未保存修改确认。此处不修改工程或补旧输出。";
                learningStepDetailsS148.IsExpanded = false;
                return;
            }
            var session = learningSessionS146; var step = session.Steps[session.Index];
            string stepKey = session.SessionId + "|" + session.FlowId + "|" + session.Index.ToString(CultureInfo.InvariantCulture);
            if (learningGuideStepKeyS148 != stepKey) { learningStepDetailsS148.IsExpanded = false; learningGuideStepKeyS148 = stepKey; }
            learningStepTitleS146.Text = "学习步骤 " + (session.Index + 1).ToString(CultureInfo.InvariantCulture) + " / " + session.Steps.Count;
            learningStepTitleS146.ToolTip = LearningHoverS148(session.Title + "\n" + step.Title);
            var tool = flowTools.FirstOrDefault(item => item.ToolId == step.ToolId);
            learningStepInstructionS146.Text = LearningBriefInstructionS148(step, tool);
            learningStepInstructionS146.ToolTip = LearningHoverS148(step.Instruction);
            string actual = tool == null ? "当前是页面/保存任务，步骤导航不会执行流程。"
                : "配置：" + LearningProseS146(tool.ConfigurationStatus, 120) + "\n运行：" + LearningProseS146(tool.RunStatus, 120) +
                    "\n结果：" + LearningProseS146(tool.ResultCode, 60) + (string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "" : "\n实际错误：" + LearningProseS146(tool.ErrorMessage, 800));
            learningStepStateS146.Text = tool == null ? "仅任务导航 · 不代表验收"
                : "状态：" + LearningProseS146(tool.ConfigurationStatus, 24) + " · " + LearningProseS146(tool.RunStatus, 24) + " · " + LearningProseS146(tool.ResultCode, 16);
            learningStepStateS146.ToolTip = LearningHoverS148(actual + "\n这些是已有实际状态，不是产品合格或能力验收。" +
                (tool == null ? "" : "\nToolId=" + tool.ToolId));
            learningStepActionS146.Content = step.Action == "Module" ? "打开当前模块" : step.Action == "Production" ? "进入运行页面" : step.Action == "Save" ? "另存工程…" : "打开随行清单…";
            learningPrerequisiteS147 = tool == null ? null : GetModulePrerequisiteFactsS147(tool).FirstOrDefault();
            learningStepGapS148.Visibility = tool == null ? Visibility.Collapsed : Visibility.Visible;
            string fullDefinitionHint;
            if (learningPrerequisiteS147 != null)
            {
                bool hasRoute = HasModulePrerequisiteRouteS147(learningPrerequisiteS147);
                // Keep the next action on the surface; full qualification and
                // technical boundaries remain available in hover and details.
                string briefReason = (learningPrerequisiteS147.Reason ?? string.Empty).Split(new[] { '。', '；', '\n' })[0];
                learningStepGapS148.Text = (hasRoute ? "先处理：" : "未知：") + LearningProseS146(briefReason, 48);
                learningStepGapS148.Foreground = Brushes.DarkGoldenrod;
                learningStepActionS146.Content = hasRoute ? learningPrerequisiteS147.ActionText : "查看未知说明";
                learningStepActionS146.ToolTip = LearningHoverS148(hasRoute
                    ? "打开原精确任务；不主动读图、训练、采参考/标定、运行或应用。原窗已有预览照常展示，不代表正式输入/模型资格。"
                    : "仅展开当前完整未知说明，不猜测修复位置，不修改或运行工程；未知不能当准备通过。");
                fullDefinitionHint = learningPrerequisiteS147.Reason;
            }
            else
            {
                learningStepActionS146.ToolTip = LearningHoverS148("只执行按钮所写的操作，不自动训练、采参考/标定、运行或标记学习通过。");
                fullDefinitionHint = tool == null ? "学习步号是临时导航，不是算法状态或准备检查。"
                    : !tool.IsEnabled ? "当前模块停用；不会运行，也不表示准备通过。"
                    : "未发现此类已应用定义缺口；提示范围仅模板、固定参考、平面标定及明确输入，实际输入、算法和设备仍待运行。";
                if (tool != null)
                {
                    learningStepGapS148.Text = !tool.IsEnabled ? "模块已停用，不会执行"
                        : ReferenceEquals(dockEditingTool, tool) && dockDraftDirty ? "当前有未应用参数；试运行后确认，再应用"
                        : tool.Kind == VmToolKind.ImageSource && tool.Parameters != null &&
                            VmImageSourceModeS114.Normalize(tool.Parameters.ImageSourceModeS114) == VmImageSourceModeS114.File && string.IsNullOrWhiteSpace(tool.Parameters.LocalImagePath)
                            ? "先选择图片，再明确读取；不会自动取图"
                        : !tool.LastOutputUtc.HasValue ? "尚无当前输出；应用后重新运行，再继续下游"
                        : "已有运行记录；修改参数后需应用并重新运行";
                    learningStepGapS148.Foreground = Brushes.SlateGray;
                }
            }
            learningStepGapS148.ToolTip = LearningHoverS148(fullDefinitionHint);
            learningStepDetailS148.Text = step.Instruction + "\n\n定义提示：" + fullDefinitionHint + "\n\n已有实际状态：\n" + actual +
                "\n\n修改参数后的顺序：F5试草稿 → 应用 → 重新运行，再继续下游。应用会清除旧输出；只试草稿不能代替已应用定义的正式结果。" +
                "\n\n选择步骤或上一步/下一步只导航；训练、参考、标定、F5、应用、运行与保存仍须明确操作。临时步号不是准备通过、产品合格或现场验收。";
            learningPreviousS146.IsEnabled = session.Index > 0; learningNextS146.IsEnabled = session.Index < session.Steps.Count - 1;
            learningGuideUpdatingS146 = true;
            try { learningStepsListS146.ItemsSource = session.Steps; learningStepsListS146.SelectedIndex = session.Index; }
            finally { learningGuideUpdatingS146 = false; }
        }

        private void InvokeLearningStepS146()
        {
            if (!IsLearningSessionCurrentS146()) { RefreshLearningGuideS146(); return; }
            var step = learningSessionS146.Steps[learningSessionS146.Index];
            if (step.Action == "Module" && learningPrerequisiteS147 != null && !HasModulePrerequisiteRouteS147(learningPrerequisiteS147))
            {
                // Unknown is an actual read-only action, not a blank/disabled repair.
                // Reading remains available while locked or busy; no navigation into
                // an inferred model/input and no SDK/model operations are performed.
                RefreshLearningGuideS146();
                learningStepDetailsS148.IsExpanded = true;
                learningStepDetailS148.BringIntoView();
                return;
            }
            if (TemplateTargetInteractionBusyS145()) { HeaderStatusText.Text = "运行、训练或工程切换尚未结束；学习说明仍可阅读，请停止后再操作。"; return; }
            if (step.Action == "Module")
            {
                if (learningPrerequisiteS147 != null)
                {
                    OpenModulePrerequisiteTaskS147(learningPrerequisiteS147);
                    RefreshLearningGuideS146();
                    return;
                }
                if (productionRunMode)
                {
                    ToggleProductionModeButton_Click(new Button { Tag = "Developer" }, null);
                    if (productionRunMode || !IsLearningSessionCurrentS146()) return;
                }
                var tool = flowTools.Single(item => item.ToolId == step.ToolId);
                OpenCentralModuleConfigurationS77(tool);
                if (!IsLearningSessionCurrentS146() || !centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, tool))
                { RefreshLearningGuideS146(); return; }
            }
            else if (step.Action == "Production") ToggleProductionModeButton_Click(new Button { Tag = "Production" }, null);
            else if (step.Action == "Save") SaveRecipeAsButton_Click(null, null);
            else if (step.Action == "Export") OpenEngineeringPackageS139();
            RefreshLearningGuideS146();
        }

        private static string LearningBriefInstructionS148(LearningStepS146 step, VmToolInstance tool)
        {
            if (step.Action == "Production") return "进入运行页面后，主动运行完整流程看本周期结果。";
            if (step.Action == "Save") return "另存到自己选择的新路径，保存已应用工程定义。";
            if (step.Action == "Export") return "在原清单核对资源后，主动选择位置导出独立副本。";
            if (tool == null) return "原模块已不可定位，请重新选择学习项目。";
            switch (tool.Kind)
            {
                case VmToolKind.ImageSource: return "选择图片 → F5预览 → 应用 → 再读一帧供下游使用。";
                case VmToolKind.ImageThreshold: return "调整灰度范围 → F5看区域 → 应用 → 重跑后继续下游。";
                case VmToolKind.ConnectedObjectAnalysisS144: return "查看对象面积；需要选取时明确排序与0起编号。";
                case VmToolKind.NumberArrayStatistics: return "核对本次真实面积数组，主动试运行查看统计。";
                case VmToolKind.NccTemplateMatch: return "先运行图像源，再在本模块读取、制作并应用模板。";
                case VmToolKind.PoseImageAlignment: return "先运行已应用的定位，再明确采固定参考并应用。";
                case VmToolKind.ImageEdgeLineFindingS143: return "把搜索带放在真实直边，主动F5查看有效点与覆盖。";
                case VmToolKind.TypedPointSetLineFittingMeasurement: return "核对本次保留边缘点，主动F5查看拟合端点。";
                case VmToolKind.PlanarCalibration2D: return "在同链图像采至少3组对应点，填写真实单位后应用。";
                case VmToolKind.WorldPointPairMeasurement: return "核对同一标定的世界点，主动试运行查看距离与单位。";
                case VmToolKind.NumericJudge: return "核对输入并设置学习上下限，试运行后应用。";
                case VmToolKind.ImageFilter: return "核对Passed条件，主动运行流程后看跳过或本次输出。";
                case VmToolKind.GrayStat: return "核对正式图像与条件，主动试运行查看本次统计。";
                default: return "打开原参数窗，主动试运行、确认并应用。";
            }
        }

        private static ToolTip LearningHoverS148(string text)
        {
            return new ToolTip { Background = Brushes.AliceBlue, Foreground = Brushes.SlateGray, BorderBrush = Brushes.LightSlateGray,
                BorderThickness = new Thickness(1), Padding = new Thickness(8), MaxWidth = 390,
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap } };
        }
    }
}
