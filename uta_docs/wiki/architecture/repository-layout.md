---
title: リポジトリ構成・レイヤー・共通の規約（実装設計書 共通・基盤編）
status: draft
updated: 2026-11-01
---

# リポジトリ構成・レイヤー・共通の規約（実装設計書 共通・基盤編）

実装設計書（4編）の1編目。4編すべてが前提にする「どこに何を置くか」「何が何を参照してよいか」を定める。

- ゲームの仕様は [systems/](../systems/produce.md) が正で、ここには書かない
- サーバー基盤の機能（認証・冪等性・エラー・ヘッダー）は [server-foundation.md](server/server-foundation.md) が正で、ここでは繰り返さない

## 凡例

- **[決定]**: [CLAUDE.md](../../../CLAUDE.md)・accepted の ADR、またはユーザーが確認した方針（日付を添える。ADR にしていないものもある）
- **[Uta案]**: この叩き台の提案。採否はレビューと ADR で決める
- 推測は「推測」と明記する

## 1. 設計書の構成（4編）[決定]

| 編 | ページ | 対象 | 対応する主なコード |
|---|---|---|---|
| 共通・基盤 | 本ページ | 構成、依存ルール、Shared のコードの制約、機能ID | リポジトリ全体 |
| マスター設計 | [masterdata-pipeline.md](master/masterdata-pipeline.md) | マスタの定義・生成・配信 | `masterdata/`、各側の生成物 |
| API設計 | [api-design.md](api-design.md) | サービスIF・DTO、プレイヤーデータ、ゲームルールの置き場 | `shared/Uta.Shared.Contracts`、`server/` |
| 画面設計 | [screen-design.md](screen-design.md) | 画面・遷移・クライアントの層 | `client/uta/` の Presentation |

- 進め方は [CLAUDE.md](../../../CLAUDE.md) の「実装前のルール」に従う
- **機能ID**: 仕様ページの ID（`PRD-xx` / `GAC-xx` / `IDL-xx` / `SUP-xx`）を、4編すべての見出し・表に付ける。層で分けた4編を、機能単位で横断して追うため。VolTech-MagicOnion の機能（認証など）は ID を付けず [server-foundation.md](server/server-foundation.md) を参照する

## 2. リポジトリ構成

[CLAUDE.md](../../../CLAUDE.md) の構成に、`shared/`・`server/`・`client/` の中身を足したもの。`masterdata/` の最上位への配置は [決定]（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）。[決定] / [Uta案] は「決めたか」の印で、「あるか」は「（未作成）」で示す（2026-10-31 時点）。

```
uta/
├─ CLAUDE.md
├─ global.json              # [決定] dotnet test の実行方式（Microsoft.Testing.Platform）
├─ Uta.slnx                 # [決定] shared/・server/ のソリューション（2.1）
├─ Directory.Build.props    # [決定] .NET のプロジェクト共通の設定（2.1）
├─ Directory.Packages.props # [決定] NuGet の版を一か所で固定する（2.1）
├─ external/                 # [決定] サブモジュール（ADR 0005）
│  ├─ VolTech-MagicOnion/    #   サーバー基盤、Unity 側 Abstractions
│  ├─ MasterMemoryAddons/    #   マスタのコード生成・検証・バイナリ生成
│  ├─ Supplement/            #   Unity 補助パッケージ
│  └─ UnityScreenNavigator/  #   画面遷移
├─ uta_docs/
├─ masterdata/               # [決定] マスタの正本。Unity にも shared/ にも入れない（未作成）
│  ├─ Definitions/           #   全列入りの C# 定義（[ClientOnly]/[ServerOnly] 付き）
│  ├─ csv/                   #   スプレッドシートから書き出した CSV
│  ├─ masterdata.yaml        #   出力先の設定
│  └─ gen.ps1                #   生成のスクリプト（masterdata-pipeline.md 7章）
├─ shared/                   # [決定] Unity・サーバー共有。C# 9 の範囲（.editorconfig）
│  ├─ Uta.Shared.Domain/            # [決定] 純粋なゲームルール（ADR 0008）
│  ├─ Uta.Shared.Contracts/            # [決定] サービスIF・DTO（通信契約。ADR 0008）。今は中身が空
│  └─ Uta.Shared.Domain.Tests/      # [決定] Uta.Shared.Domain の単体テスト（10.4）
├─ server/                   # [決定] .NET 10
│  ├─ compose.yaml           # [決定] ローカル開発用の PostgreSQL（11.4）
│  ├─ Uta.Server.Tests/      # [決定] サーバーのテスト 1 プロジェクト（10.4）
│  └─ Uta.Server/            # [決定] 1プロジェクト。フォルダで層を分ける
│     ├─ Api/                #   MagicOnion のサービス実装
│     ├─ Application/        #   ユースケース
│     ├─ Domain/             #   サーバー固有のルール（抽選、天井など）
│     ├─ Infrastructure/     #   DB、マスタの読み込み
│     ├─ Migrations/         # [決定] DbUp の SQL（ADR 0012）
│     └─ MasterData/         #   生成物（サーバー用）: Generated/ のソースと masterdata.bytes（ADR 0013）
└─ client/
   ├─ Directory.Build.props  # [決定] 上の階層の設定を Unity の csproj に効かせない（2.1）
   └─ uta/                   # [決定] Unity プロジェクトのルート
      └─ Assets/Scripts/     # [決定] 層ごとに asmdef（3章）（未作成）
         ├─ Application/     #   ユースケース、ポート
         ├─ Infrastructure/  #   API 呼び出し、マスタ、ローカル保存
         │  └─ MasterData/Generated/   生成物（クライアント用のソース。バイナリは Assets/StreamingAssets/MasterData/。ADR 0013）
         ├─ Presentation/    #   View、Presenter
         └─ Bootstrap/       #   Composition Root
```

- **マスタ型を `shared/` に置かない**。サーバー専用の列・テーブルをクライアントから見えなくするため（[ADR 0007](../decisions/0007-masterdata-with-addons.md)。ADR 0002 の当初の案から変えた）
- **`shared/` の取り込み方** [決定。2026-10-31。動作は 8章で確かめる]:
  - Unity: `shared/Uta.Shared.Domain/` と `shared/Uta.Shared.Contracts/` の直下に `package.json` と asmdef を置き、`client/uta/Packages/manifest.json` から `file:` で参照する。`package.json` の `name` は `com.uta.shared.domain` / `com.uta.shared.contracts`、asmdef の名前はアセンブリ名と同じ（`Uta.Shared.Domain` / `Uta.Shared.Contracts`）。`manifest.json` の行は `"com.uta.shared.domain": "file:../../../shared/Uta.Shared.Domain"`
  - `dotnet build` の出力が Unity に読まれないよう、`BaseOutputPath` / `BaseIntermediateOutputPath` を `bin~/` / `obj~/` にする（Unity は `~` で終わるフォルダを読まない）。**設定は `shared/Directory.Build.props` に書く**（親の `Directory.Build.props` を `Import` する）。`.csproj` の本文に書くと NuGet の復元が先に `obj/` を作ってしまうため（MSB3539） [決定。2026-11-01。ユーザーの判断]。確かめ方は 8章（ビルドの後に `obj/` が無いこと）。`Uta.Shared.Domain.Tests/` はパッケージの外（`shared/` の直下で別フォルダ）なので取り込まれない
  - `.meta` は Unity が作ったものをコミットする（CLAUDE.md の Git の規則）
  - サーバー: `ProjectReference` で参照する

