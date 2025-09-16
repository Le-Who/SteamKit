@echo off
chcp 65001 >nul
echo ========================================
echo ДИАГНОСТИКА СЕТЕВЫХ ПРОБЛЕМ STEAM
echo ========================================
echo.

echo 1. Проверка доступности Steam Community...
ping -n 1 steamcommunity.com
echo.

echo 2. Проверка доступности Steam API...
ping -n 1 api.steampowered.com
echo.

echo 3. Проверка DNS разрешения...
nslookup steamcommunity.com
echo.

echo 4. Проверка портов Steam (TCP 27015-27030)...
echo Попытка подключения к Steam серверам...
telnet steamcommunity.com 80
echo.

echo 5. Проверка прокси настроек...
echo HTTP_PROXY: %HTTP_PROXY%
echo HTTPS_PROXY: %HTTPS_PROXY%
echo.

echo 6. Проверка файрвола Windows...
netsh advfirewall firewall show rule name="Steam" dir=in
echo.

echo ========================================
echo ДИАГНОСТИКА ЗАВЕРШЕНА
echo ========================================
echo.
echo Если есть проблемы с сетью, попробуйте:
echo 1. Отключить антивирус/файрвол временно
echo 2. Проверить настройки прокси
echo 3. Использовать VPN
echo 4. Проверить блокировку Steam в вашей сети
echo.
pause
