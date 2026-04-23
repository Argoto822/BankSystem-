using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class ClientDialog : Window
    {
        public Client Client { get; private set; }
        public string CreatedAccountNumber1 { get; private set; }
        public string CreatedAccountNumber2 { get; private set; }

        public ClientDialog()
        {
            InitializeComponent();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ShowError("Введите ФИО/название клиента");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassport.Text))
            {
                ShowError("Введите паспортные данные или ИНН");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                ShowError("Введите номер телефона");
                return;
            }

            btnSave.IsEnabled = false;
            txtError.Visibility = Visibility.Collapsed;

            Client = new Client
            {
                FullName = txtFullName.Text,
                ClientType = ((ComboBoxItem)cmbType.SelectedItem).Tag.ToString(),
                PassportInn = txtPassport.Text,
                Phone = txtPhone.Text,
                Email = txtEmail.Text,
                Address = txtAddress.Text,
                IsActive = true
            };

            try
            {
                // Используем метод с двумя счетами
                var result = await App.Database.CreateClientWithTwoAccountsAsync(Client, App.Session.CurrentUser?.Id ?? 1);

                if (result.ClientId > 0 && !string.IsNullOrEmpty(result.AccountNumber1))
                {
                    CreatedAccountNumber1 = result.AccountNumber1;
                    CreatedAccountNumber2 = result.AccountNumber2;
                    DialogResult = true;

                    MessageBox.Show(
                        $"Клиент успешно добавлен!\n\n" +
                        $"Клиент: {Client.FullName}\n" +
                        $"Тип: {(Client.ClientType == "individual" ? "Физическое лицо" : "Юридическое лицо")}\n\n" +
                        $"Открыты счета:\n" +
                        $"  • Основной счет: {result.AccountNumber1}\n" +
                        $"  • Сберегательный счет: {result.AccountNumber2}\n\n" +
                        $"Баланс каждого счета: 0.00 ₽\n\n" +
                        $"Вы можете пополнить счета через кассу или переводом.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Close();
                }
                else
                {
                    ShowError("Ошибка при создании клиента");
                    btnSave.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
                btnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}