### 2.1 ソリューションと NuGet の版 [決定。2026-10-31]

VolTech-MagicOnion（`external/VolTech-MagicOnion`）と同じ形にする。VolTech-MagicOnion の設計書 `external/VolTech-MagicOnion/docs/design/project-structure.md` の方式をそのまま使い、別の方式を考えない。

| ファイル | 中身 |
|---|---|
| `Uta.slnx` | `shared/` と `server/` の全プロジェクト。VolTech-MagicOnion の `VolTech.Server`・`VolTech.Server.Abstractions` も `external` のソリューションフォルダに入れる（デバッグで VolTech-MagicOnion のコードに入るため）。Unity の csproj は入れない |
| `Directory.Build.props` | 全プロジェクト共通の設定: `Deterministic`、`EnforceCodeStyleInBuild`（`.editorconfig` の規約をビルドの警告にする）。`Nullable`・`LangVersion` などは各 `.csproj` に書く（`shared/` は C# 9・`Nullable` 無効、`server/` は既定の C# 14・`Nullable` 有効）。`Uta.Shared.Domain` は `TreatWarningsAsErrors` なので、`EnforceCodeStyleInBuild` で既存のコードがビルドエラーになることがある。そのときは**コードを `.editorconfig` に合わせて直す**（規約を緩めない）。直すのは 12章の手順 1 の中で行う |
| `Directory.Packages.props` | NuGet の版の中央管理（`ManagePackageVersionsCentrally`。推移的な依存も固定する）。各 `.csproj` の `PackageReference` には版を書かない |
| `client/Directory.Build.props` | 中身が空の `<Project />`。Unity が `client/uta/` に作る csproj に、上の 2 つが効かないようにする（VolTech-MagicOnion の `Unity/Directory.Build.props` と同じ） |

- **版は VolTech-MagicOnion に合わせる**: VolTech-MagicOnion と共通のパッケージ（MagicOnion・MessagePack・Grpc・Npgsql・dbup-postgresql・Ulid・Testcontainers など）は、VolTech-MagicOnion の `Directory.Packages.props` と同じ版にする。上げるときは VolTech-MagicOnion を先に上げ、ポインタを更新してから Uta を上げる（3.1）。版がずれると、VolTech-MagicOnion を `ProjectReference` したときに NuGet の版の衝突（NU1605 など）になる
- VolTech-MagicOnion と共通でないもの（xunit.v3 など）は Uta が決める。xunit.v3 は 10.4
- 今ある `shared/Uta.Shared.Domain.Tests` の `Version` の直書きは、最初の実装計画で `Directory.Packages.props` に移す

## 3. アセンブリと依存 [決定]

**どこで動くか・Unity と共有するかで分け、機能ごとには分けない**（MasterMemoryAddons の [構成ルール](../../../external/MasterMemoryAddons/docs/dev/structure.md)、VolTech-MagicOnion の設計書 `external/VolTech-MagicOnion/docs/design/project-structure.md` と同じ考え方）。

- **名前** [決定。2026-10-31]: アセンブリ名とルート名前空間を同じにする。`shared/` は `Uta.Shared.Domain` / `Uta.Shared.Contracts`、サーバーは `Uta.Server`（層はその下の名前空間 `Uta.Server.Api` など）、クライアントは `Uta.Client.<層>`（`UnityEngine.Application` と衝突しないよう `Uta.Client.` を付ける）
- **層の呼び名と改名** [決定。2026-10-31]: Domain は Shared とサーバー・クライアントで混ざりやすいので、設計書では次の呼び名で書く（[glossary.md](../glossary.md)）。コードもこの名前にそろえた（**2026-11-01 に改名した**）

| 呼び名 | コード |
|---|---|
| Shared.Domain | `shared/Uta.Shared.Domain`（テストは `shared/Uta.Shared.Domain.Tests`）。Unity のパッケージ名は `com.uta.shared.domain` |
| Shared.Contracts | `shared/Uta.Shared.Contracts`（サービスIF・DTO。今は中身が空）。パッケージ名は `com.uta.shared.contracts` |
| Server.Domain | `server/Uta.Server/Domain/`（名前空間 `Uta.Server.Domain`。未作成） |
| Client.Domain | なし。呼び名だけ用意する。クライアント固有の知識は今は `Application` / `Presentation` に置き（6.1）、層を作るときに `Uta.Client.Domain` にする |

| アセンブリ | 置き場 | 形式 | 参照してよいもの |
|---|---|---|---|
| `Uta.Shared.Domain` | `shared/Uta.Shared.Domain` | netstandard2.1、C# 9 | なし（BCL のみ） |
| `Uta.Shared.Contracts` | `shared/Uta.Shared.Contracts` | netstandard2.1、C# 9 | `Uta.Shared.Domain`、VolTech-MagicOnion の `VolTech.Server.Abstractions`、MagicOnion / MessagePack |
| `Uta.Server` | `server/Uta.Server` | net10.0 | `Uta.Shared.Domain`、`Uta.Shared.Contracts`、`VolTech.Server`（下の表）、生成したマスタ |
| `Uta.Client.Application` | `Assets/Scripts/Application` | asmdef | `Uta.Shared.Domain`、`UniTask` |
| `Uta.Client.Infrastructure` | `Assets/Scripts/Infrastructure` | asmdef | `Uta.Client.Application`、`Uta.Shared.Domain`、`Uta.Shared.Contracts`、VolTech-MagicOnion のクライアント、MasterMemory と生成物、`Supplement` |
| `Uta.Client.Presentation` | `Assets/Scripts/Presentation` | asmdef | `Uta.Client.Application`、`Uta.Shared.Domain`、`UnityScreenNavigator`、`UniTask`、`VContainer`（`[Inject]` を使うため） |
| `Uta.Client.Bootstrap` | `Assets/Scripts/Bootstrap` | asmdef | すべて（DI のスコープを組む。`VContainer`） |

- `VContainer` を参照してよいのは `Bootstrap` と `Presentation` だけ [決定。2026-11-01。ユーザーの判断]。`Application`・`Infrastructure` はコンストラクタで受け取るだけにし、`VContainer` を知らない

