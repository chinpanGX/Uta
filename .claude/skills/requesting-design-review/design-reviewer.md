# Goldfish への依頼文の雛形

Goldfish（`.claude/agents/uta-goldfish-*.md`）を出すときに、Agent ツールの `prompt` に渡す。観点ごとに 1 つずつ出し、同じメッセージで並べる。

役割・守ること・出力の形式・モデル・使えるツールはエージェントの定義と [goldfish-rules.md](goldfish-rules.md) にあるので、依頼文には**何を見るか**だけを書く。会話の経緯は書かない。

```
Agent:
  subagent_type: uta-goldfish-consistency | uta-goldfish-readiness | uta-goldfish-critique
  description: "設計書のレビュー（[観点]）"
  prompt: |
    ## 対象

    [TARGET]

    [PURPOSE]

    ## 関係するページ

    [CONTEXT_PAGES]

    ## 関係するコード

    [CODE]

    ## 特に見てほしいこと

    [FOCUS]
```

`model`・`effort` は渡さない（エージェントの定義の値を使う）。ユーザーが別の指定をしたときだけ渡す。

**埋めるもの:**
- `[TARGET]`: 対象のページのパス
- `[PURPOSE]`: ページの目的と段階（1〜2 文）。実装計画なら「実装計画」と明記する
- `[CONTEXT_PAGES]`: 関係する wiki のページと ADR のパス
- `[CODE]`: 関係するコードのフォルダ（なければ「なし」）
- `[FOCUS]`: ユーザーが特に気にしていること（なければ「なし」）

**Goldfish が返すもの:** 良い点、指摘（Critical / Important / Minor）、未決事項に加える候補、ADR の見直しの提案、判断しなかったこと、判定
