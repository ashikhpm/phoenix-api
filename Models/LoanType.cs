using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

public class LoanType
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string LoanTypeName { get; set; } = string.Empty;

    [Required]
    public double InterestRate { get; set; }
} 