using System.IO;
using System.Linq;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadNccExampleS131_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("ncc-template");
            if (lastExampleProgramKeyS78 != "ncc-template" ||
                !flowTools.Any(t => t.Kind == VmToolKind.NccTemplateMatch)) return;
            bool imageAvailable = flowTools.Where(t => t.Kind == VmToolKind.ImageSource)
                .Any(t => File.Exists(t.Parameters.LocalImagePath));
            HeaderStatusText.Text = imageAvailable
                ? "NCC 示例已装载：双击第 02 步→读取输入（可直接读取已连接图像源）→框选有纹理的目标→制作模板→F5。模板未预训练；第 03 步分数阈值仅供学习。"
                : "NCC 示例已装载，但未找到 fabrik.png：请先在第 01 步自选单通道图片并运行，再到第 02 步读取输入、框选制作模板。不会自动训练或运行。";
        }

        private bool BuildNccExampleS131()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 单通道目标图像", true, null);
            var matching = CreateFlowTool(VmToolKind.NccTemplateMatch, "02 制作 NCC 模板并定位", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "03 最佳匹配分数判定", true, null);
            foreach (var tool in new[] { source, matching, judge }) flowTools.Add(tool);
            matching.SetInputBinding("Image", source.ToolId, "Image");
            judge.SetInputBinding("Value", matching.ToolId, "Score");
            judge.InputToolId = matching.ToolId;
            judge.InputPortName = "Score";
            judge.NumericLowerLimit = 0.7;
            judge.NumericUpperLimit = 1;
            string path = GetHalconExampleImageS78("fabrik.png");
            source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "EXAMPLE-NCC";
            return File.Exists(path);
        }
    }
}
