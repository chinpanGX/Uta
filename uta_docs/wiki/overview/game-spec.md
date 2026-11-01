---
title: ゲーム概要仕様
status: draft
updated: 2026-11-01
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
- サーバー権威: 育成計算、ガチャ抽選、所持品更新はサーバー。クライアントは入力と表示に加え、Shared.Domain で使用可否の判定と予測の表示まで行う。シードは持たない（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）
- IP方針: 模倣するのはシステム構造のみ。キャラ・名称・楽曲・UI素材・世界観はオリジナル（[ADR 0001](../decisions/0001-ip-policy.md)）。ゲームシステムの数値は参考作品の値を使う（[ADR 0016](../decisions/0016-reference-game-values.md)）
- Shared の型は `shared/` に置く（`Uta.Shared.Domain`・`Uta.Shared.Contracts`）。マスタは `masterdata/` の定義から MasterMemoryAddons でクライアント用・サーバー用に生成する（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- クライアント: オニオンアーキテクチャ、uGUI + UnityScreenNavigator・VContainer・UniTask、縦画面（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)、[ADR 0009](../decisions/0009-client-stack.md)）
- DB: PostgreSQL + Npgsql + Dapper（[ADR 0010](../decisions/0010-database.md)）。マイグレーションは DbUp（[ADR 0012](../decisions/0012-migration-dbup.md)）
- マスタは CSV が正本で、生成したソースとバイナリもコミットする（[ADR 0007](../decisions/0007-masterdata-with-addons.md)、[ADR 0013](../decisions/0013-commit-masterdata-binary.md)）
- デプロイ: API はコンテナにして Google Cloud の Cloud Run、DB は Cloud SQL（[ADR 0011](../decisions/0011-deploy-google-cloud.md)）
- 実装設計書は4編（[共通・基盤](../architecture/repository-layout.md) / [マスター](../architecture/master/masterdata-pipeline.md) / [API](../architecture/api-design.md) / [画面](../architecture/screen-design.md)）。補足のページ: [サーバー基盤](../architecture/server/server-foundation.md)、[レッスンの Shared.Domain の型](../architecture/lesson-domain-types.md)、[レッスンの enum](../architecture/lesson-enums.md)、[画面ワイヤー](screen-wireframes.md)

## ゲームループ [決定。骨格は ADR 0003、報酬は 2026-10-28、強化は 2026-10-31]

```
 ガチャ ──所持品（育成アイドル／サポートカード）──▶ 編成 ──▶ プロデュース ──▶ 報酬 ──┐
   ▲                                       ▲                                        │
   │                                       └── 強化（サポートカードのレベル上げ・上限解放）◀─┤
   └──────────────────────────── ガチャ通貨 ◀────────────────────────────────────────┘
```

- 骨格「ガチャ → 編成 → プロデュース → 報酬」は [ADR 0003](../decisions/0003-defer-3d-live.md)。報酬はガチャ通貨・ゴールド・サポート強化pt・プレイヤー経験値（[produce.md](../systems/produce.md) 6.1）
- ガチャ通貨はガチャへ、ゴールドとサポート強化pt はサポートカードの強化へ回る（[idol-support.md](../systems/idol-support.md) 2.2）。経験値はプレイヤーLv に使う（MVP ではカードの解放 `unlockLv` だけ。機能の解放は MVP の後）
- 育成アイドルのトレーニング・覚醒は MVP では作らない（次の「最小実装範囲」）

## システム別の役割

| システム | 主な実行場所 | 入力 | 出力 | 詳細 |
|---|---|---|---|---|
| プロデュース | サーバー（計算・確定）＋クライアント（表示、Shared.Domain で使用可否の判定と予測） | 編成、各ターンの行動選択 | 育成結果、最終評価、報酬 | [produce.md](../systems/produce.md) |
| ガチャ | サーバー | 排出テーブル、ガチャ通貨 | 所持品の追加、かけら、サポート強化pt、交換pt、抽選履歴 | [gacha.md](../systems/gacha.md) |
| 育成アイドル・サポートカード | サーバー（確定）＋クライアント（強化後の予測、編成の可否） | ガチャの結果、ゴールド・サポート強化pt | 編成に使うカード、強化状態 | [idol-support.md](../systems/idol-support.md) |
| 3Dライブ | （保留） | — | — | [live.md](../systems/live.md) |

