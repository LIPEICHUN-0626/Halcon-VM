# HALCON Vision Station

基于 WPF + .NET Framework 4.8 + HALCON 20.11 Progress 的 VM 风格工业视觉工作站。产品体验以海康 VisionMaster / VM 为参照，视觉算法内核保持 HALCON；HALCON 图像窗口通过 `WindowsFormsHost + HWindowControl` 承载。

## 核心能力

- VM 单工作台：顶部运行工具栏、左侧可搜索工具箱、带序号的顺序流程、中央图像、右侧选中工具 Inspector、底部结果/日志/报警区。
- 图像交互：拖拽图片、文件夹队列、上一张/下一张、自动播放/停止、滚轮缩放、右键平移、双击适应窗口。
- ROI：矩形、圆形、多边形 ROI，缩放和平移后仍可绘制，确认/清除状态明确。
- 检测工具链：Shape Model、Blob、灰度统计、边缘测量和 HDevelop 均可添加到流程、启停、排序、重命名、运行当前或顺序运行全流程。
- 配方：本地 JSON 保存工具顺序/实例状态、ROI、模板路径、阈值、HDevelop 路径和 TCP 参数；兼容没有 `ToolFlow` 字段的旧配方。
- 追溯：结果记录、回看图像/ROI、CSV/XLSX 导出、截图保存。
- 运行监控：OK/NG 计数、良率、节拍、连续运行、启动自检、日志与报警落盘。
- 通讯：TCP 客户端/服务端，支持 UTF-8、ASCII、GBK，检测结果可自动发送 JSON。

## 使用流程

1. 打开图片或拖拽图片/文件夹到窗口。
2. 在左侧工具箱搜索或选择工具，双击或点击“添加到流程”。
3. 在流程区选择实例，通过上移/下移调整顺序，并在右侧“参数” Inspector 配置。
4. 在“图像/ROI”页绘制并确认 ROI；Shape Model 需要训练或加载 `.shm` 模板。
5. 点击“运行当前”调试单工具，或点击“运行全流程/单次运行/连续运行”。
6. 在底部 `结果记录` 页回看、导出 CSV/XLSX 或保存截图。
7. 使用顶部“保存”持久化配方；在“工程”页运行自检、查看配方和日志目录。

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
