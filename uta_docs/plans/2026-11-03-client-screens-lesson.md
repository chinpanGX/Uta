---
title: 実装計画: client/ の画面 6/6 レッスン・試験・試験結果・プロデュース結果
status: ready
updated: 2026-11-07
spec: ../wiki/systems/produce.md
---

# 実装計画: client/ の画面 6/6 レッスン・試験・試験結果・プロデュース結果

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **View は P3 の骨格に `IXxxView` の実装を足す**（[foundation](2026-11-03-client-screens-foundation.md) 3.3.3）。`LessonPage`（`ILessonView`）・`DeckListModal`（`IDeckListView`。山札・捨札・除外の一覧のモーダル）・`ExamResultPage`（`IExamResultView`）・`ProduceResultPage`（`IProduceResultView`）と、行の View（`DeckSectionHeaderRowView`・`DeckCardRowView`・`ExamRankingRowView`）。フィールド・子の名前は [P3](2026-11-07-client-prefabs-screens-b.md) の 3.1・3.2・4章が正で、この計画は UI を組まない（旧版の `BuildUi`・`UiFactory`・「画面のレイアウト」の節と、`UtaBootstrap.RegisterScreens` への登録は消した。Addressables への登録は P3 が済ませている）。Prefab の作り直しは要らない
2. **画面は `Produce` シーンに積む**（[formation](2026-11-03-client-screens-formation.md) の決めたこと 1〜3）。底はいつも背景のページ `ProduceBackdropPage`。この計画は `ProduceFlow.PushForPhaseAsync` に、局面 `Lesson`・`Exam`（レッスンのページ）・`ExamResult`・`Finished`（プロデュース結果のページ）の `case` と、**試験結果の先出し**（応答に `ExamResult` があり、まだ見せていなければ、局面によらず試験結果のページを先に出す）を足す
3. **レッスン・試験の間は、同じページをその場で描き直す**（`Refresh()`）。操作のあとも局面が `Lesson`・`Exam` のままで、試験結果が無いときだけ。仕上げレッスンから続けて試験に入るときも同じページで描き直す。それ以外（局面が変わった・試験が終わった・セッションが終わった）は `ProduceFlow.HandleStepAsync` に渡し、`ShowCurrentAsync`（背景より上を全部閉じてから次の局面の画面を積む）に任せる。この計画の Presenter は自分のページを閉じない
4. **試験結果の「次へ」（再挑戦の選択が無いとき）は、`session.ExamResultShown = true` のあと `ProduceFlow.ShowCurrentAsync()`**。旧版の `AdvanceAsync` は formation の書き直しで無くなった
5. **プロデュース結果の「ホームへ」は、`session.Clear()` のあと `ProduceFlow.ExitToHomeAsync()`**（中身は `ISceneNavigator.GoToHomeAsync`。`Home` シーンを読み込み直し、`Produce` のページはシーンごと破棄される）。所持品の取り直しは、局面 `Finished` の応答を受けたときに `ProduceActions` が済ませている
6. **例外は formation の決めたこと 12 に合わせる**: ボタンの処理（Presenter の公開 `UniTask` メソッド）が `catch (Exception e)` で受け、`Debug.LogException(e)` のあと `"エラーが発生しました: " + e.Message` を出す。そのため `ProduceResultPresenter` も `DialogService` を受ける（旧版は受けていなかった）
7. **スキルカードの文字は 5 本目（[week](2026-11-03-client-screens-week.md)）の `CardSummaryBuilder`・`DisplayText.Card`・`DisplayText.Costs` を使う**（獲得の選択肢と同じ書式 `"まっすぐ一声+ [N] アクティブ"`・`"消費 3"`）。この計画の `LessonCardText` は、デッキからカード ID と強化の有無を引いてそれを呼ぶだけ
8. **行の View のメソッド**: `DeckSectionHeaderRowView.SetText`・`DeckCardRowView.SetText`・`ExamRankingRowView.SetRow(text, isPlayer)` を足す。**順位表の自分の行だけ、C# が文字の色を `UiTheme.Accent` に変える**（P3 の決めたこと 10 の d・3.2 の表。foundation 3.3.3 の「C# は色を変えない」の例外）。手札の行（`SkillCardRowView`）の文字を入れるメソッド `SetTexts` は、獲得の選択肢で先に使う week の計画が足した `SetTexts(string title, string costs, string description)` を使う（この計画は足さない）
9. **`DisplayText` の 3 つのメソッドは、型と同じ名前にしない**: `LessonKindName`・`ExamOutcomeText`・`ExamKindName`（旧版の `LessonKind`・`ExamOutcome`・`ExamKind`）。型と同じ名前のメソッドにすると、メソッドの中の `LessonKind.Normal` がメソッドに解決されてコンパイルできないため（week の `CardTypeName` と同じ理由）。文字列は旧版のまま
10. 画面の表示の決定（旧版の 1.2）は変えない: カードはタップ → 確認 → 使用、スキップは確認なし、操作の前後の差の行、レッスン終了のダイアログ、山札などの一覧のモーダル、残りターンの表示、順位表のライバルの仮の名前（1.2）
11. **順位表のライバルの名前を、仮に「ライバル 1・2・3」（上から順）にしてよいか**（1.2）。`ExamResultDto` は NPC の名前を返さず、名前を決める仕様も無いため（ユーザーが推奨どおり了承した。2026-11-07）
12. **レッスンが終わるたびに「レッスン終了」と上がった値のダイアログで 1 回止まる**（1.2。ターン終了時の効果の 1 件ずつの表示を MVP から外した代わり）。この動きでよいか（ユーザーが推奨どおり了承した。2026-11-07）
13. **自分の行の印 `◀`** は P3 の決めたこと 11（Noto Sans JP に字形があるか）の答えに従う。豆腐なら、この計画の文言（3.4.3 の `" ◀"`）を P3 の確認で決めた文字に置き換える（ユーザーが推奨どおり了承した。2026-11-07）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7（client/ の画面）の C# の 6 本目（最後）。プロデュースの残りの局面の画面を作る: レッスン・試験（`ProducePhase.Lesson`・`Exam`）、山札・捨札・除外の一覧（モーダル。PRD-08）、試験結果（`ExamResult`、および試験が終わった応答の `ExamResult`）、プロデュース結果（`Finished`）。これで、起動 → ガチャ → 強化 → 編成 → プロデュースを最後まで → 結果 → ホーム、が画面だけで通る（完了条件 6）。

