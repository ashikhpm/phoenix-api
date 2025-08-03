# GetLoansDue Method - UserId Parameter Implementation

## Changes Made

### 1. Method Signature Update
**DashboardController.cs** - Added optional `userId` parameter:

```csharp
[HttpGet("loans-due")]
public async Task<ActionResult<LoanDueResponse>> GetLoansDue([FromQuery] int? userId = null)
```

### 2. Enhanced Query Logic
Updated the query logic to handle the `userId` parameter:

```csharp
// Check if current user is admin
bool isAdminUser = string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase);

// Build base query based on user role and userId parameter
IQueryable<Loan> baseQuery;
if (isAdminUser)
{
    if (userId.HasValue)
    {
        // Admin user requesting specific user's loans
        _logger.LogInformation("Admin user ({Role}) - returning loans due for user {TargetUserId}", currentUser.UserRole?.Name, userId.Value);
        baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == userId.Value);
    }
    else
    {
        // Admin user requesting all loans (current behavior)
        _logger.LogInformation("Admin user ({Role}) - returning all loans due", currentUser.UserRole?.Name);
        baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType);
    }
}
else
{
    // Regular user can only see their own loans (current behavior)
    _logger.LogInformation("Regular user - returning only user's loans due. User ID: {UserId}", currentUserId);
    baseQuery = _context.Loans.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == currentUserId);
}
```

## Functionality

### 1. **When userId is null (default behavior)**
- **Admin Users**: Get all loans due in the system
- **Regular Users**: Get only their own loans due

### 2. **When userId has a value**
- **Admin Users**: Get loans due for the specific user ID
- **Regular Users**: Still get only their own loans (userId parameter is ignored for non-admin users)

## API Usage Examples

### 1. **Get Current User's Loans Due (Default)**
```http
GET /api/dashboard/loans-due
```
- Returns loans due for the authenticated user
- Works for both admin and regular users

### 2. **Get Specific User's Loans Due (Admin Only)**
```http
GET /api/dashboard/loans-due?userId=123
```
- Returns loans due for user with ID 123
- Only works for admin users (Secretary, President, Treasurer)
- Regular users will ignore the userId parameter and get their own loans

### 3. **Get All Loans Due (Admin Only)**
```http
GET /api/dashboard/loans-due
```
- When called by admin users, returns all loans due in the system
- When called by regular users, returns only their own loans

## Security Features

### 1. **Role-Based Access Control**
- Admin users can filter by specific user ID
- Regular users can only access their own loans
- No unauthorized access to other users' loan data

### 2. **Backward Compatibility**
- Existing API calls continue to work without modification
- No breaking changes for current clients

### 3. **Logging**
- Comprehensive logging for different scenarios
- Tracks which user is requesting which data

## Response Format

The response format remains unchanged:

```json
{
  "overdueLoans": [
    {
      "id": 1,
      "userId": 123,
      "userName": "John Doe",
      "date": "2024-01-01T00:00:00Z",
      "dueDate": "2024-02-01T00:00:00Z",
      "amount": 1000.00,
      "interestAmount": 50.00,
      "isOverdue": true,
      "daysOverdue": 30
    }
  ],
  "dueTodayLoans": [...],
  "dueThisWeekLoans": [...],
  "totalOverdueCount": 5,
  "totalDueTodayCount": 2,
  "totalDueThisWeekCount": 8
}
```

## Benefits

### 1. **Enhanced Flexibility**
- Admin users can now query loans for specific users
- Maintains backward compatibility for existing clients
- No changes needed for regular user functionality

### 2. **Improved Admin Experience**
- Admins can easily check loans for any user
- No need to switch accounts or use different endpoints
- Single endpoint handles all scenarios

### 3. **Security Maintained**
- Regular users cannot access other users' data
- Role-based restrictions are properly enforced
- No security vulnerabilities introduced

## Testing

### 1. Build Verification
```bash
dotnet build
```
✅ **Result**: Successful with 0 errors

### 2. Test Scenarios
- ✅ **Admin user without userId**: Gets all loans
- ✅ **Admin user with userId**: Gets specific user's loans
- ✅ **Regular user without userId**: Gets own loans
- ✅ **Regular user with userId**: Gets own loans (ignores userId)
- ✅ **Backward compatibility**: Existing calls work unchanged

## Summary

✅ **GetLoansDue Method Enhanced**
- Added optional `userId` parameter
- Implemented role-based filtering logic
- Maintained backward compatibility
- Enhanced admin functionality without compromising security

The method now provides flexible access to loan due information while maintaining security and proper authorization controls. 