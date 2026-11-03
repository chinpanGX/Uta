---
title: 実装計画: client/ の画面 6/6 レッスン・試験・試験結果・プロデュース結果
status: draft
updated: 2026-11-03
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 6/6 レッスン・試験・試験結果・プロデュース結果

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 6 本目（最後）。プロデュースの残りの局面の画面を作る: レッスン・試験（`ProducePhase.Lesson`・`Exam`）、山札・捨札・除外の一覧（モーダル。PRD-08）、試験結果（`ExamResult`、および試験が終わった応答の `ExamResult`）、プロデュース結果（`Finished`）。これで、起動 → ガチャ → 強化 → 編成 → プロデュースを最後まで → 結果、が画面だけで通る（完了条件 6）。

- 仕様: レッスン・カードは [lesson-card.md](../wiki/systems/lesson-card.md)（8章 ステータス、14章 カード）、プロデュースは [produce.md](../wiki/systems/produce.md) 4〜6章・5.1.1（試験の結果と局面）・9章（実行要件。PRD-03〜08）・10.1（局面の遷移）。DTO は [produce-server.md](../wiki/architecture/server/produce-server.md) 6章と `shared/Uta.Shared.Contracts/Produce/`。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 3・7・8。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 6・7章
- 書き方の正: **Presenter・View・遷移・テストの偽は [1 本目](2026-11-03-client-screens-foundation.md) 3.3 に従う**（`IPresenter`・2 本のコンストラクター・`[AssetAddress]`・`InitializeAsync`・`Dispose` での購読の解除・実行中フラグ・`CallLog`・`FakeScreenNavigator`・`FakeDialogService`）。この計画では繰り返さない
- **前提**: 計画 0（[スキルカードの名前と説明文](2026-11-03-masterdata-card-text.md)）と 1〜5 本目が done。使うもの: `IGameMaster.GetCardText`（1 本目 3.4）、`DisplayText.Rarity`・`Param`（3 本目）、`ProduceSession`・`ProduceActions`・`ProduceFlow`・`ProduceMenu`・応答の写しの型・`FakeProducePort`（4 本目）、`CardSummaryBuilder`・`ProduceChange`・`DisplayText.CardType`・`Costs`・`Change`・`FakeProduceFlow`（5 本目）、`LessonDto.UsableHand`（4 本目のサーバーの直し）
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（クライアントは判定と予測まで。MVP の使用可否はサーバーの `UsableHand`: screen-design.md 6章 [決定 2026-11-03]）、[0009](../wiki/decisions/0009-client-stack.md)

### 1.1 作らないもの（理由）

| 作らないもの | 理由 |
|---|---|
| ターン終了時の効果を 1 件ずつ見せる表示 | MVP から外した [決定。2026-11-03。ユーザーの判断。[screen-wireframes.md](../wiki/overview/screen-wireframes.md) 3]。サーバーの応答に効果の記録が無い。代わりに操作の前後の差を出す（1.2） |
| カードの効果の予測値 | MVP は出さない（[screen-design.md](../wiki/architecture/screen-design.md) 6・7.3） |
| ドリンク・育成アイテム | PRD-10（次点）。MVP 外 |
| ステータスの説明（タップで説明） | 叩き台の要素で PRD に要件が無い。ステータスは名前と値だけ出す |
| 試験結果の「合格！ 報酬」の欄（順位報酬・育成pt・育成アイテム） | `ExamResultDto` に報酬の項目が無い。育成pt は MVP で扱わない（game-spec.md の MVP の表）。中間試験の合格時の育成アイテムは MVP では付けない（produce.md 5.1.1）。合否の文字だけ出す |
| 最終評価の評価値の内訳（パラメータ × 倍率・順位分・スコア分） | `ProduceResultResponse` に内訳の項目が無い。合計だけ出す |
| キャラ表示、特別レッスンの見出し以外の演出 | 見た目は仮（screen-design.md 7.1） |
| 持続効果（`Persistents`）の表示 | 種類（`PersistentTrigger`）と残りしか応答に無く、何が起きるかを言えない。カードの説明文で読む |

### 1.2 決めたこと（この計画で）

- カードをタップ → 確認ダイアログ（名前・消費・説明文。ボタン「使用」「やめる」）→ 使用。叩き台の「詳細 → 使用」と同じ結果。使えないカード（`enabled == false`）のボタンは押せない
- スキップは確認なし（叩き台の決定。2026-11-01）
- レッスン・試験のあいだの画面の更新は、同じページの中で行う（ページを作り直さない）。レッスンから試験に続けて入るとき（仕上げレッスンの次の週が試験）も同じページで描き直す
- **操作の前後の差**: カードの使用・スキップのあと、その操作で変わった `獲得`（試験は `スコア`）・`体力`・`元気` を、ページの「直前の結果」の行に小さく出す（3.4.1 の `SetLastResult`）。**レッスン（試験でない）が終わったら**、情報ダイアログ「レッスン終了」で、パラメータと体力の差（5 本目の `ProduceChange`・`DisplayText.Change`）を出してから次の画面に進む。試験の終わりは試験結果の画面が出るので、ダイアログを出さない
- **山札・捨札・除外の一覧（PRD-08）**: レッスンのページに専用のボタン（ラベルは `"山札 8　捨札 5　除外 2"`）を置き、押すとモーダル `DeckListModal` を出す。中身は手札・山札・捨札・除外の 4 つの区分で、カードごとに名前・レア度・種類・消費・説明文。山札は並びを返さない（produce-server.md 6章）ので、どの区分もインスタンス ID の昇順（手札だけは手札の並び）。「≡」（4 本目の `ProduceMenu`）には足さない（`ProduceMenu` は週の画面と共有で、4 本目の持ち物のため）
- ターンの表示は叩き台どおり残りのターン（`"残り 4 / 6"`）
- 試験結果の順位表のライバルの名前は仮に「ライバル 1・2…」（上から順に振る）。自分の行は `"あなた"` で、末尾に `" ◀"` を付け、色を `UiTheme.Accent` にする（叩き台の `◀`）
- **局面の切り替えはすべて `ProduceFlow` に任せる**: この計画の Presenter は自分のページを `PopPageAsync` しない（二重に閉じないため。閉じるのは `ProduceFlow`）。`ProduceFlow` を呼んだあと（画面を離れるとき）は、そのページの以降のボタンを全部無視する（3.4 の `leaving`）

### 1.3 完了条件

- 順 7 は完了条件 **6 だけ**を受け持つ。この計画のタスク 5 で、Windows の IL2CPP のビルドで 6 を手で確かめる
- 完了条件 7（Android の実機・IL2CPP との計算の一致）は順 7 の範囲外（game-spec.md の別の項目。表の受け持ちは進行役が直す）。この計画では確かめない

## 2. 触るファイル

