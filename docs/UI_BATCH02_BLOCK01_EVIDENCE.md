# 界面第二批 · 01 普通灰度阈值 ROI 与有效域

2026-10-13。复用原ImageThreshold执行、S86 ROI策略、S62位置修正、S89唯一草稿和S112来源/坐标，不新增查看器/ROI模型或算法替代。

## 实际改进与代码依据

- 实读原普通阈值不在SupportsRoi且Threshold直接作用灰度图，已有ROI页面/图层并不影响计算。现在启用原ROI契约/可选Region端口，沿原本地检测并集、屏蔽、继承/原策略，HALCON ReduceDomain与输入既有domain取交集，再真正Threshold/Connection/AreaCenter。保持原Region/Area/RegionCount/Threshold输出与NG空检测语义，不把面积摘要当算法输出。
- 原ROI图层列表藏在更多，常用选择需要额外展开。复用同一ItemsControl，前置到ROI页并可滚动；位置修正/逐ROI/批量管理仍收起。普通阈值常用页长说明改短范围摘要，技术含义放悬停。
- 原GetBoundRoiLayers会过滤缺失/跨来源/停用层，若无剩余层，可能按全图运行。普通阈值现在检查绑定身份、有效几何、图像坐标、停用与空交集，不自动回退全图或补旧输出；未知旧无来源ROI保留原兼容语义，没有把它冒充已标记来源。
- 缺失ID保留原编辑行的MissingRoiId，不在加载草稿时悄悄消失；F5/应用前拒绝仍被勾选的失效项，用户明确取消后再重选。已绑定的跨源行可取消勾选，仍不能跨源添加；原草稿点击处理现在会标记解除操作，不漏dirty。
- 原“返回图像区”会先自动ApplyDockConfigurationDraft并展开220px旧侧栏，与草稿/布局要求冲突。现在明确“在图像区画矩形”，只进入原矩形绘制任务，保留草稿/主布局；其他形状原菜单保持。确认几何仍沿原ROI生命周期，不宣称物理绘制通过。

## 最终证据

- 最终Debug/x64和Release/x64构建均exit 0。首轮编译局部lambda名称与旧C#作用域冲突已修；一次审批超时没有执行，按允许重试后成功，不把超时当通过。
- `tests/UIBatch01WorkspaceRender.ps1 -Phase ui02block01`在Debug、Release最终各exit 0，窗口离屏ShowActivated=false。原核心四页520×480、草稿100–200不应用、普通选中不切编辑对象和保存复用通过。
- 64×64灰度梯度真实HALCON结果：未绑定ROI的输入有效域Area1664；本地矩形5..15×25..35为121；同范围屏蔽10..15×30..35后85；输入自身已ReduceDomain至10..12×28..30时只有9，不把ROI扩大到域外。
- 原ROI行勾选只修改草稿，F5为121但正式绑定未改变；Ctrl+Enter原应用后正式绑定保留，明确运行121。原连通对象下游读取正式Region，TotalArea=121，不解析摘要造区域。
- 原绘制入口调用后dirty与未应用绑定保持、旧RoiLayerPanel列宽不变；原取消绘制入口退出。检测区列表在更多未展开时可见。首次追加验证误用了取消处理器旧名称，已改真实CancelRoiDrawingButton_Click，最终通过。
- 全部停用、跨来源、图像外ROI导致domain交集为空、缺失ID均通过原ExecuteFlowTool失败清空旧Area。缺失条目仍在草稿并可取消，ValidateDockRoiSourceDraft拒绝勾选失效行；明确修复应用后121恢复。
- 新阈值继承同链前序阈值Region真实Area121，扣本地屏蔽后85；上游Region失效时拒旧值。恢复121后原Schema3活动Tools/ROI保存，经原事务载入不自动执行；明确重跑源图和阈值仍121，之后源图缺失清旧值。
- 共享ROI页面必要依赖回归 `-Phase block05`最终exit 0：原Blob Area121、编辑模块归属、绘制坐标/尺寸/来源失效保护、双视口同坐标和保存仍通过。
- 最终阈值ROI页及缺失绑定图已查看，检测区直接可选、缺失警告/解绑可见、四页选中反馈正常，低频更多收起：`artifacts/ui-batch-02/ui02block01-threshold-roi-task.png`、`ui02block01-threshold-missing-roi.png`。它们是WPF离屏图，不是实体桌面/原生HALCON截图。
- 相关脚本解析零错误，差异检查exit 0。专项执行静态方法/原事件与真实SDK，不模拟物理鼠标/键盘或确认框。

## 支持边界与下一步

本地检测/屏蔽、已有domain、同链继承/屏蔽与失效/保存路径已由上述具体值证明。合并/旧交集策略和位置修正复用原代码，未由本块对所有来源/变换证明；全部灰度类型、RGB、高位图、所有ROI形状/实体绘制、重复身份/空输入domain全部分支仍不是全验证。普通阈值只合并执行，逐ROI参数明确拒绝；本块不把执行OK当产品合格或VM对等。

第二批1/10，暂不Git；第一批`61adc04`已上传，旧6/10不合计。下一02原Shape/NCC制作/匹配与输入/模型草稿应用/正式输出资源保存闭环，长期目标活动。
