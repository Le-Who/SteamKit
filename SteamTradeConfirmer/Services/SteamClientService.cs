using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Authentication;
using SteamTradeConfirmer.Models;

namespace SteamTradeConfirmer.Services
{
    public class SteamClientService
    {
        private readonly Dictionary<SteamAccount, SteamClient> _clients = new();
        private readonly Dictionary<SteamAccount, CallbackManager> _managers = new();
        private readonly Dictionary<SteamAccount, SteamUser> _steamUsers = new();

        public event EventHandler<SteamAccount>? AccountAuthenticated;
        public event EventHandler<SteamAccount>? AccountDisconnected;
        public event EventHandler<(SteamAccount account, string error)>? AuthenticationFailed;

        public async Task<bool> AuthenticateAccountAsync(SteamAccount account)
        {
            try
            {
                LoggingService.Instance.LogInfo($"=== НАЧАЛО АУТЕНТИФИКАЦИИ АККАУНТА: {account.Username} ===", account.Username);
                account.Status = "Подключение...";
                account.ErrorMessage = string.Empty;

                // Создание SteamClient с детальным логированием
                LoggingService.Instance.LogInfo("Создание SteamClient...", account.Username);
                
                // Создаем конфигурацию SteamKit2 с диагностикой
                var config = SteamConfiguration.Create(builder => { });
                LoggingService.Instance.LogInfo($"SteamConfiguration создана. WebAPIBaseAddress: {config.WebAPIBaseAddress}", account.Username);
                
                // Попробуем альтернативную конфигурацию с явными серверами
                LoggingService.Instance.LogInfo("🔧 Попытка альтернативной конфигурации SteamKit2...", account.Username);
                try
                {
                    var alternativeConfig = SteamConfiguration.Create(builder =>
                    {
                        builder.WithWebAPIBaseAddress(new Uri("https://api.steampowered.com/"));
                        builder.WithDirectoryFetch(false); // Отключаем автоматическое получение списка серверов
                        builder.WithConnectionTimeout(TimeSpan.FromSeconds(30)); // Увеличиваем таймаут
                        builder.WithProtocolTypes(ProtocolTypes.WebSocket); // Используем только WebSocket
                    });
                    LoggingService.Instance.LogInfo("✅ Альтернативная конфигурация создана (WebSocket, 30s timeout)", account.Username);
                    config = alternativeConfig;
                }
                catch (Exception configEx)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Ошибка создания альтернативной конфигурации: {configEx.Message}", account.Username);
                }
                
                // Проверяем доступность Steam серверов
                LoggingService.Instance.LogInfo("🔍 Проверка доступности Steam серверов...", account.Username);
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(3);
                    var steamResponse = await httpClient.GetAsync("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/");
                    LoggingService.Instance.LogInfo($"🌐 Steam API доступен: {steamResponse.StatusCode}", account.Username);
                }
                catch (Exception steamEx)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Steam API недоступен: {steamEx.Message}", account.Username);
                }
                
                var steamClient = new SteamClient(config);
                LoggingService.Instance.LogInfo($"SteamClient создан с конфигурацией. IsConnected: {steamClient.IsConnected}", account.Username);
                
                var manager = new CallbackManager(steamClient);
                var steamUser = steamClient.GetHandler<SteamUser>();
                LoggingService.Instance.LogInfo("CallbackManager и SteamUser инициализированы", account.Username);

                _clients[account] = steamClient;
                _managers[account] = manager;
                _steamUsers[account] = steamUser;

                LoggingService.Instance.LogInfo("SteamClient создан, подписываемся на события", account.Username);

                // Подписываемся на события с детальным логированием
                manager.Subscribe<SteamClient.ConnectedCallback>(callback => OnConnected(callback, account));
                manager.Subscribe<SteamClient.DisconnectedCallback>(callback => OnDisconnected(callback, account));
                manager.Subscribe<SteamUser.LoggedOnCallback>(callback => OnLoggedOn(callback, account));
                manager.Subscribe<SteamUser.LoggedOffCallback>(callback => OnLoggedOff(callback, account));
                manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(callback => OnUpdateMachineAuth(callback, account));
                manager.Subscribe<SteamUser.AccountInfoCallback>(callback => OnAccountInfo(callback, account));
                LoggingService.Instance.LogInfo("Подписка на события завершена", account.Username);

