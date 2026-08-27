using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<VmPortMonitorRow> portDataMonitorRows = new ObservableCollection<VmPortMonitorRow>();
        private readonly HashSet<string> pinnedPortMonitorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool portMonitorUpdating;

        private static string BuildPortMonitorKey(string toolId, string direction, string portName)
        {
            return (toolId ?? string.Empty) + "|" + (direction ?? string.Empty).ToUpperInvariant() + "|" + (portName ?? string.Empty);
        }

        private void RefreshPortDataMonitorS64(VmToolInstance selectedTool)
        {
            if (PortDataMonitorGrid == null || portMonitorUpdating) return;
            portMonitorUpdating = true;
            try
            {
                VmPortMonitorRow previous = PortDataMonitorGrid.SelectedItem as VmPortMonitorRow;
                string previousKey = previous == null ? null : previous.MonitorKey;
                Dictionary<string, VmPortMonitorRow> rows = new Dictionary<string, VmPortMonitorRow>(StringComparer.OrdinalIgnoreCase);

                VmFlowConnectionVisual connection = GetSelectedFlowConnection();
                if (connection != null)
                {
                    AddPortMonitorOutputRow(rows, flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, connection.SourceToolId, StringComparison.OrdinalIgnoreCase)), connection.SourcePortName);
                    AddPortMonitorInputRow(rows, flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, connection.TargetToolId, StringComparison.OrdinalIgnoreCase)), connection.TargetPortName);
                }

                if (selectedTool != null)
                {
                    foreach (VmPortDefinition port in ToolMetadata.GetInstanceInputPorts(selectedTool)) AddPortMonitorInputRow(rows, selectedTool, port.PortName);
                    foreach (VmPortDefinition port in ToolMetadata.GetInstanceOutputPorts(selectedTool)) AddPortMonitorOutputRow(rows, selectedTool, port.PortName);
                }

                foreach (string key in pinnedPortMonitorKeys.ToList())
                {
                    string[] parts = key.Split('|');
                    VmToolInstance tool = parts.Length == 3 ? flowTools.FirstOrDefault(item => string.Equals(item.ToolId, parts[0], StringComparison.OrdinalIgnoreCase)) : null;
                    if (tool == null)
                    {
                        pinnedPortMonitorKeys.Remove(key);
                        continue;
                    }
                    if (string.Equals(parts[1], "IN", StringComparison.OrdinalIgnoreCase)) AddPortMonitorInputRow(rows, tool, parts[2]);
                    else AddPortMonitorOutputRow(rows, tool, parts[2]);
                    if (!rows.ContainsKey(key)) pinnedPortMonitorKeys.Remove(key);
                }

                portDataMonitorRows.Clear();
                foreach (VmPortMonitorRow row in rows.Values
                    .OrderByDescending(item => connection != null && (string.Equals(item.ConnectionKey, connection.ConnectionKey, StringComparison.OrdinalIgnoreCase)))
                    .ThenByDescending(item => item.IsPinned)
                    .ThenBy(item => flowTools.FindIndex(tool => string.Equals(tool.ToolId, item.OwnerToolId, StringComparison.OrdinalIgnoreCase)))
                    .ThenBy(item => item.Direction == "IN" ? 0 : 1)
                    .ThenBy(item => item.PortName, StringComparer.OrdinalIgnoreCase))
                {
                    row.IsPinned = pinnedPortMonitorKeys.Contains(row.MonitorKey);
                    portDataMonitorRows.Add(row);
                }

                PortDataMonitorCountText.Text = portDataMonitorRows.Count.ToString(CultureInfo.InvariantCulture) + " 项";
                PortDataMonitorScopeText.Text = connection != null
                    ? "优先显示所选连接 " + connection.SourceEndpointText + " → " + connection.TargetEndpointText + "；并保留当前模块和固定端口。"
                    : selectedTool == null
                        ? (pinnedPortMonitorKeys.Count == 0 ? "选择模块或连接后显示本周期数据。" : "当前只显示已固定端口。")
                        : "当前模块：#" + selectedTool.SequenceText + " " + selectedTool.InstanceName + "；点击行可定位节点、连接和 I/O。";
                if (!string.IsNullOrWhiteSpace(previousKey)) PortDataMonitorGrid.SelectedItem = portDataMonitorRows.FirstOrDefault(item => string.Equals(item.MonitorKey, previousKey, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                portMonitorUpdating = false;
            }
        }

        private void AddPortMonitorInputRow(IDictionary<string, VmPortMonitorRow> rows, VmToolInstance owner, string portName)
        {
            if (owner == null) return;
            VmPortDefinition port = ToolMetadata.GetInstanceInputPorts(owner).FirstOrDefault(item => string.Equals(item.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (port == null) return;
            string key = BuildPortMonitorKey(owner.ToolId, "IN", port.PortName);
            VmToolInputBindingData binding = owner.GetInputBinding(port.PortName);
            if (binding == null && owner.Kind == VmToolKind.NumericJudge && string.Equals(port.PortName, "Value", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(owner.InputToolId))
            {
                binding = new VmToolInputBindingData { TargetPortName = port.PortName, SourceToolId = owner.InputToolId, SourcePortName = owner.InputPortName };
            }
            VmToolInstance source = GetInputSourceTool(binding);
            object raw = null;
            bool hasValue = source != null && binding != null && source.TryGetOutputValue(binding.SourcePortName, out raw) && raw != null;
            string error = binding == null ? string.Empty : GetInputBindingConfigurationError(owner, binding);
            string stateKey;
            string stateText;
            if (binding == null) { stateKey = "Waiting"; stateText = port.IsOptional ? "可选 · 未连接" : "等待连接"; }
            else if (!string.IsNullOrWhiteSpace(error)) { stateKey = "Error"; stateText = "错误 · " + error; }
            else if (source != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && hasValue) { stateKey = "Valid"; stateText = "有效"; }
            else if (hasValue) { stateKey = "Stale"; stateText = "旧值 · 等待本周期 OK"; }
            else if (source != null && string.Equals(source.RunStatus, "异常", StringComparison.OrdinalIgnoreCase)) { stateKey = "Error"; stateText = "上游异常"; }
            else { stateKey = "Waiting"; stateText = "等待本周期值"; }
            string connectionKey = binding == null ? null : BuildFlowConnectionKey(binding.SourceToolId, binding.SourcePortName, owner.ToolId, port.PortName);
            rows[key] = new VmPortMonitorRow
            {
                MonitorKey = key, OwnerToolId = owner.ToolId, OwnerToolName = owner.InstanceName, Direction = "IN", PortName = port.PortName, DisplayName = port.DisplayName,
                SourceToolId = source == null ? null : source.ToolId, SourceToolName = source == null ? null : source.InstanceName, SourcePortName = binding == null ? null : binding.SourcePortName,
                DataType = port.DataType, ValueText = hasValue ? source.GetFormattedOutput(binding.SourcePortName) : "--", ContextText = GetPortMonitorContext(port, raw),
                CycleText = source == null || source.RuntimeOutputVersion <= 0 ? "--" : "#" + source.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture),
                StateKey = stateKey, StateText = stateText, ConnectionKey = connectionKey, IsPinned = pinnedPortMonitorKeys.Contains(key)
            };
        }

        private void AddPortMonitorOutputRow(IDictionary<string, VmPortMonitorRow> rows, VmToolInstance owner, string portName)
        {
            if (owner == null) return;
            VmPortDefinition port = ToolMetadata.GetInstanceOutputPorts(owner).FirstOrDefault(item => string.Equals(item.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (port == null) return;
            string key = BuildPortMonitorKey(owner.ToolId, "OUT", port.PortName);
            object raw;
            bool hasValue = owner.TryGetOutputValue(port.PortName, out raw) && raw != null;
            string stateKey;
            string stateText;
            if (string.Equals(owner.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && hasValue) { stateKey = "Valid"; stateText = "有效"; }
            else if (hasValue) { stateKey = "Stale"; stateText = "旧值 · 已失效"; }
            else if (string.Equals(owner.RunStatus, "异常", StringComparison.OrdinalIgnoreCase) || string.Equals(owner.ResultCode, "NG", StringComparison.OrdinalIgnoreCase)) { stateKey = "Error"; stateText = "无有效输出"; }
            else { stateKey = "Waiting"; stateText = "等待运行"; }
            rows[key] = new VmPortMonitorRow
            {
                MonitorKey = key, OwnerToolId = owner.ToolId, OwnerToolName = owner.InstanceName, Direction = "OUT", PortName = port.PortName, DisplayName = port.DisplayName,
                SourceToolId = owner.ToolId, SourceToolName = owner.InstanceName, SourcePortName = port.PortName, DataType = port.DataType,
                ValueText = hasValue ? owner.GetFormattedOutput(port.PortName) : "--", ContextText = GetPortMonitorContext(port, raw),
                CycleText = owner.RuntimeOutputVersion <= 0 ? "--" : "#" + owner.RuntimeOutputVersion.ToString(CultureInfo.InvariantCulture), StateKey = stateKey, StateText = stateText,
                IsPinned = pinnedPortMonitorKeys.Contains(key)
            };
        }

        private static string GetPortMonitorContext(VmPortDefinition port, object raw)
        {
            VmPose2D pose = raw as VmPose2D;
            if (pose != null) return pose.CoordinateContext + " · " + pose.CoordinateContextId;
            VmPoint2D point = raw as VmPoint2D;
            if (point != null) return point.CoordinateContext + " · " + point.CoordinateContextId;
            VmPoint2DArray points = raw as VmPoint2DArray;
            if (points != null) return points.CoordinateContext + " · " + points.CoordinateContextId + " · " + points.Count.ToString(CultureInfo.InvariantCulture) + " 点";
            VmLine2D line = raw as VmLine2D;
            if (line != null) return line.CoordinateContext + " · " + line.CoordinateContextId + " · L=" + line.Length.ToString("0.###", CultureInfo.InvariantCulture) + "px";
            VmCircle2D circle = raw as VmCircle2D;
            if (circle != null) return circle.CoordinateContext + " · " + circle.CoordinateContextId + " · R=" + circle.Radius.ToString("0.###", CultureInfo.InvariantCulture) + "px";
            VmEllipse2D ellipse = raw as VmEllipse2D;
            if (ellipse != null) return ellipse.CoordinateContext + " · " + ellipse.CoordinateContextId + " · R1/R2=" + ellipse.Radius1.ToString("0.###", CultureInfo.InvariantCulture) + "/" + ellipse.Radius2.ToString("0.###", CultureInfo.InvariantCulture) + "px";
            string name = port == null ? string.Empty : port.PortName ?? string.Empty;
            string type = port == null ? string.Empty : port.DataType ?? string.Empty;
            if (name.IndexOf("Angle", StringComparison.OrdinalIgnoreCase) >= 0) return "° · 图像坐标";
            if (name.IndexOf("Row", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Width", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Height", StringComparison.OrdinalIgnoreCase) >= 0) return "px · 图像坐标";
            if (string.Equals(type, "Image", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Region", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "XLD", StringComparison.OrdinalIgnoreCase)) return "图像像素坐标";
            if (string.Equals(type, "Pose2D", StringComparison.OrdinalIgnoreCase)) return "ImagePixel · 等待值";
            if (string.Equals(type, "Point2D", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Point2D[]", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Line2D", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Circle2D", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Ellipse2D", StringComparison.OrdinalIgnoreCase)) return "ImagePixel · 等待值";
            return "流程标量";
        }

        private void PortDataMonitorExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (FlowDebugMonitorColumn != null && FlowDebugMonitorBorder.Visibility == Visibility.Visible) FlowDebugMonitorColumn.Width = new GridLength(460);
            RefreshPortDataMonitorS64(GetDebugMonitorTool());
        }

        private void PortDataMonitorExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (FlowDebugMonitorColumn != null && FlowDebugMonitorBorder.Visibility == Visibility.Visible) FlowDebugMonitorColumn.Width = new GridLength(304);
        }

        private void PortDataMonitorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (portMonitorUpdating) return;
            LocatePortMonitorRowS64(PortDataMonitorGrid.SelectedItem as VmPortMonitorRow);
        }

        private void TogglePortMonitorPinButton_Click(object sender, RoutedEventArgs e)
        {
            VmPortMonitorRow row = (sender as FrameworkElement) == null ? null : (sender as FrameworkElement).Tag as VmPortMonitorRow;
            if (row == null) return;
            if (!pinnedPortMonitorKeys.Add(row.MonitorKey)) pinnedPortMonitorKeys.Remove(row.MonitorKey);
            RefreshPortDataMonitorS64(GetDebugMonitorTool());
            HeaderStatusText.Text = pinnedPortMonitorKeys.Contains(row.MonitorKey) ? "已固定端口：" + row.EndpointText : "已取消固定端口：" + row.EndpointText;
            e.Handled = true;
        }

        private void OpenPortMonitorIoButton_Click(object sender, RoutedEventArgs e)
        {
            LocatePortMonitorRowS64(PortDataMonitorGrid.SelectedItem as VmPortMonitorRow);
        }

        private void RefreshPortMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshPortDataMonitorS64(GetDebugMonitorTool());
            HeaderStatusText.Text = "端口数据已从真实本周期运行快照刷新。";
        }

        private void ClearPortMonitorPinsButton_Click(object sender, RoutedEventArgs e)
        {
            pinnedPortMonitorKeys.Clear();
            RefreshPortDataMonitorS64(GetDebugMonitorTool());
            HeaderStatusText.Text = "已清除全部固定端口；运行数据和连接未改变。";
        }

        private void LocatePortMonitorRowS64(VmPortMonitorRow row)
        {
            if (row == null || portMonitorUpdating) return;
            VmToolInstance owner = flowTools.FirstOrDefault(tool => string.Equals(tool.ToolId, row.OwnerToolId, StringComparison.OrdinalIgnoreCase));
            if (owner == null) return;
            FlowToolList.SelectedItems.Clear();
            FlowToolList.SelectedItem = owner;
            FlowToolList.ScrollIntoView(owner);
            DebugMonitorToolComboBox.SelectedItem = owner;
            selectedFlowPortToolId = owner.ToolId;
            selectedFlowPortName = row.PortName;
            selectedFlowPortDirection = row.Direction;
            selectedFlowConnectionKey = string.IsNullOrWhiteSpace(row.ConnectionKey) ? null : row.ConnectionKey;
            RefreshFlowPortVisualization();
            RightTabs.SelectedItem = IoTab;
            HeaderStatusText.Text = "已定位 " + row.EndpointText + (string.IsNullOrWhiteSpace(row.ConnectionKey) ? " 的真实 I/O。" : " 及其真实连接，并打开 I/O。 ");
        }

        private void ClearPortMonitorPinsS64()
        {
            pinnedPortMonitorKeys.Clear();
            portDataMonitorRows.Clear();
        }
    }

    internal static class VmToolCollectionExtensionsS64
    {
        public static int FindIndex(this IEnumerable<VmToolInstance> tools, Func<VmToolInstance, bool> predicate)
        {
            int index = 0;
            foreach (VmToolInstance tool in tools)
            {
                if (predicate(tool)) return index;
                index++;
            }
            return int.MaxValue;
        }
    }
}
