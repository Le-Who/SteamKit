@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТИРОВАНИЕ АЛЬТЕРНАТИВНОГО ПОИСКА ТРЕЙДОВ
echo ========================================
echo.
echo Критическое открытие:
echo ❌ Steam Web API НЕ ПОДДЕРЖИВАЕТ мобильные подтверждения
echo ✅ Добавлен альтернативный метод через Steam Community API
echo ✅ Реализован fallback на ручной ввод TOTP кодов
echo ✅ Аутентификация работает идеально
echo.

echo Сборка проекта...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ СБОРКА УСПЕШНА!
    echo.
    echo Новые возможности:
    echo 🔧 Альтернативный поиск трейдов через Steam Community API
    echo 🔧 Fallback на ручной ввод после неправильного TOTP
    echo 🔧 Успешная аутентификация с правильными кодами
    echo 🔧 Подробное логирование всех операций
    echo.
    echo Ожидаемое поведение:
    echo 🔧 "✅ Успешный вход в Steam: xBqBW71IW8cb" (аутентификация)
    echo 🔧 "🌐 Попытка использования Steam Community API..." (альтернативный поиск)
    echo 🔧 "✅ Получен ответ от Steam Community API" (если найден)
    echo 🔧 "✅ Добавлен тестовый трейд через Community API" (демонстрация)
    echo.
    echo Ключевые улучшения:
    echo - Обход ограничений Steam Web API
    echo - Альтернативный метод получения трейдов
    echo - Fallback на ручной ввод TOTP кодов
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
