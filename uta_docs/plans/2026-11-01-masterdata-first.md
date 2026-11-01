---
title: 実装計画: masterdata の最初の計画 1/2（骨組み・レッスンのマスタ・サーバーでの読み込み）
status: ready
updated: 2026-11-01
spec: ../wiki/architecture/master/masterdata-pipeline.md, ../wiki/architecture/master/lesson-tables.md, ../wiki/architecture/lesson-enums.md, ../wiki/systems/lesson-card.md, ../wiki/overview/game-spec.md
---

# 実装計画: masterdata の最初の計画 1/2（骨組み・レッスンのマスタ・サーバーでの読み込み）

設計レビュー（1 回目。整合性・実装可能性・設計の批評）の指摘を反映した版。レビューで「31 テーブルを 1 計画に入れると、タスクとレビューが大きすぎる」と指摘されたので、**1/2（この計画）と 2/2（[2026-11-01-masterdata-rest.md](2026-11-01-masterdata-rest.md)）に分けた**。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の **2 の前半**。`masterdata/` を作り、マスタ生成のツール（`gen.ps1`）、定義の共通部分、レッスン・カードのマスタ 5 テーブルの定義と CSV（スキルカード 36 枚）を置く。サーバー（`Uta.Server`）が `masterdata.bytes` を埋め込んで起動時に読み込み、テストで中身を確かめる。残りの 26 テーブル（プロデュース・育成アイドル・サポートカード・ガチャ）は 2/2 で、この計画が済んでから実装する。

- 仕様の正: テーブルと列は [lesson-tables.md](../wiki/architecture/master/lesson-tables.md)、enum と効果の列は [lesson-enums.md](../wiki/architecture/lesson-enums.md)、生成・配信は [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md)、値の出どころは [lesson-card.md](../wiki/systems/lesson-card.md) 14章
- 関係する ADR: [0007](../wiki/decisions/0007-masterdata-with-addons.md)、[0013](../wiki/decisions/0013-commit-masterdata-binary.md)（バイナリもコミット。バイナリを無視する `.gitignore` は足さない）、[0016](../wiki/decisions/0016-reference-game-values.md)（参考作品の値を使う）、[0001](../wiki/decisions/0001-ip-policy.md)（名称はオリジナル）、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)
- 前提: `external/MasterMemoryAddons` が取り込み済み（サブモジュール。`41b42ef`）。.NET 10 SDK。サーバーは `Uta.Server`（Api / Application / Infrastructure と、型がまだない Domain の 4 層。[ArchitectureTests](../../server/Uta.Server.Tests/Architecture/ArchitectureTests.cs)）
- **この計画で決めないこと**（範囲外）:
  - プロデュース・育成アイドル・サポートカード・ガチャのテーブル（2/2）
  - クライアント用の出力（`client/uta/` に生成ソースを置くこと、Unity に MasterMemory・`MasterMemory.Addons.Core` を入れる方法、`StreamingAssets` の読み方、実機での読み込み）。Unity に取り込む計画で `masterdata.yaml` に出力先を足す。masterdata-pipeline.md の未決事項のまま
  - マスタ → `Uta.Shared.Domain` の型への変換（`ICardCatalog` の実装など、各側の Infrastructure）。使う計画で作る
  - 初期付与の処理（`SignUpAsync` に足す。順 5）、ガチャ・強化・プロデュースの処理
  - CI（GitHub Actions）への `master-gen --check` の組み込み。`gen.ps1` に `check` を作るところまで
  - `[ClientOnly]` の列（表示名・アイコン。まだ定義しない）

## 2. この計画で決めたこと（wiki に書いていなかった細部）

2026-11-01 に、ツールを実際に動かして確かめた事実（表の「確認」）を含む。ユーザーの確認が要るものは 7章に並べる。

