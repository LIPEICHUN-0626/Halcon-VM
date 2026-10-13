using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly List<VmProductionImagePolicyS142> productionImageDraftPoliciesS142 = new List<VmProductionImagePolicyS142>();
        private List<VmProjectFlowData> productionImageSourceFlowsS142 = new List<VmProjectFlowData>();
        private bool productionImageDraftUpdatingS142;
        private bool productionImageEditorEnabledS142 = true;
        private string productionImageSourceReadErrorS142;

        private sealed class ProductionImageFlowOptionS142
        {
            public string Id { get; set; }
            public string Label { get; set; }
            public string TechnicalDetail { get; set; }
        }
        private sealed class ProductionImageSourceRowS142 : INotifyPropertyChanged
        {
            public string ToolId { get; set; }
            public string Label { get; set; }
            public string TechnicalDetail { get; set; }
            public bool IsValid { get; set; }
            public bool IsEnabled { get; set; }
            public bool IsSelected { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
            public void Refresh() { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected")); }
        }

        private void LoadProductionImageDraftS142(VmProductionRunLayoutData layout)
        {
            if (ProductionImageFlowS142 == null) return;
            productionImageDraftUpdatingS142 = true;
            try
            {
                productionImageDraftPoliciesS142.Clear();
                productionImageDraftPoliciesS142.AddRange((layout?.ImagePoliciesS142 ?? new List<VmProductionImagePolicyS142>()).Where(value => value != null).Select(value => value.Clone()));
                productionImageSourceReadErrorS142 = null;
                try { productionImageSourceFlowsS142 = (CaptureRecipe().Flows ?? new List<VmProjectFlowData>()).Where(value => value != null).Select(value => value.Clone()).ToList(); }
                catch (Exception error) { productionImageSourceFlowsS142 = new List<VmProjectFlowData>(); productionImageSourceReadErrorS142 = "来源目录读取失败：" + error.Message; }
                var options = productionImageSourceFlowsS142.Where(value => !string.IsNullOrWhiteSpace(value.FlowId)).GroupBy(value => value.FlowId, StringComparer.Ordinal)
                    .Select(group => new ProductionImageFlowOptionS142 { Id = group.Key, Label = group.Count() == 1 ? group.First().Name : "流程标识重复 · 不可用", TechnicalDetail = "FlowId：" + group.Key }).ToList();
                foreach (var id in productionImageDraftPoliciesS142.Where(value => !string.IsNullOrWhiteSpace(value.FlowId)).Select(value => value.FlowId).Distinct(StringComparer.Ordinal))
                    if (!options.Any(value => string.Equals(value.Id, id, StringComparison.Ordinal)))
                        options.Add(new ProductionImageFlowOptionS142 { Id = id, Label = "原流程已失效（不按名称重绑）", TechnicalDetail = "原 FlowId：" + id });
                ProductionImageFlowS142.ItemsSource = options;
                ProductionImageFlowS142.SelectedItem = options.FirstOrDefault(value => string.Equals(value.Id, activeProjectFlowIdS104, StringComparison.Ordinal)) ?? options.FirstOrDefault();
            }
            finally { productionImageDraftUpdatingS142 = false; }
            RefreshProductionImageEditorS142();
        }

        private List<VmProductionImagePolicyS142> CaptureProductionImageDraftS142()
        { return productionImageDraftPoliciesS142.Select(value => value.Clone()).ToList(); }

        private void SetProductionImageEditorEnabledS142(bool enabled)
        {
            productionImageEditorEnabledS142 = enabled;
            if (ProductionImageEditorS142 != null) ProductionImageEditorS142.IsEnabled = enabled;
        }

        private string SelectedProductionImageFlowS142()
        { return (ProductionImageFlowS142?.SelectedItem as ProductionImageFlowOptionS142)?.Id; }

        private VmProjectFlowData SelectedProductionImageDefinitionS142()
        {
            string id = SelectedProductionImageFlowS142();
            var matches = productionImageSourceFlowsS142.Where(value => string.Equals(value.FlowId, id, StringComparison.Ordinal)).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private VmProductionImagePolicyS142 SelectedProductionImagePolicyS142(bool create)
        {
            string id = SelectedProductionImageFlowS142();
            if (string.IsNullOrWhiteSpace(id)) return null;
            var matches = productionImageDraftPoliciesS142.Where(value => string.Equals(value.FlowId, id, StringComparison.Ordinal)).Take(2).ToList();
            if (matches.Count == 1) return matches[0];
            if (matches.Count != 0 || !create || SelectedProductionImageDefinitionS142() == null) return null;
            if (productionImageDraftPoliciesS142.Count >= 32)
            { ProductionImageSourceStatusS142.Text = "最多保存 32 个流程策略；本次没有新增策略。"; return null; }
            var policy = new VmProductionImagePolicyS142 { FlowId = id, Mode = "Off", IncludeStopped = false, SourceToolIds = new List<string>() };
            productionImageDraftPoliciesS142.Add(policy); return policy;
        }

        private bool CanEditProductionImageDraftS142()
        {
            if (productionImageDraftUpdatingS142 || productionLayoutDraftUpdating || !productionLayoutDesignMode) return false;
            if (!productionImageEditorEnabledS142 || !RequireEngineeringEditS137("编辑周期留图草稿") || !CanChangeRecipeS134() || recipeChangeoverFaultedS134)
            { RefreshProductionImageEditorS142(); return false; }
            return true;
        }

        private void NotifyProductionImageDraftS142(object sender, RoutedEventArgs e)
        {
            RefreshProductionImageEditorS142();
            ProductionLayoutDraftValue_Changed(sender, e);
        }

        private void ProductionImageFlowS142_Changed(object sender, SelectionChangedEventArgs e)
        { if (!productionImageDraftUpdatingS142) RefreshProductionImageEditorS142(); }

        private void ProductionImageModeS142_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!CanEditProductionImageDraftS142()) return;
            string mode = (ProductionImageModeS142.SelectedItem as ComboBoxItem)?.Tag as string;
            if (mode != "Off" && mode != "Failures" && mode != "All") return;
            var policy = SelectedProductionImagePolicyS142(true);
            if (policy == null) { RefreshProductionImageEditorS142(); return; }
            policy.Mode = mode;
            NotifyProductionImageDraftS142(sender, e);
        }

        private void ProductionImageIncludeStoppedS142_Changed(object sender, RoutedEventArgs e)
        {
            if (!CanEditProductionImageDraftS142()) return;
            var policy = SelectedProductionImagePolicyS142(true);
            if (policy == null) { RefreshProductionImageEditorS142(); return; }
            policy.IncludeStopped = ProductionImageIncludeStoppedS142.IsChecked == true;
            NotifyProductionImageDraftS142(sender, e);
        }

        private void ProductionImageSourceS142_Click(object sender, RoutedEventArgs e)
        {
            var control = sender as CheckBox; var row = control?.Tag as ProductionImageSourceRowS142;
            if (row == null) return;
            if (!CanEditProductionImageDraftS142()) { row.Refresh(); return; }
            bool requested = control.IsChecked == true;
            if (requested && (!row.IsValid || !row.IsEnabled))
            { control.IsChecked = false; ProductionImageSourceStatusS142.Text = "此根图像源不存在、标识重复、端口失效或已停用；不能新增绑定。"; return; }
            var policy = SelectedProductionImagePolicyS142(requested);
            if (policy == null) { RefreshProductionImageEditorS142(); return; }
            policy.SourceToolIds = policy.SourceToolIds ?? new List<string>();
            if (!requested) policy.SourceToolIds.RemoveAll(value => string.Equals(value, row.ToolId, StringComparison.Ordinal));
            else
            {
                if (!policy.SourceToolIds.Contains(row.ToolId, StringComparer.Ordinal))
                {
                    if (policy.SourceToolIds.Count >= 4) { control.IsChecked = false; ProductionImageSourceStatusS142.Text = "每个流程最多四个明确图像源，请先取消不需要的来源。"; return; }
                    policy.SourceToolIds.Add(row.ToolId);
                }
            }
            NotifyProductionImageDraftS142(sender, e);
        }

        private void RefreshProductionImageEditorS142()
        {
            if (ProductionImageSourcesS142 == null) return;
            productionImageDraftUpdatingS142 = true;
            try
            {
                var flow = SelectedProductionImageDefinitionS142();
                var policy = SelectedProductionImagePolicyS142(false);
                var selectedIds = policy?.SourceToolIds ?? new List<string>();
                string mode = policy?.Mode ?? "Off";
                ProductionImageModeS142.SelectedValue = mode;
                ProductionImageIncludeStoppedS142.IsChecked = policy != null && policy.IncludeStopped;
                ProductionImageIncludeStoppedS142.IsEnabled = flow != null && mode != "Off";
                var rows = new List<ProductionImageSourceRowS142>();
                foreach (var group in (flow?.Tools ?? new List<ToolFlowRecipeItem>()).Where(value => value != null && !string.IsNullOrWhiteSpace(value.ToolId)).GroupBy(value => value.ToolId, StringComparer.Ordinal))
                {
                    var tool = group.First(); VmToolKind kind;
                    bool selected = selectedIds.Contains(group.Key, StringComparer.Ordinal);
                    bool source = group.Count() == 1 && Enum.TryParse(tool.ToolType, out kind) && kind == VmToolKind.ImageSource;
                    bool imagePort = source && ToolMetadata.GetOutputPorts(VmToolKind.ImageSource).Any(value => value.PortName == "Image" && value.DataType == "Image");
                    if (!source && !selected) continue;
                    rows.Add(new ProductionImageSourceRowS142 { ToolId = group.Key, Label = (tool.InstanceName ?? "图像源") + (!imagePort ? "（来源失效）" : tool.IsEnabled ? string.Empty : "（已停用）"),
                        IsValid = imagePort, IsEnabled = tool.IsEnabled, IsSelected = selected,
                        TechnicalDetail = "FlowId：" + flow.FlowId + "\nToolId：" + group.Key + "\n正式端口：Image\n只取本周期采集原图；不读取子流程、上周期、失败模块图或活动缓存。" });
                }
                foreach (var id in selectedIds.Distinct(StringComparer.Ordinal))
                    if (!rows.Any(value => string.Equals(value.ToolId, id, StringComparison.Ordinal)))
                        rows.Add(new ProductionImageSourceRowS142 { ToolId = id, Label = "原图像源已失效（可取消）", IsValid = false, IsEnabled = false, IsSelected = true, TechnicalDetail = "原 ToolId：" + id + "\n不会按同名模块或 SourcePath 重新绑定。" });
                ProductionImageSourcesS142.ItemsSource = rows;
                ProductionImageSourceCountS142.Text = "选定 " + selectedIds.Count.ToString(CultureInfo.InvariantCulture) + " / 4 个根图像源";
                ProductionImageModeS142.IsEnabled = flow != null || policy != null;
                ProductionImageSourceStatusS142.Text = !string.IsNullOrWhiteSpace(productionImageSourceReadErrorS142) ? productionImageSourceReadErrorS142 : flow == null ?
                    "根流程标识缺失、重复或已失效；原策略保留，不按名称重新绑定。" :
                    productionImageDraftPoliciesS142.Count(value => value.FlowId == flow.FlowId) > 1 ? "此流程有重复策略，正式应用必须修正；本页不猜哪一份有效。" :
                    mode != "Off" && mode != "Failures" && mode != "All" ? "留图范围未知，请明确选择关闭 / 失败 / 全部；不把未知范围当启用。" :
                    rows.Any(value => value.IsSelected && (!value.IsValid || !value.IsEnabled)) ? "选定来源存在失效 / 停用项；取消或明确选新源后应用。" :
                    mode != "Off" && selectedIds.Count == 0 ? "请显式选择 1–4 个根图像源；未指定时不凭缓存补图。" :
                    mode == "Off" ? "此流程未启用；配置查看不新增磁盘写入。" :
                    "仅明确非模拟的生产完整周期留图；失败范围不包含用户 STOP，除非明确勾选。";
                ProductionImageEditorS142.IsEnabled = productionImageEditorEnabledS142;
            }
            finally { productionImageDraftUpdatingS142 = false; }
        }
    }
}
