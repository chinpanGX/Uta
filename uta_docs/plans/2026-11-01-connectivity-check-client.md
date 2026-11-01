---
title: 実装計画: 通信の疎通（クライアント）
status: in-progress
updated: 2026-11-01
spec: ../wiki/architecture/repository-layout.md, ../wiki/overview/game-spec.md, ../wiki/architecture/screen-design.md
---

# 実装計画: 通信の疎通（クライアント）

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の 1b。Unity 7（`7000.0.0a12`）に外部パッケージと `shared/` を取り込み、クライアントの 4 層（[repository-layout.md](../wiki/architecture/repository-layout.md) 3章）の asmdef を作り、ローカルのサーバーに **サインアップ（端末の登録 `IDeviceService.RegisterAsync`）・サインイン（トークンの発行 `IssueTokenAsync`）** を通す最小の起動処理を書く。中身の決定は repository-layout.md 12章の手順 5・6。サインアップは「端末の登録」で、Uta のプレイヤーを作る `SyncAsync` とは別。

- 関係する ADR: [0005](../wiki/decisions/0005-foundation-submodule.md)、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0009](../wiki/decisions/0009-client-stack.md)
- 受け持つ完了条件: [game-spec.md](../wiki/overview/game-spec.md) 完了条件 0（Editor と Windows の IL2CPP で、サインアップ・サインインが通る）
- 1a（サーバー・shared の側。別のセッション・PR）との関係: 手順 6 の接続確認は、1a が main に入り、`server/compose.yaml` と `Uta.Server` が動いてから行う。`com.uta.shared.contracts` は 1a が作る `shared/Uta.Shared.Contracts` を使うので、タスク 2 は 1a のマージ後
- **この計画が触るのは `client/uta/` の下、`shared/Uta.Shared.Domain/`・`shared/Uta.Shared.Contracts/` の直下の新規ファイル（`package.json`・asmdef・`.meta`・タスク 2 の印のクラス）、計画・wiki だけ**。ルートの props・`shared/` の既存ファイル・`server/` は 1a が触るので変えない
- 範囲外:
  - `ApiCore`（順番待ち・再認証・エラーハンドラー）の組み立て。認証が要る RPC が出る計画で入れる。「無効・期限切れなら 1 回だけ取り直して再送する」は `ApiCore` の振る舞い（[auth.md](../../external/VolTech-MagicOnion/docs/spec/auth.md)）なので、ここでは `DeviceTokenProvider` の「トークンを捨てて取り直す」までを確かめる。**`ApiCore` 経由の再送と、`ApiServiceBase` が属性をリフレクションで読む部分の IL2CPP（基盤の [client.md](../../external/VolTech-MagicOnion/docs/design/client.md) が未確認と書いている）は未確認のまま残る**
  - UnityScreenNavigator・Supplement を**使うこと**（パッケージの取り込みだけを行い、asmdef の参照とコードは、使う計画で足す）、MasterMemory・MasterMemoryAddons（Unity のパッケージ（`package.json`・asmdef）が無く、入れ方は masterdata-pipeline.md の未決。マスタの計画で決める。2026-11-01 にユーザーが「今回は入れない」と判断）、Android / iOS の実機ビルド、Uta の RPC・DTO、画面
  - 共通基盤（`external/`）の変更。必要とわかったら、実装担当は止まって報告する（3.1 のルール）

### この計画で決めたこと（wiki に書いていなかった細部）

