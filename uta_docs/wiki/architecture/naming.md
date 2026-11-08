---
title: 命名規則（型の名前の付け方）
status: draft
updated: 2026-11-08
---

# 命名規則（型の名前の付け方）

C# の型の名前を、役割ごとに決める。書式（大文字・小文字、`_` の有無、波括弧）は [CLAUDE.md](../../../CLAUDE.md) の「C# コーディング規約」と `.editorconfig` が正。

決めた経緯は raw の 3 本（[命名と層](../../raw/2026-11-08-naming-and-layers-decisions.md)・[Presentation](../../raw/2026-11-08-presentation-rules-decisions.md)・[残りの決定](../../raw/2026-11-08-naming-remaining-decisions.md)）。その後の設計書のレビュー（2026-11-08）でユーザーが変えた決定（UseCase の入力に型を作らない、結果の種類の enum の形、Factory と Loader の線、`Summary` をやめる）と、ready の計画の読み替え（2026-11-08）で決めたこと（クライアントの UseCase の具象の名前、`Find`・`FindAll`、Contracts の enum の写しを作らない、Shared.Domain と同名にしない、`Snapshot` の使いどころ、テストの補助の名前）は、このページを直したコミットのメッセージにある（raw は書き換えない）。

印: [ADR 案] は ADR（[0020](../decisions/0020-masterdata-referenced-outside-shared-domain.md)・[0021](../decisions/0021-domain-entities-and-repositories.md)・[0022](../decisions/0022-client-external-concretes-in-infrastructure.md)）が根拠の規則。3 本とも 2026-11-08 に accepted になった（[基盤移行の計画](../../plans/2026-11-08-client-foundation-migration.md)の PR）ので、[ADR 案] の印の規則も正。サーバーに関わる規則は、サーバー側の実装計画までサーバーのコードは今のまま（[ADR 0022](../decisions/0022-client-external-concretes-in-infrastructure.md)「accepted にする時点と、進めてよいこと」）。

## 決まっていること

### 1. 全体の決まり [決定。2026-11-08。ユーザーの判断]

- **サーバーとクライアントで、同じ役割には同じ名前を使う**
- **型の名前に `Uta` を付けない**。名前空間が `Uta.*` なので、役割の名前にする（Prefab の部品も同じ。[screen-design.md](screen-design.md) 7.4）
- **`View` はクライアントの Presentation だけで使う**
- **`Request` は通信の入力（Contracts）だけ**。UseCase の入力は引数で受け、入力の型を作らない（Contracts の `LevelUpRequest` などと同じ名前の型ができるのを避けるため）
- **結果は `Result`**。`Outcome` は使わない
- **Shared.Domain のエンジンへの操作の入力は `Command`**（`LessonCommand`・`ProduceCommand`）
- **Application の型は、Contracts の型と同じ名前にしない**（Infrastructure とテストは両方を参照するため）。**Contracts の enum のクライアントの写しは作らない**: Contracts の enum を引数に取る RPC は、UseCase の IF のメソッドを値ごとに分ける（`PickCardOfferAsync`・`DeclineCardOfferAsync` など）。結果に入る enum で写しが要るものだけ、Contracts と違う名前にする（`ProduceEnding` → `ProduceEndKind`）[決定。2026-11-08。ユーザーの判断]
- **クライアント・サーバーの型は、Shared.Domain の型と同じ名前にしない**（両方を参照するファイルで CS0104 になるため。例: Shared.Domain の保存用の写し `ProduceSnapshot` があるので、クライアントの `ProduceStateResponse` の写しは `ProduceStateResult`）。生成したマスタの型と Shared.Domain の型が同じ名前になるのは、4章のとおり許す
- **表にない役割は、役割を表す名詞を接尾辞にする**。使ってよい接尾辞と意味は 9章。新しい接尾辞を作る前に、9章の語で言えないかを確かめる

### 2. 層をまたぐ型

