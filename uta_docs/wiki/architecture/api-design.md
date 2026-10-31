---
title: サービスIF・プレイヤーデータ・ゲームルール（実装設計書 API設計編）
status: draft
updated: 2026-11-01
---

# サービスIF・プレイヤーデータ・ゲームルール（実装設計書 API設計編）

実装設計書（4編）の3編目。クライアントとサーバーの間の契約（サービスIF・DTO）と、サーバーが持つプレイヤーデータ、サーバー側のゲームルールの置き場を定める。前提は [repository-layout.md](repository-layout.md)。

- 認証・冪等性・エラー・ヘッダー・フィルター順序は、VolTech-MagicOnion の仕様 [server-foundation.md](server/server-foundation.md) が正。ここでは繰り返さず、Uta 固有の部分だけを書く
- ゲームの仕様は [systems/](../systems/produce.md) が正

## 凡例

- **[決定]**: CLAUDE.md・accepted の ADR、またはユーザーが確認した方針（日付を添える）
- **[Uta案]**: この叩き台の提案
- 推測は「推測」と明記する

## 決まっていること

- 通信は MagicOnion の Unary のみ（StreamingHub は対象外）[決定。[ADR 0002](../decisions/0002-magiconion.md)]
- サービスIF・DTO は `shared/Uta.Shared.Contracts` に置く（C# 9、Unity と共有）。`Uta.Shared.Contracts` には Uta 固有のものだけを置き、VolTech-MagicOnion の `Abstractions` を参照する [決定。2026-10-28]
- サーバー権威: 育成の計算、ガチャ抽選、所持品の更新はサーバー [決定。[CLAUDE.md](../../../CLAUDE.md)]
- RPC ごとの設定（認証不要、副作用なし）は、サービスIFの属性（`[SkipAuth]` / `[NoSideEffects]`）で持つ [決定。VolTech-MagicOnion の仕様]
- 再送は `x-request-id` と `last_responses` による冪等性に任せ、仕様側で独自の重複防止を作らない [決定。[server-foundation.md](server/server-foundation.md)]
- プロデュースの操作の RPC は、局面ごとに分ける [決定。1章、2026-10-31]
- RPC の名前は1章の表で確定。メソッドは `Async` を付け、DTO は RPC ごとに `XxxRequest` / `XxxResponse` を1組作る [決定。2026-10-28]
- Uta 固有のエラーは、domain `uta`、reason は機能の接頭辞付きの大文字スネークケース [決定。2026-10-28。2章]
- DB は PostgreSQL + Npgsql + Dapper。セッションは検索用の列と状態の1列に分けて保存する [決定。[ADR 0010](../decisions/0010-database.md)、3章]
- マイグレーションは DbUp。API とは別の `migrate` コマンドで流す [決定。[ADR 0012](../decisions/0012-migration-dbup.md)]
- DTO ↔ Domain のマッピングはクライアントの Infrastructure に閉じる。DTO は enum とプリミティブ型で組む [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]
- 進行中のプロデュースは、開始時とアプリのバージョンが違えば破棄する [決定。2026-10-28。5章]

## 1. サービスの一覧 [決定。2026-10-28]