- サーバーを1プロジェクトにするのは VolTech-MagicOnion と同じ理由（プロジェクトを細かく分けると `internal` が使えず、依存も切れない）。層の分離は**フォルダと名前空間**で行い、依存の向きは**アーキテクチャテスト**で守る（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）。ツールは VolTech-MagicOnion の設計書が挙げる **NetArchTest** で、サーバーのテストプロジェクトに置く [決定。2026-10-28]（テストの置き場は 10.4）
- クライアントは、コンパイル時に依存を強制できる手段が asmdef しかないため、層ごとに asmdef を分ける
- **Unity に入れる外部パッケージ** [決定。2026-10-31]: VolTech-MagicOnion の利用ガイド（[getting-started.md](../../../external/VolTech-MagicOnion/docs/guide/getting-started.md)「使う版」）に合わせる。何をどう入れるかの正はこのガイドで、Uta はそれに足すものだけを書く
  - NuGet のパッケージ（`MagicOnion.Client`・`MessagePack`・`Grpc.Net.Client` などの Grpc・`Google.Protobuf`）は **NuGetForUnity** で入れ、版は `Assets/packages.config` で固定する。UPM の `package.json` は NuGet のパッケージを依存に書けないため
  - UPM にあるもの（NuGetForUnity・UniTask・VContainer・`MagicOnion.Client.Unity`・`MessagePack.Unity`・YetAnotherHttpHandler）は UPM の git URL で入れ、版はタグで固定する（`manifest.json`）
  - IL2CPP のビルドに要る `Microsoft.Bcl.AsyncInterfaces` は、ガイドのとおり手で置く
  - MasterMemory の実行時ライブラリの入れ方は未決（[masterdata-pipeline.md](master/masterdata-pipeline.md) の未決事項）
  - サブモジュール（`external/`）のものは `file:` で参照する。具体の版は client/ の実装計画で決める（VolTech-MagicOnion と共通のものは同じ版。2.1）

参照する VolTech-MagicOnion のプロジェクト（[server-foundation.md](server/server-foundation.md)）[決定。2026-10-31。VolTech-MagicOnion の構成に合わせて直した]:

| VolTech-MagicOnion のプロジェクト・パッケージ | 参照する側 |
|---|---|
| `VolTech.Server.Abstractions`（`external/VolTech-MagicOnion/src/VolTech.Server.Abstractions/`） | `Uta.Shared.Contracts` と `Uta.Server` が `ProjectReference` で参照する |
| `VolTech.Server`（`external/VolTech-MagicOnion/src/VolTech.Server/`） | `Uta.Server` が `ProjectReference` で参照する |
| Unity パッケージ `com.chinpangx.voltech.magiconion`（`external/VolTech-MagicOnion/Unity/com.chinpangx.voltech.magiconion/`） | クライアントが `manifest.json` から `file:` で参照する。`Abstractions` のソースの実体と、クライアント（`ApiCore` など）が入っている |

- VolTech-MagicOnion は「Unity と共有するか」だけで 2 つのプロジェクトに分け、サーバー側の関心ごと（`Auth/`・`Status/`・`Transaction/`・`Logging/`・`Hosting/`）はフォルダで分けている。Uta が参照するのは公開 API だけ（3.1）

### 3.1 共通基盤（`external/`）の扱い [決定。2026-10-31]

**目指す形**: Uta の開発を通して共通基盤を整え、リリースして、別のアプリでもそのまま使える構造にする。Uta は共通基盤の最初の利用者で、Uta で要るものを共通基盤に足していく。

- **固定はサブモジュールのポインタ（コミット）で行い、タグを待たない**。開発を速くするため。共通基盤を更新したら、Uta でポインタを上げる（[ADR 0005](../decisions/0005-foundation-submodule.md) の手順）
- **区切りでリリースする**: 完了条件 0（[game-spec.md](../overview/game-spec.md)。Unity から VolTech-MagicOnion の RPC が通る）が通ったときなど、共通基盤の機能が一区切りしたら、共通基盤側で版を出し（`release` スキル）、ポインタをそのタグのコミットに合わせる。その後もタグは必須にしない

開発のルール（`external/` の 4 つすべてに当てはめる。主な対象は `VolTech-MagicOnion`）:

1. **Uta 固有のものを共通基盤に入れない**。Uta の名前、ゲームのルール、マスタ、Uta のエラーの reason、Uta の DB のテーブルは Uta 側に置く。共通基盤は Uta を参照しない
2. **足りない機能の置き場は「別のゲームでも同じものが要るか」で決める**。要るなら共通基盤に足す（Uta 側で回避の実装を書かない）。要らないなら Uta 側で、共通基盤の拡張点（`IPlayerResolver`、設定の `VoltechServerOptions` など）から差し込む。拡張点が足りなければ、拡張点を共通基盤に足す
3. **Uta は共通基盤の公開 API だけを使う**。共通基盤の `InternalsVisibleTo` を Uta に向けない。`internal` に頼ると、別のアプリから使えない形のまま固まるため
4. **共通基盤の変更は共通基盤の中で完結させる**: 共通基盤のテスト・ドキュメント・`CHANGELOG.md`（Unreleased）まで共通基盤のリポジトリで済ませ、共通基盤を push してから Uta のポインタを更新する（CLAUDE.md の Git の規則）。互換のない変更は CHANGELOG の「互換性が壊れる変更」に書く
5. **NuGet の版は VolTech-MagicOnion が先**（2.1）
6. Uta の実装計画で共通基盤の変更が要るときは、計画のタスクを「共通基盤の変更」と「Uta のポインタ更新と利用」に分ける

サーバーの中の依存の向き [決定。2026-10-31]。「参照してよいもの」は設計の意図で、NetArchTest で検査するのは「禁止」の列。**層ごとに「禁止」の列を 1 ルールにする**（2026-11-01、1a の実装計画で、下の箇条書きの形に統一した。許可の一覧の形は、禁止の列に無い依存を検出できるので、層に型が増えたときに見直す）:

| フォルダ（名前空間） | 参照してよいもの | 禁止 |
|---|---|---|
| `Api` | `Application`、`Uta.Shared.Contracts`（DTO）、`Uta.Shared.Domain` | `Infrastructure` |
| `Application` | `Domain`、`Uta.Shared.Domain` | `Api`、`Infrastructure`、`Uta.Shared.Contracts` |
| `Domain` | `Uta.Shared.Domain` | `Api`、`Application`、`Infrastructure`、`Uta.Shared.Contracts`、Npgsql・Dapper |
| `Infrastructure` | `Application`（ポートを実装する）、`Domain`、`Uta.Shared.Domain`、生成したマスタ | `Api` |

- DTO ↔ ドメインの変換は `Api` で行う。`Application` は DTO を知らない
- **生成したマスタの置き場** [決定。2026-11-01。ユーザーの判断]: 生成したマスタ（名前空間 `Uta.MasterData`）は、各側の Infrastructure の下に置く（サーバーは `server/Uta.Server/Infrastructure/MasterData/`、クライアントは `Assets/Scripts/Infrastructure/MasterData/`）。名前空間を置き場に合わせない（両側で同じ名前空間にして生成を 1 つにするため）のは、「ルート名前空間はアセンブリ名と同じ」の規則の例外として扱う。生成物を参照してよいのは Infrastructure だけ
- 起動の組み立て（`Program.cs`・`AddVoltechServer()`）はすべてを参照してよい
- 上の「禁止」を、アーキテクチャテストのルール 1 つずつにする（層ごとに 1 件。型がまだ無い層のルールは空の集合で通るので、その層に最初の型が入る計画が「型が 1 つ以上ある」テストを足す）。動作の確認（2026-11-01）: NetArchTest.Rules 1.3.2 は .NET 10・xunit.v3（Microsoft.Testing.Platform）で動き、違反を検出できる
- クライアントの `Application` の asmdef は **`noEngineReferences` を使わない**。UniTask を参照するため（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)・[ADR 0009](../decisions/0009-client-stack.md)）。UnityEngine の API を使わないことは、コードレビュー（[layers/client.md](../../../.claude/skills/requesting-code-review/layers/client.md)）で守る [決定。2026-10-31。2026-10-28 の「`true` にする」は ADR と食い違っていたので戻した]
- DI は VContainer、非同期処理は UniTask [決定。[ADR 0009](../decisions/0009-client-stack.md)]

