---
title: 命名規則（型の名前の付け方）
status: draft
updated: 2026-11-08
---

# 命名規則（型の名前の付け方）

C# の型の名前を、役割ごとに決める。書式（大文字・小文字、`_` の有無、波括弧）は [CLAUDE.md](../../../CLAUDE.md) の「C# コーディング規約」と `.editorconfig` が正。決めた経緯は [raw の記録](../../raw/2026-11-08-naming-remaining-decisions.md)（と同じ日の 2 本）。

印: [ADR 案] は proposed の ADR（[0020](../decisions/0020-masterdata-referenced-outside-shared-domain.md)・[0021](../decisions/0021-domain-entities-and-repositories.md)・[0022](../decisions/0022-client-external-concretes-in-infrastructure.md)）が根拠の規則。ADR が accepted になるまでは、今のコードと、ほかのページ（[repository-layout.md](repository-layout.md)・[screen-design.md](screen-design.md)）の書き方が正。

## 決まっていること

### 1. 全体の決まり [決定。2026-11-08。ユーザーの判断]

- **サーバーとクライアントで、同じ役割には同じ名前を使う**
- **型の名前に `Uta` を付けない**。名前空間が `Uta.*` なので、役割の名前にする（Prefab の部品も同じ。[screen-design.md](screen-design.md) 7.4）
- **`View` はクライアントの Presentation だけで使う**
- **`Request` は通信の入力（Contracts）だけ**。UseCase の入力は引数で受け、入力の型を作らない（Contracts の `LevelUpRequest` などと同じ名前の型ができるのを避けるため）
- **結果は `Result`**。`Outcome` は使わない
- **Shared.Domain のエンジンへの操作の入力は `Command`**（`LessonCommand`・`ProduceCommand`）
- **表にない役割は、役割を表す名詞を接尾辞にする**（`ProduceCommandNormalizer`、`ProduceStateSerializer`、`CountingTokenProvider`、`PlayerCommonApplier`、`SceneNavigator`、`SecureRandomSource`）。新しい役割の名前を作るときは、同じ接尾辞の既存の型が同じ役割かを確かめる

### 2. 層をまたぐ型

| 種類 | 名前 | 例 | 置き場 |
|---|---|---|---|
| エンティティ [ADR 案] | `<名詞>Entity`。所持を表す `Owned` は付けない | `PlayerEntity`、`IdolEntity` | 各側の Domain（ADR 0021） |
| リポジトリの IF [ADR 案] | `I<名詞>Repository` | `IPlayerRepository` | 各側の Domain |
| リポジトリの IF（読み取り専用。クライアント） [ADR 案] | `IReadOnly<名詞>Repository`（`I<名詞>Repository` がこれを継ぐ） | `IReadOnlyPlayerRepository` | クライアントの Domain |
| リポジトリの実装 | `<名詞>Repository` | `PlayerRepository` | Infrastructure |
| UseCase の IF（クライアント） [ADR 案] | `I<機能>UseCase`。機能ごとに 1 つ（Contracts の `I<機能>Service` と同じ単位） | `IGachaUseCase` | クライアントの Application（ADR 0022） |
| UseCase の具象（サーバー） | `<動詞><名詞>UseCase`。1 つの操作に 1 つ | `DrawUseCase` | サーバーの Application |
| 結果の値 | `<操作>Result`（操作の戻り値）、`<名詞>Result`（戻り値の中の 1 件分） | `DrawResult`、`GachaResult` | 返すメソッドの層（UseCase の戻り値は Application、ルールの戻り値は Domain・Shared.Domain） |
| 結果の種類（enum） | `<分類する Result の名前>Kind` | `GachaResultKind`（`GachaResult` の新規・重複）、`ExamResultKind` | 同上 |
| ある時点の値の写し | `<名詞>Snapshot` | `ProduceSnapshot` | Shared.Domain |
| 状態（class） | `<機能>State` | `LessonState` | Shared.Domain |
| 段階・状態の区分（enum） | `<名詞>Status` | `ProduceSessionStatus` | 使う層 |
| それ以外の分類（enum） | `<名詞>Kind` | `ItemKind`、`CostKind` | 使う層 |
| まとめた値 | `<名詞>Summary`: 表示や結果のために、いくつかの値を読み取り専用にまとめたもの。状態でも結果そのものでもない | `ProduceEndSummary`、`ClassOptionSummary` | 使う層 |

- リポジトリの名前は、エンティティ名から `Entity` を除いたもの
- 結果の種類の enum が、どの `Result` も持たない操作の戻り値（`UseCheckResultKind` のように、enum だけを返す判定）は、`<操作>ResultKind` にする
- 順位・ランクなど、上の区分に当たらない enum は 1章の「表にない役割」に従う（`ProduceRank`）

