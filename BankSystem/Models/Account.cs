using System;

namespace BankSystem.Models
{
    public class Account : BaseEntity
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public string AccountName { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Status { get; set; } = "active";
        public string StatusCode { get; set; }
        public DateTime OpeningDate { get; set; } = DateTime.Now;
        public DateTime? ClosingDate { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? OverdraftLimit { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsDeleted { get; set; }

        private string _displayName;

        public string DisplayName
        {
            get
            {
                // Если _displayName не установлен, формируем автоматически
                if (string.IsNullOrEmpty(_displayName))
                {
                    return $"{AccountName} - {AccountNumber} - {Balance:N2} ₽";
                }
                return _displayName;
            }
            set => _displayName = value;
        }

        // Альтернативный вариант с автоматическим вычислением
        public string FullDisplayName => $"{AccountName} - {AccountNumber} - {Balance:N2} ₽";
    }
}