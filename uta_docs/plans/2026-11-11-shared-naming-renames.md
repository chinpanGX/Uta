---
title: 実装計画: shared/ の型の改名（naming.md の差の表の Shared.Domain・Contracts の行）
status: draft
updated: 2026-11-11
spec: ../wiki/architecture/naming.md, ../wiki/architecture/api-design.md, ../wiki/architecture/lesson-domain-types.md, ../wiki/architecture/produce-domain.md, ../wiki/architecture/master/gacha-tables.md, ../wiki/systems/gacha.md
---

# 実装計画: shared/ の型の改名

## この計画で決めたこと

- **改名する型とメンバー**（naming.md「今のコードとの差」の行のとおり）:

  | 今 | 新 | 定義のファイル |
  |---|---|---|
  | Contracts の enum `GachaOutcome` | `GachaResultKind` | `shared/Uta.Shared.Contracts/Gacha/GachaOutcome.cs` → `GachaResultKind.cs`（`.meta` も） |
  | Contracts の `DrawResultDto.Outcome`（プロパティ）と、コンストラクターの引数 `outcome` | `ResultKind`・`resultKind` | `shared/Uta.Shared.Contracts/Gacha/DrawResultDto.cs` |
  | Shared.Domain の enum `UseCheckResult` | `UseCheckResultKind` | `shared/Uta.Shared.Domain/Lesson/LessonCommand.cs` の中のまま |
  | Shared.Domain の enum `LessonApplyResult` | `LessonApplyResultKind` | 同上 |
  | Shared.Domain の enum `ProduceApplyResult` | `ProduceApplyResultKind` | `shared/Uta.Shared.Domain/Produce/ProduceEnums.cs` の中のまま |
  | Shared.Domain の static class `ExamTurnBuilder` とメソッド `Build` | `ExamTurnFactory`・`Create` | `shared/Uta.Shared.Domain/Produce/ExamTurnBuilder.cs` → `ExamTurnFactory.cs`（`.meta` も） |
  | テストの class `ExamTurnBuilderTests` と、メソッド名の接頭辞 `Build_` | `ExamTurnFactoryTests`・`Create_` | `shared/Uta.Shared.Domain.Tests/Produce/ExamTurnBuilderTests.cs` → `ExamTurnFactoryTests.cs` |

- **Factory の作るメソッドは `Create`**: naming.md 7章の Factory の例（`CardCatalogFactory.Create`・`ProduceStateResultFactory.Create`）と同じにする。サーバーの改名の計画で `ProduceViewBuilder.Build` → `ProduceStateResultFactory.Create` にしたのと同じ扱い
- **enum の値の名前と数値は変えない**。`LessonEngine.UseCard` の `(LessonApplyResult)check`（`UseCheckResult` から数値で写す）は、型名だけ `(LessonApplyResultKind)check` にする
- **`DrawResultDto` はプロパティと引数の名前をそろえて変え、引数の順番・型と `[Key(n)]` は変えない**: MessagePack はキーの番号（`[Key(3)]`）で読み書きするので通信の形は変わらない（名前で直列化する設定・経路は無い）。`[SerializationConstructor]` のコンストラクターの引数とメンバーの対応は、MessagePack の版によって位置（キーの番号の順）か名前で取る（どちらかはこのリポジトリでは確かめていない）。どちらでも壊れないよう、プロパティ `ResultKind` と引数 `resultKind` を同じコミットで変え、引数の並び（`slot, itemKind, itemMasterId, resultKind, shards, supportPoints, supportCardUniqueId`）と型は今のままにする
- **Unity が読む `.meta` は `git mv` で一緒に改名する**（`shared/` は `client/uta/Packages/manifest.json` の `file:` で Unity に入るので、`shared/Uta.Shared.Domain/` と `shared/Uta.Shared.Contracts/` の `.cs` には `.meta` がある。GUID を変えない）。`shared/Uta.Shared.Domain.Tests/` には `.meta` が無い
- **変えない名前**:
  - `ExamOutcome` 系（enum `ExamOutcome`、`ProduceResult.Outcome`、`ProduceResultResponse.ExamOutcome`、サーバーの `ProduceEndResult.ExamOutcome`・`ProduceResultRecord.ExamOutcome`、`ProduceResultSnapshot.Outcome`）。[formation の計画](2026-11-03-client-screens-formation.md)のタスク 2 が行う（formation の R1・R6）。R1 に書かれていなかった後ろの 3 つ（サーバーの 2 つと `ProduceResultSnapshot.Outcome`）は、この計画を書く段階で formation の R6 のタスク 2 の箇条に書き足した（naming.md の「プロパティ名」の行が「Shared.Domain の `ExamOutcome` の改名と同じ計画で直す」としているため）
  - サーバーの `GachaResult.ToLogJson` のキー `"outcome"`、DB の列 `gacha_histories.outcome`（ログ・DB の形。サーバーの改名の計画と同じ扱い）
  - 戻り値を受けるローカル変数の名前（`var result = engine.Apply(...)` の `result`、`var check = CanUse(...)` の `check` など）
  - `uta_docs/plans/`・`uta_docs/raw/` の旧い名前（記録なので直さない）
