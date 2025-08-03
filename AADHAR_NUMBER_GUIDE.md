# Aadhar Number Field Implementation Guide

## Overview
This guide documents the implementation of the Aadhar number field in the Phoenix Sangam API. The Aadhar number field has been added to the User model and integrated into all relevant API endpoints.

## Changes Made

### 1. Database Schema Changes

#### User Model (`User.cs`)
- **Added Field**: `AadharNumber` (string, max length 12 characters)
- **Validation**: StringLength(12) attribute for database constraint
- **Default Value**: Empty string

#### Database Migration
- **Migration File**: `20250803071025_AddAadharNumberToUser.cs`
- **Action**: Adds `AadharNumber` column to `Users` table
- **Type**: `character varying(12)` (PostgreSQL)
- **Default**: Empty string for existing records

### 2. API Endpoint Updates

#### Create User (`POST /api/user`)
- **Request Body**: Now includes `AadharNumber` field
- **Validation**: Optional field (can be empty string)
- **Response**: Returns user with Aadhar number

#### Update User (`PUT /api/user/{id}`)
- **Request Body**: Now includes `AadharNumber` field
- **Validation**: Optional field (can be empty string)
- **Response**: Returns updated user with Aadhar number

#### Get All Users (`GET /api/user`)
- **Response**: Now includes `AadharNumber` field in user objects
- **Filtering**: Available in both active and inactive user lists

#### Get Active Users (`GET /api/user/active`)
- **Response**: Now includes `AadharNumber` field in user objects

#### Get User by ID (`GET /api/user/{id}`)
- **Response**: Now includes `AadharNumber` field in user object

## API Usage Examples

### Create User with Aadhar Number
```http
POST /api/user
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "John Doe",
  "address": "123 Main Street",
  "email": "john@example.com",
  "phone": "9876543210",
  "aadharNumber": "123456789012",
  "userRoleId": 1,
  "joiningDate": "2024-01-15T00:00:00Z",
  "isActive": true
}
```

### Update User with Aadhar Number
```http
PUT /api/user/123
Content-Type: application/json
Authorization: Bearer <token>

{
  "id": 123,
  "name": "John Doe",
  "address": "123 Main Street",
  "email": "john@example.com",
  "phone": "9876543210",
  "aadharNumber": "123456789012",
  "userRoleId": 1,
  "isActive": true,
  "joiningDate": "2024-01-15T00:00:00Z",
  "inactiveDate": null
}
```

### Get All Users Response
```json
[
  {
    "id": 1,
    "name": "John Doe",
    "address": "123 Main Street",
    "email": "john@example.com",
    "phone": "9876543210",
    "aadharNumber": "123456789012",
    "isActive": true,
    "joiningDate": "2024-01-15T00:00:00Z",
    "inactiveDate": null,
    "userRoleId": 1,
    "userRole": {
      "id": 1,
      "name": "Member",
      "description": "Regular member"
    }
  }
]
```

## Database Migration Details

### Migration Applied
- **Migration ID**: `20250803071025_AddAadharNumberToUser`
- **Status**: Applied successfully
- **Database**: PostgreSQL

### SQL Changes
```sql
-- Add AadharNumber column
ALTER TABLE "Users" ADD "AadharNumber" character varying(12) NOT NULL DEFAULT '';

-- Update existing records (if any)
UPDATE "Users" SET "AadharNumber" = '' WHERE "Id" = 1;
```

## Validation Rules

### Aadhar Number Format
- **Length**: Maximum 12 characters
- **Type**: String (alphanumeric)
- **Required**: No (optional field)
- **Default**: Empty string

### Input Validation
- **Empty String**: Allowed (optional field)
- **Null Values**: Converted to empty string
- **Length Check**: Enforced at database level (12 characters max)

## Frontend Integration

### Form Fields
- Add Aadhar number input field to user creation/editing forms
- Use appropriate input validation (12 characters max)
- Make field optional but recommended

### Display
- Show Aadhar number in user profile pages
- Include in user lists and detail views
- Consider masking for privacy (e.g., "1234****9012")

### Validation
```javascript
// Frontend validation example
function validateAadharNumber(aadhar) {
  if (aadhar && aadhar.length > 12) {
    return "Aadhar number must be 12 characters or less";
  }
  return null;
}
```

## Security Considerations

### Data Privacy
- **Sensitive Data**: Aadhar numbers are sensitive personal information
- **Storage**: Stored as plain text in database (consider encryption for production)
- **Display**: Consider masking in UI for privacy
- **Access Control**: Ensure proper authorization for viewing/editing

### Validation
- **Format Validation**: Consider adding format validation (12 digits)
- **Uniqueness**: Consider if Aadhar numbers should be unique per user
- **Verification**: Consider integration with Aadhar verification APIs

## Migration Notes

### Existing Data
- **Default Value**: All existing users have empty Aadhar number
- **Backward Compatibility**: All existing functionality continues to work
- **No Data Loss**: No existing data is affected

### Rollback
If needed, the migration can be rolled back:
```bash
dotnet ef database update 20250803045942_AddUserStatusAndMeetingMinutesFields
```

## Testing Scenarios

### Create User
1. Create user without Aadhar number (should work)
2. Create user with Aadhar number (should work)
3. Create user with Aadhar number > 12 characters (should fail)

### Update User
1. Update user to add Aadhar number
2. Update user to change Aadhar number
3. Update user to remove Aadhar number (set to empty)

### List Users
1. Verify Aadhar number appears in user lists
2. Verify Aadhar number appears in active user lists
3. Verify Aadhar number appears in individual user details

## Error Handling

### Common Errors
- **400 Bad Request**: Invalid Aadhar number format (if validation added)
- **500 Internal Server Error**: Database errors during migration
- **Validation Errors**: Length exceeds 12 characters

### Error Messages
- Database constraint violations for length > 12 characters
- Application-level validation errors (if implemented)

## Future Enhancements

### Potential Improvements
1. **Format Validation**: Add regex validation for 12-digit format
2. **Uniqueness Constraint**: Add unique constraint if needed
3. **Encryption**: Encrypt Aadhar numbers in database
4. **Verification API**: Integrate with Aadhar verification services
5. **Masking**: Implement automatic masking in API responses

### Additional Features
1. **Search by Aadhar**: Add search functionality
2. **Bulk Import**: Support Aadhar numbers in bulk user import
3. **Export**: Include Aadhar numbers in user data exports
4. **Audit Trail**: Track Aadhar number changes

## API Documentation Updates

### Swagger/OpenAPI
- Update API documentation to include Aadhar number field
- Add field descriptions and examples
- Update request/response schemas

### Postman Collections
- Update request examples to include Aadhar number
- Add test cases for Aadhar number validation
- Update response examples

## Compliance Notes

### Data Protection
- **GDPR**: Consider data retention policies for Aadhar numbers
- **Local Laws**: Ensure compliance with local data protection laws
- **Consent**: Ensure user consent for Aadhar number collection
- **Purpose**: Clearly define purpose of Aadhar number collection

### Security Best Practices
- **Encryption**: Consider encrypting Aadhar numbers at rest
- **Access Logs**: Log access to Aadhar number data
- **Audit Trail**: Track changes to Aadhar number fields
- **Data Minimization**: Only collect if necessary for business purpose 