using System;

namespace HalconWinFormsDemo.Models
{
    public static class VmModbusFunctionS118
    {
        public static bool IsRead(int function) { return function >= 1 && function <= 4; }
        public static bool IsBit(int function) { return function == 1 || function == 2 || function == 5; }

        public static VmToolValueOption[] CreateAll()
        {
            return new[]
            {
                new VmToolValueOption { Key = "01", DisplayText = "读线圈 · 01" },
                new VmToolValueOption { Key = "02", DisplayText = "读离散输入 · 02" },
                new VmToolValueOption { Key = "03", DisplayText = "读保持寄存器 · 03" },
                new VmToolValueOption { Key = "04", DisplayText = "读输入寄存器 · 04" },
                new VmToolValueOption { Key = "05", DisplayText = "写单线圈 · 05" },
                new VmToolValueOption { Key = "06", DisplayText = "写单寄存器 · 06" }
            };
        }

        public static string Display(int function)
        {
            foreach (VmToolValueOption item in CreateAll())
                if (item.Key == function.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)) return item.DisplayText;
            return "未知功能码";
        }
    }
}
