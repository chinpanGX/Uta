---
title: ガチャ
status: draft
updated: 2026-10-31
---

# ガチャ

## 目的

排出テーブルに基づく抽選をサーバーで行い、結果を所持品へ反映する基礎部分を実装する。排出されるのは [育成アイドルとサポートカード](idol-support.md)。

参考作品の構造を調査して決めた。名称は Uta の仮称([ADR 0001](../decisions/0001-ip-policy.md))。

凡例: **[確認]** 参考作品で裏取りした構造・数値 / **[決定]** Uta で決めたこと / **[Uta案]** 提案

用語の定義は [glossary.md](../glossary.md) の「育成アイドル・ガチャ（仮称）」の表が正。このページには書かない。

## 完了条件(基礎部分)

- [ ] 単発 / 10連の抽選がサーバーで実行される
- [ ] 排出テーブル(マスタデータ)から抽選できる
- [ ] 天井(交換pt)が動く
- [ ] 抽選結果と所持品の更新が1トランザクションで整合する
- [ ] 抽選履歴が保存される

## 決まっていること

- 抽選・所持品更新はすべてサーバー(サーバー権威)。抽選の結果は乱数が絡むのでクライアントは予測しない。引けるか・交換できるか(通貨・交換pt が足りるか)の判定はクライアントもする([ADR 0008](../decisions/0008-shared-domain-and-onion.md))
- **課金は実装しない。ガチャ通貨は無償の1種類だけ**。有償・無償の区別や決済まわりの処理は作らない [決定]
- **MVP のガチャは 1 本で、期間を持たない**。交換メダル・ショップ・1日1回割引は作らない [決定。2026-10-31。[game-spec.md](../overview/game-spec.md) 最小実装範囲]
- 3〜5章の数値は、参考作品の値を**マスタの初期値**として使う。Uta で検証しやすい値に変えてよい [決定。2026-10-31。[ADR 0016](../decisions/0016-reference-game-values.md)]
- ガチャ通貨の入手は初期付与とプロデュースの報酬だけ(8章)

---

## 1. ガチャの持ち方 [決定。2026-10-31]

- ガチャは**マスタの 1 行**(価格・排出テーブル・天井の交換対象)。参考作品の「常設 / ピックアップ / 限定」の種類は持たない。中身を入れ替えたいときは、新しいガチャ ID の行を作る(「ラインナップ更新」という操作は持たない)
- MVP はガチャを 1 本だけ置く
- 参考作品には、常設ガチャ(恒常。ラインナップは不定期に更新)、ピックアップガチャ(新しいカードがピックアップされる)、限定ガチャ(その種類でしか出ないカードがある)がある [確認]。期間・ピックアップは MVP の後に足す(6章 GAC-06)

## 2. 価格 [確認]

- 単発: ガチャ通貨250。10連: 2,500(10枠目は SR 以上)。
- 育成アイドルとサポートカードは同じガチャから出る。

## 3. 排出率

参考作品の排出率 [確認]。マスタの初期値はこの率になるように重みを置く。

| | SSR | SR | R | 合計 |
|---|---|---|---|---|
| 育成アイドル | 2.0% | 6.8% | 31.2% | 40.0% |
| サポートカード | 3.0% | 10.2% | 46.8% | 60.0% |
| 合計 | 5.0% | 17.0% | 78.0% | 100% |

参考作品のピックアップの率(全体の内数。ピックアップ育成アイドル SSR 0.75%、ピックアップサポートカード SSR 1.0%・SR 4.0%)は、ピックアップを作るときの参考にする。

### 3.1 排出テーブル [決定。2026-10-31]