| 種類 | 名前 | 例 | 置き場 |
|---|---|---|---|
| エンティティ [ADR 案] | `<名詞>Entity`。所持を表す `Owned` は付けない | `PlayerEntity`、`IdolEntity` | 各側の Domain（ADR 0021） |
| エンティティの ID [決定。2026-11-08。ユーザーの判断] | サーバーが採番する ULID は `UniqueId`（`string`）、マスタの `int` の ID は `MasterId`（`int`）。持つものだけ持つ（`PlayerEntity` は `UniqueId`、`IdolEntity`・`SupportCardEntity` は両方）。Contracts の DTO も同じ名前にし（下の「Contracts の ID」）、Mapper は同じ名前どうしで写す（`PlayerEntity.UniqueId` は `SignInResponse.PlayerId` から）[2026-11-08。R9 で改めた] | `IdolEntity.UniqueId`、`IdolEntity.MasterId` | クライアントの Domain |
| Contracts の ID（DTO・RPC のリクエスト・レスポンス）[決定。2026-11-08。ユーザーの判断] | 名前はクライアントとサーバーで統一する。DB の列名は今のまま（C# では Dapper の `AS` で写す）。1. DTO（またはサーバーの型）がそのものを表すときは `UniqueId`（サーバーが採番する ULID。`string`、サーバーの型では `Ulid`）・`MasterId`（マスタの `int`）。2. ほかのものを指すときは `<名詞>UniqueId`・`<名詞>MasterId`（配列は `…UniqueIds`）。3. `PlayerId` はそのまま。プロデュースの中で振る番号（`InstanceId`・`CardInstanceId`）は ULID でもマスタの ID でもないので変えない。4. MessagePack の `[Key(n)]` の番号は変えない（プロパティ名とコンストラクターの引数名だけを変える）。コンストラクターの引数名・IService の RPC の引数も同じ規則（camelCase）。サーバーの所持品の型 `OwnedIdol`・`OwnedSupportCard` も `UniqueId`・`MasterId`。それ以外のサーバーの内部の型・DB の列・マスタの列は対象外（改名の一覧は [移行計画](../../plans/2026-11-08-client-foundation-migration.md) の R9） | `OwnedIdolDto.UniqueId`・`OwnedIdolDto.MasterId`、`DrawResultDto.ItemMasterId`、`LimitBreakRequest.MaterialUniqueId`、`StartProduceRequest.SupportCardUniqueIds` | `Uta.Shared.Contracts`、サーバーの `OwnedIdol`・`OwnedSupportCard` |
| リポジトリの IF [ADR 案] | `I<名詞>Repository` | `IPlayerRepository` | 各側の Domain |
| リポジトリの IF（読み取り専用。クライアント） [ADR 案] | `IReadOnly<名詞>Repository`（`I<名詞>Repository` がこれを継ぐ） | `IReadOnlyPlayerRepository` | クライアントの Domain |
| リポジトリの実装 | `<名詞>Repository` | `PlayerRepository` | Infrastructure |
| リポジトリの取得のメソッド [ADR 案] | 1 件は `Find`（無ければ null）、全件は `FindAll`。サーバーは `FindAsync`・`FindAllAsync` | `Find(uniqueId)`、`FindAll()`。クライアントの 1 件の取得・置き換え・削除は `UniqueId` で行う（2026-11-08） | — |
| UseCase の IF（クライアント） [ADR 案] | `I<機能>UseCase`。機能ごとに 1 つ（Contracts の `I<機能>Service` と同じ単位） | `IGachaUseCase` | クライアントの Application（ADR 0022） |
| UseCase の具象（クライアント） [ADR 案] | `<機能>UseCase`（IF から `I` を除いたもの） | `GachaUseCase`、`PlayerUseCase` | クライアントの Infrastructure（ADR 0022） |
| UseCase の具象（サーバー） | `<動詞><名詞>UseCase`。1 つの操作に 1 つ | `DrawUseCase` | サーバーの Application |
| UseCase の共通の戻り値（クライアント） [ADR 案] | `UseCaseResult<T>`（業務エラーの reason を Presenter に渡す。Presenter が reason で文言を分ける操作があるときだけ作る。ADR 0022） | — | クライアントの Application |
| 結果の値 | `<操作>Result`（操作の戻り値）、`<名詞>Result`（戻り値の中の 1 件分） | `DrawResult`、`GachaResult`、`ProduceEndResult` | 返すメソッドの層（UseCase の戻り値は Application、ルールの戻り値は Domain・Shared.Domain） |
| 結果の種類（enum） | `<何の結果か>ResultKind`。親の `Result` の型が無くてもよい | `GachaResultKind`（ガチャの 1 枠が新規か重複か）、`ExamResultKind`、`UseCheckResultKind` | 同上 |
| ある時点の値の写し（保存・復元のための平らな写し） | `<名詞>Snapshot`。RPC の応答の写し（UseCase の戻り値）には使わず、`<操作>Result` にする | `ProduceSnapshot`（Shared.Domain） | 使う層 |
| 状態（class・record） | `<名詞>State` | `LessonState`、`RandomStreamState` | 使う層 |
| 保存される進行の段階（enum） | `<名詞>Status` | `ProduceSessionStatus` | 使う層 |
| それ以外の分類（enum） | `<名詞>Kind` | `ItemKind`、`CostKind` | 使う層 |

