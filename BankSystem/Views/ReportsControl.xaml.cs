using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class ReportsControl : UserControl
    {
        private List<Client> _clients;
        private List<Account> _accounts;
        private List<Transaction> _transactions;
        private Account _selectedAccount;
        private Client _selectedClient;
        private DateTime _dateFrom;
        private DateTime _dateTo;

        public ReportsControl()
        {
            InitializeComponent();

            _dateFrom = DateTime.Now.AddMonths(-1);
            _dateTo = DateTime.Now;
            dpDateFrom.SelectedDate = _dateFrom;
            dpDateTo.SelectedDate = _dateTo;

            LoadClients();
        }

        private async void LoadClients()
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
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    _accounts = await App.Database.GetClientAccountsAsync(client.Id);

                    foreach (var account in _accounts)
                    {
                        account.DisplayName = $"{account.AccountName} - {account.AccountNumber} - {account.Balance:N2} ₽";
                    }

                    cmbAccounts.ItemsSource = _accounts;
                    cmbAccounts.DisplayMemberPath = "DisplayName";
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
        }

        private void CmbAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAccounts.SelectedItem is Account account)
            {
                _selectedAccount = account;
                btnExportPdf.IsEnabled = false;
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpDateFrom.SelectedDate.HasValue)
                _dateFrom = dpDateFrom.SelectedDate.Value;
            if (dpDateTo.SelectedDate.HasValue)
                _dateTo = dpDateTo.SelectedDate.Value;

            btnExportPdf.IsEnabled = false;
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAccount == null)
            {
                MessageBox.Show("Выберите счет", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _transactions = await App.Database.GetAccountTransactionsAsync(_selectedAccount.Id);

                var filteredTransactions = _transactions
                    .Where(t => t.Date.Date >= _dateFrom.Date && t.Date.Date <= _dateTo.Date)
                    .ToList();

                var income = filteredTransactions.Where(t => t.Type == "Пополнение" ||
                    (t.Type == "Перевод" && t.ToAccount == _selectedAccount.AccountNumber))
                    .Sum(t => t.Amount);

                var expense = filteredTransactions.Where(t => t.Type == "Снятие" ||
                    (t.Type == "Перевод" && t.FromAccount == _selectedAccount.AccountNumber))
                    .Sum(t => t.Amount);

                txtIncome.Text = $"{income:N2} ₽";
                txtExpense.Text = $"{expense:N2} ₽";
                txtTurnover.Text = $"{income + expense:N2} ₽";
                txtCount.Text = filteredTransactions.Count.ToString();

                dgTransactions.ItemsSource = filteredTransactions;
                txtEmpty.Visibility = filteredTransactions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                dgTransactions.Visibility = filteredTransactions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                btnExportPdf.IsEnabled = filteredTransactions.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAccount == null || _transactions == null || _transactions.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var filteredTransactions = _transactions
                    .Where(t => t.Date.Date >= _dateFrom.Date && t.Date.Date <= _dateTo.Date)
                    .ToList();

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Выписка_{_selectedClient?.FullName}_{_selectedAccount.AccountNumber}_{_dateFrom:yyyyMMdd}_{_dateTo:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => ExportToCsv(saveFileDialog.FileName, filteredTransactions));
                    MessageBox.Show($"Отчет успешно сохранен!\n{saveFileDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта CSV: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ExportToCsv(string filePath, List<Transaction> transactions)
        {
            var sb = new StringBuilder();

            // Добавляем информацию о клиенте и счете
            sb.AppendLine("БАНКОВСКАЯ СИСТЕМА - ВЫПИСКА ПО СЧЕТУ");
            sb.AppendLine($"Клиент: {_selectedClient?.FullName}");
            sb.AppendLine($"Номер счета: {_selectedAccount?.AccountNumber}");
            sb.AppendLine($"Тип счета: {_selectedAccount?.AccountName}");
            sb.AppendLine($"Период: {_dateFrom:dd.MM.yyyy} - {_dateTo:dd.MM.yyyy}");
            sb.AppendLine();

            // Заголовки таблицы
            sb.AppendLine("\"Дата\",\"Тип\",\"Со счета\",\"На счет\",\"Сумма\",\"Статус\",\"Назначение\"");

            // Данные
            foreach (var transaction in transactions)
            {
                sb.AppendLine($"\"{transaction.Date:dd.MM.yyyy HH:mm}\"," +
                             $"\"{transaction.Type}\"," +
                             $"\"{transaction.FromAccount ?? "—"}\"," +
                             $"\"{transaction.ToAccount ?? "—"}\"," +
                             $"\"{transaction.Amount:N2}\"," +
                             $"\"{transaction.Status}\"," +
                             $"\"{transaction.Description ?? ""}\"");
            }

            sb.AppendLine();

            // Итоги
            var income = transactions.Where(t => t.Type == "Пополнение" ||
                (t.Type == "Перевод" && t.ToAccount == _selectedAccount?.AccountNumber))
                .Sum(t => t.Amount);

            var expense = transactions.Where(t => t.Type == "Снятие" ||
                (t.Type == "Перевод" && t.FromAccount == _selectedAccount?.AccountNumber))
                .Sum(t => t.Amount);

            sb.AppendLine($"Итого поступлений:,{income:N2}");
            sb.AppendLine($"Итого расходов:,{expense:N2}");
            sb.AppendLine($"Оборот:,{(income + expense):N2}");
            sb.AppendLine($"Количество операций:,{transactions.Count}");
            sb.AppendLine();
            sb.AppendLine($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");

            // Сохраняем файл с кодировкой UTF-8
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}