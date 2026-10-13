using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<VmCameraBoardSampleS127> cameraSamplesS127 = new ObservableCollection<VmCameraBoardSampleS127>();
        private string cameraBoardPathS127;
        private int cameraBoardRevisionS127;
        private CancellationTokenSource cameraBoardSolveCancellationS127;

        private void LoadCameraBoardDraftS127(VmToolParameterData parameters)
        {
            if (cameraBoardSolveCancellationS127 != null) cameraBoardSolveCancellationS127.Cancel();
            cameraBoardRevisionS127++;
            cameraBoardPathS127 = parameters.CameraBoardPathS127;
            cameraSamplesS127.Clear();
            foreach (string path in (parameters.CameraSamplePathsS127 ?? new List<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Take(64))
                cameraSamplesS127.Add(new VmCameraBoardSampleS127 { Path = path, Status = "待检测" });
            DockCameraSampleListS127.ItemsSource = cameraSamplesS127;
            DockCameraBoardTextS127.Text = string.IsNullOrWhiteSpace(cameraBoardPathS127) ? "未选择 .cpd/.descr" : Path.GetFileName(cameraBoardPathS127);
            DockCameraBoardStatusS127.Text = cameraSamplesS127.Count == 0 ? "尚未检测 · 至少加入 6 张不同姿态图像" :
                "已加入 " + cameraSamplesS127.Count.ToString(CultureInfo.InvariantCulture) + " 张 · 点击检测并求内参";
        }

        private void ReadCameraBoardDraftS127(VmToolParameterData parameters)
        {
            parameters.CameraBoardPathS127 = cameraBoardPathS127;
            parameters.CameraSamplePathsS127 = cameraSamplesS127.Select(item => item.Path).ToList();
        }

        private void InvalidateCameraBoardSolveS127()
        {
            cameraBoardRevisionS127++;
            if (cameraBoardSolveCancellationS127 != null) cameraBoardSolveCancellationS127.Cancel();
            cameraSolvedDraftS127 = null;
            cameraImportedFileNameS126 = null;
            DockCameraBoardStatusS127.Text = "样本/标定板已改变 · 当前内参仍是旧值，请重新求参或按手动参数应用";
            DockCameraSummaryS126.Text = "样本已改变 · 当前数值是上次结果；重新求参前不能视作新样本的标定结论";
            foreach (var item in cameraSamplesS127) item.Status = "待检测";
            DockCameraSampleListS127.Items.Refresh();
        }

        private void DockCameraBoardBrowseS127_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("选择标定板文件");
            if (editLeaseS137 < 0) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.CameraDistortionCorrection) return;
            var dialog = new OpenFileDialog { Title = "选择 HALCON 标定板描述文件", Filter = "HALCON 标定板 (*.cpd;*.descr)|*.cpd;*.descr|所有文件 (*.*)|*.*",
                CheckFileExists = true, Multiselect = false };
            if (dialog.ShowDialog(this) != true) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            cameraBoardPathS127 = dialog.FileName;
            DockCameraBoardTextS127.Text = Path.GetFileName(cameraBoardPathS127);
            InvalidateCameraBoardSolveS127();
            DockDraftValue_Changed(sender, e);
        }

        private void DockCameraAddImagesS127_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("加入标定板图像样本");
            if (editLeaseS137 < 0) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.CameraDistortionCorrection) return;
            var dialog = new OpenFileDialog { Title = "加入标定板实拍图像", Filter = "图像 (*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff)|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|所有文件 (*.*)|*.*",
                CheckFileExists = true, Multiselect = true };
            if (dialog.ShowDialog(this) != true) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            int added = 0;
            foreach (string path in dialog.FileNames)
            {
                if (cameraSamplesS127.Count >= 64) break;
                if (cameraSamplesS127.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))) continue;
                cameraSamplesS127.Add(new VmCameraBoardSampleS127 { Path = path, Status = "待检测" });
                added++;
            }
            if (added == 0) { DockCameraBoardStatusS127.Text = "没有加入新图像；最多 64 张，重复路径不会重复添加。"; return; }
            InvalidateCameraBoardSolveS127();
            DockDraftValue_Changed(sender, e);
        }

        private void DockCameraRemoveImageS127_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("移除标定板图像样本")) return;
            var row = DockCameraSampleListS127.SelectedItem as VmCameraBoardSampleS127;
            if (row == null) return;
            cameraSamplesS127.Remove(row);
            InvalidateCameraBoardSolveS127();
            DockDraftValue_Changed(sender, e);
        }

        private void DockCameraSampleSelectionS127_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var row = DockCameraSampleListS127.SelectedItem as VmCameraBoardSampleS127;
            DockCameraSamplePreviewS127.Source = null;
            DockCameraSamplePreviewS127.Visibility = Visibility.Collapsed;
            if (row == null || !File.Exists(row.Path)) return;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 320;
                bitmap.UriSource = new Uri(row.Path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                DockCameraSamplePreviewS127.Source = bitmap;
                DockCameraSamplePreviewS127.Visibility = Visibility.Visible;
            }
            catch { DockCameraBoardStatusS127.Text = "该格式无法缩略预览；HALCON 原图检测仍可使用。"; }
        }

        private async void DockCameraSolveS127_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("标定板检测与内参求解");
            if (editLeaseS137 < 0) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.CameraDistortionCorrection) return;
            if (string.IsNullOrWhiteSpace(cameraBoardPathS127) || !File.Exists(cameraBoardPathS127))
            { DockCameraBoardStatusS127.Text = "先选择有效的 HALCON 标定板 .cpd/.descr 文件。"; return; }
            if (cameraSamplesS127.Count < 6)
            { DockCameraBoardStatusS127.Text = "至少加入 6 张不同姿态的真实标定板图像。"; return; }
            VmCameraIntrinsicsS126 initial;
            try { initial = VmCameraIntrinsicsS126.Parse(DockCameraParamsTextS126.Text, null); }
            catch (Exception ex) { DockCameraBoardStatusS127.Text = "先提供已知焦距/像元尺度与图像尺寸初值：" + ex.Message; return; }
            string board = cameraBoardPathS127;
            string[] paths = cameraSamplesS127.Select(item => item.Path).ToArray();
            string initialText = DockCameraParamsTextS126.Text;
            int revision = cameraBoardRevisionS127;
            if (cameraBoardSolveCancellationS127 != null) cameraBoardSolveCancellationS127.Cancel();
            var cancellation = new CancellationTokenSource();
            cameraBoardSolveCancellationS127 = cancellation;
            DockCameraSolveButtonS127.IsEnabled = false;
            DockCameraBoardStatusS127.Text = "HALCON 正在逐张检测并求参；切换模块或修改样本将丢弃旧计算结果。";
            try
            {
                VmCameraBoardSolveResultS127 result = await Task.Run(() => VmCameraBoardSolverS127.Solve(board, paths, initial, cancellation.Token));
                if (dockEditingTool == null || dockEditingTool.ToolId != tool.ToolId || revision != cameraBoardRevisionS127 || cancellation.IsCancellationRequested) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137))
                { DockCameraBoardStatusS127.Text = "工程授权已改变，本次求参结果未进入草稿；原内参保留。"; return; }
                if (!string.Equals(initialText, DockCameraParamsTextS126.Text, StringComparison.Ordinal))
                { DockCameraBoardStatusS127.Text = "内参初值在求参期间已改变；旧计算结果已丢弃，请重新求参。"; return; }
                for (int i = 0; i < result.Samples.Count && i < cameraSamplesS127.Count; i++)
                    cameraSamplesS127[i].Status = result.Samples[i].Status;
                DockCameraSampleListS127.Items.Refresh();
                DockCameraBoardStatusS127.Text = result.Summary;
                if (result.Camera == null) return;
                cameraSolvedDraftS127 = result.Camera.Clone();
                cameraImportedEditTextS126 = result.Camera.ToEditText();
                cameraImportedFileNameS126 = result.Camera.ImportedFileName;
                DockCameraParamsTextS126.Text = cameraImportedEditTextS126;
                DockCameraSummaryS126.Text = result.Camera.ToSummary() + " · 待点击“应用”保存到方案";
                DockDraftValue_Changed(sender, e);
            }
            catch (OperationCanceledException) { if (revision == cameraBoardRevisionS127) DockCameraBoardStatusS127.Text = "求参已取消。"; }
            catch (Exception ex) { if (revision == cameraBoardRevisionS127) DockCameraBoardStatusS127.Text = "求参失败：" + ex.Message; }
            finally
            {
                if (ReferenceEquals(cameraBoardSolveCancellationS127, cancellation)) cameraBoardSolveCancellationS127 = null;
                cancellation.Dispose();
                DockCameraSolveButtonS127.IsEnabled = true;
            }
        }
    }
}
