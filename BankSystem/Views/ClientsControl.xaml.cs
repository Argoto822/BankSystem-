using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class ClientsControl : UserControl
    {
        private List<Client> _clients;
        private Client _selectedClient;
        private bool _showDeleted = false;

        public ClientsControl()
        {
            InitializeComponent();
            LoadClientsAsync();
        }

        private async Task LoadClientsAsync(string search = "")
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                if (_showDeleted)
                {
                    _clients = await App.Database.GetDeletedClientsAsync(search);
                    btnRestore.Visibility = Visibility.Visible;
                    btnDelete.Visibility = Visibility.Collapsed;
                    btnShowDeleted.Content = "📋 КЛИЕНТЫ";
                }
                else
                {
                    _clients = await App.Database.GetClientsAsync(search, true, false);
                    btnRestore.Visibility = Visibility.Collapsed;
                    btnDelete.Visibility = Visibility.Visible;
                    btnShowDeleted.Content = "🗑️ КОРЗИНА";
                }

                dgClients.ItemsSource = _clients;
                txtEmpty.Visibility = _clients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                dgClients.Visibility = _clients.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                btnEdit.IsEnabled = false;
                btnDelete.IsEnabled = false;
                btnRestore.IsEnabled = false;
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

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var search = txtSearch.Text == "Поиск по ФИО, телефону или паспорту" ? "" : txtSearch.Text;
            LoadClientsAsync(search);
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnSearch_Click(null, null);
        }

        private void BtnShowDeleted_Click(object sender, RoutedEventArgs e)
        {
            _showDeleted = !_showDeleted;
            LoadClientsAsync();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.Client != null)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                var result = await App.Database.CreateClientWithTwoAccountsAsync(dialog.Client, App.Session.CurrentUser?.Id ?? 1);

                if (result.ClientId > 0)
                {
                    await LoadClientsAsync();

                    MessageBox.Show(
                        $"Клиент {dialog.Client.FullName} успешно добавлен!\n\n" +
                        $"Открыты счета:\n" +
                        $"  • Основной счет: {result.AccountNumber1}\n" +
                        $"  • Сберегательный счет: {result.AccountNumber2}\n\n" +
                        $"Баланс каждого счета: 0.00 ₽",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void DgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedClient = dgClients.SelectedItem as Client;
            bool hasSelection = _selectedClient != null;

            btnEdit.IsEnabled = hasSelection;

            if (_showDeleted)
            {
                btnRestore.IsEnabled = hasSelection;
                btnDelete.IsEnabled = false;
            }
            else
            {
                btnDelete.IsEnabled = hasSelection;
                btnRestore.IsEnabled = false;
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                var dialog = new ClientCardWindow(_selectedClient);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                LoadClientsAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;

            var result = MessageBox.Show(
                $"Переместить клиента {_selectedClient.FullName} в корзину?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var success = await App.Database.SoftDeleteClientAsync(_selectedClient.Id, App.Session.CurrentUser?.Id ?? 1);

                if (success)
                {
                    MessageBox.Show($"Клиент {_selectedClient.FullName} перемещен в корзину!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadClientsAsync();
                }
                else
                {
                    MessageBox.Show("Ошибка при удалении клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;

            var result = MessageBox.Show(
                $"Восстановить клиента {_selectedClient.FullName}?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var success = await App.Database.RestoreClientAsync(_selectedClient.Id, App.Session.CurrentUser?.Id ?? 1);

                if (success)
                {
                    MessageBox.Show($"Клиент {_selectedClient.FullName} восстановлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadClientsAsync();
                }
                else
                {
                    MessageBox.Show("Ошибка при восстановлении клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск по ФИО, телефону или паспорту")
                txtSearch.Text = "";
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                txtSearch.Text = "Поиск по ФИО, телефону или паспорту";
        }
    }
}