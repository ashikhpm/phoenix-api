# User Activity Tracking Methods

## Overview
This document lists all methods in the Phoenix Sangam API that currently track user activity and those that need to be updated to include activity tracking.

## Currently Tracking User Activity

### ✅ LoanController (Partially Implemented)
**Status**: Inherits from BaseController, has some activity logging

#### Methods with Activity Tracking:
1. **GetAllLoans()** - `GET /api/loan`
   - ✅ Tracks successful loan retrieval
   - ✅ Tracks failed operations (user not found, token issues)
   - ✅ Tracks performance metrics
   - **Action**: "View"
   - **Entity**: "Loan"

#### Methods WITHOUT Activity Tracking:
1. **GetLoan(int id)** - `GET /api/loan/{id}`
2. **CreateLoan(CreateLoanDto)** - `POST /api/loan`
3. **UpdateLoan(int id, CreateLoanDto)** - `PUT /api/loan/{id}`
4. **DeleteLoan(int id)** - `DELETE /api/loan/{id}`
5. **LoanRepayment(LoanRepaymentDto)** - `POST /api/loan/repayment`
6. **GetLoanTypes()** - `GET /api/loan/types`

### ✅ OptimizedLoanController
**Status**: Inherits from BaseController

#### Methods with Activity Tracking:
- All methods inherit BaseController logging capabilities
- Uses `ExecuteWithLoggingAsync` wrapper for automatic tracking

### ✅ UserActivityController
**Status**: Dedicated controller for viewing activity logs

#### Methods:
1. **GetUserActivities()** - `GET /api/useractivity`
2. **GetUserActivitiesByUser(int userId)** - `GET /api/useractivity/user/{userId}`
3. **GetUserActivitiesByAction(string action)** - `GET /api/useractivity/action/{action}`
4. **GetUserActivitiesByEntityType(string entityType)** - `GET /api/useractivity/entity/{entityType}`
5. **GetUserActivitiesByDateRange(DateTime startDate, DateTime endDate)** - `GET /api/useractivity/date-range`
6. **GetUserActivitiesWithFilter(UserActivityFilterDto)** - `POST /api/useractivity/filter`
7. **GetActivityStatistics(DateTime startDate, DateTime endDate)** - `GET /api/useractivity/statistics`
8. **ExportUserActivities()** - `GET /api/useractivity/export`

## NOT Currently Tracking User Activity

### ❌ UserController
**Status**: Does NOT inherit from BaseController

#### Methods WITHOUT Activity Tracking:
1. **GetAllUsers()** - `GET /api/user`
2. **GetActiveUsers()** - `GET /api/user/active`
3. **GetUserRoles()** - `GET /api/user/roles`
4. **GetUser(int id)** - `GET /api/user/{id}`
5. **CreateUser(CreateUserRequest)** - `POST /api/user`
6. **UpdateUser(int id, User)** - `PUT /api/user/{id}`
7. **DeleteUser(int id)** - `DELETE /api/user/{id}`
8. **ReactivateUser(int id)** - `PUT /api/user/{id}/reactivate`
9. **GetCurrentUser()** - `GET /api/user/me`
10. **Login(LoginRequest)** - `POST /api/user/login`
11. **TestEmail(TestEmailRequest)** - `POST /api/user/test-email`
12. **HealthCheck()** - `GET /api/user/health`

### ❌ MeetingController
**Status**: Does NOT inherit from BaseController

#### Methods WITHOUT Activity Tracking:
1. **GetAllMeetings()** - `GET /api/meeting`
2. **GetMeeting(int id)** - `GET /api/meeting/{id}`
3. **CreateMeeting(CreateMeetingDto)** - `POST /api/meeting`
4. **UpdateMeeting(int id, UpdateMeetingDto)** - `PUT /api/meeting/{id}`
5. **DeleteMeeting(int id)** - `DELETE /api/meeting/{id}`
6. **GetMeetingTypes()** - `GET /api/meeting/types`

