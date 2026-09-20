namespace JmaXml.Tests;

public class SyntheticTests
{
    private static Report Parse(string file) => Report.Parse(File.ReadAllText(Path.Combine(Fixtures.Synthetic, file)));

    [Fact]
    public void Order_is_free_and_unknown_content_is_ignored()
    {
        var report = Parse("root-order-and-unknown.xml");
        Assert.Equal("地震回数に関する情報", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal(["気象庁"], report.Control.PublishingOffice, StringComparer.Ordinal);
        Assert.Equal("20080824150500", report.Head.EventId, StringComparer.Ordinal);
        Assert.Equal("地震回数に関する情報をお知らせします。", report.Head.Headline.Text, StringComparer.Ordinal);
        var body = Assert.IsType<Seismology.Body>(report.Body);
        Assert.Equal("次の「地震回数に関する情報」は、２６日１８時００分頃に発表します。", body.NextAdvisory, StringComparer.Ordinal);
    }

    [Fact]
    public void Whitespace_in_strings_is_preserved()
    {
        var report = Parse("whitespace.xml");
        Assert.Equal("　１行目\n　２行目", report.Head.Headline.Text, StringComparer.Ordinal);
        var body = Assert.IsType<Seismology.Body>(report.Body);
        Assert.Equal("   ", body.Text, StringComparer.Ordinal);
        Assert.Equal("  x ", body.NextAdvisory, StringComparer.Ordinal);
    }

    [Fact]
    public void Nil_TargetDateTime_is_null()
    {
        var report = Parse("target-datetime-nil.xml");
        Assert.Null(report.Head.TargetDateTime);
        Assert.Equal("20080824150500", report.Head.EventId, StringComparer.Ordinal);
    }

    [Fact]
    public void Station_without_Int_reads_LgInt_only()
    {
        var body = Assert.IsType<Seismology.Body>(Parse("station-without-int.xml").Body);
        var station = body.Intensity!.Observation!.Pref[0].Area[0].IntensityStation[0];
        Assert.Null(station.Int);
        Assert.Equal("3", station.LgInt, StringComparer.Ordinal);
        Assert.Equal("登米市中田町", station.Name, StringComparer.Ordinal);
    }

    [Fact]
    public void Flash_report_area_reads_the_sub_city()
    {
        var body = Assert.IsType<Meteorology.Body>(Parse("flash-report-subcity.xml").Body);
        var item = body.MeteorologicalInfos[0].MeteorologicalInfo[0].Item;
        var area = item[0].Area!;
        Assert.Equal("三宅村", area.Name, StringComparer.Ordinal);
        Assert.Equal("1338100", area.Code, StringComparer.Ordinal);
        Assert.Equal("三宅村神着", area.SubCity, StringComparer.Ordinal);
        Assert.Equal("付近", area.Status, StringComparer.Ordinal);
        var analysed = item[0].Kind[0].Property[0].PrecipitationPart[0].Precipitation[0];
        Assert.Equal("前１時間解析雨量", analysed.Type, StringComparer.Ordinal);
        Assert.Equal("約", analysed.Condition, StringComparer.Ordinal);
        Assert.Equal(100f, analysed.Value);
        Assert.Null(item[1].Area);
        Assert.Equal("三宅島", item[1].Station!.Name, StringComparer.Ordinal);
        var observed = item[1].Kind[0].Property[0].PrecipitationPart[0].Precipitation[0];
        Assert.Equal("前１時間降水量", observed.Type, StringComparer.Ordinal);
        Assert.Null(observed.Condition);
        Assert.Equal(93f, observed.Value);
    }

    [Fact]
    public void Climate_values_part_reads_the_sunshine_hours()
    {
        var report = Parse("weather-report-sunshine.xml");
        Assert.Equal("日照不足に関する東北地方気象情報", report.Head.Title, StringComparer.Ordinal);
        var body = Assert.IsType<Meteorology.Body>(report.Body);
        var stations = body.MeteorologicalInfos[0].MeteorologicalInfo[1];
        Assert.Equal("気象官署及び特別地域気象観測所", stations.Type, StringComparer.Ordinal);
        Assert.Equal("８月１４日から９月２日まで", stations.Name, StringComparer.Ordinal);
        Assert.Equal("盛岡", stations.Item[1].Station!.Name, StringComparer.Ordinal);
        var part = Assert.Single(stations.Item[1].Kind[0].Property[0].ClimateValuesPart);
        Assert.Equal("日照時間の合計と平年比", part.Type, StringComparer.Ordinal);
        var sunshine = Assert.Single(part.Sunshine);
        Assert.Equal("日照時間", sunshine.Type, StringComparer.Ordinal);
        Assert.Equal("h", sunshine.Unit, StringComparer.Ordinal);
        Assert.Equal(45.2f, sunshine.Value);
        var comparison = Assert.Single(part.Comparison);
        Assert.Equal("日照時間合計平年比", comparison.Type, StringComparer.Ordinal);
        Assert.Equal(47f, comparison.Value);
    }

    [Fact]
    public void Realtime_intensity_station_reads_the_measured_intensity()
    {
        var report = Parse("realtime-intensity-station.xml");
        Assert.Equal("リアルタイム震度", report.Control.Title, StringComparer.Ordinal);
        Assert.Equal("緊急地震速報", report.Head.InfoKind, StringComparer.Ordinal);
        var body = Assert.IsType<Seismology.Body>(report.Body);
        Assert.Equal("岩手県内陸南部", Assert.Single(body.Earthquake).Hypocenter!.Area.Name, StringComparer.Ordinal);
        var observation = body.Intensity!.Observation!;
        Assert.Null(body.Intensity.Forecast);
        Assert.Equal("リアルタイム震度観測点", observation.CodeDefine!.Type[3].Value, StringComparer.Ordinal);
        Assert.Equal(["青森県", "北海道"], observation.Pref.Select(p => p.Name), StringComparer.Ordinal);
        var city = observation.Pref[0].Area[0].City[0];
        Assert.Equal("むつ市", city.Name, StringComparer.Ordinal);
        Assert.Equal("むつ市大畑町奥薬研", city.IntensityStation[0].Name, StringComparer.Ordinal);
        Assert.Equal("0220801", city.IntensityStation[0].Code, StringComparer.Ordinal);
        Assert.Equal("5+", city.IntensityStation[0].Int, StringComparer.Ordinal);
        Assert.Equal(5.3f, city.IntensityStation[0].K);
        Assert.Equal("むつ市金曲", city.IntensityStation[1].Name, StringComparer.Ordinal);
        Assert.Equal("0220800", city.IntensityStation[1].Code, StringComparer.Ordinal);
        var hakodate = observation.Pref[1].Area[0].City[0].IntensityStation[0];
        Assert.Equal("函館市大森町", hakodate.Name, StringComparer.Ordinal);
        Assert.Equal("2", hakodate.Int, StringComparer.Ordinal);
        Assert.Equal(2.4f, hakodate.K);
        Assert.Equal("この情報をもって、緊急地震速報：最終報とします。", body.NextAdvisory, StringComparer.Ordinal);
    }

    [Fact]
    public void Volcano_white_plume_and_event_comment_are_read()
    {
        var body = Assert.IsType<Volcanology.Body>(Parse("volcano-observation-plume.xml").Body);
        var item = body.VolcanoInfo[0].Item[0];
        Assert.Equal("", item.EventTime!.EventDateTimeComment, StringComparer.Ordinal);
        Assert.Equal("", item.Areas.Area[0].CraterName, StringComparer.Ordinal);
        Assert.Null(item.Areas.Area[0].AreaFromMark);
        var observation = body.VolcanoObservation!;
        Assert.Null(observation.ColorPlume!.PlumeHeightAboveCrater.Value);
        Assert.Equal("不明", observation.ColorPlume.PlumeHeightAboveCrater.Condition, StringComparer.Ordinal);
        Assert.Null(observation.ColorPlume.PlumeComment);
        var white = observation.WhitePlume!;
        Assert.Equal(600, white.PlumeHeightAboveCrater.Value);
        Assert.Equal(10400, white.PlumeHeightAboveSeaLevel!.Value);
        Assert.Equal("南東", white.PlumeDirection.Value, StringComparer.Ordinal);
        Assert.Equal("2020年5月22日12時から15時の最大噴煙高度の観測時点（14時45分）の噴煙の状況", white.PlumeComment, StringComparer.Ordinal);
    }
}
