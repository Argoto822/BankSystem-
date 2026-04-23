using System;

namespace BankSystem.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }

        // Свойство для форматированного отображения даты и времени
        public string FormattedTimestamp => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
    }
}