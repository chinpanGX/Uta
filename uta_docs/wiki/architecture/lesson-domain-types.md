---
title: レッスンの共有ドメインの型（Uta.Domain）
status: draft
updated: 2026-10-31
---

# レッスンの共有ドメインの型（Uta.Domain）

[lesson-card.md](../systems/lesson-card.md) の 3章（カード定義）・7章（効果）・8章（ステータス）・10章（処理順）と、[produce.md](../systems/produce.md) 10.2 の乱数を、`shared/Uta.Domain` の型に落とした設計。置き場とルールは [repository-layout.md](repository-layout.md) 5・6章、[ADR 0008](../decisions/0008-shared-domain-and-onion.md)。

## 凡例

- **[決定]**: 2026-10-28 にこの設計で進めると決めたこと
- **[Uta案]**: 仕様に書かれていない細部を、実装のために仮に決めたもの

## 決まっていること [決定]

- netstandard2.1、C# 9。`record`・`init`・nullable の注釈・リフレクションは使わない。コンストラクタと読み取り専用プロパティ、`switch` での切り替えで組む
- `float` / `double` を使わない。倍率はパーセントの整数、確率は万分率の整数。計算の途中は `long` の分子と分母で持ち、最後に1回だけ丸める（lesson-card.md 9.6 の「整数の固定小数点」を採る）
- 効果はマスタの `Effect` の行と同じ形（`Op`・`Status`・`Trigger`・`Value`・`Arg1`〜`Arg3`・参照先のカード ID）で持つ。enum の名前・数値と列の対応は [lesson-enums.md](lesson-enums.md) が正 [決定。2026-10-31]
- マスタの生成物には依存しない。カード定義はポート `ICardCatalog` 越しに受け取る
- 表示用のイベント列（演出に使う「何が起きたか」の記録）は後回し
- 単体テストは `shared/Uta.Domain.Tests/`（xUnit v3、.NET 10）

## 1. 名前空間とファイル

| 名前空間 | 中身 |
|---|---|
| `Uta.Domain.Numerics` | `FixedMath`（整数の切り上げ・切り捨て除算） |
| `Uta.Domain.Rng` | `IRandom`、`RandomOps`、`SplitMix64`、`Xoshiro256StarStar`、`RandomStreamKind`、`RandomStreamKey`、`RandomStreams` |
| `Uta.Domain.Lesson` | カード定義・状態・計算式・エンジン |

## 2. カード定義（lesson-card.md 3章・7章）

| 型 | 責務 |
|---|---|
| `Plan` / `CardType` / `Rarity` | プラン、種別、レアリティの enum |
| `CardDefinition` | ID（`int`）・プラン・種別・レアリティ・解放Lv・レッスン中1回・重複不可・開始時手札と、通常版・強化版の `CardFace`。不調カードは強化版を持たない |
| `CardFace` | コスト・使用条件・効果の配列（記載順） |
| `CostDef` / `CostKind` | コスト（readonly struct）。種類は 5章の7種 |
| `ConditionDef` / `ConditionSubject` / `Comparator` | 条件 `{対象, 比較, 値}`（readonly struct）。使用条件と効果内条件の両方に使う |
| `EffectDef` / `EffectOp` | 効果。`Op`・`Status`・`Trigger`・`Value`・`Arg1`〜`Arg3`・`RefCardId`・効果内条件・子の効果（発動予約と持続効果の中身） |
| `StatusKind` | 8章のステータスの enum |
| `PersistentTrigger` | 持続効果が反応するタイミングの enum |
| `ICardCatalog` | ID からカード定義を返すポート |
| `LessonRules` | lesson-card.md 4.1 の値 |
| `LessonSetup` | 1回のレッスンに渡す値（種類・ターン数・目標・対象のパラメータ） |
| `CardDefinitionValidator` | 引数の検査と、未対応の `Op` の検出（読み込み時にエラーにする。15.1） |

