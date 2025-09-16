using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using SteamTradeConfirmer.Services;

namespace SteamTradeConfirmer
{
    public partial class LogViewerDialog : Window
    {
        private readonly LoggingService _loggingService;

        public LogViewerDialog()
        {
            InitializeComponent();
            _loggingService = LoggingService.Instance;
            
            // Подписываемся на новые логи
            _loggingService.LogAdded += OnLogAdded;
            
            // Загружаем существующие логи
            RefreshLogs();
        }

        private void OnLogAdded(object? sender, LogEntry logEntry)
        {
            Dispatcher.Invoke(() =>
            {
                LogsListBox.Items.Add(logEntry);
                
                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogsListBox.ScrollIntoView(logEntry);
                }
            });
        }

        private void RefreshLogs()
        {
            var logs = _loggingService.GetLogs();
            LogsListBox.Items.Clear();
            
            foreach (var log in logs)
            {
                LogsListBox.Items.Add(log);
            }
            
            if (AutoScrollCheckBox.IsChecked == true && logs.Any())
            {
                LogsListBox.ScrollIntoView(logs.Last());
            }
            
            StatusTextBlock.Text = $"Загружено {logs.Count} записей";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogs();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить все логи?", "Подтверждение", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _loggingService.ClearLogs();
                LogsListBox.Items.Clear();
                StatusTextBlock.Text = "Логи очищены";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"steam_trade_confirmer_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _loggingService.SaveLogsToFile(dialog.FileName);
                    MessageBox.Show($"Логи сохранены в файл:\n{dialog.FileName}", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения логов:\n{ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _loggingService.LogAdded -= OnLogAdded;
            base.OnClosed(e);
        }
    }

    public class LogLevelToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Debug => Brushes.Gray,
                    LogLevel.Info => Brushes.Black,
                    LogLevel.Warning => Brushes.Orange,
                    LogLevel.Error => Brushes.Red,
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
