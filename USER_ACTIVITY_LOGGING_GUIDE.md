# User Activity Logging System Guide

## Overview
The application now includes a comprehensive user activity logging system that tracks all user actions across every endpoint. This system provides detailed audit trails for security, monitoring, and compliance purposes.

## Features

### 🔍 **Comprehensive Tracking**
- **All User Actions:** Every API endpoint logs user activities
- **Detailed Information:** User ID, name, role, action, entity type, timestamps
- **Performance Metrics:** Response times and duration tracking
- **Error Tracking:** Failed operations are logged with error details
- **Request Details:** IP address, user agent, HTTP method, endpoint

### 📊 **Activity Analytics**
- **Filtering:** By user, action, entity type, date range
- **Statistics:** Activity breakdowns and trends
- **Performance Monitoring:** Average response times
- **Error Analysis:** Failed operations tracking

### 🔐 **Security Features**
- **Audit Trail:** Complete history of all user actions
- **Role-Based Access:** Only admin users can view activity logs
- **Data Integrity:** All activities are stored with timestamps
- **Privacy Protection:** Sensitive data is handled securely

## Database Schema

### UserActivity Table
```sql
CREATE TABLE "UserActivities" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "UserName" VARCHAR(100) NOT NULL,
    "UserRole" VARCHAR(50) NOT NULL,
    "Action" VARCHAR(50) NOT NULL,
    "EntityType" VARCHAR(50) NOT NULL,
    "EntityId" INTEGER,
    "Description" VARCHAR(500),
    "Details" VARCHAR(2000),
    "HttpMethod" VARCHAR(10) NOT NULL,
    "Endpoint" VARCHAR(200) NOT NULL,
    "IpAddress" VARCHAR(45),
    "UserAgent" VARCHAR(500),
    "StatusCode" INTEGER NOT NULL,
    "IsSuccess" BOOLEAN NOT NULL,
    "ErrorMessage" VARCHAR(1000),
    "Timestamp" TIMESTAMP NOT NULL,
    "DurationMs" BIGINT NOT NULL
);
```

### Indexes for Performance
- `Timestamp` - For date-based queries
- `UserId` - For user-specific queries
- `Action` - For action-based filtering
- `EntityType` - For entity-based filtering

## API Endpoints

### User Activity Management

#### 1. Get User Activities
```http
GET /api/UserActivity
Authorization: Bearer <token>
```

**Query Parameters:**
- `userId` (optional): Filter by specific user
- `action` (optional): Filter by action type
- `entityType` (optional): Filter by entity type
- `startDate` (optional): Start date for filtering
- `endDate` (optional): End date for filtering
- `page` (default: 1): Page number
- `pageSize` (default: 50): Items per page

**Response:**
```json
{
  "activities": [
    {
      "id": 1,
      "userId": 1,
      "userName": "John Doe",
      "userRole": "Secretary",
      "action": "Create",
      "entityType": "User",
      "entityId": 5,
      "description": "Created new user",
      "details": "{\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}",
      "httpMethod": "POST",
      "endpoint": "POST /api/User",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "statusCode": 201,
      "isSuccess": true,
      "errorMessage": null,
      "timestamp": "2024-01-15T10:30:00Z",
      "durationMs": 150
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 50,
  "totalPages": 2
}
```

#### 2. Get Activity Statistics
```http
GET /api/UserActivity/statistics?startDate=2024-01-01&endDate=2024-01-31
Authorization: Bearer <token>
```

**Response:**
```json
{
  "totalActivities": 1250,
  "successfulActivities": 1200,
  "failedActivities": 50,
  "averageDurationMs": 245.5,
  "actionsBreakdown": [
    { "action": "View", "count": 500 },
    { "action": "Create", "count": 300 },
    { "action": "Update", "count": 250 },
    { "action": "Delete", "count": 200 }
  ],
  "entityTypesBreakdown": [
    { "entityType": "User", "count": 400 },
    { "entityType": "Loan", "count": 350 },
    { "entityType": "Meeting", "count": 300 },
    { "entityType": "Attendance", "count": 200 }
  ],
  "usersBreakdown": [
    { "userId": 1, "userName": "John Doe", "count": 500 },
    { "userId": 2, "userName": "Jane Smith", "count": 300 }
  ],
  "dailyActivity": [
    { "date": "2024-01-01", "count": 45 },
    { "date": "2024-01-02", "count": 52 }
  ],
  "statusCodesBreakdown": [
    { "statusCode": 200, "count": 800 },
    { "statusCode": 201, "count": 300 },
    { "statusCode": 400, "count": 50 },
    { "statusCode": 500, "count": 100 }
  ]
}
```

#### 3. Get Recent Activities
```http
GET /api/UserActivity/recent?limit=100
Authorization: Bearer <token>
```

#### 4. Get User Activities by User ID
```http
GET /api/UserActivity/user/1?page=1&pageSize=50
Authorization: Bearer <token>
```

#### 5. Get Failed Activities
```http
GET /api/UserActivity/failed?page=1&pageSize=50
Authorization: Bearer <token>
```

## Implementation Details

### BaseController Integration
All controllers inherit from `BaseController` which provides logging methods:

```csharp
// Simple activity logging
await LogUserActivityAsync("Create", "User", userId: 5, description: "Created new user");

// Logging with object details (serialized to JSON)
await LogUserActivityWithDetailsAsync("Update", "Loan", userId: 3, detailsObject: loanData);

// Execute with automatic logging and performance tracking
var result = await ExecuteWithLoggingAsync(
    async () => await _service.CreateAsync(data),
    "Create",
    "User",
    description: "Created new user",
    details: data
);
```

