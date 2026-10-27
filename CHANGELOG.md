# 変更履歴

このプロジェクトの主な変更を記録します。書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に、バージョンは[セマンティック バージョニング 2.0.0](https://semver.org/lang/ja/) に従います。

## [未リリース]

## [1.0.0] - 2026-10-27

最初のリリースです。生成元の資料は辞書（2026-01-29）と XML スキーマ（2024-10-31）です。同梱した公式サンプル電文 546 件がすべて読み込めることを確認しています。

### 追加

- 気象・地震・火山の内容部と、管理部・ヘッダ部を型で表し、`Report.Parse` で電文を読み込みます。
- 生成元の辞書と XML スキーマの版を `SchemaInfo` で取得できます。

[未リリース]: https://github.com/oruponu/JmaXml/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/oruponu/JmaXml/releases/tag/v1.0.0
