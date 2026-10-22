using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool flowTaskLensModeS76;

        private void FlowCardDisplayMode_CheckedS76(object sender, RoutedEventArgs e)
        {
            if (FlowTaskLensModeRadioS76 == null) return;
            flowTaskLensModeS76 = FlowTaskLensModeRadioS76.IsChecked == true;
            if (!uiReady) return;
            RefreshFlowGraphLayout();
            int compact = flowTools.Count(item => item.IsTaskLensCompactS76);
            HeaderStatusText.Text = flowTaskLensModeS76
                ? string.Format(CultureInfo.InvariantCulture, "任务透镜：{0} 个当前/相关步骤完整显示，{1} 个无关步骤压成状态行；执行与配方未改变。", flowTools.Count - compact, compact)
                : string.Format(CultureInfo.InvariantCulture, "固定紧凑：{0} 个步骤保持位置与卡片高度，端口按需展开；执行与配方未改变。", flowTools.Count);
        }

        private void UpdateFlowTaskLensStatesS76()
        {
            if (!flowTaskLensModeS76)
            {
                foreach (VmToolInstance item in flowTools) item.IsTaskLensCompactS76 = false;
                return;
            }

            HashSet<string> focus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<VmToolInstance> selected = flowTools.Where(item => item.IsFlowSelected).ToList();
            VmToolInstance primary = FlowToolList == null ? null : FlowToolList.SelectedItem as VmToolInstance;
            if (primary != null && !selected.Contains(primary)) selected.Add(primary);
            if (selected.Count == 0 && flowTools.Count > 0) selected.Add(flowTools[0]);
            foreach (VmToolInstance item in selected)
            {
                focus.Add(item.ToolId);
                foreach (VmToolInputBindingData binding in item.InputBindings)
                {
                    if (binding != null && !string.IsNullOrWhiteSpace(binding.SourceToolId)) focus.Add(binding.SourceToolId);
                }
                foreach (VmToolInstance consumer in flowTools.Where(candidate => candidate.InputBindings.Any(binding => binding != null && string.Equals(binding.SourceToolId, item.ToolId, StringComparison.OrdinalIgnoreCase))))
                    focus.Add(consumer.ToolId);
            }

            string sourceToolId;
            string sourcePortName;
            string targetToolId;
            string targetPortName;
            if (TryParseFlowConnectionKey(selectedFlowConnectionKey, out sourceToolId, out sourcePortName, out targetToolId, out targetPortName))
            {
                focus.Add(sourceToolId);
                focus.Add(targetToolId);
            }

            foreach (VmToolInstance item in flowTools)
            {
                bool requiresAttention = item.ShowFlowDetails || item.IsBreakpoint || item.IsExecutionPointer ||
                    string.Equals(item.ResultCode, "NG", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(item.ErrorMessage) ||
                    string.Equals(item.RunStatus, "异常", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.RunStatus, "已停止", StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(debugLastExecutedToolId) && string.Equals(debugLastExecutedToolId, item.ToolId, StringComparison.OrdinalIgnoreCase));
                item.IsTaskLensCompactS76 = !requiresAttention && !focus.Contains(item.ToolId);
            }
        }

        private void RefreshLineEllipseIntersectionConnectionStatusS76(VmToolInstance tool)
        {
            VmToolInputBindingData lineBinding = tool == null ? null : tool.GetInputBinding("Line");
            VmToolInputBindingData ellipseBinding = tool == null ? null : tool.GetInputBinding("Ellipse");
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            VmToolInstance ellipseSource = GetInputSourceTool(ellipseBinding);
            object lineRaw;
            object ellipseRaw;
            bool lineReady = lineSource != null && string.Equals(lineSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && lineSource.TryGetOutputValue(lineBinding.SourcePortName, out lineRaw) && lineRaw is VmLine2D;
            bool ellipseReady = ellipseSource != null && string.Equals(ellipseSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && ellipseSource.TryGetOutputValue(ellipseBinding.SourcePortName, out ellipseRaw) && ellipseRaw is VmEllipse2D;
            tool.ConfigurationStatus = lineBinding == null || ellipseBinding == null ? "待连接几何" : (lineReady && ellipseReady ? "就绪" : "等待上游");
            tool.ConnectionStatus = lineBinding == null || ellipseBinding == null ? "Line 与 Ellipse 必须连接" : (lineReady && ellipseReady ? "Line2D + Ellipse2D 已连接 · 有值" : "几何输入已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildLineEllipseIntersectionInputSummaryS76(tool) + " → Point2D[] / Relation / ChordLength / XLD";
        }

        private string BuildLineEllipseIntersectionInputSummaryS76(VmToolInstance tool)
        {
            VmToolInputBindingData lineBinding = tool == null ? null : tool.GetInputBinding("Line");
            VmToolInputBindingData ellipseBinding = tool == null ? null : tool.GetInputBinding("Ellipse");
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            VmToolInstance ellipseSource = GetInputSourceTool(ellipseBinding);
            string line = lineSource == null || lineBinding == null ? "Line2D←未连接" : lineSource.InstanceName + "." + lineBinding.SourcePortName;
            string ellipse = ellipseSource == null || ellipseBinding == null ? "Ellipse2D←未连接" : ellipseSource.InstanceName + "." + ellipseBinding.SourcePortName;
            return line + " + " + ellipse;
        }

        private InspectionRecord RunLineEllipseIntersectionToolS76(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            string parameterError = parameters.Validate(VmToolKind.LineEllipseIntersectionMeasurement);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);
            VmToolInputBindingData lineBinding = tool.GetInputBinding("Line");
            VmToolInputBindingData ellipseBinding = tool.GetInputBinding("Ellipse");
            if (lineBinding == null || ellipseBinding == null) throw new InvalidOperationException("直线段与椭圆交点工具必须连接 Line (Line2D) 与 Ellipse (Ellipse2D)。");
            string lineBindingError = GetInputBindingConfigurationError(tool, lineBinding);
            string ellipseBindingError = GetInputBindingConfigurationError(tool, ellipseBinding);
            if (!string.IsNullOrWhiteSpace(lineBindingError)) throw new InvalidOperationException(lineBindingError);
            if (!string.IsNullOrWhiteSpace(ellipseBindingError)) throw new InvalidOperationException(ellipseBindingError);
            VmToolInstance lineSource = GetInputSourceTool(lineBinding);
            VmToolInstance ellipseSource = GetInputSourceTool(ellipseBinding);
            if (lineSource == null || ellipseSource == null || !string.Equals(lineSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) || !string.Equals(ellipseSource.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Line2D 或 Ellipse2D 上游尚未在本周期产生 OK 结果，禁止使用未运行、旧值或失效值。");
            object lineRaw;
            object ellipseRaw;
            if (!lineSource.TryGetOutputValue(lineBinding.SourcePortName, out lineRaw) || !(lineRaw is VmLine2D)) throw new InvalidOperationException("Line 输入没有类型化 Line2D 运行值。");
            if (!ellipseSource.TryGetOutputValue(ellipseBinding.SourcePortName, out ellipseRaw) || !(ellipseRaw is VmEllipse2D)) throw new InvalidOperationException("Ellipse 输入没有类型化 Ellipse2D 运行值。");
            VmLine2D line = ((VmLine2D)lineRaw).Clone();
            VmEllipse2D ellipse = ((VmEllipse2D)ellipseRaw).Clone();
            string lineError = line.GetValidationError();
            string ellipseError = ellipse.GetValidationError();
            if (!string.IsNullOrWhiteSpace(lineError)) throw new InvalidOperationException(lineError);
            if (!string.IsNullOrWhiteSpace(ellipseError)) throw new InvalidOperationException(ellipseError);
            if (!string.Equals(line.CoordinateContext, ellipse.CoordinateContext, StringComparison.Ordinal) || !string.Equals(line.CoordinateContextId, ellipse.CoordinateContextId, StringComparison.Ordinal))
                throw new InvalidOperationException("Line2D 与 Ellipse2D 坐标上下文不一致，禁止跨图像求交。");

            HObject overlays = null;
            HObject lineContour = null;
            HObject ellipseContour = null;
            HObject crosses = null;
            HTuple rowsTuple = null;
            HTuple columnsTuple = null;
            HTuple isOverlappingTuple = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out lineContour,
                    new HTuple(new[] { line.StartRow, line.EndRow }), new HTuple(new[] { line.StartColumn, line.EndColumn }));
                HOperatorSet.GenEllipseContourXld(out ellipseContour, ellipse.CenterRow, ellipse.CenterColumn, ellipse.PhiRadians,
                    ellipse.Radius1, ellipse.Radius2, 0.0, Math.PI * 2.0, "positive", parameters.LineEllipseContourResolution);
                HOperatorSet.IntersectionContoursXld(lineContour, ellipseContour, "all", out rowsTuple, out columnsTuple, out isOverlappingTuple);
                if (rowsTuple == null || columnsTuple == null || rowsTuple.Length != columnsTuple.Length)
                    throw new InvalidOperationException("HALCON intersection_contours_xld 返回了不一致的 Row/Column 交点集合。");

                double directionRow = line.EndRow - line.StartRow;
                double directionColumn = line.EndColumn - line.StartColumn;
                double lengthSquared = directionRow * directionRow + directionColumn * directionColumn;
                List<Tuple<double, double, double>> raw = new List<Tuple<double, double, double>>();
                for (int index = 0; index < rowsTuple.Length; index++)
                {
                    double row = rowsTuple[index].D;
                    double column = columnsTuple[index].D;
                    if (double.IsNaN(row) || double.IsInfinity(row) || double.IsNaN(column) || double.IsInfinity(column))
                        throw new InvalidOperationException("HALCON 返回了非有限交点坐标。");
                    double projection = ((row - line.StartRow) * directionRow + (column - line.StartColumn) * directionColumn) / lengthSquared;
                    raw.Add(Tuple.Create(row, column, projection));
                }
                List<Tuple<double, double, double>> ordered = new List<Tuple<double, double, double>>();
                foreach (Tuple<double, double, double> candidate in raw.OrderBy(item => item.Item3))
                {
                    if (!ordered.Any(item => Math.Sqrt(Math.Pow(item.Item1 - candidate.Item1, 2) + Math.Pow(item.Item2 - candidate.Item2, 2)) <= parameters.LineEllipseIntersectionTolerance))
                        ordered.Add(candidate);
                }
                if (ordered.Count > 2) throw new InvalidOperationException("有限线段与非退化椭圆不应产生超过 2 个唯一交点。");
                bool overlapping = isOverlappingTuple != null && isOverlappingTuple.Length > 0 && isOverlappingTuple[0].I != 0;
                double[] rows = ordered.Select(item => item.Item1).ToArray();
                double[] columns = ordered.Select(item => item.Item2).ToArray();
                VmPoint2DArray points = VmPoint2DArray.CreateImagePixel(rows, columns, line.CoordinateContextId);
                int count = points.Count;
                string relation = overlapping ? "Overlapping" : (count == 0 ? "Disjoint" : (count == 1 ? "Tangent" : "Secant"));
                double chordLength = count < 2 ? 0.0 : Math.Sqrt(Math.Pow(rows[1] - rows[0], 2) + Math.Pow(columns[1] - columns[0], 2));
                string resultCode = count == 0 || overlapping ? "NG" : "OK";

                AppendOverlayObject(ref overlays, lineContour);
                AppendOverlayObject(ref overlays, ellipseContour);
                if (count > 0)
                {
                    HOperatorSet.GenCrossContourXld(out crosses, new HTuple(rows), new HTuple(columns), parameters.LineEllipseOverlayCrossSize, Math.PI / 4.0);
                    AppendOverlayObject(ref overlays, crosses);
                }
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("IntersectionPoints", points);
                if (count > 0) tool.SetOutputValue("Point1", points.Points[0].Clone());
                if (count > 1) tool.SetOutputValue("Point2", points.Points[1].Clone());
                tool.SetOutputValue("IntersectionCount", (double)count);
                tool.SetOutputValue("ChordLength", chordLength);
                tool.SetOutputValue("Relation", relation);
                tool.SetOutputValue("CoordinateContext", line.CoordinateContext + ":" + line.CoordinateContextId);
                string parameterText = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=gen_contour_polygon_xld+gen_ellipse_contour_xld+intersection_contours_xld, LineMode=FiniteSegment, Tolerance={0:0.###}px, Resolution={1:0.###}, CrossSize={2:0.###}px",
                    parameters.LineEllipseIntersectionTolerance, parameters.LineEllipseContourResolution, parameters.LineEllipseOverlayCrossSize);
                tool.SetOutputValue("Parameters", parameterText);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayContours(overlays, tool, "XLD", 2 + count, resultCode, chordLength);
                overlays = null;
                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 有限线段-椭圆求交：Relation={0}，Count={1}，ChordLength={2:0.###}px，Line=({3:0.###},{4:0.###})→({5:0.###},{6:0.###})，Ellipse=({7:0.###},{8:0.###},R1={9:0.###},R2={10:0.###},Phi={11:0.###}) · {12}",
                    relation, count, chordLength, line.StartRow, line.StartColumn, line.EndRow, line.EndColumn,
                    ellipse.CenterRow, ellipse.CenterColumn, ellipse.Radius1, ellipse.Radius2, ellipse.PhiRadians, line.CoordinateContextId);
                InspectionRecord record = CreateRecord("LineEllipseIntersectionMeasurement", resultCode, chordLength, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                if (rowsTuple != null) rowsTuple.Dispose();
                if (columnsTuple != null) columnsTuple.Dispose();
                if (isOverlappingTuple != null) isOverlappingTuple.Dispose();
                DisposeObject(crosses);
                DisposeObject(ellipseContour);
                DisposeObject(lineContour);
                DisposeObject(overlays);
            }
        }
    }
}
