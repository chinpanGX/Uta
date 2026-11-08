---
title: 実装計画: client/ の画面 4/6 編成・プロデュースの開始と再開
status: ready
updated: 2026-11-08
spec: ../wiki/systems/produce.md, ../wiki/systems/idol-support.md
---

# 実装計画: client/ の画面 4/6 編成・プロデュースの開始と再開

## ADR 0020〜0022・naming.md への読み替え（案。実装は順 0（基盤移行の計画）の PR で ADR 0020〜0022 の accepted が決まってから。この節が本文と食い違ったら、この節が正）

2026-11-08。画面の計画に共通の読み替え（対応表）は [foundation の計画](2026-11-03-client-screens-foundation.md)の同名の節の R0 が正。この節は formation に固有の読み替えだけを書く。前提: foundation・gacha・owned の読み替えが実装済み（`DialogOpener`・`HomeDtoFactory`・`FeaturedIdolPicker`・`DiscardNoticeState`・`FakePlayerUseCase`・`TestMasterData`、`ServerReasons`（gacha が作っていれば）、`OwnedListFactory`・`DisplayText`）。gacha・owned の読み替えの節はまだ無い（次のセッションが足す）。**この計画の実装に入る前に、owned の読み替えが実装済みであることを確かめる**: 選択モードの要求の型（owned が `Request` を外した名前に改名している。実在の名前に合わせる）に `ForSupportCard(int idolId, IReadOnlyCollection<string> excludedUids)` と `IdolId`・`ExcludedUids` のプロパティがあり、`IdolPlan`・`ExcludedSupportCardIds` が無いこと（R4）。無ければ止まって報告する。「基盤 11d9b06 への読み替え」の節との優先は [索引](2026-11-07-client-screens-index.md) の読み替えの節。ADR は [0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)・[0021](../wiki/decisions/0021-domain-entities-and-repositories.md)・[0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)、命名は [naming.md](../wiki/architecture/naming.md)。

### R1. 型の名前

| 本文 | 読み替え | 根拠 |
|---|---|---|
| `ProduceProgress` | `ProduceStateResult`（`GetStateAsync` の戻り値。サーバーの `ProduceView` の新しい名前と同じ: 1章「同じ役割には同じ名前」。`ProduceSnapshot` は Shared.Domain の保存用の写しと同名なので使わない。ユーザーの判断 2026-11-08） | naming.md 1・2章 |
| `ProduceOutcome` | `ProduceEndResult` | naming.md 1・2章 |
| `ProduceEndingKind` | `ProduceEndKind`（値は同じ。Contracts の `ProduceEnding` と同名にしない） | naming.md 1章 |
| `CardOfferAction`・`LessonActionType` | **作らない**（R2 でメソッドを操作ごとに分ける） | naming.md 1章 |
| `ProduceStartStatus`・`ProduceResumeStatus`・`ProduceStepStatus` | `ProduceStartResultKind`・`ProduceResumeResultKind`・`ProduceStepResultKind`（値・意味は同じ） | naming.md 2章 |
| `ProduceSession` | `ProduceSessionState`（Application の状態の型。中身は同じ: `Current`・`Previous`・`ExamResultShown`・`HasActive`・`Set`・`Clear`） | naming.md 2章、ADR 0021 |
| `FormationSlotViewData` | `FormationSlotRowViewDto`（R4） | naming.md 6章 |
| `ProduceDeckCard`・`ProduceWeekAction`・`ProduceClassOption`・`ProduceExamTurn`・`ProduceParamBonus`・`ProducePersistent`・`ProduceLesson`・`ProduceExamResult` | そのまま（Application の UseCase の出力の型。`ProduceLesson.UsableHand` の扱いも本文のまま） | — |
| サーバーの `ProduceView`・`ProduceViewBuilder`・`ProduceEndSummary`、Shared.Domain の `ClassOptionView`（タスク 1 で触る） | **タスク 1 で改名する**（naming.md「今のコードとの差」。条件なし）: `ProduceView` → `ProduceStateResult`、`ProduceViewBuilder` → `ProduceStateResultFactory`、`ProduceEndSummary` → `ProduceEndResult`（どれも `server/Uta.Server/Application/Produce/` のファイル名も）、`ClassOptionView` → `ClassOption`（`shared/Uta.Shared.Domain/Produce/ProduceEngine.cs` の `GetClassOptions` の戻り値）、Shared.Domain の `ExamOutcome`（enum）→ `ExamResultKind` と、プロパティ `ProduceResult.Outcome`（Shared.Domain）・`ProduceResultResponse.ExamOutcome`（Contracts。MessagePack はキーの番号なので通信に影響しない）→ `ResultKind`・`ExamResultKind`（この計画が `ProduceEndResult` で `ExamOutcome` を使うため。naming.md「今のコードとの差」。DB の列名 `exam_outcome` は変えない）。使っている場所は `server/Uta.Server/` の `Api/ProduceResponseMapper.cs`・`Application/Produce/AbandonProduceUseCase.cs`・`GetProduceStateUseCase.cs`・`ProduceCompletion.cs`・`ProduceOperationUseCase.cs`・`StartProduceUseCase.cs`・`Program.cs`、`server/Uta.Server.Tests/`、`shared/Uta.Shared.Domain.Tests/`（Grep `ProduceView\b\|ProduceViewBuilder\|ProduceEndSummary\|ClassOptionView` で確かめる）。旧い名前を書いている wiki（`produce-server.md`・`produce-domain.md`・`screen-design.md` 7.6 など。Grep）も同じタスクで直す。本文 3.1 とタスク 1 の手順 3 の `ProduceView`・`ProduceViewBuilder` は新しい名前で読む | naming.md 1・7章 |

### R2. Application: `IProduceUseCase`（`IProducePort`・`StartProduceUseCase`・`ResumeProduceUseCase`・`AbandonProduceUseCase`・`ProduceActions`・`ProduceRefresh` の代わり）

`Application/Produce/IProduceUseCase.cs`・`ProduceSessionState.cs`・`ProduceStartResultKind.cs`・`ProduceResumeResultKind.cs`・`ProduceStepResultKind.cs`・`ProduceStateResult.cs`・`ProduceEndResult.cs`・`ProduceEndKind.cs`・`FormationNormalizer.cs`・`ScenarioResolver.cs`（1 ファイル 1 型）。2 章の表の `IProducePort.cs`・`ProduceSession.cs`・`ProduceRefresh.cs`・`ProduceStartStatus.cs`・`StartProduceUseCase.cs`・`ProduceResumeStatus.cs`・`ResumeProduceUseCase.cs`・`AbandonProduceUseCase.cs`・`ProduceStepStatus.cs`・`ProduceActions.cs`・`ProduceEndingKind.cs`・`ProduceOutcome.cs`・`ProduceProgress.cs`・`CardOfferAction.cs`・`LessonActionType.cs` の行はこの名前に読み替える。

```csharp
public interface IProduceUseCase   // Contracts の IProduceService と同じ単位。メソッドは Presenter の操作の単位（ADR 0022）
{
    UniTask<ProduceStartResultKind> StartAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId);
    UniTask<ProduceResumeResultKind> ResumeAsync();
    /// <summary>セッションが無くなったら true（放棄した、またはもう無かった）。残っていれば false。</summary>
    UniTask<bool> AbandonAsync();
    // 1 手の操作（本文の ProduceActions の 4 つを操作ごとに分ける。操作番号は session.Current.OperationNumber を具象が入れる）
    UniTask<ProduceStepResultKind> ChooseWeekActionAsync(int slotIndex, int optionIndex, ParamKind param);
    UniTask<ProduceStepResultKind> PickCardOfferAsync(int offerIndex);    // ResolveCardOfferRequest(op, CardOfferActionKind.Pick, offerIndex)
    UniTask<ProduceStepResultKind> DeclineCardOfferAsync();              // ResolveCardOfferRequest(op, CardOfferActionKind.Decline, 0)
    UniTask<ProduceStepResultKind> DecideExamRetryAsync(bool retry);
    UniTask<ProduceStepResultKind> UseCardAsync(int cardInstanceId);     // ExecuteLessonActionRequest(op, LessonActionKind.UseCard, cardInstanceId, 0)
    UniTask<ProduceStepResultKind> UseDrinkAsync(int drinkSlotIndex);    // ExecuteLessonActionRequest(op, LessonActionKind.UseDrink, 0, drinkSlotIndex)
    UniTask<ProduceStepResultKind> SkipLessonTurnAsync();                // ExecuteLessonActionRequest(op, LessonActionKind.Skip, 0, 0)
    // 状態の型への書き込み（lesson の画面が使う。ADR 0022「状態の型への書き込みも具象が行う」。ユーザーの判断 2026-11-08）
    void MarkExamResultShown();   // session.ExamResultShown = true（試験結果の画面を出した印。lesson 3.4.3）
    void ClearSession();          // session.Clear()（プロデュース結果の「ホームへ」。lesson 3.4.4。通信しない）
}
```

- `CardOfferActionKind` の `Reroll`・`Remove` は MVP の画面（week）が使わないので IF に置かない（使う計画が足す）
- `MarkExamResultShown`・`ClearSession` はこの計画では呼ぶ側が無い（lesson が使う）。具象に実装し、`ProduceUseCaseTests` に `MarkExamResultShown_SetsFlag`・`ClearSession_ClearsSession` を足す
- 動き（成功・業務エラーの分岐。本文 3.2「ユースケースの動き」）はそのままで、次を読み替える: `port.XxxAsync(...)` → `IProduceService` の呼び出しと `ProduceMapper` での写し（R3）、`r.IsBusinessError` と `r.Reason == ServerReasons.Xxx` → `result.IsError && result.ErrorCode == ProduceErrors.Xxx`、`PortResult.Failure` → `handledCodes` に無い失敗、`ProduceRefresh.TryAsync(refresh)` → 具象の private `TryRefreshAsync()`（`await player.RefreshAsync()` を `try` で包み `InvalidOperationException` を握りつぶす。成功で true）。`RunAsync(Func<int, …>)` に 1 つにまとめる書き方は、7 つの操作で同じ
- `ProduceSessionState` への `Set`・`Clear` は具象 `ProduceUseCase` が行う（ADR 0022）
- **`ServerReasons` にプロデュースの 7 定数は足さない**（結果が enum なので Presenter が reason を見ない。本文 3.2 の先頭の段落と、2 章の `ServerReasons.cs`・`ServerReasonsTests.cs` の行は無効）
- `FormationNormalizer`（外部に触れない Application の具象。ADR 0022「編成のプランの判定」）:
  ```csharp
  public sealed class FormationNormalizer
  {
      public FormationNormalizer(IReadOnlySupportCardRepository supportCards, MemoryDatabase db);
      /// <summary>
      /// slotUids（長さは FormationPresenter.SupportSlots）を順に見て、所持に無い uid（supportCards.Find(uid) == null）と、
      /// idolId != 0 でプランが合わない uid（!FormationValidator.IsPlanCompatible(idolPlan, cardPlan)。idolPlan は (Plan)(int)db.IdolTable.FindByIdolId(idolId).Plan、
      /// cardPlan は (Plan)(int)db.SupportCardTable.FindBySupportCardId(card.CardId).Plan。Plan は Shared.Domain の enum）を null にした新しい配列を返す。
      /// removed は null にした数。slotUids が null なら ArgumentNullException。
      /// </summary>
      public string[] RemoveIncompatible(int idolId, IReadOnlyList<string> slotUids, out int removed);
  }
  ```
- `ScenarioResolver`（外部に触れない Application の具象。naming.md 9章の `Resolver`「ID などから対象を引き当てる」。シナリオの選択は開始の業務の入力なので DtoFactory に置かない）:
  ```csharp
  public sealed class ScenarioResolver
  {
      public ScenarioResolver(MemoryDatabase db);   // db.ScenarioTable.All を ScenarioId の昇順に並べたものを持つ（以下「シナリオの並び」）
      public int Count { get; }                     // シナリオの数
      public int IdAt(int index);                   // シナリオの並びの index の ScenarioId。範囲外なら ArgumentOutOfRangeException
      public string NameAt(int index);              // 同じく Name（FormationDtoFactory が使う）
  }
  ```

### R3. Infrastructure

```csharp
// Infrastructure/Produce/ProduceUseCase.cs
public sealed class ProduceUseCase : IProduceUseCase
{
    public ProduceUseCase(IProduceService produce, ProduceSessionState session, IPlayerUseCase player);
    // handledCodes は本文 3.3 の表のとおり private static readonly ErrorCode[] で持つ（4 つの操作の列は 7 つの操作で同じ）
}

// Infrastructure/Produce/ProduceMapper.cs（static。本文の ProduceGateway.ToProgress・ToOutcome。規則は本文 3.3 のまま）
public static class ProduceMapper
{
    public static ProduceStateResult ToStateResult(ProduceStateResponse response);
    public static ProduceEndResult ToEndResult(ProduceResultResponse response);   // ProduceEnding → ProduceEndKind は (ProduceEndKind)(int)
}
```

- `ProduceApi`・`ProduceGateway` は作らない（2 章の `Api/ProduceApi.cs`・`Produce/ProduceGateway.cs` の行は無効）。`ApiClient` に `public IProduceService Produce { get; }` を足し、`BootstrapEntry` に `builder.Register<IProduceService>(resolver => resolver.Resolve<ApiClient>().Produce, Lifetime.Singleton);`（「基盤 11d9b06 への読み替え」の節のとおり。`IPlayerService` の登録の次。**タスク 5 の DI の行と一緒に足す**。忘れると EditMode では分からず、タスク 6 の Play で VContainer の解決の例外になる）。呼び方は `await produce.WithHandledCodes(StartHandled).StartAsync(req).ToResult()` の形
- `MagicOnionClientInitializer` に `typeof(IProduceService)` を足すのは本文のまま

### R4. Presentation

- `ProduceFlow(ProduceSessionState session, IScreenNavigator navigator, PageStack pageStack, DialogOpener dialogs, ISceneNavigator scenes)`、`ProduceMenu(DialogOpener dialogs, IProduceUseCase produce, ProduceFlow flow)`（`abandon.ExecuteAsync()` → `produce.AbandonAsync()`）、`ProduceEntry(IScreenNavigator navigator, ProduceSessionState session, ProduceFlow flow, DialogOpener dialogs, ISceneNavigator scenes)`。中身は本文のまま
- 背景のページ: `ProduceBackdropViewDto(string header)` と `ProduceBackdropDtoFactory`（`Presentation/Produce/`。依存なし。`ProduceBackdropViewDto Create(ProduceStateResult p)` の文字列は本文の `BuildHeader` のとおり。`p` が null なら `ArgumentNullException`）。`ProduceBackdropPresenter([Inject] ProduceBackdropPage view, ProduceSessionState session, ProduceBackdropDtoFactory dtoFactory)`（テスト用も同じ並びで `IProduceBackdropView`）。`Refresh()` は、`session.Current` が null なら何もしない（本文のとおり）、そうでなければ `view.SetHeader(dtoFactory.Create(session.Current).Header)`。static の `BuildHeader` は作らない
- 編成（3.5）:
  ```csharp
  public sealed class FormationSlotRowViewDto { public FormationSlotRowViewDto(string label, bool filled); }
  public sealed class FormationViewDto
  {
      public FormationViewDto(string idolLabel, IReadOnlyList<FormationSlotRowViewDto> slots, string scenarioLabel, bool canChangeScenario, bool canStart);
  }
  public interface IFormationView
  {
      void Set(FormationViewDto dto);   // 本文の SetIdol・SetSlots・SetScenario・SetStartEnabled を 1 つに。FormationPage は 4 つの参照に順に入れる（slots の数の検査は本文の SetSlots のまま）
      // イベント 6 つは本文のまま
  }
  // Presentation/Formation/FormationDtoFactory.cs（ルートのスコープにシングルトン）
  public sealed class FormationDtoFactory
  {
      public FormationDtoFactory(IReadOnlySupportCardRepository supportCards, ScenarioResolver scenarios, MemoryDatabase db);
      /// <summary>
      /// 本文 3.5 の Refresh() の文言をそのまま作る: idolLabel は idolId == 0 なら "育成アイドル: 未選択"、そうでなければ db.IdolTable.FindByIdolId(idolId) の名前・DisplayText.RarityName・PlanName（マスタの enum は (Shared.Domain の型)(int) で変換）。
      /// slots は長さ FormationPresenter.SupportSlots の uid（null は空）。supportCards.Find(uid) が null の枠は空として出す（枠を null に直すのは FormationNormalizer）。見つかれば db.SupportCardTable.FindBySupportCardId(card.CardId) の名前・レア度と card.Level。
      /// scenarioLabel は scenarios.Count == 0 なら "シナリオ: なし"、そうでなければ "シナリオ: " + scenarios.NameAt(scenarioIndex)。canChangeScenario は scenarios.Count >= 2。canStart は idolId != 0 && scenarios.Count > 0。
      /// </summary>
      public FormationViewDto Create(int idolId, IReadOnlyList<string> slotUids, int scenarioIndex);
  }
  ```
  - `FormationPresenter`:
    ```csharp
    [Inject] public FormationPresenter(FormationPage view, FormationDtoFactory dtoFactory, FormationNormalizer normalizer, FeaturedIdolPicker picker, ScenarioResolver scenarios,
        IProduceUseCase produce, DialogOpener dialogs, IScreenNavigator navigator, ISceneNavigator scenes);
    public FormationPresenter(IFormationView view, FormationDtoFactory dtoFactory, FormationNormalizer normalizer, FeaturedIdolPicker picker, ScenarioResolver scenarios,
        IProduceUseCase produce, DialogOpener dialogs, IScreenNavigator navigator, ISceneNavigator scenes);
    ```
    - 状態（`idolId`・`slots`・`scenarioIndex`・`running`・`disposed`）は本文のまま Presenter が持つ（画面の中だけの UI の状態）。`Refresh()` は `view.Set(dtoFactory.Create(idolId, slots, scenarioIndex))` だけ
    - `InitializeAsync`: 既定の `idolId` は `picker.Pick()`
    - `SelectIdolAsync` の 4: `slots = normalizer.RemoveIncompatible(idolId, slots, out var removed);`。5: `Refresh()` のあと `removed > 0` なら `ShowInfoAsync("プランが合わないサポートカードを外しました。")`
    - `SelectSlotAsync` の 3・4: 選択モードの要求は `idolId` と `excludedUids`（枠 `index` 以外の埋まっている uid。枠の順）だけで作る。**owned への申し送り**（owned の読み替えで決め、owned が実装する。この計画はそれが実装済みであることを前提にし、無ければ止まって報告する）: 本文の `OwnedSelectionRequest.ForSupportCard(Plan idolPlan, IReadOnlyCollection<string> excludedUids, IReadOnlyCollection<int> excludedSupportCardIds)` を `ForSupportCard(int idolId, IReadOnlyCollection<string> excludedUids)` にし、プロパティは `IdolPlan`・`ExcludedSupportCardIds` を消して `IdolId` を足す。プランの判定と、除外する `CardId`（`excludedUids` の所持から引く。所持に無い uid は無視する）は owned の `OwnedListFactory` がリポジトリとマスタから引く（Presenter がマスタ・リポジトリを読まないため）。owned のテスト（`OwnedSelectionRequestTests`・`OwnedListFactoryTests` の除外のケース）もそれに合わせる。型の名前（`Request` を外す）は owned の読み替えで決める
    - `NextScenario`・`StartAsync` の `master.Scenarios.Count` は `scenarios.Count`、`master.Scenarios[scenarioIndex].ScenarioId` は `scenarios.IdAt(scenarioIndex)`。`StartAsync` は `var kind = await produce.StartAsync(idolId, uids, scenarios.IdAt(scenarioIndex));` で、分岐は `ProduceStartResultKind` の同じ値
