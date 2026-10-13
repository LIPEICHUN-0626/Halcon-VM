using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private bool BuildObjectProductionLearningProjectS146()
        {
            int firstTool = flowTools.Count;
            bool imageAvailable = BuildObjectExampleS144();
            var created = flowTools.Skip(firstTool).ToArray();
            var source = created.Single(tool => tool.Kind == VmToolKind.ImageSource);
            var objects = created.Single(tool => tool.Kind == VmToolKind.ConnectedObjectAnalysisS144);
            var judge = created.Single(tool => tool.Kind == VmToolKind.NumericJudge);
            var gray = CreateFlowTool(VmToolKind.GrayStat, "06 数量门槛通过时查看原图灰度", true, null);
            flowTools.Add(gray);
            gray.SetInputBinding("Image", source.ToolId, "Image");
            gray.SetInputBinding("$RunCondition", judge.ToolId, "Passed");
            gray.Parameters.ExecutionConditionWhenFalse = false;
            source.Parameters.LocalImageSerialNumber = "LEARN-OBJECT-PRODUCTION-S146";

            ConfigureLearningProductionItemsS146(
                LearningProductionItemS146(objects, "本次对象数", "Count", "Number", 0),
                LearningProductionItemS146(objects, "本次对象总面积", "TotalArea", "Number", 2),
                LearningProductionItemS146(judge, "数量学习门槛通过", "Passed", "Boolean", 0),
                LearningProductionItemS146(gray, "条件执行原图灰度", "Mean", "Number", 2));
            // The returned flag describes only the local sample file. It is not
            // a completed cycle, an accepted product, or a ready production flow.
            return imageAvailable;
        }

        private bool BuildAlignmentLineLearningProjectS146()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 选择单通道产品图像", true, null);
            var matching = CreateFlowTool(VmToolKind.NccTemplateMatch, "02 制作模板并明确选取目标", true, null);
            var alignment = CreateFlowTool(VmToolKind.PoseImageAlignment, "03 显式采固定参考并对齐", true, null);
            var line = CreateFlowTool(VmToolKind.ImageEdgeLineFindingS143, "04 在对齐图上设置搜索带找线", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "05 找线覆盖学习门槛", true, null);
            foreach (var tool in new[] { source, matching, alignment, line, judge }) flowTools.Add(tool);

            matching.SetInputBinding("Image", source.ToolId, "Image");
            matching.Parameters.NccMatchingS131 = new VmNccParametersS131 { MaxMatches = 10 };
            matching.Parameters.TemplateTargetsS145 = new VmTemplateTargetParametersS145
            {
                SortMode = "score",
                SelectionIndex = 0
            };
            alignment.SetInputBinding("Image", source.ToolId, "Image");
            alignment.SetInputBinding("Pose", matching.ToolId, "SelectedPose");
            alignment.Parameters.PoseImageAlignmentS132 = new VmPoseImageAlignmentParametersS132();
            line.SetInputBinding("Image", alignment.ToolId, "Image");
            line.Parameters.ImageEdgeLineS143 = new VmImageEdgeLineParametersS143();
            judge.SetInputBinding("Value", line.ToolId, "Coverage");
            judge.InputToolId = line.ToolId;
            judge.InputPortName = "Coverage";
            judge.NumericLowerLimit = 0.5;
            judge.NumericUpperLimit = 1;

            string path = GetHalconExampleImageS78("fabrik.png");
            source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "LEARN-ALIGNMENT-LINE-S146";
            ConfigureLearningProductionItemsS146(
                LearningProductionItemS146(matching, "本次有明确选中目标", "HasSelection", "Boolean", 0),
                LearningProductionItemS146(matching, "选中目标真实分数", "SelectedScore", "Number", 3),
                LearningProductionItemS146(line, "对齐图找线覆盖", "Coverage", "Number", 3),
                LearningProductionItemS146(judge, "覆盖学习门槛通过", "Passed", "Boolean", 0));
            // No model or reference is fabricated. The user must train/apply,
            // rerun, explicitly capture a reference, and edit this output band.
            return File.Exists(path);
        }

        private bool BuildWorldDistanceLearningProjectS146()
        {
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 选择待测与标定同源图像", true, null);
            var line = CreateFlowTool(VmToolKind.ImageEdgeLineFindingS143, "02 编辑搜索带获取真实边缘点", true, null);
            var fitting = CreateFlowTool(VmToolKind.TypedPointSetLineFittingMeasurement, "03 由保留点拟合像素端点", true, null);
            var calibration = CreateFlowTool(VmToolKind.PlanarCalibration2D, "04 实采标定对应点并填写真实单位", true, null);
            var distance = CreateFlowTool(VmToolKind.WorldPointPairMeasurement, "05 测量同标定世界端点距离", true, null);
            var judge = CreateFlowTool(VmToolKind.NumericJudge, "06 按真实单位修改距离学习范围", true, null);
            foreach (var tool in new[] { source, line, fitting, calibration, distance, judge }) flowTools.Add(tool);

            line.SetInputBinding("Image", source.ToolId, "Image");
            line.Parameters.ImageEdgeLineS143 = new VmImageEdgeLineParametersS143();
            fitting.SetInputBinding("Points", line.ToolId, "KeptPoints");
            fitting.Parameters.PointSetMinimumCount = 3;
            fitting.Parameters.XldLineFitAlgorithm = VmXldLineFitAlgorithm.Tukey;
            calibration.SetInputBinding("Image", source.ToolId, "Image");
            calibration.SetInputBinding("PointA", fitting.ToolId, "StartPoint");
            calibration.SetInputBinding("PointB", fitting.ToolId, "EndPoint");
            calibration.Parameters.PlanarCalibrationSamplesS124 = new List<VmPlanarSampleS124>();
            calibration.Parameters.PlanarCalibrationUnitS124 = string.Empty;
            distance.SetInputBinding("PointA", calibration.ToolId, "WorldPointA");
            distance.SetInputBinding("PointB", calibration.ToolId, "WorldPointB");
            judge.SetInputBinding("Value", distance.ToolId, "WorldDistance");
            judge.InputToolId = distance.ToolId;
            judge.InputPortName = "WorldDistance";
            judge.NumericLowerLimit = 0;
            judge.NumericUpperLimit = 100;

            string path = GetHalconExampleImageS78("fabrik.png");
            source.Parameters.LocalImagePath = path;
            source.Parameters.LocalImageSerialNumber = "LEARN-WORLD-DISTANCE-S146";
            ConfigureLearningProductionItemsS146(
                LearningProductionItemS146(line, "本次边缘覆盖", "Coverage", "Number", 3),
                LearningProductionItemS146(fitting, "像素拟合最大残差", "MaxResidual", "Number", 3),
                LearningProductionItemS146(distance, "本次世界端点距离", "WorldDistance", "Number", 3),
                LearningProductionItemS146(judge, "距离学习范围通过", "Passed", "Boolean", 0));
            // Empty calibration is intentional: no invented samples/matrix or
            // physical measurement is supplied. Legacy Normalize defaults the
            // UI unit to mm; this is not evidence of the user's actual unit.
            return File.Exists(path);
        }

        private VmProductionResultItemS141 LearningProductionItemS146(VmToolInstance tool,
            string title, string portName, string valueType, int decimalPlaces)
        {
            return new VmProductionResultItemS141
            {
                ItemId = Guid.NewGuid().ToString("N"),
                Title = title,
                FlowId = activeProjectFlowIdS104,
                ToolId = tool.ToolId,
                PortName = portName,
                ValueType = valueType,
                DecimalPlaces = decimalPlaces
            };
        }

        private void ConfigureLearningProductionItemsS146(params VmProductionResultItemS141[] items)
        {
            if (productionRunLayout == null) productionRunLayout = VmProductionRunLayoutData.CreateDefault();
            productionRunLayout.ResultItemsS141 = new List<VmProductionResultItemS141>(items);
            productionRunLayout.ImagePoliciesS142 = new List<VmProductionImagePolicyS142>();
            AutoSendMatchResultCheckBox.IsChecked = false;
            StopOnNgCheckBox.IsChecked = false;
        }
    }
}
