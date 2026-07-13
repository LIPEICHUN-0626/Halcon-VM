using System;
using System.Collections.Generic;
using System.IO;
using HalconDotNet;

namespace HalconWinFormsDemo.Services
{
    public sealed class StartupDiagnosticsService
    {
        public IList<DiagnosticItem> Run(string logDirectory, string recipeDirectory)
        {
            List<DiagnosticItem> items = new List<DiagnosticItem>();

            AddPathCheck(items, "HALCON .NET", @"C:\Program Files\MVTec\HALCON-20.11-Progress\bin\dotnet35\halcondotnet.dll");
            AddPathCheck(items, "HDevEngine .NET", @"C:\Program Files\MVTec\HALCON-20.11-Progress\bin\dotnet35\hdevenginedotnet.dll");
            AddPathCheck(items, "HALCON Native x64", @"C:\Program Files\MVTec\HALCON-20.11-Progress\bin\x64-win64\halcon.dll");
            AddDirectoryCheck(items, "日志目录", logDirectory);
            AddDirectoryCheck(items, "配方目录", recipeDirectory);

            try
            {
                HTuple version;
                HOperatorSet.GetSystem("version", out version);
                items.Add(DiagnosticItem.Ok("HALCON 运行时", version == null ? "已加载" : version.ToString()));
            }
            catch (Exception ex)
            {
                items.Add(DiagnosticItem.Fail("HALCON 运行时", ex.Message));
            }

            return items;
        }

        private static void AddPathCheck(ICollection<DiagnosticItem> items, string name, string path)
        {
            items.Add(File.Exists(path)
                ? DiagnosticItem.Ok(name, path)
                : DiagnosticItem.Fail(name, "未找到：" + path));
        }

        private static void AddDirectoryCheck(ICollection<DiagnosticItem> items, string name, string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                items.Add(DiagnosticItem.Ok(name, path));
            }
            catch (Exception ex)
            {
                items.Add(DiagnosticItem.Fail(name, ex.Message));
            }
        }
    }

    public sealed class DiagnosticItem
    {
        public string Name { get; set; }

        public string Status { get; set; }

        public string Detail { get; set; }

        public static DiagnosticItem Ok(string name, string detail)
        {
            return new DiagnosticItem { Name = name, Status = "OK", Detail = detail };
        }

        public static DiagnosticItem Fail(string name, string detail)
        {
            return new DiagnosticItem { Name = name, Status = "NG", Detail = detail };
        }
    }
}