- **client のテストの補助 `GachaFixtures` の引数名** `outcome` → `resultKind`（渡す先の `DrawResultDto` のコンストラクターの引数 `resultKind` に合わせる）
- **ファイルの改名は `git mv`**。`.cs` は BOM 付き UTF-8・LF のまま

- **いつ main へ入れるか: すぐ入れる**（2026-11-11。ユーザーの判断。lesson の後・formation の後と比べて選んだ）。理由: 後の計画（Lv・formation のタスク 2・week・lesson）の本文は改名の対象の名前を使っていない（2026-11-11 に Grep）ので読み替えが要らず、早く入れるほど直す箇所が増えない
- **順番は直列にする: この計画 → [Lv の実装](2026-11-09-player-level-from-exp.md) → formation のタスク 2**。Lv の計画は「formation の実装ブランチを切る前に main へ入れる」、formation は「Lv が main に入ってから」と既に決まっているので、この計画をその前に置く。Lv・formation の実装ブランチは、この計画の実装が main に入ってから切る（Lv の計画の「この計画で決めたこと」と formation の R6 に、この計画を書く段階で 1 行ずつ書き足した）。並行しないので、1.1 のファイルで git の衝突は出ない。順番が崩れて並行したときは、1.1 の「並行したときの見込み」のとおりに、後から main に入れる側が直し、main を取り込んだあと 4章の各タスクの `git grep` の確かめ方をもう一度回す。改名した型は main から消えるので、取り込んだ側に旧い名前が残ればコンパイルエラーになり、見落としは出ない（wiki はタスク 4 の `git grep` で見る）

## ユーザーの確認が要ること

なし（いつ main へ入れるかは 2026-11-11 にユーザーが決めた。上の節に移した）。

## 1. 目的と前提

[naming.md](../wiki/architecture/naming.md)「今のコードとの差（改名の対象）」のうち、`shared/`（Shared.Domain・Contracts）の行のうち `ExamOutcome` 系を除くもの（`GachaOutcome`・`DrawResultDto.Outcome`・`UseCheckResult`・`LessonApplyResult`・`ProduceApplyResult`・`ExamTurnBuilder`）を直し、使っている箇所（`shared/`・`server/`・`client/`）と、旧い名前を書いている wiki を同じ変更で直す。動き（RPC の形・DB・ゲームログ・乱数の消費）は変えない。

- 関係する ADR: なし（命名の規則は naming.md が正）
- 設計: [naming.md](../wiki/architecture/naming.md) 1章（`Outcome` は使わない）・2章（結果の種類は `<何の結果か>ResultKind`）・7章（組み立ては `<名詞>Factory`、`Builder` は Editor だけ）。`GachaResultKind` の名前と数値の正は [api-design.md](../wiki/architecture/api-design.md) 1.1
- [client の画面の基盤の計画](2026-11-03-client-screens-foundation.md)の R0 で「`GachaOutcome` と `DrawResultDto.Outcome` は `server/` を触る計画が行う」とした計画が、これに当たる

### 1.1 同じファイルを触るほかの計画（2026-11-11 の main で調べた）

