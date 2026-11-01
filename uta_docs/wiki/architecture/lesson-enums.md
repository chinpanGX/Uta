---
title: レッスンの効果・条件・コストの enum と列
status: draft
updated: 2026-11-01
---

# レッスンの効果・条件・コストの enum と列

レッスンの効果（[lesson-card.md](../systems/lesson-card.md) 7章）・ステータス（8章）・持続効果のトリガー（10章）・コスト（5章）・条件（6章）の **enum の名前・数値と、効果の列の対応の正**。マスタ（[masterdata-pipeline.md](master/masterdata-pipeline.md) 3章）と共有ドメイン（[lesson-domain-types.md](lesson-domain-types.md) 2章、`shared/Uta.Shared.Domain/Lesson/LessonEnums.cs`）は、どちらもこのページに合わせる。マスタの側は `masterdata/Definitions` に同じ名前・数値の写しを置き、テストでずれを見つける（[masterdata-pipeline.md](master/masterdata-pipeline.md) 2章）。ほかのページには enum の値や列の対応を書き写さず、このページへリンクする。

2026-10-31 の設計書レビュー（lesson-card.md の Critical C1）で、マスタの仕様とコードで名前・値・列の対応が別物になっていたため、名前を見直してここに 1 つにまとめた。

## 決まっていること [決定。2026-10-31]

- マスタの `Effect` の行と Domain の `EffectDef` は**同じ形**にする。列（プロパティ）は `Op`・`Status`・`Trigger`・`Value`・`Arg1`〜`Arg3` と、参照（マスタでは `RefConditionGroupId`・`RefEffectGroupId`・`RefCardId`、Domain では `Conditions`・`Children`・`RefCardId`）。マスタと Domain の間で列を読み替えない
- 対象のステータスは `Status`（`StatusKind`）、持続効果のトリガーは `Trigger`（`PersistentTrigger`）という**型付きの列**に入れる。`Arg` に enum を数値で入れない。CSV に名前で書け、`master-gen` が名前を検証できるため
- `Value` は効果の**主な量**（パラメータ、倍率、ターン数、枚数など）。`Arg1`〜`Arg3` は補助の値。どちらも `int`
- 倍率・回数の `Arg` は **0（空欄）を既定値**として扱う（連撃 0 → 1回、倍率 0 → 100%、持続の制限 0 → 無制限）
- **Uta の enum は、0 を `Nothing`（指定なし）にする**。このページの enum（効果・トリガー・ステータス・コスト・条件）、既にある `Plan`・`CardType`・`Rarity`・`ParamKind`・`LessonKind`（6章で振り直す）、これから作る enum（マスタの列・DTO・状態）のすべて。CSV の空欄は 0 になるので、`Nothing` と区別でき、必須の列が空のときに検証で弾ける [決定。2026-10-31。ユーザーの判断。それまでは 0 の名前を `None` とし、既にある 6 つは 0 が有効な値のまま変えないとしていた]。例外は `LessonPhase`（0 の `NotStarted` は「始まる前」という実際の局面で、列にも使わない）
- **数値はこのページで固定する**。以後は値を変えず、追加は末尾に足す（マスタ・保存の値に使うため）。このページを作る前のコードの数値は、保存したデータがまだ無いので付け直す（[実装計画](../../plans/2026-10-31-lesson-enums-rename.md)）
- **効果の入れ子の深さは 1**: `AddPersistent`・`ScheduleEffect` の子の効果に、`AddPersistent`・`ScheduleEffect` を置かない。グループの循環もこれで起きない [決定。2026-10-31。[masterdata-pipeline.md](master/masterdata-pipeline.md) 3章]
- 参照（`RefConditionGroupId` などの効果内条件）は**どの `Op` でも同じ意味**（満たさないとその行だけ飛ばす。判定はコストの前。lesson-card.md 6.2）。`AddPersistent` でも、その行の条件は「持続効果を付与するかどうか」の条件で、例外を作らない。発動のたびに見る条件は、発動する効果（子）の効果内条件に付ける（lesson-card.md 7.5）
- 使わない列は空欄にする。`Op` ごとの必須・不要の列を、マスタは `IValidatable`、Domain は `CardDefinitionValidator` で検証する。いまの Domain の検証は `Status`・`Trigger` の列だけ（[enum の改名の計画](../../plans/2026-10-31-lesson-enums-rename.md)。2026-11-01。ユーザーの判断）。`Value`・`Arg`・参照の列の規則は、下の「マスタの検証の規則」で決めた（2026-11-01）。Domain の検証は、まだ `Status`・`Trigger` の列だけ（差は実装メモ）
- 「フェーズ」は**コードで処理できるようになるフェーズ**（[lesson-phases.md](lesson-phases.md)）。カードで使い始めるフェーズとは限らない（使うカードがないものは合成カードで確かめる。[lesson-phases.md](lesson-phases.md) 1章）。enum には最初から全部を定義し、まだ動かせない `Op` を使うカードは読み込み時にエラーにする（[lesson-phases.md](lesson-phases.md) 1章）
- lesson-card.md 7.6 の後回しのプリミティブ（`RepeatNext` / `AddLessonTurn` / `DeckManipulate`）は enum に入れず、そのカードを採用するときにこのページの末尾に足す

