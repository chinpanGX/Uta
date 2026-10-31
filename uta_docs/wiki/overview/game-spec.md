---
title: ゲーム概要仕様
status: draft
updated: 2026-10-31
---

# ゲーム概要仕様

Uta 全体のゲームループと、システム（[プロデュース](../systems/produce.md) / [カードロジック](../systems/lesson-card.md) / [育成アイドル・サポートカード](../systems/idol-support.md) / [ガチャ](../systems/gacha.md)）の関係を定義する上位仕様。
詳細は各 `systems/` ページに書き、ここには全体像・範囲・接続だけを置く。

## 目的とスコープ

- 目的: 育成・ガチャのゲームシステム基礎部分を、サーバー権威で縦切り（Vertical Slice）として実装し、技術的に成立することを示す
- 目的（追加）: Unity 7 を早期段階で触る。Unity のバージョンは Unity 7 で確定（2026-10-27）。クライアント基盤・サーバー基盤も Unity 7 で試す（[ADR 0006](../decisions/0006-adopt-unity-7-early.md)。どこまで Unity 7 で試すかの範囲は ADR 0006 の未決事項）
- 対象: Android / iOS / Windows
- 体制: 個人開発
- 非目標: 商用公開、実課金、大量コンテンツ、ストーリー、ソーシャル機能
- **3Dライブは MVP 対象外**（[ADR 0003](../decisions/0003-defer-3d-live.md)、accepted。[live.md](../systems/live.md) は保留）

## 決まっていること

- 構成: Unity クライアント + ASP.NET Core サーバー（C#、.NET 10。[ADR 0004](../decisions/0004-server-foundation.md)）。通信は MagicOnion（[ADR 0002](../decisions/0002-magiconion.md)）
- サーバー権威: 育成計算、ガチャ抽選、所持品更新はサーバー。クライアントは入力と表示に加え、共有ドメインで使用可否の判定と予測の表示まで行う。シードは持たない（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- IP方針: 模倣するのはシステム構造のみ。キャラ・名称・楽曲・UI素材・世界観はオリジナル（[ADR 0001](../decisions/0001-ip-policy.md)）
- 共有型は `shared/` に置く（`Uta.Domain`・`Uta.Shared`）。マスタは `masterdata/` の定義から MasterMemoryAddons でクライアント用・サーバー用に生成する（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- クライアント: オニオンアーキテクチャ、uGUI + UnityScreenNavigator・VContainer・UniTask、縦画面（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)、[ADR 0009](../decisions/0009-client-stack.md)）
- DB: PostgreSQL + Npgsql + Dapper（[ADR 0010](../decisions/0010-database.md)）。マイグレーションは DbUp（[ADR 0012](../decisions/0012-migration-dbup.md)）
- マスタは CSV が正本で、生成したソースとバイナリもコミットする（[ADR 0007](../decisions/0007-masterdata-with-addons.md)、[ADR 0013](../decisions/0013-commit-masterdata-binary.md)）
- デプロイ: API はコンテナにして Google Cloud の Cloud Run、DB は Cloud SQL（[ADR 0011](../decisions/0011-deploy-google-cloud.md)）
- 実装設計書は4編（[共通・基盤](../architecture/repository-layout.md) / [マスター](../architecture/masterdata-pipeline.md) / [API](../architecture/api-design.md) / [画面](../architecture/screen-design.md)）。補足のページ: [サーバー基盤](../architecture/server-foundation.md)、[レッスンの共有ドメインの型](../architecture/lesson-domain-types.md)、[レッスンの enum](../architecture/lesson-enums.md)、[画面ワイヤー](screen-wireframes.md)

## ゲームループ [決定。骨格は ADR 0003、報酬は 2026-10-28、強化は 2026-10-31]

```
 ガチャ ──所持品（育成アイドル／サポートカード）──▶ 編成 ──▶ プロデュース ──▶ 報酬 ──┐
   ▲                                       ▲                                        │
   │                                       └── 強化（サポートカードのレベル上げ・上限解放）◀─┤
   └──────────────────────────── ガチャ通貨 ◀────────────────────────────────────────┘
```

- 骨格「ガチャ → 編成 → プロデュース → 報酬」は [ADR 0003](../decisions/0003-defer-3d-live.md)。報酬はガチャ通貨・ゴールド・サポート強化pt・プレイヤー経験値（[produce.md](../systems/produce.md) 6.1）
- ガチャ通貨はガチャへ、ゴールドとサポート強化pt はサポートカードの強化へ回る（[idol-support.md](../systems/idol-support.md) 2.2）。経験値はプレイヤーLv（機能の解放）に使う
- 育成アイドルのトレーニング・覚醒は MVP では作らない（次の「最小実装範囲」）

## システム別の役割

| システム | 主な実行場所 | 入力 | 出力 | 詳細 |
|---|---|---|---|---|
| プロデュース | サーバー（計算・確定）＋クライアント（表示、共有ドメインで使用可否の判定と予測） | 編成、各ターンの行動選択 | 育成結果、最終評価、報酬 | [produce.md](../systems/produce.md) |
| ガチャ | サーバー | 排出テーブル、ガチャ通貨 | 所持品の追加、かけら、サポート強化pt、交換pt、抽選履歴 | [gacha.md](../systems/gacha.md) |
| 育成アイドル・サポートカード | サーバー（確定）＋クライアント（強化後の予測、編成の可否） | ガチャの結果、ゴールド・サポート強化pt | 編成に使うカード、強化状態 | [idol-support.md](../systems/idol-support.md) |
| 3Dライブ | （保留） | — | — | [live.md](../systems/live.md) |

