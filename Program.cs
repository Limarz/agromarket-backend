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
        builder.WithOrigins("https://localhost:3000,https://agromarket-frontend.onrender.com")
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
// Добавлено: отладочное сообщение перед попыткой подключения
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

// Добавлено: отладочное сообщение после попытки подключения
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

app.UseHttpsRedirection();
app.UseStaticFiles(); // Добавляем поддержку статических файлов
app.UseRouting();

app.UseCors("AllowLocalhost");
app.UseSession();
app.UseAuthorization();

app.MapControllers();
/*
// Временный код для генерации хэшей
Console.WriteLine("Генерация хэшей для паролей...");
string adminPassword = "admin";
string userPassword = "password";

string adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
string userHash = BCrypt.Net.BCrypt.HashPassword(userPassword);

Console.WriteLine($"Хэш для пароля 'admin': {adminHash}");
Console.WriteLine($"Хэш для пароля 'password': {userHash}");
Console.WriteLine("Скопируйте хэши и обновите базу данных. После этого закомментируйте этот код.");
*/
app.Run();