using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private readonly ExternalResourceService externalResourceService;
        private readonly ObservableCollection<VmExternalResourceData> externalResources = new ObservableCollection<VmExternalResourceData>();
        private ICollectionView externalResourceView;
        private bool externalResourceUiUpdating;

        private void InitializeExternalResourceWorkspace()
        {
            ExternalResourceListBox.ItemsSource = externalResources;
            DockScriptResourceComboBox.ItemsSource = externalResources;
            externalResourceView = CollectionViewSource.GetDefaultView(externalResources);
            externalResourceView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
            externalResourceView.Filter = FilterExternalResource;
            SetExternalResourceEditor(null);
            RefreshExternalResourceReferences();
        }

        private List<VmExternalResourceData> CaptureExternalResources()
        {
            return externalResources.Select(item => item.CloneNormalized()).ToList();
        }

        private void LoadExternalResourcesFromRecipe(VisionRecipe recipe)
        {
            string selectedId = (ExternalResourceListBox == null ? null : ExternalResourceListBox.SelectedItem as VmExternalResourceData)?.ResourceId;
            externalResources.Clear();
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (VmExternalResourceData source in recipe == null || recipe.ExternalResources == null ? new List<VmExternalResourceData>() : recipe.ExternalResources)
            {
                if (source == null) continue;
                VmExternalResourceData item = source.CloneNormalized();
                if (!ids.Add(item.ResourceId)) continue;
                externalResourceService.Inspect(item);
                externalResources.Add(item);
            }
            RefreshExternalResourceReferences();
            VmExternalResourceData selected = externalResources.FirstOrDefault(item => string.Equals(item.ResourceId, selectedId, StringComparison.OrdinalIgnoreCase)) ?? externalResources.FirstOrDefault();
            if (ExternalResourceListBox != null) ExternalResourceListBox.SelectedItem = selected;
            SetExternalResourceEditor(selected);
        }

        private void RefreshExternalResourceReferences()
        {
            var references = CaptureScopedToolReferencesS102();
            foreach (VmExternalResourceData resource in externalResources)
            {
                resource.ReferenceCount = references.Count(item => item.Tool.ToolType == VmToolKind.CSharpDllScript.ToString() && item.Tool.Parameters != null &&
                    string.Equals(item.Tool.Parameters.ScriptResourceId, resource.ResourceId, StringComparison.OrdinalIgnoreCase));
            }
            if (externalResourceView != null) externalResourceView.Refresh();
            if (ExternalResourceWorkspaceStatusText != null)
            {
                int runnable = externalResources.Count(item => item.IsRunnable);
                int attention = externalResources.Count - runnable;
                ExternalResourceWorkspaceStatusText.Text = externalResources.Count.ToString(CultureInfo.InvariantCulture) + " 个资源 · 可运行 " + runnable.ToString(CultureInfo.InvariantCulture) + " · 需处理 " + attention.ToString(CultureInfo.InvariantCulture);
            }
            if (DockScriptResourceComboBox != null) DockScriptResourceComboBox.Items.Refresh();
            VmExternalResourceData selected = ExternalResourceListBox == null ? null : ExternalResourceListBox.SelectedItem as VmExternalResourceData;
            if (selected != null) ExternalResourceReferenceText.Text = BuildExternalResourceReferenceText(selected);
            RefreshDockScriptResourceSummary();
        }

        private bool FilterExternalResource(object value)
        {
            VmExternalResourceData item = value as VmExternalResourceData;
            if (item == null) return false;
            string query = ExternalResourceSearchTextBox == null ? string.Empty : (ExternalResourceSearchTextBox.Text ?? string.Empty).Trim();
            if (query.Length > 0)
            {
                string searchable = string.Join(" ", new[] { item.Name, item.AssemblyVersion, item.Sha256, item.SourcePath, item.DefaultTypeName, item.DefaultMethodName });
                if (searchable.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) < 0) return false;
            }
            ComboBoxItem selected = ExternalResourceHealthFilterComboBox == null ? null : ExternalResourceHealthFilterComboBox.SelectedItem as ComboBoxItem;
            string filter = selected == null ? "All" : Convert.ToString(selected.Tag, CultureInfo.InvariantCulture);
            if (filter == "Runnable" && !item.IsRunnable) return false;
            if (filter == "Attention" && item.HealthState == VmExternalResourceHealth.Healthy) return false;
            return true;
        }

        private void ExternalResourceFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (externalResourceUiUpdating || externalResourceView == null) return;
            externalResourceView.Refresh();
        }

        private void OpenExternalResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            if (productionRunMode)
            {
                HeaderStatusText.Text = "请先返回开发模式，再管理外部资源。";
                return;
            }
            if (GlobalVariableWorkspace != null) GlobalVariableWorkspace.Visibility = Visibility.Collapsed;
            if (ScriptInterfaceWorkspace != null) ScriptInterfaceWorkspace.Visibility = Visibility.Collapsed;
            MainWorkspaceGrid.Visibility = Visibility.Collapsed;
            ExternalResourceWorkspace.Visibility = Visibility.Visible;
            DeveloperMoreToggleButton.IsChecked = false;
            foreach (VmExternalResourceData resource in externalResources) externalResourceService.Inspect(resource);
            RefreshExternalResourceReferences();
            VmExternalResourceData selected = ExternalResourceListBox.SelectedItem as VmExternalResourceData ?? externalResources.FirstOrDefault();
            ExternalResourceListBox.SelectedItem = selected;
            SetExternalResourceEditor(selected);
            HeaderStatusText.Text = "外部资源工作台 · 当前方案的受管 DLL、版本、哈希、健康和流程引用。";
        }

        private void CloseExternalResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            ExternalResourceWorkspace.Visibility = Visibility.Collapsed;
            MainWorkspaceGrid.Visibility = Visibility.Visible;
            HeaderStatusText.Text = "已关闭外部资源工作台。";
            RefreshUiState();
        }

        private void ExternalResourceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (externalResourceUiUpdating) return;
            SetExternalResourceEditor(ExternalResourceListBox.SelectedItem as VmExternalResourceData);
        }

        private void SetExternalResourceEditor(VmExternalResourceData resource)
        {
            externalResourceUiUpdating = true;
            try
            {
                bool selected = resource != null;
                ExternalResourceEditorTitleText.Text = selected ? "资源详情" : "导入或选择资源";
                ExternalResourceIdText.Text = selected ? "ID  " + resource.ResourceId : string.Empty;
                ExternalResourceNameTextBox.Text = selected ? resource.Name : string.Empty;
                ExternalResourceTypeNameTextBox.Text = selected ? resource.DefaultTypeName : "UserScripts.ScriptEntry";
                ExternalResourceMethodNameTextBox.Text = selected ? resource.DefaultMethodName : "Execute";
                ExternalResourceHealthText.Text = selected ? resource.HealthDisplayText : "待导入";
                ExternalResourceSourcePathText.Text = selected ? (string.IsNullOrWhiteSpace(resource.SourcePath) ? "原始来源不可用" : resource.SourcePath) : "导入时选择 DLL";
                ExternalResourceManagedPathText.Text = selected ? (string.IsNullOrWhiteSpace(resource.ManagedPath) ? "--" : resource.ManagedPath) : "--";
                ExternalResourceVersionHashText.Text = selected ? resource.VersionDisplayText + " · SHA-256 " + resource.Sha256 : "--";
                ExternalResourceReferenceText.Text = selected ? BuildExternalResourceReferenceText(resource) : "引用 0";
                ExternalResourceDiagnosticsText.Text = selected ? resource.HealthDetail : "导入后显示签名与完整性状态。";
                ExternalResourceValidationText.Text = string.Empty;
                ExternalResourceHealthText.Foreground = new SolidColorBrush(selected && resource.IsRunnable ? Color.FromRgb(4, 120, 87) : Color.FromRgb(185, 28, 28));
            }
            finally { externalResourceUiUpdating = false; }
        }

        private string BuildExternalResourceReferenceText(VmExternalResourceData resource)
        {
            List<string> names = CaptureScopedToolReferencesS102().Where(item => item.Tool.ToolType == VmToolKind.CSharpDllScript.ToString() && item.Tool.Parameters != null &&
                string.Equals(item.Tool.Parameters.ScriptResourceId, resource.ResourceId, StringComparison.OrdinalIgnoreCase)).Select(item => item.DisplayPath).ToList();
            return "引用 " + names.Count.ToString(CultureInfo.InvariantCulture) + (names.Count == 0 ? string.Empty : "（" + string.Join("、", names) + "）");
        }

        private void ImportExternalResourceButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("导入DLL资源");
            if (editLeaseS137 < 0) return;
            OpenFileDialog dialog = CreateExternalResourceFileDialog("导入受信任托管 DLL");
            if (dialog.ShowDialog(this) != true) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            ImportExternalResourceFromPath(dialog.FileName, null);
        }

        private void ReplaceExternalResourceButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("替换DLL资源");
            if (editLeaseS137 < 0) return;
            VmExternalResourceData selected = ExternalResourceListBox.SelectedItem as VmExternalResourceData;
            if (selected == null) { ExternalResourceValidationText.Text = "请先选择要替换的资源。"; return; }
            OpenFileDialog dialog = CreateExternalResourceFileDialog("替换受管 DLL 来源");
            if (File.Exists(selected.SourcePath)) dialog.InitialDirectory = Path.GetDirectoryName(selected.SourcePath);
            if (dialog.ShowDialog(this) != true) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            ImportExternalResourceFromPath(dialog.FileName, selected);
        }

        private static OpenFileDialog CreateExternalResourceFileDialog(string title)
        {
            return new OpenFileDialog { Title = title, Filter = "托管程序集 (*.dll)|*.dll", CheckFileExists = true, Multiselect = false };
        }

        private VmExternalResourceData ImportExternalResourceFromPath(string path, VmExternalResourceData existing)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("写入DLL资源版本");
            if (editLeaseS137 < 0) return null;
            if (!CanNavigateInputRepairS96() || !ConfirmDockDraftChanges("修改外部资源")) return null;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return null;
            try
            {
                string typeName = (ExternalResourceTypeNameTextBox.Text ?? string.Empty).Trim();
                string methodName = (ExternalResourceMethodNameTextBox.Text ?? string.Empty).Trim();
                string name = (ExternalResourceNameTextBox.Text ?? string.Empty).Trim();
                if (name.Length == 0) name = Path.GetFileNameWithoutExtension(path);
                if (name.Length > 80 || name.Any(char.IsControl)) throw new InvalidOperationException("资源名称必须为 1–80 个非控制字符。");
                if (externalResources.Any(item => !ReferenceEquals(item, existing) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("资源名称不能重复（不区分大小写）。");
                FlowEditSnapshot before = CaptureFlowEditSnapshot();
                VmExternalResourceData imported = externalResourceService.Import(path, name, typeName, methodName, existing == null ? null : existing.ResourceId);
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return null;
                if (existing == null) externalResources.Add(imported);
                else
                {
                    int index = externalResources.IndexOf(existing);
                    externalResources[index] = imported;
                    InvalidateScriptsReferencingResource(imported.ResourceId, "外部资源版本已替换");
                }
                RefreshExternalResourceReferences();
                ExternalResourceListBox.SelectedItem = imported;
                SetExternalResourceEditor(imported);
                CommitFlowEditHistory((existing == null ? "导入外部资源：" : "替换外部资源：") + imported.Name, before);
                HeaderStatusText.Text = "已导入受管 DLL：" + imported.Name + " · " + imported.VersionDisplayText + " · SHA-256 " + imported.ShortSha256;
                LogInfo(HeaderStatusText.Text);
                return imported;
            }
            catch (Exception ex)
            {
                ExternalResourceValidationText.Text = ex.Message;
                AppendAlarm("外部资源导入失败：" + ex.Message);
                return null;
            }
        }

        private void RefreshExternalResourceButton_Click(object sender, RoutedEventArgs e)
        {
            VmExternalResourceData selected = ExternalResourceListBox.SelectedItem as VmExternalResourceData;
            if (selected == null) { ExternalResourceValidationText.Text = "请先选择资源。"; return; }
            if (string.IsNullOrWhiteSpace(selected.SourcePath) || !File.Exists(selected.SourcePath)) { ExternalResourceValidationText.Text = "原始来源不存在，请使用“替换来源”。"; return; }
            ExternalResourceTypeNameTextBox.Text = selected.DefaultTypeName;
            ExternalResourceMethodNameTextBox.Text = selected.DefaultMethodName;
            ExternalResourceNameTextBox.Text = selected.Name;
            ImportExternalResourceFromPath(selected.SourcePath, selected);
        }

        private void ApplyExternalResourceButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("修改DLL入口");
            if (editLeaseS137 < 0) return;
            if (!CanNavigateInputRepairS96() || !ConfirmDockDraftChanges("修改资源入口")) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            VmExternalResourceData selected = ExternalResourceListBox.SelectedItem as VmExternalResourceData;
            if (selected == null) { ExternalResourceValidationText.Text = "请先选择资源。"; return; }
            try
            {
                string name = (ExternalResourceNameTextBox.Text ?? string.Empty).Trim();
                string typeName = (ExternalResourceTypeNameTextBox.Text ?? string.Empty).Trim();
                string methodName = (ExternalResourceMethodNameTextBox.Text ?? string.Empty).Trim();
                if (name.Length == 0 || name.Length > 80 || name.Any(char.IsControl)) throw new InvalidOperationException("资源名称必须为 1–80 个非控制字符。");
                if (externalResources.Any(item => !ReferenceEquals(item, selected) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("资源名称不能重复（不区分大小写）。");
                string path = externalResourceService.ResolveResourcePathS139(selected);
                if (!File.Exists(path)) throw new FileNotFoundException("受管副本不存在。", path);
                if (!string.Equals(ExternalResourceService.ComputeSha256(path), selected.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("受管副本 SHA-256 与配方记录不一致，不能修改入口。");
                scriptToolService.Validate(new ScriptToolExecutionRequest { Mode = VmScriptExecutionMode.Dll, AssemblyPath = path, TypeName = typeName, MethodName = methodName, TimeoutMilliseconds = 1000 });
                FlowEditSnapshot before = CaptureFlowEditSnapshot();
                if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                selected.Name = name;
                selected.DefaultTypeName = typeName;
                selected.DefaultMethodName = methodName;
                externalResourceService.Inspect(selected);
                InvalidateScriptsReferencingResource(selected.ResourceId, "外部资源入口已修改");
                RefreshExternalResourceReferences();
                SetExternalResourceEditor(selected);
                CommitFlowEditHistory("应用外部资源入口：" + selected.Name, before);
            }
            catch (Exception ex) { ExternalResourceValidationText.Text = ex.Message; }
        }

        private void DeleteExternalResourceButton_Click(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("删除DLL资源");
            if (editLeaseS137 < 0) return;
            if (!CanNavigateInputRepairS96() || !ConfirmDockDraftChanges("删除外部资源")) return;
            if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
            VmExternalResourceData selected = ExternalResourceListBox.SelectedItem as VmExternalResourceData;
            if (selected == null) return;
            RefreshExternalResourceReferences();
            if (selected.ReferenceCount > 0) { ExternalResourceValidationText.Text = "不能删除仍被流程引用的资源：" + BuildExternalResourceReferenceText(selected); return; }
            FlowEditSnapshot before = CaptureFlowEditSnapshot();
            int index = externalResources.IndexOf(selected);
            externalResources.Remove(selected);
            RefreshExternalResourceReferences();
            ExternalResourceListBox.SelectedItem = externalResources.Count == 0 ? null : externalResources[Math.Min(index, externalResources.Count - 1)];
            CommitFlowEditHistory("删除外部资源：" + selected.Name, before);
            HeaderStatusText.Text = "已从当前方案移除资源；版本化受管副本保留以支持撤销/重做。";
        }

        private void InvalidateScriptsReferencingResource(string resourceId, string reason)
        {
            InvalidateScopedDependenciesS102(tool => tool.ToolType == VmToolKind.CSharpDllScript.ToString() && tool.Parameters != null &&
                string.Equals(tool.Parameters.ScriptResourceId, resourceId, StringComparison.OrdinalIgnoreCase), reason);
        }

        private VmExternalResourceData GetExternalResourceById(string resourceId)
        {
            return externalResources.FirstOrDefault(item => string.Equals(item.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase));
        }

        private void DockScriptResourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VmExternalResourceData resource = GetExternalResourceById(DockScriptResourceComboBox == null ? null : DockScriptResourceComboBox.SelectedValue as string);
            if (!dockDraftUpdating && resource != null)
            {
                DockScriptTypeNameTextBox.Text = resource.DefaultTypeName;
                DockScriptMethodNameTextBox.Text = resource.DefaultMethodName;
                DockScriptLegacyPathExpander.IsExpanded = false;
            }
            RefreshDockScriptResourceSummary();
            if (!dockDraftUpdating) DockDraftValue_Changed(sender, e);
        }

        private void RefreshDockScriptResourceSummary()
        {
            if (DockScriptResourceSummaryText == null) return;
            VmExternalResourceData resource = GetExternalResourceById(DockScriptResourceComboBox == null ? null : DockScriptResourceComboBox.SelectedValue as string);
            if (resource == null)
            {
                DockScriptResourceSummaryText.Text = externalResources.Count == 0 ? "当前方案尚无受管 DLL，请打开外部资源工作台导入。" : "请选择受管资源；旧方案仍可展开兼容路径。";
                return;
            }
            DockScriptResourceSummaryText.Text = resource.VersionDisplayText + " · SHA-256 " + resource.ShortSha256 + " · " + resource.HealthDisplayText + " · " + resource.ReferenceSummary;
        }

        private string ValidateScriptResourceDockDraft(VmToolParameterData parameters)
        {
            if (parameters == null || VmScriptExecutionMode.Normalize(parameters.ScriptExecutionMode) != VmScriptExecutionMode.Dll || string.IsNullOrWhiteSpace(parameters.ScriptResourceId)) return string.Empty;
            VmExternalResourceData resource = GetExternalResourceById(parameters.ScriptResourceId);
            if (resource == null) return "选择的受管 DLL 资源不存在，请重新选择。";
            try
            {
                string path = externalResourceService.EnsureReadyForExecution(resource);
                scriptToolService.Validate(new ScriptToolExecutionRequest { Mode = VmScriptExecutionMode.Dll, AssemblyPath = path, TypeName = parameters.ScriptTypeName, MethodName = parameters.ScriptMethodName, TimeoutMilliseconds = parameters.ScriptTimeoutMilliseconds });
                return string.Empty;
            }
            catch (Exception ex) { return ex.Message; }
        }

        private ScriptToolExecutionRequest CreateScriptExecutionRequest(VmToolParameterData parameters, ScriptToolContext context)
        {
            string assemblyPath = parameters.ScriptAssemblyPath;
            string typeName = parameters.ScriptTypeName;
            string methodName = parameters.ScriptMethodName;
            if (VmScriptExecutionMode.Normalize(parameters.ScriptExecutionMode) == VmScriptExecutionMode.Dll && !string.IsNullOrWhiteSpace(parameters.ScriptResourceId))
            {
                VmExternalResourceData resource = GetExternalResourceById(parameters.ScriptResourceId);
                assemblyPath = externalResourceService.EnsureReadyForExecution(resource);
                if (string.IsNullOrWhiteSpace(typeName)) typeName = resource.DefaultTypeName;
                if (string.IsNullOrWhiteSpace(methodName)) methodName = resource.DefaultMethodName;
            }
            return new ScriptToolExecutionRequest
            {
                Mode = parameters.ScriptExecutionMode,
                InlineCode = parameters.ScriptInlineCode,
                AssemblyPath = assemblyPath,
                TypeName = typeName,
                MethodName = methodName,
                TimeoutMilliseconds = parameters.ScriptTimeoutMilliseconds,
                Context = context
            };
        }
    }
}