- リポジトリの名前は、エンティティ名から `Entity` を除いたもの
- エンティティ・リポジトリのフォルダと名前空間は機能ごとに分ける（クライアントの Domain・Infrastructure の `Player`・`Idol`・`SupportCard`。[repository-layout.md](repository-layout.md) 2章）[決定。2026-11-08。ユーザーの判断]
- enum の区分の優先: 操作の結果を表すなら `ResultKind`、保存される進行の段階なら `Status`、それ以外の分類なら `Kind`
- **既にある enum で、上の区分に当たらないものは名前を変えない**。用語集の語・ゲームの語そのもの（`Plan`・`Rarity`・`ProduceRank`）、ゲームの局面（`ProducePhase`・`LessonPhase`。用語集の「局面」）、理由（`LessonEndReason`）、終わり方（Contracts の `ProduceEnding`）、検査の結果（`FormationCheck`）、種類（`CardType`・`SupportType`）、エラー（`UseCaseError`）。新しく作る enum は、上の区分か、この行の語（`Phase`・`Reason`）から選ぶ。`Type` は新しく使わない（`Kind` にする）

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
| 選べるものの 1 つ | 接尾辞なしの名詞 | `ClassOption`、`WeekChoice` |

- Shared.Domain の型と生成したマスタの型が同じ名前になるとき（`ClassOption` と `Uta.MasterData.ClassOption`）は、名前を変えない。同じファイルで両方を使うときは、Shared.Domain の名前空間を `using` し、マスタの型は名前空間付きで書く（[ADR 0020](../decisions/0020-masterdata-referenced-outside-shared-domain.md) の enum と同じ）
- `Row` は組み立てる前の行（マスタの列そのまま）、`Definition` は組み立てた後（グループの解決・入れ子の組み立て済み）
- enum の値の名前は [lesson-enums.md](lesson-enums.md) の「命名の規則」が正

### 5. サーバー

| 種類 | 名前 | 例 |
|---|---|---|
| MagicOnion のサービスの実装 | `<機能>Service` | `GachaService` |
| サーバー固有のルール | `<機能>Rules`、役割の名前（9章） | `SupportRules`、`GachaPicker` |
| DB の 1 行の写し（エンティティにしないもの） | `<名詞>Record` | `GachaHistoryRecord` |

- DB の 1 行の写しをエンティティにするか `Record` にするかは、ADR 0021 を実装する計画で型ごとに決める（未決事項）

