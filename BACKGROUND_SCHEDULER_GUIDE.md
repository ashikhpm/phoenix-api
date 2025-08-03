# Background Scheduler Guide

## Overview

This application includes a background scheduler that runs every Saturday at 9:00 AM using Quartz.NET. The scheduler performs various weekly maintenance tasks related to loan management.

## Features

### Weekly Tasks (Every Saturday at 9:00 AM)

1. **Check Overdue Loans**
   - Identifies loans that are past their due date
   - Logs overdue loan information
   - Ready for notification system integration

2. **Generate Weekly Reports**
   - Calculates weekly loan statistics
   - Summarizes total loan amounts and interest received
   - Logs weekly report data

3. **Clean Up Old Data**
   - Removes rejected loan requests older than 1 year
   - Helps maintain database performance
   - Logs cleanup activities

4. **Send Weekly Reminders**
   - Identifies loans due in the next week
   - Logs reminder information for upcoming due dates
   - Ready for notification system integration

## Configuration

### Schedule
- **Frequency**: Every Saturday
- **Time**: 9:00 AM
- **Timezone**: Server's local timezone

### Customization

You can modify the schedule by updating the trigger in `Services/BackgroundJobService.cs`:

```csharp
// Current schedule (every Saturday at 9:00 AM)
.WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Saturday, 9, 0))

// Examples of other schedules:
// Every day at 2:00 AM
.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(2, 0))

// Every Monday at 8:00 AM
.WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Monday, 8, 0))

// Using cron expression (every Saturday at 9:00 AM)
.WithCronSchedule("0 0 9 ? * SAT *")
```

## API Endpoints

### Background Job Management (Secretary Only)

#### 1. Trigger Weekly Job Manually
```http
POST /api/backgroundjob/trigger-weekly
Authorization: Bearer {token}
```

**Response:**
```json
{
  "message": "Weekly job triggered successfully",
  "timestamp": "2024-01-01T10:00:00"
}
```

#### 2. Get Next Run Time
```http
GET /api/backgroundjob/next-run
Authorization: Bearer {token}
```

**Response:**
```json
{
  "nextRunTime": "2024-01-06T09:00:00",
  "previousRunTime": "2023-12-30T09:00:00",
  "triggerState": "Normal",
  "currentTime": "2024-01-01T10:00:00"
}
```

#### 3. Pause Weekly Job
```http
POST /api/backgroundjob/pause
Authorization: Bearer {token}
```

**Response:**
```json
{
  "message": "Weekly job paused successfully",
  "timestamp": "2024-01-01T10:00:00"
}
```

#### 4. Resume Weekly Job
```http
POST /api/backgroundjob/resume
Authorization: Bearer {token}
```

**Response:**
```json
{
  "message": "Weekly job resumed successfully",
  "timestamp": "2024-01-01T10:00:00"
}
```

## Logging

The background scheduler logs all activities. Check your application logs for:

- Job start and completion times
- Number of overdue loans found
- Weekly report statistics
- Cleanup activities
- Reminder notifications

## Customization

### Adding New Weekly Tasks

To add new weekly tasks, modify the `PerformWeeklyTasks()` method in `Services/BackgroundJobService.cs`:

```csharp
private async Task PerformWeeklyTasks()
{
    // Existing tasks
    await CheckOverdueLoans();
    await GenerateWeeklyReports();
    await CleanupOldData();
    await SendWeeklyReminders();
    
    // Add your new task here
    await YourNewTask();
}

private async Task YourNewTask()
{
    try
    {
        // Your custom logic here
        _logger.LogInformation("Your custom task executed");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in your custom task");
    }
}
```

### Notification Integration

The scheduler is ready for notification system integration. Uncomment and implement the notification methods:

```csharp
// In CheckOverdueLoans method
// await SendOverdueNotification(loan);

// In SendWeeklyReminders method
// await SendReminderNotification(loan);
```

## Troubleshooting

### Job Not Running
1. Check if the application is running
2. Verify the scheduler is started (check logs)
3. Ensure the correct timezone is set
4. Check for any exceptions in the job execution

### Manual Testing
Use the API endpoints to manually trigger and test the job:
1. `POST /api/backgroundjob/trigger-weekly` - Run the job immediately
2. `GET /api/backgroundjob/next-run` - Check the schedule

### Logs
Monitor the application logs for:
- "Weekly job started at {Time}"
- "Weekly job completed successfully at {Time}"
- Any error messages during job execution

## Dependencies

- **Quartz.NET**: Job scheduling library
- **Quartz.Extensions.Hosting**: ASP.NET Core integration
- **Entity Framework Core**: Database access for job tasks

## Security

- All background job management endpoints require Secretary role
- Jobs run with the same security context as the application
- Database operations use the same connection and transaction management 