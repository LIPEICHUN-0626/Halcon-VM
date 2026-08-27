# 产品长期目标

更新日期：2026-08-27
状态：长期有效

## 产品定义

将当前软件持续改造成：

> 海康机器人视觉平台 VM / VisionMaster 类型的完整工业视觉平台体验 + HALCON 算法内核。

VM 是产品操作逻辑、信息架构和用户习惯参照；HALCON 是全部视觉算子、图像处理、定位、测量、模板、标定和坐标变换的算法引擎。界面可以形成项目自己的克制型工业设计语言，但不得偏离 VM 的任务组织，也不复制海康商标、图标、截图或逐像素皮肤。BSB 电池产线是长期核心验收场景。

平台扩展能力是长期产品定义的一部分：必须支持把参数传入受信任的 C# 代码或 DLL 入口并获得类型化输出，同时让该能力像普通 VM 工具一样进入工具目录、端口订阅、模块配置、试运行、日志、配方、版本和下游流程。现有 HDevelop 文件执行仅是 HALCON 兼容入口，不等于通用脚本/自定义模块已经完成。

当前执行约束（2026-07-13）：用户要求先推进视觉软件 UI、VM 操作习惯和通用功能，BSB 功能等待后续明确指令。因此 BSB 仍保留在长期目标和需求基线中，但当前 Sprint 不实施、不宣称完成。

迭代授权：后续默认由开发代理自行选择并执行 A/B 双线 Sprint，不再逐轮等待确认；每轮仍必须明确 A 线可见 UI/UX 和 B 线真实工业功能，并完成验证和文档。自 2026-07-16 起，Git 按 10 个已验证小交付统一提交/推送，计数见 `docs/GIT_BATCH_LOG.md`，不再为每个小改动上传。恢复 BSB 或触碰 HALCON 算法含义、相机 SDK、投产协议、旧工程格式等高风险边界时仍需单独确认。

当前产品基线（S75 已验证，Git 批次 `10/10`）：活动 WPF 已具备紧凑顶部应用栏、70 工具/9 类单一分类入口、约 280×68 的从上到下真实依赖画布、可操作的上下聚合锚点、简洁直线/直角折线、任务聚焦/全部依赖、可拖动 24 px 导航视窗和执行跟随、单/双图像工作区、右侧当前任务检查器、方案、生产运行页和可折叠底部历史。活动页面已去除把 VM/VisionMaster 当作本软件品牌的文字，操作逻辑仍以官方 VM 为参照并使用项目自己的工业视觉语言。点击锚点才展开精确端口，单端口可直接拖接、多端口必须明确选择，创建/替换/清除写回真实 `InputBindings` 且不恢复端口墙。S45-S61 已建立受管历史、渐进展开工作区、通用 TCP/文本/数值/逻辑、受信任 C#/DLL、变量、资源和命名标量 Schema；S62-S75 又把 HALCON 2D 位置修正、每实例 Shape、多目标候选、正式 Pose2D/Point2D/Point2D[]/Line2D/Circle2D/Ellipse2D、点集统计/直线/圆/椭圆拟合、点到椭圆测量、类型化几何关系、锚点插入、双向补链、连接问题和本机流程片段做成真链。历史仍只是本地快照，TCP 仅本机 loopback，脚本仅受信任工程扩展；这些能力不冒充 MES、数据库、PLC、BSB、合规归档、安全沙箱、多流程事务、通用数组/完整几何 SDK 或插件市场。

2026-08-21 至 2026-08-27 的产品方向纠偏继续有效：功能增加不等于把更多按钮永久铺在首屏，但“安静”也不等于隐藏工具能力。S62 把默认模式从 8 个常用工具恢复为完整分类目录，S65-S75 增至 70 个真实工具；左侧仍只有一个可见分类入口。S71-S75 已把流程改为约 280×68 的纵向紧凑卡、可操作上下连接小点、简洁折线、步骤记录子页面、任务聚焦和可拖动导航视窗；活动页面不再展示 VM/VisionMaster 品牌。下一差距是进一步把与当前任务无关的长流程卡片降噪，同时继续补齐可组合几何、采集、标定、识别、设备和输出能力；后续不得让能力以按钮墙、端口墙或品牌仿冒形式回流。

