namespace AgroMarket.Backend.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; } // Добавляем поле для URL изображения
        public int? CategoryId { get; set; } // Добавляем поле для категории
        public Category? Category { get; set; } // Связь с категорией
    }
}