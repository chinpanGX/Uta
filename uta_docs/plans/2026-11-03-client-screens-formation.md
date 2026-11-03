---
title: 実装計画: client/ の画面 4/6 編成・プロデュースの開始と再開
status: draft
updated: 2026-11-03
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 4/6 編成・プロデュースの開始と再開

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 4 本目。次を作る。

1. サーバーの小さな直し: `LessonDto` に `UsableHand`（手札のカードごとの使用可否。[screen-design.md](../wiki/architecture/screen-design.md) 6章。ユーザーの判断 2026-11-03）を足す。6 本目（レッスン）が使う
2. `IProduceService` を呼ぶクライアントの層: ポート・ゲートウェイ・応答の写し（`ProduceStateView` ほか）・`ProduceSession`（最新の状態）・ユースケース・`ProduceFlow`（局面から画面への振り分け）・`ProduceMenu`（中断・放棄）
3. 編成の画面（育成アイドル・サポートカード 5 枠・シナリオの選択 → 開始）と、ホームの「プロデュース」「再開」「放棄」

仕様は [produce.md](../wiki/systems/produce.md)・[idol-support.md](../wiki/systems/idol-support.md) 2章（編成）、RPC・DTO は [produce-server.md](../wiki/architecture/server/produce-server.md) と `shared/Uta.Shared.Contracts/Produce/`、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 1・ホーム。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章。

- **前提**: 1〜3 本目が done（`ScreenNavigator`・`PortResult`・`OwnedSelectionRequest`・`FakeDialogService`・`FakeScreenNavigator`・`FakeGameMaster` を使う）。サーバーのプロデュース（順 6）は main にある
- **決めないこと / 作らないもの**: 週の行動選択・授業・支給・カードの選択肢（5 本目）、レッスン・試験・結果（6 本目）、レンタル枠（`RentalCandidateId` は常に 0）、編成のプリセット、難易度の選択（シナリオの選択だけ）、「開始時の補正」の表示（アビリティを表示しない: 3 本目の決定）
- **未確認**: `Uta.Shared.Contracts/Produce` の DTO（`ProduceStateResponse` の入れ子の配列を含む）の Unity 7 での MessagePack の動作と IL2CPP（[produce-server.md](../wiki/architecture/server/produce-server.md) 実装メモ）。この計画のタスク 6 で Editor と Windows の IL2CPP で確かめる

## 2. 触るファイル

