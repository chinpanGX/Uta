---
title: サーバー基盤（MagicOnion + PostgreSQL）
status: draft
updated: 2026-11-01
---

# サーバー基盤（MagicOnion + PostgreSQL）

Uta のサーバーが**サーバー基盤（VolTech-MagicOnion）をどう使うか**の正 [決定。2026-11-01。ユーザーの判断]。基盤そのものの仕様・構成・使い方の正は VolTech-MagicOnion のリポジトリの docs（仕様は `external/VolTech-MagicOnion/docs/spec/`、構成は `docs/design/`、使い方は `docs/guide/`）で、このページは要点だけを書いてリンクする。基盤の docs と食い違ったら基盤を正とし、このページを直す。Uta の決めごと（ログ・ゲームログ・プレイヤー未作成の扱い・SQL の層・冪等性の対象）はこのページが正。`Program.cs`・マイグレーション・設定の骨組みは [repository-layout.md](../repository-layout.md) 12章。
最初の原典は設計書「C#サーバー基盤 設計書(MagicOnion + PostgreSQL)」（[Claude Docs](https://claude.ai/artifact/STu2PxLjvuAajZwaMNqURq)、2026-10-23 版）。採用の判断は [ADR 0004](../../decisions/0004-server-foundation.md)。

## 決まっていること

- サーバーは **.NET 10**（C# 14 が使える）
- 確定: MasterMemory、MagicOnion、MessagePack（MasterMemory と MagicOnion が前提にする）
- 通信は Unary のみ（StreamingHub は対象外。[ADR 0002](../../decisions/0002-magiconion.md)）
- シャーディングは実装しない（VolTech-MagicOnion のスコープ外。`IShardResolver` は今の VolTech-MagicOnion に無い）
- DB は PostgreSQL + Npgsql（VolTech-MagicOnion は Dapper に依存しない。Uta は Dapper を使う。[ADR 0010](../../decisions/0010-database.md)）、マイグレーションは DbUp（[ADR 0012](../../decisions/0012-migration-dbup.md)）
- テストは xUnit + Testcontainers（Uta は xunit.v3。[repository-layout.md](../repository-layout.md) 10.4）

## Uta の決めごと [決定。2026-11-01。ユーザーの判断]

- **ログの出力は ZLogger**。VolTech-MagicOnion は `ILogger` に出すだけで、出力先と形式はアプリが決める。標準出力に JSON で出す（Cloud Run が Cloud Logging に送る）。`Program.cs` で `AddZLoggerConsole` を構成し、版は 2.5.10（2026-11-01 に決めた。VolTech-MagicOnion と衝突しない）
- **ゲームログ（`game_logs`）は書く**。所持品・通貨を変える RPC（`DrawAsync`・`ExchangeAsync`・`LevelUpAsync`・`LimitBreakAsync`・`SyncAsync` の初期付与・プロデュースの報酬の付与）は、本体と同じトランザクションで 1 件書く。中身（種類・増減した量・関係する ID）は各 RPC の実装計画で決める
- **プレイヤーがまだいないとき**: `AuthFilter` はプレイヤー未作成（`NotCreated`）でも RPC を通す。Uta は `Api` の共通処理で、`SyncAsync` 以外の Uta の RPC が未作成のプレイヤーから来たら `PLAYER_NOT_CREATED`（domain `uta`）を返す。クライアントは起動の流れで必ず `SyncAsync` を先に呼ぶので、ふつうは起きない
- **SQL を書く層**: SQL（Dapper）は Infrastructure のリポジトリに置き、Application はポート（インターフェース）越しに呼ぶ。ガチャの New / Duplicate の判定などの決まりはサーバーの Domain に置く。トランザクション（VolTech-MagicOnion の `TxContext`）は Api が `RunAsync` で開き、リクエストごとのスコープでリポジトリに渡す（[repository-layout.md](../repository-layout.md) 3章の依存の表）

## プロジェクト構成

2026-10-31 時点の VolTech-MagicOnion の実装（`external/VolTech-MagicOnion/docs/design/project-structure.md` が正）。原典の設計書は関心ごとごとの 6 プロジェクトだったが、VolTech-MagicOnion は「Unity と共有するか」で 2 つに分け、関心ごとはフォルダで分ける形に変えた。