| 項目 | 決めたこと | 理由 |
|---|---|---|
| 資格情報の保存 | 基盤の `EncryptedFileAccountStore` をそのまま使う（2026-11-01 にユーザーが確認）。[10.6](../wiki/architecture/repository-layout.md) の Supplement.MessagePack は、ゲームのセーブデータの話で、端末の資格情報には使わない | 基盤のガイドの形。`IAccountStore` 越しなので、差し替えられる |
| アカウントのファイル | `persistentDataPath/account-<サーバーの URL の英数字以外を _ にしたもの>.dat`（例: `account-http___localhost_5000.dat`） | 基盤の `EncryptedFileAccountStore` が「接続先ごとに分ける」としている |
| 層を 4 つ作る理由 | 手順 5 の asmdef の連鎖（`Application` → `Infrastructure` → `Presentation` → `Bootstrap`）と VContainer を Unity 7 で通すことも、この計画の確かめることに入っている（8章の「UnityScreenNavigator・VContainer・UniTask が Unity 7 で動くか」の VContainer・UniTask の分） | 最小にするため、ユースケースは戻り値の型を持たない |
| 疎通の表示 | uGUI を使わず IMGUI（`OnGUI`）で文字を出すだけの診断用の表示。シーンは作らず、`RuntimeInitializeOnLoadMethod` で起動する | フォント・シーンのアセットを要らなくして、確かめることを通信に絞るため。画面の計画（7 番目）で作り直す |
| サーバーのエラーが届くことの確認 | 存在しない端末 ID で `IssueTokenAsync` を呼び、`RpcException`（`Unauthenticated`）が返ることを確かめる（`CanReceiveServerErrorAsync`）。**確かめられるのは gRPC のステータスが YetAnotherHttpHandler・IL2CPP で届くことまで**。この RPC は `ErrorInfo` のトレーラー（`grpc-status-details-bin`）を付けないので、基盤の client.md が未確認としている「エラー詳細のトレーラーが届くか」には答えない（5章の「未確認」に残す） | 認証の要らない最小の呼び出しで、エラーの経路を 1 つ通すため |
| 接続先 | 既定は `http://localhost:5000`。コマンドライン引数 `-uta-server-url <URL>` で上書き | 実機から LAN の IP に向けるため（11.1） |
| `x-app-version` | `Uta.Client.Bootstrap` の定数 `AppInfo.Version = 1` | 10.5 |
| 確認の終了 | `-uta-quit-after-check` があれば、確認のあと `Application.Quit(0 または 1)` | `-batchmode` のプレイヤーで結果を取るため |
| ProjectSettings の変更 | Unity が書き換えた `ProjectSettings/` は `git diff` を見て、意図した変更（Standalone の IL2CPP）だけ残し、それ以外（Unity が勝手に足す項目）は戻す。残した差分はメインのセッションがコミット前に確認する | 意図しない設定がコミットに混ざらないようにするため |
| 起動の部品 | `StartupPresenter` は `IStartable`（`Start()` から `UniTask.Void` で非同期の本体を動かす）にする。`IAsyncStartable` は、VContainer の UniTask 連携の定義（`VCONTAINER_UNITASK_INTEGRATION`）の有無で戻り値の型が変わるため使わない | 定義に左右されない書き方にするため。VContainer の asmdef の `versionDefines` が UniTask の存在で定義を自動で付けるかは**未確認**（タスク 1 で `Library/PackageCache/` の VContainer の asmdef を見て、`unity7-evaluation.md` に書く） |
| 診断用のメソッド | `IDeviceAuthPort` の `ReissueTokenAsync`・`CanReceiveServerErrorAsync` と、`SignInUseCase` の同名のメソッドは、疎通の確認のためだけの仮のもの。画面の計画（7 番目）で `Application` から外し、`Infrastructure` の診断用のクラスに移す | 確かめることを増やさず、後で消す前提を書いておく |
| `Application.identifier` | URP テンプレートの値（`com.Unity-Technologies.com.unity.template.urp-blank`）のまま使う。変えない。`EncryptedFileAccountStore` はこの値から鍵を作るので、**変えると保存済みのアカウントのファイルが読めなくなる（`Corrupted`）。変えたら、端末のアカウントのファイルを消す**。この注意を `architecture/unity7-evaluation.md` に書く | 識別子の決定はこの計画の範囲外（ユーザーの判断） |
| VContainer の版 | `1.19.0`（タグの実在は `git ls-remote --tags` で 2026-11-01 に確認済み） | Sandbox に無いので基盤の行を写せない |
| asmdef の参照名 | 3.3 の `UniTask`・`VContainer` などは仮の名前。タスク 1 でパッケージを取り込んだあと、メインのセッションが `Library/PackageCache/` の `.asmdef` の `name` で確かめて直す（実装担当は Unity を動かせない） | 取り込む前には確かめられない |
| 空の asmdef | `Uta.Shared.Contracts` の asmdef は、スクリプトが 0 だと参照した側がエラーになる恐れがある（推測）ので、タスク 2 で印のクラス `ContractsAssemblyMarker` を `shared/Uta.Shared.Contracts/` に新規ファイルとして足す。サービスIF・DTO を足す計画で消す | Unity 7 で確かめる。要らないと分かったら消す |

## 2. 触るファイル

