# プレイヤー Lv を経験値の累計から計算する: 反映のときに決めたこと（2026-11-09）

[2026-11-09-player-level-from-exp.md](2026-11-09-player-level-from-exp.md) の 3 章の論点を、wiki に反映するときに決めた。Claude が案と推奨を出し、ユーザーが選んだ。

## ユーザーの判断

- マスタの定義の形: `PlayerLevelExp` を「Lv ごとの経験値の累計の範囲（From / To）」の形にする（Claude の推奨は「`Level, TotalExp`（その Lv に届く累計）の表に置き換える」。ユーザーは From / To の形を選んだ）
- DTO: 経験値の累計だけを送る。クライアントが Lv を計算する（Claude の推奨は「Lv と累計の両方を送り、DTO の形を変えない」）
- `players` のマイグレーション: 既存のデータを捨てる（`level` の列を消し、`exp` を 0 にする。Claude の推奨は換算の式で累計に直すこと）

## 設計書のレビューのあとのユーザーの判断（同じ日。Claude の推奨どおり）

- Lv の計算は Shared.Domain の不変の表の型 `PlayerLevelCatalog`（`Create` で前提を検査し、各側の Factory が 1 度だけ作ってシングルトンにする）。static 関数に昇順の列を渡す形は採らない（前提が関数の外にあり、同じ導出を 3 か所が書くため）
- 経験値の付与（累計に足して上限で止める）はサーバーの Domain（`PlayerExpRules`）。報酬の付与はサーバーだけが行うため
- `RuleVersion` を 1 → 2 に上げる（repository-layout.md 10.5 の規則どおり）
- ホームの Lv の表示は、ホームの上部のステータスを作る別の計画で作る

## Claude が決めたこと（ユーザーの判断の範囲の細部）

- From / To は両端を含む。最大 Lv の行は `FromExp` = `ToExp` で、この値が累計の上限
- MessagePack のキーは、消した項目の後ろを詰める（リリース前）
- 2 回目のレビューの指摘で、型の名前を naming.md の「定義の集まり」に合わせて `PlayerLevelCatalog` にし、`Create` の引数 `maxExp`（最後の `FromExp` と同じでなければ例外で、情報を持たない）を消した。`PlayerExpRules` はサーバーの `Domain/Player/` に置く

## 確かめたこと

- draft の換算の式 `exp + 20 × level × (level − 1)` は、今の `masterdata/csv/playerLevelExp.csv`（Lv L → L+1 に `40 × L`、99 行）と合う（Lv L に届く累計 = Σ_{k=1}^{L−1} 40k = 20 × L × (L − 1)。Lv 100 = 198,000）。データを捨てるので使わない
