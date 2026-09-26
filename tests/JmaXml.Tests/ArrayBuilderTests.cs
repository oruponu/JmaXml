namespace JmaXml.Tests;

public class ArrayBuilderTests
{
    [Fact]
    public void Empty_builder_returns_an_empty_array()
    {
        using var reader = Xml.Reader("<a/>");
        var r = new JmaXmlReader(reader);
        var builder = new ArrayBuilder<string>();
        Assert.True(builder.IsEmpty);
        var result = builder.ToImmutable(r);
        Assert.False(result.IsDefault);
        Assert.Empty(result);
    }

    [Fact]
    public void Items_keep_their_order_beyond_the_initial_buffer()
    {
        using var reader = Xml.Reader("<a/>");
        var r = new JmaXmlReader(reader);
        var builder = new ArrayBuilder<string>();
        for (var i = 0; i < 100; i++) builder.Add(r, $"v{i}");
        Assert.False(builder.IsEmpty);
        Assert.Equal(Enumerable.Range(0, 100).Select(i => $"v{i}"), builder.ToImmutable(r));
    }

    [Fact]
    public void Builders_in_use_at_the_same_time_keep_their_own_items()
    {
        using var reader = Xml.Reader("<a/>");
        var r = new JmaXmlReader(reader);
        var outer = new ArrayBuilder<string>();
        outer.Add(r, "o1");
        var first = new ArrayBuilder<string>();
        first.Add(r, "f1");
        Assert.Equal(["f1"], first.ToImmutable(r));
        var second = new ArrayBuilder<string>();
        second.Add(r, "s1");
        var third = new ArrayBuilder<string>();
        third.Add(r, "t1");
        outer.Add(r, "o2");
        Assert.Equal(["s1"], second.ToImmutable(r));
        Assert.Equal(["t1"], third.ToImmutable(r));
        Assert.Equal(["o1", "o2"], outer.ToImmutable(r));
    }

    [Fact]
    public void Returned_buffers_hold_no_items()
    {
        using var reader = Xml.Reader("<a/>");
        var r = new JmaXmlReader(reader);
        var builder = new ArrayBuilder<string>();
        for (var i = 0; i < 20; i++) builder.Add(r, $"v{i}");
        builder.ToImmutable(r);
        for (var i = 0; i < 3; i++) Assert.All(r.RentBuffer(0), Assert.Null);
    }
}
