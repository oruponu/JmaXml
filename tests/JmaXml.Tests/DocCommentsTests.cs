using JmaXml.Generator;

namespace JmaXml.Tests;

public class DocCommentsTests
{
    [Fact]
    public void Meaning_becomes_summary_and_description_becomes_remarks()
    {
        var member = new DictMember("Int", "?", "震度階級", "震度を示す(1～4、5-、5+、6-、6+、7)", []);
        Assert.Equal(
            ["/// <summary>震度階級</summary>", "/// <remarks>", "/// <para>震度を示す(1～4、5-、5+、6-、6+、7)</para>", "/// </remarks>"],
            DocComments.ForMember(member, "Int"));
    }

    [Fact]
    public void Redundant_description_is_dropped()
    {
        var member = new DictMember("Name", "1", "観測点名", "観測点名を示す", []);
        Assert.Equal(["/// <summary>観測点名</summary>"], DocComments.ForMember(member, "Name"));
    }

    [Fact]
    public void Values_are_listed_without_quotes_and_the_wildcard_is_skipped()
    {
        var member = new DictMember("type", "?", "分類", "時刻の分類を示す。", [new("\"実況\"", "分類が\"実況\"であることを示す。"), new("*", "＜任意の文字列＞")]);
        Assert.Equal(
            [
                "/// <summary>分類</summary>",
                "/// <remarks>",
                "/// <para>時刻の分類を示す。</para>",
                "/// <list type=\"bullet\">",
                "/// <item><description>実況: 分類が&quot;実況&quot;であることを示す。</description></item>",
                "/// </list>",
                "/// </remarks>",
            ],
            DocComments.ForMember(member, "type"));
    }

    [Fact]
    public void Text_is_xml_escaped_and_multiline_becomes_paragraphs()
    {
        var member = new DictMember("X", "1", "a < b\nc & d\ne ' f", "", []);
        Assert.Equal(
            ["/// <summary>", "/// <para>a &lt; b</para>", "/// <para>c &amp; d</para>", "/// <para>e &apos; f</para>", "/// </summary>"],
            DocComments.ForMember(member, "X"));
    }

    [Fact]
    public void Missing_member_uses_the_fallback()
    {
        Assert.Equal(["/// <summary>Foo</summary>"], DocComments.ForMember(null, "Foo"));
    }

    [Fact]
    public void Generic_type_description_uses_the_fallback()
    {
        var type = new DictType("type.Body", "Body型の要素を示す", [], []);
        Assert.Equal(["/// <summary>内容部</summary>"], DocComments.ForType(type, "内容部"));
        var described = new DictType("type.IntensityStation", "各観測点単位の震度・長周期地震動階級を示す", [], []);
        Assert.Equal(["/// <summary>各観測点単位の震度・長周期地震動階級を示す</summary>"], DocComments.ForType(described, "x"));
    }
}
