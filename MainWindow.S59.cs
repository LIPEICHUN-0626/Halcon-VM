using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<VmGlobalVariableData> globalVariables = new ObservableCollection<VmGlobalVariableData>();
        private ICollectionView globalVariableView;
        private bool globalVariableUiUpdating;

        private void InitializeGlobalVariableWorkspace()
        {
            globalVariableView = CollectionViewSource.GetDefaultView(globalVariables);
            globalVariableView.Filter = FilterGlobalVariable;
            GlobalVariableListBox.ItemsSource = globalVariableView;
            DockGlobalVariableComboBox.ItemsSource = globalVariables;
            VmToolValueOption[] types =
            {
                new VmToolValueOption { Key = VmGlobalVariableType.Number, DisplayText = "数值" },
                new VmToolValueOption { Key = VmGlobalVariableType.String, DisplayText = "字符串" },
                new VmToolValueOption { Key = VmGlobalVariableType.Boolean, DisplayText = "布尔值" },
                new VmToolValueOption { Key = VmGlobalVariableType.ResultCode, DisplayText = "结果码（OK/NG）" }
            };
            GlobalVariableValueTypeComboBox.ItemsSource = types;
            GlobalVariableValueTypeComboBox.SelectedValue = VmGlobalVariableType.Number;
            SetGlobalVariableEditor(null);
            RefreshGlobalVariableSubscriptions();
        }

        private List<VmGlobalVariableData> CaptureGlobalVariables()
        {
            return globalVariables.Select(item => item.CloneNormalized()).ToList();
        }

        private void LoadGlobalVariablesFromRecipe(VisionRecipe recipe)
        {
            string selectedId = (GlobalVariableListBox == null ? null : GlobalVariableListBox.SelectedItem as VmGlobalVariableData)?.VariableId;
            globalVariableUiUpdating = true;
            try
            {
                globalVariables.Clear();
                HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (VmGlobalVariableData source in (recipe == null ? null : recipe.GlobalVariables) ?? new List<VmGlobalVariableData>())
                {
                    if (source == null) continue;
                    VmGlobalVariableData item = source.CloneNormalized();
                    if (string.IsNullOrWhiteSpace(item.VariableId) || !ids.Add(item.VariableId))
                    {
                        item.VariableId = Guid.NewGuid().ToString("N");
                        ids.Add(item.VariableId);
                    }
                    string baseName = string.IsNullOrWhiteSpace(item.Name) ? "Variable" : item.Name.Trim();
                    string candidate = baseName;
                    int suffix = 2;
                    while (!names.Add(candidate)) candidate = baseName + "_" + (suffix++).ToString("00", CultureInfo.InvariantCulture);
                    item.Name = candidate;
                    if (double.IsNaN(item.DefaultNumberValue) || double.IsInfinity(item.DefaultNumberValue)) item.DefaultNumberValue = 0;
                    if (double.IsNaN(item.NumberValue) || double.IsInfinity(item.NumberValue)) item.NumberValue = item.DefaultNumberValue;
                    if (string.IsNullOrEmpty(VmGlobalVariableData.NormalizeResultCode(item.DefaultResultCodeValue))) item.DefaultResultCodeValue = "OK";
                    if (string.IsNullOrEmpty(VmGlobalVariableData.NormalizeResultCode(item.ResultCodeValue))) item.ResultCodeValue = item.DefaultResultCodeValue;
                    globalVariables.Add(item);
                }
            }
            finally
            {
                globalVariableUiUpdating = false;
            }
            if (globalVariableView != null) globalVariableView.Refresh();
            VmGlobalVariableData selected = globalVariables.FirstOrDefault(item => string.Equals(item.VariableId, selectedId, StringComparison.OrdinalIgnoreCase));
            if (GlobalVariableListBox != null) GlobalVariableListBox.SelectedItem = selected ?? globalVariables.FirstOrDefault();
            SetGlobalVariableEditor(GlobalVariableListBox == null ? null : GlobalVariableListBox.SelectedItem as VmGlobalVariableData);
        }

        private void RefreshGlobalVariableSubscriptions()
        {
            foreach (VmGlobalVariableData variable in globalVariables)
            {
                int readers = flowTools.Count(tool => tool.Kind == VmToolKind.GlobalVariableRead && tool.Parameters != null && string.Equals(tool.Parameters.GlobalVariableId, variable.VariableId, StringComparison.OrdinalIgnoreCase));
                int writers = flowTools.Count(tool => tool.Kind == VmToolKind.GlobalVariableWrite && tool.Parameters != null && string.Equals(tool.Parameters.GlobalVariableId, variable.VariableId, StringComparison.OrdinalIgnoreCase));
                variable.SetSubscriptionCounts(readers, writers);
            }
            if (globalVariableView != null) globalVariableView.Refresh();
            if (GlobalVariableWorkspaceStatusText != null)
                GlobalVariableWorkspaceStatusText.Text = globalVariables.Count.ToString(CultureInfo.InvariantCulture) + " 个变量 · " + flowTools.Count(tool => tool.Kind == VmToolKind.GlobalVariableRead || tool.Kind == VmToolKind.GlobalVariableWrite).ToString(CultureInfo.InvariantCulture) + " 个读写模块";
            VmGlobalVariableData selected = GlobalVariableListBox == null ? null : GlobalVariableListBox.SelectedItem as VmGlobalVariableData;
            if (selected != null && GlobalVariableSubscriptionText != null) GlobalVariableSubscriptionText.Text = BuildGlobalVariableSubscriptionText(selected);
            RefreshDockGlobalVariableSummary();
        }

        private bool FilterGlobalVariable(object value)
        {
            VmGlobalVariableData item = value as VmGlobalVariableData;
            if (item == null) return false;
            string search = GlobalVariableSearchTextBox == null ? string.Empty : (GlobalVariableSearchTextBox.Text ?? string.Empty).Trim();
            ComboBoxItem selectedType = GlobalVariableTypeFilterComboBox == null ? null : GlobalVariableTypeFilterComboBox.SelectedItem as ComboBoxItem;
            string type = selectedType == null ? "All" : Convert.ToString(selectedType.Tag, CultureInfo.InvariantCulture);
            return (search.Length == 0 || (item.Name ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) &&
                   (string.Equals(type, "All", StringComparison.OrdinalIgnoreCase) || string.Equals(item.ValueType, type, StringComparison.OrdinalIgnoreCase));
        }

        private void GlobalVariableFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (globalVariableView != null) globalVariableView.Refresh();
        }

        private void OpenGlobalVariablesButton_Click(object sender, RoutedEventArgs e)
        {
            if (productionRunMode)
            {
                HeaderStatusText.Text = "请先返回开发模式，再管理全局变量。";
                return;
            }
            DeveloperMoreToggleButton.IsChecked = false;
            RefreshGlobalVariableSubscriptions();
            if (ExternalResourceWorkspace != null) ExternalResourceWorkspace.Visibility = Visibility.Collapsed;
            if (ScriptInterfaceWorkspace != null) ScriptInterfaceWorkspace.Visibility = Visibility.Collapsed;
            MainWorkspaceGrid.Visibility = Visibility.Collapsed;
            GlobalVariableWorkspace.Visibility = Visibility.Visible;
            if (GlobalVariableListBox.SelectedItem == null && globalVariables.Count > 0) GlobalVariableListBox.SelectedIndex = 0;
            HeaderStatusText.Text = "已打开全局变量工作台；关闭后恢复原开发工作区。";
        }

        private void CloseGlobalVariablesButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalVariableWorkspace.Visibility = Visibility.Collapsed;
            MainWorkspaceGrid.Visibility = Visibility.Visible;
            HeaderStatusText.Text = "已返回流程开发工作区。";
        }

        private void GlobalVariableListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (globalVariableUiUpdating) return;
            SetGlobalVariableEditor(GlobalVariableListBox.SelectedItem as VmGlobalVariableData);
        }

        private void SetGlobalVariableEditor(VmGlobalVariableData variable)
        {
            if (GlobalVariableNameTextBox == null) return;
            globalVariableUiUpdating = true;
            try
            {
                bool enabled = variable != null;
                GlobalVariableNameTextBox.IsEnabled = enabled;
                GlobalVariableValueTypeComboBox.IsEnabled = enabled;
                GlobalVariableDefaultValueTextBox.IsEnabled = enabled;
                GlobalVariableCurrentValueTextBox.IsEnabled = enabled;
                GlobalVariableNameTextBox.Text = enabled ? variable.Name : string.Empty;
                GlobalVariableValueTypeComboBox.SelectedValue = enabled ? VmGlobalVariableType.Normalize(variable.ValueType) : VmGlobalVariableType.Number;
                GlobalVariableDefaultValueTextBox.Text = enabled ? variable.DefaultValueText : string.Empty;
                GlobalVariableCurrentValueTextBox.Text = enabled ? variable.CurrentValueText : string.Empty;
                GlobalVariableEditorTitleText.Text = enabled ? "变量详情" : "选择变量查看详情";
                GlobalVariableIdText.Text = enabled ? "ID  " + variable.VariableId : string.Empty;
                GlobalVariableSubscriptionText.Text = enabled ? BuildGlobalVariableSubscriptionText(variable) : "读取 0 · 写入 0";
                GlobalVariableValidationText.Text = string.Empty;
            }
            finally
            {
                globalVariableUiUpdating = false;
            }
        }

        private string BuildGlobalVariableSubscriptionText(VmGlobalVariableData variable)
        {
            List<string> readers = flowTools.Where(tool => tool.Kind == VmToolKind.GlobalVariableRead && tool.Parameters != null && string.Equals(tool.Parameters.GlobalVariableId, variable.VariableId, StringComparison.OrdinalIgnoreCase)).Select(tool => tool.InstanceName).ToList();
            List<string> writers = flowTools.Where(tool => tool.Kind == VmToolKind.GlobalVariableWrite && tool.Parameters != null && string.Equals(tool.Parameters.GlobalVariableId, variable.VariableId, StringComparison.OrdinalIgnoreCase)).Select(tool => tool.InstanceName).ToList();
            return "读取 " + readers.Count.ToString(CultureInfo.InvariantCulture) + (readers.Count == 0 ? string.Empty : "（" + string.Join("、", readers) + "）") +
                   " · 写入 " + writers.Count.ToString(CultureInfo.InvariantCulture) + (writers.Count == 0 ? string.Empty : "（" + string.Join("、", writers) + "）");
        }

        private void NewGlobalVariableButton_Click(object sender, RoutedEventArgs e)
        {
            if (isFlowExecutionActive || isImageQueueBatchRunning) return;
            GlobalVariableSearchTextBox.Text = string.Empty;
            GlobalVariableTypeFilterComboBox.SelectedIndex = 0;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            VmGlobalVariableData item = new VmGlobalVariableData { Name = CreateUniqueGlobalVariableName("Variable") };
            globalVariables.Add(item);
            RefreshGlobalVariableSubscriptions();
            GlobalVariableListBox.SelectedItem = item;
            CommitFlowEditHistory("新建全局变量：" + item.Name, before);
            GlobalVariableNameTextBox.Focus();
            GlobalVariableNameTextBox.SelectAll();
        }

        private string CreateUniqueGlobalVariableName(string baseName)
        {
            string root = string.IsNullOrWhiteSpace(baseName) ? "Variable" : baseName.Trim();
            string candidate = root;
            int index = 2;
            while (globalVariables.Any(item => string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase))) candidate = root + "_" + (index++).ToString("00", CultureInfo.InvariantCulture);
            return candidate;
        }

        private void ApplyGlobalVariableButton_Click(object sender, RoutedEventArgs e)
        {
            VmGlobalVariableData variable = GlobalVariableListBox.SelectedItem as VmGlobalVariableData;
            if (variable == null) { GlobalVariableValidationText.Text = "请先选择或新建变量。"; return; }
            string name = (GlobalVariableNameTextBox.Text ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > 64 || name.Any(char.IsControl)) { GlobalVariableValidationText.Text = "变量名称必须为 1–64 个非控制字符。"; return; }
            if (globalVariables.Any(item => !ReferenceEquals(item, variable) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) { GlobalVariableValidationText.Text = "变量名称不能重复（不区分大小写）。"; return; }
            string type = VmGlobalVariableType.Normalize(GlobalVariableValueTypeComboBox.SelectedValue as string);
            bool subscribed = variable.ReaderCount > 0 || variable.WriterCount > 0;
            if (subscribed && !string.Equals(type, variable.ValueType, StringComparison.OrdinalIgnoreCase)) { GlobalVariableValidationText.Text = "该变量已有流程订阅，不能直接改变数据类型。请先解除读写工具绑定。"; return; }
            object defaultValue;
            object currentValue;
            string error;
            if (!TryParseGlobalVariableValue(type, GlobalVariableDefaultValueTextBox.Text, out defaultValue, out error)) { GlobalVariableValidationText.Text = "默认值无效：" + error; return; }
            if (!TryParseGlobalVariableValue(type, GlobalVariableCurrentValueTextBox.Text, out currentValue, out error)) { GlobalVariableValidationText.Text = "当前值无效：" + error; return; }

            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            variable.Name = name;
            variable.ValueType = type;
            SetGlobalVariableDefaultValue(variable, defaultValue);
            variable.SetCurrentValue(currentValue);
            RefreshGlobalVariableSubscriptions();
            SetGlobalVariableEditor(variable);
            CommitFlowEditHistory("应用全局变量：" + variable.Name, before);
            GlobalVariableValidationText.Text = string.Empty;
            HeaderStatusText.Text = "已应用全局变量：" + variable.Name + "（" + variable.TypeDisplayText + "）。";
        }

        private static bool TryParseGlobalVariableValue(string type, string text, out object value, out string error)
        {
            value = null;
            error = string.Empty;
            string source = text ?? string.Empty;
            switch (VmGlobalVariableType.Normalize(type))
            {
                case VmGlobalVariableType.String:
                    if (source.Length > 4096) { error = "字符串不能超过 4096 个字符。"; return false; }
                    value = source;
                    return true;
                case VmGlobalVariableType.Boolean:
                    bool boolean;
                    if (!bool.TryParse(source.Trim(), out boolean)) { error = "布尔值只允许 True 或 False。"; return false; }
                    value = boolean;
                    return true;
                case VmGlobalVariableType.ResultCode:
                    string code = VmGlobalVariableData.NormalizeResultCode(source);
                    if (code.Length == 0) { error = "结果码只允许 OK 或 NG。"; return false; }
                    value = code;
                    return true;
                default:
                    double number;
                    if (!double.TryParse(source.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) && !double.TryParse(source.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out number)) { error = "请输入有效数值。"; return false; }
                    if (double.IsNaN(number) || double.IsInfinity(number)) { error = "数值必须是有限值，不能为 NaN 或 Infinity。"; return false; }
                    value = number;
                    return true;
            }
        }

        private static void SetGlobalVariableDefaultValue(VmGlobalVariableData variable, object value)
        {
            switch (VmGlobalVariableType.Normalize(variable.ValueType))
            {
                case VmGlobalVariableType.String: variable.DefaultTextValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty; break;
                case VmGlobalVariableType.Boolean: variable.DefaultBooleanValue = Convert.ToBoolean(value, CultureInfo.InvariantCulture); break;
                case VmGlobalVariableType.ResultCode: variable.DefaultResultCodeValue = VmGlobalVariableData.NormalizeResultCode(Convert.ToString(value, CultureInfo.InvariantCulture)); break;
                default: variable.DefaultNumberValue = Convert.ToDouble(value, CultureInfo.InvariantCulture); break;
            }
        }

        private void ResetGlobalVariableButton_Click(object sender, RoutedEventArgs e)
        {
            VmGlobalVariableData variable = GlobalVariableListBox.SelectedItem as VmGlobalVariableData;
            if (variable == null) return;
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            variable.ResetCurrentToDefault();
            SetGlobalVariableEditor(variable);
            CommitFlowEditHistory("恢复变量默认值：" + variable.Name, before);
            HeaderStatusText.Text = "已将 " + variable.Name + " 的当前值恢复为默认值。";
        }

        private void DeleteGlobalVariableButton_Click(object sender, RoutedEventArgs e)
        {
            VmGlobalVariableData variable = GlobalVariableListBox.SelectedItem as VmGlobalVariableData;
            if (variable == null) return;
            RefreshGlobalVariableSubscriptions();
            if (variable.ReaderCount > 0 || variable.WriterCount > 0)
            {
                GlobalVariableValidationText.Text = "不能删除已被流程订阅的变量。请先解除以下绑定：" + BuildGlobalVariableSubscriptionText(variable);
                return;
            }
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            int index = globalVariables.IndexOf(variable);
            globalVariables.Remove(variable);
            RefreshGlobalVariableSubscriptions();
            GlobalVariableListBox.SelectedItem = globalVariables.Count == 0 ? null : globalVariables[Math.Min(index, globalVariables.Count - 1)];
            CommitFlowEditHistory("删除全局变量：" + variable.Name, before);
        }

        private void DockGlobalVariableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDockGlobalVariableSummary();
            if (!dockDraftUpdating) DockDraftValue_Changed(sender, e);
        }

        private void RefreshDockGlobalVariableSummary()
        {
            if (DockGlobalVariableSummaryText == null) return;
            string id = DockGlobalVariableComboBox == null ? null : DockGlobalVariableComboBox.SelectedValue as string;
            VmGlobalVariableData variable = globalVariables.FirstOrDefault(item => string.Equals(item.VariableId, id, StringComparison.OrdinalIgnoreCase));
            if (variable == null)
            {
                DockGlobalVariableSummaryText.Text = globalVariables.Count == 0 ? "当前方案尚无变量，请先打开变量工作台创建。" : "请选择一个有效变量。";
                return;
            }
            DockGlobalVariableSummaryText.Text = variable.TypeDisplayText + " · 当前=" + variable.CurrentValueText + " · " + variable.SubscriptionSummary;
            if (DockGlobalVariableHintText != null && dockEditingTool != null)
                DockGlobalVariableHintText.Text = dockEditingTool.Kind == VmToolKind.GlobalVariableWrite
                    ? "写入时必须连接 " + VmGlobalVariableType.GetWritePortName(variable.ValueType) + "；其他类型端口不会参与执行。"
                    : "运行时发布 " + VmGlobalVariableType.GetWritePortName(variable.ValueType) + "，可直接订阅到脚本、文本、数值或结果判定工具。";
        }

        private string ValidateGlobalVariableDockDraft(VmToolKind kind, VmToolParameterData parameters)
        {
            if (kind != VmToolKind.GlobalVariableRead && kind != VmToolKind.GlobalVariableWrite) return string.Empty;
            VmGlobalVariableData variable = globalVariables.FirstOrDefault(item => parameters != null && string.Equals(item.VariableId, parameters.GlobalVariableId, StringComparison.OrdinalIgnoreCase));
            if (variable == null) return "请选择当前方案中存在的全局变量。";
            if (kind == VmToolKind.GlobalVariableRead) return string.Empty;
            string activePort = VmGlobalVariableType.GetWritePortName(variable.ValueType);
            VmInputPortEditorRow active = dockInputPortRows.FirstOrDefault(row => string.Equals(row.PortName, activePort, StringComparison.OrdinalIgnoreCase));
            if (active == null || string.IsNullOrWhiteSpace(active.SelectedSourceKey)) return "写入“" + variable.Name + "”必须为 " + activePort + " 选择一个上游来源。";
            VmInputPortEditorRow incompatible = dockInputPortRows.FirstOrDefault(row => !string.Equals(row.PortName, activePort, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(row.SelectedSourceKey));
            if (incompatible != null) return "变量“" + variable.Name + "”为 " + variable.TypeDisplayText + "，请清除不匹配的 " + incompatible.PortName + " 订阅。";
            return string.Empty;
        }

        private VmGlobalVariableData GetGlobalVariableForTool(VmToolInstance tool)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string validation = parameters.Validate(tool.Kind);
            if (!string.IsNullOrWhiteSpace(validation)) throw new InvalidOperationException(validation);
            VmGlobalVariableData variable = globalVariables.FirstOrDefault(item => string.Equals(item.VariableId, parameters.GlobalVariableId, StringComparison.OrdinalIgnoreCase));
            if (variable == null) throw new InvalidOperationException("已配置的全局变量不存在，可能已被删除或方案不完整。请重新选择变量。");
            return variable;
        }

        private bool IsGlobalVariableOutputPortActive(VmToolInstance source, string portName)
        {
            return string.IsNullOrWhiteSpace(GetGlobalVariableOutputConfigurationError(source, portName));
        }

        private string GetGlobalVariableOutputConfigurationError(VmToolInstance source, string portName)
        {
            if (source == null || (source.Kind != VmToolKind.GlobalVariableRead && source.Kind != VmToolKind.GlobalVariableWrite)) return string.Empty;
            string[] typedPorts = { "NumberValue", "TextValue", "BooleanValue", "ResultCodeValue" };
            if (!typedPorts.Contains(portName, StringComparer.OrdinalIgnoreCase)) return string.Empty;
            VmGlobalVariableData variable = source.Parameters == null ? null : globalVariables.FirstOrDefault(item => string.Equals(item.VariableId, source.Parameters.GlobalVariableId, StringComparison.OrdinalIgnoreCase));
            if (variable == null) return "上游全局变量不存在或尚未选择。";
            string activePort = VmGlobalVariableType.GetWritePortName(variable.ValueType);
            return string.Equals(activePort, portName, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : "上游变量“" + variable.Name + "”为 " + variable.TypeDisplayText + "，只发布 " + activePort + "，不能订阅 " + portName + "。";
        }

        private InspectionRecord RunGlobalVariableReadTool(VmToolInstance tool)
        {
            VmGlobalVariableData variable = GetGlobalVariableForTool(tool);
            PublishGlobalVariableValue(tool, variable);
            string message = "读取全局变量 · " + variable.Name + " · " + variable.TypeDisplayText + "=" + variable.CurrentValueText;
            InspectionRecord record = CreateRecord("GlobalVariableRead", "OK", GetGlobalVariableScore(variable), message, tool);
            resultStore.Add(record);
            LogInfo(tool.InstanceName + " 完成：" + message);
            return record;
        }

        private InspectionRecord RunGlobalVariableWriteTool(VmToolInstance tool)
        {
            VmGlobalVariableData variable = GetGlobalVariableForTool(tool);
            string activePort = VmGlobalVariableType.GetWritePortName(variable.ValueType);
            List<VmToolInputBindingData> incompatible = tool.InputBindings.Where(binding => !string.Equals(binding.TargetPortName, activePort, StringComparison.OrdinalIgnoreCase)).ToList();
            if (incompatible.Count > 0) throw new InvalidOperationException("变量“" + variable.Name + "”为 " + variable.TypeDisplayText + "，只允许连接 " + activePort + "；请清除其他类型端口订阅。");
            object raw;
            if (!TryResolveOptionalToolInputValue(tool, activePort, out raw)) throw new InvalidOperationException("写入全局变量必须连接 " + activePort + "，且来源必须位于当前模块之前并已运行。 ");
            object oldValue = variable.GetCurrentValue();
            variable.SetCurrentValue(raw);
            object newValue = variable.GetCurrentValue();
            bool changed = !GlobalVariableValuesEqual(oldValue, newValue);
            PublishGlobalVariableValue(tool, variable);
            tool.SetOutputValue("Changed", changed);
            RefreshGlobalVariableSubscriptions();
            RefreshRecipeDirtyStateAfterFlowEdit("运行写入变量：" + variable.Name);
            string message = "写入全局变量 · " + variable.Name + " · " + variable.TypeDisplayText + "=" + variable.CurrentValueText + " · Changed=" + changed;
            InspectionRecord record = CreateRecord("GlobalVariableWrite", "OK", GetGlobalVariableScore(variable), message, tool);
            resultStore.Add(record);
            LogInfo(tool.InstanceName + " 完成：" + message);
            return record;
        }

        private static bool GlobalVariableValuesEqual(object left, object right)
        {
            if (left is double && right is double) return Math.Abs((double)left - (double)right) <= 1e-12;
            return Equals(left, right);
        }

        private static double GetGlobalVariableScore(VmGlobalVariableData variable)
        {
            switch (VmGlobalVariableType.Normalize(variable.ValueType))
            {
                case VmGlobalVariableType.Number: return variable.NumberValue;
                case VmGlobalVariableType.Boolean: return variable.BooleanValue ? 1 : 0;
                case VmGlobalVariableType.String: return (variable.TextValue ?? string.Empty).Length;
                default: return string.Equals(variable.ResultCodeValue, "OK", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
        }

        private static void PublishGlobalVariableValue(VmToolInstance tool, VmGlobalVariableData variable)
        {
            string port = VmGlobalVariableType.GetWritePortName(variable.ValueType);
            tool.SetOutputValue(port, variable.GetCurrentValue());
            tool.SetOutputValue("VariableName", variable.Name);
            tool.SetOutputValue("ValueType", VmGlobalVariableType.Normalize(variable.ValueType));
        }

        private string BuildGlobalVariableToolInputSummary(VmToolInstance tool)
        {
            VmGlobalVariableData variable = tool == null || tool.Parameters == null ? null : globalVariables.FirstOrDefault(item => string.Equals(item.VariableId, tool.Parameters.GlobalVariableId, StringComparison.OrdinalIgnoreCase));
            if (variable == null) return "全局变量未选择";
            if (tool.Kind == VmToolKind.GlobalVariableRead) return variable.Name + " · " + variable.TypeDisplayText + " · 当前=" + variable.CurrentValueText;
            string port = VmGlobalVariableType.GetWritePortName(variable.ValueType);
            VmToolInputBindingData binding = tool.GetInputBinding(port);
            VmToolInstance source = GetInputSourceTool(binding);
            return binding == null || source == null ? port + " ← 未连接" : port + " ← " + source.InstanceName + "." + binding.SourcePortName;
        }
    }
}