- ホーム（3.6）:
  - `HomeDtoFactory`（foundation）のコンストラクターの**末尾**に `ProduceSessionState session` を足し（`HomeDtoFactory(IReadOnlyPlayerRepository players, FeaturedIdolPicker picker, DiscardNoticeState notice, MemoryDatabase db, ProduceSessionState session)`）、`resumeVisible` を `session.Current != null ? session.HasActive : p.HasActiveProduce` にする（本文の `HasActiveProduce()`）。`HomeDtoFactoryTests` の既存の組み立てに `new ProduceSessionState()` を足し、`Create_ActiveSession_ResumeEvenIfPlayerInactive`・`Create_FinishedSession_HidesResume` を足す（タスク 5）
  - `HomePresenter` のコンストラクター（2 本）の最後（gacha が足した `IScreenNavigator navigator` の後ろ）に `IProduceUseCase produce, ISceneNavigator scenes` を足す（`ProduceSession`・`ResumeProduceUseCase`・`AbandonProduceUseCase` は受けない）。足したあとの形: `[Inject] HomePresenter(HomePage view, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs, IScreenNavigator navigator, IProduceUseCase produce, ISceneNavigator scenes) : this((IHomeView)view, view.CreateStage(), dtoFactory, player, dialogs, navigator, produce, scenes)` と、テスト用の `HomePresenter(IHomeView view, ICharacterStage stage, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs, IScreenNavigator navigator, IProduceUseCase produce, ISceneNavigator scenes)`（gacha が `IGachaUseCase` などを足していれば、その後ろ。実在の並びに合わせる）
  - `OpenProduceAsync`: 進行中かは `dtoFactory.Create()` の `ResumeVisible`（null なら何もしない）。`ResumeCoreAsync` は `var kind = await produce.ResumeAsync();`、放棄は `await produce.AbandonAsync();`。ほかは本文のまま
- DI（タスク 5 の 6 行の代わり。`BootstrapEntry.RegisterUseCases` の最後）:
  ```csharp
  builder.Register<ProduceSessionState>(Lifetime.Singleton);
  builder.Register<ProduceUseCase>(Lifetime.Singleton).As<IProduceUseCase>();
  builder.Register<ScenarioResolver>(Lifetime.Singleton);
  builder.Register<FormationNormalizer>(Lifetime.Singleton);
  builder.Register<FormationDtoFactory>(Lifetime.Singleton);
  builder.Register<ProduceBackdropDtoFactory>(Lifetime.Singleton);
  ```
  `using` は `Uta.Client.Application.Produce; Uta.Client.Infrastructure.Produce; Uta.Client.Presentation.Formation; Uta.Client.Presentation.Produce;`

### R5. テスト

- `Tests/EditMode/Fakes/FakeProduceService.cs`（`IProduceService` の偽。`ProduceUseCaseTests` が使う）: `Queue<Func<UnaryResult<ProduceStateResponse>>> StateResults`（Start・GetState・7 つの操作で共有。メソッドは `Dequeue()()` をそのまま返す。空なら `InvalidOperationException("FakeProduceService: no state result")`）、`Queue<Func<UnaryResult<ProduceResultResponse>>> AbandonResults`（空なら `"FakeProduceService: no abandon result"`）。積み方の補助は移行計画の `FakePlayerService` と同じ static の `Ok<T>(T)`・`Fail<T>(ErrorCode)`（`Fail` は `FakeRpcErrors.ApiFailureResult<T>(code)`。基盤の `FakeRpcErrors` が使えないときの扱いも移行計画 R5 と同じ）。`CallLog` への記録は本文 3.7 の `FakeProducePort` と同じ文字列（`"ResolveCardOffer:op,kind,index"`・`"ExecuteLessonAction:op,kind,card,drink"` の `kind` は Contracts の enum の名前）。`IService<IProduceService>` の 5 メンバーは `WithHeaders` が `this`
- `Tests/EditMode/Fakes/FakeProduceUseCase.cs`（Presenter・`ProduceFlow`・`ProduceMenu` のテスト用。week・lesson も使う）: 4 つの `Queue` の要素は **`Func<UniTask<T>>`**（`Queue<Func<UniTask<ProduceStartResultKind>>> StartResults`、`Queue<Func<UniTask<ProduceResumeResultKind>>> ResumeResults`、`Queue<Func<UniTask<bool>>> AbandonResults`、`Queue<Func<UniTask<ProduceStepResultKind>>> StepResults`（7 つの操作で共有））。メソッドは記録してから `Dequeue()()` を返す。空なら `StartResults` は `InvalidOperationException("FakeProduceUseCase: no start result")`、`ResumeResults` は `Resumed`、`AbandonResults` は true、`StepResults` は `Succeeded`。**偽そのものはセッション・リポジトリに書かない**が、テストは本物の具象が行う副作用（`session.Set`・`session.Clear`・`players.Upsert`）を、積む `Func` の中に書いて再現する（例: `AbandonResults.Enqueue(() => { session.Clear(); players.Upsert(inactive); return UniTask.FromResult(true); })`）。例外は `() => throw new InvalidOperationException("FakeProduceUseCase: no state result")` を積む（week・lesson の例外のテストの期待 `"…: FakeProducePort: no state result"` は `"…: FakeProduceUseCase: no state result"` と読み替える。`LogAssert.Expect` の `Regex` も同じ）。`MarkExamResultShown`・`ClearSession` は、コンストラクターの第 2 引数 `ProduceSessionState session = null` が渡されていればそれに書き（`session.ExamResultShown = true`・`session.Clear()`）、記録は `"MarkExamResultShown"`・`"ClearSession"`。記録: `"Start:" + idolId + ":" + string.Join(",", uids) + ":" + scenarioId`・`"Resume"`・`"Abandon"`・`"ChooseWeekAction:slot,option,param"`・`"PickCardOffer:index"`・`"DeclineCardOffer"`・`"DecideExamRetry:retry"`・`"UseCard:id"`・`"UseDrink:slot"`・`"SkipLessonTurn"`
- `FakePlayerPort` は作らない。foundation の `FakePlayerUseCase`（`new FakePlayerUseCase(log)`。記録 `"Refresh"`）を使う。本文の `"SignIn"` の記録は `"Refresh"` と読む（`"GetState"` → 本物の `ProduceUseCase` のテストでは `"GetState"` のまま、Presenter のテストでは `"Resume"`）
- `TestProduce`: `Progress(...)` → `StateResult(...)`（戻りは `ProduceStateResult`。引数は同じ）、`Abandoned()` は `ProduceEndResult`、`Player(...)` は `PlayerEntity(1, 0, 0, 0, gachaCurrency, hasActiveProduce, false)` を返す（所持の一覧は本物の `IdolRepository`・`SupportCardRepository` にテストが `ReplaceAll` で入れる）。足すもの: `StateResponse(int operationNumber = 0, ProducePhase phase = ProducePhase.WeekSelect, int scenarioId = 1, int idolId = 1, int week = 1, int singing = 100, int rhythm = 100, int expression = 100, int hp = 30, int maxHp = 30, int examRetryLeft = 1, DeckCardDto[] deck = null, WeekActionDto[] weekActions = null, ClassOptionDto[] classOptions = null, int[] cardOffers = null, LessonDto lesson = null, ExamResultDto examResult = null, ProduceResultResponse result = null)`（`new ProduceStateResponse(...)` のコンストラクターの並びに渡す。並びと DTO の名前は `shared/Uta.Shared.Contracts/Produce/ProduceStateResponse.cs` のとおりで、違えばそちらに合わせる。null の配列は空の配列）と `ResultResponse(ProduceEnding ending = ProduceEnding.Abandoned, int playerLevel = 1)`（ほかの引数は 0・`ExamResultKind.Nothing`・`ProduceRank` の最初の値）。`ProduceUseCaseTests` はこれで応答を作る
- 5 章の表の読み替え:
  - `StartProduceUseCaseTests`・`ResumeProduceUseCaseTests`・`AbandonProduceUseCaseTests`・`ProduceActionsTests`・`ProduceGatewayTests` → **`ProduceUseCaseTests`**（`FakeProduceService` + `new FakePlayerUseCase(log)` + 本物の `ProduceSessionState`。本文の 5 ファイルのケースを全部移す。`"SignIn"` → `"Refresh"`、`Status` → `ResultKind`、「`session.Current` が戻り値」は「`session.Current.OperationNumber`・`Phase` が積んだ応答の値」、`Operation_Finished_RefreshesPlayer` の「`PlayerCache.Current` が差し替わる」は「`"Refresh"` が記録される」）と **`ProduceMapperTests`**（`ToProgress_*` → `ToStateResult_*`、`ToOutcome_MapsEnding` → `ToEndResult_MapsEndKind`）。`AllOperations_MapToPort` は 7 つの操作で `"ChooseWeekAction:…"`・`"ResolveCardOffer:0,Pick,2"`・`"ResolveCardOffer:0,Decline,0"`・`"DecideExamRetry:0,True"`・`"ExecuteLessonAction:0,UseCard,7,0"`・`"ExecuteLessonAction:0,UseDrink,0,1"`・`"ExecuteLessonAction:0,Skip,0,0"`
  - `ProduceSessionTests` → `ProduceSessionStateTests`、`ProduceProgressTests` → `ProduceStateResultTests`
  - `ProduceFlowTests`・`ProduceEntryTests`: `DialogService` → `DialogOpener`、型の名前の読み替えだけ
  - `ProduceMenuTests`: `FakeProduceUseCase`。`Show_AbandonConfirmed_AbandonsThenExits` は `"Confirm:…"`, `"Abandon"`, `"GoToHome"`（`"SignIn"` は無い。取り直しは本物の具象の中）
  - `ProduceBackdropPresenterTests`: `Initialize_SetsHeader` の期待は `"SetHeader:" + new ProduceBackdropDtoFactory().Create(...).Header`。文字列そのものの確認は `ProduceBackdropDtoFactoryTests`（新）の `Create_FormatsHeader`（週 3、100/110/120、体力 25 / 30）
  - `FormationPresenterTests`: `FakeGameMaster` → `TestMasterData.Build(b => { b.Append(new[] { TestMasterData.Idol(1, "…", Rarity.SR) /* Plan は Sense */, ... }); b.Append(new[] { new SupportCard { SupportCardId = …, Name = …, Rarity = …, Plan = … }, ... }); b.Append(new[] { new Scenario { ScenarioId = 1, Name = … }, new Scenario { ScenarioId = 2, Name = … } }); })`（`Idol.Plan` を Logic にするテストは `TestMasterData.Idol` の戻りを `with` せず、`new Idol { … }` で書く）。所持は本物の `IdolRepository`・`SupportCardRepository`、`FakeProducePort` → `FakeProduceUseCase`、`var scenarios = new ScenarioResolver(db);`・`new FormationDtoFactory(supportCards, scenarios, db)`・`new FormationNormalizer(supportCards, db)`・`new FeaturedIdolPicker(idols, db)`。偽の View は `Set(dto)` を `"Set:" + dto.IdolLabel + "|" + string.Join("/", dto.Slots.Select(s => s.Label)) + "|" + dto.ScenarioLabel + "," + dto.CanChangeScenario + "," + dto.CanStart` と記録し、最後の dto を `LastDto` に持つ。「`SetIdol` が増えない」は「`"Set:"` の行が増えない」。`SelectSlot_BuildsRequestWithExclusions` の確かめは要求の `IdolId` と `ExcludedUids`（R4 の申し送り。`IdolPlan` は無い）。`Start_Exception_ShowsError` の例外の文は `"FakeProduceUseCase: no start result"`。`Start_AlreadyInProgress_ShowsInfoAndPops` に `"SignIn"` は無い
  - `FormationNormalizerTests`（新。タスク 2）: `RemoveIncompatible_ClearsIncompatiblePlan`、`RemoveIncompatible_ClearsUnknownUid`、`RemoveIncompatible_CountsRemoved`、`RemoveIncompatible_NoIdol_ClearsOnlyUnknown`
  - `FormationDtoFactoryTests`（新。タスク 4）: 本文の `Initialize_DefaultsToHighestRarityIdol`・`Initialize_NoIdols_StartDisabled`・`Initialize_ShowsEmptySlotsAndScenario` の文言の期待をここに移す（`Create(2, 空の 5 枠, 0)` など）。`FormationPresenterTests` の同名のテストは `"Set:"` の記録の有無と `LastDto.CanStart` だけを見る
  - `HomePresenterTests`: 新しい引数は `FakeProduceUseCase`（同じ `CallLog`）と `FakeSceneNavigator`。`"GetState"` → `"Resume"`、`"SignIn"` は無い。本物の具象が行う副作用は、積む `Func` の中で再現する: `OpenProduce_Active_AbandonConfirmed_HidesResume` は `players.Upsert(hasActiveProduce = true)` で始め、`ChooseResults` に 1、`ConfirmResults` に `Positive`、`AbandonResults.Enqueue(() => { players.Upsert(TestProduce.Player(hasActiveProduce: false)); return UniTask.FromResult(true); })` → 記録が `"Choose:…"`, `"Confirm:…"`, `"Abandon"`, `"SetResume:False,プロデュース"`。`OpenProduce_Active_ResumeNotInProgress_ShowsInfoAndRefreshes` は `ResumeResults.Enqueue(() => { players.Upsert(TestProduce.Player(hasActiveProduce: false)); return UniTask.FromResult(ProduceResumeResultKind.NotInProgress); })` → `"Choose:…"`, `"Resume"`, `"Info:進行中のプロデュースはありません。"`, `"SetResume:False,プロデュース"`。`OpenProduce_Active_Resume_GoesToProduce` は `ResumeResults.Enqueue(() => { session.Set(TestProduce.StateResult()); return UniTask.FromResult(ProduceResumeResultKind.Resumed); })` → `"Choose:…"`, `"Resume"`, `"GoToProduce"`。`OpenProduceAsync_WhileRunning_Ignored` は `ResumeResults` に `NotInProgress` を返す `Func` を 1 つ、`InfoGate` で止める（`"Resume"` は 1 回）。`Refresh_ActiveSession_*` は `session.Set(...)` のあと `"SetResume:True,再開"`

### R6. タスクの読み替え・申し送り

- タスク 1: R1 の表の最後の行の改名（サーバーの 3 型と Shared.Domain の `ClassOptionView`）を、`UsableHand` と同じタスクで行う。確かめ方は本文のまま（`dotnet build Uta.slnx` が警告 0・エラー 0、テスト、Lint）
- タスク 2: Application の型（R1 の名前）・`IProduceUseCase`・`ProduceSessionState`・`FormationNormalizer`・`ScenarioResolver`、偽（`FakeProduceUseCase`・`TestProduce`）、テスト（`ProduceSessionStateTests`・`ProduceStateResultTests`・`FormationNormalizerTests`・`ScenarioResolverTests`（`Count`・`IdAt`・`NameAt`・範囲外の例外））。`ServerReasons`・`ServerReasonsTests` は触らない
- タスク 3: `ProduceUseCase`・`ProduceMapper`・`ApiClient.Produce`・`MagicOnionClientInitializer`、`FakeProduceService`、`ProduceUseCaseTests`・`ProduceMapperTests`
- タスク 4: `ProduceBackdropDtoFactory`・`FormationDtoFactory`・`FormationViewDto`・`FormationSlotRowViewDto` を足す。`FormationSlotViewData.cs` の行は `FormationSlotRowViewDto.cs`・`FormationViewDto.cs`・`FormationDtoFactory.cs`。テストに `ProduceBackdropDtoFactoryTests`・`FormationDtoFactoryTests` を足す
- タスク 5: DI は R4 の 6 行と `Register<IProduceService>`（R3）。`HomeDtoFactory` のコンストラクターと `HomeDtoFactoryTests` の変更（R4）、`HomePresenter`・`HomePresenterTests` の変更（R4・R5）
- 1 章の ADR に 0020〜0022 を足す。「DTO の写しは Infrastructure に閉じる」（ADR 0008）は変わらない（`ProduceMapper` が Infrastructure）
- **week・lesson（次のセッション）への申し送り**: `ProduceActions.ExecuteLessonActionAsync(LessonActionType …)` → `UseCardAsync`／`UseDrinkAsync`／`SkipLessonTurnAsync`、`ResolveCardOfferAsync(CardOfferAction …)` → `PickCardOfferAsync`／`DeclineCardOfferAsync`。`ProduceSession` → `ProduceSessionState`、`ProduceProgress` → `ProduceStateResult`、`ProduceOutcome` → `ProduceEndResult`、`ProduceEndingKind` → `ProduceEndKind`。`FakeProduceFlow` の `base(...)` の引数の型は R4 の `ProduceFlow`。Presenter が受ける `ProduceActions` は `IProduceUseCase`、`FakeProducePort` は `FakeProduceUseCase`（R5 の形。本物の `ProduceActions` による状態の更新と `"no state result"` の例外を前提にしたテストは、積む `Func` の中で `session.Set` する・例外を投げる形に直す。`ServerReasons.ProduceStaleOperation` は無い）。lesson の Presenter が `session.ExamResultShown = true`（3.4.3）・`session.Clear()`（3.4.4「ホームへ」）と書く箇所は、`IProduceUseCase.MarkExamResultShown()`・`ClearSession()`（R2）を呼ぶ形に読み替える（ユーザーの判断 2026-11-08。Presenter は状態の型に直接書かない）。week のテストが `ServerReasons.ProduceInvalidArgument`・`ProduceStaleOperation` で業務エラーを作っている箇所（week 5.2）は、`FakeProduceUseCase.StepResults` に `ProduceStepResultKind.Rejected`・`Recovered` を積む形に読み替える（`ServerReasons` にプロデュースの定数は無い: R2）。**owned への申し送り**: 選択モードの要求の `ForSupportCard` は `(int idolId, IReadOnlyCollection<string> excludedUids)`（R4）

