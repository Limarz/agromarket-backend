using AgroMarket.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AgroMarket.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserActivityController : ControllerBase
    {
        private readonly AgroMarketDbContext _context;

        public UserActivityController(AgroMarketDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivities()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7); // Ограничение: 7 дней

            // Сначала выполняем запрос к базе данных, загружаем данные в память
            var activitiesFromDb = await _context.UserActivities
                .Where(ua => ua.UserId == user.Id && ua.Timestamp >= sevenDaysAgo) // Фильтрация по дате
                .Include(ua => ua.User)
                .OrderByDescending(ua => ua.Timestamp) // Сортировка: сначала новые
                .ToListAsync();

            // Теперь применяем Select в памяти, где оператор ?. поддерживается
            var activities = activitiesFromDb
                .Select(ua => new
                {
                    ua.Id,
                    ua.UserId,
                    Username = ua.User?.Username ?? "Неизвестный", // Теперь это работает, так как мы в памяти
                    ua.Action,
                    ua.Timestamp
                })
                .ToList();

            return Ok(activities);
        }
    }
}