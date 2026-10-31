---
title: レッスンの効果・条件・コストの enum と列
status: draft
updated: 2026-10-31
---

# レッスンの効果・条件・コストの enum と列

レッスンの効果（[lesson-card.md](../systems/lesson-card.md) 7章）・ステータス（8章）・持続効果のトリガー（10章）・コスト（5章）・条件（6章）の **enum の名前・数値と、効果の列の対応の正**。マスタ（[masterdata-pipeline.md](masterdata-pipeline.md) 3章）と共有ドメイン（[lesson-domain-types.md](lesson-domain-types.md) 2章、`shared/Uta.Domain/Lesson/LessonEnums.cs`）は、どちらもこのページに合わせる。ほかのページには enum の値や列の対応を書き写さず、このページへリンクする。

2026-10-31 の設計書レビュー（lesson-card.md の Critical C1）で、マスタの仕様とコードで名前・値・列の対応が別物になっていたため、名前を見直してここに 1 つにまとめた。

## 決まっていること [決定。2026-10-31]

- マスタの `Effect` の行と Domain の `EffectDef` は**同じ形**にする。列（プロパティ）は `Op`・`Status`・`Trigger`・`Value`・`Arg1`〜`Arg3` と、参照（マスタでは `RefConditionGroupId`・`RefEffectGroupId`・`RefCardId`、Domain では `Conditions`・`Children`・`RefCardId`）。マスタと Domain の間で列を読み替えない
- 対象のステータスは `Status`（`StatusKind`）、持続効果のトリガーは `Trigger`（`PersistentTrigger`）という**型付きの列**に入れる。`Arg` に enum を数値で入れない。CSV に名前で書け、`master-gen` が名前を検証できるため
- `Value` は効果の**主な量**（パラメータ、倍率、ターン数、枚数など）。`Arg1`〜`Arg3` は補助の値。どちらも `int`
- 倍率・回数の `Arg` は **0（空欄）を既定値**として扱う（連撃 0 → 1回、倍率 0 → 100%、持続の制限 0 → 無制限）
- **列に使う enum は、すべて 0 を `None` にする**。空欄（0）を「指定なし」と区別し、必須の列が空のときに検証で弾くため
- **数値はこのページで固定する**。以後は値を変えず、追加は末尾に足す（マスタ・保存の値に使うため）。このページを作る前のコードの数値は、保存したデータがまだ無いので付け直す（[実装計画](../../plans/2026-10-31-lesson-enums-rename.md)）
- 参照（`RefConditionGroupId` などの効果内条件）は**どの `Op` でも同じ意味**（満たさないとその行だけ飛ばす。判定はコストの前。lesson-card.md 6.2）。`AddPersistent` でも、その行の条件は「持続効果を付与するかどうか」の条件で、例外を作らない。発動のたびに見る条件は、発動する効果（子）の効果内条件に付ける（lesson-card.md 7.5）
- 使わない列は空欄にする。`Op` ごとの必須・不要の列を、マスタは `IValidatable`、Domain は `CardDefinitionValidator` で検証する
- 「フェーズ」はカードとして動かし始めるフェーズ（lesson-card.md 15章）。enum には最初から全部を定義し、まだ動かせない `Op` を使うカードは読み込み時にエラーにする（lesson-card.md 15.1）
- lesson-card.md 7.6 の後回しのプリミティブ（`RepeatNext` / `AddLessonTurn` / `DeckManipulate`）は enum に入れず、そのカードを採用するときにこのページの末尾に足す

### 命名の規則

- 効果の `Op` は「動詞 + 対象」（`GainParam`、`AddStatus`、`DrawCards`）。何をするかが名前だけで分かるようにする
- 参照元・対象が違うだけで処理が同じでも、**別の enum を引数で渡すより `Op` を分ける**（`GainParamByImpression` など）。引数の enum を減らし、列を `Status` / `Trigger` の 2 つに限るため
- ステータス・条件の対象は、用語集（[glossary.md](../glossary.md)）の名前に対応させる。仕組みが名前で分かるもの（消費体力を半分にする → `CostHalf`）はその名前にする

## 1. 効果（`EffectOp`）

