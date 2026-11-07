---
title: 実装計画: 共通レスポンス項目（player_diff）をサーバーとクライアントに入れる
status: draft
updated: 2026-11-08
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/architecture/server/gacha-support-server.md, ../wiki/architecture/server/produce-server.md
---

# 実装計画: 共通レスポンス項目（player_diff）

## 0. ユーザーの確認が要ること（推奨で書いてある。違う場合は直す）

ユーザーが「共通レスポンスも採る」と決めた（2026-11-08）。基盤の仕様は VolTech-MagicOnion の `docs/spec/common-response.md`・`docs/guide/server.md`「共通レスポンス項目」。次は、その上での Uta の決め方。

1. **`AppCommon` は player_diff だけを持つ**。バッジと通知は MVP に仕様が無い（`screen-wireframes.md`「作らない」: ミッション・お知らせほか）ので持たない。後で `Key(1)`・`Key(2)` に足せる。
2. **player_diff の中身は「変更後の値」**: 通貨とレベルの全部（レベル・経験値・ゴールド・サポートポイント・ガチャ通貨）、触ったアイドル、触ったサポートカード、無くなったサポートカードの uid。変化量は持たない（基盤の決めごと）。
3. **記録のしかたは「触ったキーだけを記録し、`BuildAsync` で DB から読み直す」**。リポジトリの書き込みメソッドの引数と戻り値は変えない（書き込みの中で `tx.GetCollector<PlayerDiffCollector>().Touch…()` を 1 行呼ぶだけ）。
4. **`Common` を載せるレスポンス**: `DrawResponse`・`ExchangeResponse`・`LevelUpResponse`・`LimitBreakResponse`・`ProduceStateResponse`（報酬が付くのは操作の結果。`Result` を持つ型）。載せない: `SignInResponse`（全件を返すので呼び出し側が置き換える）、`SignUpResponse`、`GetGachasResponse`、`ProduceResultResponse`（`ProduceStateResponse.Result` に入る入れ子の型で、`AbandonAsync` が単独で返すときも所持品は変わらないため）。
5. **既にあるレスポンスの項目（`DrawResponse.GachaCurrency` など）は消さない**。重複するが、順 7 の画面計画がそれらを読む形で組まれているため、消すのは画面が出来てから別に決める。
6. **クライアントの反映先は `PlayerCache`**（`Apply(PlayerDiff)` を足す）。`SignIn` で置き換えたスナップショットに、差分をマージする。

この計画は [2026-11-08-client-foundation-migration.md](2026-11-08-client-foundation-migration.md)（基盤の新しい呼び方への移行）の**後**に実装する（クライアントのタスクが、その計画の `UtaApiClient.Create` を変える）。

## 1. 目的と前提

サーバーが、所持品・通貨・レベルを変える RPC のレスポンスに、変更後の値（player_diff）を `Common` として載せる。クライアントは、呼び出し側に返す前にそれを `PlayerCache` に反映する。これで、画面が RPC の結果を読んだ時点で、キャッシュはサーバーと一致している。

- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)
- **この計画で決めないこと**: バッジ、通知、`SignInResponse` の形、既存レスポンス項目の削除、`HasActiveProduce`・`DiscardedProduce` の差分（プロデュースの状態は `ProduceStateResponse` 自体が持つ）、冪等性の再送テストを超えるクライアント側の新テスト基盤

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `shared/Uta.Shared.Contracts/Common/AppCommon.cs` | `AppCommon`（`PlayerDiff` を持つ） |
| 作成 | `shared/Uta.Shared.Contracts/Common/PlayerDiff.cs` | player_diff の DTO |
| 変更 | `shared/Uta.Shared.Contracts/Gacha/DrawResponse.cs`・`ExchangeResponse.cs`、`Support/LevelUpResponse.cs`・`LimitBreakResponse.cs`、`Produce/ProduceStateResponse.cs` | `IApiResponse<AppCommon>` を実装し `Common` を足す |
| 作成 | `server/Uta.Server/Infrastructure/PlayerDiffCollector.cs` | コレクター |
| 変更 | `server/Uta.Server/Infrastructure/PlayerRepository.cs`、`IdolRepository.cs`、`SupportCardRepository.cs`（実在のファイル名は実装担当が `Infrastructure/` で確認） | 書き込みメソッドの末尾で `Touch…` を呼ぶ |
| 変更 | `server/Uta.Server/Program.cs` | `AddVoltechServer(...)` の戻り値に `.AddCommonResponse<AppCommon, PlayerDiffCollector>()`。`PlayerDiffCollector` の DI 登録は基盤が行う（`AddCommonResponse` の内部。足りなければ `AddScoped` を足す） |
| 作成 | `server/Uta.Server.Tests/Integration/CommonResponseTests.cs` | 結合テスト |
| 作成 | `client/uta/Assets/Scripts/Application/Player/PlayerDiffData.cs` | Application の差分の型 |
| 変更 | `client/uta/Assets/Scripts/Application/Player/PlayerCache.cs` | `Apply(PlayerDiffData)` |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/Player/PlayerCommonApplier.cs` | `ICommonResponseApplier<AppCommon>`（DTO → `PlayerDiffData` → `PlayerCache.Apply`） |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/UtaApiClient.cs`、`Bootstrap/BootstrapEntry.cs`（`UtaApiClient.Create` を呼ぶ側） | `Create` に `ICommonResponseApplier<AppCommon> commonApplier` を足し `.WithCommonResponseApplier(commonApplier)` |
| 作成 | `client/uta/Assets/Tests/EditMode/PlayerCacheApplyTests.cs`、`PlayerCommonApplierTests.cs` | 単体テスト |
| 変更 | `uta_docs/wiki/architecture/api-design.md`、`server/server-foundation.md`、`server/gacha-support-server.md`、`server/produce-server.md` | 共通レスポンス項目の節を足し、各 RPC が `Common` を返すことを書く |

