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
        private Client _selectedClient;
        private List<Account> _accounts;

        public TransfersControl()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadClientAccounts();
        }

        private async Task LoadClientAccounts()
        {
            if (_selectedClient == null) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _accounts = await App.Database.GetAllClientAccountsWithBalanceAsync(_selectedClient.Id);

                cmbFromAccount.ItemsSource = _accounts;
                cmbToAccount.ItemsSource = _accounts;

                cmbFromAccount.DisplayMemberPath = "FullDisplayName";
                cmbToAccount.DisplayMemberPath = "FullDisplayName";

                // Обновляем информацию о клиенте
                decimal totalBalance = _accounts.Sum(a => a.Balance);
                txtClientName.Text = _selectedClient.FullName;
                txtClientInfo.Text = $"Клиент с {_accounts.Count} счетами | Общий баланс: {totalBalance:N2} ₽";

                // Загружаем историю
                await LoadTransactionHistory();
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

        private async Task LoadTransactionHistory()
        {
            if (_selectedClient == null) return;

            try
            {
                var allTransactions = new List<Transaction>();

                foreach (var account in _accounts)
                {
                    var transactions = await App.Database.GetAccountTransactionsAsync(account.Id, 30);
                    allTransactions.AddRange(transactions);
                }

                dgHistory.ItemsSource = allTransactions.OrderByDescending(t => t.Date).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTransactionHistory error: {ex.Message}");
            }
        }

        private async void BtnSelectClient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientSelectionDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.SelectedClient != null)
            {
                _selectedClient = dialog.SelectedClient;
                await LoadClientAccounts();
            }
        }

        private void CmbFromAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFromAccount.SelectedItem is Account selectedAccount)
            {
                txtCurrentBalance.Visibility = Visibility.Visible;
                txtCurrentBalance.Text = $"Доступно: {selectedAccount.Balance:N2} {selectedAccount.Currency}";
                txtCurrentBalance.Foreground = selectedAccount.Balance > 0 ?
                    System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            }
            else
            {
                txtCurrentBalance.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем выбранные счета
            if (cmbFromAccount.SelectedItem == null)
            {
                ShowError("Выберите счет списания");
                return;
            }

            if (cmbToAccount.SelectedItem == null)
            {
                ShowError("Выберите счет получения");
                return;
            }

            // Проверяем сумму
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                ShowError("Введите корректную сумму перевода");
                return;
            }

            var fromAccount = cmbFromAccount.SelectedItem as Account;
            var toAccount = cmbToAccount.SelectedItem as Account;

            // Проверяем, что счета разные
            if (fromAccount.Id == toAccount.Id)
            {
                ShowError("Нельзя перевести деньги на тот же счет");
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;
            txtError.Visibility = Visibility.Collapsed;

            try
            {
                var result = await App.Database.TransferMoneyWithDetailsAsync(
                    fromAccount.AccountNumber,
                    toAccount.AccountNumber,
                    amount,
                    txtDescription.Text,
                    App.Session.CurrentUser?.Id ?? 1);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем балансы счетов
                    await LoadClientAccounts();

                    // Очищаем форму
                    txtAmount.Text = "0";
                    txtDescription.Text = "";
                    cmbFromAccount.SelectedItem = null;
                    cmbToAccount.SelectedItem = null;

                    // Обновляем историю
                    await LoadTransactionHistory();
                }
                else
                {
                    ShowError(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка перевода: {ex.Message}");
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        private async void BtnDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
            {
                MessageBox.Show("Сначала выберите клиента", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_accounts == null || _accounts.Count == 0)
            {
                MessageBox.Show("У клиента нет активных счетов", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new DepositDialog(_accounts);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.Success)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    var result = await App.Database.DepositToAccountAsync(
                        dialog.TargetAccountNumber,
                        dialog.Amount,
                        $"Пополнение через оператора",
                        App.Session.CurrentUser?.Id ?? 1);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем данные
                        await LoadClientAccounts();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка пополнения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtAmount.Text = "0";
            txtDescription.Text = "";
            cmbFromAccount.SelectedItem = null;
            cmbToAccount.SelectedItem = null;
            txtError.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void TxtAmount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, точку и запятую
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}