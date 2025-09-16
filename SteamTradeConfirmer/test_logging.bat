@echo off
chcp 65001 >nul
echo ========================================
echo Тестирование улучшенного логирования
echo ========================================
echo.
echo Запуск Steam Trade Confirmer...
echo После запуска попробуйте добавить аккаунт
echo и проверьте логи в папке:
echo %APPDATA%\SteamTradeConfirmer\Logs\
echo.
echo Нажмите любую клавишу для запуска...
pause >nul

cd /d "%~dp0bin\Release\net8.0-windows"
SteamTradeConfirmer.exe

echo.
echo Приложение закрыто.
echo Проверьте логи в: %APPDATA%\SteamTradeConfirmer\Logs\
pause
