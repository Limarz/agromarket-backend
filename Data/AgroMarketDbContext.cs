using AgroMarket.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Backend.Data
{
    public class AgroMarketDbContext : DbContext
    {
        public AgroMarketDbContext(DbContextOptions<AgroMarketDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<Session> Sessions { get; set; } // Должно быть добавлено

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(ci => ci.CartId);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<Session>()
                .HasKey(s => s.Id); // Устанавливаем Id как первичный ключ
        }
    }
}