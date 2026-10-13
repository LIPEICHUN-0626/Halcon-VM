using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class VmConnectionIssueS69
        {
            public string Key { get; set; }
            public string TargetToolId { get; set; }
            public string TargetPortName { get; set; }
            public string TargetText { get; set; }
            public string DataType { get; set; }
            public string SourceText { get; set; }
            public string ReasonText { get; set; }
            public string SeverityText { get; set; }
            public Brush SeverityBrush { get; set; }
            public bool IsBlocking { get; set; }
            public bool CanSupplement { get; set; }
            public int ToolIndex { get; set; }
            public int PortIndex { get; set; }
            public string IssueCategory { get; set; }
            public bool OpensParameters { get; set; }
            public ModulePrerequisiteFactS147 PrerequisiteS147 { get; set; }
        }

        private readonly ObservableCollection<VmConnectionIssueS69> connectionIssuesS69 = new ObservableCollection<VmConnectionIssueS69>();

        private static Brush CreateIssueBrushS69(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private void OpenConnectionIssuesButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlowMoreToggleButton != null) FlowMoreToggleButton.IsChecked = false;
            if (FlowConnectionInspectorBorder != null) FlowConnectionInspectorBorder.Visibility = Visibility.Collapsed;
            if (FlowPortInspectorBorder != null) FlowPortInspectorBorder.Visibility = Visibility.Collapsed;
            RebuildConnectionIssuesS69(true);
            FlowConnectionIssuePanel.Visibility = Visibility.Visible;
            HeaderStatusText.Text = connectionIssuesS69.Count == 0
                ? "未发现当前启用模块的此类定义、连接或已配置文件问题；实际输入、算法及设备仍待运行。"
                : "已列出定义前提、连接、参数、文件及上游等待项；清单不会自动修复或执行。";
        }

        private void CloseConnectionIssuePanelButton_Click(object sender, RoutedEventArgs e)
        {
            FlowConnectionIssuePanel.Visibility = Visibility.Collapsed;
            HeaderStatusText.Text = "已关闭连接问题任务清单；流程和 InputBindings 未被修改。";
        }

        private void RefreshConnectionIssuesButton_Click(object sender, RoutedEventArgs e)
        {
            RebuildConnectionIssuesS69(true);
            HeaderStatusText.Text = "已刷新当前已应用定义前提、参数、文件引用和必需输入；等待或未知不表示准备通过。";
        }

        private void RebuildConnectionIssuesS69(bool preserveSelection, ISet<string> scopeS147 = null)
        {
            string selectedKey = preserveSelection && FlowConnectionIssueListBox != null && FlowConnectionIssueListBox.SelectedItem is VmConnectionIssueS69
                ? ((VmConnectionIssueS69)FlowConnectionIssueListBox.SelectedItem).Key
                : null;
            List<VmConnectionIssueS69> rebuilt = new List<VmConnectionIssueS69>();
            ResetModulePrerequisiteScanS147(scopeS147);
            for (int toolIndex = 0; toolIndex < flowTools.Count; toolIndex++)
            {
                VmToolInstance tool = flowTools[toolIndex];
                if (!tool.IsEnabled || (scopeS147 != null && !scopeS147.Contains(tool.ToolId))) continue;
                AddConfigurationReadinessIssuesS97(rebuilt, tool, toolIndex);
                IList<VmPortDefinition> ports = ToolMetadata.GetInstanceInputPorts(tool);

                if (tool.Kind == VmToolKind.PositionCorrection2D)
                {
                    BuildPositionCorrectionIssuesS69(rebuilt, tool, toolIndex, ports);
                    continue;
                }

                for (int portIndex = 0; portIndex < ports.Count; portIndex++)
                {
                    VmPortDefinition port = ports[portIndex];
                    if (!IsRequiredConnectionPortS69(tool, port)) continue;
                    VmToolInputBindingData binding = GetEffectiveInputBindingS69(tool, port);
                    if (binding == null && HasImplicitInputS69(tool, port)) continue;
                    AddConnectionIssueIfNeededS69(rebuilt, tool, toolIndex, port, portIndex, binding);
                }
            }

            connectionIssuesS69.Clear();
            foreach (VmConnectionIssueS69 issue in rebuilt.OrderBy(item => item.ToolIndex).ThenBy(item => item.PortIndex))
                connectionIssuesS69.Add(issue);

            int blocking = connectionIssuesS69.Count(item => item.IsBlocking);
            int unknown = connectionIssuesS69.Count(item => item.IssueCategory == "模块前提预算" ||
                (item.PrerequisiteS147 != null && item.PrerequisiteS147.Kind == "Unknown"));
            int waiting = connectionIssuesS69.Count(item => !item.IsBlocking && (item.PrerequisiteS147 == null || item.PrerequisiteS147.Kind != "Unknown"));
            if (OpenConnectionIssuesButton != null)
                OpenConnectionIssuesButton.Content = blocking > 0 ? "连接问题 " + blocking.ToString(CultureInfo.InvariantCulture) : "连接问题";
            if (FlowConnectionIssueSummaryText != null)
                FlowConnectionIssueSummaryText.Text = connectionIssuesS69.Count == 0
                    ? "未发现此类定义、连接或已配置文件问题；实际输入/算法仍待运行"
                    : blocking.ToString(CultureInfo.InvariantCulture) + " 项需处理 · " + waiting.ToString(CultureInfo.InvariantCulture) + " 项待运行（不阻断全流程）" +
                        (unknown > 0 ? " · " + unknown.ToString(CultureInfo.InvariantCulture) + " 项未知/未完整，不能当准备通过" : "");
            if (FlowConnectionIssueEmptyText != null) FlowConnectionIssueEmptyText.Visibility = connectionIssuesS69.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (FlowConnectionIssueListBox != null)
            {
                FlowConnectionIssueListBox.Visibility = connectionIssuesS69.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                FlowConnectionIssueListBox.SelectedItem = connectionIssuesS69.FirstOrDefault(item => string.Equals(item.Key, selectedKey, StringComparison.OrdinalIgnoreCase)) ?? connectionIssuesS69.FirstOrDefault();
            }
            RefreshSelectedConnectionIssueS69();
        }

        private void BuildPositionCorrectionIssuesS69(List<VmConnectionIssueS69> issues, VmToolInstance tool, int toolIndex, IList<VmPortDefinition> ports)
        {
            VmPortDefinition pose = ports.FirstOrDefault(item => string.Equals(item.PortName, "Pose2D", StringComparison.OrdinalIgnoreCase));
            VmToolInputBindingData poseBinding = pose == null ? null : tool.GetInputBinding(pose.PortName);
            VmPortDefinition[] legacyPorts = ports.Where(item => item.PortName == "CurrentRow" || item.PortName == "CurrentColumn" || item.PortName == "CurrentAngle").ToArray();
            bool hasAnyLegacy = legacyPorts.Any(item => tool.GetInputBinding(item.PortName) != null);
            if (poseBinding != null)
            {
                AddConnectionIssueIfNeededS69(issues, tool, toolIndex, pose, ports.IndexOf(pose), poseBinding);
                return;
            }
            if (!hasAnyLegacy)
            {
                AddConnectionIssueS69(issues, tool, toolIndex, pose, ports.IndexOf(pose), null, "未连接 Pose2D；也未连接兼容的 CurrentRow/CurrentColumn/CurrentAngle 三路 Number。", true);
                return;
            }
            foreach (VmPortDefinition port in legacyPorts)
                AddConnectionIssueIfNeededS69(issues, tool, toolIndex, port, ports.IndexOf(port), tool.GetInputBinding(port.PortName));
        }

        private bool IsRequiredConnectionPortS69(VmToolInstance tool, VmPortDefinition port)
        {
            if (port.PortName == "$RunCondition") return tool.GetInputBinding(port.PortName) != null;
            if (tool.Kind == VmToolKind.GlobalVariableWrite)
            {
                var variable = globalVariables.FirstOrDefault(item => tool.Parameters != null && item.VariableId == tool.Parameters.GlobalVariableId);
                return variable != null && port.PortName == VmGlobalVariableType.GetWritePortName(variable.ValueType);
            }
            if (!port.IsOptional) return true;
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            if (tool.Kind == VmToolKind.NumericCalculation && port.PortName == "NumberB")
                return VmNumericCalculationOperation.RequiresSecondOperand(parameters.NumericCalculationOperation);
            if (tool.Kind == VmToolKind.ResultLogicAggregation && port.PortName == "ResultCodeB")
                return VmResultLogicOperation.RequiresSecondInput(parameters.ResultLogicOperation);
            return false;
        }

        private VmToolInputBindingData GetEffectiveInputBindingS69(VmToolInstance tool, VmPortDefinition port)
        {
            VmToolInputBindingData binding = tool.GetInputBinding(port.PortName);
            if (binding == null && tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value" && !string.IsNullOrWhiteSpace(tool.InputToolId))
            {
                binding = new VmToolInputBindingData
                {
                    TargetPortName = port.PortName,
                    SourceToolId = tool.InputToolId,
                    SourcePortName = tool.InputPortName
                };
            }
            return binding;
        }

        private bool HasImplicitInputS69(VmToolInstance tool, VmPortDefinition port)
        {
            if (string.Equals(port.PortName, "Image", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(port.PortName, "ROI", StringComparison.OrdinalIgnoreCase) || string.Equals(port.PortName, "SearchROI", StringComparison.OrdinalIgnoreCase))
                return GetBoundRoiLayers(tool).Any(item => !item.IsMask);
            return false;
        }

        private void AddConnectionIssueIfNeededS69(List<VmConnectionIssueS69> issues, VmToolInstance tool, int toolIndex, VmPortDefinition port, int portIndex, VmToolInputBindingData binding)
        {
            if (binding == null)
            {
                AddConnectionIssueS69(issues, tool, toolIndex, port, portIndex, null, "必需输入尚未连接。", true);
                return;
            }
            string error = tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value"
                ? GetNumericJudgeConfigurationError(tool)
                : GetInputBindingConfigurationError(tool, binding);
            if (string.IsNullOrWhiteSpace(error) && IsDuplicateMultiInputEndpoint(tool, port.PortName, binding.SourceToolId, binding.SourcePortName))
                error = "与同一模块的另一输入重复订阅同一个上游端点。";
            if (!string.IsNullOrWhiteSpace(error))
            {
                AddConnectionIssueS69(issues, tool, toolIndex, port, portIndex, binding, error, true);
                return;
            }
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            bool hasValue = source != null && source.TryGetOutputValue(binding.SourcePortName, out raw) && raw != null && !IsDisposedRuntimeValueS69(raw);
            if (source == null || !string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !hasValue)
            {
                string reason = source == null
                    ? "来源已失效。"
                    : (!string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)
                        ? "连接结构有效，但来源尚未在本周期产生 OK 结果；当前值不可用于执行。"
                        : "连接结构有效，但来源端口尚无可用本周期值或快照已释放。");
                AddConnectionIssueS69(issues, tool, toolIndex, port, portIndex, binding, reason, false);
            }
        }

        private static bool IsDisposedRuntimeValueS69(object raw)
        {
            VmImageSnapshot image = raw as VmImageSnapshot;
            if (image != null) return image.IsDisposed;
            VmRegionSnapshot region = raw as VmRegionSnapshot;
            if (region != null) return region.IsDisposed;
            VmXldSnapshot xld = raw as VmXldSnapshot;
            return xld != null && xld.IsDisposed;
        }

        private void AddConnectionIssueS69(List<VmConnectionIssueS69> issues, VmToolInstance tool, int toolIndex, VmPortDefinition port, int portIndex, VmToolInputBindingData binding, string reason, bool blocking)
        {
            if (tool == null || port == null) return;
            string key = tool.ToolId + "|" + port.PortName;
            // S147 has already recorded this exact missing/invalid subscription.
            // Keep its stable repair identity instead of duplicating the same row.
            if (issues.Any(item => item.Key == key && item.PrerequisiteS147 != null)) return;
            VmToolInstance source = GetInputSourceTool(binding);
            string sourceText = binding == null
                ? "未连接"
                : (source == null ? (binding.SourceToolId ?? "?") : source.SequenceText + " " + source.InstanceName) + "." + (binding.SourcePortName ?? "?");
            issues.Add(new VmConnectionIssueS69
            {
                Key = key,
                TargetToolId = tool.ToolId,
                TargetPortName = port.PortName,
                TargetText = tool.SequenceText + "  " + tool.InstanceName + "." + port.PortName,
                DataType = port.DataType,
                SourceText = sourceText,
                ReasonText = reason,
                SeverityText = blocking ? "连接" : "待运行",
                IssueCategory = blocking ? "连接配置" : "上游待运行",
                SeverityBrush = CreateIssueBrushS69(blocking ? "#DC2626" : "#D97706"),
                IsBlocking = blocking,
                CanSupplement = blocking,
                ToolIndex = toolIndex,
                PortIndex = portIndex
            });
        }

        private void FlowConnectionIssueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshSelectedConnectionIssueS69();
        }

        private void RefreshSelectedConnectionIssueS69()
        {
            VmConnectionIssueS69 issue = FlowConnectionIssueListBox == null ? null : FlowConnectionIssueListBox.SelectedItem as VmConnectionIssueS69;
            int index = issue == null ? -1 : connectionIssuesS69.IndexOf(issue);
            if (FlowConnectionIssuePositionText != null)
                FlowConnectionIssuePositionText.Text = index < 0 ? "-- / --" : (index + 1).ToString(CultureInfo.InvariantCulture) + " / " + connectionIssuesS69.Count.ToString(CultureInfo.InvariantCulture);
            if (FlowConnectionIssueTargetText != null) FlowConnectionIssueTargetText.Text = issue == null ? "--" : issue.TargetText;
            if (FlowConnectionIssueTypeText != null) FlowConnectionIssueTypeText.Text = issue == null ? "--" : issue.DataType;
            if (FlowConnectionIssueSourceText != null) FlowConnectionIssueSourceText.Text = issue == null ? "--" : issue.SourceText;
            if (FlowConnectionIssueReasonText != null) FlowConnectionIssueReasonText.Text = issue == null ? "--" : issue.ReasonText;
            if (OpenConnectionIssueSourceButton != null) OpenConnectionIssueSourceButton.IsEnabled = issue != null && issue.CanSupplement && issue.PrerequisiteS147 == null;
            if (OpenConnectionIssueIoButtonS97 != null)
            {
                var prerequisite = issue == null ? null : issue.PrerequisiteS147;
                bool incomplete = issue != null && issue.IssueCategory == "模块前提预算";
                OpenConnectionIssueIoButtonS97.Content = incomplete ? "提示不完整" : prerequisite != null
                    ? (HasModulePrerequisiteRouteS147(prerequisite) ? prerequisite.ActionText : "暂无修复动作")
                    : issue != null && issue.OpensParameters ? "修改参数" : "修改输入";
                OpenConnectionIssueIoButtonS97.IsEnabled = issue != null && !incomplete && (prerequisite == null || HasModulePrerequisiteRouteS147(prerequisite));
                OpenConnectionIssueIoButtonS97.ToolTip = incomplete ? issue.ReasonText + "\n这是范围未完整检查，不是某个输入错误；不猜测修复位置。" : prerequisite == null
                    ? "打开原配置任务；不会自动运行或修复。"
                    : prerequisite.Reason + "\n只打开原任务，不自动读图、训练、采参考、采点、试运行或应用；锁定时仍可查看本说明。";
            }
        }

        private void PreviousConnectionIssueButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedConnectionIssueS69(-1);
        }

        private void NextConnectionIssueButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedConnectionIssueS69(1);
        }

        private void MoveSelectedConnectionIssueS69(int delta)
        {
            if (connectionIssuesS69.Count == 0) return;
            int index = FlowConnectionIssueListBox.SelectedIndex;
            if (index < 0) index = 0;
            index = (index + delta + connectionIssuesS69.Count) % connectionIssuesS69.Count;
            FlowConnectionIssueListBox.SelectedIndex = index;
            FlowConnectionIssueListBox.ScrollIntoView(FlowConnectionIssueListBox.SelectedItem);
        }

        private VmConnectionIssueS69 GetSelectedConnectionIssueS69()
        {
            return FlowConnectionIssueListBox == null ? null : FlowConnectionIssueListBox.SelectedItem as VmConnectionIssueS69;
        }

        private void LocateConnectionIssueButton_Click(object sender, RoutedEventArgs e)
        {
            VmConnectionIssueS69 issue = GetSelectedConnectionIssueS69();
            if (issue != null && issue.PrerequisiteS147 != null)
            {
                VmToolInstance currentTool; ModulePrerequisiteFactS147 currentFact;
                if (!IsModulePrerequisiteFactCurrentS147(issue.PrerequisiteS147, out currentTool, out currentFact))
                { RejectModulePrerequisiteNavigationS147("本项身份或定义前提已改变，请刷新后定位。"); return; }
            }
            FocusFlowGraphTool(issue == null ? null : issue.TargetToolId, issue == null ? "请选择一个连接问题。" : "已定位连接问题目标：" + issue.TargetText);
        }

        private void FlowConnectionIssueListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LocateConnectionIssueButton_Click(sender, e);
        }

        private void OpenConnectionIssueIoButton_Click(object sender, RoutedEventArgs e)
        {
            VmConnectionIssueS69 issue = GetSelectedConnectionIssueS69();
            if (issue != null && issue.IssueCategory == "模块前提预算")
            {
                HeaderStatusText.Text = "模块前提提示未完整，请缩小实际运行范围或先处理已列问题；没有可推定的输入修复位置。";
                return;
            }
            if (issue != null && issue.PrerequisiteS147 != null)
            {
                OpenModulePrerequisiteTaskS147(issue.PrerequisiteS147);
                return;
            }
            VmToolInstance tool = issue == null ? null : flowTools.FirstOrDefault(item => string.Equals(item.ToolId, issue.TargetToolId, StringComparison.OrdinalIgnoreCase));
            if (tool == null)
            {
                HeaderStatusText.Text = "连接问题目标已失效，请刷新任务清单。";
                return;
            }
            if (!CanNavigateInputRepairS96()) return;
            OpenCentralModuleConfigurationS77(tool);
            if (!centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, tool)) return;
            if (!issue.OpensParameters)
            {
                SelectCentralConfigurationTaskS77(DockIoTaskTab, "Io");
                var row = DockInputPortItemsControl.Items.OfType<VmInputPortEditorRow>().FirstOrDefault(item => item.PortName == issue.TargetPortName);
                if (row != null) { DockInputPortItemsControl.SelectedItem = row; DockInputPortItemsControl.ScrollIntoView(row); }
            }
            HeaderStatusText.Text = "已打开“" + tool.InstanceName + "”的" + (issue.OpensParameters ? "参数" : "输入") + "；修改后请重新运行。";
        }

        private void OpenConnectionIssueSourceButton_Click(object sender, RoutedEventArgs e)
        {
            VmConnectionIssueS69 issue = GetSelectedConnectionIssueS69();
            if (issue == null || !issue.CanSupplement)
            {
                HeaderStatusText.Text = "当前项只是等待本周期运行值；请先运行或检查上游，不需要改线。";
                return;
            }
            if (!EnterInputSourceContextS68(issue.TargetToolId, issue.TargetPortName))
            {
                HeaderStatusText.Text = "该输入已恢复有效，请刷新连接问题清单。";
                return;
            }
            FlowConnectionIssuePanel.Visibility = Visibility.Collapsed;
            HeaderStatusText.Text = "已从连接问题任务进入 S68 补源：" + issue.TargetText;
        }

        private void RefreshCircleCircleConnectionStatusS69(VmToolInstance tool)
        {
            VmToolInputBindingData bindingA = tool == null ? null : tool.GetInputBinding("CircleA");
            VmToolInputBindingData bindingB = tool == null ? null : tool.GetInputBinding("CircleB");
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            object rawA;
            object rawB;
            bool readyA = sourceA != null && bindingA != null && string.Equals(sourceA.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && sourceA.TryGetOutputValue(bindingA.SourcePortName, out rawA) && rawA is VmCircle2D;
            bool readyB = sourceB != null && bindingB != null && string.Equals(sourceB.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && sourceB.TryGetOutputValue(bindingB.SourcePortName, out rawB) && rawB is VmCircle2D;
            tool.ConfigurationStatus = bindingA == null || bindingB == null ? "待连接几何" : (readyA && readyB ? "就绪" : "等待上游");
            tool.ConnectionStatus = bindingA == null || bindingB == null ? "CircleA 与 CircleB 必须连接" : (readyA && readyB ? "两路 Circle2D 已连接 · 有值" : "圆输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildCircleCircleInputSummaryS69(tool) + " → Point2D[] / Relation / CenterDistance / XLD";
        }

        private string BuildCircleCircleInputSummaryS69(VmToolInstance tool)
        {
            VmToolInputBindingData bindingA = tool == null ? null : tool.GetInputBinding("CircleA");
            VmToolInputBindingData bindingB = tool == null ? null : tool.GetInputBinding("CircleB");
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            string circleA = sourceA == null || bindingA == null ? "CircleA←未连接" : sourceA.InstanceName + "." + bindingA.SourcePortName;
            string circleB = sourceB == null || bindingB == null ? "CircleB←未连接" : sourceB.InstanceName + "." + bindingB.SourcePortName;
            return circleA + " + " + circleB;
        }

        private InspectionRecord RunCircleCircleIntersectionToolS69(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.CircleCircleIntersectionMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData bindingA = tool.GetInputBinding("CircleA");
            VmToolInputBindingData bindingB = tool.GetInputBinding("CircleB");
            if (bindingA == null || bindingB == null) throw new InvalidOperationException("圆圆关系/交点测量必须连接 CircleA 与 CircleB 两路 Circle2D。");
            if (string.Equals(bindingA.SourceToolId, bindingB.SourceToolId, StringComparison.OrdinalIgnoreCase) && string.Equals(bindingA.SourcePortName, bindingB.SourcePortName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CircleA 与 CircleB 不能订阅同一个上游端点。");
            string bindingErrorA = GetInputBindingConfigurationError(tool, bindingA);
            string bindingErrorB = GetInputBindingConfigurationError(tool, bindingB);
            if (!string.IsNullOrWhiteSpace(bindingErrorA)) throw new InvalidOperationException("CircleA：" + bindingErrorA);
            if (!string.IsNullOrWhiteSpace(bindingErrorB)) throw new InvalidOperationException("CircleB：" + bindingErrorB);
            VmToolInstance sourceA = GetInputSourceTool(bindingA);
            VmToolInstance sourceB = GetInputSourceTool(bindingB);
            if (sourceA == null || sourceB == null || !string.Equals(sourceA.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(sourceB.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CircleA 或 CircleB 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object rawA;
            object rawB;
            if (!sourceA.TryGetOutputValue(bindingA.SourcePortName, out rawA) || !(rawA is VmCircle2D)) throw new InvalidOperationException("CircleA 没有类型化 Circle2D 运行值。");
            if (!sourceB.TryGetOutputValue(bindingB.SourcePortName, out rawB) || !(rawB is VmCircle2D)) throw new InvalidOperationException("CircleB 没有类型化 Circle2D 运行值。");
            VmCircle2D circleA = ((VmCircle2D)rawA).Clone();
            VmCircle2D circleB = ((VmCircle2D)rawB).Clone();
            string circleErrorA = circleA.GetValidationError();
            string circleErrorB = circleB.GetValidationError();
            if (!string.IsNullOrWhiteSpace(circleErrorA)) throw new InvalidOperationException("CircleA：" + circleErrorA);
            if (!string.IsNullOrWhiteSpace(circleErrorB)) throw new InvalidOperationException("CircleB：" + circleErrorB);
            if (!string.Equals(circleA.CoordinateContext, circleB.CoordinateContext, StringComparison.Ordinal) || !string.Equals(circleA.CoordinateContextId, circleB.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("CircleA 与 CircleB 坐标上下文不一致，禁止跨图像求交。");

            double deltaRow = circleB.CenterRow - circleA.CenterRow;
            double deltaColumn = circleB.CenterColumn - circleA.CenterColumn;
            double centerDistance = Math.Sqrt(deltaRow * deltaRow + deltaColumn * deltaColumn);
            if (double.IsNaN(centerDistance) || double.IsInfinity(centerDistance)) throw new InvalidOperationException("两圆心距离不是有限数值。");
            double tolerance = Math.Max(1e-8, Math.Max(circleA.Radius, circleB.Radius) * 1e-9);
            HTuple rowsTuple = null;
            HTuple columnsTuple = null;
            HTuple overlapTuple = null;
            HObject overlays = null;
            HObject contourA = null;
            HObject contourB = null;
            HObject centerGuide = null;
            HObject crosses = null;
            try
            {
                HOperatorSet.IntersectionCircles(
                    circleA.CenterRow, circleA.CenterColumn, circleA.Radius, 0.0, Math.PI * 2.0, "positive",
                    circleB.CenterRow, circleB.CenterColumn, circleB.Radius, 0.0, Math.PI * 2.0, "positive",
                    out rowsTuple, out columnsTuple, out overlapTuple);
                if (rowsTuple == null || columnsTuple == null || rowsTuple.Length != columnsTuple.Length)
                    throw new InvalidOperationException("HALCON intersection_circles 返回了不一致的 Row/Column 集合。");
                bool overlapping = overlapTuple != null && overlapTuple.Length > 0 && overlapTuple[0].I != 0;
                if (rowsTuple.Length > 2 && !overlapping) throw new InvalidOperationException("HALCON 返回了超过两个有限圆圆交点。");

                List<Tuple<double, double>> pointsRaw = new List<Tuple<double, double>>();
                if (!overlapping)
                {
                    for (int index = 0; index < rowsTuple.Length; index++)
                    {
                        double row = rowsTuple[index].D;
                        double column = columnsTuple[index].D;
                        if (double.IsNaN(row) || double.IsInfinity(row) || double.IsNaN(column) || double.IsInfinity(column))
                            throw new InvalidOperationException("HALCON 返回了非有限圆圆交点坐标。");
                        if (!pointsRaw.Any(item => Math.Abs(item.Item1 - row) <= tolerance && Math.Abs(item.Item2 - column) <= tolerance))
                            pointsRaw.Add(Tuple.Create(row, column));
                    }
                }
                IEnumerable<Tuple<double, double>> sorted = pointsRaw.OrderBy(item => item.Item1).ThenBy(item => item.Item2);
                if (string.Equals(parameters.CircleCircleIntersectionOrder, VmCircleCircleIntersectionOrder.RowColumnDescending, StringComparison.OrdinalIgnoreCase)) sorted = sorted.Reverse();
                double[] rows = sorted.Select(item => item.Item1).ToArray();
                double[] columns = sorted.Select(item => item.Item2).ToArray();
                VmPoint2DArray points = VmPoint2DArray.CreateImagePixel(rows, columns, circleA.CoordinateContextId);
                int count = points.Count;
                string relation = ClassifyCircleCircleRelationS69(circleA, circleB, centerDistance, count, overlapping, tolerance);
                double chordLength = count < 2 ? 0.0 : Math.Sqrt(Math.Pow(rows[1] - rows[0], 2) + Math.Pow(columns[1] - columns[0], 2));
                string resultCode = count > 0 && !overlapping ? "OK" : "NG";

                HOperatorSet.GenCircleContourXld(out contourA, circleA.CenterRow, circleA.CenterColumn, circleA.Radius, 0.0, Math.PI * 2.0, "positive", 1.0);
                AppendOverlayObject(ref overlays, contourA);
                HOperatorSet.GenCircleContourXld(out contourB, circleB.CenterRow, circleB.CenterColumn, circleB.Radius, 0.0, Math.PI * 2.0, "positive", 1.0);
                AppendOverlayObject(ref overlays, contourB);
                if (centerDistance > tolerance)
                    HOperatorSet.GenContourPolygonXld(out centerGuide, new HTuple(new[] { circleA.CenterRow, circleB.CenterRow }), new HTuple(new[] { circleA.CenterColumn, circleB.CenterColumn }));
                else
                    HOperatorSet.GenCrossContourXld(out centerGuide, circleA.CenterRow, circleA.CenterColumn, parameters.CircleCircleOverlayCrossSize, 0.0);
                AppendOverlayObject(ref overlays, centerGuide);
                if (count > 0)
                {
                    HOperatorSet.GenCrossContourXld(out crosses, new HTuple(rows), new HTuple(columns), parameters.CircleCircleOverlayCrossSize, Math.PI / 4.0);
                    AppendOverlayObject(ref overlays, crosses);
                }

                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("IntersectionPoints", points);
                if (count > 0) tool.SetOutputValue("PointA", points.Points[0].Clone());
                if (count > 1) tool.SetOutputValue("PointB", points.Points[1].Clone());
                tool.SetOutputValue("CenterDistance", centerDistance);
                tool.SetOutputValue("IntersectionCount", (double)count);
                tool.SetOutputValue("CommonChordLength", chordLength);
                tool.SetOutputValue("Relation", relation);
                tool.SetOutputValue("IsOverlapping", overlapping);
                tool.SetOutputValue("CoordinateContext", circleA.CoordinateContext + ":" + circleA.CoordinateContextId);
                string parameterText = "HALCON=intersection_circles, CircleMode=Full, Order=" + parameters.CircleCircleIntersectionOrder + ", CrossSize=" + parameters.CircleCircleOverlayCrossSize.ToString("0.###", CultureInfo.InvariantCulture);
                tool.SetOutputValue("Parameters", parameterText);
                ReplaceToolOverlayContours(overlays, tool, "XLD", 3 + count, resultCode, centerDistance);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 圆圆关系/交点：Relation={0}，Count={1}，CenterDistance={2:0.###}px，CommonChord={3:0.###}px，A=({4:0.###},{5:0.###},R={6:0.###})，B=({7:0.###},{8:0.###},R={9:0.###})，Overlapping={10} · {11}",
                    relation, count, centerDistance, chordLength, circleA.CenterRow, circleA.CenterColumn, circleA.Radius, circleB.CenterRow, circleB.CenterColumn, circleB.Radius, overlapping, circleA.CoordinateContextId);
                InspectionRecord record = CreateRecord("CircleCircleIntersectionMeasurement", resultCode, centerDistance, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (rowsTuple != null) rowsTuple.Dispose();
                if (columnsTuple != null) columnsTuple.Dispose();
                if (overlapTuple != null) overlapTuple.Dispose();
                DisposeObject(crosses);
                DisposeObject(centerGuide);
                DisposeObject(contourB);
                DisposeObject(contourA);
                DisposeObject(overlays);
            }
        }

        private static string ClassifyCircleCircleRelationS69(VmCircle2D circleA, VmCircle2D circleB, double centerDistance, int count, bool overlapping, double tolerance)
        {
            if (overlapping) return "Overlapping";
            if (centerDistance <= tolerance) return "ConcentricContained";
            if (count == 2) return "Secant";
            if (count == 1)
            {
                double sumGap = Math.Abs(centerDistance - (circleA.Radius + circleB.Radius));
                double differenceGap = Math.Abs(centerDistance - Math.Abs(circleA.Radius - circleB.Radius));
                return sumGap <= differenceGap ? "ExternalTangent" : "InternalTangent";
            }
            if (centerDistance > circleA.Radius + circleB.Radius + tolerance) return "Separated";
            return "Contained";
        }
    }
}
