using System;

namespace BankSystem.Models
{
    public class Client : BaseEntity
    {
        public string ClientType { get; set; }
        public string FullName { get; set; }
        public string PassportInn { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; }
        public new int? CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}