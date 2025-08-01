using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.DTOs;

// Legacy DTOs for backward compatibility
public class ResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ResponseDto CreateSuccess(object data, string message = "Success")
    {
        return new ResponseDto
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ResponseDto CreateError(string message, List<string>? errors = null)
    {
        return new ResponseDto
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

// Meeting DTOs
public class MeetingResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
}



public class MeetingWithDetailsResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<AttendanceResponseDto> Attendances { get; set; } = new();
    public List<MeetingPaymentResponseDto> MeetingPayments { get; set; } = new();
    public decimal TotalMainPayment { get; set; }
    public decimal TotalWeeklyPayment { get; set; }
    public decimal TotalPayment => TotalMainPayment + TotalWeeklyPayment;
    public int TotalAttendees { get; set; }
    public int PresentAttendees { get; set; }
    public int AbsentAttendees { get; set; }
}

public class MeetingSummaryResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public int TotalAttendees { get; set; }
    public int PresentAttendees { get; set; }
    public int AbsentAttendees { get; set; }
    public decimal TotalMainPayment { get; set; }
    public decimal TotalWeeklyPayment { get; set; }
    public decimal TotalPayment => TotalMainPayment + TotalWeeklyPayment;
}

public class PaginatedMeetingDetailsResponse
{
    public List<MeetingWithDetailsResponseDto> Meetings { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public decimal TotalMainPaymentAllEntries { get; set; }
    public decimal TotalWeeklyPaymentAllEntries { get; set; }
    public decimal TotalPaymentAllEntries => TotalMainPaymentAllEntries + TotalWeeklyPaymentAllEntries;
}

// Dashboard DTOs
public class DashboardSummaryResponse
{
    public int TotalMeetings { get; set; }
    public int TotalUsers { get; set; }
    public int TotalLoans { get; set; }
    public decimal TotalLoanAmount { get; set; }
    public decimal TotalInterestAmount { get; set; }
    public int OverdueLoans { get; set; }
    public decimal TotalMainPayment { get; set; }
    public decimal TotalWeeklyPayment { get; set; }
    public decimal TotalPayment => TotalMainPayment + TotalWeeklyPayment;
    public List<MeetingSummaryResponseDto> RecentMeetings { get; set; } = new();
    public List<LoanWithInterestDto> RecentLoans { get; set; } = new();
}

// Loan Due Response
public class LoanDueResponse
{
    public List<LoanWithInterestDto> OverdueLoans { get; set; } = new();
    public List<LoanWithInterestDto> DueTodayLoans { get; set; } = new();
    public List<LoanWithInterestDto> DueThisWeekLoans { get; set; } = new();
    public int TotalOverdueCount => OverdueLoans.Count;
    public int TotalDueTodayCount => DueTodayLoans.Count;
    public int TotalDueThisWeekCount => DueThisWeekLoans.Count;
    public decimal TotalOverdueAmount => OverdueLoans.Sum(l => l.Amount + l.InterestAmount);
    public decimal TotalDueTodayAmount => DueTodayLoans.Sum(l => l.Amount + l.InterestAmount);
    public decimal TotalDueThisWeekAmount => DueThisWeekLoans.Sum(l => l.Amount + l.InterestAmount);
}

// User DTOs
public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

// Attendance DTOs
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

// Meeting Payment DTOs
public class MeetingPaymentResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MeetingId { get; set; }
    public decimal MainPayment { get; set; }
    public decimal WeeklyPayment { get; set; }
    public decimal TotalPayment => MainPayment + WeeklyPayment;
    public DateTime CreatedAt { get; set; }
    public UserResponseDto? User { get; set; }
    public MeetingResponseDto? Meeting { get; set; }
}

// Meeting Details DTOs
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
    public decimal TotalPayment => TotalMainPayment + TotalWeeklyPayment;
    public int TotalAttendanceCount { get; set; }
    public double AttendancePercentage { get; set; }
}

// Pagination DTOs
public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

// Loan Request Action DTO
public class LoanRequestActionDto
{
    [Required]
    public string Action { get; set; } = string.Empty; // "accepted" or "rejected"
} 