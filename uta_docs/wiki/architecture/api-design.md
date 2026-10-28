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

- **[決定]**: CLAUDE.md・accepted の ADR で決まっていること
- **[Uta案]**: この叩き台の提案。サービス・RPC の名前はすべて仮
- 推測は「推測」と明記する

## 決まっていること

- 通信は MagicOnion の Unary のみ（StreamingHub は対象外）[決定。[ADR 0002](../decisions/0002-magiconion.md)]
- サービスIF・DTO は `shared/Uta.Shared` に置く（C# 9、Unity と共有）[決定／置き場の細分化は Uta案]
- サーバー権威: 育成の計算、ガチャ抽選、所持品の更新はサーバー [決定。[CLAUDE.md](../../../CLAUDE.md)]
- RPC ごとの設定（認証不要、副作用なし）は、サービスIFの属性（`[SkipAuth]` / `[NoSideEffects]`）で持つ [決定。基盤の仕様]
- 再送は `x-request-id` と `last_responses` による冪等性に任せ、仕様側で独自の重複防止を作らない [決定。[server-foundation.md](server-foundation.md)]
- プロデュースの操作の RPC は、週の行動とレッスン・試験の操作で分ける [決定。1章]
- Uta 固有のエラーコードは、機能ごとに番号帯を分ける [決定。2章]
- DB は PostgreSQL + Npgsql + Dapper。セッションは検索用の列と状態の1列に分けて保存する [決定。[ADR 0010](../decisions/0010-database.md)、3章]
- DTO ↔ Domain のマッピングはクライアントの Infrastructure に閉じる。DTO は enum とプリミティブ型で組む [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

## 1. サービスの一覧 [Uta案]

| サービス | RPC（仮） | 機能ID | 副作用 | 冪等性の対象 |
|---|---|---|---|---|
| `IDeviceService`（基盤） | Register / IssueToken | — | あり | 基盤の仕様に従う |
| `IPlayerService` | GetPlayer（所持品・通貨・Lv） | — | なし | — |
| `IGachaService` | GetGachas、Draw（単発・10連）、GetHistory、Exchange（交換pt） | GAC-01〜04 | Draw・Exchange はあり | Draw・Exchange |
| `IIdolService` | Train（トレーニング）、Awaken（覚醒） | IDL-02 | あり | Train・Awaken |
| `ISupportService` | LevelUp、LimitBreak | SUP-02 | あり | LevelUp・LimitBreak |
| `IProduceService` | GetRentalCandidates、Start、ChooseWeekAction、ExecuteLessonAction、GetState、（Abandon） | PRD-01〜14、SUP-06 | GetRentalCandidates・GetState 以外あり | Start・ChooseWeekAction・ExecuteLessonAction |

- `IProduceService` は [produce.md](../systems/produce.md) の「API案」（`StartAsync` / `ExecuteTurnAsync` / `GetStateAsync`）をもとに、`ExecuteTurn` を局面ごとに分け、レンタル候補の取得（SUP-06。サーバーがマスタから候補を返す。[idol-support.md](../systems/idol-support.md) 2.6）を足した形
- **操作の RPC は局面ごとに分ける** [決定]
  - `ChooseWeekAction`: 週の行動を選ぶ（レッスン、おでかけ、相談など。PRD-02）
  - `ExecuteLessonAction`: レッスン・試験の中の操作（カードを使う、スキップ、ドリンクを使う。PRD-03〜08）。試験もレッスンと同じ RPC で扱う
  - DTO が型で区別でき、引数の意味が明確になる。RPC の数が増えるのは受け入れる
  - どの操作の要求も、**ターン番号**（週の行動なら週番号）と**次の操作番号**を持つ。古いターンへの操作や、サーバーの操作番号と合わない要求は拒否する（クライアントは `GetState` で取り直す）。再送は `x-request-id` で冪等に扱う。操作番号は同じターンに複数の操作をするときの区別に使う（[produce.md](../systems/produce.md) 10.4。2026-10-28 決定）
- 取得系（`[NoSideEffects]`）は読み取り専用のトランザクションで動く（基盤の仕様）
- **マスタ配信は RPC にしない**。クライアントのマスタはバイナリを配信で取得する（[masterdata-pipeline.md](masterdata-pipeline.md) 6章）。バージョン不一致は基盤の強制アップデート・`x-app-version` で扱う
- DTO は、サーバー専用の値（`[ServerOnly]` のマスタの値など）を含めない。レンタル候補のように、サーバーが決めた結果だけを返す

## 2. エラー [決定／番号帯は Uta案]

- 業務エラーは `BusinessException(ErrorCode, ...)` を投げる（基盤の仕様）。クライアントは基盤の `ErrorPolicy` に従う
- **Uta 固有のエラーコードは、機能ごとに番号帯を分ける** [決定]。基盤の `ErrorCode` の形式に合わせる（数値を持てない形式なら見直す）
  - 番号帯の案: 1000番台＝プレイヤー・共通、2000番台＝ガチャ（GAC）、3000番台＝育成アイドル（IDL）、4000番台＝サポートカード（SUP）、5000番台＝プロデュース（PRD）
  - 番号から機能が分かり、設計書と対応づけやすい。番号帯が足りなくなったら、その機能に次の帯を足す
- エラーコードは `shared/Uta.Shared` に C# の定数として置き、消した reason は再利用しない（基盤の仕様）

## 3. プレイヤーデータ（データモデル）[決定／列は Uta案]

保存する対象の一覧。サーバーが発行する ID（ユーザー、セッション、履歴など）は ULID、マスタの ID は `int`（[repository-layout.md](repository-layout.md) 10.1）。そのほかの列・型・キーは未決。DB は PostgreSQL、アクセスは Npgsql + Dapper（[ADR 0010](../decisions/0010-database.md)）。

| 対象 | 主な内容 | 元の仕様 |
|---|---|---|
| プレイヤー | プレイヤーLv、ゴールド、ガチャ通貨、交換メダル、かけら | [produce.md](../systems/produce.md) 7、[gacha.md](../systems/gacha.md) |
| 所持育成アイドル | アイドル ID、トレーニング段階、覚醒段階 | [idol-support.md](../systems/idol-support.md) 1 |
| 所持サポートカード | カード ID、レベル、上限解放回数 | [idol-support.md](../systems/idol-support.md) 2 |
| ガチャの交換pt | ガチャごとの交換pt（期間終了で交換メダルへ変換） | [gacha.md](../systems/gacha.md) 4 |
| ガチャ履歴 | 1回ごとに、何がどの確率テーブルから出たか | [gacha.md](../systems/gacha.md) 6 |
| プロデュースのセッション | 週・体力・パラメータ・デッキ・ドリンク・育成pt・シード・乱数の位置・状態、レッスン中は山札・手札・捨札・除外とステータス | [produce.md](../systems/produce.md) 10 |
| プロデュースの結果 | 最終評価、報酬 | [produce.md](../systems/produce.md) 6 |
| 基盤のテーブル | `last_responses`、`game_logs`、端末・トークン | [server-foundation.md](server-foundation.md) |

- 1ユーザーにつき、進行中のプロデュースのセッションは1つ（仮。[produce.md](../systems/produce.md) 10）
- 「抽選結果と所持品の更新を1トランザクションで整合させる」（[gacha.md](../systems/gacha.md) 完了条件）。冪等性の保存済みレスポンスも同じトランザクションで書く（基盤の仕様）
- **セッションの保存形式** [決定。[ADR 0010](../decisions/0010-database.md)]
  - 検索に使う列（ユーザー、状態、週、ターン、バージョン）と、状態を丸ごと入れた1列に分ける
  - 状態はサーバーで MessagePack にして `bytea` に入れる。バージョン列で楽観ロックをかける
  - 1ターンの読み書きが1行で済み、状態の構造を変えても DB のスキーマはそのまま。そのかわり、状態の形式を変えるときは移行の仕組み（形式のバージョンを持つなど）が要る
  - 状態の中身（再開に要る内容）は [produce.md](../systems/produce.md) 10章

## 4. ゲームルールの置き場 [Uta案]

ルールを `shared/Uta.Domain`（両側で実行できる）と、サーバー専用（`server/Uta.Server/Application`）のどちらに置くか。

| ルール | 置き場 | 理由 |
|---|---|---|
| カード効果の処理、ゾーンの移動、ターン処理順、計算式（[lesson-card.md](../systems/lesson-card.md) 4〜12章） | `Uta.Domain` | クライアントとサーバーで結果を一致させる（15.7）。確定はサーバー |
| 使用可否の判定（コスト・使用条件。PRD-04） | `Uta.Domain` | クライアントでも判定と予測の表示に使う（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)） |
| ガチャの抽選、交換pt の加算・天井の判定 | サーバー専用 | 抽選はサーバー権威。表示用の排出率はマスタで公開する（GAC-02） |
| プロデュース開始時のシード生成、レンタル候補の決定 | サーバー専用 | 乱数・候補はクライアントを信用しない |
| 最終評価・報酬の計算 | 未決（推測では `Uta.Domain`） | 評価の予測表示が要るか次第 |
| 所持品・通貨の更新 | サーバー専用 | 更新はサーバーのみ |