### 3. 通信（`Uta.Shared.Contracts`）

| 種類 | 名前 | 例 |
|---|---|---|
| サービスの IF | `I<機能>Service` | `IGachaService` |
| RPC の入力 / 出力 | `<操作>Request` / `<操作>Response` | `DrawRequest` / `DrawResponse` |
| 通信の部品 | `<名詞>Dto` | `DeckCardDto` |
| エラーコード | `<機能>Errors`（static） | `GachaErrors` |

- `<名詞>Dto` は Contracts の型だけに付ける。Contracts の型の名前に `View` を含めない（クライアントの表示用の型 `<View 名>Dto` と見分けるため。6章）
- enum の名前と数値の正は、その型を定義するページ（`GachaResultKind` は [api-design.md](api-design.md) 1.1、レッスンの enum は [lesson-enums.md](lesson-enums.md)）

### 4. Shared.Domain

| 種類 | 名前 | 例 |
|---|---|---|
| 計算の本体 / 式 / 判定のルール | `<機能>Engine` / `<機能>Formulas` / `<機能>Rules` | `LessonEngine` / `ProduceFormulas` |
| 検証 | `<名詞>Validator` | `FormationValidator` |
| 定義の集まり | `I<名詞>Catalog` / `<名詞>Catalog` | `ICardCatalog` / `CardCatalog` |
| 組み立て済みの定義 | `<名詞>Definition` | `CardDefinition` |
| マスタの行と同じ列を持つ入力 | `<名詞>Row` | `CardRow` |

- `Row` は組み立てる前の行（マスタの列そのまま）、`Definition` は組み立てた後（グループの解決・入れ子の組み立て済み）
- enum の値の名前は [lesson-enums.md](lesson-enums.md) の「命名の規則」が正

### 5. サーバー

| 種類 | 名前 | 例 |
|---|---|---|
| MagicOnion のサービスの実装 | `<機能>Service` | `GachaService` |
| サーバー固有のルール | `<機能>Rules`、役割の名前 | `SupportRules`、`GachaPicker` |
| DB の 1 行の写し | `<名詞>Record` | `ProduceSessionRecord` |

### 6. クライアントの Presentation

| 種類 | 名前 | 例 |
|---|---|---|
| 画面（Prefab のルート） | `<画面>Page` / `<画面>Modal`（Prefab 名と同じ） | `HomePage`、`GachaResultModal` |
| Presenter が見る画面の IF | `I<画面>View`（`<画面>` は `Page`・`Modal` を除いた名前） | `IHomeView`、`IGachaResultView` |
| 部品 | `<名詞>View`（Prefab 名 + `View`） | `ButtonView` |
| 行 | `<名詞>RowView`（Prefab 名は `<名詞>Row`） | `DeckCardRowView` |
| Presenter | `<画面>Presenter` | `HomePresenter` |
| 前の画面から受け取る情報 | `<画面>Args` | `SupportDetailArgs` |
| View に渡す表示用の値 | 受け取る View の名前 + `Dto`（画面の IF は `I` を除く） | `OwnedRowViewDto`、`WeekViewDto` |
| 表示用の値を作る役 | `<画面>DtoFactory`（画面ごとに 1 つ）。複数の画面で使う行は `<行>DtoFactory`（`SkillCardRowDtoFactory`）を作り、画面の Factory から呼ぶ | `WeekDtoFactory` |
| シーンの DI | `<シーン>LifetimeScope` | `HomeLifetimeScope` |

- Prefab と子の GameObject の名前は [screen-design.md](screen-design.md) 7.4

### 7. 作る役・変える役の名前

| 役 | 名前 | 線引き | 例 |
|---|---|---|---|
| 1 対 1 の変換 | `<名詞>Mapper` | 1 つの入力を、別の形の 1 つに写す（DTO ↔ エンティティ、結果 → レスポンス）。両側で使う | `ProduceResponseMapper` |
| 組み立て | `<名詞>Factory` | 複数の入力から 1 つの値を組み立てる。I/O をしない | `ProduceStateResultFactory`、`CardCatalogFactory`、`ExamTurnFactory`、`WeekDtoFactory` |
| 読み込み | `<名詞>Loader` | ファイル・リソース・ネットワークから読む（I/O がある） | `MasterDataLoader` |
| Editor の生成 | `<名詞>Builder` | Editor のスクリプトがアセット（Prefab・シーン・設定）を作る。Editor 専用 | `SceneBuilder`、`TmpSettingsBuilder` |

### 8. テスト

- テストのクラスは `<対象>Tests`。メソッドは `Method_Condition_Result` を許す（CLAUDE.md）
- クライアントのテストのアセンブリは `Uta.Client.Tests.EditMode`

