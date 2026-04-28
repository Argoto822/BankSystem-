using System;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Views;

namespace BankSystem.Views
{
    public partial class MainWindow : Window
    {
        private UserControl _currentControl;

        public MainWindow()
        {
            InitializeComponent();

            // Устанавливаем информацию о пользователе
            if (App.Session.CurrentUser != null)
            {
                txtUserInfo.Text = $"{App.Session.CurrentUser.Login} ({App.Session.CurrentUser.RoleName})";
            }

            // Загружаем клиентов по умолчанию
            ShowClients();
        }

        private void ShowClients()
        {
            _currentControl = new ClientsControl();
            MainContent.Content = _currentControl;
        }

        private void ShowAccounts()
        {
            _currentControl = new AccountsControl();
            MainContent.Content = _currentControl;
        }

        private void ShowTransfers()
        {
            _currentControl = new TransfersControl();
            MainContent.Content = _currentControl;
        }

        private void ShowReports()
        {
            _currentControl = new ReportsControl();
            MainContent.Content = _currentControl;
        }

        private void ShowAdmin()
        {
            // Проверяем права доступа
            if (App.Session.CurrentUser?.RoleName == "Администратор")
            {
                _currentControl = new AdminControl();
                MainContent.Content = _currentControl;
            }
            else
            {
                MessageBox.Show("У вас нет прав доступа к разделу администрирования!",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            ShowClients();
        }

        private void BtnAccounts_Click(object sender, RoutedEventArgs e)
        {
            ShowAccounts();
        }

        private void BtnTransfers_Click(object sender, RoutedEventArgs e)
        {
            ShowTransfers();
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            ShowReports();
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            ShowAdmin();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Очищаем сессию
                App.Session.CurrentUser = null;

                // Открываем окно входа
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                // Закрываем главное окно
                this.Close();
            }
        }
    }
}