- 乱数は決定的な疑似乱数 `xoshiro256**` を `IRandom` 越しに使い、`System.Random` は使わない。シードはセッションに1つで、用途・場面ごとのストリームに分ける（[produce.md](../systems/produce.md) 10.2）
- `Uta.Domain` は、`[ServerOnly]` のマスタの値に依存しない。必要な値は引数で受け取る（[repository-layout.md](repository-layout.md) 5章）

## 5. 再開・中断の扱い（推測）

- 状態はすべてサーバーが持ち、クライアントは `GetState` で復元する（[produce.md](../systems/produce.md) 完了条件「中断・再開」）
- 再送は `x-request-id` で冪等に扱う。ターン番号と次の操作番号を要求に含め、合わない操作は拒否する（[produce.md](../systems/produce.md) 10.4）
- サーバーは入力待ちから次の入力待ちまでを1回で処理して保存するので、再開は最後の入力待ちから続けるだけになる（[produce.md](../systems/produce.md) 10.1）
- 途中放棄の扱い（報酬なしで破棄 / 一定期間で失効）は未決

## 未決事項

- サービス・RPC の確定した名前（すべて仮）
- エラーコードの番号帯の確定（基盤の `ErrorCode` の形式を確認してから）
- プレイヤーデータの列・キー・インデックス、マイグレーションのツール
- 状態の形式のバージョンを上げたときの移行の方法（状態の中身は [produce.md](../systems/produce.md) 10.5 で決定）
- 報酬（評価ランクごとの通貨など）と、ガチャ通貨の入手手段（[gacha.md](../systems/gacha.md)）
- 最終評価・報酬の計算の置き場（4章）
- ガチャの期間終了時の交換pt → 交換メダルの変換（バッチ処理）の実行方法
- MagicOnion の使う版で、基盤の前提とする API が揃っているか（[server-foundation.md](server-foundation.md) の未確定事項）

## 実装メモ

（実装後に仕様とのズレをここに記録する）
