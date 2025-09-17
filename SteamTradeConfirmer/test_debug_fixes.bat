@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ ОТЛАДОЧНЫХ ИСПРАВЛЕНИЙ
echo ========================================
echo.
echo Исправления:
echo ✅ Исправлен парсинг .maFile (server_time как object)
echo ✅ Добавлено подробное логирование TradeOfferService
echo ✅ Диагностика Steam Web API вызовов
echo ✅ Проверка API ключа и аутентификации
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Ожидаемые изменения в логах:
    echo 🔧 "✅ TOTP код сгенерирован успешно" (без ошибок JSON)
    echo 🔧 "🔍 Начало получения трейдов для username"
    echo 🔧 "✅ Steam API ключ настроен" или "⚠️ Steam API ключ не настроен"
    echo 🔧 "🌐 Вызов Steam Web API GetTradeOffers"
    echo 🔧 "📤 Найдено X отправленных трейдов"
    echo 🔧 "📋 Итого обработано X трейдов"
    echo.
    echo Готово к тестированию:
    echo .\launch.bat
    echo.
    echo Проверьте логи на:
    echo - Нет ошибок парсинга .maFile
    echo - Подробную диагностику получения трейдов
    echo - Результат Steam Web API вызовов
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
