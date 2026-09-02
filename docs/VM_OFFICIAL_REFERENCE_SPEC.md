# 海康 VM 官方参考产品规范

更新日期：2026-08-27
用途：定义本项目要复刻的 VM / VisionMaster 产品结构、交互习惯、功能边界和验收口径。
状态：长期产品基线；实现状态仍以代码和可复现验证为准。

## 1. 结论

本项目中的 VM 始终只指海康机器人 VM / VisionMaster。最终产品目标不是“借鉴 VM 配色”，而是在不复制海康专有代码、商标和美术资产的前提下，尽量完整复刻其页面分区、工程组织、流程搭建、模块配置、图像/ROI、输入输出订阅、调试运行、资源管理、运行界面和现场运维习惯；全部视觉算法和视觉算子由 HALCON 实现。视觉外观允许形成项目自己的克制型工业设计风格，VM 对齐重点是操作逻辑和信息架构，不是逐像素复制皮肤。
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
   - 同一手册还确认全局脚本/全局变量具有输入输出参数 API，自定义模块生成器可生成 XML、C++ 工程和 C# 工程，并将编译后的 DLL/XML 资产放入模块目录成为自定义工具。
   - [海康机器人视觉学院 VM 课程](https://www.hikrobotics.com/cn/machinevision/service/vcollege/video/?module=%E7%AE%97%E6%B3%95%E5%B9%B3%E5%8F%B0)列出“脚本和全局变量”以及自定义算法工具/算子封装二次开发课程，作为脚本与 DLL 扩展操作方向的补充官方证据。
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
30. 官方 VM 手册“基本参数/ROI 区域”明确说明图形 ROI 可设置屏蔽区，模块运行时不检测被屏蔽部分；还支持 ROI 绘制/继承和位置修正。S36 已交付工具级屏蔽区与真实差集执行，S62 又交付前序 2D 修正 ToolId、HALCON 刚体矩阵及检测 ROI/屏蔽区运行时共同跟随；基础 ROI DTO 不改写。ROI 继承仍未实现，S62 也不冒充标定或跨坐标系转换。MVTec HALCON 20.11 官方 [`affine_trans_image`](https://www.mvtec.com/doc/halcon/2011/en/affine_trans_image.html)确认可执行缩放、旋转、平移和斜切，使用 `HomMat2D + Interpolation + AdaptImageSize`；输入 domain 被忽略，输出 domain 为变换矩形与输出矩形的交集，`AdaptImageSize=true` 仍会裁掉变换后的负行/列。本机 20.11 反射确认 `HImage.AffineTransImage(HHomMat2D,string,string)`，S36 已落实 HALCON 行映射矩阵 x、列映射 y、0.5 像素中心校正和输出安全尺寸保护。
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
| 页面/模式| 核心用户任务| 必需区域|
| ---| ---| ---|
| 欢迎/启动页| 新建方案、选择方案类型、打开最近方案、示例与帮助| 最近方案、方案模板、帮助入口、版本/许可状态|
| 方案开发主界面| 搭建和调试流程| 菜单栏、快捷工具条、当前方案、角色、模块箱、流程编辑、图像显示、结果显示、日志/状态|
| 模块配置页/窗口| 配置一个工具的全部行为| 基本参数、运行参数、输入订阅、ROI/模板、结果显示、模型页、试运行、确定/取消、错误帮助|
| 流程逻辑配置| 多流程、串并行、触发和数据交换| 流程列表/标签、全局相机、数据队列、分支汇聚、运行参数|
| 硬件资源管理| 配置相机、控制器、IO、光源| 设备列表、连接状态、参数、实时预览、触发、诊断|
| 通信管理| 配置设备和收发逻辑| 设备、协议、地址、接收事件、发送事件、心跳、响应、收发监视、手动测试|
| 全局逻辑| 管理跨流程数据与控制| 全局变量、全局触发、全局脚本、数据队列、订阅关系|
| 运行界面设计| 为现场操作员设计 HMI| 控件箱、预览编辑区、属性区、图层、保存/导出|
| 生产运行界面| 启停、监控、换型和报警| 图像、结果、OK/NG、节拍、设备状态、报警、配方、受控操作|
| 历史与诊断| 追溯和排障| 运行历史、图像回放、日志、通信报文、设备/配方/版本、自检和导出|
| 系统设置| 权限、软件、方案和运行策略| 角色权限、通用设置、语言、License、插件、运行策略|

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
- 自定义模块必须公开参数、输入输出和可用状态；本项目将官方 C#/C++ 工程、DLL/XML 入目录的能力产品化为受信任 C#/DLL 流程工具，并补充版本/哈希、签名、诊断、超时和配方契约，不宣称这些安全边界等同于海康内部实现。

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
- 脚本/自定义模块不是隐藏代码钩子：参数传入、类型化输出、编译或装载状态、异常、版本和调用关系必须能从活动 UI 追踪。

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
- 受信任 C#/DLL 扩展必须作为正式工具处理，不得绕开端口、日志、配方和版本；用户代码不冒充安全沙箱，视觉算法仍以 HALCON/HDevEngine 为核心。
- 不提交海康手册、官方截图、商标、图标或其他受保护资产；只在文档中链接官方来源并使用自制图标/布局实现同类工作方式。

## 12. 当前代码真实基线（S66 已验证；新 Git 批次 1/10，不提交或推送）

### 12.1 已验证实现

- 活动 WPF 已采用左模块箱、中央网格顺序流程、右上 HALCON 图像/ROI、右下结果/配置/方案和底部日志/历史分区；S62 模块箱恢复当时全部 59 个工具，S65 增至 60 个，S66 增至 61 个并按 9 类折叠，支持搜索、常用、收藏、最近、按需契约、双击/Enter/拖拽追加，以及围绕所选节点插入前/后、自动订阅和安全连接桥接，不再以 8 个常用项作为默认能力上限。
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
- 首批主要参数已迁入常驻/停靠工作台；S54-S63 为主要工具维持最多 6 个默认关键输入和单一高级分组，高级错误自动展开并就地提示。2D 位置修正基准位姿/ROI 修正来源及 Shape 模型/训练/候选进入当前任务；脚本代码/DLL 入口默认折叠，变量/资源/脚本接口只随当前工具或任务出现。HDevelop 程序页仍依赖兼容入口，通用工具 Schema 和连续试运行不完整。
- 图像区已有模块/全局图像、单/双画面输入输出对比、联动/独立视图、像素值、双十字辅助线、连续 Image/Region/XLD 叠加、八类 ROI、工具级屏蔽区、2D 修正后检测/屏蔽 ROI 预览、Shape 训练/搜索区与托管候选 XLD、统一图层属性、XLD 单对象检查、三类图像内测量域、透视双四边形编辑及可折叠本地图像缩略队列。HDevelop 仍是待迁移兼容项，圆环壁厚/通用测量几何编辑、ROI 继承、多画面和完整中间/结果图链仍缺；十字线也尚无标签、快捷键和撤销/重做。
- Image/Region/XLD/Number 已形成连续链、真实二维连接及双 Image/双 Region/双 XLD 汇聚；S62-S66 已补同图像 Pose2D、Point2D/Point2D[]、Line2D、HALCON HomMat2D/位姿差、ROI/屏蔽区跟随、点集统计和点线距离。但部分逐项坐标/距离/索引/厚度仍是字符串，Circle/通用 Geometry/HObject、类型化 HomMat2D、标定映射和完整模块目录仍缺。
- 相机服务和旧 WinForms 能力对 WPF 用户不可达；S60 只有受管 .NET DLL 资源，尚无相机/控制器/IO/光源资源闭环。
- 计算已有数值判定、文本组装/解析、八类通用数值运算、结果逻辑汇总、S58 受信任 C#/DLL 脚本、S59 四类全局变量读写、S60 ResourceId 受管 DLL 和 S61 命名动态标量端口；S53 已有首个通用 TCP 通信流程工具。变量和资源只承诺当前单方案本地确定性状态。HDevelop 文件执行只属于兼容路径，不能计作通用脚本平台。
- S42-S43 已有独立生产运行页和受控部件配置；S44-S49 已有本地持久批次、图像/图层回看、逐工具结构化标量结果、双样本 ToolId 差异和跨图片 Number 趋势；S59-S61 已有按需全局变量、读写工具、外部资源和脚本接口工作台，但完整任意控件设计器、多流程、全局触发/脚本/数据队列、权限、企业归档、诊断和部署仍未实现。
- S49-S61 已把低频命令、节点端口、调试、图层、底部、顶部/图像命令、右侧任务、工具字段、变量、资源和脚本接口逐步改为渐进展开。S62 明确修正“安静=隐藏能力”的错误倾向：完整 59 工具/9 类默认可发现，但工具行、分类、说明和动作仍紧凑，顶部无按钮回流。后续 Shape/标定/坐标/触发/队列页面也必须同时满足能力可发现和当前任务聚焦。
因此当前只能称为“VM 化早期工作台”，不能称为“接近完整 VM”。
S39 最终门禁证据：旧 v1 与 S09-S39 共 32/32 功能回归、1938×1158 物理鼠标 XLD 单轮廓命中/检查器和相邻连接活动 UI、462×179 参数区通过；Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证。
S40 最终门禁证据：旧 v1 与 S09-S40 共 33/33 功能回归、1938×1158 本地图像缩略队列和 XLD 平滑常驻配置活动 UI 通过；31 项混合/损坏队列、24 项缓存、末图停止以及多/短/空 XLD、3/5/7/9、非法参数、属性语义和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；Git 当前 `5/10`，按规则不提交或推送。
S41 最终门禁证据：旧 v1 与 S09-S41 共 34/34 功能回归、1938×1158 图片运行范围/批次汇总和 XLD 仿射常驻配置/结果叠加活动 UI 通过；当前/选中/全部、停止/超时/遇 NG 停止、损坏/删除、重复运行、互斥，以及恒等/平移/中心旋转/各向异性缩放/斜切、四种组合顺序、坐标策略、空/非法、属性语义和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；Git 当前 `6/10`，按规则不提交或推送。
S42 最终门禁证据：旧 v1 与 S09-S42 共 35/35 功能回归、两张 1938×1158 生产运行页及 XLD 透视常驻配置/结果叠加活动 UI 通过；真实状态复用、开发编辑隔离、运行/队列控制，以及恒等/平移/梯形、两种坐标策略、开/闭/多/空、退化/自交/非有限、输入所有权、旧快照释放、配方和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；Git 当前 `7/10`，按规则不提交或推送。
S47 最终门禁证据：旧配方与 S09-S47 共 40/40 功能回归、两个 1938×1158 历史逐工具明细与 XLD 面积/方向常驻配置实体 UI、HALCON 导出通过；跨重启、旧/损坏记录、搜索/判定筛选、工具图层联动、实时隔离，以及面积闭区间、正/反向、开放/自交/退化/多/空、非法参数、输入所有权、上游与旧快照释放、配方和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；Git 新批次 `2/10`，按规则不提交或推送。
S47 HALCON 契约依据：[MVTec HALCON 20.11 `area_center_xld`](https://www.mvtec.com/doc/halcon/2011/en/area_center_xld.html) 明确开放轮廓会被人为闭合，`positive` 表示数学逆时针而 `negative` 表示顺时针；[`test_self_intersection_xld`](https://www.mvtec.com/doc/halcon/2011/en/test_self_intersection_xld.html) 用于在面积计算前排除自交轮廓。本机 HALCON 20.11 程序集反射与正反向矩形夹具另确认了精确 .NET 签名和方向映射。
S48 最终门禁证据：旧配方与 S09-S48 共 41/41 功能回归、两个 1938×1158 历史双样本差异与 XLD 外接框/紧凑度常驻配置实体 UI、HALCON 导出通过；ToolId 对齐、增删/结果/输出/耗时差、Number Δ、旧记录降级、左右图层关联、实时隔离，以及宽高/紧凑度边界、开放/自交/退化/多/空、非法参数、输入所有权、上游与旧快照释放、配方和下游链专项通过。Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`。真实外部产线图像、Shape 模板、相机和 `.jw/.handle` 因缺样本/设备未验证；Git 新批次 `3/10`，按规则不提交或推送。
S48 HALCON 契约依据：[MVTec HALCON 20.11 `smallest_rectangle1_xld`](https://www.mvtec.com/doc/halcon/2011/en/smallest_rectangle1_xld.html) 提供轴对齐最小外接矩形；[`compactness_xld`](https://www.mvtec.com/doc/halcon/2011/en/compactness_xld.html) 以轮廓长度平方和面积定义紧凑度，圆为 1；[`test_self_intersection_xld`](https://www.mvtec.com/doc/halcon/2011/en/test_self_intersection_xld.html) 用于先排除无意义的自交轮廓。本机 HALCON 20.11 程序集反射另确认精确 .NET 签名。历史差异布局是基于官方 VM 历史查看、结果、双画面和图层习惯的产品化推导，不冒充海康内部实现。
S49 最终门禁证据：严格只读历史 Number 趋势的稳定 ToolId/端口、跨批次、判定/时间筛选、缺失/旧记录降级、点击回看和实时隔离通过；顶部、流程、图像三处低频命令默认折叠的实体 1938×1158 UI 通过。HALCON 圆/矩形/凹形、正反点序、开放/自交/退化、多/空、闭区间、非法参数、输入所有权、上游与旧快照释放、配方和下游通过。完整旧配方与 S09-S49 42/42 在最终纯 XAML 减负前通过，减负后 7 组直接相关回归通过；Debug/Release 0 警告 0 错误，272 个 XAML 事件入口无缺失，双版本独立启动 8 秒且 `Responding=True`。S49 的趋势布局和命令分组是基于官方图形化、模块化和历史结果习惯的产品化推导，不冒充海康内部布局；实体截图仍显示工具箱长卡、节点端口墙和右侧多任务面过密，S50 必须继续整改。
S49 HALCON 契约依据：[MVTec `circularity_xld`](https://www.mvtec.com/doc/halcon/2205/en/circularity_xld.html) 将轮廓与理想圆比较并返回 0..1；[`convexity_xld`](https://www.mvtec.com/doc/halcon/11/en/convexity_xld.html) 以原轮廓面积与凸包面积关系衡量凸性，凸轮廓为 1；[`test_self_intersection_xld`](https://www.mvtec.com/doc/halcon/2205/en/test_self_intersection_xld.html) 用于排除自交轮廓。本机 HALCON 20.11 程序集反射另确认精确 .NET 签名；实现先显式检查闭合、点数、自交和面积，再调用特征算子。
S50 最终门禁证据：紧凑工具目录、流程节点渐进端口、默认隐藏调试、右侧互斥图层任务页、底部折叠记忆和低频按钮移出首屏通过；逻辑 1366×768（物理 1708×960）和 1938×1158 UI 无第二排命令。HALCON 圆/矩形/凹形、正反点序、开放/闭合、自交/退化、多/空、策略与闭区间、非法参数、输入所有权、上游与旧快照释放、配方、托管圆/直径线和真实下游通过。旧 v1 与 S09-S50 共 43/43；Debug/Release 0 警告 0 错误，276 个 XAML 事件入口无缺失，双版本独立启动 8 秒且 `Responding=True`。S50 的任务面和渐进展开是基于官方图形化、模块化与当前对象配置习惯的产品化推导，不冒充海康内部布局。
S50 HALCON 契约依据：[MVTec `smallest_circle_xld`](https://www.mvtec.com/doc/halcon/2411/en/smallest_circle_xld.html) 计算包含轮廓的最小圆并返回圆心与半径；[`diameter_xld`](https://www.mvtec.com/doc/halcon/2011/en/diameter_xld.html) 返回轮廓点之间最大距离及对应端点坐标。本机 HALCON 20.11 程序集反射另确认精确 .NET 签名；实现显式处理点数、开闭策略、自交、退化和非有限结果。
S51 最终门禁证据：顶部永久动作 8→5、图像永久动作约 9→4，低频能力在两步内可达；1938×1158 与逻辑 1366×768（物理 1708×960）实体截图无命令换行。HALCON 旋转矩形、方向半周等价、开放/闭合、自交点集、退化、多/空、尺寸闭区间、非法参数、输入所有权、旧快照释放、配方、托管矩形图层和真实 XLD/Number 下游通过。旧 v1 与 S09-S51 共 44/44；Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`；Git 当前 `6/10`，不提交或推送。
S51 HALCON 契约依据：[MVTec `smallest_rectangle2_xld`](https://www.mvtec.com/doc/halcon/11/en/smallest_rectangle2_xld.html) 计算最小面积任意方向包围矩形并输出 `Row/Column/Phi/Length1/Length2`，且 `Length2 <= Length1`；[`gen_rectangle2_contour_xld`](https://www.mvtec.com/doc/halcon/1905/en/gen_rectangle2_contour_xld.html) 由同一几何参数生成矩形 XLD。本机 HALCON 20.11 反射确认精确 .NET 签名；实现将 Phi 按半周等价归一化，自交按点集测量，退化轮廓明确跳过。
S52 最终门禁证据：选中节点保持 84 px 关键摘要、完整端口按需展开；同一来源/目标的多路标签合并为一个短摘要；工具箱默认从全部目录改为 8 个常用工具，收藏/最近/全部/搜索一步可达。HALCON 主轴方向、180° 等价、有符号/绝对目标角差、开放/闭合、正反点序、自交点集、近正方形不稳定、退化、多/空、非法参数、输入所有权、旧快照释放、配方、托管矩形+主轴图层和真实 XLD/Number 下游通过。旧 v1 与 S09-S52 共 45/45；1938×1158 与逻辑 1366×768（物理 1708×960）UI、HALCON 图形、Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`；Git 当前 `7/10`，不提交或推送。
S52 HALCON 契约依据：[MVTec `smallest_rectangle2_xld`](https://www.mvtec.com/doc/halcon/11/en/smallest_rectangle2_xld.html) 返回最小面积任意方向包围矩形及主轴 `Phi`，且 `Length2 <= Length1`。S52 在该官方几何结果之上做产品化推导：把无向主轴归一化为 `[-90°,90°)`，计算相对目标角的半周最短有符号偏差，并用宽高比阈值显式排除近正方形方向不稳定结果；不冒充海康内部算法。
S53 最终门禁证据：右侧一级任务入口固定为参数/I/O/结果，运行设置、实例、ROI/模板、帮助、图层、通讯和方案进入更多；无草稿时应用/撤销不占首屏。通用 TCP 的 UTF-8/ASCII、CR/LF/CRLF、上游模板替换、响应/空响应、拒绝、超时、停止、非法配置、资源释放、配方、复制和真实 Number 下游由本机 loopback 通过。旧 v1 与 S09-S53 共 46/46；1938×1158 与逻辑 1366×768 UI、HALCON 图形、763 个事件引用/293 个唯一入口/0 缺失、Debug/Release 0 警告 0 错误及双版本 8 秒 `Responding=True` 通过；Git 当前 `8/10`，不提交或推送。
S53 产品契约依据：海康[VM 产品页](https://www.hikrobotics.com/cn/machinevision/visionmaster/)强调全图形化、模块化拖拽与外部资源管理；[VM 算法开发平台官方手册](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)的通讯说明采用输入配置、组装配置、订阅/自定义变量和分隔符。S53 只据此实现同类任务组织和通用模板通信，不复制海康控件、资产、内部代码或专有协议。
S54 最终门禁证据：50 类右侧常驻配置工具默认最多显示 6 个真实输入控件；简单图像滤波为 4 个关键项、文本组装为 3 个关键项和 1 个高级项，高级错误自动展开并显示分组内原因。通用文本组装的中文/ASCII/空值、Keep/Skip/Reject、转义换行、InvariantCulture 格式、非法格式、配方、复制、撤销/重做、Length 数值下游和 `Text -> TCP.String` loopback 通过。旧 v1 与 S09-S54 共 47/47；1938×1158 与逻辑 1366×768（物理 1708×960）UI、HALCON 图形、287 个唯一事件入口/0 缺失、Debug/Release 0 警告 0 错误及双版本 8 秒 `Responding=True` 通过；Git 当前 `9/10`，不提交或推送。
S54 产品契约依据：继续采用官方 VM 模块化、输入订阅、模块配置和通讯组装习惯，只实现同类渐进参数与通用文本能力；关键/高级字段分组、错误展开和具体视觉样式属于本项目产品化设计，不复制海康控件、资产、内部代码或专有协议。
S55 最终门禁证据：参数任务头压为两行、配置区最小高度 300、图像区最小高度 230；逻辑 1366×768 直接显示图像滤波 4 个关键参数，1938×1158 直接显示文本解析 4 个关键参数和高级入口。文本解析覆盖字面分隔、正则首捕获组、一基索引、Trim、Keep/Skip/Reject、转义换行、越界、非法正则/数值、旧配方、复制、撤销/重做及真实 Text/Number 下游。旧 v1 与 S09-S55 共 48/48；287 个唯一 XAML 事件入口/0 缺失；Debug/Release x64 均 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`；Git 批次达到 `10/10`，本轮统一提交推送。
S55 产品契约依据：继续采用官方 VM 模块化、输入订阅、当前对象配置和渐进展开习惯；低高度空间分配、两行任务头和通用文本解析语义属于本项目产品化设计，不复制海康控件、资产、内部代码或专有协议。
S56 最终门禁证据：菜单外顶部收敛为 32 px 方案/状态行和 26 px 高频命令行，四张统计卡变为同行紧凑状态单元，低频命令进入不占布局高度的“更多”悬浮面板；逻辑 1366×768 顶部到主工作区为 94.2 px，无命令换行，工具箱、流程、图像和参数同时可见。通用数值运算的加/减/乘/除/最小/最大/绝对值/四舍五入、正负数、小数、单/双输入、除零、缺失 B、非法精度、NaN/Infinity、旧配方、复制、撤销/重做及真实 Number/String/TCP 下游通过。旧 v1 与 S09-S56 共 49/49；宽口径 294 个唯一事件入口/0 缺失；Debug/Release x64 均 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。最终双尺度图片因外部桌面截屏额度限制采用 WPF 渲染回退，HALCON Host 区域不进入图片，HALCON 功能由回归独立验证；Git 新批次 `1/10`，不提交或推送。
S56 产品契约依据：继续采用官方 VM 的高频命令前置、当前对象状态集中、低频能力渐进展开和类型化输入订阅习惯；紧凑行高、悬浮更多面板及八类数值运算语义属于本项目产品化设计，不复制海康控件、资产、内部代码或专有公式。
S57 最终门禁证据：工具行收藏动作改为收藏状态或悬停/选中时出现，永久大添加按钮移除；双击、拖拽、Enter、右键追加和收藏真实可用，双尺度图确认仅一个上下文收藏动作且永久添加按钮为 0。结果逻辑汇总的 All OK/Any OK/NOT/PassThrough、两路 NumericJudge 汇聚、默认 Trim/大小写策略、严格大小写、空/非法/缺失 B、旧配方、复制、撤销/重做及真实 Text/TCP/Number 下游通过。旧 v1 与 S09-S57 共 50/50；当前宽口径 317 个唯一事件入口/0 缺失；Debug/Release x64 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。双尺度图片采用 WPF 渲染回退，HALCON Host 不进入图片但 HALCON 回归独立通过；Git 新批次 `2/10`，不提交或推送。
S57 产品契约依据：继续采用官方 VM 的模块化、输入订阅、当前对象配置和渐进展开习惯。官方手册确认的全局脚本与 C#/C++ 自定义模块/DLL 入口用于确定 S58 的扩展方向；独立执行边界、版本哈希和诊断契约属于本项目安全与可维护性设计。
S58 最终门禁证据：工具箱永久选中详情卡已移除，说明使用悬停、右键和单行省略；脚本任务只随当前脚本模块显示，代码/DLL 入口默认折叠，双尺度图确认 `PermanentToolDetail=0`、`ScriptEditorDefaultExpanded=0`。受信任内联 C# 和外部 DLL 均通过固定 `ScriptToolContext -> ScriptToolResult` 签名运行，四个可选输入、Number/Text/ResultCode/IsOk、版本、SHA-256、诊断、耗时、NumericJudge/TextAssembly 下游、旧配方、复制和撤销/重做通过；编译、超时、非有限值、缺 DLL/类型/方法、错误签名和运行异常均可见。旧 v1 与 S09-S58 共 51/51；当前宽口径 823 个事件引用、324 个唯一入口/0 缺失；Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。WPF 图片不包含 HALCON Host，HALCON 回归独立通过；Git 新批次 `3/10`，不提交或推送。
S58 产品契约依据：官方确认的全局脚本输入输出与 C#/C++ 自定义模块/DLL 方向用于确定工具定位；固定入口签名、AppDomain 执行边界、SHA-256、失败诊断和右侧渐进布局属于本项目产品化实现，不冒充海康内部模块生成器。AppDomain 明确不是安全沙箱，全局停止仅在确定性工具步骤边界生效。
S59 最终门禁证据：全局变量入口位于菜单、现有“更多”和变量工具上下文，默认首屏不新增永久按钮；双尺度 WPF 图确认关闭后回到流程/图像/参数，打开后只显示紧凑目录与选中详情。Number/String/Boolean/ResultCode 使用稳定 VariableId，读取仅发布活动类型端口，写入仅接受同类型前序值并发布写后值、Changed/ResultCode；名称、非有限值、结果码、类型、顺序、缺值、删除/改类型保护、旧配方、配方、复制、撤销/重做和真实下游通过。旧 v1 与 S09-S59 共 52/52；当前宽口径 839 个事件引用、336 个唯一入口/0 缺失；Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`。WPF 图片不包含 HALCON Host，HALCON 回归独立通过；Git 新批次 `4/10`，不提交或推送。
S59 产品契约依据：官方全局变量/全局脚本和输入输出订阅方向用于确定工作台与类型化读写定位；稳定 ID、单一活动类型端口、确定性写入、订阅删除保护和按需独立工作台属于本项目产品化实现，不冒充海康内部代码。
S60 最终门禁证据：外部资源入口位于菜单、既有“更多”和当前脚本配置，默认首屏仍只有 7 个高频工具栏入口；1938×1158 与 1366×768 WPF 图确认工作台按需替换主工作区，版本、SHA-256、健康、引用、来源、受管路径和入口诊断清楚。稳定 ResourceId、固定签名验证、原子版本化复制、受管副本优先执行、裸路径旧配方兼容、来源更新检测、刷新/替换、缺失/漂移/错误签名/无效 ID 拒绝、被引用删除保护、配方/旧配方/方案重载/复制/撤销重做及 Number/String/ResultCode 下游通过。旧 v1 与 S09-S60 共 53/53；852 个事件引用、339 个唯一入口/0 缺失；Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`；Git 新批次 `5/10`，不提交或推送。
S60 产品契约依据：官方资源、全局脚本/自定义模块和输入输出订阅方向用于确定按需资源工作台与脚本资源引用定位；稳定 ResourceId、应用版本化副本、SHA-256 健康门禁、来源更新不静默替换和引用删除保护属于本项目产品化实现，不冒充海康内部代码。
S61 最终门禁证据：菜单、既有“更多”和当前脚本配置进入按需脚本接口工作台，默认首屏仍只有 7 个高频入口；1938×1158 与 1366×768 WPF 图确认只展开选中成员详情。稳定成员 ID、Number/String/Boolean/ResultCode 命名输入/参数/输出、真实动态端口、内联 C#/受管 DLL、严格 Schema/值校验、旧固定端口、配方、复制、撤销/重做和真实标量下游通过。旧 v1 与 S09-S61 共 54/54；866 个事件引用、351 个唯一入口/0 缺失；Debug/Release 与双版本独立启动通过；Git 新批次 `6/10`，不提交或推送。
S61 产品契约依据：官方自定义模块、输入输出订阅和任务式配置方向用于确定按需接口设计器与用户定义端口定位；稳定成员 ID、固定入口签名上的只读命名集合、严格结果校验和旧端口兼容属于本项目产品化实现，不冒充海康内部 SDK。下一轮优先补官方 VM 操作习惯中的通用位置修正和 ROI 跟随。
S62 最终门禁证据：用户反馈左侧工具不足后，实际代码核验得到 59 个注册工具而非默认可见的 8 个；默认已改为 9 类完整目录，搜索/常用/收藏/最近/双击/Enter/拖拽保持，顶部永久入口仍为 7 个。HALCON `vector_angle_to_rigid` 的真实三 Number 输入、基准/当前位姿、DeltaRow/DeltaColumn/DeltaAngle、HomMat2D、检测 ROI 与屏蔽区共同跟随、基础几何不改写、来源失效、数值下游、配方/旧配方和块复制重映射通过。旧 v1 与 S09-S62 共 55/55；870 个事件引用、351 个唯一入口/0 缺失；1938×1158 与 1366×768 WPF UI、HALCON 图形、Debug/Release 和双版本独立启动通过；Git 新批次 `7/10`，不提交或推送。
S62 产品契约依据：官方 VM 的分类模块目录、输入输出订阅和 ROI 位置修正习惯用于确定完整目录默认可发现、当前 ROI 工具选择修正来源的产品定位；9 类目录、稳定 ToolId、HALCON 刚体矩阵、运行时派生 Region 和不改基础 DTO 属于本项目产品化实现，不冒充海康内部代码。下一轮优先把 Shape Model 迁入统一定位任务并形成多目标位姿到 2D 修正的真链。
S62 HALCON 依据：[MVTec HALCON 20.11 `vector_angle_to_rigid`](https://www.mvtec.com/doc/halcon/2011/en/vector_angle_to_rigid.html)明确由原始点/角度与对应点/角度计算仅包含旋转和平移的刚体仿射 HomMat2D；本轮只据此实现同图像坐标系修正，不外推为标定或跨坐标系变换。
S63 最终门禁证据：Shape 普通路径已从兼容弹窗迁入右侧当前任务，每实例模型、训练 ROI、检测/屏蔽后的搜索 ROI、关键/高级参数、训练/加载/另存、试运行、候选表和图像双向选择可用；Shape 候选轮廓进入托管 XLD 图层，统一图层只保留 HDevelop 兼容项，完整 59 工具/9 类目录和 7 个顶部高频入口不变。HALCON `create_scaled_shape_model + find_scaled_shape_model` 的角度/等比例尺度、多目标、分数/数量/重叠/贪婪度、稳定排序、`.shm`/元数据 v3、XLD/最佳位姿/候选摘要和到 S62 位置修正的真实链通过；无匹配会清除陈旧输出，缺失资源、非法 ROI/参数和上游失效明确诊断。旧 v1 与 S09-S63 共 56/56；885 个事件引用、355 个唯一入口/0 缺失；1938×1158 与 1366×768 实体 WPF UI、Debug/Release 0 警告 0 错误和双版本独立启动 8 秒 `Responding=True` 通过；Git 新批次 `8/10`，不提交或推送。
S63 产品契约依据：官方 VM 的定位模块、模型任务、ROI/屏蔽区、输入输出订阅和当前对象配置习惯用于确定 Shape 当前任务的信息结构；每实例 ToolId 资源、HALCON 等比例多目标实现、候选稳定排序、托管 XLD、候选表与图像双向选择属于本项目产品化实现，不冒充海康内部模型算法或控件。
S63 HALCON 依据：[MVTec HALCON 20.11 `create_scaled_shape_model`](https://www.mvtec.com/doc/halcon/2011/en/create_scaled_shape_model.html)定义等比例尺度范围的 Shape Model 创建，[`find_scaled_shape_model`](https://www.mvtec.com/doc/halcon/2011/en/find_scaled_shape_model.html)定义角度、尺度、最小分数、最大匹配、重叠、亚像素、层数和贪婪度搜索语义；本机 HALCON 20.11 精确 .NET 签名、模型保存/加载、双尺度多目标、候选 XLD、无匹配和释放路径已专项验证。
S64 最终门禁证据：端口数据作为现有调试监视器内默认收起的任务按需出现，只聚合当前监视模块、所选连接和固定端口；端点、来源、真实类型、当前值、单位/坐标上下文、周期及等待/有效/失效/错误状态可见，行选择可定位真实节点/连接并打开 I/O。1938×1158 默认收起和 1366×768 Pose2D 跟踪图证明没有顶部按钮、节点端口墙或默认首屏信息回流。正式 Pose2D 已由 Shape 发布并优先驱动 2D位置修正，旧 Row/Column/Angle 三 Number 链继续兼容；混接、类型、非有限值、上下文、周期、失效、配方、复制重映射、撤销重做、日志和 ROI/屏蔽跟随通过。旧 v1 与 S09-S64 共 57/57；892 个事件引用、362 个唯一入口/0 缺失；Debug/Release 0 警告 0 错误，双版本独立启动 8 秒且 `Responding=True`；Git 新批次 `9/10`，不提交或推送。
S64 产品契约依据：[海康 VM 算法开发平台用户手册](https://pinfo.hikrobotics.com/hkws/unzip/20231101163730_26101_doc/pdf.pdf)中流程模块、单次运行/调试和输入输出组织用于确定“围绕当前任务追踪真实数据”的产品方向；固定端口、状态聚合、周期版本及 Pose2D 数据契约均为本项目产品化实现，不冒充海康内部调试器或坐标类型。
S65 最终门禁证据：左侧模块箱已从 S62 的 59 个增至 60 个真实工具/9 类，分类明确“已实现”；选中工具才展开由真实 `ToolMetadata` 生成的用途、必需/可选 IN、OUT、ROI/图形和成熟度，规划缺口不可拖入。1938×1158 默认收起图与 1366×768 点集契约图证明完整目录、流程、图像和当前参数同时可用，顶部仍只有 7 个高频入口。圆周卡尺从真实 HALCON 测量结果直接发布 `EdgePoints: Point2D[]` 并保留 EdgeRows/EdgeColumns；点集几何统计使用 HALCON 生成点、路径、质心和包围框 XLD，输出点数、质心、边界、跨度、上下文和 ResultCode，并驱动 NumericJudge。类型、点数、非有限值、Reject/SkipInvalid、混合上下文、未运行/陈旧、配方、复制重映射和生命周期通过。Legacy 与 S09-S65 共 58/58；双尺度实体 UI、XAML 入口 0 缺失、Debug/Release 0 警告 0 错误和双版本独立启动 8 秒响应通过；Git 批次达到 `10/10`，本轮统一提交推送。
S65 产品契约依据：官方 VM 的完整模块目录、按当前对象显示参数/输入输出和流程工具订阅习惯用于确定“完整能力可发现、详细契约按需展开”的信息结构；60 工具数量、元数据生成方式、Point2D 类型和点集统计均为本项目产品化实现，不冒充海康内部目录、数据类型或算法实现。
S65 HALCON 依据：圆周卡尺继续使用已验证的 `gen_measure_rectangle2 + measure_pos + close_measure` 获取真实测量点；点集几何通过 HALCON `gen_cross_contour_xld` 与 `gen_contour_polygon_xld` 生成受管 XLD 图形。质心、边界和跨度由同一已校验点集确定，不解析 EdgeRows/EdgeColumns 字符串，托管对象在重跑、失效和删除路径释放。
S66 最终门禁证据：左侧模块箱增至 61 个真实工具/9 类；工具行、流程节点/顺序列表右键和 `Ctrl+Enter`/`Ctrl+Shift+Enter` 可围绕当前锚点插入后/前，能力面按需显示兼容上游、可桥接或补接的下游、缺失类型和拒绝原因。插入写回真实 `flowTools/InputBindings` 并进入撤销/重做，没有增加永久顶部按钮。正式 `Line2D` 可克隆、显示、JSON 往返并校验 ImagePixel 上下文；XLD 直线拟合发布 `MainLine`，点到直线距离输出垂足、距离、有符号侧向值、Side、托管 XLD、参数和 ResultCode，并驱动 NumericJudge。Legacy 与 S09-S66 共 59/59；1938×1158 与 1366×768 实体 UI、353 个唯一 XAML 事件入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒响应通过；新 Git 批次 `1/10`，不提交或推送。
S66 产品契约依据：官方 VM 的模块目录、输入输出订阅和围绕当前流程位置添加模块的操作习惯用于确定“完整能力常在、详细契约和插入动作按上下文出现”的产品方向；61 工具数量、锚点桥接策略、Line2D 数据契约与侧向符号约定均为本项目产品化实现，不冒充海康内部目录、图数据结构或几何类型。
S66 HALCON 依据：现有直线工具继续以 `fit_line_contour_xld` 产生主线端点；点线距离调用 HALCON 20.11 `distance_pl`，图形由受管 XLD 参考线、点、垂足和垂足连线组成。输入所有权、陈旧周期和旧快照释放均纳入专项验证。
S67 最终门禁证据：左侧模块箱增至 62 个真实工具/9 类；选中真实 OUT 端口或连接后才进入可退出的“推荐下一工具”，仅保留至少一个必需 IN 与来源类型兼容的真实工具。横幅显示来源工具/端口/类型、本周期状态、推荐数量、计划插入位置及排除摘要；双击、Enter 或右键会在来源后插入、绑定所选来源、尽力补齐其他兼容必需输入、失效并高亮真实下游，返回全目录恢复分类、搜索、常用、收藏和最近，没有增加永久顶部按钮。正式 `Circle2D` 可克隆、显示、JSON 往返并校验 ImagePixel 上下文；XLD 圆拟合发布 `MainCircle`，点到圆径向偏差输出最近圆周点、中心距、绝对/有符号偏差、径向状态、托管 XLD、参数和 ResultCode，并驱动 NumericJudge。Legacy 与 S09-S67 共 60/60；1938×1158 与 1366×768 实体 UI、当前宽口径 379 个唯一 XAML 事件引用/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒响应通过；新 Git 批次 `2/10`，不提交或推送。
S67 产品契约依据：官方 VM 的模块目录、输入输出订阅和围绕当前数据端点添加兼容模块的操作习惯用于确定“完整能力常在、推荐与插入只按当前端口出现”的产品方向；62 工具数量、兼容过滤/补接策略、Circle2D 数据契约与径向符号约定均为本项目产品化实现，不冒充海康内部推荐系统、目录、图数据结构或几何类型。
S67 HALCON 依据：现有圆工具继续以 `fit_circle_contour_xld` 产生主圆；点圆径向测量根据同一类型化中心、半径和测量点确定最近圆周点及径向偏差，图形由受管圆、径向线、圆心/最近点/测量点十字组成。退化/非有限圆、圆心点、上下文不一致、输入所有权、陈旧周期和旧快照释放均纳入专项验证。
S68 最终门禁证据：左侧模块箱增至 63 个真实工具/9 类；选择未绑定或异常 IN 后才进入可退出的“补充上游来源”。存在合法前序 OUT 时优先列出可直接绑定项并保留顺序/启用/重复端点拒绝原因；无合法来源时目录只保留能产出目标类型的真实工具，在目标前插入、补齐可推断输入并连接目标。绑定和创建均写回真实 `InputBindings`、失效目标及下游、进入撤销/重做，返回全目录恢复分类/搜索/常用/收藏/最近，没有增加永久顶部按钮。直线圆工具输出 0/1/2 个类型化 Point2D、交点数、弦长、关系和托管 XLD并驱动 NumericJudge。Legacy 与 S09-S68 共 61/61；1938×1158 与 1366×768 实体 UI、XML 审计 914 个 XAML 事件引用/372 个唯一入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒响应通过；新 Git 批次 `3/10`，不提交或推送。
S68 产品契约依据：官方 VM 的模块目录、输入输出订阅、围绕当前端口补充上游和当前任务渐进展开习惯用于确定“已有来源优先、缺源才创建工具、完整目录可返回”的产品方向；63 工具数量、兼容/拒绝排序、插入补接策略、Line2D/Circle2D 数据契约均为本项目产品化实现，不冒充海康内部推荐系统、目录、图数据结构或几何类型。
S68 HALCON 依据：本机 HALCON 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.IntersectionLineCircle(HTuple lineRow1, lineColumn1, lineRow2, lineColumn2, circleRow, circleColumn, circleRadius, circleStartPhi, circleEndPhi, circlePointOrder, out HTuple row, out HTuple column)` 精确签名；最小直接调用确认无限直线与整圆相交、相切、相离分别返回 2/1/0 个坐标。实现按类型化直线投影稳定排序交点，以受管参考直线、圆和交点十字生成 XLD；退化/非有限、跨上下文、陈旧周期、0 交点旧 Point 清理和旧 XLD 释放均纳入专项验证。
S69 最终门禁证据：左侧模块箱增至 64 个真实工具/9 类；流程“更多”内按需连接问题清单扫描全部启用工具的必需 IN，区分阻断与等待本周期值，显示目标/类型/来源/原因并可前后导航、定位、打开 I/O 和进入 S68 补源，扫描本身不改线，顶部仍为 7 个永久高频入口。圆圆关系/交点工具订阅两路不同、同一 ImagePixel 的 Circle2D，输出 CenterDistance、0/1/2 个有限 Point2D、PointA/B、公共弦长、七类关系、重合标志、上下文、参数、ResultCode 和托管 XLD并驱动 NumericJudge。Legacy 与 S09-S69 共 62/62；1938×1158 连接任务清单与 1366×768 圆圆工作台实体 WPF UI、353 个唯一 XAML 事件入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒 `Responding=True` 通过；Git 新批次 `4/10`，不提交或推送。
S69 产品契约依据：官方 VM 的模块目录、输入输出订阅、当前任务和流程调试习惯用于确定“整条流程问题按需列出、逐项定位、用户明确选择后才补源”的产品方向；64 工具数量、阻断/等待分类、排序和交接策略均为本项目产品化实现，不冒充海康内部诊断器、AI 自动修复或调度器。首版仍是确定性线性流程，只读扫描不是自由 Graph、并行或执行型 Group。
S69 HALCON 依据：本机 HALCON 20.11 `halcondotnet.dll` 反射确认 `HOperatorSet.IntersectionCircles(HTuple circle1Row, circle1Column, circle1Radius, circle1StartPhi, circle1EndPhi, circle1PointOrder, circle2Row, circle2Column, circle2Radius, circle2StartPhi, circle2EndPhi, circle2PointOrder, out HTuple row, out HTuple column, out HTuple isOverlapping)` 精确签名；最小直接调用确认相交、外切、内切、分离及重合语义。HALCON 对重合整圆会令 `isOverlapping=1` 并返回弧端点，实现明确把它归类为 Overlapping、有限交点数为 0，避免冒充两个唯一几何交点；交点按 Row/Column 稳定排序并去重，两圆、圆心辅助和交点十字由托管 XLD 输出。
S70 最终门禁证据：左侧模块箱增至 65 个真实工具/9 类；流程“更多”内按需片段库可保存连续多工具链，显示顺序、内部连接、必需/可选外部输入和更新时间，支持改名、内联删除确认及锚点前后插入。插入生成新 ToolId、重映射内部 InputBindings、只保留合法外部来源并以一次撤销/重做回退；片段原子保存/备份、损坏不覆盖及运行态隔离通过，顶部仍为 7 个永久入口。两点构线/中点工具订阅两路不同、同一 ImagePixel 的 Point2D，真实调用 HALCON 20.11 `distance_pp` 与 XLD 生成算子，输出 MainLine、MidPoint、Distance/Angle/DeltaRow/DeltaColumn、上下文、参数、ResultCode 和 4 对象托管 XLD，驱动点线距离和 NumericJudge。Legacy 与 S09-S70 共 63/63；1938×1158 片段库与 1366×768 两点构线工作台实体 UI、378 个唯一 XAML 事件入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒 `Responding=True` 通过；Git 新批次 `5/10`，不提交或推送。
S70 产品契约依据：官方 VM 的模块目录、输入输出订阅、当前流程位置和工程复用习惯用于确定“完整工具常在、多工具模板按需管理、插入后仍是可见确定性工具序列”的方向；65 工具数量、片段文件格式、外部自动补接和两点几何类型均为本项目产品化实现，不冒充海康内部模板格式、市场、AI 或执行型子流程。
S70 HALCON 依据：本机 HALCON 20.11 反射确认 `HOperatorSet.DistancePp(HTuple, HTuple, HTuple, HTuple, out HTuple)`、`GenContourPolygonXld` 和 `GenCrossContourXld` 签名，最小调用确认 (10,20) 到 (13,24) 距离为 5。实现以两点生成类型化 MainLine 和 MidPoint，拒绝同源、重合/近重合、非有限、跨上下文和陈旧来源，并验证旧 XLD 释放。
S71 最终门禁证据：用户将当前工作习惯进一步明确为“左侧只保留一个分类入口、模块更小、默认从上到下调试、连线可柔性绕行、每步日志和通讯进专用子页面”。活动 WPF 已按此把左侧可见模式收敛为单一 `分类`，66 工具/9 类和本机片段仍由同一位置发现；自动模块约 `300 x 74 px`，按确定性顺序纵向排列，真实连接从输出底部进入输入顶部并采用左右绕行；节点 `记录` 打开中央步骤日志/I/O/TCP 通讯页面。片段插入继续复用 S70 内核并把外部缺源交给 S68。类型化双 Line2D 工具真实调用 HALCON 20.11 `intersection_lines/angle_ll/distance_pl`，输出有限交点、角度、平行距、关系和托管 XLD并驱动点线距离与 NumericJudge。Legacy 与 S09-S71 共 64/64、三张实体 WPF UI、382 个唯一 XAML 入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒 `Responding=True` 通过；Git 当前 `6/10`，不提交或推送。
S71 产品契约依据：官方 VM 的分类模块目录、纵向流程阅读、当前对象任务和按需调试信息习惯用于确认“完整能力不等于永久按钮墙、主流程方向必须符合现场调试、步骤记录按当前对象展开”的方向。单一分类菜单、300/74 尺寸、绕线路由和步骤记录页面均为本项目根据用户使用反馈形成的产品化实现，不冒充海康内部控件、布局算法或日志格式。
S71 HALCON 依据：本机 HALCON 20.11 反射确认 `IntersectionLines`、`AngleLl`、`DistancePl`、`GenContourPolygonXld` 和 `GenCrossContourXld` 的 .NET 签名；最小调用确认相交点 `(Row=5, Column=5)`、平行无有限交点和重合 `overlap=1` 语义。实现用对称端点到线距离辅助近重合判定，拒绝同源、退化、非有限、跨上下文和陈旧来源，并验证无交点时旧 Point2D 清理及旧 XLD 释放。
S72 最终门禁证据：活动 WPF 继续使用单一 `分类` 入口、67 工具/9 类、约 `300 x 74 px` 纵向紧凑节点和节点 `记录` 子页面。自动连接统一经模块右侧外部通道柔性绕行；普通合法连接为低权重且默认无标签，明确选中连接或异常才展开文字，选中节点只强化直接上下游。上下模块/当前执行定位、Alt+Up/Down/Enter、自动执行跟随及用户主动平移后暂停均定位真实节点/执行指针，顶部永久入口仍为 7 个。类型化 Point2D[] 点集直线拟合真实调用 HALCON 20.11，输出 MainLine:Line2D、端点、角度、有效/拒绝点数、平均/最大残差、上下文和托管 XLD，并驱动双线关系、点线距离及 NumericJudge。Legacy 与 S09-S72 共 65/65、1938×1158 与 1366×768 两张实体 WPF UI、972 个 XAML 事件引用/411 个唯一入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒 `Responding=True` 通过；Git 当前 `7/10`，不提交或推送。
S72 产品契约依据：官方 VM 的分类模块目录、当前流程对象、输入输出订阅和逐步调试习惯用于确认“流程主序列优先、连接文字按需、执行焦点可跟随”的产品方向；右侧单一绕线通道、具体透明度/线宽、Alt 导航和跟随暂停均为本项目依据用户反馈形成的产品化实现，不冒充海康内部连接路由、控件或快捷键。
S72 HALCON 依据：本机 HALCON 20.11 已确认并专项执行 `GenContourPolygonXld`、`fit_line_contour_xld` 封装路径和 `DistancePl`。精确直线 `(1,0)->(9,4)` 得到角度约 `63.43495°`、最大残差约 `9e-18`；50 个精确内点加 1 个强离群点的 Tukey 场景保持相同主方向并以最大残差约 `156.972 px` 显式诊断。Reject/SkipInvalid、混合上下文、退化、陈旧周期、输入所有权和旧 Line2D/XLD 释放均纳入专项验证。
S73 最终门禁证据：活动 WPF 保持单一 `分类` 入口、68 工具/9 类、约 `300 x 74 px` 纵向节点和节点 `记录` 子页面。自动画布按视口/缩放响应，1938×1158 与 1366×768 实体图均为 `HorizontalScroll=0`；手动 GraphX/GraphY 和横向浏览兼容保持。流程内 24 px 状态导航按真实顺序区分选中、下一执行、断点、NG、异常和连接阻断并定位节点，顶部仍为 7 个入口。类型化 Point2D[] 点集圆拟合真实调用 HALCON 20.11，输出 MainCircle:Circle2D、CenterPoint、Radius、有效/拒绝点数、平均/最大残差、上下文和托管 XLD，并驱动点圆、线圆、圆圆及 NumericJudge。Legacy 与 S09-S73 共 66/66、983 个 XAML 事件引用/414 个唯一入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒 `Responding=True` 通过；Git 当前 `8/10`，不提交或推送。
S73 产品契约依据：官方 VM 的当前流程对象、逐步调试、按需信息和输入输出订阅习惯用于确认“纵向主序列优先、状态概览轻量、低频记录按步骤打开”的产品方向；24 px 导航、具体状态配色和响应式通道为本项目依据用户反馈形成的产品化实现，不冒充海康内部控件、路由或快捷键。S74 的任务聚焦/全部依赖切换同样属于产品化推导，必须保留完整真实连接查看能力。
S73 HALCON 依据：本机 HALCON 20.11 已反射并专项执行 `GenContourPolygonXld`、`FitCircleContourXld` 封装路径和 `DistancePp`。36 点标准圆得到 Center=(50,80)、Radius=30、MaxResidual≈`2.59e-6` 和 74 对象托管 XLD；ATukey 离群、Reject/SkipInvalid、混合上下文、共点、近共线、陈旧周期、输入所有权和旧 Circle2D/XLD 释放均纳入专项验证。
S74 最终门禁证据：活动 WPF 已去除窗口标题、顶部标识、关于菜单和图像提示中把 `VM / VisionMaster` 当作本软件品牌的文字，保留其信息架构和操作习惯作为内部参照。默认流程卡缩至约 `280 x 68 px`，顶部/底部有 10 px 小圆锚点，真实连接改为无箭头、无阴影的直线或最少直角折线；流程“更多”内的任务聚焦默认只显示所选直接依赖、当前执行、断点及错误/阻断，全部依赖可恢复完整真实连接且不改 `InputBindings`、执行顺序、配方或运行值。24 px 导航的视窗指示随滚动、窗口和缩放同步，可点击/拖动纵向定位。两张 1938×1158/1366×768 实体 WPF 图均为 `HorizontalScroll=0`，顶部永久入口仍为 7。模块箱增至 69 工具/9 类且仍只有一个可见分类入口。类型化 Point2D[] 椭圆拟合真实调用 HALCON 20.11，48 点椭圆得到 Center=(120,180)、Radius1/Radius2=65/28、MaxResidual≈`0.004467 px` 和 98 对象托管 XLD；输出 MainEllipse:Ellipse2D、CenterPoint、Phi、AxisRatio、点数、残差、上下文和 ResultCode 并驱动 NumericJudge。Legacy 与 S09-S74 共 67/67、998 个 XAML 事件引用/419 个唯一入口/0 缺失、Debug/Release 0 警告 0 错误及双版本独立启动 8 秒 `Responding=True` 通过；Git 当前 `9/10`，不提交或推送。
S74 产品契约依据：官方 VM 的当前流程对象、纵向调试、输入输出订阅和渐进展开习惯用于确认“当前任务优先、完整依赖可按需恢复、步骤记录独立打开”的方向；280×68 尺寸、上下聚合锚点、简洁折线路由、任务聚焦规则、导航视窗和 `HALCON VISION` 自有视觉标识均为本项目依据用户反馈形成的产品化实现，不冒充海康内部控件、品牌或布局算法。S75 必须把当前只用于辨识的上下小圆锚点升级为真实端口订阅入口，但不得用单一锚点掩盖类型和具体端口。
S74 HALCON 依据：本机 HALCON 20.11 已确认并专项执行 `GenContourPolygonXld`、`FitEllipseContourXld`、`GenEllipseContourXld`、`DistancePc` 与 `GetContourXld`。正式 `Ellipse2D` 支持克隆、确定显示、JSON 往返、有限值/半轴/同一 ImagePixel ContextId 校验；Reject/SkipInvalid、混合上下文、近共线、陈旧来源、非法参数、配方、复制重映射、监视、日志、NumericJudge 和旧 XLD 释放均已验证。
S75 产品契约依据：官方 VM 的输入输出订阅、当前对象配置和渐进展开习惯用于确认“连接入口可见、具体端口按需出现”的方向；顶部输入/底部输出 10 px 聚合锚点、就地精确端口浮层、单端口直拖、多端口禁止猜测、简洁无箭头实线和 `HALCON VISION` 自有标识均为本项目产品化实现，不冒充海康内部控件、品牌或连线路由。聚合锚点复用 S21 的真实端口连接、类型/顺序/启用/同源校验、失效下游、配方脏状态和撤销/重做，详细端口、消费者、I/O 和连接 Inspector 仍可按需查看。
S75 最终门禁证据：活动目录为 70 工具/9 类，左侧仍只有单一分类入口，顶部永久入口仍为 7；默认节点保持约 280×68，上下小圆点可真实选择端口、拖接、替换和清除，精确端口列表默认收起。两张 1938×1158/1366×768 实体 WPF 图证明默认首屏没有端口墙、新按钮墙或 VM/VisionMaster 品牌露出。Legacy 与 S09-S75 共 68/68，987 个当前审计事件引用/411 个唯一入口/0 缺失，Debug/Release x64 0 警告 0 错误，两个版本各独立启动 8 秒且 `Responding=True`；本轮统一 Git 提交推送。
S75 HALCON 依据：本机 HALCON 20.11 已直接确认并执行 `GenEllipseContourXld`、`DistancePc` 与 `GetContourXld`。`Point2D + Ellipse2D` 必须属于同一稳定 ImagePixel ContextId；最近点来自真实采样轮廓，距离来自 `distance_pc`，内外关系按 HALCON 行列坐标下椭圆归一化半径计算。旋转外点得到 Distance=20 px、SignedDistance=20 px、NormalizedRadius=1.333333 和 4 对象托管 XLD；内部、边界、中心、四象限、近圆、退化、混合上下文、陈旧来源、非法参数、配方、NumericJudge 和旧 XLD 清理已验证。

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