パスは、`client/` のものは `client/uta/Assets/` からの相対、wiki はリポジトリのルートからの相対。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Produce/ExamRanking.cs` | 試験の順位表と、試験中の見込みの順位 |
| 変更 | `Scripts/Presentation/Common/DisplayText.cs` | レッスンの種類・ステータス・ランク・試験の結果・試験の種類・3 桁区切り |
| 作成 | `Scripts/Presentation/Produce/LessonCardText.cs` | レッスン中のカード 1 枚の表示用の文字（手札と一覧が共有） |
| 作成 | `Scripts/Presentation/Produce/ILessonView.cs`・`LessonPage.cs`・`LessonPresenter.cs`・`HandCardViewData.cs` | レッスン・試験 |
| 作成 | `Scripts/Presentation/Produce/IDeckListView.cs`・`DeckListModal.cs`・`DeckListPresenter.cs`・`DeckSectionViewData.cs` | 山札・捨札・除外の一覧（モーダル） |
| 作成 | `Scripts/Presentation/Produce/IExamResultView.cs`・`ExamResultPage.cs`・`ExamResultPresenter.cs`・`ExamRankingRowViewData.cs` | 試験結果 |
| 作成 | `Scripts/Presentation/Produce/IProduceResultView.cs`・`ProduceResultPage.cs`・`ProduceResultPresenter.cs` | プロデュース結果 |
| 変更 | `Scripts/Presentation/Produce/ProduceFlow.cs` | 局面 `Lesson`・`Exam`・`ExamResult`・`Finished` と、試験結果の先出し（3.5） |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | `RegisterScreens` に雛形の登録を 4 行 |
| 作成 | `Tests/EditMode/Support/LessonTestData.cs` | テスト用の状態とマスタ（5章） |
| 作成 | `Tests/EditMode/ExamRankingTests.cs`・`DisplayTextLessonTests.cs`・`LessonCardTextTests.cs`・`LessonPresenterTests.cs`・`DeckListPresenterTests.cs`・`ExamResultPresenterTests.cs`・`ProduceResultPresenterTests.cs` | テスト（5章） |
| 変更 | `Tests/EditMode/ProduceFlowTests.cs` | 追記（5章） |
| 変更 | `uta_docs/wiki/overview/game-spec.md` | 順 7 の行を `done` にする（タスク 5） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 2章の PRD-08 の行、6章、実装メモ（タスク 5） |
| 変更 | `uta_docs/wiki/architecture/server/produce-server.md` | 6章の `ExamResultDto.Attempt`・`LessonDto.ExamTurns` の注記（タスク 5） |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | 2章（タスク 5） |

## 3. 型とシグネチャ

C# は C# 9 の範囲（コレクション式 `[...]` は使わない。配列は `new[] { ... }`）。BOM 付き UTF-8、LF、Allman、`var`。名前空間はフォルダどおり（`Application/Produce` → `Uta.Client.Application.Produce`、`Presentation/Produce` → `Uta.Client.Presentation.Produce`、`Presentation/Common` → `Uta.Client.Presentation.Common`、テストは `Uta.Client.Infrastructure.Tests`）。

**4 本目の型の名前**: この計画は、応答の写しを `ProduceStateView`・`LessonView`・`ExamTurnView`・`DeckCardView`・`ExamResultView`・`ProduceResultView`・`ProduceEndingKind`、操作を `LessonActionType`・`ProduceStepStatus` と書く。4 本目（formation）が `Uta.Shared.Domain.Produce.ClassOptionView` との衝突を避けるために名前を変えた（`Produce` の接頭辞など）ときは、4 本目の名前に読み替える（プロパティの名前と形は同じ）。`ProduceSession` に `Result` は無い（結果は `session.Current.Result`）。

### 3.1 `Uta.Client.Application.Produce.ExamRanking`

```csharp
public sealed class ExamRankingRow
{
    public ExamRankingRow(int rank, bool isPlayer, int score);
    public int Rank { get; } public bool IsPlayer { get; } public int Score { get; }
}
public static class ExamRanking
{
    /// <summary>
    /// 自分（playerScore）と NPC（npcScores の並びのまま）の行を作り、スコアの降順に並べる。同点は自分が先、NPC どうしの同点は npcScores の添字の小さいほうが先（安定な並べ替え）。
    /// 順位は 1 から並び順に振る（同点でも番号は進む）。npcScores が null なら空として扱う。
    /// 自分の行の Rank は ProduceFormulas.ExamRank(playerScore, npcScores) と常に等しい（同点は自分が先なので）。
    /// </summary>
    public static IReadOnlyList<ExamRankingRow> Build(int playerScore, IReadOnlyList<int> npcScores);

    /// <summary>試験中の見込みの順位。Uta.Shared.Domain.Produce.ProduceFormulas.ExamRank(playerScore, npcScores ?? 空) を返す。</summary>
    public static int CurrentRank(int playerScore, IReadOnlyList<int> npcScores);
}
```

### 3.2 `DisplayText` の追加（`Presentation/Common/DisplayText.cs`）

```csharp
public static string LessonKind(LessonKind kind);      // Normal → "レッスン"、Special → "特別レッスン"、Finishing → "仕上げレッスン"、MidExam → "中間試験"、FinalExam → "最終試験"、Nothing → "-"、それ以外 → kind.ToString()
public static string Status(StatusKind kind);          // 下の表。Nothing → "-"、表に無い値 → kind.ToString()
public static string Rank(ProduceRank rank);           // F → "F"、E → "E"、D → "D"、C → "C"、CPlus → "C+"、B → "B"、BPlus → "B+"、A → "A"、APlus → "A+"、S → "S"、SPlus → "S+"、SS → "SS"、SSPlus → "SS+"、Nothing → "-"、それ以外 → rank.ToString()
public static string ExamOutcome(ExamOutcome outcome); // MidFailed → "中間試験で不合格"、FinalFailed → "最終試験で不合格"、FinalPassed → "最終試験に合格"、Nothing とそれ以外 → ""
public static string ExamKind(ExamKind kind);          // Mid → "中間試験"、Final → "最終試験"、Nothing とそれ以外 → "試験"
public static string Thousands(int value);             // value.ToString("N0", CultureInfo.InvariantCulture)（1234 → "1,234"、-1234 → "-1,234"）
public static string Signed(int value);                // 0 以上は "+" + value、負は value.ToString()（"-3"）。3 桁区切りはしない
```

`Status` の表示名は [lesson-card.md](../wiki/systems/lesson-card.md) 8章・[glossary.md](../wiki/glossary.md) の仮称（仮称の置き換えは glossary で決める。この計画では新しい名前を作らない）:

| `StatusKind` | 表示名 | `StatusKind` | 表示名 |
|---|---|---|---|
| `GoodCondition` | 好調 | `Motivation` | やる気 |
| `Excellent` | 絶好調 | `CostHalf` | 消費体力減少 |
| `Focus` | 集中 | `CostDouble` | 消費体力増加 |
| `FocusBoost` | 集中強化 | `CostReduce` | 消費体力削減 |
| `Impression` | 好印象 | `ShieldGainBlock` | 元気増加無効 |
| `ImpressionBoost` | 好印象強化 | `DebuffGuard` | 低下状態無効 |

（`LessonKind`・`StatusKind` は `Uta.Shared.Domain.Lesson`、`ProduceRank`・`ExamOutcome`・`ExamKind` は `Uta.Shared.Domain.Produce`。`client/uta/Assets/Scripts/Infrastructure/MasterData/Generated` に同名の enum があるので、`DisplayText.cs` は Domain の名前空間を `using` し、マスタの名前空間は `using` しない。）

### 3.3 `LessonCardText`（`Presentation/Produce/LessonCardText.cs`）

```csharp
public sealed class LessonCardText
{
    public LessonCardText(int instanceId, string title, string costs, string description);
    public int InstanceId { get; }
    public string Title { get; }         // "まっすぐ一声+ [N] アクティブ"
    public string Costs { get; }         // "消費 3"（5 本目の DisplayText.Costs）
    public string Description { get; }   // "パラメータ+14"

    /// <summary>
    /// 1. deckCard = state.Deck のうち InstanceId == instanceId のもの。無ければ new LessonCardText(instanceId, "カード？", "", "") を返す
    ///    （MVP はレッスン中にカードを作る効果（CreateCard）を入れないので起きない。起きても止めない）
    /// 2. enhanced = deckCard.Enhanced || (state.Lesson != null && state.Lesson.LessonEnhanced に instanceId が含まれる)
    /// 3. summary = CardSummaryBuilder.Build(catalog, deckCard.CardId, enhanced)、text = master.GetCardText(deckCard.CardId)
    /// 4. title = text.Name + (enhanced ? "+" : "") + " [" + DisplayText.Rarity(summary.Rarity) + "] " + DisplayText.CardType(summary.Type)
    ///    costs = DisplayText.Costs(summary)、description = enhanced ? text.DescriptionEnhanced : text.Description
    /// </summary>
    public static LessonCardText Create(ProduceStateView state, ICardCatalog catalog, IGameMaster master, int instanceId);
}
```

### 3.4 `Uta.Client.Presentation.Produce`

```csharp
public sealed class HandCardViewData
{
    public HandCardViewData(int instanceId, string title, string costs, string description, bool enabled);
    public int InstanceId { get; } public string Title { get; } public string Costs { get; } public string Description { get; } public bool Enabled { get; }
}

