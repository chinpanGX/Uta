#!/usr/bin/env bash
# 固定の worktree（../Uta-wt/1 〜）を作って使い回す。Unity の Library を worktree ごとに 1 回だけ生成するため。
# 使い方:
#   tools/worktree-pool.sh init [個数]                      固定の worktree を作る（既定 3）
#   tools/worktree-pool.sh start <番号> <新ブランチ> [base]  base（既定 origin/main）から新ブランチを切る
#   tools/worktree-pool.sh release <番号>                    origin/main に戻して空ける
#   tools/worktree-pool.sh list                              状態を一覧する
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
pool="$(dirname "$root")/Uta-wt"

case "${1:-}" in
  init)
    count="${2:-3}"
    git -C "$root" fetch -q origin
    for n in $(seq 1 "$count"); do
      dir="$pool/$n"
      if [ -d "$dir" ]; then echo "skip $dir（作成済み）"; continue; fi
      git -C "$root" worktree add --detach "$dir" origin/main
      git -C "$dir" submodule update --init
      echo "作成: $dir（Unity Hub に登録して、1 回だけ開いて Library を作る）"
    done
    ;;
  start)
    n="${2:?番号}"; branch="${3:?新ブランチ}"; base="${4:-origin/main}"
    dir="$pool/$n"
    [ -z "$(git -C "$dir" status --porcelain --ignore-submodules=all)" ] || { echo "未コミットの変更があるので使えない: $dir" >&2; exit 1; }
    git -C "$dir" fetch -q origin
    git -C "$dir" switch --detach "$base"
    git -C "$dir" switch -c "$branch"
    git -C "$dir" submodule update --init
    echo "開始: $dir ($branch ← $base)"
    ;;
  release)
    n="${2:?番号}"
    dir="$pool/$n"
    [ -z "$(git -C "$dir" status --porcelain --ignore-submodules=all)" ] || { echo "未コミットの変更があるので戻せない: $dir" >&2; exit 1; }
    git -C "$dir" fetch -q origin
    git -C "$dir" switch --detach origin/main
    echo "空けた: $dir"
    ;;
  list)
    git -C "$root" worktree list
    ;;
  *)
    sed -n '2,7p' "$0" >&2
    exit 1
    ;;
esac
