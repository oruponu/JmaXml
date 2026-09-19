using System.Security;

namespace JmaXml.Generator;

public static class DocComments
{
    public static IEnumerable<string> ForMember(DictMember? member, string fallbackSummary)
    {
        var summary = member switch
        {
            { Meaning.Length: > 0 } => member.Meaning,
            { Description.Length: > 0 } => member.Description,
            _ => fallbackSummary,
        };
        foreach (var line in Block("summary", summary)) yield return line;
        var remarks = member is { Description.Length: > 0 } && member.Description != summary && !IsRedundant(member.Meaning, member.Description)
            ? member.Description
            : null;
        var values = member?.Values.Where(v => v.Value != "*").ToList() ?? [];
        if (remarks is null && values.Count == 0) yield break;
        yield return "/// <remarks>";
        if (remarks is not null)
        {
            foreach (var line in Lines(remarks)) yield return $"/// <para>{Escape(line)}</para>";
        }
        if (values.Count > 0)
        {
            yield return "/// <list type=\"bullet\">";
            foreach (var value in values)
            {
                var description = value.Description.Length > 0 ? ": " + Escape(value.Description) : "";
                yield return $"/// <item><description>{Escape(Unquote(value.Value))}{description}</description></item>";
            }
            yield return "/// </list>";
        }
        yield return "/// </remarks>";
    }

    public static IEnumerable<string> ForType(DictType? type, string fallbackSummary)
    {
        var summary = type is { Description.Length: > 0 } && !type.Description.EndsWith("型の要素を示す", StringComparison.Ordinal)
            ? type.Description
            : fallbackSummary;
        return Block("summary", summary);
    }

    private static IEnumerable<string> Block(string tag, string text)
    {
        var lines = Lines(text).ToList();
        if (lines.Count <= 1)
        {
            yield return $"/// <{tag}>{Escape(lines.Count == 0 ? "" : lines[0])}</{tag}>";
            yield break;
        }
        yield return $"/// <{tag}>";
        foreach (var line in lines) yield return $"/// <para>{Escape(line)}</para>";
        yield return $"/// </{tag}>";
    }

    private static IEnumerable<string> Lines(string text) => text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);

    private static bool IsRedundant(string meaning, string description) =>
        description == meaning || description == meaning + "を示す" || description == meaning + "を示す。";

    private static string Unquote(string value) => value is ['"', .., '"'] ? value[1..^1] : value;

    private static string Escape(string text) => SecurityElement.Escape(text);
}