| 項目 | 決めたこと | 理由 |
|---|---|---|
| ツールの実行 | `dotnet run --project external/MasterMemoryAddons/src/MasterMemory.Addons.Tools -p:ManagePackageVersionsCentrally=false -- <command> --config masterdata/masterdata.yaml`。`gen.ps1` が包む | **確認**: Uta のルートの `Directory.Packages.props`（パッケージ版の一括管理）がサブモジュールのツールにも効き、付けないと `NU1008` で止まる。付ければ動く（アドオンのサンプルで `master-gen` が通ることを確認済み） |
| 定義の名前空間 | `Uta.MasterData`（ブロック形式）。生成されるソースは**定義の名前空間のまま**出る | **確認**: 出力先の `namespace` は `MasterDataSchema.cs` にだけ使われる。そろえて `Uta.MasterData` にする |
| `MemoryDatabase` の名前空間 | `server/Uta.Server/Infrastructure/MasterData/MasterDataAssembly.cs` に `[assembly: MasterMemory.MasterMemoryGeneratorOptions(Namespace = "Uta.MasterData")]` を置く | **確認**: 付けないと `RootNamespace`（`Uta.Server`）に出る。付けると `Uta.MasterData.MemoryDatabase`。層の名前空間（`Uta.Server.*`）に入らない |
| サーバー側の置き場所 | `server/Uta.Server/Infrastructure/MasterData/`（`Generated/`・`masterdata.bytes`・`masterdata.manifest.json`・`MasterDataAssembly.cs`）。読み込みのコード `MasterDataLoader` は `Infrastructure/` | repository-layout.md 3章のユーザーの判断（2026-11-01。生成物を参照してよいのは Infrastructure だけ）に従う。2章のツリーと masterdata-pipeline.md 5章の `Uta.Server/MasterData/` は、タスク 5 で直す。Api・Application・Domain が `Uta.MasterData` に依存しないことを `ArchitectureTests` の 1 ルールで確かめる（タスク 1） |
| テーブル名 | `[MemoryTable("…")]` はクラス名の先頭を小文字にした名前（`Card` → `card`、`CardFace` → `cardFace`）。CSV のファイル名も同じ（`cardFace.csv`）。`_` は使わない | 水平分割の区切りが `_` のため（masterdata-pipeline.md 2章）。水平分割は使わない |
| enum の CSV での名前 | `[InputName]` はメンバー名と同じ英語（`Nothing` も付ける） | 名前は仮称。glossary の仮称を CSV に持ち込まない（lesson-enums.md「命名の規則」）。テストで元の enum と名前・数値を比べやすい |
| `bool` の CSV の表記 | `TRUE` / `FALSE` | **確認**: アドオンが受け付ける表記（`TRUE`/`True`/`1` と `FALSE`/`False`/`0`） |
| クライアント用の出力 | **この計画では作らない**。`masterdata.yaml` の出力先は `server` の 1 つ | Unity に MasterMemory が入っていないので、`client/uta/` に生成ソースを置くとコンパイルできない。検証のためだけの出力先を作って `.gitignore` で除外すると、clean な clone で `gen.ps1 check` が必ず失敗し（`--check` は出力先ごとに生成ソースの有無を比べる）、ADR 0013 の「バイナリを無視する `.gitignore` は足さない」にも反する。クライアント用の定義の検証（C# 9・netstandard2.1 でのコンパイル）は、出力先を足すとき（Unity に取り込む計画）に行う |
| 生成ソースとバイナリ | サーバー用の生成ソースとバイナリ（`masterdata.bytes`）・マニフェスト（`masterdata.manifest.json`）をコミットする | ADR 0013。`master-gen` は実行のたびにマニフェストの `createdAt` が変わる。**CSV か定義を変えたときだけ実行する**（masterdata-pipeline.md 5章） |
| バイナリの持ち方 | `server/Uta.Server/Infrastructure/MasterData/masterdata.bytes` を**埋め込みリソース**にする（`Migrations\*.sql` と同じ。`LogicalName` は `Uta.Server.MasterData.masterdata.bytes`）。`MasterDataLoader.Load()` が埋め込みを読み、`Load(byte[])` はテストでバイト列を渡すために公開する | ファイルの場所・実行時の設定・テストの出力先のコピーを考えずに済む。置き場所が変わっても読み込みのコードが変わらない |
| 読み込みのタイミング | サーバーの起動時に 1 回、同期で読む（`migrate` の分岐の後）。失敗したら起動しない（`InvalidOperationException`） | 実行中に初めて失敗するより、起動時に分かる方がよい。`MemoryDatabase` は不変なので、シングルトンで共有する |
| 検証の置き場 | **自分の行の列だけで決まる検査は、行のクラスの `IValidatable<T>`（明示的実装）**に書く。**別の表を読む検査、同じ表の別の行を読む検査のうち `IValidatable` を使わないもの（表全体の検査、`[Exists]` で書けない参照の検査）は、サーバーのテスト `MasterDataInvariantTests` に書く**。属性（`[Exists]`・`[Unique]`）で書けるものは属性。**例外**: 効果の入れ子の深さは、masterdata-pipeline.md 3章のユーザーの判断（「マスタは `IValidatable` で」）に従い、`Effect` の `IValidatable` の `CallOnce()` の中で書く（`Effect` の表だけを読む） | 同じ不変条件を `IValidatable` とテストの両方に書かない。表をまたぐ検査をマスタ生成に入れると、検証のコードがクライアント用の出力にも入り、`[ServerOnly]` の表を読めない制約にぶつかる。**確認**: MasterMemory 3.0.4 の `IValidatable<T>` を定義に書くと、マスタ生成が「テーブル名 内容, PK = 値」の形で報告し、エラーのときはバイナリを書かない。述語は `Func<T, bool>`（式木ではない） |
| 定義の C# のバージョン | `masterdata/Definitions` は C# 9 の範囲で書く。**強制する仕組みはない**（クライアント用の出力を作らず、`masterdata/` に csproj もないため）。**使わない書き方**: ファイルスコープ namespace、グローバル using、`required`、`record`・`record struct`、nullable 型（`int?`）、生文字列リテラル、リストパターン、プライマリコンストラクタ、コレクション式（`[a, b]`）、Index/Range。レビューで見る | Unity に取り込む計画でクライアント用の出力を足すと、アドオンが netstandard2.1・C# 9 でコンパイルして検証する |
| 効果（`Effect`）の列の検証 | 3.3.1 の表のとおり。使わない列は空欄でなければエラー、使う列は下限を持つ。一般の「`Value`・`Arg` ≥ 0」より、`Op` ごとの規則を優先する | lesson-enums.md の未決事項「1章の表の『—』の意味と下限」を、この計画で決める（ユーザーの確認が要る。7章） |
| ID の振り方 | 3.5 のとおり。カード ID: F01〜F07 = 1〜7、S01〜S14 = 101〜114、L01〜L15 = 201〜215。効果・条件のグループ ID: `カードID × 100 + 面 × 10 + 連番`。**この 36 枚の便宜であり、以後の計画は従わなくてよい**（新しいカードには空いている ID を手で振る） | 「採番の規則は作らない」（repository-layout.md 10.1）は運用の方針のまま。36 枚に手で振る必要があるので、読みやすい規則で振る |
| `unlockLv`（仮の値） | N・R = 1、SR = 5、SSR = 10 | 参考作品の値はリポジトリにない。ユーザーの許可（2026-11-01）で仮の値を置く。Lv1 のプレイヤーに SR・SSR を出さない動きを確かめられる。タスク 5 で lesson-card.md 14章に「仮」と注記する |
| 式で決まる行 | 2/2 の `playerLevelExp`（99 行）・`supportLevelCost`（147 行）は、PowerShell の 1 行などで作ってよい（スクリプトはコミットしない） | 手で打つと途中の 1 行が狂いやすい。全行の式をテストで確かめる（2/2） |

## 3. 触るファイル

### 3.1 一覧

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `Directory.Packages.props` | `MasterMemory` の版 | 1 |
| 変更 | `server/Uta.Server/Uta.Server.csproj` | パッケージ・参照（タスク 1）、埋め込みリソース（タスク 4） | 1・4 |
| 作成 | `server/Uta.Server/Infrastructure/MasterData/MasterDataAssembly.cs` | `[assembly: MasterMemoryGeneratorOptions]` | 1 |
| 変更 | `server/Uta.Server.Tests/Architecture/ArchitectureTests.cs` | Api・Application・Domain が `Uta.MasterData` に依存しないテストを 1 件（層ごとに 1 ルール） | 1 |
| 作成 | `masterdata/masterdata.yaml` | 出力先の設定（タスク 2 に全文） | 2 |
| 作成 | `masterdata/gen.ps1` | ツールの呼び出し（タスク 2 に全文） | 2 |
| 作成 | `masterdata/.editorconfig` | `shared/.editorconfig` と同じ C# 9 の設定 | 2 |
| 作成 | `masterdata/Definitions/Common.cs` | `Plan`・`CardType`・`Rarity`・`ParamKind` | 2 |
| 作成 | `masterdata/Definitions/Lesson.cs` | レッスンの enum 7 つと表 5 つ | 2 |
| 作成（生成） | `server/Uta.Server/Infrastructure/MasterData/Generated/*.cs` | `gen.ps1 code-gen` が作る（手で書かない・直さない） | 2 |
| 作成 | `masterdata/csv/card.csv`・`cardFace.csv`・`cardCost.csv`・`effect.csv`・`condition.csv` | 3.5 の中身 | 3 |
| 作成（生成） | `server/Uta.Server/Infrastructure/MasterData/masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1 master-gen` が作る | 3 |
| 作成 | `server/Uta.Server/Infrastructure/MasterDataLoader.cs` | バイナリの読み込み | 4 |
| 変更 | `server/Uta.Server/Program.cs` | 起動時に読み込んで DI に登録 | 4 |
| 作成 | `server/Uta.Server.Tests/MasterData/MasterDataTests.cs` | 読み込み・件数・値・enum の写し | 4 |
| 作成 | `server/Uta.Server.Tests/MasterData/MasterDataInvariantTests.cs` | 表をまたぐ検証 | 4 |
| 変更 | `uta_docs/wiki/` の各ページ・`CLAUDE.md` | タスク 5（メインのセッション） | 5 |