## システム間の接続（未決を含む）

- **ガチャ → プロデュース**: 育成アイドルとサポートカードを介して接続。編成は育成アイドル1人+サポートカード6枚（自分の5枚+サーバーのマスタから借りるレンタル1枚）（[idol-support.md](../systems/idol-support.md)）。MVP はレンタルなしの自分の 0〜5 枚
- **プロデュース → 報酬**: 最終評価のランクに応じて、ガチャ通貨・ゴールド・サポート強化pt・プレイヤー経験値を付与する（決定。2026-10-28、強化pt は 2026-10-31。[produce.md](../systems/produce.md) 6.1）
- **通貨**: 課金は実装しない。ガチャ通貨は無償の1種類だけ（決定。[gacha.md](../systems/gacha.md)）。入手は初期付与とプロデュースの報酬（[gacha.md](../systems/gacha.md) 8章）。ほかに持つ値はゴールドとサポート強化pt（報酬で得て、サポートカードの強化に使う）。交換メダルは MVP では作らない
- **初期の所持品** [決定。2026-10-31]: アカウントを作ったとき（端末の登録の後の最初の `SyncAsync`。[api-design.md](../architecture/api-design.md) 1章）に、R の育成アイドル全員、初期サポートカード（マスタで定義した 5 枚）、ガチャ通貨 2,500（仮。10 連 1 回分）を付与する。これでガチャを引かなくても最初のプロデュースを始められる
- **認証**: MVP は基盤の匿名デバイス認証だけで、引き継ぎは作らない（決定。2026-10-28）。アンインストールや機種変更でデータは失われる。プレイヤーと端末は1対多のテーブルにして、あとで引き継ぎコードを足せる形にしておく（[api-design.md](../architecture/api-design.md) 3章）

## 最小実装範囲（MVP）[決定。2026-10-31]

各システムのページの実行要件で「必須」としたものが MVP。それ以外（「次点」「後回し」）は MVP では作らない。仕様はページに残し、MVP の後に作る。

### 完了条件

| # | 条件 | 確かめ方 |
|---|---|---|
| 1 | 匿名デバイス認証でプレイヤーを作り、サーバーに状態を保存できる | ローカルの PostgreSQL（`server/compose.yaml`）に対するサーバーの結合テスト |
| 2 | ガチャを引いて所持品が更新される（単発・10 連・天井の交換） | サーバーの結合テスト。天井は交換pt をマスタで小さくして確かめる |
| 3 | 初期の所持品から編成し、プロデュースを最後まで進めて評価と報酬を受け取れる | 最短シナリオ（下記）で、Windows のクライアントから手で通す。初級の通しは同じ手順で 1 回 |
| 4 | 途中で中断しても再開できる | 3 の途中でアプリを終了して再開する（手動）。状態の再生は `Uta.Domain` の単体テスト（[produce.md](../systems/produce.md) 10.6） |
| 5 | 1〜4 を Windows のビルドで通す | 手動 |

- 実機は段階に分ける: Windows が MVP の条件。Android の実機ビルドはその次、iOS は Unity 7 の対応状況（[ADR 0006](../decisions/0006-adopt-unity-7-early.md)）次第の別の項目にする
- dev 環境（Cloud Run + Cloud SQL）へのデプロイは MVP の条件に含めない別の項目（[repository-layout.md](../architecture/repository-layout.md) 11章）
- **最短シナリオ**: 確認用に、3 週（週 1 レッスン → 週 2 仕上げレッスン → 週 3 最終試験）だけの週構成をマスタに置く。週構成はマスタで定義する（[produce.md](../systems/produce.md) 3.1）ので、コードは変わらない

### MVP に入れるもの・入れないもの

| システム | MVP に入れる | MVP に入れない（次点・後回し） |
|---|---|---|
| プロデュース | 初級（難易度 1 つ）、行動はレッスン・休む・授業・支給、仕上げ、中間・最終試験と再挑戦、最終評価と報酬 | 中級・上級、相談、おでかけ、ドリンク、育成アイテム、特別レッスン、信頼度（0 固定） |
| レッスン | [lesson-card.md](../systems/lesson-card.md) 15章のフェーズ 1〜4 と試験（フェーズ 6） | フェーズ 5（ドリンク・育成アイテム） |
| 育成アイドル | R の初期付与、初期デッキ、パラメータボーナス、アビリティの枠 1 | トレーニング、覚醒、かけらでの解放、最初の SSR の選択 |
| サポートカード | レベル上げ・上限解放、アビリティの枠 1 | サポートイベント、スキルカードサポート、レンタル枠 |
| ガチャ | ガチャ 1 本（期間なし）、単発・10 連、確定枠、天井、重複のかけら変換 | 期間・ピックアップ、交換メダル、ショップ、1 日 1 回割引 |

## 未決事項

- 報酬・初期付与の具体の値（仮の値は [produce.md](../systems/produce.md) 6.1、上の「初期の所持品」）
- オリジナル名称（プロデュース等の仮称、キャラ、世界観の最小設定）。MVP は仮称のままで進められる
- マスタを Addressables によるダウンロードに移す時期（いまは同梱。[masterdata-pipeline.md](../architecture/masterdata-pipeline.md) 6章）。MVP は同梱のままで進められる

## 更新が必要になるページ

この仕様が変わったら: `systems/` 各ページ、`architecture/` の各ページ（特に api-design.md・screen-design.md）、[screen-wireframes.md](screen-wireframes.md)、関係する `uta_docs/plans/` の実装計画、`glossary.md`、`index.md`、`log.md`。
