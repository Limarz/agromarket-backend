namespace AgroMarket.Backend.Models
{
    public class OrderCreateRequest
    {
        public string? DeliveryAddress { get; set; }
        public string? DeliveryTimeSlot { get; set; }
        public DateTime? DeliveryDate { get; set; } // Новое поле
        public Location? DeliveryLocation { get; set; } // Добавляем для координат
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}