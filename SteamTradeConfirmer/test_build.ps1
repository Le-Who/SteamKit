Write-Host "Testing SteamKit2 fixes..." -ForegroundColor Green
Write-Host "Building project..." -ForegroundColor Yellow

try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ BUILD SUCCESSFUL!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Key fixes applied:" -ForegroundColor Cyan
        Write-Host "🔧 CallbackManager now runs independently of IsConnected" -ForegroundColor White
        Write-Host "🔧 Connection initiated BEFORE callback loop starts" -ForegroundColor White
        Write-Host "🔧 Proper SteamKit2 configuration with WebSocket + TCP" -ForegroundColor White
        Write-Host "🔧 Authentication flow follows official SteamKit2 samples" -ForegroundColor White
        Write-Host ""
        Write-Host "Ready for testing: .\launch.bat" -ForegroundColor Green
    } else {
        Write-Host "❌ BUILD FAILED!" -ForegroundColor Red
        Write-Host "Check error messages above" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
