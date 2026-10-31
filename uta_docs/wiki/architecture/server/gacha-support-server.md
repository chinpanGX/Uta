---
title: ガチャ・サポートカードのサーバー処理
status: draft
updated: 2026-10-31
---

# ガチャ・サポートカードのサーバー処理

[gacha.md](../../systems/gacha.md) と [idol-support.md](../../systems/idol-support.md)（ゲームの仕様）を、サーバーでどう処理するかを書いた実装設計書。ルール・値・エラーになる条件の正は各システムのページ、抽選の実装・SQL・トランザクションの手順の正はこのページ。RPC・DTO・エラーの一覧とテーブルは [api-design.md](../api-design.md)。2026-10-31 に gacha.md の 3.2・6章・6.1 と idol-support.md 2.2.1 の手順を移した（中身は変えていない）。

## 1. ガチャの抽選 [決定。2026-10-31]

- 乱数は `xoshiro256**`(CLAUDE.md・[repository-layout.md](../repository-layout.md) 10.3)。**要求ごとに**、サーバーが暗号用乱数(.NET の `RandomNumberGenerator`)で 64 ビットのシードを作り、SplitMix64 で 4 回引いて `Xoshiro256StarStar` の状態にする([lesson-domain-types.md](../lesson-domain-types.md) 4章と同じ初期化)。シードは保存しない(ガチャは再現を求めない。結果は履歴に残る)
- 1 枠につき 1 回、`RandomOps.WeightedIndex`(棄却法。偏りなし)でテーブルから 1 行を選ぶ。候補は `GachaTableEntry` の `Order` の昇順に並べて渡す。レア → カードのような 2 段階には分けない
- 置き場: 抽選はサーバー専用([api-design.md](../api-design.md) 4章)なので、`server/` の Domain 層に置く。抽選の関数は、候補の行の並びと `IRandom` を引数で受け取り、選んだ行の添字を返す純粋な関数にする(シードの作成は Application 層)。型名・シグネチャはガチャの実装計画で決める
- 確かめ方: テストでは、決まった値を返す `IRandom` を渡して、重みから期待どおりの行が選ばれることを単体テストで確かめる。大量に引いて率を見る統計のテストはしない。`RandomOps.WeightedIndex` のテストは今は重み 0 の 1 件だけなので、累積の境界(どの値がどの行に当たるか)のテストをガチャの実装計画で `RandomTests` に足す
- テスト用の `IRandom` は、選ばせたい値 `r`(0 以上・重みの合計 `total` 未満)に対して **`r + total` を返す**。`NextInt` の棄却法は `2^64 mod total` 未満の値を捨てて引き直すので、`r` をそのまま返すと永遠に引き直すことがある。`r + total` はしきい値以上で、`total` で割った余りが `r` になる

## 2. ガチャの処理の手順 [決定。2026-10-31]

| 論点 | 方針 |
|---|---|
| 抽選結果の記録 | **カード 1 枚 = 1 行**。`DrawAsync` 1 回の行は `request_id`(`x-request-id`)で束ねる。列は [api-design.md](../api-design.md) 3章 |
| 同時実行 | 所持品を変える RPC(`DrawAsync`・`ExchangeAsync`)は、トランザクションの**最初に `players` の行を更新またはロックする**。`DrawAsync` は通貨の減算 `UPDATE players SET gacha_currency = gacha_currency - @cost WHERE player_id = @id AND gacha_currency >= @cost`(更新が 0 行なら `GACHA_NOT_ENOUGH_CURRENCY`)、`ExchangeAsync` は `SELECT ... FROM players WHERE player_id = @id FOR UPDATE`。この行ロックで同じプレイヤーの要求が直列になるので、重複の判定([gacha.md](../../systems/gacha.md) 5章)はその後に読めば正しい。抽選・所持品・履歴・冪等性の保存済みレスポンスは同じトランザクションで書く |
| 所持品の書き込み | [gacha.md](../../systems/gacha.md) 5章の処理が書くのは次の 4 種だけ: 育成アイドルの行の追加(トレーニング 0・覚醒 0・かけら 0)、かけらの加算、サポートカードの行の追加(所持ID は ULID、レベル 1、上限解放 0、入手日時)、`players.support_points` の加算。列名と型はサーバーの実装計画の DDL で決める |
| `ExpectedCost` | 画面に出した価格と違う額を引かれないための保険(マスタの版はアプリのバージョンと同じなので、食い違うのはクライアントの不具合のとき) |

サーバーはマスタを Infrastructure の `MemoryDatabase` から読む([repository-layout.md](../repository-layout.md) 4章)。`server/` と `masterdata/` はまだない(作るのはそれぞれの最初の実装計画)。

