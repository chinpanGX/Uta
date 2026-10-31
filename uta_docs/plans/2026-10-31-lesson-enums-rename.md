---
title: 実装計画: レッスンの enum と効果の列を lesson-enums.md に合わせる
status: draft
updated: 2026-11-01
spec: ../wiki/architecture/lesson-enums.md, ../wiki/architecture/lesson-domain-types.md
---

# 実装計画: レッスンの enum と効果の列を lesson-enums.md に合わせる

## 1. 目的と前提

`shared/Uta.Shared.Domain/Lesson` の enum の名前・数値と、`EffectDef` の列の持ち方を、[lesson-enums.md](../wiki/architecture/lesson-enums.md)（正）に合わせる。振る舞いは変えない（フェーズ 1 のカード 13 枚の結果は変わらない）。

- 関係する ADR: [ADR 0007](../wiki/decisions/0007-masterdata-with-addons.md)（列の型）、[ADR 0008](../wiki/decisions/0008-shared-domain-and-onion.md)（共有ドメイン）
- 背景: 2026-10-31 の設計書レビュー（lesson-card.md の Critical C1・C2）
- 決めないこと（範囲外）: フェーズ 2 以降の `Op` の処理の実装、`EveryNTurns` の処理、マスタ（`masterdata/`）の作成、`ScheduleEffect` の実装

## 2. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonEnums.cs` | enum の名前・数値を lesson-enums.md に合わせる。`RatioSource`・`ResourceKind` を消す |
| 変更 | `shared/Uta.Shared.Domain/Lesson/CardDefinition.cs` | `EffectDef` に `Status`・`Trigger` を足す |
| 変更 | `shared/Uta.Shared.Domain/Lesson/StatusSet.cs` | `Nothing` の追加に合わせて配列の大きさと範囲を直す |
| 変更 | `shared/Uta.Shared.Domain/Lesson/EffectExecutor.cs` | `Op` の名前と、引数の読み方を直す |
| 変更 | `shared/Uta.Shared.Domain/Lesson/CardDefinitionValidator.cs` | `Op` の名前と検証を直す |
| 変更 | `shared/Uta.Shared.Domain/Lesson/ConditionEvaluator.cs`・`LessonEngine.cs` | 名前の変更に追従（`Nothing` の扱いは 3 章） |
| 変更 | `shared/Uta.Shared.Domain.Tests/Lesson/Phase1Cards.cs`・`LessonEngineTests.cs` | カード定義を新しい形で書き直す |
| 作成 | `shared/Uta.Shared.Domain.Tests/Lesson/CardDefinitionValidatorTests.cs` | 5章の検証のテスト |
| 変更 | `uta_docs/wiki/architecture/lesson-enums.md`・`lesson-domain-types.md` | 実装メモの「コードは古いまま」を消す |

## 3. 型とシグネチャ

### 3.1 enum

`LessonEnums.cs` の `EffectOp`・`PersistentTrigger`・`StatusKind`・`CostKind`・`ConditionSubject`・`Comparator` を、[lesson-enums.md](../wiki/architecture/lesson-enums.md) 1〜5章の表の**名前と数値どおり**に書き直す（値は明示する）。

- ファイル先頭のコメントを次に置き換える: `// 名前・数値の正は uta_docs/wiki/architecture/lesson-enums.md。マスタ・保存の値に使うので数値は変えない。追加は末尾に足す。`
- 各 enum の `<summary>` に「lesson-enums.md N章」を書く
- `RatioSource`・`ResourceKind` は消す
- `Plan`・`CardType`・`Rarity`・`ParamKind`・`LessonKind` は、0 に `Nothing` を足して [lesson-enums.md](../wiki/architecture/lesson-enums.md) 6章の数値に振り直す（2026-10-31 に追加）。`LessonPhase` は変えない
- 0 の名前は `None` ではなく `Nothing`（lesson-enums.md「決まっていること」）
- 元気の名前を `Genki` から `Shield` に変える（2026-11-01 に追加。lesson-enums.md「命名の規則」）。`shared/` のコードとテストで `Genki` を含む識別子をすべて置き換える: `GenkiBlock` → `ShieldGainBlock`、それ以外は `Genki` → `Shield`（`AddGenki`・`AddFixedGenki`・`GenkiGain`・`PaysGenkiBeforeHp`・`fromGenki`、`LessonState` のプロパティなど）。コメントの「元気」はそのまま
- `LessonRules` を `static class` の `const` にする（2026-11-01 に追加。[lesson-domain-types.md](../wiki/architecture/lesson-domain-types.md) 2章）。`Default` と、`LessonRules` を引数で受け取るところ（`LessonEngine` など）を直し、テストもそれに合わせる。値は変えない

### 3.2 `EffectDef`

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

プロパティ `public StatusKind Status { get; }` と `public PersistentTrigger Trigger { get; }` を `Op` の次に足す。ほかは変えない。

### 3.3 `EffectExecutor` の引数の読み方