               // Проверяем состояние перед подключением
               LoggingService.Instance.LogInfo($"Состояние перед подключением - IsConnected: {steamClient.IsConnected}", account.Username);
                
                // Диагностика сетевых проблем
                LoggingService.Instance.LogInfo("🔍 Проверка сетевой доступности...", account.Username);
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var response = await httpClient.GetAsync("https://steamcommunity.com");
                    LoggingService.Instance.LogInfo($"🌐 Steam Community доступен: {response.StatusCode}", account.Username);
                }
                catch (Exception netEx)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Проблемы с сетью: {netEx.Message}", account.Username);
                }
                
                // Диагностика портов Steam
                LoggingService.Instance.LogInfo("🔍 Проверка портов Steam...", account.Username);
                try
                {
                    using var tcpClient = new System.Net.Sockets.TcpClient();
                    var connectTask = tcpClient.ConnectAsync("steamcommunity.com", 443);
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == connectTask && tcpClient.Connected)
                    {
                        LoggingService.Instance.LogInfo("✅ Порт 443 (HTTPS) доступен", account.Username);
                    }
                    else
                    {
                        LoggingService.Instance.LogWarning("⚠️ Порт 443 (HTTPS) недоступен или таймаут", account.Username);
                    }
                }
                catch (Exception portEx)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Ошибка проверки портов: {portEx.Message}", account.Username);
                }
                
               LoggingService.Instance.LogInfo("Вызов steamClient.Connect()...", account.Username);
               
               try
               {
                   LoggingService.Instance.LogInfo("🔌 Попытка подключения с WebSocket протоколом...", account.Username);
                   steamClient.Connect();
                   LoggingService.Instance.LogInfo($"Команда подключения отправлена. IsConnected: {steamClient.IsConnected}", account.Username);
                   
                   // Запускаем обработку колбэков ПОСЛЕ вызова Connect()
                   LoggingService.Instance.LogInfo("Запуск цикла обработки колбэков в отдельном потоке", account.Username);
                   _ = Task.Run(() => RunCallbackLoop(manager, account));
                   
                   // Даем время на подключение
                   await Task.Delay(2000);
                   LoggingService.Instance.LogInfo($"Состояние через 2 секунды: IsConnected: {steamClient.IsConnected}", account.Username);
               }
                catch (Exception connectEx)
                {
                    LoggingService.Instance.LogError($"ОШИБКА при вызове steamClient.Connect(): {connectEx.Message}", account.Username, connectEx);
                    
                    // Попробуем с TCP протоколом
                    LoggingService.Instance.LogInfo("🔄 Попытка подключения с TCP протоколом...", account.Username);
                    try
                    {
                        var tcpConfig = SteamConfiguration.Create(builder =>
                        {
                            builder.WithWebAPIBaseAddress(new Uri("https://api.steampowered.com/"));
                            builder.WithDirectoryFetch(false);
                            builder.WithConnectionTimeout(TimeSpan.FromSeconds(30));
                            builder.WithProtocolTypes(ProtocolTypes.Tcp); // Используем только TCP
                        });
                        
                        var tcpClient = new SteamClient(tcpConfig);
                        var tcpManager = new CallbackManager(tcpClient);
                        var tcpSteamUser = tcpClient.GetHandler<SteamUser>();
                        
                        _clients[account] = tcpClient;
                        _managers[account] = tcpManager;
                        _steamUsers[account] = tcpSteamUser;
                        
                       tcpManager.Subscribe<SteamClient.ConnectedCallback>(callback => OnConnected(callback, account));
                       tcpManager.Subscribe<SteamClient.DisconnectedCallback>(callback => OnDisconnected(callback, account));
                       tcpManager.Subscribe<SteamUser.LoggedOnCallback>(callback => OnLoggedOn(callback, account));
                       tcpManager.Subscribe<SteamUser.LoggedOffCallback>(callback => OnLoggedOff(callback, account));
                       tcpManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(callback => OnUpdateMachineAuth(callback, account));
                       tcpManager.Subscribe<SteamUser.AccountInfoCallback>(callback => OnAccountInfo(callback, account));
                       
                       tcpClient.Connect();
                       LoggingService.Instance.LogInfo($"TCP подключение отправлено. IsConnected: {tcpClient.IsConnected}", account.Username);
                       
                       // Запускаем обработку колбэков ПОСЛЕ вызова Connect()
                       _ = Task.Run(() => RunCallbackLoop(tcpManager, account));
                    }
                    catch (Exception tcpEx)
                    {
                        LoggingService.Instance.LogError($"ОШИБКА TCP подключения: {tcpEx.Message}", account.Username, tcpEx);
                        account.Status = "Ошибка подключения";
                        account.ErrorMessage = $"Ошибка подключения: {connectEx.Message}";
                        AuthenticationFailed?.Invoke(this, (account, connectEx.Message));
                        return false;
                    }
                }
                
               // Ждем подключения с увеличенным таймаутом
               LoggingService.Instance.LogInfo("Ожидание подключения (таймаут 30 секунд)...", account.Username);
               var connected = await WaitForConnection(account, 30000);
                
                if (!connected)
                {
                    LoggingService.Instance.LogError($"ТАЙМАУТ ПОДКЛЮЧЕНИЯ! Финальное состояние - IsConnected: {steamClient.IsConnected}", account.Username);
                    account.Status = "Таймаут подключения";
                    account.ErrorMessage = "Не удалось подключиться к Steam в течение 10 секунд";
                    AuthenticationFailed?.Invoke(this, (account, "Таймаут подключения"));
                    return false;
                }

                LoggingService.Instance.LogInfo("Подключение установлено, начинаем аутентификацию", account.Username);
                // После подключения начинаем аутентификацию
                await BeginAuthentication(account);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"КРИТИЧЕСКАЯ ОШИБКА при инициализации подключения: {ex.Message}", account.Username, ex);
                account.Status = "Ошибка подключения";
                account.ErrorMessage = ex.Message;
                AuthenticationFailed?.Invoke(this, (account, ex.Message));
                return false;
            }
        }

        private readonly Dictionary<SteamAccount, TaskCompletionSource<bool>> _connectionWaiters = new();

        private async void OnConnected(SteamClient.ConnectedCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogInfo("🎉 ПОДКЛЮЧЕНИЕ К STEAM УСТАНОВЛЕНО! 🎉", account.Username);
            
            // Уведомляем о подключении
            if (_connectionWaiters.TryGetValue(account, out var waiter))
            {
                LoggingService.Instance.LogInfo("Уведомляем ожидающий поток о подключении", account.Username);
                waiter.SetResult(true);
                _connectionWaiters.Remove(account);
            }
            else
            {
                LoggingService.Instance.LogWarning("Получен ConnectedCallback, но нет ожидающих потоков", account.Username);
            }
        }

        private async Task<bool> WaitForConnection(SteamAccount account, int timeoutMs)
        {
            LoggingService.Instance.LogInfo($"Настройка ожидания подключения (таймаут: {timeoutMs}ms)", account.Username);
            var tcs = new TaskCompletionSource<bool>();
            _connectionWaiters[account] = tcs;

            try
            {
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    LoggingService.Instance.LogError($"ТАЙМАУТ ОЖИДАНИЯ ПОДКЛЮЧЕНИЯ ({timeoutMs}ms)", account.Username);
                    _connectionWaiters.Remove(account);
                    return false;
                }
                
                LoggingService.Instance.LogInfo("Получено уведомление о подключении", account.Username);
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Ошибка в WaitForConnection: {ex.Message}", account.Username, ex);
                _connectionWaiters.Remove(account);
                return false;
            }
        }

        private async Task BeginAuthentication(SteamAccount account)
        {
            try
            {
                LoggingService.Instance.LogInfo("🔐 === НАЧАЛО АУТЕНТИФИКАЦИИ ЧЕРЕЗ STEAM ===", account.Username);
                account.Status = "Аутентификация...";
                account.ErrorMessage = string.Empty;

                var steamUser = _steamUsers[account];
                LoggingService.Instance.LogInfo("SteamUser получен, создаем сессию аутентификации", account.Username);
                
                // Используем новый API аутентификации
                LoggingService.Instance.LogInfo("Вызов BeginAuthSessionViaCredentialsAsync...", account.Username);
                var authSession = await _clients[account].Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = account.Username,
                    Password = account.Password,
                    IsPersistentSession = false,
                    Authenticator = new InteractiveSteamAuthenticator(account)
                });

                LoggingService.Instance.LogInfo("✅ Сессия аутентификации создана успешно, начинаем опрос", account.Username);
                account.Status = "Ожидание подтверждения...";
                
                LoggingService.Instance.LogInfo("Вызов PollingWaitForResultAsync...", account.Username);
                var pollResponse = await authSession.PollingWaitForResultAsync();

                LoggingService.Instance.LogInfo($"🎉 Получен ответ аутентификации! AccountName: {pollResponse.AccountName}", account.Username);
                account.Status = "Вход в Steam...";
                
                LoggingService.Instance.LogInfo("Отправка команды LogOn в Steam...", account.Username);
                steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                    ShouldRememberPassword = false
                });
                
                LoggingService.Instance.LogInfo("✅ Команда входа в Steam отправлена успешно", account.Username);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"💥 КРИТИЧЕСКАЯ ОШИБКА аутентификации: {ex.Message}", account.Username, ex);
                account.Status = "Ошибка аутентификации";
                account.ErrorMessage = ex.Message;
                AuthenticationFailed?.Invoke(this, (account, ex.Message));
            }
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogWarning($"❌ ОТКЛЮЧЕНИЕ ОТ STEAM! UserInitiated: {callback.UserInitiated}", account.Username);
            account.Status = "Отключен";
            account.IsAuthenticated = false;
            AccountDisconnected?.Invoke(this, account);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogInfo($"🔐 Результат входа в Steam: {callback.Result}", account.Username);
            
            if (callback.Result == EResult.OK)
            {
                LoggingService.Instance.LogInfo($"✅ Успешный вход в Steam: {account.Username}", account.Username);
                LoggingService.Instance.LogInfo($"Steam ID: {callback.ClientSteamID}", account.Username);
                LoggingService.Instance.LogInfo($"Out of Game Heartbeat Seconds: {callback.OutOfGameHeartbeatSeconds}", account.Username);
                LoggingService.Instance.LogInfo($"In Game Heartbeat Seconds: {callback.InGameHeartbeatSeconds}", account.Username);
                
                account.Status = "Подключен";
                account.IsAuthenticated = true;
                account.DisplayName = account.Username; // PersonaName будет получен позже через SteamFriends
                account.ErrorMessage = string.Empty;
                AccountAuthenticated?.Invoke(this, account);
            }
            else if (callback.Result == EResult.AccountLogonDenied)
            {
                LoggingService.Instance.LogWarning($"⚠️ Требуется подтверждение входа: {callback.Result}", account.Username);
                account.Status = "Требуется подтверждение";
                account.ErrorMessage = "Проверьте email или мобильное приложение Steam";
                AuthenticationFailed?.Invoke(this, (account, "Требуется подтверждение входа"));
            }
            else if (callback.Result == EResult.InvalidPassword)
            {
                LoggingService.Instance.LogError($"❌ Неверный пароль: {callback.Result}", account.Username);
                account.Status = "Неверный пароль";
                account.ErrorMessage = "Проверьте правильность пароля";
                AuthenticationFailed?.Invoke(this, (account, "Неверный пароль"));
            }
            else if (callback.Result == EResult.TwoFactorCodeMismatch)
            {
                LoggingService.Instance.LogError($"❌ Неверный код Steam Guard: {callback.Result}", account.Username);
                account.Status = "Неверный код";
                account.ErrorMessage = "Проверьте правильность кода Steam Guard";
                AuthenticationFailed?.Invoke(this, (account, "Неверный код Steam Guard"));
            }
            else if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                LoggingService.Instance.LogWarning($"⚠️ Требуется код Steam Guard: {callback.Result}", account.Username);
                account.Status = "Требуется код";
                account.ErrorMessage = "Введите код из мобильного приложения Steam";
                AuthenticationFailed?.Invoke(this, (account, "Требуется код Steam Guard"));
            }
            else
            {
                LoggingService.Instance.LogError($"❌ Ошибка входа в Steam: {callback.Result} / {callback.ExtendedResult}", account.Username);
                account.Status = "Ошибка входа";
                account.ErrorMessage = $"Не удалось войти: {callback.Result}";
                AuthenticationFailed?.Invoke(this, (account, account.ErrorMessage));
            }
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogInfo($"Выход из Steam: {callback.Result}", account.Username);
            account.Status = "Выход выполнен";
            account.IsAuthenticated = false;
        }

        private void OnUpdateMachineAuth(SteamUser.UpdateMachineAuthCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogInfo($"🔐 Получен запрос Machine Auth для {account.Username}", account.Username);
            LoggingService.Instance.LogInfo($"Machine Auth: {callback.Data.Length} байт данных", account.Username);
            
            // Отправляем подтверждение Machine Auth
            var steamUser = _steamUsers[account];
            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,
                FileName = callback.FileName,
                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = callback.OneTimePassword,
                SentryFileHash = callback.Data,
                FileName = callback.FileName
            });
            
            LoggingService.Instance.LogInfo($"✅ Machine Auth подтвержден для {account.Username}", account.Username);
        }

        private void OnAccountInfo(SteamUser.AccountInfoCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogInfo($"📋 Получена информация об аккаунте {account.Username}", account.Username);
            LoggingService.Instance.LogInfo($"Persona Name: {callback.PersonaName}", account.Username);
            LoggingService.Instance.LogInfo($"Country: {callback.Country}", account.Username);
            LoggingService.Instance.LogInfo($"Count Authed Computers: {callback.CountAuthedComputers}", account.Username);
        }


        private void RunCallbackLoop(CallbackManager manager, SteamAccount account)
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 Запуск цикла обработки колбэков", account.Username);
                var iterationCount = 0;
                var lastIsConnected = false;
                
                while (_clients.ContainsKey(account) && _clients[account].IsConnected)
                {
                    iterationCount++;
                    var currentIsConnected = _clients[account].IsConnected;
                    
                    // Логируем изменение состояния подключения
                    if (currentIsConnected != lastIsConnected)
                    {
                        LoggingService.Instance.LogInfo($"🔄 Изменение состояния подключения: {lastIsConnected} -> {currentIsConnected} (итерация {iterationCount})", account.Username);
                        lastIsConnected = currentIsConnected;
                    }
                    
                    if (iterationCount % 5 == 0) // Логируем каждые 5 секунд
                    {
                        LoggingService.Instance.LogInfo($"Цикл обработки колбэков активен (итерация {iterationCount}), IsConnected: {_clients[account].IsConnected}", account.Username);
                    }
                    
                    try
                    {
                        manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception callbackEx)
                    {
                        LoggingService.Instance.LogError($"Ошибка в RunWaitCallbacks: {callbackEx.Message}", account.Username, callbackEx);
                    }
                }
                
                LoggingService.Instance.LogInfo($"🛑 Цикл обработки колбэков завершен после {iterationCount} итераций. IsConnected: {(_clients.ContainsKey(account) ? _clients[account].IsConnected.ToString() : "N/A")}", account.Username);
                
                // Дополнительная диагностика
                if (iterationCount == 0)
                {
                    LoggingService.Instance.LogError("🚨 КРИТИЧЕСКАЯ ПРОБЛЕМА: CallbackManager завершился немедленно! Это означает, что SteamClient.IsConnected = false сразу после Connect()", account.Username);
                    LoggingService.Instance.LogError("🔍 Возможные причины: 1) Сетевые проблемы 2) Блокировка портов Steam 3) Проблемы с DNS 4) Проблемы с прокси/файрволом", account.Username);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"💥 КРИТИЧЕСКАЯ ОШИБКА в цикле обработки колбэков: {ex.Message}", account.Username, ex);
                account.Status = "Ошибка обработки";
                account.ErrorMessage = ex.Message;
            }
        }

        public void DisconnectAccount(SteamAccount account)
        {
            if (_clients.TryGetValue(account, out var client))
            {
                client.Disconnect();
                _clients.Remove(account);
                _managers.Remove(account);
                _steamUsers.Remove(account);
            }
        }

        public void DisconnectAll()
        {
            foreach (var account in _clients.Keys.ToList())
            {
                DisconnectAccount(account);
            }
        }

        public SteamClient? GetClient(SteamAccount account)
        {
            return _clients.TryGetValue(account, out var client) ? client : null;
        }

        public SteamUser? GetSteamUser(SteamAccount account)
        {
            return _steamUsers.TryGetValue(account, out var user) ? user : null;
        }
    }
}
