using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class CreateMeetingDto
{
    [Required]
    public string Date { get; set; } = string.Empty; // Format: "yyyy-MM-dd"
    
    [Required]
    public string Time { get; set; } = string.Empty; // Format: "HH:mm" or "HH:mm:ss"
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
}

public class UpdateMeetingDto
{
    [Required]
    public string Date { get; set; } = string.Empty;
    
    [Required]
    public string Time { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    public string? Location { get; set; }
}

 