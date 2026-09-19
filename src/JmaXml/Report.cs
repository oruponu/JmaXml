using System.Xml;

namespace JmaXml;

/// <summary>
/// 気象庁防災情報 XML の電文を表します。
/// </summary>
public sealed record Report
{
    private static readonly XmlReaderSettings Settings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = false,
        CloseInput = false,
    };

    /// <summary>
    /// 管理部を取得します。
    /// </summary>
    public required Control Control { get; init; }

    /// <summary>
    /// ヘッダ部を取得します。
    /// </summary>
    public required InformationBasis.Head Head { get; init; }

    /// <summary>
    /// 内容部を取得します。
    /// </summary>
    /// <remarks>
    /// 実際の型は、<see cref="Meteorology.Body"/>、<see cref="Seismology.Body"/>、<see cref="Volcanology.Body"/> のいずれかです。
    /// </remarks>
    public required ReportBody Body { get; init; }

    /// <summary>
    /// XML 文字列から電文を読み込みます。
    /// </summary>
    /// <param name="xml">電文全体を表す XML 文字列。</param>
    /// <returns>読み込んだ電文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> が <see langword="null" /> です。</exception>
    /// <exception cref="XmlException">XML の解析でエラーが発生しました。</exception>
    /// <exception cref="JmaXmlException">電文を読み込めません。</exception>
    public static Report Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        using var reader = XmlReader.Create(new StringReader(xml), Settings);
        return ParseDocument(reader);
    }

    /// <summary>
    /// 指定した <see cref="Stream"/> から電文を読み込みます。
    /// </summary>
    /// <param name="stream">電文全体を含む <see cref="Stream"/>。</param>
    /// <returns>読み込んだ電文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> が <see langword="null" /> です。</exception>
    /// <exception cref="XmlException">XML の解析でエラーが発生しました。</exception>
    /// <exception cref="JmaXmlException">電文を読み込めません。</exception>
    /// <remarks>
    /// 読み込み後も <paramref name="stream"/> は閉じません。
    /// </remarks>
    public static Report Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = XmlReader.Create(stream, Settings);
        return ParseDocument(reader);
    }

    /// <summary>
    /// 指定した <see cref="TextReader"/> から電文を読み込みます。
    /// </summary>
    /// <param name="textReader">電文全体を返す <see cref="TextReader"/>。</param>
    /// <returns>読み込んだ電文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="textReader"/> が <see langword="null" /> です。</exception>
    /// <exception cref="XmlException">XML の解析でエラーが発生しました。</exception>
    /// <exception cref="JmaXmlException">電文を読み込めません。</exception>
    /// <remarks>
    /// 読み込み後も <paramref name="textReader"/> は閉じません。
    /// </remarks>
    public static Report Parse(TextReader textReader)
    {
        ArgumentNullException.ThrowIfNull(textReader);
        using var reader = XmlReader.Create(textReader, Settings);
        return ParseDocument(reader);
    }

    /// <summary>
    /// 指定した <see cref="XmlReader"/> から電文を読み込みます。
    /// </summary>
    /// <param name="reader"><c>Report</c> 要素、またはその手前の位置にある <see cref="XmlReader"/>。呼び出し元が生成した設定をそのまま使用します。</param>
    /// <returns>読み込んだ電文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> が <see langword="null" /> です。</exception>
    /// <exception cref="XmlException">XML の解析でエラーが発生しました。</exception>
    /// <exception cref="JmaXmlException">電文を読み込めません。</exception>
    /// <remarks>
    /// 読み込みに成功すると、<paramref name="reader"/> は <c>Report</c> の終了タグの次のノードに位置します。
    /// </remarks>
    public static Report Parse(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var r = new JmaXmlReader(reader);
        r.MoveToElement("Report", XmlNamespaces.Jmx);
        return Read(r);
    }

    private static Report ParseDocument(XmlReader reader)
    {
        var report = Parse(reader);
        while (reader.Read())
        {
        }
        return report;
    }

    private static Report Read(JmaXmlReader r)
    {
        Control? control = null;
        var controlSeen = false;
        InformationBasis.Head? head = null;
        var headSeen = false;
        ReportBody? body = null;
        var bodySeen = false;
        using var scope = r.Enter();
        while (r.NextChild())
        {
            switch (r.LocalName)
            {
                case "Control" when r.InNamespace(XmlNamespaces.Jmx):
                    r.Once(ref controlSeen, "Control");
                    control = Control.Read(r);
                    break;
                case "Head" when r.InNamespace(XmlNamespaces.InformationBasis):
                    r.Once(ref headSeen, "Head");
                    head = InformationBasis.Head.Read(r);
                    break;
                case "Body" when r.InNamespace(XmlNamespaces.Meteorology):
                    r.Once(ref bodySeen, "Body");
                    body = Meteorology.Body.Read(r);
                    break;
                case "Body" when r.InNamespace(XmlNamespaces.Seismology):
                    r.Once(ref bodySeen, "Body");
                    body = Seismology.Body.Read(r);
                    break;
                case "Body" when r.InNamespace(XmlNamespaces.Volcanology):
                    r.Once(ref bodySeen, "Body");
                    body = Volcanology.Body.Read(r);
                    break;
                case "Body":
                    throw r.Error($"Unknown Body namespace '{r.NamespaceUri}'");
                default:
                    r.Skip();
                    break;
            }
        }
        return new Report
        {
            Control = control ?? throw r.Missing("Control"),
            Head = head ?? throw r.Missing("Head"),
            Body = body ?? throw r.Missing("Body"),
        };
    }
}
