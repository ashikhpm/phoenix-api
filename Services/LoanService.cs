using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Services;

public class LoanService : ILoanService
{
    private readonly UserDbContext _context;
    private readonly ILogger<LoanService> _logger;

    public LoanService(UserDbContext context, ILogger<LoanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<LoanWithInterestDto>> GetAllLoansAsync()
    {
        var loans = await _context.Loans
            .Include(l => l.User)
            .Include(l => l.LoanType)
            .ToListAsync();

        return loans.Select(MapToLoanWithInterestDto);
    }

    public async Task<LoanWithInterestDto?> GetLoanByIdAsync(int id)
    {
        var loan = await _context.Loans
            .Include(l => l.User)
            .Include(l => l.LoanType)
            .FirstOrDefaultAsync(l => l.Id == id);

        return loan != null ? MapToLoanWithInterestDto(loan) : null;
    }

    public async Task<LoanWithInterestDto> CreateLoanAsync(CreateLoanDto loanDto)
    {
        // Validate loan type exists
        var loanType = await _context.LoanTypes.FindAsync(loanDto.LoanTypeId);
        if (loanType == null)
            throw new ArgumentException("Loan type not found");

        // Parse date strings
        if (!DateTime.TryParse(loanDto.Date, out DateTime parsedDate))
            throw new ArgumentException("Invalid date format for Date");
        
        if (!DateTime.TryParse(loanDto.DueDate, out DateTime parsedDueDate))
            throw new ArgumentException("Invalid date format for DueDate");
        
        DateTime? parsedClosedDate = null;
        if (!string.IsNullOrEmpty(loanDto.ClosedDate) && DateTime.TryParse(loanDto.ClosedDate, out DateTime tempClosedDate))
        {
            parsedClosedDate = tempClosedDate;
        }
        
        var loan = new Loan
        {
            UserId = loanDto.UserId,
            Date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
            DueDate = DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc),
            ClosedDate = parsedClosedDate.HasValue 
                ? DateTime.SpecifyKind(parsedClosedDate.Value, DateTimeKind.Utc) 
                : null,
            LoanTypeId = loanDto.LoanTypeId,
            Amount = loanDto.Amount,
            LoanTerm = loanDto.LoanTerm,
            Status = "active",
            InterestReceived = 0
        };

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created loan {LoanId} for user {UserId}", loan.Id, loan.UserId);
        return await GetLoanByIdAsync(loan.Id) ?? throw new InvalidOperationException("Failed to retrieve created loan");
    }

    public async Task<LoanWithInterestDto> UpdateLoanAsync(int id, CreateLoanDto loanDto)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
            throw new ArgumentException("Loan not found");

        // Validate loan type exists
        var loanType = await _context.LoanTypes.FindAsync(loanDto.LoanTypeId);
        if (loanType == null)
            throw new ArgumentException("Loan type not found");

        // Parse date strings
        if (!DateTime.TryParse(loanDto.Date, out DateTime parsedDate))
            throw new ArgumentException("Invalid date format for Date");
        
        if (!DateTime.TryParse(loanDto.DueDate, out DateTime parsedDueDate))
            throw new ArgumentException("Invalid date format for DueDate");
        
        DateTime? parsedClosedDate = null;
        if (!string.IsNullOrEmpty(loanDto.ClosedDate) && DateTime.TryParse(loanDto.ClosedDate, out DateTime tempClosedDate))
        {
            parsedClosedDate = tempClosedDate;
        }
        
