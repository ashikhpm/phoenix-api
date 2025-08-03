# UpdateUser Enhancement Guide

## Overview
The `UpdateUser` endpoint has been enhanced to handle joining date updates and automatically synchronize email changes with the UserLogin table.

## Enhanced Features

### 1. Joining Date Update
- **New Functionality**: The endpoint now updates the `JoiningDate` field
- **Validation**: Joining date can be set to null or a valid date
- **Logging**: All joining date changes are logged with detailed tracking

### 2. Email Synchronization with UserLogin
- **Automatic Sync**: When email is changed, the corresponding UserLogin record is automatically updated
- **Username Update**: The UserLogin `Username` field is updated to match the new email
- **Error Handling**: Logs warning if no UserLogin record is found
- **Transaction Safety**: All changes are saved in a single transaction

## API Endpoint

### URL
```
PUT /api/user/{id}
```

### Authorization
- **Roles Required**: Secretary, President, Treasurer
- **Authentication**: JWT Token required

### Request Body
```json
{
  "name": "Updated User Name",
  "address": "Updated Address",
  "email": "updated.email@example.com",
  "phone": "9876543210",
  "aadharNumber": "123456789012",
  "joiningDate": "2023-01-15T00:00:00Z",
  "userRoleId": 1,
  "isActive": true
}
```

### Response
```json
{
  "id": 1,
  "name": "Updated User Name",
  "address": "Updated Address",
  "email": "updated.email@example.com",
  "phone": "9876543210",
  "aadharNumber": "123456789012",
  "isActive": true,
  "inactiveDate": null,
  "joiningDate": "2023-01-15T00:00:00Z",
  "userRoleId": 1,
  "userRole": {
    "id": 1,
    "name": "Member",
    "description": "Regular member"
  }
}
```

## Implementation Details

### Email Change Detection
```csharp
var emailChanged = originalEmail.ToLower() != user.Email.ToLower();
```

### UserLogin Synchronization
```csharp
if (emailChanged)
{
    var userLogin = await _context.UserLogins
        .Where(ul => ul.UserId == id)
        .FirstOrDefaultAsync();
    
    if (userLogin != null)
    {
        userLogin.Username = user.Email; // Update username to match new email
    }
}
```

### Transaction Safety
- All updates (User and UserLogin) are performed in a single transaction
- If any part fails, the entire operation is rolled back
- Ensures data consistency between User and UserLogin tables

## Activity Logging

### Enhanced Logging Details
The endpoint now logs comprehensive information about all changes:

```json
{
  "userId": 1,
  "userName": "Updated User Name",
  "email": "updated.email@example.com",
  "originalName": "Original User Name",
  "originalEmail": "original.email@example.com",
  "originalJoiningDate": "2023-01-01T00:00:00Z",
  "newJoiningDate": "2023-01-15T00:00:00Z",
  "emailChanged": true,
  "userLoginUpdated": true,
  "changes": {
    "nameChanged": true,
    "emailChanged": true,
    "joiningDateChanged": true,
    "addressChanged": true,
    "phoneChanged": true,
    "aadharNumberChanged": true
  }
}
```

### Logging Categories
- **Email Change**: Tracks email modifications and UserLogin sync
- **Joining Date Change**: Tracks joining date updates
- **All Field Changes**: Comprehensive tracking of all modified fields
- **Error Scenarios**: Detailed logging for validation failures and errors

## Validation Rules

### Required Fields
- `name`: Cannot be null or empty
- `email`: Must be a valid email format

### Email Uniqueness
- Email must be unique across all users
- Cannot use an email that belongs to another user

### Joining Date
- Can be null (for users without a recorded joining date)
- Must be a valid date if provided
- No future date restrictions (allows correction of historical data)

## Error Scenarios

### 1. User Not Found (404)
```json
{
  "message": "User with ID 999 not found"
}
```

### 2. Invalid Data (400)
```json
{
  "message": "Name and Email are required"
}
```

### 3. Email Already Exists (400)
```json
{
  "message": "Email already exists"
}
```

### 4. UserLogin Not Found (Warning)
- Logs a warning if email is changed but no UserLogin record exists
- Does not fail the operation
- Allows for manual intervention if needed

## Usage Examples

### Update User with Email Change
```bash
curl -X PUT "https://api.example.com/api/user/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe Updated",
    "email": "john.updated@example.com",
    "phone": "9876543210",
    "joiningDate": "2023-01-15T00:00:00Z"
  }'
```

### Update User Joining Date Only
```bash
curl -X PUT "https://api.example.com/api/user/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com",
    "joiningDate": "2023-02-01T00:00:00Z"
  }'
```

## Benefits

### 1. Data Consistency
- Automatic synchronization between User and UserLogin tables
- Prevents login issues when email is changed
- Maintains referential integrity

### 2. Comprehensive Tracking
- Detailed logging of all changes
- Performance metrics for all operations
- Audit trail for compliance requirements

### 3. Enhanced User Management
- Flexible joining date management
- Support for historical data correction
- Improved user experience with automatic email sync

### 4. Error Handling
- Graceful handling of missing UserLogin records
- Comprehensive validation
- Detailed error messages for troubleshooting

## Migration Notes

### Existing Users
- Users without UserLogin records will log warnings when email is changed
- No impact on existing functionality
- Manual creation of UserLogin records may be needed for some users

### Database Considerations
- Ensure UserLogin table has proper foreign key constraints
- Consider adding indexes on UserId for better performance
- Monitor transaction logs for any sync issues

## Testing Scenarios

### 1. Email Change with UserLogin Sync
- Change user email
- Verify UserLogin username is updated
- Confirm login still works with new email

### 2. Joining Date Update
- Update joining date to different value
- Verify change is logged
- Check meeting attendance calculations

### 3. Multiple Field Updates
- Update name, email, joining date simultaneously
- Verify all changes are logged
- Confirm UserLogin sync works

### 4. Error Scenarios
- Try to use existing email
- Test with invalid email format
- Verify proper error responses 