using System;
using System.Collections.Generic;
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
        private void ScriptArrayEditorChangedS107(object sender, RoutedEventArgs e)
        {
            if (!scriptInterfaceUiUpdating) RefreshScriptArrayHintS107();
        }

        private void RefreshScriptArrayHintS107()
        {
            if (ScriptInterfaceArrayHintS107 == null || ScriptInterfaceTypeComboBox == null || ScriptInterfaceDefaultTextBox == null || ScriptInterfaceDirectionComboBox == null) return;
            string type = VmScriptScalarType.Normalize(ScriptInterfaceTypeComboBox.SelectedValue as string);
            string direction = VmScriptMemberDirection.Normalize(ScriptInterfaceDirectionComboBox.SelectedValue as string);
            bool array = VmArrayContractS105.IsType(type);
            ScriptInterfaceDefaultTextBox.Height = array ? 56 : 26;
            ScriptInterfaceDefaultTextBox.AcceptsReturn = array;
            ScriptInterfaceDefaultTextBox.TextWrapping = array ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ScriptInterfaceDefaultTextBox.ToolTip = array ? "JSON 数组，最多 4096 字符。空数组写 []；可选输入留空表示不提供默认值。运行时数组最多 10000 项，文本内容最多 262144 字符。" : "输入默认值仅在未连接且非必需时使用；参数始终使用默认值。";
            ScriptInterfaceArrayHintS107.Visibility = array ? Visibility.Visible : Visibility.Collapsed;
            ScriptInterfaceDefaultRow.Visibility = direction == VmScriptMemberDirection.Output ? Visibility.Collapsed : Visibility.Visible;
            if (!array) return;
            if (direction == VmScriptMemberDirection.Output) return;
            string text = ScriptInterfaceDefaultTextBox.Text ?? string.Empty;
            if (direction == VmScriptMemberDirection.Input && text.Length == 0)
            {
                ScriptInterfaceArrayHintS107.Text = "未提供默认值 · 空数组请显式填写 []";
                ScriptInterfaceArrayHintS107.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                return;
            }
            object value; string error;
            bool valid = VmScriptArrayContractS107.TryParseDefault(type, text, out value, out error);
            ScriptInterfaceArrayHintS107.Text = valid ? value.ToString() : error;
            ScriptInterfaceArrayHintS107.Foreground = new SolidColorBrush(valid ? Color.FromRgb(15, 118, 110) : Color.FromRgb(180, 83, 9));
        }

        private static string ScriptTypeSuffixS107(string type)
        {
            return type == VmScriptScalarType.NumberArray ? "NumberArray" : type == VmScriptScalarType.StringArray ? "StringArray" : type;
        }
        private static string ScriptTypeEmptyS107(string type)
        {
            switch (type)
            {
                case VmScriptScalarType.NumberArray: return "new double[0]";
                case VmScriptScalarType.StringArray: return "new string[0]";
                case VmScriptScalarType.Number: return "0d";
                case VmScriptScalarType.Boolean: return "false";
                case VmScriptScalarType.ResultCode: return "\"OK\"";
                default: return "string.Empty";
            }
        }
        private static string BuildScriptTemplateS107(IList<VmScriptInterfaceMemberData> members)
        {
            var text = new StringBuilder("// 接口读写模板：同类型输入透传到输出；请填写实际处理逻辑。\r\n// 数组读取/写入均为独立副本。此模板本身不代表检测合格。\r\n");
            var byType = new Dictionary<string, string>(StringComparer.Ordinal);
            int index = 0;
            foreach (var member in members.Where(item => item.Direction != VmScriptMemberDirection.Output))
            {
                string local = "value" + index++;
                string getter = "input.Get" + (member.Direction == VmScriptMemberDirection.Parameter ? "Parameter" : "Input") + ScriptTypeSuffixS107(member.ValueType) + "(\"" + member.Name + "\")";
                if (member.Direction == VmScriptMemberDirection.Input && !member.IsRequired)
                    getter = "input.HasInput(\"" + member.Name + "\") ? " + getter + " : " + ScriptTypeEmptyS107(member.ValueType);
                text.AppendLine("var " + local + " = " + getter + ";");
                if (!byType.ContainsKey(member.ValueType)) byType.Add(member.ValueType, local);
            }
            text.AppendLine("// 在此加入自己的计算、筛选或协议解析逻辑。");
            text.AppendLine("var result = ScriptToolResult.Ok(0, \"接口模板运行结束，请实现检测判定\");");
            foreach (var member in members.Where(item => item.Direction == VmScriptMemberDirection.Output))
            {
                string source;
                if (!byType.TryGetValue(member.ValueType, out source)) source = ScriptTypeEmptyS107(member.ValueType);
                text.AppendLine("result.Set" + ScriptTypeSuffixS107(member.ValueType) + "(\"" + member.Name + "\", " + source + ");");
            }
            text.Append("return result;");
            return text.ToString();
        }

        private void GenerateScriptTemplateS107_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("生成脚本代码草稿");
            if (editLeaseS137 < 0) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.CSharpDllScript || IsCurrentToolContinuousTrialRunningS87) return;
            var p = dockEditingTool.Parameters ?? new VmToolParameterData();
            var members = (p.ScriptInterfaceMembers ?? new List<VmScriptInterfaceMemberData>()).Select(item => item.Clone()).ToList();
            string error = VmToolParameterData.ValidateScriptInterfaceMembers(members);
            if (!string.IsNullOrWhiteSpace(error)) { HeaderStatusText.Text = error; return; }
            if (members.Count == 0) { HeaderStatusText.Text = "请先在设计接口中添加并应用输入、参数或输出，再生成代码。"; return; }
            if (!string.IsNullOrWhiteSpace(DockScriptInlineCodeTextBox.Text) && MessageBox.Show(moduleParameterWindowS89 ?? this,
                "将替换当前代码草稿，正式参数仍需点击应用才改变。是否按已应用接口生成读写模板？", "生成接口代码", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            DockScriptInlineCodeTextBox.Text = BuildScriptTemplateS107(members);
            HeaderStatusText.Text = "接口读写模板已写入代码草稿；需要补充真实处理逻辑，再试运行并应用。";
        }

        private void LoadScriptArrayExampleS107_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("script-arrays"); }
        private bool BuildScriptArrayExampleS107()
        {
            var source = CreateFlowTool(VmToolKind.NumberArrayCreate, "01 原始测量值", true, null);
            source.Parameters.ArrayText = "10.1,10.2,9.9,10.0,10.3"; source.Parameters.ArrayDelimiter = ","; flowTools.Add(source);
            var labels = CreateFlowTool(VmToolKind.TextArraySplit, "02 样本标签", true, null);
            labels.Parameters.ArrayText = "P1,P2,P3,P4,P5"; labels.Parameters.ArrayDelimiter = ","; flowTools.Add(labels);
            var script = CreateFlowTool(VmToolKind.CSharpDllScript, "03 脚本逐项补偿", true, null);
            var valuesIn = VmScriptInterfaceMemberData.Create(VmScriptMemberDirection.Input, VmScriptScalarType.NumberArray, "Measurements"); valuesIn.IsRequired = true;
            var labelsIn = VmScriptInterfaceMemberData.Create(VmScriptMemberDirection.Input, VmScriptScalarType.StringArray, "Labels"); labelsIn.IsRequired = true;
            var offset = VmScriptInterfaceMemberData.Create(VmScriptMemberDirection.Parameter, VmScriptScalarType.Number, "Offset"); offset.DefaultValue = "0.05";
            var valuesOut = VmScriptInterfaceMemberData.Create(VmScriptMemberDirection.Output, VmScriptScalarType.NumberArray, "Corrected"); valuesOut.IsRequired = true;
            var labelsOut = VmScriptInterfaceMemberData.Create(VmScriptMemberDirection.Output, VmScriptScalarType.StringArray, "Labels");
            // Interface names are unique across all directions.
            labelsOut.Name = "OutputLabels"; labelsOut.IsRequired = true;
            script.Parameters.ScriptInterfaceMembers = new List<VmScriptInterfaceMemberData> { valuesIn, labelsIn, offset, valuesOut, labelsOut };
            script.Parameters.ScriptExecutionMode = VmScriptExecutionMode.InlineCSharp;
            script.Parameters.ScriptInlineCode =
                "double[] values = input.GetInputNumberArray(\"Measurements\");\r\n" +
                "string[] labels = input.GetInputStringArray(\"Labels\");\r\n" +
                "double offset = input.GetParameterNumber(\"Offset\");\r\n" +
                "if (values.Length == 0 || values.Length != labels.Length) throw new InvalidOperationException(\"测量值与标签数量必须相同且非空\");\r\n" +
                "for (int i = 0; i < values.Length; i++) values[i] += offset;\r\n" +
                "var result = ScriptToolResult.Ok(values.Length, \"逐项补偿完成，不改变上游数组\");\r\n" +
                "result.SetNumberArray(\"Corrected\", values);\r\n" +
                "result.SetStringArray(\"OutputLabels\", labels);\r\nreturn result;";
            script.SetInputBinding(valuesIn.PortName, source.ToolId, "Array"); script.SetInputBinding(labelsIn.PortName, labels.ToolId, "Array"); flowTools.Add(script);
            var statistics = CreateFlowTool(VmToolKind.NumberArrayStatistics, "04 补偿后统计", true, null);
            statistics.SetInputBinding("Array", script.ToolId, valuesOut.PortName); flowTools.Add(statistics);
            var label = CreateFlowTool(VmToolKind.TextArraySelect, "05 首个样本标签", true, null);
            label.SetInputBinding("Array", script.ToolId, labelsOut.PortName); flowTools.Add(label);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "06 补偿后均值判定", true, null);
            judge.SetInputBinding("Value", statistics.ToolId, "Mean"); judge.InputToolId = statistics.ToolId; judge.InputPortName = "Mean";
            judge.NumericLowerLimit = 9.8; judge.NumericUpperLimit = 10.2; flowTools.Add(judge);
            StopOnNgCheckBox.IsChecked = false; AutoSendMatchResultCheckBox.IsChecked = false;
            return true;
        }
    }
}
