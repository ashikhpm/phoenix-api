using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class BulkMeetingPaymentDto
{
    [Required]
    public int MeetingId { get; set; }
    
    [Required]
    public List<MeetingPaymentEntryDto> Payments { get; set; } = new();
}

public class MeetingPaymentEntryDto
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal MainPayment { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal WeeklyPayment { get; set; }
} 