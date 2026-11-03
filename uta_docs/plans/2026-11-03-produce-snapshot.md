---
title: 実装計画: ProduceSnapshot（プロデュースの状態の保存・復元の形）
status: draft
updated: 2026-11-03
spec: ../wiki/architecture/produce-domain.md, ../wiki/architecture/lesson-domain-types.md
---

# 実装計画: ProduceSnapshot

## 1. 目的と前提

`Uta.Shared.Domain` に、`ProduceState`（と中の `LessonState`）を平らな値だけの型 `ProduceSnapshot` に写し、そこから状態を作り直す `From` / `ToState` を足す。サーバーが状態をセッションの 1 列に保存し、途中から再開するための土台（順 6 の計画 1/3。次は [サーバーのプロデュース: 開始・保存](2026-11-03-server-produce-start.md)）。

- 型の形の正: [produce-domain.md](../wiki/architecture/produce-domain.md) 6.1（プロパティの名前・型・順はそこが正。この計画は書き写さない）
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain は MessagePack に依存しない）
- 先に確かめたこと（2026-11-03）:
  - `ProduceState` は `internal` のコンストラクタ `ProduceState(ProduceStartInput input, int maxHp, int[] parameters, int[] bonusPerThousand, IReadOnlyList<CardInstance> deck, int examRetryLeft)` と、`internal set` のプロパティ（`Phase`・`Week`・`Attempt`・`NextOperationNumber`・`NextInstanceId`・`Hp`・`ExamRetryLeft`・`Lesson`・`ExamBackup`・`LastExam`・`MidExam`・`Result`）、`internal` のメソッド（`SetClassOptionParams(IReadOnlyList<int>)`・`SetOfferCards(IReadOnlyList<int>)`・`AddOperation(OperationRecord)`）を持つ。同じアセンブリの `ProduceSnapshot` から呼べる
  - `LessonState` には、保存した値から作り直す public コンストラクタがある（`LessonState.cs` の 2 つ目）。`Zones(drawPile, hand, discard, exiled)`、`StatusSet(IReadOnlyList<int> values, IReadOnlyList<bool> skipNextDecrement)`（長さは `StatusSet.KindCount`）、`ParamBonusEntry(percent, remainingTurns, skipNextDecrement)`、`PersistentEffect(trigger, effects, remainingTurns, remainingCount, grantOrder, skipNextDecrement)` も public
  - `ScheduleEffect` は `EffectExecutor` が対応していない（`NotSupportedException`）ので、`LessonState.Scheduled` は常に空
  - `ProduceStateComparer.Diff(expected, actual)`（空なら一致）と `LessonStateComparer.Diff` がある。往復の比較に使う
- この計画で**決めないこと**: サーバーの保存の形（MessagePack）、DB、RPC（次の計画）。`Lesson` の型の変更（しない）

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `shared/Uta.Shared.Domain/Produce/ProduceSnapshot.cs` と `.meta` | 4章 |
| 作成 | `shared/Uta.Shared.Domain/Produce/LessonSnapshot.cs` と `.meta` | 4章（レッスン側の型。`Produce` フォルダに置く） |
| 作成 | `shared/Uta.Shared.Domain.Tests/Produce/ProduceSnapshotTests.cs` | 5章 |
| 変更 | `uta_docs/wiki/architecture/produce-domain.md` | 実装メモに 1 行（メインのセッション） |

`.meta` は、同じフォルダの既存ファイル（例: `ProduceState.cs.meta`）をコピーし、`guid` だけ新しい値（32 桁の 16 進を `python -c "import uuid;print(uuid.uuid4().hex)"` で作る）にする。

## 3. 書式

`shared/` の `.editorconfig` に従う: C# 9（ファイルスコープ namespace・プライマリコンストラクタ・コレクション式・Index/Range は使わない。ブロック形式の `namespace Uta.Shared.Domain.Produce { ... }`）、Allman、`var`、BOM 付き UTF-8、LF。プロパティは `public X Name { get; set; }`、配列は `= new X[0];` で初期化、`bool` / `int` は既定値。

## 4. 型とシグネチャ

### 4.1 型

[produce-domain.md](../wiki/architecture/produce-domain.md) 6.1 の表の 10 個の型を、**プロパティの名前・型・宣言順をその表のとおりに**作る。置き場:

