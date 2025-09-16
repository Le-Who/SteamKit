using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SteamTradeConfirmer.Models;

namespace SteamTradeConfirmer
{
    public partial class AddAccountDialog : Window
    {
        public SteamAccount Account { get; private set; } = new();

        public AddAccountDialog()
        {
            InitializeComponent();
        }

        private void BrowseMaFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл .maFile",
                Filter = "Mobile Authenticator Files (*.maFile)|*.maFile|All Files (*.*)|*.*",
                DefaultExt = "maFile"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                MaFilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Введите имя пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(MaFilePathTextBox.Text))
            {
                MessageBox.Show("Выберите файл .maFile", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(MaFilePathTextBox.Text))
            {
                MessageBox.Show("Файл .maFile не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Account.Username = UsernameTextBox.Text.Trim();
            Account.Password = PasswordBox.Password;
            Account.MaFilePath = MaFilePathTextBox.Text;
            Account.DisplayName = Account.Username;

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