- 排出テーブルは、**カードごとの整数の重み**だけを持つ。カード 1 枚の確率は「重み ÷ テーブルの重みの合計」。画面に出すレア別・種別の % は、重みを合計して計算する。率をマスタに別に持たないので、表示と抽選が食い違わない(二重管理しない)
- 表示の % は小数第 2 位まで出し、第 3 位以下を切り捨てる。合計がちょうど 100.00% にならないことは受け入れる
- 確定枠のテーブルの初期値は、通常枠から R を除いたときの比(条件付きの分布)にする。SSR と SR の比は 5 : 17、各レアの中の育成アイドルとサポートカードの比は通常枠と同じ(40 : 60)。10 枠目の SSR は約 22.7% になる
- 排出テーブルの中身(行)を変えるときは、新しい `GachaTableId` のテーブルを作る。同じ ID のまま変えない(履歴の `gacha_table_id` から当時の確率を追えるようにする)
- ガチャは「通常枠のテーブル」と「確定枠のテーブル」の 2 つを指す。単発と 10 連の 1〜9 枠目は通常枠、10 連の 10 枠目は確定枠から引く。確定枠のテーブルには SR 以上のカードだけを入れる。参考作品の「10連のどこかに SR 以上 1 枚」ではなく「10 枠目を常に SR 以上」に単純化した
- マスタの表(名前は仮。[masterdata-pipeline.md](../architecture/masterdata-pipeline.md) の規約に従う):

| テーブル | 主キー | 列 |
|---|---|---|
| `Gacha` | `GachaId` | `SingleCost`、`TenCost`、`NormalTableId`、`GuaranteedTableId`、`CeilingPoints`(交換に要る交換pt) |
| `GachaTable` | `GachaTableId` | (見出し) |
| `GachaTableEntry` | `GachaTableId` + `Order` | `ItemKind`(enum: `None` / `Idol` / `SupportCard`)、`ItemId`、`Weight`(1 以上) |
| `GachaCeilingItem` | `GachaId` + `Order` | `ItemKind`、`ItemId` |
| `DuplicateConversion` | `ItemKind` + `Rarity` | `Amount`(育成アイドルはかけらの量、サポートカードは完凸後の強化pt の量。5章・[idol-support.md](idol-support.md) 3章の値) |

- 区分: どれもクライアントで確率を表示するので Common(クライアントにも出す)。`DuplicateConversion` も、結果の画面で量を見せるので Common
- 育成アイドルとサポートカードを 1 つの列 `ItemId` で指す(専用の参照列にしない)のは、DTO と履歴の形(`ItemKind` + `ItemId`)にそろえるため。存在の検証は `[Exists]` ではなく `IValidatable` で、`ItemKind` ごとのマスタを引いて行う
- 検証(`IValidatable`): 重みが 1 以上、テーブルごとの重みの合計が `int.MaxValue` 以下(`RandomOps.WeightedIndex` の制約)、テーブルが空でない、`ItemId` が存在する、確定枠のテーブルに R がない(`ItemId` のマスタの `Rarity` を見る)

### 3.2 抽選 [決定。2026-10-31]

- 乱数は `xoshiro256**`(CLAUDE.md・[repository-layout.md](../architecture/repository-layout.md) 10.3)。**要求ごとに**、サーバーが暗号用乱数(.NET の `RandomNumberGenerator`)で 64 ビットのシードを作り、SplitMix64 で 4 回引いて `Xoshiro256StarStar` の状態にする([lesson-domain-types.md](../architecture/lesson-domain-types.md) 4章と同じ初期化)。シードは保存しない(ガチャは再現を求めない。結果は履歴に残る)
- 1 枠につき 1 回、`RandomOps.WeightedIndex`(棄却法。偏りなし)でテーブルから 1 行を選ぶ。候補は `GachaTableEntry` の `Order` の昇順に並べて渡す。レア → カードのような 2 段階には分けない
- 10 連は 1 枠目から順に引き、所持品への反映(5章)も枠の順に行う
- 確かめ方: テストでは、決まった値を返す `IRandom` を渡して、重みから期待どおりの行が選ばれることを単体テストで確かめる。大量に引いて率を見る統計のテストはしない。`RandomOps.WeightedIndex` のテストは今は重み 0 の 1 件だけなので、累積の境界(どの値がどの行に当たるか)のテストをガチャの実装計画で `RandomTests` に足す

## 4. 天井(交換pt)

