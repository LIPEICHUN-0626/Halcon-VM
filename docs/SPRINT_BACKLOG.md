# Sprint Backlog

更新日期：2026-08-14

## 下一 Sprint：S56 VM 式紧凑顶部应用栏 + 通用数值运算工具

### A 线：继续回收首屏垂直空间

- 把标题、方案/保存状态、高频命令、模式切换和 OK/NG/良率/节拍重排为菜单外最多两行的紧凑应用栏。
- 四块大统计卡改为紧凑状态单元；现有 5 个高频命令、菜单和快捷键保持，不增加永久按钮。
- 双尺度截图证明顶部显著变矮、无命令换行，释放空间真实归还流程、图像和参数区。
- 开发/生产切换、方案脏状态和 S55 低高度参数布局不得回退。

### B 线：通用数值运算流程工具（非 BSB）

- 订阅 NumberA 和可选 NumberB，支持加、减、乘、除、最小、最大、绝对值和四舍五入。
- 输出 Number、OperandCount、Operation、ResultCode，并进入日志、异常、配方、复制和撤销/重做闭环。
- 以 `文本解析.Number -> 数值运算.NumberA -> 数值判定.Value` 真链覆盖单/双输入、除零、非法精度和非有限值；不冒充 BSB 规则。

### S56 验收

- 双尺度 UI 顶部明显变矮，无命令换行，主工作区获得真实空间。
- 旧配方与 S09-S55 不回退；Debug/Release、独立启动、事件入口、双目标 UI 和真实数值下游通过。
- BSB 继续未实施/未验证；S56 记为新 Git 批次 `1/10`，不提交或推送。

## 已完成 Sprint：S55 VM 式低高度参数工作区重排 + 通用文本解析工具

### A 线验收结果

- 参数任务激活时右下配置区最小高度由 210 提升到 300，右上图像区仍保留 230；离开参数任务恢复原 280/210 分配。
- 模块标题、类型、结果和草稿状态压为第一行，试运行及状态感知草稿动作压为第二行；输入长摘要不再常驻重复显示，保留在标题悬停和 I/O 页。
- 逻辑 1366×768 直接显示图像滤波 4 个关键参数；1938×1158 直接显示文本解析 4 个关键参数和高级入口。

### B 线验收结果

- 新增非 BSB 通用“文本解析”：String 输入、字面分隔/正则匹配、一基字段序号、Trim、Keep/Skip/Reject 和可选 InvariantCulture 数值转换真实可用。
- 输出 Text、Number、FieldCount、Matched、ResultCode；真实驱动数值判定和下游文本组装。
- 连续分隔、越界、正则多匹配/无匹配/非法表达式、转义换行、非法数值、旧配方、配方保存、复制和撤销/重做通过。

### S55 门禁

- 专项和旧 v1/S09-S55 共 48/48；287 个唯一 XAML 事件入口、0 缺失。
- 1938×1158 与逻辑 1366×768（物理 1708×960）实体 UI 通过；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。
- Git 当前批次达到 `10/10`，统一提交并推送；真实网络设备、相机、Shape 模板和 `.jw/.handle` 未验证；BSB 未实施/未验证。

## 已完成 Sprint：S54 VM 式关键/高级参数分级 + 通用文本组装工具

### A 线验收结果

- 50 类已迁入右侧常驻配置的工具统一限制为最多 6 个默认关键输入；额外字段进入一个高级分组，简单工具会明确显示“无高级参数”。
- 高级展开状态在本次会话按工具类型记忆；高级校验失败自动展开、显示就地错误并聚焦高级输入。Shape/HDevelop 兼容页未冒充迁移。
- 图像滤波实体界面显示 4 个关键参数；文本组装显示 3 个关键参数和 1 个高级项。草稿、应用、撤销、试运行、配方和撤销/重做保持原语义。

### B 线验收结果

- 新增非 BSB 通用“文本组装”，可选订阅 String/Number/ResultCode，支持模板、转义分隔符、InvariantCulture 数值格式和 Keep/Skip/Reject 空值策略。
- 输出 Text、Length、ResultCode；真实 `文本组装.Text -> TCP.String` 本机 loopback，以及 Length 到数值判定下游均通过。
- 中文、ASCII、空值、换行转义、非法格式、配方、复制和撤销/重做通过；没有使用 BSB 字段或协议。

### S54 门禁

- 专项和旧 v1/S09-S54 共 47/47；287 个唯一 XAML 事件入口、0 缺失。
- 1938×1158 与逻辑 1366×768（物理 1708×960）实体 UI 通过；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。
- Git 当前 `9/10`，不提交或推送；真实网络设备、相机、Shape 模板和 `.jw/.handle` 未验证；BSB 未实施/未验证。

## 已完成 Sprint：S53 VM 式右侧任务型配置器 + 通用 TCP 客户端流程工具

### A 线验收结果

- 右侧一级只保留 `参数 / I/O / 结果` 三个入口；运行设置、实例重命名、ROI/模板、帮助、图层、通讯和方案进入 `更多`，外层/内层标签墙不再暴露。
- 标题区显示实例、工具类型、真实输入和当前结果；无草稿时只保留一个紧凑试运行，应用/撤销仅在有变化或错误时出现。
- 1938×1158 和逻辑 1366×768（物理 1708×960）截图确认图像、流程和当前参数为首屏焦点。

### B 线验收结果

- 新增非 BSB `TCP 客户端发送/接收`：真实订阅 String/Number/ResultCode，配置主机、端口、连接/发送/接收超时、UTF-8/ASCII、行尾、模板和等待响应。
- 输出 Connected、SentText、ReceivedText、RoundTripMs、BytesSent、BytesReceived、ResultCode，并进入日志、异常、配方、复制和真实 Number 下游。
- 本机 loopback 覆盖正常/空响应、UTF-8/ASCII、CR/LF/CRLF、模板替换、超时、拒绝、停止、非法端口和资源释放；未验证真实 PLC、产线或 BSB 协议。

### S53 门禁

- 专项和旧 v1/S09-S53 共 46/46；763 个 XAML 事件引用、293 个唯一入口、0 缺失。
- Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。
- Git 当前 `8/10`，不提交或推送；真实网络设备、相机、Shape 模板和 `.jw/.handle` 未验证；BSB 未实施/未验证。

## 已完成 Sprint：S52 VM 式流程详情与工具目录聚焦 + HALCON XLD 轮廓方向一致性/角差测量

### A 线验收结果

- 选中节点保持 84 px 关键摘要卡，完整 IN/OUT 由节点“端口”或右侧 I/O 按需展开；真实连接、拖拽和执行语义不变。
- 同一来源/目标的多路连接只显示一个短标签；完整端点、类型、值和状态保留在悬停/选中检查器。
- 左侧默认从全部工具改为 8 个常用工具，收藏、最近、全部与搜索一步可达。

### B 线验收结果

- `smallest_rectangle2_xld` 主轴方向按 180° 等价归一化，支持目标角、绝对偏差闭区间、开闭策略、最小点数和近正方形不稳定阈值。
- 输出托管矩形+主轴 XLD、PrimaryAngle、TargetAngle、SignedDeviation、AbsoluteDeviation、Pass/Fail、索引、统计、参数与结果码，并驱动真实 XLD/Number 下游。
- 轴对齐/旋转、正反点序、开放/闭合、自交点集、跨角度边界、近正方形、退化、多/空、非法参数、所有权、释放、配方和试运行通过。

### S52 门禁

- 专项和旧 v1/S09-S52 共 45/45 回归通过；1938×1158、逻辑 1366×768（物理 1708×960）与 HALCON 图形截图通过。
- Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且响应正常。
- Git 当前 `7/10`，不提交或推送；真实产线样本、Shape 模板、相机和 `.jw/.handle` 未验证；BSB 未实施/未验证。

## 已完成 Sprint：S51 VM 式顶部与图像命令分级 + HALCON XLD 最小旋转矩形/方向测量

### A 线：继续减少永久暴露（已验证）

- 顶部全局工具条只保留方案保存、图像输入、运行/停止、开发/生产等最高频动作；新建、打开、另存、批次、调试和其他低频能力进入菜单或单一“更多”任务面。
- 图像工具条只保留选择/平移/适应和当前上下文动作；八类 ROI、十字、图层、轮廓检查和其他低频能力从属于统一渐进入口，不再横向铺满。
- 继续沿用 S50 的紧凑工具箱、节点渐进端口、右侧互斥任务页和底部折叠，不允许按钮回流。
- 1938×1158 与逻辑 1366×768 实体 UI 都必须无命令换行；工具提示、快捷键和菜单保证低频命令两步内可达。

### B 线：HALCON XLD 最小旋转矩形/方向测量（已验证）

- 通过现有工具箱、流程节点和选中后的右侧配置进入，不增加永久工具栏按钮。
- 实施前核对 MVTec 官方 `smallest_rectangle2_xld` 语义和本机 HALCON 20.11 精确 .NET 签名，明确方向角、半长、开放/闭合和退化轮廓策略。
- 输出托管旋转矩形 XLD、CenterRow/CenterColumn、Phi、Length1/Length2、Width/Height、Area、四角点、输入/有效/跳过计数、Min/Max/Mean、参数和 ResultCode。
- 完成输入订阅、草稿/应用/撤销/试运行、图层、日志、异常、配方回读、旧配方和 XLD/Number 下游闭环。

### S51 验收

- 与 S50 同尺度截图对比，顶部和图像区的永久按钮数量继续明显下降，流程与图像仍是首屏注意力中心。
- 原有高频路径不增加步骤，低频能力两步内可达；旧菜单、右键和快捷键功能不丢失。
- XLD 覆盖轴对齐/旋转矩形、凹形、开放/闭合、正反点序、多/空、自交/退化、角度等价、非法参数、输入所有权、上游未运行/释放、旧快照释放、配方和真实下游。
- Debug/Release、独立启动、旧配方、HALCON、事件入口和双目标 UI 通过；BSB 继续未实施/未验证。

### S51 验收结果

- 顶部常驻动作由 8 个降为 5 个，图像区常驻动作由约 9 个降为 4 个；低频能力保留在菜单、更多和图像任务内，不超过两步。
- `smallest_rectangle2_xld + gen_rectangle2_contour_xld` 的尺寸、半周方向、开闭策略、自交点集、退化跳过、范围、托管 XLD、下游和配方通过。
- 旧 v1 与 S09-S51 共 44/44；1938×1158 与逻辑 1366×768（物理 1708×960）UI、HALCON 图形、Debug/Release 0 警告 0 错误及双版本 8 秒响应通过。Git `6/10`，不提交或推送；BSB 未实施/未验证。

## 已完成 Sprint：S50 VM 式主工作区信息密度重构 + HALCON XLD 最小包围圆/直径测量

### A 线：默认首屏和当前任务聚焦（已验证）

- 左侧工具箱已改为紧凑单行与选中详情；中央流程节点默认显示状态和连接摘要，选中或手动展开后才显示完整 IN/OUT。
- 调试监视默认隐藏到流程“更多”，图层从图像浮层迁入右侧互斥任务页，底部结果/日志默认折叠并记忆，永久批量按钮行不再占据首屏。
- 1938×1158 与逻辑 1366×768（系统缩放后物理 1708×960）均无第二排永久命令，高频“添加工具→选择→配置→试运行”路径保持不变。

### B 线：HALCON XLD 最小包围圆/直径测量（已验证）

- 现有工具箱、流程和右侧常驻配置已形成真实入口，无新增永久按钮；支持全部/开放/闭合策略、最小点数、闭合容差和直径闭区间。
- 本机 HALCON 20.11 `smallest_circle_xld + diameter_xld` 输出托管圆/直径线 XLD、圆心、半径、直径、最远点对、输入/有效/开闭/自交/退化计数、统计、参数和 ResultCode，并驱动真实 XLD/Number 下游。
- 草稿、应用、撤销/重做、试运行、图层、日志、异常、配方保存/回读、旧配方和生命周期闭环通过。

### S50 验收结果

- S50 专项及旧 v1/S09-S50 共 43/43 回归通过；276 个 XAML 事件入口无缺失。
- 1938×1158、逻辑 1366×768 UI 和 HALCON 圆/直径线图形通过；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且响应。
- 真实产线样本、Shape 模板、相机及 `.jw/.handle` 缺样本/设备未验证；BSB 未实施/未验证；Git `5/10`，不提交或推送。

## 已完成 Sprint：S49 VM 式历史工具标量趋势、首屏减负 + HALCON XLD 圆度/凸性筛选

- A 线：历史工具标量趋势可按稳定 ToolId 和 Number 端口绘制跨图片趋势，支持时间/批次/结果筛选、OK/NG/异常/缺失标记、悬停和点击进入既有历史回看，旧记录明确降级且实时状态隔离。根据用户反馈，又把顶部、流程和图像三处低频命令收进默认折叠的“更多/ROI”组，并收紧按钮尺寸和间距。
- B 线：本机 HALCON 20.11 `circularity_xld + convexity_xld + test_self_intersection_xld + select_obj` 已进入工具箱、流程、右侧常驻配置、试运行、托管 XLD、图层、日志、配方和 XLD/Number 下游；有效闭合非自交轮廓按圆度/凸性闭区间筛选，开放、自交和退化轮廓可见跳过。
- 验证：完整旧配方与 S09-S49 共 42/42 在最终纯 XAML 减负前通过；减负后 S25/S34/S36/S40/S42/S47/S49 七组直接相关回归通过；两个 1938×1158 实体 UI、Debug/Release 0 警告 0 错误、272 个 XAML 事件入口无缺失、双版本独立启动 8 秒且 `Responding=True`。真实产线样本、Shape 模板、相机和 `.jw/.handle` 缺样本/设备未验证；BSB 未实施/未验证；Git `4/10`，不提交或推送。

### S49 原计划留档

#### A 线：历史工具标量趋势

- 在严格只读历史上下文选择稳定 ToolId 和一个真实 Number 输出端口，按记录时间/批次顺序绘制跨图片标量趋势。
- 显示 OK/NG/异常标记、缺失点、样本值、批次和时间；提供批次/时间/结果筛选、悬停详情和适应视图。
- 点击趋势点进入现有历史双图/图层/逐工具回看，返回趋势后保留筛选与视图；开发/生产共用同一数据源。
- 同名工具不同 ToolId 不得混线；旧记录无逐工具/端口明细时明确降级；不修改实时流程、ROI、绑定、输出、统计或配方。

#### B 线：HALCON XLD 圆度/凸性筛选

- 工具箱、流程卡、右侧常驻配置、类型化 XLD 输入、草稿/应用/撤销/试运行、日志、图层、异常和配方闭环。
- 使用本机 HALCON 20.11 `circularity_xld` 与 `convexity_xld` 测量有效闭合非自交轮廓，按圆度和凸性两组闭区间筛选；参数页明确两者均以 1 为理想上限及凹陷/细长形状语义。
- 开放、自交、点数不足、近零面积或非有限特征采用可见跳过策略并分别计数，不让 HALCON 自动闭合掩盖开放轮廓。
- 输出托管 XLD、稳定一基原始索引、输入/输出/跳过计数、圆度/凸性 Min/Max/Mean、参数、策略和结果码，并驱动现有 XLD 工具及 NumericJudge。

#### S49 验收

- 趋势覆盖跨批次顺序、同名不同 ToolId、端口缺失、旧/损坏记录、OK/NG/异常、筛选、悬停/点击回看和实时状态隔离。
- XLD 覆盖圆、矩形、凹形、正反点序、开放/闭合、自交/退化、多/空、闭区间边界、非法范围、输入所有权、上游未运行/释放、旧快照释放、配方和真实下游。
- 旧配方与 S09-S48 不回退；Debug/Release、双版本启动、HALCON 和高 DPI UI 通过。BSB 继续未实施/未验证。

## 已完成 Sprint：S48 VM 式历史逐工具双样本对比 + HALCON XLD 外接框/紧凑度筛选

- A 线：按 ToolId/实例身份对齐基准与对比样本，显示新增/缺失、结果码、耗时差、Number/String 旧新值和数值 Δ；搜索、变化筛选、左右历史图层关联、旧记录降级、明细/差异互斥展开及放大双图排查均已进入活动 UI，实时状态不变。
- B 线：本机 HALCON 20.11 `smallest_rectangle1_xld + compactness_xld + test_self_intersection_xld + select_obj` 已接入常驻工具闭环；开放、自交、点数不足、零宽高和无效紧凑度跳过，宽/高/紧凑度闭区间选择、托管 XLD、稳定索引、统计、配方、图层、日志和 XLD/Number 下游通过。
- 验证：S48 专项与旧配方/S09-S48 共 41/41；两个 1938×1158 UI 与 HALCON 导出；Debug/Release x64 0 警告 0 错误；双版本独立启动 8 秒且 `Responding=True`。真实产线样本、Shape 模板、相机和 `.jw/.handle` 缺样本/设备未验证；BSB 未实施/未验证；Git `3/10`，不提交或推送。

