namespace JmaXml.Tests;

public class SampleValueTests
{
    [Fact]
    public void Prefecture_forecast_VPFD51()
    {
        var report = Report.Parse(Fixtures.Sample("24_11_03_190925_VPFD51.xml"));
        Assert.Equal("府県天気予報（Ｒ１）", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal(new DateTimeOffset(2016, 11, 23, 17, 0, 0, TimeSpan.FromHours(9)), report.Head.ReportDateTime);
        var body = Assert.IsType<Meteorology.Body>(report.Body);
        var stationForecast = body.MeteorologicalInfos[1];
        Assert.Equal("地点予報", stationForecast.Type, StringComparer.Ordinal);
        var series = Assert.Single(stationForecast.TimeSeriesInfo);
        Assert.Equal(2, series.TimeDefines.TimeDefine.Length);
        Assert.Equal("明日朝", series.TimeDefines.TimeDefine[0].Name, StringComparer.Ordinal);
        Assert.Equal(new DateTimeOffset(2016, 11, 24, 0, 0, 0, TimeSpan.FromHours(9)), series.TimeDefines.TimeDefine[0].DateTime.Value);
        Assert.Equal(4, series.Item.Length);
        var item = series.Item[0];
        Assert.Equal("東京", item.Station!.Name, StringComparer.Ordinal);
        Assert.Equal("44132", Assert.Single(item.Station.Code).Value, StringComparer.Ordinal);
        Assert.Equal(2, item.Kind[0].Property.Length);
        Assert.Equal("朝の最低気温", item.Kind[0].Property[0].Type, StringComparer.Ordinal);
        Assert.Equal(2f, Assert.Single(item.Kind[0].Property[0].TemperaturePart!.Temperature).Value);
        Assert.Equal("日中の最高気温", item.Kind[0].Property[1].Type, StringComparer.Ordinal);
        Assert.Equal(4f, Assert.Single(item.Kind[0].Property[1].TemperaturePart!.Temperature).Value);
    }

    [Fact]
    public void Tsunami_warning_VTSE41()
    {
        var report = Report.Parse(Fixtures.Sample("32-39_11_02_250206_VTSE41.xml"));
        Assert.Equal("津波警報・注意報・予報a", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal("20110311144640", report.Head.EventId, StringComparer.Ordinal);
        var headItem = report.Head.Headline.Information[0].Item[0];
        Assert.Equal("大津波警報", headItem.Kind[0].Name, StringComparer.Ordinal);
        Assert.Equal("52", headItem.Kind[0].Code, StringComparer.Ordinal);
        Assert.Equal("津波予報区", headItem.Areas.CodeType, StringComparer.Ordinal);
        Assert.Equal("東北地方太平洋沿岸", headItem.Areas.Area[0].Name, StringComparer.Ordinal);
        Assert.Equal(2, report.Head.Headline.Information.Sum(i => i.Item.Length));
        var body = Assert.IsType<Seismology.Body>(report.Body);
        var forecast = body.Tsunami!.Forecast!;
        Assert.Equal(43, forecast.Item.Length);
        Assert.Equal(3, forecast.CodeDefine!.Type.Length);
        Assert.Equal("Item/Area/Code", forecast.CodeDefine.Type[0].Xpath, StringComparer.Ordinal);
        Assert.Equal("津波予報区", forecast.CodeDefine.Type[0].Value, StringComparer.Ordinal);
        Assert.Equal("岩手県", forecast.Item[0].Area.Name, StringComparer.Ordinal);
        Assert.Equal("大津波警報：発表", forecast.Item[0].Category!.Kind.Name, StringComparer.Ordinal);
        Assert.Equal("53", forecast.Item[0].Category!.Kind.Code, StringComparer.Ordinal);
    }

    [Fact]
    public void Eew_forecast_VXSE45()
    {
        var report = Report.Parse(Fixtures.Sample("77_01_01_240613_VXSE45.xml"));
        Assert.Equal("緊急地震速報（地震動予報）", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal("20240417231454", report.Head.EventId, StringComparer.Ordinal);
        var body = Assert.IsType<Seismology.Body>(report.Body);
        var earthquake = Assert.Single(body.Earthquake);
        Assert.Equal(new DateTimeOffset(2024, 4, 17, 23, 14, 47, TimeSpan.FromHours(9)), earthquake.OriginTime);
        Assert.Equal(new DateTimeOffset(2024, 4, 17, 23, 14, 54, TimeSpan.FromHours(9)), earthquake.ArrivalTime);
        var area = earthquake.Hypocenter!.Area;
        Assert.Equal("豊後水道", area.Name, StringComparer.Ordinal);
        Assert.Equal("681", area.Code.Value, StringComparer.Ordinal);
        Assert.Equal("震央地名", area.Code.Type, StringComparer.Ordinal);
        Assert.Equal("海域", area.LandOrSea, StringComparer.Ordinal);
        Assert.Equal("+33.1+132.4-40000/", Assert.Single(area.Coordinate).Value, StringComparer.Ordinal);
        Assert.True(float.IsNaN(earthquake.Hypocenter.Accuracy!.Epicenter.Value));
        Assert.Equal(4, earthquake.Hypocenter.Accuracy.Epicenter.Rank);
        Assert.Equal(4.2f, Assert.Single(earthquake.Magnitude).Value);
        var forecast = body.Intensity!.Forecast!;
        Assert.Equal("3", forecast.ForecastInt!.From, StringComparer.Ordinal);
        Assert.Equal("3", forecast.ForecastInt.To, StringComparer.Ordinal);
        Assert.Equal("0", forecast.ForecastLgInt!.From, StringComparer.Ordinal);
        Assert.Empty(forecast.Pref);
    }

    [Fact]
    public void Long_period_ground_motion_VXSE62()
    {
        var report = Report.Parse(Fixtures.Sample("78_01_01_240613_VXSE62.xml"));
        Assert.Equal("長周期地震動に関する観測情報", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal("20201121023300", report.Head.EventId, StringComparer.Ordinal);
        var body = Assert.IsType<Seismology.Body>(report.Body);
        var earthquake = Assert.Single(body.Earthquake);
        Assert.Equal(new DateTimeOffset(2020, 11, 21, 2, 23, 0, TimeSpan.FromHours(9)), earthquake.OriginTime);
        Assert.Equal(new DateTimeOffset(2020, 11, 21, 2, 23, 10, TimeSpan.FromHours(9)), earthquake.ArrivalTime);
        Assert.Equal("岩手県沖", earthquake.Hypocenter!.Area.Name, StringComparer.Ordinal);
        Assert.Equal("286", earthquake.Hypocenter.Area.Code.Value, StringComparer.Ordinal);
        Assert.Equal("震央地名", earthquake.Hypocenter.Area.Code.Type, StringComparer.Ordinal);
        Assert.Equal(6.3f, Assert.Single(earthquake.Magnitude).Value);
        var observation = body.Intensity!.Observation!;
        Assert.Equal("5-", observation.MaxInt, StringComparer.Ordinal);
        Assert.Equal("3", observation.MaxLgInt, StringComparer.Ordinal);
        Assert.Equal("4", observation.LgCategory, StringComparer.Ordinal);
        Assert.Equal(3, observation.Pref.Length);
        var pref = observation.Pref[0];
        Assert.Equal("宮城県", pref.Name, StringComparer.Ordinal);
        Assert.Equal("04", pref.Code, StringComparer.Ordinal);
        var area = Assert.Single(pref.Area);
        Assert.Equal("宮城県北部", area.Name, StringComparer.Ordinal);
        Assert.Equal(3, area.IntensityStation.Length);
        var station = area.IntensityStation[0];
        Assert.Equal("登米市中田町", station.Name, StringComparer.Ordinal);
        Assert.Equal("0421200", station.Code, StringComparer.Ordinal);
        Assert.Equal("4", station.Int, StringComparer.Ordinal);
        Assert.Equal("3", station.LgInt, StringComparer.Ordinal);
        Assert.Equal(7, station.LgIntPerPeriod.Length);
        Assert.Equal(1, station.LgIntPerPeriod[0].PeriodicBand);
        Assert.Equal("2", station.LgIntPerPeriod[0].Value, StringComparer.Ordinal);
        Assert.Equal(20.5f, station.Sva!.Value);
        Assert.Equal("cm/s", station.Sva.Unit, StringComparer.Ordinal);
        Assert.Equal(7, station.SvaPerPeriod.Length);
        Assert.Equal(20.5f, station.SvaPerPeriod[0].Value);
        Assert.Equal("https://www.data.jma.go.jp/eew/data/ltpgm/202011211100000/index.html", body.Comments!.Uri, StringComparer.Ordinal);
    }

    [Fact]
    public void Volcano_commentary_VFVO51()
    {
        var report = Report.Parse(Fixtures.Sample("44_01_01_151008_VFVO51.xml"));
        Assert.Equal("火山の状況に関する解説情報", report.Control.Title, StringComparer.Ordinal);
        var body = Assert.IsType<Volcanology.Body>(report.Body);
        var info = Assert.Single(body.VolcanoInfo);
        Assert.Equal("火山の状況に関する解説情報（対象火山）", info.Type, StringComparer.Ordinal);
        var item = info.Item[0];
        Assert.Equal("レベル５（避難）", item.Kind.Name, StringComparer.Ordinal);
        Assert.Equal("15", item.Kind.Code, StringComparer.Ordinal);
        Assert.Equal("継続", item.Kind.Condition, StringComparer.Ordinal);
        Assert.Equal("", item.LastKind!.Condition, StringComparer.Ordinal);
        Assert.Equal("火山名", item.Areas.CodeType, StringComparer.Ordinal);
        Assert.Equal("口永良部島", item.Areas.Area[0].Name, StringComparer.Ordinal);
    }
}
