# User Soft Delete Functionality Guide

## Overview
This guide explains the implementation of soft delete functionality for users in the Phoenix Sangam API. Instead of permanently deleting users, the system now marks them as inactive and prevents them from logging in.

## Changes Made

### 1. User Model
The User model already had the necessary fields:
- `IsActive` (boolean) - indicates if the user is active
- `InactiveDate` (nullable DateTime) - stores the date when the user was deactivated

### 2. Modified Endpoints

#### DELETE `/api/user/{id}` - Soft Delete User
- **Before**: Permanently removed user from database
- **After**: Marks user as inactive and sets `InactiveDate` to current date
- **Authorization**: Secretary, President, Treasurer roles
- **Response**: 204 No Content on success

#### POST `/api/user/login` - Enhanced Login
- **Added**: Check for user's active status before allowing login
- **Response**: Returns "Account is deactivated. Please contact administrator." for inactive users

#### GET `/api/user` - Get All Users
- **Added**: Optional query parameter `includeInactive` (default: true)
- **Usage**: 
  - `GET /api/user` - returns all users (active and inactive)
  - `GET /api/user?includeInactive=false` - returns only active users

#### GET `/api/user/active` - Get Active Users Only
- **New Endpoint**: Returns only active users
- **Authorization**: All authenticated users
- **Response**: List of active users only

#### PUT `/api/user/{id}/reactivate` - Reactivate User
- **New Endpoint**: Reactivates a deactivated user
- **Authorization**: Secretary, President, Treasurer roles
- **Actions**: 
  - Sets `IsActive = true`
  - Clears `InactiveDate` (sets to null)
- **Response**: Updated user object

## API Usage Examples

### Deactivate a User
```http
DELETE /api/user/123
Authorization: Bearer <token>
```
Response: 204 No Content

### Reactivate a User
```http
PUT /api/user/123/reactivate
Authorization: Bearer <token>
```
Response: 200 OK with updated user object

### Get All Users (including inactive)
```http
GET /api/user
Authorization: Bearer <token>
```

### Get Only Active Users
```http
GET /api/user?includeInactive=false
Authorization: Bearer <token>
```
OR
```http
GET /api/user/active
Authorization: Bearer <token>
```

### Login Attempt by Inactive User
```http
POST /api/user/login
Content-Type: application/json

{
  "username": "inactive@example.com",
  "password": "password"
}
```
Response: 401 Unauthorized with message "Account is deactivated. Please contact administrator."

## Database Considerations

### Existing Data
- All existing users will have `IsActive = true` by default
- `InactiveDate` will be null for all existing users

### Data Integrity
- User data is preserved even after deactivation
- Historical records (attendance, payments, etc.) remain intact
- Users can be reactivated at any time

## Security Features

1. **Login Prevention**: Inactive users cannot log in to the system
2. **Role-Based Access**: Only authorized roles can deactivate/reactivate users
3. **Audit Trail**: `InactiveDate` provides timestamp of when user was deactivated
4. **Reversible**: Users can be reactivated without data loss

## Frontend Integration

### User Management
- Display active/inactive status in user lists
- Show inactive date for deactivated users
- Provide reactivate button for inactive users

### Login Handling
- Handle "Account is deactivated" error message
- Display appropriate user-friendly message
- Redirect to contact administrator page

### User Lists
- Add filter options for active/inactive users
- Use `/api/user/active` endpoint for dropdowns that should only show active users
- Use `/api/user?includeInactive=false` for filtered lists

## Migration Notes

1. **No Database Migration Required**: The User model already has the necessary fields
2. **Backward Compatibility**: All existing endpoints continue to work
3. **Default Behavior**: New users are created with `IsActive = true`
4. **Existing Users**: All existing users remain active unless explicitly deactivated

## Testing Scenarios

1. **Deactivate User**: Verify user cannot login after deactivation
2. **Reactivate User**: Verify user can login after reactivation
3. **List Filtering**: Test active/inactive user filtering
4. **Authorization**: Ensure only authorized roles can deactivate/reactivate
5. **Data Integrity**: Verify user data remains intact after deactivation

## Error Handling

- **404 Not Found**: User doesn't exist
- **400 Bad Request**: User is already active (for reactivation)
- **401 Unauthorized**: Login attempt by inactive user
- **403 Forbidden**: Insufficient permissions for deactivation/reactivation
- **500 Internal Server Error**: Database or system errors 