---
title: 実装計画: client/ の画面 1/6 土台・タイトル・ホーム
status: in-progress
updated: 2026-11-08
spec: ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の画面 1/6 土台・タイトル・ホーム

## ADR 0020〜0022・naming.md への読み替え（案。実装は順 0（基盤移行の計画）の PR で ADR 0020〜0022 の accepted が決まってから。この節が本文と食い違ったら、この節が正）

2026-11-08。proposed の [ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)（マスタの参照）・[ADR 0021](../wiki/decisions/0021-domain-entities-and-repositories.md)（Domain のエンティティとリポジトリ）・[ADR 0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)（Infrastructure の具象と UseCase の IF）と [naming.md](../wiki/architecture/naming.md) の形に読み替える。前提: [基盤移行の計画](2026-11-08-client-foundation-migration.md)の読み替えの節（Player を移す: `Uta.Client.Domain`・`Uta.Client.MasterData` の asmdef、エンティティとリポジトリ、`IPlayerUseCase`・`IDeviceAuthUseCase`、`ApiClient`・`ErrorDefinitions` などの改名、テストの asmdef `Uta.Client.Tests.EditMode`）と[共通レスポンスの計画](2026-11-08-common-response-player-diff.md)が実装済み。「基盤 11d9b06 への読み替え」の節との優先は [索引](2026-11-07-client-screens-index.md) の読み替えの節（この節 > 11d9b06 の節 > 本文）。

**R0 は、C# の画面の計画（gacha・owned・formation・week・lesson）すべてに当てはまる共通の読み替え（P1〜P3 は対象外: 索引の読み替えの節）。各計画の同名の節は、その計画に固有のことだけを書く。** 規則だけで決まらなかった名前は 2026-11-08 にユーザーが決めた（具象は `<機能>UseCase`、Contracts の enum の写しはやめて操作を分ける、リポジトリの取得は `Find`・`FindAll`、`DiscardNoticeState`・`FeaturedIdolPicker`・`FormationNormalizer`・`TestMasterData`・`ICharacterStage.ShowIdol(int, string)`、`ProduceProgress` → `ProduceStateResult`）。**本文の 3.0（公開 API の表）の `IGameMaster`・`RefreshPlayerUseCase`・`PlayerCache`・`DialogService`・`UtaErrors`・`StubDeviceAuthPort`・`StubPlayerPort`・`StubIdolMaster` の行と、3.4（マスタの読み取り）・3.5（`RefreshPlayerUseCase`・`PlayerCache`）・3.6（`MasterGameData`）の節は無効**で、この節の名前で読む（後続の計画が 3.0 を正として読むため、ここに明記する）。

### R0. 画面の計画に共通の読み替え（対応表）

| 本文の形 | 読み替え | 根拠 |
|---|---|---|
| `IGameMaster`・`MasterGameData` と読み取り用の型（`IdolInfo`・`SupportCardInfo`・`SupportAbilityInfo`・`GachaEntryInfo`・`GachaInfo`・`ScenarioWeekInfo`・`ScenarioInfo`・`CardTextInfo`・`MasterItemKind`。`SupportLevelUpCost` の struct だけは残す） | **作らない**。`MemoryDatabase`（`Uta.MasterData`。`Uta.Client.MasterData` の asmdef。ルートのスコープに `RegisterInstance`）を DtoFactory・Application の具象がコンストラクターで受け、`db.IdolTable.All`・`db.IdolTable.FindByIdolId(id)` のように直接読む（MasterMemory が生成するテーブルと `FindBy<主キー>` の名前。実在の名前はコンパイルで確かめる）。本文の `KeyNotFoundException` は、`FindBy…` が null を返したときの `NullReferenceException`／`KeyNotFoundException` に当たり、捕まえない（所持の ID はサーバーと同じマスタにある）。`MasterItemKind` は `Uta.MasterData.ItemKind` を使う。計算を含むメンバーの行き先: `GetSupportLevelUpCost`（行の合計）→ Application の `SupportLevelUpCostCalculator`（owned が作る。`SupportLevelUpCost Calculate(Rarity rarity, int fromLevel, int toLevel)`。`SupportLevelUpCost` の struct は Application に残す）、`GetCardText`（Enhanced が無ければ Normal）→ Presentation の `SkillCardRowDtoFactory`（week が作る）、`GetSupportMaxLevel`・`GetSupportMaxLimitBreak` → 使う側が `db.SupportLevelCapTable` を読む。順の規則（`IdolId` の昇順など）は読む側が `OrderBy` で揃える | ADR 0020 |
| マスタの enum と Shared.Domain の enum の変換（foundation 3.6 の `DomainLesson`・`DomainProduce` の別名） | 今までどおり `(Shared.Domain の型)(int)マスタの値`。**マスタと Shared.Domain の両方の型を使うファイルは、`using Uta.MasterData;` を書かず**、Shared.Domain の名前空間（`Rarity`・`Plan`・`ParamKind`・`CardType`・`CostKind`・`StatusKind`・`LessonKind` は `Uta.Shared.Domain.Lesson`、`SupportType`・`SupportAbilityKind`・`Difficulty`・`WeekKind`・`ExamKind`・`ProduceRank`・`ProducePhase` は `Uta.Shared.Domain.Produce`）だけを `using` し、マスタの型は `Uta.MasterData.MemoryDatabase`・`Uta.MasterData.Rarity` と名前空間付きで書く（別名 `using` は使わない。`MemoryDatabase` だけを使うファイル（マスタの enum を読まない）は `using Uta.MasterData;` を書いてよい） | ADR 0020、naming.md 4章 |
| `I<機能>Port`・`<機能>Gateway`・Application の UseCase の具象（`<動詞><名詞>UseCase`・`ProduceActions`） | Application に機能ごとの IF **`I<機能>UseCase`**（Contracts の `I<機能>Service` と同じ単位。メソッドは Presenter の操作の単位で、手順（呼ぶ順・取り直し・状態の型への書き込み）を含む）。Infrastructure に具象 **`<機能>UseCase`**（`I<機能>Service` を直接呼び、`handledCodes` を持ち、DTO を Application・Domain の型に詰め替え、リポジトリ・状態の型に書く）。DTO → Application・Domain の型の詰め替えは static の **`<機能>Mapper`**（`ToXxx`）に分け、テストも分ける。Presenter は IF だけを受ける | ADR 0022、naming.md 2・7章 |
| `PortResult<T>`・`ServerReasons`（gacha が作り、owned・formation が使う） | **使い分けの基準**: その操作の本文で Presenter が `r.Reason`（`ServerReasons` の定数）を見て文言を分けているなら `UseCaseResult<T>`（`Application/Common/`。中身は本文の `PortResult<T>` と同じ: `Succeeded`・`Value`・`Reason`・`ErrorText`・`IsBusinessError`、`Success`／`BusinessError`／`Failure`）と `ServerReasons`（Infrastructure の具象が `result.ErrorCode == <機能>Errors.Xxx` で比べて `BusinessError(ServerReasons.Xxx, result.Error.ToString())` に直す）、`handledCodes` のコードが本文で既に enum（`DrawStatus`・`SupportStatus`・`ProduceStartStatus` など）に写されているなら `<操作>ResultKind` の enum（値も返すなら enum と値を持つ `<操作>Result` の class）。**gacha・owned・formation の本文はどの操作も enum に写しているので、`UseCaseResult<T>`・`ServerReasons`（と `PortResultTests`・`ServerReasonsTests`）は作らない**（ユーザーの判断 2026-11-08。gacha の読み替えで、reason を見る操作が無いことを確かめてから確定する。ADR 0022 31 行の「残す」は「要るときだけ」に直す提案を親のセッションへの報告に挙げた）。week・lesson の見分け方も同じ（本文が `ProduceStepStatus` で分岐しているので enum） | naming.md 2章、ADR 0022 |
| `PlayerCache`・`PlayerSnapshot`・`OwnedIdol`・`OwnedSupportCard`・`RefreshPlayerUseCase` | Domain の `IReadOnlyPlayerRepository`／`IPlayerRepository`（`PlayerEntity`）、`IReadOnlyIdolRepository`／`IIdolRepository`（`IdolEntity`）、`IReadOnlySupportCardRepository`／`ISupportCardRepository`（`SupportCardEntity`）。取得は `Find`（1 件。無ければ null）・`FindAll`（全件）。取り直しは `IPlayerUseCase.RefreshAsync()`（失敗は `InvalidOperationException`。移行計画 R3）。**Presenter はリポジトリを受けない**: 表示の読み取りは DtoFactory（`IReadOnly…` だけを受ける）、書き込みは Infrastructure の具象と `PlayerCommonApplier` だけ | ADR 0021 |
| 3.3.5「RPC が成功したあとの取り直しの失敗は、印を付けて成功を返す」 | 印は持たない。取り直しは Infrastructure の具象（`<機能>UseCase`）が RPC の成功のあとに `IPlayerUseCase.RefreshAsync()` を呼び、`InvalidOperationException` を握りつぶして成功を返す（formation R2 の `TryRefreshAsync` と同じ形。ダイアログは `ApiCore` が出した分だけ）。シーンをまたいでホームへ行く前の取り直しも、状態を変えた操作の具象が済ませる（formation の決めたこと 10）。Presenter は取り直しを呼ばない | ADR 0022 |
| `cache.Current.HasActiveProduce`・`DiscardedProduce`、`cache.Current == null` | `PlayerEntity.HasActiveProduce`・`DiscardedProduce`（サインインの応答の値。共通レスポンスの反映では据え置き）、`players.Find() == null` | ADR 0021 |
| `PlayerCache.DiscardNoticeShown`・`MarkDiscardNoticeShown` | Application の状態の型 `DiscardNoticeState`（R2）。書くのは `PlayerUseCase`（具象）だけで、Presenter は `IPlayerUseCase.MarkDiscardNoticeShown()` を呼ぶ（ADR 0022「状態の型への書き込みは具象が行う」） | ADR 0021・0022 |
| `<名詞>ViewData` | **`<View 名>Dto`**。型ごとに次の順で決める: (1) 行の View のクラス（`<名詞>RowView`）が受けるなら、その名前 + `Dto`（`OwnedRowViewData` → `OwnedRowViewDto`、`FormationSlotViewData` → `FormationSlotRowViewDto`、`ExamRankingRowViewData` → `ExamRankingRowViewDto`、`DeckSectionViewData` → `DeckSectionHeaderRowViewDto`（行は `DeckSectionHeaderRowView`）、gacha の `GachaRateLine` → `GachaLineRowViewDto`）。**複数の画面で使う行**（`SkillCardRowView`。手札と獲得の選択肢）は、行の Dto `SkillCardRowViewDto` に表示の値だけ（title・costs・description）を持たせ、画面ごとの ID・enabled は画面側の Dto に持たせる（naming.md 6章）: `HandCardViewData` → `LessonHandRowViewDto(int instanceId, bool enabled, SkillCardRowViewDto card)`、`CardOfferItemViewData` → `CardOfferRowViewDto(int index, SkillCardRowViewDto card)`。(2) 行の View のクラスが無く、画面の IF の `Set…(IReadOnlyList<…>)` が受ける一覧の要素なら `<画面><項目>RowViewDto`（`WeekActionViewData` → `WeekActionRowViewDto`、`ClassOptionViewData` → `ClassOptionRowViewDto`）。(3) 画面全体に 1 つ渡すものは `<画面>ViewDto`（`HomeViewDto`・`FormationViewDto`）。置き場は、その画面の Presenter と同じフォルダ（`Presentation/<機能>/`。共用の行の Dto は `Presentation/Produce/`）に 1 ファイル 1 型 | naming.md 6章 |
| Presenter が表示用の文言・値を組み立てる（`Refresh()` の中の文字列、`BuildHeader`、static の組み立ての呼び出し） | 画面ごとの **`<画面>DtoFactory`**（Presentation の `Presentation/<機能>/`。インスタンス。状態を持たず、ルートのスコープにシングルトンで登録。受けてよいのは `IReadOnly<名詞>Repository`・`MemoryDatabase`・`ICardCatalog`・Application の状態の型と外部に触れない具象だけ。リポジトリに書かず、通信・遷移・View に触れない）。Presenter は DtoFactory を受け、`view.Set…(dto)` を呼ぶだけ。`DisplayText` は Presentation に残し、DtoFactory から呼ぶ。複数の画面で使う行は `<行>DtoFactory`（`SkillCardRowDtoFactory`） | ADR 0022、naming.md 6章 |
| `CardSummaryBuilder`・`CardSummary`（week）、`LessonCardText.Create` の中の組み立て（lesson） | `CardSummaryBuilder`・`CardSummary` は作らない。`SkillCardRowDtoFactory`（Presentation。`ICardCatalog`・`MemoryDatabase` を受ける）が `SkillCardRowViewDto` を作り、`DisplayText.Card`・`Costs`・`LessonCardText` は Dto を受ける形にする（名前の細部は week・lesson の読み替えで決める） | naming.md「今のコードとの差」、ADR 0020 |
| `OwnedListBuilder`（owned）、`GachaRateCalculator`（gacha）、`SupportLevelPlanner`・`SupportSnapshotEdits`（owned） | 外部に触れない Application の具象として残す。`OwnedListBuilder` → **`OwnedListFactory`**（インスタンス。`IReadOnly…Repository`・`MemoryDatabase` を受ける）、`GachaRateCalculator`（`MemoryDatabase` を受けるインスタンス）、`SupportLevelPlanner` はそのまま（`IGameMaster` の引数を `MemoryDatabase` に）、`SupportSnapshotEdits`（`PlayerSnapshot` の書き換え）は作らない（具象がリポジトリに `Upsert` する） | ADR 0022、naming.md 7・9章 |
| `DialogService`・`FakeDialogService` | **`DialogOpener`・`FakeDialogOpener`**（中身・メソッド名は同じ。`SceneUiInstaller`・`ActiveSceneUi.Dialogs` の型も） | naming.md 9章 |
| `UtaApiClient`・`UtaErrors`・`UtaMagicOnionClientInitializer`・`UtaErrorsTests` | `ApiClient`・`ErrorDefinitions`・`MagicOnionClientInitializer`・`ErrorDefinitionsTests`（移行計画で改名済み） | naming.md 1章 |
| 操作の結果の enum `<名詞>Status`（`ProduceStartStatus`・`ProduceResumeStatus`・`ProduceStepStatus`・gacha の `DrawStatus`・owned の `SupportStatus`） | `<操作>ResultKind`（`ProduceStartResultKind`・`ProduceResumeResultKind`・`ProduceStepResultKind`・`DrawResultKind`・`SupportResultKind`。値は同じ） | naming.md 2章 |
| 操作の結果の class `<名詞>Outcome`（gacha の `DrawOutcome`、owned の `SupportOutcome`、formation の `ProduceOutcome`）と、サーバーに同じ役割の型がある Application の型（gacha の `DrawCallResult`・`DrawItem`・`GachaStatus`、owned の `SupportLevelUpResult`・`SupportLimitBreakResult`） | `Outcome` は使わない（`<操作>Result`。formation は `ProduceOutcome` → `ProduceEndResult`）。**同じ役割にはサーバーと同じ名前**（naming.md 1章）: gacha・owned の読み替えで、サーバーの `DrawResult`（結果の一覧・通貨・交換pt）・`GachaResult`（1 枠分）・`LevelUpResult`・`LimitBreakResult`・`GetGachasUseCase` の戻り値の型を見て揃える（`DrawCallResult` → `DrawResult`、`DrawItem` → `GachaResult` の見込み。`DrawOutcome`（enum と値を持つ UseCase の戻り値）はサーバーに相手が無いので `<操作>Result` の形で別の名前にする）。class の `GachaStatus`（`Status` は enum だけ）も同じ場所で決める。決めた名前はこの表に書き戻す | naming.md 1・2章 |
| 新しく作る、分類の enum（owned の `OwnedSortKey`・`OwnedTab`・`LevelUpStep` など） | `<名詞>Kind`（`OwnedSortKind`・`OwnedTabKind`・`LevelUpStepKind`） | naming.md 2章 |
| `<名詞>Row` の名前で、マスタの行でないもの（gacha の `GachaRateRow`・`GachaRateLine`、lesson の `ExamRankingRow`）と、クライアントの `GachaLineRow`（行の View） | `Row` はマスタの行（naming.md 4章）なので、それ以外は別の語にする（表示用なら `<…>RowViewDto`、Application の値なら `<名詞>Entry` など 9章にない語を避け、`<名詞>Result`・`<名詞>Line` の候補から gacha・lesson の読み替えで決める）。`GachaLineRow` → `GachaLineRowView` | naming.md 4・6章 |
| 応答の写しの型と状態の型（formation の `ProduceProgress`・`ProduceOutcome`・`ProduceSession` を week・lesson が使う） | `ProduceStateResult`・`ProduceEndResult`・`ProduceSessionState`（formation R1。`ProduceSnapshot` は Shared.Domain の保存用の写しと同名なので使わない） | naming.md 1・2章 |
| 先送りした名前（上の行の「gacha・owned の読み替えで決める」など） | 次のセッションが決めたら、この表の該当の行に書き戻す（R0 が共通の対応表として完結するように）。索引の読み替えの節の申し送りにも書いた | — |
| `WeekProgress`（week。引数 `ScenarioInfo`） | `ScenarioInfo` は作られないので、マスタの `Scenario`・`ScenarioWeek` の行（`MemoryDatabase` から読む）を受ける形にする（week の読み替えで決める） | ADR 0020 |
| Shared.Domain・Contracts の改名（naming.md「今のコードとの差」: Contracts の `GachaOutcome` → `GachaResultKind`、Shared.Domain の `ExamOutcome` → `ExamResultKind`、`ClassOptionView` → `ClassOption`、プロパティ `Outcome`・`ExamOutcome` → `ResultKind`・`ExamResultKind`） | その型を最初に使う C# の計画が、プロパティ名の改名と wiki の直しも含めて同じ PR で行う: `GachaOutcome` と `DrawResultDto.Outcome` は gacha、`ClassOptionView` と `ExamOutcome`（`ProduceEndResult` が持つ）と `ProduceResultResponse.ExamOutcome`・Shared.Domain の `ProduceResult.Outcome` は formation（R1）。改名したら、旧い名前を書いている wiki（`systems/`・`architecture/`）を Grep で探して同じ変更で直す（naming.md） | naming.md 1章 |
| Contracts の enum のクライアントの写し（`CardOfferAction`・`LessonActionType`・`MasterItemKind`） | 作らない。`IProduceUseCase` のメソッドを操作ごとに分ける（formation の読み替え R2）。`MasterItemKind` はマスタの `ItemKind`。残る写し `ProduceEndingKind` は **`ProduceEndKind`**（Contracts の `ProduceEnding` と同名にしない。naming.md 163 行の `ProduceEnding` は naming.md 1章と食い違うので、親のセッションへの報告に挙げた） | naming.md 1章 |
| `OwnedSelectionRequest`（owned） | `Request` を外した名前にする。naming.md の `OwnedSelection` は owned の結果の型 `OwnedSelection` と同名なので使えない。owned の読み替えで決める（親のセッションへの報告に挙げた） | naming.md 1章 |
| テストの補助の名前（gacha の `GachaFixtures`、lesson の `LessonTestData`、formation の `TestProduce`、この節の `TestMasterData`） | そのまま（naming.md 8章に規則が無い。親のセッションへの報告に挙げた）。置き場は `Tests/EditMode/Support/` | — |
| テストの偽（`StubPlayerPort`・`StubDeviceAuthPort`・`FakePlayerPort`・`FakeProducePort`・`FakeSupportPort`・`FakeGameMaster`・`StubIdolMaster`） | Presenter・Flow のテスト: **`Fake<機能>UseCase`**（`FakePlayerUseCase`・`FakeDeviceAuthUseCase`・`FakeGachaUseCase`・`FakeSupportUseCase`・`FakeProduceUseCase`。`Tests/EditMode/Fakes/` に 1 つずつ。返す値の `Queue` と、同じ `CallLog` への記録）。Infrastructure の具象のテスト: **`Fake<機能>Service`**（`FakePlayerService`・`FakeGachaService`・`FakeSupportService`・`FakeProduceService`。`IService<T>` の 5 メンバーを実装し `WithHeaders` は `this`。失敗は基盤の `FakeRpcErrors.ApiFailureResult<T>`）。**マスタの偽は作らず**、`TestMasterData.Build(...)`（R6）で小さな `MemoryDatabase` を作る。リポジトリは本物（Infrastructure のメモリ実装）を使う | ADR 0022、naming.md 8章 |
| テストのアセンブリ `Uta.Client.Infrastructure.Tests.EditMode`、名前空間 `Uta.Client.Infrastructure.Tests` | `Uta.Client.Tests.EditMode`、`Uta.Client.Tests`（移行計画で改名済み） | naming.md 8章 |
| `BootstrapEntry.RegisterUseCases` の行 | ゲートウェイの行は無い。具象を IF で登録する: `builder.Register<GachaUseCase>(Lifetime.Singleton).As<IGachaUseCase>();`。DtoFactory・外部に触れない具象・状態の型も同じ場所にシングルトンで登録する（`builder.Register<HomeDtoFactory>(Lifetime.Singleton);`） | ADR 0022 |
| 3.3 の共通ルール | ADR 0022 が accepted になったら `screen-design.md` に移す（ADR 0022「accepted にしたら直すもの」）。移すまでは本文の 3.3 とこの節が正。読み替え: 3.3.2 の Presenter の形は R3 の `HomePresenter`（DtoFactory を受け、`Refresh()` は `view.Set…(dtoFactory.Create())`）。3.3.3 の「文言の組み立ては Presenter でもページでもよい」→「DtoFactory が作り、View は値を入れるだけ」。3.3.5 の「ゲートウェイの `handledCodes`」→「Infrastructure の具象の `handledCodes`」、「ユースケースの結果の型」→「`UseCaseResult<T>` か `<操作>ResultKind`」、「`RefreshPlayerUseCase`」→「`IPlayerUseCase.RefreshAsync`（具象が呼ぶ）」。3.3.8 の偽と 3.3.9 の DI の表（ルートの行の「ゲートウェイ、`PlayerCache`、`IGameMaster`」→「リポジトリ（2 つの IF で）、`MemoryDatabase`、UseCase の具象（IF で）、DtoFactory、状態の型、外部に触れない具象」）はこの表に読み替える | ADR 0022 |

### R1. asmdef・DI

- asmdef の参照は移行計画の R1 で足し済み（`Uta.Client.Domain`・`Uta.Client.MasterData`、`MasterMemory.dll`・`MessagePack.dll`）。この計画で足すのは本文 3.1 のとおり（テストの asmdef の名前は `Uta.Client.Tests.EditMode.asmdef`）
- 2 章の表: `Scripts/Application/Master/*.cs`（11 ファイル）・`Scripts/Infrastructure/MasterData/MasterGameData.cs`・`Scripts/Application/Player/RefreshPlayerUseCase.cs`・`PlayerCache.cs`（変更。型が無い）・`Scripts/Application/Auth/SignInUseCase.cs`（コメントだけ直す。型が無い。3.7 のコメントの直しも無効）・`Tests/EditMode/Fakes/StubIdolMaster.cs`・`StubDeviceAuthPort.cs`・`StubPlayerPort.cs`・`Tests/EditMode/MasterGameDataTests.cs`・`RefreshPlayerUseCaseTests.cs`・`PlayerCacheTests.cs` の行は無効。代わりに作る・変えるもの: 作成 `Scripts/Application/Player/DiscardNoticeState.cs`、`Scripts/Application/Idol/FeaturedIdolPicker.cs`、`Scripts/Presentation/Home/HomeViewDto.cs`・`HomeDtoFactory.cs`、`Tests/EditMode/Fakes/FakePlayerUseCase.cs`・`FakeDeviceAuthUseCase.cs`、`Tests/EditMode/Support/TestMasterData.cs`、`Tests/EditMode/DiscardNoticeStateTests.cs`・`FeaturedIdolPickerTests.cs`・`HomeDtoFactoryTests.cs`。変更 `Scripts/Infrastructure/Player/PlayerUseCase.cs`（コンストラクターに `DiscardNoticeState` を足す。R2）・`Tests/EditMode/PlayerUseCaseTests.cs`（2 本足す）。改名済みの名前で読む: `Infrastructure/Api/UtaErrors.cs`・`UtaApiClient.cs` は `ErrorDefinitions.cs`・`ApiClient.cs`、`Presentation/Common/DialogService.cs` は `DialogOpener.cs`、`Tests/EditMode/DialogServiceTests.cs` は `DialogOpenerTests.cs`、`Tests/EditMode/Fakes/FakeDialogService.cs` は `FakeDialogOpener.cs`、`Tests/EditMode/UtaErrorsTests.cs` は `ErrorDefinitionsTests.cs`、テストの asmdef は `Uta.Client.Tests.EditMode.asmdef`
- 3.3.9 の DI の表のルートの行: `MemoryDatabase`・`ICardCatalog`、`ActiveSceneUi`、`StartupOptions`、`ISceneNavigator`・`IErrorNavigator`、`ApiClient`、リポジトリ 3 つ（2 つの IF で）、UseCase の具象（IF で）、`DiscardNoticeState`、`FeaturedIdolPicker`、DtoFactory

### R2. Application