        loan.UserId = loanDto.UserId;
        loan.Date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        loan.DueDate = DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc);
        loan.ClosedDate = parsedClosedDate.HasValue 
            ? DateTime.SpecifyKind(parsedClosedDate.Value, DateTimeKind.Utc) 
            : null;
        loan.LoanTypeId = loanDto.LoanTypeId;
        loan.Amount = loanDto.Amount;
        loan.LoanTerm = loanDto.LoanTerm;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated loan {LoanId}", id);
        return await GetLoanByIdAsync(id) ?? throw new InvalidOperationException("Failed to retrieve updated loan");
    }

    public async Task<bool> DeleteLoanAsync(int id)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
            return false;

        _context.Loans.Remove(loan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted loan {LoanId}", id);
        return true;
    }

    public async Task<LoanWithInterestDto> ProcessLoanRepaymentAsync(LoanRepaymentDto repaymentDto)
    {
        var loan = await _context.Loans.FindAsync(repaymentDto.LoanId);
        if (loan == null)
            throw new ArgumentException("Loan not found");

        if (!DateTime.TryParse(repaymentDto.ClosedDate, out DateTime parsedClosedDate))
            throw new ArgumentException("Invalid date format for ClosedDate");
        
        loan.ClosedDate = DateTime.SpecifyKind(parsedClosedDate, DateTimeKind.Utc);
        loan.InterestReceived = repaymentDto.InterestAmount;
        loan.Status = "closed";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed repayment for loan {LoanId}", repaymentDto.LoanId);
        return await GetLoanByIdAsync(repaymentDto.LoanId) ?? throw new InvalidOperationException("Failed to retrieve updated loan");
    }

    public async Task<IEnumerable<LoanTypeDto>> GetLoanTypesAsync()
    {
        var loanTypes = await _context.LoanTypes.ToListAsync();
        return loanTypes.Select(lt => new LoanTypeDto
        {
            Id = lt.Id,
            LoanTypeName = lt.LoanTypeName,
            InterestRate = lt.InterestRate
        });
    }

    public async Task<IEnumerable<LoanWithInterestDto>> GetLoansByUserAsync(int userId, bool isSecretary)
    {
        IQueryable<Loan> loansQuery;

        if (isSecretary)
        {
            loansQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType);
        }
        else
        {
            loansQuery = _context.Loans
                .Include(l => l.User)
                .Include(l => l.LoanType)
                .Where(l => l.UserId == userId);
        }

        var loans = await loansQuery.ToListAsync();
        return loans.Select(MapToLoanWithInterestDto);
    }

    public async Task<LoanDueResponse> GetLoansDueAsync(int userId, bool isSecretary)
    {
        var today = DateTime.UtcNow.Date;
        IQueryable<Loan> baseQuery;

        if (isSecretary)
        {
            baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType);
        }
        else
        {
            baseQuery = _context.Loans
                .Include(l => l.User)
                .Include(l => l.LoanType)
                .Where(l => l.UserId == userId);
        }

        var allLoans = await baseQuery.ToListAsync();

        var overdueLoans = allLoans
            .Where(l => l.DueDate.Date < today && l.ClosedDate == null && l.Status.ToLower() != "closed")
            .Select(MapToLoanWithInterestDto)
            .ToList();

        var dueTodayLoans = allLoans
            .Where(l => l.DueDate.Date == today && l.ClosedDate == null && l.Status.ToLower() != "closed")
            .Select(MapToLoanWithInterestDto)
            .ToList();

        var dueThisWeekLoans = allLoans
            .Where(l => l.DueDate.Date > today && l.DueDate.Date <= today.AddDays(7) && l.ClosedDate == null && l.Status.ToLower() != "closed")
            .Select(MapToLoanWithInterestDto)
            .ToList();

        return new LoanDueResponse
        {
            OverdueLoans = overdueLoans,
            DueTodayLoans = dueTodayLoans,
            DueThisWeekLoans = dueThisWeekLoans
        };
    }

    public async Task<IEnumerable<LoanRequestResponseDto>> GetLoanRequestsAsync(int userId, bool isSecretary)
    {
        IQueryable<LoanRequest> requestsQuery;

        if (isSecretary)
        {
            requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType);
        }
        else
        {
            requestsQuery = _context.LoanRequests
                .Include(l => l.User)
                .Include(l => l.LoanType)
                .Where(l => l.UserId == userId);
        }

        var requests = await requestsQuery.OrderByDescending(l => l.Date).ToListAsync();
        return requests.Select(MapToLoanRequestResponseDto);
    }

    public async Task<LoanRequestResponseDto> CreateLoanRequestAsync(CreateLoanRequestDto requestDto, int userId)
    {
        if (!DateTime.TryParse(requestDto.DueDate, out DateTime parsedDueDate))
            throw new ArgumentException("Invalid date format for DueDate");
        
        var loanRequest = new LoanRequest
        {
            UserId = userId,
            Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            DueDate = DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc),
            LoanTypeId = requestDto.LoanTypeId,
            Amount = requestDto.Amount,
            LoanTerm = requestDto.LoanTerm,
            Status = "Requested"
        };

        _context.LoanRequests.Add(loanRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created loan request {RequestId} for user {UserId}", loanRequest.Id, userId);
        return await GetLoanRequestByIdAsync(loanRequest.Id, userId, true) ?? throw new InvalidOperationException("Failed to retrieve created loan request");
    }

    public async Task<LoanRequestResponseDto?> GetLoanRequestByIdAsync(int id, int userId, bool isSecretary)
    {
        var loanRequest = await _context.LoanRequests
            .Include(l => l.User)
            .Include(l => l.LoanType)
            .Include(l => l.ProcessedByUser)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loanRequest == null)
            return null;

        if (!isSecretary && loanRequest.UserId != userId)
            throw new UnauthorizedAccessException("Access denied");

        return MapToLoanRequestResponseDto(loanRequest);
    }

    public async Task<bool> DeleteLoanRequestAsync(int id, int userId, bool isSecretary)
    {
        var loanRequest = await _context.LoanRequests.FindAsync(id);
        if (loanRequest == null)
            return false;

        if (!isSecretary && loanRequest.UserId != userId)
            throw new UnauthorizedAccessException("Access denied");

        _context.LoanRequests.Remove(loanRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted loan request {RequestId}", id);
        return true;
    }

    public async Task<LoanRequestResponseDto> ProcessLoanRequestAsync(int id, string action, int secretaryId)
    {
        var loanRequest = await _context.LoanRequests
            .Include(l => l.User)
            .Include(l => l.LoanType)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loanRequest == null)
            throw new ArgumentException("Loan request not found");

        if (action.ToLower() != "accepted" && action.ToLower() != "rejected")
            throw new ArgumentException("Action must be 'accepted' or 'rejected'");

        loanRequest.Status = action;
        loanRequest.ProcessedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        loanRequest.ProcessedByUserId = secretaryId;

        if (action.ToLower() == "accepted")
        {
            var newLoan = new Loan
            {
                UserId = loanRequest.UserId,
                Date = loanRequest.Date,
                DueDate = loanRequest.DueDate,
                LoanTypeId = loanRequest.LoanTypeId,
                Amount = loanRequest.Amount,
                LoanTerm = loanRequest.LoanTerm,
                Status = "active",
                ClosedDate = null
            };
            _context.Loans.Add(newLoan);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed loan request {RequestId} with action {Action}", id, action);
        return await GetLoanRequestByIdAsync(id, loanRequest.UserId, true) ?? throw new InvalidOperationException("Failed to retrieve processed loan request");
    }

    public decimal CalculateInterest(decimal monthlyRate, decimal principal, DateTime loanDate, DateTime calculationDate)
    {
        if (calculationDate <= loanDate)
            return 0;

        var daysSinceIssue = (calculationDate - loanDate).Days;
        var monthsSinceIssue = daysSinceIssue / 30.0;
        var interestAmount = principal * (monthlyRate / 100) * (decimal)monthsSinceIssue;
        return Math.Round(interestAmount, 2);
    }

    private LoanWithInterestDto MapToLoanWithInterestDto(Loan loan)
    {
        var calculationDate = loan.ClosedDate ?? loan.DueDate;
        var interestAmount = CalculateInterest((decimal)(loan.LoanType?.InterestRate ?? 0), loan.Amount, loan.Date, calculationDate);

        return new LoanWithInterestDto
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
            InterestAmount = interestAmount,
            InterestReceived = loan.InterestReceived,
            Status = loan.Status,
            DaysSinceIssue = (int)(DateTime.UtcNow.Date - loan.Date.Date).TotalDays,
            IsOverdue = loan.DueDate.Date < DateTime.UtcNow.Date && loan.ClosedDate == null && loan.Status.ToLower() != "closed",
            DaysOverdue = loan.DueDate.Date < DateTime.UtcNow.Date && loan.ClosedDate == null 
                ? (DateTime.UtcNow.Date - loan.DueDate.Date).Days 
                : 0,
            DaysUntilDue = loan.DueDate.Date >= DateTime.UtcNow.Date 
                ? (loan.DueDate.Date - DateTime.UtcNow.Date).Days 
                : 0
        };
    }

    private LoanRequestResponseDto MapToLoanRequestResponseDto(LoanRequest loanRequest)
    {
        return new LoanRequestResponseDto
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
            LoanTerm = loanRequest.LoanTerm,
            Status = loanRequest.Status,
            RequestDate = loanRequest.Date,
            ProcessedDate = loanRequest.ProcessedDate,
            ProcessedByUserName = loanRequest.ProcessedByUser?.Name
        };
    }
} 