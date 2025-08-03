# Bulk Attendance Management Guide

## Overview
The attendance system now supports bulk operations that allow you to replace all attendance records for a meeting with a new list. If existing attendance records exist for a meeting, they will be deleted and replaced with the new list.

## New Endpoint

### Bulk Attendance Creation/Replacement
**Endpoint:** `POST /api/Attendance/bulk`

**Description:** Creates or replaces all attendance records for a specific meeting. If existing records exist, they will be deleted and replaced with the new list.

**Request Format:**
```json
{
  "meetingId": 1,
  "attendances": [
    {
      "userId": 1,
      "isPresent": true
    },
    {
      "userId": 2,
      "isPresent": false
    },
    {
      "userId": 3,
      "isPresent": true
    }
  ]
}
```

**Response Format:**
```json
[
  {
    "id": 1,
    "userId": 1,
    "meetingId": 1,
    "isPresent": true,
    "createdAt": "2024-01-15T09:00:00",
    "user": {
      "id": 1,
      "name": "John Doe",
      "address": "123 Main St",
      "email": "john@example.com",
      "phone": "1234567890"
    },
    "meeting": {
      "id": 1,
      "date": "2024-01-15T00:00:00",
      "time": "2024-01-15T09:00:00",
      "description": "Weekly Meeting",
      "location": "Main Hall"
    }
  },
  {
    "id": 2,
    "userId": 2,
    "meetingId": 1,
    "isPresent": false,
    "createdAt": "2024-01-15T09:00:00",
    "user": {
      "id": 2,
      "name": "Jane Smith",
      "address": "456 Oak Ave",
      "email": "jane@example.com",
      "phone": "0987654321"
    },
    "meeting": {
      "id": 1,
      "date": "2024-01-15T00:00:00",
      "time": "2024-01-15T09:00:00",
      "description": "Weekly Meeting",
      "location": "Main Hall"
    }
  }
]
```

## Key Features

### 1. Replace Existing Records
- If attendance records already exist for the meeting, they will be **deleted**
- New attendance records will be created based on the provided list
- This ensures a clean replacement of all attendance data

### 2. Validation
- Validates that the meeting exists
- Validates that all users exist
- Returns appropriate error messages for missing meetings or users

### 3. Transaction Safety
- All operations (delete + create) are performed in a single transaction
- If any part fails, the entire operation is rolled back
- Ensures data consistency

### 4. Comprehensive Response
- Returns complete attendance records with user and meeting details
- Includes all created records in the response

## Usage Examples

### Replace All Attendance for a Meeting
```http
POST /api/Attendance/bulk
Content-Type: application/json

{
  "meetingId": 1,
  "attendances": [
    {"userId": 1, "isPresent": true},
    {"userId": 2, "isPresent": false},
    {"userId": 3, "isPresent": true},
    {"userId": 4, "isPresent": false}
  ]
}
```

### Update Single User Attendance (Original Method)
```http
POST /api/Attendance
Content-Type: application/json

{
  "userId": 1,
  "meetingId": 1,
  "isPresent": true
}
```

## Behavior Changes

### Original CreateAttendance Endpoint
- **Before:** Returned error if attendance already existed
- **Now:** Updates existing attendance if it exists, creates new if it doesn't
- **Benefit:** More flexible, no duplicate key errors

### New Bulk Endpoint
- **Complete Replacement:** Deletes all existing records for the meeting
- **Bulk Operation:** Handles multiple users in a single request
- **Atomic Operation:** All operations succeed or fail together

## Error Handling

### 404 Not Found
- Meeting doesn't exist
- User doesn't exist

### 400 Bad Request
- Invalid request format
- Missing required fields
- One or more users not found

### 500 Internal Server Error
- Database errors
- Transaction failures

## Best Practices

### 1. Use Bulk Endpoint for Complete Updates
- When you want to replace all attendance for a meeting
- When you have the complete list of attendance data
- When you want to ensure no orphaned records

### 2. Use Single Endpoint for Individual Updates
- When updating attendance for a single user
- When you don't have the complete list
- For incremental updates

### 3. Data Validation
- Always validate user and meeting IDs before sending
- Ensure all required users exist in the system
- Check meeting existence before bulk operations

### 4. Error Handling
- Handle 400 errors for validation issues
- Handle 404 errors for missing resources
- Implement retry logic for 500 errors

## Performance Considerations

### Bulk Operations
- More efficient for large datasets
- Single database transaction
- Reduced network overhead

### Single Operations
- Better for real-time updates
- Lower memory usage
- More granular error handling

## Migration Notes

- **No Breaking Changes:** Existing endpoints continue to work
- **Enhanced Functionality:** Original endpoint now updates existing records
- **New Capability:** Bulk endpoint for complete replacements
- **Backward Compatible:** All existing code will continue to work

## Example Scenarios

### Scenario 1: Complete Meeting Attendance Update
```json
{
  "meetingId": 1,
  "attendances": [
    {"userId": 1, "isPresent": true},
    {"userId": 2, "isPresent": true},
    {"userId": 3, "isPresent": false},
    {"userId": 4, "isPresent": true}
  ]
}
```

### Scenario 2: Single User Update
```json
{
  "userId": 1,
  "meetingId": 1,
  "isPresent": false
}
```

The bulk attendance system provides a robust way to manage attendance data with complete control over the replacement process, ensuring data consistency and providing flexible options for different use cases. 