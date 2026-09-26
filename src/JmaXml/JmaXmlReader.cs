using System.Runtime.InteropServices;
using System.Xml;

namespace JmaXml;

internal sealed partial class JmaXmlReader(XmlReader reader)
{
    private const int LinearSiblingLimit = 16;
    private const int MinBufferLength = 8;

    private readonly XmlReader _reader = reader;
    private readonly IXmlLineInfo? _lineInfo = reader as IXmlLineInfo;
    private readonly List<Segment> _path = [];
    private readonly List<ScopeState> _scopes = [];
    private readonly List<(string Name, int Count)> _siblings = [];
    private readonly List<object?[]> _buffers = [];

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
        _scopes.Add(new ScopeState(_reader.Depth, _reader.IsEmptyElement, line, position, _siblings.Count));
        return new Scope(this);
    }

    public bool NextChild()
    {
        ref var scope = ref CollectionsMarshal.AsSpan(_scopes)[^1];
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
                    PushSegment(ref scope);
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

    public object?[] RentBuffer(int minLength)
    {
        for (var i = _buffers.Count - 1; i >= 0; i--)
        {
            var buffer = _buffers[i];
            if (buffer.Length >= minLength)
            {
                _buffers.RemoveAt(i);
                return buffer;
            }
        }
        return new object?[Math.Max(MinBufferLength, minLength)];
    }

    public void ReturnBuffer(object?[] buffer) => _buffers.Add(buffer);

    public void Once(ref bool seen, string name)
    {
        if (seen) throw Duplicate(name);
        seen = true;
    }

    public JmaXmlException Missing(string name)
    {
        var (line, position) = _scopes.Count > 0 ? (_scopes[^1].Line, _scopes[^1].Position) : Position();
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
        var depth = _scopes.Count > 0 ? _scopes[^1].Depth : int.MaxValue;
        return string.Join("/", _path.Where(s => s.Depth <= depth).Select(s => s.Ordinal >= 2 ? $"{s.Name}[{s.Ordinal}]" : s.Name));
    }

    private void PushSegment(ref ScopeState scope)
    {
        var depth = _reader.Depth;
        var name = _reader.LocalName;
        var ordinal = scope.Siblings is { } siblings ? CountInDictionary(siblings, name) : CountInList(ref scope, name);
        while (_path.Count > 0 && _path[^1].Depth >= depth) _path.RemoveAt(_path.Count - 1);
        _path.Add(new Segment(name, ordinal, depth));
    }

    private int CountInList(ref ScopeState scope, string name)
    {
        for (var i = scope.SiblingStart; i < _siblings.Count; i++)
        {
            if (_siblings[i].Name == name)
            {
                var ordinal = _siblings[i].Count + 1;
                _siblings[i] = (name, ordinal);
                return ordinal;
            }
        }
        if (_siblings.Count - scope.SiblingStart < LinearSiblingLimit)
        {
            _siblings.Add((name, 1));
            return 1;
        }
        var siblings = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = scope.SiblingStart; i < _siblings.Count; i++) siblings[_siblings[i].Name] = _siblings[i].Count;
        _siblings.RemoveRange(scope.SiblingStart, _siblings.Count - scope.SiblingStart);
        scope.Siblings = siblings;
        return CountInDictionary(siblings, name);
    }

    private static int CountInDictionary(Dictionary<string, int> siblings, string name)
    {
        var ordinal = siblings.GetValueOrDefault(name) + 1;
        siblings[name] = ordinal;
        return ordinal;
    }

    private readonly record struct Segment(string Name, int Ordinal, int Depth);

    private struct ScopeState(int depth, bool empty, int line, int position, int siblingStart)
    {
        public int Depth { get; } = depth;
        public bool Empty { get; } = empty;
        public int Line { get; } = line;
        public int Position { get; } = position;
        public bool Entered { get; set; }
        public int SiblingStart { get; } = siblingStart;
        public Dictionary<string, int>? Siblings { get; set; }
    }

    public readonly struct Scope(JmaXmlReader owner) : IDisposable
    {
        public void Dispose()
        {
            var scope = owner._scopes[^1];
            owner._scopes.RemoveAt(owner._scopes.Count - 1);
            owner._siblings.RemoveRange(scope.SiblingStart, owner._siblings.Count - scope.SiblingStart);
        }
    }
}
