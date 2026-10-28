---
title: サービスIF・プレイヤーデータ・ゲームルール（実装設計書 API設計編）
status: draft
updated: 2026-10-28
---

# サービスIF・プレイヤーデータ・ゲームルール（実装設計書 API設計編）

実装設計書（4編）の3編目。クライアントとサーバーの間の契約（サービスIF・DTO）と、サーバーが持つプレイヤーデータ、サーバー側のゲームルールの置き場を定める。前提は [repository-layout.md](repository-layout.md)。

- 認証・冪等性・エラー・ヘッダー・フィルター順序は、基盤の仕様 [server-foundation.md](server-foundation.md) が正。ここでは繰り返さず、Uta 固有の部分だけを書く
- ゲームの仕様は [systems/](../systems/produce.md) が正

## 凡例

- **[決定]**: CLAUDE.md・accepted の ADR、またはユーザーが確認した方針（日付を添える）
- **[Uta案]**: この叩き台の提案
- 推測は「推測」と明記する

## 決まっていること

- 通信は MagicOnion の Unary のみ（StreamingHub は対象外）[決定。[ADR 0002](../decisions/0002-magiconion.md)]
- サービスIF・DTO は `shared/Uta.Shared` に置く（C# 9、Unity と共有）。`Uta.Shared` には Uta 固有のものだけを置き、基盤の `Abstractions` を参照する [決定。2026-10-28]
- サーバー権威: 育成の計算、ガチャ抽選、所持品の更新はサーバー [決定。[CLAUDE.md](../../../CLAUDE.md)]
- RPC ごとの設定（認証不要、副作用なし）は、サービスIFの属性（`[SkipAuth]` / `[NoSideEffects]`）で持つ [決定。基盤の仕様]
- 再送は `x-request-id` と `last_responses` による冪等性に任せ、仕様側で独自の重複防止を作らない [決定。[server-foundation.md](server-foundation.md)]
- プロデュースの操作の RPC は、週の行動とレッスン・試験の操作で分ける [決定。1章]
- RPC の名前は1章の表で確定。メソッドは `Async` を付け、DTO は RPC ごとに `XxxRequest` / `XxxResponse` を1組作る [決定。2026-10-28]
- Uta 固有のエラーは、domain `uta`、reason は機能の接頭辞付きの大文字スネークケース [決定。2026-10-28。2章]
- DB は PostgreSQL + Npgsql + Dapper。セッションは検索用の列と状態の1列に分けて保存する [決定。[ADR 0010](../decisions/0010-database.md)、3章]
- マイグレーションは DbUp。API とは別の `migrate` コマンドで流す [決定。[ADR 0012](../decisions/0012-migration-dbup.md)]
- DTO ↔ Domain のマッピングはクライアントの Infrastructure に閉じる。DTO は enum とプリミティブ型で組む [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]
- 進行中のプロデュースは、開始時とアプリのバージョンが違えば破棄する [決定。2026-10-28。5章]

## 1. サービスの一覧 [決定。2026-10-28]

| サービス | RPC | 機能ID | 副作用 | 冪等性の対象 |
|---|---|---|---|---|
| `IDeviceService`（基盤） | `RegisterAsync` / `IssueTokenAsync` | — | あり | 基盤の仕様に従う |
| `IPlayerService` | `SyncAsync`（起動時の同期。下記）、`GetPlayerAsync`（所持品・通貨・Lv） | — | `SyncAsync` はあり | `SyncAsync` |
| `IGachaService` | `GetGachasAsync`、`DrawAsync`（単発・10連）、`GetHistoryAsync`、`ExchangeAsync`（交換pt） | GAC-01〜04 | `DrawAsync`・`ExchangeAsync` はあり | `DrawAsync`・`ExchangeAsync` |
| `IIdolService` | `TrainAsync`（トレーニング）、`AwakenAsync`（覚醒） | IDL-02 | あり | 両方 |
| `ISupportService` | `LevelUpAsync`、`LimitBreakAsync` | SUP-02 | あり | 両方 |
| `IProduceService` | `GetRentalCandidatesAsync`、`StartAsync`、`ChooseWeekActionAsync`、`ExecuteLessonActionAsync`、`GetStateAsync`、`AbandonAsync` | PRD-01〜14、SUP-06 | `GetRentalCandidatesAsync`・`GetStateAsync` 以外あり | 副作用ありのすべて |

