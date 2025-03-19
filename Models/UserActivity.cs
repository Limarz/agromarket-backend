namespace AgroMarket.Backend.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; } // Добавим ? если допускаем null
        public string? Action { get; set; } // Добавим ? если допускаем null
        public DateTime Timestamp { get; set; } // Добавлено свойство
    }
}