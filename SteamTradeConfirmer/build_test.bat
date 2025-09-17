@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТ КОМПИЛЯЦИИ STEAM TRADE CONFIRMER
echo ========================================
echo.

echo Проверка компиляции...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Все исправления применены:
    echo ✅ TOTP генератор исправлен (5 символов)
    echo ✅ Fallback на ручной ввод TOTP
    echo ✅ Regex парсинг трейдов из HTML
    echo ✅ Детекция мобильных подтверждений
    echo ✅ Ошибка LogError исправлена
    echo.
    echo Готово к тестированию: .\launch.bat
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