## 4. 層の依存ルール [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

依存は**常に内側へ**。`Domain` → `Application` → `Infrastructure` / `Presentation` → `Bootstrap`。

| 層 | 責務 | 禁止 |
|---|---|---|
| Domain | ゲームルール（計算式、状態遷移、検証）、値オブジェクト、enum、ID 型 | UnityEngine、MasterMemory とその生成物、MagicOnion、MessagePack、DB、`float` / `double` |
| Application | ユースケース。外部とのやりとりは**ポート（インターフェース）**で定義する。UniTask は使ってよい | UnityEngine の API（コードレビューで守る。3章）、Infrastructure・Presentation・`Uta.Shared.Contracts` への参照 |
| Infrastructure | ポートの実装（API 呼び出し、マスタ読み込み、ローカル保存）。DTO ↔ Domain のマッピングはここで行う | Presentation への参照 |
| Presentation | View（表示）と Presenter（Application を呼ぶ） | Infrastructure・`Uta.Shared.Contracts`（通信 DTO）への参照 |
| Bootstrap | 起動、DI の組み立て（Composition Root）。すべてを参照してよい | — |

- **クライアントの DTO ↔ Domain のマッピングは Infrastructure に閉じる** [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]。サーバーは `Api` で行う（3章）。この 4章の表はクライアントの層 [決定。2026-11-01。ユーザーの判断]。DTO は enum とプリミティブ型で組み、マッピングを薄く保つ
- マスタの生成物（`MemoryDatabase`）は Infrastructure に置き、Application へは**ポート越しに必要な値だけ**を渡す。`Domain` / `Application` をマスタの形式から独立させるため
- **マスタの行 → `Uta.Shared.Domain` の型の組み立て** [決定。2026-10-31]: 生成クラスはクライアント用・サーバー用で別なので、両側の Infrastructure に読み込みのコードが要る（二重になるのは受け入れる）。Infrastructure は**行の値をそのまま `Uta.Shared.Domain` の行の型（効果・条件の 1 行と同じ列を持つ型）の配列に詰めて渡すだけ**にし、グループ ID の解決と入れ子（`EffectDef` の子）の組み立ては `Uta.Shared.Domain` の関数 1 つで行う。両側で組み立てが食い違わないようにするため（[lesson-enums.md](lesson-enums.md)）

## 5. Shared（`shared/`）のコードの制約 [決定／一部 Uta案]

既存のルール（[CLAUDE.md](../../../CLAUDE.md)、`.editorconfig`）:

- C# 9 の範囲（ファイルスコープ namespace、プライマリコンストラクタ、コレクション式、Index/Range は使わない）
- リフレクション依存を避ける（iOS の IL2CPP / AOT）

Uta で足す制約 [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]:

- UnityEngine に依存しない。MasterMemory・生成物にも依存しない（サーバーから参照できなくなる）
- **`float` / `double` を使わない**。整数または固定小数点（[lesson-card.md](../systems/lesson-card.md) 9.6）。クライアント（IL2CPP）とサーバー（.NET 10）で結果を一致させるため
- **乱数は `IRandom` を注入し、決定的な疑似乱数 `xoshiro256**`** を使う（10.3）。`System.Random` は使わない（[lesson-card.md](../systems/lesson-card.md) 13章）。シードの生成はサーバー
- `[ServerOnly]` の列に依存するルールは `Domain` に入れない。必要な値は引数で受け取る
- **`init`・`record`・`required`・nullable の注釈は `shared/` で使わない** [決定。2026-10-31。[lesson-domain-types.md](lesson-domain-types.md) と同じ]。コンストラクタと読み取り専用プロパティで書く。理由: `init` は netstandard2.1 では `IsExternalInit` の定義（polyfill）が要り、Shared に置くと、それを参照するアセンブリごとに定義の重複を気にすることになるため。nullable の値型（`int?`）は使ってよい
- **マスタの生成ソースは `init` を使う**（アドオンの規約。`{ get; init; }`。[masterdata-pipeline.md](master/masterdata-pipeline.md) 2章）。Shared の規約とは別で、生成ソースは Shared に入らない（2章）。Unity 側の生成ソースは `Uta.Client.Infrastructure` に入るので、**`Uta.Client.Infrastructure` に `internal` の `System.Runtime.CompilerServices.IsExternalInit` を 1 つ置く**（アドオンの `samples/Client/IsExternalInit.cs` と同じ）[決定。2026-10-31]。サーバー（net10.0）には要らない

## 6. Shared.Domain の範囲 [決定]

`Uta.Shared.Domain` の主な中身は、[lesson-card.md](../systems/lesson-card.md) の効果エンジン（ゾーンの移動、ターン処理順、計算式）と、その状態遷移・使用条件の判定。

| 場所 | 使い方 |
|---|---|
| サーバー | 結果を**確定**する（権威） |
| クライアント | 同じルールで、使用可否の判定と予測の表示まで行う。**シードは持たない**ので、乱数が絡む結果（ドローなど）は予測しない |

- 範囲は [ADR 0008](../decisions/0008-shared-domain-and-onion.md) で決定（2026-10-27）。これに合わせて [CLAUDE.md](../../../CLAUDE.md) の実装方針と [produce.md](../systems/produce.md) 10章を更新した
- 結果の先行表示（シードをクライアントに渡す）は、山札の引きを先読みする不正を防げないため行わない
- **古いクライアントの計算式が、サーバーの最新の式とずれることがある**。予測の表示はずれてもよく、確定は必ずサーバーのレスポンスで行う。強制アップデートは実装しないので（10.5）、ルールを変えたら手元のアプリを入れ直す。進行中のプロデュースは、アプリのバージョンが変われば破棄される
- [lesson-phases.md](lesson-phases.md) 8章 の「同じシードと操作列で、同じ結果になる」は、**`Uta.Shared.Domain` の単体テスト（.NET 10）**で確かめる（10.4）。本番のクライアントはシードを持たないので、本番で IL2CPP 上の乱数の結果を再現する場面はない。ただし計算が IL2CPP でも同じになることは、実機ビルドのときに手動で 1 回確かめる（[lesson-phases.md](lesson-phases.md) 8章、[game-spec.md](../overview/game-spec.md) 完了条件 7）。ADR 0008 の「Unity 側のテストは必須にしない」は、CI で毎回回すテストにしないという意味で読む [決定。2026-11-01。ユーザーの判断]。乱数が絡まない予測の一致は、整数演算と同じコードを使うことで担保する