## 已完成 Sprint：S47 VM 式历史逐工具结果明细 + HALCON XLD 面积/方向筛选

- A 线：批次 JSON v3 已逐图持久化工具顺序、ToolId、实例名、类型、结果码、耗时、错误和真实 Number/String 输出；开发/生产共用同一只读明细工作区，搜索、判定筛选、旧记录空状态、损坏字段降级和历史图层联动通过，不保存运行态 HImage/HRegion/XLD。
- B 线：HALCON `get_contour_xld + test_self_intersection_xld + area_center_xld + select_obj` 工具完整进入工具箱、端口、右侧配置、试运行/应用/撤销、执行、托管 XLD、图层、日志/异常、配方和下游；开放、自交、点数不足及近零面积轮廓明确跳过，面积边界包含两端，方向映射在 UI 明示。
- 验证：专项与旧配方/S09-S47 共 40/40；两个 1938×1158 UI/HALCON 导出；Debug/Release x64 0 警告 0 错误；双版本独立启动 8 秒响应。真实产线样本、Shape 模板、相机和 `.jw/.handle` 未验证；BSB 未实施/未验证；Git 新批次 `2/10`，不提交或推送。

## 已完成 Sprint：S46 VM 式历史结果浏览/图层控制 + HALCON XLD 点数/闭合筛选

状态：**已验证实现**

- A 线已验证：只读历史上下文支持逐图上一/下一和跨批次连续导航，不可读项自动跳过；源图/结果图可单独或双画面对比，支持独立/联动缩放平移和坐标隔离；历史 Region/XLD 清单支持显隐、全显/全隐、独显。返回实时恢复进入前的单双画面、联动、最大化和精确视口，不修改实时图像、流程、ROI、绑定、输出或统计。
- B 线已验证：HALCON `get_contour_xld + select_obj`“XLD 点数/闭合筛选”按点数闭区间、全部/开放/闭合和首末端点欧氏距离容差选择轮廓，输出托管 XLD、稳定原始索引、输入/输出与开闭计数、点数 Min/Max/Mean、参数和结果码，并驱动 XLD/Number 下游。
- 验证：首/中/末、跨批次、损坏跳过、双图坐标隔离、独立/联动、空/多层、显隐/独显和实时状态恢复，以及点数/容差边界、开闭/多/空、非法参数、所有权、释放、配方和下游通过；旧配方与 S09-S46 共 39/39，两个 1938×1158 实体 UI 和 HALCON 导出通过，Debug/Release 0 警告 0 错误，双版本独立启动 8 秒响应。真实产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备未验证；BSB 按用户指令未实施/未验证；Git 新批次 `1/10`，不提交或推送。

## 已完成 Sprint：S45 VM 式持久结果图像回看 + HALCON XLD 长度排序/索引选择

状态：**已验证实现**

- A 线已验证：源图/结果图及 Region/XLD 图层按安全相对路径保存；跨重启、源图删除、结果损坏降级、空/多层、同名文件、容量/批次/孤立清理、只读隔离、返回实时及开发/生产入口同源通过。
- B 线已验证：`length_xld + select_obj` 稳定升/降序，Top-N、Bottom-N、排名范围、原始索引字符串、统计、托管 XLD、配方和 XLD/Number 下游通过。
- 验证：旧配方与 S09-S45 共 38/38，1938×1158 UI 与 HALCON 历史 Region/排序 XLD 导出，Debug/Release 0 警告 0 错误，双版本独立启动 8 秒响应。BSB 按用户指令未实施/未验证。

## 已完成 Sprint：S44 VM 式批次追溯工作台 + HALCON XLD 长度分级

状态：**已验证实现**

- A 线：图片批次以独立 JSON 原子保存批次 ID、真实方案名/SHA-256 指纹、起止、范围、各类计数、总耗时和逐图路径/状态/结果码/耗时/消息；开发态“批次追溯”和生产页“最近批次追溯”共用同一集合，时间/批次/方案/结果筛选、明细、重载、CSV、旧字段归一化、损坏隔离及保存失败不阻塞通过。当前仅为本地文件追溯，不等于 MES/数据库。
- B 线：右侧常驻“XLD 长度分级”真实调用 HALCON `length_xld + select_obj`，以 `短≤T1 / T1<中≤T2 / 长>T2` 互斥输出三组托管 XLD、各组 Count/Total/Min/Max/Mean、阈值/参数/结果码，默认绿/黄/红图层并驱动 XLD/Number 下游。
- 门禁：S44 专项覆盖跨重载、旧/损坏记录、重复文件名、损坏图、筛选/CSV/方案指纹/开发生产同源，以及阈值边界、相等/反序/非有限、空/多、所有权、释放、配方、三图层和下游；旧配方与 S09-S44 `37/37` 回归、两张 1938×1158 实体 UI、Debug/Release 0 警告 0 错误、双版本独立启动 8 秒 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 未验证；BSB 按用户指令未实施/未验证；Git `9/10`，不提交或推送。
- 未完成：批次记录没有持久结果图片/叠加回放、数据库/MES、权限、归档和容量治理；仍缺完整 VM 任意控件设计器、多流程、资源、坐标/标定和完整通信。

## 已完成 Sprint：S43 VM 式运行界面配置首版 + HALCON XLD 矩形裁剪

状态：**已验证实现**

- A 线：开发态“运行界面配置”以同一生产页实时预览，八个真实部件可改标题、可选显隐和受控顺序；图像、总判定和运行控制不能隐藏，图像固定左侧。草稿、应用、取消、恢复默认、配方、旧配方安全默认、脏状态和撤销/重做通过；独立分栏消除了 HALCON `WindowsFormsHost` 对 WPF 配置面板的遮挡。
- B 线：右侧常驻“XLD 矩形裁剪”真实调用 HALCON 20.11 `crop_contours_xld`，支持 `Row1/Column1/Row2/Column2/CloseContours`、四角/四边/整体图像草稿及无图像数值回退，输出托管 `XLD/InputContourCount/ContourCount/InputTotalLength/TotalLength/Bounds/CropRectangle/CloseContours/Parameters/ResultCode`，并驱动 XLD/Number 下游。
- 门禁：S43 专项覆盖布局、旧配方、撤销、穿越四边、内外/相切、开闭/多/空、闭合两态、异常、所有权、释放、配方与下游；S09-S43 `35/35` 回归、两张 1938×1158 实体 UI、Debug/Release 0 警告 0 错误、双版本独立启动 8 秒 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 未验证；BSB 按用户指令未实施/未验证；Git `8/10`，不提交或推送。
- 未完成：仍不是完整 VM 任意控件设计器；缺权限、持久结果图片/叠加回放、真正多画面、Shape/HDevelop 受管叠加、位置修正、ROI 继承、类型化数组、Geometry/Pose、多流程、资源和完整通信。

## 已完成 Sprint：S42 VM 式生产运行界面首版 + HALCON XLD 透视变换

状态：**已验证实现**

- A 线：顶部“开发/运行”模式和独立生产运行页已接入活动 WPF；运行页真实复用当前方案、确定性执行器、HALCON 图像/叠加、单次/连续/暂停/停止、图片当前/选中/全部、OK/NG、良率、节拍、报警和最近结果。运行模式收起菜单、模块箱、流程、配置、底部页签和图像编辑工具，退出 ROI/测量/透视/XLD 拾取等编辑态；切换不复制执行状态或托管快照，并在退出后恢复开发布局与图层工作台。
- B 线：右侧常驻“XLD 透视变换”以四组源/目标点和 `normalized_dlt` 生成投影矩阵，真实调用本机 HALCON 20.11 `projective_trans_contour_xld`；支持标准像素中心 ±0.5 与原始 `affine_trans_pixel` 两种坐标策略，输出托管 `XLD/ContourCount/InputTotalLength/TotalLength/Bounds/HomMat2D/PointError/CoordinatePolicy/AttributePolicy/Parameters/ResultCode`，并真实驱动后续 XLD 工具与 NumericJudge。
- 门禁：`S42ProductionRunAndXldProjectiveSmoke.ps1` 覆盖运行页状态复用/编辑隔离/运行与队列控制，以及 XLD 恒等、平移、梯形、两种坐标策略、开/闭/多/空、退化/自交/非有限、输入所有权、旧快照释放、配方和下游链；Legacy 与 S09-S42 最终 `35/35` 回归通过。两张 1938×1158 实体 UI 确认生产运行页和 XLD 透视常驻配置；Debug/Release x64 均 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git `7/10`，不提交或推送。
- 未完成：生产运行页尚无受控布局设计器、权限和持久追溯；仍缺真正多画面、Shape/HDevelop 真实受管叠加、位置修正、ROI 继承、类型化数组、Geometry/Pose、多流程、资源和完整通信。

## 已完成 Sprint：S41 VM 式图片运行范围 + HALCON XLD 仿射变换

状态：**已验证实现**

- A 线：S40 同一本地图像队列新增首张/末张、全选/清选和“当前/选中/全部”范围；缩略项双向多选并显示未运行/运行中/OK/NG/异常/停止/耗时，批次区显示进度和 OK/NG/异常/未运行汇总。逐图真实调用现有确定性流程，复用停止、50 ms–1 h 超时、遇 NG 停止和托管输出语义；播放/手动运行/连续运行/图片批次互斥，停止在图片间隔内以 50 ms 分片响应。损坏项只记录本项异常，默认继续下一张；末图结束不循环。
- B 线：右侧常驻“XLD 仿射变换”真实调用本机 HALCON 20.11 `affine_trans_contour_xld`，配置输入包围中心/手动中心、Row/Column 平移与缩放、旋转、Row 斜切、四种组合顺序和标准像素中心/原始 `affine_trans_pixel` 两种坐标策略。输出托管 `XLD/ContourCount/InputTotalLength/TotalLength/Bounds/Center/HomMat2D/TransformOrder/CoordinatePolicy/AttributePolicy/Parameters/ResultCode`，真实驱动直线拟合和 NumericJudge；空输入为受管 NG，属性明确交由 HALCON 算子处理且本工具不人工改写。
- 门禁：`S41ImageQueueRunRangeAndXldAffineSmoke.ps1` 覆盖 11 项含损坏队列、当前/选中/全部、首末、多选、停止、遇 NG 停止、50 ms 超时、逐图状态/汇总，以及 XLD 恒等、Row/Column 平移、中心 90°旋转、各向异性缩放、斜切、组合顺序、两种坐标策略、开/闭/多/空、非法参数、输入所有权、旧快照释放、上游释放、配方和下游链。Legacy 与 S09-S41 最终 `34/34` 回归通过；两张 1938×1158 实体 UI 确认完整批次汇总和常驻仿射参数/真实 XLD 叠加；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git `6/10`，不提交或推送。
- 未完成：批次结果尚未持久化为可追溯历史；仍缺生产运行页面/设计器、多画面、Shape/HDevelop 真实受管叠加、位置修正、ROI 继承、类型化数组、Geometry/Pose、多流程、资源和完整通信。

## 已完成 Sprint：S40 VM 式图像队列缩略导航 + HALCON XLD 轮廓平滑

状态：**已验证实现**

- A 线：图像工作区新增可折叠本地图像队列，真实复用 `imageFiles/imageIndex`；缩略项显示图像、文件名、序号、尺寸及当前/已读/失败状态，支持点击、横向滚动、上一/下一、播放/暂停、末图停止、进度、目录重载和清空。BMP/JPG/PNG/TIF/PGM、损坏/删除项、24 项 LRU 缩略缓存、单/双画面、全局图像依赖失效和旧单图入口兼容已闭环；队列使用独立布局行，避免 WPF 浮层被 HALCON WinFormsHost 遮挡。
- B 线：右侧常驻“XLD 轮廓平滑”真实调用本机 HALCON 20.11 `smooth_contours_xld`，配置 `3–999` 奇数 `NumRegrPoints`，输出托管 `XLD/InputContourCount/ContourCount/InputTotalLength/TotalLength/LengthDelta/NumRegrPoints/AttributePolicy/Parameters/ResultCode`，明确 HALCON 输出不保留输入点属性，并驱动直线拟合和 NumericJudge。
- 门禁：`S40ImageQueueAndXldSmoothingSmoke.ps1` 覆盖 31 项混合/损坏队列、点击/前后/播放/末图停止、删除/重载/清空、24 项缓存、多 XLD、短/空、3/5/7/9、偶数/越界、属性丢弃、输入所有权、旧快照释放、配方、路径长度抑制及拟合残差输出；Legacy 与 S09-S40 最终 `33/33` 回归通过。1938×1158 实体 UI 确认队列和常驻配置；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git `5/10`，不提交或推送。
- 未完成：图片队列尚无“当前/选中/全部”流程运行范围和逐图 OK/NG 汇总；仍缺多画面、Shape/HDevelop 真实受管叠加、位置修正、ROI 继承、类型化数组、Geometry/Pose、多流程、资源和生产运行。

## 已完成 Sprint：S39 VM 式 XLD 轮廓拾取/检查器 + HALCON 相邻轮廓连接

状态：**已验证实现**

- A 线：图像工具栏“轮廓选择”可在当前选中或可见真实 XLD 图层中按最近线段命中单条对象，白色描边加品红高亮；常驻检查器显示来源工具/端口、左右坐标上下文、序号/总数、点数、长度、起终点、最近亚像素 Row/Column 和命中距离，并提供上一条/下一条、单条独显、定位生产者、打开配置和清除。状态不持有新 HObject，不修改/释放算法快照；重跑、删除、隐藏、视口范围和图像来源变化会安全失效，并与 ROI、十字线、透视、测量域模式互斥。
- B 线：右侧常驻“相邻轮廓连接”真实调用本机 HALCON 20.11 `union_adjacent_contours_xld`，配置绝对/相对端点距离和 `attr_keep/attr_forget`，输出托管 `XLD/InputContourCount/ContourCount/JoinedCount/TotalLength/Mode/Parameters/ResultCode`，真实驱动 XLD 筛选/分段/四类拟合/双线关系及 NumericJudge。
- 门禁：`S39XldContourInspectorAndAdjacentUnionSmoke.ps1` 覆盖 4 px 短间隙连接、2 px 超阈值不连接、反向点序、两种属性策略、空/非法、输入所有权、旧快照释放、配方、下游及最近线段亚像素投影、前后导航、独显、左右隔离、重跑/来源失效；旧 v1/S09-S39 共 32/32 回归通过。1938×1158 物理鼠标工作区、相邻连接流程/结果和 462×179 参数区通过；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒、`Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git `4/10`，不提交或推送。
- 未完成：图像队列仍只有顶部前后/播放按钮而无缩略导航；Shape/HDevelop 尚未成为受管图层；位置修正、ROI 继承、类型化数组、Geometry/Pose、多流程、资源和生产运行仍缺。

## 已完成 Sprint：S38 VM 式透视四边形图像交互 + HALCON 亚像素边缘提取

状态：**已验证实现**

- A 线：选择“图像透视校正”可在单/双图像区开启源/目标四边形编辑；左侧只显示真实输入坐标源四点，右侧只显示输出坐标目标四点，`1-4` 控制点、方向、数值双向同步、拖动草稿、试运行、应用/取消、恢复图像四角、源到目标复制、非法几何/越界状态、配方及撤销/重做均已闭环。编辑开启时不会误触 ROI 或十字线；该交互明确是依据 VM 图像预览/ROI 习惯的产品化推导。
- B 线：右侧常驻“亚像素边缘提取”显式订阅 Image 和可选 ROI/屏蔽 Region，真实调用本机 HALCON 20.11 `edges_sub_pix`；支持九种 Filter、Alpha/Low/High、RGB 自动转灰/强制单通道，输出托管 `XLD/ContourCount/TotalLength/Filter/ColorPolicy/Parameters/ResultCode`，并真实建立到筛选、分段、四类拟合、双线关系和 NumericJudge 的类型化绑定。
- 门禁：`S38ProjectiveEditorAndSubpixelEdgeSmoke.ps1` 覆盖源 128×96/目标 80×60 坐标隔离、草稿/试运行/应用/取消、恢复/复制、非法几何、配方/撤销，九种 Filter、灰度/RGB、reduced domain、空边缘 NG、参数边界、旧 XLD 释放和真实下游链；旧 v1/S09-S38 共 31/31 回归通过。1938×1158 双视口透视编辑、亚像素 XLD 工作区和 462×264 常驻参数页通过；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒、`Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git `3/10`，不提交或推送。
- 未完成：图像区尚不能选中单条 XLD 并检查亚像素点；Shape/HDevelop 仍未成为受管图层；位置修正、ROI 继承、类型化数组、Geometry/Pose、多流程、资源和生产运行仍缺。

