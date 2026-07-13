using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    public enum VmFlowRunRequestMode
    {
        Full,
        Current,
        ToHere,
        FromHere
    }

    public enum VmFlowStepDecision
    {
        Continue,
        Pause,
        UserStop,
        Timeout,
        NgStop
    }

    [DataContract]
    public sealed class VmFlowRunPolicy
    {
        public VmFlowRunPolicy()
        {
            ContinuousIntervalMilliseconds = 500;
            FlowTimeoutMilliseconds = 0;
            StopOnNg = false;
        }

        [DataMember(Order = 1)]
        public int ContinuousIntervalMilliseconds { get; set; }

        [DataMember(Order = 2)]
        public int FlowTimeoutMilliseconds { get; set; }

        [DataMember(Order = 3)]
        public bool StopOnNg { get; set; }

        public VmFlowRunPolicy Normalize()
        {
            return new VmFlowRunPolicy
            {
                ContinuousIntervalMilliseconds = ContinuousIntervalMilliseconds <= 0
                    ? 500
                    : Math.Min(60000, Math.Max(50, ContinuousIntervalMilliseconds)),
                FlowTimeoutMilliseconds = FlowTimeoutMilliseconds <= 0
                    ? 0
                    : Math.Min(3600000, Math.Max(50, FlowTimeoutMilliseconds)),
                StopOnNg = StopOnNg
            };
        }
    }

    public sealed class VmFlowExecutionPlan
    {
        public VmFlowExecutionPlan(VmFlowRunRequestMode mode, IList<VmToolInstance> tools, string rangeText)
        {
            Mode = mode;
            Tools = tools ?? new List<VmToolInstance>();
            RangeText = rangeText ?? string.Empty;
        }

        public VmFlowRunRequestMode Mode { get; private set; }

        public IList<VmToolInstance> Tools { get; private set; }

        public string RangeText { get; private set; }
    }

    public static class VmFlowExecutionPlanner
    {
        public static VmFlowExecutionPlan BuildPlan(
            IEnumerable<VmToolInstance> source,
            VmToolInstance selected,
            VmFlowRunRequestMode mode)
        {
            List<VmToolInstance> ordered = (source ?? Enumerable.Empty<VmToolInstance>()).ToList();
            if (ordered.Count == 0)
            {
                throw new InvalidOperationException("流程中没有工具。");
            }

            int selectedIndex = selected == null ? -1 : ordered.IndexOf(selected);
            if (mode != VmFlowRunRequestMode.Full && selectedIndex < 0)
            {
                throw new InvalidOperationException("请先选择流程工具。");
            }

            IEnumerable<VmToolInstance> range;
            string rangeText;
            switch (mode)
            {
                case VmFlowRunRequestMode.Current:
                    range = ordered.Skip(selectedIndex).Take(1);
                    rangeText = "当前 · " + selected.InstanceName;
                    break;
                case VmFlowRunRequestMode.ToHere:
                    range = ordered.Take(selectedIndex + 1);
                    rangeText = "运行到此 · 1-" + (selectedIndex + 1);
                    break;
                case VmFlowRunRequestMode.FromHere:
                    range = ordered.Skip(selectedIndex);
                    rangeText = "从此运行 · " + (selectedIndex + 1) + "-" + ordered.Count;
                    break;
                default:
                    range = ordered;
                    rangeText = "全流程 · 1-" + ordered.Count;
                    break;
            }

            List<VmToolInstance> enabled = range.Where(item => item != null && item.IsEnabled).ToList();
            if (enabled.Count == 0)
            {
                throw new InvalidOperationException("所选运行范围内没有启用的工具。");
            }

            return new VmFlowExecutionPlan(mode, enabled, rangeText);
        }
    }

    public static class VmFlowRuntimeDecider
    {
        public static VmFlowStepDecision EvaluateBeforeStep(
            VmFlowRunPolicy policy,
            long elapsedMilliseconds,
            bool pauseRequested,
            bool stopRequested)
        {
            VmFlowRunPolicy effective = (policy ?? new VmFlowRunPolicy()).Normalize();
            if (stopRequested)
            {
                return VmFlowStepDecision.UserStop;
            }

            if (pauseRequested)
            {
                return VmFlowStepDecision.Pause;
            }

            return effective.FlowTimeoutMilliseconds > 0 && elapsedMilliseconds >= effective.FlowTimeoutMilliseconds
                ? VmFlowStepDecision.Timeout
                : VmFlowStepDecision.Continue;
        }

        public static VmFlowStepDecision EvaluateAfterStep(
            VmFlowRunPolicy policy,
            long elapsedMilliseconds,
            string resultCode,
            bool stopRequested)
        {
            VmFlowRunPolicy effective = (policy ?? new VmFlowRunPolicy()).Normalize();
            if (stopRequested)
            {
                return VmFlowStepDecision.UserStop;
            }

            if (effective.FlowTimeoutMilliseconds > 0 && elapsedMilliseconds >= effective.FlowTimeoutMilliseconds)
            {
                return VmFlowStepDecision.Timeout;
            }

            return effective.StopOnNg && string.Equals(resultCode, "NG", StringComparison.OrdinalIgnoreCase)
                ? VmFlowStepDecision.NgStop
                : VmFlowStepDecision.Continue;
        }
    }
}
