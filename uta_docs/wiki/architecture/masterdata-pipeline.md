---
title: マスタデータの定義・生成・配信（実装設計書 マスター設計編）
status: draft
updated: 2026-10-28
---

# マスタデータの定義・生成・配信（実装設計書 マスター設計編）

実装設計書（4編）の2編目。マスタデータ（排出テーブル、カード定義、週構成など）を、1つの定義からクライアント用・サーバー用に出し分けて作り、配る仕組みを定める。前提は [repository-layout.md](repository-layout.md)。

- マスタの**中身**（どんなテーブルが要るか）は `systems/` が正。ここには、定義・生成・配信の仕組みと、表現の規約を書く
- 仕組みは自作の [MasterMemoryAddons](../../../external/MasterMemoryAddons/README.md)（`external/`）を使う。採用は [ADR 0007](../decisions/0007-masterdata-with-addons.md)（accepted）

## 凡例

- **[決定]**: CLAUDE.md・accepted の ADR（主に [ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- **[Uta案]**: この叩き台の提案
- **[確認]**: MasterMemoryAddons の docs（2026-10-25 版）で確認した仕様。アドオンが更新されたら見直す

## 決まっていること

- マスタデータは MasterMemory を使う（[server-foundation.md](server-foundation.md)）
- **列の型は `int` / `long` / `string` / `bool` / enum だけ**。`float`・配列・nullable はマスタでは扱わない [決定。2026-10-27 に確定]
- クライアント用・サーバー用の出し分けは、定義に付ける `[ClientOnly]` / `[ServerOnly]` で行う（列にも、型全体にも付けられる）[確認]
- マスタの正本は最上位の `masterdata/` に置く [決定]
- カード効果などの入れ子は、汎用列＋型付きの参照列で表す。効果と条件はグループにまとめる [決定。3章、`Op` の一覧は 3.1]
- 生成したソースはコミットし、CI の `master-gen --check` で検出する。バイナリもコミットする [決定。5章、[ADR 0013](../decisions/0013-commit-masterdata-binary.md)]
- 配信は、まずアプリに同梱（StreamingAssets）し、あとで Addressables に移る [決定。6章]
- CSV は Git の `masterdata/csv/` を正本にする [決定。7章]

## 1. 全体フロー

```
masterdata/Definitions/*.cs        … 全列入りの定義（正本）
        │  code-gen
        ├──▶ client/uta/.../MasterData/Generated/   クライアント用ソース（ServerOnly を除く）
        └──▶ server/Uta.Server/MasterData/Generated/ サーバー用ソース（ClientOnly を除く）
                   │  各プロジェクトで MasterMemory の Source Generator が MemoryDatabase を生成

masterdata/csv/*.csv               … スプレッドシートから書き出した CSV
        │  master-gen（検証 → バイナリ生成）
        ├──▶ クライアント用 masterdata.bytes（暗号化）＋ manifest.json
        └──▶ サーバー用 masterdata.bytes ＋ manifest.json
                   │
                   ├─ クライアント: Addressables などで配信（Uta 側の仕事。アドオンに配信の仕組みはない）
                   └─ サーバー: ファイルから読み込み
```

- コード生成（`code-gen`）とマスタ生成（`master-gen`）は別の機能。マスタ生成は、生成ソースが古ければコード生成をやり直す [確認]
- スプレッドシートから CSV を書き出す部分は、アドオンの範囲外 [確認]。Uta では CSV を正本にする（7章）

## 2. 定義の書き方 [確認]

[defining-master-data](../../../external/MasterMemoryAddons/docs/guide/defining-master-data.md) のルールを、Uta の規約として採用する。

- `class` に `[MemoryTable("名前")]` と `[MessagePackObject(true)]`。**テーブル名に `_` は使えない**（CSV の水平分割の区切りに使うため）
- プロパティは `{ get; init; }`。テーブルの型は `class` のみ（`record` / `struct` は定義のエラー）
- 主キーは `[PrimaryKey]`（複合キーは `keyOrder`）、検索用は `[SecondaryKey(n)]`
- enum は C# で定義し、CSV に書く名前を `[InputName]` で付ける（名称は仮称。[glossary.md](../glossary.md)、[ADR 0001](../decisions/0001-ip-policy.md)）
- 検証は属性で書く: `[Unique]`、`[Exists(typeof(T))]`（空欄を許すなら `AllowEmpty = true`）。違反は `master-gen` が「ファイル 行 列」で報告する。生成ソースには残らない
- Unity に入るソースとしての注意: `namespace` はブロック形式、`required` 不可、`record struct` 不可、nullable 不可、`init` は `IsExternalInit` が必要
- 空欄は型の既定値（`0` / `false` / `""` / enum の 0）。主キーの空欄はエラー
- `IValidatable<T>` は使えるが、検証のコードがクライアント用の出力にも入る。**`[ServerOnly]` の列・テーブルを参照する検証は書けない**。できるだけ属性で書く

定義の置き場 `masterdata/Definitions/` は、C# 9 の範囲で書く（クライアント用の生成ソースを Unity と同じ制約でコンパイルして検証されるため。[確認]）。`masterdata/.editorconfig` に `shared/` と同じ設定を置く案 [Uta案]。

## 3. 表現の規約 [決定]

列の型を5種に絞ったことの帰結として、次のように表す。

| 表したいもの | 表し方 | 例 |
|---|---|---|
| 小数・確率・倍率 | 整数で持つ。単位を列名の末尾で示す | 排出率は万分率（`RatePerTenThousand`）、倍率はパーセント（`ScalePercent`） |
| 複数の値 | 子テーブルに分け、親の ID と順序（`Order`）で持つ | 週構成の「選べる行動の候補」 |
| 親子の整合 | 子の親 ID に `[Exists(typeof(親))]` | |
| 同じ列を型違いで使いたい | enum で種別を持ち、値は `int` の汎用列にする | 効果の種別 + 引数 |
| 真偽・有無 | `bool` / enum。nullable は使わない（空欄＝既定値） | |
| ID | `int`（[repository-layout.md](repository-layout.md) 10.1） | |
| 日時 | `string`。形式は `yyyy-MM-dd HH:mm:ss`、JST として解釈する（[repository-layout.md](repository-layout.md) 10.2） | ガチャの開催期間 |

- 計算は整数または固定小数点（[lesson-card.md](../systems/lesson-card.md) 9.6）。この規約は同じ理由（クライアントとサーバーで結果を一致させる）による
- [lesson-card.md](../systems/lesson-card.md) 3.2 の YAML 例には `focusScale: 1.5` のような小数がある。マスタに移すときは `150`（パーセント）のように整数にする

### 難所: カード定義の入れ子

[lesson-card.md](../systems/lesson-card.md) 3章のカード定義は、`base` / `enhanced`（`CardFace`）の中に `costs` / `useConditions` / `effects` の**配列**を持つ。さらに効果は、持続効果・発動予約の中に効果を持ち（入れ子）、効果ごとに条件（効果内条件）を持つ。列が5型だけで配列を使えないと、そのままでは表せない。

**効果と条件は「グループ」にまとめ、カードからグループを指す** [決定。2026-10-28]（テーブル・列の名前は仮）:

| テーブル | 主キー | 主な列 |
|---|---|---|
| `Card` | `CardId` | プラン、種別、レアリティ、`UnlockLv`、`OnceInLesson`、`Unique`、`StartInHand` |
| `CardFace` | `CardId` + `Face`（通常 / 強化） | `EffectGroupId`、`UseConditionGroupId`（空欄＝条件なし） |
| `CardCost` | `CardId` + `Face` + `Order` | `Kind`（enum `CostKind`）、`Value` |
| `EffectGroup` | `EffectGroupId` | （見出し。`Memo` など） |
| `Effect` | `EffectGroupId` + `Order` | `Op`（enum）、`Status`（enum）、`Trigger`（enum）、`Value`、`Arg1`〜`Arg3`（`int`）、`RefConditionGroupId`、`RefEffectGroupId`、`RefCardId` |
| `ConditionGroup` | `ConditionGroupId` | （見出し） |
| `Condition` | `ConditionGroupId` + `Order` | `Subject`（enum）、`Comparator`（enum）、`Value`。グループ内はすべて満たすと真（AND） |

- グループの見出しテーブル（`EffectGroup` / `ConditionGroup`）を置くのは、参照列に `[Exists(typeof(EffectGroup))]` を付けて、存在を属性で検証するため（複合キーの子テーブルは `Exists` の参照先にしにくい。推測）
- グループの ID も、マスタの ID の方針どおり**採番の規則は作らない**（[repository-layout.md](repository-layout.md) 10.1）。手で振る
- コストはカードにしかない（ドリンク・育成アイテムはコストを持たない）ので、グループにせず `CardCost` のままにする
- メリット: 入れ子の深さに制限がなく、効果内条件を複数持てる。同じ `Effect` / `Condition` を育成アイテム・ドリンク（[lesson-card.md](../systems/lesson-card.md) 15.6）、アビリティ（[idol-support.md](../systems/idol-support.md)）からも使える
- デメリット: 1枚のカードが CSV の複数の表に分かれ、読みにくい。代替の「カードごとの表に `ParentOrder` と条件の列を足す」案は、効果内条件が1つに限られ、アイテム・アビリティで同じ表を作り直すことになるため採らない

- **効果は汎用列＋型付きの列で表す** [決定]
  - `Op` は効果の種類の enum（[lesson-card.md](../systems/lesson-card.md) 7章の `op:` にあたる）。効果エンジンは `Op` を見て処理を切り替える。一覧は 3.1
  - 対象のステータスは `Status`、持続効果の発動タイミングは `Trigger` という**型付きの enum の列**に入れる（`Arg` に数値で入れない）。CSV に名前で書け、enum の名前を `master-gen` が検証できるため。使わない行では空欄（enum の 0 ＝ `None`）
  - 数値は `Value` と `Arg1`〜`Arg3`（`int`）に入れる。小数は整数にする（例: `focusScale: 1.5` → `150`。パーセント）
  - **倍率・回数の `Arg` は 0（空欄）を既定値として扱う**（連撃 0 → 1回、倍率 0 → 100%、持続の制限 0 → 無制限）。どの項目も 0 そのものに意味がないため、「空欄＝既定値」の規約にそろえる
  - 他のテーブルを指す値は、汎用列に入れず `RefCardId` などの**専用の列**にし、`[Exists(typeof(...), AllowEmpty = true)]` を付ける。参照先の存在を属性で検証するため
  - `Op` ごとの引数の意味（`Value` / `Arg1`〜`Arg3` が何か、どの参照列を使うか）は一覧にして保守し、`IValidatable` で検証する（必須の引数が空でないか、使わない引数が埋まっていないか）。`[ServerOnly]` の値を参照しないので、クライアント用の出力に入っても問題ない
  - 引数が3つで足りない `Op` が出たら、`Arg` を増やすか、その `Op` だけ子テーブルにするかをその時に決める
- **日時の扱い** [決定]
  - 形式を検証する: `master-gen` は文字列の形式を検証しないので、`IValidatable` で `ParseExact` できるかを確かめる（共通の値なので、クライアント用の出力に入ってもよい）
  - スプレッドシートが日付を自動で変換しないよう、日時の列のセルは書式なしのテキストにする（書き出しで `2026/11/1 12:00` のような形に変わるのを防ぐ）
  - 解析は読み込みのたびに行わず、マスタを読むラッパーで一度だけ解析して持つ
- 代替の「`Op` ごとにテーブルを分ける」案は、テーブルが効果の種類だけ増え、記載順に処理するための結合も重くなるため採らない（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- 効果は「記載順に処理する」ので、`Order` を必ず持つ
- 同じ構造が週構成（[produce.md](../systems/produce.md) 3.1）、アイドル・サポートカードのアビリティ・イベント（[idol-support.md](../systems/idol-support.md)）にも出る。先に1つ（カード）で型を決め、他へ流用する

### 3.1 効果の `Op` 一覧 [決定。2026-10-28]

効果プリミティブ（[lesson-card.md](../systems/lesson-card.md) 7章）と列の対応。「フェーズ」はカードとして動かし始めるフェーズ（[lesson-card.md](../systems/lesson-card.md) 15章）で、enum には最初から全部を定義する（15.1）。空欄の列は使わない（`IValidatable` で埋まっていないことを確かめる）。

| `Op` | フェーズ | `Status` / `Trigger` | `Value` | `Arg1`〜`Arg3`・参照列 |
|---|---|---|---|---|
| `Param` | 1 | — | パラメータ | `Arg1` 連撃の回数（4）、`Arg2` 集中の倍率%（4）、`Arg3` 好調の倍率%（3） |
| `ParamByRatio` | 1（好印象）/ 2（元気・やる気） | `Status`＝参照元 | 倍率% | — |
| `ParamBonus` | 未定 | — | 上昇量増加% | `Arg1` ターン数 |
| `AddGenki` | 1 | — | 値 | — |
| `AddFixedGenki` | 2 | — | 値 | — |
| `RecoverHp` | 2 | — | 値 | — |
| `ScaleResource` | 2 | `Status`＝対象 | 倍率%（端数は切り捨て） | — |
| `SetResource` | 2 | `Status`＝対象 | 値 | — |
| `AddTurns` | 1（消費体力減少） | `Status` | ターン数 | — |
| `AddStack` | 1（好印象・消費体力削減） | `Status` | 値 | — |
| `AddPercent` | 未定 | `Status` | % | `Arg1` ターン数（0＝ずっと） |
| `Draw` | 1 | — | 枚数 | — |
| `AddUses` | 1 | — | 回数 | — |
| `ReplaceHand` | 未定 | — | — | — |
| `EnhanceHand` | 未定 | — | — | — |
| `CreateCard` | 未定 | — | — | `RefCardId`。生成する位置の持ち方は採用時に決める |
| `Schedule` | 未定 | — | 何ターン後か（次のターン＝1） | `RefEffectGroupId` |
| `Persistent` | 1（使用時） | `Trigger` | — | `Arg1` ターン数・`Arg2` 回数（0＝無制限）、`RefEffectGroupId`（発動する効果）、`RefConditionGroupId`（発動条件） |

- どの `Op` でも、`RefConditionGroupId` を埋めると**効果内条件**になる（満たさないとその行だけ飛ばす。判定はコストの前。[lesson-card.md](../systems/lesson-card.md) 6.2）。`Persistent` の `RefConditionGroupId` は効果内条件ではなく、発動のたびに見る条件として扱う
- [lesson-card.md](../systems/lesson-card.md) 7.6 の後回しのプリミティブ（`RepeatNext` / `AddLessonTurn` / `DeckManipulate`）は enum に入れず、そのカードを採用するときに足す
- 「未定」は、どのフェーズのカードにも使われていないもの（初期カードセットに無い）

`Status`（enum `StatusKind`）の値: `Genki`、`Impression`、`Motivation`、`Focus`、`GoodCondition`、`Excellent`、`CostDown`（消費体力減少）、`CostUp`（消費体力増加）、`CostReduce`（消費体力削減）、`GenkiBlock`（元気増加無効）、`DebuffGuard`（低下状態無効）、`ImpressionBoost`（好印象強化）、`FocusBoost`（集中強化）。名前は仮称（[glossary.md](../glossary.md)）。

`Trigger`（enum `TriggerKind`）の値。カードの種別での絞り込みは、条件ではなく `Trigger` の種類で表す:

| 値 | タイミング（[lesson-card.md](../systems/lesson-card.md) 10章） |
|---|---|
| `LessonStart` | L1 |
| `TurnStart` / `AfterTurnStart` | T3 / T5 |
| `EveryNTurns` | T7（N は `Value`） |
| `OnCostHpDecrease` | P4 |
| `OnCardUse` / `OnActiveCardUse` / `OnMentalCardUse` | P5 |
| `AfterCardUse` / `AfterActiveCardUse` / `AfterMentalCardUse` | P7 |
| `OnSkip` | E1 |
| `TurnEnd` | E2 |

`CostKind`: `Consume`、`HpDirect`（フェーズ1）、`GoodConditionTurns`、`ExcellentTurns`（フェーズ3）、`Focus`（フェーズ4）、`Impression`、`Motivation`（使うカードを採用したとき）。

`Condition` の `Subject`（enum `ConditionSubject`）と `Comparator`（`Ge` / `Le` / `Eq`）:

| `Subject` | `Value` | 例 |
|---|---|---|
| `Impression` / `Genki` / `Motivation` / `Focus` | 値 | L02（`Impression` `Ge` 1） |
| `GoodConditionTurns` / `ExcellentTurns` | ターン数（「状態である」は `Ge` 1） | S04、S11 |
| `HpPercent` | 最大体力に対する% | F05（`HpPercent` `Ge` 50） |
| `Turn` | 何ターン目か | |
| `TargetPercent` | クリア目標に対する% | |

例（L14 の通常版。ID は説明用）:

| テーブル | 行 |
|---|---|
| `CardCost` | `Consume` 4 |
| `CardFace` | `EffectGroupId` 1401 |
| `Effect`（1401） | 1: `AddStack` `Impression` 3 ／ 2: `Persistent` `OnMentalCardUse` → `RefEffectGroupId` 1402 |
| `Effect`（1402） | 1: `AddStack` `Impression` 1 |

## 4. 出し分けの区分 [Uta案]

どちらの側にあるべきかの暫定案。**要確認**のものはレビューで決める。

| 区分 | テーブル・列の例 | 理由 |
|---|---|---|
| 両方（Common） | 育成アイドル・サポートカードの基本値、スキルカード（表示と判定に使う面）、週構成、レッスンルール、ガチャの排出率・天井 | 画面で表示・確率表示（GAC-02）に使う。`Uta.Domain` が使う値も含む |
| `[ClientOnly]` | アイコンのアドレス、表示名、演出の設定 | サーバーに不要 |
| `[ServerOnly]` | レンタル候補（SUP-06）、報酬の設定、不正検知のしきい値（推測） | クライアントに渡さない |
| 要確認 | 試験の NPC スコア（[produce.md](../systems/produce.md) 5.3） | 画面に出すなら両方、出さないなら `[ServerOnly]` |

- `[ClientOnly]` / `[ServerOnly]` の列が `[Exists]` で別の側にだけ存在するテーブルを参照した場合の挙動は**未確認**。定義に書く前にアドオンで試す

## 5. 出力先と設定 [決定／パスは Uta案]

`masterdata/masterdata.yaml` の例。パスは設定ファイルのあるディレクトリからの相対 [確認]。

```yaml
definitions: Definitions
csv: csv
outputs:
  - name: unity
    target: client
    source: ../client/uta/Assets/Scripts/Infrastructure/MasterData/Generated
    namespace: Uta.MasterData
    binary: ../client/uta/Assets/StreamingAssets/MasterData/masterdata.bytes   # 同梱。コミットする
    encrypt: true
  - name: server
    target: server
    source: ../server/Uta.Server/MasterData/Generated
    namespace: Uta.MasterData
    binary: ../server/Uta.Server/MasterData/masterdata.bytes                   # コミットする
```

- **`source` のディレクトリは生成のたびに中身を丸ごと入れ替える**。生成専用のディレクトリにし、手書きのファイルを置かない [確認]
- 出力先には `namespace` が必須。`MasterDataSchema.cs`（スキーマのハッシュを持つ）も一緒に出る [確認]
- 開発用の出力先は `environments` で上書きできる [確認]
- **生成したソースはコミットする** [決定]。ツールがなくても Unity でプロジェクトを開け、レビューで差分も見える。生成し忘れは CI の `master-gen --check` が終了コード 1 で検出する
- **バイナリ（`masterdata.bytes`）とマニフェストもコミットする** [決定。2026-10-28。[ADR 0013](../decisions/0013-commit-masterdata-binary.md)]。clone した直後から、ツールなしで Unity で動かせる
  - **生成は CSV か定義を変えたときだけ行う**。クライアント用は暗号化のソルトと IV を毎回乱数で作り、マニフェストには `createdAt` が入るので、中身が同じでも生成するたびに差分が出る [確認。アドオンの pipeline.md]
  - CSV を変えたら、同じコミットでバイナリも作り直す。レビューは CSV の差分で行う
  - 作り直し忘れは、今の `--check` では検出できない（生成ソースしか見ない）。アドオンの改善（8章）が入るまでは運用で防ぐ

## 6. バイナリと配信

[確認]（[build-and-distribute](../../../external/MasterMemoryAddons/docs/guide/build-and-distribute.md)）

- バイナリには平文のヘッダ（スキーマの ID、データのハッシュ）が付く。アプリは `MasterDataBinary.Read(bytes, MasterDataSchema.Hash)` で、ヘッダを確かめ、暗号化されていれば復号してから `MemoryDatabase` に渡す
- 読めないときは `MasterDataFormatException`（`SchemaMismatch` はアプリとデータの定義が違う、`Corrupted` は壊れている）
- クライアント用だけ暗号化できる。**鍵はスキーマのハッシュで、アプリの中にある**ので、効果は難読化の範囲（改ざん・解析の防止ではない）と考える [Uta案の解釈]
- ハッシュ（鍵）をログ・サーバーへの送信・CDN のパスに出さない。外に出す値はスキーマの ID（マニフェストの `schemaId`）
- CDN の置き場所はスキーマの ID ごとに分け、古いアプリが残る間は古いスキーマのバイナリも置く
- 配信（CDN への配置とダウンロード）はアドオンにない。Uta 側で行う
- **段階的に進める** [決定]
  1. いまは**アプリに同梱**する（StreamingAssets）。配信基盤が要らない。そのかわり、マスタを更新するたびにアプリの更新が要る
  2. あとで **Addressables によるダウンロード**に移る。スキーマの ID ごとに置き場を分ける（上記）

クライアントの起動時にマスタを取得する流れは [screen-design.md](screen-design.md) の起動フローを参照。

## 7. 導入と運用

- ツール（`net10.0`）の実行方法: `external/MasterMemoryAddons` を `dotnet run --project ...Tools -- code-gen --config masterdata/masterdata.yaml` で呼ぶ。アドオンは Makefile を前提にしているが、Uta では make を使わず PowerShell のスクリプトで包む（下記）
- アプリが参照する MasterMemory / MessagePack のバージョンは、**アドオンのツールと同じにする**（アドオンの docs 時点で MasterMemory 3.0.4、MessagePack 3.1.12）[確認]。MagicOnion が要求する MessagePack のバージョンと揃うかは未確認（基盤の設計書は「MagicOnion 7 系なら MessagePack は 3 系」と書いている）
- アドオンの変更は、サブモジュール側を先に push してから Uta のポインタを更新する（[ADR 0005](../decisions/0005-foundation-submodule.md)）
- **CSV は Git の `masterdata/csv/` を正本にする** [決定]。差分をレビューでき、ビルドが再現できる
  - ファイル名＝テーブル名。水平分割は `item_weapon.csv` のように `_` で区切る [確認]
  - **最初はスプレッドシートを使わず、CSV を直接編集する** [決定。2026-10-28]。書き出しの手順が要らない。グループ ID を使う入れ子の表（3章）を手で編集するのがつらくなったら、スプレッドシートと書き出しのスクリプトを足す（そのときは、タブ名をテーブル名にそろえる）
- **ツールは PowerShell のスクリプトで包む** [決定。2026-10-28]。`masterdata/gen.ps1` から `dotnet run --project external/MasterMemoryAddons/...` を呼ぶ（`code-gen`・`master-gen`・`--check` を引数で切り替える案）。Windows で make が要らず、GitHub Actions でも `pwsh` で動く。スクリプトの中身は、実装するときに書く

## 8. アドオンの改善バックログ

Uta で MasterMemoryAddons を使って見つかった改善は、ここに書く。直したらアドオン側（`external/MasterMemoryAddons`）の docs と log に反映し、ここから消す（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）。

| 項目 | 内容 | 状態 |
|---|---|---|
| Unity での読み込みの確認 | アドオンの docs が「Unity での読み込みは未確認」と明記している。Uta が最初の実機確認になる | 未着手 |
| `[Exists]` が別の側を参照する場合 | `[ClientOnly]` / `[ServerOnly]` の列・テーブルを参照したとき、片側の生成物や検証がどうなるか | 未確認（4章） |
| Windows での実行 | アドオンは make を前提にしている。Uta は `masterdata/gen.ps1` で回避する（7章）。アドオン側に PowerShell のスクリプトを用意してもよい | 回避済み |
| 導入方法（配布形態） | アドオンの docs では未定。Uta ではサブモジュールの `dotnet run` で使う | 未定 |
| 日時の列の検証 | `string` の日時の形式（`yyyy-MM-dd HH:mm:ss`）を属性で宣言し、`master-gen` が検証できるとよい。いまは `IValidatable` で書く | 案 |
| 中身が同じなら書き換えない | データのハッシュ（`dataHash`）が既存のバイナリと同じなら、ファイルを書き換えない（マニフェストも）。バイナリをコミットするので、意味のない差分を出さないため（[ADR 0013](../decisions/0013-commit-masterdata-binary.md)） | 案 |
| `--check` でバイナリの古さも検出 | 出力済みのバイナリの `dataHash` が、今の CSV から計算した値と違えば終了コード 1 にする。作り直し忘れを CI で検出するため（[ADR 0013](../decisions/0013-commit-masterdata-binary.md)） | 案 |
| `Op` ごとの引数の検証 | 3章の「汎用列＋参照列」を属性で宣言できると、`IValidatable` の手書きが減る（推測。必要になってから検討） | 案 |

- 列の型（`int` / `long` / `string` / `bool` / enum のみ）は**改善の対象にしない**。Uta の確定事項（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）

## 未決事項

- 区分が「要確認」のテーブル（4章）、`Exists` が別の側のテーブルを参照した場合の挙動
- クライアントでの実機読み込み（Windows / Android / iOS）。アドオンの docs は「Unity での読み込みは未確認」と明記している
- MasterMemory・MessagePack・MagicOnion のバージョン整合
- Addressables へ移る時期と、古いスキーマのバイナリをどれだけ残すか
- ゲーム仕様の変更時の運用（定義の変更でスキーマ ID が変わる → 古いアプリとの互換）

## 実装メモ

（実装後に仕様とのズレをここに記録する）
