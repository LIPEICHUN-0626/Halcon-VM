using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private List<VmJsonFieldS110> jsonFieldsDraftS110 = new List<VmJsonFieldS110>();
        private void LoadJsonParametersS110(VmToolParameterData p)
        {
            jsonFieldsDraftS110 = VmStructuredDataS110.Fields(p).Select(field => field == null ? null : field.Clone()).ToList();
            DockJsonTextS110.Text = p.JsonText ?? "{\"value\":0}";
            UpdateJsonSummaryS110();
        }
        private void UpdateJsonSummaryS110()
        {
            DockJsonFieldSummaryS110.Text = jsonFieldsDraftS110.Count + " 个字段 · " + string.Join(" / ", jsonFieldsDraftS110.Where(f => f != null).Take(3).Select(f => f.Name));
        }
        private bool ReadJsonParametersS110(VmToolInstance tool, VmToolParameterData p)
        {
            p.JsonFields = jsonFieldsDraftS110.Select(field => field == null ? null : field.Clone()).ToList();
            p.JsonText = DockJsonTextS110.Text;
            string error = VmStructuredDataS110.Validate(tool.Kind, p) ?? JsonFieldCompatibilityS110(tool, p.JsonFields);
            if (!string.IsNullOrEmpty(error)) return FailDockDraft(error);
            if (tool.Kind == VmToolKind.JsonWrite)
                foreach (var field in p.JsonFields)
                {
                    var row = dockInputPortRows.FirstOrDefault(item => item.PortName == field.PortName);
                    if (row == null || !row.SourceOptions.Any(option => option.Key == row.SelectedSourceKey && option.IsValid && !option.IsDefault))
                        return FailDockDraft(field.Name + "：请在输入页选择真实来源。");
                }
            return true;
        }
        private string JsonFieldCompatibilityS110(VmToolInstance tool, IList<VmJsonFieldS110> fields)
        {
            foreach (var old in VmStructuredDataS110.Fields(tool.Parameters ?? new VmToolParameterData()).Where(field => field != null))
            {
                var next = fields.FirstOrDefault(field => field != null && field.Id == old.Id);
                if (next != null && next.DataType == old.DataType) continue;
                if (tool.Kind == VmToolKind.JsonWrite && tool.GetInputBinding(old.PortName) != null)
                    return old.Name + " 已连接；请先在输入页清除连接并应用，再删除或改类型。";
                if (tool.Kind != VmToolKind.JsonRead) continue;
                bool consumed = flowTools.Any(candidate => candidate.InputBindings.Any(binding => binding.SourceToolId == tool.ToolId && binding.SourcePortName == old.PortName));
                consumed |= subflowEditorsS102.Any(frame => frame.Definition.Outputs.Any(port => port.SourceToolId == tool.ToolId && port.SourcePortName == old.PortName));
                if (consumed) return old.Name + " 已被下游或子流程返回引用；请先解除引用，再删除或改类型。";
            }
            return null;
        }
        private void ConfigureJsonFieldsS110_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("编辑JSON字段草稿");
            if (editLeaseS137 < 0) return;
            if (dockEditingTool == null || !VmStructuredDataS110.IsTool(dockEditingTool.Kind) || isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87) return;
            var tool = dockEditingTool;
            var fields = new ObservableCollection<VmJsonFieldS110>(jsonFieldsDraftS110.Where(field => field != null).Select(field => field.Clone()));
            var window = new Window { Owner = this, Title = "字段映射 · " + tool.InstanceName, Width = 700, Height = 430, MinWidth = 480, MinHeight = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            var layout = new DockPanel { Margin = new Thickness(12) };
            var hint = new TextBlock { Text = tool.Kind == VmToolKind.JsonRead ? "按路径读取，例如 /result/score 或 /items/0；字段名就是输出名称。" : "按路径组装对象，例如 /result/score；字段名就是输入名称。",
                Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8),
                ToolTip = "最多 32 字段。路径区分大小写，键中 / 写 ~1，~ 写 ~0。读取可索引数组；组装中路径各层均为对象键，完整数组作为一个字段输入。改名不改变稳定端口身份。" };
            DockPanel.SetDock(hint, Dock.Top); layout.Children.Add(hint);
            var actions = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var apply = new Button { Content = "写入草稿", MinWidth = 90, Margin = new Thickness(8, 0, 0, 0) };
            var cancel = new Button { Content = "取消", IsCancel = true, MinWidth = 64 };
            DockPanel.SetDock(apply, Dock.Right); actions.Children.Add(apply); DockPanel.SetDock(cancel, Dock.Right); actions.Children.Add(cancel);
            var add = new Button { Content = "添加字段", HorizontalAlignment = HorizontalAlignment.Left };
            var remove = new Button { Content = "删除所选", HorizontalAlignment = HorizontalAlignment.Left };
            DockPanel.SetDock(add, Dock.Left); actions.Children.Add(add); DockPanel.SetDock(remove, Dock.Left); actions.Children.Add(remove);
            DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
            var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Firebrick, Margin = new Thickness(0, 8, 0, 0) };
            DockPanel.SetDock(error, Dock.Bottom); layout.Children.Add(error);
            var grid = new DataGrid { ItemsSource = fields, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single, HeadersVisibility = DataGridHeadersVisibility.Column, RowHeaderWidth = 0 };
            grid.Columns.Add(new DataGridTextColumn { Header = "字段名称", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            grid.Columns.Add(new DataGridTextColumn { Header = "JSON 路径", Binding = new Binding("Path"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            grid.Columns.Add(new DataGridComboBoxColumn { Header = "值类型", ItemsSource = VmStructuredDataS110.Types, SelectedItemBinding = new Binding("DataType"), Width = 110 });
            layout.Children.Add(grid); window.Content = layout;
            add.Click += delegate
            {
                if (fields.Count >= 32) { error.Text = "最多 32 个字段。"; return; }
                int index = 1; while (fields.Any(field => field.Name == "字段" + index || field.Path == "/field" + index)) index++;
                var fieldToAdd = new VmJsonFieldS110 { Name = "字段" + index, Path = "/field" + index }; fields.Add(fieldToAdd); grid.SelectedItem = fieldToAdd; grid.ScrollIntoView(fieldToAdd);
            };
            remove.Click += delegate { var selected = grid.SelectedItem as VmJsonFieldS110; if (selected != null) fields.Remove(selected); };
            apply.Click += delegate
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true); grid.CommitEdit(DataGridEditingUnit.Row, true);
                var preview = (tool.Parameters ?? new VmToolParameterData()).Clone(); preview.JsonFields = fields.Select(field => field.Clone()).ToList();
                string message = VmStructuredDataS110.Validate(tool.Kind, preview) ?? JsonFieldCompatibilityS110(tool, preview.JsonFields);
                if (!string.IsNullOrEmpty(message)) { error.Text = message; return; }
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137) || !ReferenceEquals(dockEditingTool, tool)) return;
                jsonFieldsDraftS110 = preview.JsonFields; UpdateJsonSummaryS110(); RebuildJsonInputDraftS110();
                DockDraftValue_Changed(sender, e); window.DialogResult = true;
            };
            window.ShowDialog();
        }
        private void RebuildJsonInputDraftS110()
        {
            if (dockEditingTool == null || !VmStructuredDataS110.IsTool(dockEditingTool.Kind)) return;
            var saved = dockInputPortRows.ToDictionary(row => row.PortName, row => row.SelectedSourceKey);
            var original = dockEditingTool.Parameters; bool updating = dockDraftUpdating;
            try
            {
                dockDraftUpdating = true; var preview = (original ?? new VmToolParameterData()).Clone(); preview.JsonFields = jsonFieldsDraftS110;
                dockEditingTool.Parameters = preview; BuildDockInputPortRows(dockEditingTool);
                foreach (var row in dockInputPortRows)
                {
                    string key; if (saved.TryGetValue(row.PortName, out key) && row.SourceOptions.Any(option => option.Key == key && option.IsValid)) row.RestoreSourceKeyUI06(key);
                    RefreshDockInputRowState(row);
                }
            }
            finally { dockEditingTool.Parameters = original; dockDraftUpdating = updating; }
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
        }
        private string GetJsonDraftBindingErrorS110(VmToolInstance tool, VmToolInputBindingData binding)
        {
            var original = tool.Parameters;
            try
            {
                var preview = (original ?? new VmToolParameterData()).Clone(); preview.JsonFields = jsonFieldsDraftS110; tool.Parameters = preview;
                return GetInputBindingConfigurationError(tool, binding);
            }
            finally { tool.Parameters = original; }
        }
        private void RefreshJsonConnectionS110(VmToolInstance tool)
        {
            var ports = ToolMetadata.GetInstanceInputPorts(tool).Where(port => !port.IsOptional).ToList();
            var missing = ports.Where(port => tool.GetInputBinding(port.PortName) == null).Select(port => port.DisplayName).ToList();
            bool local = tool.Kind == VmToolKind.JsonRead && tool.GetInputBinding("Json") == null;
            tool.ConfigurationStatus = missing.Count > 0 ? "待连接字段" : local ? "本地 JSON" : "已配置";
            tool.ConnectionStatus = missing.Count > 0 ? "需要：" + string.Join(" / ", missing) : local ? "未连接时使用本地文本" : "运行时读取前序有效值";
            tool.ConnectionSummary = VmStructuredDataS110.Fields(tool.Parameters ?? new VmToolParameterData()).Count + " 个字段 · " + (tool.Kind == VmToolKind.JsonRead ? "JSON → 类型化值" : "类型化值 → JSON");
        }
        private InspectionRecord RunJsonToolS110(VmToolInstance tool)
        {
            var p = tool.Parameters ?? new VmToolParameterData(); string error = VmStructuredDataS110.Validate(tool.Kind, p);
            if (error != null) throw new InvalidOperationException(error);
            var fields = VmStructuredDataS110.Fields(p);
            if (tool.Kind == VmToolKind.JsonRead)
            {
                string text = p.JsonText ?? "{\"value\":0}";
                if (tool.GetInputBinding("Json") != null)
                {
                    VmToolInstance producer; object raw = ReadBranchInputS101(tool, "Json", out producer);
                    if (!(raw is string)) throw new InvalidOperationException("JSON 输入必须是 String。"); text = (string)raw;
                }
                var values = VmStructuredDataS110.Read(text, fields);
                foreach (var pair in values) tool.SetOutputValue(pair.Key, pair.Value);
            }
            else
            {
                string text = VmStructuredDataS110.Write(fields, field => { VmToolInstance producer; return ReadBranchInputS101(tool, field.PortName, out producer); });
                tool.SetOutputValue("Json", text); tool.SetOutputValue("Length", (double)text.Length);
            }
            tool.SetOutputValue("FieldCount", (double)fields.Count);
            string message = (tool.Kind == VmToolKind.JsonRead ? "读取" : "组装") + " JSON · " + fields.Count + " 个字段";
            tool.OutputSummary = message; var record = CreateRecord(tool.Kind.ToString(), "OK", fields.Count, message, tool);
            resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message); return record;
        }
        private void ShowJsonOutputS110_Click(object sender, RoutedEventArgs e)
        {
            object value;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.JsonWrite || !string.IsNullOrEmpty(dockEditingTool.ErrorMessage) ||
                (dockEditingTool.ResultCode != "OK" && dockEditingTool.ResultCode != "NG") || !dockEditingTool.TryGetOutputValue("Json", out value) || !(value is string))
            { HeaderStatusText.Text = "请先运行组装模块，生成有效 JSON。"; return; }
            var window = new Window { Owner = this, Title = "JSON 输出 · " + dockEditingTool.InstanceName, Width = 650, Height = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            window.Content = new TextBox { Text = (string)value, IsReadOnly = true, AcceptsReturn = true, FontFamily = new FontFamily("Consolas"),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(12), ToolTip = "本次输出的只读快照；可选中复制，不会触发外发。" };
            window.ShowDialog();
        }
        private void LoadJsonExampleS110_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("structured-data"); }
        private bool BuildJsonExampleS110()
        {
            var read = CreateFlowTool(VmToolKind.JsonRead, "01 读取测量报文", true, null);
            read.Parameters.JsonText = "{\"product\":{\"sn\":\"P01\",\"accepted\":true},\"measurements\":[10.2,9.8,10.1],\"labels\":[\"A\",\"B\",\"C\"]}";
            read.Parameters.JsonFields = new List<VmJsonFieldS110> {
                new VmJsonFieldS110 { Name = "产品编号", Path = "/product/sn", DataType = "String" },
                new VmJsonFieldS110 { Name = "尺寸数组", Path = "/measurements", DataType = "Number[]" },
                new VmJsonFieldS110 { Name = "标签数组", Path = "/labels", DataType = "String[]" },
                new VmJsonFieldS110 { Name = "初始判定", Path = "/product/accepted", DataType = "Boolean" } }; flowTools.Add(read);
            var stats = CreateFlowTool(VmToolKind.NumberArrayStatistics, "02 统计测量尺寸", true, null);
            stats.SetInputBinding("Array", read.ToolId, read.Parameters.JsonFields[1].PortName); flowTools.Add(stats);
            var write = CreateFlowTool(VmToolKind.JsonWrite, "03 组装结果报文", true, null);
            write.Parameters.JsonFields = new List<VmJsonFieldS110> {
                new VmJsonFieldS110 { Name = "产品编号", Path = "/result/sn", DataType = "String" },
                new VmJsonFieldS110 { Name = "均值", Path = "/result/mean", DataType = "Number" },
                new VmJsonFieldS110 { Name = "原始尺寸", Path = "/result/values", DataType = "Number[]" },
                new VmJsonFieldS110 { Name = "原始标签", Path = "/result/labels", DataType = "String[]" },
                new VmJsonFieldS110 { Name = "初始判定", Path = "/result/accepted", DataType = "Boolean" } };
            write.SetInputBinding(write.Parameters.JsonFields[0].PortName, read.ToolId, read.Parameters.JsonFields[0].PortName);
            write.SetInputBinding(write.Parameters.JsonFields[1].PortName, stats.ToolId, "Mean");
            for (int i = 2; i < 5; i++) write.SetInputBinding(write.Parameters.JsonFields[i].PortName, read.ToolId, read.Parameters.JsonFields[i - 1].PortName);
            flowTools.Add(write);
            var back = CreateFlowTool(VmToolKind.JsonRead, "04 下游读取结果", true, null);
            back.Parameters.JsonFields = new List<VmJsonFieldS110> { new VmJsonFieldS110 { Name = "均值", Path = "/result/mean" }, new VmJsonFieldS110 { Name = "首项标签", Path = "/result/labels/0", DataType = "String" } };
            back.SetInputBinding("Json", write.ToolId, "Json"); flowTools.Add(back);
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false; return true;
        }
    }
}
