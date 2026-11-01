# マスタを Unity に取り込む: アドオンのツールでの確認（2026-11-01）

実装計画 [2026-11-01-masterdata-unity.md](../plans/2026-11-01-masterdata-unity.md) を書くために、`external/MasterMemoryAddons`（`41b42ef`）のツールを実際に動かして確かめた記録。出力先はすべて作業用の一時フォルダで、リポジトリには何も書いていない。

## 1. クライアント用の出力（1/2 の定義と CSV）

- 設定: `definitions` = `masterdata/Definitions`、`csv` = `masterdata/csv`、出力先 1 つ（`target: client`、`namespace: Uta.MasterData`、`encrypt: true`）
- 実行: `dotnet run --project external/MasterMemoryAddons/src/MasterMemory.Addons.Tools -p:ManagePackageVersionsCentrally=false -- master-gen --config <設定>`
- 結果: 終了コード 0。「対象: Client / テーブル: 5 / 4008 bytes / 暗号化」。ツールは生成ソースを netstandard2.1・C# 9 でコンパイルして検証するので、定義は Unity の制約を満たす
- 出力: `Common.cs`・`Lesson.cs`・`MasterDataSchema.cs`、`masterdata.bytes`、`masterdata.manifest.json`（`formatVersion` 2・`schemaId`・`dataHash`・`encrypted: true`・`size`・`createdAt`）
- 生成ソースの `using`: `System`・`System.Collections.Generic`・`MasterMemory`・`MessagePack`（`MasterMemory.Addons.Attributes` は不要）

## 2. `[Exists]` が別の側の表を参照する場合

- 定義: `Item`（Common）が `[Exists(typeof(Secret))]` の列を持ち、`Secret` は `[ServerOnly]` の表。`Item` に `[ClientOnly]` の列もある。出力先は client と server の 2 つ
- 結果: 終了コード 0。client は 1 テーブル（`Secret` なし）、server は 2 テーブル。client の生成ソースに `[Exists]`・`Secret` は出ない
- ServerOnly → Common の向きは、アドオンのサンプル（`DropTable` → `Item`）にある

## 3. そのほか

- `AesMasterDataCipher` の PBKDF2 反復は 1000 回（`Pbkdf2IterationCount`）
- NuGet `MasterMemory` 3.0.4: `lib/netstandard2.0/MasterMemory.dll`、`analyzers/dotnet/cs/MasterMemory.SourceGenerator.dll`。依存は `MasterMemory.Annotations` 3.0.4 と `MessagePack` 3.1.3
- Unity 7 の組み込みの `com.unity.test-framework` は 1.9.0（`client/uta/Packages/packages-lock.json`）
- `shared/Uta.Shared.Domain` は、csproj のあるフォルダに `package.json`・asmdef を置き、`shared/Directory.Build.props` で `bin~/`・`obj~/` を出力先にしている（Unity のパッケージ化の前例）
- 確かめていないこと: Unity での Source Generator（NuGetForUnity の `analyzers` 経由）、IL2CPP、Android・iOS