順番を直列にした（「この計画で決めたこと」）ので、下の重なりは衝突にならない。「並行したときの見込み」は、順番が崩れたときのためのもの。

| ほかの計画 | 状態（2026-11-11） | 同じファイル | 並行したときの見込み |
|---|---|---|---|
| [Lv の実装](2026-11-09-player-level-from-exp.md) | ready。未着手 | `client/uta/Assets/Tests/EditMode/Support/GachaFixtures.cs`（Lv は 128 行の `Player` の `new PlayerEntity(...)` の引数を変える。この計画は 131・133・136・138 行の `IdolResult`・`SupportCardResult`） | 間に変わらない行が 2 行しか無いので、衝突するかもしれない（推測）。両方の変更を残す |
| 同上 | 同上 | `server/Uta.Server.Tests/Integration/ProducePlayTests.cs`（Lv は 90〜94 行と `Complete_LevelUp_AndMaxLevel`、この計画は 324 行の `ProduceApplyResult.Accepted`） | 離れているので自動でまとまる見込み |
| 同上 | 同上 | `uta_docs/wiki/architecture/api-design.md`（Lv は 1章の `SignInResponse`・1.3、この計画は 1.1 の `DrawAsync` の行） | 別の行。自動でまとまる見込み |
| [formation](2026-11-03-client-screens-formation.md) のタスク 2（`ExamOutcome` 系の改名） | ready。タスク 1 は PR #59 で済み | `shared/Uta.Shared.Domain/Produce/ProduceEnums.cs`（formation は 133 行の `ExamOutcome`、この計画は 125 行）、`ProduceEngine.cs`（formation は 691〜728 行、この計画は 165〜641 行）、`shared/Uta.Shared.Domain.Tests/Produce/ProduceEngineExamTests.cs`（formation は 182〜324 行、この計画は 50・339〜353 行）、`ProduceReplayTests.cs`（formation は 106 行、この計画は 24・121〜155 行）、`server/Uta.Server.Tests/Integration/ProducePlayTests.cs`（formation は 77・237 行、この計画は 324 行）、`uta_docs/wiki/architecture/produce-domain.md`（formation は 190 行の `ProduceResultSnapshot` など、この計画は 276・280 行） | 離れているので自動でまとまる見込み |
| 同上 | 同上 | `uta_docs/wiki/architecture/naming.md`（両方が差の表の「プロパティ名の `Outcome`・`ExamOutcome`」の行と前書きの段落を変える） | 同じ行。解き方は 1 つに決まる: この計画が先なら、formation はその行（この計画で `ExamOutcome` 系だけになった行）を消す。formation が先でその行が消えていたら、この計画はその行の書き換え（3.4 の 2 つ目の箇条）を飛ばす。前書きの段落は、両方の足した文を残す |
| [week](2026-11-03-client-screens-week.md)・[lesson](2026-11-03-client-screens-lesson.md) | ready | なし（本文に改名の対象の名前と、この計画が触るファイルの変更が無い。week は `ProduceEngine.cs` を根拠として挙げるだけ） | — |
| owned の O11（`Uta-wt/1` で作業中） | client の `Support` のファイルだけ | なし | — |

**client のコードが使うのは `GachaOutcome` と `DrawResultDto.Outcome` だけ**（gacha の画面。PR #57 で main に入った）。Shared.Domain の 3 つの enum と `ExamTurnBuilder` は、client のコードも、ready の client の計画（formation・week・lesson）の本文も使っていない（2026-11-11 に `git grep` で確かめた。formation の本文の `UseCheckResult.Ok` はタスク 1 の分で、PR #59 で済んだ）。

### 決めないこと（範囲外）

- `ExamOutcome` 系の改名（formation のタスク 2）
- 計画にある画面向けの名前（`OwnedListBuilder` など。各画面の計画の読み替えで行う）
- Contracts の `GachaStatusDto` などの、naming.md の差の表に無い名前
- `ExamTurnFactory` の中身（審査ターンの作り方・乱数の消費の順）。名前だけを変える

