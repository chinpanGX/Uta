---
title: マスタデータの定義・生成・配信（実装設計書 マスター設計編）
status: draft
updated: 2026-10-31
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
- カード効果などの入れ子は、汎用列＋型付きの参照列で表す。効果と条件はグループにまとめる [決定。3章、`Op` と enum の一覧は [lesson-enums.md](lesson-enums.md)]
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
- Unity に入るソースとしての注意: `namespace` はブロック形式、`required` 不可、`record struct` 不可、nullable 不可、`init` は `IsExternalInit` が必要（Uta では `Uta.Client.Infrastructure` に置く。[repository-layout.md](repository-layout.md) 5章）
- 空欄は型の既定値（`0` / `false` / `""` / enum の 0）。主キーの空欄はエラー
- `IValidatable<T>` は使えるが、検証のコードがクライアント用の出力にも入る。**`[ServerOnly]` の列・テーブルを参照する検証は書けない**。できるだけ属性で書く

定義の置き場 `masterdata/Definitions/` は、C# 9 の範囲で書く（クライアント用の生成ソースを Unity と同じ制約でコンパイルして検証されるため。[確認]）。`masterdata/.editorconfig` に `shared/` と同じ設定を置く案 [Uta案]。

## 3. 表現の規約 [決定]

列の型を5種に絞ったことの帰結として、次のように表す。

| 表したいもの | 表し方 | 例 |
|---|---|---|
| 小数・確率・倍率 | 整数で持つ。単位を列名の末尾で示す | 確率は万分率（`RatePerTenThousand`）、カードの効果の倍率はパーセント（`ScalePercent`）、0.1% 刻みが要る係数（評価・スコアボーナスの係数、パラメータボーナス）は千分率（`PerThousand`）[2026-10-31]。ガチャの排出は率ではなく整数の重み（[gacha.md](../systems/gacha.md) 3.1） |
| 複数の値 | 子テーブルに分け、親の ID と順序（`Order`）で持つ | 週構成の「選べる行動の候補」 |
| 親子の整合 | 子の親 ID に `[Exists(typeof(親))]` | |
| 同じ列を型違いで使いたい | enum で種別を持ち、値は `int` の汎用列にする | 効果の種別 + 引数 |
| 真偽・有無 | `bool` / enum。nullable は使わない（空欄＝既定値） | |
| ID | `int`（[repository-layout.md](repository-layout.md) 10.1） | |
| 日時 | `string`。形式は `yyyy-MM-dd HH:mm:ss`、JST として解釈する（[repository-layout.md](repository-layout.md) 10.2） | お知らせの掲載期間など（MVP のガチャは期間を持たない） |

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
- メリット: 入れ子の深さに制限がなく、効果内条件を複数持てる。同じ `Effect` / `Condition` を育成アイテム・ドリンク（[lesson-card.md](../systems/lesson-card.md) 15.6）からも使える。アビリティの枠 1 は専用のテーブルにした（[idol-support.md](../systems/idol-support.md) 2.3）。枠 2〜6 で流用するかは idol-support.md の未決事項
- デメリット: 1枚のカードが CSV の複数の表に分かれ、読みにくい。代替の「カードごとの表に `ParentOrder` と条件の列を足す」案は、効果内条件が1つに限られ、アイテム・アビリティで同じ表を作り直すことになるため採らない

- **効果は汎用列＋型付きの列で表す** [決定]
  - `Op` は効果の種類の enum（[lesson-card.md](../systems/lesson-card.md) 7章の `op:` にあたる）。効果エンジンは `Op` を見て処理を切り替える。一覧は [lesson-enums.md](lesson-enums.md)
  - 対象のステータスは `Status`、持続効果の発動タイミングは `Trigger` という**型付きの enum の列**に入れる（`Arg` に数値で入れない）。CSV に名前で書け、enum の名前を `master-gen` が検証できるため。使わない行では空欄（enum の 0 ＝ `None`）。Domain の `EffectDef` も同じ列を型付きで持つ（[lesson-enums.md](lesson-enums.md)）
  - 数値は `Value` と `Arg1`〜`Arg3`（`int`）に入れる。小数は整数にする（例: `focusScale: 1.5` → `150`。パーセント）
  - **倍率・回数の `Arg` は 0（空欄）を既定値として扱う**（連撃 0 → 1回、倍率 0 → 100%、持続の制限 0 → 無制限）。どの項目も 0 そのものに意味がないため、「空欄＝既定値」の規約にそろえる
  - 他のテーブルを指す値は、汎用列に入れず `RefCardId` などの**専用の列**にし、`[Exists(typeof(...), AllowEmpty = true)]` を付ける。参照先の存在を属性で検証するため
  - `Op` ごとの引数の意味（`Value` / `Arg1`〜`Arg3` が何か、どの参照列を使うか）は [lesson-enums.md](lesson-enums.md) で保守し、`IValidatable` で検証する（必須の引数が空でないか、使わない引数が埋まっていないか）。`[ServerOnly]` の値を参照しないので、クライアント用の出力に入っても問題ない
  - 引数が3つで足りない `Op` が出たら、`Arg` を増やすか、その `Op` だけ子テーブルにするかをその時に決める
