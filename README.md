# JmaXml

気象庁防災情報XMLの電文を、公式の XML スキーマと辞書から生成した型に読み込む .NET ライブラリ

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Status](https://img.shields.io/badge/status-WIP-yellow)

A .NET library that parses JMA (Japan Meteorological Agency) disaster information XML messages into strongly typed, immutable models generated from the official XML schemas and data dictionary. It covers every body type (meteorology, seismology and volcanology), uses a forward-only `XmlReader` without building a DOM, and is Native AOT- and trim-compatible. This is not an official JMA product. The documentation below is in Japanese.

## 概要

JmaXml は気象庁が公開している[気象庁防災情報XMLフォーマット](https://xml.kishou.go.jp/)の XML スキーマ（XSD）と辞書から、型とパーサーを自動生成しています。

- **気象・地震・火山の電文に対応**：気象・地震・火山の 3 種類の内容部（`Body`）と、共通の管理部（`Control`）・ヘッダ部（`Head`）を型で表します。同梱した公式サンプル電文がすべて読み込めることを、テストで確認しています。
- **イミュータブルな型**：`sealed record` と `init` 専用プロパティで構成され、コレクションには `ImmutableArray<T>` を採用しています。XSD の出現回数と nillable の指定をプロパティの型に反映しているため、必須の要素は nillable なものを除いて非 null として扱えます。
- **辞書由来のドキュメントコメント**：各型・プロパティに辞書の名称と説明が付いており、IDE で要素の意味を確認できます。
- **DOM を構築しない**：`XmlReader` で電文を先頭から 1 回だけ走査し、生成した型に直接読み込みます。
- **Native AOT とトリミングに対応**：実行時にリフレクションを使用せず、生成したコードで値を読み込みます。
- **診断しやすい例外**：必須の要素・属性が無い電文や値を変換できない電文を読み込むと、問題の位置を示すパス・行番号・行内の位置を持つ `JmaXmlException` をスローします。

> [!NOTE]
> 本ライブラリは気象庁の公式ソフトウェアではありません。本ライブラリについて、気象庁へのお問い合わせはご遠慮ください。

## 対象外の機能

- XML の書き出し
- 個別コード表（観測点・予報区・震央地名など）によるコードの変換
- XSD による検証
- `xs:any`（`addition1`）の内容の読み込み
- BUFR 形式の電文の読み込み
- 非同期 API
  - ネットワークから受信する場合は、電文全体を非同期に受信してから `Report.Parse` に渡してください。

## システム要件

- [.NET 10](https://dotnet.microsoft.com/download) 以上

## インストール

```bash
dotnet add package JmaXml
```

## 使い方

`Report.Parse` で電文を読み込み、`report.Body` の型をパターンマッチングで判別して内容部ごとに処理します。次の例では、同梱したサンプル電文 [`32-35_01_03_240613_VXSE53.xml`](tests/JmaXml.Tests/fixtures/samples/32-35_01_03_240613_VXSE53.xml) を読み込みます。ファイルのパスは、電文の保存先に合わせて変更してください。

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

```text
震源・震度情報
震央地名：駿河湾
マグニチュード：Ｍ５．９
最大震度：5-
```

`Report.Parse` は XML 文字列（`string`）・`Stream`・`TextReader`・`XmlReader` を受け取ります。`string` には、ファイルのパスではなく電文の XML そのものを渡します。

`Control.Parse`・`InformationBasis.Head.Parse`・`Seismology.Body.Parse`（`Meteorology`・`Volcanology` も同様）を使うと、電文の一部だけを `XmlReader` から読み込めます。

## XSD と C# の対応

XML 名前空間ごとに静的クラスを設け、型をその入れ子の型として定義しています。ただし、`http://xml.kishou.go.jp/jmaxml1/` の型（`Report` と `Control`）は `JmaXml` 名前空間の直下にあります。

| XML 名前空間 | C# |
|---|---|
| `http://xml.kishou.go.jp/jmaxml1/` | `JmaXml.Report`、`JmaXml.Control` |
| `http://xml.kishou.go.jp/jmaxml1/informationBasis1/` | `JmaXml.InformationBasis.Head` など |
| `http://xml.kishou.go.jp/jmaxml1/elementBasis1/` | `JmaXml.ElementBasis.Magnitude` など |
| `http://xml.kishou.go.jp/jmaxml1/body/meteorology1/` | `JmaXml.Meteorology.Body` など |
| `http://xml.kishou.go.jp/jmaxml1/body/seismology1/` | `JmaXml.Seismology.Body` など |
| `http://xml.kishou.go.jp/jmaxml1/body/volcanology1/` | `JmaXml.Volcanology.Body` など |

型名とプロパティ名は、.NET の命名規則に合わせたパスカルケースです。

- **型名**：XSD の型名から `type.` を除いて変換します（`type.volcanoInfoContent` → `VolcanoInfoContent`）。
- **プロパティ名**：要素名・属性名を変換します（`codeType` → `CodeType`）。
- **略語**：3 文字以上の頭字語は先頭だけを大文字にします（`URI` → `Uri`）。2 文字の頭字語はそのままです（`TargetDTDubious`）。ただし、`ID` は `Id` にします（`EventID` → `EventId`）。

| XSD | C# |
|---|---|
| 要素 `1..1` | `required` の非 null プロパティ（nillable な要素は `required` の無い `T?`） |
| 要素 `0..1` | `T?` |
| 要素 `0..*`・`1..*` | `ImmutableArray<T>`（要素が無ければ空。`1..*` は、読み込みに成功すれば 1 件以上） |
| 値と属性を持つ要素（`simpleContent`） | `Value` プロパティと属性のプロパティ |
| 属性 `required` | 非 null |
| 属性 `optional` | `T?` |
| `xs:string`、`xs:token`、`xs:anyURI`、`xs:gMonthDay`、列挙 | `string`（列挙は `enum` にせず、列挙された値かどうかも検査しません） |
| `xs:dateTime` | `DateTimeOffset`（nillable なら `DateTimeOffset?`） |
| `xs:float` | `float` |
| `jmx_eb:nullablefloat` | `float?` |
| `xs:int` | `int` |
| `jmx_eb:nullableinteger` | `int?` |
| `xs:unsignedByte` | `byte` |
| `xs:boolean` | `bool` |
| `xs:duration` | `TimeSpan` |
| `xs:list` | `ImmutableArray<string>` |
| `xs:any`（`addition1`） | プロパティを生成しません（内容は読み飛ばします） |

## 読み込みの規則

XSD による検証は行わず、次の「例外」の表にある項目だけを検査します。そのため、読み込みに成功しても電文が XSD のすべての制約を満たすとは限りません。

- 子要素の順序は検査しません。
- 将来的に要素や属性が追加されても読み込みが失敗しないように、未知の属性を無視します。XSD で子要素を持つと定義された要素では、未知の子要素も無視します。
- `xsi:nil="true"` は、nillable な要素（`Head/TargetDateTime`）だけ `null` にします。nillable な要素も XSD で必須であれば省略できず、要素が無い電文は例外になります。
- 空の要素は、`xs:string` なら空文字列、`jmx_eb:nullablefloat`・`jmx_eb:nullableinteger` なら `null` です。それ以外の数値・日時の要素が空の場合は、例外になります。
- `xs:string` は、空白や改行を除去・置換せずに返します。ただし、改行は `XmlReader` が XML の仕様に従って LF に正規化します。`xs:token` は前後の空白を除き、連続する空白を半角スペース 1 つにまとめます。
- 数値・日時・期間・真偽値は、カルチャに依存しない `XmlConvert` で変換します。

## 例外

| 状況 | 例外 |
|---|---|
| XML として壊れている | `System.Xml.XmlException` |
| ルート要素が `Report` でない | `JmaXmlException` |
| `Control`・`Head`・`Body` が無い、または 2 回以上出現する | `JmaXmlException` |
| `Body` の名前空間が不明 | `JmaXmlException` |
| 必須の要素・属性が無い（`1..*` の要素が 0 件の場合を含む） | `JmaXmlException` |
| `0..1`・`1..1` の要素が 2 回以上出現する | `JmaXmlException` |
| 値だけを持つ要素の中に子要素がある | `JmaXmlException` |
| 数値・日時・期間・真偽値に変換できない、日時にタイムゾーンのオフセットが無い | `JmaXmlException`（`InnerException` に `FormatException` または `OverflowException`） |

`JmaXmlException` は問題の位置を `Path`・`LineNumber`・`LinePosition` で示します。行情報を取得できない `XmlReader` を渡した場合、`LineNumber` と `LinePosition` は `0` です。`Path` は `Report/Body/Intensity/Observation/Pref[2]/Area/Name` の形式です。属性は `.../Magnitude/@type` のように表します。

```csharp
try
{
    var report = Report.Parse(xml);
}
catch (JmaXmlException e)
{
    Console.WriteLine($"{e.Path} ({e.LineNumber}:{e.LinePosition})");
}
```

## 注意事項

- **入力の扱い**：渡された `Stream`・`TextReader`・`XmlReader` は、読み込みの成否にかかわらず閉じません。破棄は呼び出し側で行ってください。`string`・`Stream`・`TextReader` を渡した場合は、ライブラリが DTD の処理を禁止した `XmlReader` を作ります。一方、`XmlReader` を受け取る `Parse`（`Report.Parse`・`Control.Parse` など）は、渡された `XmlReader` の設定をそのまま使います。
  - 呼び出し側で作成した `XmlReader` で信頼できない入力を読み込む場合は、`DtdProcessing.Prohibit` を設定して DTD の処理を禁止してください。DTD を悪用した攻撃（外部エンティティの参照や、エンティティの展開による大量のメモリ消費）を防ぐためです。
  - `IgnoreWhitespace = true` の `XmlReader` を渡すと、空白だけの文字列が空文字列になることがあります。空白だけの文字列も保持する場合は、`IgnoreWhitespace = false` にしてください。
  - `XmlReader` は対象の要素（`Report`・`Control` など）の開始タグか、そこまでの間に他の要素が無い手前の位置で渡してください。
  - 読み込みに成功すると、`XmlReader` は対象の要素の終了タグの次のノードに位置し、それ以降は読みません。後続のノードが無ければ、入力の末尾に到達した状態になります。
- **`ImmutableArray<T>` への `default` の代入**：ライブラリが返す `ImmutableArray<T>` は常に初期化済み（`IsDefault` が `false`）です。`with` 式などで `default` を代入すると、`foreach` での列挙や `Length` の参照で例外になります。
- **スレッド安全性**：`Report.Parse` は状態を持たないため、別々の入力に対して複数のスレッドから同時に呼び出せます。返されるインスタンスはイミュータブルなため、複数のスレッドで共有できます。ただし、1 つの `Stream`・`TextReader`・`XmlReader` を複数の呼び出しで共有しないでください。

## バージョン

[セマンティック バージョニング 2.0.0](https://semver.org/lang/ja/)に従います。

生成元の辞書と XSD の版は、`SchemaInfo.DictionaryDate` と `SchemaInfo.Schemas` から取得できます。

## 開発

```bash
git clone https://github.com/oruponu/JmaXml.git
cd JmaXml
dotnet build
dotnet test
```

`src/JmaXml/Generated/` のコードは、`schema/` の XSD と辞書から `tools/JmaXml.Generator` で自動生成しています。生成したコードもリポジトリに含めています。

```bash
# コードを再生成する
dotnet run --project tools/JmaXml.Generator

# 生成したコードが最新か確認する
dotnet run --project tools/JmaXml.Generator -- --check
```

公開 API は `src/JmaXml/PublicAPI.Shipped.txt`（公開済み）と `src/JmaXml/PublicAPI.Unshipped.txt`（未公開）に記録しています。一覧に無い公開 API があるとビルドが失敗します。生成したコードは `dotnet format` で一覧に追加できないため、次のスクリプトで追加します。ビルドが失敗した場合や、警告の文面を解釈できなかった場合は、一覧を変更せずに終了します。

```bash
bash tools/update-public-api.sh
```

公開 API を削除または変更すると、変更前の API について RS0017 でビルドが失敗します。対処は、変更前の API が公開済みかどうかで異なります。

- `PublicAPI.Unshipped.txt` にある未公開の API：その行を削除します。
- `PublicAPI.Shipped.txt` にある公開済みの API：互換性のない変更なので、メジャーバージョンを上げます。`PublicAPI.Shipped.txt` は変更せず、先頭に `*REMOVED*` を付けた行を `PublicAPI.Unshipped.txt` に追加します。

API を変更した場合は、変更後の API について RS0016 も出るので、上のスクリプトで一覧に追加します。

気象庁が辞書や XSD を改版したときは、`schema/` のファイルを差し替えて再生成し、スクリプトで公開 API の一覧を更新してから、生成したコードと一覧の差分を確認してください。

## 出典とライセンス

JmaXml は [MIT License](LICENSE) で公開しています。ただし、次のものは MIT License の対象外で、公共データ利用規約（第1.0版）に準拠した[気象庁ホームページの利用規約](https://www.jma.go.jp/jma/kishou/info/coment.html)に従います。

- `schema/` に同梱した XSD と辞書
- `tests/JmaXml.Tests/fixtures/samples/` に同梱したサンプル電文
- `src/JmaXml/Generated/` のドキュメントコメント（NuGet パッケージに含まれる XML ドキュメントを含む）

ドキュメントコメントは、気象庁「[気象庁防災情報XMLフォーマット 辞書](https://xml.kishou.go.jp/tec_material.html)」を加工して作成しています。同梱した資料の取得元と日付は [`schema/README.md`](schema/README.md) を参照してください。

出典：[気象庁ホームページ](https://xml.kishou.go.jp/tec_material.html)