| 値 | 名前 | 内容（lesson-card.md 7章） | フェーズ | `Status` / `Trigger` | `Value` | `Arg1`〜`Arg3`・参照 |
|---|---|---|---|---|---|---|
| 0 | `None` | 指定なし（エラー） | — | — | — | — |
| 1 | `GainParam` | パラメータ上昇 | 1 | — | 上昇量 | `Arg1` 連撃の回数（0 は 1）、`Arg2` 集中の倍率%（0 は 100）、`Arg3` 好調の倍率%（0 は 100） |
| 2 | `GainParamByImpression` | 好印象の N% 分パラメータ上昇 | 1 | — | 倍率% | — |
| 3 | `GainParamByGenki` | 元気の N% 分パラメータ上昇 | 2 | — | 倍率% | — |
| 4 | `GainParamByMotivation` | やる気の N% 分パラメータ上昇 | 2 | — | 倍率% | — |
| 5 | `AddParamBonus` | パラメータ上昇量増加 | 未定 | — | 上昇量増加% | `Arg1` ターン数 |
| 6 | `AddGenki` | 元気（やる気の影響を受ける） | 1 | — | 量 | — |
| 7 | `AddFixedGenki` | 固定元気 | 2 | — | 量 | — |
| 8 | `RecoverHp` | 体力回復 | 2 | — | 量 | — |
| 9 | `ScaleGenki` | 元気を N% にする（端数は切り捨て） | 2 | — | 倍率% | — |
| 10 | `SetGenki` | 元気を N にする | 2 | — | 値 | — |
| 11 | `AddStatus` | ステータスを足す。単位（ターン / 値 / 回数）はステータスで決まる（3章） | 1 | `Status` | 量 | — |
| 12 | `AddStatusPercent` | % のステータスを足す（好印象強化・集中強化） | 未定 | `Status` | % | `Arg1` ターン数（0 はレッスン中ずっと） |
| 13 | `DrawCards` | スキルカードを引く | 1 | — | 枚数 | — |
| 14 | `AddCardUses` | スキルカード使用数追加 | 1 | — | 回数 | — |
| 15 | `ReplaceHand` | 手札をすべて入れ替える | 未定 | — | — | — |
| 16 | `EnhanceHand` | 手札をすべてレッスン中強化 | 未定 | — | — | — |
| 17 | `CreateCard` | カードを生成する | 未定 | — | 枚数 | `RefCardId`。生成する位置の持ち方は採用時に決める |
| 18 | `ScheduleEffect` | 発動予約（N ターン後に効果を発動） | 未定 | — | 何ターン後か（次のターン＝1） | 子の効果（マスタは `RefEffectGroupId`） |
| 19 | `AddPersistent` | 持続効果を付与する | 1(トリガーは `EveryNTurns` を除く全部) | `Trigger` | `EveryNTurns` の N(ほかは空欄) | `Arg1` ターン数・`Arg2` 回数(0 は無制限)、子の効果(マスタは `RefEffectGroupId`) |

- `AddStatus` に `%` のステータス（`FocusBoost`・`ImpressionBoost`）を渡したら検証で弾く（`AddStatusPercent` を使う）。逆も同じ
- 効果ごとの境界 [決定。2026-10-31]:

| `Op` | 境界 |
|---|---|
| `AddFixedGenki` | やる気の影響を受けない。元気増加無効の間は増えない(lesson-card.md 9.3) |
| `SetGenki` | 元気増加無効のときも値を設定する(「増加」ではないため)。`Value` は 0 以上 |
| `ScaleGenki` | `Value` は 0〜100(減らすだけ)。端数は切り捨て |
| `RecoverHp` | `Value` は 1 以上。最大体力で止める |
| `AddStatus` | `Value` は 1 以上(減らす効果はない) |
| `DrawCards` | `Value` は 1 以上。引ける分だけ引く(lesson-card.md 4章) |
| どの `Op` も | 負の値は読み込み時にエラー(`CardDefinitionValidator`) |

- 好調・絶好調・消費体力減少などの「ターン」のステータスも、集中・好印象などの「値」のステータスも、`AddStatus` 1 つで足す。足し方の違い（11章の「プレイヤー操作中に初めて付与した」印）は `StatusKind` で決まる

## 2. 持続効果のトリガー（`PersistentTrigger`）

カードの種別での絞り込みは、条件ではなくトリガーの種類で表す。

| 値 | 名前 | タイミング（lesson-card.md 10章） | フェーズ |
|---|---|---|---|
| 0 | `None` | 指定なし（エラー） | — |
| 1 | `LessonStart` | L1 | 1 |
| 2 | `TurnStart` | T3 | 1 |
| 3 | `AfterTurnStart` | T5 | 1 |
| 4 | `EveryNTurns` | T7（N は `AddPersistent` の `Value`） | 未定 |
| 5 | `OnCostHpDecrease` | P4 | 1 |
| 6 | `OnCardUse` | P5 | 1 |
| 7 | `OnActiveCardUse` | P5（アクティブのみ） | 1 |
| 8 | `OnMentalCardUse` | P5（メンタルのみ） | 1 |
| 9 | `AfterCardUse` | P7 | 1 |
| 10 | `AfterActiveCardUse` | P7（アクティブのみ） | 1 |
| 11 | `AfterMentalCardUse` | P7（メンタルのみ） | 1 |
| 12 | `OnSkip` | E1 | 1 |
| 13 | `TurnEnd` | E2 | 1 |

## 3. ステータス（`StatusKind`）

