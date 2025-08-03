using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class CreateAttendanceDto
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int MeetingId { get; set; }
    
    [Required]
    public bool IsPresent { get; set; }
}

public class UpdateAttendanceDto
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int MeetingId { get; set; }
    
    [Required]
    public bool IsPresent { get; set; }
}

public class BulkAttendanceDto
{
    [Required]
    public int MeetingId { get; set; }
    
    [Required]
    public List<AttendanceEntryDto> Attendances { get; set; } = new();
}

public class AttendanceEntryDto
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public bool IsPresent { get; set; }
} 