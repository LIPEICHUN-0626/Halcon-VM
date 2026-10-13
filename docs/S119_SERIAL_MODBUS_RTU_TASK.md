# S119 串口与 Modbus RTU 工业设备任务

状态：A/B 代码已接入，部分实现（待集中验证）；S118 Modbus TCP 同样未集中验证。

A 线已写入：原“通讯”分类可直接添加 Modbus RTU 串口读写任务。双击统一参数窗的常用区仅保留 COM 口、功能、零起始 PDU 地址和读数量/写数值；波特率、站号、奇偶校验、停止位、超时及离线学习按需展开，当前 8E1/站号摘要不占额外表单。串口列表可刷新，也可手工填写；结果画像、步骤记录和诊断显示本次状态，不增加顶部常驻按钮。

B 线已写入：使用 `SerialPort` 单次独占打开/请求/释放 COM 口，复用功能码 01–06 的类型化语义但独立构造 RTU 帧；CRC16（低字节在前）、站号、功能码、长度、异常帧和写单点回显均作严格校验。读位最多 2000、读 16 位寄存器最多 125，输出首值、Number[]、位状态、异常码、耗时和诊断；写值可订阅上游 Number，缺失不回退本地。流程配方/复制/草稿/日志复用原契约；运行时仅本进程单个任务占用同名 COM 口，超时/停止后 `Dispose`。离线示例可生成、修改和解析 CRC 帧，不打开硬件。

边界：当前只支持 Windows COM、8 数据位、None/Even/Odd、1/2 停止位和一次请求一次响应；无广播站号 0、RS-485 RTS 自动方向控制、32/64 位寄存器组合、批量写、自动重连或多主站调度。发送前按波特率留至少约 3.5 字符静默间隔；Windows `SerialPort` 加调度计时不保证严格检测帧内 1.5 字符间隔，采用预期长度、总超时和 CRC 判断。当前流程叶节点调用是同步的，界面“停止”不能保证在单次阻塞串口请求期间响应；代码只在打开后/发送前及每段读取间检查停止标志，总超时是兜底，不能宣称即时中断。集中验证前不能宣称硬件已验收。

依据：[Modbus Serial Line Protocol and Implementation Guide V1.02](https://www.modbus.org/docs/Modbus_over_serial_line_V1_02.pdf)、[Microsoft SerialPort 文档](https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=netframework-4.8.1)。