`.meta` は不要（`masterdata/` と `server/` は Unity に入らない）。`.gitignore` と `.gitattributes`（`*.bytes binary` は既にある）は変えない。`appsettings.json` も変えない。

### 3.2 enum

`masterdata/Definitions` に置く enum。**数値は 0 を `Nothing` にし、「値」の列の順に振る**。lesson-enums.md・各 `*-tables.md` の名前・数値と**同じ**にする（`InputName` はメンバー名と同じ）。

| 置き場 | enum | メンバー（0 から順。明記のあるものは数値を使う） |
|---|---|---|
| Common.cs | `Plan` | `Nothing` 0 / `Free` 1 / `Sense` 2 / `Logic` 3 |
| | `CardType` | `Nothing` 0 / `Active` 1 / `Mental` 2 / `Trouble` 3 |
| | `Rarity` | `Nothing` 0 / `N` 1 / `R` 2 / `SR` 3 / `SSR` 4 |
| | `ParamKind` | `Nothing` 0 / `Singing` 1 / `Rhythm` 2 / `Expression` 3 |
| Lesson.cs | `EffectOp` | lesson-enums.md 1章の表の 0〜19（`Nothing`〜`AddPersistent`） |
| | `PersistentTrigger` | 同 2章の 0〜13 |
| | `StatusKind` | 同 3章の 0〜12 |
| | `CostKind` | 同 4章の 0〜7 |
| | `ConditionSubject` | 同 5章の 0〜9 |
| | `Comparator` | 同 5章の 0〜3 |
| | `CardFaceKind` | `Nothing` 0 / `Normal` 1 / `Enhanced` 2（`CardFace.Face` の列。lesson-tables.md の「通常 / 強化」） |

### 3.3 テーブルと列 — レッスン（`Lesson.cs`）

書式: `名前 型`（`int` は型を省く）。`PK(n)` = `[PrimaryKey(keyOrder: n)]`（単一キーは `[PrimaryKey]`）。`→ T.Col` = `[Exists(typeof(T), nameof(T.Col))]`、`→? ` = `AllowEmpty = true` 付き。クラスは `public class X { public … { get; init; } }`、文字列は `= ""`。区分は特記がなければ Common（属性なし）。

| クラス（`MemoryTable`） | 列 |
|---|---|
| `Card`（`card`） | `CardId` PK、`Plan Plan`、`CardType Type`、`Rarity Rarity`、`UnlockLv`、`bool OnceInLesson`、`bool Unique`、`bool StartInHand` |
| `CardFace`（`cardFace`） | `CardId` PK(0) → Card.CardId、`CardFaceKind Face` PK(1)、`EffectGroupId` → Effect.EffectGroupId、`UseConditionGroupId` →? Condition.ConditionGroupId |
| `CardCost`（`cardCost`） | `CardId` PK(0) → Card.CardId、`CardFaceKind Face` PK(1)、`Order` PK(2)、`CostKind Kind`、`Value` |
| `Effect`（`effect`） | `EffectGroupId` PK(0)、`Order` PK(1)、`EffectOp Op`、`StatusKind Status`、`PersistentTrigger Trigger`、`Value`、`Arg1`、`Arg2`、`Arg3`、`RefConditionGroupId` →? Condition.ConditionGroupId、`RefEffectGroupId` →? Effect.EffectGroupId、`RefCardId` →? Card.CardId |
| `Condition`（`condition`） | `ConditionGroupId` PK(0)、`Order` PK(1)、`ConditionSubject Subject`、`Comparator Comparator`、`Value` |

`[Exists]` が主キーでない列を指せることは、アドオンの docs で確認済み（defining-master-data.md）。

**検証**。メッセージは「テーブル名: 内容」の形にする（マスタ生成が主キーを付けて報告する）。

*行の検証（`IValidatable<T>`）*

- `Card`: `Plan`・`Type`・`Rarity` が `Nothing` でない。`UnlockLv` ≥ 1
- `CardFace`: `Face` が `Nothing` でない。`EffectGroupId` ≠ 0
- `CardCost`: `Face`・`Kind` が `Nothing` でない。`Value` ≥ 0。`Order` ≥ 1
- `Condition`: `Subject`・`Comparator` が `Nothing` でない。`Order` ≥ 1。`Value` ≥ 0
- `Effect`: `Order` ≥ 1、`Op` が `Nothing` でない、`Value`・`Arg1`〜`Arg3` ≥ 0、3.3.1 の表（`Op` ごとの規則。一般の下限より優先）。**入れ子の深さは 1**（`CallOnce()` で `Effect` の全行を読む）: ある行の `RefEffectGroupId` が指すグループの行に、`Op` が `AddPersistent` / `ScheduleEffect` の行がない

*表をまたぐ検証（`MasterDataInvariantTests`。タスク 4）*

- すべてのカードに、`Normal` と `Enhanced` の `CardFace` が 1 行ずつある
- すべての `CardFace` に、同じ `(CardId, Face)` の `CardCost` が 1 行以上ある。すべての `CardCost` に、同じ `(CardId, Face)` の `CardFace` がある

**`IValidatable<T>` の書き方**:

- `using MasterMemory;`（`IValidatable<T>`・`IValidator<T>`）。`Validate(Func<T, bool>, string)` は**行ごとに**呼ばれる。述語は `Func<T, bool>` で、式木ではないので、`switch`・`&&`・`||`・メソッド呼び出しを使ってよい。メッセージは呼び出しごとの定数（`Op` の名前は書き込む）。違反するとマスタ生成が「`<クラス名> - Validate failed: <メッセージ>, PK(...) = 値`」の形で報告し、バイナリを書かない（**確認**: 2026-11-01）
- `CallOnce()`（表ごとに 1 回だけ `true`）・`GetTableSet().TableData`（表の全行）・`Fail(メッセージ)` は、2026-11-01 にスパイクで、定義に書いてマスタ生成から動くことを確認した（違反を報告し、バイナリを書かない）
- 行の検証の例（`Card`）:

