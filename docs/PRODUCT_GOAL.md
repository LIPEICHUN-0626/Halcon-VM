# 产品长期目标

更新日期：2026-08-08
状态：长期有效

## 产品定义

将当前软件持续改造成：

> 海康机器人视觉平台 VM / VisionMaster 类型的完整工业视觉平台体验 + HALCON 算法内核。

VM 是产品体验和用户习惯参照；HALCON 是全部视觉算子、图像处理、定位、测量、模板、标定和坐标变换的算法引擎。BSB 电池产线是长期核心验收场景。

当前执行约束（2026-07-13）：用户要求先推进视觉软件 UI、VM 操作习惯和通用功能，BSB 功能等待后续明确指令。因此 BSB 仍保留在长期目标和需求基线中，但当前 Sprint 不实施、不宣称完成。

迭代授权：后续默认由开发代理自行选择并执行 A/B 双线 Sprint，不再逐轮等待确认；每轮仍必须明确 A 线可见 UI/UX 和 B 线真实工业功能，并完成验证和文档。自 2026-07-16 起，Git 按 10 个已验证小交付统一提交/推送，计数见 `docs/GIT_BATCH_LOG.md`，不再为每个小改动上传。恢复 BSB 或触碰 HALCON 算法含义、相机 SDK、投产协议、旧工程格式等高风险边界时仍需单独确认。

当前产品基线（S35，新 Git 批次 `10/10`，本轮统一提交/上传）：活动 WPF 已形成顶部菜单/快捷工具条、左侧模块箱、中央流程、右上 HALCON 图像工作区、右下模块结果/配置/方案和底部状态/历史。中央默认“连接画布”把真实 `InputBindings` 映射为有方向的 Image/Region/XLD/Number 曲线，支持连接检查、端口导航、OUT→IN 创建/替换/重连、橡皮框/Ctrl 多选、复制/副本、批量启停/删除、可见网格、用户节点坐标、50 项撤销/重做、组织型 Group、断点/单步、常驻调试监视和运行到所选。右上结果图层页管理本周期真实 Region/XLD 输出；单/双画面左侧严格来自当前模块真实 Image 输入端口，右侧显示模块输出或兼容上下文，两侧支持来源/尺寸/像素/叠加状态、适应、1:1、放大及联动/独立缩放平移。矩形、圆、旋转矩形、多边形、椭圆、圆环、线和点共八类 ROI 均可在图像区创建、控制点编辑、复制、锁定、删除、显隐、启停、命名、绑定、保存和数值编辑；S35 的线 ROI 具有两端点/整体移动/长度/角度，点 ROI 具有单控制点和 X/Y 精确编辑。S31-S35 的极坐标展开、ROI 图像提取、图像尺寸缩放、图像镜像和图像旋转形成连续托管 Image 链；`rotate_image` 已按 0°/90°/180°/270°、任意角、非方图定位像素、五种插值及 domain 语义核对。Shape/HDevelop 仍走兼容配置，执行仍为确定性线性调度；Shape 兼容叠加尚未进入逐层清单，圆环壁厚工具几何仍靠数值配置，成组输出仍是可读字符串而非类型化数组；单个 HALCON 算子不可中断，真实并行、执行型 Group、运行到光标/变量表达式监视、屏蔽区、位置修正、通用几何/姿态类型、多流程、相机、坐标、工业通讯、权限、`.jw/.handle` 和 BSB 仍未完成。

