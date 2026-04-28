using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class ClientSelectionDialog : Window
    {
        public Client SelectedClient { get; private set; }
        private List<Client> _clients;

        public ClientSelectionDialog()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadClients();
        }

        private async Task LoadClients(string search = null)
        {
            _clients = await App.Database.GetClientsAsync(search, true, false);
            dgClients.ItemsSource = _clients;
        }

        private async void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var search = txtSearch.Text == "Поиск клиента..." ? "" : txtSearch.Text;
                await LoadClients(search);
            }
        }

        private void DgClients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedClient = dgClients.SelectedItem as Client;
            if (SelectedClient != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            SelectedClient = dgClients.SelectedItem as Client;
            if (SelectedClient != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите клиента", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск клиента...")
                txtSearch.Text = "";
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Поиск клиента...";
        }
    }
}