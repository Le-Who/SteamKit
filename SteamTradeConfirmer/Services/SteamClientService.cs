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
                var steamClient = new SteamClient();
                LoggingService.Instance.LogInfo($"SteamClient создан. IsConnected: {steamClient.IsConnected}", account.Username);
                
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
                LoggingService.Instance.LogInfo("Подписка на события завершена", account.Username);

                // Запускаем обработку колбэков в отдельном потоке
                LoggingService.Instance.LogInfo("Запуск цикла обработки колбэков в отдельном потоке", account.Username);
                _ = Task.Run(() => RunCallbackLoop(manager, account));

                // Проверяем состояние перед подключением
                LoggingService.Instance.LogInfo($"Состояние перед подключением - IsConnected: {steamClient.IsConnected}", account.Username);
                
                LoggingService.Instance.LogInfo("Вызов steamClient.Connect()...", account.Username);
                steamClient.Connect();
                LoggingService.Instance.LogInfo($"Команда подключения отправлена. IsConnected: {steamClient.IsConnected}", account.Username);
                
                // Ждем подключения с таймаутом
                LoggingService.Instance.LogInfo("Ожидание подключения (таймаут 10 секунд)...", account.Username);
                var connected = await WaitForConnection(account, 10000);
                
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
            if (callback.Result == EResult.OK)
            {
                LoggingService.Instance.LogInfo($"Успешный вход в Steam: {callback.Result}", account.Username);
                account.Status = "Подключен";
                account.IsAuthenticated = true;
                account.DisplayName = account.Username; // PersonaName будет получен позже через SteamFriends
                AccountAuthenticated?.Invoke(this, account);
            }
            else
            {
                LoggingService.Instance.LogError($"Ошибка входа в Steam: {callback.Result} / {callback.ExtendedResult}", account.Username);
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
                
                while (_clients.ContainsKey(account) && _clients[account].IsConnected)
                {
                    iterationCount++;
                    if (iterationCount % 5 == 0) // Логируем каждые 5 секунд
                    {
                        LoggingService.Instance.LogInfo($"Цикл обработки колбэков активен (итерация {iterationCount}), IsConnected: {_clients[account].IsConnected}", account.Username);
                    }
                    
                    manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
                
                LoggingService.Instance.LogInfo($"🛑 Цикл обработки колбэков завершен после {iterationCount} итераций. IsConnected: {(_clients.ContainsKey(account) ? _clients[account].IsConnected.ToString() : "N/A")}", account.Username);
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
