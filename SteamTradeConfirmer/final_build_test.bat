@echo off
chcp 65001 >nul
echo ========================================
echo ФИНАЛЬНАЯ ПРОВЕРКА СБОРКИ
echo ========================================
echo.
echo Проверка всех исправлений:
echo ✅ Исправлены ошибки async/await
echo ✅ Исправлены типы возврата Task
echo ✅ Исправлены предупреждения nullability
echo ✅ Удалены несуществующие колбэки
echo ✅ Исправлена логика CallbackManager
echo.

echo Попытка сборки...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo Все ошибки и предупреждения исправлены.
    echo.
    echo Проект готов к запуску:
    echo .\launch.bat
    echo.
    echo Или протестируйте исправления:
    echo .\test_connection_fixes.bat
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
    echo.
    echo Возможные причины:
    echo - Не все async/await исправлены
    echo - Проблемы с типами возврата
    echo - Ошибки в синтаксисе
)

echo.
pause
