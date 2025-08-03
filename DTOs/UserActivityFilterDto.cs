using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.DTOs;

/// <summary>
/// DTO for filtering user activities
/// </summary>
public class UserActivityFilterDto
{
    /// <summary>
    /// Filter by specific user ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Filter by user name (partial match)
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Filter by user role
    /// </summary>
    public string? UserRole { get; set; }

    /// <summary>
    /// Filter by action type (Create, Update, Delete, View, etc.)
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by entity type (User, Loan, Meeting, etc.)
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by specific entity ID
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Filter by HTTP method (GET, POST, PUT, DELETE)
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Filter by endpoint path (partial match)
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Filter by IP address (partial match)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Filter by success status
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Filter by HTTP status code
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Filter by minimum status code
    /// </summary>
    public int? MinStatusCode { get; set; }

    /// <summary>
    /// Filter by maximum status code
    /// </summary>
    public int? MaxStatusCode { get; set; }

    /// <summary>
    /// Filter by minimum duration (milliseconds)
    /// </summary>
    public long? MinDurationMs { get; set; }

    /// <summary>
    /// Filter by maximum duration (milliseconds)
    /// </summary>
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// Filter by start date (inclusive)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by description (partial match)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Filter by error message (partial match)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Filter by user agent (partial match)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Search in details field (JSON content)
    /// </summary>
    public string? DetailsSearch { get; set; }

    /// <summary>
    /// Sort by field
    /// </summary>
    public string? SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string? SortDirection { get; set; } = "desc";

    /// <summary>
    /// Page number (default: 1)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (default: 50, max: 1000)
    /// </summary>
    [Range(1, 1000)]
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Whether to include user details in response
    /// </summary>
    public bool IncludeUserDetails { get; set; } = false;

    /// <summary>
    /// Whether to include formatted details (parsed JSON)
    /// </summary>
    public bool IncludeFormattedDetails { get; set; } = false;

    /// <summary>
    /// Whether to include performance metrics
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = false;
}

/// <summary>
/// Response DTO for user activity with additional details
/// </summary>
public class UserActivityDetailDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Description { get; set; }
    public string? Details { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public long DurationMs { get; set; }

    // Additional details
    public object? FormattedDetails { get; set; }
    public string? FormattedDuration { get; set; }
    public string? StatusCodeCategory { get; set; }
    public string? PerformanceCategory { get; set; }
    public UserDto? User { get; set; }
}

/// <summary>
/// Response DTO for user activity list with metadata
/// </summary>
public class UserActivityListResponseDto
{
    public List<UserActivityDetailDto> Activities { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public UserActivityFilterDto AppliedFilters { get; set; } = new();
    public object? PerformanceMetrics { get; set; }
    public object? Summary { get; set; }
}

/// <summary>
/// DTO for user information in activity details
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int UserRoleId { get; set; }
} 