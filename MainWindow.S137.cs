using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly StationEditingProtectionServiceS137 stationProtectionS137;
        private readonly DispatcherTimer stationProtectionTimerS137 = new DispatcherTimer();
        private StationEditingProtectionWindowS137 stationProtectionWindowS137;
        private bool stationProtectionLoadingS137 = true, stationProtectionOperationBusyS137, stationProtectionClosingS137;
        private bool returnDeveloperAfterUnlockS137;
        private long displayedStationGenerationS137 = -1;
        private readonly List<VmApprovedStationRecipeS137> pendingApprovedRecipesS137 = new List<VmApprovedStationRecipeS137>();
        private string sealedProductionSessionS137;
        private readonly HashSet<string> sealedProductionFlowsS137 = new HashSet<string>(StringComparer.Ordinal);
        private bool approvedRecipeApplicationS137;
        private string sealedTcpConfigurationS137;
        private readonly Dictionary<TextBox, bool> originalProtectedReadOnlyS137 = new Dictionary<TextBox, bool>();

        private bool RequireEngineeringEditS137(string action)
        {
            if (!uiReady) return true; // Constructor-only initialization, not a user authorization bypass.
            if (engineeringPackageBusyS139) { HeaderStatusText.Text = "工程迁移任务正在处理，暂不能" + action + "；副本不修改活动方案，停止/取消仍可用。"; return false; }
            if (!stationProtectionClosingS137 && !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && stationProtectionS137.CanEdit) return true;
            string message = stationProtectionLoadingS137 ? "工位保护正在加载，暂不能" + action + "。" :
                stationProtectionOperationBusyS137 ? "工程保护操作正在处理，暂不能" + action + "。" :
                "工程编辑已锁定，不能" + action + "；原草稿保留。请从保护菜单、状态栏或返回开发入口显式解锁。";
            HeaderStatusText.Text = message;
            stationProtectionWindowS137?.SetStatus(message);
            return false;
        }

        private long CaptureEngineeringEditLeaseS137(string action)
        {
            if (!uiReady) return long.MaxValue;
            return RequireEngineeringEditS137(action) ? stationProtectionS137.CaptureLease() : -1;
        }

        private bool IsEngineeringEditLeaseCurrentS137(long lease, bool showStatus = true)
        {
            if (!uiReady && lease == long.MaxValue) return true;
            bool valid = !stationProtectionClosingS137 && !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && lease >= 0 && stationProtectionS137.IsLeaseCurrent(lease);
            if (!valid && showStatus)
            {
                HeaderStatusText.Text = "工程授权已锁定、到期或换代；本次修改未发布，原应用定义保留。";
                stationProtectionWindowS137?.SetStatus(HeaderStatusText.Text);
            }
            return valid;
        }

        private void SealConfiguredProductionScopeS137()
        {
            sealedProductionSessionS137 = projectSessionIdS104;
            sealedTcpConfigurationS137 = ReadConfiguredTcpIdentityS137();
            sealedProductionFlowsS137.Clear();
            foreach (var flow in projectFlowsS104.Where(flow => flow != null && !string.IsNullOrWhiteSpace(flow.FlowId)))
                sealedProductionFlowsS137.Add(flow.FlowId);
        }

        private string ReadConfiguredTcpIdentityS137()
        {
            // Length-prefixed fields avoid treating delimiter-containing host text as a different configuration.
            var fields = new[] { TcpClientModeRadio.IsChecked == true ? "Client" : "Server", TcpIpTextBox.Text ?? "",
                TcpPortTextBox.Text ?? "", GetTcpEncodingText(), TcpAppendNewLineCheckBox.IsChecked == true ? "1" : "0",
                AutoSendMatchResultCheckBox.IsChecked == true ? "1" : "0" };
            return string.Concat(fields.Select(value => value.Length + ":" + value));
        }

        private bool RequireConfiguredTcpConnectionS137(string action)
        {
            if (!uiReady) return true;
            if (stationProtectionClosingS137) return false;
            if (recipeChangeoverFaultedS134 || recipeChangeoverInProgressS134)
            { HeaderStatusText.Text = "方案换型尚未恢复，不能启用 TCP；工程解锁不会解除该故障。"; return false; }
            if (!stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && stationProtectionS137.CanEdit) return true;
            if (!RequireConfiguredProductionRunS137(VmFlowRunRequestMode.Full)) return false;
            if (!string.Equals(sealedTcpConfigurationS137, ReadConfiguredTcpIdentityS137(), StringComparison.Ordinal))
            { HeaderStatusText.Text = "TCP 当前设置不是工程封存的配置，不能" + action + "；请工程解锁后配置。"; return false; }
            if (!CanChangeRecipeS134(false))
            { HeaderStatusText.Text = "执行、换型或设备任务尚未结束，不能启动另一 TCP 连接。"; return false; }
            return true;
        }

        private bool CanSelectConfiguredFlowS137(string flowId)
        {
            if (!uiReady) return true;
            if (stationProtectionClosingS137) return false;
            if (!stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && stationProtectionS137.CanEdit) return true;
            var state = stationProtectionS137.Snapshot;
            bool allowed = !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && !state.IsFaulted && state.Enabled &&
                !dockDraftDirty && subflowEditorsS102.Count == 0 && sealedProductionSessionS137 == projectSessionIdS104 &&
                sealedProductionFlowsS137.Contains(flowId ?? "");
            if (!allowed) HeaderStatusText.Text = "当前不是已配置的根流程，或仍有待处理草稿；请工程解锁后切换。";
            return allowed;
        }

        private bool RequireConfiguredProductionRunS137(VmFlowRunRequestMode mode)
        {
            if (!uiReady) return true;
            if (engineeringPackageBusyS139) { HeaderStatusText.Text = "工程迁移任务正在处理，不接受新的运行；不会排队或重放。"; return false; }
            if (stationProtectionClosingS137) return false;
            if (recipeChangeoverFaultedS134 || recipeChangeoverInProgressS134)
            { HeaderStatusText.Text = "方案换型尚未完成或回退失败，禁止新运行；工程解锁不会恢复设备或解除故障。"; return false; }
            if (stationProtectionLoadingS137 || stationProtectionOperationBusyS137)
            { HeaderStatusText.Text = "工位保护正在加载或修改，暂不接受新的运行。"; return false; }
            var state = stationProtectionS137.Snapshot;
            RefreshStationProtectionUiS137(state);
            if (stationProtectionS137.CanEdit) return true;
            if (!state.IsFaulted && mode == VmFlowRunRequestMode.Full && subflowEditorsS102.Count == 0 &&
                !dockDraftDirty && sealedProductionSessionS137 == projectSessionIdS104 && sealedProductionFlowsS137.Contains(activeProjectFlowIdS104)) return true;
            HeaderStatusText.Text = state.IsFaulted ? "工位策略不可用，禁止新运行；停止和只读查询仍可用。" :
                "操作员仅可运行已配置的完整根流程；未应用草稿、内部调试或选中范围须先工程解锁。";
            return false;
        }

        private async void InitializeStationProtectionS137()
        {
            stationProtectionTimerS137.Interval = TimeSpan.FromMilliseconds(500);
            stationProtectionTimerS137.Tick += delegate { if (!stationProtectionClosingS137) RefreshStationProtectionUiS137(); };
            try { await Task.Run(() => stationProtectionS137.Reload()); }
            catch (Exception ex) { HeaderStatusText.Text = "工位保护加载失败：" + ex.Message; }
            finally
            {
                stationProtectionLoadingS137 = false;
                if (!stationProtectionClosingS137)
                {
                    SealConfiguredProductionScopeS137();
                    RefreshStationProtectionUiS137();
                    stationProtectionTimerS137.Start();
                }
            }
        }

        private static void SetProtectedContentEnabledS137(TabItem tab, bool enabled)
        {
            var content = tab?.Content as UIElement;
            if (content != null) content.IsEnabled = enabled;
        }

        private void SetEditingFieldsEnabledS137(DependencyObject scope, bool enabled)
        {
            if (scope == null) return;
            foreach (var node in EnumerateLogicalTree(scope, false))
            {
                var text = node as TextBox;
                if (text != null)
                {
                    bool original;
                    if (!originalProtectedReadOnlyS137.TryGetValue(text, out original))
                    { original = text.IsReadOnly; originalProtectedReadOnlyS137.Add(text, original); }
                    text.IsReadOnly = original || !enabled;
                }
                else if (node is ComboBox || node is CheckBox || node is RadioButton)
                    ((UIElement)node).IsEnabled = enabled;
            }
            // Apply commands have their own real guard. Buttons for Close/Cancel/view are never disabled here.
        }

        private void StationProtectionBadgeS137_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        { OpenStationProtectionS137_Click(sender, e); e.Handled = true; }

        private void RefreshStationProtectionUiS137(VmStationProtectionSnapshotS137 state = null)
        {
            if (!uiReady || stationProtectionClosingS137) return;
            state = state ?? stationProtectionS137.Snapshot;
            bool canEdit = !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && !state.IsFaulted &&
                (!state.Enabled || state.IsEngineeringAuthorized);
            bool changed = displayedStationGenerationS137 != state.Generation;
            if (changed)
            {
                InvalidateRecipeDiffS140("工位授权策略或状态已改变；旧审阅已失效，请重新准备目标。");
                displayedStationGenerationS137 = state.Generation;
                if (!state.IsFaulted) SealConfiguredProductionScopeS137();
                if (!canEdit)
                {
                    // End only uncommitted editing gestures; do not stop an accepted HALCON/communication operation.
                    if (!isFlowExecutionActive && !isImageQueueBatchRunning)
                    {
                        CancelFlowNodeDrag();
                        CancelFlowPortDrag("工程已锁定，未提交新的连接。", false);
                        FinishRoiGeometryEdit(false);
                    }
                    if (!stationProtectionLoadingS137 && !dockDraftDirty && CanChangeRecipeS134(false) && !productionLayoutDesignMode)
                        SetProductionRunMode(true);
                }
            }
            string label = stationProtectionLoadingS137 ? "工位保护加载中" : state.IsFaulted ? "工位保护异常 · 禁止编辑" :
                !state.Enabled ? "编辑保护未启用" : state.IsEngineeringAuthorized ? "工程已解锁" : "操作员 · 工程锁定";
            if (StationEditingProtectionBadgeS137 != null)
            {
                StationEditingProtectionBadgeS137.Text = label;
                StationEditingProtectionBadgeS137.ToolTip = state.Status + "\n切换页面不代表授权；停止始终可用。";
            }
            if (ReturnDeveloperModeButtonS137 != null) ReturnDeveloperModeButtonS137.Content = canEdit ? "← 工程编辑" : "工程解锁…";
            if (ProductionEngineeringImageCommandsS137 != null) ProductionEngineeringImageCommandsS137.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            if (ProductionStopImageBatchS137 != null) ProductionStopImageBatchS137.Visibility = isImageQueueBatchRunning ? Visibility.Visible : Visibility.Collapsed;
            SetProtectedContentEnabledS137(DockParametersTaskTab, canEdit);
            SetProtectedContentEnabledS137(DockRunSettingsTaskTab, canEdit);
            SetProtectedContentEnabledS137(DockIoTaskTab, canEdit);
            SetProtectedContentEnabledS137(DockRoiTaskTab, canEdit);
            if (ProductionLayoutWidgetListBox != null) ProductionLayoutWidgetListBox.IsEnabled = canEdit;
            if (ProductionResultEditorS141 != null) SetEditingFieldsEnabledS137(ProductionResultEditorS141, canEdit);
            SetProductionImageEditorEnabledS142(canEdit);
            if (AddProductionResultItemButtonS141 != null) AddProductionResultItemButtonS141.IsEnabled = canEdit && productionResultDraftRowsS141.Count < 8;
            if (RemoveProductionResultItemButtonS141 != null) RemoveProductionResultItemButtonS141.IsEnabled = canEdit && SelectedResultDraftS141() != null;
            SetEditingFieldsEnabledS137((GlobalVariableEditorTitleText?.Parent as FrameworkElement)?.Parent, canEdit);
            SetEditingFieldsEnabledS137((ExternalResourceEditorTitleText?.Parent as FrameworkElement)?.Parent, canEdit);
            SetEditingFieldsEnabledS137((ScriptInterfaceMemberIdText?.Parent as FrameworkElement)?.Parent, canEdit);
            foreach (var text in new[] { RecipeNameEditTextBox, ContinuousIntervalTextBox, FlowTimeoutTextBox,
                TcpIpTextBox, TcpPortTextBox, TcpSendTextBox, TcpTriggerCommandS104,
                TcpTriggerReturnOkS123, TcpTriggerReturnNgS123, TcpTriggerReturnErrorS123, TcpTriggerReturnAddressS123,
                TcpTriggerReturnTimeoutS123, TcpTriggerReturnHostS123, TcpTriggerReturnPortS123, TcpTriggerReturnUnitS123,
                TcpTriggerReturnComS123, TcpTriggerReturnBaudS123, TcpTriggerReturnStationS123 })
                if (text != null) text.IsReadOnly = !canEdit;
            foreach (var choice in new UIElement[] { StopOnNgCheckBox, TcpClientModeRadio, TcpServerModeRadio,
                TcpAppendNewLineCheckBox, AutoSendMatchResultCheckBox, TcpTriggerFlowPickerS104, TcpTriggerReturnModeS123,
                TcpTriggerReturnFunctionS123, TcpTriggerReturnParityS123, TcpTriggerReturnStopBitsS123 })
                if (choice != null) choice.IsEnabled = canEdit && !isFlowExecutionActive && !isContinuousRunning;
            if (TcpEncodingCombo != null) TcpEncodingCombo.IsEnabled = canEdit && !tcpService.IsRunning;
            if (stationProtectionWindowS137 != null)
            {
                var shown = state.Clone();
                if (!state.Enabled && pendingApprovedRecipesS137.Count > 0) shown.ApprovedRecipes = pendingApprovedRecipesS137.Select(recipe => recipe.Clone()).ToList();
                stationProtectionWindowS137.SetSnapshot(shown);
                stationProtectionWindowS137.SetBusy(stationProtectionLoadingS137 || stationProtectionOperationBusyS137);
            }
        }

        private void OpenStationProtectionS137_Click(object sender, RoutedEventArgs e)
        {
            if (stationProtectionClosingS137) return;
            if (stationProtectionWindowS137 != null) { stationProtectionWindowS137.Activate(); return; }
            var window = new StationEditingProtectionWindowS137 { Owner = this };
            stationProtectionWindowS137 = window;
            window.UnlockRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { PerformStationProtectionActionS137(args, "Unlock"); };
            window.EnableRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { PerformStationProtectionActionS137(args, "Enable"); };
            window.ConfigureRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { PerformStationProtectionActionS137(args, "Configure"); };
            window.DisableRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { PerformStationProtectionActionS137(args, "Disable"); };
            window.LockRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { LockStationEngineeringS137(args.ExpectedGeneration); };
            window.AddApprovedRecipeRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { AddApprovedStationRecipeS137(args); };
            window.RemoveApprovedRecipeRequested += delegate(object s, VmStationProtectionActionEventArgsS137 args) { RemoveApprovedStationRecipeS137(args); };
            window.Closed += delegate { if (ReferenceEquals(window, stationProtectionWindowS137)) stationProtectionWindowS137 = null; returnDeveloperAfterUnlockS137 = false; };
            RefreshStationProtectionUiS137();
            window.Show();
        }

        private bool MatchStationActionGenerationS137(VmStationProtectionActionEventArgsS137 args)
        {
            if (args == null || stationProtectionClosingS137 || stationProtectionLoadingS137 || stationProtectionOperationBusyS137 ||
                stationProtectionS137.Snapshot.Generation != args.ExpectedGeneration)
            { stationProtectionWindowS137?.SetStatus("工位授权状态已经变化或正在处理，请重新查看后操作。" ); return false; }
            return true;
        }

        private bool ConfirmStationEditingDraftsS137(string action)
        {
            if (!ConfirmDockDraftChanges(action)) return false;
            if (!productionLayoutDesignMode || !productionLayoutDraftDirty) return true;
            long lease = CaptureEngineeringEditLeaseS137("处理运行布局草稿");
            var choice = MessageBox.Show(this, "运行布局仍有未应用草稿。\n是：明确应用；否：明确取消布局草稿；取消：保留草稿，不继续" + action + "。",
                "处理布局草稿", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (choice == MessageBoxResult.Cancel) return false;
            if (choice == MessageBoxResult.No)
            { CancelProductionLayoutButton_Click(this, new RoutedEventArgs()); return !productionLayoutDraftDirty; }
            if (!IsEngineeringEditLeaseCurrentS137(lease)) return false;
            ApplyProductionLayoutButton_Click(this, new RoutedEventArgs());
            return !productionLayoutDraftDirty;
        }

        private async void PerformStationProtectionActionS137(VmStationProtectionActionEventArgsS137 args, string action)
        {
            if (!MatchStationActionGenerationS137(args)) return;
            if (action != "Unlock")
            {
                if (!RequireEngineeringEditS137("修改工位保护") || !CanChangeRecipeS134()) return;
                if (!ConfirmStationEditingDraftsS137("修改工位保护") || !MatchStationActionGenerationS137(args) || !CanChangeRecipeS134()) return;
            }
            var window = stationProtectionWindowS137;
            var approved = stationProtectionS137.Snapshot.Enabled ? stationProtectionS137.Snapshot.ApprovedRecipes.Select(recipe => recipe.Clone()).ToList()
                : pendingApprovedRecipesS137.Select(recipe => recipe.Clone()).ToList();
            string password = args.ActionPassword, newPassword = args.NewPassword;
            int minutes = args.UnlockMinutes;
            stationProtectionOperationBusyS137 = true;
            RefreshStationProtectionUiS137();
            try
            {
                var result = await Task.Run(() => action == "Unlock" ? stationProtectionS137.Unlock(password) :
                    action == "Enable" ? stationProtectionS137.Enable(newPassword, minutes, approved) :
                    action == "Configure" ? stationProtectionS137.Configure(newPassword, minutes, approved) : stationProtectionS137.Disable());
                if (stationProtectionClosingS137) return;
                if (result.Success && action != "Unlock") pendingApprovedRecipesS137.Clear();
                if (ReferenceEquals(window, stationProtectionWindowS137)) window?.SetStatus(result.Message);
                HeaderStatusText.Text = result.Message; // Service messages contain no password, salt or derived verification value.
                if (result.Success && returnDeveloperAfterUnlockS137 && stationProtectionS137.CanEdit)
                {
                    returnDeveloperAfterUnlockS137 = false;
                    stationProtectionOperationBusyS137 = false;
                    if (productionLayoutDesignMode)
                        HeaderStatusText.Text = "工程已解锁，运行布局草稿保留；请明确应用或取消后再返回开发。";
                    else SetProductionRunMode(false);
                    if (ReferenceEquals(window, stationProtectionWindowS137)) window?.Close();
                }
            }
            catch (Exception ex) { stationProtectionWindowS137?.SetStatus("工位操作未完成：" + ex.Message); }
            finally
            {
                password = newPassword = null;
                stationProtectionOperationBusyS137 = false;
                if (!stationProtectionClosingS137) RefreshStationProtectionUiS137();
            }
        }

        private void LockStationEngineeringS137(long expectedGeneration)
        {
            if (stationProtectionClosingS137 || stationProtectionLoadingS137 || stationProtectionOperationBusyS137 || stationProtectionS137.Snapshot.Generation != expectedGeneration) return;
            if (!ConfirmStationEditingDraftsS137("锁定工程编辑")) return;
            if (stationProtectionS137.Snapshot.Generation != expectedGeneration)
            { stationProtectionWindowS137?.SetStatus("草稿确认期间授权已经改变，未按旧状态执行锁定。" ); return; }
            SealConfiguredProductionScopeS137();
            var result = stationProtectionS137.Lock();
            HeaderStatusText.Text = result.Message;
            stationProtectionWindowS137?.SetStatus(result.Message);
            RefreshStationProtectionUiS137();
        }

        private void LockStationEngineeringS137_Click(object sender, RoutedEventArgs e)
        { LockStationEngineeringS137(stationProtectionS137.Snapshot.Generation); }

        private async void AddApprovedStationRecipeS137(VmStationProtectionActionEventArgsS137 args)
        {
            if (!MatchStationActionGenerationS137(args)) return;
            long lease = CaptureEngineeringEditLeaseS137("批准操作员换型方案");
            if (lease < 0 || !CanChangeRecipeS134()) return;
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "批准操作员可换型的实际方案", Filter = "视觉方案 (*.json)|*.json", CheckFileExists = true };
            if (dialog.ShowDialog(stationProtectionWindowS137 ?? (Window)this) != true || !IsEngineeringEditLeaseCurrentS137(lease)) return;
            string path = Path.GetFullPath(dialog.FileName);
            VmApprovedStationRecipeS137 approved;
            try
            {
                using (var file = OpenPreparedRecipeFileS134(path))
                {
                    string hash = HashRecipeFileS134(file);
                    var recipe = recipeService.LoadRecipe(path);
                    approved = new VmApprovedStationRecipeS137 { Path = path, FileSha256 = hash, Name = recipe.Name ?? Path.GetFileNameWithoutExtension(path) };
                    string error = approved.Validate(); if (error.Length != 0) throw new InvalidDataException(error);
                }
                if (!IsEngineeringEditLeaseCurrentS137(lease)) return;
                var state = stationProtectionS137.Snapshot;
                var values = state.Enabled ? state.ApprovedRecipes.Select(value => value.Clone()).ToList() : pendingApprovedRecipesS137.Select(value => value.Clone()).ToList();
                values.RemoveAll(value => string.Equals(value.Path, path, StringComparison.OrdinalIgnoreCase));
                if (values.Count >= 32) throw new InvalidOperationException("最多批准 32 个方案文件。" );
                values.Add(approved);
                if (!state.Enabled)
                {
                    pendingApprovedRecipesS137.Clear(); pendingApprovedRecipesS137.AddRange(values);
                    stationProtectionWindowS137?.SetStatus("已加入启用前的批准草稿；尚未写入站点策略，也未加载或运行方案。" );
                    RefreshStationProtectionUiS137(); return;
                }
                stationProtectionOperationBusyS137 = true; RefreshStationProtectionUiS137();
                var result = await Task.Run(() => stationProtectionS137.Configure("", state.UnlockMinutes, values));
                HeaderStatusText.Text = result.Message; stationProtectionWindowS137?.SetStatus(result.Message);
            }
            catch (Exception ex) { stationProtectionWindowS137?.SetStatus("未批准方案：" + ex.Message); }
            finally { stationProtectionOperationBusyS137 = false; RefreshStationProtectionUiS137(); }
        }

        private async void RemoveApprovedStationRecipeS137(VmStationProtectionActionEventArgsS137 args)
        {
            if (!MatchStationActionGenerationS137(args) || !RequireEngineeringEditS137("移除批准方案")) return;
            var state = stationProtectionS137.Snapshot;
            var values = state.Enabled ? state.ApprovedRecipes.Select(value => value.Clone()).ToList() : pendingApprovedRecipesS137.Select(value => value.Clone()).ToList();
            if (values.RemoveAll(value => string.Equals(value.Path, args.SelectedRecipePath, StringComparison.OrdinalIgnoreCase)) == 0) return;
            if (!state.Enabled)
            { pendingApprovedRecipesS137.Clear(); pendingApprovedRecipesS137.AddRange(values); RefreshStationProtectionUiS137(); return; }
            stationProtectionOperationBusyS137 = true; RefreshStationProtectionUiS137();
            try
            {
                var result = await Task.Run(() => stationProtectionS137.Configure("", state.UnlockMinutes, values));
                HeaderStatusText.Text = result.Message; stationProtectionWindowS137?.SetStatus(result.Message);
            }
            catch (Exception ex) { stationProtectionWindowS137?.SetStatus("批准状态未修改：" + ex.Message); }
            finally { stationProtectionOperationBusyS137 = false; RefreshStationProtectionUiS137(); }
        }

        private bool IsOperatorApprovedRecipeS137(string path, string fileSha, bool showStatus = true)
        {
            if (stationProtectionClosingS137) return false;
            if (!stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && stationProtectionS137.CanEdit) return true;
            var state = stationProtectionS137.Snapshot;
            bool allowed = !stationProtectionLoadingS137 && !stationProtectionOperationBusyS137 && !state.IsFaulted && state.Enabled &&
                !string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(fileSha) && state.ApprovedRecipes.Any(value =>
                    string.Equals(value.Path, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase) && string.Equals(value.FileSha256, fileSha, StringComparison.OrdinalIgnoreCase));
            if (!allowed && showStatus) HeaderStatusText.Text = "目标不是工程批准的同一路径 / 文件 SHA；操作员不能加载任意或已更改方案。";
            return allowed;
        }
    }
}
