---
title: 実装計画: マスタを Unity に取り込む（クライアント用の出力・MasterMemory の導入・StreamingAssets からの読み込み）
status: draft
updated: 2026-11-01
spec: ../wiki/architecture/master/masterdata-pipeline.md, ../wiki/architecture/repository-layout.md
---

# 実装計画: マスタを Unity に取り込む

[1/2](2026-11-01-masterdata-first.md)（done）はクライアント用の出力を作らなかった（Unity に MasterMemory が入っておらず、生成ソースを置くとコンパイルできないため）。この計画で、Unity 7 に MasterMemory と `MasterMemory.Addons.Core` を入れ、`masterdata.yaml` にクライアント用の出力先を足し、StreamingAssets のバイナリを読んで `MemoryDatabase` を作るところまで進める。

**2/2（[2026-11-01-masterdata-rest.md](2026-11-01-masterdata-rest.md)）がマージされてから実装する。生成するのは、2/2 の全テーブル（1/2 の 5 + 2/2 の 26 = 31）を含む定義。**

## 1. 目的と前提

- 仕様の正: 出力先と暗号化は [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 5・6章、Unity に入るソースの制約は同 2章、層と置き場は [repository-layout.md](../wiki/architecture/repository-layout.md) 3・3.1・5章
- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0013](../wiki/decisions/0013-commit-masterdata-binary.md)（バイナリもコミット）、[0005](../wiki/decisions/0005-foundation-submodule.md)（サブモジュール）、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)、[0009](../wiki/decisions/0009-client-stack.md)
- 前提:
  - 2/2 がマージされ、`masterdata/Definitions`・`masterdata/csv` が 31 テーブルそろっている。`powershell -NoProfile -File masterdata/gen.ps1 check` が通る
  - Unity 7（`7000.0.0a12`）の Editor を開ける環境で実施する（コンパイル確認に `unity recompile` を使う）。Editor が同じプロジェクトを開いているときにバッチで回すと競合する（[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章）
  - `external/MasterMemoryAddons` が初期化済み（`git submodule update --init external/MasterMemoryAddons`）
- **この計画で決めないこと**（範囲外）:
  - マスタ → `Uta.Shared.Domain` の型への変換（`ICardCatalog` の実装など）、DI への登録、画面からの利用。使う計画で作る
  - Addressables への移行（[masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 6章。いまは同梱）
  - `[ClientOnly]` の列（表示名・アイコン。まだ定義がない）
  - IL2CPP・Android・iOS の実機での読み込み（5章「確かめられないこと」。Windows の IL2CPP ビルドは `IL1009` で通らない: [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.1）
  - CI への組み込み

## 2. 調べて確かめたこと（2026-11-01）

記録は [raw/2026-11-01-masterdata-unity-spike.md](../raw/2026-11-01-masterdata-unity-spike.md)。アドオンのツールを実際に動かして確かめた（1/2 の定義と CSV を使い、出力先を作業用の場所に向けた。リポジトリには何も書いていない）。

| 項目 | 結果 |
|---|---|
| クライアント用の出力（`target: client`、`encrypt: true`）で `master-gen` | 通る。ツールは生成ソースを netstandard2.1・C# 9 でコンパイルして検証するので、**1/2 の定義は Unity の制約を満たしている**。出力は `Common.cs`・`Lesson.cs`・`MasterDataSchema.cs`。生成ソースの `using` は `System`・`System.Collections.Generic`・`MasterMemory`・`MessagePack` だけで、`MasterMemory.Addons.Attributes` は要らない（`[Exists]` などの検証の属性は生成ソースに残らない） |
| マニフェスト | バイナリの隣に `masterdata.manifest.json`（`formatVersion`・`schemaId`・`dataHash`・`encrypted`・`size`・`createdAt`）が出る。`schemaId` は外に出してよい値 |
| `[Exists]` が別の側の表を参照する場合（未決事項） | **問題ない**。`[Exists]` を持つ Common の表が `[ServerOnly]` の表を指す定義でも、`master-gen` は検証をデータ全体に対して行い、クライアントの出力では `[ServerOnly]` の表と `[Exists]` を落として生成する（終了コード 0）。ServerOnly → Common の向きは、アドオンのサンプル（`DropTable` → `Item`）にある。Uta の定義の `EvaluationRankReward.Rank` → `EvaluationRank`（ServerOnly → Common）も同じ向き |
| `AesMasterDataCipher` | PBKDF2 の反復は 1000 回。読み込みの時間はほぼ増えない。鍵はスキーマのハッシュ（アプリの中にある）なので、難読化の範囲 |
| MasterMemory 3.0.4 の NuGet | `lib/netstandard2.0/MasterMemory.dll` と `analyzers/dotnet/cs/MasterMemory.SourceGenerator.dll`（Source Generator）を持つ。依存は `MasterMemory.Annotations` 3.0.4 と `MessagePack` ≥ 3.1.3。Uta の MessagePack は 3.1.12 なので満たす |
| Unity での Source Generator | **未確認**。NuGetForUnity の `analyzers` 経由の Source Generator が Unity 7 で動くかは、タスク 1 で最初に確かめる。参考: MagicOnion の Source Generator（`[MagicOnionClientGeneration]`）は Unity 7 で `Uta.Client.Infrastructure` に `Resolver` を生成している（[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.2。UPM 経由）。`packages.config` の `MessagePackAnalyzer` は NuGetForUnity 経由だが、動作の記録は無い |
| `MasterMemory.Addons.Core` の Unity での扱い | `netstandard2.1`・名前空間 `MasterMemory.Addons`・参照は BCL のみ（`System.Buffers.Binary`・`System.Security.Cryptography`）。Unity 側に asmdef・`package.json` が無い。`#nullable` なしの `?` は Unity で警告 CS8632 になる（エラーではない。MagicOnion.Client.Unity でも出ている） |

## 3. この計画で決めたこと

ユーザーの確認が要るもの（`**要確認**`）は 7章に並べた。確認が済むまで `status: draft`。

| 項目 | 決めたこと | 理由 |
|---|---|---|
| 出力先 | `masterdata.yaml` に `unity`（`target: client`）を足す。`source` は `../client/uta/Assets/Scripts/Infrastructure/MasterData/Generated`、`binary` は `../client/uta/Assets/StreamingAssets/MasterData/masterdata.bytes`、`namespace` は `Uta.MasterData` | masterdata-pipeline.md 5章の案。生成物は `Uta.Client.Infrastructure`（asmdef）の中に入る |
| 暗号化 | `encrypt: true`（[ADR 0007](../wiki/decisions/0007-masterdata-with-addons.md) の決定: クライアント用バイナリは暗号化する）。読み込みのコードは暗号化の有無に関わらず動く（`MasterDataBinary.Read` がヘッダで判定する）。`false` にするには ADR の見直しが要る | 読み込みの時間はほぼ増えない（PBKDF2 は 1000 回）。難読化の範囲（鍵はアプリの中） |
| `MemoryDatabase` の名前空間 | `Assets/Scripts/Infrastructure/MasterData/MasterDataAssembly.cs` に `[assembly: MasterMemory.MasterMemoryGeneratorOptions(Namespace = "Uta.MasterData")]` を置く | サーバーと同じ（1/2 の決定）。`Generated/` は生成のたびに丸ごと入れ替わるので、手書きのファイルは `Generated/` の外に置く |
| `IsExternalInit` | `Assets/Scripts/Infrastructure/MasterData/IsExternalInit.cs` に `internal static class IsExternalInit`（名前空間 `System.Runtime.CompilerServices`）を置く | repository-layout.md 5章の決定。アドオンの `samples/Client/IsExternalInit.cs` と同じ内容 |
| MasterMemory の入れ方 | NuGetForUnity（`Assets/packages.config` に `MasterMemory` と `MasterMemory.Annotations` を 3.0.4 で足す） | repository-layout.md 3章「NuGet のパッケージは NuGetForUnity で入れる」。MasterMemory の docs・アドオンの docs も同じ（NuGetForUnity で導入） |
| `MasterMemory.Addons.Core` の入れ方 | **要確認**: アドオンの `src/MasterMemory.Addons.Core/` に `package.json` と asmdef を置いて Unity のパッケージにし（`shared/Uta.Shared.Domain` と同じ形。ソースは移さない）、Uta は `manifest.json` から `file:` で参照する（推奨） | 7章 1。repository-layout.md 3.1 のルール 2（別のゲームでも同じものが要る）に沿う |
| `Uta.Client.Infrastructure` の asmdef | `references` に `MasterMemory.Addons.Core` を足す。MasterMemory・MessagePack の DLL は `overrideReferences: false` のまま自動で参照される（`MessagePack` は既にそうしている） | 最小の変更 |
| 読み込みのコード | `Assets/Scripts/Infrastructure/MasterDataLoader.cs`（名前空間 `Uta.Client.Infrastructure`）。サーバーの `MasterDataLoader` と同じ形: `Load(byte[])` と、StreamingAssets を読む `LoadFromStreamingAssetsAsync` | 5章タスク 3。サーバーとの対称。生成物（`Uta.MasterData`）を参照してよいのは Infrastructure だけ（repository-layout.md 3.1） |
| StreamingAssets の読み方 | **全プラットフォームで `UnityWebRequest`**（UniTask の `WithCancellation`）。パスは `Application.streamingAssetsPath + "/MasterData/masterdata.bytes"`。`"://"` を含まなければ（Windows・iOS・Editor）`new Uri(path).AbsoluteUri`（`file:///…`）にする。Android の `jar:file://…` はそのまま | Android では `File` の API で読めない。1 本の経路にして、プラットフォーム分岐を避ける。**Android での読み込みは未確認**（5章） |
| 失敗 | `MasterDataFormatException` は `InvalidOperationException("マスタのバイナリを読めない（<Reason>）")` に包む（サーバーと同じ。スキーマのハッシュを含めない）。`UnityWebRequest` の失敗は UniTask が投げる `UnityWebRequestException` のまま | 鍵をログに出さない（masterdata-pipeline.md 6章） |
| 呼び出し側 | この計画では DI に登録しない・誰も呼ばない（テストだけが呼ぶ） | 範囲外（使う計画で作る） |
| マニフェスト | `masterdata.manifest.json` も StreamingAssets に出て同梱される（`schemaId`・`dataHash` だけで、外に出してよい値）。除外しない | ツールの出力先がバイナリと同じフォルダに固定。害がない |
| クライアントのテスト | **要確認**: Unity Test Framework の EditMode テストを足す（推奨）。タスク 3 で `Packages/manifest.json` に `com.unity.test-framework` 1.9.0 を足す（Unity 7 に組み込みで、`packages-lock.json` に間接依存として既に入っている） | 7章 2 |
| 生成の運用 | クライアント用の出力はバイナリが毎回変わる（暗号化のソルト・IV と `createdAt`）。**CSV か定義を変えたときだけ `master-gen` を実行する**。実行すると `server` の出力も書き直されるので、サーバーのバイナリの `dataHash` が同じなら、`git checkout -- server/Uta.Server/Infrastructure/MasterData/masterdata.manifest.json` で `createdAt` だけの差分を戻す | ADR 0013 |

## 4. 触るファイル

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `client/uta/Assets/packages.config` | `MasterMemory`・`MasterMemory.Annotations` 3.0.4 | 1 |
| 変更 | `client/uta/Packages/packages-lock.json` | Unity が更新する（手で書かない） | 1・2・3 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/MasterData/MasterDataAssembly.cs` | 4.2 | 1 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/MasterData/IsExternalInit.cs` | 4.2 | 1 |
| 変更 | `masterdata/masterdata.yaml` | `unity` の出力先（4.3） | 1 |
| 作成（生成） | `client/uta/Assets/Scripts/Infrastructure/MasterData/Generated/*.cs`（2/2 の定義ファイルに対応する分と `MasterDataSchema.cs`） | `gen.ps1 master-gen` が作る。手で書かない・直さない | 1 |
| 作成（生成） | `client/uta/Assets/StreamingAssets/MasterData/masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1 master-gen` が作る | 1 |
| 作成 | 上の新規ファイルと `MasterData`・`StreamingAssets` フォルダの `.meta` | Unity が作る。コミットする | 1・2・3 |
| 作成 | `external/MasterMemoryAddons/src/MasterMemory.Addons.Core/package.json`・`MasterMemory.Addons.Core.asmdef` と `.meta` | UPM パッケージの定義（4.1） | 2 |
| 変更 | `external/MasterMemoryAddons/src/MasterMemory.Addons.Core/MasterMemory.Addons.Core.csproj` | 出力を `bin~/`・`obj~/` にする（4.1） | 2 |
| 変更 | `external/MasterMemoryAddons/.gitignore`・`docs/dev/structure.md`・`docs/guide/build-and-distribute.md`・`docs/design/decisions.md`・`docs/log.md` | 4.1 | 2 |
| 変更 | `external/MasterMemoryAddons`（サブモジュールのポインタ） | タスク 2 のコミットに上げる | 2 |
| 変更 | `client/uta/Packages/manifest.json` | `com.chinpangx.mastermemory.addons`（`file:`）・`com.unity.test-framework` | 2・3 |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Uta.Client.Infrastructure.asmdef` | `references` に `MasterMemory.Addons.Core` | 2 |
| 作成 | `client/uta/Assets/Scripts/Infrastructure/MasterDataLoader.cs` と `.meta` | 4.4 | 3 |
| 作成 | `client/uta/Assets/Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef`・`MasterDataLoaderTests.cs` と `.meta`（`Tests`・`EditMode` フォルダの `.meta` も） | 4.5・6章 | 3 |
| 変更 | `uta_docs/wiki/`・`CLAUDE.md` | 実装で決めたこと（メインのセッション） | 4 |

Uta の `.gitignore` は変えない（`Assets/Packages/` は既に除外済み。`StreamingAssets` のバイナリはコミットする: ADR 0013）。`.gitattributes` の `*.bytes binary` は既にある。

### 4.1 アドオンの Unity パッケージ（タスク 2）

`Uta.Shared.Domain`（`shared/Uta.Shared.Domain`）と同じ形にする: **csproj のあるフォルダに `package.json` と asmdef を直接置き、ビルドの出力を `bin~/`・`obj~/`（Unity が無視するフォルダ）にする**。ソースは移さない。

`src/MasterMemory.Addons.Core/package.json`:

```json
{
  "name": "com.chinpangx.mastermemory.addons",
  "version": "0.1.0",
  "displayName": "MasterMemory Addons",
  "description": "MasterMemory のバイナリ(ヘッダ・暗号化)を読む実行時ライブラリ(MasterMemory.Addons.Core)。",
  "unity": "7000.0"
}
```

`src/MasterMemory.Addons.Core/MasterMemory.Addons.Core.asmdef`（アセンブリ名は csproj の `AssemblyName` と同じ）:

```json
{
    "name": "MasterMemory.Addons.Core",
    "rootNamespace": "MasterMemory.Addons",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`MasterMemory.Addons.Core.csproj` の `PropertyGroup` に足す（`shared/Directory.Build.props` と同じ値）:

```xml
<BaseOutputPath>bin~/</BaseOutputPath>
<BaseIntermediateOutputPath>obj~/</BaseIntermediateOutputPath>
```

- アドオンの `.gitignore` に `bin~/`・`obj~/` を足す（既存の `bin/`・`obj/` の行の隣）。既にある `src/MasterMemory.Addons.Core/bin`・`obj` は消してよい（ビルドの生成物）
- 別の csproj（`Tools`・テスト）から Core を `ProjectReference` で参照している形は変えない。ビルドの出力先が変わるだけ
- アドオンの `CLAUDE.md` のコマンド: `dotnet build MasterMemory.Addons.slnx`（警告 0・エラー 0）、`dotnet test MasterMemory.Addons.slnx`（すべて通る）。変更の前後で結果が変わらないこと
- docs: `docs/log.md` の先頭（「新しいものを上に追記」）に `- 2026-11-01 [dev/structure / build-and-distribute / decisions] Core を Unity のパッケージ(com.chinpangx.mastermemory.addons)としても使えるようにした(package.json・asmdef を Core のフォルダに置く)` の形で 1 行足す。`docs/design/decisions.md` の決定の表に同じ日付の行を足す（内容: 「Unity 専用のアセンブリは作らない」(2026-10-25)は変えない。Core のソースはそのままで、Core のフォルダに Unity のパッケージの定義を置く）。`docs/dev/structure.md` の「Unity のプロジェクトは Core と Attributes を直接参照する」を「Core は `file:` で参照できる(`package.json`・asmdef)。Attributes は Unity では要らない」に直す。`docs/guide/build-and-distribute.md` の「Unity」の項に、Core の入れ方（`manifest.json` に `"com.chinpangx.mastermemory.addons": "file:<相対パス>/src/MasterMemory.Addons.Core"`）を足す。CHANGELOG はアドオンに無い
- `.meta`: `file:` のローカルパッケージは、Unity が開いたときにパッケージ側にも `.meta` を作る（`shared/Uta.Shared.Domain` に `.meta` がコミットされているのと同じ）。**アドオンの `.meta` は、Uta のプロジェクトを Editor で開いて作らせてから、アドオン側にコミットする**（タスク 2 の手順）

### 4.2 Uta 側の手書きファイル（タスク 1・2）

`MasterDataAssembly.cs`（BOM 付き UTF-8、LF）:

```csharp
[assembly: MasterMemory.MasterMemoryGeneratorOptions(Namespace = "Uta.MasterData")]
```

`IsExternalInit.cs`:

```csharp
// init アクセサーを netstandard2.1 / Unity で使うための polyfill（マスタの生成ソースが使う）。
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
```

`Assets/packages.config` に足す行:

```xml
  <package id="MasterMemory" version="3.0.4" manuallyInstalled="true" />
  <package id="MasterMemory.Annotations" version="3.0.4" manuallyInstalled="true" />
```

`manifest.json` の `dependencies` に足す行（`file:` の相対パスは `client/uta/Packages/` から）:

```json
"com.chinpangx.mastermemory.addons": "file:../../../external/MasterMemoryAddons/src/MasterMemory.Addons.Core",
"com.unity.test-framework": "1.9.0",
```

### 4.3 `masterdata.yaml`（タスク 1）

`outputs` に足す（`server` の出力先はそのまま）:

```yaml
  - name: unity
    target: client
    source: ../client/uta/Assets/Scripts/Infrastructure/MasterData/Generated
    namespace: Uta.MasterData
    binary: ../client/uta/Assets/StreamingAssets/MasterData/masterdata.bytes
    encrypt: true
```

### 4.4 `MasterDataLoader`（タスク 3）

C# 9 の範囲（ブロック形式の namespace。`client/` の `.editorconfig`）。

```csharp
namespace Uta.Client.Infrastructure
{
    /// <summary>StreamingAssets のマスタのバイナリ（masterdata.bytes）を読む。</summary>
    public static class MasterDataLoader
    {
        public const string RelativePath = "MasterData/masterdata.bytes";

        public static MemoryDatabase Load(byte[] bytes);

        public static UniTask<MemoryDatabase> LoadFromStreamingAssetsAsync(CancellationToken cancellationToken = default);
    }
}
```

- `using` は `System`・`System.Threading`・`Cysharp.Threading.Tasks`・`MasterMemory.Addons`・`UnityEngine.Networking`・`Uta.MasterData`。`UnityEngine.Application` は `Uta.Client.Application` の名前空間と紛れるので、**`UnityEngine.Application.streamingAssetsPath` と完全修飾で書く**（`UtaBootstrap.cs` と同じ）
- `Load(byte[] bytes)`: `new MemoryDatabase(MasterDataBinary.Read(bytes, MasterDataSchema.Hash))`。`MasterDataFormatException` を捕まえて `throw new InvalidOperationException($"マスタのバイナリを読めない（{exception.Reason}）", exception)`。`bytes` が `null` なら `ArgumentNullException`（`MasterDataBinary.Read` が投げるものをそのまま通す）
- `LoadFromStreamingAssetsAsync`:
  1. `var path = UnityEngine.Application.streamingAssetsPath + "/" + RelativePath;`
  2. `var uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;`
  3. `using (var request = UnityWebRequest.Get(uri))`
  4. `await request.SendWebRequest().WithCancellation(cancellationToken);`（失敗は `UnityWebRequestException`。握りつぶさない）
  5. `return Load(request.downloadHandler.data);`

### 4.5 テスト用 asmdef（タスク 3）

`Uta.Client.Infrastructure.Tests.EditMode.asmdef`:

```json
{
    "name": "Uta.Client.Infrastructure.Tests.EditMode",
    "rootNamespace": "Uta.Client.Infrastructure.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Uta.Client.Infrastructure",
        "UniTask"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "MasterMemory.dll",
        "MasterMemory.Annotations.dll",
        "MessagePack.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`MasterMemory.dll` などの参照が足りずに CS0012 が出たら、**エラーメッセージが名指しする DLL を `precompiledReferences` に足す**（これ以外の変更はしない。足した DLL は 6章で報告する）。

## 5. タスク

各タスクが終わったとき、`dotnet build Uta.slnx` が通り、`unity recompile --format json` のエラーが 0 になる状態にする。順序は、**一番の不確定（Source Generator が Unity 7 で動くか）を先に確かめる**ため、基盤の変更の前に MasterMemory の導入と生成を置く。

### タスク 1: MasterMemory を入れ、クライアント用の出力を足して生成する

- 触る: 4章の表のタスク 1 の行
- 手順:
  1. `packages.config` に MasterMemory・MasterMemory.Annotations を足し、`MasterDataAssembly.cs`・`IsExternalInit.cs` を 4.2 のとおり作る
  2. Editor でプロジェクトを開く（NuGetForUnity が `Assets/Packages/` を復元する。復元されなければメニューの NuGet の Restore Packages）。`unity command package resolve`、`unity recompile --format json`。エラー 0 を確認する
  3. `masterdata.yaml` に 4.3 の出力先を足す
  4. `powershell -NoProfile -File masterdata/gen.ps1 master-gen`（実行ポリシーで止まる環境では `-ExecutionPolicy Bypass` を足す）。ツールが出す「テーブル」の数が、サーバーの出力の数から `[ServerOnly]` の表（`EvaluationRankReward`）を引いた数であることを確認する
  5. サーバーの出力を確認する: `git diff --stat server/` で `masterdata.bytes` が変わっていなければ（`dataHash` が同じ）、`git checkout -- server/Uta.Server/Infrastructure/MasterData/masterdata.manifest.json` で `createdAt` だけの差分を戻す。`masterdata.bytes` や `Generated/` が変わっていたら、**止まって報告する**（2/2 のコミット後に CSV か定義が変わっている）
  6. Editor に戻り `unity recompile --format json`
  7. `.meta`（`MasterData/`・`Generated/`・`StreamingAssets/`・バイナリ・マニフェスト）を含めてコミットする
  8. `powershell -NoProfile -File masterdata/gen.ps1 check` が終了コード 0
- 境界: クライアントの出力に `EvaluationRankReward` が含まれないこと（`Generated/` にその文字列がない）
- 確かめ方: `unity recompile` のエラーが 0。`unity command eval 'return typeof(Uta.MasterData.MemoryDatabase).FullName;'` が `Uta.MasterData.MemoryDatabase` を返す（Source Generator が動いた証拠）。**動かなければ止まって報告する**（6章）

### タスク 2: アドオンの Unity パッケージ（基盤の変更。repository-layout.md 3.1 のルール 4・6）

- 触る: 4章の表のタスク 2 の行
- 手順:
  1. `external/MasterMemoryAddons` の中で `git switch -c unity-package` し、4.1 のとおり変更する。`dotnet build MasterMemory.Addons.slnx`・`dotnet test MasterMemory.Addons.slnx` を流す
  2. アドオンをコミットし、**push する**（ブランチのまま。`main` に取り込むかは 7章 3）。Uta のポインタをそのコミットに上げる（`git add external/MasterMemoryAddons`）
  3. Uta の `manifest.json` に `com.chinpangx.mastermemory.addons` を足し、`Uta.Client.Infrastructure.asmdef` の `references` に `MasterMemory.Addons.Core` を足す（4.2）
  4. Editor で開き直し（または `unity command package resolve`）、`unity recompile --format json` でエラー 0 を確認する。Unity がアドオンのフォルダに作った `.meta` を、アドオンの側にコミットして**もう一度 push する**。Uta のポインタをその新しいコミットに上げる
- 境界: アドオンの `Tools` と `tests` のビルドとテストの結果が、変更の前と同じ
- 確かめ方: アドオンの build・test が通る。`dotnet build Uta.slnx`、`dotnet test --project server/Uta.Server.Tests` が通る（サーバーは Core を `ProjectReference` で参照している）。`unity recompile` のエラーが 0。`unity command eval 'return typeof(MasterMemory.Addons.MasterDataBinary).FullName;'`

### タスク 3: `MasterDataLoader` とテスト

- 触る: 4章の表のタスク 3 の行
- 手順:
  1. `manifest.json` に `com.unity.test-framework` 1.9.0 を足す。テスト用 asmdef（4.5）を作る
  2. `MasterDataLoader.cs` を 4.4 のとおりに書く
  3. `MasterDataLoaderTests.cs` を 6章のとおりに書く
  4. `unity recompile --format json` でエラー 0
  5. テストを実行する。`unity` CLI にテストを回すコマンドがあればそれ（`unity list` で確かめる）。無ければ**ユーザーに「Editor の Test Runner → EditMode → Run All を実行して結果を教えてほしい」と依頼する**。結果を見ずに「通った」と書かない
- 確かめ方: 6章のテストがすべて通る

### タスク 4: wiki への反映（メインのセッション）

- 実装で決めたことを masterdata-pipeline.md（1章の図、5章の `unity`、未決事項、8章の「Unity での読み込みの確認」、実装メモ）・repository-layout.md（2章のツリー、3章の「MasterMemory の入れ方」、8章の要検証の行）・master/README.md・unity7-evaluation.md 3.3・game-spec.md の計画の順に書く。計画を `status: done` にする

## 6. テスト

`MasterDataLoaderTests`（`Uta.Client.Infrastructure.Tests` の名前空間。NUnit。`using NUnit.Framework; using UnityEngine.TestTools; using Cysharp.Threading.Tasks;`）。

| テスト名 | 内容 |
|---|---|
| `Load_Bundled_ReturnsDatabase` | `File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath)` を `Load` に渡し、**サーバーのテスト `server/Uta.Server.Tests/MasterData/MasterDataTests.cs` にある `Assert.Equal(n, Db.XxxTable.Count)` の検査をすべて、同じ値で写す**（実装する時点の 2/2 後のファイルから写す。1/2 の時点では `CardTable` 36・`CardFaceTable` 72・`CardCostTable` 72・`EffectTable` 129・`ConditionTable` 14）。`EvaluationRankReward` の検査は、クライアントに表がないので写さない |
| `Load_NotMasterData_ThrowsInvalidOperation` | `new byte[] { 1, 2, 3 }` で `InvalidOperationException`。メッセージに `NotMasterData` を含み、スキーマのハッシュ（`MasterDataSchema.Hash`）を含まない |
| `Load_Null_ThrowsArgumentNull` | `null` で `ArgumentNullException` |
| `Load_TamperedPayload_ThrowsInvalidOperation` | 同梱のバイナリの最後の 1 バイトを `^ 0xFF` に変えて `Load`。`InvalidOperationException`（`Corrupted`） |
| `LoadFromStreamingAssetsAsync_ReturnsDatabase` | `[UnityTest]`。`UniTask.ToCoroutine(async () => { … })` で `LoadFromStreamingAssetsAsync()` を呼び、`CardTable.Count == 36` |
| `LoadFromStreamingAssetsAsync_Cancelled_Throws` | `[UnityTest]`。キャンセル済みのトークンで呼び、`OperationCanceledException` |

- 暗号化の有無に関係なく通る。`EvaluationRankReward` の検査はクライアントの `MemoryDatabase` に表がないので書かない（コンパイルできない）
- 実行方法は、5章タスク 3 の手順 5。サーバー側（`dotnet test --project server/Uta.Server.Tests`）は変えない

### 確かめられないこと（この計画では未確認として残す）

- IL2CPP（Windows の IL2CPP ビルドは `IL1009` で通らない）での読み込み。MasterMemory のデータ型は `[MessagePackObject(true)]`（文字列キー）なので、IL2CPP ではフォーマッターの事前生成が要る。MessagePack の Source Generator が `Uta.Client.Infrastructure` の `Resolver` に含めるか、MasterMemory が自前のリゾルバーを生成するかは、Editor のコンパイル後に生成物を見ればわかる（タスク 1 の後に報告する）が、動くかは IL2CPP のビルドが通るまで確かめられない
- Android（`jar:file://` の `UnityWebRequest`）・iOS の実機での読み込み
- 起動フローからの利用（DI への登録）

### 実装担当が止まって報告するもの

- タスク 1 の手順 6 でコンパイルエラー、または Source Generator が `MemoryDatabase` を生成しない（`Uta.MasterData.MemoryDatabase` 型が存在しない）。エラーの全文と、`Assets/Packages/MasterMemory.3.0.4/` の中身（`analyzers/` の DLL に `RoslynAnalyzer` のラベルが付いているか: インスペクターの Asset Labels）を添える
- 4.1 の変更で、アドオンのビルド・テストの結果が変わる
- 4.5 の `precompiledReferences` を、CS0012 に名指しされる 3 つ以外に足したくなった

## 7. 確認してほしいこと（未決事項）

1. **`MasterMemory.Addons.Core` の Unity への入れ方**。推奨は **C**（タスク 2。`src/MasterMemory.Addons.Core/` に `package.json` と asmdef を置く。`shared/Uta.Shared.Domain` と同じ形で、ソースを移さない）。**B**: Uta 側で `dotnet build external/MasterMemoryAddons/src/MasterMemory.Addons.Core -c Release` の DLL を `Assets/Plugins/MasterMemory.Addons.Core/` に置いて `.meta` をコミットする（基盤は変えないが、バイナリをコミットし、アドオンの更新のたびに作り直す）。B の場合はタスク 2 を「DLL をコピーして `.meta` をコミット」に置き換え、asmdef の参照を `precompiledReferences: ["MasterMemory.Addons.Core.dll"]` にする。ソースを `Unity/…/Runtime/` に移す形（VolTech-MagicOnion の `Abstractions` と同じ）は、Core が Unity と .NET の両方で使われるだけで、移す手間に見合わないとして採らなかった。
2. **クライアントのテストの置き方**。推奨は **Unity Test Framework の EditMode テストを足す**（タスク 3。`com.unity.test-framework` 1.9.0 は `packages-lock.json` に間接依存として既にある）。しない場合は、`unity command eval` で手で確かめる記述に差し替え、6章のテストは書かない（再現できない確認になる）。
3. **アドオンの変更の取り込み先**。タスク 2 のブランチ `unity-package` を、アドオンの `main` に取り込むか（PR を作るか、直接マージして push するか）。取り込む前は、Uta のポインタがブランチのコミットを指す。
4. **本計画が始まる時期**。2/2 のマージを待つ（推奨）。待たずに、1/2 の定義（5 テーブル）だけで先に実装することもできる（テーブル数と 6章の件数が 5 テーブル分になり、2/2 のマージ後に `master-gen` をやり直す。クライアントのバイナリの差分が 1 回増える）。

## 8. 引き継ぎ（2/2 の実装セッションへ）

- 2章で確かめたとおり、**`[Exists]` が別の側の表を参照しても `master-gen` は通る**。2/2 の「`EvaluationRankReward.Rank` に `[Exists]` を付けない」は、付けてよくなった（ServerOnly → Common の向き。アドオンのサンプルと同じ）。付けるかは 2/2 の判断に任せる
- クライアントの出力を足したあと、定義のファイル名が 2/2 の計画のとおり（`Common.cs`・`Lesson.cs` ＋ 2/2 の追加分）であれば、`Generated/` の `.cs` の名前もそれに対応する