- `LessonSnapshot.cs`: `CardInstanceSnapshot`、`ParamBonusSnapshot`、`PersistentSnapshot`、`LessonSnapshot`
- `ProduceSnapshot.cs`: `SupportCardSnapshot`、`ExamReportSnapshot`、`ExamBackupSnapshot`、`ProduceResultSnapshot`、`OperationSnapshot`、`ProduceSnapshot`

（`using Uta.Shared.Domain.Lesson;` が要る。`Rank` は `ExamReportSnapshot` では `int`、`ProduceResultSnapshot` では `ProduceRank`。）

### 4.2 `LessonSnapshot` のメソッド

```csharp
public static LessonSnapshot From(LessonState state, ICardCatalog cards);
public LessonState ToState(ICardCatalog cards);
```

`From`:

1. `state.Scheduled.Count > 0` なら `NotSupportedException("発動予約は保存できません。")`
2. 値を次のとおり写す。`Kind` / `TargetParam` / `MaxTurns` / `ClearTarget` / `PerfectTarget` は `state.Setup` から。`state.Setup.Exam != null` のとき `ExamTurnParams` に `Exam.Turns[i].Param`、`ExamTurnScoreBonusPercents` に `Exam.Turns[i].ScoreBonusPercent`、`ExamEarlyEndScore` に `Exam.EarlyEndScore`（`Exam == null` のときは 2 つの配列は空、`ExamEarlyEndScore` は 0）。`Deck` は `state.Deck` の並びのまま。`DrawPile` / `Hand` / `Discard` / `Exiled` は `state.Zones` の並びのまま。`StatusValues[i]` は `state.Statuses.Get((StatusKind)i)`、`StatusSkipNextDecrement[i]` は `state.Statuses.IsSkipNextDecrement((StatusKind)i)`（`i` は 0 から `StatusSet.KindCount - 1`）。`ParamBonuses` は `state.ParamBonuses` の並びのまま。`LessonEnhanced` は `state.LessonEnhanced`（`SortedSet` の昇順のまま）。`Turn` / `Phase` / `Hp` / `MaxHp` / `Shield` / `UsesLeft` / `ParamGained` / `NextGrantOrder` は `state` の同名
3. `Persistents`: `state.Persistents` の並びのまま、1 件ずつ `FindOrigin(cards, persistent)` で `(CardId, Enhanced, EffectIndex)` を求めて `PersistentSnapshot` にする（残りの値は `persistent` の `RemainingTurns` / `RemainingCount` / `GrantOrder` / `SkipNextDecrement`）

`FindOrigin`（`private static`）: `cards.CardIds` の昇順に、`def = cards.Get(id)` を取り、面を `(def.Base, false)`、`def.Enhanced != null` なら `(def.Enhanced, true)` の順に見る。各面の `Effects` を添字 `i` で走り、`effect.Op == EffectOp.AddPersistent && ReferenceEquals(effect.Children, persistent.Effects)` になる最初の `(id, enhanced, i)` を返す。どれにも当たらなければ `InvalidOperationException("持続効果の由来のカードが見つかりません。")`。

`ToState(ICardCatalog cards)`:

1. 検査（`ArgumentException`。メッセージは `"<項目名>: <理由>"`）: 配列プロパティが `null`、`StatusValues` / `StatusSkipNextDecrement` の長さが `StatusSet.KindCount` でない、`ExamTurnParams` と `ExamTurnScoreBonusPercents` の長さが違う
2. `exam`: `Kind` が `LessonKind.MidExam` / `LessonKind.FinalExam` なら `new ExamSetup(turns, ExamEarlyEndScore)`（`turns` は `ExamTurn(param, bonus)` の配列）、そうでなければ `null`。`setup = new LessonSetup(Kind, TargetParam, MaxTurns, ClearTarget, PerfectTarget, exam)`
3. `Persistents` を 1 件ずつ `PersistentEffect` にする: `def = cards.Get(CardId)`、`face = Enhanced ? def.Enhanced : def.Base`（`Enhanced` なのに `def.Enhanced == null` なら `ArgumentException`）、`EffectIndex` が `0` 未満または `face.Effects.Count` 以上、または `face.Effects[EffectIndex].Op != EffectOp.AddPersistent` なら `ArgumentException`。`effect = face.Effects[EffectIndex]` として `new PersistentEffect(effect.Trigger, effect.Children, RemainingTurns, RemainingCount, GrantOrder, SkipNextDecrement)`
4. `new LessonState(setup, deckInstances, new Zones(DrawPile, Hand, Discard, Exiled), new StatusSet(StatusValues, StatusSkipNextDecrement), paramBonusEntries, new ScheduledEffect[0], persistentEffects, LessonEnhanced, Turn, Phase, Hp, MaxHp, Shield, UsesLeft, ParamGained, NextGrantOrder)`。`deckInstances` は `Deck` から `new CardInstance(InstanceId, CardId, Enhanced)` を並びのまま

