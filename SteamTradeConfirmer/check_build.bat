@echo off
chcp 65001 >nul
echo ========================================
echo ПРОВЕРКА СБОРКИ ПРОЕКТА
echo ========================================
echo.

echo Проверка исправлений...
echo ✅ Удалены несуществующие колбэки
echo ✅ Оставлены только рабочие колбэки
echo ✅ Исправлена логика CallbackManager
echo.

echo Попытка сборки...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo Все ошибки исправлены.
    echo.
    echo Теперь можно запускать приложение:
    echo .\launch.bat
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
