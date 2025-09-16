using System;
using System.IO;
using System.Text.Json;

namespace SteamTradeConfirmer.Services
{
    public class ConfigurationService
    {
        private static ConfigurationService? _instance;
        private static readonly object _lock = new object();
        private readonly AppSettings _settings;

        private ConfigurationService()
        {
            _settings = LoadSettings();
        }

        public static ConfigurationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationService();
                        }
                    }
                }
                return _instance;
            }
        }

        public string SteamApiKey => _settings.SteamApiKey;

        private AppSettings LoadSettings()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                if (!File.Exists(configPath))
                {
                    // Создаем файл с настройками по умолчанию
                    var defaultSettings = new AppSettings
                    {
                        SteamApiKey = "YOUR_STEAM_API_KEY_HERE"
                    };
                    
                    var json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(configPath, json);
                    return defaultSettings;
                }

                var jsonContent = File.ReadAllText(configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonContent);
                
                return settings ?? new AppSettings { SteamApiKey = "YOUR_STEAM_API_KEY_HERE" };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка загрузки конфигурации: {ex.Message}", ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка сохранения конфигурации: {ex.Message}", ex);
            }
        }

        public void UpdateApiKey(string newApiKey)
        {
            _settings.SteamApiKey = newApiKey;
            SaveSettings();
        }

        public bool IsApiKeyConfigured()
        {
            return !string.IsNullOrEmpty(_settings.SteamApiKey) && 
                   _settings.SteamApiKey != "YOUR_STEAM_API_KEY_HERE";
        }
    }

    public class AppSettings
    {
        public string SteamApiKey { get; set; } = "YOUR_STEAM_API_KEY_HERE";
    }
}
