using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VisionRecipe
    {
        public VisionRecipe()
        {
            SchemaVersion = RecipeSchema.CurrentVersion;
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
            RightImageCrosshairs = VmImageCrosshairViewSettings.CreateDefault();
            LeftImageCrosshairs = VmImageCrosshairViewSettings.CreateDefault();
            ProductionRunLayout = VmProductionRunLayoutData.CreateDefault();
            GlobalVariables = new List<VmGlobalVariableData>();
            ExternalResources = new List<VmExternalResourceData>();
        }

        // New workspaces are empty; the constructor retains legacy defaults for
        // explicit old-recipe migration and must not serve as a new-project template.
        public static VisionRecipe CreateBlank()
        {
            return new VisionRecipe
            {
                Name = "未命名方案",
                EnableShapeMatch = false,
                ToolFlow = new List<ToolFlowRecipeItem>(),
                RoiLayers = new List<RoiLayerRecipeData>()
            };
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

        [DataMember(Order = 27, EmitDefaultValue = false)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 28, EmitDefaultValue = false)]
        public string SavedAtUtc { get; set; }

        [DataMember(Order = 29, EmitDefaultValue = false)]
        public VmImageCrosshairViewSettings RightImageCrosshairs { get; set; }

        [DataMember(Order = 30, EmitDefaultValue = false)]
        public VmImageCrosshairViewSettings LeftImageCrosshairs { get; set; }

        [DataMember(Order = 31, EmitDefaultValue = false)]
        public List<ResultLayerStyleRecipeData> ResultLayerStyles { get; set; }

        [DataMember(Order = 32, EmitDefaultValue = false)]
        public VmProductionRunLayoutData ProductionRunLayout { get; set; }

        [DataMember(Order = 33, EmitDefaultValue = false)]
        public List<VmGlobalVariableData> GlobalVariables { get; set; }

        [DataMember(Order = 34, EmitDefaultValue = false)]
        public List<VmExternalResourceData> ExternalResources { get; set; }

        [DataMember(Order = 35, EmitDefaultValue = false)]
        public List<VmProjectFlowData> Flows { get; set; }

        [DataMember(Order = 36, EmitDefaultValue = false)]
        public string ActiveFlowId { get; set; }

        [DataMember(Order = 37, EmitDefaultValue = false)]
        public VmTcpFlowTriggerData TcpFlowTrigger { get; set; }

        [DataMember(Order = 38, EmitDefaultValue = false)]
        public VmEngineeringPackageManifestS139 EngineeringPackageS139 { get; set; }

    }

    [DataContract]
    public sealed class VmProductionRunLayoutData
    {
        public const string Scheme = "Scheme";
        public const string Image = "Image";
        public const string OverallResult = "OverallResult";
        public const string Statistics = "Statistics";
        public const string RunControl = "RunControl";
        public const string Batch = "Batch";
        public const string Alarm = "Alarm";
        public const string RecentResults = "RecentResults";

        [DataMember(Order = 1, EmitDefaultValue = false)]
        public List<VmProductionWidgetLayoutData> Widgets { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public List<VmProductionResultItemS141> ResultItemsS141 { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false)]
        public List<VmProductionImagePolicyS142> ImagePoliciesS142 { get; set; }

        public static VmProductionRunLayoutData CreateDefault()
        {
            return new VmProductionRunLayoutData
            {
                Widgets = new List<VmProductionWidgetLayoutData>
                {
                    Item(Scheme, "生产运行", true, 0, false),
                    Item(Image, "实时图像", true, 1, true),
                    Item(OverallResult, "总判定", true, 2, true),
                    Item(Statistics, "生产统计", true, 3, false),
                    Item(RunControl, "运行控制", true, 4, true),
                    Item(Batch, "图片批次", true, 5, false),
                    Item(Alarm, "报警与故障定位", true, 6, false),
                    Item(RecentResults, "最近结果（真实结果记录）", true, 7, false)
                }
            };
        }

        public VmProductionRunLayoutData CloneNormalized()
        {
            VmProductionRunLayoutData defaults = CreateDefault();
            Dictionary<string, VmProductionWidgetLayoutData> source = (Widgets ?? new List<VmProductionWidgetLayoutData>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.WidgetId))
                .GroupBy(item => item.WidgetId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            List<VmProductionWidgetLayoutData> normalized = new List<VmProductionWidgetLayoutData>();
            foreach (VmProductionWidgetLayoutData fallback in defaults.Widgets)
            {
                VmProductionWidgetLayoutData value;
                source.TryGetValue(fallback.WidgetId, out value);
                value = value ?? fallback;
                normalized.Add(new VmProductionWidgetLayoutData
                {
                    WidgetId = fallback.WidgetId,
                    Title = NormalizeTitle(value.Title, fallback.Title),
                    IsVisible = fallback.IsRequired || value.IsVisible,
                    Order = value.Order,
                    IsRequired = fallback.IsRequired
                });
            }
            normalized = normalized.OrderBy(item => item.Order).ThenBy(item => DefaultOrder(item.WidgetId)).ToList();
            for (int index = 0; index < normalized.Count; index++) normalized[index].Order = index;
            var resultItems = ResultItemsS141 ?? new List<VmProductionResultItemS141>();
            if (resultItems.Count > 8)
                throw new InvalidOperationException("生产结果项最多 8 项；不会静默丢弃超限配置。");
            var resultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var copies = new List<VmProductionResultItemS141>();
            foreach (var item in resultItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || !resultIds.Add(item.ItemId))
                    throw new InvalidOperationException("生产结果项的身份缺失或重复；不会按名称猜测映射。");
                copies.Add(item.Clone());
            }
            var imagePolicies = ImagePoliciesS142 ?? new List<VmProductionImagePolicyS142>();
            if (imagePolicies.Count > 32) throw new InvalidOperationException("周期留图策略最多 32 个流程，不能静默丢弃超限配置。");
            var policyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var policyCopies = new List<VmProductionImagePolicyS142>();
            foreach (var policy in imagePolicies)
            {
                if (policy == null || !policyIds.Add(policy.FlowId ?? string.Empty)) throw new InvalidOperationException("周期留图策略为空或流程身份重复。");
                string error = policy.Validate(); if (error.Length > 0) throw new InvalidOperationException(error);
                policyCopies.Add(policy.Clone());
            }
            return new VmProductionRunLayoutData { Widgets = normalized, ResultItemsS141 = copies, ImagePoliciesS142 = policyCopies };
        }

        public VmProductionRunLayoutData Clone()
        {
            return CloneNormalized();
        }

        private static VmProductionWidgetLayoutData Item(string id, string title, bool visible, int order, bool required)
        {
            return new VmProductionWidgetLayoutData { WidgetId = id, Title = title, IsVisible = visible, Order = order, IsRequired = required };
        }

        private static int DefaultOrder(string id)
        {
            VmProductionWidgetLayoutData item = CreateDefault().Widgets.First(value => string.Equals(value.WidgetId, id, StringComparison.OrdinalIgnoreCase));
            return item.Order;
        }

        private static string NormalizeTitle(string title, string fallback)
        {
            string value = string.IsNullOrWhiteSpace(title) ? fallback : title.Trim().Replace("\r", " ").Replace("\n", " ");
            return value.Length > 40 ? value.Substring(0, 40) : value;
        }
    }

    [DataContract]
    public sealed class VmProductionWidgetLayoutData
    {
        [DataMember(Order = 1)] public string WidgetId { get; set; }
        [DataMember(Order = 2)] public string Title { get; set; }
        [DataMember(Order = 3)] public bool IsVisible { get; set; }
        [DataMember(Order = 4)] public int Order { get; set; }
        [DataMember(Order = 5, EmitDefaultValue = false)] public bool IsRequired { get; set; }

        public VmProductionWidgetLayoutData Clone()
        {
            return new VmProductionWidgetLayoutData { WidgetId = WidgetId, Title = Title, IsVisible = IsVisible, Order = Order, IsRequired = IsRequired };
        }
    }

    [DataContract]
    public sealed class VmImageCrosshairViewSettings
    {
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public VmImageCrosshairGuideData Guide1 { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false)]
        public VmImageCrosshairGuideData Guide2 { get; set; }

        public static VmImageCrosshairViewSettings CreateDefault()
        {
            return new VmImageCrosshairViewSettings
            {
                Guide1 = VmImageCrosshairGuideData.CreateDefault("green"),
                Guide2 = VmImageCrosshairGuideData.CreateDefault("yellow")
            };
        }

        public VmImageCrosshairViewSettings CloneNormalized()
        {
            return new VmImageCrosshairViewSettings
            {
                Guide1 = VmImageCrosshairGuideData.Normalize(Guide1, "green"),
                Guide2 = VmImageCrosshairGuideData.Normalize(Guide2, "yellow")
            };
        }
    }

    [DataContract]
    public sealed class VmImageCrosshairGuideData
    {
        [DataMember(Order = 1, EmitDefaultValue = false)] public bool IsEnabled { get; set; }
        [DataMember(Order = 2, EmitDefaultValue = false)] public double X { get; set; }
        [DataMember(Order = 3, EmitDefaultValue = false)] public double Y { get; set; }
        [DataMember(Order = 4, EmitDefaultValue = false)] public string Color { get; set; }
        [DataMember(Order = 5, EmitDefaultValue = false)] public int Thickness { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public bool IsLocked { get; set; }

        public static VmImageCrosshairGuideData CreateDefault(string color)
        {
            return new VmImageCrosshairGuideData { Color = color, Thickness = 1 };
        }

        public static VmImageCrosshairGuideData Normalize(VmImageCrosshairGuideData source, string defaultColor)
        {
            VmImageCrosshairGuideData value = source ?? CreateDefault(defaultColor);
            return new VmImageCrosshairGuideData
            {
                IsEnabled = value.IsEnabled,
                X = IsFinite(value.X) && value.X >= 0 ? value.X : 0,
                Y = IsFinite(value.Y) && value.Y >= 0 ? value.Y : 0,
                Color = NormalizeColor(value.Color, defaultColor),
                Thickness = Math.Max(1, Math.Min(5, value.Thickness <= 0 ? 1 : value.Thickness)),
                IsLocked = value.IsLocked
            };
        }

        private static string NormalizeColor(string value, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
            string[] supported = { "green", "yellow", "cyan", "magenta", "red", "orange", "blue", "white" };
            foreach (string item in supported)
            {
                if (string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return fallback;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
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

        [DataMember(Order = 8, EmitDefaultValue = false)]
        public List<VmToolInputBindingData> InputBindings { get; set; }

        [DataMember(Order = 9, EmitDefaultValue = false)]
        public double? GraphX { get; set; }

        [DataMember(Order = 10, EmitDefaultValue = false)]
        public double? GraphY { get; set; }

        [DataMember(Order = 11, EmitDefaultValue = false)]
        public string GroupId { get; set; }

        [DataMember(Order = 12, EmitDefaultValue = false)]
        public string GroupName { get; set; }

        [DataMember(Order = 13, EmitDefaultValue = false)]
        public bool IsGroupCollapsed { get; set; }

        [DataMember(Order = 14, EmitDefaultValue = false)]
        public bool IsBreakpoint { get; set; }
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

        [DataMember(Order = 11, EmitDefaultValue = false)]
        public double Phi { get; set; }

        [DataMember(Order = 12, EmitDefaultValue = false)]
        public double Length1 { get; set; }

        [DataMember(Order = 13, EmitDefaultValue = false)]
        public double Length2 { get; set; }

        [DataMember(Order = 14, EmitDefaultValue = false)]
        public double InnerRadius { get; set; }

        [DataMember(Order = 15, EmitDefaultValue = false)]
        public double OuterRadius { get; set; }
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

        [DataMember(Order = 6, EmitDefaultValue = false)]
        public bool IsLocked { get; set; }

        [DataMember(Order = 7, EmitDefaultValue = false)]
        public bool IsMask { get; set; }

        [DataMember(Order = 8, EmitDefaultValue = false)] public string DisplayColor { get; set; }
        [DataMember(Order = 9, EmitDefaultValue = false)] public int DisplayLineWidth { get; set; }
        [DataMember(Order = 10, EmitDefaultValue = false)] public int DisplayOpacity { get; set; }
        [DataMember(Order = 11, EmitDefaultValue = false)] public string ViewportScope { get; set; }
        [DataMember(Order = 12, EmitDefaultValue = false)] public int LayerOrder { get; set; }
        [DataMember(Order = 13, EmitDefaultValue = false)] public string ImageContextId { get; set; }
    }

    [DataContract]
    public sealed class ResultLayerStyleRecipeData
    {
        public ResultLayerStyleRecipeData Clone() { return (ResultLayerStyleRecipeData)MemberwiseClone(); }
        [DataMember(Order = 1)] public string LayerKey { get; set; }
        [DataMember(Order = 2, EmitDefaultValue = false)] public string ToolId { get; set; }
        [DataMember(Order = 3, EmitDefaultValue = false)] public string PortName { get; set; }
        [DataMember(Order = 4, EmitDefaultValue = false)] public string DisplayName { get; set; }
        [DataMember(Order = 5, EmitDefaultValue = false)] public string Color { get; set; }
        [DataMember(Order = 6, EmitDefaultValue = false)] public int LineWidth { get; set; }
        [DataMember(Order = 7, EmitDefaultValue = false)] public int Opacity { get; set; }
        [DataMember(Order = 8, EmitDefaultValue = false)] public bool IsVisible { get; set; }
        [DataMember(Order = 9, EmitDefaultValue = false)] public bool IsLocked { get; set; }
        [DataMember(Order = 10, EmitDefaultValue = false)] public string ViewportScope { get; set; }
        [DataMember(Order = 11, EmitDefaultValue = false)] public int LayerOrder { get; set; }
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

        [DataMember(Order = 6, EmitDefaultValue = false)]
        public double ScaleMin { get; set; }

        [DataMember(Order = 7, EmitDefaultValue = false)]
        public double ScaleMax { get; set; }
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

        [DataMember(Order = 5, EmitDefaultValue = false)]
        public List<string> RecentRecipePaths { get; set; }

        [DataMember(Order = 6, EmitDefaultValue = false)]
        public List<string> FavoriteToolKinds { get; set; }

        [DataMember(Order = 7, EmitDefaultValue = false)]
        public List<string> RecentToolKinds { get; set; }

        [DataMember(Order = 8, EmitDefaultValue = false)]
        public bool IsBottomPanelExpanded { get; set; }
    }
}
