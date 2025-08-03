# User Activity Logging Implementation Guide

## Overview
This guide explains how to implement comprehensive user activity logging for all actions in the Phoenix Sangam API, including loan creation and other operations.

## Current Status

### ✅ What's Already Implemented
1. **UserActivity Model**: Complete model with all necessary fields
2. **UserActivityService**: Comprehensive service with logging and filtering capabilities
3. **BaseController**: Base controller with logging helper methods
4. **Database Schema**: UserActivities table with proper indexes

### ❌ What Needs to be Implemented
1. **Controller Integration**: All controllers need to inherit from BaseController
2. **Action Logging**: All CRUD operations need activity logging
3. **Error Logging**: Failed operations need to be logged
4. **Performance Tracking**: Response times need to be tracked

## Implementation Steps

### 1. Update Controller Inheritance

All controllers should inherit from `BaseController` instead of `ControllerBase`:

```csharp
// Before
public class LoanController : ControllerBase

// After
public class LoanController : BaseController
```

### 2. Update Controller Constructor

Add `IUserActivityService` to the constructor:

```csharp
// Before
public LoanController(UserDbContext context, ILogger<LoanController> logger, IEmailService emailService)

// After
public LoanController(UserDbContext context, ILogger<LoanController> logger, IEmailService emailService, IUserActivityService userActivityService)
    : base(context, logger, userActivityService)
{
    _emailService = emailService;
}
```

### 3. Implement Activity Logging in Methods

#### For GET Operations (View Actions)
```csharp
[HttpGet]
[Authorize]
public async Task<ActionResult<IEnumerable<LoanWithInterestDto>>> GetAllLoans()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var isSuccess = false;
    string? errorMessage = null;

    try
    {
        LogOperation("Getting loans list");
        
        // Your existing logic here
        var loans = await _context.Loans.Include(l => l.User).Include(l => l.LoanType).ToListAsync();
        
        isSuccess = true;
        await LogUserActivityAsync("View", "Loan", null, $"Retrieved {loans.Count} loans", 
            new { Count = loans.Count }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
        
        return Ok(loans);
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        LogError(ex, "Error retrieving loans list");
        await LogUserActivityAsync("View", "Loan", null, "Error retrieving loans list", 
            null, false, errorMessage, stopwatch.ElapsedMilliseconds);
        return StatusCode(500, "An error occurred while retrieving loans list");
    }
}
```

#### For POST Operations (Create Actions)
```csharp
[HttpPost]
[Authorize(Roles = "Secretary,President,Treasurer")]
public async Task<ActionResult<LoanWithInterestDto>> CreateLoan([FromBody] CreateLoanDto loanDto)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var isSuccess = false;
    string? errorMessage = null;

    try
    {
        if (!ModelState.IsValid)
        {
            await LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - Invalid model state", 
                loanDto, false, "Invalid model state", stopwatch.ElapsedMilliseconds);
            return BadRequest(ModelState);
        }
        
        // Your existing validation logic
        var user = await _context.Users.FindAsync(loanDto.UserId);
        if (user == null)
        {
            await LogUserActivityAsync("Create", "Loan", null, "Failed to create loan - User not found", 
                loanDto, false, "User not found", stopwatch.ElapsedMilliseconds);
            return BadRequest("User not found");
        }
        
        // Your existing creation logic
        var loan = new Loan { /* ... */ };
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
        
        isSuccess = true;
        await LogUserActivityWithDetailsAsync("Create", "Loan", loan.Id, 
            $"Created loan for user {user.Name} with amount {loan.Amount}", 
            new { 
                LoanId = loan.Id, 
                UserId = loan.UserId, 
                UserName = user.Name, 
                Amount = loan.Amount, 
                LoanType = loanType.LoanTypeName,
                DueDate = loan.DueDate,
                Status = loan.Status
            }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
        
        return Ok(loanWithInterest);
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        LogError(ex, "Error creating loan");
        await LogUserActivityAsync("Create", "Loan", null, "Error creating loan", 
            loanDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
        return StatusCode(500, "An error occurred while creating the loan");
    }
}
```

