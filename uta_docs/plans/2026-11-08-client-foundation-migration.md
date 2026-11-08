---
title: 実装計画: クライアントを共通基盤の新しい呼び方（11d9b06）に移す
status: ready
updated: 2026-11-08
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/architecture/screen-design.md
---

# 実装計画: クライアントを共通基盤の新しい呼び方（11d9b06）に移す

## 0. 決まっていること

ユーザーが推奨どおりに了承した（2026-11-08）。

1. **共通レスポンス項目（player_diff）は採る**が、範囲が広いので後続の [2026-11-08-common-response-player-diff.md](2026-11-08-common-response-player-diff.md) で行う。この計画では `.WithCommonResponseApplier` を付けない。
2. **順 7 の画面計画のうち、旧い呼び方を書いている 4 本（foundation・formation・gacha・owned）は、各計画の先頭に「基盤 11d9b06 への読み替え」の節を足して直す**（本文は書き換えない。節が本文と食い違ったら節が正）。
3. **実装は Unity を開く worktree で行う**。この計画を書いた worktree（`Uta-wt/2`）では Unity を開いておらず、`packages-lock.json` の解決もコンパイルもできなかった。
4. **順番**: この計画 → 共通レスポンスの計画 の順。順 7 の `main` への取り込みは、この計画を **P1 より前**に `main` へ入れる（P1 の積み PR の前提を変えないため）。索引 [2026-11-07-client-screens-index.md](2026-11-07-client-screens-index.md) の 1章に「順 0」として足す（タスク 4）。

## 1. 目的と前提

PR #34 で共通基盤（VolTech-MagicOnion）の固定を `11d9b06` に上げた。基盤は互換性を壊す変更を入れた（基盤の `CHANGELOG.md`「互換性が壊れる変更」）ので、今の Uta のクライアントは Unity でコンパイルできない（`ApiServiceBase<T>` が無い、`ApiResult<T>.IsSuccess` が無い、`ApiCore.CreateBuilder` の引数が違う）。サーバーは `dotnet build Uta.slnx` が警告 0・エラー 0、`dotnet test --project server/Uta.Server.Tests` が 293 件成功で、影響は無い（Uta は `[SkipAuth]`・`[NoSideEffects]` を共有インターフェースのメソッドにだけ付けている）。

この計画は、クライアントを基盤の新しい呼び方に移して通る状態に戻し、Monitor（Editor の通信ウィンドウ）が使えることを確かめる。

- 基盤の使い方の正: VolTech-MagicOnion の `docs/guide/client.md`。署名は `Unity/com.chinpangx.voltech.magiconion/Runtime/Client/`（`ApiCore.cs`・`ApiClientExtensions.cs`・`ApiResult.cs`）
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0009](../wiki/decisions/0009-client-stack.md)、[0019](../wiki/decisions/0019-foundation-packages.md)
- **この計画で決めないこと**: 共通レスポンス項目（後続の計画）、`IApiActivityHandler`（foundation の計画）、`IApiLogger`、Tracing、実機での Monitor の表示（基盤にまだ無い）、画面の実装そのもの

## 2. 新旧の対応

| 今（旧） | 新 |
|---|---|
| `ApiServiceBase<IPlayerService>` のラッパー `PlayerApi` | 無くす。`core.CreateClient<IPlayerService>()` で作った共有インターフェースを直接呼ぶ |
| `InvokeAsync(nameof(...), req, (c, r) => c.XxxAsync(r), handledCodes)` | `await client.WithHandledCodes(codes).XxxAsync(req).ToResult()`（コードを渡さないときは `WithHandledCodes` を付けない） |
| `result.IsSuccess` | `!result.IsError` |
| `result.Error.Code.Equals(X)` | `result.ErrorCode == X`（`ErrorCode?` と `ErrorCode` の `==`） |
| `result.Error.ToString()`（失敗の文字列） | **そのまま `result.Error.ToString()`**（`ApiResult<T>.Error` は public のまま。コードとステータスが文字列に入る。`ErrorMessage` はメッセージだけなので使わない） |
| `ApiCore.CreateBuilder(options, tokenProvider, policy, handler)` | `ApiCore.CreateBuilder(callInvoker, serializerProvider, clientFactoryProvider, options, tokenProvider, policy, handler)` |

