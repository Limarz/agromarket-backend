using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AgroMarket.Backend.Data
{
    public class AgroMarketDbContextFactory : IDesignTimeDbContextFactory<AgroMarketDbContext>
    {
        public AgroMarketDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AgroMarketDbContext>();
            optionsBuilder.UseMySql(configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(8, 0, 21)));

            return new AgroMarketDbContext(optionsBuilder.Options);
        }
    }
}