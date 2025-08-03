using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.DTOs;

/// <summary>
/// DTO for updating meeting minutes
/// </summary>
public class MeetingMinutesDto
{
    /// <summary>
    /// Meeting ID
    /// </summary>
    [Required]
    public int MeetingId { get; set; }
    
    /// <summary>
    /// Meeting minutes content
    /// </summary>
    [Required]
    public string MeetingMinutes { get; set; } = string.Empty;
}

/// <summary>
/// DTO for meeting minutes response
/// </summary>
public class MeetingMinutesResponseDto
{
    public int MeetingId { get; set; }
    public string MeetingMinutes { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
} 