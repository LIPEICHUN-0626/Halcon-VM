# S85 全目录任务参数与局部自适应阈值

## 交付结论

S85 是新 Git 批次第 **1/10** 次，已验证完成，按 10 次规则不提交、不推送。

## A 线：参数配置继续降噪

- 任务参数档案由 45 类扩展到当前全部 76 个目录入口；HDevelop 使用空档案明确保留兼容页，不冒充已迁入统一 Schema。
- 中央 `常用` 对任务档案同样执行硬性上限，任何工具第一屏最多 4 个输入决定，不能因档案配置而突破上限。
- 首批真实模式条件显隐覆盖：通道序号、均值模板/中值半径、线性增强/局部增强参数、方向差分偏移、透视显式输出尺寸、XLD 排名结束值和四舍五入位数。
- 1366×768 的局部自适应阈值第一屏只有窗口宽、高、目标明暗、最小灰差 4 项；标准差系数进入唯一“更多参数”。1938×1158 的 XLD 长度排序在排名区间时显示结束排名，Top-N/Bottom-N 时收起。
- S83 的单击整卡拖动、双击配置、8 px 圆点、18×16 命中区、8 px 连线命中带，以及 `全部 / 常用 / 收藏 / 最近 / 片段` 五模式直达和 10 类原位折叠没有回退。

## B 线：HALCON 局部自适应阈值

- 新增第 76 个正式工具 `LocalAdaptiveThreshold`，显示名“局部自适应阈值”，目录分类为“检测/分割”。
- 输入：真实 `Image`；可复用模块检测 ROI、启用屏蔽区及其有效域。
- 参数：`MaskWidth/MaskHeight` 为 3–511 的奇数，`StdDevScale` 为大于 0 且不超过 100，`Offset` 为 0–255，极性为 Dark/Light。
- 执行：彩色图像先转灰，对有效域调用 HALCON 20.11 `var_threshold`，再连接区域并发布托管快照。
- 输出：`Region`、`Area`、`RegionCount`、`WindowWidth`、`WindowHeight`、`Offset`、`StdDevScale`、`Polarity`、`Parameters`、`ResultCode`。
- 下游：Region 驱动形态学；Offset 等 Number 输出驱动 NumericJudge。配方、克隆、重跑释放、陈旧输入清理、日志和图层叠加已覆盖。

## 验证证据

- S85 专项：`Catalog=76; Profiles=76; RegionCount=5899; ROI=True; Downstream=Region+Number; Recipe=True; Conditional=True`。
- 回归：S83 单击拖动/双击配置/工具箱直达、S84 自动阈值、S82 中央配置、S79 自动绑定与来源改选、S36 ROI 屏蔽均通过。
- 静态门禁：156 个 PowerShell 脚本解析通过；XAML XML、1209 个名称唯一性、413 个事件处理器、215 个任务档案控件引用通过；禁用旧专题术语匹配为 0；`git diff --check` 通过。
- 构建启动：Debug/Release x64 均 0 警告、0 错误；两个版本独立启动 8 秒且 `Responding=True`。
- 实体界面：`artifacts/s85/s85-local-adaptive-1366x768.png`、`artifacts/s85/s85-conditional-parameters-1938x1158.png`。

## 真实边界

- 76/76 表示每个目录入口已有明确任务档案，不表示所有历史字段、单位、枚举文案和控件形态均已达到最终质量。
- HDevelop 仍是兼容页；不能据此宣称通用参数 Schema 已覆盖该入口。
- 图像/ROI 仍缺统一 ROI 继承任务入口；相机、完整标定/世界坐标、多流程、设备协议、权限部署和长期产线验证仍未完成。