すべて `client/uta/` の下（`Packages/`・`Assets/`）。ただし `shared/` の行は `shared/` の直下。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `shared/Uta.Shared.Domain/package.json` | 3.1 | 1 |
| 作成 | `shared/Uta.Shared.Domain/Uta.Shared.Domain.asmdef` | 3.1 | 1 |
| 変更 | `client/uta/Packages/manifest.json` | 3.2。タスク 1 は Contracts 以外、タスク 2 で Contracts | 1・2 |
| 作成 | `client/uta/Assets/NuGet.config`・`packages.config` | 基盤の Sandbox から写す | 1 |
| 作成 | `client/uta/Assets/Plugins/Microsoft.Bcl.AsyncInterfaces/`（DLL と `.meta`）・`Plugins.meta` | 基盤の Sandbox から写す | 1 |
| 変更 | `.gitignore` | Unity の `Assets/Packages/` を足す | 1 |
| 作成 | `client/uta/Assets/Scripts/Application/Uta.Client.Application.asmdef` | 3.3 | 1 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/Uta.Client.Infrastructure.asmdef` | 3.3 | 1 |
| 作成 | `client/uta/Assets/Scripts/Presentation/Uta.Client.Presentation.asmdef` | 3.3 | 1 |
| 作成 | `client/uta/Assets/Scripts/Bootstrap/Uta.Client.Bootstrap.asmdef` | 3.3 | 1 |
| 作成 | `shared/Uta.Shared.Contracts/package.json`・`Uta.Shared.Contracts.asmdef`・`ContractsAssemblyMarker.cs` | 3.1・3.4 | 2 |
| 作成 | `client/uta/Assets/Scripts/Application/Auth/AccountState.cs` | 3.5 | 3 |
| 作成 | `client/uta/Assets/Scripts/Application/Auth/IDeviceAuthPort.cs` | 3.5 | 3 |
| 作成 | `client/uta/Assets/Scripts/Application/Auth/SignInUseCase.cs` | 3.5 | 3 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/Api/UtaMagicOnionClientInitializer.cs` | 3.6 | 3 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/Api/DeviceServiceClientFactory.cs` | 3.6 | 3 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/Auth/DeviceAuthGateway.cs` | 3.6 | 3 |
| 作成 | `client/uta/Assets/Scripts/Presentation/Startup/StartupOptions.cs`・`StartupView.cs`・`StartupPresenter.cs` | 3.7（1 型 1 ファイル） | 3 |
| 作成 | `client/uta/Assets/Scripts/Bootstrap/AppInfo.cs`・`ServerEndpoint.cs`・`UtaBootstrap.cs` | 3.8（1 型 1 ファイル） | 3 |
| 作成 | `client/uta/Assets/Scripts/Editor/Uta.Client.Editor.asmdef`・`BuildCommands.cs` | 3.9 | 3 |
| 作成 | Unity が作る `.meta`・`client/uta/Packages/packages-lock.json` | すべてコミットする | 1〜3 |
| 作成 | `uta_docs/wiki/architecture/unity7-evaluation.md` | Unity 7 で確かめたもの・未確認のものの一覧（5章の表）と、`Application.identifier` の注意 | 4 |
| 変更 | `uta_docs/wiki/architecture/README.md`・`uta_docs/wiki/index.md`（必要なら） | `unity7-evaluation.md` の登録 | 4 |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md` | 8章の要検証のうち、確かめた行を消す。10.6 に「端末の資格情報（device_id・secretKey）は基盤の `EncryptedFileAccountStore` で保存する。Supplement.MessagePack はゲームのセーブデータ用」と書く（2026-11-01 のユーザーの判断）。12章・game-spec.md の 1a / 1b の区分は 1a のセッションが書くので触らない | 4 |
| 変更 | `uta_docs/wiki/architecture/server/server-foundation.md` | 「基盤の機能のうち Uta で使うか未定のもの」から `StoredAccount`（形式）を外し、基盤の `EncryptedFileAccountStore` を使うと書く | 4 |
| 変更 | `uta_docs/wiki/glossary.md` | 「サインアップ」（端末の登録 `RegisterAsync`）・「サインイン」（トークンの発行 `IssueTokenAsync`）を足す。Uta のプレイヤーを作るのは `SyncAsync` で別（基盤の `IDeviceService` のコメントはプレイヤー作成までを「サインアップ」と呼ぶが、Uta ではこの意味で使う） | 4 |
| 変更 | `uta_docs/wiki/architecture/api-design.md` | 53 行目付近の「サインインの流れ」を「起動の流れ」に直す（glossary の「サインイン」＝トークンの発行と食い違わないように。意味は変えない） | 4 |
| 変更 | `uta_docs/wiki/architecture/repository-layout.md`（2章の構成図） | `Assets/Scripts/` に `Editor/`（ビルドのコマンド）を足す | 4 |
| 変更 | `CLAUDE.md` | 「ビルド・テスト・実行」に、動いた Unity のバッチモードのコマンド（コンパイルの確認・Windows の IL2CPP ビルド）を足す。動かなかったものは書かない | 4 |
| 変更 | `uta_docs/plans/2026-11-01-connectivity-check-client.md` | `status: done` | 4 |

ADR 0005・0006・0009 は書き換えない（既存の ADR は変えない。CLAUDE.md の実装前のルール）。`unity7-evaluation.md` から、各 ADR の未決事項・影響へリンクして、解決した点を書く。`x-app-version` が付かない呼び出し（端末の登録）は、サーバーが判定しないので問題ない（10.5）。screen-design.md 4章の起動の流れとの違い（この計画は 1 の認証だけ。画面は IMGUI の診断用）は `unity7-evaluation.md` に書く。

## 3. 型とシグネチャ

### 3.1 `shared/Uta.Shared.Domain` と `shared/Uta.Shared.Contracts` のパッケージ

`package.json`:

| ファイル | `name` | `displayName` |
|---|---|---|
| `shared/Uta.Shared.Domain/package.json` | `com.uta.shared.domain` | `Uta Shared Domain` |
| `shared/Uta.Shared.Contracts/package.json` | `com.uta.shared.contracts` | `Uta Shared Contracts` |

どちらも `version` は `0.1.0`、`unity` は `7000.0`、`description` は日本語で 1 文。`license` は書かない。

asmdef（名前はアセンブリ名と同じ。`autoReferenced` は `false`）:

| ファイル | `name`・`rootNamespace` | `references` | `noEngineReferences` |
|---|---|---|---|
| `Uta.Shared.Domain.asmdef` | `Uta.Shared.Domain` | なし | `true` |
| `Uta.Shared.Contracts.asmdef` | `Uta.Shared.Contracts` | `Uta.Shared.Domain`、`VolTech.Server.Abstractions` | `true` |

### 3.2 `client/uta/Packages/manifest.json`

`dependencies` に次を足す（既存の行は変えない）。`file:` はこのファイルのあるフォルダ（`Packages/`）からの相対パス。UPM の git URL は、基盤の `external/VolTech-MagicOnion/Unity/VolTech.Sandbox/Packages/manifest.json` の行をそのまま写す（版が同じになる）。

| キー | 値 | タスク |
|---|---|---|
| `com.chinpangx.voltech.magiconion` | `file:../../../external/VolTech-MagicOnion/Unity/com.chinpangx.voltech.magiconion` | 1 |
| `com.uta.shared.domain` | `file:../../../shared/Uta.Shared.Domain` | 1 |
| `com.cysharp.unitask` | Sandbox の行（2.5.11） | 1 |
| `com.cysharp.magiconion.client.unity` | Sandbox の行（7.11.0） | 1 |
| `com.cysharp.yetanotherhttphandler` | Sandbox の行（1.11.7） | 1 |
| `com.github.messagepack-csharp` | Sandbox の行（v3.1.12） | 1 |
| `com.github-glitchenzo.nugetforunity` | Sandbox の行（v4.5.0） | 1 |
| `jp.hadashikick.vcontainer` | `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.19.0` | 1 |
| `com.uta.shared.contracts` | `file:../../../shared/Uta.Shared.Contracts` | 2 |
| `com.harumak.unityscreennavigator` | `file:../../../external/UnityScreenNavigator/Assets/UnityScreenNavigator`（相対パス。絶対パスで入れない） | 2 |
| `com.unity.addressables` | `4.1.2`（下の説明） | 2 |
| `com.chinpangx.supplement` | `file:../../../external/Supplement/Assets/Supplement`（依存の Addressables を引き込む） | 2 |
| `com.chinpangx.supplement.messagepack` | `file:../../../external/Supplement/Assets/Supplement.MessagePack`（依存が `com.chinpangx.supplement` 1.4.0、取り込むのは 1.5.0 なので、Unity のログに版の警告が出るかを控える） | 2 |

`com.unity.addressables` を 4.1.2 にした理由: Supplement は 2.7.6 を求めるが、Unity 7 の a12 は 4.1.1 に解決し、4.1.1 は `ScriptCompilationOptions.DevelopmentBuild` が obsolete で CS0619 のコンパイルエラーになる。4.1.2 で通ることを Editor（2026-11-01）で確認した。ユーザーの判断で 4.1.2 を直接指定する。

UnityScreenNavigator・Supplement は、ユーザーの判断（2026-11-01）で、この計画で取り込みまで行う（使うのは画面・ローカル保存の計画）。`com.chinpangx.supplement.zeromessenger` は入れない。`manifest.json` の並びは、Unity が書き直したアルファベット順のままにして、元の順に戻さない（差分を小さくするためには戻さない）。

`Assets/packages.config` と `Assets/NuGet.config` は `external/VolTech-MagicOnion/Unity/VolTech.Sandbox/Assets/` のものをそのまま写す（NuGet の版はここで固定される。getting-started.md の「使う版」と一致）。`Assets/Plugins/Microsoft.Bcl.AsyncInterfaces/` は、Sandbox の同じ場所のフォルダを `.meta`（Editor を外す設定が入っている）ごと写し、`Assets/Plugins.meta` も写す。`.gitignore` に `client/*/[Aa]ssets/Packages/` と `client/*/[Aa]ssets/Packages.meta` を足す。

### 3.3 クライアントの asmdef（`client/uta/Assets/Scripts/`）

名前はアセンブリ名（3章）。`autoReferenced` は `false`、`rootNamespace` は名前と同じ。参照の名前（`UniTask`・`VContainer` など）は、取り込んだあとに `Library/PackageCache/` の各パッケージの `.asmdef` の `name` で確かめて合わせる。

| asmdef | `references` | `overrideReferences` | `noEngineReferences` |
|---|---|---|---|
| `Application/Uta.Client.Application.asmdef` | `Uta.Shared.Domain`、`UniTask` | `true`（`precompiledReferences` は空。NuGet の DLL を参照させない） | `false`（3章） |
| `Infrastructure/Uta.Client.Infrastructure.asmdef` | `Uta.Client.Application`、`Uta.Shared.Domain`、`Uta.Shared.Contracts`（タスク 2 で足す）、`VolTech.Server.Abstractions`、`VolTech.MagicOnion.Client`、`UniTask` | `false`（NuGet の DLL は自動で見える） | `false` |
| `Presentation/Uta.Client.Presentation.asmdef` | `Uta.Client.Application`、`Uta.Shared.Domain`、`UniTask`、`VContainer` | `true`（`precompiledReferences` は空） | `false` |
| `Bootstrap/Uta.Client.Bootstrap.asmdef` | `Uta.Client.Application`、`Uta.Client.Infrastructure`、`Uta.Client.Presentation`、`VolTech.MagicOnion.Client`、`UniTask`、`VContainer` | `false` | `false` |

`Presentation` の `UnityScreenNavigator` は、この計画では入れない。

### 3.4 `ContractsAssemblyMarker`（タスク 2）

```csharp
namespace Uta.Shared.Contracts
{
    /// <summary>
    /// Unity の asmdef をスクリプトが 0 のままにしないための印。サービスIF・DTO を足したら消す。
    /// </summary>
    internal static class ContractsAssemblyMarker
    {
    }
}
```

`shared/Uta.Shared.Contracts/ContractsAssemblyMarker.cs`（C# 9、BOM 付き UTF-8、LF）。

### 3.5 `Uta.Client.Application`（名前空間 `Uta.Client.Application.Auth`）

`using` は `System`（`SignInUseCase` の `InvalidOperationException`）と `Cysharp.Threading.Tasks`。

```csharp
// AccountState.cs
public enum AccountState { NotRegistered = 0, Registered = 1, Corrupted = 2 }

