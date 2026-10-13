# S145 模板多目标类型化结果与明确选取

状态：A/B 开发接线已保存，部分实现（待集中验证），2026-10-04。2026-10-05 后续 S146 可编辑学习项目接线也已保存，部分实现（待集中验证），见 `S146_EDITABLE_LEARNING_PROJECTS.md`；当前下一完整 S147 见 `S147_MODULE_PREREQUISITES_AND_FOCUSED_REPAIR.md`（计划，尚未实现）。继续约 S150 的功能广度、简洁页面与低学习成本；文件即时保存，不逐轮编译/测试/启动/截图/鼠标或 Git，Git 仍 6/10。

## 当前实际接线与证据边界

已必要读取 `MainWindow.S145.cs`、`MainWindow.S145.UI.cs`、原示例加载 `MainWindow.S78.cs` 及实际 S132 消费路径。Shape/NCC 继续原训练和最佳输出；新多目标任务只在原“更多”按需展开，排序/从 0 起选取、只读同序明细/图上编号及明确填入草稿接原 F5/应用/保存。单击明细只查看，关闭或输入/模块/输出失效清理旧定位，不改变原最佳目标，不增加永久按钮或压窄流程。旧配方可选参数为 null；只查看或应用未改的默认 score/0 仍保留 null，非默认选取才显式配置，不向无关工具注入默认参数。

正式输出直接取本次 HALCON 候选：同序 Centers、Scores、Angles、Scales、SourceIndices，以及 TargetCount/HasSelection、独立 SelectedIndex/Point/Pose/Score/Scale。Shape 沿真实刚体与 Scale 矩阵做 AffineTransPixel 得像素参考点；NCC 明确 Scale=1。Scales 是真实数值，SelectedPose 仍仅像素位置/角度的刚体契约，不含缩放。SourceIndices 为算法原序 0 起；稳定排序以分数/行/列/角度及原序决胜，不从 Candidates 文本造数组，不冒跨帧跟踪。

多目标发布要求本模块明确正式 Image 与实际模型身份；缺资格时不冒正式数组/选取，旧兼容最佳路径仍保留。下游直接 Shape/NCC SelectedPose 消费在 S132 固定参考对齐及原 S62 位置修正共用资格守卫：复核真实当前输入版本、参数/订阅/ROI 本地定义键、已应用模型与 Shape 实际内存模型代际，并要求 SelectedScale 与 1 相差不超过 1e-9。非单位 Scale 可保留真实目标/分数/数组，但不能作为这两类刚体消费者的完整缩放变换；转发及全部跨子流程路径不冒称已全审计。

Shape 请求/候选最多 1000，NCC 最多 256；图上最多 100 个编号，正式全部候选 XLD 累计最多 100 万点，超预算整次拒绝，不截断冒完整。多目标转换限单幅 1600 万像素、边长 16384。真实无匹配保留空数组/0 数量，越界选取缺值不钳位，不补零位姿/旧帧；模型未应用、实际代际变更或输入/定义失效明确拒绝旧选择。局部键/运行版本不是全工程算法 SHA、防篡改、算法正确性或现场精度证明。

新增显式五步可编辑学习流程：图像→NCC→Centers 点集分析、Scores 数组统计与 SelectedScore 判定；须用户真实制作/应用模板，不自动训练或执行，学习分数门槛不是产品标准。没有本轮编译、测试、启动、鼠标、设备或完整 VM 对等结论。

## 实施前范围存档（下列起点描述为实施前历史）

## 实际起点与缺口

已必要实读 `MainWindow.S131.cs`：NCC当前真实匹配列表可在原候选表查看，但正式Centers/Score数组未输出；Point/Pose仍为按Score的最佳目标，Candidates是文本。`Models/VmToolModels.cs` 的Shape/NCC正式目录同样暴露MatchCount与Candidates摘要，不能把候选查看当正式选择。S63查看高亮不得被改成直接改变下游。实施前读取Shape真实结果和模板参考/坐标/草稿接线，若原能力重复则复用，不再做一套匹配引擎。

## A线：同一模板小窗里的多目标任务

复用Shape/NCC原分类、原参数小窗、原训练/候选页。在现有“更多”中提供一个紧凑结果组织/选取任务：按分数、图像中心行/列排序与0起的明确选取编号；常用首屏仍≤4项，不挤掉最关键搜索参数，不加永久按钮。原候选表标清“查看”和“正式选中”，查看可高亮但不改最佳/选中输出。明确写入草稿，经原F5/应用/保存生效；输出/模型/模块改变使旧查看失效。多目标编号不保证跨帧物理目标身份，不冒称跟踪。

## B线：真实同序候选值和选定位姿

从本次HALCON真实匹配列表在输出边界生成同序Centers Point2D[]、Scores/Angles/Scales/SourceIndices Number[]、候选数及明确SelectedIndex/SelectedPoint/SelectedPose/SelectedScore/SelectedScale/HasSelection。复用现有Point2D/Pose2D，不为了数组强建全新几何平台，不解析Candidates摘要。Shape和NCC的真实角度/像素中心/模型参考均按各自算法契约保留；HALCON中心与显示像素中心不可混淆。已有最佳Point/Pose/Score与S132参考/对齐订阅保持兼容，新增Selected*独立，不把改变查看顺序偷偷当旧最佳语义改变。

匹配无结果、显式编号越界、草稿模板未应用、资源版本不符、坐标/正式Image失效时：真实空数组/Count=0或明确失败，Selected*缺值、HasSelection=false；不钳到最后一个、不补零目标/位姿或使用旧帧。匹配请求数量、结果数量、元组/数组复制、候选图形和诊断文字均有上限；超限不截断冒完整结果。稳定排序同值决胜明确，SourceIndices是本次算法候选原编号，不是物理跟踪ID。数组/几何与唯一图像链同源，SDK模型/轮廓/元组沿原所有权释放，不让UI列表或长期记录持有HObject。