```csharp
[MemoryTable("card"), MessagePackObject(true)]
public class Card : IValidatable<Card>
{
    [PrimaryKey]
    public int CardId { get; init; }
    // …（列）

    void IValidatable<Card>.Validate(IValidator<Card> validator)
    {
        validator.Validate(x => x.Plan != Plan.Nothing, "card: Plan が Nothing");
        validator.Validate(x => x.UnlockLv >= 1, "card: UnlockLv が 1 未満");
    }
}
```

- `Effect` の例。3.3.1 の表の 1 行につき、**「使う列の下限」の `Validate` と「使わない列が空欄」の `Validate` を並べる**。使わない列の検査は、下のヘルパー `OnlyUses` に使う列だけ `true` で渡す:

```csharp
void IValidatable<Effect>.Validate(IValidator<Effect> validator)
{
    validator.Validate(x => x.Order >= 1, "effect: Order が 1 未満");
    validator.Validate(x => x.Op != EffectOp.Nothing, "effect: Op が Nothing");
    validator.Validate(x => x.Value >= 0 && x.Arg1 >= 0 && x.Arg2 >= 0 && x.Arg3 >= 0, "effect: Value か Arg が負");

    // Op ごとの規則（3.3.1）
    validator.Validate(x => x.Op != EffectOp.GainParam || x.Value >= 1, "effect: GainParam の Value が 1 未満");
    validator.Validate(
        x => x.Op != EffectOp.GainParam || OnlyUses(x, value: true, arg1: true, arg2: true, arg3: true),
        "effect: GainParam に使わない列がある");
    validator.Validate(x => x.Op != EffectOp.ReplaceHand || OnlyUses(x), "effect: ReplaceHand に使わない列がある");
    // …（3.3.1 の表の残りの Op も、同じ形で並べる）

    // 入れ子の深さは 1（表全体を 1 回だけ見る）
    if (validator.CallOnce())
    {
        var rows = validator.GetTableSet().TableData;
        var childGroups = new HashSet<int>();
        foreach (var row in rows)
        {
            if (row.RefEffectGroupId != 0)
            {
                childGroups.Add(row.RefEffectGroupId);
            }
        }

        foreach (var row in rows)
        {
            if (childGroups.Contains(row.EffectGroupId) && (row.Op == EffectOp.AddPersistent || row.Op == EffectOp.ScheduleEffect))
            {
                validator.Fail("effect: 子のグループ " + row.EffectGroupId + " に AddPersistent / ScheduleEffect がある");
            }
        }
    }
}

// 使う列を true で渡す。渡さなかった列がすべて空欄（0 / Nothing）なら true
private static bool OnlyUses(
    Effect x, bool value = false, bool arg1 = false, bool arg2 = false, bool arg3 = false,
    bool status = false, bool trigger = false, bool refEffect = false, bool refCard = false)
{
    return (value || x.Value == 0) && (arg1 || x.Arg1 == 0) && (arg2 || x.Arg2 == 0) && (arg3 || x.Arg3 == 0)
        && (status || x.Status == StatusKind.Nothing) && (trigger || x.Trigger == PersistentTrigger.Nothing)
        && (refEffect || x.RefEffectGroupId == 0) && (refCard || x.RefCardId == 0);
}
```

（`RefConditionGroupId` はどの `Op` でも使ってよいので、`OnlyUses` は見ない）


#### 3.3.1 `Effect` の `Op` ごとの列の規則

「使う」列だけに値を置き、**使わない列は空欄（0）でなければエラー**（CSV では空のセル。3.4）。`RefConditionGroupId` は、どの `Op` でも使ってよい（空欄も可）。

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
| `AddPersistent` | `Trigger`（`Nothing` と `EveryNTurns` は不可。`EveryNTurns` は採用時まで弾く。lesson-enums.md 2章・`CardDefinitionValidator` と同じ）、`RefEffectGroupId`（空欄不可）、`Arg1`・`Arg2`（0 以上）。`Value` は空欄 |

`Status` は `AddStatus`・`AddStatusPercent` 以外で空欄、`Trigger` は `AddPersistent` 以外で空欄、`RefEffectGroupId` は `ScheduleEffect`・`AddPersistent` 以外で空欄、`RefCardId` は `CreateCard` 以外で空欄。


### 3.4 CSV の書式

- UTF-8（BOM なし）、LF、区切りはカンマ。ファイル名は `<MemoryTable の名前>.csv`（例: `cardFace.csv`）
- 1 行目: クラス名（例: `CardFace`）。2 行目: `出典: uta_docs/wiki/systems/lesson-card.md 14章`（この計画の 5 ファイルとも同じ）。3 行目: 型（`int`・`string`・`bool`・enum の名前。参考）。4 行目: ヘッダ（**C# のプロパティ名そのまま**。全列を書く）。5 行目から: データ。1〜3 行目は、ヘッダの列数に合わせてカンマで埋める（アドオンの `samples/csv/` と同じ形）
- 空欄は型の既定値。`bool` は `TRUE` / `FALSE`。enum は 3.2 の名前（空欄は `Nothing`）
- **参照の列（`UseConditionGroupId`・`RefConditionGroupId`・`RefEffectGroupId`・`RefCardId`）は、使わないときセルを空にする**（`0` と書かない。`[Exists(AllowEmpty = true)]` は空のセルだけを飛ばし、`0` は「ID 0 を探す」ことになって失敗する）。ほかの数値の列は、使わないとき空欄でも `0` でもよいが、そろえて空欄にする
- 行の並びは主キー順

### 3.5 CSV の中身

出典は lesson-card.md 14章。**ID の規則**:

- `CardId`: F01〜F07 = 1〜7、S01〜S14 = 101〜114、L01〜L15 = 201〜215（F08 は作らない）
- 面: `Normal` = 1、`Enhanced` = 2。`EffectGroupId` = `CardId × 100 + 面 × 10 + 1`（`CardFace.EffectGroupId`）。持続効果の子のグループは `+ 2`（`× 100 + 面 × 10 + 2`）
- `ConditionGroupId` = `CardId × 100 + 面 × 10 + 連番`。連番 1 = 使用条件、連番 2 = 効果内条件（下の表の「条件」）
- `CardCost` は面ごとに 1 行（`Order` = 1）。「消費 0」も `Consume` の値 0 の行を置く
- 効果は表の左から右の順に `Order` 1, 2, 3…（`Effect` の `EffectGroupId` ごと）

