using AgroMarket.Backend.Data;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;
using BCrypt.Net; // Добавляем для BCrypt

// Добавляем CORS с поддержкой credentials
var builder = WebApplication.CreateBuilder(args);

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

// Настройка сессий
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
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

app.UseStaticFiles(); // Добавляем поддержку статических файлов
app.UseRouting();

app.UseCors("AllowLocalhost");

// Добавляем обработку OPTIONS запросов
app.Use((context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "https://agromarket-frontend.onrender.com");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
        context.Response.StatusCode = 200;
        return Task.CompletedTask;
    }
    return next();
});

app.UseSession();
app.UseAuthorization();

app.MapControllers();

app.Run();