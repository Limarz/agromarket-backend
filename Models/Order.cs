namespace AgroMarket.Backend.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string? DeliveryAddress { get; set; }
        public double? DeliveryLatitude { get; set; } // Nullable
        public double? DeliveryLongitude { get; set; } // Nullable
        public string? DeliveryTimeSlot { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}