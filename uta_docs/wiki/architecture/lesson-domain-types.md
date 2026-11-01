---
title: レッスンの Shared.Domain の型（Uta.Shared.Domain）
status: draft
updated: 2026-11-01
---

# レッスンの Shared.Domain の型（Uta.Shared.Domain）

[lesson-card.md](../systems/lesson-card.md) の 3章（カード定義）・7章（効果）・8章（ステータス）・10章（処理順）と、[produce-domain.md](produce-domain.md) 3章 の乱数を、`shared/Uta.Shared.Domain` の型に落とした設計。置き場とルールは [repository-layout.md](repository-layout.md) 5・6章、[ADR 0008](../decisions/0008-shared-domain-and-onion.md)。

## 凡例

- **[決定]**: 2026-10-28 にこの設計で進めると決めたこと
- **[Uta案]**: 仕様に書かれていない細部を、実装のために仮に決めたもの
- **[未実装]**: コード（`shared/Uta.Shared.Domain`）にまだ無い型。書いてあるのは今の案で、シグネチャはその型を作る実装計画で決める（2026-10-31 に、実装済みの型と区別するために足した）

## 決まっていること [決定]

- netstandard2.1、C# 9。`record`・`init`・nullable の注釈・リフレクションは使わない。コンストラクタと読み取り専用プロパティ、`switch` での切り替えで組む
- `float` / `double` を使わない。倍率はパーセントの整数、確率は万分率の整数。計算の途中は `long` の分子と分母で持ち、最後に1回だけ丸める（lesson-card.md 9.6 の「整数の固定小数点」を採る）
- 効果はマスタの `Effect` の行と同じ形（`Op`・`Status`・`Trigger`・`Value`・`Arg1`〜`Arg3`・参照先のカード ID）で持つ。enum の名前・数値と列の対応は [lesson-enums.md](lesson-enums.md) が正 [決定。2026-10-31]
- マスタの生成物には依存しない。カード定義はポート `ICardCatalog` 越しに受け取る
- 表示用のイベント列（演出に使う「何が起きたか」の記録）は後回し
- 単体テストは `shared/Uta.Shared.Domain.Tests/`（xUnit v3、.NET 10）

## 1. 名前空間とファイル

| 名前空間 | 中身 |
|---|---|
| `Uta.Shared.Domain.Numerics` | `FixedMath`（整数の切り上げ・切り捨て除算） |
| `Uta.Shared.Domain.Rng` | `IRandom`、`RandomOps`、`SplitMix64`、`Xoshiro256StarStar`、`RandomStreamKind`、`RandomStreamKey`、`RandomStreams` |
| `Uta.Shared.Domain.Lesson` | カード定義・状態・計算式・エンジン |

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
| `LessonRules` | lesson-card.md 4.1 の値。`static class` の `const` にする（差し替えない。保存する状態にルールが入らないので、差し替えられると再生の結果が黙って変わるため）[決定。2026-11-01。ユーザーの判断]|
| `LessonSetup` | 1回のレッスンに渡す値(種類・ターン数・目標・対象のパラメータ)。`LessonSetup(LessonKind kind, ParamKind targetParam, int maxTurns, int clearTarget, int perfectTarget, ExamSetup exam = null)` [Uta案。2026-11-01。フェーズ 6 の計画]。**`IsExam` は `exam != null`**。種類が `MidExam`/`FinalExam` であることと `exam != null` が食い違えば `ArgumentException`。種類が `Nothing`、または試験でないのに対象のパラメータが `Nothing` なら `ArgumentOutOfRangeException`。**試験のときは対象のパラメータを使わない**(審査ターンごとのパラメータは `ExamTurn` が持つ。どの値でもよく、`Nothing` も許す)。試験のとき `clearTarget` と `perfectTarget` は 0 でなければ `ArgumentOutOfRangeException`(試験はどちらの目標も持たない。lesson-card.md 9.5) |
| `ExamSetup` | 試験に渡す値(フェーズ 6。lesson-card.md 9.5)。`ExamSetup(IReadOnlyList<ExamTurn> turns, int earlyEndScore)`: 審査ターンの列と早期終了スコア(0 ＝なし)。`ExamTurn(ParamKind param, int scoreBonusPercent)` は 1 ターン分で、`scoreBonusPercent` は [produce.md](../systems/produce.md) 5.2 の「最終スコアボーナス(%)」(信頼度補正の後。MVP は補正 0)。**週の進行(プロデュースのドメイン)が試験の開始時のパラメータから先に計算して渡す**。レッスンのエンジンは 5.2 の式を持たない(試験中にパラメータは変わらないので先に計算できる) [決定。2026-11-01。ユーザーの判断]。`LessonSetup` にはコンストラクタの省略可能な引数 `ExamSetup exam = null` で渡し、`IsExam` は `exam != null`。試験のとき `MaxTurns` は列の長さと同じでなければ例外。`ExamTurn` は `readonly struct`、`ExamSetup` は `sealed class`(プロパティは `Turns`・`EarlyEndScore`、`ExamTurn` は `Param`・`ScoreBonusPercent`)。`turns` が null・空、`param` が `Nothing`、`scoreBonusPercent` が負、`earlyEndScore` が負のとき例外。エンジンが読むのは `ScoreBonusPercent` と `EarlyEndScore` だけ(`Param` は表示のために持つ) [Uta案。2026-11-01]。レッスン開始時(L1。`Turn` が 0)に試験でパラメータが上がるときの倍率は、1 ターン目の値を使う [Uta案。2026-11-01。36 枚には起きない] |
| `CardDefinitionValidator` | 引数の検査と、未対応の `Op` の検出（読み込み時にエラーにする。[lesson-phases.md](lesson-phases.md) 1章）。検査する列は `Status`・`Trigger`・`Value`・`Arg` の負・コスト・条件・カードの区分。エラーの文言と検査の順は [実装計画](../../plans/2026-10-31-lesson-enums-rename.md) 3.4 |

