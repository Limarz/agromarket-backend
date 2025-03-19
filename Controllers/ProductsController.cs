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
    [Route("api/[controller]")]
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
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

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

            return Ok(products);
        }

        // Метод для получения информации о конкретном товаре
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

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

        // Метод для создания товара (для админ-панели)
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromForm] ProductCreateModel model)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.Role?.Name != "Admin")
                return Unauthorized(new { Message = "Доступ только для администраторов." });

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

        // Метод для обновления товара (для админ-панели)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductCreateModel model)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.Role?.Name != "Admin")
                return Unauthorized(new { Message = "Доступ только для администраторов." });

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

        // Метод для удаления товара (для админ-панели)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.Role?.Name != "Admin")
                return Unauthorized(new { Message = "Доступ только для администраторов." });

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
    }

    public class ProductCreateModel
    {
        public string? Name { get; set; } // Делаем свойство допускающим null
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; } // Делаем свойство допускающим null
        public IFormFile? Image { get; set; } // Делаем свойство допускающим null
        public int? CategoryId { get; set; }
    }
}