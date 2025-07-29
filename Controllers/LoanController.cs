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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<LoanWithInterestDto>>> GetAllLoans()
    {
        var loans = await _context.Loans.Include(l => l.User).ToListAsync();
        var today = DateTime.Today;
        
        var loansWithInterest = loans.Select(l => new LoanWithInterestDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User?.Name ?? "Unknown User",
            Date = l.Date,
            DueDate = l.DueDate,
            InterestRate = l.InterestRate,
            Amount = l.Amount,
            Status = l.Status,
            DaysSinceIssue = (today - l.Date.Date).Days,
            InterestAmount = CalculateInterest(l.InterestRate, l.Amount, l.Date, today)
        }).ToList();
        
        return Ok(loansWithInterest);
    }

    // GET: api/loan/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanWithInterestDto>> GetLoan(int id)
    {
        var loan = await _context.Loans.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
            return NotFound();
            
        var today = DateTime.Today;
        var loanWithInterest = new LoanWithInterestDto
        {
            Id = loan.Id,
            UserId = loan.UserId,
            UserName = loan.User?.Name ?? "Unknown User",
            Date = loan.Date,
            DueDate = loan.DueDate,
            InterestRate = loan.InterestRate,
            Amount = loan.Amount,
            Status = loan.Status,
            DaysSinceIssue = (today - loan.Date.Date).Days,
            InterestAmount = CalculateInterest(loan.InterestRate, loan.Amount, loan.Date, today)
        };
        
        return Ok(loanWithInterest);
    }

    // POST: api/loan (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanWithInterestDto>> CreateLoan([FromBody] Loan loan)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        // Verify that the user exists
        var user = await _context.Users.FindAsync(loan.UserId);
        if (user == null)
            return BadRequest("User not found");
        
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
        
        // Return the created loan with user details
        var createdLoan = await _context.Loans.Include(l => l.User).FirstOrDefaultAsync(l => l.Id == loan.Id);
        // Return the created loan with user details and calculated interest
        var today = DateTime.Today;
        var loanWithInterest = new LoanWithInterestDto
        {
            Id = loan.Id,
            UserId = loan.UserId,
            UserName = loan.User?.Name ?? "Unknown User",
            Date = loan.Date,
            DueDate = loan.DueDate,
            InterestRate = loan.InterestRate,
            Amount = loan.Amount,
            Status = loan.Status,
            DaysSinceIssue = (today - loan.Date.Date).Days,
            InterestAmount = CalculateInterest(loan.InterestRate, loan.Amount, loan.Date, today)
        };
        
        return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loanWithInterest);
    }

    // PUT: api/loan/{id} (Admin only)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLoan(int id, [FromBody] Loan loan)
    {
        if (id != loan.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var existingLoan = await _context.Loans.FindAsync(id);
        if (existingLoan == null)
            return NotFound();
        existingLoan.UserId = loan.UserId;
        existingLoan.Date = loan.Date;
        existingLoan.DueDate = loan.DueDate;
        existingLoan.InterestRate = loan.InterestRate;
        existingLoan.Amount = loan.Amount;
        existingLoan.Status = loan.Status;
        await _context.SaveChangesAsync();
        return Ok(existingLoan);
    }

    // DELETE: api/loan/{id} (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLoan(int id)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
            return NotFound();
        _context.Loans.Remove(loan);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    /// <summary>
    /// Calculates interest amount based on monthly rate and days since loan issue
    /// </summary>
    /// <param name="monthlyRate">Monthly interest rate as percentage</param>
    /// <param name="principal">Loan principal amount</param>
    /// <param name="loanDate">Date when loan was issued</param>
    /// <param name="calculationDate">Date to calculate interest until</param>
    /// <returns>Interest amount</returns>
    private decimal CalculateInterest(decimal monthlyRate, decimal principal, DateTime loanDate, DateTime calculationDate)
    {
        if (calculationDate <= loanDate)
            return 0;
            
        var daysSinceIssue = (calculationDate - loanDate).Days;
        var monthsSinceIssue = daysSinceIssue / 30.0; // Convert days to months
        
        // Calculate interest: Principal * (Monthly Rate / 100) * Number of Months
        var interestAmount = principal * (monthlyRate / 100) * (decimal)monthsSinceIssue;
        
        return Math.Round(interestAmount, 2);
    }
} 