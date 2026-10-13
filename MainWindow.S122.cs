using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void LoadModbusDecodeDraftS122(VmToolParameterData p, bool rtu)
        {
            ComboBox type = rtu ? DockRtuTypeS122 : DockModbusTypeS122;
            ComboBox order = rtu ? DockRtuWordOrderS122 : DockModbusWordOrderS122;
            if (type.ItemsSource == null) type.ItemsSource = VmModbusRegisterDecodeS122.TypeOptions();
            if (order.ItemsSource == null) order.ItemsSource = VmModbusRegisterDecodeS122.OrderOptions();
            type.SelectedValue = VmModbusRegisterDecodeS122.NormalizeType(p.ModbusRegisterTypeS122);
            order.SelectedValue = VmModbusRegisterDecodeS122.NormalizeOrder(p.ModbusWordOrderS122);
            UpdateModbusDecodeModeS122(rtu);
        }

        private void ReadModbusDecodeDraftS122(VmToolParameterData p, bool rtu)
        {
            p.ModbusRegisterTypeS122 = VmModbusRegisterDecodeS122.NormalizeType((rtu ? DockRtuTypeS122 : DockModbusTypeS122).SelectedValue as string);
            p.ModbusWordOrderS122 = VmModbusRegisterDecodeS122.NormalizeOrder((rtu ? DockRtuWordOrderS122 : DockModbusWordOrderS122).SelectedValue as string);
        }

        private void DockModbusTypeS122_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 Modbus TCP 主值解码")) return;
            UpdateModbusDecodeModeS122(false);
            DockDraftValue_Changed(sender, e);
        }

        private void DockRtuTypeS122_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!dockDraftUpdating && !RequireEngineeringEditS137("修改 RTU 主值解码")) return;
            UpdateModbusDecodeModeS122(true);
            DockDraftValue_Changed(sender, e);
        }

        private void UpdateModbusDecodeModeS122(bool rtu)
        {
            StackPanel panel = rtu ? DockRtuDecodeS122 : DockModbusDecodeS122;
            DockPanel orderRow = rtu ? DockRtuOrderRowS122 : DockModbusOrderRowS122;
            if (panel == null || orderRow == null) return;
            string rawFunction = (rtu ? DockRtuFunctionS119 : DockModbusFunctionS118).SelectedValue as string;
            int function;
            bool registers = int.TryParse(rawFunction, NumberStyles.Integer, CultureInfo.InvariantCulture, out function) && (function == 3 || function == 4);
            panel.Visibility = registers ? Visibility.Visible : Visibility.Collapsed;
            string selectedType = (rtu ? DockRtuTypeS122 : DockModbusTypeS122).SelectedValue as string;
            orderRow.Visibility = registers && VmModbusRegisterDecodeS122.IsSupportedType(selectedType) &&
                VmModbusRegisterDecodeS122.WordCount(selectedType) > 1 ? Visibility.Visible : Visibility.Collapsed;
            TextBlock hint = rtu ? DockRtuDecodeHintS122 : DockModbusDecodeHintS122;
            if (hint != null && registers)
            {
                int needed = VmModbusRegisterDecodeS122.IsSupportedType(selectedType) ? VmModbusRegisterDecodeS122.WordCount(selectedType) : 1;
                hint.Text = "主值读取前 " + needed.ToString(CultureInfo.InvariantCulture) +
                    " 个连续寄存器；数量不能少于此值。原始寄存器和帧在结果 → 更多中查看。";
            }
        }

        private static string FormatModbusDecodePreviewS122(double[] raw, int function, VmToolParameterData p)
        {
            if (function != 3 && function != 4) return "位/写入回显：" + (raw == null || raw.Length == 0 ? "--" : raw[0].ToString(CultureInfo.InvariantCulture));
            VmModbusDecodedValueS122 decoded = VmModbusRegisterDecodeS122.Decode(raw, function, p.ModbusRegisterTypeS122, p.ModbusWordOrderS122);
            return decoded.RawWords + " → " + decoded.Type + " / " +
                (decoded.WordOrder == VmModbusRegisterDecodeS122.HighWordFirst ? "高字在前" : "低字在前") + " = " + decoded.ExactText +
                (string.IsNullOrWhiteSpace(decoded.Warning) ? string.Empty : "；" + decoded.Warning);
        }

        private static VmModbusDecodedValueS122 DecodeModbusPrimaryS122(double[] raw, int function, VmToolParameterData p)
        {
            if (raw == null || raw.Length == 0) throw new InvalidOperationException("响应没有可用的原始值。");
            if (function == 3 || function == 4)
                return VmModbusRegisterDecodeS122.Decode(raw, function, p.ModbusRegisterTypeS122, p.ModbusWordOrderS122);
            return new VmModbusDecodedValueS122
            {
                Number = raw[0], ExactText = raw[0].ToString(CultureInfo.InvariantCulture),
                RawWords = raw[0].ToString(CultureInfo.InvariantCulture),
                Type = function == 1 || function == 2 ? "Bit" : "WriteEcho", WordOrder = "-"
            };
        }

        private static void PublishModbusPrimaryS122(VmToolInstance tool, VmModbusDecodedValueS122 value, string decodeError)
        {
            if (value == null)
            {
                tool.SetOutputValue("DecodeStatus", "失败：" + decodeError);
                return;
            }
            tool.SetOutputValue("DecodedText", value.ExactText);
            tool.SetOutputValue("RawWords", value.RawWords);
            tool.SetOutputValue("DecodeType", value.Type);
            tool.SetOutputValue("WordOrder", value.WordOrder);
            tool.SetOutputValue("DecodeSummary", value.Type + (value.WordOrder == "-" ? string.Empty :
                " · " + (value.WordOrder == VmModbusRegisterDecodeS122.HighWordFirst ? "高字在前" : "低字在前")) +
                " · " + value.RawWords);
            tool.SetOutputValue("DecodeStatus", string.IsNullOrWhiteSpace(value.Warning) ? "已解码" : value.Warning);
            if (value.Number.HasValue) tool.SetOutputValue("Value", value.Number.Value);
        }
    }
}