```csharp
// Application/Player/DiscardNoticeState.cs（ADR 0021「UI の状態はエンティティに入れない」。本文 3.5 の PlayerCache の印の代わり）
public sealed class DiscardNoticeState
{
    public bool Shown { get; private set; }
    public void MarkShown();   // Shown = true
    public void Reset();       // Shown = false
}

// Application/Idol/FeaturedIdolPicker.cs（名前空間 Uta.Client.Application.Idol。naming.md 9章の Picker。外部に触れない具象）
public sealed class FeaturedIdolPicker
{
    public FeaturedIdolPicker(IReadOnlyIdolRepository idols, MemoryDatabase db);
    /// <summary>
    /// 所持の育成アイドル（idols.FindAll()）のうち、マスタの Rarity（db.IdolTable.FindByIdolId(e.IdolId).Rarity を (int) にした値）が最も大きいもの、
    /// 同じなら IdolId が小さいものの IdolId（本文 3.7 の RefreshStage の選び方。所持の順に依らない）。所持が 0 なら 0。
    /// マスタに無い IdolId は捕まえない（所持の IdolId はサーバーと同じマスタにある）。
    /// </summary>
    public int Pick();
}
```

- 移行計画の `IPlayerUseCase` に `void MarkDiscardNoticeShown();`（`notice.MarkShown()`。通信しない）を足す
- 移行計画の `PlayerUseCase`（Infrastructure）のコンストラクターの**末尾**に `DiscardNoticeState notice` を足し（`PlayerUseCase(IPlayerService player, IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards, DiscardNoticeState notice)`）、`EnterAsync`・`RefreshAsync` が**成功して**リポジトリを置き換えたあとに `notice.Reset()` を呼ぶ（本文の「`Set` で false に戻る」。失敗では呼ばない）。既存の `PlayerUseCaseTests` の `new PlayerUseCase(...)` に `new DiscardNoticeState()` を足し、`Enter_Success_ResetsDiscardNotice`・`Refresh_Success_ResetsDiscardNotice`（`MarkShown()` のあとに成功 → `Shown == false`）・`MarkDiscardNoticeShown_SetsFlag` を足す
- `MasterItemKind`（3.4）は作らない（gacha が `Uta.MasterData.ItemKind` を使う）

### R3. Presentation（タイトル・ホーム）

タイトル（3.7）:
- `TitlePresenter`: `[Inject] TitlePresenter(TitlePage view, IDeviceAuthUseCase deviceAuth, IPlayerUseCase player, ISceneNavigator scenes)` と `TitlePresenter(ITitleView view, IDeviceAuthUseCase deviceAuth, IPlayerUseCase player, ISceneNavigator scenes)`。`StartAsync` の 3 は `var registered = await deviceAuth.SignInAsync();` → `var entered = await player.EnterAsync(registered);`（手順・ログ・`disposed` の確かめは本文のまま）
- `StartupLogs.SignIn(EnterPlayerResult e)`: `"sign-in: ok level=" + e.Player.Level + " exp=" + e.Player.Exp + " gold=" + e.Player.Gold + " supportPoints=" + e.Player.SupportPoints + " gachaCurrency=" + e.Player.GachaCurrency + " idols=" + e.IdolCount + " supportCards=" + e.SupportCardCount + " hasActiveProduce=" + e.Player.HasActiveProduce + " discardedProduce=" + e.Player.DiscardedProduce`（`PlayerSnapshot` を受けない。出る文字列は同じ）
- `TitleDiagnostics`: `SignInUseCase signIn, EnterPlayerUseCase enterPlayer` の 2 引数を `IDeviceAuthUseCase deviceAuth, IPlayerUseCase player` にする（`ConnectivityCheck` は移行計画で同じ形に直し済み）。`signIn.ExecuteAsync()` → `deviceAuth.SignInAsync()`、`enterPlayer.ExecuteAsync(registered)` → `player.EnterAsync(registered)`
- `TitleLifetimeScope`・`TitleEntry` は本文のまま

ホーム（3.7。ADR 0022「Presenter は DtoFactory を受ける」）:

```csharp
public interface IHomeView
{
    void SetProfile(string text);                          // 本文の SetProfile(int level, int gachaCurrency) を、組み立て済みの文字を受ける形に（View は値を入れるだけ）
    void SetResume(bool visible, string primaryLabel);     // 本文の SetResumeVisible(bool)。主ボタンのラベルも Dto から受ける
    event Action ProduceClicked; event Action GachaClicked; event Action OwnedClicked;   // 本文のまま
}

/// <summary>ホームのキャラ表示のステージ。ステージはマスタを読まない（ADR 0020: Presentation でマスタを読むのは DtoFactory だけ）。</summary>
public interface ICharacterStage
{
    void ShowIdol(int idolId, string name);   // 3D の ModelCharacterStage は idolId を使う。MVP の仮の枠は name を出す
    void Clear();
}
public sealed class PlaceholderCharacterStage : ICharacterStage
{
    public PlaceholderCharacterStage(TMP_Text label);   // label が null なら ArgumentNullException("label")。master は受けない
    public void ShowIdol(int idolId, string name);      // label.text = name
    public void Clear();                                // label.text = EmptyText
}
// HomePage.CreateStage() は引数なし（new PlaceholderCharacterStage(stageLabel)）。SetProfile(string)・SetResume(bool, string) は参照に入れるだけ:
//   profileText.text = text; resumePanel.SetActive(visible); primaryButton.SetLabel(primaryLabel);

// Presentation/Home/HomeViewDto.cs
public sealed class HomeViewDto
{
    public HomeViewDto(string profileText, bool resumeVisible, string primaryLabel, bool showDiscardNotice, int stageIdolId, string stageIdolName);
    // stageIdolId が 0 なら所持なし（stageIdolName は null）。showDiscardNotice は「破棄の知らせのダイアログを出すか」（表示の値。ADR 0022 の DtoFactory の線の中）
}

// Presentation/Home/HomeDtoFactory.cs（ルートのスコープにシングルトン）
public sealed class HomeDtoFactory
{
    public HomeDtoFactory(IReadOnlyPlayerRepository players, FeaturedIdolPicker picker, DiscardNoticeState notice, MemoryDatabase db);   // notice は読むだけ
    /// <summary>
    /// var p = players.Find(); null なら null。
    /// profileText = "プレイヤーLv " + p.Level + "　ガチャ通貨 " + p.GachaCurrency.ToString("N0", CultureInfo.InvariantCulture)（間は全角の空白 1 つ。本文の HomePage.SetProfile の書式）。
    /// showDiscardNotice = p.DiscardedProduce && !notice.Shown。
    /// resumeVisible = p.HasActiveProduce（formation の読み替えが ProduceSessionState を足して読み替える）。primaryLabel = resumeVisible ? "再開" : "プロデュース"。
    /// stageIdolId = picker.Pick()。0 でなければ stageIdolName = db.IdolTable.FindByIdolId(stageIdolId).Name。
    /// </summary>
    public HomeViewDto Create();
}

[AssetAddress(nameof(HomePage))]
public sealed class HomePresenter : IPresenter
{
    [Inject] public HomePresenter(HomePage view, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs);   // : this((IHomeView)view, view.CreateStage(), dtoFactory, player, dialogs)
    public HomePresenter(IHomeView view, ICharacterStage stage, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs);
    // Refresh(): var dto = dtoFactory.Create(); null なら何もしない。view.SetProfile(dto.ProfileText); view.SetResume(dto.ResumeVisible, dto.PrimaryLabel);
    //   dto.StageIdolId == 0 ? stage.Clear() : stage.ShowIdol(dto.StageIdolId, dto.StageIdolName);（この順）。RefreshStage は無い（選び方は FeaturedIdolPicker）
    // ShowDiscardNoticeIfNeededAsync(): var dto = dtoFactory.Create(); dto が null なら何もしない。dto.ShowDiscardNotice なら player.MarkDiscardNoticeShown() してから await dialogs.ShowInfoAsync("バージョンが変わったため、進行中のプロデュースは破棄されました。")
    // ほか（InitializeAsync・DidPushEnter・DidPopEnter・Dispose・OpenXxxAsync）は本文のまま
}

/// <summary>Home シーンの入口。</summary>
public sealed class HomeEntry : IStartable
{
    public HomeEntry(IScreenNavigator navigator, IReadOnlyPlayerRepository players, ISceneNavigator scenes);
    // StartAsync: players.Find() == null なら GoToTitleAsync、そうでなければ PushPageAsync<HomePresenter>（入口は Presenter ではないので、読み取り専用のリポジトリを受けてよい）
}
```

- `PlaceholderCharacterStage`・`HomePage` の `using Uta.Client.Application.Master;` は要らない。`HomeDtoFactory` は `using Uta.MasterData;`（`MemoryDatabase`）・`using Uta.Client.Domain.Player;`・`using Uta.Client.Application.Idol;`・`using System.Globalization;`
- 後続の計画が `HomePresenter` のコンストラクターに足すものは、UseCase の IF（`IGachaUseCase`・`IProduceUseCase` など）と `IScreenNavigator`・`ISceneNavigator`。リポジトリ・`MemoryDatabase` は足さない（DtoFactory が受ける）
- 2〜4 本目が差し替えるホームのボタンのテスト（決めたこと 16）の規則は変わらない

### R4. `Bootstrap` と `Infrastructure.Api`

- `UtaErrors.Create()` → `ErrorDefinitions.Create()`（19 個の中身は本文のまま）。`UtaApiClient.Create` → `ApiClient.Create`（引数の足し方は本文のまま）
- `ErrorNavigator.GoToHomeAsync`: `resolver.Resolve<RefreshPlayerUseCase>().ExecuteAsync()` → `resolver.Resolve<IPlayerUseCase>().RefreshAsync()`。遅延解決の理由（`ApiClient` → `IErrorNavigator` → `IPlayerUseCase` → `IPlayerService` → `ApiClient` の循環）は同じ
- `BootstrapEntry.RunAsync` の 2: `cardCatalog = CardCatalogFactory.Create(db);`（`gameMaster = new MasterGameData(db)` は無い）。登録は `builder.RegisterInstance(db); builder.RegisterInstance<ICardCatalog>(cardCatalog);`（`RegisterInstance<IGameMaster>` は無い）
- `RegisterUseCases` を作るとき、移行計画 R4 の 5 行（リポジトリ 3 つ・`DeviceAuthUseCase`・`PlayerUseCase`）と共通レスポンスの計画の `builder.Register<PlayerCommonApplier>(Lifetime.Singleton);`（どれも今は `LifetimeScope.Create` のラムダに直書き）をここへ移し、続けてこの計画で次を足す。`ApiClient.Create(...)` の呼び出しは、共通レスポンスの計画が足した最後の引数 `resolver.Resolve<PlayerCommonApplier>()` を保ったまま、本文 3.8 の `IApiActivityHandler`・`IErrorNavigator` の引数をその前（`Create` の引数の並びは移行計画の `Create` → 本文 3.8 → 共通レスポンスの順に末尾に足されている）に入れる。**2〜6 本目はここに足す**（UseCase の具象は IF で、DtoFactory・具象・状態の型はそのまま登録する）:
  ```csharp
  builder.Register<DiscardNoticeState>(Lifetime.Singleton);
  builder.Register<FeaturedIdolPicker>(Lifetime.Singleton);
  builder.Register<HomeDtoFactory>(Lifetime.Singleton);
  ```
  本文の 6 行（`DeviceAuthGateway`・`PlayerGateway`・`PlayerCache`・`SignInUseCase`・`EnterPlayerUseCase`・`RefreshPlayerUseCase`）は無い
- `ActiveSceneUi.Dialogs`・`SceneUiInstaller`・`ErrorDialogView` の `DialogService` は `DialogOpener`

### R5. タスクの読み替え

- タスク 1: `DialogService` → `DialogOpener`。偽は `CallLog`・`FakeScreenNavigator`・`FakeDialogOpener`・`FakeSceneNavigator`・`FakeDeviceAuthUseCase`（R6）と `Support/TestMasterData.cs`。`FakePlayerUseCase` はタスク 2 で作る（`DiscardNoticeState`・`IPlayerUseCase.MarkDiscardNoticeShown` がタスク 2 で入るため。2026-11-08 の実装で直した）
- タスク 2: 「マスタの読み取り口」は無い。作るのは `DiscardNoticeState`・`FeaturedIdolPicker`、R6 の `Tests/EditMode/Fakes/FakePlayerUseCase.cs`（`DiscardNoticeState` と `MarkDiscardNoticeShown` を足したあと）、`PlayerUseCase` への `DiscardNoticeState` の追加、`ErrorDefinitions` の 18 個。テストは `DiscardNoticeStateTests`・`FeaturedIdolPickerTests`・`PlayerUseCaseTests` の 2 本の追加・`ErrorDefinitionsTests`。計画 0 の前提の確認（`Card.Name`・`CardFace.Description`）は、`Assets/Scripts/MasterData/Generated/Lesson/` を見る
- タスク 3: `HomeViewDto`・`HomeDtoFactory` を作る。`StubIdolMaster` は作らない。テストに `HomeDtoFactoryTests` を足す
- タスク 4: `IGameMaster` の登録は無い（R4）。止まる条件 B5 の循環の経路は R4 のとおり
- タスク 6（wiki）: `screen-design.md` 7.5 の「マスタ」の行と 7.4・8 章は移行計画のタスク 3 が ADR 0020〜0022 の形に直している（この計画で直すのは、本文の記述のうち実装と違うところだけ）。この計画で足して直すもの: 7.8 の「`IGameMaster.GetIdol(idolId).Name`」を「`HomeDtoFactory` がマスタから名前を引き、`ICharacterStage.ShowIdol(idolId, name)` に渡す」に。3.3 の共通ルールを `screen-design.md` に移す仕事は ADR 0022 の accepted の後の別の作業（この計画ではしない）

### R6. テストの読み替え

```csharp
// Tests/EditMode/Support/TestMasterData.cs（名前空間 Uta.Client.Tests）
public static class TestMasterData
{
    /// <summary>
    /// append で足した行だけを持つ MemoryDatabase を作る: var b = new DatabaseBuilder(); append(b); return new MemoryDatabase(b.Build());
    /// （MasterMemory の DatabaseBuilder・MemoryDatabase のコンストラクターの実在の形はコンパイルで確かめ、違えば止まって報告する。ADR 0020「最初に使う実装計画で確かめる」= この計画）。
    /// 検証（IValidatable）は走らないので、テストは要る列だけ入れる。
    /// </summary>
    public static MemoryDatabase Build(Action<DatabaseBuilder> append);
    /// <summary>名前とレア度だけを決めた Idol の行（本文の StubIdolMaster.Idol と同じ値: Plan は Sense、MaxHp 30、Initial は 0、First/Second/ThirdParam は Singing・Rhythm・Expression。ほかの列は 0）。</summary>
    public static Idol Idol(int idolId, string name, Rarity rarity);   // Rarity・Plan・ParamKind はマスタの enum（Uta.MasterData）
}

// Tests/EditMode/Fakes/FakeDeviceAuthUseCase.cs（本文 3.3.8 の StubDeviceAuthPort の代わり）
public sealed class FakeDeviceAuthUseCase : IDeviceAuthUseCase
{
    public FakeDeviceAuthUseCase(CallLog log = null);   // log が null なら記録しない（Title のテストは null で作り、本文の期待する記録の並びを変えない）
    /// <summary>SignInAsync の 1 回ごとの振る舞い。空なら UniTask.FromResult(false)（登録済みの端末）。Dequeue した Func を呼んでその UniTask を返す（例外を投げる・未完了の源を返す）。log があれば "DeviceSignIn" を記録。</summary>
    public Queue<Func<UniTask<bool>>> SignInResults { get; }
    // ReissueTokenAsync は完了済み。InvalidateServerSideTokenAsync は TokenInvalidationCount を 1 増やして完了済み。CanReceiveServerErrorAsync は true。TokenInvalidationCount の初期値は 0
}

// Tests/EditMode/Fakes/FakePlayerUseCase.cs（本文 3.3.8 の StubPlayerPort、formation の FakePlayerPort の代わり。formation 以降も使う）
public sealed class FakePlayerUseCase : IPlayerUseCase
{
    public FakePlayerUseCase(CallLog log = null, DiscardNoticeState notice = null);   // notice があれば MarkDiscardNoticeShown が notice.MarkShown() する   // log が null なら記録しない。Title のテストは null、ErrorNavigator・formation 以降のテスト（"Refresh" の順を見る）は同じ CallLog を渡す
    /// <summary>EnterAsync の 1 回ごとの振る舞い。空なら new EnterPlayerResult(afterRegistration, afterRegistration, Player, 0, 0)。log があれば "Enter:" + afterRegistration を記録。</summary>
    public Queue<Func<UniTask<EnterPlayerResult>>> EnterResults { get; }
    /// <summary>RefreshAsync の 1 回ごとの振る舞い。空なら完了済み。失敗は () => throw new InvalidOperationException("sign-in failed: x")。log があれば "Refresh" を記録。</summary>
    public Queue<Func<UniTask>> RefreshResults { get; }
    /// <summary>既定の EnterAsync が返すエンティティ。既定は new PlayerEntity("P1", 1, 0, 0, 0, 0, false, false)（先頭は移行計画 R8 の uniqueId。PlayerUseCaseTests と同じ "P1"）。</summary>
    public PlayerEntity Player { get; set; }
    public int RefreshCount { get; }
    // 偽はリポジトリに書かない。テストは本物のリポジトリ（PlayerRepository など）に Upsert・ReplaceAll で値を入れる
}
```

- 5 章の表の読み替え:
  - `DialogServiceTests` → `DialogOpenerTests`（中身は同じ）
  - `MasterGameDataTests` → 無い。`RefreshPlayerUseCaseTests` → 無い（移行計画の `PlayerUseCaseTests` の `Refresh_*`）。`PlayerCacheTests` → `DiscardNoticeStateTests`（`MarkShown_SetsFlag`・`Reset_ClearsFlag`）
  - `FeaturedIdolPickerTests`（新。タスク 2）: `Pick_ReturnsHighestRarityThenSmallestId`（`TestMasterData.Build` に 1（SR）・2（SSR）・3（SSR）・4（R）、`IdolRepository.ReplaceAll` に 4・3・1・2 の順 → 2）、`Pick_SingleIdol_ReturnsIt`、`Pick_NoIdols_ReturnsZero`
  - `HomeDtoFactoryTests`（新。タスク 3）: `Create_NoPlayer_ReturnsNull`、`Create_FormatsProfileAndResume`（Lv 12・ガチャ通貨 3250・`HasActiveProduce = true` → `ProfileText == "プレイヤーLv 12　ガチャ通貨 3,250"`・`ResumeVisible`・`PrimaryLabel == "再開"`）、`Create_StageFromPicker`（所持 2（SSR）→ `StageIdolId == 2`、`StageIdolName` がマスタの名前）、`Create_NoIdols_StageZero`
  - `UtaErrorsTests` → `ErrorDefinitionsTests`（`new ErrorPolicyProvider(ErrorDefinitions.Create())`）
  - `TitlePresenterTests`: `StubDeviceAuthPort`・`StubPlayerPort` で作った `SignInUseCase`・`EnterPlayerUseCase` → `new FakeDeviceAuthUseCase()`・`new FakePlayerUseCase()`（`log` は渡さない。本文の期待する記録の並び（`"SetStatus:…"`, `"GoToHome"`）はそのまま）。`StartAsync_SignInFails_ShowsRetry` は `deviceAuth.SignInResults.Enqueue(() => throw new InvalidOperationException("x"))`。`StartAsync_WhileRunning_Ignored`・`_DisposedDuringSignIn_…` の未完了の源は `SignInResults` に `() => source.Task`（`UniTaskCompletionSource<bool>`）
  - `TitleEntryTests`: `TitleDiagnostics` は `new TitleDiagnostics(deviceAuth, player, new ConnectivityCheck(deviceAuth, player), code => codes.Add(code))`
  - `HomePresenterTests`: 共通の組み立ては `var db = TestMasterData.Build(b => b.Append(new[] { TestMasterData.Idol(...), ... }));`（育成アイドルの要らないテストは空の `Build(_ => { })`）、`var players = new PlayerRepository(); var idols = new IdolRepository();`、`var notice = new DiscardNoticeState();`、`new HomeDtoFactory(players, new FeaturedIdolPicker(idols, db), notice, db)`、`new HomePresenter(view, stage, dtoFactory, new FakePlayerUseCase(log, notice), dialogs)`（`FakePlayerUseCase` の第 2 引数 `DiscardNoticeState notice = null` は、`MarkDiscardNoticeShown` で書く先。記録は `"MarkDiscardNoticeShown"`。ADR 0022 の偽の規約の例外）。`PlayerCache` に `Set` していた所は `players.Upsert(new PlayerEntity(...))`、所持の育成アイドルは `idols.ReplaceAll(new[] { new IdolEntity(id, 0, 0, 0), ... })`。記録の形は `"SetProfile:プレイヤーLv 12　ガチャ通貨 3,250"`・`"SetResume:True,再開"`、ステージは `"ShowIdol:2,<名前>"`・`"ClearStage"`。`InitializeAsync_ShowsProfileAndResume` の期待は `"SetProfile:…"`, `"SetResume:True,再開"`, `"ClearStage"`。`ShowDiscardNotice_*` の `cache.DiscardNoticeShown` は `notice.Shown`（期待する記録の `"Info:…"` の前に `"MarkDiscardNoticeShown"` が入る）。`HomeDtoFactoryTests` に `Create_ShowDiscardNotice_WhenDiscardedAndNotShown`・`Create_HidesDiscardNotice_AfterShown` を足す
  - `PlaceholderCharacterStageTests`: `new PlaceholderCharacterStage(label)`、`ShowIdol(2, "テストアイドル")` で `label.text == "テストアイドル"`。`Constructor_Null_Throws` は `label` だけ
  - `HomeEntryTests`: `cache.Current == null` → 空の `PlayerRepository`、`Set` 済み → `players.Upsert(...)`
  - `ErrorNavigatorTests`: コンテナーには `builder.RegisterInstance<IPlayerUseCase>(fake);`（`new FakePlayerUseCase(log)`。同じ `CallLog` に、`RefreshAsync` が**呼ばれた時点**（積んだ `Func` を呼ぶ前）で `"Refresh"` を記録する。`PlayerCache`・`RefreshPlayerUseCase` の登録は無い）。期待する記録は本文から次のように変わる: `GoToHomeAsync_ResolvesRefreshLazily_RefreshesThenGoesHome` は `"Refresh"`, `"GoToHome"`（「`PlayerCache.Current` が `port.Snapshot` と同じ」の確認は `fake.RefreshCount == 1` に代える）。`_WhileRefreshing_SkipsRefresh` は `GatedPlayerPort` の代わりに `fake.RefreshResults.Enqueue(() => gate.Task)`（`gate` は `UniTaskCompletionSource`）: `var first = navigator.GoToHomeAsync();` → `log` が `"Refresh"` だけ、`RefreshCount == 1` → `await navigator.GoToHomeAsync();` → `log` が `"Refresh"`, `"GoToHome"`、`RefreshCount == 1` → `gate.TrySetResult()` → `await first` → `log` が `"Refresh"`, `"GoToHome"`, `"GoToHome"` → もう 1 回 `GoToHomeAsync()`（`RefreshResults` は空なので完了済み）→ `RefreshCount == 2`。`_RefreshFails_StillGoesHome` は `RefreshResults.Enqueue(() => throw new InvalidOperationException("sign-in failed: x"))` → `log` が `"Refresh"`, `"GoToHome"`（「`PlayerCache.Current` は null のまま」の確認は無い）。`_ResolveFails_StillGoesHome` は本文のまま（何も登録しないコンテナー。`log` は `"GoToHome"` だけ）
  - `StubIdolMaster` を使うテストは `TestMasterData.Build` と本物の `IdolRepository` に読み替える
- Play モード・手動の確認は本文のまま

## 基盤 11d9b06 への読み替え（2026-11-08。[クライアントの移行計画](2026-11-08-client-foundation-migration.md)。この節が本文と食い違ったら、この節が正）