### 命名の規則

- 効果の `Op` は「動詞 + 対象」（`GainParam`、`AddStatus`、`DrawCards`）。何をするかが名前だけで分かるようにする
- 参照元・対象が違うだけで処理が同じでも、**別の enum を引数で渡すより `Op` を分ける**（`GainParamByImpression` など）。引数の enum を減らし、列を `Status` / `Trigger` の 2 つに限るため
- **このページの名前が、コードの名前の正**。用語集の仮称（[glossary.md](../glossary.md)「置き換え対象の仮称」）は公開前に置き換えるが、コードの名前は変えない [決定。2026-11-01。ユーザーの判断]
- ステータス・条件の対象は、用語集の語の意味に対応させた英語の名前にする。仮称をローマ字にしない（元気 → `Shield`。体力より先にコストを受ける役割から。2026-11-01 に `Genki` から変えた）。仕組みが名前で分かるもの（消費体力を半分にする → `CostHalf`）はその名前にする

## 1. 効果（`EffectOp`）

| 値 | 名前 | 内容（lesson-card.md 7章） | フェーズ | `Status` / `Trigger` | `Value` | `Arg1`〜`Arg3`・参照 |
|---|---|---|---|---|---|---|
| 0 | `Nothing` | 指定なし（エラー） | — | — | — | — |
| 1 | `GainParam` | パラメータ上昇 | 1 | — | 上昇量 | `Arg1` 連撃の回数（0 は 1）、`Arg2` 集中の倍率%（0 は 100）、`Arg3` 好調の倍率%（0 は 100） |
| 2 | `GainParamByImpression` | 好印象の N% 分パラメータ上昇 | 1 | — | 倍率% | — |
| 3 | `GainParamByShield` | 元気の N% 分パラメータ上昇 | 2 | — | 倍率% | — |
| 4 | `GainParamByMotivation` | やる気の N% 分パラメータ上昇 | 2 | — | 倍率% | — |
| 5 | `AddParamBonus` | パラメータ上昇量増加 | 採用時 | — | 上昇量増加% | `Arg1` ターン数 |
| 6 | `AddShield` | 元気（やる気の影響を受ける） | 1 | — | 量 | — |
| 7 | `AddFixedShield` | 固定元気 | 2 | — | 量 | — |
| 8 | `RecoverHp` | 体力回復 | 2 | — | 量 | — |
| 9 | `ScaleShield` | 元気を N% にする（端数は切り捨て） | 2 | — | 倍率% | — |
| 10 | `SetShield` | 元気を N にする | 2 | — | 値 | — |
| 11 | `AddStatus` | ステータスを足す。単位（ターン / 値 / 回数）はステータスで決まる（3章） | 1 | `Status` | 量 | — |
| 12 | `AddStatusPercent` | % のステータスを足す（好印象強化・集中強化） | 採用時 | `Status` | % | `Arg1` ターン数（0 はレッスン中ずっと） |
| 13 | `DrawCards` | スキルカードを引く | 1 | — | 枚数 | — |
| 14 | `AddCardUses` | スキルカード使用数追加 | 1 | — | 回数 | — |
| 15 | `ReplaceHand` | 手札をすべて入れ替える | 採用時 | — | — | — |
| 16 | `EnhanceHand` | 手札をすべてレッスン中強化 | 採用時 | — | — | — |
| 17 | `CreateCard` | カードを生成する | 採用時 | — | 枚数 | `RefCardId`。生成する位置の持ち方は採用時に決める |
| 18 | `ScheduleEffect` | 発動予約（N ターン後に効果を発動） | 採用時 | — | 何ターン後か（次のターン＝1） | 子の効果（マスタは `RefEffectGroupId`） |
| 19 | `AddPersistent` | 持続効果を付与する | 1(トリガーは `EveryNTurns` を除く全部) | `Trigger` | `EveryNTurns` の N(ほかは空欄) | `Arg1` ターン数・`Arg2` 回数(0 は無制限)、子の効果(マスタは `RefEffectGroupId`) |

