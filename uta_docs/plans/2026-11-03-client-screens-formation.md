---
title: 実装計画: client/ の画面 4/6 編成・プロデュースの開始と再開
status: draft
updated: 2026-11-03
spec: ../wiki/systems/produce.md, ../wiki/systems/idol-support.md
---

# 実装計画: client/ の画面 4/6 編成・プロデュースの開始と再開

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 4 本目。次を作る。

1. サーバーの小さな直し: `LessonDto` に `UsableHand`（手札のカードごとの使用可否。[screen-design.md](../wiki/architecture/screen-design.md) 6章。ユーザーの判断 2026-11-03）を足す。6 本目（レッスン）が使う
2. `IProduceService` を呼ぶクライアントの層: ポート・ゲートウェイ・応答の写し（`ProduceProgress` ほか）・`ProduceSession`（最新の状態）・ユースケース・`ProduceFlow`（局面から画面への振り分け）・`ProduceMenu`（ホームへ戻る・放棄）・`ProduceBackdropPage`（カードの選択肢のモーダルの下に敷くページ）
3. 編成の画面（育成アイドル・サポートカード 5 枠・シナリオの選択 → 開始）と、ホームの「プロデュース」「再開」（と、その中の「放棄」）

仕様は [produce.md](../wiki/systems/produce.md)・[idol-support.md](../wiki/systems/idol-support.md) 2章（編成）、RPC・DTO は [produce-server.md](../wiki/architecture/server/produce-server.md) 5・6章と `shared/Uta.Shared.Contracts/Produce/`、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 1・ホーム。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章。

- **画面を作る共通ルールは [1 本目（foundation）](2026-11-03-client-screens-foundation.md) 3.3 が正**（Presenter の形・コンストラクター 2 本・`[AssetAddress]`・`EnsureBuilt`・実行中フラグ・購読と `Dispose`・テストの書き方・偽の作り）。この計画はそれに従い、同じことを繰り返して書かない
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層。DTO の写しは Infrastructure に閉じる）、[0009](../wiki/decisions/0009-client-stack.md)
- **前提**（実装に入る前に、次がブランチに入っていることを確かめる。無ければ止まって報告する）:
  - 1 本目（foundation）: `IScreenNavigator`（基盤）の使い方、`PageStack`、`PageBase`、`DialogService`、`UiFactory`・`UiTheme`、`IGameMaster`・`ScenarioInfo`、`RefreshPlayerUseCase`、`HomePresenter`、`UtaErrors`（`ProduceErrors` の 7 つを `ErrorAction.Stay` で登録済み）、`Tests/EditMode/Fakes/`（`CallLog`・`FakeScreenNavigator`・`FakeDialogService`）
  - 2 本目（gacha）: `PortResult<T>`・`ServerReasons`（`ProduceStaleOperation` ほか 7 つ）、`FakeGameMaster`、`HomePresenter` への `IScreenNavigator` の追加
  - 3 本目（owned）: `OwnedSelectionRequest`（`ForIdol`・`ForSupportCard`・`Task`・`Complete`）・`OwnedSelection`、選択モードの `OwnedPresenter`（Args は `OwnedArgs`。3.5 で使う形は下に書く）、`DisplayText`（`Rarity`・`Plan`・`Param`）、`FormationValidator.IsPlanCompatible(Plan idolPlan, Plan supportPlan)`（Shared.Domain）
  - サーバーのプロデュース（順 6）は main にある