**共通の列**（`card`）: 下の 3 つの表の「Pl」= `Plan`（F = `Free`、S = `Sense`、L = `Logic`）、「種」= `Type`（A = `Active`、M = `Mental`）、「レア」= `Rarity`、「制限」= `OnceInLesson`（1回）・`Unique`（重）・`StartInHand`（手）。制限に書いたものだけ `TRUE`、ほかは `FALSE`。`UnlockLv` は N・R = 1、SR = 5、SSR = 10。

**効果の書き方**: `Op(Status/Trigger) 値 [Arg…] {条件}`。略: `GP` = `GainParam`、`GPI` = `GainParamByImpression`、`GPS` = `GainParamByShield`、`GPM` = `GainParamByMotivation`、`Shield` = `AddShield`、`Status(名)` = `AddStatus`（`Status` 列に名）、`Uses` = `AddCardUses`、`Draw` = `DrawCards`、`Scale` = `ScaleShield`、`Persist(トリガー)` = `AddPersistent`（`Value` と `Arg` は空欄で、`RefEffectGroupId` = 子のグループ）。`GP 10 A3=200` は `Value` 10・`Arg3` 200。`{…}` は効果内条件（`RefConditionGroupId` = 連番 2 の条件グループ）。コストは `C` = `Consume`、`H` = `HpDirect`。使用条件は `UC`。

条件の略: `GCT` = `GoodConditionTurns`、`Foc` = `Focus`、`Imp` = `Impression`、`HpP` = `HpPercent`。比較は `>=` = `GreaterOrEqual`。

#### 共通・感性・理論

| ID | Pl | 種 | レア | 制限 | 通常 | 強化 |
|---|---|---|---|---|---|---|
| 1 F01 | F | A | N | | C4 / GP 9 | C3 / GP 14 |
| 2 F02 | F | A | N | | C3 / GP 2, Shield 2 | C3 / GP 6, Shield 4 |
| 3 F03 | F | M | N | 1回 | C0 / Shield 4 | C0 / Shield 7 |
| 4 F04 | F | M | R | | C0 / Shield 2, Status(CostHalf) 2 | C0 / Shield 2, Status(CostHalf) 3 |
| 5 F05 | F | M | R | 1回 重 | C0 / Shield 3, Status(CostReduce) 1 {HpP>=50} | C0 / Shield 6, Status(CostReduce) 1 {HpP>=50} |
| 6 F06 | F | M | SR | 1回 重 | H1 / Uses 1, Draw 2 | C0 / Uses 1, Draw 2, Status(CostHalf) 1 |
| 7 F07 | F | M | SR | 1回 | C0 / Shield 11, Status(ShieldGainBlock) 2, Status(CostHalf) 3 | C0 / Shield 13, Status(ShieldGainBlock) 2, Status(CostHalf) 4 |
| 101 S01 | S | M | N | | C1 / Shield 1, Status(GoodCondition) 2 | C1 / Shield 1, Status(GoodCondition) 3 |
| 102 S02 | S | M | N | | C1 / Shield 1, Status(Focus) 2 | C1 / Shield 1, Status(Focus) 3 |
| 103 S03 | S | A | N | 1回 | C3 / GP 6, Status(Focus) 2 | C3 / GP 8, Status(Focus) 3 |
| 104 S04 | S | A | N | 1回 | C2 / UC GCT>=2 / GP 10 A3=200 | C2 / UC GCT>=2 / GP 15 A3=200 |
| 105 S05 | S | A | N | 1回 | C2 / UC Foc>=3 / GP 5 A1=2 | C2 / UC Foc>=3 / GP 9 A1=2 |
| 106 S06 | S | M | N | 1回 | C0 / Shield 5, Status(Excellent) 1 | C0 / Shield 6, Status(Excellent) 2 |
| 107 S07 | S | A | R | | C4 / GP 6, Status(GoodCondition) 2 | C4 / GP 9, Status(GoodCondition) 3 |
| 108 S08 | S | A | R | | C4 / GP 6, Status(Focus) 2 | C4 / GP 9, Status(Focus) 3 |
| 109 S09 | S | A | R | | C4 / GP 6, Status(Focus) 3 {GCT>=1} | C4 / GP 9, Status(Focus) 4 {GCT>=1} |
| 110 S10 | S | A | R | 1回 | C4 / GP 17 A2=150 | C4 / GP 23 A2=200 |
| 111 S11 | S | A | R | 1回 | C6 / UC GCT>=1 / GP 27 | C6 / UC GCT>=1 / GP 38 |
| 112 S12 | S | M | SR | 1回 手 | C4 / Status(Focus) 3, Status(GoodCondition) 2 | C4 / Status(Focus) 4, Status(GoodCondition) 3 |
| 113 S13 | S | M | SR | 1回 重 | C4 / Status(Focus) 2, Persist(OnActiveCardUse)→子 | C3 / Status(Focus) 3, Persist(OnActiveCardUse)→子 |
| 114 S14 | S | A | SSR | 1回 重 | C6 / GP 15, GP 15 {Foc>=3} | C6 / GP 15, GP 34 A2=150 {Foc>=3} |
| 201 L01 | L | M | N | | C2 / Shield 1, Status(Impression) 2 | C2 / Shield 2, Status(Impression) 3 |
| 202 L02 | L | M | N | 1回 | C2 / UC Imp>=1 / Status(Impression) 4 | C2 / UC Imp>=1 / Status(Impression) 5 |
| 203 L03 | L | M | N | | C2 / Shield 1, Status(Motivation) 2 | C2 / Shield 2, Status(Motivation) 3 |
| 204 L04 | L | A | N | 1回 | C5 / Status(Impression) 2, GPI 100 | C5 / Status(Impression) 3, GPI 120 |
| 205 L05 | L | A | N | 1回 | H5 / GPS 100 | H3 / GPS 110 |
| 206 L06 | L | A | N | 1回 | C2 / Shield 3, GPM 120 | C2 / Shield 6, GPM 150 |
| 207 L07 | L | A | R | | C4 / GP 7, Status(Impression) 3 | C4 / GP 9, Status(Impression) 4 |
| 208 L08 | L | A | R | 1回 | H4 / Status(Motivation) 3, GPS 60 | H4 / Status(Motivation) 4, GPS 80 |
| 209 L09 | L | M | R | | C0 / Shield 6 | C0 / Shield 8 |
| 210 L10 | L | A | R | 1回 | C5 / GPI 150 | C5 / GPI 200 |
| 211 L11 | L | A | R | 1回 | H4 / GPS 110 | H3 / GPS 120 |
| 212 L12 | L | M | SR | 1回 手 | C5 / Status(Impression) 4, Status(Motivation) 3 | C5 / Status(Impression) 5, Status(Motivation) 4 |
| 213 L13 | L | A | SR | 1回 | H3 / GPS 130, Scale 50 | H3 / GPS 180, Scale 50 |
| 214 L14 | L | M | SR | 1回 重 | C4 / Status(Impression) 3, Persist(OnMentalCardUse)→子 | C4 / Status(Impression) 5, Persist(OnMentalCardUse)→子 |
| 215 L15 | L | A | SSR | 1回 重 | C6 / Status(Impression) 5, GPI 100 | C6 / Status(Impression) 6, GPI 170 |

