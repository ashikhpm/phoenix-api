using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class UserLogin
{
    [Key]
    public int Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;
    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User? User { get; set; }
} 