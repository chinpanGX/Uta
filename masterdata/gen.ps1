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
$config = Join-Path $PSScriptRoot 'masterdata.yaml'

if ($Command -eq 'check')
{
    $toolArgs = @('master-gen', '--check')
}
else
{
    $toolArgs = @($Command)
}

# ツールは packages/ の版を .config/dotnet-tools.json で固定している（ADR 0019）
Set-Location $repoRoot
dotnet tool restore
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

dotnet tool run mastermemory-addons -- @toolArgs --config $config
exit $LASTEXITCODE