## 基盤 11d9b06 への読み替え（2026-11-08。[クライアントの移行計画](2026-11-08-client-foundation-migration.md)。この節が本文と食い違ったら、この節が正）

- `XxxApi`（`ApiServiceBase<…>` のラッパー。`GachaApi`・`SupportApi`・`ProduceApi`）は**作らない**。`UtaApiClient` が `public IGachaService Gacha { get; }`（`ISupportService Support`・`IProduceService Produce`）を持ち、`Create` の中で `core.CreateClient<I…Service>()` で作る。本文の「`ServiceClientFactory.Create<I…Service>(callInvoker)` で作って `new XxxApi(core, client)`」は「`core.CreateClient<I…Service>()`」に読み替える。private コンストラクターの引数の型は `XxxApi` ではなく `I…Service`
- ゲートウェイのコンストラクターは `I…Service` を受ける（`UtaApiClient` を受けない）。`BootstrapEntry` に `builder.Register<I…Service>(resolver => resolver.Resolve<UtaApiClient>().Xxx, Lifetime.Singleton);` を足す（`PlayerGateway` と同じ）
- 呼び方: `xxx.XxxAsync(req, handledCodes)` → `await xxx.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` の無い RPC は `WithHandledCodes` を付けない）。`result.IsSuccess` → `!result.IsError`。`result.Error.Code.Equals(X)` → `result.ErrorCode == X`。`result.Error.ToString()`・`result.Error.Code.Reason` は今のまま使える
- 本文のファイル表の `Api/XxxApi.cs`（作成）の行は無効（作らない）。テストの偽物は、`PlayerGatewayTests` と同じく偽の `I…Service`（`IService<T>` の 5 メンバーを実装し、`WithHeaders` は `this` を返す）
- `[MagicOnionClientGeneration]` の型の並びに `typeof(I…Service)` を足す指示は今のまま有効（`Uta.Shared.Contracts` の同じアセンブリなので、足し忘れても `IPlayerService` で足りるが、本文どおり足す）
- 3 章の `UtaApiClient` の変更（private コンストラクターの末尾に `ProduceApi produce` を足す箇条書き。移行計画を書いた時点の 314 行目）は `ProduceApi` ではなく `IProduceService Produce`（上の節）

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **編成は `Home` シーンのページ、プロデュースの局面の画面は `Produce` シーン**（[screen-design.md](../wiki/architecture/screen-design.md) 8章）。ホームの「プロデュース」で編成のページ（`FormationPage`）を `Home` の上に積み、「プロデュース開始」が成功したら `ISceneNavigator.GoToProduceAsync` で `Produce` シーンへ移る。ホームの「再開」も、状態を取り直してから `GoToProduceAsync`。プロデュースをやめる（ホームへ戻る・放棄・終了・セッションが無くなった）ときは `ISceneNavigator.GoToHomeAsync` で `Home` シーンを読み込み直す（ページを Pop しない。`Produce` のページはシーンごと破棄される）
2. **`Produce` シーンの底のページは、いつも背景のページ `ProduceBackdropPage`**（週・パラメータ・体力だけを出す。P3 3.7 の Prefab）。`Produce` シーンの入口 `ProduceEntry` が、セッションがあれば背景のページを積んでから `ProduceFlow.ShowCurrentAsync` を呼ぶ。局面の切り替えは「背景より上のページ・モーダルを全部 Pop してから、次の局面の画面を Push」（`PageStack.PopToRootAsync`。`Replace` は作らない: foundation 3.3.1）。間に一瞬背景のページが見えるのは受け入れる。局面 `CardOffer` は、背景のページの上にモーダルを出すだけになる（旧案の「`CardOffer` のときだけ背景のページを積む」分岐は要らなくなる）
3. **`ProduceFlow`・`ProduceMenu` は `Produce` シーンのスコープに登録する**（`IScreenNavigator`・`PageStack`・`DialogService` がシーンごとのため。foundation 3.3.9）。`ProduceSession`・ユースケース・ゲートウェイはルートのスコープ（`BootstrapEntry.RegisterUseCases`）。`Home` シーンの `HomePresenter`・`FormationPresenter` は `ProduceFlow` を使わず、`ISceneNavigator` で `Produce` へ移る
4. **局面の画面がまだ無い局面**（この計画の時点では全部。5・6 本目が `case` を足す）は、「この局面の画面は準備中です: <局面>」を出してからホームへ戻る（`ExitToHomeAsync`）。セッションはサーバーに残り、「再開」から続けられる
5. **View は P3 の骨格に `IXxxView` の実装を足す**（foundation 3.3.3）。`FormationPage`（`IFormationView`）・`FormationSlotRowView`（枠の行の表示のメソッド）・`ProduceBackdropPage`（`IProduceBackdropView`）。フィールド・子の名前は P3 の 3.1・3.2・4章が正で、この計画は UI を組まない（旧版の `BuildUi`・`UiFactory`・「画面のレイアウト」の節は消した）。Prefab の作り直しは要らない（メソッドとイベントを足すだけ）
6. **`UsableHand` のキーは末尾の `Key(19)`**（既存のキーを動かさないため。古いクライアント・保存済みの応答と互換）。[produce-server.md](../wiki/architecture/server/produce-server.md) 6章の表は意味の近さで `Hand` の次に並べたまま、「Key は末尾の 19」と注記する（表の `UsableHand` は進行役が足し済み。二重に足さない）
7. **`UsableHand` はサーバーの `ProduceViewBuilder` が `ProduceEngine.CanUse(state, id)`（既存）で作る**。`Uta.Shared.Domain` に公開メソッドを足さない（クライアントは使用可否を計算しないため。critique M3）
8. **型の名前**: クライアントの応答の写しは `Produce` で始め、`View` で終えない（`Uta.Shared.Domain.Produce.ClassOptionView`・`ProduceResult` と衝突させない。Presentation の `IXxxView`（画面の IF）と紛らわしくしない）。最上位は `ProduceProgress`（旧案 `ProduceStateView`）、結果は `ProduceOutcome`（`ProduceResult` は Domain にある）
9. **編成のプランの規則は Shared.Domain の `FormationValidator.IsPlanCompatible` を使う**（idol-support.md 2.1「同じ関数」）。クライアントに `FormationRules` は作らない。枠の数 5 は `FormationPresenter.SupportSlots`（P3 の Prefab の `Slot0`〜`Slot4` と同じ数）
10. **取り直し（`RefreshPlayerUseCase`）は状態を変えたユースケースが行い、`ProduceFlow` は行わない**（サインインの重複を避ける。consistency M6・critique M1）。RPC が成功した後の取り直しの失敗は成功として扱う（foundation 3.3.5）。`ProduceFlow.ExitToHomeAsync` は取り直さずに `GoToHomeAsync` を呼ぶ（取り直しは、終了・放棄・セッション切れを起こしたユースケースが済ませている）
11. **業務エラー**: `handledCodes` に入れたコードはユースケースの結果の型で返し、画面が文言を選ぶ。入れないコードは `UtaErrors` の `Stay`（`ApiCore` の共通ダイアログが出て画面に残る。ユースケースは `Failed`／`false` を返し、画面は追加で何も出さない）。RPC ごとの割り振りは 3.3
12. **例外**: ユースケースは通信の失敗を例外にしない（`PortResult` で返す）。例外は想定外のもの（不具合）だけで、**ボタンの処理（Presenter の公開 `UniTask` メソッド）と `ProduceEntry.StartAsync` が `catch (Exception e)` で受け、`Debug.LogException(e)` のあと `dialogs.ShowInfoAsync("エラーが発生しました: " + e.Message)` を出す**（`ProduceEntry` はそのあとホームへ戻る）。`ProduceFlow`・`ProduceMenu`・ユースケースは例外を受けない（投げたまま返す）。5・6 本目の Presenter も同じ書き方にする
13. **ホームの「再開」の表示**: `session.Current` があれば `session.HasActive`（`ProduceSession` が新しい）、無ければ `cache.Current.HasActiveProduce`（起動時のサインインの値）。`ProduceSession` はルートにあるので、`Produce` から `Home` を読み込み直しても残る
14. **`Produce` シーンはこの計画が作り直す**（foundation は変えていない: foundation の決めたこと 12）。`SceneBuilder.BuildProduce` を `Home` と同じ形（`UiRoot.prefab` を置き、`ProduceLifetimeScope.uiRoot` に入れる）にして legacy の補助と `using` を消し、`Produce.unity` を作り直し、#24 の `ProduceView`・`ProducePresenter` を消す（3.4・タスク 5）
15. **`Produce` シーンの底をいつも背景のページ（週・パラメータ・体力だけの画面）にしてよいか**（決めたこと 2）。局面が変わるたびに、一瞬この背景のページが見えてから次の画面が出る。代わりの案は「最初の局面のページを底にする」だが、局面の切り替えで底のページを閉じられない（基盤は底のページの Pop で空にできるが、`PageStack` の数え方とスキルカード獲得の下に敷くページが局面ごとに分かれ、分岐が増える）ので採らない（ユーザーが推奨どおり了承した。2026-11-07）
16. **5・6 本目ができるまで、ホームの「プロデュース開始」「再開」は `Produce` シーンへ移って、すぐ「この局面の画面は準備中です: WeekSelect」を出してホームへ戻る動きになる**（決めたこと 4）。プロデュースのセッションはサーバーに残る（「再開」のボタンが出る）。この途中の動きでよいか（ユーザーが推奨どおり了承した。2026-11-07）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の C# の 4 本目。次を作る。

1. サーバーの小さな直し: `LessonDto` に `UsableHand`（手札のカードごとの使用可否。[screen-design.md](../wiki/architecture/screen-design.md) 6章。ユーザーの判断 2026-11-03）を足す。6 本目（レッスン）が使う
2. `IProduceService` を呼ぶクライアントの層: ポート・ゲートウェイ・応答の写し（`ProduceProgress` ほか）・`ProduceSession`（最新の状態）・ユースケース・`ProduceFlow`（局面から画面への振り分け）・`ProduceMenu`（ホームへ戻る・放棄）
3. `Produce` シーンの入口（`ProduceEntry`）と底のページ（`ProduceBackdropPage` の View の実装と `ProduceBackdropPresenter`）
4. 編成の画面（育成アイドル・サポートカード 5 枠・シナリオの選択 → 開始。`Home` シーンのページ）と、ホームの「プロデュース」「再開」（と、その中の「放棄」）

仕様は [produce.md](../wiki/systems/produce.md)・[idol-support.md](../wiki/systems/idol-support.md) 2章（編成）、RPC・DTO は [produce-server.md](../wiki/architecture/server/produce-server.md) 5・6章と `shared/Uta.Shared.Contracts/Produce/`、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 1・ホーム。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TMP、7.6 プロデュースの画面の流れ）と 8章（シーン構成）。

- **画面を作る共通ルールは [1 本目（foundation）](2026-11-03-client-screens-foundation.md) 3.3 が正**（遷移・Presenter の形・コンストラクター 2 本・`[AssetAddress]`・View の形・実行中フラグ・購読と `Dispose`・破棄の印・テストの書き方・偽の作り・DI の置き場・型の名前の衝突）。この計画はそれに従い、同じことを繰り返して書かない
- **View のフィールドと Prefab の子の対応は [P3](2026-11-07-client-prefabs-screens-b.md) の 3.1・3.2・4章が正**（`FormationPage`・`FormationSlotRowView`・`ProduceBackdropPage`）
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層。DTO の写しは Infrastructure に閉じる）、[0009](../wiki/decisions/0009-client-stack.md)
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0 → P1 → P2 → foundation → gacha → owned → P3 → **この計画** → week → lesson
- **前提**（実装に入る前に、次がブランチに入っていることを確かめる。無ければ止まって報告する）:
  - 1 本目（foundation）が done: `IScreenNavigator`（基盤）を各シーンのスコープに登録する `SceneUiInstaller`、`PageStack`、`PageBase`、`DialogService`、部品の View のメソッド（`ButtonView.Button`・`SetLabel`・`SetInteractable`、`HeaderView.BackButton`）、`ISceneNavigator`（`GoToHomeAsync`・`GoToProduceAsync`）、`IGameMaster`・`ScenarioInfo`、`RefreshPlayerUseCase`、`HomePresenter`・`HomeEntry`、`SceneBuilder` の `InstantiateUiRoot`・`SetRef`（foundation 3.10。foundation は `Produce` シーンを一切変えていない: `BuildProduce`・`Produce.unity`・`ProduceLifetimeScope`・`ProduceView`・`ProducePresenter` は #24 のまま。この計画が作り直す: 3.4・タスク 5）、`BootstrapEntry.RegisterUseCases`、`UtaErrors`（`ProduceErrors` の 7 つを `ErrorAction.Stay` で登録済み）、`Tests/EditMode/Fakes/`（`CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`）
  - 2 本目（gacha）が done: `PortResult<T>`・`ServerReasons` と `ServerReasonsTests`（`ProduceStaleOperation` ほか 7 つ。無ければこの計画のタスク 2 で定数とテストの行を足す: 下の 3.2）、`FakeGameMaster`、`HomePresenter` のコンストラクターへの `IScreenNavigator` の追加
  - 3 本目（owned）が done: `OwnedSelectionRequest`（`ForIdol`・`ForSupportCard`）・`OwnedSelection`（`IsCancelled`・`IdolId`・`SupportCardUid`）、選択モードの `OwnedPresenter`（Args は `OwnedArgs`。結果は `WaitForPopAsync<OwnedSelection>`）、`DisplayText`（`RarityName`・`PlanName`・`ParamName`）、`FormationValidator.IsPlanCompatible(Plan idolPlan, Plan supportPlan)`（Shared.Domain。**main には無く、owned のタスク 1 が `shared/` に足す**）
  - P3 が done: `Scripts/Presentation/Formation/FormationPage.cs`・`FormationSlotRowView.cs`、`Scripts/Presentation/Produce/ProduceBackdropPage.cs`（P3 3.1 のフィールドだけの骨格）と、`Assets/Prefabs/Screens/Formation/FormationPage.prefab`・`Assets/Prefabs/Screens/Produce/ProduceBackdropPage.prefab`（Addressables の `Screens` に、アドレス `FormationPage`・`ProduceBackdropPage`）。フィールドの名前・型が P3 3.1 と違えば止まって報告する
  - サーバーのプロデュース（順 6）は main にある
  - Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready`）。**Editor を開けない環境では、client のコンパイル・テスト・Play のどれも確かめられない**。そのときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）。タスク 1（server・shared）は .NET SDK と Docker で確かめる
- **決めないこと / 作らないもの**: 週の行動選択・授業・支給・カードの選択肢の画面（5 本目）、レッスン・試験・結果の画面（6 本目）、レンタル枠（`RentalCandidateId` は常に 0）、編成のプリセット、難易度の選択（シナリオの選択だけ）、「開始時の補正」の表示（アビリティは所持一覧の詳細で出す: 3 本目）、取り直しで返った `DiscardedProduce` の表示（起動のときだけ出す: 1 本目。プロデュースの画面からの取り直しでは出さない）、Prefab と見た目（P3）
- **未確認**: `Uta.Shared.Contracts/Produce` の DTO の、Unity 7 の IL2CPP での MessagePack の送受信（[produce-server.md](../wiki/architecture/server/produce-server.md) 実装メモ、[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.2）。この計画のタスク 6 で確かめるのは、局面 `WeekSelect` の応答（最上位の値・enum と `Deck`・`WeekActions`・`ClassOptions`・`CardOffers` の配列、`Lesson`・`ExamResult`・`Result` は `null` の経路）まで。`LessonDto`（`bool[]` を含む入れ子）・`ExamResultDto`・`ProduceResultResponse` は 6 本目で確かめる。`Home` → `Produce` → `Home` のシーンの往復で、ルートのスコープの `ProduceSession` が残ることも、タスク 6 の Play で確かめる

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対（クライアント）、それ以外はリポジトリのルートからの相対。新しい `.cs` には Unity が作った `.meta` をコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `shared/Uta.Shared.Contracts/Produce/LessonDto.cs` | `UsableHand`（`bool[]`、`Key(19)`）を足す |
| 変更 | `server/Uta.Server/Application/Produce/ProduceView.cs` | `ProduceView` の末尾に `UsableHand` |
| 変更 | `server/Uta.Server/Application/Produce/ProduceViewBuilder.cs` | `UsableHand` を作る |
| 変更 | `server/Uta.Server/Api/ProduceResponseMapper.cs` | `UsableHand` を `LessonDto` に通す |
| 変更 | `server/Uta.Server.Tests/Integration/ProduceOperationTests.cs` | 結合テスト 2 つ（5章） |
| 変更 | `uta_docs/wiki/architecture/server/produce-server.md` | 6章の `LessonDto` の行に「Key は末尾の 19」、5章の `StartAsync`（破棄）・`AbandonAsync` の `PRODUCE_STALE_OPERATION`、実装メモ（タスク 1） |
| 変更 | `uta_docs/wiki/architecture/lesson-domain-types.md`・`produce-domain.md` | 使用可否の判定の書き方を screen-design 6章に合わせる（タスク 1） |
| 作成 | `Scripts/Application/Produce/ProduceDeckCard.cs`・`ProduceWeekAction.cs`・`ProduceClassOption.cs`・`ProduceExamTurn.cs`・`ProduceParamBonus.cs`・`ProducePersistent.cs`・`ProduceLesson.cs`・`ProduceExamResult.cs`・`ProduceEndingKind.cs`・`ProduceOutcome.cs`・`ProduceProgress.cs`・`CardOfferAction.cs`・`LessonActionType.cs` | 応答の写し（3.2。1 ファイル 1 型） |
| 作成 | `Scripts/Application/Produce/IProducePort.cs`・`ProduceSession.cs`・`ProduceRefresh.cs`・`ProduceStartStatus.cs`・`StartProduceUseCase.cs`・`ProduceResumeStatus.cs`・`ResumeProduceUseCase.cs`・`AbandonProduceUseCase.cs`・`ProduceStepStatus.cs`・`ProduceActions.cs` | ポート・状態・ユースケース（3.2） |
| 変更 | `Scripts/Application/Common/ServerReasons.cs` | プロデュースの reason が無ければ足す（3.2） |
| 変更 | `Tests/EditMode/ServerReasonsTests.cs` | 足した reason の行を足す（3.2・5章） |
| 作成 | `Scripts/Infrastructure/Api/ProduceApi.cs` | `ApiServiceBase<IProduceService>`（3.3） |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs` | `Produce` を足し、`typeof(IProduceService)` を足す（3.3） |
| 作成 | `Scripts/Infrastructure/Produce/ProduceGateway.cs` | `IProducePort` の実装と、応答の写し（3.3） |
| 作成 | `Scripts/Presentation/Produce/ProduceFlow.cs`・`ProduceMenu.cs`・`ProduceEntry.cs` | 局面から画面への振り分け、メニュー、`Produce` シーンの入口（3.4） |
| 作成 | `Scripts/Presentation/Produce/IProduceBackdropView.cs`・`ProduceBackdropPresenter.cs` | 底の背景のページの IF と Presenter（3.4） |
| 変更 | `Scripts/Presentation/Produce/ProduceBackdropPage.cs` | P3 の骨格に `IProduceBackdropView` の実装を足す（3.4） |
| 変更 | `Scripts/Presentation/Produce/ProduceLifetimeScope.cs` | 中身を置き換える: `uiRoot` と `SceneUiInstaller.Install`（foundation の `HomeLifetimeScope` の形）に、`ProduceFlow`・`ProduceMenu`・`ProduceEntry` の登録（3.4）。#24 の `ProduceView`・`ProducePresenter` の登録を消す |
| 削除 | `Scripts/Presentation/Produce/ProduceView.cs`・`ProducePresenter.cs` | #24 の仮の画面（legacy `Text`）。`.meta` も消す（タスク 5） |
| 変更 | `Scripts/Editor/SceneBuilder.cs` | `BuildProduce` を `BuildHome` と同じ形にし、legacy の補助と `using` を消す（3.4・タスク 5） |
| 生成 | `Scenes/Produce.unity` | `Uta/Create Scenes` で作り直す（`UiRoot` を置く。タスク 5） |
| 作成 | `Scripts/Presentation/Formation/IFormationView.cs`・`FormationPresenter.cs`・`FormationSlotViewData.cs` | 編成の IF と Presenter（3.5） |
| 変更 | `Scripts/Presentation/Formation/FormationPage.cs`・`FormationSlotRowView.cs` | P3 の骨格に `IFormationView` の実装と行の表示のメソッドを足す（3.5） |
| 変更 | `Scripts/Presentation/Home/HomePresenter.cs` | `OpenProduceAsync` を置き換える、再開の表示（3.6） |
| 変更 | `Scripts/Bootstrap/BootstrapEntry.cs` | `RegisterUseCases` にプロデュースの 6 行（タスク 5） |
| 作成 | `Tests/EditMode/Fakes/FakeProducePort.cs`・`FakePlayerPort.cs`・`TestProduce.cs` | テスト用の偽と組み立て（3.7。5・6 本目も使う） |
| 作成 | `Tests/EditMode/ProduceSessionTests.cs`・`ProduceProgressTests.cs`・`StartProduceUseCaseTests.cs`・`ResumeProduceUseCaseTests.cs`・`AbandonProduceUseCaseTests.cs`・`ProduceActionsTests.cs`・`ProduceGatewayTests.cs`・`ProduceFlowTests.cs`・`ProduceMenuTests.cs`・`ProduceEntryTests.cs`・`ProduceBackdropPresenterTests.cs`・`FormationPresenterTests.cs` | EditMode のテスト（5章） |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | 新しいコンストラクターに合わせ、テストを足す（5章） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`unity7-evaluation.md` | 結果の記録（タスク 6） |

