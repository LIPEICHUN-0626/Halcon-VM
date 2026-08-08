# HALCON VM 工业视觉平台开发约束

本文件适用于仓库全部目录。任何后续实现、审查和文档更新都必须遵守。

## 1. 唯一产品定义

- 本项目中的 **VM** 始终仅指海康机器人视觉平台 **VM / VisionMaster**。
- 最终产品必须成为“海康 VM 类型的完整工业视觉平台体验 + HALCON 算法内核”。
- 海康 VM 是页面分区、布局、工作流、工具组织、参数编辑、输入输出订阅、图像/ROI、调试、资源、运行界面和生产运维习惯的参照；所有视觉算子、定位、测量、标定和坐标转换继续以 HALCON 为算法引擎。
- 目标是尽量完整复刻 VM 的产品信息结构和操作习惯，不复制海康专有源代码、商标、图标、截图或手册资产；项目使用自制视觉资产和 HALCON 实现同类产品能力。
- 本项目不是 Cognex 软件、通用 Graph 编辑器、只画节点连线的演示，也不是在旧 WinForms 外面套一层配色。
- BSB 电池产线是长期核心验收场景，不是演示案例。
- 2026-07-13 用户明确要求暂缓 BSB 开发；在用户给出新的 BSB 实施指令前，不得自行启动 BSB 工具、协议或坐标功能开发。本阶段 B 线改为通用工业视觉能力，且不得把通用能力冒充 BSB 已完成。

## 2. 开始工作前必须阅读

1. `AGENTS.md`
2. `docs/VM_OFFICIAL_REFERENCE_SPEC.md`
3. `docs/CODEX_MASTER_PROMPT_VM_REPLICA.md`
4. `docs/PRODUCT_GOAL.md`
5. `docs/PRODUCT_ROADMAP.md`
6. `docs/BSB_REQUIREMENTS.md`
7. `docs/SPRINT_BACKLOG.md`
8. `docs/PRODUCT_GAP_ANALYSIS.md`
9. 与本轮功能直接相关的代码，而不是只看历史总结

若文档与代码冲突，以实际代码和可复现验证结果为准，并在本轮修正文档。

## 3. 每轮必须双线同步推进

2026-07-13 用户已授权后续迭代默认自行执行 A/B 双线，不再逐轮等待确认。每轮开始仍必须在计划和文档中明确两条线，并满足：

- A 线：肉眼可见且符合 VM 使用习惯的 UI/UX 或工作流升级。
- B 线：可从 UI 配置、运行、独立验证并能用于真实工业场景的功能。

不接受只增加后台类、Adapter、Model、Service、Graph、Snapshot；不接受只换颜色、字体和文案；不接受只有按钮没有功能，或只有功能没有 UI 入口。

只有涉及恢复 BSB 开发、改变 HALCON 算法含义、相机 SDK、投产协议、旧工程格式或其他需要新增授权的高风险边界时，才暂停并请求用户确认。

每轮交付必须形成“工具入口 -> 参数配置 -> 输入 -> 执行 -> 输出/图形 -> 日志/异常 -> 配方保存 -> 验证”的闭环。

## 4. 产品层规则