// IDeviceAuthPort.cs
public interface IDeviceAuthPort
{
    /// <summary>端末に保存したアカウント情報の状態。通信はしない。</summary>
    UniTask<AccountState> GetAccountStateAsync();

    /// <summary>有効なトークンを取る。未登録なら端末の登録（サインアップ）をしてから発行する。トークンは返さない。</summary>
    UniTask EnsureSignedInAsync();

    /// <summary>今のトークンを捨てて、発行し直す。</summary>
    UniTask ReissueTokenAsync();

    /// <summary>存在しない端末でトークンを求め、サーバーの認証エラー（Unauthenticated）が届けば true。</summary>
    UniTask<bool> CanReceiveServerErrorAsync();
}

// SignInUseCase.cs
public sealed class SignInUseCase
{
    public SignInUseCase(IDeviceAuthPort port);

    /// <summary>
    /// 先に GetAccountStateAsync を 1 回だけ呼ぶ（EnsureSignedInAsync の後に呼ぶと、常に Registered になる）。
    /// 状態が Corrupted なら、通信せずに InvalidOperationException（メッセージは "account corrupted"）を投げる（保存したデータには触らない）。
    /// それ以外は EnsureSignedInAsync を呼び、先に取った状態が NotRegistered だったなら true（サインアップした）、Registered だったなら false を返す。
    /// </summary>
    public UniTask<bool> ExecuteAsync();

