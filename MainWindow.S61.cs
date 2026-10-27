using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<VmScriptInterfaceMemberData> scriptInterfaceDraftMembers = new ObservableCollection<VmScriptInterfaceMemberData>();
        private VmToolInstance scriptInterfaceTool;
        private bool scriptInterfaceUiUpdating;
        private Window scriptInterfaceWindowUI08;
        private Panel scriptInterfaceHomeUI08;
        private bool closingScriptInterfaceUI08;
        private string scriptInterfaceSessionUI08, scriptInterfaceFlowUI08, scriptInterfaceDefinitionUI08;

        private void PopulateScriptNamedContext(VmToolInstance tool, VmToolParameterData parameters, ScriptToolContext context)
        {
            foreach (VmScriptInterfaceMemberData member in parameters.ScriptInterfaceMembers.Where(item => item != null))
            {
                if (string.Equals(member.Direction, VmScriptMemberDirection.Parameter, StringComparison.OrdinalIgnoreCase))
                {
                    context.SetNamedParameter(member.Name, ParseScriptScalar(member, member.DefaultValue, "参数默认值"));
                    continue;
                }
                if (!string.Equals(member.Direction, VmScriptMemberDirection.Input, StringComparison.OrdinalIgnoreCase)) continue;

                object raw;
                bool hasValue;
                if (VmArrayContractS105.IsType(member.ValueType) && tool.GetInputBinding(member.PortName) != null)
                {
                    VmToolInstance producer;
                    raw = ReadBranchInputS101(tool, member.PortName, out producer);
                    hasValue = true;
                }
                else hasValue = TryResolveOptionalToolInputValue(tool, member.PortName, out raw);
                if (hasValue)
                {
                    context.SetNamedInput(member.Name, ConvertScriptScalar(member, raw, "输入"));
                }
                else if (member.IsRequired)
                {
                    throw new InvalidOperationException("脚本必需输入 “" + member.Name + "” 尚未连接；请在 I/O 或接口设计器中完成订阅。");
                }
                else if (!string.IsNullOrEmpty(member.DefaultValue))
                {
                    context.SetNamedInput(member.Name, ParseScriptScalar(member, member.DefaultValue, "输入默认值"));
                }
            }
        }

        private void ApplyScriptNamedOutputs(VmToolInstance tool, VmToolParameterData parameters, ScriptToolResult result)
        {
            Dictionary<string, VmScriptInterfaceMemberData> schema = parameters.ScriptInterfaceMembers
                .Where(item => item != null && string.Equals(item.Direction, VmScriptMemberDirection.Output, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(item => item.Name, item => item, StringComparer.OrdinalIgnoreCase);
            IReadOnlyDictionary<string, ScriptScalarValue> actual = result.NamedOutputs;

            foreach (KeyValuePair<string, ScriptScalarValue> pair in actual)
            {
                VmScriptInterfaceMemberData member;
                if (!schema.TryGetValue(pair.Key, out member))
                    throw new InvalidOperationException("脚本写入了未声明的命名输出：“" + pair.Key + "”。请先在接口设计器中声明。 ");
                ScriptScalarValue value = pair.Value;
                if (value == null || !string.Equals(value.ValueType, member.ValueType, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("脚本输出 “" + pair.Key + "” 类型不匹配；接口声明为 " + member.ValueType + "，实际为 " + (value == null ? "null" : value.ValueType) + "。");
            }

            var prepared = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (VmScriptInterfaceMemberData member in schema.Values)
            {
                ScriptScalarValue value;
                if (!actual.TryGetValue(member.Name, out value))
                {
                    if (member.IsRequired) throw new InvalidOperationException("脚本未写入必需输出：“" + member.Name + "”。");
                    prepared.Add(member.PortName, null);
                    continue;
                }
                value.Validate();
                prepared.Add(member.PortName, value.ToObject());
            }
            foreach (var output in prepared) tool.SetOutputValue(output.Key, output.Value);
        }

        private static ScriptScalarValue ConvertScriptScalar(VmScriptInterfaceMemberData member, object raw, string sourceLabel)
        {
            string type = VmScriptScalarType.Normalize(member.ValueType);
            try
            {
                if (type == VmScriptScalarType.NumberArray)
                {
                    VmArrayContractS105.RequireType(type, raw);
                    return ScriptScalarValue.FromNumberArray(((VmNumberArrayS105)raw).ToArray());
                }
                if (type == VmScriptScalarType.StringArray)
                {
                    VmArrayContractS105.RequireType(type, raw);
                    var array = (VmStringArrayS105)raw;
                    return ScriptScalarValue.FromStringArray(Enumerable.Range(0, array.Count).Select(index => array[index]).ToArray());
                }
                if (type == VmScriptScalarType.Number)
                {
                    double number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                    return ScriptScalarValue.FromNumber(number);
                }
                if (type == VmScriptScalarType.Boolean)
                {
                    if (!(raw is bool)) throw new InvalidCastException("实际值不是 Boolean。");
                    return ScriptScalarValue.FromBoolean((bool)raw);
                }
                if (type == VmScriptScalarType.ResultCode)
                    return ScriptScalarValue.FromResultCode(Convert.ToString(raw, CultureInfo.InvariantCulture));
                return ScriptScalarValue.FromString(Convert.ToString(raw, CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("脚本" + sourceLabel + " “" + member.Name + "” 无法按 " + type + " 读取：" + ex.Message, ex);
            }
        }

        private static ScriptScalarValue ParseScriptScalar(VmScriptInterfaceMemberData member, string text, string sourceLabel)
        {
            object parsed;
            string error;
            if (!VmScriptScalarType.TryParseDefault(member.ValueType, text, false, out parsed, out error))
                throw new InvalidOperationException("脚本 “" + member.Name + "” 的" + sourceLabel + "无效：" + error);
            return ConvertScriptScalar(member, parsed, sourceLabel);
        }

        private static string BuildScriptNamedOutputSummary(VmToolParameterData parameters, ScriptToolResult result)
        {
            if (parameters == null || result == null || result.NamedOutputs.Count == 0) return "Named[--]";
            List<string> items = new List<string>();
            foreach (VmScriptInterfaceMemberData member in parameters.ScriptInterfaceMembers
                .Where(item => item != null && item.Direction == VmScriptMemberDirection.Output)
                .Take(4))
            {
                ScriptScalarValue value;
                if (!result.NamedOutputs.TryGetValue(member.Name, out value)) continue;
                string displayValue = Convert.ToString(value.ToObject(), CultureInfo.InvariantCulture) ?? string.Empty;
                if (displayValue.Length > 48) displayValue = displayValue.Substring(0, 45) + "...";
                items.Add(member.Name + "=" + displayValue);
            }
            int remaining = Math.Max(0, result.NamedOutputs.Count - items.Count);
            return "Named[" + (items.Count == 0 ? "--" : string.Join(", ", items)) + (remaining > 0 ? ", +" + remaining.ToString(CultureInfo.InvariantCulture) : string.Empty) + "]";
        }

        private void InitializeScriptInterfaceWorkspace()
        {
            ScriptInterfaceMemberListBox.ItemsSource = scriptInterfaceDraftMembers;
            ScriptInterfaceDirectionComboBox.ItemsSource = new[]
            {
                new VmToolValueOption { Key = VmScriptMemberDirection.Input, DisplayText = "输入" },
                new VmToolValueOption { Key = VmScriptMemberDirection.Parameter, DisplayText = "参数" },
                new VmToolValueOption { Key = VmScriptMemberDirection.Output, DisplayText = "输出" }
            };
            ScriptInterfaceTypeComboBox.ItemsSource = new[]
            {
                new VmToolValueOption { Key = VmScriptScalarType.Number, DisplayText = "数值 Number" },
                new VmToolValueOption { Key = VmScriptScalarType.String, DisplayText = "文本 String" },
                new VmToolValueOption { Key = VmScriptScalarType.Boolean, DisplayText = "布尔 Boolean" },
                new VmToolValueOption { Key = VmScriptScalarType.ResultCode, DisplayText = "结果码 ResultCode" },
                new VmToolValueOption { Key = VmScriptScalarType.NumberArray, DisplayText = "数值数组 Number[]" },
                new VmToolValueOption { Key = VmScriptScalarType.StringArray, DisplayText = "文本数组 String[]" }
            };
            SetScriptInterfaceEditor(null);
        }

        private void OpenScriptInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (productionRunMode)
            {
                HeaderStatusText.Text = "请先返回开发模式，再设计脚本接口。";
                return;
            }
            bool fromDock = (sender as FrameworkElement)?.Tag as string == "ScriptDock";
            VmToolInstance selected = fromDock ? dockEditingTool : FlowToolList?.SelectedItem as VmToolInstance;
            if (selected == null || selected.Kind != VmToolKind.CSharpDllScript || !flowTools.Contains(selected))
            {
                HeaderStatusText.Text = "请先选择要配置的 C# / DLL 脚本；不会自动打开其他脚本。";
                return;
            }
            if (scriptInterfaceWindowUI08 != null)
            {
                if (ReferenceEquals(scriptInterfaceTool, selected))
                { if (scriptInterfaceWindowUI08.ShowActivated) scriptInterfaceWindowUI08.Activate(); }
                else HeaderStatusText.Text = "请先完成或关闭当前接口草稿，再打开另一个脚本。";
                return;
            }
            long lease = CaptureEngineeringEditLeaseS137("打开脚本接口");
            string session = projectSessionIdS104, flow = activeProjectFlowIdS104;
            string reason;
            if (lease < 0 || !CanNavigateProductionAlarmTaskS150(out reason)) return;
            if (!ConfirmDockDraftChanges("设计脚本接口")) return;
            if (!IsEngineeringEditLeaseCurrentS137(lease) || session != projectSessionIdS104 || flow != activeProjectFlowIdS104 ||
                !flowTools.Contains(selected) || !CanNavigateProductionAlarmTaskS150(out reason)) return;
            OpenScriptInterfaceWorkspace(selected);
        }

        private void OpenScriptInterfaceWorkspace(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.CSharpDllScript || !flowTools.Contains(tool)) return;
            scriptInterfaceTool = tool;
            scriptInterfaceDraftMembers.Clear();
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            parameters.Normalize();
            foreach (VmScriptInterfaceMemberData member in parameters.ScriptInterfaceMembers) scriptInterfaceDraftMembers.Add(member.Clone());
            ScriptInterfaceToolNameText.Text = tool.InstanceName;
            ScriptInterfaceContractText.Text = "应用接口后，回参数窗的“输入”页连接来源。";
            ScriptInterfaceContractText.ToolTip = "固定入口：public static ScriptToolResult Execute(ScriptToolContext input)";
            ScriptInterfaceValidationText.Text = "接口草稿尚未修改。固定兼容端口继续保留。";
            ScriptInterfaceValidationText.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            ScriptInterfaceMemberListBox.SelectedItem = scriptInterfaceDraftMembers.FirstOrDefault();
            RefreshScriptInterfaceCounts();
            GlobalVariableWorkspace.Visibility = Visibility.Collapsed;
            ExternalResourceWorkspace.Visibility = Visibility.Collapsed;
            scriptInterfaceSessionUI08 = projectSessionIdS104;
            scriptInterfaceFlowUI08 = activeProjectFlowIdS104;
            scriptInterfaceDefinitionUI08 = recipeService.GetRecipeFingerprint(CaptureRecipe());
            if (scriptInterfaceWindowUI08 == null)
            {
                scriptInterfaceHomeUI08 = ScriptInterfaceWorkspace.Parent as Panel;
                scriptInterfaceHomeUI08?.Children.Remove(ScriptInterfaceWorkspace);
                scriptInterfaceWindowUI08 = new Window
                {
                    Owner = this, Title = "脚本接口 · " + tool.InstanceName, Width = 800, Height = 580,
                    MinWidth = 700, MinHeight = 440, ShowInTaskbar = false, ShowActivated = ShowActivated,
                    WindowStartupLocation = WindowStartupLocation.Manual, Content = ScriptInterfaceWorkspace,
                    Left = ShowActivated ? Math.Max(SystemParameters.WorkArea.Left, Math.Min(SystemParameters.WorkArea.Right - 800, Left + 50)) : Left,
                    Top = ShowActivated ? Math.Max(SystemParameters.WorkArea.Top, Math.Min(SystemParameters.WorkArea.Bottom - 580, Top + 50)) : Top
                };
                scriptInterfaceWindowUI08.Resources.MergedDictionaries.Add(Resources);
                scriptInterfaceWindowUI08.Closing += delegate(object source, System.ComponentModel.CancelEventArgs args)
                {
                    if (closingScriptInterfaceUI08) return;
                    args.Cancel = true;
                    Dispatcher.BeginInvoke(new Action(delegate { CloseScriptInterfaceButton_Click(null, null); }));
                };
                scriptInterfaceWindowUI08.Show();
            }
            ScriptInterfaceWorkspace.Visibility = Visibility.Visible;
            if (DeveloperMoreToggleButton != null) DeveloperMoreToggleButton.IsChecked = false;
        }

        private void CloseScriptInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptInterfaceWorkspace.Visibility = Visibility.Collapsed;
            if (scriptInterfaceWindowUI08 != null)
            {
                closingScriptInterfaceUI08 = true;
                try { scriptInterfaceWindowUI08.Content = null; scriptInterfaceWindowUI08.Close(); scriptInterfaceWindowUI08 = null; }
                finally { closingScriptInterfaceUI08 = false; }
                if (scriptInterfaceHomeUI08 != null && !scriptInterfaceHomeUI08.Children.Contains(ScriptInterfaceWorkspace))
                    scriptInterfaceHomeUI08.Children.Add(ScriptInterfaceWorkspace);
            }
            scriptInterfaceDefinitionUI08 = null;
            scriptInterfaceTool = null;
            scriptInterfaceDraftMembers.Clear();
            SetScriptInterfaceEditor(null);
            MainWorkspaceGrid.Visibility = Visibility.Visible;
            HeaderStatusText.Text = "接口窗口已关闭；未应用的接口草稿不写入方案。";
        }

        private void ScriptInterfaceMemberListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (scriptInterfaceUiUpdating) return;
            SetScriptInterfaceEditor(ScriptInterfaceMemberListBox.SelectedItem as VmScriptInterfaceMemberData);
        }

        private void AddScriptInputButton_Click(object sender, RoutedEventArgs e) { AddScriptInterfaceMember(VmScriptMemberDirection.Input); }
        private void AddScriptParameterButton_Click(object sender, RoutedEventArgs e) { AddScriptInterfaceMember(VmScriptMemberDirection.Parameter); }
        private void AddScriptOutputButton_Click(object sender, RoutedEventArgs e) { AddScriptInterfaceMember(VmScriptMemberDirection.Output); }

        private void AddScriptInterfaceMember(string direction)
        {
            if (!RequireEngineeringEditS137("新增脚本接口")) return;
            int count = scriptInterfaceDraftMembers.Count(item => string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase)) + 1;
            string prefix = direction == VmScriptMemberDirection.Parameter ? "Parameter" : (direction == VmScriptMemberDirection.Output ? "Output" : "Input");
            VmScriptInterfaceMemberData member = VmScriptInterfaceMemberData.Create(direction, VmScriptScalarType.Number, prefix + count.ToString(CultureInfo.InvariantCulture));
            if (direction == VmScriptMemberDirection.Parameter) member.DefaultValue = "0";
            scriptInterfaceDraftMembers.Add(member);
            ScriptInterfaceMemberListBox.SelectedItem = member;
            RefreshScriptInterfaceCounts();
            SetScriptInterfaceStatus("已添加" + member.DirectionText + "草稿；应用接口后才写入流程。", false);
        }

        private void ApplyScriptMemberButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("修改脚本接口字段")) return;
            VmScriptInterfaceMemberData member = ScriptInterfaceMemberListBox.SelectedItem as VmScriptInterfaceMemberData;
            if (member == null) return;
            member.Direction = Convert.ToString(ScriptInterfaceDirectionComboBox.SelectedValue, CultureInfo.InvariantCulture);
            member.Name = (ScriptInterfaceNameTextBox.Text ?? string.Empty).Trim();
            member.ValueType = Convert.ToString(ScriptInterfaceTypeComboBox.SelectedValue, CultureInfo.InvariantCulture);
            member.DefaultValue = ScriptInterfaceDefaultTextBox.Text ?? string.Empty;
            member.IsRequired = ScriptInterfaceRequiredCheckBox.IsChecked == true;
            member.Description = (ScriptInterfaceDescriptionTextBox.Text ?? string.Empty).Trim();
            member.Normalize();
            string error = VmToolParameterData.ValidateScriptInterfaceMembers(scriptInterfaceDraftMembers);
            if (!string.IsNullOrWhiteSpace(error))
            {
                SetScriptInterfaceStatus(error, true);
                return;
            }
            ScriptInterfaceMemberListBox.Items.Refresh();
            RefreshScriptInterfaceCounts();
            SetScriptInterfaceEditor(member);
            SetScriptInterfaceStatus("成员草稿有效；点击“应用接口”写入当前脚本实例。", false);
        }

        private void DuplicateScriptMemberButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("复制脚本接口字段")) return;
            VmScriptInterfaceMemberData selected = ScriptInterfaceMemberListBox.SelectedItem as VmScriptInterfaceMemberData;
            if (selected == null) return;
            VmScriptInterfaceMemberData copy = selected.Clone();
            copy.MemberId = Guid.NewGuid().ToString("N");
            copy.Name = FindAvailableScriptMemberName(selected.Name + "Copy");
            scriptInterfaceDraftMembers.Add(copy);
            ScriptInterfaceMemberListBox.SelectedItem = copy;
            RefreshScriptInterfaceCounts();
        }

        private void DeleteScriptMemberButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("删除脚本接口字段")) return;
            VmScriptInterfaceMemberData selected = ScriptInterfaceMemberListBox.SelectedItem as VmScriptInterfaceMemberData;
            if (selected == null) return;
            string usage = GetScriptMemberUsageError(scriptInterfaceTool, selected);
            if (!string.IsNullOrWhiteSpace(usage))
            {
                SetScriptInterfaceStatus(usage, true);
                return;
            }
            int index = scriptInterfaceDraftMembers.IndexOf(selected);
            scriptInterfaceDraftMembers.Remove(selected);
            ScriptInterfaceMemberListBox.SelectedItem = scriptInterfaceDraftMembers.Count == 0 ? null : scriptInterfaceDraftMembers[Math.Min(index, scriptInterfaceDraftMembers.Count - 1)];
            RefreshScriptInterfaceCounts();
            SetScriptInterfaceStatus("成员已从草稿移除；应用接口后生效。", false);
        }

        private void ApplyScriptInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("应用脚本接口")) return;
            string reason;
            if (scriptInterfaceWindowUI08 == null || scriptInterfaceTool == null || !flowTools.Contains(scriptInterfaceTool) ||
                scriptInterfaceSessionUI08 != projectSessionIdS104 || scriptInterfaceFlowUI08 != activeProjectFlowIdS104)
            {
                SetScriptInterfaceStatus("当前脚本工具已不存在，请关闭后重新选择。", true);
                return;
            }
            if (!CanNavigateProductionAlarmTaskS150(out reason)) { SetScriptInterfaceStatus(reason, true); return; }
            if (dockDraftDirty || scriptInterfaceDefinitionUI08 != recipeService.GetRecipeFingerprint(CaptureRecipe()))
            { SetScriptInterfaceStatus("参数草稿或方案已改变；请先处理参数草稿，再取消接口草稿重新载入，避免覆盖旧配置。", true); return; }
            ApplyScriptMemberButton_Click(sender, e);
            string error = VmToolParameterData.ValidateScriptInterfaceMembers(scriptInterfaceDraftMembers);
            if (!string.IsNullOrWhiteSpace(error)) { SetScriptInterfaceStatus(error, true); return; }

            VmToolParameterData current = scriptInterfaceTool.Parameters ?? new VmToolParameterData();
            current.Normalize();
            string compatibilityError = ValidateScriptInterfaceCompatibility(scriptInterfaceTool, current.ScriptInterfaceMembers, scriptInterfaceDraftMembers);
            if (!string.IsNullOrWhiteSpace(compatibilityError)) { SetScriptInterfaceStatus(compatibilityError, true); return; }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            VmToolParameterData updated = current.Clone();
            updated.ScriptInterfaceMembers = scriptInterfaceDraftMembers.Select(item => item.Clone()).ToList();
            updated.Normalize();
            scriptInterfaceTool.Parameters = updated;
            InvalidateToolRunResult(scriptInterfaceTool, "脚本接口已修改");
            InvalidateDownstreamResults(scriptInterfaceTool, "脚本接口已修改");
            RefreshFlowSequence();
            RefreshDockConfigurationRuntime(scriptInterfaceTool);
            if (ReferenceEquals(dockEditingTool, scriptInterfaceTool)) LoadDockConfigurationDraft(scriptInterfaceTool);
            CommitFlowEditHistory("修改脚本接口：" + scriptInterfaceTool.InstanceName, before);
            ScheduleRecipeStateCheck();
            scriptInterfaceDefinitionUI08 = recipeService.GetRecipeFingerprint(CaptureRecipe());
            SetScriptInterfaceStatus("接口已应用；回“输入”页绑定来源，再明确试运行。", false);
        }

        private void CancelScriptInterfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (scriptInterfaceTool != null) OpenScriptInterfaceWorkspace(scriptInterfaceTool);
        }

        private void CopyScriptSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            string snippet = ScriptInterfaceSnippetTextBox.Text ?? string.Empty;
            if (snippet.Length == 0) return;
            try
            {
                Clipboard.SetText(snippet);
                SetScriptInterfaceStatus("访问代码已复制。", false);
            }
            catch (Exception ex) { SetScriptInterfaceStatus("复制失败：" + ex.Message, true); }
        }

        private void SetScriptInterfaceEditor(VmScriptInterfaceMemberData member)
        {
            scriptInterfaceUiUpdating = true;
            try
            {
                bool has = member != null;
                ScriptInterfaceEditorPanel.IsEnabled = has;
                ScriptInterfaceEditorPanel.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
                ScriptInterfaceEmptyText.Visibility = has ? Visibility.Collapsed : Visibility.Visible;
                ScriptInterfaceMemberIdText.Text = has ? member.MemberId : "--";
                ScriptInterfaceDirectionComboBox.SelectedValue = has ? member.Direction : VmScriptMemberDirection.Input;
                ScriptInterfaceNameTextBox.Text = has ? member.Name : string.Empty;
                ScriptInterfaceTypeComboBox.SelectedValue = has ? member.ValueType : VmScriptScalarType.Number;
                ScriptInterfaceDefaultTextBox.Text = has ? member.DefaultValue : string.Empty;
                ScriptInterfaceRequiredCheckBox.IsChecked = has && member.IsRequired;
                ScriptInterfaceRequiredCheckBox.IsEnabled = has && member.Direction != VmScriptMemberDirection.Parameter;
                ScriptInterfaceRequiredCheckBox.Content = !has || member.Direction == VmScriptMemberDirection.Input
                    ? "必需；未连接时立即停止当前工具并清除旧输出"
                    : (member.Direction == VmScriptMemberDirection.Output
                        ? "必需；脚本未写入时立即停止当前工具并清除旧输出"
                        : "参数始终使用确定默认值");
                ScriptInterfaceDescriptionTextBox.Text = has ? member.Description : string.Empty;
                ScriptInterfaceDefaultRow.Visibility = has && member.Direction == VmScriptMemberDirection.Output ? Visibility.Collapsed : Visibility.Visible;
                ScriptInterfaceSnippetTextBox.Text = has ? BuildScriptAccessSnippet(member) : string.Empty;
                ScriptInterfacePortText.Text = has && member.Direction != VmScriptMemberDirection.Parameter ? member.PortName : "参数不占用流程端口";
            }
            finally { scriptInterfaceUiUpdating = false; }
            RefreshScriptArrayHintS107();
        }

        private static string BuildScriptAccessSnippet(VmScriptInterfaceMemberData member)
        {
            string name = member.Name ?? string.Empty;
            string type = VmScriptScalarType.Normalize(member.ValueType);
            string suffix = type == VmScriptScalarType.NumberArray ? "NumberArray" : type == VmScriptScalarType.StringArray ? "StringArray" :
                type == VmScriptScalarType.Number ? "Number" : (type == VmScriptScalarType.Boolean ? "Boolean" : (type == VmScriptScalarType.ResultCode ? "ResultCode" : "String"));
            if (member.Direction == VmScriptMemberDirection.Output)
            {
                string sample = type == VmScriptScalarType.NumberArray ? "new double[] { 1, 2, 3 }" : type == VmScriptScalarType.StringArray ? "new string[] { \"工位1\", \"工位2\" }" :
                    type == VmScriptScalarType.Number ? "value" : (type == VmScriptScalarType.Boolean ? "passed" : (type == VmScriptScalarType.ResultCode ? "\"OK\"" : "text"));
                return "result.Set" + suffix + "(\"" + name + "\", " + sample + ");";
            }
            if (member.Direction == VmScriptMemberDirection.Parameter)
                return "var " + ToScriptLocalName(name) + " = input.GetParameter" + suffix + "(\"" + name + "\");";
            return "var " + ToScriptLocalName(name) + " = input.GetInput" + suffix + "(\"" + name + "\");";
        }

        private static string ToScriptLocalName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "value";
            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        private string FindAvailableScriptMemberName(string seed)
        {
            string candidate = string.IsNullOrWhiteSpace(seed) ? "Member" : seed;
            int suffix = 2;
            while (scriptInterfaceDraftMembers.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                candidate = seed + (suffix++).ToString(CultureInfo.InvariantCulture);
            return candidate;
        }

        private static string GetScriptMemberUsageError(VmToolInstance tool, VmScriptInterfaceMemberData member)
        {
            if (tool == null || member == null) return string.Empty;
            if (member.Direction == VmScriptMemberDirection.Input && tool.GetInputBinding(member.PortName) != null)
                return "该输入仍有真实订阅，先在 I/O 页清除连接后再删除。";
            return string.Empty;
        }

        private string ValidateScriptInterfaceCompatibility(VmToolInstance tool, IEnumerable<VmScriptInterfaceMemberData> oldMembers, IEnumerable<VmScriptInterfaceMemberData> newMembers)
        {
            Dictionary<string, VmScriptInterfaceMemberData> next = newMembers.ToDictionary(item => item.MemberId, item => item, StringComparer.OrdinalIgnoreCase);
            foreach (VmScriptInterfaceMemberData oldMember in oldMembers ?? Enumerable.Empty<VmScriptInterfaceMemberData>())
            {
                VmScriptInterfaceMemberData replacement;
                bool exists = next.TryGetValue(oldMember.MemberId, out replacement);
                bool changedPortContract = !exists || oldMember.Direction != replacement.Direction || oldMember.ValueType != replacement.ValueType;
                if (!changedPortContract) continue;
                if (oldMember.Direction == VmScriptMemberDirection.Input && tool.GetInputBinding(oldMember.PortName) != null)
                    return "“" + oldMember.Name + "” 已连接真实输入；改变方向/类型或删除前必须先清除连接。";
                if (oldMember.Direction == VmScriptMemberDirection.Output)
                {
                    bool consumed = flowTools.Any(candidate => candidate.InputBindings.Any(binding =>
                        string.Equals(binding.SourceToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(binding.SourcePortName, oldMember.PortName, StringComparison.OrdinalIgnoreCase)));
                    if (consumed) return "“" + oldMember.Name + "” 已被下游订阅；改变方向/类型或删除前必须先清除消费者连接。";
                }
            }
            return string.Empty;
        }

        private void RefreshScriptInterfaceCounts()
        {
            int inputs = scriptInterfaceDraftMembers.Count(item => item.Direction == VmScriptMemberDirection.Input);
            int parameters = scriptInterfaceDraftMembers.Count(item => item.Direction == VmScriptMemberDirection.Parameter);
            int outputs = scriptInterfaceDraftMembers.Count(item => item.Direction == VmScriptMemberDirection.Output);
            ScriptInterfaceCountText.Text = string.Format(CultureInfo.InvariantCulture, "输入 {0} · 参数 {1} · 输出 {2}", inputs, parameters, outputs);
        }

        private void SetScriptInterfaceStatus(string message, bool error)
        {
            ScriptInterfaceValidationText.Text = message ?? string.Empty;
            ScriptInterfaceValidationText.Foreground = new SolidColorBrush(error ? Color.FromRgb(185, 28, 28) : Color.FromRgb(15, 118, 110));
        }
    }
}
