#!/usr/bin/env bash
# Claude Code on the web（クラウド）のセッション開始・再開時に、環境を整えて状態を報告する。
# 何度動いてもよい。ローカル（CLAUDE_CODE_REMOTE が true でない）では何もしない。
# 標準出力はセッションの文脈に入るので、最後に「何が使えるか」を書く。失敗してもセッションは止めない。
set -uo pipefail

[ "${CLAUDE_CODE_REMOTE:-}" = "true" ] || exit 0

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}" || exit 0

# 1. .NET 10 SDK（クラウドに最初から入っていない）。dotnet-install の取得先が許可されていないときは apt に回す
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
has_sdk() { dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; }
if ! has_sdk; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet" >/dev/null 2>&1
fi
if ! has_sdk; then
  sudo_cmd=""
  [ "$(id -u)" = "0" ] || sudo_cmd="sudo -n"
  $sudo_cmd apt-get update -qq >/dev/null 2>&1
  $sudo_cmd apt-get install -y -qq dotnet-sdk-10.0 >/dev/null 2>&1
fi
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    [ -x "$HOME/.dotnet/dotnet" ] && echo "export DOTNET_ROOT=\"$HOME/.dotnet\""
    echo "export PATH=\"$HOME/.dotnet:$HOME/.dotnet/tools:\$PATH\""
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
  } >> "$CLAUDE_ENV_FILE"
fi

# 2. サブモジュール。計画とレビューは サーバーとマスタの生成が使う 2 つを取り込む。Supplement・UnityScreenNavigator は Unity の git URL だけで参照する（ADR 0019）。
#    .gitmodules の SSH URL はクラウドで使えないので、この環境の git だけ HTTPS に読み替える（リポジトリはすべて公開）。
git config --global url."https://github.com/".insteadOf "git@github.com:"
missing=""
for name in VolTech-MagicOnion MasterMemoryAddons; do
  git submodule update --init "external/$name" >/dev/null 2>&1 || missing="$missing $name"
done

# 3. 復元と、masterdata/gen.ps1 用の PowerShell
restore="未実行"
if has_sdk; then
  if dotnet restore Uta.slnx >/dev/null 2>&1; then restore="OK"; else restore="失敗"; fi
  command -v pwsh >/dev/null 2>&1 || dotnet tool install --global PowerShell >/dev/null 2>&1
fi

# 4. 状態の報告
echo "[cloud-setup] .NET 10 SDK: $(has_sdk && echo OK || echo なし)、dotnet restore: $restore"
echo "[cloud-setup] 取り込めなかったサブモジュール:${missing:- なし}"
echo "[cloud-setup] pwsh: $(command -v pwsh >/dev/null 2>&1 && echo OK || echo なし)、docker: $(docker info >/dev/null 2>&1 && echo OK || echo 動いていない)"
echo "[cloud-setup] ここで確かめられなかったビルド・テストは、確認できていないと明記する（CLAUDE.md「クラウドでの作業」）"
exit 0
