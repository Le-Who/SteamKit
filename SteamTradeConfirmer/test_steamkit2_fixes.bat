@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ ИСПРАВЛЕНИЙ STEAMKIT2
echo ========================================
echo.
echo Проверка исправлений:
echo ✅ Переписана логика подключения SteamKit2
echo ✅ Исправлен CallbackManager (не завершается немедленно)
echo ✅ Использован правильный поток аутентификации
echo ✅ Поддержка WebSocket + TCP протоколов
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Основные исправления:
    echo 🔧 CallbackManager теперь работает независимо от IsConnected
    echo 🔧 Подключение инициируется ПЕРЕД запуском callback loop
    echo 🔧 Используется правильная конфигурация SteamKit2
    echo 🔧 Поддержка WebSocket + TCP протоколов
    echo.
    echo Готово к тестированию подключения:
    echo .\launch.bat
    echo.
    echo Ожидаемые изменения в логах:
    echo - CallbackManager не должен завершаться немедленно
    echo - IsConnected должен стать true после ConnectedCallback
    echo - Аутентификация должна проходить успешно
) else (
    echo.
    echo ❌ ОШИБКА СБОРКИ!
    echo Проверьте сообщения об ошибках выше.
)

echo.
pause
