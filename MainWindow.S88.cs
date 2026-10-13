using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using HalconWinFormsDemo.Models;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<VmTaskResultSummaryRow> dockTaskResultRowsS88 = new ObservableCollection<VmTaskResultSummaryRow>();
        private readonly ObservableCollection<VmTaskResultSummaryRow> dockTechnicalResultRowsS88 = new ObservableCollection<VmTaskResultSummaryRow>();
        private readonly ObservableCollection<VmTrialStabilityMetricOption> currentToolTrialMetricOptionsS88 = new ObservableCollection<VmTrialStabilityMetricOption>();
        private readonly Dictionary<string, string> currentToolTrialMetricByToolS88 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private CurrentToolTrialStabilityHistoryS88 currentToolTrialStabilityHistoryS88;

        private sealed class CurrentToolTrialStabilityHistoryS88
        {
            public VmToolInstance Tool { get; set; }
            public string ToolId { get; set; }
            public string ToolName { get; set; }
            public string MetricPortName { get; set; }
            public string MetricDisplayName { get; set; }
            public int Capacity { get; set; }
            public ObservableCollection<VmTrialStabilitySample> Samples { get; private set; }

            public CurrentToolTrialStabilityHistoryS88()
            {
                Samples = new ObservableCollection<VmTrialStabilitySample>();
            }
        }

        private static readonly Dictionary<VmToolKind, string[]> PreferredResultPortsS88 =
            new Dictionary<VmToolKind, string[]>
            {
                { VmToolKind.ImageSource, new[] { "SN", "Width", "Height", "Path" } },
                { VmToolKind.ImageThreshold, new[] { "Area", "RegionCount", "Threshold" } },
                { VmToolKind.AutomaticThreshold, new[] { "Threshold", "RegionCount", "Area", "Polarity" } },
                { VmToolKind.LocalAdaptiveThreshold, new[] { "RegionCount", "Area", "Offset", "Polarity" } },
                { VmToolKind.Blob, new[] { "BlobCount", "Area", "CenterRow", "CenterColumn" } },
                { VmToolKind.GrayStat, new[] { "Mean", "Deviation", "Minimum", "Maximum" } },
                { VmToolKind.GrayHistogram, new[] { "Mean", "Deviation", "P50", "SampleCount" } },
                { VmToolKind.GrayLineProfile, new[] { "Mean", "Deviation", "Range", "SampleCount" } },
                { VmToolKind.BranchValueSelection, new[] { "SelectedBranch", "Value", "SelectedResultCode" } },
                { VmToolKind.SubflowCall, new[] { VmSubflowLoopS111.Completed, VmSubflowLoopS111.Planned, "ExecutedCount" } },
                { VmToolKind.EdgeMeasure, new[] { "EdgeCount", "MeanAmplitude", "Distance" } },
                { VmToolKind.ShapeMatch, new[] { "MatchCount", "HasSelection", "SelectedIndex", "SelectedScore" } },
                { VmToolKind.DataCode2DRecognition, new[] { "DecodedText", "CodeCount", "CenterRow", "CenterColumn" } },
                { VmToolKind.BarCode1DRecognition, new[] { "DecodedText", "CodeCount", "CenterRow", "CenterColumn" } },
                { VmToolKind.OcrRecognition, new[] { "Text", "CharacterCount", "MinimumConfidence", "LowConfidenceCount" } },
                { VmToolKind.NumericJudge, new[] { "Value", "LowerLimit", "UpperLimit" } },
                { VmToolKind.CaliperEdgePairMeasurement, new[] { "PrimaryDistance", "PairCount", "FirstAmplitude", "SecondAmplitude" } },
                { VmToolKind.LinearCaliperArrayMeasurement, new[] { "MeanDistance", "ValidCount", "FailedCount", "PairCount" } },
                { VmToolKind.ImageEdgeLineFindingS143, new[] { "HasLine", "ValidCount", "AngleDegrees", "MaxResidual" } },
                { VmToolKind.ConnectedObjectAnalysisS144, new[] { "Count", "HasSelection", "SelectedIndex", "SelectedArea" } },
                { VmToolKind.CircularCaliperCircleFinder, new[] { "Radius", "ValidCount", "FailedCount", "MaxResidual" } },
                { VmToolKind.RingWallThicknessMeasurement, new[] { "MeanThickness", "MinThickness", "MaxThickness", "Eccentricity" } },
                { VmToolKind.TextAssembly, new[] { "Text", "Length", "InputCount" } },
                { VmToolKind.TextParser, new[] { "Text", "Number", "FieldCount" } },
                { VmToolKind.NumericCalculation, new[] { "Number", "OperandCount", "Operation" } },
                { VmToolKind.NumberArrayCreate, new[] { "Count", "Array" } },
                { VmToolKind.TextArraySplit, new[] { "Count", "Array" } },
                { VmToolKind.NumberArrayStatistics, new[] { "Count", "Mean", "SelectedValue", "Index" } },
                { VmToolKind.TextArraySelect, new[] { "Text", "Count", "Index", "Length" } },
                { VmToolKind.NumericExpression, new[] { "Number", "VariableCount" } },
                { VmToolKind.NumberArrayReorder, new[] { "Count", "Array", "Operation" } },
                { VmToolKind.TextArrayReorder, new[] { "Count", "Array", "Operation" } },
                { VmToolKind.ResultLogicAggregation, new[] { "IsOk", "InputCount", "PassedCount" } },
                { VmToolKind.CSharpDllScript, new[] { "Number", "Text", "IsOk", "ElapsedMilliseconds" } },
                { VmToolKind.TcpClientExchange, new[] { "ReceivedText", "BytesReceived", "RoundTripMs" } },
                { VmToolKind.ModbusTcpExchange, new[] { "Value", "DecodedText", "DecodeSummary", "Count" } },
                { VmToolKind.ModbusRtuExchange, new[] { "Value", "DecodedText", "DecodeSummary", "Count" } },
                { VmToolKind.PositionCorrection2D, new[] { "DeltaRow", "DeltaColumn", "DeltaAngle", "Score" } },
                { VmToolKind.PlanarCalibration2D, new[] { "WorldDistance", "RmsResidual", "MaxResidual", "SampleCount" } },
                { VmToolKind.CameraDistortionCorrection, new[] { "Width", "Height", "MappedObjectCount", "CalibrationRmsPx" } },
                { VmToolKind.ColorInspectionHsv, new[] { "AreaRatio", "Area", "RegionCount", "HueCenter" } },
                { VmToolKind.VariationSurfaceInspection, new[] { "Area", "RegionCount", "AreaRatio", "SampleCount" } },
                { VmToolKind.NccTemplateMatch, new[] { "MatchCount", "HasSelection", "SelectedIndex", "SelectedScore" } },
                { VmToolKind.PoseImageAlignment, new[] { "EffectivePixels", "TranslationRow", "TranslationColumn", "RotationDegrees" } },
                { VmToolKind.WorldPointPairMeasurement, new[] { "WorldDistance", "DirectionAngle", "Unit" } }
            };

        private void InitializeResultTaskAndStabilityS88()
        {
            if (DockTaskResultItemsControlS88 != null) DockTaskResultItemsControlS88.ItemsSource = dockTaskResultRowsS88;
            if (DockTechnicalResultItemsControlS88 != null) DockTechnicalResultItemsControlS88.ItemsSource = dockTechnicalResultRowsS88;
            if (DockCurrentToolTrialMetricComboBoxS88 != null)
            {
                DockCurrentToolTrialMetricComboBoxS88.ItemsSource = currentToolTrialMetricOptionsS88;
                DockCurrentToolTrialMetricComboBoxS88.DisplayMemberPath = "DisplayText";
                DockCurrentToolTrialMetricComboBoxS88.SelectedValuePath = "PortName";
            }
            if (DockCurrentToolTrialWindowComboBoxS88 != null)
            {
                DockCurrentToolTrialWindowComboBoxS88.ItemsSource = new[] { 10, 25, 50, 100 };
                DockCurrentToolTrialWindowComboBoxS88.SelectedItem = 25;
            }
            RefreshDockTaskResultSummaryS88(null);
            RefreshCurrentToolTrialStabilityOptionsS88(null);
            RefreshCurrentToolTrialStabilityUiS88();
        }

        private void RefreshDockTaskResultSummaryS88(VmToolInstance tool)
        {
            RefreshHistogramResultS91(tool);
            RefreshProfileResultS92(tool);
            dockTaskResultRowsS88.Clear();
            dockTechnicalResultRowsS88.Clear();
            if (DockResultVerdictTextS88 == null) return;

            if (tool == null)
            {
                DockResultOriginTextS88.Text = "尚无执行结果";
                DockResultVerdictTextS88.Text = "--";
                DockResultElapsedTextS88.Text = "尚未选择模块";
                DockResultGraphicTextS88.Text = "图形：--";
                DockResultTaskErrorTextS88.Text = "先双击流程模块，再运行查看结果。";
                DockResultTaskErrorTextS88.Visibility = Visibility.Visible;
                DockResultTaskErrorBorderS88.Visibility = Visibility.Visible;
                SetDockResultVerdictStyleS88("--");
                return;
            }

            DockResultVerdictTextS88.Text = !string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "异常" : string.IsNullOrWhiteSpace(tool.ResultCode) ? "--" : tool.ResultCode;
            DockResultOriginTextS88.Text = BuildResultOriginTextS88(tool);
            DockResultElapsedTextS88.Text = tool.ResultCode == "--" ? "等待运行" : "耗时 " + tool.ElapsedText;
            DockResultTaskErrorTextS88.Text = string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "" : tool.ErrorMessage;
            DockResultTaskErrorTextS88.Visibility = string.IsNullOrWhiteSpace(tool.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
            DockResultTaskErrorBorderS88.Visibility = DockResultTaskErrorTextS88.Visibility;
            SetDockResultVerdictStyleS88(tool.ResultCode);

            IList<VmPortDefinition> ports = ToolMetadata.GetInstanceOutputPorts(tool);
            List<VmTaskResultSummaryRow> all = ports.Select(port => BuildTaskResultRowS88(tool, port)).ToList();
            foreach (VmTaskResultSummaryRow row in all) dockTechnicalResultRowsS88.Add(row);
            foreach (VmTaskResultSummaryRow row in SelectTaskResultRowsS88(tool, all)) dockTaskResultRowsS88.Add(row);
            if (string.IsNullOrWhiteSpace(tool.ErrorMessage) && all.Any(row => row.StateKey == "Invalid"))
            {
                DockResultTaskErrorTextS88.Text = "输出类型或数值无效：" + string.Join("、", all.Where(row => row.StateKey == "Invalid").Take(4).Select(row => row.PortName));
                DockResultTaskErrorTextS88.Visibility = DockResultTaskErrorBorderS88.Visibility = Visibility.Visible;
                DockResultVerdictTextS88.Text = "输出异常";
                SetDockResultVerdictStyleS88("NG");
            }
            bool hasKeyValue = dockTaskResultRowsS88.Any(row => row.StateKey != "Waiting");
            DockTaskResultItemsControlS88.Visibility = hasKeyValue ? Visibility.Visible : Visibility.Collapsed;
            DockResultKeyHeadingS88.Visibility = DockTaskResultItemsControlS88.Visibility;
            DockResultGraphicTextS88.Text = BuildGraphicResultSummaryS88(tool, ports);
            if (DockResultTechnicalExpanderS88 != null) DockResultTechnicalExpanderS88.Header = "更多 · 全部 " + all.Count.ToString(CultureInfo.InvariantCulture) + " 个输出与明细";
        }

        private static VmTaskResultSummaryRow BuildTaskResultRowS88(VmToolInstance tool, VmPortDefinition port)
        {
            object raw = null;
            bool hasValue = tool != null && tool.TryGetOutputValue(port.PortName, out raw) && raw != null;
            bool validType = !hasValue || IsDisplayScalarTypeValidS88(port.DataType, raw);
            string value = hasValue ? validType ? tool.GetFormattedOutput(port.PortName) : "类型/数值无效" : "--";
            return new VmTaskResultSummaryRow
            {
                PortName = port.PortName,
                DisplayName = string.IsNullOrWhiteSpace(port.DisplayName) ? port.PortName : port.DisplayName,
                DataType = port.DataType,
                ValueText = ShortResultValueS88(value, 92),
                StateKey = hasValue ? validType ? "Ready" : "Invalid" : "Waiting",
                DetailText = port.PortName + " · " + port.DataType + " · " + value
            };
        }

        private static bool IsDisplayScalarTypeValidS88(string type, object raw)
        {
            if (type == "Boolean") return raw is bool;
            if (type == "String" || type == "ResultCode") return raw is string;
            if (type != "Number") return true; // Geometry/arrays retain their existing presenters and contracts.
            if (!(raw is byte || raw is sbyte || raw is short || raw is ushort || raw is int || raw is uint ||
                raw is long || raw is ulong || raw is float || raw is double || raw is decimal)) return false;
            double number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private static string BuildResultOriginTextS88(VmToolInstance tool)
        {
            if (tool == null || string.IsNullOrWhiteSpace(tool.ResultCode) || tool.ResultCode == "--")
                return "尚无有效结果 · 请运行当前模块";
            string origin = tool.RuntimeResultOrigin == VmRuntimeResultOrigin.DraftTrial
                ? "最近草稿试运行 · 不代表已应用参数"
                : tool.RuntimeResultOrigin == VmRuntimeResultOrigin.ModuleExecution
                    ? "最近模块执行 · 不代表整流程合格"
                    : "执行来源未标注 · 请重新运行确认";
            return tool.LastOutputUtc.HasValue ? origin + " · " + tool.LastOutputUtc.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) : origin;
        }

        private string BuildBriefTaskValuesS88(VmToolInstance tool)
        {
            if (tool == null || tool.ResultCode == "--") return "尚无有效输出";
            var all = ToolMetadata.GetInstanceOutputPorts(tool).Select(port => BuildTaskResultRowS88(tool, port)).ToList();
            var keys = SelectTaskResultRowsS88(tool, all).Where(row => row.StateKey != "Waiting").Take(3).ToList();
            return keys.Count == 0 ? "暂无可用输出" : string.Join(" · ", keys.Select(row => row.DisplayName + " " + row.ValueText));
        }

        private static string ShortResultValueS88(string value, int maximumLength)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
            return text.Length <= maximumLength ? text : text.Substring(0, Math.Max(1, maximumLength - 1)) + "…";
        }

        private IList<VmTaskResultSummaryRow> SelectTaskResultRowsS88(VmToolInstance tool, IList<VmTaskResultSummaryRow> rows)
        {
            string[] preferred;
            PreferredResultPortsS88.TryGetValue(tool.Kind, out preferred);
            if (tool.Kind == VmToolKind.SubflowCall)
                preferred = new[] { VmSubflowLoopS111.Completed, VmSubflowLoopS111.Planned, "ExecutedCount" };
            if (tool.Kind == VmToolKind.JsonRead)
                preferred = VmStructuredDataS110.Fields(tool.Parameters ?? new VmToolParameterData()).Where(field => field != null).Take(3).Select(field => field.PortName).Concat(new[] { "FieldCount" }).ToArray();
            if (tool.Kind == VmToolKind.JsonWrite) preferred = new[] { "FieldCount", "Length", "Json" };
            if (tool.Kind == VmToolKind.GlobalVariableRead || tool.Kind == VmToolKind.GlobalVariableWrite)
            {
                var variable = globalVariables.FirstOrDefault(item => tool.Parameters != null && string.Equals(item.VariableId, tool.Parameters.GlobalVariableId, StringComparison.OrdinalIgnoreCase));
                preferred = variable == null ? new[] { "VariableName", "ValueType", "Changed" }
                    : new[] { VmGlobalVariableType.GetWritePortName(variable.ValueType), "VariableName", "Changed", "ValueType" };
                rows = rows.Where(row => !new[] { "NumberValue", "TextValue", "BooleanValue", "ResultCodeValue", "NumberArrayValue", "StringArrayValue" }.Contains(row.PortName)
                    || preferred != null && preferred.Contains(row.PortName)).ToList();
            }
            List<VmTaskResultSummaryRow> candidates = rows
                .Where(row => !string.Equals(row.PortName, "ResultCode", StringComparison.OrdinalIgnoreCase) && !IsGraphicResultTypeS88(row.DataType))
                .OrderByDescending(row => GetTaskResultPriorityS88(row, preferred))
                .ThenBy(row => rows.IndexOf(row))
                .ToList();
            List<VmTaskResultSummaryRow> selected = candidates.Where(row => GetTaskResultPriorityS88(row, preferred) > -500).Take(4).ToList();
            if (selected.Count < 2)
            {
                foreach (VmTaskResultSummaryRow row in candidates.Where(row => !selected.Contains(row)))
                {
                    selected.Add(row);
                    if (selected.Count >= 2) break;
                }
            }
            if (selected.Count < 2)
            {
                foreach (VmTaskResultSummaryRow row in rows.Where(row => IsGraphicResultTypeS88(row.DataType) && !selected.Contains(row)))
                {
                    selected.Add(row);
                    if (selected.Count >= 2) break;
                }
            }
            if (selected.Count < 2)
            {
                VmTaskResultSummaryRow resultCode = rows.FirstOrDefault(row => string.Equals(row.PortName, "ResultCode", StringComparison.OrdinalIgnoreCase));
                if (resultCode != null && !selected.Contains(resultCode)) selected.Add(resultCode);
            }
            return selected.Take(4).ToList();
        }

        private static int GetTaskResultPriorityS88(VmTaskResultSummaryRow row, string[] preferred)
        {
            if (preferred != null)
            {
                int explicitIndex = Array.FindIndex(preferred, item => string.Equals(item, row.PortName, StringComparison.OrdinalIgnoreCase));
                if (explicitIndex >= 0) return 1000 - explicitIndex * 10;
            }
            string name = row.PortName ?? string.Empty;
            if (name.IndexOf("Parameter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Diagnostic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Context", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Detail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Indices", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Points", StringComparison.OrdinalIgnoreCase) >= 0) return -1000;
            int score = string.Equals(row.DataType, "Number", StringComparison.OrdinalIgnoreCase) ? 80 : 40;
            if (row.StateKey == "Ready") score += 20;
            string[] important = { "Text", "Score", "Count", "Area", "Distance", "Radius", "Thickness", "Mean", "Value", "Angle", "Width", "Height", "Row", "Column", "Deviation", "Residual", "IsOk" };
            for (int index = 0; index < important.Length; index++)
            {
                if (name.IndexOf(important[index], StringComparison.OrdinalIgnoreCase) >= 0) score += 160 - index * 4;
            }
            return score;
        }

        private static bool IsGraphicResultTypeS88(string dataType)
        {
            return string.Equals(dataType, "Image", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Region", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "XLD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Pose2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Point2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Point2D[]", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Line2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Circle2D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Ellipse2D", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildGraphicResultSummaryS88(VmToolInstance tool, IEnumerable<VmPortDefinition> ports)
        {
            int outputCount = 0;
            int objectCount = 0;
            foreach (VmPortDefinition port in ports.Where(port => IsGraphicResultTypeS88(port.DataType)))
            {
                object raw;
                if (!tool.TryGetOutputValue(port.PortName, out raw) || raw == null) continue;
                outputCount++;
                VmRegionSnapshot region = raw as VmRegionSnapshot;
                VmXldSnapshot xld = raw as VmXldSnapshot;
                ICollection collection = raw as ICollection;
                if (region != null) objectCount += Math.Max(0, region.ObjectCount);
                else if (xld != null) objectCount += Math.Max(0, xld.ObjectCount);
                else if (collection != null) objectCount += Math.Max(0, collection.Count);
                else objectCount++;
            }
            return outputCount == 0
                ? "图形：无有效对象"
                : "图形：" + outputCount.ToString(CultureInfo.InvariantCulture) + " 项 · 对象 " + objectCount.ToString(CultureInfo.InvariantCulture);
        }

        private void SetDockResultVerdictStyleS88(string resultCode)
        {
            bool ok = string.Equals(resultCode, "OK", StringComparison.OrdinalIgnoreCase);
            bool bad = string.Equals(resultCode, "NG", StringComparison.OrdinalIgnoreCase) || string.Equals(resultCode, "ERROR", StringComparison.OrdinalIgnoreCase);
            DockResultVerdictBorderS88.Background = new SolidColorBrush(ok ? Color.FromRgb(220, 252, 231) : bad ? Color.FromRgb(254, 226, 226) : Color.FromRgb(241, 245, 249));
            DockResultVerdictTextS88.Foreground = new SolidColorBrush(ok ? Color.FromRgb(21, 128, 61) : bad ? Color.FromRgb(185, 28, 28) : Color.FromRgb(71, 85, 105));
        }

        private void RefreshCurrentToolTrialStabilityOptionsS88(VmToolInstance tool)
        {
            if (DockCurrentToolTrialMetricComboBoxS88 == null) return;
            string previous = DockCurrentToolTrialMetricComboBoxS88.SelectedValue as string;
            currentToolTrialMetricOptionsS88.Clear();
            if (tool != null)
            {
                string[] preferred;
                PreferredResultPortsS88.TryGetValue(tool.Kind, out preferred);
                IEnumerable<VmPortDefinition> numeric = ToolMetadata.GetInstanceNumericOutputPorts(tool)
                    .OrderByDescending(port => preferred == null ? 0 : Array.FindIndex(preferred, item => string.Equals(item, port.PortName, StringComparison.OrdinalIgnoreCase)) >= 0 ? 1 : 0)
                    .ThenBy(port => port.DisplayName);
                foreach (VmPortDefinition port in numeric)
                {
                    currentToolTrialMetricOptionsS88.Add(new VmTrialStabilityMetricOption
                    {
                        PortName = port.PortName,
                        DisplayText = (string.IsNullOrWhiteSpace(port.DisplayName) ? port.PortName : port.DisplayName) + " · " + port.PortName
                    });
                }
            }
            string saved;
            if (tool != null && currentToolTrialMetricByToolS88.TryGetValue(tool.ToolId, out saved)) previous = saved;
            if (!currentToolTrialMetricOptionsS88.Any(option => string.Equals(option.PortName, previous, StringComparison.OrdinalIgnoreCase)))
                previous = currentToolTrialMetricOptionsS88.Select(option => option.PortName).FirstOrDefault();
            DockCurrentToolTrialMetricComboBoxS88.SelectedValue = previous;
            DockCurrentToolTrialMetricComboBoxS88.IsEnabled = tool != null && currentToolTrialMetricOptionsS88.Count > 0 && !IsCurrentToolContinuousTrialRunningS87;
        }

        private void DockCurrentToolTrialMetricComboBox_SelectionChangedS88(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DockCurrentToolTrialMetricComboBoxS88 == null) return;
            if (IsCurrentToolContinuousTrialRunningS87 && currentToolTrialStabilityHistoryS88 != null)
            {
                DockCurrentToolTrialMetricComboBoxS88.SelectedValue = currentToolTrialStabilityHistoryS88.MetricPortName;
                return;
            }
            string selected = DockCurrentToolTrialMetricComboBoxS88.SelectedValue as string;
            if (dockEditingTool != null && !string.IsNullOrWhiteSpace(selected)) currentToolTrialMetricByToolS88[dockEditingTool.ToolId] = selected;
        }

        private void DockCurrentToolTrialWindowComboBox_SelectionChangedS88(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsCurrentToolContinuousTrialRunningS87 && currentToolTrialStabilityHistoryS88 != null)
                DockCurrentToolTrialWindowComboBoxS88.SelectedItem = currentToolTrialStabilityHistoryS88.Capacity;
        }

        private void BeginCurrentToolTrialStabilityS88(VmToolInstance tool)
        {
            RefreshCurrentToolTrialStabilityOptionsS88(tool);
            string metric = DockCurrentToolTrialMetricComboBoxS88.SelectedValue as string;
            VmTrialStabilityMetricOption option = currentToolTrialMetricOptionsS88.FirstOrDefault(item => string.Equals(item.PortName, metric, StringComparison.OrdinalIgnoreCase));
            int capacity = DockCurrentToolTrialWindowComboBoxS88.SelectedItem == null ? 25 : Convert.ToInt32(DockCurrentToolTrialWindowComboBoxS88.SelectedItem, CultureInfo.InvariantCulture);
            currentToolTrialStabilityHistoryS88 = new CurrentToolTrialStabilityHistoryS88
            {
                Tool = tool,
                ToolId = tool.ToolId,
                ToolName = tool.InstanceName,
                MetricPortName = metric,
                MetricDisplayName = option == null ? "未选择 Number" : option.DisplayText,
                Capacity = Math.Max(10, Math.Min(100, capacity))
            };
            DockCurrentToolTrialStabilityGridS88.ItemsSource = currentToolTrialStabilityHistoryS88.Samples;
            RefreshCurrentToolTrialStabilityUiS88();
        }

        private void RecordCurrentToolTrialStabilityS88(VmToolInstance tool, int cycle, string resultCode, double elapsedMilliseconds, string errorMessage)
        {
            CurrentToolTrialStabilityHistoryS88 history = currentToolTrialStabilityHistoryS88;
            if (history == null || tool == null || !ReferenceEquals(history.Tool, tool)) return;
            double value;
            double? metric = !string.IsNullOrWhiteSpace(history.MetricPortName) && tool.TryGetNumericOutput(history.MetricPortName, out value) && !double.IsNaN(value) && !double.IsInfinity(value)
                ? (double?)value
                : null;
            history.Samples.Add(new VmTrialStabilitySample
            {
                Cycle = cycle,
                Timestamp = DateTime.Now,
                ResultCode = string.IsNullOrWhiteSpace(resultCode) ? "--" : resultCode,
                ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds),
                MetricPortName = history.MetricPortName,
                MetricValue = metric,
                ErrorMessage = errorMessage ?? string.Empty
            });
            while (history.Samples.Count > history.Capacity) history.Samples.RemoveAt(0);
            RefreshCurrentToolTrialStabilityUiS88();
            if (DockCurrentToolTrialStabilityGridS88.Items.Count > 0)
                DockCurrentToolTrialStabilityGridS88.ScrollIntoView(DockCurrentToolTrialStabilityGridS88.Items[DockCurrentToolTrialStabilityGridS88.Items.Count - 1]);
        }

        private void RefreshCurrentToolTrialStabilityUiS88()
        {
            if (DockCurrentToolTrialStabilitySummaryTextS88 == null) return;
            CurrentToolTrialStabilityHistoryS88 history = currentToolTrialStabilityHistoryS88;
            bool running = IsCurrentToolContinuousTrialRunningS87;
            DockCurrentToolTrialMetricComboBoxS88.IsEnabled = !running && dockEditingTool != null && currentToolTrialMetricOptionsS88.Count > 0;
            DockCurrentToolTrialWindowComboBoxS88.IsEnabled = !running;
            if (history == null || history.Samples.Count == 0)
            {
                DockCurrentToolTrialStabilitySummaryTextS88.Text = "尚无周期样本 · 启动连续试运行后记录当前调试会话";
                DockCurrentToolTrialStabilityAnomalyTextS88.Text = "最近异常：无";
                DockCurrentToolTrialStabilityGridS88.ItemsSource = history == null ? null : history.Samples;
                return;
            }

            IList<VmTrialStabilitySample> samples = history.Samples.ToList();
            double elapsedMin = samples.Min(item => item.ElapsedMilliseconds);
            double elapsedMax = samples.Max(item => item.ElapsedMilliseconds);
            double elapsedMean = samples.Average(item => item.ElapsedMilliseconds);
            IList<double> metrics = samples.Where(item => item.MetricValue.HasValue).Select(item => item.MetricValue.Value).ToList();
            string metricText = metrics.Count == 0
                ? " · " + history.MetricDisplayName + "：无有效值"
                : string.Format(CultureInfo.InvariantCulture, " · {0}：Min {1:0.###} / Max {2:0.###} / Mean {3:0.###} / 波动 {4:0.###}", history.MetricDisplayName, metrics.Min(), metrics.Max(), metrics.Average(), metrics.Max() - metrics.Min());
            DockCurrentToolTrialStabilitySummaryTextS88.Text = string.Format(
                CultureInfo.InvariantCulture,
                "窗口 {0}/{1} · OK {2} / NG {3} / 异常 {4} · 耗时 Min {5:0.0} / Max {6:0.0} / Mean {7:0.0} ms{8}",
                samples.Count,
                history.Capacity,
                samples.Count(item => string.Equals(item.ResultCode, "OK", StringComparison.OrdinalIgnoreCase)),
                samples.Count(item => string.Equals(item.ResultCode, "NG", StringComparison.OrdinalIgnoreCase)),
                samples.Count(item => !string.IsNullOrWhiteSpace(item.ErrorMessage) || string.Equals(item.ResultCode, "ERROR", StringComparison.OrdinalIgnoreCase)),
                elapsedMin,
                elapsedMax,
                elapsedMean,
                metricText);
            int skipped = samples.Count(item => string.Equals(item.ResultCode, "SKIP", StringComparison.OrdinalIgnoreCase));
            if (skipped > 0) DockCurrentToolTrialStabilitySummaryTextS88.Text += " · 跳过 " + skipped.ToString(CultureInfo.InvariantCulture);
            VmTrialStabilitySample abnormal = samples.LastOrDefault(item =>
                (!string.Equals(item.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && !string.Equals(item.ResultCode, "SKIP", StringComparison.OrdinalIgnoreCase)) || !string.IsNullOrWhiteSpace(item.ErrorMessage));
            DockCurrentToolTrialStabilityAnomalyTextS88.Text = abnormal == null
                ? "最近异常：无"
                : "最近异常：#" + abnormal.Cycle.ToString(CultureInfo.InvariantCulture) + " · " + abnormal.ResultCode + (string.IsNullOrWhiteSpace(abnormal.ErrorMessage) ? string.Empty : " · " + abnormal.ErrorMessage);
        }

        private void ClearCurrentToolTrialStabilityButton_ClickS88(object sender, RoutedEventArgs e)
        {
            ClearCurrentToolTrialStabilityS88("用户清空", true);
        }

        private void ClearCurrentToolTrialStabilityS88(string reason, bool keepSession)
        {
            if (currentToolTrialStabilityHistoryS88 != null) currentToolTrialStabilityHistoryS88.Samples.Clear();
            if (!keepSession) currentToolTrialStabilityHistoryS88 = null;
            if (DockCurrentToolTrialStabilityGridS88 != null) DockCurrentToolTrialStabilityGridS88.ItemsSource = currentToolTrialStabilityHistoryS88 == null ? null : currentToolTrialStabilityHistoryS88.Samples;
            RefreshCurrentToolTrialStabilityUiS88();
            if (!string.IsNullOrWhiteSpace(reason) && HeaderStatusText != null) HeaderStatusText.Text = "稳定性窗口已清理：" + reason;
        }

        private void ClearCurrentToolTrialStabilityForContextS88(VmToolInstance nextTool, string reason)
        {
            if (currentToolTrialStabilityHistoryS88 == null) return;
            if (nextTool != null && ReferenceEquals(nextTool, currentToolTrialStabilityHistoryS88.Tool)) return;
            ClearCurrentToolTrialStabilityS88(reason, false);
        }

        private void ExportCurrentToolTrialStabilityButton_ClickS88(object sender, RoutedEventArgs e)
        {
            if (currentToolTrialStabilityHistoryS88 == null || currentToolTrialStabilityHistoryS88.Samples.Count == 0)
            {
                HeaderStatusText.Text = "当前调试会话没有可导出的稳定性样本。";
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "导出当前模块稳定性窗口",
                Filter = "CSV 文件 (*.csv)|*.csv",
                FileName = "trial-stability-" + SanitizeFileNameS88(currentToolTrialStabilityHistoryS88.ToolName) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".csv"
            };
            if (dialog.ShowDialog(this) != true) return;
            ExportCurrentToolTrialStabilityCsvS88(dialog.FileName);
            HeaderStatusText.Text = "稳定性窗口已导出：" + dialog.FileName;
        }

        private string ExportCurrentToolTrialStabilityCsvS88(string path)
        {
            CurrentToolTrialStabilityHistoryS88 history = currentToolTrialStabilityHistoryS88;
            if (history == null || history.Samples.Count == 0) throw new InvalidOperationException("当前调试会话没有稳定性样本。");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("导出路径不能为空。", "path");
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Cycle,Timestamp,ResultCode,ElapsedMs,MetricPort,MetricValue,Error");
            foreach (VmTrialStabilitySample sample in history.Samples)
            {
                csv.Append(sample.Cycle.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(CsvValueS88(sample.Timestamp.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                    .Append(CsvValueS88(sample.ResultCode)).Append(',')
                    .Append(sample.ElapsedMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                    .Append(CsvValueS88(sample.MetricPortName)).Append(',')
                    .Append(sample.MetricValue.HasValue ? sample.MetricValue.Value.ToString("0.###############", CultureInfo.InvariantCulture) : string.Empty).Append(',')
                    .Append(CsvValueS88(sample.ErrorMessage)).AppendLine();
            }
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            return path;
        }

        private static string CsvValueS88(string value)
        {
            string text = value ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string SanitizeFileNameS88(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "tool" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars()) text = text.Replace(invalid, '_');
            return text;
        }
    }
}
