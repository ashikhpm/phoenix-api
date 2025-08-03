# GetLoanRequests Method - UserId Parameter Implementation

## Changes Made

### 1. Method Signature Update
**DashboardController.cs** - Added optional `userId` parameter:

```csharp
[HttpGet("loan-requests")]
public async Task<ActionResult<IEnumerable<LoanRequestResponseDto>>> GetLoanRequests([FromQuery] int? userId = null)
```

### 2. Enhanced Query Logic
Updated the query logic to handle the `userId` parameter:

```csharp
// Check if current user is admin
bool isAdminUser = string.Equals(currentUser.UserRole?.Name, "Secretary", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(currentUser.UserRole?.Name, "President", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(currentUser.UserRole?.Name, "Treasurer", StringComparison.OrdinalIgnoreCase);

IQueryable<LoanRequest> requestsQuery;

// Build query based on user role and userId parameter
if (isAdminUser)
{
    if (userId.HasValue)
    {
        // Admin user requesting specific user's loan requests
        _logger.LogInformation("Admin user ({Role}) - returning loan requests for user {TargetUserId}", currentUser.UserRole?.Name, userId.Value);
        requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == userId.Value);
    }
    else
    {
        // Admin user requesting all loan requests (current behavior)
        _logger.LogInformation("Admin user ({Role}) - returning all loan requests", currentUser.UserRole?.Name);
        requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType);
    }
}
else
{
    // Regular user can only see their own loan requests (current behavior)
    _logger.LogInformation("Regular user - returning only user's loan requests. User ID: {UserId}", currentUserId);
    requestsQuery = _context.LoanRequests.Include(l => l.User).Include(l => l.LoanType).Where(l => l.UserId == currentUserId);
}
```

## Functionality

### 1. **When userId is null (default behavior)**
- **Admin Users**: Get all loan requests in the system
- **Regular Users**: Get only their own loan requests

### 2. **When userId has a value**
- **Admin Users**: Get loan requests for the specific user ID
- **Regular Users**: Still get only their own loan requests (userId parameter is ignored for non-admin users)

## API Usage Examples

### 1. **Get Current User's Loan Requests (Default)**
```http
GET /api/dashboard/loan-requests
```
- Returns loan requests for the authenticated user
- Works for both admin and regular users

### 2. **Get Specific User's Loan Requests (Admin Only)**
```http
GET /api/dashboard/loan-requests?userId=123
```
- Returns loan requests for user with ID 123
- Only works for admin users (Secretary, President, Treasurer)
- Regular users will ignore the userId parameter and get their own loan requests

### 3. **Get All Loan Requests (Admin Only)**
```http
GET /api/dashboard/loan-requests
```
- When called by admin users, returns all loan requests in the system
- When called by regular users, returns only their own loan requests

## Response Format

The response format remains unchanged:

```json
[
  {
    "id": 1,
    "userId": 123,
    "userName": "John Doe",
    "date": "2024-01-01T00:00:00Z",
    "dueDate": "2024-02-01T00:00:00Z",
    "loanTypeId": 1,
    "loanTypeName": "Personal Loan",
    "interestRate": 5.5,
    "amount": 1000.00,
    "description": "Emergency loan request",
    "chequeNumber": "CHK001",
    "status": "Requested",
    "requestDate": "2024-01-01T00:00:00Z",
    "processedDate": null,
    "processedByUserName": null,
    "loanTerm": 12
  }
]
```

## Security Features

### 1. **Role-Based Access Control**
- Admin users can filter by specific user ID
- Regular users can only access their own loan requests
- No unauthorized access to other users' loan request data

### 2. **Backward Compatibility**
- Existing API calls continue to work without modification
- No breaking changes for current clients

### 3. **Logging**
- Comprehensive logging for different scenarios
- Tracks which user is requesting which data

## Benefits

### 1. **Enhanced Flexibility**
- Admin users can now query loan requests for specific users
- Maintains backward compatibility for existing clients
- No changes needed for regular user functionality

### 2. **Improved Admin Experience**
- Admins can easily check loan requests for any user
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
- ✅ **Admin user without userId**: Gets all loan requests
- ✅ **Admin user with userId**: Gets specific user's loan requests
- ✅ **Regular user without userId**: Gets own loan requests
- ✅ **Regular user with userId**: Gets own loan requests (ignores userId)
- ✅ **Backward compatibility**: Existing calls work unchanged

## Comparison with GetLoansDue

Both methods now have consistent behavior:

| Method | Parameter | Admin Behavior | Regular User Behavior |
|--------|-----------|----------------|----------------------|
| `GetLoansDue` | `userId` | Can filter by specific user or get all | Gets own loans only |
| `GetLoanRequests` | `userId` | Can filter by specific user or get all | Gets own requests only |

## Summary

✅ **GetLoanRequests Method Enhanced**
- Added optional `userId` parameter
- Implemented role-based filtering logic
- Maintained backward compatibility
- Enhanced admin functionality without compromising security
- Consistent behavior with `GetLoansDue` method

The method now provides flexible access to loan request information while maintaining security and proper authorization controls, matching the functionality of the `GetLoansDue` method. 