## 今のコードとの差（改名の対象）

実装計画で直す。直したらこの節から消す。改名するときは、旧い名前を書いている wiki のページ（`systems/`・`architecture/`）を Grep で探し、同じ変更で直す。

| 今 | 新 | 根拠 |
|---|---|---|
| Contracts の `GachaOutcome` | `GachaResultKind` | 1章。正は [api-design.md](api-design.md) 1.1 なので、そこも直す |
| Shared.Domain の `ExamOutcome`、クライアントの `PlayerSignInOutcome`、サーバーの `GrantOutcome` | `ExamResultKind`・`PlayerSignInResultKind`・`GrantResultKind` | 1章・2章 |
| Shared.Domain の enum の `UseCheckResult`・`LessonApplyResult`・`ProduceApplyResult` | `UseCheckResultKind`・`LessonApplyResultKind`・`ProduceApplyResultKind` | 2章 |
| プロパティ名の `Outcome`・`ExamOutcome`（`DrawResultDto.Outcome`、`ProduceResultResponse.ExamOutcome` など） | `ResultKind`・`ExamResultKind` | 1章。MessagePack はキーの番号で読み書きするので通信に影響しない。DB の列名（`exam_outcome`）は変えない |
| クライアントの `AccountState`（enum） | `AccountStatus` | 2章（`State` は class） |
| サーバーの `ProduceView`・`ProduceViewBuilder` | `ProduceStateResult`・`ProduceStateResultFactory` | 1章（`View` はクライアントだけ）・7章 |
| Shared.Domain の `ClassOptionView`（`ProduceEngine.GetClassOptions` の戻り値） | `ClassOptionSummary` | 1章・2章。サーバーとクライアントの利用箇所も直す |
| サーバーの `GachaHistoryRow` | `GachaHistoryRecord` | 5章 |
| `UtaServiceAttribute`・`UtaDatabase`・`UtaApiClient`・`UtaErrors`・`UtaMagicOnionClientInitializer`、テストの `UtaServerFactory` と `Services_HaveUtaServiceAttribute` | `GameServiceFilterAttribute`・`DatabaseMigrator`・`ApiClient`・`ErrorDefinitions`・`MagicOnionClientInitializer`、`ServerFactory`・`Services_HaveGameServiceFilterAttribute` | 1章 |
| `MasterCardCatalog`・`MasterProduceCatalog`（static の組み立て役） | `CardCatalogFactory`・`ProduceCatalogFactory` | 7章 |
| Shared.Domain の `ExamTurnBuilder` | `ExamTurnFactory` | 7章（`Builder` は Editor だけ） |
| 計画にある `CardSummaryBuilder` | `CardSummaryFactory` | 7章 |
| クライアントの `GachaLineRow` | `GachaLineRowView` | 6章 |
| Bootstrap の `ErrorDialogView`（診断用の IMGUI） | 置き場（Presentation に移す）か名前（`View` を外す）を、触る実装計画で決める | 1章 |
| `Presentation/Home/HomeView.cs`・`Title/TitleView.cs`・`Produce/ProduceView.cs`・`Startup/StartupView.cs` | 使われているかを確かめて片付ける | 6章（Prefab の画面を作る前の View とみられる） |
| サーバーの `I<名詞>Catalog`・`Master<名詞>Catalog`・`GachaDefinition` など | やめる [ADR 案] | ADR 0020 |
| `OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`（サーバー・クライアント） | `IdolEntity`・`SupportCardEntity`・`PlayerEntity` [ADR 案] | 2章、ADR 0021 |
| クライアントの `PlayerCache` | `IPlayerRepository` の実装 [ADR 案] | ADR 0021 |
| クライアントの `IPlayerPort`・`IDeviceAuthPort`・`PlayerGateway`・`DeviceAuthGateway`・`EnterPlayerUseCase`・`SignInUseCase` と、その結果の型（`PlayerSignInResult` など） | UseCase の IF と Infrastructure の具象 [ADR 案] | ADR 0022 |
| 計画にある `<名詞>ViewData` | `<View 名>Dto` | 6章 |
| `Uta.Client.Infrastructure.Tests.EditMode` | `Uta.Client.Tests.EditMode` | 8章 |

- `ApiClient`・`MagicOnionClientInitializer` は、VolTech-MagicOnion・MagicOnion に同じ名前の型があるかを、実装のときにコンパイルで確かめる
- `PlayerApi` は [基盤移行の計画](../../plans/2026-11-08-client-foundation-migration.md) が消す

## 未決事項

- クライアントの UseCase の具象の名前（IF と対にして `<機能>UseCase` にする案）。ADR 0022 を実装する計画で決める
- サーバーの `ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord` をエンティティにするか、どの名前にするか。ADR 0021 を実装する計画で決める
