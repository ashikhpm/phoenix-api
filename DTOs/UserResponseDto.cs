namespace phoenix_sangam_api.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AadharNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? JoiningDate { get; set; }
    public DateTime? InactiveDate { get; set; }
    public int UserRoleId { get; set; }
    public UserRoleDto? UserRole { get; set; }
}

public class UserRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
} 