補足:

- `{…}` の効果内条件と `UC` の使用条件は、`condition` に次の行を作る（`Order` = 1、`Value` は表の数）。使用条件: `UseConditionGroupId` = 連番 1 の条件グループ（例: S04 通常 = 10411）。効果内条件: 連番 2 の条件グループ（例: S09 通常 = 10912、F05 通常 = 512）。`CardFace.UseConditionGroupId` は使用条件がないとき空欄
- `Persist(…)→子`: 子のグループ（`EffectGroupId` = `… + 2`、例: S13 通常 = 11312、強化 = 11322、L14 通常 = 21412、強化 = 21422）に 1 行置く。S13 の子は `Status(Focus) 1`、L14 の子は `Status(Impression) 1`（通常・強化とも）。`AddPersistent` の `Value`・`Arg1`・`Arg2` は空欄（無制限）
- 行数: `card` 36、`cardFace` 72、`cardCost` 72、`effect` 129（共通 27 + 感性 50 + 理論 52。子の行を含む）、`condition` 14
- **`{…}` の効果内条件は、その直前の 1 効果の行にだけ付ける**（`RefConditionGroupId`）。同じ面のほかの効果の行には付けない
- 行数: `card` 36、`cardFace` 72、`cardCost` 72、`effect` 129（共通 27 + 感性 50 + 理論 52。子の行を含む）、`condition` 14
- **検算**（`effect` を作り終えたら、テストの前に自分でも確かめる）: `Op` ごとの行数と `Value` の合計は次のとおり。`CardCost` は `Consume` の `Value` の合計 176、`HpDirect` の合計 30

| `Op` | 行数 | `Value` の合計 |
|---|---|---|
| `GainParam` | 26 | 329 |
| `GainParamByImpression` | 6 | 840 |
| `GainParamByShield` | 8 | 890 |
| `GainParamByMotivation` | 2 | 270 |
| `AddShield` | 24 | 98 |
| `AddStatus` | 53（子の行 4 を含む） | 148（子の行の 1 × 4 を含む） |
| `AddCardUses` | 2 | 2 |
| `DrawCards` | 2 | 4 |
| `ScaleShield` | 2 | 100 |
| `AddPersistent` | 4 | 0 |

## 4. タスク

各タスクが終わったとき、`dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project server/Uta.Server.Tests` が通る状態にする。実装担当は、**生成されたファイル（`server/Uta.Server/Infrastructure/MasterData/Generated/`・`masterdata.bytes`・`masterdata.manifest.json`）を手で直さない**。定義か CSV を直して `gen.ps1` を実行し直す。この計画にない設計判断に出会ったら止まって報告する。

生成ソースを `Nullable` 有効・`EnforceCodeStyleInBuild` の `Uta.Server` でビルドして警告 0 になることは、2026-11-01 にアドオンのサンプルで確認した（生成ソースは `<auto-generated>` ヘッダ付き）。警告が出たら止まって報告する。

### タスク 1: サーバーの下準備

1. `Directory.Packages.props` に `<PackageVersion Include="MasterMemory" Version="3.0.4" />` を足す（アドオンのツールと同じ版。MessagePack は `3.1.12` で、すでにそろっている）
2. `Uta.Server.csproj` に `<PackageReference Include="MasterMemory" />`・`<PackageReference Include="MessagePack" />`、`<ProjectReference Include="..\..\external\MasterMemoryAddons\src\MasterMemory.Addons.Core\MasterMemory.Addons.Core.csproj" />` を足す
3. `server/Uta.Server/Infrastructure/MasterData/MasterDataAssembly.cs`: `[assembly: MasterMemory.MasterMemoryGeneratorOptions(Namespace = "Uta.MasterData")]`（`namespace` なしの 1 ファイル。ファイルスコープ namespace は使わない）
4. `ArchitectureTests.cs` に 1 件足す（既存と同じ形。repository-layout.md の「禁止は層ごとに 1 ルール」）: `MasterData_IsReferencedOnlyByInfrastructure`。`Types.InAssembly(typeof(PlayerResolver).Assembly).That().ResideInNamespaceStartingWith("Uta.Server.Api").Or().ResideInNamespaceStartingWith("Uta.Server.Application").Or().ResideInNamespaceStartingWith("Uta.Server.Domain").ShouldNot().HaveDependencyOnAny("Uta.MasterData").GetResult()` が成功する（この時点では `Uta.MasterData` の型がないので自明に通る。型ができるタスク 2 以降も通ることが、規則の中身）
5. 確かめる: `dotnet build Uta.slnx`（`MasterMemory` を参照してテーブルが 0 件の状態でも警告 0 になることは、2026-11-01 にスクラッチで確認した）、`dotnet test --project server/Uta.Server.Tests`

### タスク 2: masterdata の骨組みとレッスンの定義

1. **`masterdata/masterdata.yaml`** を次のとおり作る:

   ```yaml
   definitions: Definitions
   csv: csv

   outputs:
     - name: server
       target: server
       source: ../server/Uta.Server/Infrastructure/MasterData/Generated
       namespace: Uta.MasterData
       binary: ../server/Uta.Server/Infrastructure/MasterData/masterdata.bytes
   ```

2. **`masterdata/gen.ps1`**（Windows PowerShell 5.1 と pwsh の両方で動く書き方）:

   ```powershell
   # マスタの生成。使い方: powershell -NoProfile -File masterdata/gen.ps1 [code-gen | master-gen | check]
   #   code-gen   定義から生成ソースを作る
   #   master-gen CSV を検証してバイナリを作る（生成ソースが古ければコード生成もやり直す）。省略時
   #   check      検証だけを行い、何も書き込まない
   param(
       [Parameter(Position = 0)]
       [ValidateSet('code-gen', 'master-gen', 'check')]
       [string]$Command = 'master-gen'
   )

   $ErrorActionPreference = 'Stop'
   $repoRoot = Split-Path -Parent $PSScriptRoot
   $tools = Join-Path $repoRoot 'external/MasterMemoryAddons/src/MasterMemory.Addons.Tools'
   $config = Join-Path $PSScriptRoot 'masterdata.yaml'

   if ($Command -eq 'check')
   {
       $toolArgs = @('master-gen', '--check')
   }
   else
   {
       $toolArgs = @($Command)
   }

   # Uta のルートの Directory.Packages.props（パッケージ版の一括管理）がツールにも効くので、この呼び出しだけ切る
   dotnet run --project $tools -p:ManagePackageVersionsCentrally=false -- @toolArgs --config $config
   exit $LASTEXITCODE
   ```

