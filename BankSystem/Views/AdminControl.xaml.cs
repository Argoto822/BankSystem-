using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BankSystem.Models;

namespace BankSystem.Views
{
    public partial class AdminControl : UserControl
    {
        private List<User> _users;
        private List<Role> _roles;
        private User _selectedUser;

        public AdminControl()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                await LoadUsers();
                await LoadRoles();
                await LoadLogs();
                await LoadUsersForLogFilter();
                await LoadSystemInfo();
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

        private async Task LoadSystemInfo()
        {
            try
            {
                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    await connection.OpenAsync();
                    txtDbStatus.Text = "✓ Подключено";
                    txtDbStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch
            {
                txtDbStatus.Text = "✗ Ошибка подключения";
                txtDbStatus.Foreground = System.Windows.Media.Brushes.Red;
            }

            using (var connection = new SqlConnection(App.Database.ConnectionString))
            {
                string query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM Users) as UserCount,
                        (SELECT COUNT(*) FROM Clients) as ClientCount,
                        (SELECT COUNT(*) FROM Accounts) as AccountCount,
                        (SELECT COUNT(*) FROM Transactions) as TransactionCount";

                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int userCount = Convert.ToInt32(reader["UserCount"]);
                            int clientCount = Convert.ToInt32(reader["ClientCount"]);
                            int accountCount = Convert.ToInt32(reader["AccountCount"]);
                            int transactionCount = Convert.ToInt32(reader["TransactionCount"]);

                            txtStats.Text = $"Пользователей: {userCount} | Клиентов: {clientCount} | Счетов: {accountCount} | Операций: {transactionCount}";
                        }
                    }
                }
            }

            if (App.Session.CurrentUser != null)
            {
                txtUserInfo.Text = $"{App.Session.CurrentUser.Login} ({App.Session.CurrentUser.RoleName})";
            }
        }

        private async Task LoadUsers()
        {
            _users = await GetUsersFromDb();
            dgUsers.ItemsSource = _users;
        }

        private async Task<List<User>> GetUsersFromDb()
        {
            var users = new List<User>();

            using (var connection = new SqlConnection(App.Database.ConnectionString))
            {
                string query = @"
                    SELECT u.id_user, u.login, u.email, u.is_active, u.last_login, r.name as role_name
                    FROM Users u
                    INNER JOIN Roles r ON u.id_role = r.id_role
                    ORDER BY u.id_user";

                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                Id = Convert.ToInt32(reader["id_user"]),
                                Login = reader["login"].ToString(),
                                Email = reader["email"].ToString(),
                                IsActive = Convert.ToBoolean(reader["is_active"]),
                                RoleName = reader["role_name"].ToString(),
                                LastLogin = reader["last_login"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["last_login"])
                                    : (DateTime?)null
                            });
                        }
                    }
                }
            }

            return users;
        }

        private async Task LoadRoles()
        {
            _roles = await App.Database.GetRolesAsync();
            cmbNewRole.ItemsSource = _roles;
            dgRoles.ItemsSource = _roles;
        }

        // ИСПРАВЛЕННЫЙ МЕТОД LoadLogs - использует TOP вместо LIMIT
        private async Task LoadLogs(DateTime? from = null, DateTime? to = null, int? userId = null)
        {
            var logs = new List<LogEntry>();

            using (var connection = new SqlConnection(App.Database.ConnectionString))
            {
                string query = @"
                    SELECT TOP 500 l.*, u.login as user_name
                    FROM Logs l
                    LEFT JOIN Users u ON l.id_user = u.id_user
                    WHERE 1=1";

                if (from.HasValue)
                    query += " AND l.timestamp >= @from";
                if (to.HasValue)
                    query += " AND l.timestamp <= @to";
                if (userId.HasValue)
                    query += " AND l.id_user = @userId";

                query += " ORDER BY l.timestamp DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    if (from.HasValue)
                        command.Parameters.AddWithValue("@from", from.Value);
                    if (to.HasValue)
                        command.Parameters.AddWithValue("@to", to.Value);
                    if (userId.HasValue)
                        command.Parameters.AddWithValue("@userId", userId.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new LogEntry
                            {
                                Id = Convert.ToInt32(reader["id_log"]),
                                UserId = reader["id_user"] != DBNull.Value ? Convert.ToInt32(reader["id_user"]) : (int?)null,
                                UserName = reader["user_name"]?.ToString(),
                                Action = reader["action"].ToString(),
                                EntityType = reader["entity_type"]?.ToString(),
                                EntityId = reader["entity_id"] != DBNull.Value ? Convert.ToInt32(reader["entity_id"]) : (int?)null,
                                Details = reader["details"]?.ToString(),
                                Timestamp = Convert.ToDateTime(reader["timestamp"])
                            });
                        }
                    }
                }
            }

            dgLogs.ItemsSource = logs;
        }

        private async Task LoadUsersForLogFilter()
        {
            var users = await GetUsersFromDb();
            users.Insert(0, new User { Id = 0, Login = "Все пользователи" });
            cmbLogUser.ItemsSource = users;
            cmbLogUser.SelectedIndex = 0;

            dpLogFrom.SelectedDate = DateTime.Now.AddDays(-30);
            dpLogTo.SelectedDate = DateTime.Now;
        }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = dgUsers.SelectedItem as User;
            bool hasSelection = _selectedUser != null;

            btnEditUser.IsEnabled = hasSelection;
            btnBlockUser.IsEnabled = hasSelection;
            btnDeleteUser.IsEnabled = hasSelection && _selectedUser.Login != "admin";
            btnResetPassword.IsEnabled = hasSelection;
        }

        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewLogin.Text))
            {
                txtLoginError.Text = "Введите логин";
                return;
            }
            else
            {
                txtLoginError.Text = "";
            }

            if (string.IsNullOrWhiteSpace(txtNewPassword.Password))
            {
                txtPasswordError.Text = "Введите пароль";
                return;
            }
            else if (txtNewPassword.Password.Length < 6)
            {
                txtPasswordError.Text = "Пароль должен быть не менее 6 символов";
                return;
            }
            else
            {
                txtPasswordError.Text = "";
            }

            if (string.IsNullOrWhiteSpace(txtNewEmail.Text))
            {
                txtEmailError.Text = "Введите email";
                return;
            }
            else if (!IsValidEmail(txtNewEmail.Text))
            {
                txtEmailError.Text = "Неверный формат email";
                return;
            }
            else
            {
                txtEmailError.Text = "";
            }

            if (cmbNewRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var role = (Role)cmbNewRole.SelectedItem;
                var passwordHash = HashPassword(txtNewPassword.Password);

                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    string query = @"
                        INSERT INTO Users (login, password_hash, email, id_role, is_active)
                        VALUES (@login, @password, @email, @roleId, @isActive)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", txtNewLogin.Text);
                        command.Parameters.AddWithValue("@password", passwordHash);
                        command.Parameters.AddWithValue("@email", txtNewEmail.Text);
                        command.Parameters.AddWithValue("@roleId", role.Id);
                        command.Parameters.AddWithValue("@isActive", chkIsActive.IsChecked ?? true);

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await AddLog("create_user", "user", null, $"Создан пользователь {txtNewLogin.Text}");

                MessageBox.Show("Пользователь успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                txtNewLogin.Text = "";
                txtNewPassword.Password = "";
                txtNewEmail.Text = "";
                cmbNewRole.SelectedIndex = -1;
                chkIsActive.IsChecked = true;

                await LoadUsers();
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

        private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            var dialog = new UserEditDialog(_selectedUser, _roles);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                await LoadUsers();
                await AddLog("edit_user", "user", _selectedUser.Id, $"Отредактирован пользователь {_selectedUser.Login}");
            }
        }

        private async void BtnBlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            string action = _selectedUser.IsActive ? "заблокировать" : "разблокировать";
            var result = MessageBox.Show($"Вы уверены, что хотите {action} пользователя {_selectedUser.Login}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    using (var connection = new SqlConnection(App.Database.ConnectionString))
                    {
                        string query = "UPDATE Users SET is_active = @isActive WHERE id_user = @userId";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@isActive", !_selectedUser.IsActive);
                            command.Parameters.AddWithValue("@userId", _selectedUser.Id);

                            await connection.OpenAsync();
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    await AddLog(_selectedUser.IsActive ? "block_user" : "unblock_user", "user",
                        _selectedUser.Id, $"{(_selectedUser.IsActive ? "Заблокирован" : "Разблокирован")} пользователь {_selectedUser.Login}");

                    await LoadUsers();
                    MessageBox.Show($"Пользователь {(_selectedUser.IsActive ? "заблокирован" : "разблокирован")}!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
        }

        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {_selectedUser.Login}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    using (var connection = new SqlConnection(App.Database.ConnectionString))
                    {
                        string query = "DELETE FROM Users WHERE id_user = @userId AND login != 'admin'";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", _selectedUser.Id);

                            await connection.OpenAsync();
                            int rows = await command.ExecuteNonQueryAsync();

                            if (rows > 0)
                            {
                                await AddLog("delete_user", "user", _selectedUser.Id, $"Удален пользователь {_selectedUser.Login}");
                                MessageBox.Show("Пользователь удален!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                await LoadUsers();
                            }
                            else
                            {
                                MessageBox.Show("Нельзя удалить администратора!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
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
        }

        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            var result = MessageBox.Show($"Сбросить пароль для пользователя {_selectedUser.Login}?\nНовый пароль: 123456",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    var newPasswordHash = HashPassword("123456");

                    using (var connection = new SqlConnection(App.Database.ConnectionString))
                    {
                        string query = "UPDATE Users SET password_hash = @password WHERE id_user = @userId";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@password", newPasswordHash);
                            command.Parameters.AddWithValue("@userId", _selectedUser.Id);

                            await connection.OpenAsync();
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    await AddLog("reset_password", "user", _selectedUser.Id, $"Сброшен пароль для пользователя {_selectedUser.Login}");

                    MessageBox.Show($"Пароль для пользователя {_selectedUser.Login} сброшен!\nНовый пароль: 123456", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
        }

        private async void BtnAddRole_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRoleName.Text))
            {
                MessageBox.Show("Введите название роли", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    string query = "INSERT INTO Roles (name, description) VALUES (@name, @desc)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", txtRoleName.Text);
                        command.Parameters.AddWithValue("@desc", txtRoleDesc.Text ?? (object)DBNull.Value);

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await AddLog("create_role", "role", null, $"Создана роль {txtRoleName.Text}");

                txtRoleName.Text = "";
                txtRoleDesc.Text = "";

                await LoadRoles();
                MessageBox.Show("Роль успешно добавлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void BtnDeleteRole_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var role = button?.Tag as Role;

            if (role == null) return;

            string[] systemRoles = { "Администратор", "Оператор", "Кассир", "Аналитик", "Клиент" };
            if (systemRoles.Contains(role.Name))
            {
                MessageBox.Show("Нельзя удалить системную роль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить роль '{role.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    using (var connection = new SqlConnection(App.Database.ConnectionString))
                    {
                        string query = "DELETE FROM Roles WHERE id_role = @roleId";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@roleId", role.Id);
                            await connection.OpenAsync();
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    await AddLog("delete_role", "role", role.Id, $"Удалена роль {role.Name}");
                    await LoadRoles();
                    MessageBox.Show("Роль удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
        }

        private async void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                int? userId = null;
                if (cmbLogUser.SelectedItem is User user && user.Id > 0)
                    userId = user.Id;

                await LoadLogs(dpLogFrom.SelectedDate, dpLogTo.SelectedDate, userId);
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите очистить журнал действий?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            loadingOverlay.Visibility = Visibility.Visible;

            try
            {
                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    string query = "DELETE FROM Logs WHERE timestamp < DATEADD(month, -3, GETDATE())";
                    using (var command = new SqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        int deleted = await command.ExecuteNonQueryAsync();
                        MessageBox.Show($"Удалено {deleted} записей старше 3 месяцев!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                await LoadLogs();
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

        private async void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SQL файлы (*.sql)|*.sql",
                DefaultExt = ".sql",
                FileName = $"BankSystem_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                loadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    await CreateDatabaseBackup(saveFileDialog.FileName);
                    MessageBox.Show($"Резервная копия создана!\n{saveFileDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания резервной копии: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async Task CreateDatabaseBackup(string filePath)
        {
            using (var connection = new SqlConnection(App.Database.ConnectionString))
            {
                string backupQuery = $@"
                    BACKUP DATABASE BankSystem 
                    TO DISK = '{filePath}'
                    WITH FORMAT, NAME = 'BankSystem Backup'";

                using (var command = new SqlCommand(backupQuery, connection))
                {
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task AddLog(string action, string entityType, int? entityId, string details)
        {
            try
            {
                using (var connection = new SqlConnection(App.Database.ConnectionString))
                {
                    string query = @"
                        INSERT INTO Logs (id_user, action, entity_type, entity_id, details, timestamp)
                        VALUES (@userId, @action, @entityType, @entityId, @details, GETDATE())";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", App.Session.CurrentUser?.Id ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@action", action);
                        command.Parameters.AddWithValue("@entityType", entityType);
                        command.Parameters.AddWithValue("@entityId", entityId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@details", details);

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddLog error: {ex.Message}");
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

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}