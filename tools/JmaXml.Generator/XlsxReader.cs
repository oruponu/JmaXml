using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace JmaXml.Generator;

public static class XlsxReader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Pkg = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static IReadOnlyDictionary<string, string[][]> ReadSheets(Stream xlsx, IEnumerable<string> sheetNames)
    {
        using var zip = new ZipArchive(xlsx, ZipArchiveMode.Read, leaveOpen: true);
        const string workbookPart = "xl/workbook.xml";
        const string relsPart = "xl/_rels/workbook.xml.rels";
        var sheetIds = Load(zip, workbookPart).Descendants(Main + "sheet")
            .ToDictionary(
                s => RequiredAttribute(s, "name", "name", workbookPart),
                s => RequiredAttribute(s, Rel + "id", "r:id", workbookPart),
                StringComparer.Ordinal);
        var targets = Load(zip, relsPart).Descendants(Pkg + "Relationship")
            .ToDictionary(
                r => RequiredAttribute(r, "Id", "Id", relsPart),
                r => NormalizeTarget(RequiredAttribute(r, "Target", "Target", relsPart)),
                StringComparer.Ordinal);
        var shared = zip.GetEntry("xl/sharedStrings.xml") is null
            ? []
            : Load(zip, "xl/sharedStrings.xml").Root!.Elements(Main + "si")
                .Select(TextWithoutFurigana).ToList();
        var result = new Dictionary<string, string[][]>(StringComparer.Ordinal);
        foreach (var name in sheetNames)
        {
            if (!sheetIds.TryGetValue(name, out var id) || !targets.TryGetValue(id, out var target))
            {
                throw new InvalidDataException($"Sheet '{name}' not found in workbook");
            }
            result[name] = ReadSheet(target, Load(zip, target), shared);
        }
        return result;
    }

    private static string[][] ReadSheet(string part, XDocument sheet, List<string> shared)
    {
        var cells = new Dictionary<(int Row, int Column), string>();
        var maxRow = 0;
        var maxColumn = 0;
        foreach (var c in sheet.Descendants(Main + "c"))
        {
            var (row, column) = ParseReference(RequiredAttribute(c, "r", "r", part));
            cells[(row, column)] = CellValue(c, shared);
            maxRow = Math.Max(maxRow, row);
            maxColumn = Math.Max(maxColumn, column);
        }
        var rows = new string[maxRow][];
        for (var r = 0; r < maxRow; r++)
        {
            rows[r] = new string[maxColumn];
            for (var col = 0; col < maxColumn; col++)
            {
                rows[r][col] = cells.GetValueOrDefault((r + 1, col + 1), "");
            }
        }
        return rows;
    }

    private static string CellValue(XElement c, List<string> shared)
    {
        var type = (string?)c.Attribute("t");
        if (type == "inlineStr") return TextWithoutFurigana(c);
        var v = c.Element(Main + "v")?.Value ?? "";
        return type == "s" ? shared[int.Parse(v, CultureInfo.InvariantCulture)] : v;
    }

    private static string TextWithoutFurigana(XElement container) =>
        string.Concat(container.Descendants(Main + "t")
            .Where(t => t.Parent?.Name != Main + "rPh")
            .Select(t => t.Value));

    private static (int Row, int Column) ParseReference(string reference)
    {
        var column = 0;
        var i = 0;
        while (i < reference.Length && char.IsAsciiLetterUpper(reference[i]))
        {
            column = column * 26 + (reference[i] - 'A' + 1);
            i++;
        }
        return (int.Parse(reference.AsSpan(i), CultureInfo.InvariantCulture), column);
    }

    private static string NormalizeTarget(string target) => target.StartsWith('/') ? target[1..] : "xl/" + target;

    private static string RequiredAttribute(XElement element, XName name, string label, string part) =>
        (string?)element.Attribute(name) ?? throw new InvalidDataException($"'{part}' has a <{element.Name.LocalName}> with no '{label}' attribute");

    private static XDocument Load(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path) ?? throw new InvalidDataException($"'{path}' not found in xlsx");
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }
}
