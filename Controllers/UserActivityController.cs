using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using phoenix_sangam_api.Models;
using phoenix_sangam_api.Services;
using phoenix_sangam_api.DTOs;
using System.Text.Json;

namespace phoenix_sangam_api.Controllers;

/// <summary>
/// Controller for managing and viewing user activities
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Secretary,President,Treasurer")]
public class UserActivityController : ControllerBase
{
    private readonly IUserActivityService _userActivityService;
    private readonly ILogger<UserActivityController> _logger;

    public UserActivityController(IUserActivityService userActivityService, ILogger<UserActivityController> logger)
    {
        _userActivityService = userActivityService;
        _logger = logger;
    }

    /// <summary>
    /// Get user activities with optional filtering
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="action">Filter by action (optional)</param>
    /// <param name="entityType">Filter by entity type (optional)</param>
    /// <param name="startDate">Filter by start date (optional)</param>
    /// <param name="endDate">Filter by end date (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>Paginated list of user activities</returns>
    [HttpGet]
    public async Task<ActionResult<object>> GetUserActivities(
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Retrieving user activities with filters: UserId={UserId}, Action={Action}, EntityType={EntityType}, StartDate={StartDate}, EndDate={EndDate}, Page={Page}, PageSize={PageSize}",
                userId, action, entityType, startDate, endDate, page, pageSize);

            var (activities, totalCount) = await _userActivityService.GetUserActivitiesAsync(
                userId, action, entityType, startDate, endDate, page, pageSize);

            var result = new
            {
                Activities = activities,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            _logger.LogInformation("Retrieved {Count} user activities out of {TotalCount}", activities.Count, totalCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activities");
            return StatusCode(500, "An error occurred while retrieving user activities");
        }
    }

    /// <summary>
    /// Get user activity statistics for a date range
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>Activity statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetActivityStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Retrieving activity statistics from {StartDate} to {EndDate}", startDate, endDate);

            var statistics = await _userActivityService.GetActivityStatisticsAsync(startDate, endDate);

