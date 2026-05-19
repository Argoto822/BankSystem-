using BankSystem.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BankSystem.Views
{
    public partial class OpenAccountDialog : Window
    {
        private DatabaseService _databaseService;
        private int _clientId;

        public OpenAccountDialog()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            cmbAccountType.SelectedIndex = 0;
        }

        public void SetClientId(int clientId)
        {
            _clientId = clientId;
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAccountName.Text))
            {
                MessageBox.Show("Введите название счета!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string accountType = (cmbAccountType.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "current";
                string accountName = txtAccountName.Text.Trim();
                string currency = (cmbCurrency.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "RUB";

                bool result = await _databaseService.OpenAccountAsync(_clientId, accountType, App.Session.CurrentUser?.Id ?? 1);

                // Дополнительное обновление названия и валюты счета
                if (result)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при открытии счета!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}