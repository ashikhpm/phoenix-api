using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api;

public class CreateLoanDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Date { get; set; } = string.Empty;

    [Required]
    public string DueDate { get; set; } = string.Empty;

    public string? ClosedDate { get; set; }

    [Required]
    public decimal InterestRate { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
}

public class LoanWithInterestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public decimal InterestRate { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestReceived { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysSinceIssue { get; set; }
    public decimal InterestAmount { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
}

public class LoanRepaymentDto
{
    [Required]
    public int LoanId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public decimal LoanAmount { get; set; }
    
    [Required]
    public decimal InterestAmount { get; set; }
    
    [Required]
    public string ClosedDate { get; set; } = string.Empty;
} 