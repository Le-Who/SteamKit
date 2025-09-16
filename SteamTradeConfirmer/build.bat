@echo off
chcp 65001 >nul
echo Сборка Steam Trade Confirmer...
dotnet build --configuration Release
if %ERRORLEVEL% EQU 0 (
    echo Сборка успешна!
    echo Запуск приложения...
    cd bin\Release\net8.0-windows
    SteamTradeConfirmer.exe
) else (
    echo Ошибка сборки!
    pause
)
