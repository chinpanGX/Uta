---
title: 実装計画: client/ の画面 6/6 レッスン・試験・試験結果・プロデュース結果
status: draft
updated: 2026-11-03
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 6/6 レッスン・試験・試験結果・プロデュース結果

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 6 本目（最後）。プロデュースの残りの局面の画面を作る: レッスン・試験（`ProducePhase.Lesson`・`Exam`）、試験結果（`ExamResult`、および試験が終わった応答の `ExamResult`）、プロデュース結果（`Finished`）。これで、起動 → ガチャ → 強化 → 編成 → プロデュースを最後まで → 結果、が画面だけで通る（完了条件 6）。レッスンの仕様は [lesson-card.md](../wiki/systems/lesson-card.md) と [produce.md](../wiki/systems/produce.md) 4・5・6章、局面の遷移は produce.md 5.1.1・10.1、DTO は [produce-server.md](../wiki/architecture/server/produce-server.md) 6章、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 3・7・8。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 6・7章。

- **前提**: 1〜5 本目が done（`ProduceSession`・`ProduceActions`・`ProduceFlow`・`ProduceMenu`・`CardSummaryBuilder`・`DisplayText`・`FakeProduceFlow`・`LessonView.UsableHand`（4 本目のサーバーの直し）を使う）
- **決めないこと / 作らないもの**: ドリンク・育成アイテム・山札/捨札/除外の一覧・ステータスの説明（MVP 外または未対応）、カードの予測値（MVP は使用可否だけ: [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md)）、カードの効果の説明文、**ターン終了時の効果を 1 件ずつ見せる表示**（[screen-wireframes.md](../wiki/overview/screen-wireframes.md) 3 の決定。サーバーの応答に効果の記録が無く、状態の差しか分からないため、MVP では出さない。出すには応答に効果の記録を足す設計が要る）、評価値の内訳（応答に項目が無い。合計だけ出す）、特別レッスンの印以外の演出
- **決めたこと（この計画で）**: カードをタップ → 確認ダイアログ → 使用（叩き台の「詳細 → 使用」と同じ結果）。スキップは確認なし（叩き台の決定）。レッスン・試験のあいだの画面の更新は、同じページの中で行う（ページを作り直さない）。試験結果の順位表の「ライバル」の名前は仮に「ライバル 1・2…」とする
- **完了条件**: 6 を手動で確かめる（Windows。この計画のタスク 5）。7（Android）はこの計画の範囲外

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Produce/ExamRanking.cs` | 試験の順位表と、試験中の見込みの順位 |
| 変更 | `Scripts/Presentation/Common/DisplayText.cs` | レッスンの種類・ステータス・ランク・結果の文字 |
| 作成 | `Scripts/Presentation/Produce/ILessonView.cs`・`LessonPage.cs`・`LessonPresenter.cs`・`HandCardViewData.cs` | レッスン・試験 |
| 作成 | `Scripts/Presentation/Produce/IExamResultView.cs`・`ExamResultPage.cs`・`ExamResultPresenter.cs` | 試験結果 |
| 作成 | `Scripts/Presentation/Produce/IProduceResultView.cs`・`ProduceResultPage.cs`・`ProduceResultPresenter.cs` | プロデュース結果 |
| 変更 | `Scripts/Presentation/Produce/ProduceFlow.cs` | 局面 `Lesson`・`Exam`・`ExamResult`・`Finished` と、試験結果の先出し |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | Presenter の登録 |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |
| 変更 | `uta_docs/wiki/overview/game-spec.md` | 順 7 の行を `done` にして、6 本の計画へのリンクを書く |

## 3. 型とシグネチャ

C# は C# 9 の範囲。

### 3.1 `Uta.Client.Application.Produce.ExamRanking`

```csharp
public sealed class ExamRankingRow
{
    public ExamRankingRow(int rank, bool isPlayer, int score);
    public int Rank { get; } public bool IsPlayer { get; } public int Score { get; }
}
public static class ExamRanking
{
    /// <summary>自分のスコアと NPC のスコアを、スコアの降順に並べる。同点は自分が先。順位は 1 から並び順に振る（同点の NPC も並び順の番号）。</summary>
    public static IReadOnlyList<ExamRankingRow> Build(int playerScore, IReadOnlyList<int> npcScores);
    /// <summary>試験中の見込みの順位 = 1 + スコアが playerScore より大きい NPC の数（ProduceFormulas.ExamRank と同じ）。</summary>
    public static int CurrentRank(int playerScore, IReadOnlyList<int> npcScores);
}
```

`CurrentRank` は `Uta.Shared.Domain.Produce.ProduceFormulas.ExamRank` を呼ぶ。

### 3.2 `DisplayText` の追加

```csharp
public static string LessonKind(LessonKind kind);      // Normal → "レッスン"、Special → "特別レッスン"、Finishing → "仕上げレッスン"、MidExam → "中間試験"、FinalExam → "最終試験"、Nothing → "-"
public static string Status(StatusKind kind);          // GoodCondition → "好調"、Excellent → "絶好調"、Focus → "集中"、FocusBoost → "集中増加"、Impression → "好印象"、ImpressionBoost → "好印象増加"、Motivation → "やる気"、CostHalf → "消費半減"、CostDouble → "消費倍"、CostReduce → "消費減少"、ShieldGainBlock → "元気増加不可"、DebuffGuard → "低下無効"、Nothing → "-"（仮称）
public static string Rank(ProduceRank rank);           // F E D C "C+" B "B+" A "A+" S "S+" SS "SS+"（"Plus" を "+" にする）、Nothing → "-"
public static string ExamOutcome(ExamOutcome outcome); // MidFailed → "中間試験で不合格"、FinalFailed → "最終試験で不合格"、FinalPassed → "最終試験に合格"、Nothing → ""
public static string ExamKind(ExamKind kind);          // Mid → "中間試験"、Final → "最終試験"、Nothing → "-"
public static string Thousands(int value);             // 3 桁区切り（1,234）。CultureInfo.InvariantCulture の "N0"
```

### 3.3 `Uta.Client.Presentation.Produce`

```csharp
public sealed class HandCardViewData
{
    public HandCardViewData(int instanceId, string title, string costs, bool enabled);
    public int InstanceId { get; } public string Title { get; } public string Costs { get; } public bool Enabled { get; }
}