    public UniTask ReissueAsync();                    // port.ReissueTokenAsync
    public UniTask<bool> CanReceiveServerErrorAsync(); // port.CanReceiveServerErrorAsync
}
```

### 3.6 `Uta.Client.Infrastructure`

`Api/UtaMagicOnionClientInitializer.cs`（名前空間 `Uta.Client.Infrastructure.Api`）:

```csharp
[MagicOnionClientGeneration(typeof(IDeviceService))]
internal partial class UtaMagicOnionClientInitializer
{
}
```

`Api/DeviceServiceClientFactory.cs`: `internal static class DeviceServiceClientFactory`、`public static IDeviceService Create(CallInvoker callInvoker)`。基盤の Sandbox の `external/VolTech-MagicOnion/Unity/VolTech.Sandbox/Assets/Smoke/SmokeClient.cs` と同じ形で、`MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(UtaMagicOnionClientInitializer.Resolver, StandardResolver.Instance))` から `MessagePackMagicOnionSerializerProvider.Default.WithOptions(options)` を作り、`MagicOnionClient.Create<IDeviceService>(callInvoker, serializerProvider, Array.Empty<IClientFilter>(), UtaMagicOnionClientInitializer.ClientFactoryProvider)` を返す。

`Auth/DeviceAuthGateway.cs`（名前空間 `Uta.Client.Infrastructure.Auth`）:

```csharp
public sealed class DeviceAuthGateway : IDeviceAuthPort, IDisposable
{
    /// <param name="serverUrl">例: http://localhost:5000</param>
    /// <param name="platform">AppVersion.Windows など（Bootstrap が決める）</param>
    public static DeviceAuthGateway Create(string serverUrl, string platform, int appVersion, string accountFilePath, string applicationIdentifier);

    public UniTask<AccountState> GetAccountStateAsync();  // DeviceTokenProvider.GetAccountStatusAsync の AccountStatus を 1 対 1 で写す
    public UniTask EnsureSignedInAsync();                 // provider.GetTokenAsync を呼び、トークンは捨てる
    public UniTask ReissueTokenAsync();                   // provider.Invalidate(null) してから GetTokenAsync
    public UniTask<bool> CanReceiveServerErrorAsync();    // 締め切り 15 秒（下の段落）を付けて client.IssueTokenAsync("00000000000000000000000000", "0123456789abcdef") を呼び、RpcException の StatusCode が Unauthenticated なら true。ほかの例外は再スロー、成功したら false
    public void Dispose();                                // GrpcChannel を Dispose する
}
```

`Create` の中身: `ApiChannel.Create(serverUrl)` → `ApiChannel.CreateCallInvoker(channel)` → `DeviceServiceClientFactory.Create` → `new DeviceTokenProvider(client, new EncryptedFileAccountStore(accountFilePath, applicationIdentifier), new ApiCoreOptions(new AppVersion(platform, appVersion)))`。コンストラクターは `private DeviceAuthGateway(GrpcChannel channel, IDeviceService client, DeviceTokenProvider provider)` で、3 つを `private readonly` のフィールドに持つ。

`using`（ファイルごと。実装担当は Unity を動かせないので、ここに無い `using` が要るとわかったら、コンパイルエラーの直しはメインのセッションが行う）:

| ファイル | `using` |
|---|---|
| `DeviceAuthGateway.cs` | `System`、`Cysharp.Threading.Tasks`、`Grpc.Core`、`Grpc.Net.Client`、`VolTech.Client`、`VolTech.Client.Auth`（`AccountStatus`・`DeviceTokenProvider`・`EncryptedFileAccountStore`）、`VolTech.Client.Unity`（`ApiChannel`）、`VolTech.Server.Abstractions`、`Uta.Client.Application.Auth`、`Uta.Client.Infrastructure.Api` |
| `DeviceServiceClientFactory.cs` | `System`、`Grpc.Core`、`MagicOnion.Client`、`MagicOnion.Serialization.MessagePack`、`MessagePack`、`MessagePack.Resolvers`、`VolTech.Server.Abstractions` |
| `UtaMagicOnionClientInitializer.cs` | `MagicOnion.Client`、`VolTech.Server.Abstractions` |

`CanReceiveServerErrorAsync` の呼び出しは、`DeviceTokenProvider` と同じ `client`（`IDeviceService`）に、`new CallOptions(deadline: DateTime.UtcNow.AddSeconds(15))` を付ける（`client.WithOptions(...)`）。**締め切りを付けないと、サーバーを止めた実行（タスク 3 の確認 4）が長くぶら下がる。**

### 3.7 `Uta.Client.Presentation`（名前空間 `Uta.Client.Presentation.Startup`。1 型 1 ファイル）

- `StartupOptions`: `public StartupOptions(bool quitAfterCheck)`、`public bool QuitAfterCheck { get; }`
- `StartupView : MonoBehaviour`（`using UnityEngine;`）: `public void SetText(string value)`。`OnGUI` で `GUI.Label(new Rect(16, 16, Screen.width - 32, Screen.height - 32), text, style)`。`style` は `OnGUI` の中で `new GUIStyle(GUI.skin.label)` から作り、`wordWrap = true`、`fontSize = Mathf.Max(14, Screen.height / 40)`、`normal.textColor = Color.white` にする
- `StartupPresenter : IStartable`（`VContainer.Unity`。`using Cysharp.Threading.Tasks; using UnityEngine; using Uta.Client.Application.Auth;` も付ける）: コンストラクタは `(SignInUseCase signIn, StartupView view, StartupOptions options)`。`public void Start()` は `RunAsync().Forget()`（`private async UniTaskVoid RunAsync()`）を呼ぶだけで、`RunAsync` が次の順に行を足し、足すたびに `Debug.Log("[Uta] " + 行)` と `view.SetText(全行を改行でつないだ文字列)` を行う:
  1. `sign-in: start`
  2. `signIn.ExecuteAsync()` → `sign-in: SignedUp` または `sign-in: SignedIn`
  3. `signIn.ReissueAsync()` → `reissue: ok`
  4. `signIn.CanReceiveServerErrorAsync()` → `server error delivery: ok`（`false` なら `server error delivery: FAILED` と `connectivity check: FAILED server error delivery` の 2 行を足して、失敗として 6 へ）
  5. `connectivity check: OK`
  6. `options.QuitAfterCheck` なら `UnityEngine.Application.Quit(成功なら 0、失敗なら 1)`
  - 例外は、`connectivity check: FAILED {例外の型の完全名}: {Message}` を足して 6 へ（`RunAsync` の全体を `try` で包む）。トークンの値は出さない
  - **`Uta.Client.*` の名前空間の中では、`Application` は `Uta.Client.Application` の名前空間に解決される。UnityEngine の `Application` は `UnityEngine.Application` と完全修飾で書く**（Presentation・Bootstrap のどちらも）

### 3.8 `Uta.Client.Bootstrap`（名前空間 `Uta.Client.Bootstrap`。1 型 1 ファイル）

- `AppInfo`: `public const int Version = 1;`。`public static string Platform` は `#if UNITY_EDITOR` → `AppVersion.Editor`、`#elif UNITY_STANDALONE_WIN` → `AppVersion.Windows`、`#elif UNITY_ANDROID` → `AppVersion.Android`、`#elif UNITY_IOS` → `AppVersion.Ios`、`#else` → `throw new PlatformNotSupportedException()`
- `ServerEndpoint`: `public static string Resolve(string[] commandLineArgs)`（`-uta-server-url` の次の要素があればそれ、なければ `http://localhost:5000`）と、`public static string AccountFileName(string serverUrl)`（`"account-"` ＋ URL の英数字以外を `_` にしたもの ＋ `".dat"`）
- `UtaBootstrap`（`internal static class`）: `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Start()`。`var args = Environment.GetCommandLineArgs();` と `var serverUrl = ServerEndpoint.Resolve(args);` を最初に作り、`new GameObject("StartupView").AddComponent<StartupView>()` を作って `DontDestroyOnLoad` にし、`LifetimeScope.Create(builder => ...)` で次を登録して、その `LifetimeScope` の GameObject も `DontDestroyOnLoad` にする:
  - `RegisterInstance(view)`、`RegisterInstance(new StartupOptions(args に "-uta-quit-after-check" がある))`
  - `Register<IDeviceAuthPort>(_ => (IDeviceAuthPort)DeviceAuthGateway.Create(serverUrl, AppInfo.Platform, AppInfo.Version, Path.Combine(UnityEngine.Application.persistentDataPath, ServerEndpoint.AccountFileName(serverUrl)), UnityEngine.Application.identifier), Lifetime.Singleton)`
  - `Register<SignInUseCase>(Lifetime.Singleton)`
  - `RegisterEntryPoint<StartupPresenter>()`
  - `using`（ファイルごと）: `UtaBootstrap.cs` は `System`、`System.IO`、`UnityEngine`、`VContainer`、`VContainer.Unity`、`Uta.Client.Application.Auth`、`Uta.Client.Infrastructure.Auth`、`Uta.Client.Presentation.Startup`。`AppInfo.cs` は `System`、`VolTech.Client`。`ServerEndpoint.cs` は `System`、`System.Text`（`AccountFileName` で `StringBuilder` を使うなら）

