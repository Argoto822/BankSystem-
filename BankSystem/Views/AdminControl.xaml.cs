using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;
using System.Security.Cryptography;
using System.Text;

namespace BankSystem.Views
{
    public partial class AdminControl : UserControl
    {
        private List<User> _users;
        private User _selectedUser;
        private List<Role> _roles;

        public AdminControl()
        {
            InitializeComponent();
            this.Loaded += async (sender, e) => await LoadData();
        }

        private async Task LoadData()
        {
            loadingOverlay.Visibility = Visibility.Visible;
            try
            {
                await LoadRoles();
                await LoadUsers();
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

        private async Task LoadRoles()
        {
            _roles = await App.Database.GetRolesAsync();
            cmbRole.ItemsSource = _roles;
            if (_roles != null && _roles.Any())
                cmbRole.SelectedIndex = 0;
        }

        private async Task LoadUsers()
        {
            _users = await App.Database.GetUsersAsync();
            dgUsers.ItemsSource = _users;
            UpdateButtonsState();
        }

        private void UpdateButtonsState()
        {
            bool hasSelection = _selectedUser != null;
            btnResetPassword.IsEnabled = hasSelection;
            if (hasSelection && _selectedUser != null)
            {
                btnBlock.IsEnabled = _selectedUser.IsActive;
                btnUnblock.IsEnabled = !_selectedUser.IsActive;
            }
            else
            {
                btnBlock.IsEnabled = false;
                btnUnblock.IsEnabled = false;
            }
        }

        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var role = cmbRole.SelectedItem as Role;
                string passwordHash = HashPassword(txtPassword.Password);

                var newUser = new User
                {
                    Login = txtLogin.Text.Trim(),
                    PasswordHash = passwordHash,
                    Email = txtEmail.Text.Trim(),
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsActive = true
                };

                bool success = await App.Database.CreateUserAsync(newUser, App.Session.CurrentUser?.Id ?? 1);

                if (success)
                {
                    MessageBox.Show($"Пользователь {txtLogin.Text} успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем форму
                    txtLogin.Text = "";
                    txtPassword.Password = "";
                    txtEmail.Text = "";
                    await LoadUsers();
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении пользователя. Возможно, логин уже существует.", "Ошибка",
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

        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            var result = MessageBox.Show(
                $"Сбросить пароль для пользователя {_selectedUser.Login}?\n\nНовый пароль будет: 123456",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                string newPasswordHash = HashPassword("123456");
                bool success = await App.Database.ResetUserPasswordAsync(_selectedUser.Id, newPasswordHash);

                if (success)
                {
                    MessageBox.Show($"Пароль для {_selectedUser.Login} сброшен на '123456'", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при сбросе пароля", "Ошибка",
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

        private async void BtnBlock_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            var result = MessageBox.Show(
                $"Заблокировать пользователя {_selectedUser.Login}?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                bool success = await App.Database.SetUserActiveStatusAsync(_selectedUser.Id, false);

                if (success)
                {
                    await LoadUsers();
                    MessageBox.Show($"Пользователь {_selectedUser.Login} заблокирован", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при блокировке", "Ошибка",
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

        private async void BtnUnblock_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            var result = MessageBox.Show(
                $"Активировать пользователя {_selectedUser.Login}?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                bool success = await App.Database.SetUserActiveStatusAsync(_selectedUser.Id, true);

                if (success)
                {
                    await LoadUsers();
                    MessageBox.Show($"Пользователь {_selectedUser.Login} активирован", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при активации", "Ошибка",
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

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem is User user)
            {
                _selectedUser = user;
            }
            else
            {
                _selectedUser = null;
            }
            UpdateButtonsState();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}