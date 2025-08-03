# Scheduled Jobs Management Guide

## Overview

The application includes a comprehensive job management system that allows you to view, control, and monitor scheduled jobs. All endpoints require Secretary role authentication.

## Available Endpoints

### 1. View All Scheduled Jobs
```http
GET /api/backgroundjob/jobs
Authorization: Bearer {token}
```

**Response:**
```json
{
  "schedulerName": "QuartzScheduler",
  "schedulerInstanceId": "NON_CLUSTERED",
  "isStarted": true,
  "isShutdown": false,
  "currentTime": "2024-01-01T10:00:00",
  "jobs": [
    {
      "jobKey": "DEFAULT.weeklyJob",
      "jobGroup": "DEFAULT",
      "jobName": "weeklyJob",
      "jobClass": "WeeklyJob",
      "description": null,
      "triggers": [
        {
          "triggerKey": "DEFAULT.weeklyTrigger",
          "triggerGroup": "DEFAULT",
          "triggerName": "weeklyTrigger",
          "triggerState": "Normal",
          "nextFireTime": "2024-01-06T09:00:00",
          "previousFireTime": "2023-12-30T09:00:00",
          "description": null,
          "calendarName": null,
          "misfireInstruction": "SmartPolicy",
          "priority": 5
        }
      ]
    }
  ]
}
```

### 2. View Specific Job Details
```http
GET /api/backgroundjob/jobs/{jobName}?jobGroup={groupName}
Authorization: Bearer {token}
```

**Example:**
```http
GET /api/backgroundjob/jobs/weeklyJob?jobGroup=DEFAULT
Authorization: Bearer {token}
```

**Response:**
```json
{
  "jobKey": "DEFAULT.weeklyJob",
  "jobName": "weeklyJob",
  "jobGroup": "DEFAULT",
  "jobClass": "WeeklyJob",
  "description": null,
  "jobDataMap": {},
  "triggers": [
    {
      "triggerKey": "DEFAULT.weeklyTrigger",
      "triggerState": "Normal",
      "nextFireTime": "2024-01-06T09:00:00",
      "previousFireTime": "2023-12-30T09:00:00",
      "description": null,
      "calendarName": null,
      "misfireInstruction": "SmartPolicy",
      "priority": 5
    }
  ]
}
```

### 3. Get Next Run Time
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

### 4. Manually Trigger Job
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

### 5. Pause Job
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

### 6. Resume Job
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

## Understanding Job Information

### Job Details
- **jobKey**: Unique identifier for the job (format: Group.Name)
- **jobName**: Name of the job
- **jobGroup**: Group the job belongs to
- **jobClass**: The .NET class that implements the job
- **description**: Optional description of the job
- **jobDataMap**: Data associated with the job

### Trigger Details
- **triggerKey**: Unique identifier for the trigger
- **triggerState**: Current state of the trigger
  - `Normal`: Trigger is active and will fire
  - `Paused`: Trigger is paused and won't fire
  - `Complete`: Trigger has completed all executions
  - `Error`: Trigger encountered an error
  - `Blocked`: Trigger is blocked by another trigger
  - `None`: Trigger doesn't exist
- **nextFireTime**: When the trigger will fire next
- **previousFireTime**: When the trigger last fired
- **description**: Optional description of the trigger
- **calendarName**: Associated calendar (if any)
- **misfireInstruction**: How to handle missed executions
- **priority**: Trigger priority (higher numbers = higher priority)

## Current Scheduled Jobs

### Weekly Job
- **Name**: `weeklyJob`
- **Group**: `DEFAULT`
- **Class**: `WeeklyJob`
- **Schedule**: Every Saturday at 9:00 AM
- **Tasks**:
  - Check overdue loans
  - Generate weekly reports
  - Clean up old data
  - Send weekly reminders

## Job States

### Trigger States
1. **Normal**: Trigger is active and will execute as scheduled
2. **Paused**: Trigger is paused and won't execute
3. **Complete**: Trigger has completed all its executions
4. **Error**: Trigger encountered an error during execution
5. **Blocked**: Trigger is blocked by another trigger
6. **None**: Trigger doesn't exist or has been removed

### Scheduler States
- **isStarted**: Whether the scheduler is running
- **isShutdown**: Whether the scheduler has been shut down
- **schedulerName**: Name of the scheduler instance
- **schedulerInstanceId**: Unique identifier for the scheduler

## Testing and Debugging

### 1. Check Job Status
```bash
# View all jobs
curl -X GET "http://localhost:5276/api/backgroundjob/jobs" \
  -H "Authorization: Bearer your-token"

# View specific job
curl -X GET "http://localhost:5276/api/backgroundjob/jobs/weeklyJob" \
  -H "Authorization: Bearer your-token"
```

### 2. Test Job Execution
```bash
# Manually trigger the job
curl -X POST "http://localhost:5276/api/backgroundjob/trigger-weekly" \
  -H "Authorization: Bearer your-token"
```

### 3. Control Job Execution
```bash
# Pause the job
curl -X POST "http://localhost:5276/api/backgroundjob/pause" \
  -H "Authorization: Bearer your-token"

# Resume the job
curl -X POST "http://localhost:5276/api/backgroundjob/resume" \
  -H "Authorization: Bearer your-token"
```

## Monitoring and Logs

### Application Logs
Monitor the application logs for job execution:
```
info: WeeklyJob[0]
      Weekly job started at 1/6/2024 9:00:00 AM
info: WeeklyJob[0]
      Found 5 overdue loans
info: WeeklyJob[0]
      Weekly Report - Loans: 12, Total Amount: 50000.00, Total Interest: 2500.00
info: WeeklyJob[0]
      Cleaned up 3 old loan requests
info: WeeklyJob[0]
      Found 8 loans due next week
info: WeeklyJob[0]
      Weekly job completed successfully at 1/6/2024 9:05:30 AM
```

### Common Issues

1. **Job Not Running**
   - Check if scheduler is started
   - Verify trigger state is "Normal"
   - Check for errors in application logs

2. **Job Paused**
   - Use resume endpoint to restart
   - Check why it was paused

3. **Job Errors**
   - Check application logs for error details
   - Verify database connectivity
   - Check job implementation for exceptions

## Security

- All endpoints require Secretary role authentication
- Jobs run with the same security context as the application
- Database operations use the same connection management
- No sensitive data is exposed in job responses

## Best Practices

1. **Regular Monitoring**: Check job status regularly
2. **Log Analysis**: Monitor logs for job execution issues
3. **Testing**: Use manual trigger for testing
4. **Backup**: Ensure job configuration is backed up
5. **Documentation**: Keep track of job schedules and purposes 