- **日時の扱い** [決定]
  - 形式を検証する: `master-gen` は文字列の形式を検証しないので、`IValidatable` で `ParseExact` できるかを確かめる（共通の値なので、クライアント用の出力に入ってもよい）
  - スプレッドシートが日付を自動で変換しないよう、日時の列のセルは書式なしのテキストにする（書き出しで `2026/11/1 12:00` のような形に変わるのを防ぐ）
  - 解析は読み込みのたびに行わず、マスタを読むラッパーで一度だけ解析して持つ
- 代替の「`Op` ごとにテーブルを分ける」案は、テーブルが効果の種類だけ増え、記載順に処理するための結合も重くなるため採らない（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- 効果は「記載順に処理する」ので、`Order` を必ず持つ
- 似た構造が、アイドル・サポートカードのアビリティ・イベント（[idol-support.md](../systems/idol-support.md)）にも出る。先に1つ（カード）で型を決める。流用するかは、それぞれを作るときに決める（アビリティの枠 1 は専用のテーブル、週構成の行動の効果は効果グループを使わない。[produce.md](../systems/produce.md) 3.1）

### 3.1 効果の `Op` 一覧と enum

`Op` ごとの列（`Status` / `Trigger` / `Value` / `Arg1`〜`Arg3` / 参照列）の対応と、効果・トリガー・ステータス・コスト・条件の enum の名前と数値は、[lesson-enums.md](lesson-enums.md) が正 [決定。2026-10-31]。マスタの `Effect` の行と Domain の `EffectDef` は同じ形にし、ここには書き写さない。

- `RefConditionGroupId` は、どの `Op` でも**効果内条件**（満たさないとその行だけ飛ばす。判定はコストの前。[lesson-card.md](../systems/lesson-card.md) 6.2）。持続効果（`AddPersistent`）の行でも同じで、付与するかどうかの条件になる。発動のたびに見る条件は、発動する効果のグループの行に付ける（[lesson-enums.md](lesson-enums.md)）

## 4. 出し分けの区分 [Uta案]

どちらの側にあるべきかの暫定案。**要確認**のものはレビューで決める。

| 区分 | テーブル・列の例 | 理由 |
|---|---|---|
| 両方（Common） | 育成アイドル・サポートカードの基本値、スキルカード（表示と判定に使う面）、週構成、レッスンルール、ガチャの排出率・天井 | 画面で表示・確率表示（GAC-02）に使う。`Uta.Domain` が使う値も含む |
| `[ClientOnly]` | アイコンのアドレス、表示名、演出の設定 | サーバーに不要 |
| `[ServerOnly]` | レンタル候補（SUP-06）、報酬の設定、不正検知のしきい値（推測） | クライアントに渡さない |
| （2026-10-31 に解決）試験の NPC スコア | Common（`ExamRivalScore`。[produce.md](../systems/produce.md) 3.1） | 試験中の順位の表示と判定を Shared.Domain で行うため |

- テーブルごとの区分は 9章の索引にまとめる

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

## 9. テーブルの索引 [索引。2026-10-31 時点]

MVP で使うマスタのテーブルの一覧。**列と検証の正は「正のページ」の表**で、ここは名前・主キー・区分だけを引くための索引（列はここに書かない。二重管理しないため）。テーブル・列の名前は、どれも正のページで「名前は仮」。テーブルを足す・消す・名前を変えるときは、正のページと同じ変更でここも直す。

区分の Common は両側に出すもの、ServerOnly はサーバーだけに出すもの（4章）。

### 9.1 レッスン・カード（7 テーブル）