`ToResult()` は `ApiResultExtensions`、`WithHandledCodes` は `ApiClientExtensions`（どちらも `using VolTech.Client;`）。`IsError` が真のとき `Value` を読むと例外になるので、必ず先に `IsError` を見る。`WithErrorNavigator(null)` は基盤が受ける（`ApiCoreBuilder.cs`）ので、分岐は要らない。

## 3. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 削除 | `client/uta/Assets/Scripts/Infrastructure/Api/PlayerApi.cs`（と `.meta`） | ラッパーをやめる |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/ServiceClientFactory.cs` | `Create<T>` は `IDeviceService` 用に残す（`DeviceTokenProvider` は `ApiCore` の前に要るため）。シリアライザーの組み立てを `SerializerProvider`・`ClientFactoryProvider` に切り出す |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/UtaApiClient.cs` | `Player` を `IPlayerService` にする。`CreateBuilder` の新しい引数 |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Player/PlayerGateway.cs` | コンストラクターを `IPlayerService` にし、新しい呼び方で読む |
| 変更 | `client/uta/Assets/Scripts/Bootstrap/BootstrapEntry.cs` | `using Uta.Shared.Contracts.Player;` を足し、`IPlayerService` を VContainer に登録する（81 行目付近） |
| 変更 | `client/uta/Assets/Scripts/Bootstrap/Uta.Client.Bootstrap.asmdef` | `references` に `"Uta.Shared.Contracts"` を足す（`Uta.Shared.Domain` の次） |
| 変更 | `client/uta/Assets/Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | `precompiledReferences` に `MagicOnion.Abstractions.dll`・`MagicOnion.Client.dll`・`Grpc.Core.Api.dll` を足す |
| 変更 | `client/uta/Assets/Tests/EditMode/PlayerGatewayTests.cs` | 偽の `IPlayerService` で 4 本を足す（既存の `ToSnapshot` の 2 本は残す） |
| 変更 | `client/uta/Packages/packages-lock.json` | `unity command package resolve` の結果（手で書かない） |
| 変更 | `uta_docs/wiki/architecture/api-design.md`（58 行目）、`unity7-evaluation.md`（31・61・63 行目） | `ApiServiceBase`・`PlayerApi` の記述を新しい呼び方に |
| 変更 | `uta_docs/plans/2026-11-03-client-screens-foundation.md`・`-formation.md`・`-gacha.md`・`-owned.md` | 先頭に「基盤 11d9b06 への読み替え」の節（タスク 4 の文面） |
| 変更 | `uta_docs/plans/2026-11-07-client-screens-index.md` | 1章の表に「順 0」の行を足す |

`2026-11-03-client-signup-signin.md`（done）は履歴なので直さない。`UtaMagicOnionClientInitializer.cs`・`CountingTokenProvider.cs`・`UtaErrors.cs` は変えない。`client-prefabs-common.md`・`week`・`lesson` に旧い呼び方は無い（Grep 済み）。

## 4. 型とシグネチャ

```csharp
// ServiceClientFactory.cs（internal static）
public static IMagicOnionSerializerProvider SerializerProvider { get; }   // 今の Create<T> の中の options / serializerProvider を static readonly に持つ
public static IMagicOnionClientFactoryProvider ClientFactoryProvider => UtaMagicOnionClientInitializer.ClientFactoryProvider;
public static TService Create<TService>(CallInvoker callInvoker) where TService : IService<TService>;   // 中で SerializerProvider を使う。IDeviceService 用