パスは `client/uta/Assets/`（クライアント）、リポジトリのルート（`shared/`・`server/`）からの相対で書く。

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `shared/Uta.Shared.Contracts/Produce/LessonDto.cs` | `UsableHand`（`bool[]`、`Key(19)`）を足す |
| 変更 | `shared/Uta.Shared.Domain/Produce/ProduceEngine.cs` | `GetUsableHand(ProduceState)` を足す |
| 変更 | `server/Uta.Server/Application/Produce/ProduceView.cs`・`ProduceViewBuilder.cs`、`server/Uta.Server/Api/ProduceResponseMapper.cs` | `UsableHand` を通す |
| 変更 | `shared/Uta.Shared.Domain.Tests/Produce/ProduceEngineLessonTests.cs`、`server/Uta.Server.Tests/Integration/ProduceStartTests.cs` ほか | テスト（5章） |
| 変更 | `uta_docs/wiki/architecture/server/produce-server.md` | 6章の `LessonDto` の行に `UsableHand` を足す |
| 作成 | `Scripts/Application/Produce/*.cs`（3.2） | 応答の写し・ポート・`ProduceSession`・ユースケース・`FormationRules` |
| 作成 | `Scripts/Infrastructure/Api/ProduceApi.cs` | `ApiServiceBase<IProduceService>` |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs` | `Produce` を足し、`typeof(IProduceService)` を足す |
| 作成 | `Scripts/Infrastructure/Produce/ProduceGateway.cs` | `IProducePort` の実装と、応答の写し |
| 作成 | `Scripts/Presentation/Produce/ProduceFlow.cs`・`ProduceMenu.cs` | 局面から画面への振り分け、中断・放棄のメニュー |
| 作成 | `Scripts/Presentation/Formation/IFormationView.cs`・`FormationPage.cs`・`FormationPresenter.cs` | 編成 |
| 変更 | `Scripts/Presentation/Home/HomeDestinations.cs`・`HomePresenter.cs` | プロデュース・再開・放棄 |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | 登録 |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |

## 3. 型とシグネチャ

C# は C# 9 の範囲（`client/`・`shared/`）。`server/` は C# 14。

### 3.1 サーバー・shared の直し（`UsableHand`）

- `LessonDto`: コンストラクターの末尾に `bool[] usableHand` を足し、`[Key(19)] public bool[] UsableHand { get; }`（`[SerializationConstructor]`、既存のキーは変えない）
- `ProduceEngine`: `public IReadOnlyList<bool> GetUsableHand(ProduceState state)`。`state.Lesson` が `null` なら空の配列。そうでなければ `state.Lesson.Zones.Hand` の各 ID について `lessonEngine.CanUse(state.Lesson, id) == UseCheckResult.Ok`（`Hand` と同じ長さ・同じ順）
- `ProduceView`: 末尾に `IReadOnlyList<bool> UsableHand` を足す（`None` は `[]`）。`ProduceViewBuilder.Build` は `engine.GetUsableHand(state)` を渡す
- `ProduceResponseMapper.ToLessonDto(LessonState lesson, IReadOnlyList<int> npcScores, IReadOnlyList<bool> usableHand)`: `usableHand.ToArray()` を `LessonDto` の末尾に渡す。`ToStateResponse` は `view.UsableHand` を渡す
- `ProduceView` を作っているほかの箇所（`ProduceView.None`、テスト）もコンパイルが通るように直す

### 3.2 `Uta.Client.Application.Produce`

Domain の enum（`Uta.Shared.Domain.Produce`: `ProducePhase`・`WeekChoiceKind`・`LessonKind`・`ExamKind`・`ExamOutcome`・`ProduceRank`、`Uta.Shared.Domain.Lesson`: `ParamKind`・`PersistentTrigger`）をそのまま使う。

```csharp
public sealed class DeckCardView { public DeckCardView(int instanceId, int cardId, bool enhanced); public int InstanceId { get; } public int CardId { get; } public bool Enhanced { get; } }
public sealed class WeekActionView { public WeekActionView(int slotIndex, WeekChoiceKind kind, bool selectable); public int SlotIndex { get; } public WeekChoiceKind Kind { get; } public bool Selectable { get; } }
public sealed class ClassOptionView { public ClassOptionView(int index, int hpCost, ParamKind param, int paramGain, bool givesCard, bool selectable); public int Index { get; } public int HpCost { get; } public ParamKind Param { get; } public int ParamGain { get; } public bool GivesCard { get; } public bool Selectable { get; } }
public sealed class ExamTurnView { public ExamTurnView(ParamKind param, int scoreBonusPercent); public ParamKind Param { get; } public int ScoreBonusPercent { get; } }
public sealed class ParamBonusView { public ParamBonusView(int percent, int remainingTurns); public int Percent { get; } public int RemainingTurns { get; } }
public sealed class PersistentView { public PersistentView(PersistentTrigger trigger, int remainingTurns, int remainingCount); public PersistentTrigger Trigger { get; } public int RemainingTurns { get; } public int RemainingCount { get; } }

public sealed class LessonView
{
    public LessonView(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget,
        IReadOnlyList<ExamTurnView> examTurns, int turn, int shield, int usesLeft, int paramGained,
        IReadOnlyList<int> hand, IReadOnlyList<bool> usableHand, IReadOnlyList<int> drawPile, IReadOnlyList<int> discard, IReadOnlyList<int> exiled,
        IReadOnlyList<int> statusValues, IReadOnlyList<ParamBonusView> paramBonuses, IReadOnlyList<PersistentView> persistents,
        IReadOnlyList<int> lessonEnhanced, IReadOnlyList<int> npcScores);
    // 同じ名前のプロパティ（Kind, TargetParam, MaxTurns, ClearTarget, PerfectTarget, ExamTurns, Turn, Shield, UsesLeft, ParamGained, Hand, UsableHand, DrawPile, Discard, Exiled, StatusValues, ParamBonuses, Persistents, LessonEnhanced, NpcScores）
    public bool IsExam { get; }   // Kind が MidExam か FinalExam のとき true
}
public sealed class ExamResultView { public ExamResultView(ExamKind kind, int attempt, int score, int rank, IReadOnlyList<int> npcScores, bool passed); /* 同名のプロパティ */ }

public enum ProduceEndingKind { Nothing = 0, Completed = 1, Abandoned = 2, Discarded = 3 }   // Contracts.Produce.ProduceEnding と同じ数値
public sealed class ProduceResultView
{
    public ProduceResultView(ProduceEndingKind ending, ExamOutcome examOutcome, int evaluationValue, ProduceRank rank, int finalScore, int finalRank,
        int gachaCurrency, int gold, int supportPoints, int exp, int playerLevel, int playerExp);
    // 同名のプロパティ
}

