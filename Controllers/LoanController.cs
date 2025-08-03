using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Services;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Models;
using phoenix_sangam_api.DTOs;
using System.Security.Claims;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoanController : BaseController
{
    private readonly IEmailService _emailService;

    public LoanController(UserDbContext context, ILogger<LoanController> logger, IEmailService emailService, IUserActivityService userActivityService, IServiceProvider serviceProvider)
        : base(context, logger, userActivityService, serviceProvider)
    {
        _emailService = emailService;
    }

    // GET: api/loan (Accessible to both Members and Secretary)
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<LoanWithInterestDto>>> GetAllLoans()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting loans list");
            
            // Get the current user's ID from the JWT token
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                LogWarning("User ID not found in token");
                LogUserActivityAsync("View", "Loan", null, "Failed to retrieve loans - User ID not found in token", null, false, "User ID not found in token", stopwatch.ElapsedMilliseconds);
                return BadRequest("User ID not found in token");
            }

            // Get the current user to check their role
            var currentUser = await GetCurrentUserAsync();
            
            if (currentUser == null)
            {
                LogWarning("Current user not found");
                LogUserActivityAsync("View", "Loan", null, "Failed to retrieve loans - Current user not found", null, false, "Current user not found", stopwatch.ElapsedMilliseconds);
                return BadRequest("Current user not found");
            }

            var today = DateTime.Today;
            IQueryable<Loan> loansQuery;

            // If user is secretary, return all loans; otherwise, return only user's loans
            if (IsAdmin())
            {
                LogOperation("Secretary user - returning all loans");
                loansQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType);
            }
            else
            {
                LogOperation("Regular user - returning only user's loans. User ID: {UserId}", currentUserId);
                loansQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == currentUserId);
            }

            var loans = await loansQuery.ToListAsync();
            
            var loansWithInterest = loans.Select(l => 
            {
                var isOverdue = false;
                var daysOverdue = 0;
                var daysUntilDue = 0;
                
                // Only check for overdue if status is not "closed"
                if (!string.Equals(l.Status, "closed", StringComparison.OrdinalIgnoreCase))
                {
                    daysOverdue = (today - l.DueDate.Date).Days;
                    isOverdue = daysOverdue > 0;
                    daysUntilDue = l.DueDate.Date >= today ? (l.DueDate.Date - today).Days : 0;
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
                    LoanTerm = l.LoanTerm,
                    InterestReceived = l.InterestReceived,
                    Status = l.Status,
                    DaysSinceIssue = (today - l.Date.Date).Days,
                    InterestAmount = l.InterestReceived == 0 
                        ? CalculateInterest((decimal)(l.LoanType?.InterestRate ?? 0), l.Amount, l.Date, DateTime.Now)
                        : l.InterestReceived,
                    IsOverdue = isOverdue,
                    DaysOverdue = daysOverdue,
                    DaysUntilDue = daysUntilDue,
                    ChequeNumber = l.ChequeNumber
                };
            }).ToList();
            
            LogOperation("Retrieved {Count} loans for user {UserId}", loansWithInterest.Count, currentUserId);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Loan", null, $"Retrieved {loansWithInterest.Count} loans", new { Count = loansWithInterest.Count, UserId = currentUserId }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            return Ok(loansWithInterest);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving loans list");
            LogUserActivityAsync("View", "Loan", null, "Error retrieving loans list", null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving loans list");
        }
    }

    // GET: api/loan/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<LoanWithInterestDto>> GetLoan(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting loan with ID: {Id}", id);
            
            var loan = await _context.Loans.Include(l => l.User).Include(l => l.LoanType).FirstOrDefaultAsync(l => l.Id == id);
            if (loan == null)
            {
                LogWarning("Loan with ID {Id} not found", id);
                LogUserActivityAsync("View", "Loan", id, "Failed to retrieve loan - Loan not found", 
                    null, false, "Loan not found", stopwatch.ElapsedMilliseconds);
                return NotFound();
            }
            
            var today = DateTime.Today;
            var isOverdue = false;
            var daysOverdue = 0;
            var daysUntilDue = 0;
            
            // Only check for overdue if status is not "closed"
            if (!string.Equals(loan.Status, "closed", StringComparison.OrdinalIgnoreCase))
            {
                daysOverdue = (today - loan.DueDate.Date).Days;
                isOverdue = daysOverdue > 0;
                daysUntilDue = loan.DueDate.Date >= today ? (loan.DueDate.Date - today).Days : 0;
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
                LoanTerm = loan.LoanTerm,
                InterestReceived = loan.InterestReceived,
                Status = loan.Status,
                DaysSinceIssue = (today - loan.Date.Date).Days,
                InterestAmount = CalculateInterest((decimal)(loan.LoanType?.InterestRate ?? 0), loan.Amount, loan.Date, loan.ClosedDate ?? loan.DueDate),
                IsOverdue = isOverdue,
                DaysOverdue = daysOverdue,
                DaysUntilDue = daysUntilDue,
                ChequeNumber = loan.ChequeNumber
            };
            
            LogOperation("Successfully retrieved loan with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Loan", id, $"Retrieved loan for user {loan.User?.Name}", 
                new { 
                    LoanId = id, 
                    UserId = loan.UserId,
                    UserName = loan.User?.Name,
                    Amount = loan.Amount,
                    Status = loan.Status,
                    IsOverdue = isOverdue,
                    DaysOverdue = daysOverdue
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(loanWithInterest);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving loan with ID: {Id}", id);
            LogUserActivityAsync("View", "Loan", id, "Error retrieving loan", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving the loan");
        }
    }

    // POST: api/loan (Admin only)
    [HttpPost]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<LoanWithInterestDto>> CreateLoan([FromBody] CreateLoanDto loanDto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Creating new loan for user ID: {UserId}", loanDto.UserId);
            
            if (!ModelState.IsValid)
            {
                LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - Invalid model state", 
                    loanDto, false, "Invalid model state", stopwatch.ElapsedMilliseconds);
                return BadRequest(ModelState);
            }
            
            // Verify that the user exists
            var user = await _context.Users.FindAsync(loanDto.UserId);
            if (user == null)
            {
                LogWarning("User not found for loan creation: {UserId}", loanDto.UserId);
                LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - User not found", 
                    loanDto, false, "User not found", stopwatch.ElapsedMilliseconds);
                return BadRequest("User not found");
            }

            // Verify that the loan type exists
            var loanType = await _context.LoanTypes.FindAsync(loanDto.LoanTypeId);
            if (loanType == null)
            {
                LogWarning("Loan type not found for loan creation: {LoanTypeId}", loanDto.LoanTypeId);
                LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - Loan type not found", 
                    loanDto, false, "Loan type not found", stopwatch.ElapsedMilliseconds);
                return BadRequest("Loan type not found");
            }

            // Parse and convert dates to UTC
            if (!DateTime.TryParse(loanDto.Date, out DateTime parsedDate))
            {
                LogWarning("Invalid date format: {Date}", loanDto.Date);
                LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - Invalid date format", 
                    loanDto, false, "Invalid date format", stopwatch.ElapsedMilliseconds);
                return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
            }

            if (!DateTime.TryParse(loanDto.DueDate, out DateTime parsedDueDate))
            {
                LogWarning("Invalid due date format: {DueDate}", loanDto.DueDate);
                LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - Invalid due date format", 
                    loanDto, false, "Invalid due date format", stopwatch.ElapsedMilliseconds);
                return BadRequest("Invalid due date format. Please use yyyy-MM-dd format.");
            }

            DateTime? parsedClosedDate = null;
            if (!string.IsNullOrEmpty(loanDto.ClosedDate))
            {
                if (!DateTime.TryParse(loanDto.ClosedDate, out DateTime tempClosedDate))
                {
                    LogWarning("Invalid closed date format: {ClosedDate}", loanDto.ClosedDate);
                    LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - Invalid closed date format", 
                        loanDto, false, "Invalid closed date format", stopwatch.ElapsedMilliseconds);
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
                LoanTerm = loanDto.LoanTerm,
                Status = "Sanctioned",
                ChequeNumber = loanDto.ChequeNumber
            };
            
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
            
            // Send loan created email
            try
            {
                // Calculate expected interest
                var expectedInterest = CalculateInterest((decimal)loanType.InterestRate, loan.Amount, loan.Date, loan.DueDate);
                
                var emailSent = await _emailService.SendLoanCreatedEmailAsync(
                    user.Email,
                    user.Name,
                    loan.Amount,
                    loanType.LoanTypeName,
                    loan.DueDate,
                    loanType.InterestRate,
                    expectedInterest
                );
                
                if (emailSent)
                {
                    LogOperation("Loan created email sent successfully to {Email}", user.Email);
                }
                else
                {
                    LogWarning("Failed to send loan created email to {Email}", user.Email);
                }
            }
            catch (Exception emailEx)
            {
                LogError(emailEx, "Error sending loan created email to {Email}", user.Email);
                // Don't fail the loan creation if email fails
            }
            
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
                DaysOverdue = daysOverdue,
                ChequeNumber = loan.ChequeNumber
            };
            
            LogOperation("Successfully created loan with ID: {Id} for user: {UserName}", loan.Id, user.Name);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Create", "Loan", loan.Id, $"Created loan for user {user.Name}", 
                new { 
                    LoanId = loan.Id, 
                    UserId = loan.UserId,
                    UserName = user.Name,
                    Amount = loan.Amount,
                    LoanType = loanType.LoanTypeName,
                    InterestRate = loanType.InterestRate,
                    DueDate = loan.DueDate,
                    Status = loan.Status,
                    ChequeNumber = loan.ChequeNumber
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loanWithInterest);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error creating loan for user ID: {UserId}", loanDto.UserId);
            LogUserActivityAsync("Create", "Loan", null, "Error creating loan", 
                loanDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while creating the loan");
        }
    }

    // PUT: api/loan/{id} (Admin only)
    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
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
        existingLoan.LoanTerm = loanDto.LoanTerm;
        existingLoan.ChequeNumber = loanDto.ChequeNumber;
        
        await _context.SaveChangesAsync();
        return Ok(existingLoan);
    }

    // DELETE: api/loan/{id} (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
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
    [Authorize(Roles = "Secretary,President,Treasurer")]
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
        loan.Status = "Closed";
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
            DaysOverdue = daysOverdue,
            ChequeNumber = loan.ChequeNumber
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
    /// Gets user list for dropdown (excluding logged-in user)
    /// </summary>
    /// <returns>List of users with ID and name for dropdown</returns>
    [HttpGet("users")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<IEnumerable<UserDropdownDto>>> GetUsersForDropdown()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting users for dropdown");
            
            // Get the current user's ID from the JWT token
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                LogWarning("User ID not found in token");
                LogUserActivityAsync("View", "User", null, "Failed to retrieve users for dropdown - User ID not found in token", null, false, "User ID not found in token", stopwatch.ElapsedMilliseconds);
                return BadRequest("User ID not found in token");
            }

            // Get all active users except the logged-in user
            var users = await _context.Users
                .Where(u => u.IsActive && u.Id != currentUserId.Value)
                .OrderBy(u => u.Name)
                .Select(u => new UserDropdownDto
                {
                    Id = u.Id,
                    Name = u.Name
                })
                .ToListAsync();
            
            LogOperation("Retrieved {Count} users for dropdown", users.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "User", null, $"Retrieved {users.Count} users for dropdown", 
                new { Count = users.Count, ExcludedUserId = currentUserId.Value }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(users);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving users for dropdown");
            LogUserActivityAsync("View", "User", null, "Error retrieving users for dropdown", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving users for dropdown");
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
