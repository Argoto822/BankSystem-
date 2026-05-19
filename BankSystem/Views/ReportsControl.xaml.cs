using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Microsoft.Win32;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.Views
{
    public partial class ReportsControl : UserControl
    {
        private DatabaseService _databaseService;
        private List<Client> _clients;
        private List<Account> _accounts;
        private List<Transaction> _transactions;

        public ReportsControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadClients();

            // Устанавливаем начальные даты
            dpStartDate.SelectedDate = DateTime.Now.AddMonths(-1);
            dpEndDate.SelectedDate = DateTime.Now;

            // Отключаем выбор счета до выбора клиента
            cmbAccounts.IsEnabled = false;
            accountInfoPanel.Visibility = Visibility.Collapsed;
        }

        private async void LoadClients()
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                _clients = await _databaseService.GetClientsAsync();
                cmbClients.ItemsSource = _clients;
                cmbClients.DisplayMemberPath = "FullName";
                cmbClients.SelectedValuePath = "Id";

                // Не выбираем клиента по умолчанию
                cmbClients.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки клиентов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void CmbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Client selectedClient = cmbClients.SelectedItem as Client;
            if (selectedClient != null)
            {
                await LoadAccounts(selectedClient.Id);
            }
            else
            {
                // Если клиент не выбран, очищаем счета
                cmbAccounts.ItemsSource = null;
                cmbAccounts.IsEnabled = false;
                accountInfoPanel.Visibility = Visibility.Collapsed;
                dgTransactions.ItemsSource = null;
                txtEmptyTransactions.Visibility = Visibility.Visible;

                // Сбрасываем статистику
                txtIncomes.Text = "0 ₽";
                txtExpenses.Text = "0 ₽";
                txtBalance.Text = "0 ₽";
                txtTransactionsCount.Text = "0";
            }
        }

        private async Task LoadAccounts(int clientId)
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                _accounts = await _databaseService.GetClientAccountsAsync(clientId);

                cmbAccounts.ItemsSource = _accounts;
                // Не используем DisplayMemberPath, так как у нас есть ItemTemplate
                cmbAccounts.SelectedValuePath = "Id";

                if (_accounts != null && _accounts.Any())
                {
                    cmbAccounts.SelectedIndex = 0;
                    cmbAccounts.IsEnabled = true;
                }
                else
                {
                    cmbAccounts.IsEnabled = false;
                    cmbAccounts.ItemsSource = null;
                    accountInfoPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show("У выбранного клиента нет активных счетов!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки счетов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void CmbAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Account selectedAccount = cmbAccounts.SelectedItem as Account;
            if (selectedAccount != null)
            {
                accountInfoPanel.Visibility = Visibility.Visible;
                txtAccountNumber.Text = selectedAccount.AccountNumber;
                txtAccountType.Text = selectedAccount.AccountTypeName;

                if (selectedAccount.AccountType == "credit")
                {
                    txtAccountBalance.Text = "- " + Math.Abs(selectedAccount.Balance).ToString("N2") + " ₽";
                    txtAccountBalance.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    txtAccountBalance.Text = selectedAccount.Balance.ToString("N2") + " ₽";
                    if (selectedAccount.Balance >= 0)
                    {
                        txtAccountBalance.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        txtAccountBalance.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }

                await LoadTransactions(selectedAccount.Id);
            }
        }

        private async Task LoadTransactions(int accountId)
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                DateTime startDate = dpStartDate.SelectedDate ?? DateTime.Now.AddMonths(-1);
                DateTime endDate = dpEndDate.SelectedDate ?? DateTime.Now;
                endDate = endDate.AddDays(1).AddSeconds(-1);

                _transactions = await _databaseService.GetAccountTransactionsAsync(accountId, 365);

                List<Transaction> filteredTransactions = _transactions
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .OrderByDescending(t => t.Date)
                    .ToList();

                dgTransactions.ItemsSource = filteredTransactions;

                UpdateStatistics(filteredTransactions);

                if (filteredTransactions.Any())
                {
                    txtEmptyTransactions.Visibility = Visibility.Collapsed;
                    dgTransactions.Visibility = Visibility.Visible;
                }
                else
                {
                    txtEmptyTransactions.Visibility = Visibility.Visible;
                    dgTransactions.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки транзакций: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateStatistics(List<Transaction> transactions)
        {
            decimal incomes = transactions.Where(t => t.Type == "Пополнение" || t.Type == "deposit").Sum(t => t.Amount);
            txtIncomes.Text = incomes.ToString("N2") + " ₽";

            decimal expenses = transactions.Where(t => t.Type == "Перевод" || t.Type == "transfer").Sum(t => t.Amount);
            txtExpenses.Text = expenses.ToString("N2") + " ₽";

            decimal balance = incomes - expenses;
            txtBalance.Text = balance.ToString("N2") + " ₽";

            if (balance >= 0)
            {
                txtBalance.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                txtBalance.Foreground = System.Windows.Media.Brushes.Red;
            }

            txtTransactionsCount.Text = transactions.Count.ToString();
        }

        private async void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            Account selectedAccount = cmbAccounts.SelectedItem as Account;
            if (selectedAccount != null)
            {
                await LoadTransactions(selectedAccount.Id);
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Account selectedAccount = cmbAccounts.SelectedItem as Account;
            if (selectedAccount != null)
            {
                await LoadTransactions(selectedAccount.Id);
            }
            else
            {
                MessageBox.Show("Выберите счет для обновления данных!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            Account selectedAccount = cmbAccounts.SelectedItem as Account;
            Client selectedClient = cmbClients.SelectedItem as Client;

            if (selectedAccount != null && _transactions != null && _transactions.Any())
            {
                ExportToCsv(selectedAccount, selectedClient);
            }
            else
            {
                MessageBox.Show("Нет данных для экспорта!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExportToCsv(Account account, Client client)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                saveFileDialog.DefaultExt = ".csv";
                saveFileDialog.FileName = $"Отчет_по_счету_{account.AccountNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                saveFileDialog.Title = "Сохранить отчет в CSV";

                if (saveFileDialog.ShowDialog() == true)
                {
                    StringBuilder csv = new StringBuilder();

                    csv.AppendLine($"\"Отчет по банковскому счету\"");
                    csv.AppendLine($"\"Дата формирования\",\"{DateTime.Now:dd.MM.yyyy HH:mm:ss}\"");
                    csv.AppendLine($"\"Клиент\",\"{client?.FullName ?? "Не указан"}\"");
                    csv.AppendLine($"\"Номер счета\",\"{account.AccountNumber}\"");
                    csv.AppendLine($"\"Тип счета\",\"{account.AccountTypeName}\"");
                    csv.AppendLine($"\"Баланс\",\"{account.FormattedBalance}\"");
                    csv.AppendLine($"\"Период\",\"{dpStartDate.SelectedDate:dd.MM.yyyy} - {dpEndDate.SelectedDate:dd.MM.yyyy}\"");
                    csv.AppendLine();

                    csv.AppendLine($"\"Статистика за период\"");
                    csv.AppendLine($"\"Поступления\",\"{txtIncomes.Text}\"");
                    csv.AppendLine($"\"Расходы\",\"{txtExpenses.Text}\"");
                    csv.AppendLine($"\"Баланс\",\"{txtBalance.Text}\"");
                    csv.AppendLine($"\"Всего операций\",\"{txtTransactionsCount.Text}\"");
                    csv.AppendLine();

                    csv.AppendLine($"\"Дата\",\"Тип\",\"Сумма\",\"От кого\",\"Кому\",\"Описание\",\"Статус\"");

                    foreach (var transaction in _transactions.OrderByDescending(t => t.Date))
                    {
                        csv.AppendLine($"\"{transaction.Date:dd.MM.yyyy HH:mm:ss}\",\"{transaction.Type}\",\"{transaction.Amount:N2}\",\"{transaction.FromAccount ?? ""}\",\"{transaction.ToAccount ?? ""}\",\"{transaction.Description ?? ""}\",\"{transaction.Status}\"");
                    }

                    csv.AppendLine();
                    csv.AppendLine($"\"Итого записей: {_transactions.Count}\"");

                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);

                    MessageBox.Show($"Отчет успешно экспортирован!\n\nФайл: {saveFileDialog.FileName}", "Экспорт выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_transactions != null && _transactions.Any())
            {
                PrintReport();
            }
            else
            {
                MessageBox.Show("Нет данных для печати!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PrintReport()
        {
            try
            {
                Account selectedAccount = cmbAccounts.SelectedItem as Account;
                Client selectedClient = cmbClients.SelectedItem as Client;

                StringBuilder html = new StringBuilder();
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset='UTF-8'>");
                html.AppendLine("<title>Банковский отчет</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                html.AppendLine("h1 { color: #2C3E50; text-align: center; }");
                html.AppendLine("h2 { color: #34495E; margin-top: 20px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 10px; }");
                html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                html.AppendLine("th { background-color: #2C3E50; color: white; }");
                html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
                html.AppendLine(".stats { background-color: #F8F9FA; padding: 10px; margin: 10px 0; }");
                html.AppendLine(".footer { text-align: center; margin-top: 30px; font-size: 12px; color: #7F8C8D; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                html.AppendLine("<h1>БАНКОВСКАЯ СИСТЕМА</h1>");
                html.AppendLine("<h2>Отчет по банковскому счету</h2>");

                html.AppendLine("<div class='stats'>");
                html.AppendLine($"<p><strong>Дата формирования:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</p>");
                html.AppendLine($"<p><strong>Клиент:</strong> {selectedClient?.FullName ?? "Не указан"}</p>");
                html.AppendLine($"<p><strong>Номер счета:</strong> {selectedAccount?.AccountNumber}</p>");
                html.AppendLine($"<p><strong>Тип счета:</strong> {selectedAccount?.AccountTypeName}</p>");
                html.AppendLine($"<p><strong>Баланс:</strong> {selectedAccount?.FormattedBalance}</p>");
                html.AppendLine($"<p><strong>Период:</strong> {dpStartDate.SelectedDate:dd.MM.yyyy} - {dpEndDate.SelectedDate:dd.MM.yyyy}</p>");
                html.AppendLine("</div>");

                html.AppendLine("<h3>Статистика за период</h3>");
                html.AppendLine("<div class='stats'>");
                html.AppendLine($"<p><strong>📈 Поступления:</strong> {txtIncomes.Text}</p>");
                html.AppendLine($"<p><strong>📉 Расходы:</strong> {txtExpenses.Text}</p>");
                html.AppendLine($"<p><strong>💰 Баланс:</strong> {txtBalance.Text}</p>");
                html.AppendLine($"<p><strong>🔄 Всего операций:</strong> {txtTransactionsCount.Text}</p>");
                html.AppendLine("</div>");

                html.AppendLine("<h3>Детализация операций</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Дата</th><th>Тип</th><th>Сумма</th><th>От кого</th><th>Кому</th><th>Описание</th><th>Статус</th></tr>");

                foreach (var transaction in _transactions.OrderByDescending(t => t.Date))
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{transaction.Date:dd.MM.yyyy HH:mm:ss}</td>");
                    html.AppendLine($"<td>{transaction.Type}</td>");
                    html.AppendLine($"<td style='text-align: right'>{transaction.Amount:N2} ₽</td>");
                    html.AppendLine($"<td>{transaction.FromAccount ?? ""}</td>");
                    html.AppendLine($"<td>{transaction.ToAccount ?? ""}</td>");
                    html.AppendLine($"<td>{transaction.Description ?? ""}</td>");
                    html.AppendLine($"<td>{transaction.Status}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table>");
                html.AppendLine($"<p><strong>Итого записей:</strong> {_transactions.Count}</p>");
                html.AppendLine("<div class='footer'>");
                html.AppendLine("<p>© 2024 Банковская система. Все права защищены.</p>");
                html.AppendLine("</div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                string tempFile = Path.GetTempPath() + $"BankReport_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                File.WriteAllText(tempFile, html.ToString(), Encoding.UTF8);
                System.Diagnostics.Process.Start(tempFile);

                MessageBox.Show("Отчет открыт в браузере. Используйте печать браузера (Ctrl+P) для печати.",
                    "Печать", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}