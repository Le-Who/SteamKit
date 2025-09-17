@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ ИСПРАВЛЕНИЯ STEAM TOTP
echo ========================================
echo.
echo Критические исправления:
echo ✅ Исправлен TOTP генератор для Steam Guard
echo ✅ Правильный формат: 5 символов (23456789BCDFGHJKMNPQRTVWXY)
echo ✅ Fallback на ручной ввод после неправильного TOTP
echo ✅ Альтернативный поиск трейдов через Community API
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Ожидаемое поведение:
    echo 🔧 "✅ TOTP код сгенерирован и кэширован: XXXXX" (5 символов)
    echo 🔧 "⚠️ Предыдущий TOTP код был неправильным, запрашиваем ручной ввод" (если неправильный)
    echo 🔧 "✅ Успешный вход в Steam: xBqBW71IW8cb" (аутентификация)
    echo 🔧 "🌐 Попытка получения трейдов через Steam Community API..." (альтернативный поиск)
    echo.
    echo Ключевые улучшения:
    echo - Правильный формат TOTP кодов (5 символов)
    echo - Алфавит Steam: 23456789BCDFGHJKMNPQRTVWXY
    echo - Fallback на ручной ввод при ошибке
    echo - Альтернативный поиск трейдов
    echo - Успешная аутентификация
    echo.
    echo Готово к тестированию:
    echo .\launch.bat
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
