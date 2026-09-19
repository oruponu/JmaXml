using JmaXml.Generator;

namespace JmaXml.Tests;

public class XlsxReaderTests
{
    [Fact]
    public void Reads_cells_by_row_and_column_and_fills_gaps()
    {
        using var xlsx = TestXlsx.Build(new Dictionary<string, string[][]>
        {
            ["s1"] = [["a", "", "c"], [], ["", "b"]],
            ["s2"] = [["x"]],
        });
        var sheets = XlsxReader.ReadSheets(xlsx, ["s1", "s2"]);
        Assert.Equal(["a", "", "c"], sheets["s1"][0]);
        Assert.Equal(["", "", ""], sheets["s1"][1]);
        Assert.Equal(["", "b", ""], sheets["s1"][2]);
        Assert.Equal(["x"], sheets["s2"][0]);
    }

    [Fact]
    public void Inline_string_drops_furigana_and_keeps_the_visible_text()
    {
        using var xlsx = TestXlsx.BuildRaw("s1",
            "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>項番</t><rPh sb=\"0\" eb=\"1\"><t>コウ</t></rPh><rPh sb=\"1\" eb=\"2\"><t>バン</t></rPh></is></c></row>");
        var sheets = XlsxReader.ReadSheets(xlsx, ["s1"]);
        Assert.Equal("項番", sheets["s1"][0][0]);
    }

    [Fact]
    public void Multi_run_shared_string_concatenates_runs_and_drops_furigana()
    {
        using var xlsx = TestXlsx.BuildRaw("s1",
            "<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c></row>",
            "<si><r><t>ab</t></r><r><t>cd</t></r><rPh sb=\"0\" eb=\"2\"><t>zz</t></rPh><phoneticPr fontId=\"1\"/></si>");
        var sheets = XlsxReader.ReadSheets(xlsx, ["s1"]);
        Assert.Equal("abcd", sheets["s1"][0][0]);
    }

    [Fact]
    public void Reads_multi_letter_column_references()
    {
        var row = Enumerable.Repeat("", 26).Append("aa").ToArray();
        using var xlsx = TestXlsx.Build(new Dictionary<string, string[][]> { ["s1"] = [row] });
        var sheets = XlsxReader.ReadSheets(xlsx, ["s1"]);
        Assert.Equal(27, sheets["s1"][0].Length);
        Assert.Equal("aa", sheets["s1"][0][26]);
    }

    [Fact]
    public void Missing_sheet_throws()
    {
        using var xlsx = TestXlsx.Build(new Dictionary<string, string[][]> { ["s1"] = [["a"]] });
        Assert.Throws<InvalidDataException>(() => XlsxReader.ReadSheets(xlsx, ["nope"]));
    }

    [Fact]
    public void Cell_without_a_reference_attribute_throws_InvalidDataException()
    {
        using var xlsx = TestXlsx.BuildRaw("s1", "<row r=\"1\"><c t=\"str\"><v>x</v></c></row>");
        Assert.Throws<InvalidDataException>(() => XlsxReader.ReadSheets(xlsx, ["s1"]));
    }

    [Fact]
    public void Official_dictionary_has_the_expected_sheets_and_header()
    {
        using var stream = File.OpenRead(Fixtures.DictionaryPath);
        var sheets = XlsxReader.ReadSheets(stream, ["jmx", "jmx_ib", "jmx_eb", "jmx_mete", "jmx_seis", "jmx_volc"]);
        Assert.Equal(
            ["項番", "親要素", "子要素", "属性", "基底型", "サイズ", "出現回数", "意味", "とりうる値", "解説"],
            sheets["jmx_seis"][3].Take(10).Select(c => c.Trim()));
        Assert.True(sheets["jmx_mete"].Length > 1400);
    }
}