## 已完成 Sprint：S37 VM 式统一图层属性工作台 + HALCON 图像透视校正

状态：**已验证实现**

- A 线：统一“全部图层”真实合并 ROI、屏蔽区和本周期 Region/XLD；显示来源/端口/坐标上下文、名称、颜色、线宽、透明度、显隐、独显、锁定、层级和左右视口范围，并与流程/生产者选择联动。样式进入配方和撤销/重做，显示操作不修改或释放算法快照；Shape/HDevelop 明确标为待迁移兼容项。
- B 线：右侧常驻“图像透视校正”以四组源/目标点、`normalized_dlt` 和源/目标 `+0.5` 像素中心补偿执行本机 HALCON 20.11 `projective_trans_image`/`projective_trans_image_size`；支持 nearest/bilinear、保持输入/自适应/显式尺寸、TransformDomain，输出托管 Image 并驱动 Threshold/Blob/Gray/Edge。
- 门禁：`S37UnifiedLayerAndProjectiveTransformSmoke.ps1` 和旧 v1/S09-S37 共 30/30 回归通过；1938×1158 统一图层 UI、462×415 透视参数页和 HALCON 投影输出实拍通过；Debug/Release x64 0 警告 0 错误，双版本各独立启动 8 秒且保持响应。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 未实施/未验证；Git `2/10`，不提交或推送。
- 未完成：透视四边形仍主要靠右侧数值输入，Shape/HDevelop 尚未成为真实受管图层；位置修正、ROI 继承、多画面/缩略图、通用 Geometry/Pose 和类型化数组仍缺。

## 已完成 Sprint：S36 VM 式 ROI 屏蔽区工作台 + HALCON 图像仿射变换

状态：**已验证实现**

- A 线：面 ROI 已可从图像工具栏、ROI 图层和右侧常驻页切换检测/屏蔽角色；红色斜线覆盖、角色标签、排序、复制、锁定、显隐、启停、命名、删除、绑定、配方和撤销/重做可见。Blob/Gray/Edge、Shape/HDevelop 兼容路径及 ROI 图像提取真实执行 `有效域 = 检测域 - 启用屏蔽区`；逐 ROI 模式逐项扣除屏蔽区，显隐不改变算法，空有效域给出明确错误。线/点因无面积不能作为屏蔽区。
- B 线：右侧常驻 HALCON“图像仿射变换”真实调用本机 20.11 `HImage.AffineTransImage`，支持 Row/Column 平移、Row/Column 缩放、逆时针旋转、Row 斜切、五种插值和 `AdaptImageSize`，输出托管 `Image/Width/Height/Channels/HomMat2D/Interpolation/AdaptImageSize/Parameters/ResultCode` 并驱动 Threshold/Blob/Gray/Edge。行映射矩阵 x、列映射 y和官方 0.5 像素中心校正已落实，输入 domain、负坐标裁剪和安全尺寸限制明确。
- 门禁：`S36RoiMaskAndImageAffineTransformSmoke.ps1` 通过 `8000 - 400 = 7600 px²`、隐藏/停用差异、复制/排序/撤销、合并/逐 ROI Blob、前序 Region/Gray、屏蔽专用/全空拒绝，以及仿射单位、平移、尺寸适应、90°逆时针、五插值、RGB、domain、非法参数/危险尺寸、上游/旧快照释放、配方、旧字段和下游 Threshold。旧 v1 与 S09-S36 共 29/29 回归通过；Debug/Release x64 0 警告 0 错误，Debug 启动、屏蔽区 UI/红色斜线 HALCON 导出和仿射 UI/输出导出通过。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git 新批次 `1/10`，不提交或推送。
- 未完成：位置修正、ROI 继承、完整统一图层属性、Shape 兼容叠加逐层化、多画面/缩略图和通用 Geometry/Pose 端口仍缺；仿射输出属于新坐标上下文，不能直接叠加输入 ROI。

## 已完成 Sprint：S35 VM 式线/点 ROI 图像编辑 + HALCON 图像旋转

状态：**已验证实现**

- A 线：图像工具栏与右侧 ROI 页已新增线/点入口；线具备起止端点、整体移动、长度/角度和数值双向编辑，点具备单控制点和 X/Y 数值编辑。两类均复用选择、复制、锁定、显隐、启停、命名、删除、工具绑定、边界保护、配方和撤销/重做。
- B 线：右侧常驻 HALCON“图像旋转”真实调用 `rotate_image`，支持 0–360°逆时针角度和五种插值，输出托管 `Image/Width/Height/Channels/Angle/Interpolation/Parameters/ResultCode` 并驱动 Threshold/Blob/Gray/Edge；90°/270°交换非方图宽高，输入 domain 被忽略且输出使用新坐标语义。
- 门禁：`S35LinePointRoiAndImageRotateSmoke.ps1` 覆盖线/点控制点、移动、数值草稿、复制/锁定/删除/绑定、配方、撤销/重做和边界；旋转覆盖 0°/90°/180°/270°/45°、4×3 定位像素、灰度/RGB、五插值、缩小 domain、非法参数、上游/旧快照释放、重复运行、配方和下游 Threshold。旧 v1 与 S09-S35 共 28/28 回归通过；1942×1222 UI、原图线/点 ROI 与旋转 HALCON 图像导出、全仓库 558 个 XAML 事件引用/238 个唯一入口/0 缺失通过；Debug/Release x64 0 警告 0 错误，双版本启动响应。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git 批次达到 `10/10`，本轮统一提交/推送。
- 未完成：屏蔽区、位置修正、ROI 继承、多画面/缩略图和通用 Geometry/Pose 端口仍缺；图像旋转输出属于新坐标上下文，不能直接叠加输入 ROI。

## 已完成 Sprint：S34 VM 式双十字辅助线 + HALCON 图像镜像

状态：**已验证实现**

- A 线：图像工具栏“十字线”入口和可折叠常驻面板已交付；两组辅助线分别支持启用、中心 X/Y、颜色、1–5 线宽、居中和锁定，未锁定时图像内拖动与数值双向同步。左右视口设置独立保存，单/双画面切换不串坐标，显示变化不修改算法输出、结果图层或 `InputBindings`。
- B 线：右侧常驻 HALCON“图像镜像”已交付；真实 `mirror_image` 支持 `row/column/diagonal`，输出托管 `Image/Width/Height/Channels/Mode/Parameters/ResultCode` 并可继续驱动 Threshold/Blob/Gray/Edge。4×3 非方图的三种定位像素、对角 3×4、RGB、缩小 domain、异常路径和生命周期通过。
- 门禁：`S34CrosshairAndImageMirrorSmoke.ps1` 和 27/27 全量功能回归通过；1942×1222 十字面板、镜像常驻配置及左右 HALCON 图像导出通过。低高度 UI 首轮发现默认展开面板会压缩图像区，已改为默认折叠并复测通过。532 个 XAML 事件引用对应 231 个唯一入口且 0 缺失；Debug/Release x64 0 警告 0 错误，双版本启动 5 秒保持响应。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 按用户指令未实施/未验证；Git 批次 `9/10`，不提交或推送。
- 未完成：十字线没有标签、快捷键和撤销/重做；面板为本项目产品化布局，不宣称逐像素复制海康；图像镜像是新坐标上下文，原图 ROI 不直接叠加。

## 已完成 Sprint：S33 VM 式双画面图像对比 + HALCON 图像尺寸缩放

状态：**已验证实现**

- A 线：单/双画面显式切换、左侧严格当前模块真实 Image 输入端口、右侧模块输出/兼容上下文、ImageA/ImageB 选择、来源/尺寸/像素/叠加状态、适应/1:1/最大化和联动/独立缩放平移已交付；不同尺寸按归一化中心与可视比例联动，缩放输出不误叠输入 ROI。
- B 线：右侧常驻 HALCON“图像尺寸缩放”已交付；`FullDomain + zoom_image_size` 支持五种插值，输出托管 `Image/Width/Height/Channels/ScaleRow/ScaleColumn/Interpolation/Parameters/ResultCode` 和完整矩形 domain，并可继续驱动 Threshold/Blob/Gray/Edge。
- 门禁：640×440→320×220 得到行列比例 `0.5/0.5`；缩小/放大、灰度/RGB、定位像素、缩小 domain 输入、非法宽高/插值、上游未运行/释放、旧 Image 释放、配方和下游 Threshold 通过。旧 v1 与 S09-S33 共 26 组功能回归、1942×1222 物理 UI/左右 HALCON 图像导出、468 个 XAML 事件引用对应 191 个唯一处理入口且无缺失。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 未实施/未验证。Git 批次 `8/10`，不提交或推送。

## 已完成 Sprint：S32 VM 式 ROI 数值几何属性工作台 + HALCON ROI 图像提取

状态：**已验证实现**

- A 线：六类 ROI 的 X/Y、宽高、角度、长短轴直径、内外直径和多边形顶点常驻数值页已交付；图像/数值双向同步、实时草稿预览、应用/取消、锁定只读、边界/尺寸/半径顺序/自相交校验、配方及撤销/重做通过。
- B 线：HALCON `reduce_domain/crop_domain` 的绑定 ROI/前序 Region、保留原矩阵/原坐标、裁剪外接框和 `OffsetRow/OffsetColumn` 通过；输出托管 Image 可继续驱动 Threshold，灰度/RGB、空交集、上游释放、旧快照释放和配方通过。
- 门禁：旧 v1 与 S09-S32 共 25 组功能回归、1942×1222 物理 UI/四张图像导出、181 个去重 XAML 事件处理方法、Debug/Release 0 警告 0 错误及双版本启动 5 秒通过。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证；BSB 未实施/未验证。Git 批次 `7/10`，不提交或推送。

## 已完成 Sprint：S31 VM 式椭圆/圆环 ROI 图像编辑 + HALCON 极坐标展开

状态：**已验证实现**

- A 线：椭圆/圆环入口、中心/长短轴/旋转与内外半径控制点、创建/选择/移动/复制/锁定/删除/显隐/绑定/配方及几何保护已验证。
- B 线：HALCON `polar_trans_image_ext` 常驻配置、托管 Image、整圆去重、顺逆/开弧、灰度/RGB、插值配置、越界/释放、下游 Threshold 及输入坐标叠加隔离已验证。
- 门禁：旧 v1 与 S09-S31 共 24 组功能回归、Debug/Release 0 警告 0 错误、双版本启动 5 秒、逻辑 1554×978 UI 和四张图像导出通过；真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 缺样本/设备，未验证。BSB 未实施/未验证。Git 批次 `6/10`，不提交或推送。

## 已完成 Sprint：S30 VM 式圆周测量域图像编排 + HALCON 圆环壁厚/同心度测量

状态：**已验证实现**

### A 线完成

1. “圆周卡尺找圆”已接入图像区测量域编辑；选择模块或结果图层后同步真实模块输入、流程、配置和图层，显示搜索圆/弧、全部径向卡尺、红色失败项及圆心、半径、起始角、结束角、Length1、Length2 六个控制点。
2. 真实鼠标可移动圆心/整体、调整搜索半径、起止角和卡尺长宽；拖动只写右侧草稿，试运行不自动应用，应用才写入实例参数、配方脏状态和 50 项撤销/重做，取消恢复。整圆起止角控制点采用不同半径显示，避免重叠后无法命中。
3. 全部卡尺遵守 HALCON bilinear 安全边界，并校验最小半径、30–360°覆盖、数量和 Length/Sigma。切换工具/图层、删除、方案加载和历史恢复会清理预览与拖动状态。

### B 线完成

1. 新增右侧常驻“圆环壁厚/同心度测量”，接入工具箱、流程、真实 Image 输入、参数、试运行、结果、帮助、图层、I/O、日志、配方和 NumericJudge。
2. 每个径向卡尺独立调用 HALCON `gen_measure_rectangle2 + measure_pairs + close_measure`，选择中点最接近搜索半径的边缘对并按配置中心半径排序内外点，再以 ATukey `fit_circle_contour_xld` 分别拟合内外圆。
3. 输出托管搜索弧/卡尺矩形/边缘对/内外边缘点/内外拟合圆 `XLD`，以及内外圆心/半径、Mean/Min/Max/StdDevThickness、Eccentricity、Valid/FailedCount、残差、逐项坐标/厚度、失败详情、参数和结果码；局部失败保留其他有效项，每个 handle 在全部路径关闭。逐项集合当前仍为可读字符串，不冒充类型化数组。

### S30 验证结论

- `S30CircularEditorAndRingThicknessSmoke.ps1` 通过：真实鼠标覆盖圆心、半径、起始角、结束角、Length1、Length2 六控制点，草稿隔离、试运行/取消/应用、撤销/重做、配方和上下文清理通过；圆环测量覆盖整圆、开弧、反向、非同心、部分缺边、低对比、bilinear 越界、扇区离群点、非法数量/角度、上游释放、配方、NumericJudge、旧 XLD 释放和 20 次 handle 生命周期。
- 旧 v1 配方与 S09-S30 共 23 组功能回归全部通过。逻辑 1554×978 主界面和四张 HALCON 图像导出确认圆周编辑控制点、径向卡尺/失败项、内外边缘点和双拟合圆；界面夹具得到 `ValidCount=24`、`MeanThickness=55.060 px`、`Eccentricity=0.017 px`。
- Debug/Release x64 均 0 警告 0 错误；Debug 独立启动 8 秒保持响应，标题为“HALCON VM 工业视觉平台”。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证。S30 记为新 Git 批次第 `5/10` 次，不提交或推送。

## 已完成 Sprint：S29 VM 式线性阵列图像编排 + HALCON 圆周卡尺找圆

状态：**已验证实现**

### A 线完成

1. “线性多卡尺阵列”已接入图像区测量域编辑；选择模块或结果图层后同步真实模块输入、流程、配置和图层，显示基线、起点、终点、整体、Length1/Length2 控制点、全部卡尺和红色失败项。
2. 真实鼠标可移动起点/终点、整体平移和调整卡尺长宽；拖动只写右侧草稿，试运行不自动应用，应用才写入实例参数、配方脏状态和 50 项撤销/重做，取消恢复。
3. 整体移动触边时保持基线长度，不会逐端点裁剪而压缩阵列；全部卡尺遵守 HALCON bilinear 安全边界。切换工具/图层、删除、方案加载和历史恢复会清理预览与拖动状态。

### B 线完成

1. 新增右侧常驻“圆周卡尺找圆”，接入工具箱、流程、真实 Image 输入、参数、试运行、结果、帮助、图层、I/O、日志、配方和 NumericJudge。
2. 沿整圆或正/反向开弧布置 3–360 个径向卡尺，每项独立调用 HALCON `gen_measure_rectangle2 + measure_pos + close_measure`；从多边缘中选择最接近搜索圆的点，再用 `fit_circle_contour_xld` 的 ATukey 鲁棒拟合圆。
3. 输出托管 `XLD/CenterRow/CenterColumn/Radius/ValidCount/FailedCount/MaxResidual/MeanAmplitude/EdgeRows/EdgeColumns/EdgeAmplitudes/ValidIndices/FailedIndices/FailureDetails/Parameters/ResultCode`；部分失败仍保留可用拟合，全部 handle 在成功、NG 和异常路径逐项关闭。

### S29 验证结论

- `S29LinearArrayEditorAndCircularCaliperSmoke.ps1` 通过：真实窗口鼠标验证阵列整体/起点/Length1/Length2、草稿试运行/取消/应用、撤销/重做和陈旧预览清理；找圆覆盖整圆、200°开弧、顺/逆、24 卡尺、半圆缺边、低对比、全无边、bilinear 越界、扇区离群点、非法参数、上游释放、配方、旧 XLD 释放和 20 次 handle 生命周期。
- 旧 v1 配方与 S09-S29 共 22 组功能回归全部通过。逻辑 1554×978 主界面确认线性阵列编辑条和圆周找圆常驻配置；HALCON 窗口导出确认阵列控制点/红色失败卡尺，以及独显 20 个径向卡尺、边缘点和拟合圆，结果为 `ValidCount=20`、`Radius=144.969 px`。
- Debug/Release x64 均 0 警告 0 错误；Debug 独立启动 8 秒保持响应，标题为“HALCON VM 工业视觉平台”。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证。S29 记为新 Git 批次第 `4/10` 次，不提交或推送。

## 已完成 Sprint：S28 VM 式图像内测量几何交互 + HALCON 线性多卡尺阵列

状态：**已验证实现**

### A 线完成

1. 图像区新增“卡尺域”入口和编辑条；选择单卡尺模块或其 XLD 图层后同步真实模块输入图像、流程选择和右侧常驻配置，并显示旋转矩形、中心、Length1/Length2 和旋转控制点。
2. 拖动只更新右侧参数草稿与实时 Row/Column/Phi/Length1/Length2；试运行不自动应用，应用后才写入工具参数、方案脏状态和 50 项撤销/重做，取消恢复原值。
3. HALCON bilinear 安全边界、最小半长和 `Length1 >= 2*Sigma` 受保护；重叠控制点改为最近距离命中。退出编辑、选择其他工具、删除、方案加载和历史恢复会释放预览并清除陈旧上下文。

