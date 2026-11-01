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

## 3. 確かめられなかったもの・失敗したもの

### 3.1 Windows の IL2CPP のプレイヤーが、`Microsoft.Bcl.AsyncInterfaces` の `IL1009` でビルドに失敗する

```
ILLink: error IL1009: Assembly reference 'Microsoft.Bcl.AsyncInterfaces' could not be resolved.
ILLink: error IL1040: Type 'System.IAsyncDisposable' reference could not be resolved.
```

VolTech-MagicOnion の導入ガイド（[getting-started.md](../../../external/VolTech-MagicOnion/docs/guide/getting-started.md)「Microsoft.Bcl.AsyncInterfaces を手で置く」）のとおり、`Assets/Plugins/Microsoft.Bcl.AsyncInterfaces/` に DLL を置き、Editor を外した設定にしても、通らなかった。基盤の `docs/index.md` は 2026-10-28 に通ったと書いているが、同じ Editor（`7000.0.0a12`）で再現できなかった。

| 構成 | 経路 | 結果 |
|---|---|---|
| Uta（`develop`） | `BuildPipeline.BuildPlayer`（Standalone を IL2CPP に指定） | 失敗 |
| Uta + `Uta.Client.Infrastructure` の `precompiledReferences` に DLL を明示 | 同上 | 失敗 |
| Uta + `Assembly-CSharp`（asmdef なしの空のスクリプト）を足す | 同上 | 失敗 |
| 基盤の Sandbox（コピー） | 同上 | 失敗 |
| 基盤の Sandbox（コピー） | ビルドプロファイル（`-activeBuildProfile`） | 失敗 |

わかっていること:

- プレイヤー用のコンパイルは、`Microsoft.Bcl.AsyncInterfaces` を、Unity 7 の `Editor/Data/BCLExtensions/TargetingPacks/netstandard2.1/ref/` の参照専用の DLL から取る。`Assets/Plugins/` の DLL は、リンカーの `--include-directory` に入らない
- Editor のログに `Microsoft.Bcl.AsyncInterfaces_AIA`（別名）が出る。同じ名前の衝突を、Unity が別名で扱っているように見える
- この DLL を要するのは、MagicOnion.Client が依存する `Microsoft.Bcl.TimeProvider`（netstandard2.0 版）が、`IAsyncDisposable` を `Microsoft.Bcl.AsyncInterfaces` から参照するため

わからないこと: ビルドに入らない理由、基盤の記録の再現条件。直し方（基盤の変更になる）は未決。案として、`Microsoft.Bcl.TimeProvider` への依存を外す（net10 には `TimeProvider` がある）などがあるが、試していない。

### 3.2 生成された `Resolver` が、別アセンブリの DTO のフォーマッタを持たない

`Uta.Client.Infrastructure` に生成された `Resolver` で、`GetFormatter<RegisterResult>()`・`GetFormatter<IssueTokenResult>()` が `null` を返した（この 2 つの DTO は `VolTech.Server.Abstractions` にある）。Editor では、実行時にコードを生成する `StandardResolver` に頼れるので動く。IL2CPP（実行時のコード生成が使えない）で動くかは、3.1 のビルドが通らないので確かめられていない。基盤の `docs/index.md` の「`Abstractions` の MessagePack の型のフォーマッターが生成されるか」の項目への答えにあたる（生成されなかった）。

### 3.3 そのほか未確認

- **サーバーを止めた実行**で、`connectivity check: FAILED` になること（タスク 3 の確認 4）。まだ実行していない
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
- **`Application.identifier` を変えると、保存済みのアカウントのファイルが読めなくなる（`account corrupted`）**。`EncryptedFileAccountStore` がこの値から鍵を作るため。いまは URP テンプレートの値（`com.Unity-Technologies.com.unity.template.urp-blank`）のまま。変えたら、端末のアカウントのファイル（`persistentDataPath` の `account-<サーバーの URL>.dat`）を消す
- `ProjectSettings.asset` では Android だけ `applicationIdentifier` が別の値で、Editor の `Application.identifier` は Active Build Target の値を返す。Windows のプレイヤーと鍵を合わせるには、Active Build Target を Windows にして登録する
- **起動の流れ**: この計画で作ったのは、[screen-design.md](screen-design.md) 4章の「1. 認証（端末の登録・トークンの発行）」だけ。画面は IMGUI の診断用で、画面の計画（7 番目）で作り直す
- **`unity` CLI（Editor を操作する）の使い方**（動いたもの）: `unity status`（`--until-ready`）、`unity recompile --format json`、`unity command package resolve`、`unity command console get --tail N --level error|warning --format json`、`unity command editor.playmode start|stop`、`unity command eval '<C#>'`（`return` が要る）。`unity command` の名前は、`group name` ではなく、`editor.playmode start` のように `.` でつなぐ形もある（`unity list` で確認する）
- Unity のバッチモードでビルドするときは、Editor が同じプロジェクトを開いていると競合する。別のワークスペース（`../Uta-wt/<名前>`）に worktree を作り、サブモジュールの初期化と `Assets/Packages/` のコピーをしてから回す

## 5. 次にやること

- 3.1 の IL2CPP のビルドを通す（基盤の変更になる。[repository-layout.md](repository-layout.md) 3.1 のルールで、基盤を先に直してポインタを上げる）。通ったら、3.2 の Resolver の問題が実際に起きるかを、プレイヤーで確かめる
- 3.3 のサーバーを止めた実行
