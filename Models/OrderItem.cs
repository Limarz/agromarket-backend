namespace AgroMarket.Backend.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; } // Сделали nullable
        public int ProductId { get; set; }
        public Product? Product { get; set; } // Сделали nullable
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}