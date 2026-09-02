using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private int ocrModelsLoadedS81;
        private int ocrModelsClearedS81;

        private static string GetDefaultOcrModelPathS81()
        {
            string root = Environment.GetEnvironmentVariable("HALCONROOT");
            if (string.IsNullOrWhiteSpace(root)) root = @"C:\Program Files\MVTec\HALCON-20.11-Progress";
            return Path.GetFullPath(Path.Combine(root, "ocr", "Industrial_NoRej.omc"));
        }

        private void LoadOcrParametersS81(VmToolParameterData p)
        {
            DockOcrModelTextBox.Text = p.OcrModelPath;
            DockOcrMinimumCharactersTextBox.Text = p.OcrMinimumCharacters.ToString(CultureInfo.InvariantCulture);
            DockOcrConfidenceTextBox.Text = p.OcrMinimumConfidence.ToString("0.######", CultureInfo.InvariantCulture);
            DockOcrThresholdTextBox.Text = p.OcrThreshold.ToString(CultureInfo.InvariantCulture);
            DockOcrSortComboBox.SelectedValue = p.OcrSortMode;
            DockOcrDarkCheckBox.IsChecked = p.OcrDarkOnLight;
            DockOcrMinimumAreaTextBox.Text = p.OcrMinimumArea.ToString("0.###", CultureInfo.InvariantCulture);
            DockOcrMaximumAreaTextBox.Text = p.OcrMaximumArea.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private bool ReadOcrParametersS81(VmToolParameterData p)
        {
            int integer; double number;
            p.OcrModelPath = (DockOcrModelTextBox.Text ?? string.Empty).Trim();
            if (!int.TryParse(DockOcrMinimumCharactersTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("最少字符数必须为整数。");
            p.OcrMinimumCharacters = integer;
            if (!TryParseUiDouble(DockOcrConfidenceTextBox.Text, out number)) return FailDockDraft("OCR 最低置信度必须为数值。");
            p.OcrMinimumConfidence = number;
            if (!int.TryParse(DockOcrThresholdTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)) return FailDockDraft("OCR 分割灰度必须为整数。");
            p.OcrThreshold = integer;
            if (!TryParseUiDouble(DockOcrMinimumAreaTextBox.Text, out number)) return FailDockDraft("OCR 最小面积必须为数值。");
            p.OcrMinimumArea = number;
            if (!TryParseUiDouble(DockOcrMaximumAreaTextBox.Text, out number)) return FailDockDraft("OCR 最大面积必须为数值。");
            p.OcrMaximumArea = number;
            p.OcrSortMode = DockOcrSortComboBox.SelectedValue as string;
            p.OcrDarkOnLight = DockOcrDarkCheckBox.IsChecked == true;
            return true;
        }

        private void OcrModelBrowse_ClickS81(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择 OCR 字符模型", Filter = "HALCON MLP 字库 (*.omc)|*.omc", CheckFileExists = true,
                InitialDirectory = Path.GetDirectoryName(GetDefaultOcrModelPathS81())
            };
            if (dialog.ShowDialog(this) == true) DockOcrModelTextBox.Text = Path.GetFullPath(dialog.FileName);
        }

        private void RefreshOcrConnectionStatusS81(VmToolInstance tool)
        {
            bool ready = HasToolInputImage(tool, "Image", false);
            bool modelAvailable = tool.Parameters != null && File.Exists(tool.Parameters.OcrModelPath);
            tool.ConfigurationStatus = !modelAvailable ? "待选择字库" : ready ? "就绪" : "待连接图像";
            tool.ConnectionStatus = ready ? "Image 已连接 · 有值" : "Image 必须连接前序图像输出";
            tool.ConnectionSummary = GetImageInputSummary(tool) + (tool.GetInputBinding("Characters") == null ? " · ROI 内分割" : " · 前序字符 Region") + " → Text / Confidence / XLD";
        }

        private InspectionRecord RunOcrToolS81(VmToolInstance tool, HImage input, HRegion characters, HRegion roi)
        {
            VmToolParameterData p = tool.Parameters.Clone();
            string error = p.Validate(VmToolKind.OcrRecognition);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            if (!File.Exists(p.OcrModelPath)) throw new InvalidOperationException("OCR 字库不存在，请在中央配置中重新选择：" + p.OcrModelPath);
            HImage gray = null;
            HObject domain = null, search = null, threshold = null, connected = null, filtered = null, sorted = null, contours = null, inverted = null;
            HTuple count = null, model = null, classes = null, confidence = null, areas = null, rows = null, columns = null;
            bool loaded = false;
            try
            {
                int channels = imageService.GetChannelCount(input);
                gray = channels == 1 ? input.CopyImage() : CreateGrayImage(input);
                HOperatorSet.GetDomain(gray, out domain);
                if (roi != null)
                {
                    HObject intersected;
                    HOperatorSet.Intersection(domain, roi, out intersected);
                    domain.Dispose(); domain = intersected;
                }
                if (characters == null)
                {
                    HOperatorSet.ReduceDomain(gray, domain, out search);
                    HOperatorSet.Threshold(search, out threshold, p.OcrDarkOnLight ? 0 : p.OcrThreshold, p.OcrDarkOnLight ? p.OcrThreshold : 255);
                    HOperatorSet.Connection(threshold, out connected);
                }
                else HOperatorSet.Intersection(characters, domain, out connected);
                HOperatorSet.SelectShape(connected, out filtered, "area", "and", p.OcrMinimumArea, p.OcrMaximumArea);
                if (p.OcrSortMode == "Source") HOperatorSet.CopyObj(filtered, out sorted, 1, -1);
                else HOperatorSet.SortRegion(filtered, out sorted, "character", "true", p.OcrSortMode == "Column" ? "column" : "row");
                HOperatorSet.CountObj(sorted, out count);
                int characterCount = count.I;
                if (characterCount > 10000) throw new InvalidOperationException("字符候选超过 10000，请缩小 ROI 或调整分割/面积范围。");
                string[] texts = new string[0]; double[] scores = new double[0];
                double[] centerRows = new double[0], centerColumns = new double[0];
                if (characterCount > 0)
                {
                    HOperatorSet.ReadOcrClassMlp(p.OcrModelPath, out model);
                    loaded = true; ocrModelsLoadedS81++;
                    HObject recognitionImage = gray;
                    if (!p.OcrDarkOnLight) { HOperatorSet.InvertImage(gray, out inverted); recognitionImage = inverted; }
                    HOperatorSet.DoOcrMultiClassMlp(sorted, recognitionImage, model, out classes, out confidence);
                    texts = classes.SArr; scores = confidence.DArr;
                    if (texts.Length != characterCount || scores.Length != characterCount || scores.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
                        throw new InvalidOperationException("HALCON OCR 返回的字符/置信度数量或数值无效。");
                    HOperatorSet.AreaCenter(sorted, out areas, out rows, out columns);
                    centerRows = rows.DArr; centerColumns = columns.DArr;
                    HOperatorSet.GenContourRegionXld(sorted, out contours, "border");
                }
                else HOperatorSet.GenEmptyObj(out contours);

                double minimum = scores.Length == 0 ? 0 : scores.Min();
                double mean = scores.Length == 0 ? 0 : scores.Average();
                int low = scores.Count(value => value < p.OcrMinimumConfidence);
                bool rejected = texts.Any(value => string.IsNullOrWhiteSpace(value) || value == "\u001a");
                string result = characterCount >= p.OcrMinimumCharacters && low == 0 && !rejected ? "OK" : "NG";
                string text = string.Concat(texts);
                string parameters = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=MLP, Model={0}, Source={1}, Order={2}, MinimumCount={3}, MinimumConfidence={4:0.######}, Threshold={5}, DarkOnLight={6}, Area={7:0.###}..{8:0.###}, Channels={9}->1, ROI={10}",
                    p.OcrModelPath, characters == null ? "Threshold" : "Characters", p.OcrSortMode, p.OcrMinimumCharacters, p.OcrMinimumConfidence,
                    p.OcrThreshold, p.OcrDarkOnLight, p.OcrMinimumArea, p.OcrMaximumArea, channels, roi != null);
                string diagnostics = string.Format(CultureInfo.InvariantCulture, "Characters={0}, LowConfidence={1}, Rejected={2}, Minimum={3:0.######}, Mean={4:0.######}; 置信度不等于正确率，需匹配字库与现场样本。", characterCount, low, rejected, minimum, mean);
                tool.SetOutputValue("Text", text);
                tool.SetOutputValue("FirstCharacter", texts.FirstOrDefault() ?? string.Empty);
                tool.SetOutputValue("Characters", string.Join(Environment.NewLine, texts));
                tool.SetOutputValue("CharacterCount", (double)characterCount);
                tool.SetOutputValue("MeanConfidence", mean);
                tool.SetOutputValue("MinimumConfidence", minimum);
                tool.SetOutputValue("LowConfidenceCount", (double)low);
                tool.SetOutputValue("Centers", VmPoint2DArray.CreateImagePixel(centerRows, centerColumns, GetImageCoordinateContextIdS65(tool, "Image")));
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(contours, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("Parameters", parameters);
                tool.SetOutputValue("Diagnostics", diagnostics);
                tool.SetOutputValue("ResultCode", result);
                ReplaceToolOverlayContours(contours, tool, "XLD", characterCount, result, characterCount);
                string message = "OCR " + result + "：" + characterCount.ToString(CultureInfo.InvariantCulture) + " 字符，文本=" + text + "；" + diagnostics;
                InspectionRecord record = CreateRecord("OcrRecognition", result, characterCount, message, tool);
                resultStore.Add(record); LogInfo(tool.InstanceName + "：" + message);
                return record;
            }
            finally
            {
                try
                {
                    if (loaded) { HOperatorSet.ClearOcrClassMlp(model); ocrModelsClearedS81++; }
                }
                finally
                {
                    foreach (HTuple tuple in new[] { count, model, classes, confidence, areas, rows, columns }) if (tuple != null) tuple.Dispose();
                    foreach (HObject obj in new[] { domain, search, threshold, connected, filtered, sorted, contours, inverted }) if (obj != null) obj.Dispose();
                    if (gray != null) gray.Dispose();
                }
            }
        }
    }
}
