# Verification Script - Check if publish is ready for deployment

Write-Host ""
Write-Host "=================================="
Write-Host "  DEPLOYMENT READINESS CHECK"
Write-Host "=================================="
Write-Host ""

# Check main files
Write-Host "Checking critical files..."
$exe = Test-Path "./publish/pos_system_api.exe"
$web = Test-Path "./publish/web.config"
$app = Test-Path "./publish/appsettings.json"
$log = Test-Path "./publish/logs"

if ($exe) { Write-Host "  [OK] pos_system_api.exe" } else { Write-Host "  [MISSING] pos_system_api.exe" }
if ($web) { Write-Host "  [OK] web.config" } else { Write-Host "  [MISSING] web.config" }
if ($app) { Write-Host "  [OK] appsettings.json" } else { Write-Host "  [MISSING] appsettings.json" }
if ($log) { Write-Host "  [OK] logs folder" } else { Write-Host "  [MISSING] logs folder" }

Write-Host ""
Write-Host "Counting files..."
$dllCount = (Get-ChildItem "./publish/*.dll" -ErrorAction SilentlyContinue | Measure-Object).Count
$fileCount = (Get-ChildItem "./publish" -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "  DLL files: $dllCount"
Write-Host "  Total files: $fileCount"

Write-Host ""
Write-Host "Calculating size..."
$totalSize = (Get-ChildItem "./publish" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "  Total: $([math]::Round($totalSize, 2)) MB"

Write-Host ""
Write-Host "=================================="
if ($exe -and $web -and $app -and $dllCount -gt 50) {
    Write-Host "  STATUS: READY TO DEPLOY!"
    Write-Host "=================================="
    Write-Host ""
    Write-Host "Next: Upload ALL files to MonsterASP.NET"
} else {
    Write-Host "  STATUS: INCOMPLETE"
    Write-Host "=================================="
    Write-Host ""
    Write-Host "Run: dotnet publish -c Release -r win-x64 --self-contained true -o ./publish"
}
Write-Host ""