## システム間の接続（未決を含む）

- **ガチャ → プロデュース**: 育成アイドルとサポートカードを介して接続。編成は育成アイドル1人+サポートカード6枚（自分の5枚+サーバーのマスタから借りるレンタル1枚）（[idol-support.md](../systems/idol-support.md)）。MVP はレンタルなしの自分の 0〜5 枚
- **プロデュース → 報酬**: 最終評価のランクに応じて、ガチャ通貨・ゴールド・サポート強化pt・プレイヤー経験値を付与する（決定。2026-10-28、強化pt は 2026-10-31。[produce.md](../systems/produce.md) 6.1）
- **通貨**: 課金は実装しない。ガチャ通貨は無償の1種類だけ（決定。[gacha.md](../systems/gacha.md)）。入手は初期付与とプロデュースの報酬（[gacha.md](../systems/gacha.md) 8章）。ほかに持つ値はゴールドとサポート強化pt（報酬で得て、サポートカードの強化に使う）。交換メダルは MVP では作らない
- **初期の所持品** [決定。2026-10-31]: アカウントを作ったとき（端末の登録の後の `SignUpAsync`。[api-design.md](../architecture/api-design.md) 1章）に、R の育成アイドル全員、初期サポートカード（マスタで定義した 5 種。カード ID はすべて別）、ガチャ通貨 2,500・ゴールド 10,000・サポート強化pt 10,000（どれもマスタの値。初期値。ゴールドと強化pt は、最初のプロデュースの前に強化を試せるように置く [仮]）を付与する。置き場のマスタは [idol-support-tables.md](../architecture/master/idol-support-tables.md)。これでガチャを引かなくても最初のプロデュースを始められる
- **認証**: MVP は VolTech-MagicOnion の匿名デバイス認証だけで、引き継ぎは作らない（決定。2026-10-28）。アンインストールや機種変更でデータは失われる。いまは 1 つの端末が 1 人のプレイヤーになり、引き継ぎを作るときに 1 人のプレイヤーに複数の端末を結べるようにする（2026-10-31 に「今のうちに 1 対多にしておく」から直した。実装は [api-design.md](../architecture/api-design.md) 3章）

## 最小実装範囲（MVP）[決定。2026-10-31]

**MVP の範囲の正は、下の「MVP に入れるもの・入れないもの」の表**。各システムのページの実行要件の「必須」は、この表と同じ範囲にそろえる（食い違ったら表が正）。表にないもの（「次点」「後回し」）は MVP では作らない。仕様はページに残し、MVP の後に作る。

### 完了条件

0〜5 は上から順に確かめる。6・7 は client/ の最初の実装計画から並行して進める。技術検証で一番不確かなもの（Unity 7 の上での MagicOnion・IL2CPP）を先に置くため、条件 0 は Uta のサーバーの実装を待たずに確かめられる形にしている。

- **Unity 7（Alpha）の不具合で詰まったとき** [決定。2026-10-31]: 条件 0 が通らなければ [ADR 0006](../decisions/0006-adopt-unity-7-early.md) の見直しを起こし、Unity に依存しない条件 1〜5 はサーバーだけで先に進める。条件 7 が Android で通らなければ、Windows の IL2CPP スタンドアロンで代え、Android は iOS と同じ別の項目に回す