- 主界面持续向官方 VM 工作区靠近：顶部菜单/快捷工具条、左侧模块箱、中央流程编辑区、右上图像区、右下结果区、底部状态/日志区。
- S04 已把活动主分区改为“中央流程 + 右上图像 + 右下结果/配置”首版；当前流程仍是确定性线性节点，属于继续演进中的过渡实现，不得写成完整 VM 已完成。
- 流程顺序、分支和汇聚必须可见，不得为了 Graph 隐藏执行语义，也不得建立与真实执行引擎脱节的 Node/Edge 动画。
- S05 已交付统一模块配置工作台首版；S10-S34 已把本地图像源、图像通道、图像滤波、图像增强、图像尺寸缩放、图像镜像、极坐标展开、ROI 图像提取、双图像差分、阈值分割、动态阈值分割、Region 形态学、Region 特征筛选、Region 集合运算、Region 轮廓测量、XLD 轮廓筛选、XLD 轮廓分段、XLD 直线/圆/椭圆/旋转矩形拟合测量、双直线关系测量、卡尺边缘对测量、线性多卡尺阵列、圆周卡尺找圆、圆环壁厚/同心度测量、Blob、灰度、边缘和数值判定迁入右侧常驻“模块配置”，支持基本/运行/输入/ROI/结果/帮助、草稿、应用、撤销和试运行。上述主要参数不再依赖弹窗；Shape/HDevelop 与模型训练仍明确使用兼容页，通用 Schema 和连续试运行尚未完成。
- S06 已把统一配置的 `ROI/模板` 页升级为真实绑定工作台，并为 Blob、灰度、边缘交付兼容默认的合并 ROI 与逐 ROI 模式、集合结果和图像侧栏状态。
- S07 已把统一配置的 `输入` 页升级为类型化端口订阅编辑器，并交付首条真实 `Blob.SelectedRegion -> GrayStat/EdgeMeasure.ROI` 数据链、实例输入绑定持久化、连接校验和 HALCON Region 生命周期管理。
- S08 已把原“工程”摘要页升级为 `当前/最近/恢复/自检` 方案工作区，顶部同步方案名和保存状态；JSON 配方具备版本字段、最近列表、另存、脏状态、独立恢复副本、原子替换和单份备份。当前仍只有单方案单流程 JSON 生命周期，自动换型、权限、方案模板、多流程和 `.jw/.handle` 均未完成。
- S09 已把右上图像区升级为全局输入/当前模块输入/当前模块输出上下文工作台，显示真实来源、文件/SN、尺寸、像素值与叠加状态；“本地图像源”工具可从统一配置选择文件和 SN，输出托管 `Image` 快照，Blob/灰度/边缘可订阅并真实运行。S10-S34 已补图像通道、图像滤波、图像增强、图像尺寸缩放、图像镜像、极坐标展开、ROI 图像提取、双图像差分、阈值分割、动态阈值分割、Region 形态学、Region 特征筛选、双 Region 集合运算、Region 轮廓测量、XLD 轮廓筛选/分段/直线/圆/椭圆/旋转矩形拟合、双直线关系测量、卡尺边缘对测量、线性多卡尺阵列、圆周卡尺找圆及圆环壁厚/同心度测量，形成连续 Image/Region 链、真实双 Image/双 Region/双 XLD 汇聚、`Region -> XLD -> fitted/related XLD -> Number`、`Image -> mirrored/resized/extracted Image -> Region`、`Image -> measurement XLD -> Number` 和 `Image -> polar Image -> Region` 连续链；数组端口、几何/姿态类型、更完整的 XLD/视觉工具目录和真实分支调度仍未完成。
- S11 已为矩形/圆 ROI 增加选择控制点、移动/缩放、复制、锁定和删除保护，状态同步到图层侧栏与右侧 ROI 页并进入配方；S14 已补旋转矩形的移动/旋转/宽高控制点和多边形逐顶点编辑，并增加边界、最小尺寸/面积、自相交、端点/共线重叠保护。椭圆、圆环、屏蔽区、位置修正和多画面仍未完成。S11 同时新增真实 HALCON 均值/中值“图像滤波”工具，可从常驻配置订阅 Image、校验参数、试运行并继续输出托管 Image。
- S31 已为活动图像工具栏和 ROI 页补椭圆/圆环；S32 已把右侧 ROI 页升级为六类 ROI 的常驻数值几何属性工作台；S35 又补线/点 ROI，当前八类 ROI 均可从图像工具栏和右侧 ROI 页创建。线 ROI 支持两端点、整体移动、长度/角度显示和数值双向同步，点 ROI 支持单控制点与 X/Y 数值同步；两类均复用选择、复制、锁定、显隐、启停、命名、删除、工具绑定、边界保护、配方和撤销/重做闭环。屏蔽区和位置修正仍未完成；S33 已补单/双画面首版，多画面/缩略图仍未完成。
- S12 已把左侧平铺列表升级为分类折叠、搜索、收藏、最近使用、双击追加和拖拽指定线性位置；收藏/最近只写本机布局。中央流程仍是确定性线性顺序，不得把拖拽插入描述为自由 Graph。S12 同时新增 HALCON“阈值分割”，按 `rgb1_to_gray + threshold` 明确约定输出托管 `Region/Area/RegionCount/Threshold/ResultCode`。
- S13 已把右上图像区升级为可放大/原位恢复的图像工作区，ROI 与算法结果可独立显隐，底部图例显示真实来源端口、颜色、对象数、面积和结果状态，无图像时显示可操作空状态；隐藏叠加不得释放输出快照或破坏绑定。S13 同时新增 HALCON“Region 形态学”，支持圆形开/闭、膨胀和腐蚀，输出托管 `Region/Area/RegionCount/Operation/ResultCode` 并可继续驱动 Gray/Edge。S27 已补 Region/XLD 常驻结果图层首版；S33 已按官方限制补单/双画面、输入/输出对比、联动/独立视图和坐标体系隔离；S34 已补按左右视口隔离的双十字辅助线。仍缺多画面、缩略图、Shape 兼容叠加逐层化、完整图层属性和完整 ROI 类型。
- S14 已新增 HALCON“Region 特征筛选”，可在右侧常驻配置按面积、外接框宽度、高度或圆度设置范围，必须订阅前序 Region，并输出托管 `Region/Area/RegionCount/Feature/Range/ResultCode` 继续驱动 Gray/Edge。该模块是本项目基于 HALCON 的通用工业封装，不得冒充海康内部算法实现。
- S15 已在中央流程卡显示真实 IN/OUT 端口、类型、来源、当前值、消费者和等待/异常状态；选中模块会高亮直接上游/下游，并交付 HALCON“Region 集合运算”双输入闭环。
- S16 已把真实 `InputBindings` 映射为默认可见的二维“连接画布”，按依赖层自动放置串联、分支和汇聚模块；连接路径显示 Image/Region/Number 类型、方向、端口标签、等待/有值/错误和选中状态，并支持连接画布/顺序列表切换、缩放、中键平移、适应、自动布局和工具箱拖入。该画布只展示真实数据依赖，执行仍是确定性线性调度；不得宣称真实并行、自由拖线、Group、多流程或断点已完成。S16 同时新增 HALCON“图像增强”，支持 `scale_image` 线性灰度缩放和 `emphasize` 局部对比度增强，输出托管 `Image/Width/Height/Channels/Mode/Parameters/ResultCode` 并可驱动 Threshold/Blob/Gray/Edge。
- S17 已让真实连接路径可选，并在画布内提供常驻连接检查器，显示来源/目标端口、数据类型、本周期值和异常原因；可定位端点、打开目标输入页、清除/自动修复无效连接及适应所选依赖，所有修改写回真实 `InputBindings` 并复用类型/顺序校验。S17 同时新增 HALCON“动态阈值分割”，必须显式订阅两路不同、同尺寸的单通道 Image，按 `dyn_threshold` Light/Dark 语义输出托管 `Region/Area/RegionCount/Polarity/Offset/ResultCode` 并驱动形态学、特征筛选、Blob/Gray/Edge。仍不得宣称自由拖线、真实并行、Group 或多流程已完成。
- S18 已让连接画布和顺序列表的真实 IN/OUT 端口成为可点击入口：输入端口显示当前订阅和兼容前序输出，可直接替换/清除；输出端口列出全部真实消费者并可定位对应连接。修改写回 `InputBindings`，数值判定同步兼容字段，并复用类型、顺序、启用和双输入不同来源校验。S18 同时新增 HALCON“双图像差分”，显式订阅两路不同、同尺寸、同通道 Image，支持 `abs_diff_image` 绝对差分及 `sub_image` A-B/B-A 方向减法，输出托管 `Image/Width/Height/Channels/Mode/Parameters/ResultCode` 并驱动 Threshold、DynamicThreshold、Blob、Gray 和 Edge。仍不得宣称自由拖线、真实并行、Group 或多流程已完成。
- S19 已在编辑菜单、流程工具条、连接画布和顺序列表右键菜单提供复制、粘贴、创建副本及 `Ctrl+C/Ctrl+V`；顺序列表支持多选，副本生成新 ToolId/唯一名称，复制参数、启用状态、现存 ROI 绑定和合法连接，块内来源重映射，不复制运行结果、HImage/HRegion/XLD、耗时或错误。S19 同时新增 HALCON“Region 轮廓测量”，按 `gen_contour_region_xld + length_xld` 支持 `border/center/border_holes`，输出托管 `XLD/TotalLength/ContourCount/Mode/ResultCode` 并驱动数值判定。
- S20 已在连接画布提供可见橡皮框、Ctrl 增选/取消、选择计数及与顺序列表同步的多选状态；复制、创建副本、批量启用/停用/删除作用于真实 `flowTools/InputBindings`，删除会清理消费者并失效下游。S20 同时新增 HALCON“XLD 轮廓筛选”，按 `select_contours_xld` 的 `contour_length/maximum_extent` 范围订阅并输出托管 `XLD/TotalLength/ContourCount/Feature/Range/ResultCode`。
- S21 已让连接画布真实 OUT/IN 端口支持直接拖拽创建、替换和从已连接 IN 端重连；临时曲线、端点/类型及绿色可连接/红色拒绝原因可见，Esc、空白和非法放开保持原订阅，成功后写回 `InputBindings`、失效目标及下游并进入配方脏状态；连接仍可选择删除。S21 同时新增 HALCON“XLD 轮廓分段”，按 `segment_contours_xld` 支持 `lines/lines_circles/lines_ellipses`，输出托管 `XLD/TotalLength/SegmentCount/LineCount/ArcCount/Mode/Parameters/ResultCode` 并可驱动数值判定。
- S22 已让连接画布模块卡可拖动并按可见 24 像素网格吸附，提供水平/垂直对齐、横向/纵向等距、自动/手动状态和恢复自动布局；手动坐标使用配方可选 `GraphX/GraphY`，旧配方无坐标时继续依赖自动布局，移动不改变 `flowTools` 顺序或 `InputBindings`。S22 同时新增 HALCON“XLD 直线拟合测量”，按 `fit_line_contour_xld` 支持 `regression/huber/tukey/drop/gauss`、采样、端点裁剪、迭代和裁剪因子，输出托管拟合 `XLD/LineCount/TotalLength/MeanAngle/MaxResidual/主线端点/Algorithm/Parameters/ResultCode` 并可驱动数值判定。
- S23 已在编辑菜单、流程工具条和 `Ctrl+Z/Ctrl+Y/Ctrl+Shift+Z` 提供真实流程编辑撤销/重做，覆盖模块添加/粘贴、启停/删除、参数应用、顺序、连接创建/替换/清除/修复及节点移动/对齐/分布/自动布局；S32 把 ROI 图层纯 DTO 一并纳入快照，并覆盖图像拖动和数值几何应用。历史上限 50，不保存运行态 HImage/HRegion/XLD；恢复会释放旧工具和托管输出、保留 ToolId/选择并让模块等待重跑，新动作清空重做分支，打开/新建/恢复方案重置基线。S23 同时新增 HALCON“XLD 圆拟合测量”，按 `fit_circle_contour_xld` 支持六种代数/几何及鲁棒算法、最大采样点、最大闭合距离、端点裁剪、迭代和裁剪因子，输出托管 `XLD/CircleCount/CenterRow/CenterColumn/Radius/StartPhi/EndPhi/PointOrder/MaxResidual/Algorithm/Parameters/ResultCode`。撤销栈仍不是全量动作历史面板，不得把可视连接、手动布局或历史恢复冒充执行语义。
- S24 已在编辑菜单、流程工具条、画布和节点右键提供创建组合、解组、折叠、展开和重命名；组合容器显示成员、边界 IN/OUT、内部连接、确定性序号及异常，`GroupId/GroupName/IsGroupCollapsed` 进入配方与 S23 撤销/重做。安全折叠只压缩成员端口卡，不隐藏任何真实端口或连接，不改变 `flowTools` 顺序；完整组合复制会重映射新组，部分复制自动解组，删除到单成员会清理组合。S24 同时新增 HALCON“XLD 椭圆拟合测量”，按 `fit_ellipse_contour_xld` 支持十种算法和七项参数，输出托管 `XLD/EllipseCount/CenterRow/CenterColumn/Phi/Radius1/Radius2/StartPhi/EndPhi/PointOrder/MaxResidual/Algorithm/Parameters/ResultCode`。S24 完成时尚无断点/单步；S25 已补首版，但真实并行、多流程、执行型 Group 和完整动作历史面板仍未完成，不得把组合容器冒充并行或组合调度。
- S25 已在运行菜单、顶部/流程工具条、连接画布、节点和顺序列表右键提供断点、清除断点、单步、继续及 `F9/F10/F5`；断点在启用模块执行前命中，红色断点标记、橙色下一执行指针、组合摘要、暂停原因和下一模块同步可见。单步只执行一个启用模块后再次暂停，继续、停止、超时、遇 NG 停止和确定性顺序保持原语义；`IsBreakpoint` 进入配方及 S23 撤销/重做，运行指针不入配方。该能力仍不能中断单个 HALCON 算子，也没有跳过、运行到光标、监视表、真实并行或多流程。S25 同时新增 HALCON“XLD 旋转矩形拟合测量”，按 `fit_rectangle2_contour_xld` 支持 regression/Huber/Tukey、六项参数和至少 8 点/四边覆盖契约，输出托管 `XLD/RectangleCount/CenterRow/CenterColumn/Phi/Length1/Length2/四角点/PointOrder/OrientationState/MaxResidual/Algorithm/Parameters/ResultCode`；近正方形明确提示 90°方向等价。
- S26 已在中央流程区交付常驻调试监视器，显示真实断点列表、当前/下一模块、执行范围、暂停原因、所选模块本周期输入/输出、结果码、耗时和错误，并提供监视模块选择、定位节点、打开输入/I/O、切换/清除断点和“运行到所选”。运行到所选从真实确定性计划起点执行至目标完成，再在下一启用模块前暂停；断点仍可提前命中，运行目标和监视选择不入配方。重复的撤销/复制/组合/批量/对齐命令已从高频流程工具条收回菜单、右键和底栏，保留运行/调试/视图高频入口，画布与监视器获得真实可用空间。该监视器是基于 VM 调试习惯的产品化推导，不冒充海康内部调试器；仍不能中断单个 HALCON 算子，也没有跳过/运行到光标、变量表达式监视、真实并行或多流程。S26 同时新增 HALCON“双直线关系测量”，显式订阅两路不同 XLD，按鲁棒直线拟合结合 `angle_ll/intersection_lines/distance_pl/gen_cross_contour_xld` 输出托管 `XLD/IntersectionRow/IntersectionColumn/IntersectionValid/DirectionA/DirectionB/SignedAngle/AcuteAngle/ParallelDistance/IsParallel/IsCoincident/Relation/ResidualA/ResidualB/Parameters/ResultCode`，可驱动 NumericJudge、I/O、日志和图像叠加。
- S27 已把右上图像侧栏升级为常驻“结果图层”页，按真实运行输出列出本周期全部 Region/XLD 图层的来源工具/端口、类型、颜色、对象数、顺序和等待/有效/失效状态，并支持显隐、独显、全显/全隐、上下排序、定位生产者和打开配置。图层只持有托管输出的显示引用；显隐、排序和“清除叠加”不得释放快照、改变 `InputBindings`、执行顺序或算法结果，重跑、删除、配方切换和历史恢复后必须刷新失效引用。Shape 兼容叠加尚未纳入逐层清单，完整图层选择/属性页和多视口仍未完成。S27 同时新增 HALCON“卡尺边缘对测量”，显式订阅真实 Image，按 `gen_measure_rectangle2 + measure_pairs + close_measure` 配置旋转矩形测量域、Sigma、阈值、边缘极性和选择方式，输出托管 `XLD/PrimaryDistance/PairCount/第一边缘坐标与幅值/第二边缘坐标与幅值/IntraDistances/InterDistances/Parameters/ResultCode`，可驱动 NumericJudge、I/O、日志和图像叠加。当前成组坐标/幅值/距离仍以可读字符串输出，尚无类型化数组端口。
- S28 已为单卡尺交付图像区“卡尺域”直接编辑，并新增 HALCON“线性多卡尺阵列”；S29 已把线性阵列升级为真实模块输入图像上的起点、终点、整体、Length1/Length2 直接编辑，显示全部卡尺数量和失败项，拖动只写右侧草稿，试运行、应用/取消、配方脏状态、撤销/重做及切换/删除/方案/历史清理均复用真实闭环。整体移动到边界只平移、不压缩基线，所有卡尺受 HALCON bilinear 安全范围保护。S29 同时新增 HALCON“圆周卡尺找圆”，显式订阅 Image，沿整圆或正/反向开弧逐项执行 `gen_measure_rectangle2 + measure_pos + close_measure`，再用鲁棒 `fit_circle_contour_xld` 输出托管 `XLD/CenterRow/CenterColumn/Radius/ValidCount/FailedCount/MaxResidual/MeanAmplitude/边缘点/有效与失败索引/FailureDetails/Parameters/ResultCode` 并驱动 NumericJudge；单项缺边、低对比或越界不污染其他点，每个 handle 在全部路径关闭。S30 已把圆周找圆升级为真实输入图像上的圆心、半径、起止角、Length1/Length2 六控制点直接编辑，并显示搜索弧、全部径向卡尺及红色失败项；拖动仍只写草稿，试运行、应用/取消、配方、撤销/重做和上下文清理复用真实闭环。S30 同时新增 HALCON“圆环壁厚/同心度测量”，沿圆周逐项执行 `gen_measure_rectangle2 + measure_pairs + close_measure`，选择靠近搜索半径的边缘对并对内外点分别执行 ATukey `fit_circle_contour_xld`，输出托管 `XLD/内外圆心与半径/Mean/Min/Max/StdDevThickness/Eccentricity/ValidCount/FailedCount/残差/坐标与失败详情/Parameters/ResultCode` 并驱动 NumericJudge；局部缺边、低对比、越界和离群点保留其他有效项，每个 handle 在全部路径关闭。S31 新增 HALCON“极坐标展开”；S32 新增 HALCON“ROI 图像提取”，以 `reduce_domain` 保留输入矩阵及原坐标，以 `crop_domain` 裁剪有效域外接框并输出 `OffsetRow/OffsetColumn`，托管 Image 可继续驱动 Threshold/Blob/Gray/Edge，裁剪输出会隔离原图坐标 ROI 叠加。逐项集合仍为可读字符串而非类型化数组；圆环壁厚图像内测量域、通用测量几何和几何/姿态端口尚未完成。
- S33 已按官方 VM 双画面限制交付单/双画面切换：左视口只能选择当前模块真实 Image 输入端口，右视口保留当前模块输出/兼容上下文；两侧具备独立来源、尺寸、像素/叠加状态、适应、1:1、放大/恢复和可选联动缩放平移，联动按归一化中心与可视比例同步，不把输入坐标 ROI 误叠到缩放输出。S33 同时新增 HALCON“图像尺寸缩放”，以 `FullDomain + zoom_image_size` 配置目标宽高和五种插值，输出完整矩形 domain 的托管 `Image/Width/Height/Channels/ScaleRow/ScaleColumn/Interpolation/Parameters/ResultCode` 并驱动 Threshold/Blob/Gray/Edge；输出属于新坐标系，不能直接冒充原图坐标。
- S34 已按官方 VM“十字辅助线 1/2”习惯在图像工作区交付两组独立辅助线；每个左右视口分别保存启用、中心 X/Y、颜色、线宽和锁定，支持居中、未锁定时图像内拖动及数值双向同步，显示状态进入配方但不改变算法快照、结果图层或 `InputBindings`。S34 同时新增 HALCON“图像镜像”，按 `mirror_image` 支持 `row/column/diagonal`，其中 `row` 为上下翻转、`column` 为左右翻转、`diagonal` 沿主对角线交换行列并在非方图上交换宽高；输出托管 `Image/Width/Height/Channels/Mode/Parameters/ResultCode` 并驱动 Threshold/Blob/Gray/Edge。十字配置使用可折叠的图像区内面板以保护低高度布局；尚无十字线撤销/重做、标签和快捷键。
- S35 已新增 HALCON“图像旋转”，显式订阅 Image，按 `rotate_image` 围绕图像中心逆时针旋转并支持 HALCON 20.11 的五种插值；输出托管 `Image/Width/Height/Channels/Angle/Interpolation/Parameters/ResultCode` 并驱动 Threshold/Blob/Gray/Edge。0°/90°/180°/270°和任意角、非方图定位像素、灰度/RGB、输入 domain 被忽略、输出 domain、非法参数、上游释放、旧 Image 释放、配方和下游均已验证；90°/270°交换宽高，旋转输出隔离原图坐标 ROI 叠加。S35 与线/点 ROI 一同完成新 Git 批次第 10 次交付；BSB 按用户指令未实施/未验证。
- VM5 流程区长期必须支持网格布局、多模块汇聚、自动串/并连排列和组合折叠；S24 只完成组织型 Group 首版，未建立真实执行语义前不得宣称并行或执行型 Group 已完成。
- 每个产品工具逐步统一具备 ToolId、实例名、类型、分类、说明、启用/配置/运行状态、参数、输入、输出、ROI、图形结果、OK/NG、错误、耗时、独立试运行、保存、复制、重命名、删除和帮助。
- HALCON 原子算子应围绕工业任务封装，不机械暴露给现场用户。