- 1回引くごとに1pt(10連は10pt)。ガチャごとに数える。[確認]
- `CeilingPoints`(初期値 200 [確認])を消費して、`GachaCeilingItem` の一覧から 1 つ選んで受け取る [決定。2026-10-31]。交換すると交換pt は `CeilingPoints` だけ減る(余りは残る)
- 受け取ったカードは、引いたときと同じ扱い(5章。所持済みなら重複。完凸後のサポート強化pt への変換も同じ)。交換では交換pt を加算しない
- 交換も履歴に 1 行残す: `slot` は 0、`gacha_table_id` は 0(テーブルから引かない)、`request_id` は `x-request-id`
- MVP はガチャに期間がないので、交換pt は消えない。参考作品の「期間終了後に交換メダルへ変換」「常設はラインナップ更新時にリセット」[確認] は、期間を作るときに決める
- 天井の確認は、`CeilingPoints` をマスタで小さくして行う

## 5. 重複したとき

育成アイドル [決定。2026-10-31]:

- 育成アイドルは 1 キャラ 1 枚([idol-support.md](idol-support.md) 1.1)。持っていないアイドルなら所持にする。持っているアイドルなら、そのアイドルのかけらに変換する。量はそのアイドルのレアで決まる(下の表 [確認])
- 同じ 10 連の中で同じアイドルが 2 回出たら、枠の順に処理する(1 回目で所持、2 回目はかけら)

| レア | かけら |
|---|---|
| SSR | 50 |
| SR | 25 |
| R | 5 |

- かけらは覚醒([idol-support](idol-support.md) 1.6)に使う。覚醒は MVP では作らないので、かけらは貯まるだけになる
- 参考作品の「余ったかけらの交換メダルへの変換(SSR 1個=5、SR=2、R=1)とショップ」「ピックアップ入手のおまけのかけら 50」[確認] は採らない(MVP の後に検討)

サポートカード: 1枚ずつ所持ID(ULID)で持ち、重複は手動の上限解放の素材に使う([idol-support](idol-support.md) 2.2.1)。同じカードの完凸のものを持っていれば、引いた時点でサポート強化pt に変換する(7章) [決定。2026-10-31]。

結果の種類(`Outcome`。DTO と履歴の `outcome`)[決定。2026-10-31]: **行が増えた・所持になったら `New`、増やさずに変換したら `Duplicate`**。

| 場合 | `Outcome` | `Shards` | `SupportPoints` | `SupportCardUid` |
|---|---|---|---|---|
| 育成アイドルを初めて得た | `New` | 0 | 0 | 空 |
| 育成アイドルを持っていた(かけらに変換) | `Duplicate` | 量 | 0 | 空 |
| サポートカードの行が 1 つ増えた(初めてでも、完凸のものがない 2 枚目以降でも) | `New` | 0 | 0 | 増えた所持ID |
| サポートカードを強化pt に変換した(完凸のものを持っていた) | `Duplicate` | 0 | 量 | 空 |

量は `DuplicateConversion`(3.1)から引く。

## 6. 実行要件・設計上の論点

| ID | 要件 | 優先度 |
|---|---|---|
| GAC-01 | ガチャを引ける(単発・10連) | 必須 |
| GAC-02 | 排出率をマスタで定義でき、画面で確率を表示できる | 必須 |
| GAC-03 | 交換pt による天井 | 必須 |
| GAC-04 | 重複時のかけら変換・サポート強化pt 変換 | 必須 |
| GAC-05 | 交換メダル(かけら・交換pt からの変換)とショップ | 後回し |
| GAC-06 | ガチャの期間とピックアップ | 後回し |
| GAC-07 | 限定ガチャ | 後回し |

