using System.Xml;

namespace JmaXml;

internal sealed partial class JmaXmlReader(XmlReader reader)
{
    private readonly XmlReader _reader = reader;
    private readonly IXmlLineInfo? _lineInfo = reader as IXmlLineInfo;
    private readonly List<Segment> _path = [];
    private readonly Stack<ScopeState> _scopes = new();

    public string LocalName => _reader.LocalName;

    public string NamespaceUri => _reader.NamespaceURI;

    public bool InNamespace(string ns) => _reader.NamespaceURI == ns;

    public string Path => string.Join("/", _path.Select(s => s.Ordinal >= 2 ? $"{s.Name}[{s.Ordinal}]" : s.Name));

    public void MoveToElement(string name, string ns)
    {
        while (_reader.NodeType != XmlNodeType.Element)
        {
            if (!_reader.Read()) throw Error($"Element '{name}' not found");
        }
        if (_reader.LocalName != name || _reader.NamespaceURI != ns)
        {
            throw Error($"Expected element '{name}' in namespace '{ns}' but found '{_reader.LocalName}' in namespace '{_reader.NamespaceURI}'");
        }
        _path.Clear();
        _path.Add(new Segment(name, 1, _reader.Depth));
    }

    public Scope Enter()
    {
        var (line, position) = Position();
        _scopes.Push(new ScopeState(_reader.Depth, _reader.IsEmptyElement, line, position));
        return new Scope(this);
    }

    public bool NextChild()
    {
        var scope = _scopes.Peek();
        if (!scope.Entered)
        {
            scope.Entered = true;
            _reader.Read();
            if (scope.Empty) return false;
        }
        while (true)
        {
            switch (_reader.NodeType)
            {
                case XmlNodeType.Element:
                    PushSegment(scope);
                    return true;
                case XmlNodeType.EndElement when _reader.Depth == scope.Depth:
                    _reader.Read();
                    return false;
                case XmlNodeType.None:
                    throw Error("Unexpected end of document");
                default:
                    _reader.Read();
                    break;
            }
        }
    }

    public void Skip() => _reader.Skip();

    public void Once(ref bool seen, string name)
    {
        if (seen) throw Duplicate(name);
        seen = true;
    }

    public JmaXmlException Missing(string name)
    {
        var (line, position) = _scopes.TryPeek(out var scope) ? (scope.Line, scope.Position) : Position();
        return new JmaXmlException($"Required element '{name}' is missing", ScopePath() + "/" + name, line, position);
    }

    public JmaXmlException MissingAttribute(string name) => Error($"Required attribute '{name}' is missing", Path + "/@" + name);

    public JmaXmlException Duplicate(string name) => Error($"Element '{name}' occurs more than once");

    public JmaXmlException Error(string message, Exception? inner = null) => Error(message, Path, inner);

    private JmaXmlException Error(string message, string path, Exception? inner = null)
    {
        var (line, position) = Position();
        return new JmaXmlException(message, path, line, position, inner);
    }

    private (int Line, int Position) Position() =>
        _lineInfo is { } info && info.HasLineInfo() ? (info.LineNumber, info.LinePosition) : (0, 0);

    private string ScopePath()
    {
        var depth = _scopes.Count > 0 ? _scopes.Peek().Depth : int.MaxValue;
        return string.Join("/", _path.Where(s => s.Depth <= depth).Select(s => s.Ordinal >= 2 ? $"{s.Name}[{s.Ordinal}]" : s.Name));
    }

    private void PushSegment(ScopeState scope)
    {
        var depth = _reader.Depth;
        var name = _reader.LocalName;
        scope.Siblings ??= new Dictionary<string, int>(StringComparer.Ordinal);
        var ordinal = scope.Siblings.GetValueOrDefault(name) + 1;
        scope.Siblings[name] = ordinal;
        while (_path.Count > 0 && _path[^1].Depth >= depth) _path.RemoveAt(_path.Count - 1);
        _path.Add(new Segment(name, ordinal, depth));
    }

    private readonly record struct Segment(string Name, int Ordinal, int Depth);

    private sealed class ScopeState(int depth, bool empty, int line, int position)
    {
        public int Depth { get; } = depth;
        public bool Empty { get; } = empty;
        public int Line { get; } = line;
        public int Position { get; } = position;
        public bool Entered { get; set; }
        public Dictionary<string, int>? Siblings { get; set; }
    }

    public readonly struct Scope(JmaXmlReader owner) : IDisposable
    {
        public void Dispose() => owner._scopes.Pop();
    }
}
