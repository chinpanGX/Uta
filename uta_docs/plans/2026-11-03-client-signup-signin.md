---
title: 実装計画: クライアントのサインアップ・サインイン（Uta の RPC の呼び出し）
status: ready
updated: 2026-11-03
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/screen-design.md, ../wiki/architecture/repository-layout.md, ../wiki/architecture/server/server-foundation.md
---

# 実装計画: クライアントのサインアップ・サインイン

## 1. 目的と前提

クライアントから `IPlayerService.SignUpAsync`・`SignInAsync` を呼び、受け取った所持データをキャッシュする。あわせて、認証が要る最初の RPC なので `ApiCore`（順番待ち・再認証・エラーハンドラー）をクライアントに組み立てる。流れの正は [api-design.md](../wiki/architecture/api-design.md) 1章と [screen-design.md](../wiki/architecture/screen-design.md) 4章。サーバー側は [5a の計画](2026-11-01-player-signup-signin.md)（done）で済んでいる。

- 位置づけ: [game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の 5a が「1b の後の計画」に回したクライアント側。1b は [2026-11-01-connectivity-check-client.md](2026-11-01-connectivity-check-client.md)（done）。
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0009](../wiki/decisions/0009-client-stack.md)
- **ユーザーが決めたこと（2026-11-03）**:
  1. `ApiCore` をこの計画で入れる。`IPlayerService` は `ApiServiceBase<IPlayerService>` のラッパー（`PlayerApi`）で呼ぶ。属性のリフレクション（`RpcSettings`）と再送の IL2CPP は Windows の IL2CPP のプレイヤーで確かめる（6章）。通らなければ実装担当は止まって報告する
  2. 共通エラーハンドラーは `DefaultCommonErrorHandler` を使う。`IErrorDialogView` は IMGUI の診断用の実装を Bootstrap に置く（画面の計画で作り直す）。`IErrorNavigator` は渡さない
  3. 起動の呼び出し順は「初回は SignUp から」: 端末を登録した直後は `SignUpAsync` → `SignInAsync`。保存ありは `SignInAsync` だけで、`PLAYER_NOT_CREATED` が返ったら `SignUpAsync` → `SignInAsync`（screen-design.md 4章の文面どおり）
- **この計画で決めないこと**（範囲外）:
  - 画面（順 7）。結果は診断用の IMGUI（`StartupView`）に出すだけ
  - `DATE_CHANGED` を受けた後のサインインのやり直しと、タイトルへの遷移（`IErrorNavigator`。画面の計画）
  - ホームの「再開」・プロデュース破棄のダイアログ（`HasActiveProduce`・`DiscardedProduce` は読んでキャッシュするだけ）
  - 共通レスポンス項目（player_diff など）の反映、`IApiActivityHandler`（通信中のブロッカー）、`IApiLogger`
  - ガチャ・サポートカードのエラーコードの `ErrorDefinition` への登録（それぞれのクライアントの計画で足す）
  - Android・iOS の実機での確認

## 2. この計画で決めたこと

