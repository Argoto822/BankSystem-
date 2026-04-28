using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class DepositDialog : Window
    {
        public bool Success { get; private set; }
        public string TargetAccountNumber { get; private set; }
        public decimal Amount { get; private set; }
        public string Description { get; private set; }

        private List<Account> _accounts;

        public DepositDialog(List<Account> accounts)
        {
            InitializeComponent();
            _accounts = accounts.Where(a => a.Status == "Активен").ToList();
            cmbAccount.ItemsSource = _accounts;

            // Если есть счета, выбираем первый по умолчанию
            if (_accounts.Any())
            {
                cmbAccount.SelectedIndex = 0;
            }
        }

        private void CmbAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAccount.SelectedItem is Account selectedAccount)
            {
                txtAccountInfo.Text = $"{selectedAccount.AccountName} - {selectedAccount.AccountNumber}";
                txtCurrentBalance.Text = $"Текущий баланс: {selectedAccount.Balance:N2} {selectedAccount.Currency}";
            }
        }

        private void QuickAmount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string amountText = btn.Content.ToString().Replace(" ₽", "").Replace(" ", "");
                if (decimal.TryParse(amountText, out decimal amount))
                {
                    txtAmount.Text = amount.ToString("N0");
                    txtAmount.Focus();
                    txtAmount.SelectAll();
                }
            }
        }

        private void BtnDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAccount.SelectedItem == null)
            {
                MessageBox.Show("Выберите счет для пополнения", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму пополнения (больше 0)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ограничение на максимальную сумму
            if (amount > 10000000)
            {
                MessageBox.Show("Максимальная сумма пополнения: 10 000 000 ₽", "Ограничение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedAccount = cmbAccount.SelectedItem as Account;
            TargetAccountNumber = selectedAccount.AccountNumber;
            Amount = amount;
            Description = $"Пополнение счета на {amount:N2} ₽";
            Success = true;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Success = false;
            DialogResult = false;
            Close();
        }

        private void TxtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void TxtAmount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnDeposit_Click(sender, null);
                e.Handled = true;
            }
        }
    }
}