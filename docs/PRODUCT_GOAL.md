# 产品长期目标

更新日期：2026-07-23
状态：长期有效

## 产品定义

将当前软件持续改造成：

> 海康机器人视觉平台 VM / VisionMaster 类型的完整工业视觉平台体验 + HALCON 算法内核。

VM 是产品体验和用户习惯参照；HALCON 是全部视觉算子、图像处理、定位、测量、模板、标定和坐标变换的算法引擎。BSB 电池产线是长期核心验收场景。

当前执行约束（2026-07-13）：用户要求先推进视觉软件 UI、VM 操作习惯和通用功能，BSB 功能等待后续明确指令。因此 BSB 仍保留在长期目标和需求基线中，但当前 Sprint 不实施、不宣称完成。

迭代授权：后续默认由开发代理自行选择并执行 A/B 双线 Sprint，不再逐轮等待确认；每轮仍必须明确 A 线可见 UI/UX 和 B 线真实工业功能，并完成验证和文档。自 2026-07-16 起，Git 按 10 个已验证小交付统一提交/推送，计数见 `docs/GIT_BATCH_LOG.md`，不再为每个小改动上传。恢复 BSB 或触碰 HALCON 算法含义、相机 SDK、投产协议、旧工程格式等高风险边界时仍需单独确认。

当前产品基线（S25，Git 批次 `10/10`，按规则触发本轮统一提交/推送）：活动 WPF 已形成顶部菜单/快捷工具条、左侧模块箱、中央流程、右上 HALCON 图像工作区、右下模块结果/配置/方案和底部状态/历史。中央默认“连接画布”把真实 `InputBindings` 映射为有方向的 Image/Region/XLD/Number 曲线，支持连接检查、端口导航、OUT→IN 创建/替换/重连、橡皮框/Ctrl 多选、复制/副本、批量启停/删除、可见网格、用户节点坐标、50 项撤销/重做和组织型 Group。S25 在运行菜单、顶部/流程工具条、画布/节点/列表右键增加断点、清除断点、单步、继续及 F9/F10/F5；断点在启用模块执行前命中，红色断点、橙色下一执行指针、组合摘要、暂停原因和下一模块可见，单步只执行一个启用模块，断点随配方和撤销/重做保存。右侧常驻配置新增“XLD 旋转矩形拟合测量”，连续链可达 `Image -> Region -> XLD -> fitted rectangle XLD -> Length1/Length2/Number`；HALCON `fit_rectangle2_contour_xld` 输出托管矩形、数量、中心、方向、两边半长、四角点、点序、方向稳定性、实际残差、算法和参数，并进入青色叠加、I/O、结果、日志、配方和 NumericJudge。矩形、圆、旋转矩形和多边形 ROI 具备直接编辑、复制、锁定、几何保护和配方闭环。Shape/HDevelop 仍走兼容配置，执行仍为确定性线性调度；单个 HALCON 算子不可中断，真实并行、执行型 Group、监视表/运行到光标、椭圆 ROI/圆环/屏蔽区、数组及几何/姿态类型、多流程、相机、坐标、工业通讯、权限、`.jw/.handle` 和 BSB 仍未完成。

官方产品基线见 `docs/VM_OFFICIAL_REFERENCE_SPEC.md`。S25 的流程断点/单步属于依据官方 VM 调试/运行习惯做出的本项目产品化实现，不声称复刻海康内部调度器。HALCON 20.11 官方文档与本机程序集反射确认 `fit_rectangle2_contour_xld` 精确 .NET 签名、`regression/huber/tukey`、至少 8 点与四边覆盖要求，以及 `Row/Column/Phi/Length1/Length2/PointOrder` 语义；合成旋转矩形经三算法得到中心 `(120,180)`、方向约 `31.513°`、半边长 `60/25 px`，物理 UI 链得到中心 `(210,320)`、方向 `25.783°`、半边长 `140/70.003 px`。空/短/直线退化和非法参数受控，近正方形明确显示“90°方向等价”。当前画布已有真实拖线、用户布局、撤销/重做、组织型 Group 和首版断点/单步，但仍没有真实并行调度、执行型 Group、多流程、全量编辑历史或调试监视表；完整 ROI/数组/几何/模型、Shape/HDevelop、自动换型和权限仍不完整，不能据此宣称完整 VM 工作区已经完成。

## 官方依据