- `AddStatus` に `%` のステータス（`FocusBoost`・`ImpressionBoost`）を渡したら検証で弾く（`AddStatusPercent` を使う）。逆も同じ
- 効果ごとの境界 [決定。2026-10-31]:

| `Op` | 境界 |
|---|---|
| `AddFixedShield` | やる気の影響を受けない。元気増加無効の間は増えない(lesson-card.md 9.3) |
| `SetShield` | 元気増加無効のときも値を設定する([lesson-card.md](../systems/lesson-card.md) 7.2)。`Value` は 0 以上 |
| `ScaleShield` | `Value` は 0〜100(減らすだけ)。端数は切り捨て |
| `RecoverHp` | `Value` は 1 以上。最大体力で止める |
| `GainParamByShield` / `GainParamByMotivation` / `AddFixedShield` | `Value` は 1 以上(2026-11-01。マスタの検証の規則と同じ。Domain の検証にも入れた) |
| `AddStatus` | `Value` は 1 以上(減らす効果はない) |
| `DrawCards` | `Value` は 1 以上。引ける分だけ引く(lesson-card.md 4章) |
| どの `Op` も | 負の値は読み込み時にエラー(`CardDefinitionValidator`) |

- 好調・絶好調・消費体力減少などの「ターン」のステータスも、集中・好印象などの「値」のステータスも、`AddStatus` 1 つで足す。足し方の違い（11章の「プレイヤー操作中に初めて付与した」印）は `StatusKind` で決まる

## 2. 持続効果のトリガー（`PersistentTrigger`）

カードの種別での絞り込みは、条件ではなくトリガーの種類で表す。

| 値 | 名前 | タイミング（lesson-card.md 10章） | フェーズ |
|---|---|---|---|
| 0 | `Nothing` | 指定なし（エラー） | — |
| 1 | `LessonStart` | L1 | 1 |
| 2 | `TurnStart` | T3 | 1 |
| 3 | `AfterTurnStart` | T5 | 1 |
| 4 | `EveryNTurns` | T7（N は `AddPersistent` の `Value`） | 採用時 |
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

元気と体力はステータスではなく、Domain では `LessonState` に持つ（[lesson-domain-types.md](lesson-domain-types.md) 3章）。元気を扱う効果は `AddShield` などの専用の `Op` にする。

| 値 | 名前 | 用語（[glossary.md](../glossary.md)） | 単位 |
|---|---|---|---|
| 0 | `Nothing` | 指定なし | — |
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
| 11 | `ShieldGainBlock` | 元気増加無効 | ターン |
| 12 | `DebuffGuard` | 低下状態無効 | 回数 |

## 4. コスト（`CostKind`）

