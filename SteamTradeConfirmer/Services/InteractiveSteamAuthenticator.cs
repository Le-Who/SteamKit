using System;
using System.Threading.Tasks;
using SteamKit2.Authentication;
using SteamTradeConfirmer.Models;

namespace SteamTradeConfirmer.Services
{
    public class InteractiveSteamAuthenticator : IAuthenticator
    {
        private readonly SteamAccount _account;
        private readonly string _maFileContent;

        public InteractiveSteamAuthenticator(SteamAccount account)
        {
            _account = account;
            _maFileContent = LoadMaFile();
        }

        private string LoadMaFile()
        {
            if (string.IsNullOrEmpty(_account.MaFilePath) || !System.IO.File.Exists(_account.MaFilePath))
            {
                return string.Empty;
            }

            try
            {
                return System.IO.File.ReadAllText(_account.MaFilePath);
            }
            catch (Exception ex)
            {
                _account.ErrorMessage = $"Ошибка чтения .maFile: {ex.Message}";
                return string.Empty;
            }
        }

        public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            LoggingService.Instance.LogInfo($"🔑 === ЗАПРОС КОДА УСТРОЙСТВА === (предыдущий код был неверным: {previousCodeWasIncorrect})", _account.Username);
            
            if (string.IsNullOrEmpty(_maFileContent))
            {
                LoggingService.Instance.LogWarning("⚠️ .maFile не найден, запрашиваем код у пользователя", _account.Username);
                // Если нет .maFile, запрашиваем код у пользователя
                return await RequestCodeFromUserAsync("Введите код из мобильного приложения Steam:");
            }

            try
            {
                LoggingService.Instance.LogInfo("📄 Парсинг .maFile...", _account.Username);
                var maFile = System.Text.Json.JsonSerializer.Deserialize<MaFile>(_maFileContent);
                if (maFile == null)
                {
                    LoggingService.Instance.LogError("❌ Неверный формат .maFile", _account.Username);
                    return await RequestCodeFromUserAsync("Неверный формат .maFile. Введите код вручную:");
                }

                LoggingService.Instance.LogInfo("🔐 Генерируем TOTP код из .maFile...", _account.Username);
                // Генерируем TOTP код
                var totp = new TOTPGenerator(maFile.SharedSecret);
                var code = totp.GenerateCode();
                
                LoggingService.Instance.LogInfo($"✅ TOTP код сгенерирован успешно: {code}", _account.Username);
                // Обновляем статус аккаунта
                _account.Status = $"Сгенерирован код: {code}";
                
                return code;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"💥 ОШИБКА генерации TOTP кода: {ex.Message}", _account.Username, ex);
                _account.ErrorMessage = $"Ошибка генерации кода: {ex.Message}";
                return await RequestCodeFromUserAsync($"Ошибка генерации кода. Введите код вручную: {ex.Message}");
            }
        }

        public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            LoggingService.Instance.LogInfo($"Запрос email кода для {email} (предыдущий код был неверным: {previousCodeWasIncorrect})", _account.Username);
            return await RequestCodeFromUserAsync($"Введите код подтверждения, отправленный на {email}:");
        }

        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            // Не поддерживаем подтверждение через мобильное приложение
            return Task.FromResult(false);
        }

        private async Task<string> RequestCodeFromUserAsync(string message)
        {
            return await Task.Run(() =>
            {
                var dialog = new ConfirmationDialog(_account.Username, message);
                dialog.Title = "Подтверждение Steam Guard";
                
                // Показываем диалог в UI потоке
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    dialog.ShowDialog();
                });

                return dialog.ConfirmationCode;
            });
        }
    }

    public class MaFile
    {
        public string SharedSecret { get; set; } = string.Empty;
        public string IdentitySecret { get; set; } = string.Empty;
        public string RevocationCode { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public string ServerTime { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string TokenGid { get; set; } = string.Empty;
        public string Identity { get; set; } = string.Empty;
        public string Secret1 { get; set; } = string.Empty;
        public int Status { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string FullyEnrolled { get; set; } = string.Empty;
        public string SessionData { get; set; } = string.Empty;
    }
}
