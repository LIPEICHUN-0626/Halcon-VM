using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public sealed class VmStationProtectionActionEventArgsS137 : EventArgs
    {
        public string ActionPassword { get; private set; }
        public string NewPassword { get; private set; }
        public int UnlockMinutes { get; private set; }
        public string SelectedRecipePath { get; private set; }
        public long ExpectedGeneration { get; private set; }
        public VmStationProtectionActionEventArgsS137(string actionPassword, string newPassword, int unlockMinutes, string selectedRecipePath, long expectedGeneration)
        { ActionPassword = actionPassword ?? string.Empty; NewPassword = newPassword ?? string.Empty; UnlockMinutes = unlockMinutes; SelectedRecipePath = selectedRecipePath ?? string.Empty; ExpectedGeneration = expectedGeneration; }
    }

    // Sends explicit, generation-frozen requests only. No credentials, policies or authorization are persisted here.
    public partial class StationEditingProtectionWindowS137 : Window
    {
        private VmStationProtectionSnapshotS137 snapshot;
        private bool busy, updating = true;
        private sealed class MinutesOption { public int Minutes { get; set; } public string Label { get; set; } }
        public event EventHandler<VmStationProtectionActionEventArgsS137> UnlockRequested;
        public event EventHandler<VmStationProtectionActionEventArgsS137> EnableRequested;
        public event EventHandler<VmStationProtectionActionEventArgsS137> ConfigureRequested;
        public event EventHandler<VmStationProtectionActionEventArgsS137> DisableRequested;
        public event EventHandler<VmStationProtectionActionEventArgsS137> LockRequested;
        public event EventHandler<VmStationProtectionActionEventArgsS137> AddApprovedRecipeRequested;
        public event EventHandler<VmStationProtectionActionEventArgsS137> RemoveApprovedRecipeRequested;

        public StationEditingProtectionWindowS137()
        {
            InitializeComponent(); SetMinutes(30); updating = false; UpdateActions();
            Closed += delegate { ClearPasswords(); snapshot = null; };
        }
        public void SetSnapshot(VmStationProtectionSnapshotS137 state)
        {
            if (state == null) { snapshot = null; ClearPasswords(); ProtectionStateS137.Text = "无法读取工位状态 · 编辑保护状态未知"; UpdateActions(); return; }
            bool changed = snapshot == null || snapshot.Generation != state.Generation;
            string selectedPath = (ApprovedRecipesS137.SelectedItem as VmApprovedStationRecipeS137)?.Path;
            snapshot = state.Clone();
            if (changed) { ClearPasswords(); SetMinutes(snapshot.UnlockMinutes); }
            updating = true;
            try
            {
                var approved = (snapshot.ApprovedRecipes ?? new List<VmApprovedStationRecipeS137>()).Where(item => item != null).Select(item => item.Clone()).ToList();
                ApprovedRecipesS137.ItemsSource = approved;
                ApprovedRecipesS137.SelectedItem = approved.FirstOrDefault(item => string.Equals(item.Path, selectedPath, StringComparison.OrdinalIgnoreCase));
                ApprovedSummaryS137.Text = "当前批准 " + approved.Count + " / 32 个实际 JSON 文件；名称不代表批准，主窗口复核 SHA 和路径。";
            }
            finally { updating = false; }
            ProtectionStateS137.Text = snapshot.IsFaulted ? "保护策略异常 · 受保护修改不可用" : !snapshot.Enabled ? "未启用编辑保护" : snapshot.IsEngineeringAuthorized ? "工程已解锁" : "操作员 · 工程锁定";
            ProtectionDetailS137.Text = snapshot.Status + (snapshot.UnlockedUntilUtc.HasValue ? "\n授权至本机时间 " + snapshot.UnlockedUntilUtc.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) : string.Empty) +
                (snapshot.RetryAfterSeconds > 0 ? "\n解锁失败节流：请等待 " + snapshot.RetryAfterSeconds + " 秒后重试。" : string.Empty) +
                (!snapshot.Enabled && !snapshot.IsFaulted ? "\n沿用现有开发行为，不等于已认证；没有默认口令。" : string.Empty);
            GenerationTextS137.Text = "当前显示的策略/会话代际：" + snapshot.Generation + "。操作冻结此代际，过期请求由主窗口拒绝。";
            if (changed) SetStatus("工位状态已更新，口令输入已清空；请按当前显示状态明确操作。");
            UpdateActions();
        }
        public void SetStatus(string text) { StatusTextS137.Text = text ?? string.Empty; }
        public void SetBusy(bool value) { busy = value; UpdateActions(); }
        private void SetMinutes(int minutes)
        {
            var options = new[] { 5, 15, 30, 60, 120 }.ToList();
            if (minutes >= 5 && minutes <= 120 && !options.Contains(minutes)) options.Add(minutes);
            UnlockMinutesS137.ItemsSource = options.OrderBy(value => value).Select(value => new MinutesOption { Minutes = value, Label = value + " 分钟" }).ToList();
            UnlockMinutesS137.SelectedValue = minutes >= 5 && minutes <= 120 ? minutes : 30;
        }
        private int SelectedMinutes() { return UnlockMinutesS137.SelectedValue is int ? (int)UnlockMinutesS137.SelectedValue : 30; }
        private bool CanConfigure() { return !busy && snapshot != null && !snapshot.IsFaulted && (!snapshot.Enabled || snapshot.IsEngineeringAuthorized); }
        private void UpdateActions()
        {
            if (UnlockPanelS137 == null) return;
            bool known = snapshot != null, enabled = known && snapshot.Enabled, authorized = known && snapshot.IsEngineeringAuthorized;
            bool configure = CanConfigure();
            UnlockPanelS137.Visibility = enabled && !authorized && !snapshot.IsFaulted ? Visibility.Visible : Visibility.Collapsed;
            UnlockPasswordS137.IsEnabled = known && enabled && !authorized && !snapshot.IsFaulted && !busy && snapshot.RetryAfterSeconds == 0;
            UnlockButtonS137.IsEnabled = UnlockPasswordS137.IsEnabled && UnlockPasswordS137.Password.Length > 0;
            LockButtonS137.Visibility = enabled && authorized ? Visibility.Visible : Visibility.Collapsed;
            LockButtonS137.IsEnabled = !busy && known && !snapshot.IsFaulted;
            StartEnableButtonS137.Visibility = known && !enabled && !snapshot.IsFaulted ? Visibility.Visible : Visibility.Collapsed;
            StartEnableButtonS137.IsEnabled = configure;
            ConfigurationPanelS137.IsEnabled = configure;
            EnableButtonS137.Visibility = !enabled ? Visibility.Visible : Visibility.Collapsed;
            ConfigureButtonS137.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            DisableButtonS137.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            NewPasswordLabelS137.Text = enabled ? "新口令（改口令时填写；8–128 字符）" : "设置自己的口令（8–128 字符，无默认口令）";
            AddApprovedButtonS137.IsEnabled = configure && enabled && (snapshot.ApprovedRecipes == null || snapshot.ApprovedRecipes.Count < 32);
            RemoveApprovedButtonS137.IsEnabled = configure && enabled && ApprovedRecipesS137.SelectedItem is VmApprovedStationRecipeS137;
        }
        private void ClearPasswords()
        { UnlockPasswordS137.Clear(); NewPasswordS137.Clear(); ConfirmPasswordS137.Clear(); }
        private VmStationProtectionActionEventArgsS137 FreezeAction(bool unlocking, string selectedPath, bool requireNewPassword, bool includeNewPassword = false)
        {
            if (snapshot == null || busy || snapshot.IsFaulted) { ClearPasswords(); return null; }
            string password = unlocking ? UnlockPasswordS137.Password : string.Empty;
            string newPassword = includeNewPassword ? NewPasswordS137.Password : string.Empty;
            string confirmation = includeNewPassword ? ConfirmPasswordS137.Password : string.Empty;
            long generation = snapshot.Generation; int minutes = SelectedMinutes();
            ClearPasswords(); // Before any event, confirmation dialog or asynchronous work.
            if (includeNewPassword && (requireNewPassword || newPassword.Length > 0 || confirmation.Length > 0))
            {
                if (newPassword.Length < 8 || newPassword.Length > 128 || !string.Equals(newPassword, confirmation, StringComparison.Ordinal))
                { SetStatus("新口令须为 8–128 字符，且两次输入完全一致；未提交，请重新输入。"); return null; }
            }
            if (minutes < 5 || minutes > 120) { SetStatus("工程会话有效期须为 5–120 分钟，未提交。"); return null; }
            return new VmStationProtectionActionEventArgsS137(password, unlocking ? string.Empty : newPassword, minutes, selectedPath, generation);
        }
        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            if (snapshot == null || !snapshot.Enabled || snapshot.IsEngineeringAuthorized || snapshot.RetryAfterSeconds > 0) return;
            var args = FreezeAction(true, string.Empty, false); if (args != null && UnlockRequested != null) UnlockRequested(this, args);
        }
        private void StartEnable_Click(object sender, RoutedEventArgs e)
        { if (!CanConfigure() || snapshot.Enabled) return; MoreS137.IsExpanded = true; NewPasswordS137.Focus(); }
        private void Enable_Click(object sender, RoutedEventArgs e)
        {
            if (!CanConfigure() || snapshot.Enabled) return;
            var args = FreezeAction(false, string.Empty, true, true); if (args != null && EnableRequested != null) EnableRequested(this, args);
        }
        private void Configure_Click(object sender, RoutedEventArgs e)
        {
            if (!CanConfigure() || !snapshot.Enabled) return;
            var args = FreezeAction(false, string.Empty, false, true); if (args != null && ConfigureRequested != null) ConfigureRequested(this, args);
        }
        private void Disable_Click(object sender, RoutedEventArgs e)
        {
            if (!CanConfigure() || !snapshot.Enabled) return;
            var args = FreezeAction(false, string.Empty, false); if (args == null) return;
            if (MessageBox.Show(this, "关闭本机工程编辑保护后，现有开发修改入口将不再需要工程解锁。\n这不改变故障门禁或设备状态。确定关闭保护？", "明确关闭编辑保护", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
            if (!CanConfigure() || snapshot.Generation != args.ExpectedGeneration) { SetStatus("状态在确认期间变化，未提交关闭保护；请重新核对。"); return; }
            if (DisableRequested != null) DisableRequested(this, args);
        }
        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            if (snapshot == null || !snapshot.Enabled || !snapshot.IsEngineeringAuthorized || busy) return;
            var args = FreezeAction(false, string.Empty, false); if (args != null && LockRequested != null) LockRequested(this, args);
        }
        private void AddApproved_Click(object sender, RoutedEventArgs e)
        {
            if (!CanConfigure() || !snapshot.Enabled || (snapshot.ApprovedRecipes != null && snapshot.ApprovedRecipes.Count >= 32)) return;
            var args = FreezeAction(false, string.Empty, false); if (args != null && AddApprovedRecipeRequested != null) AddApprovedRecipeRequested(this, args);
        }
        private void RemoveApproved_Click(object sender, RoutedEventArgs e)
        {
            if (!CanConfigure() || !snapshot.Enabled) return;
            var item = ApprovedRecipesS137.SelectedItem as VmApprovedStationRecipeS137; if (item == null) return;
            var args = FreezeAction(false, item.Path, false); if (args != null && RemoveApprovedRecipeRequested != null) RemoveApprovedRecipeRequested(this, args);
        }
        private void Password_Changed(object sender, RoutedEventArgs e) { if (!updating) UpdateActions(); }
        private void Approved_Changed(object sender, SelectionChangedEventArgs e) { if (!updating) UpdateActions(); }
        private void Close_Click(object sender, RoutedEventArgs e) { ClearPasswords(); Close(); }
    }
}