### 6.1 固有の知識の置き場 [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

クライアントまたはサーバーだけが持つ知識は、**Shared.Domain に入れず、各側の層に置く**。依存は内向きなので、Shared.Domain は Server.Domain・クライアントの層を知らない。各側の層は Shared.Domain を参照してよく、逆は禁止。

| 種類 | 置き場 | 例 |
|---|---|---|
| 両側で一致させるルール | Shared.Domain（`shared/Uta.Shared.Domain`） | カード効果、ターン処理順、計算式、使用可否の判定 |
| サーバー固有 | Server.Domain（`server/Uta.Server/Domain/`） | ガチャ抽選、交換pt・天井、シード生成、レンタル候補の決定、報酬付与 |
| クライアント固有 | クライアントの `Application` / `Presentation`（Client.Domain は今は作らない。3章） | 表示用の整形、並び順、演出の制御、UI の状態、ローカル設定 |

置き場の判断基準（上から順に当てはめる）:

1. クライアントに渡してはいけない知識（秘匿）→ Server.Domain
2. 結果が両側で一致しなければならないルール → Shared.Domain
3. 表示にしか関係しない → クライアント固有

Shared.Domain と固有の知識が混ざるときの作法:

- 値やルールは Shared.Domain の型で持ち、固有の属性は各側のエンティティが持つ（Shared.Domain の型を包む＝合成）。例: サーバーのプロデュースのセッションは、Shared.Domain の状態の型に、シードや乱数の位置を足して持つ
- 継承や `partial` は、アセンブリをまたげないので使わない。合成と拡張メソッドを使う
- 側ごとに実装が変わる部分は、インターフェースで差し替える（`IRandom`、検証のポリシーなど）
- `[ServerOnly]` のマスタの値は、Shared.Domain に入れず、引数で受け取る（5章）

**移動のサイン**: 「クライアント固有」に置いたルールが、結果に影響し始めたら、Shared.Domain へ移す。別々に実装すると食い違うため。

## 7. ビルド・CI

**今あるもの**: `Uta.Shared.Domain` のビルドと単体テスト、wiki の点検。コマンドは [CLAUDE.md](../../../CLAUDE.md) の「ビルド・テスト・実行」。

**これから作るもの**（CI は未実装。作ったら CLAUDE.md にも追記する）。CI で回すもの（GitHub Actions）と、手で回すものに分ける。実機ビルドは Unity のライセンスと macOS のランナーが要るので、当面は手で回す:

- `master-gen --check`（CSV の検証と、生成ソースが古くないかの検出。バイナリの古さの検出は、アドオンの改善待ち。[ADR 0013](../decisions/0013-commit-masterdata-binary.md)）
- マスタのバイナリを `MemoryDatabase` で読めること（クライアント用・サーバー用の両方）
- `Uta.Shared.Domain` の単体テスト（同じシードと操作列で同じ結果になること）と、サーバーの結合テスト（10.4）
- サーバーの依存方向のアーキテクチャテスト
- `git clone --recurse-submodules` での初期化（CI も同様）
- クライアントの asmdef の参照が、4章の依存ルールに沿っているか（`Application` が UnityEngine を使わないことは asmdef では強制しない。3章）
- （手で回す）Windows / Android / iOS の実機ビルド（早期・継続的に）。ほかの項目は CI で回す

## 8. 実装前に確かめること（要検証）

決定の前提になっているが、まだ動作を確かめていないもの。確かめたら結果を該当ページに書き、ここから消す。

| 項目 | 関係するページ | 確かめ方 | ブロックする作業 |
|---|---|---|---|
| `shared/` を Unity からローカルパッケージ（`file:` 参照）で取り込めるか。`dotnet build` の後に Unity を開いても型が重複しないか | 2章、[ADR 0005](../decisions/0005-foundation-submodule.md) | Windows / Android / iOS の実機ビルド。`dotnet build` 後に Unity を開く | client/ の最初の実装計画 |
| MasterMemoryAddons のバイナリを Unity で読めるか | [masterdata-pipeline.md](master/masterdata-pipeline.md) 6章 | 実機で `MasterDataBinary.Read` → `MemoryDatabase` | クライアントのマスタ読み込み |
| MasterMemory・MessagePack・MagicOnion・Supplement.MessagePack のバージョンが揃うか。机上では揃う（2026-10-31: VolTech-MagicOnion は MagicOnion 7.11.0・MessagePack 3.1.12、アドオンは MasterMemory 3.0.4・MessagePack 3.1.12）。Supplement.MessagePack は未確認。サーバー側の MagicOnion・MessagePack は 2026-11-01 に確認済み（`Uta.Server` をビルドして NU1605 などが出ない） | [masterdata-pipeline.md](master/masterdata-pipeline.md) 7章 | サーバーとクライアントでのビルド | client/ の最初の実装計画 |
| `[Exists]` が、別の側にだけあるテーブル・列を参照した場合の挙動 | [masterdata-pipeline.md](master/masterdata-pipeline.md) 4章 | アドオンのテストまたはサンプル | マスタの定義 |
| UnityScreenNavigator・VContainer・UniTask が Unity 7（`7000.0.0a12`）で動くか | [ADR 0009](../decisions/0009-client-stack.md) | 実機ビルド | client/ の最初の実装計画 |
| Unity（実機）から Cloud Run へ HTTPS + HTTP/2 で MagicOnion をつなげるか | 11章、[ADR 0011](../decisions/0011-deploy-google-cloud.md) | デプロイ先に実機から接続 | dev へのデプロイ |
| Cloud Run で gRPC を受ける設定（HTTP/2 の end-to-end、Kestrel の h2c） | 11章 | デプロイして確認 | dev へのデプロイ |
| Cloud Run の gRPC のヘルスチェック（起動・生存のプローブ）が `grpc.health.v1` で動くか | 11.4 | デプロイして確認 | dev へのデプロイ |
| Cloud SQL で PostgreSQL 18 を選べるか | 11.4 | 公式の対応表を見る。選べなければ選べる最新の版に、Compose と Testcontainers もそろえる | dev へのデプロイ |
| `Uta.Shared.Domain` の計算が IL2CPP と .NET 10 で一致するか | 6章、10.4 | 実機ビルドのときに 1 回、固定のシードと操作列で再生した状態を項目ごとに比べる（[produce-domain.md](produce-domain.md) 7章）。（[lesson-phases.md](lesson-phases.md) 8章） | なし（実機ビルドの手順に入れる） |

## 9. 共通規約の参照先

| 項目 | 参照先 |
|---|---|
| エラー、ヘッダー、認証、冪等性、トランザクション、フィルター順 | [server-foundation.md](server/server-foundation.md) |
| コード規約、言語バージョン、Git 運用 | [CLAUDE.md](../../../CLAUDE.md)、`.editorconfig` |
| サブモジュールの扱い | [ADR 0005](../decisions/0005-foundation-submodule.md) |
| マスタの定義・生成・配信 | [masterdata-pipeline.md](master/masterdata-pipeline.md) |
| 用語 | [glossary.md](../glossary.md) |

