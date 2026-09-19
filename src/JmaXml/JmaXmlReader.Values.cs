using System.Collections.Immutable;
using System.Xml;
using System.Xml.Schema;

namespace JmaXml;

internal sealed partial class JmaXmlReader
{
    private static readonly char[] XmlWhitespace = [' ', '\t', '\r', '\n'];

    public string ReadString() => ReadContent();

    public string ReadToken() => Collapse(ReadContent());

    public ImmutableArray<string> ReadList() => [.. ReadContent().Split(XmlWhitespace, StringSplitOptions.RemoveEmptyEntries)];

    public float ReadFloat() => ConvertContent(XmlConvert.ToSingle);

    public float? ReadNullableFloat() => ConvertNullableContent(XmlConvert.ToSingle);

    public int ReadInt() => ConvertContent(XmlConvert.ToInt32);

    public int? ReadNullableInt() => ConvertNullableContent(XmlConvert.ToInt32);

    public byte ReadByte() => ConvertContent(XmlConvert.ToByte);

    public bool ReadBool() => ConvertContent(XmlConvert.ToBoolean);

    public DateTimeOffset ReadDateTimeOffset() => ConvertContent(ToDateTimeOffset);

    public DateTimeOffset? ReadNullableDateTimeOffset()
    {
        if (IsNil())
        {
            _reader.Skip();
            return null;
        }
        return ReadDateTimeOffset();
    }

    public TimeSpan ReadDuration() => ConvertContent(XmlConvert.ToTimeSpan);

    public string? AttributeString(string name) => _reader.GetAttribute(name);

    public string RequiredAttributeString(string name) => _reader.GetAttribute(name) ?? throw MissingAttribute(name);

    public string? AttributeToken(string name) => _reader.GetAttribute(name) is { } s ? Collapse(s) : null;

    public string RequiredAttributeToken(string name) => Collapse(RequiredAttributeString(name));

    public float? AttributeFloat(string name) => ConvertAttribute(name, XmlConvert.ToSingle);

    public float RequiredAttributeFloat(string name) => ConvertRequiredAttribute(name, XmlConvert.ToSingle);

    public int? AttributeInt(string name) => ConvertAttribute(name, XmlConvert.ToInt32);

    public int RequiredAttributeInt(string name) => ConvertRequiredAttribute(name, XmlConvert.ToInt32);

    public byte? AttributeByte(string name) => ConvertAttribute(name, XmlConvert.ToByte);

    public byte RequiredAttributeByte(string name) => ConvertRequiredAttribute(name, XmlConvert.ToByte);

    public bool? AttributeBool(string name) => ConvertAttribute(name, XmlConvert.ToBoolean);

    public bool RequiredAttributeBool(string name) => ConvertRequiredAttribute(name, XmlConvert.ToBoolean);

    private string ReadContent()
    {
        var (line, position) = Position();
        try
        {
            return _reader.ReadElementContentAsString();
        }
        catch (XmlException e) when (_reader.ReadState != ReadState.Error)
        {
            throw new JmaXmlException("Element content could not be read as text", Path, line, position, e);
        }
    }

    private T ConvertContent<T>(Func<string, T> convert)
    {
        var (line, position) = Position();
        var text = ReadContent();
        return ConvertValue(text, convert, Path, line, position);
    }

    private T? ConvertNullableContent<T>(Func<string, T> convert) where T : struct
    {
        var (line, position) = Position();
        var text = ReadContent();
        return text.Length == 0 ? null : ConvertValue(text, convert, Path, line, position);
    }

    private T? ConvertAttribute<T>(string name, Func<string, T> convert) where T : struct
    {
        var text = _reader.GetAttribute(name);
        if (text is null) return null;
        var (line, position) = Position();
        return ConvertValue(text, convert, Path + "/@" + name, line, position);
    }

    private T ConvertRequiredAttribute<T>(string name, Func<string, T> convert)
    {
        var text = _reader.GetAttribute(name) ?? throw MissingAttribute(name);
        var (line, position) = Position();
        return ConvertValue(text, convert, Path + "/@" + name, line, position);
    }

    private static T ConvertValue<T>(string text, Func<string, T> convert, string path, int line, int position)
    {
        try
        {
            return convert(text);
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
        {
            throw new JmaXmlException($"Value '{text}' could not be converted", path, line, position, e);
        }
    }

    private bool IsNil()
    {
        var text = _reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
        if (text is null) return false;
        var (line, position) = Position();
        return ConvertValue(text, XmlConvert.ToBoolean, Path + "/@nil", line, position);
    }

    private static DateTimeOffset ToDateTimeOffset(string text)
    {
        var value = text.Trim(XmlWhitespace);
        if (!HasOffset(value)) throw new FormatException("The dateTime value has no time zone offset");
        return XmlConvert.ToDateTimeOffset(value);
    }

    private static bool HasOffset(string s) =>
        s.EndsWith('Z') || (s.Length >= 6 && (s[^6] == '+' || s[^6] == '-') && s[^3] == ':');

    private static string Collapse(string s) => string.Join(' ', s.Split(XmlWhitespace, StringSplitOptions.RemoveEmptyEntries));
}
