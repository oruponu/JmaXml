using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JmaXml;

internal struct ArrayBuilder<T> where T : class
{
    private object?[]? _items;
    private int _count;

    public readonly bool IsEmpty => _count == 0;

    public void Add(JmaXmlReader r, T item)
    {
        if (_items is null)
        {
            _items = r.RentBuffer(0);
        }
        else if (_count == _items.Length)
        {
            var grown = r.RentBuffer(_count * 2);
            Array.Copy(_items, grown, _count);
            Release(r);
            _items = grown;
        }
        _items[_count++] = item;
    }

    public ImmutableArray<T> ToImmutable(JmaXmlReader r)
    {
        if (_items is null) return [];
        var result = new T[_count];
        for (var i = 0; i < result.Length; i++) result[i] = Unsafe.As<T>(_items[i]!);
        Release(r);
        _items = null;
        _count = 0;
        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }

    private readonly void Release(JmaXmlReader r)
    {
        Array.Clear(_items!, 0, _count);
        r.ReturnBuffer(_items!);
    }
}
