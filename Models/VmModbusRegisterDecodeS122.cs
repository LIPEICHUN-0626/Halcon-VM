using System;
using System.Globalization;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmModbusDecodedValueS122
    {
        public double? Number { get; set; }
        public string ExactText { get; set; }
        public string RawWords { get; set; }
        public string Type { get; set; }
        public string WordOrder { get; set; }
        public string Warning { get; set; }
    }

    public static class VmModbusRegisterDecodeS122
    {
        public const string U16 = "U16";
        public const string HighWordFirst = "HighWordFirst";
        public const string LowWordFirst = "LowWordFirst";
        private static readonly string[] types = { U16, "I16", "U32", "I32", "F32", "U64", "I64", "F64" };
        private const ulong LargestExactDoubleInteger = 9007199254740992UL;

        public static string NormalizeType(string type) { return string.IsNullOrWhiteSpace(type) ? U16 : type; }
        public static string NormalizeOrder(string order) { return string.IsNullOrWhiteSpace(order) ? HighWordFirst : order; }
        public static bool IsSupportedType(string type) { return types.Contains(NormalizeType(type), StringComparer.Ordinal); }
        public static bool IsSupportedOrder(string order) { return NormalizeOrder(order) == HighWordFirst || NormalizeOrder(order) == LowWordFirst; }
        public static int WordCount(string type)
        {
            string selected = NormalizeType(type);
            if (!IsSupportedType(selected)) throw new InvalidOperationException("请选择受支持的寄存器主值类型。");
            return selected.EndsWith("16", StringComparison.Ordinal) ? 1 : selected.EndsWith("32", StringComparison.Ordinal) ? 2 : 4;
        }

        public static VmToolValueOption[] TypeOptions()
        {
            return new[]
            {
                Option(U16, "无符号 16 位（原始）"), Option("I16", "有符号 16 位"),
                Option("U32", "无符号 32 位"), Option("I32", "有符号 32 位"), Option("F32", "浮点 32 位"),
                Option("U64", "无符号 64 位"), Option("I64", "有符号 64 位"), Option("F64", "浮点 64 位")
            };
        }

        public static VmToolValueOption[] OrderOptions()
        {
            return new[] { Option(HighWordFirst, "高字在前"), Option(LowWordFirst, "低字在前") };
        }

        private static VmToolValueOption Option(string key, string label)
        {
            return new VmToolValueOption { Key = key, DisplayText = label };
        }

        public static string Validate(int function, int quantity, string type, string order)
        {
            if (function != 3 && function != 4) return null;
            if (!IsSupportedType(type)) return "寄存器主值类型无效。";
            if (!IsSupportedOrder(order)) return "寄存器字序只能是高字在前或低字在前。";
            int needed = WordCount(type);
            if (needed > 1 && quantity < needed)
                return "当前类型需要至少 " + needed.ToString(CultureInfo.InvariantCulture) + " 个连续寄存器；请增加读取数量。";
            return null;
        }

        public static VmModbusDecodedValueS122 Decode(double[] registers, int function, string type, string order)
        {
            if (function != 3 && function != 4)
                throw new InvalidOperationException("寄存器类型解码只适用于功能码 03/04。");
            string selected = NormalizeType(type);
            string selectedOrder = NormalizeOrder(order);
            string error = Validate(function, registers == null ? 0 : registers.Length, selected, selectedOrder);
            if (error != null) throw new InvalidOperationException(error);
            int count = WordCount(selected);
            if (registers == null || registers.Length < count) throw new InvalidOperationException("响应寄存器数量不足，不能解码主值。");
            ushort[] raw = new ushort[count];
            for (int index = 0; index < count; index++)
            {
                double word = registers[index];
                if (double.IsNaN(word) || double.IsInfinity(word) || word != Math.Truncate(word) || word < 0 || word > 65535)
                    throw new InvalidOperationException("原始寄存器包含非 0–65535 整数，已拒绝解码。");
                raw[index] = (ushort)word;
            }
            ushort[] ordered = selectedOrder == LowWordFirst ? raw.Reverse().ToArray() : raw;
            ulong bits = 0;
            foreach (ushort word in ordered) bits = (bits << 16) | word;
            var result = new VmModbusDecodedValueS122
            {
                Type = selected, WordOrder = selectedOrder,
                RawWords = string.Join(" ", raw.Select(word => "0x" + word.ToString("X4", CultureInfo.InvariantCulture)).ToArray())
            };
            switch (selected)
            {
                case U16: result.Number = raw[0]; result.ExactText = raw[0].ToString(CultureInfo.InvariantCulture); break;
                case "I16": result.Number = unchecked((short)raw[0]); result.ExactText = ((short)result.Number.Value).ToString(CultureInfo.InvariantCulture); break;
                case "U32": result.Number = (uint)bits; result.ExactText = ((uint)bits).ToString(CultureInfo.InvariantCulture); break;
                case "I32": result.Number = unchecked((int)bits); result.ExactText = unchecked((int)bits).ToString(CultureInfo.InvariantCulture); break;
                case "F32":
                    float single = BitConverter.ToSingle(BitConverter.GetBytes((uint)bits), 0);
                    if (float.IsNaN(single) || float.IsInfinity(single)) throw new InvalidOperationException("浮点主值不是有限数，已拒绝作为 Number 输出。");
                    result.Number = single; result.ExactText = single.ToString("R", CultureInfo.InvariantCulture); break;
                case "U64":
                    result.ExactText = bits.ToString(CultureInfo.InvariantCulture);
                    if (bits <= LargestExactDoubleInteger) result.Number = bits;
                    else result.Warning = "64 位整数超过 Number 精确范围；仅保留 DecodedText，不发布近似数值。";
                    break;
                case "I64":
                    long signed = unchecked((long)bits);
                    result.ExactText = signed.ToString(CultureInfo.InvariantCulture);
                    if (signed >= -(long)LargestExactDoubleInteger && signed <= (long)LargestExactDoubleInteger) result.Number = signed;
                    else result.Warning = "64 位整数超过 Number 精确范围；仅保留 DecodedText，不发布近似数值。";
                    break;
                case "F64":
                    double floating = BitConverter.ToDouble(BitConverter.GetBytes(bits), 0);
                    if (double.IsNaN(floating) || double.IsInfinity(floating)) throw new InvalidOperationException("浮点主值不是有限数，已拒绝作为 Number 输出。");
                    result.Number = floating; result.ExactText = floating.ToString("R", CultureInfo.InvariantCulture); break;
            }
            return result;
        }
    }
}
