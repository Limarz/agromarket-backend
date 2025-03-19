using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;

        public CartController(AgroMarketDbContext context)
        {
            _context = context;
        }

        // Метод для получения корзины пользователя
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart { UserId = user.Id };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return Ok(cart);
        }

        // Метод для добавления товара в корзину
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromQuery] int productId, [FromQuery] int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            if (quantity <= 0)
                return BadRequest(new { Message = "Количество должно быть больше 0." });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { Message = "Товар не найден." });

            if (product.Stock < quantity)
                return BadRequest(new { Message = "Недостаточно товара на складе." });

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart { UserId = user.Id };
                _context.Carts.Add(cart);
            }

            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                if (cartItem.Quantity > product.Stock)
                    return BadRequest(new { Message = "Недостаточно товара на складе." });
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                cart.Items.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = $"Добавление товара {productId} в корзину",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Товар добавлен в корзину." });
        }

        // Метод для обновления количества товара в корзине
        [HttpPost("update")]
        public async Task<IActionResult> UpdateCartItem([FromQuery] int productId, [FromQuery] int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
                return NotFound(new { Message = "Корзина не найдена." });

            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null)
                return NotFound(new { Message = "Товар в корзине не найден." });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { Message = "Товар не найден." });

            if (quantity <= 0)
            {
                cart.Items.Remove(cartItem);
            }
            else
            {
                if (quantity > product.Stock)
                    return BadRequest(new { Message = "Недостаточно товара на складе." });
                cartItem.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = $"Обновление количества товара {productId} в корзине",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Корзина обновлена." });
        }

        // Метод для удаления товара из корзины
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
                return NotFound(new { Message = "Корзина не найдена." });

            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null)
                return NotFound(new { Message = "Товар в корзине не найден." });

            cart.Items.Remove(cartItem);
            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = $"Удаление товара {productId} из корзины",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Товар удалён из корзины." });
        }

        // Метод для полной очистки корзины
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null || !cart.Items.Any())
                return NotFound(new { Message = "Корзина пуста." });

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = "Очистка корзины",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Корзина очищена." });
        }
    }
}