public sealed class ProduceStateView
{
    public ProduceStateView(int operationNumber, ProducePhase phase, int scenarioId, int idolId, int week, int singing, int rhythm, int expression,
        int hp, int maxHp, int examRetryLeft, IReadOnlyList<DeckCardView> deck, IReadOnlyList<WeekActionView> weekActions,
        IReadOnlyList<ClassOptionView> classOptions, IReadOnlyList<int> cardOffers, LessonView lesson, ExamResultView examResult, ProduceResultView result);
    // 同名のプロパティ。lesson・examResult・result は無ければ null
    public int GetParam(ParamKind param);   // Singing / Rhythm / Expression。Nothing は ArgumentOutOfRangeException
}

public enum CardOfferAction { Nothing = 0, Pick = 1, Reroll = 2, Remove = 3, Decline = 4 }   // Contracts.Produce.CardOfferActionKind と同じ数値
public enum LessonActionType { Nothing = 0, UseCard = 1, UseDrink = 2, Skip = 3 }             // Contracts.Produce.LessonActionKind と同じ数値

public interface IProducePort
{
    UniTask<PortResult<ProduceStateView>> StartAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId);
    UniTask<PortResult<ProduceStateView>> GetStateAsync();
    UniTask<PortResult<ProduceResultView>> AbandonAsync();
    UniTask<PortResult<ProduceStateView>> ChooseWeekActionAsync(int operationNumber, int slotIndex, int optionIndex, ParamKind param);
    UniTask<PortResult<ProduceStateView>> ResolveCardOfferAsync(int operationNumber, CardOfferAction kind, int offerIndex);
    UniTask<PortResult<ProduceStateView>> DecideExamRetryAsync(int operationNumber, bool retry);
    UniTask<PortResult<ProduceStateView>> ExecuteLessonActionAsync(int operationNumber, LessonActionType kind, int cardInstanceId, int drinkSlotIndex);
}

public sealed class ProduceSession
{
    public ProduceStateView Current { get; }     // 無ければ null
    public ProduceStateView Previous { get; }    // 直前の Set の前の Current（授業・支給・休むの結果の差に使う）。無ければ null
    public ProduceResultView Result { get; }     // Result が入った応答を受けたときに保存する。Clear まで残る
    public bool ExamResultShown { get; set; }    // 試験結果を画面に出したか（6 本目が使う。Set で false に戻る）
    public void Set(ProduceStateView next);      // next が null なら ArgumentNullException。Previous = Current; Current = next; ExamResultShown = false; next.Result != null なら Result = next.Result
    public void Clear();                         // Current・Previous・Result を null、ExamResultShown を false
}

public enum ProduceStartStatus { Succeeded, InvalidFormation, AlreadyInProgress, InvalidArgument, Failed }
public sealed class StartProduceUseCase
{
    public StartProduceUseCase(IProducePort port, ProduceSession session);
    /// <summary>応答を session.Set して Succeeded。業務エラー: PRODUCE_INVALID_FORMATION → InvalidFormation、PRODUCE_ALREADY_IN_PROGRESS → AlreadyInProgress、PRODUCE_INVALID_ARGUMENT → InvalidArgument。それ以外の失敗は Failed。</summary>
    public async UniTask<ProduceStartStatus> ExecuteAsync(int idolId, IReadOnlyList<string> supportCardUids, int scenarioId);
}