            _logger.LogInformation("Retrieved activity statistics successfully");
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity statistics");
            return StatusCode(500, "An error occurred while retrieving activity statistics");
        }
    }

    /// <summary>
    /// Get recent user activities (last 24 hours)
    /// </summary>
    /// <param name="limit">Number of activities to retrieve (default: 100)</param>
    /// <returns>Recent user activities</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<object>> GetRecentActivities([FromQuery] int limit = 100)
    {
        try
        {
            _logger.LogInformation("Retrieving recent user activities (limit: {Limit})", limit);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddHours(-24);

            var (activities, totalCount) = await _userActivityService.GetUserActivitiesAsync(
                startDate: startDate,
                endDate: endDate,
                page: 1,
                pageSize: limit);

            var result = new
            {
                Activities = activities,
                TotalCount = totalCount,
                TimeRange = new { StartDate = startDate, EndDate = endDate }
            };

            _logger.LogInformation("Retrieved {Count} recent user activities", activities.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent user activities");
            return StatusCode(500, "An error occurred while retrieving recent user activities");
        }
    }

    /// <summary>
    /// Get user activities for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>User activities for the specified user</returns>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<object>> GetUserActivitiesByUserId(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Retrieving user activities for user {UserId}, Page={Page}, PageSize={PageSize}", userId, page, pageSize);

            var (activities, totalCount) = await _userActivityService.GetUserActivitiesAsync(
                userId: userId,
                page: page,
                pageSize: pageSize);

            var result = new
            {
                UserId = userId,
                Activities = activities,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            _logger.LogInformation("Retrieved {Count} activities for user {UserId}", activities.Count, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activities for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user activities");
        }
    }

    /// <summary>
    /// Get failed activities (activities with errors)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>Failed user activities</returns>
    [HttpGet("failed")]
    public async Task<ActionResult<object>> GetFailedActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Retrieving failed user activities, Page={Page}, PageSize={PageSize}", page, pageSize);

            // Note: This would require adding a method to filter by IsSuccess = false
            // For now, we'll get all activities and filter in memory
            var (activities, totalCount) = await _userActivityService.GetUserActivitiesAsync(
                page: page,
                pageSize: pageSize);

            var failedActivities = activities.Where(a => !a.IsSuccess).ToList();
            var failedCount = failedActivities.Count;

            var result = new
            {
                Activities = failedActivities,
                TotalCount = failedCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)failedCount / pageSize)
            };

            _logger.LogInformation("Retrieved {Count} failed user activities", failedCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed user activities");
            return StatusCode(500, "An error occurred while retrieving failed user activities");
        }
    }

    /// <summary>
    /// Get user activities with comprehensive filtering and details
    /// </summary>
    /// <param name="filter">Comprehensive filter object</param>
    /// <returns>Paginated list of user activities with enhanced details</returns>
    [HttpPost("filter")]
    public async Task<ActionResult<UserActivityListResponseDto>> GetUserActivitiesWithFilter([FromBody] UserActivityFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Retrieving user activities with comprehensive filter: {@Filter}", filter);

            // Validate filter
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1) filter.PageSize = 1;
            if (filter.PageSize > 1000) filter.PageSize = 1000;

            var (activities, totalCount) = await _userActivityService.GetUserActivitiesWithFilterAsync(filter);

            // Convert to detailed DTOs
            var detailedActivities = activities.Select(activity => new UserActivityDetailDto
            {
                Id = activity.Id,
                UserId = activity.UserId,
                UserName = activity.UserName,
                UserRole = activity.UserRole,
                Action = activity.Action,
                EntityType = activity.EntityType,
                EntityId = activity.EntityId,
                Description = activity.Description,
                Details = activity.Details,
                HttpMethod = activity.HttpMethod,
                Endpoint = activity.Endpoint,
                IpAddress = activity.IpAddress,
                UserAgent = activity.UserAgent,
                StatusCode = activity.StatusCode,
                IsSuccess = activity.IsSuccess,
                ErrorMessage = activity.ErrorMessage,
                Timestamp = activity.Timestamp,
                DurationMs = activity.DurationMs,
                FormattedDuration = FormatDuration(activity.DurationMs),
                StatusCodeCategory = GetStatusCodeCategory(activity.StatusCode),
                PerformanceCategory = GetPerformanceCategory(activity.DurationMs),
                FormattedDetails = filter.IncludeFormattedDetails ? ParseJsonDetails(activity.Details) : null,
                User = filter.IncludeUserDetails ? new UserDto
                {
                    Id = activity.User?.Id ?? 0,
                    Name = activity.User?.Name ?? activity.UserName,
                    Email = activity.User?.Email ?? "",
                    Address = activity.User?.Address ?? "",
                    Phone = activity.User?.Phone ?? "",
                    UserRoleId = activity.User?.UserRoleId ?? 0
                } : null
            }).ToList();

            // Calculate performance metrics if requested
            object? performanceMetrics = null;
            if (filter.IncludePerformanceMetrics)
            {
                performanceMetrics = new
                {
                    AverageDurationMs = activities.Any() ? activities.Average(a => a.DurationMs) : 0,
                    MinDurationMs = activities.Any() ? activities.Min(a => a.DurationMs) : 0,
                    MaxDurationMs = activities.Any() ? activities.Max(a => a.DurationMs) : 0,
                    SuccessRate = activities.Any() ? (double)activities.Count(a => a.IsSuccess) / activities.Count * 100 : 0,
                    AverageStatusCode = activities.Any() ? activities.Average(a => a.StatusCode) : 0
                };
            }

            // Calculate summary
            var summary = new
            {
                TotalActivities = totalCount,
                RetrievedActivities = activities.Count,
                SuccessCount = activities.Count(a => a.IsSuccess),
                FailureCount = activities.Count(a => !a.IsSuccess),
                UniqueUsers = activities.Select(a => a.UserId).Distinct().Count(),
                UniqueActions = activities.Select(a => a.Action).Distinct().Count(),
                UniqueEntityTypes = activities.Select(a => a.EntityType).Distinct().Count(),
                DateRange = activities.Any() ? new
                {
                    Earliest = activities.Min(a => a.Timestamp),
                    Latest = activities.Max(a => a.Timestamp)
                } : null
            };

            var response = new UserActivityListResponseDto
            {
                Activities = detailedActivities,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
                HasNextPage = filter.Page < (int)Math.Ceiling((double)totalCount / filter.PageSize),
                HasPreviousPage = filter.Page > 1,
                AppliedFilters = filter,
                PerformanceMetrics = performanceMetrics,
                Summary = summary
            };

            _logger.LogInformation("Retrieved {Count} user activities out of {TotalCount} with comprehensive filter", activities.Count, totalCount);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activities with comprehensive filter");
            return StatusCode(500, "An error occurred while retrieving user activities");
        }
    }

    /// <summary>
    /// Get available filter options for user activities
    /// </summary>
    /// <returns>Available filter options</returns>
    [HttpGet("filter-options")]
    public async Task<ActionResult<object>> GetFilterOptions()
    {
        try
        {
            _logger.LogInformation("Retrieving filter options for user activities");

            var allActivities = await _userActivityService.GetUserActivitiesAsync(pageSize: 10000);

            var filterOptions = new
            {
                Actions = allActivities.Activities.Select(a => a.Action).Distinct().OrderBy(a => a).ToList(),
                EntityTypes = allActivities.Activities.Select(a => a.EntityType).Distinct().OrderBy(a => a).ToList(),
                UserRoles = allActivities.Activities.Select(a => a.UserRole).Distinct().OrderBy(a => a).ToList(),
                HttpMethods = allActivities.Activities.Select(a => a.HttpMethod).Distinct().OrderBy(a => a).ToList(),
                StatusCodes = allActivities.Activities.Select(a => a.StatusCode).Distinct().OrderBy(a => a).ToList(),
                SortOptions = new[]
                {
                    "Timestamp", "UserId", "UserName", "Action", "EntityType", "StatusCode", "DurationMs"
                },
                SortDirections = new[] { "asc", "desc" }
            };

            _logger.LogInformation("Retrieved filter options successfully");
            return Ok(filterOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filter options");
            return StatusCode(500, "An error occurred while retrieving filter options");
        }
    }

    /// <summary>
    /// Format duration in milliseconds to human-readable format
    /// </summary>
    private static string FormatDuration(long durationMs)
    {
        if (durationMs < 1000) return $"{durationMs}ms";
        if (durationMs < 60000) return $"{durationMs / 1000.0:F1}s";
        return $"{durationMs / 60000.0:F1}m";
    }

    /// <summary>
    /// Get status code category
    /// </summary>
    private static string GetStatusCodeCategory(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "Success",
            >= 300 and < 400 => "Redirect",
            >= 400 and < 500 => "Client Error",
            >= 500 => "Server Error",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get performance category based on duration
    /// </summary>
    private static string GetPerformanceCategory(long durationMs)
    {
        return durationMs switch
        {
            < 100 => "Excellent",
            < 500 => "Good",
            < 1000 => "Average",
            < 5000 => "Slow",
            _ => "Very Slow"
        };
    }

    /// <summary>
    /// Parse JSON details string to object
    /// </summary>
    private static object? ParseJsonDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return null;

        try
        {
            return JsonSerializer.Deserialize<object>(details);
        }
        catch
        {
            return details; // Return as string if parsing fails
        }
    }
} 