### 4.3 `ProduceSnapshot` のメソッド

```csharp
public static ProduceSnapshot From(ProduceState state, ICardCatalog cards);
public ProduceState ToState(ProduceCatalog catalog);
```

`From`: `state` の値を表のとおり写す（`SupportCards` は `state.Input.SupportCards`、`Parameters` は `state.CopyParams()`、`BonusPerThousand` は `state.GetBonusPerThousand(ProduceParams.FromIndex(i))`（i = 0..2）、`Deck` / `ClassOptionParams` / `OfferCards` / `Operations` は並びのまま、`Lesson` は `state.Lesson == null ? null : LessonSnapshot.From(state.Lesson, cards)`、`ExamBackup`（`Parameters` / `Hp` / `Deck` / `NextInstanceId`）・`LastExam`・`MidExam`（`ExamReport` の各プロパティ）・`Result`（`ProduceResult` の各プロパティ）は `null` ならそのまま `null`、`Operations[i]` は `OperationRecord` の `Number` / `Command.Kind` / `Command.Arg1..3` / `Week` / `Attempt` / `Turn`）。

`ToState`:

1. 検査（`ArgumentException`、メッセージは `"<項目名>: <理由>"`）: 配列プロパティが `null`、`Parameters` / `BonusPerThousand` の長さが `ProduceRules.ParamCount` でない
2. `input = new ProduceStartInput(ScenarioId, IdolId, PlayerLevel, supportInputs)`（`supportInputs` は `new SupportCardInput(CardId, Level, LimitBreak)`）
3. `state = new ProduceState(input, MaxHp, Parameters, BonusPerThousand, deckInstances, ExamRetryLeft)`
4. `state.Phase = Phase; Week; Attempt; NextOperationNumber; NextInstanceId; Hp` を代入。`state.SetClassOptionParams(ClassOptionParams); state.SetOfferCards(OfferCards);`
5. `Lesson != null` なら `state.Lesson = Lesson.ToState(catalog.Cards)`。`ExamBackup != null` なら `new ExamBackup(Parameters, Hp, deck, NextInstanceId)`。`LastExam` / `MidExam` は `new ExamReport(Kind, Attempt, Score, Rank, NpcScores, Passed)`。`Result` は `new ProduceResult(Outcome, EvaluationValue, Rank, FinalScore, FinalRank)`
6. `Operations` を並びのまま `state.AddOperation(new OperationRecord(Number, new ProduceCommand(Kind, Arg1, Arg2, Arg3), Week, Attempt, Turn))`
7. `state` を返す

## 5. タスク

### タスク 1: 型と `From` / `ToState`（`ProduceState` ごと）