public interface ILessonView
{
    void SetHeader(string title, string turnText);               // "律動レッスン（特別）"、"ターン 3 / 6"
    void SetProgress(IReadOnlyList<string> lines);               // 3.3.1
    void SetStatus(IReadOnlyList<string> lines);                 // 3.3.1
    void SetHand(IReadOnlyList<HandCardViewData> cards);
    event Action<int> CardClicked;                               // instanceId
    event Action SkipClicked;
    event Action MenuClicked;
}
public sealed class LessonPage : PageBase, ILessonView { ... }

public sealed class LessonPresenter : Presenter<ILessonView>
{
    public LessonPresenter(ProduceSession session, ICardCatalog catalog, ProduceActions actions, ProduceFlow flow, ProduceMenu menu, DialogService dialogs);
}

public interface IExamResultView
{
    void SetTitle(string text);                                  // "中間試験 結果（2 回目）"
    void SetRanking(IReadOnlyList<string> rows);                 // "1位 ライバル 1　1,440" "2位 あなた　1,388"
    void SetOutcome(string text);                                // "合格！" / "不合格"
    void SetButtons(string retryLabel, string nextLabel);        // retryLabel が null なら再挑戦のボタンを出さない
    event Action RetryClicked;
    event Action NextClicked;
}
public sealed class ExamResultPage : PageBase, IExamResultView { ... }
public sealed class ExamResultPresenter : Presenter<IExamResultView>
{
    public ExamResultPresenter(ProduceSession session, ProduceActions actions, ProduceFlow flow, DialogService dialogs);
}

