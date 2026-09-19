namespace JmaXml.Tests;

public class ReportTests
{
    [Fact]
    public void Parses_a_seismology_sample_end_to_end()
    {
        var report = Report.Parse(Fixtures.Sample("32-35_01_03_240613_VXSE53.xml"));
        Assert.Equal("震源・震度に関する情報", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal("訓練", report.Control.Status, StringComparer.Ordinal);
        Assert.Equal(new DateTimeOffset(2009, 10, 1, 4, 50, 1, TimeSpan.Zero), report.Control.DateTime);
        Assert.Equal(["気象庁"], report.Control.PublishingOffice, StringComparer.Ordinal);
        Assert.Equal("20091001134500", report.Head.EventId, StringComparer.Ordinal);
        Assert.Equal(new DateTimeOffset(2009, 10, 1, 13, 50, 0, TimeSpan.FromHours(9)), report.Head.TargetDateTime);
        var body = Assert.IsType<Seismology.Body>(report.Body);
        var earthquake = Assert.Single(body.Earthquake);
        Assert.Equal("駿河湾", earthquake.Hypocenter!.Area.Name, StringComparer.Ordinal);
        Assert.Equal(new DateTimeOffset(2009, 10, 1, 13, 45, 0, TimeSpan.FromHours(9)), earthquake.OriginTime);
        var magnitude = Assert.Single(earthquake.Magnitude);
        Assert.Equal(5.9f, magnitude.Value);
        Assert.Equal("Mj", magnitude.Type, StringComparer.Ordinal);
        Assert.Equal("Ｍ５．９", magnitude.Description, StringComparer.Ordinal);
        Assert.Equal("5-", body.Intensity!.Observation!.MaxInt, StringComparer.Ordinal);
        Assert.Equal(8, body.Intensity.Observation.Pref.Length);
        Assert.Equal(129, body.Intensity.Observation.Pref.Sum(p => p.Area.Sum(a => a.City.Length)));
        Assert.Equal(218, body.Intensity.Observation.Pref.Sum(p => p.Area.Sum(a => a.City.Sum(c => c.IntensityStation.Length))));
    }
}
