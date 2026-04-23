using BankSystem.Models;

namespace BankSystem.Services
{
    public class SessionService
    {
        public User CurrentUser { get; private set; }

        public void SetCurrentUser(User user) => CurrentUser = user;

        public bool IsAuthenticated => CurrentUser != null;

        public bool IsAdmin => CurrentUser?.RoleName == "Администратор";
        public bool IsOperator => CurrentUser?.RoleName == "Оператор";
        public bool IsCashier => CurrentUser?.RoleName == "Кассир";
        public bool IsAnalyst => CurrentUser?.RoleName == "Аналитик";

        public void Logout() => CurrentUser = null;
    }
}