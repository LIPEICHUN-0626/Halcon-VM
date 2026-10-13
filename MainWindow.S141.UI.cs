using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly ObservableCollection<ProductionResultDraftRowS141> productionResultDraftRowsS141 = new ObservableCollection<ProductionResultDraftRowS141>();
        private List<VmProjectFlowData> productionResultSourceFlowsS141 = new List<VmProjectFlowData>();
        private bool productionResultEditorUpdatingS141;
        private string productionResultSourceReadFailureS141;

        private sealed class ProductionResultDraftRowS141 : INotifyPropertyChanged
        {
            public VmProductionResultItemS141 Item { get; set; }
            public string DisplayTitle { get { return string.IsNullOrWhiteSpace(Item.Title) ? "未命名结果项" : Item.Title; } }
            public string SourceLabel { get; set; }
            public string TechnicalDetail { get { return "ItemId：" + Item.ItemId + "\nFlowId：" + Item.FlowId + "\nToolId：" + Item.ToolId + "\n正式端口：" + Item.PortName + "\n声明类型：" + Item.ValueType; } }
            public event PropertyChangedEventHandler PropertyChanged;
            public void Refresh()
            {
                var handler = PropertyChanged;
                if (handler == null) return;
                handler(this, new PropertyChangedEventArgs("DisplayTitle"));
                handler(this, new PropertyChangedEventArgs("SourceLabel"));
                handler(this, new PropertyChangedEventArgs("TechnicalDetail"));
            }
        }
        private sealed class ProductionResultSourceOptionS141
        {
            public string Id { get; set; }
            public string Label { get; set; }
            public string TechnicalDetail { get; set; }
            public string Type { get; set; }
        }

        private void LoadProductionResultDraftS141(VmProductionRunLayoutData layout)
        {
            if (ProductionResultDraftListS141 == null) return;
            productionResultEditorUpdatingS141 = true;
            try
            {
                productionResultDraftRowsS141.Clear();
                foreach (var item in (layout?.ResultItemsS141 ?? new List<VmProductionResultItemS141>()).Where(value => value != null).Take(8))
                    productionResultDraftRowsS141.Add(new ProductionResultDraftRowS141 { Item = CloneResultItemForUiS141(item) });
                productionResultSourceReadFailureS141 = null;
                try { productionResultSourceFlowsS141 = (CaptureRecipe().Flows ?? new List<VmProjectFlowData>()).Where(value => value != null).Select(value => value.Clone()).ToList(); }
                catch (Exception error) { productionResultSourceFlowsS141 = new List<VmProjectFlowData>(); productionResultSourceReadFailureS141 = "来源清单无法读取：" + error.Message; }
                ProductionResultDraftListS141.ItemsSource = productionResultDraftRowsS141;
                ProductionResultDecimalsS141.ItemsSource = Enumerable.Range(0, 9).ToList();
                ProductionLayoutTaskTabsS141.SelectedIndex = 0;
                ProductionResultDraftListS141.SelectedIndex = productionResultDraftRowsS141.Count > 0 ? 0 : -1;
            }
            finally { productionResultEditorUpdatingS141 = false; }
            RefreshProductionResultDraftUiS141();
        }

        private List<VmProductionResultItemS141> CaptureProductionResultDraftS141()
        { return productionResultDraftRowsS141.Take(8).Select(value => CloneResultItemForUiS141(value.Item)).ToList(); }

        private static VmProductionResultItemS141 CloneResultItemForUiS141(VmProductionResultItemS141 item)
        {
            return new VmProductionResultItemS141 { ItemId = item.ItemId, Title = item.Title, FlowId = item.FlowId,
                ToolId = item.ToolId, PortName = item.PortName, ValueType = item.ValueType, DecimalPlaces = item.DecimalPlaces };
        }

        private void RefreshProductionResultCardS141(VmProductionRunLayoutData layout)
        {
            if (ProductionCustomResultsS141 == null) return;
            var rows = (GetProductionResultRowsS141(layout) ?? new List<VmProductionResultRowS141>()).Take(8).ToList();
            ProductionCustomResultItemsS141.ItemsSource = rows;
            ProductionCustomResultsS141.Visibility = rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            string status = GetProductionResultStatusS141() ?? string.Empty;
            ProductionCustomResultStatusS141.Text = (productionLayoutDesignMode ? "布局草稿预览 · " : string.Empty) + status;
            ProductionCustomResultStatusS141.ToolTip = ProductionCustomResultStatusS141.Text;
            if (ProductionWidgetStack != null)
            {
                ProductionWidgetStack.Children.Remove(ProductionCustomResultsS141);
                int resultIndex = ProductionWidgetStack.Children.IndexOf(ProductionOverallResultWidget);
                ProductionWidgetStack.Children.Insert(resultIndex < 0 ? ProductionWidgetStack.Children.Count : resultIndex + 1, ProductionCustomResultsS141);
            }
        }

        private ProductionResultDraftRowS141 SelectedResultDraftS141()
        { return ProductionResultDraftListS141?.SelectedItem as ProductionResultDraftRowS141; }

        private bool CanEditProductionResultDraftS141()
        {
            if (productionResultEditorUpdatingS141 || productionLayoutDraftUpdating || !productionLayoutDesignMode) return false;
            if (!RequireEngineeringEditS137("编辑生产结果项草稿") || !CanChangeRecipeS134()) { RefreshProductionResultDraftUiS141(); return false; }
            if (recipeChangeoverFaultedS134) { ProductionLayoutDesignerStatusText.Text = "换型故障尚未解除，不能修改显示映射。"; RefreshProductionResultDraftUiS141(); return false; }
            return true;
        }

        private void NotifyProductionResultDraftChangedS141(object sender, RoutedEventArgs e)
        {
            foreach (var row in productionResultDraftRowsS141) { row.SourceLabel = ResultSourceLabelS141(row.Item); row.Refresh(); }
            RefreshProductionResultDraftUiS141();
            ProductionLayoutDraftValue_Changed(sender, e);
        }

        private void AddProductionResultItemS141_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            if (productionResultDraftRowsS141.Count >= 8) { ProductionResultSourceStatusS141.Text = "最多八项，请先移除不需要的结果项。"; return; }
            var row = new ProductionResultDraftRowS141 { Item = new VmProductionResultItemS141
                { ItemId = Guid.NewGuid().ToString("N"), Title = "结果项 " + (productionResultDraftRowsS141.Count + 1).ToString(CultureInfo.InvariantCulture),
                    FlowId = activeProjectFlowIdS104, ToolId = string.Empty, PortName = string.Empty, ValueType = string.Empty, DecimalPlaces = 2 } };
            productionResultDraftRowsS141.Add(row);
            ProductionResultDraftListS141.SelectedItem = row;
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void RemoveProductionResultItemS141_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            var row = SelectedResultDraftS141(); if (row == null) return;
            int index = productionResultDraftRowsS141.IndexOf(row);
            productionResultDraftRowsS141.Remove(row);
            ProductionResultDraftListS141.SelectedIndex = productionResultDraftRowsS141.Count == 0 ? -1 : Math.Min(index, productionResultDraftRowsS141.Count - 1);
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void ProductionResultDraftSelectionS141_Changed(object sender, SelectionChangedEventArgs e)
        { if (!productionResultEditorUpdatingS141) RefreshProductionResultDraftUiS141(); }

        private void ProductionResultTitleS141_Changed(object sender, TextChangedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            var row = SelectedResultDraftS141(); if (row == null) return;
            row.Item.Title = ProductionResultTitleS141.Text;
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void ProductionResultFlowS141_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            var row = SelectedResultDraftS141(); var option = ProductionResultFlowS141.SelectedItem as ProductionResultSourceOptionS141;
            if (row == null || option == null) return;
            if (string.Equals(row.Item.FlowId, option.Id, StringComparison.Ordinal)) return;
            row.Item.FlowId = option.Id; row.Item.ToolId = row.Item.PortName = row.Item.ValueType = string.Empty;
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void ProductionResultToolS141_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            var row = SelectedResultDraftS141(); var option = ProductionResultToolS141.SelectedItem as ProductionResultSourceOptionS141;
            if (row == null || option == null || string.Equals(row.Item.ToolId, option.Id, StringComparison.Ordinal)) return;
            row.Item.ToolId = option.Id; row.Item.PortName = row.Item.ValueType = string.Empty;
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void ProductionResultPortS141_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            var row = SelectedResultDraftS141(); var option = ProductionResultPortS141.SelectedItem as ProductionResultSourceOptionS141;
            if (row == null || option == null) return;
            row.Item.PortName = option.Id; row.Item.ValueType = option.Type;
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void ProductionResultDecimalsS141_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!CanEditProductionResultDraftS141()) return;
            var row = SelectedResultDraftS141();
            if (row == null || !string.Equals(row.Item.ValueType, "Number", StringComparison.OrdinalIgnoreCase) || !(ProductionResultDecimalsS141.SelectedItem is int)) return;
            row.Item.DecimalPlaces = (int)ProductionResultDecimalsS141.SelectedItem;
            NotifyProductionResultDraftChangedS141(sender, e);
        }

        private void RefreshProductionResultDraftUiS141()
        {
            if (ProductionResultDraftListS141 == null) return;
            productionResultEditorUpdatingS141 = true;
            try
            {
                foreach (var value in productionResultDraftRowsS141) { value.SourceLabel = ResultSourceLabelS141(value.Item); value.Refresh(); }
                var row = SelectedResultDraftS141(); var item = row?.Item;
                ProductionResultDraftCountS141.Text = productionResultDraftRowsS141.Count.ToString(CultureInfo.InvariantCulture) + " / 8 项 · 单位随真实输出，不改变判定";
                AddProductionResultItemButtonS141.IsEnabled = productionResultDraftRowsS141.Count < 8;
                RemoveProductionResultItemButtonS141.IsEnabled = row != null;
                ProductionResultEditorS141.IsEnabled = row != null;
                ProductionResultTitleS141.Text = item?.Title ?? string.Empty;
                var flowOptions = productionResultSourceFlowsS141.Where(value => !string.IsNullOrWhiteSpace(value.FlowId)).GroupBy(value => value.FlowId, StringComparer.Ordinal)
                    .Select(group => new ProductionResultSourceOptionS141 { Id = group.Key, Label = group.Count() == 1 ? group.First().Name : "流程标识重复 · 不可绑定", TechnicalDetail = "FlowId：" + group.Key }).ToList();
                PreserveUnresolvedOptionS141(flowOptions, item?.FlowId, "流程不存在 / 已变化");
                SetSourceOptionsS141(ProductionResultFlowS141, flowOptions, item?.FlowId);
                var flow = ResultFlowS141(item?.FlowId);
                var toolOptions = (flow?.Tools ?? new List<ToolFlowRecipeItem>()).Where(value => value != null && !string.IsNullOrWhiteSpace(value.ToolId)).GroupBy(value => value.ToolId, StringComparer.Ordinal)
                    .Select(group => new ProductionResultSourceOptionS141 { Id = group.Key,
                        Label = group.Count() == 1 ? (group.First().InstanceName ?? group.First().ToolType) + (group.First().IsEnabled ? string.Empty : "（已停用）") : "模块标识重复 · 不可绑定", TechnicalDetail = "ToolId：" + group.Key }).ToList();
                PreserveUnresolvedOptionS141(toolOptions, item?.ToolId, "模块不存在 / 已变化");
                SetSourceOptionsS141(ProductionResultToolS141, toolOptions, item?.ToolId);
                string portError;
                var ports = ResultPortsS141(item, out portError);
                var portOptions = ports.Select(value => new ProductionResultSourceOptionS141 { Id = value.PortName, Label = value.DisplayName + " · " + value.DataType,
                    Type = value.DataType, TechnicalDetail = "正式端口：" + value.PortName + "\n类型：" + value.DataType }).ToList();
                PreserveUnresolvedOptionS141(portOptions, item?.PortName, "端口不存在 / 类型待核对", item?.ValueType);
                SetSourceOptionsS141(ProductionResultPortS141, portOptions, item?.PortName);
                ProductionResultDecimalsPanelS141.Visibility = string.Equals(item?.ValueType, "Number", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
                ProductionResultDecimalsS141.SelectedItem = item != null && item.DecimalPlaces >= 0 && item.DecimalPlaces <= 8 ? (object)item.DecimalPlaces : null;
                ProductionResultSourceStatusS141.Text = ResultSourceStatusS141(item, ports, portError);
                ProductionResultSourceStatusS141.ToolTip = row?.TechnicalDetail;
            }
            finally { productionResultEditorUpdatingS141 = false; }
        }

        private VmProjectFlowData ResultFlowS141(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var matches = productionResultSourceFlowsS141.Where(value => string.Equals(value.FlowId, id, StringComparison.Ordinal)).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }
        private ToolFlowRecipeItem ResultToolS141(VmProductionResultItemS141 item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ToolId)) return null;
            var matches = (ResultFlowS141(item.FlowId)?.Tools ?? new List<ToolFlowRecipeItem>()).Where(value => value != null && string.Equals(value.ToolId, item.ToolId, StringComparison.Ordinal)).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }
        private List<VmPortDefinition> ResultPortsS141(VmProductionResultItemS141 item, out string error)
        {
            error = null;
            var tool = ResultToolS141(item); VmToolKind kind;
            if (tool == null || !Enum.TryParse(tool.ToolType, out kind) || !Enum.IsDefined(typeof(VmToolKind), kind)) return new List<VmPortDefinition>();
            try
            {
                using (var instance = new VmToolInstance { ToolId = tool.ToolId, Kind = kind, Parameters = tool.Parameters == null ? new VmToolParameterData() : tool.Parameters.Clone() })
                    return ToolMetadata.GetInstanceOutputPorts(instance).Where(value => value != null && ResultScalarTypeS141(value.DataType))
                        .GroupBy(value => value.PortName, StringComparer.Ordinal).Where(group => group.Count() == 1)
                        .Select(group => new VmPortDefinition { PortName = group.First().PortName, DisplayName = group.First().DisplayName, DataType = group.First().DataType }).ToList();
            }
            catch (Exception failure) { error = "正式端口无法解析：" + failure.Message; return new List<VmPortDefinition>(); }
        }
        private static bool ResultScalarTypeS141(string type)
        { return string.Equals(type, "Number", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "String", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "Boolean", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "ResultCode", StringComparison.OrdinalIgnoreCase); }
        private string ResultSourceLabelS141(VmProductionResultItemS141 item)
        {
            var flow = ResultFlowS141(item.FlowId); var tool = ResultToolS141(item);
            return (flow?.Name ?? "流程未指定 / 失效") + " / " + (tool?.InstanceName ?? "模块未指定 / 失效") + " / " + (string.IsNullOrWhiteSpace(item.PortName) ? "待选输出" : item.PortName);
        }
        private string ResultSourceStatusS141(VmProductionResultItemS141 item, List<VmPortDefinition> ports, string error)
        {
            if (item == null) return "添加或选择一项，再配置来源。";
            if (!string.IsNullOrWhiteSpace(productionResultSourceReadFailureS141)) return productionResultSourceReadFailureS141;
            if (ResultFlowS141(item.FlowId) == null) return "流程标识缺失、重复或已失效；不会按名称重新绑定。";
            var tool = ResultToolS141(item);
            if (tool == null) return "请选择该根流程的模块；内部子流程端口不能跨作用域绑定。";
            if (!tool.IsEnabled) return "来源模块已停用；本次不会产生有效值。";
            if (!string.IsNullOrWhiteSpace(error)) return error;
            var port = ports.FirstOrDefault(value => string.Equals(value.PortName, item.PortName, StringComparison.Ordinal));
            if (port == null) return "请选择 Number / String / Boolean / ResultCode 正式输出；旧端口失效时不猜替代来源。";
            if (!string.Equals(port.DataType, item.ValueType, StringComparison.OrdinalIgnoreCase)) return "正式端口类型已变化，请明确重新选择后再应用。";
            if (string.Equals(item.ValueType, "Number", StringComparison.OrdinalIgnoreCase) && (item.DecimalPlaces < 0 || item.DecimalPlaces > 8)) return "显示小数位须为 0–8。";
            return "来源已配置 · 仅本次生产周期显示；单位未声明时如实提示。";
        }
        private static void PreserveUnresolvedOptionS141(List<ProductionResultSourceOptionS141> values, string id, string label, string type = null)
        {
            if (!string.IsNullOrWhiteSpace(id) && !values.Any(value => string.Equals(value.Id, id, StringComparison.Ordinal)))
                values.Add(new ProductionResultSourceOptionS141 { Id = id, Label = label, Type = type, TechnicalDetail = "原稳定身份：" + id + "\n悬空来源保留，不按同名对象重新绑定。" });
        }
        private static void SetSourceOptionsS141(ComboBox picker, List<ProductionResultSourceOptionS141> values, string id)
        { picker.ItemsSource = values; picker.SelectedItem = values.FirstOrDefault(value => string.Equals(value.Id, id, StringComparison.Ordinal)); }
    }
}
