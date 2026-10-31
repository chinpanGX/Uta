---
title: architecture（実装設計書）の目次
status: draft
updated: 2026-10-31
---

# architecture（実装設計書）の目次

ゲームの仕様（[systems/](../systems/README.md)・[overview/](../overview/README.md)）を、どう作るかを書く。**詳細はここで決める**。レビューは 3 つの観点で回し、ページ全体のレビューは 2 回までを目安にする（[ADR 0017](../decisions/0017-spec-design-plan-stages.md)）。

## 決まり

- **書くこと**: 層と依存、型、RPC・DTO・エラー、テーブル、処理の手順、乱数・状態・再現。根拠として、仕様の節にリンクする
- **書かないこと**: ゲームのルールの説明（仕様にリンクする）。ファイルごとのシグネチャ・DDL・テストの中身は実装計画（[plans/](../../plans/README.md)）
- コードを変えたら、関係するページを同じ変更で更新する
- 同じことを 2 か所に書かない。下の表の「正のページ」に書き、ほかのページはリンクする

## 何の正がどのページか

| 何 | 正のページ |
|---|---|
| リポジトリ構成、ソリューションと版、アセンブリと層の依存、共通基盤（external/）の扱い、Shared のコードの制約、ID・時刻・乱数・テスト・アプリのバージョンの規約、環境とデプロイ、1 番目の計画の中身 | [repository-layout.md](repository-layout.md) |
| VolTech-MagicOnion（サーバー基盤）の構成、フィルター順、エラー、ヘッダー、認証、冪等性、トランザクション | [server-foundation.md](server/server-foundation.md) |
| マスタの定義・生成・配信、列の型と表現の規約、出し分け（Common / ServerOnly） | [master/masterdata-pipeline.md](master/masterdata-pipeline.md) |
| マスタのテーブル・列・区分・検証、全テーブルの索引 | [master/](master/README.md) のシステムごとのページ（`*-tables.md`）。索引は master/README.md |
| RPC の一覧、DTO、エラーの reason、プレイヤーデータのテーブル、ゲームルールの置き場（Shared.Domain / サーバー）、中断・放棄・破棄の流れ | [api-design.md](api-design.md) |
| 画面一覧・遷移、起動フロー、クライアントの Presentation 層 | [screen-design.md](screen-design.md) |
| レッスンの Shared.Domain の型と責務 | [lesson-domain-types.md](lesson-domain-types.md) |
| レッスンの enum の名前・数値と、効果の列の対応 | [lesson-enums.md](lesson-enums.md) |
| レッスンの実装の順番、フェーズごとに変えるコード、テストの置き場、完了条件 | [lesson-phases.md](lesson-phases.md) |
| プロデュースの乱数ストリームと引く順、操作の記録、保存する状態、再現の確かめ方、プロデュースの RPC と DTO | [produce-domain.md](produce-domain.md) |
| ガチャの抽選の実装、ガチャとサポートカードの強化の処理の手順（SQL・トランザクション） | [gacha-support-server.md](server/gacha-support-server.md) |

## 一覧

フォルダ:

- [master/README.md](master/README.md) — マスタ設計：定義・生成・配信と列の規約（masterdata-pipeline.md）、システムごとのテーブルと列、全テーブルの索引
- [server/README.md](server/README.md) — サーバーの実装設計：サーバー基盤（VolTech-MagicOnion）の使い方、ガチャ・サポートカードのサーバー処理

共通の 4 編（[repository-layout.md](repository-layout.md) 1章）。マスター設計編は master/ にある:

- [repository-layout.md](repository-layout.md) — 共通・基盤編：リポジトリ構成、ソリューションと NuGet の版、アセンブリと層の依存ルール、共通基盤（external/）の扱い、Shared のコードの制約、機能ID、共通の規約、環境構成とデプロイ、実装の進め方（1 番目の計画の中身）
- [api-design.md](api-design.md) — API設計編：サービスIF、プレイヤーデータ、ゲームルールの置き場
- [screen-design.md](screen-design.md) — 画面設計編：Presentation 層、画面一覧・遷移、起動フロー

Shared.Domain（`shared/Uta.Domain`）の機能ごとの実装設計:

- [lesson-domain-types.md](lesson-domain-types.md) — レッスンの Shared.Domain の型（`Uta.Domain`）：カード定義・状態・乱数・計算式・エンジンの型と責務、フェーズ1の範囲
- [lesson-enums.md](lesson-enums.md) — レッスンの効果・トリガー・ステータス・コスト・条件の enum の名前・数値と、効果の列の対応（マスタとコードの正）
- [lesson-phases.md](lesson-phases.md) — レッスンの実装フェーズ：順番、フェーズごとに変えるコード、テストのカードの置き場、完了条件
- [produce-domain.md](produce-domain.md) — プロデュースの実装設計：処理の単位、乱数ストリームと引く順、操作の記録、保存する状態、再現の確かめ方、API案

未作成:

- `build-ci.md`（ビルド・CI。repository-layout.md 7章の実体）
- `unity7-evaluation.md`（Unity 7 検証の記録）
