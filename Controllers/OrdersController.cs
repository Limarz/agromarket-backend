using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;

        public OrdersController(AgroMarketDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return Unauthorized();

            return await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .ToListAsync();
        }

        [HttpGet("admin/orders")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderDate,
                        o.TotalAmount,
                        o.Status,
                        Username = o.User != null ? o.User.Username : "Неизвестный",
                        o.DeliveryAddress,
                        o.DeliveryTimeSlot,
                        o.DeliveryDate, // Добавляем дату доставки
                        o.DeliveryLatitude,
                        o.DeliveryLongitude,
                        Items = o.Items.Select(oi => new
                        {
                            oi.ProductId,
                            oi.Quantity,
                            oi.UnitPrice,
                            ProductName = oi.Product != null ? oi.Product.Name : "Неизвестный товар"
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetAllOrders: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { message = "Произошла ошибка на сервере.", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] OrderCreateRequest request)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return Unauthorized();

            if (string.IsNullOrEmpty(request.DeliveryAddress) || string.IsNullOrEmpty(request.DeliveryTimeSlot) || request.DeliveryDate == null)
            {
                return BadRequest("Адрес доставки, временной слот и дата доставки должны быть указаны.");
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null || !cart.Items.Any()) return BadRequest("Корзина пуста.");

            foreach (var item in cart.Items)
            {
                if (item.Product?.Stock < item.Quantity)
                {
                    return BadRequest($"Недостаточно товара {item.Product?.Name} на складе. Остаток: {item.Product?.Stock}.");
                }
            }

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                DeliveryAddress = request.DeliveryAddress,
                DeliveryTimeSlot = request.DeliveryTimeSlot,
                DeliveryDate = request.DeliveryDate, // Сохраняем дату доставки
                DeliveryLatitude = request.DeliveryLocation?.Latitude,
                DeliveryLongitude = request.DeliveryLocation?.Longitude,
                TotalAmount = cart.Items.Sum(ci => ci.Product?.Price * ci.Quantity ?? 0),
                Items = cart.Items.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product?.Price ?? 0
                }).ToList()
            };

            foreach (var item in cart.Items)
            {
                if (item.Product != null) item.Product.Stock -= item.Quantity;
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = "Создание заказа",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }

        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound();

            order.Status = "Confirmed";
            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = "Подтверждение заказа",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Заказ подтверждён!" });
        }

        [HttpPut("admin/orders/{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound("Заказ не найден.");
            }

            order.Status = dto.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Статус заказа обновлён." });
        }
    }

    public class UpdateOrderStatusDto
    {
        public string? Status { get; set; }
    }
}