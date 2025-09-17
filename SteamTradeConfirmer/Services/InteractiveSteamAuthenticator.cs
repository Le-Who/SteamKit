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
        private string _cachedCode = string.Empty;
        private DateTime _codeGeneratedAt = DateTime.MinValue;

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
            
            // Если предыдущий код был неправильным, запрашиваем ручной ввод
            if (previousCodeWasIncorrect)
            {
                LoggingService.Instance.LogWarning($"⚠️ Предыдущий TOTP код был неправильным, запрашиваем ручной ввод", _account.Username);
                // Очищаем кэш неправильного кода
                _cachedCode = string.Empty;
                _codeGeneratedAt = DateTime.MinValue;
                return await RequestCodeFromUserAsync("TOTP код был неправильным. Введите код вручную из мобильного приложения Steam:");
            }
            
            // Проверяем кэшированный код (действителен 30 секунд)
            if (!string.IsNullOrEmpty(_cachedCode) && 
                DateTime.Now.Subtract(_codeGeneratedAt).TotalSeconds < 30)
            {
                LoggingService.Instance.LogInfo($"🔄 Используем кэшированный TOTP код: {_cachedCode}", _account.Username);
                return _cachedCode;
            }
            
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
                
                // Проверяем наличие SharedSecret
                if (string.IsNullOrEmpty(maFile.SharedSecret))
                {
                    LoggingService.Instance.LogError("❌ SharedSecret отсутствует в .maFile", _account.Username);
                    return await RequestCodeFromUserAsync("SharedSecret отсутствует в .maFile. Введите код вручную:");
                }
                
                LoggingService.Instance.LogInfo($"🔍 SharedSecret найден: {maFile.SharedSecret.Substring(0, Math.Min(10, maFile.SharedSecret.Length))}...", _account.Username);
                
                // Генерируем TOTP код
                try
                {
                    var totp = new TOTPGenerator(maFile.SharedSecret);
                    var code = totp.GenerateCode();
                
                    // Кэшируем код на 30 секунд
                    _cachedCode = code;
                    _codeGeneratedAt = DateTime.Now;
                    
                    LoggingService.Instance.LogInfo($"✅ TOTP код сгенерирован и кэширован: {code}", _account.Username);
                    // Обновляем статус аккаунта
                    _account.Status = $"Сгенерирован код: {code}";
                    
                    return code;
                }
                catch (Exception totpEx)
                {
                    LoggingService.Instance.LogError($"💥 ОШИБКА генерации TOTP кода: {totpEx.Message}", _account.Username, totpEx);
                    LoggingService.Instance.LogError($"🔍 SharedSecret содержит недопустимые символы для Base32", _account.Username);
                    return await RequestCodeFromUserAsync($"Ошибка генерации TOTP кода. Введите код вручную: {totpEx.Message}");
                }
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
            string result = string.Empty;
            
            // Создаем и показываем диалог в UI потоке
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new ConfirmationDialog(_account.Username, message);
                dialog.Title = "Подтверждение Steam Guard";
                dialog.ShowDialog();
                result = dialog.ConfirmationCode;
            });

            return result;
        }
    }

    public class MaFile
    {
        [System.Text.Json.Serialization.JsonPropertyName("shared_secret")]
        public string SharedSecret { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("identity_secret")]
        public string IdentitySecret { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("revocation_code")]
        public string RevocationCode { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("server_time")]
        public object? ServerTime { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("account_name")]
        public string AccountName { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("token_gid")]
        public string TokenGid { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("identity")]
        public string Identity { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("secret_1")]
        public string Secret1 { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public int Status { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("device_id")]
        public string DeviceId { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("fully_enrolled")]
        public object? FullyEnrolled { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("session_data")]
        public string SessionData { get; set; } = string.Empty;
    }
}
