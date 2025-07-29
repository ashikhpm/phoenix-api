using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(UserDbContext context, ILogger<AttendanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all attendances
    /// </summary>
    /// <returns>List of all attendances</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetAllAttendances()
    {
        try
        {
            _logger.LogInformation("Getting all attendances");
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
                    Location = a.Meeting.Location
                } : null
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} attendances", responseDtos.Count);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendances");
            return StatusCode(500, "An error occurred while retrieving attendances");
        }
    }

    /// <summary>
    /// Gets attendance details by meeting ID
    /// </summary>
    /// <param name="meetingId">The ID of the meeting to get attendance details for</param>
    /// <returns>List of attendances for the specified meeting</returns>
    [HttpGet("meeting/{meetingId}")]
    public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetAttendancesByMeeting(int meetingId)
    {
        try
        {
            _logger.LogInformation("Getting attendances for meeting ID: {MeetingId}", meetingId);
            
            // Check if meeting exists
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {MeetingId} not found", meetingId);
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
                    Location = a.Meeting.Location
                } : null
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} attendances for meeting ID: {MeetingId}", responseDtos.Count, meetingId);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendances for meeting ID: {MeetingId}", meetingId);
            return StatusCode(500, "An error occurred while retrieving attendances for the meeting");
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
        try
        {
            _logger.LogInformation("Getting attendance with ID: {Id}", id);
            
            var attendance = await _context.Attendances
                .Include(a => a.User)
                .Include(a => a.Meeting)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (attendance == null)
            {
                _logger.LogWarning("Attendance with ID {Id} not found", id);
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
            
            _logger.LogInformation("Successfully retrieved attendance with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance with ID: {Id}", id);
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
        try
        {
            _logger.LogInformation("Creating new attendance for User ID: {UserId}, Meeting ID: {MeetingId}", 
                attendanceDto.UserId, attendanceDto.MeetingId);
            
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

            // Check if attendance already exists for this user and meeting
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == attendanceDto.UserId && a.MeetingId == attendanceDto.MeetingId);
                
            if (existingAttendance != null)
            {
                _logger.LogWarning("Attendance already exists for User ID: {UserId}, Meeting ID: {MeetingId}", 
                    attendanceDto.UserId, attendanceDto.MeetingId);
                return BadRequest("Attendance already exists for this user and meeting");
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
            
            _logger.LogInformation("Successfully created attendance with ID: {Id}", attendance.Id);
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance for User ID: {UserId}, Meeting ID: {MeetingId}", 
                attendanceDto.UserId, attendanceDto.MeetingId);
            return StatusCode(500, "An error occurred while creating the attendance");
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