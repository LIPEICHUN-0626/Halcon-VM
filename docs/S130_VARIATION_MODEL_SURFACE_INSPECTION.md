# S130 良品学习与表面缺陷检测

状态：2026-10-02 代码已接入，部分实现（待集中验证）。S111–S130 代码待集中验证；未进行编译、启动、回归、渲染或鼠标验收。

A 线已接入：原检测分类新增“良品变化缺陷检测”。统一参数小窗常用仅最小灰差、波动倍数、最小缺陷面积三项；良品样本/训练默认折叠。可保存模块实际输入良品和有效域，或确认同坐标对齐后多选外部良品复制到受管目录；队列显示可读名称、缩略图、逐张状态/失败原因，后台训练可取消。沿用输入/ROI/结果、草稿/F5/应用和原图/掩膜视口，不增加永久顶部按钮、不压窄流程。全窗口不冻结，修改样本/切换/停止会取消并拒绝旧结果。

B 线已接入：真实 HALCON create_variation_model(standard)/train_variation_model/prepare_variation_model/compare_variation_model；全部样本重建一份独立版本，学习位置相关的良品灰度均值/波动。保存前清理增量训练数据，原统计图和阈值仍用于检测时按当前容差重新准备。连接 Image，可选 ROI/屏蔽复用原运行域；共同有效域随 `.reg` 保存，检测域须完全被所有样本覆盖，拒绝静默缩小 ROI。输出托管 Region、筛选后 Area/AreaRatio(%)/RegionCount、筛选前 RawArea、学习 SampleCount、EffectivePixels、HasDefect、ModelVersion/Parameters/ResultCode；缺陷图层与下游区域筛选/轮廓/数值判定沿用原合同。空缺陷为有效执行 OK，不等于产品判定合格。

资源/草稿：模型 `.vam`、覆盖域 `.reg` 和样本写入原方案目录下 VariationModels，使用随机不可变版本与 SHA-256。新模型只进入同一草稿，应用/保存/撤销依靠原 DTO；失败不修改已应用模型。每次检测独立读取/准备/清理句柄，不与草稿或其他实例共享可变模型。读资源持有只读文件锁并核对哈希；加载支持相对路径，打包导出自动复制所有模型/覆盖域/样本。路径变化不改变数据集键；来源、相机接口/标识、上游参数与 ROI 几何变化有签名守卫。明确复制了来源的根流程/流程块只在原数据集仍有效时重映射身份；未解析的嵌套来源不自动重绑定。旧文件保留供撤销、备份与恢复引用，尚无引用感知清理。

首版边界：3–64 张良品，单张最多 16777216 像素，总样本最多 268435456 像素，单资源文件最多 512 MB；单通道 byte/int2/uint2、同尺寸、同来源且已对齐，RGB 须上游显式转灰度。外部良品责任确认不是自动证明对齐；没有配准不宣称吸收平移旋转或光照漂移。取消在当前 HALCON 算子后生效；检测范围不受当前模块未应用 ROI 偷改。仅 standard、亮暗合并；稳健/直接模式、均值/波动图交互和自动图像对齐仍未实现。

学习：顶部示例程序→检测·良品学习与表面缺陷，显式装载 Image→检测→轮廓，检测 Area 同时接数值判定。尝试读取本机 `cap_illumination/cap_illumination_01.png`，缺图须自选；不预训练、不自动运行、不把单图重复成 3 张良品。用户必须自行采样/加入至少 3 张对齐良品。中文帮助含步骤和资源迁移边界。

实现：`MainWindow.S130.cs`、`Models/VmVariationInspectionS130.cs`，及原工具元数据、XAML、草稿/执行器、常用与结果画像、复制、项目包含、RecipeService 打包/相对路径、示例和帮助。直接读取本机 HALCON 20.11 API，并参考 [创建模型](https://www.mvtec.com/doc/halcon/2011/en/create_variation_model.html)、[训练](https://www.mvtec.com/doc/halcon/2011/en/train_variation_model.html)、[准备阈值](https://www.mvtec.com/doc/halcon/2011/en/prepare_variation_model.html)、[比较](https://www.mvtec.com/doc/halcon/2011/en/compare_variation_model.html)、[保存模型](https://www.mvtec.com/doc/halcon/2011/en/write_variation_model.html)；未运行算法测试。

主要风险：样本覆盖不足、错位被学成正常、光照漂移、ROI/模型版本失配、训练耗时与资源丢失。实现只负责可理解的来源/训练/异常边界，不宣称现场精度。集中阶段再验证训练统计、阈值语义、掩膜/坐标、空缺陷、失效资源、取消/切换、复制保存重开与完整操作学习路径。

连续开发到约 S150，按完整功能块不拆零碎编号；上述是代码接入，不是验证完成声明。不逐轮编译/回归/启动/渲染/鼠标验收，文件即时保存，Git 已验证批次仍 6/10。下一 S131 NCC 灰度模板匹配，见 `S131_NCC_TEMPLATE_MATCHING.md`（计划）；后续工程生产与集中收敛分组沿用 `S150_PRODUCT_TARGET.md`。