| サービス | RPC | 機能ID | 副作用 | 冪等性の対象 |
|---|---|---|---|---|
| `IDeviceService`（VolTech-MagicOnion） | `RegisterAsync` / `IssueTokenAsync` | — | あり | VolTech-MagicOnion の仕様に従う |
| `IPlayerService` | `SyncAsync`（起動時の同期。下記）、`GetPlayerAsync`（所持品・通貨・Lv） | — | `SyncAsync` はあり | `SyncAsync` |
| `IGachaService` | `GetGachasAsync`（ガチャごとの交換pt）、`DrawAsync`（単発・10連）、`GetHistoryAsync`、`ExchangeAsync`（交換pt で天井の交換） | GAC-01〜04 | `DrawAsync`・`ExchangeAsync` はあり | `DrawAsync`・`ExchangeAsync` |
| `IIdolService` | `TrainAsync`（トレーニング）、`AwakenAsync`（覚醒）。MVP では作らない | IDL-02 | あり | 両方 |
| `ISupportService` | `LevelUpAsync`（所持IDと目標のレベル。強化pt とゴールドを使う）、`LimitBreakAsync`（解放する所持IDと素材の所持ID。[idol-support.md](../systems/idol-support.md) 2.2.1） | SUP-02 | あり | 両方 |
| `IProduceService` | `GetRentalCandidatesAsync`、`StartAsync`、`ChooseWeekActionAsync`、`ExecuteConsultActionAsync`、`ResolveCardOfferAsync`、`DiscardDrinkAsync`、`DecideExamRetryAsync`、`ExecuteLessonActionAsync`、`GetStateAsync`、`AbandonAsync` | PRD-01〜14、IDL-03・04、SUP-03〜06 | `GetRentalCandidatesAsync`・`GetStateAsync` 以外あり | 副作用ありのすべて |

- MVP で作らない RPC(インターフェースにも宣言しない。作るときに足す): `IIdolService` の 2 つ、`GetRentalCandidatesAsync`、`ExecuteConsultActionAsync`、`DiscardDrinkAsync`、`GetHistoryAsync`（履歴を見る画面を MVP に置かないため。履歴の保存はする）（[game-spec.md](../overview/game-spec.md) 最小実装範囲）

- 名前の付け方: 動詞から始める PascalCase に `Async`。DTO は RPC ごとに1組作り、名前は RPC 名から `Async` を除いて `Request` / `Response` を付ける（`DrawAsync` → `DrawRequest` / `DrawResponse`）。引数を後から足しても、ほかの RPC に影響しない。DTO の型が増えるのは受け入れる。例外は 2 つ [決定。2026-10-31]: 引数の無い RPC（`GetStateAsync`・`AbandonAsync`・`GetGachasAsync` など）は Request を作らない。プロデュースの操作の RPC は、共通の `ProduceStateResponse` を返す（[produce-domain.md](produce-domain.md) 8章）
- **`SyncAsync`**: 起動フローでホームに入る前に1回呼ぶ（[screen-design.md](screen-design.md) 4章）。副作用のある「起動時の処理」をここにまとめる
  - プレイヤーがまだいなければ作り、初期の所持品（中身と量の正は [game-spec.md](../overview/game-spec.md) の「初期の所持品」。育成アイドル・サポートカード・ガチャ通貨・ゴールド・サポート強化pt）を付与する。端末の登録（VolTech-MagicOnion の `RegisterAsync`）の後の最初の `SyncAsync` が作る。2 回目からは何もしない [決定。2026-10-31]
  - アプリのバージョンが違う進行中のプロデュースを破棄する（5章）
  - 日付の更新（12:00）をまたいだときの処理（いまは無し。ログインボーナスなどを足すときの置き場）
  - Response（`SyncResponse`）[決定。2026-10-31]: 進行中のプロデュースがあるか（`HasActiveProduce`。破棄した後の値）と、この同期でプロデュースを破棄したか（`DiscardedProduce`）を返す。ホームは、起動時（サインインの流れ。[screen-design.md](screen-design.md) 4章）に受け取ったこの値で「再開」とダイアログを出し分ける。再開を押したら `GetStateAsync` で状態を取る。起動した後は、クライアントが自分の操作で持ち替える（`StartAsync` が通れば有り、終了・放棄で無し）。ホームに戻るたびにサーバーへ聞き直さない
  - `GetPlayerAsync` は `[NoSideEffects]`（読み取り専用のトランザクション）なので、これらを入れられない。そのため RPC を分ける
  - ガチャの交換pt の変換は持たない（MVP のガチャは期間を持たない。[gacha.md](../systems/gacha.md) 4章）
