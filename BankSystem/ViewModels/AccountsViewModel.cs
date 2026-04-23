using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BankSystem.Models;
using BankSystem.Services;

namespace BankSystem.ViewModels
{
    public class AccountsViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private ObservableCollection<Client> _clients;
        private ObservableCollection<Account> _accounts;
        private Client _selectedClient;
        private Account _selectedAccount;
        private int _accountCount;
        private decimal _totalBalance;
        private decimal _averageBalance;
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

        public int AccountCount
        {
            get => _accountCount;
            set => SetProperty(ref _accountCount, value);
        }

        public decimal TotalBalance
        {
            get => _totalBalance;
            set => SetProperty(ref _totalBalance, value);
        }

        public decimal AverageBalance
        {
            get => _averageBalance;
            set => SetProperty(ref _averageBalance, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public RelayCommand LoadClientsCommand { get; }
        public RelayCommand OpenAccountCommand { get; }

        public AccountsViewModel()
        {
            _db = App.Database;
            Clients = new ObservableCollection<Client>();
            Accounts = new ObservableCollection<Account>();

            LoadClientsCommand = new RelayCommand(async _ => await LoadClientsAsync());
            OpenAccountCommand = new RelayCommand(_ => OpenAccountDialog(), _ => SelectedClient != null);

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

            AccountCount = Accounts.Count;
            TotalBalance = Accounts.Sum(a => a.Balance);
            AverageBalance = AccountCount > 0 ? TotalBalance / AccountCount : 0;

            IsLoading = false;
        }

        private void OpenAccountDialog()
        {
            var dialog = new Views.OpenAccountDialog();
            dialog.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

            if (dialog.ShowDialog() == true && SelectedClient != null)
            {
                LoadAccountsAsync(SelectedClient.Id);
            }
        }
    }
}