## 10. 共通の規約 [決定]

2026-10-27 に決めた、全編が前提にする規約。

### 10.1 ID の体系

| 対象 | 型 | 備考 |
|---|---|---|
| マスタの ID（カード、アイドル、ガチャなど） | `int` | マスタの列の型の制約（[masterdata-pipeline.md](master/masterdata-pipeline.md)）に合う。**採番の規則は作らない**（2026-10-27 決定） |
| サーバーが発行する ID（ユーザー、プロデュースのセッション、ガチャ履歴など） | ULID | 時刻順に並び、サーバーを分けても衝突しない |

- **ULID の持ち方** [決定。2026-10-28]:

| 場所 | 型 | 理由 |
|---|---|---|
| サーバーの中 | Cysharp の `Ulid` | 生成・比較・変換がそろっている |
| DB | 26 文字の `char(26)` | DTO・ログ・DB が同じ表現になり、辞書順が時刻順になる。TypeHandler は文字列との変換だけ（2026-10-31 に `uuid` から変更）。**`uuid` は使わない**（2026-10-31 決定）。VolTech-MagicOnion のテーブル（`devices`・`last_responses`・`game_logs`）の `player_id` も `char(26)`（2026-10-31 に VolTech-MagicOnion を変更済み。コミット `f79974d`） |
| DTO（`Uta.Shared.Contracts`） | 26文字の `string` | クライアントが Ulid のライブラリに依存しない。ログでもそのまま読める。受け取ったら解析して検証する |
| `Uta.Shared.Domain` | 持たない | `Uta.Shared.Domain` を外部ライブラリから独立させる。ID はアプリ層のエンティティが持つ（6.1 の合成） |

### 10.2 時刻

- **時刻はサーバーが正**。クライアントの時刻で判定しない
- **タイムゾーンは JST 固定**
- **日付の更新は 12 時**（12:00 JST）。日ごとの区切り（日替わりの処理など）はこの時刻を境にする
- **マスタの日時は `string` で持つ**。形式は `yyyy-MM-dd HH:mm:ss` で、JST として解釈する（オフセットは書かない。例: `2026-11-01 12:00:00`）。読むときは `DateTime.ParseExact`（`CultureInfo.InvariantCulture`）で厳密に読み、JST の `DateTimeOffset` にする（[masterdata-pipeline.md](master/masterdata-pipeline.md) 3章）

### 10.3 乱数

- **アルゴリズムは `xoshiro256**`**（64 ビット。xorshift の後継）
- 実装は `Uta.Shared.Domain` に置き、サーバーで使う。範囲内の整数の取り出し方も、同じコードで固定する
- マスターシードはサーバーが暗号用の乱数（.NET の `RandomNumberGenerator`）で生成する [決定。2026-10-31]。マスターシードと、使用中のストリームごとの `xoshiro256**` の状態（と引いた回数）を、プロデュースのセッションに保存する（[api-design.md](api-design.md) 3.1、[produce-domain.md](produce-domain.md) 3・6章）
- シードから用途・場面ごとのストリームを SplitMix64 で作る。範囲内の整数は棄却法、シャッフルは Fisher–Yates。ストリームの一覧と引く順番は [produce-domain.md](produce-domain.md) 3・4章（2026-10-28 決定）
- `System.Random` は使わない
- ガチャも `xoshiro256**` を使う。要求ごとに暗号用の乱数でシードを作り、保存しない（再現は求めない。[gacha.md](../systems/gacha.md) 3.2）

### 10.4 テスト

- **単体テストは専用のプロジェクト（dll）に分ける**。主な対象は `Uta.Shared.Domain`（同じシードと操作列で同じ結果になること、計算式）
- 期待値（固定のシード・操作列の結果、乱数の参照値）は、テストのコードに定数で書く。別ファイルにしない（今の `Uta.Shared.Domain.Tests` と同じ）
- **サーバーは結合テストを書く**（DB を含めて、RPC から結果・保存までを確かめる）
- **Unity 側のテストは必須にしない**
- **フレームワークは xUnit v3（`xunit.v3`）**。最新を触る目的で、外部のリポジトリ（VolTech-MagicOnion・MasterMemoryAddons。いずれも xUnit v2 系）より新しい版を使う。版は実装時に NuGet で最新の安定版を確認して固定する
- **サーバーの結合テストは Testcontainers** で、テストのたびに Docker で PostgreSQL を立ち上げる。本番と同じ DB の挙動で確かめる（Docker が要る）。`server/compose.yaml` の PostgreSQL でも足りるが、テストごとにまっさらな DB を作れることと、技術先行で触る目的で採る
- テストプロジェクトはテスト対象の隣に置く。`Uta.Shared.Domain` は `shared/Uta.Shared.Domain.Tests/`（2026-10-28 決定）
- サーバーは `server/Uta.Server.Tests/` の 1 プロジェクトに、単体テスト・アーキテクチャテスト（NetArchTest。3章）・結合テスト（Testcontainers）をまとめ、フォルダで分ける（2026-10-31 決定。VolTech-MagicOnion の `VolTech.Server.Tests` と同じ形）。Docker が無いと結合テストは通らない
- 使っている版: xunit.v3 4.0.2（2026-10-28 時点の最新の安定版）。サーバーのテストの版（Testcontainers.PostgreSql・NetArchTest.Rules・Mvc.Testing など）は `Directory.Packages.props` が正。実行方式は Microsoft.Testing.Platform（ルートの `global.json`。.NET 10 SDK の `dotnet test` で必要）

### 10.5 アプリのバージョン

- **強制アップデートは実装しない** [決定。2026-10-31]。VolTech-MagicOnion もサーバー側の判定を持たない（予約エラー `UPDATE_REQUIRED` は定義だけで返さない。`external/VolTech-MagicOnion/docs/spec/index.md`）。サーバーは古いアプリを弾かない。個人開発の確認用で、配布したアプリが手元にしかないため。要るようになったら、3.1 のルールで VolTech-MagicOnion に判定を足す
- マスタのスキーマ（定義）や `Uta.Shared.Domain` のルールを変えたら、新しいアプリのバージョンを出し、手元のアプリを入れ直してそろえる。マスタはいまアプリに同梱するので、スキーマの変更はアプリの更新と一緒になる（[masterdata-pipeline.md](master/masterdata-pipeline.md) 6章）。古いアプリのまま接続すると、予測の表示がずれたり、DTO が読めずに失敗したりする（確定はサーバーなので、所持品が壊れることはない）
- `x-app-version` は、クライアントがすべての要求に付ける（VolTech-MagicOnion のクライアントが付ける）。サーバーはプロデュースの破棄の判定（下）にだけ使う。VolTech-MagicOnion に読み出す関数は無いので、Uta の `Api` で MagicOnion の `Context.CallContext.RequestHeaders` から読む（2026-10-31 に VolTech-MagicOnion のコードで確認）
- 進行中のプロデュースは、開始したときとアプリのバージョンが違えば破棄する（2026-10-28 決定。[api-design.md](api-design.md) 5章）。1つのセッションを、常に1つのルールで最後まで進めるため
- **マスタの版・ルールの版は、アプリのバージョンと同じ** [決定。2026-10-31]。サーバーのマスタの値だけを変えるときも、アプリのバージョンを上げる。別の版の番号は持たない（[produce-domain.md](produce-domain.md) 7章）
- **サーバーのルールの版** [決定。2026-11-01。ユーザーの判断]: サーバーだけを更新すると、古いアプリで始めたセッションが新しいルールで続いてしまう。そこで `Uta.Server` に版の定数を 1 つ置き（`RuleVersion`。整数）、プロデュースの開始時にセッションへ保存する。違えば、`app_version` と同じく破棄する（[api-design.md](api-design.md) 5章）。`Uta.Shared.Domain`・マスタ・サーバーのルールを変えたコミットで上げる
- **アプリのバージョンの持ち方** [決定。2026-10-31]: クライアントの `x-app-version` は `Uta.Client.Bootstrap` の定数 1 か所に置き、`Uta.Shared.Domain`・マスタ・DTO を変えたコミットで上げる。値は整数（VolTech-MagicOnion のヘッダーの規約）

