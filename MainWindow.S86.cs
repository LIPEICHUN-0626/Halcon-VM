using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        // Session UI state only: never a tool parameter or algorithm definition.
        private readonly Dictionary<string, bool> toolCatalogCategoryExpansionS86 = new Dictionary<string, bool>(StringComparer.Ordinal);
        private bool toolCatalogCategoryUpdatingS86;

        private bool ForceToolCatalogCategoriesOpenS86
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ToolSearchTextBox?.Text) ||
                    !string.Equals(toolCatalogMode, "All", StringComparison.OrdinalIgnoreCase) ||
                    IsToolRecommendationContextActiveS67 || IsInputSourceContextActiveS68;
            }
        }

        private void ToolCatalogCategory_LoadedS86(object sender, RoutedEventArgs e)
        {
            ApplyToolCatalogCategoryExpansionS86(sender as Expander);
        }

        private void ToolCatalogCategory_ChangedS86(object sender, RoutedEventArgs e)
        {
            if (toolCatalogCategoryUpdatingS86 || ForceToolCatalogCategoriesOpenS86) return;
            Expander expander = sender as Expander;
            var group = expander?.DataContext as System.Windows.Data.CollectionViewGroup;
            if (group != null && ReferenceEquals(e.OriginalSource, expander))
                toolCatalogCategoryExpansionS86[Convert.ToString(group.Name, CultureInfo.InvariantCulture)] = expander.IsExpanded;
        }

        private void ApplyToolCatalogCategoryExpansionS86(Expander expander)
        {
            var group = expander?.DataContext as System.Windows.Data.CollectionViewGroup;
            if (group == null) return;
            string category = Convert.ToString(group.Name, CultureInfo.InvariantCulture);
            bool expanded;
            if (!toolCatalogCategoryExpansionS86.TryGetValue(category, out expanded))
            {
                VmToolCatalogItem first = toolCatalog.OrderBy(item => item.CategoryOrder).FirstOrDefault();
                expanded = first != null && string.Equals(category, first.Category, StringComparison.Ordinal);
            }
            toolCatalogCategoryUpdatingS86 = true;
            try { expander.IsExpanded = ForceToolCatalogCategoriesOpenS86 || expanded; }
            finally { toolCatalogCategoryUpdatingS86 = false; }
        }

        private void RefreshToolCatalogCategoryExpansionS86(DependencyObject parent)
        {
            if (parent == null) return;
            Expander expander = parent as Expander;
            if (expander != null && expander.Name == "ToolCatalogCategoryExpander")
                ApplyToolCatalogCategoryExpansionS86(expander);
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
                RefreshToolCatalogCategoryExpansionS86(VisualTreeHelper.GetChild(parent, index));
        }

        private void InitializeToolboxDirectModesS86()
        {
            toolCatalogMode = "All";
            if (toolCatalogView != null) toolCatalogView.Refresh();
            UpdateToolCatalogModeButtons();
        }

        private static string GetRoiSourcePortNameS86(VmToolInstance tool)
        {
            return tool != null && tool.Kind == VmToolKind.ShapeMatch ? "SearchROI" : "ROI";
        }

        private VmToolInputBindingData GetRoiSourceBindingS86(VmToolInstance tool)
        {
            return tool == null ? null : tool.GetInputBinding(GetRoiSourcePortNameS86(tool));
        }

        private void PromoteRoiSourceModeAfterConnectionS86(VmToolInstance tool, string targetPortName)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind) ||
                !string.Equals(targetPortName, GetRoiSourcePortNameS86(tool), StringComparison.OrdinalIgnoreCase)) return;
            if (tool.Parameters == null) tool.Parameters = new VmToolParameterData();
            string current = VmRoiSourceMode.Normalize(tool.Parameters.RoiSourceMode);
            if (current == VmRoiSourceMode.Local || current == VmRoiSourceMode.Legacy)
                tool.Parameters.RoiSourceMode = VmRoiSourceMode.Inherited;
        }

        private void ResetRoiSourceModeAfterClearS86(VmToolInstance tool, string targetPortName)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind) ||
                !string.Equals(targetPortName, GetRoiSourcePortNameS86(tool), StringComparison.OrdinalIgnoreCase)) return;
            if (tool.Parameters == null) tool.Parameters = new VmToolParameterData();
            tool.Parameters.RoiSourceMode = VmRoiSourceMode.Local;
        }

        private string GetAppliedRoiSourceModeS86(VmToolInstance tool)
        {
            if (tool == null || tool.Parameters == null) return VmRoiSourceMode.Local;
            string mode = VmRoiSourceMode.Normalize(tool.Parameters.RoiSourceMode);
            VmToolInputBindingData binding = GetRoiSourceBindingS86(tool);
            // InputBindings is the authoritative graph. A Region connected by an
            // older recipe, SDK or direct graph edit must never be silently ignored.
            if (mode == VmRoiSourceMode.Local && binding != null) return VmRoiSourceMode.Inherited;
            if (mode != VmRoiSourceMode.Legacy) return mode;
            return binding == null ? VmRoiSourceMode.Local : VmRoiSourceMode.Legacy;
        }

        private void RefreshDockRoiSourceTaskS86(VmToolInstance tool, VmToolParameterData parameters)
        {
            bool supportsRoi = tool != null && ToolMetadata.SupportsRoi(tool.Kind);
            DockRoiTaskDecisionPanelS86.Visibility = supportsRoi ? Visibility.Visible : Visibility.Collapsed;
            DockRoiSelectionTaskUI02.Visibility = supportsRoi ? Visibility.Visible : Visibility.Collapsed;
            DockRoiEmptyHintUI02.Visibility = supportsRoi && dockRoiBindingRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DockRoiLocalDetailsExpander.Visibility = supportsRoi ? Visibility.Visible : Visibility.Collapsed;
            if (!supportsRoi) return;

            string portName = GetRoiSourcePortNameS86(tool);
            VmInputPortEditorRow row = dockInputPortRows.FirstOrDefault(item => string.Equals(item.PortName, portName, StringComparison.OrdinalIgnoreCase));
            List<VmToolValueOption> modes = VmRoiSourceMode.CreateAll().ToList();
            string savedMode = VmRoiSourceMode.Normalize(parameters == null ? null : parameters.RoiSourceMode);
            if (savedMode == VmRoiSourceMode.Local && GetRoiSourceBindingS86(tool) != null)
                savedMode = VmRoiSourceMode.Inherited;
            if (savedMode == VmRoiSourceMode.Legacy)
            {
                modes.Add(new VmToolValueOption { Key = VmRoiSourceMode.Legacy, DisplayText = "旧配方 · 前序与本地交集" });
            }
            DockRoiSourceModeComboBox.ItemsSource = modes;
            DockRoiSourceModeComboBox.SelectedValue = savedMode;
            DockRoiInheritedSourceComboBox.ItemsSource = row == null ? new List<VmInputSourceOption>() : row.SourceOptions;
            DockRoiInheritedSourceComboBox.SelectedValue = row == null ? null : row.SelectedSourceKey;
            UpdateDockRoiSourceTaskStateS86(tool);
        }

        private void UpdateDockRoiSourceTaskStateS86(VmToolInstance tool)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind)) return;
            string mode = VmRoiSourceMode.Normalize(DockRoiSourceModeComboBox.SelectedValue as string);
            bool needsSource = mode == VmRoiSourceMode.Inherited || mode == VmRoiSourceMode.Merge || mode == VmRoiSourceMode.Legacy;
            DockRoiInheritedSourceLabelS86.Visibility = needsSource ? Visibility.Visible : Visibility.Collapsed;
            DockRoiInheritedSourceComboBox.Visibility = needsSource ? Visibility.Visible : Visibility.Collapsed;

            int detections = dockRoiBindingRows.Count(item => item.IsBound && item.Layer != null && item.Layer.IsEnabled && !item.Layer.IsMask);
            int masks = dockRoiBindingRows.Count(item => item.IsBound && item.Layer != null && item.Layer.IsEnabled && item.Layer.IsMask);
            DockRoiRoleSummaryText.Text = "本地：检测 " + detections.ToString(CultureInfo.InvariantCulture) + " · 屏蔽 " + masks.ToString(CultureInfo.InvariantCulture);
            if (tool.Kind == VmToolKind.ImageThreshold)
            {
                int bound = dockRoiBindingRows.Count(item => item.IsBound);
                DockImageThresholdScopeUI02.Text = mode == VmRoiSourceMode.Local
                    ? (bound == 0 ? "范围：输入有效域 · 在 ROI 页选择检测区" : "范围：本地检测区 " + detections + " · 屏蔽 " + masks)
                    : "范围：" + VmRoiSourceMode.GetDisplayText(mode) + " · 输入有效域交集";
            }

            VmInputSourceOption source = DockRoiInheritedSourceComboBox.SelectedItem as VmInputSourceOption;
            string sourceText = source == null || source.IsDefault ? "尚未选择前序 Region" : source.DisplayText;
            if (mode == VmRoiSourceMode.Local)
            {
                DockRoiSourceStatusText.Text = tool.Kind == VmToolKind.ImageThreshold && dockRoiBindingRows.Any(item => item.IsBound) && detections == 0
                    ? "已绑定但没有启用检测区：不会回退全图；请启用检测区或明确解绑。"
                    : detections == 0
                    ? (ToolMetadata.RequiresRoi(tool.Kind) ? "本地模式需要至少一个已启用检测区。" : "本地模式未绑定检测区：该工具按全图运行；屏蔽区不能单独使用。")
                    : "本地检测区先做位置修正，再扣除本地屏蔽区。";
                if (tool.Kind == VmToolKind.ImageThreshold && !dockRoiBindingRows.Any(item => item.IsBound))
                    DockRoiSourceStatusText.Text = "未绑定检测区：使用输入图像已有有效域，不扩大到域外像素。";
            }
            else if (mode == VmRoiSourceMode.Inherited)
            {
                DockRoiSourceStatusText.Text = sourceText + "；继承区域保持上游像素坐标，只扣除本地屏蔽区。";
            }
            else if (mode == VmRoiSourceMode.Merge)
            {
                DockRoiSourceStatusText.Text = sourceText + "；与位置修正后的本地检测区做并集，再扣除本地屏蔽区。";
            }
            else
            {
                DockRoiSourceStatusText.Text = sourceText + "；旧配方保持前序 Region 与本地检测区交集语义，重新选择策略后迁移。";
            }
            DockRoiSourceStatusText.Foreground = needsSource && (source == null || source.IsDefault || !source.IsValid)
                ? Brushes.Firebrick
                : Brushes.SlateGray;
        }

        private void DockRoiSourceModeComboBox_SelectionChangedS86(object sender, SelectionChangedEventArgs e)
        {
            if (dockDraftUpdating || dockEditingTool == null) return;
            UpdateDockRoiSourceTaskStateS86(dockEditingTool);
            UpdateDockRoiSummary();
            DockDraftValue_Changed(sender, e);
        }

        private void DockRoiInheritedSourceComboBox_SelectionChangedS86(object sender, SelectionChangedEventArgs e)
        {
            if (dockDraftUpdating || dockEditingTool == null) return;
            string portName = GetRoiSourcePortNameS86(dockEditingTool);
            VmInputPortEditorRow row = dockInputPortRows.FirstOrDefault(item => string.Equals(item.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (row != null)
            {
                row.RestoreSourceKeyUI06(DockRoiInheritedSourceComboBox.SelectedValue as string);
                RefreshDockInputRowState(row);
                DockInputPortItemsControl.Items.Refresh();
                DockInputSummaryText.Text = BuildDockInputDraftSummary();
            }
            UpdateDockRoiSourceTaskStateS86(dockEditingTool);
            UpdateDockRoiSummary();
            DockDraftValue_Changed(sender, e);
        }

        private string ValidateDockRoiSourceDraftS86(VmToolInstance tool, VmToolParameterData parameters)
        {
            if (tool == null || parameters == null || !ToolMetadata.SupportsRoi(tool.Kind)) return string.Empty;
            string mode = VmRoiSourceMode.Normalize(DockRoiSourceModeComboBox.SelectedValue as string);
            parameters.RoiSourceMode = mode;
            if (tool.Kind == VmToolKind.ImageThreshold)
            {
                VmRoiBindingItem missing = dockRoiBindingRows.FirstOrDefault(item => item.IsBound && (item.Layer == null || !roiLayers.Contains(item.Layer)));
                if (missing != null) return "阈值 ROI 绑定已失效：" + missing.Name + "。请取消该绑定并明确重选，不会回退输入全域。";
                if (mode == VmRoiSourceMode.Local && dockRoiBindingRows.Any(item => item.IsBound) &&
                    !dockRoiBindingRows.Any(item => item.IsBound && item.Layer != null && item.Layer.IsEnabled && !item.Layer.IsMask))
                    return "阈值没有已启用的检测区。请启用检测区，或明确解绑后使用输入有效域；屏蔽区不能单独运行。";
            }
            string portName = GetRoiSourcePortNameS86(tool);
            VmInputPortEditorRow row = dockInputPortRows.FirstOrDefault(item => string.Equals(item.PortName, portName, StringComparison.OrdinalIgnoreCase));
            if (row == null) return "当前工具缺少可用的 ROI 来源端口。";

            if (mode == VmRoiSourceMode.Local)
            {
                row.RestoreSourceKeyUI06(GetDockDefaultInputKey(portName));
                return string.Empty;
            }

            VmInputSourceOption selected = row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
            if (selected == null || selected.IsDefault || !selected.IsValid)
                return "“" + VmRoiSourceMode.GetDisplayText(mode) + "”必须选择一个有效的前序 Region 输出。";
            if (VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi)
                return "逐 ROI 独立执行只适用于本地图层；请改为本地绘制，或切换到合并执行。";

            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selected.SourceToolId, StringComparison.OrdinalIgnoreCase));
            string sourceContext = ResolveRoiImageContextIdS86(source, selected.SourcePortName);
            string targetContext = ResolveTargetImageContextIdS86(tool);
            if (!IsPendingCoordinateContextS102(sourceContext) && !IsPendingCoordinateContextS102(targetContext) && !string.Equals(sourceContext, targetContext, StringComparison.OrdinalIgnoreCase))
            {
                return "ROI 坐标上下文不一致：前序区域=" + sourceContext + "，当前图像=" + targetContext + "。首版只允许同一 ImagePixel 图像链，不执行跨相机或标定转换。";
            }
            return string.Empty;
        }

        private string ResolveTargetImageContextIdS86(VmToolInstance tool)
        {
            if (tool == null) return "Global.Image";
            foreach (VmPortDefinition imagePort in ToolMetadata.GetInstanceInputPorts(tool).Where(item => string.Equals(item.DataType, "Image", StringComparison.OrdinalIgnoreCase)))
            {
                VmToolInputBindingData binding = tool.GetInputBinding(imagePort.PortName);
                if (binding == null) continue;
                VmToolInstance source = GetInputSourceTool(binding);
                if (source == null) continue;
                VmPortDefinition sourcePort = ToolMetadata.GetInstanceOutputPorts(source).FirstOrDefault(item => string.Equals(item.PortName, binding.SourcePortName, StringComparison.OrdinalIgnoreCase));
                if (sourcePort != null && string.Equals(sourcePort.DataType, "Image", StringComparison.OrdinalIgnoreCase))
                    return ResolveOutputCoordinateContextS102(source, sourcePort.PortName);
                return ResolveImagePixelContextIdS66(source);
            }
            return "Global.Image";
        }

        private string ResolveRoiImageContextIdS86(VmToolInstance source, string portName = null)
        {
            return source == null ? "无效来源" : string.IsNullOrWhiteSpace(portName) ? ResolveImagePixelContextIdS66(source) : ResolveOutputCoordinateContextS102(source, portName);
        }

        private HRegion CreateEffectiveRoiRegionS86(VmToolInstance tool, bool required)
        {
            if (tool != null && tool.Kind == VmToolKind.ImageThreshold) ValidateThresholdRoiBindingsUIBatch02(tool);
            string mode = GetAppliedRoiSourceModeS86(tool);
            VmToolInputBindingData binding = GetRoiSourceBindingS86(tool);
            if (mode == VmRoiSourceMode.Local || (binding == null && mode == VmRoiSourceMode.Legacy))
                return CreateBoundRoiRegion(tool, required);
            if (binding == null)
                throw new InvalidOperationException(tool.InstanceName + " 的 ROI 来源策略为“" + VmRoiSourceMode.GetDisplayText(mode) + "”，但尚未选择前序 Region。请在 ROI 任务页选择来源。");

            string error = GetInputBindingConfigurationError(tool, binding);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            VmToolInstance source = GetInputSourceTool(binding);
            if (source == null || !string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException((source == null ? "前序 ROI 来源" : source.InstanceName) + " 尚无本周期 OK Region，请先按流程顺序运行上游工具。");
            string sourceContext = ResolveRoiImageContextIdS86(source, binding.SourcePortName);
            string targetContext = ResolveTargetImageContextIdS86(tool);
            if (IsPendingCoordinateContextS102(sourceContext) || IsPendingCoordinateContextS102(targetContext) || !string.Equals(sourceContext, targetContext, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("ROI 坐标上下文不一致：前序区域=" + sourceContext + "，当前图像=" + targetContext + "。首版不做跨相机、世界坐标或标定转换。");

            object raw;
            VmRegionSnapshot snapshot = source.TryGetOutputValue(binding.SourcePortName, out raw) ? raw as VmRegionSnapshot : null;
            if (snapshot == null || snapshot.IsDisposed)
                throw new InvalidOperationException(source.InstanceName + "." + binding.SourcePortName + " 没有可用的本周期 Region 快照，请重新运行上游工具。");

            using (HRegion inherited = snapshot.CreateRegionCopy())
            {
                if (mode == VmRoiSourceMode.Legacy)
                    return CreateEffectiveRegionFromBase(tool, inherited, GetBoundDetectionRoiLayers(tool), GetBoundMaskRoiLayers(tool), true);
                if (mode == VmRoiSourceMode.Inherited)
                    return CreateEffectiveRegionFromBase(tool, inherited, new List<VmRoiLayer>(), GetBoundMaskRoiLayers(tool), false);

                List<VmRoiLayer> detections = GetBoundDetectionRoiLayers(tool);
                if (detections.Count == 0)
                    return CreateEffectiveRegionFromBase(tool, inherited, new List<VmRoiLayer>(), GetBoundMaskRoiLayers(tool), false);
                using (HRegion local = CreatePositionCorrectedUnionRoiRegion(tool, detections))
                {
                    HObject inheritedUnion = null;
                    HObject merged = null;
                    try
                    {
                        // Union2 is tuple-wise for a multi-object Region input.  A
                        // threshold snapshot commonly contains hundreds of objects;
                        // collapse it first so the local area is not duplicated once
                        // per connected component.
                        HOperatorSet.Union1(inherited, out inheritedUnion);
                        HOperatorSet.Union2(inheritedUnion, local, out merged);
                        using (HRegion mergedRegion = new HRegion(merged))
                        {
                            merged = null;
                            return CreateEffectiveRegionFromBase(tool, mergedRegion, new List<VmRoiLayer>(), GetBoundMaskRoiLayers(tool), false);
                        }
                    }
                    finally
                    {
                        DisposeObject(merged);
                        DisposeObject(inheritedUnion);
                    }
                }
            }
        }

        private void ValidateThresholdRoiBindingsUIBatch02(VmToolInstance tool)
        {
            if (VmRoiExecutionMode.Normalize(tool.Parameters == null ? null : tool.Parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi)
                throw new InvalidOperationException("普通灰度阈值使用合并检测域，不支持逐 ROI 独立执行。请使用合并执行配置。");
            string mode = GetAppliedRoiSourceModeS86(tool);
            string targetContext = ResolveTargetImageContextIdS86(tool);
            bool inheritedOnly = mode == VmRoiSourceMode.Inherited;
            int activeLocal = 0;
            foreach (string id in tool.BoundRoiIds)
            {
                var matches = roiLayers.Where(candidate => string.Equals(candidate.RoiId, id, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count != 1)
                    throw new InvalidOperationException("阈值检测区绑定已失效或身份重复：" + id + "。请在 ROI 页明确重选，不会回退全图。");
                VmRoiLayer layer = matches[0];
                if (inheritedOnly && !layer.IsMask) continue; // Original inherited mode ignores local detections.
                if (!layer.IsEnabled) continue;
                if (layer.Geometry == null)
                    throw new InvalidOperationException("阈值 ROI“" + layer.Name + "”没有可用几何，请重新绘制或解绑。");
                if (!string.IsNullOrWhiteSpace(layer.ImageContextId) &&
                    (IsPendingCoordinateContextS102(targetContext) || !string.Equals(layer.ImageContextId, targetContext, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("阈值 ROI“" + layer.Name + "”属于另一图像来源或旧坐标版本，请重选对应图像/ROI，不会回退全图。");
                activeLocal++;
            }
            if (!inheritedOnly && GetRoiSourceBindingS86(tool) == null && tool.BoundRoiIds.Count > 0 && activeLocal == 0)
                throw new InvalidOperationException("阈值已绑定 ROI，但全部停用；请启用检测区或明确解绑后使用输入有效域。");
        }

        private string GetRoiSourceSummaryS86(VmToolInstance tool)
        {
            string mode = GetAppliedRoiSourceModeS86(tool);
            VmToolInputBindingData binding = GetRoiSourceBindingS86(tool);
            if (mode == VmRoiSourceMode.Local) return "本地绘制";
            VmToolInstance source = GetInputSourceTool(binding);
            string sourceText = source == null || binding == null ? "未选择" : source.InstanceName + "." + binding.SourcePortName;
            return VmRoiSourceMode.GetDisplayText(mode) + " ← " + sourceText;
        }

        private void AdjustRoiConfigurationStatusS86(VmToolInstance tool)
        {
            if (tool == null || !ToolMetadata.SupportsRoi(tool.Kind)) return;
            string mode = GetAppliedRoiSourceModeS86(tool);
            if (mode == VmRoiSourceMode.Local) return;
            VmToolInputBindingData binding = GetRoiSourceBindingS86(tool);
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            VmRegionSnapshot snapshot = source != null && binding != null && source.TryGetOutputValue(binding.SourcePortName, out raw) ? raw as VmRegionSnapshot : null;
            bool ready = source != null && string.Equals(source.ResultCode, "OK", StringComparison.OrdinalIgnoreCase) && snapshot != null && !snapshot.IsDisposed;
            if (!ready && tool.ConfigurationStatus == "就绪") tool.ConfigurationStatus = binding == null ? "待选择 ROI 来源" : "等待上游 Region";
            tool.ConnectionStatus = ready ? "ROI 继承已连接 · 有值" : (binding == null ? "ROI 来源未选择" : "ROI 继承已连接 · 等待上游");
            tool.ConnectionSummary = GetImageInputSummary(tool) + " + " + GetRoiSourceSummaryS86(tool);
        }

        private void DrawInheritedRoiPreviewS86(HWindow window, bool allowInputCoordinateRoi, VmToolInstance tool)
        {
            if (!allowInputCoordinateRoi || window == null || tool == null || !ToolMetadata.SupportsRoi(tool.Kind)) return;
            string mode = GetAppliedRoiSourceModeS86(tool);
            if (mode == VmRoiSourceMode.Local) return;
            VmToolInputBindingData binding = GetRoiSourceBindingS86(tool);
            VmToolInstance source = GetInputSourceTool(binding);
            object raw;
            VmRegionSnapshot snapshot = source != null && binding != null && source.TryGetOutputValue(binding.SourcePortName, out raw) ? raw as VmRegionSnapshot : null;
            if (snapshot == null || snapshot.IsDisposed) return;
            try
            {
                using (HRegion region = snapshot.CreateRegionCopy())
                {
                    window.SetDraw("margin");
                    window.SetLineWidth(2);
                    window.SetColor("cyan");
                    window.DispObj(region);
                }
            }
            catch (HalconException) { }
            catch (ObjectDisposedException) { }
        }
    }
}