public interface IProduceResultView
{
    void SetRank(string text);                                   // "ランク A+"
    void SetLines(IReadOnlyList<string> lines);
    event Action HomeClicked;
}
public sealed class ProduceResultPage : PageBase, IProduceResultView { ... }
public sealed class ProduceResultPresenter : Presenter<IProduceResultView>
{
    public ProduceResultPresenter(ProduceSession session, ProduceFlow flow);
}
```

#### 3.3.1 `LessonPresenter` の動き

- `Refresh()`（`OnBind` で呼ぶ。操作のあとも呼ぶ）: `state = session.Current`、`lesson = state.Lesson`（`null` なら何もしない）。
  - `SetHeader`: `title` = `DisplayText.LessonKind(lesson.Kind)` に、レッスン（`!IsExam`）なら `（<パラメータ>）`（`DisplayText.Param(lesson.TargetParam)`）を付ける（例 `"特別レッスン（律動）"`）。`turnText` = `$"ターン {lesson.Turn} / {lesson.MaxTurns}"`（試験の `MaxTurns` は審査ターンの数）
  - `SetProgress`: レッスン: `$"獲得 {lesson.ParamGained}　クリア目標 {lesson.ClearTarget}　パーフェクト {lesson.PerfectTarget}"`。試験: `$"スコア {DisplayText.Thousands(lesson.ParamGained)}　順位 {ExamRanking.CurrentRank(lesson.ParamGained, lesson.NpcScores)} 位"`、`1 <= lesson.Turn <= lesson.ExamTurns.Count` のとき `$"このターン: {DisplayText.Param(turn.Param)}（ボーナス {turn.ScoreBonusPercent}%）"`、そのあとに続くターンがあれば `"次: " + 残りのパラメータを "→" でつないだもの`（`DisplayText.Param`）
  - `SetStatus`: `$"体力 {state.Hp} / {state.MaxHp}"`、`$"元気 {lesson.Shield}"`、`lesson.StatusValues` の添字 `i`（`(StatusKind)i`）で値が 0 でないものを `$"{DisplayText.Status(kind)} {value}"`（`StatusKind.Nothing` は飛ばす）、`$"山札 {lesson.DrawPile.Count}　捨札 {lesson.Discard.Count}　除外 {lesson.Exiled.Count}"`、`$"使用できる回数 {lesson.UsesLeft}"`
  - `SetHand`: `lesson.Hand` の各 `instanceId`（添字 `i`）→ `deckCard = state.Deck` から `InstanceId` で探す（無ければ `title = "カード？"`、`costs = ""`）。`enhanced = deckCard.Enhanced || lesson.LessonEnhanced` に `instanceId` を含む。`summary = CardSummaryBuilder.Build(catalog, deckCard.CardId, enhanced)`。`title = DisplayText.Card(summary)`、`costs = DisplayText.Costs(summary)`、`enabled = i < lesson.UsableHand.Count && lesson.UsableHand[i]`（`UsableHand` が空のときは `false` ではなく **`true`**。応答に判定が無い古いサーバーでも操作できるようにする）
- `CardClicked(instanceId)`（実行中は無視）: 手札に無い・`enabled` が false なら何もしない。`ConfirmAsync($"{title}（{costs}）を使用します。よろしいですか？")` が `Positive` なら `await StepAsync(LessonActionType.UseCard, instanceId)`
- `SkipClicked`（実行中は無視）: 確認なしで `await StepAsync(LessonActionType.Skip, 0)`
- `StepAsync(kind, instanceId)`: `status = await actions.ExecuteLessonActionAsync(kind, instanceId)`:
  - `Rejected`: `dialogs.ShowInfoAsync("その操作はできません。")` のあと `Refresh()`
  - `Failed`: 何もしない
  - `Succeeded`・`Recovered` で、`session.Current.Phase` が `Lesson` か `Exam` で、`session.Current.ExamResult == null` のとき: ページを作り直さず `Refresh()`
  - それ以外（局面が変わった・`SessionEnded`）: `await flow.HandleStepAsync(status);`
  - 例外: `dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`
- `MenuClicked` → `await menu.ShowAsync();`

#### 3.3.2 `ExamResultPresenter` の動き

- `OnBind`: `state = session.Current`、`result = state.ExamResult`（`null` なら `dialogs.ShowInfoAsync("結果がありません。")` のあと `flow.AdvanceAsync()`）。
  - `SetTitle`: `$"{DisplayText.ExamKind(result.Kind)} 結果（{result.Attempt} 回目）"`
  - `SetRanking`: `ExamRanking.Build(result.Score, result.NpcScores)` の各行を `$"{row.Rank}位 {(row.IsPlayer ? "あなた" : "ライバル " + n)}　{DisplayText.Thousands(row.Score)}"`（`n` は NPC の行に上から 1, 2, … と振る）
  - `SetOutcome(result.Passed ? "合格！" : "不合格")`
  - `retryAvailable = state.Phase == ProducePhase.ExamResult`（再挑戦の判断を待つ局面）。`SetButtons`: `retryAvailable` なら `retryLabel = $"再挑戦（残り {state.ExamRetryLeft}）"`、`nextLabel = result.Passed ? "次へ" : "プロデュース終了"`。そうでなければ `retryLabel = null`、`nextLabel = "次へ"`
- `RetryClicked`（実行中は無視）: `ConfirmAsync("試験を最初からやり直します。よろしいですか？")` が `Positive` なら `status = await actions.DecideExamRetryAsync(true)` → `Rejected` は `dialogs.ShowInfoAsync("その操作はできません。")`、`Failed` は何もしない、それ以外は `await flow.HandleStepAsync(status)`
- `NextClicked`（実行中は無視）:
  - `retryAvailable` のとき: 合格でなければ `ConfirmAsync("プロデュースを終了します。評価と報酬が出ます。よろしいですか？")` が `Positive` のときだけ、合格なら確認なしで、`status = await actions.DecideExamRetryAsync(false)` → `Rejected`・`Failed` は上と同じ、それ以外は `await flow.HandleStepAsync(status)`
  - そうでないとき（結果を見せるだけ）: `session.ExamResultShown = true;` → `await flow.AdvanceAsync();`
- 例外: `dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`

#### 3.3.3 `ProduceResultPresenter` の動き

- `OnBind`: `result = session.Result`。`null` なら `SetRank("-")`、`SetLines(["結果はありません。"])`。それ以外:
  - `SetRank($"ランク {DisplayText.Rank(result.Rank)}")`
  - `SetLines`: `"プロデュース完了"`（`result.Ending` が `Completed` 以外のときは `"プロデュース終了"`）、`$"評価値 {DisplayText.Thousands(result.EvaluationValue)}"`、`result.FinalRank > 0` なら `$"最終試験 {result.FinalRank}位　スコア {DisplayText.Thousands(result.FinalScore)}"`、`DisplayText.ExamOutcome(result.ExamOutcome)`（空でなければ）、`"報酬"`、`$"ガチャ通貨 +{result.GachaCurrency}"`、`$"ゴールド +{result.Gold}"`、`$"サポート強化pt +{result.SupportPoints}"`、`$"経験値 +{result.Exp}　（プレイヤーLv {result.PlayerLevel}）"`
- `HomeClicked`: `session.Clear();` → `await flow.ExitToHomeAsync();`

#### 3.3.4 `ProduceFlow` に足す局面

`ShowAsync(bool replace)` の先頭に、局面の `switch` の前の判定を足す:

1. `state.ExamResult != null && !session.ExamResultShown` なら、`replace ? ReplacePageAsync<ExamResultPage, ExamResultPresenter>() : PushPageAsync<ExamResultPage, ExamResultPresenter>()` して終わる（局面が何であっても先に試験結果を見せる）

`switch` に足す `case`（`replace` の扱いは `WeekSelect` と同じ。`Replace`／`Push`）:
- `ProducePhase.Lesson`・`ProducePhase.Exam`: `LessonPage` / `LessonPresenter`
- `ProducePhase.ExamResult`: `ExamResultPage` / `ExamResultPresenter`（1. で処理済みのはずなので、ここに来るのは `ExamResult == null` のとき。`dialogs.ShowInfoAsync("結果がありません。")` のあと `ExitToHomeAsync()`）
- `ProducePhase.Finished`: `ProduceResultPage` / `ProduceResultPresenter`

これで 4 本目の「準備中」の分岐は、`Nothing` と未知の値だけに残る。

#### 3.3.5 画面のレイアウト（`BuildUi`。見た目は仮）

- `LessonPage`: ヘッダー行（`title`、右に「≡」）、ターンの Text、進行の Text（`SetProgress` の行を改行でつなぐ）、状態の Text（同様）、手札を `CreateScrollContent` の中のボタン（`Title` と `Costs` の 2 行。`enabled == false` は `SetInteractable(false)`）で縦に並べる、下に「スキップ」ボタン
- `ExamResultPage`: タイトル、順位表（`CreateScrollContent`、行ごとの Text。自分の行は `UiTheme.Accent` の色）、`SetOutcome` の Text、ボタン（`retryLabel` があれば「再挑戦」と `nextLabel` を縦に 2 つ、なければ 1 つ）
- `ProduceResultPage`: ランクの Text（`UiTheme.FontTitle`）、行の Text、「ホームへ」ボタン

## 4. タスク

各タスクの終わりに `unity recompile --format json` でコンパイルエラー 0、そのタスク分のテストが通る状態にする。Editor を開けないときは、確かめていないと報告する。

### タスク 1: 順位表と文字

触る: `Application/Produce/ExamRanking.cs`、`Presentation/Common/DisplayText.cs`。
手順: 3.1・3.2 のとおり。
確かめ方: `ExamRankingTests`・`DisplayTextLessonTests`。

### タスク 2: レッスン・試験の画面

触る: `Presentation/Produce/ILessonView.cs`・`LessonPage.cs`・`LessonPresenter.cs`・`HandCardViewData.cs`、`ProduceFlow.cs`（`Lesson`・`Exam` の `case`）、`Bootstrap/UtaBootstrap.cs`。
手順: 3.3.1・3.3.4（`Lesson`・`Exam`）・3.3.5 のとおり。`UtaBootstrap` に `Register<LessonPresenter>(Transient)`。
確かめ方: `LessonPresenterTests`、`ProduceFlowTests`（追記）。

### タスク 3: 試験結果・プロデュース結果の画面

触る: `Presentation/Produce/IExamResultView.cs` ほか、`ProduceFlow.cs`（試験結果の先出し・`ExamResult`・`Finished`）、`Bootstrap/UtaBootstrap.cs`。
手順: 3.3.2・3.3.3・3.3.4・3.3.5 のとおり。`UtaBootstrap` に `Register<ExamResultPresenter>(Transient)`、`Register<ProduceResultPresenter>(Transient)`。
確かめ方: `ExamResultPresenterTests`・`ProduceResultPresenterTests`、`ProduceFlowTests`（追記）。

### タスク 4: Play モードの通し

Editor の Play モードで、ローカルのサーバー（初期付与済み）に対して、5 章の手動の手順を行う。確かめられなかった項目と、見つけた不具合は、直すか（計画の範囲内なら）、直さずに報告する。

### タスク 5: Windows の通し（完了条件 6）と記録

1. Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」。Editor を閉じてから）で、起動 → ガチャ → 強化 → 編成 → **最短シナリオを最後まで** → 結果、を手で通す。続けて**初級を 1 回**通す（初級は時間がかかるので、途中で「≡」から放棄してもよいが、完了条件 6 は最後まで通すこと）。結果を `unity7-evaluation.md` の 2 章に足す。通らなかった所は、そのまま事実を書く
2. `screen-design.md` の「実装メモ」に、6 本分のズレをまとめて書く（なければ「なし」）。`game-spec.md` の順 7 の行を `done` にして、6 本の計画へのリンクを書く（完了条件 6 を確かめた日付も）。完了条件 7（Android）は別の項目のままと書く
3. この計画を `done` にする（ほかの 5 本が `done` のときだけ）

## 5. テスト

| ファイル | テスト | 内容 |
|---|---|---|
| `ExamRankingTests.cs` | `Build_SortedDescending_PlayerFirstOnTies` | |
| | `Build_RankIsPosition` | 1 から並び順 |
| | `CurrentRank_CountsStrictlyGreaterNpcs` | `ProduceFormulas.ExamRank` と一致 |
| `DisplayTextLessonTests.cs` | `LessonKind_AllValues` / `Status_AllValues` / `Rank_PlusSuffix` / `ExamOutcome_AllValues` / `Thousands_Groups` | |
| `LessonPresenterTests.cs` | `Bind_Lesson_ShowsHeaderProgressStatusAndHand` | 偽の `ILessonView`。ヘッダー "特別レッスン（律動）"・"ターン 2 / 6"、進行・状態の行、手札の `enabled` が `UsableHand` どおり |
| | `Bind_Exam_ShowsScoreRankAndTurnInfo` | "このターン: 歌唱（ボーナス 285%）"・"次: 律動→表現" |
| | `Bind_UsableHandEmpty_AllEnabled` | |
| | `Bind_EnhancedByLessonEnhanced_ShowsPlus` | `LessonEnhanced` に含む ID は "+" が付く |
| | `CardClicked_Confirmed_UsesCard` | `ExecuteLessonActionAsync(UseCard, instanceId)` |
| | `CardClicked_Disabled_DoesNothing` | |
| | `CardClicked_Declined_DoesNothing` | |
| | `SkipClicked_NoConfirm_CallsSkip` | `FakeDialogService.Messages` に確認が無い |
| | `Step_StillInLesson_RefreshesInPlace` | `FakeProduceFlow.HandleStepCalls` が 0 |
| | `Step_PhaseChanged_HandlesStep` | 応答が `WeekSelect` なら `HandleStepAsync` が 1 回 |
| | `Step_ExamResultPresent_HandlesStep` | 応答が `Exam` でも `ExamResult != null` なら `HandleStepAsync` |
| | `Step_Rejected_ShowsInfoAndRefreshes` | |
| | `MenuClicked_ShowsMenu` | |
| `ExamResultPresenterTests.cs` | `Bind_RetryAvailable_ShowsBothButtons` | `Phase == ExamResult`。合格なら "次へ"、不合格なら "プロデュース終了" |
| | `Bind_NoRetry_ShowsNextOnly` | `Phase != ExamResult` で `retryLabel == null` |
| | `Bind_RankingMarksPlayer` | |
| | `RetryClicked_Confirmed_CallsDecideTrue` / `RetryClicked_Declined_DoesNothing` | |
| | `NextClicked_RetryAvailablePassed_CallsDecideFalseWithoutConfirm` | |
| | `NextClicked_RetryAvailableFailed_ConfirmsThenDecideFalse` | |
| | `NextClicked_NoRetry_MarksShownAndAdvances` | `session.ExamResultShown == true`、`FakeProduceFlow.AdvanceCalls == 1` |
| `ProduceResultPresenterTests.cs` | `Bind_Completed_ShowsRankAndRewards` | |
| | `Bind_NoResult_ShowsPlaceholder` | |
| | `HomeClicked_ClearsSessionAndExits` | |
| `ProduceFlowTests.cs`（追記） | `Enter_Lesson_PushesLessonPage` / `Advance_Exam_ReplacesLessonPage` / `Advance_Finished_ReplacesProduceResultPage` / `Advance_ExamResultPresentNotShown_ShowsExamResultFirst` / `Advance_ExamResultShown_ShowsPhaseScreen` / `Advance_PhaseExamResultWithoutResult_ShowsInfoAndExits` | `FakeScreenNavigator.Calls` |

（`FakeProduceFlow` は 5 本目で作ったものに、`HandleStepCalls`・`AdvanceCalls` のカウンターを足してよい。）

Play モード・手動（Editor。タスク 4。サーバー起動済み・初期付与済み）:
- 最短シナリオ（3 週）: 編成で最短シナリオを選ぶ → 開始 → 週 1 のレッスン → 手札が出る。使えないカードが灰色。カードをタップ → 確認 → 使用すると、獲得・体力・手札が更新される。スキップできる。レッスンが終わると（クリアの報酬があれば）スキルカードの獲得 → 週 2（仕上げ）→ 週 3 の最終試験 → スコアと順位が出る → 試験結果 → 次へ（または再挑戦）→ プロデュース結果 → ホームへ。ホームのガチャ通貨・プレイヤーLv が増えている
- 中間試験があるシナリオ（初級）: 中間試験の結果で「再挑戦」が出る。再挑戦で最初からやり直す。不合格で「プロデュース終了」を選ぶと結果に進む
- レッスンの途中でアプリを終了 → 再起動 → ホームの「再開」でレッスンに戻る（同じ手札・ターン）
- 「≡」→ 放棄 → 確認 → ホーム

## 6. 未決事項

なし