- `XxxApi`（`ApiServiceBase<…>` のラッパー。`GachaApi`・`SupportApi`・`ProduceApi`）は**作らない**。`UtaApiClient` が `public IGachaService Gacha { get; }`（`ISupportService Support`・`IProduceService Produce`）を持ち、`Create` の中で `core.CreateClient<I…Service>()` で作る。本文の「`ServiceClientFactory.Create<I…Service>(callInvoker)` で作って `new XxxApi(core, client)`」は「`core.CreateClient<I…Service>()`」に読み替える。private コンストラクターの引数の型は `XxxApi` ではなく `I…Service`
- ゲートウェイのコンストラクターは `I…Service` を受ける（`UtaApiClient` を受けない）。`BootstrapEntry` に `builder.Register<I…Service>(resolver => resolver.Resolve<UtaApiClient>().Xxx, Lifetime.Singleton);` を足す（`PlayerGateway` と同じ）
- 呼び方: `xxx.XxxAsync(req, handledCodes)` → `await xxx.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` の無い RPC は `WithHandledCodes` を付けない）。`result.IsSuccess` → `!result.IsError`。`result.Error.Code.Equals(X)` → `result.ErrorCode == X`。`result.Error.ToString()`・`result.Error.Code.Reason` は今のまま使える
- 本文のファイル表の `Api/XxxApi.cs`（作成）の行は無効（作らない）。テストの偽物は、`PlayerGatewayTests` と同じく偽の `I…Service`（`IService<T>` の 5 メンバーを実装し、`WithHeaders` は `this` を返す）
- `[MagicOnionClientGeneration]` の型の並びに `typeof(I…Service)` を足す指示は今のまま有効（`Uta.Shared.Contracts` の同じアセンブリなので、足し忘れても `IPlayerService` で足りるが、本文どおり足す）
- `UtaApiClient.Create` の `IApiActivityHandler`（3.8）は、移行計画の `Create`（引数は今のまま）に足す。`ApiCoreBuilder.AddActivityHandler` を使う
- DTO の ID の名前（`OwnedIdolDto.IdolId` など）は、[移行計画](2026-11-08-client-foundation-migration.md)の R9 の表に読み替える（2026-11-08）
- クライアントのエンティティ・リポジトリの形（`IdolEntity.IdolId` → `MasterId`、`SupportCardEntity.SupportCardUid`・`CardId` → `UniqueId`・`MasterId`、`PlayerEntity` の `UniqueId`、コンストラクターの先頭の `uniqueId`、`Find(string uniqueId)`、名前空間 `Uta.Client.Domain.Idol`・`…SupportCard`・`Uta.Client.Infrastructure.Idol`・`…SupportCard`）は、[移行計画](2026-11-08-client-foundation-migration.md)の R8 に読み替える（2026-11-08）
- リポジトリの `ReplaceAll` は、[移行計画](2026-11-08-client-foundation-migration.md)の R10 の `Clear()` のあと `Upsert(IReadOnlyList<T>)` に読み替える（2026-11-08）

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **PR #24 のシーン構成（`Bootstrap`／`Title`／`Home`／`Produce`、`ISceneNavigator`、ルートのスコープにユースケース、各シーンの `LifetimeScope`）の上に作る**。旧版の `StartupFlow`・`StartupPage`・`UtaBootstrap`（1 つのスコープで起動からホームまでを積む流れ）はやめる。起動の流れ（タップ → 端末の認証 → サインアップ・サインイン → `Home` へ）は `Title` シーンの `TitlePresenter`、ホームは `Home` シーンの `HomePresenter` が持つ。シーンをまたぐ移動は今までどおり `ISceneNavigator`（`SceneManager.LoadSceneAsync`）
2. **C# で UI を組まない**。旧版の `UiTheme`・`UiFonts`・`UiFactory`・`SafeAreaFitter`・`WindowAspectKeeper`・`PageBase`／`ModalBase`（`EnsureBuilt`・`BuildUi`）・`UiRoot.Create`・`RegisterPage` と `UiFactoryTests` は消す（`UiTheme`・`SafeAreaFitter`・`WindowAspectKeeper`・`PageBase`・`ModalBase`・`UiRoot` は P1 が作る）。View は P1／P2 が作る骨格（`[SerializeField]` のフィールドだけ）に、この計画（と後続の C# の計画）が `IXxxView` の実装を足す。Prefab の子の参照は生成時に入っているので、旧版の `EnsureBuilt`（UI を組む順番の問題）は要らない
3. **`UiRoot` は `Bootstrap`・`Title`・`Home` の 3 シーンに 1 つずつ置く**（`SceneBuilder` が P1 の `UiRoot.prefab` を置く。`DontDestroyOnLoad` にしない。`Produce` は 4 本目（formation）が置く: 決めたこと 12）。`IScreenNavigator`（基盤の `ScreenNavigator`）・`PageStack`・`DialogService` は、**各シーンの `LifetimeScope` がそのシーンの `UiRoot` のコンテナーで登録する**（`SceneUiInstaller.Install`）。**`Bootstrap` の `UiRoot` は、マスタを読めなかったとき（と起動の失敗）のダイアログにだけ使う**。これで IMGUI の `StartupView` を消せる（P1 の決めたこと 10 と同じ。`Produce` は 4 本目）
4. **ルートのスコープ（Bootstrap）にある `ApiActivityBridge`（ブロッカー）と `ErrorDialogView`（共通エラーのダイアログ）が「いまのシーン」の `BusyIndicator`・`DialogService` を見つける方法**: ルートに `ActiveSceneUi`（入れ物。1 つ）を登録し、各シーンのスコープの `SceneUiActivation`（エントリーポイント）が `Start` で自分のシーンの `UiRoot` と `DialogService` を入れ、`Dispose`（シーンを閉じたとき）で自分のものだけを外す。シーンの切り替えの間（入れ物が空）にエラーが出たら、ダイアログを出さずに既定のボタン（診断の自動応答と同じ対応）を返す。ブロッカーは、通信の開始のときのシーンの `BusyIndicator` に `Begin` し、同じものに `End` する
5. **基盤の `IScreenNavigator` をそのまま使う**（自前の Navigator・Presenter の基底を作らない）。`Replace` は作らない。局面の切り替えは「Pop してから Push」、シーンの中でホームへ戻るのは `PageStack.PopToRootAsync`、**シーンをまたいでホームへ戻るのは `ISceneNavigator.GoToHomeAsync`**（プロデュースからホームへ戻る、など）
6. **この計画は P2 の後に 1 つの計画として実行する**（段に分けない）。全体の順は「計画 0 → P1 → P2 → **この計画** → gacha → owned → P3 → formation → week → lesson」。P2 はこの計画の型を使わない。`TitlePage`・`HomePage` の View は、P2 が作った骨格（フィールドだけ）にこの計画が `ITitleView`・`IHomeView` の実装を足す
7. **`TitlePage`・`HomePage` のフィールド**（3.9）は P2 の 3.1・3.3 と一致している（`HomePage.resumePanel` は `GameObject`）。食い違いが見つかったら P2 が正で、この計画の 3.9 と View の実装を直す
8. **診断（`-uta-quit-after-check`）は `TitleDiagnostics` に分ける**（screen-design 8章「画面の計画で、診断を別の入口に分ける」）。`Title` シーンの `TitleEntry` が、診断なら画面を出さずに `TitleDiagnostics` を走らせ、そうでなければ `TitlePage` を積む。ログの行（`sign-in: ok`・`connectivity check: OK`・`start: FAILED`）は今までと同じ文字列。`TitleDiagnostics` は終了を直接呼ばず、コンストラクターで `Action<int> quit`（既定 `UnityEngine.Application.Quit`）を受ける（テストで終了コードを確かめるため）
9. **タイトルでの失敗は、状態の文を出して、もう一度タップできる**（screen-design 8章）。旧版の「起動に失敗しました（再試行／終了）」のダイアログと実行番号（`currentRun`）は作らない（`ReturnToTitle` で `Title` シーンが読み込み直されると、古い Presenter はスコープごと破棄されるので、Presenter の `disposed` の印で止める）
10. **プロデュースを破棄した知らせは `Home` で 1 回だけ出す**（screen-design 4章）: `PlayerCache` に「知らせ済み」の印を足し（`Set` で戻る）、`HomePresenter.DidPushEnter` で出す
11. **エラーの「ホームへ」は、所持データを取り直してから `Home` を読み込み直す**（`ErrorNavigator` が `IObjectResolver` を受け、`GoToHomeAsync` の中で `RefreshPlayerUseCase` を遅延解決して呼ぶ。コンストラクターで受けると `UtaApiClient` → `IErrorNavigator` → `RefreshPlayerUseCase` → `IPlayerPort` → `UtaApiClient` の DI の循環になるため。取り直しの失敗はログだけで、ホームへは行く。取り直しの中で再び「ホームへ」が来たら、取り直しを飛ばす）。`ApiCore` は順番待ちを解いてから遷移を呼ぶので、遷移の中で通信してよい
12. **ホームの 3 つのボタンの動作（プロデュース・ガチャ・アイドル）は「準備中です。」のダイアログ**（2〜4 本目が自分のボタンを差し替える。「プロデュース」は主ボタンとナビの 2 つのボタンが同じ `ProduceClicked` を起こす。決めたこと 17）。#24 の「PRODUCE → `Produce` シーン」はやめる。**`Produce` シーンはこの計画では変えない**（#24 の仮の画面のまま: `ProduceLifetimeScope`・`ProduceView`・`ProducePresenter`・`SceneBuilder` の `BuildProduce` を消さない・直さない）。ホームから `Produce` へ移る道が無くなるので、4 本目（formation）までは使われない。4 本目が `Produce` シーンを作り直す（`UiRoot` を置き、`ProduceLifetimeScope` を `SceneUiInstaller` と入口の形にし、#24 の仮の画面を消す。底のページはいつも `ProduceBackdropPage`）
13. 業務エラー 18 個を `UtaErrors` に `ErrorAction.Stay` で登録する（旧版と同じ）。`IGameMaster`（`GetCardText`・`GetSupportAbilities` を含む）・`MasterGameData`・`RefreshPlayerUseCase` も旧版と同じ
14. 部品の View（P1 の骨格）に表示のメソッドを足す（3.2）。見出しを変える `HeaderView.SetTitle`、ラベルと押せるかを変える `ButtonView.SetLabel`・`SetInteractable` を含む（P3 の画面と formation・week・lesson が使う）。後続の計画はこれを使い、部品の View を書き換えない
15. 型の名前の衝突: 基盤に `UnityScreenNavigator.ISceneNavigator`（別物）がある。`using UnityScreenNavigator;` と `using Uta.Client.Application.Navigation;` の両方を書くファイルは、`using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;` を足す（3.3.10。後続の計画も従う）
16. **ホームのボタンのテストはイベントごとに分ける**（5章の `ProduceClicked_ShowsNotReady`・`GachaClicked_ShowsNotReady`・`OwnedClicked_ShowsNotReady` の 3 本。2026-11-07 に 4 本から変更: 再開のボタンが無くなったため）。2〜4 本目は、自分が差し替えたボタンのテストだけを書き換える（「準備中です。」が何回出るかに依らない）
17. **ホームの構成とキャラ表示のステージ（2026-11-07 追加。ユーザーの判断。screen-design 7.8）**: P2 の `HomePage` は、上部の `ProfileText`、中央のステージ `Stage`（`RawImage` と枠の文字 `StageLabel`）、「プロデュース中」の帯 `ResumePanel`、大きな主ボタン `PrimaryButton`、下部のナビ `Nav`（プロデュース・ガチャ・アイドル）になった（P2 の決めたこと 15）。C# は次のとおり: (a) `IHomeView` のイベントは `ProduceClicked`・`GachaClicked`・`OwnedClicked` の 3 つ。**`ResumeClicked` と `HomePresenter.ResumeProduceAsync` は作らない**（主ボタンとナビの「プロデュース」はどちらも `ProduceClicked`。進行中の再開・放棄は「プロデュース」を押したときの選択: formation）。(b) `SetResumeVisible(visible)` は帯の表示に加えて、主ボタンのラベルを「再開」／「プロデュース」に切り替える。(c) キャラ表示は `ICharacterStage`（`ShowIdol(int idolId)`・`Clear()`）で隠し、MVP の実装 `PlaceholderCharacterStage`（枠の文字に育成アイドルの名前）を `HomePage.CreateStage(IGameMaster)` が作る。`HomePresenter` は所持の育成アイドルのうちレア度が最も高いもの（同率なら `IdolId` が小さいもの）を選んで `ShowIdol`、所持が 0 なら `Clear`。3D の `ModelCharacterStage` は MVP 外で、差し替えるのは `HomePage.CreateStage` だけ。(d) `HomePresenter` のコンストラクターに `IGameMaster master` を足し、テスト用のコンストラクターは `IHomeView` の直後に `ICharacterStage stage` を受ける（3.7）

## ユーザーの確認が要ること

なし（2026-11-07 の判断で、計画の順・`Bootstrap` の `UiRoot`・ホームの「プロデュース」の扱いは決めたこと 3・6・12 に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）の C# の 1 本目。画面を作る共通の土台（ダイアログ・ブロッカー・ページの数え方・シーンのスコープへの登録・テスト用の偽）、マスタの読み取り口（`IGameMaster`）、業務エラーの登録、タイトル（起動の流れ）とホームを、PR #24 のシーン構成と P1・P2 の Prefab の上に作る。2〜6 本目（ガチャ・所持一覧・編成とプロデュースの開始・週の行動選択・レッスン）は、**3章の公開 API と 3.3 の共通ルールを正**として、この計画の部品を使う。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TextMeshPro。基盤の Presenter 駆動のナビゲーション。`Replace` は作らない。通信・状態・マスタの扱い）と 8章（シーン構成）。起動の順序の正は同 4章。ホームの見た目の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md)「ホーム」（見た目の値は P2 が持つ）
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面）、[0006](../wiki/decisions/0006-adopt-unity-7-early.md)（Unity 7）、[0005](../wiki/decisions/0005-foundation-submodule.md)（共通基盤。`external/` は直さない）
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0（[masterdata-card-text](2026-11-03-masterdata-card-text.md)）→ P1（[prefabs-common](2026-11-07-client-prefabs-common.md)）→ P2（[prefabs-screens-a](2026-11-07-client-prefabs-screens-a.md)）→ **この計画** → gacha → owned → P3（[prefabs-screens-b](2026-11-07-client-prefabs-screens-b.md)）→ formation → week → lesson（決めたこと 6）
- 画面遷移は、基盤 `external/UnityScreenNavigator` の `Runtime/ChinpanGX`（`IScreenNavigator`・`ScreenNavigator`・`IPresenter`・`ILifecycleHandler`・`IScreenWithArgs<TArgs>`・`[AssetAddress]`・`OverlayContainer`。使い方は基盤の README「Presenter-Driven Navigation」）を**そのまま使う**
- **PR #28 の読み替え**: PR #28（未マージ。PR #28 が足す ADR）は `external/` のサブモジュールを全部消す（UnityScreenNavigator・Supplement・VolTech-MagicOnion・MasterMemoryAddons）。サーバー・shared は `packages/` の `.nupkg`、Unity は `manifest.json` の git URL で参照し、`Uta.slnx` から共通基盤が外れる。**PR #28 のあとは、共通基盤のソース（USN の `Runtime/ChinpanGX`・README・`AddressableAssetLoader`、VolTech-MagicOnion のコードなど）は `client/uta/Library/PackageCache/<パッケージ名>@<ハッシュ>/`（USN は `com.harumak.unityscreennavigator`。Unity でパッケージを解決した環境にだけある）か GitHub のリポジトリを読む。マージ前は `external/<名前>`**。この計画に書いた `external/` のパスは、そう読み替える（`external/` は直さない、の決まりは変わらない）
- **前提 1**: PR #24（シーン構成・サインアップ・サインイン）が main に入っていること（入っている）。触る既存のコードは `client/uta/Assets/Scripts/` の `Bootstrap/`・`Presentation/{Title,Home,Startup}/`・`Editor/SceneBuilder.cs`・`Infrastructure/Api/`（`Presentation/Produce/` は触らない: 決めたこと 12）
- **前提 2**: 計画 0（[masterdata-card-text](2026-11-03-masterdata-card-text.md)）が **main にマージ済み**で（積み PR の連鎖 main → P1 → P2 → この計画には計画 0 の変更が入らないので、計画 0 を先に main へマージしてから P1 を始める。[索引](2026-11-07-client-screens-index.md) 1章・5章の 1）、生成された `Uta.MasterData.Card` に `string Name`、`Uta.MasterData.CardFace` に `string Description` があること。タスク 2 の前に確かめ、無ければ止まって報告する
- **前提 3**: P1 が done で、`Presentation/Common/` に `UiTheme`・`PageBase`・`ModalBase`・`UiRoot`・`BusyIndicator`（骨格）・`SafeAreaFitter`・`WindowAspectKeeper`、`Common/Parts/` に部品の View の骨格、`Assets/Prefabs/Parts/` に 10 個の部品（`UiRoot.prefab` を含む）、テストの `PrefabContract` があること。タスク 1 の前に確かめ、無ければ止まって報告する。テストは P1 の決めたこと 12 の回し方で回す（Editor を開いたまま `TestRunnerApi` を eval で回す。コマンドは CLAUDE.md「ビルド・テスト・実行」・unity7-evaluation 4章）で回す。**eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない**（P1 の決めたこと 12）
- **前提 4**: P2 が done で、`Presentation/Title/TitlePage.cs`・`Presentation/Home/HomePage.cs`（`PageBase` の派生。3.9 のフィールド）と、`Assets/Prefabs/Screens/Title/TitlePage.prefab`・`Assets/Prefabs/Screens/Home/HomePage.prefab`（Addressables の `Screens` に、アドレス `TitlePage`・`HomePage`）があること。タスク 1 の前に確かめ、3.9 とフィールドの名前・型が違えば止まって報告する
- **前提 5**: Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready`）。**Editor を開けない環境では、コンパイル・テスト・Play のどれも確かめられない**。そのときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **この計画で決めないこと**: ガチャ・所持一覧・編成・プロデュースの画面（2〜6 本目）。ホームの 3 つのボタンの動作（プロデュース・ガチャ・アイドル）の行き先（2〜4 本目が `HomePresenter` の対応するメソッドの中身を書き換える）。`Produce` シーンの作り直し（4 本目）。モーダル（`PushModalAsync`）を実際に出して確かめるのは、最初に使う計画（2 本目のガチャの結果）。画面の Prefab と見た目（P2・P3）。Android・iOS の実機（順 7 の別項目）
- **Unity 7 の未確認**: 基盤の `ScreenNavigator`（Presenter 駆動。シーンのスコープの子スコープで Presenter を解決し、Addressables から Prefab を読む）は、P1 のスパイク（`PageContainer` + `AddressableAssetLoader` の Push）より先は動作未確認。タスク 4 の Play で確かめ、**止まる条件に当たったら止まって報告する**（勝手に別の手段へ変えない）

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs`・フォルダには Unity の再コンパイルのあとに生成された `.meta` をコミットする。

| タスク | 操作 | パス | 中身 |
|---|---|---|---|
| 1 | 変更 | `Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | 参照に `Uta.Client.Bootstrap`・`VContainer` を足す（3.1） |
| 1 | 変更 | `Scripts/Presentation/Common/BusyIndicator.cs` | `Begin`・`End`・`IsBusy` を足す（3.2。P1 の骨格） |
| 1 | 変更 | `Scripts/Presentation/Common/Parts/ButtonView.cs`・`HeaderView.cs`・`TabView.cs`・`TabBarView.cs`・`DialogView.cs`・`ListRowBase.cs`・`ListRowView.cs` | 表示のメソッドを足す（3.2。P1 の骨格） |
| 1 | 作成 | `Scripts/Presentation/Common/UiObjects.cs` | 消す・子を全部消す・行を作るの補助（3.2） |
| 1 | 作成 | `Scripts/Presentation/Common/DialogResult.cs`・`DialogService.cs` | 確認・お知らせ・選択のダイアログ（3.2） |
| 1 | 作成 | `Scripts/Presentation/Common/PageStack.cs` | ページ・モーダルの枚数を数えて底まで戻す（3.2） |
| 1 | 作成 | `Scripts/Presentation/Common/ActiveSceneUi.cs`・`SceneUiActivation.cs`・`SceneUiInstaller.cs` | いまのシーンの UI の入れ物と、シーンのスコープへの登録（3.2） |
| 2 | 作成 | `Scripts/Application/Master/MasterItemKind.cs`・`IdolInfo.cs`・`SupportCardInfo.cs`・`SupportAbilityInfo.cs`・`GachaEntryInfo.cs`・`GachaInfo.cs`・`ScenarioWeekInfo.cs`・`ScenarioInfo.cs`・`SupportLevelUpCost.cs`・`CardTextInfo.cs`・`IGameMaster.cs` | マスタの読み取り用の型と `IGameMaster`（3.4。1 ファイル 1 型） |
| 2 | 作成 | `Scripts/Application/Player/RefreshPlayerUseCase.cs` | サインインで所持データを取り直す（3.5） |
| 2 | 変更 | `Scripts/Application/Player/PlayerCache.cs` | 破棄の知らせの印を足す（3.5） |
| 2 | 作成 | `Scripts/Infrastructure/MasterData/MasterGameData.cs` | `MemoryDatabase` → `IGameMaster`（3.6） |
| 2 | 変更 | `Scripts/Infrastructure/Api/UtaErrors.cs` | 業務エラー 18 個を `ErrorAction.Stay` で足す（3.8） |
| 1 | 作成 | `Tests/EditMode/Fakes/CallLog.cs`・`FakeScreenNavigator.cs`・`FakeDialogService.cs`・`FakeSceneNavigator.cs`・`StubDeviceAuthPort.cs`・`StubPlayerPort.cs` | テスト用の偽（3.3.8。2〜6 本目も使う） |
| 3 | 作成 | `Tests/EditMode/Fakes/StubIdolMaster.cs` | 育成アイドルだけを返す `IGameMaster` の偽（3.3.8。`IGameMaster` はタスク 2 で作るので、タスク 3 で作る） |
| 1・2 | 作成 | `Tests/EditMode/BusyIndicatorTests.cs`・`DialogServiceTests.cs`・`PageStackTests.cs`・`ActiveSceneUiTests.cs`・`MasterGameDataTests.cs`・`RefreshPlayerUseCaseTests.cs`・`PlayerCacheTests.cs` | EditMode のテスト（5章） |
| 2 | 変更 | `Tests/EditMode/UtaErrorsTests.cs` | 登録の数と `Stay` の確認に直す（5章） |
| 3 | 変更 | `Scripts/Presentation/Title/TitlePage.cs`・`Scripts/Presentation/Home/HomePage.cs` | P2 の骨格に `ITitleView`・`IHomeView` の実装を足す（3.7） |
| 3 | 作成 | `Scripts/Presentation/Title/ITitleView.cs`・`TitleEntry.cs`・`TitleDiagnostics.cs`・`StartupLogs.cs` | タイトルの View の IF・入口・診断・ログの文（3.7） |
| 3 | 変更 | `Scripts/Presentation/Title/TitlePresenter.cs` | 中身を全部置き換える（`IPresenter`。3.7） |
| 3 | 変更 | `Scripts/Presentation/Title/TitleLifetimeScope.cs` | `SceneUiInstaller`・`TitleDiagnostics`・`TitleEntry`（3.7） |
| 3 | 削除 | `Scripts/Presentation/Title/TitleView.cs` | legacy `Text` の仮の View |
| 3 | 変更 | `Scripts/Presentation/Title/ConnectivityCheck.cs`・`Scripts/Application/Auth/SignInUseCase.cs` | コメントだけ直す（「画面の計画で消す」→ 診断の `TitleDiagnostics` が使う。3.7） |
| 3 | 作成 | `Scripts/Presentation/Home/IHomeView.cs`・`HomeEntry.cs` | ホームの View の IF・入口（3.7） |
| 3 | 作成 | `Scripts/Presentation/Home/ICharacterStage.cs`・`PlaceholderCharacterStage.cs` | キャラ表示のステージの IF と MVP の実装（3.7。決めたこと 17） |
| 3 | 変更 | `Scripts/Presentation/Home/HomePresenter.cs`・`HomeLifetimeScope.cs` | 中身を全部置き換える（3.7） |
| 3 | 削除 | `Scripts/Presentation/Home/HomeView.cs` | legacy `Text` の仮の View |
| 4 | 削除 | `Scripts/Presentation/Startup/StartupView.cs` | IMGUI のマスタの失敗の表示（決めたこと 3。`Bootstrap` の `UiRoot` のダイアログに置き換える） |
| 4 | 変更 | `Scripts/Bootstrap/Uta.Client.Bootstrap.asmdef` | 参照に `UnityScreenNavigator` を足す（3.1） |
| 4 | 作成 | `Scripts/Bootstrap/ApiActivityBridge.cs` | 通信中のブロッカー（3.8） |
| 4 | 変更 | `Scripts/Bootstrap/ErrorDialogView.cs` | IMGUI をやめ、`ActiveSceneUi` の `DialogService` を使う普通のクラスにする（3.8） |
| 4 | 変更 | `Scripts/Bootstrap/ErrorNavigator.cs` | 「ホームへ」の前に取り直す（`IObjectResolver` で遅延解決。3.8） |
| 4 | 変更 | `Scripts/Bootstrap/BootstrapEntry.cs` | `UiRoot` の参照、マスタの失敗のダイアログ、起動の失敗のダイアログ（`ShowFailureDialog`。破棄済みの `UiRoot` ではログだけ）、`IGameMaster`・`ActiveSceneUi` などの登録（3.8） |
| 4 | 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Create` に `IApiActivityHandler` を足す（3.8） |
| 3 | 変更 | `Scripts/Editor/SceneBuilder.cs` | `Bootstrap`・`Title`・`Home` に `UiRoot.prefab` を置く。`BuildProduce` と、それが使う legacy `Text` の補助は残す（3.10） |
| 4 | 生成 | `Scenes/Bootstrap.unity`・`Title.unity`・`Home.unity` | `Uta/Create Scenes` で作り直す（`Produce.unity` も作り直されるが、中身は #24 のまま） |
| 4 | 変更 | `client/uta/ProjectSettings/ProjectSettings.asset` | Windows のウィンドウと向き（タスク 4） |
| 3・4 | 作成 | `Tests/EditMode/TitlePresenterTests.cs`・`TitleEntryTests.cs`・`HomePresenterTests.cs`・`PlaceholderCharacterStageTests.cs`・`HomeEntryTests.cs`・`ApiActivityBridgeTests.cs`・`ErrorDialogViewTests.cs`・`ErrorNavigatorTests.cs`・`BootstrapEntryTests.cs` | EditMode のテスト（5章） |
| 6 | 変更 | `uta_docs/wiki/architecture/screen-design.md` | 4・5・7.4・7.5・8章の古い記述、実装メモ（タスク 6） |
| 6 | 変更 | `uta_docs/wiki/architecture/repository-layout.md` | 126 行の `ErrorDialogView`、277 行付近の USN の確認範囲（タスク 6） |
| 6 | 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | 2章の表、3.3、4章（タスク 6） |

