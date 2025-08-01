using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoanController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<LoanController> _logger;

    public LoanController(UserDbContext context, ILogger<LoanController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/loan (Admin only)
    [HttpGet]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<IEnumerable<LoanWithInterestDto>>> GetAllLoans()
    {
        var loans = await _context.Loans.Include(l => l.User).Include(l => l.LoanType).ToListAsync();
        var today = DateTime.Today;
        
        var loansWithInterest = loans.Select(l => 
        {
            var isOverdue = false;
            var daysOverdue = 0;
            
            // Only check for overdue if status is not "closed"
            if (!string.Equals(l.Status, "closed", StringComparison.OrdinalIgnoreCase))
            {
                daysOverdue = (today - l.DueDate.Date).Days;
                isOverdue = daysOverdue > 0;
            }
            
            return new LoanWithInterestDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User?.Name ?? "Unknown User",
                Date = l.Date,
                DueDate = l.DueDate,
                ClosedDate = l.ClosedDate,
                LoanTypeId = l.LoanTypeId,
                LoanTypeName = l.LoanType?.LoanTypeName ?? "Unknown",
                InterestRate = l.LoanType?.InterestRate ?? 0,
                Amount = l.Amount,
                InterestReceived = l.InterestReceived,
                Status = l.Status,
                DaysSinceIssue = (today - l.Date.Date).Days,
                InterestAmount = CalculateInterest((decimal)(l.LoanType?.InterestRate ?? 0), l.Amount, l.Date, l.ClosedDate ?? l.DueDate),
                IsOverdue = isOverdue,
                DaysOverdue = daysOverdue
            };
        }).ToList();
        
        return Ok(loansWithInterest);
    }

    // GET: api/loan/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<LoanWithInterestDto>> GetLoan(int id)
    {
        var loan = await _context.Loans.Include(l => l.User).Include(l => l.LoanType).FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
            return NotFound();
            
        var today = DateTime.Today;
        var isOverdue = false;
        var daysOverdue = 0;
        
        // Only check for overdue if status is not "closed"
        if (!string.Equals(loan.Status, "closed", StringComparison.OrdinalIgnoreCase))
        {
            daysOverdue = (today - loan.DueDate.Date).Days;
            isOverdue = daysOverdue > 0;
        }
        
        var loanWithInterest = new LoanWithInterestDto
        {
            Id = loan.Id,
            UserId = loan.UserId,
            UserName = loan.User?.Name ?? "Unknown User",
            Date = loan.Date,
            DueDate = loan.DueDate,
            ClosedDate = loan.ClosedDate,
            LoanTypeId = loan.LoanTypeId,
            LoanTypeName = loan.LoanType?.LoanTypeName ?? "Unknown",
            InterestRate = loan.LoanType?.InterestRate ?? 0,
            Amount = loan.Amount,
            InterestReceived = loan.InterestReceived,
            Status = loan.Status,
            DaysSinceIssue = (today - loan.Date.Date).Days,
            InterestAmount = CalculateInterest((decimal)(loan.LoanType?.InterestRate ?? 0), loan.Amount, loan.Date, loan.ClosedDate ?? loan.DueDate),
            IsOverdue = isOverdue,
            DaysOverdue = daysOverdue
        };
        
        return Ok(loanWithInterest);
    }

    // POST: api/loan (Admin only)
    [HttpPost]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<LoanWithInterestDto>> CreateLoan([FromBody] CreateLoanDto loanDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        // Verify that the user exists
        var user = await _context.Users.FindAsync(loanDto.UserId);
        if (user == null)
            return BadRequest("User not found");

        // Verify that the loan type exists
        var loanType = await _context.LoanTypes.FindAsync(loanDto.LoanTypeId);
        if (loanType == null)
            return BadRequest("Loan type not found");

        // Parse and convert dates to UTC
        if (!DateTime.TryParse(loanDto.Date, out DateTime parsedDate))
        {
            return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
        }

        if (!DateTime.TryParse(loanDto.DueDate, out DateTime parsedDueDate))
        {
            return BadRequest("Invalid due date format. Please use yyyy-MM-dd format.");
        }

        DateTime? parsedClosedDate = null;
        if (!string.IsNullOrEmpty(loanDto.ClosedDate))
        {
            if (!DateTime.TryParse(loanDto.ClosedDate, out DateTime tempClosedDate))
            {
                return BadRequest("Invalid closed date format. Please use yyyy-MM-dd format.");
            }
            parsedClosedDate = DateTime.SpecifyKind(tempClosedDate.Date, DateTimeKind.Utc);
        }

        var loan = new Loan
        {
            UserId = loanDto.UserId,
            Date = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc),
            DueDate = DateTime.SpecifyKind(parsedDueDate.Date, DateTimeKind.Utc),
            ClosedDate = parsedClosedDate,
            LoanTypeId = loanDto.LoanTypeId,
            Amount = loanDto.Amount,
            Status = loanDto.Status
        };
        
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
        
        // Return the created loan with user details and calculated interest
        var today = DateTime.Today;
        var isOverdue = false;
        var daysOverdue = 0;
        
        // Only check for overdue if status is not "closed"
        if (!string.Equals(loan.Status, "closed", StringComparison.OrdinalIgnoreCase))
        {
            daysOverdue = (today - loan.DueDate.Date).Days;
            isOverdue = daysOverdue > 0;
        }
        
        var loanWithInterest = new LoanWithInterestDto
        {
            Id = loan.Id,
            UserId = loan.UserId,
            UserName = user.Name,
            Date = loan.Date,
            DueDate = loan.DueDate,
            ClosedDate = loan.ClosedDate,
            LoanTypeId = loan.LoanTypeId,
            LoanTypeName = "Unknown", // Will be populated when loan is retrieved with includes
            InterestRate = 0, // Will be populated when loan is retrieved with includes
            Amount = loan.Amount,
            InterestReceived = loan.InterestReceived,
            Status = loan.Status,
            DaysSinceIssue = (today - loan.Date.Date).Days,
            InterestAmount = 0, // Will be calculated when loan is retrieved with includes
            IsOverdue = isOverdue,
            DaysOverdue = daysOverdue
        };
        
        return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loanWithInterest);
    }

    // PUT: api/loan/{id} (Admin only)
    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<IActionResult> UpdateLoan(int id, [FromBody] CreateLoanDto loanDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        var existingLoan = await _context.Loans.FindAsync(id);
        if (existingLoan == null)
            return NotFound();

        // Verify that the user exists
        var user = await _context.Users.FindAsync(loanDto.UserId);
        if (user == null)
            return BadRequest("User not found");

        // Verify that the loan type exists
        var loanType = await _context.LoanTypes.FindAsync(loanDto.LoanTypeId);
        if (loanType == null)
            return BadRequest("Loan type not found");

        // Parse and convert dates to UTC
        if (!DateTime.TryParse(loanDto.Date, out DateTime parsedDate))
        {
            return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
        }

        if (!DateTime.TryParse(loanDto.DueDate, out DateTime parsedDueDate))
        {
            return BadRequest("Invalid due date format. Please use yyyy-MM-dd format.");
        }

        DateTime? parsedClosedDate = null;
        if (!string.IsNullOrEmpty(loanDto.ClosedDate))
        {
            if (!DateTime.TryParse(loanDto.ClosedDate, out DateTime tempClosedDate))
            {
                return BadRequest("Invalid closed date format. Please use yyyy-MM-dd format.");
            }
            parsedClosedDate = DateTime.SpecifyKind(tempClosedDate.Date, DateTimeKind.Utc);
        }

        existingLoan.UserId = loanDto.UserId;
        existingLoan.Date = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
        existingLoan.DueDate = DateTime.SpecifyKind(parsedDueDate.Date, DateTimeKind.Utc);
        existingLoan.ClosedDate = parsedClosedDate;
        existingLoan.LoanTypeId = loanDto.LoanTypeId;
        existingLoan.Amount = loanDto.Amount;
        existingLoan.Status = loanDto.Status;
        
        await _context.SaveChangesAsync();
        return Ok(existingLoan);
    }

    // DELETE: api/loan/{id} (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<IActionResult> DeleteLoan(int id)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
            return NotFound();
        _context.Loans.Remove(loan);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/loan/repayment (Admin only)
    [HttpPost("repayment")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<LoanWithInterestDto>> LoanRepayment([FromBody] LoanRepaymentDto repaymentDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verify that the loan exists
        var loan = await _context.Loans.Include(l => l.User).Include(l => l.LoanType).FirstOrDefaultAsync(l => l.Id == repaymentDto.LoanId);
        if (loan == null)
            return BadRequest("Loan not found");

        // Verify that the user exists and matches the loan
        var user = await _context.Users.FindAsync(repaymentDto.UserId);
        if (user == null)
            return BadRequest("User not found");

        if (loan.UserId != repaymentDto.UserId)
            return BadRequest("User ID does not match the loan");

        // Parse the closed date
        if (!DateTime.TryParse(repaymentDto.ClosedDate, out DateTime parsedClosedDate))
        {
            return BadRequest("Invalid closed date format. Please use yyyy-MM-dd format.");
        }

        // Update the loan with repayment information
        loan.ClosedDate = DateTime.SpecifyKind(parsedClosedDate.Date, DateTimeKind.Utc);
        loan.Status = "closed";
        loan.Amount = repaymentDto.LoanAmount;
        loan.InterestReceived = repaymentDto.InterestAmount;
        
        await _context.SaveChangesAsync();

        // Return the updated loan with calculated interest
        var today = DateTime.Today;
        var isOverdue = false;
        var daysOverdue = 0;
        
        // Only check for overdue if status is not "closed" (but it is closed now, so this will be false)
        if (!string.Equals(loan.Status, "closed", StringComparison.OrdinalIgnoreCase))
        {
            daysOverdue = (today - loan.DueDate.Date).Days;
            isOverdue = daysOverdue > 0;
        }
        
        var loanWithInterest = new LoanWithInterestDto
        {
            Id = loan.Id,
            UserId = loan.UserId,
            UserName = loan.User?.Name ?? "Unknown User",
            Date = loan.Date,
            DueDate = loan.DueDate,
            ClosedDate = loan.ClosedDate,
            LoanTypeId = loan.LoanTypeId,
            LoanTypeName = loan.LoanType?.LoanTypeName ?? "Unknown",
            InterestRate = loan.LoanType?.InterestRate ?? 0,
            Amount = loan.Amount,
            Status = loan.Status,
            DaysSinceIssue = (today - loan.Date.Date).Days,
            InterestAmount = CalculateInterest((decimal)(loan.LoanType?.InterestRate ?? 0), loan.Amount, loan.Date, loan.ClosedDate ?? loan.DueDate),
            IsOverdue = isOverdue,
            DaysOverdue = daysOverdue
        };
        
        _logger.LogInformation("Loan repayment processed for Loan ID: {LoanId}, User ID: {UserId}, Amount: {Amount}, Interest: {Interest}", 
            repaymentDto.LoanId, repaymentDto.UserId, repaymentDto.LoanAmount, repaymentDto.InterestAmount);
        
        return Ok(loanWithInterest);
    }
    
    // GET: api/loan/types (Available to all authenticated users)
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<LoanTypeDto>>> GetLoanTypes()
    {
        try
        {
            var loanTypes = await _context.LoanTypes
                .OrderBy(lt => lt.LoanTypeName)
                .ToListAsync();

            var response = loanTypes.Select(lt => new LoanTypeDto
            {
                Id = lt.Id,
                LoanTypeName = lt.LoanTypeName,
                InterestRate = lt.InterestRate
            }).ToList();

            _logger.LogInformation("Retrieved {Count} loan types", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan types");
            return StatusCode(500, "An error occurred while retrieving loan types");
        }
    }

    /// <summary>
    /// Calculates interest amount based on monthly rate and days since loan issue
    /// </summary>
    /// <param name="monthlyRate">Monthly interest rate as percentage</param>
    /// <param name="principal">Loan principal amount</param>
    /// <param name="loanDate">Date when loan was issued</param>
    /// <param name="endDate">Date to calculate interest until (ClosedDate if available, otherwise DueDate)</param>
    /// <returns>Interest amount</returns>
    private decimal CalculateInterest(decimal monthlyRate, decimal principal, DateTime loanDate, DateTime endDate)
    {
        if (endDate <= loanDate)
            return 0;
            
        var daysSinceIssue = (endDate - loanDate).Days;
        var monthsSinceIssue = daysSinceIssue / 30.0; // Convert days to months
        
        // Calculate interest: Principal * (Monthly Rate / 100) * Number of Months
        var interestAmount = principal * (monthlyRate / 100) * (decimal)monthsSinceIssue;
        
        return Math.Round(interestAmount, 2);
    }
} 