### 6. クライアントの Presentation

| 種類 | 名前 | 例 |
|---|---|---|
| 画面（Prefab のルート） | `<画面>Page` / `<画面>Modal`（Prefab 名と同じ） | `HomePage`、`GachaResultModal` |
| Presenter が見る画面の IF | `I<画面>View`（`<画面>` は `Page`・`Modal` を除いた名前） | `IHomeView`、`IGachaResultView` |
| 部品 | `<名詞>View`（Prefab 名 + `View`） | `ButtonView` |
| 行 | `<名詞>RowView`。Prefab 名は `<名詞>Row`。いくつかの行の Prefab が 1 つの View を使うときは、View の名前を共通の名詞にする | `DeckCardRowView`、`GachaLineRowView`（Prefab は `GachaRateHeadingRow`・`GachaRateRow`・`GachaResultRow`） |
| Presenter | `<画面>Presenter` | `HomePresenter` |
| 前の画面から受け取る情報 | `<画面>Args` | `SupportDetailArgs` |
| View に渡す表示用の値 | 受け取る View の名前 + `Dto`（画面の IF は `I` を除く） | `OwnedRowViewDto`、`WeekViewDto` |
| 表示用の値を作る役 | `<画面>DtoFactory`（画面ごとに 1 つ）。表示用の Dto を作る役は、入力の数にかかわらず必ず DtoFactory（Mapper にしない）。複数の画面で使う行は、行の View の名前から `View` を除いた `<行>DtoFactory` を作り、画面の Factory から呼ぶ。共用の行の Dto は表示の値だけを持ち、画面ごとの ID（添字・インスタンス ID）は画面の Dto に持たせる | `WeekDtoFactory`、`SkillCardRowDtoFactory` |
| シーンの DI | `<シーン>LifetimeScope` | `HomeLifetimeScope` |

- Prefab と子の GameObject の名前は [screen-design.md](screen-design.md) 7.4

### 7. 作る役・変える役の名前

| 役 | 名前 | 線引き | 例 |
|---|---|---|---|
| 1 対 1 の変換 | `<対象>Mapper` | 変換元が 1 つ（マスタを引くだけなら 1 つと数える）で、別の形の 1 つに写す（DTO ↔ エンティティ、結果 → レスポンス）。表示用の Dto は 6章の DtoFactory で、Mapper にしない。`<対象>` は扱う業務の名詞で、変換の向きはメソッド名（`ToEntity`・`ToResponse`）で表す。両側で使う | `ProduceResponseMapper`、`PlayerMapper` |
| 組み立て | `<名詞>Factory` | 変換元が 2 つ以上、または計算を伴って 1 つの値を組み立てる。ファイル・ネットワークの I/O をしない（DI で受けたリポジトリ・マスタを読むのはよい） | `ProduceStateResultFactory`、`CardCatalogFactory`、`ExamTurnFactory`、`WeekDtoFactory` |
| 読み込み | `<名詞>Loader` | ファイル・リソース・ネットワークから読む（I/O がある） | `MasterDataLoader` |
| Editor の生成 | `<名詞>Builder` | Editor のスクリプトがアセット（Prefab・シーン・設定）を作る。Editor 専用。型の中の private の組み立て役（`CardCatalog` の入れ子の `Builder`）は対象外 | `SceneBuilder`、`TmpSettingsBuilder` |

### 8. テスト

- テストのクラスは `<対象>Tests`。メソッドは `Method_Condition_Result` を許す（CLAUDE.md）
- テストの偽物は `Fake<IF の名前から I を除いたもの>`（`FakeGachaService`、`FakeGachaUseCase`）
- クライアントのテストのアセンブリは `Uta.Client.Tests.EditMode`
- テストのデータを組み立てる static の補助は `Test<名詞>`（`TestMasterData`・`TestProduce`）。置き場は `Tests/EditMode/Support/`。ready の計画にある `GachaFixtures`・`LessonTestData` は計画の名前のまま（実装のときに改名しない）

