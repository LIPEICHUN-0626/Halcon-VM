# HDevelop 高级检测示例

这个示例用于说明如何把 HDevelop 脚本作为可选增强模块接入当前软件。普通用户只需要使用主流程：

1. 打开图片。
2. 需要时点击 `转灰度` 或 `转彩色`。
3. 框选 ROI 并点击 `确认ROI`。
4. 点击 `创建模板`。
5. 点击 `执行匹配`。

## HDevelop 过程约定

软件默认调用过程名 `RunInspection`。建议过程输入当前图像和 ROI，输出检测结果、分数和说明文本。

示例伪代码：

```hdevelop
proc RunInspection(Image, RoiRegion : : : ResultCode, Score, Message)
    reduce_domain(Image, RoiRegion, ImageReduced)
    * 在这里加入阈值、边缘、测量或其他 HALCON 算子
    ResultCode := 'OK'
    Score := 1.0
    Message := 'HDevelop inspection passed'
endproc
```

## 推荐用途

- 模板匹配之外的二次复核。
- 边缘测量、尺寸测量、缺陷阈值检测。
- 批量验证前的算法试验。

主界面会保持 ROI 到模板匹配的极简路径，HDevelop 只作为高级入口存在。