## 2. 触るファイル

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonCommand.cs` | enum `UseCheckResult`・`LessonApplyResult` の名前 | 1 |
| 変更 | `shared/Uta.Shared.Domain/Lesson/LessonEngine.cs` | 型名（14 行） | 1 |
| 変更 | `shared/Uta.Shared.Domain/Produce/ProduceEnums.cs` | enum `ProduceApplyResult` の名前 | 1 |
| 変更 | `shared/Uta.Shared.Domain/Produce/ProduceEngine.cs` | 型名（`ProduceApplyResult` 30 行・`UseCheckResult` 2 行・`LessonApplyResult` 1 行） | 1 |
| 変更 | `shared/Uta.Shared.Domain.Tests/Lesson/` の `ExamTests.cs`・`LessonEngineTests.cs`・`LessonReplayTests.cs`・`Phase2CardsTests.cs`・`Phase2OpsTests.cs`・`Phase3CardsTests.cs`・`Phase4CardsTests.cs` | 型名 | 1 |
| 変更 | `shared/Uta.Shared.Domain.Tests/Produce/` の `ProduceBot.cs`・`ProduceEngineExamTests.cs`・`ProduceEngineLessonTests.cs`・`ProduceEngineWeekTests.cs`・`ProduceReplayTests.cs`・`ProduceSnapshotTests.cs` | 型名 | 1 |
| 変更 | `server/Uta.Server/Application/Produce/ProduceStateResultFactory.cs`・`ProduceOperationUseCase.cs` | 型名 | 1 |
| 変更 | `server/Uta.Server.Tests/Infrastructure/ProduceStateSerializerTests.cs`・`Integration/ProducePlayTests.cs` | 型名 | 1 |
| 改名＋変更 | `shared/Uta.Shared.Domain/Produce/ExamTurnBuilder.cs`（と `.meta`）→ `ExamTurnFactory.cs`（と `.meta`） | class 名・`Build` → `Create` | 2 |
| 変更 | `shared/Uta.Shared.Domain/Produce/ProduceEngine.cs` | 641 行の呼び出し | 2 |
| 改名＋変更 | `shared/Uta.Shared.Domain.Tests/Produce/ExamTurnBuilderTests.cs` → `ExamTurnFactoryTests.cs` | class 名・メソッド名・呼び出し | 2 |
| 改名＋変更 | `shared/Uta.Shared.Contracts/Gacha/GachaOutcome.cs`（と `.meta`）→ `GachaResultKind.cs`（と `.meta`） | enum の名前 | 3 |
| 変更 | `shared/Uta.Shared.Contracts/Gacha/DrawResultDto.cs` | 引数・プロパティ・型名 | 3 |
| 変更 | `server/Uta.Server/Api/GachaService.cs`・`Domain/Gacha/GrantResultKind.cs` | キャスト・XML コメント | 3 |
| 変更 | `server/Uta.Server.Tests/Integration/GachaTests.cs`・`MasterData/ContractsEnumsMatchTests.cs` | 型名・プロパティ名 | 3 |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Gacha/GachaMapper.cs`・`Application/Gacha/GachaResult.cs` | 型名・プロパティ名・XML コメント | 3 |
| 変更 | `client/uta/Assets/Tests/EditMode/GachaMapperTests.cs`・`GachaUseCaseTests.cs`・`Support/GachaFixtures.cs` | 型名・引数名 | 3 |
| 変更 | `uta_docs/wiki/architecture/naming.md`・`api-design.md`・`lesson-domain-types.md`・`produce-domain.md`・`master/gacha-tables.md`、`uta_docs/wiki/systems/gacha.md` | 旧い名前 | 4 |

パスと出現の数は 2026-11-11 の main（`2f8b919`）のもの。

## 3. 型とシグネチャ

### 3.1 Shared.Domain の enum（タスク 1）

