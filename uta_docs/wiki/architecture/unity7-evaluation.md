---
title: Unity 7 の検証結果
status: draft
updated: 2026-11-01
---

# Unity 7 の検証結果

[ADR 0006](../decisions/0006-adopt-unity-7-early.md)（Unity 7 を早期に触る）の検証の記録。最初の実装計画「通信の疎通（クライアント）」（[計画](../../plans/2026-11-01-connectivity-check-client.md)）で確かめたものと、確かめられなかったものを書く。確認していないものを「通った」と書かない。

## 1. 環境

- Unity `7000.0.0a12`（8751f7c83ed7）、Windows 11。プロジェクトは `client/uta/`
- 確認は、Editor が開いた状態で `unity` CLI（`com.unity.pipeline` 経由）から行ったものと、バッチモード（`Unity.exe -batchmode`）で行ったものがある。それぞれ下の表に書く
- サーバーは `dotnet run --project server/Uta.Server`（`http://localhost:5000`、h2c）、PostgreSQL 18 は `server/compose.yaml`

## 2. 確かめたもの

| 項目 | 結果 | 方法 | 日付 |
|---|---|---|---|
| `shared/` の `file:` 取り込み（`com.uta.shared.domain`・`com.uta.shared.contracts`） | 通った。取り込んだ後に `dotnet build Uta.slnx` と Unity の再コンパイルを重ねても、型は重複しなかった（`shared/Directory.Build.props` が出力を `obj~/`・`bin~/` に出す） | Editor（`unity recompile`）とバッチモード | 2026-11-01 |
| 外部パッケージの取り込み（UPM: 基盤・UniTask 2.5.11・VContainer 1.19.0・MagicOnion.Client.Unity 7.11.0・MessagePack.Unity 3.1.12・YetAnotherHttpHandler 1.11.7・NuGetForUnity 4.5.0・UnityScreenNavigator 1.8.0・Supplement 1.5.0・Supplement.MessagePack 1.0.0。NuGet: `Assets/packages.config` の版） | 通った。コンパイルエラー 0。Supplement.MessagePack は Supplement 1.4.0 を求めるが、1.5.0 で通り、版の警告は出なかった | バッチモードと Editor | 2026-11-01 |
| NuGetForUnity の自動復元 | 他のアセンブリにコンパイルエラーがあると、起動時の復元が走らなかった（`Assets/Packages/` ができない）。NuGet の DLL を要する UPM の 3 パッケージを一時的に `manifest.json` から外して開くと復元され、戻して通った | バッチモード | 2026-11-01 |
| Addressables（Supplement の依存） | Supplement は 2.7.6 を求めるが、Unity 7 の a12 は 4.1.1 に解決し、4.1.1 は `ScriptCompilationOptions.DevelopmentBuild` が obsolete で CS0619 のコンパイルエラーになる。**4.1.2 を `manifest.json` で直接指定すると通った**（ユーザーの判断） | Editor（`unity command package resolve`） | 2026-11-01 |
| クライアントの 4 層の asmdef と参照 | 通った。`UniTask`・`VContainer`・`VolTech.Server.Abstractions`・`VolTech.MagicOnion.Client` の参照名は実在の名前と一致。VContainer の asmdef は、UniTask があると `VCONTAINER_UNITASK_INTEGRATION` を自動で定義する（`versionDefines`） | Library/PackageCache の asmdef を確認 | 2026-11-01 |
| `[MagicOnionClientGeneration]` が asmdef の中で効くか | 効いた。`Uta.Client.Infrastructure` に `Resolver`・`ClientFactoryProvider` などが生成される | Editor（`unity command eval` で型のメンバーを列挙） | 2026-11-01 |
| Editor（Play モード）で、ローカルのサーバーに対するサインアップ・サインイン | 通った。アカウントのファイルが無い 1 回目は `SignedUp`、2 回目は `SignedIn`。トークンの取り直し（`reissue`）と、存在しない端末での `IssueTokenAsync` が `Unauthenticated` で返ること（`server error delivery`）も通った | Editor の Play、コンソールの `[Uta]` の行 | 2026-11-01 |
| 保存したアカウントのファイルが壊れたとき | `connectivity check: FAILED System.InvalidOperationException: account corrupted`。ファイルは書き換わらない | 末尾 5 バイトを削って Play | 2026-11-01 |
| Windows の IL2CPP のプレイヤーのビルド | 通った（3.1 の `.meta` の直しが要る）。基盤の Sandbox と Uta の `Uta.exe` | バッチモード（`BuildPipeline.BuildPlayer`） | 2026-11-01 |
| Windows の IL2CPP のプレイヤーから、ローカルのサーバーに対するサインアップ・サインイン | 通った。アカウントのファイルが無い 1 回目は `SignedUp`、2 回目は `SignedIn`、`reissue`・`server error delivery` も `ok`。サーバーに繋がらないとき（存在しないポート）は `connectivity check: FAILED`（`Unavailable`）で終了コード 1 | `Uta.exe -batchmode -nographics -uta-quit-after-check`（`-uta-server-url` でサーバーを変えた） | 2026-11-01 |

