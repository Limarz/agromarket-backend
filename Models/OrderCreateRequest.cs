namespace AgroMarket.Backend.Models
{
    public class OrderCreateRequest
    {
        public string? DeliveryAddress { get; set; }
        public string? DeliveryTimeSlot { get; set; }
    }
}