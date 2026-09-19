namespace JmaXml;

/// <summary>
/// 電文が気象庁防災情報 XML フォーマットに準拠していない場合にスローされる例外を表します。
/// </summary>
/// <param name="message">エラーの内容を説明するメッセージ。パス・行番号・行内の位置を付加した文字列が <see cref="Exception.Message" /> になります。</param>
/// <param name="path">問題が発生した要素または属性のパス。</param>
/// <param name="lineNumber">問題が発生した位置の行番号。位置情報を取得できない場合は <c>0</c>。</param>
/// <param name="linePosition">問題が発生した行内の位置。位置情報を取得できない場合は <c>0</c>。</param>
/// <param name="innerException">現在の例外の原因となった例外。存在しない場合は <see langword="null" />。</param>
public sealed class JmaXmlException(
    string message,
    string path,
    int lineNumber,
    int linePosition,
    Exception? innerException = null)
    : Exception($"{message} (path: {path}, line: {lineNumber}, position: {linePosition})", innerException)
{
    /// <summary>
    /// 問題が発生した要素または属性のパスを取得します。
    /// </summary>
    /// <remarks>
    /// パスは <c>Report/Body/Intensity/Observation/Pref[2]/Area/Name</c> の形式で表されます。
    /// </remarks>
    public string Path { get; } = path;

    /// <summary>
    /// 問題が発生した位置の行番号を取得します。
    /// 位置情報を取得できない場合は <c>0</c> です。
    /// </summary>
    public int LineNumber { get; } = lineNumber;

    /// <summary>
    /// 問題が発生した行内の位置を取得します。
    /// 位置情報を取得できない場合は <c>0</c> です。
    /// </summary>
    public int LinePosition { get; } = linePosition;
}
