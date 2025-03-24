using BCrypt.Net;

class Systen
{
    static void Main()
    {
        Console.WriteLine("Hash for 'farmer1': " + BCrypt.Net.BCrypt.HashPassword("farmer1"));
        Console.WriteLine("Hash for 'user1': " + BCrypt.Net.BCrypt.HashPassword("user1"));
    }
}