- 仕様: レッスン・カードは [lesson-card.md](../wiki/systems/lesson-card.md)（8章 ステータス、14章 カード）、プロデュースは [produce.md](../wiki/systems/produce.md) 4〜6章・5.1.1（試験の結果と局面）・9章（実行要件。PRD-03〜08）・10.1（局面の遷移）。DTO は [produce-server.md](../wiki/architecture/server/produce-server.md) 6章と `shared/Uta.Shared.Contracts/Produce/`。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 3・7・8。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 6章・7章（7.6 プロデュースの画面の流れ）・8章（シーン構成）
- **画面を作る共通ルールは [1 本目（foundation）](2026-11-03-client-screens-foundation.md) 3.3 が正**（遷移・Presenter の形・2 本のコンストラクター・`[AssetAddress]`・`InitializeAsync`・`Dispose` での購読の解除・実行中フラグ・破棄の印・View の形（`Awake` で `onClick` を購読して C# の `event` を起こす）・`UiObjects`・テストの偽 `CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`）。この計画では繰り返さない
- **View のフィールドと Prefab の子の対応は [P3](2026-11-07-client-prefabs-screens-b.md) の 3.1・3.2・4章が正**（`LessonPage`・`DeckListModal`・`ExamResultPage`・`ProduceResultPage`、行 `SkillCardRowView`・`DeckSectionHeaderRowView`・`DeckCardRowView`・`ExamRankingRowView`）
- **`Produce` シーン・`ProduceFlow` の形は [4 本目（formation）](2026-11-03-client-screens-formation.md) 3.4 が正**（`ProduceEntry`・`ProduceFlow.ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync`・`PushForPhaseAsync`。`ProduceFlow`・`ProduceMenu` は `Produce` シーンのスコープ）
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（クライアントは判定と予測まで。MVP の使用可否はサーバーの `UsableHand`: screen-design.md 6章 [決定 2026-11-03]）、[0009](../wiki/decisions/0009-client-stack.md)
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0 → P1 → P2 → foundation → gacha → owned → P3 → formation → week → **この計画**
- **前提**（実装に入る前に、次がブランチに入っていることを確かめる。無ければ止まって報告する）:
  - 計画 0（[スキルカードの名前と説明文](2026-11-03-masterdata-card-text.md)）が done: 同梱のマスタにカードの名前・説明文がある
  - foundation が done: `PageBase`・`ModalBase`、部品の View のメソッド（`ButtonView.Button`・`SetLabel`・`SetInteractable`、`HeaderView.SetTitle`、`ListRowBase.Button`・`SetInteractable`）、`UiObjects`（`ClearChildren`・`AddRow`）、`DialogService`、`PageStack`、`IGameMaster.GetCardText`（3.4）、テストの偽（`CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`）
  - gacha・owned が done: `DisplayText.RarityName`・`ParamName`
  - P3 が done: `Scripts/Presentation/Produce/LessonPage.cs`・`DeckListModal.cs`・`ExamResultPage.cs`・`ProduceResultPage.cs`・`SkillCardRowView.cs`・`DeckSectionHeaderRowView.cs`・`DeckCardRowView.cs`・`ExamRankingRowView.cs`（P3 3.1 のフィールドだけの骨格）と、`Assets/Prefabs/Screens/Produce/` の 4 つの Prefab（Addressables の `Screens` に、アドレス `LessonPage`・`DeckListModal`・`ExamResultPage`・`ProduceResultPage`）。フィールドの名前・型が P3 3.1 と違えば止まって報告する
  - formation が done: 応答の写し（`ProduceProgress`・`ProduceLesson`・`ProduceExamTurn`・`ProduceDeckCard`・`ProduceExamResult`・`ProduceOutcome`・`ProduceEndingKind`）、`ProduceSession`（`Current`・`ExamResultShown`・`Clear`）、`ProduceActions`・`ProduceStepStatus`・`LessonActionType`、`ProduceFlow`（3.4 の形。`PushForPhaseAsync` は `switch (phase)`）、`ProduceMenu`、`ProduceEntry`、`LessonDto.UsableHand`（サーバーの直し）、テストの偽 `FakeProducePort`・`FakePlayerPort`・`TestProduce`
  - week が done: `CardSummaryBuilder`・`CardSummary`・`ProduceChange`、`DisplayText.Card`・`Costs`・`Change`、`PushForPhaseAsync` の `WeekSelect`・`CardOffer` の `case`、テストの偽 `FakeProduceFlow`
  - Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready` が ready）。**Editor を開けない環境では、この計画のどのタスクも確かめられない**。そのときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **決めないこと**: Prefab の階層・見た目・フォントサイズ・色（P3）。`Produce` シーンの組み立てと `ProduceFlow` の共通の処理（formation）。週の行動・授業・支給・スキルカード獲得の画面（week）

### 1.1 作らないもの（理由）

| 作らないもの | 理由 |
|---|---|
| ターン終了時の効果を 1 件ずつ見せる表示 | MVP から外した [決定。2026-11-03。ユーザーの判断。[screen-wireframes.md](../wiki/overview/screen-wireframes.md) 3]。サーバーの応答に効果の記録が無い。代わりに操作の前後の差を出す（1.2） |
| カードの効果の予測値 | MVP は出さない（[screen-design.md](../wiki/architecture/screen-design.md) 6章・7.5） |
| ドリンク・育成アイテム | PRD-10（次点）。MVP 外 |
| ステータスの説明（タップで説明） | 叩き台の要素で PRD に要件が無い。ステータスは名前と値だけ出す |
| 試験結果の「合格！ 報酬」の欄（順位報酬・育成pt・育成アイテム） | `ExamResultDto` に報酬の項目が無い。育成pt は MVP で扱わない（game-spec.md の MVP の表）。中間試験の合格時の育成アイテムは MVP では付けない（produce.md 5.1.1）。合否の文字だけ出す |
| 最終評価の評価値の内訳（パラメータ × 倍率・順位分・スコア分） | `ProduceResultResponse` に内訳の項目が無い。合計だけ出す |
| キャラ表示、特別レッスンの見出し以外の演出 | 見た目は仮（P3） |
| 持続効果（`Persistents`）の表示 | 種類（`PersistentTrigger`）と残りしか応答に無く、何が起きるかを言えない。カードの説明文で読む |
| プロデュースのメニュー（[≡]）の画面 | formation の `ProduceMenu`（`DialogService.ChooseAsync`）を使う（P3 の決めたこと 3） |

### 1.2 表示の決定（旧版から変えない）

- カードをタップ → 確認ダイアログ（名前・消費・説明文。ボタン「使用」「やめる」）→ 使用。叩き台の「詳細 → 使用」と同じ結果。使えないカード（`enabled == false`）の行は押せない
- スキップは確認なし（叩き台の決定。2026-11-01）
- **操作の前後の差**: カードの使用・スキップのあと、その操作で変わった `獲得`（試験は `スコア`）・`体力`・`元気` を、ページの「直前の結果」の行（`lastResultText`）に出す（3.4.1 の `SetLastResult`）。**レッスン（試験でない）が終わったら**、情報ダイアログ「レッスン終了」で、**レッスン全体の**パラメータと体力の差（week の `ProduceChange`・`DisplayText.Change`。体力はレッスン開始時の値との差: 3.4.1 の `LessonEndText`）を出してから次の画面に進む。試験の終わりは試験結果の画面が出るので、ダイアログを出さない
- **山札・捨札・除外の一覧（PRD-08）**: レッスンのページのボタン（`pilesButton`。ラベルは `"山札 8　捨札 5　除外 2"`）を押すと、モーダル `DeckListModal` を出す。中身は手札・山札・捨札・除外の 4 つの区分で、カードごとに名前・レア度・種類・消費・説明文。山札は並びを返さない（produce-server.md 6章）ので、どの区分もインスタンス ID の昇順（手札だけは手札の並び）。[≡]（`ProduceMenu`）には足さない（`ProduceMenu` は週の画面と共有で、formation の持ち物のため）
- ターンの表示は叩き台どおり残りのターン（`"残り 4 / 6"`）
- 試験結果の順位表のライバルの名前は仮に「ライバル 1・2…」（上から順に振る）。自分の行は `"あなた"` で、末尾に `" ◀"` を付け、文字の色を `UiTheme.Accent` にする（叩き台の `◀`）

### 1.3 完了条件

- 順 7 は完了条件 **6 だけ**を受け持つ。この計画のタスク 5 で、Windows の IL2CPP のビルドで 6 を手で確かめる
- **完了条件 6**（[game-spec.md](../wiki/overview/game-spec.md)「完了条件」の表の 6 の文のまま）: 「Windows のクライアントで、起動（`SignUpAsync` → `SignInAsync`）→ ガチャ → 強化 → 編成 → 最短シナリオを最後まで → 結果、を通す。初級の通しも 1 回」。この計画では次の 2 つがそろったときに 6 を満たしたとする（5.3 の通し A・通し B を Windows の IL2CPP で行う）:
  - **通し A**: 起動 → ガチャ → 強化 → 編成 → 最短シナリオ（`ScenarioId` 1「最短シナリオ（仮）」）を放棄せずに局面 `Finished` まで進め、プロデュース結果の画面 →「ホームへ」で `Home` に戻る
  - **通し B**: 初級（`ScenarioId` 2「初級（仮）」）を 1 回、放棄せずに局面 `Finished` まで進め、プロデュース結果の画面 →「ホームへ」で `Home` に戻る（**途中で放棄しない。中間試験で止めない**）。中間試験で再挑戦と不合格を 1 回ずつ出す（5.3 の落とし方）
- 完了条件 7（Android の実機・IL2CPP との計算の一致）は順 7 の範囲外（game-spec.md の別の項目）。この計画では確かめない

## 2. 触るファイル

パスは、`client/` のものは `client/uta/Assets/` からの相対、wiki はリポジトリのルートからの相対。新しい `.cs` には Unity が作った `.meta` をコミットする。Prefab・シーン・Addressables のアセットは触らない（P3・foundation が作ったもの）。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Produce/ExamRanking.cs` | 試験の順位表と、試験中の見込みの順位（3.1） |
| 変更 | `Scripts/Presentation/Common/DisplayText.cs` | レッスンの種類・ステータス・ランク・試験の結果・試験の種類・3 桁区切り・符号つき（3.2） |
| 作成 | `Scripts/Presentation/Produce/LessonCardText.cs` | レッスン中のカード 1 枚の表示用の文字（手札と一覧が共有。3.3） |
| 作成 | `Scripts/Presentation/Produce/ILessonView.cs`・`HandCardViewData.cs`・`LessonPresenter.cs` | レッスン・試験（3.4・3.4.1） |
| 変更 | `Scripts/Presentation/Produce/LessonPage.cs` | P3 の骨格に `ILessonView` の実装を足す（3.6） |
| 作成 | `Scripts/Presentation/Produce/IDeckListView.cs`・`DeckSectionViewData.cs`・`DeckListPresenter.cs` | 山札・捨札・除外の一覧（モーダル。3.4・3.4.2） |
| 変更 | `Scripts/Presentation/Produce/DeckListModal.cs`・`DeckSectionHeaderRowView.cs`・`DeckCardRowView.cs` | P3 の骨格に `IDeckListView` の実装と行のメソッドを足す（3.6） |
| 作成 | `Scripts/Presentation/Produce/IExamResultView.cs`・`ExamRankingRowViewData.cs`・`ExamResultPresenter.cs` | 試験結果（3.4・3.4.3） |
| 変更 | `Scripts/Presentation/Produce/ExamResultPage.cs`・`ExamRankingRowView.cs` | P3 の骨格に `IExamResultView` の実装と行のメソッドを足す（3.6） |
| 作成 | `Scripts/Presentation/Produce/IProduceResultView.cs`・`ProduceResultPresenter.cs` | プロデュース結果（3.4・3.4.4） |
| 変更 | `Scripts/Presentation/Produce/ProduceResultPage.cs` | P3 の骨格に `IProduceResultView` の実装を足す（3.6） |
| 変更 | `Scripts/Presentation/Produce/ProduceFlow.cs` | `PushForPhaseAsync` に試験結果の先出しと、局面 `Lesson`・`Exam`・`ExamResult`・`Finished` の `case`（3.5） |
| 作成 | `Tests/EditMode/Support/LessonTestData.cs` | テスト用の状態とマスタ（5.1） |
| 作成 | `Tests/EditMode/ExamRankingTests.cs`・`DisplayTextLessonTests.cs`・`LessonCardTextTests.cs`・`LessonPresenterTests.cs`・`DeckListPresenterTests.cs`・`ExamResultPresenterTests.cs`・`ProduceResultPresenterTests.cs` | テスト（5章） |
| 変更 | `Tests/EditMode/ProduceFlowTests.cs` | 追記（5章） |
| 変更 | `uta_docs/wiki/overview/game-spec.md` | 順 7 の行を `done` にする（タスク 5。条件つき） |
| 変更 | `uta_docs/plans/2026-11-07-client-screens-index.md` | この計画の状態（タスク 5。条件つき） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md` | 2章の PRD-08 の行、6章、実装メモ（タスク 5） |
| 変更 | `uta_docs/wiki/architecture/server/produce-server.md` | 6章の `ExamResultDto.Attempt`・`LessonDto.ExamTurns` の注記（タスク 5） |
| 変更 | `uta_docs/wiki/architecture/unity7-evaluation.md` | 2章（タスク 5） |

## 3. 型とシグネチャ

C# は `client/` の `.editorconfig` の範囲（C# 9 の範囲。コレクション式 `[...]`・record・ファイルスコープ namespace は使わない。配列は `new[] { ... }`、空は `Array.Empty<T>()`）。BOM 付き UTF-8、LF、Allman、`var`。名前空間はフォルダどおり（`Application/Produce` → `Uta.Client.Application.Produce`、`Presentation/Produce` → `Uta.Client.Presentation.Produce`、`Presentation/Common` → `Uta.Client.Presentation.Common`、テストは `Uta.Client.Infrastructure.Tests`）。`using UnityScreenNavigator;` と `using Uta.Client.Application.Navigation;` を両方書くファイルは foundation 3.3.10 の別名を足す。

**型の名前**は formation 3.2 のとおり: 最上位の状態は `ProduceProgress`、レッスンは `ProduceLesson`、試験のターンは `ProduceExamTurn`、デッキのカードは `ProduceDeckCard`、試験結果は `ProduceExamResult`、プロデュースの結果は `ProduceOutcome`（`ProduceProgress.Result`）、終わり方は `ProduceEndingKind`。`ProduceSession` に `Result` は無い（結果は `session.Current.Result`）。

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
public static string LessonKindName(LessonKind kind);   // Normal → "レッスン"、Special → "特別レッスン"、Finishing → "仕上げレッスン"、MidExam → "中間試験"、FinalExam → "最終試験"、Nothing → "-"、それ以外 → kind.ToString()
public static string Status(StatusKind kind);           // 下の表。Nothing → "-"、表に無い値 → kind.ToString()
public static string Rank(ProduceRank rank);            // F → "F"、E → "E"、D → "D"、C → "C"、CPlus → "C+"、B → "B"、BPlus → "B+"、A → "A"、APlus → "A+"、S → "S"、SPlus → "S+"、SS → "SS"、SSPlus → "SS+"、Nothing → "-"、それ以外 → rank.ToString()
public static string ExamOutcomeText(ExamOutcome outcome); // MidFailed → "中間試験で不合格"、FinalFailed → "最終試験で不合格"、FinalPassed → "最終試験に合格"、Nothing とそれ以外 → ""
public static string ExamKindName(ExamKind kind);       // Mid → "中間試験"、Final → "最終試験"、Nothing とそれ以外 → "試験"
public static string Thousands(int value);              // value.ToString("N0", CultureInfo.InvariantCulture)（1234 → "1,234"、-1234 → "-1,234"）
public static string Signed(int value);                 // 0 以上は "+" + value、負は value.ToString()（"-3"）。3 桁区切りはしない
```