## 3. 確かめられなかったもの・失敗したもの

### 3.1 Windows の IL2CPP のプレイヤーが `Microsoft.Bcl.AsyncInterfaces` の `IL1009` で失敗した（解決済み。2026-11-01）

```
ILLink: error IL1009: Assembly reference 'Microsoft.Bcl.AsyncInterfaces' could not be resolved.
ILLink: error IL1040: Type 'System.IAsyncDisposable' reference could not be resolved.
```

**原因**: `Assets/Plugins/Microsoft.Bcl.AsyncInterfaces/Microsoft.Bcl.AsyncInterfaces.dll` の `.meta` が、`platformData` の `Any` の `settings` に `Exclude Editor: 1` を書く古い形式だった（基盤のガイドの見本）。Unity 7（`7000.0.0a12`）はこの形式だと、DLL をプレイヤーのコンパイルにもリンカーにも渡さない。失敗したビルドのリンカーの rsp には、`Assets/Packages/` の NuGet の DLL は `--reference`・`--include-directory` で渡されていたが、この DLL だけが無かった。

**直し方**: `.meta` を `Any` 有効（`settings: {}`）・`Editor` 無効（NuGetForUnity が作る DLL の `.meta` と同じ形）にする。基盤の Sandbox のコピーで、Windows の IL2CPP のビルドが通った。基盤の導入ガイドに `.meta` の形を書いた（基盤の `fix/bcl-asyncinterfaces-meta`、コミット 41ed58c）。Uta でも同じ形にし、`Uta.exe` を作れた。

調べて原因ではなかったもの: Unity の参照専用の DLL（`TargetingPacks/netstandard2.1/ref/`）の優先、`precompiledReferences` への明示、`Assembly-CSharp` の追加、ビルドプロファイル。Editor のログの `Microsoft.Bcl.AsyncInterfaces_AIA` は、`com.unity.ai.assistant` パッケージが付けたエディター用の別名で、関係ない。

基盤の `docs/index.md` が 2026-10-28 に「通った」と書いていた条件は、再現できなかった（そのときの `.meta` の形は未確認）。

なぜ古い形式で渡されなくなるのかは、Unity の内部を見ていないので推測。a12 の挙動で、ほかの版では変わるかもしれない。

### 3.2 生成された `Resolver` が、別アセンブリの DTO のフォーマッタを持たない

`Uta.Client.Infrastructure` に生成された `Resolver` で、`GetFormatter<RegisterResult>()`・`GetFormatter<IssueTokenResult>()` が `null` を返した（この 2 つの DTO は `VolTech.Server.Abstractions` にある）。基盤の `docs/index.md` の「`Abstractions` の MessagePack の型のフォーマッターが生成されるか」の項目への答えにあたる（生成されなかった）。