| 項目 | 決めたこと | 理由 |
|---|---|---|
| Application の ポート | `IPlayerPort`（`SignUpAsync`・`SignInAsync`）。通信の DTO は Application に出さず、`PlayerSnapshot` などの Application の型で返す | Application は Contracts を参照しない（repository-layout.md 4章）。DTO ↔ 型の変換は Infrastructure に閉じる（ADR 0008） |
| 通信の失敗の表し方 | `ApiCore` は失敗でも例外を投げず、エラーハンドラー（ダイアログ・Retry）を済ませてから失敗の結果を返す。`PlayerGateway` はそれを `PlayerSignInOutcome.Failed`（`PLAYER_NOT_CREATED` だけ `PlayerNotCreated`）に直し、ユースケースが `InvalidOperationException` にして `StartupPresenter` の既存の catch に渡す | 再試行の判断はエラーハンドラー側に済んでいる。ユースケースは「次へ進めるか」だけ見る |
| `PLAYER_NOT_CREATED` | `SignInAsync` の呼び出しで `handledCodes` に渡す。`ErrorDefinition` にも登録する（既定の action は `Title`。サインイン以外の RPC が受けたときは共通ハンドラーがタイトルへ戻す。起動の流れがサインアップから入り直す） | server-foundation.md「クライアントは `SignInAsync` を呼ぶとき `handledCodes` に渡す」。**既定の action が `Title` なのは Uta案（推測）**。api-design.md に決めた記述が無く、`ErrorPolicyProvider` の未登録の既定と同じにした。実装後に api-design.md 2章へ [Uta案] として書く（ユーザーに報告する） |
| `ErrorDialogView` の置き場 | Bootstrap。Presentation の参照先（repository-layout.md 3章の表）に `VolTech.MagicOnion.Client` が無く、`IErrorDialogView` を実装するには要るため。画面の計画で、Presentation に移すなら表を直す | 診断用で、画面の計画が作り直す。置き場の理由を設計書に残す（タスク 4） |
| 「サインイン」の呼び名 | 既存の `SignInUseCase`・`IDeviceAuthPort`（端末の登録とトークンの発行）は名前を変えない。用語集の「サインイン」（`SignInAsync`）は `EnterPlayerUseCase`・`IPlayerPort`。診断の最初の行は `startup: start` にする。unity7-evaluation.md の「サインアップ・サインイン」（1b の端末の登録とトークンの発行）は、タスク 4 で「端末の登録・トークンの発行」に直す | 名前の変更は 1b の計画・テスト・ドキュメントに広がる。混同は説明で防ぐ |
| 結果型（`PlayerSignInResult` など）を残す理由 | 画面の計画で、失敗に再試行の導線を付けるときに「失敗」と「未作成」を値で区別できるようにするため。例外にするのは、ユースケースの外（診断のプレゼンター）だけ | 批評の指摘（3 型を消せる）を、画面の計画の見通しで退けた |
| 再認証の確認で `DeviceTokenProvider` をもう 1 つ作る理由 | `IssueTokenAsync` を `DeviceClient` で直接呼ぶと、`DeviceTokenProvider` が付けるアプリのバージョンのヘッダーなどを再現することになる。`DeviceTokenProvider` を使い回すほうが、確かめたい経路（`ApiCore` の再送）以外の差が出ない | 批評の指摘（直接 1 回呼べば足りる）を退けた |
| 引数なしの RPC の `request` | `InvokeAsync` の `request` に `int` の `0` を渡す（`call` では使わない） | `ApiServiceBase.InvokeAsync<TRequest, TResponse>` が `TRequest` を要るため。基盤のテストも同じ |
| キャッシュ | `PlayerCache`（Application。`Current` を持つ単純なクラス）を DI のシングルトンにする。`SignInAsync` が成功するたびに `Set` で入れ替える | api-design.md「クライアントは受け取った所持データをキャッシュする」。画面の計画が `PlayerCache` を読む |
| 診断の `-uta-quit-after-check` | 診断用のダイアログは、このフラグのときボタンを押さずに即座に「再試行以外」のボタンを返す（`RetryOrTitle` → `Title`、ほかは唯一のボタン）。ログに出す | バッチモードでは誰もボタンを押せず、ダイアログで永久に待つため |
| 再認証の再送の確認 | 診断に 1 手順足す: 別の `DeviceTokenProvider` で発行し直して、メインの持つトークンをサーバーで無効にし、そのあと `SignInAsync` を呼ぶ。`ApiCore` が `UNAUTHENTICATED` を受けて取り直し、同じ要求を再送して成功すれば `reauth retry: ok` | 基盤の client.md が「未確認」と書いている `ApiCore` の再送の IL2CPP を、実機相当（Windows の IL2CPP）で確かめる |
| テストの置き場 | Application のユースケースのテストは、既存の `Uta.Client.Infrastructure.Tests.EditMode` に置く（asmdef に `Uta.Client.Application` を足す） | テスト用の asmdef を増やさない。名前と中身が合わないのは受け入れる |
| `DeviceAuthGateway` の `IDisposable` | やめる。チャンネルの破棄は `UtaApiClient`（`IDisposable`）が持つ | チャンネルと `ApiCore` を `DeviceAuthGateway` と `PlayerGateway` で共有するため |

## 3. 触るファイル

`A` = `client/uta/Assets/Scripts/Application`、`I` = `client/uta/Assets/Scripts/Infrastructure`、`P` = `client/uta/Assets/Scripts/Presentation`、`B` = `client/uta/Assets/Scripts/Bootstrap`、`T` = `client/uta/Assets/Tests/EditMode`。新しい `.cs` / `.asmdef` / フォルダごとに `.meta` が要る（Unity が作る。タスク末尾で確かめる）。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `A/Player/OwnedIdol.cs`・`OwnedSupportCard.cs`・`PlayerSnapshot.cs`・`PlayerCache.cs`・`PlayerSignInOutcome.cs`・`PlayerSignInResult.cs`・`PlayerSignUpResult.cs`・`IPlayerPort.cs`・`EnterPlayerResult.cs`・`EnterPlayerUseCase.cs` | 4.1 | 1 |
| 変更 | `T/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | `references` に `Uta.Client.Application`・`Uta.Shared.Contracts`・`VolTech.Server.Abstractions`・`VolTech.MagicOnion.Client` を足す | 1 |
| 作成 | `T/EnterPlayerUseCaseTests.cs` | 5章 | 1 |
| 削除 | `I/Api/DeviceServiceClientFactory.cs` とその `.meta` | `ServiceClientFactory` に置き換える | 2 |
| 作成 | `I/Api/ServiceClientFactory.cs` | 4.2 | 2 |
| 変更 | `I/Api/UtaMagicOnionClientInitializer.cs` | `IPlayerService` を生成の対象に足す | 2 |
| 作成 | `I/Api/UtaErrors.cs`・`PlayerApi.cs`・`UtaApiClient.cs` | 4.2 | 2 |
| 作成 | `I/Player/PlayerGateway.cs` | 4.3 | 2 |
| 作成 | `T/UtaErrorsTests.cs`・`T/PlayerGatewayTests.cs` | 5章 | 2 |
| 変更 | `A/Auth/IDeviceAuthPort.cs`・`A/Auth/SignInUseCase.cs` | 4.4 | 3 |
| 変更 | `I/Auth/DeviceAuthGateway.cs` | 4.4 | 3 |
| 作成 | `B/ErrorDialogView.cs` | 4.5 | 3 |
| 変更 | `B/UtaBootstrap.cs`・`B/Uta.Client.Bootstrap.asmdef` | 4.5 | 3 |
| 変更 | `P/Startup/StartupPresenter.cs`・`P/Startup/StartupView.cs`（コメントのみ） | 4.6 | 3 |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`api-design.md`・`repository-layout.md`・`unity7-evaluation.md` | 実装で決めたこと・確かめたこと（メインのセッション） | 4 |

## 4. 型とシグネチャ

C# 9 の範囲（`client/` の `.editorconfig`。ブロック形式の namespace、`var`、Allman、プライマリコンストラクタ・`record`・`init` を使わない）。BOM 付き UTF-8、LF。static の可変なフィールドを作らない（Unity 7 の UAL0010）。

### 4.1 Application（タスク 1。名前空間 `Uta.Client.Application.Player`。参照してよいのは UniTask と Domain だけ）

```csharp
public sealed class OwnedIdol
{
    public OwnedIdol(int idolId, int training, int awakening, int shards) { /* 各プロパティに代入 */ }
    public int IdolId { get; }
    public int Training { get; }
    public int Awakening { get; }
    public int Shards { get; }
}

