using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;
using phoenix_sangam_api.Services;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingPaymentController : BaseController
{
    public MeetingPaymentController(UserDbContext context, ILogger<MeetingPaymentController> logger, IUserActivityService userActivityService, IServiceProvider serviceProvider)
        : base(context, logger, userActivityService, serviceProvider)
    {
    }

    /// <summary>
    /// Gets all meeting payments
    /// </summary>
    /// <returns>List of all meeting payments</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetingPaymentResponseDto>>> GetAllMeetingPayments()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting all meeting payments");
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
                    Location = p.Meeting.Location,
                    MeetingMinutes = p.Meeting.MeetingMinutes
                } : null
            }).ToList();
            
            LogOperation("Retrieved {Count} meeting payments", responseDtos.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "MeetingPayment", null, $"Retrieved {responseDtos.Count} meeting payments", 
                new { Count = responseDtos.Count, TotalMainPayment = responseDtos.Sum(p => p.MainPayment), TotalWeeklyPayment = responseDtos.Sum(p => p.WeeklyPayment) }, 
                isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meeting payments");
            LogUserActivityAsync("View", "MeetingPayment", null, "Error retrieving meeting payments", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving meeting payments");
        }
    }

    /// <summary>
    /// Gets payment summary by meeting ID
    /// </summary>
    /// <param name="meetingId">The ID of the meeting to get payment summary for</param>
    /// <returns>Payment summary for the specified meeting</returns>
    [HttpGet("meeting/{meetingId}")]
    public async Task<ActionResult<MeetingPaymentSummaryDto>> GetMeetingPaymentsByMeeting(int meetingId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting payment summary for meeting ID: {MeetingId}", meetingId);
            
            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", meetingId);
                LogUserActivityAsync("View", "MeetingPayment", null, "Failed to get payment summary - Meeting not found", 
                    new { MeetingId = meetingId }, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {meetingId} not found");
            }
            
            var payments = await _context.MeetingPayments
                .Include(p => p.User)
                .Where(p => p.MeetingId == meetingId)
                .ToListAsync();
            
            var users = payments.Select(p => new MeetingPaymentUserDto
            {
                Id = p.UserId,
                Name = p.User?.Name ?? "Unknown User",
                MainPayment = p.MainPayment,
                WeeklyPayment = p.WeeklyPayment
            }).ToList();
            
            var response = new MeetingPaymentSummaryDto
            {
                MeetingId = meetingId,
                Users = users
            };
            
            LogOperation("Retrieved payment summary for meeting ID: {MeetingId} with {Count} users", meetingId, users.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "MeetingPayment", null, $"Retrieved payment summary for meeting {meeting.Description}", 
                new { 
                    MeetingId = meetingId, 
                    MeetingDescription = meeting.Description,
                    UserCount = users.Count,
                    TotalMainPayment = users.Sum(u => u.MainPayment),
                    TotalWeeklyPayment = users.Sum(u => u.WeeklyPayment)
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving payment summary for meeting ID: {MeetingId}", meetingId);
            LogUserActivityAsync("View", "MeetingPayment", null, "Error retrieving payment summary", 
                new { MeetingId = meetingId }, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving payment summary for the meeting");
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
                    Location = payment.Meeting.Location,
                    MeetingMinutes = payment.Meeting.MeetingMinutes
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
                _logger.LogInformation("Updating existing payment for User ID: {UserId}, Meeting ID: {MeetingId}", 
                    meetingPayment.UserId, meetingPayment.MeetingId);
                // Update existing payment
                existingPayment.MainPayment = meetingPayment.MainPayment;
                existingPayment.WeeklyPayment = meetingPayment.WeeklyPayment;
                await _context.SaveChangesAsync();
                
                // Return the updated payment with response DTO
                var updatedResponseDto = new MeetingPaymentResponseDto
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
                        Location = meeting.Location,
                        MeetingMinutes = meeting.MeetingMinutes
                    }
                };
                
                _logger.LogInformation("Successfully updated payment with ID: {Id}", existingPayment.Id);
                return Ok(updatedResponseDto);
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
                    Location = meeting.Location,
                    MeetingMinutes = meeting.MeetingMinutes
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
    /// Creates or replaces meeting payment records for a meeting
    /// If existing payment records exist for the meeting, they will be deleted and replaced with the new list
    /// </summary>
    /// <param name="bulkPaymentDto">The bulk payment data</param>
    /// <returns>List of created payment records</returns>
    [HttpPost("bulk")]
    public async Task<ActionResult<List<MeetingPaymentResponseDto>>> CreateBulkMeetingPayment([FromBody] BulkMeetingPaymentDto bulkPaymentDto)
    {
        try
        {
            _logger.LogInformation("Creating bulk meeting payment for Meeting ID: {MeetingId} with {Count} records", 
                bulkPaymentDto.MeetingId, bulkPaymentDto.Payments.Count);
            
            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(bulkPaymentDto.MeetingId);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {MeetingId} not found", bulkPaymentDto.MeetingId);
                return BadRequest($"Meeting with ID {bulkPaymentDto.MeetingId} not found");
            }

            // Get all user IDs from the request
            var userIds = bulkPaymentDto.Payments.Select(p => p.UserId).ToList();
            
            // Check if all users exist
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            if (users.Count != userIds.Count)
            {
                var existingUserIds = users.Select(u => u.Id).ToList();
                var missingUserIds = userIds.Except(existingUserIds).ToList();
                _logger.LogWarning("Some users not found: {MissingUserIds}", string.Join(", ", missingUserIds));
                return BadRequest($"Users with IDs {string.Join(", ", missingUserIds)} not found");
            }

            // Validate payment amounts
            foreach (var payment in bulkPaymentDto.Payments)
            {
                if (payment.MainPayment < 0)
                {
                    _logger.LogWarning("Invalid main payment amount: {MainPayment} for User ID: {UserId}", 
                        payment.MainPayment, payment.UserId);
                    return BadRequest($"Main payment amount cannot be negative for User ID: {payment.UserId}");
                }

                if (payment.WeeklyPayment < 0)
                {
                    _logger.LogWarning("Invalid weekly payment amount: {WeeklyPayment} for User ID: {UserId}", 
                        payment.WeeklyPayment, payment.UserId);
                    return BadRequest($"Weekly payment amount cannot be negative for User ID: {payment.UserId}");
                }
            }

            // Delete existing payment records for this meeting
            var existingPayments = await _context.MeetingPayments
                .Where(p => p.MeetingId == bulkPaymentDto.MeetingId)
                .ToListAsync();
            
            if (existingPayments.Any())
            {
                _logger.LogInformation("Deleting {Count} existing payment records for Meeting ID: {MeetingId}", 
                    existingPayments.Count, bulkPaymentDto.MeetingId);
                _context.MeetingPayments.RemoveRange(existingPayments);
            }

            // Create new payment records
            var newPayments = bulkPaymentDto.Payments.Select(p => new MeetingPayment
            {
                UserId = p.UserId,
                MeetingId = bulkPaymentDto.MeetingId,
                MainPayment = p.MainPayment,
                WeeklyPayment = p.WeeklyPayment
            }).ToList();

            _context.MeetingPayments.AddRange(newPayments);
            await _context.SaveChangesAsync();
            
            // Create response DTOs
            var responseDtos = newPayments.Select(p => new MeetingPaymentResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                MeetingId = p.MeetingId,
                MainPayment = p.MainPayment,
                WeeklyPayment = p.WeeklyPayment,
                CreatedAt = p.CreatedAt,
                User = users.FirstOrDefault(u => u.Id == p.UserId) != null ? new UserResponseDto
                {
                    Id = users.First(u => u.Id == p.UserId).Id,
                    Name = users.First(u => u.Id == p.UserId).Name,
                    Address = users.First(u => u.Id == p.UserId).Address,
                    Email = users.First(u => u.Id == p.UserId).Email,
                    Phone = users.First(u => u.Id == p.UserId).Phone
                } : null,
                Meeting = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Description = meeting.Description,
                    Location = meeting.Location,
                    MeetingMinutes = meeting.MeetingMinutes
                }
            }).ToList();
            
            _logger.LogInformation("Successfully created {Count} payment records for Meeting ID: {MeetingId}", 
                responseDtos.Count, bulkPaymentDto.MeetingId);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk meeting payment for Meeting ID: {MeetingId}", bulkPaymentDto.MeetingId);
            return StatusCode(500, "An error occurred while creating the bulk meeting payment");
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
                    Location = meeting.Location,
                    MeetingMinutes = meeting.MeetingMinutes
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
