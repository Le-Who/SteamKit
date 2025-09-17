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

                // Создание SteamClient с правильной конфигурацией
                LoggingService.Instance.LogInfo("Создание SteamClient...", account.Username);
                
                // Используем стандартную конфигурацию SteamKit2 с поддержкой всех протоколов
                var config = SteamConfiguration.Create(builder =>
                {
                    builder.WithWebAPIBaseAddress(new Uri("https://api.steampowered.com/"));
                    builder.WithConnectionTimeout(TimeSpan.FromSeconds(30));
                    // Разрешаем все протоколы для лучшей совместимости
                    builder.WithProtocolTypes(ProtocolTypes.WebSocket | ProtocolTypes.Tcp);
                });
                LoggingService.Instance.LogInfo($"SteamConfiguration создана. WebAPIBaseAddress: {config.WebAPIBaseAddress}", account.Username);
                
                var steamClient = new SteamClient(config);
                LoggingService.Instance.LogInfo($"SteamClient создан. IsConnected: {steamClient.IsConnected}", account.Username);
                
                var manager = new CallbackManager(steamClient);
                var steamUser = steamClient.GetHandler<SteamUser>();
                LoggingService.Instance.LogInfo("CallbackManager и SteamUser инициализированы", account.Username);

                if (steamUser == null)
                {
                    LoggingService.Instance.LogError("Не удалось получить SteamUser handler", account.Username);
                    account.Status = "Ошибка инициализации";
                    account.ErrorMessage = "Не удалось получить SteamUser handler";
                    AuthenticationFailed?.Invoke(this, (account, "Ошибка инициализации SteamUser"));
                    return false;
                }

                _clients[account] = steamClient;
                _managers[account] = manager;
                _steamUsers[account] = steamUser;

                LoggingService.Instance.LogInfo("SteamClient создан, подписываемся на события", account.Username);

                // Подписываемся на события
                manager.Subscribe<SteamClient.ConnectedCallback>(callback => OnConnected(callback, account));
                manager.Subscribe<SteamClient.DisconnectedCallback>(callback => OnDisconnected(callback, account));
                manager.Subscribe<SteamUser.LoggedOnCallback>(callback => OnLoggedOn(callback, account));
                manager.Subscribe<SteamUser.LoggedOffCallback>(callback => OnLoggedOff(callback, account));
                LoggingService.Instance.LogInfo("Подписка на события завершена", account.Username);

                // Запускаем callback loop в отдельном потоке ПЕРЕД подключением
                LoggingService.Instance.LogInfo("Запуск callback loop в отдельном потоке", account.Username);
                _ = Task.Run(() => RunCallbackLoop(manager, account));

                // Инициируем подключение
                LoggingService.Instance.LogInfo("Вызов steamClient.Connect()...", account.Username);
                steamClient.Connect();
                LoggingService.Instance.LogInfo($"Команда подключения отправлена. IsConnected: {steamClient.IsConnected}", account.Username);
                
                // Ждем подключения с таймаутом
                LoggingService.Instance.LogInfo("Ожидание подключения (таймаут 30 секунд)...", account.Username);
                var connected = await WaitForConnection(account, 30000);
                
                if (!connected)
                {
                    LoggingService.Instance.LogError($"ТАЙМАУТ ПОДКЛЮЧЕНИЯ! Финальное состояние - IsConnected: {steamClient.IsConnected}", account.Username);
                    account.Status = "Таймаут подключения";
                    account.ErrorMessage = "Не удалось подключиться к Steam в течение 30 секунд";
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



        private void RunCallbackLoop(CallbackManager manager, SteamAccount account)
        {
            try
            {
                LoggingService.Instance.LogInfo("🔄 Запуск цикла обработки колбэков", account.Username);
                var iterationCount = 0;
                
                // Цикл должен работать пока клиент существует, независимо от состояния подключения
                while (_clients.ContainsKey(account))
                {
                    iterationCount++;
                    
                    if (iterationCount % 10 == 0) // Логируем каждые 10 секунд
                    {
                        var isConnected = _clients.ContainsKey(account) ? _clients[account].IsConnected : false;
                        LoggingService.Instance.LogInfo($"Цикл обработки колбэков активен (итерация {iterationCount}), IsConnected: {isConnected}", account.Username);
                    }
                    
                    try
                    {
                        // Используем RunWaitCallbacks с таймаутом 1 секунда
                        manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception callbackEx)
                    {
                        LoggingService.Instance.LogError($"Ошибка в RunWaitCallbacks: {callbackEx.Message}", account.Username, callbackEx);
                        // Не прерываем цикл при ошибке, продолжаем обработку
                    }
                }
                
                LoggingService.Instance.LogInfo($"🛑 Цикл обработки колбэков завершен после {iterationCount} итераций", account.Username);
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
