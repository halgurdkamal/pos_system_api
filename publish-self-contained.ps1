# PowerShell script to publish the application as self-contained
# This includes the .NET runtime so the hosting doesn't need .NET 8.0 installed

Write-Host "Publishing POS System API (Self-Contained)..." -ForegroundColor Green
Write-Host ""

# Clean previous publish
if (Test-Path "./publish") {
    Write-Host "Cleaning previous publish folder..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "./publish"
}

# Publish as self-contained for Windows x64
Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

# Create logs directory
Write-Host "Creating logs directory..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path "./publish/logs" | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Publish successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Go to the 'publish' folder"
    Write-Host "2. Upload ALL files to MonsterASP.NET"
    Write-Host "3. Make sure 'pos_system_api.exe' and 'web.config' are uploaded"
    Write-Host ""
    Write-Host "To test locally, run:" -ForegroundColor Cyan
    Write-Host "  cd publish"
    Write-Host "  .\pos_system_api.exe"
    Write-Host ""
    
    # Show publish folder location
    $publishPath = Resolve-Path "./publish"
    Write-Host "Files are ready at: $publishPath" -ForegroundColor Green
    
} else {
    Write-Host ""
    Write-Host "✗ Publish failed!" -ForegroundColor Red
    Write-Host "Check the error messages above."
}
