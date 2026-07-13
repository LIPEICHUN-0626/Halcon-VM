using System.Collections.Generic;
using System.IO;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class CsvExportService
    {
        public void Export(string filePath, IEnumerable<InspectionRecord> records)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("ID,Time,Type,Result,Score,ImageSource,RoiType,Template,MatchRow,MatchColumn,MatchAngle,Message");
                foreach (InspectionRecord record in records)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Escape(record.Id.ToString()),
                        Escape(record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                        Escape(record.InspectionType),
                        Escape(record.ResultCode),
                        Escape(record.Score.ToString("F3")),
                        Escape(record.ImageSource),
                        Escape(record.RoiType),
                        Escape(record.TemplatePath),
                        Escape(FormatNullable(record.MatchRow)),
                        Escape(FormatNullable(record.MatchColumn)),
                        Escape(FormatNullable(record.MatchAngle)),
                        Escape(record.Message)
                    }));
                }
            }
        }

        private static string FormatNullable(double? value)
        {
            return value.HasValue ? value.Value.ToString("F3") : string.Empty;
        }

        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            if (value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
            {
                value = "\"" + value + "\"";
            }

            return value;
        }
    }
}