| 論点 | 方針 |
|---|---|
| 抽選結果の記録 | **カード 1 枚 = 1 行**。`DrawAsync` 1 回の行は `request_id`(`x-request-id`)で束ねる。列は [api-design.md](../architecture/api-design.md) 3章 |
| 確率の表示と実際の一致 | 3.1 の重みから表示と抽選の両方を作る |
| 同時実行 | 所持品を変える RPC(`DrawAsync`・`ExchangeAsync`)は、トランザクションの**最初に `players` の行を更新またはロックする**。`DrawAsync` は通貨の減算 `UPDATE players SET gacha_currency = gacha_currency - @cost WHERE player_id = @id AND gacha_currency >= @cost`(更新が 0 行なら `GACHA_NOT_ENOUGH_CURRENCY`)、`ExchangeAsync` は `SELECT ... FROM players WHERE player_id = @id FOR UPDATE`。この行ロックで同じプレイヤーの要求が直列になるので、重複の判定(5章)はその後に読めば正しい。抽選・所持品・履歴・冪等性の保存済みレスポンスは同じトランザクションで書く |
| API | RPC・DTO・エラーは [api-design.md](../architecture/api-design.md) 1・2章 |
| `ExpectedCost` | 画面に出した価格と違う額を引かれないための保険(マスタの版はアプリのバージョンと同じなので、食い違うのはクライアントの不具合のとき) |

### 6.1 処理の手順 [決定。2026-10-31]

サーバーはマスタを Infrastructure の `MemoryDatabase` から読む([repository-layout.md](../architecture/repository-layout.md) 4章)。`server/` と `masterdata/` はまだない(作るのはそれぞれの最初の実装計画)。

`DrawAsync`(1 つのトランザクション):

1. `Count` が 1 か 10 でなければ `GACHA_INVALID_COUNT`
2. `GachaId` がマスタになければ `GACHA_NOT_FOUND`
3. 価格を決める(`Count` が 1 なら `SingleCost`、10 なら `TenCost`)。`ExpectedCost` と違えば `GACHA_COST_MISMATCH`
4. 通貨を減らす(上の SQL)。0 行なら `GACHA_NOT_ENOUGH_CURRENCY`
5. シードを作って乱数を用意する(3.2)
6. 枠ごとに(1 から `Count` まで)、抽選する(10 連の 10 枠目は確定枠)→ 5章の処理 → 履歴に 1 行
7. 交換pt を `Count` だけ足す(`gacha_exchange_points` に行がなければ作る。`INSERT ... ON CONFLICT (player_id, gacha_id) DO UPDATE SET points = points + @n`)
8. Response を作る(api-design.md 1.1)

`ExchangeAsync`(1 つのトランザクション):

1. `GachaId` がマスタになければ、または `CeilingItemOrder` が `GachaCeilingItem` になければ `GACHA_NOT_FOUND`
2. `players` の行をロックする(上の SQL)
3. 交換pt を減らす: `UPDATE gacha_exchange_points SET points = points - @c WHERE player_id = @id AND gacha_id = @g AND points >= @c`。0 行(行がない場合を含む)なら `GACHA_NOT_ENOUGH_POINTS`
4. 5章の処理 → 履歴に 1 行(4章)
5. Response を作る

`GetGachasAsync`: マスタの `Gacha` を `GachaId` の昇順にすべて返す。交換pt の行がないガチャは 0 を返す。

## 7. Uta で決めること(参考作品で未公開)

| 項目 | 決めたこと |
|---|---|
| 10連の確定枠 | 確定枠のテーブルから引く(3.1) [決定] |
| 完凸後のサポートカードの重複 | サポート強化pt に変換。量は [idol-support.md](idol-support.md) 3章が正 [決定] |

## 8. ガチャ通貨の入手 [決定。2026-10-28]

- ガチャ通貨の入手手段は、**アカウント作成時の初期付与**(2,500。仮。[game-spec.md](../overview/game-spec.md))と、**プロデュースの報酬**([produce.md](produce.md) 6.1)の2つ。ログインボーナスは後回し

## 未決事項

- 期間・ピックアップを作るとき、またはガチャ ID を替えるとき(1章)の、古い ID に残った交換pt の扱い(交換メダルへの変換・新しい ID への引き継ぎ・リセット)。MVP の間はガチャ ID を替えない


## 実装メモ

(実装後に仕様とのズレをここに記録する)