`Scripts/Presentation/Produce/` の名前空間は `Uta.Client.Presentation.Produce`、ただし `ProduceLifetimeScope.cs` は #24 のまま `Uta.Client.Presentation`。Prefab・Addressables は触らない（P3 の生成物）。`Produce` シーン（`SceneBuilder.BuildProduce`・`Produce.unity`・`ProduceLifetimeScope`・#24 の `ProduceView`・`ProducePresenter`）は foundation が変えていないので、この計画が作り直す（3.4・タスク 5）。

## 3. 型とシグネチャ

C# は C# 9 の範囲（`client/`・`shared/`。コレクション式 `[...]`・record・ファイルスコープ namespace は使わない。配列は `new[] { ... }`、空は `new bool[0]` か `Array.Empty<T>()`）。`server/` は C# 14。名前空間はフォルダに合わせる: `Scripts/Application/Produce` → `Uta.Client.Application.Produce`、`Scripts/Infrastructure/Api` → `Uta.Client.Infrastructure.Api`、`Scripts/Infrastructure/Produce` → `Uta.Client.Infrastructure.Produce`、`Scripts/Presentation/Produce` → `Uta.Client.Presentation.Produce`、`Scripts/Presentation/Formation` → `Uta.Client.Presentation.Formation`、テストは `Uta.Client.Infrastructure.Tests`。`using UnityScreenNavigator;` と `using Uta.Client.Application.Navigation;` の両方を書くファイル（`ProduceFlow`・`ProduceEntry`・`FormationPresenter`・`HomePresenter`）は `using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;` を足す（foundation 3.3.10）。

### 3.0 名前空間と `using`

**新しいファイルの名前空間は、上流の各計画の 2 章（触るファイル）のフォルダに合わせる**（上の段落の対応のとおり。P3 の骨格のファイル `FormationPage.cs`・`FormationSlotRowView.cs`・`ProduceBackdropPage.cs` は P3 の名前空間のまま）。各ファイルの `using` は、下の表から**そのファイルが使う型の行だけ**を書く（使わない `using` は書かない。`unity recompile` の警告を増やさない）。

| 使う型 | 名前空間（`using`） | 正（作る計画） |
|---|---|---|
| `DisplayText` | `Uta.Client.Presentation.Common` | owned 3.2 |
| `PageStack`・`DialogService`・`DialogResult`・`PageBase`・`UiRoot`・`SceneUiInstaller` | `Uta.Client.Presentation.Common` | foundation 3.2、P1 3.3 |
| `ButtonView`・`HeaderView` | `Uta.Client.Presentation.Common.Parts` | foundation 3.2、P1 |
| `OwnedSelectionRequest`・`OwnedSelection`・`OwnedSelectionKind` | `Uta.Client.Application.Owned` | owned 3.4 |
| `OwnedArgs`・`OwnedPresenter` | `Uta.Client.Presentation.Owned` | owned 3.6 |
| `IGameMaster`・`IdolInfo`・`SupportCardInfo`・`ScenarioInfo` | `Uta.Client.Application.Master` | foundation 3.4 |
| `PlayerCache`・`RefreshPlayerUseCase`・`IPlayerPort`・`PlayerSnapshot`・`PlayerSignInResult`・`OwnedIdol`・`OwnedSupportCard` | `Uta.Client.Application.Player` | 既存のコード・foundation 3.5 |
| `PortResult<T>`・`ServerReasons` | `Uta.Client.Application.Common` | gacha 3.1 |
| `ISceneNavigator`（Uta の） | `Uta.Client.Application.Navigation`（`using UnityScreenNavigator;` と並べるファイルは別名 `using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;`） | 既存のコード・foundation 3.3.10 |
| `IScreenNavigator`・`IPresenter`・`IScreenWithArgs<TArgs>`・`AssetAddressAttribute` | `UnityScreenNavigator` | foundation 3.3（基盤） |
| `ApiCore`・`ApiResult<T>`・`ApiServiceBase<T>` | `VolTech.Client` | 既存の `PlayerApi.cs` |
| `ErrorCode` | `VolTech.Server.Abstractions` | 既存の `PlayerApi.cs` |
| `IProduceService`・`ProduceStateResponse` ほかの DTO・`ProduceErrors`・`ProduceEnding`・`CardOfferActionKind`・`LessonActionKind` | `Uta.Shared.Contracts.Produce` | shared/ |
| `ProducePhase`・`WeekChoiceKind`・`ExamKind`・`ExamOutcome`・`ProduceRank`・`FormationValidator` | `Uta.Shared.Domain.Produce`（`FormationValidator` は `shared/Uta.Shared.Domain/Produce/FormationValidator.cs`。2026-11-07 に確かめた） | shared/、owned |
| `LessonKind`・`ParamKind`・`PersistentTrigger`・`Plan`・`Rarity` | `Uta.Shared.Domain.Lesson`（マスタの生成の名前空間 `…MasterData.Generated` には同名の `Rarity` があるので `using` しない: owned 3.2） | shared/ |
| `TMP_Text` | `TMPro` | P1 |
| `UniTask`・`UniTaskCompletionSource` | `Cysharp.Threading.Tasks` | — |
| `IContainerBuilder`・`Lifetime`・`[Inject]`／`LifetimeScope`・`IStartable` | `VContainer`／`VContainer.Unity` | — |
| テストの偽（`CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`・`FakeGameMaster`・この計画の偽） | `Uta.Client.Infrastructure.Tests`（テストと同じ名前空間なので `using` は要らない） | foundation 3.3.8、gacha タスク 1 |

- 表と実在のコード（上流の計画の実装）の名前空間が違ったら、実在のほうを書く（型の名前が無い・違うときは止まって報告する）

### 3.1 サーバー・shared の直し（`UsableHand`）

- `LessonDto`: コンストラクターの末尾（`int[] npcScores` の後ろ）に `bool[] usableHand` を足し、`[Key(19)] public bool[] UsableHand { get; }`。既存のキー 0〜18 は変えない
- `ProduceView`（record）: 末尾に `IReadOnlyList<bool> UsableHand` を足す。`None` は `new(null, [], [], [], null, null, [])`。`new ProduceView(` は `ProduceViewBuilder.Build` と `None` の 2 か所だけ（テストには無い）
- `ProduceViewBuilder.Build`: `bool[] usableHand = state.Lesson is null ? [] : state.Lesson.Zones.Hand.Select(id => engine.CanUse(state, id) == UseCheckResult.Ok).ToArray();` を作り、`new ProduceView(...)` の末尾に渡す（`using Uta.Shared.Domain.Lesson;` を足す）
- `ProduceResponseMapper`: `ToLessonDto(LessonState lesson, IReadOnlyList<int> npcScores, IReadOnlyList<bool> usableHand)` にし、`new LessonDto(...)` の末尾に `usableHand.ToArray()` を渡す。`ToStateResponse` は `ToLessonDto(state.Lesson, view.NpcScores, view.UsableHand)`

### 3.2 `Uta.Client.Application.Produce`

使う Domain の enum: `Uta.Shared.Domain.Produce` の `ProducePhase`・`WeekChoiceKind`・`ExamKind`・`ExamOutcome`・`ProduceRank`、`Uta.Shared.Domain.Lesson` の `LessonKind`・`ParamKind`・`PersistentTrigger`・`Plan`。

`ServerReasons`（2 本目の `Scripts/Application/Common/ServerReasons.cs`）に、次の 7 つの定数が無ければ足す（値は `Uta.Shared.Contracts.Produce.ProduceErrors` の各 `ErrorCode` の `Reason` と同じ文字列。`Application` は Contracts を参照しないので文字列で持つ）: `ProduceStaleOperation`・`ProduceUnexpectedPhase`・`ProduceInvalidArgument`・`ProduceInvalidFormation`・`ProduceNotInProgress`・`ProduceSessionDiscarded`・`ProduceAlreadyInProgress`。**定数を足したら、同じタスクで `ServerReasonsTests` の `Reasons_MatchContractsErrorCodes` に、足した定数ごとに 1 行足す**（gacha 3.1 の規則「定数とテストの行を一緒に足す」。owned と同じ書き方。5章）。

応答の写し（どれも `sealed class`、コンストラクターで全部受け、get だけのプロパティ。プロパティ名は引数名の先頭を大文字にしたもの。配列の引数が `null` なら `ArgumentNullException`）:

```csharp
public sealed class ProduceDeckCard { public ProduceDeckCard(int instanceId, int cardId, bool enhanced); }
public sealed class ProduceWeekAction { public ProduceWeekAction(int slotIndex, WeekChoiceKind kind, bool selectable); }
public sealed class ProduceClassOption { public ProduceClassOption(int index, int hpCost, ParamKind param, int paramGain, bool givesCard, bool selectable); }
public sealed class ProduceExamTurn { public ProduceExamTurn(ParamKind param, int scoreBonusPercent); }
public sealed class ProduceParamBonus { public ProduceParamBonus(int percent, int remainingTurns); }
public sealed class ProducePersistent { public ProducePersistent(PersistentTrigger trigger, int remainingTurns, int remainingCount); }

public sealed class ProduceLesson
{
    public ProduceLesson(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget,
        IReadOnlyList<ProduceExamTurn> examTurns, int turn, int shield, int usesLeft, int paramGained,
        IReadOnlyList<int> hand, IReadOnlyList<bool> usableHand, IReadOnlyList<int> drawPile, IReadOnlyList<int> discard, IReadOnlyList<int> exiled,
        IReadOnlyList<int> statusValues, IReadOnlyList<ProduceParamBonus> paramBonuses, IReadOnlyList<ProducePersistent> persistents,
        IReadOnlyList<int> lessonEnhanced, IReadOnlyList<int> npcScores);
    // 同じ名前のプロパティ（Kind … NpcScores）。UsableHand は Hand と同じ長さか、空（古いサーバーの応答。扱いは 6 本目）
    public bool IsExam { get; }   // Kind が LessonKind.MidExam か LessonKind.FinalExam のとき true
}
public sealed class ProduceExamResult { public ProduceExamResult(ExamKind kind, int attempt, int score, int rank, IReadOnlyList<int> npcScores, bool passed); }

public enum ProduceEndingKind { Nothing = 0, Completed = 1, Abandoned = 2, Discarded = 3 }   // Contracts.Produce.ProduceEnding と同じ数値
public sealed class ProduceOutcome
{
    public ProduceOutcome(ProduceEndingKind ending, ExamOutcome examOutcome, int evaluationValue, ProduceRank rank, int finalScore, int finalRank,
        int gachaCurrency, int gold, int supportPoints, int exp, int playerLevel, int playerExp);
}

public sealed class ProduceProgress
{
    public ProduceProgress(int operationNumber, ProducePhase phase, int scenarioId, int idolId, int week, int singing, int rhythm, int expression,
        int hp, int maxHp, int examRetryLeft, IReadOnlyList<ProduceDeckCard> deck, IReadOnlyList<ProduceWeekAction> weekActions,
        IReadOnlyList<ProduceClassOption> classOptions, IReadOnlyList<int> cardOffers, ProduceLesson lesson, ProduceExamResult examResult, ProduceOutcome result);
    // 同名のプロパティ。Lesson・ExamResult・Result は無ければ null
    public int GetParam(ParamKind param);   // Singing / Rhythm / Expression の値。それ以外は ArgumentOutOfRangeException
}

public enum CardOfferAction { Nothing = 0, Pick = 1, Reroll = 2, Remove = 3, Decline = 4 }   // Contracts.Produce.CardOfferActionKind と同じ数値
public enum LessonActionType { Nothing = 0, UseCard = 1, UseDrink = 2, Skip = 3 }             // Contracts.Produce.LessonActionKind と同じ数値

public interface IProducePort
{
    UniTask<PortResult<ProduceProgress>> StartAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId);
    UniTask<PortResult<ProduceProgress>> GetStateAsync();
    UniTask<PortResult<ProduceOutcome>> AbandonAsync();
    UniTask<PortResult<ProduceProgress>> ChooseWeekActionAsync(int operationNumber, int slotIndex, int optionIndex, ParamKind param);
    UniTask<PortResult<ProduceProgress>> ResolveCardOfferAsync(int operationNumber, CardOfferAction kind, int offerIndex);
    UniTask<PortResult<ProduceProgress>> DecideExamRetryAsync(int operationNumber, bool retry);
    UniTask<PortResult<ProduceProgress>> ExecuteLessonActionAsync(int operationNumber, LessonActionType kind, int cardInstanceId, int drinkSlotIndex);
}

public sealed class ProduceSession
{
    public ProduceProgress Current { get; private set; }     // 無ければ null
    public ProduceProgress Previous { get; private set; }    // 直前の Set の前の Current（授業・支給・休むの結果の差に使う: 5 本目）。無ければ null
    public bool ExamResultShown { get; set; }                // 試験結果を画面に出したか（6 本目が使う。Set で false に戻る）
    /// <summary>Current があり、Phase が Nothing でも Finished でもない。</summary>
    public bool HasActive { get; }
    /// <summary>next が null なら ArgumentNullException。Previous = Current; Current = next; ExamResultShown = false。</summary>
    public void Set(ProduceProgress next);
    /// <summary>Current・Previous を null、ExamResultShown を false。</summary>
    public void Clear();
}
// 結果の画面（6 本目）は session.Current.Result を読む（Finished の応答の Result）。
// ルートのスコープの Singleton なので、Home・Produce のシーンを往復しても残る。

internal static class ProduceRefresh
{
    /// <summary>
    /// RPC が成功した後の取り直し。refresh.ExecuteAsync() を待ち、InvalidOperationException（取り直しの失敗。ApiCore が共通のダイアログを出し済み）は握りつぶす。
    /// 成功で true、失敗で false。
    /// </summary>
    public static async UniTask<bool> TryAsync(RefreshPlayerUseCase refresh);
}

public enum ProduceStartStatus { Succeeded, InvalidFormation, AlreadyInProgress, InvalidArgument, Failed }
public sealed class StartProduceUseCase
{
    public StartProduceUseCase(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    public UniTask<ProduceStartStatus> ExecuteAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId);
}

public enum ProduceResumeStatus { Resumed, NotInProgress, Failed }
public sealed class ResumeProduceUseCase
{
    public ResumeProduceUseCase(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    public UniTask<ProduceResumeStatus> ExecuteAsync();
}

public sealed class AbandonProduceUseCase
{
    public AbandonProduceUseCase(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    /// <summary>セッションが無くなったら true（放棄した、またはもう無かった）。残っていれば false。</summary>
    public UniTask<bool> ExecuteAsync();
}

public enum ProduceStepStatus
{
    Succeeded,      // session.Current が次の局面になった
    Recovered,      // 食い違い（操作番号・局面）だったので GetState で取り直した。session.Current は最新。画面は Phase から選び直す
    SessionEnded,   // セッションが無い・破棄された。session は Clear 済みで、所持品は取り直した（取り直しの失敗は無視）。ホームへ戻る
    Rejected,       // PRODUCE_INVALID_ARGUMENT
    Failed          // それ以外（ApiCore の共通ダイアログが出ている）
}
public sealed class ProduceActions
{
    public ProduceActions(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    public UniTask<ProduceStepStatus> ChooseWeekActionAsync(int slotIndex, int optionIndex, ParamKind param);
    public UniTask<ProduceStepStatus> ResolveCardOfferAsync(CardOfferAction kind, int offerIndex);
    public UniTask<ProduceStepStatus> DecideExamRetryAsync(bool retry);
    public UniTask<ProduceStepStatus> ExecuteLessonActionAsync(LessonActionType kind, int cardInstanceId, int drinkSlotIndex = 0);
}
```

