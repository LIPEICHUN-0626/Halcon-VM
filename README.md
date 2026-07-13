# HALCON Vision Station

基于 WPF + .NET Framework 4.8 + HALCON 20.11 Progress 的单相机视觉检测工作站。主界面保持 WPF，HALCON 图像窗口通过 `WindowsFormsHost + HWindowControl` 承载。

## 核心能力

- 固定产品级布局：顶部功能区、中央图像区、右侧参数区、底部可拖动结果/日志/报警区、状态栏。
- 图像交互：拖拽图片、文件夹队列、上一张/下一张、自动播放/停止、滚轮缩放、右键平移、双击适应窗口。
- ROI：矩形、圆形、多边形 ROI，缩放和平移后仍可绘制，确认/清除状态明确。
- 检测工具链：Shape Model 模板匹配、Blob 面积筛选、灰度统计、边缘测量、HDevelop 扩展调用。
- 配方：本地 JSON 保存工具启用状态、ROI、模板路径、阈值、HDevelop 路径、TCP 参数。
- 追溯：结果记录、回看图像/ROI、CSV/XLSX 导出、截图保存。
- 运行监控：OK/NG 计数、良率、节拍、连续运行、启动自检、日志与报警落盘。
- 通讯：TCP 客户端/服务端，支持 UTF-8、ASCII、GBK，检测结果可自动发送 JSON。

## 使用流程

1. 打开图片或拖拽图片/文件夹到窗口。
2. 在 `ROI/模板` 页绘制 ROI，点击 `确认ROI`。
3. 点击 `训练/编辑模板` 创建 Shape Model，必要时保存为 `.shm`。
4. 在 `运行流程` 页选择检测工具并设置参数。
5. 点击 `单次运行` 或 `连续运行`。
6. 在底部 `结果记录` 页回看、导出 CSV/XLSX 或保存截图。
7. 在 `配方/诊断` 页保存配方，后续可直接加载复用。

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
