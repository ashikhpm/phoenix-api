using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace phoenix_sangam_api.Models;

public class Loan
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

    public DateTime? ClosedDate { get; set; }

    [Required]
    public decimal InterestRate { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public decimal InterestReceived { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
} 