# 海康 VM 官方参考产品规范

更新日期：2026-08-14
用途：定义本项目要复刻的 VM / VisionMaster 产品结构、交互习惯、功能边界和验收口径。
状态：长期产品基线；实现状态仍以代码和可复现验证为准。

## 1. 结论

本项目中的 VM 始终只指海康机器人 VM / VisionMaster。最终产品目标不是“借鉴 VM 配色”，而是在不复制海康专有代码、商标和美术资产的前提下，尽量完整复刻其页面分区、工程组织、流程搭建、模块配置、图像/ROI、输入输出订阅、调试运行、资源管理、运行界面和现场运维习惯；全部视觉算法和视觉算子由 HALCON 实现。

目标产品定义：

> VM / VisionMaster 类型的完整工业视觉开发平台体验 + HALCON 视觉算法内核。

## 2. 官方依据与证据等级

### 2.1 官方来源

1. [海康机器人 VM 算法平台产品页](https://www.hikrobotics.com/cn/machinevision/visionmaster/)：确认图形化拖拽、SDK 二次开发、算子设计、定位/测量/识别/检测、深度学习、外部资源管理和 3D 能力。
2. [2025 海康机器人机器视觉新品发布会 - VM5.0](https://www.hikrobotics.com/cn/publish/)：确认 VM5.0 的网格化布局、多模块汇聚、自动串/并连排列、多模块组合折叠，以及通用配置、菜单导航和核心配置重塑。
3. [海康机器人 VM3D 算法开发平台官方用户手册](https://pinfo.hikrobotics.com/hkws/unzip/20240919112534_17943_doc/index.html)：确认主界面、方案/流程/模块/全局逻辑、硬件与通信管理、运行界面设计、调试运行、图像/结果显示、权限和运行策略等完整信息架构。VM3D 不是本项目的 2D 功能边界，但其通用框架和工作区是海康官方 VM 家族界面证据。
4. [海康官方相机管理章节](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/GUID-1526E9AD-10BE-4190-B48D-CE9BD83597A2.html)：确认全局相机、设备列表、相机参数、触发、缓存、断线重连、IO 和光源等资源管理习惯。
5. [海康官方图像源模块章节](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/GUID-8D7949F0-EB91-4794-BD3C-D4182726D931.html)：确认本地图像、相机、SDK 图像源、缓存、SN、自动切换、触发过滤等图像输入行为。
6. [海康机器人 VM 算法开发平台官方用户手册 PDF](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)：确认打开方案、最近打开方案、保存方案、新方案首次保存选择路径，以及方案管理/自动切换和角色权限边界。
7. [海康 VM3D 官方手册目录](https://pinfo.hikrobotics.com/hkws/unzip/20240919112534_17943_doc/index.html)：其通用模块配置与 2D 图像处理目录确认图像滤波属于 VM 家族的图像处理能力域；本项目具体滤波模式和 HALCON 参数仍属于产品化实现。
8. [海康机器人 VM 算法开发平台官方用户手册 PDF](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)与[VM3D 官方手册目录](https://pinfo.hikrobotics.com/hkws/unzip/20240919112534_17943_doc/index.html)：再次确认图像控件可订阅流程模块图像、提供自适应显示并按层显示图像叠加，2D 图像处理目录包含形态学处理。放大方式、叠加开关粒度、具体颜色和 HALCON 参数属于本项目产品化实现。
9. 同一官方 VM 手册和[图像源模块章节](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/GUID-8D7949F0-EB91-4794-BD3C-D4182726D931.html)确认模块输入来源可订阅前序模块输出且具有明确数据类型约束；S15 的端口类型、来源、消费者和双输入汇聚可视化据此作为产品化实现，不代表海康内部连接模型。
10. MVTec 官方 HALCON 算子文档：[union2](https://www.mvtec.com/doc/halcon/2011/en/union2.html)、[intersection](https://www.mvtec.com/doc/halcon/2011/en/intersection.html)、[difference](https://www.mvtec.com/doc/halcon/2011/en/difference.html)和[symm_difference](https://www.mvtec.com/doc/halcon/2111/en/symm_difference.html)作为 S15 集合语义依据；项目以已安装 HALCON 20.11 .NET 导出签名和专项结果共同验证实际运行。
11. 同一 VM5.0 官方发布资料与 VM 手册确认网格布局、多模块汇聚、自动串/并连排列和前后模块连接习惯；S16 将真实 `InputBindings` 映射为二维依赖层和连接曲线，是本项目产品化推导，不代表海康内部连接模型或并行调度已经复刻。MVTec 官方 [scale_image](https://www.mvtec.com/doc/halcon/11/en/scale_image.html) 与 [emphasize](https://www.mvtec.com/doc/halcon/2011/en/emphasize.html) 算子文档作为 S16 线性灰度缩放和局部对比度增强语义依据，实际运行以已安装 HALCON 20.11 .NET 签名和专项像素结果验证。
12. S17 的连接检查器是依据 VM 前后模块连接/输入订阅习惯做出的产品化推导：选择、定位、清除和修复必须作用于本项目真实 `InputBindings`，不代表海康内部实现已知。MVTec 官方 HALCON 20.11.4 [`dyn_threshold`](https://www.mvtec.com/doc/halcon/2011/en/dyn_threshold.html) 文档确认两路单通道图像、Light/Dark 比较和 `-255 < Offset < 255` 算子语义；本项目将现场配置收窄为 `1 <= Offset < 255`，以保留明/暗方向含义并规避接近零偏移的噪声风险，该限制属于本项目工业封装。
13. S18 继续依据 VM 官方手册“参数可从前序模块结果订阅”的操作习惯，把输入端口订阅替换/清除和输出消费者导航作为本项目产品化实现；所有动作写回真实 `InputBindings`，不代表海康内部端口编辑器已被复制。MVTec HALCON 20.11.4 官方 [`sub_image`](https://www.mvtec.com/doc/halcon/2011/en/sub_image.html) 文档确认 `(g1-g2)*Mult+Add`、裁剪和 .NET 签名；官方 [`abs_diff_image`](https://www.mvtec.com/doc/halcon/2011/en/abs_diff_image.html) 说明绝对差 `abs(g1-g2)*Mult` 与 `Mult > 0`。本机 20.11 `halcondotnet.dll` 反射进一步确认 `HOperatorSet.AbsDiffImage(..., HTuple)` 与 `SubImage(..., HTuple, HTuple)` 精确导出签名。
14. VM 官方手册明确描述“右键单模块复制”以及“框选多个模块后复制，在目标区域右键粘贴”。S19 先交付多入口模块剪贴板、顺序列表多选、新 ToolId/安全名称、配置复制和合法连接重映射；连接画布框选留待 S20，因此不能宣称官方多选交互已完整复刻。MVTec HALCON 20.11.4 官方 [`gen_contour_region_xld`](https://www.mvtec.com/doc/halcon/2011/en/gen_contour_region_xld.html) 确认 `center/border/border_holes` 语义，[`length_xld`](https://www.mvtec.com/doc/halcon/2011/en/length_xld.html) 确认逐 XLD 轮廓返回长度元组；本机 20.11 `halcondotnet.dll` 反射确认对应 `HOperatorSet` 精确签名。
15. S20 已按上述官方多模块习惯补连接画布可见橡皮框、Ctrl 增选/取消、选择计数和真实批量动作；这是本项目对交互习惯的实现，不代表海康内部选择模型。MVTec HALCON 20.11.4 官方 [`select_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/select_contours_xld.html) 确认 `contour_length/maximum_extent` 的范围均由 `Min1/Max1` 控制、`Min2/Max2` 对这两个特征无影响；本机 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.SelectContoursXld(HObject, out HObject, HTuple, HTuple, HTuple, HTuple, HTuple)` 精确签名。S20 不据此宣称用户拖拽连线、撤销、Group 或并行已完成。
16. 海康 VM 官方产品页明确以“完全图形化”与“拖拽式操作”描述快速搭建视觉方案；S21 据此把真实 OUT→IN 端口拖线、已连接 IN 重连和可连接/拒绝反馈作为本项目产品化实现，所有动作写回真实 `InputBindings`，不代表海康内部连接模型已知。MVTec HALCON 20.11.4 官方 [`segment_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/segment_contours_xld.html) 确认 `lines/lines_circles/lines_ellipses`、平滑及两项最大线距语义；本机 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.SegmentContoursXld(HObject, out HObject, HTuple, HTuple, HTuple, HTuple)` 精确签名。S21 不据此宣称撤销、手动节点布局、Group、并行或多流程已完成。
17. VM5.0 官方发布资料明确提到网格布局、多模块汇聚与自动排列；S22 据此实现可见网格、模块手动定位、吸附、对齐/等距和恢复自动布局，坐标只作为本项目可选配方呈现字段，不代表海康内部布局存储或调度模型已知。MVTec HALCON 20.11 官方 [`fit_line_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_line_contour_xld.html) 确认逐轮廓直线拟合、regression/Huber/Tukey/drop/Gauss、最大采样点、端点裁剪、迭代与裁剪因子语义；本机 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.FitLineContourXld(HObject, HTuple, HTuple, HTuple, HTuple, HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple)` 精确签名。S22 不据此宣称撤销/重做、Group、并行或多流程已完成。
18. 海康 VM 官方资料中的图形化方案搭建、多模块编辑和网格工作区用于确认撤销/重做应恢复真实流程状态而非播放动画；S23 的 50 项纯 DTO 历史、快捷键和恢复语义是本项目产品化实现，不代表海康内部历史模型已知。MVTec HALCON 20.11 官方 [`fit_circle_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_circle_contour_xld.html) 确认一条轮廓拟合一个圆、`algebraic/ahuber/atukey/geometric/geohuber/geotukey`、`MaxNumPoints`、`MaxClosureDist`、端点裁剪、迭代、裁剪因子及 `Row/Column/Radius/StartPhi/EndPhi/PointOrder` 输出语义；本机 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.FitCircleContourXld(HObject, HTuple, HTuple, HTuple, HTuple, HTuple, HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple)` 精确签名。S23 不据此宣称完整 Group、并行、多流程或海康内部圆算法已知。
19. VM5.0 官方发布资料明确提到多模块组合折叠；S24 据此实现可命名、可持久化、安全折叠且显示边界连接的组织型 Group 首版，这是本项目产品化实现，不代表海康内部 Group 存储或调度模型已知。MVTec HALCON 20.11 官方 [`fit_ellipse_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_ellipse_contour_xld.html) 确认 `fitzgibbon/fhuber/ftukey/focpoints/fphuber/fptukey/geometric/geohuber/geotukey/voss`、`MaxNumPoints/MaxClosureDist/ClippingEndPoints/VossTabSize/Iterations/ClippingFactor` 及 `Row/Column/Phi/Radius1/Radius2/StartPhi/EndPhi/PointOrder` 语义，其中 `Radius1` 为长半轴、`Radius2` 为短半轴；本机 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.FitEllipseContourXld(HObject, HTuple, HTuple, HTuple, HTuple, HTuple, HTuple, HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple)` 精确签名。S24 不据此宣称执行型 Group、并行、多流程或海康内部椭圆算法已知。
20. S25 的流程断点、单步、继续、红色断点和下一执行指针，是依据官方 VM 调试/运行工作方式与本项目真实确定性执行器做出的产品化推导，不代表海康内部断点存储或调度器实现已知。MVTec HALCON 20.11 官方 [`fit_rectangle2_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_rectangle2_contour_xld.html) 确认 `regression/huber/tukey`、`MaxNumPoints/MaxClosureDist/ClippingEndPoints/Iterations/ClippingFactor`、至少 8 个轮廓点及四条矩形边内部均须被轮廓覆盖的前提，并输出 `Row/Column/Phi/Length1/Length2/PointOrder`；本机 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.FitRectangle2ContourXld(HObject, HTuple, HTuple, HTuple, HTuple, HTuple, HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple, out HTuple)` 精确签名。S25 的四角点、近正方形方向提示和实际最大残差属于本项目基于官方拟合结果的产品化派生，不冒充海康内部矩形算法。
21. S26 的常驻调试监视、真实断点清单、本周期 I/O 快照和运行到所选，是依据官方 VM 调试/运行习惯及本项目确定性执行器做出的产品化推导；未找到可证明海康内部调试监视器具体布局或状态模型的官方证据，因此不得声称逐像素复刻。MVTec HALCON 20.11 官方 [`angle_ll`](https://www.mvtec.com/doc/halcon/2011/en/angle_ll.html)、[`intersection_lines`](https://www.mvtec.com/doc/halcon/2011/en/intersection_lines.html)、[`distance_pl`](https://www.mvtec.com/doc/halcon/2011/en/distance_pl.html)和[`gen_cross_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/gen_cross_contour_xld.html)分别确认有向线夹角、无限直线交点/重合状态、点到无限直线正交距离及十字 XLD 生成语义；本机 20.11 `halcondotnet.dll` 反射确认对应 `HOperatorSet` 精确签名。S26 对两路最长拟合线的锐角、平行/近重合分类、残差和托管叠加属于本项目工业封装，不冒充海康内部双线算法。
22. 海康 VM 官方手册确认图像控件可按层显示图像叠加；S27 把本项目真实 Region/XLD 输出组织为常驻结果图层清单，并提供显隐、隔离、排序、生产者定位和配置入口，这是依据官方工作习惯及托管快照模型做出的产品化实现，不代表海康内部图层数据结构或具体布局已知。MVTec HALCON 20.11 官方 [`gen_measure_rectangle2`](https://www.mvtec.com/doc/halcon/2011/en/gen_measure_rectangle2.html)、[`measure_pairs`](https://www.mvtec.com/doc/halcon/2011/en/measure_pairs.html)和[`close_measure`](https://www.mvtec.com/doc/halcon/2011/en/close_measure.html)分别确认旋转矩形测量域、成对边缘坐标/幅值/对内与对间距离及 measure handle 关闭语义；本机 20.11 `halcondotnet.dll` 反射确认对应 `HOperatorSet` 精确签名。S27 的 PrimaryDistance、可读字符串集合输出、叠加配色和状态模型属于本项目工业封装，不冒充海康内部卡尺算法或数组契约。
23. 海康 VM 官方手册确认基础参数可订阅前序图像，并可在图像预览区绘制或继承 ROI；S28 据此把单卡尺模块/结果图层选择、真实模块输入图像、旋转矩形控制点和右侧参数草稿组织为同一上下文，应用后才持久化，属于本项目产品化实现，不代表海康内部测量控件或数据模型已知。HALCON 官方 `gen_measure_rectangle2` 文档确认 `Row/Column/Phi/Length1/Length2`、bilinear 图像边界及 Sigma 与测量长度约束，`close_measure` 确认 handle 释放责任；S28 的线性阵列逐项调用与 Mean/Min/Max/StdDev、索引、错误字符串属于本项目工业封装，不冒充海康内部数组契约。MVTec HALCON 20.11 官方 [`measure_pos`](https://www.mvtec.com/doc/halcon/2011/en/measure_pos.html) 确认沿旋转矩形主轴按 Transition/Select 返回边缘位置、幅值和间距；S29 据此逐径向卡尺找边并以 `fit_circle_contour_xld` 鲁棒拟合圆，圆周布置、失败模型和托管 XLD 属于本项目工业封装，不冒充海康内部圆查找算法。
24. 海康 VM 官方产品页和手册将圆查找、卡尺类测量、前序图像订阅与图像预览交互作为图形化方案开发习惯；S30 把圆周找圆的圆心、半径、起止角和 Length1/Length2 六控制点组织到真实模块输入图像，并显示搜索弧、全部径向卡尺和失败项，这是本项目产品化实现，不代表海康内部控件布局或状态模型已知。MVTec HALCON 官方 [`measure_pairs`](https://www.mvtec.com/doc/halcon/2011/en/measure_pairs.html)确认正/负极性、First/Last/All 选择、成对边缘坐标/幅值及对内/对间距离；[`gen_measure_rectangle2`](https://www.mvtec.com/doc/halcon/2011/en/gen_measure_rectangle2.html)、[`fit_circle_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/fit_circle_contour_xld.html)和[`close_measure`](https://www.mvtec.com/doc/halcon/2011/en/close_measure.html)分别确认测量域、鲁棒圆拟合和 handle 释放语义。S30 以边缘对中点接近搜索半径选择目标环、分别拟合内外圆并派生厚度统计和偏心量，属于 HALCON 工业封装，不冒充海康内部圆环算法或数组契约。
25. 海康 VM 官方 2D 用户手册确认圆 ROI 由圆心和圆周点创建，选中后可编辑位置、尺寸、内径和外径，并以中心 X/Y、内/外直径显示几何值；S31 圆环语义据此实现。椭圆具体控制点布局仍缺足够官方证据，属于遵循 VM ROI 习惯的产品化推导。MVTec 官方 [`polar_trans_image_ext`](https://www.mvtec.com/doc/halcon/2511/en/polar_trans_image_ext.html)确认按 `Row/Column/AngleStart/AngleEnd/RadiusStart/RadiusEnd/Width/Height/Interpolation` 将圆环弧转换为矩形图像，左上输出像素对应起始角和起始半径；S31 已用完整 360°去重、顺逆方向、定位标记、灰度/RGB、输入 domain/输出矩形和插值配置专项核对这些语义。
26. S32 的 ROI 数值几何属性工作台继续以同一官方手册的中心、尺寸、角度、内外直径和 ROI 图像交互为准；多边形顶点表属于产品化推导。MVTec HALCON 20.11 官方 [`reduce_domain`](https://www.mvtec.com/doc/halcon/2011/en/reduce_domain.html)确认输出 domain 为输入 domain 与 Region 的交集且图像矩阵尺寸不变；[`crop_domain`](https://www.mvtec.com/doc/halcon/2011/en/crop_domain.html)确认按有效域最小外接矩形裁剪。S32 已以 100×80 图像和 R10..40/C20..60 ROI 验证前者保持尺寸与零偏移、后者输出 41×31 和 `OffsetRow=10/OffsetColumn=20`，不能把局部坐标冒充原图坐标。
27. 海康 VM 2D 官方用户手册明确说明可切换单/双画面；双画面左侧预览窗口只可选择当前模块输入源订阅的图像数据，右侧保留单画面的输出/预览能力。S33 已遵守该上下文限制，而不是任意双图拼接；左右视口按各自图像坐标显示，并可用归一化中心与可视比例联动。MVTec HALCON 20.11 官方 [`zoom_image_size`](https://www.mvtec.com/doc/halcon/2011/en/zoom_image_size.html)确认按目标 Width/Height 和 Interpolation 缩放图像，并指出输入 domain 被忽略、按整幅矩形处理；本机反射确认 `HImage.ZoomImageSize(int, int, string)`，专项确认 `bicubic/bilinear/constant/nearest_neighbor/weighted`、定位像素和比例语义。实现显式取 `FullDomain`，让输出 domain 与官方“整幅矩阵”产品契约一致。
28. 同一官方手册确认“十字辅助线 1/2”可分别启用，字段包含交点图像 X/Y、线宽、颜色、居中和锁定；锁定用于阻止图像区直接拖动。S34 已按这些语义交付两组独立辅助线，并进一步按本项目单/双画面模型把设置分为左/右视口；图像区内可折叠面板、颜色选择项、拖动命中范围和配方字段属于产品化实现，不代表海康内部布局或存储模型。MVTec HALCON 20.11 官方 [`mirror_image`](https://www.mvtec.com/doc/halcon/2011/en/mirror_image.html)确认 `row/column/diagonal` 分别关于水平轴、垂直轴和主对角线反射；S34 已以 4×3 定位像素确认上下、左右及对角行列映射，对角输出 3×4，并验证灰度/RGB、缩小 domain、异常路径和托管生命周期。
29. 官方 VM 手册的 ROI 目录和交互说明确认点、线属于图像预览中的基础 ROI 类型；S35 已补真实创建、选择、控制点、移动、锁定、复制、删除、绑定、配方和常驻数值几何，不是隐藏数据类型。MVTec HALCON 20.11 官方 [`rotate_image`](https://www.mvtec.com/doc/halcon/2011/en/rotate_image.html)确认按图像中心逆时针旋转，角度单位为度；通常输出尺寸不变，90°/270°交换宽高；输入 domain 被忽略，输出 domain 为变换矩形与输出矩形的交集。S35 已以本机 20.11 `HImage.RotateImage(double,string)` 和五种插值完成定位像素、灰度/RGB、domain、异常、生命周期、配方和下游验证。
30. 官方 VM 手册“基本参数/ROI 区域”明确说明图形 ROI 可设置屏蔽区，模块运行时不检测被屏蔽部分；还支持 ROI 绘制/继承和位置修正。S36 A 线已交付可见的工具级屏蔽区工作台，并以“有效 ROI = 检测 ROI - 启用屏蔽区”进入真实执行，不把单纯的彩色覆盖冒充屏蔽完成；位置修正和 ROI 继承仍未实现。MVTec HALCON 20.11 官方 [`affine_trans_image`](https://www.mvtec.com/doc/halcon/2011/en/affine_trans_image.html)确认可执行缩放、旋转、平移和斜切，使用 `HomMat2D + Interpolation + AdaptImageSize`；输入 domain 被忽略，输出 domain 为变换矩形与输出矩形的交集，`AdaptImageSize=true` 仍会裁掉变换后的负行/列。本机 20.11 反射确认 `HImage.AffineTransImage(HHomMat2D,string,string)`，S36 已落实 HALCON 行映射矩阵 x、列映射 y、0.5 像素中心校正和输出安全尺寸保护。
31. 海康 VM 官方手册确认图像预览按多个显示层组织叠加，模块图像和显示属性应在同一调试上下文中使用；S37 的统一图层属性、跨 ROI/结果/生产者选择和左右视口范围属于基于该习惯的产品化实现，不代表海康内部图层模型已知。MVTec HALCON 20.11 官方 [`vector_to_proj_hom_mat2d`](https://www.mvtec.com/doc/halcon/2011/en/vector_to_proj_hom_mat2d.html)、[`projective_trans_image`](https://www.mvtec.com/doc/halcon/2011/en/projective_trans_image.html)和[`projective_trans_image_size`](https://www.mvtec.com/doc/halcon/2011/en/projective_trans_image_size.html)确认四点投影矩阵、nearest/bilinear、尺寸适应/显式尺寸和 TransformDomain 语义；本机反射与专项进一步确认 .NET 精确签名。S37 采用 `normalized_dlt`，并按文档对源/目标坐标同时加 0.5 处理图像像素中心差异。
32. 海康 VM 官方手册确认模块参数与图像预览处于同一调试上下文，ROI 可在预览图中选择和编辑；S38 把透视源/目标四点升级为带编号控制点的输入/目标视口编辑，是沿用该操作习惯的产品化推导，不宣称海康存在相同透视控件。MVTec HALCON 官方 [`edges_sub_pix`](https://www.mvtec.com/doc/halcon/2011/en/edges_sub_pix.html)确认从单通道图像提取亚像素 XLD，支持 Deriche、Lanser、Shen、Canny、Sobel 系列 Filter，并以 `Alpha/Low/High` 控制平滑与滞后阈值；本机 HALCON 20.11 程序集反射和专项运行已确认九种 Filter、精确签名、阈值约束、reduced domain、RGB 产品化策略和托管 XLD 生命周期。
33. 海康 VM 官方手册确认图像控件以多个显示层组织轮廓/结果，并可在同一模块调试上下文查看结果；S39 已交付单条 XLD 鼠标拾取、亚像素点检查和生产者联动，属于对该结果检查习惯的产品化推导，不宣称海康内部存在相同检查器或数据结构。MVTec HALCON 20.11 官方 [`union_adjacent_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/union_adjacent_contours_xld.html)确认按端点最大绝对距离、相对轮廓长度距离和 `attr_keep/attr_forget` 连接相邻轮廓，并可能反转输入轮廓点序；本机 20.11 程序集反射已确认 `HOperatorSet.UnionAdjacentContoursXld(HObject,out HObject,HTuple,HTuple,HTuple)`，专项运行已确认短间隙/超阈值、反向点序、两种属性策略、空对象、输入所有权和托管输出生命周期。
34. 海康 VM 官方本地图像资料确认可从图像显示区添加图片/文件夹、设置自动切换并在最后一张停止；S40 已将同一真实文件列表产品化为可折叠缩略队列，支持点击、前后、播放/暂停、末图停止、进度、失败状态、目录重载、清空和有界缓存。队列布局和状态卡属于本项目基于 VM 多图调试习惯的产品化实现，不宣称逐像素复制官方界面。MVTec HALCON 20.11 官方 [`smooth_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/smooth_contours_xld.html)确认使用局部回归线投影平滑 XLD，`NumRegrPoints` 默认 5、建议 3/5/7/9，且必须为不小于 3 的奇数；本机程序集反射和专项确认 `HOperatorSet.SmoothContoursXld(HObject,out HObject,HTuple)`、多/短/空轮廓、长度变化、输入所有权、托管生命周期，并确认输出不保留输入点属性。
35. 海康 VM 本地图像区资料还确认“运行所有/运行选中”等批图调试入口；S41 已在 S40 同一队列交付当前/选中/全部运行范围、逐图状态和批次汇总，属于官方确认的操作方向，具体状态卡与汇总布局仍为本项目产品化推导。MVTec 官方 [`affine_trans_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/affine_trans_contour_xld.html)确认对 XLD 应用任意二维仿射矩阵，并特别指出其采用 `affine_trans_pixel` 坐标体系；本机 HALCON 20.11 反射和专项已确认 `HOperatorSet.AffineTransContourXld(HObject,out HObject,HTuple)`、标准像素中心补偿、Row/Column、中心、四种组合顺序、属性和托管生命周期。
36. 海康 VM 官方手册确认运行界面可由控件区编排，并用于方案的运行监视与控制；S42 A 线已据此交付与当前方案、真实执行器、图像、结果、统计和报警共用状态的生产运行页首版，具体布局属于产品化推导，不把空设计器或第二套状态冒充官方运行界面。MVTec HALCON 20.11 官方 [`projective_trans_contour_xld`](https://www.mvtec.com/doc/halcon/2011/en/projective_trans_contour_xld.html)确认使用齐次投影矩阵变换 XLD；S42 B 线已按本机精确签名验证 Row/Column、像素中心策略、退化矩阵、无穷远点、属性和托管生命周期。
37. S43 的受控运行界面配置继续依据上述官方“控件区编排、数据绑定、运行监视”能力，但只开放既有真实部件的标题、显隐和顺序，不把首版受控布局冒充完整自由设计器。MVTec HALCON 20.11 官方 [`crop_contours_xld`](https://www.mvtec.com/doc/halcon/2011/en/crop_contours_xld.html)确认以矩形边界裁剪 XLD，并可通过 `CloseContours` 控制裁剪轮廓闭合；本机精确签名为 `CropContoursXld(HObject,HObject ByRef,HTuple,HTuple,HTuple,HTuple,HTuple)`，S43 必须验证边界交点、闭合语义和托管生命周期。

### 2.2 证据等级

- `官方确认`：官网或手册明确描述，可作为产品需求。
- `界面图确认`：官方手册界面截图中可观察到的区域、入口和布局，可作为交互/布局要求。
- `产品化推导`：为在本项目技术栈中复现 VM 工作方式所做的设计推导，必须标注，不得冒充海康原功能名称或实现细节。
- `待实机确认`：需要安装对应 VM 版本、真实相机/控制器或现场协议才能确认的细节。

## 3. 官方确认的 VM 产品能力

### 3.1 开发模式

- 完全图形化界面，模块直观，通过拖拽快速搭建视觉方案。
- 提供控制和数据获取接口用于 SDK 二次开发。
- 支持把算子包装为视觉工具并融入自定义检测流程。
- 本项目对应实现：产品层使用 VM 类型工具；工具内部视觉计算统一调用 HALCON/HDevEngine，不直接散乱暴露 HALCON 原子调用。

### 3.2 视觉能力域

- 定位：模板、圆、直线、Blob、边缘、顶点等，并应对平移、旋转、缩放和光照差异。
- 测量：尺寸、距离、角度、面积、灰度、轮廓等。
- 识别：一维码、二维码、OCR/字符识别和分类。
- 检测：表面、形状、轮廓和标准件差异。
- 深度学习：分割、分类、目标检测、字符定位与识别、检索、异常检测等。
- 3D：深度图、轮廓图、点云、测量、定位、预处理和拼接。

### 3.3 外部资源与工业集成

- 相机、视觉控制器、IO、光源统一管理。
- 通信设备、PLC 地址、收发事件、心跳、响应和通信示例。
- 全局相机、全局变量、全局触发、全局脚本和数据队列。
- 权限、方案设置、运行策略、运行异常采集和版本工具。

### 3.4 方案生命周期

- `官方确认`：主界面/运行界面提供打开方案、最近打开方案和保存方案；已保存方案直接覆盖保存，新建方案首次保存时选择路径。
- `官方确认`：方案管理可与通信事件结合实现受控自动换型；启用权限时，方案操作受管理员/技术员/操作员角色限制。
- `产品化推导`：本项目当前优先交付当前路径、保存状态、最近列表、另存、原子写入和异常恢复；自动换型与权限在通信/权限 Sprint 实施前保持未实现。

## 4. 目标信息架构与页面清单

最终产品至少具有以下一级页面或工作模式。名称可本地化，但工作职责不得缺失。

| 页面/模式 | 核心用户任务 | 必需区域 |
| --- | --- | --- |
| 欢迎/启动页 | 新建方案、选择方案类型、打开最近方案、示例与帮助 | 最近方案、方案模板、帮助入口、版本/许可状态 |
| 方案开发主界面 | 搭建和调试流程 | 菜单栏、快捷工具条、当前方案、角色、模块箱、流程编辑、图像显示、结果显示、日志/状态 |
| 模块配置页/窗口 | 配置一个工具的全部行为 | 基本参数、运行参数、输入订阅、ROI/模板、结果显示、模型页、试运行、确定/取消、错误帮助 |
| 流程逻辑配置 | 多流程、串并行、触发和数据交换 | 流程列表/标签、全局相机、数据队列、分支汇聚、运行参数 |
| 硬件资源管理 | 配置相机、控制器、IO、光源 | 设备列表、连接状态、参数、实时预览、触发、诊断 |
| 通信管理 | 配置设备和收发逻辑 | 设备、协议、地址、接收事件、发送事件、心跳、响应、收发监视、手动测试 |
| 全局逻辑 | 管理跨流程数据与控制 | 全局变量、全局触发、全局脚本、数据队列、订阅关系 |
| 运行界面设计 | 为现场操作员设计 HMI | 控件箱、预览编辑区、属性区、图层、保存/导出 |
| 生产运行界面 | 启停、监控、换型和报警 | 图像、结果、OK/NG、节拍、设备状态、报警、配方、受控操作 |
| 历史与诊断 | 追溯和排障 | 运行历史、图像回放、日志、通信报文、设备/配方/版本、自检和导出 |
| 系统设置 | 权限、软件、方案和运行策略 | 角色权限、通用设置、语言、License、插件、运行策略 |

## 5. 方案开发主界面布局契约

官方手册界面图确认的主框架如下：

```text
┌ 菜单栏 / 当前方案 / 角色 / 窗口与系统状态 ───────────────────────────┐
├ 快捷工具条：保存、资源、全局变量、运行、停止、运行界面等 ────────────┤
├──────────┬────────────────────────────┬───────────────────────────┤
│ 模块箱    │ 流程标签 + 流程编辑区       │ 图像 / 模块结果             │
│ 分类/搜索 │ 网格、顺序、分支、汇聚、Group│ 模块图像/全局图像/叠加/切换  │
│ 拖拽添加  │ 状态、耗时、缩放和小地图     ├───────────────────────────┤
│           │                            │ 结果表/输出参数/帮助/错误     │
├──────────┴────────────────────────────┴───────────────────────────┤
│ 日志栏 / 总耗时 / 缩放 / 连接状态 / 版本 / 报警                       │
└───────────────────────────────────────────────────────────────────┘
```

重要纠正：

- 最终开发主界面的中心主区域应是流程编辑区，不是中央大图像区。
- 图像和结果应作为流程调试的并列核心区域，默认位于右侧上/下分区，并支持放大、隐藏、停靠和恢复。
- 当前项目的右侧常驻 Inspector 是过渡实现；最终应形成 VM 类型的模块配置页/窗口。不得继续把“右侧 Inspector”误写成唯一最终形态。
- “减少弹窗”不是禁止 VM 类型模块配置窗口，而是禁止散乱、互不一致、缺少输入输出和试运行闭环的旧式小弹窗。统一模块配置工作台属于目标形态。

## 6. VM5 流程编辑区契约

### 6.1 必须可见

- 流程标签和当前流程名。
- 模块实例卡：实例名、类型/图标、启用、配置、运行、OK/NG、错误、耗时。
- 有方向的串联、并联、分支与汇聚关系。
- 网格背景、缩放、平移、适应、最小地图或定位工具。
- 当前运行模块、已完成模块、NG 模块和未执行模块的状态差异。
- 多模块选择、复制粘贴、删除、重命名、组合和折叠。

### 6.2 必须可操作

- 搜索、双击或拖拽添加模块。
- 自动串/并连排列和可预测的插入位置。
- 单模块运行、运行到此、从此运行、单次流程、连续流程、暂停、停止。
- 运行间隔、流程超时、遇 NG 停止。
- 多流程建立、复制、导入/导出、重命名和异步执行。
- Group 循环、条件分支、数据队列和全局变量参与执行逻辑。

### 6.3 不是通用 Graph 演示

连线必须对应真实数据或执行语义；流程顺序和分支结果必须可验证并可持久化。不得用只有 Node/Edge/动画、没有模块配置和执行闭环的图编辑器冒充 VM 流程。

## 7. 模块箱与工具目录契约

### 7.1 通用交互

- 分类、搜索、筛选、展开/折叠、收藏、最近使用和帮助。
- 双击添加和拖拽添加。
- 工具图标、名称、类别、简述、输入输出类型和许可/可用状态。
- 自定义模块可注册、版本化并进入目录。

### 7.2 目标类别

1. 2D 采集：图像源、多图采集、输出图像、缓存图像、光源。
2. 2D 定位：模板、图形、位置修正、Blob、圆/椭圆/直线、卡尺、边缘、顶点、路径。
3. 2D 测量：点/线/圆组合、距离、角度、亮度、间距、像素统计、直方图。
4. 2D 识别：条码、二维码、OCR、字符、分类。
5. 2D 缺陷：表面、边缘、轮廓、标准比对和趋势分析。
6. 2D 标定：N 点/N 图像、标定板、映射、畸变、旋转、标定加载。
7. 2D 运算：对位、抓取、纠偏、点集、旋转、单位、变量、坐标转换。
8. 2D 图像处理：阈值、滤波、形态学、增强、组合、仿射、畸变、拼接、清晰度、融合。
9. 逻辑与数据：条件、逻辑、格式化、字符串、脚本、Group、点集、耗时、集合、触发、图形收集。
10. 通信：接收、发送、相机 IO、协议解析和协议组装。
11. 3D 与深度学习：作为后续平台能力预留完整目录和数据类型，不得用空按钮宣称完成。
12. BSB：长期保留，按用户当前指令暂停实施。

## 8. 统一模块配置契约

双击流程模块进入统一配置工作台。每个工具至少具备：

- 基本参数：图像输入、ROI、任务参数、模型/标定选择。
- 运行参数：启用、触发过滤、超时、错误策略、缓存和条件运行。
- 输入订阅：端口类型、来源模块/全局变量/数据队列、当前值和连接错误。
- 输出定义：输出参数、类型、别名、下游订阅和 SDK 名称。
- ROI/模板：绘制、继承、屏蔽区、位置修正、模型训练和预览。
- 结果显示：图像/图形/文本叠加、颜色、线宽、显示条件和输出选择。
- 模型页：仅模型类工具出现，管理训练、加载、版本和验证。
- 底部操作：试运行、连续试运行、恢复默认、应用、确定、取消和帮助。

参数必须有名称、说明、类型、单位、范围、默认值、校验、可见条件、持久化键和兼容迁移规则。新工具不得只增加代码类而没有配置页。

## 9. 图像、ROI 与结果显示契约

### 9.1 图像显示

- 模块输入/输出图像和全局流程图像切换。
- 单画面/双画面，多图像缩略图或序号切换。
- 缩放、平移、适应、1:1、十字辅助线、鼠标坐标、灰度/RGB/深度值。
- 原图、中间图、结果图、轮廓、深度图和点云数据类型入口。
- 图像名称、SN、尺寸、像素格式和当前模块信息。

官方手册确认图像控件可订阅流程模块图像、提供自适应显示，并以多个显示层组织图像叠加。S09-S49 已有全局/模块图像上下文、单/双画面、联动/独立视图、双十字、屏蔽区、缩略队列、统一 ROI/Region/XLD 图层、透视编辑、XLD 单条检查，以及本地批次、受管历史图像/图层、连续导航、双图对比、逐工具明细、双样本差异和 Number 趋势。仍缺真正多画面、像素格式、Shape/HDevelop 受管叠加和多数工具的中间/结果 Image；S49 实体截图还暴露图层浮层会遮挡图像，S50 必须改成不遮挡的单任务右侧面。

### 9.2 ROI

- 矩形、旋转矩形、圆、椭圆、圆环、多边形、线、点和自由/轮廓区域。
- 创建、控制点编辑、移动、缩放、旋转、复制、锁定、显隐、启停、重命名和删除。
- 多 ROI、屏蔽区、继承/引用、位置修正和工具级绑定。
- 逐 ROI 结果与集合结果，两者均能被下游订阅。
- ROI 坐标、图像尺寸和坐标系变更时有明确迁移或失效提示。

官方手册明确描述选中 ROI 后可调整位置、宽、高和角度，并提供复制/删除及屏蔽区相关操作；这是本项目继续完善 ROI 编辑器的官方依据。S11/S14 的绿色控制点、橙色旋转点、多边形顶点编号、锁定保护、复制偏移、非法几何保护和稳定 RoiId 是本项目的产品化实现，不代表海康内部实现细节。

### 9.3 结果

- 图形叠加、文本叠加、表格、输出参数、OK/NG、耗时和错误。
- 选中模块时显示该模块结果；流程模式显示全局结果。
- 结果显示配置随工具和配方保存。
- 隐藏结果只影响绘制，不得销毁工具输出或下游订阅；来源失效、删除或无图像时必须给出明确状态。

## 10. 资源、通信、全局逻辑和运行界面

### 10.1 资源管理

- 相机列表、类型、别名、连接、占用、自动重连、缓存、软触发超时和实时预览。
- 曝光、增益、Gamma、像素格式、帧率、触发模式/源/延时和线阵参数。
- IO、光源和控制器入口；断线、超时和丢包诊断。
- 本项目保留既有相机 SDK 稳定边界，通过产品层适配进入统一页面。

### 10.2 通信管理

- TCP Client/Server、UDP、串口、Modbus、PLC 和自定义协议逐步扩展。
- 设备管理、地址/字段、连接状态、自动重连、心跳、超时、接收/发送事件、响应和日志。
- 协议解析/组装成为流程工具，设备连接配置成为全局资源；两者职责分离。

### 10.3 全局逻辑

- 全局变量、数据队列、全局触发和全局脚本均有真实入口。
- 跨流程数据必须类型化、可追踪、有当前值和生产运行诊断。

### 10.4 运行界面设计

- 控件箱、工具条、画布、属性区和图层。
- 图像、文本、表格、按钮、指示灯、趋势、报警、配方和设备状态等控件。
- 控件属性可绑定模块输出、全局变量和设备状态。
- 支持预览、保存、导出和受权限控制的生产运行。

## 11. HALCON 替代与架构边界

- 所有 2D/3D 视觉计算、图像处理、定位、测量、标定和坐标转换由 HALCON/HDevEngine 实现。
- VM 类型工具只定义产品契约：参数、端口、ROI、结果、状态、帮助和持久化；不得伪造海康内部算法或代码。
- 图像、Region、XLD、点/线/圆/姿态、标定映射和点云需要明确的产品数据类型及 HALCON 生命周期管理。
- 相机 SDK、投产通信协议和旧工程格式是稳定边界；先适配、再迁移、可回退。
- 不提交海康手册、官方截图、商标、图标或其他受保护资产；只在文档中链接官方来源并使用自制图标/布局实现同类工作方式。

## 12. 当前代码真实基线（S55 已验证；S46-S55 Git 批次 10/10，本轮统一提交/推送）

### 12.1 已验证实现

- 活动 WPF 已采用左模块箱、中央网格顺序流程、右上 HALCON 图像/ROI、右下结果/配置/方案和底部日志/历史分区；模块箱支持分类折叠、搜索、收藏、最近、双击和拖拽确定性插入。
- Shape、Blob、灰度、边缘、HDevelop、数值判定、本地图像源、常用 Image/Region 处理、亚像素边缘、XLD 相邻连接/平滑/仿射/透视/裁剪/长度分级/长度排序/点数闭合/面积方向/外接框紧凑度/圆度凸性筛选/通用筛选/分段/拟合/关系和四类测量均已进入运行适配；右侧停靠配置覆盖除 Shape/HDevelop 外的上述主要工具。
- 类型化 I/O 与 Image/Region/XLD/Number 真实数据链；中央默认连接画布显示真实依赖层、方向曲线、端口标签、消费者、等待/错误和选中关系，连接可检查/修复，IN/OUT 可真实改线；画布还支持模块拖动、网格、对齐/等距、配方坐标和组织型 Group。Image 可通过 `edges_sub_pix` 生成托管亚像素 XLD，XLD 可连接、平滑、筛选、分段和拟合直线/圆/椭圆/旋转矩形并驱动数值判定；双 Image/双 Region/双 XLD 汇聚可用。托管 HALCON Image/Region/XLD 快照在上游重跑、删除、历史恢复、配方重载和关闭时释放。
- 编辑菜单、流程工具条、连接画布和顺序列表右键菜单已有复制、粘贴、创建副本及快捷键；连接画布支持可见橡皮框、Ctrl 增减和选择计数，并与顺序列表同步；批量复制/副本/启停/删除作用于真实流程，批量删除清理消费者并失效下游，不复制运行结果或 HALCON 快照。
- 运行菜单、顶部/流程工具条、画布/节点/列表右键和 `F9/F10/F5` 已提供断点开关、清除断点、单步和继续；红色断点、橙色下一执行指针、组合摘要、暂停原因和下一工具可见。断点在启用模块执行前命中，单步只执行一个启用工具，断点随配方和撤销/重做保存；执行指针只属于运行态。
- 中央流程区常驻调试监视显示真实断点清单、当前/下一模块、执行范围、暂停原因和所选模块本周期 I/O/结果/耗时/错误；可定位节点、打开输入/I/O、切换/清除断点和运行到所选。运行到所选复用真实确定性执行计划，目标只属于运行态且不持久化。
- 右上支持全局输入/当前模块输入/当前模块输出、来源工具/端口、文件/SN、尺寸、鼠标坐标、灰度/RGB 值、工作区放大/恢复、单/双画面、联动/独立缩放平移、双十字及可折叠本地图像缩略队列。队列真实支持点击、前后、播放/末图停止、进度、失败状态、重载、清空、24 项缓存以及当前/选中/全部真实流程运行、逐图状态和批次汇总；统一“全部图层”管理 ROI、屏蔽区和本周期 Region/XLD，透视四边形可编辑，真实 XLD 可单条检查。Shape/HDevelop 仅显示待迁移状态。显示和预览只使用托管对象，不取得运行输出所有权。
- ROI 图层、矩形/圆/旋转矩形/多边形/椭圆/圆环/线/点、显隐/启停/命名/删除、工具级多 ROI 绑定、检测/屏蔽角色、逐 ROI 结果和 HALCON Region 执行；八类 ROI 支持控制点与常驻数值几何双向编辑、草稿/预览/应用/取消、复制、锁定、边界/非法几何、配方及撤销/重做。面 ROI 屏蔽真实从合并或逐项检测域扣除，显隐不改变算法。极坐标、裁剪、缩放、镜像、旋转、仿射与透视输出会隔离输入坐标 ROI/测量域叠加；透视工具按四点 `normalized_dlt` 和 0.5 像素中心补偿提供三种输出尺寸、domain 与托管 Image。
- 单方案 JSON v2 当前/最近/恢复/自检、脏状态、原子保存、单份备份和独立恢复副本。
- TCP Client/Server 基础手动收发与结果自动发送；S53 另有可订阅 String/Number/ResultCode 的一次性通用 TCP 客户端流程工具，S54-S55 又增加通用文本组装/解析并以 `Text -> TCP.String`、`Text -> Parser -> Number` 真链验证。持久会话、心跳、重连、工业协议帧编排、字段映射和真实设备仍缺。
- 内存结果、CSV/XLSX、日志、报警、良率和节拍基础。
- 开发/运行模式与独立生产运行页复用同一当前方案、确定性执行器、HALCON 图像/叠加、运行控制、图片批次、OK/NG、良率、节拍、报警和最近结果；运行模式隔离开发编辑，切换不复制状态或托管快照。S44-S49 的本地批次记录可保存受管源图/结果图/Region/XLD 和逐工具 Number/String 摘要，并从开发/生产同源入口进行连续导航、双图对比、历史图层、逐工具筛选、ToolId 双样本差异和 Number 趋势；返回实时恢复原视口且不触发流程。

### 12.2 与 VM 的真实差距

- 主分区已纠正，真实端口、二维连接、依赖分层、分支/汇聚、连接导航、OUT→IN 拖线、模块复制粘贴、画布多选/批量动作、用户节点位置持久化、首批真实撤销/重做、组织型 Group、执行前断点/单步、常驻调试监视和运行到所选已可用，但执行仍是确定性线性调度；没有表达式监视、运行到光标、全量动作历史面板、真实分支并行、执行型 Group、多流程和完整调度，单个 HALCON 算子也不可中断。
- 首批主要参数已迁入常驻/停靠工作台；S54-S55 为 51 类工具统一最多 6 个默认关键输入和单一高级分组，高级错误自动展开并就地提示。参数任务头已压为两行且低高度配置区最小 300，逻辑 1366×768 可直接看到 4 个真实关键字段；Shape/HDevelop/模型页仍依赖兼容弹窗，通用 Schema 和连续试运行不完整。
- 图像区已有模块/全局图像、单/双画面输入输出对比、联动/独立视图、像素值、双十字辅助线、连续 Image/Region/XLD 叠加、八类 ROI、工具级屏蔽区、统一图层属性、XLD 单对象检查、三类图像内测量域、透视双四边形编辑及可折叠本地图像缩略队列；图像仿射/透视和 XLD 仿射/透视均进入执行/配方链，队列已支持当前/选中/全部真实流程运行及逐图汇总。Shape/HDevelop 仍只是待迁移兼容项，圆环壁厚/通用测量几何编辑、位置修正、ROI 继承、多画面和完整中间/结果图链仍缺；十字线也尚无标签、快捷键和撤销/重做。
- Image/Region/XLD/Number 已形成连续链、真实二维连接及双 Image/双 Region/双 XLD 汇聚，Image 可经极坐标展开或 ROI 提取继续驱动 Region/数值工具；但逐项坐标/距离/索引/厚度仍是字符串，通用几何对象/姿态、类型化数组、标定映射和完整模块目录仍缺。
- 相机服务和旧 WinForms 能力对 WPF 用户不可达；没有资源管理闭环。
- 计算已有数值判定、S54 文本组装和 S55 文本解析；S53 已有首个通用 TCP 通信流程工具，但通用数值运算、逻辑组合、数组、表达式、坐标、标定和 BSB 工具仍没有完整入口。
- S42-S43 已有独立生产运行页和受控部件配置；S44-S49 已有本地持久批次、图像/图层回看、逐工具结构化标量结果、双样本 ToolId 差异和跨图片 Number 趋势，但完整任意控件设计器、多流程、全局变量/触发/脚本/数据队列、权限、企业归档、诊断和部署仍未实现。
- S49-S55 已把低频工具命令、工具箱长描述、节点完整端口、调试监视、图层浮层、底部表格、顶部/图像命令、右侧任务、工具字段和低高度参数空间逐步改为渐进展开。S55 已解决右侧首屏看不到真实字段的问题；当前顶部大标题、方案摘要、五个动作和四块统计卡仍占高，S56 必须压缩为最多两行紧凑应用栏，不得因本轮改善而标记为接近完整 VM。

因此当前只能称为“VM 化早期工作台”，不能称为“接近完整 VM”。

S39 最终门禁证据：旧 v1 与 S09-S39 共 32/32 功能回归、1938×1158 物理鼠标 XLD 单轮廓命中/检查器和相邻连接活动 UI、462×179 参数区通过；Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证。

S40 最终门禁证据：旧 v1 与 S09-S40 共 33/33 功能回归、1938×1158 本地图像缩略队列和 XLD 平滑常驻配置活动 UI 通过；31 项混合/损坏队列、24 项缓存、末图停止以及多/短/空 XLD、3/5/7/9、非法参数、属性语义和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证；Git 当前 `5/10`，按规则不提交或推送。

S41 最终门禁证据：旧 v1 与 S09-S41 共 34/34 功能回归、1938×1158 图片运行范围/批次汇总和 XLD 仿射常驻配置/结果叠加活动 UI 通过；当前/选中/全部、停止/超时/遇 NG 停止、损坏/删除、重复运行、互斥，以及恒等/平移/中心旋转/各向异性缩放/斜切、四种组合顺序、坐标策略、空/非法、属性语义和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证；Git 当前 `6/10`，按规则不提交或推送。

S42 最终门禁证据：旧 v1 与 S09-S42 共 35/35 功能回归、两张 1938×1158 生产运行页及 XLD 透视常驻配置/结果叠加活动 UI 通过；真实状态复用、开发编辑隔离、运行/队列控制，以及恒等/平移/梯形、两种坐标策略、开/闭/多/空、退化/自交/非有限、输入所有权、旧快照释放、配方和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证；Git 当前 `7/10`，按规则不提交或推送。

S47 最终门禁证据：旧配方与 S09-S47 共 40/40 功能回归、两个 1938×1158 历史逐工具明细与 XLD 面积/方向常驻配置实体 UI、HALCON 导出通过；跨重启、旧/损坏记录、搜索/判定筛选、工具图层联动、实时隔离，以及面积闭区间、正/反向、开放/自交/退化/多/空、非法参数、输入所有权、上游与旧快照释放、配方和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证；Git 新批次 `2/10`，按规则不提交或推送。

S47 HALCON 契约依据：[MVTec HALCON 20.11 `area_center_xld`](https://www.mvtec.com/doc/halcon/2011/en/area_center_xld.html) 明确开放轮廓会被人为闭合，`positive` 表示数学逆时针而 `negative` 表示顺时针；[`test_self_intersection_xld`](https://www.mvtec.com/doc/halcon/2011/en/test_self_intersection_xld.html) 用于在面积计算前排除自交轮廓。本机 HALCON 20.11 程序集反射与正反向矩形夹具另确认了精确 .NET 签名和方向映射。

S48 最终门禁证据：旧配方与 S09-S48 共 41/41 功能回归、两个 1938×1158 历史双样本差异与 XLD 外接框/紧凑度常驻配置实体 UI、HALCON 导出通过；ToolId 对齐、增删/结果/输出/耗时差、Number Δ、旧记录降级、左右图层关联、实时隔离，以及宽高/紧凑度边界、开放/自交/退化/多/空、非法参数、输入所有权、上游与旧快照释放、配方和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；BSB 按用户指令未实施/未验证；Git 新批次 `3/10`，按规则不提交或推送。

S48 HALCON 契约依据：[MVTec HALCON 20.11 `smallest_rectangle1_xld`](https://www.mvtec.com/doc/halcon/2011/en/smallest_rectangle1_xld.html) 提供轴对齐最小外接矩形；[`compactness_xld`](https://www.mvtec.com/doc/halcon/2011/en/compactness_xld.html) 以轮廓长度平方和面积定义紧凑度，圆为 1；[`test_self_intersection_xld`](https://www.mvtec.com/doc/halcon/2011/en/test_self_intersection_xld.html) 用于先排除无意义的自交轮廓。本机 HALCON 20.11 程序集反射另确认精确 .NET 签名。历史差异布局是基于官方 VM 历史查看、结果、双画面和图层习惯的产品化推导，不冒充海康内部实现。

S49 最终门禁证据：严格只读历史 Number 趋势的稳定 ToolId/端口、跨批次、判定/时间筛选、缺失/旧记录降级、点击回看和实时隔离通过；顶部、流程、图像三处低频命令默认折叠的实体 1938×1158 UI 通过。HALCON 圆/矩形/凹形、正反点序、开放/自交/退化、多/空、闭区间、非法参数、输入所有权、上游与旧快照释放、配方和下游通过。完整旧配方与 S09-S49 42/42 在最终纯 XAML 减负前通过，减负后 7 组直接相关回归通过；Debug/Release 0 警告 0 错误，272 个 XAML 事件入口无缺失，双版本独立启动 8 秒且 `Responding=True`。S49 的趋势布局和命令分组是基于官方图形化、模块化和历史结果习惯的产品化推导，不冒充海康内部布局；实体截图仍显示工具箱长卡、节点端口墙和右侧多任务面过密，S50 必须继续整改。

S49 HALCON 契约依据：[MVTec `circularity_xld`](https://www.mvtec.com/doc/halcon/2205/en/circularity_xld.html) 将轮廓与理想圆比较并返回 0..1；[`convexity_xld`](https://www.mvtec.com/doc/halcon/11/en/convexity_xld.html) 以原轮廓面积与凸包面积关系衡量凸性，凸轮廓为 1；[`test_self_intersection_xld`](https://www.mvtec.com/doc/halcon/2205/en/test_self_intersection_xld.html) 用于排除自交轮廓。本机 HALCON 20.11 程序集反射另确认精确 .NET 签名；实现先显式检查闭合、点数、自交和面积，再调用特征算子。

S50 最终门禁证据：紧凑工具目录、流程节点渐进端口、默认隐藏调试、右侧互斥图层任务页、底部折叠记忆和低频按钮移出首屏通过；逻辑 1366×768（物理 1708×960）和 1938×1158 UI 无第二排命令。HALCON 圆/矩形/凹形、正反点序、开放/闭合、自交/退化、多/空、策略与闭区间、非法参数、输入所有权、上游与旧快照释放、配方、托管圆/直径线和真实下游通过。旧 v1 与 S09-S50 共 43/43；Debug/Release 0 警告 0 错误，276 个 XAML 事件入口无缺失，双版本独立启动 8 秒且 `Responding=True`。S50 的任务面和渐进展开是基于官方图形化、模块化与当前对象配置习惯的产品化推导，不冒充海康内部布局。

S50 HALCON 契约依据：[MVTec `smallest_circle_xld`](https://www.mvtec.com/doc/halcon/2411/en/smallest_circle_xld.html) 计算包含轮廓的最小圆并返回圆心与半径；[`diameter_xld`](https://www.mvtec.com/doc/halcon/2011/en/diameter_xld.html) 返回轮廓点之间最大距离及对应端点坐标。本机 HALCON 20.11 程序集反射另确认精确 .NET 签名；实现显式处理点数、开闭策略、自交、退化和非有限结果。

S51 最终门禁证据：顶部永久动作 8→5、图像永久动作约 9→4，低频能力在两步内可达；1938×1158 与逻辑 1366×768（物理 1708×960）实体截图无命令换行。HALCON 旋转矩形、方向半周等价、开放/闭合、自交点集、退化、多/空、尺寸闭区间、非法参数、输入所有权、旧快照释放、配方、托管矩形图层和真实 XLD/Number 下游通过。旧 v1 与 S09-S51 共 44/44；Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。BSB 未实施/未验证；Git 当前 `6/10`，不提交或推送。

S51 HALCON 契约依据：[MVTec `smallest_rectangle2_xld`](https://www.mvtec.com/doc/halcon/11/en/smallest_rectangle2_xld.html) 计算最小面积任意方向包围矩形并输出 `Row/Column/Phi/Length1/Length2`，且 `Length2 <= Length1`；[`gen_rectangle2_contour_xld`](https://www.mvtec.com/doc/halcon/1905/en/gen_rectangle2_contour_xld.html) 由同一几何参数生成矩形 XLD。本机 HALCON 20.11 反射确认精确 .NET 签名；实现将 Phi 按半周等价归一化，自交按点集测量，退化轮廓明确跳过。

S52 最终门禁证据：选中节点保持 84 px 关键摘要、完整端口按需展开；同一来源/目标的多路标签合并为一个短摘要；工具箱默认从全部目录改为 8 个常用工具，收藏/最近/全部/搜索一步可达。HALCON 主轴方向、180° 等价、有符号/绝对目标角差、开放/闭合、正反点序、自交点集、近正方形不稳定、退化、多/空、非法参数、输入所有权、旧快照释放、配方、托管矩形+主轴图层和真实 XLD/Number 下游通过。旧 v1 与 S09-S52 共 45/45；1938×1158 与逻辑 1366×768（物理 1708×960）UI、HALCON 图形、Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。BSB 未实施/未验证；Git 当前 `7/10`，不提交或推送。

S52 HALCON 契约依据：[MVTec `smallest_rectangle2_xld`](https://www.mvtec.com/doc/halcon/11/en/smallest_rectangle2_xld.html) 返回最小面积任意方向包围矩形及主轴 `Phi`，且 `Length2 <= Length1`。S52 在该官方几何结果之上做产品化推导：把无向主轴归一化为 `[-90°,90°)`，计算相对目标角的半周最短有符号偏差，并用宽高比阈值显式排除近正方形方向不稳定结果；不冒充海康内部算法。

S53 最终门禁证据：右侧一级任务入口固定为参数/I/O/结果，运行设置、实例、ROI/模板、帮助、图层、通讯和方案进入更多；无草稿时应用/撤销不占首屏。通用 TCP 的 UTF-8/ASCII、CR/LF/CRLF、上游模板替换、响应/空响应、拒绝、超时、停止、非法配置、资源释放、配方、复制和真实 Number 下游由本机 loopback 通过。旧 v1 与 S09-S53 共 46/46；1938×1158 与逻辑 1366×768 UI、HALCON 图形、763 个事件引用/293 个唯一入口/0 缺失、Debug/Release 0 警告 0 错误及双版本 8 秒 `Responding=True` 通过。真实网络设备、PLC/产线协议和 BSB 未验证；Git 当前 `8/10`，不提交或推送。

S53 产品契约依据：海康[VM 产品页](https://www.hikrobotics.com/cn/machinevision/visionmaster/)强调全图形化、模块化拖拽与外部资源管理；[VM 算法开发平台官方手册](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)的通讯说明采用输入配置、组装配置、订阅/自定义变量和分隔符。S53 只据此实现同类任务组织和通用模板通信，不复制海康控件、资产、内部代码或专有协议。

S54 最终门禁证据：50 类右侧常驻配置工具默认最多显示 6 个真实输入控件；简单图像滤波为 4 个关键项、文本组装为 3 个关键项和 1 个高级项，高级错误自动展开并显示分组内原因。通用文本组装的中文/ASCII/空值、Keep/Skip/Reject、转义换行、InvariantCulture 格式、非法格式、配方、复制、撤销/重做、Length 数值下游和 `Text -> TCP.String` loopback 通过。旧 v1 与 S09-S54 共 47/47；1938×1158 与逻辑 1366×768（物理 1708×960）UI、HALCON 图形、287 个唯一事件入口/0 缺失、Debug/Release 0 警告 0 错误及双版本 8 秒 `Responding=True` 通过。真实网络设备、PLC/产线协议和 BSB 未验证；Git 当前 `9/10`，不提交或推送。

S54 产品契约依据：继续采用官方 VM 模块化、输入订阅、模块配置和通讯组装习惯，只实现同类渐进参数与通用文本能力；关键/高级字段分组、错误展开和具体视觉样式属于本项目产品化设计，不复制海康控件、资产、内部代码或专有协议。

S55 最终门禁证据：参数任务头压为两行、配置区最小高度 300、图像区最小高度 230；逻辑 1366×768 直接显示图像滤波 4 个关键参数，1938×1158 直接显示文本解析 4 个关键参数和高级入口。文本解析覆盖字面分隔、正则首捕获组、一基索引、Trim、Keep/Skip/Reject、转义换行、越界、非法正则/数值、旧配方、复制、撤销/重做及真实 Text/Number 下游。旧 v1 与 S09-S55 共 48/48；287 个唯一 XAML 事件入口/0 缺失；Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实网络设备、PLC/产线协议和 BSB 未验证；Git 批次达到 `10/10`，本轮统一提交推送。

S55 产品契约依据：继续采用官方 VM 模块化、输入订阅、当前对象配置和渐进展开习惯；低高度空间分配、两行任务头和通用文本解析语义属于本项目产品化设计，不复制海康控件、资产、内部代码或专有协议。

## 13. 完成定义

只有同时满足以下条件，某一能力才可标记为 `已验证实现`：

1. 用户能从活动 UI 找到入口。
2. 能完成配置、输入订阅、执行、输出、图形、日志和异常闭环。
3. 能保存、重载并兼容旧配方。
4. 有正常、边界和失败路径测试。
5. Debug/Release 编译、启动和相关 HALCON 回归通过。
6. 界面截图证明布局、状态和高 DPI 可用。
7. 文档与实际代码一致。

完整产品验收还需使用正式的 VM 对照清单，逐页验证开发主界面、模块配置、资源、通信、运行界面、生产运行、历史诊断和目标工具目录；只完成内部模型、空页面或按钮不算完成。