`Presentation/Startup/StartupOptions.cs`（診断の印）・`Presentation/Title/ConnectivityCheck.cs`（コメントだけ直す）・`Bootstrap/SceneNavigator.cs`・`Application/Navigation/ISceneNavigator.cs` は変えずに使う。`Presentation/Produce/`（`ProduceLifetimeScope.cs`・`ProduceView.cs`・`ProducePresenter.cs`）は変えない（決めたこと 12。4 本目が作り直す）。

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。ファイルスコープ namespace・プライマリコンストラクタ・コレクション式 `[...]`・Index/Range・record は使わない。配列は `new[] { ... }`）。BOM 付き UTF-8、LF、Allman、`var`、private フィールドは接頭辞なしの camelCase。名前空間は、フォルダ `Scripts/Presentation/Common` が `Uta.Client.Presentation.Common`、`Common/Parts` が `Uta.Client.Presentation.Common.Parts`、`Title` が `Uta.Client.Presentation.Title`、`Home` が `Uta.Client.Presentation.Home`、`Application/Master` が `Uta.Client.Application.Master`、`Application/Player` が `Uta.Client.Application.Player`、`Infrastructure/MasterData/MasterGameData.cs` が `Uta.Client.Infrastructure`（同じフォルダの `MasterCardCatalog` に合わせる）、`Bootstrap` が `Uta.Client.Bootstrap`、テストが `Uta.Client.Infrastructure.Tests`（既存のテストと同じ）。各シーンの `LifetimeScope` は今までどおり `Uta.Client.Presentation`（#24 のまま）。

### 3.0 後続の計画が使う公開 API（一覧）

| 型・メンバー | 置き場 | 節 |
|---|---|---|
| `IScreenNavigator`（基盤）の Push／Pop／`WaitForPopAsync`。`playAnimation: false` | 各シーンのスコープ | 3.3.1 |
| `ISceneNavigator.GoToTitleAsync`・`GoToHomeAsync`・`GoToProduceAsync`（#24） | ルート | 3.3.1 |
| `PageStack.Depth`・`ModalDepth`・`PopToRootAsync(IScreenNavigator)` | 各シーンのスコープ | 3.2 |
| `DialogService.ShowInfoAsync`・`ConfirmAsync`・`ChooseAsync`、`DialogResult` | 各シーンのスコープ | 3.2 |
| `BusyIndicator.Begin`・`End`・`IsBusy`（画面は直接呼ばない） | `UiRoot.Busy` | 3.2 |
| `UiObjects.Destroy`・`ClearChildren`・`AddRow<T>` | 静的 | 3.2 |
| 部品の View のメソッド（`ButtonView.Button`・`SetLabel`・`SetInteractable`・`SetColor`、`HeaderView.BackButton`・`RightSlot`・`SetTitle`、`TabView.Button`・`SetLabel`・`SetSelected`、`TabBarView.Count`・`GetTab`・`TabClicked`・`SetSelected`、`ListRowBase.Button`・`SetInteractable`、`ListRowView.SetTexts`、`DialogView.SetMessage`・`AddButton`・`Destroyed`） | `Common/Parts` | 3.2 |
| `SceneUiInstaller.Install(IContainerBuilder, UiRoot)`（新しいシーンのスコープを作るとき） | 静的 | 3.2 |
| `IGameMaster`（`GetCardText`・`GetSupportAbilities` を含む）と読み取り用の型 | ルート | 3.4 |
| `RefreshPlayerUseCase.ExecuteAsync` | ルート | 3.5 |
| `PlayerCache.DiscardNoticeShown`・`MarkDiscardNoticeShown` | ルート | 3.5 |
| `HomePresenter` のコンストラクター（2 本）と `OpenProduceAsync`・`OpenGachaAsync`・`OpenOwnedAsync` | `Home` | 3.7 |
| `ICharacterStage`・`PlaceholderCharacterStage`・`HomePage.CreateStage`（ホームのキャラ表示。3D に差し替える拡張点） | `Home` | 3.7 |
| `BootstrapEntry.RegisterUseCases`（ユースケース・ゲートウェイを 1 行ずつ足す場所） | `Bootstrap` | 3.8 |
| `UtaErrors.Create()`（登録済みの業務エラー） | `Infrastructure.Api` | 3.8 |
| テスト用の偽 `CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`・`StubDeviceAuthPort`・`StubPlayerPort`・`StubIdolMaster` | `Tests/EditMode/Fakes` | 3.3.8 |
| 画面を作る共通ルール | — | 3.3 |

### 3.1 asmdef の参照

P1 が `Uta.Client.Presentation`（`UnityScreenNavigator`・`Unity.TextMeshPro` など）・`Uta.Client.Editor`・テストの asmdef（`Uta.Client.Presentation`・`UnityScreenNavigator`・`UnityEngine.UI`・`Unity.TextMeshPro`・Addressables）の参照を足してある。この計画で足すのは次だけ:

- `Uta.Client.Infrastructure.Tests.EditMode.asmdef` の `references` の最後に `"Uta.Client.Bootstrap"`, `"VContainer"` を足す（タスク 1。`ApiActivityBridge`・`ErrorDialogView`・`ErrorNavigator`・`BootstrapEntry` のテストと、`ErrorNavigatorTests` の `ContainerBuilder` のため。`Uta.Client.Bootstrap` は `autoReferenced: false` なので名前で足す）
- `Uta.Client.Bootstrap.asmdef` の `references` の最後に `"UnityScreenNavigator"`, `"Unity.TextMeshPro"`, `"UnityEngine.UI"` の 3 つを、この順で足す（タスク 4。3 つとも必ず足す）。`Bootstrap` が使う `UiRoot`（`PageContainer` などの USN の型）・`DialogView`（`TMP_Text`）・`ButtonView`（`UnityEngine.UI.Button`）・`BusyIndicator`（`Image`）のメンバーの型が、この 3 つのアセンブリにあるため。asmdef の名前は `com.unity.ugui` 7.0.0 のパッケージ（`Library/PackageCache/com.unity.ugui@<ハッシュ>/Runtime/TMP/Unity.TextMeshPro.asmdef`・`Runtime/UGUI/UnityEngine.UI.asmdef`）で確かめた（2026-11-07）。`UnityScreenNavigator` は P1 が `Uta.Client.Presentation` に足したものと同じ名前
- `Uta.Client.Presentation.asmdef` は変えない（Input System は `UiRoot.prefab` の `EventSystem` が持つので、C# から参照しない）

### 3.2 共通部品（`Uta.Client.Presentation.Common`）

#### `BusyIndicator`（P1 の骨格に足す）

```csharp
public sealed class BusyIndicator : MonoBehaviour
{
    [SerializeField] private Image blocker;   // P1 のまま

    private int count;

    public bool IsBusy => count > 0;

    /// <summary>数を 1 増やし、0 → 1 のとき blocker が null でなければ blocker.enabled = true。</summary>
    public void Begin();

    /// <summary>数が 0 なら何もしない。そうでなければ 1 減らし、1 → 0 のとき blocker が null でなければ blocker.enabled = false。</summary>
    public void End();
}
```

#### 部品の View（`Uta.Client.Presentation.Common.Parts`。P1 の骨格に足す）

フィールドは P1 のまま。足すのは次のメンバーだけ（どれも Prefab の参照を使うだけで、値の検証はしない）。

| 型 | 足すメンバー |
|---|---|
| `ButtonView` | `public Button Button => button;`（`onClick` の購読用）、`public void SetLabel(string text)`（`label.text = text`）、`public void SetInteractable(bool value)`（`button.interactable = value`。見た目は ColorTint）、`public void SetColor(Color color)`（`background.color = color`） |
| `HeaderView` | `public ButtonView BackButton => backButton;`、`public RectTransform RightSlot => rightSlot;`、`public void SetTitle(string text)`（`titleText.text = text`） |
| `TabView` | `public Button Button => button;`、`public void SetLabel(string text)`、`public void SetSelected(bool selected)`（`underline.enabled = selected`） |
| `TabBarView` | `public int Count => tabs.Length;`、`public TabView GetTab(int index)`（`tabs[index]`）、`public event Action<int> TabClicked;`、`private void Awake()`（各 i について `var index = i; tabs[i].Button.onClick.AddListener(() => TabClicked?.Invoke(index));`）、`public void SetSelected(int index)`（`index < 0 \|\| index >= Count` なら `ArgumentOutOfRangeException`。各 i で `tabs[i].SetSelected(i == index)`） |
| `DialogView` | `public event Action Destroyed;`、`public void SetMessage(string message)`（`messageText.text = message`）、`public ButtonView AddButton(string name, string label, Color color)`（`var b = UnityEngine.Object.Instantiate(buttonPrefab, buttonList, false); b.gameObject.name = name; b.SetLabel(label); b.SetColor(color); return b;`）、`private void OnDestroy()`（`Destroyed?.Invoke()`） |
| `ListRowBase` | `public Button Button => button;`、`public void SetInteractable(bool value)`（`button.interactable = value`）、`protected Image Background => background;` |
| `ListRowView` | `public void SetTexts(string title, string sub, string right)`（3 つの `TMP_Text.text` に入れる。null は空文字にする） |

#### `UiObjects`・`DialogResult`・`DialogService`

```csharp
public static class UiObjects
{
    /// <summary>target が null なら何もしない。Application.isPlaying なら target.SetActive(false) してから Object.Destroy（レイアウトから即座に外すため）、そうでなければ Object.DestroyImmediate。</summary>
    public static void Destroy(GameObject target);

    /// <summary>parent の子を最後から順に全部 Destroy する。</summary>
    public static void ClearChildren(Transform parent);

    /// <summary>
    /// 一覧の行を作る: Object.Instantiate(rowPrefab, parent, false)。名前は name。戻り値はその行の View。
    /// rowPrefab・parent が null なら ArgumentNullException。
    /// </summary>
    public static T AddRow<T>(T rowPrefab, Transform parent, string name) where T : Component;
}

public enum DialogResult { Positive, Negative }

/// <summary>
/// テストで派生させる（FakeDialogService）ので sealed にせず、公開メソッドは virtual。
/// USN のモーダルにしない（ページ遷移の最中にも出せるように、USN の遷移の外の素の層（UiRoot.DialogLayer）に出す）。
/// </summary>
public class DialogService
{
    /// <summary>layer・prefab を保存するだけ（C# の null を許す。ArgumentNullException は投げない。FakeDialogService が base(null, null) で作り、公開メソッドを全部上書きするので layer を使わない。下の「順番が来たら」）。</summary>
    public DialogService(RectTransform layer, DialogView prefab);

    /// <summary>お知らせ。ボタンは「OK」1 つ（ButtonPrimary）。押されたら閉じて終わる。</summary>
    public virtual UniTask ShowInfoAsync(string message);

    /// <summary>確認。ボタンは上から positiveLabel（ButtonPrimary）→ Positive、negativeLabel（ButtonSecondary）→ Negative。</summary>
    public virtual UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ");

    /// <summary>
    /// labels を上から並べ（全部 ButtonPrimary）、押されたボタンの添字（0 始まり）を返す。押されずにダイアログが消えたら（シーンを閉じた）−1。
    /// labels が null か、長さが 0 または 5 以上なら、待ち行列に入れる前に ArgumentException を投げる。
    /// </summary>
    public virtual UniTask<int> ChooseAsync(string message, string[] labels);
}
```

`DialogService` の動き:
- 待ち行列: ダイアログの要求を FIFO で 1 つずつ処理する。先頭のダイアログが閉じて（結果が決まって）から、次のダイアログを出す。同時に出るのは 1 つだけ。前のダイアログが例外で終わっても次へ進む。基盤の `TransitionQueue`（internal なので使えない。作りの参考だけ）と同じ考え方で、次の骨子のとおりに書く:
  ```csharp
  private UniTask tail = UniTask.CompletedTask;   // 最後に積んだ要求が終わったら完了する

  // ShowInfoAsync・ConfirmAsync・ChooseAsync は、引数の検証（ChooseAsync のラベル）のあと、これを呼んで添字を読み替える
  private async UniTask<int> EnqueueAsync(string message, string[] labels, Color[] colors)
  {
      var previous = tail;
      var done = new UniTaskCompletionSource();
      tail = done.Task;                            // 次の要求は、この要求の done を待つ
      try
      {
          await previous;                          // 前の要求が終わるまで待つ（previous は done.Task なので例外にならない）
          if (IsLayerDestroyed()) return -1;       // 要求ごとに、開始時に層の破棄を確かめる（シーンを閉じたあとの待ち行列の残り。下の「順番が来たら」）
          return await ShowOneAsync(message, labels, colors);   // 下の「順番が来たら」。ボタンが押されるかダイアログが消えるまで待つ
      }
      finally
      {
          done.TrySetResult();                     // 成功・例外を問わず、次の要求へ進める
      }
  }

  // C# の null（コンストラクターに null を渡した）は「破棄済み」に数えない（ShowOneAsync が InvalidOperationException を投げる）。
  // Unity の null（GameObject が破棄済み。ReferenceEquals では null でないのに == null が true）だけを破棄済みとする
  private bool IsLayerDestroyed()
  {
      return !ReferenceEquals(layer, null) && layer == null;
  }
  ```
  `ShowOneAsync` は `UniTaskCompletionSource<int>` を 1 つ作り、下の「順番が来たら」でダイアログを出し、`Complete(i)`／`Destroyed` でその源に結果を入れ、`source.Task` を返す。待ち行列が空のとき（`previous` が完了済み）は、`await previous` が同期で抜けるので、ダイアログは呼んだその場で（同じフレーム・同期で）出る。`done.TrySetResult()` は待っている次の要求の続きを同期で走らせるので、前のダイアログのボタンを押したその場で次のダイアログが出る（5章の `DialogServiceTests` はこれを前提にする）
- 層の null は 2 通りに分けて扱う（要求ごとに、順番が来たときに確かめる。待ち行列に入れるときではない）:
  - **Unity の null（破棄済み）**: シーンを閉じて `UiRoot` ごと `layer` が破棄されたあと、待ち行列に残っていた要求・新しく来た要求。ダイアログを出さずに −1 を結果にする（`ShowInfoAsync` はそのまま終わる、`ConfirmAsync` は `Negative`、`ChooseAsync` は −1。上の骨子の `IsLayerDestroyed`）。例外にしない（`MissingReferenceException` を出さない）
  - **C# の null（コンストラクターに null を渡した）**: テスト用に層を使わない経路（`FakeDialogService` は公開メソッドを全部上書きするので、ここに来ない）。来たら下の `InvalidOperationException`（今までの仕様のとおり。`ArgumentNullException` にはしない）
- 順番が来たら、同じフレームのうちに（`UniTask.Yield` を挟まず）: `ReferenceEquals(layer, null) || ReferenceEquals(prefab, null)` なら `InvalidOperationException("dialog layer is not set")`。`var view = UnityEngine.Object.Instantiate(prefab, layer, false); view.gameObject.name = "Dialog"; view.SetMessage(message);` → ボタンを `view.AddButton("Button" + i, label, 色)` で上から順に作り、`button.Button.onClick.AddListener(() => Complete(i))`
- `Complete(i)`: 済みなら何もしない。済みにして `UiObjects.Destroy(view.gameObject)` し、i を結果にする。`view.Destroyed` では、済みでなければ済みにして −1 を結果にする（シーンを閉じてダイアログが消えた）
- 結果の読み替え: `ShowInfoAsync` は添字を捨てる。`ConfirmAsync` は 0 → `Positive`、それ以外（1・−1）→ `Negative`。`ChooseAsync` は添字をそのまま返す。`ShowInfoAsync`・`ConfirmAsync` は `async` で `await EnqueueAsync(...)` して読み替える。**`ChooseAsync` は `async` にしない**: ラベルを検証して（違反なら同期に `ArgumentException` を投げる）から、`EnqueueAsync(...)` の `UniTask<int>` をそのまま返す（`async` にすると例外が `UniTask` の中に入り、呼んだその場で投げられないため）
- ボタンの色は `UiTheme.ButtonPrimary`／`UiTheme.ButtonSecondary`（P1）。文字の大きさ・並びは `Dialog.prefab`（P1）が決める

#### `PageStack`

```csharp
public sealed class PageStack
{
    /// <summary>VContainer が使う。</summary>
    [Inject]
    public PageStack(PageContainer pages, ModalContainer modals);   // : this(() => pages.Pages.Count, () => modals.Modals.Count)

    /// <summary>テスト用。</summary>
    public PageStack(Func<int> pageCount, Func<int> modalCount);

    /// <summary>積まれているページの枚数（呼んだ時点）。</summary>
    public int Depth { get; }
    /// <summary>積まれているモーダルの枚数（呼んだ時点）。</summary>
    public int ModalDepth { get; }

    /// <summary>
    /// シーンの底のページ（ホームなど）へ戻る。ModalDepth > 0 なら navigator.PopModalAsync(playAnimation: false, popCount: ModalDepth) を待ち、
    /// そのあと Depth > 1 なら navigator.PopPageAsync(playAnimation: false, popCount: Depth - 1) を待つ。底が 1 枚以下なら Pop を呼ばない。
    /// </summary>
    public UniTask PopToRootAsync(IScreenNavigator navigator);
}
```

枚数は呼んだ時点で数える。基盤の `ScreenNavigator` は遷移を順番待ちにするので、順番待ちの遷移が残っている間に呼ぶと数がずれうる。MVP では通信中はブロッカーで入力が止まり、Presenter は遷移を `await` してから次の操作に進む（3.3.5）ので、この呼び方で足りる。旧版の `ClearAsync` は作らない（タイトルへ戻るのはシーンの読み込み直し）。

#### `ActiveSceneUi`・`SceneUiActivation`・`SceneUiInstaller`（決めたこと 3・4）

```csharp
/// <summary>いまのシーンの UiRoot と DialogService の入れ物。ルートのスコープに 1 つ（BootstrapEntry が作って RegisterInstance）。</summary>
public sealed class ActiveSceneUi
{
    /// <summary>入っていなければ null。</summary>
    public UiRoot Root { get; private set; }
    public DialogService Dialogs { get; private set; }
    /// <summary>Root が null または破棄済み（Unity の == null）なら null、そうでなければ Root.Busy。</summary>
    public BusyIndicator Busy { get; }

    /// <summary>root・dialogs のどちらかが null なら ArgumentNullException。前に入っていたものは置き換える。</summary>
    public void Attach(UiRoot root, DialogService dialogs);

    /// <summary>ReferenceEquals(Root, root) のときだけ Root・Dialogs を null にする。違えば何もしない（新しいシーンが先に Attach した場合）。</summary>
    public void Detach(UiRoot root);
}

/// <summary>各シーンのスコープのエントリーポイント。SceneUiInstaller が登録する。</summary>
public sealed class SceneUiActivation : IStartable, IDisposable
{
    public SceneUiActivation(ActiveSceneUi active, UiRoot root, DialogService dialogs);
    public void Start();     // active.Attach(root, dialogs)
    public void Dispose();   // active.Detach(root)
}

public static class SceneUiInstaller
{
    /// <summary>
    /// シーンの LifetimeScope の Configure から呼ぶ。uiRoot が null なら ArgumentNullException(nameof(uiRoot), "UiRoot is not set on the scene's LifetimeScope")。
    /// 登録（この順）:
    ///   builder.RegisterComponent(uiRoot);
    ///   builder.RegisterComponent(uiRoot.Pages);      // PageContainer
    ///   builder.RegisterComponent(uiRoot.Modals);     // ModalContainer（Overlays の ModalContainer は登録しない）
    ///   builder.RegisterComponent(uiRoot.Overlays);   // OverlayContainer
    ///   builder.Register<ScreenNavigator>(Lifetime.Singleton).As<IScreenNavigator>();   // 基盤。IDisposable なのでスコープの破棄で Dispose される
    ///   builder.Register<PageStack>(Lifetime.Singleton);
    ///   builder.RegisterInstance(new DialogService(uiRoot.DialogLayer, uiRoot.DialogPrefab));
    ///   builder.RegisterEntryPoint<SceneUiActivation>();
    /// UiRoot の Pages・Modals・Overlays のプロパティは AssetLoader（Addressables）を設定してから返す（P1）。
    /// </summary>
    public static void Install(IContainerBuilder builder, UiRoot uiRoot);
}
```

### 3.3 画面を作る共通ルール（2〜6 本目も従う）

2〜6 本目の計画は、この節を参照して書く（同じことを繰り返して書かない）。

#### 3.3.1 遷移

`UnityScreenNavigator.IScreenNavigator`（シーンのスコープにある）のうち、Uta が使うのは次だけ。**`playAnimation` は常に `false` を名前付き引数で渡す**（遷移のアニメーションを作らないため）。

| メソッド | 使う場面 |
|---|---|
| `PushPageAsync<TPresenter>(bool stack = true, bool playAnimation = true)` | 引数の要らないページを積む。`stack` は既定の true のまま（`stack: false` は「積むページ自身を、次の Push で消してよいページにする」意味。Uta は使わない） |
| `PushPageAsync<TPresenter, TArgs>(TArgs args, bool stack = true, bool playAnimation = true)` | 引数を渡してページを積む。`TPresenter : IScreenWithArgs<TArgs>`、`TArgs : class` |
| `PopPageAsync(IPresenter presenter, bool playAnimation = true)` | 自分（`this`）のページを、上に積まれたものごと閉じる。「戻る」と局面の切り替え |
| `PopPageAsync(bool playAnimation = true, int popCount = 1)` | `PageStack` だけが使う |
| `PushModalAsync<TPresenter>(bool playAnimation = true)`・`PushModalAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)` | モーダルを出す |
| `PopModalAsync(IPresenter presenter, bool playAnimation = true)` | 自分（`this`）のモーダルを閉じる |
| `PopModalAsync(bool playAnimation = true, int popCount = 1)` | `PageStack` だけが使う |
| `WaitForPopAsync<TResult>(IPresenter presenter, CancellationToken cancellation = default)` | 出したモーダル（またはページ）が閉じたときの結果を受け取る。結果は相手の Presenter の `CompleteAsync()` が返す。1 つの Presenter に 1 回だけ |

`PushOverlayAsync`・`PopOverlayAsync` は使わない（ダイアログは `DialogService`、ブロッカーは `BusyIndicator`）。`Push…Async` が返す Presenter に対しては、`WaitForPopAsync`・`Pop…Async` に渡す以外のことをしない（テストの偽は `null` を返す。値の受け渡しは Args と `WaitForPopAsync` で行う）。

遷移の型:
- **シーンの最初のページ**: シーンのエントリー（`TitleEntry`・`HomeEntry` の形。3.7）が `await navigator.PushPageAsync<XxxPresenter>(playAnimation: false);`。底のページは閉じない
- **底の上に積む**（ホームからガチャ・所持一覧・編成など）: `await navigator.PushPageAsync<XxxPresenter>(playAnimation: false);`
- **戻る**: `await navigator.PopPageAsync(this, playAnimation: false);`
- **プロデュースの局面の切り替え**（`Replace` は作らない）: 今の局面のページを閉じてから次を積む。`await navigator.PopPageAsync(current, playAnimation: false); await navigator.PushPageAsync<NextPresenter>(playAnimation: false);`（`current` は今の局面の Presenter。間に一瞬下のページが見えるのは受け入れる）
- **同じシーンの底へ戻る**（ホームの上のガチャからホームへ、など）: `await pageStack.PopToRootAsync(navigator);`（上のモーダルとページを全部閉じる。底の Presenter は `DidPopEnter` で表示をやり直す）
- **シーンをまたぐ**: `ISceneNavigator`（ルート）の `GoToHomeAsync`（`Produce` → `Home` など）・`GoToProduceAsync`（`Home` → `Produce`）。いまのシーンのページ・モーダルはシーンごと破棄される（Pop しない）。所持データが変わったあとにホームへ行くときは、先に `RefreshPlayerUseCase` を呼ぶ（3.3.5）。`GoToTitleAsync` は `ErrorNavigator` と、例外として `HomeEntry`（`Home` に入ったときサインインしていなければ `Title` へ戻す。3.7・screen-design 8章）だけが呼ぶ。Presenter は呼ばない

#### 3.3.2 Presenter の形

Presenter は `IPresenter`（引数があれば `IScreenWithArgs<TArgs>`）を実装する普通のクラス。基盤の `ScreenNavigator` が Push のたびに、**シーンのスコープの子スコープ**を作り、`Scoped` で登録して解決する。**Presenter をどのスコープにも登録しない**。子スコープには、読み込まれたページ（モーダル）の**具象型**と Args の**具象型**だけが入る。シーンのスコープ（`IScreenNavigator`・`PageStack`・`DialogService`・`UiRoot`）とルートのスコープ（ユースケース・`PlayerCache`・`IGameMaster`・`ICardCatalog`・`ISceneNavigator`）に登録したものも受けられる。