- 名前の付け方: 動詞から始める PascalCase に `Async`。DTO は `DrawGachaRequest` / `DrawGachaResponse` のように、RPC ごとに1組作る。引数を後から足しても、ほかの RPC に影響しない。DTO の型が増えるのは受け入れる
- **`SyncAsync`**: 起動フローでホームに入る前に1回呼ぶ（[screen-design.md](screen-design.md) 4章）。副作用のある「起動時の処理」をここにまとめる
  - 期間が終わったガチャの交換pt を交換メダルに変換する（[gacha.md](../systems/gacha.md) 4章。バッチ処理にしない）
  - アプリのバージョンが違う進行中のプロデュースを破棄する（5章）
  - 日付の更新（12:00）をまたいだときの処理（いまは無し。ログインボーナスなどを足すときの置き場）
  - `GetPlayerAsync` は `[NoSideEffects]`（読み取り専用のトランザクション）なので、これらを入れられない。そのため RPC を分ける
  - 起動時に呼ばれなかった場合（アクセスの無いユーザー）は、変換されないまま残る。表示にしか影響しないので受け入れる
- **操作の RPC は局面ごとに分ける** [決定]
  - `ChooseWeekActionAsync`: 週の行動を選ぶ（レッスン、おでかけ、相談など。PRD-02）
  - `ExecuteLessonActionAsync`: レッスン・試験の中の操作（カードを使う、スキップ、ドリンクを使う。PRD-03〜08）。試験もレッスンと同じ RPC で扱う
  - DTO が型で区別でき、引数の意味が明確になる。RPC の数が増えるのは受け入れる
  - どの操作の要求も、**ターン番号**（週の行動なら週番号）を持つ。古いターンへの操作は拒否する。再送は `x-request-id` で冪等に扱う
- 取得系（`[NoSideEffects]`）は読み取り専用のトランザクションで動く（基盤の仕様）
- **マスタ配信は RPC にしない**。クライアントのマスタはバイナリを配信で取得する（[masterdata-pipeline.md](masterdata-pipeline.md) 6章）。バージョン不一致は基盤の強制アップデート・`x-app-version` で扱う
- DTO は、サーバー専用の値（`[ServerOnly]` のマスタの値など）を含めない。レンタル候補のように、サーバーが決めた結果だけを返す
- DTO の ID: サーバーが発行する ID（ULID）は26文字の `string`、マスタの ID は `int`（[repository-layout.md](repository-layout.md) 10.1）

## 2. エラー [決定。2026-10-28]

- 業務エラーは `BusinessException(ErrorCode, ...)` を投げる（基盤の仕様）。クライアントは基盤の `ErrorPolicy` に従う
- 基盤の `ErrorCode` は数値ではなく、`domain` と `reason` の2つの文字列で表す（`external/VolTech-MagicOnion` の `ErrorCode`）。そのため、以前の案の「機能ごとの番号帯」はやめた
- **domain は `uta` の1つ**。基盤の予約エラーは domain `voltech` なので、domain は「アプリか基盤か」の区別に使い、機能では分けない
- **reason は、機能の接頭辞を付けた大文字スネークケース**:

