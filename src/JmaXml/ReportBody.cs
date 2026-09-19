namespace JmaXml;

/// <summary>
/// 電文の内容部を表します。
/// </summary>
/// <remarks>
/// 実際の型は、気象・地震・火山のいずれかの <c>Body</c> です。
/// </remarks>
public abstract record ReportBody
{
    private protected ReportBody() { }
}
