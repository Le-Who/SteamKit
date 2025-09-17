using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SteamTradeConfirmer.Models;
using SteamTradeConfirmer.Services;

namespace SteamTradeConfirmer
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<SteamAccount> _accounts = new();
        private readonly ObservableCollection<TradeOffer> _tradeOffers = new();
        private readonly SteamClientService _steamClientService;
        private readonly TradeOfferService _tradeOfferService;
        private readonly AccountStorageService _accountStorageService;

        public MainWindow()
        {
            InitializeComponent();
            
            _steamClientService = new SteamClientService();
            _tradeOfferService = new TradeOfferService(_steamClientService);
            _accountStorageService = new AccountStorageService();

            // Подписываемся на события
            _steamClientService.AccountAuthenticated += OnAccountAuthenticated;
            _steamClientService.AccountDisconnected += OnAccountDisconnected;
            _steamClientService.AuthenticationFailed += OnAuthenticationFailed;

            // Настраиваем привязки данных
            AccountsListBox.ItemsSource = _accounts;
            TradeOffersDataGrid.ItemsSource = _tradeOffers;

            // Загружаем сохраненные аккаунты
            LoadAccounts();

            // Показываем информацию о логах при запуске
            var logPath = LoggingService.Instance.GetLogFilePath();
            UpdateStatus($"Готов к работе. Логи: {Path.GetFileName(logPath)}");
        }

        private async void LoadAccounts()
        {
            try
            {
                var accounts = await _accountStorageService.LoadAccountsAsync();
                foreach (var account in accounts)
                {
                    _accounts.Add(account);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка загрузки аккаунтов: {ex.Message}");
            }
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddAccountDialog();
            if (dialog.ShowDialog() == true)
            {
                var account = dialog.Account;
                _accounts.Add(account);
                await _accountStorageService.SaveAccountsAsync(_accounts.ToList());
                
                // Автоматически подключаемся к аккаунту
                await _steamClientService.AuthenticateAccountAsync(account);
            }
        }

        private async void RemoveAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListBox.SelectedItem is SteamAccount selectedAccount)
            {
                _steamClientService.DisconnectAccount(selectedAccount);
                _accounts.Remove(selectedAccount);
                await _accountStorageService.SaveAccountsAsync(_accounts.ToList());
                UpdateStatus($"Аккаунт {selectedAccount.Username} удален");
            }
        }

        private void AccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveAccountButton.IsEnabled = AccountsListBox.SelectedItem != null;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshTradeOffers();
        }

        private Task RefreshTradeOffers()
        {
            UpdateStatus("Обновление списка обменов...");
            _tradeOffers.Clear();

            var authenticatedAccounts = _accounts.Where(a => a.IsAuthenticated).ToList();
            
            foreach (var account in authenticatedAccounts)
            {
                try
                {
                    var offers = _tradeOfferService.GetTradeOffers(account);
                    foreach (var offer in offers)
                    {
                        _tradeOffers.Add(offer);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Ошибка получения обменов для {account.Username}: {ex.Message}");
                }
            }

            UpdateStatus($"Загружено {_tradeOffers.Count} обменов");
            return Task.CompletedTask;
        }

        private async void ConfirmTradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tradeOfferId)
            {
                var selectedAccount = AccountsListBox.SelectedItem as SteamAccount;
                if (selectedAccount == null)
                {
                    MessageBox.Show("Выберите аккаунт для подтверждения обмена", "Ошибка", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateStatus($"Подтверждение обмена {tradeOfferId}...");
                
                var success = _tradeOfferService.ConfirmTradeOffer(selectedAccount, tradeOfferId);
                if (success)
                {
                    UpdateStatus("Обмен успешно подтвержден");
                    await RefreshTradeOffers();
                }
                else
                {
                    UpdateStatus($"Ошибка подтверждения обмена: {selectedAccount.ErrorMessage}");
                }
            }
        }

        private async void CancelTradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tradeOfferId)
            {
                var selectedAccount = AccountsListBox.SelectedItem as SteamAccount;
                if (selectedAccount == null)
                {
                    MessageBox.Show("Выберите аккаунт для отмены обмена", "Ошибка", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("Вы уверены, что хотите отменить этот обмен?", 
                                           "Подтверждение отмены", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    UpdateStatus($"Отмена обмена {tradeOfferId}...");
                    
                    var success = _tradeOfferService.CancelTradeOffer(selectedAccount, tradeOfferId);
                    if (success)
                    {
                        UpdateStatus("Обмен успешно отменен");
                        await RefreshTradeOffers();
                    }
                    else
                    {
                        UpdateStatus($"Ошибка отмены обмена: {selectedAccount.ErrorMessage}");
                    }
                }
            }
        }

        private void OnAccountAuthenticated(object? sender, SteamAccount account)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Аккаунт {account.Username} успешно подключен");
                // Автоматически обновляем список обменов при подключении нового аккаунта
                _ = RefreshTradeOffers();
            });
        }

        private void OnAccountDisconnected(object? sender, SteamAccount account)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Аккаунт {account.Username} отключен");
            });
        }

        private void OnAuthenticationFailed(object? sender, (SteamAccount account, string error) e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Ошибка аутентификации {e.account.Username}: {e.error}");
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog();
            if (dialog.ShowDialog() == true)
            {
                UpdateStatus("Настройки обновлены");
            }
        }

        private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = LoggingService.Instance.GetLogFilePath();
                var logDir = Path.GetDirectoryName(logPath);
                
                if (Directory.Exists(logDir))
                {
                    // Открываем папку с логами в проводнике Windows
                    System.Diagnostics.Process.Start("explorer.exe", logDir);
                    UpdateStatus($"Открыта папка с логами: {logDir}");
                }
                else
                {
                    UpdateStatus("Папка с логами не найдена");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка открытия папки логов: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _steamClientService.DisconnectAll();
            base.OnClosed(e);
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
