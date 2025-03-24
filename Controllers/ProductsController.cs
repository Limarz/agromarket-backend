using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Amazon.S3; // Для работы с S3 API
using Amazon.S3.Model; // Для запросов к S3

namespace AgroMarket.Backend.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;
        private readonly IAmazonS3 _s3Client; // Клиент для Yandex Cloud (S3-совместимый)
        private readonly string _bucketName;

        public ProductsController(AgroMarketDbContext context, IConfiguration configuration)
        {
            _context = context;

            // Настройка клиента для Yandex Cloud
            var accessKey = configuration["YandexCloud:AccessKey"];
            var secretKey = configuration["YandexCloud:SecretKey"];
            _bucketName = configuration["YandexCloud:BucketName"];
            var endpoint = configuration["YandexCloud:Endpoint"];

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(_bucketName) || string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentException("Yandex Cloud configuration is missing.");
            }

            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        // Метод для получения списка всех товаров
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            try
            {
                Console.WriteLine("Получен запрос на получение списка товаров");

                var username = HttpContext.Session.GetString("Username");
                Console.WriteLine($"Username из сессии: {username}");
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

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name ?? "Без названия",
                        Price = p.Price,
                        Stock = p.Stock,
                        Description = p.Description ?? "Без описания",
                        ImageUrl = p.ImageUrl, // Теперь это URL от Yandex Cloud
                        Category = p.Category != null ? p.Category.Name : "Без категории"
                    })
                    .ToListAsync();

                Console.WriteLine($"Успешно загружено {products.Count} продуктов");
                foreach (var product in products)
                {
                    Console.WriteLine($"Продукт: Id={product.Id}, Name={product.Name}, Price={product.Price}, Category={product.Category}");
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetProducts: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
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
                        Name = p.Name ?? "Без названия",
                        Price = p.Price,
                        Stock = p.Stock,
                        Description = p.Description ?? "Без описания",
                        ImageUrl = p.ImageUrl,
                        Category = p.Category != null ? p.Category.Name : "Без категории"
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

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);
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

                // Обработка загрузки изображения в Yandex Cloud
                if (model.Image != null)
                {
                    var fileName = $"products/{Guid.NewGuid()}_{model.Image.FileName}";
                    using var stream = model.Image.OpenReadStream();
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName,
                        InputStream = stream,
                        ContentType = model.Image.ContentType
                    };

                    var response = await _s3Client.PutObjectAsync(putRequest);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception("Ошибка загрузки изображения в Yandex Cloud.");
                    }

                    product.ImageUrl = $"https://storage.yandexcloud.net/{_bucketName}/{fileName}";
                    Console.WriteLine($"Изображение загружено в Yandex Cloud: {product.ImageUrl}");
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

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);
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

                // Обработка загрузки нового изображения в Yandex Cloud
                if (model.Image != null)
                {
                    // Удаляем старое изображение из Yandex Cloud, если оно есть
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldFileName = product.ImageUrl.Replace($"https://storage.yandexcloud.net/{_bucketName}/", "");
                        var deleteRequest = new DeleteObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = oldFileName
                        };
                        await _s3Client.DeleteObjectAsync(deleteRequest);
                        Console.WriteLine($"Старое изображение удалено из Yandex Cloud: {oldFileName}");
                    }

                    var fileName = $"products/{Guid.NewGuid()}_{model.Image.FileName}";
                    using var stream = model.Image.OpenReadStream();
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName,
                        InputStream = stream,
                        ContentType = model.Image.ContentType
                    };

                    var response = await _s3Client.PutObjectAsync(putRequest);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception("Ошибка загрузки изображения в Yandex Cloud.");
                    }

                    product.ImageUrl = $"https://storage.yandexcloud.net/{_bucketName}/{fileName}";
                    Console.WriteLine($"Новое изображение загружено в Yandex Cloud: {product.ImageUrl}");
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

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);
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

                // Удаляем изображение из Yandex Cloud, если оно есть
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var fileName = product.ImageUrl.Replace($"https://storage.yandexcloud.net/{_bucketName}/", "");
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName
                    };
                    await _s3Client.DeleteObjectAsync(deleteRequest);
                    Console.WriteLine($"Изображение удалено из Yandex Cloud: {fileName}");
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