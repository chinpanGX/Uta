---
name: cleaning-up-worktrees
description: main にマージ済みのブランチの worktree とローカルのブランチを片付ける。固定の worktree（../Uta-wt/N）は消さずに origin/main へ戻して空け、それ以外の worktree（.claude/worktrees/ の使い捨てなど）は消し、マージ済みのローカルのブランチを消す。PR をマージした後、worktree が空かないとき、「worktree を掃除して」「マージ済みのブランチを消して」「クリーンアップして」と言われたときに使う。
---

# マージ済みの worktree を片付ける

PR をマージした後に残る worktree とローカルのブランチを、`tools/worktree-pool.sh cleanup` でまとめて片付ける。判定と操作はスクリプトが行い、LLM は結果を読んで、スクリプトが飛ばしたものだけを判断する。

## 何をするか

| 対象 | 操作 |
|---|---|
| 固定の worktree（`../Uta-wt/<番号>`）で、マージ済みのブランチを開いている | **消さない。**`origin/main` の detached HEAD に戻して空け、ブランチを消す（Unity の Library を作り直さないため。CLAUDE.md「Git」） |
| それ以外の worktree（`.claude/worktrees/` の使い捨てなど） | `git worktree remove` で消し、ブランチを消す。detached HEAD なら、HEAD が `origin/main` に入っているときに消す |
| メインのチェックアウト（`C:/Work/Uta`）がマージ済みのブランチにいる | `main` に移って `pull --ff-only` し、ブランチを消す |
| どの worktree でも開いていない、マージ済みのローカルのブランチ | 消す |
| 固定の worktree が `main` を開いている | `origin/main` の detached HEAD に戻す（`main` を開いたままだと、メインのチェックアウトが `main` に移れない）。メインのチェックアウトは最後に処理する |
| 未コミットの変更がある worktree | **触らない。**変更の一覧を出して飛ばす |
| `.claude/worktrees/` にある、git に登録のないフォルダ | **触らない。**「登録のない残り」として出す |

- **マージ済み**: 同じ名前のブランチの PR が GitHub でマージ済みで、ブランチの先端が `origin/main` に入っている（または PR の先端と同じ）。PR を見るので、`start` で切ったばかりでコミットのないブランチは消えない。マージした後にコミットを足したブランチも消えない
- リモートのブランチ（`origin/claude/...`）は消さない
- `gh` を使う（PR の取得）。クラウドのセッションでは使わない（固定の worktree が無い。CLAUDE.md「クラウドでの作業」）

## 手順

### 1. 予定を見る

どの worktree からでもよい（メインのチェックアウトを基準にする）。

```bash
tools/worktree-pool.sh cleanup
```

何も変えずに、`予定:` の行（実行するコマンド）と、`飛ばす:`・`作業中:`・`空き:`・`登録のない残り:` の行を出す。

### 2. 飛ばしたものを判断する

- **`飛ばす:`（マージ済みだが未コミットの変更がある）**: 変更の中身で分ける
  - `client/uta/Assets/Fonts/NotoSansJP/` の差分は、Prefab の生成のたびに出る既知の差分（CLAUDE.md「ビルド・テスト・実行」）なので、`git -C <worktree> restore client/uta/Assets/Fonts/NotoSansJP` で戻してよい
  - それ以外の変更は消さない。一覧をユーザーに見せて、どうするか（別のブランチに残す・捨てる）を確認する
- **`登録のない残り:`**: git の worktree としての登録が消え、フォルダだけが残ったもの。中身（`ls`）をユーザーに見せ、消してよいと言われたら消す（`rm -rf` は戻せない）
- **`フォルダが残った:`**: git の登録とブランチは消えたが、ほかのプロセス（そのフォルダを開いたセッションやエディター）が使っていて、フォルダを消し切れなかったもの。開いているものを閉じてもらってから消す
- **`作業中:`**: マージされていないブランチ。触らない

### 3. 実行する

予定に問題がなければ:

```bash
tools/worktree-pool.sh cleanup --apply
```

`実行:` の行が実行したコマンド。最後に `tools/worktree-pool.sh list` で、固定の worktree が detached HEAD で空いていることを確かめる。

### 4. 報告する

空けた worktree、消した worktree・ブランチ、飛ばしたもの（と理由）を短く書く。
