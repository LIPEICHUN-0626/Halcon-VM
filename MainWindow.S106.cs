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
        private void DockExpressionChangedS106(object sender, TextChangedEventArgs e)
        {
            if (DockExpressionTextS106 == null || DockExpressionHintS106 == null) return;
            try
            {
                var formula = VmNumericExpressionS106.Compile(DockExpressionTextS106.Text);
                DockExpressionHintS106.Text = formula.Variables.Count == 0 ? "常量公式 · 无需变量输入" : "在输入页连接：" + string.Join(" / ", formula.Variables);
                DockExpressionHintS106.Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110));
            }
            catch (InvalidOperationException ex)
            {
                DockExpressionHintS106.Text = ex.Message;
                DockExpressionHintS106.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
            }
            if (dockDraftUpdating || dockEditingTool == null || dockEditingTool.Kind != VmToolKind.NumericExpression) return;
            var saved = dockInputPortRows.ToDictionary(row => row.PortName, row => row.SelectedSourceKey);
            var original = dockEditingTool.Parameters;
            bool wasUpdating = dockDraftUpdating;
            try
            {
                dockDraftUpdating = true;
                var preview = (original ?? new VmToolParameterData()).Clone();
                preview.NumericExpressionText = DockExpressionTextS106.Text;
                dockEditingTool.Parameters = preview;
                BuildDockInputPortRows(dockEditingTool);
                foreach (var row in dockInputPortRows)
                {
                    string key;
                    if (saved.TryGetValue(row.PortName, out key) && row.SourceOptions.Any(option => option.Key == key && option.IsValid)) row.SelectedSourceKey = key;
                    RefreshDockInputRowState(row);
                }
            }
            finally { dockEditingTool.Parameters = original; dockDraftUpdating = wasUpdating; }
            DockInputSummaryText.Text = BuildDockInputDraftSummary();
            DockDraftValue_Changed(sender, e);
        }

        private void InsertExpressionS106_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("插入公式参数");
            if (editLeaseS137 < 0) return;
            var owner = sender as FrameworkElement;
            if (owner == null) return;
            var menu = new ContextMenu { PlacementTarget = owner };
            Action<MenuItem, string, string> add = (group, title, text) =>
            {
                var item = new MenuItem { Header = title, ToolTip = text };
                item.Click += delegate
                {
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    if (IsCurrentToolContinuousTrialRunningS87) return;
                    int start = DockExpressionTextS106.SelectionStart;
                    DockExpressionTextS106.SelectedText = text;
                    DockExpressionTextS106.Focus(); DockExpressionTextS106.Select(start, text.Length);
                };
                group.Items.Add(item);
            };
            var patterns = new MenuItem { Header = "常见公式" };
            add(patterns, "两值均值", "(A+B)/2");
            add(patterns, "相对偏差 (%)", "100*(A-B)/abs(B)");
            add(patterns, "线性补偿", "A*B+C");
            add(patterns, "二维距离", "sqrt((A-B)^2+(C-D)^2)");
            menu.Items.Add(patterns);
            var functions = new MenuItem { Header = "数学函数" };
            foreach (var item in new[] { new[] { "绝对值", "abs(A)" }, new[] { "平方根", "sqrt(A)" }, new[] { "最小值", "min(A,B)" },
                new[] { "最大值", "max(A,B)" }, new[] { "幂", "pow(A,2)" }, new[] { "四舍五入", "round(A,2)" }, new[] { "限幅", "clamp(A,0,100)" },
                new[] { "向下取整", "floor(A)" }, new[] { "向上取整", "ceil(A)" }, new[] { "自然对数", "log(A)" }, new[] { "指数", "exp(A)" },
                new[] { "正弦（弧度）", "sin(A)" }, new[] { "余弦（弧度）", "cos(A)" }, new[] { "正切（弧度）", "tan(A)" } }) add(functions, item[0], item[1]);
            menu.Items.Add(functions);
            var variables = new MenuItem { Header = "变量 / 常数" };
            foreach (string name in new[] { "A", "B", "C", "D", "PI", "E" }) add(variables, name, name);
            menu.Items.Add(variables); menu.IsOpen = true;
        }

        private void RefreshExpressionConnectionStatusS106(VmToolInstance tool)
        {
            var formula = VmNumericExpressionS106.Compile((tool.Parameters ?? new VmToolParameterData()).NumericExpressionText);
            var missing = formula.Variables.Where(name => tool.GetInputBinding(name) == null).ToList();
            bool ready = missing.Count == 0 && formula.Variables.All(name =>
            {
                var binding = tool.GetInputBinding(name); var producer = GetInputSourceTool(binding); object raw;
                return producer != null && (producer.ResultCode == "OK" || producer.ResultCode == "NG") && string.IsNullOrWhiteSpace(producer.ErrorMessage) &&
                    producer.TryGetOutputValue(binding.SourcePortName, out raw) && raw != null;
            });
            tool.ConfigurationStatus = missing.Count > 0 ? "待连接变量" : ready ? "就绪" : "等待上游";
            tool.ConnectionStatus = missing.Count > 0 ? "公式需要：" + string.Join(" / ", missing) : formula.Variables.Count == 0 ? "常量公式 · 可运行" : ready ? "公式变量已连接 · 有值" : "公式变量已连接 · 等待上游";
            tool.ConnectionSummary = (formula.Variables.Count == 0 ? "常量" : string.Join(" / ", formula.Variables)) + " → 公式 → Number";
        }

        private InspectionRecord RunExpressionS106(VmToolInstance tool)
        {
            string text = (tool.Parameters ?? new VmToolParameterData()).NumericExpressionText;
            var formula = VmNumericExpressionS106.Compile(text);
            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (string name in formula.Variables)
            {
                VmToolInstance producer;
                object value = ReadBranchInputS101(tool, name, out producer);
                RequireSubflowValueTypeS102("Number", value);
                values.Add(name, Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }
            double result = formula.Evaluate(name => values[name]);
            tool.SetOutputValue("Number", result); tool.SetOutputValue("VariableCount", (double)values.Count);
            tool.SetOutputValue("Formula", text); tool.SetOutputValue("ResultCode", "OK");
            tool.InputSummary = values.Count == 0 ? "常量公式" : string.Join(" · ", values.Select(item => item.Key + "=" + item.Value.ToString("G8", CultureInfo.InvariantCulture)));
            string message = "公式结果 " + result.ToString("G12", CultureInfo.InvariantCulture) + " · " + values.Count + " 个变量";
            tool.OutputSummary = message;
            var record = CreateRecord("NumericExpression", "OK", result, message, tool);
            resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
            return record;
        }

        private void LoadExpressionExampleS106_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("numeric-expression"); }

        private bool BuildExpressionExampleS106()
        {
            var source = CreateFlowTool(VmToolKind.NumberArrayCreate, "01 测量样本", true, null);
            source.Parameters.ArrayText = "10.1,10.2,9.9,10.0,10.3"; source.Parameters.ArrayDelimiter = ","; flowTools.Add(source);
            var statistics = CreateFlowTool(VmToolKind.NumberArrayStatistics, "02 尺寸均值与极值", true, null);
            statistics.SetInputBinding("Array", source.ToolId, "Array"); flowTools.Add(statistics);
            var deviation = CreateFlowTool(VmToolKind.NumericExpression, "03 尺寸偏差百分比", true, null);
            deviation.Parameters.NumericExpressionText = "round(100*(A-10)/10,3)";
            deviation.SetInputBinding("A", statistics.ToolId, "Mean"); flowTools.Add(deviation);
            var range = CreateFlowTool(VmToolKind.NumericExpression, "04 多点尺寸极差", true, null);
            range.Parameters.NumericExpressionText = "A-B";
            range.SetInputBinding("A", statistics.ToolId, "Max"); range.SetInputBinding("B", statistics.ToolId, "Min"); flowTools.Add(range);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "05 平均偏差判定", true, null);
            judge.SetInputBinding("Value", deviation.ToolId, "Number"); judge.InputToolId = deviation.ToolId; judge.InputPortName = "Number";
            judge.NumericLowerLimit = -2; judge.NumericUpperLimit = 2; flowTools.Add(judge);
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false;
            return true;
        }
    }
}