| 接頭辞 | 機能 | 例 |
|---|---|---|
| `PLAYER_` | プレイヤー・共通 | `PLAYER_NOT_ENOUGH_GOLD` |
| `GACHA_` | ガチャ（GAC） | `GACHA_NOT_ENOUGH_CURRENCY`、`GACHA_OUT_OF_PERIOD` |
| `IDOL_` | 育成アイドル（IDL） | `IDOL_NOT_ENOUGH_SHARDS` |
| `SUPPORT_` | サポートカード（SUP） | `SUPPORT_MAX_LEVEL` |
| `PRODUCE_` | プロデュース（PRD） | `PRODUCE_STALE_TURN`、`PRODUCE_SESSION_DISCARDED`、`PRODUCE_ALREADY_IN_PROGRESS` |

- エラーコードは `shared/Uta.Shared` に `static readonly ErrorCode` の定数として置き、消した reason は再利用しない（基盤の仕様）
- 個々の reason は、その機能を実装するときに足す

## 3. プレイヤーデータ（データモデル）[決定。2026-10-28／列の細部は実装時]

サーバーが発行する ID は ULID（DB では `uuid`）、マスタの ID は `int`（[repository-layout.md](repository-layout.md) 10.1）。DB は PostgreSQL、アクセスは Npgsql + Dapper（[ADR 0010](../decisions/0010-database.md)）、マイグレーションは DbUp（[ADR 0012](../decisions/0012-migration-dbup.md)）。

| テーブル | 主キー | 主な列 | 元の仕様 |
|---|---|---|---|
| `players` | `player_id` | プレイヤーLv、経験値、ゴールド、ガチャ通貨、交換メダル、作成日時 | [produce.md](../systems/produce.md) 7、[gacha.md](../systems/gacha.md) |
| `player_idols` | `player_id` + `idol_id` | トレーニング段階、覚醒段階、かけら | [idol-support.md](../systems/idol-support.md) 1、[gacha.md](../systems/gacha.md) 5 |
| `player_support_cards` | `player_id` + `card_id` | レベル、上限解放回数 | [idol-support.md](../systems/idol-support.md) 2 |
| `gacha_exchange_points` | `player_id` + `gacha_id` | 交換pt、変換済みか | [gacha.md](../systems/gacha.md) 4 |
| `gacha_histories` | `history_id` | `player_id`、`gacha_id`、何がどの確率テーブルから出たか、日時 | [gacha.md](../systems/gacha.md) 6 |
| `produce_sessions` | `session_id` | 検索用の列と状態の1列（下記） | [produce.md](../systems/produce.md) 10 |
| `produce_actions` | `session_id` + `seq` | 操作の要求（MessagePack）、日時 | [lesson-card.md](../systems/lesson-card.md) 13 |
| `produce_results` | `session_id` | `player_id`、終わり方（完了 / 放棄 / 破棄）、評価値、ランク、付与した報酬 | [produce.md](../systems/produce.md) 6 |
| 基盤のテーブル | — | `last_responses`、`game_logs`、端末・トークン | [server-foundation.md](server-foundation.md) |

- **通貨は `players` の列で持つ**。汎用の所持テーブル（`種別・ID・数`）にはしない。SQL と Dapper が素直に書ける。通貨の種類を増やすときはマイグレーションが要るが、MVP では種類が少ないので受け入れる
- かけらは育成アイドルごとに持つ（覚醒はそのアイドルのかけらを使う、と読んだ。推測。[idol-support.md](../systems/idol-support.md) 1.6 で確認する）
- 端末とプレイヤーは1対多にする（基盤の端末テーブルがプレイヤーを指す）。MVP は引き継ぎを作らないが、あとで引き継ぎコードを足せる形にしておく（[game-spec.md](../overview/game-spec.md)）
- 1ユーザーにつき、進行中のプロデュースのセッションは1つ。進行中があるときの `StartAsync` は `PRODUCE_ALREADY_IN_PROGRESS`
- 「抽選結果と所持品の更新を1トランザクションで整合させる」（[gacha.md](../systems/gacha.md) 完了条件）。冪等性の保存済みレスポンスも同じトランザクションで書く（基盤の仕様）
- インデックスは、実装するときに検索の形に合わせて付ける

