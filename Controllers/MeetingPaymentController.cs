using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingPaymentController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<MeetingPaymentController> _logger;

    public MeetingPaymentController(UserDbContext context, ILogger<MeetingPaymentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all meeting payments
    /// </summary>
    /// <returns>List of all meeting payments</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetingPaymentResponseDto>>> GetAllMeetingPayments()
    {
        try
        {
            _logger.LogInformation("Getting all meeting payments");
            var payments = await _context.MeetingPayments
                .Include(p => p.User)
                .Include(p => p.Meeting)
                .ToListAsync();
            
            var responseDtos = payments.Select(p => new MeetingPaymentResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                MeetingId = p.MeetingId,
                MainPayment = p.MainPayment,
                WeeklyPayment = p.WeeklyPayment,
                CreatedAt = p.CreatedAt,
                User = p.User != null ? new UserResponseDto
                {
                    Id = p.User.Id,
                    Name = p.User.Name,
                    Address = p.User.Address,
                    Email = p.User.Email,
                    Phone = p.User.Phone
                } : null,
                Meeting = p.Meeting != null ? new MeetingResponseDto
                {
                    Id = p.Meeting.Id,
                    Date = p.Meeting.Date,
                    Time = p.Meeting.Time,
                    Description = p.Meeting.Description,
                    Location = p.Meeting.Location
                } : null
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} meeting payments", responseDtos.Count);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting payments");
            return StatusCode(500, "An error occurred while retrieving meeting payments");
        }
    }

    /// <summary>
    /// Gets a specific meeting payment by ID
    /// </summary>
    /// <param name="id">The ID of the meeting payment to retrieve</param>
    /// <returns>The meeting payment if found, otherwise NotFound</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<MeetingPaymentResponseDto>> GetMeetingPayment(int id)
    {
        try
        {
            _logger.LogInformation("Getting meeting payment with ID: {Id}", id);
            
            var payment = await _context.MeetingPayments
                .Include(p => p.User)
                .Include(p => p.Meeting)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (payment == null)
            {
                _logger.LogWarning("Meeting payment with ID {Id} not found", id);
                return NotFound($"Meeting payment with ID {id} not found");
            }
            
            var responseDto = new MeetingPaymentResponseDto
            {
                Id = payment.Id,
                UserId = payment.UserId,
                MeetingId = payment.MeetingId,
                MainPayment = payment.MainPayment,
                WeeklyPayment = payment.WeeklyPayment,
                CreatedAt = payment.CreatedAt,
                User = payment.User != null ? new UserResponseDto
                {
                    Id = payment.User.Id,
                    Name = payment.User.Name,
                    Address = payment.User.Address,
                    Email = payment.User.Email,
                    Phone = payment.User.Phone
                } : null,
                Meeting = payment.Meeting != null ? new MeetingResponseDto
                {
                    Id = payment.Meeting.Id,
                    Date = payment.Meeting.Date,
                    Time = payment.Meeting.Time,
                    Description = payment.Meeting.Description,
                    Location = payment.Meeting.Location
                } : null
            };
            
            _logger.LogInformation("Successfully retrieved meeting payment with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting payment with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the meeting payment");
        }
    }

    /// <summary>
    /// Creates a new meeting payment
    /// </summary>
    /// <param name="meetingPayment">The meeting payment data to create</param>
    /// <returns>The created meeting payment with assigned ID</returns>
    [HttpPost]
    public async Task<ActionResult<MeetingPaymentResponseDto>> CreateMeetingPayment([FromBody] MeetingPayment meetingPayment)
    {
        try
        {
            _logger.LogInformation("Creating new meeting payment for User ID: {UserId}, Meeting ID: {MeetingId}", 
                meetingPayment.UserId, meetingPayment.MeetingId);
            
            // Check if user exists
            var user = await _context.Users.FindAsync(meetingPayment.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", meetingPayment.UserId);
                return BadRequest($"User with ID {meetingPayment.UserId} not found");
            }

            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(meetingPayment.MeetingId);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {MeetingId} not found", meetingPayment.MeetingId);
                return BadRequest($"Meeting with ID {meetingPayment.MeetingId} not found");
            }

            // Check if payment already exists for this user and meeting
            var existingPayment = await _context.MeetingPayments
                .FirstOrDefaultAsync(p => p.UserId == meetingPayment.UserId && p.MeetingId == meetingPayment.MeetingId);
                
            if (existingPayment != null)
            {
                _logger.LogWarning("Payment already exists for User ID: {UserId}, Meeting ID: {MeetingId}", 
                    meetingPayment.UserId, meetingPayment.MeetingId);
                return BadRequest("Payment already exists for this user and meeting");
            }

            // Validate payment amounts
            if (meetingPayment.MainPayment < 0)
            {
                _logger.LogWarning("Invalid main payment amount: {MainPayment}", meetingPayment.MainPayment);
                return BadRequest("Main payment amount cannot be negative");
            }

            if (meetingPayment.WeeklyPayment < 0)
            {
                _logger.LogWarning("Invalid weekly payment amount: {WeeklyPayment}", meetingPayment.WeeklyPayment);
                return BadRequest("Weekly payment amount cannot be negative");
            }

            _context.MeetingPayments.Add(meetingPayment);
            await _context.SaveChangesAsync();
            
            // Return the created payment with response DTO
            var responseDto = new MeetingPaymentResponseDto
            {
                Id = meetingPayment.Id,
                UserId = meetingPayment.UserId,
                MeetingId = meetingPayment.MeetingId,
                MainPayment = meetingPayment.MainPayment,
                WeeklyPayment = meetingPayment.WeeklyPayment,
                CreatedAt = meetingPayment.CreatedAt,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Address = user.Address,
                    Email = user.Email,
                    Phone = user.Phone
                },
                Meeting = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Description = meeting.Description,
                    Location = meeting.Location
                }
            };
            
            _logger.LogInformation("Successfully created meeting payment with ID: {Id}", meetingPayment.Id);
            return CreatedAtAction(nameof(GetMeetingPayment), new { id = meetingPayment.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meeting payment for User ID: {UserId}, Meeting ID: {MeetingId}", 
                meetingPayment.UserId, meetingPayment.MeetingId);
            return StatusCode(500, "An error occurred while creating the meeting payment");
        }
    }

    /// <summary>
    /// Updates an existing meeting payment
    /// </summary>
    /// <param name="id">The ID of the meeting payment to update</param>
    /// <param name="meetingPayment">The updated meeting payment data</param>
    /// <returns>The updated meeting payment</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<MeetingPaymentResponseDto>> UpdateMeetingPayment(int id, [FromBody] MeetingPayment meetingPayment)
    {
        try
        {
            _logger.LogInformation("Updating meeting payment with ID: {Id}", id);
            
            var existingPayment = await _context.MeetingPayments.FindAsync(id);
            if (existingPayment == null)
            {
                _logger.LogWarning("Meeting payment with ID {Id} not found for update", id);
                return NotFound($"Meeting payment with ID {id} not found");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(meetingPayment.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", meetingPayment.UserId);
                return BadRequest($"User with ID {meetingPayment.UserId} not found");
            }

            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(meetingPayment.MeetingId);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {MeetingId} not found", meetingPayment.MeetingId);
                return BadRequest($"Meeting with ID {meetingPayment.MeetingId} not found");
            }

            // Validate payment amounts
            if (meetingPayment.MainPayment < 0)
            {
                _logger.LogWarning("Invalid main payment amount: {MainPayment}", meetingPayment.MainPayment);
                return BadRequest("Main payment amount cannot be negative");
            }

            if (meetingPayment.WeeklyPayment < 0)
            {
                _logger.LogWarning("Invalid weekly payment amount: {WeeklyPayment}", meetingPayment.WeeklyPayment);
                return BadRequest("Weekly payment amount cannot be negative");
            }

            existingPayment.UserId = meetingPayment.UserId;
            existingPayment.MeetingId = meetingPayment.MeetingId;
            existingPayment.MainPayment = meetingPayment.MainPayment;
            existingPayment.WeeklyPayment = meetingPayment.WeeklyPayment;
            
            await _context.SaveChangesAsync();
            
            // Return the updated payment with response DTO
            var responseDto = new MeetingPaymentResponseDto
            {
                Id = existingPayment.Id,
                UserId = existingPayment.UserId,
                MeetingId = existingPayment.MeetingId,
                MainPayment = existingPayment.MainPayment,
                WeeklyPayment = existingPayment.WeeklyPayment,
                CreatedAt = existingPayment.CreatedAt,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Address = user.Address,
                    Email = user.Email,
                    Phone = user.Phone
                },
                Meeting = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Description = meeting.Description,
                    Location = meeting.Location
                }
            };
            
            _logger.LogInformation("Successfully updated meeting payment with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meeting payment with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the meeting payment");
        }
    }

    /// <summary>
    /// Deletes a meeting payment
    /// </summary>
    /// <param name="id">The ID of the meeting payment to delete</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMeetingPayment(int id)
    {
        try
        {
            _logger.LogInformation("Deleting meeting payment with ID: {Id}", id);
            
            var payment = await _context.MeetingPayments.FindAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Meeting payment with ID {Id} not found for deletion", id);
                return NotFound($"Meeting payment with ID {id} not found");
            }
            
            _context.MeetingPayments.Remove(payment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully deleted meeting payment with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meeting payment with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the meeting payment");
        }
    }
} 