### B 线完成

1. 右侧常驻“线性多卡尺阵列”接入工具箱、流程、Image 输入、参数、试运行、结果、帮助、图层、I/O、日志、配方和 NumericJudge。
2. 沿起止基线等距布置 2–200 个垂直卡尺，每个卡尺独立调用 HALCON `gen_measure_rectangle2 + measure_pairs + close_measure`；输出基线、测量矩形、边缘十字/连线组成的托管 XLD。
3. 输出 `MeanDistance/ValidCount/PairCount/MinDistance/MaxDistance/StdDev/Distances/ValidIndices/FailedIndices/FailureDetails/Parameters/ResultCode`；单项失败保留其他结果并定位索引，每个 handle 在成功、NG 和异常路径关闭。逐项集合当前仍为可读字符串，不冒充类型化数组。

### S28 验证结论

- `S28MeasurementDomainAndLinearCaliperArraySmoke.ps1` 通过：真实鼠标拖动只改草稿、取消、应用、撤销/重做和陈旧预览清理；水平 3/5/10、旋转 5 卡尺、局部缺边、全无边、低对比、越界、非法数量、输入释放、配方、NumericJudge、旧 XLD 释放和 25 次 handle 生命周期全部通过。
- S09-S28 与旧 v1 配方共 21 组烟测全回归；逻辑 1554×978 UI 确认编辑条、控制点上下文、单卡尺常驻参数、阵列流程节点与结果图层，阵列得到 `ValidCount=7`、`PairCount=14`。WPF 离屏截图不能捕获原生 HALCON HWindow 内容，但实际 HALCON 运行与绘制未抛异常。
- Debug/Release x64 均 0 警告 0 错误，双版本独立启动 8 秒保持运行。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证。S28 记为新 Git 批次第 `3/10` 次，不提交或推送。

## 已完成 Sprint：S27 VM 式多结果图层管理 + HALCON 卡尺边缘对测量

状态：**已验证实现**

### A 线完成

1. 右上图像侧栏新增常驻“结果图层”页，按本周期真实 Region/XLD 输出显示来源工具/端口、类型、颜色、对象数、显示顺序和等待/有效/失效状态。
2. 提供显隐、独显、全显、全隐、上移、下移、定位生产者和打开配置；绘制只使用托管快照临时副本，显示操作不释放输出、不改变真实 `InputBindings`、执行顺序或算法结果。
3. 模块重跑、删除、上游释放、配方切换和历史恢复后图层安全刷新；显示顺序和显隐状态可在同一运行上下文保持。Shape 兼容叠加尚未进入逐层清单，不冒充完整图层系统。

### B 线完成

1. 右侧常驻“卡尺边缘对测量”接入工具箱、流程、Image 输入、参数、试运行、结果、帮助、图层、I/O、日志、配方和 NumericJudge。
2. 使用 HALCON 20.11 `gen_measure_rectangle2 + measure_pairs + close_measure`，配置 Row/Column/Phi/Length1/Length2/Sigma/Threshold/Transition/Select；输出测量矩形、全部边缘十字、成对连线、第一/第二边缘坐标与幅值、对内/对间距离、PairCount、PrimaryDistance、参数和结果码。
3. 颜色图像按固定 `rgb1_to_gray` 进入测量；measure handle 在成功、无边缘对、非法输入和异常路径均关闭。成组坐标/幅值/距离当前作为可读字符串输出，类型化数组端口尚未实现。

### S27 验证结论

- `S27ResultLayersAndCaliperSmoke.ps1` 通过：正/负/全部极性、First/Last、90°旋转、多边缘对、缺边、低对比、越界、非法半长/半宽/Sigma/阈值/极性/选择方式、上游未运行/释放、配方、NumericJudge、25 次重跑 handle 生命周期及图层显隐/隔离/排序/失效/恢复全部通过。
- S09-S27 与旧 v1 配方共 20 组烟测全回归；物理 1554×978 UI 确认结果图层、卡尺常驻配置、`Image -> Caliper -> PrimaryDistance Judge` 和真实叠加，得到 `PairCount=2`、`PrimaryDistance=66 px`。WPF 离屏截图无法捕获原生 HALCON HWindow 内容，但实际运行与绘制未抛异常，此限制已明确记录。
- Debug/Release x64 均 0 警告 0 错误，Debug 独立启动 5 秒保持运行。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证。S27 记为新 Git 批次第 `2/10` 次，不提交或推送。

## 已完成 Sprint：S26 VM 式调试监视工作台 + HALCON 双直线关系测量

状态：**已验证实现**

### A 线完成

1. 中央流程区新增常驻“调试监视”，真实显示断点清单、当前/下一模块、执行范围、暂停原因和所选模块本周期输入/输出、结果码、状态、耗时、错误。
2. 可从监视区定位节点、打开第一个输入/完整 I/O、切换断点、清除全部断点；“运行到所选”复用确定性 Full 执行计划，执行完目标启用模块后停在下一启用模块前，较早断点仍可优先中断。
3. 调试运行目标只保存在运行态，不进入配方；工具删除、方案切换、停止/继续和结果失效会同步刷新。高频流程工具条已收敛为运行/调试/视图入口，为画布和监视区释放空间。该工作台是依据 VM 调试/运行习惯的产品化推导，不宣称复刻海康内部调试器。

### B 线完成

1. 右侧常驻“双直线关系测量”接入工具箱、流程、输入、参数、试运行、结果、帮助、叠加、I/O、日志、配方和 NumericJudge；两路 `LineA/LineB` 必须来自不同、合法前序 XLD。
2. 使用 HALCON 20.11 `fit_line_contour_xld` 拟合各输入最长直线，再以 `angle_ll`、`intersection_lines`、`distance_pl` 计算关系，并用 `gen_cross_contour_xld + concat_obj` 生成两线/交点托管 XLD 叠加。
3. 输出 `IntersectionRow/Column/Valid`、`DirectionA/B`、`SignedAngle`、`AcuteAngle`、`ParallelDistance`、`IsParallel`、`IsCoincident`、`Relation`、两路残差、参数和结果码；空/短/弧形、同源、类型/顺序、非法容差、未运行/释放对象均明确拒绝。

### S26 验证结论

- `S26DebugMonitorAndXldLineRelationSmoke.ps1` 通过：相交/垂直、平行距离 `15 px`、近重合、Tukey 含噪离群点、空/单点/非直线、非法算法/容差、双输入约束、上游状态、配方、重跑释放、断点列表/切换、运行到所选和继续全部通过。
- S09-S26 与旧 v1 配方共 19 组烟测全回归；物理 1554×978 UI 确认常驻监视、真实当前/下一/范围/暂停原因/I/O 快照、精简流程工具条及双直线配置/叠加，锐角 `65.772°`、交点约 `(210,349)`。
- Debug/Release x64 均 0 警告 0 错误，双版本启动保持响应，402 个 XAML 事件引用对应 194 个去重处理方法且无缺失；差异、敏感信息和构建产物门禁通过。真实 Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证。S26 记为新 Git 批次第 `1/10` 次，不提交或推送。

## 已完成 Sprint：S25 VM 式断点/单步调试 + HALCON XLD 旋转矩形拟合测量

状态：**已验证实现**

### A 线完成

1. 运行菜单、顶部/流程工具条、连接画布、节点和顺序列表右键均提供断点、清除断点、单步和继续；F9/F10/F5 可直接操作。
2. 断点在启用模块执行前命中；节点/列表显示红色断点和橙色下一执行指针，组合摘要、流程状态和底部状态显示断点数、暂停原因与下一模块。
3. `IsBreakpoint` 作为可选工具配方字段保存并进入 50 项撤销/重做；运行指针不持久化。单步只执行一个启用模块，继续/停止/超时/遇 NG 停止和确定性顺序保持兼容，停用断点被跳过。

### B 线完成

1. 右侧常驻“XLD 旋转矩形拟合测量”已接入工具箱、流程、基本/运行/输入/结果/帮助、试运行、叠加、I/O、日志、配方和数值判定；HALCON 20.11 `fit_rectangle2_contour_xld` 精确 .NET 签名经官方文档和本机程序集确认。
2. 支持 regression、Huber、Tukey，以及最大采样点、最大闭合距离、端点裁剪、迭代和裁剪因子；输出托管 `XLD/RectangleCount/CenterRow/CenterColumn/Phi/Length1/Length2/Corner1..4/PointOrder/OrientationState/MaxResidual/Algorithm/Parameters/ResultCode`。
3. 每条轮廓至少 8 点且需覆盖四边内部；空对象返回托管 NG，短/直线/病态轮廓和非法参数明确拒绝。近正方形保持 OK 并提示“90°方向等价”，重跑会释放旧拟合 XLD。

### S25 验证结论

- `S25FlowDebugAndXldRectangleFittingSmoke.ps1` 通过：断点前暂停、单步一个、继续、停用跳过、配方回读、撤销/重做及指针清理均通过；旋转矩形三算法得到中心 `(120,180)`、方向约 `31.513°`、半边长 `60/25 px`，近正方形、空/短/直线退化、六类非法参数、参数回读和 XLD 生命周期通过。
- S09-S25 与旧 v1 配方共 18 组烟测全回归；物理 1554×978 UI 确认顶部/流程工具条、断点/单步/继续、红色断点、橙色下一执行指针、旋转矩形常驻配置和 `Image -> Region -> XLD -> Rectangle -> Length1 Judge` 链，输出半边长 `140/70.003 px`、方向 `25.783°`。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 4 秒保持响应。旧 `.jw/.handle`、真实 Shape 模板和相机设备因缺样本/设备未验证；BSB 按用户指令未实施/未验证。S25 使上一批达到 `10/10` 并创建本地批次提交，该提交已于 2026-08-08 随 S26-S35 批次成功上传 GitHub。

## 已完成 Sprint：S24 VM 式流程组合/折叠 + HALCON XLD 椭圆拟合测量

状态：**已验证实现**

### A 线完成

1. 编辑菜单、流程工具条、连接画布和节点右键已提供创建组合、解组、折叠、展开和重命名；橙色组合容器显示名称、成员摘要、边界 IN/OUT、内部连接、确定性执行范围和异常数。
2. `GroupId/GroupName/IsGroupCollapsed` 进入工具配方项并兼容旧配方；创建、重命名、折叠、展开和解组进入 S23 撤销/重做，配方重载重建组合并释放旧托管输出。
3. 安全折叠只压缩成员端口卡，所有真实端口、连接曲线、上游/下游语义和 `flowTools` 顺序保持可见且不变；完整组复制映射新 GroupId，部分复制自动解组，删除到单成员自动清理。

### B 线完成

1. 右侧常驻“XLD 椭圆拟合测量”已接入工具箱、流程、基本/运行/输入/结果/帮助、试运行、图形、I/O、日志、配方和数值判定；HALCON 20.11 `fit_ellipse_contour_xld` 精确 .NET 签名经官方文档和本机程序集确认。
2. 支持 fitzgibbon、fhuber、ftukey、focpoints、fphuber、fptukey、geometric、geohuber、geotukey、voss，及最大采样点、最大闭合距离、端点裁剪、Voss 表、迭代和裁剪因子；输出托管 `XLD/EllipseCount/CenterRow/CenterColumn/Phi/Radius1/Radius2/StartPhi/EndPhi/PointOrder/MaxResidual/Algorithm/Parameters/ResultCode`。
3. 主椭圆按面积与弧长组合选择，实际几何残差进入输出；少于 5 点、直线/圆、近圆、极扁和超大半轴明确拒绝，空对象返回托管 NG，运行快照所有权和下游失效闭环。

### S24 验证结论

- `S24FlowGroupsAndXldEllipseFittingSmoke.ps1` 通过：整椭圆 `(80,90,Phi=0.45,Radius1=40,Radius2=20)`、开弧、十算法、FTukey 离群点、圆/直线退化、空/短对象、七类非法参数、配方保存回读、组合全生命周期、撤销/重做及运行 XLD 释放均通过。
- S09-S24 与旧 v1 配方全部回归；物理 1554×978 UI 截图确认组合容器、边界端口、安全折叠、七参数常驻配置及 `Image -> Region -> XLD -> Ellipse -> Radius1 Judge` 链，输出 `Radius1≈140.021 px / Radius2≈74.954 px`。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 6 秒保持响应。BSB 按用户指令未实施/未验证。S24 记为当前 Git 批次第 `9/10` 次，不提交或推送。

## 已完成 Sprint：S23 流程编辑撤销/重做 + HALCON XLD 圆拟合测量

状态：**已验证实现**

### A 线完成

1. 编辑菜单、流程工具条和 `Ctrl+Z/Ctrl+Y/Ctrl+Shift+Z` 已提供真实撤销/重做、可用状态、`历史 撤销数/重做数` 与最近动作说明。
2. 50 项有界历史只保存 `ToolFlowRecipeItem` 纯 DTO，覆盖模块添加/粘贴、参数应用、启停/删除、顺序、连接创建/替换/清除/修复、节点移动/对齐/分布/恢复自动布局；不保存 HImage/HRegion/XLD、耗时或结果。
3. 恢复会释放旧工具和托管运行输出、重建真实 `flowTools/InputBindings`、保留 ToolId/选择并让模块等待重跑；新编辑清空重做分支，打开/新建/恢复方案重置基线，执行仍为确定性线性顺序。

### B 线完成

1. 右侧常驻“XLD 圆拟合测量”已接入工具箱、流程、基本/运行/输入/结果/帮助、试运行、图形、I/O、日志、配方和数值判定；HALCON 20.11 `fit_circle_contour_xld` 精确 .NET 签名经官方文档和本机程序集确认。
2. 支持 algebraic、ahuber、atukey、geometric、geohuber、geotukey、最大采样点、最大闭合距离、端点裁剪、迭代和裁剪因子；输出托管拟合 `XLD/CircleCount/CenterRow/CenterColumn/Radius/StartPhi/EndPhi/PointOrder/MaxResidual/Algorithm/Parameters/ResultCode`。
3. 主圆按拟合弧长选择；实际径向残差进入输出。直线段、少于 3 点、无效/超大半径退化轮廓明确拒绝，空对象返回托管 NG，运行快照所有权和下游失效闭环。

### S23 验证结论

- `S23FlowUndoRedoAndXldCircleFittingSmoke.ps1` 通过：整圆 `(80,90,r=25)`、开弧 `(100,150,r=40)`、六算法、空/短/非圆/退化、非法参数、配方保存回读、撤销/重做、重做分支清除、方案基线和旧 XLD 释放均通过。
- S09-S22 与旧 v1 配方全部回归；物理 1554×978 UI 截图确认菜单/工具条撤销/重做、`历史 1/0`、五模块真实链、圆拟合常驻参数和结果表可见，圆件半径 `109.997 px`、Radius 判定 OK。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 5 秒保持响应，343 个 XAML 事件引用对应 183 个去重处理方法且无缺失；`git diff --check` 与构建产物/敏感信息门禁通过。BSB 按用户指令未实施/未验证。S23 记为当前 Git 批次第 `8/10` 次，不提交或推送。

## 已完成 Sprint：S22 网格节点定位/布局持久化 + HALCON XLD 直线拟合测量

状态：**已验证实现**

### A 线完成

1. 默认连接画布支持模块卡拖动、可见 24 像素网格吸附、水平/垂直对齐、横向/纵向等距，并显示“自动·按依赖”或“手动·已入配方”状态；恢复自动布局可清除手动坐标。
2. 手动坐标以可选 `GraphX/GraphY` 写入每个工具配方项，保存/应用后恢复；旧配方或自动模式不写坐标并继续依赖分层布局。复制模块在手动模式偏移放置，画布拖入使用放开位置。
3. 移动、对齐、分布和恢复均不改变 `flowTools` 顺序、真实 `InputBindings` 或启用状态；Esc 取消移动恢复原坐标，与端口拖线、空白框选、中键平移和右键菜单分离。

### B 线完成

1. 右侧常驻“XLD 直线拟合测量”已接入工具箱、流程、参数、输入、试运行、结果、图形、I/O、日志和配方；HALCON 20.11 `fit_line_contour_xld` 精确 .NET 签名经官方文档和本机反射确认。
2. 支持 regression、Huber、Tukey、drop、Gauss、最大采样点、端点裁剪、迭代和裁剪因子；输出托管拟合 XLD、线数、总长度、平均无向角、实际最大点线残差、主线四端点、算法、参数和结果码，并可驱动 NumericJudge。
3. 直线 XLD 进入右上青色叠加；弧/椭圆弧按 `cont_approx` 明确拒绝，空对象返回托管 NG，短对象、离群点、非法参数、类型/顺序、未运行/释放、重跑失效和对象所有权均闭环。

