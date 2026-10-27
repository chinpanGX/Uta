---
title: ゲーム概要仕様
status: draft
updated: 2026-10-27
---

# ゲーム概要仕様

Uta 全体のゲームループと、システム（[プロデュース](../systems/produce.md) / [カードロジック](../systems/lesson-card.md) / [育成アイドル・サポートカード](../systems/idol-support.md) / [ガチャ](../systems/gacha.md)）の関係を定義する上位仕様。
詳細は各 `systems/` ページに書き、ここには全体像・範囲・接続だけを置く。

## 目的とスコープ

- 目的: 育成・ガチャのゲームシステム基礎部分を、サーバー権威で縦切り（Vertical Slice）として実装し、技術的に成立することを示す
- 目的（追加）: Unity 7 を早期段階で触る。Unity のバージョンは Unity 7 で確定（2026-10-27）。クライアント基盤・サーバー基盤も Unity 7 で試す（[ADR 0006](../decisions/0006-adopt-unity-7-early.md)）
- 対象: Android / iOS / Windows
- 体制: 個人開発
- 非目標: 商用公開、実課金、大量コンテンツ、ストーリー、ソーシャル機能
- **3Dライブは MVP 対象外**（[ADR 0003](../decisions/0003-defer-3d-live.md)、accepted。[live.md](../systems/live.md) は保留）

## 決まっていること

- 構成: Unity クライアント + ASP.NET Core サーバー（C#）。通信は MagicOnion（[ADR 0002](../decisions/0002-magiconion.md)）
- サーバー権威: 育成計算、ガチャ抽選、所持品更新はサーバー。クライアントは入力と表示に加え、共有ドメインで使用可否の判定と予測の表示まで行う。シードは持たない（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- IP方針: 模倣するのはシステム構造のみ。キャラ・名称・楽曲・UI素材・世界観はオリジナル（[ADR 0001](../decisions/0001-ip-policy.md)）
- 共有型は `shared/` に置く（`Uta.Domain`・`Uta.Shared`）。マスタは `masterdata/` の定義から MasterMemoryAddons でクライアント用・サーバー用に生成する（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- クライアント: オニオンアーキテクチャ、uGUI + UnityScreenNavigator・VContainer・UniTask、縦画面（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)、[ADR 0009](../decisions/0009-client-stack.md)）
- DB: PostgreSQL + Npgsql + Dapper（[ADR 0010](../decisions/0010-database.md)）
- デプロイ: API はコンテナにして Google Cloud の Cloud Run、DB は Cloud SQL（[ADR 0011](../decisions/0011-deploy-google-cloud.md)）
- 実装設計書は4編（[共通・基盤](../architecture/repository-layout.md) / [マスター](../architecture/masterdata-pipeline.md) / [API](../architecture/api-design.md) / [画面](../architecture/screen-design.md)）

## ゲームループ（推測・未確定）

```
 ガチャ ──所持品（キャラ／カード）──▶ 編成 ──▶ プロデュース ──▶ 報酬（疑似通貨）──┐
   ▲                                                                                │
   └────────────────────────────────────────────────────────────────────────────────┘
```

## システム別の役割

| システム | 主な実行場所 | 入力 | 出力 | 詳細 |
|---|---|---|---|---|
| プロデュース | サーバー（計算）＋クライアント（表示） | 編成、各ターンの行動選択 | 育成結果、最終評価、報酬 | [produce.md](../systems/produce.md) |
| ガチャ | サーバー | 排出テーブル、ガチャ通貨 | 所持品の追加、抽選履歴 | [gacha.md](../systems/gacha.md) |
| 育成アイドル・サポートカード | サーバー | ガチャ・強化素材 | 編成に使うカード、強化状態 | [idol-support.md](../systems/idol-support.md) |
| 3Dライブ | （保留） | — | — | [live.md](../systems/live.md) |

## システム間の接続（未決を含む）

- **ガチャ → プロデュース**: 育成アイドルとサポートカードを介して接続。編成は育成アイドル1人+サポートカード6枚（自分の5枚+サーバーのマスタから借りるレンタル1枚）（[idol-support.md](../systems/idol-support.md)）
- **プロデュース → 報酬**: 最終評価に応じて疑似通貨を付与する（推測）
- **通貨**: 課金は実装しない。ガチャ通貨は無償の1種類だけ（決定。[gacha.md](../systems/gacha.md)）

## 最小実装範囲（MVP / 縦切り・推測）

- [ ] ユーザー識別（匿名ログイン相当）でサーバーに状態を保存できる
- [ ] ガチャを1回引いて、所持品が更新される
- [ ] 所持品から編成してプロデュースを開始し、最後まで進めて評価と報酬を受け取れる
- [ ] 途中で中断しても再開できる
- [ ] 上記を Windows / Android / iOS 実機で通す

## 未決事項

- ユーザー認証の方式（匿名デバイスID／アカウント）
- 報酬・疑似通貨の設計（獲得手段、消費先）
- オリジナル名称（プロデュース等の仮称、キャラ、世界観の最小設定）
- マスタを Addressables によるダウンロードに移す時期（いまは同梱。[masterdata-pipeline.md](../architecture/masterdata-pipeline.md) 6章）

## 更新が必要になるページ

この仕様が変わったら: `systems/` 各ページ、`glossary.md`、`index.md`、`log.md`。
