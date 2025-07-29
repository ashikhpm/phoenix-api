using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(UserDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets meeting details with pagination and date filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 100)</param>
    /// <param name="startDate">Start date for filtering (format: yyyy-MM-dd)</param>
    /// <param name="endDate">End date for filtering (format: yyyy-MM-dd)</param>
    /// <returns>Paginated list of meeting details</returns>
    [HttpGet("meetings")]
    public async Task<ActionResult<PaginatedMeetingDetailsResponse>> GetMeetingDetails(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting meeting details. Page: {Page}, PageSize: {PageSize}, StartDate: {StartDate}, EndDate: {EndDate}", 
                page, pageSize, startDate, endDate);

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Parse date filters
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                if (!DateTime.TryParse(startDate, out DateTime parsedStartDate))
                {
                    _logger.LogWarning("Invalid start date format: {StartDate}", startDate);
                    return BadRequest("Invalid start date format. Please use yyyy-MM-dd format.");
                }
                startDateTime = parsedStartDate.Date;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParse(endDate, out DateTime parsedEndDate))
                {
                    _logger.LogWarning("Invalid end date format: {EndDate}", endDate);
                    return BadRequest("Invalid end date format. Please use yyyy-MM-dd format.");
                }
                endDateTime = parsedEndDate.Date.AddDays(1).AddTicks(-1); // End of the day
            }

            // Build query with date filtering
            var query = _context.Meetings
                .Include(m => m.Attendances)
                .Include(m => m.MeetingPayments)
                .AsQueryable();

            if (startDateTime.HasValue)
            {
                query = query.Where(m => m.Date >= startDateTime.Value);
            }

            if (endDateTime.HasValue)
            {
                query = query.Where(m => m.Date <= endDateTime.Value);
            }

            // Calculate totals for all entries in the filtered set (not paginated)
            var allFilteredMeetings = await query.ToListAsync();
            var totalMainPaymentAllEntries = allFilteredMeetings.Sum(m => m.MeetingPayments.Sum(p => p.MainPayment));
            var totalWeeklyPaymentAllEntries = allFilteredMeetings.Sum(m => m.MeetingPayments.Sum(p => p.WeeklyPayment));

            // Apply pagination
            var meetings = allFilteredMeetings
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Time)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Transform to response DTOs
            var meetingDetails = meetings.Select(m => new MeetingDetailsDto
            {
                Id = m.Id,
                Date = m.Date,
                Time = m.Time,
                Description = m.Description,
                Location = m.Location,
                AttendedUsersCount = m.Attendances.Count(a => a.IsPresent),
                TotalMainPayment = m.MeetingPayments.Where(p => m.Attendances.Any(a => a.UserId == p.UserId && a.IsPresent)).Sum(p => p.MainPayment),
                TotalWeeklyPayment = m.MeetingPayments.Where(p => m.Attendances.Any(a => a.UserId == p.UserId && a.IsPresent)).Sum(p => p.WeeklyPayment),
                TotalAttendanceCount = m.Attendances.Count,
                AttendancePercentage = m.Attendances.Count > 0 ? 
                    (double)m.Attendances.Count(a => a.IsPresent) / m.Attendances.Count * 100 : 0
            }).ToList();

            var totalCount = allFilteredMeetings.Count;
            var response = new PaginatedMeetingDetailsResponse
            {
                Meetings = meetingDetails,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1
                },
                TotalMainPaymentAllEntries = totalMainPaymentAllEntries,
                TotalWeeklyPaymentAllEntries = totalWeeklyPaymentAllEntries
            };

            _logger.LogInformation("Successfully retrieved {Count} meeting details. Total: {TotalCount}, Page: {Page}/{TotalPages}", 
                meetingDetails.Count, totalCount, page, response.Pagination.TotalPages);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting details");
            return StatusCode(500, "An error occurred while retrieving meeting details");
        }
    }

    /// <summary>
    /// Gets dashboard summary statistics
    /// </summary>
    /// <param name="startDate">Start date for filtering (format: yyyy-MM-dd)</param>
    /// <param name="endDate">End date for filtering (format: yyyy-MM-dd)</param>
    /// <returns>Dashboard summary statistics</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboardSummary(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting dashboard summary. StartDate: {StartDate}, EndDate: {EndDate}", 
                startDate, endDate);

            // Parse date filters
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                if (!DateTime.TryParse(startDate, out DateTime parsedStartDate))
                {
                    _logger.LogWarning("Invalid start date format: {StartDate}", startDate);
                    return BadRequest("Invalid start date format. Please use yyyy-MM-dd format.");
                }
                startDateTime = parsedStartDate.Date;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParse(endDate, out DateTime parsedEndDate))
                {
                    _logger.LogWarning("Invalid end date format: {EndDate}", endDate);
                    return BadRequest("Invalid end date format. Please use yyyy-MM-dd format.");
                }
                endDateTime = parsedEndDate.Date.AddDays(1).AddTicks(-1);
            }

            // Build query with date filtering
            var query = _context.Meetings
                .Include(m => m.Attendances)
                .Include(m => m.MeetingPayments)
                .AsQueryable();

            if (startDateTime.HasValue)
            {
                query = query.Where(m => m.Date >= startDateTime.Value);
            }

            if (endDateTime.HasValue)
            {
                query = query.Where(m => m.Date <= endDateTime.Value);
            }

            var meetings = await query.ToListAsync();

            var response = new DashboardSummaryResponse
            {
                TotalMeetings = meetings.Count,
                TotalMainPayment = meetings.Sum(m => m.MeetingPayments.Sum(p => p.MainPayment)),
                TotalWeeklyPayment = meetings.Sum(m => m.MeetingPayments.Sum(p => p.WeeklyPayment)),
                TotalAttendedUsers = meetings.Sum(m => m.Attendances.Count(a => a.IsPresent)),
                TotalAttendanceRecords = meetings.Sum(m => m.Attendances.Count),
                AverageAttendancePercentage = meetings.Count > 0 ? 
                    meetings.Average(m => m.Attendances.Count > 0 ? 
                        (double)m.Attendances.Count(a => a.IsPresent) / m.Attendances.Count * 100 : 0) : 0,
                DateRange = new DateRangeDto
                {
                    StartDate = startDateTime,
                    EndDate = endDateTime?.Date
                }
            };

            _logger.LogInformation("Successfully retrieved dashboard summary. Total Meetings: {TotalMeetings}, " +
                "Total Main Payment: {TotalMainPayment}, Total Weekly Payment: {TotalWeeklyPayment}, " +
                "Total Attended Users: {TotalAttendedUsers}", 
                response.TotalMeetings, response.TotalMainPayment, response.TotalWeeklyPayment, response.TotalAttendedUsers);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard summary");
            return StatusCode(500, "An error occurred while retrieving dashboard summary");
        }
    }

    /// <summary>
    /// Gets loans with due dates that have passed and loans due within 2 weeks
    /// </summary>
    /// <returns>Loans with overdue and upcoming due dates</returns>
    [HttpGet("loans-due")]
    public async Task<ActionResult<LoanDueResponse>> GetLoansDue()
    {
        try
        {
            _logger.LogInformation("Getting loans with due dates");
            
            var today = DateTime.Today;
            var twoWeeksFromNow = today.AddDays(14);
            
            // Get overdue loans (due date has passed)
            var overdueLoans = await _context.Loans
                .Include(l => l.User)
                .Where(l => l.DueDate.Date < today)
                .OrderBy(l => l.DueDate)
                .ToListAsync();
            
            // Get loans due within 2 weeks
            var upcomingLoans = await _context.Loans
                .Include(l => l.User)
                .Where(l => l.DueDate.Date >= today && l.DueDate.Date <= twoWeeksFromNow)
                .OrderBy(l => l.DueDate)
                .ToListAsync();
            
            var response = new LoanDueResponse
            {
                OverdueLoans = overdueLoans.Select(l => new LoanDueDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserName = l.User?.Name ?? "Unknown User",
                    Date = l.Date,
                    DueDate = l.DueDate,
                    InterestRate = l.InterestRate,
                    Amount = l.Amount,
                    Status = l.Status,
                    DaysOverdue = (today - l.DueDate.Date).Days,
                    InterestAmount = CalculateInterest(l.InterestRate, l.Amount, l.Date, today)
                }).ToList(),
                UpcomingLoans = upcomingLoans.Select(l => new LoanDueDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserName = l.User?.Name ?? "Unknown User",
                    Date = l.Date,
                    DueDate = l.DueDate,
                    InterestRate = l.InterestRate,
                    Amount = l.Amount,
                    Status = l.Status,
                    DaysUntilDue = (l.DueDate.Date - today).Days,
                    InterestAmount = CalculateInterest(l.InterestRate, l.Amount, l.Date, today)
                }).ToList()
            };
            
            _logger.LogInformation("Retrieved {OverdueCount} overdue loans and {UpcomingCount} upcoming loans", 
                response.OverdueLoans.Count, response.UpcomingLoans.Count);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loans due");
            return StatusCode(500, "An error occurred while retrieving loans due");
        }
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