- **操作の RPC は局面ごとに分ける** [決定]
  - `ChooseWeekActionAsync`: 週の行動を選ぶ（レッスン、授業、支給、休む、相談に入る、など。PRD-02）
  - `ExecuteConsultActionAsync`: 相談中の操作（交換・強化・削除・終える）
  - `ResolveCardOfferAsync`: スキルカードの選択肢への操作（選ぶ・再抽選・除去・受け取らない）
  - `DiscardDrinkAsync`: ドリンクを捨てる（ドリンクの整理の局面と、終了以外の入力待ち）
  - `DecideExamRetryAsync`: 試験の後、再挑戦が残るときに（合否にかかわらず）、再挑戦するかを選ぶ
  - `ExecuteLessonActionAsync`: レッスン・試験の中の操作（カードを使う、スキップ、ドリンクを使う。PRD-03〜08）。試験もレッスンと同じ RPC で扱う
  - 局面と操作・RPC の対応は [produce-domain.md](produce-domain.md) 5章、Request のフィールドは同じページの 8章が正（2026-10-31 決定）。要求の局面が今の局面と違えば `PRODUCE_UNEXPECTED_PHASE`
  - DTO が型で区別でき、引数の意味が明確になる。RPC の数が増えるのは受け入れる
  - どの操作の要求も、**次の操作番号**を持つ。サーバーの操作番号と合わない要求は `PRODUCE_STALE_OPERATION` で拒否する(クライアントは `GetState` で取り直す)。週・ターンは要求に持たない(サーバーの状態から分かる)。再送は `x-request-id` で冪等に扱う([produce-domain.md](produce-domain.md) 5章。2026-10-31 決定)
- 取得系（`[NoSideEffects]`）は読み取り専用のトランザクションで動く（VolTech-MagicOnion の仕様）
- **マスタ配信は RPC にしない**。クライアントのマスタはバイナリをアプリに同梱し、あとで Addressables のダウンロードに移す（[masterdata-pipeline.md](master/masterdata-pipeline.md) 6章）。強制アップデートは実装しないので、マスタの版の不一致はサーバーでは扱わない（手元のアプリを入れ直す。[repository-layout.md](repository-layout.md) 10.5）
- DTO は、サーバー専用の値（`[ServerOnly]` のマスタの値など）を含めない。レンタル候補のように、サーバーが決めた結果だけを返す
- DTO の ID: サーバーが発行する ID（ULID）は26文字の `string`、マスタの ID は `int`（[repository-layout.md](repository-layout.md) 10.1）

### 1.1 ガチャの DTO [決定。2026-10-31]

| RPC | Request | Response |
|---|---|---|
| `GetGachasAsync` | なし | `Gachas`: ガチャごとに `GachaId`（`int`）・`ExchangePoints`（`int`）。ガチャの中身と排出率はクライアントのマスタから表示する |
| `DrawAsync` | `GachaId`（`int`）、`Count`（`int`。1 か 10）、`ExpectedCost`（`int`。画面に出した価格。マスタと違えば拒否） | `Results`: 枠ごとに `Slot`（`int`）・`ItemKind`（enum）・`ItemId`（`int`）・`Outcome`（enum。`Nothing` = 0 / `New` = 1 / `Duplicate` = 2。名前と数値の正はここ。2026-11-01 に gacha-tables.md から移した。`ItemKind` の正は [gacha-tables.md](master/gacha-tables.md)）・`Shards`（`int`。重複の育成アイドル）・`SupportPoints`（`int`。完凸後の重複）・`SupportCardUid`（`string`。増えたサポートカード。無ければ空）。ほかに `GachaCurrency`（残り）・`ExchangePoints`（このガチャの交換pt） |
| `GetHistoryAsync` | `Before`（`string`。この履歴 ID より前。空なら最新から）、`Limit`（`int`。1〜50） | `Items`: 履歴の行（3章）。新しい順 |
| `ExchangeAsync` | `GachaId`、`CeilingItemOrder`（`int`。`GachaCeilingItem` の `Order`） | `DrawAsync` の `Results` と同じ形の 1 件（`Slot` は 0）、`ExchangePoints`。`GachaCurrency` は含めない（変わらないため） |

