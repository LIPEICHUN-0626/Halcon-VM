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
        private bool filteringTopicsUI10;
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
            allTopics.Add(new HelpTopic("学习示例与完整项目", "原示例分类、当前一步与多流程学习", "LearningProjects"));
            allTopics.Add(new HelpTopic("界面与操作习惯", "开发界面、运行界面和参数小窗", "Workspace"));
            allTopics.Add(new HelpTopic("标准建项流程", "模块、连接、参数、ROI、验证与保存", "Workflow"));
            allTopics.Add(new HelpTopic("调试与结果", "单步、断点、图层、日志和异常定位", "Debug"));
            allTopics.Add(new HelpTopic("方案换型与运行准备", "当前准备、目标方案预览和显式换型", "RecipeChangeover"));
            allTopics.Add(new HelpTopic("工程迁移与目标机器准备", "真实资源随行、缺失修复和运行环境边界", "EngineeringDeployment"));
            allTopics.Add(new HelpTopic("运行周期记录与追溯", "完整周期、来源区分和跨重启只读查询", "ProductionRunRecords"));
            allTopics.Add(new HelpTopic("周期趋势与过程观察", "耗时、真实数值、范围隔离和同周期明细", "ProductionTrends"));
            allTopics.Add(new HelpTopic("生产页自定义结果项", "选择正式输出、当前周期值和缺失状态", "ProductionResultItems"));
            allTopics.Add(new HelpTopic("周期原图证据", "显式图像源、失败留图与只读历史查看", "ProductionImageEvidence"));
            allTopics.Add(new HelpTopic("报警查看与处置", "发生事实、确认阅读和人工处置说明", "ProductionAlarms"));
            allTopics.Add(new HelpTopic("工程编辑保护", "本机口令/密码、权限、锁定与批准换型", "StationEditingProtection"));
            foreach (VmToolKind kind in Enum.GetValues(typeof(VmToolKind)).Cast<VmToolKind>()
                .OrderBy(ToolMetadata.GetCategoryOrder)
                .ThenBy(ToolMetadata.GetDisplayName, StringComparer.CurrentCulture))
            {
                allTopics.Add(new HelpTopic(
                    ToolMetadata.GetDisplayName(kind),
                    ToolMetadata.GetCategory(kind) + " · 模块参考",
                    "Tool",
                    kind,
                    ToolMetadata.GetDescription(kind) + " " + string.Join(" ",
                        ToolMetadata.GetInputPorts(kind).Concat(ToolMetadata.GetOutputPorts(kind))
                        .Select(port => port.PortName + " " + port.DisplayName + " " + port.DataType))));
            }
        }

        private void ApplyFilter(string search)
        {
            if (HelpTopicList == null || HelpStatusText == null || HelpDocument == null) return;
            string query = (search ?? string.Empty).Trim();
            string[] words = query.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var selected = HelpTopicList.SelectedItem as HelpTopic;
            int scope = HelpScopeUI10 == null ? 0 : HelpScopeUI10.SelectedIndex;
            filteringTopicsUI10 = true;
            try
            {
                visibleTopics.Clear();
                foreach (HelpTopic topic in allTopics)
                {
                    if (scope == 1 && topic.ToolKind.HasValue || scope == 2 && !topic.ToolKind.HasValue) continue;
                    if (words.All(word => topic.SearchText.IndexOf(word, StringComparison.CurrentCultureIgnoreCase) >= 0))
                        visibleTopics.Add(topic);
                }
                HelpTopicList.SelectedItem = selected != null && visibleTopics.Contains(selected) ? selected : visibleTopics.FirstOrDefault();
            }
            finally { filteringTopicsUI10 = false; }
            HelpStatusText.Text = "当前目录 " + visibleTopics.Count + " 项 · 帮助只读，不运行或修改方案";
            if (HelpClearSearchUI10 != null) HelpClearSearchUI10.Visibility = query.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            RenderTopic(HelpTopicList.SelectedItem as HelpTopic);
        }

        private void HelpScopeUI10_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { ApplyFilter(HelpSearchTextBox == null ? string.Empty : HelpSearchTextBox.Text); }

        private void HelpClearSearchUI10_Click(object sender, RoutedEventArgs e)
        { HelpSearchTextBox.Clear(); }

        public void SelectToolTopicUI10(VmToolKind? kind)
        {
            HelpScopeUI10.SelectedIndex = 0; HelpSearchTextBox.Clear(); ApplyFilter(string.Empty);
            HelpTopicList.SelectedItem = kind.HasValue ? allTopics.FirstOrDefault(topic => topic.ToolKind == kind) : allTopics.FirstOrDefault();
            HelpTopicList.ScrollIntoView(HelpTopicList.SelectedItem);
        }

        private void AddTopicLinkUI10(string text, string key)
        {
            var link = new Hyperlink(new Run(text)) { ToolTip = "只打开对应帮助章节，不运行或改变方案。" };
            link.Click += delegate
            {
                HelpScopeUI10.SelectedIndex = 0; HelpSearchTextBox.Clear(); ApplyFilter(string.Empty);
                HelpTopicList.SelectedItem = allTopics.FirstOrDefault(topic => topic.Key == key);
                HelpTopicList.ScrollIntoView(HelpTopicList.SelectedItem);
            };
            HelpDocument.Blocks.Add(new Paragraph(link) { Margin = new Thickness(0, 3, 0, 3) });
        }

        private void HelpSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(HelpSearchTextBox.Text);
        }

        private void HelpTopicList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (filteringTopicsUI10) return;
            RenderTopic(HelpTopicList.SelectedItem as HelpTopic);
        }

        private void RenderTopic(HelpTopic topic)
        {
            HelpDocument.Blocks.Clear();
            if (topic == null)
            {
                AddSubheading("没有匹配的帮助");
                AddParagraph("试着缩短关键词、切换到全部目录，或清空搜索。这里不会改变当前方案。", false, Color.FromRgb(71, 85, 105));
                return;
            }
            AddHeading(topic.Title, 25, Colors.White, Color.FromRgb(38, 52, 73));
            if (topic.ToolKind.HasValue) RenderToolTopic(topic.ToolKind.Value);
            else RenderGuideTopic(topic.Key);
        }

        private void RenderToolTopic(VmToolKind kind)
        {
            if (kind == VmToolKind.ConnectedObjectAnalysisS144)
            {
                AddParagraph("分析真实连通对象，再将每个对象的中心、面积和选中区域接给下游。部分实现（待集中验证）。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[] { "在检测/分割分类添加连通域对象分析，输入页明确连接已运行阈值/区域工具的Region。双击原参数小窗，不改变流程布局。", "常用仅四项：面积下限/上限、排序规则、选取编号（从0开始）。输入先合并域，再按8邻域求连通对象；重叠区域不会重复计数。", "F5看本次对象数、是否选中、选中编号/面积。按需打开对象明细；选行的紫色高亮只是查看，绿色才是冻结的正式选取。重跑后旧明细不跟新图定位。", "需要选取查看对象时明确点击填入草稿，返回原小窗F5/应用，再保存；查看本身不改变下游。Centers接点集测量，Areas接数组统计，SelectedRegion接区域/灰度工具，Count或SelectedArea接数值判定。" });
                AddBullet("所有输出同序。SourceIndices是合并后HALCON连接结果的原编号（从0开始），不是原输入集合编号。面积为px²，中心为px；面积/行/列排序有明确同值决胜，不保证不同帧同编号就是同一个物理对象。此工具不是跟踪器。");
                AddBullet("无对象时Count=0、TotalArea=0、数组和Region为空。选取越界时HasSelection=false，SelectedRegion/SelectedPoint/SelectedArea/SelectedIndex无值，不用零几何或旧对象补齐。ResultCode=OK只表示分析成功，不是产品合格；将对象数/面积/HasSelection接判定。空数组统计模块按其自身契约明确拒绝，不伪造均值。");
                AddBullet("输入最多256对象，累计区域面积最多100000 px²（重叠计预算），坐标0–16383，连接结果最多1000对象。超预算明确拒绝不截断；请在上游限定ROI/去噪。图上编号最多100个但明细/数组完整。首版不支持本地/继承ROI、屏蔽或位置修正；HALCON进程须保持max_connection=0、neighborhood=8，工具只检查不修改全局设置。受信任DLL不得在运行中更改这些进程参数。");
                AddPortSection("输入", ToolMetadata.GetInputPorts(kind)); AddPortSection("输出", ToolMetadata.GetOutputPorts(kind)); return;
            }
            if (kind == VmToolKind.ImageEdgeLineFindingS143)
            {
                AddParagraph("在输入图像上画窄搜索带，沿法向采单边，再拟合可订阅的像素直线。部分实现，待集中验证。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[] { "在测量分类添加边缘找直线，直接连接已运行图像源 Image；双击打开原参数小窗。", "常用只选四项：极性、阈值、采样数、搜索半宽。点击图像编辑搜索带，拖动起终点/中心/半宽，箭头就是实际搜索方向；调换端点会改变极性意义，程序不会自动翻转选项。", "F5 只试运行当前草稿。结果看找到/未找到、有效点、方向、残差；更多端口中的 Samples 保留逐采样索引/位置/幅值/拒绝原因。", "应用后将 MainLine 接原点线/线线几何工具、EdgePoints 接点集分析、数值接数值判定。保存保留搜索几何和绑定；示例菜单提供不自动运行的学习方案。" });
                AddBullet("首次拟合为 HALCON Tukey/Huber，质量拒绝是显式像素残差门槛，不冒充算子内部权重。最终保留点须满足残差、数量与沿基线跨度要求；缺线不输出零角度、零端点或旧线。找到几何不等于产品合格。");
                AddBullet("支持 byte/uint2/real 单通道及 byte RGB（明确 rgb1_to_gray），不缩放高位原值。每个窗口连同双线性边界余量都须在真实有效域内；采样最多200，输入最多1600万像素/边长16384。首版不支持本地/继承 ROI、屏蔽或位置修正；需要时先在上游生成正确有效域，不展示未实现选项。");
                AddPortSection("输入", ToolMetadata.GetInputPorts(kind)); AddPortSection("输出", ToolMetadata.GetOutputPorts(kind));
                return;
            }
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
                "从端口或参数小窗的“输入”页连接真实上游数据。",
                "双击模块，在参数小窗的“常用”页先设置最多 4 项关键参数；其余进入更多参数。",
                "涉及图像区域时在“ROI”页绑定检测区或屏蔽区。",
                "点击“试运行”，在图像区、结果页和步骤记录中同时核对输出。",
                "点击“应用”，再保存方案；应用前的草稿不会改变正式配方。"
            });
            if (VmArrayReorderS108.IsTool(kind))
            {
                AddSubheading("排序、切片、合并与标签对应");
                AddNumberedSteps(new[]
                {
                    "从逻辑/计算添加数值数组整理或文本数组整理，在常用页选择任务。排序、合并和重排只显示任务选择；切片另显示起始索引与数量。切换任务后，输入页只显示所需端口，应用时清除不适用连接。",
                    "排序连接 Array；同值保留原有先后。切片按从 0 开始的起点保序取指定数量。合并连接 Array 和 OtherArray，结果始终第一数组在前、追加数组在后。",
                    "让标签跟随数值排序：文本数组整理选择按索引重排，Array 接原始标签，Indices 和 SourceCount 分别接数值排序模块同名输出。切片后继续关联时，Array 改接已经按上一步排序的标签，不要再次接最初标签。",
                    "结果默认看数量、数组短摘要和任务。展开更多后打开 Array 明细，并排查看当前索引、来源索引和原始值；Indices 本身也可按普通数组查看。",
                    "输出 Array 可继续连接统计/取值、脚本、分支和子流程；应用参数后保存方案。学习入口为示例程序 → 数据 · 数组整理与标签关联（无需图像），不自动运行或外发。"
                });
                AddBullet("来源索引只相对本模块直接输入，不是累计追溯到最初源的全局索引。合并时以第一数组 + 追加数组组成逻辑输入，FirstCount 标明追加部分起点，SourceCount 是总量。标签重排要求来源数量相等且每项索引为有效整数；允许显式重复索引，保留索引列表的原顺序。数量相等不能证明业务身份相同，仍应连接对应标签。", Color.FromRgb(71, 85, 105));
                AddBullet("文本按 Ordinal 字符顺序，区分大小写，不是拼音或自然数排序。空数组可排序、合并、空索引重排；切片允许起点等于输入数量且长度为 0。越界不自动裁剪，合并/重复取项后的数量最多 10000，文本内容总计最多 262144 字符，超限明确失败；所有输出均为独立数组。", Color.FromRgb(71, 85, 105));
                AddBullet("示例学习预期：第一批 3,1,1,2 升序后对应原索引 1,2,3,0，相同值先后不变；从排后索引 1 保留 2 项，再追加第二批 4,5，标签使用同一排序/切片索引并按同顺序合并。以上是设计预期，尚非已验证运行结论。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.GlobalVariableRead || kind == VmToolKind.GlobalVariableWrite)
            {
                AddSubheading("方案变量与数组缓存");
                AddNumberedSteps(new[]
                {
                    "打开现有全局变量页，新建变量，选择数值、字符串、布尔、结果码、数值数组或文本数组。数组首屏只显示数量与短摘要，点击默认值或当前值旁的编辑查看完整内容。",
                    "数组采用 JSON，例如 [10.2,9.8] 或 [\"P01\",\"P02\"]；空数组写 []。写入草稿后仍须点击应用。最多 10000 项，文本内容合计最多 262144 字符，不接受 null、混合类型、嵌套数组和非有限数值。",
                    "双击读取或写入全局变量模块，在常用页选变量。写入模块的输入页只显示当前类型所需的值及可选执行条件；连接前序真实输出，应用后运行。读取模块发布所选类型的端口，数组可以继续订阅到统计、整理、脚本、分支和子流程。",
                    "默认值供显式重置使用，当前值才是运行读写的数据。保存包含两者，重新打开恢复保存时的当前值；打开方案不会自动清空数组。手动修改或恢复默认值后，下游须重新运行。",
                    "示例程序 → 数据 · 方案数组缓存与读写（无需图像）包含尺寸、标签两条写入/读取链。先运行，再在变量页查看；第二次写入相同数组时 Changed 应为 False。修改任意元素再运行，Changed 应为 True。此为设计预期，仍待集中验证。"
                });
                AddBullet("变量按稳定 ID 在同一方案的根流程与子流程间共享，按实际执行顺序读写，数组传递独立复制。已读取的输出是该次读取的快照，不是会自动变化的实时引用；它不是数据库、消息队列或并行共享内存。已有读写引用时禁止删除或改类型。");
            }
            if (VmStructuredDataS110.IsTool(kind))
            {
                AddSubheading("JSON 与类型化字段");
                AddNumberedSteps(new[]
                {
                    "从逻辑/计算添加结构化数据读取或组装。双击模块，在常用页点击配置字段，填写名称、JSON 路径和类型；确认只写入模块草稿，仍须应用。最多 32 字段。",
                    "路径用 / 分层，例如 /result/score；读取数组项用 /values/0，索引从 0 开始，不允许前导零。路径区分大小写，键中的 / 写 ~1，~ 写 ~0。读取根值时路径留空；不是 JSONPath 表达式，不支持通配符。",
                    "读取模块可在本地报文折叠区输入 JSON，也可在输入页订阅 Json 文本；连接后绝不因上游失效而改用本地值。字段必须真实存在且类型一致，null、数字字符串与混合数组不会自动转换。",
                    "组装模块在输入页逐项订阅真实值；路径各层创建对象，/items/0 在组装时表示对象键 0，不自动创建数组。数组通过完整 Number[] 或 String[] 字段输入。重复路径或父子覆盖被拒绝，字符串自动转义，生成后不自动外发。",
                    "结果默认看关键字段与数量；更多中查看全部输出、数组明细或完整 JSON。字段名称可以修改而不破坏连接；已连接的字段须先解除引用，再删除或改类型。保存、复制和撤销沿用原方案操作。",
                    "示例程序 → 数据 · 结构化报文与测量结果（无需图像），包含产品文本、布尔、尺寸数组和标签数组，统计均值后组装报文，再读取均值和首项标签；不自动运行或发送。"
                });
                AddBullet("容量：JSON 最多 1048576 字符、32 层、65536 个值，单数组最多 10000 项；单文本及文本数组内容分别受 262144 字符限制。拒绝重复对象键、不完整 Unicode、非有限数字和超限内容。Number 采用平台双精度数值，不承诺任意精度整数。只有所有字段读取/组装成功才发布输出；本能力不是 JSON Schema 校验器、数据库或 MES。新增路径待集中验证。");
            }
            if (kind == VmToolKind.CSharpDllScript)
            {
                AddSubheading("脚本与 DLL 的数组输入输出");
                AddNumberedSteps(new[]
                {
                    "双击脚本模块，点击设计接口，添加输入、参数或输出，类型选择数值数组 Number[] 或文本数组 String[]；名称与成员稳定 ID 一起保存。应用成员草稿后再应用接口。",
                    "输入页可连接数组生成、灰度线剖面的 Samples 或前序脚本数组。必需输入必须连接，可选输入未连接时才使用默认值；已连接但来源异常不会回退。",
                    "数组默认值写 JSON，例如 [1,2,3] 或 [\"P1\",\"P2\"]。空数组写 []；可选输入留空表示不提供，使用前可调用 HasInput。JSON 不接受 null、混合类型或嵌套数组。",
                    "代码 / DLL 入口中可以按接口生成代码，确认后仅替换草稿；模板演示同类型输入透传，不是完整检测算法。补充处理逻辑后再试运行并应用。",
                    "用 GetInputNumberArray/GetInputStringArray 或 GetParameterNumberArray/GetParameterStringArray 读取副本，用 result.SetNumberArray/SetStringArray 返回数组。DLL 入口仍为 public static ScriptToolResult Execute(ScriptToolContext input)，引用当前宿主契约。",
                    "返回数组直接连接数组统计 / 取值模块；结果更多中可查看只读分页明细。未声明输出、类型错误或缺少必需输出会使本次发布失败，不留半组结果。"
                });
                AddBullet("每数组最多 10000 项，文本数组总内容最多 262144 字符；默认值文本最多 4096 字符。读取与写入均复制；修改脚本取到的 double[]/string[] 不影响上游，写入后继续修改本地数组也不影响已设置输出。空数组允许传递，统计或取值仍按各工具契约处理。数值必须有限，文本项不能为 null。", Color.FromRgb(71, 85, 105));
                AddBullet("学习：示例程序 → 脚本 · 数组逐项补偿（无需图像）。两个数组输入按位置配对，脚本给各测量值加 Offset 后返回，数组统计求均值并判定，文本取值显示首个标签。输入长度不一致或为空明确报错；改 Offset 后重新运行可学习参数传入。示例不自动运行，不连接设备或外发。", Color.FromRgb(71, 85, 105));
                AddBullet("旧固定标量端口、标量 SDK 方法与入口签名保留；新的数组 DLL 需要引用含数组接口的宿主契约。旧二进制兼容仍待集中验证，不支持跨版本契约任意混用。受信任代码可以产生外部副作用，AppDomain 不是安全沙箱。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.NumericExpression)
            {
                AddSubheading("用一条公式代替多个简单运算模块");
                AddNumberedSteps(new[]
                {
                    "从逻辑/计算分类添加数值表达式，双击后在常用页填写公式，例如 (A+B)/2。插入菜单提供均值、偏差、补偿、二维距离及函数示例。",
                    "根据公式下方提示，在输入页把 A/B/C/D 连接真实 Number 输出。实际使用的变量标记必需，未使用变量无需连接；常量公式可以独立运行。",
                    "F5 使用当前草稿公式试运行；查看 Number 和参与变量数，再把 Number 接到数值判定、文本组装或通讯。未应用草稿不改变正式参数。",
                    "应用并保存方案。学习入口为示例程序 → 计算 · 测量偏差与数值公式（无需图像），观察多点测量均值的百分比偏差和尺寸极差。示例不自动运行或外发。"
                });
                AddBullet("变量/函数不区分大小写，常数 PI/E；支持 + - * / % ^ 和括号。幂右结合且优先于一元负号：-2^2 为 -4，2^3^2 为 512。% 为带被除数符号的余数，不是百分号；百分比请写 100*(A-B)/abs(B)。使用英文小数点与逗号。", Color.FromRgb(71, 85, 105));
                AddBullet("函数 abs/sqrt/sin/cos/tan/log/exp/floor/ceil 接收 1 项；min/max/pow/round 接收 2 项；clamp(value,lower,upper) 接收 3 项。三角函数用弧度，log 为自然对数；round 小数位为 0–15 整数、中点远离零。", Color.FromRgb(71, 85, 105));
                AddBullet("公式最多 1024 字符、256 节点、32 层解析嵌套。缺变量、除零、函数定义域错误、溢出或未知名称明确失败；不把无效值当 0，不读取上一周期结果。不执行 C#、文件或网络；不支持布尔/数组表达式和任意变量名。", Color.FromRgb(71, 85, 105));
            }
            if (VmArrayContractS105.IsTool(kind))
            {
                AddSubheading("数组搭建与取值");
                AddNumberedSteps(new[]
                {
                    "在逻辑/计算分类添加数值数组生成或文本分割为数组；常用页填写本地文本和字面分隔符。已有上游文本时，在输入页连接 String，运行只使用该来源，失败不回退。",
                    "把 Array 连接数值数组统计 / 取值或文本数组取值。索引从 0 开始，0 是第 1 项；默认只需决定这个索引。",
                    "数值模块的 Mean、Min、Max、Sum、SelectedValue 可接数值判定；文本模块的 Text 可接文本组装、文本解析或通讯。灰度线剖面的 Samples 可直接传入数值数组统计，不需要把测量值转为文本。",
                    "在结果页先看数量和短摘要；需要每项原始值时展开更多，再点击数组明细，每页 100 项。查看不会排序、过滤或修改输入。",
                    "应用参数并保存方案。Number[] / String[] 可经过分支选择和子流程接口独立复制，也可传入 C# / DLL 的命名数组接口；全局变量数组接口尚未扩展。"
                });
                AddBullet("最多 10000 项，输入文本最多 262144 字符。整段空文本产生空数组；连续分隔符保留空字段，数值数组拒绝空字段、非数值和非有限数。文本保留空白与空项。统计/取值拒绝空数组、索引越界和求和溢出，不静默跳过或截断。", Color.FromRgb(71, 85, 105));
                AddBullet("学习入口：示例程序 → 数据 · 数组统计与报文组装（无需图像）。流程把 5 个尺寸值传入子流程求均值，接范围判定，再从文本数组选择工位字段组装报文。示例不自动运行、不连接设备、不发送结果。修改第 01 步文本后运行流程，观察均值与判定变化；这些是学习步骤，不是已验证结论。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.ShapeMatch)
            {
                AddSubheading("在模块内制作模板");
                AddNumberedSteps(new[]
                {
                    "把图像源的 Image 直接连接本模块的 Image，不需要独立模板模块。",
                    "双击打开参数小窗，在制作模板区域点击读取输入图像；其他处理类上游须先运行。",
                    "在窗内图像拖动框选目标，点击制作模板。框选仅是训练范围，不会限制搜索。",
                    "默认全图搜索；需要限制范围时再到 ROI 页配置。按 F5 只试运行本模块。",
                    "查看分数、位姿和轮廓，必要时把 Score 接到数值判定，最后保存方案与模型资源。"
                });
                AddBullet("更换输入图像后请重新读取并框选；最小分数和最大数量在常用页，其余搜索参数按需展开。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.OcrRecognition)
            {
                AddSubheading("逐字符识别结果与文本规则");
                AddNumberedSteps(new[]
                {
                    "从识别分类添加 OCR 字符识别，连接 Image。ROI 页圈住需要读取的字行或标签，选择黑字白底/白字黑底、分割灰度、字符面积和顺序；也可订阅同图像链前序 Characters Region，保留来源或按行/列排序。",
                    "双击统一参数小窗，从原字库入口导入 HALCON MLP .omc，或点击固定当前字库，将旧路径复制为受管 SHA 版本草稿。F5 可试运行，确认后应用；不会覆盖原字库。旧方案路径继续兼容，但未固定 SHA 不具备已保存版本防篡改边界。",
                    "按 F5 查看真实文本、字符数和最低/平均置信度。展开结果页单一更多，逐字表显示原候选编号、字符标签、真实分数、低信/拒识和中心；勾选只看低信/拒识，单击字符在当前输入图像显示框与编号，只查看不改识别文字或判定。",
                    "默认不校验文本，仍保留最少字符数、最低置信度和拒识判定。需要固定产品字样时展开文本校验，选精确期望文本并填自己确认的完整 Text；不自动去空格、替换错字或补缺字。可明确选择忽略大小写。",
                    "需要序号/日期格式时选完整格式匹配，填写 .NET 正则，例如 [A-Z]{2}[0-9]{6}；规则匹配整段原文，不是找到一个子串就通过。输入文本最多 10000 字符、表达式最多 1024 字符、匹配最多 100 ms，超时按 NG 并给出原因。规则匹配不能掩盖零字符、低信或拒识。",
                    "CharacterTexts 和 CharacterConfidences 是对应真实候选的 String[]/Number[]，可接文本数组取值与数值数组统计；CharactersRegion 保留同序区域，Centers/XLD 供图像与几何下游。查看 TextRuleMatch 和 ValidationFailureReason，并结合 ResultCode，不把不校验时的规则 true 当产品合格。",
                    "应用并保存；迁移电脑使用打包导出，受管字库路径和 SHA 随方案复制。示例程序→识别·OCR 逐字结果与校验提供图像→OCR→置信度数组统计/首字符取值；默认规则关闭，需自己依据本次真实结果填写期望，装载不自动执行或外发。"
                });
                AddBullet("逐字分数来自 HALCON MLP 对同序字符 Region 的真实输出，不是把整段文本拆字再分配平均分。置信度不是正确率；字库、成像、分割及字符顺序都会影响结果，示例阈值不能代表现场可靠性。零字符保持 NG，下游空数组统计或取值明确失败，不补旧字符。", Color.FromRgb(71, 85, 105));
                AddBullet("受管字库最大 512 MiB、类别数 1–10000，读取/识别期间保留文件只读锁并核对 SHA；内容变化须显式重新导入。模型句柄每次独立读取和释放，草稿、复制与撤销不共享可变模型。首版不提供字体自训练、自动文本行检测或深度 OCR；新增路径全部待集中验证。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.PoseImageAlignment)
            {
                AddSubheading("固定参考姿态与真正图像对齐");
                AddNumberedSteps(new[]
                {
                    "从图像处理分类添加定位图像对齐。Image 接原图像来源，Pose 可接同图像坐标中 NCC 的正式最佳 Pose，或模板明确选取的 SelectedPose；不要把框图形、分数或查看行当位姿。新模板及选取参数须先应用并重新运行，未应用草稿或旧输入的 SelectedPose 不能消费。",
                    "双击统一参数小窗，点击采集当前参考，把当前产品位姿和输出参考尺寸保存到草稿；不会在每次运行时自动更新，也不会随查看另一个匹配候选漂移。确认参考后应用，F5 可沿用当前草稿试运行。",
                    "默认按参考图像尺寸输出；常用只需选择输出尺寸模式，其他插值和参考数值按需展开。先查看参考状态，再按 F5 观察模块输出图像，或双画面比较输入与真正对齐图；不会压窄流程画布。",
                    "以后换测试图像，先重新运行图像来源与定位，再运行对齐。产品当前平移/旋转被变换回固定参考，输出为新的受管 Image；连接灰度、分割、测量或良品变化检测可以继续使用，不是只移动 ROI 或显示示意线。",
                    "在对齐图中建立新的检测 ROI。输出具有独立参考版本和坐标身份；源图 ROI 不会自动冒充已经对齐，不能静默跨链继承。需要重新采集参考时必须明确操作，并重新检查下游 ROI/良品模型，不把旧配置自动重新解释到新参考。",
                    "应用并保存方案。学习入口为示例程序→图像·定位后对齐，提供原图→NCC→对齐→灰度统计的真实连接；必须自己制作 NCC 并运行，再采集参考，示例不自动训练、采集参考或运行。"
                });
                AddBullet("首版只做 2D 刚体平移/旋转，比例固定为 1，不提供尺度、透视、非刚性或亮度归一化。无匹配/无位姿、Image 与 Pose 不同坐标来源、世界坐标、无效参考和空输出有效域明确失败，不回退旧对齐图。", Color.FromRgb(71, 85, 105));
                AddBullet("输入限单张、1 或 3 通道同类型 byte/int2/uint2/real 图像，最多 16777216 像素；不自动转型、缩放灰度或选择多图。参考像素位置须在参考画面内，当前像素位置须在当前输入内。输出尺寸只选已保存参考图或当前输入图，不通过尺寸模式暗中缩放产品。", Color.FromRgb(71, 85, 105));
                AddBullet("有效域按真实源域和插值边界变换，默认双线性采取保守 3×3 邻域保护；即使身份变换也可能减少 1 像素边缘，不等于全画布都有有效像素。黑色显示背景不代表可检测区域，后续检测应使用输出 Image 的 domain。", Color.FromRgb(71, 85, 105));
                AddBullet("对显式使用 Region 的统计/测量模块，把对齐输出 Region 接到其 ROI 输入；学习示例的灰度统计同时订阅对齐 Image 与 Region，避免将画布黑边计入统计。源图 ROI 不会自动变成对齐图 ROI。", Color.FromRgb(71, 85, 105));
                AddBullet("NCC 输出为真实像素中心 Pose。本消费者对旧 Shape 的原始 HALCON Pose2D 做显式坐标转换，并解析其真实输入来源；不修改旧 Shape 算法或标量输出，也不把旧结果宣称为已通过同精度认证。示例只使用 NCC，全部新增代码仍待集中验证。", Color.FromRgb(71, 85, 105));
                AddBullet("订阅模板 SelectedPose 时还要核对真实 SelectedScale：本工具只支持单位尺度 1 的刚体平移/旋转，非单位尺度明确拒绝，不会把 Shape 的缩放忽略后冒充完整对齐。NCC 没有尺度搜索，其 Scales 为 1；Shape 的角度/坐标 Pose 与 Scales 是分开的输出。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.NccTemplateMatch)
            {
                AddSubheading("NCC 灰度模板制作与定位");
                AddNumberedSteps(new[]
                {
                    "从定位分类添加 NCC 灰度模板匹配，直接连接图像源或前序图像工具的 Image。模块可直接读取已连接图像源，其他处理类上游须先运行；RGB 须先在上游显式转为单通道灰度。",
                    "双击打开统一参数小窗，在制作模板区读取当前模块实际输入图像，然后在窗内拖动框选有灰度纹理的目标。输入绑定草稿改动须先应用，不会在取图时偷偷提交；训练框只用于制作模板，不等于搜索 ROI，不要只框无纹理的均匀背景。",
                    "点击制作模板，新版本只进入当前草稿；失败不覆盖已应用模板。常用调整最小分数和最大匹配数，旋转范围、重叠、极性、层数等进入更多参数。无需先应用搜索参数才能训练或 F5 试运行。",
                    "默认全图搜索。需要限制检测范围时才到 ROI 页配置搜索区及屏蔽。按 F5 仅试运行当前模块；在结果页查看本次数量、分数和图形，展开候选后选择定位到原图像。候选选择只是查看，不会悄悄替换正式下游使用的最佳位姿。",
                    "Score 接数值判定用于定位可靠性检查；正式 Point/Pose 可供同坐标的下游使用。无匹配时 MatchCount=0、HasMatch=false、ResultCode=NG，Score=0 只是无匹配哨兵而非实际相关分数；不发布几何，也不借用旧候选。找到目标并执行 OK 不等于产品合格，仍由下游判定阈值决定。",
                    "应用并保存方案；需要迁移图片和模型资源时使用打包导出。示例程序→定位·NCC 灰度模板提供 Image→NCC→分数判定的真实连接，但必须自己读取、框选制作模板，不自动训练或运行。"
                });
                AddBullet("NCC 使用灰度相关性，和已有 Shape 轮廓匹配是不同算法。首版不提供缩放 NCC，不把训练框当作默认搜索限制，也不会把位置修正 ROI 冒充图像已经对齐。", Color.FromRgb(71, 85, 105));
                AddBullet("搜索 ROI/屏蔽限定模板参考点可以出现的位置，不保证整个模板足迹都在 ROI 内；模板可能跨范围边界。这和阈值分割把检测像素逐个排除的语义不同。单张单通道 byte/uint2 图像最多 16777216 像素，训练矩形须完整位于真实有效域，包含 8–1048576 个有灰度变化的像素；最多 256 个候选。", Color.FromRgb(71, 85, 105));
                AddBullet("改极性或建模层数必须重新制作模板；搜索旋转范围不能超出实际模型已训练范围。默认角度为 −180° 起、范围 360°，层数 0 自动；这些是模块设置，不是旋转定位准确性承诺。搜索超时为 10 秒，超时属于执行错误，不能当作零匹配。", Color.FromRgb(71, 85, 105));
                AddBullet("模板参考点为训练矩形的几何中心，参考角度为 0°；输出 Point/Pose 是该参考点在当前输入图像中的像素坐标，不是识别对象区域的质心，和 HALCON 原始候选坐标有显式转换。模板可以用于其他图像，训练来源只保留历史身份，复制不会把它冒充新来源；当前 Image 与搜索 ROI 仍须同坐标。模型、通道或输入失效明确报错，不回退旧位置。", Color.FromRgb(71, 85, 105));
                AddBullet("新版本资源使用 .ncm 和 SHA-256；保存/复制/撤销保留不可变文件引用，每次训练/搜索释放独立句柄，不共享可变模型。旧文件保留供撤销/恢复，尚无引用感知自动清理。当前代码待集中验证，不是现场定位精度保证。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.ShapeMatch || kind == VmToolKind.NccTemplateMatch)
            {
                AddSubheading("多个目标：查看与正式选取");
                AddNumberedSteps(new[]
                {
                    "沿用本模块的读取输入、框选及制作/加载模板流程，明确连接正式 Image。双击原参数小窗，常用仍是搜索任务；展开同一个更多参数，在多目标排序与选取中选择分数高到低、行从上到下或列从左到右，填写从 0 开始的选取编号。",
                    "F5 只试运行当前草稿。点击更多参数内的查看本次多目标，按需打开只读冻结明细；选行只在同一当前图像上高亮，绿色标记正式选取、紫色标记查看，均不是产品合格判定。原候选查看同样不会修改正式输出。",
                    "想把查看行用于正式选取时，必须点击将查看目标填入草稿；它同时填写冻结时的排序和编号，不自动应用或运行。回原小窗 F5 观察，确认后应用并重新运行，再保存方案。锁定、运行中、模块/源图/输出改变时拒绝写草稿；旧明细可只读，不在新图上借用旧目标。",
                    "Centers 是同链像素参考点 Point2D[]；Scores、Angles、Scales、SourceIndices 是同长度、同顺序的 Number[]。同一个索引对应同一个本次真实匹配：分数 0–1、角度 °、尺度无单位。SourceIndices 是本次 HALCON 原序编号，不是排序后的查看行号，也不是跨帧身份。",
                    "SelectedIndex 从 0 开始，0 表示排序后第一个目标；它不是 SourceIndices。HasSelection=true 才有 SelectedPoint、SelectedPose、SelectedScore、SelectedScale。无匹配产生空数组；编号超出本次数量时明确缺选，不选末项、不补零点、不借旧结果。原最佳 Point/Pose/Score 继续兼容，不随数组排序、明细点击或明确选取而改变。",
                    "示例程序→定位·模板多目标与明确选取提供图像→NCC→参考点分析/同序分数统计/选中分数判定的可编辑连接；先自己读取、框选、制作并应用模板，再运行。示例不自动训练或运行，也不预造多个目标或合格结果，学习分数不是现场产品标准。"
                });
                AddBullet("这里的中心是模型模板参考点在当前图像中的真实像素位置，不是检测对象区域的几何质心。SelectedPose 仅表达刚体坐标与角度，Shape 的真实缩放仍在 Scales/SelectedScale 中，不能把它冒充含缩放的完整变换；定位图像对齐消费 SelectedPose 时只允许单位尺度 1。", Color.FromRgb(71, 85, 105));
                AddBullet("NCC 请求最多 256 个候选，Shape 最多 1000 个；只读明细保留完整预算内结果，图上编号仅显示前 100 个，选其余行仍可定位。Shape 正式 XLD 另受全部候选累计 1000000 点预算约束，超出明确失败，不只画前 100 个轮廓冒完整输出。候选数由搜索参数、阈值、重叠等决定，不能宣称找到画面中所有目标，也不是目标跟踪器。", Color.FromRgb(71, 85, 105));
                AddBullet("旧 Shape 仍可兼容其原最佳结果；但没有正式 Image 订阅，或模型未明确归属当前模块时，不发布新的正式数组/选取，并显示原因。这不等于本次零目标。输入、模型或坐标失效也不从 Candidates 摘要补造数组、自动换源或回退全局旧图。", Color.FromRgb(71, 85, 105));
                AddBullet("SelectedPose 下游消费核对已应用定义、模型版本及当前源图身份。未应用的模板/选取草稿、源图更新或模型换代都会拒绝旧位姿；请先应用并重新运行，不把 F5 的临时草稿结果直接当生产依据。新增路径部分实现、待集中验证，不是定位精度或现场通过承诺。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.VariationSurfaceInspection)
            {
                AddSubheading("多良品学习与表面缺陷");
                AddNumberedSteps(new[]
                {
                    "从检测/分割分类添加良品变化缺陷检测，连接已对齐的单通道 Image。先运行上游，再双击模块；常用只设置最小灰差、波动倍数和最小缺陷面积。",
                    "展开良品样本与训练。每次取得不同良品后点击加入当前输入良品；实际订阅图像及有效域会保存为受管资源。也可确认外部图片均为良品且已按当前输入坐标对齐后，多选加入对齐图片。",
                    "至少 3 张、最多 64 张。点击学习良品，查看逐张尺寸/类型/哈希失败原因与训练进度。成功生成独立草稿模型，必须点击应用才成为正式配置；F5 可在原草稿路径试运行。",
                    "ROI 页选择本地、继承或合并及屏蔽。有效检测区必须完全落在所有良品共同有效域内，不会偷偷缩小 ROI。按 F5 查看缺陷 Region、筛选后面积/比例/数量和学习样本数。",
                    "Area/AreaRatio/RegionCount 接数值判定，Region 可接区域筛选、轮廓或测量。空缺陷是有效执行 OK，是否产品合格由判定阈值决定；RawArea 保留面积筛选前的差异。",
                    "应用并保存方案。普通保存保留受管资源路径；迁移到别的电脑请用文件→导出工程副本，模型、共同有效域和良品样本随 JSON 一起复制。示例程序→检测·良品学习与表面缺陷提供已连接流程，但不会自动训练、运行或伪造良品集。"
                });
                AddBullet("首版使用 HALCON standard 变化模型，学习同一位置的良品灰度均值与波动。仅单通道 byte/int2/uint2，同尺寸；RGB 须上游显式转灰度。不会自动定位/旋转对齐或缩放灰度。单张最多 16777216 像素，总样本最多 268435456 像素，资源文件最多 512 MB。", Color.FromRgb(71, 85, 105));
                AddBullet("来源、相机标识、上游处理/ROI 几何改变会使旧模型拒绝检测。改当前模块检测 ROI 只需保持在训练有效域内；调整三个容差字段无需重新学习。样本增删需重建模型；失败/取消不覆盖原已应用版本。取消在当前 HALCON 算子完成后受理，关闭、切换和停止会取消并拒绝过期结果。", Color.FromRgb(71, 85, 105));
                AddBullet("复制后的模块独立读取不可变模型文件，不共享可变句柄；明确复制了根流程图像来源时可重映射身份，未解析的嵌套来源仍须重新采样，不把旧模型自动绑定到任意图像。旧版本文件保留供撤销/恢复引用，尚无自动清理。当前全部待集中验证，不是现场缺陷精度保证。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.ColorInspectionHsv)
            {
                AddSubheading("颜色选样与覆盖比例");
                AddNumberedSteps(new[]
                {
                    "图像源的 RGB Image 直接连接 HSV 颜色检测；先运行上游。双击模块，常用页点击图像取色，再在模块输入画面单击有色点。Esc 取消；取样只改当前草稿，不自动应用或运行。",
                    "常用页设置目标色相、色相容差、饱和度下限和亮度下限；上限进入更多参数。色相用角度，饱和度/亮度用百分比。红色跨 0° 的范围自动环绕，不需要拆两个模块。",
                    "需要局部检测时在 ROI 页选择本地、继承或合并，并配置屏蔽区。修改输入/ROI 来源后先应用再取色，图像变更会结束取色状态。",
                    "按 F5 试运行本模块；查看颜色 Region 掩膜、覆盖比例和区域数。AreaRatio 接数值判定，用于颜色有无或覆盖不足；Region 接区域特征筛选，按面积排除小色块。",
                    "零匹配是有效结果：覆盖 0%，HasMatch=false；执行 OK 不代表产品合格，须设置下游数值判定。应用参数并保存方案。学习入口为示例程序 → 检测 · 颜色取样与覆盖比例。"
                });
                AddBullet("仅支持 3 通道 RGB：byte 按 255、uint2 按 65535、real 要求有效域为 0–1；不逐图拉伸。比例分母为图像 domain 与有效 ROI/屏蔽的交集像素数，不是整图或外接框。空有效域、无效幅值和错误坐标来源明确失败，不复用旧输出。", Color.FromRgb(71, 85, 105));
                AddBullet("低饱和或太暗的点不适合直接取色；黑/白/灰检测可手填色相容差 180°（不限色相），限制饱和度和亮度。取样色块只供预览，不写入方案。示例阈值只用于学习，不是产线验收结果；当前代码待集中验证。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.GrayHistogram)
            {
                AddSubheading("灰度分布与分位数");
                AddNumberedSteps(new[]
                {
                    "连接图像源的 Image；需要局部统计时在 ROI 页选择本地、继承或合并，并配置屏蔽区。",
                    "常用页只设置自选分位百分比，默认 95。固定 P10、P50 和 P90 不受此参数影响。",
                    "试运行后在结果页展开灰度分布；有效检测范围由图像结果图层的 Region 显示。",
                    "把平均值、标准差或分位数等 Number 输出连接数值判定，用于曝光、亮度波动或背景分离判断。"
                });
                AddBullet("分位数采用最近秩：P0 为最小值，P100 为最大值。相同峰值频数时取较小灰度。图表合并柱只影响显示，不影响数值结果。", Color.FromRgb(71, 85, 105));
                AddBullet("支持 byte/int1/int2/uint2 单通道和可转亮度的 RGB；不自动把 real/int4 压缩成 8 位。空有效域、已释放输入或非法百分比会报错，不保留旧分布。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.GrayLineProfile)
            {
                AddSubheading("沿线灰度采样");
                AddNumberedSteps(new[]
                {
                    "连接 Image 并运行上游。双击灰度线剖面模块，在常用页点击图像取线，在输入图像空白处拖出采样线；双画面也可在左侧输入图像操作。",
                    "绿色为起点、橙色为终点，箭头表示采样方向。拖端点微调、拖线段整体平移；边界处保持线长。坐标框支持小数像素精调。",
                    "点击反向交换起终点；完成取线保留草稿，Esc 撤回当前拖动并退出取线。上述操作不自动应用或保存。",
                    "更多参数设置 2–8192 个采样点，包含首尾并等距分布；固定采用双线性插值。",
                    "试运行后展开结果页的沿线曲线。横轴为从起点出发的距离，图像 XLD 图层显示实际采样线。",
                    "把 Mean、Deviation、Range 或极值位置等输出接入数值判定或计算，应用参数并保存方案。"
                });
                AddBullet("所有采样点的插值邻域必须处于输入有效域，穿过无效区域会拒绝整次运行。反向线按反向顺序取样；极值相同时取沿线最先的位置。", Color.FromRgb(71, 85, 105));
                AddBullet("切换图像来源会结束取线；上游快照失效时先重跑上游，再重新取线。不会在全局回退图像上继续写入订阅模块的坐标。反向后需重新试运行，旧曲线不会伪装成新方向结果。", Color.FromRgb(71, 85, 105));
            }
            if (kind == VmToolKind.BranchValueSelection)
            {
                AddSubheading("真 / 假分支汇合");
                AddBullet("结果类型按数值文本、图像图形、几何点集分组。点、点集、位置姿态、直线、圆和椭圆使用正式类型传递；只复制选中路径，保留原坐标身份，不解析字符串坐标。点集的空集合与无效项保留给下游异常点策略处理，混合坐标来源则拒绝。");
                AddNumberedSteps(new[] { "在常用页选择结果类型，例如图像；两条路径必须提供同一类型。", "在输入页连接选择条件（Boolean），再连接真路径值与假路径值。", "运行上游条件及相应路径，然后试运行当前模块；只读取被条件选中的路径。", "把 Value 输出接到后续模块；在结果页查看选中分支和来源状态。" });
                AddBullet("顶部示例程序中的真 / 假条件分支包含图像汇合与后续亮度分析。切换判定范围可学习两条路径；示例不会自动运行。");
                AddBullet("选中路径无有效结果时明确报错，不读取跳过路径的旧值，也不自动换另一条路径。未选中路径可以跳过。选择成功不抹除前序模块的 NG 判定。");
            }
            if (kind == VmToolKind.WorldPointPairMeasurement)
                AddBullet("直接订阅平面标定的世界点时，未应用标定草稿的 F5 输出不能作为正式距离输入。请应用标定后重新运行来源；定义、绑定或输出已失效也须重跑。标定模块自身的草稿 F5 保留，这不是全部转发或跨子流程世界点的完整审计。");
            if (kind == VmToolKind.TcpClientExchange || kind == VmToolKind.ModbusTcpExchange || kind == VmToolKind.ModbusRtuExchange)
            {
                AddSubheading("离线样本队列");
                AddNumberedSteps(new[]
                {
                    "双击本模块，进入更多中的原离线响应样本任务；没有队列的旧工程仍使用原单响应。",
                    "编辑当前响应，从管理菜单选择将当前响应加入队列。用0起编号下拉框明确选取，再在原编辑框修改响应；名称可就地修改。",
                    "删除所选后必须明确选择另一项，不自动夹紧编号；删除最后一项或返回单样本会保留当前响应文本。",
                    "确认原离线模拟已勾选，再主动F5。样本选择不会自动开启模拟；未勾选时F5仍是原真实通讯路径。",
                    "使用正常、空或坏帧学习成功与失败。结果和日志标记当前选中样本；坏帧使用原异常路径，不回退上一项或旧值。独立试解析只给预览，不写正式流程输出。",
                    "确认后应用并保存工程。样本及选取随参数保存/复制；每次由你选择和运行，不自动轮转、重试、连接、外发或写生产周期。"
                });
                AddBullet("1–16项，名称最多64字符，名称与响应总UTF-8最多64KiB；TCP每项最多65536字符，Modbus帧最多2048字符。超预算拒绝整组，不截断。离线解析不能证明网络、串口、PLC写入或现场握手通过。");
            }
            if (kind == VmToolKind.SubflowCall)
            {
                AddSubheading("子流程调用");
                AddNumberedSteps(new[]
                {
                    "双击子流程调用模块，在常用页选单次调用、固定次数或数组逐项。固定次数填 0–1000；逐项选一个已经定义的 Number[] 或 String[] 调用输入，并在输入页连接上游真实数组。",
                    "进入内部编辑时，输入代理额外提供当前项索引、总项数；逐数组项时还提供当前项 Number 或 String。内部模块从这些端口订阅并按原有顺序执行。预览只显示第 1 项，不代表全部项已运行。",
                    "从内部接口的返回页选择 Number 或 String 返回值，可勾选“同时收集每次返回为数组”。普通返回仍表示最后一次，新增 $Items 端口给下游统计、变量、脚本或报文使用；改变/删除已被订阅的返回前先解除引用。",
                    "运行时当前项、总数、内部模块和执行记录可见；零次或空数组不执行内部模块，HasLast 为 False、收集数组为空，也不发布普通返回。超时、停止或异常不会发布半组返回值。",
                    "学习入口是示例程序 → 流程 · 子流程逐项补偿与固定编号（无需图像）。运行后查看补偿结果数组、编号数组、下游统计与按需步骤记录；示例不会自动运行或外发。"
                });
                AddBullet("每次调用最多 1000 项，嵌套最多 8 层，单次根调用累计最多 10000 步。遵守原流程超时、停止和遇 NG 策略；不能中断一个正在执行的 HALCON 算子。已执行的变量写入、通讯等副作用不会自动回滚。新增循环路径待集中验证。");
                AddBullet("几何点集同样可以作为子流程输入和返回：Point2D、Point2D[]、Pose2D、Line2D、Circle2D、Ellipse2D。使用独立克隆，不共享可变点列表；当前几何复用只接受已有 ImagePixel 坐标，不自动做标定或跨相机转换。输入 / 返回中的类型选择按三组排列，点集摘要显示数量、有效/异常项和坐标来源。");
                AddBullet("一个方案可以包含多条流程：在流程区名称下拉框切换，旁边“⋯”可新建空白流程、重命名、复制或删除。保存方案会包含全部流程和内部调用，不只是当前画布；运行按钮只运行当前流程，不代表全部流程并行运行。切换前请停止运行并处理未应用参数，内部编辑会先返回上级。切换会清空旧图像和运行结果，请重新载入本流程图像或运行其图像源。");
                AddBullet("多图像源：在图像区“画面来源”下拉框选择具体图像源，状态会显示尺寸、待运行或异常。选定来源没有有效输出时画面保持空白，不会拿全局图像或另一图像源代替。双击图像源可选本地图片、目录逐帧回放或 HALCON 相机；相机必须显式连接，方案打开不自动连接。回放每次运行取下一张，末尾循环；折叠的模拟设置可复现缺图和过快请求。相机可选自由采集或软件触发，连接时检查设备支持；每次“采一帧”只运行当前源，运行流程才驱动下游，失败不回退旧图。采集诊断和图像区的源状态显示上次帧时间、耗时、帧号与错误，异常可直达源配置。Image、来源、逻辑 SN、UTC 时间、帧号、耗时及触发模式进入原输出端口；逻辑 SN 不是厂商硬件 SN。新绘制 ROI 记录图像坐标来源，跨源 ROI 在绑定页标为不兼容；结果图层只叠在同一图像链。学习入口：示例程序 → 采集 · 离线逐帧回放、图像 · 双来源独立检测。相机硬件待集中验证，不是多相机同步采集。");
                AddBullet("设备资源：右侧“通讯”工作区按需展开“相机资源与占用”，可查看当前方案根流程和子流程的相机源、连接所有者、触发状态及上次错误，并直达原模块参数窗。同一 HALCON 接口与设备标识在本进程只允许一个模块连接；换流程或方案后不会自动重连。default 只是接口默认设备，不是硬件唯一序列号；此保护不能识别不同接口/别名是否指向同一物理相机，硬件行为仍待集中验证。");
                AddBullet("TCP 响应字段：双击 TCP 客户端流程工具，常用区配置主机、端口、发送模板和等待响应；I/O 页订阅模板变量。按需展开“响应字段映射”，选择按分隔符从 0 开始的字段索引或 JSON /路径，分别配置文本、数值、布尔值输出；空白项不会出现端口。运行后原始收发、映射值和错误可在结果页查看；缺字段、格式或类型不符本次 NG，不复用旧结果。“无设备试解析”用同一解析器验证粘贴样本，不连接网络或发布运行输出。编码/行尾在折叠传输格式中，超时在更多参数；这不是 Modbus 或其他工业协议。");
                AddBullet("双源成组检测：图像区“更多图像工具 → 图像队列 → 双源配对”按需展开，指定两个不同图像源与 A/B 图片目录，点击配对检查。同名（去扩展名）文件成为一组；缺图或重复键会拒绝运行。可先预览两源真实图片，再运行当前组或全部组。左画面默认 B、右画面 A，各自选择器仍可调整；组文件仅在本次执行中覆盖源模块文件，不改方案参数与连接。若流程包含双图差分/动态阈值，配对图尺寸、通道或位深不一致会拒绝该组。结果记录含组键、两路路径与逐工具输出；双源历史不冒充单图快照。学习入口：示例程序 → 图像 · 双源配对与绝对差分。它不是相机同步触发。");
                AddBullet("TCP 指令触发：进入通讯页，展开“收到指令后运行流程”，选择目标并填写短指令，例如 RUN；应用配置后先连接受信任设备，再点“启用触发”。对端发送 RUN 加 LF 或 CRLF 换行才会运行，区分大小写，拆包和多行合包按完整行处理。忙碌时指令直接丢弃并写通讯记录，不排队补跑；需要对端按现场节拍控制发送。结果回复沿用原自动发送设置，没有另加 ACK 协议。");
                AddBullet("触发配置随方案保存，但启用态不会保存。切换流程/方案、断线、通讯错误或点击停止后需重新启用；配置改动也会使后续触发停止。启用时切换到所选根流程，不会因当前进入子流程就改跑内部模块。此入口没有新增鉴权和加密，只能用于受信任设备与网络；不要将无认证端口暴露到公网。");
                AddBullet("ROI、DLL 和全局变量仍是方案共享资源，引用清单包含其他流程和内部模块，仍有引用时不能直接删除。若在其他流程更改共享资源，旧流程的撤销历史可能清空以避免覆盖这些修改。删除整条流程需要确认，目前不能用模块级撤销恢复；正式删除前可先另存方案副本。");
                AddBullet("生产页只显示本次打开方案中当前流程的最近根流程结果，统计按流程分别累计；切换流程会清空当前判定和旧外发缓存，重新打开方案会开始新的统计会话。完整结果页保留全部流程记录，并显示执行时的方案与流程，CSV/XLSX 同样包含归属字段。批次历史可见流程名称；趋势和样本对比限定同一流程，缺少流程标识的旧批次明确显示未知，不自动拼接到新流程。此统计不是持久生产历史或 SPC。");
                AddBullet("示例程序中的“数据 · 几何点集跨子流程复用（自带图像）”展示圆弧卡尺点集返回、第二个子流程传入点集拟合圆、Point2D 与 Circle2D 驱动点圆关系以及半径判定。图像由软件自行生成，点击后不自动运行。圆弧点集质心与拟合圆心含义不同，本例采用部分圆弧，不能把两者当成同一个位置。先运行主流程，再进入两个子流程查看输入与返回和调试记录。");
                AddBullet("点集输出可在“结果 → 更多 · 全部输出与明细 → 点集明细”中查看。每页 200 项，可只看异常点，原始索引不变；表格是打开时的只读快照，后续重跑不会替换它，也不会修改下游收到的点集。主/双图像中的几何参考底图只按唯一的实际坐标来源关联，来源未确定或失效时不会猜测使用全局图。");
                AddBullet("“数据 · 判定选择几何分支（自带图像）”展示按半径判定选择圆结果：通过时沿用原拟合圆，未通过时才执行 Geometric 备选拟合，再由同一个 Circle2D 输出继续点圆测量。可缩窄数值判定范围后重新运行，观察假分支；备选拟合不代表质量自动通过，原判定仍保留。装载示例不会自动运行。");
                AddBullet("从顶部示例程序选择“流程 · 子流程检测与返回（自带图像）”，无需相机或额外示例文件。主流程为自带图像→亮区检测子流程→返回区域内亮度→返回面积判定。首次先运行流程生成上游输入，再双击第 02 个模块，进入内部查看阈值分割与轮廓测量。示例不会自动执行，也不会覆盖已有方案。");
                AddBullet("在内部把提取亮区的阈值由 235 改为 250，应用后运行可观察无亮区的结果；改回 235，再返回上级运行。输入 / 返回页展示图像传入及区域、面积、轮廓、长度返回。也可在内部设置断点，再返回上级用 F5 或连续试运行，学习端口记录、继续/单步与结果叠加。最后通过另存为或资源随行导出保留方案。这里是学习步骤，不是已验证的运行结论。");
                AddNumberedSteps(new[] { "在主流程连续多选至少两个模块，通过编辑菜单选择所选模块创建子流程。", "预览内部步骤、输入和返回边界，命名后创建。原下游连接自动经过调用节点。", "双击调用节点，在输入页重选外部来源；常用页可以查看内部步骤。", "F5 只试运行当前子流程草稿；应用后可使用流程区运行当前或全流程。内部模块按原执行器依次运行。" });
                AddBullet("常用页的进入内部编辑使用原流程画布和参数窗。顶部显示当前位置，返回上级保留修改并恢复父流程视图。输入 / 返回入口分栏编辑接口，可改名、按类型新增、移除未使用项和重选返回来源，点击应用才生效；取消不改变接口。改名保持连接，已有接口类型固定，要换类型请新增接口后改接。保存始终保存完整方案，不会把内部流程另存成主流程。");
                AddBullet("新增输入后，内部模块从调用输入边界订阅；返回上级后在调用模块输入页连接真实来源。移除接口时会提示仍使用它的模块，须先改接或清除对应连接；至少保留一个返回值。接口修改参与内部撤销，返回上级合并为一次父流程撤销。图像、区域、轮廓携带真实输出端口的坐标来源；等待上游运行不代表已确认坐标兼容，不能跨相机或世界坐标直接复用 ROI。");
                AddBullet("内部输入边界使用父流程已有结果；无值时先返回父流程运行上游。内部手动运行属于调试，不发送生产结果；外部触发在内部编辑期间不会误运行内部流程。内部图像和 Shape 模板纳入资源随行导出。");
                AddBullet("参数窗的连续会重复执行当前子流程草稿，不运行父流程。运行设置选择轮间等待 100–2000 ms，每轮完成后才等待并开始下一轮；草稿、输入绑定及子流程停止/超时策略在开始时冻结。点击停止后等待内部执行退出再恢复编辑，正式参数不自动应用。断点暂停时原试运行按钮变为继续调用（F5），旁边按需出现单步（F10），只控制当前调用。原子 HALCON 算子执行中不能立即中断；超时也在可响应的步骤边界检查。");
                AddBullet("连续试运行沿用当前调试会话统计和数值稳定性窗口。正常 NG、条件跳过、超时和异常分开显示，用户停止不计执行异常；失败或中止不保留本轮成功返回值。记录不计生产统计，不自动发送汇总结果，但内部通信/脚本模块本身的执行效果仍遵循其配置。");
                AddBullet("ROI 图层和 DLL 资源属于完整方案共享资源，引用清单会显示主流程 / 子流程 / 模块路径。其他层级或模板训练仍引用的 ROI 不能直接删除，全部清空也会先检查；请先解除对应引用。DLL 删除同样检查内部脚本引用，替换资源或修改共享 ROI 后，相关调用和后续模块等待重跑。该保护不等于跨相机坐标转换或隔离的子流程资源库。");
                AddBullet("打开调试监视中的子流程调用，选择步骤后可展开输入 / 输出端口记录。输入显示开始时可用的来源值，不代表所有分支都读取；输出记录本次发布值。悬停查看来源和坐标，默认/缺失输入不猜测。每方向最多 64 个端口，文本摘要限 512 字。停止后点击定位模块，可进入对应内部流程并选中模块，再双击编辑；模块已删除时不会按名称猜测。定位不是恢复历史运行结果。");
                AddBullet("查看本次图像支持适应、1:1、滚轮缩放和平移；本次结果叠加开关仅显示与底图坐标一致的 Region/XLD 输出，输入参考底图和真实图形分别说明。来源未知、不一致或超出调试预算时明确不叠加，不编辑主图 ROI，也不改变流程参数。记录最多 256 项，图像最多 8 张、估算 64 MiB；叠加每记录最多 8 层、估算 8 MiB，总计估算 32 MiB，单层最多 256 对象。缓存不是生产历史，每轮新调用或切换上下文后清理。已有六类几何/点集边界与共享 ROI/DLL 引用保护；任意数组、隔离资源库及完整内部调试仍在开发。");
            }
            AddSubheading("常见问题");
            AddBullet("显示“待连接”时，先检查输入页的来源端口、数据类型和执行顺序。", Color.FromRgb(71, 85, 105));
            AddBullet("需要调整上游时，展开输入卡“详情 / 来源修复”，点击“打开来源模块配置”；处理后用参数窗上方“返回”回到原输入端口。往返都会保留未应用草稿确认，不会自动改绑或运行。", Color.FromRgb(71, 85, 105));
            AddBullet("原问题清单中的“配置模板 / 配置固定参考 / 配置标定 / 打开输入”会定位原参数窗的对应任务，只展开和聚焦。您仍需主动读取、制作、采参考或采点、F5并应用；缺口提示依据已应用定义，不能用草稿或上一帧结果冒准备完成。", Color.FromRgb(71, 85, 105));
            AddBullet("提示已失效时刷新清单：切换方案/流程、删除重建、撤销替换、改参数/订阅或授权变化后不会按同名误跳。未发现此类定义问题也不证明算法、设备或模型字节可用；预算超限是未知，不是准备通过。当前模块草稿F5仍沿原试运行路径。", Color.FromRgb(71, 85, 105));
            AddBullet("显示 NG 不等于软件异常；先查看 ResultCode、结果数值和步骤记录。", Color.FromRgb(71, 85, 105));
            AddBullet("图像、Region、XLD 等对象由平台管理生命周期，不要把历史图层当作新的算法输入。", Color.FromRgb(71, 85, 105));
        }

        private void RenderGuideTopic(string key)
        {
            if (key == "QuickStart")
            {
                AddParagraph("先完成一个简单流程，再逐步增加工具。启动是空白方案；示例必须主动选择，不会自动运行。", false, Color.FromRgb(51, 65, 85));
                AddNumberedSteps(new[] {
                    "添加：从左侧展开分类，双击图像源和所需工具；也可搜索名称或任务词。",
                    "连接：从上游小端点连到下游，兼容端口自动绑定；点卡片底部订阅栏可重新选择来源。",
                    "配置：双击模块打开小窗。常用 / 输入 / ROI / 结果四页快速切换，单击用于选择或拖动。",
                    "试运行：参数窗打开时F5试当前草稿；点击应用后才进入正式方案。Ctrl+Enter可应用。",
                    "看结果：图像区看本次图层，结果页看真实值与异常。卡片记录打开独立日志/通讯窗。",
                    "保存：保存方案保留已应用参数与订阅。关闭参数窗后F5运行当前流程；Shift+F5请求停止。" });
                AddSubheading("需要更多说明时");
                AddTopicLinkUI10("示例与完整学习项目", "LearningProjects");
                AddTopicLinkUI10("界面与参数操作", "Workspace");
                AddTopicLinkUI10("调试、结果与步骤记录", "Debug");
                return;
            }
            else if (key == "LearningProjects")
            {
                AddParagraph("最快学习方式：顶部“示例程序”按入门、检测定位、测量标定、数据扩展、流程工程分组，原示例仍保留；完整学习项目提供组合流程。主动装载后按流程从上到下逐个双击模块，不会自动运行。", true, Color.FromRgb(15, 118, 110));
                AddParagraph("推荐从“示例程序 → 入门 → 入门 · 亮度分布与沿线分析（自带图像）”开始：运行流程后看全图灰度分布，再打开水平线分析，拖动采样线离开亮区，重新试运行并比较曲线。示例的判定上下限仅用于教学，不是现场验收标准。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("三条完整学习路径");
                AddBullet("示例程序 → 完整学习项目 → 对象检测、条件判定与生产查看：源图、阈值与连通对象经真实面积数组/数量判定，再用 Passed 驱动条件执行，最后在本流程生产结果项看本次周期。需自己设置符合图像的阈值与学习门槛，主动切换运行界面并执行完整周期；分析成功不等于产品合格，证据默认关闭，不连接设备或启用触发。", Color.FromRgb(71, 85, 105));
                AddBullet("示例程序 → 完整学习项目 → 明确选取、参考对齐与边缘测量：NCC 的正式 SelectedPose 驱动固定参考对齐，对齐图进入搜索带找边与覆盖判定。项目不预造模板或固定参考；先在真实输入上制作并应用模板、重新运行并明确采参考，再编辑对齐图上的搜索带。原图与对齐图坐标独立，原 ROI 不会自动迁移。", Color.FromRgb(71, 85, 105));
                AddBullet("示例程序 → 完整学习项目 → 同链标定与世界距离：真实找边点进入点集拟合，像素端点经同链平面标定变成世界点，再计算世界距离并判定。项目不预造标定样本、比例或毫米结果；须采至少三个非退化的像素/世界对应点，填写真实单位并应用。边缘段拟合端点距离不是工件尺寸或现场精度认证。", Color.FromRgb(71, 85, 105));
                AddParagraph("从“示例程序 → 当前项目学习步骤…”按需打开一个可关闭的任务窗：默认看当前一步，主动打开已有模块或切换步骤，不缩窄流程。上一步/下一步仅导航，训练、采参考、标定、F5、应用、运行和保存仍须您明确操作。临时步号不是算法状态、真实启动准备、合格证明或验收；删除模块、切换方案或身份失效时不会按同名猜替代模块。缺图片、模板、参考或标定须按提示补齐，不补旧结果。", false, Color.FromRgb(51, 65, 85));
                AddParagraph("当前步骤按需显示第一条已知定义缺口及准确入口；例如尚无已应用NCC模板或固定参考时，按钮直达原制作/参考任务。提示不自动读取、训练或采参考；应用后再查看缺口，没有已知缺口也不等于实际输入/算法通过。条件后继尚未执行不被当成旧结果可用。", false, Color.FromRgb(51, 65, 85));
                AddParagraph("不知道工具名时，在模块箱搜索任务词，例如去噪、找圆、亮度或扫码；多个词以空格分隔。搜索只筛选目录，不会添加模块或修改流程；清空后回到原来的工具模式。", false, Color.FromRgb(51, 65, 85));
                AddParagraph("启动和新建均从空白方案开始，不会自动加入模板匹配或加载上次方案。已有方案请从文件或最近列表主动打开；恢复副本和示例程序也由您主动选择。", false, Color.FromRgb(51, 65, 85));
                AddParagraph("双流程学习：选择“示例程序 → 流程工程 → 方案 · 双流程检测与指令触发”。两条流程使用不同的自制图像，示例不连接设备、不自动运行，并关闭自动结果发送。", false, Color.FromRgb(51, 65, 85));
                AddNumberedSteps(new[] { "先运行“工位 1 · 亮度检测”，双击亮度/剖面模块查看输入和结果。", "在流程下拉框切换到“工位 2 · 圆弧测量”，运行后进入子流程查看点集、圆和返回值；旧图像会清空，不代表新流程已运行。", "在结果记录中观察两条流程的方案/流程归属；生产页只显示当前流程的会话统计和最近根流程结果。", "可复制一条流程后删除副本，再从“⋯ → 恢复已删除流程”恢复；最近 10 条恢复副本只保留到关闭或重新打开方案。", "保存或导出方案及图像后，再主动打开保存的文件，确认两条流程都在；所有流程都属于同一方案。", "通讯页已预填 RUN_CIRCLE 指向圆弧测量，但触发保持停止。仅在连接受信任设备并主动启用后，对端发送 RUN_CIRCLE 加换行才会运行；学习视觉流程无需启用通讯。" });
                AddNumberedSteps(new[] { "从空白方案开始，或主动选择示例程序。", "从左侧加入本地图像源，在参数中选择图片。", "加入处理工具并连接输入。", "双击模块，在参数小窗中完成常用参数和试运行。", "查看图像叠加、模块结果与步骤记录。", "应用参数、运行全流程并保存方案。" });
            }
            else if (key == "Workspace")
            {
                AddSubheading("开发界面"); AddParagraph("左侧是模块箱，中间是自上而下的流程，右上是图像，右下是结果和任务入口。单击模块选择或拖动位置；双击打开可移动参数小窗，不改变流程大小或缩放。单击其他节点不会切换窗内编辑对象，双击才切换。", false, Color.FromRgb(51, 65, 85));
                AddBullet("参数小窗默认紧凑显示，仍可拖动和调整大小。Ctrl+1 / 2 / 3 / 4 切换常用 / 输入 / ROI / 结果；F5只试运行正在编辑的模块草稿，不自动应用；Ctrl+Enter应用当前草稿，确认后再保存方案。应用会使旧运行结果失效，等待重新运行，不用旧数值冒充当前结果。");
                AddSubheading("运行界面"); AddParagraph("顶部“运行界面”只切换到操作员页面，不会自动启动检测。流程执行使用“运行流程”或运行界面的运行按钮。", false, Color.FromRgb(51, 65, 85));
                AddBullet("流程区“选中模块”只运行该节点的已应用参数，“当前流程”运行当前流程全部启用步骤，不运行其他流程。F5在参数窗打开时只控制编辑模块草稿；关闭后运行或继续当前流程，输入框焦点不会屏蔽它。Shift+F5请求停止任务并停用TCP触发；等待安全边界，不强行中断HALCON。已有任务执行时不能隐式切换运行范围；先明确停止并等待退出。");
                AddSubheading("按钮说明"); AddParagraph("高频按钮使用明确动词；低频命令收在“更多命令”。把鼠标停在按钮或参数组上，会显示浅色技术说明。", false, Color.FromRgb(51, 65, 85));
                AddBullet("模块卡“记录”打开独立只读步骤窗口，不遮住流程。取消跟随最新只冻结查看，不停止执行或采集；搜索过滤快照，复制/导出当前页文本。步骤历史只显示具有精确会话、流程、模块归属的最近200条记录，同名或改名不猜关联；旧纯文本仍在全局日志。通讯页标注真实离线模拟输出，查看不会连接或发送；导出是当前查看快照，不是完整历史。");
                AddBullet("结果页默认显示关键类型值，更多里查看全部端口。最近草稿试运行与模块执行明确区分，不代表整流程合格；应用、改输入或失效后旧值和耗时清空，等待重跑。执行异常单独显示，不把它当产品NG；类型或数值无效会明确提示，不能从日志摘要猜结果。未标注的执行来源不会冒称正式生产。");
                AddBullet("图像区的适应和1:1只改变查看比例；ROI菜单直接选择绘制形状。确认新ROI时以正在编辑的模块为准，普通单击其他节点不会改归属；已有草稿保留。切换到另一图像坐标、尺寸或编辑对象时，未确认绘制取消，已有ROI不会自动迁移。指定来源尚未运行或已移除时保持无图，不借用全局旧图。只在明确支持ROI的工具中绑定；普通阈值分割目前不支持本地ROI。");
            }
            else if (key == "Workflow")
            {
                AddSubheading("条件执行与真 / 假路径");
                AddParagraph("在模块参数窗的输入页展开条件执行，选择前序 Boolean 输出；默认真时运行，勾选假时运行可构成另一条路径。未连接条件时始终运行。条件不满足记录为 SKIP，不调用算法或脚本，也不产生本次输出；订阅该模块结果的下游会随之跳过。条件没有可用值会报错，不会当作假。", false, Color.FromRgb(51, 85, 65));
                AddParagraph("示例程序中的判定驱动真 / 假分支提供自带图像。修改数值判定上下限后重新运行，观察滤波与增强中哪一条执行。用 NG 判定进入假分支时需允许遇 NG 继续；总体结果仍保留真实 NG，不会因走过处理分支改成 OK。普通双输入工具不是分支汇合器，若订阅了被跳过路径也会跳过。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("多选批量编辑");
                AddParagraph("多选模块后，从编辑菜单进入批量命名，先看新旧名称预览，再应用。需要统一多个数值判定规则时，只选择数值判定模块，打开复用数值判定规则并选择一个来源；同步比较方式、上下限和容差，但每个模块的输入来源不变。两项操作都可一次撤销，修改后需重新运行。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("复用流程片段");
                AddParagraph("连续选择至少两个模块可保存为本机流程片段。再次插入时，内部连接按新模块身份保留；外部输入可接受自动选择、指定前序输出或不指定。选择不指定仍遵守模块原有默认输入语义，例如系统图像。空白方案也可直接插入片段；插入不会自动运行，一次撤销可整体回退。待连接项从问题清单打开输入页继续配置。", false, Color.FromRgb(51, 65, 85));
                AddNumberedSteps(new[] { "确定输入图像和验收目标。", "从上到下搭建真实数据链。", "先设置常用参数，再处理输入和 ROI。", "逐模块试运行并检查类型化输出。", "用数值判定、结果逻辑、文本或通讯形成输出闭环。", "保存方案并在运行界面验证。" });
            }
            else if (key == "ProductionImageEvidence")
            {
                AddParagraph("周期留图默认关闭。在原运行界面配置的“周期留图”页按需启用；不会新增常驻主按钮。部分实现，待集中验证。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "选择稳定根流程，明确勾选 1–4 个图像源。选择仅 NG / 执行失败或全部生产周期；用户停止不是产品 NG，留 STOP 图必须明确勾选。应用并保存方案。",
                    "进入运行页执行完整根流程。仅明确真实非模拟的手动完整、连续或 TCP 周期符合资格；工程调试、F5、内部子流程、模拟和原图片批次不混入此库。",
                    "从原运行周期记录窗口选择周期，切到“原图证据”，显式选源查看。历史图在独立只读画面显示，不改变当前图像、ROI、判定，也不重新执行设备或算法。"
                });
                AddSubheading("图像表示什么");
                AddBullet("这是该 RunId 中成功源采集步骤的原图，不是失败步骤输入、结果或缺陷叠加。采集失败、步骤未到达/跳过、来源失效或超预算均缺图，绝不用上一帧、当前显示图或旧路径补造。");
                AddBullet("首版无损 PNG 仅支持 byte 单/三通道、完整矩形有效域；单幅最多 1600 万像素、边长 16384，本周期最多 64 MiB 副本/编码字节。其他像素、通道或 domain 明确拒绝，不自动缩放成 8 位。");
                AddBullet("证据库最多 1 GiB / 10000 文件，扫描有界；值记录仍独立受 256 MiB / 10000 条 / 30 日限制。只清理已验证旧周期的已确认关联图，坏记录、孤立图和未知临时文件保留警告；不是永久归档。");
                AddBullet("旧历史没有图片字段就显示历史未保存。读图核对记录、相对路径、长度、SHA 和解码预算；SHA 是保存字节身份，不是签名或现场真实性认证。缺图不改变业务判定，也不自动重发 PLC 结果。");
            }
            else if (key == "ProductionResultItems")
            {
                AddParagraph("从原运行界面配置切到结果项，工程人员可选最多八项正式标量输出。未配置时不增加生产卡，配置只控制显示，不改变检测判定或写入 PLC。部分实现，待集中验证。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "添加一项，选择真实根流程、模块和 Number / String / Boolean / ResultCode 输出。根子流程调用的正式输出可用，不能跨作用域直接绑定内部模块；不从全局变量当前值或日志摘要猜测结果。",
                    "设置简短标题；Number 才出现 0–8 位显示小数，单位只能来自明确的真实输出合同。未声明如实提示，不通过输入 mm 伪装标定。长身份和端口说明在悬停查看。",
                    "草稿即时预览，应用时重新读取真实定义校验来源与类型，未完成或失效来源须明确修复；应用受工程授权和真实忙碌保护。取消恢复原布局，重置清空自定义项，保存后连同方案和工程副本复用。",
                    "明确进入生产页后运行完整根流程。手动单次、连续或 TCP 触发都保留开始时页面和真实来源；页面切换不追改资格。工程 F5、范围调试、模拟和图片批次会清空旧生产值，不计作本次生产结果。",
                    "周期运行中不沿用旧值；收尾才显示本次冻结值。业务 NG 可保留有效测量，未到、停用、SKIP、类型改变、非有限数和超长文本显示缺失；ERROR / STOP / TIMEOUT 不用部分合格数值遮住失败。原检测结果不被重判。",
                    "新周期、方案 / Flow / 换型、正式配置或显示映射操作会使旧值失效。复制流程或模块不会按同名自动复制卡片映射，原项仍绑定原稳定身份；新流程请显式配置，删除旧来源后需修复悬空项。"
                });
                AddSubheading("当前周期不是历史或过程能力");
                AddParagraph("数值、文本和布尔按原始类型冻结，不持有图像或工具缓存。Number 显示保留整数/decimal 原精度，小数只是格式化；文本最多 1024 字符，过长为缺失，不把截断当完整输出。来源、RunId、算法定义和未保存/草稿事实按需查看；本卡不扩写历史库、SPC 或生产统计，也不证明产品合格、设备可用或已通过现场验收。", false, Color.FromRgb(51, 65, 85));
            }
            else if (key == "EngineeringDeployment")
            {
                AddParagraph("文件→导出工程副本打开按需任务窗，不增加主工具栏按钮。先核对资源，再看目标机器准备；导出不改变当前方案，不运行算法、扩展代码或连接设备。部分实现，待集中验证。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "先处理并应用模块草稿，打开导出任务。清单来自本次独立方案副本，覆盖所有流程及子流程的正式图片、模板、OCR/NCC/变化模型、显式训练/标定样本、脚本 DLL、资源库 DLL 和 HDevelop 主文件。回放目录与隐式依赖不会递归复制。",
                    "缺失或阻断条目禁止继续。选中条目后重新定位只修复导出副本；声明了 SHA 的资源必须保持原字节身份，不能借重新定位替换模型版本。需要更换版本时先回原模块正式应用。长路径、SHA 和原因在悬停或更多详情查看。",
                    "切到目标机器准备，可复制或另存 .txt 报告。当前进程位数和已加载 HALCON 托管标识仅说明本机观察事实；目标机器的原生运行库、HALCON 授权、相机 SDK、串口、网络、DLL 间接依赖仍需人工核对。未知项不显示为部署就绪。",
                    "有未声明依赖时显式确认，再选择独立 .json 文件。不能覆盖当前活动方案或清单源文件。发布前按冻结清单重新核对长度和 SHA，源文件变化会拒绝发布；取消或授权失效不会加载目标或启动设备。",
                    "迁移时同时复制 JSON 与同级 engineering-assets 目录，目录内自动保存资源清单和准备报告。新副本按相对路径引用资源，打开时校验清单、字节身份和目录边界；旧方案的显式绝对路径仍保持兼容语义。"
                });
                AddSubheading("打包不等于可运行证明");
                AddParagraph("复制 OCR 文件不等于 HALCON 已验证字库，复制一个 DLL/HDevelop 主文件不等于其间接依赖齐备，也不是安全沙箱。工程人员应只使用受信任扩展；正式加载/运行仍遵守原入口检查。导出不包含本机保护口令、运行对象、统计、历史连接或授权文件，不提供安装器。发布失败仅尝试清理本次创建的文件，权限或占用导致的残留不会通过递归删除处理。", false, Color.FromRgb(51, 65, 85));
            }
            else if (key == "ProductionTrends")
            {
                AddParagraph("从原运行周期记录窗口切到“趋势 / 过程”，默认主界面不增加按钮。它只观察已保存的本地周期，不修改判定，不恢复设备或重放通讯。部分实现，待集中验证。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "先选择身份。默认是打开/刷新时的已应用定义和流程；参数、订阅、ROI、设备、脚本、模型或单位变化拆开，不按同名方案混合。旧记录没有稳定键，按旧快照独立查看。",
                    "明确选择生产、工程、模拟或资格未知。旧记录没保存模拟事实，不能补称真实生产；S135 当前仍不重复保存图片批次和模拟周期。",
                    "选择最近 200/1000/2000 周期及时间范围，默认耗时；从一个数值框选择正式 Number 端口。未执行、失败、非有限值、旧记录无端口和截断保持缺失，不补零。未声明单位会明确显示，不能当作毫米。",
                    "默认排除稳定算法定义未保存/保存状态未知、未应用草稿和截断记录。原完整快照的未保存标志仍在详情保留；运行变量值和显示变化不会伪装成算法参数已更改。需要追查时显式勾选包含，不能因此证明已批准的生产能力。",
                    "曲线点击位置或选择表格行，再点击查看所选周期，回到同 RunId 的原步骤。历史只读，不按名字猜当前模块。缺失点断开；业务 NG、ERROR、TIMEOUT 和 STOP 各自显示。",
                    "导出当前趋势会先冻结点击时的范围、身份、值和警告，再选择 CSV。UTF-8 BOM、转义和公式防护保留；不包含未载入范围，不执行历史。"
                });
                AddSubheading("统计范围");
                AddParagraph("完成的 OK/业务 NG 的有限值才进入 Min/Max/Mean/波动。ERROR/TIMEOUT/STOP 可看真实事实，但不进入业务 NG 分母。窗口最多 2000 周期，正式数值最多每步骤 32 项/每周期 512 项，端口选择最多 512 项，超限有明确标记。不是完整 SPC、Cp/Cpk、现场精度或过程能力认证。", false, Color.FromRgb(51, 65, 85));
            }
            else if (key == "StationEditingProtection")
            {
                AddSubheading("入口与正常使用");
                AddNumberedSteps(new[]
                {
                    "从视图菜单打开工程编辑保护，或单击状态栏的保护文字。未启用时沿用原开发行为；切换页面不代表获得授权。",
                    "工程人员自行设置 8–128 字符的口令，并明确启用本机保护。工程解锁有效期为 5–120 分钟；软件没有默认口令或通用 PIN，重启不恢复工程授权。",
                    "为操作员选入实际 JSON 方案。批准绑定完整路径和文件 SHA，不是输入一个方案名称；方案文件更改或换位置后需要重新批准。启动仍是空白方案，操作员从换型与运行准备载入批准方案，不会自动打开上次方案。",
                    "操作员可运行已配置完整根流程、选择既有配置流程，并显式连接与工程封存设置一致的 TCP。图片调试进入工程专用菜单；任意手工发送、模块 F5、范围调试、参数、ROI、流程、资源与设备配置修改需要工程解锁。",
                    "点击原返回开发入口显式解锁，再沿原参数小窗和流程编辑。主动锁定前明确处理模块和布局草稿；到期后草稿保留、修改冻结，旧授权下的训练或连线结果不能继续发布。停止、断开、安全查看和明确取消继续可用。"
                });
                AddSubheading("边界与恢复");
                AddParagraph("本机策略独立于方案，导入 JSON 不会授予工程权限。批准文件 SHA 不是算法精度或设备可用认证；解锁不能解除换型回退失败等故障，也不会自动运行、重连或重写设备。报警确认与人工处置仍是记录操作，不等于恢复证明。", false, Color.FromRgb(51, 65, 85));
                AddBullet("忘记口令或策略损坏时，由现场工程人员核对站点备份和受管策略，进行人工站点恢复；软件不提供万能解锁码，不自动降级为无保护。保护仅防止本应用内误操作，不是 Windows 文件权限、可信 DLL 沙箱、文件防篡改或企业实名认证。部分实现，待集中验证。", Color.FromRgb(71, 85, 105));
            }
            else if (key == "ProductionAlarms")
            {
                AddParagraph("确认报警只表示已经阅读；记录人工处置不等于设备已经恢复。两项操作都不会重新运行、连接设备、解除保护或重发结果。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "从文件菜单“报警查看与处置…”或运行界面原报警卡“查看 / 处理…”打开按需窗口。卡片显示已载入的未确认/已确认待处置数量和最近简述，不压缩流程；数量不是未经核实的全部故障。",
                    "默认查看未确认和已确认待处置事件，常用只保留状态筛选；级别、方案/流程在高级筛选按需展开。收起后已启用条件仍显示摘要，可一键清除高级条件而保留状态。选事件看发生时间、来源和事实，更多看事件身份/版本与读取警告；普通产品NG和STOP不是设备故障。",
                    "点击“确认已读”只把选定版本记为已确认，仍待处置，不清故障。若版本已变，刷新后重新选择，不改成确认另一条最新事件。",
                    "处理和核对由您在原配置/现场主动完成。已确认后填写1–2048字符处置说明，再“记录人工处置”；不能为空、不会静默截断，也不能覆盖已经处置的旧说明。设备是否恢复仍需核对。",
                    "定位当前模块只选中当前工程节点，保持页面、图像与参数草稿；不会触发隐藏草稿的应用/放弃确认。打开参数是另一项显式操作：沿原工程授权返回开发，处理原未应用草稿，再打开唯一原参数窗。取消、事件版本/完整定义/模块实例变化不报告成功。两项都只认当前会话、流程和发生定义，不打开历史方案或重放PLC操作。"
                });
                AddSubheading("范围与保存边界");
                AddParagraph("首版只有七类明确来源：非子流程内部/非模拟的原根执行器异常与超时（含手动范围，不计产品）、相机连接失败、TCP异常、回传交付不确定、换型双失败和周期记录保存失败，不覆盖任意F5或所有错误。回传须开始IO后失败才记交付不确定，发送前停止不造设备故障；可能已送达的事实不会因确认变成功或未发送。确认/处置不解除原保护，清普通日志不清正式状态。", false, Color.FromRgb(51, 65, 85));
                AddBullet("本地最多2000文件/64MiB、单件32KiB；发生文字3072字符截断标明，备注2048拒绝超长。30日/容量只清已处置且已验证旧事件，未处置/坏文件不默删；新发布失败前可能已清旧历史，非全目录事务。cache有界2000/只移已处置，待写64满会明确未保存；修改锁忙碌不隐藏重试。计数仅已载入范围，保存失败/读取警告不能当作无故障，关闭等待待写。", Color.FromRgb(71, 85, 105));
                AddBullet("这是本地事件与人工处置记录，不是硬件急停、自动恢复、设备认证、不可篡改审计或MES。部分实现，待集中验证，不代表现场验收。", Color.FromRgb(71, 85, 105));
            }
            else if (key == "ProductionRunRecords")
            {
                AddParagraph("一条运行周期记录是一次真正接受并结束的整流程，不是一个模块，也不必然是一件产品。开发调试和生产来源必须分开看。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "从“文件 → 运行周期记录”或运行界面结果卡打开按需记录窗口。默认仅查看明确的生产范围，调试和未知来源不计为产品；要追查开发整流程，请显式扩大来源筛选。",
                    "按时间、方案身份、流程和终态选择范围。列表看一次周期的开始时间、实际来源/页面模式、OK/NG/STOP/TIMEOUT/ERROR与耗时；停止、超时和执行错误不是普通产品 NG。",
                    "选中周期后查看开始时方案定义指纹、路径/未保存状态与独立参数草稿状态、稳定流程身份、本次 RunId、结束事实，以及按真实顺序执行的步骤摘要和停止/异常原因。尚未成功执行任何步骤的接受周期也可能有异常记录。",
                    "保存以本地值记录为准。关闭软件后可重新打开此窗口只读查询，不会把旧判定或外发报文加载回运行态。保存失败/坏文件会明确提示，不改变原检测结果，不重试发送或重放设备操作。",
                    "按当前明确筛选范围导出 CSV。重新打开或关闭历史窗口不会修改当前方案、图像、ROI、判定或连接，不会自动运行。图片批次的图像快照与趋势仍从原批次追溯查看。"
                });
                AddSubheading("记录范围与证据边界");
                AddParagraph("记录原根流程的真实 Full 周期，保留明确来源和开发/生产页面模式；模块 F5、选中/从选中范围、子流程内部、图片批次和离线模拟不进入普通生产周期计数。准备阻断、忙碌丢弃和未获执行槽没有真正开始，不伪造已运行记录。列表以当时归属为准，不因现在切换流程而改名。", false, Color.FromRgb(51, 65, 85));
                AddBullet("首版仅保存有界数值/文本事实，不保存活 HALCON 对象或图像，不是 MES、不可篡改审计、SPC 或历史回放执行。保留数量/容量受本机策略限制；存储失败不会自动修复设备。部分实现，待集中验证，不视为已验收交付。", Color.FromRgb(71, 85, 105));
            }
            else if (key == "RecipeChangeover")
            {
                AddParagraph("换到另一种产品的方案，不等于开始生产。先预览并修复静态问题，再明确确认换型，最后主动确认设备和运行。", true, Color.FromRgb(15, 118, 110));
                AddNumberedSteps(new[]
                {
                    "从文件菜单选择“换型与运行准备…”，或在运行界面当前方案卡点击“换型与准备…”。按需窗口不会压缩流程或打开第二套参数配置。",
                    "点击“查看当前准备”只查看已应用的配置。已知参数、连接和所需文件问题列在清单中；选问题后“定位所选问题”返回原统一参数窗，不会自动提交草稿或运行。",
                    "点击“选择方案…”选择正式 JSON，再选换型后的当前流程。预览只读加载目标；清单限于该根流程及其子流程，不会为了查看问题悄悄替换当前方案。目标问题须先在开发界面打开并修复，再重新预览。",
                    "区分配置/资源阻断和设备待确认。文件 SHA 或静态检查通过不能证明相机、PLC、许可证或算法执行已经就绪，也不代表产品合格。目标无启用模块或含模拟通讯不能用于生产换型，普通打开仍可修复工程。预览后目标内容或确认期间目标选择变化须重新准备。",
                    "停止单次/连续/批次执行、模块试运行、采集和学习，等待真实执行及待回传退出。点击“确认换型”，按提示处理旧参数草稿和未保存方案；取消仍保留旧定义。不能只关闭计时器就换掉正在使用的配置。",
                    "换型成功后核对当前方案/流程身份。旧图像、候选、判定及待外发结果会清理，触发保持停用，不自动连接、采集、检测、回传或重放上一产品结果；设备和生产启动仍由您明确操作。"
                });
                AddSubheading("当前 → 目标的配置审阅");
                AddNumberedSteps(new[]
                {
                    "选定目标后切到配置审阅，主动点击生成 / 刷新审阅。比较当前已应用定义与冻结目标；有未应用草稿会另行提示，查看不会应用草稿或修改流程。",
                    "默认只看算法 / 执行变化；用同一分类框查看显示变化、未知或全部。纯拖动、分组和图层样式与参数、绑定、ROI、脚本、设备及标定分开，不让外观调整淹没关键修改。",
                    "选中条目，展开一个更多区域并排看当前与目标字段。稳定身份决定对象对应关系：同名不同 ID 不猜同一模块，缺失或重复身份明确未知。比较覆盖全部根流程与子流程，而运行准备仍只检查所选根流程。",
                    "路径文字相同不能说明资源字节相同；缺少正式 SHA 的资源显示未知，声明相同也不是算法、安全、授权或设备可用证明。字段、深度或行数超限会标不完整，空分类不等于没有差异。",
                    "当前操作、目标 / Flow / 会话 / 授权或草稿状态变化会清空旧审阅；目标文件变动通知也会失效。需要重新准备并生成。确认换型仍重新锁定文件、复核正式定义和授权，审阅不会授权自动应用或局部合并。"
                });
                AddSubheading("失败和历史的边界");
                AddParagraph("应用失败会尝试恢复旧方案定义、正确路径与保存状态；这不是恢复旧 HALCON 句柄、上一帧图像、会话统计或设备连接。换型中或原定义也未能恢复时，所有运行和启用触发入口被阻止；故障须重新打开有效方案或新建。打开、最近与恢复沿同一事务；恢复允许原正式文件已缺失，成功仍须主动保存，失败保留原恢复身份，不覆盖正式文件。旧 TCP 和在途连接停止，迟到旧会话消息不能串到新会话；不会自动重连。原有结果历史保留其执行时归属，不变成目标产品的新检测结果。", false, Color.FromRgb(51, 65, 85));
                AddBullet("这是本地方案的静态准备与显式换型，不是自动 PLC 换型、设备自动重连、权限审批或现场验收；代码路径部分实现，待集中验证。", Color.FromRgb(71, 85, 105));
            }
            else
            {
                AddSubheading("调试"); AddParagraph("断点在模块执行前命中；单步一次只执行一个启用模块。停止在工具步骤之间生效，不能中断正在执行的单个 HALCON 算子。", false, Color.FromRgb(51, 65, 85));
                AddSubheading("运行准备"); AddParagraph("手动运行与全流程连续运行开始前，会把已知连接、参数及图像/Shape 模板文件问题列入同一个清单。点击修改输入或修改参数进入统一参数窗。上游待运行不等于接线错误，不会仅因此阻止整条流程；准备阶段也不会替您运行设备或通讯。无问题不保证算法、许可证和现场设备已就绪。", false, Color.FromRgb(51, 65, 85));
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
        private void AddBullet(string text) { AddBullet(text, Color.FromRgb(71, 85, 105)); }
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