| # | 条件 | 確かめ方 |
|---|---|---|
| 0 | Unity 7 のクライアントから、MagicOnion の Unary で、ローカルのサーバーの VolTech-MagicOnion の RPC（端末の登録 `RegisterAsync` → トークンの発行）が通る。Uta の RPC は使わない | Unity Editor（Mono）と Windows の IL2CPP スタンドアロンで手で確かめる。結果は `architecture/unity7-evaluation.md`（未作成。ここで作る）に残す。Uta の RPC（`SignUpAsync` → `SignInAsync`）が通ることは条件 6 で確かめる |
| 1 | 匿名デバイス認証でプレイヤーを作り、初期の所持品を付与して保存できる | サーバーの結合テスト（Testcontainers の PostgreSQL。[repository-layout.md](../architecture/repository-layout.md) 10.4） |
| 2 | ガチャを引いて所持品が更新される（単発・10 連・確定枠・天井の交換・重複のかけらとサポート強化pt への変換） | サーバーの結合テスト。天井は、テストが `gacha_exchange_points` の行を直接入れて前提を作る（マスタは変えない） |
| 3 | サポートカードのレベル上げ・上限解放ができ、次のプロデュースの開始時の性能に反映される | サーバーの結合テスト。残高は初期の所持品で足りる。上限解放の素材は、テストが `player_support_cards` の行を直接入れて用意する |
| 4 | 初期の所持品から編成し、プロデュースを最後まで進めて、評価・報酬・プレイヤー経験値を受け取れる | サーバーの結合テストで、最短シナリオ（下記）を RPC だけで最後まで進める（[produce.md](../systems/produce.md) 完了条件）。最短シナリオで通らない局面（授業・支給・休む・スキルカードの選択肢・中間試験・再挑戦・不合格での終了）は、`Uta.Shared.Domain` の単体テストで、初級のシナリオを操作列から回して確かめる（[produce-domain.md](../architecture/produce-domain.md) 7章） |
| 5 | 途中で中断しても再開できる | 結合テストで途中に `GetStateAsync` を呼んで続ける。状態の再生は `Uta.Shared.Domain` の単体テスト（[produce-domain.md](../architecture/produce-domain.md) 7章） |
| 6 | Windows のクライアントで、起動（`SignUpAsync` → `SignInAsync`）→ ガチャ → 強化 → 編成 → 最短シナリオを最後まで → 結果、を通す。初級の通しも 1 回 | 手動 |
| 7 | Android の実機（IL2CPP）で、起動 → ログイン → ガチャ 1 回のスモークが通る。あわせて IL2CPP との計算の一致を確かめる | 手動。一致の確かめ方は [lesson-phases.md](../architecture/lesson-phases.md) 8章: 固定のシードと操作列で `Uta.Shared.Domain` を再生し、状態を .NET の結果と項目ごとに比べる。入口は Unity Test Runner の実機実行で、プレイヤー向けの画面は作らない |

- iOS の実機ビルドは、Unity 7 の対応状況（[ADR 0006](../decisions/0006-adopt-unity-7-early.md)）次第の別の項目（`architecture/unity7-evaluation.md` に書く）
- dev 環境（Cloud Run + Cloud SQL）へのデプロイは MVP の条件に含めない別の項目（[repository-layout.md](../architecture/repository-layout.md) 11章）。ローカルの `server/compose.yaml` の PostgreSQL は、条件 6・7 で手で動かすときに使う
- **最短シナリオ**: 確認用に、3 週（週 1 レッスン → 週 2 仕上げレッスン → 週 3 最終試験）だけの週構成をマスタに置く。週構成はマスタで定義する（[produce.md](../systems/produce.md) 3.1）ので、コードは変わらない。編成の画面でシナリオを選べる（下の「画面」）ので、クライアントからも選べる

### MVP に入れるもの・入れないもの

