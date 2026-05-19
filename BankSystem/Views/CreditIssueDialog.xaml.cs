using System;
using System.Windows;
using BankSystem.Models;
using BankSystem.Services;
using BankSystem.ViewModels;

namespace BankSystem.Views
{
    public partial class CreditIssueDialog : Window
    {
        private CreditIssueViewModel _viewModel;
        private DatabaseService _databaseService;

        public CreditAccount IssuedCredit { get; private set; }
        public string CreatedCreditAccountNumber { get; private set; }

        public CreditIssueDialog()
        {
            InitializeComponent();
            _viewModel = new CreditIssueViewModel();
            _databaseService = new DatabaseService();
            DataContext = _viewModel;
        }

        private void SelectClient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientSelectionDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.SelectedClient != null)
            {
                _viewModel.ClientId = dialog.SelectedClient.Id;
                _viewModel.ClientFullName = dialog.SelectedClient.FullName;
            }
        }

        private async void IssueCredit_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ClientId <= 0)
            {
                MessageBox.Show("Выберите клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_viewModel.Purpose))
            {
                var result = MessageBox.Show("Вы не указали назначение кредита. Продолжить?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                // 1. Сначала создаем кредитный счет для клиента
                var accountResult = await _databaseService.CreateCreditAccountAsync(
                    _viewModel.ClientId,
                    _viewModel.Amount,
                    App.Session.CurrentUser?.Id ?? 1);

                if (!accountResult.Success)
                {
                    MessageBox.Show($"Ошибка создания кредитного счета: {accountResult.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CreatedCreditAccountNumber = accountResult.AccountNumber;

                // 2. Создаем запись о кредите
                var credit = new CreditAccount
                {
                    ClientId = _viewModel.ClientId,
                    ClientFullName = _viewModel.ClientFullName,
                    Amount = _viewModel.Amount,
                    InterestRate = _viewModel.InterestRate,
                    TermMonths = _viewModel.TermMonths,
                    MonthlyPayment = _viewModel.MonthlyPayment,
                    RemainingDebt = _viewModel.TotalPayments,
                    IssueDate = DateTime.Now,
                    NextPaymentDate = DateTime.Now.AddMonths(1),
                    Status = "Active",
                    PaymentType = _viewModel.PaymentType,
                    AccountNumber = CreatedCreditAccountNumber,
                    TotalPaid = 0,
                    TotalInterestPaid = 0,
                    Purpose = _viewModel.Purpose
                };

                bool saved = await _databaseService.SaveCreditAsync(credit);

                if (saved)
                {
                    IssuedCredit = credit;

                    MessageBox.Show($"Кредит успешно оформлен!\n\n" +
                        $"Клиент: {_viewModel.ClientFullName}\n" +
                        $"Сумма кредита: {_viewModel.Amount:N0} ₽\n" +
                        $"Ежемесячный платеж: {_viewModel.MonthlyPayment:N2} ₽\n" +
                        $"Срок: {_viewModel.TermMonths} месяцев\n" +
                        $"Процентная ставка: {_viewModel.InterestRate}%\n" +
                        $"Тип платежа: {_viewModel.PaymentType}\n" +
                        $"\n🏦 Кредитный счет: {CreatedCreditAccountNumber}\n" +
                        $"💰 Сумма кредита зачислена на кредитный счет\n" +
                        $"📅 Первый платеж до: {DateTime.Now.AddMonths(1):dd.MM.yyyy}",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении кредита в базу данных!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении кредита: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}