官方产品基线见 `docs/VM_OFFICIAL_REFERENCE_SPEC.md`。S58-S75 的脚本、变量、资源、Schema、位置修正、Shape、端口监视、双向端口导航、连接任务与类型化点/线/圆/椭圆几何均依据官方 VM 模块化、输入订阅、当前对象配置、调试和资源习惯形成，但实现、控件、品牌和资产均为本项目自制。S62-S75 以 HALCON `vector_angle_to_rigid`、Shape Model、XLD 拟合/交点/距离算子和 Pose2D/Point2D/Point2D[]/Line2D/Circle2D/Ellipse2D 建立同图像定位、ROI 跟随及几何真链。Legacy 与 S09-S75 最终 `68/68`、1938×1158 与 1366×768 实体 WPF UI、70 工具/9 类、987 个当前审计事件引用/411 个唯一入口/0 缺失通过；Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。图片采用 WPF 渲染，HALCON Host 空图区域不能冒充完整 HALCON 实屏验证；HALCON 功能由全量回归独立覆盖。真实外部产线图像/模板、生产 DLL 依赖图、相机、网络设备和 `.jw/.handle` 仍缺样本或设备；通用数组/Geometry/HObject、正式 SDK、ROI 继承、多流程、权限和完整标定/跨坐标仍未完成，当前工作区仍不等于完整 VM。

## 官方依据

