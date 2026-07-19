using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;
using Microsoft.Win32;

namespace HalconWinFormsDemo
{
    public partial class ToolConfigurationWindow : Window
    {
        private readonly VmToolInstance tool;
        private readonly Action<VmToolInstance> trialRun;
        private readonly Func<string, string> validateInstanceName;
        private readonly ObservableCollection<VmRoiBindingItem> roiBindingRows = new ObservableCollection<VmRoiBindingItem>();
        private readonly ObservableCollection<VmInputPortEditorRow> inputPortRows = new ObservableCollection<VmInputPortEditorRow>();
        private readonly List<VmToolInstance> flowTools;

        public ToolConfigurationWindow(
            VmToolInstance tool,
            Action<VmToolInstance> trialRun,
            Func<string, string> validateInstanceName,
            IEnumerable<VmRoiLayer> roiLayers,
            IEnumerable<VmToolInstance> flowTools)
        {
            if (tool == null) throw new ArgumentNullException("tool");
            this.tool = tool;
            this.trialRun = trialRun;
            this.validateInstanceName = validateInstanceName;
            this.flowTools = (flowTools ?? Enumerable.Empty<VmToolInstance>()).ToList();
            foreach (VmRoiLayer layer in roiLayers ?? Enumerable.Empty<VmRoiLayer>())
            {
                roiBindingRows.Add(new VmRoiBindingItem { Layer = layer, IsBound = tool.IsRoiBound(layer.RoiId) });
            }
            InitializeComponent();
            LoadDraft();
        }

        public bool WasApplied { get; private set; }

        public bool OpenRoiWorkspaceRequested { get; private set; }

        public VisionTool? RequestedRoiTool { get; private set; }

        public bool OpenIoWorkspaceRequested { get; private set; }

        private void LoadDraft()
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            p.Normalize();
            ModuleTitleText.Text = tool.InstanceName;
            ModuleTypeText.Text = tool.Category + " / " + tool.DisplayType;
            ModuleStateText.Text = tool.ConfigurationStatus;
            InstanceNameTextBox.Text = tool.InstanceName;
            EnabledCheckBox.IsChecked = tool.IsEnabled;
            BlobMinTextBox.Text = F(p.BlobMinGray); BlobMaxTextBox.Text = F(p.BlobMaxGray); BlobAreaTextBox.Text = F(p.BlobMinArea);
            GrayMinTextBox.Text = F(p.GrayMin); GrayMaxTextBox.Text = F(p.GrayMax); EdgeThresholdTextBox.Text = F(p.EdgeThreshold);
            NumericOperatorComboBox.ItemsSource = NumericJudgeOperatorOption.CreateAll(); NumericOperatorComboBox.SelectedValue = tool.NumericOperator;
            NumericLowerTextBox.Text = F(tool.NumericLowerLimit); NumericUpperTextBox.Text = F(tool.NumericUpperLimit); NumericToleranceTextBox.Text = F(tool.NumericTolerance);
            LocalImagePathTextBox.Text = p.LocalImagePath ?? string.Empty;
            LocalImageSerialTextBox.Text = p.LocalImageSerialNumber ?? string.Empty;
            ImageSourcePanel.Visibility = tool.Kind == VmToolKind.ImageSource ? Visibility.Visible : Visibility.Collapsed;
            BlobPanel.Visibility = tool.Kind == VmToolKind.Blob ? Visibility.Visible : Visibility.Collapsed;
            GrayPanel.Visibility = tool.Kind == VmToolKind.GrayStat ? Visibility.Visible : Visibility.Collapsed;
            EdgePanel.Visibility = tool.Kind == VmToolKind.EdgeMeasure ? Visibility.Visible : Visibility.Collapsed;
            NumericPanel.Visibility = tool.Kind == VmToolKind.NumericJudge ? Visibility.Visible : Visibility.Collapsed;
            CompatibilityPanel.Visibility = tool.Kind == VmToolKind.ShapeMatch || tool.Kind == VmToolKind.HDevelop ? Visibility.Visible : Visibility.Collapsed;
            InputText.Text = tool.ConnectionSummary + " · " + tool.ConnectionStatus;
            BuildInputPortRows();
            RoiBindingItemsControl.ItemsSource = roiBindingRows;
            RoiExecutionModeComboBox.ItemsSource = VmRoiExecutionMode.CreateAll();
            RoiExecutionModeComboBox.SelectedValue = VmRoiExecutionMode.Normalize(p.RoiExecutionMode);
            bool supportsRoi = ToolMetadata.SupportsRoi(tool.Kind);
            bool supportsExecutionMode = tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure;
            RoiWorkspacePanel.Visibility = supportsRoi ? Visibility.Visible : Visibility.Collapsed;
            RoiDrawPanel.Visibility = supportsRoi ? Visibility.Visible : Visibility.Collapsed;
            RoiUnsupportedText.Visibility = supportsRoi ? Visibility.Collapsed : Visibility.Visible;
            RoiModePanel.Visibility = supportsExecutionMode ? Visibility.Visible : Visibility.Collapsed;
            EmptyRoiText.Visibility = supportsRoi && roiBindingRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RoiText.Text = BuildRoiSummary();
            ResultText.Text = tool.ResultCode + " · " + tool.OutputSummary + " · " + tool.ElapsedText;
            ErrorText.Text = string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "无错误" : tool.ErrorMessage;
            RoiResultsDataGrid.ItemsSource = tool.RoiResults;
            HelpText.Text = ToolMetadata.GetDescription(tool.Kind);
            FooterText.Text = "实例参数草稿";
        }