元気と体力はステータスではなく、Domain では `LessonState` に持つ（[lesson-domain-types.md](lesson-domain-types.md) 3章）。元気を扱う効果は `AddGenki` などの専用の `Op` にする。

| 値 | 名前 | 用語（[glossary.md](../glossary.md)） | 単位 |
|---|---|---|---|
| 0 | `None` | 指定なし | — |
| 1 | `GoodCondition` | 好調 | ターン |
| 2 | `Excellent` | 絶好調 | ターン |
| 3 | `Focus` | 集中 | 値 |
| 4 | `FocusBoost` | 集中強化 | % |
| 5 | `Impression` | 好印象 | 値 |
| 6 | `ImpressionBoost` | 好印象強化 | % |
| 7 | `Motivation` | やる気 | 値 |
| 8 | `CostHalf` | 消費体力減少（consume の消費を 50% に） | ターン |
| 9 | `CostDouble` | 消費体力増加（consume の消費を 2 倍に） | ターン |
| 10 | `CostReduce` | 消費体力削減（consume の消費から値を引く） | 値 |
| 11 | `GenkiBlock` | 元気増加無効 | ターン |
| 12 | `DebuffGuard` | 低下状態無効 | 回数 |

## 4. コスト（`CostKind`）

| 値 | 名前 | 内容（lesson-card.md 5章） | フェーズ |
|---|---|---|---|
| 0 | `None` | 指定なし（エラー） | — |
| 1 | `Consume` | 元気 → 体力の順に払う。補正を受ける | 1 |
| 2 | `HpDirect` | 体力から直接払う。補正を受けない | 1 |
| 3 | `GoodConditionTurns` | 好調のターン | 1(実装済み。使うカードはフェーズ 3 だが無いので合成カードで確かめる) |
| 4 | `ExcellentTurns` | 絶好調のターン | 1(同上) |
| 5 | `Focus` | 集中 | 1(同上) |
| 6 | `Impression` | 好印象 | 使うカードを採用したとき |
| 7 | `Motivation` | やる気 | 使うカードを採用したとき |

## 5. 条件（`ConditionSubject` / `Comparator`）

条件は `{ Subject, Comparator, Value }`。使用条件と効果内条件で同じ形（lesson-card.md 6.1）。

| 値 | `ConditionSubject` | `Value` | 例 |
|---|---|---|---|
| 0 | `None` | 指定なし（エラー） | |
| 1 | `GoodConditionTurns` | ターン数（「状態である」は `GreaterOrEqual` 1） | S04、S11 |
| 2 | `ExcellentTurns` | ターン数 | |
| 3 | `Focus` | 値 | S05 |
| 4 | `Impression` | 値 | L02（`Impression` `GreaterOrEqual` 1） |
| 5 | `Motivation` | 値 | |
| 6 | `Genki` | 値（「元気が 0」は `Equal` 0） | |
| 7 | `HpPercent` | 最大体力に対する% | F05（`HpPercent` `GreaterOrEqual` 50） |
| 8 | `TurnNumber` | 何ターン目か | |
| 9 | `ClearTargetPercent` | 得たパラメータの、クリア目標に対する% | |

| 値 | `Comparator` |
|---|---|
| 0 | `None`（エラー） |
| 1 | `GreaterOrEqual` |
| 2 | `LessOrEqual` |
| 3 | `Equal` |

## 6. 例（L14 の通常版。ID は説明用）

| テーブル | 行 |
|---|---|
| `CardCost` | `Consume` 4 |
| `CardFace` | `EffectGroupId` 1401 |
| `Effect`（1401） | 1: `AddStatus` `Impression` 3 ／ 2: `AddPersistent` `OnMentalCardUse` → `RefEffectGroupId` 1402 |
| `Effect`（1402） | 1: `AddStatus` `Impression` 1 |

条件付きの持続効果の例（説明用）: 「好印象が 3 以上なら、以降ターン開始時に好印象+1」は、`AddPersistent` の行の `RefConditionGroupId` に「`Impression` `GreaterOrEqual` 3」を付ける（付与するときに判定）。「以降、ターン開始時に好印象が 3 以上なら好印象+1」は、子（`AddStatus`）の行に付ける（発動のたびに判定）。

## 未決事項

- `CreateCard` の生成する位置の持ち方（採用時）
- `AddStatusPercent` の持続ターンの数え方（フェーズ 4。lesson-domain-types.md 3章）
- 引数が 3 つで足りない `Op` が出たときの扱い（masterdata-pipeline.md 3章）

## 実装メモ

- 2026-10-31: このページを作った時点で、コード（`LessonEnums.cs`・`EffectExecutor.cs`・`CardDefinitionValidator.cs`）はまだ古い名前・数値（`EffectOp.Param`、`RatioSource`、`ResourceKind`、`Arg1` にステータスを数値で入れる形）のまま。直す手順は [実装計画](../../plans/2026-10-31-lesson-enums-rename.md)
