using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Все поля (Username, Email, Password) должны быть заполнены.");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest("Пользователь с таким именем или email уже существует.");
            }

            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole == null)
            {
                customerRole = new Role { Name = "Customer" };
                _context.Roles.Add(customerRole);
                await _context.SaveChangesAsync();
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

            return Ok(new { message = "Регистрация прошла успешно!", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Имя пользователя и пароль должны быть заполнены.");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Неверное имя пользователя или пароль.");
            }

            if (user.IsPendingApproval)
            {
                return Unauthorized("Ваш аккаунт ожидает одобрения администратора.");
            }

            if (user.IsBlocked)
            {
                return Unauthorized("Ваш аккаунт заблокирован.");
            }

            // Проверяем, что Username не null перед установкой в сессию
            if (string.IsNullOrEmpty(user.Username))
            {
                return BadRequest("Имя пользователя не может быть пустым.");
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role?.Name ?? "Customer");

            Console.WriteLine($"Сессия создана: UserId={user.Id}, Username={user.Username}, SessionId={HttpContext.Session.Id}");

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
            HttpContext.Session.Clear();
            Console.WriteLine("Сессия очищена");
            return Ok(new { message = "Выход выполнен успешно!" });
        }

        [HttpGet("check-session")]
        public IActionResult CheckSession()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("Сессия не найдена");
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