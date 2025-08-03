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
public class AttendanceController : BaseController
{
    public AttendanceController(UserDbContext context, ILogger<AttendanceController> logger, IUserActivityService userActivityService, IServiceProvider serviceProvider)
        : base(context, logger, userActivityService, serviceProvider)
    {
    }

    /// <summary>
    /// Gets all attendances
    /// </summary>
    /// <returns>List of all attendances</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetAllAttendances()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting all attendances");
            var attendances = await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Meeting)
                .ToListAsync();
            
            var responseDtos = attendances.Select(a => new AttendanceResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                MeetingId = a.MeetingId,
                IsPresent = a.IsPresent,
                CreatedAt = a.CreatedAt,
                User = a.User != null ? new UserResponseDto
                {
                    Id = a.User.Id,
                    Name = a.User.Name,
                    Address = a.User.Address,
                    Email = a.User.Email,
                    Phone = a.User.Phone
                } : null,
                Meeting = a.Meeting != null ? new MeetingResponseDto
                {
                    Id = a.Meeting.Id,
                    Date = a.Meeting.Date,
                    Time = a.Meeting.Time,
                    Description = a.Meeting.Description,
                    Location = a.Meeting.Location,
                    MeetingMinutes = a.Meeting.MeetingMinutes
                } : null
            }).ToList();
            
            LogOperation("Retrieved {Count} attendances", responseDtos.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Attendance", null, $"Retrieved {responseDtos.Count} attendances", 
                new { Count = responseDtos.Count, PresentCount = responseDtos.Count(a => a.IsPresent), AbsentCount = responseDtos.Count(a => !a.IsPresent) }, 
                isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving attendances");
            LogUserActivityAsync("View", "Attendance", null, "Error retrieving attendances", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving attendances");
        }
    }

    /// <summary>
    /// Gets attendance summary by meeting ID including both attended and absent users
    /// </summary>
    /// <param name="meetingId">The ID of the meeting to get attendance summary for</param>
    /// <returns>Meeting attendance summary with attended and absent users</returns>
    [HttpGet("meeting/{meetingId}/summary")]
    public async Task<ActionResult<MeetingAttendanceSummaryDto>> GetAttendanceSummaryByMeeting(int meetingId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting attendance summary for meeting ID: {MeetingId}", meetingId);
            
            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", meetingId);
                LogUserActivityAsync("View", "AttendanceSummary", meetingId, "Failed to get attendance summary - Meeting not found", 
                    new { MeetingId = meetingId }, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {meetingId} not found");
            }
            
            // Get all users and filter based on joining/inactive dates relative to meeting date
            var meetingDate = meeting.Date.Date; // Use only the date part for comparison
            
            var eligibleUsers = await GetEligibleUsersForMeetingDate(meetingDate);
            
            _logger.LogInformation("Found {EligibleCount} eligible users for meeting on {MeetingDate} out of {TotalUsers} total users", 
                eligibleUsers.Count, meetingDate, await _context.Users.CountAsync());
            
            // Get attendance records for this meeting
            var attendances = await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.MeetingId == meetingId)
                .ToListAsync();
            
            // Create a dictionary of user attendance status
            var userAttendanceStatus = attendances.ToDictionary(a => a.UserId, a => a.IsPresent);
            
            // Separate users into attended and absent lists
            var attendedUsers = new List<UserResponseDto>();
            var absentUsers = new List<UserResponseDto>();
            
            foreach (var user in eligibleUsers)
            {
                var userDto = new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Address = user.Address,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsActive = user.IsActive,
                    JoiningDate = user.JoiningDate,
                    InactiveDate = user.InactiveDate
                };
                
                // Check if user has attendance record for this meeting
                if (userAttendanceStatus.TryGetValue(user.Id, out bool isPresent))
                {
                    if (isPresent)
                    {
                        attendedUsers.Add(userDto);
                    }
                    else
                    {
                        absentUsers.Add(userDto);
                    }
                }
                else
                {
                    // User has no attendance record, consider as absent
                    absentUsers.Add(userDto);
                }
            }
            
            // Create the response
            var response = new MeetingAttendanceSummaryDto
            {
                MeetingId = meetingId,
                Meeting = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Description = meeting.Description,
                    Location = meeting.Location,
                    MeetingMinutes = meeting.MeetingMinutes
                },
                AttendedUsers = attendedUsers,
                AbsentUsers = absentUsers,
                TotalUsers = eligibleUsers.Count,
                AttendedCount = attendedUsers.Count,
                AbsentCount = absentUsers.Count,
                AttendancePercentage = eligibleUsers.Count > 0 ? Math.Round((double)attendedUsers.Count / eligibleUsers.Count * 100, 2) : 0
            };
            
            LogOperation("Retrieved attendance summary for meeting ID: {MeetingId}. Total: {Total}, Attended: {Attended}, Absent: {Absent}", 
                meetingId, response.TotalUsers, response.AttendedCount, response.AbsentCount);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("View", "AttendanceSummary", meetingId, $"Retrieved attendance summary for meeting {meeting.Description}", 
                new { 
                    MeetingId = meetingId,
                    MeetingDescription = meeting.Description,
                    TotalUsers = response.TotalUsers,
                    AttendedCount = response.AttendedCount,
                    AbsentCount = response.AbsentCount,
                    AttendancePercentage = response.AttendancePercentage
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving attendance summary for meeting ID: {MeetingId}", meetingId);
            LogUserActivityAsync("View", "AttendanceSummary", meetingId, "Error retrieving attendance summary", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving attendance summary for the meeting");
        }
    }

    /// <summary>
    /// Gets detailed attendance records by meeting ID (original endpoint for backward compatibility)
    /// </summary>
    /// <param name="meetingId">The ID of the meeting to get attendance details for</param>
    /// <returns>List of detailed attendance records for the specified meeting</returns>
    [HttpGet("meeting/{meetingId}")]
    public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetAttendancesByMeeting(int meetingId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting detailed attendances for meeting ID: {MeetingId}", meetingId);
            
            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", meetingId);
                LogUserActivityAsync("View", "Attendance", null, "Failed to get attendances - Meeting not found", 
                    new { MeetingId = meetingId }, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {meetingId} not found");
            }
            
            var attendances = await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Meeting)
                .Where(a => a.MeetingId == meetingId)
                .ToListAsync();
            
            var responseDtos = attendances.Select(a => new AttendanceResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                MeetingId = a.MeetingId,
                IsPresent = a.IsPresent,
                CreatedAt = a.CreatedAt,
                User = a.User != null ? new UserResponseDto
                {
                    Id = a.User.Id,
                    Name = a.User.Name,
                    Address = a.User.Address,
                    Email = a.User.Email,
                    Phone = a.User.Phone
                } : null,
                Meeting = a.Meeting != null ? new MeetingResponseDto
                {
                    Id = a.Meeting.Id,
                    Date = a.Meeting.Date,
                    Time = a.Meeting.Time,
                    Description = a.Meeting.Description,
                    Location = a.Meeting.Location,
                    MeetingMinutes = a.Meeting.MeetingMinutes
                } : null
            }).ToList();
            
            LogOperation("Retrieved {Count} detailed attendance records for meeting ID: {MeetingId}", responseDtos.Count, meetingId);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Attendance", null, $"Retrieved {responseDtos.Count} attendance records for meeting {meeting.Description}", 
                new { MeetingId = meetingId, MeetingDescription = meeting.Description, Count = responseDtos.Count, PresentCount = responseDtos.Count(a => a.IsPresent), AbsentCount = responseDtos.Count(a => !a.IsPresent) }, 
                isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving detailed attendances for meeting ID: {MeetingId}", meetingId);
            LogUserActivityAsync("View", "Attendance", null, "Error retrieving detailed attendances", 
                new { MeetingId = meetingId }, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving detailed attendances for the meeting");
        }
    }

    /// <summary>
    /// Gets a specific attendance by ID
    /// </summary>
    /// <param name="id">The ID of the attendance to retrieve</param>
    /// <returns>The attendance if found, otherwise NotFound</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<AttendanceResponseDto>> GetAttendance(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting attendance with ID: {Id}", id);
            
            var attendance = await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Meeting)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (attendance == null)
            {
                LogWarning("Attendance with ID {Id} not found", id);
                LogUserActivityAsync("View", "Attendance", id, "Failed to retrieve attendance - Attendance not found", 
                    null, false, "Attendance not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Attendance with ID {id} not found");
            }
            
            var responseDto = new AttendanceResponseDto
            {
                Id = attendance.Id,
                UserId = attendance.UserId,
                MeetingId = attendance.MeetingId,
                IsPresent = attendance.IsPresent,
                CreatedAt = attendance.CreatedAt,
                User = attendance.User != null ? new UserResponseDto
                {
                    Id = attendance.User.Id,
                    Name = attendance.User.Name,
                    Address = attendance.User.Address,
                    Email = attendance.User.Email,
                    Phone = attendance.User.Phone
                } : null,
                Meeting = attendance.Meeting != null ? new MeetingResponseDto
                {
                    Id = attendance.Meeting.Id,
                    Date = attendance.Meeting.Date,
                    Time = attendance.Meeting.Time,
                    Description = attendance.Meeting.Description,
                    Location = attendance.Meeting.Location
                } : null
            };
            
            LogOperation("Successfully retrieved attendance with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Attendance", id, $"Retrieved attendance for user {attendance.User?.Name} in meeting {attendance.Meeting?.Description}", 
                new { 
                    AttendanceId = id, 
                    UserId = attendance.UserId, 
                    UserName = attendance.User?.Name,
                    MeetingId = attendance.MeetingId,
                    MeetingDescription = attendance.Meeting?.Description,
                    IsPresent = attendance.IsPresent
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving attendance with ID: {Id}", id);
            LogUserActivityAsync("View", "Attendance", id, "Error retrieving attendance", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving the attendance");
        }
    }

    /// <summary>
    /// Creates a new attendance
    /// </summary>
    /// <param name="attendanceDto">The attendance data to create</param>
    /// <returns>The created attendance with assigned ID</returns>
    [HttpPost]
    public async Task<ActionResult<AttendanceResponseDto>> CreateAttendance([FromBody] CreateAttendanceDto attendanceDto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Creating new attendance for User ID: {UserId}, Meeting ID: {MeetingId}", 
                attendanceDto.UserId, attendanceDto.MeetingId);
            
            // Check if user exists
            var user = await _context.Users.FindAsync(attendanceDto.UserId);
            if (user == null)
            {
                LogWarning("User with ID {UserId} not found", attendanceDto.UserId);
                LogUserActivityAsync("Create", "Attendance", null, "Failed to create attendance - User not found", 
                    attendanceDto, false, "User not found", stopwatch.ElapsedMilliseconds);
                return BadRequest($"User with ID {attendanceDto.UserId} not found");
            }

            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(attendanceDto.MeetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", attendanceDto.MeetingId);
                LogUserActivityAsync("Create", "Attendance", null, "Failed to create attendance - Meeting not found", 
                    attendanceDto, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return BadRequest($"Meeting with ID {attendanceDto.MeetingId} not found");
            }

            // Check if attendance already exists for this user and meeting
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == attendanceDto.UserId && a.MeetingId == attendanceDto.MeetingId);
                
            if (existingAttendance != null)
            {
                _logger.LogInformation("Updating existing attendance for User ID: {UserId}, Meeting ID: {MeetingId}", 
                    attendanceDto.UserId, attendanceDto.MeetingId);
                // Update existing attendance
                existingAttendance.IsPresent = attendanceDto.IsPresent;
                await _context.SaveChangesAsync();
                
                // Return the updated attendance with response DTO
                var updatedResponseDto = new AttendanceResponseDto
                {
                    Id = existingAttendance.Id,
                    UserId = existingAttendance.UserId,
                    MeetingId = existingAttendance.MeetingId,
                    IsPresent = existingAttendance.IsPresent,
                    CreatedAt = existingAttendance.CreatedAt,
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
                
                _logger.LogInformation("Successfully updated attendance with ID: {Id}", existingAttendance.Id);
                return Ok(updatedResponseDto);
            }

            var attendance = new Attendance
            {
                UserId = attendanceDto.UserId,
                MeetingId = attendanceDto.MeetingId,
                IsPresent = attendanceDto.IsPresent
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            
            // Return the created attendance with response DTO
            var responseDto = new AttendanceResponseDto
            {
                Id = attendance.Id,
                UserId = attendance.UserId,
                MeetingId = attendance.MeetingId,
                IsPresent = attendance.IsPresent,
                CreatedAt = attendance.CreatedAt,
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
            
            LogOperation("Successfully created attendance with ID: {Id}", attendance.Id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Create", "Attendance", attendance.Id, $"Created attendance for user {user.Name} in meeting {meeting.Description}", 
                new { 
                    AttendanceId = attendance.Id,
                    UserId = attendance.UserId,
                    UserName = user.Name,
                    MeetingId = attendance.MeetingId,
                    MeetingDescription = meeting.Description,
                    IsPresent = attendance.IsPresent,
                    Action = "Created"
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error creating attendance for User ID: {UserId}, Meeting ID: {MeetingId}", 
                attendanceDto.UserId, attendanceDto.MeetingId);
            LogUserActivityAsync("Create", "Attendance", null, "Error creating attendance", 
                attendanceDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while creating the attendance");
        }
    }

    /// <summary>
    /// Creates or replaces attendance records for a meeting
    /// If existing records exist for the meeting, they will be deleted and replaced with the new list
    /// </summary>
    /// <param name="bulkAttendanceDto">The bulk attendance data</param>
    /// <returns>List of created attendance records</returns>
    [HttpPost("bulk")]
    public async Task<ActionResult<List<AttendanceResponseDto>>> CreateBulkAttendance([FromBody] BulkAttendanceDto bulkAttendanceDto)
    {
        try
        {
            _logger.LogInformation("Creating bulk attendance for Meeting ID: {MeetingId} with {Count} records", 
                bulkAttendanceDto.MeetingId, bulkAttendanceDto.Attendances.Count);
            
            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(bulkAttendanceDto.MeetingId);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {MeetingId} not found", bulkAttendanceDto.MeetingId);
                return BadRequest($"Meeting with ID {bulkAttendanceDto.MeetingId} not found");
            }

            // Get all user IDs from the request
            var userIds = bulkAttendanceDto.Attendances.Select(a => a.UserId).ToList();
            
            // Check if all users exist
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            if (users.Count != userIds.Count)
            {
                var existingUserIds = users.Select(u => u.Id).ToList();
                var missingUserIds = userIds.Except(existingUserIds).ToList();
                _logger.LogWarning("Some users not found: {MissingUserIds}", string.Join(", ", missingUserIds));
                return BadRequest($"Users with IDs {string.Join(", ", missingUserIds)} not found");
            }

            // Delete existing attendance records for this meeting
            var existingAttendances = await _context.Attendances
                .Where(a => a.MeetingId == bulkAttendanceDto.MeetingId)
                .ToListAsync();
            
            if (existingAttendances.Any())
            {
                _logger.LogInformation("Deleting {Count} existing attendance records for Meeting ID: {MeetingId}", 
                    existingAttendances.Count, bulkAttendanceDto.MeetingId);
                _context.Attendances.RemoveRange(existingAttendances);
            }

            // Create new attendance records
            var newAttendances = bulkAttendanceDto.Attendances.Select(a => new Attendance
            {
                UserId = a.UserId,
                MeetingId = bulkAttendanceDto.MeetingId,
                IsPresent = a.IsPresent
            }).ToList();

            _context.Attendances.AddRange(newAttendances);
            await _context.SaveChangesAsync();
            
            // Create response DTOs
            var responseDtos = newAttendances.Select(a => new AttendanceResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                MeetingId = a.MeetingId,
                IsPresent = a.IsPresent,
                CreatedAt = a.CreatedAt,
                User = users.FirstOrDefault(u => u.Id == a.UserId) != null ? new UserResponseDto
                {
                    Id = users.First(u => u.Id == a.UserId).Id,
                    Name = users.First(u => u.Id == a.UserId).Name,
                    Address = users.First(u => u.Id == a.UserId).Address,
                    Email = users.First(u => u.Id == a.UserId).Email,
                    Phone = users.First(u => u.Id == a.UserId).Phone
                } : null,
                Meeting = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Description = meeting.Description,
                    Location = meeting.Location
                }
            }).ToList();
            
            _logger.LogInformation("Successfully created {Count} attendance records for Meeting ID: {MeetingId}", 
                responseDtos.Count, bulkAttendanceDto.MeetingId);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk attendance for Meeting ID: {MeetingId}", bulkAttendanceDto.MeetingId);
            return StatusCode(500, "An error occurred while creating the bulk attendance");
        }
    }

    /// <summary>
    /// Updates an existing attendance
    /// </summary>
    /// <param name="id">The ID of the attendance to update</param>
    /// <param name="attendanceDto">The updated attendance data</param>
    /// <returns>The updated attendance</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<AttendanceResponseDto>> UpdateAttendance(int id, [FromBody] UpdateAttendanceDto attendanceDto)
    {
        try
        {
            _logger.LogInformation("Updating attendance with ID: {Id}", id);
            
            var existingAttendance = await _context.Attendances.FindAsync(id);
            if (existingAttendance == null)
            {
                _logger.LogWarning("Attendance with ID {Id} not found for update", id);
                return NotFound($"Attendance with ID {id} not found");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(attendanceDto.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", attendanceDto.UserId);
                return BadRequest($"User with ID {attendanceDto.UserId} not found");
            }

            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(attendanceDto.MeetingId);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {MeetingId} not found", attendanceDto.MeetingId);
                return BadRequest($"Meeting with ID {attendanceDto.MeetingId} not found");
            }

            existingAttendance.UserId = attendanceDto.UserId;
            existingAttendance.MeetingId = attendanceDto.MeetingId;
            existingAttendance.IsPresent = attendanceDto.IsPresent;
            
            await _context.SaveChangesAsync();
            
            // Return the updated attendance with response DTO
            var responseDto = new AttendanceResponseDto
            {
                Id = existingAttendance.Id,
                UserId = existingAttendance.UserId,
                MeetingId = existingAttendance.MeetingId,
                IsPresent = existingAttendance.IsPresent,
                CreatedAt = existingAttendance.CreatedAt,
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
            
            _logger.LogInformation("Successfully updated attendance with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the attendance");
        }
    }

    /// <summary>
    /// Deletes an attendance
    /// </summary>
    /// <param name="id">The ID of the attendance to delete</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAttendance(int id)
    {
        try
        {
            _logger.LogInformation("Deleting attendance with ID: {Id}", id);
            
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                _logger.LogWarning("Attendance with ID {Id} not found for deletion", id);
                return NotFound($"Attendance with ID {id} not found");
            }
            
            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully deleted attendance with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the attendance");
        }
    }
} 
