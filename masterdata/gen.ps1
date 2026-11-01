# マスタの生成。使い方: powershell -NoProfile -File masterdata/gen.ps1 [code-gen | master-gen | check]
#   code-gen   定義から生成ソースを作る
#   master-gen CSV を検証してバイナリを作る（生成ソースが古ければコード生成もやり直す）。省略時
#   check      検証だけを行い、何も書き込まない
param(
    [Parameter(Position = 0)]
    [ValidateSet('code-gen', 'master-gen', 'check')]
    [string]$Command = 'master-gen'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $repoRoot 'external/MasterMemoryAddons/src/MasterMemory.Addons.Tools'
$config = Join-Path $PSScriptRoot 'masterdata.yaml'

if ($Command -eq 'check')
{
    $toolArgs = @('master-gen', '--check')
}
else
{
    $toolArgs = @($Command)
}

# Uta のルートの Directory.Packages.props（パッケージ版の一括管理）がツールにも効くので、この呼び出しだけ切る
dotnet run --project $tools -p:ManagePackageVersionsCentrally=false -- @toolArgs --config $config
exit $LASTEXITCODE
