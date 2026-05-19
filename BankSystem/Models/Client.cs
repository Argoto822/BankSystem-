using System;

namespace BankSystem.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string ClientType { get; set; } = "individual";
        public string FullName { get; set; }
        public string PassportInn { get; set; } // Паспорт/ИНН (из БД)
        public string Passport { get; set; } // Для совместимости с ClientSelectionDialog
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}