#### For PUT Operations (Update Actions)
```csharp
[HttpPut("{id}")]
[Authorize(Roles = "Secretary,President,Treasurer")]
public async Task<IActionResult> UpdateLoan(int id, [FromBody] CreateLoanDto loanDto)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var isSuccess = false;
    string? errorMessage = null;

    try
    {
        var existingLoan = await _context.Loans.FindAsync(id);
        if (existingLoan == null)
        {
            await LogUserActivityAsync("Update", "Loan", id, "Failed to update loan - Loan not found", 
                loanDto, false, "Loan not found", stopwatch.ElapsedMilliseconds);
            return NotFound("Loan not found");
        }
        
        // Your existing update logic
        existingLoan.Amount = loanDto.Amount;
        // ... other updates
        
        await _context.SaveChangesAsync();
        
        isSuccess = true;
        await LogUserActivityWithDetailsAsync("Update", "Loan", id, 
            $"Updated loan {id} for user {user.Name}", 
            new { 
                LoanId = id, 
                UserId = existingLoan.UserId, 
                Amount = existingLoan.Amount,
                Status = existingLoan.Status
            }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
        
        return Ok(existingLoan);
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        LogError(ex, "Error updating loan");
        await LogUserActivityAsync("Update", "Loan", id, "Error updating loan", 
            loanDto, false, errorMessage, stopwatch.ElapsedMilliseconds);
        return StatusCode(500, "An error occurred while updating the loan");
    }
}
```

#### For DELETE Operations (Delete Actions)
```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Secretary,President,Treasurer")]
public async Task<IActionResult> DeleteLoan(int id)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var isSuccess = false;
    string? errorMessage = null;

    try
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
        {
            await LogUserActivityAsync("Delete", "Loan", id, "Failed to delete loan - Loan not found", 
                null, false, "Loan not found", stopwatch.ElapsedMilliseconds);
            return NotFound("Loan not found");
        }
        
        _context.Loans.Remove(loan);
        await _context.SaveChangesAsync();
        
        isSuccess = true;
        await LogUserActivityWithDetailsAsync("Delete", "Loan", id, 
            $"Deleted loan {id} for user {loan.User?.Name}", 
            new { 
                LoanId = id, 
                UserId = loan.UserId, 
                UserName = loan.User?.Name,
                Amount = loan.Amount
            }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
        
        return NoContent();
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        LogError(ex, "Error deleting loan");
        await LogUserActivityAsync("Delete", "Loan", id, "Error deleting loan", 
            null, false, errorMessage, stopwatch.ElapsedMilliseconds);
        return StatusCode(500, "An error occurred while deleting the loan");
    }
}
```

## Controllers That Need Updates

### 1. LoanController
- ✅ Already updated to inherit from BaseController
- ❌ Needs activity logging in all methods

### 2. UserController
- ❌ Needs to inherit from BaseController
- ❌ Needs activity logging in all methods

### 3. MeetingController
- ❌ Needs to inherit from BaseController
- ❌ Needs activity logging in all methods

### 4. AttendanceController
- ❌ Needs to inherit from BaseController
- ❌ Needs activity logging in all methods

### 5. MeetingPaymentController
- ❌ Needs to inherit from BaseController
- ❌ Needs activity logging in all methods

### 6. DashboardController
- ❌ Needs to inherit from BaseController
- ❌ Needs activity logging in all methods

## Activity Logging Best Practices

### 1. Action Names
Use consistent action names:
- `View` - For GET operations
- `Create` - For POST operations
- `Update` - For PUT operations
- `Delete` - For DELETE operations
- `Login` - For authentication
- `Logout` - For logout
- `Export` - For data exports
- `Import` - For data imports

### 2. Entity Types
Use consistent entity type names:
- `Loan` - For loan operations
- `User` - For user operations
- `Meeting` - For meeting operations
- `Attendance` - For attendance operations
- `Payment` - For payment operations
- `Dashboard` - For dashboard operations