ユースケースの動き（`r` は port の結果。業務エラーの `r.Reason` は `ServerReasons` の定数と比べる）:

- `StartProduceUseCase.ExecuteAsync`: `r = await port.StartAsync(...)`。成功 → `session.Set(r.Value)`、`Succeeded`。`r.IsBusinessError` で `ProduceInvalidFormation` → `InvalidFormation`、`ProduceAlreadyInProgress` → `await ProduceRefresh.TryAsync(refresh)`（`PlayerCache.HasActiveProduce` を最新にする）して `AlreadyInProgress`、`ProduceInvalidArgument` → `InvalidArgument`。それ以外 → `Failed`
- `ResumeProduceUseCase.ExecuteAsync`: `r = await port.GetStateAsync()`。成功で `r.Value.Phase != ProducePhase.Nothing` → `session.Set(r.Value)`、`Resumed`。成功で `Phase == Nothing` → `session.Clear()`、`await ProduceRefresh.TryAsync(refresh)`、`NotInProgress`。失敗 → `Failed`
- `AbandonProduceUseCase.ExecuteAsync`: `r = await port.AbandonAsync()`。成功、または業務エラーの `ProduceNotInProgress`・`ProduceSessionDiscarded` → `session.Clear()`、`await ProduceRefresh.TryAsync(refresh)`、`true`。それ以外（`PRODUCE_STALE_OPERATION` は `handledCodes` に入れないので `Failure` で届く）→ `false`
- `ProduceActions` の 4 つの操作（共通。`port` の対応するメソッドに `session.Current.OperationNumber` と引数を渡す）:
  1. `session.Current` が `null` なら `InvalidOperationException("no active produce")`
  2. 成功: `session.Set(r.Value)`。`r.Value.Phase == ProducePhase.Finished` なら `await ProduceRefresh.TryAsync(refresh)`（報酬を所持品に反映する）。`Succeeded`
  3. 業務エラー:
     - `ProduceStaleOperation`・`ProduceUnexpectedPhase`: `g = await port.GetStateAsync()`。成功で `Phase != Nothing` → `session.Set(g.Value)`、`Recovered`。成功で `Phase == Nothing` → `session.Clear()`、`TryAsync`、`SessionEnded`。`g` の失敗 → `Failed`
     - `ProduceNotInProgress`・`ProduceSessionDiscarded`: `session.Clear()`、`TryAsync`、`SessionEnded`
     - `ProduceInvalidArgument`: `Rejected`
     - それ以外: `Failed`
  4. 業務エラーでない失敗: `Failed`
  - 実装は `private async UniTask<ProduceStepStatus> RunAsync(Func<int, UniTask<PortResult<ProduceProgress>>> call)`（`call` は操作番号を受ける）に 1 つにまとめ、4 つの公開メソッドはそれを呼ぶだけ

### 3.3 `Uta.Client.Infrastructure`

```csharp
// Api/ProduceApi.cs
public sealed class ProduceApi : ApiServiceBase<IProduceService>
{
    public ProduceApi(ApiCore core, IProduceService client) : base(core, client) { }
    public UniTask<ApiResult<ProduceStateResponse>> StartAsync(StartProduceRequest request, IEnumerable<ErrorCode> handledCodes = null);
    public UniTask<ApiResult<ProduceStateResponse>> GetStateAsync(IEnumerable<ErrorCode> handledCodes = null);
    public UniTask<ApiResult<ProduceResultResponse>> AbandonAsync(IEnumerable<ErrorCode> handledCodes = null);
    public UniTask<ApiResult<ProduceStateResponse>> ChooseWeekActionAsync(ChooseWeekActionRequest request, IEnumerable<ErrorCode> handledCodes = null);
    public UniTask<ApiResult<ProduceStateResponse>> ResolveCardOfferAsync(ResolveCardOfferRequest request, IEnumerable<ErrorCode> handledCodes = null);
    public UniTask<ApiResult<ProduceStateResponse>> DecideExamRetryAsync(DecideExamRetryRequest request, IEnumerable<ErrorCode> handledCodes = null);
    public UniTask<ApiResult<ProduceStateResponse>> ExecuteLessonActionAsync(ExecuteLessonActionRequest request, IEnumerable<ErrorCode> handledCodes = null);
}
// 実装は PlayerApi と同じ形: InvokeAsync(nameof(IProduceService.XxxAsync), request, static (c, r) => c.XxxAsync(r), handledCodes)。
// 引数の無い GetStateAsync・AbandonAsync は PlayerApi.SignInAsync と同じ（request に 0、static (c, _) => c.GetStateAsync()）。

// Produce/ProduceGateway.cs
public sealed class ProduceGateway : IProducePort
{
    public ProduceGateway(UtaApiClient api);
    // IProducePort の 7 メソッド
    public static ProduceProgress ToProgress(ProduceStateResponse response);    // テスト用に public static
    public static ProduceOutcome ToOutcome(ProduceResultResponse response);
}
```

- **`handledCodes`**（`ProduceGateway` の `private static readonly ErrorCode[]` に持つ。どれも `Uta.Shared.Contracts.Produce.ProduceErrors`）:

  | RPC | `handledCodes` | 入れないコードの扱い（`Stay`: 共通ダイアログ → 画面に残る） |
  |---|---|---|
  | `StartAsync` | `InvalidFormation`・`AlreadyInProgress`・`InvalidArgument` | `StaleOperation`（開始の中の古いセッションの破棄で楽観ロックが 0 行。まれ）→ `Failed`。編成の画面に残り、もう一度押せる |
  | `GetStateAsync` | なし | 失敗はすべて `Failed`（再開・取り直しをやめて、その画面に残る） |
  | `AbandonAsync` | `NotInProgress`・`SessionDiscarded` | `StaleOperation`（楽観ロックが 0 行）→ `false`。メニュー・ホームに残り、もう一度放棄できる |
  | 4 つの操作 | `StaleOperation`・`UnexpectedPhase`・`InvalidArgument`・`NotInProgress`・`SessionDiscarded` | なし（7 つのうち操作が返しうるものは全部入れた） |

- 結果の直し方は `GachaGateway.DrawAsync`（2 本目）と同じ: `result.IsSuccess` → `PortResult.Success(写したもの)`。失敗で `result.Error.Code.Domain == "uta"` かつ `Reason` がその RPC の `handledCodes` のどれかの `Reason` → `PortResult.BusinessError(result.Error.Code.Reason, result.Error.ToString())`。それ以外 → `PortResult.Failure(result.Error.ToString())`
- `StartAsync` の要求: `new StartProduceRequest(idolId, supportCardUids.ToArray(), 0, scenarioId)`（`RentalCandidateId` は 0）。操作の要求: `new ChooseWeekActionRequest(op, slot, option, param)`、`new ResolveCardOfferRequest(op, (CardOfferActionKind)(int)kind, offerIndex)`、`new DecideExamRetryRequest(op, retry)`、`new ExecuteLessonActionRequest(op, (LessonActionKind)(int)kind, cardInstanceId, drinkSlotIndex)`（引数の並びは各 DTO のコンストラクターのとおり）
- `ToProgress`: `response.Phase == ProducePhase.Nothing` のときも `ProduceProgress` を返す。`null` の配列は空のリストにする。`Lesson` が `null` でなければ `ProduceLesson` に写す（`UsableHand` が `null` なら**空のリスト**。`Hand` と同じ長さの `true` にはしない）。`ExamResult`・`Result` も `null` でなければ写す。`ProduceEnding` → `ProduceEndingKind` は `(ProduceEndingKind)(int)`
- `UtaApiClient`: private コンストラクターの**末尾**（1〜3 本目が足した引数の後ろ）に `ProduceApi produce` を足して `public ProduceApi Produce { get; }` に入れる。`Create`（foundation 3.8 の引数のまま）で `var produceClient = ServiceClientFactory.Create<IProduceService>(callInvoker);` を作り、`new ProduceApi(core, produceClient)` を末尾に渡す
- `UtaMagicOnionClientInitializer`: `[MagicOnionClientGeneration(...)]` の型の並びの最後（前の計画が足した型の後ろ）に `typeof(IProduceService)` を足す（`using Uta.Shared.Contracts.Produce;`）

### 3.4 `Uta.Client.Presentation.Produce`（`Produce` シーン）

**`Produce` シーンの組み立て**（決めたこと 1〜3）:

```
Home シーン                                  Produce シーン（底は ProduceBackdropPage）
  HomePage ─「プロデュース」→ FormationPage    ProduceEntry: PushPage<ProduceBackdropPresenter> → flow.ShowCurrentAsync()
     │  開始の成功 / 「再開」                     ProduceFlow.ShowCurrentAsync: 背景より上を全部 Pop → 局面の画面を Push（5・6 本目）
     └──── ISceneNavigator.GoToProduceAsync ──▶  ExitToHomeAsync / 局面の画面が無い / セッション切れ
                                    ◀──── ISceneNavigator.GoToHomeAsync ───┘
```

```csharp
// ProduceLifetimeScope.cs（Uta.Client.Presentation。中身を置き換える: foundation の HomeLifetimeScope の形 + 3 行）
public sealed class ProduceLifetimeScope : LifetimeScope
{
    [SerializeField] private UiRoot uiRoot;   // SceneBuilder.BuildProduce が入れる（下の SceneBuilder の直し）
    protected override void Configure(IContainerBuilder builder)
    {
        SceneUiInstaller.Install(builder, uiRoot);
        builder.Register<ProduceFlow>(Lifetime.Singleton);   // この計画が足す（ここから 3 行）
        builder.Register<ProduceMenu>(Lifetime.Singleton);
        builder.RegisterEntryPoint<ProduceEntry>();
    }
}
```

- `ProduceLifetimeScope.cs` の `using` は `UnityEngine`・`Uta.Client.Presentation.Common`（`UiRoot`・`SceneUiInstaller`）・`Uta.Client.Presentation.Produce`・`VContainer`・`VContainer.Unity`。#24 の `RegisterComponentInHierarchy<ProduceView>()`・`RegisterEntryPoint<ProducePresenter>()` は消し、`ProduceView.cs`・`ProducePresenter.cs`（と `.meta`）を削除する
- **`SceneBuilder`（`Uta.Client.Editor`）の直し**: `BuildProduce` を foundation 3.10 の `BuildHome` と同じ形にする: `CreateCamera()` → `var uiRoot = InstantiateUiRoot();` → `var scope = new GameObject("ProduceScope").AddComponent<ProduceLifetimeScope>();` → `SetRef(scope, "uiRoot", uiRoot);`。使わなくなる legacy の補助 `CreateEventSystem`・`CreateCanvas`・`CreateRect`・`CreateText`・`ConfigureText`・`CreateButton` と、`using` の `UnityEngine.EventSystems`・`UnityEngine.InputSystem.UI`・`UnityEngine.UI`・`Uta.Client.Presentation.Produce` を消す（foundation 3.10 が「4 本目が消す」と残したもの）。ほかのシーンの作り方は変えない
- `ProduceFlow`・`ProduceMenu` はこのシーンのスコープにだけある。`Produce` シーンに積む Presenter（5・6 本目の `WeekPresenter` など。基盤がこのスコープの子スコープで解決する）は、コンストラクターで受けられる。`Home` シーンの Presenter は受けられない（受けない）

```csharp
/// <summary>Produce シーンの入口。</summary>
public sealed class ProduceEntry : IStartable
{
    public ProduceEntry(IScreenNavigator navigator, ProduceSession session, ProduceFlow flow, DialogService dialogs, ISceneNavigator scenes);
    public void Start();   // StartAsync().Forget()
    /// <summary>
    /// 1. session.HasActive が false なら Debug.LogWarning("[Uta] produce: no active session") → await scenes.GoToHomeAsync() → 終わる
    ///    （ホームの「再開」・編成の開始を通らずにこのシーンが読まれた場合。Editor で Produce シーンを開いて Play しても Bootstrap から始まるので、通常は起きない）
    /// 2. await navigator.PushPageAsync&lt;ProduceBackdropPresenter&gt;(playAnimation: false);（シーンの底。閉じない）
    /// 3. await flow.ShowCurrentAsync();
    /// 2・3 を try で包み、catch (Exception e) で Debug.LogException(e) → await dialogs.ShowInfoAsync("エラーが発生しました: " + e.Message) → await scenes.GoToHomeAsync()（決めたこと 12）。
    /// </summary>
    public UniTask StartAsync();
}

public class ProduceFlow    // テストで派生させるので sealed にせず、公開メソッドは virtual
{
    public ProduceFlow(ProduceSession session, IScreenNavigator navigator, PageStack pageStack, DialogService dialogs, ISceneNavigator scenes);

    /// <summary>
    /// session.Current の局面の画面を出す。ProduceEntry（シーンに入ったとき）と HandleStepAsync（操作の後）から呼ぶ。
    /// 1. session.Current が null なら InvalidOperationException("no active produce")
    /// 2. await pageStack.PopToRootAsync(navigator);（底の背景のページより上のページ・モーダルを全部閉じる。呼んだ Presenter もここで閉じられる）
    /// 3. await PushForPhaseAsync(session.Current.Phase);
    /// </summary>
    public virtual UniTask ShowCurrentAsync();

    /// <summary>プロデュースの画面を離れてホームへ戻る（await scenes.GoToHomeAsync()。Produce シーンのページはシーンごと破棄される）。session も所持品の取り直しも触らない。</summary>
    public virtual UniTask ExitToHomeAsync();

    /// <summary>
    /// ProduceStepStatus を見て次の画面へ:
    /// Succeeded・Recovered → await ShowCurrentAsync()。
    /// SessionEnded → await dialogs.ShowInfoAsync("プロデュースは終了しています。") のあと await ExitToHomeAsync()。
    /// Rejected → await dialogs.ShowInfoAsync("その操作はできません。")。
    /// Failed → 何もしない。
    /// </summary>
    public virtual UniTask HandleStepAsync(ProduceStepStatus status);

    /// <summary>
    /// 局面ごとの画面を積む（底の背景のページの上に）。この計画の時点では case が無い（5・6 本目が足す）。
    /// default: await dialogs.ShowInfoAsync("この局面の画面は準備中です: " + phase); のあと await ExitToHomeAsync();
    /// （セッションはサーバーに残り、ホームの「再開」から続けられる）
    /// </summary>
    protected virtual UniTask PushForPhaseAsync(ProducePhase phase);
}
```

- `PushForPhaseAsync` は `switch (phase)` で書き、各 `case` は `await navigator.Push…Async<…>(playAnimation: false); return;` の形にする。5・6 本目が足す `case` の形（この計画では書かない）:
  - ページの局面: `case ProducePhase.WeekSelect: await navigator.PushPageAsync<WeekPresenter>(playAnimation: false); return;`
  - **カードの選択肢**: `case ProducePhase.CardOffer: await navigator.PushModalAsync<CardOfferPresenter>(playAnimation: false); return;`（2 で背景のページだけになっているので、モーダルは背景の上に出る。入口が再開でも操作の後でも同じ。モーダルの Presenter は操作のあと `flow.HandleStepAsync` を呼ぶだけでよい: `ShowCurrentAsync` がモーダルを閉じる）
- 局面の画面の Presenter が `ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync` を呼ぶと、その Presenter 自身が閉じられる（Pop、またはシーンの破棄で `Dispose` が呼ばれる）。**呼んだ後は View に触らない**（`finally` の実行中フラグを戻すことだけはしてよい）
- `PageStack` の枚数は呼んだ時点で数える（foundation 3.2）。呼ぶ前の遷移は全部 `await` 済みであること（foundation 3.3.5）。`Produce` シーンでは底（`Depth` の 1 枚目）が背景のページ