- [海康机器人 VM 算法平台](https://www.hikrobotics.com/cn/machinevision/visionmaster/)：图形化拖拽、SDK、算子设计、定位/测量/识别/检测、外部资源和 3D。
- [VM5.0 官方发布资料](https://www.hikrobotics.com/cn/publish/)：网格化布局、多模块汇聚、自动串/并连排列、组合折叠和通用框架重塑。
- [海康 VM3D 官方用户手册](https://pinfo.hikrobotics.com/hkws/unzip/20240919112534_17943_doc/index.html)：主界面、流程、模块配置、资源、通信、运行界面设计、调试、权限和运行策略。其通用框架作为 VM 家族界面证据，3D 算法目录不等于本项目当前完成范围。
- [海康 VM 算法开发平台官方用户手册 PDF](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)：方案生命周期、模块参数和 ROI 位置/尺寸/角度编辑、复制/删除及屏蔽区等官方行为依据。
- [同一官方 VM 用户手册 PDF](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)：全局脚本/全局变量可定义输入输出参数；自定义模块生成器可生成 C#/C++ 工程，并将编译后的 DLL/XML 作为工具进入模块目录。
- [海康机器人视觉学院 VM 课程](https://www.hikrobotics.com/cn/machinevision/service/vcollege/video/?module=%E7%AE%97%E6%B3%95%E5%B9%B3%E5%8F%B0)：官方课程目录包含“脚本和全局变量”以及自定义算法工具/算子封装二次开发，作为脚本模块长期需求的补充依据。
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
- [MVTec HALCON 20.11 `projective_trans_image`](https://www.mvtec.com/doc/halcon/2011/en/projective_trans_image.html)：S37 投影变换、插值、尺寸适应和 domain 语义的官方依据；本机 20.11 精确 .NET 签名另由程序集反射与运行夹具确认。
- [MVTec HALCON `edges_sub_pix`](https://www.mvtec.com/doc/halcon/2011/en/edges_sub_pix.html)：S38 亚像素边缘 XLD、Filter、Alpha 与 Low/High 语义的官方依据；本机 HALCON 20.11 反射和专项结果已确认精确签名、九种 Filter、reduced domain 与 RGB 产品化策略。
- [MVTec HALCON 20.11 `union_adjacent_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/union_adjacent_contours_xld.html)：S39 相邻轮廓连接的端点绝对/相对距离、属性模式和点序反转语义依据；本机程序集与专项夹具已确认精确 .NET 签名、短间隙/超阈值、反向端点、空对象和托管生命周期。
- [MVTec HALCON 20.11 `smooth_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/smooth_contours_xld.html)：S40 XLD 轮廓平滑依据；本机已确认 `HOperatorSet.SmoothContoursXld(HObject,out HObject,HTuple)`、`NumRegrPoints >= 3` 且为奇数、3/5/7/9、短/多/空轮廓、长度变化、输入所有权和输出点属性不保留。
- [MVTec HALCON 20.11 `affine_trans_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/affine_trans_contour_xld.html)：S41 已实现 XLD 仿射变换的依据；官方明确其应用任意二维仿射矩阵、使用 `affine_trans_pixel` 坐标体系并给出标准像素中心的 ±0.5 补偿，本机 20.11 精确签名为 `HOperatorSet.AffineTransContourXld(HObject,out HObject,HTuple)`。
- [MVTec HALCON 20.11 `projective_trans_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/projective_trans_contour_xld.html)：S42 已实现 XLD 透视变换的依据；官方给出九元素投影矩阵、`affine_trans_pixel` 坐标体系及相同的 ±0.5 补偿约定，本机精确签名与正常/退化/生命周期专项均已通过。
- [MVTec HALCON 20.11 `crop_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/crop_contours_xld.html)：S43 已实现 XLD 矩形裁剪的依据；本机 20.11 已确认精确 .NET 签名，穿越四边、内外/相切、开闭/多/空、两种闭合策略、所有权和托管生命周期专项均已通过。
- [MVTec HALCON `length_xld`](https://www.mvtec.com/doc/halcon/2011/en/length_xld.html) 与 [`select_obj`](https://www.mvtec.com/doc/halcon/2011/en/select_obj.html)：S44 长度分级与 S45 长度排序/索引选择依据；前者按轮廓顺序返回欧氏路径长度，后者使用一基对象索引。本机 20.11 的稳定排序、范围选择、托管快照、统计和生命周期已专项验证。
- [MVTec HALCON 20.11 `vector_angle_to_rigid`](https://www.mvtec.com/doc/halcon/2011/en/vector_angle_to_rigid.html)：S62 从一组参考 Row/Column/Angle 与当前 Row/Column/Angle 生成只包含旋转和平移的 HomMat2D，并以该矩阵派生跟随 ROI/屏蔽区。
- [MVTec HALCON 20.11 `create_scaled_shape_model`](https://www.mvtec.com/doc/halcon/2011/en/create_scaled_shape_model.html) 与 [`find_scaled_shape_model`](https://www.mvtec.com/doc/halcon/2011/en/find_scaled_shape_model.html)：S63 以等比例尺度范围创建 Shape Model，并按分数、数量、角度、尺度、重叠和贪婪度搜索多目标位姿；本机 HALCON 20.11 的模型保存/加载、候选稳定排序、XLD 轮廓、无匹配和资源释放已专项验证。

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
7. 默认首屏不展示全部能力；功能入口完整应通过任务上下文和渐进展开实现，而不是持续增加永久按钮、卡片和面板。

## 最高验收标准

每轮都必须回答：VM 工程师能否快速上手；能否用熟悉方式添加、配置、运行工具；是否在统一工作区完成采集、算法、计算、通讯和输出；UI 是否肉眼可见变化；是否新增真实工业价值；BSB 开发启用时是否新增真实 BSB 价值；是否减少弹窗和割裂窗口；是否缩短搭建调试路径；是否更适合现场长期使用；是否比上一轮更接近完整 VM 产品。

## 当前交互纠偏与 S73 基线（2026-08-26）

- 开发流程默认采用从上到下的确定性调试顺序，而不是横向依赖层铺排；自动节点已收紧为约 `300 x 74 px`，连接允许从模块两侧柔性绕行。分支、汇聚和类型仍来自真实 `InputBindings`，视觉放松不等于执行语义放松。
- 左侧模块箱只显示一个 `分类` 入口；默认内容现为 68 个已实现工具/9 类，流程片段由同一入口按需切换。收藏、最近等内部能力不得再次成为一排永久筛选按钮。
- 单步骤日志、输入输出摘要和 TCP 收发记录保留，但通过节点 `记录` 打开中央专用子页面；默认首屏不同时展开流程节点详情、步骤记录、通讯、图层和底部日志。
- S71 新增类型化双直线关系/交点；S72 又新增 `Point2D[] -> HALCON fit_line_contour_xld -> Line2D/Point2D/Number/XLD`，并真实驱动双直线关系、点线距离和 NumericJudge。当前几何仍限定同一 `ImagePixel`，不能冒充标定、世界坐标、跨相机、机器人/振镜或 BSB 坐标能力。

S72 对默认首屏的进一步约束是：未选合法连接的透明度降到低视觉权重，节点选中只强化真实相邻路径而不自动展开文字；只有明确选中连接或连接异常才显示合并端点标签。自动纵向连接统一走模块右侧外部通道，避免左侧裁切和穿过模块；上下模块导航、当前执行定位及用户主动滚动后暂停自动跟随均从键盘或上下文进入，不新增永久顶部按钮。

S73 又把自动画布宽度改为随真实视口和缩放响应，常见双尺度自动布局的底部横向滚动为 `0`；手动布局坐标及其横向浏览不受影响。流程区内新增 `24 px` 细导航轨，只编码步骤位置、选中、执行、断点、NG、异常和连接阻断，点击定位真实节点，不形成名称或按钮墙。类型化几何链扩展为 `Point2D[] -> HALCON fit_circle_contour_xld -> Circle2D/Point2D/Number/XLD`，同样只承诺同一 ImagePixel 上下文。
