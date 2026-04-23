using System.Windows;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class ClientCardWindow : Window
    {
        private Client _client;

        public ClientCardWindow(Client client)
        {
            InitializeComponent();
            _client = client;
            LoadClientData();
            LoadStats();
        }

        private void LoadClientData()
        {
            txtFullName.Text = _client.FullName;
            txtType.Text = _client.ClientType == "individual" ? "Физическое лицо" : "Юридическое лицо";
            txtPassport.Text = _client.PassportInn;
            txtPhone.Text = _client.Phone;
            txtEmail.Text = _client.Email;
            txtAddress.Text = _client.Address;
            txtRegDate.Text = _client.RegistrationDate.ToString("dd.MM.yyyy");
        }

        private async void LoadStats()
        {
            var stats = await App.Database.GetClientStatsAsync(_client.Id);
            txtAccountCount.Text = stats.AccountCount.ToString();
            txtTotalBalance.Text = $"{stats.TotalBalance:N2} ₽";
            txtMaxBalance.Text = $"{stats.MaxBalance:N2} ₽";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}