// UtaApiClient.cs
public IPlayerService Player { get; }      // core.CreateClient<IPlayerService>()
// Create(...) の引数は今のまま。中の組み立て:
var core = ApiCore.CreateBuilder(
        callInvoker, ServiceClientFactory.SerializerProvider, ServiceClientFactory.ClientFactoryProvider,
        options, counting, new ErrorPolicyProvider(UtaErrors.Create()), errorHandler)
    .WithErrorNavigator(errorNavigator)
    .Build();
var playerClient = core.CreateClient<IPlayerService>();
```

```csharp
// PlayerGateway.cs
private readonly IPlayerService player;
public PlayerGateway(IPlayerService player) { this.player = player; }   // UtaApiClient を受けない

public async UniTask<PlayerSignUpResult> SignUpAsync()
{
    var result = await player.SignUpAsync().ToResult();
    return result.IsError ? PlayerSignUpResult.Failure(result.Error.ToString()) : PlayerSignUpResult.Success(result.Value.Created);
}

public async UniTask<PlayerSignInResult> SignInAsync()
{
    var result = await player.WithHandledCodes(PlayerErrors.NotCreated).SignInAsync().ToResult();
    if (!result.IsError) { return PlayerSignInResult.Success(ToSnapshot(result.Value)); }
    if (result.ErrorCode == PlayerErrors.NotCreated) { return PlayerSignInResult.NotCreated(); }
    return PlayerSignInResult.Failure(result.Error.ToString());
}
```

`notCreated` フィールドは消す。`ToSnapshot` は今のまま。

```csharp
// BootstrapEntry.cs: UtaApiClient の登録の次の行に足す
builder.Register<IPlayerService>(resolver => resolver.Resolve<UtaApiClient>().Player, Lifetime.Singleton);
// PlayerGateway の登録（既存の builder.Register<PlayerGateway>(...).As<IPlayerPort>()）は、コンストラクターの引数が IPlayerService になるだけで、そのまま動く
```

## 5. タスク

**タスク 1（Unity worktree）: パッケージの解決**

- 手順: 実装する worktree の `client/uta` で `unity command package resolve` を実行し、`packages-lock.json` の `com.chinpangx.voltech.magiconion` が `11d9b06` になったことを確かめて commit する
- 終わりの確かめ: `unity recompile --format json` が `ApiServiceBase`・`IsSuccess`・`CreateBuilder` のコンパイルエラーを返す（タスク 2 の直す対象の確認）

**タスク 2: Api 層・ゲートウェイ・テストを移す**（`ServiceClientFactory.cs`・`UtaApiClient.cs`・`PlayerApi.cs` の削除・`PlayerGateway.cs`・`BootstrapEntry.cs`・テストの asmdef・`PlayerGatewayTests.cs`）

- 手順: 4 章のとおり。壊れたまま終わらないよう、1 つのタスクでコンパイルが通るところまで直す
- 偽の `IPlayerService`（テスト側の private クラス）: `IPlayerService` の 2 メソッド（`SignUpAsync`・`SignInAsync`）に加え、`IService<IPlayerService>` の 5 メンバー（`WithOptions`・`WithHeaders`・`WithDeadline`・`WithCancellationToken`・`WithHost`）を実装する。**`WithHeaders` は `this` を返す**（`WithHandledCodes` の実体が `client.WithHeaders(...)` を呼ぶため。戻りを新しい偽物にすると、登録した返り値が引き継がれない）。ほかの `With…` も `this` を返し、`WithHost` だけ `NotSupportedException`。`SignUpAsync`・`SignInAsync` は登録した `Func<UniTask<…>>` を `new UnaryResult<T>(task.AsTask())` で包んで返す。見本は基盤の `Tests/EditMode/DeviceTokenProviderTests.cs` の `FakeDeviceService`（353 行目付近）。失敗は基盤の `FakeRpcErrors.ApiFailureResult<T>` で作った `UnaryResult<T>` を返す（偽物のメソッドから同期で例外を投げない）
- 境界: `UtaApiClient.Dispose` は変えない。asmdef の DLL 名がリストアされたパッケージと違ってコンパイルできなければ、止まって報告する
- 終わりの確かめ: `unity recompile --format json` がエラー 0。EditMode のテスト（6 章）が通る

**タスク 3（docs のみ）: wiki を直す**

- `api-design.md` 58 行目: 「`PlayerApi`（`ApiServiceBase<IPlayerService>`）が `ApiCore` 経由で呼び」を「`UtaApiClient.Player`（`ApiCore.CreateClient<IPlayerService>()` が作る共有インターフェース）を `PlayerGateway` が `WithHandledCodes(...).SignInAsync().ToResult()` の形で呼び」に
- `unity7-evaluation.md` 31 行目: 表の記録に「（2026-11-08 に呼び方を `ApiCore.CreateClient` へ移したため、**この新しい呼び方では未確認**。タスク 5 で確かめる）」と足す。61 行目の取り消し線の項目はそのまま残し、直後に「→ 新しい呼び方（`RpcSettings`／`ApiCallFilter`）での確認はタスク 5」と足す。63 行目の「`ApiResult` の失敗を `Failure` に直す経路」は、偽の `IPlayerService` の EditMode のテストで確かめられるようになった旨に直す

**タスク 4（docs のみ）: 画面の計画に「読み替え」の節を足す**

foundation・formation・gacha・owned の 4 本の、タイトル（`# 実装計画: …`）の直後に、次の節をそのまま足す。

