using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.Views
{
    public partial class TransfersControl : UserControl
    {
        private DatabaseService _databaseService;
        private Client _selectedClient;
        private Account _selectedFromAccount;
        private Account _selectedToAccount;
        private Account _selectedCreditAccount;

        public TransfersControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                if (App.Session.CurrentUser != null)
                {
                    var clients = await _databaseService.GetClientsAsync();
                    if (clients != null && clients.Any())
                    {
                        _selectedClient = clients.First();
                        await LoadClientInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadClientInfo()
        {
            if (_selectedClient != null)
            {
                txtClientName.Text = _selectedClient.FullName;

                var accounts = await _databaseService.GetAllClientAccountsWithBalanceAsync(_selectedClient.Id);
                decimal totalBalance = 0;
                if (accounts != null && accounts.Any())
                {
                    totalBalance = accounts.Where(a => a.AccountType != "credit").Sum(a => Math.Abs(a.Balance));
                }
                txtClientInfo.Text = $"Клиент с {accounts?.Count ?? 0} счетами | Общий баланс: {totalBalance:N2} ₽";

                await LoadAccounts();
            }
        }

        private async Task LoadAccounts()
        {
            var accounts = await _databaseService.GetAllClientAccountsWithBalanceAsync(_selectedClient.Id);

            var activeAccounts = accounts?.Where(a => a.StatusCode == "active").ToList() ?? new System.Collections.Generic.List<Account>();

            cmbFromAccount.ItemsSource = activeAccounts;
            cmbToAccount.ItemsSource = activeAccounts;

            // Находим кредитный счет
            _selectedCreditAccount = activeAccounts.FirstOrDefault(a => a.AccountType == "credit");

            if (_selectedCreditAccount != null)
            {
                creditPaymentPanel.Visibility = Visibility.Visible;
                txtCreditAccountNumber.Text = _selectedCreditAccount.AccountNumber;
                decimal debt = Math.Abs(_selectedCreditAccount.Balance);
                txtCreditDebt.Text = debt.ToString("N2") + " ₽";

                // Рассчитываем минимальный платеж (5% от суммы долга, но не менее 1000 руб)
                decimal minPayment = Math.Max(debt * 0.05m, 1000);
                txtPaymentInfo.Text = $"Минимальный платеж: {minPayment:N2} ₽. Полное погашение: {debt:N2} ₽.";
            }
            else
            {
                creditPaymentPanel.Visibility = Visibility.Collapsed;
            }

            if (activeAccounts != null && activeAccounts.Any())
            {
                cmbFromAccount.SelectedIndex = 0;
                cmbToAccount.IsEnabled = true;
            }
            else
            {
                cmbFromAccount.IsEnabled = false;
                cmbToAccount.IsEnabled = false;
                txtStatus.Text = "У клиента нет активных счетов";
            }
        }

        private void CmbFromAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedFromAccount = cmbFromAccount.SelectedItem as Account;

            if (cmbToAccount.ItemsSource != null)
            {
                var accounts = cmbToAccount.ItemsSource.Cast<Account>().ToList();
                var filteredAccounts = accounts.Where(a => a.Id != (_selectedFromAccount?.Id ?? 0)).ToList();
                cmbToAccount.ItemsSource = filteredAccounts;

                if (filteredAccounts.Any())
                {
                    cmbToAccount.SelectedIndex = 0;
                    _selectedToAccount = filteredAccounts.First();
                }
                else
                {
                    _selectedToAccount = null;
                }
            }
        }

        private async void BtnChangeClient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientSelectionDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.SelectedClient != null)
            {
                _selectedClient = dialog.SelectedClient;
                await LoadClientInfo();
                ClearForm();
                txtStatus.Text = "Клиент успешно изменен";
            }
        }

        private async void BtnDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFromAccount == null)
            {
                MessageBox.Show("Выберите счет для пополнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                var result = await _databaseService.DepositToAccountAsync(
                    _selectedFromAccount.AccountNumber,
                    amount,
                    string.IsNullOrEmpty(txtDescription.Text) ? "Пополнение счета" : txtDescription.Text,
                    App.Session.CurrentUser?.Id ?? 1
                );

                if (result.Success)
                {
                    txtStatus.Text = result.Message;
                    await LoadAccounts();
                    ClearForm();
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtStatus.Text = result.Message;
                    MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Ошибка: " + ex.Message;
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFromAccount == null)
            {
                MessageBox.Show("Выберите счет списания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedToAccount == null)
            {
                MessageBox.Show("Выберите счет получателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedFromAccount.Id == _selectedToAccount.Id)
            {
                MessageBox.Show("Нельзя перевести деньги на тот же счет!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка для перевода на кредитный счет
            if (_selectedToAccount.AccountType == "credit")
            {
                decimal debt = Math.Abs(_selectedToAccount.Balance);
                if (amount > debt)
                {
                    var result = MessageBox.Show($"Сумма платежа ({amount:N2} ₽) превышает задолженность ({debt:N2} ₽).\n" +
                        $"Будет погашена только задолженность. Продолжить?", "Предупреждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                    amount = debt;
                }
            }

            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                var result = await _databaseService.TransferMoneyWithDetailsAsync(
                    _selectedFromAccount.AccountNumber,
                    _selectedToAccount.AccountNumber,
                    amount,
                    txtDescription.Text,
                    App.Session.CurrentUser?.Id ?? 1
                );

                if (result.Success)
                {
                    txtStatus.Text = result.Message;
                    await LoadAccounts();
                    ClearForm();
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtStatus.Text = result.Message;
                    MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Ошибка: " + ex.Message;
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnMinPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreditAccount == null)
            {
                MessageBox.Show("Кредитный счет не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal debt = Math.Abs(_selectedCreditAccount.Balance);
            decimal minPayment = Math.Max(debt * 0.05m, 1000);
            minPayment = Math.Min(minPayment, debt); // Не больше суммы долга

            if (debt <= 0)
            {
                MessageBox.Show("У вас нет задолженности по кредиту!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Ищем счет для списания (текущий или сберегательный)
            var accounts = await _databaseService.GetAllClientAccountsWithBalanceAsync(_selectedClient.Id);
            var sourceAccount = accounts.FirstOrDefault(a => a.AccountType != "credit" && a.Balance >= minPayment && a.StatusCode == "active");

            if (sourceAccount == null)
            {
                MessageBox.Show($"Недостаточно средств для минимального платежа ({minPayment:N2} ₽)!\n" +
                    "Пополните счет или выберите другой способ оплаты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                var result = await _databaseService.TransferMoneyWithDetailsAsync(
                    sourceAccount.AccountNumber,
                    _selectedCreditAccount.AccountNumber,
                    minPayment,
                    "Минимальный платеж по кредиту",
                    App.Session.CurrentUser?.Id ?? 1
                );

                if (result.Success)
                {
                    txtStatus.Text = $"Минимальный платеж {minPayment:N2} ₽ выполнен!";
                    await LoadAccounts();
                    MessageBox.Show($"Минимальный платеж {minPayment:N2} ₽ успешно списан со счета {sourceAccount.AccountNumber}\n" +
                        $"Остаток задолженности: {Math.Abs(_selectedCreditAccount.Balance - minPayment):N2} ₽",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtStatus.Text = result.Message;
                    MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnFullPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreditAccount == null)
            {
                MessageBox.Show("Кредитный счет не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal debt = Math.Abs(_selectedCreditAccount.Balance);

            if (debt <= 0)
            {
                MessageBox.Show("У вас нет задолженности по кредиту!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Ищем счета для списания
            var accounts = await _databaseService.GetAllClientAccountsWithBalanceAsync(_selectedClient.Id);
            var sourceAccounts = accounts.Where(a => a.AccountType != "credit" && a.Balance > 0 && a.StatusCode == "active").ToList();

            decimal totalBalance = sourceAccounts.Sum(a => a.Balance);

            if (totalBalance < debt)
            {
                MessageBox.Show($"Недостаточно средств для полного погашения кредита!\n" +
                    $"Необходимо: {debt:N2} ₽\n" +
                    $"Доступно: {totalBalance:N2} ₽\n\n" +
                    $"Внесите дополнительные средства на счета.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                decimal remainingDebt = debt;
                bool success = true;

                foreach (var sourceAccount in sourceAccounts.OrderByDescending(a => a.Balance))
                {
                    if (remainingDebt <= 0) break;

                    decimal paymentAmount = Math.Min(sourceAccount.Balance, remainingDebt);

                    var result = await _databaseService.TransferMoneyWithDetailsAsync(
                        sourceAccount.AccountNumber,
                        _selectedCreditAccount.AccountNumber,
                        paymentAmount,
                        "Погашение кредита",
                        App.Session.CurrentUser?.Id ?? 1
                    );

                    if (result.Success)
                    {
                        remainingDebt -= paymentAmount;
                        txtStatus.Text = $"Погашение кредита: списано {paymentAmount:N2} ₽ со счета {sourceAccount.AccountNumber}";
                    }
                    else
                    {
                        success = false;
                        MessageBox.Show($"Ошибка при списании со счета {sourceAccount.AccountNumber}: {result.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
                }

                if (success && remainingDebt <= 0)
                {
                    await LoadAccounts();
                    MessageBox.Show($"Кредит полностью погашен!\n\nОбщая сумма погашения: {debt:N2} ₽",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (success && remainingDebt > 0)
                {
                    MessageBox.Show($"Частично погашено: {debt - remainingDebt:N2} ₽\n" +
                        $"Остаток задолженности: {remainingDebt:N2} ₽",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtStatus.Text = "Форма очищена";
        }

        private void ClearForm()
        {
            txtAmount.Text = "";
            txtDescription.Text = "";

            if (cmbFromAccount.ItemsSource != null && cmbFromAccount.Items.Cast<object>().Any())
            {
                cmbFromAccount.SelectedIndex = 0;
            }
        }

        private void TxtAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Проверяем, что вводится число
            if (decimal.TryParse(txtAmount.Text, out decimal amount) && amount > 0 && _selectedCreditAccount != null)
            {
                decimal debt = Math.Abs(_selectedCreditAccount.Balance);
                if (amount > debt)
                {
                    txtAmount.Text = debt.ToString();
                    txtAmount.Select(txtAmount.Text.Length, 0);
                }
            }
        }
    }
}