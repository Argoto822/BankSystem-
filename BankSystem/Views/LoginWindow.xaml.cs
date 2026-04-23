using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtLogin.Text = "admin";
            txtPassword.Password = "admin123";

            txtLogin.KeyDown += OnKeyDown;
            txtPassword.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnLogin_Click(null, null);
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var login = txtLogin.Text.Trim();
            var password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            btnLogin.IsEnabled = false;
            txtError.Visibility = Visibility.Collapsed;

            // Хешируем введенный пароль
            var passwordHash = HashPassword(password);

            // Проверяем пользователя в базе данных
            var user = await AuthenticateUser(login, passwordHash);

            if (user != null)
            {
                App.Session.SetCurrentUser(user);
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            else
            {
                ShowError("Неверный логин или пароль");
                txtPassword.Clear();
                btnLogin.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task<User> AuthenticateUser(string login, string passwordHash)
        {
            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(App.Database.ConnectionString))
                {
                    string query = @"
                        SELECT u.id_user, u.login, u.email, r.name as role_name, u.is_active
                        FROM Users u
                        LEFT JOIN Roles r ON u.id_role = r.id_role
                        WHERE u.login = @login AND u.password_hash = @passwordHash AND u.is_active = 1";

                    using (var command = new System.Data.SqlClient.SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@passwordHash", passwordHash);

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User
                                {
                                    Id = Convert.ToInt32(reader["id_user"]),
                                    Login = reader["login"].ToString(),
                                    Email = reader["email"]?.ToString(),
                                    RoleName = reader["role_name"]?.ToString() ?? "Пользователь",
                                    IsActive = Convert.ToBoolean(reader["is_active"])
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auth error: {ex.Message}");
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}