```csharp
// shared/Uta.Shared.Domain/Lesson/LessonCommand.cs（値・数値・XML コメントは今のまま）
public enum UseCheckResultKind { Ok = 0, NotPlayerPhase = 1, NotInHand = 2, NoUsesLeft = 3, UseConditionNotMet = 4, CannotPayCost = 5, }
public enum LessonApplyResultKind { Accepted = 0, NotPlayerPhase = 1, NotInHand = 2, NoUsesLeft = 3, UseConditionNotMet = 4, CannotPayCost = 5, NotSupported = 6, }

// shared/Uta.Shared.Domain/Produce/ProduceEnums.cs（同上）
public enum ProduceApplyResultKind { Accepted = 0, UnexpectedPhase = 1, InvalidArgument = 2, }
```

（上は値の一覧。ファイルの書式は今のとおり 1 行 1 値・Allman のまま、名前の行だけを変える）

- シグネチャの変わるメソッド: `LessonEngine.CanUse(LessonState, int)` → 戻り値 `UseCheckResultKind`、`LessonEngine.Apply(LessonState, LessonCommand, IRandom)` → `LessonApplyResultKind`、private の `LessonEngine.UseCard` → `LessonApplyResultKind`、`ProduceEngine.Apply(ProduceState, ProduceCommand, IRandomStreamSource)` → `ProduceApplyResultKind`、`ProduceEngine.CanUse(ProduceState, int)` → `UseCheckResultKind`、private の `ProduceEngine.ApplyWeekSelect`・`ApplyLesson`・`ApplyCardOffer`・`ApplyExamResult` → `ProduceApplyResultKind`。引数は変えない
- 手順は機械的な置き換え: 対象のファイル（2章のタスク 1 の行）で、単語の `UseCheckResult` → `UseCheckResultKind`、`LessonApplyResult` → `LessonApplyResultKind`、`ProduceApplyResult` → `ProduceApplyResultKind`（単語の一致。`UseCheckResultKind` を二重に `UseCheckResultKindKind` にしない）。Git Bash で次を 1 回流す（GNU sed は 1 行目の BOM と LF を変えない）:

  ```bash
  git grep -lw "UseCheckResult\|LessonApplyResult\|ProduceApplyResult" -- shared server | xargs sed -i -E 's/\b(UseCheckResult|LessonApplyResult|ProduceApplyResult)\b/\1Kind/g'
  ```

  流したあと、`git diff --numstat` で、変わったのが 2章のタスク 1 のファイルだけで、各ファイルの変わった行の数が出現の行の数と同じ（ファイル全体や 1 行目だけが差分になっていない）ことを確かめる

### 3.2 `ExamTurnFactory`（タスク 2）

```csharp
// shared/Uta.Shared.Domain/Produce/ExamTurnFactory.cs（本体・XML コメントは今の ExamTurnBuilder のまま）
public static class ExamTurnFactory
{
    /// <summary>paramValues は試験の開始時のパラメータ（添字は ProduceParams.ToIndex）。</summary>
    public static IReadOnlyList<ExamTurn> Create(ProduceCatalog catalog, IdolDefinition idol, ExamKind kind, IReadOnlyList<int> paramValues, IRandom random)
}
```

- `ProduceEngine.cs` 641 行: `var turns = ExamTurnBuilder.Build(catalog, idol, GetExamKind(week), state.CopyParams(), random);` → `var turns = ExamTurnFactory.Create(catalog, idol, GetExamKind(week), state.CopyParams(), random);`
- `ExamTurnFactoryTests.cs`: `public class ExamTurnBuilderTests` → `public class ExamTurnFactoryTests`、`ExamTurnBuilder.Build(` → `ExamTurnFactory.Create(`（11 か所）、メソッド名の接頭辞 `Build_` → `Create_`（`Build_Mid_TotalCountsMatchPattern`・`Build_Final_Sense_Has12Turns`・`Build_Final_Logic_Counts`・`Build_LastThreeTurns_AreThirdSecondFirst`・`Build_FirstTurn_FollowsFirstTurnDraw`・`Build_RateZero_FirstTurnAlwaysFirstParam`・`Build_ScoreBonusMatchesFormula`・`Build_SameSeed_SameResult` の 8 つ）

### 3.3 Contracts のガチャ（タスク 3）

```csharp
// shared/Uta.Shared.Contracts/Gacha/GachaResultKind.cs
namespace Uta.Shared.Contracts.Gacha
{
    public enum GachaResultKind
    {
        Nothing = 0,
        New = 1,
        Duplicate = 2,
    }
}
```