| プロジェクト | 中身 | Unity と共有 |
|---|---|---|
| `VolTech.Server.Abstractions` | `ErrorCode`、予約エラーの定数、`BusinessException`、ヘッダー名の定数、属性（`[SkipAuth]`・`[NoSideEffects]`）、`IDeviceService`。ソースの実体は Unity パッケージ `com.chinpangx.voltech.magiconion` の `Runtime/Abstractions/` | する（netstandard2.1） |
| `VolTech.Server` | フォルダ `Status/`（サーバー時刻・日付変更）、`Auth/`（`DeviceService`・トークン・`AuthFilter`・`IPlayerResolver`）、`Transaction/`（`ITransactions`・冪等性・ゲームログ・基盤のテーブルのマイグレーション）、`Logging/`（`RpcLogFilter`）、`Hosting/`（`AddVoltechServer()`。フィルターの順序を固定する） | しない（net10.0） |

- Unity から参照するのは `Abstractions` とクライアント（Unity パッケージ）だけ。サーバー専用の依存（Npgsql など）を Unity に入れないため
- アプリは `VolTech.Server` を参照し、`AddVoltechServer()` で組み立てる。Uta からの参照のしかたと VolTech-MagicOnion の扱いは [repository-layout.md](../repository-layout.md) 3章・3.1

## フィルターの順序（外側から）

VolTech-MagicOnion の `docs/spec/processing-order.md` が正。

1. `RpcLogFilter`: RPC ごとのログ。例外を一元変換する（`BusinessException` → `FAILED_PRECONDITION` + ErrorInfo、想定外 → `INTERNAL`）
2. `ServerTimeFilter`: レスポンスに `x-server-time` と `x-game-date` を付ける。DB は使わない。**強制アップデートとメンテナンスの判定は持たない**（VolTech-MagicOnion のスコープ外。Uta も実装しない。[repository-layout.md](../repository-layout.md) 10.5）
3. `AuthFilter`: Bearer を検証して `AuthContext`（端末 ID・プレイヤー ID・プレイヤーを作ったか）を入れる。`[SkipAuth]` の RPC は検証を省く
4. `DateChangeFilter`: `x-game-date` が今のゲーム内の日付と違えば `DATE_CHANGED`
5. ハンドラー: 本体を `ITransactions.RunAsync` で包む

トランザクションと冪等性をフィルターにしないのは、判定と保存済みレスポンスの書き込みを本体と同じトランザクションで行うため。順序は `AddVoltechServer()` が固定し、アプリは入れ替えられない。

## エラー

| 種類 | ステータス | ErrorInfo |
|---|---|---|
| 業務エラー | `FAILED_PRECONDITION` | アプリのエラーはアプリの domain、VolTech-MagicOnion の予約エラーは domain `voltech` |
| サーバー内部エラー | `INTERNAL` | `voltech` / `INTERNAL_ERROR`。詳細はログにだけ出す |
| 認証エラー | `UNAUTHENTICATED` | なし |
| 要求の形の誤り | `INVALID_ARGUMENT` | `x-request-id` が無い・形が違う・別の RPC で使い回した（冪等性の対象の RPC）、端末の登録の引数が不正。VolTech-MagicOnion の `Transactions`・`DeviceService` が返す |

- `google.rpc.Status` の details に `ErrorInfo` を入れ、トレーラー `grpc-status-details-bin` で返す
- ハンドラーとフィルターは `BusinessException(ErrorCode, message, metadata?)` を投げるだけにする
- reason を消しても使い回さない（古いアプリが `UNKNOWN_ERROR` として扱う）

## ヘッダー

| 向き | ヘッダー | 内容 |
|---|---|---|
| リクエスト | `authorization` | `Bearer <token>`（`[SkipAuth]` には付かない） |
| リクエスト | `x-request-id` | 冪等性の対象の RPC のみ。1〜64 文字、英数字と `-` |
| リクエスト | `x-app-platform` | ios / android / windows / editor |
| リクエスト | `x-app-version` | リリースごとに増やす整数 |
| リクエスト | `x-game-date` | セッションの最初に受け取った日付（YYYY-MM-DD） |
| レスポンス | `x-server-time` | Unix 時間（ミリ秒） |
| レスポンス | `x-game-date` | 今のゲーム内の日付 |

RPC ごとの設定（認証不要、副作用なし）は、共有インターフェースの属性で持つ。サーバーとクライアントが同じ属性を読むので食い違わない。

## 認証・冪等性・トランザクション

