using System.IO;
using System.Linq;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadAlignmentExampleS132_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("pose-image-alignment");
            if (lastExampleProgramKeyS78 != "pose-image-alignment" ||
                !flowTools.Any(t => t.Kind == VmToolKind.PoseImageAlignment)) return;
            bool imageAvailable = flowTools.Where(t => t.Kind == VmToolKind.ImageSource)
                .Any(t => File.Exists(t.Parameters.LocalImagePath));
            HeaderStatusText.Text = imageAvailable
                ? "对齐示例已装载：第 02 步读取/框选制作 NCC→应用/F5；第 03 步采集当前参考→应用/F5。以后换图运行，观察真正对齐图；未自动训练、保存参考或运行。"
                : "对齐示例已装载但缺少 fabrik.png：先在第 01 步自选单通道图像，再制作 NCC/应用/F5，最后在第 03 步采集并应用参考。不会自动训练或运行。";
        }

        private bool BuildAlignmentExampleS132()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 单通道产品图像", true, null);
            var matching = CreateFlowTool(VmToolKind.NccTemplateMatch, "02 制作 NCC 与定位产品", true, null);
            var alignment = CreateFlowTool(VmToolKind.PoseImageAlignment, "03 保存参考并对齐图像", true, null);
            var gray = CreateFlowTool(VmToolKind.GrayStat, "04 查看对齐图灰度", true, null);
            foreach (var tool in new[] { source, matching, alignment, gray }) flowTools.Add(tool);
            matching.SetInputBinding("Image", source.ToolId, "Image");
            alignment.SetInputBinding("Image", source.ToolId, "Image");
            alignment.SetInputBinding("Pose", matching.ToolId, "Pose");
            gray.SetInputBinding("Image", alignment.ToolId, "Image");
            gray.SetInputBinding("ROI", alignment.ToolId, "Region");
            gray.Parameters.RoiSourceMode = VmRoiSourceMode.Inherited;
            string path = GetHalconExampleImageS78("fabrik.png");
            source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "EXAMPLE-POSE-ALIGNMENT";
            return File.Exists(path);
        }
    }
}
