using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class XlsxExportService
    {
        public void Export(string filePath, IEnumerable<InspectionRecord> records)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (FileStream stream = new FileStream(filePath, FileMode.CreateNew))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "[Content_Types].xml", ContentTypesXml());
                WriteEntry(archive, "_rels/.rels", RootRelationshipsXml());
                WriteEntry(archive, "xl/workbook.xml", WorkbookXml());
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
                WriteEntry(archive, "xl/styles.xml", StylesXml());
                WriteEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(records));
            }
        }

        private static string WorksheetXml(IEnumerable<InspectionRecord> records)
        {
            string[] headers = new[]
            {
                "ID", "Time", "Type", "Result", "Score", "ImageSource", "RoiType", "Template",
                "MatchRow", "MatchColumn", "MatchAngle", "Message"
            };

            StringBuilder rows = new StringBuilder();
            rows.Append(RowXml(1, headers));
            int rowIndex = 2;
            foreach (InspectionRecord record in records)
            {
                rows.Append(RowXml(rowIndex++, new[]
                {
                    record.Id.ToString(),
                    record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    record.InspectionType,
                    record.ResultCode,
                    record.Score.ToString("F3"),
                    record.ImageSource,
                    record.RoiType,
                    record.TemplatePath,
                    FormatNullable(record.MatchRow),
                    FormatNullable(record.MatchColumn),
                    FormatNullable(record.MatchAngle),
                    record.Message
                }));
            }

            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                   "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                   "<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>" +
                   "<sheetFormatPr defaultRowHeight=\"18\"/>" +
                   "<cols><col min=\"1\" max=\"12\" width=\"18\" customWidth=\"1\"/></cols>" +
                   "<sheetData>" + rows + "</sheetData></worksheet>";
        }

        private static string RowXml(int rowIndex, IEnumerable<string> values)
        {
            StringBuilder cells = new StringBuilder();
            int column = 1;
            foreach (string value in values)
            {
                cells.Append(CellXml(ColumnName(column++) + rowIndex, value));
            }

            return "<row r=\"" + rowIndex + "\">" + cells + "</row>";
        }

        private static string CellXml(string reference, string value)
        {
            return "<c r=\"" + reference + "\" t=\"inlineStr\"><is><t>" + XmlEscape(value) + "</t></is></c>";
        }

        private static string ColumnName(int index)
        {
            string name = string.Empty;
            while (index > 0)
            {
                int modulo = (index - 1) % 26;
                name = Convert.ToChar('A' + modulo) + name;
                index = (index - modulo) / 26;
            }

            return name;
        }

        private static string FormatNullable(double? value)
        {
            return value.HasValue ? value.Value.ToString("F3") : string.Empty;
        }

        private static string XmlEscape(string value)
        {
            return System.Security.SecurityElement.Escape(value ?? string.Empty);
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string ContentTypesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                   "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                   "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                   "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                   "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                   "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                   "</Types>";
        }

        private static string RootRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                   "</Relationships>";
        }

        private static string WorkbookXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                   "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                   "<sheets><sheet name=\"Results\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        }

        private static string WorkbookRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                   "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                   "</Relationships>";
        }

        private static string StylesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                   "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                   "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
                   "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                   "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                   "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                   "</styleSheet>";
        }
    }
}