### 10.6 クライアントのローカル保存

- **共通基盤の Supplement（`external/Supplement`）の MessagePack 版のセーブデータ（`Supplement.MessagePack`、パッケージ `com.chinpangx.supplement.messagepack`）を使う**（2026-10-27 決定）。`IFileStorageService` / `SaveDataRepository` を MessagePack で読み書きし、AES で暗号化、HMAC-SHA256 で改ざん・破損を検知する（[Supplement の docs](../../../external/Supplement/docs/features/messagepack-save-data.md)）
- MessagePack は通信とマスタでも使うので、Supplement が参照する MessagePack の版を、MagicOnion・MasterMemory とそろえる（8章の要検証）
- 何を保存するか（認証トークン、設定など）は、**実装するときに決める**

## 11. 環境構成とデプロイ [決定／手順は Uta案]

ローカルでテストし、問題がなければデプロイして実機から確かめる。デプロイ先は Google Cloud（[ADR 0011](../decisions/0011-deploy-google-cloud.md)）。

### 11.1 環境

| 環境 | API | DB | 用途 |
|---|---|---|---|
| ローカル（開発） | `dotnet run` / IDE で直接起動 | PostgreSQL を Docker Compose で起動 | 実装・デバッグ。Unity エディターと実機から接続 |
| ローカル（確認。任意） | コンテナ | Docker Compose | デプロイ前に、イメージがそのまま動くかを確かめる。dev にすぐ出せるなら省いてよい |
| dev（Google Cloud） | Cloud Run | Cloud SQL for PostgreSQL | デプロイ後の確認。実機から接続 |

- 本番環境は分けない（商用公開は非目標）
- 結合テストは Testcontainers がテストのたびに DB を立ち上げるので、上の DB とは別（10.4）
- 実機からローカルのサーバーにつなぐときは、`localhost` ではなく PC の LAN の IP を使い、ポートを公開する

### 11.2 工程

1. **ローカルで確かめる**: テスト（CLAUDE.md の「ビルド・テスト・実行」のコマンド。単体テストと結合テスト）を通し、Unity エディターと実機から操作して確かめる
2. **イメージを作る**: `dotnet publish /t:PublishContainer` で API のイメージを作り、Artifact Registry に登録する。サーバー用のマスタのバイナリもイメージに含める
3. **マイグレーションを流す**: DbUp でスキーマを更新する。API と同じイメージの `migrate` コマンドを、Cloud Run のジョブとして流す（ローカルでは `dotnet run --project server/Uta.Server -- migrate`。[ADR 0012](../decisions/0012-migration-dbup.md)）。`migrate` は、VolTech-MagicOnion のテーブル（`VoltechDatabase.Migrate()`）を先に、Uta のテーブル（`Migrations/`）を後に流す。VolTech-MagicOnion のテーブルと Uta のテーブルの履歴は分ける（3.1 の 1）
4. **デプロイする**: Cloud Run のサービスを、新しいイメージで更新する
5. **確かめる**: ヘルスチェックと、実機から「ログイン → ガチャを1回引く」までのスモークテスト
6. **問題があれば前に進めて直す**: dev 環境ではロールバックしない。DbUp はマイグレーションを戻せず、手順 3 でスキーマは進んでいるため、古い API に戻すと新しいスキーマと合わない。直したイメージを作って手順 2 からやり直す [決定。2026-10-31]。本番環境は持たない（11.1）ので、これで足りる

**CI は、最初は上の工程を手動で流し、手順が固まったら GitHub Actions で自動化する**（2026-10-28 決定）。手動の間に、コマンドと順番を `architecture/build-ci.md`（7章の実体）に書き留め、それを Actions のワークフローに移す。どこまで自動化するか（テストだけか、デプロイまでか）は、移すときに決める。

### 11.3 費用を抑える

- Cloud Run は最小のインスタンス数を 0 にする
- Cloud SQL は使わない期間に停止する
- 料金は作る前に公式の料金表で確認する

### 11.4 構成の詳細 [決定。2026-10-28]

| 項目 | 決定 | メリット / デメリット |
|---|---|---|
| リージョン | 東京 `asia-northeast1` | 実機からの遅延が小さい / 米国のリージョンより少し高い |
| 秘密の値 | Secret Manager に置き、Cloud Run の環境変数として渡す | 標準のやり方で、リポジトリに入らない / わずかに費用がかかる（無料枠に収まる見込み。推測） |
| Cloud SQL への接続 | Cloud Run 組み込みの Cloud SQL 接続（Unix ソケット）と、パスワード認証（パスワードは Secret Manager） | 設定が最小 / IAM 認証よりパスワードの管理が要る。プライベート IP は VPC の費用がかかるので採らない |
| マイグレーション | DbUp。`migrate` コマンドを Cloud Run のジョブで流す（[ADR 0012](../decisions/0012-migration-dbup.md)） | 複数のインスタンスで同時に流れない / デプロイの手順が1つ増える |
| Compose のファイル | `server/compose.yaml` | サーバー専用だと分かる / 最上位から起動するときはパスを指定する |
| PostgreSQL の版 [2026-10-31] | 18（イメージは `postgres:18-alpine`）。Compose と Testcontainers でそろえる | VolTech-MagicOnion のテストと同じ版 / Cloud SQL で 18 を選べるかは要確認（8章） |
| ローカルの DB [2026-10-31] | ポート 5432、DB 名 `uta`。パスワードは `server/.env`（`.gitignore` に入れる）から読み、リポジトリに入れない | 標準のポートで設定が要らない / ほかの PostgreSQL がポート 5432 を使っていると衝突する |
| ヘルスチェック | gRPC のヘルスチェック（`grpc.health.v1`、`Grpc.AspNetCore.HealthChecks`）。DB の確認は含めない | HTTP/2 だけで受ける構成に合う。Cloud SQL を止めても API が再起動を繰り返さない / Cloud Run のプローブの設定は要確認（8章） |

- 料金は作る前に公式の料金表で確認する（11.3）

## 12. 実装の進め方 [決定。2026-10-31]

