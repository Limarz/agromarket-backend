using AgroMarket.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Backend.Services
{
    public class AuthService
    {
        private readonly AgroMarketDbContext _context;

        public AuthService(AgroMarketDbContext context)
        {
            _context = context;
        }

        public async Task<int?> GetUserIdAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.Id;
        }
    }
}