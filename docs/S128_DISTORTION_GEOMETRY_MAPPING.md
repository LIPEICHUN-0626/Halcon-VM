# S128 畸变坐标映射与 ROI 迁移

状态：A/B 代码已接入，部分实现（待集中验证）。S126/S127 同样待集中验证；未执行编译、回归、启动或鼠标验收，Git 已验证批次仍 6/10。

A 线：在原校正图像/ROI 任务内清楚区分原图与校正图坐标，给已选点、轮廓和 ROI 提供按需可见的“迁移到校正图”操作及前后叠加预览；不自动把旧 ROI 套到新图，不在默认首屏增加命令。

B 线：基于 S126 的同一相机参数版本和输入图像链，调用 HALCON 的真实径向畸变点/轮廓映射，将 Point2D、XLD 及可支持的 ROI 几何显式转换到校正 ImagePixel 坐标；输出类型、图像链、版本和生命周期必须进入原订阅/配方/撤销路径。对非可逆、越界、不同相机/图像链、尺寸不符或不支持的区域形状明示拒绝，不以只改 CoordinateContextId 冒充转换。

已写入实际代码：

- 原“镜头畸变校正”的“输入”页新增可选 Point2D 与 XLD 来源，显式连接即参与迁移；Image 仍为原图必需输入。运行用同一 `CamParamIn/CamParamOut` 调用 `change_radial_distortion_points` 与 `change_radial_distortion_contours_xld`，发布 Point、XLD、PointMarker 和 MappedObjectCount。输出坐标身份与校正 Image 相同，图层只在对应画面显示，类型化结果可继续订阅。
- 拒绝未运行/失效来源、跨原图链、World2D、混校正版本和越界点；每个迁移顶点逆向复核误差上限 0.05 px。XLD 上限 4096 对象/262144 点，失败清理本工具已发布输出。
- 参数小窗“迁移原图点 / 轮廓 / ROI”默认折叠。应用并运行校正后，选定带明确原图身份的 ROI，点击“预览所选 ROI”打开左原图/右校正图双画面对照，黄色预览跟随相应坐标。点击“保留为新 ROI”创建独立图层、保留检测/屏蔽角色，不自动绑定下游；新图层可在接收校正 Image 的下游 ROI 页选择，进入原配方和整体撤销历史。
- 支持点、矩形、旋转矩形、圆、椭圆和多边形；点保持点形状，面 ROI 边界按最多 2 px 间距采样，最多 2048 顶点，映射后变为可编辑多边形。曲线面是采样近似，不保证计量级面积误差。当前拒绝直线、圆环、有洞复合区域和来源未明确的旧 ROI，避免把弯曲线或内孔丢失后冒充正确转换。
- 预览只在编辑模块、源 ROI 几何、输入链、输出快照和内参版本均未变化时有效；参数/模块/方案切换、重新运行、删除或改动源 ROI 后不能保存旧预览。关闭参数窗释放预览对象。校正图坐标身份可在重新打开方案、上游尚未运行时由参数和绑定解析，保留版本化 ROI 的绑定路径。
- 修正 `RoiData` 的多边形区域创建：此前 `gen_region_polygon` 只生成不闭合、不填充的边线，现改为 `gen_region_polygon_filled`，使多边形检测/屏蔽使用填充面积。该行为修正及现有多边形配方需纳入集中验证。

主要代码：`MainWindow.S128.cs`、`Models/VmCameraGeometryS128.cs`、`MainWindow.S126.cs`、`MainWindow.S102.Coordinates.cs`、`MainWindow.xaml` 和原 ROI/图层/结果元数据。内参仍为单相机 `area_scan_division`，没有独立多相机或现场精度证据。

集中验证须覆盖：实际内参两种视野模式、边缘/不可逆映射、Point/XLD 上下游、六类 ROI、拒绝形状、屏蔽与填充面积、双视口来源、草稿与旧预览、重新求参/清空输出、配方重开/复制/撤销/释放。代码存在不等于上述操作已通过。

HALCON 依据：[像素点径向映射](https://www.mvtec.com/doc/halcon/2011/en/change_radial_distortion_points.html)、[轮廓径向映射](https://www.mvtec.com/doc/halcon/1911/en/change_radial_distortion_contours_xld.html)、[填充多边形](https://www.mvtec.com/doc/halcon/2011/en/gen_region_polygon_filled.html)。

下一完整主题：`S129_COLOR_SAMPLE_AND_HSV_INSPECTION.md`，扩展目前缺少的颜色选样/HSV 工业检测任务，不把本轮未验证状态升级为交付完成。
