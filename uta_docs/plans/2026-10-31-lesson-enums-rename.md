---
title: 実装計画: レッスンの enum と効果の列を lesson-enums.md に合わせる
status: done
updated: 2026-11-01
spec: ../wiki/architecture/lesson-enums.md, ../wiki/architecture/lesson-domain-types.md
---

# 実装計画: レッスンの enum と効果の列を lesson-enums.md に合わせる

## 1. 目的と前提

`shared/Uta.Shared.Domain/Lesson` を [lesson-enums.md](../wiki/architecture/lesson-enums.md)（正）と [lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) に合わせる。次の 4 つを、4 つのタスクに分けて行う。**どのタスクも振る舞いを変えない**（フェーズ 1 のカード 13 枚の結果は変わらない）。

1. レッスンの 6 つの enum の名前・数値と、`EffectDef` の列の持ち方（3.1〜3.5）
2. 元気の名前を `Genki` から `Shield` に変える（3.6）
3. 既にある 5 つの enum の 0 に `Nothing` を足して振り直す（3.7）
4. `LessonRules` を `static class` の `const` にする（3.8）

- 関係する ADR: [ADR 0007](../wiki/decisions/0007-masterdata-with-addons.md)（列の型）、[ADR 0008](../wiki/decisions/0008-shared-domain-and-onion.md)（共有ドメイン）
- 背景: 2026-10-31 の設計書レビュー（lesson-card.md の Critical C1・C2）と、2026-11-01 の architecture/ のレビューの判断
- 決めないこと（範囲外）:
  - フェーズ 2 以降の `Op` の処理の実装、`EveryNTurns`・`ScheduleEffect` の処理、マスタ（`masterdata/`）の作成
  - **`Op` ごとの `Value`・`Arg`・参照の列の必須・不要の検証**。lesson-enums.md「決まっていること」は「`Op` ごとに使わない列を検証する」としているが、この計画は `Status`・`Trigger` の列だけを検証する。`Value`・`Arg`・参照の表は、マスタの最初の計画の前に lesson-enums.md 1章で埋めてから入れる（2026-11-01。ユーザーの判断）

## 2. 触るファイル

| 操作 | パス | タスク | 中身 |
|---|---|---|---|
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonEnums.cs` | 1・2・3 | 3.1・3.6・3.7 |
| 変更 | `shared/Uta.Shared.Domain/Lesson/CardDefinition.cs` | 1・3・4 | `EffectDef` に `Status`・`Trigger` を足す（3.2）。`LessonSetup` の検査（3.7）。`LessonRules`（3.8） |
| 変更 | `shared/Uta.Shared.Domain/Lesson/StatusSet.cs` | 1・2 | 3.5 と、enum のメンバーの参照（`ShieldGainBlock`。タスク 1）。enum 以外の識別子（タスク 2。3.6） |
| 変更 | `shared/Uta.Shared.Domain/Lesson/EffectExecutor.cs` | 1・2 | 3.3 と、enum のメンバーの参照（タスク 1）。enum 以外の識別子（タスク 2。3.6） |
| 変更 | `shared/Uta.Shared.Domain/Lesson/CardDefinitionValidator.cs` | 1・2・3 | 3.4。名前（3.6）。カードの区分の検査（3.7） |
| 変更 | `shared/Uta.Shared.Domain/Lesson/ConditionEvaluator.cs` | 1・2 | enum のメンバーの参照（`ConditionSubject.Shield` など。タスク 1）。enum 以外の識別子（タスク 2）。`Nothing` の扱いは 3.4 の最後 |
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonEngine.cs` | 1・2・4 | 名前の変更に追従する。`LessonRules`（3.8） |
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonState.cs` | 2 | 名前（3.6） |
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonFormulas.cs` | 2 | 名前（3.6） |
| 変更 | `shared/Uta.Shared.Domain.Tests/Lesson/Phase1Cards.cs` | 1・2 | 4 章の対応表のとおりに書き直す。補助の `Genki(int)` は `Shield(int)` にする |
| 変更 | `shared/Uta.Shared.Domain.Tests/Lesson/LessonEngineTests.cs` | 1・2・4 | `Validator_UnsupportedOp_ReportsError` の書き直し（3.4）。名前（3.6）。`new LessonEngine(Catalog)`（3.8） |
| 変更 | `shared/Uta.Shared.Domain.Tests/Lesson/LessonFormulasTests.cs` | 2 | 名前（3.6） |
| 作成 | `shared/Uta.Shared.Domain.Tests/Lesson/CardDefinitionValidatorTests.cs` | 1・3 | 5.1 |
| 作成 | `shared/Uta.Shared.Domain.Tests/Lesson/StatusSetTests.cs` | 1 | 5.2 |
| 変更 | `uta_docs/wiki/architecture/lesson-enums.md`・`lesson-domain-types.md` | 4 の後 | lesson-enums.md の実装メモの「コードはまだ古い名前・数値」と、lesson-domain-types.md の「今のコードは…」（2.1 と `LessonRules` の行）を、実装に合わせて消す。[未実装] の印（`ExamSetup`・`LessonResult`）は残す（フェーズ 6 で作るため）。この計画で決めた細部（`StatusSet` の添字 0、`LessonSetup` の `Nothing` の検査、検証のエラーの文言は 3.4 を見る、と書く）を lesson-domain-types.md の該当の行に 1 行ずつ足す |

