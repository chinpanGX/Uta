---
title: 命名規則（型の名前の付け方）
status: draft
updated: 2026-11-08
---

# 命名規則（型の名前の付け方）

C# の型の名前を、役割ごとに決める。書式（大文字・小文字、`_` の有無、波括弧）は [CLAUDE.md](../../../CLAUDE.md) の「C# コーディング規約」と `.editorconfig` が正。決めた経緯は [raw の記録](../../raw/2026-11-08-naming-remaining-decisions.md)（と同じ日の 2 本）。

## 決まっていること

### 1. 全体の決まり [決定。2026-11-08。ユーザーの判断]

- **サーバーとクライアントで、同じ役割には同じ名前を使う**
- **型の名前に `Uta` を付けない**。名前空間が `Uta.*` なので、役割の名前にする（Prefab の部品も同じ。[screen-design.md](screen-design.md) 7.4）
- **`View` はクライアントの Presentation だけで使う**
- **要求は `Request`、結果は `Result`**。`Request` は通信と UseCase の入力に限る。`Outcome` は使わない
- **Shared.Domain のエンジンへの操作の入力は `Command`**（`LessonCommand`・`ProduceCommand`）

### 2. 層をまたぐ型

| 種類 | 名前 | 例 | 置き場 |
|---|---|---|---|
| エンティティ | `<名詞>Entity`。所持を表す `Owned` は付けない | `PlayerEntity`、`IdolEntity` | 各側の Domain（[ADR 0021](../decisions/0021-domain-entities-and-repositories.md)） |
| リポジトリの IF（読み取り） | `IReadOnly<名詞>Repository` | `IReadOnlyPlayerRepository` | 各側の Domain |
| リポジトリの IF（書き込み） | `I<名詞>Repository : IReadOnly<名詞>Repository` | `IPlayerRepository` | 各側の Domain |
| リポジトリの実装 | `<名詞>Repository` | `PlayerRepository` | Infrastructure |
| UseCase の IF（クライアント） | `I<機能>UseCase`。機能ごとに 1 つ | `IGachaUseCase` | クライアントの Application（[ADR 0022](../decisions/0022-client-external-concretes-in-infrastructure.md)） |
| UseCase の具象 | `<機能>UseCase`（クライアント）、`<動詞><名詞>UseCase`（サーバー。今のまま） | `GachaUseCase`、`DrawUseCase` | クライアントは外部に触れるなら Infrastructure、触れないなら Application。サーバーは Application |
| 結果の値 | `<操作>Result`（record・class） | `DrawResult`、`ProduceStateResult` | 返す層 |
| 結果の種類 | `<操作>ResultKind`（enum） | `GachaResultKind`、`ExamResultKind` | 返す層 |
| ある時点の値の写し | `<名詞>Snapshot` | `ProduceSnapshot` | Shared.Domain |
| 状態 | `<機能>State` | `LessonState` | Shared.Domain |
| 種類の enum | `<名詞>Kind` | `ItemKind`、`CostKind` | 使う層 |

- リポジトリの名前は、エンティティ名から `Entity` を除いたもの
- `IReadOnly<名詞>Repository` と `I<名詞>Repository` は、実装 1 つを両方の IF で DI に登録する

### 3. 通信（`Uta.Shared.Contracts`）

| 種類 | 名前 | 例 |
|---|---|---|
| サービスの IF | `I<機能>Service` | `IGachaService` |
| RPC の入力 / 出力 | `<操作>Request` / `<操作>Response` | `DrawRequest` / `DrawResponse` |
| 通信の部品 | `<名詞>Dto` | `DeckCardDto` |
| エラーコード | `<機能>Errors`（static） | `GachaErrors` |

- `<名詞>Dto` は Contracts の型だけに付ける（クライアントの表示用の型は 6章の `<View 名>Dto`）

### 4. Shared.Domain

| 種類 | 名前 | 例 |
|---|---|---|
| 計算の本体 / 式 / 判定のルール | `<機能>Engine` / `<機能>Formulas` / `<機能>Rules` | `LessonEngine` / `ProduceFormulas` |
| 検証 | `<名詞>Validator` | `FormationValidator` |
| 定義の集まり | `I<名詞>Catalog` / `<名詞>Catalog` | `ICardCatalog` / `CardCatalog` |
| 定義の行 | `<名詞>Row`・`<名詞>Definition` | `CardRow`、`CardDefinition` |

- enum の値の名前は [lesson-enums.md](lesson-enums.md) の「命名の規則」が正

### 5. サーバー

| 種類 | 名前 | 例 |
|---|---|---|
| MagicOnion のサービスの実装 | `<機能>Service` | `GachaService` |
| 結果 → レスポンスの詰め替え | `<名詞>Mapper` | `ProduceResponseMapper` |
| サーバー固有のルール | `<機能>Rules`、役割の名前 | `SupportRules`、`GachaPicker` |