```csharp
public class ProduceMenu     // [≡] のメニュー。5・6 本目の画面から使う。テストで派生させるので sealed にせず、ShowAsync は virtual
{
    public ProduceMenu(DialogService dialogs, AbandonProduceUseCase abandon, ProduceFlow flow);
    /// <summary>
    /// var index = await dialogs.ChooseAsync("メニュー", new[] { "ホームへ戻る（あとで再開できます）", "プロデュースを放棄する", "閉じる" });
    /// 0 → await flow.ExitToHomeAsync()。
    /// 1 → await dialogs.ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？") が Positive で、await abandon.ExecuteAsync() が true のとき await flow.ExitToHomeAsync()（false なら何もしない。共通ダイアログが出ている）。Negative なら何もしない。
    /// 2・−1（ダイアログが消えた）→ 何もしない。
    /// 例外は受けない（呼んだ Presenter のボタンの処理が受ける: 決めたこと 12）。
    /// </summary>
    public virtual UniTask ShowAsync();
}
// メニューの Prefab は作らない（P3 決めたこと 3。P1 の Dialog を DialogService.ChooseAsync で出す）。

public interface IProduceBackdropView
{
    void SetHeader(string text);
}

// ProduceBackdropPage.cs（P3 の骨格。フィールド headerText（TMP_Text）は P3 のまま）に足す:
public sealed class ProduceBackdropPage : PageBase, IProduceBackdropView
{
    public void SetHeader(string text) { headerText.text = text; }
}

[AssetAddress(nameof(ProduceBackdropPage))]
public sealed class ProduceBackdropPresenter : IPresenter
{
    [Inject] public ProduceBackdropPresenter(ProduceBackdropPage view, ProduceSession session);   // : this((IProduceBackdropView)view, session)
    public ProduceBackdropPresenter(IProduceBackdropView view, ProduceSession session);
    /// <summary>Refresh() して UniTask.CompletedTask。</summary>
    public UniTask InitializeAsync();
    /// <summary>上のページが閉じてこのページに戻ったとき（局面の切り替えの途中）。Refresh()。</summary>
    public void DidPopEnter();
    public void Dispose();   // 何もしない（購読が無い）
    // private void Refresh(): session.Current が null なら何もしない。そうでなければ view.SetHeader(BuildHeader(session.Current))。
    /// <summary>
    /// "週 " + p.Week + "\n" + DisplayText.ParamName(ParamKind.Singing) + " " + p.Singing + "　" + DisplayText.ParamName(ParamKind.Rhythm) + " " + p.Rhythm + "　" + DisplayText.ParamName(ParamKind.Expression) + " " + p.Expression + "　体力 " + p.Hp + " / " + p.MaxHp
    /// （"　" は全角の空白 1 つ）。p が null なら ArgumentNullException。
    /// </summary>
    public static string BuildHeader(ProduceProgress p);
}
```

- 背景のページの見出しは、モーダルだけを閉じたとき（`CardOffer` → 次の局面のページ）には更新されない（`DidPopEnter` はページの Pop でだけ呼ばれる）。次の局面のページが上を覆うので、見えるのは一瞬で、受け入れる

### 3.5 `Uta.Client.Presentation.Formation`（`Home` シーン）

```csharp
public sealed class FormationSlotViewData
{
    public FormationSlotViewData(string label, bool filled);
    public string Label { get; } public bool Filled { get; }
}

public interface IFormationView
{
    void SetIdol(string label);                                // 下の Refresh の文言
    void SetSlots(IReadOnlyList<FormationSlotViewData> slots); // 5 件
    void SetScenario(string label, bool canChange);
    void SetStartEnabled(bool enabled);
    event Action IdolClicked;
    event Action<int> SlotClicked;        // 選ぶ（枠の添字 0〜4）
    event Action<int> SlotClearClicked;   // 外す
    event Action ScenarioClicked;
    event Action StartClicked;
    event Action BackClicked;
}
```

**View の実装**（P3 の骨格に足す。フィールドは P3 3.1 のまま、対応は P3 3.2・4章。foundation 3.3.3 の形）:

```csharp
// FormationSlotRowView.cs（P3 の骨格: labelText・selectButton・clearButton）に足す
public sealed class FormationSlotRowView : MonoBehaviour
{
    public ButtonView SelectButton => selectButton;
    public ButtonView ClearButton => clearButton;
    public void SetLabel(string text) { labelText.text = text; }
}

// FormationPage.cs（P3 の骨格: header・idolText・selectIdolButton・slots[5]・scenarioText・changeScenarioButton・startButton）に足す
public sealed class FormationPage : PageBase, IFormationView
{
    public event Action IdolClicked;
    public event Action<int> SlotClicked;
    public event Action<int> SlotClearClicked;
    public event Action ScenarioClicked;
    public event Action StartClicked;
    public event Action BackClicked;

    private void Awake()
    {
        // header.BackButton.Button.onClick       → BackClicked
        // selectIdolButton.Button.onClick         → IdolClicked
        // 各 i（0〜slots.Length-1）について var index = i; で受けてから
        //   slots[i].SelectButton.Button.onClick  → SlotClicked(index)
        //   slots[i].ClearButton.Button.onClick   → SlotClearClicked(index)
        // changeScenarioButton.Button.onClick     → ScenarioClicked
        // startButton.Button.onClick              → StartClicked
        // （どれも AddListener(() => Xxx?.Invoke(...))。解除は書かない: foundation 3.3.3）
    }

    public void SetIdol(string label);       // idolText.text = label
    /// <summary>
    /// slots が null なら ArgumentNullException。slots.Count != this.slots.Length なら ArgumentException("slot count mismatch: " + slots.Count)。
    /// 各 i: this.slots[i].SetLabel(slots[i].Label); this.slots[i].ClearButton.SetInteractable(slots[i].Filled);
    /// </summary>
    public void SetSlots(IReadOnlyList<FormationSlotViewData> slots);
    public void SetScenario(string label, bool canChange);   // scenarioText.text = label; changeScenarioButton.SetInteractable(canChange);
    public void SetStartEnabled(bool enabled);               // startButton.SetInteractable(enabled)
}
```

- 引数と同じ名前のフィールド（`slots`）があるので、`SetSlots` の中ではフィールドを `this.slots` と書く
- 文字の大きさ・色・ボタンのラベル（「選ぶ」「外す」「変更」「育成アイドルを選ぶ」「プロデュース開始」、ヘッダーの「編成」「戻る」）は Prefab（P3 3.5・3.6）のまま。C# は変えない

```csharp
[AssetAddress(nameof(FormationPage))]
public sealed class FormationPresenter : IPresenter
{
    public const int SupportSlots = 5;

    [Inject]
    public FormationPresenter(FormationPage view, PlayerCache cache, IGameMaster master, StartProduceUseCase start,
        DialogService dialogs, IScreenNavigator navigator, ISceneNavigator scenes);   // : this((IFormationView)view, ...)
    public FormationPresenter(IFormationView view, PlayerCache cache, IGameMaster master, StartProduceUseCase start,
        DialogService dialogs, IScreenNavigator navigator, ISceneNavigator scenes);

    public UniTask InitializeAsync();          // 6 つのイベントを購読し、既定値を決めて Refresh()。UniTask.CompletedTask
    public void Dispose();                     // 6 つの購読を解き、disposed = true
    public UniTask SelectIdolAsync();          // IdolClicked
    public UniTask SelectSlotAsync(int index); // SlotClicked
    public void ClearSlot(int index);          // SlotClearClicked
    public void NextScenario();                // ScenarioClicked
    public UniTask StartAsync();               // StartClicked
    public UniTask BackAsync();                // BackClicked
}
```

**状態**: `idolId`（0 = 未選択）、`slots`（長さ `SupportSlots` の `string[]`。サポートカードの所持ID（`SupportCardUid`）。空は `null`）、`scenarioIndex`（`master.Scenarios` の添字）、`running`（実行中フラグ。foundation 3.3.2 の 5）、`disposed`。

**動き**（イベントの受け口は同期で、対応する公開メソッドを呼ぶだけ。`UniTask` のものは `.Forget()`）:
- `InitializeAsync`: 購読する。`idolId` = `cache.Current.Idols` のうち `master.GetIdol(o.IdolId).Rarity` が最も大きいもの（`Rarity` の数値。同じなら `IdolId` が小さいもの。所持が 0 なら 0）。`scenarioIndex = 0`。`Refresh()`
- `Refresh()`（private）:
  - `SetIdol`: `idolId == 0` なら `"育成アイドル: 未選択"`。そうでなければ `var idol = master.GetIdol(idolId);` → `"育成アイドル: " + idol.Name + " [" + DisplayText.RarityName(idol.Rarity) + "] " + DisplayText.PlanName(idol.Plan)`
  - `SetSlots`: 枠 `i`（0〜4）ごとに、`slots[i]` が `null` でなければ `cache.Current.SupportCards` から `SupportCardUid == slots[i]` のものを探す。見つからなければ `slots[i] = null` にする。見つかれば `var info = master.GetSupportCard(owned.CardId);` → `new FormationSlotViewData("枠" + (i + 1) + ": " + info.Name + " [" + DisplayText.RarityName(info.Rarity) + "] Lv " + owned.Level, true)`。空は `new FormationSlotViewData("枠" + (i + 1) + ": （空）", false)`
  - `SetScenario`: `master.Scenarios.Count == 0` なら `("シナリオ: なし", false)`。そうでなければ `("シナリオ: " + master.Scenarios[scenarioIndex].Name, master.Scenarios.Count >= 2)`
  - `SetStartEnabled(idolId != 0 && master.Scenarios.Count > 0)`
- `SelectIdolAsync`（実行中フラグ・`try`/`catch`/`finally`。以下の `UniTask` のメソッドも同じ。catch は決めたこと 12 の例外の書き方）:
  1. `var request = OwnedSelectionRequest.ForIdol();`
  2. `var presenter = await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(request), playAnimation: false);`
  3. `var selection = await navigator.WaitForPopAsync<OwnedSelection>(presenter);` `disposed` なら終わる（編成のページが別の理由で閉じられた）。`selection == null` か `selection.IsCancelled` か `selection.IdolId == idolId` なら終わる
  4. `idolId = selection.IdolId`。`var plan = master.GetIdol(idolId).Plan;` 枠ごとに、埋まっていて `!FormationValidator.IsPlanCompatible(plan, master.GetSupportCard(owned.CardId).Plan)` のものを `null` にする（`owned` は所持の中のそのカード。見つからない枠も `null`）
  5. `Refresh()`。4 で 1 枠でも外したら、そのあと `await dialogs.ShowInfoAsync("プランが合わないサポートカードを外しました。");`
- `SelectSlotAsync(index)`:
  1. `index` が 0〜4 の外なら `ArgumentOutOfRangeException`（catch で受ける）
  2. `idolId == 0` なら `await dialogs.ShowInfoAsync("先に育成アイドルを選んでください。");` して終わる
  3. `excludedUids` = 枠 `index` 以外の埋まっている所持ID（枠の順）、`excludedSupportCardIds` = その所持のカードの `OwnedSupportCard.CardId`（枠の順）
  4. `var request = OwnedSelectionRequest.ForSupportCard(master.GetIdol(idolId).Plan, excludedUids, excludedSupportCardIds);` → 育成アイドルと同じ 2・3 の手順（`PushPageAsync<OwnedPresenter, OwnedArgs>` → `WaitForPopAsync<OwnedSelection>` → `disposed` なら終わる）。`null` かキャンセルなら終わる
  5. `slots[index] = selection.SupportCardUid;` `Refresh()`
- `ClearSlot(index)`: `running` なら何もしない。`index` が範囲外なら何もしない。`slots[index] = null;` `Refresh()`
- `NextScenario()`: `running` か `master.Scenarios.Count < 2` なら何もしない。`scenarioIndex = (scenarioIndex + 1) % master.Scenarios.Count;` `Refresh()`
- `StartAsync`: `idolId == 0` か `master.Scenarios.Count == 0` なら何もしない。`uids` = `slots` の `null` でないものを枠の順に（`List<string>`）。`var status = await start.ExecuteAsync(idolId, uids, master.Scenarios[scenarioIndex].ScenarioId);` `disposed` なら終わる
  - `Succeeded`: `await scenes.GoToProduceAsync();`（`Home` シーンごと編成のページも破棄される。以後は View に触らない）
  - `InvalidFormation`: `await dialogs.ShowInfoAsync("編成が正しくありません。");`
  - `AlreadyInProgress`: `await dialogs.ShowInfoAsync("進行中のプロデュースがあります。ホームから再開してください。");` → `await navigator.PopPageAsync(this, playAnimation: false);`（ユースケースが取り直し済みなので、ホームの `DidPopEnter` で「再開」が出る）
  - `InvalidArgument`: `await dialogs.ShowInfoAsync("開始できませんでした。");`
  - `Failed`: 何も出さない（共通のダイアログが出ている）
- `BackAsync`: `await navigator.PopPageAsync(this, playAnimation: false);`

**`OwnedArgs` について**: 3 本目（owned）が作る `OwnedArgs(OwnedSelectionRequest selection)`（`null` は閲覧モード）と `OwnedPresenter : IScreenWithArgs<OwnedArgs>` を使う。選ばれたら `OwnedPresenter` が自分を Pop し、結果（`OwnedSelection`）は基盤の `WaitForPopAsync` で返る。戻る・画面の破棄では `OwnedSelection.Cancelled`（3 本目の決まり）。`FakeScreenNavigator` は `PopResults` が空なら `null` を返すので、`null` はキャンセルと同じに扱う。

### 3.6 ホーム（`HomePresenter` の変更）

- コンストラクター（2 本）の**最後**（2 本目が足した `IScreenNavigator navigator` の後ろ）に `ProduceSession session, ResumeProduceUseCase resume, AbandonProduceUseCase abandon, ISceneNavigator scenes` を足す（`ProduceFlow` は `Produce` シーンのスコープにあり、`Home` からは受けられない: 決めたこと 3）。足したあとの形: `[Inject] HomePresenter(HomePage view, PlayerCache cache, DialogService dialogs, IGameMaster master, IScreenNavigator navigator, ProduceSession session, ResumeProduceUseCase resume, AbandonProduceUseCase abandon, ISceneNavigator scenes) : this((IHomeView)view, view.CreateStage(master), cache, dialogs, master, navigator, session, resume, abandon, scenes)` と、テスト用の `HomePresenter(IHomeView view, ICharacterStage stage, PlayerCache cache, DialogService dialogs, IGameMaster master, IScreenNavigator navigator, ProduceSession session, ResumeProduceUseCase resume, AbandonProduceUseCase abandon, ISceneNavigator scenes)`（foundation 3.7。ステージの選び直しの `RefreshStage` は変えない）
- `Refresh()` の `view.SetResumeVisible(...)` の引数を `HasActiveProduce()` にする（`HomePage.SetResumeVisible` が帯の表示と主ボタンのラベル「再開」／「プロデュース」を切り替える: foundation 3.7）。`private bool HasActiveProduce()`: `session.Current != null ? session.HasActive : cache.Current.HasActiveProduce`
- foundation の `OpenProduceAsync`（「準備中です。」）を次に置き換える（実行中フラグ・`try`/`catch`/`finally`。catch は決めたこと 12）。主ボタン（進行中は「再開」の表示）とナビの「プロデュース」はどちらも `ProduceClicked` → `OpenProduceAsync`（foundation の決めたこと 17。`ResumeClicked`・`ResumeProduceAsync` は無い）:
  - `OpenProduceAsync`: `HasActiveProduce()` が false なら `await navigator.PushPageAsync<FormationPresenter>(playAnimation: false);`。true なら `var index = await dialogs.ChooseAsync("進行中のプロデュースがあります。", new[] { "再開", "放棄", "やめる" });` `disposed` なら終わる。0 → `await ResumeCoreAsync();`、1 → `ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？")` が `Positive` なら `await abandon.ExecuteAsync();` のあと `disposed` でなければ `Refresh()`（結果が false でも呼ぶ。共通ダイアログは出ている）、2・−1 → 何もしない
  - `private async UniTask ResumeCoreAsync()`: `var status = await resume.ExecuteAsync();` `disposed` なら終わる。`Resumed` → `await scenes.GoToProduceAsync();`（以後は View に触らない）、`NotInProgress` → `await dialogs.ShowInfoAsync("進行中のプロデュースはありません。");` のあと `disposed` でなければ `Refresh()`、`Failed` → 何もしない
- `using Uta.Client.Application.Produce; using Uta.Client.Application.Navigation; using Uta.Client.Presentation.Formation;` と、3.3.10 の別名 `using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;` を足す
- `HomeLifetimeScope`・`HomeEntry` は変えない（`FormationPresenter` は基盤が `Home` のスコープの子スコープで解決する。依存はルートと `Home` のスコープにある）
- `Produce` から `Home` に戻ると、`HomeEntry` が `HomePresenter` を積み直し、`InitializeAsync` の `Refresh()` が `ProduceSession`（ルート）を見て「再開」を出し分ける

### 3.7 テスト用の偽と組み立て（`Tests/EditMode/Fakes/`。5・6 本目も使う）

```csharp
public sealed class FakeProducePort : IProducePort
{
    public FakeProducePort(CallLog log);
    /// <summary>StartAsync・GetStateAsync・4 つの操作が、呼ばれた順に共有して取り出す。空なら InvalidOperationException("FakeProducePort: no state result")。</summary>
    public Queue<PortResult<ProduceProgress>> StateResults { get; }
    /// <summary>AbandonAsync が取り出す。空なら InvalidOperationException("FakeProducePort: no abandon result")。</summary>
    public Queue<PortResult<ProduceOutcome>> AbandonResults { get; }
}
```

記録（`log.Add`。数値は `ToString()`、enum は名前、bool は `True`/`False`）:
- `StartAsync` → `"Start:" + idolId + ":" + string.Join(",", supportCardUids) + ":" + scenarioId`
- `GetStateAsync` → `"GetState"`、`AbandonAsync` → `"Abandon"`
- `ChooseWeekActionAsync` → `"ChooseWeekAction:" + op + "," + slot + "," + option + "," + param`
- `ResolveCardOfferAsync` → `"ResolveCardOffer:" + op + "," + kind + "," + offerIndex`
- `DecideExamRetryAsync` → `"DecideExamRetry:" + op + "," + retry`
- `ExecuteLessonActionAsync` → `"ExecuteLessonAction:" + op + "," + kind + "," + cardInstanceId + "," + drinkSlotIndex`

