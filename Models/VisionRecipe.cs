using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VisionRecipe
    {
        public VisionRecipe()
        {
            Name = "DefaultRecipe";
            ProcedureName = "RunInspection";
            BlobMinGray = 80;
            BlobMaxGray = 255;
            BlobMinArea = 50;
            GrayMin = 0;
            GrayMax = 255;
            EdgeThreshold = 30;
            EnableShapeMatch = true;
            EnableBlob = false;
            EnableGrayStat = false;
            EnableEdgeMeasure = false;
            EnableHDevelop = false;
            TcpIp = "127.0.0.1";
            TcpPort = 9000;
            TcpMode = "Client";
            TcpEncoding = "UTF-8";
            AutoSendResult = true;
            FlowRunPolicy = new VmFlowRunPolicy();
        }

        [DataMember(Order = 1)]
        public string Name { get; set; }

        [DataMember(Order = 2)]
        public string LastImageDirectory { get; set; }

        [DataMember(Order = 3)]
        public RoiRecipeData SearchRoi { get; set; }

        [DataMember(Order = 4)]
        public string TemplatePath { get; set; }

        [DataMember(Order = 5)]
        public TemplateMatchRecipeData TemplateOptions { get; set; }

        [DataMember(Order = 6)]
        public bool EnableShapeMatch { get; set; }

        [DataMember(Order = 7)]
        public bool EnableBlob { get; set; }

        [DataMember(Order = 8)]
        public bool EnableGrayStat { get; set; }

        [DataMember(Order = 9)]
        public bool EnableEdgeMeasure { get; set; }

        [DataMember(Order = 10)]
        public bool EnableHDevelop { get; set; }

        [DataMember(Order = 11)]
        public double BlobMinGray { get; set; }

        [DataMember(Order = 12)]
        public double BlobMaxGray { get; set; }

        [DataMember(Order = 13)]
        public double BlobMinArea { get; set; }

        [DataMember(Order = 14)]
        public double GrayMin { get; set; }

        [DataMember(Order = 15)]
        public double GrayMax { get; set; }

        [DataMember(Order = 16)]
        public double EdgeThreshold { get; set; }

        [DataMember(Order = 17)]
        public string HDevelopPath { get; set; }

        [DataMember(Order = 18)]
        public string ProcedureName { get; set; }

        [DataMember(Order = 19)]
        public string TcpMode { get; set; }

        [DataMember(Order = 20)]
        public string TcpIp { get; set; }

        [DataMember(Order = 21)]
        public int TcpPort { get; set; }

        [DataMember(Order = 22)]
        public string TcpEncoding { get; set; }

        [DataMember(Order = 23)]
        public bool AutoSendResult { get; set; }

        [DataMember(Order = 24, EmitDefaultValue = false)]
        public List<ToolFlowRecipeItem> ToolFlow { get; set; }

        [DataMember(Order = 25, EmitDefaultValue = false)]
        public List<RoiLayerRecipeData> RoiLayers { get; set; }

        [DataMember(Order = 26, EmitDefaultValue = false)]
        public VmFlowRunPolicy FlowRunPolicy { get; set; }

    }

    [DataContract]
    public sealed class ToolFlowRecipeItem
    {
        [DataMember(Order = 1)]
        public string ToolId { get; set; }

        [DataMember(Order = 2)]
        public string ToolType { get; set; }

        [DataMember(Order = 3)]
        public string InstanceName { get; set; }

        [DataMember(Order = 4)]
        public bool IsEnabled { get; set; }

        [DataMember(Order = 5, EmitDefaultValue = false)]
        public NumericJudgeRecipeData NumericJudge { get; set; }

        [DataMember(Order = 6, EmitDefaultValue = false)]
        public List<string> RoiIds { get; set; }

        [DataMember(Order = 7, EmitDefaultValue = false)]
        public VmToolParameterData Parameters { get; set; }
    }

    [DataContract]
    public sealed class NumericJudgeRecipeData
    {
        [DataMember(Order = 1)]
        public string InputToolId { get; set; }

        [DataMember(Order = 2)]
        public string InputPortName { get; set; }

        [DataMember(Order = 3)]
        public string Operator { get; set; }

        [DataMember(Order = 4)]
        public double LowerLimit { get; set; }

        [DataMember(Order = 5)]
        public double UpperLimit { get; set; }

        [DataMember(Order = 6)]
        public double Tolerance { get; set; }
    }

    [DataContract]
    public sealed class RoiRecipeData
    {
        [DataMember(Order = 1)]
        public string ShapeType { get; set; }

        [DataMember(Order = 2)]
        public double Row1 { get; set; }

        [DataMember(Order = 3)]
        public double Column1 { get; set; }

        [DataMember(Order = 4)]
        public double Row2 { get; set; }

        [DataMember(Order = 5)]
        public double Column2 { get; set; }

        [DataMember(Order = 6)]
        public double Row { get; set; }

        [DataMember(Order = 7)]
        public double Column { get; set; }

        [DataMember(Order = 8)]
        public double Radius { get; set; }

        [DataMember(Order = 9)]
        public double[] PolygonRows { get; set; }

        [DataMember(Order = 10)]
        public double[] PolygonColumns { get; set; }
    }

    [DataContract]
    public sealed class RoiLayerRecipeData
    {
        [DataMember(Order = 1)]
        public string RoiId { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public bool IsEnabled { get; set; }

        [DataMember(Order = 4)]
        public bool IsVisible { get; set; }

        [DataMember(Order = 5)]
        public RoiRecipeData Geometry { get; set; }
    }

    [DataContract]
    public sealed class TemplateMatchRecipeData
    {
        [DataMember(Order = 1)]
        public double MinScore { get; set; }

        [DataMember(Order = 2)]
        public int MaxMatches { get; set; }

        [DataMember(Order = 3)]
        public double AngleStartDeg { get; set; }

        [DataMember(Order = 4)]
        public double AngleExtentDeg { get; set; }

        [DataMember(Order = 5)]
        public bool LimitToSearchRoi { get; set; }
    }

    [DataContract]
    public sealed class UiLayoutState
    {
        [DataMember(Order = 1)]
        public double BottomPanelHeight { get; set; }

        [DataMember(Order = 2)]
        public double RightPanelWidth { get; set; }

        [DataMember(Order = 3)]
        public string LastRecipePath { get; set; }

        [DataMember(Order = 4)]
        public string LastImageDirectory { get; set; }
    }
}
