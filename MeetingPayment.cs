using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class MeetingPayment
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int MeetingId { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal MainPayment { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal WeeklyPayment { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
    public Meeting? Meeting { get; set; }
} 