| 旧 | 新 |
|---|---|
| `Param`（`Arg1`〜`Arg3`） | `GainParam`（同じ） |
| `ParamByRatio`（`Arg1`＝`RatioSource`） | `GainParamByImpression`（参照元は好印象）。`GainParamByShield`・`GainParamByMotivation` はフェーズ 2 なので処理しない |
| `AddTurns` / `AddStack`（`Arg1`＝ステータス） | `AddStatus`（`effect.Status`） |
| `Draw` / `AddUses` | `DrawCards` / `AddCardUses` |
| `Persistent`（`Arg1`＝トリガー、`Arg2`＝ターン数、`Arg3`＝回数） | `AddPersistent`（`effect.Trigger`、`Arg1`＝ターン数、`Arg2`＝回数）。「プレイヤー操作中に付与した」の判定は旧 `effect.Arg2 > 0` を `effect.Arg1 > 0` にする |

`ReadRatioSource` は消し、`GainParamByImpression` は好印象の値を直接読む。

### 3.4 `CardDefinitionValidator`

- 動かせる `Op`（旧 15〜22行目の一覧）: `GainParam`・`GainParamByImpression`・`AddShield`・`AddStatus`・`DrawCards`・`AddCardUses`・`AddPersistent`。それ以外（`Nothing` を含む）は「未対応の効果です」のエラー
- `AddStatus`: `Status` が `GoodCondition`〜`DebuffGuard` の範囲で、かつ `FocusBoost`・`ImpressionBoost` でないこと。`Trigger` が `Nothing` であること
- 効果ごとの境界（[lesson-enums.md](../wiki/architecture/lesson-enums.md) 1章）: どの `Op` も `Value`・`Arg` が負ならエラー。`AddStatus`・`DrawCards` は `Value` が 1 以上。`ScaleShield` は 0〜100（動かせる `Op` に入ったとき）。`AddStatusPercent` は「未対応の効果です」のエラー（lesson-card.md 9.2）
- `AddPersistent`: `Trigger` が `LessonStart`〜`TurnEnd` の範囲で、かつ `EveryNTurns` でないこと（未実装）。`Arg1`・`Arg2` が 0 以上。`Status` が `Nothing`。子が 1 つ以上。子に `AddPersistent` があればエラー（今の「持続効果の中に持続効果を置かない」検査を残す。入れ子の深さは 1。lesson-enums.md「決まっていること」）
- `AddStatus`・`AddPersistent` 以外の `Op` で `Status` または `Trigger` が `Nothing` でなければエラー（「使わない列が埋まっている」）
- コスト・条件（今は検証していないので足す）: 通常版・強化版の各面のコストの `Kind` が `Consume`〜`Motivation` の範囲外なら `prefix + "コストの種類が不正"`、使用条件と効果内条件の `Subject` が `GoodConditionTurns`〜`ClearTargetPercent`、`Comparator` が `GreaterOrEqual`〜`Equal` の範囲外なら `prefix + "条件が不正"` をエラーに足す。範囲の確かめ方は既存の `IsDefined` と同じ形の private メソッドにする
- `IsDefined(RatioSource)` は消す。`IsDefined(StatusKind)`・`IsDefined(PersistentTrigger)` の下限・上限を新しい値に合わせる

### 3.5 `StatusSet`

- `KindCount` は `(int)StatusKind.DebuffGuard + 1`（13）。添字は `(int)kind` のまま。添字 0（`Nothing`）は常に 0
- `Add` に `StatusKind.Nothing` が来たら `ArgumentOutOfRangeException`
- 保存した状態から作り直すコンストラクタは、長さ 13 の並びを受け取る（0 番目は 0 でなければ `ArgumentException`）

## 4. タスク

1 タスクで行う（enum の数値を変えるとすべてのファイルが同時に壊れるため、分けるとビルドが通らない）。

- 手順: 2 章の表の上から順に、3 章のとおりに書き換える。テストの `Phase1Cards.cs` は、`new EffectDef(EffectOp.AddStack, 3, (int)StatusKind.Impression)` のような書き方を `new EffectDef(EffectOp.AddStatus, status: StatusKind.Impression, value: 3)` のように名前付き引数で書き直す
- 境界: 3.4・3.5 のとおり
- 確かめ方: `dotnet build shared/Uta.Shared.Domain` と `dotnet test --project shared/Uta.Shared.Domain.Tests` が通る。既存のテストの期待値は**変えない**（変えないと通らないなら、止まって報告する）

## 5. テスト

既存のテストはすべて期待値を変えずに通す。足すテスト（`shared/Uta.Shared.Domain.Tests/Lesson/CardDefinitionValidatorTests.cs` を作成。2 章の表にも足す）:

- `Validate_OpNone_ReturnsError`
- `Validate_AddStatusWithPercentStatus_ReturnsError`（`FocusBoost`）
- `Validate_AddStatusWithTrigger_ReturnsError`
- `Validate_AddPersistentWithEveryNTurns_ReturnsError`
- `Validate_GainParamByShield_ReturnsError`（フェーズ 2 で未対応）
- `Validate_ConditionSubjectNone_ReturnsError`

実行: `dotnet build shared/Uta.Shared.Domain`、`dotnet test --project shared/Uta.Shared.Domain.Tests`。Unity（IL2CPP）でのコンパイルはこの計画では確かめない。

## 6. 未決事項

なし（`requesting-design-review` に出して `ready` にする）。