### Automatic Logging
The system automatically logs:
- **User Information:** ID, name, role
- **Request Details:** HTTP method, endpoint, IP address, user agent
- **Response Information:** Status code, success/failure
- **Performance:** Response time in milliseconds
- **Error Details:** Error messages for failed operations

### Logging Levels

#### 1. **View Operations**
- Action: "View"
- Examples: Get users, get loans, get meetings
- Details: Entity type and ID

#### 2. **Create Operations**
- Action: "Create"
- Examples: Create user, create loan, create meeting
- Details: Created entity data

#### 3. **Update Operations**
- Action: "Update"
- Examples: Update user, update loan, update meeting
- Details: Updated entity data

#### 4. **Delete Operations**
- Action: "Delete"
- Examples: Delete user, delete loan, delete meeting
- Details: Deleted entity information

#### 5. **Bulk Operations**
- Action: "BulkCreate", "BulkUpdate", "BulkDelete"
- Examples: Bulk attendance, bulk payments
- Details: Number of items processed

#### 6. **System Operations**
- Action: "Login", "Logout", "Process"
- Examples: User login, loan processing
- Details: Operation-specific information

## Usage Examples

### Controller Implementation
```csharp
[HttpPost]
[Authorize(Roles = "Secretary,President,Treasurer")]
public async Task<ActionResult<User>> CreateUser([FromBody] User user)
{
    try
    {
        // Log the activity
        await LogUserActivityAsync(
            action: "Create",
            entityType: "User",
            description: $"Created user: {user.Name}",
            details: new { user.Name, user.Email }
        );

        // Perform the operation
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
    catch (Exception ex)
    {
        // Log the error
        await LogUserActivityAsync(
            action: "Create",
            entityType: "User",
            description: $"Failed to create user: {user.Name}",
            isSuccess: false,
            errorMessage: ex.Message
        );

        return StatusCode(500, "An error occurred while creating the user");
    }
}
```

### Performance Tracking
```csharp
public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
{
    return await ExecuteWithLoggingAsync(
        async () => await _context.Users.ToListAsync(),
        "View",
        "User",
        description: "Retrieved all users"
    );
}
```

## Security Considerations

### Access Control
- **Admin Only:** Only Secretary, President, and Treasurer can view activity logs
- **No Personal Data:** Sensitive information is not logged
- **Audit Trail:** All admin actions are also logged

### Data Privacy
- **IP Addresses:** Stored for security analysis
- **User Agents:** Stored for compatibility tracking
- **Request Details:** Stored for debugging purposes
- **Error Messages:** Stored for troubleshooting

### Performance Impact
- **Asynchronous Logging:** Activities are logged asynchronously
- **Non-Blocking:** Logging failures don't affect main operations
- **Indexed Queries:** Database indexes ensure fast retrieval
- **Pagination:** Large datasets are paginated

## Monitoring and Alerts

### Key Metrics to Monitor
1. **Activity Volume:** Number of activities per day
2. **Error Rate:** Percentage of failed operations
3. **Response Times:** Average and peak response times
4. **User Activity:** Most active users and operations
5. **System Health:** Database performance and storage

### Alert Thresholds
- **High Error Rate:** >5% failed operations
- **Slow Response:** >5 seconds average response time
- **Storage Warning:** >80% database storage used
- **Unusual Activity:** Sudden spikes in activity volume

## Best Practices

### 1. **Meaningful Descriptions**
```csharp
// Good
await LogUserActivityAsync("Create", "User", description: "Created new member user");

// Avoid
await LogUserActivityAsync("Create", "User", description: "Created user");
```

### 2. **Appropriate Detail Level**
```csharp
// Include relevant details
await LogUserActivityWithDetailsAsync("Update", "Loan", detailsObject: new {
    LoanId = loan.Id,
    OldAmount = oldAmount,
    NewAmount = loan.Amount,
    Reason = "Amount adjustment"
});
```

### 3. **Error Handling**
```csharp
try
{
    // Operation
    await LogUserActivityAsync("Create", "User", isSuccess: true);
}
catch (Exception ex)
{
    await LogUserActivityAsync("Create", "User", isSuccess: false, errorMessage: ex.Message);
    throw;
}
```

### 4. **Performance Considerations**
- Use `ExecuteWithLoggingAsync` for automatic performance tracking
- Log activities asynchronously to avoid blocking
- Include relevant details but avoid excessive data

## Migration and Setup

### Database Migration
The system includes a migration that creates the UserActivity table:
```bash
dotnet ef migrations add AddUserActivityTable
dotnet ef database update
```

### Service Registration
The UserActivityService is automatically registered in Program.cs:
```csharp
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
```

### Controller Updates
All controllers that inherit from BaseController automatically get logging capabilities.

## Troubleshooting

### Common Issues

#### 1. **Missing User Information**
- Ensure user is authenticated
- Check JWT token contains user claims
- Verify user exists in database

#### 2. **Performance Issues**
- Check database indexes
- Monitor query performance
- Consider archiving old activities

#### 3. **Storage Issues**
- Implement data retention policies
- Archive old activities
- Monitor database size

#### 4. **Logging Failures**
- Check database connectivity
- Verify service registration
- Monitor error logs

### Debugging
```csharp
// Enable detailed logging
_logger.LogDebug("Logging activity: {Action} on {EntityType}", action, entityType);

// Check service availability
if (_userActivityService == null)
{
    _logger.LogError("UserActivityService is not available");
}
```

The user activity logging system provides comprehensive tracking of all user actions while maintaining performance and security. It serves as a powerful tool for audit trails, security monitoring, and system analysis. 