### ❌ AttendanceController
**Status**: Does NOT inherit from BaseController

#### Methods WITHOUT Activity Tracking:
1. **GetAllAttendances()** - `GET /api/attendance`
2. **GetAttendance(int id)** - `GET /api/attendance/{id}`
3. **CreateAttendance(CreateAttendanceDto)** - `POST /api/attendance`
4. **UpdateAttendance(int id, UpdateAttendanceDto)** - `PUT /api/attendance/{id}`
5. **DeleteAttendance(int id)** - `DELETE /api/attendance/{id}`
6. **GetAttendancesByMeeting(int meetingId)** - `GET /api/attendance/meeting/{meetingId}`
7. **GetAttendancesByUser(int userId)** - `GET /api/attendance/user/{userId}`
8. **BulkCreateAttendance(BulkAttendanceDto)** - `POST /api/attendance/bulk`

### ❌ MeetingPaymentController
**Status**: Does NOT inherit from BaseController

#### Methods WITHOUT Activity Tracking:
1. **GetAllMeetingPayments()** - `GET /api/meetingpayment`
2. **GetMeetingPayment(int id)** - `GET /api/meetingpayment/{id}`
3. **CreateMeetingPayment(CreateMeetingPaymentDto)** - `POST /api/meetingpayment`
4. **UpdateMeetingPayment(int id, UpdateMeetingPaymentDto)** - `PUT /api/meetingpayment/{id}`
5. **DeleteMeetingPayment(int id)** - `DELETE /api/meetingpayment/{id}`
6. **GetPaymentsByMeeting(int meetingId)** - `GET /api/meetingpayment/meeting/{meetingId}`
7. **GetPaymentsByUser(int userId)** - `GET /api/meetingpayment/user/{userId}`
8. **BulkCreateMeetingPayment(BulkMeetingPaymentDto)** - `POST /api/meetingpayment/bulk`

### ❌ DashboardController
**Status**: Does NOT inherit from BaseController

#### Methods WITHOUT Activity Tracking:
1. **GetDashboardData()** - `GET /api/dashboard`
2. **GetLoanStatistics()** - `GET /api/dashboard/loans`
3. **GetUserStatistics()** - `GET /api/dashboard/users`
4. **GetMeetingStatistics()** - `GET /api/dashboard/meetings`
5. **GetPaymentStatistics()** - `GET /api/dashboard/payments`
6. **GetActivityStatistics()** - `GET /api/dashboard/activity`

### ❌ BackgroundJobController
**Status**: Does NOT inherit from BaseController

#### Methods WITHOUT Activity Tracking:
1. **GetBackgroundJobs()** - `GET /api/backgroundjob`
2. **GetBackgroundJob(int id)** - `GET /api/backgroundjob/{id}`
3. **CreateBackgroundJob(CreateBackgroundJobDto)** - `POST /api/backgroundjob`
4. **UpdateBackgroundJob(int id, UpdateBackgroundJobDto)** - `PUT /api/backgroundjob/{id}`
5. **DeleteBackgroundJob(int id)** - `DELETE /api/backgroundjob/{id}`
6. **RunBackgroundJob(int id)** - `POST /api/backgroundjob/{id}/run`

## Activity Tracking Methods Available

### BaseController Methods (Available to Inheriting Controllers)

#### 1. LogUserActivityAsync()
```csharp
protected async Task LogUserActivityAsync(
    string action,
    string entityType,
    int? entityId = null,
    string? description = null,
    string? details = null,
    bool isSuccess = true,
    string? errorMessage = null,
    long durationMs = 0)
```

#### 2. LogUserActivityWithDetailsAsync()
```csharp
protected async Task LogUserActivityWithDetailsAsync(
    string action,
    string entityType,
    int? entityId = null,
    string? description = null,
    object? detailsObject = null,
    bool isSuccess = true,
    string? errorMessage = null,
    long durationMs = 0)
```

