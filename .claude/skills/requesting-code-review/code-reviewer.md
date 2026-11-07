# レビュー担当への依頼文の雛形

レビュー担当（`.claude/agents/uta-code-reviewer.md`）を出すときに、Agent ツールの `prompt` に渡す。レイヤーごとに 1 つずつ出し、同じメッセージで並べる。

役割・見ること・重さの付け方・出力の形式・モデル・使えるツールはエージェントの定義にあるので、依頼文には**何を見るか**だけを書く。会話の経緯は書かない。

```
Agent:
  subagent_type: uta-code-reviewer
  description: "[LAYER] のコードレビュー"
  prompt: |
    ## レイヤー

    [LAYER]

    ## チェックリスト

    [CHECKLIST]

    ## 作ったもの

    [DESCRIPTION]

    ## 仕様・計画

    [PLAN_OR_REQUIREMENTS]

    ## レビューする範囲

    **Base:** [BASE_SHA]
    **Head:** [HEAD_SHA]

    このレイヤーのファイル:
    [FILES]

    同じ変更でほかのレイヤーに入ったファイル:
    [OTHER_LAYERS]

    ```bash
    git diff --stat [BASE_SHA]..[HEAD_SHA] -- [FILES]
    git diff [BASE_SHA]..[HEAD_SHA] -- [FILES]
    ```

    foundation（共通基盤）の場合は、範囲は共通基盤のリポジトリのコミットです。
    別の clone で `git diff [BASE_SHA]..[HEAD_SHA]` を見てください。

    ## 特に見てほしいこと

    [FOCUS]

    ## 出力先

    [OUTPUT]
```

`model`・`effort` は渡さない（エージェントの定義の値を使う）。ユーザーが別の指定をしたときだけ渡す。

**埋めるもの:**
- `[LAYER]`: client / server / shared / foundation
- `[CHECKLIST]`: `.claude/skills/requesting-code-review/layers/<レイヤー>.md` のパス
- `[DESCRIPTION]`: 何を作ったかの短い要約
- `[PLAN_OR_REQUIREMENTS]`: 実装計画・wiki の仕様ページ・ADR のパス、タスクの文
- `[BASE_SHA]` / `[HEAD_SHA]`: 範囲（foundation は共通基盤のリポジトリの範囲）
- `[FILES]`: このレイヤーのファイル
- `[OTHER_LAYERS]`: ほかのレイヤーのファイル（なければ「なし」）
- `[FOCUS]`: 特に気にしていること（なければ「なし」）。2 回目以降は、直した箇所をここに挙げる。経緯は書かない
- `[OUTPUT]`: 報告の全文を書くファイル。`uta_docs/reviews/.raw/YYYY-MM-DD-<件名>/code-<レイヤー>-<回>.md`（例: `.../2026-11-08-gacha/code-server-1.md`）。`.raw/` は Git に入れない（`.gitignore`）

**レビュー担当が返すもの:** 最後の返答は要約（判定・件数・Critical と Important の 1 行ずつ・確認できなかったこと）だけ。報告の全文（良い点、指摘、ADR の見直しの提案、判断しなかったこと、確認できなかったこと、判定）は `[OUTPUT]` のファイルにある
