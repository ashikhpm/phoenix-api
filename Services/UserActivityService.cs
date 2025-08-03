using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Models;
using phoenix_sangam_api.DTOs;
using System.Text.Json;

namespace phoenix_sangam_api.Services;

/// <summary>
/// Service for logging and retrieving user activities
/// </summary>
public class UserActivityService : IUserActivityService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserActivityService> _logger;

    public UserActivityService(UserDbContext context, ILogger<UserActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Log a user activity
    /// </summary>
    public async Task LogActivityAsync(UserActivity activity)
    {
        try
        {
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "User activity logged: User {UserId} ({UserName}) performed {Action} on {EntityType} {EntityId}",
                activity.UserId, activity.UserName, activity.Action, activity.EntityType, activity.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user activity for user {UserId}", activity.UserId);
            // Don't throw - logging should not break the main application flow
        }
    }

    /// <summary>
    /// Log a user activity with basic parameters
    /// </summary>
    public async Task LogActivityAsync(
        int userId,
        string userName,
        string userRole,
        string action,
        string entityType,
        int? entityId = null,
        string? description = null,
        string? details = null,
        string httpMethod = "GET",
        string endpoint = "",
        string? ipAddress = null,
        string? userAgent = null,
        int statusCode = 200,
        bool isSuccess = true,
        string? errorMessage = null,
        long durationMs = 0)
    {
        var activity = new UserActivity
        {
            UserId = userId,
            UserName = userName,
            UserRole = userRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Details = details,
            HttpMethod = httpMethod,
            Endpoint = endpoint,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            StatusCode = statusCode,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            Timestamp = DateTime.UtcNow
        };

        await LogActivityAsync(activity);
    }

    /// <summary>
    /// Get user activities with optional filtering
    /// </summary>
    public async Task<(List<UserActivity> Activities, int TotalCount)> GetUserActivitiesAsync(
        int? userId = null,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var query = _context.UserActivities
                .Include(ua => ua.User)
                .AsQueryable();

            // Apply filters
            if (userId.HasValue)
                query = query.Where(ua => ua.UserId == userId.Value);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(ua => ua.Action == action);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(ua => ua.EntityType == entityType);

            if (startDate.HasValue)
                query = query.Where(ua => ua.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(ua => ua.Timestamp <= endDate.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and ordering
            var activities = await query
                .OrderByDescending(ua => ua.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (activities, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activities");
            throw;
        }
    }

    /// <summary>
    /// Get user activities with comprehensive filtering
    /// </summary>
    public async Task<(List<UserActivity> Activities, int TotalCount)> GetUserActivitiesWithFilterAsync(UserActivityFilterDto filter)
    {
        try
        {
            var query = _context.UserActivities
                .Include(ua => ua.User)
                .AsQueryable();

            // Apply comprehensive filters
            if (filter.UserId.HasValue)
                query = query.Where(ua => ua.UserId == filter.UserId.Value);

            if (!string.IsNullOrEmpty(filter.UserName))
                query = query.Where(ua => ua.UserName.Contains(filter.UserName));

            if (!string.IsNullOrEmpty(filter.UserRole))
                query = query.Where(ua => ua.UserRole == filter.UserRole);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(ua => ua.Action == filter.Action);

            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(ua => ua.EntityType == filter.EntityType);

            if (filter.EntityId.HasValue)
                query = query.Where(ua => ua.EntityId == filter.EntityId.Value);

            if (!string.IsNullOrEmpty(filter.HttpMethod))
                query = query.Where(ua => ua.HttpMethod == filter.HttpMethod);

            if (!string.IsNullOrEmpty(filter.Endpoint))
                query = query.Where(ua => ua.Endpoint.Contains(filter.Endpoint));

            if (!string.IsNullOrEmpty(filter.IpAddress))
                query = query.Where(ua => ua.IpAddress != null && ua.IpAddress.Contains(filter.IpAddress));

            if (filter.IsSuccess.HasValue)
                query = query.Where(ua => ua.IsSuccess == filter.IsSuccess.Value);

            if (filter.StatusCode.HasValue)
                query = query.Where(ua => ua.StatusCode == filter.StatusCode.Value);

            if (filter.MinStatusCode.HasValue)
                query = query.Where(ua => ua.StatusCode >= filter.MinStatusCode.Value);

            if (filter.MaxStatusCode.HasValue)
                query = query.Where(ua => ua.StatusCode <= filter.MaxStatusCode.Value);

            if (filter.MinDurationMs.HasValue)
                query = query.Where(ua => ua.DurationMs >= filter.MinDurationMs.Value);

            if (filter.MaxDurationMs.HasValue)
                query = query.Where(ua => ua.DurationMs <= filter.MaxDurationMs.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(ua => ua.Timestamp >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(ua => ua.Timestamp <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.Description))
                query = query.Where(ua => ua.Description != null && ua.Description.Contains(filter.Description));

            if (!string.IsNullOrEmpty(filter.ErrorMessage))
                query = query.Where(ua => ua.ErrorMessage != null && ua.ErrorMessage.Contains(filter.ErrorMessage));

            if (!string.IsNullOrEmpty(filter.UserAgent))
                query = query.Where(ua => ua.UserAgent != null && ua.UserAgent.Contains(filter.UserAgent));

            if (!string.IsNullOrEmpty(filter.DetailsSearch))
                query = query.Where(ua => ua.Details != null && ua.Details.Contains(filter.DetailsSearch));

            // Apply sorting
            var sortBy = filter.SortBy?.ToLower() ?? "timestamp";
            var sortDirection = filter.SortDirection?.ToLower() ?? "desc";

            query = sortBy switch
            {
                "timestamp" => sortDirection == "asc" ? query.OrderBy(ua => ua.Timestamp) : query.OrderByDescending(ua => ua.Timestamp),
                "userid" => sortDirection == "asc" ? query.OrderBy(ua => ua.UserId) : query.OrderByDescending(ua => ua.UserId),
                "username" => sortDirection == "asc" ? query.OrderBy(ua => ua.UserName) : query.OrderByDescending(ua => ua.UserName),
                "action" => sortDirection == "asc" ? query.OrderBy(ua => ua.Action) : query.OrderByDescending(ua => ua.Action),
                "entitytype" => sortDirection == "asc" ? query.OrderBy(ua => ua.EntityType) : query.OrderByDescending(ua => ua.EntityType),
                "statuscode" => sortDirection == "asc" ? query.OrderBy(ua => ua.StatusCode) : query.OrderByDescending(ua => ua.StatusCode),
                "durationms" => sortDirection == "asc" ? query.OrderBy(ua => ua.DurationMs) : query.OrderByDescending(ua => ua.DurationMs),
                _ => query.OrderByDescending(ua => ua.Timestamp)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var activities = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (activities, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activities with filter");
            throw;
        }
    }

    /// <summary>
    /// Get user activity statistics
    /// </summary>
    public async Task<object> GetActivityStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var activities = await _context.UserActivities
                .Where(ua => ua.Timestamp >= startDate && ua.Timestamp <= endDate)
                .ToListAsync();

            var statistics = new
            {
                TotalActivities = activities.Count,
                SuccessfulActivities = activities.Count(a => a.IsSuccess),
                FailedActivities = activities.Count(a => !a.IsSuccess),
                AverageDurationMs = activities.Any() ? activities.Average(a => a.DurationMs) : 0,
                
                // Actions breakdown
                ActionsBreakdown = activities
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                
                // Entity types breakdown
                EntityTypesBreakdown = activities
                    .GroupBy(a => a.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                
                // Users breakdown
                UsersBreakdown = activities
                    .GroupBy(a => new { a.UserId, a.UserName })
                    .Select(g => new { UserId = g.Key.UserId, UserName = g.Key.UserName, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                
                // Daily activity
                DailyActivity = activities
                    .GroupBy(a => a.Timestamp.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToList(),
                
                // Status codes breakdown
                StatusCodesBreakdown = activities
                    .GroupBy(a => a.StatusCode)
                    .Select(g => new { StatusCode = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList()
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity statistics");
            throw;
        }
    }

    /// <summary>
    /// Log activity with object details (serializes object to JSON)
    /// </summary>
    public async Task LogActivityWithDetailsAsync(
        int userId,
        string userName,
        string userRole,
        string action,
        string entityType,
        int? entityId,
        string? description,
        object? detailsObject,
        string httpMethod,
        string endpoint,
        string? ipAddress,
        string? userAgent,
        int statusCode,
        bool isSuccess,
        string? errorMessage,
        long durationMs)
    {
        string? details = null;
        if (detailsObject != null)
        {
            try
            {
                details = JsonSerializer.Serialize(detailsObject, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize details object for user activity");
                details = "Serialization failed";
            }
        }

        await LogActivityAsync(
            userId, userName, userRole, action, entityType, entityId, description, details,
            httpMethod, endpoint, ipAddress, userAgent, statusCode, isSuccess, errorMessage, durationMs);
    }
} 