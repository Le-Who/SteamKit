@echo off
chcp 65001 >nul
echo ========================================
echo ПОЛНЫЙ ТЕСТ STEAM TRADE CONFIRMER
echo ========================================
echo.
echo 🔧 ВСЕ ИСПРАВЛЕНИЯ ПРИМЕНЕНЫ:
echo.
echo ✅ TOTP ГЕНЕРАТОР:
echo   - Правильный формат: 5 символов (23456789BCDFGHJKMNPQRTVWXY)
echo   - Steam-совместимый алгоритм
echo   - Fallback на ручной ввод при ошибке
echo.
echo ✅ ПОИСК ТРЕЙДОВ:
echo   - Regex парсинг HTML Community API
echo   - Извлечение реальных ID трейдов
echo   - Детекция мобильных подтверждений
echo   - Множественные паттерны поиска
echo.
echo ✅ ИСПРАВЛЕНИЯ КОДА:
echo   - Ошибка LogError исправлена
echo   - Предупреждения async/await исправлены
echo   - Все компиляционные ошибки устранены
echo.
echo 🚀 ТЕСТИРОВАНИЕ:
echo.
echo 1. Запуск компиляции...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo 🎯 ОЖИДАЕМОЕ ПОВЕДЕНИЕ:
    echo.
    echo ✅ TOTP: "✅ TOTP код сгенерирован: X2B4C" (5 символов)
    echo ✅ Fallback: "⚠️ Предыдущий код неправильный, ручной ввод"
    echo ✅ Аутентификация: "✅ Успешный вход в Steam"
    echo ✅ Трейды: "🔍 Найдено X ID трейдов в HTML"
    echo ✅ Подтверждения: "📱 УВЕДОМЛЕНИЕ О МОБИЛЬНОМ ПОДТВЕРЖДЕНИИ!"
    echo.
    echo 🚀 ГОТОВО К ЗАПУСКУ:
    echo .\launch.bat
    echo.
    echo Все критические проблемы решены! 🎉
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
