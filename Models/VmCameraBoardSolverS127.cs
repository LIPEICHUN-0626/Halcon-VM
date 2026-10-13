using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using HalconDotNet;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmCameraBoardSampleS127
    {
        public string Path { get; set; }
        public string Status { get; set; }
        public string DisplayText { get { return System.IO.Path.GetFileName(Path) + " · " + Status; } }
    }

    public sealed class VmCameraBoardSolveResultS127
    {
        public List<VmCameraBoardSampleS127> Samples { get; set; } = new List<VmCameraBoardSampleS127>();
        public VmCameraIntrinsicsS126 Camera { get; set; }
        public string Summary { get; set; }
    }

    public static class VmCameraBoardSolverS127
    {
        public static VmCameraBoardSolveResultS127 Solve(string boardPath, IList<string> paths, VmCameraIntrinsicsS126 initial, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(boardPath) || !File.Exists(boardPath)) throw new InvalidOperationException("标定板描述文件不存在。" );
            if (paths == null || paths.Count < 6 || paths.Count > 64) throw new InvalidOperationException("标定板图像应为 6–64 张。" );
            if (initial == null || !string.IsNullOrWhiteSpace(initial.Validate())) throw new InvalidOperationException("相机内参初值无效。" );
            var result = new VmCameraBoardSolveResultS127();
            HTuple data = null, start = null, empty = new HTuple(), error = null, optimized = null;
            var centerRows = new List<double>();
            var centerColumns = new List<double>();
            int accepted = 0;
            try
            {
                HOperatorSet.CreateCalibData("calibration_object", 1, 1, out data);
                start = initial.CreateHalconTuple();
                HOperatorSet.SetCalibDataCamParam(data, 0, empty, start);
                HOperatorSet.SetCalibDataCalibObject(data, 0, boardPath);
                foreach (string path in paths)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var sample = new VmCameraBoardSampleS127 { Path = path, Status = "待检测" };
                    result.Samples.Add(sample);
                    HTuple width = null, height = null, channels = null, rows = null, columns = null, indices = null, pose = null;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new InvalidOperationException("图像文件不存在");
                        if (new FileInfo(path).Length > 536870912L) throw new InvalidOperationException("单张文件超过 512 MiB 安全上限");
                        using (var image = new HImage(path))
                        {
                            HOperatorSet.GetImageSize(image, out width, out height);
                            if (width.I != initial.Width || height.I != initial.Height)
                                throw new InvalidOperationException("分辨率 " + width.I + "×" + height.I + " 与内参 " + initial.Width + "×" + initial.Height + " 不同");
                            HOperatorSet.CountChannels(image, out channels);
                            if (channels.I != 1 && channels.I != 3) throw new InvalidOperationException("只支持单通道或 RGB 标定图像");
                            using (HImage gray = ToGrayS127(image, channels.I))
                                HOperatorSet.FindCalibObject(gray, data, 0, 0, accepted, empty, empty);
                        }
                        HOperatorSet.GetCalibDataObservPoints(data, 0, 0, accepted, out rows, out columns, out indices, out pose);
                        if (rows.Length < 6)
                        {
                            HOperatorSet.RemoveCalibDataObserv(data, 0, 0, accepted);
                            throw new InvalidOperationException("标定点少于 6 个");
                        }
                        double[] rr = rows.ToDArr(), cc = columns.ToDArr();
                        centerRows.Add(rr.Average()); centerColumns.Add(cc.Average());
                        sample.Status = "成功 · " + rows.Length.ToString(CultureInfo.InvariantCulture) + " 个标记";
                        accepted++;
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        string reason = (ex.Message ?? "检测异常").Replace("\r", " ").Replace("\n", " ");
                        sample.Status = "失败 · " + (reason.Length > 96 ? reason.Substring(0, 96) + "…" : reason);
                        try { HOperatorSet.RemoveCalibDataObserv(data, 0, 0, accepted); } catch { }
                    }
                    finally
                    {
                        if (width != null) width.Dispose(); if (height != null) height.Dispose(); if (channels != null) channels.Dispose();
                        if (rows != null) rows.Dispose(); if (columns != null) columns.Dispose(); if (indices != null) indices.Dispose(); if (pose != null) pose.Dispose();
                    }
                }
                if (accepted < 6)
                {
                    result.Summary = "有效 " + accepted.ToString(CultureInfo.InvariantCulture) + "/" + paths.Count.ToString(CultureInfo.InvariantCulture) +
                        " 张，至少需要 6 张；请移除失败图像并从不同位置/角度补拍。";
                    return result;
                }
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    HOperatorSet.CalibrateCameras(data, out error);
                    HOperatorSet.GetCalibData(data, "camera", 0, "params", out optimized);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    result.Summary = "标记检测成功 " + accepted.ToString(CultureInfo.InvariantCulture) +
                        " 张，但 HALCON 求参未收敛：" + ex.Message + "；请补拍不同位置/倾斜角图像并检查内参初值。";
                    return result;
                }
                double rms = error[0].D;
                if (double.IsNaN(rms) || double.IsInfinity(rms) || rms < 0)
                    throw new InvalidOperationException("HALCON 返回无效的重投影 RMS。" );
                var camera = VmCameraIntrinsicsS126.FromHalconTuple(optimized, "标定板 " + Path.GetFileName(boardPath));
                if (camera.Width != initial.Width || camera.Height != initial.Height)
                    throw new InvalidOperationException("求出的内参图像尺寸与样本尺寸不一致。" );
                camera.CalibrationSampleCount = accepted;
                camera.CalibrationRmsPx = rms;
                camera.CalibrationBoardName = Path.GetFileName(boardPath);
                string validation = camera.Validate();
                if (!string.IsNullOrWhiteSpace(validation)) throw new InvalidOperationException(validation);
                result.Camera = camera;
                double rowSpread = centerRows.Max() - centerRows.Min();
                double columnSpread = centerColumns.Max() - centerColumns.Min();
                string coverage = rowSpread < initial.Height * 0.1 && columnSpread < initial.Width * 0.1
                    ? " · 板中心覆盖偏窄，建议补拍边缘/倾斜姿态" : " · 板中心位置有变化";
                result.Summary = string.Format(CultureInfo.InvariantCulture,
                    "有效 {0}/{1} 张 · 重投影 RMS {2:0.###} px{3} · 已填入内参草稿，点击“应用”保存",
                    accepted, paths.Count, rms, coverage);
                return result;
            }
            finally
            {
                if (data != null) { try { HOperatorSet.ClearCalibData(data); } catch { } data.Dispose(); }
                if (start != null) start.Dispose(); if (empty != null) empty.Dispose();
                if (error != null) error.Dispose(); if (optimized != null) optimized.Dispose();
            }
        }

        private static HImage ToGrayS127(HImage image, int channels)
        {
            if (channels == 1) return image.CopyImage();
            return image.Rgb1ToGray();
        }
    }
}
