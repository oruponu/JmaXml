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
        if (_items is null || _count == _items.Length) Grow(r);
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_items!), _count++) = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> ToImmutable(JmaXmlReader r) => _items is null ? [] : Build(r);

    private void Grow(JmaXmlReader r)
    {
        if (_items is null)
        {
            _items = r.RentBuffer(0);
            return;
        }
        var grown = r.RentBuffer(_count * 2);
        Array.Copy(_items, grown, _count);
        Release(r);
        _items = grown;
    }

    private ImmutableArray<T> Build(JmaXmlReader r)
    {
        var result = new T[_count];
        var source = new ReadOnlySpan<object?>(_items, 0, _count);
        var destination = MemoryMarshal.CreateSpan(ref Unsafe.As<T, object?>(ref MemoryMarshal.GetArrayDataReference(result)), result.Length);
        source.CopyTo(destination);
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
