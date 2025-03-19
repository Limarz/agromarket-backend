using AgroMarket.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Backend.Data
{
    public class AgroMarketDbContext : DbContext
    {
        public AgroMarketDbContext(DbContextOptions<AgroMarketDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; } // Добавляем категории
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().Property(u => u.PasswordHash).HasMaxLength(255);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");

            // Добавляем начальные данные для категорий
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Овощи" },
                new Category { Id = 2, Name = "Инвентарь" },
                new Category { Id = 3, Name = "Зерна" },
                new Category { Id = 4, Name = "Саженцы" }
            );
        }
    }
}