### 3.9 `Uta.Client.Editor`（`Assets/Scripts/Editor/`）

asmdef: `name` は `Uta.Client.Editor`、`includePlatforms` は `["Editor"]`、`references` は空。

```csharp
namespace Uta.Client.Editor
{
    public static class BuildCommands
    {
        /// <summary>Windows の IL2CPP のプレイヤーを Build/Windows/Uta.exe に作る。-batchmode -executeMethod Uta.Client.Editor.BuildCommands.BuildWindowsIl2Cpp で呼ぶ。</summary>
        public static void BuildWindowsIl2Cpp()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = "Build/Windows/Uta.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
```

`BuildCommands` の `using` は `UnityEditor`・`UnityEditor.Build`・`UnityEditor.Build.Reporting`。出力先 `client/uta/Build/` は `.gitignore` 済み（`client/*/[Bb]uild/`）。

## 4. タスク

実装担当が守ること: 計画にない設計判断に出会ったら止まって報告する。**コミットはしない。Unity を起動しない**（Unity のエディターを動かす確認は、メインのセッションかユーザーが行う）。ファイルを書き終えたら、そこで止まる。`client/uta/` 以外で触ってよいのは、2章の `shared/` の行と `.gitignore` だけ。

### タスク 1: パッケージと asmdef（Contracts 以外。手順 5 の前半）

- 触るファイル: 2章のタスク 1 の行
- 手順: 3.1（Domain の分）・3.2（タスク 1 の行）・3.3 のとおりに作る。`Assets/NuGet.config`・`packages.config`・`Plugins/` は Sandbox から写す。`.gitignore` を直す。`Uta.Client.Infrastructure` の asmdef には、`Uta.Shared.Contracts` の参照をまだ書かない
- 境界: **`shared/Uta.Shared.Domain/` に `obj/`・`bin/`（`dotnet build` の生成物。`.gitignore` 済み）が残っていると、Unity が中の `.cs` を拾って型が重複する**。書き始める前に、これらを削除する（メインのセッションが行う）。1a の `shared/Directory.Build.props` が main に入るまで、この作業ツリーでは `shared/` を `dotnet build`・`dotnet test` しない（`obj/`・`bin/` が再び作られる）。入ったら `obj~/`・`bin~/` に出る
- 確かめ方（ファイルを書いたあと、**メインのセッションが**）: Unity をバッチモードで開き（`"C:\Program Files\Unity\Hub\Editor\7000.0.0a12\Editor\Unity.exe" -batchmode -nographics -quit -projectPath C:\Work\Uta\client\uta -logFile <ログ>`。NuGet が復元されなければ `-executeMethod NugetForUnity.NugetHelper.Restore`）、ログに `error CS`・`Assembly has missing references`・パッケージの解決の失敗が無いことを確かめる。通らなければ、ユーザーに Editor での確認を頼む。asmdef の参照名が実際と違えば直す。VContainer の asmdef の `versionDefines` を見て、`VCONTAINER_UNITASK_INTEGRATION` が UniTask の存在で付くかを控える（`unity7-evaluation.md` に書く）。Unity が書いた `ProjectSettings/` の差分は 1章の方針で扱う。Unity の警告は一覧にする

