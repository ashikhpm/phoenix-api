using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    // Role relationship
    public int UserRoleId { get; set; }
    public UserRole? UserRole { get; set; }
    
    // Navigation properties
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<MeetingPayment> MeetingPayments { get; set; } = new List<MeetingPayment>();
} 