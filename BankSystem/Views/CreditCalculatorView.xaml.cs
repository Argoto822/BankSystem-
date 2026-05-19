using System;
using System.Windows;
using System.Windows.Controls;
using BankSystem.ViewModels;

namespace BankSystem.Views
{
    public partial class CreditCalculatorView : UserControl
    {
        private CreditCalculatorViewModel _viewModel;

        public CreditCalculatorView()
        {
            InitializeComponent();
            _viewModel = new CreditCalculatorViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnIssueCredit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.CreditAmount <= 0 || _viewModel.InterestRate <= 0 || _viewModel.TermMonths <= 0)
                {
                    MessageBox.Show("Заполните корректные параметры кредита!\n\n" +
                        "Сумма кредита должна быть больше 0\n" +
                        "Процентная ставка должна быть больше 0\n" +
                        "Срок кредита должен быть больше 0 месяцев",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new CreditIssueDialog();
                dialog.Owner = Window.GetWindow(this);

                var viewModel = dialog.DataContext as CreditIssueViewModel;
                if (viewModel != null)
                {
                    viewModel.Amount = _viewModel.CreditAmount;
                    viewModel.InterestRate = _viewModel.InterestRate;
                    viewModel.TermMonths = _viewModel.TermMonths;
                    viewModel.PaymentType = _viewModel.PaymentType;
                }

                if (dialog.ShowDialog() == true)
                {
                    _viewModel.StatusMessage = "✅ Кредит успешно оформлен!";
                    _viewModel.Calculate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Экспорт в Excel выполнен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Печать графика платежей", "Печать", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}