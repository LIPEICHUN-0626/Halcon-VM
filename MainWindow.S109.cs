using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool IsVariableEditingBusyS109()
        {
            bool busy = isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87;
            if (busy) GlobalVariableValidationText.Text = "请先停止运行，再修改方案变量。";
            return busy;
        }

        private void ConfigureVariableArrayEditorS109(VmGlobalVariableData variable)
        {
            if (GlobalVariableDefaultArrayButtonS109 == null) return;
            bool array = variable != null && VmArrayContractS105.IsType(variable.ValueType);
            GlobalVariableDefaultValueTextBox.Tag = array ? variable.DefaultArrayJson ?? "[]" : null;
            GlobalVariableCurrentValueTextBox.Tag = array ? variable.CurrentArrayJson ?? "[]" : null;
            SetVariableArrayEditorModeS109(array);
        }

        private void SetVariableArrayEditorModeS109(bool array)
        {
            GlobalVariableDefaultValueTextBox.IsReadOnly = array;
            GlobalVariableCurrentValueTextBox.IsReadOnly = array;
            GlobalVariableDefaultArrayButtonS109.Visibility = array ? Visibility.Visible : Visibility.Collapsed;
            GlobalVariableCurrentArrayButtonS109.Visibility = array ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string VariableEditorTextS109(TextBox box, string type)
        {
            return VmArrayContractS105.IsType(type) ? box.Tag as string ?? "[]" : box.Text;
        }

        private void GlobalVariableTypeChangedS109(object sender, SelectionChangedEventArgs e)
        {
            if (globalVariableUiUpdating || GlobalVariableDefaultArrayButtonS109 == null) return;
            var selected = GlobalVariableListBox.SelectedItem as VmGlobalVariableData;
            string type = VmGlobalVariableType.Normalize(GlobalVariableValueTypeComboBox.SelectedValue as string);
            if (selected != null && selected.ValueType == type)
            {
                GlobalVariableDefaultValueTextBox.Text = selected.DefaultValueText;
                GlobalVariableCurrentValueTextBox.Text = selected.CurrentValueText;
                ConfigureVariableArrayEditorS109(selected);
                return;
            }
            bool array = VmArrayContractS105.IsType(type);
            string value = array ? (type == VmGlobalVariableType.NumberArray ? new VmNumberArrayS105(new double[0]).ToString() : new VmStringArrayS105(new string[0]).ToString())
                : type == VmGlobalVariableType.Number ? "0" : type == VmGlobalVariableType.Boolean ? "False" : type == VmGlobalVariableType.ResultCode ? "OK" : string.Empty;
            GlobalVariableDefaultValueTextBox.Text = GlobalVariableCurrentValueTextBox.Text = value;
            GlobalVariableDefaultValueTextBox.Tag = GlobalVariableCurrentValueTextBox.Tag = array ? "[]" : null;
            SetVariableArrayEditorModeS109(array);
        }

        private void EditVariableArrayS109_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("编辑变量数组草稿");
            if (editLeaseS137 < 0) return;
            if (IsVariableEditingBusyS109()) return;
            var button = sender as Button;
            var variable = GlobalVariableListBox.SelectedItem as VmGlobalVariableData;
            if (button == null || variable == null) return;
            bool useDefault = string.Equals(button.Tag as string, "default", StringComparison.Ordinal);
            TextBox target = useDefault ? GlobalVariableDefaultValueTextBox : GlobalVariableCurrentValueTextBox;
            string type = VmGlobalVariableType.Normalize(GlobalVariableValueTypeComboBox.SelectedValue as string);
            if (!VmArrayContractS105.IsType(type)) return;
            var window = new Window { Owner = this, Title = (useDefault ? "默认数组" : "当前数组") + " · " + variable.Name,
                Width = 640, Height = 420, MinWidth = 440, MinHeight = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            var layout = new DockPanel { Margin = new Thickness(14) };
            var hint = new TextBlock { Text = type == VmGlobalVariableType.NumberArray ? "JSON 数组，例如 [10.2,9.8,10.1]；空数组用 []。" : "JSON 数组，例如 [\"产品 A\",\"产品 B\"]；空字符串用 \"\"。",
                Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8),
                ToolTip = "最多 10000 项；文本内容合计最多 262144 字符；不接受 null、混合类型或非有限数值。写入草稿后，仍须在变量页点击应用。" };
            DockPanel.SetDock(hint, Dock.Top); layout.Children.Add(hint);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var cancel = new Button { Content = "取消", IsCancel = true, MinWidth = 64 };
            var apply = new Button { Content = "写入草稿", MinWidth = 90, Margin = new Thickness(8, 0, 0, 0) };
            actions.Children.Add(cancel); actions.Children.Add(apply); DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
            var error = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            DockPanel.SetDock(error, Dock.Bottom); layout.Children.Add(error);
            var editor = new TextBox { Text = VariableEditorTextS109(target, type), AcceptsReturn = true, AcceptsTab = true,
                FontFamily = new FontFamily("Consolas"), VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, MaxLength = 2097152 };
            layout.Children.Add(editor); window.Content = layout;
            apply.Click += delegate
            {
                object parsed; string message;
                if (!VmScriptArrayContractS107.TryParseArray(type, editor.Text, 2097152, out parsed, out message)) { error.Text = message; return; }
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                target.Tag = VmGlobalVariableData.SerializeArray(parsed); target.Text = parsed.ToString();
                GlobalVariableValidationText.Text = "数组已写入草稿；点击应用后生效。";
                window.DialogResult = true;
            };
            window.ShowDialog();
        }

        private void InvalidateVariableReadersS109(string id, string reason)
        {
            InvalidateScopedDependenciesS102(tool =>
                (tool.ToolType == VmToolKind.GlobalVariableRead.ToString() || tool.ToolType == VmToolKind.GlobalVariableWrite.ToString()) &&
                tool.Parameters != null && string.Equals(tool.Parameters.GlobalVariableId, id, StringComparison.OrdinalIgnoreCase), reason);
        }

        private IEnumerable<VmPortDefinition> GetDockVariableInputPortsS109(VmToolInstance tool)
        {
            var ports = ToolMetadata.GetInstanceInputPorts(tool);
            if (tool.Kind != VmToolKind.GlobalVariableWrite) return ports;
            string id = ReferenceEquals(tool, dockEditingTool) ? DockGlobalVariableComboBox.SelectedValue as string : tool.Parameters?.GlobalVariableId;
            var variable = globalVariables.FirstOrDefault(item => string.Equals(item.VariableId, id, StringComparison.OrdinalIgnoreCase));
            string active = variable == null ? null : VmGlobalVariableType.GetWritePortName(variable.ValueType);
            return ports.Where(port => port.PortName == "$RunCondition" || port.PortName == active).Select(port => new VmPortDefinition
            {
                PortName = port.PortName, DisplayName = port.DisplayName, DataType = port.DataType,
                IsOptional = port.PortName == "$RunCondition" && port.IsOptional
            }).ToList();
        }

        private void RebuildVariableInputDraftS109()
        {
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.GlobalVariableWrite) return;
            var saved = dockInputPortRows.ToDictionary(row => row.PortName, row => row.SelectedSourceKey);
            bool updating = dockDraftUpdating;
            try
            {
                dockDraftUpdating = true;
                BuildDockInputPortRows(dockEditingTool);
                foreach (var row in dockInputPortRows)
                {
                    string key;
                    if (saved.TryGetValue(row.PortName, out key) && row.SourceOptions.Any(option => option.Key == key && option.IsValid)) row.RestoreSourceKeyUI06(key);
                    RefreshDockInputRowState(row);
                }
            }
            finally { dockDraftUpdating = updating; }
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
        }

        private void LoadVariableArraysExampleS109_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("variable-arrays"); }
        private bool BuildVariableArraysExampleS109()
        {
            var dimensions = new VmGlobalVariableData { Name = "尺寸缓存", ValueType = VmGlobalVariableType.NumberArray };
            dimensions.SetDefaultArray(new VmNumberArrayS105(new double[0])); dimensions.ResetCurrentToDefault(); globalVariables.Add(dimensions);
            var labels = new VmGlobalVariableData { Name = "产品标签", ValueType = VmGlobalVariableType.StringArray };
            labels.SetDefaultArray(new VmStringArrayS105(new string[0])); labels.ResetCurrentToDefault(); globalVariables.Add(labels);
            var source = CreateFlowTool(VmToolKind.NumberArrayCreate, "01 本批尺寸", true, null);
            source.Parameters.ArrayText = "10.2,9.8,10.1"; flowTools.Add(source);
            var writer = CreateFlowTool(VmToolKind.GlobalVariableWrite, "02 保存尺寸数组", true, null);
            writer.Parameters.GlobalVariableId = dimensions.VariableId; writer.SetInputBinding("NumberArrayValue", source.ToolId, "Array"); flowTools.Add(writer);
            var read = CreateFlowTool(VmToolKind.GlobalVariableRead, "03 读取尺寸缓存", true, null);
            read.Parameters.GlobalVariableId = dimensions.VariableId; flowTools.Add(read);
            var statistics = CreateFlowTool(VmToolKind.NumberArrayStatistics, "04 缓存尺寸统计", true, null);
            statistics.SetInputBinding("Array", read.ToolId, "NumberArrayValue"); flowTools.Add(statistics);
            var text = CreateFlowTool(VmToolKind.TextArraySplit, "05 本批产品标签", true, null);
            text.Parameters.ArrayText = "P01,P02,P03"; flowTools.Add(text);
            var labelWriter = CreateFlowTool(VmToolKind.GlobalVariableWrite, "06 保存标签数组", true, null);
            labelWriter.Parameters.GlobalVariableId = labels.VariableId; labelWriter.SetInputBinding("StringArrayValue", text.ToolId, "Array"); flowTools.Add(labelWriter);
            var labelRead = CreateFlowTool(VmToolKind.GlobalVariableRead, "07 读取标签缓存", true, null);
            labelRead.Parameters.GlobalVariableId = labels.VariableId; flowTools.Add(labelRead);
            var pick = CreateFlowTool(VmToolKind.TextArraySelect, "08 取第二件标签", true, null);
            pick.Parameters.ArrayIndex = 1; pick.SetInputBinding("Array", labelRead.ToolId, "StringArrayValue"); flowTools.Add(pick);
            RefreshGlobalVariableSubscriptions();
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false;
            return true;
        }
    }
}