`DrawResultDto.cs`:

- コンストラクター: `public DrawResultDto(int slot, ItemKind itemKind, int itemMasterId, GachaResultKind resultKind, int shards, int supportPoints, string supportCardUniqueId)`、本体の `Outcome = outcome;` → `ResultKind = resultKind;`
- プロパティ: `[Key(3)] public GachaOutcome Outcome { get; }` → `[Key(3)] public GachaResultKind ResultKind { get; }`（`[Key(3)]` と、その上の XML コメントがあれば変えない）

サーバー:

- `Api/GachaService.cs` 51 行: `(GachaOutcome)(int)x.ResultKind` → `(GachaResultKind)(int)x.ResultKind`
- `Domain/Gacha/GrantResultKind.cs` の XML コメント: `/// <summary>GachaOutcome（Contracts）と同じ数値。</summary>` → `/// <summary>GachaResultKind（Contracts）と同じ数値。</summary>`
- `Tests/MasterData/ContractsEnumsMatchTests.cs`: 4 行の `using GachaOutcome = Uta.Shared.Contracts.Gacha.GachaOutcome;` → `using GachaResultKind = Uta.Shared.Contracts.Gacha.GachaResultKind;`、20・21 行の `GachaOutcome.` → `GachaResultKind.`。テストのメソッド名は変えない
- `Tests/Integration/GachaTests.cs`: `GachaOutcome` → `GachaResultKind`（11 か所）、DTO のプロパティの `.Outcome` → `.ResultKind`（66・113・116・118・119・121・123・141・158・304・336 行。116〜123 行はタプルの要素）

client:

- `Scripts/Infrastructure/Gacha/GachaMapper.cs` 43 行: `dto.Outcome == GachaOutcome.New,` → `dto.ResultKind == GachaResultKind.New,`
- `Scripts/Application/Gacha/GachaResult.cs` 27 行の XML コメント: `応答の Outcome が New` → `応答の ResultKind が New`（プロパティ `IsNew` は変えない）
- `Tests/EditMode/Support/GachaFixtures.cs`: `IdolResult(int slot, int idolId, GachaOutcome outcome, int shards)` → `IdolResult(int slot, int idolId, GachaResultKind resultKind, int shards)`、本体の `outcome` → `resultKind`。`SupportCardResult` も同じ
- `Tests/EditMode/GachaMapperTests.cs`（16・17 行）・`GachaUseCaseTests.cs`（35 行）: `GachaOutcome.` → `GachaResultKind.`

`using` は足さない・消さない（名前空間 `Uta.Shared.Contracts.Gacha` は同じ）。

### 3.4 wiki（タスク 4）

`uta_docs/wiki/architecture/naming.md`:

- 「今のコードとの差」の表で、1 列目が「Contracts の `GachaOutcome`」「Shared.Domain の enum の `UseCheckResult`・…」「Shared.Domain の `ExamTurnBuilder`」の 3 行を消す
- 1 列目が「プロパティ名の `Outcome`・`ExamOutcome`」の行で、括弧の中の「`DrawResultDto.Outcome`、`ProduceResultResponse.ExamOutcome` など」を「Shared.Domain の `ProduceResult.Outcome`、Contracts の `ProduceResultResponse.ExamOutcome` など」にする（`DrawResultDto.Outcome` は済んだため）
- 前書きの、サーバーの改名の計画で済んだと書いた文（「サーバーだけで閉じる改名（…）は…で済んだ。」）の直後に、次の文を足す（同じ段落の続き）

  ```markdown
  shared/ の改名（`GachaResultKind`・`DrawResultDto.ResultKind`・`UseCheckResultKind`・`LessonApplyResultKind`・`ProduceApplyResultKind`・`ExamTurnFactory`）は[shared/ の改名の計画](../../plans/2026-11-11-shared-naming-renames.md)で済んだ。
  ```
- frontmatter の `updated` を実装した日にする

