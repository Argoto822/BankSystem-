using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.Views
{
    public partial class ClientSelectionDialog : Window
    {
        public Client SelectedClient { get; private set; }
        private DatabaseService _databaseService;

        public ClientSelectionDialog()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadClientsFromDatabase();
        }

        private async void LoadClientsFromDatabase()
        {
            try
            {
                var clients = await _databaseService.GetClientsAsync();

                if (clients != null && clients.Any())
                {
                    foreach (var client in clients)
                    {
                        client.Passport = client.PassportInn;
                    }
                    dgClients.ItemsSource = clients;
                }
                else
                {
                    LoadTestClients();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки клиентов: {ex.Message}");
                LoadTestClients();
            }
        }

        private void LoadTestClients()
        {
            var clients = new List<Client>
            {
                new Client { Id = 1, FullName = "Иванов Иван Иванович", Phone = "+7 (123) 456-78-90", Passport = "4510 123456", Email = "ivanov@mail.ru", RegistrationDate = new DateTime(2026, 4, 28) },
                new Client { Id = 2, FullName = "Петров Петр Петрович", Phone = "+7 (987) 654-32-10", Passport = "4510 654321", Email = "petrov@mail.ru", RegistrationDate = new DateTime(2026, 4, 28) },
                new Client { Id = 3, FullName = "Тестов Тест Тестович", Phone = "+7 (999) 999-99-99", Passport = "9999 999999", Email = "test@test.ru", RegistrationDate = new DateTime(2026, 4, 28) },
                new Client { Id = 4, FullName = "Логинова Анна Александровна", Phone = "89217202380", Passport = "123156", Email = "ann@gmail.com", RegistrationDate = new DateTime(2026, 4, 28) },
                new Client { Id = 7, FullName = "Логинов Дмитрий Андреевич", Phone = "4546132154", Passport = "456789", Email = "dima@yandex.ru", RegistrationDate = new DateTime(2026, 4, 28) },
                new Client { Id = 8, FullName = "Веричева Ирина", Phone = "2132146545", Passport = "4654564", Email = "irin@yandex.ru", RegistrationDate = new DateTime(2026, 4, 28) }
            };
            dgClients.ItemsSource = clients;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text.ToLower();
            var clients = (dgClients.ItemsSource as IEnumerable<Client>)?.ToList() ?? new List<Client>();

            if (string.IsNullOrEmpty(searchText))
            {
                dgClients.ItemsSource = clients;
            }
            else
            {
                var filtered = clients.Where(c =>
                    c.FullName.ToLower().Contains(searchText) ||
                    (c.Phone != null && c.Phone.Contains(searchText)) ||
                    (c.Passport != null && c.Passport.Contains(searchText)) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchText))
                ).ToList();
                dgClients.ItemsSource = filtered;
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            SelectedClient = dgClients.SelectedItem as Client;
            if (SelectedClient != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите клиента!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}