### 1.2 サポートカードの DTO [決定。2026-10-31]

| RPC | Request | Response |
|---|---|---|
| `LevelUpAsync` | `SupportCardUid`（`string`）、`TargetLevel`（`int`） | `Level`、`SupportPoints`（残り）、`Gold`（残り） |
| `LimitBreakAsync` | `SupportCardUid`（解放する）、`MaterialUid`（素材） | `LimitBreak`（回数）、`MaxLevel` |

## 2. エラー [決定。2026-10-28]

- 業務エラーは `BusinessException(ErrorCode, ...)` を投げる（VolTech-MagicOnion の仕様）。クライアントは VolTech-MagicOnion の `ErrorPolicy` に従う
- VolTech-MagicOnion の `ErrorCode` は数値ではなく、`domain` と `reason` の2つの文字列で表す（`external/VolTech-MagicOnion` の `ErrorCode`）。そのため、以前の案の「機能ごとの番号帯」はやめた
- **domain は `uta` の1つ**。VolTech-MagicOnion の予約エラーは domain `voltech` なので、domain は「アプリか VolTech-MagicOnion か」の区別に使い、機能では分けない
- **reason は、機能の接頭辞を付けた大文字スネークケース**:

| 接頭辞 | 機能 | 例 |
|---|---|---|
| `PLAYER_` | プレイヤー・共通 | `PLAYER_NOT_ENOUGH_GOLD`、`PLAYER_NOT_CREATED`（`SyncAsync` の前に Uta の RPC が来た。[server-foundation.md](server/server-foundation.md)。2026-11-01） |
| `GACHA_` | ガチャ（GAC） | `GACHA_NOT_ENOUGH_CURRENCY`、`GACHA_NOT_ENOUGH_POINTS`（交換pt 不足）、`GACHA_NOT_FOUND`（無いガチャ ID・交換対象）、`GACHA_INVALID_COUNT`（1・10 以外）、`GACHA_COST_MISMATCH`（`ExpectedCost` が違う） |
| `IDOL_` | 育成アイドル（IDL） | MVP では使わない（トレーニング・覚醒を作るときに足す） |
| `SUPPORT_` | サポートカード（SUP） | `SUPPORT_INVALID_TARGET_LEVEL`（目標が今のレベル以下・上限を超える）、`SUPPORT_NOT_ENOUGH_POINTS`、`SUPPORT_MAX_LIMIT_BREAK`、`SUPPORT_NOT_OWNED`（自分の所持IDでない）、`SUPPORT_INVALID_MATERIAL`（素材が同じ所持ID・別のカード・無い） |
| `PRODUCE_` | プロデュース（PRD） | `PRODUCE_STALE_OPERATION`、`PRODUCE_UNEXPECTED_PHASE`、`PRODUCE_INVALID_ARGUMENT`、`PRODUCE_INVALID_FORMATION`、`PRODUCE_NOT_IN_PROGRESS`、`PRODUCE_SESSION_DISCARDED`、`PRODUCE_ALREADY_IN_PROGRESS` |

- エラーコードは `shared/Uta.Shared.Contracts` に `static readonly ErrorCode` の定数として置き、消した reason は再利用しない（VolTech-MagicOnion の仕様）
- 上の表が MVP の reason の一覧。ゴールド不足は共通の `PLAYER_NOT_ENOUGH_GOLD`。MVP の後の機能の reason は、その機能を実装するときに足す

## 3. プレイヤーデータ（データモデル）[決定。2026-10-28／列の細部は実装時]