- メソッド名を型と同じにしない（決めたこと 9）。`Status`・`Rank` は型名（`StatusKind`・`ProduceRank`）と違うのでそのまま

`Status` の表示名は [lesson-card.md](../wiki/systems/lesson-card.md) 8章・[glossary.md](../wiki/glossary.md) の仮称（仮称の置き換えは glossary で決める。この計画では新しい名前を作らない）:

| `StatusKind` | 表示名 | `StatusKind` | 表示名 |
|---|---|---|---|
| `GoodCondition` | 好調 | `Motivation` | やる気 |
| `Excellent` | 絶好調 | `CostHalf` | 消費体力減少 |
| `Focus` | 集中 | `CostDouble` | 消費体力増加 |
| `FocusBoost` | 集中強化 | `CostReduce` | 消費体力削減 |
| `Impression` | 好印象 | `ShieldGainBlock` | 元気増加無効 |
| `ImpressionBoost` | 好印象強化 | `DebuffGuard` | 低下状態無効 |

**`DisplayText.cs` の `using`**: owned が書いた `using Uta.Shared.Domain.Lesson;`・`using Uta.Shared.Domain.Produce;` と、week が足したもの（`System.Collections.Generic`・`Uta.Client.Application.Produce`）はそのまま使う。この計画は `using System.Globalization;`（`Thousands` の `CultureInfo`）を、無ければ足す。ほかの `using` は足さない（`LessonKind`・`StatusKind` は `Uta.Shared.Domain.Lesson`、`ProduceRank`・`ExamOutcome`・`ExamKind` は `Uta.Shared.Domain.Produce` で、どちらも owned の時点である）。

（`LessonKind`・`StatusKind` は `Uta.Shared.Domain.Lesson`、`ProduceRank`・`ExamOutcome`・`ExamKind` は `Uta.Shared.Domain.Produce`。`client/uta/Assets/Scripts/Infrastructure/MasterData/Generated` に同名の enum があるので、`DisplayText.cs` は Domain の名前空間を `using` し、マスタの名前空間は `using` しない。）

### 3.3 `LessonCardText`（`Presentation/Produce/LessonCardText.cs`）

```csharp
public sealed class LessonCardText
{
    public LessonCardText(int instanceId, string title, string costs, string description);
    public int InstanceId { get; }
    public string Title { get; }         // "まっすぐ一声+ [N] アクティブ"（week の DisplayText.Card）
    public string Costs { get; }         // "消費 3"（week の DisplayText.Costs）
    public string Description { get; }   // "パラメータ+14"（CardSummary.Description）

    /// <summary>
    /// 1. deckCard = state.Deck のうち InstanceId == instanceId のもの。無ければ new LessonCardText(instanceId, "カード？", "", "") を返す
    ///    （MVP はレッスン中にカードを作る効果（CreateCard）を入れないので起きない。起きても止めない）
    /// 2. enhanced = deckCard.Enhanced || (state.Lesson != null && state.Lesson.LessonEnhanced に instanceId が含まれる)
    /// 3. summary = CardSummaryBuilder.Build(catalog, master, deckCard.CardId, enhanced)
    /// 4. new LessonCardText(instanceId, DisplayText.Card(summary), DisplayText.Costs(summary), summary.Description)
    /// </summary>
    public static LessonCardText Create(ProduceProgress state, ICardCatalog catalog, IGameMaster master, int instanceId);
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
// LessonPage : PageBase, ILessonView（3.6）

[AssetAddress(nameof(LessonPage))]
public sealed class LessonPresenter : IPresenter
{
    [Inject]
    public LessonPresenter(LessonPage view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions,
        ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);   // : this((ILessonView)view, ...)
    public LessonPresenter(ILessonView view, ProduceSession session, ICardCatalog catalog, IGameMaster master, ProduceActions actions,
        ProduceFlow flow, ProduceMenu menu, DialogService dialogs, IScreenNavigator navigator);

    public UniTask InitializeAsync();                        // 購読（4 つのイベント）→ Refresh() → SetLastResult("")
    public void Dispose();                                   // disposed = true、4 つのイベントの購読を解く
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
// DeckListModal : ModalBase, IDeckListView（3.6）

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
// ExamResultPage : PageBase, IExamResultView（3.6）

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
// ProduceResultPage : PageBase, IProduceResultView（3.6）

[AssetAddress(nameof(ProduceResultPage))]
public sealed class ProduceResultPresenter : IPresenter
{
    [Inject]
    public ProduceResultPresenter(ProduceResultPage view, ProduceSession session, ProduceFlow flow, DialogService dialogs);
    public ProduceResultPresenter(IProduceResultView view, ProduceSession session, ProduceFlow flow, DialogService dialogs);
    public UniTask InitializeAsync();
    public void Dispose();
    public UniTask GoHomeAsync();                            // HomeClicked → GoHomeAsync().Forget()
}
```

- Presenter はどこにも登録しない（foundation 3.3.2）。`ProduceFlow`・`ProduceMenu` は `Produce` シーンのスコープ、`ProduceSession`・`ProduceActions`・`ICardCatalog`・`IGameMaster` はルートのスコープにあり、基盤が `Produce` シーンのスコープの子スコープで解決する（formation 3.4）

**4 つの Presenter に共通の決まり**（foundation 3.3.2 の 5・6・9 に足す）:
- 実行中フラグ `running`（foundation 3.3.2 の 5。`finally` で `false` に戻す）と破棄の印 `disposed`（同 6。`await` のあと View に触る前に `if (disposed) return;`）に加えて、**`leaving`**（`bool`。初期値 `false`）を持つ。`ProduceFlow` のメソッド（`HandleStepAsync`・`ShowCurrentAsync`・`ExitToHomeAsync`）を呼ぶ直前に `leaving = true` にし、以後 `false` に戻さない（例外のときだけ戻す: 下）。ボタンの処理（`XxxAsync`）の先頭は `if (running || leaving) return;`
- `ProduceFlow` を呼んだあとは、View に触らない（`Refresh()` などを呼ばない。ページは `ProduceFlow` が閉じている: `ShowCurrentAsync` の `PopToRootAsync`、または `GoToHomeAsync` のシーンの破棄。formation 3.4）
- 自分のページを `PopPageAsync` しない（閉じるのは `ProduceFlow` だけ）。モーダル（`DeckListPresenter`）は自分を `PopModalAsync(this, playAnimation: false)` で閉じる
- 例外（`catch (Exception e)`。formation の決めたこと 12）: `UnityEngine.Debug.LogException(e)` → `disposed` なら終わる → `leaving = false` → `await dialogs.ShowInfoAsync("エラーが発生しました: " + e.Message)`。`ApiCore` が出すダイアログ（foundation 3.3.5）とは別に、想定外の例外だけをここで出す（ユースケースは通信の失敗を例外にしない）

#### 3.4.1 `LessonPresenter` の動き

フィールド（3.4 の共通の決まりの `running`・`disposed`・`leaving` のほかに）:
- `private List<HandCardViewData> hand = new List<HandCardViewData>();`（**初期値は空のリスト**。`Refresh` の前・`Refresh` が何もしなかったときに `UseCardAsync` が来ても、カードが見つからず何もしない）
- `private int? lessonStartHp;`（**レッスン開始時の体力**。初期値 `null`。`LessonEndText` が使う）

`Refresh()`（`InitializeAsync` と、同じページのまま更新するときに呼ぶ。`private`）:
- `state = session.Current`、`lesson = state?.Lesson`。どちらかが `null` なら何もしない
- `lessonStartHp == null && lesson.Turn <= 1` なら `lessonStartHp = state.Hp`（**そのレッスンで最初に `Turn` が 1 以下の状態を見た `Refresh` でだけ**保持する。1 ターンに複数枚使えるカードで `Turn` が 1 のまま体力が減っても上書きしない。レッスン・試験の間の `state.Hp` は `LessonState` の体力: produce.md 4.3。再開で `Turn` が 2 以上から始まったら `null` のまま）
- `SetHeader(title, turnText)`:
  - `title` = `DisplayText.LessonKindName(lesson.Kind)`。`!lesson.IsExam` のときは末尾に `"（" + DisplayText.ParamName(lesson.TargetParam) + "）"`（例 `"特別レッスン（律動）"`。試験は `"中間試験"`）
  - `remaining` = `lesson.Turn <= 0 ? lesson.MaxTurns : Math.Max(0, lesson.MaxTurns - lesson.Turn + 1)`（今のターンを含む残り。試験の `MaxTurns` は審査ターンの数）。`turnText` = `$"残り {remaining} / {lesson.MaxTurns}"`
- `SetProgress(lines)`。`lines` は次の順で足す:
  - レッスン（`!lesson.IsExam`）: 1 行 `$"獲得 {lesson.ParamGained}　クリア目標 {lesson.ClearTarget}　パーフェクト {lesson.PerfectTarget}"`
  - 試験（`lesson.IsExam`）。`turns = lesson.ExamTurns`、`t = lesson.Turn`（1 始まり。開始前は 0）、`n = turns.Count`:
    1. `$"スコア {DisplayText.Thousands(lesson.ParamGained)}　順位 {ExamRanking.CurrentRank(lesson.ParamGained, lesson.NpcScores)} 位"`
    2. `1 <= t && t <= n` のとき: `cur = turns[t - 1]`（**今のターンは添字 `t - 1`**。サーバーの `EffectExecutor` が `turns[Math.Max(0, state.Turn - 1)]` を使うのと同じ）で `$"このターン: {DisplayText.ParamName(cur.Param)}（ボーナス {cur.ScoreBonusPercent}%）"`
    3. `1 <= t && t < n` のとき: 添字 `t` から `n - 1` まで（今のターンの次から最後まで）の `DisplayText.ParamName(turns[k].Param)` を `"→"` でつないで `"次: " + それ`（例 `t = 1`、`turns` が 歌唱・律動・表現 なら `"次: 律動→表現"`）
    4. `t < 1` または `t > n` のときは 2・3 を足さない
- `SetStatus(lines)`。次の順で足す:
  1. `$"体力 {state.Hp} / {state.MaxHp}　元気 {lesson.Shield}"`
  2. ステータス: 添字 `i` = 1 から `lesson.StatusValues.Count - 1` まで（添字 0 は `Nothing`。配列が短ければ、ある添字だけ見る）、値 `v = lesson.StatusValues[i]` が 0 でないものを `kind = (StatusKind)i` として `DisplayText.Status(kind) + " " + v`（`kind` が `FocusBoost`・`ImpressionBoost` のときだけ末尾に `"%"`）にし、`"　"` でつないで 1 行。1 つも無ければ行を足さない
  3. `lesson.ParamBonuses` の各要素 `b` を `$"パラメータ上昇量増加 {b.Percent}%（残り {b.RemainingTurns} ターン）"` で 1 行ずつ（0 件なら足さない）
  4. `$"スキルカード使用 残り {lesson.UsesLeft}"`
