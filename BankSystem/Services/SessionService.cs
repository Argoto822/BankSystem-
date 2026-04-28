using BankSystem.Models;

namespace BankSystem.Services
{
    public class SessionService
    {
        private User _currentUser;

        public User CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public bool IsAuthenticated => _currentUser != null;
        public bool IsAdmin => _currentUser != null && _currentUser.RoleName == "Администратор";
        public bool IsOperator => _currentUser != null && _currentUser.RoleName == "Оператор";

        public void Clear()
        {
            _currentUser = null;
        }
    }
}