## 3. 型とシグネチャ

`shared/` は C# 9・`Nullable` 無効・`TreatWarningsAsErrors`（`.editorconfig` に従う）。

```csharp
// Common/AppCommon.cs
[MessagePackObject]
public sealed class AppCommon
{
    [SerializationConstructor]
    public AppCommon(PlayerDiff playerDiff) { PlayerDiff = playerDiff; }

    /// <summary>null なら変化なし。</summary>
    [Key(0)] public PlayerDiff PlayerDiff { get; }
}

// Common/PlayerDiff.cs
[MessagePackObject]
public sealed class PlayerDiff
{
    [SerializationConstructor]
    public PlayerDiff(bool hasBalance, int level, int exp, int gold, int supportPoints, int gachaCurrency,
        OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards, string[] removedSupportCardUids) { ... }

    /// <summary>false なら通貨とレベル（次の 5 つ）は変化なし。</summary>
    [Key(0)] public bool HasBalance { get; }
    [Key(1)] public int Level { get; }
    [Key(2)] public int Exp { get; }
    [Key(3)] public int Gold { get; }
    [Key(4)] public int SupportPoints { get; }
    [Key(5)] public int GachaCurrency { get; }
    /// <summary>触ったアイドル（変更後の値）。無ければ空配列。</summary>
    [Key(6)] public OwnedIdolDto[] Idols { get; }
    /// <summary>触ったサポートカード（変更後の値）。無ければ空配列。</summary>
    [Key(7)] public OwnedSupportCardDto[] SupportCards { get; }
    /// <summary>無くなったサポートカードの uid。無ければ空配列。</summary>
    [Key(8)] public string[] RemovedSupportCardUids { get; }
}
```

`OwnedIdolDto`・`OwnedSupportCardDto` は `Player/` の既存の型を使う。

各レスポンスに足すもの（キー番号は、そのクラスの今の最大の `Key` の次。既存の番号は変えない）:

```csharp
public sealed class DrawResponse : IApiResponse<AppCommon>
{
    // コンストラクターの引数は増やさない（Common は基盤が入れる）
    [Key(3)] public AppCommon Common { get; set; }
}
```

`ProduceStateResponse` も同じ（キーは今の最大の次）。`using VolTech.Server.Abstractions;`。

```csharp
// server/Uta.Server/Infrastructure/PlayerDiffCollector.cs（C# 14 可）
public sealed class PlayerDiffCollector : ICommonResponseCollector<AppCommon>
{
    public PlayerDiffCollector(IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards) { ... }

    public void TouchBalance(Ulid playerId);
    public void TouchIdol(Ulid playerId, int idolId);
    public void TouchSupportCard(Ulid playerId, Ulid uid);
    public void RemoveSupportCard(Ulid playerId, Ulid uid);   // upsert の記録からも外す

    public async ValueTask<AppCommon?> BuildAsync(TxContext tx);
}
```

