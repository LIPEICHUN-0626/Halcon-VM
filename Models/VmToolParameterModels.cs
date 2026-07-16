using System;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmToolParameterData
    {
        public VmToolParameterData()
        {
            BlobMinGray = 80;
            BlobMaxGray = 255;
            BlobMinArea = 50;
            GrayMin = 0;
            GrayMax = 255;
            EdgeThreshold = 30;
        }

        [DataMember(Order = 1)] public double BlobMinGray { get; set; }
        [DataMember(Order = 2)] public double BlobMaxGray { get; set; }
        [DataMember(Order = 3)] public double BlobMinArea { get; set; }
        [DataMember(Order = 4)] public double GrayMin { get; set; }
        [DataMember(Order = 5)] public double GrayMax { get; set; }
        [DataMember(Order = 6)] public double EdgeThreshold { get; set; }

        public VmToolParameterData Clone()
        {
            return (VmToolParameterData)MemberwiseClone();
        }

        public string Validate(VmToolKind kind)
        {
            if (kind == VmToolKind.Blob)
            {
                if (!IsFinite(BlobMinGray) || !IsFinite(BlobMaxGray) || BlobMinGray < 0 || BlobMaxGray > 255 || BlobMinGray > BlobMaxGray)
                {
                    return "Blob 灰度范围必须满足 0 ≤ 下限 ≤ 上限 ≤ 255。";
                }

                if (!IsFinite(BlobMinArea) || BlobMinArea < 0)
                {
                    return "Blob 最小面积必须是非负数。";
                }
            }
            else if (kind == VmToolKind.GrayStat)
            {
                if (!IsFinite(GrayMin) || !IsFinite(GrayMax) || GrayMin < 0 || GrayMax > 255 || GrayMin > GrayMax)
                {
                    return "灰度判定范围必须满足 0 ≤ 下限 ≤ 上限 ≤ 255。";
                }
            }
            else if (kind == VmToolKind.EdgeMeasure && (!IsFinite(EdgeThreshold) || EdgeThreshold <= 0))
            {
                return "边缘阈值必须大于 0。";
            }

            return string.Empty;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public static class VmNumericJudgeParameterValidator
    {
        public static string Validate(string comparisonOperator, double lower, double upper, double tolerance)
        {
            if (!NumericJudgeOperatorOption.IsSupported(comparisonOperator))
            {
                return "比较方式无效。";
            }

            if (!IsFinite(lower) || !IsFinite(upper))
            {
                return "阈值必须是有限数值。";
            }

            if ((comparisonOperator == NumericJudgeOperatorOption.BetweenInclusive ||
                 comparisonOperator == NumericJudgeOperatorOption.OutsideInclusive) && lower > upper)
            {
                return "区间下限不能大于上限。";
            }

            if (!IsFinite(tolerance) || tolerance < 0)
            {
                return "相等容差必须大于或等于 0。";
            }

            return string.Empty;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