| 値 | 名前 | 内容（lesson-card.md 5章） | フェーズ |
|---|---|---|---|
| 0 | `Nothing` | 指定なし（エラー） | — |
| 1 | `Consume` | 元気 → 体力の順に払う。補正を受ける | 1 |
| 2 | `HpDirect` | 体力から直接払う。補正を受けない | 1 |
| 3 | `GoodConditionTurns` | 好調のターン | 1(実装済み。使うカードはフェーズ 3 だが無いので合成カードで確かめる) |
| 4 | `ExcellentTurns` | 絶好調のターン | 1(同上) |
| 5 | `Focus` | 集中 | 1(同上) |
| 6 | `Impression` | 好印象 | 1(実装済み。使うカードがないので合成カードで確かめる) |
| 7 | `Motivation` | やる気 | 1(同上) |

## 5. 条件（`ConditionSubject` / `Comparator`）

条件は `{ Subject, Comparator, Value }`。使用条件と効果内条件で同じ形（lesson-card.md 6.1）。

| 値 | `ConditionSubject` | `Value` | 例 |
|---|---|---|---|
| 0 | `Nothing` | 指定なし（エラー） | |
| 1 | `GoodConditionTurns` | ターン数（「状態である」は `GreaterOrEqual` 1） | S04、S11 |
| 2 | `ExcellentTurns` | ターン数 | |
| 3 | `Focus` | 値 | S05 |
| 4 | `Impression` | 値 | L02（`Impression` `GreaterOrEqual` 1） |
| 5 | `Motivation` | 値 | |
| 6 | `Shield` | 値（「元気が 0」は `Equal` 0） | |
| 7 | `HpPercent` | 最大体力に対する% | F05（`HpPercent` `GreaterOrEqual` 50） |
| 8 | `TurnNumber` | 何ターン目か | |
| 9 | `ClearTargetPercent` | 得たパラメータの、クリア目標に対する% | |

| 値 | `Comparator` |
|---|---|
| 0 | `Nothing`（エラー） |
| 1 | `GreaterOrEqual` |
| 2 | `LessOrEqual` |
| 3 | `Equal` |

## 6. 既にある enum の数値 [決定。2026-10-31]

`Nothing` を 0 に足し、ほかを 1 つずつずらす。保存したデータもマスタもまだ無いので、付け直してよい（[実装計画](../../plans/2026-10-31-lesson-enums-rename.md) 3.1）。

| enum | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|
| `Plan` | `Nothing` | `Free` | `Sense` | `Logic` | | |
| `CardType` | `Nothing` | `Active` | `Mental` | `Trouble` | | |
| `Rarity` | `Nothing` | `N` | `R` | `SR` | `SSR` | |
| `ParamKind` | `Nothing` | `Singing` | `Rhythm` | `Expression` | | |
| `LessonKind` | `Nothing` | `Normal` | `Special` | `Finishing` | `MidExam` | `FinalExam` |

- `ParamKind` を配列の添字に使うところは、`(int)kind - 1` ではなく大きさ 4 の配列にして添字 0 を使わない（`StatusSet` と同じ形）
- マスタの enum（[masterdata-pipeline.md](master/masterdata-pipeline.md) 2章の写し）も同じ名前・数値にする

## 7. 例（L14 の通常版。ID は説明用）

| テーブル | 行 |
|---|---|
| `CardCost` | `Consume` 4 |
| `CardFace` | `EffectGroupId` 1401 |
| `Effect`（1401） | 1: `AddStatus` `Impression` 3 ／ 2: `AddPersistent` `OnMentalCardUse` → `RefEffectGroupId` 1402 |
| `Effect`（1402） | 1: `AddStatus` `Impression` 1 |

### マスタの検証の規則 [決定。2026-11-01。ユーザーの確認済み]

「使う」列だけに値を置き、**使わない列は空欄（0）でなければエラー**（CSV では空のセル）。`RefConditionGroupId` は、どの `Op` でも使ってよい（空欄も可）。マスタの `Effect` の `IValidatable` が検証する。

