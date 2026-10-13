using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Windows;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private List<VmPlanarSampleS124> planarDraftSamplesS124 = new List<VmPlanarSampleS124>();
        private VmPlanarFitS124 planarDraftFitS124;

        // Local run provenance, not another calibration model or recipe state.
        // The weak key cannot bless a replacement instance with the same ToolId.
        private sealed class PlanarOutputDefinitionS148
        {
            public string DefinitionKey;
            public long OutputVersion;
        }

        private readonly ConditionalWeakTable<VmToolInstance, PlanarOutputDefinitionS148> planarOutputDefinitionsS148 =
            new ConditionalWeakTable<VmToolInstance, PlanarOutputDefinitionS148>();

        [DataContract]
        private sealed class PlanarAppliedDefinitionS148
        {
            [DataMember(Order = 1)] public string ToolId;
            [DataMember(Order = 2)] public string Kind;
            [DataMember(Order = 3)] public string Unit;
            [DataMember(Order = 4)] public List<VmPlanarSampleS124> Samples;
            [DataMember(Order = 5)] public List<VmToolInputBindingData> Bindings;
        }

        private sealed class PlanarDefinitionStreamS148 : MemoryStream
        {
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count > 65536 - Position) throw new InvalidOperationException("标定输出定义超过64KiB，不生成部分身份。");
                base.Write(buffer, offset, count);
            }
            public override void WriteByte(byte value)
            {
                if (Position >= 65536) throw new InvalidOperationException("标定输出定义超过64KiB。");
                base.WriteByte(value);
            }
        }

        private string CapturePlanarOutputDefinitionS148(VmToolInstance tool)
        {
            var parameters = tool.Parameters;
            if (parameters == null || parameters.PlanarCalibrationSamplesS124 == null ||
                parameters.PlanarCalibrationSamplesS124.Count > 64 || tool.InputBindings.Count > 64 ||
                string.IsNullOrWhiteSpace(tool.ToolId) || tool.ToolId.Length > 256)
                throw new InvalidOperationException("当前标定定义缺失或超出有界身份预算，请修正后重新运行。");
            var definition = new PlanarAppliedDefinitionS148
            {
                ToolId = tool.ToolId, Kind = tool.Kind.ToString(), Unit = parameters.PlanarCalibrationUnitS124,
                Samples = parameters.PlanarCalibrationSamplesS124,
                Bindings = tool.InputBindings.OrderBy(binding => binding == null ? string.Empty : binding.TargetPortName,
                    StringComparer.Ordinal).ToList()
            };
            using (var stream = new PlanarDefinitionStreamS148())
            using (var sha = SHA256.Create())
            {
                // Serialize actual fields without Clone/Normalize or fitting.
                // This key is not resource-byte identity or field-accuracy proof.
                new DataContractJsonSerializer(typeof(PlanarAppliedDefinitionS148),
                    new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 2048 }).WriteObject(stream, definition);
                stream.Position = 0;
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private bool IsAppliedPlanarOutputCurrentS148(VmToolInstance source)
        {
            if (source == null || source.Kind != VmToolKind.PlanarCalibration2D) return false;
            PlanarOutputDefinitionS148 published;
            if (!planarOutputDefinitionsS148.TryGetValue(source, out published) || published.OutputVersion != source.RuntimeOutputVersion)
                return false;
            try { return published.DefinitionKey == CapturePlanarOutputDefinitionS148(source); }
            catch (Exception) { return false; }
        }

        private void RequireAppliedPlanarOutputS148(VmToolInstance source)
        {
            if (!IsAppliedPlanarOutputCurrentS148(source))
                throw new InvalidOperationException("世界点来自未应用的标定草稿、已改变的定义或旧运行输出；请先应用标定并重新运行来源，再测量世界距离。");
        }

        private void LoadPlanarCalibrationDraftS124(VmToolParameterData parameters)
        {
            if (DockPlanarSampleListS124 == null) return;
            DockPlanarUnitS124.Text = parameters.PlanarCalibrationUnitS124 ?? "mm";
            planarDraftSamplesS124 = (parameters.PlanarCalibrationSamplesS124 ?? new List<VmPlanarSampleS124>())
                .Where(item => item != null).Select(item => item.Clone()).ToList();
            RefreshPlanarCalibrationPreviewS124();
        }

        private bool ReadPlanarCalibrationDraftS124(VmToolParameterData parameters)
        {
            parameters.PlanarCalibrationUnitS124 = (DockPlanarUnitS124.Text ?? string.Empty).Trim();
            parameters.PlanarCalibrationSamplesS124 = planarDraftSamplesS124.Select(item => item.Clone()).ToList();
            if (parameters.PlanarCalibrationSamplesS124.Count < 3)
                return FailDockDraft("平面标定至少需要 3 个来自同一图像链的非共线点；请在图像中创建点 ROI 并逐个加入。");
            try { VmPlanarCalibrationS124.Fit(parameters.PlanarCalibrationSamplesS124, parameters.PlanarCalibrationSamplesS124[0].ImageContextId); }
            catch (Exception ex) { return FailDockDraft(ex.Message); }
            return true;
        }

        private void DockPlanarStartPointS124_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("平面标定取点")) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.PlanarCalibration2D) return;
            if (dockEditingTool.GetInputBinding("Image") == null)
            {
                DockPlanarFitStatusS124.Text = "请先在输入页连接 Image 来源，再从当前模块输入图像取点。";
                return;
            }
            SetImageContextSelection(VmImageContextOption.ModuleInput);
            UpdateImageContextViewport(true);
            PointRoiButton_Click(sender, e);
            DockPlanarFitStatusS124.Text = "在图像中单击并确认点 ROI，再填写世界 X/Y 并加入。";
        }

        private void DockPlanarAddPointS124_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("加入平面标定样本")) return;
            try
            {
                VmToolInstance tool = dockEditingTool;
                if (tool == null || tool.Kind != VmToolKind.PlanarCalibration2D) return;
                VmRoiLayer layer = RoiLayerList == null ? null : RoiLayerList.SelectedItem as VmRoiLayer;
                if (layer == null || layer.Geometry == null || layer.Geometry.ShapeType != RoiShapeType.Point || !layer.IsEnabled)
                    throw new InvalidOperationException("请先在当前图像区创建并选中一个启用的点 ROI。");
                if (tool.GetInputBinding("Image") == null)
                    throw new InvalidOperationException("请先在输入页连接标定 Image 来源，再从该图像选点。");
                string context = ResolveTargetImageContextIdS86(tool);
                if (string.IsNullOrWhiteSpace(layer.ImageContextId) || !string.Equals(layer.ImageContextId, context, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("选中点 ROI 与标定 Image 不属于同一图像链；请切换到该来源重新取点。");
                double worldX, worldY;
                if (!TryParseUiDouble(DockPlanarWorldXS124.Text, out worldX) || !TryParseUiDouble(DockPlanarWorldYS124.Text, out worldY) ||
                    double.IsNaN(worldX) || double.IsInfinity(worldX) || double.IsNaN(worldY) || double.IsInfinity(worldY))
                    throw new InvalidOperationException("实际 X/Y 必须填写有限数值。");
                if (planarDraftSamplesS124.Count >= 64 && !planarDraftSamplesS124.Any(item => item.SourceRoiId == layer.RoiId))
                    throw new InvalidOperationException("标定最多 64 个点；可先移除不用的样本。");
                VmPlanarSampleS124 sample = new VmPlanarSampleS124 { PixelRow = layer.Geometry.Row,
                    PixelColumn = layer.Geometry.Column, WorldX = worldX, WorldY = worldY,
                    ImageContextId = context, SourceRoiId = layer.RoiId };
                int existing = planarDraftSamplesS124.FindIndex(item => item.SourceRoiId == layer.RoiId);
                if (existing >= 0) planarDraftSamplesS124[existing] = sample;
                else planarDraftSamplesS124.Add(sample);
                RefreshPlanarCalibrationPreviewS124();
                DockDraftValue_Changed(sender, e);
                ScheduleRefreshDisplay();
            }
            catch (Exception ex)
            {
                DockPlanarFitStatusS124.Text = "无法加入：" + ex.Message;
            }
        }

        private void DockPlanarRemovePointS124_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireEngineeringEditS137("移除平面标定样本")) return;
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.PlanarCalibration2D) return;
            int index = DockPlanarSampleListS124.SelectedIndex;
            if (index < 0 || index >= planarDraftSamplesS124.Count) return;
            planarDraftSamplesS124.RemoveAt(index);
            RefreshPlanarCalibrationPreviewS124();
            DockDraftValue_Changed(sender, e);
            ScheduleRefreshDisplay();
        }

        private void RefreshPlanarCalibrationPreviewS124()
        {
            if (DockPlanarSampleListS124 == null) return;
            VmPlanarFitS124 fit = null;
            string status = "还需 " + Math.Max(0, 3 - planarDraftSamplesS124.Count).ToString(CultureInfo.InvariantCulture) + " 个分散的对应点";
            if (planarDraftSamplesS124.Count >= 3)
            {
                try
                {
                    fit = VmPlanarCalibrationS124.Fit(planarDraftSamplesS124, planarDraftSamplesS124[0].ImageContextId);
                    status = string.Format(CultureInfo.InvariantCulture, "HALCON 拟合预览 · {0} 点 · RMS {1:0.####} / 最大 {2:0.####} {3}；应用后才保存",
                        planarDraftSamplesS124.Count, fit.RmsResidual, fit.MaxResidual, DockPlanarUnitS124.Text ?? "mm");
                }
                catch (Exception ex) { status = "待修复：" + ex.Message; }
            }
            DockPlanarSampleListS124.ItemsSource = planarDraftSamplesS124.Select((item, index) =>
                item.ToDisplayString(index, fit == null ? (double?)null : fit.Residuals[index])).ToList();
            planarDraftFitS124 = fit;
            DockPlanarFitStatusS124.Text = status;
        }

        private void DrawPlanarCalibrationLabelsS124(HWindow window, ResolvedImageContext image, bool isLeftViewport)
        {
            VmToolInstance tool = GetImageTaskToolS89();
            if (tool == null || tool.Kind != VmToolKind.PlanarCalibration2D || image == null || !image.HasImage) return;
            bool draft = centralModuleConfigurationOpenS77 && ReferenceEquals(tool, dockEditingTool);
            if (!draft && !resultOverlayLayers.Any(layer => layer.ToolId == tool.ToolId && layer.PortName == "XLD" &&
                layer.IsVisible && layer.StateKey == "Valid" && VmLayerViewportScope.Includes(layer.ViewportScope, isLeftViewport))) return;
            var samples = draft ? planarDraftSamplesS124 : tool.Parameters == null ? null : tool.Parameters.PlanarCalibrationSamplesS124;
            if (samples == null || samples.Count == 0) return;
            string context = GetImageCoordinateContextS112(image);
            if (string.IsNullOrWhiteSpace(context) || samples.Any(item => item == null ||
                !string.Equals(item.ImageContextId, context, StringComparison.OrdinalIgnoreCase))) return;
            double[] residuals = null;
            double rms = 0;
            if (draft && planarDraftFitS124 != null)
            {
                residuals = planarDraftFitS124.Residuals;
                rms = planarDraftFitS124.RmsResidual;
            }
            else
            {
                object raw;
                if (tool.ResultCode == "OK" && tool.TryGetOutputValue("Residuals", out raw)) residuals = raw as double[];
                tool.TryGetNumericOutput("RmsResidual", out rms);
            }
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                bool abnormal = residuals != null && i < residuals.Length && residuals[i] > Math.Max(2.5 * rms, 1e-9);
                string color = abnormal ? "red" : "cyan";
                try
                {
                    window.SetColor(color);
                    window.DispCross(sample.PixelRow, sample.PixelColumn, abnormal ? 17 : 11, Math.PI / 4.0);
                    DrawProjectiveGeometryLabel(window, (i + 1).ToString(CultureInfo.InvariantCulture) +
                        (abnormal ? " !" : ""), (int)Math.Max(0, sample.PixelRow - 20), (int)Math.Max(0, sample.PixelColumn + 8), color);
                }
                catch (HalconException) { /* A stale viewport is refreshed by the next image update. */ }
            }
        }

        private string BuildPlanarCalibrationInputSummaryS124(VmToolInstance tool)
        {
            var image = tool.GetInputBinding("Image");
            var a = tool.GetInputBinding("PointA");
            var b = tool.GetInputBinding("PointB");
            return "Image←" + (image == null ? "未连接" : image.SourcePortName) +
                " · 测量点 " + (a == null ? "未连接" : "A") + (b == null ? "" : "+B");
        }

        private VmPoint2D ReadPlanarPointInputS124(VmToolInstance tool, string portName, string imageContext)
        {
            var binding = tool.GetInputBinding(portName);
            if (binding == null) return null;
            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(portName + "：" + error);
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            if (source == null || source.ResultCode != "OK" || !source.TryGetOutputValue(binding.SourcePortName, out raw) || !(raw is VmPoint2D))
                throw new InvalidOperationException(portName + " 上游尚未在本周期输出有效 Point2D。");
            var point = ((VmPoint2D)raw).Clone();
            if (!string.IsNullOrWhiteSpace(point.GetValidationError()) ||
                !string.Equals(point.CoordinateContextId, imageContext, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(portName + " 与标定 Image 不属于同一 ImagePixel 图像链。");
            return point;
        }

        private InspectionRecord RunPlanarCalibrationToolS124(VmToolInstance tool)
        {
            planarOutputDefinitionsS148.Remove(tool);
            var parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string error = parameters.Validate(VmToolKind.PlanarCalibration2D);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            string outputDefinitionS148 = CapturePlanarOutputDefinitionS148(tool);
            if (tool.GetInputBinding("Image") == null) throw new InvalidOperationException("平面标定必须连接明确的上游 Image 来源。");
            if (tool.GetInputBinding("PointB") != null && tool.GetInputBinding("PointA") == null)
                throw new InvalidOperationException("要测量世界距离，请同时连接 PointA 和 PointB。");
            var bindingA = tool.GetInputBinding("PointA");
            var bindingB = tool.GetInputBinding("PointB");
            if (bindingA != null && bindingB != null && bindingA.SourceToolId == bindingB.SourceToolId && bindingA.SourcePortName == bindingB.SourcePortName)
                throw new InvalidOperationException("PointA 与 PointB 不能来自同一个上游端点。");
            string imageContext = ResolveTargetImageContextIdS86(tool);
            VmPlanarFitS124 fit = VmPlanarCalibrationS124.Fit(parameters.PlanarCalibrationSamplesS124, imageContext);
            HObject overlays = null, crosses = null, segment = null;
            HTuple inverse = null, matrix = null, predictedColumn = null, predictedRow = null;
            int imageWidth = 0, imageHeight = 0;
            try
            {
                using (HImage image = CreateToolInputImage(tool, "Image", false))
                {
                    imageWidth = GetImageWidth(image); imageHeight = GetImageHeight(image);
                    if (parameters.PlanarCalibrationSamplesS124.Any(p => p.PixelRow < 0 || p.PixelRow >= imageHeight || p.PixelColumn < 0 || p.PixelColumn >= imageWidth))
                        throw new InvalidOperationException("标定点超出当前图像尺寸，请检查图像来源/分辨率并重新采样。");
                }
                var pointA = ReadPlanarPointInputS124(tool, "PointA", imageContext);
                var pointB = ReadPlanarPointInputS124(tool, "PointB", imageContext);
                if (pointA != null)
                    tool.SetOutputValue("WorldPointA", fit.Transform(pointA, imageContext, tool.ToolId, parameters.PlanarCalibrationUnitS124));
                double distance = double.NaN;
                if (pointB != null)
                {
                    var worldA = (VmWorldPoint2DS124)GetPlanarOutputS124(tool, "WorldPointA");
                    var worldB = fit.Transform(pointB, imageContext, tool.ToolId, parameters.PlanarCalibrationUnitS124);
                    tool.SetOutputValue("WorldPointB", worldB);
                    distance = Math.Sqrt(Math.Pow(worldB.X - worldA.X, 2) + Math.Pow(worldB.Y - worldA.Y, 2));
                    if (double.IsNaN(distance) || double.IsInfinity(distance)) throw new InvalidOperationException("世界距离非有限，标定或测量点超出数值范围。");
                    tool.SetOutputValue("WorldDistance", distance);
                }
                var samples = parameters.PlanarCalibrationSamplesS124;
                using (HTuple rows = new HTuple(samples.Select(p => p.PixelRow).ToArray()))
                using (HTuple columns = new HTuple(samples.Select(p => p.PixelColumn).ToArray()))
                    HOperatorSet.GenCrossContourXld(out crosses, rows, columns, 12.0, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, crosses);
                matrix = new HTuple(fit.Matrix);
                HOperatorSet.HomMat2dInvert(matrix, out inverse);
                for (int i = 0; i < samples.Count; i++)
                {
                    HOperatorSet.AffineTransPoint2d(inverse, samples[i].WorldX, samples[i].WorldY, out predictedColumn, out predictedRow);
                    double row = predictedRow[0].D, column = predictedColumn[0].D;
                    if (double.IsNaN(row) || double.IsInfinity(row) || double.IsNaN(column) || double.IsInfinity(column) ||
                        row < -imageHeight || row > 2.0 * imageHeight || column < -imageWidth || column > 2.0 * imageWidth)
                        throw new InvalidOperationException("标定残差反投影超出安全图像范围，不能叠加；请检查点序和实际坐标。");
                    if (Math.Abs(row - samples[i].PixelRow) + Math.Abs(column - samples[i].PixelColumn) > 0.01)
                    {
                        using (HTuple rows = new HTuple(new[] { samples[i].PixelRow, row }))
                        using (HTuple columns = new HTuple(new[] { samples[i].PixelColumn, column }))
                            HOperatorSet.GenContourPolygonXld(out segment, rows, columns);
                        AppendOverlayObject(ref overlays, segment);
                        DisposeObject(segment); segment = null;
                    }
                    predictedColumn.Dispose(); predictedColumn = null;
                    predictedRow.Dispose(); predictedRow = null;
                }
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("RmsResidual", fit.RmsResidual);
                tool.SetOutputValue("MaxResidual", fit.MaxResidual);
                tool.SetOutputValue("Residuals", fit.Residuals.ToArray());
                tool.SetOutputValue("SampleCount", (double)samples.Count);
                tool.SetOutputValue("Unit", parameters.PlanarCalibrationUnitS124);
                tool.SetOutputValue("ImageContext", imageContext);
                tool.SetOutputValue("PixelToWorldMatrix", string.Join(",", fit.Matrix.Select(v => v.ToString("R", CultureInfo.InvariantCulture))));
                planarOutputDefinitionsS148.Add(tool, new PlanarOutputDefinitionS148
                {
                    DefinitionKey = outputDefinitionS148,
                    OutputVersion = tool.RuntimeOutputVersion
                });
                tool.SetOutputCoordinateContext("XLD", imageContext);
                ReplaceToolOverlayContours(overlays, tool, "XLD", samples.Count, "OK", fit.MaxResidual);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 平面标定 {0} 点，RMS {1:0.####}，最大残差 {2:0.####} {3}{4} · {5}", samples.Count,
                    fit.RmsResidual, fit.MaxResidual, parameters.PlanarCalibrationUnitS124,
                    double.IsNaN(distance) ? "" : "，世界距离 " + distance.ToString("0.####", CultureInfo.InvariantCulture) + " " + parameters.PlanarCalibrationUnitS124,
                    imageContext);
                var record = CreateRecord("PlanarCalibration2D", "OK", double.IsNaN(distance) ? fit.MaxResidual : distance, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            catch
            {
                planarOutputDefinitionsS148.Remove(tool);
                tool.ClearRuntimeOutputs();
                throw;
            }
            finally
            {
                if (predictedColumn != null) predictedColumn.Dispose(); if (predictedRow != null) predictedRow.Dispose();
                if (inverse != null) inverse.Dispose(); if (matrix != null) matrix.Dispose();
                DisposeObject(segment); DisposeObject(crosses); DisposeObject(overlays);
            }
        }

        private static object GetPlanarOutputS124(VmToolInstance tool, string portName)
        {
            object value;
            if (!tool.TryGetOutputValue(portName, out value)) throw new InvalidOperationException(portName + " 未产生本周期输出。");
            return value;
        }
    }
}
