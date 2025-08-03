# User Roles System Guide

## Overview
The application now supports multiple user roles with different access levels. The system includes four roles: Secretary, President, Treasurer, and Member.

## User Roles

### 1. Secretary (ID: 1)
- **Description:** Secretary with full access
- **Access Level:** Full administrative access
- **Permissions:** All system features

### 2. President (ID: 2) - NEW
- **Description:** President with full access
- **Access Level:** Full administrative access (same as Secretary)
- **Permissions:** All system features

### 3. Treasurer (ID: 3) - NEW
- **Description:** Treasurer with full access
- **Access Level:** Full administrative access (same as Secretary)
- **Permissions:** All system features

### 4. Member (ID: 4)
- **Description:** Regular member with limited access
- **Access Level:** Limited access
- **Permissions:** View own loans, create loan requests, view own data

## Role-Based Access Control

### Admin Roles (Secretary, President, Treasurer)
Users with these roles have full access to all system features:

#### User Management
- ✅ View all users (excluding admin roles)
- ✅ Create new users
- ✅ Update user information
- ✅ Delete users

#### Meeting Management
- ✅ View all meetings
- ✅ Create meetings
- ✅ Update meetings
- ✅ Delete meetings
- ✅ View meeting details and summaries

#### Loan Management
- ✅ View all loans
- ✅ Create loans
- ✅ Update loans
- ✅ Delete loans
- ✅ Process loan repayments
- ✅ View loan requests
- ✅ Process loan requests (approve/reject)

#### Attendance Management
- ✅ View all attendance records
- ✅ Create attendance records
- ✅ Update attendance records
- ✅ Delete attendance records
- ✅ Bulk attendance operations

#### Payment Management
- ✅ View all payment records
- ✅ Create payment records
- ✅ Update payment records
- ✅ Delete payment records
- ✅ Bulk payment operations

#### Dashboard Access
- ✅ View all dashboard data
- ✅ Access all reports and summaries
- ✅ View all loans and requests

### Member Role
Users with the Member role have limited access:

#### User Management
- ❌ Cannot view other users
- ❌ Cannot create/update/delete users

#### Meeting Management
- ✅ View meetings (read-only)
- ❌ Cannot create/update/delete meetings

#### Loan Management
- ✅ View own loans only
- ✅ Create loan requests
- ✅ View own loan requests
- ❌ Cannot process loans or requests

#### Attendance Management
- ✅ View own attendance records
- ❌ Cannot create/update/delete attendance

#### Payment Management
- ✅ View own payment records
- ❌ Cannot create/update/delete payments

#### Dashboard Access
- ✅ View own dashboard data
- ✅ Access own reports and summaries
- ❌ Cannot view all system data

## Implementation Details

### Authorization Attributes
All admin-only endpoints now use:
```csharp
[Authorize(Roles = "Secretary,President,Treasurer")]
```

### Role Checking Logic
The system includes helper methods for role checking:
```csharp
// Check if user is admin (Secretary, President, Treasurer)
protected bool IsAdmin()

// Check if user is Secretary (for backward compatibility)
protected bool IsSecretary()
```

### Database Structure
- **UserRoles Table:** Contains role definitions
- **Users Table:** References UserRoles via UserRoleId
- **Seed Data:** Includes all four roles

### Migration
The new roles are added via migration:
- President (ID: 2)
- Treasurer (ID: 3)
- Member role updated to ID: 4

## User Creation

### Default Role Assignment
When creating new users, the system automatically assigns:
- **Default Role:** Member (ID: 4)
- **Reason:** Security - prevents accidental admin creation

### Admin User Creation
To create admin users, manually update the UserRoleId:
- Secretary: UserRoleId = 1
- President: UserRoleId = 2
- Treasurer: UserRoleId = 3

## Security Considerations

### Role-Based Filtering
- User lists exclude admin roles by default
- Dashboard data filtered based on user role
- Loan and request access controlled by role

### Authorization Checks
- All admin endpoints require appropriate role
- Client-side role checking for data filtering
- Server-side validation for all operations

### Audit Logging
- All admin operations are logged
- Role-based access attempts are tracked
- Failed authorization attempts are recorded

## Migration Notes

### Database Changes
1. **New Roles Added:** President and Treasurer
2. **Role ID Changes:** Member role moved to ID 4
3. **Seed Data Updated:** All roles properly seeded

### Application Changes
1. **Authorization Updated:** All admin endpoints support new roles
2. **Role Checking Enhanced:** New IsAdmin() method added
3. **User Filtering Updated:** Excludes all admin roles from user lists
4. **Default Role Updated:** New users get Member role (ID: 4)

### Backward Compatibility
- ✅ Existing Secretary users continue to work
- ✅ All existing functionality preserved
- ✅ No breaking changes to API contracts
- ✅ IsSecretary() method maintained for compatibility

## Best Practices

### Role Assignment
1. **Principle of Least Privilege:** Assign minimum required role
2. **Regular Review:** Periodically review user roles
3. **Secure Creation:** Admin users should be created carefully

### Access Control
1. **Client-Side Filtering:** Implement role-based UI
2. **Server-Side Validation:** Always validate on server
3. **Audit Trail:** Log all role-based operations

### User Management
1. **Role Documentation:** Clearly document role permissions
2. **Training:** Train users on role-based access
3. **Monitoring:** Monitor role-based access patterns

## Example Usage

### Creating Admin User
```json
{
  "name": "John President",
  "email": "president@example.com",
  "address": "123 Main St",
  "phone": "1234567890",
  "userRoleId": 2  // President role
}
```

### Role-Based API Access
```http
GET /api/Users
Authorization: Bearer <token>
# Returns only Member users (excludes admin roles)
```

### Dashboard Access
```http
GET /api/Dashboard/loans
Authorization: Bearer <token>
# Returns all loans for admin users, own loans for members
```

The role system provides flexible access control while maintaining security and usability for all user types. 