```
## 基盤 11d9b06 への読み替え（2026-11-08。[クライアントの移行計画](2026-11-08-client-foundation-migration.md)。この節が本文と食い違ったら、この節が正）

- `XxxApi`（`ApiServiceBase<…>` のラッパー。`GachaApi`・`SupportApi`・`ProduceApi`）は**作らない**。`UtaApiClient` が `public IGachaService Gacha { get; }`（`ISupportService Support`・`IProduceService Produce`）を持ち、`Create` の中で `core.CreateClient<I…Service>()` で作る。本文の「`ServiceClientFactory.Create<I…Service>(callInvoker)` で作って `new XxxApi(core, client)`」は「`core.CreateClient<I…Service>()`」に読み替える。private コンストラクターの引数の型は `XxxApi` ではなく `I…Service`
- ゲートウェイのコンストラクターは `I…Service` を受ける（`UtaApiClient` を受けない）。`BootstrapEntry` に `builder.Register<I…Service>(resolver => resolver.Resolve<UtaApiClient>().Xxx, Lifetime.Singleton);` を足す（`PlayerGateway` と同じ）
- 呼び方: `xxx.XxxAsync(req, handledCodes)` → `await xxx.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` の無い RPC は `WithHandledCodes` を付けない）。`result.IsSuccess` → `!result.IsError`。`result.Error.Code.Equals(X)` → `result.ErrorCode == X`。`result.Error.ToString()`・`result.Error.Code.Reason` は今のまま使える
- 本文のファイル表の `Api/XxxApi.cs`（作成）の行は無効（作らない）。テストの偽物は、`PlayerGatewayTests` と同じく偽の `I…Service`（`IService<T>` の 5 メンバーを実装し、`WithHeaders` は `this` を返す）
- `[MagicOnionClientGeneration]` の型の並びに `typeof(I…Service)` を足す指示は今のまま有効（`Uta.Shared.Contracts` の同じアセンブリなので、足し忘れても `IPlayerService` で足りるが、本文どおり足す）
```

加えて各計画に、次の 1 行を足す。
- foundation: 「`UtaApiClient.Create` の `IApiActivityHandler`（3.8）は、移行計画の `Create`（引数は今のまま）に足す。`ApiCoreBuilder.AddActivityHandler` を使う」
- formation: 「314 行目の `UtaApiClient` の変更は `ProduceApi` ではなく `IProduceService Produce`（上の節）」