### タスク 2: `com.uta.shared.contracts` の追加と、相対パスの外部パッケージの取り込み（1a は main に入った）

- 前提: `gh pr list` と main で、1a がマージされ、`shared/Uta.Shared.Contracts/` があること。main を取り込む
- 触るファイル: 2章のタスク 2 の行、`Uta.Client.Infrastructure.asmdef`（`Uta.Shared.Contracts` を `references` に足す）
- 手順: 3.1（Contracts の分）・3.4 のとおりに作り、`manifest.json` に `com.uta.shared.contracts` と、3.2 の表の相対パスの 3 つ（UnityScreenNavigator・Supplement・Supplement.MessagePack）を足す。Unity のエディターが `manifest.json` を書き直していた場合（アルファベット順の並べ替え、絶対パスの追加）は、絶対パスの行を 3.2 の相対パスの行に置き換え、並びは Unity のままにする
- 確かめ方: タスク 1 と同じ（メインのセッションのバッチモード）。**ログに「スクリプトが無い asmdef」の警告・参照側のエラーが出るか**を結果に書く。`dotnet build Uta.slnx` と `dotnet test --project shared/Uta.Shared.Domain.Tests` が通る（Unity が `shared/` に作った `.meta` が .NET に影響しないこと）

### タスク 3: サインアップ・サインイン（手順 6）

- 前提: タスク 1・2 の確認が済み、1a が main に入り、ローカルでサーバーが動く（`server/compose.yaml` の PostgreSQL、`dotnet run --project server/Uta.Server -- migrate`、`dotnet run --project server/Uta.Server`）
- 触るファイル: 2章のタスク 3 の行
- 手順: 3.5〜3.9 のとおりに `.cs` を書く。コードの書式は `client/` の `.editorconfig`（C# 9。ファイルスコープ namespace なし、`var`、Allman）。`Uta.Client.Application` の `.cs` で `UnityEngine` を使わない
- 境界: `Corrupted` の分岐は、確認の手順 6 で通す。実装担当は Unity を動かせないので、書き終えたら止まる
- 止まる条件: 次のどれかが起きたら、**メインのセッションが**（Unity を動かして観測する）自分で別の方法を選ばず、ログを添えてユーザーに報告して止まる（基盤の変更が要るかもしれない。基盤の Sandbox は `Assembly-CSharp` の中で確かめており、asmdef の中で、DTO が別アセンブリ（`VolTech.Server.Abstractions`）にある構成は未確認）
  - `[MagicOnionClientGeneration]` の生成が asmdef の中で効かない（`UtaMagicOnionClientInitializer.Resolver` が無い）
  - 生成された `Resolver` が、別アセンブリの DTO（`RegisterResult`・`IssueTokenResult`）の MessagePack のフォーマッタを持たず、実行時に失敗する
  - asmdef の参照名が実際と合わない、`StartupPresenter` の `IStartable` が見つからない、YetAnotherHttpHandler がプレイヤーで動かない
  - **IL2CPP のプレイヤーでだけ起きる失敗**（VContainer のコンストラクター注入が Managed Stripping で消える、`Resolver` が AOT で落ちる、など）。**`link.xml` の追加や Managed Stripping Level の変更は、この計画の範囲（ProjectSettings の意図した変更だけ残す）を超えるので、自分で入れず**、原因の候補を添えて報告し、ユーザーに判断を仰ぐ
- 確かめ方（メインのセッション・ユーザー）:
  1. Unity のバッチモードで `error CS` が無い
  2. **Active Build Target を Windows にしてから**（`ProjectSettings.asset` では Android だけ `applicationIdentifier` が別の値で、Editor の `Application.identifier` は Active Build Target の値を返す。Android のまま登録すると、Windows のプレイヤーは鍵が違って `Corrupted` になり、手順 5 の「壊した」と区別できなくなる）、**実行前に `persistentDataPath` の `account-http___localhost_5000.dat` を消す**（Editor とプレイヤーは同じ `persistentDataPath`（会社名・製品名が同じ）を使うため、どちらかが先に登録すると、もう一方の 1 回目が `SignedIn` になる）。サーバーを起動した状態で、Editor（Play モード）の画面と Console の `[Uta]` の行を見る: 1 回目は `sign-in: SignedUp`、続けて `reissue: ok`・`server error delivery: ok`・`connectivity check: OK`。もう一度 Play すると `sign-in: SignedIn`
  3. `-executeMethod Uta.Client.Editor.BuildCommands.BuildWindowsIl2Cpp -buildTarget Win64` で `client/uta/Build/Windows/Uta.exe` を作る。`Uta.exe -batchmode -nographics -logFile <ログ> -uta-quit-after-check` を、アカウントのファイルを消してから 1 回実行（終了コード 0、`SignedUp`）、もう 1 回実行（`SignedIn`）
  4. サーバーを止めて `Uta.exe` を実行すると `connectivity check: FAILED ...` で終了コード 1
  5. `Corrupted` の確認: 先に、同じ実行の形（Active Build Target・プレイヤー）で `sign-in: SignedIn` になることを 1 回確かめてから、端末のアカウントのファイルを、末尾を削って壊して実行すると、`connectivity check: FAILED System.InvalidOperationException: account corrupted`（終了コード 1）になり、ファイルは書き換わらない。確認したらファイルを消す
  6. サーバーの DB を作り直したときは、端末のアカウントのファイルを消してからやり直す（古い資格情報だと `Unauthenticated` で `FAILED` になる）

