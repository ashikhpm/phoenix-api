# Loan Request Activity Logging Guide

## Overview
The loan request processing functionality has been enhanced with comprehensive user activity logging to track all approval and rejection actions with detailed context and performance metrics.

## Enhanced Features

### 1. Comprehensive Activity Logging
- **Success Tracking**: Detailed logging of successful loan request processing
- **Error Tracking**: Comprehensive error logging with context
- **Performance Metrics**: Execution time tracking for all operations
- **Change Tracking**: Detailed tracking of all field changes

### 2. Enhanced DashboardController
- **BaseController Inheritance**: Now inherits from BaseController for consistent logging
- **UserActivityService Integration**: Injected and used for all activity tracking
- **Structured Logging**: Uses consistent logging patterns across all methods

## API Endpoint

### URL
```
PUT /api/dashboard/loan-requests/{id}/action
```

### Authorization
- **Roles Required**: Secretary, President, Treasurer
- **Authentication**: JWT Token required

### Request Body
```json
{
  "action": "Accepted",
  "description": "Loan approved with additional terms",
  "chequeNumber": "CHQ123456"
}
```

### Response
```json
{
  "id": 1,
  "userId": 5,
  "userName": "John Doe",
  "date": "2023-01-15T00:00:00Z",
  "dueDate": "2023-07-15T00:00:00Z",
  "loanTypeId": 1,
  "loanTypeName": "Personal Loan",
  "interestRate": 12.5,
  "amount": 50000.00,
  "description": "Loan approved with additional terms",
  "chequeNumber": "CHQ123456",
  "status": "Accepted",
  "requestDate": "2023-01-15T00:00:00Z",
  "processedDate": "2023-01-20T10:30:00Z",
  "processedByUserName": "Secretary Name"
}
```

## Activity Logging Details

### Success Scenario Logging
When a loan request is successfully processed, the system logs:

```json
{
  "operation": "Process",
  "entityType": "LoanRequest",
  "entityId": 1,
  "description": "Loan request accepted for user John Doe",
  "details": {
    "requestId": 1,
    "userId": 5,
    "userName": "John Doe",
    "action": "Accepted",
    "amount": 50000.00,
    "loanType": "Personal Loan",
    "originalStatus": "Pending",
    "newStatus": "Accepted",
    "originalDescription": "Initial request",
    "newDescription": "Loan approved with additional terms",
    "originalChequeNumber": null,
    "newChequeNumber": "CHQ123456",
    "processedByUserId": 10,
    "processedDate": "2023-01-20T10:30:00Z",
    "newLoanCreated": true,
    "newLoanId": 25,
    "emailSent": true,
    "changes": {
      "statusChanged": true,
      "descriptionChanged": true,
      "chequeNumberChanged": true
    }
  },
  "isSuccess": true,
  "executionTimeMs": 1250
}
```

### Error Scenario Logging
When errors occur, the system logs detailed error information:

```json
{
  "operation": "Process",
  "entityType": "LoanRequest",
  "entityId": 1,
  "description": "Failed to process loan request - Request not found",
  "details": {
    "action": "Accepted",
    "description": "Loan approved",
    "chequeNumber": "CHQ123456"
  },
  "isSuccess": false,
  "errorMessage": "Loan request not found",
  "executionTimeMs": 45
}
```

## Implementation Details

### Enhanced ProcessLoanRequest Method
The method now includes:

1. **Stopwatch Tracking**: Performance measurement
2. **Comprehensive Validation**: Model state, request existence, action validity
3. **Original Value Capture**: Tracks changes for detailed logging
4. **Transaction Safety**: All operations in single transaction
5. **Email Integration**: Tracks email success/failure
6. **Loan Creation**: Tracks new loan creation for accepted requests

### Key Logging Points

#### 1. Model Validation
```csharp
if (!ModelState.IsValid)
{
    await LogUserActivityAsync("Process", "LoanRequest", id, 
        "Failed to process loan request - Invalid model state", 
        actionDto, false, "Invalid model state", stopwatch.ElapsedMilliseconds);
    return BadRequest(ModelState);
}
```

#### 2. Request Not Found
```csharp
if (loanRequest == null)
{
    LogWarning("Loan request with ID {RequestId} not found", id);
    await LogUserActivityAsync("Process", "LoanRequest", id, 
        "Failed to process loan request - Request not found", 
        actionDto, false, "Loan request not found", stopwatch.ElapsedMilliseconds);
    return NotFound("Loan request not found");
}
```

#### 3. Invalid Action
```csharp
if (action != "Accepted" && action != "Rejected")
{
    LogWarning("Invalid action for loan request {RequestId}: {Action}", id, action);
    await LogUserActivityAsync("Process", "LoanRequest", id, 
        "Failed to process loan request - Invalid action", 
        actionDto, false, "Action must be 'Accepted' or 'Rejected'", stopwatch.ElapsedMilliseconds);
    return BadRequest("Action must be 'accepted' or 'rejected'");
}
```

