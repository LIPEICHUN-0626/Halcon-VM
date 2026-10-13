using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class InputRepairVisitS96
        {
            public VmToolInstance Consumer;
            public VmToolInstance Producer;
            public string InputPort;
        }

        // Navigation references only. Parameter drafts and runtime objects remain owned by the existing editor.
        private readonly List<InputRepairVisitS96> inputRepairVisitsS96 = new List<InputRepairVisitS96>();

        private bool CanNavigateInputRepairS96()
        {
            if (!isFlowExecutionActive && !isContinuousRunning && !isImageQueueBatchRunning &&
                !IsCurrentToolContinuousTrialRunningS87 && !historicalImageMode) return true;
            HeaderStatusText.Text = "请先停止运行或退出历史查看，再切换来源配置。";
            return false;
        }

        private void OpenInputProducerS96_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var row = element == null ? null : element.DataContext as VmInputPortEditorRow;
            if (row == null || row.SourceOptions == null || dockEditingTool == null || !CanNavigateInputRepairS96()) return;
            var option = row.SourceOptions.FirstOrDefault(item => item.Key == row.SelectedSourceKey);
            var source = option == null ? null : flowTools.FirstOrDefault(item => item.ToolId == option.SourceToolId);
            if (source == null || ReferenceEquals(source, dockEditingTool))
            {
                HeaderStatusText.Text = "此输入没有可打开的上游模块；请在来源下拉框选择实际流程输出。";
                return;
            }
            VmToolInstance consumer = dockEditingTool;
            OpenCentralModuleConfigurationS77(source, true);
            // A cancelled draft confirmation must not create a history entry.
            if (!centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, source)) return;
            if (inputRepairVisitsS96.Count >= 32) inputRepairVisitsS96.RemoveAt(0);
            inputRepairVisitsS96.Add(new InputRepairVisitS96 { Consumer = consumer, Producer = source, InputPort = row.PortName });
            RefreshInputRepairNavigationS96();
            HeaderStatusText.Text = "已打开来源“" + source.InstanceName + "”；处理后点击返回，可回到“" + consumer.InstanceName + "”的输入页。";
        }

        private void ReturnInputConsumerS96_Click(object sender, RoutedEventArgs e)
        {
            if (!CanNavigateInputRepairS96()) return;
            RefreshInputRepairNavigationS96();
            if (inputRepairVisitsS96.Count == 0) return;
            var visit = inputRepairVisitsS96[inputRepairVisitsS96.Count - 1];
            OpenCentralModuleConfigurationS77(visit.Consumer, true);
            if (!centralModuleConfigurationOpenS77 || !ReferenceEquals(dockEditingTool, visit.Consumer)) return;
            inputRepairVisitsS96.RemoveAt(inputRepairVisitsS96.Count - 1);
            SelectCentralConfigurationTaskS77(DockIoTaskTab, "Io");
            var row = DockInputPortItemsControl.Items.OfType<VmInputPortEditorRow>().FirstOrDefault(item => item.PortName == visit.InputPort);
            if (row != null)
            {
                DockInputPortItemsControl.SelectedItem = row;
                DockInputPortItemsControl.ScrollIntoView(row);
            }
            RefreshInputRepairNavigationS96();
            HeaderStatusText.Text = "已返回“" + visit.Consumer.InstanceName + "”输入页；来源修改不会自动运行下游。";
        }

        private void ClearInputRepairNavigationS96()
        {
            inputRepairVisitsS96.Clear();
            RefreshInputRepairNavigationS96();
        }

        private void RefreshInputRepairNavigationS96()
        {
            // Never navigate into instances replaced by a recipe load, delete or undo operation.
            if (!centralModuleConfigurationOpenS77 || inputRepairVisitsS96.Any(item =>
                !flowTools.Contains(item.Consumer) || !flowTools.Contains(item.Producer))) inputRepairVisitsS96.Clear();
            if (ReturnInputConsumerButtonS96 == null) return;
            var visit = inputRepairVisitsS96.LastOrDefault();
            bool available = visit != null && ReferenceEquals(dockEditingTool, visit.Producer);
            ReturnInputConsumerButtonS96.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
            ReturnInputConsumerButtonS96.ToolTip = available
                ? "返回“" + visit.Consumer.InstanceName + "”的 " + visit.InputPort + " 输入；未应用草稿会先询问。"
                : "返回来源导航前的模块";
        }
    }
}
