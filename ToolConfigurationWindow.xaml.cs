using System;
using System.Globalization;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class ToolConfigurationWindow : Window
    {
        private readonly VmToolInstance tool;
        private readonly Action<VmToolInstance> trialRun;
        private readonly Func<string, string> validateInstanceName;

        public ToolConfigurationWindow(
            VmToolInstance tool,
            Action<VmToolInstance> trialRun,
            Func<string, string> validateInstanceName)
        {
            if (tool == null) throw new ArgumentNullException("tool");
            this.tool = tool;
            this.trialRun = trialRun;
            this.validateInstanceName = validateInstanceName;
            InitializeComponent();
            LoadDraft();
        }

        public bool WasApplied { get; private set; }

        private void LoadDraft()
        {
            VmToolParameterData p = tool.Parameters ?? new VmToolParameterData();
            ModuleTitleText.Text = tool.InstanceName;
            ModuleTypeText.Text = tool.Category + " / " + tool.DisplayType;
            ModuleStateText.Text = tool.ConfigurationStatus;
            InstanceNameTextBox.Text = tool.InstanceName;
            EnabledCheckBox.IsChecked = tool.IsEnabled;
            BlobMinTextBox.Text = F(p.BlobMinGray); BlobMaxTextBox.Text = F(p.BlobMaxGray); BlobAreaTextBox.Text = F(p.BlobMinArea);
            GrayMinTextBox.Text = F(p.GrayMin); GrayMaxTextBox.Text = F(p.GrayMax); EdgeThresholdTextBox.Text = F(p.EdgeThreshold);
            NumericOperatorComboBox.ItemsSource = NumericJudgeOperatorOption.CreateAll(); NumericOperatorComboBox.SelectedValue = tool.NumericOperator;
            NumericLowerTextBox.Text = F(tool.NumericLowerLimit); NumericUpperTextBox.Text = F(tool.NumericUpperLimit); NumericToleranceTextBox.Text = F(tool.NumericTolerance);
            BlobPanel.Visibility = tool.Kind == VmToolKind.Blob ? Visibility.Visible : Visibility.Collapsed;
            GrayPanel.Visibility = tool.Kind == VmToolKind.GrayStat ? Visibility.Visible : Visibility.Collapsed;
            EdgePanel.Visibility = tool.Kind == VmToolKind.EdgeMeasure ? Visibility.Visible : Visibility.Collapsed;
            NumericPanel.Visibility = tool.Kind == VmToolKind.NumericJudge ? Visibility.Visible : Visibility.Collapsed;
            CompatibilityPanel.Visibility = tool.Kind == VmToolKind.ShapeMatch || tool.Kind == VmToolKind.HDevelop ? Visibility.Visible : Visibility.Collapsed;
            InputText.Text = tool.ConnectionSummary + " · " + tool.ConnectionStatus;
            RoiText.Text = tool.BoundRoiIds.Count == 0 ? "未绑定 ROI" : string.Join(", ", tool.BoundRoiIds);
            ResultText.Text = tool.ResultCode + " · " + tool.OutputSummary + " · " + tool.ElapsedText;
            ErrorText.Text = string.IsNullOrWhiteSpace(tool.ErrorMessage) ? "无错误" : tool.ErrorMessage;
            HelpText.Text = ToolMetadata.GetDescription(tool.Kind);
            FooterText.Text = "实例参数草稿";
        }

        private bool TryReadDraft(out VmToolParameterData parameters, out double lower, out double upper, out double tolerance, out string comparisonOperator)
        {
            parameters = (tool.Parameters ?? new VmToolParameterData()).Clone(); lower = tool.NumericLowerLimit; upper = tool.NumericUpperLimit; tolerance = tool.NumericTolerance; comparisonOperator = tool.NumericOperator;
            double value;
            if (tool.Kind == VmToolKind.Blob)
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
            if (tool.Kind == VmToolKind.NumericJudge) { tool.NumericOperator = comparisonOperator; tool.NumericLowerLimit = lower; tool.NumericUpperLimit = upper; tool.NumericTolerance = tolerance; }
            WasApplied = true; ModuleTitleText.Text = tool.InstanceName; FooterText.Text = "已应用到当前实例"; return true;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) { ApplyDraft(); }
        private void OkButton_Click(object sender, RoutedEventArgs e) { if (ApplyDraft()) DialogResult = true; }
        private void TrialRunButton_Click(object sender, RoutedEventArgs e)
        {
            VmToolParameterData draft; double lower; double upper; double tolerance; string comparisonOperator; if (!TryReadDraft(out draft, out lower, out upper, out tolerance, out comparisonOperator)) return;
            VmToolParameterData old = tool.Parameters; double oldLower = tool.NumericLowerLimit; double oldUpper = tool.NumericUpperLimit; double oldTolerance = tool.NumericTolerance; string oldOperator = tool.NumericOperator;
            try { tool.Parameters = draft; tool.NumericLowerLimit = lower; tool.NumericUpperLimit = upper; tool.NumericTolerance = tolerance; tool.NumericOperator = comparisonOperator; if (trialRun != null) trialRun(tool); FooterText.Text = "试运行完成，草稿尚未应用"; }
            finally { tool.Parameters = old; tool.NumericLowerLimit = oldLower; tool.NumericUpperLimit = oldUpper; tool.NumericTolerance = oldTolerance; tool.NumericOperator = oldOperator; }
        }

        private bool Fail(string text) { ValidationText.Text = text; return false; }
        private static bool Read(string text, out double value) { return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value); }
        private static string F(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
    }
}
