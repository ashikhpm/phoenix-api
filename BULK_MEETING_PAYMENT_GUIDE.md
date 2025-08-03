# Bulk Meeting Payment Management Guide

## Overview
The meeting payment system now supports bulk operations that allow you to replace all payment records for a meeting with a new list. If existing payment records exist for a meeting, they will be deleted and replaced with the new list.

## New Endpoint

### Bulk Meeting Payment Creation/Replacement
**Endpoint:** `POST /api/MeetingPayment/bulk`

**Description:** Creates or replaces all payment records for a specific meeting. If existing records exist, they will be deleted and replaced with the new list.

**Request Format:**
```json
{
  "meetingId": 1,
  "payments": [
    {
      "userId": 1,
      "mainPayment": 1000.00,
      "weeklyPayment": 500.00
    },
    {
      "userId": 2,
      "mainPayment": 1500.00,
      "weeklyPayment": 750.00
    },
    {
      "userId": 3,
      "mainPayment": 800.00,
      "weeklyPayment": 400.00
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
    "mainPayment": 1000.00,
    "weeklyPayment": 500.00,
    "totalPayment": 1500.00,
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
    "mainPayment": 1500.00,
    "weeklyPayment": 750.00,
    "totalPayment": 2250.00,
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
- If payment records already exist for the meeting, they will be **deleted**
- New payment records will be created based on the provided list
- This ensures a clean replacement of all payment data

### 2. Validation
- Validates that the meeting exists
- Validates that all users exist
- Validates that payment amounts are non-negative
- Returns appropriate error messages for missing meetings, users, or invalid amounts

### 3. Transaction Safety
- All operations (delete + create) are performed in a single transaction
- If any part fails, the entire operation is rolled back
- Ensures data consistency

### 4. Comprehensive Response
- Returns complete payment records with user and meeting details
- Includes calculated total payment amounts
- Includes all created records in the response

## Usage Examples

### Replace All Payments for a Meeting
```http
POST /api/MeetingPayment/bulk
Content-Type: application/json

{
  "meetingId": 1,
  "payments": [
    {"userId": 1, "mainPayment": 1000.00, "weeklyPayment": 500.00},
    {"userId": 2, "mainPayment": 1500.00, "weeklyPayment": 750.00},
    {"userId": 3, "mainPayment": 800.00, "weeklyPayment": 400.00},
    {"userId": 4, "mainPayment": 1200.00, "weeklyPayment": 600.00}
  ]
}
```

### Update Single User Payment (Original Method)
```http
POST /api/MeetingPayment
Content-Type: application/json

{
  "userId": 1,
  "meetingId": 1,
  "mainPayment": 1000.00,
  "weeklyPayment": 500.00
}
```

## Behavior Changes

### Original CreateMeetingPayment Endpoint
- **Before:** Returned error if payment already existed
- **Now:** Updates existing payment if it exists, creates new if it doesn't
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
- Negative payment amounts

### 500 Internal Server Error
- Database errors
- Transaction failures

## Best Practices

### 1. Use Bulk Endpoint for Complete Updates
- When you want to replace all payments for a meeting
- When you have the complete list of payment data
- When you want to ensure no orphaned records

### 2. Use Single Endpoint for Individual Updates
- When updating payment for a single user
- When you don't have the complete list
- For incremental updates

### 3. Data Validation
- Always validate user and meeting IDs before sending
- Ensure all required users exist in the system
- Check meeting existence before bulk operations
- Validate payment amounts are non-negative

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

### Scenario 1: Complete Meeting Payment Update
```json
{
  "meetingId": 1,
  "payments": [
    {"userId": 1, "mainPayment": 1000.00, "weeklyPayment": 500.00},
    {"userId": 2, "mainPayment": 1500.00, "weeklyPayment": 750.00},
    {"userId": 3, "mainPayment": 800.00, "weeklyPayment": 400.00},
    {"userId": 4, "mainPayment": 1200.00, "weeklyPayment": 600.00}
  ]
}
```

### Scenario 2: Single User Payment Update
```json
{
  "userId": 1,
  "meetingId": 1,
  "mainPayment": 1200.00,
  "weeklyPayment": 600.00
}
```

## Payment Amount Validation

### Main Payment
- Must be non-negative (≥ 0)
- Supports decimal values
- No maximum limit

### Weekly Payment
- Must be non-negative (≥ 0)
- Supports decimal values
- No maximum limit

### Total Payment
- Automatically calculated as Main Payment + Weekly Payment
- Included in response for convenience

## Response Fields

### MeetingPaymentResponseDto
- **id:** Payment record ID
- **userId:** User ID
- **meetingId:** Meeting ID
- **mainPayment:** Main payment amount
- **weeklyPayment:** Weekly payment amount
- **totalPayment:** Calculated total (main + weekly)
- **createdAt:** Creation timestamp
- **user:** Complete user details
- **meeting:** Complete meeting details

The bulk meeting payment system provides a robust way to manage payment data with complete control over the replacement process, ensuring data consistency and providing flexible options for different use cases. 