### 2.1 `Op` ごとの引数

[lesson-enums.md](lesson-enums.md) 1章が正（2026-10-31 に、ここにあった [Uta案] の表から移した）。

- `EffectDef` は `Status`（`StatusKind`）と `Trigger`（`PersistentTrigger`）を型付きのプロパティで持つ。`Arg1`〜`Arg3` に enum を数値で入れない
- 持続効果の「アクティブ使用時」「メンタル使用時」は、`PersistentTrigger` の種類で区別する（`OnActiveCardUse` など）
- `AddPersistent` の効果内条件は、付与するかどうかの条件（ほかの `Op` と同じ）。発動のたびに見る条件は、子の効果の効果内条件で表す
- コードはまだ古い名前・数値のまま。直す手順は [実装計画](../../plans/2026-10-31-lesson-enums-rename.md)

## 3. 状態（lesson-card.md 4章・8章・11章）

| 型 | 責務 |
|---|---|
| `CardInstance` | インスタンス ID・カード ID・強化済みか（[produce.md](../systems/produce.md) 10.4） |
| `Zones` | 山札・手札・捨札・除外を、インスタンス ID の並びで持つ。山札の先頭が一番上。ドロー、再シャッフル、ランダムな位置への挿入 |
| `StatusSet` | 8章のステータスの値と、「プレイヤー操作中に初めて付与した」印（11章） |
| `ParamBonusEntry` | パラメータ上昇量増加の、% と残りターンの組 |
| `ScheduledEffect` / `PersistentEffect` | 発動予約と持続効果。付与した順番を持つ |
| `LessonPhase` | レッスン開始 / ターン開始 / プレイヤー操作 / ターン終了 / 終了 |
| `LessonState` | 上のすべてと、ターン・体力・最大体力・元気・残りの使用数・レッスン中強化・得たパラメータ。サーバーが保存の形に変換できるよう、全部の値を読めて、全部の値から作り直せる |

- 元気と体力はステータスではなく `LessonState` に持つ
- 「スキルカード使用数追加」はステータスにせず、残りの使用数（ターン開始で基本値に戻す）に足す [Uta案]
- 集中強化・好印象強化の持続ターンは、フェーズ4で決める（いまは値だけ）

## 4. 乱数（produce.md 10.2）

| 型 | 責務 |
|---|---|
| `IRandom` | `NextUInt64()` だけを持つ |
| `RandomOps` | 棄却法の `NextInt`、万分率の判定、範囲、重み付き抽選、Fisher–Yates。取り出し方はここだけに置く |
| `SplitMix64` | ストリームの初期化 |
| `Xoshiro256StarStar` | 本体。状態（64 ビット × 4）と引いた回数を出し入れできる |
| `RandomStreamKind` / `RandomStreamKey` / `RandomStreams` | キーとマスターシードからストリームを作る |

ストリームの作り方 [Uta案]: キー `(種類 << 48) | (週 << 32) | 挑戦回数` を SplitMix64 の混ぜる関数に通し、マスターシードと XOR した値を SplitMix64 の初期値にして、4回引いた値を `xoshiro256**` の状態にする。

## 5. 計算と処理（lesson-card.md 9章・10章）

| 型 | 責務 |
|---|---|
| `LessonFormulas` | 9.1 最終消費量、9.2 パラメータ上昇値、9.3 元気、9.4 好印象（9.5 試験スコアは試験の実装時） |
| `LessonCommand` | カードを使う / ドリンクを使う / スキップ |
| `UseCheckResult` / `LessonApplyResult` | 使用可否と、操作を受け付けたか |
| `LessonEngine` | `Start`（L1・L2 と1ターン目の T1〜T7）、`CanUse`（乱数なし。クライアントの予測にも使う）、`Apply`（P1〜P8、ターンが終われば E1〜E6 と次のターンの T1〜T7。入力待ちまで進める） |
| `EffectExecutor` | `Op` ごとの `switch` で効果を処理する |
| `ConditionEvaluator` | 条件の判定（P1・P2） |
| `TriggerDispatcher` | 同じタイミングの発動を、10章の順（サポートアビリティ → 育成アイテム → 持続効果）で並べて処理する |