索引: 1章の表の先頭に行を足す: `| 0 | [クライアントの基盤移行](2026-11-08-client-foundation-migration.md) | 共通基盤 11d9b06 の新しい呼び方への移行（`PlayerApi` の廃止ほか） | なし | `main` | ready |` と、「順 0 は P1 より前に main へ入れる」の 1 行。続けて共通レスポンスの計画は「MVP の後の計画」の箇条書きに足す（順 4 の前でも後でもよい。foundation の `UtaApiClient.Create` を変えるので、**順 4 の前**に main へ入れる）

**タスク 5（Unity。Windows の IL2CPP）: 実機相当の確認**

- 準備: 基盤の固定で `devices`・`last_responses`・`game_logs` の `player_id` が `uuid` から `char(26)` に変わり、既存のマイグレーションが書き換わっている。**手元のローカル DB は作り直す**（`docker compose --env-file server/.env -f server/compose.yaml down -v` → `up -d --wait` → `dotnet run --project server/Uta.Server -- migrate`）
- 手順: CLAUDE.md の「ビルド・テスト・実行」の Windows の IL2CPP のビルドで `Uta.exe` を作り、ローカルのサーバー（`docker compose --env-file server/.env -f server/compose.yaml up -d --wait` → `dotnet run --project server/Uta.Server -- migrate` → `dotnet run --project server/Uta.Server`）に対して、次の順に `Uta.exe -batchmode -nographics -uta-quit-after-check` を実行する。アカウントのファイルは `%USERPROFILE%\AppData\LocalLow\<会社名>\<製品名>\` の `ServerEndpoint.AccountFileName(接続先 URL)` が返す名前（実装担当は `Uta.exe` のログか `ServerEndpoint.cs` で名前を確かめる）
  1. ①アカウント無し: 上のアカウントのファイルを消して実行 → `device: registered` → `sign-up: created` → `sign-in: ok`
  2. ②そのままもう一度実行 → `sign-up: skipped` → `sign-in: ok`
  3. ③`players` の行を消して実行: `docker compose --env-file server/.env -f server/compose.yaml exec postgres psql -U postgres -d uta -c "TRUNCATE players CASCADE"` の後で実行 → `SignInAsync` が `PLAYER_NOT_CREATED` を `WithHandledCodes` で受け、`SignUpAsync` → `SignInAsync` で復帰（ローカルの確認用 DB なので `CASCADE` で所持品も消えてよい。外部キーの関係で失敗したら、そのエラーを添えて止まって報告）
  4. ④トークンの無効化: ①〜③のどの実行でも、`ConnectivityCheck` が `reauth retry: ok (invalidations=1)` をログに出す（既存の診断。出なければ止まって報告）
  結果は `unity7-evaluation.md` の 31 行目を書き換えて記録する
- Monitor: Editor の Play で起動し、**Window > VolTech MagicOnion > Monitor** に `RegisterAsync`・`SignUpAsync`・`SignInAsync` が行になることを確かめる（コード変更は要らない。`ApiChannel.CreateCallInvoker` を通っているため）

## 6. テスト

`PlayerGatewayTests`（EditMode。偽の `IPlayerService`）に足す 4 本:
- `SignIn_ReturnsSuccess_WhenOk`
- `SignIn_ReturnsNotCreated_WhenPlayerNotCreated`（`FakeRpcErrors.ApiFailureResult<SignInResponse>` で `PlayerErrors.NotCreated` を返す）
- `SignIn_ReturnsFailure_WhenOtherError`
- `SignUp_ReturnsFailure_WhenError`

偽物は `ApiCore` を通らないので、`WithHandledCodes` が `handledCodes` に載せたコードを `ApiCore` が尊重するところ（共通ハンドラーに回さないこと）は、このテストでは確かめない。基盤のテストと、タスク 5 の ③ で確かめる。既存の `EnterPlayerUseCaseTests`・`UtaErrorsTests`・`MasterDataLoaderTests`・`MasterCardCatalogTests` は変えず、通ること。サーバー: `dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project server/Uta.Server.Tests` は変えない（この計画で触らない）。

## 7. 未決事項

なし（0 章の了承を反映した）。
