using JmaXml.Generator;
using System.Text.RegularExpressions;

namespace JmaXml.Tests;

public partial class CoverageListTests(ITestOutputHelper output)
{
    private static readonly HashSet<string> ElementsOfDiscontinuedProducts = new(StringComparer.Ordinal)
    {
        "Tokai", "ArrivalTimeFrom", "ArrivalTimeTo", "TsunamiHeightFrom", "TsunamiHeightTo",
    };

    private static readonly HashSet<string> ElementsDeclaredUnused = new(StringComparer.Ordinal)
    {
        "MaxLgIntChangeReason",
    };

    private static readonly HashSet<string> ElementsRemovedFromSamples = new(StringComparer.Ordinal)
    {
        "Naming",
        "FormalName", "AreaFromMark", "OtherInfo",
    };

    private static readonly HashSet<string> ElementsNeverObserved = new(StringComparer.Ordinal)
    {
        "LongAxis", "ShortAxis", "Bearings", "WindScale", "ReliabilityValue", "ProbabilityOfAftershock",
        "TargetDateTimeNotice", "OtherReport", "SunshinePart", "ReliabilityValuePart", "PrefectureList", "PrefectureCodeList", "SubPrefecture", "SubPrefectureCode", "SubPrefectureList", "SubPrefectureCodeList", "CityList", "CityCodeList", "SubCityCode", "EventClass",
        "Aftershock", "Release", "Period", "CurrentHeight", "TargetMagnitude", "ObservationComment",
    };

    private static IEnumerable<string> Excluded =>
        ElementsOfDiscontinuedProducts.Concat(ElementsDeclaredUnused).Concat(ElementsRemovedFromSamples).Concat(ElementsNeverObserved);

    [Fact]
    public void Lists_schema_elements_not_covered_by_fixtures()
    {
        var schema = SchemaLoader.Load(Fixtures.XsdDir);
        var declared = schema.Namespaces.SelectMany(n => n.Types).SelectMany(t => t.Elements).Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
        var inSamples = ElementNames(Fixtures.Samples);
        var inSynthetic = ElementNames(Fixtures.Synthetic);
        var absentFromSamples = declared.Where(n => !inSamples.Contains(n)).Order(StringComparer.Ordinal).ToList();
        var absentFromAll = absentFromSamples.Where(n => !inSynthetic.Contains(n)).ToList();
        var excluded = Excluded.ToHashSet(StringComparer.Ordinal);
        var uncovered = absentFromAll.Where(n => !excluded.Contains(n)).ToList();
        output.WriteLine($"absent from samples ({absentFromSamples.Count}): {string.Join(' ', absentFromSamples)}");
        output.WriteLine($"absent from samples and synthetic ({absentFromAll.Count}): {string.Join(' ', absentFromAll)}");
        output.WriteLine($"uncovered, excluding discontinued, declared unused, removed from samples and never observed ({uncovered.Count}): {string.Join(' ', uncovered)}");
        Assert.NotEmpty(declared);
        Assert.NotEmpty(inSynthetic);
        Assert.True(absentFromSamples.Count < declared.Count);
    }

    [Fact]
    public void Excluded_elements_are_still_declared_in_the_schema()
    {
        var schema = SchemaLoader.Load(Fixtures.XsdDir);
        var declared = schema.Namespaces.SelectMany(n => n.Types).SelectMany(t => t.Elements).Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(Excluded.Order(StringComparer.Ordinal), Excluded.Where(declared.Contains).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static HashSet<string> ElementNames(string dir) =>
        Directory.GetFiles(dir, "*.xml")
            .SelectMany(f => ElementName().Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"<(?:[A-Za-z_]+:)?([A-Za-z][A-Za-z0-9]*)")]
    private static partial Regex ElementName();
}
