using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace JmaXml.Generator;

public static partial class SchemaLoader
{
    private const string AdditionNamespace = "http://xml.kishou.go.jp/jmaxml1/addition1/";

    public static readonly ImmutableArray<(string Prefix, string Uri, string? ClassName)> KnownNamespaces =
    [
        ("jmx", "http://xml.kishou.go.jp/jmaxml1/", null),
        ("jmx_ib", "http://xml.kishou.go.jp/jmaxml1/informationBasis1/", "InformationBasis"),
        ("jmx_eb", "http://xml.kishou.go.jp/jmaxml1/elementBasis1/", "ElementBasis"),
        ("jmx_mete", "http://xml.kishou.go.jp/jmaxml1/body/meteorology1/", "Meteorology"),
        ("jmx_seis", "http://xml.kishou.go.jp/jmaxml1/body/seismology1/", "Seismology"),
        ("jmx_volc", "http://xml.kishou.go.jp/jmaxml1/body/volcanology1/", "Volcanology"),
    ];

    public static SchemaModel Load(string xsdDir)
    {
        var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        var problems = new List<string>();
        set.ValidationEventHandler += (_, e) => problems.Add($"{e.Severity}: {e.Message}");
        set.Add(null, Path.Combine(xsdDir, "jmx.xsd"));
        set.Compile();
        if (problems.Count > 0) throw new SchemaException(string.Join(Environment.NewLine, problems));

        var errors = new List<string>();
        var complexTypes = set.GlobalTypes.Values.OfType<XmlSchemaComplexType>()
            .Where(t => t.QualifiedName.Namespace != XmlSchema.Namespace)
            .OrderBy(t => t.SourceUri, StringComparer.Ordinal).ThenBy(t => t.LineNumber).ToList();
        foreach (var type in complexTypes.Where(t => KnownNamespaces.All(n => n.Uri != t.QualifiedName.Namespace)))
        {
            errors.Add($"{type.QualifiedName}: type in an unknown namespace");
        }
        var namespaces = KnownNamespaces
            .Select(ns => new NamespaceModel(ns.Prefix, ns.Uri, ns.ClassName,
                [.. complexTypes.Where(t => t.QualifiedName.Namespace == ns.Uri).Select(t => ToType(set, t, errors))]))
            .ToImmutableArray();
        if (errors.Count > 0) throw new SchemaException(string.Join(Environment.NewLine, errors));
        return new SchemaModel(namespaces, ReadVersions(set));
    }

    internal static string PrefixOf(string uri)
    {
        foreach (var ns in KnownNamespaces)
        {
            if (ns.Uri == uri) return ns.Prefix;
        }
        throw new SchemaException($"{uri}: unknown namespace");
    }

    private static TypeModel ToType(XmlSchemaSet set, XmlSchemaComplexType type, List<string> errors)
    {
        var name = $"{PrefixOf(type.QualifiedName.Namespace)}:{type.QualifiedName.Name}";
        if (type.IsMixed) errors.Add($"{name}: mixed content is not supported");
        Primitive? content = null;
        if (type.ContentModel is XmlSchemaSimpleContent)
        {
            if (type.BaseXmlSchemaType is XmlSchemaSimpleType baseType) content = MapSimple(baseType, errors, name);
            else errors.Add($"{name}: simpleContent without a simple base type");
        }
        var attributes = type.AttributeUses.Values.OfType<XmlSchemaAttribute>()
            .OrderBy(a => a.LineNumber).ThenBy(a => a.LinePosition)
            .Select(a => new AttributeModel(
                a.QualifiedName.Name,
                MapSimple(a.AttributeSchemaType!, errors, $"{name}/@{a.QualifiedName.Name}") ?? Primitive.String,
                a.Use == XmlSchemaUse.Required))
            .ToImmutableArray();
        var elements = ImmutableArray.CreateBuilder<ElementModel>();
        switch (type.Particle)
        {
            case null:
                break;
            case XmlSchemaSequence sequence:
                foreach (var item in sequence.Items)
                {
                    switch (item)
                    {
                        case XmlSchemaElement element:
                            elements.Add(ToElement(set, element, errors, name));
                            break;
                        case XmlSchemaAny { Namespace: AdditionNamespace or "##other" }:
                            break;
                        default:
                            errors.Add($"{name}: unsupported particle {item.GetType().Name}");
                            break;
                    }
                }
                break;
            default:
                errors.Add($"{name}: unsupported particle {type.Particle.GetType().Name}");
                break;
        }
        return new TypeModel(type.QualifiedName.Name, type.QualifiedName.Namespace, content, attributes, elements.ToImmutable());
    }

