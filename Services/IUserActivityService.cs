using phoenix_sangam_api.Models;
using phoenix_sangam_api.DTOs;

namespace phoenix_sangam_api.Services;

/// <summary>
/// Interface for user activity logging service
/// </summary>
public interface IUserActivityService
{
    /// <summary>
    /// Log a user activity
    /// </summary>
    /// <param name="activity">The activity to log</param>
    /// <returns>Task representing the async operation</returns>
    Task LogActivityAsync(UserActivity activity);
    
    /// <summary>
    /// Log a user activity with basic parameters
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userName">User name</param>
    /// <param name="userRole">User role</param>
    /// <param name="action">Action performed</param>
    /// <param name="entityType">Entity type</param>
    /// <param name="entityId">Entity ID (optional)</param>
    /// <param name="description">Description (optional)</param>
    /// <param name="details">Additional details (optional)</param>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="ipAddress">IP address (optional)</param>
    /// <param name="userAgent">User agent (optional)</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed (optional)</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <returns>Task representing the async operation</returns>
    Task LogActivityAsync(
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
        long durationMs = 0);
    
    /// <summary>
    /// Get user activities with optional filtering
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="action">Filter by action (optional)</param>
    /// <param name="entityType">Filter by entity type (optional)</param>
    /// <param name="startDate">Filter by start date (optional)</param>
    /// <param name="endDate">Filter by end date (optional)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of user activities</returns>
    Task<(List<UserActivity> Activities, int TotalCount)> GetUserActivitiesAsync(
        int? userId = null,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50);
    
    /// <summary>
    /// Get user activities with comprehensive filtering
    /// </summary>
    /// <param name="filter">Comprehensive filter object</param>
    /// <returns>Paginated list of user activities with details</returns>
    Task<(List<UserActivity> Activities, int TotalCount)> GetUserActivitiesWithFilterAsync(UserActivityFilterDto filter);
    
    /// <summary>
    /// Get user activity statistics
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>Activity statistics</returns>
    Task<object> GetActivityStatisticsAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Log activity with object details (serializes object to JSON)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userName">User name</param>
    /// <param name="userRole">User role</param>
    /// <param name="action">Action performed</param>
    /// <param name="entityType">Entity type</param>
    /// <param name="entityId">Entity ID (optional)</param>
    /// <param name="description">Description (optional)</param>
    /// <param name="detailsObject">Object to serialize as details (optional)</param>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="ipAddress">IP address (optional)</param>
    /// <param name="userAgent">User agent (optional)</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if failed (optional)</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <returns>Task representing the async operation</returns>
    Task LogActivityWithDetailsAsync(
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
        long durationMs);
} 