using System.Collections.Immutable;

namespace JmaXml.Generator;

public sealed record DictValue(string Value, string Description);

public sealed record DictMember(string Name, string Occurrence, string Meaning, string Description, ImmutableArray<DictValue> Values);

public sealed record DictType(string Name, string Description, ImmutableArray<DictMember> Elements, ImmutableArray<DictMember> Attributes)
{
    public DictMember? Element(string name) => Elements.FirstOrDefault(e => e.Name == name);

    public DictMember? Attribute(string name) => Attributes.FirstOrDefault(a => a.Name == name);
}

public sealed record DictNamespace(string Prefix, ImmutableArray<DictType> Types)
{
    public DictType? Type(string name) => Types.FirstOrDefault(t => t.Name == name);
}

public sealed record DictionaryModel(string Date, ImmutableArray<DictNamespace> Namespaces)
{
    public DictNamespace? Namespace(string prefix) => Namespaces.FirstOrDefault(n => n.Prefix == prefix);
}