官方产品基线见 `docs/VM_OFFICIAL_REFERENCE_SPEC.md`。官方 VM 手册确认线/点基础 ROI，并明确图形 ROI 可设置屏蔽区且运行时不检测被屏蔽部分；S35 已完成线/点的真实 UI 和配方闭环，S36 将继续屏蔽区。MVTec HALCON 20.11 官方文档和本机运行确认 `rotate_image` 围绕图像中心逆时针旋转、90°/270°交换宽高、输入 domain 被忽略及输出 domain 交集语义。旧 v1 与 S09-S35 共 28 组功能回归全部通过，1942×1222 线/点 ROI 与旋转常驻配置 UI、原图 ROI/HALCON 旋转图像导出通过；全仓库 XAML 审计得到 558 个事件引用、238 个唯一处理入口且 0 缺失。Debug/Release x64 均为 0 警告、0 错误且双版本启动响应。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本或设备仍未验证；当前工作区仍不等于完整 VM。

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
- [MVTec HALCON 20.11 `intersection_lines`](https://www.mvtec.com/doc/halcon/2011/en/intersection_lines.html)：S26 计算两条无限直线交点，并区分完全重合 `IsOverlapping` 的官方依据。
- [MVTec HALCON 20.11 `angle_ll`](https://www.mvtec.com/doc/halcon/2011/en/angle_ll.html)：S26 输出受端点方向影响的两线有向夹角，并派生无向锐角的官方依据。
- [MVTec HALCON 20.11 `distance_pl`](https://www.mvtec.com/doc/halcon/2011/en/distance_pl.html)：S26 对平行线计算点到无限直线正交距离的官方依据。
- [MVTec HALCON 20.11 `gen_cross_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/gen_cross_contour_xld.html)：S26 为有效交点生成可显示十字 XLD 的官方依据。
- [MVTec HALCON 20.11 `gen_measure_rectangle2`](https://www.mvtec.com/doc/halcon/2011/en/gen_measure_rectangle2.html)：S27 从旋转矩形测量域创建 measure handle，并约束图像尺寸、插值、宽度和 Sigma 的官方依据。
- [MVTec HALCON 20.11 `measure_pairs`](https://www.mvtec.com/doc/halcon/2011/en/measure_pairs.html)：S27 按边缘极性与选择方式返回成对边缘坐标、幅值、对内/对间距离的官方依据。
- [MVTec HALCON 20.11 `measure_pos`](https://www.mvtec.com/doc/halcon/2011/en/measure_pos.html)：S29 沿旋转矩形主轴按极性和选择方式提取单边缘位置、幅值与间距，并约束 Sigma 和测量方向的官方依据。
- [MVTec HALCON 20.11 `close_measure`](https://www.mvtec.com/doc/halcon/2011/en/close_measure.html)：S27 在成功、NG 和异常路径关闭 measure handle 的生命周期依据。
- [MVTec HALCON 20.11 `measure_pairs`](https://www.mvtec.com/doc/halcon/2011/en/measure_pairs.html)与[`fit_circle_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_circle_contour_xld.html)：S30 逐径向卡尺获取内外边缘对、分别鲁棒拟合内外圆并派生壁厚统计与偏心量的算法依据。
- [MVTec HALCON `polar_trans_image_ext`](https://www.mvtec.com/doc/halcon/2511/en/polar_trans_image_ext.html)：S31 已按圆心、角度、内外半径、输出尺寸和插值把指定圆环弧展开为矩形 Image，并用完整 360°去重、顺逆方向和定位标记专项确认输出列/行语义。
- [MVTec HALCON 20.11 `reduce_domain`](https://www.mvtec.com/doc/halcon/2011/en/reduce_domain.html)：S32 以输入图像 domain 与 ROI/Region 的交集限定有效域，并保持原图矩阵尺寸和坐标。
- [MVTec HALCON 20.11 `crop_domain`](https://www.mvtec.com/doc/halcon/2011/en/crop_domain.html)：S32 将有效域裁成最小外接矩形，并通过 `OffsetRow/OffsetColumn` 明确局部输出相对原图的坐标偏移。
- [MVTec HALCON 20.11 `zoom_image_size`](https://www.mvtec.com/doc/halcon/2011/en/zoom_image_size.html)：S33 按目标宽高和插值缩放完整图像矩阵，并显式输出行列比例及新坐标系。
- [MVTec HALCON 20.11 `rotate_image`](https://www.mvtec.com/doc/halcon/2011/en/rotate_image.html)：S35 围绕图像中心逆时针旋转、90°/270°宽高交换和 domain 语义的官方依据。
- [MVTec HALCON 20.11 `affine_trans_image`](https://www.mvtec.com/doc/halcon/2011/en/affine_trans_image.html)：S36 任意二维仿射、五种插值、输出尺寸适应、行列矩阵坐标与 domain 语义的官方依据。

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