サーバーが発行する ID は ULID（DB でも 26 文字の `char(26)`。[repository-layout.md](repository-layout.md) 10.1）、マスタの ID は `int`（[repository-layout.md](repository-layout.md) 10.1）。DB は PostgreSQL、アクセスは Npgsql + Dapper（[ADR 0010](../decisions/0010-database.md)）、マイグレーションは DbUp（[ADR 0012](../decisions/0012-migration-dbup.md)）。

| テーブル | 主キー | 主な列 | 元の仕様 |
|---|---|---|---|
| `players` | `player_id` | プレイヤーLv、経験値、ゴールド、サポート強化pt、ガチャ通貨、作成日時 | [produce.md](../systems/produce.md) 7、[gacha.md](../systems/gacha.md) |
| `player_idols` | `player_id` + `idol_id` | トレーニング段階、覚醒段階、かけら（所持している育成アイドルだけ行がある。信頼度は PRD-11 を作るときに足す） | [idol-support.md](../systems/idol-support.md) 1、[gacha.md](../systems/gacha.md) 5 |
| `player_support_cards` | `support_card_uid`（所持ID。ULID） | `player_id`、`card_id`、レベル、上限解放回数、入手日時。同じカードを引くたびに1行増える（2026-10-31 決定） | [idol-support.md](../systems/idol-support.md) 2.2.1 |
| `gacha_exchange_points` | `player_id` + `gacha_id` | 交換pt | [gacha.md](../systems/gacha.md) 4 |
| `gacha_histories` | `history_id`（ULID） | カード 1 枚 = 1 行。`request_id`（`DrawAsync` 1 回の束）、`player_id`、`gacha_id`、`slot`（1〜10。交換は 0）、`gacha_table_id`、`item_kind`、`item_id`、`outcome`（新規 / 重複）、`shards`、`support_points`、日時 | [gacha.md](../systems/gacha.md) 6 |
| `produce_sessions` | `session_id` | 検索用の列と状態の1列（下記） | [produce.md](../systems/produce.md) 10 |
| `produce_results` | `session_id` | `player_id`、終わり方（完了 / 放棄 / 破棄）、試験の結果（中間不合格 / 最終不合格 / 最終合格。完了のときだけ）、評価値、ランク、付与した報酬 | [produce.md](../systems/produce.md) 6 |
| VolTech-MagicOnion のテーブル | — | `last_responses`、`game_logs`、端末・トークン | [server-foundation.md](server/server-foundation.md) |

- **通貨は `players` の列で持つ**。汎用の所持テーブル（`種別・ID・数`）にはしない。SQL と Dapper が素直に書ける。通貨の種類を増やすときはマイグレーションが要るが、MVP では種類が少ないので受け入れる
- かけらは育成アイドル（`idol_id` = 1 キャラ）ごとに持つ [決定。2026-10-31。[idol-support.md](../systems/idol-support.md) 1.1]
- 端末とプレイヤーは、いまは 1 対 1（VolTech-MagicOnion の `devices` の `player_id` が `UNIQUE`。端末の登録のたびに新しい `player_id` が発行される）。MVP は引き継ぎを作らない。引き継ぎを足すときは、1 人のプレイヤーに複数の端末を結べるよう VolTech-MagicOnion の `devices` を変える（[repository-layout.md](repository-layout.md) 3.1。[game-spec.md](../overview/game-spec.md)）
- `players.player_id` は `char(26)`（ULID）。VolTech-MagicOnion のテーブルの `player_id` も `char(26)` にそろえる（[repository-layout.md](repository-layout.md) 10.1）
- 1ユーザーにつき、進行中のプロデュースのセッションは1つ。進行中があるときの `StartAsync` は `PRODUCE_ALREADY_IN_PROGRESS`(バージョンが違うセッションは破棄してから始める。5章)
- 「抽選結果と所持品の更新を1トランザクションで整合させる」（[gacha.md](../systems/gacha.md) 完了条件）。冪等性の保存済みレスポンスも同じトランザクションで書く（VolTech-MagicOnion の仕様）
- **同じプレイヤーの副作用のある RPC は、VolTech-MagicOnion が直列にする**: 冪等性の対象の RPC は、本体の前に `last_responses` のそのプレイヤーの行を `FOR UPDATE` でロックする（`Transactions.cs`。[server-foundation.md](server/server-foundation.md)）。そのため Uta の手順で `players` の行を明示的にロックしない。通貨・pt を払うところは、条件付きの `UPDATE ... WHERE 残高 >= @cost` の 1 文で行う（0 行なら不足のエラー）[決定。2026-10-31。ユーザーの判断。それまでは「最初に `players` をロックする」としていた]
- 通貨・pt を減らす更新は、`UPDATE ... SET x = x - @cost WHERE player_id = @id AND x >= @cost` の 1 文にし、更新が 0 行なら不足のエラーにする。ガチャの交換pt は `gacha_exchange_points` なので `gacha_id` も条件に入れる（[gacha-support-server.md](server/gacha-support-server.md) 2章）。同じプレイヤーの同時の別要求でも残高がマイナスにならない [決定。2026-10-31]
- インデックスは、実装するときに検索の形に合わせて付ける