3. **`masterdata/.editorconfig`** は `shared/.editorconfig` の中身をコピーし、1 行目のコメントを「masterdata の定義用。Unity にソースで入るため C# 9 の範囲に収める」にする
4. **`masterdata/Definitions/Common.cs`**・**`Lesson.cs`** を、3.2・3.3 のとおり書く（`using System; using System.Collections.Generic; using MasterMemory; using MasterMemory.Addons; using MessagePack;`（アドオンは ImplicitUsings を使わない）、ブロック形式の `namespace Uta.MasterData`、`[MemoryTable("…"), MessagePackObject(true)]`、`{ get; init; }`、メッセージは日本語）。`IValidatable<T>` は各クラスで明示的実装にする。`Effect` は 3.3 の例（`OnlyUses`・`CallOnce()`）の形で、3.3.1 の表の全 `Op` を並べる。**C# 9 の範囲に収める**（2章「定義の C# のバージョン」の使わない書き方を使わない）
5. `powershell -NoProfile -File masterdata/gen.ps1 code-gen` を実行する。終了コード 0 で、`server/Uta.Server/Infrastructure/MasterData/Generated/` に `Common.cs`・`Lesson.cs`（定義のファイル名と同じ。enum と表の型）と `MasterDataSchema.cs` の 3 つができる（コミット対象。`MemoryDatabase` はここには出ず、ビルドのときに MasterMemory のソースジェネレーターが作る）。エラーが出たら定義を直す
6. 確かめる: `dotnet build Uta.slnx`（生成ソースが `Uta.Server` に入ってビルドされる。警告 0）、`dotnet test --project server/Uta.Server.Tests`

### タスク 3: レッスンの CSV と生成

1. **CSV 5 つ**を 3.4・3.5 のとおり作る。作り終えたら、3.5 の検算の表と照らす
2. `powershell -NoProfile -File masterdata/gen.ps1 master-gen` を実行する。エラー（検証の失敗）が出たら CSV か定義を直す。成功すると `masterdata.bytes`・`masterdata.manifest.json` ができる（コミット対象）。続けて `powershell -NoProfile -File masterdata/gen.ps1 check` が終了コード 0
3. **規則が効くことを手で確かめる**（テストは足さない）。先に `server/Uta.Server/Infrastructure/MasterData/masterdata.bytes` のハッシュ（`Get-FileHash`）を控える。(a) `effect.csv` の `AddShield` の行の `Arg1` に `1` を入れて `gen.ps1 master-gen` を実行する、(b) `cardCost.csv` の 1 行の `Kind` を空にして実行する、(c) `effect.csv` の `AddPersistent` の行の `Trigger` を `EveryNTurns` にして実行する。**どれも終了コード 1 で、出力に `Validate failed` と該当の主キーが出る**（(a) は `AddShield` に使わない列がある、という意味のメッセージ）。確認のたびに、`masterdata.bytes` のハッシュが控えた値と同じであること（エラーのときはバイナリを書かない）を確かめ、CSV を元の値に戻す。全部戻したら `gen.ps1 check` が終了コード 0 になること。（この時点ではファイルを `git add` もコミットもしていなくてよい。確認はハッシュと `check` で行う）
4. 確かめる: `dotnet build Uta.slnx`、`dotnet test --project server/Uta.Server.Tests`

### タスク 4: 埋め込みと読み込み・テスト

1. `Uta.Server.csproj` に `<EmbeddedResource Include="Infrastructure\MasterData\masterdata.bytes" LogicalName="Uta.Server.MasterData.masterdata.bytes" />` を足す
2. **`Infrastructure/MasterDataLoader.cs`**:

   ```csharp
   using MasterMemory.Addons;
   using Uta.MasterData;

   namespace Uta.Server.Infrastructure;

   /// <summary>埋め込みのマスタのバイナリ（masterdata.bytes）を読む。</summary>
   public static class MasterDataLoader
   {
       private const string ResourceName = "Uta.Server.MasterData.masterdata.bytes";

       public static MemoryDatabase Load()
       {
           using var stream = typeof(MasterDataLoader).Assembly.GetManifestResourceStream(ResourceName)
               ?? throw new InvalidOperationException($"埋め込みリソース {ResourceName} がない");
           using var buffer = new MemoryStream();
           stream.CopyTo(buffer);
           return Load(buffer.ToArray());
       }

       public static MemoryDatabase Load(byte[] bytes)
       {
           try
           {
               return new MemoryDatabase(MasterDataBinary.Read(bytes, MasterDataSchema.Hash));
           }
           catch (MasterDataFormatException exception)
           {
               // スキーマのハッシュ（暗号化の鍵）はメッセージに入れない（masterdata-pipeline.md 6章）
               throw new InvalidOperationException($"マスタのバイナリを読めない（{exception.Reason}）", exception);
           }
       }
   }
   ```

3. **`Program.cs`**: `migrate` の分岐の後（`builder.Logging.ClearProviders();` の前）に `builder.Services.AddSingleton(MasterDataLoader.Load());` を 1 行足す
4. **テスト** `server/Uta.Server.Tests/MasterData/MasterDataTests.cs`（`namespace Uta.Server.Tests.MasterData;`。Docker 不要。`MemoryDatabase` は `private static readonly MemoryDatabase Db = MasterDataLoader.Load();` で 1 つ持つ）:
   - `Load_Embedded_ReturnsDatabase`
   - `Load_NotMasterData_ThrowsInvalidOperation`: `MasterDataLoader.Load(Encoding.UTF8.GetBytes("not master data"))` が `InvalidOperationException` を投げ、`InnerException` が `MasterDataFormatException`
   - `Cards_HaveExpectedCounts`: `CardTable.Count` 36、`CardFaceTable` 72、`CardCostTable` 72、`EffectTable` 129、`ConditionTable` 14
   - `Effects_CountAndValueSumByOp`: `EffectTable.All` を `Op` で集計し、3.5 の検算の表の行数と `Value` の合計に一致する。`CardCosts_SumByKind`: `Consume` の合計 176、`HpDirect` の合計 30
   - `Cards_SpotCheck`:
     - F01（`CardId` 1）: 通常面は `CardCost` が `Consume` 4、効果グループ 111 が `GainParam` 9 の 1 行。強化面は `Consume` 3 と `GainParam` 14
     - S04（104）: 通常面の `UseConditionGroupId` = 10411 で、`Condition` 10411 は `GoodConditionTurns` `GreaterOrEqual` 2。効果グループ 10411 は `GainParam` 10 / `Arg3` 200
     - S13（113）: 通常面の効果グループ 11311 は 2 行（Order 1 = `AddStatus` `Focus` 2、Order 2 = `AddPersistent` `OnActiveCardUse` で `RefEffectGroupId` = 11312）。グループ 11312 は 1 行（`AddStatus` `Focus` 1）
     - S14（114）: 強化面の効果グループ 11421 の Order 2 は `GainParam` 34 / `Arg2` 150 / `RefConditionGroupId` 11422。`Condition` 11422 は `Focus` `GreaterOrEqual` 3
   - `EnumMirrors_MatchDomain`: `Uta.MasterData` の enum と `Uta.Shared.Domain.Lesson` の同名の enum の対（`Plan`・`CardType`・`Rarity`・`ParamKind`・`EffectOp`・`PersistentTrigger`・`StatusKind`・`CostKind`・`ConditionSubject`・`Comparator`）を、`typeof(Uta.MasterData.Plan)` と `typeof(Uta.Shared.Domain.Lesson.Plan)` のように**完全修飾名**で書いて `MemberData` で渡す。名前の一覧（`Enum.GetNames`）と、名前ごとの数値が同じ。リフレクションはテストの中だけで使う。`CardFaceKind` は元の enum がまだないので比べない（元の enum を作る計画が、写しとの比較テストを足す）
