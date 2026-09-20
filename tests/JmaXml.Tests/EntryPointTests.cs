using System.Text;
using System.Xml;

namespace JmaXml.Tests;

public class EntryPointTests
{
    private const string File = "32-35_01_03_240613_VXSE53.xml";

    private static int Stations(Report report) =>
        ((Seismology.Body)report.Body).Intensity!.Observation!.Pref.Sum(p => p.Area.Sum(a => a.City.Sum(c => c.IntensityStation.Length)));

    private static string Fragment(string report) =>
        report.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n", "", StringComparison.Ordinal);

    [Fact]
    public void All_four_entry_points_give_the_same_result()
    {
        var text = Fixtures.Sample(File);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var textReader = new StringReader(text);
        using var xmlReader = Xml.Reader(text);
        Report[] reports = [Report.Parse(text), Report.Parse(stream), Report.Parse(textReader), Report.Parse(xmlReader)];
        foreach (var report in reports)
        {
            Assert.Equal("震源・震度に関する情報", report.Control.Title, StringComparer.Ordinal);
            Assert.Equal("20091001134500", report.Head.EventId, StringComparer.Ordinal);
            Assert.Equal("　１日１３時４５分ころ、地震がありました。各地の震度をお知らせします。", report.Head.Headline.Text, StringComparer.Ordinal);
            Assert.Equal(218, Stations(report));
        }
    }

    [Fact]
    public void Whitespace_only_content_is_the_same_through_every_entry_point()
    {
        var text = System.IO.File.ReadAllText(Path.Combine(Fixtures.Synthetic, "whitespace.xml"));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var textReader = new StringReader(text);
        using var xmlReader = Xml.Reader(text);
        Report[] reports = [Report.Parse(text), Report.Parse(stream), Report.Parse(textReader), Report.Parse(xmlReader)];
        foreach (var report in reports)
        {
            Assert.Equal("   ", ((Seismology.Body)report.Body).Text, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void Null_is_rejected_by_every_entry_point()
    {
        Assert.Throws<ArgumentNullException>(() => Report.Parse((string)null!));
        Assert.Throws<ArgumentNullException>(() => Report.Parse((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => Report.Parse((TextReader)null!));
        Assert.Throws<ArgumentNullException>(() => Report.Parse((XmlReader)null!));
    }

    [Fact]
    public void Control_Head_and_Body_can_be_read_in_sequence_from_one_reader()
    {
        using var reader = Xml.Reader(Fixtures.Sample(File));
        reader.MoveToContent();
        reader.Read();
        var control = Control.Parse(reader);
        Assert.Equal("震源・震度に関する情報", control.Title, StringComparer.Ordinal);
        var head = InformationBasis.Head.Parse(reader);
        Assert.Equal("20091001134500", head.EventId, StringComparer.Ordinal);
        var body = Seismology.Body.Parse(reader);
        Assert.Equal(218, body.Intensity!.Observation!.Pref.Sum(p => p.Area.Sum(a => a.City.Sum(c => c.IntensityStation.Length))));
        Assert.Equal(XmlNodeType.EndElement, reader.MoveToContent());
        Assert.Equal("Report", reader.LocalName, StringComparer.Ordinal);
    }

    [Fact]
    public void Parse_XmlReader_leaves_the_reader_after_the_Report_end_tag()
    {
        using var reader = Xml.Reader(Fixtures.Sample(File));
        Report.Parse(reader);
        Assert.Equal(XmlNodeType.None, reader.MoveToContent());
        Assert.True(reader.EOF);
    }

    [Fact]
    public void Two_reports_are_read_in_sequence_from_one_fragment_reader()
    {
        var text = Fragment(Skeleton.Report(Skeleton.SeisBody("<Text>1</Text>")))
            + Fragment(Skeleton.Report(Skeleton.SeisBody("<Text>2</Text>")));
        using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = false,
        });
        Assert.Equal("1", ((Seismology.Body)Report.Parse(reader).Body).Text, StringComparer.Ordinal);
        Assert.Equal("2", ((Seismology.Body)Report.Parse(reader).Body).Text, StringComparer.Ordinal);
        Assert.True(reader.EOF);
    }

    [Fact]
    public void Content_after_the_root_is_rejected_even_when_whitespace_precedes_it()
    {
        var text = Skeleton.Report(Skeleton.SeisBody("<Text>x</Text>")) + "\n<extra/>";
        Assert.Throws<XmlException>(() => Report.Parse(text));
        using var reader = Xml.Reader(text);
        Assert.Equal("x", ((Seismology.Body)Report.Parse(reader).Body).Text, StringComparer.Ordinal);
    }

    [Fact]
    public void Caller_owned_stream_and_text_reader_stay_open()
    {
        var text = Fixtures.Sample(File);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        Report.Parse(stream);
        Assert.True(stream.CanRead);
        using var textReader = new StringReader(text);
        Report.Parse(textReader);
        Assert.Equal(-1, textReader.Peek());
    }
}
