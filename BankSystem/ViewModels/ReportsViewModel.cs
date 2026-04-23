using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private ObservableCollection<Client> _clients;
        private ObservableCollection<Account> _accounts;
        private ObservableCollection<Transaction> _transactions;
        private Client _selectedClient;
        private Account _selectedAccount;
        private decimal _totalIncome;
        private decimal _totalExpense;
        private bool _isLoading;

        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set => SetProperty(ref _clients, value);
        }

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set => SetProperty(ref _accounts, value);
        }

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                SetProperty(ref _selectedClient, value);
                if (value != null) LoadAccountsAsync(value.Id);
            }
        }

        public Account SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
        }

        public decimal TotalIncome
        {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
        }

        public decimal TotalExpense
        {
            get => _totalExpense;
            set => SetProperty(ref _totalExpense, value);
        }

        public decimal TotalTurnover => TotalIncome + TotalExpense;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public RelayCommand LoadClientsCommand { get; }
        public RelayCommand GenerateReportCommand { get; }

        public ReportsViewModel()
        {
            _db = App.Database;
            Clients = new ObservableCollection<Client>();
            Accounts = new ObservableCollection<Account>();
            Transactions = new ObservableCollection<Transaction>();

            LoadClientsCommand = new RelayCommand(async _ => await LoadClientsAsync());
            GenerateReportCommand = new RelayCommand(async _ => await GenerateReportAsync(), _ => SelectedAccount != null);

            LoadClientsAsync();
        }

        private async Task LoadClientsAsync()
        {
            IsLoading = true;
            var clients = await _db.GetClientsAsync();
            Clients.Clear();
            foreach (var client in clients) Clients.Add(client);
            IsLoading = false;
        }

        private async Task LoadAccountsAsync(int clientId)
        {
            IsLoading = true;
            var accounts = await _db.GetClientAccountsAsync(clientId);
            Accounts.Clear();
            foreach (var account in accounts) Accounts.Add(account);
            IsLoading = false;
        }

        private async Task GenerateReportAsync()
        {
            if (SelectedAccount == null) return;

            IsLoading = true;
            var transactions = await _db.GetAccountTransactionsAsync(SelectedAccount.Id);

            Transactions.Clear();
            TotalIncome = transactions.Where(t => t.Type == "Пополнение" ||
                (t.Type == "Перевод" && t.ToAccount == SelectedAccount.AccountNumber))
                .Sum(t => t.Amount);
            TotalExpense = transactions.Where(t => t.Type == "Снятие" ||
                (t.Type == "Перевод" && t.FromAccount == SelectedAccount.AccountNumber))
                .Sum(t => t.Amount);

            foreach (var transaction in transactions)
            {
                Transactions.Add(transaction);
            }

            IsLoading = false;
        }
    }
}