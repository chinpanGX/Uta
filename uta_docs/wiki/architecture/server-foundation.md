---
title: サーバー基盤（MagicOnion + PostgreSQL）
status: draft
updated: 2026-10-31
---

# サーバー基盤（MagicOnion + PostgreSQL）

Uta のサーバーが土台にする C# サーバー基盤の要約。原典は設計書「C#サーバー基盤 設計書(MagicOnion + PostgreSQL)」（[Claude Docs](https://claude.ai/artifact/STu2PxLjvuAajZwaMNqURq)、2026-10-23 版）。
採用の判断は [ADR 0004](../decisions/0004-server-foundation.md)。原典が更新されたら、このページも更新する。

## 決まっていること

- サーバーは **.NET 10**（C# 14 が使える）
- 確定: MasterMemory、MagicOnion、MessagePack（MasterMemory と MagicOnion が前提にする）
- 通信は Unary のみ（StreamingHub は対象外。[ADR 0002](../decisions/0002-magiconion.md)）
- シャーディングは実装しない。後から入れられる形（`IShardResolver`）だけ残す

## 推奨（未確定）

- DB: PostgreSQL + Npgsql + Dapper、マイグレーション: DbUp
- ログ: ZLogger、テスト: xUnit + Testcontainers

## プロジェクト構成

| プロジェクト | 中身 | Unity と共有 |
|---|---|---|
| `Server.Abstractions` | `ErrorCode`、`BusinessException`、ヘッダー名の定数、属性（`[SkipAuth]`・`[NoSideEffects]`）、`IApiResponse` | する（netstandard2.1） |
| `Server.Status` | メンテナンス・強制アップデート・日付変更の判定、設定の読み込み | しない |
| `Server.Auth` | `DeviceService`、トークン、`AuthFilter`、`IPlayerResolver` | しない |
| `Server.Transaction` | Npgsql、冪等性、ゲームログ、`IShardResolver` | しない |
| `Server.Logging` | ZLogger、`RpcLogFilter` | しない |
| `Server.Hosting` | `AddVoltechServer()`。フィルターの順序を固定する | しない |

- Unity から参照するのは `Abstractions` だけ。サーバー専用の依存（Npgsql など）を Unity に入れないため
- アプリは必要なプロジェクトだけを参照し、`AddVoltechServer()` で組み立てる

## フィルターの順序（外側から）

1. `RpcLogFilter`: RPC ごとのログ。例外を一元変換する（`BusinessException` → `FAILED_PRECONDITION` + ErrorInfo、想定外 → `INTERNAL`）
2. `ServerStatusFilter`: 強制アップデート・メンテナンスの判定。レスポンスに `x-server-time` と `x-game-date` を付ける。DB は使わない
3. `AuthFilter`: Bearer を検証して `AuthContext` を入れる。`[SkipAuth]` は通さない
4. `DateChangeFilter`: `x-game-date` が今のゲーム内の日付と違えば `DATE_CHANGED`
5. ハンドラー: 本体を `ITransactions.RunAsync` で包む

トランザクションと冪等性をフィルターにしないのは、判定と保存済みレスポンスの書き込みを本体と同じトランザクションで行うため。順序は `AddVoltechServer()` が固定し、アプリは入れ替えられない。

## エラー

| 種類 | ステータス | ErrorInfo |
|---|---|---|
| 業務エラー（メンテナンス・強制アップデート含む） | `FAILED_PRECONDITION` | アプリのエラーはアプリの domain、基盤の予約エラーは domain `voltech` |
| サーバー内部エラー | `INTERNAL` | `voltech` / `INTERNAL_ERROR`。詳細はログにだけ出す |
| 認証エラー | `UNAUTHENTICATED` | なし |

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
- プレイヤー状態: `IPlayerResolver`（NotCreated / Active / Suspended）をアプリが実装。停止中は `ACCOUNT_SUSPENDED`
- 冪等性: 対象の RPC は同じ `x-request-id` の再送で本体を呼ばず、保存済みのレスポンスを返す。保存先は `last_responses(player_id, request_id, rpc, response)` で、本体と同じトランザクションで書く
- `[NoSideEffects]` の RPC は読み取り専用のトランザクション
- ゲームログの出力先は DB の `game_logs` か標準出力（JSON）

## Uta での扱い（推測）

- Uta の `shared/` に置く Uta 固有のサービスIF・DTO は、基盤の `Abstractions` と同じく Unity と共有する。マスタ型は `shared/` に置かない（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- 育成の操作（`ChooseWeekActionAsync` などの操作の RPC。[api-design.md](api-design.md) 1章）やガチャは冪等性の対象

## 未確定事項

- 基盤の取り込み方は Git サブモジュールの方針（[ADR 0005](../decisions/0005-foundation-submodule.md)、proposed）。配置先・固定するタグ・Unity からの参照方法、Uta の `shared/` と `Abstractions` の関係は未決
- MagicOnion の使う版で次の API があるか: `IMagicOnionServiceFilter` / `GlobalFilters` / `ServiceContext.Items`、クライアントの `WithOptions(CallOptions)` / `ResponseHeadersAsync`、サーバーから投げた `RpcException` のトレーラーがクライアントに届くか
- DB の最終決定（PostgreSQL + Npgsql + Dapper が推奨）
- `server_status.toml`（メンテナンス・最低バージョン・ゲーム日付）と同じ設定形式にするか
- 分岐元にする基盤のタグ（v0.2〜v0.6 で互換のない変更が続いている）
- Tracing（gRPC Monitor）の扱い、`StoredAccount` のシリアライズ形式
