@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТ ИСПРАВЛЕНИЙ ПОДКЛЮЧЕНИЯ
echo ========================================
echo.
echo Этот скрипт протестирует исправления:
echo.
echo ✅ Увеличен таймаут подключения (30 секунд)
echo ✅ Исправлена логика CallbackManager
echo ✅ Добавлена обработка Steam Guard
echo ✅ Улучшена диагностика подключения
echo ✅ Добавлена обработка Machine Auth
echo.

echo 1. Сборка проекта...
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Ошибка сборки!
    pause
    exit /b 1
)
echo ✅ Сборка успешна!
echo.

echo 2. Запуск приложения для тестирования...
echo.
echo ВАЖНО: Теперь логи будут показывать:
echo - 🔍 Детальную диагностику подключения
echo - 🔐 Обработку Steam Guard кодов
echo - ⚠️ Конкретные причины ошибок
echo - ✅ Успешные этапы подключения
echo.
echo Попробуйте добавить аккаунт и проверьте логи!
echo.

cd /d "%~dp0bin\Release\net8.0-windows"
SteamTradeConfirmer.exe

echo.
echo Тестирование завершено.
echo Проверьте логи в папке: %APPDATA%\SteamTradeConfirmer\Logs\
pause
