using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using phoenix_sangam_api.Services;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : BaseController
{
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public UserController(UserDbContext context, ILogger<UserController> logger, IConfiguration configuration, IEmailService emailService, IUserActivityService userActivityService, IServiceProvider serviceProvider)
        : base(context, logger, userActivityService, serviceProvider)
    {
        _configuration = configuration;
        _emailService = emailService;
    }

    /// <summary>
    /// Gets all users (Secretary only) - excludes Secretary role users from the list
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users (default: true)</param>
    /// <returns>List of all users excluding Secretary role</returns>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers([FromQuery] bool includeInactive = true)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting all users, includeInactive: {IncludeInactive}", includeInactive);
            var query = _context.Users
                .Include(u => u.UserRole)
                .AsQueryable();

            // Filter by active status if requested
            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            var users = await query.ToListAsync();
            
            LogOperation("Retrieved {Count} users", users.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "User", null, $"Retrieved {users.Count} users", 
                new { Count = users.Count, IncludeInactive = includeInactive }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(users);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving users");
            LogUserActivityAsync("View", "User", null, "Error retrieving users", 
                new { IncludeInactive = includeInactive }, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Gets only active users (Secretary only)
    /// </summary>
    /// <returns>List of active users only</returns>
    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<User>>> GetActiveUsers()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting active users only");
            var users = await _context.Users
                .Include(u => u.UserRole)
                .Where(u => u.IsActive)
                .ToListAsync();
            
            LogOperation("Retrieved {Count} active users", users.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "User", null, $"Retrieved {users.Count} active users", 
                new { Count = users.Count, Filter = "Active Only" }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(users);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving active users");
            LogUserActivityAsync("View", "User", null, "Error retrieving active users", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving active users");
        }
    }

    /// <summary>
    /// Gets all user roles for dropdown selection
    /// </summary>
    /// <returns>List of user roles with ID and Name</returns>
    [HttpGet("roles")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetUserRoles()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting all user roles for dropdown");
            
            var roles = await _context.UserRoles
                .Select(r => new
                {
                    Id = r.Id,
                    Name = r.Name
                })
                .ToListAsync();
            
            LogOperation("Retrieved {Count} user roles", roles.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "UserRole", null, $"Retrieved {roles.Count} user roles", 
                new { Count = roles.Count }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(roles);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving user roles");
            LogUserActivityAsync("View", "UserRole", null, "Error retrieving user roles", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving user roles");
        }
    }

    /// <summary>
    /// Gets a specific user by ID (Admin only)
    /// </summary>
    /// <param name="id">The ID of the user to retrieve</param>
    /// <returns>The user if found, otherwise NotFound</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting user with ID: {Id}", id);
            
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                LogWarning("User with ID {Id} not found", id);
                LogUserActivityAsync("View", "User", id, "Failed to retrieve user - User not found", 
                    null, false, "User not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"User with ID {id} not found");
            }
            
            LogOperation("Successfully retrieved user with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityAsync("View", "User", id, $"Retrieved user {user.Name}", 
                new { UserId = id, UserName = user.Name, Email = user.Email }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(user);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving user with ID: {Id}", id);
            LogUserActivityAsync("View", "User", id, "Error retrieving user", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving the user");
        }
    }

    /// <summary>
    /// Creates a new user (Admin only)
    /// </summary>
    /// <param name="request">The user data to create</param>
    /// <returns>The created user with assigned ID</returns>
    [HttpPost]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Creating new user: {Name}", request.Name);
            
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            {
                LogWarning("Invalid user data: Name or Email is empty");
                LogUserActivityAsync("Create", "User", null, "Failed to create user - Invalid data", 
                    request, false, "Name and Email are required", stopwatch.ElapsedMilliseconds);
                return BadRequest("Name and Email are required");
            }

            // Validate user role
            var userRole = await _context.UserRoles.FindAsync(request.UserRoleId);
            if (userRole == null)
            {
                LogWarning("Invalid user role ID: {UserRoleId}", request.UserRoleId);
                LogUserActivityAsync("Create", "User", null, "Failed to create user - Invalid role", 
                    request, false, $"User role with ID {request.UserRoleId} not found", stopwatch.ElapsedMilliseconds);
                return BadRequest($"User role with ID {request.UserRoleId} not found");
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .Where(u => u.Email.ToLower() == request.Email.ToLower())
                .FirstOrDefaultAsync();
                
            if (existingUser != null)
            {
                LogWarning("Email already exists: {Email}", request.Email);
                LogUserActivityAsync("Create", "User", null, "Failed to create user - Email exists", 
                    request, false, "Email already exists", stopwatch.ElapsedMilliseconds);
                return BadRequest("Email already exists");
            }

            // Create new user with provided role
            var user = new User
            {
                Name = request.Name,
                Address = request.Address,
                Email = request.Email,
                Phone = request.Phone,
                AadharNumber = request.AadharNumber,
                UserRoleId = request.UserRoleId,
                IsActive = request.IsActive,
                JoiningDate = request.JoiningDate,
                InactiveDate = null
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Create UserLogin entry
            var userLogin = new UserLogin
            {
                Username = user.Email,
                Password = "password1",
                UserId = user.Id
            };
            _context.UserLogins.Add(userLogin);
            await _context.SaveChangesAsync();
            
            // Send welcome email
            try
            {
                var emailSent = await _emailService.SendUserWelcomeEmailAsync(user.Email, user.Name);
                if (emailSent)
                {
                    LogOperation("Welcome email sent successfully to {Email}", user.Email);
                }
                else
                {
                    LogWarning("Failed to send welcome email to {Email}", user.Email);
                }
            }
            catch (Exception emailEx)
            {
                LogError(emailEx, "Error sending welcome email to {Email}", user.Email);
                // Don't fail the user creation if email fails
            }
            
            LogOperation("Successfully created user with ID: {Id}", user.Id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Create", "User", user.Id, $"Created user {user.Name} with email {user.Email}", 
                new { 
                    UserId = user.Id, 
                    UserName = user.Name, 
                    Email = user.Email, 
                    Role = userRole.Name,
                    IsActive = user.IsActive,
                    JoiningDate = user.JoiningDate
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error creating user: {Name}", request.Name);
            LogUserActivityAsync("Create", "User", null, "Error creating user", 
                request, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while creating the user");
        }
    }

    /// <summary>
    /// Updates an existing user (Admin only)
    /// </summary>
    /// <param name="id">The ID of the user to update</param>
    /// <param name="user">The updated user data</param>
    /// <returns>The updated user</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User user)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Updating user with ID: {Id}", id);
            
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                LogWarning("User with ID {Id} not found for update", id);
                LogUserActivityAsync("Update", "User", id, "Failed to update user - User not found", 
                    user, false, "User not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"User with ID {id} not found");
            }

            if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
            {
                LogWarning("Invalid user data for update: Name or Email is empty");
                LogUserActivityAsync("Update", "User", id, "Failed to update user - Invalid data", 
                    user, false, "Name and Email are required", stopwatch.ElapsedMilliseconds);
                return BadRequest("Name and Email are required");
            }

            // Check if email already exists for another user - Fixed for EF Core compatibility
            var duplicateEmail = await _context.Users
                .Where(u => u.Id != id && u.Email.ToLower() == user.Email.ToLower())
                .FirstOrDefaultAsync();
                
            if (duplicateEmail != null)
            {
                LogWarning("Email already exists for another user: {Email}", user.Email);
                LogUserActivityAsync("Update", "User", id, "Failed to update user - Email exists", 
                    user, false, "Email already exists", stopwatch.ElapsedMilliseconds);
                return BadRequest("Email already exists");
            }

            var originalName = existingUser.Name;
            var originalEmail = existingUser.Email;
            var originalJoiningDate = existingUser.JoiningDate;

            // Update user fields
            existingUser.Name = user.Name;
            existingUser.Address = user.Address;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;
            existingUser.AadharNumber = user.AadharNumber;
            existingUser.JoiningDate = user.JoiningDate;
            
            // Check if email was changed and update UserLogin table accordingly
            var emailChanged = originalEmail.ToLower() != user.Email.ToLower();
            if (emailChanged)
            {
                LogOperation("Email changed for user {UserId} from {OriginalEmail} to {NewEmail}", 
                    id, originalEmail, user.Email);
                
                // Find and update the corresponding UserLogin record
                var userLogin = await _context.UserLogins
                    .Where(ul => ul.UserId == id)
                    .FirstOrDefaultAsync();
                
                if (userLogin != null)
                {
                    userLogin.Username = user.Email; // Update username to match new email
                    LogOperation("Updated UserLogin username for user {UserId} to {NewEmail}", id, user.Email);
                }
                else
                {
                    LogWarning("No UserLogin record found for user {UserId} when updating email", id);
                }
            }
            
            await _context.SaveChangesAsync();
            
            LogOperation("Successfully updated user with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Update", "User", id, $"Updated user {user.Name}", 
                new { 
                    UserId = id, 
                    UserName = user.Name, 
                    Email = user.Email,
                    OriginalName = originalName,
                    OriginalEmail = originalEmail,
                    OriginalJoiningDate = originalJoiningDate,
                    NewJoiningDate = user.JoiningDate,
                    EmailChanged = emailChanged,
                    UserLoginUpdated = emailChanged,
                    Changes = new { 
                        NameChanged = originalName != user.Name,
                        EmailChanged = originalEmail != user.Email,
                        JoiningDateChanged = originalJoiningDate != user.JoiningDate,
                        AddressChanged = existingUser.Address != user.Address,
                        PhoneChanged = existingUser.Phone != user.Phone,
                        AadharNumberChanged = existingUser.AadharNumber != user.AadharNumber
                    }
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error updating user with ID: {Id}", id);
            LogUserActivityAsync("Update", "User", id, "Error updating user", 
                user, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Soft deletes a user by marking them as inactive (Admin only)
    /// </summary>
    /// <param name="id">The ID of the user to deactivate</param>
    /// <returns>No content on successful deactivation</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Deactivating user with ID: {Id}", id);
            
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                LogWarning("User with ID {Id} not found for deactivation", id);
                LogUserActivityAsync("Delete", "User", id, "Failed to deactivate user - User not found", 
                    null, false, "User not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"User with ID {id} not found");
            }
            
            // Soft delete: mark user as inactive and set inactive date
            user.IsActive = false;
            user.InactiveDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            LogOperation("Successfully deactivated user with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Delete", "User", id, $"Deactivated user {user.Name}", 
                new { 
                    UserId = id, 
                    UserName = user.Name, 
                    Email = user.Email,
                    InactiveDate = user.InactiveDate,
                    PreviousStatus = "Active"
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error deactivating user with ID: {Id}", id);
            LogUserActivityAsync("Delete", "User", id, "Error deactivating user", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while deactivating the user");
        }
    }

    /// <summary>
    /// Gets current user information from JWT token
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            // Get the current user's ID from the JWT token
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                LogWarning("User ID not found in token");
                LogUserActivityAsync("View", "User", null, "Failed to get current user - User ID not found in token", 
                    null, false, "User ID not found in token", stopwatch.ElapsedMilliseconds);
                return BadRequest("User ID not found in token");
            }

            // Get the current user with role information
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                LogWarning("Current user not found");
                LogUserActivityAsync("View", "User", currentUserId, "Failed to get current user - User not found", 
                    null, false, "User not found", stopwatch.ElapsedMilliseconds);
                return NotFound("User not found");
            }

            var response = new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.UserRole?.Name ?? "Unknown"
            };

            LogOperation("Retrieved current user: {UserId}", user.Id);
            isSuccess = true;
            
            LogUserActivityAsync("View", "User", user.Id, $"Retrieved current user {user.Name}", 
                new { UserId = user.Id, UserName = user.Name, Role = user.UserRole?.Name }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving current user");
            LogUserActivityAsync("View", "User", null, "Error retrieving current user", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving user information");
        }
    }

    /// <summary>
    /// Reactivates a deactivated user (Admin only)
    /// </summary>
    /// <param name="id">The ID of the user to reactivate</param>
    /// <returns>The reactivated user</returns>
    [HttpPut("{id}/reactivate")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<User>> ReactivateUser(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Reactivating user with ID: {Id}", id);
            
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                LogWarning("User with ID {Id} not found for reactivation", id);
                LogUserActivityAsync("Reactivate", "User", id, "Failed to reactivate user - User not found", 
                    null, false, "User not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"User with ID {id} not found");
            }
            
            if (user.IsActive)
            {
                LogWarning("User with ID {Id} is already active", id);
                LogUserActivityAsync("Reactivate", "User", id, "Failed to reactivate user - Already active", 
                    null, false, "User is already active", stopwatch.ElapsedMilliseconds);
                return BadRequest("User is already active");
            }
            
            var previousInactiveDate = user.InactiveDate;
            
            // Reactivate user
            user.IsActive = true;
            user.InactiveDate = null;
            
            await _context.SaveChangesAsync();
            
            LogOperation("Successfully reactivated user with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Reactivate", "User", id, $"Reactivated user {user.Name}", 
                new { 
                    UserId = id, 
                    UserName = user.Name, 
                    Email = user.Email,
                    PreviousInactiveDate = previousInactiveDate,
                    PreviousStatus = "Inactive"
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(user);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error reactivating user with ID: {Id}", id);
            LogUserActivityAsync("Reactivate", "User", id, "Error reactivating user", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while reactivating the user");
        }
    }

    /// <summary>
    /// Health check endpoint for debugging
    /// </summary>
    /// <returns>Database connection status</returns>
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<ActionResult> HealthCheck()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Performing health check");
            
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            var userCount = await _context.Users.CountAsync();
            
            var healthStatus = new
            {
                DatabaseConnected = canConnect,
                UserCount = userCount,
                Timestamp = DateTime.UtcNow
            };
            
            LogOperation("Health check completed. Database connected: {Connected}, User count: {Count}", 
                canConnect, userCount);
            isSuccess = true;
            
            LogUserActivityAsync("HealthCheck", "System", null, "Health check completed successfully", 
                new { DatabaseConnected = canConnect, UserCount = userCount }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Health check failed");
            LogUserActivityAsync("HealthCheck", "System", null, "Health check failed", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new { Error = "Health check failed", Details = ex.Message });
        }
    }

    // Login endpoint
    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AadharNumber { get; set; } = string.Empty;
        public int UserRoleId { get; set; }
        public DateTime? JoiningDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Sending test email to: {Email}", request.Email);
            
            var emailSent = await _emailService.SendUserWelcomeEmailAsync(request.Email, request.Name);
            if (emailSent)
            {
                LogOperation("Test email sent successfully to: {Email}", request.Email);
                isSuccess = true;
                
                LogUserActivityAsync("TestEmail", "Email", null, $"Test email sent successfully to {request.Email}", 
                    new { Email = request.Email, Name = request.Name }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { message = "Test email sent successfully", email = request.Email });
            }
            else
            {
                LogWarning("Failed to send test email to: {Email}", request.Email);
                LogUserActivityAsync("TestEmail", "Email", null, "Failed to send test email", 
                    new { Email = request.Email, Name = request.Name }, false, "Email service failed", stopwatch.ElapsedMilliseconds);
                
                return BadRequest(new { message = "Failed to send test email", email = request.Email });
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error sending test email to {Email}", request.Email);
            LogUserActivityAsync("TestEmail", "Email", null, "Error sending test email", 
                new { Email = request.Email, Name = request.Name }, false, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return StatusCode(500, new { message = "Error sending test email", error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Login attempt for user: {Username}", request.Username);
            
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                LogWarning("Invalid login attempt: Username or password is empty");
                LogUserActivityAsync("Login", "User", null, "Failed to login - Invalid credentials", 
                    new { Username = request.Username }, false, "Username and password are required", stopwatch.ElapsedMilliseconds);
                return BadRequest("Username and password are required");
            }

            var userLogin = await _context.UserLogins
                .Include(ul => ul.User)
                .ThenInclude(u => u.UserRole)
                .FirstOrDefaultAsync(ul => ul.Username == request.Username && ul.Password == request.Password);

            if (userLogin == null)
            {
                LogWarning("Login failed: User not found for username: {Username}", request.Username);
                LogUserActivityAsync("Login", "User", null, "Failed to login - User not found", 
                    new { Username = request.Username }, false, "Invalid username or password", stopwatch.ElapsedMilliseconds);
                return Unauthorized("Invalid username or password");
            }

            // Check if user is active
            if (userLogin.User == null || !userLogin.User.IsActive)
            {
                LogWarning("Login failed: Account is deactivated for username: {Username}", request.Username);
                LogUserActivityAsync("Login", "User", userLogin.User?.Id, "Failed to login - Account deactivated", 
                    new { Username = request.Username, UserId = userLogin.User?.Id }, false, "Account is deactivated", stopwatch.ElapsedMilliseconds);
                return Unauthorized("Account is deactivated. Please contact administrator.");
            }

            // Generate JWT token with role
            var token = GenerateJwtToken(userLogin.User);
            
            LogOperation("Successful login for user: {Username} (ID: {UserId})", request.Username, userLogin.User.Id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Login", "User", userLogin.User.Id, $"Successful login for {userLogin.User.Name}", 
                new { 
                    UserId = userLogin.User.Id, 
                    Username = request.Username, 
                    UserName = userLogin.User.Name,
                    Role = userLogin.User.UserRole?.Name,
                    LoginTime = DateTime.UtcNow
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(new { 
                token,
                user = new {
                    id = userLogin.User.Id,
                    name = userLogin.User.Name,
                    email = userLogin.User.Email,
                    role = userLogin.User.UserRole?.Name ?? "Member"
                }
            });
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error during login for username: {Username}", request.Username);
            LogUserActivityAsync("Login", "User", null, "Error during login", 
                new { Username = request.Username }, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred during login");
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiresInMinutes"]!));

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.UserRole?.Name ?? "Member")
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
    
} 