### 3.1 プロデュースのセッション [決定。2026-10-28]

保存形式は [ADR 0010](../decisions/0010-database.md) のとおり、検索用の列と、状態を丸ごと入れた1列に分ける。

検索用の列:

| 列 | 内容 |
|---|---|
| `session_id` | ULID |
| `player_id` | ULID |
| `status` | 進行中 / 完了 / 放棄 / 破棄（放棄は `AbandonAsync` で、`produce_results` の終わり方と同じ値にする） |
| `week` / `turn` | 一覧と調査のための値（古い要求の拒否には使わない。拒否は状態の中の操作番号で行う。2026-10-31） |
| `app_version` | 開始したときの `x-app-version`（5章） |
| `state_version` | 状態の形式のバージョン（下記） |
| `rule_version` | 開始したときのサーバーのルールの版（`RuleVersion`。[repository-layout.md](repository-layout.md) 10.5。2026-11-01 に追加） |
| `version` | 楽観ロック |
| `state` | 状態（MessagePack の `bytea`） |

状態（`state`）の中身は [produce-domain.md](produce-domain.md) 6章 が正。概要:

- 週、体力、パラメータ、デッキ、育成アイテム、ドリンク、育成pt、編成（開始時の性能を写したもの）
- レッスン・試験の中なら:
  - カードの個体の一覧（`instanceId`、`cardId`、強化、レッスン中強化）と、それぞれのゾーン（山札の順番を含む）
  - ステータス（[lesson-card.md](../systems/lesson-card.md) 8章）と、ターン制ステータスの「プレイヤー操作中に初めて付与したか」の印（11章）
  - 持続効果・発動予約の一覧（発動する効果の中身を丸ごと、残りターン・回数、付与の順番）。効果グループ ID では持たない。マスタの版はアプリのバージョンと同じで、版が違うセッションは破棄される（5章）ので、中身を持っても食い違わない [決定。2026-10-31]
  - 残りの使用数、ターン番号、スコア・パラメータの途中経過
- 乱数: **マスターシードと、使用中のストリームごとの `xoshiro256**` の現在の状態の両方を持つ**（[produce-domain.md](produce-domain.md) 3章）。現在の状態があれば再開がすぐにでき、マスターシードと操作の記録があれば最初から再現できる
- 操作の記録（[produce-domain.md](produce-domain.md) 5章）
- MessagePack は `[MessagePackObject]` と整数のキー（`[Key(n)]`）で書く。文字列のキーより小さく、項目の名前を変えても読める

**状態の形式のバージョン（`state_version`）**: サーバーで状態の形式を変えたら上げる。MVP の間は、古い形式を新しい形式に変換するコードは書かず、**`state_version` が今のサーバーと違うセッションは、アプリのバージョン違いと同じく破棄する**（5章）。進行中のプロデュースが消えるのは、個人開発の確認用なので受け入れる。