`uta_docs/wiki/architecture/api-design.md` 1.1 の `DrawAsync` の行（82 行目）: `` `Outcome`（enum。型名は `GachaOutcome`。 `` → `` `ResultKind`（enum。型名は `GachaResultKind`。 ``（続く「`Nothing` = 0 / …名前と数値の正はここ。」は変えない）。`updated` を実装した日にする

`uta_docs/wiki/architecture/master/gacha-tables.md` 24 行目: `` `Outcome`(New / Duplicate)はマスタで使わないので `` → `` `GachaResultKind`(New / Duplicate)はマスタで使わないので ``。`updated` を実装した日にする

`uta_docs/wiki/systems/gacha.md`:

- 68 行目: `` `Outcome`(新規か重複か)の enum `` → `` `GachaResultKind`(新規か重複か)の enum ``
- 103 行目: `` 結果の種類(`Outcome`。DTO と履歴の `outcome`) `` → `` 結果の種類(`GachaResultKind`。DTO の `ResultKind` と、履歴の列 `outcome`) ``
- 105 行目（表の見出し）: `` | 場合 | `Outcome` | `` → `` | 場合 | `ResultKind` | ``
- `updated` を実装した日にする（ゲームの挙動は変えない。名前だけ）

`uta_docs/wiki/architecture/lesson-domain-types.md` 94 行目: `` | `UseCheckResult` / `LessonApplyResult` | `` → `` | `UseCheckResultKind` / `LessonApplyResultKind` | ``。`updated` を実装した日にする

`uta_docs/wiki/architecture/produce-domain.md`:

- 276 行目: `` `ExamTurnBuilder`(審査ターンの列) `` → `` `ExamTurnFactory`(審査ターンの列) ``
- 280 行目: `` `LessonApplyResult` の拒否 `` → `` `LessonApplyResultKind` の拒否 ``
- 190 行目の `ProduceResultSnapshot` の `Outcome`(`ExamOutcome`) は変えない（`ExamOutcome` 系。formation）
- `updated` を実装した日にする

行番号は 2026-11-11 の main のもの。ずれていたら、旧い名前の文字列で探す。

## 4. タスク

各タスクは固定の worktree（`tools/worktree-pool.sh start <番号> claude/shared-naming-renames`）で行う。タスク 3 は client のコンパイルとテストを Unity 7 の Editor で確かめるので、その worktree の `client/uta` を Editor で開いておく。

### タスク 1: Shared.Domain の結果の enum を改名する

- 触るファイル: 2章のタスク 1 の行
- 手順: 3.1 のとおりに置き換える。enum の値・数値・XML コメントは変えない
- 確かめ方:
  - `git grep -nw "UseCheckResult\|LessonApplyResult\|ProduceApplyResult" -- shared server client` が 0 件
  - `dotnet build Uta.slnx`（警告 0・エラー 0）
  - `dotnet test --project shared/Uta.Shared.Domain.Tests`、`dotnet test --project server/Uta.Server.Tests` がすべて通る（結合テストは Docker が要る）

### タスク 2: `ExamTurnBuilder` を `ExamTurnFactory` に改名する

- 触るファイル: 2章のタスク 2 の行
- 手順:
  1. `git mv shared/Uta.Shared.Domain/Produce/ExamTurnBuilder.cs shared/Uta.Shared.Domain/Produce/ExamTurnFactory.cs`、`git mv shared/Uta.Shared.Domain/Produce/ExamTurnBuilder.cs.meta shared/Uta.Shared.Domain/Produce/ExamTurnFactory.cs.meta`、`git mv shared/Uta.Shared.Domain.Tests/Produce/ExamTurnBuilderTests.cs shared/Uta.Shared.Domain.Tests/Produce/ExamTurnFactoryTests.cs`
  2. 3.2 のとおりに書き換える
- 確かめ方:
  - `git grep -nw "ExamTurnBuilder\|ExamTurnBuilderTests" -- shared server client` が 0 件
  - `.meta` の中身（`guid`）が改名の前と同じ（`git diff -M --stat` で `ExamTurnBuilder.cs.meta => ExamTurnFactory.cs.meta` が 100% の改名として出る）
  - `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project shared/Uta.Shared.Domain.Tests` がすべて通り、`ExamTurnFactoryTests` の 8 つが含まれる

