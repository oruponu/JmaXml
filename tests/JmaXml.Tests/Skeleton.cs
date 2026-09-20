namespace JmaXml.Tests;

internal static class Skeleton
{
    public const string Control =
        "<Control><Title>震度速報</Title><DateTime>2026-01-01T00:00:00Z</DateTime><Status>通常</Status>"
        + "<EditorialOffice>気象庁本庁</EditorialOffice><PublishingOffice>気象庁</PublishingOffice></Control>";

    public const string Head =
        "<Head xmlns=\"http://xml.kishou.go.jp/jmaxml1/informationBasis1/\"><Title>T</Title>"
        + "<ReportDateTime>2026-01-01T09:00:00+09:00</ReportDateTime><TargetDateTime>2026-01-01T09:00:00+09:00</TargetDateTime>"
        + "<EventID>1</EventID><InfoType>発表</InfoType><Serial>1</Serial><InfoKind>K</InfoKind><InfoKindVersion>1.0_0</InfoKindVersion>"
        + "<Headline><Text>H</Text></Headline></Head>";

    public static string Report(string body, string control = Control, string head = Head) =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Report xmlns=\"http://xml.kishou.go.jp/jmaxml1/\">\n{control}\n{head}\n{body}\n</Report>";

    public static string SeisBody(string content) =>
        $"<Body xmlns=\"http://xml.kishou.go.jp/jmaxml1/body/seismology1/\" xmlns:jmx_eb=\"http://xml.kishou.go.jp/jmaxml1/elementBasis1/\">{content}</Body>";
}
