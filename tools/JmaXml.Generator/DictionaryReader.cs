using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace JmaXml.Generator;

public static partial class DictionaryReader
{
    private static readonly ImmutableArray<string> Prefixes = ["jmx", "jmx_ib", "jmx_eb", "jmx_mete", "jmx_seis", "jmx_volc"];

    private static readonly string[] Header = ["項番", "親要素", "子要素", "属性", "基底型", "サイズ", "出現回数", "意味", "とりうる値", "解説"];

    public static DictionaryModel Read(string path)
    {
        var match = FileNamePattern().Match(Path.GetFileName(path));
        if (!match.Success) throw new InvalidDataException($"Unexpected dictionary file name: {path}");
        var date = $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}";
        using var stream = File.OpenRead(path);
        var sheets = XlsxReader.ReadSheets(stream, Prefixes);
        return new DictionaryModel(date, [.. Prefixes.Select(p => new DictNamespace(p, ParseSheet(p, sheets[p])))]);
    }

    public static ImmutableArray<DictType> ParseSheet(string prefix, string[][] rows)
    {
        if (rows.Length < 4)
        {
            throw new InvalidDataException($"Sheet '{prefix}': has fewer than 4 rows, no header row to read");
        }
        if (!Header.SequenceEqual(rows[3].Take(Header.Length).Select(c => c.Trim()), StringComparer.Ordinal))
        {
            throw new InvalidDataException($"Sheet '{prefix}': row 4 does not match the expected header");
        }
        var types = new List<DictType>();
        TypeBuilder? current = null;
        MemberBuilder? last = null;
        for (var i = 4; i < rows.Length; i++)
        {
            var c = Cells(rows[i]);
            if (c[1].Length > 0)
            {
                if (current is not null) types.Add(current.Build());
                if (c[1].StartsWith("type.", StringComparison.Ordinal))
                {
                    current = new TypeBuilder(c[1], c[9]);
                }
                else if (c[1] == "(element)" || c[1] == "(end)")
                {
                    current = null;
                }
                else
                {
                    throw new InvalidDataException($"Sheet '{prefix}': row {i + 1} has an unrecognized 親要素 marker '{c[1]}'");
                }
                last = null;
                if (c[2].Length == 0 && c[3].Length == 0) continue;
            }
            if (current is null) continue;
            if (c[4] == "*")
            {
                last?.Values.Add(new DictValue(c[8], c[9]));
                continue;
            }
            if (c[2].Length > 0)
            {
                last = new MemberBuilder(c[2], c[6], c[7], c[9]);
                current.Elements.Add(last);
            }
            else if (c[3].Length > 0)
            {
                last = new MemberBuilder(c[3], c[6], c[7], c[9]);
                current.Attributes.Add(last);
            }
        }
        if (current is not null) types.Add(current.Build());
        return [.. types];
    }

    private static string[] Cells(string[] row)
    {
        var cells = new string[Header.Length];
        for (var i = 0; i < Header.Length; i++) cells[i] = i < row.Length ? row[i].Trim() : "";
        return cells;
    }

    [GeneratedRegex(@"^jmaxml_(\d{4})(\d{2})(\d{2})_dictionary\.xlsx$")]
    private static partial Regex FileNamePattern();

    private sealed class TypeBuilder(string name, string description)
    {
        public List<MemberBuilder> Elements { get; } = [];

        public List<MemberBuilder> Attributes { get; } = [];

        public DictType Build() => new(name, description, [.. Elements.Select(e => e.Build())], [.. Attributes.Select(a => a.Build())]);
    }

    private sealed class MemberBuilder(string name, string occurrence, string meaning, string description)
    {
        public List<DictValue> Values { get; } = [];

        public DictMember Build() => new(name, occurrence, meaning, description, [.. Values]);
    }
}
