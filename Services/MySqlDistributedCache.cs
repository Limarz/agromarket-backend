using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgroMarket.Backend.Services
{
    public class MySqlDistributedCache : IDistributedCache
    {
        private readonly IServiceProvider _serviceProvider;

        public MySqlDistributedCache(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public byte[]? Get(string key)
        {
            return GetAsync(key).GetAwaiter().GetResult();
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgroMarketDbContext>();

            var session = await dbContext.Sessions
                .FirstOrDefaultAsync(s => s.Id == key, token);

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
            {
                if (session != null)
                {
                    dbContext.Sessions.Remove(session);
                    await dbContext.SaveChangesAsync(token);
                }
                return null;
            }

            return session.Data != null ? Convert.FromBase64String(session.Data) : null;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, options).GetAwaiter().GetResult();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgroMarketDbContext>();

            var session = await dbContext.Sessions
                .FirstOrDefaultAsync(s => s.Id == key, token);

            DateTime expiresAt = CalculateExpiration(options);

            if (session == null)
            {
                session = new Session
                {
                    Id = key,
                    Data = Convert.ToBase64String(value),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };
                dbContext.Sessions.Add(session);
            }
            else
            {
                session.Data = Convert.ToBase64String(value);
                session.ExpiresAt = expiresAt;
            }

            await dbContext.SaveChangesAsync(token);
        }

        public void Remove(string key)
        {
            RemoveAsync(key).GetAwaiter().GetResult();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgroMarketDbContext>();

            var session = await dbContext.Sessions
                .FirstOrDefaultAsync(s => s.Id == key, token);

            if (session != null)
            {
                dbContext.Sessions.Remove(session);
                await dbContext.SaveChangesAsync(token);
            }
        }

        public void Refresh(string key)
        {
            RefreshAsync(key).GetAwaiter().GetResult();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgroMarketDbContext>();

            var session = await dbContext.Sessions
                .FirstOrDefaultAsync(s => s.Id == key, token);

            if (session != null && session.ExpiresAt >= DateTime.UtcNow)
            {
                session.ExpiresAt = DateTime.UtcNow.AddMinutes(30); // Продлеваем срок действия
                await dbContext.SaveChangesAsync(token);
            }
        }

        private DateTime CalculateExpiration(DistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue)
            {
                return options.AbsoluteExpiration.Value.UtcDateTime;
            }

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                return DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            if (options.SlidingExpiration.HasValue)
            {
                return DateTime.UtcNow.Add(options.SlidingExpiration.Value);
            }

            return DateTime.UtcNow.AddMinutes(30); // Значение по умолчанию
        }
    }
}