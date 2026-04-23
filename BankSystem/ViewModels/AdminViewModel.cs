using BankSystem.Models;
using BankSystem.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace BankSystem.ViewModels
{
    public class AdminViewModel : BaseViewModel
    {
        private ObservableCollection<User> _users;
        private ObservableCollection<Roles> _roles;
        private bool _isLoading;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public ObservableCollection<Roles> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public RelayCommand LoadUsersCommand { get; }
        public RelayCommand LoadRolesCommand { get; }

        public AdminViewModel()
        {
            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Roles>();

            LoadUsersCommand = new RelayCommand(async _ => await LoadUsersAsync());
            LoadRolesCommand = new RelayCommand(async _ => await LoadRolesAsync());

            LoadUsersAsync();
            LoadRolesAsync();
        }

        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            // TODO: Реализовать загрузку пользователей
            await Task.Delay(100);
            IsLoading = false;
        }

        private async Task LoadRolesAsync()
        {
            // TODO: Реализовать загрузку ролей
            await Task.Delay(100);
        }
    }
}