using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class AccountsControl : UserControl
    {
        private List<Client> _clients;
        private List<Account> _accounts;
        private Client _selectedClient;

        public AccountsControl()
        {
            InitializeComponent();
            LoadClientsAsync();
        }

        private async void LoadClientsAsync()
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _clients = await App.Database.GetClientsAsync(null, true);
                cmbClients.ItemsSource = _clients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void CmbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClients.SelectedItem is Client client)
            {
                _selectedClient = client;
                await LoadAccountsAsync(client.Id);
            }
        }

        private async Task LoadAccountsAsync(int clientId)
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _accounts = await App.Database.GetClientAccountsAsync(clientId);
                dgAccounts.ItemsSource = _accounts;

                txtCount.Text = _accounts.Count.ToString();
                txtTotal.Text = $"{_accounts.Sum(a => a.Balance):N2} ₽";
                txtAverage.Text = _accounts.Count > 0 ? $"{_accounts.Average(a => a.Balance):N2} ₽" : "0 ₽";

                txtEmpty.Visibility = _accounts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                dgAccounts.Visibility = _accounts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                btnOpenAccount.Visibility = App.Session.IsOperator ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки счетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnOpenAccount_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;

            var dialog = new OpenAccountDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                var success = await App.Database.OpenAccountAsync(
                    _selectedClient.Id,
                    dialog.SelectedAccountType,
                    App.Session.CurrentUser?.Id ?? 1);

                if (success)
                {
                    await LoadAccountsAsync(_selectedClient.Id);
                    MessageBox.Show("Счет успешно открыт!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при открытии счета", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}