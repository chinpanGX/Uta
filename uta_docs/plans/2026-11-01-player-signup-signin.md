---
title: 実装計画: プレイヤーのサインアップ・サインイン（サーバー）
status: draft
updated: 2026-11-01
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/overview/game-spec.md
---

# 実装計画: プレイヤーのサインアップ・サインイン（サーバー）

**初期案（ブレスト用）。** 「未決事項」の案をユーザーと固めてから、設計レビューに出す。

## 1. 目的と前提

- 何を実装するか: `IPlayerService` の `SignUpAsync`（プレイヤーの作成）と `SignInAsync`（所持データの全件取得）を、サーバーと `Uta.Shared.Contracts` に作り、結合テストで通す。仕様は [api-design.md](../wiki/architecture/api-design.md) 1章の「サインアップとサインイン」、エラーは同 2章の `PLAYER_NOT_CREATED`。
- 前提: 1a（[2026-11-01-connectivity-check-server.md](2026-11-01-connectivity-check-server.md)）が済んでいる。端末の登録・トークンの発行・`PlayerResolver`・`players` テーブル（`player_id`・`created_at` だけ）がある。
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0010](../wiki/decisions/0010-database.md)、[0012](../wiki/decisions/0012-migration-dbup.md)
- **この計画で決めないこと**（後の計画に回す。案は未決事項 1）:
  - 初期の所持品の付与（マスタの `InitialSetting`・育成アイドル・サポートカードが要る。masterdata の計画と `player_idols`・`player_support_cards` の計画の後）
  - バージョン違いのプロデュースの破棄（`produce_sessions` が無い。プロデュースの計画）
  - クライアント側（`IPlayerService` を呼ぶユースケース。1b の後の計画）

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `shared/Uta.Shared.Contracts/Player/IPlayerService.cs` | サービス IF（`SignUpAsync`・`SignInAsync`） |
| 作成 | `shared/Uta.Shared.Contracts/Player/SignUpResponse.cs`・`SignInResponse.cs` | DTO |
| 削除 | `shared/Uta.Shared.Contracts/ContractsAssemblyMarker.cs`（と `.meta`） | 中身ができたので消す（印のコメントの指示どおり） |
| 作成 | `server/Uta.Server/Migrations/0002_add_player_columns.sql` | `players` に列を足す |
| 作成 | `server/Uta.Server/Api/PlayerService.cs` | MagicOnion のサービス実装 |
| 作成 | `server/Uta.Server/Application/Player/SignUpUseCase.cs`・`SignInUseCase.cs`・`IPlayerRepository.cs` | ユースケースとポート |
| 作成 | `server/Uta.Server/Infrastructure/PlayerRepository.cs` | Dapper の実装 |
| 変更 | `server/Uta.Server/Program.cs` | DI の登録 |
| 作成 | `server/Uta.Server.Tests/Integration/PlayerSignUpSignInTests.cs` | 結合テスト |
| 変更 | `uta_docs/wiki/architecture/api-design.md`・`repository-layout.md` | 実装とのズレを反映（完了時） |

（Api・Application の置き方は [repository-layout.md](../wiki/architecture/repository-layout.md) 3章。ここまでのサーバーは Infrastructure しか無いので、層のフォルダを足す）

## 3. 型とシグネチャ（初期案）

```csharp
// shared/Uta.Shared.Contracts/Player/IPlayerService.cs（C# 9・netstandard2.1）
public interface IPlayerService : IService<IPlayerService>
{
    UnaryResult<SignUpResponse> SignUpAsync();
    UnaryResult<SignInResponse> SignInAsync();
}

[MessagePackObject] public sealed class SignUpResponse { [Key(0)] public bool Created { get; set; } }

[MessagePackObject] public sealed class SignInResponse
{
    [Key(0)] public int Level { get; set; }
    [Key(1)] public int Exp { get; set; }
    [Key(2)] public int Gold { get; set; }
    [Key(3)] public int SupportPoints { get; set; }
    [Key(4)] public int GachaCurrency { get; set; }
    [Key(5)] public bool HasActiveProduce { get; set; }   // この計画では常に false
    [Key(6)] public bool DiscardedProduce { get; set; }   // この計画では常に false
}
```

