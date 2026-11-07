#!/usr/bin/env bash
# 共通基盤のリポジトリの指定コミットから .nupkg を作り、packages/ に置いて、Uta の固定をそのコミットに揃える（ADR 0019）。
# 使い方: tools/pack-foundation.sh <VolTech-MagicOnion|MasterMemoryAddons> <コミット>
# 書き換えるもの: packages/、Directory.Packages.props、.config/dotnet-tools.json、client/uta/Packages/manifest.json
# 書き換えたら Unity で解決し直し（unity command package resolve）、packages-lock.json もコミットする。
set -euo pipefail

repo="${1:?リポジトリ名}"
commit="${2:?コミット}"
root="$(git rev-parse --show-toplevel)"
out="$root/packages"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

case "$repo" in
  VolTech-MagicOnion)
    ids=(VolTech.Server.Abstractions VolTech.Server)
    ;;
  MasterMemoryAddons)
    ids=(MasterMemory.Addons.Core MasterMemory.Addons.Tools)
    ;;
  *)
    echo "知らないリポジトリ: $repo" >&2
    exit 1
    ;;
esac

git clone -q "https://github.com/chinpanGX/$repo.git" "$work/src"
git -C "$work/src" checkout -q --detach "$commit"
full="$(git -C "$work/src" rev-parse HEAD)"
version="0.1.0-g$(git -C "$work/src" rev-parse --short=7 HEAD)"

for id in "${ids[@]}"; do
  project="$work/src/src/$id/$id.csproj"
  props=(-p:Version="$version" -p:RepositoryUrl="https://github.com/chinpanGX/$repo" -p:RepositoryCommit="$full")
  if [ "$id" = MasterMemory.Addons.Tools ]; then
    props+=(-p:PackAsTool=true -p:ToolCommandName=mastermemory-addons)
  fi
  # Tools の csproj は復元後のパスを評価時に読むので、復元と pack を分ける
  dotnet restore "$project" -v quiet
  dotnet pack "$project" -c Release -o "$work/out" -v quiet "${props[@]}"
done

for id in "${ids[@]}"; do
  rm -f "$out/$id".[0-9]*.nupkg
  cp "$work/out/$id.$version.nupkg" "$out/"
  sed -i "s|Include=\"$id\" Version=\"[^\"]*\"|Include=\"$id\" Version=\"$version\"|" "$root/Directory.Packages.props"
done

if [ "$repo" = MasterMemoryAddons ]; then
  sed -i "s|\"version\": \"[^\"]*\"|\"version\": \"$version\"|" "$root/.config/dotnet-tools.json"
fi
sed -i -E "s|(chinpanGX/$repo\.git\?path=[^#\"]*#)[0-9a-f]{40}|\1$full|" "$root/client/uta/Packages/manifest.json"

echo "$repo $version（$full）"
