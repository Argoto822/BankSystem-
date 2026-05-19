using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using BankSystem.Models;

namespace BankSystem.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = @"Server=DESKTOP-94H8IDC\SQLEXPRESS;Database=BankSystem;Trusted_Connection=True;";

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

        // =============================================
        // КЛИЕНТЫ - ПОЛУЧЕНИЕ
        // =============================================
        public async Task<List<Client>> GetClientsAsync(string search = null, bool showActiveOnly = true, bool includeDeleted = false)
        {
            var clients = new List<Client>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT id_client, client_type, full_name, passport_inn, phone, email, address, registration_date, is_active 
                        FROM Clients";

                    var conditions = new List<string>();
                    if (showActiveOnly) conditions.Add("is_active = 1");
                    if (conditions.Count > 0) query += " WHERE " + string.Join(" AND ", conditions);
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
                                var client = new Client
                                {
                                    Id = Convert.ToInt32(reader["id_client"]),
                                    ClientType = reader["client_type"]?.ToString() ?? "individual",
                                    FullName = reader["full_name"]?.ToString() ?? $"Клиент #{reader["id_client"]}",
                                    PassportInn = reader["passport_inn"]?.ToString() ?? "Не указан",
                                    Phone = reader["phone"]?.ToString() ?? "Не указан",
                                    Email = reader["email"]?.ToString(),
                                    Address = reader["address"]?.ToString(),
                                    RegistrationDate = reader["registration_date"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["registration_date"]),
                                    IsActive = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"])
                                };
                                clients.Add(client);
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

        public async Task<List<Client>> GetDeletedClientsAsync(string search = null)
        {
            var clients = new List<Client>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT id_client, client_type, full_name, passport_inn, phone, email, address, registration_date, is_active 
                        FROM Clients WHERE is_active = 0";

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
                                var client = new Client
                                {
                                    Id = Convert.ToInt32(reader["id_client"]),
                                    ClientType = reader["client_type"]?.ToString() ?? "individual",
                                    FullName = reader["full_name"]?.ToString() ?? $"Клиент #{reader["id_client"]}",
                                    PassportInn = reader["passport_inn"]?.ToString() ?? "Не указан",
                                    Phone = reader["phone"]?.ToString() ?? "Не указан",
                                    Email = reader["email"]?.ToString(),
                                    Address = reader["address"]?.ToString(),
                                    RegistrationDate = reader["registration_date"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["registration_date"]),
                                    IsActive = false
                                };
                                clients.Add(client);
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
        // КЛИЕНТЫ - СОЗДАНИЕ
        // =============================================
        public async Task<(int ClientId, string AccountNumber1, string AccountNumber2)> CreateClientWithTwoAccountsAsync(Client client, int userId)
        {
            int clientId = 0;
            string accountNumber1 = null, accountNumber2 = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var insertCmd = new SqlCommand(@"
                            INSERT INTO Clients (client_type, full_name, passport_inn, phone, email, address, created_by, is_active)
                            VALUES (@client_type, @full_name, @passport_inn, @phone, @email, @address, @created_by, 1);
                            SELECT SCOPE_IDENTITY();", connection, transaction);
                        insertCmd.Parameters.AddWithValue("@client_type", client.ClientType);
                        insertCmd.Parameters.AddWithValue("@full_name", client.FullName);
                        insertCmd.Parameters.AddWithValue("@passport_inn", client.PassportInn);
                        insertCmd.Parameters.AddWithValue("@phone", client.Phone);
                        insertCmd.Parameters.AddWithValue("@email", client.Email ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@address", client.Address ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@created_by", userId);
                        clientId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                        if (clientId == 0)
                        {
                            transaction.Rollback();
                            return (0, null, null);
                        }

                        var random = new Random();
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

                        var acc1Cmd = new SqlCommand(@"
                            INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by)
                            VALUES (@account_number, 'current', 'Основной счет', @client_id, 0, 'RUB', 'active', @created_by)", connection, transaction);
                        acc1Cmd.Parameters.AddWithValue("@account_number", accountNumber1);
                        acc1Cmd.Parameters.AddWithValue("@client_id", clientId);
                        acc1Cmd.Parameters.AddWithValue("@created_by", userId);
                        await acc1Cmd.ExecuteNonQueryAsync();

                        var acc2Cmd = new SqlCommand(@"
                            INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by)
                            VALUES (@account_number, 'saving', 'Сберегательный счет', @client_id, 0, 'RUB', 'active', @created_by)", connection, transaction);
                        acc2Cmd.Parameters.AddWithValue("@account_number", accountNumber2);
                        acc2Cmd.Parameters.AddWithValue("@client_id", clientId);
                        acc2Cmd.Parameters.AddWithValue("@created_by", userId);
                        await acc2Cmd.ExecuteNonQueryAsync();

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
        // КРЕДИТЫ - СОХРАНЕНИЕ
        // =============================================
        public async Task<bool> SaveCreditAsync(CreditAccount credit)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string createTableQuery = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Credits' AND xtype='U')
                        CREATE TABLE Credits (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            ClientId INT NOT NULL,
                            ClientFullName NVARCHAR(200) NOT NULL,
                            AccountNumber NVARCHAR(50) NOT NULL,
                            Amount DECIMAL(18,2) NOT NULL,
                            InterestRate DECIMAL(18,2) NOT NULL,
                            TermMonths INT NOT NULL,
                            MonthlyPayment DECIMAL(18,2) NOT NULL,
                            RemainingDebt DECIMAL(18,2) NOT NULL,
                            IssueDate DATETIME NOT NULL,
                            NextPaymentDate DATETIME NOT NULL,
                            Status NVARCHAR(20) NOT NULL,
                            PaymentType NVARCHAR(50) NOT NULL,
                            TotalPaid DECIMAL(18,2) NOT NULL,
                            TotalInterestPaid DECIMAL(18,2) NOT NULL,
                            Purpose NVARCHAR(500) NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )";

                    using (var createCmd = new SqlCommand(createTableQuery, connection))
                    {
                        await createCmd.ExecuteNonQueryAsync();
                    }

                    string query = @"
                        INSERT INTO Credits (ClientId, ClientFullName, AccountNumber, Amount, InterestRate, 
                             TermMonths, MonthlyPayment, RemainingDebt, IssueDate, NextPaymentDate, Status, 
                             PaymentType, TotalPaid, TotalInterestPaid, Purpose, CreatedAt) 
                        VALUES (@ClientId, @ClientFullName, @AccountNumber, @Amount, @InterestRate, 
                             @TermMonths, @MonthlyPayment, @RemainingDebt, @IssueDate, @NextPaymentDate, @Status, 
                             @PaymentType, @TotalPaid, @TotalInterestPaid, @Purpose, @CreatedAt)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", credit.ClientId);
                        command.Parameters.AddWithValue("@ClientFullName", credit.ClientFullName ?? "");
                        command.Parameters.AddWithValue("@AccountNumber", credit.AccountNumber);
                        command.Parameters.AddWithValue("@Amount", credit.Amount);
                        command.Parameters.AddWithValue("@InterestRate", credit.InterestRate);
                        command.Parameters.AddWithValue("@TermMonths", credit.TermMonths);
                        command.Parameters.AddWithValue("@MonthlyPayment", credit.MonthlyPayment);
                        command.Parameters.AddWithValue("@RemainingDebt", credit.RemainingDebt);
                        command.Parameters.AddWithValue("@IssueDate", credit.IssueDate);
                        command.Parameters.AddWithValue("@NextPaymentDate", credit.NextPaymentDate);
                        command.Parameters.AddWithValue("@Status", credit.Status ?? "Active");
                        command.Parameters.AddWithValue("@PaymentType", credit.PaymentType ?? "Аннуитетный");
                        command.Parameters.AddWithValue("@TotalPaid", credit.TotalPaid);
                        command.Parameters.AddWithValue("@TotalInterestPaid", credit.TotalInterestPaid);
                        command.Parameters.AddWithValue("@Purpose", (object)credit.Purpose ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения кредита: {ex.Message}");
                return false;
            }
        }

        // =============================================
        // КЛИЕНТЫ - УДАЛЕНИЕ И ВОССТАНОВЛЕНИЕ
        // =============================================
        public async Task<bool> SoftDeleteClientAsync(int clientId, int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Clients WHERE id_client = @clientId AND is_active = 1", connection);
                    checkCmd.Parameters.AddWithValue("@clientId", clientId);
                    int exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0) return false;

                    var updateClientCmd = new SqlCommand(@"
                        UPDATE Clients SET is_active = 0, updated_at = GETDATE() WHERE id_client = @clientId", connection);
                    updateClientCmd.Parameters.AddWithValue("@clientId", clientId);
                    int rowsAffected = await updateClientCmd.ExecuteNonQueryAsync();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoftDeleteClient error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreClientAsync(int clientId, int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Clients WHERE id_client = @clientId AND is_active = 0", connection);
                    checkCmd.Parameters.AddWithValue("@clientId", clientId);
                    int exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0) return false;

                    var updateClientCmd = new SqlCommand(@"
                        UPDATE Clients SET is_active = 1, updated_at = GETDATE() WHERE id_client = @clientId", connection);
                    updateClientCmd.Parameters.AddWithValue("@clientId", clientId);
                    int rowsAffected = await updateClientCmd.ExecuteNonQueryAsync();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RestoreClient error: {ex.Message}");
                return false;
            }
        }

        // =============================================
        // СЧЕТА
        // =============================================
        public async Task<List<Account>> GetClientAccountsAsync(int clientId, bool includeDeleted = false)
        {
            var accounts = new List<Account>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT id_account, account_number, account_type, account_name, balance, currency, status, opening_date
                        FROM Accounts WHERE id_client = @clientId";
                    query += " AND status != 'closed' ORDER BY account_type DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);
                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string accountType = reader["account_type"].ToString();
                                var account = new Account
                                {
                                    Id = Convert.ToInt32(reader["id_account"]),
                                    AccountNumber = reader["account_number"].ToString(),
                                    AccountType = accountType,
                                    AccountName = reader["account_name"]?.ToString() ?? (accountType == "current" ? "Текущий" : "Сберегательный"),
                                    Balance = Convert.ToDecimal(reader["balance"]),
                                    Currency = reader["currency"].ToString(),
                                    Status = reader["status"].ToString() == "active" ? "Активен" : "Заблокирован",
                                    StatusCode = reader["status"].ToString(),
                                    OpeningDate = Convert.ToDateTime(reader["opening_date"])
                                };
                                accounts.Add(account);
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

        public async Task<List<Account>> GetAllClientAccountsWithBalanceAsync(int clientId)
        {
            return await GetClientAccountsAsync(clientId);
        }

        public async Task<bool> OpenAccountAsync(int clientId, string accountType, int userId)
        {
            try
            {
                var random = new Random();
                var accountNumber = $"40817.810.2.{random.Next(10000000, 99999999)}";
                string accountName = accountType == "current" ? "Основной счет" : "Сберегательный счет";

                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by)
                        VALUES (@accountNumber, @accountType, @accountName, @clientId, 0, 'RUB', 'active', @userId)";

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

        // =============================================
        // ТРАНЗАКЦИИ И ПЕРЕВОДЫ
        // =============================================
        public async Task<List<Transaction>> GetAccountTransactionsAsync(int accountId, int days = 30)
        {
            var transactions = new List<Transaction>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT TOP 50 t.transaction_date, t.type, t.amount, t.description, t.status,
                               from_acc.account_number AS from_account, to_acc.account_number AS to_account
                        FROM Transactions t
                        LEFT JOIN Accounts from_acc ON t.from_account = from_acc.id_account
                        LEFT JOIN Accounts to_acc ON t.to_account = to_acc.id_account
                        WHERE (t.from_account = @accountId OR t.to_account = @accountId)
                            AND t.transaction_date >= DATEADD(day, -@days, GETDATE())
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
                                var transaction = new Transaction
                                {
                                    Date = Convert.ToDateTime(reader["transaction_date"]),
                                    Type = reader["type"].ToString() == "transfer" ? "Перевод" : "Пополнение",
                                    Amount = Convert.ToDecimal(reader["amount"]),
                                    Description = reader["description"]?.ToString(),
                                    Status = reader["status"].ToString() == "completed" ? "Выполнен" : "В обработке",
                                    FromAccount = reader["from_account"]?.ToString(),
                                    ToAccount = reader["to_account"]?.ToString()
                                };
                                transactions.Add(transaction);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAccountTransactions error: {ex.Message}");
            }
            return transactions;
        }

        public async Task<bool> TransferMoneyAsync(string fromAccount, string toAccount, decimal amount, string description, int userId)
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
                            var checkCmd = new SqlCommand("SELECT balance FROM Accounts WHERE account_number = @accountNumber", connection, transaction);
                            checkCmd.Parameters.AddWithValue("@accountNumber", fromAccount);
                            decimal balance = (decimal)await checkCmd.ExecuteScalarAsync();
                            if (balance < amount) return false;

                            var debitCmd = new SqlCommand("UPDATE Accounts SET balance = balance - @amount WHERE account_number = @accountNumber", connection, transaction);
                            debitCmd.Parameters.AddWithValue("@amount", amount);
                            debitCmd.Parameters.AddWithValue("@accountNumber", fromAccount);
                            await debitCmd.ExecuteNonQueryAsync();

                            var creditCmd = new SqlCommand("UPDATE Accounts SET balance = balance + @amount WHERE account_number = @accountNumber", connection, transaction);
                            creditCmd.Parameters.AddWithValue("@amount", amount);
                            creditCmd.Parameters.AddWithValue("@accountNumber", toAccount);
                            await creditCmd.ExecuteNonQueryAsync();

                            var transCmd = new SqlCommand(@"
                                INSERT INTO Transactions (from_account, to_account, amount, description, type, status, created_by, transaction_date)
                                VALUES ((SELECT id_account FROM Accounts WHERE account_number = @fromAccount),
                                        (SELECT id_account FROM Accounts WHERE account_number = @toAccount),
                                        @amount, @description, 'transfer', 'completed', @userId, GETDATE())", connection, transaction);
                            transCmd.Parameters.AddWithValue("@fromAccount", fromAccount);
                            transCmd.Parameters.AddWithValue("@toAccount", toAccount);
                            transCmd.Parameters.AddWithValue("@amount", amount);
                            transCmd.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                            transCmd.Parameters.AddWithValue("@userId", userId);
                            await transCmd.ExecuteNonQueryAsync();

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Transfer error: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TransferMoney error: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, string Message, decimal NewBalance)> TransferMoneyWithDetailsAsync(
            string fromAccount, string toAccount, decimal amount, string description, int userId)
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
                            var checkBalanceCmd = new SqlCommand("SELECT balance FROM Accounts WHERE account_number = @accountNumber", connection, transaction);
                            checkBalanceCmd.Parameters.AddWithValue("@accountNumber", fromAccount);
                            decimal fromBalance = (decimal)await checkBalanceCmd.ExecuteScalarAsync();

                            if (fromBalance < amount)
                            {
                                return (false, $"Недостаточно средств. Доступно: {fromBalance:N2} ₽", fromBalance);
                            }

                            var debitCmd = new SqlCommand("UPDATE Accounts SET balance = balance - @amount WHERE account_number = @accountNumber", connection, transaction);
                            debitCmd.Parameters.AddWithValue("@amount", amount);
                            debitCmd.Parameters.AddWithValue("@accountNumber", fromAccount);
                            await debitCmd.ExecuteNonQueryAsync();

                            var creditCmd = new SqlCommand("UPDATE Accounts SET balance = balance + @amount WHERE account_number = @accountNumber", connection, transaction);
                            creditCmd.Parameters.AddWithValue("@amount", amount);
                            creditCmd.Parameters.AddWithValue("@accountNumber", toAccount);
                            await creditCmd.ExecuteNonQueryAsync();

                            decimal newBalance = fromBalance - amount;

                            var transCmd = new SqlCommand(@"
                                INSERT INTO Transactions (from_account, to_account, amount, description, type, status, created_by, transaction_date)
                                VALUES ((SELECT id_account FROM Accounts WHERE account_number = @fromAccount),
                                        (SELECT id_account FROM Accounts WHERE account_number = @toAccount),
                                        @amount, @description, 'transfer', 'completed', @userId, GETDATE())", connection, transaction);
                            transCmd.Parameters.AddWithValue("@fromAccount", fromAccount);
                            transCmd.Parameters.AddWithValue("@toAccount", toAccount);
                            transCmd.Parameters.AddWithValue("@amount", amount);
                            transCmd.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                            transCmd.Parameters.AddWithValue("@userId", userId);
                            await transCmd.ExecuteNonQueryAsync();

                            transaction.Commit();
                            return (true, $"Перевод выполнен успешно! Новый баланс: {newBalance:N2} ₽", newBalance);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return (false, $"Ошибка перевода: {ex.Message}", 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message, decimal NewBalance)> DepositToAccountAsync(
            string accountNumber, decimal amount, string description, int userId)
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
                            var checkAccountCmd = new SqlCommand(@"SELECT id_account, balance, status FROM Accounts WHERE account_number = @accountNumber", connection, transaction);
                            checkAccountCmd.Parameters.AddWithValue("@accountNumber", accountNumber);

                            using (var reader = await checkAccountCmd.ExecuteReaderAsync())
                            {
                                if (!await reader.ReadAsync())
                                {
                                    return (false, $"Счет {accountNumber} не найден", 0);
                                }

                                int accountId = reader.GetInt32(0);
                                decimal currentBalance = reader.GetDecimal(1);
                                string status = reader.GetString(2);
                                reader.Close();

                                if (status != "active")
                                {
                                    return (false, $"Счет {accountNumber} неактивен. Статус: {status}", currentBalance);
                                }

                                decimal newBalance = currentBalance + amount;

                                var depositCmd = new SqlCommand("UPDATE Accounts SET balance = balance + @amount WHERE account_number = @accountNumber", connection, transaction);
                                depositCmd.Parameters.AddWithValue("@amount", amount);
                                depositCmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                                int rowsAffected = await depositCmd.ExecuteNonQueryAsync();

                                if (rowsAffected == 0)
                                {
                                    return (false, "Ошибка при пополнении счета", currentBalance);
                                }

                                var transCmd = new SqlCommand(@"
                                    INSERT INTO Transactions (to_account, amount, description, type, status, created_by, transaction_date)
                                    VALUES (@accountId, @amount, @description, 'deposit', 'completed', @userId, GETDATE())", connection, transaction);
                                transCmd.Parameters.AddWithValue("@accountId", accountId);
                                transCmd.Parameters.AddWithValue("@amount", amount);
                                transCmd.Parameters.AddWithValue("@description", description ?? "Пополнение счета");
                                transCmd.Parameters.AddWithValue("@userId", userId);
                                await transCmd.ExecuteNonQueryAsync();

                                transaction.Commit();
                                return (true, $"Счет пополнен на {amount:N2} ₽! Новый баланс: {newBalance:N2} ₽", newBalance);
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return (false, $"Ошибка пополнения: {ex.Message}", 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", 0);
            }
        }

        // =============================================
        // ПОЛЬЗОВАТЕЛИ
        // =============================================
        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT u.id_user, u.login, u.email, u.is_active, u.created_at, u.last_login,
                               r.id_role, r.name as role_name
                        FROM Users u
                        LEFT JOIN Roles r ON u.id_role = r.id_role
                        ORDER BY u.id_user";

                    using (var command = new SqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var user = new User
                                {
                                    Id = Convert.ToInt32(reader["id_user"]),
                                    Login = reader["login"].ToString(),
                                    Email = reader["email"] == DBNull.Value ? null : reader["email"].ToString(),
                                    IsActive = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"]),
                                    CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"]),
                                    LastLogin = reader["last_login"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["last_login"]),
                                    RoleId = Convert.ToInt32(reader["id_role"]),
                                    RoleName = reader["role_name"].ToString()
                                };
                                users.Add(user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUsersAsync error: {ex.Message}");
            }
            return users;
        }

        public async Task<bool> CreateUserAsync(User user, int createdBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        INSERT INTO Users (login, password_hash, email, id_role, is_active, created_by, created_at)
                        VALUES (@login, @password_hash, @email, @role_id, 1, @created_by, GETDATE())";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", user.Login);
                        command.Parameters.AddWithValue("@password_hash", user.PasswordHash);
                        command.Parameters.AddWithValue("@email", string.IsNullOrEmpty(user.Email) ? (object)DBNull.Value : user.Email);
                        command.Parameters.AddWithValue("@role_id", user.RoleId);
                        command.Parameters.AddWithValue("@created_by", createdBy);

                        await connection.OpenAsync();
                        int result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateUserAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(int userId, string newPasswordHash)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "UPDATE Users SET password_hash = @password_hash WHERE id_user = @userId";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@password_hash", newPasswordHash);
                        command.Parameters.AddWithValue("@userId", userId);

                        await connection.OpenAsync();
                        int result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetUserPasswordAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "UPDATE Users SET is_active = @is_active WHERE id_user = @userId";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@is_active", isActive);
                        command.Parameters.AddWithValue("@userId", userId);

                        await connection.OpenAsync();
                        int result = await command.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetUserActiveStatusAsync error: {ex.Message}");
                return false;
            }
        }
        // Добавьте эти методы в конец класса DatabaseService (перед последней закрывающей скобкой)

        // =============================================
        // КРЕДИТНЫЙ СЧЕТ
        // =============================================
        public async Task<(bool Success, string AccountNumber, string Message)> CreateCreditAccountAsync(int clientId, decimal creditAmount, int userId)
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
                            // Генерируем номер кредитного счета
                            var random = new Random();
                            string accountNumber = $"45507.810.2.{random.Next(10000000, 99999999)}";
                            string accountName = "Кредитный счет";

                            // Проверяем, существует ли уже кредитный счет у клиента
                            var checkCmd = new SqlCommand(
                                "SELECT COUNT(*) FROM Accounts WHERE id_client = @clientId AND account_type = 'credit' AND status = 'active'",
                                connection, transaction);
                            checkCmd.Parameters.AddWithValue("@clientId", clientId);
                            int existingCount = (int)await checkCmd.ExecuteScalarAsync();

                            if (existingCount > 0)
                            {
                                return (false, null, "У клиента уже есть активный кредитный счет");
                            }

                            // Создаем кредитный счет с отрицательным балансом (сумма кредита)
                            var insertCmd = new SqlCommand(@"
                        INSERT INTO Accounts (account_number, account_type, account_name, id_client, balance, currency, status, created_by, opening_date)
                        VALUES (@accountNumber, 'credit', @accountName, @clientId, -@creditAmount, 'RUB', 'active', @userId, GETDATE());
                        SELECT SCOPE_IDENTITY();", connection, transaction);

                            insertCmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                            insertCmd.Parameters.AddWithValue("@accountName", accountName);
                            insertCmd.Parameters.AddWithValue("@clientId", clientId);
                            insertCmd.Parameters.AddWithValue("@creditAmount", creditAmount);
                            insertCmd.Parameters.AddWithValue("@userId", userId);

                            int accountId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                            if (accountId > 0)
                            {
                                // Записываем транзакцию выдачи кредита
                                var transCmd = new SqlCommand(@"
                            INSERT INTO Transactions (to_account, amount, description, type, status, created_by, transaction_date)
                            VALUES (@accountId, @amount, @description, 'credit_issue', 'completed', @userId, GETDATE())", connection, transaction);
                                transCmd.Parameters.AddWithValue("@accountId", accountId);
                                transCmd.Parameters.AddWithValue("@amount", creditAmount);
                                transCmd.Parameters.AddWithValue("@description", $"Выдача кредита на сумму {creditAmount:N2} ₽");
                                transCmd.Parameters.AddWithValue("@userId", userId);
                                await transCmd.ExecuteNonQueryAsync();

                                transaction.Commit();
                                return (true, accountNumber, $"Кредитный счет {accountNumber} успешно создан");
                            }
                            else
                            {
                                transaction.Rollback();
                                return (false, null, "Ошибка при создании кредитного счета");
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"CreateCreditAccount error: {ex.Message}");
                            return (false, null, $"Ошибка: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCreditAccountAsync error: {ex.Message}");
                return (false, null, $"Ошибка: {ex.Message}");
            }
        }

        // =============================================
        // ПОЛУЧЕНИЕ КРЕДИТНОГО СЧЕТА КЛИЕНТА
        // =============================================
        public async Task<Account> GetCreditAccountAsync(int clientId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                SELECT id_account, account_number, account_type, account_name, balance, currency, status, opening_date
                FROM Accounts 
                WHERE id_client = @clientId AND account_type = 'credit' AND status = 'active'";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);
                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Account
                                {
                                    Id = Convert.ToInt32(reader["id_account"]),
                                    AccountNumber = reader["account_number"].ToString(),
                                    AccountType = reader["account_type"].ToString(),
                                    AccountName = reader["account_name"].ToString(),
                                    Balance = Convert.ToDecimal(reader["balance"]),
                                    Currency = reader["currency"].ToString(),
                                    Status = reader["status"].ToString() == "active" ? "Активен" : "Заблокирован",
                                    StatusCode = reader["status"].ToString(),
                                    OpeningDate = Convert.ToDateTime(reader["opening_date"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCreditAccountAsync error: {ex.Message}");
                return null;
            }
            return null;
        }

        // =============================================
        // ВНЕСЕНИЕ ПЛАТЕЖА ПО КРЕДИТУ
        // =============================================
        public async Task<(bool Success, string Message, decimal NewBalance)> MakeCreditPaymentAsync(int creditAccountId, decimal paymentAmount, int userId)
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
                            // Получаем текущий баланс кредитного счета
                            var getBalanceCmd = new SqlCommand("SELECT balance FROM Accounts WHERE id_account = @accountId", connection, transaction);
                            getBalanceCmd.Parameters.AddWithValue("@accountId", creditAccountId);
                            decimal currentBalance = (decimal)await getBalanceCmd.ExecuteScalarAsync();

                            decimal newBalance = currentBalance + paymentAmount; // Прибавляем к отрицательному балансу

                            // Обновляем баланс кредитного счета
                            var updateCmd = new SqlCommand("UPDATE Accounts SET balance = @newBalance WHERE id_account = @accountId", connection, transaction);
                            updateCmd.Parameters.AddWithValue("@newBalance", newBalance);
                            updateCmd.Parameters.AddWithValue("@accountId", creditAccountId);
                            await updateCmd.ExecuteNonQueryAsync();

                            // Записываем транзакцию платежа
                            var transCmd = new SqlCommand(@"
                        INSERT INTO Transactions (to_account, amount, description, type, status, created_by, transaction_date)
                        VALUES (@accountId, @amount, @description, 'credit_payment', 'completed', @userId, GETDATE())", connection, transaction);
                            transCmd.Parameters.AddWithValue("@accountId", creditAccountId);
                            transCmd.Parameters.AddWithValue("@amount", paymentAmount);
                            transCmd.Parameters.AddWithValue("@description", $"Платеж по кредиту на сумму {paymentAmount:N2} ₽");
                            transCmd.Parameters.AddWithValue("@userId", userId);
                            await transCmd.ExecuteNonQueryAsync();

                            transaction.Commit();

                            string statusMessage = newBalance >= 0 ? "Кредит полностью погашен!" : $"Остаток задолженности: {Math.Abs(newBalance):N2} ₽";
                            return (true, statusMessage, newBalance);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return (false, $"Ошибка при внесении платежа: {ex.Message}", 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", 0);
            }
        }

        // =============================================
        // СТАТИСТИКА И РОЛИ
        // =============================================
        public async Task<ClientStats> GetClientStatsAsync(int clientId)
        {
            var stats = new ClientStats();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT COUNT(*) as AccountCount, ISNULL(SUM(balance), 0) as TotalBalance
                        FROM Accounts 
                        WHERE id_client = @clientId AND status = 'active'";

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

        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT id_role, name, description FROM Roles ORDER BY id_role";
                    using (var command = new SqlCommand(query, connection))
                    {
                        await connection.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var role = new Role
                                {
                                    Id = Convert.ToInt32(reader["id_role"]),
                                    Name = reader["name"].ToString(),
                                    Description = reader["description"]?.ToString()
                                };
                                roles.Add(role);
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