```csharp
using Cysharp.Threading.Tasks;
using Uta.Client.Application.Master;
using Uta.Client.Application.Player;
using Uta.Client.Presentation.Common;
using UnityScreenNavigator;
using VContainer;

namespace Uta.Client.Presentation.Home
{
    [AssetAddress(nameof(HomePage))]                       // Addressables のアドレス = Prefab 名 = View の型名（P2 が Screens グループに登録する）
    public sealed class HomePresenter : IPresenter
    {
        private readonly IHomeView view;
        private readonly ICharacterStage stage;
        private readonly PlayerCache cache;
        private readonly DialogService dialogs;
        private readonly IGameMaster master;
        private bool running;                              // 実行中フラグ。Presenter に 1 つ。全部のボタンの処理が共有する
        private bool disposed;

        [Inject]                                           // VContainer はこちらを使う（具象型のページを受ける）。[Inject] は IL2CPP のストリップも防ぐ
        public HomePresenter(HomePage view, PlayerCache cache, DialogService dialogs, IGameMaster master)
            : this((IHomeView)view, view.CreateStage(master), cache, dialogs, master)   // キャストが要る（無いと自分自身を呼ぶ）。ステージは View から作る（3.7）
        {
        }

        public HomePresenter(IHomeView view, ICharacterStage stage, PlayerCache cache, DialogService dialogs, IGameMaster master)   // EditMode のテストが偽の View・偽のステージで使う
        {
            this.view = view;
            this.stage = stage;
            this.cache = cache;
            this.dialogs = dialogs;
            this.master = master;
        }

        public UniTask InitializeAsync()                   // ILifecycleHandler。画面が読み込まれた直後に 1 回
        {
            view.GachaClicked += OnGachaClicked;           // 購読はここで。Dispose で必ず解く
            Refresh();
            return UniTask.CompletedTask;
        }

        public void DidPopEnter()                          // ILifecycleHandler。上のページが閉じてこのページに戻ったとき
        {
            Refresh();
        }

        public void Dispose()                              // 画面が破棄されたとき（Pop・シーンを閉じた）
        {
            disposed = true;
            view.GachaClicked -= OnGachaClicked;
        }

        private void OnGachaClicked()                      // イベントの受け口は同期。公開の UniTask メソッドを Forget で呼ぶだけ
        {
            OpenGachaAsync().Forget();
        }

        public async UniTask OpenGachaAsync()              // テストはこれを await する
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await dialogs.ShowInfoAsync("準備中です。");
                if (disposed)                              // await のあとは、View に触る前に必ず確かめる
                {
                    return;
                }
            }
            finally
            {
                running = false;                           // 例外でも必ず戻す
            }
        }

        private void Refresh() { /* cache.Current から view.Set… を呼ぶ */ }
    }
}
```

決まり:
1. **コンストラクターは 2 本**: `[Inject]` を付けた「具象型のページ（モーダル）を受ける」ものと、「`IXxxView` を受ける」もの。前者は `: this((IXxxView)view, ...)` で後者に渡す。Args・ほかの依存は両方で同じ順に受ける。例外: ページから作る部品（ホームの `ICharacterStage`）は、後者だけが `IXxxView` の直後で受け、前者はページのメソッド（`view.CreateStage(master)`）で作って渡す
2. **`[AssetAddress(nameof(XxxPage))]`** を必ず付ける。アドレスは Prefab の名前（= View の型名）。Addressables への登録は Prefab の計画（P2・P3）の生成スクリプトが行う（C# の計画は登録しない）
3. **購読と最初の表示は `InitializeAsync`** で行う（コンストラクターでは View に触らない）。`InitializeAsync` で通信してよい（`async UniTask InitializeAsync()`。終わるまで画面は出ない）。画面が出たあとに一度だけ行うこと（お知らせのダイアログなど）は `DidPushEnter()` で行う
4. **購読したイベントは `Dispose` で全部解く**。`Dispose` は `InitializeAsync` の前に呼ばれても安全に書く（未購読の `-=` は害がない）
5. **ボタンの処理**: イベントの受け口（`OnXxxClicked`）は同期で、`XxxAsync().Forget()` を呼ぶだけ。`XxxAsync` は `public async UniTask` で、先頭で `if (running) return; running = true;`、本体を `try`、`finally` で `running = false;`。連打は通信中ならブロッカー、通信の前後（ダイアログを出している間など）は実行中フラグで止める
6. **破棄の印**: `Dispose` で `disposed = true`。`await` のあとは View・ナビゲーターに触る前に `if (disposed) return;`（エラーの「タイトルへ」「ホームへ」でシーンが読み込み直されると、通信中の Presenter はスコープごと破棄される。screen-design 8章）
7. **戻ってきたときの更新**は `DidPopEnter()` で行う（ページの側にイベントを作らない）
8. **結果を返すモーダル**は `public UniTask<object> CompleteAsync()` を書き、呼ぶ側は `var presenter = await navigator.PushModalAsync<XPresenter, XArgs>(args, playAnimation: false); var result = await navigator.WaitForPopAsync<T>(presenter);`。モーダルからの「もう一度」などの呼び戻しは、Args に `Action`／`Func<UniTask>` を持たせる
9. **閉じる**: ページは `navigator.PopPageAsync(this, playAnimation: false)`、モーダルは `navigator.PopModalAsync(this, playAnimation: false)`。閉じる処理も 5 の実行中フラグの中で行い、同じ Presenter で 2 回呼ばない（閉じたあとの Presenter を渡すと、基盤が `InvalidOperationException` を投げる）

#### 3.3.3 ページ（View）の形

View は Prefab のルートに付く `PageBase`／`ModalBase` の派生で、Prefab の計画（P1・P2・P3）が「`[SerializeField] private` のフィールドだけ」の骨格を作る。C# の計画は、その骨格に `IXxxView` の実装を足す。フィールド名・型・子の名前の対応は Prefab の計画の表が正。

```csharp
public interface IHomeView
{
    void SetProfile(int level, int gachaCurrency);
    event Action GachaClicked;
}

public sealed class HomePage : PageBase, IHomeView
{
    [SerializeField] private TMP_Text profileText;        // P2 の骨格のまま
    [SerializeField] private ButtonView navGachaButton;   // P2 の骨格のまま

    public event Action GachaClicked;                     // ここから下をこの計画（C#）が足す

    private void Awake()
    {
        navGachaButton.Button.onClick.AddListener(() => GachaClicked?.Invoke());
    }

    public void SetProfile(int level, int gachaCurrency)
    {
        profileText.text = ...;
    }
}
```

- **ボタンの `onClick` は View の `Awake` で購読し、C# の `event` を起こす**（Prefab に永続リスナーを張らない）。`Awake` は USN が Prefab を `Instantiate` したときに走る（Presenter の `InitializeAsync` より前）。View と Prefab の寿命が同じなので、`onClick` の解除は書かない
- `Set…` は `[SerializeField]` の参照に値を入れるだけ（旧版の `EnsureBuilt` は要らない）
- 動的に増減する行は、行の Prefab（`[SerializeField]` の `rowPrefab`）から `UiObjects.AddRow` で作り、消すときは `UiObjects.ClearChildren(listContent)`。行の中の部品は行の View（`ListRowBase` の派生）のメソッドで入れる
- View はロジックを持たない。文言の組み立て（数値の書式など）は Presenter でしてもページでしてもよいが、計画にどちらでするかを書く
- `IXxxView` に `event Action`（引数があれば `Action<int>` など）と `void SetXxx(...)` だけを置く。戻り値のあるメソッドを置かない
- 文字の大きさ・色・フォントは Prefab（TMP）が決める。C# は文字・表示/非表示・押せる/押せないだけを変える

#### 3.3.4 Args

```csharp
public sealed class XxxArgs
{
    public XxxArgs(int gachaId) { GachaId = gachaId; }
    public int GachaId { get; }
}
```
`sealed class`、コンストラクターで全部受け、get だけのプロパティ。Presenter は `IScreenWithArgs<XxxArgs>` を実装し、両方のコンストラクターで `XxxArgs args` を受ける。

#### 3.3.5 通信とエラー

- 通信中のブロッカーは `ApiActivityBridge` がいまのシーンの `BusyIndicator` に付けるので、**Presenter は通信を個別に包まない**
- `ApiCore` の共通ハンドラーが出すダイアログ（ネットワークの失敗、`DATE_CHANGED`、**`UtaErrors` に `Stay` で登録した業務エラーのうち `handledCodes` に入れなかったもの**）は、Presenter から追加で出さない
- 業務エラーを画面ごとの文言で出したいときは、そのコードをゲートウェイの `handledCodes` に入れ、ユースケースの結果の型（成功／業務エラー／その他）で Presenter に返し、Presenter が `DialogService` で出す。どのコードを `handledCodes` に入れるかは各計画が書く
- **RPC が成功したあとの `RefreshPlayerUseCase` の失敗は、成功として扱う**: ユースケースが例外を握りつぶして「取り直しに失敗した」印を付けて成功を返し、画面は RPC の応答で更新できる範囲を更新する。ダイアログは `ApiCore` が出した分だけ（Presenter は追加で出さない）
- 遷移（`Push…`／`Pop…`／`ISceneNavigator`）は `await` してから次に進む
- 「タイトルへ」「ホームへ」は `ErrorNavigator`（ルート）が `ISceneNavigator` でシーンを読み込み直す。画面は自分で遷移しない

#### 3.3.6 文字と書式

- 数値の 3 桁区切りは `value.ToString("N0", CultureInfo.InvariantCulture)`
- C# は文字の大きさ・色・フォントを変えない（Prefab の TMP の値。P1 の `TextSpec`）
- `TMP_Text` は `richText = false`（P1）なので、`<` を含む文字もそのまま出る

#### 3.3.7 テストの書き方

- Presenter のテストは EditMode で、偽の View（各計画がテストのファイルの中に `private sealed class FakeXxxView : IXxxView` を書く。呼ばれた `Set…` を `CallLog` に文字列で記録し、`RaiseXxx()` でイベントを起こす）、`FakeScreenNavigator`、`FakeDialogService`、`FakeSceneNavigator` を、**同じ `CallLog`** で作り、`log.Entries` の並びで確かめる
- 非同期のテストは `[UnityTest] public IEnumerator Xxx() => UniTask.ToCoroutine(async () => { ... });` にし、Presenter の公開の `UniTask` メソッド（`XxxAsync`、`InitializeAsync`）を `await` する。イベントを起こすテスト（購読の確認）は、偽が同期で終わるので、起こした直後に確かめてよい
- 同期のテストは `[Test]`
- 偽の View の記録の書式は `"<メソッド名>:<引数を , でつないだもの>"`（例 `"SetProfile:12,3250"`、bool は `True`/`False`）
- Prefab の View（`HomePage` など）の EditMode のテストは書かない（参照の欠けは P1 の `PrefabContractTests` が見る。表示は Play で見る）
- TMP の文字を EditMode・Play で入れると、動的フォントのアセットに字が足されて `.asset` が変わることがある。`BuildAll()` は最後に `FontAssets.ClearDynamicData()` を呼ぶ（P1）が、テスト・Play のあとは、コミットの前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'` を実行する。それでも `Assets/Fonts/NotoSansJP/` の `.asset` に差分が残ったら、コミットせず `git restore` で戻す（P1 の決めたこと 4）

#### 3.3.8 テスト用の偽（`Tests/EditMode/Fakes/`）

```csharp
public sealed class CallLog
{
    public List<string> Entries { get; } = new List<string>();
    public void Add(string entry) { Entries.Add(entry); }
}

public sealed class FakeScreenNavigator : IScreenNavigator
{
    public FakeScreenNavigator(CallLog log);
    /// <summary>Push に渡された Args（渡された順）。</summary>
    public List<object> Args { get; }
    /// <summary>WaitForPopAsync が返す値。空なら default(TResult)。</summary>
    public Queue<object> PopResults { get; }
}
```

`FakeScreenNavigator` の記録（`log.Add`）と戻り値。どれも同期で終わる `UniTask` を返す:

| メソッド | 記録 | 戻り値 |
|---|---|---|
| `PushPageAsync<TPresenter>(stack, playAnimation)` | `"PushPage:" + typeof(TPresenter).Name`、`stack` が false なら末尾に `"(nostack)"` | `default(TPresenter)` |
| `PushPageAsync<TPresenter, TArgs>(args, stack, playAnimation)` | 同上。`Args.Add(args)` | `default(TPresenter)` |
| `PopPageAsync(bool playAnimation, int popCount)` | `"PopPage:" + popCount` | — |
| `PopPageAsync(IPresenter presenter, bool playAnimation)` | `"PopPage:" + presenter.GetType().Name`（presenter が null なら `"PopPage:null"`） | — |
| `PushModalAsync<TPresenter>(playAnimation)`・`<TPresenter, TArgs>(args, playAnimation)` | `"PushModal:" + typeof(TPresenter).Name`（Args 付きは `Args.Add(args)`） | `default(TPresenter)` |
| `PopModalAsync(bool, int popCount)` | `"PopModal:" + popCount` | — |
| `PopModalAsync(IPresenter presenter, bool)` | `"PopModal:" + presenter.GetType().Name`（null なら `"PopModal:null"`） | — |
| `PushOverlayAsync…`・`PopOverlayAsync…` | `"PushOverlay:" + 型名`・`"PopOverlay:" + popCount か型名` | `default` |
| `WaitForPopAsync<TResult>(presenter, cancellation)` | `"WaitForPop"` | `PopResults` が空でなければ `(TResult)PopResults.Dequeue()`、空なら `default(TResult)` |

```csharp
public sealed class FakeDialogService : DialogService
{
    public FakeDialogService(CallLog log) : base(null, null) { ... }
    /// <summary>ConfirmAsync が返す値。空なら DialogResult.Positive。</summary>
    public Queue<DialogResult> ConfirmResults { get; }
    /// <summary>ChooseAsync が返す値。空なら 0。</summary>
    public Queue<int> ChooseResults { get; }
    /// <summary>null でなければ、ShowInfoAsync はこの Task を待ってから終わる（実行中フラグのテスト用）。既定は null。</summary>
    public UniTaskCompletionSource InfoGate { get; set; }

    public override UniTask ShowInfoAsync(string message);                // 記録 "Info:" + message
    public override UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ");   // 記録 "Confirm:" + message
    public override UniTask<int> ChooseAsync(string message, string[] labels);   // 記録 "Choose:" + message + ":" + string.Join("/", labels)
}

/// <summary>Uta.Client.Application.Navigation.ISceneNavigator の偽。どれも記録して同期で終わる。</summary>
public sealed class FakeSceneNavigator : ISceneNavigator
{
    public FakeSceneNavigator(CallLog log);
    // GoToTitleAsync → "GoToTitle"、GoToHomeAsync → "GoToHome"、GoToProduceAsync → "GoToProduce"
}

/// <summary>IDeviceAuthPort の偽。</summary>
public sealed class StubDeviceAuthPort : IDeviceAuthPort
{
    /// <summary>GetAccountStateAsync が返す値。既定 AccountState.Registered。</summary>
    public AccountState State { get; set; }
    /// <summary>EnsureSignedInAsync の 1 回ごとの振る舞い。空なら完了済みの UniTask。Dequeue した Func を呼んでその UniTask を返す（例外を投げる・未完了の源を返す）。</summary>
    public Queue<Func<UniTask>> EnsureSignedIn { get; }
    // ReissueTokenAsync は完了済み。InvalidateServerSideTokenAsync は TokenInvalidationCount を 1 増やして完了済み（ConnectivityCheck の再認証の確認が通るように）。
    // CanReceiveServerErrorAsync は true。TokenInvalidationCount の初期値は 0
}

/// <summary>IPlayerPort の偽。</summary>
public sealed class StubPlayerPort : IPlayerPort
{
    /// <summary>SignInAsync が返す値。空なら PlayerSignInResult.Success(Snapshot)。</summary>
    public Queue<PlayerSignInResult> SignInResults { get; }
    /// <summary>SignUpAsync が返す値。空なら PlayerSignUpResult.Success(true)。</summary>
    public Queue<PlayerSignUpResult> SignUpResults { get; }
    /// <summary>既定の成功で返すスナップショット。既定は Lv 1・Exp 0・Gold 0・SupportPoints 0・GachaCurrency 0・進行中なし・破棄なし・空のリスト 2 つ。</summary>
    public PlayerSnapshot Snapshot { get; set; }
}
```

```csharp
/// <summary>育成アイドルだけを返す IGameMaster の偽（タスク 3。HomePresenterTests・PlaceholderCharacterStageTests が使う）。2 本目（gacha）の FakeGameMaster とは別物で、gacha 以降もこのまま残す。</summary>
public sealed class StubIdolMaster : IGameMaster
{
    public StubIdolMaster(params IdolInfo[] idols);   // Idols は渡された順のまま（IdolId の昇順で渡す）
    /// <summary>名前とレア度だけを決めた IdolInfo。ほかは固定: plan Plan.Sense、maxHp 30、initial 0・0・0、ParamKind.Singing・Rhythm・Expression。</summary>
    public static IdolInfo Idol(int idolId, string name, Rarity rarity);
    // Idols: 渡された配列。GetIdol(idolId): IdolId が一致するもの。無ければ KeyNotFoundException("idol " + idolId)
    // ほかのメンバー（SupportCards・GetSupportCard・GetSupportAbilities・Gachas・GetGacha・Scenarios・GetScenario・GetSupportMaxLevel・
    //   GetSupportMaxLimitBreak・GetSupportLevelUpCost・GetCardText）は NotSupportedException("StubIdolMaster")
}
```

既存の `EnterPlayerUseCaseTests` の `FakePlayerPort`（入れ子の private）は変えない。

#### 3.3.9 DI の置き場

| スコープ | 登録するもの | 誰が足すか |
|---|---|---|
| ルート（`BootstrapEntry`。`DontDestroyOnLoad`） | マスタ（`ICardCatalog`・`IGameMaster`）、`ActiveSceneUi`、`StartupOptions`、`ISceneNavigator`・`IErrorNavigator`、`UtaApiClient`、ゲートウェイ、`PlayerCache`、**すべてのユースケース** | 2〜6 本目は `BootstrapEntry.RegisterUseCases` に 1 行ずつ足す（3.8） |
| シーン（`TitleLifetimeScope` など） | `SceneUiInstaller.Install(builder, uiRoot)`、シーンの入口（`XxxEntry`）、そのシーンだけの補助（`ConnectivityCheck` など） | 新しいシーンの入口を作る計画 |
| Push ごとの子スコープ（基盤が作る） | ページ・Args の具象型と Presenter | 登録しない |

#### 3.3.10 型の名前の衝突

- 基盤に `UnityScreenNavigator.ISceneNavigator`・`UnityScreenNavigator.SceneNavigator`（シーンの切り替え。Uta は使わない）がある。**`using UnityScreenNavigator;` と `using Uta.Client.Application.Navigation;` の両方を書くファイルは、`using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;` を足す**（無いと CS0104）。`Uta.Client.Bootstrap.SceneNavigator` は、`Uta.Client.Bootstrap` の名前空間の中では自分の型が先に見つかるので、別名は要らない
- `Application` は `Uta.Client` の中では `Uta.Client.Application` に解決されるので、Unity の `Application` は `UnityEngine.Application` と書く（#24 のまま）

### 3.4 マスタの読み取り（`Uta.Client.Application.Master`）

```csharp
public enum MasterItemKind { Nothing = 0, Idol = 1, SupportCard = 2 }   // Contracts.Gacha.ItemKind・マスタの ItemKind と同じ数値

public sealed class IdolInfo
{
    public IdolInfo(int idolId, string name, Rarity rarity, Plan plan, int maxHp,
        int initialSinging, int initialRhythm, int initialExpression,
        ParamKind firstParam, ParamKind secondParam, ParamKind thirdParam);
    public int IdolId { get; } public string Name { get; } public Rarity Rarity { get; } public Plan Plan { get; } public int MaxHp { get; }
    public int InitialSinging { get; } public int InitialRhythm { get; } public int InitialExpression { get; }
    public ParamKind FirstParam { get; } public ParamKind SecondParam { get; } public ParamKind ThirdParam { get; }
}
public sealed class SupportCardInfo
{
    public SupportCardInfo(int supportCardId, string name, Rarity rarity, SupportType type, Plan plan);
    public int SupportCardId { get; } public string Name { get; } public Rarity Rarity { get; } public SupportType Type { get; } public Plan Plan { get; }
}
public sealed class SupportAbilityInfo
{
    public SupportAbilityInfo(int order, SupportAbilityKind kind, ParamKind param, int valueAtLv1, int valueAtMax);
    public int Order { get; } public SupportAbilityKind Kind { get; } public ParamKind Param { get; }   // Kind が MaxHp のとき Param は ParamKind.Nothing
    public int ValueAtLv1 { get; } public int ValueAtMax { get; }
}
public sealed class GachaEntryInfo
{
    public GachaEntryInfo(MasterItemKind itemKind, int itemId, int weight);
    public MasterItemKind ItemKind { get; } public int ItemId { get; } public int Weight { get; }   // ItemId は育成アイドルなら IdolId、サポートカードなら SupportCardId
}
public sealed class GachaInfo
{
    public GachaInfo(int gachaId, int singleCost, int tenCost, int ceilingPoints,
        IReadOnlyList<GachaEntryInfo> normalEntries, IReadOnlyList<GachaEntryInfo> guaranteedEntries);
    public int GachaId { get; } public int SingleCost { get; } public int TenCost { get; } public int CeilingPoints { get; }
    public IReadOnlyList<GachaEntryInfo> NormalEntries { get; } public IReadOnlyList<GachaEntryInfo> GuaranteedEntries { get; }   // どちらも GachaTableEntry.Order の昇順
}
public sealed class ScenarioWeekInfo
{
    public ScenarioWeekInfo(int week, WeekKind kind, int clearTarget, int perfectTarget);
    public int Week { get; } public WeekKind Kind { get; } public int ClearTarget { get; } public int PerfectTarget { get; }
}
public sealed class ScenarioInfo
{
    public ScenarioInfo(int scenarioId, string name, Difficulty difficulty, int weekCount, int paramCap, int retryCount, IReadOnlyList<ScenarioWeekInfo> weeks);
    public int ScenarioId { get; } public string Name { get; } public Difficulty Difficulty { get; } public int WeekCount { get; } public int ParamCap { get; } public int RetryCount { get; }
    public IReadOnlyList<ScenarioWeekInfo> Weeks { get; }      // Week の昇順
}
public readonly struct SupportLevelUpCost
{
    public SupportLevelUpCost(int supportPoints, int gold);
    public int SupportPoints { get; } public int Gold { get; }
}
public sealed class CardTextInfo
{
    public CardTextInfo(int cardId, string name, string description, string descriptionEnhanced);
    public int CardId { get; }
    public string Name { get; }                  // Card.Name
    public string Description { get; }           // CardFace（Face = Normal）の Description
    public string DescriptionEnhanced { get; }   // CardFace（Face = Enhanced）の Description。Enhanced の行が無ければ Description と同じ
}
public interface IGameMaster
{
    IReadOnlyList<IdolInfo> Idols { get; }                 // IdolId の昇順
    IdolInfo GetIdol(int idolId);                          // 無ければ KeyNotFoundException
    IReadOnlyList<SupportCardInfo> SupportCards { get; }   // SupportCardId の昇順
    SupportCardInfo GetSupportCard(int supportCardId);     // 無ければ KeyNotFoundException
    /// <summary>SupportAbility の行を Order の昇順で。サポートカードが無ければ KeyNotFoundException、行が 0 件なら空のリスト。
    /// 表示する値は ProduceFormulas.AbilityValue(ValueAtLv1, ValueAtMax, level, maxLevel)（Uta.Shared.Domain.Produce）。maxLevel は GetSupportMaxLevel(rarity, GetSupportMaxLimitBreak(rarity))（完凸の最大 Lv）。</summary>
    IReadOnlyList<SupportAbilityInfo> GetSupportAbilities(int supportCardId);
    IReadOnlyList<GachaInfo> Gachas { get; }               // GachaId の昇順
    GachaInfo GetGacha(int gachaId);                       // 無ければ KeyNotFoundException
    IReadOnlyList<ScenarioInfo> Scenarios { get; }         // ScenarioId の昇順
    ScenarioInfo GetScenario(int scenarioId);              // 無ければ KeyNotFoundException
    int GetSupportMaxLevel(Rarity rarity, int limitBreak);   // SupportLevelCap。無ければ KeyNotFoundException
    int GetSupportMaxLimitBreak(Rarity rarity);              // SupportLevelCap のその Rarity の LimitBreak の最大値。行が無ければ KeyNotFoundException
    /// <summary>fromLevel <= level < toLevel の SupportLevelCost（Rarity, level）の合計。サーバーの MasterSupportCatalog.GetLevelUpCost と同じ。fromLevel >= toLevel なら (0, 0)。間の行が無ければ InvalidOperationException。</summary>
    SupportLevelUpCost GetSupportLevelUpCost(Rarity rarity, int fromLevel, int toLevel);
    /// <summary>スキルカードの名前と説明文。カードが無ければ KeyNotFoundException。</summary>
    CardTextInfo GetCardText(int cardId);
}
```

`Rarity`・`Plan`・`ParamKind` は `Uta.Shared.Domain.Lesson`、`SupportType`・`SupportAbilityKind`・`Difficulty`・`WeekKind` は `Uta.Shared.Domain.Produce` の enum（マスタの enum と数値が同じ。`MasterCardCatalog` のとおり `(Domain の型)(int)マスタの値` で変換する）。スキルカードの効果・コスト・種類は、これまでどおり `ICardCatalog`（`Uta.Shared.Domain.Lesson.CardCatalog`）から読む。

### 3.5 `Uta.Client.Application.Player`

```csharp
public sealed class RefreshPlayerUseCase
{
    public RefreshPlayerUseCase(IPlayerPort port, PlayerCache cache);
    /// <summary>
    /// port.SignInAsync を呼び、Outcome が Succeeded なら cache.Set(snapshot) して snapshot を返す。
    /// Failed は InvalidOperationException("sign-in failed: " + ErrorText)、PlayerNotCreated は InvalidOperationException("sign-in failed: player not created")。どちらもキャッシュは変えない。
    /// </summary>
    public UniTask<PlayerSnapshot> ExecuteAsync();
}

public sealed class PlayerCache   // 既存に足す
{
    /// <summary>Current の DiscardedProduce をホームで知らせたか。Set で false に戻る。</summary>
    public bool DiscardNoticeShown { get; private set; }
    /// <summary>DiscardNoticeShown = true。</summary>
    public void MarkDiscardNoticeShown();
    // Set(snapshot): 今までの処理のあと DiscardNoticeShown = false
}
```