### 3.1 プロデュースのセッション [決定。2026-10-28]

保存形式は [ADR 0010](../decisions/0010-database.md) のとおり、検索用の列と、状態を丸ごと入れた1列に分ける。

検索用の列:

| 列 | 内容 |
|---|---|
| `session_id` | ULID |
| `player_id` | ULID |
| `status` | 進行中 / 完了 / 放棄 / 破棄 |
| `week` / `turn` | 古いターンへの操作の拒否に使う |
| `app_version` | 開始したときの `x-app-version`（5章） |
| `state_version` | 状態の形式のバージョン（下記） |
| `version` | 楽観ロック |
| `state` | 状態（MessagePack の `bytea`） |

状態（`state`）の中身:

- [produce.md](../systems/produce.md) 10章の項目（週、体力、パラメータ、デッキ、育成アイテム、ドリンク、育成pt、編成）
- レッスン・試験の中なら:
  - カードの個体の一覧（`instanceId`、`cardId`、強化、レッスン中強化）と、それぞれのゾーン（山札の順番を含む）
  - ステータス（[lesson-card.md](../systems/lesson-card.md) 8章）と、ターン制ステータスの初付与のフェーズ（11章）
  - 持続効果・発動予約の一覧（効果グループ ID、残りターン・回数、付与の順番）
  - 残りの使用数、ターン番号、スコア・パラメータの途中経過
- 乱数: **シードと、`xoshiro256**` の現在の状態（256 ビット）の両方を持つ**。現在の状態があれば再開がすぐにでき、シードと `produce_actions` があれば最初から再現できる
- MessagePack は `[MessagePackObject]` と整数のキー（`[Key(n)]`）で書く。文字列のキーより小さく、項目の名前を変えても読める

**状態の形式のバージョン（`state_version`）**: サーバーで状態の形式を変えたら上げる。MVP の間は、古い形式を新しい形式に変換するコードは書かず、**`state_version` が今のサーバーと違うセッションは、アプリのバージョン違いと同じく破棄する**（5章）。進行中のプロデュースが消えるのは、個人開発の確認用なので受け入れる。

**操作の記録（`produce_actions`）**: 状態を変える操作（開始、週の行動、レッスン・試験の操作）を、要求のまま順番に記録する。シードと合わせて、最初から同じ結果を再現できる（不具合の調査、[lesson-card.md](../systems/lesson-card.md) 13章）。行は増えるが、セッションの終了後に消すかどうかは運用で決める。

## 4. ゲームルールの置き場 [決定]

ルールを `shared/Uta.Domain`（両側で実行できる）と、サーバー専用（`server/Uta.Server/Domain/`）のどちらに置くか（[repository-layout.md](repository-layout.md) 6.1）。

| ルール | 置き場 | 理由 |
|---|---|---|
| カード効果の処理、ゾーンの移動、ターン処理順、計算式（[lesson-card.md](../systems/lesson-card.md) 4〜12章） | `Uta.Domain` | クライアントとサーバーで結果を一致させる（15.7）。確定はサーバー |
| 使用可否の判定（コスト・使用条件。PRD-04） | `Uta.Domain` | クライアントでも判定と予測の表示に使う（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)） |
| 最終評価の計算（[produce.md](../systems/produce.md) 6章） | `Uta.Domain` | 最終試験の前後で、評価の見込みを表示できる。確定はサーバー（2026-10-28 決定） |
| 報酬の決定と付与 | サーバー専用 | 所持品・通貨の更新はサーバーのみ |
| ガチャの抽選、交換pt の加算・天井の判定 | サーバー専用 | 抽選はサーバー権威。表示用の排出率はマスタで公開する（GAC-02） |
| プロデュース開始時のシード生成、レンタル候補の決定 | サーバー専用 | 乱数・候補はクライアントを信用しない |
| 所持品・通貨の更新 | サーバー専用 | 更新はサーバーのみ |

