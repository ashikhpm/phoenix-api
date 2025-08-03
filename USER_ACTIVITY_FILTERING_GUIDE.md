# User Activity Filtering System Guide

## Overview
The enhanced user activity filtering system provides comprehensive filtering capabilities for viewing and analyzing user activities with detailed information and proper filters in the request.

## New Enhanced Endpoints

### 1. **POST /api/UserActivity/filter** - Comprehensive Filtering
This is the main endpoint for viewing user activities with advanced filtering capabilities.

#### Request Body (UserActivityFilterDto)
```json
{
  "userId": 1,
  "userName": "John",
  "userRole": "Secretary",
  "action": "Create",
  "entityType": "User",
  "entityId": 5,
  "httpMethod": "POST",
  "endpoint": "/api/User",
  "ipAddress": "192.168.1.100",
  "isSuccess": true,
  "statusCode": 201,
  "minStatusCode": 200,
  "maxStatusCode": 299,
  "minDurationMs": 100,
  "maxDurationMs": 5000,
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-01-31T23:59:59Z",
  "description": "Created user",
  "errorMessage": "validation",
  "userAgent": "Mozilla",
  "detailsSearch": "email",
  "sortBy": "Timestamp",
  "sortDirection": "desc",
  "page": 1,
  "pageSize": 50,
  "includeUserDetails": true,
  "includeFormattedDetails": true,
  "includePerformanceMetrics": true
}
```

#### Response (UserActivityListResponseDto)
```json
{
  "activities": [
    {
      "id": 1,
      "userId": 1,
      "userName": "John Doe",
      "userRole": "Secretary",
      "action": "Create",
      "entityType": "User",
      "entityId": 5,
      "description": "Created new user",
      "details": "{\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}",
      "httpMethod": "POST",
      "endpoint": "POST /api/User",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "statusCode": 201,
      "isSuccess": true,
      "errorMessage": null,
      "timestamp": "2024-01-15T10:30:00Z",
      "durationMs": 150,
      "formattedDuration": "150ms",
      "statusCodeCategory": "Success",
      "performanceCategory": "Good",
      "formattedDetails": {
        "name": "Jane Smith",
        "email": "jane@example.com"
      },
      "user": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com",
        "address": "123 Main St",
        "phone": "1234567890",
        "userRoleId": 1
      }
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 50,
  "totalPages": 2,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "appliedFilters": {
    "userId": 1,
    "action": "Create",
    "page": 1,
    "pageSize": 50
  },
  "performanceMetrics": {
    "averageDurationMs": 245.5,
    "minDurationMs": 50,
    "maxDurationMs": 1200,
    "successRate": 95.5,
    "averageStatusCode": 201.2
  },
  "summary": {
    "totalActivities": 100,
    "retrievedActivities": 50,
    "successCount": 48,
    "failureCount": 2,
    "uniqueUsers": 5,
    "uniqueActions": 3,
    "uniqueEntityTypes": 2,
    "dateRange": {
      "earliest": "2024-01-01T00:00:00Z",
      "latest": "2024-01-15T23:59:59Z"
    }
  }
}
```

### 2. **GET /api/UserActivity/filter-options** - Available Filter Options
Get all available filter options for building filter interfaces.

#### Response
```json
{
  "actions": ["Create", "Update", "Delete", "View", "Login", "Logout"],
  "entityTypes": ["User", "Loan", "Meeting", "Attendance", "Payment"],
  "userRoles": ["Secretary", "President", "Treasurer", "Member"],
  "httpMethods": ["GET", "POST", "PUT", "DELETE"],
  "statusCodes": [200, 201, 400, 401, 403, 404, 500],
  "sortOptions": ["Timestamp", "UserId", "UserName", "Action", "EntityType", "StatusCode", "DurationMs"],
  "sortDirections": ["asc", "desc"]
}
```

## Filter Parameters

