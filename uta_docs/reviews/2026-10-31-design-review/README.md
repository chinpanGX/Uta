# 設計書レビュー（2026-10-31）

main ブランチ（`e365ac0`）の設計書を、`requesting-design-review` スキルでレビューした結果。観点は 3 つ（整合性・実装可能性・設計の批評）で、ページごとに会話の経緯を持たないレビュー担当（`uta-goldfish-*`）を並べて出し、重複をまとめた。

- このレビューではページを直していない。直すかどうか・どう直すかはこれから決める
- 指摘の重さ: **Critical** は直ちに直す、**Important** は実装に入る前に直す、**Minor** は後でよい
- 各ファイルの「未決事項に加える候補」は、レビュー担当が挙げた選択肢。決めたものではない

## 進み具合

12 ページのうち 6 ページが済み。土台のページから順に見ている。

| # | ページ | 判定 | Critical | Important | Minor | 結果 |
|---|---|---|---|---|---|---|
| 1 | overview/game-spec.md | 直せば | 0 | 6 | 9 | [game-spec.md](game-spec.md) |
| 2 | systems/gacha.md | 直せば（実装可能性は「いいえ」） | 0 | 12 | 8 | [gacha.md](gacha.md) |
| 3 | systems/idol-support.md | 直せば | 2 | 17 | 12 | [idol-support.md](idol-support.md) |
| 4 | systems/produce.md | 直せば | 2 | 20 | 16 | [produce.md](produce.md) |
| 5 | systems/lesson-card.md | 直せば | 2 | 15 | 11 | [lesson-card.md](lesson-card.md) |
| 6 | architecture/repository-layout.md | 直せば | 1 | 16 | 17 | [repository-layout.md](repository-layout.md) |
| 7 | architecture/masterdata-pipeline.md | 未 | | | | |
| 8 | architecture/api-design.md | 未 | | | | |
| 9 | architecture/server-foundation.md | 未 | | | | |
| 10 | architecture/lesson-domain-types.md | 未 | | | | |
| 11 | architecture/screen-design.md | 未 | | | | |
| 12 | overview/screen-wireframes.md | 未 | | | | |

ページをまたぐ矛盾（73 件）: [_cross-page.md](_cross-page.md)

対象から外したもの: systems/live.md（保留。ADR 0003）、index.md・log.md・glossary.md（整合性の観点の中で参照）、decisions/ の ADR（accepted のため見直さない）。

## Critical の一覧（ここまで）

| ページ | # | 要点 |
|---|---|---|
| idol-support.md | C1 | サポートカードのレベル上げに使うものが食い違う（2.2 は強化pt、produce.md 6.1 はゴールド。どちらも確定扱い） |
| idol-support.md | C2 | サポートカードの重複を自動で上限解放するか手動か未定。手動なら要る「余った枚数」の列が `player_support_cards` にない |
| produce.md | C1 | 試験で不合格になったときの局面の遷移・評価・報酬・経験値・`produce_results` の終わり方が決まっていない |
| produce.md | C2 | 10.4 の操作（相談・カードの選択肢・ドリンクを捨てる・再挑戦）に受け口の RPC がない。API案に `AbandonAsync` もない |
| lesson-card.md | C1 | 効果の enum の名前・値・列の対応が、masterdata-pipeline.md 3.1（正とされている）と実装済みのコードで別物 |
| lesson-card.md | C2 | 持続効果に付けた条件の意味が、pipeline・lesson-domain-types・コードで 3 通り |
| repository-layout.md | C1 | `noEngineReferences: true` を [決定] としているが、accepted の ADR 0008・0009 は「使わない」 |

## ページをまたいで繰り返し出た論点

どれも複数のページのレビューで別々に指摘された。1 つ決めれば複数のページの指摘が片づく。

- **MVP の範囲**: game-spec.md に上限がなく、systems/ の「必須」の合計（3 難易度、強化、天井・交換メダル、レンタル枠など）がそのまま MVP になっている
- **強化の資源**: 育成素材・サポート強化pt に入手手段も保存場所もない。レベル上げの通貨も食い違う
- **初期の所持品**: 「ガチャ 1 回 → 編成」で自分のサポートカード 5 枚が埋まらない。idol-support.md 1.8 の初期所持は [確認] のまま採否がない
- **かけらの単位**: 育成アイドルが人物かカード（アイドル × 楽曲）かが曖昧で、かけら・覚醒・信頼度・テーブルのキーが決まらない
- **信頼度**: スコアの式（必須）に入っているのに、保存する場所も開始時に写す場所もない
- **整数の単位**: 8.5%（idol-support.md）、0.225（produce.md の千分率）が、glossary・masterdata-pipeline.md の「確率は万分率、倍率はパーセント」に収まらない
- **乱数を引く回数**: 特別レッスン（パラメータ別か 1 回か）、サポートイベント（カードごとかイベントごとか）、レンタルの編成順が決まらず、再現性の前提に響く
- **審査比率とターン割り振り**: Uta 独自の比率（idol-support.md の [決定]）に対し、produce.md 5.1 は参考作品の 4 型の表しかない
- **数値型**: lesson-card.md 9.6 が [Uta案] のままで、ほかのページの根拠として参照されている（実際は整数の固定小数点に決定済み）

## ADR の見直しの提案（ここまで）

- **ADR 0001**（lesson-card.md）: 参考作品のカードの数値を仮の値として使ってよいか、公開前に差し替えが必須かを明記する
- **ADR 0008 / 0009**（repository-layout.md）: `noEngineReferences` の扱い。新しい ADR で置き換えるか、ページを ADR に戻すか
- **ADR 0008**（repository-layout.md）: 表示専用のデータは DTO をそのまま使えるように、「`Application` は `Uta.Shared` を参照しない」の範囲を絞る

## まだ作っていないもの

- 残り 6 ページのレビュー
- 全体のまとめ（ページごとの判定、Critical の一覧、未決事項に加える候補、ADR の見直しの提案、直す順番の提案）。全ページを終えてから作る
