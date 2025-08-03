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
public class MeetingController : BaseController
{
    public MeetingController(UserDbContext context, ILogger<MeetingController> logger, IUserActivityService userActivityService, IServiceProvider serviceProvider)
        : base(context, logger, userActivityService, serviceProvider)
    {
    }

    /// <summary>
    /// Gets all meetings (Secretary only)
    /// </summary>
    /// <returns>List of all meetings</returns>
    [HttpGet]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<IEnumerable<MeetingResponseDto>>> GetAllMeetings()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting all meetings");
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
                Location = m.Location,
                MeetingMinutes = m.MeetingMinutes
            }).ToList();
            
            LogOperation("Retrieved {Count} meetings", responseDtos.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Meeting", null, $"Retrieved {responseDtos.Count} meetings", 
                new { Count = responseDtos.Count }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meetings");
            LogUserActivityAsync("View", "Meeting", null, "Error retrieving meetings", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving meetings");
        }
    }

    /// <summary>
    /// Gets a specific meeting by ID (Secretary only)
    /// </summary>
    /// <param name="id">The ID of the meeting to retrieve</param>
    /// <returns>The meeting if found, otherwise NotFound</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<MeetingResponseDto>> GetMeeting(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting meeting with ID: {Id}", id);
            
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {Id} not found", id);
                LogUserActivityAsync("View", "Meeting", id, "Failed to retrieve meeting - Meeting not found", 
                    null, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {id} not found");
            }
            
            var responseDto = new MeetingResponseDto
            {
                Id = meeting.Id,
                Date = meeting.Date,
                Time = meeting.Time,
                Description = meeting.Description,
                Location = meeting.Location,
                MeetingMinutes = meeting.MeetingMinutes
            };
            
            LogOperation("Successfully retrieved meeting with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Meeting", id, $"Retrieved meeting {meeting.Description}", 
                new { MeetingId = id, Description = meeting.Description, Date = meeting.Date }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meeting with ID: {Id}", id);
            LogUserActivityAsync("View", "Meeting", id, "Error retrieving meeting", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting meeting details with ID: {Id}", id);
            
            var meeting = await _context.Meetings
                .Include(m => m.Attendances)
                    .ThenInclude(a => a.User)
                .Include(m => m.MeetingPayments)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (meeting == null)
            {
                LogWarning("Meeting with ID {Id} not found", id);
                LogUserActivityAsync("View", "Meeting", id, "Failed to retrieve meeting details - Meeting not found", 
                    null, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
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
            
            LogOperation("Successfully retrieved meeting details with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Meeting", id, $"Retrieved meeting details for {meeting.Description}", 
                new { 
                    MeetingId = id, 
                    Description = meeting.Description, 
                    Date = meeting.Date,
                    AttendanceCount = meeting.Attendances.Count,
                    PaymentCount = meeting.MeetingPayments.Count
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meeting details with ID: {Id}", id);
            LogUserActivityAsync("View", "Meeting", id, "Error retrieving meeting details", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting meeting summary with ID: {Id}", id);
            
            var meeting = await _context.Meetings
                .Include(m => m.Attendances)
                .Include(m => m.MeetingPayments)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (meeting == null)
            {
                LogWarning("Meeting with ID {Id} not found", id);
                LogUserActivityAsync("View", "Meeting", id, "Failed to retrieve meeting summary - Meeting not found", 
                    null, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
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
            
            LogOperation("Successfully retrieved meeting summary with ID: {Id}. " +
                "Total Main Payment: {MainPayment}, Total Weekly Payment: {WeeklyPayment}, " +
                "Present Attendance: {PresentCount}/{TotalCount}", 
                id, totalMainPayment, totalWeeklyPayment, presentAttendanceCount, totalAttendanceCount);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Meeting", id, $"Retrieved meeting summary for {meeting.Description}", 
                new { 
                    MeetingId = id, 
                    Description = meeting.Description, 
                    Date = meeting.Date,
                    TotalMainPayment = totalMainPayment,
                    TotalWeeklyPayment = totalWeeklyPayment,
                    PresentAttendees = presentAttendanceCount,
                    TotalAttendees = totalAttendanceCount
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meeting summary with ID: {Id}", id);
            LogUserActivityAsync("View", "Meeting", id, "Error retrieving meeting summary", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting all meeting summaries");
            
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
            
            LogOperation("Retrieved {Count} meeting summaries", responseDtos.Count);
            isSuccess = true;
            
            LogUserActivityAsync("View", "Meeting", null, $"Retrieved {responseDtos.Count} meeting summaries", 
                new { Count = responseDtos.Count }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDtos);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meeting summaries");
            LogUserActivityAsync("View", "Meeting", null, "Error retrieving meeting summaries", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving meeting summaries");
        }
    }

    /// <summary>
    /// Creates a new meeting (Admin only)
    /// </summary>
    /// <param name="meetingDto">The meeting data to create</param>
    /// <returns>The created meeting with assigned ID</returns>
    [HttpPost]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<MeetingResponseDto>> CreateMeeting([FromBody] Models.CreateMeetingDto meetingDto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Creating new meeting with date: {Date}, time: {Time}", 
                meetingDto.Date, meetingDto.Time);
            
            // Parse date
            if (!DateTime.TryParse(meetingDto.Date, out DateTime parsedDate))
            {
                LogWarning("Invalid date format: {Date}", meetingDto.Date);
                LogUserActivityAsync("Create", "Meeting", null, "Failed to create meeting - Invalid date format", 
                    meetingDto, false, "Invalid date format", stopwatch.ElapsedMilliseconds);
                return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
            }

            // Parse time
            if (!TimeSpan.TryParse(meetingDto.Time, out TimeSpan parsedTime))
            {
                LogWarning("Invalid time format: {Time}", meetingDto.Time);
                LogUserActivityAsync("Create", "Meeting", null, "Failed to create meeting - Invalid time format", 
                    meetingDto, false, "Invalid time format", stopwatch.ElapsedMilliseconds);
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
                Location = meeting.Location,
                MeetingMinutes = meeting.MeetingMinutes
            };
            
            LogOperation("Successfully created meeting with ID: {Id}", meeting.Id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Create", "Meeting", meeting.Id, $"Created meeting {meeting.Description}", 
                new { 
                    MeetingId = meeting.Id, 
                    Description = meeting.Description, 
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Location = meeting.Location
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error creating meeting with date: {Date}, time: {Time}", 
                meetingDto.Date, meetingDto.Time);
            LogUserActivityAsync("Create", "Meeting", null, "Error creating meeting", 
                meetingDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
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
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<MeetingResponseDto>> UpdateMeeting(int id, [FromBody] Models.UpdateMeetingDto meetingDto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Updating meeting with ID: {Id}", id);
            
            var existingMeeting = await _context.Meetings.FindAsync(id);
            if (existingMeeting == null)
            {
                LogWarning("Meeting with ID {Id} not found for update", id);
                LogUserActivityAsync("Update", "Meeting", id, "Failed to update meeting - Meeting not found", 
                    meetingDto, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {id} not found");
            }

            // Parse date
            if (!DateTime.TryParse(meetingDto.Date, out DateTime parsedDate))
            {
                LogWarning("Invalid date format: {Date}", meetingDto.Date);
                LogUserActivityAsync("Update", "Meeting", id, "Failed to update meeting - Invalid date format", 
                    meetingDto, false, "Invalid date format", stopwatch.ElapsedMilliseconds);
                return BadRequest("Invalid date format. Please use yyyy-MM-dd format.");
            }

            // Parse time
            if (!TimeSpan.TryParse(meetingDto.Time, out TimeSpan parsedTime))
            {
                LogWarning("Invalid time format: {Time}", meetingDto.Time);
                LogUserActivityAsync("Update", "Meeting", id, "Failed to update meeting - Invalid time format", 
                    meetingDto, false, "Invalid time format", stopwatch.ElapsedMilliseconds);
                return BadRequest("Invalid time format. Please use HH:mm or HH:mm:ss format.");
            }

            // Combine date and time and convert to UTC
            var meetingDateTime = parsedDate.Date.Add(parsedTime);
            
            var originalDescription = existingMeeting.Description;
            var originalDate = existingMeeting.Date;
            var originalTime = existingMeeting.Time;
            var originalLocation = existingMeeting.Location;
            
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
                Location = existingMeeting.Location,
                MeetingMinutes = existingMeeting.MeetingMinutes
            };
            
            LogOperation("Successfully updated meeting with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Update", "Meeting", id, $"Updated meeting {existingMeeting.Description}", 
                new { 
                    MeetingId = id, 
                    Description = existingMeeting.Description,
                    OriginalDescription = originalDescription,
                    Date = existingMeeting.Date,
                    OriginalDate = originalDate,
                    Time = existingMeeting.Time,
                    OriginalTime = originalTime,
                    Location = existingMeeting.Location,
                    OriginalLocation = originalLocation,
                    Changes = new {
                        DescriptionChanged = originalDescription != existingMeeting.Description,
                        DateChanged = originalDate != existingMeeting.Date,
                        TimeChanged = originalTime != existingMeeting.Time,
                        LocationChanged = originalLocation != existingMeeting.Location
                    }
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error updating meeting with ID: {Id}", id);
            LogUserActivityAsync("Update", "Meeting", id, "Error updating meeting", 
                meetingDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while updating the meeting");
        }
    }

    /// <summary>
    /// Deletes a meeting (Admin only)
    /// </summary>
    /// <param name="id">The ID of the meeting to delete</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult> DeleteMeeting(int id)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Deleting meeting with ID: {Id}", id);
            
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {Id} not found for deletion", id);
                LogUserActivityAsync("Delete", "Meeting", id, "Failed to delete meeting - Meeting not found", 
                    null, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {id} not found");
            }
            
            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();
            
            LogOperation("Successfully deleted meeting with ID: {Id}", id);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Delete", "Meeting", id, $"Deleted meeting {meeting.Description}", 
                new { 
                    MeetingId = id, 
                    Description = meeting.Description,
                    Date = meeting.Date,
                    Time = meeting.Time,
                    Location = meeting.Location
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error deleting meeting with ID: {Id}", id);
            LogUserActivityAsync("Delete", "Meeting", id, "Error deleting meeting", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while deleting the meeting");
        }
    }

    /// <summary>
    /// Updates meeting minutes for a specific meeting
    /// </summary>
    /// <param name="request">Meeting minutes request</param>
    /// <returns>Updated meeting minutes</returns>
    [HttpPost("minutes")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<MeetingMinutesResponseDto>> UpdateMeetingMinutes([FromBody] MeetingMinutesDto request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Updating meeting minutes for meeting ID: {MeetingId}", request.MeetingId);

            // Get current user for logging
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                LogWarning("Current user not found");
                LogUserActivityAsync("Update", "MeetingMinutes", request.MeetingId, "Failed to update meeting minutes - User not authenticated", 
                    request, false, "User not authenticated", stopwatch.ElapsedMilliseconds);
                return Unauthorized("User not authenticated");
            }

            // Find the meeting
            var meeting = await _context.Meetings.FindAsync(request.MeetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", request.MeetingId);
                LogUserActivityAsync("Update", "MeetingMinutes", request.MeetingId, "Failed to update meeting minutes - Meeting not found", 
                    request, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {request.MeetingId} not found");
            }

            var originalMinutes = meeting.MeetingMinutes;

            // Update meeting minutes
            meeting.MeetingMinutes = request.MeetingMinutes;
            
            // Save changes
            await _context.SaveChangesAsync();

            var response = new MeetingMinutesResponseDto
            {
                MeetingId = meeting.Id,
                MeetingMinutes = meeting.MeetingMinutes ?? string.Empty,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = currentUser.Name
            };

            LogOperation("Successfully updated meeting minutes for meeting ID: {MeetingId} by user: {UserName}", 
                request.MeetingId, currentUser.Name);
            isSuccess = true;
            
            LogUserActivityWithDetailsAsync("Update", "MeetingMinutes", request.MeetingId, $"Updated meeting minutes for meeting {meeting.Description}", 
                new { 
                    MeetingId = request.MeetingId, 
                    MeetingDescription = meeting.Description,
                    UpdatedBy = currentUser.Name,
                    UpdatedAt = DateTime.UtcNow,
                    OriginalMinutes = originalMinutes,
                    NewMinutes = request.MeetingMinutes,
                    MinutesLength = request.MeetingMinutes?.Length ?? 0
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);

            return Ok(response);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error updating meeting minutes for meeting ID: {MeetingId}", request.MeetingId);
            LogUserActivityAsync("Update", "MeetingMinutes", request.MeetingId, "Error updating meeting minutes", 
                request, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while updating meeting minutes");
        }
    }

    /// <summary>
    /// Gets meeting minutes for a specific meeting
    /// </summary>
    /// <param name="meetingId">Meeting ID</param>
    /// <returns>Meeting minutes</returns>
    [HttpGet("{meetingId}/minutes")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<MeetingMinutesResponseDto>> GetMeetingMinutes(int meetingId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting meeting minutes for meeting ID: {MeetingId}", meetingId);

            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", meetingId);
                LogUserActivityAsync("View", "MeetingMinutes", meetingId, "Failed to retrieve meeting minutes - Meeting not found", 
                    null, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {meetingId} not found");
            }

            var response = new MeetingMinutesResponseDto
            {
                MeetingId = meeting.Id,
                MeetingMinutes = meeting.MeetingMinutes ?? string.Empty,
                UpdatedAt = DateTime.UtcNow, // Note: This would need a separate field to track actual update time
                UpdatedBy = "System" // Note: This would need a separate field to track who updated
            };

            LogOperation("Successfully retrieved meeting minutes for meeting ID: {MeetingId}", meetingId);
            isSuccess = true;
            
            LogUserActivityAsync("View", "MeetingMinutes", meetingId, $"Retrieved meeting minutes for meeting {meeting.Description}", 
                new { 
                    MeetingId = meetingId, 
                    MeetingDescription = meeting.Description,
                    MinutesLength = meeting.MeetingMinutes?.Length ?? 0,
                    HasMinutes = !string.IsNullOrEmpty(meeting.MeetingMinutes)
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error retrieving meeting minutes for meeting ID: {MeetingId}", meetingId);
            LogUserActivityAsync("View", "MeetingMinutes", meetingId, "Error retrieving meeting minutes", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while retrieving meeting minutes");
        }
    }

    /// <summary>
    /// Gets comprehensive meeting summary including attended/absent users and meeting minutes
    /// </summary>
    /// <param name="meetingId">Meeting ID</param>
    /// <returns>Comprehensive meeting summary with attendance details</returns>
    [HttpGet("{meetingId}/comprehensive-summary")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ComprehensiveMeetingSummaryDto>> GetComprehensiveMeetingSummary(int meetingId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var isSuccess = false;
        string? errorMessage = null;

        try
        {
            LogOperation("Getting comprehensive meeting summary for meeting ID: {MeetingId}", meetingId);

            // Get meeting details
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
            {
                LogWarning("Meeting with ID {MeetingId} not found", meetingId);
                LogUserActivityAsync("View", "MeetingSummary", meetingId, "Failed to retrieve comprehensive meeting summary - Meeting not found", 
                    null, false, "Meeting not found", stopwatch.ElapsedMilliseconds);
                return NotFound($"Meeting with ID {meetingId} not found");
            }

            // Get all users who were active at the time of the meeting
            var meetingDate = meeting.Date.Date;
            var eligibleUsers = await GetEligibleUsersForMeetingDate(meetingDate, includeUserRole: true);

            // Get attendance records for this meeting
            var attendances = await _context.Attendances
                .Include(a => a.User)
                .ThenInclude(u => u.UserRole)
                .Where(a => a.MeetingId == meetingId)
                .ToListAsync();

            // Separate attended and absent users
            var attendedUsers = new List<MeetingAttendeeDto>();
            var absentUsers = new List<MeetingAttendeeDto>();

            foreach (var user in eligibleUsers)
            {
                var attendance = attendances.FirstOrDefault(a => a.UserId == user.Id);
                
                if (attendance != null && attendance.IsPresent)
                {
                    // User attended
                    attendedUsers.Add(new MeetingAttendeeDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.UserRole?.Name ?? "Unknown",
                        JoiningDate = user.JoiningDate,
                        InactiveDate = user.InactiveDate,
                        IsActive = user.IsActive,
                        AbsenceReason = string.Empty
                    });
                }
                else
                {
                    // User was absent
                    var absenceReason = DetermineAbsenceReason(user, meetingDate);
                    absentUsers.Add(new MeetingAttendeeDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.UserRole?.Name ?? "Unknown",
                        JoiningDate = user.JoiningDate,
                        InactiveDate = user.InactiveDate,
                        IsActive = user.IsActive,
                        AbsenceReason = absenceReason
                    });
                }
            }

            // Calculate statistics
            var totalEligibleUsers = eligibleUsers.Count;
            var attendedCount = attendedUsers.Count;
            var absentCount = absentUsers.Count;
            var attendancePercentage = totalEligibleUsers > 0 ? (double)attendedCount / totalEligibleUsers * 100 : 0;

            // Get absence reasons for statistics
            var absenceReasons = absentUsers
                .Where(u => !string.IsNullOrEmpty(u.AbsenceReason))
                .Select(u => u.AbsenceReason)
                .Distinct()
                .ToList();

            var attendanceStats = new MeetingAttendanceStatsDto
            {
                TotalEligibleUsers = totalEligibleUsers,
                AttendedCount = attendedCount,
                AbsentCount = absentCount,
                InactiveUsersCount = 0, // Already filtered out
                NotYetJoinedCount = 0, // Already filtered out
                AttendancePercentage = Math.Round(attendancePercentage, 2),
                AbsenceReasons = absenceReasons
            };

            var comprehensiveSummary = new ComprehensiveMeetingSummaryDto
            {
                MeetingId = meeting.Id,
                Description = meeting.Description,
                Date = meeting.Date,
                Time = meeting.Time,
                Location = meeting.Location,
                MeetingMinutes = meeting.MeetingMinutes ?? string.Empty,
                AttendedUsers = attendedUsers,
                AbsentUsers = absentUsers,
                AttendanceStats = attendanceStats,
                GeneratedAt = DateTime.UtcNow
            };

            LogOperation("Successfully generated comprehensive meeting summary for meeting ID: {MeetingId}. " +
                "Total Eligible: {TotalEligible}, Attended: {Attended}, Absent: {Absent}, Attendance: {AttendancePercentage}%", 
                meetingId, totalEligibleUsers, attendedCount, absentCount, attendancePercentage);
            isSuccess = true;
            
            LogUserActivityAsync("View", "MeetingSummary", meetingId, $"Generated comprehensive meeting summary for {meeting.Description}", 
                new { 
                    MeetingId = meetingId, 
                    MeetingDescription = meeting.Description,
                    TotalEligibleUsers = totalEligibleUsers,
                    AttendedCount = attendedCount,
                    AbsentCount = absentCount,
                    AttendancePercentage = attendancePercentage
                }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
            
            return Ok(comprehensiveSummary);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            LogError(ex, "Error generating comprehensive meeting summary for meeting ID: {MeetingId}", meetingId);
            LogUserActivityAsync("View", "MeetingSummary", meetingId, "Error generating comprehensive meeting summary", 
                null, false, errorMessage, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, "An error occurred while generating the comprehensive meeting summary");
        }
    }

    /// <summary>
    /// Determines the reason for a user's absence based on their status and meeting date
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <param name="meetingDate">The date of the meeting</param>
    /// <returns>Reason for absence</returns>
    private string DetermineAbsenceReason(User user, DateTime meetingDate)
    {
        if (!user.IsActive)
        {
            return "User is currently inactive";
        }

        if (!user.JoiningDate.HasValue)
        {
            return "User has no joining date recorded";
        }

        if (user.JoiningDate.Value.Date > meetingDate)
        {
            return "User had not joined yet at meeting date";
        }

        if (user.InactiveDate.HasValue && user.InactiveDate.Value.Date <= meetingDate)
        {
            return "User was inactive at meeting date";
        }

        return "Absent without specific reason";
    }

    /// <summary>
    /// Helper method to get current user
    /// </summary>
    private new async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return null;
        }

        return await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
} 
