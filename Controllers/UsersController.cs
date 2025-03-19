using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;

        public UsersController(AgroMarketDbContext context)
        {
            _context = context;
        }

        // Метод для получения профиля текущего пользователя
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                Role = user.Role?.Name
            });
        }

        // Метод для получения информации о пользователе по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            var requestedUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (requestedUser == null)
            {
                return NotFound();
            }

            return requestedUser;
        }

        // Метод для обновления данных пользователя
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateModel userUpdate)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { Message = "Пользователь не авторизован." });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден." });

            if (user.Id != id)
                return Unauthorized(new { Message = "Вы можете обновлять только свои данные." });

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Username = userUpdate.Username ?? existingUser.Username;
            existingUser.Email = userUpdate.Email ?? existingUser.Email;
            if (!string.IsNullOrEmpty(userUpdate.Password))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userUpdate.Password);
            }

            await _context.SaveChangesAsync();

            var activity = new UserActivity
            {
                UserId = user.Id,
                Action = "Обновление профиля",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(existingUser);
        }
    }

    public class UserUpdateModel
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}