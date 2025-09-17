@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ ИСПРАВЛЕНИЙ UI И TOTP
echo ========================================
echo.
echo Исправления:
echo ✅ Добавлена проверка SharedSecret в .maFile
echo ✅ Исправлена ошибка STA thread для UI диалогов
echo ✅ Использован Dispatcher.InvokeAsync для UI операций
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Исправленные проблемы:
    echo 🔧 TOTP генерация: проверка null SharedSecret
    echo 🔧 UI диалоги: правильная работа с STA thread
    echo 🔧 Dispatcher: использование InvokeAsync для UI
    echo.
    echo Ожидаемые изменения:
    echo - Нет ошибки "Value cannot be null" для TOTP
    echo - Нет ошибки "calling thread must be STA"
    echo - UI диалоги открываются корректно
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
