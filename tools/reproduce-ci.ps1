<# ============================================================================
   本地复现 GitHub Actions 的 syntax-and-logic job（Unity 仓库）

   用途：上传前确认 CI 会是绿的。对应 .github/workflows/ci.yml 里的
   syntax-and-logic job（runs-on: ubuntu-latest + setup-dotnet 8.0.x）。

   用法：在任意目录执行
       pwsh -ExecutionPolicy Bypass -File tools\reproduce-ci.ps1
   不需要 Unity License，也不需要 Unity 编辑器。

   说明：若本机 PowerShell 执行策略导致无法运行（提示"未进行数字签名"），
   加上 -ExecutionPolicy Bypass 参数即可，无需修改系统设置。
   ============================================================================ #>

$ErrorActionPreference = "Continue"

Write-Host "=== 复现 syntax-and-logic job ===" -ForegroundColor Cyan
Write-Host ""

# 1) 定位 dotnet：优先 PATH，其次常见安装位置
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) {
    foreach ($c in @("C:\Program Files\dotnet\dotnet.exe", "C:\Program Files (x86)\dotnet\dotnet.exe")) {
        if (Test-Path $c) { $dotnet = $c; break }
    }
}
if (-not $dotnet) {
    Write-Host "找不到 dotnet，请先安装 .NET SDK（6.0 或更高）" -ForegroundColor Red
    exit 1
}
Write-Host ("dotnet: " + $dotnet)

$ver = & $dotnet --version 2>&1
Write-Host ("版本 : " + $ver)
Write-Host ""

# 2) CI 里的原命令（相对路径基于仓库根目录，不是脚本所在目录）
#    脚本位于 <仓库根>/tools/reproduce-ci.ps1，所以仓库根 = $PSScriptRoot 的上一级
$repoRoot = Split-Path $PSScriptRoot -Parent
Write-Host ("仓库根: " + $repoRoot)
Push-Location $repoRoot
try {
    Write-Host "--- dotnet run --project tools/logic-smoke/LogicSmoke.csproj ---" -ForegroundColor Yellow
    & $dotnet run --project "tools/logic-smoke/LogicSmoke.csproj" 2>&1
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}

Write-Host ""
if ($code -eq 0) {
    Write-Host "syntax-and-logic job 在本地通过 ✓（推送到 GitHub 后这个 job 应该也是绿的）" -ForegroundColor Green
} else {
    Write-Host ("失败 (exit=" + $code + ")，请把上面的输出发出来排查") -ForegroundColor Red
}

exit $code
