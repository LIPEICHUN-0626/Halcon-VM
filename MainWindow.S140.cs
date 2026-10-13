using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private long recipeDiffGenerationS140;
        private bool recipeDiffWorkerBusyS140;
        private string recipeDiffCurrentIdentityS140, recipeDiffSessionS140, recipeDiffActiveFlowS140;
        private string recipeDiffTargetPathS140, recipeDiffTargetShaS140, recipeDiffTargetDefinitionS140, recipeDiffTargetFlowS140;
        private long recipeDiffStationGenerationS140 = -1;
        private bool recipeDiffHadDraftS140;
        private FileSystemWatcher recipeDiffFileWatcherS140;

        private void AttachRecipeDiffS140(RecipeChangeoverWindowS134 window)
        {
            window.DiffRequested += delegate { GenerateRecipeDiffS140(); };
            window.SetDiffBusyS140(recipeDiffWorkerBusyS140);
        }

        private void InvalidateRecipeDiffS140(string reason)
        {
            recipeDiffGenerationS140++;
            recipeDiffCurrentIdentityS140 = null;
            StopRecipeDiffFileWatcherS140();
            if (recipePreparationWindowS134 == null) return;
            recipePreparationWindowS134.InvalidateDiffReviewS140(reason);
            recipePreparationWindowS134.SetDiffBusyS140(recipeDiffWorkerBusyS140);
        }

        private void NotifyRecipeDiffDefinitionChangeS140()
        {
            // Existing definition-change notifications are conservative; they never approve or apply a report.
            if (recipeDiffCurrentIdentityS140 != null || recipeDiffWorkerBusyS140)
                InvalidateRecipeDiffS140("当前配置或显示设置已操作；旧审阅已失效，请重新生成。运行变量当前值不参与字段差异。");
        }

        private void RefreshRecipeDiffContextS140()
        {
            if (recipeDiffCurrentIdentityS140 == null || recipePreparationWindowS134 == null) return;
            if (recipeDiffSessionS140 != projectSessionIdS104 || recipeDiffActiveFlowS140 != activeProjectFlowIdS104 ||
                recipeDiffStationGenerationS140 != stationProtectionS137.Snapshot.Generation || recipeDiffHadDraftS140 != dockDraftDirty ||
                preparedCurrentRecipeS134 || recipeDiffTargetPathS140 != preparedRecipePathS134 ||
                recipeDiffTargetShaS140 != preparedRecipeShaS134 || recipeDiffTargetDefinitionS140 != preparedRecipeFingerprintS134 ||
                recipeDiffTargetFlowS140 != SelectedPreparedFlowS134())
                InvalidateRecipeDiffS140("方案会话、流程、草稿状态、目标或授权策略已变化；旧审阅不再代表当前配置。");
        }

        private bool IsRecipeDiffRequestCurrentS140(RecipeChangeoverWindowS134 window, long generation,
            string session, string activeFlow, long stationGeneration, bool hadDraft,
            string path, string sha, string definition, string targetFlow)
        {
            return generation == recipeDiffGenerationS140 && ReferenceEquals(window, recipePreparationWindowS134) &&
                !stationProtectionClosingS137 && session == projectSessionIdS104 && activeFlow == activeProjectFlowIdS104 &&
                stationGeneration == stationProtectionS137.Snapshot.Generation && !stationProtectionLoadingS137 &&
                !stationProtectionOperationBusyS137 && hadDraft == dockDraftDirty && !preparedCurrentRecipeS134 &&
                preparedRecipeS134 != null && path == preparedRecipePathS134 && sha == preparedRecipeShaS134 &&
                definition == preparedRecipeFingerprintS134 && targetFlow == SelectedPreparedFlowS134();
        }

        private async void GenerateRecipeDiffS140()
        {
            RecipeChangeoverWindowS134 window = recipePreparationWindowS134;
            if (window == null) return;
            if (recipeDiffWorkerBusyS140)
            { window.InvalidateDiffReviewS140("前一次有界审阅计算尚未退出；关闭仍可使用，不排队重复计算。"); return; }
            if (preparedRecipeS134 == null || preparedCurrentRecipeS134 || string.IsNullOrWhiteSpace(preparedRecipeShaS134))
            { InvalidateRecipeDiffS140("请先选择真实目标方案；查看当前准备不等于当前与目标的配置比较。"); return; }
            if (!CanChangeRecipeS134(false))
            { InvalidateRecipeDiffS140("运行、学习或文件任务尚未结束，请显式停止并等待后再捕获配置。查看不会强制终止任务。"); return; }

            InvalidateRecipeDiffS140("正在冻结当前已应用定义与目标；未应用草稿不进入比较。");
            long generation = recipeDiffGenerationS140;
            string session = projectSessionIdS104, activeFlow = activeProjectFlowIdS104;
            long stationGeneration = stationProtectionS137.Snapshot.Generation;
            bool hadDraft = dockDraftDirty;
            string path = preparedRecipePathS134, sha = preparedRecipeShaS134, definition = preparedRecipeFingerprintS134;
            string targetFlow = SelectedPreparedFlowS134();
            recipeDiffWorkerBusyS140 = true; window.SetDiffBusyS140(true);
            try
            {
                VisionRecipe current = FreezeRecipeDiffDefinitionS140(CaptureRecipe());
                VisionRecipe target = FreezeRecipeDiffDefinitionS140(preparedRecipeS134);
                VmRecipeDiffReportS140 report = await Task.Run(() =>
                {
                    using (var file = OpenPreparedRecipeFileS134(path))
                    {
                        if (!string.Equals(HashRecipeFileS134(file), sha, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("目标 JSON 自准备后已变化，请重新选择目标。");
                        return RecipeDiffServiceS140.Compare(current, target);
                    }
                });
                if (!IsRecipeDiffRequestCurrentS140(window, generation, session, activeFlow, stationGeneration, hadDraft,
                    path, sha, definition, targetFlow)) return;
                // Re-capture only on explicit publication, not by polling the active project every timer tick.
                VisionRecipe latest = FreezeRecipeDiffDefinitionS140(CaptureRecipe());
                string latestIdentity = await Task.Run(() => RecipeDiffServiceS140.ComputeReviewIdentity(latest));
                if (!IsRecipeDiffRequestCurrentS140(window, generation, session, activeFlow, stationGeneration, hadDraft,
                    path, sha, definition, targetFlow)) return;
                if (!string.Equals(latestIdentity, report.CurrentIdentity, StringComparison.Ordinal))
                    throw new InvalidOperationException("计算期间当前已应用定义或显示配置已变化，旧报告未发布。");
                // Start observing before the final file check; queued notifications invalidate this generation too.
                StartRecipeDiffFileWatcherS140(path, generation);
                bool sameFile = await Task.Run(() =>
                {
                    using (var file = OpenPreparedRecipeFileS134(path))
                        return string.Equals(HashRecipeFileS134(file), sha, StringComparison.OrdinalIgnoreCase);
                });
                if (!IsRecipeDiffRequestCurrentS140(window, generation, session, activeFlow, stationGeneration, hadDraft,
                    path, sha, definition, targetFlow)) return;
                if (!sameFile) throw new InvalidOperationException("发布前目标文件已变化，旧报告未发布，请重新准备。");

                recipeDiffCurrentIdentityS140 = report.CurrentIdentity;
                recipeDiffSessionS140 = session; recipeDiffActiveFlowS140 = activeFlow;
                recipeDiffStationGenerationS140 = stationGeneration; recipeDiffHadDraftS140 = hadDraft;
                recipeDiffTargetPathS140 = path; recipeDiffTargetShaS140 = sha;
                recipeDiffTargetDefinitionS140 = definition; recipeDiffTargetFlowS140 = targetFlow;
                var selectedFlow = window.TargetFlowPickerS134.SelectedItem as VmProjectFlowData;
                window.SetDiffReviewS140(report, "当前已应用定义 → " + Path.GetFileName(path) +
                    " · 目标流程：" + (selectedFlow == null ? "旧单流程" : selectedFlow.Name) +
                    (hadDraft ? "\n有未应用草稿：本报告不包含草稿，不会替您应用。" : "\n仅审阅定义，不证明算法/设备可用。") +
                    "\n目标文件 SHA：" + sha + " · 目标 FlowId：" + (targetFlow ?? LegacyFlowIdS134) +
                    " · 比较覆盖全部根流程与子流程，不仅是所选运行流程。");
            }
            catch (Exception error)
            {
                if (generation == recipeDiffGenerationS140 && ReferenceEquals(window, recipePreparationWindowS134))
                    InvalidateRecipeDiffS140("审阅未生成：" + error.Message + " 当前方案、参数草稿和设备未改变。");
            }
            finally
            {
                recipeDiffWorkerBusyS140 = false;
                if (recipePreparationWindowS134 != null) recipePreparationWindowS134.SetDiffBusyS140(false);
            }
        }

        private VisionRecipe FreezeRecipeDiffDefinitionS140(VisionRecipe recipe)
        {
            string json = recipeService.GetRecipeFingerprint(recipe);
            if (Encoding.UTF8.GetByteCount(json) > MaximumRecipeBytesS134)
                throw new InvalidOperationException("定义超过 64 MiB，不生成无界审阅。");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (VisionRecipe)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(VisionRecipe)).ReadObject(stream);
        }

        private void StartRecipeDiffFileWatcherS140(string path, long generation)
        {
            StopRecipeDiffFileWatcherS140();
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
            { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName, IncludeSubdirectories = false };
            FileSystemEventHandler changed = delegate { QueueRecipeDiffFileInvalidationS140(generation); };
            watcher.Changed += changed; watcher.Deleted += changed;
            watcher.Renamed += delegate { QueueRecipeDiffFileInvalidationS140(generation); };
            watcher.Error += delegate { QueueRecipeDiffFileInvalidationS140(generation); };
            recipeDiffFileWatcherS140 = watcher;
            watcher.EnableRaisingEvents = true;
        }

        private void QueueRecipeDiffFileInvalidationS140(long generation)
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (generation == recipeDiffGenerationS140 && recipePreparationWindowS134 != null)
                        InvalidateRecipeDiffS140("目标文件已收到变动通知或监视失效；旧审阅清空，请重新准备和生成。确认换型仍会重新锁定/核对文件。");
                }));
            }
            catch (InvalidOperationException) { }
        }

        private void StopRecipeDiffFileWatcherS140()
        {
            var watcher = recipeDiffFileWatcherS140; recipeDiffFileWatcherS140 = null;
            if (watcher != null) watcher.Dispose();
        }
    }
}
