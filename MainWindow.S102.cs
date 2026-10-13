using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly Stack<List<VmToolInstance>> subflowRuntimeScopesS102 = new Stack<List<VmToolInstance>>();
        private bool subflowTrialActiveS102;
        private int subflowWorkCountS111;
        private sealed class SubflowNgStopS102 : Exception
        {
            public SubflowNgStopS102(string message) : base(message) { }
        }

        private VmToolInstance ResolveRuntimeSourceS102(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var scope in subflowRuntimeScopesS102)
            {
                var found = scope.FirstOrDefault(tool => string.Equals(tool.ToolId, id, StringComparison.OrdinalIgnoreCase));
                if (found != null) return found;
            }
            var local = flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, id, StringComparison.OrdinalIgnoreCase));
            if (local != null) return local;
            foreach (var frame in subflowEditorsS102)
            {
                var parent = frame.ParentTools.FirstOrDefault(tool => string.Equals(tool.ToolId, id, StringComparison.OrdinalIgnoreCase));
                if (parent != null) return parent;
            }
            return null;
        }

        private IList<VmToolInstance> GetRuntimeScopeS102(VmToolInstance target)
        {
            foreach (var scope in subflowRuntimeScopesS102) if (scope.Contains(target)) return scope;
            return flowTools;
        }

        private static object CopySubflowValueS102(object value, VmToolInstance owner, string portName)
        {
            if (value is VmNumberArrayS105 || value is VmStringArrayS105) return VmArrayContractS105.Clone(value);
            if (VmFlowValueContractS103.GetGeometryType(value) != null) return VmFlowValueContractS103.CloneGeometry(value);
            var image = value as VmImageSnapshot;
            if (image != null)
            {
                using (HImage copy = image.CreateImageCopy())
                    return VmImageSnapshot.Create(copy, owner.ToolId, owner.InstanceName, portName, image.SourcePath, image.SerialNumber);
            }
            var region = value as VmRegionSnapshot;
            if (region != null)
            {
                using (HRegion copy = region.CreateRegionCopy()) return VmRegionSnapshot.Create(copy, owner.ToolId, owner.InstanceName, portName);
            }
            var xld = value as VmXldSnapshot;
            if (xld != null)
            {
                using (HObject copy = xld.CreateXldCopy()) return VmXldSnapshot.Create(copy, owner.ToolId, owner.InstanceName, portName);
            }
            if (value is string || value is bool || value is double || value is float || value is decimal || value is int || value is long || value is short || value is byte || value is uint || value is ulong || value is ushort || value is sbyte) return value;
            throw new InvalidOperationException("当前子流程边界尚不支持此结果类型；不传递未管理的可变对象。");
        }

        private static void RequireSubflowValueTypeS102(string type, object value)
        {
            if (VmArrayContractS105.IsType(type)) { VmArrayContractS105.RequireType(type, value); return; }
            if (VmFlowValueContractS103.IsGeometryType(type)) { VmFlowValueContractS103.RequireGeometry(type, value); return; }
            bool valid = type == "Image" ? value is VmImageSnapshot : type == "Region" ? value is VmRegionSnapshot : type == "XLD" ? value is VmXldSnapshot :
                type == "Boolean" ? value is bool : type == "String" || type == "ResultCode" ? value is string :
                type == "Number" && (value is double || value is float || value is decimal || value is int || value is long || value is short || value is byte || value is uint || value is ulong || value is ushort || value is sbyte);
            if (!valid) throw new InvalidOperationException("子流程边界结果与声明类型 " + type + " 不一致。");
            if (type == "Number")
            {
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidOperationException("子流程数值必须为有限值。");
            }
        }

        private async Task<InspectionRecord> ExecuteScheduledToolS102(VmToolInstance tool, string source, VmFlowRunPolicy policy, Stopwatch flowClock)
        {
            if (IsManagedCommunicationS120(tool)) return await ExecuteCommunicationLeafWithTraceS120(tool, source, policy, flowClock);
            if (tool.Kind != VmToolKind.SubflowCall) return ExecuteSubflowLeafWithTraceS102(tool, source);
            var watch = Stopwatch.StartNew();
            InvalidateDownstreamResults(tool, "子流程重新调用");
            tool.ClearRuntimeOutputs(); tool.RunStatus = "调用中"; tool.ResultCode = "--"; tool.ErrorMessage = string.Empty;
            tool.OutputSummary = "等待本次返回";
            var scope = new List<VmToolInstance>();
            var lastValues = new Dictionary<string, object>(StringComparer.Ordinal);
            var lastContexts = new Dictionary<string, string>(StringComparer.Ordinal);
            int completedIterations = 0, plannedIterations = 0;
            if (subflowRuntimeScopesS102.Count == 0) subflowWorkCountS111 = 0;
            bool pushed = false;
            var traceFrame = BeginSubflowCallTraceS102(tool);
            try
            {
                string skip = GetExecutionSkipReasonS100(tool);
                if (skip != null) return CreateConditionSkipS100(tool, skip);
                var definition = (tool.Parameters ?? new VmToolParameterData()).Subflow;
                string error = definition == null ? "子流程内容缺失。" : definition.Validate();
                if (error != null) throw new InvalidOperationException(error);
                if (subflowRuntimeScopesS102.Count >= 8) throw new InvalidOperationException("子流程调用深度超过 8 层。");
                var proxy = new VmToolInstance { ToolId = VmSubflowData.InputProxyId, Kind = VmToolKind.SubflowCall, InstanceName = tool.InstanceName + " · 输入", ResultCode = "OK" };
                proxy.Parameters.Subflow = new VmSubflowData { Outputs = VmSubflowLoopS111.ProxyPorts(definition) };
                scope.Add(proxy);
                foreach (var port in definition.Inputs)
                {
                    var binding = tool.GetInputBinding(port.PortName);
                    if (binding == null) throw new InvalidOperationException("子流程输入未连接：" + port.DisplayName);
                    string bindingError = GetInputBindingConfigurationError(tool, binding);
                    if (!string.IsNullOrWhiteSpace(bindingError)) throw new InvalidOperationException(port.DisplayName + "：" + bindingError);
                    var producer = GetInputSourceTool(binding); object raw;
                    if (producer == null || (producer.ResultCode != "OK" && producer.ResultCode != "NG") || !string.IsNullOrWhiteSpace(producer.ErrorMessage) ||
                        !producer.TryGetOutputValue(binding.SourcePortName, out raw) || raw == null || IsDisposedRuntimeValueS69(raw))
                        throw new InvalidOperationException(port.DisplayName + " 没有有效上游结果。");
                    RequireSubflowValueTypeS102(port.DataType, raw);
                    proxy.SetOutputValue(port.PortName, CopySubflowValueS102(raw, proxy, port.PortName));
                    if (port.DataType == "Image" || port.DataType == "Region" || port.DataType == "XLD")
                        proxy.SetOutputCoordinateContext(port.PortName, ResolveOutputCoordinateContextS102(producer, binding.SourcePortName));
                }
                var runtimeIds = definition.Tools.ToDictionary(item => item.ToolId, item => Guid.NewGuid().ToString("N"), StringComparer.OrdinalIgnoreCase);
                foreach (var item in definition.Tools)
                {
                    VmToolKind kind;
                    if (!Enum.TryParse(item.ToolType, out kind)) throw new InvalidOperationException("子流程工具类型无效。");
                    var runtimeItem = VmSubflowData.CloneTool(item);
                    runtimeItem.ToolId = runtimeIds[item.ToolId];
                    foreach (var binding in runtimeItem.InputBindings)
                    {
                        string mapped;
                        if (runtimeIds.TryGetValue(binding.SourceToolId ?? string.Empty, out mapped)) binding.SourceToolId = mapped;
                    }
                    string numericSource;
                    if (runtimeItem.NumericJudge != null && runtimeIds.TryGetValue(runtimeItem.NumericJudge.InputToolId ?? string.Empty, out numericSource)) runtimeItem.NumericJudge.InputToolId = numericSource;
                    var child = CreateFlowTool(kind, item.InstanceName, item.IsEnabled, runtimeItem.ToolId);
                    child.CoordinateIdentityBase = GetCoordinateIdentityBaseS102(tool) + "/" + item.ToolId;
                    subflowDefinitionPathsS102[child.ToolId] = traceFrame.Row.DefinitionPath.Concat(new[] { item.ToolId }).ToList();
                    scope.Add(child); ApplyToolRecipeData(child, runtimeItem);
                }
                subflowRuntimeScopesS102.Push(scope); pushed = true;
                tool.InputSummary = definition.Inputs.Count + " 项输入已传入独立调用作用域";
                bool hasNg = false; int count = 0;
                plannedIterations = VmSubflowLoopS111.Iterations(definition, proxy);
                var collected = definition.Outputs.Where(port => port.CollectIterations).ToDictionary(port => port.PortName, port => new List<object>());
                var collectedTextLength = collected.Keys.ToDictionary(key => key, key => 0L);
                for (int iteration = 0; iteration < plannedIterations; iteration++)
                {
                ThrowIfSubflowStoppedS102(policy, flowClock);
                VmSubflowLoopS111.SetContext(definition, proxy, iteration, plannedIterations);
                traceFrame.Iteration = VmSubflowLoopS111.Mode(definition) == VmSubflowLoopS111.Once ? null : (iteration + 1) + "/" + plannedIterations;
                foreach (var reset in scope.Skip(1))
                {
                    reset.ClearRuntimeOutputs(); reset.ResultCode = "--"; reset.ErrorMessage = string.Empty; reset.RunStatus = "等待本项"; reset.OutputSummary = "等待本项执行";
                }
                tool.RunStatus = "执行第 " + (iteration + 1) + "/" + plannedIterations + " 项";
                tool.InputSummary = definition.Inputs.Count + " 项固定上下文 · 当前索引 " + iteration + " · 总数 " + plannedIterations;
                foreach (var child in scope.Skip(1).Where(item => item.IsEnabled))
                {
                    var pendingTrace = AddSubflowTraceRowS102(child, true);
                    traceFrame.Next = child.InstanceName;
                    traceFrame.Current = child.InstanceName;
                    RefreshSubflowTraceUiS102();
                    FlowCurrentToolText.Text = tool.InstanceName + " [" + (iteration + 1) + "/" + plannedIterations + "] / " + child.InstanceName;
                    HeaderStatusText.Text = "子流程调用：" + FlowCurrentToolText.Text;
                    if (child.IsBreakpoint && !debugSingleStepRequested)
                    {
                        isPauseRequested = true; isDebugBreakpointPause = true; debugPauseReason = "子流程断点：" + child.InstanceName;
                        FlowRunStatusText.Text = debugPauseReason; RefreshUiState();
                    }
                    if (isPauseRequested) { pendingTrace.State = "已暂停"; RefreshSubflowTraceUiS102(); }
                    while (isPauseRequested && !isStopRequested) { ThrowIfSubflowStoppedS102(policy, flowClock); await Task.Delay(50); }
                    ThrowIfSubflowStoppedS102(policy, flowClock);
                    if (++subflowWorkCountS111 > VmSubflowLoopS111.MaxWork) throw new InvalidOperationException("单次根子流程调用累计超过 10000 步；已中断，不发布部分返回值。");
                    bool stepping = debugSingleStepRequested;
                    if (stepping) debugSingleStepRequested = false;
                    var childRecord = await ExecuteScheduledToolS102(child, source + " / " + tool.InstanceName, policy, flowClock);
                    traceFrame.Current = child.InstanceName + " · " + childRecord.ResultCode;
                    var nextChild = scope.Skip(1).Where(item => item.IsEnabled).SkipWhile(item => !ReferenceEquals(item, child)).Skip(1).FirstOrDefault();
                    traceFrame.Next = nextChild == null
                        ? (iteration + 1 < plannedIterations ? "下一项 " + (iteration + 2) + "/" + plannedIterations : "返回上一级")
                        : nextChild.InstanceName;
                    RefreshSubflowTraceUiS102();
                    count++; hasNg |= childRecord.ResultCode == "NG";
                    if (!string.IsNullOrWhiteSpace(child.ErrorMessage)) throw new InvalidOperationException(child.InstanceName + "：" + child.ErrorMessage);
                    if (policy.StopOnNg && childRecord.ResultCode == "NG") throw new SubflowNgStopS102("子流程遇 NG 停止：" + child.InstanceName);
                    if (stepping)
                    {
                        isPauseRequested = true; isDebugBreakpointPause = true; debugPauseReason = "子流程单步完成：" + child.InstanceName;
                        FlowRunStatusText.Text = debugPauseReason; RefreshUiState();
                    }
                    await Dispatcher.Yield(DispatcherPriority.Background);
                    ThrowIfSubflowStoppedS102(policy, flowClock);
                }
                foreach (var port in definition.Outputs)
                {
                    string producerId = runtimeIds[port.SourceToolId];
                    var producer = scope.FirstOrDefault(item => item.ToolId == producerId); object raw;
                    if (producer == null || (producer.ResultCode != "OK" && producer.ResultCode != "NG") || !string.IsNullOrWhiteSpace(producer.ErrorMessage) ||
                        !producer.TryGetOutputValue(port.SourcePortName, out raw) || raw == null || IsDisposedRuntimeValueS69(raw))
                        throw new InvalidOperationException("子流程返回值不可用：" + port.DisplayName + "；不会返回跳过分支或上一周期旧值。");
                    var sourcePort = ToolMetadata.GetInstanceOutputPorts(producer).FirstOrDefault(item => item.PortName == port.SourcePortName);
                    if (sourcePort == null || sourcePort.DataType != port.DataType) throw new InvalidOperationException("子流程返回端口类型已变化：" + port.DisplayName);
                    RequireSubflowValueTypeS102(port.DataType, raw);
                    object previous;
                    if (lastValues.TryGetValue(port.PortName, out previous) && previous is IDisposable) ((IDisposable)previous).Dispose();
                    lastValues[port.PortName] = CopySubflowValueS102(raw, tool, port.PortName);
                    if (port.DataType == "Image" || port.DataType == "Region" || port.DataType == "XLD")
                        lastContexts[port.PortName] = ResolveOutputCoordinateContextS102(producer, port.SourcePortName);
                    if (port.CollectIterations)
                    {
                        if (port.DataType == "Number") collected[port.PortName].Add(Convert.ToDouble(raw, CultureInfo.InvariantCulture));
                        else
                        {
                            collectedTextLength[port.PortName] += ((string)raw).Length;
                            if (collectedTextLength[port.PortName] > VmArrayContractS105.MaxTextLength) throw new InvalidOperationException(port.DisplayName + " 收集文本超过 262144 字符，未发布部分结果。");
                            collected[port.PortName].Add((string)raw);
                        }
                    }
                }
                completedIterations++;
                }
                ThrowIfSubflowStoppedS102(policy, flowClock);
                foreach (var port in definition.Outputs.Where(port => port.CollectIterations))
                    lastValues.Add(VmSubflowLoopS111.CollectionPort(port), port.DataType == "Number"
                        ? (object)new VmNumberArrayS105(collected[port.PortName].Cast<double>()) : new VmStringArrayS105(collected[port.PortName].Cast<string>()));
                foreach (var value in lastValues) tool.SetOutputValue(value.Key, value.Value);
                lastValues.Clear();
                foreach (var context in lastContexts) tool.SetOutputCoordinateContext(context.Key, context.Value);
                tool.SetOutputValue(VmSubflowLoopS111.Completed, (double)completedIterations);
                tool.SetOutputValue(VmSubflowLoopS111.Planned, (double)plannedIterations);
                tool.SetOutputValue(VmSubflowLoopS111.HasLast, completedIterations > 0);
                tool.SetOutputValue("ExecutedCount", (double)count);
                string code = hasNg ? "NG" : "OK";
                string message = "子流程返回 · 完成 " + completedIterations + "/" + plannedIterations + " 项 · " + count + " 步" +
                    (completedIterations == 0 ? " · 零项未执行；仅发布空收集数组，无最后一次值" : " · 普通返回为最后一次，全部项见收集数组");
                tool.SetOutputValue("ResultCode", code); tool.ResultCode = code; tool.RunStatus = "已返回"; tool.OutputSummary = message;
                var record = CreateRecord("SubflowCall", code, count, message, tool); resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
                return record;
            }
            catch (SubflowNgStopS102 ex)
            {
                tool.ClearRuntimeOutputs(); tool.ResultCode = "NG"; tool.ErrorMessage = string.Empty; tool.RunStatus = "遇 NG 停止";
                tool.OutputSummary = "已完成 " + completedIterations + "/" + plannedIterations + " 项；未发布返回值：" + ex.Message + "；已发生副作用不回滚。";
                LogInfo(tool.InstanceName + "：" + ex.Message); throw;
            }
            catch (Exception ex)
            {
                tool.ClearRuntimeOutputs(); tool.ResultCode = ex is OperationCanceledException ? "STOP" : ex is TimeoutException ? "TIMEOUT" : "NG";
                tool.ErrorMessage = ex.Message; tool.RunStatus = "调用中断";
                tool.OutputSummary = "已完成 " + completedIterations + "/" + plannedIterations + " 项；未发布返回值：" + ex.Message + "；已发生副作用不回滚。";
                LogInfo(tool.InstanceName + "：" + ex.Message);
                throw;
            }
            finally
            {
                foreach (var value in lastValues.Values.OfType<IDisposable>()) value.Dispose();
                if (pushed) subflowRuntimeScopesS102.Pop();
                tool.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
                CompleteSubflowTraceRowS102(traceFrame.Row, tool);
                subflowTraceFramesS102.Pop();
                foreach (var child in scope)
                {
                    var pending = subflowTraceRowsS102.LastOrDefault(row => row.RuntimeToolId == child.ToolId && (row.State == "待执行" || row.State == "已暂停"));
                    if (pending != null) { pending.State = "未执行"; pending.Details = "调用在执行此步骤前停止或中断，未发布输出。"; }
                    subflowDefinitionPathsS102.Remove(child.ToolId);
                    DisposeShapeToolResource(child.ToolId); child.Dispose();
                }
                RefreshSubflowTraceUiS102();
                RefreshResultOverlayLayers(); UpdateImageContextViewport(true); ScheduleRefreshDisplay();
            }
        }

        private void ThrowIfSubflowStoppedS102(VmFlowRunPolicy policy, Stopwatch clock)
        {
            if (isStopRequested) throw new OperationCanceledException("用户停止子流程调用。");
            if (policy.FlowTimeoutMilliseconds > 0 && clock.ElapsedMilliseconds >= policy.FlowTimeoutMilliseconds)
                throw new TimeoutException("流程超时，子流程未发布返回值。");
        }

        private void CreateSubflowS102_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("创建子流程");
            if (editLeaseS137 < 0) return;
            RunUiAction("创建子流程", delegate
            {
                if (!CanNavigateInputRepairS96()) return;
                var selected = GetSelectedFlowToolsInSequence();
                if (selected.Count < 2) { HeaderStatusText.Text = "请连续多选至少两个模块，再创建子流程。"; return; }
                int start = flowTools.IndexOf(selected[0]);
                if (!flowTools.Skip(start).Take(selected.Count).SequenceEqual(selected))
                    throw new InvalidOperationException("请选择连续执行的一段模块，避免封装后改变其他工具的执行顺序。");
                if (!CloseCentralModuleConfigurationS77()) return;
                var all = CaptureFlowRecipe();
                var ids = new HashSet<string>(selected.Select(tool => tool.ToolId), StringComparer.OrdinalIgnoreCase);
                var definition = new VmSubflowData { Tools = all.Where(item => ids.Contains(item.ToolId)).Select(VmSubflowData.CloneTool).ToList() };
                var callBindings = new List<VmToolInputBindingData>();
                var boundaryKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in definition.Tools)
                {
                    var original = selected.First(tool => tool.ToolId == item.ToolId);
                    if (item.NumericJudge != null && !item.InputBindings.Any(binding => binding.TargetPortName == "Value") && !string.IsNullOrWhiteSpace(item.NumericJudge.InputToolId))
                        item.InputBindings.Add(new VmToolInputBindingData { TargetPortName = "Value", SourceToolId = item.NumericJudge.InputToolId, SourcePortName = item.NumericJudge.InputPortName });
                    foreach (var binding in item.InputBindings.Where(binding => !ids.Contains(binding.SourceToolId ?? string.Empty)))
                    {
                        string error = GetInputBindingConfigurationError(original, binding);
                        if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(original.InstanceName + "：" + error);
                        var port = ToolMetadata.GetInstanceInputPorts(original).First(portDefinition => portDefinition.PortName == binding.TargetPortName);
                        if (!VmSubflowData.IsBoundaryTypeSupported(port.DataType)) throw new InvalidOperationException("当前子流程边界尚不支持 " + port.DataType + "；请保留这些模块在主流程中，后续扩展类型契约。");
                        string key = binding.SourceToolId + "|" + binding.SourcePortName;
                        string inputName;
                        if (!boundaryKeys.TryGetValue(key, out inputName))
                        {
                            inputName = "In" + (definition.Inputs.Count + 1).ToString(CultureInfo.InvariantCulture);
                            boundaryKeys.Add(key, inputName);
                            definition.Inputs.Add(new VmSubflowPortData { PortName = inputName, DisplayName = original.InstanceName + " · " + port.DisplayName, DataType = port.DataType });
                            callBindings.Add(new VmToolInputBindingData { TargetPortName = inputName, SourceToolId = binding.SourceToolId, SourcePortName = binding.SourcePortName });
                        }
                        binding.SourceToolId = VmSubflowData.InputProxyId; binding.SourcePortName = inputName;
                    }
                    if (item.NumericJudge != null)
                    {
                        var value = item.InputBindings.FirstOrDefault(binding => binding.TargetPortName == "Value");
                        if (value != null) { item.NumericJudge.InputToolId = value.SourceToolId; item.NumericJudge.InputPortName = value.SourcePortName; }
                    }
                    item.GroupId = null; item.GroupName = null; item.IsGroupCollapsed = false;
                }
                var outside = all.Where(item => !ids.Contains(item.ToolId)).ToList();
                foreach (var item in outside.Where(item => item.NumericJudge != null && !item.InputBindings.Any(binding => binding.TargetPortName == "Value") && ids.Contains(item.NumericJudge.InputToolId ?? string.Empty)))
                    item.InputBindings.Add(new VmToolInputBindingData { TargetPortName = "Value", SourceToolId = item.NumericJudge.InputToolId, SourcePortName = item.NumericJudge.InputPortName });
                var outputKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var producer in selected)
                {
                    foreach (var port in ToolMetadata.GetInstanceOutputPorts(producer))
                    {
                        bool consumed = outside.Any(item => item.InputBindings.Any(binding => binding.SourceToolId == producer.ToolId && binding.SourcePortName == port.PortName));
                        if (!consumed && (!ReferenceEquals(producer, selected.Last()) || port.IsOptional)) continue;
                        if (!VmSubflowData.IsBoundaryTypeSupported(port.DataType))
                        { if (consumed) throw new InvalidOperationException("下游使用了尚未支持的子流程返回类型：" + port.DataType); else continue; }
                        string outputName = "Out" + (definition.Outputs.Count + 1).ToString(CultureInfo.InvariantCulture);
                        definition.Outputs.Add(new VmSubflowPortData { PortName = outputName, DisplayName = producer.InstanceName + " · " + port.DisplayName, DataType = port.DataType, SourceToolId = producer.ToolId, SourcePortName = port.PortName });
                        outputKeys.Add(producer.ToolId + "|" + port.PortName, outputName);
                    }
                }
                string definitionError = definition.Validate();
                if (definitionError != null) throw new InvalidOperationException(definitionError);
                StackPanel content; Button apply;
                var window = CreateBatchEditWindowS99("创建子流程 · 边界预览", out content, out apply);
                var name = new TextBox { Text = "子流程 · " + selected[0].InstanceName, Margin = new Thickness(0,0,0,10), MaxLength = 64 };
                content.Children.Add(name);
                content.Children.Add(new TextBlock { Text = selected.Count + " 个连续模块将收为一个真实调用节点。参数和内部连接保留；外部连接自动经过以下边界。", TextWrapping = TextWrapping.Wrap });
                foreach (var item in definition.Tools) content.Children.Add(new TextBlock { Text = "  " + item.InstanceName, Margin = new Thickness(0,3,0,0) });
                foreach (var port in definition.Inputs) content.Children.Add(new TextBlock { Text = "输入  " + port.DisplayName, ToolTip = port.DataType });
                foreach (var port in definition.Outputs) content.Children.Add(new TextBlock { Text = "返回  " + port.DisplayName, ToolTip = port.DataType });
                apply.Content = "创建子流程";
                apply.Click += delegate
                {
                    if (string.IsNullOrWhiteSpace(name.Text) || !CanNavigateInputRepairS96()) return;
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    if (flowTools.Any(tool => !selected.Contains(tool) && string.Equals(tool.InstanceName, name.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                    { MessageBox.Show(window, "模块名称已存在，请更换名称。", "子流程命名"); return; }
                    var before = CaptureFlowEditSnapshot();
                    string callId = Guid.NewGuid().ToString("N");
                    foreach (var item in outside)
                    {
                        foreach (var binding in item.InputBindings.Where(binding => ids.Contains(binding.SourceToolId ?? string.Empty)))
                        { binding.SourcePortName = outputKeys[binding.SourceToolId + "|" + binding.SourcePortName]; binding.SourceToolId = callId; }
                        if (item.NumericJudge != null)
                        {
                            var value = item.InputBindings.FirstOrDefault(binding => binding.TargetPortName == "Value");
                            if (value != null) { item.NumericJudge.InputToolId = value.SourceToolId; item.NumericJudge.InputPortName = value.SourcePortName; }
                        }
                    }
                    outside.Insert(start, new ToolFlowRecipeItem { ToolId = callId, ToolType = VmToolKind.SubflowCall.ToString(), InstanceName = name.Text.Trim(), IsEnabled = true,
                        Parameters = new VmToolParameterData { Subflow = definition }, InputBindings = callBindings,
                        RoiIds = new List<string>(), GraphX = all[start].GraphX, GraphY = all[start].GraphY });
                    ApplyFlowFromRecipe(new VisionRecipe { ToolFlow = outside });
                    var call = flowTools.First(tool => tool.ToolId == callId); FlowToolList.SelectedItem = call;
                    CommitFlowEditHistory("创建子流程：" + call.InstanceName, before);
                    ScheduleRecipeStateCheck(); RefreshUiState(); HeaderStatusText.Text = "子流程已创建；双击查看边界，应用配置后可运行当前或全流程。";
                    window.DialogResult = true;
                };
                window.ShowDialog();
            });
        }

        private void ShowSubflowContentsS102_Click(object sender, RoutedEventArgs e)
        {
            RunUiAction("编辑子流程", delegate { EnterSubflowEditorS102(dockEditingTool); });
        }
    }
}
