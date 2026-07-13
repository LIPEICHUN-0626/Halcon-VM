using System.Collections.Generic;

namespace HalconWinFormsDemo.Models
{
    public sealed class VmPortDefinition
    {
        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public bool IsOptional { get; set; }
    }

    public sealed class VmPortDisplayItem
    {
        public string Direction { get; set; }

        public string PortName { get; set; }

        public string DisplayName { get; set; }

        public string DataType { get; set; }

        public string Source { get; set; }

        public string CurrentValue { get; set; }

        public string Status { get; set; }

        public bool IsConnected { get; set; }
    }

    public sealed class VmSourceToolOption
    {
        public string ToolId { get; set; }

        public string DisplayText { get; set; }
    }

    public sealed class VmSourcePortOption
    {
        public string PortName { get; set; }

        public string DisplayText { get; set; }

        public string DataType { get; set; }
    }

    public sealed class NumericJudgeOperatorOption
    {
        public const string BetweenInclusive = "BetweenInclusive";
        public const string OutsideInclusive = "OutsideInclusive";
        public const string GreaterOrEqual = "GreaterOrEqual";
        public const string LessOrEqual = "LessOrEqual";
        public const string Equal = "Equal";
        public const string NotEqual = "NotEqual";

        public string Key { get; set; }

        public string DisplayText { get; set; }

        public static IList<NumericJudgeOperatorOption> CreateAll()
        {
            return new List<NumericJudgeOperatorOption>
            {
                new NumericJudgeOperatorOption { Key = BetweenInclusive, DisplayText = "区间内（含边界）" },
                new NumericJudgeOperatorOption { Key = OutsideInclusive, DisplayText = "区间外（含边界）" },
                new NumericJudgeOperatorOption { Key = GreaterOrEqual, DisplayText = "大于等于下限" },
                new NumericJudgeOperatorOption { Key = LessOrEqual, DisplayText = "小于等于上限" },
                new NumericJudgeOperatorOption { Key = Equal, DisplayText = "等于目标值（下限）" },
                new NumericJudgeOperatorOption { Key = NotEqual, DisplayText = "不等于目标值（下限）" }
            };
        }

        public static bool IsSupported(string key)
        {
            return key == BetweenInclusive ||
                   key == OutsideInclusive ||
                   key == GreaterOrEqual ||
                   key == LessOrEqual ||
                   key == Equal ||
                   key == NotEqual;
        }

        public static string GetDisplayText(string key)
        {
            foreach (NumericJudgeOperatorOption item in CreateAll())
            {
                if (item.Key == key)
                {
                    return item.DisplayText;
                }
            }

            return key ?? string.Empty;
        }
    }
}
