using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BankSystem.Models;
using System.Data.SqlClient;

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

            System.Diagnostics.Debug.WriteLine($"=== ПОПЫТКА ВХОДА ===");
            System.Diagnostics.Debug.WriteLine($"Логин: {login}");
            System.Diagnostics.Debug.WriteLine($"Пароль: {password}");

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            btnLogin.IsEnabled = false;
            txtError.Visibility = Visibility.Collapsed;

            try
            {
                // Сначала проверяем подключение к БД
                System.Diagnostics.Debug.WriteLine("Проверяем подключение к БД...");
                bool dbConnected = await TestDatabaseConnection();
                System.Diagnostics.Debug.WriteLine($"Подключение к БД: {(dbConnected ? "Успешно" : "Ошибка")}");

                if (!dbConnected)
                {
                    ShowError("Не удалось подключиться к базе данных");
                    btnLogin.IsEnabled = true;
                    return;
                }

                // Проверяем пользователя
                System.Diagnostics.Debug.WriteLine("Проверяем пользователя...");
                var user = await AuthenticateUser(login, password);

                System.Diagnostics.Debug.WriteLine($"Результат аутентификации: {(user != null ? $"Успех: {user.Login}" : "Неудача")}");

                if (user != null)
                {
                    App.Session.CurrentUser = user;
                    System.Diagnostics.Debug.WriteLine("Пользователь установлен в сессию");

                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    ShowError("Неверный логин или пароль. Доступ запрещен.");
                    txtPassword.Clear();
                    txtPassword.Focus();
                    btnLogin.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                ShowError($"Ошибка: {ex.Message}");
                btnLogin.IsEnabled = true;
            }
        }

        private async Task<bool> TestDatabaseConnection()
        {
            try
            {
                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        private async Task<User> AuthenticateUser(string login, string password)
        {
            try
            {
                string passwordHash = HashPassword(password);
                System.Diagnostics.Debug.WriteLine($"Хеш пароля: {passwordHash}");

                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    string query = @"
                        SELECT u.id_user, u.login, u.email, r.name as role_name, u.is_active
                        FROM Users u
                        LEFT JOIN Roles r ON u.id_role = r.id_role
                        WHERE u.login = @login AND u.password_hash = @password_hash AND u.is_active = 1";

                    System.Diagnostics.Debug.WriteLine($"SQL: {query}");

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password_hash", passwordHash);

                        await connection.OpenAsync();
                        System.Diagnostics.Debug.WriteLine("Соединение с БД открыто");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var user = new User
                                {
                                    Id = Convert.ToInt32(reader["id_user"]),
                                    Login = reader["login"].ToString(),
                                    Email = reader["email"]?.ToString(),
                                    RoleName = reader["role_name"]?.ToString() ?? "Пользователь",
                                    IsActive = Convert.ToBoolean(reader["is_active"])
                                };
                                System.Diagnostics.Debug.WriteLine($"Найден пользователь: Id={user.Id}, Login={user.Login}, Role={user.RoleName}");
                                return user;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Пользователь не найден в БД");
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuthenticateUser error: {ex.Message}");
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
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