- `SetPileCounts(lesson.DrawPile.Count, lesson.Discard.Count, lesson.Exiled.Count)`
- `SetHand(cards)`: `lesson.Hand` の添字 `i` ごとに `id = lesson.Hand[i]`、`text = LessonCardText.Create(state, catalog, master, id)`、
  `enabled = lesson.UsableHand.Count == 0 || i >= lesson.UsableHand.Count || lesson.UsableHand[i]`
  （**`UsableHand` が空なら全部有効、`Hand` より短いときは範囲外を有効**。`UsableHand` が欠けていても手が止まらないようにする。使えないカードを送ってもサーバーが `PRODUCE_INVALID_ARGUMENT`（`Rejected`）で止める）→ `new HandCardViewData(id, text.Title, text.Costs, text.Description, enabled)`
- 作った `HandCardViewData` のリストをフィールド `hand` に入れる（`UseCardAsync` が使う）

`UseCardAsync(int instanceId)`（`running`・`leaving` の決まりの中）:
- `card` = `hand` のうち `InstanceId == instanceId` のもの。無い・`Enabled == false` なら何もしない
- `await dialogs.ConfirmAsync($"{card.Title}\n{card.Costs}\n{card.Description}\n\n使用しますか？", "使用", "やめる")` が `Positive` のときだけ（`disposed` を確かめてから）`await StepAsync(LessonActionType.UseCard, instanceId)`

`SkipAsync()`（同じ決まりの中）: 確認なしで `await StepAsync(LessonActionType.Skip, 0)`

`StepAsync(LessonActionType kind, int instanceId)`（`private`。呼び出し元の `running` の中で動く）:
1. `before = session.Current`、`wasExam = before.Lesson.IsExam`
2. `status = await actions.ExecuteLessonActionAsync(kind, instanceId)`（`drinkSlotIndex` は既定の 0）。`disposed` なら終わる
3. `after = session.Current`。`stillHere` = `after != null && (after.Phase == ProducePhase.Lesson || after.Phase == ProducePhase.Exam) && after.ExamResult == null && after.Lesson != null`（**`ShowCurrentAsync` に任せても同じレッスンのページが積み直されるだけの状態**。この場合はページを作り直さず、その場で描き直す）
4. 分岐（上から最初に当たったもの）:
   - `Rejected`: `await dialogs.ShowInfoAsync("そのカードは使えません。")` → `Refresh()`（`SetLastResult` は変えない）。`flow` を呼ばない（`HandleStepAsync(Rejected)` の「その操作はできません。」を重ねて出さないため）
   - `Failed`: 何もしない（共通のダイアログは `ApiCore` が出している）
   - `Succeeded` で `!wasExam` で、`after.Lesson == null` または `after.Lesson.IsExam`（**レッスンが終わった**）: `var text = LessonEndText(before, after, lessonStartHp); lessonStartHp = null;`（次のレッスン・試験の開始時の体力を取り直すため）→ `await dialogs.ShowInfoAsync(text)` のあと、`stillHere` なら（仕上げレッスンから続けて試験に入った）`Refresh()`・`SetLastResult("")`、そうでなければ `leaving = true`・`await flow.HandleStepAsync(status)`
   - `Succeeded` で `stillHere`: `Refresh()` → `SetLastResult(DiffText(before, after))`
   - `Recovered` で `stillHere`: `Refresh()` → `SetLastResult("")`（取り直した状態は、直前の操作の結果とは限らないため差を出さない）
   - それ以外（局面が変わった・試験が終わった・`SessionEnded`）: `leaving = true` → `await flow.HandleStepAsync(status)`（`Succeeded`・`Recovered` は `ShowCurrentAsync` がこのページを閉じて次の局面の画面を積む。`SessionEnded` はダイアログのあと `GoToHomeAsync`）

`DiffText(before, after)`（`private static string`。`before.Lesson`・`after.Lesson` はどちらも `null` でない）: 次の 0 でないものを `"　"` でつなぐ。全部 0 なら `"変化なし"`。
- `dGain = after.Lesson.ParamGained - before.Lesson.ParamGained` → `(after.Lesson.IsExam ? "スコア " : "獲得 ") + DisplayText.Signed(dGain)`
- `dHp = after.Hp - before.Hp` → `"体力 " + DisplayText.Signed(dHp)`
- `dShield = after.Lesson.Shield - before.Lesson.Shield` → `"元気 " + DisplayText.Signed(dShield)`

`LessonEndText(ProduceProgress before, ProduceProgress after, int? startHp)`（`private static string`。**レッスン全体の差**を出す）:
- `before` は最後の 1 操作の前の状態なので、体力の差 `after.Hp - before.Hp` は最後の操作の分しか無い。そこで体力だけはレッスン開始時の値 `startHp` と比べる。パラメータはレッスンの終わりにまとめて足される（produce.md 4.3）ので、`before` のパラメータはレッスン開始時の値と同じで、`before` との差がレッスン全体の差になる
- `var c = ProduceChange.Create(before, after);` → `var change = new ProduceChange(c.Singing, c.Rhythm, c.Expression, startHp.HasValue ? after.Hp - startHp.Value : 0);`（`startHp` が `null`（再開でレッスンの途中から入った）なら体力の行を出さない。最後の操作の分だけの差を全体の差に見せないため）
- `lines = DisplayText.Change(change)`（week。0 でないものだけ）。`lines` が空なら `"レッスン終了"`、そうでなければ `"レッスン終了\n" + string.Join("\n", lines)`

`OpenMenuAsync()`（同じ決まりの中）: `await menu.ShowAsync();`（ホームへ戻る・放棄は `ProduceMenu` が `flow.ExitToHomeAsync` を呼ぶ。ここでは `leaving` を立てない。メニューが「閉じる」で終わったときに操作を続けられるようにするため。ホームへ戻ったときはシーンごと破棄され、`disposed` が立つ）

`OpenPilesAsync()`（同じ決まりの中）: `await navigator.PushModalAsync<DeckListPresenter>(playAnimation: false);`（閉じるのを待たない。モーダルが自分で閉じる）

#### 3.4.2 `DeckListPresenter` の動き

`InitializeAsync()`: `view.CloseClicked += OnCloseClicked;` → `state = session.Current`、`lesson = state?.Lesson`:
- `lesson == null`: `SetSections(new[] { new DeckSectionViewData("レッスン中ではありません", Array.Empty<string>()) })`
- それ以外: 次の 4 つの区分を、この順で `SetSections`:

  | 区分の名前 | カード（インスタンス ID）の並び |
  |---|---|
  | 手札 | `lesson.Hand` の並びのまま |
  | 山札 | `lesson.DrawPile` をインスタンス ID の昇順（`OrderBy(x => x)`。サーバーも昇順で返すが、並べ直しても同じ） |
  | 捨札 | `lesson.Discard` を同じく昇順 |
  | 除外 | `lesson.Exiled` を同じく昇順 |

  `header` = `$"{区分の名前} {枚数} 枚"`。`rows` = 各 ID の `text = LessonCardText.Create(state, catalog, master, id)` を `$"{text.Title}　{text.Costs}\n{text.Description}"`（P3 3.5 の `DeckCardRow` の形）。0 枚の区分は `rows` = `new[] { "（なし）" }`

`CloseAsync()`（`running` の中。`leaving` は使わない）: `closed`（`bool`）が true なら何もしない。`closed = true` → `await navigator.PopModalAsync(this, playAnimation: false);`

`Dispose()`: `disposed = true`、`view.CloseClicked -= OnCloseClicked;`

#### 3.4.3 `ExamResultPresenter` の動き

フィールド: `retryAvailable`（`bool`）、`passed`（`bool`）。

`InitializeAsync()`: `view.RetryClicked += …; view.NextClicked += …;` → `state = session.Current`、`result = state?.ExamResult`:
- **`result == null`**（通常は来ない。`ProduceFlow` は `ExamResult != null` のときだけこのページを出す: 3.5）: **`flow` を呼ばない**。`SetTitle("試験結果")`、`SetRanking(空のリスト)`、`SetOutcome("結果がありません。")`、`SetButtons(null, "次へ")`、`retryAvailable = false`。`NextAsync` が下の「結果を見せるだけ」の経路で進める
- それ以外:
  - `SetTitle($"{DisplayText.ExamKindName(result.Kind)} 結果（{result.Attempt + 1} 回目）")`（**`Attempt` は 0 始まり**: produce.md 5.1.1、glossary「挑戦回数」、`ProduceEngine` が `state.Attempt` をそのまま入れる。初回は「1 回目」）
  - `SetRanking`: `ExamRanking.Build(result.Score, result.NpcScores)` の各行を、NPC の行に上から 1, 2, … と番号 `k` を振って `new ExamRankingRowViewData(text, row.IsPlayer)`。`text` = 自分の行は `$"{row.Rank}位　あなた　{DisplayText.Thousands(row.Score)} ◀"`、NPC の行は `$"{row.Rank}位　ライバル {k}　{DisplayText.Thousands(row.Score)}"`
  - `passed = result.Passed`。`SetOutcome(passed ? "合格！" : "不合格")`
  - `retryAvailable = state.Phase == ProducePhase.ExamResult`（再挑戦が残り、入力を待つ局面。produce.md 5.1.1）
  - `retryAvailable` なら `SetButtons($"再挑戦（残り {state.ExamRetryLeft}）", passed ? "次へ" : "プロデュース終了")`、そうでなければ `SetButtons(null, "次へ")`

`RetryAsync()`（`running`・`leaving` の決まりの中）: `retryAvailable` が false なら何もしない。`await dialogs.ConfirmAsync("試験を最初からやり直します。体力・デッキは試験の開始前に戻ります。よろしいですか？")` が `Positive` なら `status = await actions.DecideExamRetryAsync(true)`:
- `Rejected`: `await dialogs.ShowInfoAsync("その操作はできません。")`。`Failed`: 何もしない
- それ以外: `leaving = true` → `await flow.HandleStepAsync(status)`（応答は局面 `Exam`・`ExamResult == null`。`ShowCurrentAsync` がこのページを閉じてレッスンのページを積む）

`NextAsync()`（同じ決まりの中）:
- `retryAvailable` のとき: 合格なら確認なし、不合格なら `ConfirmAsync("プロデュースを終了します。評価と報酬を受け取ります。よろしいですか？")` が `Positive` のときだけ、`status = await actions.DecideExamRetryAsync(false)` → `Rejected`・`Failed` は `RetryAsync` と同じ、それ以外は `leaving = true` → `await flow.HandleStepAsync(status)`
- そうでないとき（結果を見せるだけ。`result == null` の場合も）: `session.ExamResultShown = true;` → `leaving = true` → `await flow.ShowCurrentAsync();`（`ShowCurrentAsync` がこのページを閉じ、今の局面の画面を積む。`ExamResultShown` が true なので試験結果は先出しされない: 3.5）

