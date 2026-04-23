using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class ClientSelectionDialog : Window
    {
        private List<Client> _allClients;
        private List<Client> _filteredClients;

        public Client SelectedClient { get; private set; }

        public ClientSelectionDialog(List<Client> clients)
        {
            InitializeComponent();
            _allClients = clients;
            _filteredClients = new List<Client>(clients);
            lstClients.ItemsSource = _filteredClients;
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск по имени или телефону")
                txtSearch.Text = "";
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Поиск по имени или телефону";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text;
            if (string.IsNullOrWhiteSpace(search) || search == "Поиск по имени или телефону")
            {
                _filteredClients = new List<Client>(_allClients);
            }
            else
            {
                _filteredClients = _allClients.Where(c =>
                    c.FullName.ToLower().Contains(search.ToLower()) ||
                    (c.Phone != null && c.Phone.Contains(search))).ToList();
            }
            lstClients.ItemsSource = _filteredClients;
        }

        private void LstClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSelect.IsEnabled = lstClients.SelectedItem != null;
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (lstClients.SelectedItem is Client client)
            {
                SelectedClient = client;
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}