### 2.1 `Op` ごとの引数

[lesson-enums.md](lesson-enums.md) 1章が正（2026-10-31 に、ここにあった [Uta案] の表から移した）。

- `EffectDef` は `Status`（`StatusKind`）と `Trigger`（`PersistentTrigger`）を型付きのプロパティで持つ。`Arg1`〜`Arg3` に enum を数値で入れない
- 持続効果の「アクティブ使用時」「メンタル使用時」は、`PersistentTrigger` の種類で区別する（`OnActiveCardUse` など）
- `AddPersistent` の効果内条件は、付与するかどうかの条件（ほかの `Op` と同じ）。発動のたびに見る条件は、子の効果の効果内条件で表す

## 3. 状態（lesson-card.md 4章・8章・11章）

| 型 | 責務 |
|---|---|
| `CardInstance` | インスタンス ID・カード ID・強化済みか（[produce-domain.md](produce-domain.md) 5章） |
| `Zones` | 山札・手札・捨札・除外を、インスタンス ID の並びで持つ。山札の先頭が一番上。ドロー、再シャッフル、ランダムな位置への挿入 |
| `StatusSet` | 8章のステータスの値と、「プレイヤー操作中に初めて付与したか」の印（11章）。添字は `(int)StatusKind` で、添字 0（`Nothing`）は使わない（長さ 13。作り直すときに 0 番目が 0 でなければ例外） |
| `ParamBonusEntry` | パラメータ上昇量増加の、% と残りターンと「プレイヤー操作中に初めて付与したか」の印 |
| `ScheduledEffect` / `PersistentEffect` | 発動予約と持続効果。付与した順番を持つ |
| `LessonPhase` | レッスン開始 / ターン開始 / プレイヤー操作 / ターン終了 / 終了 |
| `LessonState` | 上のすべてと、ターン・体力・最大体力・元気・残りの使用数・レッスン中強化・得たパラメータ。サーバーが保存の形に変換できるよう、全部の値を読めて、全部の値から作り直せる |

- 元気と体力はステータスではなく `LessonState` に持つ
- 「スキルカード使用数追加」はステータスにせず、残りの使用数（ターン開始で基本値に戻す）に足す [Uta案]
- 集中強化・好印象強化の持続ターンは、`AddStatusPercent` を採用するときに決める（いまは値だけ。[lesson-phases.md](lesson-phases.md) 1章）

## 4. 乱数（[produce-domain.md](produce-domain.md) 3章）