```csharp
/// <summary>
/// IPlayerPort の偽（サインインを CallLog に記録する）。foundation の StubPlayerPort（記録しない）とは別に作る:
/// この計画のテストは「RPC → "SignIn"」の順を確かめるため。
/// </summary>
public sealed class FakePlayerPort : IPlayerPort
{
    public FakePlayerPort(CallLog log);                       // log は null を許す（記録しない）
    /// <summary>SignInAsync が返す値。空なら PlayerSignInResult.Success(Snapshot)。</summary>
    public Queue<PlayerSignInResult> SignInResults { get; }
    /// <summary>既定の戻り値のスナップショット。初期値は TestProduce.Player()。</summary>
    public PlayerSnapshot Snapshot { get; set; }
    public int SignInCount { get; }
    // SignInAsync: log?.Add("SignIn"); SignInCount を 1 増やす。SignUpAsync: NotSupportedException
}

public static class TestProduce
{
    /// <summary>scenarioId = 1、idolId = 1、examRetryLeft = 1。null の配列は空のリスト。</summary>
    public static ProduceProgress Progress(ProducePhase phase = ProducePhase.WeekSelect, int operationNumber = 0, int week = 1,
        int singing = 100, int rhythm = 100, int expression = 100, int hp = 30, int maxHp = 30,
        IReadOnlyList<ProduceDeckCard> deck = null, IReadOnlyList<ProduceWeekAction> weekActions = null,
        IReadOnlyList<ProduceClassOption> classOptions = null, IReadOnlyList<int> cardOffers = null,
        ProduceLesson lesson = null, ProduceExamResult examResult = null, ProduceOutcome result = null);
    /// <summary>ProduceEndingKind.Abandoned、ほかは 0（playerLevel = 1）。</summary>
    public static ProduceOutcome Abandoned();
    /// <summary>level = 1、exp・gold・supportPoints = 0、discardedProduce = false。null のリストは空。</summary>
    public static PlayerSnapshot Player(int gachaCurrency = 0, bool hasActiveProduce = false,
        IReadOnlyList<OwnedIdol> idols = null, IReadOnlyList<OwnedSupportCard> supportCards = null);
}
```

- 既存のテスト（`EnterPlayerUseCaseTests` など）の入れ子の `private sealed class FakePlayerPort` は変えない（入れ子のほうが優先されるので、名前は衝突しない）

## 4. タスク

各タスクの終わりに、`dotnet build Uta.slnx`（server・shared を触ったタスク。警告 0・エラー 0）、`unity recompile --format json`（client を触ったタスク。`client/uta` で実行。コンパイルエラー 0）、そのタスクまでのテストが通る状態にする。EditMode のテストは Editor が要る（回し方は P1 の決めたこと 12: Editor を開いたまま `TestRunnerApi` を eval で回す。コマンドは CLAUDE.md「ビルド・テスト・実行」・unity7-evaluation 4章）。**eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない**（P1 の決めたこと 12）。**.NET SDK・Docker・Unity の Editor が無い環境では、通ったと書かず、確かめていないと報告する**。TMP の文字を EditMode で入れるテストは無いが、Play のあとでコミットする前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`（P1 の `FontAssets`）を実行する（foundation 3.3.7）。

### タスク 1: サーバー・shared の `UsableHand` と wiki

触る: `LessonDto.cs`、`ProduceView.cs`、`ProduceViewBuilder.cs`、`ProduceResponseMapper.cs`、`ProduceOperationTests.cs`、`produce-server.md`、`lesson-domain-types.md`、`produce-domain.md`。

手順:
1. 3.1 のとおり直す
2. `ProduceOperationTests.cs` に 5 章の 2 つを足す（既存の補助メソッド `StartAsync(postgres)`・`StartProduceAsync`・`StartLessonAsync`・`GetStateAsync` を使う）
3. [produce-server.md](../wiki/architecture/server/produce-server.md):
   - 6章の `LessonDto` の行の `UsableHand` の括弧の最後（「2026-11-03 追加」）を「2026-11-03 追加。**Key は末尾の 19**（既存のキーを動かさないため。表は意味の近さで `Hand` の次に置く）」に直す。行を足さない（すでにある）
   - 5章の「破棄(`SignInAsync`・`StartAsync` の中)」の「楽観ロックつき」の後ろに「。0 行なら `PRODUCE_STALE_OPERATION`」を、「`AbandonAsync`」の「(楽観ロックつき)」を「(楽観ロックつき。0 行なら `PRODUCE_STALE_OPERATION`)」に直す
   - 実装メモの最後に「- **`UsableHand`** [2026-11-03。計画 `plans/2026-11-03-client-screens-formation.md`]: `ProduceViewBuilder` が `ProduceEngine.CanUse(state, id)` で作る（`Lesson` が無ければ空）。`Uta.Shared.Domain` に公開メソッドは足していない」を足す
4. lesson-domain-types.md 5章の `LessonEngine` の行の「`CanUse`（乱数なし。クライアントの予測にも使う。**MVP のクライアントの予測は使用可否だけ**で、効果の見込みは出さない [決定。2026-11-01。ユーザーの判断]）」を「`CanUse`（乱数なし。サーバーが `LessonDto.UsableHand` を作るのに使う。**MVP のクライアントは使用可否もサーバーの `UsableHand` を使い、`CanUse` を呼ばない** 決定。2026-11-03。[screen-design.md 6章]。効果の見込みは出さない [決定。2026-11-01。ユーザーの判断]）」に直す
5. produce-domain.md 1章の最初の箇条書き「…Shared.Domain で使用可否の判定と予測の表示までを行う。…」の文の後ろに「（MVP の使用可否はサーバーの `LessonDto.UsableHand` を使う: screen-design.md 6章）」を足す

確かめ方: `dotnet build Uta.slnx`、`dotnet test --project shared/Uta.Shared.Domain.Tests`（既存が壊れていない）、`dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）。`python -I .claude/skills/linting-wiki/lint_wiki.py` のエラーが 0。

### タスク 2: Application の型・ポート・ユースケースと偽

触る: `Application/Produce/*`（2 章の 2 行）、`Application/Common/ServerReasons.cs`・`Tests/EditMode/ServerReasonsTests.cs`（定数が無いときだけ。足した定数の行をテストにも足す）、`Tests/EditMode/Fakes/FakeProducePort.cs`・`FakePlayerPort.cs`・`TestProduce.cs`、`Tests/EditMode/ProduceSessionTests.cs`・`ProduceProgressTests.cs`・`StartProduceUseCaseTests.cs`・`ResumeProduceUseCaseTests.cs`・`AbandonProduceUseCaseTests.cs`・`ProduceActionsTests.cs`。

手順: 3.2・3.7 のとおり作る。
境界: `ProduceSession.Set(null)` は `ArgumentNullException`。`ProduceProgress.GetParam` は `Singing`・`Rhythm`・`Expression` 以外で `ArgumentOutOfRangeException`。写しの型の配列の引数が `null` なら `ArgumentNullException`。`ProduceActions` は `session.Current == null` で `InvalidOperationException`。
確かめ方: 5 章のタスク 2 の行のテスト。

### タスク 3: API・ゲートウェイ

触る: `Infrastructure/Api/ProduceApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Produce/ProduceGateway.cs`、`Tests/EditMode/ProduceGatewayTests.cs`。
手順: 3.3 のとおり。
確かめ方: `ProduceGatewayTests`。`unity recompile` のあと `unity command console get --tail 50 --level error --format json` のエラーが 0。

### タスク 4: ProduceFlow・ProduceMenu・背景のページ・編成

触る: `Presentation/Produce/ProduceFlow.cs`・`ProduceMenu.cs`・`IProduceBackdropView.cs`・`ProduceBackdropPage.cs`・`ProduceBackdropPresenter.cs`、`Presentation/Formation/IFormationView.cs`・`FormationSlotViewData.cs`・`FormationPage.cs`・`FormationSlotRowView.cs`・`FormationPresenter.cs`、`Tests/EditMode/ProduceFlowTests.cs`・`ProduceMenuTests.cs`・`ProduceBackdropPresenterTests.cs`・`FormationPresenterTests.cs`。

手順:
1. P3 の骨格（`FormationPage.cs`・`FormationSlotRowView.cs`・`ProduceBackdropPage.cs`）のフィールドが P3 3.1 と同じ名前・型であることを確かめる。違えば止まって報告する
2. 3.4（`ProduceEntry`・`ProduceLifetimeScope` を除く）・3.5 のとおり作る。骨格のフィールド宣言は変えず、メソッド・イベント・インターフェースだけを足す（Prefab の作り直しは要らない）
3. `unity recompile --format json` のエラーが 0。P1 の `PrefabContractTests` と P3 の `ProducePrefabTests` が引き続き通る

確かめ方: 5 章のタスク 4 の行のテスト。

### タスク 5: シーンの接続と登録

触る: `Presentation/Produce/ProduceEntry.cs`・`ProduceLifetimeScope.cs`・`ProduceView.cs`（削除）・`ProducePresenter.cs`（削除）、`Editor/SceneBuilder.cs`、`Scenes/Produce.unity`（生成）、`Presentation/Home/HomePresenter.cs`、`Bootstrap/BootstrapEntry.cs`、`Tests/EditMode/ProduceEntryTests.cs`・`HomePresenterTests.cs`。
手順:
1. 3.4 のとおり `ProduceEntry` を作り、`ProduceLifetimeScope` を置き換え（`uiRoot`・`SceneUiInstaller.Install` と 3 行）、`ProduceView.cs`・`ProducePresenter.cs` を `.meta` ごと消す。`SceneBuilder.BuildProduce` を 3.4 のとおり直し、legacy の補助と `using` を消す
2. 3.6 のとおり `HomePresenter` を直す
3. `BootstrapEntry.RegisterUseCases`（foundation 3.8）の最後に、次の 6 行を足す（`using Uta.Client.Application.Produce; using Uta.Client.Infrastructure.Produce;`）:
   ```csharp
   builder.Register<ProduceGateway>(Lifetime.Singleton).As<IProducePort>();
   builder.Register<ProduceSession>(Lifetime.Singleton);
   builder.Register<StartProduceUseCase>(Lifetime.Singleton);
   builder.Register<ResumeProduceUseCase>(Lifetime.Singleton);
   builder.Register<AbandonProduceUseCase>(Lifetime.Singleton);
   builder.Register<ProduceActions>(Lifetime.Singleton);
   ```
   `ProduceFlow`・`ProduceMenu` はルートに登録しない（1 の `ProduceLifetimeScope`）。Presenter はどこにも登録しない（foundation 3.3.2）
4. `HomePresenterTests` の既存のテストの `HomePresenter` の生成に、新しい引数（`new ProduceSession()`、`resume`・`abandon` は `null`、`scenes` は同じ `CallLog` の `FakeSceneNavigator`）を足す。5 章の追記のテストと `ProduceEntryTests` を作る。**追記のテスト（と差し替えの 2 本）では、本物の `AbandonProduceUseCase`・`ResumeProduceUseCase` を、同じ `CallLog` の `FakeProducePort` と `new RefreshPlayerUseCase(new FakePlayerPort(log), cache)` で作る**（既存のテストでは `null` のまま）
5. `unity recompile --format json` のエラーが 0 になってから、`Uta/Create Scenes`（`unity command eval` で `Uta.Client.Editor.SceneBuilder.CreateScenes()`、または メニュー）でシーンを作り直す。`Produce.unity` に `UiRoot` と `ProduceScope`（`uiRoot` が入っている）があることを確かめる。`Bootstrap`・`Title`・`Home` の `.unity` に差分が出たら、中身が foundation の形のままであることを確かめてからコミットする

確かめ方: `HomePresenterTests`（既存と追記）、`ProduceEntryTests`。

### タスク 6: 通信の確認と記録

