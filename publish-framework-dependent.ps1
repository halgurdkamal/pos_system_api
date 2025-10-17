# PowerShell script to publish the application as framework-dependent
# This creates a smaller package but requires .NET runtime on the hosting

Write-Host "Publishing POS System API (Framework-Dependent)..." -ForegroundColor Green
Write-Host "Note: The hosting server must have .NET 8.0 runtime installed!" -ForegroundColor Yellow
Write-Host ""

# Clean previous publish
if (Test-Path "./publish") {
    Write-Host "Cleaning previous publish folder..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "./publish"
}

# Publish as framework-dependent
Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish -c Release -o ./publish

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Publish successful!" -ForegroundColor Green
    Write-Host ""
    
    # Copy the framework-dependent web.config
    if (Test-Path "./web.config.framework-dependent") {
        Write-Host "Copying framework-dependent web.config..." -ForegroundColor Cyan
        Copy-Item "./web.config.framework-dependent" "./publish/web.config" -Force
        Write-Host "✓ web.config updated for framework-dependent deployment" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Go to the 'publish' folder"
    Write-Host "2. Upload ALL files to MonsterASP.NET"
    Write-Host "3. Make sure the hosting has .NET 8.0 runtime installed"
    Write-Host ""
    
    # Show publish folder location
    $publishPath = Resolve-Path "./publish"
    Write-Host "Files are ready at: $publishPath" -ForegroundColor Green
    
} else {
    Write-Host ""
    Write-Host "✗ Publish failed!" -ForegroundColor Red
    Write-Host "Check the error messages above."
}
