using System;
using System.Collections.Generic;
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
            LoadOcrValidationDraftS133(p);
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
            return ReadOcrValidationDraftS133(p);
        }

        private void OcrModelBrowse_ClickS81(object sender, RoutedEventArgs e)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("选择 OCR 字库");
            if (editLeaseS137 < 0) return;
            VmToolInstance editingToolS137 = dockEditingTool;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择 OCR 字符模型", Filter = "HALCON MLP 字库 (*.omc)|*.omc", CheckFileExists = true,
                InitialDirectory = Path.GetDirectoryName(GetDefaultOcrModelPathS81())
            };
            if (dialog.ShowDialog(this) == true && IsEngineeringEditLeaseCurrentS137(editLeaseS137) &&
                ReferenceEquals(dockEditingTool, editingToolS137) && CanChangeRecipeS134(false))
                ImportOcrModelS133(dialog.FileName);
        }

        private void RefreshOcrConnectionStatusS81(VmToolInstance tool)
        {
            bool ready = HasToolInputImage(tool, "Image", false);
            bool modelAvailable = tool.Parameters != null && File.Exists(tool.Parameters.OcrModelPath);
            tool.ConfigurationStatus = !modelAvailable ? "待选择字库" : ready ? "就绪" : "待连接图像";
            tool.ConnectionStatus = ready ? "Image 已连接 · 有值" : "Image 必须连接前序图像输出";
            tool.ConnectionSummary = GetImageInputSummary(tool) + (tool.GetInputBinding("Characters") == null ? " · ROI 内分割" : " · 前序字符 Region") + " → 文本 / 逐字结果 / 校验";
        }

        private InspectionRecord RunOcrToolS81(VmToolInstance tool, HImage input, HRegion characters, HRegion roi)
        {
            ClearOcrCharacterViewsS133(tool);
            VmToolParameterData p = tool.Parameters.Clone();
            string error = p.Validate(VmToolKind.OcrRecognition);
            if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
            if (!File.Exists(p.OcrModelPath)) throw new InvalidOperationException("OCR 字库不存在，请在中央配置中重新选择：" + p.OcrModelPath);
            VmOcrValidationParametersS133 validation = p.OcrValidationS133 ?? new VmOcrValidationParametersS133();
            var characterBinding = tool.GetInputBinding("Characters");
            if (characterBinding != null)
            {
                string sourceContext = ResolveRoiImageContextIdS86(GetInputSourceTool(characterBinding), characterBinding.SourcePortName);
                string inputContext = GetImageCoordinateContextIdS65(tool, "Image");
                if (string.IsNullOrWhiteSpace(sourceContext) || sourceContext == "无效来源" || IsPendingCoordinateContextS102(sourceContext) || sourceContext != inputContext)
                    throw new InvalidOperationException("前序字符 Region 与 OCR Image 必须属于同一图像像素坐标链，不按相同尺寸冒充相同来源。" );
            }
            bool legacy = string.IsNullOrWhiteSpace(validation.ModelPath);
            string modelPath = legacy ? p.OcrModelPath : validation.ModelPath;
            HImage gray = null;
            HObject domain = null, search = null, threshold = null, connected = null, filtered = null, sorted = null, contours = null, inverted = null;
            HTuple count = null, model = null, classes = null, confidence = null, areas = null, rows = null, columns = null;
            HTuple imageType = null, row1 = null, column1 = null, row2 = null, column2 = null;
            VmOcrResourceLeaseS133 resource = null;
            bool loaded = false;
            try
            {
                int channels = imageService.GetChannelCount(input);
                gray = channels == 1 ? input.CopyImage() : CreateGrayImage(input);
                HOperatorSet.GetImageType(gray, out imageType);
                if (imageType.Length != 1 || imageType.S != "byte")
                    throw new InvalidOperationException("当前 OCR 的分割/明暗参数采用 byte 0–255 灰度语义；非 byte 输入请在上游显式转换，不自动缩放或归一化。" );
                resource = VmOcrModelResourceS133.OpenResource(modelPath, legacy ? string.Empty : validation.ModelSha256);
                HOperatorSet.ReadOcrClassMlp(resource.ModelPath, out model);
                loaded = true; ocrModelsLoadedS81++;
                int actualClassCount = VmOcrModelResourceS133.GetClassCount(model);
                if (!legacy && validation.ClassCount != actualClassCount)
                    throw new InvalidOperationException("OCR 字库字符类别数与受管版本不一致，请显式重新导入。");
                string modelVersion = resource.Sha256;
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
                var characterViews = new List<VmOcrCharacterS133>();
                if (characterCount > 0)
                {
                    HObject recognitionImage = gray;
                    if (!p.OcrDarkOnLight) { HOperatorSet.InvertImage(gray, out inverted); recognitionImage = inverted; }
                    HOperatorSet.DoOcrMultiClassMlp(sorted, recognitionImage, model, out classes, out confidence);
                    texts = classes.SArr; scores = confidence.DArr;
                    if (texts.Length != characterCount || scores.Length != characterCount || texts.Any(value => value == null) ||
                        scores.Any(value => double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1))
                        throw new InvalidOperationException("HALCON OCR 返回的字符/置信度数量或数值无效。");
                    HOperatorSet.AreaCenter(sorted, out areas, out rows, out columns);
                    centerRows = rows.DArr; centerColumns = columns.DArr;
                    HOperatorSet.SmallestRectangle1(sorted, out row1, out column1, out row2, out column2);
                    if (centerRows.Length != characterCount || centerColumns.Length != characterCount || areas.Length != characterCount ||
                        row1.Length != characterCount || column1.Length != characterCount || row2.Length != characterCount || column2.Length != characterCount ||
                        areas.DArr.Any(value => double.IsNaN(value) || double.IsInfinity(value) || value <= 0))
                        throw new InvalidOperationException("HALCON OCR 字符区域与逐字坐标数量不一致或含空区域。");
                    for (int index = 0; index < characterCount; index++)
                        characterViews.Add(new VmOcrCharacterS133(index + 1, texts[index], scores[index], p.OcrMinimumConfidence,
                            centerRows[index], centerColumns[index], row1[index].D, column1[index].D, row2[index].D, column2[index].D));
                    HOperatorSet.GenContourRegionXld(sorted, out contours, "border");
                }
                else HOperatorSet.GenEmptyObj(out contours);

                double minimum = scores.Length == 0 ? 0 : scores.Min();
                double mean = scores.Length == 0 ? 0 : scores.Average();
                int low = scores.Count(value => value < p.OcrMinimumConfidence);
                bool rejected = texts.Any(value => string.IsNullOrWhiteSpace(value) || value == "\u001a");
                string text = string.Concat(texts);
                VmOcrTextValidationResultS133 rule = VmOcrTextValidationS133.Evaluate(text, validation);
                string result = characterCount > 0 && characterCount >= p.OcrMinimumCharacters && low == 0 && !rejected && rule.Match ? "OK" : "NG";
                var failureReasons = new List<string>();
                if (characterCount == 0) failureReasons.Add("未识别到字符。");
                if (characterCount < p.OcrMinimumCharacters) failureReasons.Add("字符数少于最少字符数。");
                if (low > 0) failureReasons.Add("存在低置信度字符。");
                if (rejected) failureReasons.Add("存在拒识字符。");
                if (!rule.Match) failureReasons.Add(rule.FailureReason);
                string failureReason = string.Join(" ", failureReasons);
                string parameters = string.Format(CultureInfo.InvariantCulture,
                    "HALCON=MLP, Model={0}, Source={1}, Order={2}, MinimumCount={3}, MinimumConfidence={4:0.######}, Threshold={5}, DarkOnLight={6}, Area={7:0.###}..{8:0.###}, Channels={9}->1, ROI={10}, TextRule={11}, IgnoreCase={12}, ModelSHA={13}, ModelClasses={14}, Legacy={15}",
                    modelPath, characters == null ? "Threshold" : "Characters", p.OcrSortMode, p.OcrMinimumCharacters, p.OcrMinimumConfidence,
                    p.OcrThreshold, p.OcrDarkOnLight, p.OcrMinimumArea, p.OcrMaximumArea, channels, roi != null, validation.Mode,
                    validation.IgnoreCase, modelVersion, actualClassCount, legacy);
                string diagnostics = string.Format(CultureInfo.InvariantCulture,
                    "Characters={0}, LowConfidence={1}, Rejected={2}, Minimum={3:0.######}, Mean={4:0.######}, TextRuleMatch={5}; {6} {7}置信度不等于正确率，需匹配字库与现场样本。",
                    characterCount, low, rejected, minimum, mean, rule.Match, failureReason, legacy ? "旧字库兼容：本次实际 SHA 已发布，尚未绑定受管版本。" : string.Empty);
                tool.SetOutputValue("Text", text);
                tool.SetOutputValue("FirstCharacter", texts.FirstOrDefault() ?? string.Empty);
                tool.SetOutputValue("Characters", string.Join(Environment.NewLine, texts));
                tool.SetOutputValue("CharacterTexts", new VmStringArrayS105(texts));
                tool.SetOutputValue("CharacterConfidences", new VmNumberArrayS105(scores));
                tool.SetOutputValue("CharactersRegion", VmRegionSnapshot.Create(sorted, tool.ToolId, tool.InstanceName, "CharactersRegion"));
                tool.SetOutputValue("CharacterCount", (double)characterCount);
                tool.SetOutputValue("MeanConfidence", mean);
                tool.SetOutputValue("MinimumConfidence", minimum);
                tool.SetOutputValue("LowConfidenceCount", (double)low);
                string context = GetImageCoordinateContextIdS65(tool, "Image");
                tool.SetOutputCoordinateContext("CharactersRegion", context);
                tool.SetOutputCoordinateContext("XLD", context);
                tool.SetOutputCoordinateContext("Centers", context);
                tool.SetOutputValue("Centers", VmPoint2DArray.CreateImagePixel(centerRows, centerColumns, context));
                tool.SetOutputValue("XLD", VmXldSnapshot.Create(contours, tool.ToolId, tool.InstanceName, "XLD"));
                tool.SetOutputValue("Parameters", parameters);
                tool.SetOutputValue("Diagnostics", diagnostics);
                tool.SetOutputValue("TextRuleMatch", rule.Match);
                tool.SetOutputValue("ValidationMode", validation.Mode);
                tool.SetOutputValue("ValidationFailureReason", failureReason);
                tool.SetOutputValue("ModelVersion", modelVersion);
                tool.SetOutputValue("ResultCode", result);
                ReplaceToolOverlayContours(contours, tool, "XLD", characterCount, result, characterCount);
                contours = null;
                PublishOcrCharacterViewsS133(tool, characterViews, modelVersion, context);
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
                    foreach (HTuple tuple in new[] { count, model, classes, confidence, areas, rows, columns, imageType, row1, column1, row2, column2 }) if (tuple != null) tuple.Dispose();
                    foreach (HObject obj in new[] { domain, search, threshold, connected, filtered, sorted, contours, inverted }) if (obj != null) obj.Dispose();
                    if (gray != null) gray.Dispose();
                    if (resource != null) resource.Dispose();
                }
            }
        }
    }
}
