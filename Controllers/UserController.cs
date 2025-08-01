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

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly IConfiguration _configuration;

    public UserController(UserDbContext context, ILogger<UserController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets all users (Secretary only)
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Getting all users");
            var users = await _context.Users.ToListAsync();
            _logger.LogInformation("Retrieved {Count} users", users.Count);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Gets a specific user by ID (Admin only)
    /// </summary>
    /// <param name="id">The ID of the user to retrieve</param>
    /// <returns>The user if found, otherwise NotFound</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        try
        {
            _logger.LogInformation("Getting user with ID: {Id}", id);
            
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found", id);
                return NotFound($"User with ID {id} not found");
            }
            
            _logger.LogInformation("Successfully retrieved user with ID: {Id}", id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the user");
        }
    }

    /// <summary>
    /// Creates a new user (Admin only)
    /// </summary>
    /// <param name="user">The user data to create</param>
    /// <returns>The created user with assigned ID</returns>
    [HttpPost]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        try
        {
            _logger.LogInformation("Creating new user: {Name}", user.Name);
            
            if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Invalid user data: Name or Email is empty");
                return BadRequest("Name and Email are required");
            }

            // Check if email already exists - Fixed for EF Core compatibility
            var existingUser = await _context.Users
                .Where(u => u.Email.ToLower() == user.Email.ToLower())
                .FirstOrDefaultAsync();
                
            if (existingUser != null)
            {
                _logger.LogWarning("Email already exists: {Email}", user.Email);
                return BadRequest("Email already exists");
            }

            // Assign default role (User role - ID 2)
            user.UserRoleId = 2;
            
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
            
            _logger.LogInformation("Successfully created user with ID: {Id}", user.Id);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Name}", user.Name);
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
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User user)
    {
        try
        {
            _logger.LogInformation("Updating user with ID: {Id}", id);
            
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {Id} not found for update", id);
                return NotFound($"User with ID {id} not found");
            }

            if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Invalid user data for update: Name or Email is empty");
                return BadRequest("Name and Email are required");
            }

            // Check if email already exists for another user - Fixed for EF Core compatibility
            var duplicateEmail = await _context.Users
                .Where(u => u.Id != id && u.Email.ToLower() == user.Email.ToLower())
                .FirstOrDefaultAsync();
                
            if (duplicateEmail != null)
            {
                _logger.LogWarning("Email already exists for another user: {Email}", user.Email);
                return BadRequest("Email already exists");
            }

            existingUser.Name = user.Name;
            existingUser.Address = user.Address;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully updated user with ID: {Id}", id);
            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Deletes a user (Admin only)
    /// </summary>
    /// <param name="id">The ID of the user to delete</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            _logger.LogInformation("Deleting user with ID: {Id}", id);
            
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found for deletion", id);
                return NotFound($"User with ID {id} not found");
            }
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully deleted user with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the user");
        }
    }

    /// <summary>
    /// Gets current user information from JWT token
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // Get the current user's ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                _logger.LogWarning("User ID not found in token");
                return BadRequest("User ID not found in token");
            }

            // Get the current user with role information
            var user = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null)
            {
                _logger.LogWarning("Current user not found");
                return NotFound("User not found");
            }

            var response = new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.UserRole?.Name ?? "Unknown"
            };

            _logger.LogInformation("Retrieved current user: {UserId}", user.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, "An error occurred while retrieving user information");
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
        try
        {
            _logger.LogInformation("Performing health check");
            
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            var userCount = await _context.Users.CountAsync();
            
            var healthStatus = new
            {
                DatabaseConnected = canConnect,
                UserCount = userCount,
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogInformation("Health check completed. Database connected: {Connected}, User count: {Count}", 
                canConnect, userCount);
            
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { Error = "Health check failed", Details = ex.Message });
        }
    }

    // Login endpoint
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var userLogin = await _context.UserLogins
            .Include(ul => ul.User)
                .ThenInclude(u => u.UserRole)
            .FirstOrDefaultAsync(ul => ul.Username == request.Username && ul.Password == request.Password);

        if (userLogin == null)
        {
            return Unauthorized("Invalid username or password");
        }

        // Generate JWT token with role
        var token = GenerateJwtToken(userLogin.User!);
        return Ok(new { 
            token,
            user = new {
                id = userLogin.User!.Id,
                name = userLogin.User.Name,
                email = userLogin.User.Email,
                role = userLogin.User.UserRole?.Name ?? "Member"
            }
        });
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