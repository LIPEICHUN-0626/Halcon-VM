using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    // No file access, algorithm execution, normalization, setters, or device/DLL probing.
    // Every actual DataMember is retained, including fields added after this service.
    public static class RecipeDiffServiceS140
    {
        public const int MaximumItems = 2000;
        public const int MaximumFieldCharacters = 8192;
        private const long MaximumDefinitionBytes = 64L * 1024 * 1024;
        private const int MaximumObjects = 100000;
        private const int MaximumFields = 1000000;
        private const int MaximumCollectionItems = 100000;
        private const int MaximumObjectDepth = 64;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly ConcurrentDictionary<Type, ContractMember[]> Contracts = new ConcurrentDictionary<Type, ContractMember[]>();

        public static string ComputeReviewIdentity(VisionRecipe recipe)
        {
            if (recipe == null) throw new InvalidOperationException("没有可冻结的方案定义。");
            return HashValue(recipe, "RecipeReview/v1");
        }

        public static VmRecipeDiffReportS140 Compare(VisionRecipe current, VisionRecipe target)
        {
            if (current == null || target == null) throw new InvalidOperationException("当前和目标已应用定义均不可缺失。");
            // This full-content identity does not depend on successful entity matching.
            // Duplicate/missing IDs can therefore produce a useful, explicitly partial report.
            var report = new VmRecipeDiffReportS140
            {
                CurrentIdentity = ComputeReviewIdentity(current), TargetIdentity = ComputeReviewIdentity(target), IsComplete = true
            };
            var review = new Review(current, target, report);
            if (current.SchemaVersion != RecipeSchema.CurrentVersion || target.SchemaVersion != RecipeSchema.CurrentVersion)
                review.Warn("存在非当前方案版本；仅比较已解析真实字段，不能证明未知文件字段已完整读取。", true);
            if ((current.Flows == null) != (target.Flows == null))
            {
                review.Add("未知", "流程身份", "方案", "流程模型", current.Flows == null ? "旧单流程（无 FlowId）" : "稳定 FlowId 多流程",
                    target.Flows == null ? "旧单流程（无 FlowId）" : "稳定 FlowId 多流程", false, true,
                    "旧单流程与具备 FlowId 的流程没有确定映射；不按名称或首条流程猜同一对象。各自实际字段继续独立列出。");
                review.Warn("旧单流程与多流程之间没有确定 FlowId 对齐，跨格式对象映射不完整。", true);
            }
            else if (current.Flows == null)
                review.Warn("双方按原 ToolFlow 旧单流程兼容作用域比较；这是兼容语义，不是新造的稳定 FlowId。", false);
            review.Warn("SHA/版本均为定义声明；本审阅不读取资源实际字节，不证明算法、DLL 安全、许可或设备可用。旧兼容镜像字段保守保留，多流程执行以 Flows 为准。", false);
            try { review.CompareNode(current, target, "", "方案", "方案/兼容配置", false, 0, 0); }
            catch (ReviewLimitException ex) { review.Warn(ex.Message, true); }
            review.Finish();
            return report;
        }

        private static ContractMember[] Members(Type type)
        {
            return Contracts.GetOrAdd(type, value =>
            {
                if (!Attribute.IsDefined(value, typeof(DataContractAttribute), true))
                    throw new ReviewLimitException("定义包含没有 DataContract 的未知对象类型，不能完整比较：" + value.Name);
                var result = new List<ContractMember>();
                foreach (var property in value.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var member = (DataMemberAttribute)Attribute.GetCustomAttribute(property, typeof(DataMemberAttribute), true);
                    if (member != null && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null)
                        result.Add(new ContractMember(property, member));
                }
                foreach (var field in value.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var member = (DataMemberAttribute)Attribute.GetCustomAttribute(field, typeof(DataMemberAttribute), true);
                    if (member != null) result.Add(new ContractMember(field, member));
                }
                if (result.GroupBy(item => item.Name, StringComparer.Ordinal).Any(group => group.Count() != 1))
                    throw new ReviewLimitException("定义包含重复序列化字段名，不能完整比较：" + value.Name);
                return result.OrderBy(item => item.Order).ThenBy(item => item.Name, StringComparer.Ordinal).ToArray();
            });
        }

        private static bool RuntimeMember(Type type, string name)
        {
            if (type == typeof(VisionRecipe))
                return name == "LastImageDirectory" || name == "SavedAtUtc" || name == "ActiveFlowId";
            if (type == typeof(VmGlobalVariableData))
                return name == "NumberValue" || name == "TextValue" || name == "BooleanValue" || name == "ResultCodeValue" || name == "CurrentArrayJson";
            return false;
        }

        private static bool DisplayMember(Type type, string name)
        {
            if (type == typeof(VisionRecipe))
                return name == "RightImageCrosshairs" || name == "LeftImageCrosshairs" || name == "ResultLayerStyles" || name == "ProductionRunLayout" || name == "EngineeringPackageS139";
            if (type == typeof(VmProjectFlowData)) return name == "ResultLayerStyles";
            if (type == typeof(ToolFlowRecipeItem))
                return name == "GraphX" || name == "GraphY" || name == "GroupId" || name == "GroupName" || name == "IsGroupCollapsed";
            if (type == typeof(RoiLayerRecipeData))
                return name == "IsVisible" || name == "IsLocked" || name == "DisplayColor" || name == "DisplayLineWidth" || name == "DisplayOpacity" ||
                    name == "ViewportScope" || name == "LayerOrder";
            return type == typeof(ResultLayerStyleRecipeData) || type == typeof(VmProductionRunLayoutData) || type == typeof(VmProductionWidgetLayoutData) ||
                type == typeof(VmImageCrosshairViewSettings) || type == typeof(VmImageCrosshairGuideData);
        }

        private static string EntityKey(Type type)
        {
            if (type == typeof(VmProjectFlowData)) return "FlowId";
            if (type == typeof(ToolFlowRecipeItem)) return "ToolId";
            if (type == typeof(RoiLayerRecipeData)) return "RoiId";
            if (type == typeof(VmGlobalVariableData)) return "VariableId";
            if (type == typeof(VmExternalResourceData)) return "ResourceId";
            if (type == typeof(VmScriptInterfaceMemberData)) return "MemberId";
            if (type == typeof(VmVariationSampleS130)) return "SampleId";
            if (type == typeof(VmJsonFieldS110)) return "Id";
            if (type == typeof(VmToolInputBindingData)) return "TargetPortName";
            if (type == typeof(VmSubflowPortData)) return "PortName";
            if (type == typeof(ResultLayerStyleRecipeData)) return "LayerKey";
            if (type == typeof(VmProductionWidgetLayoutData)) return "WidgetId";
            if (type == typeof(VmEngineeringPackageFileS139)) return "RelativePath";
            return null;
        }

        private static bool Scalar(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) || type == typeof(Guid);
        }

        private static string ScalarText(object value)
        {
            if (value == null) return "（未设置）";
            if (value is string) return (string)value;
            if (value is double) return ((double)value).ToString("R", Invariant);
            if (value is float) return ((float)value).ToString("R", Invariant);
            if (value is DateTime) return ((DateTime)value).ToString("O", Invariant);
            if (value is DateTimeOffset) return ((DateTimeOffset)value).ToString("O", Invariant);
            return Convert.ToString(value, Invariant);
        }

        private static string Short(string text, int limit)
        {
            text = text ?? "（未设置）";
            if (text.Length <= limit) return text;
            string suffix = "…（共 " + text.Length.ToString(Invariant) + " 字符）";
            return suffix.Length >= limit ? suffix.Substring(0, limit) : text.Substring(0, limit - suffix.Length) + suffix;
        }

        private sealed class Review
        {
            private readonly VisionRecipe currentRecipe, targetRecipe;
            private readonly VmRecipeDiffReportS140 report;
            private readonly List<string> warnings = new List<string>();
            private int objects, fields;
            public Review(VisionRecipe current, VisionRecipe target, VmRecipeDiffReportS140 value)
            { currentRecipe = current; targetRecipe = target; report = value; }

            public void Warn(string text, bool incomplete)
            {
                if (incomplete) report.IsComplete = false;
                if (warnings.Count < 24 && !warnings.Contains(text)) warnings.Add(text);
            }

            public void Add(string change, string area, string owner, string field, string a, string b, bool display, bool unknown, string detail = null)
            {
                if (report.Items.Count >= MaximumItems) throw new ReviewLimitException("差异达到 2000 项预算，后续未展开；报告不完整，不可据此认定无其他变化。");
                string fullA = a ?? "（未设置）", fullB = b ?? "（未设置）";
                string description = (area == "部署/随行声明" ? "随行清单是部署声明元数据，不是算法设置；添加清单不等于算法变化。\n" : "") +
                    "完整技术路径：" + field + "\n" + (detail ?? "稳定对象内的真实定义字段；仅审阅，不自动应用。");
                string combined = description + "\n\n当前：\n" + Short(fullA, MaximumFieldCharacters) + "\n\n目标：\n" + Short(fullB, MaximumFieldCharacters);
                bool truncated = fullA.Length > MaximumFieldCharacters || fullB.Length > MaximumFieldCharacters || combined.Length > MaximumFieldCharacters ||
                    fullA.Contains("详情达到有界展示预算") || fullB.Contains("详情达到有界展示预算");
                if (truncated) Warn("部分源码、数组或字段详情超过 8192 字符，全文未显示；差异身份仍基于完整内容。", true);
                report.Items.Add(new VmRecipeDiffItemS140
                {
                    ChangeText = change, AreaText = display && area != "部署/随行声明" ? "显示/布局" : area, ObjectText = Short(owner, 1024), FieldText = FieldLabel(field),
                    CurrentText = Short(fullA.Replace("\r", " ").Replace("\n", " "), 150),
                    TargetText = Short(fullB.Replace("\r", " ").Replace("\n", " "), 150), IsDisplay = display, IsUnknown = unknown,
                    DetailText = Short(combined, MaximumFieldCharacters)
                });
                if (unknown) Warn("存在无法确定的资源字节或对象映射；未知不等于相同，也不等于就绪。", false);
            }

            public void CompareNode(object a, object b, string path, string owner, string area, bool display, int depth, int subflows)
            {
                if (++objects > MaximumObjects || ++fields > MaximumFields || depth > MaximumObjectDepth)
                    throw new ReviewLimitException("对象/字段/递归预算已用尽；报告不完整。");
                if (subflows > 8) throw new ReviewLimitException("子流程超过 8 层，后续不能完整审阅。");
                if (a == null && b == null) return;
                var type = (a ?? b).GetType();
                if (a != null && b != null && a.GetType() != b.GetType())
                {
                    Add("改变", area, owner, path + "（类型）", a.GetType().Name, b.GetType().Name, display, true); Warn("同一字段出现不同 DTO 类型，映射不完整。", true); return;
                }
                if (Scalar(type))
                {
                    if (!Equals(a, b)) Add(a == null ? "新增" : b == null ? "删除" : "改变", area, owner, path, ScalarText(a), ScalarText(b), display, false);
                    return;
                }
                var list = (a ?? b) as IList;
                if (list != null)
                {
                    Type itemType = type.IsArray ? type.GetElementType() : type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
                    CompareList(a as IList, b as IList, itemType, path, owner, area, display, depth, subflows); return;
                }
                if (type == typeof(VmSubflowData)) subflows++;
                foreach (var member in Members(type))
                {
                    if (RuntimeMember(type, member.Name)) continue;
                    string fieldPath = string.IsNullOrEmpty(path) ? member.Name : path + "." + member.Name;
                    object valueA = a == null ? null : member.Get(a), valueB = b == null ? null : member.Get(b);
                    bool fieldDisplay = display || DisplayMember(type, member.Name);
                    string fieldArea = FieldArea(type, member.Name, area);
                    CompareNode(valueA, valueB, fieldPath, owner, fieldArea, fieldDisplay, depth + 1, subflows);
                    if (!fieldDisplay) ReviewResource(type, member.Name, a, b, valueA, valueB, fieldPath, owner, fieldArea);
                }
            }

            private void CompareList(IList a, IList b, Type type, string path, string owner, string area, bool display, int depth, int subflows)
            {
                int countA = a == null ? 0 : a.Count, countB = b == null ? 0 : b.Count;
                if (countA > MaximumCollectionItems || countB > MaximumCollectionItems) throw new ReviewLimitException("集合超过 100000 项，不能完整审阅。");
                string identity = EntityKey(type);
                if (identity == null)
                {
                    string hashA = HashValue(a, "OrderedValue/v1"), hashB = HashValue(b, "OrderedValue/v1");
                    if (hashA != hashB)
                        Add(a == null ? "新增" : b == null ? "删除" : "改变", area, owner, path,
                            Preview(a), Preview(b), display, false,
                            "该字段是原定义中的有序值集合；比较完整内容与顺序，不把列表位置当稳定对象身份。\n当前内容键：" + hashA + "\n目标内容键：" + hashB);
                    // Collections without identity (e.g. planar correspondence points) are
                    // compared as one ordered value, rather than guessed entity mappings.
                    return;
                }
                var invalid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool badA, badB;
                var mapA = Map(a, type, identity, invalid, out badA);
                var mapB = Map(b, type, identity, invalid, out badB);
                if (badA || badB)
                {
                    Add("未知", area, owner, path + ".身份", "对象 " + countA.ToString(Invariant), "对象 " + countB.ToString(Invariant), display, true,
                        "集合包含空对象、缺失或重复 " + identity + "；受影响身份不映射，也不按名称或索引猜。其他唯一身份仍可审阅。");
                    Warn("缺失/重复对象身份导致部分映射不完整。", true);
                    foreach (string id in invalid) { mapA.Remove(id); mapB.Remove(id); }
                }
                if (!badA && !badB)
                {
                    var sequenceA = Sequence(a, type, identity); var sequenceB = Sequence(b, type, identity);
                    if (!sequenceA.SequenceEqual(sequenceB, StringComparer.OrdinalIgnoreCase))
                        Add("改变", area, owner, path + ".顺序", string.Join(" → ", sequenceA), string.Join(" → ", sequenceB), display, false,
                            type == typeof(ToolFlowRecipeItem) ? "这是正式执行顺序；GraphX/GraphY 与组外观另列，不改变本条语义。" : "这是原定义集合顺序；稳定对象字段另行比较。");
                    if ((a == null) != (b == null) && countA == 0 && countB == 0)
                        Add(a == null ? "新增" : "删除", area, owner, path, a == null ? "未声明集合" : "显式空集合", b == null ? "未声明集合" : "显式空集合", display, false,
                            "显式空 ToolFlow 与缺失 ToolFlow 保留各自兼容语义，不能自动归一化。");
                }
                var keys = Sequence(a, type, identity).Concat(Sequence(b, type, identity)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                foreach (string key in keys)
                {
                    if (invalid.Contains(key)) continue;
                    object valueA, valueB; mapA.TryGetValue(key, out valueA); mapB.TryGetValue(key, out valueB);
                    string childOwner = owner + " / " + ObjectLabel(type, valueA ?? valueB, key);
                    string childPath = path + "[" + key + "]";
                    if (valueA == null || valueB == null)
                        Add(valueA == null ? "新增" : "删除", area, childOwner, "对象", valueA == null ? "不存在" : ObjectLabel(type, valueA, key),
                            valueB == null ? "不存在" : ObjectLabel(type, valueB, key), display, false,
                            "按真实 " + identity + " 对齐；同名而不同身份不会合并。新增/删除对象的实际字段继续展开。");
                    CompareNode(valueA, valueB, childPath, childOwner, ObjectArea(type, area), display, depth + 1, subflows);
                }
            }

            private Dictionary<string, object> Map(IList list, Type type, string identity, HashSet<string> invalid, out bool bad)
            {
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); bad = false;
                var member = Members(type).First(item => item.Name == identity);
                if (list == null) return result;
                foreach (object value in list)
                {
                    string key = value == null ? null : member.Get(value) as string;
                    if (string.IsNullOrWhiteSpace(key)) { bad = true; continue; }
                    if (result.ContainsKey(key)) { bad = true; invalid.Add(key); continue; }
                    result.Add(key, value);
                }
                return result;
            }

            private static string[] Sequence(IList list, Type type, string identity)
            {
                if (list == null) return new string[0];
                var member = Members(type).First(item => item.Name == identity);
                return list.Cast<object>().Where(value => value != null).Select(value => member.Get(value) as string).Where(key => !string.IsNullOrWhiteSpace(key)).ToArray();
            }

            private void ReviewResource(Type type, string member, object a, object b, object valueA, object valueB, string path, string owner, string area)
            {
                if (!ResourceField(type, member)) return;
                string textA = valueA as string, textB = valueB as string;
                if (valueA is IList || valueB is IList)
                {
                    if ((valueA as IList)?.Count > 0 || (valueB as IList)?.Count > 0)
                        Add("未知", "资源字节", owner, path, Preview(valueA), Preview(valueB), false, true,
                            "此显式采样/回放集合没有逐项声明 SHA；配置文字和顺序可以比较，实际图片字节未知。审阅不枚举目录、不哈希文件。");
                    return;
                }
                if (string.IsNullOrWhiteSpace(textA) && string.IsNullOrWhiteSpace(textB)) return;
                string shaA = DeclaredSha(a, member, currentRecipe), shaB = DeclaredSha(b, member, targetRecipe);
                bool unknownA = !string.IsNullOrWhiteSpace(textA) && !IsSha(shaA);
                bool unknownB = !string.IsNullOrWhiteSpace(textB) && !IsSha(shaB);
                if (unknownA || unknownB)
                    Add("未知", "资源字节", owner, path + ".实际字节", string.IsNullOrWhiteSpace(textA) ? "未声明" : IsSha(shaA) ? "已声明 SHA（未现场复核）" : "无有效声明 SHA，实际字节未知",
                        string.IsNullOrWhiteSpace(textB) ? "未声明" : IsSha(shaB) ? "已声明 SHA（未现场复核）" : "无有效声明 SHA，实际字节未知", false, true,
                        "即使路径相同，也仅表示路径文字相同；本服务不打开文件、不加载 DLL，不把缺失 SHA 的同路径资源判为字节相同。\n当前路径：" + Short(textA, 2048) + "\n目标路径：" + Short(textB, 2048));
            }

            public void Finish()
            {
                int added = report.Items.Count(item => item.ChangeText == "新增"), deleted = report.Items.Count(item => item.ChangeText == "删除"),
                    changed = report.Items.Count(item => item.ChangeText == "改变"), unknown = report.Items.Count(item => item.IsUnknown), display = report.Items.Count(item => item.IsDisplay);
                report.SummaryText = string.Format(Invariant, "新增 {0} · 删除 {1} · 改变 {2} · 未知 {3} · 显示/布局 {4}（单列）{5}",
                    added, deleted, changed, unknown, display, report.IsComplete ? "" : " · 报告不完整");
                if (report.Items.Count == 0) report.SummaryText = report.IsComplete ? "已比较定义字段无差异；资源实际字节与运行环境仍未验证。" : "暂未列出可确定差异，但报告不完整，不能认定无变化。";
                report.WarningText = string.Join("\n", warnings);
            }
        }

        private static string FieldArea(Type type, string name, string fallback)
        {
            if (type == typeof(VisionRecipe))
            {
                if (name == "Flows" || name == "ToolFlow") return "流程/执行";
                if (name == "RoiLayers" || name == "SearchRoi") return "ROI/坐标";
                if (name == "GlobalVariables") return "全局变量";
                if (name == "ExternalResources") return "外部资源";
                if (name == "EngineeringPackageS139") return "部署/随行声明";
                if (name.StartsWith("Tcp", StringComparison.Ordinal) || name == "AutoSendResult") return "通讯/触发";
            }
            if (type == typeof(ToolFlowRecipeItem))
            {
                if (name == "InputBindings") return "输入订阅";
                if (name == "RoiIds") return "ROI/坐标";
                if (name == "Parameters") return "工具参数";
                if (name == "NumericJudge") return "数值判定";
            }
            if (type == typeof(VmToolParameterData))
            {
                if (name.StartsWith("Script", StringComparison.Ordinal)) return "脚本/入口";
                if (name.StartsWith("Camera", StringComparison.Ordinal) || name.StartsWith("Planar", StringComparison.Ordinal)) return "相机/标定";
                if (name.StartsWith("Tcp", StringComparison.Ordinal) || name.StartsWith("Modbus", StringComparison.Ordinal) || name.StartsWith("Rtu", StringComparison.Ordinal) || name.StartsWith("Communication", StringComparison.Ordinal)) return "通讯/设备";
                if (name == "Subflow") return "子流程/循环";
            }
            return fallback;
        }

        private static string ObjectArea(Type type, string fallback)
        {
            if (type == typeof(RoiLayerRecipeData)) return "ROI/坐标";
            if (type == typeof(VmGlobalVariableData)) return "全局变量";
            if (type == typeof(VmExternalResourceData)) return "外部资源";
            if (type == typeof(VmEngineeringPackageFileS139)) return "部署/随行声明";
            return fallback;
        }

        private static string FieldLabel(string path)
        {
            string name = path ?? "";
            int dot = name.LastIndexOf('.'); if (dot >= 0) name = name.Substring(dot + 1);
            switch (name)
            {
                case "Name": case "InstanceName": case "DisplayName": return "名称";
                case "IsEnabled": return "启用";
                case "ToolType": return "工具类型";
                case "ToolId": return "模块身份";
                case "FlowId": return "流程身份";
                case "RoiId": return "ROI 身份";
                case "VariableId": return "变量身份";
                case "ResourceId": return "资源身份";
                case "GraphX": return "流程图位置 X";
                case "GraphY": return "流程图位置 Y";
                case "GroupId": case "GroupName": return "流程图分组";
                case "IsGroupCollapsed": return "分组折叠";
                case "IsBreakpoint": return "断点";
                case "IsVisible": return "可见";
                case "IsLocked": return "编辑锁定";
                case "IsMask": return "屏蔽区域";
                case "ImageContextId": return "图像坐标来源";
                case "ShapeType": return "ROI 形状";
                case "Row": case "Row1": case "Row2": return "ROI 行坐标（" + name + "）";
                case "Column": case "Column1": case "Column2": return "ROI 列坐标（" + name + "）";
                case "Radius": return "ROI 半径";
                case "TargetPortName": return "目标输入端口";
                case "SourceToolId": case "InputToolId": return "来源模块";
                case "SourcePortName": case "InputPortName": return "来源输出端口";
                case "LowerLimit": return "判定下限";
                case "UpperLimit": return "判定上限";
                case "Tolerance": return "判定容差";
                case "Operator": return "判定运算";
                case "ValueType": case "DataType": return "数据类型";
                case "DefaultNumberValue": case "DefaultTextValue": case "DefaultBooleanValue": case "DefaultResultCodeValue": case "DefaultArrayJson": return "变量默认值（" + name + "）";
                case "ScriptInlineCode": return "脚本源码";
                case "ScriptTypeName": return "脚本入口类型";
                case "ScriptMethodName": return "脚本入口方法";
                case "ScriptExecutionMode": return "脚本执行方式";
                case "ScriptAssemblyPath": return "脚本 DLL 路径";
                case "ScriptResourceId": return "订阅 DLL 资源";
                case "PlanarCalibrationUnitS124": case "Unit": return "坐标/测量单位";
                case "Sha256": case "ModelSha256": case "ImageSha256": case "DomainSha256": case "CoverageSha256": return "声明 SHA（" + name + "）";
                case "ModelPath": case "ShapeModelPath": case "TemplatePath": case "OcrModelPath": return "模型资源路径（" + name + "）";
                case "LocalImagePath": return "输入图像路径";
                case "CameraSamplePathsS127": return "标定采样文件";
                case "CameraBoardPathS127": return "标定板描述文件";
                case "LoopMode": return "子流程循环模式";
                case "RepeatCount": return "子流程循环次数";
                case "LoopInputPort": return "循环输入端口";
                case "StopOnNg": return "NG 停止策略";
                case "ContinuousIntervalMilliseconds": return "连续运行间隔（ms）";
                case "FlowTimeoutMilliseconds": return "流程超时（ms）";
                case "顺序": return path.Contains("Tools") || path.Contains("ToolFlow") ? "执行顺序" : "定义集合顺序";
                default: return Short(name, 180); // Future fields remain visible, not silently dropped.
            }
        }

        private static string ObjectLabel(Type type, object value, string id)
        {
            string name = null;
            if (value != null)
            {
                var member = Members(type).FirstOrDefault(item => item.Name == "InstanceName" || item.Name == "Name" || item.Name == "DisplayName");
                if (member != null) name = member.Get(value) as string;
            }
            string category = type == typeof(VmProjectFlowData) ? "流程" : type == typeof(ToolFlowRecipeItem) ? "模块" :
                type == typeof(RoiLayerRecipeData) ? "ROI" : type == typeof(VmGlobalVariableData) ? "变量" : type == typeof(VmExternalResourceData) ? "资源" : "定义对象";
            return category + (string.IsNullOrWhiteSpace(name) ? "" : "「" + Short(name, 80) + "」") + " [" + Short(id, 100) + "]";
        }

        private static bool ResourceField(Type type, string name)
        {
            if (type == typeof(VmJsonFieldS110)) return false; // JSON pointer is not a filesystem path.
            if (type == typeof(VisionRecipe)) return name == "TemplatePath" || name == "HDevelopPath";
            if (type == typeof(VmToolParameterData)) return name == "CameraSamplePathsS127" || name == "ImageReplayFolderS114" || name.IndexOf("Path", StringComparison.Ordinal) >= 0;
            return (type == typeof(VmExternalResourceData) || type == typeof(VmVariationSampleS130) || type == typeof(VmVariationParametersS130) ||
                type == typeof(VmNccParametersS131) || type == typeof(VmOcrValidationParametersS133) || type == typeof(VmEngineeringPackageFileS139)) && name.IndexOf("Path", StringComparison.Ordinal) >= 0;
        }

        private static string DeclaredSha(object value, string pathMember, VisionRecipe recipe)
        {
            if (value == null) return null;
            var parameters = value as VmToolParameterData;
            if (parameters != null && pathMember == "OcrModelPath") return parameters.OcrValidationS133?.ModelSha256;
            if (parameters != null && pathMember == "ScriptAssemblyPath" && !string.IsNullOrWhiteSpace(parameters.ScriptResourceId))
            {
                var resources = (recipe.ExternalResources ?? new List<VmExternalResourceData>()).Where(item => item != null &&
                    string.Equals(item.ResourceId, parameters.ScriptResourceId, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
                return resources.Length == 1 ? resources[0].Sha256 : null;
            }
            string shaMember = pathMember.EndsWith("Path", StringComparison.Ordinal) ? pathMember.Substring(0, pathMember.Length - 4) + "Sha256" : "Sha256";
            if (value is VmExternalResourceData || value is VmEngineeringPackageFileS139) shaMember = "Sha256";
            var member = Members(value.GetType()).FirstOrDefault(item => item.Name == shaMember);
            return member == null ? null : member.Get(value) as string;
        }

        private static bool IsSha(string value)
        { return value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'); }

        private static string Preview(object value)
        {
            if (value == null) return "（未设置）";
            var builder = new StringBuilder(); int nodes = 0; bool cut = false;
            PreviewValue(value, builder, 0, ref nodes, ref cut);
            return builder.ToString() + (cut ? "\n…（详情达到有界展示预算；内容键比较仍覆盖全文）" : "");
        }

        private static void PreviewValue(object value, StringBuilder result, int depth, ref int nodes, ref bool cut)
        {
            if (++nodes > 4096 || depth > MaximumObjectDepth || result.Length >= MaximumFieldCharacters) { cut = true; return; }
            if (value == null) { result.Append("null"); return; }
            Type type = value.GetType();
            if (Scalar(type))
            {
                string text = ScalarText(value); int room = MaximumFieldCharacters - result.Length;
                if (text.Length > room) { result.Append(text.Substring(0, Math.Max(0, room))); cut = true; } else result.Append(text);
                return;
            }
            IList list = value as IList;
            if (list != null)
            {
                result.Append("[");
                for (int i = 0; i < list.Count && !cut; i++) { if (i > 0) result.Append(", "); PreviewValue(list[i], result, depth + 1, ref nodes, ref cut); }
                result.Append("]"); return;
            }
            result.Append("{"); bool first = true;
            foreach (var member in Members(type))
            {
                if (RuntimeMember(type, member.Name) || cut) continue;
                if (!first) result.Append(", "); first = false;
                result.Append(member.Name).Append(": "); PreviewValue(member.Get(value), result, depth + 1, ref nodes, ref cut);
            }
            result.Append("}");
        }

        private static string HashValue(object value, string version)
        {
            using (var writer = new ContentHash())
            {
                writer.Text(version); var budget = new HashBudget();
                WriteValue(value, writer, budget, 0, 0, new HashSet<object>(ReferenceComparer.Instance));
                return writer.Finish();
            }
        }

        private static void WriteValue(object value, ContentHash writer, HashBudget budget, int depth, int subflows, HashSet<object> ancestors)
        {
            if (++budget.Objects > MaximumObjects || ++budget.Fields > MaximumFields || depth > MaximumObjectDepth)
                throw new ReviewLimitException("定义对象/字段/深度超出预算，无法建立完整审阅身份。");
            if (value == null) { writer.Text("null"); return; }
            Type type = value.GetType(); writer.Text(type.FullName);
            if (Scalar(type)) { writer.Text(ScalarText(value)); return; }
            if (type == typeof(VmSubflowData) && ++subflows > 8) throw new ReviewLimitException("子流程超过 8 层，无法建立完整审阅身份。");
            if (!ancestors.Add(value)) throw new ReviewLimitException("定义包含循环引用，无法建立完整审阅身份。");
            try
            {
                var list = value as IList;
                if (list != null)
                {
                    if (list.Count > MaximumCollectionItems) throw new ReviewLimitException("定义集合超过 100000 项，无法建立完整审阅身份。");
                    writer.Text(list.Count.ToString(Invariant));
                    foreach (object item in list) WriteValue(item, writer, budget, depth + 1, subflows, ancestors);
                }
                else
                {
                    foreach (var member in Members(type))
                    {
                        if (RuntimeMember(type, member.Name)) continue;
                        writer.Text(member.Name); WriteValue(member.Get(value), writer, budget, depth + 1, subflows, ancestors);
                    }
                }
                writer.Text("end");
            }
            finally { ancestors.Remove(value); }
        }

        private sealed class ContentHash : IDisposable
        {
            private readonly SHA256 sha = SHA256.Create();
            private readonly Encoding utf8 = new UTF8Encoding(false, true);
            private long bytes;
            public void Text(string text)
            {
                text = text ?? "";
                int length = utf8.GetByteCount(text);
                if (bytes + sizeof(int) + length > MaximumDefinitionBytes) throw new ReviewLimitException("定义超过 64 MiB 预算，无法建立完整审阅身份。");
                bytes += sizeof(int) + length;
                byte[] size = { (byte)length, (byte)(length >> 8), (byte)(length >> 16), (byte)(length >> 24) };
                sha.TransformBlock(size, 0, size.Length, size, 0);
                // Strings are already bounded by the definition budget; no serialized
                // graph is allocated, and every byte participates in the identity.
                byte[] data = utf8.GetBytes(text); if (data.Length > 0) sha.TransformBlock(data, 0, data.Length, data, 0);
            }
            public string Finish()
            {
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
            public void Dispose() { sha.Dispose(); }
        }

        private sealed class HashBudget { public int Objects, Fields; }
        private sealed class ContractMember
        {
            private readonly PropertyInfo property; private readonly FieldInfo field;
            public string Name { get; private set; } public int Order { get; private set; }
            public ContractMember(PropertyInfo item, DataMemberAttribute attribute)
            { property = item; Name = string.IsNullOrEmpty(attribute.Name) ? item.Name : attribute.Name; Order = attribute.Order; }
            public ContractMember(FieldInfo item, DataMemberAttribute attribute)
            { field = item; Name = string.IsNullOrEmpty(attribute.Name) ? item.Name : attribute.Name; Order = attribute.Order; }
            public object Get(object value) { return property != null ? property.GetValue(value, null) : field.GetValue(value); }
        }
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }
        private sealed class ReviewLimitException : InvalidOperationException { public ReviewLimitException(string text) : base(text) { } }
    }
}
