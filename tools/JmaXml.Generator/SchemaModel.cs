using System.Collections.Immutable;

namespace JmaXml.Generator;

public enum Primitive
{
    String,
    Token,
    StringList,
    Float,
    NullableFloat,
    Int,
    NullableInt,
    Byte,
    Bool,
    DateTimeOffset,
    Duration,
}

public sealed record TypeRef(string? Namespace, string? TypeName, Primitive? Primitive)
{
    public bool IsComplex => TypeName is not null;

    public static TypeRef Complex(string ns, string name) => new(ns, name, null);

    public static TypeRef Simple(Primitive primitive) => new(null, null, primitive);
}

public sealed record ElementModel(string Name, string Namespace, bool IsRef, string DictKey, TypeRef Type, int MinOccurs, bool Unbounded, bool Nillable)
{
    public string Occurrence => Unbounded ? MinOccurs == 0 ? "*" : "+" : MinOccurs == 0 ? "?" : "1";
}

public sealed record AttributeModel(string Name, Primitive Type, bool Required);

public sealed record TypeModel(string XsdName, string Namespace, Primitive? Content, ImmutableArray<AttributeModel> Attributes, ImmutableArray<ElementModel> Elements);

public sealed record NamespaceModel(string Prefix, string Uri, string? ClassName, ImmutableArray<TypeModel> Types);

public sealed record XsdVersion(string File, string Namespace, string Version, string Date);

public sealed record SchemaModel(ImmutableArray<NamespaceModel> Namespaces, ImmutableArray<XsdVersion> Versions)
{
    public NamespaceModel? Namespace(string prefix) => Namespaces.FirstOrDefault(n => n.Prefix == prefix);

    public TypeModel? Type(string namespaceUri, string xsdName) =>
        Namespaces.FirstOrDefault(n => n.Uri == namespaceUri)?.Types.FirstOrDefault(t => t.XsdName == xsdName);
}

public sealed class SchemaException(string message) : Exception(message);
