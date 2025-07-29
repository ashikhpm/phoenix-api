namespace phoenix_sangam_api.Models;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class MeetingResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
}

public class AttendanceResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MeetingId { get; set; }
    public bool IsPresent { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserResponseDto? User { get; set; }
    public MeetingResponseDto? Meeting { get; set; }
}

public class MeetingPaymentResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MeetingId { get; set; }
    public decimal MainPayment { get; set; }
    public decimal WeeklyPayment { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserResponseDto? User { get; set; }
    public MeetingResponseDto? Meeting { get; set; }
}

public class MeetingWithDetailsResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<AttendanceResponseDto> Attendances { get; set; } = new List<AttendanceResponseDto>();
    public List<MeetingPaymentResponseDto> MeetingPayments { get; set; } = new List<MeetingPaymentResponseDto>();
}

public class MeetingSummaryResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public decimal TotalMainPayment { get; set; }
    public decimal TotalWeeklyPayment { get; set; }
    public int PresentAttendanceCount { get; set; }
    public int TotalAttendanceCount { get; set; }
    public double AttendancePercentage { get; set; }
}

// Dashboard DTOs
public class MeetingDetailsDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public int AttendedUsersCount { get; set; }
    public decimal TotalMainPayment { get; set; }
    public decimal TotalWeeklyPayment { get; set; }
    public int TotalAttendanceCount { get; set; }
    public double AttendancePercentage { get; set; }
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class PaginatedMeetingDetailsResponse
{
    public List<MeetingDetailsDto> Meetings { get; set; } = new List<MeetingDetailsDto>();
    public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    public decimal TotalMainPaymentAllEntries { get; set; }
    public decimal TotalWeeklyPaymentAllEntries { get; set; }
}

public class DateRangeDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class DashboardSummaryResponse
{
    public int TotalMeetings { get; set; }
    public decimal TotalMainPayment { get; set; }
    public decimal TotalWeeklyPayment { get; set; }
    public int TotalAttendedUsers { get; set; }
    public int TotalAttendanceRecords { get; set; }
    public double AverageAttendancePercentage { get; set; }
    public DateRangeDto DateRange { get; set; } = new DateRangeDto();
}

// Loan DTOs
public class LoanDueDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InterestRate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? DaysOverdue { get; set; }
    public int? DaysUntilDue { get; set; }
    public decimal InterestAmount { get; set; }
}

public class LoanDueResponse
{
    public List<LoanDueDto> OverdueLoans { get; set; } = new List<LoanDueDto>();
    public List<LoanDueDto> UpcomingLoans { get; set; } = new List<LoanDueDto>();
}

public class LoanWithInterestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InterestRate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysSinceIssue { get; set; }
    public decimal InterestAmount { get; set; }
} 