using System;
using System.Diagnostics;
using System.Windows;
using SteamTradeConfirmer.Services;

namespace SteamTradeConfirmer
{
    public partial class SettingsDialog : Window
    {
        private readonly ConfigurationService _configService;

        public SettingsDialog()
        {
            InitializeComponent();
            _configService = ConfigurationService.Instance;
            
            // Загружаем текущий API ключ
            ApiKeyTextBox.Text = _configService.SteamApiKey;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var apiKey = ApiKeyTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    MessageBox.Show("Введите API ключ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _configService.UpdateApiKey(apiKey);
                
                MessageBox.Show("Настройки сохранены успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ApiKeyLink_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://steamcommunity.com/dev/apikey",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