### 3.6 `Uta.Client.Infrastructure.MasterGameData`

```csharp
public sealed class MasterGameData : IGameMaster
{
    public MasterGameData(MemoryDatabase db);   // db が null なら ArgumentNullException。読み取り用の型は構築時に全部作る
}
```

- `using` は既存の `MasterCardCatalog.cs` と同じ書き方にする: `using Uta.MasterData;`（マスタの型は修飾なしで書く: `MemoryDatabase`・`CardFaceKind` など）、`using DomainLesson = Uta.Shared.Domain.Lesson;`、`using DomainProduce = Uta.Shared.Domain.Produce;`、`using Uta.Client.Application.Master;`。`Uta.Shared.Domain.Lesson`・`Uta.Shared.Domain.Produce` を別名なしで `using` しない（マスタと同名の enum `Rarity`・`Plan`・`ParamKind`・`SupportType`・`SupportAbilityKind`・`Difficulty`・`WeekKind` などが CS0104 になる）。Domain の enum は `DomainLesson.Rarity`・`DomainLesson.Plan`・`DomainLesson.ParamKind`・`DomainProduce.SupportType`・`DomainProduce.SupportAbilityKind`・`DomainProduce.Difficulty`・`DomainProduce.WeekKind` と書く。`MasterGameDataTests.cs` も同じ `using` にする
- 読み方はサーバーの `MasterGachaCatalog`・`MasterSupportCatalog`（`server/Uta.Server/Infrastructure/MasterData/`）と同じ。使うテーブル: `db.IdolTable.All`・`db.SupportCardTable.All`・`db.SupportAbilityTable.All`・`db.GachaTable.All`・`db.GachaTableEntryTable.All`（`GachaTableId` で絞り、`Order` の昇順）・`db.ScenarioTable.All`・`db.ScenarioWeekTable.All`・`db.SupportLevelCostTable.All`・`db.SupportLevelCapTable.All`・`db.CardTable.All`・`db.CardFaceTable.All`
- ガチャの通常枠は `Gacha.NormalTableId`、確定枠は `Gacha.GuaranteedTableId` のエントリー。`ItemKind`（マスタ）→ `MasterItemKind` は `(MasterItemKind)(int)`
- `GetSupportAbilities`: `SupportAbilityTable` を `SupportCardId` で絞り `Order` の昇順。`Kind` は `(DomainProduce.SupportAbilityKind)(int)`、`Param` は `(DomainLesson.ParamKind)(int)`
- `GetCardText`: `CardTable` の `Name`、`CardFaceTable` の `Face == CardFaceKind.Normal` の行の `Description`、`Face == CardFaceKind.Enhanced` の行の `Description`（無ければ Normal と同じ）。Normal の行が無いカードがあれば、構築時に `InvalidOperationException("card face missing: " + cardId)`

### 3.7 タイトル・ホーム・プロデュースのシーン（`Uta.Client.Presentation.*`）

#### タイトル（`Uta.Client.Presentation.Title`）

```csharp
public interface ITitleView
{
    void SetStatus(string text);
    void SetStartInteractable(bool interactable);
    event Action StartClicked;
}

public sealed class TitlePage : PageBase, ITitleView   // フィールドは P2 の骨格（3.9）
{
    public event Action StartClicked;
    // Awake: startButton.Button.onClick.AddListener(() => StartClicked?.Invoke())
    // SetStatus: statusText.text = text
    // SetStartInteractable: startButton.SetInteractable(interactable)
}

/// <summary>ログの行の文（TitlePresenter と TitleDiagnostics が同じ文を出す。#24 の TitlePresenter と同じ文字列）。</summary>
public static class StartupLogs
{
    public static string Device(bool registered);        // registered ? "device: registered" : "device: token issued"
    public static string SignUp(EnterPlayerResult entered);   // "sign-up: " + (!entered.SignedUp ? "skipped" : entered.Created ? "created" : "already exists")
    public static string SignIn(PlayerSnapshot s);       // "sign-in: ok level=" + s.Level + " exp=" + s.Exp + " gold=" + s.Gold + " supportPoints=" + s.SupportPoints + " gachaCurrency=" + s.GachaCurrency + " idols=" + s.Idols.Count + " supportCards=" + s.SupportCards.Count + " hasActiveProduce=" + s.HasActiveProduce + " discardedProduce=" + s.DiscardedProduce
    public static string Failed(Exception e);            // "start: FAILED " + e.GetType().FullName + ": " + e.Message
}

[AssetAddress(nameof(TitlePage))]
public sealed class TitlePresenter : IPresenter
{
    [Inject] public TitlePresenter(TitlePage view, SignInUseCase signIn, EnterPlayerUseCase enterPlayer, ISceneNavigator scenes);   // : this((ITitleView)view, ...)
    public TitlePresenter(ITitleView view, SignInUseCase signIn, EnterPlayerUseCase enterPlayer, ISceneNavigator scenes);
    /// <summary>StartClicked を購読し、view.SetStatus("タップしてスタート")、view.SetStartInteractable(true)。</summary>
    public UniTask InitializeAsync();
    /// <summary>disposed = true、購読を解く。</summary>
    public void Dispose();
    /// <summary>StartClicked → StartAsync().Forget()。テストはこれを await する。</summary>
    public UniTask StartAsync();
}
```

`ISceneNavigator` は `Uta.Client.Application.Navigation` のもの（3.3.10 の別名を書く）。

`TitlePresenter.StartAsync()` の手順（実行中フラグ・破棄の印は 3.3.2 の 5・6）:
1. `if (running) return; running = true;` 以下を `try`、`finally` で `running = false;`
2. `view.SetStartInteractable(false); view.SetStatus("接続中…");` `Debug.Log("[Uta] startup: start");`
3. `try` の中で: `var registered = await signIn.ExecuteAsync();` → `if (disposed) return;` → `Debug.Log("[Uta] " + StartupLogs.Device(registered));` → `var entered = await enterPlayer.ExecuteAsync(registered);` → `if (disposed) return;` → `Debug.Log("[Uta] " + StartupLogs.SignUp(entered)); Debug.Log("[Uta] " + StartupLogs.SignIn(entered.Snapshot));` → `await scenes.GoToHomeAsync();` → `return`（成功ではボタンを戻さない。シーンが変わる）
4. `catch (Exception e)`: `if (disposed) return;` `Debug.Log("[Uta] " + StartupLogs.Failed(e));` `view.SetStatus("接続に失敗しました: " + e.Message + "\nタップしてもう一度");` `view.SetStartInteractable(true);`

端末の認証の失敗（サーバーが止まっているなど。`ApiCore` を通らない）は 4 に来る。`ApiCore` を通る失敗（サインアップ・サインイン）は、`ApiCore` のダイアログのあと（「再試行」以外なら）`EnterPlayerUseCase` が例外を投げて 4 に来るか、「タイトルへ」で `Title` が読み込み直されて `disposed` で止まる。

```csharp
/// <summary>診断（-uta-quit-after-check）。#24 の TitlePresenter の診断の手順をそのまま移す。画面に出さない。</summary>
public sealed class TitleDiagnostics
{
    /// <summary>VContainer が使う。: this(signIn, enterPlayer, connectivityCheck, code => UnityEngine.Application.Quit(code))</summary>
    [Inject]
    public TitleDiagnostics(SignInUseCase signIn, EnterPlayerUseCase enterPlayer, ConnectivityCheck connectivityCheck);
    /// <summary>テスト用。quit は終了の代わり（終了コードを受ける）。quit が null なら ArgumentNullException。</summary>
    public TitleDiagnostics(SignInUseCase signIn, EnterPlayerUseCase enterPlayer, ConnectivityCheck connectivityCheck, Action<int> quit);
    /// <summary>
    /// "startup: start" → signIn → StartupLogs.Device → enterPlayer → StartupLogs.SignUp・SignIn → succeeded = await connectivityCheck.RunAsync(Log)。
    /// 例外は StartupLogs.Failed を Log して succeeded = false。最後に quit(succeeded ? 0 : 1)（UnityEngine.Application.Quit を直接呼ばない）。Log は Debug.Log("[Uta] " + line)。
    /// </summary>
    public UniTask RunAsync();
}

/// <summary>Title シーンの入口。</summary>
public sealed class TitleEntry : IStartable
{
    public TitleEntry(IScreenNavigator navigator, StartupOptions options, TitleDiagnostics diagnostics);
    public void Start();          // StartAsync().Forget()
    /// <summary>options.QuitAfterCheck なら await diagnostics.RunAsync()（画面を積まない）。そうでなければ await navigator.PushPageAsync&lt;TitlePresenter&gt;(playAnimation: false)。</summary>
    public UniTask StartAsync();
}
```

```csharp
// Uta.Client.Presentation（#24 と同じ名前空間）
public sealed class TitleLifetimeScope : LifetimeScope
{
    [SerializeField] private UiRoot uiRoot;   // SceneBuilder が入れる
    protected override void Configure(IContainerBuilder builder)
    {
        SceneUiInstaller.Install(builder, uiRoot);
        builder.Register<ConnectivityCheck>(Lifetime.Singleton);
        builder.Register<TitleDiagnostics>(Lifetime.Singleton);
        builder.RegisterEntryPoint<TitleEntry>();
    }
}
```

コメントだけ直す（タスク 3。コードは変えない）:
- `Presentation/Title/ConnectivityCheck.cs` の `<summary>` の 2 行目「診断用（-uta-quit-after-check）。画面の計画で消す。」を「診断用（-uta-quit-after-check）。`TitleDiagnostics` が使う。」に
- `Application/Auth/SignInUseCase.cs` の 42 行「// 以下の 2 つは診断用。画面の計画で消す。」を「// 以下の 2 つは診断用（ConnectivityCheck。-uta-quit-after-check）。」に

#### ホーム（`Uta.Client.Presentation.Home`）

構成は screen-design 7.8・P2 の決めたこと 15（決めたこと 17）。ファイルは 1 型 1 ファイル（`IHomeView.cs`・`ICharacterStage.cs`・`PlaceholderCharacterStage.cs`・`HomePage.cs`・`HomePresenter.cs`・`HomeEntry.cs`）。

```csharp
public interface IHomeView
{
    void SetProfile(int level, int gachaCurrency);
    void SetResumeVisible(bool visible);
    event Action ProduceClicked;   // 主ボタンとナビの「プロデュース」の両方
    event Action GachaClicked;
    event Action OwnedClicked;
}

/// <summary>ホームのキャラ表示のステージ。MVP は PlaceholderCharacterStage、3D は MVP 外の ModelCharacterStage（screen-design 7.8）。</summary>
public interface ICharacterStage
{
    /// <summary>育成アイドル idolId を出す。</summary>
    void ShowIdol(int idolId);
    /// <summary>何も出さない（所持が 0 のとき）。</summary>
    void Clear();
}

/// <summary>MVP の仮の枠。枠の文字（P2 の Stage/StageLabel）に育成アイドルの名前を出すだけ。RawImage には触らない。</summary>
public sealed class PlaceholderCharacterStage : ICharacterStage
{
    public const string EmptyText = "キャラ表示";   // P2 の StageLabel の仮の文字と同じ
    /// <summary>label・master が null なら ArgumentNullException（引数名 "label"・"master"）。</summary>
    public PlaceholderCharacterStage(TMP_Text label, IGameMaster master);
    public void ShowIdol(int idolId);   // label.text = master.GetIdol(idolId).Name（無ければ GetIdol の KeyNotFoundException がそのまま出る）
    public void Clear();                // label.text = EmptyText
}

public sealed class HomePage : PageBase, IHomeView   // フィールドは P2 の骨格（3.9）
{
    // Awake: primaryButton と navProduceButton の Button.onClick で ProduceClicked、navGachaButton で GachaClicked、navOwnedButton で OwnedClicked を起こす
    // SetProfile: profileText.text = "プレイヤーLv " + level + "　ガチャ通貨 " + gachaCurrency.ToString("N0", CultureInfo.InvariantCulture)（間は全角の空白 1 つ。書式はページで組む）
    // SetResumeVisible: resumePanel.SetActive(visible); primaryButton.SetLabel(visible ? "再開" : "プロデュース");
    /// <summary>キャラ表示のステージを作る（IHomeView には置かない。HomePresenter の [Inject] のコンストラクターだけが呼ぶ）。
    /// MVP は new PlaceholderCharacterStage(stageLabel, master)。3D に替えるときは、ここだけを ModelCharacterStage（stageImage に RenderTexture を入れる）に差し替える。</summary>
    public ICharacterStage CreateStage(IGameMaster master);
}

[AssetAddress(nameof(HomePage))]
public sealed class HomePresenter : IPresenter
{
    [Inject] public HomePresenter(HomePage view, PlayerCache cache, DialogService dialogs, IGameMaster master);   // : this((IHomeView)view, view.CreateStage(master), cache, dialogs, master)
    public HomePresenter(IHomeView view, ICharacterStage stage, PlayerCache cache, DialogService dialogs, IGameMaster master);
    public UniTask InitializeAsync();   // 3 つのイベントを購読し、Refresh()
    public void DidPushEnter();         // ShowDiscardNoticeIfNeededAsync().Forget()
    public void DidPopEnter();          // Refresh()
    public void Dispose();              // disposed = true、3 つの購読を解く
    /// <summary>cache.Current が null なら何もしない。Current.DiscardedProduce が true で cache.DiscardNoticeShown が false なら、
    /// cache.MarkDiscardNoticeShown() してから await dialogs.ShowInfoAsync("バージョンが変わったため、進行中のプロデュースは破棄されました。")。</summary>
    public UniTask ShowDiscardNoticeIfNeededAsync();
    public UniTask OpenProduceAsync();  // この計画: 実行中フラグ（3.3.2 の 5）の中で dialogs.ShowInfoAsync("準備中です。")。4 本目が書き換える
    public UniTask OpenGachaAsync();    // 同上。2 本目が書き換える
    public UniTask OpenOwnedAsync();    // 同上。3 本目が書き換える
    // Refresh(): var s = cache.Current; s が null なら何もしない。view.SetProfile(s.Level, s.GachaCurrency); view.SetResumeVisible(s.HasActiveProduce); RefreshStage(s);（この順）
    // private void RefreshStage(PlayerSnapshot s): 下の選び方で選んだ育成アイドルがあれば stage.ShowIdol(idolId)、所持が 0 なら stage.Clear()
    //   選び方: s.Idols を前から見て、master.GetIdol(owned.IdolId).Rarity を (int) にした値が今の最良より大きい、
    //   または同じで owned.IdolId が今の最良より小さいなら、それを最良にする（最初の 1 つは無条件に最良）。
    //   GetIdol の KeyNotFoundException は捕まえない（所持の IdolId はサーバーと同じマスタにある）
    // ProduceClicked → OpenProduceAsync().Forget()、GachaClicked → OpenGachaAsync().Forget()、OwnedClicked → OpenOwnedAsync().Forget()
}

/// <summary>Home シーンの入口。</summary>
public sealed class HomeEntry : IStartable
{
    public HomeEntry(IScreenNavigator navigator, PlayerCache cache, ISceneNavigator scenes);
    public void Start();          // StartAsync().Forget()
    /// <summary>cache.Current が null（サインインしていない）なら await scenes.GoToTitleAsync()（screen-design 8章）。そうでなければ await navigator.PushPageAsync&lt;HomePresenter&gt;(playAnimation: false)。</summary>
    public UniTask StartAsync();
}
```

- `PlaceholderCharacterStage`・`HomePage.CreateStage` は `using Uta.Client.Application.Master;`（`IGameMaster`）、`PlaceholderCharacterStage` は `using TMPro;` を書く。`HomePresenter` は `using Uta.Client.Application.Master;` を足す
- `IGameMaster` はルートのスコープにある（3.8 の `RegisterInstance<IGameMaster>`）ので、`[Inject]` のコンストラクターで受けられる。ステージは DI に登録しない（ページごとに `HomePage.CreateStage` が作る）
- ステージの選び直しは `Refresh()` の中（最初の表示と、上のページ（ガチャ・所持一覧）から戻ったとき）。ガチャで新しい育成アイドルを引いて戻ると、取り直したキャッシュで選び直す
- 2〜4 本目は、`HomePresenter` のコンストラクター（2 本）の最後に依存を足し、対応するメソッドの中身を書き換える。`IScreenNavigator` を足すのは 2 本目（ガチャ）。3・4 本目は足された前提で、自分のユースケースだけを足す（計画は並行しない。[索引](2026-11-07-client-screens-index.md) 1章）
- 「プロデュース中」の帯は、`PlayerSnapshot` に週の情報が無いので週を出さない（[Uta案]。文字は P2 の Prefab の固定の「プロデュース中」）
- `HomeLifetimeScope`（`Uta.Client.Presentation`）: `[SerializeField] private UiRoot uiRoot;`、`Configure` は `SceneUiInstaller.Install(builder, uiRoot); builder.RegisterEntryPoint<HomeEntry>();`

#### プロデュース

- **変えない**（決めたこと 12）。`ProduceLifetimeScope`・`ProduceView`・`ProducePresenter`（#24 の仮の画面）と `Produce.unity` の形はそのまま。ホームの「プロデュース」（主ボタン・ナビ）は「準備中です。」なので、この計画の時点では `Produce` シーンへ移らない。4 本目（formation）が `Produce` シーンを作り直す（`UiRoot` を置き、`ProduceLifetimeScope` を `SceneUiInstaller.Install` と入口 `ProduceEntry` の形にし、`ProduceView`・`ProducePresenter` を消し、`SceneBuilder.BuildProduce` を `BuildHome` と同じ形にする）

### 3.8 `Bootstrap` と `Infrastructure.Api`

- `UtaErrors.Create()`（`Uta.Client.Infrastructure.Api`。タスク 2）: 次の 19 個を、この順で返す。`PlayerErrors.NotCreated` → `ErrorAction.Title`（今までどおり）。残りの 18 個は `ErrorAction.Stay`: `GachaErrors.NotEnoughCurrency`・`NotEnoughPoints`・`NotFound`・`InvalidCount`・`CostMismatch`、`SupportErrors.InvalidTargetLevel`・`NotEnoughPoints`・`MaxLimitBreak`・`NotOwned`・`InvalidMaterial`、`PlayerErrors.NotEnoughGold`、`ProduceErrors.StaleOperation`・`UnexpectedPhase`・`InvalidArgument`・`InvalidFormation`・`NotInProgress`・`SessionDiscarded`・`AlreadyInProgress`（`Uta.Shared.Contracts` の `Gacha`・`Support`・`Player`・`Produce` 名前空間）。登録が無いコードは基盤が `UNKNOWN_ERROR`（`Title`）に変えてしまうため。`handledCodes` に入れなかったコードは、共通ダイアログを出して画面に残る
- `UtaApiClient.Create(string serverUrl, string platform, int appVersion, string accountFilePath, string keySource, ICommonErrorHandler errorHandler, IApiActivityHandler activityHandler, IErrorNavigator errorNavigator)`（タスク 4）: `ApiCore.CreateBuilder(...)` を変数に受け、`.AddActivityHandler(activityHandler)`（null は基盤が無視する）、`.WithErrorNavigator(errorNavigator)` を呼んでから `.Build()`。`IApiActivityHandler` は `VolTech.Client` の IF
- `ApiActivityBridge : IApiActivityHandler`（`Uta.Client.Bootstrap`）:
  ```csharp
  public sealed class ApiActivityBridge : IApiActivityHandler
  {
      public ApiActivityBridge(ActiveSceneUi sceneUi);
      /// <summary>active = sceneUi.Busy（null を許す）。active が null でなければ active.Begin()。</summary>
      public void OnActivityStarted();
      /// <summary>active が null でなく破棄済みでもなければ（Unity の != null）active.End()。active = null。開始のときと違うシーンになっていても、開始したほうの数を戻す。</summary>
      public void OnActivityEnded();
  }
  ```
  `ApiCore` は進行中の数が 0 → 1 で `OnActivityStarted`、1 → 0 で `OnActivityEnded` を 1 回ずつ呼ぶ（エラーハンドラーの間は止める）。通信中にシーンが変わったとき、新しいシーンにはブロッカーが付かない（MVP では受け入れる）
- `ErrorDialogView : IErrorDialogView`（`Uta.Client.Bootstrap`。`MonoBehaviour` をやめた普通の `sealed class`）: コンストラクター `ErrorDialogView(bool autoDismiss, ActiveSceneUi sceneUi)`。`ShowAsync(ApiError error, ErrorDialogButtons buttons)`（`async`）:
  1. 今までどおり `Debug.LogWarning("[Uta] error dialog: " + error + " buttons=" + buttons)`
  2. `autoDismiss` なら `Fallback(buttons)` を返す。`Fallback`: Ok → Ok、RetryOrTitle → Title、Home → Home、Title → Title、それ以外 → Store（今までの自動応答と同じ）
  3. `var dialogs = sceneUi.Dialogs;` が null なら `Debug.LogWarning("[Uta] error dialog: no scene ui")` して `Fallback(buttons)`
  4. ラベルと読み替え: `Ok` → `new[] { "OK" }` → 0: `Ok`、`RetryOrTitle` → `new[] { "再試行", "タイトルへ" }` → 0: `Retry`／1: `Title`、`Home` → `new[] { "ホームへ" }` → 0: `Home`、`Title` → `new[] { "タイトルへ" }` → 0: `Title`、それ以外 → `new[] { "ストアへ" }` → 0: `Store`。`var index = await dialogs.ChooseAsync("エラーが発生しました。\n" + error, labels);` が −1（シーンを閉じてダイアログが消えた）なら `Fallback(buttons)`
  - 旧 `Initialize`・`OnGUI`・`OnDestroy`・`ButtonAt`・`ButtonText` は削除する。置き場は `Bootstrap` のまま（`Presentation` は VolTech-MagicOnion のクライアントを参照しないため）
- `ErrorNavigator : IErrorNavigator`（既存に足す）: コンストラクターを `ErrorNavigator(ISceneNavigator scenes, IObjectResolver resolver)` にする（`IObjectResolver` は VContainer がどのスコープにも自動で登録する。ルートのスコープのものが入る）。`private bool refreshing;`。**`RefreshPlayerUseCase` をコンストラクターで受けない**: `UtaApiClient` のファクトリー → `IErrorNavigator` → `RefreshPlayerUseCase` → `IPlayerPort`（`PlayerGateway`）→ `UtaApiClient` の循環になり、VContainer が解決で例外を投げるため
  - `GoToTitleAsync()`: 今までどおり `scenes.GoToTitleAsync()`
  - `GoToHomeAsync()`（`async`）: `refreshing` が false なら `refreshing = true;` → `try { await resolver.Resolve<RefreshPlayerUseCase>().ExecuteAsync(); } catch (Exception e) { Debug.LogWarning("[Uta] refresh on go-to-home failed: " + e.Message); } finally { refreshing = false; }`（`Resolve` は呼ぶたびに行う。`Singleton` なので同じものが返る。解決の例外も同じ `catch` に入る）。そのあと `await scenes.GoToHomeAsync();`（取り直しの中で再び「ホームへ」が来たら、取り直しを飛ばして読み込むだけ。`ApiCore` は順番待ちを解いてから遷移を呼ぶので、ここで通信してよい）
  - `OpenStoreAsync()`: 今までどおり（ログだけ）
  - テスト: 5章の `ErrorNavigatorTests`（遅延解決で取り直す・取り直しの再入を止める・取り直しの失敗でもホームへ・解決の失敗でもホームへ）