public sealed class OwnedSupportCard
{
    public OwnedSupportCard(string supportCardUid, int cardId, int level, int limitBreak) { /* 代入 */ }
    public string SupportCardUid { get; }   // ULID 26 文字
    public int CardId { get; }
    public int Level { get; }
    public int LimitBreak { get; }
}

public sealed class PlayerSnapshot
{
    public PlayerSnapshot(int level, int exp, int gold, int supportPoints, int gachaCurrency,
        bool hasActiveProduce, bool discardedProduce,
        IReadOnlyList<OwnedIdol> idols, IReadOnlyList<OwnedSupportCard> supportCards) { /* 代入。idols・supportCards が null なら ArgumentNullException */ }
    public int Level { get; }  public int Exp { get; }  public int Gold { get; }
    public int SupportPoints { get; }  public int GachaCurrency { get; }
    public bool HasActiveProduce { get; }  public bool DiscardedProduce { get; }
    public IReadOnlyList<OwnedIdol> Idols { get; }
    public IReadOnlyList<OwnedSupportCard> SupportCards { get; }
}

public sealed class PlayerCache
{
    public PlayerSnapshot Current { get; private set; }   // サインインするまで null
    public void Set(PlayerSnapshot snapshot) { /* null なら ArgumentNullException。Current に代入 */ }
}

public enum PlayerSignInOutcome { Succeeded = 0, PlayerNotCreated = 1, Failed = 2 }

public sealed class PlayerSignInResult
{
    public static PlayerSignInResult Success(PlayerSnapshot snapshot)       // Outcome=Succeeded, Snapshot=snapshot, ErrorText=null
    public static PlayerSignInResult NotCreated()                           // Outcome=PlayerNotCreated, Snapshot=null, ErrorText=null
    public static PlayerSignInResult Failure(string errorText)              // Outcome=Failed, Snapshot=null, ErrorText=errorText
    public PlayerSignInOutcome Outcome { get; }
    public PlayerSnapshot Snapshot { get; }
    public string ErrorText { get; }
}

public sealed class PlayerSignUpResult
{
    public static PlayerSignUpResult Success(bool created)                  // Succeeded=true, Created=created, ErrorText=null
    public static PlayerSignUpResult Failure(string errorText)              // Succeeded=false, Created=false, ErrorText=errorText
    public bool Succeeded { get; }
    public bool Created { get; }        // 今回作ったか（2 回目以降は false）
    public string ErrorText { get; }
}

public interface IPlayerPort
{
    /// <summary>プレイヤーを作る（2 回目以降は何もしない）。通信の失敗は例外にせず Failure で返す。</summary>
    UniTask<PlayerSignUpResult> SignUpAsync();

    /// <summary>所持データを全件取る。プレイヤー未作成は NotCreated、そのほかの失敗は Failure で返す。</summary>
    UniTask<PlayerSignInResult> SignInAsync();
}

public sealed class EnterPlayerResult
{
    public EnterPlayerResult(bool signedUp, bool created, PlayerSnapshot snapshot) { /* 代入 */ }
    public bool SignedUp { get; }       // SignUpAsync を呼んだか
    public bool Created { get; }        // SignUpAsync が「今回作った」と返したか（呼ばなかったら false）
    public PlayerSnapshot Snapshot { get; }
}

public sealed class EnterPlayerUseCase
{
    public EnterPlayerUseCase(IPlayerPort port, PlayerCache cache) { /* 代入 */ }