public interface ILessonView
{
    void SetHeader(string title, string turnText);               // "特別レッスン（律動）"、"残り 4 / 6"
    void SetProgress(IReadOnlyList<string> lines);               // 3.4.1
    void SetStatus(IReadOnlyList<string> lines);                 // 3.4.1
    void SetLastResult(string text);                             // 直前の操作の差。"" なら空の行
    void SetPileCounts(int drawPile, int discard, int exiled);   // 一覧のボタンのラベル
    void SetHand(IReadOnlyList<HandCardViewData> cards);
    event Action<int> CardClicked;                               // instanceId
    event Action SkipClicked;
    event Action MenuClicked;
    event Action PilesClicked;
}
public sealed class LessonPage : PageBase, ILessonView { ... }

[AssetAddress(nameof(LessonPage))]
public sealed class LessonPresenter : IPresenter
{
    [Inject]
    public LessonPresenter(LessonPage view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions,
        ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);   // : this((ILessonView)view, ...)
    public LessonPresenter(ILessonView view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions,
        ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);

    public UniTask InitializeAsync();                        // 購読（4 つのイベント）→ Refresh() → SetLastResult("")
    public void Dispose();                                   // 4 つのイベントの購読を解く
    public UniTask UseCardAsync(int instanceId);             // CardClicked → UseCardAsync(id).Forget()
    public UniTask SkipAsync();                              // SkipClicked → SkipAsync().Forget()
    public UniTask OpenMenuAsync();                          // MenuClicked → OpenMenuAsync().Forget()
    public UniTask OpenPilesAsync();                         // PilesClicked → OpenPilesAsync().Forget()
}

public sealed class DeckSectionViewData
{
    public DeckSectionViewData(string header, IReadOnlyList<string> rows);
    public string Header { get; } public IReadOnlyList<string> Rows { get; }
}
public interface IDeckListView
{
    void SetSections(IReadOnlyList<DeckSectionViewData> sections);
    event Action CloseClicked;
}
public sealed class DeckListModal : ModalBase, IDeckListView { ... }

[AssetAddress(nameof(DeckListModal))]
public sealed class DeckListPresenter : IPresenter
{
    [Inject]
    public DeckListPresenter(DeckListModal view, ProduceSession session, ICardCatalog catalog, IGameMaster master, IScreenNavigator navigator);
    public DeckListPresenter(IDeckListView view, ProduceSession session, ICardCatalog catalog, IGameMaster master, IScreenNavigator navigator);
    public UniTask InitializeAsync();
    public void Dispose();
    public UniTask CloseAsync();                             // CloseClicked → CloseAsync().Forget()
}

public sealed class ExamRankingRowViewData
{
    public ExamRankingRowViewData(string text, bool isPlayer);
    public string Text { get; } public bool IsPlayer { get; }
}
public interface IExamResultView
{
    void SetTitle(string text);                                  // "中間試験 結果（1 回目）"
    void SetRanking(IReadOnlyList<ExamRankingRowViewData> rows);
    void SetOutcome(string text);                                // "合格！" / "不合格"
    void SetButtons(string retryLabel, string nextLabel);        // retryLabel が null なら再挑戦のボタンを隠す
    event Action RetryClicked;
    event Action NextClicked;
}
public sealed class ExamResultPage : PageBase, IExamResultView { ... }

[AssetAddress(nameof(ExamResultPage))]
public sealed class ExamResultPresenter : IPresenter
{
    [Inject]
    public ExamResultPresenter(ExamResultPage view, ProduceSession session, ProduceActions actions, ProduceFlow flow, DialogService dialogs);
    public ExamResultPresenter(IExamResultView view, ProduceSession session, ProduceActions actions, ProduceFlow flow, DialogService dialogs);
    public UniTask InitializeAsync();
    public void Dispose();
    public UniTask RetryAsync();                             // RetryClicked → RetryAsync().Forget()
    public UniTask NextAsync();                              // NextClicked → NextAsync().Forget()
}

public interface IProduceResultView
{
    void SetRank(string text);                                   // "ランク A+"
    void SetLines(IReadOnlyList<string> lines);
    event Action HomeClicked;
}
public sealed class ProduceResultPage : PageBase, IProduceResultView { ... }