正のページ: 3章「難所: カード定義の入れ子」（列）、[lesson-enums.md](lesson-enums.md)（enum と効果の列の対応）、[lesson-card.md](../systems/lesson-card.md) 3章（カード定義の考え方）

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Card` | `CardId` | Common | スキルカードの見出し（プラン、種別、レアリティ、解放 Lv など） |
| `CardFace` | `CardId` + `Face` | Common | 通常 / 強化の面。効果・使用条件のグループを指す |
| `CardCost` | `CardId` + `Face` + `Order` | Common | コスト |
| `EffectGroup` | `EffectGroupId` | Common | 効果のグループの見出し |
| `Effect` | `EffectGroupId` + `Order` | Common | 効果の 1 行（`Op` と引数） |
| `ConditionGroup` | `ConditionGroupId` | Common | 条件のグループの見出し |
| `Condition` | `ConditionGroupId` + `Order` | Common | 条件の 1 行（AND） |

- 区分は 4章の Uta案（スキルカードは表示と判定に使うので Common）。テーブルごとには決めていない

### 9.2 プロデュース（14 テーブル）

正のページ: [produce.md](../systems/produce.md) 3.1「マスタの表」

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Scenario` | `ScenarioId` | Common | シナリオ（難易度、週の数、パラメータ上限、再挑戦の回数など） |
| `ScenarioWeek` | `ScenarioId` + `Week` | Common | 週の種類と、その週のレッスン・試験の設定 |
| `WeekActionSlot` | `ScenarioId` + `Week` + `Order` | Common | 週の行動の枠 |
| `LessonSetting` | `LessonSettingId` | Common | レッスン・試験のターン数と目標 |
| `ClassOption` | `ScenarioId` + `Week` + `Order` | Common | 授業の選択肢 |
| `ExamRivalScore` | `ScenarioId` + `Week` | Common | 試験の NPC のスコア |
| `ExamPattern` | `ExamPatternId` | Common | 審査比率と試験のターン配分 |
| `ExamRankReward` | `ExamKind` + `Rank` | Common | 試験の順位ごとのパラメータ上昇 |
| `ScoreBonusSetting` | `Id`（1 行） | Common | スコアボーナスの係数 |
| `EvaluationScoreBand` | `Order` | Common | 最終評価のスコア分の帯 |
| `EvaluationRank` | `Rank` | Common | 評価値ごとのランク |
| `EvaluationRankReward` | `Rank` | **ServerOnly** | ランクごとの報酬 |
| `OfferRarityWeight` | `Rarity` | Common | カードの選択肢のレアリティの重み |
| `PlayerLevelExp` | `Level` | Common | プレイヤー Lv に要る経験値 |

### 9.3 育成アイドル・サポートカード（9 テーブル）

正のページ: [idol-support.md](../systems/idol-support.md) 6章「マスタの表」

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Idol` | `IdolId` | Common | 育成アイドル（レアリティ、プラン、ステータス、ボーナス、固有スキルカードなど） |
| `IdolRecommendDeck` | `Recommend` + `Order` | Common | おすすめ効果ごとの初期デッキのカード |
| `CommonDeckCard` | `Order` | Common | 初期デッキの共通の 5 枚 |
| `SupportCard` | `SupportCardId` | Common | サポートカード（レアリティ、タイプ、プラン） |
| `SupportAbility` | `SupportCardId` + `Order` | Common | サポートカードのアビリティ |
| `SupportLevelCost` | `Rarity` + `Level` | Common | レベル上げに要る強化pt・ゴールド |
| `SupportLevelCap` | `Rarity` + `LimitBreak` | Common | 上限解放の回数ごとの最大レベル |
| `InitialSupportCard` | `Order` | **ServerOnly** | 初期付与のサポートカード |
| `InitialSetting` | `Id`（1 行） | **ServerOnly** | 初期付与の通貨・ゴールド・強化pt |

### 9.4 ガチャ（5 テーブル）

正のページ: [gacha.md](../systems/gacha.md) 2章「マスタの表」（`ItemKind` の enum の正もここ）

| テーブル | 主キー | 区分 | 中身 |
|---|---|---|---|
| `Gacha` | `GachaId` | Common | ガチャ（価格、通常枠・確定枠のテーブル、天井の交換pt） |
| `GachaTable` | `GachaTableId` | Common | 排出テーブルの見出し |
| `GachaTableEntry` | `GachaTableId` + `Order` | Common | 排出テーブルの 1 行（`ItemKind` + `ItemId` と重み） |
| `GachaCeilingItem` | `GachaId` + `Order` | Common | 天井で交換できるもの |
| `DuplicateConversion` | `ItemKind` + `Rarity` | Common | 重複したときの変換量 |

### 9.5 索引から見えること

- 合計 35 テーブル。ServerOnly は 3 つ（`EvaluationRankReward`・`InitialSupportCard`・`InitialSetting`）。`[ClientOnly]` のテーブル・列はまだ無い（表示名・アイコンは未定義）
- ServerOnly のテーブルを Common のテーブルから `[Exists]` で参照するものは無い。逆向き（ServerOnly → Common。例: `InitialSupportCard.SupportCardId` → `SupportCard`）はある。両側に出すテーブルを指すので問題にならない見込み（推測。8章の `[Exists]` の確認で確かめる）

## 未決事項

- `Exists` が別の側のテーブルを参照した場合の挙動
- クライアントでの実機読み込み（Windows / Android / iOS）。アドオンの docs は「Unity での読み込みは未確認」と明記している
- MasterMemory・MessagePack・MagicOnion のバージョン整合
- Addressables へ移る時期と、古いスキーマのバイナリをどれだけ残すか
- ゲーム仕様の変更時の運用（定義の変更でスキーマ ID が変わる → 古いアプリとの互換）

## 実装メモ

（実装後に仕様とのズレをここに記録する）
