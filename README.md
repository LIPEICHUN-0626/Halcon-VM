# HALCON Vision Station

基于 WPF + .NET Framework 4.8 + HALCON 20.11 Progress 的 VM 风格工业视觉工作站。产品体验以海康 VisionMaster / VM 为参照，视觉算法内核保持 HALCON；HALCON 图像窗口通过 `WindowsFormsHost + HWindowControl` 承载。

## 核心能力

- VM 单工作台：顶部运行工具栏、左侧可搜索工具箱、带序号的顺序流程、中央图像、右侧选中工具 Inspector、底部结果/日志/报警区。
- 图像交互：中央 VM 工具栏提供选择、平移、矩形/圆形/多边形、确认、取消、适应和 1:1；支持拖拽图片、文件夹队列、滚轮缩放和右键平移。
- ROI 图层：每次确认新增独立图层，可命名、显示/隐藏、启停、删除，并在图像侧栏查看绑定摘要。
- 工具级多 ROI：Shape、Blob、灰度、边缘和 HDevelop 可分别绑定多个 ROI；运行时只合并该工具已绑定且启用的 HALCON Region。
- 检测工具链：Shape Model、Blob、灰度统计、边缘测量、HDevelop 和数值判定均可添加到流程、启停、排序、重命名、运行当前或顺序运行全流程。
- 类型化 I/O：右侧 I/O 页显示每个工具的 IN/OUT 端口、数据类型、来源、当前值和连接状态；流程卡同步显示连接摘要。
- 数值判定：可订阅上游分数、数量、面积、均值、长度或 HDevelop 数值，以区间、大小或相等规则形成 OK/NG。
- 配方：本地 JSON 保存工具顺序/实例状态、ROI、模板路径、阈值、HDevelop 路径和 TCP 参数；兼容没有 `ToolFlow` 字段的旧配方。
- 追溯：结果记录、回看图像/ROI、CSV/XLSX 导出、截图保存。
- 运行监控：OK/NG 计数、良率、节拍、连续运行、启动自检、日志与报警落盘。
- 通讯：TCP 客户端/服务端，支持 UTF-8、ASCII、GBK，检测结果可自动发送 JSON。

## 使用流程

1. 打开图片或拖拽图片/文件夹到窗口。
2. 在左侧工具箱搜索或选择工具，双击或点击“添加到流程”。
3. 在流程区选择实例，通过上移/下移调整顺序，并在右侧“参数” Inspector 配置。
4. 使用中央图像工具栏绘制并确认一个或多个 ROI；在右侧“图像/ROI”页勾选它们要绑定的视觉工具，图层的“眼/用”分别控制显示和参与运行。
5. Shape Model 需要训练或加载 `.shm` 模板；Blob、灰度和边缘未绑定 ROI 时按全图运行，Shape/HDevelop 必须绑定至少一个已启用 ROI。
6. 点击“运行当前”调试单工具，或点击“运行全流程/单次运行/连续运行”。
7. 需要质量门禁时添加“数值判定”，选择位于其前方的上游工具和数值端口，配置比较方式与阈值；在 I/O 页确认连接和实时值。
8. 在底部 `结果记录` 页回看、导出 CSV/XLSX 或保存截图。
9. 使用顶部“保存”持久化工具顺序、ROI 图层、绑定关系和参数；在“工程”页运行自检、查看配方和日志目录。

BSB 电池产线是长期验收场景，但按 2026-07-13 用户指令暂缓实施；当前版本不包含 BSB 专用工具。

## 构建

```powershell
& 'D:\vsanzhuangbao\MSBuild\Current\Bin\MSBuild.exe' 'D:\codex\codex1\HalconWinFormsDemo.sln' /p:Configuration=Debug /p:Platform=x64 /m
& 'D:\vsanzhuangbao\MSBuild\Current\Bin\MSBuild.exe' 'D:\codex\codex1\HalconWinFormsDemo.sln' /p:Configuration=Release /p:Platform=x64 /m
```

输出路径：

- `bin\x64\Debug\HalconWinFormsDemo.exe`
- `bin\x64\Release\HalconWinFormsDemo.exe`

## 目录

- `logs\`：运行日志。
- `recipes\`：配方 JSON。
- `config\ui-state.json`：窗口分隔条和最近配方状态。
