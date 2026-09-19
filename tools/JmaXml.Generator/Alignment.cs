using System.Collections.Immutable;

namespace JmaXml.Generator;

public sealed record AlignmentResult(ImmutableArray<string> Errors, ImmutableArray<string> Warnings);

public static class Alignment
{
    public static AlignmentResult Check(SchemaModel schema, DictionaryModel dictionary)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        foreach (var ns in schema.Namespaces)
        {
            var dictNamespace = dictionary.Namespace(ns.Prefix);
            if (dictNamespace is null)
            {
                errors.Add($"{ns.Prefix}: sheet is missing from the dictionary");
                continue;
            }
            foreach (var type in ns.Types)
            {
                var where = $"{ns.Prefix}:{type.XsdName}";
                var dictType = dictNamespace.Type(type.XsdName);
                if (dictType is null)
                {
                    errors.Add($"{where}: not in the dictionary");
                    continue;
                }
                foreach (var element in type.Elements)
                {
                    var member = dictType.Element(element.DictKey);
                    if (member is null)
                    {
                        errors.Add($"{where}/{element.DictKey}: not in the dictionary");
                        continue;
                    }
                    var occurrence = member.Occurrence.Replace("(nil)", "", StringComparison.Ordinal).Trim();
                    if (occurrence != element.Occurrence)
                    {
                        errors.Add($"{where}/{element.DictKey}: occurrence {element.Occurrence} in XSD but {member.Occurrence} in the dictionary");
                    }
                }
                foreach (var attribute in type.Attributes)
                {
                    if (dictType.Attribute(attribute.Name) is null) errors.Add($"{where}/@{attribute.Name}: not in the dictionary");
                }
                foreach (var member in dictType.Elements)
                {
                    if (type.Elements.All(e => e.DictKey != member.Name)) warnings.Add($"{where}/{member.Name}: in the dictionary but not in the XSD");
                }
                foreach (var member in dictType.Attributes)
                {
                    if (type.Attributes.All(a => a.Name != member.Name)) warnings.Add($"{where}/@{member.Name}: in the dictionary but not in the XSD");
                }
            }
            foreach (var dictType in dictNamespace.Types)
            {
                if (ns.Types.All(t => t.XsdName != dictType.Name)) warnings.Add($"{ns.Prefix}:{dictType.Name}: in the dictionary but not in the XSD");
            }
        }
        return new AlignmentResult([.. errors], [.. warnings]);
    }
}
