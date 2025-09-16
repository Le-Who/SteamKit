using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Authentication;
using SteamTradeConfirmer.Models;

namespace SteamTradeConfirmer.Services
{
    public class SteamAuthenticator : IAuthenticator
    {
        private readonly SteamAccount _account;
        private readonly string _maFileContent;

        public SteamAuthenticator(SteamAccount account)
        {
            _account = account;
            _maFileContent = LoadMaFile();
        }

        private string LoadMaFile()
        {
            if (string.IsNullOrEmpty(_account.MaFilePath) || !File.Exists(_account.MaFilePath))
            {
                return string.Empty;
            }

            try
            {
                return File.ReadAllText(_account.MaFilePath);
            }
            catch (Exception ex)
            {
                _account.ErrorMessage = $"Ошибка чтения .maFile: {ex.Message}";
                return string.Empty;
            }
        }

        public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            if (string.IsNullOrEmpty(_maFileContent))
            {
                throw new InvalidOperationException("Не удалось загрузить данные мобильного аутентификатора");
            }

            try
            {
                var maFile = JsonSerializer.Deserialize<MaFile>(_maFileContent);
                if (maFile == null)
                {
                    throw new InvalidOperationException("Неверный формат .maFile");
                }

                // Генерируем TOTP код
                var totp = new TOTPGenerator(maFile.SharedSecret);
                return totp.GenerateCode();
            }
            catch (Exception ex)
            {
                _account.ErrorMessage = $"Ошибка генерации кода: {ex.Message}";
                throw;
            }
        }

        public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            // Для данного приложения не поддерживаем email аутентификацию
            throw new NotSupportedException("Email аутентификация не поддерживается");
        }

        public async Task<bool> AcceptDeviceConfirmationAsync()
        {
            // Не поддерживаем подтверждение через мобильное приложение
            return false;
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
