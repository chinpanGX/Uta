---
title: 実装計画: 共通レスポンス項目（player_diff）をサーバーとクライアントに入れる
status: ready
updated: 2026-11-08
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/architecture/server/gacha-support-server.md, ../wiki/architecture/server/produce-server.md
---

# 実装計画: 共通レスポンス項目（player_diff）

## 0. 決まっていること（ユーザーが推奨どおりに了承した。2026-11-08）

ユーザーが「共通レスポンスも採る」と決めた（2026-11-08）。基盤の仕様は VolTech-MagicOnion の `docs/spec/common-response.md`・`docs/guide/server.md`「共通レスポンス項目」。次は、その上での Uta の決め方。

1. **`AppCommon` は player_diff だけを持つ**。バッジと通知は MVP に仕様が無い（`screen-wireframes.md`「作らない」: ミッション・お知らせほか）ので持たない。後で `Key(1)`・`Key(2)` に足せる。
2. **player_diff の中身は「変更後の値」**: 通貨とレベルの全部（レベル・経験値・ゴールド・サポートポイント・ガチャ通貨）、触ったアイドル、触ったサポートカード、無くなったサポートカードの uid。変化量は持たない（基盤の決めごと）。
3. **記録のしかたは「触ったキーだけを記録し、`BuildAsync` で DB から読み直す」**。リポジトリの書き込みメソッドの引数と戻り値は変えない（書き込みの中で `tx.GetCollector<PlayerDiffCollector>().Touch…()` を 1 行呼ぶだけ）。
4. **`Common` を載せるレスポンス**: `DrawResponse`・`ExchangeResponse`・`LevelUpResponse`・`LimitBreakResponse`・`ProduceStateResponse`（報酬が付くのは操作の結果。`Result` を持つ型）。載せない: `SignInResponse`（全件を返すので呼び出し側が置き換える）、`SignUpResponse`、`GetGachasResponse`、`ProduceResultResponse`（`ProduceStateResponse.Result` に入る入れ子の型で、`AbandonAsync` が単独で返すときも所持品は変わらないため）。
5. **既にあるレスポンスの項目（`DrawResponse.GachaCurrency` など）は消さない**。重複するが、順 7 の画面計画がそれらを読む形で組まれているため、消すのは画面が出来てから別に決める。
6. **クライアントの反映先は `PlayerCache`**（`Apply(PlayerDiff)` を足す）。`SignIn` で置き換えたスナップショットに、差分をマージする。

7. **コレクターは `Infrastructure/PlayerDiffCollector.cs` に置き、Infrastructure から Contracts の DTO を参照するのをこの 1 ファイルだけ例外として認める**（ユーザーの判断 2026-11-08）。基盤の `TxContext.GetCollector<T>` が具象型の完全一致を要求し、リポジトリ（Infrastructure）が記録する必要があるため。DTO への写し（2 行）は `Api/PlayerService.cs` と重複させる。`repository-layout.md` 3章にこの例外を書く（タスク 4）。基盤は変えない。
8. **`ISupportCardRepository.UpdateLevelAsync` に `Ulid playerId` を足す**（`UpdateLevelAsync(TxContext tx, Ulid playerId, Ulid uid, int level)`。SQL の `WHERE` に `AND player_id = @playerId`）。呼び出し元の `LevelUpUseCase` は `playerId` を持っている。ほかのメソッドと形がそろい、所有者の検査も兼ねる。
9. **サインインで取り直す `RefreshPlayerUseCase` は今は残す**（ユーザーの判断 2026-11-08）。`Common` は先に `PlayerCache` を更新し、`RefreshPlayerUseCase` は安全網として残す。画面の計画は直さない。冗長な呼び出しの削除は、画面が動いてから別に決める。

