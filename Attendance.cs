using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class Attendance
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int MeetingId { get; set; }
    
    [Required]
    public bool IsPresent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
    public Meeting? Meeting { get; set; }
} 