### **User Filters**
- `userId` (int?): Filter by specific user ID
- `userName` (string?): Filter by user name (partial match)
- `userRole` (string?): Filter by user role (exact match)

### **Action Filters**
- `action` (string?): Filter by action type (exact match)
- `entityType` (string?): Filter by entity type (exact match)
- `entityId` (int?): Filter by specific entity ID

### **Request Filters**
- `httpMethod` (string?): Filter by HTTP method (GET, POST, PUT, DELETE)
- `endpoint` (string?): Filter by endpoint path (partial match)
- `ipAddress` (string?): Filter by IP address (partial match)
- `userAgent` (string?): Filter by user agent (partial match)

### **Status Filters**
- `isSuccess` (bool?): Filter by success status
- `statusCode` (int?): Filter by exact status code
- `minStatusCode` (int?): Filter by minimum status code
- `maxStatusCode` (int?): Filter by maximum status code

### **Performance Filters**
- `minDurationMs` (long?): Filter by minimum duration
- `maxDurationMs` (long?): Filter by maximum duration

### **Date Filters**
- `startDate` (DateTime?): Filter by start date (inclusive)
- `endDate` (DateTime?): Filter by end date (inclusive)

### **Content Filters**
- `description` (string?): Filter by description (partial match)
- `errorMessage` (string?): Filter by error message (partial match)
- `detailsSearch` (string?): Search in JSON details field

### **Pagination & Sorting**
- `sortBy` (string?): Sort field (default: "Timestamp")
- `sortDirection` (string?): Sort direction "asc" or "desc" (default: "desc")
- `page` (int): Page number (default: 1, min: 1)
- `pageSize` (int): Page size (default: 50, max: 1000)

### **Response Options**
- `includeUserDetails` (bool): Include user information in response
- `includeFormattedDetails` (bool): Parse JSON details into objects
- `includePerformanceMetrics` (bool): Include performance metrics

## Enhanced Response Features

### **Formatted Duration**
```json
{
  "durationMs": 150,
  "formattedDuration": "150ms"
}
```

### **StatusCode Categories**
```json
{
  "statusCode": 201,
  "statusCodeCategory": "Success"
}
```

Categories:
- **Success** (200-299): Successful operations
- **Redirect** (300-399): Redirect responses
- **Client Error** (400-499): Client-side errors
- **Server Error** (500+): Server-side errors

### **Performance Categories**
```json
{
  "durationMs": 150,
  "performanceCategory": "Good"
}
```

Categories:
- **Excellent** (< 100ms): Very fast operations
- **Good** (100-500ms): Fast operations
- **Average** (500-1000ms): Normal operations
- **Slow** (1000-5000ms): Slow operations
- **Very Slow** (> 5000ms): Very slow operations

### **Formatted Details**
```json
{
  "details": "{\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}",
  "formattedDetails": {
    "name": "Jane Smith",
    "email": "jane@example.com"
  }
}
```

### **Performance Metrics**
```json
{
  "performanceMetrics": {
    "averageDurationMs": 245.5,
    "minDurationMs": 50,
    "maxDurationMs": 1200,
    "successRate": 95.5,
    "averageStatusCode": 201.2
  }
}
```

### **Summary Information**
```json
{
  "summary": {
    "totalActivities": 100,
    "retrievedActivities": 50,
    "successCount": 48,
    "failureCount": 2,
    "uniqueUsers": 5,
    "uniqueActions": 3,
    "uniqueEntityTypes": 2,
    "dateRange": {
      "earliest": "2024-01-01T00:00:00Z",
      "latest": "2024-01-15T23:59:59Z"
    }
  }
}
```

## Usage Examples

### **1. Get All Activities for a Specific User**
```json
{
  "userId": 1,
  "page": 1,
  "pageSize": 100,
  "includeUserDetails": true,
  "includeFormattedDetails": true
}
```