#### 3.4.4 `ProduceResultPresenter` の動き

`InitializeAsync()`: `view.HomeClicked += …;` → `state = session.Current`、`result = state?.Result`:
- `result == null`: `SetRank("-")`、`SetLines(new[] { "結果はありません。" })`
- それ以外:
  - `SetRank($"ランク {DisplayText.Rank(result.Rank)}")`
  - `SetLines`: 次の順（括弧の条件に合うものだけ）
    1. `result.Ending == ProduceEndingKind.Completed` なら `"プロデュース完了"`、それ以外は `"プロデュース終了"`（放棄は `ProduceMenu` から直接ホームへ戻るので、この画面には来ない。来ても止めない）
    2. `$"評価値 {DisplayText.Thousands(result.EvaluationValue)}"`
    3. （`result.FinalRank > 0` のとき。0 は最終試験を受けていない: `ProduceFormulas.Evaluate`）`$"最終試験 {result.FinalRank}位　スコア {DisplayText.Thousands(result.FinalScore)}"`
    4. （`DisplayText.ExamOutcomeText(result.ExamOutcome)` が空でないとき）その文字
    5. `$"歌唱 {DisplayText.Thousands(state.Singing)} / 律動 {DisplayText.Thousands(state.Rhythm)} / 表現 {DisplayText.Thousands(state.Expression)}"`
    6. `"報酬"`
    7. `$"ガチャ通貨 +{DisplayText.Thousands(result.GachaCurrency)}"`
    8. `$"ゴールド +{DisplayText.Thousands(result.Gold)}"`
    9. `$"サポート強化pt +{DisplayText.Thousands(result.SupportPoints)}"`
    10. `$"経験値 +{DisplayText.Thousands(result.Exp)}　（プレイヤーLv {result.PlayerLevel}）"`

`GoHomeAsync()`（`running`・`leaving` の決まりの中）: `leaving = true` → `session.Clear();` → `await flow.ExitToHomeAsync();`（formation 3.4: `ISceneNavigator.GoToHomeAsync` で `Home` シーンを読み込み直す。この Presenter はシーンごと破棄される）
- **この画面には戻れない**: 戻るボタンは無い（P3 3.15）。報酬はサーバーで付与済み（局面 `Finished` の応答を受けたとき formation の `ProduceActions` が `RefreshPlayerUseCase` で所持品を取り直している）ので、もう一度押せなくても失うものは無い。サーバーのセッションは `Finished` で終わっていて、`session` も空なので、ホームの「再開」は出ない（formation の決めたこと 13）
- 連打は `running`・`leaving` で 1 回だけにする（`ExitToHomeAsync` を 2 回呼ばない）

### 3.5 `ProduceFlow` に足すもの

**先に formation の `ProduceFlow.cs` を読み、この節で使うフィールドの名前が `session`（`ProduceSession`）・`navigator`（`IScreenNavigator`）・`dialogs`（`DialogService`）であることを確かめる。違えば止まって報告する**（名前を読み替えて進めない。week が足した `case` も同じフィールドを使っているはず）。

formation 3.4 の `ProduceFlow.PushForPhaseAsync(ProducePhase phase)`（`ShowCurrentAsync` が背景より上を全部閉じてから呼ぶ。`switch (phase)` で、各 `case` は `await navigator.Push…Async<…>(playAnimation: false); return;`）に、week が足した `case`（`WeekSelect`・`CardOffer`）のほかに次を足す。`ShowCurrentAsync`・`HandleStepAsync`・`ExitToHomeAsync` と `default`（「この局面の画面は準備中です」）は変えない。

1. **`switch` の前**（メソッドの先頭）: `var state = session.Current;` が `null` でなく、`state.ExamResult != null && !session.ExamResultShown` なら、`await navigator.PushPageAsync<ExamResultPresenter>(playAnimation: false); return;`（局面が何であっても先に試験結果を見せる。再挑戦が残らず `ExamResult` の局面を飛ばした応答を含む: screen-design.md 7.6）
2. `switch` に足す `case`:
   - `case ProducePhase.Lesson:`・`case ProducePhase.Exam:` → `await navigator.PushPageAsync<LessonPresenter>(playAnimation: false); return;`
   - `case ProducePhase.ExamResult:` → **ページを出さない**。ここに来るのは `ExamResult == null` のとき（1. で処理済みのため）。`await dialogs.ShowInfoAsync("試験の結果を取得できませんでした。ホームから再開してください。"); await ExitToHomeAsync(); return;`（セッションはサーバーに残り、ホームの「再開」の `GetState` は `LastExam` を返すので、再開すれば `ProduceEntry` → `ShowCurrentAsync` で試験結果が出る）
   - `case ProducePhase.Finished:` → `await navigator.PushPageAsync<ProduceResultPresenter>(playAnimation: false); return;`

これで formation の「準備中」の分岐は、`Nothing` と未知の値だけに残る。`ProduceEntry` は `session.HasActive`（`Finished` は含まない）が false ならホームへ戻すので、`Finished` の結果のページは操作の後（`HandleStepAsync` → `ShowCurrentAsync`）からだけ出る。

### 3.6 View の実装（P3 の骨格に足す）

P3 3.1 のフィールド宣言は変えない（名前・型・`[SerializeField]`）。足すのはメソッド・イベント・インターフェースだけ。フィールドと IF のメンバーの対応は P3 3.2 の表のとおり。ボタンの `onClick` は `Awake` で購読して C# の `event` を起こす（foundation 3.3.3。解除は書かない）。行は `UiObjects.ClearChildren`・`UiObjects.AddRow`（foundation 3.2）で作り直す。文字の大きさ・フォントは Prefab のまま（C# は文字・表示/非表示・押せるかだけを変える。例外は `ExamRankingRowView.SetRow` の色: 決めたこと 8）。`using System; using System.Collections.Generic; using TMPro; using UnityEngine; using Uta.Client.Presentation.Common; using Uta.Client.Presentation.Common.Parts;`（使うものだけ）。

**行の View**（P3 3.1 の骨格に足す）:

| 型 | 足すメンバー |
|---|---|
| `SkillCardRowView` | 足さない。week が足した `public void SetTexts(string title, string costs, string description)` を使う（無い・シグネチャが違えば止まって報告する） |
| `DeckSectionHeaderRowView` | `public void SetText(string text)`（`headerText.text = text ?? ""`） |
| `DeckCardRowView` | `public void SetText(string text)`（`this.text.text = text ?? ""`） |
| `ExamRankingRowView` | `public void SetRow(string text, bool isPlayer)`（`this.text.text = text ?? ""; this.text.color = isPlayer ? UiTheme.Accent : UiTheme.Text;`。`background` は触らない） |

**`LessonPage : PageBase, ILessonView`**:
- イベント: `public event Action<int> CardClicked; public event Action SkipClicked; public event Action MenuClicked; public event Action PilesClicked;`
- `private void Awake()`: `skipButton.Button.onClick.AddListener(() => SkipClicked?.Invoke());`、`menuButton.Button.onClick.AddListener(() => MenuClicked?.Invoke());`、`pilesButton.Button.onClick.AddListener(() => PilesClicked?.Invoke());`
- `SetHeader(title, turnText)`: `header.SetTitle(title);` `this.turnText.text = turnText;`
- `SetProgress(lines)`: `progressText.text = string.Join("\n", lines);`。`SetStatus(lines)`: `statusText.text = string.Join("\n", lines);`
- `SetLastResult(text)`: `lastResultText.text = text ?? "";`
- `SetPileCounts(drawPile, discard, exiled)`: `pilesButton.SetLabel($"山札 {drawPile}　捨札 {discard}　除外 {exiled}");`
- `SetHand(cards)`: `UiObjects.ClearChildren(handContent);` のあと、添字 `i` ごとに `var c = cards[i]; var row = UiObjects.AddRow(handRowPrefab, handContent, "Card" + i); row.SetTexts(c.Title, c.Costs, c.Description); row.SetInteractable(c.Enabled); var id = c.InstanceId; row.Button.onClick.AddListener(() => CardClicked?.Invoke(id));`（ラムダはループの中のローカル変数 `id` を使う）

**`DeckListModal : ModalBase, IDeckListView`**:
- `public event Action CloseClicked;`、`Awake`: `closeButton.Button.onClick.AddListener(() => CloseClicked?.Invoke());`
- `SetSections(sections)`: `UiObjects.ClearChildren(listContent);` のあと、区分の添字 `s` ごとに `UiObjects.AddRow(sectionHeaderPrefab, listContent, "Section" + s).SetText(sections[s].Header);`、続けて行の添字 `r` ごとに `UiObjects.AddRow(cardRowPrefab, listContent, "Row" + s + "_" + r).SetText(sections[s].Rows[r]);`

**`ExamResultPage : PageBase, IExamResultView`**:
- `public event Action RetryClicked; public event Action NextClicked;`、`Awake`: `retryButton.Button.onClick.AddListener(() => RetryClicked?.Invoke());`、`nextButton.Button.onClick.AddListener(() => NextClicked?.Invoke());`
- `SetTitle(text)`: `titleText.text = text;`。`SetOutcome(text)`: `outcomeText.text = text;`
- `SetRanking(rows)`: `UiObjects.ClearChildren(rankingContent);` のあと、添字 `i` ごとに `UiObjects.AddRow(rankingRowPrefab, rankingContent, "Rank" + i).SetRow(rows[i].Text, rows[i].IsPlayer);`
- `SetButtons(retryLabel, nextLabel)`: **ボタンの文字は引数をそのまま使う**。`retryButton.gameObject.SetActive(retryLabel != null);`、`retryLabel != null` なら `retryButton.SetLabel(retryLabel);`。`nextButton.SetLabel(nextLabel);`

**`ProduceResultPage : PageBase, IProduceResultView`**:
- `public event Action HomeClicked;`、`Awake`: `homeButton.Button.onClick.AddListener(() => HomeClicked?.Invoke());`
- `SetRank(text)`: `rankText.text = text;`。`SetLines(lines)`: `linesText.text = string.Join("\n", lines);`

