namespace AgroMarket.Backend.Models
{
    public class Session
    {
        public string? Id { get; set; }
        public string? Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}