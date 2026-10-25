---
title: Uta wiki 目次
status: draft
updated: 2026-10-26
---

# Uta wiki 目次

全ページの一覧と一行要約。ページを追加・削除したら必ずここを更新する。

## 概要

- [log.md](log.md) — 時系列の更新履歴

## 全体仕様

- [overview/game-spec.md](overview/game-spec.md) — ゲーム概要仕様：ゲームループ、システムの関係、MVP範囲、未決事項

## システム仕様

- [systems/produce.md](systems/produce.md) — プロデュース（育成）：ターン・カード・計算式・画面ワイヤーの叩き台
- [systems/gacha.md](systems/gacha.md) — ガチャ：排出テーブル、天井、サーバー抽選、所持品反映
- [systems/live.md](systems/live.md) — 3Dライブ：**保留（MVP対象外）**

## アーキテクチャ

- [architecture/server-foundation.md](architecture/server-foundation.md) — サーバー基盤（.NET 10、MagicOnion + PostgreSQL）：構成、フィルター順序、エラー、認証・冪等性
- （未作成）リポジトリ構成 / マスタデータ配信 / ビルド・CI

## 決定記録（ADR）

- [decisions/0000-template.md](decisions/0000-template.md) — ADRテンプレート
- [decisions/0001-ip-policy.md](decisions/0001-ip-policy.md) — IP方針：システムのみ模倣、素材・名称はオリジナル
- [decisions/0002-magiconion.md](decisions/0002-magiconion.md) — 通信方式：MagicOnion（gRPC）を採用
- [decisions/0003-defer-3d-live.md](decisions/0003-defer-3d-live.md) — 3DライブをMVPから外す（accepted）
- [decisions/0004-server-foundation.md](decisions/0004-server-foundation.md) — サーバーは .NET 10 と C# サーバー基盤を採用（accepted）
- [decisions/0005-foundation-submodule.md](decisions/0005-foundation-submodule.md) — 基盤と自作パッケージを `external/` に Git サブモジュールで取り込む（proposed。4 件登録済み）
- [decisions/0006-adopt-unity-7-early.md](decisions/0006-adopt-unity-7-early.md) — Unity 7 を早期に触ることを目的に加える（proposed）

## 未作成だが必要なページ

- glossary.md（用語集）
- architecture/repository-layout.md
- architecture/masterdata-pipeline.md
- architecture/unity7-evaluation.md（Unity 7 検証の記録）
