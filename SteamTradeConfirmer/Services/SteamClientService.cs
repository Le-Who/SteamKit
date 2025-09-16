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
                LoggingService.Instance.LogInfo($"Начинаем аутентификацию аккаунта: {account.Username}", account.Username);
                account.Status = "Подключение...";
                account.ErrorMessage = string.Empty;

                var steamClient = new SteamClient();
                var manager = new CallbackManager(steamClient);
                var steamUser = steamClient.GetHandler<SteamUser>();

                _clients[account] = steamClient;
                _managers[account] = manager;
                _steamUsers[account] = steamUser;

                LoggingService.Instance.LogInfo("SteamClient создан, подписываемся на события", account.Username);

                // Подписываемся на события
                manager.Subscribe<SteamClient.ConnectedCallback>(callback => OnConnected(callback, account));
                manager.Subscribe<SteamClient.DisconnectedCallback>(callback => OnDisconnected(callback, account));
                manager.Subscribe<SteamUser.LoggedOnCallback>(callback => OnLoggedOn(callback, account));
                manager.Subscribe<SteamUser.LoggedOffCallback>(callback => OnLoggedOff(callback, account));
                // Machine Auth пока отключен - может быть добавлен позже при необходимости

                LoggingService.Instance.LogInfo("События подписаны, запускаем обработку колбэков", account.Username);

                // Запускаем обработку колбэков в отдельном потоке
                _ = Task.Run(() => RunCallbackLoop(manager, account));

                LoggingService.Instance.LogInfo("Подключаемся к Steam...", account.Username);
                // Подключаемся к Steam
                steamClient.Connect();

                LoggingService.Instance.LogInfo("Команда подключения отправлена", account.Username);
                
                // Ждем подключения с таймаутом
                var connected = await WaitForConnection(account, 10000); // 10 секунд на подключение
                if (!connected)
                {
                    LoggingService.Instance.LogError("Таймаут подключения к Steam (10 секунд)", account.Username);
                    account.Status = "Таймаут подключения";
                    account.ErrorMessage = "Не удалось подключиться к Steam в течение 10 секунд";
                    AuthenticationFailed?.Invoke(this, (account, "Таймаут подключения"));
                    return false;
                }

                // После подключения начинаем аутентификацию
                await BeginAuthentication(account);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Ошибка при инициализации подключения: {ex.Message}", account.Username, ex);
                account.Status = "Ошибка подключения";
                account.ErrorMessage = ex.Message;
                AuthenticationFailed?.Invoke(this, (account, ex.Message));
                return false;
            }
        }

        private readonly Dictionary<SteamAccount, TaskCompletionSource<bool>> _connectionWaiters = new();

        private async void OnConnected(SteamClient.ConnectedCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogInfo("Подключение к Steam установлено", account.Username);
            
            // Уведомляем о подключении
            if (_connectionWaiters.TryGetValue(account, out var waiter))
            {
                waiter.SetResult(true);
                _connectionWaiters.Remove(account);
            }
        }

        private async Task<bool> WaitForConnection(SteamAccount account, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<bool>();
            _connectionWaiters[account] = tcs;

            try
            {
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _connectionWaiters.Remove(account);
                    return false;
                }
                
                return await tcs.Task;
            }
            catch
            {
                _connectionWaiters.Remove(account);
                return false;
            }
        }

        private async Task BeginAuthentication(SteamAccount account)
        {
            try
            {
                LoggingService.Instance.LogInfo("Начинаем аутентификацию через Steam", account.Username);
                account.Status = "Аутентификация...";
                account.ErrorMessage = string.Empty;

                var steamUser = _steamUsers[account];
                
                // Используем новый API аутентификации
                var authSession = await _clients[account].Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = account.Username,
                    Password = account.Password,
                    IsPersistentSession = false,
                    Authenticator = new InteractiveSteamAuthenticator(account)
                });

                LoggingService.Instance.LogInfo("Сессия аутентификации создана, ожидаем подтверждения", account.Username);
                account.Status = "Ожидание подтверждения...";
                
                var pollResponse = await authSession.PollingWaitForResultAsync();

                LoggingService.Instance.LogInfo($"Получен ответ аутентификации: {pollResponse.AccountName}", account.Username);
                account.Status = "Вход в Steam...";
                
                steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                    ShouldRememberPassword = false
                });
                
                LoggingService.Instance.LogInfo("Команда входа в Steam отправлена", account.Username);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Ошибка аутентификации: {ex.Message}", account.Username, ex);
                account.Status = "Ошибка аутентификации";
                account.ErrorMessage = ex.Message;
                AuthenticationFailed?.Invoke(this, (account, ex.Message));
            }
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback, SteamAccount account)
        {
            LoggingService.Instance.LogWarning("Отключение от Steam", account.Username);
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
                LoggingService.Instance.LogInfo("Запуск цикла обработки колбэков", account.Username);
                while (_clients.ContainsKey(account) && _clients[account].IsConnected)
                {
                    manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
                LoggingService.Instance.LogInfo("Цикл обработки колбэков завершен", account.Username);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Ошибка в цикле обработки колбэков: {ex.Message}", account.Username, ex);
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
