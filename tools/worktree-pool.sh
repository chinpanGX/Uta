#!/usr/bin/env bash
# 固定の worktree（../Uta-wt/1 〜）を作って使い回す。Unity の Library を worktree ごとに 1 回だけ生成するため。
# 使い方:
#   tools/worktree-pool.sh init [個数]                      固定の worktree を作る（既定 3）
#   tools/worktree-pool.sh start <番号> <新ブランチ> [base]  base（既定 origin/main）から新ブランチを切る
#   tools/worktree-pool.sh release <番号>                    origin/main に戻して空ける
#   tools/worktree-pool.sh list                              状態を一覧する
#   tools/worktree-pool.sh cleanup [--apply]                 main にマージ済みのブランチの worktree を片付ける（既定は予定の表示だけ）
set -euo pipefail

# 固定の worktree の中から呼んでも、メインのチェックアウトを基準にする
root="$(dirname "$(git rev-parse --path-format=absolute --git-common-dir)")"
pool="$(dirname "$root")/Uta-wt"

case "${1:-}" in
  init)
    count="${2:-3}"
    git -C "$root" fetch -q origin
    for n in $(seq 1 "$count"); do
      dir="$pool/$n"
      if [ -d "$dir" ]; then echo "skip $dir（作成済み）"; continue; fi
      git -C "$root" worktree add --detach "$dir" origin/main
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
  cleanup)
    # 固定の worktree は消さずに空ける（Library を作り直さないため）。それ以外の worktree は消す。
    # マージ済みの判定: 同じ名前のブランチの PR がマージ済みで、先端が origin/main に入っている（または PR の先端と同じ）。
    # PR を見るので、作ったばかりでコミットのないブランチはマージ済みにならない。
    apply=0
    case "${2:-}" in
      --apply) apply=1 ;;
      "") ;;
      *) echo "使い方: $0 cleanup [--apply]" >&2; exit 1 ;;
    esac
    act() { if [ "$apply" = 1 ]; then echo "実行: $*"; "$@"; else echo "予定: $*"; fi; }
    dirty() { [ -n "$(git -C "$1" status --porcelain --ignore-submodules=all)" ]; }

    git -C "$root" fetch -q --prune origin
    [ "$apply" = 1 ] && git -C "$root" worktree prune
    merged="$(cd "$root" && gh pr list --state merged --limit 500 --json headRefName,headRefOid --jq '.[] | .headRefName + " " + .headRefOid')"       || { echo "gh でマージ済みの PR を取れない（gh auth status を確かめる）" >&2; exit 1; }
    is_merged() {
      local tip name oid
      tip="$(git -C "$root" rev-parse "refs/heads/$1")"
      while read -r name oid; do
        [ "$name" = "$1" ] || continue
        [ "$oid" = "$tip" ] && return 0
        git -C "$root" merge-base --is-ancestor "$tip" origin/main && return 0
      done <<< "$merged"
      return 1
    }
    skip_dirty() {
      echo "飛ばす: $1（$2 はマージ済みだが未コミットの変更がある）"
      git -C "$1" status --short --ignore-submodules=all | sed 's/^/    /'
    }

    # worktree の一覧（パス・ブランチ）を先に読む
    wts=(); brs=(); wt=""; br=""; prunable=0
    while IFS= read -r line; do
      case "$line" in
        "worktree "*) wt="${line#worktree }"; br=""; prunable=0 ;;
        "branch refs/heads/"*) br="${line#branch refs/heads/}" ;;
        prunable*) prunable=1 ;;
        "")
          if [ -n "$wt" ]; then
            if [ "$prunable" = 1 ]; then echo "記録だけ残っている: $wt（--apply で prune する）"; else wts+=("$wt"); brs+=("$br"); fi
          fi
          wt="" ;;
      esac
    done < <(git -C "$root" worktree list --porcelain; echo)

    for i in "${!wts[@]}"; do
      wt="${wts[$i]}"; br="${brs[$i]}"
      if [ "$wt" = "$root" ]; then
        # メインのチェックアウト: マージ済みのブランチにいたら main に移る
        [ -n "$br" ] && [ "$br" != main ] && is_merged "$br" || continue
        if dirty "$wt"; then skip_dirty "$wt" "$br"; continue; fi
        act git -C "$root" switch -q main
        act git -C "$root" pull -q --ff-only origin main
        act git -C "$root" branch -D "$br"
      elif [[ "$wt" == "$pool"/* && "${wt#"$pool"/}" =~ ^[0-9]+$ ]]; then
        # 固定の worktree: origin/main に戻して空ける
        if [ -z "$br" ]; then echo "空き: $wt"; continue; fi
        is_merged "$br" || { echo "作業中: $wt ($br)"; continue; }
        if dirty "$wt"; then skip_dirty "$wt" "$br"; continue; fi
        act git -C "$wt" switch -q --detach origin/main
        act git -C "$root" branch -D "$br"
      else
        # それ以外（アプリが作った使い捨てなど）: 消す
        if [ -n "$br" ]; then
          is_merged "$br" || { echo "作業中: $wt ($br)"; continue; }
        else
          git -C "$root" merge-base --is-ancestor "$(git -C "$wt" rev-parse HEAD)" origin/main || { echo "作業中: $wt（detached）"; continue; }
        fi
        if dirty "$wt"; then skip_dirty "$wt" "${br:-HEAD}"; continue; fi
        if act git -C "$root" worktree remove "$wt"; then
          [ -n "$br" ] && act git -C "$root" branch -D "$br"
        else
          echo "消せなかった: $wt（ロックされているかもしれない。git worktree list で見る）" >&2
        fi
      fi
    done

    # どの worktree でも開いていないマージ済みのブランチ
    while IFS= read -r b; do
      [ "$b" = main ] && continue
      for x in "${brs[@]}"; do [ "$x" = "$b" ] && continue 2; done
      is_merged "$b" && act git -C "$root" branch -D "$b"
    done < <(git -C "$root" for-each-ref --format='%(refname:short)' refs/heads)

    # .claude/worktrees/ に残った、git に登録のないフォルダ（中身を見てから手で消す）
    for d in "$root"/.claude/worktrees/*/; do
      [ -d "$d" ] || continue
      d="${d%/}"
      for x in "${wts[@]}"; do [ "$x" = "$d" ] && continue 2; done
      echo "登録のない残り: $d"
    done

    [ "$apply" = 1 ] || echo "（予定だけ。実行するには: $0 cleanup --apply）"
    ;;
  *)
    sed -n '2,8p' "$0" >&2
    exit 1
    ;;
esac
