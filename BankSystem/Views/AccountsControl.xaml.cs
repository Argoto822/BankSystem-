using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.Views
{
    public partial class AccountsControl : UserControl
    {
        private DatabaseService _databaseService;
        private List<Client> _clients;
        private List<Account> _accounts;

        public AccountsControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadClients();
        }

        private async void LoadClients()
        {
            try
            {
                _clients = await _databaseService.GetClientsAsync();
                cmbClients.ItemsSource = _clients;

                if (_clients != null && _clients.Any())
                {
                    cmbClients.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CmbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClients.SelectedItem is Client selectedClient)
            {
                await LoadAccounts(selectedClient.Id);
            }
        }

        private async Task LoadAccounts(int clientId)
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                _accounts = await _databaseService.GetClientAccountsAsync(clientId);

                if (_accounts != null && _accounts.Any())
                {
                    dgAccounts.ItemsSource = _accounts;
                    dgAccounts.Visibility = Visibility.Visible;
                    txtEmpty.Visibility = Visibility.Collapsed;

                    UpdateStatistics(_accounts);
                }
                else
                {
                    dgAccounts.ItemsSource = null;
                    dgAccounts.Visibility = Visibility.Collapsed;
                    txtEmpty.Visibility = Visibility.Visible;

                    txtCount.Text = "0";
                    txtTotal.Text = "0 ₽";
                    txtAverage.Text = "0 ₽";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки счетов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateStatistics(List<Account> accounts)
        {
            txtCount.Text = accounts.Count.ToString();

            decimal totalBalance = accounts.Sum(a => Math.Abs(a.Balance));
            txtTotal.Text = $"{totalBalance:N2} ₽";

            decimal averageBalance = accounts.Any() ? totalBalance / accounts.Count : 0;
            txtAverage.Text = $"{averageBalance:N2} ₽";
        }

        private async void BtnOpenAccount_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClients.SelectedItem is Client selectedClient)
            {
                var dialog = new OpenAccountDialog();
                dialog.Owner = Window.GetWindow(this);
                dialog.SetClientId(selectedClient.Id);

                if (dialog.ShowDialog() == true)
                {
                    await LoadAccounts(selectedClient.Id);
                }
            }
        }
    }
}