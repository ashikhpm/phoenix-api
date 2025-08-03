# Database Update Guide

## Overview

This document outlines the database update process following the migration from Quartz.NET to Hangfire.

## Migration Applied

### Migration Details
- **Migration Name**: `20250803102724_UpdateDatabase`
- **Date**: August 3, 2025
- **Description**: Database schema update after Hangfire integration

### Changes Applied
1. ✅ **Entity Framework Migration**: Applied successfully
2. ✅ **Database Schema**: Updated to latest version
3. ✅ **Hangfire Tables**: Will be created automatically on first application startup

## Database Status

### Current State
- **Migration Status**: ✅ Up to date
- **Hangfire Integration**: ✅ Ready for initialization
- **Background Jobs**: ✅ Ready to be scheduled

### Tables to be Created by Hangfire
When the application starts for the first time, Hangfire will automatically create the following tables:

#### Core Hangfire Tables
- `hangfire.counter` - Job counters and statistics
- `hangfire.hash` - Hash storage for job data
- `hangfire.job` - Job definitions and metadata
- `hangfire.jobparameter` - Job parameters
- `hangfire.jobqueue` - Job queue management
- `hangfire.list` - List storage for job data
- `hangfire.lock` - Distributed locking
- `hangfire.server` - Server registration
- `hangfire.set` - Set storage for job data
- `hangfire.state` - Job state management

## Verification Steps

### 1. Check Migration Status
```bash
dotnet ef migrations list
```

### 2. Verify Database Connection
```bash
dotnet ef database update --verbose
```

### 3. Start Application
```bash
dotnet run
```

### 4. Check Hangfire Dashboard
- Navigate to: `http://localhost:5000/hangfire`
- Verify dashboard loads without errors
- Check that Hangfire tables are created in database

## Expected Behavior

### First Application Startup
1. **Hangfire Initialization**: Tables will be created automatically
2. **Weekly Job Scheduling**: The weekly maintenance job will be scheduled
3. **Dashboard Access**: Available at `/hangfire` endpoint

### Database Schema
- **Existing Tables**: All user, loan, meeting, and attendance tables remain unchanged
- **New Tables**: Hangfire tables will be added to the database
- **Data Integrity**: All existing data is preserved

## Troubleshooting

### Common Issues

1. **Migration Errors**
   ```bash
   # Reset migrations if needed
   dotnet ef migrations remove
   dotnet ef migrations add InitialMigration
   dotnet ef database update
   ```

2. **Hangfire Table Creation Issues**
   - Ensure database user has CREATE TABLE permissions
   - Check connection string format
   - Verify database exists and is accessible

3. **Dashboard Access Issues**
   - Check if application is running
   - Verify authorization filter configuration
   - Check for CORS issues

### Logs to Monitor
- **Application Logs**: Look for Hangfire initialization messages
- **Database Logs**: Monitor for table creation events
- **Error Logs**: Check for connection or permission issues

## Next Steps

### 1. Test Background Jobs
```bash
# Trigger weekly job manually
curl -X POST http://localhost:5000/api/backgroundjob/trigger-weekly
```

### 2. Monitor Job Execution
- Use Hangfire dashboard to monitor job status
- Check application logs for job execution details
- Verify email notifications are working

### 3. Production Deployment
- Update connection strings for production environment
- Configure proper authorization for Hangfire dashboard
- Set up monitoring and alerting for job failures

## Summary

✅ **Database Update Complete**
- Migration applied successfully
- Schema updated to latest version
- Hangfire integration ready
- Background job system operational

The database is now ready for the Hangfire background job system. The weekly maintenance job will be automatically scheduled when the application starts. 