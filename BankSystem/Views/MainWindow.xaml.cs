using System.Windows;

namespace BankSystem.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (App.Session.CurrentUser != null)
            {
                txtUser.Text = $"Пользователь: {App.Session.CurrentUser.Login} ({App.Session.CurrentUser.RoleName})";

                if (App.Session.IsAdmin)
                {
                    btnAdmin.Visibility = Visibility.Visible;
                    btnReports.Visibility = Visibility.Visible;
                }
                else if (App.Session.IsAnalyst)
                {
                    btnReports.Visibility = Visibility.Visible;
                    btnClients.Visibility = Visibility.Collapsed;
                    btnAccounts.Visibility = Visibility.Collapsed;
                    btnTransfers.Visibility = Visibility.Collapsed;
                }
                else if (App.Session.IsCashier)
                {
                    btnTransfers.Visibility = Visibility.Visible;
                    btnClients.Visibility = Visibility.Collapsed;
                    btnAccounts.Visibility = Visibility.Collapsed;
                }
                else if (App.Session.IsOperator)
                {
                    btnClients.Visibility = Visibility.Visible;
                    btnAccounts.Visibility = Visibility.Visible;
                    btnTransfers.Visibility = Visibility.Visible;
                }
            }

            LoadClientsPage();
        }

        private void LoadClientsPage()
        {
            ContentArea.Content = new ClientsControl();
            txtStatus.Text = "Управление клиентами";
        }

        private void LoadAccountsPage()
        {
            ContentArea.Content = new AccountsControl();
            txtStatus.Text = "Управление счетами";
        }

        private void LoadTransfersPage()
        {
            ContentArea.Content = new TransfersControl();
            txtStatus.Text = "Платежи и переводы";
        }

        private void LoadReportsPage()
        {
            ContentArea.Content = new ReportsControl();
            txtStatus.Text = "Формирование отчетов";
        }

        private void LoadAdminPage()
        {
            ContentArea.Content = new AdminControl();
            txtStatus.Text = "Администрирование";
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e) => LoadClientsPage();
        private void BtnAccounts_Click(object sender, RoutedEventArgs e) => LoadAccountsPage();
        private void BtnTransfers_Click(object sender, RoutedEventArgs e) => LoadTransfersPage();
        private void BtnReports_Click(object sender, RoutedEventArgs e) => LoadReportsPage();
        private void BtnAdmin_Click(object sender, RoutedEventArgs e) => LoadAdminPage();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            App.Session.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}