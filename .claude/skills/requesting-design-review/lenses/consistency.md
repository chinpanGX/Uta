# 整合性のチェックリスト

対象のページが、決まっていること・ほかのページ・既にあるコードと食い違っていないかを見る。wiki の書式もここで見る。
根拠: CLAUDE.md、[ADR](../../../../uta_docs/wiki/decisions/)（特に [0001](../../../../uta_docs/wiki/decisions/0001-ip-policy.md)・[0007](../../../../uta_docs/wiki/decisions/0007-masterdata-with-addons.md)・[0008](../../../../uta_docs/wiki/decisions/0008-shared-domain-and-onion.md)）、[glossary.md](../../../../uta_docs/wiki/glossary.md)、[index.md](../../../../uta_docs/wiki/index.md)、[repository-layout.md](../../../../uta_docs/wiki/architecture/repository-layout.md) 10章。
egm の codebase_details・structural・style の観点を Uta 向けにまとめたもの。

**矛盾は、どちらが正しいかを決めずに両方の場所を挙げる。** 正は ADR > CLAUDE.md > 仕様ページの順だが、仕様ページ同士の食い違いはユーザーが決める。

## IP 方針（ADR 0001。最優先）

- [ ] キャラクター・名称・楽曲・UI 素材・ロゴ・世界観・シナリオが、既存作品のものになっていないか。参考作品の名称や固有の表現が残っていないか
- [ ] 参考にした構造の出典を書くとき、作品名・URL を残していないか（[raw の調査メモ](../../../../uta_docs/raw/2026-10-26-reference-structure-research.md) の扱いにそろえる）

## ADR・CLAUDE.md との一致

- [ ] ADR で決めた技術・方針（MagicOnion、PostgreSQL、Cloud Run、Unity 7、uGUI + UnityScreenNavigator・VContainer・UniTask など）と違うものを前提にしていないか
- [ ] **サーバー権威**: 育成の計算結果・ガチャ抽選・所持品の更新を、クライアントで確定させる書き方になっていないか
- [ ] クライアントの役割が「表示・入力・Shared.Domain での使用可否の判定と予測の表示」に収まっているか。**クライアントにシードを持たせていないか**（ADR 0008）
- [ ] マスタの列の型が `int` / `long` / `string` / `bool` / enum だけか。Shared.Domain の値に `float` / `double` を使う前提になっていないか（小数は万分率などの整数）
- [ ] ID（マスタは `int`、サーバー発行は ULID）、時刻（サーバーが正、JST 固定、日付の更新は 12 時）、乱数（`xoshiro256**`）の決まりに沿っているか
- [ ] iOS（IL2CPP / AOT）で困る前提（リフレクション、実行時のコード生成）を置いていないか
- [ ] 層の置き場所が repository-layout.md の依存ルールに沿っているか（マスタ型を `shared/` に置かない、など）
- [ ] 保留にしたもの（3D ライブ。ADR 0003）を、MVP の範囲として書いていないか

## ほかのページとの一致

- [ ] 同じもの（数値、状態、処理の順、画面、API）について、関係するページと違うことを書いていないか
- [ ] 対象のページで決めたことが、ほかのページの「未決事項」に残ったままになっていないか（逆も）
- [ ] リンク先のページ・節が実在するか。リンクは相対パスか
- [ ] 新しいページなら、そのフォルダの `README.md` に登録されているか（実装計画は `uta_docs/plans/` に置き、index には登録しない）
- [ ] 実装計画なら、frontmatter の `spec` の仕様ページと食い違っていないか。仕様にない機能を足していないか

## 用語

- [ ] [glossary.md](../../../../uta_docs/wiki/glossary.md) の用語を使っているか。同じものを別の名前で呼んでいないか
- [ ] 新しい用語を使うなら、glossary に足す必要があると指摘しているか（足されていなければ指摘する）
- [ ] 仮称が仮称と分かるか

## 既にあるコードとの一致（関係するコードがあるときだけ）

- [ ] 「今こうなっている」と書いたことが、コードと一致しているか。型名・メソッド名・ファイルの場所・値
- [ ] 存在しないファイル・型・コマンドを、今あるものとして書いていないか（CLAUDE.md「存在しないビルドコマンドやファイルを、あるものとして書かない」）
- [ ] これから作るものは、作る予定と分かるように書かれているか（作る予定のものが無いのは指摘しない）
- [ ] コードにある大事な決めごと（定数、検証、境界の扱い）が文書から抜けていないか

## wiki の書式

- [ ] 先頭に frontmatter（`title` / `status` / `updated`）があるか。`status` が中身に合っているか（wiki は draft / stable / deprecated / deferred、実装計画は [plans/README.md](../../../../uta_docs/plans/README.md) の draft / ready / in-progress / done）
- [ ] 「決まっていること」と「未決事項」が分かれているか
- [ ] 推測が推測と明記されているか。凡例（**[確認]** / **[決定]** / **[Uta案]** など）を使うページでは、それが付いているか
- [ ] 返答・文書が日本語か
