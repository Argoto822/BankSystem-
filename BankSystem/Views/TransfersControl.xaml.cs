using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class TransfersControl : UserControl
    {
        private List<Account> _accounts;
        private List<Transaction> _history;
        private Client _selectedClient;
        private List<Client> _clients;

        public TransfersControl()
        {
            InitializeComponent();
            LoadClients();
        }

        private async void LoadClients()
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _clients = await App.Database.GetClientsAsync();

                if (_clients != null && _clients.Count > 0)
                {
                    _selectedClient = _clients.First();
                    UpdateClientInfo();
                    await LoadAccountsAsync(_selectedClient.Id);
                }
                else
                {
                    txtClientName.Text = "Клиенты не найдены";
                    txtClientInfo.Text = "Сначала добавьте клиентов";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateClientInfo()
        {
            if (_selectedClient != null)
            {
                txtClientName.Text = _selectedClient.FullName;
                int accountCount = _accounts?.Count ?? 0;
                decimal totalBalance = _accounts?.Sum(a => a.Balance) ?? 0;
                txtClientInfo.Text = $"Клиент с {accountCount} счетами | Общий баланс: {totalBalance:N2} ₽";
            }
        }

        private async Task LoadAccountsAsync(int clientId)
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _accounts = await App.Database.GetClientAccountsAsync(clientId);
                _accounts = _accounts.Where(a => a.StatusCode == "active").ToList();

                cmbFromAccount.ItemsSource = _accounts;
                cmbToAccount.ItemsSource = _accounts;

                UpdateAccountDisplay();
                UpdateClientInfo();

                if (_accounts.Count > 0)
                {
                    await LoadHistoryAsync(_accounts.First().Id);
                }
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

        private void UpdateAccountDisplay()
        {
            if (_accounts == null) return;

            foreach (var account in _accounts)
            {
                account.DisplayName = $"{account.AccountName} - {account.AccountNumber} - {account.Balance:N2} ₽";
            }

            var tempFrom = cmbFromAccount.SelectedItem;
            var tempTo = cmbToAccount.SelectedItem;

            cmbFromAccount.ItemsSource = null;
            cmbToAccount.ItemsSource = null;

            cmbFromAccount.ItemsSource = _accounts;
            cmbToAccount.ItemsSource = _accounts;

            if (tempFrom != null) cmbFromAccount.SelectedItem = tempFrom;
            if (tempTo != null) cmbToAccount.SelectedItem = tempTo;
        }

        private async Task LoadHistoryAsync(int accountId)
        {
            try
            {
                _history = await App.Database.GetAccountTransactionsAsync(accountId);
                dgHistory.ItemsSource = _history;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
            }
        }

        private void CmbFromAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFromAccount.SelectedItem is Account selected)
            {
                cmbToAccount.ItemsSource = _accounts.Where(a => a.AccountNumber != selected.AccountNumber).ToList();

                if (selected != null)
                {
                    txtCurrentBalance.Text = $"Доступно: {selected.Balance:N2} ₽";
                    txtCurrentBalance.Visibility = Visibility.Visible;
                }
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД BtnTransfer_Click
        private async void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            txtError.Visibility = Visibility.Collapsed;

            if (cmbFromAccount.SelectedItem == null || cmbToAccount.SelectedItem == null)
            {
                ShowError("Выберите счета для перевода");
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                ShowError("Введите корректную сумму");
                return;
            }

            var fromAccount = (Account)cmbFromAccount.SelectedItem;
            var toAccount = (Account)cmbToAccount.SelectedItem;

            if (fromAccount.Id == toAccount.Id)
            {
                ShowError("Нельзя перевести на тот же счет");
                return;
            }

            if (amount > fromAccount.Balance)
            {
                ShowError($"Недостаточно средств на счете \"{fromAccount.AccountName}\". Доступно: {fromAccount.Balance:N2} ₽");
                return;
            }

            // Подтверждение перевода
            var confirmResult = MessageBox.Show(
                $"Подтвердите перевод:\n\n" +
                $"Со счета: {fromAccount.AccountName} ({fromAccount.AccountNumber})\n" +
                $"На счет: {toAccount.AccountName} ({toAccount.AccountNumber})\n" +
                $"Сумма: {amount:N2} ₽\n\n" +
                $"Баланс после списания: {(fromAccount.Balance - amount):N2} ₽",
                "Подтверждение перевода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;
            btnTransfer.IsEnabled = false;

            try
            {
                var success = await App.Database.TransferMoneyAsync(
                    fromAccount.AccountNumber,
                    toAccount.AccountNumber,
                    amount,
                    txtDescription.Text,
                    App.Session.CurrentUser?.Id ?? 1);

                if (success)
                {
                    // Обновляем балансы
                    decimal oldFromBalance = fromAccount.Balance;
                    decimal oldToBalance = toAccount.Balance;

                    fromAccount.Balance -= amount;
                    toAccount.Balance += amount;

                    MessageBox.Show(
                        $"Перевод {amount:N2} ₽ выполнен успешно!\n\n" +
                        $"Счет списания: {fromAccount.AccountName}\n" +
                        $"  Было: {oldFromBalance:N2} ₽\n" +
                        $"  Стало: {fromAccount.Balance:N2} ₽\n\n" +
                        $"Счет зачисления: {toAccount.AccountName}\n" +
                        $"  Было: {oldToBalance:N2} ₽\n" +
                        $"  Стало: {toAccount.Balance:N2} ₽",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Обновляем отображение
                    UpdateAccountDisplay();
                    UpdateClientInfo();
                    await LoadHistoryAsync(fromAccount.Id);

                    // Очищаем форму
                    txtAmount.Text = "0";
                    txtDescription.Text = "";
                    txtCurrentBalance.Text = "";
                    txtCurrentBalance.Visibility = Visibility.Collapsed;
                    cmbFromAccount.SelectedItem = null;
                    cmbToAccount.SelectedItem = null;
                }
                else
                {
                    ShowError("Ошибка при выполнении перевода");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
                btnTransfer.IsEnabled = true;
            }
        }

        private void BtnSelectClient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientSelectionDialog(_clients);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.SelectedClient != null)
            {
                _selectedClient = dialog.SelectedClient;
                UpdateClientInfo();
                LoadAccountsAsync(_selectedClient.Id);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            cmbFromAccount.SelectedItem = null;
            cmbToAccount.SelectedItem = null;
            txtAmount.Text = "0";
            txtDescription.Text = "";
            txtCurrentBalance.Text = "";
            txtCurrentBalance.Visibility = Visibility.Collapsed;
            txtError.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}