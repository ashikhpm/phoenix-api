# Debugging Guide for Phoenix Sangam API

## 🚀 Quick Start

1. **Run the debug script:**
   ```powershell
   .\debug.ps1
   ```

2. **Or manually start debugging:**
   ```powershell
   dotnet run --profile Debug
   ```

## 🔧 Debugging Features Added

### 1. Enhanced Logging
- **Detailed logging** in all controller methods
- **Error tracking** with full exception details
- **Performance monitoring** with timing information
- **Database connection status** logging

### 2. Health Check Endpoint
- **URL**: `GET /api/user/health`
- **Purpose**: Test database connection and get system status
- **Returns**: Database connection status, user count, timestamp

### 3. Debug Configuration
- **Profile**: "Debug" in launchSettings.json
- **Ports**: HTTPS: 7001, HTTP: 5001
- **Environment**: Development
- **Auto-launch**: Swagger UI

## 🐛 Common Issues & Solutions

### Database Connection Issues

**Problem**: Cannot connect to SQL Server
```json
// Check your connection string in appsettings.json
{
  "Database": {
    "Provider": "SqlServer",
    "DefaultConnection": "Server=LAPTOP-U9M78M5A;Database=phoenix;User Id=smart;Password=Abcd@1234;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

**Solutions**:
1. Verify SQL Server is running
2. Check firewall settings
3. Ensure user has proper permissions
4. Test connection with SQL Server Management Studio

### Migration Issues

**Problem**: Migrations not applying
```powershell
# Clean and recreate migrations
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Build Issues

**Problem**: File lock errors
```powershell
# Kill any running dotnet processes
taskkill /f /im dotnet.exe
dotnet clean
dotnet build
```

## 📊 Debugging Endpoints

### 1. Health Check
```http
GET https://localhost:7001/api/user/health
```

**Expected Response**:
```json
{
  "databaseConnected": true,
  "userCount": 2,
  "timestamp": "2025-07-26T16:30:00.000Z"
}
```

### 2. Get All Users
```http
GET https://localhost:7001/api/user
```

### 3. Create User
```http
POST https://localhost:7001/api/user
Content-Type: application/json

{
  "name": "Test User",
  "email": "test@example.com",
  "address": "123 Test St",
  "phone": "555-0123"
}
```

## 🔍 Logging Levels

The application uses structured logging with different levels:

- **Information**: Normal operations
- **Warning**: Non-critical issues
- **Error**: Exceptions and failures
- **Debug**: Detailed debugging information

## 🛠️ Development Tools

### 1. Swagger UI
- **URL**: `https://localhost:7001/swagger`
- **Features**: Interactive API documentation and testing

### 2. Entity Framework Tools
```powershell
# View migrations
dotnet ef migrations list

# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

### 3. Database Tools
```powershell
# Test connection
dotnet ef database update --verbose

# Remove database
dotnet ef database drop

# Create database
dotnet ef database update
```

## 📝 Debugging Checklist

- [ ] SQL Server is running
- [ ] Connection string is correct
- [ ] User has database permissions
- [ ] Migrations are applied
- [ ] Application builds successfully
- [ ] Health check endpoint responds
- [ ] Swagger UI is accessible
- [ ] CRUD operations work

## 🚨 Troubleshooting

### Application Won't Start
1. Check if ports are in use
2. Verify connection string
3. Ensure SQL Server is accessible
4. Check firewall settings

### Database Connection Fails
1. Test connection with SSMS
2. Verify server name and credentials
3. Check SQL Server configuration
4. Ensure database exists

### API Endpoints Return Errors
1. Check application logs
2. Verify request format
3. Test with Swagger UI
4. Check database connectivity

## 📞 Support

If you encounter issues:
1. Check the logs in the console output
2. Use the health check endpoint
3. Verify database connection
4. Test with Swagger UI 