処理の細部 [Uta案]（仕様に書かれていないもの）:

- 使ったカードは、効果の処理（P6）が終わってから捨札・除外へ移す。効果のドローで再シャッフルが起きても、使用中のカードは混ざらない
- 山札が0枚になった瞬間に再シャッフルする（4章）。ドローのときに山札が0枚で捨札があれば、そこでも再シャッフルする
- パーフェクト目標に届いたら、その時点で以降の効果を処理せずにレッスンを終える
- 1ターン目のドロー枚数は `max(0, 1ターン目の最低手札枚数 − 開始時手札の枚数)`
- スキップの体力回復は、E1 より前に行う

## 6. フェーズ1で動かす範囲（lesson-card.md 15.2）

| 区分 | 範囲 |
|---|---|
| 効果 | `GainParam`、`GainParamByImpression`、`AddGenki`、`AddStatus`、`DrawCards`、`AddCardUses`、`AddPersistent`（名前は [lesson-enums.md](lesson-enums.md)） |
| コスト | 全種類（ステータスを払うコストは仕組みが同じなので先に入れる） |
| 条件 | 全種類（同上） |
| 対象外 | 試験、ドリンク、育成アイテム、サポートアビリティ、上の表にない `Op` |

## 未決事項

- `LessonState` を保存の形へ変換する型（サーバーの `Infrastructure`。サーバーの実装時）
- 状態のハッシュ（produce.md 10.6）
- 表示用のイベント列

## 実装メモ

2026-10-28 にフェーズ0・1の範囲を実装した（`shared/Uta.Domain`、`shared/Uta.Domain.Tests`）。

- ファイルは型ごとに分けず、近いものをまとめた（enum は `Lesson/LessonEnums.cs`、カード定義・ポート・ルールは `Lesson/CardDefinition.cs`、状態の部品は `Lesson/LessonState.cs`、`EffectExecutor` と `TriggerDispatcher` は `Lesson/EffectExecutor.cs`）
- `EffectExecutor` と `TriggerDispatcher` は `internal`。外から使うのは `LessonEngine`・`ConditionEvaluator`・`LessonFormulas`・`CardDefinitionValidator`
- 状態を書き換えるメソッドは `internal`。サーバーは「全部の値を受け取るコンストラクタ」で作り直す（テストの `Replay_RestoredMidway_SameResultAsContinuousRun` で、途中で作り直しても結果が変わらないことを確かめた）
- `Uta.Domain` は `TreatWarningsAsErrors`。`Enum.IsDefined`（リフレクション）は使わず、範囲で確かめる
- 9.2 の計算は、分子が最大で 10^16 程度（パラメータ 1,000・倍率 300%）。桁あふれは `checked` で例外にする
- 乱数の参照値: SplitMix64（seed 1234567）と xoshiro256**（状態 1,2,3,4）は公開されている値と一致。ストリームの値は Python の独立した実装で計算した値をテストに固定した
- フェーズ1のカード 13 枚は、マスタではなくテストの `Phase1Cards` に定義している（マスタの生成は masterdata-pipeline.md の作業）
- テスト: xunit.v3 4.0.2。.NET 10 SDK の `dotnet test` は Microsoft.Testing.Platform を選ぶ必要があるため、リポジトリのルートに `global.json`（`test.runner`）を置いた。VSTest 用のパッケージ（`Microsoft.NET.Test.Sdk`・`xunit.runner.visualstudio`）は入れていない
- 未確認: Unity（IL2CPP）でのコンパイル。`shared/` を Unity に取り込む方法が未検証（repository-layout.md 8章）
