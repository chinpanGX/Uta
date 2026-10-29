---
name: requesting-code-review
description: Uta でコードの変更をレビューに出すときに使う。変更のパスからレイヤー（client / server / shared / foundation）を判別し、レイヤーごとに会話の経緯を持たないレビュー担当のサブエージェントを出して、そのレイヤーの規約で見てもらう。タスクを終えたとき、大きな機能を実装したとき、main にマージする前、「レビューして」「レビューに出して」と言われたときに使う。
---

# コードレビューを依頼する（Uta 版）

元は [obra/superpowers の requesting-code-review](https://github.com/obra/superpowers/tree/2ce8aa15d05bd57213e6f746f953aafb7ded7a8b/skills/requesting-code-review)（MIT、[LICENSE-superpowers](LICENSE-superpowers)）。Uta 向けに、レイヤーごとのチェックリストと、wiki・ADR を仕様として渡す形に書き換えた。

レビュー担当のサブエージェントには、**このセッションの経緯を渡さず、組み立てた文脈だけを渡す**。レビュー担当が見るのは作ったものであって、考えた過程ではない。

**原則**: 早めに、こまめにレビューする。

## いつ依頼するか

**必ず:**
- 大きな機能を実装し終えたとき
- main にマージする前
- 計画の1タスクを終えたとき（タスクに分けて進めている場合）

**あると良い:**
- 行き詰まったとき（別の視点）
- リファクタリングの前（基準の確認）
- 込み入ったバグを直した後

## 手順

### 1. 範囲を決める

```bash
BASE_SHA=$(git merge-base origin/main HEAD)   # または: git rev-parse HEAD~1
HEAD_SHA=$(git rev-parse HEAD)
git diff --stat --submodule=log $BASE_SHA..$HEAD_SHA
```

コミットしていない変更をレビューするときは、先にコミットするか、`HEAD_SHA` の代わりに作業ツリー（`git diff $BASE_SHA`）を対象にすると明記する。

### 2. レイヤーを判別する

変更されたパスを次の表で振り分け、該当するレイヤーごとにチェックリストを選ぶ。

| パス | レイヤー | チェックリスト |
|---|---|---|
| `client/uta/`（`Assets/`・`Packages/manifest.json`・`ProjectSettings/`） | client | [layers/client.md](layers/client.md) |
| `server/` | server | [layers/server.md](layers/server.md) |
| `shared/`、`masterdata/` | shared | [layers/shared.md](layers/shared.md) |
| `external/<名前>`（サブモジュールのポインタ） | foundation | [layers/foundation.md](layers/foundation.md) |
| `uta_docs/`、`CLAUDE.md`、設定ファイルだけ | （コードのレビューは不要） | — |

- コードの変更がなければ、このスキルは使わない。
- `external/` はポインタの差分しか出ないので、サブモジュールの中の範囲（古いポインタ..新しいポインタ）を `git diff --submodule=log` で調べて渡す。

### 3. レイヤーごとにレビュー担当を出す

該当するレイヤーごとに `general-purpose` のサブエージェントを1つずつ出し、[code-reviewer.md](code-reviewer.md) の雛形を埋めて渡す。複数のレイヤーにまたがるときは、**同じメッセージで並べて出す**（並行して動く）。

**埋めるもの:**
- `[LAYER]`: client / server / shared / foundation
- `[CHECKLIST]`: そのレイヤーのチェックリストのパス（例: `.claude/skills/requesting-code-review/layers/server.md`）
- `[DESCRIPTION]`: 何を作ったかの短い要約
- `[PLAN_OR_REQUIREMENTS]`: 仕様。wiki の該当ページ（`uta_docs/wiki/systems/`・`architecture/`）と、関係する ADR のパス。タスクの文があればそれも
- `[BASE_SHA]` / `[HEAD_SHA]`: 範囲。foundation はサブモジュールの中の範囲と、サブモジュールのパス
- `[FILES]`: そのレイヤーに振り分けたファイルの一覧
- `[OTHER_LAYERS]`: 同じ変更でほかのレイヤーに入ったファイルの一覧（通信契約やマッピングの食い違いを見るため。なければ「なし」）

### 4. 指摘に対応する

レイヤーごとの結果を、Critical → Important → Minor の順にまとめてユーザーに見せる。

- **Critical** は直ちに直す
- **Important** は次に進む前に直す
- **Minor** は後でよいものとして控える
- レビュー担当が間違っていると思うなら、理由を添えて反論する（コードやテストで示す）
- **ADR・CLAUDE.md の方針そのものへの反対**は、勝手に直さず、ADR を見直す提案としてユーザーに確認する
- 「判断しなかったこと」の一覧は、1行ずつ扱いを決める。黙って捨てない
- 直したら、仕様ページ・`log.md` の更新が必要か確かめる（CLAUDE.md の「実装前のルール」）

## 例

```
[サーバーのガチャ抽選と、Uta.Shared の DTO、クライアントの呼び出しを実装し終えた]

あなた: マージ前にレビューに出します。

BASE_SHA=$(git merge-base origin/main HEAD)
HEAD_SHA=$(git rev-parse HEAD)
→ server/・shared/・client/uta/ に変更あり → server・shared・client の3人を並べて出す

[server のレビュー担当]
  LAYER: server
  CHECKLIST: .claude/skills/requesting-code-review/layers/server.md
  DESCRIPTION: ガチャ1回・10回の抽選と天井、交換pt の加算
  PLAN_OR_REQUIREMENTS: uta_docs/wiki/systems/gacha.md、architecture/api-design.md、decisions/0010-database.md
  FILES: server/Uta.Server/...
  OTHER_LAYERS: shared/Uta.Shared/...、client/uta/Assets/Scripts/Infrastructure/...

[結果]
  server: Important 1件（冪等性の対象になっていない）→ 直す
  shared: Minor 1件
  client: 指摘なし。マージしてよい
```

## よくある言い訳

| 言い訳 | 実際 |
|---|---|
| 「レビュー担当を出さず、自分で差分を見ればいい」 | あなたは進行役。差分を自分で読むと、作業を続けるのに必要なコンテキストを使い切る。差分と評価はレビュー担当の中に置き、指摘だけを受け取る |
| 「レビュー担当には、このセッションの経緯が全部必要」 | 組み立てた文脈だけを渡す。経緯を渡すと、作ったものではなく考えた過程を見てしまう |
| 「レイヤーをまとめて1人で見てもらえば足りる」 | レイヤーごとに守るものが違う（C# の版、Unity の制約、DB、互換性）。チェックリストを1つに絞った方が見落としが減る |

## してはいけないこと

- 「簡単だから」とレビューを飛ばす
- Critical を無視する
- Important を直さずに先へ進む
- 正しい技術的な指摘に言い返す
- .NET SDK / Unity で実行していないのに「テストが通った」と書く（CLAUDE.md）

**レビュー担当が間違っているとき:**
- 技術的な理由を添えて反論する
- 動くことを示すコードやテストを見せる
- 意図を確かめる質問をする

雛形: [code-reviewer.md](code-reviewer.md)
