---
title: 実装計画: クライアントを共通基盤の新しい呼び方（11d9b06）に移す
status: draft
updated: 2026-11-08
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/architecture/screen-design.md
---

# 実装計画: クライアントを共通基盤の新しい呼び方（11d9b06）に移す

## 0. ユーザーの確認が要ること

1. **共通レスポンス項目（player_diff・バッジ・通知）を今は採り入れない**（推奨）。サーバーは `Common` を返さず、クライアントも `WithCommonResponseApplier` を付けない。画面の計画（順 7）はサインイン／各 RPC のレスポンスをそのまま読む形で組まれており、採り入れると DTO とサーバーの全 RPC に手が入る。採り入れるなら別の計画にする。
2. **画面の計画（順 7）7 本が書いている `XxxApi : ApiServiceBase<…>` と `result.IsSuccess` を、この計画のタスク 4 で新しい呼び方に書き直す**（推奨）。書き直さないと、実装担当が存在しない型を書くことになる。
3. **この計画は Unity で確かめる**。この worktree（`Uta-wt/2`）では Unity を開いておらず、パッケージの解決（`packages-lock.json`）もコンパイルもできない。タスク 1〜3 は Unity を開く worktree で実装する。

## 1. 目的と前提

PR #34 で共通基盤（VolTech-MagicOnion）の固定を `11d9b06` に上げた。基盤は互換性を壊す変更を入れた（基盤の `CHANGELOG.md`「互換性が壊れる変更」）ので、今の Uta のクライアント（`client/uta/Assets/Scripts/Infrastructure/Api/` ほか）は Unity でコンパイルできない（`ApiServiceBase<T>` が無い、`ApiResult<T>.IsSuccess` が無い、`ApiCore.CreateBuilder` の引数が違う）。サーバーは `dotnet build Uta.slnx` が警告 0・エラー 0 で、影響は無い（Uta は `[SkipAuth]`・`[NoSideEffects]` を共有インターフェースのメソッドにだけ付けている）。

この計画は、クライアントを基盤の新しい呼び方に移し、通る状態に戻す。あわせて Monitor（Editor の通信ウィンドウ）が使えることを確かめる。

- 基盤の使い方の正: VolTech-MagicOnion の `docs/guide/client.md`
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0009](../wiki/decisions/0009-client-stack.md)、[0019](../wiki/decisions/0019-foundation-packages.md)
- **この計画で決めないこと**: 共通レスポンス項目の採用（0 章の 1）、`IApiActivityHandler`（通信中のブロッカー）、`IApiLogger`、Tracing、実機での Monitor の表示（基盤にまだ無い）、画面の実装そのもの（順 7 の各計画）

## 2. 新旧の対応

| 今（旧） | 新 |
|---|---|
| `ApiServiceBase<IPlayerService>` のラッパー `PlayerApi` | 無くす。`core.CreateClient<IPlayerService>()` で作った共有インターフェースを直接呼ぶ |
| `InvokeAsync(nameof(...), req, (c, r) => c.XxxAsync(r), handledCodes)` | `await client.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` が無いときは `WithHandledCodes` を付けない） |
| `result.IsSuccess` | `!result.IsError` |
| `result.Error.Code.Equals(X)` | `result.ErrorCode == X`（`ErrorCode?` と `ErrorCode` の `==`。基盤の `ApiResult<T>.ErrorCode` は `ErrorCode?`） |
| `result.Error.ToString()` | `result.ErrorMessage`（ログ用） |
| `ApiCore.CreateBuilder(options, tokenProvider, policy, handler)` | `ApiCore.CreateBuilder(callInvoker, serializerProvider, clientFactoryProvider, options, tokenProvider, policy, handler)` |

`ToResult()` は `using VolTech.Client;`（`ApiResultExtensions`）、`WithHandledCodes` は同じ名前空間の `ApiClientExtensions`。`IsError` が真のとき `Value` を読むと例外になるので、必ず先に `IsError` を見る。

## 3. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 削除 | `client/uta/Assets/Scripts/Infrastructure/Api/PlayerApi.cs`（と `.meta`） | ラッパーをやめる |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/ServiceClientFactory.cs` | `Create<T>` は `IDeviceService` 用に残し、シリアライザーの組み立てを `SerializerProvider` プロパティに切り出す |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/UtaApiClient.cs` | `Player` を `IPlayerService` にする。`CreateBuilder` の新しい引数 |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Player/PlayerGateway.cs` | 新しい呼び方と `ApiResult` の読み方 |
| 変更 | `client/uta/Assets/Tests/EditMode/PlayerGatewayTests.cs` | `PlayerApi` の偽物を、`IPlayerService` の偽物に替える（基盤の `FakeRpcErrors.ApiFailureResult<T>` で失敗を返す） |
| 変更 | `client/uta/Packages/packages-lock.json` | `unity command package resolve` の結果（手で書かない） |
| 変更 | `uta_docs/wiki/architecture/api-design.md`（58 行目）、`unity7-evaluation.md`（31・61・63 行目） | `ApiServiceBase`・`PlayerApi` の記述を新しい呼び方に直す |
| 変更 | `uta_docs/plans/2026-11-03-client-screens-formation.md`・`-gacha.md`・`-owned.md`・`2026-11-07-client-prefabs-common.md` | `XxxApi : ApiServiceBase<…>` と `IsSuccess` の記述（タスク 4 の表） |