- `BootstrapEntry`（`MonoBehaviour`。`Bootstrap` シーンに置く）:
  - `[SerializeField] private UiRoot uiRoot;` を足す（`SceneBuilder` が入れる）。`Start` → `RunAsync().Forget()`（`RunAsync` はインスタンスメソッドにする。`uiRoot` を使うため）
  - `RunAsync` の手順（この順）:
    1. `serverUrl`・`quitAfterCheck` を今までと同じに読む
    2. マスタ: `var db = await MasterDataLoader.LoadFromStreamingAssetsAsync(); cardCatalog = MasterCardCatalog.Create(db); gameMaster = new MasterGameData(db);` を `try` で包む。失敗したら `Debug.LogError("[Uta] master: FAILED " + e.GetType().FullName + ": " + e.Message)`。`quitAfterCheck` なら `UnityEngine.Application.Quit(1)` して終わる。そうでなければ `await new DialogService(uiRoot.DialogLayer, uiRoot.DialogPrefab).ShowInfoAsync("マスタを読めませんでした: " + e.Message);` して終わる（認証に進まない。screen-design 4章 1）。成功したら今までどおり `Debug.Log("[Uta] master: ok cards=" + cardCatalog.CardIds.Count)`（診断の確認で見る行）
    3. `var sceneUi = new ActiveSceneUi(); var errorDialog = new ErrorDialogView(quitAfterCheck, sceneUi);`
    4. `var accountPath = ...`（今までと同じ）。`LifetimeScope.Create` で下の登録をする。`DontDestroyOnLoad(scope.gameObject)`、`await scope.Container.Resolve<ISceneNavigator>().GoToTitleAsync();` は今までどおり
    5. 2〜4 の外側の `catch`（今までの "bootstrap: FAILED"）は、`Debug.LogError` のあと、`quitAfterCheck` なら `Quit(1)`、そうでなければ `ShowFailureDialog(uiRoot, "起動に失敗しました: " + e.Message)`。**手順 4 の `GoToTitleAsync` で `Bootstrap` シーンが破棄されたあとに例外が来ると、`uiRoot` は Unity の null（破棄済み）になっている**。そのときは破棄済みの `uiRoot.DialogLayer` に触らず（`MissingReferenceException` を出さず）、`Debug.LogError` のログだけにする（`ShowFailureDialog` が `false` を返す）
  - `public static bool ShowFailureDialog(UiRoot uiRoot, string message)`（起動の失敗のダイアログ。テストのため `static` で分ける）: `if (uiRoot == null) return false;`（Unity の `==`。破棄済みも C# の null も `false`。ダイアログは出さない）。そうでなければ `new DialogService(uiRoot.DialogLayer, uiRoot.DialogPrefab).ShowInfoAsync(message).Forget(); return true;`。手順 2（マスタの失敗）は `GoToTitleAsync` の前で `uiRoot` が生きているので、今までどおり `await new DialogService(...).ShowInfoAsync(...)` を使う
  - `StartupView` の作成・`SetText` は消す（`StartupView.cs` を削除）
  - ルートのスコープの登録:
    ```csharp
    builder.RegisterInstance<ICardCatalog>(cardCatalog);
    builder.RegisterInstance<IGameMaster>(gameMaster);
    builder.RegisterInstance(sceneUi);
    builder.RegisterInstance(new StartupOptions(quitAfterCheck));
    builder.Register<SceneNavigator>(Lifetime.Singleton).As<ISceneNavigator>();
    builder.Register<ErrorNavigator>(Lifetime.Singleton).As<IErrorNavigator>();
    builder.Register<UtaApiClient>(
        resolver => UtaApiClient.Create(serverUrl, AppInfo.Platform, AppInfo.Version, accountPath, AppInfo.AccountKeySource,
            new DefaultCommonErrorHandler(errorDialog),
            quitAfterCheck ? null : new ApiActivityBridge(sceneUi),
            quitAfterCheck ? null : resolver.Resolve<IErrorNavigator>()),
        Lifetime.Singleton);
    RegisterUseCases(builder);
    ```
  - `private static void RegisterUseCases(IContainerBuilder builder)`: ゲートウェイ・キャッシュ・ユースケースの登録をまとめる。この計画では次の 6 行。**2〜6 本目はここに 1 行ずつ足す**:
    ```csharp
    builder.Register<DeviceAuthGateway>(Lifetime.Singleton).As<IDeviceAuthPort>();
    builder.Register<PlayerGateway>(Lifetime.Singleton).As<IPlayerPort>();
    builder.Register<PlayerCache>(Lifetime.Singleton);
    builder.Register<SignInUseCase>(Lifetime.Singleton);
    builder.Register<EnterPlayerUseCase>(Lifetime.Singleton);
    builder.Register<RefreshPlayerUseCase>(Lifetime.Singleton);
    ```
  - 診断（`quitAfterCheck`）では、今までどおりナビゲーターを `ApiCore` に渡さない。ブロッカーも渡さない（画面を出さないため）

### 3.9 P2 に求める骨格（`TitlePage`・`HomePage`）

P2 が作る骨格に、次のフィールドがあることを前提にする（子の名前は役割の PascalCase、フィールドは camelCase。prefab-memo の規則）。P2 の表が正で、食い違ったらこの表と 3.7 を直す（前提 4）。

| View | フィールド（`[SerializeField] private`） | Prefab の子（名前） | 使い方 |
|---|---|---|---|
| `TitlePage` | `ButtonView startButton` | `StartButton`（→ `Button`） | `StartClicked`・`SetStartInteractable` |
| | `TMP_Text statusText` | `StatusText`（→ `Label`） | `SetStatus`（複数行。折り返しあり） |
| `HomePage` | `TMP_Text profileText` | `ProfileText` | `SetProfile` |
| | `RawImage stageImage` | `Stage` | 使わない（MVP。3D の `ModelCharacterStage` が使う） |
| | `TMP_Text stageLabel` | `Stage/StageLabel` | `CreateStage`（`PlaceholderCharacterStage` に渡す） |
| | `GameObject resumePanel` | `ResumePanel` | `SetResumeVisible` |
| | `ButtonView primaryButton` | `PrimaryButton` | `ProduceClicked`・`SetResumeVisible`（ラベル） |
| | `ButtonView navProduceButton` | `Nav/NavProduceButton` | `ProduceClicked` |
| | `ButtonView navGachaButton` | `Nav/NavGachaButton` | `GachaClicked` |
| | `ButtonView navOwnedButton` | `Nav/NavOwnedButton` | `OwnedClicked` |

### 3.10 `SceneBuilder`（`Uta.Client.Editor`）

`Uta/Create Scenes`（`public static void CreateScenes()`）の形は今のまま（4 シーンを空から作り、Build Settings に並べる）。変えるのは `Bootstrap`・`Title`・`Home` だけ（`BuildProduce` は #24 のまま。決めたこと 12）:

- `private const string UiRootPrefabPath = "Assets/Prefabs/Parts/UiRoot.prefab";`
- `private static UiRoot InstantiateUiRoot()`: `AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPrefabPath)` が null なら `InvalidOperationException("UiRoot prefab not found. run Uta/Prefabs/Build All first")`。`(GameObject)PrefabUtility.InstantiatePrefab(prefab)`（いま開いている新しいシーンに置かれる）の `GetComponent<UiRoot>()` を返す
- `private static void SetRef(Object target, string field, Object value)`: `new SerializedObject(target)` の `FindProperty(field)` が null なら `InvalidOperationException(target.GetType().Name + "." + field + " not found")`。`objectReferenceValue = value` → `ApplyModifiedPropertiesWithoutUndo()`
- `BuildBootstrap`: `CreateCamera()` → `var uiRoot = InstantiateUiRoot();` → `new GameObject("BootstrapEntry").AddComponent<BootstrapEntry>()` に `SetRef(entry, "uiRoot", uiRoot)`
- `BuildTitle`・`BuildHome`: `CreateCamera()` → `var uiRoot = InstantiateUiRoot();` → `new GameObject("TitleScope").AddComponent<TitleLifetimeScope>()`（Home は `"HomeScope"`・`HomeLifetimeScope`）に `SetRef(scope, "uiRoot", uiRoot)`。Canvas・EventSystem・ボタン・文字は作らない（`UiRoot.prefab` が `Canvas`・`EventSystem` を持つ。画面はページの Prefab）
- `BuildProduce` は変えない。`BuildProduce` が使う `CreateEventSystem`・`CreateCanvas`・`CreateRect`・`CreateText`・`ConfigureText`・`CreateButton` と、`using` の `UnityEngine.EventSystems`・`UnityEngine.InputSystem.UI`・`UnityEngine.UI`・`Uta.Client.Presentation.Produce` も残す（4 本目が `BuildProduce` を直すときに消す）
- 使わなくなる `using` の `Uta.Client.Presentation.Title`・`Uta.Client.Presentation.Home`（`TitleView`・`HomeView` を消したため）は消し、`Uta.Client.Presentation.Common`（`UiRoot`）を足す
- `CreateCamera` は今のまま（`ScreenSpaceOverlay` の Canvas の後ろを塗る）

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行。コンパイルエラー 0）が通り、EditMode のテスト（回し方は P1 の決めたこと 12: Editor を開いたまま `TestRunnerApi` を eval で回す。コマンドは CLAUDE.md「ビルド・テスト・実行」・unity7-evaluation 4章）で既存とそのタスクまでのテストが全部通る状態にする。**eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない**（P1 の決めたこと 12）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。コミットの前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'` を実行する（P1。テスト・Play で動的フォントに足された字を消す。3.3.7）。

タスク 1・2 の間、アプリの動き（#24 のシーン・legacy `Text` の仮の画面）は変わらない（足した型はまだシーンから使わない）。タスク 3 から Title・Home を P2 の Prefab に置き換える。

#### タスク 1: 共通部品とテスト用の偽

前提の確認: 前提 3（P1 の型・Prefab。テストを eval で回せないことは止まる理由にしない）と前提 4（P2 の `TitlePage`・`HomePage` の骨格と Prefab）。無ければ止まって報告する。

触る: テストの asmdef（3.1）、`Presentation/Common/BusyIndicator.cs`（足す）・`Parts/` の 7 ファイル（足す）、`UiObjects.cs`・`DialogResult.cs`・`DialogService.cs`・`PageStack.cs`・`ActiveSceneUi.cs`・`SceneUiActivation.cs`・`SceneUiInstaller.cs`、`Tests/EditMode/Fakes/*.cs`、`Tests/EditMode/BusyIndicatorTests.cs`・`DialogServiceTests.cs`・`PageStackTests.cs`・`ActiveSceneUiTests.cs`。

手順:
1. テストの asmdef を 3.1 のとおりに直す
2. 3.2 のとおりに足す・作る（P1 のフィールドは変えない）
3. 3.3.8 の偽を作る
4. 5章のテストを作り、`unity recompile` → EditMode のテスト

境界: `ChooseAsync` に null・0 個・5 個以上のラベルを渡すと `ArgumentException`（待ち行列に入れない）。`DialogService` の層が破棄済み（Unity の null）なら、ダイアログを出さずに −1（`Negative`）で終わる。C# の null の層は今までどおり `InvalidOperationException`（3.2）。`BusyIndicator.End` を `Begin` より多く呼んでも数は 0 より下がらない。`PageStack` は枚数が 0・1 のとき Pop を呼ばない。`ActiveSceneUi.Detach` は別の `UiRoot` を渡されたら何もしない。

止まる条件（当たったら止まって報告する。直し方を自分で決めない）:
- VContainer 1.19.0 に `RegisterComponent`・`RegisterEntryPoint`・`Register<T>().As<TInterface>()` が 3.2 の使い方で無い
- 基盤の `ScreenNavigator` のコンストラクターが `(PageContainer, ModalContainer, OverlayContainer, IObjectResolver)` でない
- P1 の部品の View のフィールドの名前・型が P1 の 3.3 の表と違う
- `DialogServiceTests` で、待ち行列が空のときにダイアログが同期で出ない、または前のダイアログのボタンを押した直後に次のダイアログが同期で出ない（3.2 の待ち行列の前提。`UniTask.Yield` などで直さない）

終わったことの確かめ方: コンパイルエラー 0。`BusyIndicatorTests`・`DialogServiceTests`・`PageStackTests`・`ActiveSceneUiTests` と既存のテスト（P1 の `PrefabContractTests` を含む）が通る。

#### タスク 2: マスタの読み取り口・取り直し・業務エラーの登録

前提の確認: 生成された `Scripts/Infrastructure/MasterData/Generated/Lesson/Card.cs` に `public string Name`、`CardFace.cs` に `public string Description` がある（計画 0）。無ければ止まって報告する。

触る: `Application/Master/*.cs`（11 ファイル）、`Application/Player/RefreshPlayerUseCase.cs`・`PlayerCache.cs`、`Infrastructure/MasterData/MasterGameData.cs`、`Infrastructure/Api/UtaErrors.cs`、`Tests/EditMode/MasterGameDataTests.cs`・`RefreshPlayerUseCaseTests.cs`・`PlayerCacheTests.cs`・`UtaErrorsTests.cs`。

手順: 3.4・3.5・3.6・3.8（`UtaErrors`）のとおり作る・直す。`UtaErrorsTests` は 5章のとおり書き直す（旧 `Create_ContainsPlayerNotCreatedWithTitleAction` は消す）。

境界: `GetSupportLevelUpCost` は `fromLevel >= toLevel` なら `SupportLevelUpCost(0, 0)`。`GetSupportAbilities` は行が 0 件なら空のリスト（例外にしない）。

終わったことの確かめ方: `MasterGameDataTests`・`RefreshPlayerUseCaseTests`・`PlayerCacheTests`・`UtaErrorsTests` と既存のテストが通る。

#### タスク 3: タイトル・ホームの Presentation

触る: `Presentation/Title/*`（`TitlePage.cs` は足す、`TitlePresenter.cs`・`TitleLifetimeScope.cs` は置き換え、`ITitleView.cs`・`TitleEntry.cs`・`TitleDiagnostics.cs`・`StartupLogs.cs` は作成、`TitleView.cs` は削除）、`Presentation/Home/*`（`HomePage.cs` は足す、`HomePresenter.cs`・`HomeLifetimeScope.cs` は置き換え、`IHomeView.cs`・`ICharacterStage.cs`・`PlaceholderCharacterStage.cs`・`HomeEntry.cs` は作成、`HomeView.cs` は削除）、`Editor/SceneBuilder.cs`（3.10）、`Presentation/Title/ConnectivityCheck.cs`・`Application/Auth/SignInUseCase.cs`（コメントだけ）、`Tests/EditMode/Fakes/StubIdolMaster.cs`、`Tests/EditMode/TitlePresenterTests.cs`・`TitleEntryTests.cs`・`HomePresenterTests.cs`・`PlaceholderCharacterStageTests.cs`・`HomeEntryTests.cs`。

手順:
1. 3.7 のとおりに作る・置き換える・削除する（削除は `.cs` と `.meta` の両方）。3.7 のコメントの直しもする
2. `Editor/SceneBuilder.cs` が消した型（`TitleView`・`HomeView`）を参照してコンパイルが通らなくなるので、このタスクで 3.10 のとおりに `SceneBuilder.cs` も直す（シーンの作り直しはタスク 4）
3. 5章のテストを作り、`unity recompile` → EditMode のテスト

境界: `TitlePresenter.StartAsync` を実行中にもう一度呼ぶと何もしない。サインインの途中で `Dispose` されたら、そのあと View に触らない。`HomeEntry` は `cache.Current` が null ならページを積まない。

このタスクの終わりでは、シーン（`.unity`）が古いスコープの形のままなので、Play は確かめない（タスク 4 で作り直してから確かめる）。

終わったことの確かめ方: コンパイルエラー 0。5章の Title・Home のテストと既存のテストが通る。

#### タスク 4: Bootstrap・シーンの組み替えと Play の確認（止まる条件あり）

触る: `Bootstrap/Uta.Client.Bootstrap.asmdef`（3.1）、`Bootstrap/ApiActivityBridge.cs`・`ErrorDialogView.cs`・`ErrorNavigator.cs`・`BootstrapEntry.cs`、`Presentation/Startup/StartupView.cs`（削除）、`Infrastructure/Api/UtaApiClient.cs`、`Scenes/*.unity`（生成）、`client/uta/ProjectSettings/ProjectSettings.asset`、`Tests/EditMode/ApiActivityBridgeTests.cs`・`ErrorDialogViewTests.cs`・`ErrorNavigatorTests.cs`・`BootstrapEntryTests.cs`。

手順:
1. 3.1 の Bootstrap の asmdef、3.8 のとおりに作る・直す。`StartupView.cs`（と `.meta`）を削除する
2. 5章の `ApiActivityBridgeTests`・`ErrorDialogViewTests`・`ErrorNavigatorTests`・`BootstrapEntryTests` を作り、`unity recompile` → EditMode のテスト
3. シーンを作り直す: `unity command eval 'Uta.Client.Editor.SceneBuilder.CreateScenes(); return "ok";'` が `ok`。`git status` で `Scenes/` の `Bootstrap`・`Title`・`Home` の `.unity` が変わり、`SampleScene` などほかのシーンが増えていないこと（`Produce.unity` は #24 と同じ中身を作り直すだけなので、差分が出ても出なくてもよい。中身の形が変わっていたら止まって報告する）
4. `ProjectSettings.asset` を直す（テキストで該当行だけ。Editor を閉じてから編集し、開き直す）: `defaultScreenWidth: 540`、`defaultScreenHeight: 960`、`fullscreenMode: 3`、`resizableWindow: 1`、`defaultScreenOrientation: 0`、`allowedAutorotateToPortrait: 1`、`allowedAutorotateToPortraitUpsideDown: 0`、`allowedAutorotateToLandscapeRight: 0`、`allowedAutorotateToLandscapeLeft: 0`（いまは順に 1024・768・1・0・4・1・1・1・1）
5. Play で確かめる（ローカルのサーバーを起動した状態。CLAUDE.md「ローカルのサーバー」）:
   1. `unity command editor.playmode start` → 数秒後 `unity command console get --tail 50 --level error --format json` にエラーが無い
   2. `unity command eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ":" + UnityEngine.Object.FindFirstObjectByType<Uta.Client.Presentation.Common.UiRoot>().Pages.Pages.Count + ":" + (UnityEngine.Object.FindFirstObjectByType<Uta.Client.Presentation.Title.TitlePage>() != null);'` が `Title:1:True`
   3. `unity command eval 'UnityEngine.GameObject.Find("StartButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "ok";'` → 数秒後 `unity command console get --tail 50 --format json` に `[Uta] sign-in: ok` があり、2 と同じ式（`TitlePage` を `Uta.Client.Presentation.Home.HomePage` に変える）が `Home:1:True`
   4. `unity command eval 'UnityEngine.GameObject.Find("NavGachaButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return (UnityEngine.GameObject.Find("Dialog") != null).ToString();'` が `True`（「準備中です。」）→ `unity command eval 'UnityEngine.GameObject.Find("Button0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "ok";'` → `unity command eval 'return (UnityEngine.GameObject.Find("Dialog") == null).ToString();'` が `True`（`Destroy` は次のフレームなので、1 回目が `False` なら 1 秒おいてもう一度）
   5. `unity command editor.playmode stop`。見た目は 5章の手動の確認で見る

止まる条件（当たったら、その時点のコンソールの出力を添えて止まって報告する。別の手段へ変えない）:
- B1: 手順 5 の 2 で `TitlePage` が積まれない（基盤の `ScreenNavigator` の Push が、シーンのスコープの子スコープ・Addressables の読み込みのどちらかで失敗する。コンソールに例外が出る）
- B2: 子スコープで `TitlePresenter` が解決できない（VContainer の例外。`[Inject]` のコンストラクターが選ばれない、など）
- B3: シーンの `LifetimeScope` の `uiRoot` が null で `SceneUiInstaller` が例外を投げる（`SceneBuilder` の `SetRef` が効いていない）
- B4: `VContainer` の `RegisterComponent` で登録した `PageContainer` などが、基盤の `ScreenNavigator` のコンストラクターに渡らない
- B5: DI の循環が起きる（VContainer の循環参照の例外。`ErrorNavigator` の遅延解決（3.8）でも残る、別の経路で循環する、など）。登録やコンストラクターの形を自分で変えない

終わったことの確かめ方: コンパイルエラー 0。全部のテストが通る。手順 5 の 2〜4 の値が期待どおり。

#### タスク 5: 診断モードとプレイヤーの確認

手順:
1. サーバー（`docker compose ... up` → `dotnet run --project server/Uta.Server -- migrate` → `dotnet run --project server/Uta.Server`）を起動した状態で、Editor を閉じ、Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」。P1 が `BuildPlayerContent` を足してある）が終了コード 0
2. `Uta.exe -batchmode -nographics -uta-quit-after-check` が終了コード 0 で、ログに `connectivity check: OK`（`TitleDiagnostics`）
3. ユーザーに `Uta.exe` を引数なしで起動してもらい、5章の手動の確認（タイトル → ホーム → 「準備中です。」、ウィンドウの大きさ）を見てもらう（手動）

Editor を閉じる・サーバーを起動する環境がなければ、確かめていないと報告する。

#### タスク 6: 結果の記録（wiki）

1. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md):
   - 2章の表に、確かめたものを 1 行ずつ足す（基盤の `ScreenNavigator`（Presenter 駆動）で、シーンのスコープから Addressables のページを積む（Editor の Play・Windows の IL2CPP）／`IApiActivityHandler` → `BusyIndicator`／`DialogService`（`Dialog.prefab`））。確かめられなかったものは 3.3 に足す。モーダル（`ModalBase`・`DefaultModalBackdrop`）は「この計画では出していない。2 本目で確かめる」と 3.3 に書く
   - 3.3 の 63 行「`ErrorDialogView`（診断用の IMGUI）」を「`ErrorDialogView`（いまのシーンの `DialogService`。`-uta-quit-after-check` では自動で閉じる）」に直す（経路が未確認なのは変わらない）
   - 3.3 の 68 行「UnityScreenNavigator・Supplement は取り込みだけで、使っていない」を「UnityScreenNavigator は基盤の `Runtime/ChinpanGX`（`ScreenNavigator`）を画面に使った（2章）。Supplement は使っていない」に直す（P1 のタスク 7 が先に直していれば、それに合わせて足す）
   - 4章 80 行「画面は IMGUI の診断用で、画面の計画（7 番目）で作り直す」を「画面は順 7 の計画（`uta_docs/plans/2026-11-03-client-screens-foundation.md`）で Prefab に作り直した。診断は `-uta-quit-after-check` のときだけ（`TitleDiagnostics`）」に直す
2. [screen-design.md](../wiki/architecture/screen-design.md)（frontmatter の `updated` を実施日に）:
   - 4章 手順 4 の最後「`Title` の最小の uGUI に状態を出し、成功したら `Home` へ進む（7章）。診断用の IMGUI（`ErrorDialogView`、マスタの失敗の `StartupView`）が残る。画面の作り込みは順 7 [2026-11-03]」を「`Title` の `TitlePresenter` が状態を出し、成功したら `Home` へ進む。プロデュースの破棄の知らせは `HomePresenter` が 1 回だけ出す。マスタの失敗と共通エラーは、いまのシーンの `DialogService` のダイアログ（8章）[実施日]」に直す
   - 5章 1 つ目の括弧「ナビゲーターは `ErrorNavigator`（7章）、進行中通知は未接続で、エラーハンドラーは `DefaultCommonErrorHandler` と診断用の `ErrorDialogView`。画面の計画で作り直す。2026-11-03」を「ナビゲーターは `ErrorNavigator`（8章。「ホームへ」の前に所持データを取り直す）、進行中通知は `ApiActivityBridge` → いまのシーンの `BusyIndicator`、エラーハンドラーは `DefaultCommonErrorHandler` と `ErrorDialogView`（いまのシーンの `DialogService`）。業務エラーは `UtaErrors` に `Stay` で登録する。[実施日]」に直す
   - 7.4 の表の「組み立て」の行に「シーンの `LifetimeScope` は `SceneUiInstaller.Install` で `UiRoot` のコンテナー・`IScreenNavigator`・`PageStack`・`DialogService` を登録する」を足す。同じ行の「Presenter はどのスコープにも登録しない（`[AssetAddress]` で、`IScreenNavigator` が Push のたびにシーンのスコープの子スコープで解決する）」は計画の段階（2026-11-07）で直してある。実装と違えば実装に合わせて直す。表の下に「**Presenter の書き方の正**は `uta_docs/plans/2026-11-03-client-screens-foundation.md` 3.3」を 1 行足す
   - 7.5 の「エラー」の項の「（8章の `ISceneNavigator` を呼ぶ）」のあとに「。ルートの `ErrorDialogView`・`ApiActivityBridge` は `ActiveSceneUi`（各シーンのスコープが `Start` で入れる）から、いまのシーンのダイアログ・ブロッカーを取る」を足す
   - 8章の冒頭「（順 7 の画面の計画で使う。この章の時点では各シーンに最小の uGUI を置くだけ）」を「（`Bootstrap`・`Title`・`Home` に `UiRoot.prefab` を 1 つずつ置く。`Bootstrap` の `UiRoot` はマスタの失敗と起動の失敗のダイアログだけに使う。`Produce` は順 7 の 4 本目が作り直す）」に、「最小の uGUI は、Editor のスクリプト（`Uta/Create Scenes`）で作り、…置き換える（…）」の項を「シーンは Editor のスクリプト（`Uta/Create Scenes`）で作り、`.unity` をコミットする。`Bootstrap`・`Title`・`Home` にはカメラ・`UiRoot.prefab`・`LifetimeScope`（`Bootstrap` は `BootstrapEntry`）だけを置き、画面はページの Prefab（7章）。`Produce` はまだ最小の uGUI（4 本目が同じ形にする）」に、「画面の計画で、診断を別の入口に分ける [Uta案]」を「診断は `TitleDiagnostics`（`TitleEntry` が分ける）[実施日]」に直す。8章の 1 つ目の項（ルートの DI スコープ）の「シーンの `LifetimeScope` は `UiRoot`・入口と補助だけを登録し、Presenter と View は登録しない」は計画の段階（2026-11-07）で直してある。実装と違えば実装に合わせて直す。表の `Home` の行の遷移「→ `Produce`」を「→ `Produce`（順 7 の 4 本目（formation）から。1 本目の時点ではホームのボタンは「準備中です。」で、移る道が無い）」に、役割の「ここから `Produce` へ」を「ここから `Produce` へ（4 本目から）」に直す。`Produce` の行は変えない（4 本目が直す）
   - 「実装メモ」に、設計とのズレを書く（なければ「順 7 の 1 本目: なし」）
3. [repository-layout.md](../wiki/architecture/repository-layout.md):
   - 126 行「診断用の `ErrorDialogView`（`IErrorDialogView` の IMGUI 実装）は `Bootstrap` に置く [2026-11-03]: …」を「`ErrorDialogView`（`IErrorDialogView` の実装。表示は `Presentation` の `DialogService` に任せる）・`ApiActivityBridge`（`IApiActivityHandler` の実装）は `Bootstrap` に置く [実施日]: `Presentation` の参照先に VolTech-MagicOnion のクライアントが無いため」に直す
   - 3章の表に `Uta.Client.Bootstrap` の行があれば、参照に `UnityScreenNavigator` を足す（無ければ足さない）
   - 277 行付近の未確認の表の UnityScreenNavigator の「確かめた範囲」に、この計画で確かめた範囲（基盤の `ScreenNavigator` でページの Push、Editor の Play と Windows の IL2CPP）を足す
4. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
5. 全部のタスクが終わったら、この計画の `status` を `done` にする

## 5. テスト

