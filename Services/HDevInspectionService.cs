using System;
using System.Globalization;
using System.IO;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class HDevInspectionService : IDisposable
    {
        private readonly HDevEngine engine = new HDevEngine();

        public HDevInspectionResult RunInspection(string programPath, string procedureName, HImage image, RoiData roi)
        {
            return RunInspection(programPath, procedureName, image, roi == null ? null : roi.Region);
        }

        public HDevInspectionResult RunInspection(string programPath, string procedureName, HImage image, HRegion roiRegion)
        {
            if (string.IsNullOrWhiteSpace(programPath))
            {
                throw new ArgumentException("请选择 HDevelop 程序文件。", "programPath");
            }

            if (!File.Exists(programPath))
            {
                throw new FileNotFoundException("HDevelop 程序文件不存在。", programPath);
            }

            if (image == null)
            {
                throw new InvalidOperationException("当前没有图片。");
            }

            if (roiRegion == null)
            {
                throw new InvalidOperationException("请先设置 ROI。");
            }

            string resolvedProcedure = string.IsNullOrWhiteSpace(procedureName) ? "RunInspection" : procedureName.Trim();
            string programDirectory = Path.GetDirectoryName(programPath);
            if (!string.IsNullOrWhiteSpace(programDirectory))
            {
                engine.SetEngineAttribute("procedure_path", programDirectory);
            }

            HDevProgram program = null;
            HDevProcedure procedure = null;
            HDevProcedureCall call = null;

            try
            {
                if (string.Equals(Path.GetExtension(programPath), ".hdvp", StringComparison.OrdinalIgnoreCase))
                {
                    procedure = new HDevProcedure(programPath);
                }
                else
                {
                    program = new HDevProgram(programPath);
                    procedure = new HDevProcedure(program, resolvedProcedure);
                }

                call = procedure.CreateCall();
                call.SetInputIconicParamObject("Image", image);
                call.SetInputIconicParamObject("ROI", roiRegion);
                call.Execute();

                string resultCode = ReadControlString(call, "ResultCode", "UNKNOWN");
                double score = ReadControlDouble(call, "Score", 0.0);
                string message = ReadControlString(call, "Message", string.Empty);

                return new HDevInspectionResult(resultCode, score, message);
            }
            finally
            {
                if (call != null)
                {
                    call.Dispose();
                }

                if (procedure != null)
                {
                    procedure.Dispose();
                }

                if (program != null)
                {
                    program.Dispose();
                }
            }
        }

        public void Dispose()
        {
            engine.Dispose();
        }

        private static string ReadControlString(HDevProcedureCall call, string name, string fallback)
        {
            try
            {
                HTuple value = call.GetOutputCtrlParamTuple(name);
                if (value == null || value.Length == 0)
                {
                    return fallback;
                }

                return value.ToString();
            }
            catch (HDevEngineException)
            {
                return fallback;
            }
            catch (HalconException)
            {
                return fallback;
            }
        }

        private static double ReadControlDouble(HDevProcedureCall call, string name, double fallback)
        {
            string text = ReadControlString(call, name, null);
            double value;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            return fallback;
        }
    }

    public sealed class HDevInspectionResult
    {
        public HDevInspectionResult(string resultCode, double score, string message)
        {
            ResultCode = resultCode;
            Score = score;
            Message = message;
        }

        public string ResultCode { get; private set; }

        public double Score { get; private set; }

        public string Message { get; private set; }
    }
}
