using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using HalconWinFormsDemo.Models;
using HalconWinFormsDemo.Services;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private void FreezeDefinitionSavedStateS138(VmProductionRunRecordS135 record)
        {
            if (string.IsNullOrWhiteSpace(currentRecipePath))
            { record.DefinitionSaveKnownS138 = true; record.AlgorithmDefinitionWasUnsavedS138 = true; return; }
            try
            {
                record.AlgorithmDefinitionWasUnsavedS138 = !string.Equals(record.AlgorithmDefinitionKeyS138,
                    ProductionDefinitionIdentityS138.ComputeFromSnapshotJson(savedRecipeFingerprint), StringComparison.Ordinal);
                record.DefinitionSaveKnownS138 = true;
            }
            catch { record.DefinitionSaveKnownS138 = false; } // Do not guess a saved state or change the old raw-snapshot fact.
        }

        private string CaptureRuntimeInputsS138(VisionRecipe definition)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(List<VmGlobalVariableData>)).WriteObject(stream, definition.GlobalVariables);
                return "开始时显示图像路径（不等于每个图像源本帧）：" + (currentImagePath ?? string.Empty) +
                    "\n开始时变量值快照：" + Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private void CaptureProductionNumbersS138(VmProductionRunRecordS135 run, VmProductionRunStepS135 step,
            VmToolInstance tool, InspectionRecord inspection, DateTime stepStartedUtc)
        {
            step.NumberValuesS138 = new List<VmProductionNumberValueS138>();
            int remaining = Math.Max(0, 512 - run.CapturedSteps.Sum(s => s.NumberValuesS138 == null ? 0 : s.NumberValuesS138.Count));
            var ports = ToolMetadata.GetInstanceNumericOutputPorts(tool);
            bool fresh = inspection != null && (inspection.ResultCode == "OK" || inspection.ResultCode == "NG") &&
                string.IsNullOrWhiteSpace(tool.ErrorMessage) && tool.LastOutputUtc.HasValue && tool.LastOutputUtc.Value >= stepStartedUtc;
            foreach (var port in ports.Take(Math.Min(32, remaining)))
            {
                object raw; double number = 0;
                bool valid = fresh && tool.TryGetOutputValue(port.PortName, out raw) && TryFreezeNumberS138(raw, out number);
                var value = new VmProductionNumberValueS138
                {
                    PortName = port.PortName, DisplayName = port.DisplayName ?? port.PortName,
                    Unit = FreezeNumberUnitS138(tool, port.PortName, fresh), Semantic = tool.Kind + "/" + port.PortName,
                    State = valid ? "Valid" : "Missing", Value = valid ? (double?)number : null
                };
                step.NumberValuesS138.Add(value);
            }
            if (ports.Count > step.NumberValuesS138.Count) { step.NumbersTruncatedS138 = true; run.NumbersTruncatedS138 = true; }
        }

        private static bool TryFreezeNumberS138(object raw, out double value)
        {
            value = 0;
            if (!(raw is double || raw is float || raw is decimal || raw is byte || raw is sbyte || raw is short ||
                raw is ushort || raw is int || raw is uint || raw is long || raw is ulong)) return false;
            try
            {
                value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                if (double.IsInfinity(value) || double.IsNaN(value)) return false;
                // Do not silently round a typed integral/decimal output into a different historical measurement.
                if (!(raw is double || raw is float) && Convert.ToDecimal(value, CultureInfo.InvariantCulture) != Convert.ToDecimal(raw, CultureInfo.InvariantCulture)) return false;
                return true;
            }
            catch (OverflowException) { return false; }
        }

        private static string FreezeNumberUnitS138(VmToolInstance tool, string port, bool fresh)
        {
            // Only explicit contracts read in S124/S125/S131 are declared; never infer unit from display names.
            if (tool.Kind == VmToolKind.WorldPointPairMeasurement)
            {
                if (port == "DirectionAngle") return "deg";
                if (port == "WorldDistance" || port == "DeltaX" || port == "DeltaY")
                { object unit; return fresh && tool.TryGetOutputValue("Unit", out unit) && unit is string ? (string)unit : string.Empty; }
            }
            if (tool.Kind == VmToolKind.NccTemplateMatch)
            {
                if (port == "CenterRow" || port == "CenterColumn" || port == "ReferenceRow" || port == "ReferenceColumn" || port == "TemplateWidth" || port == "TemplateHeight") return "px";
                if (port == "AngleDegrees" || port == "ReferenceAngleDegrees") return "deg";
                if (port == "TargetCount" || port == "MatchCount") return "count";
                if (port == "SelectedIndex") return "index(0-based)";
                if (port == "SelectedScale") return "ratio";
            }
            if (tool.Kind == VmToolKind.ShapeMatch)
            {
                if (port == "TargetCount" || port == "MatchCount") return "count";
                if (port == "SelectedIndex") return "index(0-based)";
                if (port == "SelectedScale" || port == "Scale") return "ratio";
            }
            if (tool.Kind == VmToolKind.ImageEdgeLineFindingS143)
            {
                if (port == "AngleDegrees") return "deg";
                if (port == "Length" || port == "MeanResidual" || port == "MaxResidual") return "px";
                if (port == "Coverage") return "ratio";
                if (port == "ValidCount" || port == "SampledCount" || port == "FailedCount" || port == "RejectedCount") return "count";
            }
            if (tool.Kind == VmToolKind.ConnectedObjectAnalysisS144)
            {
                if (port == "TotalArea" || port == "SelectedArea") return "px²";
                if (port == "Count" || port == "ConnectedCount") return "count";
                if (port == "SelectedIndex") return "index(0-based)";
            }
            return string.Empty;
        }

        private void SetCurrentTrendIdentityS138(ProductionRunHistoryWindowS135 window)
        {
            try { window.SetCurrentTrendIdentityS138(activeProjectFlowIdS104, ProductionDefinitionIdentityS138.Compute(CaptureRecipe()), ProductionDefinitionIdentityS138.Version); }
            catch (Exception ex) { window.SetCurrentTrendIdentityS138(activeProjectFlowIdS104, string.Empty, 0); window.SetStatus("当前稳定定义无法冻结，仅可显式选择历史身份：" + ex.Message); }
        }
    }
}
