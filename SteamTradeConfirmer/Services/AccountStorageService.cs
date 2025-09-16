using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SteamTradeConfirmer.Models;

namespace SteamTradeConfirmer.Services
{
    public class AccountStorageService
    {
        private readonly string _accountsFilePath;
        private readonly byte[] _entropy = Encoding.UTF8.GetBytes("SteamTradeConfirmer2025");

        public AccountStorageService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SteamTradeConfirmer");
            Directory.CreateDirectory(appFolder);
            _accountsFilePath = Path.Combine(appFolder, "accounts.dat");
        }

        public async Task<List<SteamAccount>> LoadAccountsAsync()
        {
            try
            {
                if (!File.Exists(_accountsFilePath))
                {
                    return new List<SteamAccount>();
                }

                var encryptedData = await File.ReadAllBytesAsync(_accountsFilePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedData);
                
                var accounts = JsonSerializer.Deserialize<List<SteamAccountData>>(json);
                if (accounts == null)
                {
                    return new List<SteamAccount>();
                }

                var result = new List<SteamAccount>();
                foreach (var accountData in accounts)
                {
                    result.Add(new SteamAccount
                    {
                        Username = accountData.Username,
                        Password = accountData.Password,
                        MaFilePath = accountData.MaFilePath,
                        DisplayName = accountData.DisplayName
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка загрузки аккаунтов: {ex.Message}", ex);
            }
        }

        public async Task SaveAccountsAsync(List<SteamAccount> accounts)
        {
            try
            {
                var accountDataList = new List<SteamAccountData>();
                foreach (var account in accounts)
                {
                    accountDataList.Add(new SteamAccountData
                    {
                        Username = account.Username,
                        Password = account.Password,
                        MaFilePath = account.MaFilePath,
                        DisplayName = account.DisplayName
                    });
                }

                var json = JsonSerializer.Serialize(accountDataList, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var data = Encoding.UTF8.GetBytes(json);
                var encryptedData = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
                
                await File.WriteAllBytesAsync(_accountsFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения аккаунтов: {ex.Message}", ex);
            }
        }

        private class SteamAccountData
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string MaFilePath { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}