## 3. 型とシグネチャ

### 3.1 レッスンの 6 つの enum（タスク 1）

`LessonEnums.cs` の `EffectOp`・`PersistentTrigger`・`StatusKind`・`CostKind`・`ConditionSubject`・`Comparator` を、[lesson-enums.md](../wiki/architecture/lesson-enums.md) 1〜5章の表の**名前と数値どおり**に書き直す（値は明示する）。0 は `Nothing`。

- ファイル先頭のコメントを次に置き換える: `// 名前・数値の正は uta_docs/wiki/architecture/lesson-enums.md。マスタ・保存の値に使うので数値は変えない。追加は末尾に足す。`
- 各 enum の `<summary>` に「lesson-enums.md N章」を書く
- `RatioSource`・`ResourceKind` は消す
- タスク 1 では enum のメンバーの名前を lesson-enums.md の新しい名前（`AddShield`・`AddFixedShield`・`ShieldGainBlock`・`ConditionSubject.Shield` など）にする。**enum のメンバーを参照しているところ（`ConditionEvaluator`・`StatusSet`・`EffectExecutor`・`CardDefinitionValidator`・`LessonEngine`・テスト）は、タスク 1 の中で新しい名前に直す**（直さないとビルドが通らない）。enum 以外の識別子（`LessonState.Genki`・`GenkiGain`・引数・ローカル変数など）はタスク 2 で直す

### 3.2 `EffectDef`（タスク 1）

```csharp
public EffectDef(
    EffectOp op,
    StatusKind status = StatusKind.Nothing,
    PersistentTrigger trigger = PersistentTrigger.Nothing,
    int value = 0,
    int arg1 = 0,
    int arg2 = 0,
    int arg3 = 0,
    int refCardId = 0,
    IReadOnlyList<ConditionDef> conditions = null,
    IReadOnlyList<EffectDef> children = null)
```

プロパティ `public StatusKind Status { get; }` と `public PersistentTrigger Trigger { get; }` を `Op` の次に足す。ほかは変えない。呼び出しは、`op` のあとをすべて名前付き引数で書く（位置引数の `0` が enum に暗黙に変換されるのを避けるため）。

### 3.3 `EffectExecutor` の引数の読み方（タスク 1）

| 旧 | 新 |
|---|---|
| `Param`（`Value`・`Arg1`〜`Arg3`） | `GainParam`（同じ） |
| `ParamByRatio`（`Value`＝%、`Arg1`＝`RatioSource`） | `GainParamByImpression`（`Value`＝%。参照元は好印象で固定）。`GainParamByShield`・`GainParamByMotivation` はフェーズ 2 なので処理しない（`default` の例外のまま） |
| `AddTurns` / `AddStack`（`Value`＝量、`Arg1`＝ステータス） | `AddStatus`（`effect.Status`、`Value`＝量） |
| `AddGenki`（`Value`） | `AddShield`（`Value`） |
| `Draw` / `AddUses`（`Value`） | `DrawCards` / `AddCardUses`（`Value`） |
| `Persistent`（`Arg1`＝トリガー、`Arg2`＝ターン数、`Arg3`＝回数） | `AddPersistent`（`effect.Trigger`、`Arg1`＝ターン数、`Arg2`＝回数）。0 は無制限（今と同じ）。「プレイヤー操作中に付与した」の判定は、旧 `effect.Arg2 > 0` を `effect.Arg1 > 0` にする |