- **決めないこと / 作らないもの**: 週の行動選択・授業・支給・カードの選択肢の画面（5 本目）、レッスン・試験・結果の画面（6 本目）、レンタル枠（`RentalCandidateId` は常に 0）、編成のプリセット、難易度の選択（シナリオの選択だけ）、「開始時の補正」の表示（アビリティは所持一覧の詳細で出す: 3 本目）、取り直しで返った `DiscardedProduce` の表示（起動のときだけ出す: 1 本目。プロデュースの画面からの取り直しでは出さない）
- **未確認**: `Uta.Shared.Contracts/Produce` の DTO の、Unity 7 の IL2CPP での MessagePack の送受信（[produce-server.md](../wiki/architecture/server/produce-server.md) 実装メモ、[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 3.2）。この計画のタスク 6 で確かめるのは、局面 `WeekSelect` の応答（最上位の値・enum と `Deck`・`WeekActions`・`ClassOptions`・`CardOffers` の配列、`Lesson`・`ExamResult`・`Result` は `null` の経路）まで。`LessonDto`（`bool[]` を含む入れ子）・`ExamResultDto`・`ProduceResultResponse` は 6 本目で確かめる

### 1.1 この計画で決めたこと

- **`UsableHand` のキーは末尾の `Key(19)`**（既存のキーを動かさないため。古いクライアント・保存済みの応答と互換）。[produce-server.md](../wiki/architecture/server/produce-server.md) 6章の表は意味の近さで `Hand` の次に並べたまま、「Key は末尾の 19」と注記する（表の `UsableHand` は進行役が足し済み。二重に足さない）
- **`UsableHand` はサーバーの `ProduceViewBuilder` が `ProduceEngine.CanUse(state, id)`（既存）で作る**。`Uta.Shared.Domain` に公開メソッドを足さない（クライアントは使用可否を計算しないため。critique M3）
- **型の名前**: クライアントの応答の写しは `Produce` で始め、`View` で終えない（`Uta.Shared.Domain.Produce.ClassOptionView`・`ProduceResult` と衝突させない。Presentation の `IXxxView`（画面の IF）と紛らわしくしない）。最上位は `ProduceProgress`（旧案 `ProduceStateView`）、結果は `ProduceOutcome`（`ProduceResult` は Domain にある）
- **編成のプランの規則は Shared.Domain の `FormationValidator.IsPlanCompatible` を使う**（idol-support.md 2.1「同じ関数」）。クライアントに `FormationRules` は作らない。枠の数 5 は `FormationPresenter.SupportSlots`
- **プロデュースの局面の切り替えは「ホームまで Pop してから、次の局面の画面を Push」**（`ProduceFlow.ShowCurrentAsync`。`Replace` は作らない: foundation 3.3.1）。プロデュースの画面は必ずホームの直上に積まれる（編成から開始したときは編成のページも Pop される）。間に一瞬ホームが見えるのは受け入れる
- **局面 `CardOffer` は、背景のページ `ProduceBackdropPage`（週・パラメータ・体力だけを出す）を Push してから、その上にカードの選択肢のモーダルを出す**（入口が再開でも操作の後でも同じ。5 本目が `case` を足す）
- **取り直し（`RefreshPlayerUseCase`）は状態を変えたユースケースが行い、`ProduceFlow` は行わない**（サインインの重複を避ける。consistency M6・critique M1）。RPC が成功した後の取り直しの失敗は成功として扱う（foundation 3.3.5）
- **業務エラー**: `handledCodes` に入れたコードはユースケースの結果の型で返し、画面が文言を選ぶ。入れないコードは `UtaErrors` の `Stay`（`ApiCore` の共通ダイアログが出て画面に残る。ユースケースは `Failed`／`false` を返し、画面は追加で何も出さない）。RPC ごとの割り振りは 3.3
- **例外**: ユースケースは通信の失敗を例外にしない（`PortResult` で返す）。例外は想定外のもの（不具合）だけで、**ボタンの処理（Presenter の公開 `UniTask` メソッド）が `catch (Exception e)` で受け、`Debug.LogException(e)` のあと `dialogs.ShowInfoAsync("エラーが発生しました: " + e.Message)` を出す**。`ProduceFlow`・`ProduceMenu`・ユースケースは例外を受けない（投げたまま返す）。5・6 本目の Presenter も同じ書き方にする
- **ホームの「再開」の表示**: `session.Current` があれば `session.HasActive`（`ProduceSession` が新しい）、無ければ `cache.Current.HasActiveProduce`（起動時のサインインの値）

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対（クライアント）、それ以外はリポジトリのルートからの相対。`.meta` は Unity が作ったものをコミットする。

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
| 作成 | `Scripts/Infrastructure/Api/ProduceApi.cs` | `ApiServiceBase<IProduceService>`（3.3） |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs` | `Produce` を足し、`typeof(IProduceService)` を足す（3.3） |
| 作成 | `Scripts/Infrastructure/Produce/ProduceGateway.cs` | `IProducePort` の実装と、応答の写し（3.3） |
| 作成 | `Scripts/Presentation/Produce/ProduceFlow.cs`・`ProduceMenu.cs` | 局面から画面への振り分け、メニュー（3.4） |
| 作成 | `Scripts/Presentation/Produce/IProduceBackdropView.cs`・`ProduceBackdropPage.cs`・`ProduceBackdropPresenter.cs` | カードの選択肢の下に敷くページ（3.4） |
| 作成 | `Scripts/Presentation/Formation/IFormationView.cs`・`FormationPage.cs`・`FormationPresenter.cs`・`FormationSlotViewData.cs` | 編成（3.5） |
| 変更 | `Scripts/Presentation/Home/HomePresenter.cs` | `OpenProduceAsync`・`ResumeProduceAsync` を置き換える、再開の表示（3.6） |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | DI の登録と `RegisterScreens`（タスク 5） |
| 作成 | `Tests/EditMode/Fakes/FakeProducePort.cs`・`FakePlayerPort.cs`・`TestProduce.cs` | テスト用の偽と組み立て（3.7。5・6 本目も使う） |
| 作成 | `Tests/EditMode/ProduceSessionTests.cs`・`ProduceProgressTests.cs`・`StartProduceUseCaseTests.cs`・`ResumeProduceUseCaseTests.cs`・`AbandonProduceUseCaseTests.cs`・`ProduceActionsTests.cs`・`ProduceGatewayTests.cs`・`ProduceFlowTests.cs`・`ProduceMenuTests.cs`・`ProduceBackdropPresenterTests.cs`・`FormationPresenterTests.cs` | EditMode のテスト（5章） |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | 新しいコンストラクターに合わせ、テストを足す（5章） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`unity7-evaluation.md` | 結果の記録（タスク 6） |

## 3. 型とシグネチャ

C# は C# 9 の範囲（`client/`・`shared/`。コレクション式 `[...]`・record・ファイルスコープ namespace は使わない。配列は `new[] { ... }`、空は `new bool[0]` か `Array.Empty<T>()`）。`server/` は C# 14。名前空間はフォルダに合わせる: `Scripts/Application/Produce` → `Uta.Client.Application.Produce`、`Scripts/Infrastructure/Api` → `Uta.Client.Infrastructure.Api`、`Scripts/Infrastructure/Produce` → `Uta.Client.Infrastructure.Produce`、`Scripts/Presentation/Produce` → `Uta.Client.Presentation.Produce`、`Scripts/Presentation/Formation` → `Uta.Client.Presentation.Formation`、テストは `Uta.Client.Infrastructure.Tests`。

### 3.1 サーバー・shared の直し（`UsableHand`）

- `LessonDto`: コンストラクターの末尾（`int[] npcScores` の後ろ）に `bool[] usableHand` を足し、`[Key(19)] public bool[] UsableHand { get; }`。既存のキー 0〜18 は変えない
- `ProduceView`（record）: 末尾に `IReadOnlyList<bool> UsableHand` を足す。`None` は `new(null, [], [], [], null, null, [])`。`new ProduceView(` は `ProduceViewBuilder.Build` と `None` の 2 か所だけ（テストには無い）
- `ProduceViewBuilder.Build`: `bool[] usableHand = state.Lesson is null ? [] : state.Lesson.Zones.Hand.Select(id => engine.CanUse(state, id) == UseCheckResult.Ok).ToArray();` を作り、`new ProduceView(...)` の末尾に渡す（`using Uta.Shared.Domain.Lesson;` を足す）
- `ProduceResponseMapper`: `ToLessonDto(LessonState lesson, IReadOnlyList<int> npcScores, IReadOnlyList<bool> usableHand)` にし、`new LessonDto(...)` の末尾に `usableHand.ToArray()` を渡す。`ToStateResponse` は `ToLessonDto(state.Lesson, view.NpcScores, view.UsableHand)`

### 3.2 `Uta.Client.Application.Produce`

使う Domain の enum: `Uta.Shared.Domain.Produce` の `ProducePhase`・`WeekChoiceKind`・`ExamKind`・`ExamOutcome`・`ProduceRank`、`Uta.Shared.Domain.Lesson` の `LessonKind`・`ParamKind`・`PersistentTrigger`・`Plan`。

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
- `UtaApiClient`: private コンストラクターの**末尾**（1〜3 本目が足した引数の後ろ）に `ProduceApi produce` を足して `public ProduceApi Produce { get; }` に入れる。`Create` で `var produceClient = ServiceClientFactory.Create<IProduceService>(callInvoker);` を作り、`new ProduceApi(core, produceClient)` を末尾に渡す
- `UtaMagicOnionClientInitializer`: `[MagicOnionClientGeneration(...)]` の型の並びの最後（前の計画が足した型の後ろ）に `typeof(IProduceService)` を足す（`using Uta.Shared.Contracts.Produce;`）

### 3.4 `Uta.Client.Presentation.Produce`

```csharp
public class ProduceFlow    // テストで派生させるので sealed にせず、公開メソッドは virtual
{
    public ProduceFlow(ProduceSession session, IScreenNavigator navigator, PageStack pageStack, DialogService dialogs);

    /// <summary>
    /// session.Current の局面の画面を出す。編成の開始・ホームの再開・操作の後（HandleStepAsync）から呼ぶ。
    /// 1. session.Current が null なら InvalidOperationException("no active produce")
    /// 2. await pageStack.PopToRootAsync(navigator);（ホームの上のページ・モーダルを全部閉じる。呼んだ Presenter もここで閉じられる）
    /// 3. await PushForPhaseAsync(session.Current.Phase);
    /// </summary>
    public virtual UniTask ShowCurrentAsync();

    /// <summary>プロデュースの画面をすべて閉じてホームに戻る（await pageStack.PopToRootAsync(navigator)）。session も所持品の取り直しも触らない。</summary>
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
    /// 局面ごとの画面を積む。この計画の時点では case が無い（5・6 本目が足す）。
    /// default: await dialogs.ShowInfoAsync("この局面の画面は準備中です: " + phase);（ホームに残る。セッションはサーバーに残り、「再開」から続けられる）
    /// </summary>
    protected virtual UniTask PushForPhaseAsync(ProducePhase phase);
}
```

- `PushForPhaseAsync` は `switch (phase)` で書き、各 `case` は `await navigator.Push…Async<…>(playAnimation: false); return;` の形にする。5・6 本目が足す `case` の形（この計画では書かない）:
  - ページの局面: `case ProducePhase.WeekSelect: await navigator.PushPageAsync<WeekPresenter>(playAnimation: false); return;`
  - **カードの選択肢**: `case ProducePhase.CardOffer: await navigator.PushPageAsync<ProduceBackdropPresenter>(playAnimation: false); await navigator.PushModalAsync<CardOfferPresenter>(playAnimation: false); return;`（入口が再開でも操作の後でも、背景のページを積んでからモーダルを出す。モーダルの Presenter は操作のあと `flow.HandleStepAsync` を呼ぶだけでよい: `ShowCurrentAsync` がモーダルと背景のページをまとめて閉じる）
- 局面の画面の Presenter が `ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync` を呼ぶと、その Presenter 自身が閉じられる（`Dispose` が呼ばれる）。**呼んだ後は View に触らない**（`finally` の実行中フラグを戻すことだけはしてよい）
- `PageStack` の枚数は呼んだ時点で数える（foundation 3.2）。呼ぶ前の遷移は全部 `await` 済みであること（foundation 3.3.5）

```csharp
public class ProduceMenu     // [≡] のメニュー。5・6 本目の画面から使う。テストで派生させるので sealed にせず、ShowAsync は virtual
{
    public ProduceMenu(DialogService dialogs, AbandonProduceUseCase abandon, ProduceFlow flow);
    /// <summary>
    /// var index = await dialogs.ChooseAsync("メニュー", new[] { "ホームへ戻る（あとで再開できます）", "プロデュースを放棄する", "閉じる" });
    /// 0 → await flow.ExitToHomeAsync()。
    /// 1 → await dialogs.ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？") が Positive で、await abandon.ExecuteAsync() が true のとき await flow.ExitToHomeAsync()（false なら何もしない。共通ダイアログが出ている）。Negative なら何もしない。
    /// 2 → 何もしない。
    /// 例外は受けない（呼んだ Presenter のボタンの処理が受ける: 1.1）。
    /// </summary>
    public virtual UniTask ShowAsync();
}

public interface IProduceBackdropView
{
    void SetHeader(string text);
}

public sealed class ProduceBackdropPage : PageBase, IProduceBackdropView
{
    // BuildUi(body): var root = UiFactory.CreateVertical(body, "Root"); の子に
    //   "Header"（UiFactory.CreateText(root, "Header", "", UiTheme.FontBody, TextAnchor.MiddleLeft)、UiFactory.SetHeight(header, 160)）、
    //   "Spacer"（UiFactory.CreateRect(root, "Spacer")、UiFactory.SetFlexibleHeight(spacer, 1)）。
    // SetHeader: EnsureBuilt() してから Header の text を変える。
}

[AssetAddress(nameof(ProduceBackdropPage))]
public sealed class ProduceBackdropPresenter : IPresenter
{
    [Inject] public ProduceBackdropPresenter(ProduceBackdropPage view, ProduceSession session);   // : this((IProduceBackdropView)view, session)
    public ProduceBackdropPresenter(IProduceBackdropView view, ProduceSession session);
    /// <summary>view.SetHeader(ProduceBackdropPresenter.BuildHeader(session.Current)) して UniTask.CompletedTask。</summary>
    public UniTask InitializeAsync();
    public void Dispose();   // 何もしない（購読が無い）
    /// <summary>
    /// "週 " + p.Week + "\n" + DisplayText.Param(ParamKind.Singing) + " " + p.Singing + "　" + DisplayText.Param(ParamKind.Rhythm) + " " + p.Rhythm + "　" + DisplayText.Param(ParamKind.Expression) + " " + p.Expression + "　体力 " + p.Hp + " / " + p.MaxHp
    /// （"　" は全角の空白 1 つ）。p が null なら ArgumentNullException。
    /// </summary>
    public static string BuildHeader(ProduceProgress p);
}
```

### 3.5 `Uta.Client.Presentation.Formation`

```csharp
public sealed class FormationSlotViewData
{
    public FormationSlotViewData(string label, bool filled);
    public string Label { get; } public bool Filled { get; }
}

public interface IFormationView
{
    void SetIdol(string label);                                // 3.5 の文言
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
public sealed class FormationPage : PageBase, IFormationView { ... }   // レイアウトは下

[AssetAddress(nameof(FormationPage))]
public sealed class FormationPresenter : IPresenter
{
    public const int SupportSlots = 5;

    [Inject]
    public FormationPresenter(FormationPage view, PlayerCache cache, IGameMaster master, StartProduceUseCase start,
        ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);   // : this((IFormationView)view, ...)
    public FormationPresenter(IFormationView view, PlayerCache cache, IGameMaster master, StartProduceUseCase start,
        ProduceFlow flow, DialogService dialogs, IScreenNavigator navigator);

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
  - `SetIdol`: `idolId == 0` なら `"育成アイドル: 未選択"`。そうでなければ `var idol = master.GetIdol(idolId);` → `"育成アイドル: " + idol.Name + " [" + DisplayText.Rarity(idol.Rarity) + "] " + DisplayText.Plan(idol.Plan)`
  - `SetSlots`: 枠 `i`（0〜4）ごとに、`slots[i]` が `null` でなければ `cache.Current.SupportCards` から `SupportCardUid == slots[i]` のものを探す。見つからなければ `slots[i] = null` にする。見つかれば `var info = master.GetSupportCard(owned.CardId);` → `new FormationSlotViewData("枠" + (i + 1) + ": " + info.Name + " [" + DisplayText.Rarity(info.Rarity) + "] Lv " + owned.Level, true)`。空は `new FormationSlotViewData("枠" + (i + 1) + ": （空）", false)`
  - `SetScenario`: `master.Scenarios.Count == 0` なら `("シナリオ: なし", false)`。そうでなければ `("シナリオ: " + master.Scenarios[scenarioIndex].Name, master.Scenarios.Count >= 2)`
  - `SetStartEnabled(idolId != 0 && master.Scenarios.Count > 0)`
- `SelectIdolAsync`（実行中フラグ・`try`/`catch`/`finally`。以下の `UniTask` のメソッドも同じ。catch は 1.1 の例外の書き方）:
  1. `var request = OwnedSelectionRequest.ForIdol();`
  2. `await navigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(request), playAnimation: false);`
  3. `var selection = await request.Task;` `disposed` なら終わる（編成のページが別の理由で閉じられた）。`selection.IsCancelled` か `selection.IdolId == idolId` なら終わる
  4. `idolId = selection.IdolId`。`var plan = master.GetIdol(idolId).Plan;` 枠ごとに、埋まっていて `!FormationValidator.IsPlanCompatible(plan, master.GetSupportCard(owned.CardId).Plan)` のものを `null` にする（`owned` は所持の中のそのカード。見つからない枠も `null`）
  5. `Refresh()`。4 で 1 枠でも外したら、そのあと `await dialogs.ShowInfoAsync("プランが合わないサポートカードを外しました。");`
- `SelectSlotAsync(index)`:
  1. `index` が 0〜4 の外なら `ArgumentOutOfRangeException`（catch で受ける）
  2. `idolId == 0` なら `await dialogs.ShowInfoAsync("先に育成アイドルを選んでください。");` して終わる
  3. `excludedUids` = 枠 `index` 以外の埋まっている所持ID（枠の順）、`excludedSupportCardIds` = その所持のカードの `OwnedSupportCard.CardId`（枠の順）
  4. `var request = OwnedSelectionRequest.ForSupportCard(master.GetIdol(idolId).Plan, excludedUids, excludedSupportCardIds);` → 育成アイドルと同じ 2・3 の手順（`PushPageAsync<OwnedPresenter, OwnedArgs>` → `await request.Task` → `disposed` なら終わる）。キャンセルなら終わる
  5. `slots[index] = selection.SupportCardUid;` `Refresh()`
- `ClearSlot(index)`: `running` なら何もしない。`index` が範囲外なら何もしない。`slots[index] = null;` `Refresh()`
- `NextScenario()`: `running` か `master.Scenarios.Count < 2` なら何もしない。`scenarioIndex = (scenarioIndex + 1) % master.Scenarios.Count;` `Refresh()`
- `StartAsync`: `idolId == 0` か `master.Scenarios.Count == 0` なら何もしない。`uids` = `slots` の `null` でないものを枠の順に（`List<string>`）。`var status = await start.ExecuteAsync(idolId, uids, master.Scenarios[scenarioIndex].ScenarioId);`
  - `Succeeded`: `await flow.ShowCurrentAsync();`（編成のページも閉じられる。以後は View に触らない）
  - `InvalidFormation`: `await dialogs.ShowInfoAsync("編成が正しくありません。");`
  - `AlreadyInProgress`: `await dialogs.ShowInfoAsync("進行中のプロデュースがあります。ホームから再開してください。");` → `await navigator.PopPageAsync(this, playAnimation: false);`（ユースケースが取り直し済みなので、ホームの `DidPopEnter` で「再開」が出る）
  - `InvalidArgument`: `await dialogs.ShowInfoAsync("開始できませんでした。");`
  - `Failed`: 何も出さない（共通のダイアログが出ている）
- `BackAsync`: `await navigator.PopPageAsync(this, playAnimation: false);`

**`OwnedArgs` について**: 3 本目（owned）が作る `public sealed class OwnedArgs { public OwnedArgs(OwnedSelectionRequest selection); public OwnedSelectionRequest Selection { get; } }`（`null` は閲覧モード）と `OwnedPresenter : IScreenWithArgs<OwnedArgs>` を使う。選ばれたら `OwnedPresenter` が `Selection.Complete(...)` して自分を Pop し、戻る・画面の破棄では `OwnedSelection.Cancelled` で完了する（3 本目の決まり）。

**画面のレイアウト**（`FormationPage.BuildUi(body)`。見た目は仮。高さは `UiFactory.SetHeight`）: `var root = UiFactory.CreateVertical(body, "Root");` の子に上から
1. "Header"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "Back"（`CreateButton("＜")`。`LayoutElement` の `flexibleWidth = 0`、`preferredWidth = 160`）と "Title"（`CreateText("編成", UiTheme.FontTitle, TextAnchor.MiddleCenter)`）
2. "IdolRow"（`CreatePanel`。`SetHeight(240)`）の子に "IdolText"（`CreateText("", UiTheme.FontBody, TextAnchor.MiddleLeft)`、`SetHeight(80)`）と "SelectIdol"（`CreateButton("育成アイドルを選ぶ")`）
3. "Slot0"〜"Slot4"（それぞれ `CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "Text"（`CreateText("", UiTheme.FontSmall, TextAnchor.MiddleLeft)`）・"Select"（`CreateButton("選ぶ")`。`flexibleWidth = 0`、`preferredWidth = 200`）・"Clear"（`CreateButton("外す")`。同じ幅）。`SetSlots` で Text を `Label` にし、"Clear" を `UiFactory.SetInteractable(clear, filled)`
4. "ScenarioRow"（`CreateHorizontal`。`SetHeight(UiTheme.ButtonHeight)`）の子に "ScenarioText"（`CreateText("", UiTheme.FontBody, TextAnchor.MiddleLeft)`）と "ChangeScenario"（`CreateButton("変更")`。`flexibleWidth = 0`、`preferredWidth = 200`）。`SetScenario` の `canChange` で `SetInteractable`
5. "Spacer"（`CreateRect`。`SetFlexibleHeight(1)`）
6. "Start"（`CreateButton("プロデュース開始")`）。`SetStartEnabled` で `SetInteractable`
- `onClick`: "Back" → `BackClicked`、"SelectIdol" → `IdolClicked`、"Slot{i}/Select" → `SlotClicked(i)`、"Slot{i}/Clear" → `SlotClearClicked(i)`、"ChangeScenario" → `ScenarioClicked`、"Start" → `StartClicked`（ループ変数は `var index = i;` で受けてからラムダに渡す）
- `LayoutElement` の幅は `GetComponent<LayoutElement>()`（`CreateButton` が `SetHeight` で付けたもの）に設定する

### 3.6 ホーム（`HomePresenter` の変更）

- コンストラクター（2 本）の**最後**（2・3 本目が足した引数の後ろ）に `ProduceSession session, ResumeProduceAsync` ではなく `ProduceSession session, ResumeProduceUseCase resume, AbandonProduceUseCase abandon, ProduceFlow flow` を足す
- `Refresh()` の `view.SetResumeVisible(...)` の引数を `HasActiveProduce()` にする。`private bool HasActiveProduce()`: `session.Current != null ? session.HasActive : cache.Current.HasActiveProduce`
- foundation の `OpenProduceAsync`・`ResumeProduceAsync`（「準備中です。」）を次に置き換える（どちらも実行中フラグ・`try`/`catch`/`finally`。catch は 1.1）:
  - `OpenProduceAsync`: `HasActiveProduce()` が false なら `await navigator.PushPageAsync<FormationPresenter>(playAnimation: false);`。true なら `var index = await dialogs.ChooseAsync("進行中のプロデュースがあります。", new[] { "再開", "放棄", "やめる" });` 0 → `await ResumeCoreAsync();`、1 → `ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？")` が `Positive` なら `await abandon.ExecuteAsync();` のあと `Refresh()`（結果が false でも呼ぶ。共通ダイアログは出ている）、2 → 何もしない
  - `ResumeProduceAsync`: `await ResumeCoreAsync();`
  - `private async UniTask ResumeCoreAsync()`: `var status = await resume.ExecuteAsync();` `Resumed` → `await flow.ShowCurrentAsync();`、`NotInProgress` → `await dialogs.ShowInfoAsync("進行中のプロデュースはありません。");` のあと `Refresh()`、`Failed` → 何もしない
- ホームから `ShowCurrentAsync` を呼んだとき、`PopToRootAsync` はホーム 1 枚なので何もしない。この計画の時点では局面の画面が無いので「この局面の画面は準備中です: …」が出てホームに残る

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

## 4. タスク

各タスクの終わりに、`dotnet build Uta.slnx`（server・shared を触ったタスク。警告 0・エラー 0）、`unity recompile --format json`（client を触ったタスク。`client/uta` で実行。コンパイルエラー 0）、そのタスクまでのテストが通る状態にする。EditMode のテストは `unity test --mode EditMode`（Editor が要る。動かなければ Editor の Test Runner。foundation のタスク 1 の結果に従う）。**.NET SDK・Docker・Unity の Editor が無い環境では、通ったと書かず、確かめていないと報告する**。

### タスク 1: サーバー・shared の `UsableHand` と wiki

触る: `LessonDto.cs`、`ProduceView.cs`、`ProduceViewBuilder.cs`、`ProduceResponseMapper.cs`、`ProduceOperationTests.cs`、`produce-server.md`、`lesson-domain-types.md`、`produce-domain.md`。

手順:
1. 3.1 のとおり直す
2. `ProduceOperationTests.cs` に 5 章の 2 つを足す（既存の補助メソッド `StartAsync(postgres)`・`StartProduceAsync`・`StartLessonAsync`・`GetStateAsync` を使う）
3. [produce-server.md](../wiki/architecture/server/produce-server.md):
   - 6章の `LessonDto` の行の `UsableHand` の括弧の最後（「2026-11-03 追加」）を「2026-11-03 追加。**Key は末尾の 19**（既存のキーを動かさないため。表は意味の近さで `Hand` の次に置く）」に直す。行を足さない（すでにある）
   - 5章の「破棄(`SignInAsync`・`StartAsync` の中)」の「楽観ロックつき」の後ろに「。0 行なら `PRODUCE_STALE_OPERATION`」を、「`AbandonAsync`」の「(楽観ロックつき)」を「(楽観ロックつき。0 行なら `PRODUCE_STALE_OPERATION`)」に直す
   - 実装メモの最後に「- **`UsableHand`** [2026-11-03。計画 `plans/2026-11-03-client-screens-formation.md`]: `ProduceViewBuilder` が `ProduceEngine.CanUse(state, id)` で作る（`Lesson` が無ければ空）。`Uta.Shared.Domain` に公開メソッドは足していない」を足す
4. [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) 5章の `LessonEngine` の行の「`CanUse`（乱数なし。クライアントの予測にも使う。**MVP のクライアントの予測は使用可否だけ**で、効果の見込みは出さない [決定。2026-11-01。ユーザーの判断]）」を「`CanUse`（乱数なし。サーバーが `LessonDto.UsableHand` を作るのに使う。**MVP のクライアントは使用可否もサーバーの `UsableHand` を使い、`CanUse` を呼ばない** [決定。2026-11-03。[screen-design.md](screen-design.md) 6章]。効果の見込みは出さない [決定。2026-11-01。ユーザーの判断]）」に直す
5. [produce-domain.md](../wiki/architecture/produce-domain.md) 1章の最初の箇条書き「…Shared.Domain で使用可否の判定と予測の表示までを行う。…」の文の後ろに「（MVP の使用可否はサーバーの `LessonDto.UsableHand` を使う: [screen-design.md](screen-design.md) 6章）」を足す

確かめ方: `dotnet build Uta.slnx`、`dotnet test --project shared/Uta.Shared.Domain.Tests`（既存が壊れていない）、`dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）。`python -I .claude/skills/linting-wiki/lint_wiki.py` のエラーが 0。

### タスク 2: Application の型・ポート・ユースケースと偽

触る: `Application/Produce/*`（2 章の 2 行）、`Tests/EditMode/Fakes/FakeProducePort.cs`・`FakePlayerPort.cs`・`TestProduce.cs`、`Tests/EditMode/ProduceSessionTests.cs`・`ProduceProgressTests.cs`・`StartProduceUseCaseTests.cs`・`ResumeProduceUseCaseTests.cs`・`AbandonProduceUseCaseTests.cs`・`ProduceActionsTests.cs`。

手順: 3.2・3.7 のとおり作る。
境界: `ProduceSession.Set(null)` は `ArgumentNullException`。`ProduceProgress.GetParam` は `Singing`・`Rhythm`・`Expression` 以外で `ArgumentOutOfRangeException`。写しの型の配列の引数が `null` なら `ArgumentNullException`。`ProduceActions` は `session.Current == null` で `InvalidOperationException`。
確かめ方: 5 章のタスク 2 の行のテスト。

### タスク 3: API・ゲートウェイ

触る: `Infrastructure/Api/ProduceApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Produce/ProduceGateway.cs`、`Tests/EditMode/ProduceGatewayTests.cs`。
手順: 3.3 のとおり。
確かめ方: `ProduceGatewayTests`。`unity recompile` のあと `unity command console get --tail 50 --level error --format json` のエラーが 0。

### タスク 4: ProduceFlow・ProduceMenu・背景のページ・編成

触る: `Presentation/Produce/*`、`Presentation/Formation/*`、`Tests/EditMode/ProduceFlowTests.cs`・`ProduceMenuTests.cs`・`ProduceBackdropPresenterTests.cs`・`FormationPresenterTests.cs`。
手順: 3.4・3.5 のとおり。
確かめ方: 5 章のタスク 4 の行のテスト。

### タスク 5: ホームの接続と登録

触る: `Presentation/Home/HomePresenter.cs`、`Bootstrap/UtaBootstrap.cs`、`Tests/EditMode/HomePresenterTests.cs`。
手順:
1. 3.6 のとおり `HomePresenter` を直す
2. `UtaBootstrap` の DI の登録（foundation 3.8 の並びの後ろ）に、`builder.Register<ProduceGateway>(Lifetime.Singleton).As<IProducePort>();`、`builder.Register<ProduceSession>(Lifetime.Singleton);`、`builder.Register<StartProduceUseCase>(Lifetime.Singleton);`、`builder.Register<ResumeProduceUseCase>(Lifetime.Singleton);`、`builder.Register<AbandonProduceUseCase>(Lifetime.Singleton);`、`builder.Register<ProduceActions>(Lifetime.Singleton);`、`builder.Register<ProduceFlow>(Lifetime.Singleton);`、`builder.Register<ProduceMenu>(Lifetime.Singleton);` を足す。Presenter は登録しない（foundation 3.3.2）
3. `RegisterScreens` に `uiRoot.RegisterPage<FormationPage>();` と `uiRoot.RegisterPage<ProduceBackdropPage>();` を足す
4. `HomePresenterTests` の既存のテストの `HomePresenter` の生成に、新しい引数（`new ProduceSession()`、`resume`・`abandon`・`flow` は `null`）を足す。5 章の追記のテストを足す
確かめ方: `HomePresenterTests`（既存と追記）。

### タスク 6: 通信の確認と記録

1. **Editor の Play モード**（サーバーを起動した状態。CLAUDE.md「ローカルのサーバー」）: `unity command editor.playmode start` のあと、手で次を確かめる: ホーム →「プロデュース」→ 編成（既定の育成アイドルが入っている。「選ぶ」で所持一覧の選択モードが開き、選ぶと戻ってラベルが変わる。サポートカードを 1 枚入れる）→「プロデュース開始」→「この局面の画面は準備中です: WeekSelect」が出て、閉じるとホームに戻り「再開」が出る。「再開」→ 同じダイアログ。「プロデュース」→「進行中のプロデュースがあります。」→「放棄」→ 確認 →「再開」が消える。終わったら `unity command console get --tail 50 --level error --format json` のエラーが 0 → `unity command editor.playmode stop`
2. **Windows の IL2CPP**（CLAUDE.md の「ビルド・テスト・実行」のビルドのコマンド。Editor を閉じてから）: できた `client/uta/Build/Windows/Uta.exe` を `-batchmode` なしで起動し、上の 1 と同じ操作を手で確かめる。結果を [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 2章の表に 1 行で足す: 「`IProduceService` の `StartAsync`・`GetStateAsync`・`AbandonAsync`（IL2CPP）。確かめた範囲は局面 `WeekSelect` の応答（最上位の値・enum、`Deck`・`WeekActions`・`ClassOptions`・`CardOffers` の配列、`Lesson`・`ExamResult`・`Result` は `null`）と `ProduceResultResponse`（放棄）。`LessonDto`（`bool[]` を含む）・`ExamResultDto` は未確認（6 本目）」。通らなければ止まって報告する。Editor を閉じられない・ビルドできない環境では、確かめていないと報告する
3. [screen-design.md](../wiki/architecture/screen-design.md) を直す:
   - 7.3 の 1 つ目の箇条書きの「サインインはプロデュースの破棄（古いセッション）も行うが、実行中のクライアントのバージョンは変わらないので起きない」を「サインインはプロデュースの破棄（古いセッション）も行う。実行中のクライアントでも、サーバーの更新（`state_version`・`rule_version`）で起きうる。プロデュースの画面からの取り直しで返った `DiscardedProduce` は画面に出さない（起動のときだけ出す）」に直す
   - 7.4 の「最新の `ProduceStateView` と次の操作番号を持つ」を「最新の `ProduceProgress`（応答の写し）を持ち、次の操作番号は `Current.OperationNumber`」に、「操作の前後の `ProduceStateView` の差」を「操作の前後の `ProduceProgress` の差（`ProduceSession.Previous` と `Current`）」に直す
   - 7.4 の 3 つ目の箇条書きを「`PRODUCE_STALE_OPERATION`・`PRODUCE_UNEXPECTED_PHASE` を受けたら `GetStateAsync` で取り直して同じ流れに戻す。`PRODUCE_SESSION_DISCARDED`・`PRODUCE_NOT_IN_PROGRESS` を受けたらホームに戻る（サインインで取り直す）」に直す
   - 7.4 の最後に「- 局面の切り替えは、ホームの上のページ・モーダルを全部 `Pop` してから次の局面の画面を `Push` する（`ProduceFlow.ShowCurrentAsync`）。カードの選択肢は、背景のページ（`ProduceBackdropPage`。週・パラメータ・体力）の上にモーダルで出す。取り直しは状態を変えたユースケースが行い、`ProduceFlow` は行わない [Uta案。2026-11-03]」を足す
   - 「実装メモ」に、設計とのズレを書く（なければ「順 7 の 4 本目: なし」）
4. [produce-server.md](../wiki/architecture/server/produce-server.md) の実装メモの「未確認: Unity 7(IL2CPP)での `Uta.Shared.Contracts/Produce` のコンパイルと MessagePack の動作」を、2 の結果に合わせて「…`WeekSelect` の応答までは確かめた（[unity7-evaluation.md](../unity7-evaluation.md) 2章）。`LessonDto`・`ExamResultDto` は未確認」に直す（確かめられなかったら直さない）
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` のエラーが 0。この計画を `done` にする

## 5. テスト

EditMode のテストは foundation 3.3.7 の書き方（偽の View・`FakeScreenNavigator`・`FakeDialogService`・`FakeProducePort`・`FakePlayerPort` を同じ `CallLog` で作り、`log.Entries` の並びで確かめる。非同期は `[UnityTest]` + `UniTask.ToCoroutine`）。`RefreshPlayerUseCase` は本物を `new RefreshPlayerUseCase(fakePlayerPort, cache)` で作る。

| ファイル | テスト | 内容 |
|---|---|---|
| `ProduceOperationTests.cs`（server、結合。タスク 1） | `Lesson_Response_HasUsableHandSameLengthAsHand` | `var lesson = await StartLessonAsync(player);` → `Assert.NotNull(lesson.Lesson.UsableHand)`、`Assert.Equal(lesson.Lesson.Hand.Length, lesson.Lesson.UsableHand.Length)`。続けて `GetStateAsync(player, "req-5")` の応答でも同じ長さ |
| | `WeekSelect_Response_HasNoLesson` | `StartProduceAsync(player, "req-3")` の応答の `Lesson` が `null`（`UsableHand` を読む経路が無い） |
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
| `ProduceGatewayTests.cs`（タスク 3） | `ToProgress_MapsAllFields` | 入れ子の `LessonDto`（`UsableHand` 含む）・`ExamResultDto`・`ProduceResultResponse` を写す |
| | `ToProgress_PhaseNothing_ReturnsEmpty` | 配列は空、`Lesson`・`ExamResult`・`Result` は `null` |
| | `ToProgress_NullArrays_BecomeEmpty` | `UsableHand == null` も空 |
| | `ToOutcome_MapsEnding` | `ProduceEnding.Abandoned` → `ProduceEndingKind.Abandoned` |
| `ProduceFlowTests.cs`（タスク 4） | `ShowCurrent_UnsupportedPhase_PopsToRootThenShowsInfo` | `PageStack(() => 2, () => 0)`。`log` が `"PopPage:1"`, `"Info:この局面の画面は準備中です: WeekSelect"` |
| | `ShowCurrent_NoSession_Throws` | |
| | `ExitToHome_PopsModalsAndPages` | `PageStack(() => 3, () => 1)` → `"PopModal:1"`, `"PopPage:2"`。`"SignIn"` が無い |
| | `HandleStep_SessionEnded_ShowsInfoThenExits` | `"Info:プロデュースは終了しています。"` のあと `"PopPage:…"` |
| | `HandleStep_Rejected_ShowsInfo` / `HandleStep_Failed_DoesNothing` | |
| | `HandleStep_SucceededAndRecovered_ShowCurrent` | どちらも `"PopPage:1"`, `"Info:この局面の画面は準備中です: …"` |
| `ProduceMenuTests.cs` | `Show_GoHome_ExitsWithoutAbandon` | `ChooseResults` 0 → `"Choose:メニュー:ホームへ戻る（あとで再開できます）/プロデュースを放棄する/閉じる"`, `"PopPage:1"`。`"Abandon"` が無い |
| | `Show_AbandonConfirmed_AbandonsThenExits` | 1・Positive → `"Confirm:放棄すると報酬は受け取れません。放棄しますか？"`, `"Abandon"`, `"SignIn"`, `"PopPage:1"` |
| | `Show_AbandonDeclined_DoesNothing` / `Show_AbandonFailed_DoesNotExit` / `Show_Close_DoesNothing` | |
| `ProduceBackdropPresenterTests.cs` | `Initialize_SetsHeader` | `"SetHeader:" + BuildHeader(...)`。`BuildHeader` の文字列が 3.4 のとおり（週 3、100/110/120、体力 25 / 30） |
| `FormationPresenterTests.cs` | `Initialize_DefaultsToHighestRarityIdol` | `FakeGameMaster` に育成アイドル 1（SR・Sense）・2（SSR・Logic）・3（SSR・Sense）、所持 1・2・3 → `"SetIdol:育成アイドル: <2 の名前> [" + DisplayText.Rarity(Rarity.SSR) + "] " + DisplayText.Plan(Plan.Logic)` |
| | `Initialize_NoIdols_StartDisabled` | `"SetIdol:育成アイドル: 未選択"`, `"SetStartEnabled:False"` |
| | `Initialize_ShowsEmptySlotsAndScenario` | `SetSlots` の 5 件が `"枠1: （空）"`〜`"枠5: （空）"`、`"SetScenario:シナリオ: <1 つ目の名前>,True"`（シナリオ 2 つ） |
| | `SelectIdol_Selected_ChangesIdolAndClearsIncompatibleSupport` | 育成アイドル 3（Sense）で枠 0 に Sense のカード・枠 1 に Free のカード。`var task = presenter.SelectIdolAsync();` → `log` に `"PushPage:OwnedPresenter"` → `((OwnedArgs)navigator.Args[0]).Selection.Complete(OwnedSelection.ForIdol(2));` → `await task;` → 枠 0 が空、枠 1 は残る、最後が `"Info:プランが合わないサポートカードを外しました。"` |
| | `SelectIdol_Cancelled_KeepsState` | `OwnedSelection.Cancelled` → `SetIdol` が増えない |
| | `SelectIdol_DisposedWhileSelecting_DoesNothing` | 選択中に `presenter.Dispose()` → 完了させても `SetIdol` が増えない |
| | `SelectSlot_NoIdol_ShowsInfo` | `"Info:先に育成アイドルを選んでください。"`、Push なし |
| | `SelectSlot_BuildsRequestWithExclusions` | 枠 1・2 が埋まった状態で枠 0 を選ぶ → `OwnedArgs.Selection` を `Complete(OwnedSelection.ForSupportCard("u9"))` → 枠 0 が "u9"。要求の除外は 3 本目の `OwnedListBuilder.BuildSupportCards` で確かめる（枠 1・2 の所持ID が `AlreadyInFormation`、同じ `CardId` の別の所持が `SameCardInFormation`） |
| | `ClearSlot_ClearsSlot` / `NextScenario_Cycles` | |
| | `Start_Success_CallsShowCurrent` | `"Start:3:u1,u2:<ScenarioId>"`（枠 0・2 に u1・u2、枠 1 は空）のあと `"PopPage:1"`（`PageStack(() => 2, () => 0)`）, `"Info:この局面の画面は準備中です: WeekSelect"` |
| | `Start_InvalidFormation_ShowsInfo` | |
| | `Start_AlreadyInProgress_ShowsInfoAndPops` | `"SignIn"`, `"Info:進行中のプロデュースがあります。ホームから再開してください。"`, `"PopPage:FormationPresenter"` |
| | `Start_WhileRunning_Ignored` | `FakeDialogService.InfoGate` で 1 回目を止め（`InvalidFormation`）、2 回目の `StartAsync` は `"Start:"` を増やさない |
| | `Start_Exception_ShowsError` | `StateResults` を空にする → `"Info:エラーが発生しました: FakeProducePort: no state result"` |
| | `Back_PopsSelf` | `"PopPage:FormationPresenter"` |
| `HomePresenterTests.cs`（追記。タスク 5） | `OpenProduce_NoActive_PushesFormation` | `"PushPage:FormationPresenter"` |
| | `OpenProduce_Active_Resume_ShowsCurrent` | キャッシュ `hasActiveProduce = true`、`ChooseResults` 0、`StateResults` に `WeekSelect` → `"Choose:進行中のプロデュースがあります。:再開/放棄/やめる"`, `"GetState"`, `"Info:この局面の画面は準備中です: WeekSelect"` |
| | `OpenProduce_Active_AbandonConfirmed_HidesResume` | 1・Positive、`FakePlayerPort.Snapshot` を `hasActiveProduce = false` に → `"Abandon"`, `"SignIn"`, `"SetResumeVisible:False"` |
| | `OpenProduce_Active_Cancel_DoesNothing` | 2 → `"Choose:…"` のあと何も無い |
| | `ResumeProduce_NotInProgress_ShowsInfoAndRefreshes` | `Nothing` → `"GetState"`, `"SignIn"`, `"Info:進行中のプロデュースはありません。"`, `"SetResumeVisible:False"` |
| | `Refresh_ActiveSession_ShowsResumeEvenIfCacheInactive` / `Refresh_FinishedSession_HidesResume` | `session.Set(TestProduce.Progress(WeekSelect))` → `"SetResumeVisible:True"`、`Finished` → `False`（キャッシュが true でも） |

Play モード・IL2CPP・手動はタスク 6。

## 6. 未決事項

- （進行役が照合する）3 本目（owned）の書き直し後の名前: `OwnedArgs`（`Selection`）・`OwnedSelectionRequest.ForIdol()`／`ForSupportCard(Plan, IReadOnlyCollection<string>, IReadOnlyCollection<int>)`・`OwnedSelection`（`IsCancelled`・`IdolId`・`SupportCardUid`）・`FormationValidator.IsPlanCompatible`・`DisplayText.Param`。2 本目（gacha）の `PortResult<T>`・`ServerReasons`・`FakeGameMaster`（`IdolList`・`SupportCardList`・`ScenarioList`）。違っていたら、この計画を合わせる
