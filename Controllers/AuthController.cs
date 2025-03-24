using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;

        public AuthController(AgroMarketDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            Console.WriteLine($"Попытка регистрации: Username={request.Username}, Email={request.Email}");

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                Console.WriteLine("Ошибка: Все поля должны быть заполнены.");
                return BadRequest("Все поля (Username, Email, Password) должны быть заполнены.");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
            {
                Console.WriteLine("Ошибка: Пользователь уже существует.");
                return BadRequest("Пользователь с таким именем или email уже существует.");
            }

            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole == null)
            {
                customerRole = new Role { Name = "Customer" };
                _context.Roles.Add(customerRole);
                await _context.SaveChangesAsync();
                Console.WriteLine("Роль Customer создана.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = customerRole.Id,
                IsPendingApproval = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = "Registered",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Регистрация успешна: UserId={user.Id}");
            return Ok(new { message = "Регистрация прошла успешно!", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            Console.WriteLine($"Попытка входа: Username={request.Username}");

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                Console.WriteLine("Ошибка: Имя пользователя и пароль должны быть заполнены.");
                return BadRequest("Имя пользователя и пароль должны быть заполнены.");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                Console.WriteLine("Пользователь не найден.");
                return BadRequest("Неверное имя пользователя или пароль.");
            }

            Console.WriteLine($"Пользователь найден: Username={user.Username}, PasswordHash={user.PasswordHash}");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                Console.WriteLine("Пароль не совпадает.");
                return BadRequest("Неверное имя пользователя или пароль.");
            }

            if (user.IsPendingApproval)
            {
                Console.WriteLine("Аккаунт ожидает одобрения.");
                return Unauthorized("Ваш аккаунт ожидает одобрения администратора.");
            }

            if (user.IsBlocked)
            {
                Console.WriteLine("Аккаунт заблокирован.");
                return Unauthorized("Ваш аккаунт заблокирован.");
            }

            // Проверяем, что Username не null перед установкой в сессию
            if (string.IsNullOrEmpty(user.Username))
            {
                Console.WriteLine("Ошибка: Имя пользователя пустое.");
                return BadRequest("Имя пользователя не может быть пустым.");
            }

            Console.WriteLine("Пароль верный, создаём сессию.");

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role?.Name ?? "Customer");

            // Логируем SessionId и проверяем, что сессия установлена
            Console.WriteLine($"Сессия создана: UserId={user.Id}, Username={user.Username}, SessionId={HttpContext.Session.Id}");
            var sessionUsername = HttpContext.Session.GetString("Username");
            Console.WriteLine($"Проверка сессии после установки: Username={sessionUsername}");

            // Добавляем заголовок для отладки
            HttpContext.Response.Headers.Add("Set-Cookie-Debug", $"AgroMarket.Session={HttpContext.Session.Id}; path=/; samesite=none; secure; httponly");

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = "Logged in",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Вход успешен!", userId = user.Id, username = user.Username, role = user.Role?.Name });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Console.WriteLine("Попытка выхода из системы.");
            HttpContext.Session.Clear();
            Console.WriteLine("Сессия очищена.");
            return Ok(new { message = "Выход выполнен успешно!" });
        }

        [HttpGet("check-session")]
        public IActionResult CheckSession()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("Сессия не найдена.");
                return Unauthorized("Сессия не активна");
            }
            Console.WriteLine($"Сессия активна: Username={username}");
            return Ok(new { message = "Сессия активна", username });
        }
    }

    public class RegisterRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class LoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}