`ReadRatioSource` は消し、`GainParamByImpression` は好印象の値（`state.Statuses.Get(StatusKind.Impression)`）を直接読む。

### 3.4 `CardDefinitionValidator`（タスク 1）

検査の順と打ち切り:

1. 効果ごとに、まず `Op` が動かせるかを見る（`IsSupported` の `case` をこの一覧に置き換える）。動かせる `Op` は `GainParam`・`GainParamByImpression`・`AddShield`・`AddStatus`・`DrawCards`・`AddCardUses`・`AddPersistent`。それ以外（`Nothing` を含む）は `prefix + "未対応の効果 " + effect.Op` を 1 件足し、**その効果の残りの検査はしない**（今と同じ）
2. 動かせる `Op` には、下の検査を上から順にすべて行い、当たったものを 1 件ずつ足す（1 つの効果から複数のエラーが出てよい）

| 検査 | 対象 | エラーの文言（`prefix +` のあと） |
|---|---|---|
| `Value`・`Arg1`〜`Arg3` のどれかが負 | 動かせる `Op` すべて | `effect.Op + " の値が負"` |
| `Value` が 1 未満 | `AddStatus`・`DrawCards` | `effect.Op + " の値が 1 未満"` |
| `Status` が `GoodCondition`〜`DebuffGuard` の外、または `FocusBoost`・`ImpressionBoost` | `AddStatus` | `effect.Op + " のステータスが不正"` |
| `Trigger` が `LessonStart`〜`TurnEnd` の外、または `EveryNTurns` | `AddPersistent` | `effect.Op + " のトリガーが不正"` |
| `Status` が `Nothing` でない（`AddStatus` 以外）、`Trigger` が `Nothing` でない（`AddPersistent` 以外） | 動かせる `Op` すべて | `effect.Op + " に使わない列がある"` |
| 子が 0 個 | `AddPersistent` | `"持続効果に発動する効果が無い"` |
| 自分が `AddPersistent` で、持続効果の子として検査されている（`insidePersistent` が `true`。入れ子の深さは 1。lesson-enums.md「決まっていること」） | 子の `AddPersistent` | `"持続効果の中に持続効果がある"` |

- 子の効果も同じ表で検査する。入れ子のエラーは**子を検査するときにだけ**足す（今のコードと同じ。外側の `AddPersistent` では足さない）。5.1 の 7 は、内側の `AddPersistent` から 1 件だけ出る
- コスト・条件（今は検査していないので足す）: 通常版・強化版の各面で、コストの `Kind` が `Consume`〜`Motivation` の外なら `"コストの種類が不正"`、使用条件と効果内条件の `Subject` が `GoodConditionTurns`〜`ClearTargetPercent` の外、または `Comparator` が `GreaterOrEqual`〜`Equal` の外なら `"条件が不正"`。範囲の確かめ方は、既存の `IsDefined` と同じ形の private メソッドにする
- `IsDefined(RatioSource)` は消す。`IsDefined(StatusKind)`・`IsDefined(PersistentTrigger)` の下限・上限を新しい値に合わせる（下限は 1。`Nothing` を含めない）
- **`Nothing` が実行時に来たとき**: `ConditionEvaluator`・`LessonEngine.ToStatus`・`EffectExecutor` の `switch` には `Nothing` の `case` を足さない。`default` の例外のままにする（読み込みの検査で弾くので、実行時には来ない）
- 既存のテスト `Validator_UnsupportedOp_ReportsError` は、`new EffectDef(EffectOp.ScaleResource, 50)` を `new EffectDef(EffectOp.ScaleShield, value: 50)` に変える。`ScaleShield` は動かせない `Op` なので、エラーは 1 件のまま（`Assert.Single` を変えない）

### 3.5 `StatusSet`（タスク 1）

- `KindCount` は `(int)StatusKind.DebuffGuard + 1`（13）。添字は `(int)kind` のまま。添字 0（`Nothing`）は使わない
- `Get(StatusKind.Nothing)` は 0、`IsSkipNextDecrement(StatusKind.Nothing)` は `false` を返す（配列の 0 番目は常に 0・`false`）
- `Add` に `StatusKind.Nothing` が来たら `ArgumentOutOfRangeException`
- 保存した状態から作り直すコンストラクタは、長さ 13 の並びを受け取る。`values[0]` が 0 でない、または `skipNextDecrement[0]` が `true` なら `ArgumentException`

### 3.6 元気の名前（タスク 2）