public enum ProduceResumeStatus { Resumed, NotInProgress, Failed }
public sealed class ResumeProduceUseCase
{
    public ResumeProduceUseCase(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    /// <summary>GetStateAsync。Phase が Nothing 以外なら session.Set して Resumed。Phase が Nothing なら session.Clear・refresh.ExecuteAsync() して NotInProgress。失敗は Failed。</summary>
    public async UniTask<ProduceResumeStatus> ExecuteAsync();
}

public sealed class AbandonProduceUseCase
{
    public AbandonProduceUseCase(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    /// <summary>AbandonAsync。成功と、PRODUCE_NOT_IN_PROGRESS・PRODUCE_SESSION_DISCARDED（もう無い）は、session.Clear・refresh.ExecuteAsync() して true。それ以外の失敗は false。</summary>
    public async UniTask<bool> ExecuteAsync();
}

public enum ProduceStepStatus
{
    Succeeded,      // session.Current が次の局面になった
    Recovered,      // 食い違い（操作番号・局面）だったので GetState で取り直した。session.Current は最新。画面は Phase から選び直す
    SessionEnded,   // セッションが無い・破棄された。session は Clear 済みで、所持品は取り直した。ホームへ戻る
    Rejected,       // PRODUCE_INVALID_ARGUMENT
    Failed          // それ以外（共通のダイアログが出ている）
}
public sealed class ProduceActions
{
    public ProduceActions(IProducePort port, ProduceSession session, RefreshPlayerUseCase refresh);
    public async UniTask<ProduceStepStatus> ChooseWeekActionAsync(int slotIndex, int optionIndex, ParamKind param);
    public async UniTask<ProduceStepStatus> ResolveCardOfferAsync(CardOfferAction kind, int offerIndex);
    public async UniTask<ProduceStepStatus> DecideExamRetryAsync(bool retry);
    public async UniTask<ProduceStepStatus> ExecuteLessonActionAsync(LessonActionType kind, int cardInstanceId, int drinkSlotIndex = 0);
}

public static class FormationRules
{
    public const int SupportSlots = 5;
    /// <summary>サポートカードのプランが育成アイドルに合うか（Free か同じプラン）。</summary>
    public static bool IsCompatible(Plan idolPlan, Plan supportPlan);
}
```

`ProduceActions` の共通の動き（4 つの操作はどれも同じ。`port` の対応するメソッドに `session.Current.OperationNumber` を渡す）:
1. `session.Current` が `null` なら `InvalidOperationException("no active produce")`
2. 呼び出しの結果が成功: `session.Set(value)`。`value.Phase == ProducePhase.Finished` なら `await refresh.ExecuteAsync()`（報酬を所持品に反映する）。`Succeeded`
3. 失敗で業務エラー:
   - `PRODUCE_STALE_OPERATION`・`PRODUCE_UNEXPECTED_PHASE`: `port.GetStateAsync()`。成功で `Phase != Nothing` なら `session.Set(value)` して `Recovered`。`Phase == Nothing` なら `session.Clear()`・`refresh.ExecuteAsync()` して `SessionEnded`。取り直しの失敗は `Failed`
   - `PRODUCE_NOT_IN_PROGRESS`・`PRODUCE_SESSION_DISCARDED`: `session.Clear()`・`refresh.ExecuteAsync()` して `SessionEnded`
   - `PRODUCE_INVALID_ARGUMENT`: `Rejected`
   - それ以外: `Failed`
4. 業務エラーでない失敗: `Failed`

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
    public static ProduceStateView ToView(ProduceStateResponse response);        // テスト用に public static
    public static ProduceResultView ToResultView(ProduceResultResponse response);
}
```

- `handledCodes`: `StartAsync` は `InvalidFormation`・`AlreadyInProgress`・`InvalidArgument`。`GetStateAsync` はなし。`AbandonAsync` は `NotInProgress`・`SessionDiscarded`。4 つの操作は `StaleOperation`・`UnexpectedPhase`・`InvalidArgument`・`NotInProgress`・`SessionDiscarded`（すべて `ProduceErrors` の `ErrorCode`）
- 結果の直し方は `GachaGateway.DrawAsync` と同じ（成功 → `Success(ToView(...))`、`Domain == "uta"` で `handledCodes` の reason → `BusinessError`、それ以外 → `Failure`）
- `StartAsync` の `RentalCandidateId` は 0 を送る。`supportCardUids` は `string[]` にして渡す
- `ToView`: `response.Phase == ProducePhase.Nothing` のときも `ProduceStateView` を返す（配列は空、`Lesson`・`ExamResult`・`Result` は `null`）。`null` の配列は空として扱う。`Lesson` が `null` でなければ `LessonView`（`UsableHand` が `null` のときは `Hand` と同じ長さの `true` の配列にはせず、**空の配列**にする）。`ExamResult`・`Result` も同様に写す（`ProduceEnding` → `ProduceEndingKind` は `(ProduceEndingKind)(int)`）
- `UtaApiClient` に `public ProduceApi Produce { get; }`、`Create` で `ServiceClientFactory.Create<IProduceService>(callInvoker)`。`UtaMagicOnionClientInitializer` の属性に `typeof(IProduceService)`（`using Uta.Shared.Contracts.Produce;`）

### 3.4 `Uta.Client.Presentation.Produce`

```csharp
public class ProduceFlow    // テストで派生させるので sealed にせず、公開メソッドは virtual
{
    public ProduceFlow(ProduceSession session, ScreenNavigator navigator, DialogService dialogs, RefreshPlayerUseCase refresh);

    /// <summary>編成の開始・ホームの再開の直後に呼ぶ。session.Current の局面の画面を Push する（stack: true）。</summary>
    public virtual UniTask EnterAsync();

    /// <summary>操作のあと。session.Current の局面の画面へ置き換える（Replace）。</summary>
    public virtual UniTask AdvanceAsync();

    /// <summary>プロデュースの画面をすべて閉じてホームに戻る（refresh.ExecuteAsync() → navigator.PopToRootPageAsync()）。session は触らない。</summary>
    public virtual UniTask ExitToHomeAsync();

    /// <summary>ProduceStepStatus を見て次の画面へ。Succeeded・Recovered → AdvanceAsync()、SessionEnded → dialogs.ShowInfoAsync("プロデュースは終了しています。") のあと ExitToHomeAsync()、Rejected → dialogs.ShowInfoAsync("その操作はできません。")、Failed → 何もしない。</summary>
    public virtual UniTask HandleStepAsync(ProduceStepStatus status);
}
```

`ProduceFlow.EnterAsync`/`AdvanceAsync` は内部の `ShowAsync(bool replace)` を呼ぶ。`session.Current.Phase` ごとに画面を出す（`replace == true` なら `ReplacePageAsync`、false なら `PushPageAsync`）。**この計画の時点で対応する局面は無い**（5・6 本目が `case` を足す。足す場所と内容は各計画に書いてある）。対応する画面が無い局面のとき: `dialogs.ShowInfoAsync("この局面の画面は準備中です: " + phase)` を出して `ExitToHomeAsync()`（セッションはサーバーに残り、ホームの「再開」から続けられる）。`session.Current` が `null` なら `InvalidOperationException`。

```csharp
public class ProduceMenu     // [≡] のメニュー。5・6 本目の画面から使う
{
    public ProduceMenu(DialogService dialogs, AbandonProduceUseCase abandon, ProduceFlow flow);
    /// <summary>
    /// dialogs.ChooseAsync("メニュー", ["ホームへ戻る（あとで再開できます）", "プロデュースを放棄する", "閉じる"])。
    /// 0 → flow.ExitToHomeAsync()。1 → dialogs.ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？") が Positive なら abandon.ExecuteAsync() の結果が true のとき flow.ExitToHomeAsync()（false なら何もしない）。2 → 何もしない。
    /// </summary>
    public virtual UniTask ShowAsync();
}
```

### 3.5 `Uta.Client.Presentation.Formation`

```csharp
public sealed class FormationSlotViewData
{
    public FormationSlotViewData(string label, bool filled);
    public string Label { get; } public bool Filled { get; }   // "枠1: <名前> [SR] Lv 12" / "枠1: （空）"
}

public interface IFormationView
{
    void SetIdol(string label, bool selected);                 // "育成アイドル: <名前> [SSR] <プラン>" / "育成アイドル: 未選択"
    void SetSlots(IReadOnlyList<FormationSlotViewData> slots); // 5 件
    void SetScenario(string label, bool canChange);            // "シナリオ: <名前>"。2 つ以上あるとき canChange
    void SetStartEnabled(bool enabled);
    event Action IdolClicked;
    event Action<int> SlotClicked;        // 選ぶ（枠の添字 0〜4）
    event Action<int> SlotClearClicked;   // 外す
    event Action ScenarioClicked;
    event Action StartClicked;
    event Action BackClicked;
}
public sealed class FormationPage : PageBase, IFormationView { ... }

public sealed class FormationPresenter : Presenter<IFormationView>
{
    public FormationPresenter(PlayerCache cache, IGameMaster master, StartProduceUseCase start, RefreshPlayerUseCase refresh, ProduceFlow flow, DialogService dialogs, ScreenNavigator navigator);
}
```

**`FormationPresenter` の動き**
- 状態: `idolId`（0 = 未選択）、`slots`（長さ 5 の `string[]`。サポートカードの所持ID。空は `null`）、`scenarioIndex`
- `OnBind`: `idolId` = 所持の育成アイドル（`cache.Current.Idols`）のうち、レア度が最も高いもの（同じなら `IdolId` が小さいもの。所持が 0 なら 0）。`scenarioIndex = 0`（`master.Scenarios` が空なら `dialogs.ShowInfoAsync("シナリオがありません。")` して `PopPageAsync`）。`Refresh()`
- `Refresh()`: `SetIdol`、`SetSlots`（枠ごとに `cache.Current.SupportCards` から所持IDで探し、`master.GetSupportCard(card.SupportCardId)` の名前・レア度・`card.Level`）、`SetScenario`、`SetStartEnabled(idolId != 0)`
- `IdolClicked`: `var request = OwnedSelectionRequest.ForIdol();` → `await navigator.PushPageAsync<OwnedPage, OwnedPresenter>(p => p.Selection = request);` → `var selection = await request.Task;` キャンセルでなければ `idolId = selection.IdolId`。育成アイドルを変えたら、新しい育成アイドルのプランに合わない（`!FormationRules.IsCompatible`）サポートカードの枠を `null` にする。`Refresh()`
- `SlotClicked(i)`: `idolId == 0` なら `dialogs.ShowInfoAsync("先に育成アイドルを選んでください。")`。それ以外は、`excludedUids` = 枠 `i` 以外の埋まっている所持ID、`excludedSupportCardIds` = 枠 `i` 以外の埋まっているカードの `SupportCardId` で `OwnedSelectionRequest.ForSupportCard(master.GetIdol(idolId).Plan, excludedUids, excludedSupportCardIds)` を作り、育成アイドルと同じ手順で選ばせて `slots[i]` に入れる。`Refresh()`
- `SlotClearClicked(i)`: `slots[i] = null`、`Refresh()`
- `ScenarioClicked`: `scenarioIndex = (scenarioIndex + 1) % master.Scenarios.Count`、`Refresh()`
- `StartClicked`: `idolId == 0` なら何もしない。`uids` = `slots` の `null` でないものを枠の順に。`start.ExecuteAsync(idolId, uids, master.Scenarios[scenarioIndex].ScenarioId)`:
  - `Succeeded`: `await flow.EnterAsync();`
  - `InvalidFormation`: `dialogs.ShowInfoAsync("編成が正しくありません。")`
  - `AlreadyInProgress`: `dialogs.ShowInfoAsync("進行中のプロデュースがあります。ホームから再開してください。")` のあと `await refresh.ExecuteAsync();`（`PlayerCache` の `HasActiveProduce` を最新にする）→ `await navigator.PopPageAsync();`
  - `InvalidArgument`: `dialogs.ShowInfoAsync("開始できませんでした。")`。`Failed`: 何も出さない（共通のダイアログが出ている）
  - 例外: `dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`
  - 連打を避けるため、実行中は次の要求を無視する（フラグ）
- `BackClicked` → `navigator.PopPageAsync()`


**画面のレイアウト**（`BuildUi`。見た目は仮）: ヘッダー（「＜」ボタン、タイトル「編成」）、育成アイドルの行（`Text` と「選ぶ」ボタン）、サポート 5 枠（枠ごとに `CreateHorizontal`: `Text` と「選ぶ」「外す」ボタン）、シナリオの行（`Text` と「変更」ボタン。`canChange` が false なら無効）、下に「プロデュース開始」ボタン（`SetStartEnabled` で有効/無効）。

### 3.6 ホーム

`HomeDestinations` のコンストラクターを `(DialogService dialogs, ScreenNavigator navigator, PlayerCache cache, ResumeProduceUseCase resume, AbandonProduceUseCase abandon, ProduceFlow flow)` にし、2 つを実装する（`OpenGachaAsync`・`OpenOwnedAsync` は変えない）:

- `OpenProduceAsync`: `cache.Current.HasActiveProduce` が false なら `navigator.PushPageAsync<FormationPage, FormationPresenter>()`。true なら `dialogs.ChooseAsync("進行中のプロデュースがあります。", ["再開", "放棄", "やめる"])`: 0 → `ResumeProduceAsync()`、1 → `ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？")` が Positive なら `abandon.ExecuteAsync()`（失敗は何も出さない）、2 → 何もしない
- `ResumeProduceAsync`: `resume.ExecuteAsync()`: `Resumed` → `await flow.EnterAsync();`、`NotInProgress` → `dialogs.ShowInfoAsync("進行中のプロデュースはありません。")`、`Failed` → 何も出さない。例外は `"通信に失敗しました: " + e.Message`

`HomePresenter`: `ProduceClicked`・`ResumeClicked` の処理を `await destinations.XxxAsync();` のあと `SetProfile(...)`/`SetResumeVisible(...)`（`cache.Current` から）をやり直すものにする（放棄・終了のあとホームの表示を更新するため）。

## 4. タスク

各タスクの終わりに、`dotnet build Uta.slnx`（警告 0・エラー 0。server・shared を触ったタスク）、`unity recompile --format json`（client を触ったタスク）、そのタスク分のテストが通る状態にする。.NET SDK・Docker・Unity が無い環境では、確かめていないと報告する（推測で「通った」と書かない）。

### タスク 1: サーバー・shared の `UsableHand`

触る: 2 章の `shared/`・`server/`・`produce-server.md` の行。
手順: 3.1 のとおり。`produce-server.md` の 6章の `LessonDto` の行の `Hand` の次に `UsableHand`（`bool[]`。`Hand` と同じ長さ・同じ順。`LessonEngine.CanUse` が Ok のとき true。レッスン・試験のときだけ）を足し、実装メモに 1 行足す。
確かめ方: `dotnet test --project shared/Uta.Shared.Domain.Tests`、`dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）。

### タスク 2: Application の型・ポート・ユースケース

触る: `Application/Produce/*`。
手順: 3.2 のとおり。
境界: `ProduceSession.Set(null)` は `ArgumentNullException`。`ProduceStateView.GetParam` は `Singing`・`Rhythm`・`Expression` 以外で `ArgumentOutOfRangeException`。
確かめ方: `ProduceSessionTests`・`StartProduceUseCaseTests`・`ResumeProduceUseCaseTests`・`AbandonProduceUseCaseTests`・`ProduceActionsTests`・`FormationRulesTests`。

### タスク 3: API・ゲートウェイ

触る: `Infrastructure/Api/ProduceApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Produce/ProduceGateway.cs`。
手順: 3.3 のとおり。
確かめ方: `ProduceGatewayTests`（`ToView`・`ToResultView`）。`unity recompile` のあとコンソールのエラーが 0。

### タスク 4: ProduceFlow・ProduceMenu・編成

触る: `Presentation/Produce/*`、`Presentation/Formation/*`。
手順: 3.4・3.5 のとおり。
確かめ方: `ProduceFlowTests`・`ProduceMenuTests`・`FormationPresenterTests`。

### タスク 5: ホームの接続と登録

触る: `Presentation/Home/HomeDestinations.cs`・`HomePresenter.cs`、`Bootstrap/UtaBootstrap.cs`、`Tests/EditMode/HomePresenterTests.cs`。
手順: 3.6 のとおり。`UtaBootstrap` に `Register<ProduceGateway>(Singleton).As<IProducePort>()`、`Register<ProduceSession>(Singleton)`、`Register<StartProduceUseCase>(Singleton)`、`Register<ResumeProduceUseCase>(Singleton)`、`Register<AbandonProduceUseCase>(Singleton)`、`Register<ProduceActions>(Singleton)`、`Register<ProduceFlow>(Singleton)`、`Register<ProduceMenu>(Singleton)`、`Register<FormationPresenter>(Transient)` を足す。`HomePresenterTests` の派生のコンストラクター呼び出しを新しい引数（すべて `null`）に直す。
確かめ方: `HomePresenterTests`（直したもの）、`HomeDestinationsTests`。

### タスク 6: 通信の確認と記録

1. Editor の Play モードで、ローカルのサーバーに対して、編成 → 「プロデュース開始」→ 局面が `WeekSelect` の応答を受け取れること（「この局面の画面は準備中です: WeekSelect」のダイアログが出て、ホームに戻る）、ホームの「再開」が同じ応答を取れること（同じダイアログ）、「放棄」でセッションが消えること（ホームの「再開」が消える）を確かめる
2. Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」。Editor を閉じてから）で、上の 1 を手で確かめる（ビルドしたプレイヤーを `-batchmode` なしで起動する）。`ProduceStateResponse`（入れ子の DTO・enum・`bool[]`）の送受信が IL2CPP で通るかの結果を `unity7-evaluation.md` の 2 章に足す。通らなければ止まって報告する
3. `screen-design.md` と `produce-server.md` の「実装メモ」にズレを書く（なければ「なし」）。この計画を `done` にする

## 5. テスト

| ファイル | テスト | 内容 |
|---|---|---|
| `ProduceEngineLessonTests.cs`（shared、xUnit） | `GetUsableHand_InLesson_MatchesCanUsePerHandCard` | `Start` → `ChooseLesson(0, Singing)` のあと、`GetUsableHand(state)` の長さが `state.Lesson.Zones.Hand.Count` で、各要素が `new LessonEngine(Catalog.Cards).CanUse(state.Lesson, id) == UseCheckResult.Ok` と等しい |
| | `GetUsableHand_NoLesson_ReturnsEmpty` | `Start` 直後（`WeekSelect`）は空 |
| `ProduceStartTests.cs` ほか（server、結合） | `Lesson_Response_HasUsableHandSameLengthAsHand` | 最短シナリオでレッスンに入った応答の `Lesson.UsableHand.Length == Lesson.Hand.Length`（既存の `ProduceDriver` で週の行動を選んで確かめる。既存の結合テストの書き方に合わせる） |
| `ProduceSessionTests.cs` | `Set_UpdatesCurrentAndPrevious` / `Set_ResultKept_UntilClear` / `Set_ResetsExamResultShown` / `Set_Null_Throws` / `Clear_ResetsAll` | |
| `ProduceStateViewTests.cs` | `GetParam_ReturnsValues` / `GetParam_Nothing_Throws` | |
| `StartProduceUseCaseTests.cs` | `Execute_Success_SetsSession` / `Execute_InvalidFormation_ReturnsStatus` / `Execute_AlreadyInProgress_ReturnsStatus` / `Execute_OtherFailure_ReturnsFailed` | 偽の `IProducePort`（`Tests/EditMode/Support/FakeProducePort.cs`: 各メソッドの戻り値を設定でき、呼ばれた引数を記録する） |
| `ResumeProduceUseCaseTests.cs` | `Execute_Active_SetsSessionAndReturnsResumed` / `Execute_PhaseNothing_ClearsAndRefreshes` / `Execute_Failure_ReturnsFailed` | |
| `AbandonProduceUseCaseTests.cs` | `Execute_Success_ClearsAndRefreshes` / `Execute_NotInProgress_ClearsAndReturnsTrue` / `Execute_OtherFailure_ReturnsFalse` | |
| `ProduceActionsTests.cs` | `ChooseWeekAction_Success_SetsSessionAndPassesOperationNumber` | 要求の `operationNumber` が `session.Current.OperationNumber` |
| | `Operation_Finished_RefreshesPlayer` | `Phase == Finished` の応答のあと `PlayerCache` が差し替わる |
| | `Operation_Stale_RecoversWithGetState` | `Recovered`、`session.Current` が `GetState` の値 |
| | `Operation_StaleThenGetStateNothing_SessionEnded` | `session` が `Clear` される |
| | `Operation_SessionDiscarded_ClearsAndSessionEnded` | |
| | `Operation_InvalidArgument_Rejected` | |
| | `Operation_NoSession_Throws` | `InvalidOperationException` |
| | `AllOperations_MapToPort` | 4 つの操作が `IProducePort` の対応するメソッドを引数どおりに呼ぶ |
| `FormationRulesTests.cs` | `IsCompatible_FreeAnyPlan` / `IsCompatible_SamePlan` / `IsCompatible_DifferentPlan_False` | |
| `ProduceGatewayTests.cs` | `ToView_MapsAllFields` | 入れ子の `LessonDto`（`UsableHand` 含む）・`ExamResultDto`・`ProduceResultResponse` を写す |
| | `ToView_PhaseNothing_ReturnsEmptyView` | |
| | `ToView_NullArrays_BecomeEmpty` | `UsableHand == null` は空 |
| | `ToResultView_MapsEnding` | `ProduceEnding.Abandoned` → `ProduceEndingKind.Abandoned` |
| `ProduceFlowTests.cs` | `ExitToHome_RefreshesAndPopsToRoot` | `FakeScreenNavigator.Calls` に `PopToRoot` |
| | `Enter_UnsupportedPhase_ShowsInfoAndExits` | この計画の時点の全局面で、`dialogs` に "準備中" が記録され `PopToRoot` |
| | `HandleStep_*` | `SessionEnded` は info → `PopToRoot`、`Rejected` は info、`Failed` は何も呼ばない、`Succeeded`・`Recovered` は `AdvanceAsync` を 1 回 |
| `ProduceMenuTests.cs` | `Show_GoHome_CallsExitToHome` / `Show_AbandonConfirmed_AbandonsThenExits` / `Show_AbandonDeclined_DoesNothing` / `Show_AbandonFailed_DoesNotExit` / `Show_Close_DoesNothing` | |
| `FormationPresenterTests.cs` | `Bind_DefaultsToHighestRarityIdol` | レア度が最も高い、同率なら ID が小さい |
| | `Bind_NoIdols_StartDisabled` | |
| | `IdolClicked_Selected_ChangesIdolAndClearsIncompatibleSupport` | 偽の `FakeScreenNavigator` で `Selection` を取り出して `Complete` する |
| | `SlotClicked_NoIdol_ShowsInfo` | |
| | `SlotClicked_BuildsRequestWithExclusions` | 他の枠の所持ID・カード ID が除外される |
| | `SlotClearClicked_ClearsSlot` | |
| | `ScenarioClicked_Cycles` | |
| | `StartClicked_Success_CallsFlowEnter` | `StartProduceUseCase` への引数（`uids` が枠の順で `null` を除く、`ScenarioId`） |
| | `StartClicked_InvalidFormation_ShowsInfo` / `StartClicked_AlreadyInProgress_RefreshesAndPops` | |
| `HomeDestinationsTests.cs` | `OpenProduce_NoActive_PushesFormation` / `OpenProduce_Active_Resume` / `OpenProduce_Active_AbandonConfirmed` / `ResumeProduce_NotInProgress_ShowsInfo` | |
| `HomePresenterTests.cs`（追記） | `ProduceClicked_AfterDestination_RefreshesProfile` | `destinations` の処理のあとに `SetResumeVisible` が再び呼ばれる |

Play モード・手動はタスク 6。

## 6. 未決事項

なし
