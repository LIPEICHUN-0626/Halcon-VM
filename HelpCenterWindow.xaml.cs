using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class HelpCenterWindow : Window
    {
        private readonly List<HelpTopic> allTopics = new List<HelpTopic>();
        private readonly ObservableCollection<HelpTopic> visibleTopics = new ObservableCollection<HelpTopic>();
        public HelpCenterWindow(VmToolKind? selectedTool)
        {
            InitializeComponent();
            HelpTopicList.ItemsSource = visibleTopics;
            BuildTopics();
            ApplyFilter(string.Empty);
            HelpTopic selected = selectedTool.HasValue
                ? allTopics.FirstOrDefault(item => item.ToolKind.HasValue && item.ToolKind.Value == selectedTool.Value)
                : allTopics.FirstOrDefault();
            HelpTopicList.SelectedItem = selected ?? allTopics.FirstOrDefault();
        }

        private void BuildTopics()
        {
            allTopics.Add(new HelpTopic("快速开始", "从打开图像到运行结果", "QuickStart"));
            allTopics.Add(new HelpTopic("界面与操作习惯", "开发界面、运行界面和中央配置", "Workspace"));
            allTopics.Add(new HelpTopic("标准建项流程", "模块、连接、参数、ROI、验证与保存", "Workflow"));
            allTopics.Add(new HelpTopic("调试与结果", "单步、断点、图层、日志和异常定位", "Debug"));
            foreach (VmToolKind kind in Enum.GetValues(typeof(VmToolKind)).Cast<VmToolKind>()
                .OrderBy(ToolMetadata.GetCategoryOrder)
                .ThenBy(ToolMetadata.GetDisplayName, StringComparer.CurrentCulture))
            {
                allTopics.Add(new HelpTopic(
                    ToolMetadata.GetDisplayName(kind),
                    ToolMetadata.GetCategory(kind) + " · 模块参考",
                    "Tool",
                    kind,
                    ToolMetadata.GetDescription(kind)));
            }
        }

        private void ApplyFilter(string search)
        {
            string query = (search ?? string.Empty).Trim();
            visibleTopics.Clear();
            foreach (HelpTopic topic in allTopics)
            {
                if (query.Length == 0 || topic.SearchText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    visibleTopics.Add(topic);
            }
            HelpStatusText.Text = query.Length == 0
                ? "中文版帮助 · 4 个操作章节 · " + Enum.GetValues(typeof(VmToolKind)).Length + " 个真实模块"
                : "搜索“" + query + "”：" + visibleTopics.Count + " 个结果";
            if (HelpTopicList.SelectedItem == null && visibleTopics.Count > 0) HelpTopicList.SelectedIndex = 0;
        }

        private void HelpSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(HelpSearchTextBox.Text);
        }

        private void HelpTopicList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderTopic(HelpTopicList.SelectedItem as HelpTopic);
        }

        private void RenderTopic(HelpTopic topic)
        {
            HelpDocument.Blocks.Clear();
            if (topic == null) return;
            AddHeading(topic.Title, 25, Colors.White, Color.FromRgb(38, 52, 73));
            if (topic.ToolKind.HasValue) RenderToolTopic(topic.ToolKind.Value);
            else RenderGuideTopic(topic.Key);
        }

        private void RenderToolTopic(VmToolKind kind)
        {
            AddParagraph(ToolMetadata.GetCategory(kind) + " / " + ToolMetadata.GetDisplayName(kind), true, Color.FromRgb(15, 118, 110));
            AddParagraph(ToolMetadata.GetDescription(kind), false, Color.FromRgb(51, 65, 85));
            AddSubheading("什么时候使用");
            AddParagraph("当流程需要“" + ToolMetadata.GetDisplayName(kind) + "”所描述的处理、测量或逻辑能力时使用。先确认上游输出类型与本模块输入类型一致，再进行参数试运行。", false, Color.FromRgb(51, 65, 85));
            AddPortSection("输入", ToolMetadata.GetInputPorts(kind));
            AddPortSection("输出", ToolMetadata.GetOutputPorts(kind));
            AddSubheading("推荐操作顺序");
            AddNumberedSteps(new[]
            {
                "从左侧模块箱把模块拖入流程，放在数据来源之后。",
                "从端口或中央配置的“输入”页连接真实上游数据。",
                "双击模块，在中央配置的“常用”页先设置关键参数；需要时再展开高级参数。",
                "涉及图像区域时在“ROI”页绑定检测区或屏蔽区。",
                "点击“试运行”，在图像区、结果页和步骤记录中同时核对输出。",
                "点击“应用”，再保存方案；应用前的草稿不会改变正式配方。"
            });
            AddSubheading("常见问题");
            AddBullet("显示“待连接”时，先检查输入页的来源端口、数据类型和执行顺序。", Color.FromRgb(71, 85, 105));
            AddBullet("显示 NG 不等于软件异常；先查看 ResultCode、结果数值和步骤记录。", Color.FromRgb(71, 85, 105));
            AddBullet("图像、Region、XLD 等对象由平台管理生命周期，不要把历史图层当作新的算法输入。", Color.FromRgb(71, 85, 105));
        }

        private void RenderGuideTopic(string key)
        {
            if (key == "QuickStart")
            {
                AddParagraph("最快学习方式：从顶部“示例程序”打开一个真实示例，再按流程从上到下逐个双击模块。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[] { "打开示例程序或创建新方案。", "打开图像，确认右上图像区显示正确来源。", "从模块箱加入工具并连接输入。", "双击模块，在中央配置中完成常用参数和试运行。", "查看图像叠加、模块结果与步骤记录。", "应用参数、运行全流程并保存方案。" });
            }
            else if (key == "Workspace")
            {
                AddSubheading("开发界面"); AddParagraph("左侧是模块箱，中间是自上而下的流程，右上是图像，右下是结果和任务入口。双击模块后，配置在中央紧凑工作台打开。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("运行界面"); AddParagraph("顶部“运行界面”只切换到操作员页面，不会自动启动检测。流程执行使用“运行流程”或运行界面的运行按钮。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("按钮说明"); AddParagraph("高频按钮使用明确动词；低频命令收在“更多命令”。把鼠标停在按钮或参数组上，会显示浅色技术说明。", false, Color.FromRgb(51, 65, 85));
            }
            else if (key == "Workflow")
            {
                AddNumberedSteps(new[] { "确定输入图像和验收目标。", "从上到下搭建真实数据链。", "先设置常用参数，再处理输入和 ROI。", "逐模块试运行并检查类型化输出。", "用数值判定、结果逻辑、文本或通讯形成输出闭环。", "保存方案并在运行界面验证。" });
            }
            else
            {
                AddSubheading("调试"); AddParagraph("断点在模块执行前命中；单步一次只执行一个启用模块。停止在工具步骤之间生效，不能中断正在执行的单个 HALCON 算子。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("结果与图层"); AddParagraph("结果页显示真实输出和异常；图像图层只控制显示，不改变算法结果或连接。步骤卡的“记录”用于查看日志和通讯交互。", false, Color.FromRgb(51, 65, 85));
            }
        }

        private void AddPortSection(string title, IList<VmPortDefinition> ports)
        {
            AddSubheading(title);
            if (ports == null || ports.Count == 0) { AddParagraph("无。", false, Color.FromRgb(100, 116, 139)); return; }
            Table table = new Table { CellSpacing = 0, BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), BorderThickness = new Thickness(1) };
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn());
            TableRowGroup group = new TableRowGroup(); table.RowGroups.Add(group);
            group.Rows.Add(CreatePortRow("端口", "类型", "要求", true));
            foreach (VmPortDefinition port in ports) group.Rows.Add(CreatePortRow(port.DisplayName + " (" + port.PortName + ")", port.DataType, port.IsOptional ? "可选" : "必需", false));
            HelpDocument.Blocks.Add(table);
        }

        private static TableRow CreatePortRow(string name, string type, string requirement, bool header)
        {
            TableRow row = new TableRow { Background = new SolidColorBrush(header ? Color.FromRgb(232, 238, 246) : Colors.White) };
            row.Cells.Add(CreateCell(name, header)); row.Cells.Add(CreateCell(type, header)); row.Cells.Add(CreateCell(requirement, header)); return row;
        }

        private static TableCell CreateCell(string text, bool bold)
        {
            return new TableCell(new Paragraph(new Run(text)) { Margin = new Thickness(0), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal }) { Padding = new Thickness(7, 5, 7, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), BorderThickness = new Thickness(0, 0, 1, 1) };
        }

        private void AddHeading(string text, double size, Color foreground, Color background)
        {
            HelpDocument.Blocks.Add(new Paragraph(new Run(text)) { FontSize = size, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(foreground), Background = new SolidColorBrush(background), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 14) });
        }
        private void AddSubheading(string text) { HelpDocument.Blocks.Add(new Paragraph(new Run(text)) { FontSize = 17, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)), Margin = new Thickness(0, 16, 0, 7) }); }
        private void AddParagraph(string text, bool bold, Color color) { HelpDocument.Blocks.Add(new Paragraph(new Run(text)) { FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = new SolidColorBrush(color), LineHeight = 22, Margin = new Thickness(0, 3, 0, 8) }); }
        private void AddBullet(string text, Color color) { System.Windows.Documents.List list = new System.Windows.Documents.List { MarkerStyle = TextMarkerStyle.Disc, Margin = new Thickness(22, 2, 0, 5) }; list.ListItems.Add(new ListItem(new Paragraph(new Run(text)) { Foreground = new SolidColorBrush(color), Margin = new Thickness(0) })); HelpDocument.Blocks.Add(list); }
        private void AddNumberedSteps(IEnumerable<string> steps) { System.Windows.Documents.List list = new System.Windows.Documents.List { MarkerStyle = TextMarkerStyle.Decimal, Margin = new Thickness(26, 4, 0, 8) }; foreach (string step in steps) list.ListItems.Add(new ListItem(new Paragraph(new Run(step)) { Margin = new Thickness(0, 2, 0, 5), LineHeight = 21 })); HelpDocument.Blocks.Add(list); }
        private void CloseButton_Click(object sender, RoutedEventArgs e) { Close(); }

        private sealed class HelpTopic
        {
            public HelpTopic(string title, string subtitle, string key, VmToolKind? toolKind = null, string description = null) { Title = title; Subtitle = subtitle; Key = key; ToolKind = toolKind; SearchText = string.Join(" ", new[] { title, subtitle, key, description ?? string.Empty, toolKind.HasValue ? toolKind.Value.ToString() : string.Empty }); }
            public string Title { get; private set; }
            public string Subtitle { get; private set; }
            public string Key { get; private set; }
            public VmToolKind? ToolKind { get; private set; }
            public string SearchText { get; private set; }
        }
    }
}
