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
    public int LoanTypeId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public int LoanTerm { get; set; } // Loan term in months

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
    public int LoanTypeId { get; set; }
    public string LoanTypeName { get; set; } = string.Empty;
    public double InterestRate { get; set; }
    public decimal Amount { get; set; }
    public int LoanTerm { get; set; }
    public decimal InterestReceived { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysSinceIssue { get; set; }
    public decimal InterestAmount { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
    public int DaysUntilDue { get; set; }
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

public class CreateLoanRequestDto
{
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public int LoanTerm { get; set; } // Loan term in months
    
    [Required]
    public int LoanTypeId { get; set; }
    
    [Required]
    public string DueDate { get; set; } = string.Empty;
}

public class LoanRequestResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DueDate { get; set; }
    public int LoanTypeId { get; set; }
    public string LoanTypeName { get; set; } = string.Empty;
    public double InterestRate { get; set; }
    public decimal Amount { get; set; }
    public int LoanTerm { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? ProcessedByUserName { get; set; }
}

public class LoanTypeDto
{
    public int Id { get; set; }
    public string LoanTypeName { get; set; } = string.Empty;
    public double InterestRate { get; set; }
}

public class LoanRequestActionDto
{
    [Required]
    public string Action { get; set; } = string.Empty; // "accepted" or "rejected"
} 