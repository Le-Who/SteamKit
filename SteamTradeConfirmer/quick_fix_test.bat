@echo off
chcp 65001 >nul
echo ========================================
echo БЫСТРЫЙ ТЕСТ РЕШЕНИЯ ПРОБЛЕМЫ
echo ========================================
echo.
echo Этот скрипт поможет быстро проверить
echo основные причины проблемы подключения.
echo.

echo 1. Проверка антивируса...
echo Убедитесь, что антивирус не блокирует приложение.
echo Временно отключите антивирус на 5 минут.
echo.
pause

echo 2. Проверка файрвола...
echo Добавляем правило в Windows Firewall...
netsh advfirewall firewall add rule name="SteamTradeConfirmer" dir=in action=allow program="%~dp0bin\Release\net8.0-windows\SteamTradeConfirmer.exe"
echo.
echo Правило добавлено. Если была ошибка - запустите как администратор.
echo.
pause

echo 3. Проверка DNS...
echo Очищаем кэш DNS...
ipconfig /flushdns
echo.
echo Кэш DNS очищен.
echo.
pause

echo 4. Проверка сетевых служб...
echo Сбрасываем сетевые настройки...
netsh winsock reset
echo.
echo Сетевые настройки сброшены. Перезагрузите компьютер после теста.
echo.
pause

echo 5. Запуск приложения...
echo Теперь запускаем приложение для тестирования...
echo.
cd /d "%~dp0bin\Release\net8.0-windows"
SteamTradeConfirmer.exe

echo.
echo Тест завершен.
echo Если проблема не решена, попробуйте:
echo 1. Другую сеть (мобильный интернет)
echo 2. VPN
echo 3. Перезагрузку компьютера
echo.
pause