置き場: `Tests/EditMode/`。既存のテスト用 asmdef（`Uta.Client.Infrastructure.Tests.EditMode`）を使う。名前空間は `Uta.Client.Infrastructure.Tests`。書き方は 3.3.7。実行は Editor を開いたまま `TestRunnerApi` を eval で回す（P1 の決めたこと 12。回せなければ Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。4章の冒頭）。

| タスク | ファイル | テスト | 内容 |
|---|---|---|---|
| 1 | `BusyIndicatorTests.cs` | `Begin_Once_IsBusy` | `new GameObject().AddComponent<BusyIndicator>()`（blocker は null）。`Begin()` で `IsBusy == true`、`End()` で false |
| | | `Begin_Nested_StaysBusyUntilAllEnded` | `Begin()` 2 回、`End()` 1 回ではまだ true、もう 1 回で false |
| | | `End_WithoutBegin_DoesNothing` | `IsBusy == false` のまま、例外なし。そのあと `Begin()` 1 回で true |
| | | `Begin_WithBlocker_EnablesImage` | `Image`（enabled = false）を `new SerializedObject(busy)` の `"blocker"` に入れて `ApplyModifiedPropertiesWithoutUndo` → `Begin()` で `image.enabled == true`、`End()` で false |
| 1 | `DialogServiceTests.cs` | `ConfirmAsync_PositiveClicked_ReturnsPositive` | 下の「`DialogServiceTests` の書き方」のとおり。`var task = service.ConfirmAsync("m");`（await しない）→ 層の子が 1 → ボタン [0] の `onClick.Invoke()` → `task.Status == UniTaskStatus.Succeeded`、`task.GetAwaiter().GetResult() == DialogResult.Positive`、層の子が 0 |
| | | `ConfirmAsync_NegativeClicked_ReturnsNegative` | 同じく [1] を押すと `Negative` |
| | | `ChooseAsync_SecondClicked_ReturnsOne` | ラベル 3 つ。ボタンが 3 つで、[1] を押すと結果が 1 |
| | | `ChooseAsync_FiveLabels_Throws` | `Assert.Throws<ArgumentException>(() => service.ChooseAsync("m", new[] { "a", "b", "c", "d", "e" }))`（検証は待ち行列の前で同期に投げる）。層の子は 0 のまま |
| | | `ShowInfoAsync_Twice_ShowsOneAtATime` | `var first = service.ShowInfoAsync("a"); var second = service.ShowInfoAsync("b");`（どちらも await しない）→ 層の子が 1 で、その `MessageText`（名前で探した `TMP_Text`）の text が `"a"`、`second.Status == Pending` → ボタン [0] を押す → `first` が `Succeeded`、層の子が 1 で text が `"b"`（次のダイアログは同期で出る。3.2）→ もう一度 [0] を押す → `second` が `Succeeded`、層の子が 0 |
| | | `LayerDestroyed_ReturnsWithoutDialog` | `Object.DestroyImmediate(layer.gameObject)` のあとに `var confirm = service.ConfirmAsync("m"); var choose = service.ChooseAsync("m", new[] { "a" }); var info = service.ShowInfoAsync("m");`（await しない）→ 3 つとも `Status == Succeeded`（例外なし。`MissingReferenceException` が出ない）、`confirm` の結果が `Negative`、`choose` の結果が −1（層が破棄済みなら、要求ごとの開始時の確かめで −1。3.2）。`[TearDown]` は `if (layer != null)` のときだけ `DestroyImmediate` する |
| | | `ButtonColors_PrimaryThenSecondary` | `ConfirmAsync("m")`（await しない）のボタン [0] の `ButtonView` の `Image.color == UiTheme.ButtonPrimary`、[1] が `ButtonSecondary`。最後に [0] を押して閉じる |
| 1 | `PageStackTests.cs` | `PopToRoot_ThreePages_PopsTwo` | `new PageStack(() => 3, () => 0)` → `log` が `"PopPage:2"` だけ |
| | | `PopToRoot_WithModals_PopsModalsFirst` | 2・1 → `"PopModal:1"`, `"PopPage:1"` |
| | | `PopToRoot_OnePage_DoesNothing` | 1・0 → 記録なし |
| | | `PopToRoot_Empty_DoesNothing` | 0・0 → 記録なし |
| 1 | `ActiveSceneUiTests.cs` | `Attach_SetsRootDialogsAndBusy` | `var root = new GameObject("Root").AddComponent<UiRoot>();`（EditMode では `Awake` が走らないので、`EnsureConfigured` も走らない）、`var busy = new GameObject("Busy").AddComponent<BusyIndicator>();`。`var so = new SerializedObject(root); so.FindProperty("busyIndicator").objectReferenceValue = busy; so.ApplyModifiedPropertiesWithoutUndo();`（`ApiActivityBridgeTests` と同じ入れ方。ほかの参照は null のまま）。`new DialogService(null, null)` と一緒に `Attach` → `Root` が root、`Dialogs` が同じ `DialogService`、`Busy` が busy |
| | | `Detach_SameRoot_Clears` | 上と同じ組み立て → `Detach(同じ root)` で `Root`・`Dialogs`・`Busy` が null |
| | | `Detach_OtherRoot_KeepsCurrent` | A を Attach → B を Attach → `Detach(A)` で `Root` は B のまま |
| | | `Attach_Null_Throws` | `ArgumentNullException` |
| | | `Busy_DestroyedRoot_ReturnsNull` | 上と同じ組み立てで Attach したあと root の GameObject を `DestroyImmediate` → `Busy` が null（例外なし） |
| 2 | `MasterGameDataTests.cs` | `Constructor_FromStreamingAssets_ReadsAllTables` | `MasterDataLoader.Load(File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath))` で作る（`MasterDataLoaderTests` と同じ読み方）。`Idols.Count == 4`、`SupportCards.Count == 10`、`Gachas.Count == 1`、`Scenarios.Count == 2` |
| | | `GetSupportLevelUpCost_SumsRows` | R の Lv1→Lv3 が `SupportLevelCostTable` の (R, 1) と (R, 2) の行の合計 |
| | | `GetSupportLevelUpCost_SameLevel_ReturnsZero` | `fromLevel == toLevel` で (0, 0) |
| | | `GetSupportMaxLevel_MatchesCap` | `SupportLevelCapTable` の各行と一致 |
| | | `GetGacha_NormalEntriesOrderedAndWeighted` | `NormalEntries` が `GachaTableEntry.Order` の昇順で、`Weight` の合計が 10000 |
| | | `GetSupportAbilities_OrderedAndMapped` | `SupportCards[0]` の `SupportCardId` で、`SupportAbilityTable` の行（同じ ID、`Order` の昇順）と件数・`Kind`・`Param`・値が一致。件数 1 以上 |
| | | `GetCardText_ReturnsNameAndDescriptions` | `CardTable` の最初の行の `CardId` で、`Name` が `Card.Name`、`Description` が Normal の行の `Description`、`DescriptionEnhanced` が Enhanced の行の `Description`（無ければ Normal と同じ） |
| | | `GetIdol_Unknown_ThrowsKeyNotFound` | |
| | | `GetCardText_Unknown_ThrowsKeyNotFound` | |
| 2 | `RefreshPlayerUseCaseTests.cs` | `Execute_Success_UpdatesCache` | `StubPlayerPort` が返した `PlayerSnapshot` が `PlayerCache.Current` になり、戻り値と同じ |
| | | `Execute_Failed_Throws` | `PlayerSignInResult.Failure("x")` → `InvalidOperationException`（メッセージが `"sign-in failed: "` で始まる）、キャッシュは変わらない |
| | | `Execute_NotCreated_Throws` | メッセージが `"sign-in failed: player not created"` |
| 2 | `PlayerCacheTests.cs` | `MarkDiscardNoticeShown_SetsFlag` | `Set` → `MarkDiscardNoticeShown()` → true |
| | | `Set_ResetsDiscardNoticeShown` | true のあと `Set(別のスナップショット)` で false |
| 2 | `UtaErrorsTests.cs` | `Create_ContainsAllBusinessErrors` | 長さ 19。`PlayerErrors.NotCreated` が `Title`、3.8 の残り 18 個が全部入っていて `Stay` |
| | | `Create_IsAcceptedByErrorPolicyProvider` | `new ErrorPolicyProvider(UtaErrors.Create())` が例外を投げず、`GetDefaultAction(PlayerErrors.NotCreated) == Title`、`GetDefaultAction(GachaErrors.NotEnoughCurrency) == Stay` |
| 3 | `TitlePresenterTests.cs` | `InitializeAsync_ShowsTapToStart` | 偽の `ITitleView`。`log` が `"SetStatus:タップしてスタート"`, `"SetStartInteractable:True"` |
| | | `StartAsync_Success_GoesHome` | `StubDeviceAuthPort`（Registered）・`StubPlayerPort`（成功）で作った `SignInUseCase`・`EnterPlayerUseCase`（`PlayerCache` は新しいもの）、`FakeSceneNavigator`。`InitializeAsync` のあと `StartAsync` → `log` の続きが `"SetStartInteractable:False"`, `"SetStatus:接続中…"`, `"GoToHome"`。`SetStartInteractable:True` は 2 回目が出ない |
| | | `StartAsync_SignInFails_ShowsRetry` | `EnsureSignedIn` に `() => throw new InvalidOperationException("x")` を 1 つ → 続きが `"SetStartInteractable:False"`, `"SetStatus:接続中…"`, `"SetStatus:接続に失敗しました: x\nタップしてもう一度"`, `"SetStartInteractable:True"`。`"GoToHome"` は無い |
| | | `StartAsync_WhileRunning_Ignored` | `EnsureSignedIn` に未完了の `UniTaskCompletionSource` の Task を返す Func → `var first = presenter.StartAsync();` → `await presenter.StartAsync();`（すぐ終わる）→ 源を完了 → `await first` → `"SetStatus:接続中…"` は 1 回、`"GoToHome"` は 1 回 |
| | | `StartAsync_DisposedDuringSignIn_TouchesNothing` | 上と同じ未完了の源 → `var first = presenter.StartAsync();` → `presenter.Dispose()` → 源に `TrySetException(new InvalidOperationException("x"))` → `await first` → `Dispose` のあとの記録が無い（`"SetStatus:接続に失敗しました…"`・`"GoToHome"` が無い） |
| | | `StartClicked_AfterDispose_DoesNothing` | `Dispose()` のあと偽の View の `RaiseStartClicked()` → `"SetStatus:接続中…"` が無い |
| 3 | `TitleEntryTests.cs` | `StartAsync_Normal_PushesTitle` | `new TitleEntry(navigator, new StartupOptions(false), null)` → `log` が `"PushPage:TitlePresenter"` |
| | | `StartAsync_QuitAfterCheck_Success_QuitsWithZero` | `var codes = new List<int>();`。`StubDeviceAuthPort`（既定）・`StubPlayerPort`（既定）で `SignInUseCase`・`EnterPlayerUseCase`（`PlayerCache` は新しいもの）を作り、`new TitleDiagnostics(signIn, enterPlayer, new ConnectivityCheck(signIn, enterPlayer), code => codes.Add(code))`。`new TitleEntry(navigator, new StartupOptions(true), diagnostics)` の `StartAsync()` を await → `codes` が `{ 0 }`、`log` に `"PushPage:"` で始まる記録が無い |
| | | `StartAsync_QuitAfterCheck_SignInFails_QuitsWithOne` | 上と同じで、`EnsureSignedIn` に `() => throw new InvalidOperationException("x")` を 1 つ → `codes` が `{ 1 }`、`"PushPage:"` の記録が無い |
| 3 | `HomePresenterTests.cs` | （共通の組み立て） | 偽の `IHomeView`（`FakeHomeView`。3.3.7）と偽のステージ `FakeCharacterStage : ICharacterStage`（テストのファイルの中の `private sealed class`。同じ `CallLog` に `ShowIdol` → `"ShowIdol:" + idolId`、`Clear` → `"ClearStage"` を記録）。`new HomePresenter(view, stage, cache, dialogs, master)`。`master` は `new StubIdolMaster(...)`（ステージのテストのほかは育成アイドルなしの `new StubIdolMaster()`）。スナップショットの `Idols` は、指定が無ければ空 |
| | | `InitializeAsync_ShowsProfileAndResume` | `PlayerCache` に Lv 12・ガチャ通貨 3250・`HasActiveProduce = true`・所持の育成アイドルなし → `log` が `"SetProfile:12,3250"`, `"SetResumeVisible:True"`, `"ClearStage"` |
| | | `InitializeAsync_ShowsHighestRarityIdolOnStage` | `StubIdolMaster` に 1（SR）・2（SSR）・3（SSR）・4（R）、所持の順を 4・3・1・2（`OwnedIdol(id, 0, 0, 0)`）→ 最後の記録が `"ShowIdol:2"`（SSR の 2 と 3 のうち `IdolId` が小さい 2。所持の順に依らない） |
| | | `InitializeAsync_SingleIdol_ShowsIt` | 1（N）だけ所持 → 最後の記録が `"ShowIdol:1"` |
| | | `DidPopEnter_RepicksStage` | 所持なしで `InitializeAsync`（`"ClearStage"`）→ キャッシュを 3（SSR）所持のスナップショットに差し替えて `DidPopEnter()` → 最後の記録が `"ShowIdol:3"` |
| | | `ProduceClicked_ShowsNotReady` | `InitializeAsync` のあと `RaiseProduceClicked()` → `InitializeAsync` の後の記録が `"Info:準備中です。"` だけ（4 本目が差し替える） |
| | | `GachaClicked_ShowsNotReady` | 同じく `RaiseGachaClicked()` → `"Info:準備中です。"` だけ（2 本目が差し替える） |
| | | `OwnedClicked_ShowsNotReady` | 同じく `RaiseOwnedClicked()` → `"Info:準備中です。"` だけ（3 本目が差し替える） |
| | | `DidPopEnter_RefreshesFromCache` | キャッシュを Lv 13 に差し替えて `DidPopEnter()` → `"SetProfile:13,..."` |
| | | `Dispose_UnsubscribesEvents` | `Dispose()` のあと 3 つのイベントを起こしても、記録が 1 つも増えない（ボタンの行き先に依らない） |
| | | `OpenGachaAsync_WhileRunning_Ignored` | `FakeDialogService.InfoGate` に未完了の源を入れ、`OpenGachaAsync()` を 2 回呼ぶ → `"Info:"` は 1 回。源を完了させたあと、もう 1 回呼ぶと 2 回目が出る（実行中フラグが finally で戻る）。ガチャのボタンのテストなので、2 本目が `GachaClicked_ShowsNotReady` と一緒に差し替える |
| | | `ShowDiscardNotice_Discarded_ShowsOnce` | `DiscardedProduce = true` のスナップショット → `ShowDiscardNoticeIfNeededAsync()` を 2 回 → `"Info:バージョンが変わったため、進行中のプロデュースは破棄されました。"` が 1 回、`cache.DiscardNoticeShown == true` |
| | | `ShowDiscardNotice_NotDiscarded_ShowsNothing` | `DiscardedProduce = false` → 記録なし |
| 3 | `PlaceholderCharacterStageTests.cs` | `ShowIdol_SetsIdolName` | `var go = new GameObject(); var label = go.AddComponent<TextMeshProUGUI>();`、`new PlaceholderCharacterStage(label, new StubIdolMaster(StubIdolMaster.Idol(2, "テストアイドル", Rarity.SSR)))` → `ShowIdol(2)` で `label.text == "テストアイドル"`。最後に `Object.DestroyImmediate(go)` |
| | | `Clear_SetsEmptyText` | `ShowIdol(2)` のあと `Clear()` → `label.text == PlaceholderCharacterStage.EmptyText`（"キャラ表示"） |
| | | `Constructor_Null_Throws` | `label` が null → `ArgumentNullException`（`ParamName == "label"`）、`master` が null → `ParamName == "master"` |
| 3 | `HomeEntryTests.cs` | `StartAsync_NoPlayer_GoesTitle` | `cache.Current == null` → `log` が `"GoToTitle"` だけ |
| | | `StartAsync_WithPlayer_PushesHome` | `Set` 済み → `"PushPage:HomePresenter"` だけ |
| 4 | `ApiActivityBridgeTests.cs` | `Started_BeginsCurrentBusy` | `UiRoot` を `AddComponent` し、`busyIndicator` に `AddComponent<BusyIndicator>()` を `SerializedObject` で入れる → `Attach` → `OnActivityStarted()` で `IsBusy == true`、`OnActivityEnded()` で false |
| | | `Ended_AfterSceneChange_EndsOriginal` | A で Started → B を Attach → Ended → A の `IsBusy == false`、B は Begin されていない |
| | | `NoScene_DoesNothing` | 何も Attach せずに Started・Ended → 例外なし |
| 4 | `ErrorDialogViewTests.cs` | `AutoDismiss_ReturnsFallback` | `autoDismiss: true` で 5 種類の `ErrorDialogButtons` → Ok・Title・Home・Title・Store |
| | | `NoSceneUi_ReturnsFallback` | `autoDismiss: false`、何も Attach しない → `RetryOrTitle` で `Title` |
| | | `RetryOrTitle_SecondClicked_ReturnsTitle` | `FakeDialogService`（`ChooseResults` に 1）を Attach → `Title`。`log` が `"Choose:エラーが発生しました。\n:再試行/タイトルへ"`（`error` は null。下の注） |
| | | `Dismissed_ReturnsFallback` | `ChooseResults` に −1 → `RetryOrTitle` で `Title` |
| 4 | `ErrorNavigatorTests.cs` | `GoToHomeAsync_ResolvesRefreshLazily_RefreshesThenGoesHome` | 下の「`ErrorNavigatorTests` の組み立て」。`StubPlayerPort`（既定の `Snapshot`）を登録したコンテナー。`PlayerCache.Current` が呼ぶ前は null → `await navigator.GoToHomeAsync()` → `container.Resolve<PlayerCache>().Current` が `port.Snapshot` と同じ（取り直した）、`log` が `"GoToHome"` だけ |
| | | `GoToHomeAsync_WhileRefreshing_SkipsRefresh` | `GatedPlayerPort`（下）を登録 → `var first = navigator.GoToHomeAsync();`（await しない）→ `port.SignInCalls == 1`、`log` が空 → `await navigator.GoToHomeAsync();`（取り直しを飛ばして遷移だけ）→ `log` が `"GoToHome"` だけ、`SignInCalls == 1` → `port.Gate.TrySetResult(PlayerSignInResult.Success(snapshot))` → `await first` → `log` が `"GoToHome"`, `"GoToHome"`、`SignInCalls == 1` → もう 1 回 `GoToHomeAsync()`（`Gate` は新しい完了済みの源に差し替える）→ `SignInCalls == 2`（`refreshing` が `finally` で戻る） |
| | | `GoToHomeAsync_RefreshFails_StillGoesHome` | `StubPlayerPort` の `SignInResults` に `PlayerSignInResult.Failure("x")` → `LogAssert.Expect(LogType.Warning, new Regex("refresh on go-to-home failed"))` → `await navigator.GoToHomeAsync()`（例外が出ない）→ `log` が `"GoToHome"` だけ、`PlayerCache.Current` は null のまま |
| | | `GoToHomeAsync_ResolveFails_StillGoesHome` | `RefreshPlayerUseCase` を登録しないコンテナー（`new ContainerBuilder().Build()`。`ErrorNavigator` の作成は例外にならない＝コンストラクターで解決しない）→ `LogAssert.Expect(LogType.Warning, new Regex("refresh on go-to-home failed"))` → `await navigator.GoToHomeAsync()`（解決の例外も同じ `catch` に入る）→ `log` が `"GoToHome"` だけ |
| 4 | `BootstrapEntryTests.cs` | `ShowFailureDialog_DestroyedRoot_ReturnsFalse` | `var root = new GameObject("Root").AddComponent<UiRoot>(); Object.DestroyImmediate(root.gameObject);` → `BootstrapEntry.ShowFailureDialog(root, "m") == false`、例外なし（`MissingReferenceException` が出ない。3.8 手順 5） |
| | | `ShowFailureDialog_Null_ReturnsFalse` | `BootstrapEntry.ShowFailureDialog(null, "m") == false` |

**`ErrorNavigatorTests` の組み立て**: どれも `[UnityTest] public IEnumerator Xxx() => UniTask.ToCoroutine(async () => { ... });`。`var log = new CallLog();`。`var builder = new ContainerBuilder(); builder.RegisterInstance<IPlayerPort>(port); builder.Register<PlayerCache>(Lifetime.Singleton); builder.Register<RefreshPlayerUseCase>(Lifetime.Singleton); var container = builder.Build();`（`ContainerBuilder`・`Lifetime` は `VContainer`。テストの asmdef は 3.1 で `VContainer` を参照済み）。`var navigator = new ErrorNavigator(new FakeSceneNavigator(log), container);`（`IObjectResolver` は `container`。`using ISceneNavigator` の衝突は無い: `UnityScreenNavigator` を `using` しない）。`[TearDown]` で `container.Dispose()`。遷移の記録は `FakeSceneNavigator`（3.3.8）の `"GoToHome"` で確かめる。`GatedPlayerPort` はこのテストファイルの中の `private sealed class GatedPlayerPort : IPlayerPort`: `public UniTaskCompletionSource<PlayerSignInResult> Gate { get; set; } = new UniTaskCompletionSource<PlayerSignInResult>();`、`public int SignInCalls { get; private set; }`、`SignInAsync()` は `SignInCalls++` して `Gate.Task` を返す、`SignUpAsync()` は `UniTask.FromResult(PlayerSignUpResult.Success(true))`。`snapshot` は `new StubPlayerPort().Snapshot`（既定のスナップショット）を使う

**`DialogServiceTests` の書き方**: どれも同期の `[Test]`（`[UnityTest]` にしない）。`[SetUp]` で層 `layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>()`、prefab = `AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Parts/Dialog.prefab").GetComponent<DialogView>()`、`service = new DialogService(layer, prefab)`。`[TearDown]` で `if (layer != null) Object.DestroyImmediate(layer.gameObject);`（`LayerDestroyed_ReturnsWithoutDialog` は先に消すため）。`ConfirmAsync` などを呼んだ戻り値の `UniTask` は **await せずに変数に受け**、そのまま同期で層の子（`layer.childCount`）とボタン（`layer.GetComponentsInChildren<Button>()`。並びは上から）を確かめる（待ち行列が空なら、ダイアログは呼んだその場で出る。3.2）。ボタンは `onClick.Invoke()` で押す（EditMode では `UiObjects.Destroy` が `DestroyImmediate` なので、押した直後に子が減る）。完了は、押したあとに `task.Status == UniTaskStatus.Succeeded` を確かめ、結果は `task.GetAwaiter().GetResult()` で読む（1 つの `UniTask` で 1 回だけ）。押す前に `Status` が `Pending` でなければ、または押したあとに次のダイアログが同期で出なければ、`UniTask.Yield` などを足して直さず、止まって報告する（3.2 の待ち行列の前提が崩れている）

**ホームのボタンのテストの差し替え**（決めたこと 16）: 2〜4 本目は、自分が行き先を変えたボタンのテスト（上の表の「差し替える」と書いた行）だけを、自分の処理を確かめるテストに置き換える。ほかのボタンのテストは触らない（「準備中です。」が全部で何回出るかを数えるテストは作らない）。

`ApiError` は `sealed class` でコンストラクターが `internal` なので、テストから作れない。`ErrorDialogViewTests` の `ShowAsync` には `null` を渡す（`ErrorDialogView` は `error` を文字列の連結とログにしか使わない）。記録は `"Choose:エラーが発生しました。\n:再試行/タイトルへ"` と一致すること。

Play モード・手動（Editor、サーバー起動済み）:
- 初回: 起動 → タイトル（「タップしてスタート」）→ タップ → 「接続中…」→ ホーム。プレイヤーLv とガチャ通貨が出る。2 回目（再起動）も同じ
- ホームの主ボタン（「プロデュース」。進行中は「再開」の表示で、帯「プロデュース中」も出る）と、下部のナビの「プロデュース」「ガチャ」「アイドル」を押すと「準備中です。」が出て、OK で閉じる
- ホームの中央のステージ（色つきの枠）に、所持の育成アイドルのうちレア度が最も高いものの名前が出る（初回の初期付与に育成アイドルが無ければ「キャラ表示」のまま）
- 通信中（サインインの間）は入力が効かない（`ApiActivityBridge`。サーバーを遅くする手段がなければ、確かめていないと報告する）
- サーバーを止めてタップ → タイトルに「接続に失敗しました: …」が出て、もう一度タップできる（端末の認証は `ApiCore` を通らないので、共通エラーのダイアログは出ない）。サーバーを起動してからタップでホームに進む
- 画面を縦長にしたとき、背景が全面に出てボタンが safe area の中にある。日本語が TMP（Noto Sans JP）で出る
- Windows のウィンドウの初期サイズ（540×960）と比率補正（`WindowAspectKeeper`）はスタンドアロンのビルドでだけ確かめる（タスク 5。Editor では効かない）。確かめられなければ、そう報告する
- Android・iOS の実機での表示（safe area）は順 7 の別項目（実機ビルド）で確かめる。この計画では未確認

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

- なし（2026-11-07: ユーザーの確認は決めたこと 3・6・12 に移した。P2 の `TitlePage`・`HomePage` の表（3.1・3.3）と 3.9 は突き合わせ済みで一致）

## 7. 実装メモ

| タスク | 状態 | コミット | 計画とのズレ・確認できていないこと |
|---|---|---|---|
| 1 | 完了 | `63d3742` | `FakePlayerUseCase` をタスク 2 に移した（計画の書き漏れ。R5 を直した）。`DialogOpenerTests` は `UnityEditor.DialogResult` と曖昧になるので別名 `using` |
