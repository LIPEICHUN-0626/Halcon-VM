using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class ProductionResultCycleS141
        {
            public string RunId, FlowId, SessionId, DefinitionKey, SourceLabel, PageMode, Terminal;
            public int DefinitionVersion;
            public VmRunOriginS135 Origin;
            public bool Complete, DefinitionUnsaved, HasDraft;
            public List<VmProductionResultItemS141> Items;
            public Dictionary<string, VmProductionResultValueS141> Values = new Dictionary<string, VmProductionResultValueS141>(StringComparer.OrdinalIgnoreCase);
        }

        private ProductionResultCycleS141 productionResultCycleS141;
        private string productionResultStatusS141 = "尚无本次生产周期；配置不会自动运行。";

        private void InvalidateProductionResultS141(string reason)
        {
            productionResultCycleS141 = null;
            productionResultStatusS141 = reason;
        }

        private void NotifyProductionResultDefinitionChangeS141()
        {
            // During execution, current variable writes can notify the original recovery writer.
            // They are not new algorithm settings. The full stable definition is checked at cycle completion instead.
            if (!isFlowExecutionActive && productionResultCycleS141 != null)
                InvalidateProductionResultS141("配置或显示映射已操作；旧周期值已失效，请重新运行生产流程。");
        }

        private void BeginProductionResultCycleS141(VmProductionRunRecordS135 record, string runId,
            VmFlowRunRequestMode mode, VmRunOriginS135 origin, bool childEditor, bool simulated)
        {
            InvalidateProductionResultS141("本次执行不是明确的完整生产周期；不显示旧生产值。");
            bool production = mode == VmFlowRunRequestMode.Full && !childEditor && !simulated && !isImageQueueBatchRunning &&
                (origin == VmRunOriginS135.ManualFull || origin == VmRunOriginS135.Continuous || origin == VmRunOriginS135.TcpTrigger) &&
                record != null && record.RunId == runId && record.PageMode == "Production" &&
                record.SimulationKnownS138 && !record.IsSimulationS138 &&
                record.FlowId == activeProjectFlowIdS104 && record.WorkspaceSessionId == projectSessionIdS104 &&
                !string.IsNullOrWhiteSpace(record.AlgorithmDefinitionKeyS138);
            if (!production) return;
            try
            {
                var items = (productionRunLayout.ResultItemsS141 ?? new List<VmProductionResultItemS141>())
                    .Where(item => item != null && string.Equals(item.FlowId, record.FlowId, StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Clone()).ToList();
                if (items.Count == 0)
                { productionResultStatusS141 = "当前流程未配置结果项。"; return; }
                if (items.Count > 8 || items.Select(item => item.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Count)
                    throw new InvalidOperationException("结果项超限或身份重复，不能猜测显示。");
                var cycle = new ProductionResultCycleS141
                {
                    RunId = runId, FlowId = record.FlowId, SessionId = record.WorkspaceSessionId,
                    DefinitionKey = record.AlgorithmDefinitionKeyS138, DefinitionVersion = record.AlgorithmDefinitionKeyVersionS138,
                    Origin = record.Origin, SourceLabel = record.SourceLabel, PageMode = record.PageMode,
                    DefinitionUnsaved = !record.DefinitionSaveKnownS138 || record.AlgorithmDefinitionWasUnsavedS138,
                    HasDraft = record.HasUnappliedDraft, Items = items
                };
                foreach (var item in items)
                {
                    var sources = flowTools.Where(tool => string.Equals(tool.ToolId, item.ToolId, StringComparison.Ordinal)).Take(2).ToList();
                    string missing = sources.Count != 1 ? "来源模块缺失或身份重复，本次不可用。"
                        : !sources[0].IsEnabled ? "来源模块已停用，本次没有输出。"
                        : "本次未执行/未到达该来源步骤；不会沿用旧值。";
                    cycle.Values[item.ItemId] = ProductionResultValueServiceS141.Freeze(item, null, null, false, missing);
                }
                productionResultCycleS141 = cycle;
                productionResultStatusS141 = "本次周期运行中；完成后显示冻结值，不沿用上周期。";
            }
            catch (Exception error)
            { InvalidateProductionResultS141("本次结果显示身份未冻结：" + error.Message + " 检测逻辑未改变。"); }
        }

        private void CaptureProductionResultStepS141(VmProductionRunRecordS135 run, VmToolInstance tool,
            InspectionRecord inspection, string terminal, DateTime startedUtc)
        {
            var cycle = productionResultCycleS141;
            if (cycle == null || run == null || tool == null || cycle.RunId != run.RunId || cycle.Complete) return;
            if (cycle.SessionId != projectSessionIdS104 || cycle.FlowId != activeProjectFlowIdS104)
            { InvalidateProductionResultS141("步骤归属与本次周期不符；不显示跨方案或跨流程数值。"); return; }
            try
            {
                var ports = ToolMetadata.GetInstanceOutputPorts(tool);
                foreach (var item in cycle.Items.Where(item => string.Equals(item.ToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase)))
                {
                    var matches = ports.Where(port => string.Equals(port.PortName, item.PortName, StringComparison.Ordinal)).ToList();
                    object raw = null;
                    bool typeMatches = matches.Count == 1 && string.Equals(matches[0].DataType, item.ValueType, StringComparison.Ordinal) &&
                        ProductionResultValueServiceS141.IsSupportedType(item.ValueType);
                    bool fresh = typeMatches && tool.IsEnabled && inspection != null &&
                        (inspection.ResultCode == "OK" || inspection.ResultCode == "NG") &&
                        string.IsNullOrWhiteSpace(tool.ErrorMessage) && tool.LastOutputUtc.HasValue &&
                        tool.LastOutputUtc.Value >= startedUtc && tool.TryGetOutputValue(item.PortName, out raw);
                    string reason = !typeMatches ? "来源端口缺失、重复或类型已改变。"
                        : inspection == null ? "本次步骤未完成：" + (terminal ?? "失败")
                        : inspection.ResultCode == "SKIP" ? "本次条件跳过，未产生当前输出。"
                        : !fresh ? "本次步骤没有有效的当前输出。" : string.Empty;
                    string unit = item.ValueType == "Number" ? FreezeNumberUnitS138(tool, item.PortName, fresh) : string.Empty;
                    cycle.Values[item.ItemId] = ProductionResultValueServiceS141.Freeze(item, raw, unit, fresh, reason);
                }
            }
            catch (Exception error)
            {
                foreach (var item in cycle.Items.Where(item => string.Equals(item.ToolId, tool.ToolId, StringComparison.OrdinalIgnoreCase)))
                    cycle.Values[item.ItemId] = ProductionResultValueServiceS141.Freeze(item, null, null, false, "本次值冻结失败：" + error.Message);
            }
        }

        private void CompleteProductionResultCycleS141(VmProductionRunRecordS135 run, string code, bool executionError)
        {
            var cycle = productionResultCycleS141;
            if (cycle == null || run == null || cycle.RunId != run.RunId) return;
            try
            {
                if (cycle.SessionId != projectSessionIdS104 || cycle.FlowId != activeProjectFlowIdS104 ||
                    cycle.DefinitionVersion != ProductionDefinitionIdentityS138.Version ||
                    !string.Equals(cycle.DefinitionKey, ProductionDefinitionIdentityS138.Compute(CaptureRecipe()), StringComparison.Ordinal))
                { InvalidateProductionResultS141("运行中正式定义或流程身份已变化；本次显示失效，未重判检测结果。"); return; }
                cycle.Terminal = code == "STOP" || code == "TIMEOUT" ? code : executionError || code != "OK" && code != "NG" ? "ERROR" : code;
                if (cycle.Terminal != "OK" && cycle.Terminal != "NG")
                    foreach (var item in cycle.Items)
                        cycle.Values[item.ItemId] = ProductionResultValueServiceS141.Freeze(item, null, null, false,
                            "本次周期 " + cycle.Terminal + "，不以部分测量遮蔽失败或停止。");
                cycle.Complete = true;
                productionResultStatusS141 = "本次周期 " + cycle.Terminal + (cycle.DefinitionUnsaved ? " · 定义未保存/保存状态未知" : string.Empty) +
                    (cycle.HasDraft ? " · 有未应用草稿（不含在数值内）" : string.Empty);
            }
            catch (Exception error)
            { InvalidateProductionResultS141("本次显示定义复核失败：" + error.Message + " 原检测判定未修改。"); }
        }

        private string GetProductionResultStatusS141() { return productionResultStatusS141; }

        private void ValidateProductionResultConfigurationS141(VmProductionRunLayoutData layout)
        {
            var definition = CaptureRecipe();
            var flows = definition.Flows ?? new List<VmProjectFlowData>();
            foreach (var item in layout.ResultItemsS141 ?? new List<VmProductionResultItemS141>())
            {
                if (string.IsNullOrWhiteSpace(item.Title) || !ProductionResultValueServiceS141.IsSupportedType(item.ValueType))
                    throw new InvalidOperationException("每个结果项须有标题并选择正式标量输出；未完成草稿不能正式应用。");
                var flowMatches = flows.Where(flow => flow != null && string.Equals(flow.FlowId, item.FlowId, StringComparison.Ordinal)).Take(2).ToList();
                if (flowMatches.Count != 1) throw new InvalidOperationException("结果项的流程身份缺失、重复或已变化，请明确重选。");
                var toolMatches = (flowMatches[0].Tools ?? new List<ToolFlowRecipeItem>()).Where(tool => tool != null &&
                    string.Equals(tool.ToolId, item.ToolId, StringComparison.Ordinal)).Take(2).ToList();
                VmToolKind kind;
                if (toolMatches.Count != 1 || !Enum.TryParse(toolMatches[0].ToolType, out kind) || !Enum.IsDefined(typeof(VmToolKind), kind))
                    throw new InvalidOperationException("结果项须选择真实根模块，不能按同名或内部子流程猜来源。");
                var source = toolMatches[0];
                using (var instance = new VmToolInstance { ToolId = source.ToolId, Kind = kind,
                    Parameters = source.Parameters == null ? new VmToolParameterData() : source.Parameters.Clone() })
                {
                    var ports = ToolMetadata.GetInstanceOutputPorts(instance).Where(port => port != null &&
                        string.Equals(port.PortName, item.PortName, StringComparison.Ordinal)).Take(2).ToList();
                    if (ports.Count != 1 || ports[0].DataType != item.ValueType)
                        throw new InvalidOperationException("结果项的正式输出端口或类型已变化，请明确重新选择后应用。");
                }
            }
        }

        private List<VmProductionResultRowS141> GetProductionResultRowsS141(VmProductionRunLayoutData layout)
        {
            var rows = new List<VmProductionResultRowS141>();
            var cycle = productionResultCycleS141;
            bool sameContext = cycle != null && cycle.Complete && cycle.FlowId == activeProjectFlowIdS104 && cycle.SessionId == projectSessionIdS104;
            foreach (var item in (layout.ResultItemsS141 ?? new List<VmProductionResultItemS141>()).Where(value => value != null &&
                string.Equals(value.FlowId, activeProjectFlowIdS104, StringComparison.OrdinalIgnoreCase)).Take(8))
            {
                VmProductionResultValueS141 value = null;
                var frozenItem = sameContext ? cycle.Items.FirstOrDefault(frozen => string.Equals(frozen.ItemId, item.ItemId, StringComparison.OrdinalIgnoreCase)) : null;
                bool sameSource = frozenItem != null && frozenItem.FlowId == item.FlowId && frozenItem.ToolId == item.ToolId &&
                    frozenItem.PortName == item.PortName && frozenItem.ValueType == item.ValueType;
                if (sameSource) cycle.Values.TryGetValue(item.ItemId, out value);
                bool valid = value != null && value.State == "Valid";
                string state = valid ? "本次值" : value != null ? value.Reason : !sameContext ? productionResultStatusS141 : "显示来源未在本次冻结映射中；应用并重新运行。";
                string detail = "来源 FlowId：" + item.FlowId + "\nToolId：" + item.ToolId + " · 端口：" + item.PortName + " · 类型：" + item.ValueType +
                    "\n" + state;
                if (sameContext)
                    detail += "\nRunId：" + cycle.RunId + "\n算法定义 v" + cycle.DefinitionVersion.ToString(CultureInfo.InvariantCulture) + "：" + cycle.DefinitionKey +
                        "\n来源：" + cycle.Origin + " / " + cycle.SourceLabel + " · 开始页面：" + cycle.PageMode + " · 已知非模拟";
                if (value != null && !string.IsNullOrWhiteSpace(value.Reason)) detail += "\n" + value.Reason;
                if (valid && (item.ValueType == "String" || item.ValueType == "ResultCode")) detail += "\n完整文本：" + value.TextValue;
                string formatted = valid ? ProductionResultValueServiceS141.Format(value, item.DecimalPlaces) : "--";
                if (valid && string.IsNullOrEmpty(formatted)) formatted = "（空文本）";
                rows.Add(new VmProductionResultRowS141
                {
                    Title = string.IsNullOrWhiteSpace(item.Title) ? "结果项" : item.Title,
                    ValueText = formatted,
                    UnitText = item.ValueType == "Number" ? valid && !string.IsNullOrWhiteSpace(value.Unit) ? value.Unit : "单位未声明" : string.Empty,
                    StateText = state, DetailText = detail
                });
            }
            return rows;
        }
    }
}