        private bool TryReadDraft(out VmToolParameterData parameters, out double lower, out double upper, out double tolerance, out string comparisonOperator)
        {
            parameters = (tool.Parameters ?? new VmToolParameterData()).Clone(); lower = tool.NumericLowerLimit; upper = tool.NumericUpperLimit; tolerance = tool.NumericTolerance; comparisonOperator = tool.NumericOperator;
            if (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure)
            {
                parameters.RoiExecutionMode = VmRoiExecutionMode.Normalize(RoiExecutionModeComboBox.SelectedValue as string);
            }
            string inputError = ValidateInputDraft(parameters);
            if (!string.IsNullOrWhiteSpace(inputError)) return Fail(inputError);
            double value;
            if (tool.Kind == VmToolKind.ImageSource)
            {
                parameters.LocalImagePath = LocalImagePathTextBox.Text == null ? string.Empty : LocalImagePathTextBox.Text.Trim();
                parameters.LocalImageSerialNumber = LocalImageSerialTextBox.Text == null ? string.Empty : LocalImageSerialTextBox.Text.Trim();
            }
            else if (tool.Kind == VmToolKind.Blob)
            {
                if (!Read(BlobMinTextBox.Text, out value)) return Fail("Blob 灰度下限必须是数值。"); parameters.BlobMinGray = value;
                if (!Read(BlobMaxTextBox.Text, out value)) return Fail("Blob 灰度上限必须是数值。"); parameters.BlobMaxGray = value;
                if (!Read(BlobAreaTextBox.Text, out value)) return Fail("Blob 最小面积必须是数值。"); parameters.BlobMinArea = value;
            }
            else if (tool.Kind == VmToolKind.GrayStat)
            {
                if (!Read(GrayMinTextBox.Text, out value)) return Fail("灰度下限必须是数值。"); parameters.GrayMin = value;
                if (!Read(GrayMaxTextBox.Text, out value)) return Fail("灰度上限必须是数值。"); parameters.GrayMax = value;
            }
            else if (tool.Kind == VmToolKind.EdgeMeasure)
            {
                if (!Read(EdgeThresholdTextBox.Text, out value)) return Fail("边缘阈值必须是数值。"); parameters.EdgeThreshold = value;
            }
            else if (tool.Kind == VmToolKind.NumericJudge)
            {
                comparisonOperator = NumericOperatorComboBox.SelectedValue as string;
                if (!Read(NumericLowerTextBox.Text, out lower) || !Read(NumericUpperTextBox.Text, out upper) || !Read(NumericToleranceTextBox.Text, out tolerance)) return Fail("数值判定阈值和容差必须是数值。");
                string numericError = VmNumericJudgeParameterValidator.Validate(comparisonOperator, lower, upper, tolerance);
                if (!string.IsNullOrWhiteSpace(numericError)) return Fail(numericError);
            }
            string error = parameters.Validate(tool.Kind); if (!string.IsNullOrWhiteSpace(error)) return Fail(error);
            string name = InstanceNameTextBox.Text == null ? string.Empty : InstanceNameTextBox.Text.Trim();
            string nameError = validateInstanceName == null ? (string.IsNullOrWhiteSpace(name) ? "实例名称不能为空。" : string.Empty) : validateInstanceName(name);
            if (!string.IsNullOrWhiteSpace(nameError)) return Fail(nameError);
            ValidationText.Text = string.Empty; return true;
        }

