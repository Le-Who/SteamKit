@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ УЛУЧШЕННОЙ ДИАГНОСТИКИ
echo ========================================
echo.
echo Это обновленная версия с расширенной диагностикой:
echo.
echo ✅ Проверка сетевой доступности Steam
echo ✅ Мониторинг состояния подключения  
echo ✅ Диагностика CallbackManager
echo ✅ Детальное логирование процесса аутентификации
echo ✅ Альтернативная конфигурация SteamKit2
echo.
echo После запуска попробуйте добавить аккаунт.
echo В логах вы увидите детальную диагностику:
echo.
echo 🔍 - Проверка сети
echo 🔧 - Конфигурация
echo 🔄 - CallbackManager
echo 🚨 - Критические проблемы
echo.
echo Логи сохраняются в: %APPDATA%\SteamTradeConfirmer\Logs\
echo.
echo Нажмите любую клавишу для запуска...
pause >nul

cd /d "%~dp0bin\Release\net8.0-windows"
SteamTradeConfirmer.exe

echo.
echo Приложение закрыто.
echo Проверьте логи в: %APPDATA%\SteamTradeConfirmer\Logs\
echo.
echo Если проблема не решена, запустите:
echo network_diagnostics.bat
echo.
pause
