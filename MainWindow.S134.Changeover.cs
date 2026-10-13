using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private RecipeChangeoverWindowS134 recipePreparationWindowS134;
        private VisionRecipe preparedRecipeS134;
        private string preparedRecipePathS134, preparedRecipeShaS134, preparedRecipeFingerprintS134;
        private bool preparedCurrentRecipeS134, updatingPreparationS134, recipeChangeoverInProgressS134;
        private bool recipeChangeoverFaultedS134;
        private long preparedStationGenerationS137 = -1;
        private const long MaximumRecipeBytesS134 = 67108864;
        private const string LegacyFlowIdS134 = "$legacy";

        private bool CanChangeRecipeS134(bool showStatus = true)
        {
            bool allowed = !engineeringPackageBusyS139 && !recipeChangeoverInProgressS134 && !isFlowExecutionActive && !isContinuousRunning &&
                !isImageQueueBatchRunning && !IsCurrentToolContinuousTrialRunningS87 && !subflowTrialActiveS102 &&
                variationTrainingCancellationS130 == null && nccTrainingCancellationS131 == null &&
                cameraBoardSolveCancellationS127 == null && System.Threading.Volatile.Read(ref tcpTriggerPendingS104) == 0;
            // Shape learning is synchronous on the UI thread; it cannot overlap this UI command.
            if (!allowed && showStatus)
                HeaderStatusText.Text = "当前流程、批次、通讯触发或后台学习尚未结束。请显式停止/取消并等待当前步骤退出，再切换方案；此处不强行终止任务。";
            return allowed;
        }

        private void OpenRecipePreparationS134_Click(object sender, RoutedEventArgs e)
        {
            if (recipePreparationWindowS134 != null) { recipePreparationWindowS134.Activate(); return; }
            recipePreparationWindowS134 = new RecipeChangeoverWindowS134 { Owner = this };
            AttachRecipeDiffS140(recipePreparationWindowS134);
            recipePreparationWindowS134.PreviewCurrentRequested += delegate { PreviewCurrentRecipeS134(); };
            recipePreparationWindowS134.BrowseRequested += delegate
            {
                InvalidateRecipeDiffS140("正在选择目标，旧审阅已清空；取消也不复用旧计算。");
                BrowsePreparedRecipeS134();
            };
            recipePreparationWindowS134.TargetFlowChanged += delegate { if (!updatingPreparationS134) RefreshPreparedReportS134(); };
            recipePreparationWindowS134.ApplyRequested += delegate { ApplyPreparedRecipeS134(); };
            recipePreparationWindowS134.LocateRequested += delegate { LocatePreparedIssueS134(); };
            recipePreparationWindowS134.Closed += delegate
            {
                ClearPreparedRecipeS134(); recipePreparationWindowS134 = null;
            };
            recipePreparationWindowS134.Show();
            PreviewCurrentRecipeS134();
        }

        private void ClearPreparedRecipeS134()
        {
            InvalidateRecipeDiffS140("目标身份已清空；选择目标后可按需生成配置审阅。");
            preparedRecipeS134 = null; preparedRecipePathS134 = preparedRecipeShaS134 = preparedRecipeFingerprintS134 = null;
            preparedCurrentRecipeS134 = false;
            preparedStationGenerationS137 = -1;
            if (recipePreparationWindowS134 == null) return;
            updatingPreparationS134 = true;
            try
            {
                recipePreparationWindowS134.TargetPathTextS134.Text = "尚未选择目标方案";
                recipePreparationWindowS134.TargetFlowPickerS134.ItemsSource = null;
                recipePreparationWindowS134.IssuesListS134.ItemsSource = null;
                recipePreparationWindowS134.ApplyButtonS134.IsEnabled = false;
            }
            finally { updatingPreparationS134 = false; }
        }

        private void SetPreparationSnapshotS134(VisionRecipe recipe, string path, string hash, bool current, long stationGenerationS137 = -1)
        {
            preparedRecipeS134 = recipe; preparedRecipePathS134 = path; preparedRecipeShaS134 = hash;
            preparedRecipeFingerprintS134 = recipeService.GetRecipeFingerprint(recipe); preparedCurrentRecipeS134 = current;
            preparedStationGenerationS137 = stationGenerationS137 >= 0 ? stationGenerationS137 : stationProtectionS137.Snapshot.Generation;
            updatingPreparationS134 = true;
            try
            {
                recipePreparationWindowS134.TargetPathTextS134.Text = current
                    ? "当前编辑配置（未切换；不应用快照）" + (string.IsNullOrWhiteSpace(path) ? " · 尚未保存" : " · " + path)
                    : path;
                var flows = recipe.Flows == null
                    ? new List<VmProjectFlowData> { new VmProjectFlowData { FlowId = LegacyFlowIdS134, Name = "流程 01（旧单流程）" } }
                    : recipe.Flows;
                recipePreparationWindowS134.TargetFlowPickerS134.ItemsSource = flows;
                recipePreparationWindowS134.TargetFlowPickerS134.SelectedValue = recipe.Flows == null ? LegacyFlowIdS134
                    : flows.Any(flow => flow.FlowId == recipe.ActiveFlowId) ? recipe.ActiveFlowId : flows.First().FlowId;
            }
            finally { updatingPreparationS134 = false; }
            RefreshPreparedReportS134();
        }

        private void PreviewCurrentRecipeS134()
        {
            if (recipePreparationWindowS134 == null) return;
            ClearPreparedRecipeS134();
            try
            {
                if (!CanChangeRecipeS134()) throw new InvalidOperationException(HeaderStatusText.Text);
                // CaptureRootFlowRecipeS102 preserves child-editor edits without applying a parameter draft or changing the editor.
                SetPreparationSnapshotS134(CaptureRecipe(), currentRecipePath, null, true);
            }
            catch (Exception ex) { ClearPreparedRecipeS134(); recipePreparationWindowS134.SummaryTextS134.Text = "当前配置准备失败：" + ex.Message; }
        }

        private void BrowsePreparedRecipeS134()
        {
            if (recipePreparationWindowS134 == null) return;
            long stationGenerationS137 = stationProtectionS137.Snapshot.Generation;
            if (!IsPreparedStationGenerationCurrentS137(stationGenerationS137)) return;
            var dialog = new Microsoft.Win32.OpenFileDialog
            { Title = "选择待换型方案（先准备，不会立即加载）", Filter = "视觉方案 (*.json)|*.json", CheckFileExists = true, InitialDirectory = GetRecipeDialogDirectory() };
            if (dialog.ShowDialog(recipePreparationWindowS134) != true) return;
            ClearPreparedRecipeS134();
            try
            {
                if (!IsPreparedStationGenerationCurrentS137(stationGenerationS137))
                    throw new InvalidOperationException(HeaderStatusText.Text);
                string path = Path.GetFullPath(dialog.FileName);
                using (var file = OpenPreparedRecipeFileS134(path))
                {
                    string hash = HashRecipeFileS134(file);
                    if (!IsPreparedChangeoverAuthorizedS137(stationGenerationS137, path, hash))
                        throw new InvalidOperationException(HeaderStatusText.Text);
                    VisionRecipe recipe = recipeService.LoadRecipe(path);
                    if (!IsPreparedChangeoverAuthorizedS137(stationGenerationS137, path, hash))
                        throw new InvalidOperationException(HeaderStatusText.Text);
                    SetPreparationSnapshotS134(recipe, path, hash, false, stationGenerationS137);
                }
            }
            catch (Exception ex)
            {
                ClearPreparedRecipeS134(); recipePreparationWindowS134.SummaryTextS134.Text = "目标未准备：" + ex.Message + "。原方案未加载、未运行；请重新选择目标。";
            }
        }

        private string SelectedPreparedFlowS134()
        {
            return preparedRecipeS134 == null || preparedRecipeS134.Flows == null ? null : recipePreparationWindowS134.TargetFlowPickerS134.SelectedValue as string;
        }

        private bool IsPreparedStationGenerationCurrentS137(long generation, bool showStatus = true)
        {
            if (!uiReady) return true;
            bool current = !stationProtectionClosingS137 && !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 &&
                generation >= 0 && stationProtectionS137.Snapshot.Generation == generation;
            if (!current && showStatus) HeaderStatusText.Text = "工位授权已变化或正在处理；旧目标准备身份失效，请重新选择并准备。";
            return current;
        }

        private bool IsPreparedChangeoverAuthorizedS137(long generation, string path, string hash, bool showStatus = true)
        {
            return IsPreparedStationGenerationCurrentS137(generation, showStatus) && IsOperatorApprovedRecipeS137(path, hash, showStatus);
        }

        private void RefreshPreparedReportS134()
        {
            if (recipePreparationWindowS134 == null || preparedRecipeS134 == null) return;
            InvalidateRecipeDiffS140("目标或所选流程已准备/改变；原审阅不复用，请按需生成当前 → 目标。");
            try
            {
                string flowId = SelectedPreparedFlowS134();
                if (preparedRecipeS134.Flows != null && !preparedRecipeS134.Flows.Any(flow => flow.FlowId == flowId))
                    throw new InvalidOperationException("目标流程已缺失，请重新选择。" );
                var report = RecipeReadinessServiceS134.Evaluate(preparedRecipeS134, flowId);
                recipePreparationWindowS134.CurrentTextS134.Text = "当前：" + RecipeNameEditTextBox.Text + " / " + GetProjectFlowNameS104() +
                    (isRecipeDirty ? " · 有未保存修改" : "") + (dockDraftDirty ? " · 有未应用草稿" : "");
                recipePreparationWindowS134.SummaryTextS134.Text = report.SummaryText +
                    (preparedCurrentRecipeS134 ? "\n当前配置只读快照；修改后请重新准备。" : "\n目标尚未加载；静态检查不保证算法/设备运行就绪。应用前会复核文件和全部目标定义。" );
                recipePreparationWindowS134.IssuesListS134.ItemsSource = report.Issues;
                recipePreparationWindowS134.ApplyButtonS134.IsEnabled = !preparedCurrentRecipeS134 && report.CanApply &&
                    IsPreparedChangeoverAuthorizedS137(preparedStationGenerationS137, preparedRecipePathS134, preparedRecipeShaS134, false);
            }
            catch (Exception ex)
            {
                ClearPreparedRecipeS134(); recipePreparationWindowS134.SummaryTextS134.Text = "准备失败：" + ex.Message + "；旧目标应用身份已清空。";
            }
        }

        private void ApplyPreparedRecipeS134()
        {
            if (recipePreparationWindowS134 == null || preparedRecipeS134 == null || preparedCurrentRecipeS134) return;
            if (!CanChangeRecipeS134()) { recipePreparationWindowS134.SummaryTextS134.Text = HeaderStatusText.Text; return; }
            RecipeChangeoverWindowS134 window = recipePreparationWindowS134;
            string path = preparedRecipePathS134, hash = preparedRecipeShaS134, definition = preparedRecipeFingerprintS134;
            string flowId = SelectedPreparedFlowS134();
            long stationGenerationS137 = preparedStationGenerationS137;
            try
            {
                if (!IsPreparedChangeoverAuthorizedS137(stationGenerationS137, path, hash))
                    throw new InvalidOperationException(HeaderStatusText.Text);
                // Close the one parameter editor and return through the existing child-editor path before any confirmation/capture.
                if (!PrepareProjectFlowChangeS104() || !ConfirmUnsavedRecipeChanges("应用目标方案")) return;
                InvalidateRecipeDiffS140("换型确认已处理草稿或当前状态；旧审阅不作为换型授权，继续原完整目标复核。");
                if (!ReferenceEquals(window, recipePreparationWindowS134) || preparedRecipeS134 == null) return;
                if (!IsPreparedChangeoverAuthorizedS137(stationGenerationS137, path, hash))
                    throw new InvalidOperationException(HeaderStatusText.Text);
                if (preparedCurrentRecipeS134 || preparedRecipePathS134 != path || preparedRecipeShaS134 != hash ||
                    preparedRecipeFingerprintS134 != definition || SelectedPreparedFlowS134() != flowId)
                { window.SummaryTextS134.Text = "确认期间目标选择已改变，未换型；请重新确认当前预览。"; return; }
                using (var file = OpenPreparedRecipeFileS134(path))
                {
                    string lockedHashS137 = HashRecipeFileS134(file);
                    if (!string.Equals(lockedHashS137, hash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("目标 JSON 自准备后已变化，请重新准备，不能应用旧清单。" );
                    if (!IsPreparedChangeoverAuthorizedS137(stationGenerationS137, path, lockedHashS137))
                        throw new InvalidOperationException(HeaderStatusText.Text);
                    VisionRecipe fresh = recipeService.LoadRecipe(path);
                    if (!string.Equals(recipeService.GetRecipeFingerprint(fresh), definition, StringComparison.Ordinal))
                        throw new InvalidOperationException("目标完整定义已变化，请重新准备。" );
                    if (fresh.Flows != null && !fresh.Flows.Any(flow => flow.FlowId == flowId))
                        throw new InvalidOperationException("所选目标流程不存在，请重新准备。" );
                    var report = RecipeReadinessServiceS134.Evaluate(fresh, flowId);
                    recipePreparationWindowS134.IssuesListS134.ItemsSource = report.Issues;
                    if (!report.CanApply) throw new InvalidOperationException("目标静态准备存在阻断，未应用：" + report.SummaryText);
                    if (!CanChangeRecipeS134()) throw new InvalidOperationException(HeaderStatusText.Text);
                    if (!IsPreparedChangeoverAuthorizedS137(stationGenerationS137, path, lockedHashS137))
                        throw new InvalidOperationException(HeaderStatusText.Text);
                    bool applied = LoadRecipeTransactionalS134(fresh, path, flowId, operatorApprovedChangeoverS137: true);
                    ClearPreparedRecipeS134();
                    if (ReferenceEquals(window, recipePreparationWindowS134))
                        window.SummaryTextS134.Text = HeaderStatusText.Text + (applied ? string.Empty : "\n目标应用身份已清空；修复后请重新准备。" );
                }
            }
            catch (Exception ex)
            {
                ClearPreparedRecipeS134(); HeaderStatusText.Text = "未应用目标：" + ex.Message;
                if (recipePreparationWindowS134 != null) recipePreparationWindowS134.SummaryTextS134.Text = HeaderStatusText.Text;
                AppendAlarm(HeaderStatusText.Text);
            }
        }

        // Normal Open can load an incomplete engineering definition for repair. Only the explicit changeover path gates on readiness.
        private bool LoadRecipeTransactionalS134(VisionRecipe recipe, string path, string flowId = null, bool recovery = false, bool operatorApprovedChangeoverS137 = false)
        {
            if (recipe == null) throw new ArgumentNullException("recipe");
            long editLeaseS137 = operatorApprovedChangeoverS137 ? -1 : CaptureEngineeringEditLeaseS137("打开或恢复任意方案定义");
            if (!operatorApprovedChangeoverS137 && editLeaseS137 < 0) return false;
            if (operatorApprovedChangeoverS137 && !IsPreparedChangeoverAuthorizedS137(preparedStationGenerationS137, path, preparedRecipeShaS134)) return false;
            if (!CanChangeRecipeS134() || !PrepareProjectFlowChangeS104()) return false;
            if (!CanChangeRecipeS134()) return false; // Confirmation dialogs can pump the dispatcher and start a queued trigger.
            string fullPath = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
            if (!recovery && fullPath == null) throw new ArgumentException("正式方案路径不能为空。", "path");
            if (operatorApprovedChangeoverS137)
            {
                // This narrow grant belongs only to the still-locked, fully prepared file in ApplyPreparedRecipeS134.
                if (recovery || preparedCurrentRecipeS134 || preparedRecipeS134 == null ||
                    !string.Equals(fullPath, preparedRecipePathS134, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(recipeService.GetRecipeFingerprint(recipe), preparedRecipeFingerprintS134, StringComparison.Ordinal) ||
                    flowId != SelectedPreparedFlowS134() ||
                    !IsPreparedChangeoverAuthorizedS137(preparedStationGenerationS137, fullPath, preparedRecipeShaS134)) return false;
            }
            else if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return false;
            VisionRecipe previous = CaptureRecipe();
            string oldPath = currentRecipePath, oldFingerprint = savedRecipeFingerprint;
            DateTime? oldSavedAt = lastRecipeSavedAt;
            bool oldDirty = isRecipeDirty, oldTracking = recipeTrackingSuspended, oldFaulted = recipeChangeoverFaultedS134;
            string oldDetail = RecipeWorkspaceStatusText == null ? null : RecipeWorkspaceStatusText.Text;
            RecipeRecoveryData oldRecovery = pendingRecovery;
            var oldRecent = recentRecipes.ToList();
            VisionRecipe target = CloneRecipeDefinitionS134(recipe);
            string originalFlow = target.Flows == null ? null : target.Flows.Any(flow => flow.FlowId == target.ActiveFlowId)
                ? target.ActiveFlowId : target.Flows.First().FlowId;
            bool selectedDifferentFlow = target.Flows != null && flowId != null && flowId != originalFlow;
            if (flowId != null)
            {
                if (target.Flows == null || !target.Flows.Any(flow => flow.FlowId == flowId)) throw new InvalidOperationException("目标流程不存在。");
                target.ActiveFlowId = flowId;
            }
            var changeoverAlarmContextS136 = FreezeProductionAlarmS136(VmProductionAlarmSourceS136.ChangeoverDoubleFailure,
                VmProductionAlarmSeverityS136.Error, "换型和配置回退均失败", "", null, null, null);
            recipeChangeoverInProgressS134 = true; recipeTrackingSuspended = true; recipeAutosaveTimer.Stop();
            try
            {
                DisarmTcpTriggerS104("开始换型；触发与 TCP 必须手动重新启用");
                tcpService.Stop();
                ClearChangeoverRuntimeS134();
                if (operatorApprovedChangeoverS137 ? !IsPreparedChangeoverAuthorizedS137(preparedStationGenerationS137, fullPath, preparedRecipeShaS134) :
                    !IsEngineeringEditLeaseCurrentS137(editLeaseS137)) throw new OperationCanceledException("工程授权或批准换型身份已改变，目标未应用。" );
                bool oldApplicationGrantS137 = approvedRecipeApplicationS137;
                try { approvedRecipeApplicationS137 = true; ApplyRecipe(target); }
                finally { approvedRecipeApplicationS137 = oldApplicationGrantS137; }
                ClearChangeoverRuntimeS134();
                // Publish formal identity only after every definition application step has completed.
                currentRecipePath = fullPath; recipeChangeoverFaultedS134 = false;
                VisionRecipe appliedBaseline = CaptureRecipe();
                savedRecipeFingerprint = recipeService.GetRecipeFingerprint(appliedBaseline);
                lastRecipeSavedAt = fullPath != null && File.Exists(fullPath) ? (DateTime?)File.GetLastWriteTime(fullPath) : null;
                SetRecipeDirtyState(false, "目标定义已加载；原 JSON 未改写");
                if (recovery)
                {
                    savedRecipeFingerprint = "__RECOVERED_REQUIRES_SAVE__";
                    SetRecipeDirtyState(true, "恢复副本已加载；正式方案文件未改写，请检查后保存");
                }
                else if (selectedDifferentFlow)
                {
                    VisionRecipe fileBaseline = CaptureRecipe(); fileBaseline.ActiveFlowId = originalFlow;
                    savedRecipeFingerprint = recipeService.GetRecipeFingerprint(fileBaseline);
                    SetRecipeDirtyState(true, "已选择目标流程，未改写原 JSON；保存可保留该流程选择");
                }
                if (fullPath != null) AddRecentRecipe(fullPath);
                SaveLayoutState();
                pendingRecovery = null;
                // Recovery housekeeping occurs after the definition and formal baseline have committed.
                try { recipeService.DeleteRecovery(); }
                catch (Exception cleanupError) { AppendAlarm("目标已加载，但旧恢复副本清理失败：" + cleanupError.Message); }
                RefreshRecoveryWorkspace();
                SealConfiguredProductionScopeS137();
                LogInfo("方案已事务加载：" + (fullPath ?? "未落盘恢复副本") + " / " + GetProjectFlowNameS104() + "；未运行、未连接设备、未外发。" );
                HeaderStatusText.Text = "已加载：" + RecipeNameEditTextBox.Text + " / " + GetProjectFlowNameS104() + "。图像与判定已清空；请检查后手动连接设备、启用触发或运行。";
                return true;
            }
            catch (Exception ex)
            {
                Exception rollbackError = null;
                try
                {
                    bool oldApplicationGrantS137 = approvedRecipeApplicationS137;
                    try { approvedRecipeApplicationS137 = true; ApplyRecipe(previous); }
                    finally { approvedRecipeApplicationS137 = oldApplicationGrantS137; }
                    ClearChangeoverRuntimeS134(); recipeChangeoverFaultedS134 = oldFaulted;
                    SealConfiguredProductionScopeS137();
                }
                catch (Exception restoreError) { rollbackError = restoreError; recipeChangeoverFaultedS134 = true; }
                finally
                {
                    currentRecipePath = oldPath; savedRecipeFingerprint = oldFingerprint; lastRecipeSavedAt = oldSavedAt; pendingRecovery = oldRecovery;
                    recentRecipes.Clear(); foreach (var item in oldRecent) recentRecipes.Add(item);
                    if (RecentRecipeList != null) RecentRecipeList.Items.Refresh();
                    SetRecipeDirtyState(oldDirty, oldDetail);
                    DisarmTcpTriggerS104("换型失败；触发保持停用，需人工核对");
                    tcpService.Stop();
                    lastResultPayload = null;
                }
                if (rollbackError != null)
                {
                    try { ClearChangeoverRuntimeS134(); }
                    catch (Exception cleanupError) { AppendAlarm("双失败后的运行缓存清理异常：" + cleanupError.Message); }
                }
                try { SaveLayoutState(); }
                catch (Exception stateError) { AppendAlarm("配置回退后的本机布局保存失败：" + stateError.Message); }
                if (rollbackError == null)
                    HeaderStatusText.Text = "换型失败，原方案配置已恢复：" + ex.Message + "。图像、执行句柄、生产统计与设备连接不恢复；触发保持停用。";
                else
                    HeaderStatusText.Text = "换型和配置回退均失败，当前配置禁止运行。目标错误：" + ex.Message + "；回退错误：" + rollbackError.Message + "。请重新打开可用方案。";
                if (rollbackError != null)
                {
                    changeoverAlarmContextS136.OccurredAtUtc = DateTime.UtcNow;
                    changeoverAlarmContextS136.Detail = HeaderStatusText.Text + "\n换型前方案：" + (oldPath ?? "未落盘") + "；目标：" + (fullPath ?? "恢复副本") +
                        "。身份字段是换型前定义，不把失败后的混合配置冒充已应用目标。";
                    PublishFrozenProductionAlarmS136(changeoverAlarmContextS136);
                }
                AppendAlarm(HeaderStatusText.Text); return false;
            }
            finally
            {
                recipeTrackingSuspended = oldTracking; recipeChangeoverInProgressS134 = false;
                RefreshUiState(); RefreshProductionRunView(); ScheduleRefreshDisplay();
            }
        }

        private void ClearChangeoverRuntimeS134()
        {
            InvalidateProductionResultS141("换型已清理旧周期值；不恢复旧测量或旧设备结果。");
            ExitHistoricalImageMode(false); ClearHistoricalSessionState();
            ClearInputRepairNavigationS96(); ClearSubflowTraceS102(); ClearShapeTrainingPreviewS89(); ClearNccPreviewS131();
            ClearImageQueue(true); CancelRoiGeometryDraft(false); ClearPendingRoi();
            foreach (var tool in flowTools) { ClearOcrCharacterViewsS133(tool); InvalidateToolRunResult(tool, "方案已切换，旧结果清空"); }
            currentMatches.Clear(); DisposeToolOverlays(); ResetFlowDebugRuntimeState(true); ResetActiveFlowPresentationS104();
            lastResultPayload = null;
            SetProductionRunSaveStatusS135("周期记录：本方案尚未运行；已有历史只读保留");
        }

        private VisionRecipe CloneRecipeDefinitionS134(VisionRecipe recipe)
        {
            string json = recipeService.GetRecipeFingerprint(recipe);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (VisionRecipe)new DataContractJsonSerializer(typeof(VisionRecipe)).ReadObject(stream);
        }

        private static FileStream OpenPreparedRecipeFileS134(string path)
        {
            var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 1 || stream.Length > MaximumRecipeBytesS134)
            { stream.Dispose(); throw new InvalidOperationException("目标方案 JSON 为空或超过 64 MiB，不加载无界定义。" ); }
            return stream;
        }

        private static string HashRecipeFileS134(FileStream stream)
        {
            stream.Position = 0;
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                stream.Position = 0; return hash;
            }
        }

        private void LocatePreparedIssueS134()
        {
            if (recipePreparationWindowS134 == null || preparedRecipeS134 == null) return;
            var issue = recipePreparationWindowS134.IssuesListS134.SelectedItem as VmRecipeReadinessIssueS134;
            if (issue == null) return;
            if (!preparedCurrentRecipeS134)
            {
                recipePreparationWindowS134.SummaryTextS134.Text = "目标问题：" + issue.DisplayText + " · " + issue.Reason + "。目标尚未加载；本入口不会偷偷切换。可通过普通“打开方案”进入修复，不能用阻断清单直接换型。";
                return;
            }
            if (!CanChangeRecipeS134() || !PrepareProjectFlowChangeS104()) return;
            if (!projectFlowsS104.Any(flow => flow.FlowId == issue.FlowId))
            { recipePreparationWindowS134.SummaryTextS134.Text = "该快照流程已变化，请重新准备当前配置。"; return; }
            if (issue.FlowId != activeProjectFlowIdS104) SwitchProjectFlowS104(issue.FlowId);
            if (issue.FlowId != activeProjectFlowIdS104) return;
            var path = FindIssueToolPathS134(CaptureRootFlowRecipeS102(), issue.ToolId);
            if (path == null) { HeaderStatusText.Text = "该问题属于方案/资源或模块已变化：" + issue.Reason; return; }
            foreach (string id in path)
            {
                var tool = flowTools.FirstOrDefault(item => item.ToolId == id);
                if (tool == null) { HeaderStatusText.Text = "模块已变化，请重新准备当前配置。"; return; }
                FlowToolList.SelectedItem = tool; FlowToolList.ScrollIntoView(tool);
                if (id != issue.ToolId) EnterSubflowEditorS102(tool);
                else { LoadDockConfigurationDraft(tool); OpenCentralModuleConfigurationS77(tool); }
            }
            HeaderStatusText.Text = "已定位：" + issue.DisplayText;
        }

        private static List<string> FindIssueToolPathS134(IEnumerable<ToolFlowRecipeItem> tools, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || tools == null) return null;
            foreach (var tool in tools)
            {
                if (tool.ToolId == id) return new List<string> { id };
                var child = tool.Parameters == null ? null : tool.Parameters.Subflow;
                var path = child == null ? null : FindIssueToolPathS134(child.Tools, id);
                if (path != null) { path.Insert(0, tool.ToolId); return path; }
            }
            return null;
        }
    }
}