- 内部は `HashSet`。何も記録されていなければ `null` を返す（`Common` は null）
- `BuildAsync`: 記録された `playerId`（1 つのトランザクションで 1 人。違う ID が来たら `InvalidOperationException`）について、`TouchBalance` があれば `players.FindAsync(tx, playerId)` で通貨とレベルを読んで `HasBalance = true`。`TouchIdol` の ID は `idols.ListAsync(tx, playerId)` から該当だけ抜く。`TouchSupportCard` の uid は `supportCards.FindAsync(tx, playerId, uid)`（見つからなければ飛ばす）。`RemoveSupportCard` の uid は文字列（`Ulid.ToString()`）で `RemovedSupportCardUids` に入れる
- DTO への写しは、`SignInUseCase` が `OwnedIdolDto`・`OwnedSupportCardDto` を作る処理があればそれを再利用する（実装担当が `Application/Player/` と `Api/PlayerService.cs` を読んで、重複するなら共通の静的メソッドに切り出す。新しい設計ではない）

記録を呼ぶ場所（リポジトリの実装の、書き込みが成功した後）:

| 書き込み | 呼ぶ |
|---|---|
| `GrantCurrenciesAsync`・`TrySpendGachaCurrencyAsync`（成功したとき）・`AddSupportPointsAsync`・`TrySpendSupportAndGoldAsync`（成功）・`GrantProduceRewardAsync` | `TouchBalance(playerId)` |
| `IdolRepository.TryInsertAsync`（挿入したとき）・`AddShardsAsync` | `TouchIdol(playerId, idolId)` |
| `SupportCardRepository.InsertAsync`・`UpdateLevelAsync`（uid だけを受け取るので、実装は更新後に `playerId` が取れないなら `TryLimitBreakAsync` と同様に SQL の `RETURNING player_id` で取る。取れない場合は止まって報告） | `TouchSupportCard(playerId, uid)` |
| `SupportCardRepository.TryDeleteMaterialAsync`（削除したとき） | `RemoveSupportCard(playerId, uid)` |
| `SupportCardRepository.TryLimitBreakAsync`（成功） | `TouchSupportCard(playerId, uid)` |

```csharp
// client Application: PlayerDiffData.cs（C# 9）
public sealed class PlayerDiffData
{
    public PlayerDiffData(bool hasBalance, int level, int exp, int gold, int supportPoints, int gachaCurrency,
        IReadOnlyList<OwnedIdol> idols, IReadOnlyList<OwnedSupportCard> supportCards, IReadOnlyList<string> removedSupportCardUids) { ... }
    // 同名のプロパティ
}

// PlayerCache.cs
public void Apply(PlayerDiffData diff);   // Current が null なら何もしない
```

`Apply`: `Current` から新しい `PlayerSnapshot` を作って `Set` する。`HasBalance` が真なら通貨とレベルを置き換え、偽なら据え置き。`HasActiveProduce`・`DiscardedProduce` は据え置き。アイドルは `IdolId` が同じものを置き換え（無ければ末尾に足す）、サポートカードは `SupportCardUid` が同じものを置き換え（無ければ末尾）、`removedSupportCardUids` のものを除く。同じ差分を 2 回当てても結果が変わらない。

```csharp
// PlayerCommonApplier.cs（Infrastructure/Player）
public sealed class PlayerCommonApplier : ICommonResponseApplier<AppCommon>
{
    public PlayerCommonApplier(PlayerCache cache) { ... }
    public UniTask ApplyAsync(AppCommon common);   // common.PlayerDiff が null なら何もしない。DTO の null 配列は空として扱う（PlayerGateway.ToSnapshot と同じ）
}
```

`BootstrapEntry` の DI（実装担当は今の `UtaApiClient.Create` の呼び出し元を読む）で、`PlayerCache` を作ってから `new PlayerCommonApplier(cache)` を `UtaApiClient.Create` の新しい引数に渡す。

## 4. タスク

**タスク 1（shared＋server）: DTO・コレクター・登録**

- 手順: 2・3 章の shared と server の行をすべて実装する
- 境界: 何も記録されない RPC（`GetGachasAsync` など）は `Common` が null。`BuildAsync` が `null` を返したとき `Common` は null のまま。失敗（`UseCaseException`）ではロールバックされ記録も捨てられる（基盤の仕様）
- 終わりの確かめ: `dotnet build Uta.slnx` が警告 0・エラー 0。既存のサーバーのテスト 293 件が通る。タスク 2 のテストが通る

**タスク 2（server のテスト）: 結合テスト**