### タスク 4: wiki の反映（メインのセッション。確認のあと）

- 触るファイル: 2章のタスク 4 の行
- 手順: 5章の結果（確かめたもの・未確認のもの）を `unity7-evaluation.md` に書く（確認していないものを「通った」と書かない）。8章の要検証は、行ごとに次のとおり直す（丸ごと確かめられた行だけ消し、一部だけ確かめた行は、確かめた範囲と日付を「確かめ方」の列に書き足して残す）:
  - 行「`shared/` を Unity からローカルパッケージ（`file:` 参照）で取り込めるか…」: Windows の IL2CPP と Editor で確かめた範囲を書き足す。Android / iOS の実機ビルドは未確認なので、行は残す
  - 行「MasterMemoryAddons のバイナリを Unity で読めるか」: 触らない
  - 行「MasterMemory・MessagePack・MagicOnion・Supplement.MessagePack のバージョンが揃うか」: MessagePack・MagicOnion が Unity で揃ったことを書き足す。MasterMemory・Supplement は未確認なので、行は残す
  - 行「UnityScreenNavigator・VContainer・UniTask が Unity 7 で動くか」: VContainer・UniTask の分を書き足す。UnityScreenNavigator は未確認なので、行は残す
  - ほかの行: 触らない
  `unity7-evaluation.md` の書式: frontmatter（`title: Unity 7 の検証結果`、`status: draft`、`updated`）、節は「1. 環境」（Unity・OS・日付）、「2. 確かめたもの」（表: 項目・結果・確かめた方法・日付）、「3. 未確認のもの」（5章の未確認の行をそのまま）、「4. 警告・注意」（Unity のログの警告の一覧、`Application.identifier` の注意）。`architecture/README.md` の表に 1 行足す（`index.md` は変えない）。10.6・server-foundation.md・glossary.md を 2章のとおり直す。`linting-wiki` で点検する（`python -I .claude/skills/linting-wiki/lint_wiki.py`）
- 確かめ方: lint がエラー 0

## 5. テスト

自動のテストは書かない（Unity 側のテストは必須にしない。10.4）。確かめることと、確かめる人:

| 確かめること | 方法 | 担当 |
|---|---|---|
| `shared/` の `file:` 取り込み、`dotnet build` 後に型が重複しない | タスク 1・2 の Unity のログ。`dotnet build Uta.slnx` の後に Unity を開く | メインのセッション |
| 各パッケージ（NuGetForUnity・UniTask・VContainer・MagicOnion.Client.Unity・MessagePack.Unity・YetAnotherHttpHandler）が Unity 7 で動く、版が揃う | タスク 1〜3 の Unity のログと、実行結果 | メインのセッション・ユーザー |
| IL2CPP のビルドが通る（`Microsoft.Bcl.AsyncInterfaces`） | タスク 3 の手順 3 | メインのセッション・ユーザー |
| Editor（Play モード）で、サインアップ・サインイン・取り直し・エラーの到達が通る | タスク 3 の手順 2 | ユーザー（メインのセッションが `unity-cli` で試せたものは、そう書く） |
| Windows の IL2CPP のプレイヤーで通る | タスク 3 の手順 3・4 | メインのセッション・ユーザー |
| `ApiCore` 経由の再送、`ApiServiceBase` の IL2CPP、Android / iOS | 範囲外（1章）。**未確認のまま残す** | — |
| エラー詳細のトレーラー（`ErrorInfo`・`grpc-status-details-bin`）が YetAnotherHttpHandler・IL2CPP で届く | この計画の RPC は `ErrorInfo` を付けないので確かめられない。**未確認のまま残す**（業務エラーを返す RPC が出る計画で確かめる） | — |

結果は `architecture/unity7-evaluation.md`（この計画で作る）に、確かめたもの・未確認のものを一覧にして書く。

## 6. 未決事項

なし（`requesting-design-review` に出して `ready` にする）。

## 7. 結果（2026-11-01）

実装は済んだが、完了条件 0 のうち **Windows の IL2CPP が未達**（ビルドが `IL1009` で失敗）なので、`status` は `done` にせず `in-progress` のまま。結果の詳細と確かめ方は [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md)。

| 完了条件 0 の分 | 状態 |
|---|---|
| Editor で、端末の登録（サインアップ）とトークンの発行（サインイン）が通る | 通った（Play モード。`SignedUp` → `SignedIn`、取り直し、サーバーのエラーの到達、保存データの破損の検出） |
| Windows の IL2CPP で通る | **未達**。ビルドが `Microsoft.Bcl.AsyncInterfaces` の `IL1009` で失敗する。基盤の Sandbox でも再現し、Uta の構成が原因ではない（基盤の変更が要る） |

計画から変えたこと:

- UnityScreenNavigator・Supplement・Supplement.MessagePack を、ユーザーの判断で、取り込みまで行った（相対パスの `file:`。1章・3.2）。MasterMemoryAddons は Unity のパッケージが無いので入れていない
- `com.unity.addressables` を 4.1.2 にした（3.2。4.1.1 は Unity 7 a12 でコンパイルエラー）
- NuGetForUnity の復元は、コンパイルエラーがあると走らないので、一時的に 3 パッケージを外して復元させた（`unity7-evaluation.md` 2章）
- `ProjectSettings/` の変更（Standalone の IL2CPP）は、ビルドが通らないので、`develop` に入れていない

残ること（別の計画・基盤の変更）:

- Windows の IL2CPP のビルドを通す（基盤の変更。3.1 のルール）。通ったら、Resolver が DTO のフォーマッタを持たない問題（`unity7-evaluation.md` 3.2）が実際に起きるかを確かめる
- サーバーを止めた実行（タスク 3 の確認 4）
