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

        public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
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
                var code = totp.GenerateCode();
                
                // Обновляем статус аккаунта
                _account.Status = $"Сгенерирован код: {code}";
                
                return code;
            }
            catch (Exception ex)
            {
                _account.ErrorMessage = $"Ошибка генерации кода: {ex.Message}";
                throw;
            }
        }

        public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            // Для данного приложения не поддерживаем email аутентификацию
            throw new NotSupportedException("Email аутентификация не поддерживается");
        }

        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            // Не поддерживаем подтверждение через мобильное приложение
            return Task.FromResult(false);
        }
    }

}