### 9. 補助の役割の接尾辞

1章の「表にない役割」で使う語。今のコードにあるもの。

| 接尾辞 | 意味 | 例 |
|---|---|---|
| `Provider` | 基盤・ライブラリの IF に合わせて値を供給する | `CountingTokenProvider` |
| `Source` | 値の出どころ（乱数など） | `SecureRandomSource`、`IRandomStreamSource` |
| `Applier` | 受け取った値を、リポジトリなどに反映する | `PlayerCommonApplier` |
| `Normalizer` | 入力を決まった形に直す | `ProduceCommandNormalizer` |
| `Serializer` | バイト列との変換 | `ProduceStateSerializer` |
| `Resolver` | ID などから対象を引き当てる | `PlayerResolver` |
| `Navigator` | 画面・シーンの遷移 | `SceneNavigator`、`ErrorNavigator` |
| `Executor` / `Evaluator` | Shared.Domain の効果を実行する / 条件を評価する | `EffectExecutor`、`ConditionEvaluator` |
| `Picker` / `Granter` | 候補から選ぶ / 所持品を付与する | `GachaPicker`、`ItemGranter` |
| `Calculator` | 表示などのための値を計算する（Shared.Domain の `Formulas` にしない、片側だけの計算） | `GachaRateCalculator`（計画） |
| `Opener` | ダイアログなどを出して結果を待つ | `DialogOpener`（計画） |
| `Handler` | 基盤から呼ばれる受け口（エラー・進行中の通知など） | `DefaultCommonErrorHandler` |
| `Options` | 起動時の設定の値 | `StartupOptions` |
| `Entry` | シーンの入口 | `BootstrapEntry` |

- `PlayerCommonApplier` は計画にある型（まだコードに無い）

## 今のコードとの差（改名の対象）

実装計画で直す。直したらこの節から消す。改名するときは、旧い名前を書いている wiki のページ（`systems/`・`architecture/`）を Grep で探し、同じ変更で直す。