| 型 | 責務 |
|---|---|
| `IRandom` | `NextUInt64()` だけを持つ |
| `RandomOps` | 棄却法の `NextInt`、万分率の判定、範囲、重み付き抽選、Fisher–Yates。取り出し方はここだけに置く |
| `SplitMix64` | ストリームの初期化 |
| `Xoshiro256StarStar` | 本体。状態（64 ビット × 4）と引いた回数を出し入れできる |
| `RandomStreamKind` / `RandomStreamKey` / `RandomStreams` | キーとマスターシードからストリームを作る |

ストリームの作り方 [決定。2026-10-31。[produce-domain.md](produce-domain.md) 3章 はここを正とする]: キー `(種類 << 48) | (週 << 32) | 挑戦回数` を SplitMix64 の混ぜる関数に通し、マスターシードと XOR した値を SplitMix64 の初期値にして、4回引いた値を `xoshiro256**` の状態にする。

## 5. 計算と処理（lesson-card.md 9章・10章）

| 型 | 責務 |
|---|---|
| `LessonFormulas` | 9.1 最終消費量、9.2 パラメータ上昇値、9.3 元気、9.4 好印象（9.5 試験スコアは `ExamScore`(試験の実装時)） |
| `LessonCommand` | カードを使う / ドリンクを使う / スキップ |
| `UseCheckResult` / `LessonApplyResult` | 使用可否と、操作を受け付けたか |
| `LessonResult` / `LessonEndReason` | レッスンの出力(lesson-card.md 10.1): `LessonResult(int gained, int lastTurn, LessonEndReason reason, int hp)`。`LessonEngine.GetResult(LessonState)` で作る。終了理由は状態から導く(lesson-card.md 10.1)。フェーズ 6 で足す。`LessonResult` は `readonly struct`(プロパティ `Gained`・`LastTurn`・`Reason`・`Hp`)。`LessonEndReason` は `Nothing = 0`・`TurnsOver = 1`・`Perfect = 2`・`EarlyEnd = 3`(lesson-card.md 10.1)。`GetResult` は `public static`。レッスンが終わっていない(`IsFinished` でない)状態を渡したら `InvalidOperationException`。`LastTurn` は `state.Turn` [Uta案。2026-11-01] |
| `LessonEngine` | `Start`（L1・L2 と1ターン目の T1〜T7）、`CanUse`（乱数なし。クライアントの予測にも使う。**MVP のクライアントの予測は使用可否だけ**で、効果の見込みは出さない [決定。2026-11-01。ユーザーの判断]）、`Apply`（P1〜P8、ターンが終われば E1〜E6 と次のターンの T1〜T7。入力待ちまで進める） |
| `LessonStateComparer` | 再生した状態の比べ方([produce-domain.md](produce-domain.md) 7章)。`Describe(LessonState)` は状態の全部の値を `項目=値` の文字列の並びにし、`Diff(LessonState expected, LessonState actual)` は食い違った項目だけを返す(同じなら空)。.NET の単体テストと IL2CPP の確認で同じ関数を使う。形は [フェーズごとの実装計画](../../plans/2026-11-01-shared-lesson-phases.md) 3.2 [Uta案。2026-11-01]。今の `LessonEngineTests` の `Summarize` はこれに置き換える |
| `EffectExecutor` | `Op` ごとの `switch` で効果を処理する |
| `ConditionEvaluator` | 条件の判定（P1・P2） |
| `TriggerDispatcher` | 同じタイミングの発動を、10章の順（サポートアビリティ → 育成アイテム → 持続効果）で並べて処理する |

処理の細部 [Uta案]（仕様に書かれていないもの）:

- 使ったカードは、効果の処理（P6）が終わってから捨札・除外へ移す。効果のドローで再シャッフルが起きても、使用中のカードは混ざらない
- 山札が0枚になった瞬間に再シャッフルする（4章）。ドローのときに山札が0枚で捨札があれば、そこでも再シャッフルする（2026-10-31 に lesson-card.md 4章で [決定] にした）
- パーフェクト目標に届いたら、その時点で以降の効果を処理せずにレッスンを終える
- 1ターン目のドロー枚数は `max(0, 1ターン目の最低手札枚数 − 開始時手札の枚数)`
- スキップの体力回復は、E1 より前に行う

## 6. フェーズ1で動かす範囲（[lesson-phases.md](lesson-phases.md) 2章）