原脚本/数组/点集可消费正式结果，SelectedPose可显式接位置修正或固定参考对齐，参数/输入/ROI/资源、复制重映射、撤销、保存/工程随行、工程保护及F5范围闭环保持。补原中文帮助与不自动运行的多目标学习项目；算法搜索结果不是产品合格或现场精度认证，不重做NCC训练/Shape训练。

## 完整用户路径与后续集中范围

选择图像→在模板模块制作/应用真实模型→设真实多匹配数量→F5→按需看同序目标→明确选取/应用→SelectedPose或数组下游→判定/保存。集中阶段再覆盖空匹配、同分/位置排序、编号越界、模型草稿/版本、旋转参考、复制/撤销/旧配方、输入失效与生命周期/预算；此前不逐轮验收、不按编号虚报全部VM对等。

## 服务请求与候选返回边界（部分接入，待集中验证）

真实 Shape 核心为 `Services/ShapeTemplateService.cs`，继续原 `FindScaledShapeModel` 与训练/资源方式，不新建匹配引擎。搜索请求明确限制 1–1000，服务拒绝 0、负数和超限；历史实现实际用 Math.Max(1, MaxMatches) 把 0 改为 1，并不是受限全目标搜索。此次不把 0 默默改为 1000，也不将最多 1000 个请求称为“找到全部物理目标”。最低分须为有限 0–1；实际返回空元组表示本次无匹配。

Shape 返回在任何 double[]/候选列表复制前，先核对 Row/Column/Angle/Scale/Score 均存在、长度完全一致且不超过本次请求及 1000；再逐项确认原始位置、角度及度数转换、尺度和分数均有限、尺度正值、分数符合真实搜索范围。异常整次拒绝，不发布半列表、截断列表、零目标或旧结果。`Models/ShapeMatchResult.cs` 新增 SourceIndex 为本次 HALCON 原序 0 起编号（尚未赋值为 -1），搜索服务逐项真实赋值；旧 Index 仍为按分数/行/列排序后一基查看编号。完全同值以 SourceIndex 决胜，不能把查看编号冒原候选编号或跨帧目标身份。搜索输出元组及临时 reduced Image 沿 finally 释放。

NCC 核心实际在 `Models/VmNccMatchingS131.cs` 的 `VmNccModelS131`（与参数 DTO 同文件），不是 Services 下另一个类。保持已有请求 1–256、10 秒搜索超时及训练/参数契约，仅增强 Match 返回保护：元组须存在、同长且≤请求/256；全部原始位置/分数/角度检查在候选列表分配前完成，之后真实 AffineTransPixel 输出须各恰一个有限且在图像范围内的像素参考点。空匹配仍返回真实空列表，不截断或补点。NCC Index 为本次 HALCON 原序的一基编号，正式 SourceIndices 使用 Index−1，不是跨帧跟踪 ID。

Shape 不是固定尺度算法：默认尺度为 1，但原小窗和 `CreateScaledShapeModel / FindScaledShapeModel` 支持非单位尺度，轮廓按真实 Scale 变换。服务仍返回原 HALCON iconic Row/Column 和真实 Scale，旧 S63 Pose2D 直接使用原位置/角度且类型不含 Scale，也未做 AffineTransPixel；旧兼容语义不能证明新增精确中心或缩放位姿。当前 S145 正式中心已接真实刚体与缩放矩阵转换为像素参考点，Scales 独立保留；非单位尺度不能丢掉后伪造可用于刚体位置修正/对齐的 SelectedPose。整体 UI/正式输出接线已保存，仍待集中验证，不把服务代码存在当验收通过。

### 正式轮廓预算与模型代际（部分接入，待集中验证）

本机 HALCON 20.11 的 `bin/dotnet35/halcondotnet.xml` 已确认 `HOperatorSet.ContourPointNumXld(HObject, out HTuple)` 点数接口；本轮仅阅读 API 文档，没有执行算子或编译验证。图形服务先要求输入为具明确 Count 的本次候选集合、Count≤1000，再冻结独立候选值；拒绝任意无界 IEnumerable、数量变动和无效几何，不调用无界 ToList。空列表返回真实空 XLD。

`CreateMatchContours` 在按候选变换或 CopyObj 前先 CountObj 确认模型轮廓为 1–256 对象，再调用 ContourPointNumXld；点数元组须与对象数一致，每项有限、正整数，累计模型点数乘本次实际候选数量≤1000000。超预算整次明确失败，不能默默只保留前100个候选、部分模型轮廓或先 GetContourXld 拷无限点数组再数。正式 XLD 保持全部本次候选，Search/预算/变换矩阵 HTuple 和临时图形均有 finally 释放。

`DisplayMatchContours` 仅视觉预览，最多100个候选，调用方必须明示“图上最多100”；按实际预览数量核同样模型/点数预算。这个显示上限不改变正式候选数组、选择编号或完整 XLD，不将预览范围冒完整检测结果。

服务新增只读 `RuntimeModelVersionS145`：成功 Train/Load 后生成新的 Guid N，Clear/Dispose 时清空，Save 不换代；Getter 不执行 I/O。这仅标识实际内存 SDK 模型代际，不是文件 SHA、不可篡改或模型算法正确性证明。根流程可冻结字符串并复核，拒绝未应用模型/实际服务换代的选定位姿；DTO/历史不持有 SDK 服务对象。