| システム | MVP に入れる | MVP に入れない（次点・後回し） |
|---|---|---|
| プロデュース | 初級と最短シナリオ、行動はレッスン・休む・授業・支給、仕上げ、中間・最終試験と再挑戦、最終評価と報酬、プレイヤー経験値と Lv（カードの `unlockLv`） | 中級・上級、相談、おでかけ、ドリンク、育成アイテム、特別レッスン、信頼度（0 固定）、育成pt（使い道の相談・おでかけがないため扱わない）、Lv による機能の解放（PRD-14）、メモリー（PRD-15） |
| レッスン | [lesson-phases.md](../architecture/lesson-phases.md)のフェーズ 1〜4 と試験（フェーズ 6）。感性・理論の両方 | フェーズ 5（ドリンク・育成アイテム）、不調カード（F08）と `CreateCard`、7.6 の後回しのプリミティブ |
| 育成アイドル | R の初期付与、初期デッキ、パラメータボーナス | トレーニング、覚醒、アビリティ（どれもトレーニング・覚醒で解放）、かけらでの解放、最初の SSR の選択 |
| サポートカード | レベル上げ・上限解放、アビリティの枠 1（開始時の効果） | アビリティの枠 2〜6、サポートイベント、スキルカードサポート、レンタル枠 |
| ガチャ | ガチャ 1 本（期間なし）、単発・10 連、確定枠、天井、確率の表示、重複のかけら・サポート強化pt への変換 | 期間・ピックアップ、限定ガチャ、交換メダル、ショップ、1 日 1 回割引、抽選履歴の画面（保存はする） |

- 感性プランを残すのは、9.2 の式（好調・集中）全体をカードで確かめるため。フェーズ 3・4 はカード定義とテストを足すだけで済む（[lesson-phases.md](../architecture/lesson-phases.md) 1章）
- 試験の再挑戦を残すのは、局面の遷移と状態の巻き戻しの技術検証の題材にするため。マスタの `RetryCount` を 0 にすれば、コードを変えずに無効にできる

### 実装計画の単位と順 [決定。2026-10-31]

| 順 | 実装計画 | 受け持つ完了条件 | 待つもの |
|---|---|---|---|
| 1a | 通信の疎通（サーバー）（ソリューションと版の管理、`Uta.Shared.Contracts`・`Uta.Server` の骨組み。中身は [repository-layout.md](../architecture/repository-layout.md) 12章の手順 1・2・4） | — | なし（VolTech-MagicOnion はコミットで固定する。repository-layout.md 3.1） |
| 1b | 通信の疎通（クライアント）（Unity 7 に MagicOnion・MessagePack を入れ、VolTech-MagicOnion の RPC を呼ぶ。12章の手順 5・6） | 0 | 1a（手順 6 は 1a のマージ後） |
| 2a | masterdata の最初の計画 1/2（[計画](../../plans/2026-11-01-masterdata-first.md)。骨組み・レッスンの 5 テーブル・サーバーでの読み込み） | — | なし |
| 2b | **done**（2026-11-01）masterdata の最初の計画 2/2（[計画](../../plans/2026-11-01-masterdata-rest.md)。プロデュース・育成アイドル・サポートカード・ガチャの 26 テーブル） | — | 2a |
| 3 | `Uta.Shared.Domain` のレッスン（[lesson-phases.md](../architecture/lesson-phases.md)のフェーズ 2〜4・6。2026-11-01 に実装済み: [計画](../../plans/2026-11-01-shared-lesson-phases.md)） | — | なし（1・2 と並行できる） |
| 2c | **done**（2026-11-01）マスタを Unity に取り込む（[計画](../../plans/2026-11-01-masterdata-unity.md)。クライアント用の出力・MasterMemory の導入・StreamingAssets からの読み込み） | — | 2b |
| 2d | クライアントでマスタを使う（[計画](../../plans/2026-11-01-masterdata-client-use.md)。マスタ → `CardCatalog` の詰め替えと、起動時の読み込み・DI 登録。**done**） | — | 2c |
| 3 | `Uta.Shared.Domain` のレッスン（[lesson-phases.md](../architecture/lesson-phases.md)のフェーズ 2〜4・6） | — | なし（1・2 と並行できる） |
| 4 | `Uta.Shared.Domain` の週の進行（[produce.md](../systems/produce.md) 10章） | 5 の再生の部分 | 2・3 |
| 5a | server/ のプレイヤー（サインアップ・サインイン。`players` の行の作成と全件取得。初期付与は 5 で足す。[計画](../../plans/2026-11-01-player-signup-signin.md)） | — | 1a（2 を待たない） |
| 5 | **done**（2026-11-01）server/ のプレイヤー（初期付与）・ガチャ・強化（[初期付与](../../plans/2026-11-01-server-initial-grant.md)・[ガチャ](../../plans/2026-11-01-server-gacha.md)・[強化](../../plans/2026-11-01-server-support-upgrade.md)） | 1・2・3（レベル上げ・上限解放まで。性能への反映は 6） | 1、2、5a |
| 6 | server/ のプロデュース | 3 の「開始時の性能への反映」・4・5 | 4・5 |
| 7 | client/ の画面 | 6・7 | 1・6 |

