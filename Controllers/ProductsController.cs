using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(AgroMarketDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Метод для получения списка всех товаров
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            try
            {
                Console.WriteLine("Получен запрос на получение списка товаров");

                // Временно убираем проверку сессии для отладки
                // var username = HttpContext.Session.GetString("Username");
                // if (string.IsNullOrEmpty(username))
                // {
                //     Console.WriteLine("Пользователь не авторизован: Username не найден в сессии.");
                //     return Unauthorized(new { Message = "Пользователь не авторизован." });
                // }

                // Console.WriteLine($"Поиск пользователя: Username={username}");
                // var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                // if (user == null)
                // {
                //     Console.WriteLine("Пользователь не найден в базе данных.");
                //     return Unauthorized(new { Message = "Пользователь не найден." });
                // }

                Console.WriteLine("Загружаем продукты из базы данных...");
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        Stock = p.Stock,
                        Description = p.Description,
                        ImageUrl = p.ImageUrl,
                        Category = p.Category != null ? p.Category.Name : null
                    })
                    .ToListAsync();

                Console.WriteLine($"Успешно загружено {products.Count} продуктов");
                return Ok(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetProducts: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Details = ex.Message });
            }
        }

        // Метод для получения информации о конкретном товаре
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Пользователь не авторизован: Username не найден в сессии.");
                    return Unauthorized(new { Message = "Пользователь не авторизован." });
                }

                Console.WriteLine($"Поиск пользователя: Username={username}");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    Console.WriteLine("Пользователь не найден в базе данных.");
                    return Unauthorized(new { Message = "Пользователь не найден." });
                }

                var product = await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        Stock = p.Stock,
                        Description = p.Description,
                        ImageUrl = p.ImageUrl,
                        Category = p.Category != null ? p.Category.Name : null
                    })
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound();
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetProduct: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Details = ex.Message });
            }
        }

        // Метод для создания товара (для админ-панели)
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromForm] ProductCreateModel model)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Пользователь не авторизован: Username не найден в сессии.");
                    return Unauthorized(new { Message = "Пользователь не авторизован." });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || user.Role?.Name != "Admin")
                {
                    Console.WriteLine("Доступ запрещён: Пользователь не администратор.");
                    return Unauthorized(new { Message = "Доступ только для администраторов." });
                }

                var product = new Product
                {
                    Name = model.Name,
                    Price = model.Price,
                    Stock = model.Stock,
                    Description = model.Description,
                    CategoryId = model.CategoryId
                };

                // Обработка загрузки изображения
                if (model.Image != null)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(stream);
                    }

                    product.ImageUrl = $"/uploads/{fileName}";
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CreateProduct: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Details = ex.Message });
            }
        }

        // Метод для обновления товара (для админ-панели)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductCreateModel model)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Пользователь не авторизован: Username не найден в сессии.");
                    return Unauthorized(new { Message = "Пользователь не авторизован." });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || user.Role?.Name != "Admin")
                {
                    Console.WriteLine("Доступ запрещён: Пользователь не администратор.");
                    return Unauthorized(new { Message = "Доступ только для администраторов." });
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                product.Name = model.Name;
                product.Price = model.Price;
                product.Stock = model.Stock;
                product.Description = model.Description;
                product.CategoryId = model.CategoryId;

                // Обработка загрузки нового изображения
                if (model.Image != null)
                {
                    // Удаляем старое изображение, если оно есть
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, product.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(stream);
                    }

                    product.ImageUrl = $"/uploads/{fileName}";
                }

                await _context.SaveChangesAsync();
                return Ok(product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateProduct: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Details = ex.Message });
            }
        }

        // Метод для удаления товара (для админ-панели)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Пользователь не авторизован: Username не найден в сессии.");
                    return Unauthorized(new { Message = "Пользователь не авторизован." });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || user.Role?.Name != "Admin")
                {
                    Console.WriteLine("Доступ запрещён: Пользователь не администратор.");
                    return Unauthorized(new { Message = "Доступ только для администраторов." });
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                // Удаляем изображение, если оно есть
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Товар удалён." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в DeleteProduct: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Details = ex.Message });
            }
        }
    }

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

    public class ProductCreateModel
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
        public int? CategoryId { get; set; }
    }
}