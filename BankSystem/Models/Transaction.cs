using System;

namespace BankSystem.Models
{
    public class Transaction : BaseEntity
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Type { get; set; }
        public int? FromAccountId { get; set; }
        public string FromAccount { get; set; }
        public int? ToAccountId { get; set; }
        public string ToAccount { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "completed";
        public decimal Commission { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; }
    }
}