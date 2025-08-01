using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace phoenix_sangam_api.Models;

public class LoanRequest
{
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    [ForeignKey("LoanType")]
    public int LoanTypeId { get; set; }
    public LoanType? LoanType { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty; // Requested, Accepted, Rejected

    public DateTime? ProcessedDate { get; set; }

    public int? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }
} 