    /// <summary>
    /// afterRegistration が true（端末を登録した直後）なら、先に SignUp を呼ぶ。そのあと SignIn を呼ぶ。
    /// SignIn が PlayerNotCreated を返したら、まだ SignUp を呼んでいなければ SignUp してから SignIn をやり直す（1 回だけ）。
    /// SignUp 済みなのに PlayerNotCreated が返ったら InvalidOperationException。
    /// 成功したら cache.Set(snapshot)。
    /// </summary>
    public UniTask<EnterPlayerResult> ExecuteAsync(bool afterRegistration);
}
```

例外のメッセージ（`InvalidOperationException`。`StartupPresenter` が画面に出す）:

| 場面 | メッセージ |
|---|---|
| SignUp が失敗 | `"sign-up failed: " + result.ErrorText` |
| SignIn が失敗（Failed） | `"sign-in failed: " + result.ErrorText` |
| SignUp 済みなのに PlayerNotCreated | `"sign-in: player not created after sign-up"` |

### 4.2 Infrastructure の通信の組み立て（タスク 2。名前空間 `Uta.Client.Infrastructure.Api`）

```csharp
// ServiceClientFactory.cs（DeviceServiceClientFactory を置き換える。internal static）
public static TService Create<TService>(CallInvoker callInvoker) where TService : IService<TService>
{
    /* 今の DeviceServiceClientFactory.Create の本体と同じ。MagicOnionClient.Create<TService>(...) にする */
}

// UtaMagicOnionClientInitializer.cs: [MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService))]
//   （属性が複数の型を受け取れずコンパイルエラーなら、属性を 2 行に分ける。using Uta.Shared.Contracts.Player; を足す）

// UtaErrors.cs（public static class）
public static ErrorDefinition[] Create()   // 新しい配列を返す（static の可変な配列を持たない）
{
    return new[] { new ErrorDefinition(PlayerErrors.NotCreated, ErrorAction.Title) };
}

// PlayerApi.cs（public sealed class。using VolTech.Client; using VolTech.Server.Abstractions; using Uta.Shared.Contracts.Player;）
public sealed class PlayerApi : ApiServiceBase<IPlayerService>
{
    public PlayerApi(ApiCore core, IPlayerService client) : base(core, client) { }

    public UniTask<ApiResult<SignUpResponse>> SignUpAsync()
        => InvokeAsync(nameof(IPlayerService.SignUpAsync), 0, static (c, _) => c.SignUpAsync());

    public UniTask<ApiResult<SignInResponse>> SignInAsync(IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(IPlayerService.SignInAsync), 0, static (c, _) => c.SignInAsync(), handledCodes);
}

// UtaApiClient.cs（public sealed class : IDisposable）
public sealed class UtaApiClient : IDisposable
{
    public IDeviceService DeviceClient { get; }
    public DeviceTokenProvider TokenProvider { get; }   // ApiCore が使うもの
    public PlayerApi Player { get; }

    /// <param name="errorHandler">アプリの共通エラーハンドラー（Bootstrap が DefaultCommonErrorHandler を渡す）</param>
    private UtaApiClient(GrpcChannel channel, IDeviceService deviceClient, ApiCoreOptions options, string accountFilePath, string keySource, DeviceTokenProvider tokenProvider, PlayerApi player)
    {
        /* 各引数をフィールドに代入し、公開プロパティ DeviceClient・TokenProvider・Player にも代入する */
    }

    public static UtaApiClient Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource, ICommonErrorHandler errorHandler)
    {
        // 1. channel = ApiChannel.Create(serverUrl); callInvoker = ApiChannel.CreateCallInvoker(channel);
        // 2. options = new ApiCoreOptions(new AppVersion(platform, appVersion));
        // 3. deviceClient = ServiceClientFactory.Create<IDeviceService>(callInvoker); playerClient = ServiceClientFactory.Create<IPlayerService>(callInvoker);
        // 4. tokenProvider = new DeviceTokenProvider(deviceClient, new EncryptedFileAccountStore(accountFilePath, keySource), options);
        // 5. core = ApiCore.CreateBuilder(options, tokenProvider, new ErrorPolicyProvider(UtaErrors.Create()), errorHandler).Build();
        // 6. new UtaApiClient(channel, deviceClient, options, accountFilePath, keySource, tokenProvider, new PlayerApi(core, playerClient))
    }

    /// <summary>同じ端末のアカウントのファイルを読む、もう 1 つの DeviceTokenProvider（診断で、メインの持つトークンをサーバーで無効にするために使う）。</summary>
    public DeviceTokenProvider CreateTokenProvider()
    {
        // new DeviceTokenProvider(deviceClient, new EncryptedFileAccountStore(accountFilePath, keySource), options)
    }

    public void Dispose() { /* channel.Dispose() */ }
}
```

`UtaApiClient` に `options`・`accountFilePath`・`keySource` を持たせる理由は `CreateTokenProvider`（診断）と `Dispose`（チャンネルの破棄の置き場）。

`using`（ファイルごと）: `UtaApiClient` は `System`・`Grpc.Net.Client`・`VolTech.Client`・`VolTech.Client.Auth`・`VolTech.Client.Unity`・`VolTech.Server.Abstractions`・`Uta.Shared.Contracts.Player`。`PlayerApi` は `System.Collections.Generic`・`Cysharp.Threading.Tasks`・`VolTech.Client`・`VolTech.Server.Abstractions`・`Uta.Shared.Contracts.Player`。足りない `using` はコンパイルエラーに従って足してよい（設計判断ではない）。

`ApiCore` はアプリで 1 つ（`UtaApiClient` がシングルトンなので満たす）。`WithErrorNavigator`・`WithLogger`・`AddActivityHandler` は呼ばない。

### 4.3 `PlayerGateway`（タスク 2。`Uta.Client.Infrastructure.Player`。`public sealed class PlayerGateway : IPlayerPort`）

```csharp
public PlayerGateway(UtaApiClient api) { /* api.Player を持つ */ }