`2026-11-03-client-signup-signin.md`（done）は履歴なので直さない。`UtaMagicOnionClientInitializer.cs`・`CountingTokenProvider.cs`・`UtaErrors.cs` は変えない。

## 4. 型とシグネチャ

```csharp
// ServiceClientFactory.cs（internal static）
public static IMagicOnionSerializerProvider SerializerProvider { get; }   // 今の Create<T> の中身の options / serializerProvider を static readonly に持つ
public static IMagicOnionClientFactoryProvider ClientFactoryProvider => UtaMagicOnionClientInitializer.ClientFactoryProvider;
public static TService Create<TService>(CallInvoker callInvoker) where TService : IService<TService>;   // IDeviceService 用。中で SerializerProvider を使う

// UtaApiClient.cs
public IPlayerService Player { get; }      // core.CreateClient<IPlayerService>()
// Create(...) の中:
var core = ApiCore.CreateBuilder(
        callInvoker, ServiceClientFactory.SerializerProvider, ServiceClientFactory.ClientFactoryProvider,
        options, counting, new ErrorPolicyProvider(UtaErrors.Create()), errorHandler)
    .WithErrorNavigator(errorNavigator)
    .Build();
var playerClient = core.CreateClient<IPlayerService>();
```

`WithErrorNavigator(null)` は診断のときに渡される（今のコードと同じ）。基盤が null を許さないなら、`errorNavigator != null` のときだけ `WithErrorNavigator` を付ける（実装担当は `ApiCoreBuilder.cs` の `WithErrorNavigator` を読んで、null を受けるか確かめる。受けなければこの分岐を入れる。設計判断ではなく基盤の仕様の確認）。

```csharp
// PlayerGateway.cs
public async UniTask<PlayerSignUpResult> SignUpAsync()
{
    var result = await player.SignUpAsync().ToResult();
    return result.IsError ? PlayerSignUpResult.Failure(result.ErrorMessage) : PlayerSignUpResult.Success(result.Value.Created);
}

public async UniTask<PlayerSignInResult> SignInAsync()
{
    var result = await player.WithHandledCodes(PlayerErrors.NotCreated).SignInAsync().ToResult();
    if (!result.IsError) { return PlayerSignInResult.Success(ToSnapshot(result.Value)); }
    if (result.ErrorCode == PlayerErrors.NotCreated) { return PlayerSignInResult.NotCreated(); }
    return PlayerSignInResult.Failure(result.ErrorMessage);
}
```

`PlayerGateway` のコンストラクターは `UtaApiClient api` を受けて `player = api.Player;`（型だけ `IPlayerService` に変わる）。`notCreated` フィールドは消す。

## 5. タスク

**タスク 1（Unity worktree。Unity を開く）: パッケージの解決**

- 手順: `client/uta` で `unity command package resolve` を実行し、`packages-lock.json` の `com.chinpangx.voltech.magiconion` が `11d9b06` になったことを確かめて commit する
- 終わりの確かめ: `unity recompile --format json` が、この時点では `ApiServiceBase` などのコンパイルエラーを返す（タスク 2・3 で直す対象の確認）

**タスク 2: Api 層を移す**（`ServiceClientFactory.cs`・`UtaApiClient.cs`・`PlayerApi.cs` の削除）

- 手順: 4 章のとおり。`PlayerApi.cs` と `.meta` を削除する。`UtaApiClient` の `Player` を `IPlayerService` にする
- 境界: `UtaApiClient.Dispose` は変えない（チャンネルを閉じる）
- 終わりの確かめ: このタスクだけではまだ `PlayerGateway` が壊れているので、タスク 3 と続けてコンパイルを確かめる

**タスク 3: ゲートウェイとテストを移す**（`PlayerGateway.cs`・`PlayerGatewayTests.cs`）

