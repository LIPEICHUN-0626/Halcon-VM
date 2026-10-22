using System;
using System.Collections.Generic;
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
        private void LoadArrayParametersS105(VmToolParameterData p)
        {
            DockArrayTextS105.Text = p.ArrayText ?? string.Empty;
            DockArrayDelimiterS105.Text = p.ArrayDelimiter ?? ",";
            DockArrayIndexS105.Text = p.ArrayIndex.ToString(CultureInfo.InvariantCulture);
        }

        private bool ReadArrayParametersS105(VmToolKind kind, VmToolParameterData p)
        {
            if (VmArrayContractS105.IsSource(kind))
            {
                p.ArrayText = DockArrayTextS105.Text ?? string.Empty;
                p.ArrayDelimiter = DockArrayDelimiterS105.Text ?? string.Empty;
            }
            else
            {
                int index;
                if (!int.TryParse(DockArrayIndexS105.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                    return FailDockDraft("索引必须是从 0 开始的整数。");
                p.ArrayIndex = index;
            }
            return true;
        }

        private void RefreshArrayConnectionStatusS105(VmToolInstance tool)
        {
            bool sourceTool = VmArrayContractS105.IsSource(tool.Kind);
            var binding = tool.GetInputBinding(sourceTool ? "String" : "Array");
            var source = GetInputSourceTool(binding);
            object raw;
            bool ready = source != null && binding != null && (source.ResultCode == "OK" || source.ResultCode == "NG") &&
                string.IsNullOrWhiteSpace(source.ErrorMessage) && source.TryGetOutputValue(binding.SourcePortName, out raw) && raw != null;
            tool.ConfigurationStatus = binding == null ? (sourceTool ? "就绪" : "待连接数组") : (ready ? "就绪" : "等待上游");
            tool.ConnectionStatus = binding == null ? (sourceTool ? "本地文本 · 可运行" : "请选择数组来源") : (ready ? "已连接 · 有值" : "已连接 · 等待本周期值");
            tool.ConnectionSummary = binding == null ? (sourceTool ? "本地文本 → 数组" : "数组 → 统计 / 取值") :
                (source == null ? "未找到来源" : source.InstanceName + "." + binding.SourcePortName) + " → " + (sourceTool ? "数组" : "统计 / 取值");
        }

        private InspectionRecord RunArrayToolS105(VmToolInstance tool)
        {
            var p = tool.Parameters ?? new VmToolParameterData();
            string error = VmArrayContractS105.Validate(tool.Kind, p);
            if (error != null) throw new InvalidOperationException(error);
            int count; double score; string message;
            if (VmArrayContractS105.IsSource(tool.Kind))
            {
                string input = p.ArrayText ?? string.Empty;
                if (tool.GetInputBinding("String") != null)
                {
                    VmToolInstance producer;
                    object raw = ReadBranchInputS101(tool, "String", out producer);
                    if (!(raw is string)) throw new InvalidOperationException("数组生成的输入必须是 String。");
                    input = (string)raw;
                }
                string[] fields = VmArrayContractS105.Split(input, p.ArrayDelimiter);
                object array;
                if (tool.Kind == VmToolKind.NumberArrayCreate)
                {
                    var numbers = new double[fields.Length];
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (!double.TryParse(fields[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]) ||
                            double.IsNaN(numbers[i]) || double.IsInfinity(numbers[i]))
                            throw new InvalidOperationException("索引 " + i + " 不是有限数值：" + VmArrayContractS105.ShortText(fields[i]) + "。不会跳过空项或异常项；小数请使用英文句点。");
                    }
                    array = new VmNumberArrayS105(numbers);
                }
                else array = new VmStringArrayS105(fields);
                count = fields.Length; score = count;
                tool.SetOutputValue("Array", array);
                message = array.ToString();
                tool.InputSummary = tool.GetInputBinding("String") == null ? "本地文本 · 字面分隔" : "上游 String · 字面分隔";
            }
            else
            {
                VmToolInstance producer;
                object raw = ReadBranchInputS101(tool, "Array", out producer);
                bool numeric = tool.Kind == VmToolKind.NumberArrayStatistics;
                VmArrayContractS105.RequireType(numeric ? "Number[]" : "String[]", raw);
                count = numeric ? ((VmNumberArrayS105)raw).Count : ((VmStringArrayS105)raw).Count;
                if (count == 0) throw new InvalidOperationException("输入数组为空，没有可统计或提取的元素。");
                if (p.ArrayIndex >= count) throw new InvalidOperationException("索引 " + p.ArrayIndex + " 越界；当前 " + count + " 项，有效范围为 0–" + (count - 1) + "。");
                if (numeric)
                {
                    var array = (VmNumberArrayS105)raw;
                    double min = array[0], max = array[0], sum = 0, compensation = 0;
                    for (int i = 0; i < count; i++)
                    {
                        min = Math.Min(min, array[i]); max = Math.Max(max, array[i]);
                        double corrected = array[i] - compensation;
                        double next = sum + corrected;
                        if (double.IsNaN(next) || double.IsInfinity(next)) throw new InvalidOperationException("数组求和超出有限数值范围，请缩小输入范围。");
                        compensation = (next - sum) - corrected; sum = next;
                    }
                    score = array[p.ArrayIndex];
                    tool.SetOutputValue("SelectedValue", score);
                    tool.SetOutputValue("Min", min); tool.SetOutputValue("Max", max);
                    tool.SetOutputValue("Mean", sum / count); tool.SetOutputValue("Sum", sum);
                    message = string.Format(CultureInfo.InvariantCulture, "{0} 项 · 均值 {1:G8} · 范围 {2:G8}–{3:G8} · [{4}]={5:G8}", count, sum / count, min, max, p.ArrayIndex, score);
                }
                else
                {
                    string value = ((VmStringArrayS105)raw)[p.ArrayIndex]; score = value.Length;
                    tool.SetOutputValue("Text", value); tool.SetOutputValue("Length", (double)value.Length);
                    message = count + " 项 · [" + p.ArrayIndex + "]=" + VmArrayContractS105.ShortText(value);
                }
                tool.SetOutputValue("Index", (double)p.ArrayIndex);
                tool.InputSummary = producer.InstanceName + " · " + count + " 项";
            }
            tool.SetOutputValue("Count", (double)count); tool.SetOutputValue("ResultCode", "OK");
            tool.OutputSummary = message;
            var record = CreateRecord(tool.Kind.ToString(), "OK", score, message, tool);
            resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
            return record;
        }

        private sealed class ArrayDetailRowS105
        {
            public int Index { get; set; }
            public string SourceIndex { get; set; }
            public string Value { get; set; }
        }

        private void ShowArrayDetailsS105_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            VmToolInstance tool; VmPortDefinition port; object raw;
            if (!TryResolveDetailValueUI06(element == null ? null : element.DataContext, out tool, out port, out raw) || !VmArrayContractS105.IsType(port.DataType)) return;
            BuildArrayDetailWindowUI06(tool, port, raw).ShowDialog();
        }

        private Window BuildArrayDetailWindowUI06(VmToolInstance tool, VmPortDefinition port, object raw)
        {
            VmArrayContractS105.RequireType(port.DataType, raw);
            object snapshot = VmArrayContractS105.Clone(raw);
            var numbers = snapshot as VmNumberArrayS105; var texts = snapshot as VmStringArrayS105;
            int count = numbers != null ? numbers.Count : texts.Count;
            VmNumberArrayS105 sourceIndices = null;
            object indexValue;
            if (VmArrayReorderS108.IsTool(tool.Kind) && port.PortName == "Array" && tool.TryGetOutputValue("Indices", out indexValue) &&
                indexValue is VmNumberArrayS105 && ((VmNumberArrayS105)indexValue).Count == count) sourceIndices = ((VmNumberArrayS105)indexValue).Clone();
            var root = new DockPanel { Margin = new Thickness(12) };
            var header = new TextBlock { Text = tool.InstanceName + " · " + port.DisplayName + " · " + count + " 项\n索引从 0 开始；只读快照，不随重跑变化。" +
                "\n" + BuildResultOriginTextS88(tool) +
                (sourceIndices == null ? "" : "\n来源索引相对本模块输入；合并时以第一数组 + 追加数组为一组。"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,10) };
            DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,8,0,0) };
            var previous = new Button { Content = "上一页", Padding = new Thickness(8,3,8,3) };
            var next = new Button { Content = "下一页", Padding = new Thickness(8,3,8,3), Margin = new Thickness(6,0,0,0) };
            var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12,0,0,0) };
            actions.Children.Add(previous); actions.Children.Add(next); actions.Children.Add(status);
            DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, CanUserDeleteRows = false, CanUserSortColumns = false, HeadersVisibility = DataGridHeadersVisibility.Column };
            grid.Columns.Add(new DataGridTextColumn { Header = "索引（从 0 开始）", Binding = new Binding("Index"), Width = new DataGridLength(120) });
            if (sourceIndices != null) grid.Columns.Add(new DataGridTextColumn { Header = "来源索引", Binding = new Binding("SourceIndex"), Width = new DataGridLength(85) });
            grid.Columns.Add(new DataGridTextColumn { Header = "值（保留原始文本）", Binding = new Binding("Value"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            root.Children.Add(grid);
            int page = 0; const int size = 100; int pages = Math.Max(1, (count + size - 1) / size);
            Action refresh = delegate
            {
                grid.ItemsSource = Enumerable.Range(page * size, Math.Min(size, count - page * size)).Select(index => new ArrayDetailRowS105
                { Index = index, SourceIndex = sourceIndices == null ? "" : sourceIndices[index].ToString("G17", CultureInfo.InvariantCulture), Value = numbers != null ? numbers[index].ToString("G17", CultureInfo.InvariantCulture) : texts[index] }).ToList();
                previous.IsEnabled = page > 0; next.IsEnabled = page + 1 < pages;
                status.Text = (page + 1) + " / " + pages + " 页";
            };
            previous.Click += delegate { if (page > 0) { page--; refresh(); } };
            next.Click += delegate { if (page + 1 < pages) { page++; refresh(); } }; refresh();
            var detail = new Window { Owner = ShowActivated ? moduleParameterWindowS89 ?? this : null, ShowActivated = ShowActivated,
                Title = "数组明细 · " + tool.InstanceName, Content = root, Width = 570, Height = 420,
                MinWidth = 420, MinHeight = 300, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (!ShowActivated) { detail.WindowStartupLocation = WindowStartupLocation.Manual; detail.Left = Left; detail.Top = Top; }
            return detail;
        }

        private void LoadArrayExampleS105_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("array-data"); }

        private bool BuildArrayExampleS105()
        {
            var source = CreateFlowTool(VmToolKind.NumberArrayCreate, "01 多点测量数值", true, null);
            source.Parameters.ArrayText = "10.1,10.2,9.9,10.0,10.3"; source.Parameters.ArrayDelimiter = ","; flowTools.Add(source);
            var statistics = CreateFlowTool(VmToolKind.NumberArrayStatistics, "01 批量数值统计", true, null);
            var call = CreateFlowTool(VmToolKind.SubflowCall, "02 数组统计子流程", true, null);
            try
            {
                statistics.SetInputBinding("Array", VmSubflowData.InputProxyId, "Values");
                call.Parameters.Subflow = new VmSubflowData
                {
                    Tools = CaptureFlowRecipeS102(new[] { statistics }),
                    Inputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "Values", DisplayName = "测量值数组", DataType = "Number[]" } },
                    Outputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "Mean", DisplayName = "平均尺寸", DataType = "Number", SourceToolId = statistics.ToolId, SourcePortName = "Mean" } }
                };
                call.SetInputBinding("Values", source.ToolId, "Array"); flowTools.Add(call);
            }
            catch { call.Dispose(); throw; }
            finally { statistics.Dispose(); }
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "03 平均尺寸判定", true, null);
            judge.SetInputBinding("Value", call.ToolId, "Mean"); judge.InputToolId = call.ToolId; judge.InputPortName = "Mean";
            judge.NumericLowerLimit = 9.8; judge.NumericUpperLimit = 10.2; flowTools.Add(judge);
            var labels = CreateFlowTool(VmToolKind.TextArraySplit, "04 输出字段", true, null);
            labels.Parameters.ArrayText = "尺寸检测,工位01,复核"; labels.Parameters.ArrayDelimiter = ","; flowTools.Add(labels);
            var label = CreateFlowTool(VmToolKind.TextArraySelect, "05 选择工位字段", true, null);
            label.Parameters.ArrayIndex = 1; label.SetInputBinding("Array", labels.ToolId, "Array"); flowTools.Add(label);
            var output = CreateFlowTool(VmToolKind.TextAssembly, "06 组装检测报文", true, null);
            output.Parameters.TextAssemblyTemplate = "{String},{Number},{ResultCode}";
            output.SetInputBinding("String", label.ToolId, "Text"); output.SetInputBinding("Number", call.ToolId, "Mean"); output.SetInputBinding("ResultCode", judge.ToolId, "ResultCode"); flowTools.Add(output);
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false;
            return true;
        }
    }
}
