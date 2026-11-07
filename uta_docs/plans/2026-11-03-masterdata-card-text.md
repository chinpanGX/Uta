---
title: 実装計画: マスタ スキルカードの名前と説明文
status: ready
updated: 2026-11-08
spec: ../wiki/architecture/master/lesson-tables.md
---

# 実装計画: マスタ スキルカードの名前と説明文

順 7（client/ の画面）の計画 0。画面の計画（[foundation](2026-11-03-client-screens-foundation.md) の `IGameMaster` がカードの名前と説明文を読む）より先に実装する。

## 1. 目的と前提

スキルカードのマスタに、表示用の **名前**（`Card.Name`）と、面ごとの **説明文**（`CardFace.Description`）を足す。MVP の 36 枚（[lesson-card.md](../wiki/systems/lesson-card.md) 14章。F08 を除く）の値は、この計画の 3.4 の表がすべて決める。画面は名前・レア度・種類・消費・強化の印・説明文を出す（[screen-design.md](../wiki/architecture/screen-design.md) 7章「スキルカードの表示」[決定。2026-11-03。ユーザーの判断]）。

- 仕様の正: テーブルと列は [lesson-tables.md](../wiki/architecture/master/lesson-tables.md)（この計画で同じ変更で更新する）、効果の値は [lesson-card.md](../wiki/systems/lesson-card.md) 14章と `masterdata/csv/effect.csv`・`cardCost.csv`・`condition.csv`
- 定義の書き方・生成・検証の置き場は [masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 2章（1 型 1 ファイル、行の検査は `IValidatable<T>`、表をまたぐ検査はサーバーのテスト）
- 関係する ADR: [0001](../wiki/decisions/0001-ip-policy.md)（名前はすべてオリジナル。参考作品のカード名・固有の表現を使わない）、[0016](../wiki/decisions/0016-reference-game-values.md)（効果の数値は参考作品の値を使ってよい。説明文の数値はその値をそのまま書く）、[0007](../wiki/decisions/0007-masterdata-with-addons.md)（列の型は `int` / `long` / `string` / `bool` / enum）、[0013](../wiki/decisions/0013-commit-masterdata-binary.md)（バイナリはコミットする）

### 1.1 この計画で決めないこと

- 効果の説明文を `Effect` の行から自動で作る仕組み（今は人が書いた文を CSV に置く）
- 多言語化（言語ごとの列・テーブル、ローカライズのパッケージ）
- 表示名を `[ClientOnly]` の列に分けるか（[masterdata-pipeline.md](../wiki/architecture/master/masterdata-pipeline.md) 未決事項。今は `Idol.Name`・`SupportCard.Name` と同じく Common に置く）
- ステータスの表示名（元気・好調・集中・好印象・やる気・絶好調など）の Uta 独自の名前への置き換え（[lesson-card.md](../wiki/systems/lesson-card.md) 未決事項）。説明文は今の用語集（[glossary.md](../wiki/glossary.md)「レッスン・試験（仮称）」）の語で書く。置き換えるときは `cardFace.csv` の説明文も同じ変更で書き直す
- 名前の文字数の上限の検査（3.4 の名前は 8 文字以内で書いたが、検査は足さない。画面の表示幅は画面の計画が決める）
- **`Description` が `effect.csv`・`cardCost.csv`・`condition.csv` の数値と食い違わないことは、自動では検証しない**。人が書いた文なので、CSV を変えたときはレビューで突き合わせる（3.4 の表を書くときに 72 面すべてを突き合わせた。結果は 3.5）

### 1.2 この計画で決めたこと

| 項目 | 決めたこと | 理由 |
|---|---|---|
| 列の名前と型 | `Card.Name`（`string`）、`CardFace.Description`（`string`） | ユーザーの判断（2026-11-03）。名前はカードに 1 つ、説明文は面（通常 / 強化）ごとに変わる |
| 区分 | どちらも Common（属性を付けない） | `Idol.Name`・`SupportCard.Name` と同じ。サーバーは使わないが、出し分けは未決事項（1.1） |
| 列の位置 | `card.csv` は 2 列目（`CardId` の次）。`cardFace.csv` は 5 列目（最後。`UseConditionGroupId` の次） | `idol.csv`・`supportCard.csv` の `Name` が 2 列目。説明文は長いので行の最後に置き、既存の列の位置を変えない |
| 空の拒否 | `Card` の `IValidatable` に `Name`、`CardFace` の `IValidatable` に `Description` の検査を足す（`string.IsNullOrWhiteSpace` が真なら違反） | 行の列だけで決まる検査なので、検証の置き場の規則（masterdata-pipeline.md 2章）どおり行のクラス |
| 名前の重複 | `Card.Name` に `[Unique]`（`MasterMemory.Addons`）を付ける | 重複なしの規則を `master-gen` が検証できる（属性は生成ソースから取り除かれる。`string` の列に付けられる） |
| 説明文の書き方 | 3.3 の規則 | 実装担当に文を作らせない。画面ごとの表記ゆれを防ぐ |
| 名前の付け方 | 3.4 の表（Uta案。日本語の短いオリジナルの名前） | IP 方針。参考作品のカード名・固有の表現を使わない |

## 2. 触るファイル

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 変更 | `masterdata/Definitions/Lesson/Card.cs` | `Name` 列・`[Unique]`・検証を足す（3.1） | 1 |
| 変更 | `masterdata/Definitions/Lesson/CardFace.cs` | `Description` 列・検証を足す（3.1） | 1 |
| 変更 | `masterdata/csv/card.csv` | 2 列目に `Name` を足す（3.2・3.4） | 1 |
| 変更 | `masterdata/csv/cardFace.csv` | 5 列目に `Description` を足す（3.2・3.4） | 1 |
| 変更（再生成） | `server/Uta.Server/Infrastructure/MasterData/Generated/Lesson/Card.cs`・`CardFace.cs`・`Generated/MasterDataSchema.cs`・`masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1` が作り直す（手で直さない） | 1 |
| 変更（再生成） | `client/uta/Assets/Scripts/Infrastructure/MasterData/Generated/Lesson/Card.cs`・`CardFace.cs`・`Generated/MasterDataSchema.cs`、`client/uta/Assets/StreamingAssets/MasterData/masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1` が作り直す（手で直さない）。ファイルは増えないので `.meta` は増えない | 1 |
| 変更 | `server/Uta.Server.Tests/MasterData/MasterDataTests.cs` | テストを 4 つ足す（5章） | 1 |
| 変更 | `client/uta/Assets/Tests/EditMode/MasterDataLoaderTests.cs` | テストを 1 つ足す（5章） | 2 |
| 変更 | `uta_docs/wiki/architecture/master/lesson-tables.md` | 1章・2章・実装メモ（タスク 3） | 3 |
| 変更 | `uta_docs/wiki/architecture/master/README.md` | 1章の `Card`・`CardFace` の行、5章の `[ClientOnly]` の文（タスク 3） | 3 |
| 変更 | `uta_docs/wiki/architecture/master/masterdata-pipeline.md` | 未決事項の 1 行（タスク 3） | 3 |
| 変更 | `uta_docs/wiki/systems/lesson-card.md` | 14章の冒頭に 1 行、未決事項の 1 行（タスク 3） | 3 |

変えないもの: 件数のテスト（`MasterDataTests.Cards_HaveExpectedCounts`、`MasterDataLoaderTests.Load_Bundled_ReturnsDatabase`）。行は増えないので、件数（`card` 36、`cardFace` 72）はそのまま。`glossary.md` は変えない（新しい用語を足さない。「説明文」「名前」は一般の語）。`MasterCardCatalog`（サーバー・クライアント）は変えない（`Uta.Shared.Domain` の `CardDefinition` に名前を持たせない。画面は `IGameMaster` から読む。foundation の計画）。`external/` は触らない。

## 3. 型とシグネチャ

### 3.1 定義（`masterdata/Definitions/Lesson/`。C# 9 の範囲）

**`Card.cs`**（変更後の全文）。`Name` は `CardId` の次。`using MasterMemory.Addons;` を足す（`[Unique]` のため）。

```csharp
using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("card"), MessagePackObject(true)]
    public class Card : IValidatable<Card>
    {
        [PrimaryKey]
        public int CardId { get; init; }

        [Unique]
        public string Name { get; init; } = "";

        public Plan Plan { get; init; }

        public CardType Type { get; init; }

        public Rarity Rarity { get; init; }

        public int UnlockLv { get; init; }

        public bool OnceInLesson { get; init; }

        public bool Unique { get; init; }

        public bool StartInHand { get; init; }

        void IValidatable<Card>.Validate(IValidator<Card> validator)
        {
            validator.Validate(x => !string.IsNullOrWhiteSpace(x.Name), "card: Name が空");
            validator.Validate(x => x.Plan != Plan.Nothing, "card: Plan が Nothing");
            validator.Validate(x => x.Type != CardType.Nothing, "card: Type が Nothing");
            validator.Validate(x => x.Rarity != Rarity.Nothing, "card: Rarity が Nothing");
            validator.Validate(x => x.UnlockLv >= 1, "card: UnlockLv が 1 未満");
        }
    }
}
```

注意: `Card` にはすでに `bool Unique` というプロパティがある。属性 `[Unique]` は `MasterMemory.Addons.UniqueAttribute` で、プロパティの `Unique` とは別物（属性の位置では `UniqueAttribute` に解決される）。`code-gen` か `dotnet build` で CS0104・CS0616 などの名前の衝突が出たら、`[MasterMemory.Addons.Unique]` と完全修飾で書く。それでも通らなければ止まって報告する（`[Unique]` を外して重複のテスト 5章 `Cards_NamesAreDistinct` だけに頼るかを決め直す）。

**`CardFace.cs`**（変更後の全文）。`Description` は最後の列。

```csharp
using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    [MemoryTable("cardFace"), MessagePackObject(true)]
    public class CardFace : IValidatable<CardFace>
    {
        [PrimaryKey(keyOrder: 0)]
        [Exists(typeof(Card), nameof(Card.CardId))]
        public int CardId { get; init; }

        [PrimaryKey(keyOrder: 1)]
        public CardFaceKind Face { get; init; }

        [Exists(typeof(Effect), nameof(Effect.EffectGroupId))]
        public int EffectGroupId { get; init; }

        [Exists(typeof(Condition), nameof(Condition.ConditionGroupId), AllowEmpty = true)]
        public int UseConditionGroupId { get; init; }

        public string Description { get; init; } = "";

        void IValidatable<CardFace>.Validate(IValidator<CardFace> validator)
        {
            validator.Validate(x => x.Face != CardFaceKind.Nothing, "cardFace: Face が Nothing");
            validator.Validate(x => x.EffectGroupId != 0, "cardFace: EffectGroupId が 0");
            validator.Validate(x => !string.IsNullOrWhiteSpace(x.Description), "cardFace: Description が空");
        }
    }
}
```

**生成後のプロパティ名**（順 7 のほかの計画が使う。foundation の `IGameMaster.GetCardText` の実装 `MasterGameData` が読む）:

| 生成される型（名前空間 `Uta.MasterData`） | プロパティ | 型 | 引き方 |
|---|---|---|---|
| `Card` | `Name` | `string` | `db.CardTable.FindByCardId(cardId).Name` |
| `CardFace` | `Description` | `string` | 主キー `(CardId, Face)` で引く: `db.CardFaceTable.FindByCardIdAndFace((cardId, face)).Description`（MasterMemory の複合キーの命名からの推測。今のコードに使用例がない。コンパイルで名前が違えば `db.CardFaceTable.All.Single(x => x.CardId == cardId && x.Face == face)` を使う）。`face` は `CardFaceKind.Normal`（通常）/ `CardFaceKind.Enhanced`（強化） |

どちらも空でない（`master-gen` の検証で保証される）。`MemoryDatabase` の索引のメソッド名は MasterMemory が生成するもので、この計画では変わらない（列を足すだけで主キーは変えない）。

### 3.2 CSV の形

どちらも UTF-8（BOM なし）・LF・末尾は LF 1 つ（今と同じ）。先頭 4 行（クラス名・出典・型・ヘッダ）は**列数ちょうど**にする。セルの値にカンマ（`,`）とダブルクォート（`"`）を使わない（3.3 の規則で使わないので、クォートは要らない）。

**`card.csv`**（9 列。`Name` を 2 列目に差し込む）:

```
Card,,,,,,,,
出典: uta_docs/wiki/systems/lesson-card.md 14章,,,,,,,,
int,string,Plan,CardType,Rarity,int,bool,bool,bool
CardId,Name,Plan,Type,Rarity,UnlockLv,OnceInLesson,Unique,StartInHand
1,まっすぐ一声,Free,Active,N,1,FALSE,FALSE,FALSE
…
```

データ行は、今の行の `CardId` の後に `,<名前>` を差し込むだけ（3 列目以降は今の値のまま。行の順も変えない）。名前は 3.4 の表。

**`cardFace.csv`**（5 列。`Description` を最後に足す）:

```
CardFace,,,,
出典: uta_docs/wiki/systems/lesson-card.md 14章,,,,
int,CardFaceKind,int,int,string
CardId,Face,EffectGroupId,UseConditionGroupId,Description
1,Normal,111,,パラメータ+9
1,Enhanced,121,,パラメータ+14
…
104,Normal,10411,10411,好調2ターン以上のとき使用可。パラメータ+10（好調の効果2倍）
…
```

データ行は、今の行の末尾に `,<説明文>` を足すだけ（`UseConditionGroupId` が空の行は `,,` の後に説明文が続く。行の順も変えない）。`Normal` の行は 3.4 の「通常」、`Enhanced` の行は「強化」の列。

### 3.3 説明文の書き方の規則（3.4 はこの規則で書いた）

- 効果は 14章の記載順（= `effect.csv` の `Order` 順）に書き、「、」でつなぐ。文末に句点を付けない
- **消費（`cardCost.csv`）は書かない**（画面が `CostKind` と値を別に出す）
- 使用条件（`CardFace.UseConditionGroupId`）があるときは、先頭に「<条件>のとき使用可。」を置く
- 効果内条件（`Effect.RefConditionGroupId`）は「<条件>なら<効果>」。同じ量の効果をもう一度足すものは「さらに」を付ける（S14）
- 書き方: `GainParam` は「パラメータ+N」（連撃は「パラメータ+Nを2回」、`Arg2` は「（集中の効果X倍）」、`Arg3` は「（好調の効果X倍）」。X は % ÷ 100）。`AddShield` は「元気+N」。`AddStatus` のターン制は「<ステータス>Nターン」、値のものは「<ステータス>+N」（消費体力削減だけは 14章の表記どおり「消費体力削減N」）。`GainParamByX` は「<元>のN%分パラメータ上昇」。`ScaleShield` 50 は「その後元気を半分にする」。`AddCardUses` は「スキルカード使用数+N」。`DrawCards` は「カードをN枚引く」。`AddPersistent` は「以降<種別>カード使用時に<子の効果>」
- 文字: 数字・`+`・`%`・`.` は半角、括弧は全角（`（）`）、区切りは「、」「。」

### 3.4 36 枚の名前と説明文

ID の対応は 14章の ID → `CardId`（F01〜F07 = 1〜7、S01〜S14 = 101〜114、L01〜L15 = 201〜215）。「通常」は `cardFace.csv` の `Normal` の行、「強化」は `Enhanced` の行の `Description`。

| CardId | 14章 | Name | 通常（Normal） | 強化（Enhanced） |
|---|---|---|---|---|
| 1 | F01 | まっすぐ一声 | パラメータ+9 | パラメータ+14 |
| 2 | F02 | はずむ足取り | パラメータ+2、元気+2 | パラメータ+6、元気+4 |
| 3 | F03 | 深呼吸ひとつ | 元気+4 | 元気+7 |
| 4 | F04 | 肩の力を抜いて | 元気+2、消費体力減少2ターン | 元気+2、消費体力減少3ターン |
| 5 | F05 | 余裕のペース | 元気+3、体力が50%以上なら消費体力削減1 | 元気+6、体力が50%以上なら消費体力削減1 |
| 6 | F06 | ひらめきの連鎖 | スキルカード使用数+1、カードを2枚引く | スキルカード使用数+1、カードを2枚引く、消費体力減少1ターン |
| 7 | F07 | 嵐の前の休符 | 元気+11、元気増加無効2ターン、消費体力減少3ターン | 元気+13、元気増加無効2ターン、消費体力減少4ターン |
| 101 | S01 | 晴れ間の鼻歌 | 元気+1、好調2ターン | 元気+1、好調3ターン |
| 102 | S02 | 耳をすまして | 元気+1、集中+2 | 元気+1、集中+3 |
| 103 | S03 | 一音入魂 | パラメータ+6、集中+2 | パラメータ+8、集中+3 |
| 104 | S04 | 波に乗るサビ | 好調2ターン以上のとき使用可。パラメータ+10（好調の効果2倍） | 好調2ターン以上のとき使用可。パラメータ+15（好調の効果2倍） |
| 105 | S05 | 刻むダブルビート | 集中3以上のとき使用可。パラメータ+5を2回 | 集中3以上のとき使用可。パラメータ+9を2回 |
| 106 | S06 | きらめく予感 | 元気+5、絶好調1ターン | 元気+6、絶好調2ターン |
| 107 | S07 | 弾む旋律 | パラメータ+6、好調2ターン | パラメータ+9、好調3ターン |
| 108 | S08 | 研ぎ澄ます声 | パラメータ+6、集中+2 | パラメータ+9、集中+3 |
| 109 | S09 | ノリのまま集中 | パラメータ+6、好調なら集中+3 | パラメータ+9、好調なら集中+4 |
| 110 | S10 | 一点突破 | パラメータ+17（集中の効果1.5倍） | パラメータ+23（集中の効果2倍） |
| 111 | S11 | 最高潮の一節 | 好調のとき使用可。パラメータ+27 | 好調のとき使用可。パラメータ+38 |
| 112 | S12 | 開演の合図 | 集中+3、好調2ターン | 集中+4、好調3ターン |
| 113 | S13 | 澄んだ水面 | 集中+2、以降アクティブカード使用時に集中+1 | 集中+3、以降アクティブカード使用時に集中+1 |
| 114 | S14 | 光るソロパート | パラメータ+15、集中3以上ならさらにパラメータ+15 | パラメータ+15、集中3以上ならさらにパラメータ+34（集中の効果1.5倍） |
| 201 | L01 | 笑顔の練習 | 元気+1、好印象+2 | 元気+2、好印象+3 |
| 202 | L02 | 心をつなぐ | 好印象1以上のとき使用可。好印象+4 | 好印象1以上のとき使用可。好印象+5 |
| 203 | L03 | 小さな決意 | 元気+1、やる気+2 | 元気+2、やる気+3 |
| 204 | L04 | 届けたい気持ち | 好印象+2、好印象の100%分パラメータ上昇 | 好印象+3、好印象の120%分パラメータ上昇 |
| 205 | L05 | 全力の一歩 | 元気の100%分パラメータ上昇 | 元気の110%分パラメータ上昇 |
| 206 | L06 | 燃える意気込み | 元気+3、やる気の120%分パラメータ上昇 | 元気+6、やる気の150%分パラメータ上昇 |
| 207 | L07 | 目を合わせて | パラメータ+7、好印象+3 | パラメータ+9、好印象+4 |
| 208 | L08 | 汗と根性 | やる気+3、元気の60%分パラメータ上昇 | やる気+4、元気の80%分パラメータ上昇 |
| 209 | L09 | ひと息つこう | 元気+6 | 元気+8 |
| 210 | L10 | 客席への手紙 | 好印象の150%分パラメータ上昇 | 好印象の200%分パラメータ上昇 |
| 211 | L11 | 勢いまかせ | 元気の110%分パラメータ上昇 | 元気の120%分パラメータ上昇 |
| 212 | L12 | 心の準備運動 | 好印象+4、やる気+3 | 好印象+5、やる気+4 |
| 213 | L13 | 出し切る覚悟 | 元気の130%分パラメータ上昇、その後元気を半分にする | 元気の180%分パラメータ上昇、その後元気を半分にする |
| 214 | L14 | ひだまりの輪 | 好印象+3、以降メンタルカード使用時に好印象+1 | 好印象+5、以降メンタルカード使用時に好印象+1 |
| 215 | L15 | 響きあう想い | 好印象+5、好印象の100%分パラメータ上昇 | 好印象+6、好印象の170%分パラメータ上昇 |

名前は 36 個とも互いに異なり、8 文字以内（最長は「刻むダブルビート」の 8 文字）。

### 3.5 14章・CSV との突き合わせ（2026-11-03）

72 面すべてについて、`cardFace.csv` の `EffectGroupId`・`UseConditionGroupId` → `effect.csv`（`Op`・`Status`・`Value`・`Arg1`〜`Arg3`・`RefConditionGroupId`・持続効果の子）・`condition.csv`、と 14章の「通常」「強化」の記載を 1 枚ずつ比べた。

- **効果の数値の食い違いはない**（36 枚・72 面とも 14章の記載と `effect.csv`・`condition.csv` が一致する）。消費（`cardCost.csv`）も 14章の記載と一致する（説明文には書かない）
- 表記の読み替え（食い違いではない）: 14章の「好調2倍」「集中1.5倍」は `GainParam` の `Arg3`・`Arg2`（好調・集中の倍率%。[lesson-enums.md](../wiki/architecture/lesson-enums.md)）なので、説明文では「（好調の効果2倍）」「（集中の効果1.5倍）」と書いた。L13 の「元気を半分に」は `ScaleShield` 50（端数切り捨て）
- `CardFaceKind` のメンバーは `Normal` / `Enhanced`（「Base」ではない）

## 4. タスク

各タスクが終わったとき、`dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project server/Uta.Server.Tests` が通る状態にする（結合テストは Docker が要る）。**生成されたファイル（`Generated/`・`masterdata.bytes`・`masterdata.manifest.json`）を手で直さない**。定義か CSV を直して `gen.ps1` を実行し直す。この計画にない設計判断に出会ったら止まって報告する。.NET SDK・Unity が無い環境では、結果を推測で「通った」と書かない。

### タスク 1: 定義・CSV・生成・サーバーのテスト

- 触るファイル: 2章のタスク 1 の行
- 手順:
  1. `Card.cs`・`CardFace.cs` を 3.1 の全文に書き換える（BOM 付き UTF-8・LF。今のファイルと同じ）
  2. `card.csv` を 3.2 の形に直す: 1・2 行目の末尾にカンマを 1 つ足す（8 個に）、3 行目を `int,string,Plan,CardType,Rarity,int,bool,bool,bool`、4 行目を `CardId,Name,Plan,Type,Rarity,UnlockLv,OnceInLesson,Unique,StartInHand` にし、36 行のデータ行の `CardId` の後に `,<3.4 の Name>` を差し込む
  3. `cardFace.csv` を 3.2 の形に直す: 1・2 行目の末尾にカンマを 1 つ足す（4 個に）、3 行目を `int,CardFaceKind,int,int,string`、4 行目を `CardId,Face,EffectGroupId,UseConditionGroupId,Description` にし、72 行のデータ行の末尾に `,<3.4 の説明文>` を足す
  4. CSV は BOM なし・LF で保存する。PowerShell で書くときは `[System.IO.File]::WriteAllText(<絶対パス>, <中身>, (New-Object System.Text.UTF8Encoding($false)))` を使う（`Out-File`・`Set-Content`・`>` は BOM や改行が変わるので使わない）。Edit ツールで直してもよい
  5. リポジトリのルートで `powershell -NoProfile -File masterdata/gen.ps1 code-gen` → `powershell -NoProfile -File masterdata/gen.ps1 master-gen` → `powershell -NoProfile -File masterdata/gen.ps1 check` を実行する（どれも終了コード 0。実行ポリシーで読めなければ `-ExecutionPolicy Bypass` を足す）。`masterdata.yaml` の 2 つの出力先（`server`・`unity`）の生成ソース・`MasterDataSchema.cs`・`masterdata.bytes`・`masterdata.manifest.json` が変わる。列を足したので `MasterDataSchema.Hash` が変わり、古いバイナリは読めなくなる（クライアントのバイナリは `Hash` を鍵に暗号化される）。**サーバー（埋め込みリソース `Infrastructure/MasterData/masterdata.bytes`）とクライアント（`Assets/StreamingAssets/MasterData/masterdata.bytes`）の両方が、同じ `master-gen` で作り直されたこと**を `git status` で確かめる（`Generated/Lesson/Card.cs`・`CardFace.cs`・`MasterDataSchema.cs`・`masterdata.bytes`・`masterdata.manifest.json` が出力先ごとに変わる。ほかのテーブルの生成ソースは変わらない）
  6. **検証が効くことを手で確かめる**（テストは足さない）。先に `server/Uta.Server/Infrastructure/MasterData/masterdata.bytes` のハッシュ（`sha256sum`）を控える。(a) `card.csv` の `CardId` 1 の `Name` を空にして `gen.ps1 master-gen`、(b) `cardFace.csv` の `1,Normal` の `Description` を空にして実行、(c) `card.csv` の `CardId` 2 の `Name` を `まっすぐ一声`（`CardId` 1 と同じ）にして実行。**どれも終了コード 1 で、(a) は `card: Name が空`、(b) は `cardFace: Description が空`、(c) は `Name` の重複を示すエラーが出る**。確認のたびに `masterdata.bytes` のハッシュが控えた値と同じであること（エラーのときはバイナリを書かない）を確かめ、CSV を元に戻す。全部戻したら `gen.ps1 check` が終了コード 0
  7. `MasterDataTests.cs` に 5章のサーバーのテスト 4 つを足す
- 境界: 空文字・空白だけの名前と説明文は検証で弾く（`IsNullOrWhiteSpace`）。名前の重複は `[Unique]` で弾く。3.1 の注意（属性名の衝突）に当たったらそこに書いた手順で
- 確かめ方: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（既存のテストと足した 4 つがすべて通る）

### タスク 2: クライアントのテスト

- 前提: Unity 7（`7000.0.0a12`）の Editor を開ける環境。タスク 1 でクライアントの生成ソースとバイナリが作り直されている
- 触るファイル: `client/uta/Assets/Tests/EditMode/MasterDataLoaderTests.cs`
- 手順:
  1. 5章のクライアントのテスト 1 つを `MasterDataLoaderTests` に足す（C# 9 の範囲。`using System.Linq;` を足す）
  2. `client/uta` で `unity recompile --format json` を実行し、エラー 0
  3. Editor を開いたまま、EditMode のテストを `TestRunnerApi` の eval で回す（[P1](2026-11-07-client-prefabs-common.md) の決めたこと 12。コマンドは CLAUDE.md「ビルド・テスト・実行」・[unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md) 4章。Editor は閉じない）。回せなければユーザーに「Editor の Test Runner → EditMode → Run All を実行して結果を教えてほしい」と依頼する
- 確かめ方: EditMode のテストがすべて通る（既存の `Load_Bundled_ReturnsDatabase`・`MasterCardCatalogTests` を含む。新しいバイナリが新しい `MasterDataSchema.Hash` で読めることもこれで確かめる）。結果を見ずに「通った」と書かない

### タスク 3: wiki の更新

- 触るファイル: 2章のタスク 3 の行。各ページの frontmatter の `updated` を `2026-11-03` にする
- 手順（文はこのとおりに書く）:
  1. `lesson-tables.md` 1章の表:
     - `Card` の行の「主な列」を「`Name`（`string`。表示名。空でなく、表の中で重ならない）、プラン、種別、レアリティ、`UnlockLv`、`OnceInLesson`、`Unique`、`StartInHand`」にする
     - `CardFace` の行の「主な列」を「`EffectGroupId`、`UseConditionGroupId`（空欄＝条件なし）、`Description`（`string`。その面の効果の説明文。空でない。消費は書かない）」にする
     - 表の下の箇条に「- `Card.Name`・`CardFace.Description` は人が書いた文。値は `card.csv`・`cardFace.csv` が正。説明文が `Effect`・`CardCost`・`Condition` の数値と食い違わないことは自動では検証しない（CSV を変えたらレビューで突き合わせる）決定。2026-11-03。[計画]」を足す
  2. `lesson-tables.md` 2章の 1 つ目の箇条の後に「- `Card` の `IValidatable` は `Name` が空・空白だけでないこと、`CardFace` の `IValidatable` は `Description` が空・空白だけでないことを見る。`Card.Name` の重複は `[Unique]`」を足す
  3. `lesson-tables.md` 実装メモの最後に「2026-11-03（計画）: `Card.Name`（`card.csv` の 2 列目）・`CardFace.Description`（`cardFace.csv` の 5 列目）を足した。36 枚の名前と 72 面の説明文は計画の 3.4。列を足したので `MasterDataSchema.Hash` が変わり、サーバー・クライアントのバイナリを作り直した」を足す
  4. `master/README.md` 1章の表: `Card` の行の「中身」を「スキルカードの見出し（名前、プラン、種別、レアリティ、解放 Lv など）」、`CardFace` の行の「中身」を「通常 / 強化の面。効果・使用条件のグループを指す。説明文」にする。5章の「`[ClientOnly]` のテーブル・列はまだ無い（表示名・アイコンは未定義）」を「`[ClientOnly]` のテーブル・列はまだ無い（表示名・説明文は Common の列: `Idol.Name`・`SupportCard.Name`・`Card.Name`・`CardFace.Description`。アイコンは未定義）」にする
  5. `masterdata-pipeline.md` 未決事項の「表示名を `[ClientOnly]` の列に分けるか。今は各テーブルの `Name` が Common にある」を「表示名・説明文を `[ClientOnly]` の列に分けるか。今は各テーブルの `Name` と `CardFace.Description` が Common にある」にする
  6. `lesson-card.md` 14章の見出しの直後（「効果の数値は参考作品の値を…」の段落の前）に「カードの名前と説明文はマスタ（`masterdata/csv/card.csv` の `Name`・`cardFace.csv` の `Description`）が正。このページの ID と効果の記載は名前の代わりではなく、効果の値の出典（lesson-tables.md）。」の 1 行を足す。未決事項の「カード・ステータスの表示名(仮称を Uta 独自の名称に置き換える)」を「ステータスの表示名(仮称を Uta 独自の名称に置き換える。置き換えたらカードの説明文も書き直す)。カードの名前は 2026-11-03 にマスタへ入れた（Uta 独自の名前）」にする
  7. `python -I .claude/skills/linting-wiki/lint_wiki.py` が終了コード 0
- 確かめ方: lint が通る。リンク（`../../../plans/…`・`../architecture/master/…`）が切れていない

## 5. テスト

### サーバー（`server/Uta.Server.Tests/MasterData/MasterDataTests.cs`。既存の `Db` を使う）

| テスト | 中身 |
|---|---|
| `Cards_NameIsNotBlank` | `Db.CardTable.All` の全行で `string.IsNullOrWhiteSpace(x.Name)` が偽。失敗のメッセージに `CardId` を出す（`Assert.True(…, $"card {card.CardId} の Name が空")`） |
| `Cards_NamesAreDistinct` | `Db.CardTable.All.Select(x => x.Name)` に重複がない（`Distinct().Count()` が 36） |
| `CardFaces_DescriptionIsNotBlank` | `Db.CardFaceTable.All` の全行（72）で `string.IsNullOrWhiteSpace(x.Description)` が偽。メッセージに `CardId` と `Face` を出す |
| `CardTexts_SpotCheck` | 列のずれを見つけるための抜き取り: `CardId` 1 の `Name` が `まっすぐ一声`、`(1, Normal)` の `Description` が `パラメータ+9`、`(1, Enhanced)` が `パラメータ+14`。`CardId` 104 の `Name` が `波に乗るサビ`、`(104, Normal)` が `好調2ターン以上のとき使用可。パラメータ+10（好調の効果2倍）`。`CardId` 215 の `Name` が `響きあう想い`、`(215, Enhanced)` が `好印象+6、好印象の170%分パラメータ上昇` |

件数のテスト（`Cards_HaveExpectedCounts`）は変えない（36・72 のまま）。

### クライアント（`client/uta/Assets/Tests/EditMode/MasterDataLoaderTests.cs`。Unity でしか確かめられない）

| テスト | 中身 |
|---|---|
| `Load_Bundled_CardTextsAreNotBlank` | `MasterDataLoader.Load(ReadBundled())` の `CardTable.All` の全行で `Name`、`CardFaceTable.All` の全行で `Description` が `string.IsNullOrWhiteSpace` で偽。加えて `db.CardTable.FindByCardId(1).Name` が `まっすぐ一声`（クライアントのバイナリが新しい列で作り直されたことの確認） |

`Load_Bundled_ReturnsDatabase` の件数は変えない。

## 6. 未決事項

なし。名前の案（3.4 の 36 個）は、ユーザーが「案のままでよい」と確認した（2026-11-07。ユーザーの判断）。名前の付け方を直すときは 3.4 の表と 5章のテストを直す。

## 7. 結果の記録（実装の後）

- 実装結果と計画のズレ（3.1 の属性名の衝突に当たったかなど）を `lesson-tables.md` の実装メモに書き、この計画を `status: done` にする
- コミットメッセージに、足した列・決めたこと（区分は Common、空の拒否は `IValidatable`、重複は `[Unique]`、説明文と数値の一致は自動で検証しない）と理由を書く
