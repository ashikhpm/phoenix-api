using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace phoenix_sangam_api.Models;

public class UserRole
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
} 