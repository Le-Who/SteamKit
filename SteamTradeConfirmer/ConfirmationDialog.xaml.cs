using System;
using System.Windows;

namespace SteamTradeConfirmer
{
    public partial class ConfirmationDialog : Window
    {
        public string ConfirmationCode { get; private set; } = string.Empty;

        public ConfirmationDialog(string accountName)
        {
            InitializeComponent();
            AccountTextBlock.Text = accountName;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmationCode = ConfirmationCodeTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(ConfirmationCode))
            {
                MessageBox.Show("Введите код подтверждения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
