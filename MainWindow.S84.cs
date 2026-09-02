using System;
using System.Globalization;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private InspectionRecord RunAutomaticThresholdToolS84(VmToolInstance tool, HImage inputImage)
        {
            VmToolParameterData parameters = tool.Parameters ?? new VmToolParameterData();
            parameters.Normalize();
            string error = parameters.Validate(VmToolKind.AutomaticThreshold);
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            HImage grayImage = null;
            HRegion effectiveRoi = null;
            HObject reducedImage = null;
            HObject automaticRegion = null;
            HObject connectedRegion = null;
            HTuple thresholdTuple = null;
            try
            {
                if (imageService.GetChannelCount(inputImage) <= 0)
                {
                    throw new InvalidOperationException("输入图像没有可用通道。");
                }

                grayImage = CreateGrayImage(inputImage);
                effectiveRoi = CreateEffectiveRoiRegion(tool, false);
                HObject operationImage = grayImage;
                if (effectiveRoi != null)
                {
                    HOperatorSet.ReduceDomain(grayImage, effectiveRoi, out reducedImage);
                    operationImage = reducedImage;
                }

                string polarity = VmDynamicThresholdPolarity.Normalize(parameters.AutomaticThresholdPolarity);
                HOperatorSet.BinaryThreshold(
                    operationImage,
                    out automaticRegion,
                    "max_separability",
                    VmDynamicThresholdPolarity.GetHalconValue(polarity),
                    out thresholdTuple);
                HOperatorSet.Connection(automaticRegion, out connectedRegion);

                HTuple count;
                HTuple area;
                HTuple row;
                HTuple column;
                HOperatorSet.CountObj(connectedRegion, out count);
                HOperatorSet.AreaCenter(automaticRegion, out area, out row, out column);
                double totalArea = SumTuple(area);
                int regionCount = totalArea <= 0 || count == null || count.Length == 0 ? 0 : count.I;
                double threshold = thresholdTuple == null || thresholdTuple.Length == 0 ? double.NaN : thresholdTuple.D;
                if (double.IsNaN(threshold) || double.IsInfinity(threshold))
                {
                    throw new InvalidOperationException("HALCON 未返回有效的自动阈值。");
                }
                if (totalArea <= 0)
                {
                    DisposeObject(connectedRegion);
                    HOperatorSet.GenEmptyObj(out connectedRegion);
                }

                string resultCode = regionCount > 0 && totalArea > 0 ? "OK" : "NG";
                string scope = effectiveRoi == null ? "全图" : "ROI-屏蔽有效域";
                string parameterSummary = string.Format(
                    CultureInfo.InvariantCulture,
                    "max_separability; Polarity={0}; Scope={1}",
                    polarity,
                    scope);

                tool.SetOutputValue("Region", VmRegionSnapshot.Create(connectedRegion, tool.ToolId, tool.InstanceName, "Region"));
                tool.SetOutputValue("Area", totalArea);
                tool.SetOutputValue("RegionCount", (double)regionCount);
                tool.SetOutputValue("Threshold", threshold);
                tool.SetOutputValue("Polarity", polarity);
                tool.SetOutputValue("Parameters", parameterSummary);
                tool.SetOutputValue("ResultCode", resultCode);
                ReplaceToolOverlayRegion(connectedRegion, tool, "Region", regionCount, totalArea, resultCode);
                connectedRegion = null;

                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "HALCON max_separability {0}，Threshold={1:0.###}，{2}，Region={3}，Area={4:0.###}",
                    VmDynamicThresholdPolarity.GetDisplayText(polarity),
                    threshold,
                    scope,
                    regionCount,
                    totalArea);
                InspectionRecord record = CreateRecord("AutomaticThreshold", resultCode, totalArea, message, tool);
                resultStore.Add(record);
                LogInfo(tool.InstanceName + " 完成：" + message);
                return record;
            }
            finally
            {
                DisposeObject(connectedRegion);
                DisposeObject(automaticRegion);
                DisposeObject(reducedImage);
                if (effectiveRoi != null) effectiveRoi.Dispose();
                if (grayImage != null) grayImage.Dispose();
                if (thresholdTuple != null) thresholdTuple.Dispose();
            }
        }
    }
}
