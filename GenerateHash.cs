using System;

class HashGenerator
{
    static void Main()
    {
        string password = "admin";
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine($"Хэш для пароля 'admin': {hash}");

        password = "password";
        hash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine($"Хэш для пароля 'password': {hash}");
    }
}