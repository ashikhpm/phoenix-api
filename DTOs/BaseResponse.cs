using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.DTOs;

/// <summary>
/// Base response wrapper for all API responses
/// </summary>
/// <typeparam name="T">Type of the data being returned</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> CreateSuccess(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> CreateError(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of the data being returned</typeparam>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PaginatedResponse<T> Create(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }
}

/// <summary>
/// Base DTO with common properties
/// </summary>
public abstract class BaseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Base request DTO with validation
/// </summary>
public abstract class BaseRequestDto
{
    public virtual bool IsValid(out List<string> errors)
    {
        errors = new List<string>();
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(this, context, results, true))
        {
            errors.AddRange(results.Select(r => r.ErrorMessage ?? "Validation error"));
        }
        
        return !errors.Any();
    }
}

/// <summary>
/// Comprehensive meeting summary response DTO
/// </summary>
public class ComprehensiveMeetingSummaryDto
{
    public int MeetingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime Time { get; set; }
    public string Location { get; set; } = string.Empty;
    public string MeetingMinutes { get; set; } = string.Empty;
    public List<MeetingAttendeeDto> AttendedUsers { get; set; } = new();
    public List<MeetingAttendeeDto> AbsentUsers { get; set; } = new();
    public MeetingAttendanceStatsDto AttendanceStats { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Meeting attendee DTO for comprehensive summary
/// </summary>
public class MeetingAttendeeDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? JoiningDate { get; set; }
    public DateTime? InactiveDate { get; set; }
    public bool IsActive { get; set; }
    public string AbsenceReason { get; set; } = string.Empty; // For absent users
}

/// <summary>
/// Meeting attendance statistics DTO
/// </summary>
public class MeetingAttendanceStatsDto
{
    public int TotalEligibleUsers { get; set; }
    public int AttendedCount { get; set; }
    public int AbsentCount { get; set; }
    public int InactiveUsersCount { get; set; }
    public int NotYetJoinedCount { get; set; }
    public double AttendancePercentage { get; set; }
    public List<string> AbsenceReasons { get; set; } = new();
} 