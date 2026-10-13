using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool engineeringPackageBusyS139;
        private CancellationTokenSource engineeringPackageCancellationS139;

        private async void OpenEngineeringPackageS139()
        {
            long lease = CaptureEngineeringEditLeaseS137("导出工程副本");
            if (lease < 0 || !CanChangeRecipeS134() || !ConfirmDockDraftChanges("导出工程副本")) return;
            if (!CanChangeRecipeS134() || !IsEngineeringEditLeaseCurrentS137(lease)) return;
            string session = projectSessionIdS104;
            VisionRecipe snapshot = CaptureRecipe();
            var cancellation = new CancellationTokenSource();
            engineeringPackageCancellationS139 = cancellation;
            engineeringPackageBusyS139 = true;
            EngineeringPackageWindowS139 window = null;
            try
            {
                HeaderStatusText.Text = "正在冻结工程随行清单；不执行算法或设备。可关闭请求取消，等待文件读写退出。";
                var plan = await Task.Run(() => EngineeringPackageServiceS139.CreatePlan(snapshot, recipeService.ResourceDirectory, cancellation.Token));
                if (!IsEngineeringEditLeaseCurrentS137(lease) || projectSessionIdS104 != session) return;
                cancellation.Token.ThrowIfCancellationRequested();
                window = new EngineeringPackageWindowS139(plan) { Owner = this };
                window.Closing += delegate { if (window.DialogResult != true) cancellation.Cancel(); };
                window.RelocateRequested += async delegate(object sender, VmEngineeringPackageRelocateEventS139 args)
                {
                    if (!IsEngineeringEditLeaseCurrentS137(lease) || cancellation.IsCancellationRequested) return;
                    var before = window.Plan;
                    window.SetBusy(true);
                    try
                    {
                        var next = await Task.Run(() => EngineeringPackageServiceS139.Relocate(before, args.EntryKey, args.Path, cancellation.Token));
                        if (window.IsLoaded && ReferenceEquals(before, window.Plan) && IsEngineeringEditLeaseCurrentS137(lease)) window.SetPlan(next);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { if (window.IsLoaded) MessageBox.Show(window, "重新定位没有改动活动工程：" + ex.Message, "工程迁移", MessageBoxButton.OK, MessageBoxImage.Warning); }
                    finally { if (window.IsLoaded) window.SetBusy(false); }
                };
                if (window.ShowDialog() != true) return;
                plan = window.Plan;
                if (plan == null || plan.BlockingCount > 0 || !IsEngineeringEditLeaseCurrentS137(lease)) return;
                var dialog = new Microsoft.Win32.SaveFileDialog { Title = "保存随行工程副本", Filter = "工程方案 (*.json)|*.json", DefaultExt = ".json", FileName = "工程副本.json" };
                if (dialog.ShowDialog(this) != true || !IsEngineeringEditLeaseCurrentS137(lease)) return;
                string target = Path.GetFullPath(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(currentRecipePath) && string.Equals(target, Path.GetFullPath(currentRecipePath), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("请选择独立副本位置；导出不能覆盖当前活动方案。 ");
                HeaderStatusText.Text = "正在按冻结清单复制并复核；未解析依赖和目标环境不自动处理。";
                Func<bool> stillAuthorized = () => Dispatcher.Invoke(new Func<bool>(() =>
                    IsEngineeringEditLeaseCurrentS137(lease, false) && projectSessionIdS104 == session && !stationProtectionClosingS137 &&
                    !recipeChangeoverInProgressS134 && !isFlowExecutionActive && !isContinuousRunning));
                string directory = await Task.Run(() => EngineeringPackageServiceS139.Publish(target, plan, recipeService, stillAuthorized, cancellation.Token));
                HeaderStatusText.Text = "已导出工程副本、资源清单和目标准备报告；请同时迁移 JSON 与 " + Path.GetFileName(directory) + "。资源已复制不等于目标机器可运行。";
                LogInfo(HeaderStatusText.Text);
            }
            catch (OperationCanceledException) { HeaderStatusText.Text = "工程导出已取消；活动方案不变，不发布未完成副本。"; }
            catch (Exception ex)
            {
                HeaderStatusText.Text = "工程迁移未发布：" + ex.Message + "（当前方案不变；安全清理失败可能留本次临时文件。）";
                AppendAlarm(HeaderStatusText.Text);
            }
            finally
            {
                if (ReferenceEquals(engineeringPackageCancellationS139, cancellation)) engineeringPackageCancellationS139 = null;
                engineeringPackageBusyS139 = false;
                cancellation.Cancel(); cancellation.Dispose();
                RefreshUiState();
            }
        }
    }
}