### S22 验证结论

- `S22NodeLayoutAndXldLineFittingSmoke.ps1` 通过；100×80 矩形链拟合 `4` 条线、总长度约 `244`，合成双线含离群点时 Tukey 平均角接近 `0°` 且最大残差大于 `20 px`，五种算法均通过。
- S09-S22 共 14 组专项与旧 v1 配方全部回归；物理 1943×1222 手动/自动双截图确认网格、布局状态、六模块真实链、拟合 XLD 叠加和常驻配置可见。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 5 秒保持运行，181 个去重 XAML 事件处理方法无缺失。
- BSB 按用户指令未实施/未验证；S22 记为当前 Git 批次第 `7/10` 次，不提交或推送。

## 已完成 Sprint：S21 真实端口拖拽连线/重连 + HALCON XLD 轮廓分段

状态：**已验证实现**

### A 线完成

1. 真实 OUT→IN 拖拽和已连接 IN 重连已进入默认连接画布，拖动中显示临时曲线、来源/目标、数据类型及绿色可连接/红色拒绝反馈。
2. 成功放开创建或替换真实 `InputBindings`，失效目标及下游、标记配方脏状态并选中真实连接；类型、顺序、启用、端口支持和双输入不同来源校验复用现有契约。
3. Esc、空白、非法放开和取消均保持原绑定，连接删除仍操作真实订阅；与 S20 框选/Ctrl 多选、中键平移、右键菜单和端口导航路径已验证不冲突。

### B 线完成

1. 右侧常驻“XLD 轮廓分段”已接入工具箱、流程、参数、输入、试运行、结果、图形、I/O、日志与配方，支持 `lines/lines_circles/lines_ellipses`、平滑点数和两项最大距离。
2. `segment_contours_xld` 输出托管 `XLD/TotalLength/SegmentCount/LineCount/ArcCount/Mode/Parameters/ResultCode`；矩形外轮廓稳定得到 `4` 条线段、`0` 条弧和约 `244` 总长度，并驱动 NumericJudge。
3. 空/短 XLD、非法模式/平滑/距离、类型/顺序、上游未运行/释放、重跑失效、对象释放和配方往返均已验证。

### S21 验证结论

- `S21PortDragAndXldSegmentationSmoke.ps1` 通过，S09-S20 与旧 v1 配方全部回归。
- 物理 1943×1222 界面截图确认连接画布、拖拽状态、真实 XLD 链、分段叠加和常驻配置；Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒保持运行，179 个去重 XAML 事件处理方法无缺失。
- BSB 按用户指令未实施/未验证；S21 记为当前 Git 批次第 `6/10` 次，不提交或推送。

## 已完成 Sprint：S20 连接画布框选/批量操作 + HALCON XLD 轮廓筛选

状态：**已验证实现**

### A 线完成

1. 连接画布支持可见蓝色虚线橡皮框、Ctrl 增选/取消、`已选 n/总数` 和单击空白清除，并与顺序列表 `SelectedItems` 同步；最后操作模块保持为右侧配置主目标。
2. 编辑菜单、流程工具条及画布/列表右键菜单的复制、创建副本、启用、停用和删除支持真实多选；副本继续执行新 ToolId/唯一名称/合法连接重映射，批量删除会清理消费者连接、失效下游和释放托管输出。
3. 框选与中键平移、右键菜单、端口/连接点击分离；本轮仍只宣称确定性线性调度，不宣称 Group、并行、用户拖拽连线、撤销栈或多流程。

### B 线完成

1. 新增右侧常驻 HALCON“XLD 轮廓筛选”，真实订阅前序 XLD，按 `select_contours_xld` 支持 `contour_length` 和 `maximum_extent` 上下限。
2. 输出托管 `XLD/TotalLength/ContourCount/Feature/Range/ResultCode`，可继续驱动 NumericJudge、I/O、结果、日志和右上青色图形叠加；XLD 获取使用安全副本，重跑、删除、配方重载和关闭时释放。
3. 未运行、类型、顺序、释放、空 XLD、非法特征/范围、重跑失效、配方保存回读和生命周期均已闭环。

### 验证证据

- 合成带孔 Region 得到 2 条 XLD 后，`contour_length=100..300` 与 `maximum_extent=50..100` 均稳定筛为 `1 条/206`，NumericJudge 订阅 `TotalLength` 为 OK；空 XLD 为托管 `NG/0/0`。
- 框选跨分支 3 个模块、Ctrl 基线增选、画布/顺序列表状态同步、批量启停、复制、创建 3 个副本及批量删除/消费者清理/下游快照释放均通过自动化；物理屏幕 1943×1222 双截图显示蓝色橡皮框、3 个选中节点、`已选 3/6`、批量入口和右侧 XLD 参数。
- S09-S19 与旧 v1 配方全回归；Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒保持响应，177 个去重 XAML 事件入口无缺失。
- BSB 按用户指令继续未实施/未验证；S20 记为当前 Git 批次第 `5/10` 次，不提交/推送。

## 已完成 Sprint：S19 模块复制/粘贴工作流 + HALCON Region 轮廓测量

状态：**已验证实现**

### A 线完成：模块复制、粘贴与创建副本

1. 在连接画布、顺序列表、右键菜单和流程操作区提供清晰可见的复制、粘贴与创建副本入口，并给出剪贴板状态和插入位置反馈。
2. 复制实例类型、参数、启用状态、ROI 绑定及稳定配置字段；粘贴必须生成新 ToolId 和不冲突实例名，不复制运行时 HImage/HRegion/XLD、结果、耗时或错误。
3. 对输入订阅制定并实现可验证规则：仍合法的外部前序来源可保留，指向被复制集合内部的来源要重映射；不满足顺序/类型/启用约束的连接明确失效或清除，不静默制造错误执行语义。
4. 副本加入确定性线性顺序并即时刷新连接画布、端口、配置、图像上下文、配方脏状态和下游失效；不冒充 Group、并行或多流程复制。

### B 线完成：HALCON Region 轮廓/XLD 测量

1. 正式实现前核对 HALCON 20.11 `gen_contour_region_xld` 与 `length_xld` 精确签名和空对象语义，新增“Region 轮廓测量”工具。
2. 从右侧常驻配置订阅一个前序 Region，选择轮廓模式并输出可安全持有/复制/释放的 `XLD/TotalLength/ContourCount/Mode/ResultCode`；XLD 进入图像叠加、I/O、结果和日志，Number 可供数值判定订阅。
3. 空 Region、上游未运行/NG/释放、非法模式、重跑/删除失效、配方保存回读和 XLD 生命周期形成闭环；不得只保存一个内部 HObject 而没有可见轮廓和用户结果。
4. 不改变既有 Region/Edge 算法语义，不触碰相机 SDK、投产协议、`.jw/.handle` 或 BSB。

### 验证证据

- 物理屏幕 1943×1222（WPF 逻辑窗口 1554×978）连接画布/顺序列表截图确认编辑菜单、流程工具条、右键菜单、复制/粘贴/创建副本、3 模块剪贴板状态、安全副本名称和右侧轮廓模式可见；顺序列表支持多选，画布右键可指定粘贴位置，中键负责平移；真实屏幕采集同时确认嵌入式 HALCON 图像和青色 XLD 外轮廓/孔轮廓可见。
- 三模块专项确认新 ToolId/唯一名称、参数、启用状态、现存 ROI、合法外部连接和块内来源重映射；重复粘贴无重名，运行结果/XLD 快照不复制，无效连接清除并给出数量反馈。
- 合成带孔 Region：`border=1 条/206`、`center=1 条`、`border_holes=2 条/276`；托管 XLD 可复制/释放，`TotalLength` 真实驱动数值判定；空 Region、未运行、类型、顺序、释放、非法模式、配方、失效和生命周期通过。
- 旧 v1 配方与 S09-S18 全量回归通过；Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒保持响应，170 个去重 XAML 事件入口无缺失，`git diff --check`、敏感信息和构建产物扫描通过。
- BSB 按用户指令继续未实施/未验证。
- S19 完成后记为当前 Git 批次第 `4/10` 次，不提前提交或推送。

## 已完成 Sprint：S18 端口级订阅导航 + HALCON 双图像差分

状态：**已验证实现**

### A 线完成

- 连接画布和顺序列表的 IN/OUT 端口可点击，选中端口与模块、连接状态同步高亮。
- 输入端口常驻面板显示当前订阅、数据类型、状态、本周期值和所有兼容前序输出，可直接替换/清除；输出端口列出全部真实消费者并可定位对应连接和模块。
- 替换写回真实 `InputBindings`，数值判定同步兼容字段；类型、顺序、启用、必需端口和双输入不同来源校验继续生效，失败会回滚原连接。

### B 线完成

- 新增右侧常驻 HALCON“双图像差分”，显式订阅 `ImageA`/`ImageB` 两路不同、同尺寸、同通道 Image。
- 支持 `abs_diff_image` 绝对差分、`sub_image` A-B 与 B-A；产品化参数为 `0 < Mult <= 100`，方向减法 Add 为 `-65535..65535`。
- 输出托管 `Image/Width/Height/Channels/Mode/Parameters/ResultCode`，进入图像上下文、I/O、结果、日志、帮助和 v2 配方，并真实驱动 Threshold、DynamicThreshold、Blob、Gray 和 Edge。

### 验证证据

- 端口当前来源、兼容候选、替换、清除、重复双输入回滚、缺失状态、5 个消费者和连接定位专项通过；物理 1554×978 输入订阅、消费者和差分参数三截图可见。
- 合成像素：绝对差为 `40/120`，A-B 为 `30/70`，B-A 为 `80/40`；Threshold 面积 `400`、Gray 均值 `120`，DynamicThreshold/Blob/Edge 均完成真实下游执行。
- 未运行、同源、尺寸/通道不匹配、非法 Mult、已释放快照、重跑失效、配方和 HImage 生命周期通过；S09-S17 与旧 v1 配方全部回归。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒保持响应，137 个去重 XAML 事件入口无缺失；BSB 按指令未实施/未验证。S18 记为当前 Git 批次第 `3/10` 次，不提交/推送。

## 已完成 Sprint：S17 连接检查/导航 + HALCON 动态阈值分割

状态：**已验证实现**

### A 线完成

- 二维画布真实连接路径可选择，选中路径、来源端口和目标端口同时高亮。
- 画布内常驻“连接检查器”显示来源/目标模块与端口、数据类型、本周期值、连接状态和错误原因。
- “定位来源”“定位目标”“打开目标输入”“适应选中依赖”“清除连接”“自动修复”均作用于真实 `InputBindings`；自动修复选择最近的兼容前序输出并避免双输入同源。

### B 线完成

- 新增右侧常驻 HALCON“动态阈值分割”，显式订阅 `OriginalImage` 与 `ReferenceImage` 两路不同、同尺寸的单通道 Image。
- 按 HALCON 20.11 `dyn_threshold` 官方语义支持 Light/Dark，并以产品化 `1 <= Offset < 255` 范围保护极性含义和低偏移噪声风险。
- 输出托管 `Region/Area/RegionCount/Polarity/Offset/ResultCode`，进入图像叠加、I/O、结果、日志、帮助、配方，并真实驱动形态学、特征筛选、Blob、Gray 和 Edge。

### 验证证据

- 合成单通道夹具的 Light/Dark 结果均为 `Area=400 / RegionCount=1`；缺一路、同源、顺序/类型、上游未运行/NG/释放、尺寸/通道不匹配、非法偏移、空结果、重跑失效、配方和 HImage/HRegion 生命周期通过。
- S09-S16 与旧 v1 配方 `90/245/120 -> Schema 2` 全部回归；连接检查器、双 Image 汇聚、目标输入页和动态阈值参数在物理 1554×978 双截图中可见。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒保持响应，144 个 XAML 事件入口无缺失；`git diff --check`、敏感信息和构建产物门禁通过。
- BSB 按用户指令未实施/未验证；相机、真实 Shape 模板和 `.jw/.handle` 缺少设备/样本，未验证。S17 记为当前 Git 批次第 `2/10` 次，不提交/推送。

## 已完成 Sprint：S16 真实连接画布首版 + HALCON 图像增强

状态：**已验证实现**

### A 线：真实连接画布与依赖分层

1. 中央默认显示二维“连接画布”，真实 `InputBindings` 生成有方向的 Image/Region/Number 曲线、端口标签和消费者关系，等待/有值/错误分别以状态和虚线表达。
2. 串联、分支和汇聚按前序依赖自动分层，选中模块同步高亮直接上游/下游及相关路径；连接画布/顺序列表可切换，并支持缩放、平移（S19 起改为中键）、适应、自动布局和工具箱拖入。
3. 添加、删除、重排、重命名、绑定修改、上游重跑和配方重载均基于现有执行/绑定状态重算，不持久化脱离语义的 Node/Edge。

### B 线：HALCON 图像增强

1. 模块箱和右侧常驻配置新增“图像增强”，支持线性灰度缩放 `scale_image` 和局部对比度增强 `emphasize`，模式相关参数会禁用无关输入并校验范围。
2. 工具订阅系统或前序 Image，输出托管 `Image/Width/Height/Channels/Mode/Parameters/ResultCode`，结果进入模块图像、I/O、记录、日志、帮助和 v2 配方。
3. Threshold、Blob、Gray、Edge 可订阅增强图；未运行/NG/释放、非法参数、RGB、重跑失效和对象释放有专项闭环。

### 验证证据

- 专项烟测确认 8 条真实分支/汇聚路径、依赖层、类型颜色、选中高亮、视图切换、适应和缺失来源虚线；物理 1554×978 UI 截图确认连接画布、右侧增强模块和结果记录。
- `scale_image` 灰度像素 `20→50`、`110→230`，RGB `10/20/30→25/45/65`；`emphasize`、Threshold/Blob/Gray/Edge、常驻草稿/校验/应用、配方和生命周期通过。
- S09-S15 与旧 v1 配方 `90/245/120 -> Schema 2` 全部回归；Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒响应，146 个 XAML 事件入口无缺失，敏感信息/构建产物和 `git diff --check` 通过。
- BSB 按用户指令未实施/未验证；相机、真实 Shape 模板和 `.jw/.handle` 缺少设备/样本，未验证。S16 记为当前 Git 批次第 `1/10` 次，不提交/推送。

## 已完成 Sprint：S15 流程依赖可视化 + HALCON Region 集合运算

状态：**已验证实现**

### A 线：真实端口与直接依赖可视化

1. 中央流程卡显示来自工具元数据和实际绑定的 IN/OUT 端口、数据类型、来源、当前值、消费者以及缺失/等待/异常/有值状态。
2. 选中模块时蓝色标记当前模块、紫色标记直接上游、青色标记直接下游；Region 集合运算卡同时显示 `RegionA` 与 `RegionB` 两路真实汇聚。
3. 绑定修改、上游重跑、删除、顺序错误和输出失效会即时刷新端口状态；未建立脱离执行器的自由连线，执行仍明确为确定性线性顺序。

### B 线：HALCON Region 集合运算

1. 模块箱和右侧常驻配置新增“Region 集合运算”，两个必需 Region 输入要求不同的前序 Region 端口，支持并集、交集、差集 A-B 和对称差。
2. 运行调用 HALCON `union2/intersection/difference/symm_difference`，输出托管 `Region/Area/RegionCount/Operation/ResultCode`，同一结果进入黄色叠加、记录、日志、I/O 和配方。
3. Region 特征筛选、Gray 和 Edge 可订阅输出；缺一路、同源、类型/顺序、未运行、空结果、删除、失效和快照释放均有真实校验。

### 验证证据

- 合成双矩形的并集/交集/差集/对称差面积为 `2800/400/1200/2400 px²`；空交集发布托管空 Region 与 NG，下游 Feature/Gray/Edge 运行通过。
- 端口来源、消费者、上下游高亮、等待/有值/失效、删除和配方往返专项烟测通过；S09-S14 与旧 v1 配方全部回归。
- Debug/Release x64 均 0 警告 0 错误，双版本启动 8 秒响应；物理 1600×930（1942×1163 像素）截图确认双输入汇聚、常驻集合配置、关联图像和结果叠加。
- BSB 按用户指令未实施/未验证；相机、真实 Shape 模板和 `.jw/.handle` 缺少设备/样本，未验证。S15 为上一批第 `10/10` 次，按规则统一提交推送。

## 已完成 Sprint：S14 高级 ROI 编辑 + HALCON Region 特征筛选

状态：**已验证实现**

### A 线：旋转矩形与多边形 ROI 精细编辑