DB（`0002_add_player_columns.sql`）: `players` に `level int NOT NULL DEFAULT 1`、`exp`・`gold`・`support_points`・`gacha_currency` を `int NOT NULL DEFAULT 0`（通貨を `players` の列で持つ。api-design 3章）。

`SignUpAsync`: `INSERT INTO players (player_id) VALUES (@id) ON CONFLICT DO NOTHING`。挿入できたら `Created = true`。`SignInAsync`: 行が無ければ `PLAYER_NOT_CREATED`、あれば列を読んで返す。

## 4. タスク（初期案）

1. **Contracts**: IF と DTO、マーカーの削除。Unity 側の `.meta`・asmdef を確認（Unity のビルドは 1b の側）。確かめ: `dotnet build Uta.slnx`
2. **マイグレーション**: `0002` と、`UtaDatabase` が拾うことの確認。確かめ: 既存のマイグレーションの結合テスト
3. **Application・Infrastructure**: ユースケース、ポート、`PlayerRepository`。`PLAYER_NOT_CREATED` を返す例外の型と変換（未決事項 3）
4. **Api**: `PlayerService`。未作成のプレイヤーを弾く共通処理（`SignUpAsync` 以外。未決事項 3）。`Program.cs` の登録
5. **結合テスト**: 下の 5章

境界: `SignUpAsync` の 2 回目以降は何もしない（`Created = false`）。同じ `x-request-id` の再送は基盤が保存済みのレスポンスを返す。`SignInAsync` も副作用ありの扱い（冪等性の対象）。

## 5. テスト（初期案）

- `SignUp_NewPlayer_CreatesRow`（`Created = true`、`players` に 1 行）
- `SignUp_Twice_DoesNothing`（別の `x-request-id` で 2 回目、`Created = false`、行は 1 行のまま）
- `SignUp_SameRequestId_ReturnsStoredResponse`
- `SignIn_AfterSignUp_ReturnsDefaults`（Level 1、通貨 0、`HasActiveProduce` false）
- `SignIn_BeforeSignUp_ReturnsPlayerNotCreated`
- `SignIn_WithoutToken_Unauthenticated`
- アーキテクチャテスト: Api → Application → Infrastructure の依存（既存の 6 件が通ること）

実行: `dotnet test --project server/Uta.Server.Tests`（Docker が要る）

## 6. 未決事項（ブレストの論点。案つき）

1. ~~初期付与をこの計画に入れるか~~ [決定。2026-11-01。ユーザーの判断]: 入れない（案 A）。`players` の行だけ作り、付与は masterdata と所持テーブルの計画で `SignUpAsync` の中身に足す。
2. **`SignInResponse` の中身**: 案 = `players` の列（Level・Exp・通貨 3 種）と、破棄・再開の 2 値だけ。育成アイドル・サポートカードの一覧は、`player_idols`・`player_support_cards` の計画で足す。DTO は RPC に属さない型（`PlayerData`）に切るか（api-design 未決事項）。`GetPlayerAsync` を残すか。
3. **`PLAYER_NOT_CREATED` の実装**: 例外の型（MagicOnion の `ReturnStatusException` ＋ `ErrorCode`）、クライアントが受け取る条件（`handledCodes`）、`SignUpAsync` 以外を弾く仕組み（フィルターか、`ServiceBase` を継承した共通の基底クラスか）。基盤の `ErrorDefinition` と `ApiCore` の扱いを読んで案を出す。
4. **Level の初期値と `exp`**: 仕様（produce.md 7）の初期値の確認。
5. **`HasActiveProduce` / `DiscardedProduce` の仮実装**: 常に false で問題ないか（プロデュースの計画で本実装）。
6. ~~ゲームログ~~: 1 の決定により、付与が無いので `SignUpAsync` は `game_logs` を書かない（付与を足す計画で書く）。
7. 起動の流れを 1 本にするか（screen-design 4章）。この計画のサーバーには影響しないが、クライアントの計画の前に決める。
