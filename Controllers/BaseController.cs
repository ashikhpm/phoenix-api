using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;
using phoenix_sangam_api.Services;
using System.Security.Claims;
using System.Diagnostics;

namespace phoenix_sangam_api.Controllers;

/// <summary>
/// Base controller with common functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected readonly UserDbContext _context;
    protected readonly ILogger _logger;
    protected readonly IUserActivityService _userActivityService;
    protected readonly IServiceProvider _serviceProvider;

    protected BaseController(UserDbContext context, ILogger logger, IUserActivityService userActivityService, IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _userActivityService = userActivityService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Get current user ID from JWT token
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Get current user role from JWT token
    /// </summary>
    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Check if current user is Secretary, President, or Treasurer
    /// </summary>
    protected bool IsAdmin()
    {
        var role = GetCurrentUserRole();
        return string.Equals(role, "Secretary", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "President", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Treasurer", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if current user is Secretary (for backward compatibility)
    /// </summary>
    protected bool IsSecretary()
    {
        var role = GetCurrentUserRole();
        return string.Equals(role, "Secretary", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get current user from database
    /// </summary>
    protected async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        return await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
    }

    /// <summary>
    /// Create success response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.CreateSuccess(data, message));
    }

    /// <summary>
    /// Create error response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Error<T>(string message, List<string>? errors = null)
    {
        return BadRequest(ApiResponse<T>.CreateError(message, errors));
    }

    /// <summary>
    /// Create not found response
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found")
    {
        return NotFound(ApiResponse<T>.CreateError(message));
    }

    /// <summary>
    /// Handle exceptions consistently
    /// </summary>
    protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation)
    {
        _logger.LogError(ex, "Error during {Operation}", operation);
        return StatusCode(500, ApiResponse<T>.CreateError($"An error occurred during {operation}"));
    }

    /// <summary>
    /// Validate model state and return errors if invalid
    /// </summary>
    protected ActionResult<ApiResponse<T>> ValidateModelState<T>()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Error<T>("Validation failed", errors);
        }
        return null;
    }

    /// <summary>
    /// Log operation with structured logging
    /// </summary>
    protected void LogOperation(string operation, params object[] parameters)
    {
        _logger.LogInformation("Operation: {Operation}, Parameters: {@Parameters}", operation, parameters);
    }

    /// <summary>
    /// Log warning with structured logging
    /// </summary>
    protected void LogWarning(string message, params object[] parameters)
    {
        _logger.LogWarning(message, parameters);
    }

    /// <summary>
    /// Log error with structured logging
    /// </summary>
    protected void LogError(Exception ex, string message, params object[] parameters)
    {
        _logger.LogError(ex, message, parameters);
    }

    /// <summary>
    /// Log user activity with comprehensive details (asynchronous fire-and-forget)
    /// </summary>
    protected void LogUserActivityAsync(
        string action,
        string entityType,
        int? entityId = null,
        string? description = null,
        object? details = null,
        bool isSuccess = true,
        string? errorMessage = null,
        long durationMs = 0)
    {
        // Capture current user ID and other data before the context is disposed
        var currentUserId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        var endpoint = GetCurrentEndpoint();
        var httpMethod = GetCurrentHttpMethod();
        var statusCode = Response.StatusCode;

        // Fire-and-forget asynchronous logging
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope for the background task
                using var scope = _serviceProvider.CreateScope();
                var userActivityService = scope.ServiceProvider.GetRequiredService<IUserActivityService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<BaseController>>();
                var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                // Get current user in the new scope
                var currentUser = currentUserId.HasValue 
                    ? await context.Users
                        .Include(u => u.UserRole)
                        .FirstOrDefaultAsync(u => u.Id == currentUserId.Value)
                    : null;

                if (currentUser == null)
                {
                    logger.LogWarning("Cannot log user activity - current user not found");
                    return;
                }

                await userActivityService.LogActivityAsync(
                    currentUser.Id,
                    currentUser.Name,
                    currentUser.UserRole?.Name ?? "Unknown",
                    action,
                    entityType,
                    entityId,
                    description,
                    details?.ToString(),
                    httpMethod,
                    endpoint,
                    ipAddress,
                    userAgent,
                    statusCode,
                    isSuccess,
                    errorMessage,
                    durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user activity in background");
                // Don't throw - logging should not break the main application flow
            }
        });
    }

    /// <summary>
    /// Log user activity with object details (serializes to JSON) - asynchronous fire-and-forget
    /// </summary>
    protected void LogUserActivityWithDetailsAsync(
        string action,
        string entityType,
        int? entityId = null,
        string? description = null,
        object? detailsObject = null,
        bool isSuccess = true,
        string? errorMessage = null,
        long durationMs = 0)
    {
        // Capture current user ID and other data before the context is disposed
        var currentUserId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        var endpoint = GetCurrentEndpoint();
        var httpMethod = GetCurrentHttpMethod();
        var statusCode = Response.StatusCode;

        // Fire-and-forget asynchronous logging
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope for the background task
                using var scope = _serviceProvider.CreateScope();
                var userActivityService = scope.ServiceProvider.GetRequiredService<IUserActivityService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<BaseController>>();
                var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                // Get current user in the new scope
                var currentUser = currentUserId.HasValue 
                    ? await context.Users
                        .Include(u => u.UserRole)
                        .FirstOrDefaultAsync(u => u.Id == currentUserId.Value)
                    : null;

                if (currentUser == null)
                {
                    logger.LogWarning("Cannot log user activity - current user not found");
                    return;
                }

                await userActivityService.LogActivityWithDetailsAsync(
                    currentUser.Id,
                    currentUser.Name,
                    currentUser.UserRole?.Name ?? "Unknown",
                    action,
                    entityType,
                    entityId,
                    description,
                    detailsObject,
                    httpMethod,
                    endpoint,
                    ipAddress,
                    userAgent,
                    statusCode,
                    isSuccess,
                    errorMessage,
                    durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user activity with details in background");
                // Don't throw - logging should not break the main application flow
            }
        });
    }

    /// <summary>
    /// Gets eligible users for a specific meeting date based on joining date, active state, and inactive date
    /// </summary>
    /// <param name="meetingDate">The date of the meeting</param>
    /// <param name="includeUserRole">Whether to include UserRole navigation property</param>
    /// <returns>List of eligible users</returns>
    protected async Task<List<User>> GetEligibleUsersForMeetingDate(DateTime meetingDate, bool includeUserRole = false)
    {
        var query = _context.Users.AsQueryable();
        
        if (includeUserRole)
        {
            query = query.Include(u => u.UserRole);
        }
        
        return await query
            .Where(u => 
                // User must be active
                u.IsActive &&
                // User must have joined before or on the meeting date
                (u.JoiningDate == null || u.JoiningDate.Value.Date <= meetingDate) &&
                // User must not be inactive on or before the meeting date
                (u.InactiveDate == null || u.InactiveDate.Value.Date > meetingDate)
            )
            .ToListAsync();
    }

    /// <summary>
    /// Gets total eligible users count for a specific meeting date
    /// </summary>
    /// <param name="meetingDate">The date of the meeting</param>
    /// <returns>Count of eligible users</returns>
    protected async Task<int> GetEligibleUsersCountForMeetingDate(DateTime meetingDate)
    {
        return await _context.Users
            .Where(u => 
                // User must be active
                u.IsActive &&
                // User must have joined before or on the meeting date
                (u.JoiningDate == null || u.JoiningDate.Value.Date <= meetingDate) &&
                // User must not be inactive on or before the meeting date
                (u.InactiveDate == null || u.InactiveDate.Value.Date > meetingDate)
            )
            .CountAsync();
    }

    /// <summary>
    /// Gets total eligible users count for current date (for dashboard purposes)
    /// </summary>
    /// <returns>Count of eligible users for current date</returns>
    protected async Task<int> GetCurrentEligibleUsersCount()
    {
        var currentDate = DateTime.Today;
        return await GetEligibleUsersCountForMeetingDate(currentDate);
    }

    /// <summary>
    /// Get client IP address
    /// </summary>
    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ??
               HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
    }

    /// <summary>
    /// Get user agent string
    /// </summary>
    private string? GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Get current endpoint path
    /// </summary>
    private string GetCurrentEndpoint()
    {
        return $"{HttpContext.Request.Method} {HttpContext.Request.Path}";
    }

    /// <summary>
    /// Get current HTTP method
    /// </summary>
    private string GetCurrentHttpMethod()
    {
        return HttpContext.Request.Method;
    }

    /// <summary>
    /// Execute action with performance tracking and logging
    /// </summary>
    protected async Task<T> ExecuteWithLoggingAsync<T>(
        Func<Task<T>> action,
        string operation,
        string entityType,
        int? entityId = null,
        string? description = null,
        object? details = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            var result = await action();
            isSuccess = true;
            return result;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogUserActivityAsync(
                operation,
                entityType,
                entityId,
                description,
                details,
                isSuccess,
                errorMessage,
                stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Execute action with performance tracking and logging (void version)
    /// </summary>
    protected async Task ExecuteWithLoggingAsync(
        Func<Task> action,
        string operation,
        string entityType,
        int? entityId = null,
        string? description = null,
        object? details = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            await action();
            isSuccess = true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogUserActivityAsync(
                operation,
                entityType,
                entityId,
                description,
                details,
                isSuccess,
                errorMessage,
                stopwatch.ElapsedMilliseconds);
        }
    }
} 