private readonly PlayerApi player;   // コンストラクターで api.Player を代入
private readonly ErrorCode[] notCreated = { PlayerErrors.NotCreated };   // インスタンスのフィールド（static にしない）

public async UniTask<PlayerSignUpResult> SignUpAsync()
{
    var result = await player.SignUpAsync();
    return result.IsSuccess ? PlayerSignUpResult.Success(result.Value.Created) : PlayerSignUpResult.Failure(result.Error.ToString());
}

public async UniTask<PlayerSignInResult> SignInAsync()
{
    var result = await player.SignInAsync(notCreated);
    if (result.IsSuccess) { return PlayerSignInResult.Success(ToSnapshot(result.Value)); }
    if (result.Error.Code.Equals(PlayerErrors.NotCreated)) { return PlayerSignInResult.NotCreated(); }
    return PlayerSignInResult.Failure(result.Error.ToString());
}

/// <summary>DTO → Application の型。Idols・SupportCards が null なら空として扱う。</summary>
public static PlayerSnapshot ToSnapshot(SignInResponse response)
```

`ToSnapshot` は `OwnedIdolDto[]` → `OwnedIdol` の `List`、`OwnedSupportCardDto[]` → `OwnedSupportCard` の `List` に詰め替え、`PlayerSnapshot` を返す。

### 4.4 端末の認証まわりの変更（タスク 3）

```csharp
// IDeviceAuthPort に足す
/// <summary>別のトークンの提供者で発行し直し、いまこの端末が持つトークンをサーバー側で無効にする（再認証の再送の診断用）。</summary>
UniTask InvalidateServerSideTokenAsync();

// SignInUseCase に足す
public UniTask InvalidateServerSideTokenAsync() => port.InvalidateServerSideTokenAsync();
```

`DeviceAuthGateway` の変更:

- `IDisposable` をやめる（`Dispose` を消す）。`Create` を消し、コンストラクターを `public DeviceAuthGateway(UtaApiClient api)` にする（`api.DeviceClient`・`api.TokenProvider` を持つ。`api` も持つ）。`using` の不要になったものを消す
- 既存のメソッド（`GetAccountStateAsync`・`EnsureSignedInAsync`・`ReissueTokenAsync`・`CanReceiveServerErrorAsync`）は、`provider` を `api.TokenProvider`、`client` を `api.DeviceClient` に読み替えて本体を変えない
- 足す:

```csharp
public async UniTask InvalidateServerSideTokenAsync()
{
    var other = api.CreateTokenProvider();
    await other.GetAccountStatusAsync();
    other.Invalidate(null);
    await other.GetTokenAsync();   // サーバーが新しいトークンを発行し、古いトークン（メインの持つもの）が無効になる
}
```

### 4.5 Bootstrap（タスク 3）

`B/ErrorDialogView.cs`（名前空間 `Uta.Client.Bootstrap`。`public sealed class ErrorDialogView : MonoBehaviour, IErrorDialogView`。診断用の IMGUI。画面の計画で作り直す、と XML コメントに書く）:

- `public void Initialize(bool autoDismiss)`
- `public UniTask<ErrorDialogButton> ShowAsync(ApiError error, ErrorDialogButtons buttons)`:
  1. `Debug.LogWarning("[Uta] error dialog: " + error + " buttons=" + buttons)`
  2. `autoDismiss` なら、押さずに即座に返す: `Ok`→`ErrorDialogButton.Ok`、`RetryOrTitle`→`Title`、`Home`→`Home`、`Title`→`Title`、`Store`→`Store`（`UniTask.FromResult`）
  3. そうでなければ `UniTaskCompletionSource<ErrorDialogButton>` を作ってフィールドに持ち、表示する内容（`error.ToString()` と `buttons`）をフィールドに入れ、`source.Task` を返す
- `OnGUI`: 表示中のときだけ描く。`GUI.depth = -1`（`StartupView` より手前）。`new GUIStyle(GUI.skin.box)` と `new GUIStyle(GUI.skin.button)` を毎回作り（`GUI.skin` 自体は書き換えない）、両方の `fontSize` に `Mathf.Max(14, Screen.height / 40)` を当て、box は `wordWrap = true`・`normal.textColor = Color.white`。メッセージは `GUI.Box(new Rect(16, Screen.height / 2, Screen.width - 32, Screen.height / 4), error.ToString(), boxStyle)`。ボタンは個数 n を `buttons` から決め（`RetryOrTitle` だけ 2、ほかは 1）、各ボタンを `new Rect(16 + k * (w + 16), Screen.height * 3 / 4 + 16, w, Screen.height / 12)`（`w = (Screen.width - 32 - 16 * (n - 1)) / n`、k は 0 始まり）に `GUI.Button(rect, 文字, buttonStyle)` で描く（`Ok`→「OK」、`RetryOrTitle`→「Retry」「Title」、`Home`→「Home」、`Title`→「Title」、`Store`→「Store」）。押されたら対応する `ErrorDialogButton` で `source.TrySetResult` し、表示を消す（`OnGUI` の中で同期的に呼んでよい。UniTask の継続は後の PlayerLoop で走る）

`B/UtaBootstrap.cs` の変更（`RunAsync` の `LifetimeScope.Create` の前後）:

```csharp
var quitAfterCheck = Array.IndexOf(args, "-uta-quit-after-check") >= 0;   // 既存の 2 か所の判定もこの変数にする
var errorDialog = new GameObject("ErrorDialogView").AddComponent<ErrorDialogView>();
errorDialog.Initialize(quitAfterCheck);
UnityEngine.Object.DontDestroyOnLoad(errorDialog.gameObject);
var accountPath = Path.Combine(UnityEngine.Application.persistentDataPath, ServerEndpoint.AccountFileName(serverUrl));

