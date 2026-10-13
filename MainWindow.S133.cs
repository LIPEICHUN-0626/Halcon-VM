using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmOcrValidationParametersS133 ocrValidationDraftS133 = new VmOcrValidationParametersS133();
        private bool ocrCharacterGridUpdatingS133;
        private VmToolInstance ocrCharactersToolS133;
        private OcrCharacterStateS133 ocrVisibleCharacterStateS133;
        private bool ocrOnlyAbnormalAppliedS133;

        private sealed class OcrCharacterStateS133
        {
            public VmToolInstance Tool;
            public IList<VmOcrCharacterS133> Characters;
            public string ModelVersion, Context, Text;
        }
        private readonly Dictionary<string, OcrCharacterStateS133> ocrCharacterStatesS133 = new Dictionary<string, OcrCharacterStateS133>(StringComparer.OrdinalIgnoreCase);

        private void LoadOcrValidationDraftS133(VmToolParameterData p)
        {
            ocrValidationDraftS133 = (p.OcrValidationS133 ?? new VmOcrValidationParametersS133()).Clone();
            DockOcrValidationModeS133.SelectedValue = ocrValidationDraftS133.Mode;
            DockOcrExpectedS133.Text = ocrValidationDraftS133.ExpectedText ?? "";
            DockOcrPatternS133.Text = ocrValidationDraftS133.Pattern ?? "";
            DockOcrIgnoreCaseS133.IsChecked = ocrValidationDraftS133.IgnoreCase;
            UpdateOcrValidationFieldsS133(); UpdateOcrModelVersionS133();
        }

        private bool ReadOcrValidationDraftS133(VmToolParameterData p)
        {
            var draft = ocrValidationDraftS133.Clone();
            draft.Mode = DockOcrValidationModeS133.SelectedValue as string;
            draft.ExpectedText = DockOcrExpectedS133.Text ?? ""; draft.Pattern = DockOcrPatternS133.Text ?? "";
            draft.IgnoreCase = DockOcrIgnoreCaseS133.IsChecked == true;
            string error = draft.Validate(); if (!string.IsNullOrWhiteSpace(error)) return FailDockDraft(error);
            try
            {
                if (!string.IsNullOrWhiteSpace(draft.ModelSha256) && !string.Equals(Path.GetFullPath(p.OcrModelPath), Path.GetFullPath(draft.ModelPath), StringComparison.OrdinalIgnoreCase))
                    return FailDockDraft("OCR 字库路径与受管版本身份不同，请重新导入字库。" );
            }
            catch (ArgumentException) { return FailDockDraft("OCR 字库路径无效，请重新导入。" ); }
            catch (NotSupportedException) { return FailDockDraft("OCR 字库路径格式不受支持，请重新导入。" ); }
            p.OcrValidationS133 = draft; return true;
        }

        private void UpdateOcrValidationFieldsS133()
        {
            if (DockOcrExactGroupS133 == null) return;
            string mode = DockOcrValidationModeS133.SelectedValue as string ?? "None";
            DockOcrExactGroupS133.Visibility = mode == "Exact" ? Visibility.Visible : Visibility.Collapsed;
            DockOcrPatternGroupS133.Visibility = mode == "Pattern" ? Visibility.Visible : Visibility.Collapsed;
            DockOcrIgnoreCaseS133.Visibility = mode == "None" ? Visibility.Collapsed : Visibility.Visible;
        }
        private void DockOcrValidationModeS133_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 OCR 文本校验模式")) return;
            UpdateOcrValidationFieldsS133(); DockDraftValue_Changed(sender, e);
        }

        private void UpdateOcrModelVersionS133()
        {
            if (DockOcrModelVersionS133 == null) return;
            string hash = ocrValidationDraftS133.ModelSha256;
            DockOcrPinCurrentS133.Visibility = string.IsNullOrWhiteSpace(hash) ? Visibility.Visible : Visibility.Collapsed;
            DockOcrModelVersionS133.Text = string.IsNullOrWhiteSpace(hash) ? "旧版字库路径 · 尚未固定 SHA 版本" :
                "字库 " + hash.Substring(0, Math.Min(10, hash.Length)) + " · " + ocrValidationDraftS133.ClassCount + " 类";
            DockOcrModelVersionS133.ToolTip = string.IsNullOrWhiteSpace(hash) ? "旧方案兼容入口；固定当前字库或导入后，SHA、受管路径和类数进入草稿，应用后保存。" : "SHA-256: " + hash;
        }

        private void ImportOcrModelS133(string sourcePath)
        {
            long editLeaseS137 = CaptureEngineeringEditLeaseS137("导入 OCR 字库受管版本");
            if (editLeaseS137 < 0) return;
            RunUiAction("导入 OCR 字库", delegate
            {
                if (dockEditingTool == null || dockEditingTool.Kind != VmToolKind.OcrRecognition || !IsVariationTaskStoppedS130() || nccTrainingCancellationS131 != null)
                    throw new InvalidOperationException("请停止运行后导入 OCR 字库。" );
                string directory = Path.Combine(recipeService.RecipeDirectory, "OcrModels");
                var resource = VmOcrModelResourceS133.Import(sourcePath, directory);
                bool retained = false;
                try
                {
                    if (!IsEngineeringEditLeaseCurrentS137(editLeaseS137)) return;
                    ocrValidationDraftS133.ModelPath = resource.ModelPath;
                    ocrValidationDraftS133.ModelSha256 = resource.ModelSha256;
                    ocrValidationDraftS133.ClassCount = resource.ClassCount;
                    bool updating = dockDraftUpdating; dockDraftUpdating = true;
                    try { DockOcrModelTextBox.Text = resource.ModelPath; UpdateOcrModelVersionS133(); }
                    finally { dockDraftUpdating = updating; }
                    retained = true;
                    DockDraftValue_Changed(DockOcrModelTextBox, new RoutedEventArgs());
                    HeaderStatusText.Text = "OCR 字库已接受管草稿 · F5 试运行，确认后应用；未覆盖原字库";
                    LogInfo(dockEditingTool.InstanceName + "：导入字库 " + resource.ModelSha256 + " / " + resource.ClassCount + " 类，尚未应用");
                }
                finally { if (!retained) VmVariationModelS130.DeleteCreated(resource.ModelPath); } // Import owns a fresh Guid .omc, never the user source.
            });
        }
        private void DockOcrPinCurrentS133_Click(object sender, RoutedEventArgs e)
        { ImportOcrModelS133(DockOcrModelTextBox.Text); }

        private void PublishOcrCharacterViewsS133(VmToolInstance tool, IList<VmOcrCharacterS133> characters, string modelVersion, string context)
        {
            if (tool == null || characters == null || characters.Count > 10000) throw new InvalidOperationException("OCR 本次字符结果不完整或超界。" );
            ocrCharacterStatesS133[tool.ToolId] = new OcrCharacterStateS133
            {
                Tool = tool, Characters = characters.ToList().AsReadOnly(), ModelVersion = modelVersion, Context = context,
                Text = string.Concat(characters.Select(c => c.Text))
            };
        }
        private void ClearOcrCharacterViewsS133(VmToolInstance tool)
        { if (tool != null) ocrCharacterStatesS133.Remove(tool.ToolId); }

        private OcrCharacterStateS133 GetOcrCharacterStateS133(VmToolInstance tool)
        {
            if (tool == null || tool.Kind != VmToolKind.OcrRecognition) return null;
            OcrCharacterStateS133 state; object version, text;
            if (!ocrCharacterStatesS133.TryGetValue(tool.ToolId, out state)) return null;
            if (!ReferenceEquals(state.Tool, tool) || !tool.TryGetOutputValue("ModelVersion", out version) || !tool.TryGetOutputValue("Text", out text) ||
                !string.Equals(version as string, state.ModelVersion, StringComparison.OrdinalIgnoreCase) || !string.Equals(text as string, state.Text, StringComparison.Ordinal))
            { ocrCharacterStatesS133.Remove(tool.ToolId); return null; }
            return state;
        }

        private void RefreshOcrCharacterGridS133(VmToolInstance tool)
        {
            if (DockOcrCharactersResultS133 == null) return;
            bool show = tool != null && tool.Kind == VmToolKind.OcrRecognition;
            DockOcrCharactersResultS133.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show) DockRoiResultsDataGrid.Visibility = Visibility.Collapsed;
            var state = GetOcrCharacterStateS133(tool); bool abnormal = DockOcrOnlyAbnormalS133.IsChecked == true;
            if (tool == ocrCharactersToolS133 && state == ocrVisibleCharacterStateS133 && abnormal == ocrOnlyAbnormalAppliedS133) return;
            ocrCharacterGridUpdatingS133 = true;
            try
            {
                ocrCharactersToolS133 = tool; ocrVisibleCharacterStateS133 = state; ocrOnlyAbnormalAppliedS133 = abnormal;
                var all = state == null ? new List<VmOcrCharacterS133>() : state.Characters;
                var visible = all.Where(c => !abnormal || c.IsLowConfidence || c.IsRejected).ToList();
                DockOcrCharactersS133.ItemsSource = visible; DockOcrCharactersS133.SelectedIndex = -1;
                DockOcrCharacterSummaryS133.Text = visible.Count + " / " + all.Count + " 字符";
            }
            finally { ocrCharacterGridUpdatingS133 = false; }
            foreach (string id in ocrCharacterStatesS133.Keys.Where(id => !flowTools.Any(t => t.ToolId == id)).ToList()) ocrCharacterStatesS133.Remove(id);
        }
        private void DockOcrFilterS133_Changed(object sender, RoutedEventArgs e)
        { RefreshOcrCharacterGridS133(dockEditingTool); ScheduleRefreshDisplay(); }

        private void DockOcrCharacterS133_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ocrCharacterGridUpdatingS133 || dockDraftUpdating) return;
            var character = DockOcrCharactersS133.SelectedItem as VmOcrCharacterS133;
            if (character == null || ocrCharactersToolS133 != dockEditingTool || GetOcrCharacterStateS133(dockEditingTool) == null) return;
            SetImageContextSelection(VmImageContextOption.ModuleInput); UpdateImageContextViewport(true);
            HeaderStatusText.Text = string.Format(CultureInfo.InvariantCulture, "OCR 字符 #{0} · 置信度 {1:0.0000} · {2} · 仅定位查看，不改文本或判定", character.Index, character.Confidence, character.StateText);
            ScheduleRefreshDisplay();
        }

        private void DrawOcrCharacterSelectionS133(HWindow window, VmToolInstance tool, bool left)
        {
            if (window == null || historicalImageMode || tool != GetImageTaskToolS89() || tool != ocrCharactersToolS133) return;
            var state = GetOcrCharacterStateS133(tool); var character = DockOcrCharactersS133.SelectedItem as VmOcrCharacterS133;
            var image = left ? ResolveDualInputImageContext() : ResolveImageContext();
            if (state == null || character == null || !state.Characters.Contains(character) || state.Context != GetImageCoordinateContextS112(image)) return;
            HObject frame = null, cross = null;
            try
            {
                HOperatorSet.GenContourPolygonXld(out frame,
                    new HTuple(new[] { character.Row1, character.Row1, character.Row2, character.Row2, character.Row1 }),
                    new HTuple(new[] { character.Column1, character.Column2, character.Column2, character.Column1, character.Column1 }));
                HOperatorSet.GenCrossContourXld(out cross, character.Row, character.Column, 8, 0);
                window.SetColor("magenta"); window.SetLineWidth(3); window.DispObj(frame); window.DispObj(cross);
                window.DispText("#" + character.Index, "image", character.Row2 + 4, character.Column1, "magenta", "box", "false");
            }
            catch (HalconException) { }
            finally { DisposeObject(frame); DisposeObject(cross); }
        }
    }
}
