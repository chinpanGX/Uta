---
title: Uta wiki 目次
status: draft
updated: 2026-10-28
---

# Uta wiki 目次

全ページの一覧と一行要約。ページを追加・削除したら必ずここを更新する。

## 概要

- [log.md](log.md) — 時系列の更新履歴
- [glossary.md](glossary.md) — 用語集(仮称を含む)

## 全体仕様

- [overview/game-spec.md](overview/game-spec.md) — ゲーム概要仕様：ゲームループ、システムの関係、MVP範囲、未決事項

## システム仕様

- [systems/produce.md](systems/produce.md) — プロデュース（育成）：週構成、行動、レッスン・試験の値、スコアボーナス、NPC、最終評価、プレイヤーLv
- [systems/lesson-card.md](systems/lesson-card.md) — レッスン・試験のカードロジック：カード定義、コスト、条件、効果、計算式、ターン処理順、初期カードセット、実装フェーズ
- [systems/idol-support.md](systems/idol-support.md) — 育成アイドル・サポートカード：ステータス、初期デッキ、トレーニング・覚醒、アビリティ、イベント、レンタル枠
- [systems/gacha.md](systems/gacha.md) — ガチャ：排出率、天井（交換pt）、重複時の扱い、無償通貨のみ
- [systems/live.md](systems/live.md) — 3Dライブ：**保留（MVP対象外）**

## アーキテクチャ

- [architecture/server-foundation.md](architecture/server-foundation.md) — サーバー基盤（.NET 10、MagicOnion + PostgreSQL）：構成、フィルター順序、エラー、認証・冪等性

### 実装設計書（4編・叩き台）

- [architecture/repository-layout.md](architecture/repository-layout.md) — 共通・基盤編：リポジトリ構成、アセンブリと層の依存ルール、共有コードの制約、機能ID、共通の規約、環境構成とデプロイ
- [architecture/masterdata-pipeline.md](architecture/masterdata-pipeline.md) — マスター設計編：MasterMemoryAddons による定義・生成・配信、列の型の制約と表現の規約
- [architecture/api-design.md](architecture/api-design.md) — API設計編：サービスIF、プレイヤーデータ、ゲームルールの置き場
- [architecture/screen-design.md](architecture/screen-design.md) — 画面設計編：Presentation 層、画面一覧・遷移、起動フロー
- [architecture/lesson-domain-types.md](architecture/lesson-domain-types.md) — レッスンの共有ドメインの型（`Uta.Domain`）：カード定義・状態・乱数・計算式・エンジンの型と責務、フェーズ1の範囲
- （未作成）ビルド・CI

## 決定記録（ADR）

- [decisions/0000-template.md](decisions/0000-template.md) — ADRテンプレート
- [decisions/0001-ip-policy.md](decisions/0001-ip-policy.md) — IP方針：システムのみ模倣、素材・名称はオリジナル
- [decisions/0002-magiconion.md](decisions/0002-magiconion.md) — 通信方式：MagicOnion（gRPC）を採用
- [decisions/0003-defer-3d-live.md](decisions/0003-defer-3d-live.md) — 3DライブをMVPから外す（accepted）
- [decisions/0004-server-foundation.md](decisions/0004-server-foundation.md) — サーバーは .NET 10 と C# サーバー基盤を採用（accepted）
- [decisions/0005-foundation-submodule.md](decisions/0005-foundation-submodule.md) — 基盤と自作パッケージを `external/` に Git サブモジュールで取り込む（proposed。4 件登録済み）
- [decisions/0006-adopt-unity-7-early.md](decisions/0006-adopt-unity-7-early.md) — Unity 7 を早期に触ることを目的に加える。Unity のバージョンは Unity 7 で確定（accepted）
- [decisions/0007-masterdata-with-addons.md](decisions/0007-masterdata-with-addons.md) — マスタを MasterMemoryAddons でクライアント用・サーバー用に出し分ける。列の型は int/long/string/bool/enum のみ、masterdata/ は最上位、CSV が正本（accepted）
- [decisions/0008-shared-domain-and-onion.md](decisions/0008-shared-domain-and-onion.md) — ドメイン層を共有し、クライアントはオニオンアーキテクチャにする。クライアントは判定・予測表示まで、シードは持たない（accepted）
- [decisions/0009-client-stack.md](decisions/0009-client-stack.md) — クライアントは uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面（accepted）
- [decisions/0010-database.md](decisions/0010-database.md) — DB は PostgreSQL + Npgsql + Dapper、セッションは状態を1列にまとめて保存（accepted）
- [decisions/0011-deploy-google-cloud.md](decisions/0011-deploy-google-cloud.md) — API サーバーをコンテナにして Google Cloud（Cloud Run + Cloud SQL）にデプロイする（accepted）

## 一次資料（raw）

- [../raw/2026-10-26-reference-structure-research.md](../raw/2026-10-26-reference-structure-research.md) — 参考作品の構造調査メモ（名称・出典URLは残さない）

## 未作成だが必要なページ

- architecture/build-ci.md（ビルド・CI。repository-layout.md 7章の実体）
- architecture/unity7-evaluation.md（Unity 7 検証の記録）
