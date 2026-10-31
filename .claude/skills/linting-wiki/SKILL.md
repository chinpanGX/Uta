---
name: linting-wiki
description: Uta の wiki・CLAUDE.md・スキルを機械的に点検する（CLAUDE.md の wiki の運用「Lint」）。リンク切れ、ADR の番号の食い違い・重複、index.md への登録漏れ、孤立ページ、frontmatter の誤り、書かれたパスの不在を、スクリプトで調べて直す。wiki のページを足した・消した・名前を変えたとき、ADR を足したとき、ブランチをマージした後、設計書のレビュー（requesting-design-review）の前、「lint して」「wiki を点検して」と言われたときに使う。
---

# wiki を点検する（Lint の機械的な部分）

CLAUDE.md の「wiki の運用」の 3 つ目、**Lint** のうち、読まなくても調べられるものをスクリプトで点検する。LLM はページを読まない。結果の一覧だけを見て直す。

ページ同士の矛盾・古い記述のように、読んで判断が要るものはここでは見ない。それは [requesting-design-review](../requesting-design-review/SKILL.md) の整合性の観点（`uta-goldfish-consistency`）で見る。設計書のレビューの前にこの点検を通しておくと、リンク切れのような機械的な指摘が Goldfish の結果に混ざらない。

## 手順

### 1. 実行する

リポジトリのルートで:

```bash
python -I .claude/skills/linting-wiki/lint_wiki.py
```

- エラーがあれば終了コード 1、なければ 0。
- 点検するもの: `uta_docs/` の全 `.md`、`CLAUDE.md`、`README.md`、`.claude/skills/`・`.claude/agents/` の `.md`（`.claude/worktrees/` は除く）。

### 2. 結果を読む

| 点検 | 重さ | 何を見るか |
|---|---|---|
| リンク切れ | エラー | 相対リンクの行き先が実在するか。`uta_docs/raw/` の中のリンク切れは警告（raw は書き換えないため） |
| ADR の番号の食い違い | エラー | リンクの文字（`ADR 0014` など）の番号と、行き先のファイルの番号が合うか。マージで ADR の番号を振り直したときに出やすい |
| frontmatter | エラー | `title` / `status` / `updated` があるか。`status` がその種類で使える値か（wiki: draft / stable / deprecated / deferred、ADR: proposed / accepted / superseded、実装計画: draft / ready / in-progress / done）。`updated` が日付か。実装計画に `spec` があるか |
| index に未登録 | エラー | `uta_docs/wiki/` のページが `index.md` からリンクされているか |
| ADR | エラー | ファイル名の形、番号の重複、title の番号、`index.md` に書いた status と本体の status、本文の「ADR NNNN」が実在する番号か |
| 孤立ページ | 警告 | `index.md` 以外のどのページからもリンクされていないか |
| 書かれたパスがない | 警告 | CLAUDE.md・スキル・エージェントにコード書式で書いたパス（`shared/...` など）が実在するか。`.gitignore` で除外したものは見ない |
| 確かめられなかったリンク | 警告 | 行き先が `external/` のサブモジュールの中で、サブモジュールが未初期化 |

### 3. 直す

- **エラーは直す。**リンクの行き先を直す、`index.md` に登録する、frontmatter を直す。
- **ADR の本体（accepted のもの）は書き換えない**（CLAUDE.md）。ADR の中のリンク切れは、リンクの行き先だけを直すのにとどめ、中身に触れるならユーザーに確認する。
- **`uta_docs/raw/` は書き換えない。**raw の中のリンク切れは警告のまま残す。
- **警告は 1 件ずつ確かめる。**
  - 孤立ページ: 関係するページからリンクを張るか、そのままでよいかを決める
  - 書かれたパスがない: これから作るもの（`masterdata/` など）なら、そう分かる書き方になっているか確かめる。もう無いものなら書き直す
  - 確かめられなかったリンク: `git submodule update --init` してから点検し直す
- 直すべきか迷うもの（どちらのページが正しいか分からない、など）は、勝手に決めずにユーザーに確認する。
- wiki を直したら、直したページの `updated` を更新し、`log.md` に `lint` の種別で 1 行追記する。

直したら、エラーが 0 件になるまで 1 を繰り返す。

## してはいけないこと

- エラーを残したまま「点検した」と報告する
- 点検のためにページを全部読み込む（スクリプトの結果だけで直せる）
- 警告を黙って捨てる。残すなら、残す理由をユーザーに伝える