#### 4. User Authentication
```csharp
var currentUserId = GetCurrentUserId();
if (!currentUserId.HasValue)
{
    LogWarning("Secretary user ID not found in token");
    await LogUserActivityAsync("Process", "LoanRequest", id, 
        "Failed to process loan request - User ID not found in token", 
        actionDto, false, "Secretary user ID not found in token", stopwatch.ElapsedMilliseconds);
    return BadRequest("Secretary user ID not found in token");
}
```

#### 5. Success Logging
```csharp
await LogUserActivityWithDetailsAsync("Process", "LoanRequest", id, 
    $"Loan request {action.ToLower()} for user {loanRequest.User?.Name}", 
    new { 
        RequestId = id, 
        UserId = loanRequest.UserId,
        UserName = loanRequest.User?.Name,
        Action = action,
        Amount = loanRequest.Amount,
        LoanType = loanRequest.LoanType?.LoanTypeName,
        OriginalStatus = originalStatus,
        NewStatus = action,
        // ... additional details
    }, isSuccess, errorMessage, stopwatch.ElapsedMilliseconds);
```

## Benefits

### 1. Audit Trail
- ✅ Complete tracking of all loan request decisions
- ✅ Detailed change history for compliance
- ✅ Performance metrics for optimization
- ✅ User accountability for all actions

### 2. Error Handling
- ✅ Comprehensive error logging with context
- ✅ Detailed error messages for troubleshooting
- ✅ Performance tracking for error scenarios
- ✅ User-friendly error responses

### 3. Data Integrity
- ✅ Transaction safety for all operations
- ✅ Automatic email notification tracking
- ✅ Loan creation tracking for accepted requests
- ✅ Change tracking for all modified fields

### 4. Performance Monitoring
- ✅ Execution time tracking for all operations
- ✅ Performance metrics for optimization
- ✅ Detailed timing for each operation step
- ✅ Performance alerts for slow operations

## Usage Examples

### Approve Loan Request
```bash
curl -X PUT "https://api.example.com/api/dashboard/loan-requests/1/action" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "action": "Accepted",
    "description": "Loan approved with standard terms",
    "chequeNumber": "CHQ123456"
  }'
```

### Reject Loan Request
```bash
curl -X PUT "https://api.example.com/api/dashboard/loan-requests/1/action" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "action": "Rejected",
    "description": "Insufficient credit history"
  }'
```

## Error Scenarios

### 1. Invalid Action (400)
```json
{
  "message": "Action must be 'accepted' or 'rejected'"
}
```

### 2. Request Not Found (404)
```json
{
  "message": "Loan request not found"
}
```

### 3. Invalid Model State (400)
```json
{
  "action": ["The Action field is required."]
}
```

### 4. User Not Authenticated (401)
```json
{
  "message": "Secretary user ID not found in token"
}
```

### 5. Server Error (500)
```json
{
  "message": "An error occurred while processing the loan request"
}
```

## Activity Log Categories

### 1. Process Operations
- **Success**: Loan request processed successfully
- **Validation Errors**: Model state, request existence, action validity
- **Authentication Errors**: User ID not found in token
- **System Errors**: Database or email service errors

### 2. Change Tracking
- **Status Changes**: Pending → Accepted/Rejected
- **Description Updates**: Additional notes or terms
- **Cheque Number**: Payment reference updates
- **Loan Creation**: New loan record for accepted requests

### 3. Email Integration
- **Email Success**: Notification sent successfully
- **Email Failure**: Failed to send notification
- **Email Errors**: Exception during email sending

### 4. Performance Metrics
- **Execution Time**: Total processing time
- **Database Operations**: Save operation timing
- **Email Operations**: Email sending timing
- **Validation Timing**: Input validation timing

## Migration Notes

### Existing Functionality
- ✅ No breaking changes to existing API
- ✅ Enhanced logging without affecting performance
- ✅ Backward compatible with existing clients
- ✅ Improved error handling and user feedback

### Database Considerations
- ✅ No schema changes required
- ✅ Existing loan request data preserved
- ✅ User activity logging table handles all new logs
- ✅ Performance impact minimal

## Testing Scenarios

### 1. Successful Approval
- Submit valid approval request
- Verify loan request status updated
- Check new loan record created
- Confirm email notification sent
- Validate activity log details

### 2. Successful Rejection
- Submit valid rejection request
- Verify loan request status updated
- Confirm no new loan record created
- Check email notification sent
- Validate activity log details

### 3. Error Scenarios
- Test with invalid action
- Test with non-existent request
- Test with invalid model state
- Test with missing authentication
- Verify proper error logging

### 4. Performance Testing
- Monitor execution times
- Test with large amounts
- Verify email timing
- Check database performance
- Validate logging performance 