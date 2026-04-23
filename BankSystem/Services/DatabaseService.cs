using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BankSystem.Models;

namespace BankSystem.Services
{
    public class DatabaseService
    {
        // ИСПРАВЛЕННАЯ строка подключения - укажите ваш сервер
        private readonly string _connectionString = @"Server=DESKTOP-94H8IDC\SQLEXPRESS;Database=BankSystem;Trusted_Connection=True;";

        // Публичное свойство для доступа из других классов
        public string ConnectionString => _connectionString;

        public DatabaseService()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    System.Diagnostics.Debug.WriteLine("✓ Подключение к БД успешно!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Ошибка подключения: {ex.Message}");
            }
        }

        // ТЕСТОВАЯ АВТОРИЗАЦИЯ (без БД) - для проверки работы приложения
        public async Task<User> AuthenticateTestAsync(string login, string password)
        {
            if (login == "admin" && password == "admin123")
            {
                return await Task.FromResult(new User
                {
                    Id = 1,
                    Login = "admin",
                    Email = "admin@bank.local",
                    RoleName = "Администратор",
                    IsActive = true
                });
            }
            return await Task.FromResult<User>(null);
        }

        // РЕАЛЬНАЯ АВТОРИЗАЦИЯ (с БД) - УЛУЧШЕННАЯ ВЕРСИЯ
        public async Task<User> AuthenticateAsync(string login, string passwordHash)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("sp_AuthenticateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password_hash", passwordHash);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string status = reader["status"].ToString();

                            if (status == "SUCCESS")
                            {
                                return new User
                                {
                                    Id = Convert.ToInt32(reader["id_user"]),
                                    Login = reader["login"].ToString(),
                                    Email = reader["email"]?.ToString(),
                                    RoleName = reader["role"].ToString(),
                                    IsActive = true
                                };
                            }
                            else
                            {
                                string errorMessage = reader["message"]?.ToString() ?? "Неизвестная ошибка";
                                System.Diagnostics.Debug.WriteLine($"Auth failed: {errorMessage}");
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

        // =============================================
        // ПОЛУЧЕНИЕ КЛИЕНТОВ (С ПОДДЕРЖКОЙ МЯГКОГО УДАЛЕНИЯ)
        // =============================================
        public async Task<List<Client>> GetClientsAsync(string search = null, bool showActiveOnly = true, bool includeDeleted = false)
        {
            var clients = new List<Client>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT id_client, client_type, full_name, passport_inn, phone, email, address, registration_date, is_active, is_deleted 
                        FROM Clients";

                    List<string> conditions = new List<string>();

                    if (showActiveOnly)
                        conditions.Add("is_active = 1");

                    if (!includeDeleted)
                        conditions.Add("is_deleted = 0");

                    if (conditions.Count > 0)
                        query += " WHERE " + string.Join(" AND ", conditions);

                    if (!string.IsNullOrWhiteSpace(search))
                        query += (conditions.Count > 0 ? " AND" : " WHERE") + " (full_name LIKE @search OR phone LIKE @search OR passport_inn LIKE @search)";

                    query += " ORDER BY full_name";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(search))
                            command.Parameters.AddWithValue("@search", $"%{search}%");

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                clients.Add(new Client
                                {
                                    Id = Convert.ToInt32(reader["id_client"]),
                                    ClientType = reader["client_type"].ToString(),
                                    FullName = reader["full_name"].ToString(),
                                    PassportInn = reader["passport_inn"].ToString(),
                                    Phone = reader["phone"].ToString(),
                                    Email = reader["email"]?.ToString(),
                                    Address = reader["address"]?.ToString(),
                                    RegistrationDate = Convert.ToDateTime(reader["registration_date"]),
                                    IsActive = Convert.ToBoolean(reader["is_active"]),
                                    IsDeleted = Convert.ToBoolean(reader["is_deleted"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetClients error: {ex.Message}");
            }
            return clients;
        }

        // =============================================
        // ПОЛУЧЕНИЕ УДАЛЕННЫХ КЛИЕНТОВ
        // =============================================
        public async Task<List<Client>> GetDeletedClientsAsync(string search = null)
        {
            var clients = new List<Client>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT id_client, client_type, full_name, passport_inn, phone, email, address, registration_date, is_active, is_deleted 
                        FROM Clients 
                        WHERE is_deleted = 1";

                    if (!string.IsNullOrWhiteSpace(search))
                        query += " AND (full_name LIKE @search OR phone LIKE @search OR passport_inn LIKE @search)";

                    query += " ORDER BY full_name";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(search))
                            command.Parameters.AddWithValue("@search", $"%{search}%");

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                clients.Add(new Client
                                {
                                    Id = Convert.ToInt32(reader["id_client"]),
                                    ClientType = reader["client_type"].ToString(),
                                    FullName = reader["full_name"].ToString(),
                                    PassportInn = reader["passport_inn"].ToString(),
                                    Phone = reader["phone"].ToString(),
                                    Email = reader["email"]?.ToString(),
                                    Address = reader["address"]?.ToString(),
                                    RegistrationDate = Convert.ToDateTime(reader["registration_date"]),
                                    IsActive = Convert.ToBoolean(reader["is_active"]),
                                    IsDeleted = Convert.ToBoolean(reader["is_deleted"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDeletedClients error: {ex.Message}");
            }
            return clients;
        }

        // =============================================
        // МЯГКОЕ УДАЛЕНИЕ КЛИЕНТА
        // =============================================
        public async Task<bool> SoftDeleteClientAsync(int clientId, int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Помечаем клиента как удаленного
                            string updateClientQuery = @"
                                UPDATE Clients 
                                SET is_deleted = 1, updated_at = GETDATE() 
                                WHERE id_client = @clientId";

                            using (var cmd = new SqlCommand(updateClientQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // 2. Помечаем счета клиента как удаленные
                            string updateAccountsQuery = @"
                                UPDATE Accounts 
                                SET is_deleted = 1, status = 'closed', updated_at = GETDATE()
                                WHERE id_client = @clientId";

                            using (var cmd = new SqlCommand(updateAccountsQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoftDeleteClient error: {ex.Message}");
                return false;
            }
        }

        // =============================================
        // ВОССТАНОВЛЕНИЕ КЛИЕНТА
        // =============================================
        public async Task<bool> RestoreClientAsync(int clientId, int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Восстанавливаем клиента
                            string updateClientQuery = @"
                                UPDATE Clients 
                                SET is_deleted = 0, updated_at = GETDATE() 
                                WHERE id_client = @clientId";

                            using (var cmd = new SqlCommand(updateClientQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // 2. Восстанавливаем счета
                            string updateAccountsQuery = @"
                                UPDATE Accounts 
                                SET is_deleted = 0, status = 'active', updated_at = GETDATE()
                                WHERE id_client = @clientId";

                            using (var cmd = new SqlCommand(updateAccountsQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreClient error: {ex.Message}");
                return false;
            }
        }

        // =============================================
        // ПОЛУЧЕНИЕ СЧЕТОВ КЛИЕНТА (С УЧЕТОМ УДАЛЕНИЯ)
        // =============================================
        public async Task<List<Account>> GetClientAccountsAsync(int clientId, bool includeDeleted = false)
        {
            var accounts = new List<Account>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT id_account, account_number, account_type, account_name, balance, currency, status, opening_date, is_deleted
                        FROM Accounts 
                        WHERE id_client = @clientId";

                    if (!includeDeleted)
                        query += " AND is_deleted = 0";

                    query += " AND status != 'closed'";
                    query += " ORDER BY account_type DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string accountType = reader["account_type"].ToString();
                                string typeName = accountType == "current" ? "Текущий" : (accountType == "saving" ? "Сберегательный" : "Карточный");
                                string statusName = reader["status"].ToString() == "active" ? "Активен" : (reader["status"].ToString() == "blocked" ? "Заблокирован" : "Закрыт");
                                string accountName = reader["account_name"]?.ToString() ?? typeName;

                                accounts.Add(new Account
                                {
                                    Id = Convert.ToInt32(reader["id_account"]),
                                    AccountNumber = reader["account_number"].ToString(),
                                    AccountType = accountType,
                                    AccountName = accountName,
                                    Balance = Convert.ToDecimal(reader["balance"]),
                                    Currency = reader["currency"].ToString(),
                                    Status = statusName,
                                    StatusCode = reader["status"].ToString(),
                                    OpeningDate = Convert.ToDateTime(reader["opening_date"]),
                                    IsDeleted = Convert.ToBoolean(reader["is_deleted"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetClientAccounts error: {ex.Message}");
            }
            return accounts;
        }

        // =============================================
        // СОЗДАНИЕ КЛИЕНТА С ДВУМЯ СЧЕТАМИ
        // =============================================
        public async Task<(int ClientId, string AccountNumber1, string AccountNumber2)> CreateClientWithTwoAccountsAsync(Client client, int userId)
        {
            int clientId = 0;
            string accountNumber1 = null;
            string accountNumber2 = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertClientQuery = @"
                            INSERT INTO Clients (client_type, full_name, passport_inn, phone, email, address, created_by, is_deleted)
                            VALUES (@client_type, @full_name, @passport_inn, @phone, @email, @address, @created_by, 0);
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertClientQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@client_type", client.ClientType);
                            cmd.Parameters.AddWithValue("@full_name", client.FullName);
                            cmd.Parameters.AddWithValue("@passport_inn", client.PassportInn);
                            cmd.Parameters.AddWithValue("@phone", client.Phone);
                            cmd.Parameters.AddWithValue("@email", client.Email ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@address", client.Address ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@created_by", userId);

                            clientId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }

                        if (clientId == 0)
                        {
                            transaction.Rollback();
                            return (0, null, null);
                        }

                        Random random = new Random();
                        int random1 = random.Next(10000000, 99999999);
                        int random2 = random.Next(10000000, 99999999);

                        if (client.ClientType == "individual")
                        {
                            accountNumber1 = $"40817.810.2.{random1}";
                            accountNumber2 = $"42301.810.3.{random2}";
                        }
                        else
                        {
                            accountNumber1 = $"40702.810.2.{random1}";
                            accountNumber2 = $"40702.810.3.{random2}";
                        }

                        string insertAccount1Query = @"
                            INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by, is_deleted)
                            VALUES (@account_number, 'current', 'Основной счет', @client_id, 0, 'RUB', 'active', @created_by, 0)";

                        using (var cmd = new SqlCommand(insertAccount1Query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@account_number", accountNumber1);
                            cmd.Parameters.AddWithValue("@client_id", clientId);
                            cmd.Parameters.AddWithValue("@created_by", userId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string insertAccount2Query = @"
                            INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by, is_deleted)
                            VALUES (@account_number, 'saving', 'Сберегательный счет', @client_id, 0, 'RUB', 'active', @created_by, 0)";

                        using (var cmd = new SqlCommand(insertAccount2Query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@account_number", accountNumber2);
                            cmd.Parameters.AddWithValue("@client_id", clientId);
                            cmd.Parameters.AddWithValue("@created_by", userId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return (clientId, accountNumber1, accountNumber2);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"CreateClientWithTwoAccounts error: {ex.Message}");
                        return (0, null, null);
                    }
                }
            }
        }

        // =============================================
        // СОЗДАНИЕ КЛИЕНТА С ОДНИМ СЧЕТОМ
        // =============================================
        public async Task<(int ClientId, string AccountNumber)> CreateClientWithAccountAsync(Client client, int userId)
        {
            int clientId = 0;
            string accountNumber = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertClientQuery = @"
                            INSERT INTO Clients (client_type, full_name, passport_inn, phone, email, address, created_by, is_deleted)
                            VALUES (@client_type, @full_name, @passport_inn, @phone, @email, @address, @created_by, 0);
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertClientQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@client_type", client.ClientType);
                            cmd.Parameters.AddWithValue("@full_name", client.FullName);
                            cmd.Parameters.AddWithValue("@passport_inn", client.PassportInn);
                            cmd.Parameters.AddWithValue("@phone", client.Phone);
                            cmd.Parameters.AddWithValue("@email", client.Email ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@address", client.Address ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@created_by", userId);

                            clientId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        }

                        if (clientId == 0)
                        {
                            transaction.Rollback();
                            return (0, null);
                        }

                        Random random = new Random();
                        int randomNum = random.Next(10000000, 99999999);
                        accountNumber = $"40817.810.2.{randomNum}";

                        string insertAccountQuery = @"
                            INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by, is_deleted)
                            VALUES (@account_number, 'current', 'Основной счет', @client_id, 0, 'RUB', 'active', @created_by, 0)";

                        using (var cmd = new SqlCommand(insertAccountQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@account_number", accountNumber);
                            cmd.Parameters.AddWithValue("@client_id", clientId);
                            cmd.Parameters.AddWithValue("@created_by", userId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return (clientId, accountNumber);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"CreateClientWithAccount error: {ex.Message}");
                        return (0, null);
                    }
                }
            }
        }

        // =============================================
        // ОТКРЫТИЕ СЧЕТА
        // =============================================
        public async Task<bool> OpenAccountAsync(int clientId, string accountType, int userId)
        {
            try
            {
                var accountNumber = GenerateAccountNumber();
                string accountName = accountType == "current" ? "Основной счет" :
                                    (accountType == "saving" ? "Сберегательный счет" : "Карточный счет");

                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by, is_deleted)
                        VALUES (@accountNumber, @accountType, @accountName, @clientId, 0, 'RUB', 'active', @userId, 0)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@accountNumber", accountNumber);
                        command.Parameters.AddWithValue("@accountType", accountType);
                        command.Parameters.AddWithValue("@accountName", accountName);
                        command.Parameters.AddWithValue("@clientId", clientId);
                        command.Parameters.AddWithValue("@userId", userId);

                        await connection.OpenAsync();
                        return await command.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenAccount error: {ex.Message}");
                return false;
            }
        }

        private string GenerateAccountNumber()
        {
            var random = new Random();
            return $"40817.810.2.{random.Next(10000000, 99999999)}";
        }

        // =============================================
        // ПЕРЕВОД СРЕДСТВ
        // =============================================
        public async Task<bool> TransferMoneyAsync(string fromAccount, string toAccount, decimal amount, string description, int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("sp_TransferMoney", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@from_account", fromAccount);
                    command.Parameters.AddWithValue("@to_account", toAccount);
                    command.Parameters.AddWithValue("@amount", amount);
                    command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@user_id", userId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        return reader["status"].ToString() == "SUCCESS";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Transfer error: {ex.Message}");
                return false;
            }
        }

        // =============================================
        // ПОЛУЧЕНИЕ ТРАНЗАКЦИЙ
        // =============================================
        public async Task<List<Transaction>> GetAccountTransactionsAsync(int accountId, int days = 30)
        {
            var transactions = new List<Transaction>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT TOP 50 
                            t.transaction_date,
                            t.type,
                            t.amount,
                            t.description,
                            t.status,
                            from_acc.account_number AS from_account,
                            to_acc.account_number AS to_account
                        FROM Transactions t
                        LEFT JOIN Accounts from_acc ON t.from_account = from_acc.id_account
                        LEFT JOIN Accounts to_acc ON t.to_account = to_acc.id_account
                        WHERE (t.from_account = @accountId OR t.to_account = @accountId)
                            AND t.transaction_date >= DATEADD(day, -@days, GETDATE())
                            AND t.is_deleted = 0
                        ORDER BY t.transaction_date DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@accountId", accountId);
                        command.Parameters.AddWithValue("@days", days);

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string type = reader["type"].ToString();
                                string typeName = type == "transfer" ? "Перевод" : (type == "deposit" ? "Пополнение" : "Снятие");
                                string statusName = reader["status"].ToString() == "completed" ? "Выполнен" : "В обработке";

                                transactions.Add(new Transaction
                                {
                                    Date = Convert.ToDateTime(reader["transaction_date"]),
                                    Type = typeName,
                                    Amount = Convert.ToDecimal(reader["amount"]),
                                    Description = reader["description"]?.ToString(),
                                    Status = statusName,
                                    FromAccount = reader["from_account"]?.ToString(),
                                    ToAccount = reader["to_account"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTransactions error: {ex.Message}");
            }
            return transactions;
        }

        // =============================================
        // СТАТИСТИКА КЛИЕНТА
        // =============================================
        public async Task<ClientStats> GetClientStatsAsync(int clientId)
        {
            var stats = new ClientStats();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT 
                            COUNT(*) as AccountCount,
                            ISNULL(SUM(balance), 0) as TotalBalance,
                            ISNULL(MAX(balance), 0) as MaxBalance,
                            ISNULL(MIN(balance), 0) as MinBalance
                        FROM Accounts 
                        WHERE id_client = @clientId AND status = 'active' AND is_deleted = 0";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);

                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                stats.AccountCount = Convert.ToInt32(reader["AccountCount"]);
                                stats.TotalBalance = Convert.ToDecimal(reader["TotalBalance"]);
                                stats.MaxBalance = Convert.ToDecimal(reader["MaxBalance"]);
                                stats.MinBalance = Convert.ToDecimal(reader["MinBalance"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetClientStats error: {ex.Message}");
            }
            return stats;
        }

        // =============================================
        // ПОЛНОЕ УДАЛЕНИЕ КЛИЕНТА
        // =============================================
        public async Task<bool> DeleteClientAsync(int clientId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string deleteTransactionsQuery = @"
                                DELETE FROM Transactions 
                                WHERE from_account IN (SELECT id_account FROM Accounts WHERE id_client = @clientId)
                                OR to_account IN (SELECT id_account FROM Accounts WHERE id_client = @clientId)";

                            using (var cmd = new SqlCommand(deleteTransactionsQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            string deleteAccountsQuery = "DELETE FROM Accounts WHERE id_client = @clientId";
                            using (var cmd = new SqlCommand(deleteAccountsQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            string deleteClientQuery = "DELETE FROM Clients WHERE id_client = @clientId";
                            using (var cmd = new SqlCommand(deleteClientQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@clientId", clientId);
                                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                                if (rowsAffected == 0)
                                {
                                    transaction.Rollback();
                                    return false;
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteClient error: {ex.Message}");
                return false;
            }
        }

        // =============================================
        // ПОЛУЧЕНИЕ СПИСКА РОЛЕЙ
        // =============================================
        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT id_role, name, description FROM Roles WHERE is_deleted = 0 ORDER BY id_role";
                    using (var command = new SqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                roles.Add(new Role
                                {
                                    Id = Convert.ToInt32(reader["id_role"]),
                                    Name = reader["name"].ToString(),
                                    Description = reader["description"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRolesAsync error: {ex.Message}");
            }
            return roles;





        }
    }
}