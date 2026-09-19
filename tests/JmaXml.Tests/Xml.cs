using System.Xml;

namespace JmaXml.Tests;

internal static class Xml
{
    public static XmlReader Reader(string xml) => XmlReader.Create(new StringReader(xml), new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = false,
    });
}