**Windows の IL2CPP のプレイヤーでは、失敗しなかった**（2026-11-01）。`Uta.exe` で、サインアップ（`SignedUp`）・サインイン（`SignedIn`）・トークンの取り直し・`Unauthenticated` のエラーの受け取りが通った。つまり `RegisterResult`・`IssueTokenResult` の送受信は動いた。生成された `Resolver` が `null` を返しても、組み合わせた別の解決先（`StandardResolver` の側）から取れたとみられるが、どのフォーマッタが使われたかは調べていない（推測）。iOS（AOT）は確かめていない。

### 3.3 そのほか未確認

- **`ApiCore` 経由の再送**（無効・期限切れのトークンを 1 回だけ取り直して再送する）と、`ApiServiceBase` が属性をリフレクションで読む部分の IL2CPP。範囲外
- **エラー詳細のトレーラー**（`ErrorInfo`・`grpc-status-details-bin`）が YetAnotherHttpHandler・IL2CPP で届くか。この計画の RPC は `ErrorInfo` を付けない
- **Android / iOS** の実機ビルド
- **MasterMemory・MasterMemoryAddons**（Unity のパッケージ `package.json`・asmdef が無く、入れ方は [masterdata-pipeline.md](master/masterdata-pipeline.md) の未決。今回は入れていない）
- UnityScreenNavigator・Supplement は取り込みだけで、使っていない

## 4. 警告・注意

- Unity のログの警告（エラーではない）:
  - `UAL0010`・`UAL0013`: 静的フィールドを持つ型に `AutoStaticsCleanup` などの属性が要る（`shared/Uta.Shared.Domain` の `CardDefinition`、Supplement・UnityScreenNavigator の複数の型）
  - `CS8632`: MagicOnion.Client.Unity の nullable の注釈
  - 基盤のテスト用の asmdef（`Tests/EditMode`）は、`Assets/` の外にあるのでコンパイルされない
  - `com.unity.ai.assistant`（もとから入っているパッケージ）が `NullReferenceException` を出す（`SessionSlice.cs`）。取り込みとは関係ない
  - `CrashReporter::UploadInsightsStackTraceContent should only be called from the main thread`。Editor 内部のもの
- **`Application.identifier` を変えると、保存済みのアカウントのファイルが読めなくなる（`account corrupted`）**。`EncryptedFileAccountStore` がこの値から鍵を作るため。Windows のプレイヤーでは `Application.identifier` が空になり、起動で `Key source must not be null or empty` になったので、Uta は鍵の元を固定の値 `AppInfo.AccountKeySource`（`Uta.IdolMaster.Hommage`）にした（ユーザーの判断。2026-11-01）。変えたら、端末のアカウントのファイル（`persistentDataPath` の `account-<サーバーの URL>.dat`）を消す
- `ProjectSettings.asset` の `applicationIdentifier` は、もう鍵に使わない（上）
- **起動の流れ**: この計画で作ったのは、[screen-design.md](screen-design.md) 4章の「1. 認証（端末の登録・トークンの発行）」だけ。画面は IMGUI の診断用で、画面の計画（7 番目）で作り直す
- **`unity` CLI（Editor を操作する）の使い方**（動いたもの）: `unity status`（`--until-ready`）、`unity recompile --format json`、`unity command package resolve`、`unity command console get --tail N --level error|warning --format json`、`unity command editor.playmode start|stop`、`unity command eval '<C#>'`（`return` が要る）。`unity command` の名前は、`group name` ではなく、`editor.playmode start` のように `.` でつなぐ形もある（`unity list` で確認する）
- Unity のバッチモードでビルドするときは、Editor が同じプロジェクトを開いていると競合する。別のワークスペース（`../Uta-wt/<名前>`）に worktree を作り、サブモジュールの初期化と `Assets/Packages/` のコピーをしてから回す

## 5. 次にやること

- Android / iOS の実機ビルド（3.3）
- 3.3 の `ApiCore` の再送・エラー詳細のトレーラーの IL2CPP の確認
