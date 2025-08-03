using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Services;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;
using System.Security.Claims;
using phoenix_sangam_api;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : BaseController
{
    private readonly IEmailService _emailService;

    public DashboardController(UserDbContext context, ILogger<DashboardController> logger, IEmailService emailService, IUserActivityService userActivityService, IServiceProvider serviceProvider)
        : base(context, logger, userActivityService, serviceProvider)
    {
        _emailService = emailService;
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
            var meetingDetails = meetings.Select(m => new MeetingWithDetailsResponseDto
            {
                Id = m.Id,
                Date = m.Date,
                Time = m.Time,
                Description = m.Description,
                Location = m.Location,
                TotalMainPayment = m.MeetingPayments.Sum(p => p.MainPayment),
                TotalWeeklyPayment = m.MeetingPayments.Sum(p => p.WeeklyPayment),
                TotalAttendees = m.Attendances.Count,
                PresentAttendees = m.Attendances.Count(a => a.IsPresent),
                AbsentAttendees = m.Attendances.Count(a => !a.IsPresent),
                Attendances = m.Attendances.Select(a => new AttendanceResponseDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    MeetingId = a.MeetingId,
                    IsPresent = a.IsPresent,
                    CreatedAt = a.CreatedAt,
                    User = a.User != null ? new UserResponseDto
                    {
                        Id = a.User.Id,
                        Name = a.User.Name,
                        Address = a.User.Address,
                        Email = a.User.Email,
                        Phone = a.User.Phone
                    } : null,
                    Meeting = null // Avoid circular reference
                }).ToList(),
                MeetingPayments = m.MeetingPayments.Select(p => new MeetingPaymentResponseDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    MeetingId = p.MeetingId,
                    MainPayment = p.MainPayment,
                    WeeklyPayment = p.WeeklyPayment,
                    CreatedAt = p.CreatedAt,
                    User = p.User != null ? new UserResponseDto
                    {
                        Id = p.User.Id,
                        Name = p.User.Name,
                        Address = p.User.Address,
                        Email = p.User.Email,
                        Phone = p.User.Phone
                    } : null,
                    Meeting = null // Avoid circular reference
                }).ToList()
            }).ToList();

            var totalCount = allFilteredMeetings.Count;
            var response = new PaginatedMeetingDetailsResponse
            {
                Meetings = meetingDetails,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                TotalMainPaymentAllEntries = totalMainPaymentAllEntries,
                TotalWeeklyPaymentAllEntries = totalWeeklyPaymentAllEntries
            };

            _logger.LogInformation("Successfully retrieved {Count} meeting details. Total: {TotalCount}, Page: {Page}/{TotalPages}", 
                meetingDetails.Count, totalCount, page, response.TotalPages);

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
                TotalUsers = await GetCurrentEligibleUsersCount(), // Get eligible users for current date
                TotalLoans = await _context.Loans.CountAsync(),
                TotalLoanAmount = await _context.Loans.SumAsync(l => l.Amount),
                TotalInterestAmount = await _context.Loans.SumAsync(l => l.InterestReceived),
                OverdueLoans = await _context.Loans.CountAsync(l => l.DueDate < DateTime.Today && l.ClosedDate == null && l.Status.ToLower() != "closed"),
                RecentMeetings = meetings.Take(5).Select(m => new MeetingSummaryResponseDto
                {
                    Id = m.Id,
                    Date = m.Date,
                    Time = m.Time,
                    Description = m.Description,
                    Location = m.Location,
                    TotalAttendees = m.Attendances.Count,
                    PresentAttendees = m.Attendances.Count(a => a.IsPresent),
                    AbsentAttendees = m.Attendances.Count(a => !a.IsPresent),
                    TotalMainPayment = m.MeetingPayments.Sum(p => p.MainPayment),
                    TotalWeeklyPayment = m.MeetingPayments.Sum(p => p.WeeklyPayment)
                }).ToList(),
                RecentLoans = await _context.Loans
                    .Include(l => l.User)
                    .Include(l => l.LoanType)
                    .OrderByDescending(l => l.Date)
                    .Take(5)
                    .Select(l => new LoanWithInterestDto
                    {
                        Id = l.Id,
                        UserId = l.UserId,
                        UserName = l.User != null ? l.User.Name : string.Empty,
                        Date = l.Date,
                        DueDate = l.DueDate,
                        ClosedDate = l.ClosedDate,
                        LoanTypeId = l.LoanTypeId,
                        LoanTypeName = l.LoanType != null ? l.LoanType.LoanTypeName : string.Empty,
                        InterestRate = l.LoanType != null ? l.LoanType.InterestRate : 0,
                        Amount = l.Amount,
                        InterestReceived = l.InterestReceived,
                        Status = l.Status,
                        DaysSinceIssue = (int)(DateTime.Today - l.Date).TotalDays,
                        InterestAmount = l.InterestReceived,
                        IsOverdue = l.DueDate < DateTime.Today && l.ClosedDate == null && l.Status.ToLower() != "closed",
                        DaysOverdue = l.DueDate < DateTime.Today && l.ClosedDate == null && l.Status.ToLower() != "closed" ? 
                            (int)(DateTime.Today - l.DueDate).TotalDays : 0,
                        ChequeNumber = l.ChequeNumber
                    })
                    .ToListAsync()
            };

            _logger.LogInformation("Successfully retrieved dashboard summary. Total Meetings: {TotalMeetings}, " +
                "Total Main Payment: {TotalMainPayment}, Total Weekly Payment: {TotalWeeklyPayment}", 
                response.TotalMeetings, response.TotalMainPayment, response.TotalWeeklyPayment);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard summary");
            return StatusCode(500, "An error occurred while retrieving dashboard summary");
        }
    }

    /// <summary>
    /// Gets loans list - returns user's loans only if not admin, all loans if admin
    /// </summary>
    /// <returns>Loans list filtered by user role</returns>
    [HttpGet("loans")]
    public async Task<ActionResult<IEnumerable<LoanWithInterestDto>>> GetLoans()
    {
        try
        {
            _logger.LogInformation("Getting loans list");
            
            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Get the current user to check their role
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
            
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found");
                return BadRequest("Current user not found");
            }

            var today = DateTime.Today;
            IQueryable<Loan> loansQuery;

            // If user is admin (Secretary, President, Treasurer), return all loans; otherwise, return only user's loans
            if (string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Admin user ({Role}) - returning all loans", currentUser.UserRole?.Name);
                loansQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType);
            }
            else
            {
                _logger.LogInformation("Regular user - returning only user's loans. User ID: {UserId}", currentUserId);
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
                    InterestAmount = CalculateInterest((decimal)(l.LoanType?.InterestRate ?? 0), l.Amount, l.Date, l.ClosedDate ?? l.DueDate),
                    IsOverdue = isOverdue,
                    DaysOverdue = daysOverdue,
                    DaysUntilDue = daysUntilDue,
                    ChequeNumber = l.ChequeNumber
                };
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} loans for user {UserId}", loansWithInterest.Count, currentUserId);
            
            return Ok(loansWithInterest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loans list");
            return StatusCode(500, "An error occurred while retrieving loans list");
        }
    }

    /// <summary>
    /// Gets loans with due dates that have passed and loans due within 2 weeks
    /// </summary>
    /// <returns>Loans with overdue and upcoming due dates</returns>
    [HttpGet("loans-due")]
    public async Task<ActionResult<LoanDueResponse>> GetLoansDue([FromQuery] int? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting loans with due dates");
            
            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Get the current user to check their role
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
            
            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found");
                return BadRequest("Current user not found");
            }

            var today = DateTime.Today;
            var twoWeeksFromNow = today.AddDays(14);
            
            // Check if current user is admin
            bool isAdminUser = string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase);
            
            // Build base query based on user role and userId parameter
            IQueryable<Loan> baseQuery;
            if (isAdminUser)
            {
                if (userId.HasValue)
                {
                    // Admin user requesting specific user's loans
                    _logger.LogInformation("Admin user ({Role}) - returning loans due for user {TargetUserId}", currentUser.UserRole?.Name, userId.Value);
                    baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == userId.Value);
                }
                else
                {
                    // Admin user requesting all loans (current behavior)
                    _logger.LogInformation("Admin user ({Role}) - returning all loans due", currentUser.UserRole?.Name);
                    baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType);
                }
            }
            else
            {
                // Regular user can only see their own loans (current behavior)
                _logger.LogInformation("Regular user - returning only user's loans due. User ID: {UserId}", currentUserId);
                baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == currentUserId);
            }
            
            // Get overdue loans (due date has passed) - exclude closed loans
            var overdueLoans = await baseQuery
                .Where(l => l.DueDate.Date < today && 
                           l.ClosedDate == null && 
                           l.Status.ToLower() != "closed")
                .OrderBy(l => l.DueDate)
                .ToListAsync();
            
            // Get loans due today - exclude closed loans
            var dueTodayLoans = await baseQuery
                .Where(l => l.DueDate.Date == today && 
                           l.ClosedDate == null && 
                           l.Status.ToLower() != "closed")
                .OrderBy(l => l.DueDate)
                .ToListAsync();
            
            // Get loans due this week (next 7 days) - exclude closed loans
            var dueThisWeekLoans = await baseQuery
                .Where(l => l.DueDate.Date > today && 
                           l.DueDate.Date <= today.AddDays(7) && 
                           l.ClosedDate == null && 
                           l.Status.ToLower() != "closed")
                .OrderBy(l => l.DueDate)
                .ToListAsync();
            
            var response = new LoanDueResponse
            {
                OverdueLoans = overdueLoans.Select(l => new LoanWithInterestDto
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
                    DaysSinceIssue = (int)(DateTime.Today - l.Date).TotalDays,
                    InterestAmount = CalculateInterest((decimal)(l.LoanType?.InterestRate ?? 0), l.Amount, l.Date, l.ClosedDate ?? l.DueDate),
                    IsOverdue = true,
                    DaysOverdue = (today - l.DueDate.Date).Days,
                    DaysUntilDue = (l.DueDate.Date - today).Days,
                    ChequeNumber = l.ChequeNumber
                }).ToList(),
                DueTodayLoans = dueTodayLoans.Select(l => new LoanWithInterestDto
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
                    DaysSinceIssue = (int)(DateTime.Today - l.Date).TotalDays,
                    InterestAmount = CalculateInterest((decimal)(l.LoanType?.InterestRate ?? 0), l.Amount, l.Date, l.ClosedDate ?? l.DueDate),
                    IsOverdue = false,
                    DaysOverdue = 0,
                    DaysUntilDue = 0,
                    ChequeNumber = l.ChequeNumber
                }).ToList(),
                DueThisWeekLoans = dueThisWeekLoans.Select(l => new LoanWithInterestDto
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
                    DaysSinceIssue = (int)(DateTime.Today - l.Date).TotalDays,
                    InterestAmount = CalculateInterest((decimal)(l.LoanType?.InterestRate ?? 0), l.Amount, l.Date, l.ClosedDate ?? l.DueDate),
                    IsOverdue = false,
                    DaysOverdue = 0,
                    DaysUntilDue = (l.DueDate.Date - today).Days,
                    ChequeNumber = l.ChequeNumber
                }).ToList()
            };
            
            _logger.LogInformation("Retrieved {OverdueCount} overdue loans, {DueTodayCount} due today, and {DueThisWeekCount} due this week", 
                response.OverdueLoans.Count, response.DueTodayLoans.Count, response.DueThisWeekLoans.Count);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loans due");
            return StatusCode(500, "An error occurred while retrieving loans due");
        }
    }

    /// <summary>
    /// Creates a new loan request (accessible to all users)
    /// </summary>
    /// <param name="requestDto">Loan request details</param>
    /// <returns>Created loan request</returns>
    [HttpPost("loan-requests")]
    public async Task<ActionResult<LoanRequestResponseDto>> CreateLoanRequest([FromBody] CreateLoanRequestDto requestDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Verify that the user exists
            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null)
                return BadRequest("User not found");

            // Parse the due date
            if (!DateTime.TryParse(requestDto.DueDate, out DateTime parsedDueDate))
            {
                return BadRequest("Invalid due date format. Please use yyyy-MM-dd format.");
            }

            var loanRequest = new LoanRequest
            {
                UserId = currentUserId,
                Date = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
                DueDate = DateTime.SpecifyKind(parsedDueDate.Date, DateTimeKind.Utc),
                LoanTypeId = requestDto.LoanTypeId,
                Amount = requestDto.Amount,
                Description = requestDto.Description,
                Status = "Requested"
            };

            _context.LoanRequests.Add(loanRequest);
            await _context.SaveChangesAsync();

            var response = new LoanRequestResponseDto
            {
                Id = loanRequest.Id,
                UserId = loanRequest.UserId,
                UserName = user.Name,
                Date = loanRequest.Date,
                DueDate = loanRequest.DueDate,
                LoanTypeId = loanRequest.LoanTypeId,
                LoanTypeName = "Unknown", // Will be populated when retrieved with includes
                InterestRate = 0, // Will be populated when retrieved with includes
                Amount = loanRequest.Amount,
                Description = loanRequest.Description,
                Status = loanRequest.Status,
                RequestDate = loanRequest.Date
            };

            _logger.LogInformation("Loan request created by user {UserId} for amount {Amount}", currentUserId, requestDto.Amount);
            return CreatedAtAction(nameof(GetLoanRequest), new { id = loanRequest.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan request");
            return StatusCode(500, "An error occurred while creating the loan request");
        }
    }

    /// <summary>
    /// Gets loan requests - returns user's requests only if not admin, all requests if admin
    /// </summary>
    /// <returns>Loan requests filtered by user role</returns>
    [HttpGet("loan-requests")]
    public async Task<ActionResult<IEnumerable<LoanRequestResponseDto>>> GetLoanRequests([FromQuery] int? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting loan requests");

            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Get the current user to check their role
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found");
                return BadRequest("Current user not found");
            }

            // Check if current user is admin
            bool isAdminUser = string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase);

            IQueryable<LoanRequest> requestsQuery;

            // Build query based on user role and userId parameter
            if (isAdminUser)
            {
                if (userId.HasValue)
                {
                    // Admin user requesting specific user's loan requests
                    _logger.LogInformation("Admin user ({Role}) - returning loan requests for user {TargetUserId}", currentUser.UserRole?.Name, userId.Value);
                    requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == userId.Value);
                }
                else
                {
                    // Admin user requesting all loan requests (current behavior)
                    _logger.LogInformation("Admin user ({Role}) - returning all loan requests", currentUser.UserRole?.Name);
                    requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType);
                }
            }
            else
            {
                // Regular user can only see their own loan requests (current behavior)
                _logger.LogInformation("Regular user - returning only user's loan requests. User ID: {UserId}", currentUserId);
                requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == currentUserId);
            }

            var requests = await requestsQuery.OrderByDescending(l => l.Date).ToListAsync();

            var response = requests.Select(l => new LoanRequestResponseDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User?.Name ?? "Unknown User",
                Date = l.Date,
                DueDate = l.DueDate,
                LoanTypeId = l.LoanTypeId,
                LoanTypeName = l.LoanType?.LoanTypeName ?? "Unknown",
                InterestRate = l.LoanType?.InterestRate ?? 0,
                Amount = l.Amount,
                Description = l.Description,
                ChequeNumber = l.ChequeNumber,
                Status = l.Status,
                RequestDate = l.Date,
                ProcessedDate = l.ProcessedDate,
                ProcessedByUserName = l.ProcessedByUser?.Name,
                LoanTerm = l.LoanTerm
            }).ToList();

            _logger.LogInformation("Retrieved {Count} loan requests for user {UserId}", response.Count, currentUserId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan requests");
            return StatusCode(500, "An error occurred while retrieving loan requests");
        }
    }

    /// <summary>
    /// Gets a specific loan request by ID
    /// </summary>
    /// <param name="id">Loan request ID</param>
    /// <returns>Loan request details</returns>
    [HttpGet("loan-requests/{id}")]
    public async Task<ActionResult<LoanRequestResponseDto>> GetLoanRequest(int id)
    {
        try
        {
            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Get the current user to check their role
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found");
                return BadRequest("Current user not found");
            }

            var loanRequest = await _context.LoanRequests
                .Include(l => l.User)
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loanRequest == null)
                return NotFound("Loan request not found");

            // Check if user has access to this request
            bool isAdmin = string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && loanRequest.UserId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to access loan request {RequestId} without permission", currentUserId, id);
                return Forbid();
            }

            var response = new LoanRequestResponseDto
            {
                Id = loanRequest.Id,
                UserId = loanRequest.UserId,
                UserName = loanRequest.User?.Name ?? "Unknown User",
                Date = loanRequest.Date,
                DueDate = loanRequest.DueDate,
                LoanTypeId = loanRequest.LoanTypeId,
                LoanTypeName = loanRequest.LoanType?.LoanTypeName ?? "Unknown",
                InterestRate = loanRequest.LoanType?.InterestRate ?? 0,
                Amount = loanRequest.Amount,
                Description = loanRequest.Description,
                ChequeNumber = loanRequest.ChequeNumber,
                Status = loanRequest.Status,
                RequestDate = loanRequest.Date,
                ProcessedDate = loanRequest.ProcessedDate,
                ProcessedByUserName = loanRequest.ProcessedByUser?.Name
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan request {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the loan request");
        }
    }

    /// <summary>
    /// Deletes a loan request (accessible to request owner and admin)
    /// </summary>
    /// <param name="id">Loan request ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("loan-requests/{id}")]
    public async Task<ActionResult> DeleteLoanRequest(int id)
    {
        try
        {
            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Get the current user to check their role
            var currentUser = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found");
                return BadRequest("Current user not found");
            }

            var loanRequest = await _context.LoanRequests
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loanRequest == null)
                return NotFound("Loan request not found");

            // Check if user has permission to delete this request
            bool isAdmin = string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && loanRequest.UserId != currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete loan request {RequestId} without permission", currentUserId, id);
                return Forbid();
            }

            _context.LoanRequests.Remove(loanRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Loan request {RequestId} deleted by user {UserId}", id, currentUserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loan request {Id}", id);
            return StatusCode(500, "An error occurred while deleting the loan request");
        }
    }

    /// <summary>
    /// Accepts or rejects a loan request (Secretary only)
    /// </summary>
    /// <param name="id">Loan request ID</param>
    /// <param name="actionDto">Action to perform (accepted/rejected)</param>
    /// <returns>Updated loan request</returns>
    [HttpPut("loan-requests/{id}/action")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<LoanRequestResponseDto>> ProcessLoanRequest(int id, [FromBody] LoanRequestActionDto actionDto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Processing loan request {RequestId} with action: {Action}", id, actionDto.Action);
            
            if (!ModelState.IsValid)
            {
                LogUserActivityAsync("Process", "LoanRequest", id, "Failed to process loan request - Invalid model state", 
                    actionDto, false, "Invalid model state", stopwatch.ElapsedMilliseconds);
                return BadRequest(ModelState);
            }

            var loanRequest = await _context.LoanRequests
                .Include(l => l.User)
                .Include(l => l.LoanType)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loanRequest == null)
            {
                LogWarning("Loan request with ID {RequestId} not found", id);
                LogUserActivityAsync("Process", "LoanRequest", id, "Failed to process loan request - Request not found", 
                    actionDto, false, "Loan request not found", stopwatch.ElapsedMilliseconds);
                return NotFound("Loan request not found");
            }

            // Validate action
            var action = actionDto.Action;
            if (action != "Accepted" && action != "Rejected")
            {
                LogWarning("Invalid action for loan request {RequestId}: {Action}", id, action);
                LogUserActivityAsync("Process", "LoanRequest", id, "Failed to process loan request - Invalid action", 
                    actionDto, false, "Action must be 'Accepted' or 'Rejected'", stopwatch.ElapsedMilliseconds);
                return BadRequest("Action must be 'accepted' or 'rejected'");
            }

            // Get the current secretary user
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                LogWarning("Secretary user ID not found in token");
                LogUserActivityAsync("Process", "LoanRequest", id, "Failed to process loan request - User ID not found in token", 
                    actionDto, false, "Secretary user ID not found in token", stopwatch.ElapsedMilliseconds);
                return BadRequest("Secretary user ID not found in token");
            }

            var originalStatus = loanRequest.Status;
            var originalDescription = loanRequest.Description;
            var originalChequeNumber = loanRequest.ChequeNumber;

            // Update the loan request status and description
            loanRequest.Status = action;
            loanRequest.ProcessedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            loanRequest.ProcessedByUserId = currentUserId.Value;
            
            // Update description if provided
            if (!string.IsNullOrEmpty(actionDto.Description))
            {
                loanRequest.Description = actionDto.Description;
            }
            
            // Update cheque number if provided
            if (!string.IsNullOrEmpty(actionDto.ChequeNumber))
            {
                loanRequest.ChequeNumber = actionDto.ChequeNumber;
            }

            // If accepted, create a new loan
            Loan? newLoan = null;
            if (action == "Accepted")
            {
                newLoan = new Loan
                {
                    UserId = loanRequest.UserId,
                    Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    DueDate = loanRequest.DueDate,
                    LoanTypeId = loanRequest.LoanTypeId,
                    Amount = loanRequest.Amount,
                    LoanTerm = loanRequest.LoanTerm,
                    Status = "Sanctioned",
                    ClosedDate = null,
                    ChequeNumber = actionDto.ChequeNumber
                };
                _context.Loans.Add(newLoan);
            }

            await _context.SaveChangesAsync();

            // Send email notification
            bool emailSent = false;
            try
            {
                if (loanRequest.User != null && !string.IsNullOrEmpty(loanRequest.User.Email))
                {
                    if (action == "Accepted")
                    {
                        // Calculate expected interest
                        var expectedInterest = CalculateInterest(
                            (decimal)(loanRequest.LoanType?.InterestRate ?? 0), 
                            loanRequest.Amount, 
                            loanRequest.Date, 
                            loanRequest.DueDate
                        );
                        
                        emailSent = await _emailService.SendLoanRequestApprovedEmailAsync(
                            loanRequest.User.Email,
                            loanRequest.User.Name,
                            loanRequest.Amount,
                            loanRequest.LoanType?.LoanTypeName ?? "Unknown",
                            loanRequest.DueDate,
                            loanRequest.LoanType?.InterestRate ?? 0,
                            expectedInterest
                        );
                    }
                    else if (action == "Rejected")
                    {
                        emailSent = await _emailService.SendLoanRequestRejectedEmailAsync(
                            loanRequest.User.Email,
                            loanRequest.User.Name,
                            loanRequest.Amount,
                            loanRequest.LoanType?.LoanTypeName ?? "Unknown",
                            actionDto.Description ?? ""
                        );
                    }

                    if (emailSent)
                    {
                        LogOperation("Loan request {Action} email sent successfully to {Email}", action, loanRequest.User.Email);
                    }
                    else
                    {
                        LogWarning("Failed to send loan request {Action} email to {Email}", action, loanRequest.User.Email);
                    }
                }
            }
            catch (Exception emailEx)
            {
                LogError(emailEx, "Error sending loan request {Action} email to {Email}", action, loanRequest.User?.Email);
                // Don't fail the loan request processing if email fails
            }

            var response = new LoanRequestResponseDto
            {
                Id = loanRequest.Id,
                UserId = loanRequest.UserId,
                UserName = loanRequest.User?.Name ?? "Unknown User",
                Date = loanRequest.Date,
                DueDate = loanRequest.DueDate,
                LoanTypeId = loanRequest.LoanTypeId,
                LoanTypeName = loanRequest.LoanType?.LoanTypeName ?? "Unknown",
                InterestRate = loanRequest.LoanType?.InterestRate ?? 0,
                Amount = loanRequest.Amount,
                Description = loanRequest.Description,
                ChequeNumber = loanRequest.ChequeNumber,
                Status = loanRequest.Status,
                RequestDate = loanRequest.Date,
                ProcessedDate = loanRequest.ProcessedDate,
                ProcessedByUserName = loanRequest.ProcessedByUser?.Name
            };

            LogOperation("Loan request {RequestId} {Action} by secretary", id, action);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Process", "LoanRequest", id, $"Loan request {action.ToLower()} for user {loanRequest.User?.Name}", 
                new { 
                    RequestId = id, 
                    UserId = loanRequest.UserId,
                    UserName = loanRequest.User?.Name,
                    Action = action,
                    Amount = loanRequest.Amount,
                    LoanType = loanRequest.LoanType?.LoanTypeName,
                    OriginalStatus = originalStatus,
                    NewStatus = action,
                    OriginalDescription = originalDescription,
                    NewDescription = loanRequest.Description,
                    OriginalChequeNumber = originalChequeNumber,
                    NewChequeNumber = loanRequest.ChequeNumber,
                    ProcessedByUserId = currentUserId.Value,
                    ProcessedDate = loanRequest.ProcessedDate,
                    NewLoanCreated = newLoan != null,
                    NewLoanId = newLoan?.Id,
                    EmailSent = emailSent,
                    Changes = new {
                        StatusChanged = originalStatus != action,
                        DescriptionChanged = originalDescription != loanRequest.Description,
                        ChequeNumberChanged = originalChequeNumber != loanRequest.ChequeNumber
                    }
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error processing loan request {Id}", id);
            LogUserActivityAsync("Process", "LoanRequest", id, "Error processing loan request", 
                actionDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while processing the loan request");
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
