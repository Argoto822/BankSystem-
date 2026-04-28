using System;

namespace BankSystem.Models
{
    public class Account : BaseEntity
    {
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
        public new int? CreatedBy { get; set; }
        public bool IsDeleted { get; set; }

        private string _displayName;

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                {
                    return $"{AccountName} - {AccountNumber} - {Balance:N2} {Currency}";
                }
                return _displayName;
            }
            set => _displayName = value;
        }

        public string FullDisplayName => $"{AccountName} - {AccountNumber} - {Balance:N2} {Currency}";

        public string ShortDisplayName => $"{AccountName} - {Balance:N2} {Currency}";
    }
}