        private bool ApplyDraft()
        {
            VmToolParameterData p; double lower; double upper; double tolerance; string comparisonOperator; if (!TryReadDraft(out p, out lower, out upper, out tolerance, out comparisonOperator)) return false;
            tool.InstanceName = InstanceNameTextBox.Text.Trim(); tool.IsEnabled = EnabledCheckBox.IsChecked == true; tool.Parameters = p;
            if (ToolMetadata.SupportsRoi(tool.Kind)) tool.ReplaceRoiBindings(roiBindingRows.Where(item => item.IsBound).Select(item => item.RoiId));
            ApplyInputDraft();
            if (tool.Kind == VmToolKind.NumericJudge) { tool.NumericOperator = comparisonOperator; tool.NumericLowerLimit = lower; tool.NumericUpperLimit = upper; tool.NumericTolerance = tolerance; }
            tool.ClearRuntimeOutputs(); tool.ResultCode = "--"; tool.RunStatus = "未运行"; tool.OutputSummary = "配置已修改，等待运行"; tool.ErrorMessage = string.Empty;
            WasApplied = true; ModuleTitleText.Text = tool.InstanceName; RoiText.Text = BuildRoiSummary(); FooterText.Text = "已应用到当前实例"; return true;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) { ApplyDraft(); }
        private void OkButton_Click(object sender, RoutedEventArgs e) { if (ApplyDraft()) DialogResult = true; }
        private void TrialRunButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolParameterData draft; double lower; double upper; double tolerance; string comparisonOperator; if (!TryReadDraft(out draft, out lower, out upper, out tolerance, out comparisonOperator)) return;
            VmToolParameterData old = tool.Parameters; double oldLower = tool.NumericLowerLimit; double oldUpper = tool.NumericUpperLimit; double oldTolerance = tool.NumericTolerance; string oldOperator = tool.NumericOperator; List<string> oldBindings = tool.BoundRoiIds.ToList(); List<VmToolInputBindingData> oldInputBindings = tool.InputBindings.Select(item => item.Clone()).ToList(); string oldInputToolId = tool.InputToolId; string oldInputPortName = tool.InputPortName;
            try { tool.Parameters = draft; tool.ReplaceRoiBindings(roiBindingRows.Where(item => item.IsBound).Select(item => item.RoiId)); ApplyInputDraft(); tool.NumericLowerLimit = lower; tool.NumericUpperLimit = upper; tool.NumericTolerance = tolerance; tool.NumericOperator = comparisonOperator; if (trialRun != null) trialRun(tool); RefreshInputRowStates(); FooterText.Text = "试运行完成，草稿尚未应用"; }
            finally { tool.Parameters = old; tool.ReplaceRoiBindings(oldBindings); tool.ReplaceInputBindings(oldInputBindings); tool.InputToolId = oldInputToolId; tool.InputPortName = oldInputPortName; tool.NumericLowerLimit = oldLower; tool.NumericUpperLimit = oldUpper; tool.NumericTolerance = oldTolerance; tool.NumericOperator = oldOperator; }
        }

        private void BuildInputPortRows()
        {
            inputPortRows.Clear();
            int toolIndex = flowTools.IndexOf(tool);
            IEnumerable<VmToolInstance> previousTools = toolIndex < 0 ? Enumerable.Empty<VmToolInstance>() : flowTools.Take(toolIndex);
            foreach (VmPortDefinition port in ToolMetadata.GetInputPorts(tool.Kind))
            {
                List<VmInputSourceOption> options = new List<VmInputSourceOption>
                {
                    new VmInputSourceOption
                    {
                        Key = GetDefaultInputKey(port.PortName),
                        DisplayText = GetDefaultInputText(port),
                        DataType = port.DataType,
                        IsDefault = true,
                        IsValid = true
                    }
                };

                if (CanSubscribePort(port))
                {
                    foreach (VmToolInstance source in previousTools)
                    {
                        foreach (VmPortDefinition output in ToolMetadata.GetOutputPorts(source.Kind).Where(item => string.Equals(item.DataType, port.DataType, StringComparison.OrdinalIgnoreCase)))
                        {
                            options.Add(new VmInputSourceOption
                            {
                                Key = BuildSourceKey(source.ToolId, output.PortName),
                                DisplayText = source.SequenceText + "  " + source.InstanceName + "." + output.DisplayName,
                                SourceToolId = source.ToolId,
                                SourcePortName = output.PortName,
                                DataType = output.DataType,
                                IsValid = source.IsEnabled
                            });
                        }
                    }
                }

                string selectedKey = GetSavedInputSourceKey(port);
                if (!options.Any(item => string.Equals(item.Key, selectedKey, StringComparison.OrdinalIgnoreCase)))
                {
                    options.Add(new VmInputSourceOption
                    {
                        Key = selectedKey,
                        DisplayText = "⚠ 已保存的来源无效或不在当前工具之前",
                        DataType = port.DataType,
                        IsValid = false
                    });
                }

                VmInputPortEditorRow row = new VmInputPortEditorRow
                {
                    PortName = port.PortName,
                    DisplayName = port.DisplayName,
                    DataType = port.DataType,
                    IsOptional = port.IsOptional,
                    SourceOptions = options,
                    SelectedSourceKey = selectedKey
                };
                RefreshInputRowState(row);
                inputPortRows.Add(row);
            }

            InputPortItemsControl.ItemsSource = inputPortRows;
            InputText.Text = BuildInputDraftSummary();
        }

        private bool CanSubscribePort(VmPortDefinition port)
        {
            return tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value" ||
                   (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure) &&
                   (port.PortName == "ROI" || port.PortName == "Image");
        }

        private void BrowseLocalImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择本地图像源",
                Filter = "图像文件|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|所有文件|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) == true)
            {
                LocalImagePathTextBox.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(LocalImageSerialTextBox.Text))
                {
                    LocalImageSerialTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                }
                FooterText.Text = "已选择本地图像，应用或试运行后生效";
            }
        }

        private string GetSavedInputSourceKey(VmPortDefinition port)
        {
            if (tool.Kind == VmToolKind.NumericJudge && port.PortName == "Value" && !string.IsNullOrWhiteSpace(tool.InputToolId) && !string.IsNullOrWhiteSpace(tool.InputPortName))
            {
                return BuildSourceKey(tool.InputToolId, tool.InputPortName);
            }

            VmToolInputBindingData binding = tool.GetInputBinding(port.PortName);
            return binding == null ? GetDefaultInputKey(port.PortName) : BuildSourceKey(binding.SourceToolId, binding.SourcePortName);
        }

        private string ValidateInputDraft(VmToolParameterData parameters)
        {
            foreach (VmInputPortEditorRow row in inputPortRows)
            {
                VmInputSourceOption selected = row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                if (selected == null || !selected.IsValid)
                {
                    return row.DisplayName + " 的输入来源无效。";
                }

                if (!selected.IsDefault && row.DataType == "Region" && VmRoiExecutionMode.Normalize(parameters.RoiExecutionMode) == VmRoiExecutionMode.PerRoi)
                {
                    return "逐 ROI 模式使用本地稳定 RoiId，不能同时订阅上游 Region；请切换为合并 ROI 或恢复默认输入。";
                }
            }

            return string.Empty;
        }

        private void ApplyInputDraft()
        {
            foreach (VmInputPortEditorRow row in inputPortRows)
            {
                VmInputSourceOption selected = row.SourceOptions.First(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                if (tool.Kind == VmToolKind.NumericJudge && row.PortName == "Value")
                {
                    tool.InputToolId = selected.IsDefault ? null : selected.SourceToolId;
                    tool.InputPortName = selected.IsDefault ? null : selected.SourcePortName;
                }
                else if (selected.IsDefault)
                {
                    tool.RemoveInputBinding(row.PortName);
                }
                else
                {
                    tool.SetInputBinding(row.PortName, selected.SourceToolId, selected.SourcePortName);
                }
            }
        }

        private void InputSourceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            VmInputPortEditorRow row = comboBox == null ? null : comboBox.DataContext as VmInputPortEditorRow;
            if (row == null) return;
            RefreshInputRowState(row);
            InputPortItemsControl.Items.Refresh();
            InputText.Text = BuildInputDraftSummary();
        }

        private void ResetInputSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VmInputPortEditorRow row in inputPortRows)
            {
                row.SelectedSourceKey = GetDefaultInputKey(row.PortName);
                RefreshInputRowState(row);
            }
            InputPortItemsControl.Items.Refresh();
            InputText.Text = BuildInputDraftSummary();
        }

        private void OpenIoWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ApplyDraft()) return;
            OpenIoWorkspaceRequested = true;
            Close();
        }

        private void RefreshInputRowStates()
        {
            foreach (VmInputPortEditorRow row in inputPortRows) RefreshInputRowState(row);
            InputPortItemsControl.Items.Refresh();
            InputText.Text = BuildInputDraftSummary();
        }

        private void RefreshInputRowState(VmInputPortEditorRow row)
        {
            VmInputSourceOption selected = row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
            if (selected == null || !selected.IsValid)
            {
                row.CurrentValue = "--";
                row.StatusText = "输入来源无效";
                return;
            }

            if (selected.IsDefault)
            {
                row.CurrentValue = "--";
                row.StatusText = selected.DisplayText;
                return;
            }

            VmToolInstance source = flowTools.FirstOrDefault(item => string.Equals(item.ToolId, selected.SourceToolId, StringComparison.OrdinalIgnoreCase));
            row.CurrentValue = source == null ? "--" : source.GetFormattedOutput(selected.SourcePortName);
            row.StatusText = source == null
                ? "上游工具不存在"
                : (row.CurrentValue == "--" ? "连接有效 · 等待上游运行" : "连接有效 · 有值 · 上游 " + source.ResultCode);
        }

        private string BuildInputDraftSummary()
        {
            if (inputPortRows.Count == 0) return "该模块没有输入端口。";
            return string.Join("；", inputPortRows.Select(row =>
            {
                VmInputSourceOption selected = row.SourceOptions.FirstOrDefault(item => string.Equals(item.Key, row.SelectedSourceKey, StringComparison.OrdinalIgnoreCase));
                return row.DisplayName + " ← " + (selected == null ? "无效来源" : selected.DisplayText);
            }));
        }

        private static string GetDefaultInputKey(string portName)
        {
            return "default:" + portName;
        }

        private static string BuildSourceKey(string toolId, string portName)
        {
            return (toolId ?? string.Empty) + "|" + (portName ?? string.Empty);
        }

        private static string GetDefaultInputText(VmPortDefinition port)
        {
            if (port.PortName == "Image") return "系统.Image（当前图像）";
            if (port.PortName == "ROI" || port.PortName == "SearchROI") return "本地 ROI 图层 / 全图兼容";
            if (port.PortName == "ShapeModel") return "当前模板资源";
            if (port.PortName == "Program") return "当前 HDevelop 程序";
            return port.IsOptional ? "未连接（可选）" : "未连接";
        }

        private void SelectAllRoiButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VmRoiBindingItem item in roiBindingRows) item.IsBound = true;
        }

        private void ClearRoiBindingsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VmRoiBindingItem item in roiBindingRows) item.IsBound = false;
        }

        private void OpenRoiWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ApplyDraft()) return;
            OpenRoiWorkspaceRequested = true;
            Close();
        }

        private void DrawRoiButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            VisionTool requested;
            if (button == null || !Enum.TryParse(Convert.ToString(button.Tag, CultureInfo.InvariantCulture), out requested) || !ApplyDraft()) return;
            RequestedRoiTool = requested;
            OpenRoiWorkspaceRequested = true;
            Close();
        }

        private string BuildRoiSummary()
        {
            List<string> names = roiBindingRows.Where(item => item.IsBound).Select(item => item.Name).ToList();
            string bindingText = names.Count == 0 ? "未绑定 ROI" : "已绑定 " + names.Count.ToString(CultureInfo.InvariantCulture) + " 个：" + string.Join("、", names);
            if (tool.Kind == VmToolKind.Blob || tool.Kind == VmToolKind.GrayStat || tool.Kind == VmToolKind.EdgeMeasure)
            {
                string mode = RoiExecutionModeComboBox == null ? (tool.Parameters ?? new VmToolParameterData()).RoiExecutionMode : RoiExecutionModeComboBox.SelectedValue as string;
                return bindingText + "；" + VmRoiExecutionMode.GetDisplayText(mode);
            }

            return bindingText;
        }

        private bool Fail(string text) { ValidationText.Text = text; return false; }
        private static bool Read(string text, out double value) { return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value); }
        private static string F(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
    }
}
