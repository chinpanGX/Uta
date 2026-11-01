---
name: uta-elephant
description: Uta で egm（Elephant-Goldfish Model）のスキルを使うときの、プロジェクトの文脈と Uta 独自の運用ルール。/egm:egm-brainstorm・egm-draft・egm-review・egm-review-loop・egm-resolve・egm-drift-report・egm-bootstrap を Uta で使うとき、egm のスキルより先に読む。設計書のレビューは egm ではなく requesting-design-review を使う。
---

# uta-elephant — Uta で egm を使うときのルール

egm の各スキルの手順に従う。ただし、ここに書いたことが egm の標準の動きと食い違うときは、**ここを優先する**。
Uta の最上位ルールは `CLAUDE.md`。ここはそれを egm 向けに言い直したもので、CLAUDE.md と矛盾したら CLAUDE.md が正。

## 最初に読むもの（Elephant の文脈）

1. `CLAUDE.md`（運用ルール・実装方針・コーディング規約）
2. `uta_docs/wiki/index.md`（wiki の入口。ページの一覧は各フォルダの `README.md`）
3. `uta_docs/wiki/decisions/` の ADR（決定はここが正。特に `0001-ip-policy.md`）
4. 扱う機能に関係するページ（`overview/game-spec.md`、`systems/`、`architecture/`）
5. `uta_docs/wiki/glossary.md`（用語）

## 文書の置き場所

egm 標準の `docs/designs/` と `SITEMAP.md` は**使わない**。Uta の wiki の流れ（raw → wiki）に乗せる。

| 段階 | egm の標準 | Uta での置き場所 |
|---|---|---|
| 作業中の brief・下書き | `./concepts_brief.md`、`./design_draft.md` | `scratch/concepts_brief.md`、`scratch/design_draft.md`（`.gitignore` で除外。何度書き換えてもよい） |
| brainstorm を区切る（「RFC として公開」を選んだとき） | `docs/designs/<feature>.md` | `uta_docs/raw/YYYY-MM-DD-<件名>-concept.md` に写しを置く |
| draft の確定（Step 6 の保存先を聞かれたとき） | `docs/designs/<FEATURE>.md` | 「custom target directory」を選び、`uta_docs/raw/YYYY-MM-DD-<件名>-design.md` に写しを置く |
| 仕様として反映 | （なし） | `uta_docs/wiki/systems/` か `architecture/` のページに、Uta の書式で反映する（下記） |

- `uta_docs/raw/` のファイルは一度置いたら書き換えない（`uta_docs/raw/README.md`）。直すときは新しいファイルを置く。
- raw に置いただけでは終わらない。必ず wiki に反映し、そのフォルダの `README.md` を更新する。変更の記録はコミットメッセージに書く。

## wiki に反映するときの書式

egm のテンプレート（Overview / Goals / Open Questions / CUJs / Technical Plan …）は、そのまま wiki に貼らない。Uta の書式に直す:

- 先頭に frontmatter（`title` / `status` / `updated`）。反映したばかりなら `status: draft`
- 「決まっていること」と「未決事項」を分ける。egm の Open Questions は「未決事項」へ
- 推測は推測と明記する
- 他ページへのリンクは相対パスの Markdown リンク
- 新しい用語は `glossary.md` に足す
- Detailed Implementation Plan（触るファイルの一覧・検証計画）は、仕様ページには書かない。`uta_docs/plans/` に plans/README.md の書式で実装計画として書き、仕様ページからリンクする（[ADR 0017](../../../uta_docs/wiki/decisions/0017-spec-design-plan-stages.md)）
- 技術選定・方針変更を含むなら、ADR を `decisions/0000-template.md` から新しく作る（既存 ADR は書き換えず Superseded にする）

## 書くときに守ること

- 文書・チャットでの返答は**日本語**で書く（egm のテンプレートの見出しも日本語にしてよい）。
- **IP方針**: キャラクター・名称・楽曲・UI 素材・ロゴ・世界観・シナリオはすべてオリジナル。既存作品の名称や固有の表現を、brief・design・レビューの指摘に入れない。
- **勝手に決めない**: 未決事項や、ADR に反する案は、決定として書かずにユーザーに確認する。egm の承認ゲート（各セクションの確認）では、ADR との関係を添えて聞く。
- 実装方針（サーバー権威、Shared.Domain では `float` / `double` を使わない、iOS の AOT 制約、乱数は `xoshiro256**` など）は CLAUDE.md の「実装方針」に従う。
- .NET SDK / Unity で実行していない検証を「通った」と書かない。Verification Plan には実際にあるコマンドだけを書く（CLAUDE.md の「ビルド・テスト・実行」）。

## レビュー

設計書のレビューは、`/egm:egm-review`・`/egm:egm-review-loop` ではなく [requesting-design-review](../requesting-design-review/SKILL.md) を使う。egm のレビューは 1 ページの仕様には重く、Claude からも呼べないため（スキルに `disable-model-invocation: true` が付いている）。

egm の brainstorm・draft の途中でレビューが走るときや、ユーザーが egm のレビューを指定したときは、`GoldfishContext` に対象の文書に加えて次を入れる。レビュー担当は会話の経緯を知らないので、これがないと決定済みのことを蒸し返す。

- `CLAUDE.md`
- `uta_docs/wiki/decisions/`（ADR 全体）
- `uta_docs/wiki/glossary.md`
- 対象の機能に関係する `systems/`・`architecture/` のページ
- 対象が実装計画なら、触る予定のコード（`shared/`・`server/`・`client/uta/Assets/`）

ADR で決まっていることへの反対意見は、「ADR を見直す提案」として扱い、そのまま直さない。採るかどうかはユーザーが決める。

## drift report

`/egm:egm-drift-report` で設計のフォルダを聞かれたら `uta_docs/wiki/` を指定する（主に `systems/` と `architecture/`）。

- ズレが見つかったら、文書とコードのどちらを直すかはユーザーが決める（egm の標準どおり）。
- 文書を直したら `updated` を更新し、直したことをコミットメッセージに書く。

## bootstrap

`/egm:egm-bootstrap` は**コードのフォルダだけ**に使う。対象は `shared/`・`server/`・`client/uta/Assets/` の自作コード。

- `uta_docs/`・`external/`（別リポジトリのサブモジュール）・`masterdata/`・Unity の自動生成物には使わない（ルートの `.egmignore`）。
- README は日本語で、フォルダとファイルの役割に絞る。仕様や設計の理由は書かず、wiki の該当ページへリンクする（知識の正は wiki）。
- `client/uta/Assets/` に README を置くと Unity が `.meta` を作る。`.meta` もコミットする。
