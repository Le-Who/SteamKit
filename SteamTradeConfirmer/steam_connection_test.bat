@echo off
chcp 65001 >nul
echo ========================================
echo ТЕСТ ПОДКЛЮЧЕНИЯ К STEAM СЕРВЕРАМ
echo ========================================
echo.
echo Этот скрипт проверит доступность Steam серверов
echo и поможет диагностировать проблемы подключения.
echo.

echo 1. Проверка DNS разрешения Steam серверов...
nslookup steamcommunity.com
echo.
nslookup api.steampowered.com
echo.

echo 2. Проверка доступности Steam серверов...
ping -n 1 steamcommunity.com
ping -n 1 api.steampowered.com
echo.

echo 3. Проверка портов Steam...
echo Попытка подключения к порту 443 (HTTPS)...
telnet steamcommunity.com 443
echo.

echo 4. Проверка WebSocket соединения...
echo Попытка подключения к WebSocket порту...
telnet steamcommunity.com 80
echo.

echo 5. Проверка прокси настроек...
echo HTTP_PROXY: %HTTP_PROXY%
echo HTTPS_PROXY: %HTTPS_PROXY%
echo NO_PROXY: %NO_PROXY%
echo.

echo 6. Проверка файрвола Windows...
netsh advfirewall firewall show rule name="Steam" dir=in
echo.

echo 7. Проверка антивируса...
echo Проверьте, не блокирует ли антивирус SteamTradeConfirmer.exe
echo.

echo ========================================
echo РЕКОМЕНДАЦИИ ПО УСТРАНЕНИЮ ПРОБЛЕМ:
echo ========================================
echo.
echo Если порты недоступны:
echo 1. Отключите антивирус временно
echo 2. Добавьте SteamTradeConfirmer.exe в исключения
echo 3. Проверьте настройки файрвола Windows
echo 4. Попробуйте другую сеть (мобильный интернет)
echo.
echo Если DNS не разрешается:
echo 1. Смените DNS на 8.8.8.8 и 8.8.4.4
echo 2. Очистите кэш DNS: ipconfig /flushdns
echo.
echo Если есть прокси:
echo 1. Отключите прокси временно
echo 2. Настройте прокси в Windows
echo.
pause
