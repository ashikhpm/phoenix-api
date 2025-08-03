using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Models;

/// <summary>
/// Model to track user activities and actions in the system
/// </summary>
public class UserActivity
{
    public int Id { get; set; }
    
    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    [Required]
    public int UserId { get; set; }
    
    /// <summary>
    /// Name of the user who performed the action
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role of the user who performed the action
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserRole { get; set; } = string.Empty;
    
    /// <summary>
    /// The action performed (e.g., "Create", "Update", "Delete", "View")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// The entity type that was acted upon (e.g., "User", "Loan", "Meeting")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity that was acted upon (if applicable)
    /// </summary>
    public int? EntityId { get; set; }
    
    /// <summary>
    /// Description of the action performed
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Additional details about the action (JSON format for complex data)
    /// </summary>
    [StringLength(2000)]
    public string? Details { get; set; }
    
    /// <summary>
    /// HTTP method used (GET, POST, PUT, DELETE)
    /// </summary>
    [Required]
    [StringLength(10)]
    public string HttpMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// API endpoint that was accessed
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address of the user
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string from the request
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Status code of the response
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Timestamp when the action was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Duration of the action in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
} 