**順番**: [2026-11-08-client-foundation-migration.md](2026-11-08-client-foundation-migration.md)（基盤の新しい呼び方への移行）の**後**に実装し、**foundation（順 4）より前**に `main` へ入れる（foundation も `UtaApiClient.Create` を変えるため。`Create` に足す引数は、それぞれの計画が自分の変更の前の形の末尾に足す）。

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
| 変更 | `server/Uta.Server/Infrastructure/PlayerRepository.cs`、`IdolRepository.cs`、`SupportCardRepository.cs` | 書き込みメソッドの末尾で `Touch…` を呼ぶ |
| 変更 | `server/Uta.Server/Application/Support/ISupportCardRepository.cs`、`LevelUpUseCase.cs`（33 行目） | `UpdateLevelAsync` に `Ulid playerId` を足す（0 章の 8） |
| 変更 | `server/Uta.Server/Program.cs` | `AddVoltechServer(...)` の戻り値に `.AddCommonResponse<AppCommon, PlayerDiffCollector>()`。`PlayerDiffCollector` は DI に登録しない（基盤がトランザクションごとにリクエストの ServiceProvider から作る。コンストラクターの引数はスコープのリポジトリが解決される） |
| 作成 | `server/Uta.Server.Tests/Integration/CommonResponseTests.cs` | 結合テスト |
| 作成 | `client/uta/Assets/Scripts/Application/Player/PlayerDiffData.cs` | Application の差分の型 |
| 変更 | `client/uta/Assets/Scripts/Application/Player/PlayerCache.cs` | `Apply(PlayerDiffData)` |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/Player/PlayerCommonApplier.cs` | `ICommonResponseApplier<AppCommon>`（DTO → `PlayerDiffData` → `PlayerCache.Apply`） |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/UtaApiClient.cs`、`Bootstrap/BootstrapEntry.cs`（`UtaApiClient.Create` を呼ぶ側） | `Create` に `ICommonResponseApplier<AppCommon> commonApplier` を足し `.WithCommonResponseApplier(commonApplier)` |
| 作成 | `client/uta/Assets/Tests/EditMode/PlayerCacheApplyTests.cs`、`PlayerCommonApplierTests.cs`、`AppCommonSerializationTests.cs` | 単体テスト。後者は `AppCommon` の MessagePack の往復（`MessagePackSerializerOptions.Standard`＋`StandardResolver`。Editor の動的な解決でよい） |
| 変更 | `uta_docs/wiki/architecture/api-design.md`、`server/server-foundation.md`、`server/gacha-support-server.md`、`server/produce-server.md`、`repository-layout.md`（3章の Infrastructure の行）、`screen-design.md`（7.5） | 共通レスポンス項目の節を足し、各 RPC が `Common` を返すことを書く。`repository-layout.md` には 0 章の 7 の例外を書く |

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
- DTO への写しは `PlayerDiffCollector` の中に書く（`new OwnedIdolDto(idol.IdolId, idol.Training, idol.Awakening, idol.Shards)`、`new OwnedSupportCardDto(card.Uid.ToString(), card.CardId, card.Level, card.LimitBreak)`。`Api/PlayerService.cs` の SignIn の写しと同じ式。重複は 0 章の 7 で受け入れた）
- 記録の取りこぼしは不具合なので、読み直しで見つからなければ握りつぶさず `InvalidOperationException`（`TouchIdol`／`TouchSupportCard` の対象が DB に無い）。基盤は `BuildAsync` の例外を `INTERNAL` にしてロールバックする

記録を呼ぶ場所（リポジトリの実装の、書き込みが成功した後）:

| 書き込み | 呼ぶ |
|---|---|
| `GrantCurrenciesAsync`・`TrySpendGachaCurrencyAsync`（成功したとき）・`AddSupportPointsAsync`・`TrySpendSupportAndGoldAsync`（成功）・`GrantProduceRewardAsync` | `TouchBalance(playerId)` |
| `IdolRepository.TryInsertAsync`（挿入したとき）・`AddShardsAsync` | `TouchIdol(playerId, idolId)` |
| `SupportCardRepository.InsertAsync`・`UpdateLevelAsync`（0 章の 8 で `playerId` が引数になる） | `TouchSupportCard(playerId, uid)` |
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
public void Apply(PlayerDiffData diff);   // Current が null なら InvalidOperationException
```

`Apply`: `Current` が null なら `InvalidOperationException`（サインイン前に `Common` が届くのは不具合。基盤が `CLIENT_ERROR` にする）。`Current` から新しい `PlayerSnapshot` を作って `Set` する。`HasBalance` が真なら通貨とレベルを置き換え、偽なら据え置き。`HasActiveProduce`・`DiscardedProduce` は据え置き。アイドルは `IdolId` が同じものを置き換え（無ければ末尾に足す）、サポートカードは `SupportCardUid` が同じものを置き換え（無ければ末尾）、`removedSupportCardUids` のものを除く。同じ差分を 2 回当てても結果が変わらない。

```csharp
// PlayerCommonApplier.cs（Infrastructure/Player）
public sealed class PlayerCommonApplier : ICommonResponseApplier<AppCommon>
{
    public PlayerCommonApplier(PlayerCache cache) { ... }
    public UniTask ApplyAsync(AppCommon common);   // common.PlayerDiff が null なら何もしない。DTO の null 配列は空として扱う（PlayerGateway.ToSnapshot と同じ）
}
```

`UtaApiClient.Create` の**最後の引数**に `ICommonResponseApplier<AppCommon> commonApplier` を足し、`Build()` の前で `if (commonApplier != null) { builder = builder.WithCommonResponseApplier(commonApplier); }`（診断で null を渡せるように）。`BootstrapEntry`（70〜78 行目）の `UtaApiClient.Create(...)` の呼び出しの最後に `resolver.Resolve<PlayerCommonApplier>()` を渡し、`builder.Register<PlayerCommonApplier>(Lifetime.Singleton);` を足す（`PlayerCache` は 82 行目で登録済み）。`PlayerCommonApplier.cs` の `using` は `VolTech.Client;`（`ICommonResponseApplier<>`）と `Uta.Shared.Contracts.Common`。

## 4. タスク

**タスク 1（shared＋server）: DTO・コレクター・登録**

- 手順: 2・3 章の shared と server の行をすべて実装する
- 境界: 何も記録されない RPC（`GetGachasAsync` など）は `Common` が null。`BuildAsync` が `null` を返したとき `Common` は null のまま。失敗（`UseCaseException`）ではロールバックされ記録も捨てられる（基盤の仕様）。`SignUpUseCase` の書き込みもコレクターに記録されるが、`SignUpResponse` は `IApiResponse<>` を実装しないので捨てられる（正しい動き）。`PlayerDiffCollector` は DI に登録しない（2 章の Program.cs の行）。`GetCollector` が失敗したら止まって報告
- 終わりの確かめ: `dotnet build Uta.slnx` が警告 0・エラー 0。既存のサーバーのテスト 293 件が通る。タスク 2 のテストが通る

**タスク 2（server のテスト）: 結合テスト**

- 追加: `CommonResponseTests`（`[Collection(PostgresCollection.Name)]`。`StartAsync`・`DrawAsync`・`ExchangeAsync`・`GiveExchangePointsAsync` は `GachaTests` の private ヘルパーなので、同じ形で `CommonResponseTests` に写す。乱数は `ScriptedRandomSource`、値は `GachaTests` の `Normal(r)`／`Guaranteed(r)`）。期待値は、レスポンス本体の項目（`DrawResponse.GachaCurrency` など）と DB の値に**合わせる**（固定の数を新しく決めない）
  - `Draw_Single_NewCard_ReturnsCommon`: `GachaTests.Draw_Single_PaysAndGrantsNewCard`（54 行目）と同じ設定で引く。`Common.PlayerDiff.HasBalance` が真、`GachaCurrency == response.GachaCurrency`、出た物が `SupportCards` か `Idols`（そのテストが出す種類の側）に入り、もう一方は空
  - `Draw_DuplicateIdol_ReturnsIdolWithShards`: `Draw_DuplicateIdol_ConvertsToShards`（132 行目）と同じ設定。`Idols` に該当のアイドルが入り `Shards` が DB と一致
  - `Exchange_ReturnsCommon`: `Exchange_EnoughPoints_GrantsAndSpends`（292 行目）と同じ設定。`Common` が null でなく、付与された物が `Idols` か `SupportCards` に入る
  - `LevelUp_ReturnsCommon`: `GiveExchangePointsAsync` と同じ要領で SQL により `players.support_points`・`gold` を十分に与え、`IPlayerService.SignInAsync` で初期付与のサポートカードの uid を取って `ISupportService.LevelUpAsync`。`SupportCards` にそのカード（`Level == response.Level`）、`HasBalance` が真で `SupportPoints == response.SupportPoints`・`Gold == response.Gold`
  - `LimitBreak_ReturnsCommon_WithRemovedMaterial`: 同じ `card_id` のカード 2 枚を SQL で与えて `LimitBreakAsync`（与え方は `Draw_MaxedSupportCard_ConvertsToSupportPoints`（148 行目）の SQL に合わせる）。素材の uid が `RemovedSupportCardUids` に、対象が `SupportCards`（`LimitBreak == response.LimitBreak`）に入る
  - `ProduceComplete_ReturnsCommon_WithReward`: `ProduceDriver.PlayToEndAsync` で最後まで進める。完了した操作のレスポンス（`Result` が非 null のもの）の `Common.PlayerDiff` に報酬後の通貨とレベル（DB の値と一致）。途中のレスポンスは `Common` が null
  - `Draw_SameRequestId_ReturnsSameCommon`: 同じ `x-request-id` の再送（`Draw_SameRequestId_ReturnsStoredResponse` と同じ）で、2 回目の `Common` の中身が 1 回目と等しい
  - 失敗（通貨不足）で `Common` が付かないことは基盤のテストで確かめられているので足さない
- 終わりの確かめ: `dotnet test --project server/Uta.Server.Tests`（Docker が要る）

**タスク 3（client。Unity を開く worktree。移行計画のタスク 2 の後）: キャッシュと反映処理**

- 手順: `PlayerDiffData`・`PlayerCache.Apply`・`PlayerCommonApplier`・`UtaApiClient.Create` の引数・`BootstrapEntry` の配線。3 章のとおり
- 終わりの確かめ: `unity recompile --format json` がエラー 0。EditMode のテストが通る
- テスト（EditMode）:
  - `PlayerCache_Apply_ReplacesBalance_WhenHasBalance` / `_KeepsBalance_WhenNoBalance`
  - `PlayerCache_Apply_UpsertsIdolAndSupportCard`（同じ ID は置き換え、無ければ追加）
  - `PlayerCache_Apply_RemovesSupportCard`
  - `PlayerCache_Apply_IsIdempotent`（同じ差分を 2 回）
  - `PlayerCache_Apply_Throws_WhenNotSignedIn`（`Current` が null なら `InvalidOperationException`）
  - `PlayerCommonApplier_ApplyAsync_ConvertsDtoAndApplies`（null の配列を空として扱う、`PlayerDiff` が null なら何もしない）
  - `AppCommon_RoundTripsThroughMessagePack`（`PlayerDiff` の全項目が往復で等しい）

**タスク 4（docs のみ）: wiki**

- 手順: 2 章の wiki の行を直す。`server-foundation.md` には「Uta は `AppCommon` に player_diff だけを持つ」「`Common` を載せるレスポンスの一覧（0 章の 4）」「記録は `PlayerDiffCollector`。リポジトリの書き込みの後に `Touch…`」「`BuildAsync` が読み直す」を書く。`api-design.md` の各 RPC の表には `Common` の有無の列を足す（実装担当は表の形を読んで合わせる）
- 画面の計画（順 7）は直さない（0 章の 9。`RefreshPlayerUseCase` は残す）。`screen-design.md` 7.5 の「所持データのキャッシュ」の行に、「`Common` の player_diff が先に反映される。`RefreshPlayerUseCase` は安全網」と 1 文だけ足す

**タスク 5: 確認できないことの記録**

- `AppCommon`／`PlayerDiff` の IL2CPP でのシリアライズは、`Common` を返す RPC（ガチャほか）を Windows の IL2CPP で呼ぶ画面の計画（gacha）まで確かめられない。`unity7-evaluation.md` の 3.3 に「`AppCommon` の IL2CPP でのシリアライズは未確認（`Uta.Shared.Contracts` は `[MagicOnionClientGeneration]` の対象のアセンブリに入っているので生成の対象になるはず）。gacha の計画の IL2CPP の確認で確かめる」と書く
- EditMode の往復テスト（タスク 3）は Editor の動的な解決で通るだけで、IL2CPP の保証にならない

## 5. 未決事項

なし（0 章の 1〜9 はユーザーが了承した。2026-11-08）。