**操作の記録**: 受け付けた操作を順番に、状態の1列の中に記録する（別テーブルにはしない。[produce-domain.md](produce-domain.md) 5章）。マスターシードと合わせて、最初から同じ結果を再現できる（不具合の調査、[lesson-card.md](../systems/lesson-card.md) 13章）。調査で SQL から見たくなったら、別テーブルに追記する形に移す。

## 4. ゲームルールの置き場 [決定]

ルールを `shared/Uta.Shared.Domain`（両側で実行できる）と、サーバー専用（`server/Uta.Server/Domain/`）のどちらに置くか（[repository-layout.md](repository-layout.md) 6.1）。

| ルール | 置き場 | 理由 |
|---|---|---|
| カード効果の処理、ゾーンの移動、ターン処理順、計算式（[lesson-card.md](../systems/lesson-card.md) 4〜12章） | `Uta.Shared.Domain` | クライアントとサーバーで結果を一致させる（[lesson-phases.md](lesson-phases.md) 8章）。確定はサーバー |
| 使用可否の判定（コスト・使用条件。PRD-04） | `Uta.Shared.Domain` | クライアントでも判定と予測の表示に使う（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)） |
| 最終評価の計算（[produce.md](../systems/produce.md) 6章） | `Uta.Shared.Domain` | 最終試験の前後で、評価の見込みを表示できる。確定はサーバー（2026-10-28 決定） |
| 週の進行（行動の処理、選択肢の抽選、レッスン後の処理、試験の結果。[produce.md](../systems/produce.md) 3〜5章・10章） | `Uta.Shared.Domain` | 再生の検証を `Uta.Shared.Domain` の単体テストで書ける（[produce-domain.md](produce-domain.md) 7章）。乱数はサーバーが渡す（2026-10-31 決定） |
| サポートカードの性能の計算、編成の検証、初期デッキ（[idol-support.md](../systems/idol-support.md) 5章） | `Uta.Shared.Domain` | 強化後の予測と編成の可否をクライアントでも表示する（2026-10-31 決定） |
| 報酬の決定と付与 | サーバー専用 | 所持品・通貨の更新はサーバーのみ |
| ガチャの抽選、交換pt の加算・天井の交換の確定 | サーバー専用 | 抽選はサーバー権威。表示用の排出率はマスタで公開する（GAC-02）。交換できるかの表示用の判定はクライアントもする |
| プロデュース開始時のシード生成、レンタル候補の決定 | サーバー専用 | 乱数・候補はクライアントを信用しない |
| 所持品・通貨の更新 | サーバー専用 | 更新はサーバーのみ |

- 最終評価の式（[produce.md](../systems/produce.md) 6章）は `2.3` や `0.15` などの小数を使う。`Uta.Shared.Domain` では `float` / `double` を使わないので、千分率の整数にする（例: `× 2.3` → `× 2300 / 1000`）。丸めの位置は式のとおり `floor` で、元の式と結果が一致することを単体テストで確かめる
- 乱数は決定的な疑似乱数 `xoshiro256**` を `IRandom` 越しに使い、`System.Random` は使わない。シードはセッションに1つで、用途・場面ごとのストリームに分ける（[produce-domain.md](produce-domain.md) 3章）
- `Uta.Shared.Domain` は、`[ServerOnly]` のマスタの値に依存しない。必要な値は引数で受け取る（[repository-layout.md](repository-layout.md) 5章）

## 5. 中断・再開・破棄 [決定。2026-10-28]

