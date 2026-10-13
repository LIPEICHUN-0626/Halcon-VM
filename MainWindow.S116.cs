using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class DeviceResourceRowS116
        {
            public string FlowId { get; set; }
            public string ToolId { get; set; }
            public List<string> ParentCalls { get; set; }
            public string ResourceKey { get; set; }
            public string Title { get; set; }
            public string Device { get; set; }
            public string State { get; set; }
            public string Error { get; set; }
        }

        private static string CameraResourceKeyS116(VmToolParameterData parameters)
        {
            string interfaceName = (parameters.CameraInterfaceS114 ?? string.Empty).Trim().ToUpperInvariant();
            string deviceName = string.IsNullOrWhiteSpace(parameters.CameraDeviceS114)
                ? "DEFAULT" : parameters.CameraDeviceS114.Trim().ToUpperInvariant();
            return interfaceName + "::" + deviceName;
        }

        private void DeviceResourcesExpanderS116_Expanded(object sender, RoutedEventArgs e)
        {
            RefreshDeviceResourcesS116();
        }

        private void RefreshDeviceResourcesS116()
        {
            if (DeviceResourcesExpanderS116 == null || DeviceResourcesExpanderS116.IsExpanded != true ||
                DeviceResourceRowsS116 == null || !uiReady) return;
            var rows = new List<DeviceResourceRowS116>();
            foreach (VmProjectFlowData flow in CaptureProjectFlowsS104())
                CollectCameraResourcesS116(flow.FlowId, flow.Name, flow.Tools, new List<string>(), rows);
            var duplicates = rows.GroupBy(item => item.ResourceKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            foreach (DeviceResourceRowS116 row in rows)
            {
                var owner = cameraSessionsS114.FirstOrDefault(item =>
                    string.Equals(item.Value.ResourceKey, row.ResourceKey, StringComparison.OrdinalIgnoreCase));
                row.State = owner.Value == null ? "未连接" : owner.Key == row.ToolId
                    ? "已连接 · " + (owner.Value.TriggerMode == VmCameraTriggerModeS115.Software ? "软件触发" : "自由采集") +
                      " · " + owner.Value.Frames.ToString(CultureInfo.InvariantCulture) + " 帧"
                    : "当前由另一模块占用：" + owner.Key;
                if (duplicates[row.ResourceKey] > 1)
                    row.State += " · 此设备标识在方案内配置 " + duplicates[row.ResourceKey].ToString(CultureInfo.InvariantCulture) + " 次";
                AcquisitionDiagnosticS115 diagnostic;
                if (acquisitionDiagnosticsS115.TryGetValue(row.ToolId, out diagnostic) && !string.IsNullOrWhiteSpace(diagnostic.Error))
                    row.Error = diagnostic.Error;
            }
            DeviceResourceRowsS116.ItemsSource = rows;
            DeviceResourceSummaryS116.Text = rows.Count == 0
                ? "当前方案没有 HALCON 相机源；本地图片与离线回放不占设备。"
                : rows.Count.ToString(CultureInfo.InvariantCulture) + " 个相机源 · " +
                  cameraSessionsS114.Count.ToString(CultureInfo.InvariantCulture) + " 个运行态连接。default 仅表示接口默认设备，不是硬件唯一序列号。";
        }

        private static void CollectCameraResourcesS116(string flowId, string flowName,
            IEnumerable<ToolFlowRecipeItem> tools, List<string> parents, List<DeviceResourceRowS116> rows)
        {
            foreach (ToolFlowRecipeItem item in tools ?? Enumerable.Empty<ToolFlowRecipeItem>())
            {
                if (item == null) continue;
                VmToolParameterData parameters = item.Parameters ?? new VmToolParameterData();
                if (item.ToolType == VmToolKind.ImageSource.ToString() &&
                    VmImageSourceModeS114.Normalize(parameters.ImageSourceModeS114) == VmImageSourceModeS114.Camera)
                {
                    rows.Add(new DeviceResourceRowS116
                    {
                        FlowId = flowId,
                        ToolId = item.ToolId,
                        ParentCalls = parents.ToList(),
                        ResourceKey = CameraResourceKeyS116(parameters),
                        Title = flowName + " / " + item.InstanceName + (parents.Count > 0 ? " · 子流程" : string.Empty),
                        Device = (parameters.CameraInterfaceS114 ?? "待配置") + " / " +
                            (string.IsNullOrWhiteSpace(parameters.CameraDeviceS114) ? "default" : parameters.CameraDeviceS114),
                        Error = string.Empty
                    });
                }
                if (item.ToolType == VmToolKind.SubflowCall.ToString() && parameters.Subflow != null)
                {
                    var next = parents.ToList();
                    next.Add(item.ToolId);
                    if (next.Count <= 8)
                        CollectCameraResourcesS116(flowId, flowName, parameters.Subflow.Tools, next, rows);
                }
            }
        }

        private void OpenDeviceResourceS116_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DeviceResourceRowS116 row = button == null ? null : button.Tag as DeviceResourceRowS116;
            if (row == null) return;
            RunUiAction("定位相机资源", delegate
            {
                if (!CanNavigateInputRepairS96()) return;
                while (subflowEditorsS102.Count > 0)
                {
                    int before = subflowEditorsS102.Count;
                    ReturnSubflowEditorS102_Click(null, null);
                    if (subflowEditorsS102.Count >= before) return;
                }
                if (row.FlowId != activeProjectFlowIdS104)
                {
                    SwitchProjectFlowS104(row.FlowId);
                    if (row.FlowId != activeProjectFlowIdS104) return;
                }
                foreach (string callId in row.ParentCalls)
                {
                    VmToolInstance call = flowTools.FirstOrDefault(item => item.ToolId == callId && item.Kind == VmToolKind.SubflowCall);
                    if (call == null) throw new InvalidOperationException("子流程调用已修改，请刷新设备资源列表。");
                    int depth = subflowEditorsS102.Count;
                    EnterSubflowEditorS102(call);
                    if (subflowEditorsS102.Count <= depth) return;
                }
                VmToolInstance source = flowTools.FirstOrDefault(item => item.ToolId == row.ToolId && item.Kind == VmToolKind.ImageSource);
                if (source == null) throw new InvalidOperationException("相机源已修改，请刷新设备资源列表。");
                OpenCentralModuleConfigurationS77(source, true);
                if (ReferenceEquals(dockEditingTool, source))
                    HeaderStatusText.Text = "已定位“" + source.InstanceName + "”；连接/断开只在此模块执行。";
            });
        }
    }
}
