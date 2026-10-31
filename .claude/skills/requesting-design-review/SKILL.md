---
name: requesting-design-review
description: Uta の設計書（uta_docs/wiki の systems/・architecture/・overview/ のページ、uta_docs/plans の実装計画、uta_docs/raw の設計の下書き、ADR の案）をレビューに出すときに使う。会話の経緯を持たないレビュー担当（Goldfish。.claude/agents/uta-goldfish-*）を、観点（整合性・実装可能性・設計の批評）ごとに並べて出し、ADR・CLAUDE.md・ほかの wiki ページに照らして見てもらう。仕様ページや実装計画を書いた・大きく直したとき、実装計画を ready にする前、「設計書をレビューして」「仕様をレビューに出して」と言われたときに使う。コードの変更のレビューには requesting-code-review を使う。
---

# 設計書をレビューに出す（Uta 版）

元は [google/elephant-goldfish-model](https://github.com/google/elephant-goldfish-model) v0.1.0 の `egm-review` スキルとレビュー担当のエージェント（Apache-2.0、[LICENSE-egm](LICENSE-egm)）。Uta 向けに次のように書き換えた:

- 5 段階のフェーズ・プリセット選び・文書の分割・チェックポイント・まとめ役のエージェント（synthesizer）をやめ、観点ごとのレビュー担当を 1 回並べて出すだけにした
- 7 つの観点を 3 つにまとめた（整合性 ← codebase_details・structural・style、実装可能性 ← implementation_readiness・comprehension、設計の批評 ← design_critic・zoom_out）
- 基準を egm の文書標準から、CLAUDE.md・ADR・wiki の書式に置き換えた。実装計画の基準には egm の Implementation Readiness Standard（Zero-Decision Rule）を、既にあるコードを説明するページには Re-Implementation Standard を残した
- 進め方を [requesting-code-review](../requesting-code-review/SKILL.md) にそろえた
- Goldfish は egm と同じく専用のエージェントにした（`.claude/agents/uta-goldfish-*.md`）。ツールを Read・Grep・Glob に絞って読み取り専用を保証し、観点ごとにモデルを固定した。egm と違い、CLAUDE.md は読ませる（`omitClaudeMd` を使わない）

レビュー担当（Goldfish）には、**このセッションの経緯を渡さず、組み立てた文脈（読むファイルのパス）だけを渡す**。レビュー担当が見るのは書かれた文書であって、書いた過程ではない。

**原則**: 進行役（あなた）は対象のページを自分で読み込まない。読むのはレビュー担当で、あなたは指摘だけを受け取る。

## いつ依頼するか

**必ず:**
- 仕様ページ（`systems/`・`architecture/`）を新しく書いたとき、大きく書き直したとき
- そのページをもとに実装計画を書く前
- **実装計画（`uta_docs/plans/`）を `status: ready` にする前**（[ADR 0014](../../../uta_docs/wiki/decisions/0014-design-heavy-sonnet-implementation.md)。実装を Sonnet に渡せるかの関門）
- ADR を proposed で起票したとき

**あると良い:**
- 未決事項を決めて反映したとき
- `raw/` に設計の下書きを置き、wiki に反映する前

## 手順

### 1. 対象を決める

- 対象は **1 回に 1 ページ**。複数のページを見たいときは、ページごとに分けて依頼する（関係するページは文脈として渡す）。
- 対象のページの段階を確かめる（frontmatter の `status`、叩き台か、実装直前か）。`head` で frontmatter だけ見れば足りる。

### 2. 文脈を組み立てる

対象のページを全部読まずに、パスを集める。

```bash
head -6 <対象>                                     # frontmatter（status・updated）
grep -oE '\]\([^)]+\.md[^)]*\)' <対象> | sort -u   # 対象がリンクしているページ
grep -rl '<対象のファイル名>' uta_docs/wiki         # 対象にリンクしているページ
```

- **関係するページ**: 上のリンクと、`uta_docs/wiki/index.md` の一行要約から選ぶ。多すぎるときは、対象が直接リンクしているものと ADR を優先する。
- **実装計画のとき**: frontmatter の `spec` に挙がった仕様ページを必ず入れる。計画が触る既存のファイルのフォルダを「関係するコード」に入れる
- **関係するコード**: 対象が既にあるコードを説明しているなら（例: `lesson-domain-types.md` と `shared/Uta.Domain/`）、そのフォルダ。なければ「なし」。

### 3. 観点を選ぶ

| 観点 | チェックリスト | 何を見るか |
|---|---|---|
| 整合性 | [lenses/consistency.md](lenses/consistency.md) | ADR・CLAUDE.md・ほかのページ・用語集・コードとの矛盾、IP 方針、wiki の書式 |
| 実装可能性 | [lenses/readiness.md](lenses/readiness.md) | このページだけで、設計判断をせずに実装を始められるか |
| 設計の批評 | [lenses/critique.md](lenses/critique.md) | 根拠のない前提、リスク、過剰な設計、もっと単純にできないか |

- **既定は 3 つとも**。同じメッセージで並べて出す（並行して動く）。
- 段階で絞ってよい:
  - 構想段階（`raw/` の concept、中身がまだ箇条書きの叩き台）: 整合性と設計の批評だけ
  - 用語や書式だけ直した小さな変更: 整合性だけ
  - ADR の案: 整合性と設計の批評だけ
  - **実装計画: 3 つとも。実装可能性は省かない**（これが Sonnet に渡せるかの判定になる）
- ユーザーが観点を指定したら、それに従う。

### 4. 観点ごとに Goldfish を出す

観点ごとに、Uta 専用の Goldfish（`.claude/agents/`）を 1 つずつ出す。依頼文は [design-reviewer.md](design-reviewer.md) の雛形を埋めて作る。

| 観点 | subagent_type | model | effort | 理由 |
|---|---|---|---|---|
| 整合性 | `uta-goldfish-consistency` | `opus` | `high` | ADR とほかのページを何枚も突き合わせる。見落としが後で一番高くつく |
| 実装可能性 | `uta-goldfish-readiness` | `sonnet` | `medium` | **実装を担当するモデルで読ませる。** Sonnet が質問せずに書けると判断できなければ、設計書が足りていない（[ADR 0014](../../../uta_docs/wiki/decisions/0014-design-heavy-sonnet-implementation.md)） |
| 設計の批評 | `uta-goldfish-critique` | `fable` | `high` | 前提を疑い、代わりの案を出す判断が中心。ここに一番強いモデルを使う |

- モデル・エフォート・使えるツール（Read・Grep・Glob だけ。読み取り専用）はエージェントの定義で決まっている。Agent ツールに `model`・`effort` は渡さない。ユーザーが別の指定をしたときだけ渡す。
- 役割・共通のルール・出力の形式は、Goldfish が自分で [goldfish-rules.md](goldfish-rules.md) とその観点の `lenses/` を読む。依頼文には何を見るかだけを書く。
- `uta-goldfish-*` が使えるエージェントの一覧にないとき（`.claude/agents/` を足した直後のセッションなど）は、セッションを開き直すようユーザーに頼む。`general-purpose` で代わりに出さない（読み取り専用とモデルの指定が外れる）。

### 5. 指摘に対応する

観点ごとの結果を、重複をまとめてから Critical → Important → Minor の順にユーザーに見せる。同じ箇所への指摘が観点をまたいで出たら 1 つにまとめる。

- **Critical** は直ちに直す
- **Important** は実装に入る前に直す
- **Minor** は後でよいものとして控える
- レビュー担当が間違っていると思うなら、理由を添えて反論する（ADR・ほかのページの該当箇所で示す）
- **未決事項を決めることになる指摘**は、勝手に決めない。選択肢を添えてユーザーに確認する（CLAUDE.md「勝手に決めない」）
- **ADR・CLAUDE.md の方針そのものへの反対**は、勝手に直さず、ADR を見直す提案としてユーザーに確認する
- 「判断しなかったこと」の一覧は、1 行ずつ扱いを決める。黙って捨てない
- 直したら、frontmatter の `updated` を更新し、`log.md` に 1 行追記する。ページを足したら `index.md` も更新する
- 実装計画は、実装可能性の判定が「はい」になり、Critical・Important と計画の「未決事項」が残っていなければ `status: ready` にする

直したあとにもう一度見てもらうなら、**Critical か Important が出た観点だけ**出し直す。2 回目でも同じ指摘が残るなら、3 回目を出す前にユーザーに相談する。

レビューの結果は、ふだんはファイルに残さない。残したいとユーザーが言ったときだけ、`uta_docs/reviews/YYYY-MM-DD-<件名>/` に置く。ページごとに 1 ファイル（重複をまとめた指摘）、ページをまたぐ矛盾は `_cross-page.md`、目次と判定・件数の一覧は `README.md` にする。

## 例

架空の例。指摘の中身は説明のためのもので、実際の gacha.md の評価ではない。

```
[systems/gacha.md の天井と交換pt の節を書き直した。次はサーバーの抽選を実装する]

あなた: 実装の前に設計書のレビューに出します。

head -6 uta_docs/wiki/systems/gacha.md → status: draft
リンク先: idol-support.md、api-design.md、masterdata-pipeline.md、ADR 0001・0007・0008・0010
→ 実装直前なので 3 つとも並べて出す

[subagent_type: uta-goldfish-consistency]
  TARGET: uta_docs/wiki/systems/gacha.md
  PURPOSE: ガチャの仕様。天井と交換pt を書き直した。次にサーバーの抽選を実装する
  CONTEXT_PAGES: systems/idol-support.md、architecture/api-design.md、architecture/masterdata-pipeline.md、
                 decisions/0001・0007・0008・0010、glossary.md
  CODE: なし
  FOCUS: 交換pt の持ち越しの扱い

[結果]
  整合性: Important 1 件（排出率の列が float 前提。ADR 0007 は int のみ）→ 万分率に直す
  実装可能性: Important 1 件（10 連の途中で天井に達したときの扱いが書かれていない）→ 未決事項なのでユーザーに確認
  設計の批評: Minor 1 件
```

## よくある言い訳

| 言い訳 | 実際 |
|---|---|
| 「レビュー担当を出さず、自分でページを読めばいい」 | あなたは進行役。ページと関係するページを自分で読むと、直す作業に必要なコンテキストを使い切る。読むのはレビュー担当に任せ、指摘だけを受け取る |
| 「レビュー担当には、このセッションの経緯が全部必要」 | 組み立てた文脈だけを渡す。経緯を渡すと、書かれた文書ではなく考えた過程を見てしまい、文書に書き漏れたことに気づけない |
| 「観点をまとめて 1 人で見てもらえば足りる」 | 矛盾を探す目と、実装できるかを見る目と、もっと単純にできないかを見る目は違う。チェックリストを 1 つに絞った方が見落としが減る |
| 「egm-review の全フェーズを回した方が丁寧」 | 1 ページの仕様には重すぎる。3 つの観点で足りないと分かったら、そのとき観点を足す |

## してはいけないこと

- 「叩き台だから」と整合性のレビューを飛ばす（ADR・IP 方針との矛盾は早いほど直しやすい）
- Critical を無視する
- 未決事項を、指摘に従って勝手に決める
- 正しい指摘に言い返す
- 対象のページを全部読み込んでからレビュー担当を出す

雛形: [design-reviewer.md](design-reviewer.md)　共通のルール: [goldfish-rules.md](goldfish-rules.md)　エージェント: `.claude/agents/uta-goldfish-*.md`