| 場面 | 扱い |
|---|---|
| アプリの終了・通信の切断（中断） | 状態はすべてサーバーにある。期限なしで、`GetStateAsync` で再開できる |
| 放棄 | 明示の操作（`AbandonAsync`。画面で確認のダイアログを出す）。報酬はなし。`produce_results` に「放棄」で残す |
| **アプリのバージョンが違う** | 開始したときの `x-app-version` と、今の要求の `x-app-version` が違えば、そのセッションを**破棄**する。報酬はなし。`produce_results` に「破棄」で残す |
| 状態の形式が違う | `state_version` が今のサーバーと違えば、同じく破棄する（3.1） |
| ルールの版が違う | `rule_version` が今のサーバーの `RuleVersion` と違えば、同じく破棄する（2026-11-01 に追加） |

- 状態はすべてサーバーが持ち、クライアントは `GetState` で復元する（[produce.md](../systems/produce.md) 完了条件「中断・再開」）
- 再送は `x-request-id` で冪等に扱う。次の操作番号を要求に含め、合わない操作は拒否する（[produce-domain.md](produce-domain.md) 5章）
- サーバーは入力待ちから次の入力待ちまでを1回で処理して保存するので、再開は最後の入力待ちから続けるだけになる（[produce-domain.md](produce-domain.md) 2章）
- 進行中のセッションがある間は、新しく開始できない（3章）

**アプリのバージョンで破棄する理由**: `Uta.Shared.Domain` のルールやマスタは、アプリのバージョンと一緒に変わる（[repository-layout.md](repository-layout.md) 10.5）。プロデュースの途中でルールが変わると、それまでの状態と新しいルールが食い違い、クライアントの予測とサーバーの確定もずれる。バージョンが変わったら破棄すれば、1つのセッションは常に1つのルールで最後まで進む。

- メリット: 状態の移行やルールの互換を考えなくてよい。新しいアプリで接続すれば、古いルールのセッションは残らない
- デメリット: アプリを更新するたびに、進行中のプロデュースが消える。個人開発の確認用なので受け入れる

破棄の流れ:

1. `SyncAsync`（起動時）で、進行中のセッションの `app_version`・`state_version`・`rule_version`（サーバーの `RuleVersion`。[repository-layout.md](repository-layout.md) 10.5。2026-11-01 に追加）を確かめ、どれかが違えば破棄して、レスポンスで「破棄した」ことを返す。クライアントはダイアログで知らせる
2. `GetStateAsync` は `[NoSideEffects]` なので破棄しない。違うセッションは「進行中のセッション無し」(`Phase` が `Nothing`。エラーにしない。[produce-domain.md](produce-domain.md) 8章)として返す
4. `StartAsync` のとき、バージョンが違うセッションが残っていれば、それを破棄してから始める(`PRODUCE_ALREADY_IN_PROGRESS` にしない) [決定。2026-10-31]
3. 起動後にアプリのバージョンが変わることはないが、`SyncAsync` を経ずに操作の RPC が来た場合に備え、操作の RPC（`ChooseWeekActionAsync` など）でも確かめる。違えば破棄して `PRODUCE_SESSION_DISCARDED` を返す

- 比べるのは「違うかどうか」（新しいか古いかは見ない）。強制アップデートは実装しないので、古いアプリで接続し直すと、新しいアプリで始めたセッションも破棄される（受け入れる。[repository-layout.md](repository-layout.md) 10.5）
- 開発中、Unity エディターから同じ `x-app-version` で接続し続ける間は破棄されない。ルールを変えた確認のときは、バージョンを上げるか、放棄してやり直す
- `x-app-version` は、`Api` で MagicOnion の `Context.CallContext.RequestHeaders` から読む（VolTech-MagicOnion に読み出す関数は無い。2026-10-31 に確認。[repository-layout.md](repository-layout.md) 10.5）。整数として読めない・無いときは `PRODUCE_INVALID_ARGUMENT` にする [決定。2026-10-31]

## 未決事項

- 報酬の具体の値（評価ランクごとの量。[produce.md](../systems/produce.md) 6.1 は仮の値）

## 実装メモ

（実装後に仕様とのズレをここに記録する）
