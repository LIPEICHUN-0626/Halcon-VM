using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadEdgeLineDraftS143(VmToolParameterData parameters)
        {
            var p = parameters.ImageEdgeLineS143 ?? new VmImageEdgeLineParametersS143();
            DockEdgeLineStartRowS143.Text = p.StartRow.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineStartColumnS143.Text = p.StartColumn.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineEndRowS143.Text = p.EndRow.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineEndColumnS143.Text = p.EndColumn.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineCountS143.Text = p.SampleCount.ToString(CultureInfo.InvariantCulture);
            DockEdgeLineHalfWidthS143.Text = p.SearchHalfWidth.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineAverageS143.Text = p.AveragingHalfWidth.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineSigmaS143.Text = p.Sigma.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineThresholdS143.Text = p.Threshold.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineTransitionS143.SelectedValue = p.Transition; DockEdgeLineSelectS143.SelectedValue = p.Selection;
            DockEdgeLineMinimumS143.Text = p.MinimumValidCount.ToString(CultureInfo.InvariantCulture);
            DockEdgeLineCoverageS143.Text = p.MinimumCoverage.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineResidualS143.Text = p.MaximumResidual.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineFitS143.SelectedValue = p.FitAlgorithm; DockEdgeLineIterationsS143.Text = p.Iterations.ToString(CultureInfo.InvariantCulture);
        }

        private VmImageEdgeLineParametersS143 ReadEdgeLineValuesS143()
        {
            var p = new VmImageEdgeLineParametersS143();
            double r1, c1, r2, c2, search, average, sigma, threshold, coverage, residual;
            int count, minimum, iterations;
            if (!TryParseUiDouble(DockEdgeLineStartRowS143.Text, out r1) || !TryParseUiDouble(DockEdgeLineStartColumnS143.Text, out c1) ||
                !TryParseUiDouble(DockEdgeLineEndRowS143.Text, out r2) || !TryParseUiDouble(DockEdgeLineEndColumnS143.Text, out c2) ||
                !TryParseUiDouble(DockEdgeLineHalfWidthS143.Text, out search) || !TryParseUiDouble(DockEdgeLineAverageS143.Text, out average) ||
                !TryParseUiDouble(DockEdgeLineSigmaS143.Text, out sigma) || !TryParseUiDouble(DockEdgeLineThresholdS143.Text, out threshold) ||
                !TryParseUiDouble(DockEdgeLineCoverageS143.Text, out coverage) || !TryParseUiDouble(DockEdgeLineResidualS143.Text, out residual) ||
                !int.TryParse(DockEdgeLineCountS143.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) ||
                !int.TryParse(DockEdgeLineMinimumS143.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out minimum) ||
                !int.TryParse(DockEdgeLineIterationsS143.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out iterations))
                throw new InvalidOperationException("找线参数必须是明确有效的数值，不能用缺失值或旧草稿补充。");
            p.StartRow = r1; p.StartColumn = c1; p.EndRow = r2; p.EndColumn = c2; p.SampleCount = count;
            p.SearchHalfWidth = search; p.AveragingHalfWidth = average; p.Sigma = sigma; p.Threshold = threshold;
            p.MinimumValidCount = minimum; p.MinimumCoverage = coverage; p.MaximumResidual = residual; p.Iterations = iterations;
            p.Transition = DockEdgeLineTransitionS143.SelectedValue as string; p.Selection = DockEdgeLineSelectS143.SelectedValue as string;
            p.FitAlgorithm = DockEdgeLineFitS143.SelectedValue as string;
            string error = p.Validate(); if (error.Length > 0) throw new InvalidOperationException(error);
            return p;
        }

        private bool ReadEdgeLineDraftS143(VmToolParameterData parameters)
        { try { parameters.ImageEdgeLineS143 = ReadEdgeLineValuesS143(); return true; } catch (Exception error) { return FailDockDraft(error.Message); } }

        private bool CanPreviewEdgeLineS143(VmToolInstance tool)
        {
            try { GetEdgeLineInputContextS143(tool); return HasToolInputImage(tool, "Image", false); }
            catch { return false; }
        }

        private static LinearCaliperArrayEditGeometry CreateEdgeLineGeometryS143(VmImageEdgeLineParametersS143 p)
        { return new LinearCaliperArrayEditGeometry { StartRow = p.StartRow, StartColumn = p.StartColumn, EndRow = p.EndRow, EndColumn = p.EndColumn,
            Count = p.SampleCount, Length1 = p.SearchHalfWidth, Length2 = p.AveragingHalfWidth, Sigma = p.Sigma, IsEdgeLineS143 = true }; }

        private void RefreshEdgeLinePreviewS143()
        {
            if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.ImageEdgeLineFindingS143) return;
            if (!CanPreviewEdgeLineS143(dockEditingTool)) { ClearMeasurementDomainContext(); return; }
            try { ReplaceLinearCaliperArrayPreview(CreateEdgeLineGeometryS143(ReadEdgeLineValuesS143())); }
            catch (Exception error)
            { ReplaceLinearCaliperArrayPreview(null); MeasurementDomainEditorHintText.Text = "找线草稿暂不可预览：" + error.Message; }
        }

        private void BeginEdgeLineGeometryEditS143(VmToolInstance tool)
        {
            if (!CanChangeRecipeS134(false) || isFlowExecutionActive || isContinuousRunning || isImageQueueBatchRunning || IsCurrentToolContinuousTrialRunningS87)
            { HeaderStatusText.Text = "运行或工程任务忙碌中；停止后再编辑搜索带，未改变草稿。"; return; }
            if (!CanPreviewEdgeLineS143(tool))
            { ClearMeasurementDomainContext(); HeaderStatusText.Text = "先连接并运行明确上游 Image，再编辑搜索带；不回退当前显示或历史图。"; return; }
            if (!ReferenceEquals(dockEditingTool, tool)) LoadDockConfigurationDraft(tool);
            try
            {
                var geometry = CreateEdgeLineGeometryS143(ReadEdgeLineValuesS143());
                SetImageContextSelection(VmImageContextOption.ModuleInput);
                ResolvedImageContext context = ResolveImageContext();
                if (!context.HasImage) throw new InvalidOperationException("当前明确输入图像不可用。");
                // Explicitly entering editing can fit the geometric draft into this image.
                // It does not swap endpoints/polarity or apply the algorithm parameters.
                var constrained = ConstrainLinearCaliperArrayGeometry(geometry, context.Width, context.Height);
                measurementDomainToolId = tool.ToolId;
                ReplaceLinearCaliperArrayPreview(constrained);
                WriteMeasurementDomainPreviewToDock(!AreLinearCaliperArrayGeometriesEqual(constrained, geometry));
                measurementDomainEditActive = true; measurementDomainDragging = false;
                FinishRoiGeometryEdit(false);
                roiEditor.Cancel(); roiEditor.Tool = VisionTool.Select; ClearPendingRoi();
                measurementDomainEditHandle = RoiEditHandle.None;
                linearCaliperArrayEditHandle = LinearCaliperArrayEditHandle.None;
                circularCaliperEditHandle = CircularCaliperEditHandle.None;
                UpdateImageContextViewport(true);
                MeasurementDomainEditorBar.Visibility = Visibility.Visible; EditMeasurementDomainButton.Content = "编辑中";
                UpdateMeasurementDomainEditorText(); ScheduleRefreshDisplay();
                RefreshUiState();
                HeaderStatusText.Text = "找线搜索带：拖动绿色端点、黄色中心及青色半宽；箭头为实际搜索方向。松开只改草稿，F5 当前模块。";
            }
            catch (Exception error) { HeaderStatusText.Text = "搜索带未打开：" + error.Message; }
        }

        private void WriteEdgeLineGeometryDraftS143(LinearCaliperArrayEditGeometry geometry)
        {
            DockEdgeLineStartRowS143.Text = geometry.StartRow.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineStartColumnS143.Text = geometry.StartColumn.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineEndRowS143.Text = geometry.EndRow.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineEndColumnS143.Text = geometry.EndColumn.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineCountS143.Text = geometry.Count.ToString(CultureInfo.InvariantCulture);
            DockEdgeLineHalfWidthS143.Text = geometry.Length1.ToString("R", CultureInfo.InvariantCulture);
            DockEdgeLineAverageS143.Text = geometry.Length2.ToString("R", CultureInfo.InvariantCulture);
        }

        private void DrawEdgeLineArrowS143(HWindow window, LinearCaliperArrayEditGeometry geometry, double normalRow, double normalColumn)
        {
            double row = (geometry.StartRow + geometry.EndRow) / 2, column = (geometry.StartColumn + geometry.EndColumn) / 2;
            double length = Math.Min(geometry.Length1, 20), endRow = row + normalRow * length, endColumn = column + normalColumn * length;
            using (var rows = new HTuple(new[] { row, endRow, endRow - normalRow * 4 - normalColumn * 3, endRow, endRow - normalRow * 4 + normalColumn * 3 }))
            using (var columns = new HTuple(new[] { column, endColumn, endColumn - normalColumn * 4 + normalRow * 3, endColumn, endColumn - normalColumn * 4 - normalRow * 3 }))
            { HObject arrow; HOperatorSet.GenContourPolygonXld(out arrow, rows, columns); using (arrow) { window.SetColor("green"); window.SetLineWidth(2); window.DispObj(arrow); } }
        }

        private void DrawEdgeLineSampleStatusS143(HWindow window, VmToolInstance tool, bool left)
        {
            if (tool == null || tool.Kind != VmToolKind.ImageEdgeLineFindingS143 || historicalImageMode || window == null) return;
            var image = left ? ResolveDualInputImageContext() : ResolveImageContext();
            string context = GetImageCoordinateContextS112(image);
            if (!image.HasImage || string.IsNullOrWhiteSpace(context)) return;
            foreach (var item in new[] { new[] { "KeptPoints", "green" }, new[] { "RejectedPoints", "red" }, new[] { "MissingSamples", "orange" } })
            {
                object raw; string outputContext;
                if (!tool.TryGetOutputCoordinateContext(item[0], out outputContext) || outputContext != context || !tool.TryGetOutputValue(item[0], out raw)) continue;
                var points = raw as VmPoint2DArray;
                if (points == null || points.Points == null || points.Points.Count == 0 || points.Points.Count > 200 || points.GetValidationError(false).Length > 0 || points.CoordinateContextId != context) continue;
                using (var rows = new HTuple(points.Points.Select(point => point.Row).ToArray()))
                using (var columns = new HTuple(points.Points.Select(point => point.Column).ToArray()))
                { HObject crosses; HOperatorSet.GenCrossContourXld(out crosses, rows, columns, 5, Math.PI / 4); using (crosses) { window.SetColor(item[1]); window.SetLineWidth(2); window.DispObj(crosses); } }
            }
        }
    }
}
