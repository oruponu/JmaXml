using JmaXml.Generator;

namespace JmaXml.Tests;

public class DictionaryReaderTests
{
    private static readonly DictionaryModel Model = DictionaryReader.Read(Fixtures.DictionaryPath);

    [Fact]
    public void Date_comes_from_the_file_name() => Assert.Equal("2026-01-29", Model.Date);

    [Fact]
    public void All_six_sheets_are_read()
    {
        Assert.Equal(["jmx", "jmx_ib", "jmx_eb", "jmx_mete", "jmx_seis", "jmx_volc"], Model.Namespaces.Select(n => n.Prefix));
        Assert.All(Model.Namespaces, n => Assert.NotEmpty(n.Types));
    }

    [Fact]
    public void IntensityStation_type_resolves_with_well_formed_elements()
    {
        var type = Model.Namespace("jmx_seis")!.Type("type.IntensityStation")!;
        Assert.NotEmpty(type.Elements);
        Assert.All(type.Elements, e => Assert.NotEmpty(e.Name));
        Assert.All(type.Elements, e => Assert.NotEmpty(e.Occurrence));
        var intElement = type.Element("Int");
        Assert.NotNull(intElement);
        Assert.NotEmpty(intElement.Meaning);
        Assert.NotEmpty(type.Description);
    }

    [Fact]
    public void Referenced_elements_keep_their_prefix()
    {
        var magnitude = Model.Namespace("jmx_seis")!.Type("type.Earthquake")!.Element("jmx_eb:Magnitude");
        Assert.NotNull(magnitude);
        Assert.StartsWith("jmx_eb:", magnitude.Name, StringComparison.Ordinal);
        Assert.NotEmpty(magnitude.Occurrence);
        Assert.NotEmpty(magnitude.Meaning);
    }

    [Fact]
    public void Values_are_attached_to_the_preceding_member()
    {
        var type = Model.Namespace("jmx_eb")!.Type("type.DateTime")!.Attribute("type");
        Assert.NotNull(type);
        Assert.True(type.Values.Length >= 20);
        Assert.All(type.Values, v => Assert.NotEmpty(v.Value));
    }

    [Fact]
    public void Parses_a_small_sheet()
    {
        string[][] rows =
        [
            [], [], [],
            ["項番", "親要素", "子要素", "属性", "基底型", "サイズ", "出現回数", "意味", "とりうる値", "解説"],
            ["1", "(element)", "Body", "", "type.Body", "", "1", "内容部", "", ""],
            ["2", "type.Body", "", "", "", "", "", "", "", "Body型の要素を示す"],
            ["3", "", "Naming", "", "type.Naming", "", "?", "命名要素", "", "命名に関する要素を示す"],
            ["4", "", "jmx_eb:Magnitude", "", "jmx_eb:type.Magnitude", "", "+", "マグニチュード", "", ""],
            ["5", "type.Magnitude", "", "", "xs:float", "", "", "", "", "（一般）マグニチュード"],
            ["6", "", "", "type", "xs:string", "10", "1", "種類", "", ""],
            ["7", "", "", "", "*", "", "", "", "\"Mj\"", "気象庁マグニチュード"],
        ];
        var types = DictionaryReader.ParseSheet("t", rows);
        Assert.Equal(2, types.Length);
        Assert.Equal(["Naming", "jmx_eb:Magnitude"], types[0].Elements.Select(e => e.Name));
        Assert.Equal("+", types[0].Elements[1].Occurrence);
        Assert.Equal("（一般）マグニチュード", types[1].Description);
        var value = Assert.Single(types[1].Attribute("type")!.Values);
        Assert.Equal("\"Mj\"", value.Value);
        Assert.Equal("気象庁マグニチュード", value.Description);
    }

    [Fact]
    public void Header_mismatch_throws()
    {
        string[][] rows = [[], [], [], ["a"]];
        Assert.Throws<InvalidDataException>(() => DictionaryReader.ParseSheet("x", rows));
    }

    [Fact]
    public void Unknown_parent_element_marker_throws()
    {
        string[][] rows =
        [
            [], [], [],
            ["項番", "親要素", "子要素", "属性", "基底型", "サイズ", "出現回数", "意味", "とりうる値", "解説"],
            ["1", "(bogus)", "Body", "", "type.Body", "", "1", "内容部", "", ""],
        ];
        Assert.Throws<InvalidDataException>(() => DictionaryReader.ParseSheet("t", rows));
    }
}