## 5. 稳定边界

未经本轮明确授权和专项验证，不得随意改变：

- 已验证 HALCON 算法结果、算子参数含义和坐标约定；
- 相机 SDK、采集接口和底层稳定行为；
- 已投产通讯协议、字段语义和握手时序；
- 旧工程、既有配方和稳定数据；
- `.jw`、`.handle` 等既有工程格式的兼容能力（当前仓库尚无实现证据，新增实现时必须补测试）。

允许通过包装、适配和迁移改造产品层，但必须写明兼容策略、验证方式和回退方案。

## 6. 文档维护

每轮完成后必须按实际代码同步更新：

- `docs/PRODUCT_GOAL.md`
- `docs/PRODUCT_ROADMAP.md`
- `docs/BSB_REQUIREMENTS.md`
- `docs/SPRINT_BACKLOG.md`
- `docs/PRODUCT_GAP_ANALYSIS.md`

状态只使用：`已验证实现`、`部分实现`、`未实现`、`提议`、`开发中`。不得把尚未实现或只有内部结构的能力标记为完成。

## 7. 强制验证与交付报告

每轮至少执行：

```powershell
& 'D:\vsanzhuangbao\MSBuild\Current\Bin\MSBuild.exe' 'D:\codex\codex1\HalconWinFormsDemo.sln' /p:Configuration=Debug /p:Platform=x64 /m
& 'D:\vsanzhuangbao\MSBuild\Current\Bin\MSBuild.exe' 'D:\codex\codex1\HalconWinFormsDemo.sln' /p:Configuration=Release /p:Platform=x64 /m
```