1. 图像工具栏与右侧 ROI 页新增旋转矩形入口；旋转矩形具有绿色中心/轴端和橙色旋转点，可移动、旋转、调整宽高并按实际四角约束图像边界和最小半长。
2. 多边形具有逐顶点控制点和编号，可整体移动或拖动单点；相邻点过近、面积过小、自相交、端点/共线重叠会拒绝本次几何更新并保留上一有效形状。
3. 两类 ROI 均进入选择、复制、独立 RoiId、绑定继承、锁定/删除保护、中文几何摘要、图层/右侧提示和 v2 配方往返；旧 Rectangle/Circle/Polygon 枚举值和旧配方读取保持兼容。
4. HALCON 嵌入窗口无法使用 `disp_text` 时标签安全降级，控制点仍可见，图像刷新不会因窗口类型异常中断。

### B 线：HALCON Region 特征筛选工具

1. 模块箱新增“Region 特征筛选”，右侧常驻配置可选择面积、外接框宽度、外接框高度或圆度并设置上下限；输入页必须订阅前序 Region。
2. 运行使用 HALCON `connection + select_shape`，输出托管 `Region/Area/RegionCount/Feature/Range/ResultCode`，同一结果进入黄色图像叠加、结果记录、日志、I/O、流程状态和配方。
3. Gray/Edge 可继续订阅筛选 Region；顺序/类型、上游未运行/NG、非法范围/圆度、释放快照、重跑/删除导致的下游失效和关闭释放均进入闭环。

### 验证证据

- 旋转矩形控制点命中、旋转到 90°、宽度调整、越界移动裁剪、复制和配方通过；多边形顶点命中/移动、非法几何拒绝、锁定和配方通过。
- 合成三目标链中，面积筛选保留 `396 px²`，宽度筛选保留 `796 px²` 横向件，高度筛选保留 `1196 px²` 纵向件，圆度筛选保留 `396 px²` 方件；Gray/Edge 成功订阅输出。
- 旧 v1 配方、S09-S13、异常和对象生命周期回归通过；134 个 XAML 事件入口无缺失；Debug/Release x64 均 0 警告 0 错误且启动 8 秒响应。
- 物理 1600×930（1942×1163 像素）旋转矩形/多边形双截图确认右侧特征配置、传递式图像来源、黄色结果 Region 与 ROI 控制点同时可见。
- BSB 按用户指令未实施/未验证；Shape 模板、相机 SDK、`.jw/.handle` 因无真实样本/设备仍未验证。Git 记为第 `9/10` 次，本轮不提交、不推送。

## 已完成 Sprint：S13 图像调试区可视化 + HALCON Region 形态学

状态：**已验证实现**

### A 线：图像调试有效面积与叠加管理

1. 右上图像区默认比例由 `2*:3*` 调整为 `3*:2*`，图像工作区可在主工作区内放大并原位恢复；中央流程、右侧配置和底部历史布局状态在恢复后保持一致。
2. ROI 与算法结果具备独立显隐控制；隐藏只影响绘制，不释放 HALCON 输出快照、不删除 ROI 或破坏工具绑定。
3. 单行图例显示真实结果生产模块/端口、颜色、对象数、Region 面积和结果状态；边缘轮廓不再误报 Region 面积，来源被删除或上游结果失效时明确提示。
4. 无可用图像时以可操作空状态替代空白 WinForms 宿主；全局/模块输入/模块输出、适应/1:1、像素值和关联图像上下文保持可用。

### B 线：HALCON Region 形态学工具

1. 新增“Region 形态学”工具及常驻配置，订阅前序 Region，支持 HALCON 圆形开运算、闭运算、膨胀和腐蚀，半径范围为 `0.5..100 px`。
2. 统一连接后输出托管 `Region/Area/RegionCount/Operation/ResultCode`，流程、I/O、结果、日志和黄色叠加使用同一真实快照；Gray/Edge 可继续订阅其 Region。
3. 顺序/类型、上游未运行/NG、非法半径、释放快照、重跑/删除导致的下游失效、配方保存回读和 HALCON 对象释放进入闭环。

### 验证证据

- 合成链阈值面积 `2657`；开/闭/膨胀/腐蚀面积分别为 `2657/2665/3321/1993`，Gray/Edge 订阅形态学 Region 通过。
- S09-S12、旧 v1 配方 `90/245/120 -> Schema 2`、异常与生命周期回归通过；118 个 XAML 入口无缺失。
- Debug/Release x64 均为 0 警告 0 错误，Release 保持启动 8 秒；物理 1600×930（1942×1163 像素）普通/放大双截图通过，图像宿主高度 `106 -> 357`。
- BSB 按用户指令未实施/未验证；相机 SDK、投产协议、坐标约定和 `.jw/.handle` 未触碰。

## 已完成 Sprint：S12 VM 模块箱效率化 + HALCON 阈值分割

状态：**已验证实现**

### A 线：模块箱高频搭建工作流

1. 左侧平铺列表已升级为按分类折叠的模块箱，保留名称/分类/说明、搜索和双击，并增加全部/收藏/最近三种可见入口及空筛选提示。
2. 收藏与最近使用保存在本机 `ui-state.json`，不进入业务配方；收藏切换与最近顺序即时反馈。
3. 模块可拖入中央流程，拖动时显示“第 N 步之前/流程末尾”的确定性插入提示；插入真实改变 `ObservableCollection` 顺序和序号，仍明确为线性执行。

### B 线：HALCON 阈值分割工具

1. “阈值分割”可从常驻配置订阅前序 Image，配置 `0..255` 灰度上下限；彩色输入固定使用 HALCON `rgb1_to_gray` 后执行 `threshold`。
2. 输出托管 `Region/Area/RegionCount/Threshold/ResultCode`，同一结果进入黄色图像叠加、流程卡、I/O、结果记录、日志和帮助；Gray/Edge 可订阅 Region。
3. 参数和输入绑定进入 v2 配方；顺序/类型、未运行/NG 上游、非法范围、已释放快照、上游重跑、配方重载和关闭均沿用真实校验与释放链。

### 验证证据

- 指定第 2 步插入、序号刷新、收藏/最近本机往返通过；1600×930 物理界面同时显示分类折叠、三种筛选、流程、阈值配置、Region 叠加与结果。
- 120×80 RGB 两目标经本地图像源、3×3 均值滤波和阈值分割得到 `Area=1792 / RegionCount=2 / rgb1_to_gray`；Gray/Edge 均成功订阅 Region。
- 空区域输出 `NG/Area=0/RegionCount=0`；非法范围、错误顺序/类型、上游未运行、释放快照、下游失效、配方参数/连接和流程释放通过。
- S09-S11、旧 v1 配方 `90/245/120 -> Schema 2`、Debug/Release x64 0 警告 0 错误和 Debug 启动 8 秒通过。
- 相机 SDK、Shape 真实模板、`.jw/.handle`、自动换型、权限和 BSB 均缺少设备/样本或按指令未实施/未验证。

## 已完成 Sprint：S11 ROI 精细编辑 + HALCON 图像滤波

状态：**已验证实现**

### A 线：ROI 可选中精细编辑

1. 右上图像区的矩形和圆具有明确选中态与控制点；选择模式可拖动整体位置、调整矩形边/角和圆半径，几何保持图像边界与最小尺寸。
2. 顶部 ROI 工具栏、图层侧栏和右侧“图像/ROI”页均有复制与锁定入口；复制产生独立 RoiId、保留工具绑定并默认解锁，锁定后拒绝几何修改和删除。
3. 几何、锁定和绑定进入配方/脏状态；旧配方缺锁定字段时默认可编辑，Union/PerRoi 与既有坐标约定不变。
4. S11 验收时多边形控制点和旋转矩形尚未实现，已由 S14 补齐；屏蔽区、位置修正和多画面仍是未实现边界。

### B 线：HALCON 图像滤波工具

1. 模块箱新增“图像滤波”，从右侧常驻配置订阅前序 Image，支持 HALCON 均值滤波（奇数宽高 3..255）和中值滤波（圆形半径 1..100）。
2. 输出托管 `Image/Width/Height/Channels/Mode/Kernel/ResultCode`，右上可查看模块输出，下游图像通道、Blob、Gray、Edge 可继续订阅。
3. 草稿应用、撤销、试运行、输入绑定、参数校验、结果、日志、异常、配方和 HImage 生命周期全部进入现有闭环。

### 验证证据

- HALCON 9×9 冲击图：3×3 均值中心值 `28`，中值半径 1 为 `0`，RGB 滤波后保持 3 通道；偶数均值模板和负中值半径被拒绝。
- 连续链下游 Blob `1267`、Gray `45.047`、Edge `139.152`；上游重跑使滤波/通道/检测结果失效，旧快照与流程释放均通过。
- ROI 命中、移动、缩放、边界/最小尺寸、复制/绑定、锁定/删除保护和配方回读通过；旧 v1 配方保持 Blob `90/245/120` 并迁移到 Schema 2。
- S09/S10 回归、128 个 XAML 事件入口、Debug/Release x64 0 警告 0 错误、Release 启动 8 秒响应、物理 1580×920（1942×1150 像素）滤波页/ROI 页双截图通过。
- 相机 SDK、真实 Shape 模板、`.jw/.handle`、自动换型、权限和 BSB 均缺少设备/样本或按指令未实施/未验证。

## 已完成 Sprint：S10 停靠式模块配置 + HALCON 图像通道工具

状态：**已验证实现**

### A 线：主要参数去弹窗化

1. 右下新增常驻“模块配置”，包含基本参数、运行参数、输入、ROI/模板、结果显示和帮助；本地图像源、图像通道、Blob、灰度、边缘和数值判定均可直接编辑。
2. 流程双击、配置按钮和 I/O 参数入口直接定位当前实例；Shape/HDevelop 保留明确的兼容弹窗，旧“兼容参数”页从正常用户路径隐藏。
3. 实例草稿支持应用、撤销、试运行、未保存切换保护，并同步流程卡、I/O、图像上下文和配方脏状态。
4. 物理 1600×950 实拍确认中央流程、右上模块输出和右下配置页、处理模式及应用操作同时可见。

### B 线：HALCON 图像通道工具

1. “图像通道”可订阅前序 Image，以 HALCON 保持原图、转灰度或抽取指定通道，输出托管 `Image/InputChannels/OutputChannels/Width/Height/Mode/ResultCode`。
2. 下游 Blob/Gray/Edge 可继续订阅；非法通道、顺序/类型、上游状态、缺失/释放快照均有明确拒绝。
3. 模式、通道号和输入绑定进入 v2 配方；上游重跑、删除、配方重载和关闭释放旧 HImage 并使下游失效。

### 验证证据

- RGB 像素三通道 `20/100/200` 均可独立抽取；转灰得到 `87`，单通道转灰保持 1 通道；非法第 4 通道被明确拒绝。
- 下游 HALCON 结果为 Blob `4000`、Gray `92.1`、Edge `136.109`；停靠草稿应用/试运行恢复/撤销、图像上下文、配方回读和快照生命周期通过。
- S09 回归保持 Blob `400→800`、Gray `17→34`、Edge `76.109→116.109`、Region 均值 `255`、Number `OK→OK`。
- Debug/Release x64 0 警告 0 错误；Debug 启动 8 秒保持响应；MainWindow 101 个、兼容配置窗口 11 个事件入口无缺失；物理 1600×950 UI 通过。
- 相机 SDK、真实 Shape 模板、`.jw/.handle`、自动换型、权限和 BSB 均缺少设备/样本或按指令未实施/未验证。

## 已完成 Sprint：S09 图像上下文工作台 + 本地图像源/Image 数据链

状态：**已验证实现**

### A 线：VM 类型图像上下文工作台

1. 右上图像区可切换全局输入、当前模块输入和当前模块输出，所有画面来自当前全局 HImage 或托管模块快照。
2. 上下文栏显示真实来源工具/端口、文件名、SN、尺寸；鼠标移动显示像素坐标与灰度/RGB 值，底栏显示 ROI/结果叠加状态。
3. 选择流程模块时自动定位可用输入/输出；用户手动选择后不被后续选中覆盖，无 Image 输出时明确回退到模块输入/全局输入。
4. 物理 1600×950、125% DPI 截图确认模块箱、流程节点、上下文、来源、HALCON 画面、I/O 和结果同时可见。

### B 线：本地图像源与真实 Image 数据链

1. 模块箱和统一配置新增“本地图像源”，可选择文件并设置 SN，运行输出 `Image/Path/SN/Width/Height/ResultCode`。
2. `VmImageSnapshot` 持有独立 HImage 副本和来源元数据；Blob/Gray/Edge 可订阅前序 Image，未订阅时继续使用 `系统.Image`。
3. 前向、类型、上游状态、未运行、缺失和已释放快照均被拒绝；上游重跑/删除/配方重载/流程释放使下游失效并释放旧快照。
4. 图像源文件/SN、Image 输入绑定进入 v2 配方；本轮没有触碰相机 SDK。

### 验证证据

- 100×60 两图真实 HALCON 链：Blob `400→800`、Gray `17→34`、Edge `76.109→116.109`；Region 链均值 `255`，Number 链 `OK→OK`。
- Image 快照尺寸、SN、路径、像素读取、旧快照重跑释放、流程释放、未运行/已释放拒绝、类型/前向错误、模块输入/输出上下文和配方回读通过。
- Debug/Release x64 均为 0 警告 0 错误；Debug 启动 8 秒保持响应；MainWindow 94 个、配置窗口 11 个 XAML 事件入口无缺失；物理 1600×950 UI 截图通过。
- 相机 SDK、`.jw/.handle`、自动换型、权限和 BSB 均未实施/未验证。

## 已完成 Sprint：S08 方案/配方工作区 + 自动保存恢复

状态：**已验证实现**

### A 线：VM 类型方案/配方工作区

1. 顶部同步当前方案名和 `新方案/未保存/已保存/可恢复` 状态，菜单与快捷工具条都有新建、打开、保存、另存入口。
2. 右下“方案”页包含 `当前/最近/恢复/自检` 四个直接可见内页；当前页显示正式路径、格式版本和最近保存，最近页支持双击打开和移除失效记录。
3. 新建、打开、最近打开和退出在存在未保存编辑时提供保存/放弃/取消；待恢复副本也必须显式恢复或忽略。
4. 1600×950 当前/最近页真实 WPF 截图确认顶部状态、内页、字段和操作按钮可见。

### B 线：版本化、原子保存与崩溃恢复

1. JSON 配方新增兼容 `SchemaVersion=v2`；旧配方缺字段按旧版读取后迁移到当前内存模型。
2. 配方指纹覆盖流程、实例参数、输入绑定、ROI、运行策略和通信设置；变化后标脏并防抖写入独立恢复副本。
3. 正式保存使用同目录临时文件、序列化回读验证、原子替换和单份 `.bak`；覆盖失败不改变正式文件。
4. 启动检测较新恢复副本并提供恢复/忽略；损坏副本隔离，正常保存/明确放弃后清理恢复文件。

### 验证证据

- 服务测试：首次/覆盖原子保存和备份通过；正式文件被独占锁定时保存失败且字节不变；无版本旧 JSON 迁移通过。
- 恢复测试：含 S07 `ROI <- SelectedRegion` 的恢复副本回读通过；损坏恢复文件被拒绝并隔离，正式文件保留。
- UI 生命周期：脏状态、自动恢复副本、保存清脏、最近列表、启动检测、恢复应用和四内页通过。
- HALCON 回归：`Blob=800 / Gray=200 / Edge=96.108 / Region ×2，Area=800`；Debug/Release 0 警告 0 错误；MainWindow 88 个、配置窗口 10 个事件入口无缺失；Debug 启动 8 秒保持响应。
- `.jw/.handle`、自动换型、角色权限、相机 SDK 和 BSB 均未实施/未验证。

## 已完成 Sprint：S07 输入订阅工作台 + 真实 Region 数据链

状态：**已验证实现**

### A 线：统一配置中的类型化输入订阅

1. 统一配置的 `输入` 页按端口显示名称、类型、必需/可选、来源、当前值和连接状态；真实端口卡片已经过高 DPI WPF 渲染检查。
2. 来源选择保留为实例草稿，应用后写入、取消不污染；可恢复全部默认输入，并可“应用并查看 I/O”。
3. 只列出当前工具之前且数据类型匹配的输出；流程节点、统一配置和右侧 I/O 读取同一绑定数据。
4. 当前仍只呈现确定性顺序数据流，没有绘制脱离实际执行语义的自由连线。

### B 线：实例输入 Schema 与 Region 数据传递

1. 实例级输入绑定 Schema 已支持克隆、替换、配方保存和旧配方缺字段兼容。
2. Blob 输出托管的 `SelectedRegion` 快照；Gray/Edge 可订阅该 Region 并把它作为 HALCON 运行域。
3. 未订阅时保留本地 ROI/全图路径；前向连接、类型不匹配、上游未运行/NG、快照缺失/释放以及 Region 订阅与逐 ROI 冲突都有明确错误。
4. 上游重跑、配置/顺序变化和删除会传递失效到下游；替换输出、删除工具、重载配方和关闭窗口会释放 Region 快照。