### **2. Get Failed Operations**
```json
{
  "isSuccess": false,
  "page": 1,
  "pageSize": 50,
  "includePerformanceMetrics": true
}
```

### **3. Get Slow Operations**
```json
{
  "minDurationMs": 1000,
  "sortBy": "DurationMs",
  "sortDirection": "desc",
  "page": 1,
  "pageSize": 50
}
```

### **4. Get Activities by Date Range**
```json
{
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-01-31T23:59:59Z",
  "page": 1,
  "pageSize": 100,
  "includePerformanceMetrics": true
}
```

### **5. Get Activities by Action Type**
```json
{
  "action": "Create",
  "entityType": "User",
  "page": 1,
  "pageSize": 50,
  "includeFormattedDetails": true
}
```

### **6. Search in Details**
```json
{
  "detailsSearch": "email",
  "page": 1,
  "pageSize": 50,
  "includeFormattedDetails": true
}
```

### **7. Get Server Errors**
```json
{
  "minStatusCode": 500,
  "page": 1,
  "pageSize": 50,
  "includePerformanceMetrics": true
}
```

### **8. Get Recent Activities**
```json
{
  "startDate": "2024-01-15T00:00:00Z",
  "endDate": "2024-01-15T23:59:59Z",
  "sortBy": "Timestamp",
  "sortDirection": "desc",
  "page": 1,
  "pageSize": 100
}
```

## Advanced Filtering Techniques

### **Combining Multiple Filters**
```json
{
  "userId": 1,
  "action": "Create",
  "entityType": "User",
  "isSuccess": true,
  "minDurationMs": 100,
  "maxDurationMs": 1000,
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-01-31T23:59:59Z",
  "page": 1,
  "pageSize": 50,
  "includeUserDetails": true,
  "includeFormattedDetails": true,
  "includePerformanceMetrics": true
}
```

### **Performance Analysis**
```json
{
  "includePerformanceMetrics": true,
  "page": 1,
  "pageSize": 1000,
  "sortBy": "DurationMs",
  "sortDirection": "desc"
}
```

### **Error Analysis**
```json
{
  "isSuccess": false,
  "includePerformanceMetrics": true,
  "page": 1,
  "pageSize": 100,
  "sortBy": "Timestamp",
  "sortDirection": "desc"
}
```

## Best Practices

### **1. Efficient Filtering**
- Use specific filters to reduce result set size
- Combine filters for precise results
- Use pagination for large datasets

### **2. Performance Considerations**
- Limit page size for large datasets
- Use date ranges to limit data
- Enable performance metrics only when needed

### **3. Data Analysis**
- Use `includePerformanceMetrics` for analysis
- Use `includeFormattedDetails` for detailed inspection
- Use `includeUserDetails` for user context

### **4. Error Monitoring**
- Filter by `isSuccess: false` for error analysis
- Use `minStatusCode: 400` for client errors
- Use `minStatusCode: 500` for server errors

### **5. Security Monitoring**
- Filter by specific IP addresses
- Monitor failed login attempts
- Track unusual activity patterns

## Integration Examples

### **Frontend Integration**
```javascript
// Get filter options
const filterOptions = await fetch('/api/UserActivity/filter-options');

// Apply filters
const activities = await fetch('/api/UserActivity/filter', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    userId: 1,
    startDate: '2024-01-01T00:00:00Z',
    endDate: '2024-01-31T23:59:59Z',
    includeUserDetails: true,
    includeFormattedDetails: true,
    includePerformanceMetrics: true
  })
});
```

### **Monitoring Dashboard**
```javascript
// Get recent activities for dashboard
const recentActivities = await fetch('/api/UserActivity/filter', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    startDate: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    endDate: new Date().toISOString(),
    pageSize: 100,
    includePerformanceMetrics: true
  })
});
```

The enhanced user activity filtering system provides comprehensive capabilities for viewing, analyzing, and monitoring user activities with detailed information and proper filters in the request. 