View の EditMode のテストは書かない（foundation 3.3.7。参照の欠けは P1 の `PrefabContractTests`、子の名前は P3 の `ProducePrefabTests`。表示は Play で見る）。

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行）でコンパイルエラー 0、そのタスクまでの EditMode のテストが通る状態にする。EditMode のテストは `unity test --mode EditMode`（Editor が要る。回し方は P1 タスク 5 の「テストの回し方」）。**`unity test` が動かなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない。CLAUDE.md に足すのは動いたときだけ**（P1 の全計画共通の規則）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。Play のあとでコミットする前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`（P1 の `FontAssets`）を実行する（foundation 3.3.7）。

### タスク 1: 順位表と文字

触る: `Application/Produce/ExamRanking.cs`、`Presentation/Common/DisplayText.cs`、`Tests/EditMode/ExamRankingTests.cs`・`DisplayTextLessonTests.cs`。
手順: 3.1・3.2 のとおり。
境界: `npcScores` が `null`・空（自分の 1 行だけ、順位 1）。同点（自分が先）。`StatusKind` の表に無い値（`(StatusKind)99` → `"99"`）。
確かめ方: `ExamRankingTests`・`DisplayTextLessonTests`。

### タスク 2: レッスン・試験の画面と、山札などの一覧

触る: `Presentation/Produce/LessonCardText.cs`・`HandCardViewData.cs`・`ILessonView.cs`・`LessonPage.cs`・`LessonPresenter.cs`・`DeckSectionViewData.cs`・`IDeckListView.cs`・`DeckListModal.cs`・`DeckListPresenter.cs`・`DeckSectionHeaderRowView.cs`・`DeckCardRowView.cs`、`ProduceFlow.cs`（3.5 の `Lesson`・`Exam` の `case` だけ）、`Tests/EditMode/Support/LessonTestData.cs`、テスト。
手順:
1. 1 章の前提（P3 の骨格・formation・week）を確かめる。`SkillCardRowView.SetTexts`（week）と `FakeProduceFlow.ShowCurrentAsync` の上書き（week 5.1）があることを確かめる。無ければ止まって報告する
2. 3.3・3.4.1・3.4.2・3.5（`Lesson`・`Exam`）・3.6（`LessonPage`・`DeckListModal`・行の View 3 つ）のとおり作る。Presenter・画面の登録はしない（foundation 3.3.2。Addressables は P3 が登録済み）
境界: `UsableHand` が空・`Hand` より短い。`StatusValues` が 13 より短い（ある添字だけ見る）。`ExamTurns` が空（試験でない）・`Turn` が 0・`Turn == ExamTurns.Count`（最後のターン。「次:」を出さない）。`Deck` に無いインスタンス ID（"カード？"）。
確かめ方: `LessonCardTextTests`・`LessonPresenterTests`・`DeckListPresenterTests`、`ProduceFlowTests`（`Lesson`・`Exam` の追記分）。

### タスク 3: 試験結果・プロデュース結果の画面

触る: `Presentation/Produce/ExamRankingRowViewData.cs`・`IExamResultView.cs`・`ExamResultPage.cs`・`ExamResultPresenter.cs`・`ExamRankingRowView.cs`・`IProduceResultView.cs`・`ProduceResultPage.cs`・`ProduceResultPresenter.cs`、`ProduceFlow.cs`（3.5 の 1. と `ExamResult`・`Finished`）、テスト。
手順: 3.4.3・3.4.4・3.5・3.6（`ExamResultPage`・`ProduceResultPage`・`ExamRankingRowView`）のとおり。
境界: `ExamResult == null` でページが出た（`flow` を呼ばない）。`Result == null`。ボタンの連打（`leaving` で 1 回だけ）。
確かめ方: `ExamResultPresenterTests`・`ProduceResultPresenterTests`、`ProduceFlowTests`（残りの追記分）。

### タスク 4: Play モードの通し

Editor の Play モードで、ローカルのサーバー（CLAUDE.md の「ビルド・テスト・実行」のローカルのサーバー。初期付与済み）に対して、5.3 の手動の手順を行う。Play モードの開始・停止とコンソールの読み出しは [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章のコマンド（`unity command editor.playmode start|stop`、`unity command console get --tail N --level error --format json`）を使う。確かめられなかった項目と、見つけた不具合は、この計画の範囲内なら直し、範囲外（Prefab の見た目は P3、`ProduceFlow` の共通の処理は formation）なら直さずに報告する。

### タスク 5: Windows の通し（完了条件 6）と記録

1. Windows の IL2CPP のビルド（CLAUDE.md の「ビルド・テスト・実行」。Editor を閉じてから。Addressables の `BuildPlayerContent` は P1 が `BuildCommands` に入れたもの）を `-batchmode` なしで起動し、**5.3 の通し A（起動 → ガチャ → 強化 → 編成 → 最短シナリオを最後まで → 結果 → ホーム）と通し B（初級を放棄せずに最後まで → 結果 → ホーム。中間試験の 1 回目はスキップだけで不合格にして再挑戦する）**を手で行う（**完了条件 6**。1.3 の 2 つがそろったら満たす）。結果を [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 2章の表に 1 行足す（項目: 「プロデュースの画面（レッスン・試験・試験結果・結果）を Windows の IL2CPP で最後まで通した。`LessonDto`（`bool[]` の `UsableHand` を含む入れ子）・`ExamResultDto`・`ProduceResultResponse` の MessagePack の送受信を含む」、方法: 「手動」、日付）。通らなかった所は、その表の結果の列にそのまま事実を書く（通し B が中間試験の不合格で終わったときは「初級は中間試験の不合格で終了（最終試験は未到達）」と書く）。**完了条件 7（Android）は確かめない**
2. [produce-server.md](../wiki/architecture/server/produce-server.md) 6章: `ExamResultDto` の行の `Attempt` を `Attempt`（挑戦回数。0 始まり）に、`LessonDto` の行の `ExamTurns`(`ExamTurnDto[]`。試験のときだけ) を `ExamTurns`(`ExamTurnDto[]`。試験のときだけ。今のターンは添字 `Turn - 1`) に直す
3. [screen-design.md](../wiki/architecture/screen-design.md)（frontmatter の `updated` を実施日に）:
   - 2章の表の下の「山札・捨札の確認（PRD-08）は、レッスン中に開くモーダルとして持つ案」を「山札・捨札・除外の確認（PRD-08）は、レッスンのページのボタンから開くモーダル（`DeckListModal`）[2026-11-03]」に直す
   - 6章の 1 つ目の項目（「効果エンジンで使用可否の判定と予測値の表示をする」）の末尾に「（MVP は下の項目のとおり、使用可否はサーバーの `UsableHand`、予測値は出さない）」を足す
   - 「実装メモ」に、この計画で設計と違ったところを書く（無ければ「順 7 の C# の 6 本目: なし」）
4. 順 7 の完了の記録（**1 で通し A・通し B の両方が通り（1.3。game-spec の完了条件 6 の文と同じ範囲）、かつ [索引](2026-11-07-client-screens-index.md) に載っている、この計画以外の全部の計画（計画 0・P1〜P3・C# の 1〜5 本目）が `done` のときだけ**。どれかが欠ければ 4 を飛ばし、何が残っているかを報告する）:
   - [game-spec.md](../wiki/overview/game-spec.md)（frontmatter の `updated` を実施日に）の「実装計画の単位と順」の表の順 7 の行の 2 列目（実装計画）の先頭に `**done**（<1 の日付>。完了条件 6（最短シナリオと初級の通し）を Windows の IL2CPP で確かめた）` を足す（ほかの文とリンクはそのまま。日付は `YYYY-MM-DD`）
   - 同じ行の 3 列目（受け持つ完了条件）が `6` だけになっていることを確かめる（7 を別の項目に移すのは進行役が行う）。`6・7…` のままなら、done にせずに止まって報告する
   - 索引のこの計画の行に状態の列があれば `done` にする（無ければ索引は触らない）
5. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
6. この計画の `status` を `done`、`updated` を実施日にする（4 を行ったときだけ。行えなかったときは `in-progress` のままにして報告する）

## 5. テスト

すべて EditMode（`Tests/EditMode/`。名前空間 `Uta.Client.Infrastructure.Tests`）。書き方は foundation 3.3.7（偽の View はテストのファイルの中に `private sealed class FakeXxxView`。`CallLog` を共有し、記録は `"<メソッド名>:<引数を , でつないだもの>"`。リストの引数は要素を `"|"` でつなぐ。要素が型のリストは、要素ごとに下の形にしてから `"|"` でつなぐ: `SetHand` は `"<InstanceId>/<Title>/<Costs>/<Description>/<Enabled>"`（例 `"SetHand:1/まっすぐ一声 [N] アクティブ/消費 4/パラメータ+9/True|2/はずむ足取り [N] アクティブ/消費 3/パラメータ+2、元気+2/False"`。偽は最後に渡されたリストを `LastHand` に持つ）、`SetSections` は `"<Header>:<Rows を ; でつなぐ>"`、`SetRanking` は `"<Text>/<IsPlayer>"`。`string` の `null` は空文字で記録する。非同期は `[UnityTest]` + `UniTask.ToCoroutine` で Presenter の公開の `UniTask` メソッドを `await`）。

### 5.1 テスト用の部品

- `Tests/EditMode/Support/LessonTestData.cs`（`internal static class LessonTestData`）:
  - `MemoryDatabase Db()`: `MasterDataLoader.Load(File.ReadAllBytes(UnityEngine.Application.streamingAssetsPath + "/" + MasterDataLoader.RelativePath))`（`MasterCardCatalogTests.ReadBundled` と同じ読み方。同梱のマスタを使う）
  - `ICardCatalog Catalog()`: `MasterCardCatalog.Create(Db())`。`IGameMaster Master()`: `new MasterGameData(Db())`
  - `ProduceLesson Lesson(LessonKind kind = LessonKind.Normal, ParamKind target = ParamKind.Rhythm, int maxTurns = 6, int turn = 1, int paramGained = 0, int shield = 0, int[] hand = null, bool[] usableHand = null, int[] drawPile = null, int[] discard = null, int[] exiled = null, int[] statusValues = null, int[] lessonEnhanced = null, ProduceExamTurn[] examTurns = null, int[] npcScores = null, int usesLeft = 1)`: 渡さなかった配列は空。`clearTarget = 85`、`perfectTarget = 170`、`paramBonuses`・`persistents` は空。`new ProduceLesson(...)` に formation 3.2 の引数の順で渡す
  - `ProduceProgress State(ProducePhase phase, ProduceLesson lesson = null, ProduceExamResult examResult = null, ProduceOutcome result = null, int hp = 18, int maxHp = 30, int examRetryLeft = 3, ProduceDeckCard[] deck = null, int operationNumber = 1, int singing = 100, int rhythm = 200, int expression = 300, int week = 1)`: `new ProduceProgress(operationNumber, phase, 1, 1, week, singing, rhythm, expression, hp, maxHp, examRetryLeft, deck ?? 既定, Array.Empty<ProduceWeekAction>(), Array.Empty<ProduceClassOption>(), Array.Empty<int>(), lesson, examResult, result)`（formation 3.2 の引数の順: scenarioId・idolId は 1。**パラメータ（歌唱・律動・表現）と週は引数で渡せる**。既定は歌唱 100・律動 200・表現 300・週 1。`TestProduce.Progress` は `examRetryLeft` を変えられないので使わない）。既定の `deck` は `new[] { new ProduceDeckCard(1, 1, false), new ProduceDeckCard(2, 2, false), new ProduceDeckCard(3, 1, true) }`（インスタンス 1 = カード 1、2 = カード 2、3 = カード 1 の強化済み）
  - 応答の写しは formation 3.2 のコンストラクターで直接作る（補助を作らない）。この計画のテストで使う値:
    - 試験結果 `new ProduceExamResult(ExamKind.Mid, 0, 1388, 2, new[] { 1440, 1170, 900 }, true)`（`ExamResultPresenterTests` の既定。テストごとに `attempt`・`passed` だけを変える）
    - プロデュースの結果（完了）`new ProduceOutcome(ProduceEndingKind.Completed, ExamOutcome.FinalPassed, 12345, ProduceRank.APlus, 3456, 1, 300, 1500, 40, 120, 5, 30)`（引数の順は `ending, examOutcome, evaluationValue, rank, finalScore, finalRank, gachaCurrency, gold, supportPoints, exp, playerLevel, playerExp`）と、`State(ProducePhase.Finished, result: それ, singing: 1000, rhythm: 862, expression: 745)`
    - プロデュースの結果（中間不合格）`new ProduceOutcome(ProduceEndingKind.Completed, ExamOutcome.MidFailed, 2100, ProduceRank.C, 0, 0, 100, 500, 10, 60, 3, 10)` と、`State(ProducePhase.Finished, result: それ)`
- 使うカード（同梱のマスタ。計画 0 の 3.4 と lesson-card.md 14.1）: カード 1 = 「まっすぐ一声」N・アクティブ、通常は消費 4・"パラメータ+9"、強化は消費 3・"パラメータ+14"。カード 2 = 「はずむ足取り」N・アクティブ、消費 3・"パラメータ+2、元気+2"。期待値の文字は week の `DisplayText.Card`・`Costs` の書式（"まっすぐ一声 [N] アクティブ"・"消費 4"）で書く
- formation の `FakeProducePort`（記録 `"ExecuteLessonAction:<op>,<kind>,<id>,<drink>"`・`"DecideExamRetry:<op>,<retry>"`・`"GetState"`）、week の `FakeProduceFlow`、foundation の `FakeScreenNavigator`・`FakeDialogService`・`FakeSceneNavigator`・`CallLog` を使う。`ProduceActions` は本物を `new ProduceActions(fakePort, session, new RefreshPlayerUseCase(new FakePlayerPort(log), new PlayerCache()))` で作る（`RefreshPlayerUseCase` は局面 `Finished`・`SessionEnded` のときだけ呼ばれる。この計画の Presenter のテストでは応答をその局面にしない）
- `FakeProduceFlow`（week。`ProduceFlow` の派生で base を呼ばない）の記録は `"Flow.HandleStep:<status>"`・`"Flow.ShowCurrent"`・`"Flow.ExitToHome"` と書く。week 5.1 の版は `ShowCurrentAsync`・`ExitToHomeAsync`・`HandleStepAsync` を上書きし、この書式で記録する（2026-11-07 に照合済み。この計画は `FakeProduceFlow` を変えない）
- `ProduceMenu` の偽は `LessonPresenterTests.cs` の中に `private sealed class FakeProduceMenu : ProduceMenu`（`base(null, null, null)`。`ShowAsync` で `log.Add("Menu.Show")` して完了済みの `UniTask` を返す）
- `ProduceFlowTests` の追記分は formation 5章のとおり本物の `ProduceFlow` を `new ProduceFlow(session, navigator, new PageStack(() => 2, () => 0), dialogs, scenes)`（背景とその上のページ 1 枚）で作り、`ShowCurrentAsync()` を `await` する

### 5.2 テストケース

| ファイル | テスト | 内容 |
|---|---|---|
| `ExamRankingTests.cs` | `Build_SortedDescending_PlayerFirstOnTies` | `Build(100, {150, 100, 50})` → (1, NPC, 150)、(2, 自分, 100)、(3, NPC, 100)、(4, NPC, 50) |
| | `Build_NpcNull_OnlyPlayer` | `Build(10, null)` → 1 行、順位 1 |
| | `Build_PlayerRank_EqualsExamRank` | スコアの組 3 つ（`(0, {0,0,0})`・`(500, {900, 600, 300})`・`(1000, {1000, 999})`）で、自分の行の `Rank == ProduceFormulas.ExamRank(...)` |
| | `CurrentRank_CountsStrictlyGreaterNpcs` | `CurrentRank(100, {150, 100, 50}) == 2` |
| `DisplayTextLessonTests.cs` | `LessonKindName_AllValues` / `Status_AllValues`（3.2 の表の 12 個と `Nothing`、`(StatusKind)99 → "99"`）/ `Rank_PlusSuffix`（`CPlus → "C+"`、`SSPlus → "SS+"`）/ `ExamOutcomeText_AllValues` / `ExamKindName_AllValues` / `Thousands_Groups`（`1234 → "1,234"`、`0 → "0"`）/ `Signed_Values`（`3 → "+3"`、`0 → "+0"`、`-2 → "-2"`） | |
| `LessonCardTextTests.cs` | `Create_Normal_UsesNameRarityTypeCostsDescription` | インスタンス 1 → Title `"まっすぐ一声 [N] アクティブ"`、Costs `"消費 4"`、Description `"パラメータ+9"` |
| | `Create_DeckEnhanced_UsesEnhancedFace` | インスタンス 3 → Title `"まっすぐ一声+ [N] アクティブ"`、Costs `"消費 3"`、Description `"パラメータ+14"` |
| | `Create_LessonEnhanced_UsesEnhancedFace` | インスタンス 1 で `lessonEnhanced = {1}` → `"まっすぐ一声+ …"` |
| | `Create_NotInDeck_Placeholder` | インスタンス 99 → Title `"カード？"`、Costs・Description が `""` |
| `LessonPresenterTests.cs` | `Initialize_Lesson_ShowsHeaderProgressStatus` | `Lesson(Special, Rhythm, maxTurns 6, turn 3, paramGained 62, shield 7, statusValues 長さ 13 で [1] = 3・[4] = 50)`、`hp 18 / 30` → `"SetHeader:特別レッスン（律動）,残り 4 / 6"`、`"SetProgress:獲得 62　クリア目標 85　パーフェクト 170"`、`"SetStatus:体力 18 / 30　元気 7|好調 3　集中強化 50%|スキルカード使用 残り 1"`、`"SetLastResult:"` |
| | `Initialize_Hand_EnabledFollowsUsableHand` | `hand {1, 2}`、`usableHand {true, false}` → `"SetHand:1/まっすぐ一声 [N] アクティブ/消費 4/パラメータ+9/True|2/はずむ足取り [N] アクティブ/消費 3/パラメータ+2、元気+2/False"` |
| | `Initialize_UsableHandEmpty_AllEnabled` | `usableHand` 空 → 全部 `Enabled` |
| | `Initialize_UsableHandShorter_OutOfRangeEnabled` | `hand {1, 2, 3}`、`usableHand {false}` → `false, true, true` |
| | `Initialize_Exam_ShowsScoreRankAndTurns` | `Lesson(MidExam, turn 2, maxTurns 4, paramGained 1240, examTurns {歌唱 100%, 律動 285%, 表現 150%, 歌唱 200%}, npcScores {1500, 1000, 900})` → `"SetHeader:中間試験,残り 3 / 4"`、`SetProgress` が `"スコア 1,240　順位 2 位"`・`"このターン: 律動（ボーナス 285%）"`（添字 1 = `Turn - 1`）・`"次: 表現→歌唱"` の 3 行 |
| | `Initialize_ExamLastTurn_NoNextLine` | `turn == examTurns.Count` → 「このターン」はあり、「次:」の行が無い |
| | `Initialize_ExamTurnZero_NoTurnLines` | `turn 0` → スコアの行だけ |
| | `Initialize_PileCounts` | `drawPile {4,5,6}`、`discard {7}`、`exiled 空` → `"SetPileCounts:3,1,0"` |
| | `UseCard_Confirmed_CallsActionAndShowsDiff` | 応答が局面 `Lesson`（`paramGained` +23、`hp` -4、`shield` +2）→ `log` に `"ExecuteLessonAction:1,UseCard,1,0"`、`log` の最後が `"SetLastResult:獲得 +23　体力 -4　元気 +2"`。確認の文字は `"Confirm:まっすぐ一声 [N] アクティブ\n消費 4\nパラメータ+9\n\n使用しますか？"`。`"Flow."` の記録が無い（ページを作り直さない） |
| | `UseCard_NoChange_ShowsNoChange` | 応答の差が 0 → `"SetLastResult:変化なし"` |
| | `UseCard_Disabled_DoesNothing` | `Enabled == false` のカード → 確認も操作も無い |
| | `UseCard_Declined_DoesNothing` | `ConfirmResults` に `Negative` → 操作が呼ばれない |
| | `Skip_NoConfirm_CallsSkip` | `log` に `"Confirm:"` が無く、`"ExecuteLessonAction:1,Skip,0,0"` がある |
| | `Step_Rejected_ShowsInfoAndRefreshes` | `StateResults` に `PortResult<ProduceProgress>.BusinessError(ServerReasons.ProduceInvalidArgument, "x")` を 1 つだけ入れる（操作の応答。取り直しは無い）→ `"Info:そのカードは使えません。"` のあと `SetHand` がもう一度記録され、`Flow.` の記録が無い |
| | `Step_Recovered_RefreshesWithoutDiff` | `StateResults` に順に 2 つ入れる: 1 つ目 `PortResult<ProduceProgress>.BusinessError(ServerReasons.ProduceStaleOperation, "x")`（操作の応答）、2 つ目 `PortResult<ProduceProgress>.Success(State(Lesson, Lesson(Normal, Rhythm, turn: 2, hand: {1}), operationNumber: 2))`（`GetState` の応答。局面 `Lesson` のまま）→ `"GetState"` が記録され、 `"SetLastResult:"`、`Flow.` の記録が無い |
| | `Step_LessonEnded_ShowsChangeThenHandlesStep` | **体力はレッスン開始時との差**。最初の状態 `State(Lesson, Lesson(Normal, Rhythm, turn: 1, hand: {1}), hp: 18, rhythm: 200)` で `InitializeAsync`（開始時の体力 18 を保持）。応答 1: `State(Lesson, Lesson(Normal, Rhythm, turn: 2, hand: {1}), hp: 14, rhythm: 200, operationNumber: 2)` → `SkipAsync()`（その場で描き直す）。応答 2: `State(WeekSelect, lesson: null, hp: 20, rhythm: 262, operationNumber: 3)` → `SkipAsync()`。`log` の後ろの並び（2 回目の操作から）が `"ExecuteLessonAction:2,Skip,0,0"`, `"Info:レッスン終了\n律動 +62\n体力 +2"`（20 − 18。最後の操作の前の 14 との差 +6 ではない）, `"Flow.HandleStep:Succeeded"` |
| | `Step_LessonEnded_ResumedMidLesson_OmitsHp` | 最初の状態が `turn: 3`（再開で途中から。開始時の体力を持たない）、`hp: 14`。応答 `State(WeekSelect, lesson: null, hp: 20, rhythm: 262, operationNumber: 2)` → `SkipAsync()` → `"Info:レッスン終了\n律動 +62"`（体力の行が無い）, `"Flow.HandleStep:Succeeded"` |
| | `Step_LessonToExam_ShowsChangeAndRefreshesInPlace` | 仕上げレッスン → 応答が局面 `Exam`（`Lesson.IsExam`、`ExamResult == null`）→ `"Info:レッスン終了…"` のあと `SetHeader` が `"最終試験"` で記録され、`Flow.` の記録が無い |
| | `Step_ExamResultPresent_HandlesStepWithoutInfo` | 試験中、応答が局面 `Exam` でも `ExamResult != null` → `"Info:"` が無く `"Flow.HandleStep:Succeeded"` |
| | `Step_AfterLeaving_IgnoresClicks` | `HandleStep` のあと `SkipAsync` をもう一度呼んでも `"ExecuteLessonAction:"` は 1 回だけ |
| | `Step_Exception_ShowsError` | `FakeProducePort.StateResults` を空にし、`SkipAsync` を `await` する前に `LogAssert.Expect(LogType.Exception, new Regex("FakeProducePort: no state result"));`（formation 5章の `Start_Exception_ShowsError`・week 5.2 と同じ書き方。`LogAssert.ignoreFailingMessages` は使わない）→ `"Info:エラーが発生しました: FakeProducePort: no state result"`、`Flow.` の記録が無い。続けて応答を入れて `SkipAsync` を呼ぶと操作が呼ばれる（`running`・`leaving` が戻っている） |
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
| | `Retry_Confirmed_CallsDecideTrueThenHandlesStep` / `Retry_Declined_DoesNothing` | 前者は `"DecideExamRetry:1,True"`, `"Flow.HandleStep:Succeeded"`。後者は `"DecideExamRetry:"` が無い |
| | `Next_RetryAvailablePassed_DecideFalseWithoutConfirm` | `"Confirm:"` が無く、`"DecideExamRetry:1,False"`, `"Flow.HandleStep:Succeeded"` |
| | `Next_RetryAvailableFailed_ConfirmsThenDecideFalse` | `"Confirm:プロデュースを終了します。…"` のあと `"DecideExamRetry:1,False"` |
| | `Next_NoRetry_MarksShownAndShowsCurrent` | `session.ExamResultShown == true`、`"Flow.ShowCurrent"` が 1 回（`"DecideExamRetry:"` が無い） |
| | `Next_Twice_ShowsCurrentOnce` | `NextAsync` を 2 回 → `"Flow.ShowCurrent"` が 1 回 |
| `ProduceResultPresenterTests.cs` | `Initialize_Completed_ShowsRankAndRewards` | 5.1 の「完了」の `ProduceOutcome` と状態 → `"SetRank:ランク A+"`、`"SetLines:プロデュース完了|評価値 12,345|最終試験 1位　スコア 3,456|最終試験に合格|歌唱 1,000 / 律動 862 / 表現 745|報酬|ガチャ通貨 +300|ゴールド +1,500|サポート強化pt +40|経験値 +120　（プレイヤーLv 5）"`（10 行。`　` は全角の空白） |
| | `Initialize_MidFailed_NoFinalLine` | 5.1 の「中間不合格」の `ProduceOutcome` と状態 → `"SetRank:ランク C"`、`"SetLines:プロデュース完了|評価値 2,100|中間試験で不合格|歌唱 100 / 律動 200 / 表現 300|報酬|ガチャ通貨 +100|ゴールド +500|サポート強化pt +10|経験値 +60　（プレイヤーLv 3）"`（`FinalRank == 0` なので「最終試験」の行が無い） |
| | `Initialize_NoResult_ShowsPlaceholder` | `"SetRank:-"`、`"SetLines:結果はありません。"` |
| | `Home_ClearsSessionAndExitsOnce` | `GoHomeAsync` を 2 回 → `session.Current == null`、`"Flow.ExitToHome"` が 1 回 |
| `ProduceFlowTests.cs`（追記） | `ShowCurrent_Lesson_PushesLessonPage` / `ShowCurrent_Exam_PushesLessonPage` | `log` が `"PopPage:1"`, `"PushPage:LessonPresenter"` |
| | `ShowCurrent_Finished_PushesProduceResultPage` | `"PopPage:1"`, `"PushPage:ProduceResultPresenter"` |
| | `ShowCurrent_ExamResultNotShown_PushesExamResultFirst` | 局面 `Finished`・`ExamResult != null`・`ExamResultShown == false` → `"PopPage:1"`, `"PushPage:ExamResultPresenter"` |
| | `ShowCurrent_ExamResultShown_PushesPhaseScreen` | 同じ状態で `session.ExamResultShown = true` → `"PopPage:1"`, `"PushPage:ProduceResultPresenter"` |
| | `ShowCurrent_PhaseExamResultWithoutResult_ShowsInfoAndGoesHome` | 局面 `ExamResult`・`ExamResult == null` → `"PopPage:1"`, `"Info:試験の結果を取得できませんでした。ホームから再開してください。"`, `"GoToHome"`（`"PushPage:"` が無い） |

### 5.3 Play モード・手動（タスク 4。Editor。サーバー起動済み・初期付与済み）

**通し A（最短シナリオ。マスタ `scenario.csv` の `ScenarioId` 1、`scenarioWeek.csv` の週 1 `Action`（クリア目標 25・パーフェクト 50）→ 週 2 `Finishing`（クリア目標 75・パーフェクト 300）→ 週 3 `FinalExam`（NPC の基準 2250。`RetryCount` 3））**:
- 起動 → ガチャ（単発 1 回）→ 所持一覧からサポートカードを 1 枚強化 → ホーム →「プロデュース」→ 編成で「最短シナリオ（仮）」を選んで開始（`Produce` シーンへ移り、背景のページの上に週の画面）
- 週 1 のレッスン → 手札が名前・レア度・種類・消費・説明文つきの行で出る。使えないカードの行が灰色で押せない。カードをタップ → 確認（説明文つき）→ 使用 → 獲得・体力・手札が変わり、「直前の結果」の行に差が出る（ページは作り直されない）。スキップできる。「山札 …」のボタン → 一覧のモーダル（4 つの区分。見出しの行とカードの行）→ 閉じる
- レッスンが終わると「レッスン終了」と、レッスン全体で上がった値・体力の差（開始時との差）のダイアログ → **獲得（`獲得` の行の値）がクリア目標（週 1 は 25）以上で終わったときだけ**、背景のページの上にスキルカードの獲得のモーダル（week。MVP のクリア報酬はスキルカードの選択肢だけ: produce.md 4.1。候補が 0 枚なら出ない: 3.5）。未満なら出ずに週 2 の行動選択へ
- 週 2（仕上げ）→ 終わると「レッスン終了」のダイアログのあと週 3 の最終試験に入る（クリア目標 75 未満なら同じページのまま見出しが「最終試験」になる。75 以上ならスキルカードの獲得のモーダルを挟んでから、試験のページが積まれる）→ スコア・順位・このターン・次 が出る。カードを使って進める → 試験結果（「最終試験 結果（1 回目）」、自分の行が `Accent` の色で ◀ つき、ライバル 1〜5）
- 試験結果で、合格なら「次へ」（確認なし）、不合格なら「プロデュース終了」→ 確認（どちらも `DecideExamRetry(false)`）→ プロデュース結果（戻るボタンが無い。ランク・評価値・報酬）→「ホームへ」→ `Home` シーンに戻り、ガチャ通貨・プレイヤーLv が増えていて、「再開」が無い

**通し B（初級。`ScenarioId` 2、13 週。週 6 が中間試験（早期終了スコア 900、NPC 720・585・450・315・180）、週 13 が最終試験（NPC 1800・1462・1125・787・450）、`RetryCount` 3）**:
- ホーム →「プロデュース」→ 編成で「初級（仮）」を選んで開始。週 1〜5 は、レッスン・授業・支給・休むのうち選べるものを選んで進める（週 1・3 はレッスン、週 5 は仕上げ）
- **再挑戦と不合格の出し方（中間試験の 1 回目）**: 週 6 の中間試験に入ったら、**カードを使わずに「スキップ」だけを 9 ターン続ける**。スキップは体力を 2 回復するだけでスコアを足さない（[lesson-card.md](../wiki/systems/lesson-card.md) の「スキップ時の体力回復 2」。試験の開始時はステータス・持続効果が無いので、スキップで発動する効果も無い）ので、スコア 0 で NPC 5 人の全員より下の 6 位 → 不合格（4 位以下: produce.md 5.1）。試験結果の題が「中間試験 結果（1 回目）」、「不合格」、ボタンが「再挑戦（残り 3）」と「プロデュース終了」
- 「再挑戦」→ 確認 → 同じレッスンのページに中間試験の 1 ターン目が出る（体力・デッキは試験の開始前に戻る）。**2 回目はカードを使って進める**。試験結果の題が「中間試験 結果（2 回目）」になる
  - 合格（スコア 450 以上で 3 位以内。同点は自分が上: produce.md 5.3）なら「次へ」（確認なし。再挑戦が残っていれば `DecideExamRetry(false)`）→ 週 7 の行動選択へ
  - 2 回目も不合格なら、もう一度「再挑戦」してカードを使って進める（残りが 0 になるまで）。残りが 0 で不合格なら、局面「試験の結果」を飛ばして結果を見せるだけになり（「次へ」だけ）、プロデュース結果に「中間試験で不合格」が出る。**このときも通し B は満たす**（局面 `Finished` まで放棄せずに進めたため。produce.md 5.1.1: 不合格で終わるのも「完了」）が、最終試験まで届かなかったことを unity7-evaluation.md の行に書く
- 週 7〜12 を進め（週 12 は仕上げ）、週 13 の最終試験 → 試験結果 →「次へ」または「プロデュース終了」→ プロデュース結果 →「ホームへ」→ `Home`（「再開」が無い）
**通し A・B のほかに Editor で確かめること**:
- レッスンの途中でアプリを終了 → 再起動 → ホームの「再開」で `Produce` シーンのレッスンに戻る（同じ手札・ターン。「直前の結果」は空）
- 「≡」→ 閉じる → そのままカードを使える。「≡」→ ホームへ戻る → `Home` シーン（「再開」がある）。「≡」→ 放棄 → 確認 → `Home` シーン
- 日本語と記号（`≡`・`◀`）が豆腐でない（P3 の決めたこと 11）。コンソールにエラーが無い

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

- （2026-11-07 に照合済み）week の形: `PushForPhaseAsync` の `switch` の `case`、`FakeProduceFlow`（`ShowCurrentAsync`・`ExitToHomeAsync`・`HandleStepAsync` の上書きと記録の書式）、`SkillCardRowView.SetTexts(title, costs, description)`（week が足す）、`DisplayText` の命名（型と同じ名前にしない: owned の `RarityName`・`ParamName` も同じ規則にそろえた）。索引（[2026-11-07-client-screens-index.md](2026-11-07-client-screens-index.md)）は 1 の表に状態の列を持つ
