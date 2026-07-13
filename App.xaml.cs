using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace HalconWinFormsDemo
{
    public partial class App : Application
    {
        private const string HalconRoot = @"C:\Program Files\MVTec\HALCON-20.11-Progress";
        private static readonly string HalconNativeBin = Path.Combine(HalconRoot, @"bin\x64-win64");

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureHalconRuntimePath();
            base.OnStartup(e);
        }

        private static void ConfigureHalconRuntimePath()
        {
            if (!Directory.Exists(HalconNativeBin))
            {
                return;
            }

            SetDllDirectory(HalconNativeBin);

            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (currentPath.IndexOf(HalconNativeBin, StringComparison.OrdinalIgnoreCase) < 0)
            {
                Environment.SetEnvironmentVariable("PATH", HalconNativeBin + ";" + currentPath);
            }
        }
    }
}