- 最終評価の式（[produce.md](../systems/produce.md) 6章）は `2.3` や `0.15` などの小数を使う。`Uta.Domain` では `float` / `double` を使わないので、千分率の整数にする（例: `× 2.3` → `× 2300 / 1000`）。丸めの位置は式のとおり `floor` で、元の式と結果が一致することを単体テストで確かめる
- 乱数は決定的な疑似乱数 `xoshiro256**` を `IRandom` 越しに使い、`System.Random` は使わない。レッスンごとにシードを1つ持つ（[lesson-card.md](../systems/lesson-card.md) 13章）
- `Uta.Domain` は、`[ServerOnly]` のマスタの値に依存しない。必要な値は引数で受け取る（[repository-layout.md](repository-layout.md) 5章）

## 5. 中断・再開・破棄 [決定。2026-10-28]

| 場面 | 扱い |
|---|---|
| アプリの終了・通信の切断（中断） | 状態はすべてサーバーにある。期限なしで、`GetStateAsync` で再開できる |
| 放棄 | 明示の操作（`AbandonAsync`。画面で確認のダイアログを出す）。報酬はなし。`produce_results` に「放棄」で残す |
| **アプリのバージョンが違う** | 開始したときの `x-app-version` と、今の要求の `x-app-version` が違えば、そのセッションを**破棄**する。報酬はなし。`produce_results` に「破棄」で残す |
| 状態の形式が違う | `state_version` が今のサーバーと違えば、同じく破棄する（3.1） |

- 再送は `x-request-id` で冪等に扱う。ターン番号を要求に含め、古いターンへの操作は拒否する（[produce.md](../systems/produce.md) 10）
- 進行中のセッションがある間は、新しく開始できない（3章）

**アプリのバージョンで破棄する理由**: `Uta.Domain` のルールやマスタは、アプリのバージョンと一緒に変わる（[repository-layout.md](repository-layout.md) 10.5）。プロデュースの途中でルールが変わると、それまでの状態と新しいルールが食い違い、クライアントの予測とサーバーの確定もずれる。バージョンが変わったら破棄すれば、1つのセッションは常に1つのルールで最後まで進む。

- メリット: 状態の移行やルールの互換を考えなくてよい。強制アップデートと組み合わせれば、古いルールのセッションが残らない
- デメリット: アプリを更新するたびに、進行中のプロデュースが消える。個人開発の確認用なので受け入れる

破棄の流れ:

1. `SyncAsync`（起動時）で、進行中のセッションの `app_version` と `state_version` を確かめ、違えば破棄して、レスポンスで「破棄した」ことを返す。クライアントはダイアログで知らせる
2. `GetStateAsync` は `[NoSideEffects]` なので破棄しない。違うセッションは「進行中のセッション無し」として返す
3. 起動後にアプリのバージョンが変わることはないが、`SyncAsync` を経ずに操作の RPC が来た場合に備え、操作の RPC（`ChooseWeekActionAsync` など）でも確かめる。違えば破棄して `PRODUCE_SESSION_DISCARDED` を返す

- 比べるのは「違うかどうか」（新しいか古いかは見ない）。古いアプリは強制アップデートで弾かれる
- 開発中、Unity エディターから同じ `x-app-version` で接続し続ける間は破棄されない。ルールを変えた確認のときは、バージョンを上げるか、放棄してやり直す
- `x-app-version` をハンドラーから読む方法（基盤の `ServiceContext` から取れるか）は、基盤の実装で確かめる（推測）

## 未決事項

- MagicOnion の使う版で、基盤の前提とする API が揃っているか（[server-foundation.md](server-foundation.md) の未確定事項）
- 報酬の具体の値（評価ランクごとの量。[produce.md](../systems/produce.md) 6.1 は仮の値）
- `produce_actions` の行を、セッションの終了後に消すか

## 実装メモ

（実装後に仕様とのズレをここに記録する）
