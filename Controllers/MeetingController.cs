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
public class MeetingController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<MeetingController> _logger;

    public MeetingController(UserDbContext context, ILogger<MeetingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all meetings (Secretary only)
    /// </summary>
    /// <returns>List of all meetings</returns>
    [HttpGet]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<IEnumerable<MeetingResponseDto>>> GetAllMeetings()
    {
        try
        {
            _logger.LogInformation("Getting all meetings");
            var meetings = await _context.Meetings
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Time)
                .ToListAsync();
            
            var responseDtos = meetings.Select(m => new MeetingResponseDto
            {
                Id = m.Id,
                Date = m.Date,
                Time = m.Time,
                Description = m.Description,
                Location = m.Location
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} meetings", responseDtos.Count);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meetings");
            return StatusCode(500, "An error occurred while retrieving meetings");
        }
    }

    /// <summary>
    /// Gets a specific meeting by ID (Secretary only)
    /// </summary>
    /// <param name="id">The ID of the meeting to retrieve</param>
    /// <returns>The meeting if found, otherwise NotFound</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<MeetingResponseDto>> GetMeeting(int id)
    {
        try
        {
            _logger.LogInformation("Getting meeting with ID: {Id}", id);
            
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {Id} not found", id);
                return NotFound($"Meeting with ID {id} not found");
            }
            
            var responseDto = new MeetingResponseDto
            {
                Id = meeting.Id,
                Date = meeting.Date,
                Time = meeting.Time,
                Description = meeting.Description,
                Location = meeting.Location
            };
            
            _logger.LogInformation("Successfully retrieved meeting with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the meeting");
        }
    }

    /// <summary>
    /// Gets a meeting with all its details (attendances and payments)
    /// </summary>
    /// <param name="id">The ID of the meeting to retrieve</param>
    /// <returns>The meeting with details if found, otherwise NotFound</returns>
    [HttpGet("{id}/details")]
    public async Task<ActionResult<MeetingWithDetailsResponseDto>> GetMeetingWithDetails(int id)
    {
        try
        {
            _logger.LogInformation("Getting meeting details with ID: {Id}", id);
            
            var meeting = await _context.Meetings
                .Include(m => m.Attendances)
                    .ThenInclude(a => a.User)
                .Include(m => m.MeetingPayments)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {Id} not found", id);
                return NotFound($"Meeting with ID {id} not found");
            }
            
            var responseDto = new MeetingWithDetailsResponseDto
            {
                Id = meeting.Id,
                Date = meeting.Date,
                Time = meeting.Time,
                Description = meeting.Description,
                Location = meeting.Location,
                Attendances = meeting.Attendances.Select(a => new AttendanceResponseDto
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
                    Meeting = null // Avoid circular reference
                }).ToList(),
                MeetingPayments = meeting.MeetingPayments.Select(p => new MeetingPaymentResponseDto
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
                    Meeting = null // Avoid circular reference
                }).ToList()
            };
            
            _logger.LogInformation("Successfully retrieved meeting details with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting details with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the meeting details");
        }
    }

    /// <summary>
    /// Gets a meeting with summary statistics (total payments and attendance count)
    /// </summary>
    /// <param name="id">The ID of the meeting to retrieve</param>
    /// <returns>The meeting with summary statistics if found, otherwise NotFound</returns>
    [HttpGet("{id}/summary")]
    public async Task<ActionResult<MeetingSummaryResponseDto>> GetMeetingSummary(int id)
    {
        try
        {
            _logger.LogInformation("Getting meeting summary with ID: {Id}", id);
            
            var meeting = await _context.Meetings
                .Include(m => m.Attendances)
                .Include(m => m.MeetingPayments)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {Id} not found", id);
                return NotFound($"Meeting with ID {id} not found");
            }
            
            // Calculate totals
            var totalMainPayment = meeting.MeetingPayments.Sum(p => p.MainPayment);
            var totalWeeklyPayment = meeting.MeetingPayments.Sum(p => p.WeeklyPayment);
            var presentAttendanceCount = meeting.Attendances.Count(a => a.IsPresent);
            var totalAttendanceCount = meeting.Attendances.Count;
            
            var responseDto = new MeetingSummaryResponseDto
            {
                Id = meeting.Id,
                Date = meeting.Date,
                Time = meeting.Time,
                Description = meeting.Description,
                Location = meeting.Location,
                TotalMainPayment = totalMainPayment,
                TotalWeeklyPayment = totalWeeklyPayment,
                PresentAttendees = presentAttendanceCount,
                TotalAttendees = totalAttendanceCount,
                AbsentAttendees = totalAttendanceCount - presentAttendanceCount
            };
            
            _logger.LogInformation("Successfully retrieved meeting summary with ID: {Id}. " +
                "Total Main Payment: {MainPayment}, Total Weekly Payment: {WeeklyPayment}, " +
                "Present Attendance: {PresentCount}/{TotalCount}", 
                id, totalMainPayment, totalWeeklyPayment, presentAttendanceCount, totalAttendanceCount);
            
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting summary with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the meeting summary");
        }
    }

    /// <summary>
    /// Gets all meetings with summary statistics
    /// </summary>
    /// <returns>List of all meetings with summary statistics</returns>
    [HttpGet("summaries")]
    public async Task<ActionResult<IEnumerable<MeetingSummaryResponseDto>>> GetAllMeetingSummaries()
    {
        try
        {
            _logger.LogInformation("Getting all meeting summaries");
            
            var meetings = await _context.Meetings
                .Include(m => m.Attendances)
                .Include(m => m.MeetingPayments)
                .ToListAsync();
            
            var responseDtos = meetings.Select(m => new MeetingSummaryResponseDto
            {
                Id = m.Id,
                Date = m.Date,
                Time = m.Time,
                Description = m.Description,
                Location = m.Location,
                TotalMainPayment = m.MeetingPayments.Sum(p => p.MainPayment),
                TotalWeeklyPayment = m.MeetingPayments.Sum(p => p.WeeklyPayment),
                PresentAttendees = m.Attendances.Count(a => a.IsPresent),
                TotalAttendees = m.Attendances.Count,
                AbsentAttendees = m.Attendances.Count - m.Attendances.Count(a => a.IsPresent)
            }).ToList();
            
            _logger.LogInformation("Retrieved {Count} meeting summaries", responseDtos.Count);
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting summaries");
            return StatusCode(500, "An error occurred while retrieving meeting summaries");
        }
    }

    /// <summary>
    /// Creates a new meeting (Admin only)
    /// </summary>
    /// <param name="meetingDto">The meeting data to create</param>
    /// <returns>The created meeting with assigned ID</returns>
    [HttpPost]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<MeetingResponseDto>> CreateMeeting([FromBody] Models.CreateMeetingDto meetingDto)
    {
        try
        {
            _logger.LogInformation("Creating new meeting with date: {Date}, time: {Time}", 
                meetingDto.Date, meetingDto.Time);
            
            // Parse date
            if (!DateTime.TryParse(meetingDto.Date, out DateTime parsedDate))
            {
                _logger.LogWarning("Invalid date format: {Date}", meetingDto.Date);
                return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
            }

            // Parse time
            if (!TimeSpan.TryParse(meetingDto.Time, out TimeSpan parsedTime))
            {
                _logger.LogWarning("Invalid time format: {Time}", meetingDto.Time);
                return BadRequest("Invalid time format. Please use HH:mm or HH:mm:ss format.");
            }

            // Combine date and time and convert to UTC
            var meetingDateTime = parsedDate.Date.Add(parsedTime);
            
            var meeting = new Meeting
            {
                Date = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc),
                Time = DateTime.SpecifyKind(meetingDateTime, DateTimeKind.Utc),
                Description = meetingDto.Description,
                Location = meetingDto.Location
            };

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();
            
            var responseDto = new MeetingResponseDto
            {
                Id = meeting.Id,
                Date = meeting.Date,
                Time = meeting.Time,
                Description = meeting.Description,
                Location = meeting.Location
            };
            
            _logger.LogInformation("Successfully created meeting with ID: {Id}", meeting.Id);
            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meeting with date: {Date}, time: {Time}", 
                meetingDto.Date, meetingDto.Time);
            return StatusCode(500, "An error occurred while creating the meeting");
        }
    }

    /// <summary>
    /// Updates an existing meeting (Admin only)
    /// </summary>
    /// <param name="id">The ID of the meeting to update</param>
    /// <param name="meetingDto">The updated meeting data</param>
    /// <returns>The updated meeting</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult<MeetingResponseDto>> UpdateMeeting(int id, [FromBody] Models.UpdateMeetingDto meetingDto)
    {
        try
        {
            _logger.LogInformation("Updating meeting with ID: {Id}", id);
            
            var existingMeeting = await _context.Meetings.FindAsync(id);
            if (existingMeeting == null)
            {
                _logger.LogWarning("Meeting with ID {Id} not found for update", id);
                return NotFound($"Meeting with ID {id} not found");
            }

            // Parse date
            if (!DateTime.TryParse(meetingDto.Date, out DateTime parsedDate))
            {
                _logger.LogWarning("Invalid date format: {Date}", meetingDto.Date);
                return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
            }

            // Parse time
            if (!TimeSpan.TryParse(meetingDto.Time, out TimeSpan parsedTime))
            {
                _logger.LogWarning("Invalid time format: {Time}", meetingDto.Time);
                return BadRequest("Invalid time format. Please use HH:mm or HH:mm:ss format.");
            }

            // Combine date and time and convert to UTC
            var meetingDateTime = parsedDate.Date.Add(parsedTime);
            
            existingMeeting.Date = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
            existingMeeting.Time = DateTime.SpecifyKind(meetingDateTime, DateTimeKind.Utc);
            existingMeeting.Description = meetingDto.Description;
            existingMeeting.Location = meetingDto.Location;
            
            await _context.SaveChangesAsync();
            
            var responseDto = new MeetingResponseDto
            {
                Id = existingMeeting.Id,
                Date = existingMeeting.Date,
                Time = existingMeeting.Time,
                Description = existingMeeting.Description,
                Location = existingMeeting.Location
            };
            
            _logger.LogInformation("Successfully updated meeting with ID: {Id}", id);
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meeting with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the meeting");
        }
    }

    /// <summary>
    /// Deletes a meeting (Admin only)
    /// </summary>
    /// <param name="id">The ID of the meeting to delete</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary")]
    public async Task<ActionResult> DeleteMeeting(int id)
    {
        try
        {
            _logger.LogInformation("Deleting meeting with ID: {Id}", id);
            
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                _logger.LogWarning("Meeting with ID {Id} not found for deletion", id);
                return NotFound($"Meeting with ID {id} not found");
            }
            
            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully deleted meeting with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meeting with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the meeting");
        }
    }
} 