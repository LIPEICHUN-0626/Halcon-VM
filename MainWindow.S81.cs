using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private GridLength[] centralConfigurationColumnsS81;
        private double[] centralConfigurationColumnMinimumsS81;
        private double centralConfigurationRightWidthS81;
        private Visibility centralConfigurationDebugVisibilityS81;
        private double centralConfigurationZoomS81;
        private bool centralConfigurationImageRefreshPendingS81;

        private void EnterCentralConfigurationLayoutS81()
        {
            if (centralConfigurationColumnsS81 != null) return;
            centralConfigurationColumnsS81 = MainWorkspaceGrid.ColumnDefinitions.Select(column => column.Width).ToArray();
            centralConfigurationColumnMinimumsS81 = MainWorkspaceGrid.ColumnDefinitions.Select(column => column.MinWidth).ToArray();
            centralConfigurationRightWidthS81 = RightPanelColumn.ActualWidth > 0 ? RightPanelColumn.ActualWidth : RightPanelColumn.Width.Value;
            centralConfigurationDebugVisibilityS81 = FlowDebugStateText.Visibility;
            centralConfigurationZoomS81 = FlowZoomSlider.Value;
            // While editing, the existing flow occupies the toolbox slot. No second
            // flow or parameter state is created; closing restores the user's layout.
            ToolCatalogPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(FlowWorkspacePanel, 0);
            ToolCatalogColumn.MinWidth = 310;
            ToolCatalogColumn.Width = new GridLength(320);
            MainWorkspaceGrid.ColumnDefinitions[2].MinWidth = 360;
            MainWorkspaceGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            RightPanelColumn.MinWidth = 390;
            FlowExecutionHintS81.Visibility = Visibility.Collapsed;
            FlowDebugStateText.Visibility = Visibility.Collapsed;
            if (!isManualFlowLayout) FlowZoomSlider.Value = Math.Min(centralConfigurationZoomS81, 0.72);
            ResizeCentralConfigurationLayoutS81();
        }

        private void MainWorkspaceGrid_SizeChangedS81(object sender, SizeChangedEventArgs e)
        {
            ResizeCentralConfigurationLayoutS81();
        }

        private void ResizeCentralConfigurationLayoutS81()
        {
            if (centralConfigurationColumnsS81 == null) return;
            FrameworkElement root = Content as FrameworkElement;
            double width = root == null ? MainWorkspaceGrid.ActualWidth : Math.Min(MainWorkspaceGrid.ActualWidth, root.ActualWidth - MainWorkspaceGrid.Margin.Left - MainWorkspaceGrid.Margin.Right);
            // 1120px minimum window: retain >=360px parameters and >=390px image.
            // At wider sizes the image grows, rather than leaving a large empty form.
            double imageWidth = Math.Max(390, Math.Min(620, Math.Min(width * 0.38, width - 690)));
            if (Math.Abs(RightPanelColumn.Width.Value - imageWidth) > 0.5 || !RightPanelColumn.Width.IsAbsolute)
                RightPanelColumn.Width = new GridLength(imageWidth);
            RefreshCentralConfigurationImageS81();
        }

        private void RefreshCentralConfigurationImageS81()
        {
            if (centralConfigurationImageRefreshPendingS81) return;
            centralConfigurationImageRefreshPendingS81 = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                centralConfigurationImageRefreshPendingS81 = false;
                UpdateImageContextViewport(true);
                ScheduleRefreshDisplay();
            }));
        }

        private void RefreshImageZoomStatusS81()
        {
            if (ZoomStatusText == null) return;
            // Apply learns the native viewport size at render time; update afterward.
            ZoomStatusText.Text = dualImageMode
                ? "缩放：右 " + viewport.ZoomText.Replace("Zoom: ", string.Empty) +
                  " / 左 " + comparisonViewport.ZoomText.Replace("Zoom: ", string.Empty) +
                  (AreDualImageViewsLinked() ? " · 联动" : " · 独立")
                : viewport.ZoomText.Replace("Zoom", "缩放");
        }

        private void RestoreCentralConfigurationLayoutS81()
        {
            if (centralConfigurationColumnsS81 == null) return;
            for (int index = 0; index < centralConfigurationColumnsS81.Length; index++)
            {
                MainWorkspaceGrid.ColumnDefinitions[index].MinWidth = centralConfigurationColumnMinimumsS81[index];
                MainWorkspaceGrid.ColumnDefinitions[index].Width = centralConfigurationColumnsS81[index];
            }
            centralConfigurationColumnsS81 = null;
            centralConfigurationColumnMinimumsS81 = null;
            Grid.SetColumn(FlowWorkspacePanel, 2);
            ToolCatalogPanel.Visibility = Visibility.Visible;
            FlowExecutionHintS81.Visibility = Visibility.Visible;
            FlowDebugStateText.Visibility = centralConfigurationDebugVisibilityS81;
            FlowZoomSlider.Value = centralConfigurationZoomS81;
            RefreshCentralConfigurationImageS81();
        }
    }
}