// スコープの登録（IDeviceAuthPort の登録を置き換える）
builder.Register<UtaApiClient>(
    _ => UtaApiClient.Create(serverUrl, AppInfo.Platform, AppInfo.Version, accountPath, AppInfo.AccountKeySource, new DefaultCommonErrorHandler(errorDialog)),
    Lifetime.Singleton);
builder.Register<DeviceAuthGateway>(Lifetime.Singleton).As<IDeviceAuthPort>();
builder.Register<PlayerGateway>(Lifetime.Singleton).As<IPlayerPort>();
builder.Register<PlayerCache>(Lifetime.Singleton);
builder.Register<SignInUseCase>(Lifetime.Singleton);
builder.Register<EnterPlayerUseCase>(Lifetime.Singleton);
builder.RegisterEntryPoint<StartupPresenter>();
```

組み替え後の `RunAsync` の最終形（`MasterCardCatalog` の読み込みと失敗時の扱いは今のまま）:

```csharp
private static async UniTaskVoid RunAsync()
{
    var args = Environment.GetCommandLineArgs();
    var serverUrl = ServerEndpoint.Resolve(args);
    var quitAfterCheck = Array.IndexOf(args, "-uta-quit-after-check") >= 0;

    var view = new GameObject("StartupView").AddComponent<StartupView>();
    UnityEngine.Object.DontDestroyOnLoad(view.gameObject);
    var errorDialog = new GameObject("ErrorDialogView").AddComponent<ErrorDialogView>();
    errorDialog.Initialize(quitAfterCheck);
    UnityEngine.Object.DontDestroyOnLoad(errorDialog.gameObject);

    CardCatalog cardCatalog;
    try { /* 今のまま */ } catch (Exception e) { /* 今のまま。ただし Quit の判定は quitAfterCheck を使う */ }
    Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count);

    var accountPath = Path.Combine(UnityEngine.Application.persistentDataPath, ServerEndpoint.AccountFileName(serverUrl));
    var scope = LifetimeScope.Create(builder =>
    {
        builder.RegisterInstance(view);
        builder.RegisterInstance<ICardCatalog>(cardCatalog);
        builder.RegisterInstance(new StartupOptions(quitAfterCheck));
        /* 上の Register 群（UtaApiClient … StartupPresenter）をここに置く */
    });
    UnityEngine.Object.DontDestroyOnLoad(scope.gameObject);
}
```

`using` を足す（`VolTech.Client`・`Uta.Client.Application.Player`・`Uta.Client.Infrastructure.Api`・`Uta.Client.Infrastructure.Player`）。`B/Uta.Client.Bootstrap.asmdef` の `references` に `VolTech.Server.Abstractions` を足す（`ApiError` のシグネチャが `ErrorCode` を含むため）。コンパイルで `CS0012`（`Uta.Shared.Contracts` が見つからない）が出たら、`Uta.Shared.Contracts` も足してよい（設計判断ではない。足したら報告に書く）。

### 4.6 `StartupPresenter`（タスク 3）

コンストラクターに `EnterPlayerUseCase enterPlayer` を足す（引数の順: `signIn, enterPlayer, view, options`）。`RunAsync` の流れ（`AddLine` の文字列も、この表のとおりにする。`try` の外側・`succeeded`・`Quit` の扱いは今のまま）:

| 順 | 処理 | `AddLine` |
|---|---|---|
| 1 | — | `startup: start` |
| 2 | `var registered = await signIn.ExecuteAsync();` | `registered ? "device: registered" : "device: token issued"` |
| 3 | `var entered = await enterPlayer.ExecuteAsync(registered);` | `"sign-up: " + (!entered.SignedUp ? "skipped" : entered.Created ? "created" : "already exists")` |
| 4 | — | `"sign-in: ok level=" + s.Level + " exp=" + s.Exp + " gold=" + s.Gold + " supportPoints=" + s.SupportPoints + " gachaCurrency=" + s.GachaCurrency + " idols=" + s.Idols.Count + " supportCards=" + s.SupportCards.Count + " hasActiveProduce=" + s.HasActiveProduce + " discardedProduce=" + s.DiscardedProduce`（`s = entered.Snapshot`） |
| 5 | `await signIn.ReissueAsync();` | `reissue: ok` |
| 6 | `await signIn.InvalidateServerSideTokenAsync(); await enterPlayer.ExecuteAsync(false);` | `reauth retry: ok` |
| 7 | `CanReceiveServerErrorAsync`（今のまま） | 今のまま（`server error delivery: ok` / `FAILED`、`connectivity check: OK` / `FAILED ...`） |

`StartupView` は XML コメントを「疎通とサインアップ・サインインの結果を IMGUI で出す診断用の表示」に直すだけ（挙動は変えない）。

## 5. タスク

前提: 作業の場所は `C:\Work\Uta-wt\3`。Unity の Editor は閉じている。**Unity の確認は、Editor を閉じたバッチモードで、一度に 1 つだけ動かす**（同じプロジェクトを同時に開けない）。

- コンパイル: `"C:\Program Files\Unity\Hub\Editor\7000.0.0a12\Editor\Unity.exe" -batchmode -nographics -quit -projectPath C:\Work\Uta-wt\3\client\uta -logFile <ログのパス>` を実行し、ログに `error CS` が無いこと・終了コード 0 を確かめる。このとき新しい `.cs` / フォルダの `.meta` が作られる（コミットに含める）
- EditMode のテスト: `... Unity.exe -batchmode -nographics -projectPath C:\Work\Uta-wt\3\client\uta -runTests -testPlatform EditMode -testResults <XML のパス> -logFile <ログのパス>`（`-quit` は付けない）。結果の XML で失敗が 0 件であること
- ログ・XML は作業用のスクラッチパッドに出し、リポジトリに入れない

**各タスクが終わったとき、Unity のコンパイルが通り、EditMode のテストが通る状態にする。** `dotnet build Uta.slnx` は `client/` を含まないが、`shared/` を変えないので影響しない（念のため最後に 1 回流す）。

### タスク 1: Application の型とユースケース

- 触る: 3章の タスク 1 の行
- 手順: 4.1 のとおりに型を作る。`EnterPlayerUseCase.ExecuteAsync`: `signedUp=false`・`created=false` で始め、`afterRegistration` なら `SignUpAsync` を呼ぶ（失敗なら 4.1 の例外。成功なら `signedUp=true`・`created=result.Created`）。続けて `SignInAsync`: Succeeded→cache.Set して返す。PlayerNotCreated→`signedUp` なら例外、でなければ SignUp（失敗なら例外）して `signedUp=true`・`created` を更新し、もう一度 SignIn（Succeeded 以外は、PlayerNotCreated も含めて例外）。Failed→例外
- 境界: SignIn が 2 回続けて PlayerNotCreated なら、2 回目で例外（無限に繰り返さない）。例外のとき `cache` は変えない
- 確かめ方: `EnterPlayerUseCaseTests`（5章）が通る

### タスク 2: Infrastructure（`ApiCore`・`PlayerApi`・`PlayerGateway`）

- 触る: 3章の タスク 2 の行
- 手順: 4.2・4.3 のとおり。`DeviceServiceClientFactory` を消すので、`DeviceAuthGateway` の `DeviceServiceClientFactory.Create(callInvoker)` を `ServiceClientFactory.Create<IDeviceService>(callInvoker)` に直す（`DeviceAuthGateway` はこのタスクではこれ以外を変えない。`Create` も残す）
- 境界: `ErrorPolicyProvider` は重複や予約の domain を弾くので、`UtaErrors.Create()` は 1 件だけ
- 確かめ方: `UtaErrorsTests`・`PlayerGatewayTests` が通る。`UtaMagicOnionClientInitializer` の Source Generator の出力に `IPlayerService` が入ったことは、コンパイルが通れば足りる（`IPlayerService` のクライアントを作る `ServiceClientFactory.Create<IPlayerService>` の呼び出しが `PlayerApi` 経由でコンパイルできる）

### タスク 3: 起動の流れの組み替え

- 触る: 3章の タスク 3 の行
- 手順: 4.4・4.5・4.6 のとおり
- 境界: `-uta-quit-after-check` のとき、ダイアログは押されるのを待たない（4.5）。`GetAccountStateAsync` を先に 1 回だけ呼ぶ既存の決まり（`SignInUseCase`）は変えない。アカウントが `Corrupted` のときは、今までどおり通信せずに止まる
- 確かめ方: コンパイルが通り、EditMode のテストが全部通る。そのうえで、メインのセッションが 6章の実行を行う（サーバーが要るので実装担当は行わない）

### タスク 4: 設計書の反映（メインのセッション）

実装の結果を（`ErrorDialogView` を Bootstrap に置く理由・`PLAYER_NOT_CREATED` の既定の action・「サインイン」の呼び分けも含めて）[screen-design.md](../wiki/architecture/screen-design.md) 4章・[api-design.md](../wiki/architecture/api-design.md) 1章・[repository-layout.md](../wiki/architecture/repository-layout.md)（クライアントの層に `PlayerGateway`・`UtaApiClient` など）・[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md)（確かめたこと）に反映する。計画を `status: done` にして `python -I .claude/skills/linting-wiki/lint_wiki.py` を流す。

## 6. テストと実行での確認

### 6.1 EditMode のテスト（`T/`）

書き方: 偽の `IPlayerPort`（テストクラスの private なネストクラス）は、`SignInAsync` の結果を `Queue<PlayerSignInResult>`、`SignUpAsync` の結果を `Queue<PlayerSignUpResult>` で渡し、呼ばれた順を `List<string>`（`"SignUp"`・`"SignIn"`）に記録する。偽は完了済みの `UniTask` を返すので、テストは通常の `[Test]` で `.GetAwaiter().GetResult()` を使って待つ（例外は `Assert.Throws<InvalidOperationException>(() => useCase.ExecuteAsync(...).GetAwaiter().GetResult())` で確かめ、`ex.Message` を比べる）。名前空間は `Uta.Client.Infrastructure.Tests`。

- `EnterPlayerUseCaseTests`（偽の `IPlayerPort` で。呼ばれた順序を記録する）:
  - `Execute_AfterRegistration_CallsSignUpThenSignIn`（`afterRegistration=true` → SignUp, SignIn の順。`SignedUp=true`・`Created=true`）
  - `Execute_Registered_CallsOnlySignIn`（`false` で SignIn が Success → SignUp を呼ばない。`SignedUp=false`）
  - `Execute_NotCreated_SignsUpThenSignsInAgain`（SignIn: NotCreated → SignUp → SignIn: Success。呼び出しは SignIn, SignUp, SignIn）
  - `Execute_NotCreatedAfterSignUp_Throws`（`afterRegistration=true` で SignIn が NotCreated → `InvalidOperationException`、メッセージは `"sign-in: player not created after sign-up"`）
  - `Execute_NotCreatedTwice_Throws`（`false`・SignIn が 2 回とも NotCreated → 例外。SignIn は 2 回で止まる）
  - `Execute_SignUpFails_ThrowsWithoutSignIn`（SignUp が Failure → `"sign-up failed: ..."`、SignIn を呼ばない）
  - `Execute_SignInFails_Throws`（Failure → `"sign-in failed: ..."`）
  - `Execute_Success_SetsCache`／`Execute_Failure_KeepsCache`
- `UtaErrorsTests`:
  - `Create_ContainsPlayerNotCreatedWithTitleAction`
  - `Create_IsAcceptedByErrorPolicyProvider`（`new ErrorPolicyProvider(UtaErrors.Create())` が例外を投げず、`GetDefaultAction(PlayerErrors.NotCreated)` が `Title`）
- `PlayerGatewayTests`（`ToSnapshot` のみ。`ApiCore` の通信は 6.2 で確かめる）:
  - `ToSnapshot_CopiesAllFields`（Idols 2 件・SupportCards 2 件・各スカラー）
  - `ToSnapshot_NullArrays_BecomeEmpty`

### 6.2 実行での確認（メインのセッション。結果は報告に書く。できなければ「確認できていない」と書く）

1. `dotnet build Uta.slnx`（警告 0・エラー 0）
2. Docker が使えるとき: `CLAUDE.md` の「ローカルのサーバー」の手順でサーバーを起動する
3. **Windows の IL2CPP のプレイヤー**（`CLAUDE.md` のビルド手順。Editor は閉じる）で。新しい DTO（`SignUpResponse`・`SignInResponse` と配列の入れ子）は別アセンブリ（`Uta.Shared.Contracts`）にあり、1b で見えた「生成された `Resolver` が別アセンブリの DTO のフォーマッタを持たない」（unity7-evaluation.md 3.2）と同じ条件なので、IL2CPP で必ず確かめる。Editor でも同じ手順を 1 回通す。 `Uta.exe -batchmode -nographics -uta-quit-after-check` を実行し、次を確かめる:
   - アカウントのファイルが無い 1 回目: `device: registered` → `sign-up: created` → `sign-in: ok ...`（初期の所持品が入っている）→ `reissue: ok` → `reauth retry: ok` → `connectivity check: OK`、終了コード 0
   - 2 回目: `device: token issued` → `sign-up: skipped` → `sign-in: ok ...`
   - プレイヤーの行だけを消して（`docker compose ... exec` で `psql` の `TRUNCATE players CASCADE`）3 回目: `sign-up: created`（`PLAYER_NOT_CREATED` を `handledCodes` で受けて `SignUpAsync` → `SignInAsync`）。これで `ErrorInfo` のトレーラーが `ApiCore` 経由で届くことも確かめられる
   - サーバーを止めて実行: `connectivity check: FAILED ...`、終了コード 1。**この経路は端末の認証（`ApiCore` を通らない）で失敗するので、`ErrorDialogView` の自動で閉じる経路・`PlayerGateway` の `Failure` への変換は通らない**。この 2 つは実行でも EditMode でも確かめない（未確認と書く）。`DefaultCommonErrorHandler` 自体は基盤のテストが確かめている
4. 確かめられなかったもの（Docker・Unity のビルドが使えないなど）は、推測で「通った」と書かず、報告とドキュメントに「未確認」と書く

## 7. 未決事項

なし（ユーザーが決めたことは 1章。実装担当が計画にない設計判断に出会ったら、止まって報告する）。

実装中に確かめることで、結果によっては計画を直す点: (a) `MagicOnionClientGeneration` が複数の型を受け取れるか（4.2 に代案を書いた）、(b) `DeviceTokenProvider` を 2 つ作って同じアカウントのファイルを読むときの競合（`GetAccountStatusAsync` を先に呼べば足りる想定）、(c) `ApiServiceBase` の属性のリフレクションと `ApiCore` の再送が Windows の IL2CPP で動くか（動かなければ止まって報告）、(d) 新しい DTO のフォーマッタ（unity7-evaluation.md 3.2）が IL2CPP で解決されるか（されなければ止まって報告。MessagePack の生成などは設計判断なのでユーザーに確認する）。
