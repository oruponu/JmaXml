# 変更履歴

このプロジェクトの主な変更を記録します。書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に、バージョンは[セマンティック バージョニング 2.0.0](https://semver.org/lang/ja/) に従います。

## [未リリース]

## [2.0.0] - 2026-11-20

生成元の辞書と XML スキーマを 2026-11-18 版に更新しました。新設される火山灰警報・注意報の電文を読み込めるようになりました。降灰予報から名称が変わる火山灰予報の電文は、1.0.0 でも読み込めます。同梱した公式サンプル電文 552 件がすべて読み込めることを確認しています。

### 追加

- `Volcanology.Body.AshSources`（火山灰の発生源）と、型 `Volcanology.AshSources`・`Volcanology.AshSource` を追加しました。
- `Volcanology.Item.Area`（対象地域・地点）と `Volcanology.Item.AshDepthCategory`（火山灰の深さ）を追加しました。

### 変更

- **互換性のない変更**：`Volcanology.Item.Areas` を null 許容（`Areas?`）に変更しました。XML スキーマで `Areas` が省略できるようになったためです。`Areas` を参照しているコードには、`null` の場合の処理を追加してください。火山灰警報・注意報の `Item` は `Areas` を持たず、対象の地域を `Area` で表します。
- `Areas` の無い `Item` を読み込んでも `JmaXmlException` にならず、`Areas` は `null` になります。

## [1.0.0] - 2026-10-27

最初のリリースです。生成元の資料は辞書（2026-01-29）と XML スキーマ（2024-10-31）です。同梱した公式サンプル電文 546 件がすべて読み込めることを確認しています。

### 追加

- 気象・地震・火山の内容部と、管理部・ヘッダ部を型で表し、`Report.Parse` で電文を読み込みます。
- 生成元の辞書と XML スキーマの版を `SchemaInfo` で取得できます。

[未リリース]: https://github.com/oruponu/JmaXml/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/oruponu/JmaXml/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/oruponu/JmaXml/releases/tag/v1.0.0
