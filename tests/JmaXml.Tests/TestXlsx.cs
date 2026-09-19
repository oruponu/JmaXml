using System.IO.Compression;
using System.Security;
using System.Text;

namespace JmaXml.Tests;

internal static class TestXlsx
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static MemoryStream Build(IReadOnlyDictionary<string, string[][]> sheets)
    {
        var shared = new List<string>();
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var names = sheets.Keys.ToList();
            Add(zip, "xl/workbook.xml",
                $"<workbook xmlns=\"{Main}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>"
                + string.Concat(names.Select((n, i) => $"<sheet name=\"{n}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>"))
                + "</sheets></workbook>");
            Add(zip, "xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + string.Concat(names.Select((_, i) => $"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>"))
                + "<Relationship Id=\"rId99\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/></Relationships>");
            for (var s = 0; s < names.Count; s++)
            {
                var rows = sheets[names[s]];
                var sb = new StringBuilder($"<worksheet xmlns=\"{Main}\"><sheetData>");
                for (var r = 0; r < rows.Length; r++)
                {
                    sb.Append($"<row r=\"{r + 1}\">");
                    for (var c = 0; c < rows[r].Length; c++)
                    {
                        if (rows[r][c].Length == 0) continue;
                        shared.Add(rows[r][c]);
                        sb.Append($"<c r=\"{Column(c)}{r + 1}\" t=\"s\"><v>{shared.Count - 1}</v></c>");
                    }
                    sb.Append("</row>");
                }
                sb.Append("</sheetData></worksheet>");
                Add(zip, $"xl/worksheets/sheet{s + 1}.xml", sb.ToString());
            }
            Add(zip, "xl/sharedStrings.xml",
                $"<sst xmlns=\"{Main}\">" + string.Concat(shared.Select(t => $"<si><t>{SecurityElement.Escape(t)}</t></si>")) + "</sst>");
        }
        stream.Position = 0;
        return stream;
    }

    public static MemoryStream BuildRaw(string sheetName, string sheetDataXml, string? sharedStringsXml = null)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "xl/workbook.xml",
                $"<workbook xmlns=\"{Main}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"{sheetName}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(zip, "xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            Add(zip, "xl/worksheets/sheet1.xml",
                $"<worksheet xmlns=\"{Main}\"><sheetData>{sheetDataXml}</sheetData></worksheet>");
            if (sharedStringsXml is not null)
            {
                Add(zip, "xl/sharedStrings.xml", $"<sst xmlns=\"{Main}\">{sharedStringsXml}</sst>");
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static string Column(int index)
    {
        var name = "";
        index++;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static void Add(ZipArchive zip, string path, string xml)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(xml);
    }
}