### 6. クライアントの Presentation

| 種類 | 名前 | 例 |
|---|---|---|
| 画面（Prefab のルート） | `<画面>Page` / `<画面>Modal`（Prefab 名と同じ） | `HomePage`、`GachaResultModal` |
| Presenter が見る画面の IF | `I<画面>View` | `IHomeView` |
| 部品・行 | `<名詞>View` / `<名詞>RowView` | `ButtonView`、`DeckCardRowView` |
| Presenter | `<画面>Presenter` | `HomePresenter` |
| 前の画面から受け取る情報 | `<画面>Args` | `SupportDetailArgs` |
| View に渡す表示用の値 | 受け取る View の名前 + `Dto`（画面の IF は `I` を除く） | `OwnedRowViewDto`、`WeekViewDto` |
| 表示用の値を作る役 | `<画面>DtoFactory`（画面ごとに 1 つ） | `WeekDtoFactory` |
| シーンの DI | `<シーン>LifetimeScope` | `HomeLifetimeScope` |

- Prefab と子の GameObject の名前は [screen-design.md](screen-design.md) 7.4

### 7. 作る役の名前

- 値を組み立てて返す役は `<名詞>Factory`（`ProduceStateResultFactory`、`CardSummaryFactory`）
- マスタから Shared.Domain の型を組み立てる役は `<名詞>Loader`（`CardCatalogLoader`、`MasterDataLoader`）
- Prefab の生成スクリプトは `<名詞>Builder`（`IPrefabBuilder` の実装。Editor 専用）

### 8. テスト

- テストのクラスは `<対象>Tests`。メソッドは `Method_Condition_Result` を許す（CLAUDE.md）
- クライアントのテストのアセンブリは `Uta.Client.Tests.EditMode`

## 今のコードとの差（改名の対象）

実装計画で直す。直したらこの節から消す。

| 今 | 新 | 根拠 |
|---|---|---|
| `GachaOutcome`・`ExamOutcome`・`PlayerSignInOutcome`・`GrantOutcome` | `…ResultKind` | 1章 |
| enum の `UseCheckResult`・`LessonApplyResult`・`ProduceApplyResult` | `…ResultKind` | 1章 |
| プロパティ名の `Outcome`（`DrawResultDto.Outcome` など） | `ResultKind` | 1章。DB の列名（`exam_outcome`）は変えない |
| サーバーの `ProduceView`・`ProduceViewBuilder`・`ClassOptionView` | `ProduceStateResult`・`ProduceStateResultFactory`・`ClassOptionSummary` | 1章（`View` はクライアントだけ） |
| `UtaServiceAttribute`・`UtaDatabase`・`UtaApiClient`・`UtaErrors`・`UtaMagicOnionClientInitializer` | `GameServiceFilterAttribute`・`DatabaseMigrator`・`ApiClient`・`ErrorDefinitions`・`MagicOnionClientInitializer` | 1章。`ApiClient`・`MagicOnionClientInitializer` は基盤・MagicOnion と名前がぶつからないかを実装のときに確かめる |
| `MasterCardCatalog`・`MasterProduceCatalog`（static の組み立て役） | `CardCatalogLoader`・`ProduceCatalogLoader` | 7章 |
| `CardSummaryBuilder`（計画にある） | `CardSummaryFactory` | 7章 |
| サーバーの `I<名詞>Catalog`・`Master<名詞>Catalog`・`GachaDefinition` など | やめる | [ADR 0020](../decisions/0020-masterdata-referenced-outside-shared-domain.md) |
| `OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`（サーバー・クライアント）、サーバーの `ProduceSessionData` | `IdolEntity`・`SupportCardEntity`・`PlayerEntity`・`ProduceSessionEntity` | 2章、ADR 0021 |
| クライアントの `PlayerCache` | `IPlayerRepository` の実装 | ADR 0021 |
| `IPlayerPort`・`IDeviceAuthPort`・`PlayerGateway` | UseCase の IF と Infrastructure の具象 | ADR 0022 |
| 計画にある `<名詞>ViewData` | `<View 名>Dto` | 6章 |
| `Uta.Client.Infrastructure.Tests.EditMode` | `Uta.Client.Tests.EditMode` | 8章 |
| `Presentation/Home/HomeView.cs`・`Title/TitleView.cs`・`Produce/ProduceView.cs`・`Startup/StartupView.cs` | 使われているかを確かめて片付ける | 6章（Prefab の画面を作る前の View とみられる） |

## 未決事項

- `ProduceSessionData` のエンティティ名（`ProduceSessionEntity` は推測。ADR 0021 のレビューで決める）
- クライアントの UseCase の具象の名前（`<機能>UseCase` で IF と対にする案。ADR 0022 のレビューで決める）
