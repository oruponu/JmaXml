using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace JmaXml.Tests;

public class SampleTests
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> WalkableProperties = new();

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.GetFiles(Fixtures.Samples, "*.xml").Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(path));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Every_official_sample_parses(string file)
    {
        var report = Report.Parse(Fixtures.Sample(file));
        Assert.NotEmpty(report.Control.Title);
        Assert.NotNull(report.Body);
        AssertNoDefaultImmutableArrays(report, file, nameof(report), new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Samples_split_by_body_namespace_covers_all()
    {
        var bodies = Directory.GetFiles(Fixtures.Samples, "*.xml").Select(p => Report.Parse(File.ReadAllText(p)).Body).ToList();
        var meteorologyCount = bodies.Count(b => b is Meteorology.Body);
        var seismologyCount = bodies.Count(b => b is Seismology.Body);
        var volcanologyCount = bodies.Count(b => b is Volcanology.Body);
        Assert.Equal(bodies.Count, meteorologyCount + seismologyCount + volcanologyCount);
        Assert.True(meteorologyCount > 0);
        Assert.True(seismologyCount > 0);
        Assert.True(volcanologyCount > 0);
    }

    private static PropertyInfo[] GetWalkableProperties(Type type) =>
        WalkableProperties.GetOrAdd(type, static t => [.. t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0)]);

    private static void AssertNoDefaultImmutableArrays(object? value, string sample, string path, HashSet<object> visited)
    {
        if (value is null)
        {
            return;
        }
        var type = value.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
        {
            Assert.False((bool)type.GetProperty("IsDefault")!.GetValue(value)!, $"{sample}: {path} is a default (uninitialized) ImmutableArray<T>");
            var index = 0;
            foreach (var element in (IEnumerable)value)
            {
                AssertNoDefaultImmutableArrays(element, sample, $"{path}[{index}]", visited);
                index++;
            }
            return;
        }
        if (type.Namespace != "JmaXml" || !visited.Add(value))
        {
            return;
        }
        foreach (var property in GetWalkableProperties(type))
        {
            AssertNoDefaultImmutableArrays(property.GetValue(value), sample, $"{path}.{property.Name}", visited);
        }
    }
}