| 区分 | 範囲 |
|---|---|
| 効果 | `GainParam`、`GainParamByImpression`、`AddShield`、`AddStatus`、`DrawCards`、`AddCardUses`、`AddPersistent`（名前は [lesson-enums.md](lesson-enums.md)） |
| コスト | 全種類（ステータスを払うコストは仕組みが同じなので先に入れる） |
| 条件 | 全種類（同上） |
| 対象外 | 試験、ドリンク、育成アイテム、サポートアビリティ、上の表にない `Op` |

## 未決事項

- `LessonState` を保存の形へ変換する型（サーバーの `Infrastructure`。サーバーの実装時）

- 表示用のイベント列
- ~~マスタの行を受け取る型と、グループ ID を解決して `CardDefinition` を組み立てる関数~~: 2026-11-01 に実装した。行の型は `CardRow`・`CardFaceRow`・`CardCostRow`・`EffectRow`・`ConditionRow`（`CardRows.cs`）、組み立ては `CardCatalog.Create`（`CardCatalog.cs`。`ICardCatalog` の実装。問題があれば `InvalidOperationException`、`CardDefinitionValidator` も通す）。サーバーの詰め替えは `server/Uta.Server/Infrastructure/MasterData/MasterCardCatalog.cs`。クライアント側の詰め替えは未実装（[計画](../../plans/2026-11-01-card-master-match.md)）

## 実装メモ

2026-10-28 にフェーズ0・1の範囲を実装した（`shared/Uta.Shared.Domain`、`shared/Uta.Shared.Domain.Tests`）。

- ファイルは型ごとに分けず、近いものをまとめた（enum は `Lesson/LessonEnums.cs`、カード定義・ポート・ルールは `Lesson/CardDefinition.cs`、状態の部品は `Lesson/LessonState.cs`、`EffectExecutor` と `TriggerDispatcher` は `Lesson/EffectExecutor.cs`）
- `EffectExecutor` と `TriggerDispatcher` は `internal`。外から使うのは `LessonEngine`・`ConditionEvaluator`・`LessonFormulas`・`CardDefinitionValidator`
- 状態を書き換えるメソッドは `internal`。サーバーは「全部の値を受け取るコンストラクタ」で作り直す（テストの `Replay_RestoredMidway_SameResultAsContinuousRun` で、途中で作り直しても結果が変わらないことを確かめた）
- `Uta.Shared.Domain` は `TreatWarningsAsErrors`。`Enum.IsDefined`（リフレクション）は使わず、範囲で確かめる
- 9.2 の計算は、分子が最大で 10^16 程度（パラメータ 1,000・倍率 300%）。桁あふれは `checked` で例外にする
- 乱数の参照値: SplitMix64（seed 1234567）と xoshiro256**（状態 1,2,3,4）は公開されている値と一致。ストリームの値は Python の独立した実装で計算した値をテストに固定した
- フェーズ1のカード 13 枚は、マスタではなくテストの `Phase1Cards` に定義している（マスタの生成は masterdata-pipeline.md の作業）
- テスト: xunit.v3 4.0.2。.NET 10 SDK の `dotnet test` は Microsoft.Testing.Platform を選ぶ必要があるため、リポジトリのルートに `global.json`（`test.runner`）を置いた。VSTest 用のパッケージ（`Microsoft.NET.Test.Sdk`・`xunit.runner.visualstudio`）は入れていない
- 2026-11-01: フェーズ 2〜4・6 を実装した（[計画](../../plans/2026-11-01-shared-lesson-phases.md)）。決めたこと: 試験の `LessonSetup` は `exam != null` で試験とみなす。スコア換算は `EffectExecutor.GainParam` の入口の 1 か所（`LessonFormulas.ExamScore`）。早期終了の判定は E5 の後・E6 の前の 1 か所。`LessonStateComparer.Describe` は予約・持続効果の `Effects` を `Op` の数値の並びだけで出す（定義は同じカタログから来るため）。`ExamSetup` は `default(ExamTurn)` も弾く。L1（Turn 0）の試験の換算が 1 ターン目の倍率になる分岐は、今のエンジンに経路がなく、テストで確かめていない。`StatusSet.Add` は `internal` なので、低下状態無効は合成カードで確かめた
- 未確認: Unity（IL2CPP）でのコンパイル。`shared/` を Unity に取り込む方法が未検証（repository-layout.md 8章）