`shared/` のコードとテストで、`Genki` を含む識別子をすべて置き換える。規則:

- enum のメンバー（`GenkiBlock`・`AddGenki`・`AddFixedGenki`・`ConditionSubject.Genki` など）とその参照は、タスク 1 で済んでいる
- `genkiBlocked` → `shieldGainBlocked`
- それ以外は、大文字の `Genki` → `Shield`、小文字の `genki` → `shield`（`GenkiGain`・`PaysGenkiBeforeHp`・`fromGenki`、`LessonState` のプロパティ、ローカル変数、引数、テストのメソッド名 `UseCard_ConsumeCost_PaysGenkiBeforeHp`、`Phase1Cards` の補助 `Genki(int)` を含む）
- コメントの「元気」（日本語）はそのまま
- 完了の確かめ方: `grep -rni genki shared --include=*.cs --exclude-dir=obj --exclude-dir=bin` が 0 件

### 3.7 既にある 5 つの enum の `Nothing`（タスク 3）

- `Plan`・`CardType`・`Rarity`・`ParamKind`・`LessonKind` を、[lesson-enums.md](../wiki/architecture/lesson-enums.md) 6章の数値に振り直す（0 に `Nothing` を足し、ほかを 1 つずつずらす。値は明示する）。`LessonPhase` は変えない
- `ParamKind` を配列の添字に使っているところは今は無い（使うときは lesson-enums.md 6章のとおり大きさ 4 の配列にする）
- `CardDefinitionValidator`: カードの `Plan`・`Type`・`Rarity` のどれかが `Nothing` なら `"カードの区分が不正"` を 1 件足す
- `LessonSetup` のコンストラクタ: `kind` か `targetParam` が `Nothing` なら `ArgumentOutOfRangeException`

### 3.8 `LessonRules`（タスク 4）

```csharp
/// <summary>レッスンのルールの値（lesson-card.md 4.1）。差し替えない（lesson-domain-types.md 2章）。</summary>
public static class LessonRules
{
    public const int DrawPerTurn = 3;
    public const int FirstTurnMinHand = 3;
    public const int StartInHandMax = 5;
    public const int BaseUses = 1;
    public const int SkipHpRecover = 2;
}
```

- `LessonEngine` のコンストラクタは `public LessonEngine(ICardCatalog catalog)` にし、フィールド `rules` を消す。`rules.X` は `LessonRules.X` にする
- テストの `new LessonEngine(Catalog, LessonRules.Default)` は `new LessonEngine(Catalog)` にする

## 4. タスク

4 つのタスクに分け、この順に行う。**各タスクの最後に `dotnet build shared/Uta.Shared.Domain` と `dotnet test --project shared/Uta.Shared.Domain.Tests` を通す**。どのタスクでも、既存のテストの期待値は変えない（変えないと通らないなら、止まって報告する）。とくに `Validator_Phase1Cards_NoErrors` と `Replay_*` の 3 件は、振る舞いが変わっていないことの確かめなので、名前の追従以外で触らない。

| タスク | 中身 | 足すテスト |
|---|---|---|
| 1 | 3.1〜3.5。`Phase1Cards.cs` を下の対応表で書き直す | 5.1 の 1〜9、5.2 |
| 2 | 3.6 | なし（名前の追従だけ） |
| 3 | 3.7 | 5.1 の 10・11 |
| 4 | 3.8。終わったら 2 章の最後の行の wiki を直す | なし |

`Phase1Cards.cs` の補助の対応表（タスク 1。タスク 2 で `Genki` を `Shield` にする）:

| 補助 | 新しい本体 |
|---|---|
| `Param(int value)` | `new EffectDef(EffectOp.GainParam, value: value)` |
| `Genki(int value)` | `new EffectDef(EffectOp.AddShield, value: value)` |
| `Impression(int value)` | `new EffectDef(EffectOp.AddStatus, status: StatusKind.Impression, value: value)` |
| `Turns(StatusKind kind, int value)` | `new EffectDef(EffectOp.AddStatus, status: kind, value: value)` |
| `StackIf(StatusKind kind, int value, ConditionDef condition)` | `new EffectDef(EffectOp.AddStatus, status: kind, value: value, conditions: new[] { condition })` |
| `Uses(int value)` | `new EffectDef(EffectOp.AddCardUses, value: value)` |
| `Draw(int value)` | `new EffectDef(EffectOp.DrawCards, value: value)` |
| `RatioOfImpression(int percent)` | `new EffectDef(EffectOp.GainParamByImpression, value: percent)` |
| `OnMentalUse(EffectDef effect)` | `new EffectDef(EffectOp.AddPersistent, trigger: PersistentTrigger.OnMentalCardUse, children: new[] { effect })`（ターン数・回数は 0 ＝無制限。旧 `Arg2`・`Arg3` が 0 だったのと同じ） |

