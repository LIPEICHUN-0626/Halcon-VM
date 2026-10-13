using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void ToolCatalogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshToolCapabilityPreviewS65(ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem);
            RefreshRecommendationSelectionPreviewS67(ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem);
            RefreshInputSourceCatalogSelectionPreviewS68(ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem);
        }

        private void ToolCapabilityExpander_Expanded(object sender, RoutedEventArgs e)
        {
            RefreshToolCapabilityPreviewS65(ToolCatalogList == null ? null : ToolCatalogList.SelectedItem as VmToolCatalogItem);
        }

        private void RefreshToolCapabilityPreviewS65(VmToolCatalogItem item)
        {
            if (ToolCapabilityNameText == null) return;
            if (item == null)
            {
                ToolCapabilityNameText.Text = "选择一个已实现工具";
                ToolCapabilityMaturityText.Text = "真实目录 · 可拖入";
                ToolCapabilityPurposeText.Text = "选择工具后按需查看由真实 ToolMetadata 生成的用途、端口与图形能力。";
                ToolCapabilityInputText.Text = "IN  --";
                ToolCapabilityOutputText.Text = "OUT --";
                ToolCapabilityGraphicsText.Text = "ROI / 图形 --";
                RefreshToolInsertionPreviewS66(null);
                return;
            }

            IList<VmPortDefinition> inputs = ToolMetadata.GetInputPorts(item.Kind);
            IList<VmPortDefinition> outputs = ToolMetadata.GetOutputPorts(item.Kind);
            ToolCapabilityNameText.Text = item.Name + " · " + item.Category;
            ToolCapabilityMaturityText.Text = item.Kind == VmToolKind.HDevelop ? "已实现 · 兼容入口" : "已实现 · 可拖入";
            ToolCapabilityPurposeText.Text = item.Description;
            ToolCapabilityInputText.Text = "IN  " + FormatCapabilityPortsS65(inputs, true);
            ToolCapabilityOutputText.Text = "OUT " + FormatCapabilityPortsS65(outputs, false);

            List<string> capabilities = new List<string>();
            if (ToolMetadata.SupportsRoi(item.Kind) || ToolMetadata.RequiresRoi(item.Kind))
                capabilities.Add(ToolMetadata.RequiresRoi(item.Kind) ? "必需 ROI" : "可选 ROI/屏蔽区");
            if (outputs.Any(port => string.Equals(port.DataType, "Image", StringComparison.OrdinalIgnoreCase))) capabilities.Add("图像输出");
            if (outputs.Any(port => string.Equals(port.DataType, "Region", StringComparison.OrdinalIgnoreCase))) capabilities.Add("Region 图层");
            if (outputs.Any(port => string.Equals(port.DataType, "XLD", StringComparison.OrdinalIgnoreCase))) capabilities.Add("XLD 图层");
            if (outputs.Any(port => string.Equals(port.DataType, "Pose2D", StringComparison.OrdinalIgnoreCase) || string.Equals(port.DataType, "Point2D", StringComparison.OrdinalIgnoreCase) || string.Equals(port.DataType, "Point2D[]", StringComparison.OrdinalIgnoreCase) || string.Equals(port.DataType, "Line2D", StringComparison.OrdinalIgnoreCase) || string.Equals(port.DataType, "Circle2D", StringComparison.OrdinalIgnoreCase) || string.Equals(port.DataType, "Ellipse2D", StringComparison.OrdinalIgnoreCase))) capabilities.Add("类型化几何");
            ToolCapabilityGraphicsText.Text = "ROI / 图形 " + (capabilities.Count == 0 ? "无专用图形能力" : string.Join(" · ", capabilities));
            RefreshToolInsertionPreviewS66(item);
            RefreshRecommendationSelectionPreviewS67(item);
        }

        private static string FormatCapabilityPortsS65(IEnumerable<VmPortDefinition> ports, bool showOptional)
        {
            List<VmPortDefinition> values = (ports ?? Enumerable.Empty<VmPortDefinition>()).ToList();
            if (values.Count == 0) return "无";
            return string.Join("  ·  ", values.Select(port => port.PortName + ":" + port.DataType + (showOptional ? (port.IsOptional ? "（可选）" : "（必需）") : string.Empty)));
        }

        private string GetImageCoordinateContextIdS65(VmToolInstance tool, string inputPortName)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding(inputPortName);
            if (binding != null && !string.IsNullOrWhiteSpace(binding.SourceToolId) && !string.IsNullOrWhiteSpace(binding.SourcePortName))
                return ResolveOutputCoordinateContextS102(GetInputSourceTool(binding), binding.SourcePortName);
            return "Global.Image:" + (string.IsNullOrWhiteSpace(currentImagePath) ? "Memory" : Path.GetFileName(currentImagePath));
        }

        private void RefreshPointSetGeometryConnectionStatusS65(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            bool hasValue = source != null && binding != null && source.TryGetOutputValue(binding.SourcePortName, out raw) && raw is VmPoint2DArray;
            bool ready = hasValue && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase);
            tool.ConfigurationStatus = binding == null ? "待连接点集" : (ready ? "就绪" : "等待上游");
            tool.ConnectionStatus = binding == null ? "Points 未连接" : (ready ? "Point2D[] 已连接 · 有值" : "Point2D[] 已连接 · 等待本周期 OK");
            tool.ConnectionSummary = BuildPointSetGeometryInputSummaryS65(tool) + " → 质心/边界/跨度/XLD";
        }

        private string BuildPointSetGeometryInputSummaryS65(VmToolInstance tool)
        {
            VmToolInputBindingData binding = tool == null ? null : tool.GetInputBinding("Points");
            VmToolInstance source = GetInputSourceTool(binding);
            return source == null || binding == null ? "Point2D[] ← 未连接" : source.InstanceName + "." + binding.SourcePortName + " (Point2D[])";
        }

        private InspectionRecord RunPointSetGeometryStatisticsToolS65(VmToolInstance tool)
        {
            VmToolParameterData parameters = (tool.Parameters ?? new VmToolParameterData()).Clone();
            parameters.Normalize();
            string parameterError = parameters.Validate(VmToolKind.PointSetGeometryStatistics);
            if (!string.IsNullOrWhiteSpace(parameterError)) throw new InvalidOperationException(parameterError);

            VmToolInputBindingData binding = tool.GetInputBinding("Points");
            if (binding == null) throw new InvalidOperationException("点集几何统计必须连接 Points (Point2D[])。 ");
            string bindingError = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(bindingError)) throw new InvalidOperationException(bindingError);
            VmToolInstance source = GetInputSourceTool(binding);
            if (source == null || !string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("点集上游尚未在本周期产生 OK 结果，禁止使用未运行或失效值。");
            object raw;
            if (!source.TryGetOutputValue(binding.SourcePortName, out raw) || !(raw is VmPoint2DArray))
                throw new InvalidOperationException("Points 输入没有类型化 Point2D[] 运行值；不会解析字符串坐标。 ");

            VmPoint2DArray input = ((VmPoint2DArray)raw).Clone();
            if (!input.IsValid) throw new InvalidOperationException("Point2D[] 已标记为无效。");
            if (!string.Equals(input.CoordinateContext, VmPoint2D.ImagePixelContext, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(input.CoordinateContextId))
                throw new InvalidOperationException("点集当前只接受具备稳定 ContextId 的 ImagePixel 图像像素坐标。");

            bool skipInvalid = string.Equals(parameters.PointSetAbnormalPolicy, VmPointSetAbnormalPolicy.SkipInvalid, StringComparison.OrdinalIgnoreCase);
            List<VmPoint2D> valid = new List<VmPoint2D>();
            int skippedCount = 0;
            foreach (VmPoint2D point in input.Points ?? new List<VmPoint2D>())
            {
                if (point != null && (!string.Equals(point.CoordinateContext, input.CoordinateContext, StringComparison.Ordinal) || !string.Equals(point.CoordinateContextId, input.CoordinateContextId, StringComparison.Ordinal)))
                    throw new InvalidOperationException("Point2D[] 内部坐标上下文不一致，不能跨图像统计。");
                string error = point == null ? "点为空" : point.GetValidationError();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    if (!skipInvalid) throw new InvalidOperationException("Point2D[] 包含异常点：" + error);
                    skippedCount++;
                    continue;
                }
                valid.Add(point.Clone());
            }
            if (valid.Count < parameters.PointSetMinimumCount)
                throw new InvalidOperationException("有效点数量 " + valid.Count.ToString(CultureInfo.InvariantCulture) + " 小于配置的最小点数 " + parameters.PointSetMinimumCount.ToString(CultureInfo.InvariantCulture) + "。");

            double[] rows = valid.Select(point => point.Row).ToArray();
            double[] columns = valid.Select(point => point.Column).ToArray();
            double centroidRow = rows.Average();
            double centroidColumn = columns.Average();
            double minRow = rows.Min();
            double minColumn = columns.Min();
            double maxRow = rows.Max();
            double maxColumn = columns.Max();
            double rowSpan = maxRow - minRow;
            double columnSpan = maxColumn - minColumn;
            double maxSpan = Math.Max(rowSpan, columnSpan);
            HObject overlays = null;
            HObject pointCrosses = null;
            HObject pointPath = null;
            HObject centroidCross = null;
            HObject boundingBox = null;
            int overlayCount = 0;
            try
            {
                HOperatorSet.GenCrossContourXld(out pointCrosses, new HTuple(rows), new HTuple(columns), 7.0, Math.PI / 4.0);
                AppendOverlayObject(ref overlays, pointCrosses); overlayCount++;
                if (valid.Count >= 2)
                {
                    HOperatorSet.GenContourPolygonXld(out pointPath, new HTuple(rows), new HTuple(columns));
                    AppendOverlayObject(ref overlays, pointPath); overlayCount++;
                }
                HOperatorSet.GenCrossContourXld(out centroidCross, centroidRow, centroidColumn, 14.0, 0.0);
                AppendOverlayObject(ref overlays, centroidCross); overlayCount++;
                HOperatorSet.GenContourPolygonXld(out boundingBox,
                    new HTuple(new[] { minRow, minRow, maxRow, maxRow, minRow }),
                    new HTuple(new[] { minColumn, maxColumn, maxColumn, minColumn, minColumn }));
                AppendOverlayObject(ref overlays, boundingBox); overlayCount++;

                string resultCode = skippedCount == 0 ? "OK" : "NG";
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(overlays, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("Centroid", VmPoint2D.CreateImagePixel(centroidRow, centroidColumn, input.CoordinateContextId));
                tool.SetOutputValue("PointCount", (double)valid.Count);
                tool.SetOutputValue("SkippedCount", (double)skippedCount);
                tool.SetOutputValue("CentroidRow", centroidRow);
                tool.SetOutputValue("CentroidColumn", centroidColumn);
                tool.SetOutputValue("MinRow", minRow);
                tool.SetOutputValue("MinColumn", minColumn);
                tool.SetOutputValue("MaxRow", maxRow);
                tool.SetOutputValue("MaxColumn", maxColumn);
                tool.SetOutputValue("RowSpan", rowSpan);
                tool.SetOutputValue("ColumnSpan", columnSpan);
                tool.SetOutputValue("MaxSpan", maxSpan);
                tool.SetOutputValue("CoordinateContext", input.CoordinateContext + ":" + input.CoordinateContextId);
                tool.SetOutputValue("Parameters", "MinimumCount=" + parameters.PointSetMinimumCount.ToString(CultureInfo.InvariantCulture) + ", AbnormalPolicy=" + parameters.PointSetAbnormalPolicy);
                ReplaceToolOverlayContours(overlays, tool, "XLD", overlayCount, resultCode, maxSpan);
                overlays = null;

                string message = string.Format(CultureInfo.InvariantCulture,
                    "HALCON 点集几何统计：有效 {0} 点，跳过 {1} 点，质心=({2:0.###},{3:0.###})，边界=R[{4:0.###},{5:0.###}] C[{6:0.###},{7:0.###}]，最大跨度={8:0.###}px · {9}",
                    valid.Count, skippedCount, centroidRow, centroidColumn, minRow, maxRow, minColumn, maxColumn, maxSpan, input.CoordinateContextId);
                InspectionRecord record = CreateRecord("PointSetGeometryStatistics", resultCode, maxSpan, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(boundingBox);
                DisposeObject(centroidCross);
                DisposeObject(pointPath);
                DisposeObject(pointCrosses);
                DisposeObject(overlays);
            }
        }
    }
}
