using System.Collections.Immutable;
using System.Text;

namespace JmaXml.Generator;

public static class Naming
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };

    private static readonly HashSet<string> ReservedLocals = new(StringComparer.Ordinal) { "r", "scope", "content" };

    public static string TypeName(string xsdName) => Pascal(xsdName.StartsWith("type.", StringComparison.Ordinal) ? xsdName[5..] : xsdName);

    public static string PropertyName(string xmlName) => Pascal(xmlName);

    public static string LocalName(string xmlName)
    {
        var name = char.ToLowerInvariant(xmlName[0]) + xmlName[1..];
        if (ReservedLocals.Contains(name)) return name + "Value";
        return Keywords.Contains(name) ? "@" + name : name;
    }

    public static ImmutableArray<string> Validate(SchemaModel schema)
    {
        var errors = new List<string>();
        foreach (var ns in schema.Namespaces)
        {
            var typeNames = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var type in ns.Types)
            {
                var typeName = TypeName(type.XsdName);
                if (!typeNames.TryAdd(typeName, type.XsdName))
                {
                    errors.Add($"{ns.Prefix}: {type.XsdName} and {typeNames[typeName]} both become {typeName}");
                }
                if (!IsValidIdentifier(typeName))
                {
                    errors.Add($"{ns.Prefix}: {type.XsdName} becomes {typeName}, which is not a valid C# identifier");
                }
                if (ns.ClassName is null)
                {
                    var container = schema.Namespaces.FirstOrDefault(c => c.ClassName == typeName);
                    if (container is not null)
                    {
                        errors.Add($"{ns.Prefix}: {type.XsdName} becomes {typeName}, same as the {container.Prefix} container class");
                    }
                }
                var members = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var attribute in type.Attributes) CheckMember(errors, ns.Prefix, type.XsdName, typeName, members, PropertyName(attribute.Name), "attribute " + attribute.Name);
                if (type.Content is not null) CheckMember(errors, ns.Prefix, type.XsdName, typeName, members, "Value", "the content value");
                foreach (var element in type.Elements) CheckMember(errors, ns.Prefix, type.XsdName, typeName, members, PropertyName(element.Name), "element " + element.Name);
            }
        }
        return [.. errors];
    }

    private static void CheckMember(List<string> errors, string prefix, string xsdName, string typeName, Dictionary<string, string> members, string property, string what)
    {
        if (property == typeName) errors.Add($"{prefix}:{xsdName}: {what} becomes property {property}, same as the type name");
        if (!members.TryAdd(property, what)) errors.Add($"{prefix}:{xsdName}: {what} and {members[property]} both become {property}");
        if (!IsValidIdentifier(property)) errors.Add($"{prefix}:{xsdName}: {what} becomes property {property}, which is not a valid C# identifier");
    }

    private static bool IsValidIdentifier(string name) =>
        name.Length > 0
        && (char.IsLetter(name[0]) || name[0] == '_')
        && name.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');

    private static string Pascal(string s)
    {
        var name = char.ToUpperInvariant(s[0]) + s[1..];
        var result = new StringBuilder(name.Length);
        for (var i = 0; i < name.Length;)
        {
            if (!char.IsUpper(name[i]))
            {
                result.Append(name[i++]);
                continue;
            }
            var end = i + 1;
            while (end < name.Length && char.IsUpper(name[end])) end++;
            if (end - i > 1 && end < name.Length && char.IsLower(name[end])) end--;
            result.Append(Acronym(name[i..end]));
            i = end;
        }
        return result.ToString();
    }

    private static string Acronym(string word) => word switch
    {
        "ID" => "Id",
        { Length: >= 3 } => word[0] + word[1..].ToLowerInvariant(),
        _ => word,
    };
}
