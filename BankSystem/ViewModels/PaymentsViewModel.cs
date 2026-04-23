using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BankSystem.Models;
using BankSystem.Services;
using BankSystem.Validation;

namespace BankSystem.ViewModels
{
    public class PaymentsViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private ObservableCollection<Account> _accounts;
        private Account _selectedFromAccount;
        private Account _selectedToAccount;
        private string _amount;
        private string _description;
        private ObservableCollection<Transaction> _history;
        private string _error;
        private bool _isLoading;

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set => SetProperty(ref _accounts, value);
        }

        public ObservableCollection<Account> ToAccounts =>
            new ObservableCollection<Account>(Accounts?.Where(a => a != SelectedFromAccount) ?? Enumerable.Empty<Account>());

        public Account SelectedFromAccount
        {
            get => _selectedFromAccount;
            set
            {
                SetProperty(ref _selectedFromAccount, value);
                OnPropertyChanged(nameof(ToAccounts));
            }
        }

        public Account SelectedToAccount
        {
            get => _selectedToAccount;
            set => SetProperty(ref _selectedToAccount, value);
        }

        public string Amount
        {
            get => _amount;
            set
            {
                SetProperty(ref _amount, value);
                ValidateAmount();
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ObservableCollection<Transaction> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool CanTransfer => SelectedFromAccount != null &&
                                   SelectedToAccount != null &&
                                   string.IsNullOrEmpty(Error) &&
                                   decimal.TryParse(Amount, out var amount) && amount > 0;

        public RelayCommand LoadAccountsCommand { get; }
        public RelayCommand TransferCommand { get; }
        public RelayCommand ClearCommand { get; }

        public PaymentsViewModel()
        {
            _db = App.Database;
            Accounts = new ObservableCollection<Account>();
            History = new ObservableCollection<Transaction>();

            LoadAccountsCommand = new RelayCommand(async _ => await LoadAccountsAsync());
            TransferCommand = new RelayCommand(async _ => await TransferAsync(), _ => CanTransfer);
            ClearCommand = new RelayCommand(_ => ClearForm());

            LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
        {
            IsLoading = true;
            var clientId = 1; // TODO: Получить из сессии
            var accounts = await _db.GetClientAccountsAsync(clientId);
            Accounts.Clear();
            foreach (var account in accounts.Where(a => a.StatusCode == "active"))
            {
                Accounts.Add(account);
            }

            if (accounts.Any())
            {
                var history = await _db.GetAccountTransactionsAsync(accounts.First().Id);
                History.Clear();
                foreach (var transaction in history.Take(10))
                {
                    History.Add(transaction);
                }
            }
            IsLoading = false;
        }

        private void ValidateAmount()
        {
            if (string.IsNullOrWhiteSpace(Amount))
            {
                Error = "Введите сумму";
                return;
            }

            if (!decimal.TryParse(Amount, out var amount))
            {
                Error = "Введите корректное число";
                return;
            }

            if (amount <= 0)
            {
                Error = "Сумма должна быть больше 0";
                return;
            }

            if (SelectedFromAccount != null && amount > SelectedFromAccount.Balance)
            {
                Error = $"Недостаточно средств. Доступно: {SelectedFromAccount.Balance:N2} ₽";
                return;
            }

            Error = null;
        }

        private async Task TransferAsync()
        {
            if (!CanTransfer) return;

            var amount = decimal.Parse(Amount);
            IsLoading = true;

            var success = await _db.TransferMoneyAsync(
                SelectedFromAccount.AccountNumber,
                SelectedToAccount.AccountNumber,
                amount,
                Description,
                App.Session.CurrentUser?.Id ?? 1);

            if (success)
            {
                MessageBox.Show($"Перевод {amount:N2} ₽ выполнен успешно!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadAccountsAsync();
            }
            else
            {
                Error = "Ошибка при выполнении перевода";
            }

            IsLoading = false;
        }

        private void ClearForm()
        {
            SelectedFromAccount = null;
            SelectedToAccount = null;
            Amount = "0";
            Description = string.Empty;
            Error = null;
        }
    }
}