実装計画の単位と順は [game-spec.md](../overview/game-spec.md) の「実装計画の単位と順」が正。ここでは、その 1 番目の計画（**通信の疎通**）に何を入れるかを決める。1 番目の計画は、サーバー・`shared/` の側の **1a「通信の疎通（サーバー）」**（手順 1・2・4。待つもの: なし）と、Unity の側の **1b「通信の疎通（クライアント）」**（手順 5・6。手順 6 は 1a のマージ後。完了条件 0 は 1b が受け持つ）に分ける（2026-11-01。1a と 1b は別の実装計画・別のセッションで進める）。1a は 2026-11-01 に実装した（[計画](../../plans/2026-11-01-connectivity-check-server.md)）。

**方針**: 手をつけやすく、後で問題が出ない順にする。後で問題になりやすいのは、Unity 7（アルファ版）・IL2CPP・HTTP/2 まわりの、動くか分からないもの（8章）。これをゲームの機能を書く前に、最小の構成で通しておく。ゲームの機能（プロデュース・ガチャ）の計画は、1 番目の計画が通るまで始めない（`Uta.Shared.Domain` とマスタの計画は、通信に関係しないので並行してよい）。

1 番目の計画に入れるもの:

| 順 | 中身 | 確かめること（8章の要検証のうち、ここで潰すもの） |
|---|---|---|
| 1 | 2.1 のファイル（`Uta.slnx`・`Directory.Build.props`・`Directory.Packages.props`・`client/Directory.Build.props`）。`Uta.Shared.Domain.Tests` の版を中央管理へ移す | `dotnet build Uta.slnx` と既存のテストが通る |
| 2 | `shared/Uta.Shared.Contracts`（中身は空でよい。`VolTech.Server.Abstractions` を参照するだけ） | 版の衝突が出ない |
| 3 | **（2026-10-31 に済み）VolTech-MagicOnion の変更**（3.1 の 6）: `devices`・`last_responses`・`game_logs` の `player_id` を `uuid` から `char(26)` に変えた（10.1。VolTech-MagicOnion の `f79974d`、Uta のポインタも更新済み）。VolTech-MagicOnion のテスト・CHANGELOG（互換性が壊れる変更）まで済ませて push し、Uta のポインタを上げる | VolTech-MagicOnion のテストが通る |
| 4 | `server/Uta.Server`（下の「サーバーの骨組み」。Uta の RPC は無し）、`server/compose.yaml`、`server/Uta.Server.Tests`（実際は、アーキテクチャテスト 6 件と、端末登録・ヘルスチェック・マイグレーションの結合テスト 7 件） | NetArchTest が xunit.v3 で動く。ローカルで VolTech-MagicOnion の端末登録が通る（**2026-11-01 に確認済み**） |
| 5 | Unity 側: 外部パッケージを 3章の入れ方（NuGetForUnity と UPM）で入れ、`com.chinpangx.voltech.magiconion`・`com.uta.shared.domain`・`com.uta.shared.contracts` を `manifest.json` に入れ、3章の asmdef を作る | `shared/` の `file:` 取り込み、各パッケージが Unity 7 で動く、版が揃う、IL2CPP のビルドが通る（`Microsoft.Bcl.AsyncInterfaces`） |
| 6 | Unity から VolTech-MagicOnion の端末登録（`RegisterAsync` → トークン）を呼ぶ最小の画面または起動処理 | 完了条件 0（Editor と Windows の IL2CPP）。結果は `architecture/unity7-evaluation.md` に書く |

**サーバーの骨組み**（手順 4。VolTech-MagicOnion の `docs/guide/server.md` の形に従う）[決定。2026-10-31]:

- `Program.cs`: `NpgsqlDataSource` を 1 つ登録し、`IPlayerResolver` の実装（`Infrastructure` の `PlayerResolver`。scoped）を登録して、`AddVoltechServer()` を呼ぶ。Kestrel は HTTP/2 だけで待ち受ける（h2c）。gRPC のヘルスチェック（11.4）もここで足す
- **`players` テーブルは最小の形で作る**: 列は `player_id char(26) PRIMARY KEY` と `created_at timestamptz NOT NULL DEFAULT now()` だけ。所持品・通貨の列（[api-design.md](api-design.md) 3章）は、別のマイグレーションとして足す（`players` の Lv・経験値・通貨の列は 5a の計画、育成アイドル・サポートカードなどのテーブルは 5 番目の計画（プレイヤー・ガチャ・強化））。`PlayerResolver` は、行があれば `Active`、無ければ `NotCreated` を返す
- **設定**（`appsettings.json`）: `Voltech:GameDay:ResetHour` は `12`、`Voltech:GameDay:TimeZone` は `"Asia/Tokyo"`（10.2。既定は 0 時・UTC なので必ず書く）。`Voltech:AccessTokenLifetime` は既定の 1 時間のまま
- **接続文字列**は `ConnectionStrings:Default`（VolTech-MagicOnion のガイドと同じ名前）。リポジトリに入れない。ローカルは `dotnet user-secrets`、Cloud Run は環境変数 `ConnectionStrings__Default`（Secret Manager から渡す。11.4）。`server/.env` は Compose の PostgreSQL のパスワードだけに使う
- **`migrate`**: 最初の引数が `migrate` なら、Web ホストを起動せずに、`VoltechDatabase.Migrate(接続文字列)` → Uta の DbUp（`Migrations/*.sql` を埋め込みリソースにし、記録は DbUp の既定の `schemaversions`）の順に流して、終了コード 0 で終わる。失敗したら 0 以外。ほかの引数は取らない（接続文字列は上と同じ経路）
- `x-app-version` を読む処理はこの段階では作らない（プロデュースの計画で、10.5 の方法で作る）

- UnityScreenNavigator・Supplement・MasterMemory は、使う計画（画面・ローカル保存・マスタ）で入れる。1 番目の計画には入れない
- 共通基盤の変更が要るとわかったら、3.1 のルールで共通基盤の側に足す

## 未決事項

- ビルド・CI の実体（`architecture/build-ci.md` として起こす予定。手動の手順を書き留めてから GitHub Actions に移す。11.2）
- UPM の具体の版（Unity 側。1b の実装計画で決める）。NuGet の版は `Directory.Packages.props` が正（2026-11-01 に 1a で決めた。VolTech-MagicOnion と共通のものは VolTech-MagicOnion に合わせる）

（2026-10-31 に解決: ソリューションと版の管理（2.1）、共通基盤の固定と扱い（3.1）、サーバーのテストの置き場（10.4）、PostgreSQL の版・ポート・DB 名（11.4））

（2026-10-28 に解決: `Uta.Shared.Contracts` は Uta 固有のものだけを置き、VolTech-MagicOnion の `Abstractions` を参照する（[api-design.md](api-design.md)）。`Application` は `noEngineReferences` で守る（3章。2026-10-31 に ADR 0008・0009 に合わせて「使わない」に戻した）。アーキテクチャテストは NetArchTest（3章）。ULID の持ち方（10.1）。テストの置き場（10.4）。デプロイの構成（11.4））