#### 3. ExecuteWithLoggingAsync() (Generic)
```csharp
protected async Task<T> ExecuteWithLoggingAsync<T>(
    Func<Task<T>> action,
    string operation,
    string entityType,
    int? entityId = null,
    string? description = null,
    object? details = null)
```

#### 4. ExecuteWithLoggingAsync() (Void)
```csharp
protected async Task ExecuteWithLoggingAsync(
    Func<Task> action,
    string operation,
    string entityType,
    int? entityId = null,
    string? description = null,
    object? details = null)
```

### UserActivityService Methods

#### 1. LogActivityAsync()
```csharp
public async Task LogActivityAsync(UserActivity activity)
```

#### 2. LogActivityAsync() (With Parameters)
```csharp
public async Task LogActivityAsync(
    int userId,
    string userName,
    string userRole,
    string action,
    string entityType,
    int? entityId = null,
    string? description = null,
    string? details = null,
    string httpMethod = "GET",
    string endpoint = "",
    string? ipAddress = null,
    string? userAgent = null,
    int statusCode = 200,
    bool isSuccess = true,
    string? errorMessage = null,
    long durationMs = 0)
```

#### 3. LogActivityWithDetailsAsync()
```csharp
public async Task LogActivityWithDetailsAsync(
    int userId,
    string userName,
    string userRole,
    string action,
    string entityType,
    int? entityId,
    string? description,
    object? detailsObject,
    string httpMethod,
    string endpoint,
    string? ipAddress,
    string? userAgent,
    int statusCode,
    bool isSuccess,
    string? errorMessage,
    long durationMs)
```

## Implementation Priority

### High Priority (Core Business Operations)
1. **UserController** - User management is critical
2. **LoanController** - Complete the implementation (only 1/6 methods tracked)
3. **MeetingController** - Meeting management is important
4. **AttendanceController** - Attendance tracking is essential

### Medium Priority (Supporting Operations)
1. **MeetingPaymentController** - Payment tracking
2. **DashboardController** - Dashboard operations

### Low Priority (System Operations)
1. **BackgroundJobController** - Background operations
2. **WeatherForecastController** - Test controller

## Action Types to Track

### Standard CRUD Actions
- `View` - GET operations
- `Create` - POST operations
- `Update` - PUT operations
- `Delete` - DELETE operations

### Special Actions
- `Login` - Authentication
- `Logout` - Logout
- `Export` - Data export
- `Import` - Data import
- `Bulk` - Bulk operations
- `Reactivate` - User reactivation
- `Repayment` - Loan repayment

### Entity Types to Track
- `User` - User operations
- `Loan` - Loan operations
- `Meeting` - Meeting operations
- `Attendance` - Attendance operations
- `Payment` - Payment operations
- `Dashboard` - Dashboard operations
- `BackgroundJob` - Background job operations

## Summary

### Currently Tracking: 2 Controllers
- ✅ **LoanController**: 1/6 methods (17%)
- ✅ **OptimizedLoanController**: All methods (100%)
- ✅ **UserActivityController**: All methods (100%)

### Need Implementation: 6 Controllers
- ❌ **UserController**: 0/12 methods (0%)
- ❌ **MeetingController**: 0/6 methods (0%)
- ❌ **AttendanceController**: 0/8 methods (0%)
- ❌ **MeetingPaymentController**: 0/8 methods (0%)
- ❌ **DashboardController**: 0/6 methods (0%)
- ❌ **BackgroundJobController**: 0/6 methods (0%)

### Total Coverage: 3/8 Controllers (37.5%)
### Total Methods with Tracking: ~3/52 methods (5.8%)

**Recommendation**: Implement activity tracking systematically across all controllers, starting with the high-priority controllers (UserController, complete LoanController, MeetingController, AttendanceController). 