### 验证证据

- 100×60 合成图：Blob 在两个亮区输出 `Region ×2，Area=800`；Gray 全图均值为 `26.667`，订阅后为 `200`；Edge 订阅后长度为 `96.108`。
- Blob 重跑后旧快照已释放，Gray 结果回到“未运行”；Blob 变为 NG 时下游拒绝运行。`Number -> Region` 和后向来源都被配置校验拒绝。
- 配置窗口选择/应用/取消通过，Region 与逐 ROI 冲突明确拒绝；输入绑定配方回读为 `ROI <- SelectedRegion`，旧配方缺 `InputBindings` 时保持默认输入。
- 删除上游自动断开下游，NumericJudge 的 Number 输入编辑仍提供类型匹配来源；Debug/Release、启动、XAML 入口和高 DPI 输入页截图通过。
- BSB 按用户指令继续未实施/未验证。

## 已完成 Sprint：S06 ROI 配置工作台 + 逐 ROI 工业结果

状态：**已验证实现**

### A 线：统一配置中的真实 ROI 工作区

1. `ROI/模板` 页已从 ID 摘要升级为真实图层表，显示序号、名称、形状、几何、启停和绑定勾选，支持全选/清空。
2. 绑定和执行模式以草稿编辑；应用写入当前实例，取消不污染；试运行临时使用草稿并恢复原绑定。
3. 可“应用并返回图像区”，也可直接选择矩形、圆形或多边形并回到右上图像区继续绘制，保持当前工具上下文。
4. 右上 ROI 侧栏按当前工具显示逐区 OK/NG 与主数值；右下模块结果和配置窗口结果页均有逐区表格。

### B 线：逐 ROI HALCON 执行与集合结果

1. Blob、灰度、边缘实例新增 `Union` 与 `PerRoi`，旧/缺失字段默认 `Union`，非法字符串明确拒绝。
2. `PerRoi` 按每个已启用且已绑定 RoiId 分别调用原 HALCON 阈值/统计/边缘算子；单区异常记录 NG 并继续其余区域，总结果汇总 OK/NG。
3. 每区输出 RoiId、名称、形状、结果码、Area/Mean/Length、消息、错误和耗时；`RoiResults` 集合进入 I/O、右下结果、配置窗口和日志。
4. Blob/Edge 将逐区图形合并显示；Gray 保留 ROI 图层显示；原 Union 和无 ROI 全图兼容路径不变。

### 验证证据

- 配置草稿：应用前实例仍绑定 ROI_A/Union；应用后切换为 ROI_B/PerRoi，并把运行状态重置为待运行。
- 配方字段：`PerRoi` 序列化回读；缺失 `RoiExecutionMode` 规范化为 `Union`；`BogusMode` 返回“ROI 执行模式无效”。
- 两区 HALCON：Blob `Area=1200/600` 且总面积 `1800`；Gray `Mean=121.829/211.917` 且分别 OK/NG；Edge `Length=184.217/138.216`；三类集合端口均显示 `×2`。
- 兼容与异常：Union 无绑定继续全图运行，Blob 面积 `1200`、集合为 0；PerRoi 无绑定明确拒绝并引导到统一配置 ROI 页；停用图层由活动绑定解析排除，删除图层继续使用 S03 已验证的全工具解绑路径。
- Debug/Release x64 0 错误 0 警告；MainWindow 88 个、配置窗口 7 个唯一事件处理器无缺失；Debug 启动保持运行 8 秒；高 DPI ROI 工作台截图确认绑定表、模式、绘制入口和操作按钮完整可见。
- Shape/HDevelop、相机 SDK、TCP 协议和 HALCON 参数含义未修改；真实 Shape 模板与 `.jw/.handle` 仍缺样本，未验证。
- BSB 按用户指令继续未实施/未验证。

## 已完成 Sprint：S05 统一模块配置工作台 + 实例参数 Schema

状态：**已验证实现**

### A 线：统一 VM 类型模块配置

1. 双击流程模块或使用“配置”入口打开统一工作台，不再把右下手写 Inspector 当作完整参数页。
2. 工作台首版包含基本参数、运行参数、输入、ROI/模板、结果显示和帮助页，以及试运行、应用、确定和取消。
3. 右下区域继续显示选中模块摘要、运行结果和快捷入口；配置工作台负责完整编辑闭环。
4. 首批完整迁移 Blob、灰度、边缘和数值判定；Shape/HDevelop 以兼容页接入并明确未迁移项。

### B 线：实例参数 Schema 与同类型多实例

1. 为首批工具建立实例级参数对象、默认值、验证、克隆、配方保存和旧字段迁移。
2. 解除当前“每类只允许一个实例”的限制，至少验证两个 Blob 实例可保存不同阈值/面积并独立运行。
3. 应用只更新当前编辑实例；取消不污染运行参数；错误参数阻止应用并给出字段级原因。
4. 旧配方顶层 Blob/Gray/Edge/NumericJudge 参数迁移到兼容实例，不改变 HALCON 算子和参数含义。

### 验证证据

- 用户路径已实现：选择/双击模块 -> 统一配置 -> 修改 -> 试运行 -> 应用/取消 -> 查看右下结果 -> 保存配方 -> 重启回读。
- 两个 Blob 实例参数 `20/110/33` 与 `140/250/77` 保存回读后隔离；修改第一实例不会污染第二实例。
- `tests/fixtures/legacy-recipe-v1.json` 的顶层 Blob 参数迁移为 `90/245/120`；数值判定继续使用 S02 已有实例级 `NumericJudge` 配方结构。
- 窗口逻辑验证：应用写入 `90`，取消保持 `30`，试运行后恢复 `40`，非法 `240 > 100` 被拒绝且不写入。
- HALCON 固定合成图回归：Blob Area=`2000`、Gray Mean=`50`、Edge Length=`176.109`。
- Debug/Release x64 0 错误 0 警告；MainWindow 85 个、配置窗口 3 个 XAML 事件入口无缺失；Debug 启动和 1920×1200 配置窗口可见验证通过。
- Shape/HDevelop 使用明确兼容页；真实 Shape 模板、相机和 `.jw/.handle` 缺少样本，未验证。
- BSB 按用户指令继续未实施/未验证。

## 已完成 Sprint：S04 官方 VM 主工作区 + 流程运行策略

状态：**已验证实现**

### 本轮主题

依据海康官方手册和 VM5.0 发布资料，把当前过渡工作台纠正为官方 VM 开发页首版，同时补齐真实流程运行控制。BSB 继续暂停。

### A 线：官方 VM 开发主界面首版

原问题：活动 WPF 以中央大图为主，流程是左侧线性 `ListBox`，参数是右侧常驻 Inspector；官方 VM 界面图则以中央流程编辑为主，右侧上/下显示图像和模块结果。

实际交付：

1. 顶部形成菜单栏和快捷运行工具条，显示当前方案、运行模式和核心状态。
2. 左侧保留并深化分类模块箱、搜索和添加入口。
3. 中央改为网格流程编辑区；当前线性执行先以真实有向连接呈现，增加流程标签、缩放、适应和运行定位。
4. 右侧上方承载 HALCON 图像与 ROI，右侧下方承载模块输出/结果；现有 I/O、参数、ROI、通信和工程入口迁移为可达的配置/功能入口，不得丢失。
5. 模块卡显示实例、类型、启用、配置、运行、OK/NG、耗时和错误；选中态与执行态清晰。
6. 第一版不伪造并行、汇聚和 Group；只为后续真实执行模型保留产品结构。

### B 线：真实流程运行策略

入口：顶部快捷工具条、流程标签/菜单和模块右键菜单。

实际交付：

- 运行当前、运行到此、从此运行和单次全流程。
- 连续运行、暂停/恢复和停止。
- 连续运行间隔、流程超时和遇 NG 停止。
- 当前执行范围、状态、总耗时、停止原因、错误和日志。
- 运行策略进入配方，旧配方使用兼容默认值。
- HALCON 调用仍按确定性线性顺序运行；取消/停止必须在安全边界生效，不破坏图像和 ROI 生命周期。

### 用户路径

打开方案 -> 从模块箱添加/选择工具 -> 在中央流程确认顺序 -> 双击或配置入口设置参数 -> 在右上图像区检查 ROI -> 选择运行当前/到此/从此/单次/连续 -> 在右下结果和底部状态查看输出、耗时和停止原因 -> 保存配方 -> 重启后恢复运行策略。

### 验证证据

- 物理 1920×1200 截图确认“左模块箱 + 中央流程 + 右上图像 + 右下结果/配置 + 顶部菜单/工具条 + 底部状态/历史”完整显示，无横向裁切。
- 流程规划：4 个工具含 1 个停用工具时，全流程得到 3 个执行项；运行到此、从此运行各得到正确范围，运行当前得到 1 项；无选择的范围命令明确拒绝。
- 运行决策：暂停、用户停止、超时、遇 NG 停止和正常继续由活动执行器使用的 `VmFlowRuntimeDecider` 覆盖；连续间隔和超时边界由策略规范化/界面校验覆盖。
- 配方：旧 `tests/fixtures/legacy-recipe-v1.json` 缺少策略字段时保持兼容；新策略 `750/5000/true` 保存回读一致，默认策略为 `500/0/false`。
- S01-S03 的活动工具、类型化端口、数值判定、多 ROI、参数/通信/工程入口均保留；本轮没有改动 HALCON 算法方法与参数语义。
- XAML 可解析且所有新增事件已编译绑定；Debug/Release x64 均 0 错误 0 警告；Release 启动后保持运行。
- BSB 按用户指令未实施/未验证。

### 风险与回退

- 主界面重排可能造成事件入口丢失或高 DPI 截断，必须做 XAML 事件和截图回归。
- 连续运行与 UI 线程耦合，先建立可取消的确定性执行策略，不一次引入伪并行。
- 不改 HALCON 算法参数、相机 SDK、TCP 协议语义和旧配方已有字段；通过新增可选字段兼容。
- S03 提交 `95255b1` 和目标文档提交 `ebf1c0b` 是回退点；S04 以独立已验证提交交付。

## 已完成 Sprint：S03 VM 图像工具栏 + 工具级多 ROI

状态：**已验证实现**

### A 线：VM 图像与 ROI 工作区

原问题：ROI 操作只位于右侧页签，以单个全局对象存在；中央图像区没有常驻工具栏，用户看不到 ROI 列表、显隐、启停或工具归属，不符合 VM 工程师的图像调试习惯。

实际交付：

1. 中央图像区新增选择、平移、矩形、圆形、多边形、确认、取消、适应、1:1 和显示/隐藏图层工具栏。
2. 图像右侧增加常驻 ROI 图层列，显示序号、名称、形状、工具绑定摘要，并可分别控制显示和参与运行。
3. 右侧“图像/ROI”页增加选中图层命名、删除、清空和当前工具绑定清单。
4. 每次确认新增独立图层并自动绑定到当前选中的视觉工具，不再覆盖前一个 ROI。
5. 图像区同时以绿色显示选中 ROI、青色显示其他已启用 ROI、灰色显示停用 ROI。

### B 线：工具级多 ROI HALCON 执行

入口：选择 Shape、Blob、灰度、边缘或 HDevelop 工具 -> “图像/ROI” -> 勾选一个或多个 ROI。

执行闭环：

- 每个视觉工具实例保存自己的 ROI ID 集合。
- 运行时过滤已停用或已删除图层，再以 HALCON `Union2` 合并该工具的所有有效 Region。
- Shape 的搜索域、Blob 的阈值区域相交、灰度统计 Region、边缘 `ReduceDomain` 和 HDevelop `ROI` 图形参数均使用合并结果。
- Blob、灰度、边缘没有绑定时保持原全图运行；Shape 和 HDevelop 没有有效 ROI 时给出可操作错误。
- I/O 页、流程连接摘要、结果消息和日志显示实际 ROI 数量；结果记录保留主 ROI，配方保存全部图层与绑定。

配方：新增 `VisionRecipe.RoiLayers` 与 `ToolFlowRecipeItem.RoiIds`；同时继续写入旧 `SearchRoi`。缺少新字段但带 `SearchRoi` 的旧配方自动生成一个兼容图层并绑定原视觉工具。

用户路径：打开图像 -> 在流程中选择视觉工具 -> 中央工具栏绘制并确认多个 ROI -> 图层列调整显隐/启停 -> 右侧勾选工具绑定 -> 运行当前或全流程 -> I/O/结果/图形检查 -> 保存配方。

### 验证证据

- 多 ROI Blob：两个互不相交区域面积合计 `500`；停用一个图层后面积 `100`。
- 完整流程：`Blob -> NumericJudge` 在面积 `500` 时返回 OK，停用一个 ROI 后面积 `100` 返回 NG。
- 配方对象和 JSON：2 个 ROI 图层、2 个绑定 ID、显隐/启停字段保存回读通过。
- 兼容：旧单 `SearchRoi` 自动迁移为 1 图层/1 绑定；`tests/fixtures/legacy-recipe-v1.json` 无 ROI 读取后仍保持无 ROI。
- 异常：Shape 等必需 ROI 工具未绑定有效图层时明确拒绝；删除图层同步移除全部工具绑定。
- HALCON 回归：Blob Area=`3850`、Gray Mean=`51.133`、Edge Length=`246.109`。
- UI：1920×1200 下中央工具栏、常驻图层列、右侧绑定清单和两个 HALCON ROI 叠加完整显示；XAML 79 个事件入口无缺失。
- 构建/启动：Debug/Release x64 均 0 错误 0 警告；Release 启动保持运行。
- BSB：按用户指令继续暂停，未实施/未验证 BSB 专用场景。

### 风险与未完成

- ROI 仍不支持拖动控制点、旋转矩形、复制、锁定和布尔编辑。
- S03 当时多 ROI 只以并集进入工具；该差距已由 S06 为 Blob/灰度/边缘补齐逐 ROI 集合结果，Shape/HDevelop 仍使用绑定区域兼容路径。
- 汇总 `InspectionRecord` 仍只保存主 ROI；S06 的全部逐区细节保存在当前工具集合输出、结果表与日志，持久运行历史 Schema 仍待后续扩展。
- Shape Model 真实模板、相机和 `.jw/.handle` 仍缺样本，不能宣称这些路径已验证。

## 已完成 Sprint：S02 VM I/O 端口 + 数值判定

状态：**已验证实现**

### A 线：VM 输入输出体验

原问题：S01 只有输入/输出文字摘要，用户看不到真实端口、类型、来源、当前值和连接异常，流程卡也无法快速判断数据从哪里来。

实际交付：

1. 右侧 `I/O` 页增加 VM 风格 IN/OUT 端口卡片，显示端口显示名、稳定端口名、数据类型、来源、当前值和状态。
2. Shape、Blob、Gray、Edge、HDevelop 和 NumericJudge 均建立类型化端口目录。
3. 流程卡增加连接摘要，数值工具直接显示 `上游实例.端口 → Value`。
4. I/O 页支持跳转参数、运行当前和刷新值；参数 Inspector 支持跳转 I/O。
5. 未加载图像、ROI、模板、程序、未运行输出和错误订阅均以不同状态显示。

### B 线：数值判定工业工具

入口：工具箱 `逻辑/计算 / 数值判定`。

输入订阅：只能选择位于当前工具之前且具备 Number 输出的上游实例；可订阅：

- Shape：Score、MatchCount、Row、Column、Angle；
- Blob：Area；
- Gray：Mean；
- Edge：Length；
- HDevelop：Score。

参数：区间内、区间外、大于等于、小于等于、等于、不等于；下限/目标、上限、相等容差。

输出：Value、Passed、ResultCode，并进入工具状态、I/O 实时值、结果记录、全流程 OK/NG、日志和 JSON 外发结果。

配方：`ToolFlowRecipeItem.NumericJudge` 保存上游 ToolId、端口、比较方式、上下限和容差；S01 配方和无 `ToolFlow` 的旧配方保持兼容。

用户路径：工具箱添加上游视觉工具 -> 添加数值判定 -> 在参数页选择上游与数值端口 -> 配置阈值 -> 运行当前或全流程 -> I/O 页查看实时值 -> 保存配方。

### 验证证据