- 認証: `DeviceService`（Register / IssueToken、`[SkipAuth]`）。トークンの有効期限は既定 1 時間。無効・期限切れは `UNAUTHENTICATED` で、クライアントがトークンを取り直して 1 回だけ再送する
- プレイヤー状態: `IPlayerResolver`（NotCreated / Active）をアプリが実装し、`players` の行があるかで返す。アカウント停止は持たない（`ACCOUNT_SUSPENDED` は定義だけ）
- 端末の登録（`RegisterAsync`）は `player_id`（ULID）を発行するだけで、プレイヤーの行はアプリが作る（Uta では `SyncAsync`。[api-design.md](../api-design.md) 1章）
- 設定は `appsettings.json` の `Voltech` セクション（`GameDay.ResetHour`・`GameDay.TimeZone`・`AccessTokenLifetime`）。Uta の値は [repository-layout.md](../repository-layout.md) 12章
- VolTech-MagicOnion のテーブル（`devices`・`access_tokens`・`last_responses`・`game_logs`）は `VoltechDatabase.Migrate()` が作る。記録は `_voltech_migrations` で、アプリのマイグレーションとは別。`player_id` は `char(26)`（ULID の文字列。2026-10-31 に `uuid` から変更。[repository-layout.md](../repository-layout.md) 10.1・12章）
- 冪等性: 対象の RPC は、共有インターフェースのメソッドに `[NoSideEffects]` も `[SkipAuth]` も付かないもの。同じ `x-request-id` の再送では本体を呼ばず、保存済みのレスポンスを返す。保存先は `last_responses(player_id, request_id, rpc, response)` で、本体と同じトランザクションで書く
  - `last_responses` は `player_id` が主キーで、**プレイヤーごとに直前の 1 件だけ**を持つ。本体の前にその行を `FOR UPDATE` でロックするので、同じプレイヤーの副作用のある要求は直列になる（Uta はこれに頼り、`players` を明示的にロックしない。[api-design.md](../api-design.md) 3章）
  - 直前の 1 件しか見ないので、クライアントは要求を 1 つずつ順に送る（VolTech-MagicOnion の `ApiCore` の順番待ち）。Uta の RPC は `ApiCore` を通してだけ呼ぶ
- `[NoSideEffects]` の RPC は読み取り専用のトランザクション
- ゲームログの出力先は DB の `game_logs` だけ（本体と同じトランザクションで書く。2026-10-31 に「標準出力（JSON）」を消した。基盤に無い選択肢だった）

## Uta での扱い

- Uta 固有のサービスIF・DTO（`Uta.Shared.Contracts`）は、VolTech-MagicOnion の `Abstractions` と同じく Unity と共有する。マスタ型は `shared/` に置かない（[ADR 0007](../../decisions/0007-masterdata-with-addons.md)）
- 冪等性の対象は、上の属性の規則で決まる（副作用のある Uta の RPC はすべて。一覧は [api-design.md](../api-design.md) 1章）

## 未確定事項

- （2026-10-31 に解決）VolTech-MagicOnion の取り込み方 → Git サブモジュール（[ADR 0005](../../decisions/0005-foundation-submodule.md)、proposed）で `external/VolTech-MagicOnion` に置く。サーバーは `ProjectReference`、Unity は `file:` で参照し、`Uta.Shared.Contracts` は `VolTech.Server.Abstractions` を参照する（[repository-layout.md](../repository-layout.md) 3章）
- （2026-10-31 に解決）MagicOnion の API が揃うか → VolTech-MagicOnion が MagicOnion 7.11.0 で実装・テスト済み
- （2026-10-31 に解決）DB の最終決定 → [ADR 0010](../../decisions/0010-database.md)
- （2026-10-31 に解決）`server_status.toml` と同じ設定形式にするか → 強制アップデート・メンテナンスを実装しないので不要。ゲーム日付は `Voltech` セクション
- （2026-10-31 に解決）分岐元にする VolTech-MagicOnion のタグ → タグを待たず、サブモジュールのポインタ（コミット）で固定する。区切りで VolTech-MagicOnion の版を出す（[repository-layout.md](../repository-layout.md) 3.1）
- 基盤の機能のうち Uta で使うか未定のもの（MVP には要らない）: Tracing（gRPC の呼び出しの記録）、`StoredAccount`（クライアントが端末の資格情報を保存する形式）のシリアライズ形式
