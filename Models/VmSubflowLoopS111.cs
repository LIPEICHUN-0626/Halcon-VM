using System;
using System.Collections.Generic;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    public static class VmSubflowLoopS111
    {
        public const string Once = "Once", Repeat = "Repeat", Each = "Each";
        public const string Index = "$LoopIndex", Count = "$LoopCount", Item = "$LoopItem";
        public const string Completed = "$LoopCompleted", Planned = "$LoopPlanned", HasLast = "$LoopHasLast";
        public const int MaxIterations = 1000, MaxWork = 10000;
        public static string Mode(VmSubflowData definition) { return string.IsNullOrEmpty(definition.LoopMode) ? Once : definition.LoopMode; }
        public static string CollectionPort(VmSubflowPortData port) { return port.PortName + "$Items"; }
        public static string CollectionType(VmSubflowPortData port) { return port.DataType == "Number" ? "Number[]" : "String[]"; }
        public static List<VmSubflowPortData> ProxyPorts(VmSubflowData definition)
        {
            var ports = (definition.Inputs ?? new List<VmSubflowPortData>()).Where(port => port != null).Select(port => port.Clone()).ToList();
            if (Mode(definition) == Once) return ports;
            ports.Add(new VmSubflowPortData { PortName = Index, DisplayName = "当前项索引（从 0 开始）", DataType = "Number" });
            ports.Add(new VmSubflowPortData { PortName = Count, DisplayName = "本次总项数", DataType = "Number" });
            var input = ports.FirstOrDefault(port => port.PortName == definition.LoopInputPort);
            if (Mode(definition) == Each && input != null && VmArrayContractS105.IsType(input.DataType))
                ports.Add(new VmSubflowPortData { PortName = Item, DisplayName = "当前数组项", DataType = input.DataType == "Number[]" ? "Number" : "String" });
            return ports;
        }
        public static IEnumerable<VmSubflowPortData> ReturnPorts(VmSubflowData definition)
        {
            foreach (var port in definition.Outputs ?? new List<VmSubflowPortData>())
            {
                if (port == null) continue;
                yield return port;
                if (port.CollectIterations) yield return new VmSubflowPortData { PortName = CollectionPort(port), DisplayName = port.DisplayName + " · 全部项", DataType = CollectionType(port) };
            }
        }
        public static string Validate(VmSubflowData definition)
        {
            string mode = Mode(definition);
            if (mode != Once && mode != Repeat && mode != Each) return "子流程执行方式无效。";
            if (mode == Repeat && (definition.RepeatCount < 0 || definition.RepeatCount > MaxIterations)) return "重复次数须为 0–1000。";
            if (mode == Each && !definition.Inputs.Any(port => port != null && port.PortName == definition.LoopInputPort && VmArrayContractS105.IsType(port.DataType)))
                return "逐项执行须选择已有 Number[] 或 String[] 输入；先从内部接口添加数组输入。";
            if (definition.Inputs.Concat(definition.Outputs).Any(port => port != null && (port.PortName ?? string.Empty).StartsWith("$Loop", StringComparison.OrdinalIgnoreCase)))
                return "$Loop 前缀供循环上下文使用，请更换自定义端口名。";
            if (definition.Inputs.Any(port => port != null && (port.PortName == Index || port.PortName == Count || port.PortName == Item)))
                return "调用输入与循环上下文端口同名。";
            if (definition.Outputs.Any(port => port != null && port.CollectIterations && port.DataType != "Number" && port.DataType != "String"))
                return "逐项收集只支持 Number 或 String 返回值；其他类型仍返回最后一次结果。";
            var names = ReturnPorts(definition).Select(port => port.PortName).ToList();
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count) return "逐项收集端口与已有返回端口重名。";
            var proxy = ProxyPorts(definition);
            foreach (var tool in definition.Tools ?? new List<ToolFlowRecipeItem>())
                foreach (var binding in (tool == null ? null : tool.InputBindings) ?? new List<VmToolInputBindingData>())
                    if (binding.SourceToolId == VmSubflowData.InputProxyId && (binding.SourcePortName ?? string.Empty).StartsWith("$Loop", StringComparison.Ordinal) && !proxy.Any(port => port.PortName == binding.SourcePortName))
                        return "内部模块仍引用已关闭的循环上下文：" + binding.SourcePortName + "；请先解除连接。";
            return null;
        }
        public static int Iterations(VmSubflowData definition, VmToolInstance proxy)
        {
            if (Mode(definition) == Once) return 1;
            if (Mode(definition) == Repeat) return definition.RepeatCount;
            object value;
            if (!proxy.TryGetOutputValue(definition.LoopInputPort, out value)) throw new InvalidOperationException("逐项数组输入没有有效值。");
            int count = value is VmNumberArrayS105 ? ((VmNumberArrayS105)value).Count : value is VmStringArrayS105 ? ((VmStringArrayS105)value).Count : -1;
            if (count < 0 || count > MaxIterations) throw new InvalidOperationException("逐项输入须为正式数组且最多 1000 项，不截断。");
            return count;
        }
        public static void SetContext(VmSubflowData definition, VmToolInstance proxy, int index, int count)
        {
            if (Mode(definition) == Once) return;
            proxy.SetOutputValue(Index, (double)index); proxy.SetOutputValue(Count, (double)count);
            if (Mode(definition) != Each || count == 0) return;
            object array; if (!proxy.TryGetOutputValue(definition.LoopInputPort, out array)) throw new InvalidOperationException("逐项数组已失效。");
            proxy.SetOutputValue(Item, array is VmNumberArrayS105 ? (object)((VmNumberArrayS105)array)[index] : ((VmStringArrayS105)array)[index]);
        }
    }
}