- 合成图 HALCON：Blob Area=`3850`、Gray Mean=`51.133`、Edge Length=`246.109`。
- 数值判定：Blob.Area 在 `[3000,4000]` 返回 OK，在 `[4000,5000]` 返回 NG。
- 全流程：`Blob -> NumericJudge` 顺序执行，最终 JSON 包含 NumericJudge 且总结果 OK。
- 异常：上游位于下游之后、区间下限大于上限、上游未运行无值，均被明确拒绝。
- I/O：NumericJudge 显示 1 个 IN、3 个 OUT，来源为 `Blob.Area`，运行值为 `3850`。
- 配方：新增订阅字段保存/重载通过；`tests/fixtures/legacy-recipe-v1.json` 继续读取通过。
- UI：1920×1200 物理分辨率下参数 Inspector 和 I/O 端口页完整显示。
- 构建/启动：Debug/Release x64 均 0 错误 0 警告；Release 启动保持运行。
- BSB：按用户指令继续暂停，未实施/未验证 BSB 专用场景。

### 风险与未完成

- S02 当时每类工具仍限制一个实例（已由 S05 解除）；端口订阅仍只开放单个 Number 输入。
- 图像、Region、数组和多输入端口尚未开放用户订阅。
- 参数仍有部分保存在配方顶层，尚未形成统一实例参数 Schema。
- 没有真实 Shape Model、相机、`.jw/.handle` 样本，本轮不能宣称这些路径已验证。

## 已完成 Sprint：S01 VM 顺序流程工作区 + 通用视觉流程闭环

状态：**已验证实现**

### A 线：UI/UX 升级

原问题：活动 WPF 主界面只有中央图像和右侧静态页签，五个检测工具由复选框固定启停，不符合 VM 的工具箱、顺序流程和 Inspector 工作方式。

实际交付：

1. 左侧增加可搜索分类工具箱，首批包含 Shape Model、Blob、灰度统计、边缘测量和 HDevelop。
2. 增加始终可见的顺序流程实例区，显示实例名、类型、启用、配置、结果、耗时和错误。
3. 支持从工具箱添加、删除、重命名、启停、上移、下移、运行当前和全流程运行。
4. 选中工具后右侧 Inspector 显示该实例的参数、输入输出和结果。
5. 保留中央 HALCON 图像区和底部日志/结果区，并修正高 DPI 下的自适应比例和横向截断。
6. 现有 Shape/Blob/Gray/Edge/HDevelop 执行方法通过适配接入，不改变算法和参数语义。

用户路径：工具箱双击或点击添加 -> 在流程区选择实例 -> 右侧配置 -> 运行当前/全流程 -> 图像与输出更新 -> 日志记录 -> 保存配方。

### B 线：通用工业视觉功能

能力：`可持久化的顺序工具流程`

- 流程区的实际顺序决定执行顺序，不再由五段硬编码复选框决定。
- Shape、Blob、Gray、Edge、HDevelop 统一经过工具实例适配，可运行当前或运行全流程。
- 每个实例保存稳定 ID、类型、名称、启停和顺序；运行后回写配置状态、执行状态、OK/NG、耗时、输入/输出摘要和错误。
- `VisionRecipe.ToolFlow` 保存流程；无该字段的旧配方继续用原五个布尔字段按原顺序恢复已启用工具。
- 切换到无模板配方时清除上一个模板，避免配方串用。

独立验证：使用 160×120 合成图像执行 HALCON Blob 节点，白色区域面积结果 `3850`、结果 `OK`、节点状态“完成”；旧配方夹具读取和新流程配方保存/重载均通过；工具箱添加 Blob、上移为 `Blob -> ShapeMatch`、序号刷新、Inspector 切换、重命名和捕获配方顺序的 UI 逻辑回归均通过。

### BSB 状态

用户于 2026-07-13 明确要求暂缓 BSB，本 Sprint 已撤出未提交的 BSB 点位补偿代码和入口。BSB 需求仍保留为长期验收基线，收到后续明确指令后重新提出双线 Sprint，不把本轮通用能力计作 BSB 完成。

### 兼容、风险与回退

- 旧配方仍按现有布尔字段生成默认流程；新增 `ToolFlow` 缺失时使用兼容映射。
- 不修改 Shape Model、Blob、灰度、边缘和 HDevelop 算法实现。
- `MainForm`/`TemplateCreateForm` 暂时保留编译，不作为活动入口。
- S01 当时的主要风险是每类型一个实例和配方顶层参数（已由 S05 解决首批工具）；模板训练仍使用专用弹窗。
- 回退点：Git 基线提交 `858924e` 和本 Sprint 文档基线提交。

### 验收标准

- UI 肉眼可见工具箱、顺序流程区和 Inspector。
- 流程顺序真实影响执行顺序，运行状态、OK/NG、耗时和错误可见。
- Debug/Release x64 编译通过，程序启动通过。
- 合成图像 HALCON Blob 独立运行通过；旧格式配方回读和新流程配方回读通过。
- 真实 Shape Model 模板、相机和 `.jw/.handle` 缺少样本，本轮不宣称通过。
- 产品文档按实际完成情况更新；每个验证闭环记入 Git 批次，累计 10 次后统一提交并推送 GitHub。

## 后续候选 Sprint

| 优先级 | 主题 | A 线 | B 线 | 状态 |
| --- | --- | --- | --- | --- |
| P0 | S05 统一模块配置 | 基本/运行/结果/ROI 页和试运行 | 参数 Schema、实例隔离、校验与迁移 | 已验证实现 |
| P0 | S06 ROI/结果深化 | 配置页绑定、图像区上下文 | 逐 ROI 执行、集合结果和兼容迁移 | 已验证实现 |
| P0 | S07 高级输入输出 | 类型化订阅编辑、来源状态同步 | 实例输入 Schema、Region 快照和真实数据传递 | 已验证实现 |
| P0 | S08 方案/配方生命周期 | 当前/最近/恢复/自检和顶部状态 | 版本、迁移、原子保存和崩溃恢复 | 已验证实现 |
| P0 | S09 图像上下文与本地图像源 | 全局/模块输入/模块输出图像工作台 | 本地图像源、Image 快照和真实数据传递 | 已验证实现 |
| P0 | S10 停靠式配置与图像通道 | 主要参数右侧停靠编辑、草稿/应用/撤销 | HALCON 图像通道转换和连续 Image 数据链 | 已验证实现 |
| P0 | S11 ROI 精细编辑与图像滤波 | 控制点、移动/调整、复制和锁定 | HALCON 均值/中值滤波与连续 Image 数据链 | 已验证实现 |
| P0 | S12 模块箱效率化与阈值分割 | 分类/收藏/最近/拖拽插入 | HALCON 阈值 Region 与下游订阅 | 已验证实现 |
| P0 | S13 图像调试区可视化与 Region 形态学 | 放大/恢复、叠加控制和结果图例 | HALCON Region 开闭/膨胀/腐蚀与下游订阅 | 已验证实现 |
| P0 | S14 高级 ROI 编辑与 Region 特征筛选 | 旋转矩形/多边形控制点、复制和锁定 | HALCON 面积/宽度/高度/圆度筛选与下游订阅 | 已验证实现 |
| P0 | S15 流程依赖可视化与 Region 集合运算 | 类型化端口、来源/消费者和双输入汇聚状态 | HALCON 双 Region 并/交/差/对称差与下游订阅 | 已验证实现 |
| P0 | S16 真实连接画布与图像增强 | 真实连接路径、依赖分层和自动布局首版 | HALCON 线性灰度缩放/局部对比度增强与连续 Image 输出 | 已验证实现 |
| P0 | S17 连接检查/导航与动态阈值分割 | 连接选择、定位、配置和快速修复 | HALCON 双 Image 动态阈值 Region 与下游订阅 | 已验证实现 |
| P0 | S18 端口级订阅导航与图像差分 | 输入端口快速改订阅、输出端口消费者导航 | HALCON 双 Image 差分与连续 Image 输出 | 已验证实现 |
| P0 | S19 模块复制/粘贴与 Region 轮廓测量 | 多入口复制、粘贴、副本和连接重映射 | HALCON Region→XLD 总长度/轮廓数与数值订阅 | 已验证实现 |
| P0 | S20 画布框选/批量操作与 XLD 筛选 | 橡皮框、多选同步和批量真实操作 | HALCON `select_contours_xld` 连续 XLD 输出 | 已验证实现 |
| P0 | S21 端口拖拽连线与 XLD 分段 | OUT→IN 创建/替换/重连和实时校验 | HALCON `segment_contours_xld` 连续 XLD 输出 | 已验证实现 |
| P0 | S22 节点定位与 XLD 直线拟合 | 网格吸附、对齐/分布和布局持久化 | HALCON `fit_line_contour_xld` 工业测量封装 | 已验证实现 |
| P0 | S23 撤销/重做与 XLD 圆拟合 | 真实编辑历史、状态和快捷键 | HALCON `fit_circle_contour_xld` 工业测量封装 | 已验证实现 |
| P0 | S24 流程组合/折叠与 XLD 椭圆拟合 | 可持久化组合容器及边界连接 | HALCON `fit_ellipse_contour_xld` 工业测量封装 | 已验证实现 |
| P0 | S25 断点/单步调试与 XLD 旋转矩形拟合 | 确定性执行断点、单步、继续和指针 | HALCON `fit_rectangle2_contour_xld` 工业测量封装 | 已验证实现 |
| P0 | S26 调试监视工作台与双直线关系测量 | 断点列表、运行快照、节点定位和运行到所选 | HALCON 双 XLD 交点/夹角/平行距离工业测量封装 | 已验证实现 |
| P0 | S27 多结果图层管理与卡尺边缘对测量 | 全部 Region/XLD/测量叠加的显隐、隔离、排序和定位 | HALCON 旋转矩形卡尺边缘对与间距工业测量封装 | 已验证实现 |
| P0 | S28 图像内测量几何与线性多卡尺阵列 | 测量域控制点、草稿/应用/取消和上下文联动 | HALCON 多卡尺逐项测量、统计和失败定位 | 已验证实现 |
| P0 | S29 线性阵列图像编排与圆周卡尺找圆 | 基线/整体/卡尺长宽控制点和逐项状态 | HALCON 圆周采边、拟合圆与失败定位 | 已验证实现 |
| P0 | S30 圆周测量域图像编排与圆环壁厚/同心度测量 | 圆心/半径/角度/卡尺长宽控制点和逐项状态 | HALCON 径向边缘对、双圆拟合、厚度统计与偏心量 | 已验证实现 |
| P0 | S31 椭圆/圆环 ROI 图像编辑与极坐标展开 | 椭圆/圆环创建、控制点、绑定和几何保护 | HALCON `polar_trans_image_ext` 托管 Image 与下游链 | 已验证实现 |
| P0 | S32 ROI 数值几何属性与 ROI 图像提取 | 六类 ROI 数值属性与图像控制点双向同步 | HALCON `reduce_domain/crop_domain` 托管 Image 与坐标偏移 | 已验证实现 |
| P0 | S33 双画面图像对比与图像尺寸缩放 | 单/双视口输入输出对比及联动/独立视图 | HALCON `zoom_image_size` 托管 Image 与坐标比例 | 已验证实现 |
| P0 | S34 双十字辅助线与图像镜像 | 两组十字线的常驻配置与图像内交互 | HALCON `mirror_image` 托管 Image 与坐标映射 | 已验证实现 |
| P0 | S35 线/点 ROI 图像编辑与图像旋转 | 线/点创建、控制点、数值属性和配方 | HALCON `rotate_image` 托管 Image 与坐标语义 | 已验证实现 |
| P0 | S36 ROI 屏蔽区与图像仿射变换 | 屏蔽区常驻配置、图像编辑和真实 ROI 排除 | HALCON `affine_trans_image` 托管 Image 与矩阵/坐标语义 | 已验证实现 |
| P0 | S37 统一图层属性与图像透视校正 | ROI/屏蔽/结果层属性、选择联动和持久化 | HALCON `projective_trans_image` 托管 Image 与投影坐标语义 | 已验证实现 |
| P0 | S38 透视四边形图像交互与亚像素边缘 | 源/目标四点图像编辑、草稿预览和坐标隔离 | HALCON `edges_sub_pix` 托管 XLD 与下游测量链 | 已验证实现 |
| P0 | S39 XLD 轮廓拾取/检查与相邻轮廓连接 | 单轮廓鼠标命中、亚像素点检查和生产者联动 | HALCON `union_adjacent_contours_xld` 托管 XLD 与下游链 | 已验证实现 |
| P0 | S40 图像队列缩略导航与 XLD 轮廓平滑 | 真实图片队列缩略图、点击/播放/状态同步 | HALCON `smooth_contours_xld` 托管 XLD 与下游链 | 已验证实现 |
| P0 | S41 图片运行范围与 XLD 仿射变换 | 当前/选中/全部批量运行、逐图状态和汇总 | HALCON `affine_trans_contour_xld` 托管 XLD 与坐标语义 | 已验证实现 |
| P0 | S42 生产运行界面首版与 XLD 透视变换 | 开发/运行切换、真实运行状态和受控生产操作 | HALCON `projective_trans_contour_xld` 托管 XLD 与投影坐标语义 | 已验证实现 |
| P0 | S43 运行界面配置首版与 XLD 矩形裁剪 | 同一生产页的受控部件标题、显隐、顺序和配方预览 | HALCON `crop_contours_xld` 托管 XLD、真实边界裁剪与图像草稿 | 已验证实现 |
| P1 | S44 批次追溯工作台与 XLD 长度分级 | 持久批次列表、详情、筛选、重载和 CSV | HALCON XLD 短/中/长分级、统计、三类托管图层 | 已验证实现 |
| P0 | S45 持久结果图像回看与 XLD 长度排序/索引选择 | 历史图片/叠加只读回看、清理和容量状态 | HALCON XLD 稳定排序、Top/Bottom/排名范围选择 | 已验证实现 |
| P0 | S46 历史结果浏览/图层控制与 XLD 点数/闭合筛选 | 连续导航、双图对比、历史图层会话控制 | HALCON XLD 点数/闭合筛选与稳定索引 | 已验证实现 |
| P0 | S47 历史逐工具结果明细与 XLD 面积/方向筛选 | JSON v3 逐工具标量明细、搜索筛选和图层联动 | HALCON XLD 面积/方向筛选与托管输出 | 已验证实现 |
| P0 | S48 历史逐工具双样本对比与 XLD 外接框/紧凑度筛选 | 工具对齐、差异视图和双侧历史图层联动 | HALCON XLD 外接框/紧凑度闭区间筛选 | 已验证实现 |
| P0 | S49 历史工具标量趋势、首屏减负与 XLD 圆度/凸性筛选 | 跨图片趋势、点击回看和三处低频按钮折叠 | HALCON XLD 圆度/凸性闭区间筛选 | 已验证实现 |
| P0 | S50 主工作区信息密度重构与 XLD 最小包围圆/直径测量 | 紧凑工具箱/节点、单任务右侧面、底部折叠 | HALCON XLD 最小包围圆、直径和最远点对 | 已验证实现 |
| P0 | S51 顶部/图像命令分级与 XLD 最小旋转矩形/方向测量 | 顶部 5 个、图像 4 个常驻任务入口 | HALCON 最小旋转矩形、方向、尺寸、面积和四角 | 已验证实现 |
| P0 | S52 流程详情/工具目录聚焦与 XLD 方向一致性/角差测量 | 消除端口墙、标签墙和默认全部工具长表 | HALCON 主轴方向、半周角差和方向轴图形 | 已验证实现 |
| P0 | S53 右侧任务型配置器与通用 TCP 客户端 | 参数/I/O/结果三级任务、低频更多和状态感知草稿动作 | 通用 TCP 模板发送/响应/状态工具 | 已验证实现 |
| P0 | S54 关键/高级参数分级与通用文本组装 | 默认最多 6 个关键字段，高级折叠和错误展开 | 文本组装并真实驱动 TCP | 已验证实现 |
| P0 | S55 低高度参数工作区重排与通用文本解析 | 紧凑任务头、低高度直接显示关键字段 | 分隔/正则文本解析并驱动数值判定 | 已验证实现 |
| P0 | S56 紧凑顶部应用栏与通用数值运算 | 顶部两行、紧凑统计、释放主区空间 | 八种数值运算并驱动判定/文本 | 下一轮 |
| P0 | 后续资源管理 | 相机/控制器/IO/光源设备页 | WPF 单帧/连续采集和诊断闭环 | 提议，SDK 前需确认 |
| P1 | 后续通信管理 | 设备、事件、报文监视 | 心跳、重连、超时、映射、触发和回复 | 提议 |
| P1 | 后续全局逻辑 | 变量/触发/脚本/队列页面 | 类型化跨流程数据与多流程执行 | 提议 |
| P1 | 后续运行界面 | 控件箱、设计画布、属性和生产页 | 数据绑定、权限和导出运行 | 提议 |
| P1 | 后续历史诊断 | 查询、回放、诊断页 | 持久历史、图片、版本和异常采集 | 提议 |

BSB、PLC 现场协议和相机 SDK 相关 Sprint 不进入自动排期；分别等待用户恢复 BSB 或确认对应高风险边界。
