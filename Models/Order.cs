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
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
        public string? DeliveryTimeSlot { get; set; }
        public DateTime? DeliveryDate { get; set; } // Новое поле для даты доставки
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}