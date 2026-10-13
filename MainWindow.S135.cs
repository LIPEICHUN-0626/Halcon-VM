using System;
using System.Diagnostics;
using System.Globalization;
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
        private readonly ProductionRunStoreS135 productionRunStoreS135;
        private ProductionRunHistoryWindowS135 productionRunHistoryWindowS135;
        private int productionRunReadRevisionS135;
        private VmProductionRunRecordS135 activeProductionRunRecordS135;
        private bool productionRunSaveInProgressS135;
        private string productionRunSaveStatusS135 = "周期记录：尚未运行";
        private const int MaximumCapturedRunStepsS135 = 2048;

        private void SetProductionRunSaveStatusS135(string message)
        {
            productionRunSaveStatusS135 = message;
            if (ProductionRunSaveStatusTextS135 != null)
            {
                ProductionRunSaveStatusTextS135.Text = message;
                ProductionRunSaveStatusTextS135.ToolTip = message;
            }
        }

        private VmProductionRunRecordS135 BeginProductionRunS135(string runId, VmFlowRunRequestMode mode,
            VmRunOriginS135 origin, string sourceLabel, VmFlowExecutionPlan plan, bool childEditor, bool simulated)
        {
            if (mode != VmFlowRunRequestMode.Full || childEditor) return null;
            if (simulated || isImageQueueBatchRunning || origin == VmRunOriginS135.ImageBatch)
            {
                SetProductionRunSaveStatusS135(simulated ? "离线模拟：不写入生产周期日志" : "图片批次：使用原独立追溯，不重复计为普通周期");
                return null;
            }
            try
            {
                // Freeze the applied definition, not a later page mode, draft or per-step mutable record.
                VisionRecipe definition = CaptureRecipe();
                string json = recipeService.GetRecipeFingerprint(definition);
                string hash;
                using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
                var record = new VmProductionRunRecordS135
                {
                    RunId = runId, StartedAtUtc = DateTime.UtcNow,
                    RecipeName = definition.Name, RecipePath = currentRecipePath ?? string.Empty,
                    RecipeDefinitionSha256 = hash,
                    AlgorithmDefinitionKeyS138 = ProductionDefinitionIdentityS138.Compute(definition),
                    AlgorithmDefinitionKeyVersionS138 = ProductionDefinitionIdentityS138.Version,
                    SimulationKnownS138 = true, IsSimulationS138 = simulated,
                    RuntimeInputSummaryS138 = CaptureRuntimeInputsS138(definition),
                    RecipeWasUnsaved = string.IsNullOrWhiteSpace(currentRecipePath) || !string.Equals(json, savedRecipeFingerprint, StringComparison.Ordinal),
                    HasUnappliedDraft = dockDraftDirty,
                    FlowId = activeProjectFlowIdS104, FlowName = GetProjectFlowNameS104(), WorkspaceSessionId = projectSessionIdS104,
                    Origin = origin, PageMode = productionRunMode ? "Production" : "Developer", SourceLabel = sourceLabel,
                    PlannedStepCount = plan.Tools.Count
                };
                FreezeDefinitionSavedStateS138(record);
                activeProductionRunRecordS135 = record;
                SetProductionRunSaveStatusS135("本次周期 " + runId.Substring(0, 10) + " · 运行结束后保存");
                return record;
            }
            catch (Exception ex)
            {
                SetProductionRunSaveStatusS135("本次周期身份冻结失败，不能宣称已记录：" + ex.Message);
                NotifyProductionRunFailureS135(productionRunSaveStatusS135, runId);
                return null; // Trace failure never starts another inspection or sends a result.
            }
        }

        private void CaptureProductionRunStepS135(VmProductionRunRecordS135 run, VmToolInstance tool,
            InspectionRecord inspection, string terminalIfNoRecord, string failure, double elapsed, DateTime stepStartedUtc)
        {
            CaptureProductionResultStepS141(run, tool, inspection, terminalIfNoRecord, stepStartedUtc);
            CaptureProductionImageStepS142(run, tool, inspection, terminalIfNoRecord, stepStartedUtc);
            if (run == null) return;
            run.TotalStepCount++;
            if (run.CapturedSteps.Count >= MaximumCapturedRunStepsS135) { run.StepsTruncated = true; return; }
            try
            {
            var step = new VmProductionRunStepS135
            {
                Sequence = run.TotalStepCount, ToolId = tool.ToolId, ToolName = tool.InstanceName, ToolType = tool.Kind.ToString(),
                ResultCode = inspection == null ? terminalIfNoRecord : inspection.ResultCode,
                ElapsedMilliseconds = elapsed, InputSummary = tool.InputSummary, OutputSummary = tool.OutputSummary,
                Message = inspection == null ? failure : inspection.Message,
                ErrorMessage = string.IsNullOrEmpty(failure) ? tool.ErrorMessage : failure
            };
            // Only independent strings/numbers survive this boundary; no ROI, image or runtime port handle is retained.
            CaptureProductionNumbersS138(run, step, tool, inspection, stepStartedUtc);
            step.NormalizeBounded(); run.CapturedSteps.Add(step);
            }
            catch (Exception ex)
            {
                run.StepsTruncated = true;
                SetProductionRunSaveStatusS135("本次步骤摘要未完整捕获：" + ex.Message);
                NotifyProductionRunFailureS135(productionRunSaveStatusS135, run.RunId, run);
            }
        }

        private async Task PersistProductionRunS135(VmProductionRunRecordS135 run, string resultCode,
            bool executionError, string stopReason, double elapsed)
        {
            if (run == null)
            {
                if (productionImageCycleS142 != null) productionImageCycleS142.Dispose();
                productionImageCycleS142 = null;
                return;
            }
            productionRunSaveInProgressS135 = true;
            bool associationCompleteS142 = false;
            bool recordPublishedS142 = false;
            try
            {
                run.EndedAtUtc = DateTime.UtcNow;
                run.ElapsedMilliseconds = elapsed;
                run.TerminalState = resultCode == "STOP" || resultCode == "TIMEOUT" ? resultCode
                    : executionError || resultCode != "OK" && resultCode != "NG" ? "ERROR" : resultCode;
                run.StopReason = stopReason ?? string.Empty;
                await PublishProductionImagesS142(run);
                if (run.EvidenceImagesS142 != null && run.EvidenceImagesS142.Any(image => image.State == "Saved"))
                    run.EvidenceStatusS142 = "本周期源采集原图 " + run.EvidenceImagesS142.Count(image => image.State == "Saved") + "/" +
                        run.EvidenceImagesS142.Count + " 已保存并关联此记录；缺图详见来源状态。";
                run.NormalizeBounded();
                var frozen = run.Clone();
                SetProductionRunSaveStatusS135("周期 " + run.RunId.Substring(0, 10) + " · 保存中（检测耗时不含日志写入）");
                // Keep the accepted execution slot until this single publish ends. No timer can build an unbounded write queue.
                var saved = await Task.Run(() => productionRunStoreS135.Save(frozen));
                recordPublishedS142 = saved.Saved || saved.Duplicate;
                string imageAssociationS142 = await FinishProductionImageAssociationS142(run, saved.Saved || saved.Duplicate);
                associationCompleteS142 = true;
                string status = saved.Saved || saved.Duplicate
                    ? "周期已保存 · " + run.RunId.Substring(0, 10) + " · " + run.TerminalState
                    : "周期未保存 · " + run.RunId.Substring(0, 10) + " · " + saved.ErrorMessage;
                if (run.StepsTruncated || run.TextTruncated || run.NumbersTruncatedS138 || run.CapturedSteps.Any(step => step.TextTruncated))
                    status += " · 步骤/文字/数值有截断，详见记录标记";
                if (!string.IsNullOrWhiteSpace(saved.Warning)) status += " · " + saved.Warning;
                status += imageAssociationS142;
                if (run.EvidenceKnownS142 && run.EvidenceImagesS142 != null && run.EvidenceImagesS142.Count > 0 &&
                    run.EvidenceImagesS142.Any(image => image.State != "Saved")) status += " · 部分源图缺失，记录保留原因";
                SetProductionRunSaveStatusS135(status);
                if (!saved.Saved && !saved.Duplicate) NotifyProductionRunFailureS135(status, run.RunId, run);
                if (productionRunHistoryWindowS135 != null)
                    productionRunHistoryWindowS135.SetStatus(status + "；点击刷新查看最新存储记录。当前筛选和检测结果未改变。");
            }
            catch (Exception ex)
            {
                string cleanupS142 = associationCompleteS142 || recordPublishedS142 ? string.Empty : await FinishProductionImageAssociationS142(run, false);
                SetProductionRunSaveStatusS135("周期未保存 · " + run.RunId.Substring(0, 10) + " · " + ex.Message + cleanupS142);
                NotifyProductionRunFailureS135(productionRunSaveStatusS135, run.RunId, run);
            }
            finally
            {
                if (productionImageCycleS142 != null && ReferenceEquals(productionImageCycleS142.Record, run))
                { productionImageCycleS142.Dispose(); productionImageCycleS142 = null; }
                productionRunSaveInProgressS135 = false;
                if (ReferenceEquals(activeProductionRunRecordS135, run)) activeProductionRunRecordS135 = null;
            }
        }

        private void NotifyProductionRunFailureS135(string message, string runId = null, VmProductionRunRecordS135 run = null)
        {
            // A failing log disk must not recursively fail the inspection outcome or initiate device retries.
            PublishProductionAlarmS136(VmProductionAlarmSourceS136.RunRecordSaveFailure, VmProductionAlarmSeverityS136.Warning,
                "周期追溯未完整保存", message + "；检测判定未改动，不重发设备结果。", null, runId, run);
            try { AppendAlarm(message); } catch { HeaderStatusText.Text = message; }
        }

        private void OpenProductionRunHistoryS135_Click(object sender, RoutedEventArgs e)
        {
            if (productionRunHistoryWindowS135 != null) { SetCurrentTrendIdentityS138(productionRunHistoryWindowS135); productionRunHistoryWindowS135.Activate(); return; }
            var window = new ProductionRunHistoryWindowS135 { Owner = this };
            SetCurrentTrendIdentityS138(window);
            productionRunHistoryWindowS135 = window;
            window.RefreshRequested += delegate { RefreshProductionRunHistoryS135(); };
            window.ExportRequested += delegate { ExportProductionRunHistoryS135(); };
            window.EvidenceRequestedS142 += (sourceWindow, args) => ReadProductionEvidenceS142(window, args);
            window.Closed += delegate
            {
                productionRunReadRevisionS135++;
                if (ReferenceEquals(window, productionRunHistoryWindowS135)) productionRunHistoryWindowS135 = null;
            };
            window.Show(); RefreshProductionRunHistoryS135();
        }

        private async void RefreshProductionRunHistoryS135()
        {
            var window = productionRunHistoryWindowS135;
            if (window == null) return;
            int revision = ++productionRunReadRevisionS135;
            window.SetBusy(true); window.SetStatus("只读加载本地周期日志；不会把历史结果放回当前方案。" );
            try
            {
                var read = await Task.Run(() => productionRunStoreS135.ReadRecent());
                if (!ReferenceEquals(window, productionRunHistoryWindowS135) || revision != productionRunReadRevisionS135) return;
                window.SetRecords(read.Records, read.Warnings);
                SetCurrentTrendIdentityS138(window);
                window.SetStatus("已加载 " + read.Records.Count.ToString(CultureInfo.InvariantCulture) + " 条值记录" +
                    (read.Truncated ? "（读取范围有界，不代表全部历史）" : "") + " · " + read.DirectoryPath);
            }
            catch (Exception ex)
            {
                if (ReferenceEquals(window, productionRunHistoryWindowS135) && revision == productionRunReadRevisionS135)
                    window.SetStatus("周期日志读取失败，不能当作没有历史：" + ex.Message);
            }
            finally
            {
                if (ReferenceEquals(window, productionRunHistoryWindowS135) && revision == productionRunReadRevisionS135) window.SetBusy(false);
            }
        }

        private async void ExportProductionRunHistoryS135()
        {
            var window = productionRunHistoryWindowS135;
            if (window == null) return;
            var records = window.FilteredRecords.ToList();
            if (records.Count == 0) { window.SetStatus("当前筛选没有周期记录，不导出空白表冒充生产历史。" ); return; }
            var dialog = new Microsoft.Win32.SaveFileDialog
            { Title = "导出当前筛选的周期记录", Filter = "CSV (*.csv)|*.csv", FileName = "运行周期-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv" };
            if (dialog.ShowDialog(window) != true) return;
            if (!ReferenceEquals(window, productionRunHistoryWindowS135)) return;
            window.SetBusy(true);
            try
            {
                await Task.Run(() => productionRunStoreS135.ExportCsv(dialog.FileName, records));
                if (ReferenceEquals(window, productionRunHistoryWindowS135)) window.SetStatus("已导出点击时的筛选快照 " + records.Count + " 条；不包含未加载范围，不执行或发送历史结果。" );
            }
            catch (Exception ex) { if (ReferenceEquals(window, productionRunHistoryWindowS135)) window.SetStatus("周期导出失败：" + ex.Message); }
            finally { if (ReferenceEquals(window, productionRunHistoryWindowS135)) window.SetBusy(false); }
        }
    }
}
