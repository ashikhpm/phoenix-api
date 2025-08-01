using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;
using System.Security.Claims;

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

    protected BaseController(UserDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
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
    /// Check if current user is Secretary
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
} 