并验证启动、现有 HALCON 主流程、旧配方读取、本轮工业场景、异常路径和 UI 可见变化。BSB 未暂停时还必须验证本轮 BSB 场景；BSB 暂停期间必须明确写“按用户指令未实施/未验证”。没有真实旧工程样本时必须明确写“缺少样本，未验证”，不能以编译通过代替。

完成报告必须包含 UI 变化、功能入口、完整操作步骤、本轮工业场景结果、BSB 状态、编译/启动结果、修改文件、未完成项、文档更新和下一轮双线建议。

## 8. Git 规则

- 工作区文件仍在实现时即时保存；Git 改为批量提交和上传，不再为每个小改动频繁 commit/push。
- 每完成一个可独立验证、对用户可说明的小交付记 1 次，记录到 `docs/GIT_BATCH_LOG.md`；累计到 10 次后统一检查差异、敏感信息和构建产物，创建一个或少量有意义的提交并推送 `origin/main`，随后计数归零。
- 纯格式调整、失败尝试和未验证中间状态不计次，也不得以“已完成”推送。只有用户明确要求提前上传，或即将进行高风险/破坏性操作且确需安全回退点时，才可不足 10 次提前提交推送，并在批次记录说明原因。
- `bin/`、`obj/`、`.vs/`、运行日志、运行配方和本机状态不得提交；提交信息描述用户能力，不只描述内部类名。

## 9. 官方资料与研究规则

- 产品目标以 `docs/VM_OFFICIAL_REFERENCE_SPEC.md` 中记录的海康官网、VM5.0 发布资料和官方用户手册为依据。
- 重大主界面、流程、模块配置、资源管理或运行界面 Sprint 开始前，必须核对对应官方页面/手册，区分“官方确认”“界面图确认”“产品化推导”和“待实机确认”。
- 官方资料只能作为研究引用，不得把下载的手册、截图、Logo、图标或其他海康资产提交进仓库。
- 官方资料与旧目标描述冲突时，优先修正旧描述；但用户最新明确指令、HALCON 核心边界和现有稳定兼容要求仍具有更高优先级。