[AssetAddress(nameof(ProduceResultPage))]
public sealed class ProduceResultPresenter : IPresenter
{
    [Inject]
    public ProduceResultPresenter(ProduceResultPage view, ProduceSession session, ProduceFlow flow);
    public ProduceResultPresenter(IProduceResultView view, ProduceSession session, ProduceFlow flow);
    public UniTask InitializeAsync();
    public void Dispose();
    public UniTask GoHomeAsync();                            // HomeClicked → GoHomeAsync().Forget()
}
```

**4 つの Presenter に共通の決まり**（1 本目 3.3.2 の 5・8 に足す）:
- 実行中フラグ `running`（1 本目 3.3.2 の 5。`finally` で `false` に戻す）に加えて、**`leaving`**（`bool`。初期値 `false`）を持つ。`ProduceFlow` のメソッド（`HandleStepAsync`・`AdvanceAsync`・`ExitToHomeAsync`）を呼ぶ直前に `leaving = true` にし、以後 `false` に戻さない。ボタンの処理（`XxxAsync`）の先頭は `if (running || leaving) return;`
- `ProduceFlow` を呼んだあとは、View に触らない（`Refresh()` などを呼ばない。ページは `ProduceFlow` が閉じている）
- 自分のページを `PopPageAsync` しない（閉じるのは `ProduceFlow` だけ）。モーダル（`DeckListPresenter`）は自分を `PopModalAsync(this, playAnimation: false)` で閉じる
- 例外（`catch (Exception e)`）: `leaving = false` に戻したうえで `await dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`（`ProduceResultPresenter` は `dialogs` を持たないので `UnityEngine.Debug.LogException(e)` だけ）。`ApiCore` が出すダイアログ（1 本目 3.3.5）とは別に、ユースケースが投げた例外だけをここで出す

#### 3.4.1 `LessonPresenter` の動き

`Refresh()`（`InitializeAsync` と、同じページのまま更新するときに呼ぶ。`private`）:
- `state = session.Current`、`lesson = state?.Lesson`。どちらかが `null` なら何もしない
- `SetHeader(title, turnText)`:
  - `title` = `DisplayText.LessonKind(lesson.Kind)`。`!lesson.IsExam` のときは末尾に `"（" + DisplayText.Param(lesson.TargetParam) + "）"`（例 `"特別レッスン（律動）"`。試験は `"中間試験"`）
  - `remaining` = `lesson.Turn <= 0 ? lesson.MaxTurns : Math.Max(0, lesson.MaxTurns - lesson.Turn + 1)`（今のターンを含む残り。試験の `MaxTurns` は審査ターンの数）。`turnText` = `$"残り {remaining} / {lesson.MaxTurns}"`
- `SetProgress(lines)`。`lines` は次の順で足す:
  - レッスン（`!lesson.IsExam`）: 1 行 `$"獲得 {lesson.ParamGained}　クリア目標 {lesson.ClearTarget}　パーフェクト {lesson.PerfectTarget}"`
  - 試験（`lesson.IsExam`）。`turns = lesson.ExamTurns`、`t = lesson.Turn`（1 始まり。開始前は 0）、`n = turns.Count`:
    1. `$"スコア {DisplayText.Thousands(lesson.ParamGained)}　順位 {ExamRanking.CurrentRank(lesson.ParamGained, lesson.NpcScores)} 位"`
    2. `1 <= t && t <= n` のとき: `cur = turns[t - 1]`（**今のターンは添字 `t - 1`**。サーバーの `EffectExecutor` が `turns[Math.Max(0, state.Turn - 1)]` を使うのと同じ）で `$"このターン: {DisplayText.Param(cur.Param)}（ボーナス {cur.ScoreBonusPercent}%）"`
    3. `1 <= t && t < n` のとき: 添字 `t` から `n - 1` まで（今のターンの次から最後まで）の `DisplayText.Param(turns[k].Param)` を `"→"` でつないで `"次: " + それ`（例 `t = 1`、`turns` が 歌唱・律動・表現 なら `"次: 律動→表現"`）
    4. `t < 1` または `t > n` のときは 2・3 を足さない
- `SetStatus(lines)`。次の順で足す:
  1. `$"体力 {state.Hp} / {state.MaxHp}　元気 {lesson.Shield}"`
  2. ステータス: 添字 `i` = 1 から `lesson.StatusValues.Count - 1` まで（添字 0 は `Nothing`）、値 `v = lesson.StatusValues[i]` が 0 でないものを `kind = (StatusKind)i` として `DisplayText.Status(kind) + " " + v`（`kind` が `FocusBoost`・`ImpressionBoost` のときだけ末尾に `"%"`）にし、`"　"` でつないで 1 行。1 つも無ければ行を足さない
  3. `lesson.ParamBonuses` の各要素を `$"パラメータ上昇量増加 {b.Percent}%（残り {b.RemainingTurns} ターン）"` で 1 行ずつ（0 件なら足さない）
  4. `$"スキルカード使用 残り {lesson.UsesLeft}"`
- `SetPileCounts(lesson.DrawPile.Count, lesson.Discard.Count, lesson.Exiled.Count)`
- `SetHand(cards)`: `lesson.Hand` の添字 `i` ごとに `id = lesson.Hand[i]`、`text = LessonCardText.Create(state, catalog, master, id)`、
  `enabled = lesson.UsableHand.Count == 0 || i >= lesson.UsableHand.Count || lesson.UsableHand[i]`
  （**`UsableHand` が空なら全部有効、`Hand` より短いときは範囲外を有効**。`UsableHand` が欠けていても手が止まらないようにする。使えないカードを送ってもサーバーが `PRODUCE_INVALID_ARGUMENT`（`Rejected`）で止める）→ `new HandCardViewData(id, text.Title, text.Costs, text.Description, enabled)`
- 作った `HandCardViewData` のリストをフィールド `hand` に入れる（`UseCardAsync` が使う）

`UseCardAsync(int instanceId)`（`running`・`leaving` の決まりの中）:
- `card` = `hand` のうち `InstanceId == instanceId` のもの。無い・`Enabled == false` なら何もしない
- `await dialogs.ConfirmAsync($"{card.Title}\n{card.Costs}\n{card.Description}\n\n使用しますか？", "使用", "やめる")` が `Positive` のときだけ `await StepAsync(LessonActionType.UseCard, instanceId)`

`SkipAsync()`（同じ決まりの中）: 確認なしで `await StepAsync(LessonActionType.Skip, 0)`

`StepAsync(LessonActionType kind, int instanceId)`（`private`。呼び出し元の `running` の中で動く）:
1. `before = session.Current`、`wasExam = before.Lesson.IsExam`
2. `status = await actions.ExecuteLessonActionAsync(kind, instanceId)`（`drinkSlotIndex` は既定の 0）
3. `after = session.Current`。`stillHere` = `after != null && (after.Phase == ProducePhase.Lesson || after.Phase == ProducePhase.Exam) && after.ExamResult == null && after.Lesson != null`
4. 分岐（上から最初に当たったもの）:
   - `Rejected`: `await dialogs.ShowInfoAsync("そのカードは使えません。")` → `Refresh()`（`SetLastResult` は変えない）
   - `Failed`: 何もしない（共通のダイアログは `ApiCore` が出している）
   - `Succeeded` で `!wasExam` で、`after.Lesson == null` または `after.Lesson.IsExam`（**レッスンが終わった**）: `await dialogs.ShowInfoAsync(LessonEndText(before, after))` のあと、`stillHere` なら（仕上げレッスンから続けて試験に入った）`Refresh()`・`SetLastResult("")`、そうでなければ `leaving = true`・`await flow.HandleStepAsync(status)`
   - `Succeeded` で `stillHere`: `Refresh()` → `SetLastResult(DiffText(before, after))`
   - `Recovered` で `stillHere`: `Refresh()` → `SetLastResult("")`（取り直した状態は、直前の操作の結果とは限らないため差を出さない）
   - それ以外（局面が変わった・試験が終わった・`SessionEnded`）: `leaving = true` → `await flow.HandleStepAsync(status)`

`DiffText(before, after)`（`private static string`。`before.Lesson`・`after.Lesson` はどちらも `null` でない）: 次の 0 でないものを `"　"` でつなぐ。全部 0 なら `"変化なし"`。
- `dGain = after.Lesson.ParamGained - before.Lesson.ParamGained` → `(after.Lesson.IsExam ? "スコア " : "獲得 ") + DisplayText.Signed(dGain)`
- `dHp = after.Hp - before.Hp` → `"体力 " + DisplayText.Signed(dHp)`
- `dShield = after.Lesson.Shield - before.Lesson.Shield` → `"元気 " + DisplayText.Signed(dShield)`

`LessonEndText(before, after)`（`private static string`）: `lines = DisplayText.Change(ProduceChange.Create(before, after))`（5 本目。歌唱・律動・表現・体力の差のうち 0 でないもの）。`lines` が空なら `"レッスン終了"`、そうでなければ `"レッスン終了\n" + string.Join("\n", lines)`。

`OpenMenuAsync()`（同じ決まりの中）: `await menu.ShowAsync();`（ホームへ戻る・放棄は `ProduceMenu` が `ProduceFlow` を呼ぶ。ここでは `leaving` を立てない。メニューが「閉じる」で終わったときに操作を続けられるようにするため）

`OpenPilesAsync()`（同じ決まりの中）: `await navigator.PushModalAsync<DeckListPresenter>(playAnimation: false);`（閉じるのを待たない。モーダルが自分で閉じる）

#### 3.4.2 `DeckListPresenter` の動き

`InitializeAsync()`: `view.CloseClicked += OnCloseClicked;` → `state = session.Current`、`lesson = state?.Lesson`:
- `lesson == null`: `SetSections(new[] { new DeckSectionViewData("レッスン中ではありません", new string[0]) })`
- それ以外: 次の 4 つの区分を、この順で `SetSections`:

  | 区分の名前 | カード（インスタンス ID）の並び |
  |---|---|
  | 手札 | `lesson.Hand` の並びのまま |
  | 山札 | `lesson.DrawPile` をインスタンス ID の昇順（`OrderBy(x => x)`。サーバーも昇順で返すが、並べ直しても同じ） |
  | 捨札 | `lesson.Discard` を同じく昇順 |
  | 除外 | `lesson.Exiled` を同じく昇順 |

  `header` = `$"{区分の名前} {枚数} 枚"`。`rows` = 各 ID の `text = LessonCardText.Create(state, catalog, master, id)` を `$"{text.Title}　{text.Costs}\n{text.Description}"`。0 枚の区分は `rows` = `new[] { "（なし）" }`

`CloseAsync()`（`running` の中。`leaving` は使わない）: `closed`（`bool`）が true なら何もしない。`closed = true` → `await navigator.PopModalAsync(this, playAnimation: false);`

`Dispose()`: `view.CloseClicked -= OnCloseClicked;`

#### 3.4.3 `ExamResultPresenter` の動き

フィールド: `retryAvailable`（`bool`）、`passed`（`bool`）。

`InitializeAsync()`: `view.RetryClicked += …; view.NextClicked += …;` → `state = session.Current`、`result = state?.ExamResult`:
- **`result == null`**（通常は来ない。`ProduceFlow` は `ExamResult != null` のときだけこのページを出す）: **`flow` を呼ばない**。`SetTitle("試験結果")`、`SetRanking(空のリスト)`、`SetOutcome("結果がありません。")`、`SetButtons(null, "次へ")`、`retryAvailable = false`。`NextAsync` が下の「結果を見せるだけ」の経路で進める
- それ以外:
  - `SetTitle($"{DisplayText.ExamKind(result.Kind)} 結果（{result.Attempt + 1} 回目）")`（**`Attempt` は 0 始まり**: produce.md 5.1.1、glossary「挑戦回数」、`ProduceEngine` が `state.Attempt` をそのまま入れる。初回は「1 回目」）
  - `SetRanking`: `ExamRanking.Build(result.Score, result.NpcScores)` の各行を、NPC の行に上から 1, 2, … と番号 `k` を振って `new ExamRankingRowViewData(text, row.IsPlayer)`。`text` = 自分の行は `$"{row.Rank}位　あなた　{DisplayText.Thousands(row.Score)} ◀"`、NPC の行は `$"{row.Rank}位　ライバル {k}　{DisplayText.Thousands(row.Score)}"`
  - `passed = result.Passed`。`SetOutcome(passed ? "合格！" : "不合格")`
  - `retryAvailable = state.Phase == ProducePhase.ExamResult`（再挑戦が残り、入力を待つ局面。produce.md 5.1.1）
  - `retryAvailable` なら `SetButtons($"再挑戦（残り {state.ExamRetryLeft}）", passed ? "次へ" : "プロデュース終了")`、そうでなければ `SetButtons(null, "次へ")`

`RetryAsync()`（`running`・`leaving` の決まりの中）: `retryAvailable` が false なら何もしない。`ConfirmAsync("試験を最初からやり直します。体力・デッキは試験の開始前に戻ります。よろしいですか？")` が `Positive` なら `status = await actions.DecideExamRetryAsync(true)`:
- `Rejected`: `await dialogs.ShowInfoAsync("その操作はできません。")`。`Failed`: 何もしない
- それ以外: `leaving = true` → `await flow.HandleStepAsync(status)`（応答は局面 `Exam`・`ExamResult == null`。`ProduceFlow` がレッスンのページを出す）

`NextAsync()`（同じ決まりの中）:
- `retryAvailable` のとき: 合格なら確認なし、不合格なら `ConfirmAsync("プロデュースを終了します。評価と報酬を受け取ります。よろしいですか？")` が `Positive` のときだけ、`status = await actions.DecideExamRetryAsync(false)` → `Rejected`・`Failed` は `RetryAsync` と同じ、それ以外は `leaving = true` → `await flow.HandleStepAsync(status)`
- そうでないとき（結果を見せるだけ。`result == null` の場合も）: `session.ExamResultShown = true;` → `leaving = true` → `await flow.AdvanceAsync();`

#### 3.4.4 `ProduceResultPresenter` の動き

`InitializeAsync()`: `view.HomeClicked += …;` → `state = session.Current`、`result = state?.Result`:
- `result == null`: `SetRank("-")`、`SetLines(new[] { "結果はありません。" })`
- それ以外:
  - `SetRank($"ランク {DisplayText.Rank(result.Rank)}")`
  - `SetLines`: 次の順（括弧の条件に合うものだけ）
    1. `result.Ending == ProduceEndingKind.Completed` なら `"プロデュース完了"`、それ以外は `"プロデュース終了"`（放棄はホームの `ProduceMenu` から直接ホームへ戻るので、この画面には来ない。来ても止めない）
    2. `$"評価値 {DisplayText.Thousands(result.EvaluationValue)}"`
    3. （`result.FinalRank > 0` のとき。0 は最終試験を受けていない: `ProduceFormulas.Evaluate`）`$"最終試験 {result.FinalRank}位　スコア {DisplayText.Thousands(result.FinalScore)}"`
    4. （`DisplayText.ExamOutcome(result.ExamOutcome)` が空でないとき）その文字
    5. `$"歌唱 {DisplayText.Thousands(state.Singing)} / 律動 {DisplayText.Thousands(state.Rhythm)} / 表現 {DisplayText.Thousands(state.Expression)}"`
    6. `"報酬"`
    7. `$"ガチャ通貨 +{DisplayText.Thousands(result.GachaCurrency)}"`
    8. `$"ゴールド +{DisplayText.Thousands(result.Gold)}"`
    9. `$"サポート強化pt +{DisplayText.Thousands(result.SupportPoints)}"`
    10. `$"経験値 +{DisplayText.Thousands(result.Exp)}　（プレイヤーLv {result.PlayerLevel}）"`

`GoHomeAsync()`（`running`・`leaving` の決まりの中）: `leaving = true` → `session.Clear();` → `await flow.ExitToHomeAsync();`
- **この画面には戻れない**: 「＜」ボタンを作らない。`ExitToHomeAsync` がホームの上を全部閉じ、`session` は空になる。報酬はサーバーで付与済み（局面 `Finished` の応答を受けたとき 4 本目の `ProduceActions` が `RefreshPlayerUseCase` で所持品を取り直している）ので、もう一度押せなくても失うものは無い。サーバーのセッションは `Finished` で終わっているので、ホームの「再開」も出ない
- 連打は `running`・`leaving` で 1 回だけにする（`ExitToHomeAsync` を 2 回呼ばない）

### 3.5 `ProduceFlow` に足す局面

`ProduceFlow`（4 本目 3.4）が `session.Current` から次に出す画面を選ぶところ（4 本目の `ShowAsync`。今の局面のページを閉じてから次を Push する共通の処理は 4 本目のまま）に、次を足す。Push は `navigator.PushPageAsync<XxxPresenter>(playAnimation: false)`。

1. 局面の `switch` の前: `state.ExamResult != null && !session.ExamResultShown` なら、`ExamResultPresenter` のページを出して終わる（局面が何であっても先に試験結果を見せる。再挑戦が残らず `ExamResult` の局面を飛ばした応答を含む: screen-design.md 7.4）
2. `switch` に足す `case`:
   - `ProducePhase.Lesson`・`ProducePhase.Exam`: `LessonPresenter` のページ
   - `ProducePhase.ExamResult`: **ページを出さない**。ここに来るのは `ExamResult == null` のとき（1. で処理済みのため）。`await dialogs.ShowInfoAsync("試験の結果を取得できませんでした。ホームから再開してください。")` のあと `await ExitToHomeAsync()`（セッションはサーバーに残り、ホームの「再開」の `GetState` は `LastExam` を返すので、再開すれば試験結果が出る）
   - `ProducePhase.Finished`: `ProduceResultPresenter` のページ

これで 4 本目の「準備中」の分岐は、`Nothing` と未知の値だけに残る。

### 3.6 画面のレイアウト（`BuildUi`。見た目は仮）

文字の大きさは `UiTheme.FontTitle`・`FontBody`・`FontSmall` だけ（1 本目 3.3.6）。View の公開メソッドは先頭で `EnsureBuilt()` を呼ぶ（1 本目 3.3.3）。

**`LessonPage`**: `root = UiFactory.CreateVertical(body, "Root")` の子に、上から:
1. "Header"（`CreateHorizontal`、`SetHeight(UiTheme.ButtonHeight)`）の子に "Title"（`CreateText("", FontBody, MiddleLeft)`）、"Turn"（`CreateText("", FontBody, MiddleRight)`）、"Menu"（`CreateButton("≡")`。`onClick` → `MenuClicked`）
2. "Progress"（`CreateText("", FontBody, UpperLeft)`。`SetProgress` の行を `"\n"` でつなぐ）
3. "Status"（`CreateText("", FontSmall, UpperLeft)`。`SetStatus` の行を `"\n"` でつなぐ）
4. "LastResult"（`CreateText("", FontSmall, UpperLeft, UiTheme.Accent)`。`SetLastResult` の文字）
5. "Piles"（`CreateButton("山札 0　捨札 0　除外 0")`。`SetPileCounts` で `UiFactory.SetLabel(button, $"山札 {drawPile}　捨札 {discard}　除外 {exiled}")`。`onClick` → `PilesClicked`）
6. "Hand"（`CreateScrollContent`。戻り値の Content を `handContent` に持つ）
7. "Skip"（`CreateButton("スキップ")`。`onClick` → `SkipClicked`）

`SetHand(cards)`: `UiFactory.ClearChildren(handContent)` のあと、`cards` の順に `CreateButton(handContent, "Card" + 添字, $"{c.Title}\n{c.Costs}\n{c.Description}", 220f)`。`onClick` → `CardClicked(c.InstanceId)`（ラムダでは `c` をループの中のローカル変数に写してから使う）。`c.Enabled == false` なら `UiFactory.SetInteractable(button, false)`。

**`DeckListModal`**（`ModalBase`。`panel` の子）: "Title"（`CreateText("山札・捨札・除外", FontTitle, MiddleCenter)`）、"List"（`CreateScrollContent`。`UiFactory.SetHeight(ScrollRect の GameObject（Content の親の親）, 1200f)`。Content を `listContent` に持つ）、"Close"（`CreateButton("閉じる")`。`onClick` → `CloseClicked`）。
`SetSections`: `ClearChildren(listContent)` のあと、区分ごとに見出し（`CreateText(header, FontBody, MiddleLeft, UiTheme.Accent)`）、続けて行ごとに `CreateText(row, FontSmall, UpperLeft)`。

**`ExamResultPage`**: `root = CreateVertical(body, "Root")` の子に、"Title"（`CreateText("", FontTitle, MiddleCenter)`）、"Ranking"（`CreateScrollContent`。Content を `rankingContent` に）、"Outcome"（`CreateText("", FontTitle, MiddleCenter)`）、"Retry"（`CreateButton("再挑戦")`。`onClick` → `RetryClicked`）、"Next"（`CreateButton("次へ")`。`onClick` → `NextClicked`）。
- `SetRanking(rows)`: `ClearChildren(rankingContent)` のあと行ごとに `CreateText(row.Text, FontBody, MiddleLeft, row.IsPlayer ? UiTheme.Accent : UiTheme.Text)`
- `SetButtons(retryLabel, nextLabel)`: **ボタンの文字は引数をそのまま使う**。`retryLabel == null` なら "Retry" の GameObject を `SetActive(false)`、そうでなければ `SetActive(true)` と `SetLabel(retry, retryLabel)`。`SetLabel(next, nextLabel)`

**`ProduceResultPage`**: `root = CreateVertical(body, "Root")` の子に、"Rank"（`CreateText("", FontTitle, MiddleCenter)`）、"Lines"（`CreateText("", FontBody, UpperLeft)`。`SetFlexibleHeight(1)`。行を `"\n"` でつなぐ）、"Home"（`CreateButton("ホームへ")`。`onClick` → `HomeClicked`）。戻るボタンは作らない。

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行）でコンパイルエラー 0、そのタスクまでの EditMode のテストが通る状態にする。EditMode のテストの回し方は 1 本目 4章の冒頭（`unity test --mode EditMode`。動かなければ Editor の Test Runner）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。

### タスク 1: 順位表と文字

触る: `Application/Produce/ExamRanking.cs`、`Presentation/Common/DisplayText.cs`、`Tests/EditMode/ExamRankingTests.cs`・`DisplayTextLessonTests.cs`。
手順: 3.1・3.2 のとおり。
境界: `npcScores` が `null`・空（自分の 1 行だけ、順位 1）。同点（自分が先）。`StatusKind` の表に無い値（`(StatusKind)99` → `"99"`）。
確かめ方: `ExamRankingTests`・`DisplayTextLessonTests`。

### タスク 2: レッスン・試験の画面と、山札などの一覧

触る: `Presentation/Produce/LessonCardText.cs`・`HandCardViewData.cs`・`ILessonView.cs`・`LessonPage.cs`・`LessonPresenter.cs`・`DeckSectionViewData.cs`・`IDeckListView.cs`・`DeckListModal.cs`・`DeckListPresenter.cs`、`ProduceFlow.cs`（3.5 の `Lesson`・`Exam` の `case` だけ）、`Bootstrap/UtaBootstrap.cs`、`Tests/EditMode/Support/LessonTestData.cs`、テスト。
手順: 3.3・3.4.1・3.4.2・3.5（`Lesson`・`Exam`）・3.6 のとおり。`UtaBootstrap.RegisterScreens` に `uiRoot.RegisterPage<LessonPage>();`・`uiRoot.RegisterModal<DeckListModal>();` を足す（Presenter は登録しない: 1 本目 3.3.2）。
境界: `UsableHand` が空・`Hand` より短い。`StatusValues` が 13 より短い（ある添字だけ見る）。`ExamTurns` が空（試験でない）・`Turn` が 0・`Turn == ExamTurns.Count`（最後のターン。「次:」を出さない）。`Deck` に無いインスタンス ID（"カード？"）。
確かめ方: `LessonCardTextTests`・`LessonPresenterTests`・`DeckListPresenterTests`、`ProduceFlowTests`（`Lesson`・`Exam` の追記分）。

### タスク 3: 試験結果・プロデュース結果の画面

触る: `Presentation/Produce/ExamRankingRowViewData.cs`・`IExamResultView.cs`・`ExamResultPage.cs`・`ExamResultPresenter.cs`・`IProduceResultView.cs`・`ProduceResultPage.cs`・`ProduceResultPresenter.cs`、`ProduceFlow.cs`（3.5 の 1. と `ExamResult`・`Finished`）、`Bootstrap/UtaBootstrap.cs`、テスト。
手順: 3.4.3・3.4.4・3.5・3.6 のとおり。`RegisterScreens` に `uiRoot.RegisterPage<ExamResultPage>();`・`uiRoot.RegisterPage<ProduceResultPage>();` を足す。
境界: `ExamResult == null` でページが出た（`flow` を呼ばない）。`Result == null`。ボタンの連打（`leaving` で 1 回だけ）。
確かめ方: `ExamResultPresenterTests`・`ProduceResultPresenterTests`、`ProduceFlowTests`（残りの追記分）。

### タスク 4: Play モードの通し

Editor の Play モードで、ローカルのサーバー（CLAUDE.md の「ビルド・テスト・実行」のローカルのサーバー。初期付与済み）に対して、5 章の手動の手順を行う。Play モードの開始・停止とコンソールの読み出しは [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章のコマンド（`unity command editor.playmode start|stop`、`unity command console get --tail N --level error --format json`）を使う。確かめられなかった項目と、見つけた不具合は、この計画の範囲内なら直し、範囲外なら直さずに報告する。

### タスク 5: Windows の通し（完了条件 6）と記録

1. Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」。Editor を閉じてから）を `-batchmode` なしで起動し、起動 → ガチャ → 強化 → 編成 → **最短シナリオを最後まで** → 結果 → ホーム、を手で通す（**完了条件 6**）。続けて初級を 1 回、中間試験の結果の画面まで通す（その先は「≡」→ 放棄でよい）。結果を [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 2章の表に 1 行足す（確かめたもの: 「プロデュースの画面（レッスン・試験・試験結果・結果）を Windows の IL2CPP で最後まで通した」、確かめ方: 「手動」、日付）。通らなかった所は 3.3 にそのまま事実を書く。**完了条件 7（Android）は確かめない**
2. [produce-server.md](../wiki/architecture/server/produce-server.md) 6章: `ExamResultDto` の行の `Attempt` を `Attempt`（挑戦回数。0 始まり）に、`LessonDto` の行の `ExamTurns`（`ExamTurnDto[]`。試験のときだけ）を `ExamTurns`（`ExamTurnDto[]`。試験のときだけ。今のターンは添字 `Turn - 1`）に直す
3. [screen-design.md](../wiki/architecture/screen-design.md):
   - 2章の表の下の「山札・捨札の確認（PRD-08）は、レッスン中に開くモーダルとして持つ案」を「山札・捨札・除外の確認（PRD-08）は、レッスンのページのボタンから開くモーダル（`DeckListModal`）[2026-11-03]」に直す
   - 6章の 1 つ目の項目（「効果エンジンで使用可否の判定と予測値の表示をする」）の末尾に「（MVP は下の項目のとおり、使用可否はサーバーの `UsableHand`、予測値は出さない）」を足す
   - 「実装メモ」に、この計画で設計と違ったところを書く（無ければ「順 7 の 6 本目: なし」）
4. [game-spec.md](../wiki/overview/game-spec.md) の「実装計画の単位と順」の表の順 7 の行:
   - 2 列目（実装計画）の先頭の `client/ の画面（6 本の計画。` を `**done**（<1 の日付>。完了条件 6 を Windows の IL2CPP で確かめた）client/ の画面（6 本の計画。` に直す（ほかの列とリンクはそのまま。日付は `YYYY-MM-DD`）
   - 3 列目（受け持つ完了条件）が `6` だけになっていることを確かめる（7 を別の項目に移すのは進行役が行う）。`6・7…` のままなら、done にせずに止まって報告する
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
6. この計画の `status` を `done` にする（計画 0・1〜5 本目が `done` のときだけ。そうでなければ `in-progress` のままにして報告する）

## 5. テスト

すべて EditMode（`Tests/EditMode/`。名前空間 `Uta.Client.Infrastructure.Tests`）。書き方は 1 本目 3.3.7（偽の View はテストのファイルの中に `private sealed class FakeXxxView`。`CallLog` を共有し、記録は `"<メソッド名>:<引数を , でつないだもの>"`。リストの引数は要素を `"|"` でつなぐ。非同期は `[UnityTest]` + `UniTask.ToCoroutine` で Presenter の公開の `UniTask` メソッドを `await`）。

### 5.1 テスト用の部品

- `Tests/EditMode/Support/LessonTestData.cs`（`internal static class LessonTestData`）:
  - `MemoryDatabase Db()`: `MasterDataLoader.Load(File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath))`（`MasterCardCatalogTests.ReadBundled` と同じ読み方。同梱のマスタを使う）
  - `ICardCatalog Catalog()`: `MasterCardCatalog.Create(Db())`。`IGameMaster Master()`: `new MasterGameData(Db())`
  - `LessonView Lesson(LessonKind kind = LessonKind.Normal, ParamKind target = ParamKind.Rhythm, int maxTurns = 6, int turn = 1, int paramGained = 0, int shield = 0, int[] hand = null, bool[] usableHand = null, int[] drawPile = null, int[] discard = null, int[] exiled = null, int[] statusValues = null, int[] lessonEnhanced = null, ExamTurnView[] examTurns = null, int[] npcScores = null, int usesLeft = 1)`: 渡さなかった配列は空。`clearTarget = 85`、`perfectTarget = 170`、`paramBonuses`・`persistents` は空
  - `ProduceStateView State(ProducePhase phase, LessonView lesson = null, ExamResultView examResult = null, ProduceResultView result = null, int hp = 18, int maxHp = 30, int examRetryLeft = 3, DeckCardView[] deck = null, int operationNumber = 1)`: `scenarioId = 1`、`idolId = 1`、`week = 1`、`singing = 100`、`rhythm = 200`、`expression = 300`、ほかの配列は空、`deck` を渡さなければ `new[] { new DeckCardView(1, 1, false), new DeckCardView(2, 2, false), new DeckCardView(3, 1, true) }`（インスタンス 1 = カード 1、2 = カード 2、3 = カード 1 の強化済み）
- 使うカード（同梱のマスタ。計画 0 の 3.4 と lesson-card.md 14.1）: カード 1 = 「まっすぐ一声」N・アクティブ、通常は消費 4・"パラメータ+9"、強化は消費 3・"パラメータ+14"。カード 2 = 「はずむ足取り」N・アクティブ、消費 3・"パラメータ+2、元気+2"。期待値の文字は `DisplayText.Costs` の書式（5 本目。"消費 4"）で書く
- `FakeProducePort`（4 本目）・`FakeProduceFlow`（5 本目）・`FakeScreenNavigator`・`FakeDialogService`・`CallLog`（1 本目）を使う。`ProduceActions` は本物を `new ProduceActions(fakePort, session, new RefreshPlayerUseCase(null, new PlayerCache()))` で作る（`RefreshPlayerUseCase` は局面 `Finished`・`SessionEnded` のときだけ呼ばれるので、この計画のテストでは応答をその局面にしない）
- `FakeProduceFlow` の記録は 5 本目の定義に従う。下の表は `"Flow.HandleStep:<status>"`・`"Flow.Advance"`・`"Flow.ExitToHome"` と書く（5 本目の書式が違えば、その書式に読み替える）
- `ProduceMenu` の偽は `LessonPresenterTests.cs` の中に `private sealed class FakeProduceMenu : ProduceMenu`（`base(null, null, null)`。`ShowAsync` で `log.Add("Menu.Show")` して完了済みの `UniTask` を返す）

### 5.2 テストケース

| ファイル | テスト | 内容 |
|---|---|---|
| `ExamRankingTests.cs` | `Build_SortedDescending_PlayerFirstOnTies` | `Build(100, {150, 100, 50})` → (1, NPC, 150)、(2, 自分, 100)、(3, NPC, 100)、(4, NPC, 50) |
| | `Build_NpcNull_OnlyPlayer` | `Build(10, null)` → 1 行、順位 1 |
| | `Build_PlayerRank_EqualsExamRank` | スコアの組 3 つ（`(0, {0,0,0})`・`(500, {900, 600, 300})`・`(1000, {1000, 999})`）で、自分の行の `Rank == ProduceFormulas.ExamRank(...)` |
| | `CurrentRank_CountsStrictlyGreaterNpcs` | `CurrentRank(100, {150, 100, 50}) == 2` |
| `DisplayTextLessonTests.cs` | `LessonKind_AllValues` / `Status_AllValues`（3.2 の表の 12 個と `Nothing`、`(StatusKind)99 → "99"`）/ `Rank_PlusSuffix`（`CPlus → "C+"`、`SSPlus → "SS+"`）/ `ExamOutcome_AllValues` / `ExamKind_AllValues` / `Thousands_Groups`（`1234 → "1,234"`、`0 → "0"`）/ `Signed_Values`（`3 → "+3"`、`0 → "+0"`、`-2 → "-2"`） | |
| `LessonCardTextTests.cs` | `Create_Normal_UsesNameRarityTypeCostsDescription` | インスタンス 1 → Title `"まっすぐ一声 [N] アクティブ"`、Costs `"消費 4"`、Description `"パラメータ+9"` |
| | `Create_DeckEnhanced_UsesEnhancedFace` | インスタンス 3 → Title `"まっすぐ一声+ [N] アクティブ"`、Costs `"消費 3"`、Description `"パラメータ+14"` |
| | `Create_LessonEnhanced_UsesEnhancedFace` | インスタンス 1 で `lessonEnhanced = {1}` → `"まっすぐ一声+ …"` |
| | `Create_NotInDeck_Placeholder` | インスタンス 99 → Title `"カード？"`、Costs・Description が `""` |
| `LessonPresenterTests.cs` | `Initialize_Lesson_ShowsHeaderProgressStatus` | `Lesson(Special, Rhythm, maxTurns 6, turn 3, paramGained 62, shield 7, statusValues 長さ 13 で [1] = 3・[4] = 50)`、`hp 18 / 30` → `"SetHeader:特別レッスン（律動）,残り 4 / 6"`、`"SetProgress:獲得 62　クリア目標 85　パーフェクト 170"`、`"SetStatus:体力 18 / 30　元気 7|好調 3　集中強化 50%|スキルカード使用 残り 1"`、`"SetLastResult:"` |
| | `Initialize_Hand_EnabledFollowsUsableHand` | `hand {1, 2}`、`usableHand {true, false}` → `SetHand` の 2 枚目が `Enabled == false` |
| | `Initialize_UsableHandEmpty_AllEnabled` | `usableHand` 空 → 全部 `Enabled` |
| | `Initialize_UsableHandShorter_OutOfRangeEnabled` | `hand {1, 2, 3}`、`usableHand {false}` → `false, true, true` |
| | `Initialize_Exam_ShowsScoreRankAndTurns` | `Lesson(MidExam, turn 2, paramGained 1240, examTurns {歌唱 100%, 律動 285%, 表現 150%, 歌唱 200%}, npcScores {1500, 1000, 900})` → `"SetHeader:中間試験,残り 3 / 4"`（`maxTurns 4`）、`SetProgress` が `"スコア 1,240　順位 2 位"`・`"このターン: 律動（ボーナス 285%）"`（添字 1 = `Turn - 1`）・`"次: 表現→歌唱"` の 3 行 |
| | `Initialize_ExamLastTurn_NoNextLine` | `turn == examTurns.Count` → 「このターン」はあり、「次:」の行が無い |
| | `Initialize_ExamTurnZero_NoTurnLines` | `turn 0` → スコアの行だけ |
| | `Initialize_PileCounts` | `drawPile {4,5,6}`、`discard {7}`、`exiled 空` → `"SetPileCounts:3,1,0"` |
| | `UseCard_Confirmed_CallsActionAndShowsDiff` | 応答が局面 `Lesson`（`paramGained` +23、`hp` -4、`shield` +2）→ `FakeProducePort` の `ExecuteLessonActionAsync` が `(1, UseCard, 1, 0)`、`log` の最後が `"SetLastResult:獲得 +23　体力 -4　元気 +2"`。確認の文字は `"Confirm:まっすぐ一声 [N] アクティブ\n消費 4\nパラメータ+9\n\n使用しますか？"` |
| | `UseCard_NoChange_ShowsNoChange` | 応答の差が 0 → `"SetLastResult:変化なし"` |
| | `UseCard_Disabled_DoesNothing` | `Enabled == false` のカード → 確認も操作も無い |
| | `UseCard_Declined_DoesNothing` | `ConfirmResults` に `Negative` → 操作が呼ばれない |
| | `Skip_NoConfirm_CallsSkip` | `log` に `"Confirm:"` が無く、`ExecuteLessonActionAsync` が `(1, Skip, 0, 0)` |
| | `Step_Rejected_ShowsInfoAndRefreshes` | 業務エラー `PRODUCE_INVALID_ARGUMENT` → `"Info:そのカードは使えません。"` のあと `SetHand` がもう一度記録され、`Flow.` の記録が無い |
| | `Step_Recovered_RefreshesWithoutDiff` | `STALE_OPERATION` → `GetState` が局面 `Lesson` → `"SetLastResult:"`、`Flow.` の記録が無い |
| | `Step_LessonEnded_ShowsChangeThenHandlesStep` | 応答が局面 `WeekSelect`（`Lesson == null`、`rhythm` +62、`hp` +8）→ `log` の並びが `"Info:レッスン終了\n律動 +62\n体力 +8"`、`"Flow.HandleStep:Succeeded"` |
| | `Step_LessonToExam_ShowsChangeAndRefreshesInPlace` | 仕上げレッスン → 応答が局面 `Exam`（`Lesson.IsExam`）→ `"Info:レッスン終了…"` のあと `SetHeader` が `"最終試験"` で記録され、`Flow.` の記録が無い |
| | `Step_ExamResultPresent_HandlesStepWithoutInfo` | 試験中、応答が局面 `Exam` でも `ExamResult != null` → `"Info:"` が無く `"Flow.HandleStep:Succeeded"` |
| | `Step_AfterLeaving_IgnoresClicks` | `HandleStep` のあと `SkipAsync` をもう一度呼んでも `ExecuteLessonActionAsync` は 1 回だけ |
| | `Menu_ShowsMenu` | `"Menu.Show"` |
| | `Piles_PushesDeckList` | `"PushModal:DeckListPresenter"` |
| | `Dispose_Unsubscribes` | `Dispose` のあと偽の View でイベントを起こしても何も記録されない |
| `DeckListPresenterTests.cs` | `Initialize_FourSectionsInOrder` | `hand {2, 1}`、`drawPile {3, 1}`（わざと逆順）、`discard 空`、`exiled {2}` → 区分の見出しが `"手札 2 枚"`・`"山札 2 枚"`・`"捨札 0 枚"`・`"除外 1 枚"`。手札は `2, 1` の順、山札は `1, 3` の順、捨札の行は `"（なし）"`。行は `"まっすぐ一声 [N] アクティブ　消費 4\nパラメータ+9"` の形 |
| | `Initialize_NoLesson_ShowsMessage` | `Lesson == null` → `"レッスン中ではありません"` の区分 1 つ |
| | `Close_PopsModalOnce` | `CloseAsync` を 2 回 → `"PopModal:DeckListPresenter"` が 1 回 |
| `ExamResultPresenterTests.cs` | `Initialize_FirstAttempt_TitleIsOne` | `Attempt == 0`、`Kind == Mid` → `"SetTitle:中間試験 結果（1 回目）"` |
| | `Initialize_RankingMarksPlayer` | `Score 1388`、`NpcScores {1440, 1170, 900}` → `SetRanking` の行が `"1位　ライバル 1　1,440"`（False）・`"2位　あなた　1,388 ◀"`（True）・`"3位　ライバル 2　1,170"`（False）・`"4位　ライバル 3　900"`（False）。偽の View の記録は `"<Text>/<IsPlayer>"` を `"|"` でつなぐ |
| | `Initialize_RetryAvailablePassed` | `Phase == ExamResult`、`Passed`、`examRetryLeft 2` → `"SetButtons:再挑戦（残り 2）,次へ"` |
| | `Initialize_RetryAvailableFailed` | 不合格 → `"SetButtons:再挑戦（残り 2）,プロデュース終了"`、`"SetOutcome:不合格"` |
| | `Initialize_NoRetry_NextOnly` | `Phase == WeekSelect` → `"SetButtons:,次へ"`（null は空で記録） |
| | `Initialize_NoResult_DoesNotCallFlow` | `ExamResult == null` → `"SetOutcome:結果がありません。"`、`Flow.` の記録が無い |
| | `Retry_Confirmed_CallsDecideTrueThenHandlesStep` / `Retry_Declined_DoesNothing` | `DecideExamRetryAsync` が `retry == true` |
| | `Next_RetryAvailablePassed_DecideFalseWithoutConfirm` | `"Confirm:"` が無い |
| | `Next_RetryAvailableFailed_ConfirmsThenDecideFalse` | |
| | `Next_NoRetry_MarksShownAndAdvances` | `session.ExamResultShown == true`、`"Flow.Advance"` が 1 回 |
| | `Next_Twice_AdvancesOnce` | `NextAsync` を 2 回 → `"Flow.Advance"` が 1 回 |
| `ProduceResultPresenterTests.cs` | `Initialize_Completed_ShowsRankAndRewards` | `Rank == APlus`、`FinalRank 1`、`ExamOutcome FinalPassed` → `"SetRank:ランク A+"`、`SetLines` が 3.4.4 の 10 行の順 |
| | `Initialize_MidFailed_NoFinalLine` | `FinalRank == 0` → 「最終試験」の行が無く、`"中間試験で不合格"` がある |
| | `Initialize_NoResult_ShowsPlaceholder` | |
| | `Home_ClearsSessionAndExitsOnce` | `GoHomeAsync` を 2 回 → `session.Current == null`、`"Flow.ExitToHome"` が 1 回 |
| `ProduceFlowTests.cs`（追記） | `Enter_Lesson_PushesLessonPage` / `Advance_Exam_PushesLessonPage` / `Advance_Finished_PushesProduceResultPage` / `Advance_ExamResultPresentNotShown_PushesExamResultFirst`（局面 `WeekSelect` でも `ExamResultPresenter`）/ `Advance_ExamResultShown_PushesPhaseScreen` / `Advance_PhaseExamResultWithoutResult_ShowsInfoAndExits`（`"Info:試験の結果を取得できませんでした。ホームから再開してください。"` のあと 4 本目の `ExitToHomeAsync` の記録。`PushPage` が無い） | `log` の最後の `"PushPage:<Presenter>"` を確かめる（前のページを閉じる記録は 4 本目の `ProduceFlowTests` と同じなので、ここでは確かめない） |

### 5.3 Play モード・手動（タスク 4。Editor。サーバー起動済み・初期付与済み）

- 最短シナリオ（3 週）: 編成で最短シナリオを選ぶ → 開始 → 週 1 のレッスン → 手札が名前・レア度・種類・消費・説明文つきで出る。使えないカードが灰色で押せない。カードをタップ → 確認（説明文つき）→ 使用 → 獲得・体力・手札が変わり、「直前の結果」の行に差が出る。スキップできる。「山札 …」のボタン → 一覧のモーダル（4 つの区分）→ 閉じる。レッスンが終わると「レッスン終了」と上がった値のダイアログ →（クリアの報酬があれば）スキルカードの獲得 → 週 2（仕上げ）→ 週 3 の最終試験 → スコア・順位・このターン・次 が出る → 試験結果（「1 回目」、自分の行に ◀）→ 次へ（または再挑戦）→ プロデュース結果 → ホームへ。ホームのガチャ通貨・プレイヤーLv が増えていて、「再開」が無い
- 中間試験があるシナリオ（初級）: 中間試験の結果で「再挑戦（残り n）」が出る。再挑戦で最初からやり直し、結果の題が「2 回目」になる。不合格で「プロデュース終了」を選ぶと結果に進む
- レッスンの途中でアプリを終了 → 再起動 → ホームの「再開」でレッスンに戻る（同じ手札・ターン。「直前の結果」は空）
- 「≡」→ 閉じる → そのままカードを使える。「≡」→ 放棄 → 確認 → ホーム

## 6. 未決事項

なし（4 本目・5 本目の最終形の名前に合わせる読み替えは 3章の冒頭と 5.1 に書いた）。