| 今 | 新 | 根拠 |
|---|---|---|
| Contracts の `GachaOutcome` | `GachaResultKind` | 1章・2章。正は [api-design.md](api-design.md) 1.1 なので、そこも直す |
| Shared.Domain の `ExamOutcome`、サーバーの `GrantOutcome` | `ExamResultKind`・`GrantResultKind` | 2章 |
| Shared.Domain の enum の `UseCheckResult`・`LessonApplyResult`・`ProduceApplyResult` | `UseCheckResultKind`・`LessonApplyResultKind`・`ProduceApplyResultKind` | 2章 |
| プロパティ名の `Outcome`・`ExamOutcome`（`DrawResultDto.Outcome`、`ProduceResultResponse.ExamOutcome` など） | `ResultKind`・`ExamResultKind` | 1章。MessagePack はキーの番号で読み書きするので通信に影響しない。DB の列名（`exam_outcome`）は変えない |
| サーバーの `GachaStatus`（record。`GetGachasUseCase` の戻り値の 1 件分） | `<名詞>Result` の形の名前（`GachaResult` は使用済みなので、触る実装計画で決める） | 2章（`Status` は enum） |
| サーバーの `ProduceView`・`ProduceViewBuilder` | `ProduceStateResult`・`ProduceStateResultFactory` | 1章（`View` はクライアントだけ）・7章 |
| サーバーの `ProduceEndSummary` | `ProduceEndResult` | 2章 |
| Shared.Domain の `ClassOptionView`（`ProduceEngine.GetClassOptions` の戻り値） | `ClassOption` | 1章・4章。サーバーとクライアントの利用箇所も直す |
| サーバーの `GachaHistoryRow` | `GachaHistoryRecord`（エンティティにしないとき） | 5章 |
| `UtaServiceAttribute`・`UtaDatabase`、テストの `UtaServerFactory` と `Services_HaveUtaServiceAttribute` | `GameServiceFilterAttribute`・`DatabaseMigrator`、`ServerFactory`・`Services_HaveGameServiceFilterAttribute` | 1章 |
| サーバーの `MasterCardCatalog`・`MasterProduceCatalog`（static の組み立て役） | `CardCatalogFactory`・`ProduceCatalogFactory` | 7章 |
| Shared.Domain の `ExamTurnBuilder` | `ExamTurnFactory` | 7章（`Builder` は Editor だけ） |
| 計画にある `CardSummaryBuilder`（Application）と `CardSummary`（型） | 作らない。行の `SkillCardRowDtoFactory` がマスタを直接読んで Dto を作り、`DisplayText.Card`・`DisplayText.Costs`・`LessonCardText` は Dto を受ける形にする | 6章、ADR 0020 |
| 計画にある `OwnedListBuilder`・`DialogService` | `OwnedListFactory`・`DialogOpener` | 7章・9章（`Builder` は Editor、`Service` は通信） |
| 計画にある `OwnedSelectionRequest` | `Request` を外した名前（`OwnedSelection` は owned の計画の結果の型と同じ名前なので使わない。owned の読み替えで決める） | 1章（`Request` は通信だけ） |
| 計画にある `ProduceOutcome`・`ProduceEndingKind` | `ProduceEndResult`・`ProduceEndKind`（Contracts の `ProduceEnding` と同じ名前にしない） | 1章・2章 |
| 計画にある Contracts の enum のクライアントの写し（`LessonActionType`・`CardOfferAction`・`MasterItemKind`） | 作らない（`IProduceUseCase` のメソッドを操作ごとに分ける。`MasterItemKind` はマスタの `ItemKind`） | 1章 |
| 計画にある `ProduceProgress`・`ProduceSession` | `ProduceStateResult`・`ProduceSessionState` | 1章（Shared.Domain の `ProduceSnapshot` と同名にしない）・2章 |
| 計画にある操作の結果の enum `<名詞>Status`（`ProduceStartStatus`・`DrawStatus`・`SupportStatus` など） | `<操作>ResultKind` | 2章（`Status` は保存される進行の段階） |
| サーバーのリポジトリの `ListAsync` | `FindAllAsync` [ADR 案] | 2章、ADR 0021 |
| クライアントの `GachaLineRow` | `GachaLineRowView` | 6章 |
| Bootstrap の `ErrorDialogView`（診断用の IMGUI） | 置き場（Presentation に移す）か名前（`View` を外す）を、触る実装計画で決める | 1章 |
| `Presentation/Home/HomeView.cs`・`Title/TitleView.cs`・`Produce/ProduceView.cs`・`Startup/StartupView.cs` | 使われているかを確かめて片付ける | 6章（Prefab の画面を作る前の View とみられる） |
| サーバーの `I<名詞>Catalog`・`Master<名詞>Catalog`・`GachaDefinition` など | やめる [ADR 案] | ADR 0020 |
| サーバーの `OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot` | `IdolEntity`・`SupportCardEntity`・`PlayerEntity` [ADR 案] | 2章、ADR 0021 |
| 計画にある `PortResult<T>` | `UseCaseResult<T>`（要るときだけ。ready の計画では作らない見込み）[ADR 案] | 2章、ADR 0022 |
| 計画にある `<名詞>ViewData` | `<View 名>Dto` | 6章 |

## 未決事項

- サーバーの `GachaStatus` の新しい名前、Bootstrap の `ErrorDialogView` の置き場か名前、古い View 4 つの片付け、`OwnedSelectionRequest` の新しい名前。触る実装計画（読み替え）で決める
- サーバーの `ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`・`GachaHistoryRow` を、エンティティにするか `Record` にするか。ADR 0021 を実装する計画で決める