1. **Editor の Play モード**（サーバーを起動した状態。CLAUDE.md「ローカルのサーバー」）: `unity command editor.playmode start` のあと、手で次を確かめる: ホーム →「プロデュース」→ 編成（`Home` シーンの上。既定の育成アイドルが入っている。「育成アイドルを選ぶ」で所持一覧の選択モードが開き、選ぶと戻ってラベルが変わる。サポートカードの枠の「選ぶ」で 1 枚入れ、「外す」で外せる。シナリオが 2 つ以上なら「変更」で切り替わる）→「プロデュース開始」→ `Produce` シーンに移り、背景のページ（「週 1」とパラメータ・体力）の上に「この局面の画面は準備中です: WeekSelect」が出る → 閉じると `Home` シーンに戻り「再開」が出る。「再開」→ 同じく `Produce` → ダイアログ → `Home`。「プロデュース」→「進行中のプロデュースがあります。」→「放棄」→ 確認 →「再開」が消える。編成の「戻る」でホームに戻る。終わったら `unity command console get --tail 50 --level error --format json` のエラーが 0 → `unity command editor.playmode stop`
2. **Windows の IL2CPP**（CLAUDE.md の「ビルド・テスト・実行」のビルドのコマンド。Editor を閉じてから。`BuildPlayerContent` は P1 が `BuildCommands` に入れ済み）: できた `client/uta/Build/Windows/Uta.exe` を `-batchmode` なしで起動し、上の 1 と同じ操作を手で確かめる。結果を [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 2章の表に 1 行で足す: 「`IProduceService` の `StartAsync`・`GetStateAsync`・`AbandonAsync`（IL2CPP）と、`Home` ↔ `Produce` のシーンの往復。確かめた範囲は局面 `WeekSelect` の応答（最上位の値・enum、`Deck`・`WeekActions`・`ClassOptions`・`CardOffers` の配列、`Lesson`・`ExamResult`・`Result` は `null`）と `ProduceResultResponse`（放棄）。`LessonDto`（`bool[]` を含む）・`ExamResultDto` は未確認（6 本目）」。通らなければ止まって報告する。Editor を閉じられない・ビルドできない環境では、確かめていないと報告する
3. [screen-design.md](../wiki/architecture/screen-design.md)（frontmatter の `updated` を実施日に）を直す:
   - 7.6 の 1 つ目の箇条書きの「`ProduceSession`（最新の `ProduceStateView` と次の操作番号）」を「`ProduceSession`（最新の `ProduceProgress`（応答の写し）。次の操作番号は `Current.OperationNumber`。操作の前後の差は `Previous` と `Current`）」に直す
   - 7.6 の 3 つ目の箇条書きを「`PRODUCE_STALE_OPERATION`・`PRODUCE_UNEXPECTED_PHASE` を受けたら `GetStateAsync` で取り直して同じ流れに戻す。`PRODUCE_SESSION_DISCARDED`・`PRODUCE_NOT_IN_PROGRESS` を受けたら、セッションを消して所持データを取り直し、`Home` シーンへ戻る」に直す
   - **7.6 で直すのは上の 2 つの箇条書きだけ**。プロデュースの画面の流れ（背景のページ・局面の切り替え・スキルカード獲得のモーダルの出し方）を 7.6 に足すこと、[screen-wireframes.md](../wiki/overview/screen-wireframes.md) の直しは、この計画ではしない。スキルカード獲得のモーダルを背景のページの上に出すこと（week の決めたこと 15。[索引](2026-11-07-client-screens-index.md) 確認 15）は 2026-11-07 にユーザーが了承済みだが、7.6 と screen-wireframes を直す仕事は week のタスク 5 が実装時に行う
   - 8章の表の `Home` の行の役割の「ここから `Produce` へ」を「編成（`FormationPage`）はこのシーンのページ。開始・再開で `Produce` へ」に、`Produce` の行の役割を「プロデュース。底に背景のページを置き、局面の画面を積む（7.6）。ホームへ戻る・放棄・終了で `Home` へ」に直す（foundation のタスク 6 がすでに書き換えていれば、その文に合わせて「空」などの古い記述だけを直す）
   - 「実装メモ」に、設計とのズレを書く（なければ「順 7 の 4 本目: なし」）
4. produce-server.md の実装メモの「未確認: Unity 7(IL2CPP)での `Uta.Shared.Contracts/Produce` のコンパイルと MessagePack の動作」を、2 の結果に合わせて「…`WeekSelect` の応答までは確かめた（unity7-evaluation.md 2章）。`LessonDto`・`ExamResultDto` は未確認」に直す（確かめられなかったら直さない）
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` のエラーが 0。この計画を `done` にする

## 5. テスト

EditMode のテストは foundation 3.3.7 の書き方（偽の View・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`・`FakeProducePort`・`FakePlayerPort` を同じ `CallLog` で作り、`log.Entries` の並びで確かめる。非同期は `[UnityTest]` + `UniTask.ToCoroutine`）。`RefreshPlayerUseCase` は本物を `new RefreshPlayerUseCase(fakePlayerPort, cache)` で作る。`ProduceFlow` は本物を `new ProduceFlow(session, navigator, pageStack, dialogs, scenes)`（`pageStack` は `new PageStack(() => ページ数, () => モーダル数)`）で作る。View（`FormationPage`・`ProduceBackdropPage`）のテストは書かない（foundation 3.3.7。参照の欠けは `PrefabContractTests`）。

**偽の View の記録の形式**（各テストのファイルの中の `private sealed class`。`log.Add` の文字列。数値は `ToString()`、bool は `True`／`False`）:

| 偽 | メソッド | 記録 |
|---|---|---|
| `FakeFormationView : IFormationView`（`FormationPresenterTests.cs`） | `SetIdol(label)` | `"SetIdol:" + label` |
| | `SetSlots(slots)` | `"SetSlots:" + string.Join("/", slots.Select(s => s.Label))`（例 `"SetSlots:枠1: （空）/枠2: （空）/枠3: （空）/枠4: （空）/枠5: （空）"`）。`Filled` は記録しない。最後に渡された `slots` を `LastSlots` プロパティに持つ（`Filled` を確かめるテスト用） |
| | `SetScenario(label, canChange)` | `"SetScenario:" + label + "," + canChange` |
| | `SetStartEnabled(enabled)` | `"SetStartEnabled:" + enabled` |
| | イベント | `RaiseIdolClicked()`・`RaiseSlotClicked(int)`・`RaiseSlotClearClicked(int)`・`RaiseScenarioClicked()`・`RaiseStartClicked()`・`RaiseBackClicked()` で起こす（記録しない） |
| `FakeBackdropView : IProduceBackdropView`（`ProduceBackdropPresenterTests.cs`） | `SetHeader(text)` | `"SetHeader:" + text` |
| `FakeHomeView : IHomeView`（`HomePresenterTests.cs`。foundation の既存の偽をそのまま使う） | `SetProfile`・`SetResumeVisible` | foundation の形のまま（`"SetProfile:12,3250"`・`"SetResumeVisible:True"`）。`RaiseProduceClicked()` も foundation のまま。ステージの偽 `FakeCharacterStage`（`"ShowIdol:…"`・`"ClearStage"`）と `StubIdolMaster` も foundation のまま |

- 「`SetIdol` が増えない」は、`log.Entries` の中の `"SetIdol:"` で始まる行の数を、操作の前後で比べる

| ファイル | テスト | 内容 |
|---|---|---|
| `ProduceOperationTests.cs`（server、結合。タスク 1） | `Lesson_Response_HasUsableHandSameLengthAsHand` | `var lesson = await StartLessonAsync(player);` → `Assert.NotNull(lesson.Lesson.UsableHand)`、`Assert.Equal(lesson.Lesson.Hand.Length, lesson.Lesson.UsableHand.Length)`。続けて `GetStateAsync(player, "req-5")` の応答でも同じ長さ |
| | `WeekSelect_Response_HasNoLesson` | `StartProduceAsync(player, "req-3")` の応答の `Lesson` が `null`（`UsableHand` を読む経路が無い） |
| `ServerReasonsTests.cs`（変更。タスク 2。定数を足したときだけ） | `Reasons_MatchContractsErrorCodes` に、足した定数の行を足す | `ServerReasons.ProduceStaleOperation == ProduceErrors.StaleOperation.Reason`、`ProduceUnexpectedPhase`・`ProduceInvalidArgument`・`ProduceInvalidFormation`・`ProduceNotInProgress`・`ProduceSessionDiscarded`・`ProduceAlreadyInProgress` も同じ（`ProduceErrors` の同名から `Produce` を除いたもの。`using Uta.Shared.Contracts.Produce;`） |
| `ProduceSessionTests.cs`（タスク 2） | `Set_UpdatesCurrentAndPrevious` | 2 回 `Set` → `Previous` が 1 回目、`Current` が 2 回目 |
| | `Set_ResetsExamResultShown` / `Set_Null_Throws` / `Clear_ResetsAll` | |
| | `HasActive_ByPhase` | `Current == null`・`Nothing`・`Finished` は false、`WeekSelect` は true |
| `ProduceProgressTests.cs` | `GetParam_ReturnsValues` / `GetParam_Nothing_Throws` | |
| | `Lesson_IsExam_ByKind` | `MidExam`・`FinalExam` で true、`Normal` で false |
| `StartProduceUseCaseTests.cs` | `Execute_Success_SetsSession` | `log` が `"Start:1:u1,u2:2"`、`session.Current` が戻り値 |
| | `Execute_InvalidFormation_ReturnsStatus` / `Execute_InvalidArgument_ReturnsStatus` | |
| | `Execute_AlreadyInProgress_RefreshesAndReturnsStatus` | `log` が `"Start:…"`, `"SignIn"` |
| | `Execute_OtherFailure_ReturnsFailed` | `PortResult.Failure("x")` → `Failed`、`SignIn` なし |
| `ResumeProduceUseCaseTests.cs` | `Execute_Active_SetsSessionAndReturnsResumed` | |
| | `Execute_PhaseNothing_ClearsAndRefreshes` | `session.Current == null`、`"SignIn"` |
| | `Execute_Failure_ReturnsFailed` | |
| `AbandonProduceUseCaseTests.cs` | `Execute_Success_ClearsAndRefreshes` | `true`、`"Abandon"`, `"SignIn"` |
| | `Execute_NotInProgress_ClearsAndReturnsTrue` / `Execute_SessionDiscarded_ClearsAndReturnsTrue` | |
| | `Execute_OtherFailure_ReturnsFalse` | `Failure` → `false`、`session` は残る |
| | `Execute_RefreshFails_StillTrue` | `FakePlayerPort.SignInResults` に `PlayerSignInResult.Failure("x")` → `true`（例外にならない） |
| `ProduceActionsTests.cs` | `ChooseWeekAction_Success_SetsSessionAndPassesOperationNumber` | `session.Current.OperationNumber = 4` → `"ChooseWeekAction:4,1,-1,Rhythm"` |
| | `AllOperations_MapToPort` | 4 つの操作の記録が `"ChooseWeekAction:…"`・`"ResolveCardOffer:0,Pick,2"`・`"DecideExamRetry:0,True"`・`"ExecuteLessonAction:0,UseCard,7,0"` |
| | `Operation_Finished_RefreshesPlayer` | `Phase == Finished` の応答のあと `"SignIn"`、`PlayerCache.Current` が差し替わる |
| | `Operation_Stale_RecoversWithGetState` / `Operation_UnexpectedPhase_RecoversWithGetState` | `Recovered`、`"GetState"`、`session.Current` が 2 つ目の値 |
| | `Operation_StaleThenGetStateNothing_SessionEnded` | `session.Current == null`、`"SignIn"` |
| | `Operation_StaleThenGetStateFails_Failed` | |
| | `Operation_SessionDiscarded_ClearsAndSessionEnded` / `Operation_NotInProgress_ClearsAndSessionEnded` | |
| | `Operation_InvalidArgument_Rejected` / `Operation_OtherFailure_Failed` | |
| | `Operation_NoSession_Throws` | `InvalidOperationException` |
| `ProduceGatewayTests.cs`（タスク 3） | `ToProgress_MapsAllFields` | 入れ子の `LessonDto`（`UsableHand` 含む）・`ExamResultDto`・`ProduceResultResponse` を写す。入力と期待（代表値。ほかの値は 0・空の配列でよい）: **①** `ProduceStateResponse(operationNumber: 7, phase: ProducePhase.Exam, scenarioId: 2, idolId: 3, week: 9, singing: 120, rhythm: 130, expression: 140, hp: 25, maxHp: 30, examRetryLeft: 1, deck: { new DeckCardDto(1, 101, true) }, …)` → `OperationNumber == 7`・`Phase == Exam`・`GetParam(ParamKind.Rhythm) == 130`・`Deck[0]` が `(1, 101, true)`。**②** `LessonDto(kind: LessonKind.MidExam, targetParam: ParamKind.Singing, maxTurns: 6, clearTarget: 0, perfectTarget: 0, examTurns: { new ExamTurnDto(ParamKind.Rhythm, 150) }, turn: 2, shield: 3, usesLeft: 1, paramGained: 0, hand: { 11, 12 }, …, npcScores: { 900, 800, 700 }, usableHand: { true, false })` → `Lesson.IsExam == true`・`Lesson.Hand` が `{ 11, 12 }`・`Lesson.UsableHand` が `{ true, false }`・`Lesson.ExamTurns[0].Param == Rhythm`・`ScoreBonusPercent == 150`・`Lesson.NpcScores` が `{ 900, 800, 700 }`。**③** `ExamResultDto(ExamKind.Mid, 2, 1234, 1, { 1000, 900, 800 }, true)` → `ExamResult` の `Kind == Mid`・`Attempt == 2`・`Score == 1234`・`Rank == 1`・`Passed == true`。`ProduceResultResponse(ProduceEnding.Completed, ExamOutcome.FinalPassed, 5000, ProduceRank.B, 4321, 2, 300, 1500, 40, 120, 5, 30)` → `Result.Ending == ProduceEndingKind.Completed`・`Rank == ProduceRank.B`・`Gold == 1500`・`PlayerLevel == 5`（DTO の引数の並びは `shared/Uta.Shared.Contracts/Produce/` の各コンストラクターのとおり。`usableHand` はタスク 1 で末尾に足したもの） |
| | `ToProgress_PhaseNothing_ReturnsEmpty` | 配列は空、`Lesson`・`ExamResult`・`Result` は `null` |
| | `ToProgress_NullArrays_BecomeEmpty` | `UsableHand == null` も空 |
| | `ToOutcome_MapsEnding` | `ProduceEnding.Abandoned` → `ProduceEndingKind.Abandoned` |
| `ProduceFlowTests.cs`（タスク 4） | `ShowCurrent_UnsupportedPhase_PopsToBackdropThenShowsInfoAndGoesHome` | `PageStack(() => 2, () => 0)`（背景・局面のページ）。`log` が `"PopPage:1"`, `"Info:この局面の画面は準備中です: WeekSelect"`, `"GoToHome"` |
| | `ShowCurrent_ModalOpen_PopsModalFirst` | `PageStack(() => 1, () => 1)` → `"PopModal:1"`, `"Info:この局面の画面は準備中です: WeekSelect"`, `"GoToHome"`（`"PopPage:"` の行が無い） |
| | `ShowCurrent_NoSession_Throws` | `InvalidOperationException` |
| | `ExitToHome_GoesHomeWithoutPop` | `PageStack(() => 3, () => 1)` → `log` が `"GoToHome"` だけ（`"PopPage:"`・`"PopModal:"`・`"SignIn"` が無い） |
| | `HandleStep_SessionEnded_ShowsInfoThenGoesHome` | `"Info:プロデュースは終了しています。"`, `"GoToHome"` |
| | `HandleStep_Rejected_ShowsInfo` / `HandleStep_Failed_DoesNothing` | |
| | `HandleStep_SucceededAndRecovered_ShowCurrent` | どちらも `PageStack(() => 2, () => 0)` で `"PopPage:1"`, `"Info:この局面の画面は準備中です: …"`, `"GoToHome"` |
| `ProduceMenuTests.cs` | `Show_GoHome_ExitsWithoutAbandon` | `ChooseResults` 0 → `"Choose:メニュー:ホームへ戻る（あとで再開できます）/プロデュースを放棄する/閉じる"`, `"GoToHome"`。`"Abandon"` が無い |
| | `Show_AbandonConfirmed_AbandonsThenExits` | 1・Positive → `"Confirm:放棄すると報酬は受け取れません。放棄しますか？"`, `"Abandon"`, `"SignIn"`, `"GoToHome"` |
| | `Show_AbandonDeclined_DoesNothing` / `Show_AbandonFailed_DoesNotExit` / `Show_Close_DoesNothing` | `"GoToHome"` が無い |
| `ProduceBackdropPresenterTests.cs` | `Initialize_SetsHeader` | `"SetHeader:" + BuildHeader(...)`。`BuildHeader` の文字列が 3.4 のとおり（週 3、100/110/120、体力 25 / 30） |
| | `DidPopEnter_SetsHeaderFromLatest` | `InitializeAsync` のあと `session.Set(週 4 の Progress)` → `DidPopEnter()` → 最後の `"SetHeader:"` が週 4 |
| | `Initialize_NoSession_DoesNotSetHeader` | `session.Current == null` → `"SetHeader:"` の行が無い |
| `FormationPresenterTests.cs` | `Initialize_DefaultsToHighestRarityIdol` | `FakeGameMaster` に育成アイドル 1（SR・Sense）・2（SSR・Logic）・3（SSR・Sense）、所持 1・2・3 → `"SetIdol:育成アイドル: <2 の名前> [" + DisplayText.RarityName(Rarity.SSR) + "] " + DisplayText.PlanName(Plan.Logic)` |
| | `Initialize_NoIdols_StartDisabled` | `"SetIdol:育成アイドル: 未選択"`, `"SetStartEnabled:False"` |
| | `Initialize_ShowsEmptySlotsAndScenario` | `SetSlots` の 5 件が `"枠1: （空）"`〜`"枠5: （空）"`、`"SetScenario:シナリオ: <1 つ目の名前>,True"`（シナリオ 2 つ） |
| | `SelectIdol_Selected_ChangesIdolAndClearsIncompatibleSupport` | 育成アイドル 3（Sense）で枠 0 に Sense のカード・枠 1 に Free のカード。`navigator.PopResults.Enqueue(OwnedSelection.ForIdol(2));` → `await presenter.SelectIdolAsync();` → `log` に `"PushPage:OwnedPresenter"`, `"WaitForPop"`。`((OwnedArgs)navigator.Args[0]).Selection.Kind` が育成アイドルの選択。枠 0 が空、枠 1 は残る、最後が `"Info:プランが合わないサポートカードを外しました。"` |
| | `SelectIdol_Cancelled_KeepsState` | `PopResults` に `OwnedSelection.Cancelled` → `SetIdol` が増えない。`PopResults` が空（`null`）でも同じ |
| | `SelectIdol_Disposed_DoesNothing` | `PopResults` に `ForIdol(2)`、`presenter.Dispose()` のあとに `await presenter.SelectIdolAsync();` → `SetIdol` が増えない |
| | `SelectSlot_NoIdol_ShowsInfo` | `"Info:先に育成アイドルを選んでください。"`、Push なし |
| | `SelectSlot_BuildsRequestWithExclusions` | 枠 1・2 が埋まった状態で枠 0 を選ぶ。`PopResults` に `OwnedSelection.ForSupportCard("u9")` → 枠 0 が "u9"。`((OwnedArgs)navigator.Args[0]).Selection` の `IdolPlan` が育成アイドルのプラン、`ExcludedUids` が枠 1・2 の所持ID（除外の効き目は 3 本目の `OwnedListBuilder` のテストが確かめる） |
| | `ClearSlot_ClearsSlot` / `NextScenario_Cycles` | |
| | `Start_Success_GoesToProduce` | `"Start:3:u1,u2:<ScenarioId>"`（枠 0・2 に u1・u2、枠 1 は空）のあと `"GoToProduce"`。`"PopPage:"` の行が無い |
| | `Start_InvalidFormation_ShowsInfo` | |
| | `Start_AlreadyInProgress_ShowsInfoAndPops` | `"SignIn"`, `"Info:進行中のプロデュースがあります。ホームから再開してください。"`, `"PopPage:FormationPresenter"` |
| | `Start_WhileRunning_Ignored` | `FakeDialogService.InfoGate` で 1 回目を止め（`InvalidFormation`）、2 回目の `StartAsync` は `"Start:"` を増やさない |
| | `Start_Exception_ShowsError` | `StateResults` を空にする。`StartAsync` を `await` する前に `LogAssert.Expect(LogType.Exception, new Regex("FakeProducePort: no state result"));`（`catch` の `Debug.LogException` は Test Runner では予期しないログとしてテストを失敗にするため。`using System.Text.RegularExpressions; using UnityEngine; using UnityEngine.TestTools;`。`LogAssert.ignoreFailingMessages` は使わない。5・6 本目の例外のテストも同じ書き方）→ `"Info:エラーが発生しました: FakeProducePort: no state result"` |
| | `Back_PopsSelf` | `"PopPage:FormationPresenter"` |
| `ProduceEntryTests.cs`（タスク 5） | `Start_Active_PushesBackdropThenShowsCurrent` | `session.Set(TestProduce.Progress(WeekSelect))`、`PageStack(() => 1, () => 0)` → `"PushPage:ProduceBackdropPresenter"`, `"Info:この局面の画面は準備中です: WeekSelect"`, `"GoToHome"` |
| | `Start_NoSession_GoesHome` | `session.Current == null` → `log` が `"GoToHome"` だけ |
| | `Start_Finished_GoesHome` | `Finished` → `"GoToHome"` だけ（`"PushPage:"` が無い） |
| `HomePresenterTests.cs`（差し替え。タスク 5） | `ProduceClicked_ShowsNotReady` → `ProduceClicked_NoActive_PushesFormation` | `InitializeAsync` のあと `RaiseProduceClicked()` → 後の記録が `"PushPage:FormationPresenter"` だけ（foundation の決めたこと 16。ほかのボタンのテストは変えない） |
| | `OpenProduceAsync_WhileRunning_Ignored`（gacha が直したもの）を中身だけ直す | キャッシュ `hasActiveProduce = true`、`ChooseResults` に 0 を 2 つ、`StateResults` に `Nothing` を 1 つ、`FakeDialogService.InfoGate` に未完了の源 → `OpenProduceAsync()` を 2 回呼ぶ → `"Choose:"` で始まる記録と `"GetState"` は 1 回ずつ（1 回目が「進行中のプロデュースはありません。」の `ShowInfoAsync` で止まっている間、2 回目は実行中フラグで無視される）。源を完了させる |
| `HomePresenterTests.cs`（追記。タスク 5） | `OpenProduce_NoActive_PushesFormation` | `"PushPage:FormationPresenter"` |
| | `OpenProduce_Active_Resume_GoesToProduce` | キャッシュ `hasActiveProduce = true`、`ChooseResults` 0、`StateResults` に `WeekSelect` → `"Choose:進行中のプロデュースがあります。:再開/放棄/やめる"`, `"GetState"`, `"GoToProduce"` |
| | `OpenProduce_Active_AbandonConfirmed_HidesResume` | 1・Positive、`FakePlayerPort.Snapshot` を `hasActiveProduce = false` に → `"Abandon"`, `"SignIn"`, `"SetResumeVisible:False"` |
| | `OpenProduce_Active_Cancel_DoesNothing` | 2 → `"Choose:…"` のあと何も無い |
| | `OpenProduce_Active_ResumeNotInProgress_ShowsInfoAndRefreshes` | キャッシュ `hasActiveProduce = true`、`ChooseResults` 0、`StateResults` に `Nothing` → `"Choose:…"`, `"GetState"`, `"SignIn"`, `"Info:進行中のプロデュースはありません。"`, `"SetResumeVisible:False"` |
| | `Refresh_ActiveSession_ShowsResumeEvenIfCacheInactive` / `Refresh_FinishedSession_HidesResume` | `session.Set(TestProduce.Progress(WeekSelect))` → `"SetResumeVisible:True"`、`Finished` → `False`（キャッシュが true でも） |

Play モード・IL2CPP・手動はタスク 6。

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

- （2026-11-07 に照合済み）owned・gacha の名前（`OwnedArgs`・`OwnedSelectionRequest`・`OwnedSelection`・`FormationValidator.IsPlanCompatible`・`DisplayText.RarityName`／`PlanName`／`ParamName`・`PortResult<T>`・`ServerReasons`・`FakeGameMaster`・`HomePresenter` の `IScreenNavigator`）。week・lesson はこの版の `ProduceFlow`（`ProduceEntry`・`ShowCurrentAsync`・`HandleStepAsync`・`GoToHomeAsync`）に合わせてある。スキルカード獲得のモーダルを背景の上に出すこと（week の決めたこと 15）は 2026-11-07 にユーザーが了承済み（[索引](2026-11-07-client-screens-index.md)）