    private static ElementModel ToElement(XmlSchemaSet set, XmlSchemaElement element, List<string> errors, string owner)
    {
        var isRef = !element.RefName.IsEmpty;
        var name = element.QualifiedName.Name;
        var ns = element.QualifiedName.Namespace;
        var where = $"{owner}/{name}";
        var unbounded = element.MaxOccursString == "unbounded";
        if (!unbounded && element.MaxOccurs != 1) errors.Add($"{where}: maxOccurs {element.MaxOccursString} is not supported");
        if ((int)element.MinOccurs is not (0 or 1)) errors.Add($"{where}: minOccurs {element.MinOccurs} is not supported");
        var declaration = isRef ? (XmlSchemaElement?)set.GlobalElements[element.RefName] ?? element : element;
        TypeRef type;
        switch (declaration.ElementSchemaType)
        {
            case XmlSchemaComplexType { QualifiedName.IsEmpty: false } complex:
                type = TypeRef.Complex(complex.QualifiedName.Namespace, complex.QualifiedName.Name);
                break;
            case XmlSchemaSimpleType simple:
                type = TypeRef.Simple(MapSimple(simple, errors, where) ?? Primitive.String);
                break;
            default:
                errors.Add($"{where}: anonymous or missing type");
                type = TypeRef.Simple(Primitive.String);
                break;
        }
        var dictKey = isRef ? $"{PrefixOf(ns)}:{name}" : name;
        return new ElementModel(name, ns, isRef, dictKey, type, (int)element.MinOccurs, unbounded, element.IsNillable);
    }

    private static Primitive? MapSimple(XmlSchemaSimpleType type, List<string> errors, string where)
    {
        if (type.QualifiedName.Namespace == XmlSchema.Namespace)
        {
            switch (type.TypeCode)
            {
                case XmlTypeCode.String or XmlTypeCode.NormalizedString or XmlTypeCode.AnyUri or XmlTypeCode.GMonthDay:
                    return Primitive.String;
                case XmlTypeCode.Token:
                    return Primitive.Token;
                case XmlTypeCode.Float:
                    return Primitive.Float;
                case XmlTypeCode.Int or XmlTypeCode.Integer:
                    return Primitive.Int;
                case XmlTypeCode.UnsignedByte:
                    return Primitive.Byte;
                case XmlTypeCode.Boolean:
                    return Primitive.Bool;
                case XmlTypeCode.DateTime:
                    return Primitive.DateTimeOffset;
                case XmlTypeCode.Duration:
                    return Primitive.Duration;
                default:
                    errors.Add($"{where}: unsupported built-in type {type.QualifiedName.Name}");
                    return null;
            }
        }
        switch (type.Content)
        {
            case XmlSchemaSimpleTypeRestriction when type.BaseXmlSchemaType is XmlSchemaSimpleType baseType:
                return MapSimple(baseType, errors, where);
            case XmlSchemaSimpleTypeList { BaseItemType: { } item }:
                if (MapSimple(item, errors, where) == Primitive.String) return Primitive.StringList;
                errors.Add($"{where}: list of non-string items is not supported");
                return null;
            case XmlSchemaSimpleTypeUnion union:
                var members = union.BaseMemberTypes ?? [];
                var mapped = members.Select(m => MapSimple(m, errors, where)).OfType<Primitive>().Distinct().ToList();
                if (mapped.Count == 1) return mapped[0];
                if (mapped.Count == 2 && mapped.Contains(Primitive.String) && members.Any(IsEmptyStringEnumeration))
                {
                    var other = mapped.First(p => p != Primitive.String);
                    if (other == Primitive.Float) return Primitive.NullableFloat;
                    if (other == Primitive.Int) return Primitive.NullableInt;
                }
                errors.Add($"{where}: unsupported union");
                return null;
            default:
                errors.Add($"{where}: unsupported simple type");
                return null;
        }
    }

    private static bool IsEmptyStringEnumeration(XmlSchemaSimpleType type) =>
        type.Content is XmlSchemaSimpleTypeRestriction restriction
        && restriction.Facets.OfType<XmlSchemaEnumerationFacet>().Select(f => f.Value ?? "").ToArray() is [""];

    private static ImmutableArray<XsdVersion> ReadVersions(XmlSchemaSet set)
    {
        var versions = new List<XsdVersion>();
        foreach (var schema in set.Schemas().Cast<XmlSchema>().OrderBy(s => s.SourceUri, StringComparer.Ordinal))
        {
            var text = string.Concat(schema.Items.OfType<XmlSchemaAnnotation>()
                .SelectMany(a => a.Items.OfType<XmlSchemaDocumentation>())
                .SelectMany(d => d.Markup ?? [])
                .Select(n => n?.InnerText ?? ""));
            var latest = VersionPattern().Matches(text.Replace('　', ' '))
                .Select(m => (
                    Date: $"{m.Groups[1].Value}-{int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture):00}-{int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture):00}",
                    Version: m.Groups[4].Value))
                .OrderBy(v => v.Date, StringComparer.Ordinal)
                .LastOrDefault();
            if (latest.Date is null) continue;
            var file = Path.GetFileName(new Uri(schema.SourceUri!).LocalPath);
            versions.Add(new XsdVersion(file, schema.TargetNamespace ?? "", latest.Version, latest.Date));
        }
        return [.. versions];
    }

    [GeneratedRegex(@"(\d{4})年(\d{1,2})月(\d{1,2})日\s*Ver\.([0-9A-Za-z.]+)")]
    private static partial Regex VersionPattern();
}
