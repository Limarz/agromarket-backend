using AgroMarket.Backend.Data;
using AgroMarket.Backend.Models;
using AgroMarket.Backend.Services;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed; // Добавляем для IDistributedCache
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Добавляем CORS с поддержкой credentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", builder =>
    {
        builder.WithOrigins("https://localhost:3000", "https://agromarket-frontend.onrender.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Настройка подключения к базе данных
builder.Services.AddDbContext<AgroMarketDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))));

// Настройка кастомного IDistributedCache для сессий
builder.Services.AddSingleton<IDistributedCache, MySqlDistributedCache>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
});

// Добавляем контроллеры
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AgroMarket API",
        Version = "v1",
        Description = "API for AgroMarket Backend"
    });
});

// Проверка строки подключения и подключения к базе данных
Console.WriteLine("Starting database connection attempt...");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString}");

using (var connection = new MySqlConnection(connectionString))
{
    try
    {
        connection.Open();
        Console.WriteLine("Successfully connected to the database!");
        using (var command = new MySqlCommand("SELECT DATABASE();", connection))
        {
            var dbName = command.ExecuteScalar()?.ToString();
            Console.WriteLine($"Connected to database: {dbName}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to connect to the database: {ex.Message}");
    }
}

Console.WriteLine("Finished database connection attempt.");

var app = builder.Build();

// Создание пользователя admin, если он не существует
using (var scope = app.Services.CreateScope()) // Используем IServiceScopeFactory через app.Services
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgroMarketDbContext>();
    var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
    if (adminRole == null)
    {
        adminRole = new Role { Name = "Admin" };
        dbContext.Roles.Add(adminRole);
        await dbContext.SaveChangesAsync();
        Console.WriteLine("Роль Admin создана.");
    }

    var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    if (adminUser == null)
    {
        adminUser = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            RoleId = adminRole.Id,
            IsPendingApproval = false,
            IsBlocked = false
        };
        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
        Console.WriteLine("Пользователь admin создан.");
    }
    else
    {
        Console.WriteLine("Пользователь admin уже существует.");
    }
}

// Настройка pipeline (порядок важен!)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroMarket API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowLocalhost");

// Добавляем обработку OPTIONS запросов
app.Use((context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "https://agromarket-frontend.onrender.com";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        context.Response.StatusCode = 200;
        return Task.CompletedTask;
    }
    return next();
});

app.UseSession();
app.UseAuthorization();

app.MapControllers();

app.Run();