using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;

        public AdminController(AgroMarketDbContext context)
        {
            _context = context;
        }

        // Метод для проверки, является ли пользователь администратором
        private async Task<bool> IsAdmin()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return false;

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            return user?.Role?.Name == "Admin";
        }

        // Метод для получения списка пользователей, ожидающих одобрения
        [HttpGet("pending-users")]
        public async Task<IActionResult> GetPendingUsers()
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            var pendingUsers = await _context.Users
                .Where(u => u.IsPendingApproval)
                .Include(u => u.Role)
                .ToListAsync();
            return Ok(pendingUsers);
        }

        // Метод для одобрения или отклонения пользователя
        [HttpPost("approve-user")]
        public async Task<IActionResult> ApproveUser([FromBody] ApproveRequestModel request)
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            if (request == null || request.UserId <= 0)
            {
                return BadRequest("Неверный запрос.");
            }

            var targetUser = await _context.Users.FindAsync(request.UserId);
            if (targetUser == null)
            {
                return NotFound("User not found");
            }

            targetUser.IsPendingApproval = !request.Approve;
            if (!request.Approve)
            {
                targetUser.IsBlocked = true;
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = request.Approve ? "User approved" : "User rejected" });
        }

        // Метод для получения списка всех пользователей
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
            return Ok(users);
        }

        // Метод для блокировки или разблокировки пользователя
        [HttpPut("block-user/{id}")]
        public async Task<IActionResult> BlockUser(int id, [FromBody] bool block)
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            var targetUser = await _context.Users.FindAsync(id);
            if (targetUser == null)
            {
                return NotFound("User not found");
            }

            targetUser.IsBlocked = block;
            await _context.SaveChangesAsync();
            return Ok(new { message = block ? "User blocked" : "User unblocked" });
        }

        // Метод для получения списка всех заказов
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .ToListAsync();
            return Ok(orders);
        }

        // Метод для обновления статуса заказа
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("Статус заказа не может быть пустым.");
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found");
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Order status updated" });
        }

        // Метод для получения аналитики
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            var totalUsers = await _context.Users.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");

            var topProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(5)
                .Join(_context.Products,
                    g => g.ProductId,
                    p => p.Id,
                    (g, p) => new
                    {
                        p.Name,
                        g.TotalSold
                    })
                .ToListAsync();

            return Ok(new
            {
                totalUsers,
                totalOrders,
                totalRevenue,
                pendingOrders,
                topProducts
            });
        }

        // Метод для создания нового товара
        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            if (product == null || string.IsNullOrEmpty(product.Name) || product.Price <= 0 || product.Stock < 0)
            {
                return BadRequest("Неверные данные продукта. Убедитесь, что указаны название, цена (> 0) и запас (>= 0).");
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // Метод для обновления существующего товара
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            if (updatedProduct == null || id != updatedProduct.Id || string.IsNullOrEmpty(updatedProduct.Name) || updatedProduct.Price <= 0 || updatedProduct.Stock < 0)
            {
                return BadRequest("Неверные данные продукта. Убедитесь, что указаны корректный ID, название, цена (> 0) и запас (>= 0).");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            product.Name = updatedProduct.Name;
            product.Price = updatedProduct.Price;
            product.Stock = updatedProduct.Stock;
            product.Description = updatedProduct.Description;

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // Метод для получения списка всех товаров
        [HttpGet("all-products")] // Изменили маршрут с "products" на "all-products"
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                Console.WriteLine("Получен запрос на получение всех продуктов (AdminController)");
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Пользователь не авторизован: Username не найден в сессии.");
                    return Unauthorized(new { Message = "Пользователь не авторизован." });
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null || user.Role?.Name != "Admin")
                {
                    Console.WriteLine("Доступ запрещён: Пользователь не администратор.");
                    return Unauthorized(new { Message = "Доступ только для администраторов." });
                }

                var products = await _context.Products
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name ?? "Без названия",
                        Price = p.Price,
                        Stock = p.Stock,
                        Description = p.Description ?? "Без описания",
                        ImageUrl = p.ImageUrl ?? "Без изображения",
                        Category = "Без категории"
                    })
                    .ToListAsync();

                Console.WriteLine($"Успешно загружено {products.Count} продуктов (AdminController)");
                return Ok(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetAllProducts (AdminController): {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Details = ex.Message });
            }
        }

        // Метод для удаления товара
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!await IsAdmin())
                return Unauthorized("Требуется авторизация администратора.");

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Product deleted" });
        }
    }

    public class ApproveRequestModel
    {
        public int UserId { get; set; }
        public bool Approve { get; set; }
    }

    // Добавляем DTO для продуктов
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
    }
}