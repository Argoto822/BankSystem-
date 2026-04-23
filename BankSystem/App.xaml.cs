using System.Windows;
using BankSystem.Services;

namespace BankSystem
{
    public partial class App : Application
    {
        public static DatabaseService Database { get; private set; }
        public static SessionService Session { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Database = new DatabaseService();
            Session = new SessionService();
        }
    }
}