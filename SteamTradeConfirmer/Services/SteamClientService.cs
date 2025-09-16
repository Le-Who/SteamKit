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
                account.Status = "Подключение...";
                account.ErrorMessage = string.Empty;

                var steamClient = new SteamClient();
                var manager = new CallbackManager(steamClient);
                var steamUser = steamClient.GetHandler<SteamUser>();

                _clients[account] = steamClient;
                _managers[account] = manager;
                _steamUsers[account] = steamUser;

                // Подписываемся на события
                manager.Subscribe<SteamClient.ConnectedCallback>(callback => OnConnected(callback, account));
                manager.Subscribe<SteamClient.DisconnectedCallback>(callback => OnDisconnected(callback, account));
                manager.Subscribe<SteamUser.LoggedOnCallback>(callback => OnLoggedOn(callback, account));
                manager.Subscribe<SteamUser.LoggedOffCallback>(callback => OnLoggedOff(callback, account));

                // Запускаем обработку колбэков в отдельном потоке
                _ = Task.Run(() => RunCallbackLoop(manager, account));

                // Подключаемся к Steam
                steamClient.Connect();

                return true;
            }
            catch (Exception ex)
            {
                account.Status = "Ошибка подключения";
                account.ErrorMessage = ex.Message;
                AuthenticationFailed?.Invoke(this, (account, ex.Message));
                return false;
            }
        }

        private async void OnConnected(SteamClient.ConnectedCallback callback, SteamAccount account)
        {
            try
            {
                account.Status = "Аутентификация...";

                var steamUser = _steamUsers[account];
                var authSession = await _clients[account].Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = account.Username,
                    Password = account.Password,
                    IsPersistentSession = false,
                    Authenticator = new SteamAuthenticator(account)
                });

                var pollResponse = await authSession.PollingWaitForResultAsync();

                steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                    ShouldRememberPassword = false
                });
            }
            catch (Exception ex)
            {
                account.Status = "Ошибка аутентификации";
                account.ErrorMessage = ex.Message;
                AuthenticationFailed?.Invoke(this, (account, ex.Message));
            }
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback, SteamAccount account)
        {
            account.Status = "Отключен";
            account.IsAuthenticated = false;
            AccountDisconnected?.Invoke(this, account);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback, SteamAccount account)
        {
            if (callback.Result == EResult.OK)
            {
                account.Status = "Подключен";
                account.IsAuthenticated = true;
                account.DisplayName = account.Username; // PersonaName будет получен позже через SteamFriends
                AccountAuthenticated?.Invoke(this, account);
            }
            else
            {
                account.Status = "Ошибка входа";
                account.ErrorMessage = $"Не удалось войти: {callback.Result}";
                AuthenticationFailed?.Invoke(this, (account, account.ErrorMessage));
            }
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback, SteamAccount account)
        {
            account.Status = "Выход выполнен";
            account.IsAuthenticated = false;
        }

        private void RunCallbackLoop(CallbackManager manager, SteamAccount account)
        {
            try
            {
                while (_clients.ContainsKey(account) && _clients[account].IsConnected)
                {
                    manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex)
            {
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
