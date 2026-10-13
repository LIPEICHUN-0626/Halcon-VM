using System.IO;
using System.Linq;
using System.Windows;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadOcrValidationExampleS133_Click(object sender, RoutedEventArgs e)
        {
            LoadExampleProgramS78("ocr-character-validation");
            if (lastExampleProgramKeyS78 != "ocr-character-validation" ||
                !flowTools.Any(t => t.Kind == VmToolKind.OcrRecognition)) return;
            bool imageAvailable = flowTools.Where(t => t.Kind == VmToolKind.ImageSource)
                .Any(t => File.Exists(t.Parameters.LocalImagePath));
            HeaderStatusText.Text = imageAvailable
                ? "OCR 示例已装载：先运行图像源，双击第 02 步固定当前字库/应用；可在 ROI 只圈一行再 F5。规则默认关闭，先看真实逐字结果，再自己填期望文本或格式；未自动运行。"
                : "OCR 示例已装载但缺少 letters.png：第 01 步自选黑字白底图片，再配置字库和字符 ROI。规则默认关闭，不预置期望文本，不会自动执行或外发。";
        }

        private bool BuildOcrValidationExampleS133()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 黑字白底字符图像", true, null);
            var ocr = CreateFlowTool(VmToolKind.OcrRecognition, "02 逐字识别与文本校验", true, null);
            var confidence = CreateFlowTool(VmToolKind.NumberArrayStatistics, "03 真实字符置信度统计", true, null);
            var firstText = CreateFlowTool(VmToolKind.TextArraySelect, "04 读取首个字符标签", true, null);
            foreach (var tool in new[] { source, ocr, confidence, firstText }) flowTools.Add(tool);
            ocr.SetInputBinding("Image", source.ToolId, "Image");
            confidence.SetInputBinding("Array", ocr.ToolId, "CharacterConfidences");
            firstText.SetInputBinding("Array", ocr.ToolId, "CharacterTexts");
            ocr.Parameters.OcrThreshold = 128;
            ocr.Parameters.OcrDarkOnLight = true;
            ocr.Parameters.OcrSortMode = "Row";
            ocr.Parameters.OcrValidationS133 = new VmOcrValidationParametersS133 { Mode = "None" };
            string path = GetHalconExampleImageS78("letters.png");
            source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "EXAMPLE-OCR-CHARACTERS";
            return File.Exists(path);
        }
    }
}