5. **テスト** `server/Uta.Server.Tests/MasterData/MasterDataInvariantTests.cs`: 3.3 の「表をまたぐ検証」の 2 項目を、`MasterDataLoader.Load()` の `MemoryDatabase` の `All` から確かめる（`Cards_EveryCardHasNormalAndEnhancedFace`・`CardFaces_EveryFaceHasCostAndCostsHaveFace`）
6. 確かめる: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（既存の結合テストも通る = 起動時の読み込みが動く。Docker が要る）

### タスク 5: wiki の反映（メインのセッション。確認のあと）

- [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md): 1章の全体フロー（クライアント用の出力は今はない）、2章に「生成ソースの名前空間は定義のまま」「`MasterMemoryGeneratorOptions`」「検証は行が `IValidatable`、表をまたぐ検査はテスト（入れ子の深さだけ `IValidatable`）」、3章に「MVP のカード 36 枚の ID は規則で振った（以後は従わなくてよい）」、5章の `masterdata.yaml` を実物に直す（出力先のパスを `Infrastructure/MasterData/` にし、`client` の出力は Unity に取り込む計画で足す）、7章に `gen.ps1` と `-p:ManagePackageVersionsCentrally=false`・MessagePack の版の整合（3.1.12 で一致）、マニフェストのファイル名を `masterdata.manifest.json` にそろえる、実装メモ
- [lesson-enums.md](../wiki/architecture/lesson-enums.md): 未決事項の「1章の表の『—』の意味」を決めたことに直し、1章の表の後ろに「マスタの検証の規則」として 3.3.1 の表を移す（`AddPersistent` は `EveryNTurns` を弾く = `CardDefinitionValidator` と同じ）。`CardDefinitionValidator` は今、負の値・`AddStatus`/`DrawCards` の下限・`Status`/`Trigger` の使わない列・持続効果の子・入れ子の深さを検証しているので、`Value`・`Arg`・参照の列の使わない列と、ほかの `Op` の下限が、マスタとの差になる。差は実装メモに記録し、後の計画でそろえる
- [lesson-tables.md](../wiki/architecture/master/lesson-tables.md): 検証の節を、3.3 の分け方（行の検証は `IValidatable`、表をまたぐ検証はサーバーのテスト、入れ子の深さだけ `IValidatable` の `CallOnce()`）に書き直し、実装メモを書く（列と検証の正は正のページ。master/README.md）
- [lesson-card.md](../wiki/systems/lesson-card.md) 14章・[produce.md](../wiki/systems/produce.md) 7章・[game-spec.md](../wiki/overview/game-spec.md) の未決事項に、`unlockLv` は仮の値（N・R = 1、SR = 5、SSR = 10）で、参考作品の値が分かったら差し替えると注記する
- [repository-layout.md](../wiki/architecture/repository-layout.md): 2章のツリーの `Uta.Server/MasterData/` を `Infrastructure/MasterData/` に直し、`masterdata/`（未作成）の表記を直す。依存の禁止の表（165〜170 行目あたり）に「`Uta.MasterData` を参照してよいのは Infrastructure だけ。Api・Application・Domain は禁止」を足す
- `CLAUDE.md`: リポジトリ構成の `masterdata/`（未作成 → 作成済み）、ビルド・テスト・実行に `gen.ps1` の 3 つのコマンド
- [game-spec.md](../wiki/overview/game-spec.md): 実装計画の単位と順の 2 を 2a（この計画）・2b（[2/2](2026-11-01-masterdata-rest.md)）に分けてリンク
- `python -I .claude/skills/linting-wiki/lint_wiki.py` で点検。この計画を `status: done` にする

## 5. テスト

4章の各タスクのテストを足す。実行するコマンド（CLAUDE.md にあるもの）:

- マスタの生成: `powershell -NoProfile -File masterdata/gen.ps1 master-gen`、続けて `check`
- ビルド: `dotnet build Uta.slnx`（警告 0・エラー 0）
- テスト: `dotnet test --project server/Uta.Server.Tests`（既存の結合テストは Docker が要る）

Unity・実機でしか確かめられないもの（この計画では確かめない）: クライアント用の出力、Unity での読み込み。

## 6. 未決事項

なし（7章の確認は 2026-11-01 に済んだ）。

## 7. 確認してほしいこと（2026-11-01 にユーザーが確認済み）

- **`Effect` の列の規則**（3.3.1）: 「使わない列は空欄でなければエラー」とした。`AddPersistent` の `EveryNTurns` はマスタでも弾く（`CardDefinitionValidator` と同じ）。lesson-enums.md の未決事項を、この計画で決めたことになる。`CardDefinitionValidator` との差はタスク 5 で wiki に記録する
- **ID の振り方**: カード ID と効果・条件のグループ ID の規則は、この 36 枚だけのもの（以後は従わなくてよい）としている
- **`unlockLv` の仮の値**（N・R = 1、SR = 5、SSR = 10）。参考作品の値が分かったら差し替える
- **クライアント用の出力を作らない**こと、定義を C# 9 に収める仕組みがないこと（2章。ユーザーの確認済み: 2026-11-01）
- **サーバー側の置き場所**は `Infrastructure/MasterData/`（repository-layout.md 3章のユーザーの判断に従った）
