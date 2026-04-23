using BankSystem.Models;
using BankSystem.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BankSystem.ViewModels
{
    public class ClientsViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private ObservableCollection<Client> _clients;
        private string _searchText;
        private bool _isLoading;

        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set => SetProperty(ref _clients, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public RelayCommand LoadClientsCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand AddClientCommand { get; }

        public ClientsViewModel()
        {
            _db = App.Database;
            Clients = new ObservableCollection<Client>();

            LoadClientsCommand = new RelayCommand(async _ => await LoadClientsAsync());
            SearchCommand = new RelayCommand(async _ => await SearchClientsAsync());
            AddClientCommand = new RelayCommand(_ => OpenAddClientDialog());

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

        private async Task SearchClientsAsync()
        {
            IsLoading = true;
            var clients = await _db.GetClientsAsync(SearchText);
            Clients.Clear();
            foreach (var client in clients) Clients.Add(client);
            IsLoading = false;
        }

        private void OpenAddClientDialog()
        {
            var dialog = new Views.ClientDialog();
            dialog.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

            if (dialog.ShowDialog() == true && dialog.Client != null)
            {
                LoadClientsAsync();
            }
        }
    }
}