- 触るファイル: 2 の表の作成 2 つ、`ProduceSnapshotTests.cs`
- 手順: 4章のとおりに型と 2 つの `From`・2 つの `ToState` を書く。`ProduceSnapshotTests.cs` に 6章の 1〜6 を書く
- 境界の扱い: `Lesson` / `ExamBackup` / `LastExam` / `MidExam` / `Result` が `null` の状態（開始直後・終了後）、空の配列（授業の週でない `ClassOptionParams`）
- 確かめ方: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project shared/Uta.Shared.Domain.Tests` が通る

### タスク 2: 持続効果・不正な値のテスト

- 手順: 6章の 7〜10 を足す。実装（4.2）の誤りが見つかったときは、実装を直す（設計の判断が要るときは止まって報告する）
- 確かめ方: タスク 1 と同じ

## 6. テスト（`shared/Uta.Shared.Domain.Tests/Produce/ProduceSnapshotTests.cs`）

共通: `Catalog = TestProduceMaster.Create()`、`Engine = new ProduceEngine(Catalog)`。比較は `Assert.Empty(ProduceStateComparer.Diff(expected, actual))`。自動プレイは `ProduceBot`（`Next` で次のコマンドを決める）。入力は `new ProduceStartInput(scenario, idol, 1, new SupportCardInput[0])`。

1. `From_ToState_AfterStart_SameState`: シナリオ 1・アイドル 1・シード 1（`new TestRandomStreamSource(1)`）で `Start` した状態を `ProduceSnapshot.From(state, Catalog.Cards).ToState(Catalog)` で往復し、元と一致する
2. `RoundTrip_EveryOperation_SameState`（`[Theory]`）: シナリオ 1（アイドル 1・2）とシナリオ 2（アイドル 1〜4）、シード 1・2・3 のそれぞれで、`ProduceBot.Next` → `Apply` を `Phase == Finished` まで繰り返し、**`Apply` のたびに**往復して元の状態と一致する（`retryExam` は false と true の両方。サポートカードは、シナリオ 2・アイドル 1 のときだけ `new SupportCardInput(1, 5, 0)` を 1 枚つける）
3. `Resume_EveryOperation_SameFinalState`（`[Theory]`、2 と同じ組み合わせ）: 同じシードで 2 回走らせる。A は途中を保存せずに最後まで。B は `Apply` のたびに `state = ProduceSnapshot.From(state, Catalog.Cards).ToState(Catalog)` で作り直して続ける（乱数のストリームは同じ `TestRandomStreamSource` を使い続ける）。最後の状態が一致し、`Operations` の件数も同じ
4. `RoundTrip_FinishedState_KeepsResult`: 3 の A の最後の状態で、`Result` が `null` でなく、往復して一致する
5. `RoundTrip_ExamResultPhase_KeepsBackupAndReports`: シナリオ 2 を `retryExam: true` で、`Phase == ExamResult` になるまで進め（`ProduceBot.RunUntil`）、往復して一致する。`ExamBackup` が `null` でなく、`LastExam` が `null` でないことも確かめる
6. `RoundTrip_LessonInProgress_KeepsZonesAndOrder`: シナリオ 1 で `Phase == Lesson` になり、手札が空でない状態（`ProduceBot.RunUntil(..., stop: s => s.Phase == ProducePhase.Lesson && s.Lesson.Turn >= 2)`）で往復して一致する。山札の並びが `Zones.DrawPile` と同じ順であることも `Assert.Equal` で確かめる
7. `LessonSnapshot_Persistent_RoundTripsOrigin`: `catalog = new TestCardCatalog()` の `CardIds` を昇順に走査し、`Base.Effects` に `Op == EffectOp.AddPersistent` を含む最初のカードを選ぶ。`TestStates.PlayerState(new[] { cardId }, new[] { 1 }, new int[0], 30, usesLeft: 1)` を `new LessonEngine(catalog).Apply(state, LessonCommand.UseCard(1), TestStates.NewRandom())` で進め、`state.Persistents.Count == 1` を確かめる。`LessonSnapshot.From` → `ToState` で往復して `LessonStateComparer.Diff` が空。`Persistents[0].CardId == cardId`、`Enhanced == false`
8. `LessonSnapshot_PersistentOfEnhancedFace_RoundTripsOrigin`: 7 と同じカードを強化済みで（`TestStates.PlayerState(..., enhanced: new[] { 1 })`）使い、`Enhanced == true`、往復して一致する。使うカードの `Enhanced` 面に `AddPersistent` が無いときは、`Enhanced` 面に `AddPersistent` を含む最初のカードを別に探す（`def.Enhanced != null`）
9. `LessonSnapshot_Scheduled_Throws`: `LessonState` の復元用コンストラクタで `ScheduledEffect` を 1 つ持つ状態を作り、`From` が `NotSupportedException`
10. `ToState_InvalidValues_ThrowsArgumentException`（`[Theory]` か複数の `[Fact]`）: `Parameters` の長さが 2、`LessonSnapshot.StatusValues` の長さが違う、`PersistentSnapshot.EffectIndex` が範囲外、`EffectIndex` の効果が `AddPersistent` でない、`Enhanced = true` で強化面のないカード、配列プロパティが `null`。いずれも `ArgumentException`

実行するコマンド: `dotnet build Uta.slnx`、`dotnet test --project shared/Uta.Shared.Domain.Tests`。

## 7. 未決事項

なし
