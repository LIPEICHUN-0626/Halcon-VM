using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly Dictionary<string, TemplateItem> shapeToolResources = new Dictionary<string, TemplateItem>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<ShapeMatchResult>> shapeToolMatches = new Dictionary<string, List<ShapeMatchResult>>(StringComparer.OrdinalIgnoreCase);
        private int selectedShapeCandidateIndex = -1;

        private sealed class ShapeCandidateView
        {
            public ShapeMatchResult Match { get; set; }
            public int Index { get { return Match == null ? 0 : Match.Index; } }
            public string ScoreText { get { return Match == null ? "--" : Match.Score.ToString("0.000", CultureInfo.InvariantCulture); } }
            public string RowText { get { return Match == null ? "--" : Match.Row.ToString("0.00", CultureInfo.InvariantCulture); } }
            public string ColumnText { get { return Match == null ? "--" : Match.Column.ToString("0.00", CultureInfo.InvariantCulture); } }
            public string AngleText { get { return Match == null ? "--" : Match.AngleDeg.ToString("0.0°", CultureInfo.InvariantCulture); } }
            public string ScaleText { get { return Match == null ? "--" : Match.Scale.ToString("0.000", CultureInfo.InvariantCulture); } }
        }

        private void LoadShapeDockDraftFields(VmToolInstance tool, VmToolParameterData parameters)
        {
            if (DockShapeMatchPanel == null) return;
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch)
            {
                DockShapeTrainingRoiComboBox.ItemsSource = null;
                return;
            }
            parameters.Normalize();
            DockShapeModelNameTextBox.Text = parameters.ShapeModelName;
            DockShapeMinScoreTextBox.Text = parameters.ShapeMinScore.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeMaxMatchesTextBox.Text = parameters.ShapeMaxMatches.ToString(CultureInfo.InvariantCulture);
            DockShapeAngleStartTextBox.Text = parameters.ShapeAngleStartDegrees.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeAngleExtentTextBox.Text = parameters.ShapeAngleExtentDegrees.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeScaleMinTextBox.Text = parameters.ShapeScaleMin.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeScaleMaxTextBox.Text = parameters.ShapeScaleMax.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeMaxOverlapTextBox.Text = parameters.ShapeMaxOverlap.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeGreedinessTextBox.Text = parameters.ShapeGreediness.ToString("0.###", CultureInfo.InvariantCulture);
            DockShapeNumLevelsTextBox.Text = parameters.ShapeNumLevels;
            List<VmToolValueOption> roiOptions = roiLayers
                .Where(item => item != null && item.Geometry != null && item.IsEnabled && !item.IsMask && IsAreaRoiShape(item.Geometry.ShapeType))
                .Select(item => new VmToolValueOption { Key = item.RoiId, DisplayText = item.SequenceText + "  " + item.Name + " · " + item.ShapeText })
                .ToList();
            if (!string.IsNullOrWhiteSpace(parameters.ShapeTrainingRoiId) && roiOptions.All(item => !string.Equals(item.Key, parameters.ShapeTrainingRoiId, StringComparison.OrdinalIgnoreCase)))
                roiOptions.Add(new VmToolValueOption { Key = parameters.ShapeTrainingRoiId, DisplayText = "无效训练 ROI · " + parameters.ShapeTrainingRoiId.Substring(0, Math.Min(8, parameters.ShapeTrainingRoiId.Length)) });
            DockShapeTrainingRoiComboBox.ItemsSource = roiOptions;
            DockShapeTrainingRoiComboBox.SelectedValue = string.IsNullOrWhiteSpace(parameters.ShapeTrainingRoiId) && roiOptions.Count > 0 ? roiOptions[0].Key : parameters.ShapeTrainingRoiId;
            UpdateShapeModelStateText(tool);
        }

        private bool TryReadShapeDockDraftFields(VmToolParameterData parameters, out string error)
        {
            error = string.Empty;
            double number;
            int integer;
            parameters.ShapeModelName = (DockShapeModelNameTextBox.Text ?? string.Empty).Trim();
            parameters.ShapeTrainingRoiId = (DockShapeTrainingRoiComboBox.SelectedValue as string ?? string.Empty).Trim();
            if (!TryParseUiDouble(DockShapeMinScoreTextBox.Text, out number)) { error = "Shape 最小分数必须是数值。"; return false; }
            parameters.ShapeMinScore = number;
            if (!int.TryParse(DockShapeMaxMatchesTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) { error = "Shape 最大匹配数量必须是整数。"; return false; }
            parameters.ShapeMaxMatches = integer;
            if (!TryParseUiDouble(DockShapeAngleStartTextBox.Text, out number)) { error = "Shape 起始角必须是数值。"; return false; }
            parameters.ShapeAngleStartDegrees = number;
            if (!TryParseUiDouble(DockShapeAngleExtentTextBox.Text, out number)) { error = "Shape 角度范围必须是数值。"; return false; }
            parameters.ShapeAngleExtentDegrees = number;
            if (!TryParseUiDouble(DockShapeScaleMinTextBox.Text, out number)) { error = "Shape 最小尺度必须是数值。"; return false; }
            parameters.ShapeScaleMin = number;
            if (!TryParseUiDouble(DockShapeScaleMaxTextBox.Text, out number)) { error = "Shape 最大尺度必须是数值。"; return false; }
            parameters.ShapeScaleMax = number;
            if (!TryParseUiDouble(DockShapeMaxOverlapTextBox.Text, out number)) { error = "Shape 最大重叠必须是数值。"; return false; }
            parameters.ShapeMaxOverlap = number;
            if (!TryParseUiDouble(DockShapeGreedinessTextBox.Text, out number)) { error = "Shape 贪婪度必须是数值。"; return false; }
            parameters.ShapeGreediness = number;
            parameters.ShapeNumLevels = (DockShapeNumLevelsTextBox.Text ?? string.Empty).Trim();
            // A saved model can run without a training ROI. Training validates its own region.
            return true;
        }

        private TemplateMatchOptions CreateShapeOptions(VmToolParameterData p)
        {
            return new TemplateMatchOptions
            {
                MinScore = p.ShapeMinScore, MaxMatches = p.ShapeMaxMatches,
                AngleStartDeg = p.ShapeAngleStartDegrees, AngleExtentDeg = p.ShapeAngleExtentDegrees,
                ScaleMin = p.ShapeScaleMin, ScaleMax = p.ShapeScaleMax,
                MaxOverlap = p.ShapeMaxOverlap, Greediness = p.ShapeGreediness,
                NumLevels = p.ShapeNumLevels, Metric = p.ShapeMetric, SubPixel = p.ShapeSubPixel,
                LimitToSearchRoi = true
            };
        }

        private void DockShapeTrainButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("制作Shape模板");
            if (editLeaseS137 < 0) return;
            RunUiAction("训练 Shape 模型", delegate
            {
                VmToolInstance tool = dockEditingTool;
                if (tool == null || tool.Kind != VmToolKind.ShapeMatch) throw new InvalidOperationException("请先选中 Shape Model 模块。");
                if (isFlowExecutionActive || isContinuousRunning || IsCurrentToolContinuousTrialRunningS87) throw new InvalidOperationException("请先停止运行再制作模板。");
                if (!ApplyDockConfigurationDraft(false)) throw new InvalidOperationException("请先修正 Shape 配置草稿。");
                VmToolParameterData p = tool.Parameters;
                VmRoiLayer layer = roiLayers.FirstOrDefault(item => string.Equals(item.RoiId, p.ShapeTrainingRoiId, StringComparison.OrdinalIgnoreCase));
                if (layer == null || layer.Geometry == null || !layer.IsEnabled || layer.IsMask || !IsAreaRoiShape(layer.Geometry.ShapeType))
                    throw new InvalidOperationException("训练 ROI 已失效；请选择已启用的面检测区。");
                FlowEditSnapshot before = CaptureFlowEditSnapshot();
                using (HImage inputImage = CreateToolInputImage(tool))
                using (TemplateDefinition definition = new TemplateDefinition
                {
                    TemplateName = p.ShapeModelName, TemplateRoi = layer.Geometry.Clone(),
                    TrainingMask = TemplateDefinition.CloneRegion(layer.Geometry.Region), DisplayFrame = layer.Geometry.Clone(), Options = CreateShapeOptions(p)
                })
                {
                    TemplateItem item = new TemplateItem
                    {
                        Name = p.ShapeModelName, TemplateRoi = layer.Geometry.Clone(),
                        TrainingMask = TemplateDefinition.CloneRegion(layer.Geometry.Region), DisplayFrame = layer.Geometry.Clone(), Options = CreateShapeOptions(p)
                    };
                    try
                    {
                        item.Service.Train(inputImage, definition);
                        if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                        string path = CreateShapeModelVersionPathS89(tool);
                        item.Service.Save(path);
                        if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                        p.ShapeModelPath = path;
                        SetShapeToolResource(tool, item);
                        item = null;
                    }
                    finally { if (item != null) item.Dispose(); }
                }
                shapeToolMatches.Remove(tool.ToolId);
                currentMatches.Clear();
                InvalidateToolRunResult(tool, "Shape 模型已重新训练");
                InvalidateDownstreamResults(tool, "上游 Shape 模型已重新训练");
                CommitFlowEditHistory("制作模板：" + tool.InstanceName, before);
                ScheduleRecipeStateCheck();
                UpdateShapeModelStateText(tool);
                RefreshShapeCandidateGrid(tool);
                RefreshUiState();
                ScheduleRefreshDisplay();
                LogInfo("Shape 模型已训练并受管保存：" + p.ShapeModelPath);
            });
        }

        private void DockShapeLoadButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("导入Shape模板");
            if (editLeaseS137 < 0) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch) return;
            OpenFileDialog dialog = new OpenFileDialog { Filter = "HALCON shape model|*.shm|All files|*.*" };
            if (dialog.ShowDialog(this) != true) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            RunUiAction("加载 Shape 模型", delegate
            {
                TemplateItem item = LoadShapeTemplateItem(dialog.FileName);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) { item.Dispose(); return; }
                VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
                p.ShapeModelPath = dialog.FileName;
                p.ShapeModelName = string.IsNullOrWhiteSpace(item.Name) ? Path.GetFileNameWithoutExtension(dialog.FileName) : item.Name;
                ApplyShapeOptionsToParameters(p, item.Options);
                tool.Parameters = p;
                SetShapeToolResource(tool, item);
                shapeToolMatches.Remove(tool.ToolId);
                InvalidateToolRunResult(tool, "Shape 模型资源已更换");
                InvalidateDownstreamResults(tool, "上游 Shape 模型资源已更换");
                ScheduleRecipeStateCheck();
                LoadDockConfigurationDraft(tool);
                LogInfo("Shape 模型已加载：" + dialog.FileName);
            });
        }

        private void DockShapeSaveButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("另存Shape模板");
            if (editLeaseS137 < 0) return;
            VmToolInstance tool = dockEditingTool;
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch) return;
            RunUiAction("另存 Shape 模型", delegate
            {
                TemplateItem item = GetShapeTemplateItem(tool, true);
                SaveFileDialog dialog = new SaveFileDialog { Filter = "HALCON shape model|*.shm", FileName = SafeFileName((tool.Parameters ?? new VmToolParameterData()).ShapeModelName, "ShapeModel_001") + ".shm" };
                if (dialog.ShowDialog(this) != true) return;
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                item.Service.Save(dialog.FileName);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                tool.Parameters.ShapeModelPath = dialog.FileName;
                ScheduleRecipeStateCheck();
                UpdateShapeModelStateText(tool);
                LogInfo("Shape 模型已另存并设为当前资源：" + dialog.FileName);
            });
        }

        private InspectionRecord RunShapeMatchToolS63(VmToolInstance tool, string source, HImage inputImage)
        {
            shapeToolMatches.Remove(tool.ToolId);
            try
            {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            p.Normalize();
            string parameterError = p.Validate(VmToolKind.ShapeMatch);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            TemplateItem item = GetShapeTemplateItem(tool, true);
            string targetInput;
            string targetContext = PrepareTemplateTargetContextS145(tool, out targetInput);
            TemplateItem boundItem;
            bool boundModel = shapeToolResources.TryGetValue(tool.ToolId, out boundItem) && ReferenceEquals(boundItem, item) &&
                string.Equals(item.TemplatePath, p.ShapeModelPath, StringComparison.OrdinalIgnoreCase);
            if (!boundModel && p.TemplateTargetsS145 != null)
                throw new InvalidOperationException("多目标选取须在当前Shape模块制作/加载并应用模型；不能使用未归属本模块的全局兼容模型。");
            List<ShapeMatchResult> matches;
            using (HRegion searchRegion = CreateEffectiveRoiRegion(tool, false) ?? inputImage.GetDomain())
            {
                HTuple area = null, row = null, column = null;
                try
                {
                    HOperatorSet.AreaCenter(searchRegion, out area, out row, out column);
                    if (area == null || area.Length == 0 || area.ToDArr().Sum() <= 0) throw new InvalidOperationException("Shape 搜索 ROI 扣除屏蔽区后为空。请检查搜索区、屏蔽区和位置修正。");
                }
                finally { if (area != null) area.Dispose(); if (row != null) row.Dispose(); if (column != null) column.Dispose(); }
                matches = item.Service.Match(inputImage, searchRegion, CreateShapeOptions(p));
            }
            shapeToolMatches[tool.ToolId] = matches;
            currentMatches.Clear();
            currentMatches.AddRange(matches);
            selectedShapeCandidateIndex = matches.Count == 0 ? -1 : 0;
            ShapeMatchResult best = matches.FirstOrDefault();
            PublishTemplateTargetsS145(tool, targetContext != null && boundModel ? ConvertShapeTargetsS145(matches, inputImage) : new List<VmTemplateTargetS145>(),
                targetContext, targetInput, boundModel ? item.Service.RuntimeModelVersionS145 : null);
            string summary = BuildShapeCandidateSummary(matches);
            string resultCode = best == null ? "NG" : "OK";
            string message = best == null ? "未匹配到满足分数、角度、尺度和重叠约束的目标" : string.Format(CultureInfo.InvariantCulture, "候选 {0} · Best={1:0.000} · R={2:0.00}, C={3:0.00}, A={4:0.00}°, S={5:0.000} · 图上最多100个，正式结果保持完整请求范围", matches.Count, best.Score, best.Row, best.Column, best.AngleDeg, best.Scale);
            InspectionRecord record = CreateRecord("ShapeModel", resultCode, best == null ? 0 : best.Score, message, tool);
            if (best != null)
            {
                record.MatchRow = best.Row; record.MatchColumn = best.Column; record.MatchAngle = best.AngleDeg;
                using (HObject contours = item.Service.CreateMatchContours(matches))
                    tool.SetOutputValue("XLD", VmXldSnapshot.Create(contours, tool.ToolId, tool.InstanceName, "XLD"));
                if (targetContext != null) tool.SetOutputCoordinateContext("XLD", targetContext);
            }
            record.TemplatePath = p.ShapeModelPath;
            resultStore.Add(record);
            MatchResultText.Text = "结果：" + resultCode + "，" + message;
            lastResultPayload = BuildKeyValueMatchPayload(p.ShapeModelName, matches);
            LastMessageText.Text = "最新外发结果：" + lastResultPayload;
            LogInfo(tool.InstanceName + " Shape 定位完成：" + message + " · Candidates=" + summary);
            return record;
            }
            catch { tool.ClearRuntimeOutputs(); shapeToolMatches.Remove(tool.ToolId); currentMatches.Clear(); throw; }
        }

        private void PublishShapeMatchOutputs(VmToolInstance tool, InspectionRecord record)
        {
            List<ShapeMatchResult> matches;
            if (!shapeToolMatches.TryGetValue(tool.ToolId, out matches)) matches = new List<ShapeMatchResult>();
            ShapeMatchResult best = matches.FirstOrDefault();
            tool.SetOutputValue("Score", best == null ? 0d : best.Score);
            tool.SetOutputValue("MatchCount", (double)matches.Count);
            tool.SetOutputValue("Row", best == null ? null : (object)best.Row);
            tool.SetOutputValue("Column", best == null ? null : (object)best.Column);
            tool.SetOutputValue("Angle", best == null ? null : (object)best.AngleDeg);
            tool.SetOutputValue("Scale", best == null ? null : (object)best.Scale);
            tool.SetOutputValue("Pose2D", best == null
                ? null
                : (object)VmPose2D.CreateImagePixel(best.Row, best.Column, best.AngleDeg, GetShapePoseContextId(tool)));
            tool.SetOutputValue("Candidates", BuildShapeCandidateSummary(matches));
        }

        private string GetShapePoseContextId(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Image");
            if (binding != null && !string.IsNullOrWhiteSpace(binding.SourceToolId) && !string.IsNullOrWhiteSpace(binding.SourcePortName))
            {
                return binding.SourceToolId + "." + binding.SourcePortName;
            }
            return string.IsNullOrWhiteSpace(currentImagePath) ? "Global.Image" : "Global.Image:" + currentImagePath;
        }

        private static string BuildShapeCandidateSummary(IList<ShapeMatchResult> matches)
        {
            if (matches == null || matches.Count == 0) return "[]";
            return "[" + string.Join(" | ", matches.Select(item => string.Format(CultureInfo.InvariantCulture, "#{0}:Score={1:0.000},Row={2:0.00},Column={3:0.00},Angle={4:0.00},Scale={5:0.000}", item.Index, item.Score, item.Row, item.Column, item.AngleDeg, item.Scale))) + "]";
        }

        private void RefreshShapeCandidateGrid(VmToolInstance tool)
        {
            if (DockShapeCandidatesDataGrid == null || DockRoiResultsDataGrid == null) return;
            bool shape = tool != null && tool.Kind == VmToolKind.ShapeMatch;
            DockShapeCandidatesDataGrid.Visibility = shape ? Visibility.Visible : Visibility.Collapsed;
            DockRoiResultsDataGrid.Visibility = shape ? Visibility.Collapsed : Visibility.Visible;
            if (!shape) { DockShapeCandidatesDataGrid.ItemsSource = null; return; }
            List<ShapeMatchResult> matches;
            if (!shapeToolMatches.TryGetValue(tool.ToolId, out matches)) matches = new List<ShapeMatchResult>();
            List<ShapeCandidateView> views = matches.Select(item => new ShapeCandidateView { Match = item }).ToList();
            DockShapeCandidatesDataGrid.ItemsSource = views;
            if (selectedShapeCandidateIndex >= 0 && selectedShapeCandidateIndex < views.Count) DockShapeCandidatesDataGrid.SelectedIndex = selectedShapeCandidateIndex;
            UpdateShapeModelStateText(tool);
        }

        private void DockShapeCandidatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dockDraftUpdating || DockShapeCandidatesDataGrid == null) return;
            selectedShapeCandidateIndex = DockShapeCandidatesDataGrid.SelectedIndex;
            ShapeCandidateView selected = DockShapeCandidatesDataGrid.SelectedItem as ShapeCandidateView;
            if (selected != null && selected.Match != null)
                HeaderStatusText.Text = string.Format(CultureInfo.InvariantCulture, "Shape 候选 #{0} · Score={1:0.000} · R={2:0.00}, C={3:0.00}, A={4:0.00}°, S={5:0.000}", selected.Index, selected.Match.Score, selected.Match.Row, selected.Match.Column, selected.Match.AngleDeg, selected.Match.Scale);
            ScheduleRefreshDisplay();
        }

        private bool TrySelectShapeCandidateAt(PointF point, double tolerance)
        {
            VmToolInstance tool = GetImageTaskToolS89();
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch || roiEditor.Tool != VisionTool.Select) return false;
            List<ShapeMatchResult> matches;
            if (!shapeToolMatches.TryGetValue(tool.ToolId, out matches) || matches.Count == 0) return false;
            int index = -1;
            double bestDistance = Math.Max(10, tolerance * 1.5);
            for (int i = 0; i < Math.Min(matches.Count, 100); i++)
            {
                double dr = matches[i].Row - point.Y, dc = matches[i].Column - point.X;
                double distance = Math.Sqrt(dr * dr + dc * dc);
                if (distance <= bestDistance) { bestDistance = distance; index = i; }
            }
            if (index < 0) return false;
            selectedShapeCandidateIndex = index;
            if (DockShapeCandidatesDataGrid != null) DockShapeCandidatesDataGrid.SelectedIndex = index;
            HeaderStatusText.Text = "已从图像定位 Shape 候选 #" + matches[index].Index.ToString(CultureInfo.InvariantCulture);
            ScheduleRefreshDisplay();
            return true;
        }

        private void DrawShapeTaskAnnotations(HWindow window, VmToolInstance tool, bool isLeftViewport)
        {
            if (window == null || tool == null || tool.Kind != VmToolKind.ShapeMatch || historicalImageMode) return;
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            VmRoiLayer training = roiLayers.FirstOrDefault(item => string.Equals(item.RoiId, p.ShapeTrainingRoiId, StringComparison.OrdinalIgnoreCase));
            try
            {
                if (training != null && training.Geometry != null && training.IsVisible) overlayRenderer.DrawRoiLayer(window, training.Geometry, "orange", 3, true, false);
                foreach (VmRoiLayer search in GetBoundDetectionRoiLayers(tool).Where(item => item.IsVisible)) overlayRenderer.DrawRoiLayer(window, search.Geometry, "cyan", 2, true, false);
                List<ShapeMatchResult> matches;
                if (!shapeToolMatches.TryGetValue(tool.ToolId, out matches)) return;
                for (int i = 0; i < Math.Min(matches.Count, 100); i++)
                {
                    ShapeMatchResult match = matches[i];
                    string color = i == selectedShapeCandidateIndex ? "magenta" : "green";
                    HObject cross = null, direction = null;
                    try
                    {
                        double length = 22 * Math.Max(0.5, match.Scale);
                        HOperatorSet.GenCrossContourXld(out cross, match.Row, match.Column, i == selectedShapeCandidateIndex ? 18 : 12, Math.PI / 4);
                        using (var rows = new HTuple(new[] { match.Row, match.Row - Math.Sin(match.AngleDeg * Math.PI / 180.0) * length }))
                        using (var columns = new HTuple(new[] { match.Column, match.Column + Math.Cos(match.AngleDeg * Math.PI / 180.0) * length }))
                            HOperatorSet.GenContourPolygonXld(out direction, rows, columns);
                        window.SetColor(color); window.SetLineWidth(i == selectedShapeCandidateIndex ? 4 : 2); window.DispObj(cross); window.DispObj(direction);
                        window.DispText("#" + match.Index.ToString(CultureInfo.InvariantCulture), "image", match.Row + 8, match.Column + 8, color, "box", "false");
                    }
                    finally { if (cross != null) cross.Dispose(); if (direction != null) direction.Dispose(); }
                }
            }
            catch (HalconException) { }
        }

        private bool HasShapeModelResource(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.ShapeMatch) return false;
            TemplateItem item;
            if (shapeToolResources.TryGetValue(tool.ToolId, out item) && item != null && item.HasModel) return true;
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            return (!string.IsNullOrWhiteSpace(p.ShapeModelPath) && File.Exists(p.ShapeModelPath)) || (currentTemplateItem != null && currentTemplateItem.HasModel);
        }

        private TemplateItem GetShapeTemplateItem(VmToolInstance tool, bool required)
        {
            TemplateItem existing;
            if (shapeToolResources.TryGetValue(tool.ToolId, out existing) && existing != null && existing.HasModel)
            {
                string configuredPath = (tool.Parameters ?? new VmToolParameterData()).ShapeModelPath;
                if (string.IsNullOrWhiteSpace(configuredPath) || string.Equals(existing.TemplatePath, configuredPath, StringComparison.OrdinalIgnoreCase)) return existing;
                DisposeShapeToolResource(tool.ToolId);
            }
            string path = (tool.Parameters ?? new VmToolParameterData()).ShapeModelPath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Shape 模型资源已过期或丢失：" + path, path);
                TemplateItem loaded = LoadShapeTemplateItem(path); SetShapeToolResource(tool, loaded); return loaded;
            }
            if (currentTemplateItem != null && currentTemplateItem.HasModel) return currentTemplateItem;
            if (required) throw new InvalidOperationException("当前 Shape 模块没有模型资源。请在右侧定位任务中训练或加载 .shm 模型。");
            return null;
        }

        private static TemplateItem LoadShapeTemplateItem(string path)
        {
            TemplateItem item = new TemplateItem { Name = Path.GetFileNameWithoutExtension(path) };
            try
            {
                item.Service.Load(path);
                using (TemplateDefinition definition = item.Service.Definition)
                {
                    if (definition != null)
                    {
                        if (!string.IsNullOrWhiteSpace(definition.TemplateName)) item.Name = definition.TemplateName;
                        item.Options = TemplateDefinition.CloneOptions(definition.Options);
                        item.TemplateRoi = definition.TemplateRoi == null ? null : definition.TemplateRoi.Clone();
                        item.DisplayFrame = definition.DisplayFrame == null ? null : definition.DisplayFrame.Clone();
                        item.TrainingMask = TemplateDefinition.CloneRegion(definition.TrainingMask);
                    }
                }
                return item;
            }
            catch { item.Dispose(); throw; }
        }

        private void SetShapeToolResource(VmToolInstance tool, TemplateItem item) { DisposeShapeToolResource(tool.ToolId); shapeToolResources[tool.ToolId] = item; UpdateShapeModelStateText(tool); }

        private void UpdateShapeModelStateText(VmToolInstance tool)
        {
            if (DockShapeModelStateText == null || tool == null || tool.Kind != VmToolKind.ShapeMatch) return;
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            DockShapeModelStateText.Text = HasShapeModelResource(tool) ? "模型：" + p.ShapeModelName + " · 已就绪" : (string.IsNullOrWhiteSpace(p.ShapeModelPath) ? "模型：未配置" : "模型：资源失效");
        }

        private string GetManagedShapeModelPath(VmToolInstance tool)
        {
            string directory = Path.Combine(recipeService.RecipeDirectory, "ShapeModels");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, SafeFileName((tool.Parameters ?? new VmToolParameterData()).ShapeModelName, "ShapeModel_001") + "_" + tool.ToolId.Substring(0, Math.Min(8, tool.ToolId.Length)) + ".shm");
        }

        private void DisposeShapeToolResource(string toolId)
        {
            TemplateItem item;
            if (shapeToolResources.TryGetValue(toolId ?? string.Empty, out item)) { shapeToolResources.Remove(toolId); if (item != null && !ReferenceEquals(item, currentTemplateItem)) item.Dispose(); }
            shapeToolMatches.Remove(toolId ?? string.Empty);
        }

        private void DisposeAllShapeToolResources()
        {
            foreach (TemplateItem item in shapeToolResources.Values.Distinct().ToList()) if (item != null && !ReferenceEquals(item, currentTemplateItem)) item.Dispose();
            shapeToolResources.Clear(); shapeToolMatches.Clear(); selectedShapeCandidateIndex = -1;
        }

        private void ApplyLegacyShapeResourceToFlow(VisionRecipe recipe)
        {
            if (recipe == null) return;
            foreach (VmToolInstance tool in flowTools.Where(item => item.Kind == VmToolKind.ShapeMatch))
            {
                bool missingParameters = tool.Parameters == null;
                VmToolParameterData p = tool.Parameters ?? new VmToolParameterData(); p.Normalize();
                if (string.IsNullOrWhiteSpace(p.ShapeModelPath) && !string.IsNullOrWhiteSpace(recipe.TemplatePath)) p.ShapeModelPath = recipe.TemplatePath;
                if (recipe.TemplateOptions != null && missingParameters)
                {
                    p.ShapeMinScore = recipe.TemplateOptions.MinScore <= 0 ? 0.60 : recipe.TemplateOptions.MinScore;
                    p.ShapeMaxMatches = recipe.TemplateOptions.MaxMatches <= 0 ? 1 : recipe.TemplateOptions.MaxMatches;
                    p.ShapeAngleStartDegrees = recipe.TemplateOptions.AngleStartDeg;
                    p.ShapeAngleExtentDegrees = recipe.TemplateOptions.AngleExtentDeg <= 0 ? 360 : recipe.TemplateOptions.AngleExtentDeg;
                    p.ShapeScaleMin = recipe.TemplateOptions.ScaleMin <= 0 ? 1 : recipe.TemplateOptions.ScaleMin;
                    p.ShapeScaleMax = recipe.TemplateOptions.ScaleMax <= 0 ? p.ShapeScaleMin : recipe.TemplateOptions.ScaleMax;
                }
                tool.Parameters = p;
            }
        }

        private string GetPrimaryShapeModelPathForRecipe()
        {
            VmToolInstance tool = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ShapeMatch && item.Parameters != null && !string.IsNullOrWhiteSpace(item.Parameters.ShapeModelPath));
            return tool == null ? (currentTemplateItem == null ? string.Empty : currentTemplateItem.TemplatePath) : tool.Parameters.ShapeModelPath;
        }

        private TemplateMatchOptions GetPrimaryShapeOptionsForRecipe()
        {
            VmToolInstance tool = flowTools.FirstOrDefault(item => item.Kind == VmToolKind.ShapeMatch && item.Parameters != null);
            return tool == null ? (currentTemplateItem == null ? null : currentTemplateItem.Options) : CreateShapeOptions(tool.Parameters);
        }

        private static void ApplyShapeOptionsToParameters(VmToolParameterData p, TemplateMatchOptions options)
        {
            if (p == null || options == null) return;
            p.ShapeMinScore = options.MinScore; p.ShapeMaxMatches = options.MaxMatches;
            p.ShapeAngleStartDegrees = options.AngleStartDeg; p.ShapeAngleExtentDegrees = options.AngleExtentDeg;
            p.ShapeScaleMin = options.ScaleMin; p.ShapeScaleMax = options.ScaleMax;
            p.ShapeMaxOverlap = options.MaxOverlap; p.ShapeGreediness = options.Greediness;
            p.ShapeNumLevels = options.NumLevels; p.ShapeMetric = options.Metric; p.ShapeSubPixel = options.SubPixel;
        }
    }
}