- [海康机器人 VM 算法平台](https://www.hikrobotics.com/cn/machinevision/visionmaster/)：图形化拖拽、SDK、算子设计、定位/测量/识别/检测、外部资源和 3D。
- [VM5.0 官方发布资料](https://www.hikrobotics.com/cn/publish/)：网格化布局、多模块汇聚、自动串/并连排列、组合折叠和通用框架重塑。
- [海康 VM3D 官方用户手册](https://pinfo.hikrobotics.com/hkws/unzip/20240919112534_17943_doc/index.html)：主界面、流程、模块配置、资源、通信、运行界面设计、调试、权限和运行策略。其通用框架作为 VM 家族界面证据，3D 算法目录不等于本项目当前完成范围。
- [海康 VM 算法开发平台官方用户手册 PDF](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)：方案生命周期、模块参数和 ROI 位置/尺寸/角度编辑、复制/删除及屏蔽区等官方行为依据。
- [官方“图像源”模块说明](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/GUID-8D7949F0-EB91-4794-BD3C-D4182726D931.html)：模块输入可选择来源、订阅控制值，输出按类型提供给后续模块。
- [官方“相机映射”说明](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/GUID-CC3DB2DB-FC0B-4410-A58B-DDAC72E2360B.html)：后续模块可订阅前序模块输出，并要求数据类型和坐标语义保持一致。
- [MVTec HALCON 20.11.4 `dyn_threshold`](https://www.mvtec.com/doc/halcon/2011/en/dyn_threshold.html)：S17 两路单通道输入、Light/Dark 条件、Offset 范围及连接后处理的算法语义依据。
- [MVTec HALCON 20.11.4 `sub_image`](https://www.mvtec.com/doc/halcon/2011/en/sub_image.html)：S18 方向减法 `(g1-g2)*Mult+Add`、溢出裁剪与 .NET 签名依据。
- [MVTec HALCON 20.11.4 `abs_diff_image`](https://www.mvtec.com/doc/halcon/2011/en/abs_diff_image.html)：绝对差分 `abs(g1-g2)*Mult`、`Mult > 0` 与可后接阈值分割的官方算子依据；20.11 .NET 精确签名另由本机程序集反射确认。
- [MVTec HALCON 20.11.4 `gen_contour_region_xld`](https://www.mvtec.com/doc/halcon/2011/en/gen_contour_region_xld.html)：S19 从 Region 生成 `center/border/border_holes` XLD 的官方语义依据。
- [MVTec HALCON 20.11.4 `length_xld`](https://www.mvtec.com/doc/halcon/2011/en/length_xld.html)：S19 按轮廓返回长度元组并汇总 `TotalLength` 的官方依据。
- [MVTec HALCON 20.11.4 `select_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/select_contours_xld.html)：S20 按轮廓长度或最大跨度筛选 XLD，并以 `Min1/Max1` 定义范围的官方依据。
- [MVTec HALCON 20.11.4 `segment_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/segment_contours_xld.html)：S21 将 XLD 按直线、圆弧或椭圆弧模式分段，并读取轮廓近似分类的官方依据。
- [MVTec HALCON 20.11 `fit_line_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_line_contour_xld.html)：S22 对每条直线轮廓执行 regression/Huber/Tukey/drop/Gauss 拟合并输出端点、法向和距离的官方依据。
- [MVTec HALCON 20.11 `fit_circle_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_circle_contour_xld.html)：S23 对每条整圆/圆弧轮廓执行代数/几何及 Huber/Tukey 鲁棒拟合，配置最大闭合距离并输出圆心、半径、起止角和点序的官方依据。
- [MVTec HALCON 20.11 `fit_ellipse_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_ellipse_contour_xld.html)：S24 对每条椭圆/椭圆弧轮廓执行代数、焦点、几何、Voss 及 Huber/Tukey 鲁棒拟合，并输出中心、方向、长短半轴、起止角和点序的官方依据。
- [MVTec HALCON 20.11 `fit_rectangle2_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_rectangle2_contour_xld.html)：S25 对覆盖四边的 XLD 执行回归/Huber/Tukey 旋转矩形拟合，并输出中心、方向、两边半长和点序的官方依据。

## 目标用户体验

长期使用海康 VM 的视觉工程师第一次打开本软件时，应能依靠原有习惯快速完成：

1. 新建或打开工程、选择配方和切换调试/生产模式；
2. 从分类工具箱添加采集、算法、计算、坐标、通讯、输出和 BSB 工具；
3. 在中央网格流程区搭建串联、分支、汇聚、Group 和多流程逻辑，配置、排序、启停和运行工具；
4. 双击模块进入统一配置工作台，完成基本/运行参数、输入订阅、输出、ROI/模板、结果显示和试运行；
5. 在右侧图像调试区完成模块/全局图像切换、缩放、平移、ROI/模板编辑和结果叠加；
6. 配置相机、PLC 和通讯设备，并完成手动测试与流程触发；
7. 在底部统一查看日志、报警、变量、历史、良率和节拍；
8. 完成 BSB 工站换型、Mark、点集、坐标补偿、焊接点输出、防呆和追溯。

## 非目标

- 不是通用 Graph 编辑器，也不以 Node/Edge 动画数量衡量完成度。
- 不是只模仿配色和外观。
- 不是保留旧工作方式、只在外层增加一个 VM 标签页。
- 不是把 HALCON 原子算子和 HDevelop 文件直接散乱暴露给用户。
- 不是只新增后台抽象而没有可见入口和业务闭环。

## 目标工作区

### 顶部菜单与快捷工具条

文件、设置、工具、系统、帮助菜单；方案新建/打开/保存/另存、撤销/重做、相机、控制器、通信、全局变量、单次/连续/暂停/停止、当前方案、角色和系统状态。

### 左侧工具箱

采集、图像处理、定位、测量、标定、坐标转换、逻辑计算、数据处理、通讯、输出、脚本和 BSB 专用工具；支持分类、搜索、收藏、最近使用、双击和拖拽添加。

### 中央流程编辑区

流程是开发主界面的中心。使用网格、模块卡和有方向的连线清晰显示串联、并联、分支、汇聚和 Group；支持多流程标签、自动排列、组合折叠、缩放/平移。每个实例显示名称、类型、启用、配置、运行、OK/NG、耗时和错误；支持选择、编辑、复制、删除、重命名、运行当前、运行到此和从此运行。任何连线都必须对应真实执行或数据语义。

### 右上图像区

支持模块输入/输出图像和全局图像、单/双画面、缩放、平移、适应、原始大小、十字线、鼠标坐标和像素值、矩形/圆/旋转矩形/椭圆/圆环/多边形/线/点 ROI、模板、图形与文本叠加、原图/中间图/结果图和多相机切换，并为深度图和点云保留真实数据入口。

### 右下结果区

选中模块即显示输出参数、结果表、OK/NG、错误、耗时、SDK/端口信息和帮助；流程模式显示全局结果。

### 统一模块配置工作台

双击模块进入基本参数、运行参数、输入订阅、输出、ROI/模板、结果显示、模型和帮助页，并能试运行、应用、确定和取消。当前右侧 Inspector 作为快速摘要和迁移入口保留；目标不是消灭所有配置窗口，而是消灭散乱、不统一、没有输入输出和试运行闭环的旧弹窗。

### 底部信息区

开发页保留状态/日志和总耗时；运行日志、通讯日志、报警、变量、运行历史、OK/NG、节拍、良率和系统诊断可通过可停靠面板或独立页面查看。

## HALCON 工具产品化原则

每个工具逐步统一具备 ToolId、实例名、类型、图标、分类、说明、启用/配置状态、参数 Schema、输入输出端口、数据订阅、ROI、图形结果、运行状态、OK/NG、错误、耗时、独立试运行、参数保存、复制、重命名、删除和帮助。

工业任务封装优先于原子算子暴露。通用阶段优先完善定位、测量、预处理、标定、逻辑和通信工具；BSB 恢复后，“极柱寻址”“双 Mark 补偿”“焊接点输出”等应成为稳定工具，而不是让用户拼接大量底层 HALCON 调用。

## 永久开发原则

1. 每轮同时交付可见 UI 升级和真实工业能力。
2. UI 与功能必须形成从配置到验证的闭环。
3. 优先级基于 BSB 现场价值、频率、项目闭环重要性、当前短板和风险。
4. 保护 HALCON 结果、相机 SDK、投产协议和旧工程数据。
5. 小步、可验证、可回退，但每一步必须朝完整 VM 产品推进。
6. 文档状态必须由实际代码和验证结果支撑。

## 最高验收标准

每轮都必须回答：VM 工程师能否快速上手；能否用熟悉方式添加、配置、运行工具；是否在统一工作区完成采集、算法、计算、通讯和输出；UI 是否肉眼可见变化；是否新增真实工业价值；BSB 开发启用时是否新增真实 BSB 价值；是否减少弹窗和割裂窗口；是否缩短搭建调试路径；是否更适合现场长期使用；是否比上一轮更接近完整 VM 产品。
