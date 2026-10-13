using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public sealed class VmProductionEvidenceRequestEventArgsS142 : EventArgs
    {
        public VmProductionRunRecordS135 Record { get; private set; }
        public VmProductionImageEvidenceS142 Image { get; private set; }
        public long Generation { get; private set; }
        public VmProductionEvidenceRequestEventArgsS142(VmProductionRunRecordS135 record, VmProductionImageEvidenceS142 image, long generation)
        { Record = record.Clone(); Image = image.Clone(); Generation = generation; }
    }

    public partial class ProductionRunHistoryWindowS135
    {
        private const int MaximumEvidenceEncodedBytesS142 = 64 * 1024 * 1024;
        private const long MaximumEvidencePixelsS142 = 16000000;
        private const int MaximumEvidenceDimensionS142 = 16384;
        private VmProductionRunRecordS135 evidenceRecordS142;
        private VmProductionImageEvidenceS142 evidenceRequestedImageS142;
        private long evidenceGenerationS142;
        private bool evidenceUpdatingS142, evidenceClosedS142;

        public event EventHandler<VmProductionEvidenceRequestEventArgsS142> EvidenceRequestedS142;

        private sealed class EvidenceImageRowS142
        {
            public VmProductionImageEvidenceS142 Image { get; set; }
            public string Label
            {
                get
                {
                    string state = Image.State == "Saved" ? "已保存（待读取校验）" : Image.State == "Missing" ? "缺失" : Image.State == "Pending" ? "尚未完成保存" : "状态未知";
                    return "图像源 " + Short(Image.ToolId, 8) + " · " + state;
                }
            }
            public string TechnicalDetail
            {
                get
                {
                    return "RunId：" + Image.RunId + "\nFlowId：" + Image.FlowId + "\n源 ToolId：" + Image.ToolId + " / " + Image.PortName +
                        "\n采集时间：" + Convert.ToString(Image.CapturedAtUtc, CultureInfo.InvariantCulture) + "\n采集模式：" + Image.SourceMode + " · SN：" + Image.SerialNumber +
                        "\n源路径文字（不重新打开）：" + Image.SourcePath + "\n本次坐标：" + Image.CoordinateContext +
                        "\n图像：" + Image.Width + " × " + Image.Height + "，通道 " + Image.Channels + "，像素 " + Image.PixelType + "，domain：" + Image.Domain +
                        "\n相对证据文件：" + Image.RelativePath + "\n文件字节：" + Image.ByteLength + "\nSHA-256：" + Image.Sha256 +
                        "\n保存事实：" + Image.State + " · " + Image.Detail + "\nSHA 仅是保存文件身份，不是签名、不可篡改或现场认证。";
                }
            }
        }

        private void InitializeEvidenceS142()
        {
            Closed += delegate
            {
                evidenceClosedS142 = true;
                ClearEvidencePreviewS142(); evidenceRecordS142 = null;
                EvidencePickerS142.ItemsSource = null;
            };
        }

        private void ClearEvidencePreviewS142()
        {
            unchecked { evidenceGenerationS142++; }
            evidenceRequestedImageS142 = null;
            if (EvidenceImageS142 != null) EvidenceImageS142.Source = null;
            if (EvidenceEmptyS142 != null) { EvidenceEmptyS142.Text = "显式点击查看；这是独立只读预览。"; EvidenceEmptyS142.Visibility = Visibility.Visible; }
        }

        private void RefreshSelectedEvidenceS142(VmProductionRunRecordS135 record)
        {
            if (EvidencePickerS142 == null || evidenceClosedS142) return;
            ClearEvidencePreviewS142();
            evidenceRecordS142 = record?.Clone();
            evidenceUpdatingS142 = true;
            try
            {
                var rows = (record?.EvidenceImagesS142 ?? new List<VmProductionImageEvidenceS142>()).Where(value => value != null).Take(4)
                    .Select(value => new EvidenceImageRowS142 { Image = value.Clone() }).ToList();
                EvidencePickerS142.ItemsSource = rows;
                EvidencePickerS142.SelectedItem = rows.FirstOrDefault(value => value.Image.State == "Saved") ?? rows.FirstOrDefault();
                EvidenceSummaryS142.Text = record == null ? "先在周期记录页选择周期；不会自动读取历史图。" : !record.EvidenceKnownS142 ?
                    "历史未保存图像事实；不根据旧路径补造证据。" :
                    "本周期：" + (record.EvidenceStatusS142 ?? "状态未声明") + " · " + rows.Count(value => value.Image.State == "Saved") + " / " + rows.Count + " 幅声明已保存";
                EvidenceSummaryS142.ToolTip = record == null ? null : "RunId：" + record.RunId + "\nFlowId：" + record.FlowId + "\n算法定义 v" + record.AlgorithmDefinitionKeyVersionS138 + "：" + record.AlgorithmDefinitionKeyS138;
                EvidenceStatusS142.Text = "原图来自所选采集源；不是失败步骤影像。仅显式读取受管文件，不重放或补采。";
            }
            finally { evidenceUpdatingS142 = false; }
            UpdateEvidenceSelectionS142();
        }

        private void UpdateEvidenceSelectionS142()
        {
            var selected = EvidencePickerS142.SelectedItem as EvidenceImageRowS142;
            EvidenceDetailS142.Text = selected?.TechnicalDetail ?? "此周期没有原图项。未启用、未到采集、未在留图范围或旧历史均不会补图。";
            bool saved = !evidenceClosedS142 && evidenceRecordS142 != null && evidenceRecordS142.EvidenceKnownS142 && selected != null && selected.Image.State == "Saved";
            EvidenceOpenButtonS142.IsEnabled = saved;
            if (selected != null && !saved)
            {
                EvidenceEmptyS142.Text = string.IsNullOrWhiteSpace(selected.Image.Detail) ? "本项没有已保存证据图。" : selected.Image.Detail;
                EvidenceEmptyS142.Visibility = Visibility.Visible;
            }
        }

        private void EvidenceSelectionS142_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (evidenceUpdatingS142 || EvidenceImageS142 == null) return;
            ClearEvidencePreviewS142(); UpdateEvidenceSelectionS142();
        }

        private void EvidenceTabS142_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, HistoryTabsS138) || EvidenceImageS142 == null) return;
            if (HistoryTabsS138.SelectedIndex != 2)
            {
                ClearEvidencePreviewS142(); UpdateEvidenceSelectionS142();
                EvidenceStatusS142.Text = "离开证据页已清理预览；再次查看须显式读取。";
            }
        }

        private void EvidenceOpenS142_Click(object sender, RoutedEventArgs e)
        {
            var selected = EvidencePickerS142.SelectedItem as EvidenceImageRowS142;
            if (evidenceClosedS142 || evidenceRecordS142 == null || !evidenceRecordS142.EvidenceKnownS142 || selected == null || selected.Image.State != "Saved" || HistoryTabsS138.SelectedIndex != 2) return;
            ClearEvidencePreviewS142();
            evidenceRequestedImageS142 = selected.Image.Clone();
            long generation = evidenceGenerationS142;
            try
            {
                ValidateEvidenceMetadataForUiS142(evidenceRequestedImageS142);
                if (!string.Equals(evidenceRequestedImageS142.RunId, evidenceRecordS142.RunId, StringComparison.Ordinal) ||
                    !string.Equals(evidenceRequestedImageS142.FlowId, evidenceRecordS142.FlowId, StringComparison.Ordinal))
                    throw new InvalidOperationException("图像元数据与所选周期身份不一致，拒绝查看。");
                var handler = EvidenceRequestedS142;
                if (handler == null) throw new InvalidOperationException("证据读取入口未接入，没有加载活动图像或旧源文件。");
                EvidenceOpenButtonS142.IsEnabled = false;
                EvidenceEmptyS142.Text = "正在核对受管证据文件…";
                EvidenceStatusS142.Text = "正在只读核对身份、路径、长度、哈希和像素预算。";
                handler(this, new VmProductionEvidenceRequestEventArgsS142(evidenceRecordS142, evidenceRequestedImageS142, generation));
            }
            catch (Exception error) { SetEvidenceErrorS142(error.Message, generation); }
        }

        public void SetEvidenceBytesS142(byte[] bytes, string message, long generation)
        {
            if (!IsEvidenceGenerationCurrentS142(generation)) return;
            var metadata = evidenceRequestedImageS142.Clone();
            try
            {
                ValidateEvidenceMetadataForUiS142(metadata);
                if (bytes == null || bytes.Length < 33 || bytes.Length > MaximumEvidenceEncodedBytesS142 || bytes.LongLength != metadata.ByteLength)
                    throw new InvalidOperationException("证据编码字节缺失、超过 64 MiB 或与保存长度不一致。");
                // ValidatePng is a bounded header check, not a decoder; the store has already verified the file SHA.
                ProductionImageEvidenceStoreS142.ValidatePng(bytes, metadata);
                ValidateEvidencePngHeaderForUiS142(bytes, metadata);
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(bytes, false))
                {
                    bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
                    bitmap.StreamSource = stream; bitmap.EndInit();
                    if (bitmap.PixelWidth != metadata.Width || bitmap.PixelHeight != metadata.Height ||
                        (long)bitmap.PixelWidth * bitmap.PixelHeight > MaximumEvidencePixelsS142 || bitmap.Format.BitsPerPixel > 32)
                        throw new InvalidOperationException("解码尺寸或像素格式与已核对元数据不一致。");
                    bitmap.Freeze();
                }
                if (!IsEvidenceGenerationCurrentS142(generation)) return;
                EvidenceImageS142.Source = bitmap;
                EvidenceEmptyS142.Visibility = Visibility.Collapsed;
                EvidenceStatusS142.Text = string.IsNullOrWhiteSpace(message) ? "已核对并只读显示所选周期原图；活动工程未改变。" : message;
                EvidenceOpenButtonS142.IsEnabled = true;
            }
            catch (Exception error) { SetEvidenceErrorS142("证据图不可用：" + error.Message, generation); }
        }

        public void SetEvidenceErrorS142(string message, long generation)
        {
            if (!IsEvidenceGenerationCurrentS142(generation)) return;
            EvidenceImageS142.Source = null;
            EvidenceEmptyS142.Text = string.IsNullOrWhiteSpace(message) ? "证据图读取失败；没有加载旧帧。" : message;
            EvidenceEmptyS142.Visibility = Visibility.Visible;
            EvidenceStatusS142.Text = EvidenceEmptyS142.Text;
            UpdateEvidenceSelectionS142();
        }

        private bool IsEvidenceGenerationCurrentS142(long generation)
        { return !evidenceClosedS142 && evidenceRequestedImageS142 != null && generation == evidenceGenerationS142 && HistoryTabsS138.SelectedIndex == 2; }

        private static void ValidateEvidenceMetadataForUiS142(VmProductionImageEvidenceS142 metadata)
        {
            if (metadata == null || metadata.State != "Saved" || metadata.Width < 1 || metadata.Height < 1 ||
                metadata.Width > MaximumEvidenceDimensionS142 || metadata.Height > MaximumEvidenceDimensionS142 ||
                (long)metadata.Width * metadata.Height > MaximumEvidencePixelsS142 ||
                (metadata.Channels != 1 && metadata.Channels != 3) || !string.Equals(metadata.PixelType, "byte", StringComparison.OrdinalIgnoreCase) ||
                metadata.ByteLength < 33 || metadata.ByteLength > MaximumEvidenceEncodedBytesS142 || metadata.PortName != "Image")
                throw new InvalidOperationException("证据元数据超预算或不在当前预览支持范围（byte、1/3 通道、16M 像素、单边 16384）。");
        }

        private static void ValidateEvidencePngHeaderForUiS142(byte[] bytes, VmProductionImageEvidenceS142 metadata)
        {
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int index = 0; index < signature.Length; index++) if (bytes[index] != signature[index]) throw new InvalidOperationException("文件不是支持的 PNG 原图。");
            if (ReadEvidenceBigEndianS142(bytes, 8) != 13 || bytes[12] != 'I' || bytes[13] != 'H' || bytes[14] != 'D' || bytes[15] != 'R')
                throw new InvalidOperationException("PNG 首部无效。");
            uint width = ReadEvidenceBigEndianS142(bytes, 16), height = ReadEvidenceBigEndianS142(bytes, 20);
            int channels = bytes[25] == 0 ? 1 : bytes[25] == 2 ? 3 : 0;
            if (width != metadata.Width || height != metadata.Height || width > MaximumEvidenceDimensionS142 || height > MaximumEvidenceDimensionS142 ||
                (long)width * height > MaximumEvidencePixelsS142 || bytes[24] != 8 || channels != metadata.Channels || bytes[26] != 0 || bytes[27] != 0 || bytes[28] > 1)
                throw new InvalidOperationException("PNG 尺寸、通道、像素深度或编码与冻结元数据不一致，未尝试解码。");
        }

        private static uint ReadEvidenceBigEndianS142(byte[] bytes, int offset)
        { return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3]; }
    }
}
