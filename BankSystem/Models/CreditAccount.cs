using System;

namespace BankSystem.Models
{
    public class CreditAccount
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientFullName { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal RemainingDebt { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime NextPaymentDate { get; set; }
        public string Status { get; set; }
        public string PaymentType { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public string Purpose { get; set; }
    }

    public class PaymentSchedule
    {
        public int Id { get; set; }
        public int CreditId { get; set; }
        public int PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal RemainingDebt { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
    }
}