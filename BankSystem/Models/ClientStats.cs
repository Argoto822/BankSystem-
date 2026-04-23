namespace BankSystem.Models
{
    public class ClientStats
    {
        public int AccountCount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal MaxBalance { get; set; }
        public decimal MinBalance { get; set; }
    }
}