- 手順: 4 章のとおり。テストは `ToSnapshot` の静的メソッドを呼ぶ形のテストが残るなら変えない。`PlayerApi` を使っているテストがあれば、`IPlayerService` を実装した偽物（失敗は `FakeRpcErrors.ApiFailureResult<T>` で返す。偽物のメソッドから同期で例外を投げない）に替え、`ApiCore` を通さないので `PlayerGateway` の `ErrorCode` の分岐（`NotCreated` → `NotCreated()`、それ以外の失敗 → `Failure`）を確かめるテストを足す
- 終わりの確かめ: `unity recompile --format json` がエラー 0。EditMode のテストが通る（Unity の Test Runner。コマンドは `unity-cli` スキルに従う）

**タスク 4（docs のみ。Unity 不要）: 画面の計画とwiki を直す**

| ファイル | 直すもの |
|---|---|
| `client-screens-formation.md` | 77 行目の `ProduceApi.cs` を計画から外す。「`ApiCore` を通して呼ぶ」ゲートウェイの記述に（`XxxApi` の代わりに `api.Produce`＝`IProduceService`）。278〜289 行目のクラス定義は、`UtaApiClient.Produce`（`IProduceService`）に替える。`WithHandledCodes`・`ToResult`・`IsError`・`ErrorCode`・`ErrorMessage` で書く（311 行目の結果の直し方も） |
| `client-screens-gacha.md` | 62・230〜236 行目の `GachaApi` を同様に（`UtaApiClient.Gacha`） |
| `client-screens-owned.md` | `SupportApi` を同様に（`UtaApiClient.Support`） |
| `client-prefabs-common.md` | `ApiServiceBase`・`IsSuccess` の記述（2 か所）を新しい呼び方に |
| `api-design.md` 58 行目、`unity7-evaluation.md` 31・61・63 行目 | 「`ApiServiceBase`（`PlayerApi`）経由」を「`ApiCore.CreateClient<IPlayerService>()` を直接呼ぶ」に。IL2CPP で通った記録（31 行目）は、`ApiServiceBase` の属性のリフレクションが `RpcSettings`／`ApiCallFilter` に変わったので、**新しい呼び方では未確認**と書き足す（タスク 5 で確かめる） |

各 `UtaApiClient` に足すメンバー（`Produce`・`Gacha`・`Support`）は、それぞれの画面の計画が `UtaApiClient.Create` に `core.CreateClient<I…Service>()` を 1 行足す形で書く。`[MagicOnionClientGeneration]` に `IProduceService`・`IGachaService`・`ISupportService` を足し忘れると `NotSupportedException` になるが、これらは `Uta.Shared.Contracts` の同じアセンブリなので、`IPlayerService` が入っていれば足りる（基盤の guide「書いた型のアセンブリ全体が生成の対象になる」）。

**タスク 5（Unity。Windows の IL2CPP）: 実機相当の確認**

- 手順: CLAUDE.md の「ビルド・テスト・実行」にある Windows の IL2CPP のビルドと `Uta.exe -batchmode -nographics -uta-quit-after-check` を、ローカルのサーバーに対して実行する
- 確かめること: `device: registered` → `sign-up: created` → `sign-in: ok`、2 回目は `sign-up: skipped` → `sign-in: ok`、`players` の行を消した 3 回目は `PLAYER_NOT_CREATED` を `WithHandledCodes` で受けて復帰する（[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.1 と同じ）。結果は `unity7-evaluation.md` の表に足す
- Monitor: Editor の Play で起動し、メニュー **Window > VolTech MagicOnion > Monitor** に `RegisterAsync`・`SignUpAsync`・`SignInAsync` が行になることを確かめる（コード変更は要らない。基盤の `ApiChannel.CreateCallInvoker` を通っているため）

## 6. テスト

- `PlayerGatewayTests`（EditMode）: `SignIn_ReturnsSuccess_WhenOk`・`SignIn_ReturnsNotCreated_WhenPlayerNotCreated`・`SignIn_ReturnsFailure_WhenOtherError`・`SignUp_ReturnsFailure_WhenError`（偽物の `IPlayerService` で。`ApiCore` を通さないので、`ToResult()` の `ApiException` から `ApiResult` への変換は基盤側のテストに任せる）
- 既存の `EnterPlayerUseCaseTests`・`UtaErrorsTests`・`MasterDataLoaderTests`・`MasterCardCatalogTests` は変えず、通ること
- サーバー: `dotnet build Uta.slnx`（警告 0・エラー 0）。サーバーのテスト `dotnet test --project server/Uta.Server.Tests` は、固定を上げた PR #34 の後に通ることを確かめる（この計画では変えない）

## 7. 未決事項

- 0 章の 1〜3 の確認（ユーザー）
- 基盤の `ApiCoreBuilder.WithErrorNavigator` が null を受けるか（タスク 2 で実装担当が基盤のソースを読んで分岐を入れる。設計判断ではない）
