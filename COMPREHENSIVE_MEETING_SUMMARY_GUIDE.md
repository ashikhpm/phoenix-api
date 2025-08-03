# Comprehensive Meeting Summary Endpoint Guide

## Overview
The new comprehensive meeting summary endpoint provides detailed information about a specific meeting, including meeting details, attended and absent users, meeting minutes, and attendance statistics.

## Endpoint Details

### URL
```
GET /api/meeting/{meetingId}/comprehensive-summary
```

### Authorization
- **Roles Required**: Secretary, President, Treasurer
- **Authentication**: JWT Token required

### Parameters
- `meetingId` (int, path parameter): The ID of the meeting to get the summary for

## Response Structure

### ComprehensiveMeetingSummaryDto
```json
{
  "meetingId": 1,
  "description": "Monthly General Meeting",
  "date": "2024-01-15T00:00:00Z",
  "time": "2024-01-15T10:00:00Z",
  "location": "Main Hall",
  "meetingMinutes": "Meeting minutes content...",
  "attendedUsers": [
    {
      "userId": 1,
      "userName": "John Doe",
      "email": "john@example.com",
      "phone": "1234567890",
      "role": "Member",
      "joiningDate": "2023-01-01T00:00:00Z",
      "inactiveDate": null,
      "isActive": true,
      "absenceReason": ""
    }
  ],
  "absentUsers": [
    {
      "userId": 2,
      "userName": "Jane Smith",
      "email": "jane@example.com",
      "phone": "0987654321",
      "role": "Member",
      "joiningDate": "2023-02-01T00:00:00Z",
      "inactiveDate": null,
      "isActive": true,
      "absenceReason": "Absent without specific reason"
    }
  ],
  "attendanceStats": {
    "totalEligibleUsers": 50,
    "attendedCount": 35,
    "absentCount": 15,
    "inactiveUsersCount": 0,
    "notYetJoinedCount": 0,
    "attendancePercentage": 70.0,
    "absenceReasons": [
      "Absent without specific reason",
      "User had not joined yet at meeting date",
      "User was inactive at meeting date"
    ]
  },
  "generatedAt": "2024-01-16T10:30:00Z"
}
```

## Features

### 1. Meeting Details
- **Meeting ID**: Unique identifier for the meeting
- **Description**: Meeting title/description
- **Date & Time**: When the meeting was held
- **Location**: Where the meeting took place
- **Meeting Minutes**: Content of the meeting minutes

### 2. Attended Users List
- Complete list of users who attended the meeting
- Includes user details: name, email, phone, role
- Shows joining date and current status
- Only includes users who were eligible to attend (active and joined before meeting date)

### 3. Absent Users List
- Complete list of users who were absent from the meeting
- Includes user details: name, email, phone, role
- Shows joining date and current status
- **Absence Reason**: Automatically calculated based on user status and meeting date

### 4. Attendance Statistics
- **Total Eligible Users**: Number of users who should have been able to attend
- **Attended Count**: Number of users who actually attended
- **Absent Count**: Number of users who were absent
- **Attendance Percentage**: Percentage of eligible users who attended
- **Absence Reasons**: List of unique reasons why users were absent

## Absence Reason Logic

The system automatically determines absence reasons based on:

1. **User is currently inactive**: "User is currently inactive"
2. **No joining date recorded**: "User has no joining date recorded"
3. **Joined after meeting date**: "User had not joined yet at meeting date"
4. **Was inactive at meeting date**: "User was inactive at meeting date"
5. **No specific reason**: "Absent without specific reason"

## Eligibility Criteria

A user is considered eligible to attend a meeting if:
- User is currently active (`IsActive = true`)
- User has a joining date (`JoiningDate` is not null)
- User joined before or on the meeting date (`JoiningDate <= MeetingDate`)
- User was not inactive on the meeting date (`InactiveDate > MeetingDate` or `InactiveDate` is null)

## Usage Examples

### Get Comprehensive Summary for Meeting ID 1
```bash
curl -X GET "https://api.example.com/api/meeting/1/comprehensive-summary" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

### Response Example
```json
{
  "meetingId": 1,
  "description": "Monthly General Meeting - January 2024",
  "date": "2024-01-15T00:00:00Z",
  "time": "2024-01-15T10:00:00Z",
  "location": "Main Hall",
  "meetingMinutes": "1. Welcome and introductions\n2. Review of previous minutes\n3. Financial report\n4. New business\n5. Adjournment",
  "attendedUsers": [
    {
      "userId": 1,
      "userName": "John Doe",
      "email": "john@example.com",
      "phone": "1234567890",
      "role": "Secretary",
      "joiningDate": "2023-01-01T00:00:00Z",
      "inactiveDate": null,
      "isActive": true,
      "absenceReason": ""
    }
  ],
  "absentUsers": [
    {
      "userId": 2,
      "userName": "Jane Smith",
      "email": "jane@example.com",
      "phone": "0987654321",
      "role": "Member",
      "joiningDate": "2023-02-01T00:00:00Z",
      "inactiveDate": null,
      "isActive": true,
      "absenceReason": "Absent without specific reason"
    }
  ],
  "attendanceStats": {
    "totalEligibleUsers": 25,
    "attendedCount": 20,
    "absentCount": 5,
    "inactiveUsersCount": 0,
    "notYetJoinedCount": 0,
    "attendancePercentage": 80.0,
    "absenceReasons": [
      "Absent without specific reason"
    ]
  },
  "generatedAt": "2024-01-16T10:30:00Z"
}
```

## Error Responses

### Meeting Not Found (404)
```json
{
  "message": "Meeting with ID 999 not found"
}
```

### Unauthorized (401)
```json
{
  "message": "User not authenticated"
}
```

### Forbidden (403)
```json
{
  "message": "Access denied"
}
```

### Internal Server Error (500)
```json
{
  "message": "An error occurred while generating the comprehensive meeting summary"
}
```

## Activity Logging

This endpoint includes comprehensive activity logging:
- **Operation**: "View"
- **Entity**: "MeetingSummary"
- **Details**: Includes meeting ID, description, attendance statistics
- **Performance**: Response time tracking
- **Success/Failure**: Detailed logging for both scenarios

## Benefits

1. **Complete Meeting Overview**: Get all meeting information in one request
2. **Accurate Attendance Tracking**: Based on user joining/inactive dates
3. **Detailed Absence Analysis**: Automatic reason determination
4. **Statistical Insights**: Attendance percentages and trends
5. **Audit Trail**: Full activity logging for compliance

## Integration Notes

- This endpoint replaces the need for multiple API calls to get meeting details, attendance, and minutes
- The absence calculation is based on historical user status at the meeting date
- All timestamps are in UTC format
- The response includes a `generatedAt` timestamp for tracking when the summary was created 