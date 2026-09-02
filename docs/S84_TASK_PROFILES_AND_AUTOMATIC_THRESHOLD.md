# S84 任务参数深化与正式自动阈值

## A 线：参数第一屏按任务决策

- 任务参数档案由 15 类扩展为 45 类，覆盖高频 Image、Region、XLD、卡尺和几何工具。
- 中央配置仍只保留一套模块身份；单击拖动，双击打开；`常用 / 输入 / ROI / 结果` 不变。
- 1366×768 的自动阈值第一屏只有“目标明暗”1 项；1938×1158 的 XLD 圆拟合只有算法、最大采样点、最大闭合距离 3 项，其余 3 项进入单一“更多参数”。
- 未新增永久顶部按钮，8 px 圆点、18×16 命中区、8 px 连线命中带和五个直达工具箱模式保持。

## B 线：HALCON 自动阈值正式工具

- 目录名称：自动阈值分割；总目录为 75 工具/10 类。
- 算法：彩色输入转灰后调用 HALCON `binary_threshold` 的 `max_separability`，支持 Dark/Light。
- 有效域：绑定检测 ROI 的并集减去全部启用屏蔽区；无 ROI 时使用全图。
- 输出：托管 Region、Area、RegionCount、Number Threshold、Polarity、Parameters、ResultCode。
- 下游：Region 驱动形态学，Threshold 驱动 NumericJudge；重跑释放旧 Region，陈旧输入拒绝并清除旧输出。
- 配方与复制：目标明暗进入参数 DataContract、克隆和方案快照。

## 验证证据

- 专项：`S84_AUTOMATIC_THRESHOLD=PASS`，目录 75，档案 45，阈值 153，ROI、Region/Number 下游、配方和生命周期通过。
- 回归：S83 单击拖动/双击配置，S12 阈值，S36 ROI 屏蔽，S79 自动绑定，S82 中央配置全部通过。
- 静态：155 个 PowerShell 脚本解析通过，XAML XML 通过，禁用术语匹配为 0。
- 构建：Debug/Release x64 均 0 警告 0 错误；两个版本独立启动 8 秒且 `Responding=True`。
- 截图：`artifacts/s84/s84-auto-threshold-1366x768.png`、`artifacts/s84/s84-xld-task-profile-1938x1158.png`。
