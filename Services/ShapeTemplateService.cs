using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class ShapeTemplateService : IDisposable
    {
        public const int MaximumCandidateCountS145 = 1000;
        public const int MaximumPreviewCandidatesS145 = 100;
        private const int MaximumModelContoursS145 = 256;
        private const long MaximumOutputContourPointsS145 = 1000000;
        private HTuple modelId;
        private TemplateDefinition definition;

        public bool HasModel
        {
            get { return modelId != null && modelId.Length > 0; }
        }

        public string TemplatePath { get; private set; }

        // The actual in-memory SDK model generation, not a file hash or authenticity claim.
        public string RuntimeModelVersionS145 { get; private set; }

        public TemplateDefinition Definition
        {
            get { return definition == null ? null : definition.Clone(); }
        }

        public void Train(HImage image, TemplateDefinition templateDefinition)
        {
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            if (templateDefinition == null || templateDefinition.TrainingMask == null)
            {
                throw new InvalidOperationException("Please create a training mask before training.");
            }

            TemplateMatchOptions options = templateDefinition.Options ?? new TemplateMatchOptions();
            Clear();

            HObject reduced = null;
            try
            {
                HOperatorSet.ReduceDomain(image, templateDefinition.TrainingMask, out reduced);
                HOperatorSet.CreateScaledShapeModel(
                    reduced,
                    ResolveNumLevels(options.NumLevels),
                    DegToRad(options.AngleStartDeg),
                    DegToRad(options.AngleExtentDeg),
                    "auto",
                    options.ScaleMin,
                    options.ScaleMax,
                    "auto",
                    "auto",
                    ResolveMetric(options.Metric),
                    "auto",
                    "auto",
                    out modelId);
                definition = templateDefinition.Clone();
                TemplatePath = string.Empty;
                RuntimeModelVersionS145 = Guid.NewGuid().ToString("N");
            }
            finally
            {
                if (reduced != null)
                {
                    reduced.Dispose();
                }
            }
        }

        public void Save(string filePath)
        {
            EnsureModel();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Please choose a template path.", "filePath");
            }

            HOperatorSet.WriteShapeModel(modelId, filePath);
            WriteMetadata(filePath);
            TemplatePath = filePath;
            if (definition != null)
            {
                definition.TemplatePath = filePath;
            }
        }

        public void SaveCopy(string filePath)
        {
            EnsureModel();
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Please choose a template path.", "filePath");
            HOperatorSet.WriteShapeModel(modelId, filePath);
            WriteMetadata(filePath);
            // Export does not change the active resource or definition identity.
        }

        public void Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("Template file does not exist.", filePath);
            }

            Clear();
            HOperatorSet.ReadShapeModel(filePath, out modelId);
            definition = ReadMetadata(filePath);
            TemplatePath = filePath;
            if (definition != null)
            {
                definition.TemplatePath = filePath;
            }
            RuntimeModelVersionS145 = Guid.NewGuid().ToString("N");
        }

        public List<ShapeMatchResult> Match(HImage image, RoiData searchRoi, TemplateMatchOptions options)
        {
            return Match(image, searchRoi == null ? null : searchRoi.Region, options);
        }

        public List<ShapeMatchResult> Match(HImage image, HRegion searchRegion, TemplateMatchOptions options)
        {
            EnsureModel();
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            TemplateMatchOptions resolvedOptions = options ?? new TemplateMatchOptions();
            int requestedMatches = resolvedOptions.MaxMatches;
            double minimumScore = resolvedOptions.MinScore;
            if (requestedMatches < 1 || requestedMatches > MaximumCandidateCountS145)
            {
                throw new InvalidOperationException("Shape 最大匹配数量须为 1–1000；0/全部模式不受支持，不自动截断或改为一个目标。");
            }
            if (!IsFiniteS145(minimumScore) || minimumScore < 0 || minimumScore > 1)
            {
                throw new InvalidOperationException("Shape 最低匹配分数须为 0–1 的有限数。");
            }
            HObject matchImage = image;
            HObject reduced = null;
            HTuple row = null, column = null, angle = null, scale = null, score = null;

            try
            {
                if (resolvedOptions.LimitToSearchRoi && searchRegion != null)
                {
                    HOperatorSet.ReduceDomain(image, searchRegion, out reduced);
                    matchImage = reduced;
                }

                HOperatorSet.FindScaledShapeModel(
                    matchImage,
                    modelId,
                    DegToRad(resolvedOptions.AngleStartDeg),
                    DegToRad(resolvedOptions.AngleExtentDeg),
                    resolvedOptions.ScaleMin,
                    resolvedOptions.ScaleMax,
                    minimumScore,
                    requestedMatches,
                    Clamp(resolvedOptions.MaxOverlap, 0.0, 1.0),
                    ResolveSubPixel(resolvedOptions.SubPixel),
                    0,
                    Clamp(resolvedOptions.Greediness, 0.0, 1.0),
                    out row,
                    out column,
                    out angle,
                    out scale,
                    out score);

                return BuildResults(row, column, angle, scale, score, requestedMatches, minimumScore);
            }
            finally
            {
                foreach (HTuple tuple in new[] { row, column, angle, scale, score })
                {
                    if (tuple != null) tuple.Dispose();
                }
                if (reduced != null)
                {
                    reduced.Dispose();
                }
            }
        }

        public void DisplayMatchContours(HWindow window, IEnumerable<ShapeMatchResult> matches)
        {
            EnsureModel();
            List<ShapeMatchResult> frozen = FreezeContourMatchesS145(matches);
            int previewCount = Math.Min(frozen.Count, MaximumPreviewCandidatesS145);
            if (previewCount == 0) return;
            HObject contours = null;
            HObject transformed = null;
            try
            {
                HOperatorSet.GetShapeModelContours(out contours, modelId, 1);
                ValidateContourBudgetS145(contours, previewCount);
                // Display only. CreateMatchContours keeps every formal candidate.
                for (int index = 0; index < previewCount; index++)
                {
                    ShapeMatchResult match = frozen[index];
                    HTuple homMat2D = null, scaledHomMat2D = null;
                    try
                    {
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, match.Row, match.Column, DegToRad(match.AngleDeg), out homMat2D);
                        HOperatorSet.HomMat2dScale(homMat2D, match.Scale, match.Scale, match.Row, match.Column, out scaledHomMat2D);
                        HOperatorSet.AffineTransContourXld(contours, out transformed, scaledHomMat2D);
                        window.DispObj(transformed);
                    }
                    finally
                    {
                        if (transformed != null) { transformed.Dispose(); transformed = null; }
                        if (homMat2D != null) homMat2D.Dispose();
                        if (scaledHomMat2D != null) scaledHomMat2D.Dispose();
                    }
                }
            }
            finally
            {
                if (transformed != null)
                {
                    transformed.Dispose();
                }

                if (contours != null)
                {
                    contours.Dispose();
                }
            }
        }

        public void Clear()
        {
            RuntimeModelVersionS145 = null;
            if (HasModel)
            {
                HOperatorSet.ClearShapeModel(modelId);
            }

            modelId = null;
            if (definition != null)
            {
                definition.Dispose();
                definition = null;
            }

            TemplatePath = string.Empty;
        }

        public void Dispose()
        {
            Clear();
        }

        public HObject CreateMatchContours(IEnumerable<ShapeMatchResult> matches)
        {
            EnsureModel();
            List<ShapeMatchResult> frozen = FreezeContourMatchesS145(matches);
            HObject modelContours = null;
            HObject accumulated = null;
            try
            {
                if (frozen.Count == 0)
                {
                    HOperatorSet.GenEmptyObj(out accumulated);
                    HObject empty = accumulated;
                    accumulated = null;
                    return empty;
                }
                HOperatorSet.GetShapeModelContours(out modelContours, modelId, 1);
                ValidateContourBudgetS145(modelContours, frozen.Count);
                foreach (ShapeMatchResult match in frozen)
                {
                    HTuple rigid = null, scaled = null;
                    HObject transformed = null;
                    HObject combined = null;
                    try
                    {
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, match.Row, match.Column, DegToRad(match.AngleDeg), out rigid);
                        HOperatorSet.HomMat2dScale(rigid, match.Scale, match.Scale, match.Row, match.Column, out scaled);
                        HOperatorSet.AffineTransContourXld(modelContours, out transformed, scaled);
                        if (accumulated == null)
                        {
                            HOperatorSet.CopyObj(transformed, out accumulated, 1, -1);
                        }
                        else
                        {
                            HOperatorSet.ConcatObj(accumulated, transformed, out combined);
                            accumulated.Dispose();
                            accumulated = combined;
                            combined = null;
                        }
                    }
                    finally
                    {
                        if (combined != null) combined.Dispose();
                        if (transformed != null) transformed.Dispose();
                        if (rigid != null) rigid.Dispose();
                        if (scaled != null) scaled.Dispose();
                    }
                }
                if (accumulated == null) HOperatorSet.GenEmptyObj(out accumulated);
                HObject result = accumulated;
                accumulated = null;
                return result;
            }
            finally
            {
                if (accumulated != null) accumulated.Dispose();
                if (modelContours != null) modelContours.Dispose();
            }
        }

        private static List<ShapeMatchResult> FreezeContourMatchesS145(IEnumerable<ShapeMatchResult> matches)
        {
            if (matches == null) return new List<ShapeMatchResult>();
            var collection = matches as ICollection<ShapeMatchResult>;
            var readOnly = matches as IReadOnlyCollection<ShapeMatchResult>;
            if (collection == null && readOnly == null)
                throw new InvalidOperationException("Shape 轮廓须使用本次具明确数量的候选列表；不枚举无界候选来源。");
            int count = collection == null ? readOnly.Count : collection.Count;
            if (count < 0 || count > MaximumCandidateCountS145)
                throw new InvalidOperationException("Shape 轮廓候选超过 1000 上限；不复制或截断后冒充完整结果。");
            var frozen = new List<ShapeMatchResult>(count);
            foreach (ShapeMatchResult match in matches)
            {
                if (frozen.Count >= count)
                    throw new InvalidOperationException("Shape 候选列表在冻结期间数量变化，禁止混用结果。");
                if (match == null || !IsFiniteS145(match.Row) || !IsFiniteS145(match.Column) ||
                    !IsFiniteS145(match.AngleDeg) || !IsFiniteS145(DegToRad(match.AngleDeg)) ||
                    !IsFiniteS145(match.Scale) || match.Scale <= 0 || !IsFiniteS145(match.Score))
                    throw new InvalidOperationException("Shape 轮廓候选含无效位置、角度、尺度或分数。");
                frozen.Add(new ShapeMatchResult
                {
                    Index = match.Index, SourceIndex = match.SourceIndex, Row = match.Row, Column = match.Column,
                    AngleDeg = match.AngleDeg, Scale = match.Scale, Score = match.Score,
                    RoiShapeType = match.RoiShapeType, RoiWidth = match.RoiWidth, RoiHeight = match.RoiHeight, RoiRadius = match.RoiRadius
                });
            }
            if (frozen.Count != count)
                throw new InvalidOperationException("Shape 候选列表在冻结期间数量变化，禁止混用结果。");
            return frozen;
        }

        private static void ValidateContourBudgetS145(HObject modelContours, int candidateCount)
        {
            HTuple objectCount = null, pointCounts = null;
            try
            {
                HOperatorSet.CountObj(modelContours, out objectCount);
                if (objectCount == null || objectCount.Length != 1 || objectCount.I < 1 || objectCount.I > MaximumModelContoursS145)
                    throw new InvalidOperationException("Shape 模型轮廓须为 1–256 个对象，超限不复制或按候选变换。");
                // Local HALCON 20.11 .NET documentation confirms this count-only API.
                // Do not GetContourXld/ToDArr an unbounded point sequence to count it.
                HOperatorSet.ContourPointNumXld(modelContours, out pointCounts);
                if (pointCounts == null || pointCounts.Length != objectCount.I)
                    throw new InvalidOperationException("Shape 模型轮廓点数返回不完整，不能确认图形预算。");
                long totalPoints = 0;
                for (int index = 0; index < pointCounts.Length; index++)
                {
                    double points = pointCounts[index].D;
                    if (!IsFiniteS145(points) || points < 1 || points != Math.Floor(points) || points > MaximumOutputContourPointsS145)
                        throw new InvalidOperationException("Shape 模型轮廓点数无效或超过 1000000 上限。");
                    totalPoints += (long)points;
                    if (totalPoints > MaximumOutputContourPointsS145 || totalPoints * candidateCount > MaximumOutputContourPointsS145)
                        throw new InvalidOperationException("Shape 全部候选轮廓总点数超过 1000000；正式 XLD 不截取前100或部分轮廓。");
                }
            }
            finally
            {
                if (objectCount != null) objectCount.Dispose();
                if (pointCounts != null) pointCounts.Dispose();
            }
        }

        private List<ShapeMatchResult> BuildResults(HTuple row, HTuple column, HTuple angle, HTuple scale, HTuple score,
            int requestedMatches, double minimumScore)
        {
            // Validate every HALCON tuple before allocating a copied list/array.
            if (row == null || column == null || angle == null || scale == null || score == null ||
                row.Length != column.Length || row.Length != angle.Length || row.Length != scale.Length || row.Length != score.Length ||
                row.Length > requestedMatches || row.Length > MaximumCandidateCountS145)
            {
                throw new InvalidOperationException("HALCON Shape 候选输出缺失、长度不一致或超过请求/1000 上限；不截断后冒充完整结果。");
            }
            int count = row.Length;
            for (int index = 0; index < count; index++)
            {
                double r = row[index].D, c = column[index].D, a = angle[index].D, s = scale[index].D, quality = score[index].D;
                if (!IsFiniteS145(r) || !IsFiniteS145(c) || !IsFiniteS145(a) || !IsFiniteS145(RadToDeg(a)) ||
                    !IsFiniteS145(s) || s <= 0 || !IsFiniteS145(quality) || quality < minimumScore - 1e-7 || quality > 1 + 1e-7)
                {
                    throw new InvalidOperationException("HALCON Shape 返回非有限或不合法候选；不发布部分列表、零值或旧结果。");
                }
            }
            List<ShapeMatchResult> results = new List<ShapeMatchResult>(count);
            RoiData displayFrame = definition == null ? null : definition.DisplayFrame;

            for (int i = 0; i < count; i++)
            {
                results.Add(new ShapeMatchResult
                {
                    SourceIndex = i,
                    Row = row[i].D,
                    Column = column[i].D,
                    AngleDeg = RadToDeg(angle[i].D),
                    Scale = scale[i].D,
                    Score = score[i].D,
                    RoiShapeType = displayFrame == null ? (RoiShapeType?)null : displayFrame.ShapeType,
                    RoiWidth = displayFrame == null ? 0 : Math.Abs(displayFrame.Column2 - displayFrame.Column1),
                    RoiHeight = displayFrame == null ? 0 : Math.Abs(displayFrame.Row2 - displayFrame.Row1),
                    RoiRadius = displayFrame == null ? 0 : displayFrame.Radius
                });
            }

            List<ShapeMatchResult> ordered = new List<ShapeMatchResult>(results);
            ordered.Sort(delegate(ShapeMatchResult left, ShapeMatchResult right)
            {
                int scoreOrder = right.Score.CompareTo(left.Score);
                if (scoreOrder != 0) return scoreOrder;
                int rowOrder = left.Row.CompareTo(right.Row);
                if (rowOrder != 0) return rowOrder;
                int columnOrder = left.Column.CompareTo(right.Column);
                return columnOrder != 0 ? columnOrder : left.SourceIndex.CompareTo(right.SourceIndex);
            });
            for (int index = 0; index < ordered.Count; index++) ordered[index].Index = index + 1;
            return ordered;
        }

        private static bool IsFiniteS145(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void EnsureModel()
        {
            if (!HasModel)
            {
                throw new InvalidOperationException("Please train or load a template first.");
            }
        }

        private static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        private static double RadToDeg(double rad)
        {
            return rad * 180.0 / Math.PI;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static string ResolveNumLevels(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "auto" : value.Trim();
        }

        private static string ResolveMetric(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "use_polarity" : value.Trim();
        }

        private static string ResolveSubPixel(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "least_squares" : value.Trim();
        }

        private static string MetadataPath(string modelPath)
        {
            return modelPath + ".meta";
        }

        private static string MaskPath(string modelPath)
        {
            return modelPath + ".mask.reg";
        }

        private static string RoiSidecarPath(string modelPath)
        {
            return modelPath + ".roi";
        }

        private void WriteMetadata(string modelPath)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.TrainingMask != null)
            {
                HOperatorSet.WriteRegion(definition.TrainingMask, MaskPath(modelPath));
            }

            List<string> lines = new List<string>();
            TemplateMatchOptions options = definition.Options ?? new TemplateMatchOptions();
            RoiData frame = definition.DisplayFrame;
            RoiData templateRoi = definition.TemplateRoi;

            lines.Add("version=" + TemplateDefinition.CurrentMetadataVersion.ToString(CultureInfo.InvariantCulture));
            lines.Add("templateName=" + (definition.TemplateName ?? string.Empty));
            lines.Add("mask=" + Path.GetFileName(MaskPath(modelPath)));
            WriteOption(lines, "minScore", options.MinScore);
            WriteOption(lines, "maxMatches", options.MaxMatches);
            WriteOption(lines, "angleStartDeg", options.AngleStartDeg);
            WriteOption(lines, "angleExtentDeg", options.AngleExtentDeg);
            WriteOption(lines, "scaleMin", options.ScaleMin);
            WriteOption(lines, "scaleMax", options.ScaleMax);
            WriteOption(lines, "maxOverlap", options.MaxOverlap);
            WriteOption(lines, "greediness", options.Greediness);
            lines.Add("numLevels=" + (options.NumLevels ?? "auto"));
            lines.Add("metric=" + (options.Metric ?? "use_polarity"));
            lines.Add("subPixel=" + (options.SubPixel ?? "least_squares"));
            lines.Add("limitToSearchRoi=" + options.LimitToSearchRoi.ToString(CultureInfo.InvariantCulture));

            if (frame != null)
            {
                lines.Add("frameType=" + frame.ShapeType);
                WritePolygon(lines, "frame", frame);
                WriteOption(lines, "frameRow1", frame.Row1);
                WriteOption(lines, "frameColumn1", frame.Column1);
                WriteOption(lines, "frameRow2", frame.Row2);
                WriteOption(lines, "frameColumn2", frame.Column2);
                WriteOption(lines, "frameRow", frame.Row);
                WriteOption(lines, "frameColumn", frame.Column);
                WriteOption(lines, "frameRadius", frame.Radius);
            }

            if (templateRoi != null)
            {
                lines.Add("templateRoiType=" + templateRoi.ShapeType);
                WritePolygon(lines, "templateRoi", templateRoi);
                WriteOption(lines, "templateRoiRow1", templateRoi.Row1);
                WriteOption(lines, "templateRoiColumn1", templateRoi.Column1);
                WriteOption(lines, "templateRoiRow2", templateRoi.Row2);
                WriteOption(lines, "templateRoiColumn2", templateRoi.Column2);
                WriteOption(lines, "templateRoiRow", templateRoi.Row);
                WriteOption(lines, "templateRoiColumn", templateRoi.Column);
                WriteOption(lines, "templateRoiRadius", templateRoi.Radius);
            }

            File.WriteAllLines(MetadataPath(modelPath), lines.ToArray());
            WriteLegacyRoiSidecar(modelPath, frame);
        }

        private static void WriteOption(List<string> lines, string key, double value)
        {
            lines.Add(key + "=" + value.ToString(CultureInfo.InvariantCulture));
        }

        private static void WritePolygon(List<string> lines, string prefix, RoiData roi)
        {
            if (roi == null || roi.ShapeType != RoiShapeType.Polygon || roi.PolygonRows == null || roi.PolygonColumns == null)
            {
                return;
            }

            lines.Add(prefix + "Rows=" + string.Join(",", Array.ConvertAll(roi.PolygonRows, item => item.ToString(CultureInfo.InvariantCulture))));
            lines.Add(prefix + "Columns=" + string.Join(",", Array.ConvertAll(roi.PolygonColumns, item => item.ToString(CultureInfo.InvariantCulture))));
        }

        private static TemplateDefinition ReadMetadata(string modelPath)
        {
            string metadataPath = MetadataPath(modelPath);
            if (!File.Exists(metadataPath))
            {
                RoiData legacyRoi = ReadLegacyRoiSidecar(modelPath);
                if (legacyRoi == null)
                {
                    return null;
                }

                return new TemplateDefinition
                {
                    TrainingMask = TemplateDefinition.CloneRegion(legacyRoi.Region),
                    TemplateRoi = legacyRoi.Clone(),
                    DisplayFrame = legacyRoi,
                    Options = new TemplateMatchOptions(),
                    TemplatePath = modelPath
                };
            }

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(metadataPath))
            {
                int index = line.IndexOf('=');
                if (index <= 0)
                {
                    continue;
                }

                values[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
            }

            TemplateDefinition result = new TemplateDefinition();
            result.MetadataVersion = ReadInt(values, "version", TemplateDefinition.CurrentMetadataVersion);
            result.TemplateName = ReadString(values, "templateName", string.Empty);
            result.Options = new TemplateMatchOptions
            {
                MinScore = ReadDouble(values, "minScore", 0.60),
                MaxMatches = ReadInt(values, "maxMatches", 1),
                AngleStartDeg = ReadDouble(values, "angleStartDeg", -180),
                AngleExtentDeg = ReadDouble(values, "angleExtentDeg", 360),
                ScaleMin = ReadDouble(values, "scaleMin", 1.0),
                ScaleMax = ReadDouble(values, "scaleMax", 1.0),
                MaxOverlap = ReadDouble(values, "maxOverlap", 0.5),
                Greediness = ReadDouble(values, "greediness", 0.9),
                NumLevels = ReadString(values, "numLevels", "auto"),
                Metric = ReadString(values, "metric", "use_polarity"),
                SubPixel = ReadString(values, "subPixel", "least_squares"),
                LimitToSearchRoi = ReadBool(values, "limitToSearchRoi", false)
            };
            result.TemplateRoi = ReadRoi(values, "templateRoi");
            result.DisplayFrame = ReadFrame(values);
            if (result.TemplateRoi == null && result.DisplayFrame != null)
            {
                result.TemplateRoi = result.DisplayFrame.Clone();
            }

            string maskFile = ReadString(values, "mask", string.Empty);
            string resolvedMask = string.IsNullOrWhiteSpace(maskFile)
                ? MaskPath(modelPath)
                : Path.Combine(Path.GetDirectoryName(modelPath) ?? string.Empty, maskFile);
            if (File.Exists(resolvedMask))
            {
                HObject maskObject;
                HOperatorSet.ReadRegion(out maskObject, resolvedMask);
                result.TrainingMask = new HRegion(maskObject);
            }
            else if (result.DisplayFrame != null)
            {
                result.TrainingMask = TemplateDefinition.CloneRegion(result.DisplayFrame.Region);
            }

            result.TemplatePath = modelPath;
            return result;
        }

        private static RoiData ReadFrame(Dictionary<string, string> values)
        {
            return ReadRoi(values, "frame");
        }

        private static RoiData ReadRoi(Dictionary<string, string> values, string prefix)
        {
            string type = ReadString(values, prefix + "Type", string.Empty);
            if (string.IsNullOrWhiteSpace(type))
            {
                return null;
            }

            if (string.Equals(type, RoiShapeType.Circle.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return RoiData.CreateCircle(
                    ReadDouble(values, prefix + "Row", 0),
                    ReadDouble(values, prefix + "Column", 0),
                    Math.Max(1, ReadDouble(values, prefix + "Radius", 1)));
            }

            if (string.Equals(type, RoiShapeType.Polygon.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                double[] rows = ReadDoubleArray(values, prefix + "Rows");
                double[] columns = ReadDoubleArray(values, prefix + "Columns");
                if (rows.Length >= 3 && rows.Length == columns.Length)
                {
                    return RoiData.CreatePolygon(rows, columns);
                }
            }

            return RoiData.CreateRectangle(
                ReadDouble(values, prefix + "Row1", 0),
                ReadDouble(values, prefix + "Column1", 0),
                ReadDouble(values, prefix + "Row2", 0),
                ReadDouble(values, prefix + "Column2", 0));
        }

        private static string ReadString(Dictionary<string, string> values, string key, string fallback)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : fallback;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int fallback)
        {
            int value;
            return int.TryParse(ReadString(values, key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : fallback;
        }

        private static double[] ReadDoubleArray(Dictionary<string, string> values, string key)
        {
            string text = ReadString(values, key, string.Empty);
            if (string.IsNullOrWhiteSpace(text))
            {
                return new double[0];
            }

            string[] parts = text.Split(',');
            List<double> result = new List<double>();
            foreach (string part in parts)
            {
                double value;
                if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    result.Add(value);
                }
            }

            return result.ToArray();
        }

        private static double ReadDouble(Dictionary<string, string> values, string key, double fallback)
        {
            double value;
            return double.TryParse(ReadString(values, key, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : fallback;
        }

        private static bool ReadBool(Dictionary<string, string> values, string key, bool fallback)
        {
            bool value;
            return bool.TryParse(ReadString(values, key, string.Empty), out value) ? value : fallback;
        }

        private static void WriteLegacyRoiSidecar(string modelPath, RoiData frame)
        {
            if (frame == null)
            {
                return;
            }

            string text = string.Join("|", new[]
            {
                frame.ShapeType.ToString(),
                frame.Row1.ToString(CultureInfo.InvariantCulture),
                frame.Column1.ToString(CultureInfo.InvariantCulture),
                frame.Row2.ToString(CultureInfo.InvariantCulture),
                frame.Column2.ToString(CultureInfo.InvariantCulture),
                frame.Row.ToString(CultureInfo.InvariantCulture),
                frame.Column.ToString(CultureInfo.InvariantCulture),
                frame.Radius.ToString(CultureInfo.InvariantCulture)
            });
            File.WriteAllText(RoiSidecarPath(modelPath), text);
        }

        private static RoiData ReadLegacyRoiSidecar(string modelPath)
        {
            string sidecar = RoiSidecarPath(modelPath);
            if (!File.Exists(sidecar))
            {
                return null;
            }

            string[] parts = File.ReadAllText(sidecar).Split('|');
            if (parts.Length < 8)
            {
                return null;
            }

            double row1 = ParseDouble(parts[1]);
            double column1 = ParseDouble(parts[2]);
            double row2 = ParseDouble(parts[3]);
            double column2 = ParseDouble(parts[4]);
            double row = ParseDouble(parts[5]);
            double column = ParseDouble(parts[6]);
            double radius = ParseDouble(parts[7]);

            return string.Equals(parts[0], RoiShapeType.Circle.ToString(), StringComparison.OrdinalIgnoreCase)
                ? RoiData.CreateCircle(row, column, radius)
                : RoiData.CreateRectangle(row1, column1, row2, column2);
        }

        private static double ParseDouble(string text)
        {
            double value;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
        }
    }
}