- 追加: `CommonResponseTests`（`Integration/Support/PlayerClient.cs` など既存の支援を使う。実装担当は `GachaTests.cs` の書き方に合わせる）
  - `Draw_ReturnsCommon_WithBalanceAndGrantedItems`: ガチャを引くと `Common.PlayerDiff.HasBalance` が真で `GachaCurrency` が引いた後の値（`DrawResponse.GachaCurrency` と同じ）、`Idols` に出たアイドル（または `SupportCards` に出たカード）が入る
  - `LevelUp_ReturnsCommon_WithSupportCard`: `SupportCards` にレベルが上がったカード、`HasBalance` が真で、`Gold`・`SupportPoints` が減った後の値
  - `LimitBreak_ReturnsCommon_WithRemovedMaterial`: 素材の uid が `RemovedSupportCardUids` に、対象が `SupportCards` に入る
  - `ProduceComplete_ReturnsCommon_WithReward`: プロデュースを最後まで進めた最後の操作の `Common.PlayerDiff` に、報酬の通貨とレベル。途中の操作は `Common` が null（`ProduceDriver` で最後まで進められる既存のテストに合わせる）
  - `Draw_Retry_ReturnsSameCommon`: 同じ `x-request-id` の再送で `Common` が同じ（基盤が保存済みレスポンスを返す）
  - `Draw_Failed_NoCommon`: 通貨不足の失敗はエラーで、DB も変わらない（既存の `GachaTests` に同種があれば重複しない）
- 終わりの確かめ: `dotnet test --project server/Uta.Server.Tests`（Docker が要る）

**タスク 3（client。Unity を開く worktree）: キャッシュと反映処理**

- 手順: `PlayerDiffData`・`PlayerCache.Apply`・`PlayerCommonApplier`・`UtaApiClient.Create` の引数・`BootstrapEntry` の配線。移行計画の「`WithErrorNavigator(null)` の確認」と同じ箇所なので、続けて直す
- 終わりの確かめ: `unity recompile --format json` がエラー 0。EditMode のテストが通る
- テスト（EditMode）:
  - `PlayerCache_Apply_ReplacesBalance_WhenHasBalance` / `_KeepsBalance_WhenNoBalance`
  - `PlayerCache_Apply_UpsertsIdolAndSupportCard`（同じ ID は置き換え、無ければ追加）
  - `PlayerCache_Apply_RemovesSupportCard`
  - `PlayerCache_Apply_IsIdempotent`（同じ差分を 2 回）
  - `PlayerCache_Apply_DoesNothing_WhenNotSignedIn`（`Current` が null）
  - `PlayerCommonApplier_ApplyAsync_ConvertsDtoAndApplies`（null の配列を空として扱う、`PlayerDiff` が null なら何もしない）

**タスク 4（docs のみ）: wiki**

- 手順: 2 章の wiki の行を直す。`server-foundation.md` には「Uta は `AppCommon` に player_diff だけを持つ」「`Common` を載せるレスポンスの一覧（0 章の 4）」「記録は `PlayerDiffCollector`。リポジトリの書き込みの後に `Touch…`」「`BuildAsync` が読み直す」を書く。`api-design.md` の各 RPC の表には `Common` の有無の列を足す（実装担当は表の形を読んで合わせる）
- 順 7 の画面計画への影響は、移行計画のタスク 4 と同じ変更に含める（画面は結果を読んだ時点でキャッシュが更新済み、と書き足す。表の項目は消さない）

**タスク 5（Unity。Windows の IL2CPP）**: 移行計画のタスク 5 と一緒に、`SignInAsync` → `SignUpAsync` 後のサインインで `Common` が無い（`SignInResponse` は `IApiResponse` を実装しない）ことと、MessagePack の `AppCommon` の IL2CPP でのシリアライズ（`UtaMagicOnionClientInitializer` と MessagePack の生成物。`Uta.Shared.Contracts` は `[MessagePackObject]` の型を `MessagePackCodeGen` で生成している既存の仕組みに従う。実装担当は既存の生成の手順を `shared/` と `client/` で確認し、`AppCommon`・`PlayerDiff` が生成の対象に入ることを確かめる）を確認する。ガチャ以降の RPC は画面の計画で通るので、ここでは `Common` を返す RPC をまだ呼ばず、DTO のラウンドトリップは EditMode のテストで確かめる（`MessagePackSerializer` で `AppCommon` を往復して等しい）

## 5. 未決事項

- 0 章の 1〜6 の確認（ユーザー）
- `SupportCardRepository.UpdateLevelAsync` が `playerId` を取れるか（3 章の表の注記。取れなければ止まって報告）