### 3. Descriptions
Write clear, descriptive messages:
- ✅ `"Created loan for user John Doe with amount 50000"`
- ✅ `"Updated loan 123 status to 'Closed'"`
- ✅ `"Deleted loan 456 for user Jane Smith"`
- ❌ `"Loan operation"`

### 4. Details Object
Include relevant information in the details:
```csharp
new { 
    LoanId = loan.Id, 
    UserId = loan.UserId, 
    UserName = user.Name, 
    Amount = loan.Amount, 
    LoanType = loanType.LoanTypeName,
    DueDate = loan.DueDate,
    Status = loan.Status
}
```

### 5. Error Handling
Always log errors with:
- Error message
- Failed operation details
- Duration of the attempt

## Testing Activity Logging

### 1. Test Successful Operations
```bash
# Create a loan
curl -X POST /api/loan -H "Authorization: Bearer <token>" -d '{"userId": 1, "amount": 50000}'

# Check activity log
curl -X GET /api/useractivity -H "Authorization: Bearer <token>"
```

### 2. Test Failed Operations
```bash
# Try to create loan with invalid data
curl -X POST /api/loan -H "Authorization: Bearer <token>" -d '{"userId": 999, "amount": -100}'

# Check activity log for error entry
curl -X GET /api/useractivity -H "Authorization: Bearer <token>" -d '{"isSuccess": false}'
```

### 3. Test Performance Tracking
```bash
# Check duration of operations
curl -X GET /api/useractivity -H "Authorization: Bearer <token>" -d '{"minDurationMs": 100}'
```

## Monitoring and Analytics

### 1. Activity Statistics
Use the `GetActivityStatisticsAsync` method to get:
- Total activities
- Success/failure rates
- Average response times
- Most active users
- Most common actions

### 2. Filtering and Search
Use the `GetUserActivitiesWithFilterAsync` method to:
- Filter by user
- Filter by action type
- Filter by date range
- Filter by success/failure
- Search by description

### 3. Performance Monitoring
Track:
- Response times
- Error rates
- User activity patterns
- System usage trends

## Security Considerations

### 1. Sensitive Data
- Don't log sensitive information (passwords, tokens)
- Mask personal data in logs
- Use appropriate data retention policies

### 2. Access Control
- Only authorized users can view activity logs
- Implement role-based access to logs
- Audit log access itself

### 3. Data Privacy
- Comply with data protection regulations
- Implement data retention policies
- Provide data deletion capabilities

## Implementation Checklist

### Phase 1: Core Controllers
- [ ] Update LoanController with activity logging
- [ ] Update UserController with activity logging
- [ ] Update MeetingController with activity logging

### Phase 2: Supporting Controllers
- [ ] Update AttendanceController with activity logging
- [ ] Update MeetingPaymentController with activity logging
- [ ] Update DashboardController with activity logging

### Phase 3: Testing and Validation
- [ ] Test all CRUD operations
- [ ] Test error scenarios
- [ ] Validate performance tracking
- [ ] Test filtering and search

### Phase 4: Monitoring
- [ ] Set up activity monitoring
- [ ] Create activity reports
- [ ] Implement alerting for unusual activity
- [ ] Document monitoring procedures

## Expected Results

After implementation, you should see:

### 1. Comprehensive Activity Logs
- Every user action logged
- Performance metrics tracked
- Error scenarios captured
- Detailed audit trail

### 2. Improved Monitoring
- Real-time activity visibility
- Performance insights
- User behavior patterns
- System usage analytics

### 3. Enhanced Security
- Complete audit trail
- Suspicious activity detection
- Compliance with regulations
- Data protection measures

### 4. Better User Experience
- Faster issue resolution
- Proactive problem detection
- Improved system reliability
- Enhanced support capabilities

## Conclusion

Implementing comprehensive user activity logging will provide:
- ✅ Complete audit trail for all operations
- ✅ Performance monitoring and optimization
- ✅ Security and compliance benefits
- ✅ Better system monitoring and debugging
- ✅ Enhanced user support capabilities

The implementation should be done systematically across all controllers to ensure consistent logging throughout the application. 