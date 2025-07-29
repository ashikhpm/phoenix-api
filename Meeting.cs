using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class Meeting
{
    public int Id { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    public DateTime Time { get; set; }
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
    
    // Navigation properties
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<MeetingPayment> MeetingPayments { get; set; } = new List<MeetingPayment>();
} 