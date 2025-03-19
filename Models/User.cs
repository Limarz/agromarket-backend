namespace AgroMarket.Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; } // Изменено с Password на PasswordHash
        public bool IsBlocked { get; set; }
        public int? RoleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Добавляем поле
        public Role? Role { get; set; }
        public bool IsPendingApproval { get; set; }
        public List<Cart> Carts { get; set; } = new List<Cart>();
        public List<Order> Orders { get; set; } = new List<Order>();
        public List<UserActivity> Activities { get; set; } = new List<UserActivity>();
    }
}