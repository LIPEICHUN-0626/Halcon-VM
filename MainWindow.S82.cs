using System.Windows;
using System.Windows.Media;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private static readonly Brush CentralConfigurationActionBackgroundS82 =
            new SolidColorBrush(Color.FromRgb(248, 250, 252));
        private static readonly Brush DockConfigurationHeaderBackgroundS82 =
            new SolidColorBrush(Color.FromRgb(38, 52, 73));
        private static readonly Brush CentralConfigurationFooterForegroundS82 =
            new SolidColorBrush(Color.FromRgb(71, 85, 105));
        private static readonly Brush DockConfigurationFooterForegroundS82 =
            new SolidColorBrush(Color.FromRgb(203, 213, 225));

        /// <summary>
        /// The same real configuration workspace is hosted either in the small
        /// right-side tab or in the central task workspace. In central mode its
        /// identity is already shown by the outer header, so keep only the run /
        /// apply action row and remove the duplicated dark title block.
        /// </summary>
        private void SetCentralConfigurationChromeS82(bool central)
        {
            if (DockIdentityHeaderRowS82 == null || DockTaskHeaderBorder == null) return;
            DockIdentityHeaderRowS82.Height = central ? new GridLength(0) : GridLength.Auto;
            DockTaskHeaderBorder.Background = central
                ? CentralConfigurationActionBackgroundS82
                : DockConfigurationHeaderBackgroundS82;
            DockTaskHeaderBorder.BorderBrush = central
                ? new SolidColorBrush(Color.FromRgb(226, 232, 240))
                : Brushes.Transparent;
            DockTaskHeaderBorder.BorderThickness = central
                ? new Thickness(0, 0, 0, 1)
                : new Thickness(0);
            DockTaskHeaderBorder.Padding = central
                ? new Thickness(8, 4, 8, 5)
                : new Thickness(7, 5, 7, 5);
            DockActionRowS82.Margin = central
                ? new Thickness(0)
                : new Thickness(0, 4, 0, 0);
            DockFooterText.Foreground = central
                ? CentralConfigurationFooterForegroundS82
                : DockConfigurationFooterForegroundS82;
        }
    }
}
