namespace AgroMarket.Backend.Models.DTOs;

public class UserDTO
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool IsBlocked { get; set; }
    public RoleDTO? Role { get; set; }
}

public class RoleDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }
}