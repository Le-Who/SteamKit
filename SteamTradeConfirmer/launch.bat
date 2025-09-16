@echo off
chcp 65001 >nul
title Steam Trade Confirmer
echo ========================================
echo    STEAM TRADE CONFIRMER
echo ========================================
echo.
echo Запуск приложения...
echo.

cd /d "%~dp0bin\Release\net8.0-windows"

if not exist "SteamTradeConfirmer.exe" (
    echo ОШИБКА: Файл SteamTradeConfirmer.exe не найден!
    echo.
    echo Возможные решения:
    echo 1. Запустите build.bat для сборки приложения
    echo 2. Убедитесь, что вы находитесь в правильной папке
    echo.
    pause
    exit /b 1
)

echo Приложение найдено. Запуск...
echo.
SteamTradeConfirmer.exe

echo.
echo Приложение закрыто.
pause