### MVP の最小のコンテンツ

マスタに入れる最小の中身。値は参考作品の値を使い（[ADR 0016](../decisions/0016-reference-game-values.md)）、無いもの（Uta のキャラクターのステータスなど）は masterdata の最初の実装計画で仮の値を置く。

| 種類 | 中身 |
|---|---|
| 育成アイドル | 4 人: R 2 人（感性 1・理論 1）、SR 1 人、SSR 1 人。名前は仮称。固有スキルカードは 14章のカードを流用する（感性は S07、理論は L07） |
| サポートカード | 10 種: 初期付与の R 5 種（プラン区分は共通。どの育成アイドルでも編成できる）と、ガチャ用の SR 3 種・SSR 2 種 |
| スキルカード | [lesson-card.md](../systems/lesson-card.md) 14章の 36 枚（F08 を除く） |
| ガチャ | 1 本。排出テーブルは上の育成アイドル 4 人とサポートカード 10 種 |
| シナリオ | 最短シナリオと初級の 2 つ |

### 画面

MVP で作る画面（[screen-design.md](../architecture/screen-design.md) 2章の一覧のうち、この表にあるものだけ）。見た目と演出は仮のもの（プレースホルダ）でよい。

| 画面 | 完了条件 |
|---|---|
| 起動、ホーム（再開を含む） | 0・6 |
| ガチャ（確率の表示と結果を含む） | 6・7 |
| 所持一覧・サポートカードの詳細（レベル上げ・上限解放） | 6 |
| 編成（シナリオの選択を含む） | 6 |
| 週の行動選択、授業・支給、レッスン・試験、スキルカードの選択肢、試験結果、プロデュース結果 | 6 |

作らない画面: 天井の交換（条件 2 は結合テストで確かめる）、育成アイドルの強化、抽選履歴、設定・お知らせ、相談・おでかけ。

## 未決事項

- （決定。2026-11-01）Uta のキャラクターのステータス・サポートカードの値は、仮の値を置いた（ユーザーの許可。育成アイドル 4 人・サポートカード 10 種。名前も仮称。masterdata の計画 2/2）。オリジナルの名称・値の調整は後
- （決定。2026-11-01）ガチャ 1 本の中身は masterdata の計画 2/2 で決めた: 通常枠は [gacha.md](../systems/gacha.md) 3.1 の率をレア内で等分（重みの合計 10000）、確定枠は SR 以上の 7 行、天井の交換対象は育成アイドル SSR とサポートカード SSR の 1 つずつ。2 種ともサポートカードにする読みもある（`gachaCeilingItem.csv` を直す）
- スキルカードの `unlockLv`（仮の値 N・R = 1、SR = 5、SSR = 10 を入れた。参考作品の値が分かったら差し替える。[lesson-card.md](../systems/lesson-card.md) 14章）
- オリジナル名称（プロデュース等の仮称、キャラ、世界観の最小設定）。MVP は仮称のままで進められる
- マスタを Addressables によるダウンロードに移す時期（いまは同梱。[masterdata-pipeline.md](../architecture/master/masterdata-pipeline.md) 6章）。MVP は同梱のままで進められる

## 更新が必要になるページ

この仕様が変わったら: `systems/` 各ページ、`architecture/` の各ページ（特に api-design.md・screen-design.md）、[screen-wireframes.md](screen-wireframes.md)、関係する `uta_docs/plans/` の実装計画、`glossary.md`、`index.md`。
