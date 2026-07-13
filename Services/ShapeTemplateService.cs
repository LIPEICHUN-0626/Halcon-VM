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
        private HTuple modelId;
        private TemplateDefinition definition;

        public bool HasModel
        {
            get { return modelId != null && modelId.Length > 0; }
        }

        public string TemplatePath { get; private set; }

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
                HOperatorSet.CreateShapeModel(
                    reduced,
                    ResolveNumLevels(options.NumLevels),
                    DegToRad(options.AngleStartDeg),
                    DegToRad(options.AngleExtentDeg),
                    "auto",
                    "auto",
                    ResolveMetric(options.Metric),
                    "auto",
                    "auto",
                    out modelId);
                definition = templateDefinition.Clone();
                TemplatePath = string.Empty;
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
        }

        public List<ShapeMatchResult> Match(HImage image, RoiData searchRoi, TemplateMatchOptions options)
        {
            EnsureModel();
            if (image == null)
            {
                throw new InvalidOperationException("No image is loaded.");
            }

            TemplateMatchOptions resolvedOptions = options ?? new TemplateMatchOptions();
            HObject matchImage = image;
            HObject reduced = null;

            try
            {
                if (resolvedOptions.LimitToSearchRoi && searchRoi != null)
                {
                    HOperatorSet.ReduceDomain(image, searchRoi.Region, out reduced);
                    matchImage = reduced;
                }

                HTuple row;
                HTuple column;
                HTuple angle;
                HTuple score;

                HOperatorSet.FindShapeModel(
                    matchImage,
                    modelId,
                    DegToRad(resolvedOptions.AngleStartDeg),
                    DegToRad(resolvedOptions.AngleExtentDeg),
                    resolvedOptions.MinScore,
                    Math.Max(1, resolvedOptions.MaxMatches),
                    Clamp(resolvedOptions.MaxOverlap, 0.0, 1.0),
                    ResolveSubPixel(resolvedOptions.SubPixel),
                    0,
                    Clamp(resolvedOptions.Greediness, 0.0, 1.0),
                    out row,
                    out column,
                    out angle,
                    out score);

                return BuildResults(row, column, angle, score);
            }
            finally
            {
                if (reduced != null)
                {
                    reduced.Dispose();
                }
            }
        }

        public void DisplayMatchContours(HWindow window, IEnumerable<ShapeMatchResult> matches)
        {
            EnsureModel();

            HObject contours = null;
            HObject transformed = null;
            try
            {
                HOperatorSet.GetShapeModelContours(out contours, modelId, 1);
                foreach (ShapeMatchResult match in matches)
                {
                    HTuple homMat2D;
                    HOperatorSet.VectorAngleToRigid(0, 0, 0, match.Row, match.Column, DegToRad(match.AngleDeg), out homMat2D);
                    HOperatorSet.AffineTransContourXld(contours, out transformed, homMat2D);
                    window.DispObj(transformed);
                    transformed.Dispose();
                    transformed = null;
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

        private List<ShapeMatchResult> BuildResults(HTuple row, HTuple column, HTuple angle, HTuple score)
        {
            double[] rows = row.ToDArr();
            double[] columns = column.ToDArr();
            double[] angles = angle.ToDArr();
            double[] scores = score.ToDArr();
            List<ShapeMatchResult> results = new List<ShapeMatchResult>();
            RoiData displayFrame = definition == null ? null : definition.DisplayFrame;

            for (int i = 0; i < scores.Length; i++)
            {
                results.Add(new ShapeMatchResult
                {
                    Row = rows[i],
                    Column = columns[i],
                    AngleDeg = RadToDeg(angles[i]),
                    Score = scores[i],
                    RoiShapeType = displayFrame == null ? (RoiShapeType?)null : displayFrame.ShapeType,
                    RoiWidth = displayFrame == null ? 0 : Math.Abs(displayFrame.Column2 - displayFrame.Column1),
                    RoiHeight = displayFrame == null ? 0 : Math.Abs(displayFrame.Row2 - displayFrame.Row1),
                    RoiRadius = displayFrame == null ? 0 : displayFrame.Radius
                });
            }

            return results;
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
            return modelPath + ".mask";
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
