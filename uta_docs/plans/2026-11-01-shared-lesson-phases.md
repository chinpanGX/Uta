---
title: 実装計画: Uta.Shared.Domain のレッスン（フェーズ 2〜4・6）
status: done
updated: 2026-11-01
spec: ../wiki/architecture/lesson-phases.md, ../wiki/architecture/lesson-domain-types.md, ../wiki/architecture/lesson-enums.md, ../wiki/systems/lesson-card.md
---

# 実装計画: Uta.Shared.Domain のレッスン（フェーズ 2〜4・6）

## 1. 目的と前提

[lesson-phases.md](../wiki/architecture/lesson-phases.md) のフェーズ 2（元気）・3（好調）・4（集中）・6（試験）を `shared/Uta.Shared.Domain` に実装する。フェーズ 0・1（エンジン、全コスト・全条件・全ステータスの仕組み）は実装済み（ビルド警告 0・テスト 63 本成功を 2026-11-01 に確認）。フェーズ 3・4 は仕組みが入っているので、カード定義（テスト側）とテストを足すだけ。フェーズ 5（ドリンク・育成アイテム）は別の仕様を書いてから（[lesson-phases.md](../wiki/architecture/lesson-phases.md) 6章）なので、この計画に入れない。

関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain の置き方）、[0016](../wiki/decisions/0016-reference-game-values.md)（カードの数値）。

**この計画で決めないこと（範囲外）**:

- フェーズ 5（ドリンク・育成アイテム）
- MVP の 36 枚が使わない `Op`（`AddParamBonus`・`AddStatusPercent`・`ReplaceHand`・`EnhanceHand`・`CreateCard`・`ScheduleEffect`・`EveryNTurns`。[lesson-phases.md](../wiki/architecture/lesson-phases.md) 1章）。読み込み時エラーのまま
- F08（不調カード）
- `CardDefinitionValidator` をマスタの検証の規則にそろえること（[lesson-enums.md](../wiki/architecture/lesson-enums.md) 未決事項）。この計画で足す検査は、2・4・6 章に書いたものだけ
- テストのカードとマスタの値が一致することを確かめるテスト（[lesson-phases.md](../wiki/architecture/lesson-phases.md) 1章）。マスタの実際のカード ID（F = 1〜7、S = 101〜114、L = 201〜215。`masterdata/csv/card.csv`）がテストの ID（F = 番号、L = 10 + 番号、S = 30 + 番号）と違うため、どちらに合わせるかをユーザーが決めてから別の計画で行う
- IL2CPP の実機での再生の確認（`client/uta/Assets/Tests/SharedDomainReplay/`。[lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章。`client/` は別の計画の担当）。この計画は、そこから呼ぶ `LessonStateComparer`（3.2）までを作る
- `masterdata/`・`server/`・`client/` は触らない

**この計画で決めたこと [Uta案。2026-11-01]**（設計判断をここで済ませるため。仕様側は [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) 2・5章に書いた）:

- 試験の `LessonSetup` は、種類と `exam` の食い違い・目標の値を検査し、対象のパラメータは使わない
- 試験の L1（`Turn` が 0）でパラメータが上がるときの倍率は 1 ターン目の値
- `LessonStateComparer` の形（3.2）
- `LessonResult` / `LessonEndReason` / `GetResult` の細部（3.3）

## 2. 触るファイル

`D` = `shared/Uta.Shared.Domain/Lesson`、`T` = `shared/Uta.Shared.Domain.Tests/Lesson`。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `T/TestCards.cs` | テストのカードを組み立てる共通の静的クラス | 1 |
| 作成 | `T/SyntheticCards.cs` | 合成カード（ID 900 以上）の定義 | 1 |
| 作成 | `T/TestCardCatalog.cs` | 全フェーズのカードをまとめて返す `ICardCatalog` | 1 |
| 作成 | `T/TestStates.cs` | テストの状態の作り方（`Deck`・`PlayerState`・`Copy`・`NewRandom`） | 1 |
| 変更 | `T/Phase1Cards.cs` | 静的クラスにし、`TestCards` を使う | 1 |
| 変更 | `T/LessonEngineTests.cs` | 移した補助を使う。`Summarize` を置き換える | 1・2 |
| 作成 | `D/LessonStateComparer.cs` | 状態を項目ごとに文字列にし、比べる | 2 |
| 作成 | `T/LessonStateComparerTests.cs` | 同上のテスト | 2 |
| 変更 | `D/LessonFormulas.cs` | `FixedShieldGain`・`ScaledShield` を足す | 3 |
| 変更 | `D/EffectExecutor.cs` | フェーズ 2 の 6 つの `Op` を足す。試験のスコア換算 | 3・7 |
| 変更 | `D/StatusSet.cs` | 低下状態無効の打ち消し | 3 |
| 変更 | `D/CardDefinitionValidator.cs` | 対応する `Op`・下限・L1 の検査 | 3 |
| 変更 | `T/CardDefinitionValidatorTests.cs` | 既存テストの直しと新しい検査のテスト | 3 |
| 作成 | `T/Phase2OpsTests.cs` | 合成カードによる `Op`・コスト・低下状態無効のテスト | 3 |
| 変更 | `T/LessonFormulasTests.cs` | 新しい式のテスト | 3 |
| 作成 | `T/Phase2Cards.cs`・`T/Phase2CardsTests.cs` | フェーズ 2 のカード 9 枚とテスト | 4 |
| 作成 | `T/Phase3Cards.cs`・`T/Phase3CardsTests.cs` | フェーズ 3 のカード 5 枚とテスト | 5 |
| 作成 | `T/Phase4Cards.cs`・`T/Phase4CardsTests.cs` | フェーズ 4 のカード 9 枚とテスト（好調×集中の式全体を含む） | 6 |
| 作成 | `D/ExamSetup.cs` | `ExamTurn`・`ExamSetup` | 7 |
| 作成 | `D/LessonResult.cs` | `LessonResult`・`LessonEndReason` | 7 |
| 変更 | `D/CardDefinition.cs` | `LessonSetup` に `exam` を足す | 7 |
| 変更 | `D/LessonEngine.cs` | 試験の開始、早期終了、`GetResult` | 7 |
| 変更 | `D/LessonFormulas.cs`・`T/LessonFormulasTests.cs` | `ExamScore`（9.5） | 7 |
| 変更 | `D/LessonStateComparer.cs` | `exam=` の項目を足す | 7 |
| 作成 | `T/ExamTests.cs` | 試験・`LessonResult` のテスト | 7 |
| 作成 | `T/LessonReplayTests.cs` | 全フェーズのカードでの再生のテスト | 8 |
| 変更 | `T/LessonEngineTests.cs` | 再生のテストを `LessonReplayTests` に移す | 8 |

## 3. 型とシグネチャ

C# は `shared/` の `.editorconfig` に従う（C# 9。ブロック形式の namespace、Allman、`var`、`record` なし）。`float`・`double` を使わない。`.cs` は BOM 付き UTF-8、改行は LF。

### 3.1 テストの共通クラス（タスク 1。`T/` は名前空間 `Uta.Shared.Domain.Tests.Lesson`）

`TestCards`（`public static class`）。今の `Phase1Cards` の `private` な補助を `public static` にして移し、次を足す:

```csharp
public static CardDefinition Card(int id, Plan plan, CardType type, Rarity rarity,
    bool once, bool unique, CardFace baseFace, CardFace enhanced, bool startInHand = false)
    // 解放Lv は 1 固定: new CardDefinition(id, plan, type, rarity, 1, once, unique, startInHand, baseFace, enhanced)
public static CardFace Face(CostDef cost, params EffectDef[] effects)                          // 既存
public static CardFace FaceWithCondition(CostDef cost, ConditionDef useCondition, params EffectDef[] effects) // 既存
public static CostDef Consume(int value)                                                       // 既存
public static CostDef HpDirect(int value)                                                      // 既存
public static EffectDef Param(int value, int hits = 0, int focusScale = 0, int goodScale = 0)
    // new EffectDef(EffectOp.GainParam, value: value, arg1: hits, arg2: focusScale, arg3: goodScale)。既存は Param(int)
public static EffectDef Shield(int value)                                                      // 既存
public static EffectDef Impression(int value)                                                  // 既存
public static EffectDef Turns(StatusKind kind, int value)                                      // 既存
public static EffectDef Status(StatusKind kind, int value)                                     // 新規。Turns と同じ中身（AddStatus）
public static EffectDef StackIf(StatusKind kind, int value, ConditionDef condition)            // 既存
public static EffectDef Uses(int value) / Draw(int value)                                      // 既存
public static EffectDef RatioOfImpression(int percent)                                         // 既存
public static EffectDef OnMentalUse(EffectDef effect)                                          // 既存
public static ConditionDef HpAtLeast(int percent) / ImpressionAtLeast(int value)               // 既存
public static EffectDef If(EffectDef effect, ConditionDef condition)
    // effect と同じ列で、Conditions だけを new[] { condition } にした新しい EffectDef を返す
```

`SyntheticCards`（`public static class`）: `public const int StartInHandDummy = 900;` と `public static IReadOnlyList<CardDefinition> All()`。タスク 1 では 900（今の `StartInHandDummy`: `Plan.Free`・`CardType.Mental`・`Rarity.N`・once なし・unique なし・`startInHand: true`、通常版 `Face(Consume(0))`、強化版 null）だけを返す。

`Phase1Cards`（`public static class`）: 定数 `F01`〜`L15`（今と同じ値）、`AllIds`（今の `AllPhase1Ids` を改名する。`LessonEngineTests` の呼び出し側も直す）、`public static IReadOnlyList<CardDefinition> All()`（今のコンストラクタの 13 枚。定義の中身は変えない）。`StartInHandDummy` は `SyntheticCards` に移す。`Phase2Cards`〜`Phase4Cards` も同じ形（`const` の ID・`AllIds`・`All()`）。**その場で作る合成カードの ID は 901 以上**（900 は `SyntheticCards.StartInHandDummy`）。

`TestCardCatalog`（`public sealed class : ICardCatalog`）:

```csharp
public TestCardCatalog()                                   // Phase1Cards.All() と SyntheticCards.All()、後のタスクで Phase2〜4Cards.All() を足す
public TestCardCatalog(IEnumerable<CardDefinition> extra)  // 上に extra を足す。ID が重なったら ArgumentException
public CardDefinition Get(int cardId)                      // 無ければ KeyNotFoundException
```

`TestStates`（`public static class`）。今の `LessonEngineTests` の `Deck`・`PlayerState`・`Copy`・`NewRandom` を移して `public static` にする。`PlayerState` の引数は次にする（持続効果を持つ状態は `persistents` で渡す）（今の引数に足す。`impression` は `statuses` に置き換える）:

```csharp
public static LessonState PlayerState(int[] cardIds, int[] hand, int[] drawPile, int hp,
    int usesLeft = 1, int paramGained = 0, int shield = 0, int[] discard = null,
    LessonSetup setup = null,                       // null なら NormalLesson（6ターン・クリア40・パーフェクト80）
    int turn = 1, int maxHp = 30,
    IDictionary<StatusKind, int> statuses = null,   // null は全部 0
    int[] enhanced = null,                          // 強化済みにするインスタンス ID
    PersistentEffect[] persistents = null)          // null は 0 件
public static LessonSetup NormalLesson { get; }      // new LessonSetup(LessonKind.Normal, ParamKind.Singing, 6, 40, 80)
public static List<CardInstance> Deck(IEnumerable<int> cardIds, int[] enhanced = null)  // インスタンス ID は 1 から。enhanced に挙げた ID は強化済み
```

### 3.2 `LessonStateComparer`（タスク 2。`D/LessonStateComparer.cs`、名前空間 `Uta.Shared.Domain.Lesson`）

```csharp
public static class LessonStateComparer
{
    /// <summary>状態の全部の値を「項目=値」の文字列の並びにする。</summary>
    public static IReadOnlyList<string> Describe(LessonState state);

    /// <summary>食い違った項目だけを返す。同じなら空。</summary>
    public static IReadOnlyList<string> Diff(LessonState expected, LessonState actual);
}
```

`Describe` は次の順に 1 項目 1 行を返す。**enum は数値（`(int)`）で書く**（名前は使わない。`ToString` に頼らない）。整数は `ToString(CultureInfo.InvariantCulture)` で文字列にする（`long` も同じ）。真偽は `1` / `0`。列は `,` で連結する（空の列は空文字）。

| 順 | 行 | 値 |
|---|---|---|
| 1 | `setup=` | `kind,targetParam,maxTurns,clearTarget,perfectTarget` |
| 2 | `turn=` `phase=` `hp=` `maxHp=` `shield=` `usesLeft=` `paramGained=` `nextGrantOrder=` | それぞれの値（この順に 8 行） |
| 3 | `deck=` | `Deck` の順に `instanceId:cardId:enhanced` を `,` で連結 |
| 4 | `drawPile=` `hand=` `discard=` `exiled=` | 各ゾーンのインスタンス ID を並びのまま連結（この順に 4 行） |
| 5 | `lessonEnhanced=` | `LessonEnhanced` の ID を連結 |
| 6 | `status.1=` 〜 `status.<StatusSet.KindCount - 1>=` | `StatusKind` の数値 1 から `StatusSet.KindCount - 1` まで、数値ごとに `値/印`（`Get` と `IsSkipNextDecrement`。印は 1/0） |
| 7 | `paramBonus.0=` `paramBonus.1=` … | `ParamBonuses` の添字ごとに `percent,remainingTurns,skipNextDecrement` |
| 8 | `scheduled.0=` … | `Scheduled` の添字ごとに `remainingTurns,grantOrder,<効果の列>` |
| 9 | `persistent.0=` … | `Persistents` の添字ごとに `trigger,remainingTurns,remainingCount,grantOrder,skipNextDecrement,<効果の列>` |

効果の列は、`Effects` の各 `EffectDef` の `Op` の数値を `;` で連結したもの（定義は同じカタログから来るので、中身ではなく並びだけを比べる）。

`Diff` は `Describe` の結果を先頭から比べる。長いほうの行数まで回し、足りない側は `(なし)` として、`string.Equals(a, b, StringComparison.Ordinal)` が偽の行ごとに `"期待 " + 期待の行 + " / 実際 " + 実際の行` を返す。

### 3.3 試験（タスク 7）

```csharp
// D/ExamSetup.cs
public readonly struct ExamTurn
{
    public ExamTurn(ParamKind param, int scoreBonusPercent);   // param が Nothing、scoreBonusPercent が負なら ArgumentOutOfRangeException
    public ParamKind Param { get; }
    public int ScoreBonusPercent { get; }
}

public sealed class ExamSetup
{
    public ExamSetup(IReadOnlyList<ExamTurn> turns, int earlyEndScore);
    // turns が null なら ArgumentNullException。空、または earlyEndScore が負なら ArgumentOutOfRangeException（nameof で引数名）
    public IReadOnlyList<ExamTurn> Turns { get; }
    public int EarlyEndScore { get; }                          // 0 は早期終了なし
}

// D/LessonResult.cs
public enum LessonEndReason { Nothing = 0, TurnsOver = 1, Perfect = 2, EarlyEnd = 3 }

public readonly struct LessonResult
{
    public LessonResult(int gained, int lastTurn, LessonEndReason reason, int hp);
    public int Gained { get; }          // 得たパラメータ。試験ならスコア
    public int LastTurn { get; }
    public LessonEndReason Reason { get; }
    public int Hp { get; }
}

// D/LessonEngine.cs に足す
public static LessonResult GetResult(LessonState state);
```

`LessonSetup`（`D/CardDefinition.cs`）:

```csharp
public LessonSetup(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget, ExamSetup exam = null)
public ExamSetup Exam { get; }
public bool IsExam => Exam != null;     // 今の `Kind == MidExam || FinalExam` から変える
```

コンストラクタの検査（この順）: `maxTurns <= 0` → `ArgumentOutOfRangeException(nameof(maxTurns))`（今のまま）／`kind == Nothing` → `ArgumentOutOfRangeException(nameof(kind))`（今のまま）／`(kind が MidExam か FinalExam) != (exam != null)` → `ArgumentException`（引数名 `nameof(exam)`）／試験でなく `targetParam == Nothing` → `ArgumentOutOfRangeException(nameof(targetParam))`（試験は `Nothing` も許す）／試験で `exam.Turns.Count != maxTurns` → `ArgumentOutOfRangeException(nameof(maxTurns))`／試験で `clearTarget != 0` または `perfectTarget != 0` → `ArgumentOutOfRangeException`（引数名は該当する方）。

## 4. タスク

各タスクは **Sonnet のサブエージェント**に渡す単位。タスクが終わったとき、`dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project shared/Uta.Shared.Domain.Tests`（全部成功）が通る状態にする。先のタスクに必要な型・補助を、前のタスクで作っておく。**順番に実行する**（同じファイルを触るので並行しない）。

### タスク 1: テストの基盤を移す（振る舞いを変えない）

触る: `T/TestCards.cs`・`T/SyntheticCards.cs`・`T/TestCardCatalog.cs`・`T/TestStates.cs`（作成）、`T/Phase1Cards.cs`・`T/LessonEngineTests.cs`（変更）。

手順:

1. 3.1 のとおり `TestCards` を作り、`Phase1Cards` の `private` な補助を移す（`Param` は引数を足す）。
2. `SyntheticCards`・`TestCardCatalog`（タスク 1 では `Phase1Cards.All()` と `SyntheticCards.All()` だけ）を作る。
3. `Phase1Cards` を静的クラスに書き換える。13 枚の定義（レアリティ・種別・once・unique・コスト・効果）は変えない。`StartInHandDummy` を `SyntheticCards` に移す。
4. `TestStates` を作り、`LessonEngineTests` の `Deck`・`PlayerState`・`Copy`・`NewRandom` を移す。`PlayerState` の `impression` 引数は `statuses` に置き換え、呼び出し側（`UseCard_L10_ParamFromImpressionRatio` の `impression: 4`）は `statuses: new Dictionary<StatusKind, int> { { StatusKind.Impression, 4 } }` に直す。`LessonEngineTests` の `Catalog` は `new TestCardCatalog()`、`Phase1Cards.AllPhase1Ids` は `Phase1Cards.AllIds`、`Phase1Cards.StartInHandDummy` は `SyntheticCards.StartInHandDummy` に直す。
5. `LessonEngineTests` の `Summarize` はこのタスクでは残す（タスク 2 で置き換える）。

境界: テストの本数と期待値を変えない。確かめ方: ビルドとテストが通り、成功が **63 本のまま**。

### タスク 2: 状態を比べる関数

触る: `D/LessonStateComparer.cs`・`T/LessonStateComparerTests.cs`（作成）、`T/LessonEngineTests.cs`（変更）。

手順:

1. 3.2 のとおり `LessonStateComparer` を作る（`Describe`・`Diff`。`Diff` の結果の型は `IReadOnlyList<string>`）。
2. `LessonEngineTests` の `RunLesson` の記録を `string.Join("\n", LessonStateComparer.Describe(state))` に置き換え、`Summarize` を消す。`Copy` は `TestStates.Copy` を使う。
3. テストを足す（5 章の `LessonStateComparerTests`）。

境界: 空の列は空文字。`Persistents` が 0 件のとき `persistent.*` の行は出ない。確かめ方: ビルドとテストが通る。

### タスク 3: フェーズ 2 の仕組み（エンジン・検査）

触る: `D/LessonFormulas.cs`・`D/EffectExecutor.cs`・`D/StatusSet.cs`・`D/CardDefinitionValidator.cs`、`T/TestCards.cs`、`T/CardDefinitionValidatorTests.cs`・`T/LessonFormulasTests.cs`・`T/LessonEngineTests.cs`・`T/Phase2OpsTests.cs`。

手順:

1. `LessonFormulas` に足す:
   ```csharp
   /// <summary>固定元気の増加量。やる気を足さない。元気増加無効中は 0（lesson-card.md 9.3）。</summary>
   public static int FixedShieldGain(int value, bool shieldGainBlocked)
   {
       return shieldGainBlocked ? 0 : Math.Max(0, value);
   }

   /// <summary>元気を percent% にする（端数は切り捨て。lesson-card.md 7.2）。</summary>
   public static int ScaledShield(int shield, int percent)
   {
       return FixedMath.ToInt(FixedMath.FloorDiv((long)shield * percent, Percent));
   }
   ```
2. `EffectExecutor.ExecuteOne` の `switch` に 6 つの `case` を足す。`GainParamByImpression` と同じ形の処理を、private の `GainParamByRatio(LessonState state, int source, int percent)`（`GainParam(state, ComputeParamGain(state, LessonFormulas.RatioParam(source, percent), 100, 100))`）にまとめ、`GainParamByImpression`・`GainParamByShield`・`GainParamByMotivation` が使う。
   ```csharp
   case EffectOp.GainParamByShield:
       GainParamByRatio(state, state.Shield, effect.Value);
       break;
   case EffectOp.GainParamByMotivation:
       GainParamByRatio(state, state.Statuses.Get(StatusKind.Motivation), effect.Value);
       break;
   case EffectOp.AddFixedShield:
       state.Shield += LessonFormulas.FixedShieldGain(effect.Value, state.Statuses.Get(StatusKind.ShieldGainBlock) > 0);
       break;
   case EffectOp.RecoverHp:
       state.Hp = FixedMath.ToInt(Math.Min(state.MaxHp, (long)state.Hp + effect.Value));
       break;
   case EffectOp.ScaleShield:
       state.Shield = LessonFormulas.ScaledShield(state.Shield, effect.Value);
       break;
   case EffectOp.SetShield:
       state.Shield = effect.Value;   // 元気増加無効でも設定する（lesson-card.md 7.2）
       break;
   ```
   （`using Uta.Shared.Domain.Numerics;` を足す。）
3. `StatusSet` に足す。`IsDebuff` は `public static bool`（`CostDouble`・`ShieldGainBlock` のとき真）。`Add` の `kind == Nothing` の検査の直後に:
   ```csharp
   // 低下状態無効: 1 以上の付与を 1 回打ち消す（lesson-card.md 8章）。打ち消したときは付与しないので、初付与の印も立てない
   if (amount >= 1 && IsDebuff(kind) && values[(int)StatusKind.DebuffGuard] > 0)
   {
       values[(int)StatusKind.DebuffGuard]--;
       return;
   }
   ```
4. `CardDefinitionValidator`:
   - `IsSupported` に `GainParamByShield`・`GainParamByMotivation`・`AddFixedShield`・`RecoverHp`・`ScaleShield`・`SetShield` を足す。クラスの `<summary>` を「フェーズ2まで」に直す。
   - `ValidateEffect` の `AddStatus`/`DrawCards` の下限の検査の直後に足す: `GainParamByShield`・`GainParamByMotivation`・`AddFixedShield`・`RecoverHp` で `effect.Value < 1` なら `prefix + effect.Op + " の値が 1 未満"`。`ScaleShield` で `effect.Value > 100` なら `prefix + effect.Op + " の値が 100 を超える"`。`SetShield` の下限（0 以上）は既存の「負」の検査で足りる。
   - `AddPersistent` の枝で、子を検査する前に足す: `effect.Trigger == PersistentTrigger.LessonStart` のとき、`effect.Children` の各 `child` の `Op` が `DrawCards`・`ReplaceHand`・`EnhanceHand`・`CreateCard` のどれかなら `prefix + "レッスン開始時の効果が " + child.Op + " でゾーンを操作している"`（lesson-card.md 10章の L1。子の `Op` が未対応のときは、未対応のエラーと両方出てよい）。
5. `TestCards` に足す（`public static`）:
   ```csharp
   public static CostDef StatusCost(CostKind kind, int value)       // new CostDef(kind, value)
   public static EffectDef FixedShield(int value)                   // AddFixedShield
   public static EffectDef Recover(int value)                       // RecoverHp
   public static EffectDef ScaleShield(int percent)
   public static EffectDef SetShield(int value)
   public static EffectDef ParamByShield(int percent)               // GainParamByShield
   public static EffectDef ParamByMotivation(int percent)           // GainParamByMotivation
   ```
6. 既存テストの直し: `CardDefinitionValidatorTests.Validate_GainParamByShield_ReturnsError` は `GainParamByShield` が対応済みになるので、`EffectOp.AddParamBonus`（採用時まで未対応）に変えて名前を `Validate_AddParamBonus_ReturnsError` にする。`LessonEngineTests.Validator_UnsupportedOp_ReportsError` の `ScaleShield` も `AddParamBonus` に変える。
7. テストを足す（5 章の `Phase2OpsTests` ほか）。`Phase2OpsTests` は、合成カード（ID 901 以上、その場で `TestCards.Card` で作り、`new TestCardCatalog(new[] { card })` と `new LessonEngine(catalog)` で動かす）を `usesLeft: 2` の `PlayerState` で 1 枚使い、そのターンの途中の状態を確かめる。

境界: `ScaleShield` の `Value` 0〜100、`RecoverHp` は最大体力で止まる、`SetShield` の `Value` 0 を許す、`DebuffGuard` は付与量 0・負では減らない。確かめ方: 5 章のテストが通り、既存のテストも通る。

### タスク 4: フェーズ 2 のカード

触る: `T/Phase2Cards.cs`・`T/Phase2CardsTests.cs`（作成）、`T/TestCardCatalog.cs`（`Phase2Cards.All()` を足す）。

手順: `Phase1Cards` と同じ形で `Phase2Cards` を作る（`public const int F07 = 7, L03 = 13, L05 = 15, L06 = 16, L08 = 18, L09 = 19, L11 = 21, L12 = 22, L13 = 23;`、`AllIds`、`All()`）。定義は [lesson-card.md](../wiki/systems/lesson-card.md) 14章の表から、下の表のとおりに `TestCards.Card` で作る（`Plan`・`CardType`・`Rarity`・once・unique・startInHand は表。効果は記載順 = `effects` の順）。

| ID | Plan・種別・レア | once / unique / hand | 通常版 | 強化版 |
|---|---|---|---|---|
| F07 | Free・Mental・SR | ○ / × / × | `Face(Consume(0), Shield(11), Turns(StatusKind.ShieldGainBlock, 2), Turns(StatusKind.CostHalf, 3))` | 同 `Shield(13)`、`CostHalf` 4 |
| L03 | Logic・Mental・N | × / × / × | `Face(Consume(2), Shield(1), Status(StatusKind.Motivation, 2))` | `Consume(2), Shield(2), Motivation 3` |
| L05 | Logic・Active・N | ○ / × / × | `Face(HpDirect(5), ParamByShield(100))` | `HpDirect(3), ParamByShield(110)` |
| L06 | Logic・Active・N | ○ / × / × | `Face(Consume(2), Shield(3), ParamByMotivation(120))` | `Consume(2), Shield(6), ParamByMotivation(150)` |
| L08 | Logic・Active・R | ○ / × / × | `Face(HpDirect(4), Status(Motivation, 3), ParamByShield(60))` | `HpDirect(4), Motivation 4, ParamByShield(80)` |
| L09 | Logic・Mental・R | × / × / × | `Face(Consume(0), Shield(6))` | `Consume(0), Shield(8)` |
| L11 | Logic・Active・R | ○ / × / × | `Face(HpDirect(4), ParamByShield(110))` | `HpDirect(3), ParamByShield(120)` |
| L12 | Logic・Mental・SR | ○ / × / ○ | `Face(Consume(5), Impression(4), Status(Motivation, 3))` | `Consume(5), Impression(5), Motivation 4` |
| L13 | Logic・Active・SR | ○ / × / × | `Face(HpDirect(3), ParamByShield(130), ScaleShield(50))` | `HpDirect(3), ParamByShield(180), ScaleShield(50)` |

`TestCardCatalog` の既定のコンストラクタに `Phase2Cards.All()` を足す。テストは 5 章の `Phase2CardsTests`。確かめ方: ビルドとテストが通る。

### タスク 5: フェーズ 3 のカード（好調）

触る: `T/Phase3Cards.cs`・`T/Phase3CardsTests.cs`（作成）、`T/TestCards.cs`（`public static ConditionDef AtLeast(ConditionSubject subject, int value)` を足す。`new ConditionDef(subject, Comparator.GreaterOrEqual, value)`）、`T/TestCardCatalog.cs`。

手順: `Phase3Cards`（`S01 = 31, S04 = 34, S06 = 36, S07 = 37, S11 = 41`、`AllIds`、`All()`）。`Plan` はすべて `Sense`、コストはすべて `Consume`。

| ID | 種別・レア | once | 通常版 | 強化版 |
|---|---|---|---|---|
| S01 | Mental・N | × | `Face(Consume(1), Shield(1), Status(StatusKind.GoodCondition, 2))` | `Consume(1), Shield(1), GoodCondition 3` |
| S04 | Active・N | ○ | `FaceWithCondition(Consume(2), AtLeast(ConditionSubject.GoodConditionTurns, 2), Param(10, goodScale: 200))` | 同条件 `Consume(2)`、`Param(15, goodScale: 200)` |
| S06 | Mental・N | ○ | `Face(Consume(0), Shield(5), Status(StatusKind.Excellent, 1))` | `Consume(0), Shield(6), Excellent 2` |
| S07 | Active・R | × | `Face(Consume(4), Param(6), Status(StatusKind.GoodCondition, 2))` | `Consume(4), Param(9), GoodCondition 3` |
| S11 | Active・R | ○ | `FaceWithCondition(Consume(6), AtLeast(ConditionSubject.GoodConditionTurns, 1), Param(27))` | 同条件 `Consume(6)`、`Param(38)` |

unique・startInHand はすべて ×。`TestCardCatalog` に `Phase3Cards.All()` を足す。テストは 5 章の `Phase3CardsTests`（好調・絶好調のコストの合成カードを含む）。確かめ方: ビルドとテストが通る。

### タスク 6: フェーズ 4 のカード（集中）

触る: `T/Phase4Cards.cs`・`T/Phase4CardsTests.cs`（作成）、`T/TestCards.cs`（`public static EffectDef OnActiveUse(EffectDef effect)` を足す。`new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.OnActiveCardUse, children: new[] { effect })`）、`T/TestCardCatalog.cs`。

手順: `Phase4Cards`（`S02 = 32, S03 = 33, S05 = 35, S08 = 38, S09 = 39, S10 = 40, S12 = 42, S13 = 43, S14 = 44`、`AllIds`、`All()`）。`Plan` は `Sense`、コストは `Consume`。

| ID | 種別・レア | once / unique / hand | 通常版 | 強化版 |
|---|---|---|---|---|
| S02 | Mental・N | × / × / × | `Face(Consume(1), Shield(1), Status(StatusKind.Focus, 2))` | `Consume(1), Shield(1), Focus 3` |
| S03 | Active・N | ○ / × / × | `Face(Consume(3), Param(6), Status(Focus, 2))` | `Consume(3), Param(8), Focus 3` |
| S05 | Active・N | ○ / × / × | `FaceWithCondition(Consume(2), AtLeast(ConditionSubject.Focus, 3), Param(5, hits: 2))` | 同条件 `Consume(2)`、`Param(9, hits: 2)` |
| S08 | Active・R | × / × / × | `Face(Consume(4), Param(6), Status(Focus, 2))` | `Consume(4), Param(9), Focus 3` |
| S09 | Active・R | × / × / × | `Face(Consume(4), Param(6), If(Status(Focus, 3), AtLeast(ConditionSubject.GoodConditionTurns, 1)))` | `Consume(4), Param(9), If(Status(Focus, 4), 同条件)` |
| S10 | Active・R | ○ / × / × | `Face(Consume(4), Param(17, focusScale: 150))` | `Consume(4), Param(23, focusScale: 200)` |
| S12 | Mental・SR | ○ / × / ○ | `Face(Consume(4), Status(Focus, 3), Status(StatusKind.GoodCondition, 2))` | `Consume(4), Focus 4, GoodCondition 3` |
| S13 | Mental・SR | ○ / ○ / × | `Face(Consume(4), Status(Focus, 2), OnActiveUse(Status(Focus, 1)))` | `Consume(3), Focus 3, OnActiveUse(Status(Focus, 1))` |
| S14 | Active・SSR | ○ / ○ / × | `Face(Consume(6), Param(15), If(Param(15), AtLeast(ConditionSubject.Focus, 3)))` | `Consume(6), Param(15), If(Param(34, focusScale: 150), AtLeast(Focus, 3))` |

`TestCardCatalog` に `Phase4Cards.All()` を足す。テストは 5 章の `Phase4CardsTests`（集中のコストの合成カード、好調×集中の式全体を含む）。確かめ方: ビルドとテストが通る。

### タスク 7: 試験

触る: `D/ExamSetup.cs`・`D/LessonResult.cs`・`T/ExamTests.cs`（作成）、`D/CardDefinition.cs`・`D/LessonEngine.cs`・`D/EffectExecutor.cs`・`D/LessonFormulas.cs`・`T/LessonFormulasTests.cs`・`D/LessonStateComparer.cs`（変更）。

手順:

1. 3.3 のとおり `ExamTurn`・`ExamSetup`・`LessonEndReason`・`LessonResult` を作り、`LessonSetup` を変える。
2. `LessonEngine.Start` の「試験は未対応」で例外を投げる分岐を消す。
3. `LessonFormulas` に `public static int ExamScore(int paramGain, int scoreBonusPercent)`（`FixedMath.ToInt(FixedMath.CeilDiv((long)paramGain * scoreBonusPercent, 100))`）を足す。`LessonFormulasTests` に `ExamScore(9, 150)` = 14・`(3, 120)` = 4・`(5, 130)` = 7・`(10, 0)` = 0 を足す。
4. `EffectExecutor.GainParam(LessonState state, int amount)`（`internal static`。連撃・`GainParamBy*`・E3 のすべてがここを通る）で、`amount <= 0` の早期 return の直後に、試験ならスコアに換算する（[lesson-card.md](../wiki/systems/lesson-card.md) 9.5）:
   ```csharp
   if (state.Setup.IsExam)
   {
       // スコア = ceil(パラメータ上昇値 × 最終スコアボーナス / 100)。ターンの倍率は state.Turn 番目（L1 の Turn 0 は 1 ターン目）
       var turns = state.Setup.Exam.Turns;
       var bonus = turns[Math.Max(0, state.Turn - 1)].ScoreBonusPercent;
       amount = LessonFormulas.ExamScore(amount, bonus);
   }
   ```
   換算の後は既存の処理（パーフェクト目標が 0 なので止めずに足す）にそのまま続ける。
5. `LessonEngine.EndTurn` の E5（`state.RemoveExpired();`）の後、E6 の前に足す: 試験で `Exam.EarlyEndScore > 0` かつ `state.ParamGained >= Exam.EarlyEndScore` なら `state.Phase = LessonPhase.Finished; return;`（[lesson-card.md](../wiki/systems/lesson-card.md) 9.5。P8 で使用数が残るかぎり、届いたあとも操作を受け付けるので、判定はここ 1 か所だけ）。
6. `public static LessonResult GetResult(LessonState state)`:
   - `!state.IsFinished` なら `InvalidOperationException("レッスンはまだ終わっていません。")`
   - 理由: 試験でなく `Setup.PerfectTarget > 0` かつ `state.ParamGained == Setup.PerfectTarget` なら `Perfect`。試験で `Exam.EarlyEndScore > 0` かつ `state.ParamGained >= Exam.EarlyEndScore` なら `EarlyEnd`。ほかは `TurnsOver`（最後のターンで届いたときもこの判定が先に当たる）
   - `new LessonResult(state.ParamGained, state.Turn, 理由, state.Hp)`
7. `LessonStateComparer.Describe` の `setup=` の行の直後に `exam=` の行を足す: 試験でなければ `exam=none`、試験なら `exam=<earlyEndScore>;<param>:<scoreBonusPercent>,<param>:<scoreBonusPercent>,…`（`param` は `(int)`）。
8. テストは 5 章の `ExamTests`。

境界: 試験で `ScoreBonusPercent` が 0 のとき、スコアは 0 のまま進む。確かめ方: ビルドとテストが通る。

### タスク 8: 全フェーズのカードでの再生のテスト

触る: `T/LessonReplayTests.cs`（作成）、`T/LessonEngineTests.cs`（変更。再生のテスト `Replay_*` と `RunLesson` を `LessonReplayTests` に移す）。

手順: 5 章の `LessonReplayTests` を書く。`RunLesson` は、全フェーズのカード（`Phase1Cards.AllIds`・`Phase2Cards.AllIds`・`Phase3Cards.AllIds`・`Phase4Cards.AllIds`、合わせて 36 枚）を 1 枚ずつ入れたデッキで、手札に使えるカードがあれば先頭から使い、なければスキップする（今の `RunLesson` と同じ操作の決め方）。確かめ方: ビルドとテストが通る。

## 5. テスト

実行するコマンドは CLAUDE.md にあるものだけ: `dotnet build Uta.slnx`、`dotnet test --project shared/Uta.Shared.Domain.Tests`。テスト名は `Method_Condition_Result`。期待値の計算の途中は、**フェーズごとの具体例**に書いた（実装と同じ丸め方を実装担当が選ばないため）。

共通: 合成カードは、書かれていなければ `Plan.Free`・`CardType.Mental`（効果が `Param` のものは `CardType.Active`）・`Rarity.N`・once なし・unique なし・強化版 null、`Card` の通常版は `Face(...)` で作る。試験の設定の種類は、書かれていなければ `LessonKind.MidExam` で、審査ターンの `ParamKind` は `Singing`・`Rhythm`・`Expression` を順に繰り返す。`PlayerState` の最大体力は 30、`NormalLesson`（パーフェクト目標 80）。「使う」は `Engine.Apply(state, LessonCommand.UseCard(1), random)`（`random` は `TestStates.NewRandom()`）。`hand: new[] { 1 }` か `{ 1, 2 }` でインスタンス ID 1 が対象のカード。`usesLeft` は、表や項目に指定がなければ **2**（そのターンの途中で止める）。ターンを終わらせたい項目だけ 1 と書いてある。

### `LessonStateComparerTests`（タスク 2）

- `Diff_SameState_Empty`: `PlayerState(...)` と `TestStates.Copy` の結果で `Diff` が空
- `Diff_DifferentHp_ReportsHpOnly`: `hp: 30` と `hp: 29` の 2 つの状態で、`Diff` が 1 件、文字列が `hp=` を含む
- `Diff_ExtraPersistent_Reports`: `persistents: new[] { new PersistentEffect(PersistentTrigger.OnMentalCardUse, new[] { TestCards.Status(StatusKind.Impression, 1) }, -1, -1, 0, false) }` を渡した状態と渡さない状態で、`Diff` が 1 件以上
- `Describe_Fresh_ContainsInitialValues`: `PlayerState(hp: 30, shield: 3)` の `Describe` が `hp=30`・`shield=3`・`turn=1`・`status.5=0/0` を含む

### フェーズ 2 の具体例（タスク 3・4）

| # | 初期状態 | 操作 | 期待値と途中の計算 |
|---|---|---|---|
| 2-1 | L05 通常版、体力 30、元気 8 | 使う | 体力 30 − 5 = 25（`HpDirect`）。`RatioParam(8, 100)` = ceil(8.00) = 8 → `ParamGained` 8。元気 8 のまま |
| 2-2 | L05 強化版（インスタンス 1 を `enhanced`）、体力 30、元気 8 | 使う | 体力 27。`RatioParam(8, 110)` = ceil(8.8) = 9 → `ParamGained` 9 |
| 2-3 | L13 通常版、体力 30、元気 7 | 使う | 体力 27。効果 1: `RatioParam(7, 130)` = ceil(9.1) = 10 → `ParamGained` 10（元気 7 のまま参照）。効果 2: `ScaledShield(7, 50)` = floor(3.5) = 3 → 元気 3。強化版（180%）は ceil(12.6) = 13、元気 3 |
| 2-4 | L06 通常版、体力 30、元気 0、やる気 5 | 使う | `Consume(2)` は元気 0 なので体力 28。`AddShield(3)` = 3 + やる気 5 = 8 → 元気 8。`RatioParam(5, 120)` = ceil(6.00) = 6 → `ParamGained` 6 |
| 2-5 | L08 通常版、体力 30、元気 10、やる気 0 | 使う | 体力 26。やる気 +3 → 3。`RatioParam(10, 60)` = ceil(6.00) = 6 → `ParamGained` 6（元気 10 のまま） |
| 2-6 | L03 通常版 2 枚（`hand: { 1, 2 }`、`usesLeft: 2`）、体力 30、元気 0、やる気 0 | 1 枚目、2 枚目の順に使う | 1 枚目: `Consume(2)` は体力 28、元気 1（1 + 0）、やる気 2。2 枚目: `Consume(2)` は元気 1 を払い体力 27、元気 0 → `AddShield(1)` = 1 + やる気 2 = 3、やる気 4 |
| 2-7 | F07 通常版（インスタンス 1）、L09 通常版（インスタンス 2）、`hand: { 1, 2 }`、`usesLeft: 3`、元気 0 | F07、L09 の順に使う | F07 後: 元気 11、`ShieldGainBlock` 2、`CostHalf` 3。L09 の `AddShield(6)` は元気増加無効で 0 → 元気 11 のまま |
| 2-8 | F07 通常版、`DebuffGuard` 1、`usesLeft: 2` | 使う | 低下状態無効が 1 → 0、`ShieldGainBlock` は 0 のまま（打ち消し）。元気 11、`CostHalf` 3（低下状態ではない） |

### `Phase2OpsTests`（タスク 3。合成カードは `Consume(0)` の 1 コストで、効果を 1〜2 個並べる）

- `AddFixedShield_IgnoresMotivation`: やる気 3・元気 0・`FixedShield(4)` → 元気 4
- `AddFixedShield_ShieldGainBlocked_NoChange`: `ShieldGainBlock` 2・元気 2・`FixedShield(4)` → 元気 2
- `RecoverHp_AddsValue` / `RecoverHp_CapsAtMaxHp`: 体力 10・`Recover(5)` → 15／体力 28・`Recover(5)` → 30
- `ScaleShield_Percent_Floors`: 元気 7 で `ScaleShield(50)` → 3／`ScaleShield(0)` → 0／`ScaleShield(100)` → 7
- `SetShield_ShieldGainBlocked_StillSets`: `ShieldGainBlock` 2・元気 3・`SetShield(9)` → 9／`SetShield(0)` → 0
- `GainParamByShield_WithGoodCondition`: 元気 7・好調 2・`ParamByShield(130)` → ceil(9.1) = 10、× 1.5 = 15 → `ParamGained` 15
- `GainParamByMotivation_Ratio`: やる気 5・`ParamByMotivation(120)` → 6
- `Cost_Impression_PaysAndGates`: `StatusCost(CostKind.Impression, 2)`・好印象 1 → `CanUse` は `CannotPayCost`／好印象 3 → `Ok`、使うと好印象 1
- `Cost_Motivation_PaysAndGates`: `StatusCost(CostKind.Motivation, 2)` で同じ形（やる気 1 → `CannotPayCost`、3 → `Ok` で 1）
- `DebuffGuard_CancelsCostDouble`: `DebuffGuard` 1 で `Status(StatusKind.CostDouble, 2)` を付与 → `CostDouble` 0・`DebuffGuard` 0
- `DebuffGuard_NoGuard_AppliesWithSkipMark`: `DebuffGuard` 0 → `CostDouble` 2。プレイヤー操作中の初付与なので `IsSkipNextDecrement` が真
- `DebuffGuard_NonDebuffStatus_NotConsumed`: `DebuffGuard` 1 で `Status(CostHalf, 2)` → `CostHalf` 2・`DebuffGuard` 1
- `DebuffGuard_GuardItself_Accumulates`: `Status(StatusKind.DebuffGuard, 2)` を付与 → 2
- `DebuffGuard_TwoGuards_CancelTwiceThenApplies`: `DebuffGuard` 2 で、`Status(ShieldGainBlock, 1)` を 3 つ並べた 1 枚の合成カードを使う → `ShieldGainBlock` 1（最初の 2 回が打ち消され、3 回目が付く）・`DebuffGuard` 0

### `StatusSetTests`・`LessonFormulasTests`・`CardDefinitionValidatorTests`（タスク 3）

- 低下状態無効は `StatusSet.Add` が `internal` でテストから直接呼べない（`shared/` に `InternalsVisibleTo` は無い）ので、`StatusSetTests` には足さず、`Phase2OpsTests` で合成カードを使って確かめる（上の `DebuffGuard_*`）
- `LessonFormulasTests`: `FixedShieldGain(4, false)` = 4・`(4, true)` = 0／`ScaledShield(7, 50)` = 3・`(7, 0)` = 0・`(7, 100)` = 7・`(1, 50)` = 0
- `CardDefinitionValidatorTests`: 4 つの `Op` で `Value` 0 → 「の値が 1 未満」が 1 件（`Theory`）／`ScaleShield` 100 → 0 件・101 → 「の値が 100 を超える」／`SetShield` 0 → 0 件・−1 → 「の値が負」／`AddPersistent`（`LessonStart`）の子が `DrawCards(1)` → 「ゾーンを操作している」を含む 1 件／同じ `AddPersistent` の子が `AddStatus` → 0 件／`TurnStart` の子が `DrawCards(1)` → 0 件（テスト名は `Validate_<条件>_<結果>`）。フェーズ 2 の 9 枚の検査はタスク 4

### `Phase2CardsTests`（タスク 4）

上の 2-1〜2-8 を `Fact` にする（初期状態の指定は表のとおり）。追加:

- `Validator_Phase2Cards_NoErrors`: `Phase2Cards.AllIds` の 9 枚が `CardDefinitionValidator.Validate` を通る
- `Start_L12InDeck_StartsInHand`: L12 1 枚と F01 8 枚のデッキ（L12 は ID 順で先頭）で `Engine.Start` → 手札 3 枚にインスタンス 1（L12）がある
- `UseCard_L12_AddsImpressionAndMotivation`: L12 通常版・体力 30・元気 0 → 体力 25、好印象 4、やる気 3（`usesLeft: 2` なので E3 の前）
- `UseCard_L09_AddsMotivationToShield`: やる気 3・L09 通常版 → 元気 9
- `UseCard_L11_Both`: L11 通常版・元気 5 → 体力 26、`ParamGained` ceil(5.5) = 6／強化版（120%）・元気 5 → 体力 27、`ParamGained` 6（ceil(6.00)）

### フェーズ 3 の具体例（タスク 5）

| # | 初期状態 | 操作 | 期待値と途中の計算 |
|---|---|---|---|
| 3-1 | S11 通常版、体力 30、好調 2 | 使う | `CanUse` は `Ok`。体力 24。好調倍率 1 + 0.5 = 1.5 → 27 × 1.5 = 40.5 → ceil = 41 → `ParamGained` 41。強化版は 38 × 1.5 = 57 |
| 3-2 | S11 通常版、好調 0 | `CanUse` | `UseConditionNotMet` |
| 3-3 | S04 通常版、体力 30、好調 2 | 使う | `CanUse` は `Ok`（好調 ≥ 2）。体力 28。`goodScale` 200% → 倍率 1 + 0.5 × 2 = 2.0 → 10 × 2.0 = 20 → `ParamGained` 20。強化版 15 × 2.0 = 30 |
| 3-4 | S04 通常版、好調 3、絶好調 1 | 使う | 倍率 1 + (0.5 + 0.1 × 3) × 2 = 2.6 → 10 × 2.6 = 26 → `ParamGained` 26。好調 1 では `UseConditionNotMet` |
| 3-5 | S07 通常版、体力 30、好調 3、絶好調 2 | 使う | 体力 26。倍率 1 + 0.5 + 0.1 × 3 = 1.8 → 6 × 1.8 = 10.8 → ceil = 11 → `ParamGained` 11（効果の記載順で、パラメータの後に好調が付く）。そのあと好調 3 + 2 = 5 |
| 3-6 | S06 通常版、好調 2、`usesLeft: 1`（ターンが終わる） | 使う | 絶好調 1（プレイヤー操作中の初付与で、このターンの E5 では減らない）、好調 2 → 1。次のターンの入力待ち（`Turn` 2）で絶好調 1・好調 1。さらに `Skip` で絶好調 0・好調 0 |
| 3-7 | S01 通常版、`usesLeft: 1` | 使う | 好調 2（E5 で減らない）→ 次のターン 2。`Skip` で 1 |

`Phase3CardsTests`: 3-1〜3-7 を `Fact`（3-1 と 3-3 は通常版・強化版の両方）にする。追加:

- `Validator_Phase3Cards_NoErrors`
- `Cost_GoodConditionTurns_PaysAndGates`: 合成カード `Face(StatusCost(CostKind.GoodConditionTurns, 2))`（効果なし）、好調 1 → `CannotPayCost`／好調 3 → `Ok`、使うと好調 1
- `Cost_ExcellentTurns_PaysAndGates`: `StatusCost(CostKind.ExcellentTurns, 1)`、絶好調 0 → `CannotPayCost`／1 → `Ok` で 0

### フェーズ 4 の具体例（タスク 6）

| # | 初期状態 | 操作 | 期待値と途中の計算 |
|---|---|---|---|
| 4-1 | S10 通常版、体力 30、集中 4 | 使う | 体力 26。カード値 = 17 + 4 × 1.5 = 23 → `ParamGained` 23。強化版: 23 + 4 × 2.0 = 31 |
| 4-2 | S05 通常版、体力 30、集中 3 | 使う | `CanUse` は `Ok`。体力 28。1 回 = 5 + 3 = 8、2 回（連撃は 1 回ずつ計算して足す）で 16 → `ParamGained` 16。集中 2 では `UseConditionNotMet`。強化版: 9 + 3 = 12 × 2 = 24 |
| 4-3 | S14 通常版、体力 30、集中 3 | 使う | 体力 24。効果 1 = 15 + 3 = 18、効果 2（集中 ≥ 3 の条件を満たす）= 18 → 36。集中 2 では効果 2 を飛ばして 15 + 2 = 17 |
| 4-4 | S14 強化版、集中 3 | 使う | 効果 1 = 15 + 3 = 18、効果 2 = 34 + 3 × 1.5 = 38.5 → ceil = 39。計 57 |
| 4-5 | S03 通常版、集中 0 | 使う | `Param(6)` = 6 → `ParamGained` 6。そのあと集中 2。強化版: 8、集中 3 |
| 4-6 | S09 通常版、好調 2、集中 0 | 使う | 条件（好調 ≥ 1）は P2 で判定され満たす。パラメータ = 6 × 1.5 = 9 → `ParamGained` 9、そのあと集中 3。好調 0 では `ParamGained` 6・集中 0 のまま |
| 4-7 | S13 通常版（インスタンス 1）、S08 通常版（インスタンス 2）、`hand: { 1, 2 }`、`usesLeft: 2`、体力 30 | S13、S08 の順に使う | S13 後: 体力 26、集中 2、持続効果 1 件（S13 自身は反応しない）。S08: P5 で集中 +1 → 3、パラメータ = 6 + 3 = 9 → `ParamGained` 9、そのあと集中 +2 → 5 |
| 4-8 | S12 を含むデッキ（インスタンス 1 が S12、F01 が 8 枚） | `Engine.Start` → 使う | 手札にインスタンス 1。S12 通常版: 体力 **30 − 4 = 26**、集中 3、好調 2 |

好調×集中の式全体（タスク 6 の `Phase4CardsTests`）:

| # | 初期状態 | 操作 | 期待値と途中の計算 |
|---|---|---|---|
| 4-9 | S10 強化版、集中 4、好調 2、絶好調 0 | 使う | カード値 31 × 1.5 = 46.5 → ceil = 47 |
| 4-10 | S10 強化版、集中 4、好調 2、絶好調 1 | 使う | 倍率 1 + (0.5 + 0.1 × 2) = 1.7 → 31 × 1.7 = 52.7 → ceil = 53 |
| 4-11 | S04 通常版、集中 4、好調 2 | 使う | カード値 10 + 4 = 14、倍率 1 + 0.5 × 2 = 2.0 → 28 |

`Phase4CardsTests`: 4-1〜4-11 を `Fact`（4-8 は `Start` を通す）にする。追加:

- `Validator_Phase4Cards_NoErrors`
- `Cost_Focus_PaysAndGates`: 合成カード `Face(StatusCost(CostKind.Focus, 3))`（効果なし）、集中 2 → `CannotPayCost`／5 → `Ok`、使うと 2
- `UseCard_S02_AddsFocus`: S02 通常版・体力 30・元気 0 → 体力 29、元気 1、集中 2

### 試験の具体例（タスク 7。`ExamTests`）

試験の設定: `new ExamSetup(new[] { new ExamTurn(ParamKind.Singing, 150), new ExamTurn(ParamKind.Rhythm, 120), new ExamTurn(ParamKind.Expression, 100), new ExamTurn(ParamKind.Singing, 100) }, 0)`、`new LessonSetup(LessonKind.MidExam, ParamKind.Nothing, 4, 0, 0, exam)`。

| # | 初期状態 | 操作 | 期待値と途中の計算 |
|---|---|---|---|
| 6-1 | F01 通常版、`turn: 1`、`usesLeft: 2` | 使う | `Param(9)` = 9 → スコア ceil(9 × 150 / 100) = ceil(13.5) = 14 → `ParamGained` 14 |
| 6-2 | `turn: 2`、好印象 3、手札なし | `Skip` | E3 で好印象のパラメータ 3 → ceil(3 × 120 / 100) = ceil(3.6) = 4 → `ParamGained` 4 |
| 6-3 | 合成カード（`Consume(0)`、`Param(5, hits: 2)`）、`turn: 1`、審査ターンの倍率を `{130, 100, 100, 100}`（種類は上と同じ）にした試験の設定 | 使う | 連撃は 1 発ごとに換算する: ceil(5 × 1.3) = ceil(6.5) = 7 を 2 回で 14（まとめて 10 を換算すると 13 になる） |

早期終了の設定: 4 ターン、倍率 200% を 4 つ、早期終了スコア 30（`ExamSetup(turns, 30)`）。

| # | 初期状態 | 操作 | 期待値 |
|---|---|---|---|
| 6-4 | F01 通常版 9 枚のデッキ、`Engine.Start` 後 | 1 ターン目に `Hand[0]` を使う、2 ターン目も `Hand[0]` を使う | 1 ターン目: 9 × 200% = 18（< 30）で続く（`Turn` 2）。2 ターン目: 計 36 ≥ 30 → E5 のあとで終了（`IsFinished`、残り 2 ターンあり）。`GetResult`: `Gained` 36・`LastTurn` 2・`Reason` `EarlyEnd`・`Hp` 30 − 4 − 4 = 22 |
| 6-5 | 6-4 と同じで `maxTurns` 2（倍率 `{200, 200}`、`ExamSetup(turns, 30)`） | 同じ | 2 ターン目で終了、`Reason` は `TurnsOver` ではなく `EarlyEnd` |
| 6-6 | 合成カード（`Consume(0)`、`Param(40)`、`Uses(1)`）と F01 通常版、倍率 100%、早期終了スコア 30、`usesLeft: 1` | 合成カード、F01 の順に使う | 合成カード後: `ParamGained` 40 だが `IsFinished` は偽（`Phase` が `Player`、`UsesLeft` 1）。F01 後: 49、終了、`EarlyEnd` |

そのほか:

- `GetResult_TurnsOver`: 試験（倍率 `{100, 100}`、早期終了スコア 0、`maxTurns` 2）、F01 通常版 5 枚のデッキ、体力 20 / 最大体力 30 で `Engine.Start` し、`Skip` 2 回 → `Gained` 0・`LastTurn` 2・`Reason` `TurnsOver`・`Hp` 24（スキップの回復 2 × 2）
- `GetResult_Perfect`: 通常のレッスン（パーフェクト 80）、`paramGained: 75`、F01 通常版を使う → `Gained` 80・`LastTurn` 1・`Reason` `Perfect`
- `GetResult_NotFinished_Throws`: `Start` 直後の状態に `GetResult` → `InvalidOperationException`
- `Start_Exam_DoesNotThrow`: 試験の設定で `Engine.Start` が `Phase.Player`・`Turn` 1 で止まる
- `LessonSetup_*`: 種類 `MidExam` で `exam` が null → `ArgumentException`／種類 `Normal` で `exam` あり → `ArgumentException`／試験で `maxTurns` が `Turns.Count` と違う → `ArgumentOutOfRangeException`／試験で `perfectTarget` 5 → `ArgumentOutOfRangeException`／試験で `targetParam` が `Nothing` → 例外なし／試験でなく `targetParam` が `Nothing` → `ArgumentOutOfRangeException`
- `ExamSetup_*`: `turns` が null → `ArgumentNullException`／空・`earlyEndScore` が負 → `ArgumentOutOfRangeException`／`ExamTurn` の `param` が `Nothing`・`scoreBonusPercent` が負 → `ArgumentOutOfRangeException`
- `Comparer_ExamSetup_DescribesExamLine`: 試験の状態の `Describe` が `exam=0;1:150,2:120,3:100,1:100` を含む。通常のレッスンは `exam=none`

### `LessonReplayTests`（タスク 8）

`LessonEngineTests` の今の `Replay_*` 3 本を移し、デッキを全フェーズの 36 枚にする（`TestCardCatalog` を使う）。比べるのは `LessonStateComparer.Describe` の結果（各操作の後の状態を連結した文字列）。

- `Replay_SameSeedAndCommands_SameResult`
- `Replay_DifferentAttempt_DifferentResult`
- `Replay_RestoredMidway_SameResultAsContinuousRun`（3 ターン目で `TestStates.Copy` で作り直し、乱数も `Xoshiro256StarStar` の状態から作り直す）
- `Replay_Exam_SameSeedAndCommands_SameResult` と `Replay_Exam_RestoredMidway_SameResult`: 同じ操作の決め方で、試験の設定（`maxTurns` 7、倍率 `{100, 120, 150, 200, 100, 150, 200}`、早期終了スコア 300、種類は `ParamKind` の 3 つを順に繰り返す）、デッキは 36 枚、体力 40 / 最大体力 40
- `Replay_FinalDiff_Empty`: 同じシードで 2 回走らせた最後の状態の `Diff` が空

## 6. 未決事項

なし。

## 6.1 実装メモ（2026-11-01）

- 実装後のテストは 170 本成功、ビルド警告 0・エラー 0。コードレビューは Critical・Important なし。Minor のうち `default(ExamTurn)` を `ExamSetup` で弾くことを足した。
- 計画から外れた点: `ExamSetup` は `turns` をコピーして持つ。`LessonReplayTests` に 36 枚の重複なしを確かめる 1 本を足し、復元点に届いたことも確かめる。テスト名のまとめ方（`Theory` にした）。
- 見送った Minor: `Describe` が予約・持続効果の中身を `Op` の数値の並びでしか比べない／`Diff` が位置で比べる／項目ごとの網羅テスト／固定シードのゴールデン値。IL2CPP の実機確認（`client/`）の計画で足す。
- テストのカード ID は、ユーザーの判断でマスタの値（S = 100 + 番号、L = 200 + 番号）に合わせた（実装後に定数を変更）。このため 4章・5章の表の ID（`L03 = 13`、`S01 = 31` など）は旧値。一致テスト自体は別の計画。

## 7. 実装が終わったあとの wiki の更新（済み）（メインのセッションが行う。実装担当には渡さない）

- [lesson-phases.md](../wiki/architecture/lesson-phases.md): 冒頭の「フェーズ 0・1 は実装済み」を 2〜4・6 まで広げる。1章の「テストのカードの置き場」の「フェーズ 2 の実装計画で決める」を、決めたシグネチャ（3.1）への参照に直す。8章の「今の `Summarize` は置き換える」を「置き換えた」に直す
- [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md): `ExamSetup`・`LessonResult`・`LessonStateComparer` の [未実装] を外す。実装メモに足す
- [lesson-card.md](../wiki/systems/lesson-card.md): 実装メモの「未対応の決定」から、実装した項目を消す
- [lesson-enums.md](../wiki/architecture/lesson-enums.md): 1章の「フェーズ」の列の実装済み、実装メモの `CardDefinitionValidator` の差を更新する
- [game-spec.md](../wiki/overview/game-spec.md): 順 3 の表に実装済みであることを書く
- [produce-domain.md](../wiki/architecture/produce-domain.md) 7章・[lesson-phases.md](../wiki/architecture/lesson-phases.md) 8章: 比べる関数の置き場を「`Uta.Shared.Domain` の `LessonStateComparer`」に直す
- [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) 5章: `LessonFormulas` の行の 9.5 を `ExamScore` に直す
- [lesson-card.md](../wiki/systems/lesson-card.md) 実装メモの「未対応の決定」を実装の状態に合わせて書き直す
