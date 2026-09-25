# JmaXml

気象庁防災情報XMLの電文を、公式の XML スキーマと辞書から生成した型に読み込む .NET ライブラリです。

A .NET library that parses JMA (Japan Meteorological Agency) disaster information XML messages into strongly typed, immutable models generated from the official XML schemas and data dictionary. This is not an official JMA product.

本ライブラリは気象庁の公式ソフトウェアではありません。本ライブラリについて、気象庁へのお問い合わせはご遠慮ください。

## 使い方

`Report.Parse` で電文を読み込みます。内容部（`report.Body`）の型はパターンマッチングで判別し、気象・地震・火山ごとに処理を分けます。次の例では、リポジトリに同梱した[震源・震度に関する情報のサンプル電文](https://github.com/oruponu/JmaXml/blob/main/tests/JmaXml.Tests/fixtures/samples/32-35_01_03_240613_VXSE53.xml)を読み込みます。ファイルのパスは、電文の保存先に合わせて変更してください。

```csharp
using JmaXml;

using var stream = File.OpenRead("32-35_01_03_240613_VXSE53.xml");
var report = Report.Parse(stream);

Console.WriteLine(report.Head.Title);

switch (report.Body)
{
    case Seismology.Body seismology:
        foreach (var earthquake in seismology.Earthquake)
        {
            Console.WriteLine($"震央地名：{earthquake.Hypocenter?.Area.Name}");
            Console.WriteLine($"マグニチュード：{earthquake.Magnitude[0].Description}");
        }
        Console.WriteLine($"最大震度：{seismology.Intensity?.Observation?.MaxInt}");
        break;
    case Meteorology.Body meteorology:
        // 気象の内容部
        break;
    case Volcanology.Body volcanology:
        // 火山の内容部
        break;
}
```

XSD と C# の対応、読み込みの規則、例外、注意事項は [GitHub の README](https://github.com/oruponu/JmaXml#readme) を参照してください。

## 出典とライセンス

JmaXml は [MIT License](https://github.com/oruponu/JmaXml/blob/main/LICENSE) で公開しています。ただし、このパッケージに含まれる XML ドキュメント（`JmaXml.xml`）のうち、生成した型のドキュメントコメントは MIT License の対象外で、公共データ利用規約（第1.0版）に準拠した[気象庁ホームページの利用規約](https://www.jma.go.jp/jma/kishou/info/coment.html)に従います。

生成した型のドキュメントコメントは、気象庁「[気象庁防災情報XMLフォーマット 辞書](https://xml.kishou.go.jp/tec_material.html)」を加工して作成しています。

出典：[気象庁ホームページ](https://xml.kishou.go.jp/tec_material.html)