`DrawAsync`(1 つのトランザクション):

1. `Count` が 1 か 10 でなければ `GACHA_INVALID_COUNT`
2. `GachaId` がマスタになければ `GACHA_NOT_FOUND`
3. 価格を決める(`Count` が 1 なら `SingleCost`、10 なら `TenCost`)。`ExpectedCost` と違えば `GACHA_COST_MISMATCH`
4. 通貨を減らす(下の「同時実行」の SQL に `RETURNING gacha_currency` を付け、残りを受け取る)。0 行なら `GACHA_NOT_ENOUGH_CURRENCY`
5. シードを作って乱数を用意する(1章)
6. 枠ごとに(1 から `Count` まで)、抽選する(10 連の 10 枠目は確定枠)→ [gacha.md](../../systems/gacha.md) 5章の処理 → 履歴に 1 行。完凸の判定は枠ごとに `EXISTS` で読んでよい(同じ要求の中で完凸は増えないので、まとめて読んでも結果は同じ。読み方は実装に任せる)
7. 交換pt を `Count` だけ足す(`gacha_exchange_points` に行がなければ作る。`INSERT ... ON CONFLICT (player_id, gacha_id) DO UPDATE SET points = points + @n RETURNING points` で今の値を受け取る)
8. Response を作る([api-design.md](../api-design.md) 1.1)

`ExchangeAsync`(1 つのトランザクション):

1. `GachaId` がマスタになければ、または `CeilingItemOrder` が `GachaCeilingItem` になければ `GACHA_NOT_FOUND`
2. `players` の行をロックする(下の「同時実行」の SQL)
3. 交換pt を減らす: `UPDATE gacha_exchange_points SET points = points - @c WHERE player_id = @id AND gacha_id = @g AND points >= @c`。0 行(行がない場合を含む)なら `GACHA_NOT_ENOUGH_POINTS`
4. [gacha.md](../../systems/gacha.md) 5章の処理 → 履歴に 1 行([gacha.md](../../systems/gacha.md) 4章)
5. Response を作る(`RETURNING points` で受けた残りの交換pt。結果の `Slot` は 0)

`GetGachasAsync`: マスタの `Gacha` を `GachaId` の昇順にすべて返す。交換pt の行がないガチャは 0 を返す。

## 3. サポートカードの強化の処理の手順 [決定。2026-10-31]

すべての RPC は、トランザクションの最初に `players` の行をロックする([api-design.md](../api-design.md) 3章)。

`LevelUpAsync`(1 つのトランザクション):

1. 所持IDが自分の所持でなければ `SUPPORT_NOT_OWNED`
2. 目標が今のレベル以下、または上限解放で決まる上限を超えれば `SUPPORT_INVALID_TARGET_LEVEL`
3. 必要量(強化pt・ゴールド)を `SupportLevelCost` の行から合計する
4. `UPDATE players SET support_points = support_points - @p, gold = gold - @g WHERE player_id = @id AND support_points >= @p AND gold >= @g` の 1 文で払う。0 行なら残高を読み直し、強化pt が足りなければ `SUPPORT_NOT_ENOUGH_POINTS`、そうでなければ `PLAYER_NOT_ENOUGH_GOLD`(強化pt を先に見る)
5. 所持IDの行のレベルを目標にする

`LimitBreakAsync`(1 つのトランザクション。検証はこの順):

1. 解放するカードが自分の所持でなければ `SUPPORT_NOT_OWNED`
2. 素材が解放するカードと同じ所持ID なら `SUPPORT_INVALID_MATERIAL`
3. 素材を消す: `DELETE FROM player_support_cards WHERE support_card_uid = @m AND player_id = @id AND card_id = @c`。0 行(無い・他人の・別のカード)なら `SUPPORT_INVALID_MATERIAL`
4. 解放する: `UPDATE player_support_cards SET limit_break = limit_break + 1 WHERE support_card_uid = @t AND player_id = @id AND limit_break < 4`。0 行なら `SUPPORT_MAX_LIMIT_BREAK`(トランザクションごと取り消すので、3 の削除も戻る)

- 同時に同じ素材を使う要求が来ても、3・4 が 1 文の条件付きの更新なので、片方が 0 行になって失敗する。再送は `x-request-id` で冪等に扱う(VolTech-MagicOnion)

## 未決事項

- `request_id` に入れる `x-request-id` をハンドラーで読む手段([gacha.md](../../systems/gacha.md) の未決事項)

## 実装メモ

(実装後に設計とのズレをここに記録する)