## 5. テスト

### 5.1 `CardDefinitionValidatorTests.cs`

共通の補助をテストのクラスに置く:

```csharp
private static CardDefinition CardWith(params EffectDef[] effects)
{
    var face = new CardFace(new CostDef[0], new ConditionDef[0], effects);
    return new CardDefinition(999, Plan.Logic, CardType.Active, Rarity.SR, 1, true, false, false, face, null);
}
```

期待は `var errors = CardDefinitionValidator.Validate(card);` に対して、`Assert.Single(errors)` と `Assert.Contains("<文言>", errors[0])` で確かめる（10 も同じ。11 は例外の型を `Assert.Throws` で確かめる）。

| # | テスト名 | 入力 | 期待の文言 |
|---|---|---|---|
| 1 | `Validate_OpNothing_ReturnsError` | `new EffectDef(EffectOp.Nothing)` | `未対応の効果` |
| 2 | `Validate_AddStatusWithPercentStatus_ReturnsError` | `AddStatus`、`status: FocusBoost`、`value: 1` | `のステータスが不正` |
| 3 | `Validate_AddStatusValueZero_ReturnsError` | `AddStatus`、`status: Impression`、`value: 0` | `の値が 1 未満` |
| 4 | `Validate_AddStatusWithTrigger_ReturnsError` | `AddStatus`、`status: Impression`、`trigger: TurnStart`、`value: 1` | `に使わない列がある` |
| 5 | `Validate_AddPersistentWithEveryNTurns_ReturnsError` | `AddPersistent`、`trigger: EveryNTurns`、`children: { GainParam value 1 }` | `のトリガーが不正` |
| 6 | `Validate_AddPersistentWithoutChildren_ReturnsError` | `AddPersistent`、`trigger: TurnStart`、子なし | `持続効果に発動する効果が無い` |
| 7 | `Validate_NestedPersistent_ReturnsError` | `AddPersistent`（`trigger: TurnStart`）の子に `AddPersistent`（`trigger: TurnStart`、子に `GainParam value 1`） | `持続効果の中に持続効果がある` |
| 8 | `Validate_GainParamByShield_ReturnsError` | `GainParamByShield`、`value: 50`（フェーズ 2 で未対応） | `未対応の効果` |
| 9 | `Validate_ConditionSubjectNothing_ReturnsError` | 使用条件に `new ConditionDef(ConditionSubject.Nothing, Comparator.GreaterOrEqual, 1)`、効果は `GainParam value 1` | `条件が不正` |
| 10 | `Validate_CardPlanNothing_ReturnsError`（タスク 3） | `CardWith` の `Plan.Logic` を `Plan.Nothing` にしたカード、効果は `GainParam value 1` | `カードの区分が不正` |
| 11 | `LessonSetup_KindNothing_Throws`（タスク 3） | `new LessonSetup(LessonKind.Nothing, ParamKind.Singing, 6, 30, 60)` | `ArgumentOutOfRangeException`（`Assert.Throws`） |

9 と 10 は `CardWith` を使わず、同じ形でカードを組み立てる。

### 5.2 `StatusSetTests.cs`

| テスト名 | 中身 |
|---|---|
| `Get_Nothing_ReturnsZero` | `new StatusSet().Get(StatusKind.Nothing)` が 0 |
| `Restore_NonZeroAtIndexZero_Throws` | 長さ 13 で 0 番目が 1 の `values` と、すべて `false` の `skipNextDecrement` を渡すと `ArgumentException` |
| `Restore_WrongLength_Throws` | 長さ 12 の並びを渡すと `ArgumentException` |

`Add` は `internal` なので、`Nothing` を渡したときの例外は直接テストしない（`InternalsVisibleTo` は足さない）。

実行: 各タスクの最後に `dotnet build shared/Uta.Shared.Domain`、`dotnet test --project shared/Uta.Shared.Domain.Tests`。Unity（IL2CPP）でのコンパイルはこの計画では確かめない。

## 6. 未決事項

なし（`requesting-design-review` に出して `ready` にする）。
