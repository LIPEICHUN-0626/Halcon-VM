using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ProductionAlarmStoreS136 productionAlarmStoreS136;
        private ProductionAlarmWindowS136 productionAlarmWindowS136;
        private readonly Dictionary<string, VmProductionAlarmRecordS136> productionAlarmsS136 = new Dictionary<string, VmProductionAlarmRecordS136>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> productionAlarmChangesS136 = new Dictionary<string, long>(StringComparer.Ordinal);
        private long productionAlarmChangeSerialS136;
        private int productionAlarmReadRevisionS136;
        private int productionAlarmPendingWritesS136;
        private bool productionAlarmShutdownS136;
        private bool productionAlarmReadCompletedS136;
        private bool currentRootExecutionErrorS136;
        private List<string> productionAlarmWarningsS136 = new List<string>();
        private string productionAlarmStorageStatusS136 = "报警状态尚未加载";

        private void RefreshProductionAlarmSummaryS136()
        {
            if (ProductionAlarmText == null) return;
            var pending = productionAlarmsS136.Values.Where(record => record.State != VmProductionAlarmStateS136.Handled)
                .OrderByDescending(record => record.OccurredAtUtc).ToList();
            int unread = pending.Count(record => record.State == VmProductionAlarmStateS136.Open);
            int acknowledged = pending.Count - unread;
            VmProductionAlarmRecordS136 latest = pending.FirstOrDefault();
            ProductionAlarmText.Text = !productionAlarmReadCompletedS136 && productionAlarmsS136.Count == 0 ? "正在读取报警状态…" :
                "已载入：未确认 " + unread + " · 已确认待处置 " + acknowledged +
                (latest == null ? "\n当前载入记录没有未处置事件。" : "\n" + ProductionAlarmSourceLabelS136(latest.Source) + " · " + latest.Title);
            ProductionAlarmText.ToolTip = latest == null ? "确认仅表示已读；人工处置记录不等于设备恢复。" :
                ProductionAlarmSourceLabelS136(latest.Source) + " · " + latest.OccurredAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + "\n" + latest.Detail +
                (latest.TextTruncated ? "\n发生事实摘要已截断，不能视为完整诊断。" : "");
            if (ProductionAlarmStorageStatusTextS136 != null)
            {
                ProductionAlarmStorageStatusTextS136.Text = productionAlarmStorageStatusS136;
                ProductionAlarmStorageStatusTextS136.ToolTip = productionAlarmStorageStatusS136;
            }
        }

        private static string ProductionAlarmSourceLabelS136(VmProductionAlarmSourceS136 source)
        {
            switch (source)
            {
                case VmProductionAlarmSourceS136.RunError: return "流程执行";
                case VmProductionAlarmSourceS136.RunTimeout: return "运行超时";
                case VmProductionAlarmSourceS136.CameraConnectionFailure: return "相机连接";
                case VmProductionAlarmSourceS136.TcpCommunicationError: return "TCP 通讯";
                case VmProductionAlarmSourceS136.ResultReturnUncertain: return "设备回传";
                case VmProductionAlarmSourceS136.ChangeoverDoubleFailure: return "方案换型";
                default: return "周期追溯";
            }
        }

        private VmProductionAlarmRecordS136 FreezeProductionAlarmS136(VmProductionAlarmSourceS136 source,
            VmProductionAlarmSeverityS136 severity, string title, string detail, VmToolInstance tool,
            string runId, VmProductionRunRecordS135 run)
        {
            var alarm = new VmProductionAlarmRecordS136
            {
                OccurrenceId = Guid.NewGuid().ToString("N"), Version = 1, OccurredAtUtc = DateTime.UtcNow,
                Source = source, Severity = severity, State = VmProductionAlarmStateS136.Open,
                Title = title, Detail = detail, RunId = runId ?? run?.RunId ?? "",
                ToolId = tool?.ToolId ?? "", ToolName = tool?.InstanceName ?? "", HandlingNote = "",
                RecipeName = run?.RecipeName ?? RecipeNameEditTextBox?.Text ?? "",
                RecipePath = run?.RecipePath ?? currentRecipePath ?? "",
                RecipeDefinitionSha256 = run?.RecipeDefinitionSha256 ?? "",
                WorkspaceSessionId = run?.WorkspaceSessionId ?? projectSessionIdS104,
                FlowId = run?.FlowId ?? activeProjectFlowIdS104, FlowName = run?.FlowName ?? GetProjectFlowNameS104(),
                IdentityWarning = ""
            };
            if (run == null)
            {
                try
                {
                    string json = recipeService.GetRecipeFingerprint(CaptureRecipe());
                    using (var sha = SHA256.Create()) alarm.RecipeDefinitionSha256 = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex) { alarm.IdentityWarning = "发生时定义身份未能冻结：" + ex.Message; }
            }
            if (string.IsNullOrWhiteSpace(alarm.RecipeDefinitionSha256) && string.IsNullOrWhiteSpace(alarm.IdentityWarning))
                alarm.IdentityWarning = "发生时方案定义身份未知，不能按名称猜测原模块。";
            alarm.NormalizeBounded();
            return alarm;
        }

        private void PublishProductionAlarmS136(VmProductionAlarmSourceS136 source, VmProductionAlarmSeverityS136 severity,
            string title, string detail, VmToolInstance tool = null, string runId = null, VmProductionRunRecordS135 run = null)
        {
            if (productionAlarmShutdownS136) return;
            try { PublishFrozenProductionAlarmS136(FreezeProductionAlarmS136(source, severity, title, detail, tool, runId, run)); }
            catch (Exception ex) { SetProductionAlarmPersistenceFailureS136("发生身份未能冻结：" + ex.Message); }
        }

        private async void PublishFrozenProductionAlarmS136(VmProductionAlarmRecordS136 occurrence)
        {
            if (productionAlarmShutdownS136) return;
            if (productionAlarmPendingWritesS136 >= 64)
            {
                productionAlarmStorageStatusS136 = "报警待写队列达到 64 条，本次未保存；不能宣称没有遗漏。";
                RefreshProductionAlarmSummaryS136();
                return;
            }
            productionAlarmPendingWritesS136++;
            try
            {
                // Freeze all identities on the UI thread before any await; never reclassify free-form log messages.
                var frozen = occurrence.Clone().NormalizeBounded();
                var result = await Task.Run(() => productionAlarmStoreS136.Add(frozen));
                ApplyProductionAlarmMutationS136(result);
            }
            catch (Exception ex) { SetProductionAlarmPersistenceFailureS136("发生事件未保存：" + ex.Message); }
            finally { productionAlarmPendingWritesS136--; }
        }

        private void ApplyProductionAlarmMutationS136(VmProductionAlarmMutationResultS136 result)
        {
            if (result == null || !result.Success || result.Record == null)
            {
                SetProductionAlarmPersistenceFailureS136(result?.ErrorMessage ?? "报警状态保存没有返回有效记录。");
                return;
            }
            var record = result.Record.Clone();
            productionAlarmsS136[record.OccurrenceId] = record;
            productionAlarmChangesS136[record.OccurrenceId] = ++productionAlarmChangeSerialS136;
            // The cache is not a second unbounded history store; never evict an unhandled event silently.
            foreach (var old in productionAlarmsS136.Values.Where(value => value.State == VmProductionAlarmStateS136.Handled)
                .OrderBy(value => value.OccurredAtUtc).Take(Math.Max(0, productionAlarmsS136.Count - 2000)).ToList())
            {
                productionAlarmsS136.Remove(old.OccurrenceId);
                productionAlarmChangesS136.Remove(old.OccurrenceId);
            }
            productionAlarmStorageStatusS136 = "本地报警状态已保存" + (string.IsNullOrWhiteSpace(result.Warning) ? "" : " · " + result.Warning);
            RefreshProductionAlarmSummaryS136();
            if (productionAlarmWindowS136 != null)
            {
                productionAlarmWindowS136.SetRecords(productionAlarmsS136.Values.Select(value => value.Clone()), productionAlarmWarningsS136);
                productionAlarmWindowS136.SetStatus(productionAlarmStorageStatusS136 + "；确认不是恢复，处置不会自动运行或外发。");
            }
        }

        private void SetProductionAlarmPersistenceFailureS136(string message)
        {
            // Do not send a failing alarm store back through AppendAlarm/logger to recurse on the same disk failure.
            productionAlarmStorageStatusS136 = "报警状态未保存：" + message;
            RefreshProductionAlarmSummaryS136();
            productionAlarmWindowS136?.SetStatus(productionAlarmStorageStatusS136);
        }

        private void OpenProductionAlarmsS136_Click(object sender, RoutedEventArgs e)
        {
            if (productionAlarmWindowS136 != null) { productionAlarmWindowS136.Activate(); return; }
            var window = new ProductionAlarmWindowS136 { Owner = this };
            productionAlarmWindowS136 = window;
            window.RefreshRequested += delegate { RefreshProductionAlarmsS136(); };
            window.AcknowledgeRequested += delegate(object s, VmProductionAlarmActionEventArgsS136 args) { ChangeProductionAlarmStateS136(args, false); };
            window.HandleRequested += delegate(object s, VmProductionAlarmActionEventArgsS136 args) { ChangeProductionAlarmStateS136(args, true); };
            window.LocateRequested += delegate(object s, VmProductionAlarmActionEventArgsS136 args) { LocateProductionAlarmToolS136(args); };
            window.OpenTaskRequestedS150 += delegate(object s, VmProductionAlarmActionEventArgsS136 args) { NavigateProductionAlarmTaskS150(args, true); };
            window.Closed += delegate { productionAlarmReadRevisionS136++; if (ReferenceEquals(window, productionAlarmWindowS136)) productionAlarmWindowS136 = null; };
            window.SetRecords(productionAlarmsS136.Values.Select(value => value.Clone()), productionAlarmWarningsS136);
            window.Show();
            RefreshProductionAlarmsS136();
        }

        private async void RefreshProductionAlarmsS136()
        {
            if (productionAlarmShutdownS136) return;
            var window = productionAlarmWindowS136;
            int revision = ++productionAlarmReadRevisionS136;
            long serial = productionAlarmChangeSerialS136;
            window?.SetBusy(true);
            try
            {
                var read = await Task.Run(() => productionAlarmStoreS136.ReadRecent());
                if (productionAlarmShutdownS136 || revision != productionAlarmReadRevisionS136) return;
                // Preserve only mutations that completed while this read was in flight, not stale retained history.
                var changed = productionAlarmsS136.Values.Where(record => productionAlarmChangesS136.ContainsKey(record.OccurrenceId) && productionAlarmChangesS136[record.OccurrenceId] > serial).Select(record => record.Clone()).ToList();
                productionAlarmsS136.Clear();
                foreach (var record in read.Records) productionAlarmsS136[record.OccurrenceId] = record.Clone();
                foreach (var record in changed)
                {
                    VmProductionAlarmRecordS136 loaded;
                    if (!productionAlarmsS136.TryGetValue(record.OccurrenceId, out loaded) || loaded.Version < record.Version) productionAlarmsS136[record.OccurrenceId] = record;
                }
                foreach (string id in productionAlarmChangesS136.Keys.Where(id => !productionAlarmsS136.ContainsKey(id)).ToList()) productionAlarmChangesS136.Remove(id);
                productionAlarmWarningsS136 = read.Warnings.ToList();
                productionAlarmReadCompletedS136 = true;
                productionAlarmStorageStatusS136 = "已载入 " + productionAlarmsS136.Count + " 条报警事件" +
                    (read.Truncated || read.Warnings.Count > 0 ? " · 范围有界或存在读取警告，不能宣称完整" : " · 本地状态") + " · " + read.DirectoryPath;
                RefreshProductionAlarmSummaryS136();
                if (ReferenceEquals(window, productionAlarmWindowS136) && window != null)
                {
                    window.SetRecords(productionAlarmsS136.Values.Select(record => record.Clone()), productionAlarmWarningsS136);
                    window.SetStatus(productionAlarmStorageStatusS136);
                }
            }
            catch (Exception ex)
            {
                if (revision == productionAlarmReadRevisionS136 && !productionAlarmShutdownS136)
                    SetProductionAlarmPersistenceFailureS136("读取失败，不能当作没有报警：" + ex.Message);
            }
            finally
            {
                if (revision == productionAlarmReadRevisionS136 && ReferenceEquals(window, productionAlarmWindowS136)) window?.SetBusy(false);
            }
        }

        private async void ChangeProductionAlarmStateS136(VmProductionAlarmActionEventArgsS136 args, bool handle)
        {
            var window = productionAlarmWindowS136;
            if (window == null || args == null || productionAlarmShutdownS136 || productionAlarmPendingWritesS136 >= 64) return;
            string id = args.OccurrenceId, note = args.HandlingNote;
            int version = args.ExpectedVersion;
            productionAlarmPendingWritesS136++;
            window.SetBusy(true);
            try
            {
                var result = await Task.Run(() => handle ? productionAlarmStoreS136.Handle(id, version, note) : productionAlarmStoreS136.Acknowledge(id, version));
                ApplyProductionAlarmMutationS136(result);
                if (ReferenceEquals(window, productionAlarmWindowS136))
                    window.SetStatus(result.Success ? (handle ? "已记录人工处置说明；设备是否恢复仍需核对，不自动启用或重发。" : "已确认阅读，事件仍待处置；未解除任何运行保护。") : "状态未更改：" + result.ErrorMessage + "；请刷新后重新选择。");
            }
            catch (Exception ex) { SetProductionAlarmPersistenceFailureS136(ex.Message); }
            finally
            {
                productionAlarmPendingWritesS136--;
                if (ReferenceEquals(window, productionAlarmWindowS136)) window.SetBusy(false);
            }
        }

        private void LocateProductionAlarmToolS136(VmProductionAlarmActionEventArgsS136 args)
        {
            NavigateProductionAlarmTaskS150(args, false);
        }
    }
}
