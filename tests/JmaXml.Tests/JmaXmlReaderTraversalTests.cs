using System.Diagnostics;
using System.Text;

namespace JmaXml.Tests;

public class JmaXmlReaderTraversalTests
{
    [Fact]
    public void Path_numbers_repeated_siblings_from_the_second()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><p><q/><q/></p><p><q/></p></a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var paths = new List<string>();
        using (r.Enter())
        {
            while (r.NextChild())
            {
                using (r.Enter())
                {
                    while (r.NextChild())
                    {
                        paths.Add(r.Path);
                        r.Skip();
                    }
                }
            }
        }
        Assert.Equal(["a/p/q", "a/p/q[2]", "a/p[2]/q"], paths);
    }

    [Fact]
    public void Path_numbers_siblings_within_each_parent()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><q/><p><q/><r/><q/></p><r/><p/><p><q/></p></a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var paths = new List<string>();
        using (r.Enter())
        {
            while (r.NextChild())
            {
                paths.Add(r.Path);
                using (r.Enter())
                {
                    while (r.NextChild())
                    {
                        paths.Add(r.Path);
                        r.Skip();
                    }
                }
            }
        }
        Assert.Equal(["a/q", "a/p", "a/p/q", "a/p/r", "a/p/q[2]", "a/r", "a/p[2]", "a/p[3]", "a/p[3]/q"], paths);
    }

    [Fact]
    public void Path_numbers_siblings_beyond_the_linear_limit()
    {
        var names = Enumerable.Range(0, 20).Select(i => $"n{i}").ToList();
        var xml = "<a xmlns=\"urn:x\">" + string.Concat(names.Select(n => $"<{n}/>")) + "<n0/><n19/><n3><q/><q/></n3></a>";
        using var reader = Xml.Reader(xml);
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var paths = new List<string>();
        using (r.Enter())
        {
            while (r.NextChild())
            {
                paths.Add(r.Path);
                using (r.Enter())
                {
                    while (r.NextChild())
                    {
                        paths.Add(r.Path);
                        r.Skip();
                    }
                }
            }
        }
        Assert.Equal([.. names.Select(n => $"a/{n}"), "a/n0[2]", "a/n19[2]", "a/n3[2]", "a/n3[2]/q", "a/n3[2]/q[2]"], paths);
    }

    [Fact]
    public void Many_distinct_sibling_names_are_numbered_in_linear_time()
    {
        var xml = new StringBuilder("<a xmlns=\"urn:x\">");
        for (var i = 0; i < 80_000; i++) xml.Append($"<e{i}/>");
        xml.Append("</a>");
        using var reader = Xml.Reader(xml.ToString());
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var count = 0;
        var stopwatch = Stopwatch.StartNew();
        using (r.Enter())
        {
            while (r.NextChild())
            {
                count++;
                r.Skip();
            }
        }
        stopwatch.Stop();
        Assert.Equal(80_000, count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"took {stopwatch.Elapsed.TotalMilliseconds:N0} ms");
    }

    [Fact]
    public void MoveToElement_accepts_a_reader_already_on_the_element()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"/>");
        reader.MoveToContent();
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        Assert.Equal("a", r.Path);
    }

    [Fact]
    public void MoveToElement_rejects_a_different_root()
    {
        using var reader = Xml.Reader("<b xmlns=\"urn:x\"/>");
        var r = new JmaXmlReader(reader);
        var ex = Assert.Throws<JmaXmlException>(() => r.MoveToElement("a", "urn:x"));
        Assert.Contains("'b'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NextChild_visits_child_elements_only_and_consumes_the_end_tag()
    {
        using var reader = Xml.Reader("<?xml version=\"1.0\"?>\n<a xmlns=\"urn:x\">\n  <b/> text <c>1</c>\n</a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var names = new List<string>();
        using (r.Enter())
        {
            while (r.NextChild())
            {
                names.Add(r.LocalName);
                r.Skip();
            }
        }
        Assert.Equal(["b", "c"], names);
        Assert.True(reader.EOF);
    }

    [Fact]
    public void Empty_element_has_no_children()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><b/><c/></a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var names = new List<string>();
        using (r.Enter())
        {
            while (r.NextChild())
            {
                names.Add(r.LocalName);
                using (r.Enter())
                {
                    Assert.False(r.NextChild());
                }
            }
        }
        Assert.Equal(["b", "c"], names);
    }

    [Fact]
    public void Once_throws_on_the_second_occurrence_with_the_indexed_path()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><p/><p/></a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        var seen = false;
        using (r.Enter())
        {
            Assert.True(r.NextChild());
            r.Once(ref seen, "p");
            r.Skip();
            Assert.True(r.NextChild());
            var ex = Assert.Throws<JmaXmlException>(() => r.Once(ref seen, "p"));
            Assert.Equal("a/p[2]", ex.Path);
            Assert.Equal(1, ex.LineNumber);
            Assert.True(ex.LinePosition > 1);
        }
    }

    [Fact]
    public void Missing_uses_the_scope_element_path_plus_the_name()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><p/><p/></a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        using (r.Enter())
        {
            while (r.NextChild()) r.Skip();
            var ex = r.Missing("z");
            Assert.Equal("a/z", ex.Path);
            Assert.Equal("Required element 'z' is missing (path: a/z, line: 1, position: 2)", ex.Message);
        }
    }

    [Fact]
    public void Missing_reports_the_start_tag_of_the_scope_element_not_the_reader_position()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\">\n  <p>\n  </p>\n  <q/>\n</a>");
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        using (r.Enter())
        {
            Assert.True(r.NextChild());
            using (r.Enter())
            {
                Assert.False(r.NextChild());
                var ex = r.Missing("z");
                Assert.Equal("a/p/z", ex.Path);
                Assert.Equal((2, 4), (ex.LineNumber, ex.LinePosition));
            }
            Assert.True(r.NextChild());
            Assert.Equal("q", r.LocalName);
        }
    }
}
