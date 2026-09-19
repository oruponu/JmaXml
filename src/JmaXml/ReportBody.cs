namespace JmaXml;

/// <summary>
/// 電文の内容部を表します。
/// </summary>
/// <remarks>
/// 実際の型は、<see cref="Meteorology.Body"/>、<see cref="Seismology.Body"/>、<see cref="Volcanology.Body"/> のいずれかです。
/// </remarks>
public abstract record ReportBody
{
    private protected ReportBody() { }
}