### タスク 3: Contracts の `GachaOutcome` と `DrawResultDto.Outcome` を改名する

- 触るファイル: 2章のタスク 3 の行
- 手順:
  1. `git mv shared/Uta.Shared.Contracts/Gacha/GachaOutcome.cs shared/Uta.Shared.Contracts/Gacha/GachaResultKind.cs`、`git mv shared/Uta.Shared.Contracts/Gacha/GachaOutcome.cs.meta shared/Uta.Shared.Contracts/Gacha/GachaResultKind.cs.meta`
  2. 3.3 のとおりに書き換える（`DrawResultDto` の引数とプロパティは同じコミットで変える）
- 確かめ方:
  - `git grep -nw "GachaOutcome" -- shared server client` が 0 件
  - `git grep -nw "Outcome" -- shared/Uta.Shared.Contracts/Gacha server/Uta.Server/Api/GachaService.cs server/Uta.Server.Tests/Integration/GachaTests.cs client/uta/Assets/Scripts client/uta/Assets/Tests` が 0 件（`Scripts/Editor/Prefabs/Screens/Produce/ExamResultPageBuilder.cs` の `OutcomeText`・`outcomeText` は別の語なので `-w` では当たらない）
  - `.meta` の `guid` が改名の前と同じ（タスク 2 と同じ確かめ方）
  - `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests` がすべて通る（`GachaTests` で、ガチャの応答の `ResultKind` が今の `Outcome` と同じ値で返ることを確かめる）
  - **Unity でしか確かめられない**: `client/uta` で `unity command package resolve`（`file:` の shared の改名を読ませる）→ `unity recompile --format json` がエラー 0。EditMode のテスト（CLAUDE.md「ビルド・テスト・実行」の `unity command --timeout 300 eval` で `TestRunnerApi` を呼ぶ形）で `GachaMapperTests`・`GachaUseCaseTests` を含む全件が通る。Editor を開けない環境では、確かめられなかったと実装メモに書き、PR の本文にも書く

### タスク 4: wiki を直す

- 触るファイル: 2章のタスク 4 の行
- 手順: 3.4 のとおりに直す
- 確かめ方:
  - `git grep -nw "GachaOutcome\|UseCheckResult\|LessonApplyResult\|ProduceApplyResult\|ExamTurnBuilder" -- uta_docs/wiki` が 0 件（`uta_docs/plans/`・`uta_docs/raw/` は記録なので直さない）
  - `git grep -nw "Outcome" -- uta_docs/wiki/systems/gacha.md uta_docs/wiki/architecture/master/gacha-tables.md uta_docs/wiki/architecture/api-design.md` が 0 件
  - `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0

## 5. テスト

足すテストは無い（名前だけの変更で、動きを変えない）。既存のテストの改名だけ:

| テスト | 変更 |
|---|---|
| `ExamTurnBuilderTests`（8 件） | `ExamTurnFactoryTests` に改名。メソッド名の接頭辞を `Create_` に。中身は呼び出しだけ |
| shared のレッスン・プロデュースのテスト（タスク 1 の行） | 型名だけ。エンジンの結果の値が変わらないことを、今のテストがそのまま確かめる |
| サーバーの `GachaTests`・`ContractsEnumsMatchTests`・`ProducePlayTests`・`ProduceStateSerializerTests` | 型名・プロパティ名だけ |
| client の `GachaMapperTests`・`GachaUseCaseTests` | 型名だけ（Unity でしか確かめられない） |

Windows の IL2CPP のプレイヤーのビルドは確かめない（型を足さず名前だけを変えるため。`client/uta/Assets` に `link.xml` も、旧い名前を文字列で引く所も無い。2026-11-11 に確かめた）。

実行するコマンド（各タスクの終わり）:

- `dotnet build Uta.slnx`
- `dotnet test --project shared/Uta.Shared.Domain.Tests`
- `dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）
- タスク 3 だけ: `client/uta` で `unity command package resolve`、`unity recompile --format json`、EditMode のテスト（Editor を開いたまま。CLAUDE.md「ビルド・テスト・実行」）

## 6. 未決事項

なし。
