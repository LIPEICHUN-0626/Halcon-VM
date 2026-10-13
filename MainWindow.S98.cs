using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class SnippetInputChoiceS98
        {
            public string Label { get; set; }
            public string Mode { get; set; }
            public string SourceToolId { get; set; }
            public string SourcePortName { get; set; }
        }

        private static string SnippetInputKeyS98(VmFlowSnippetExternalInputData input)
        {
            return input.TargetTemplateToolId + "|" + input.TargetPortName;
        }

        private Dictionary<string, SnippetInputChoiceS98> ChooseSnippetInputsS98(VmFlowSnippetData snippet, int insertionIndex)
        {
            var choices = new Dictionary<string, SnippetInputChoiceS98>(StringComparer.OrdinalIgnoreCase);
            if (snippet.ExternalInputs == null || snippet.ExternalInputs.Count == 0) return choices;
            var window = new Window { Owner = this, Title = "插入片段 · 连接外部输入", Width = 560, Height = 470,
                MinWidth = 460, MinHeight = 340, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var root = new DockPanel { Margin = new Thickness(14) };
            var title = new TextBlock { Text = snippet.Name + " · " + snippet.ToolFlow.Count + " 个模块\n内部连接保持不变；这里只选择片段之外的数据来源。",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,12) };
            DockPanel.SetDock(title, Dock.Top); root.Children.Add(title);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,12,0,0) };
            var cancel = new Button { Content = "取消", IsCancel = true, MinWidth = 70, Margin = new Thickness(0,0,8,0) };
            var insert = new Button { Content = "插入片段", IsDefault = true, MinWidth = 90 };
            insert.Click += delegate { window.DialogResult = true; };
            actions.Children.Add(cancel); actions.Children.Add(insert);
            DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var rows = new StackPanel();
            foreach (var input in snippet.ExternalInputs)
            {
                string key = SnippetInputKeyS98(input);
                var options = new List<SnippetInputChoiceS98>
                {
                    new SnippetInputChoiceS98 { Mode = "Auto", Label = "自动选择兼容来源（插入后显示结果）" },
                    new SnippetInputChoiceS98 { Mode = "None", Label = "不指定来源（保留模块默认输入语义）" }
                };
                foreach (var source in flowTools.Take(Math.Max(0, insertionIndex)).Where(tool => tool.IsEnabled))
                    foreach (var port in ToolMetadata.GetInstanceOutputPorts(source).Where(port => string.Equals(port.DataType, input.DataType, StringComparison.OrdinalIgnoreCase)))
                        options.Add(new SnippetInputChoiceS98 { Mode = "Explicit", SourceToolId = source.ToolId, SourcePortName = port.PortName,
                            Label = "#" + source.SequenceText + " " + source.InstanceName + " · " + port.DisplayName });
                var row = new StackPanel { Margin = new Thickness(0,0,0,12) };
                row.Children.Add(new TextBlock { Text = input.TargetToolName + " · " + input.TargetPortName + "  /  " + input.DataType + (input.IsRequired ? "  必需" : "  可选"),
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,4) });
                var combo = new ComboBox { ItemsSource = options, DisplayMemberPath = "Label", SelectedIndex = 0,
                    ToolTip = "只列出插入位置之前的启用模块。最终仍按真实绑定规则判断；不合法的选择不自动替换。选择不指定时，Image 等可选默认输入仍遵守模块原有系统图像语义。" };
                choices[key] = options[0];
                combo.SelectionChanged += delegate { choices[key] = combo.SelectedItem as SnippetInputChoiceS98; };
                row.Children.Add(combo); rows.Children.Add(row);
            }
            root.Children.Add(new ScrollViewer { Content = rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            window.Content = root;
            return window.ShowDialog() == true ? choices : null;
        }

        private bool ApplySnippetExplicitInputS98(VmToolInstance target, VmPortDefinition port, SnippetInputChoiceS98 choice)
        {
            target.RemoveInputBinding(port.PortName);
            if (target.Kind == VmToolKind.NumericJudge && port.PortName == "Value")
            {
                target.InputToolId = null;
                target.InputPortName = null;
            }
            if (choice == null || choice.Mode != "Explicit") return false;
            if (IsDuplicateMultiInputEndpoint(target, port.PortName, choice.SourceToolId, choice.SourcePortName)) return false;
            return TrySetAutoBindingS66(target, port, choice.SourceToolId, choice.SourcePortName);
        }
    }
}