| `Op` | 使う列と下限 |
|---|---|
| `GainParam` | `Value` ≥ 1、`Arg1`・`Arg2`・`Arg3`（0 以上。0 は既定値） |
| `GainParamByImpression` / `GainParamByShield` / `GainParamByMotivation` | `Value` ≥ 1 |
| `AddParamBonus` | `Value` ≥ 1、`Arg1` ≥ 1 |
| `AddShield` / `AddFixedShield` / `RecoverHp` / `DrawCards` / `AddCardUses` | `Value` ≥ 1 |
| `ScaleShield` | `Value` 0〜100 |
| `SetShield` | `Value` ≥ 0 |
| `AddStatus` | `Status`（`FocusBoost`・`ImpressionBoost` と `Nothing` は不可）、`Value` ≥ 1 |
| `AddStatusPercent` | `Status`（`FocusBoost` か `ImpressionBoost` のみ）、`Value` ≥ 1、`Arg1`（0 以上） |
| `ReplaceHand` / `EnhanceHand` | なし（`Value` も空欄） |
| `CreateCard` | `Value` ≥ 1、`RefCardId`（空欄不可） |
| `ScheduleEffect` | `Value` ≥ 1、`RefEffectGroupId`（空欄不可） |
| `AddPersistent` | `Trigger`（`Nothing` と `EveryNTurns` は不可。`EveryNTurns` は採用時まで弾く。`CardDefinitionValidator` と同じ）、`RefEffectGroupId`（空欄不可）、`Arg1`・`Arg2`（0 以上）。`Value` は空欄 |

`Status` は `AddStatus`・`AddStatusPercent` 以外で空欄、`Trigger` は `AddPersistent` 以外で空欄、`RefEffectGroupId` は `ScheduleEffect`・`AddPersistent` 以外で空欄、`RefCardId` は `CreateCard` 以外で空欄。入れ子の深さは 1（`RefEffectGroupId` が指すグループに `AddPersistent` / `ScheduleEffect` がない）。

条件付きの持続効果の例（説明用）: 「好印象が 3 以上なら、以降ターン開始時に好印象+1」は、`AddPersistent` の行の `RefConditionGroupId` に「`Impression` `GreaterOrEqual` 3」を付ける（付与するときに判定）。「以降、ターン開始時に好印象が 3 以上なら好印象+1」は、子（`AddStatus`）の行に付ける（発動のたびに判定）。

## 未決事項

- `CreateCard` の生成する位置の持ち方（採用時）
- `AddStatusPercent` の持続ターンの数え方（採用時。lesson-domain-types.md 3章）
- 引数が 3 つで足りない `Op` が出たときの扱い（masterdata-pipeline.md 3章）
- `CardDefinitionValidator` をマスタの検証の規則にそろえる時期（実装メモの差）

## 実装メモ

- 2026-11-01: マスタの行から `CardDefinition` を組み立てるために、Domain に `CardFaceKind`（`Nothing` = 0・`Normal` = 1・`Enhanced` = 2。マスタの同名の enum と同じ）を足した。サーバーのテストの `EnumPairs` で名前・数値の一致を確かめる

- 2026-11-01: コード（`shared/Uta.Shared.Domain/Lesson`）をこのページに合わせた（[実装計画](../../plans/2026-10-31-lesson-enums-rename.md)）。`Op` ごとの `Value`・`Arg`・参照の列の検証は、Domain では未実装
- 2026-11-01: マスタ（`masterdata/Definitions/Lesson.cs` の `Effect`）に上の「マスタの検証の規則」を実装した（[計画](../../plans/2026-11-01-masterdata-first.md)）。**`CardDefinitionValidator` との差**: Domain は今、負の値・`AddStatus`/`DrawCards` の下限・`Status`/`Trigger` の使わない列・持続効果の子・入れ子の深さを検証している。マスタとの差は、`Value`・`Arg`・参照の列の使わない列の検査と、ほかの `Op` の下限（`GainParam` など）。後の計画でそろえる
