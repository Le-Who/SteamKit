@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ ОТЛАДКИ API И ПОИСКА ТРЕЙДОВ
echo ========================================
echo.
echo Исправления:
echo ✅ Изменен active_only с "1" на "0" (получаем ВСЕ трейды)
echo ✅ Добавлена проверка прав API ключа через ISteamUser
echo ✅ Добавлена попытка использования ISteamEconomy API
echo ✅ Добавлено подробное логирование параметров запроса
echo ✅ Добавлен полный вывод ответа API для отладки
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Ожидаемые изменения в логах:
    echo 🔧 "🔍 Проверка прав API ключа..." (проверка ISteamUser)
    echo 🔧 "✅ API ключ имеет доступ к ISteamUser" (или ошибка)
    echo 🔧 "🔍 Попытка использования ISteamEconomy API..." (проверка ISteamEconomy)
    echo 🔧 "📋 Параметры запроса API:" (подробные параметры)
    echo 🔧 "  - active_only: 0 (ВСЕ трейды)" (измененный параметр)
    echo 🔧 "🔍 Полный ответ API: {...}" (полный JSON ответ)
    echo.
    echo Ключевые изменения:
    echo - active_only изменен с "1" на "0" для получения ВСЕХ трейдов
    echo - Добавлена диагностика прав API ключа
    echo - Подробное логирование для понимания проблемы
    echo.
    echo Теперь должно найти трейд "Awaiting Mobile Confirmation"!
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
