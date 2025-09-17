@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ ФИНАЛЬНЫХ ИСПРАВЛЕНИЙ
echo ========================================
echo.
echo Исправления:
echo ✅ Исправлен парсинг .maFile (JsonPropertyName атрибуты)
echo ✅ Добавлено логирование загрузки трейдов
echo ✅ Включена долгосрочная сессия (IsPersistentSession = true)
echo ✅ Включено сохранение пароля (ShouldRememberPassword = true)
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Ожидаемые изменения:
    echo 🔧 .maFile парсится корректно (shared_secret найден)
    echo 🔧 TOTP коды генерируются автоматически
    echo 🔧 Трейды загружаются после аутентификации
    echo 🔧 Сессия сохраняется между перезапусками
    echo 🔧 Подробные логи загрузки трейдов
    echo.
    echo Готово к тестированию:
    echo .\launch.bat
    echo.
    echo Проверьте логи на:
    echo - "✅ TOTP код сгенерирован успешно"
    echo - "📋 Получено X трейдов для username"
    echo - "✅ Загружено X обменов всего"
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
